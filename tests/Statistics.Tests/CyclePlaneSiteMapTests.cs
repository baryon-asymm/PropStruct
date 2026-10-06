using System.Globalization;
using System.Text.RegularExpressions;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The per-cycle plane's three descriptions of one set of sites agree, in four directions
/// (src/Statistics/BOOT.md, "## Report", "The per-cycle plane from the executable's listing"):
/// <c>CyclePlane.listing.generated.txt</c> (what the executable does), <c>CyclePlaneSites.txt</c>
/// (where each site lives, or why it is not ported) and the <c>CyclePlaneRounding</c> calls of
/// <c>src/Statistics/*.cs</c>, each ending with a <c>// Fortran n[, n…]</c> comment. A rounding site
/// of the table is a row whose <c>rounds</c> column is yes; its call is <c>Temporary</c> for a
/// compiler temporary (kind T) and <c>Store</c> for every other kind, a sum's stores following its
/// <see cref="UnrolledSchedule"/>.
///
/// (1) every rounding site of the table has a map row and every map row names one, by address, lines,
/// kind and target; (2) every ported row is carried, in each member it names, by a call of its kind's
/// method citing one of its lines; (3) every <c>Store</c> or <c>Temporary</c> call cites lines, its
/// method has a ported row at one of them in its member, and every cited line is a line of such a
/// row, or a line a constant call of the same source line (<c>Literal</c>, <c>Fold</c>,
/// <c>Input</c>, which carry no row of their own: the table's <c>order</c> text holds the binary32
/// constants) shares with a row of the table. Directions 2 and 3 also close for this plane the gap
/// <c>Particle</c>'s own mapping test leaves open (it reads only the first citing line and has no
/// code-to-map direction).
/// </summary>
public partial class CyclePlaneSiteMapTests
{
    internal static readonly string StatisticsDirectory = Path.Combine(RepositoryPaths.Root, "src", "Statistics");

    private static readonly HashSet<string> ConstantMethods = new(StringComparer.Ordinal) { "Literal", "Fold", "Input" };

    private sealed record Site(string At, string Lines, string Kind, string Target)
    {
        public string Method => Kind == "T" ? "Temporary" : "Store";

        public IReadOnlyList<int> LineNumbers => Lines.Split('+').Select(n => int.Parse(n, CultureInfo.InvariantCulture)).ToList();

        public string Key => $"{At} | {Lines} | {Kind} | {Target}";
    }

    private sealed record MapRow(Site Site, string Members)
    {
        public bool Ported => !Members.StartsWith("not ported:", StringComparison.Ordinal);

        public IReadOnlyList<string> MemberList => Members.Split(", ");
    }

    /// <summary>One <c>.cs</c> file of the node, by name and lines (read from disk, or edited in memory by a proof).</summary>
    internal sealed record SourceFile(string Name, string[] Lines);

    /// <summary>One call of a plane object (<c>rounding.</c> or <c>order.</c>): where it is, its member, the Fortran lines its line cites.</summary>
    internal sealed record Call(string File, int SourceLine, int Column, string Member, string Method, IReadOnlyList<int> Cited);

    [Fact]
    public void GeneratedRoundingSitesAndTheMapAgreeBothWays()
    {
        var generated = RoundingSites().Select(s => s.Key).ToList();
        var mapped = MapRows().Select(r => r.Site.Key).ToList();

        var missingFromMap = MultisetMinus(generated, mapped);
        var notGenerated = MultisetMinus(mapped, generated);
        Assert.True(missingFromMap.Count == 0, "generated sites without a map row: " + string.Join("; ", missingFromMap));
        Assert.True(notGenerated.Count == 0, "map rows naming no generated site: " + string.Join("; ", notGenerated));
    }

    [Fact]
    public void EveryPortedRowIsCarriedByACallOfItsKindInEachMemberItNames()
    {
        var calls = RoundingCalls(StatisticsSources());
        var uncarried = MapRows()
            .Where(r => r.Ported)
            .SelectMany(r => r.MemberList.Select(member => (Row: r, Member: member)))
            .Where(x => !calls.Any(c => c.Member == x.Member && c.Method == x.Row.Site.Method && c.Cited.Intersect(x.Row.Site.LineNumbers).Any()))
            .Select(x => $"{x.Row.Site.Key} in {x.Member}")
            .ToList();
        Assert.True(uncarried.Count == 0, "ported rows with no matching call: " + string.Join("; ", uncarried));
    }

    [Fact]
    public void EveryRoundingCallCitesLinesOfRowsOfItsKindInItsMember()
    {
        var ported = MapRows().Where(r => r.Ported).ToList();
        var tableLines = TableLines();
        var problems = new List<string>();
        foreach (var line in RoundingCalls(StatisticsSources()).GroupBy(c => (c.File, c.SourceLine)))
        {
            var first = line.First();
            var where = $"{first.File}:{first.SourceLine}";
            var methods = line.Select(c => c.Method).ToHashSet(StringComparer.Ordinal);
            var rounding = methods.Where(m => !ConstantMethods.Contains(m)).ToList();
            if (first.Cited.Count == 0)
            {
                problems.Add($"{where} cites no Fortran line");
                continue;
            }

            foreach (var method in rounding)
            {
                if (!ported.Any(r => r.Site.Method == method && r.MemberList.Contains(first.Member) && first.Cited.Intersect(r.Site.LineNumbers).Any()))
                {
                    problems.Add($"{where}: {method} matches no ported row of {first.Member} at {string.Join(", ", first.Cited)}");
                }
            }

            foreach (var cited in first.Cited)
            {
                var carried = ported.Any(r => rounding.Contains(r.Site.Method) && r.MemberList.Contains(first.Member) && r.Site.LineNumbers.Contains(cited));
                var constant = methods.Overlaps(ConstantMethods) && (methods.Contains("Input") || tableLines.Contains(cited));
                if (!carried && !constant)
                {
                    problems.Add($"{where}: Fortran {cited} has no ported row of {first.Member} with a kind of {string.Join("/", rounding)}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void EveryUnrolledKindIsCarriedByAScheduleOfItsBlockSize()
    {
        var problems = new List<string>();
        foreach (var row in MapRows().Where(r => r.Ported && (r.Site.Kind.StartsWith('B') || r.Site.Kind.StartsWith('C'))))
        {
            var unroll = row.Site.Kind[1..];
            foreach (var member in row.MemberList)
            {
                var source = MemberSource(member);
                if (!UnrolledScheduleOf(unroll).IsMatch(source))
                {
                    problems.Add($"{row.Site.Key} in {member} has no UnrolledSchedule of {unroll}");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    /// <summary>The text of one member: from its declaration line to the next member declaration of the same file.</summary>
    private static string MemberSource(string member)
    {
        var parts = member.Split('.');
        var lines = File.ReadAllLines(Path.Combine(StatisticsDirectory, parts[0] + ".cs"));
        var start = Array.FindIndex(lines, l => MemberDeclaration().Match(l) is { Success: true } m && m.Groups[1].Value == parts[1]);
        Assert.True(start >= 0, $"{member} is not declared in {parts[0]}.cs");
        var end = Array.FindIndex(lines, start + 1, l => MemberDeclaration().IsMatch(l));
        return string.Join(Environment.NewLine, lines[start..(end < 0 ? lines.Length : end)]);
    }

    private static Regex UnrolledScheduleOf(string unroll) => new($@"new UnrolledSchedule\([^,)]+, {unroll}\)", RegexOptions.None, TimeSpan.FromSeconds(1));

    private static List<Site> RoundingSites() =>
        TableRows().Where(c => c[6] == "yes").Select(c => new Site(c[0], c[2], c[5], c[3])).ToList();

    private static HashSet<int> TableLines() =>
        TableRows().SelectMany(c => c[2].Split('+')).Select(n => int.Parse(n, CultureInfo.InvariantCulture)).ToHashSet();

    internal static List<string[]> TableRows() =>
        File.ReadAllLines(Path.Combine(StatisticsDirectory, "CyclePlane.listing.generated.txt"))
            .Where(l => !l.StartsWith('#') && l.Length > 0)
            .Select(l => l.Split(" | "))
            .ToList();

    private static List<MapRow> MapRows() =>
        File.ReadAllLines(Path.Combine(StatisticsDirectory, "CyclePlaneSites.txt"))
            .Where(l => !l.StartsWith('#') && l.Length > 0)
            .Select(l => l.Split(" | "))
            .Select(c => new MapRow(new Site(c[0], c[1], c[2], c[3]), c[4]))
            .ToList();

    /// <summary>Every <c>.cs</c> file of <c>src/Statistics</c>, as written.</summary>
    internal static List<SourceFile> StatisticsSources() =>
        Directory.EnumerateFiles(StatisticsDirectory, "*.cs").Order(StringComparer.Ordinal)
            .Select(path => new SourceFile(Path.GetFileName(path), File.ReadAllLines(path))).ToList();

    /// <summary>
    /// Every <c>rounding.Method(</c> call of the node's source, with the member it sits in
    /// (the enclosing method declared at class level, local functions belonging to it) and
    /// the Fortran lines its line's trailing comment cites.
    /// </summary>
    internal static List<Call> RoundingCalls(IEnumerable<SourceFile> sources) => Calls(sources, "CyclePlaneRounding.cs", RoundingCall());

    /// <summary>The same for <c>order.Method(</c>, the per-cycle plane's products (<c>CyclePlaneOrder</c>).</summary>
    internal static List<Call> OrderCalls(IEnumerable<SourceFile> sources) => Calls(sources, "CyclePlaneOrder.cs", OrderCall());

    private static List<Call> Calls(IEnumerable<SourceFile> sources, string excluded, Regex pattern)
    {
        var calls = new List<Call>();
        foreach (var source in sources.Where(s => !string.Equals(s.Name, excluded, StringComparison.Ordinal)))
        {
            var className = string.Empty;
            var member = string.Empty;
            var lines = source.Lines;
            for (var i = 0; i < lines.Length; i++)
            {
                var classMatch = ClassDeclaration().Match(lines[i]);
                if (classMatch.Success)
                {
                    className = classMatch.Groups[1].Value;
                }

                var memberMatch = MemberDeclaration().Match(lines[i]);
                if (memberMatch.Success)
                {
                    member = className + "." + memberMatch.Groups[1].Value;
                }

                var cited = Citation().Match(lines[i]) is { Success: true } citation
                    ? citation.Groups[1].Value.Split(", ").Select(n => int.Parse(n, CultureInfo.InvariantCulture)).ToList()
                    : [];
                foreach (Match call in pattern.Matches(lines[i]))
                {
                    calls.Add(new Call(source.Name, i + 1, call.Index, member, call.Groups[1].Value, cited));
                }
            }
        }

        return calls;
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

    [GeneratedRegex(@"^internal (?:static |readonly )*(?:class|struct) (\w+)")]
    private static partial Regex ClassDeclaration();

    [GeneratedRegex(@"^    (?:public|private|internal) static [^=]*?\b(\w+)\(")]
    private static partial Regex MemberDeclaration();

    [GeneratedRegex(@"// Fortran (\d+(?:, \d+)*)")]
    private static partial Regex Citation();

    [GeneratedRegex(@"\brounding\.(\w+)\(")]
    private static partial Regex RoundingCall();

    [GeneratedRegex(@"\border\.(\w+)\(")]
    private static partial Regex OrderCall();
}
