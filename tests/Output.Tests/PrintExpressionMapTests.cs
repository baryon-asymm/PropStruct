using System.Globalization;
using System.Text.RegularExpressions;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The three descriptions of the print-time expressions agree (<c>src/Output/BOOT.md</c>,
/// "## Print-time expressions"): <c>PrintExpressions.generated.txt</c> (what the source says),
/// <c>PrintExpressionSites.txt</c> (where each row lives, or why it is not ported) and
/// <c>ResultsMWriter.cs</c> (fragment lines ending <c>// Fortran n[, n…]</c>), in seven checks.
///
/// (1) every generated row has exactly one map row; (2) every map row names a generated row; (3) every
/// fragment of a ported row occurs on a line of the writer citing the row's line, the first fragment
/// inside the member the row names; (4) every cited line carries a fragment of a row it cites; (5) every
/// upstream member exists on its <c>PropStruct.Simulation</c> record; (6) every <c>not ported</c> reason
/// quotes a phrase found in the <c>BOOT.md</c> it names; (7) every upstream member and its row's line
/// stand in one paragraph of <c>src/Simulation/API.md</c>. A match is bounded by non-identifier
/// characters on the line as written, a fragment's blanks matching any blanks: <c>conditions[5] / fi</c>
/// is not found in <c>conditions[5] / fiPlusN</c>.
///
/// The map proves each transcription present where cited, operands and literals included. It does not
/// prove that nothing else is computed there, nor what a helper does (<c>ArrayPaddingTests</c> owns
/// <c>Take</c>), nor values (the statistical criterion's). Whether each pair is the same expression is
/// decided when the map is reviewed.
///
/// P3 and P4 are positive controls read where the checks read: the map rows of the thirteen sites of the
/// 2026-09-19 design carry the C# the design named, and the upstream rows are the nine the contract of
/// <c>src/Simulation/API.md</c> states. Seen red 2026-10-02 (<c>tests/Output.Tests/BOOT.md</c>,
/// "## Mutations" 12–19 and 23–25).
/// </summary>
public class PrintExpressionMapTests
{
    private const string NotPorted = "not ported:";

    private static readonly string OutputDirectory = RepositoryPaths.Resolve("src", "Output");

    private static readonly Regex Citation = new(@"// Fortran (\d+(?:, \d+)*)\s*$", RegexOptions.CultureInvariant);

    private static readonly Regex MemberDeclaration = new(@"^    (?:public|private|internal) static [^=]*?\b(\w+)\(", RegexOptions.CultureInvariant);

    private static readonly Regex TokenFi = new(@"(?<!\w)fi(?!\w)", RegexOptions.CultureInvariant);

    private static readonly Regex ReasonPhrase = new(@"\((root )?BOOT\.md, ""([^""]+)""\)\s*$", RegexOptions.CultureInvariant);

    private sealed record MapRow(int Line, string Role, string Expression, string Member, IReadOnlyList<string> Fragments, string Upstream)
    {
        public bool Ported => !Member.StartsWith(NotPorted, StringComparison.Ordinal);

        public string Method => Member["ResultsMWriter.".Length..];

        public string Key => $"{Line} | {Role} | {Expression}";
    }

    private sealed record CodeLine(int Number, string Text, IReadOnlyList<int> Cited, string Member);

    [Fact]
    public void EveryGeneratedRowHasExactlyOneMapRow()
    {
        var notMapped = MultisetMinus(GeneratedKeys(), MapRows().Select(r => r.Key).ToList());
        Assert.True(notMapped.Count == 0, "generated rows without a map row: " + string.Join("; ", notMapped));
    }

    [Fact]
    public void EveryMapRowNamesAGeneratedRow()
    {
        var notGenerated = MultisetMinus(MapRows().Select(r => r.Key).ToList(), GeneratedKeys());
        Assert.True(notGenerated.Count == 0, "map rows naming no generated row: " + string.Join("; ", notGenerated));
    }

    [Fact]
    public void EveryFragmentOfAPortedRowIsOnACitingLineAndTheFirstInsideItsMember()
    {
        var code = CodeLines();
        var problems = new List<string>();
        foreach (var row in MapRows().Where(r => r.Ported))
        {
            for (var i = 0; i < row.Fragments.Count; i++)
            {
                var fragment = row.Fragments[i];
                var matcher = FragmentMatcher(fragment);
                var carriers = code.Where(c => c.Cited.Contains(row.Line) && matcher.IsMatch(c.Text)).ToList();
                if (carriers.Count == 0)
                {
                    problems.Add($"{row.Key}: no line citing {row.Line} carries `{fragment}`");
                }
                else if (i == 0 && !carriers.Any(c => c.Member == row.Method))
                {
                    problems.Add($"{row.Key}: `{fragment}` is not inside {row.Method}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryCitedLineCarriesAFragmentOfARowItCites()
    {
        var ported = MapRows().Where(r => r.Ported).ToList();
        var problems = new List<string>();
        foreach (var line in CodeLines().Where(c => c.Cited.Count > 0))
        {
            foreach (var cited in line.Cited)
            {
                var carried = ported.Any(r => r.Line == cited && r.Fragments.Any(f => FragmentMatcher(f).IsMatch(line.Text)));
                if (!carried)
                {
                    problems.Add($"ResultsMWriter.cs:{line.Number} cites Fortran {cited}, whose ported rows have no fragment on that line");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryUpstreamMemberExistsOnItsSimulationRecord()
    {
        var assembly = typeof(SimulationResult).Assembly;
        var problems = new List<string>();
        foreach (var row in MapRows().Where(r => r.Upstream != "-"))
        {
            var (record, member) = SplitUpstream(row);
            var type = assembly.GetType("PropStruct.Simulation." + record);
            if (type is null || type.GetProperty(member) is null)
            {
                problems.Add($"{row.Key}: PropStruct.Simulation.{row.Upstream} does not exist");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryNotPortedReasonQuotesAPhraseOfTheBootItNames()
    {
        var own = Normalised(File.ReadAllText(Path.Combine(OutputDirectory, "BOOT.md")));
        var root = Normalised(File.ReadAllText(RepositoryPaths.Resolve("BOOT.md")));
        var problems = new List<string>();
        foreach (var row in MapRows().Where(r => !r.Ported))
        {
            var match = ReasonPhrase.Match(row.Member);
            if (!match.Success)
            {
                problems.Add($"{row.Key}: the reason does not end (BOOT.md, \"<phrase>\") or (root BOOT.md, \"<phrase>\")");
            }
            else if (!(match.Groups[1].Success ? root : own).Contains(Normalised(match.Groups[2].Value), StringComparison.Ordinal))
            {
                problems.Add($"{row.Key}: the phrase \"{match.Groups[2].Value}\" is in no {(match.Groups[1].Success ? "root " : string.Empty)}BOOT.md");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryUpstreamMemberAndItsLineStandInOneParagraphOfTheSimulationContract()
    {
        var paragraphs = ContractParagraphs();
        var problems = new List<string>();
        foreach (var row in MapRows().Where(r => r.Upstream != "-"))
        {
            var (_, member) = SplitUpstream(row);
            var line = new Regex($@"\b{row.Line}\b", RegexOptions.CultureInvariant);
            var name = new Regex($@"\b{Regex.Escape(member)}\b", RegexOptions.CultureInvariant);
            if (!paragraphs.Any(p => line.IsMatch(p) && name.IsMatch(p)))
            {
                problems.Add($"{row.Key}: no paragraph of src/Simulation/API.md names {member} and Fortran {row.Line}");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void TheMapRowsOfTheThirteenDesignedSitesCarryTheCSharpTheDesignNamed()
    {
        var rows = MapRows();
        var problems = new List<string>();

        for (var line = 1248; line <= 1252; line++)
        {
            RequireFragment(rows, problems, line, "item", fragments => fragments.Any(f => f.Contains("fiPlusN", StringComparison.Ordinal)), "fiPlusN");
        }

        for (var line = 1253; line <= 1256; line++)
        {
            RequireFragment(rows, problems, line, "item", fragments => fragments.All(f => !f.Contains("fiPlusN", StringComparison.Ordinal)) && fragments.Any(f => TokenFi.IsMatch(f)), "the token fi without fiPlusN");
        }

        RequireFragment(rows, problems, 1396, "array", fragments => fragments.Any(f => f.Contains("1.00001e6", StringComparison.Ordinal)), "1.00001e6");

        foreach (var line in new[] { 1370, 1373, 1377 })
        {
            RequireFragment(rows, problems, line, "length", fragments => fragments.Any(f => f.EndsWith("+ 2", StringComparison.Ordinal)), "+ 2");
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void TheUpstreamRowsAreTheNineTheSimulationContractStates()
    {
        var upstream = MapRows().Where(r => r.Upstream != "-").Select(r => r.Line).Distinct().Order().ToList();
        Assert.Equal([1202, 1353, 1357, 1364, 1370, 1373, 1377, 1386, 1418], upstream);
        Assert.All(MapRows().Where(r => r.Upstream != "-"), r => Assert.True(r.Ported, r.Key));
    }

    private static void RequireFragment(List<MapRow> rows, List<string> problems, int line, string role, Func<IReadOnlyList<string>, bool> accepts, string description)
    {
        var row = rows.SingleOrDefault(r => r.Line == line && r.Role == role);
        if (row is null || !row.Ported || !accepts(row.Fragments))
        {
            problems.Add($"the map row of the {role} at Fortran {line} does not carry {description}");
        }
    }

    private static (string Record, string Member) SplitUpstream(MapRow row)
    {
        var parts = row.Upstream.Split('.');
        Assert.True(parts.Length == 2, $"{row.Key}: the upstream form is <Record>.<Member>, not {row.Upstream}");
        return (parts[0], parts[1]);
    }

    private static List<string> GeneratedKeys() =>
        File.ReadAllLines(Path.Combine(OutputDirectory, "PrintExpressions.generated.txt"))
            .Where(l => !l.StartsWith('#'))
            .Select(l => l.Split(" | "))
            .Select(c => $"{int.Parse(c[0], CultureInfo.InvariantCulture)} | {c[1]} | {c[2]}")
            .ToList();

    private static List<MapRow> MapRows() =>
        File.ReadAllLines(Path.Combine(OutputDirectory, "PrintExpressionSites.txt"))
            .Where(l => !l.StartsWith('#') && l.Length > 0)
            .Select(l => l.Split(" | "))
            .Select(c => new MapRow(
                int.Parse(c[0], CultureInfo.InvariantCulture),
                c[1],
                c[2],
                c[3],
                c[4] == "-" ? [] : c[4].Split(" && "),
                c[5]))
            .ToList();

    /// <summary>
    /// Every line of <c>ResultsMWriter.cs</c> with the Fortran lines its trailing comment cites and the
    /// member declared at class level above it.
    /// </summary>
    private static List<CodeLine> CodeLines()
    {
        var member = string.Empty;
        var lines = new List<CodeLine>();
        var text = File.ReadAllLines(Path.Combine(OutputDirectory, "ResultsMWriter.cs"));
        for (var i = 0; i < text.Length; i++)
        {
            if (MemberDeclaration.Match(text[i]) is { Success: true } declaration)
            {
                member = declaration.Groups[1].Value;
            }

            var cited = Citation.Match(text[i]) is { Success: true } citation
                ? citation.Groups[1].Value.Split(", ").Select(n => int.Parse(n, CultureInfo.InvariantCulture)).ToList()
                : [];
            lines.Add(new CodeLine(i + 1, text[i], cited, member));
        }

        return lines;
    }

    /// <summary>
    /// A fragment as a pattern over a line as written: its own blanks match any blanks, and no
    /// identifier character may touch it where it begins or ends with one.
    /// </summary>
    private static Regex FragmentMatcher(string fragment)
    {
        var body = string.Join(@"\s*", fragment.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape));
        var before = IsIdentifierCharacter(fragment[0]) ? @"(?<!\w)" : string.Empty;
        var after = IsIdentifierCharacter(fragment[^1]) ? @"(?!\w)" : string.Empty;
        return new Regex(before + body + after, RegexOptions.CultureInvariant);
    }

    private static bool IsIdentifierCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string Normalised(string text) => Regex.Replace(text, @"\s+", " ");

    /// <summary>The paragraphs of the Simulation contract outside code fences, each on one line.</summary>
    private static List<string> ContractParagraphs()
    {
        var paragraphs = new List<string>();
        var current = new List<string>();
        var fenced = false;
        foreach (var line in File.ReadAllLines(RepositoryPaths.Resolve("src", "Simulation", "API.md")))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                Flush(paragraphs, current);
            }
            else if (fenced || line.Trim().Length == 0)
            {
                Flush(paragraphs, current);
            }
            else
            {
                current.Add(line);
            }
        }

        Flush(paragraphs, current);
        return paragraphs;
    }

    private static void Flush(List<string> paragraphs, List<string> current)
    {
        if (current.Count > 0)
        {
            paragraphs.Add(string.Join(' ', current));
            current.Clear();
        }
    }

    private static List<string> MultisetMinus(List<string> left, List<string> right)
    {
        var remaining = right.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var item in left)
        {
            if (remaining.TryGetValue(item, out var count) && count > 0)
            {
                remaining[item] = count - 1;
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }
}
