using System.Text.RegularExpressions;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// A corrected statement stands alone (<c>src/Statistics/BOOT.md</c>, "## Corrected model"): the block an
/// <c>if</c> on a <c>CorrectionSet</c> flag guards, and a statement that names a flag, calls no
/// <c>CyclePlaneRounding</c>, <c>CyclePlaneOrder</c> or <c>UnrolledSchedule</c> member and carries no
/// <c>// Fortran n</c> citation, which <c>CyclePlaneSiteMapTests</c> maps to a listing site that a corrected
/// expression does not have. The scan is textual, over <c>src/Statistics/*.cs</c>.
/// </summary>
public static partial class CorrectedStatementStructureTests
{
    /// <summary>Right: no guarded block or flag-naming statement of the node's sources uses a listing-driven helper.</summary>
    [Fact]
    public static void NoCorrectedStatementOfTheNodeUsesAListingDrivenHelper()
    {
        var findings = new List<string>();
        var guarded = 0;
        foreach (var path in Directory.GetFiles(RepositoryPaths.Resolve("src", "Statistics"), "*.cs").Order(StringComparer.Ordinal))
        {
            var blocks = GuardedBlocks(File.ReadAllText(path)).ToList();
            guarded += blocks.Count;
            findings.AddRange(blocks.SelectMany(block => Violations(block).Select(v => $"{Path.GetFileName(path)}: {v}: {block.Trim()}")));
        }

        Assert.Empty(findings);
        Assert.True(guarded >= 2, $"the scan found {guarded} guarded blocks; DoksdConsistent alone has two");
    }

    /// <summary>Red: one planted call of each forbidden kind, and one citation, in a guarded block, is a finding each.</summary>
    [Theory]
    [InlineData("var r = rounding.Store(x);", "rounding. or order.")]
    [InlineData("var o = order.Product(a, b);", "rounding. or order.")]
    [InlineData("var o = new CyclePlaneOrder(precision);", "CyclePlaneOrder")]
    [InlineData("var u = new UnrolledSchedule(n, 4);", "UnrolledSchedule")]
    [InlineData("x = y; // Fortran 1014", "// Fortran")]
    public static void APlantedHelperOrCitationInAGuardedBlockIsFound(string statement, string kind)
    {
        var source = $"if (corrections.HasFlag(CorrectionSet.ExactConstants))\n{{\n    {statement}\n}}\n";

        var blocks = GuardedBlocks(source).ToList();

        Assert.Collection(blocks, block => Assert.Contains(kind, Violations(block).Single(), StringComparison.Ordinal));
    }

    /// <summary>Red: a statement that names a flag in a conditional expression is guarded as a whole, up to its semicolon.</summary>
    [Fact]
    public static void AFlagNamingStatementIsScannedUpToItsSemicolon()
    {
        var source = "var c = corrections.HasFlag(CorrectionSet.ExactConstants) ? rounding.Store(a) : 3.0 / 3.14;\nvar d = 1.0;\n";

        var blocks = GuardedBlocks(source).ToList();

        Assert.Collection(blocks, block => Assert.Contains("rounding. or order.", Violations(block).Single(), StringComparison.Ordinal));
    }

    /// <summary>Right on a known answer: a clean guarded block, an <c>else</c> body and a plain statement are no finding.</summary>
    [Fact]
    public static void ACleanGuardedBlockIsNoFinding()
    {
        var source = "if (corrections.HasFlag(CorrectionSet.DoksdConsistent))\n{\n    dok2 += Math.Pow(u, 6) / (u - l);\n}\n"
            + "var rounded = rounding.Store(a);\nvar e = corrections.HasFlag(CorrectionSet.ExactConstants) ? 3.0 / Math.PI : 3.0 / 3.14;\n";

        var blocks = GuardedBlocks(source).ToList();

        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, block => Assert.Empty(Violations(block)));
    }

    private static IEnumerable<string> Violations(string block)
    {
        foreach (var forbidden in new[] { "CyclePlaneRounding", "CyclePlaneOrder", "UnrolledSchedule" })
        {
            if (block.Contains(forbidden, StringComparison.Ordinal))
            {
                yield return forbidden;
            }
        }

        if (PlaneCall().IsMatch(block))
        {
            yield return "rounding. or order. call";
        }

        if (Citation().IsMatch(block))
        {
            yield return "// Fortran";
        }
    }

    /// <summary>
    /// The text an <c>if</c> whose condition names <c>CorrectionSet.</c> guards (its condition and its braced block,
    /// or its one statement), and every other statement that names <c>CorrectionSet.</c>, from its line to its semicolon.
    /// </summary>
    private static IEnumerable<string> GuardedBlocks(string source)
    {
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("CorrectionSet.", StringComparison.Ordinal) || lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var isIf = lines[i].TrimStart().StartsWith("if (", StringComparison.Ordinal);
            var end = isIf ? BlockEnd(lines, i) : StatementEnd(lines, i);
            yield return string.Join('\n', lines[i..(end + 1)]);
            i = end;
        }
    }

    private static int StatementEnd(string[] lines, int start)
    {
        var end = start;
        while (end < lines.Length - 1 && !lines[end].TrimEnd().EndsWith(';'))
        {
            end++;
        }

        return end;
    }

    private static int BlockEnd(string[] lines, int start)
    {
        var next = start + 1;
        if (next >= lines.Length || lines[next].Trim() != "{")
        {
            return StatementEnd(lines, next < lines.Length ? next : start);
        }

        var depth = 0;
        for (var end = next; end < lines.Length; end++)
        {
            depth += lines[end].Count(c => c == '{') - lines[end].Count(c => c == '}');
            if (depth == 0)
            {
                return end;
            }
        }

        return lines.Length - 1;
    }

    [GeneratedRegex(@"\b(?:rounding|order)\.\w+\(")]
    private static partial Regex PlaneCall();

    [GeneratedRegex(@"// Fortran \d+")]
    private static partial Regex Citation();
}
