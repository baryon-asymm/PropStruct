using System.Text.RegularExpressions;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Checks the hand-written half of this node's precision-kind membership,
/// <c>src/Particle/RealFourWriteSites.txt</c>, against the generated half,
/// <c>src/Particle/RealFourAccumulators.generated.txt</c> (BOOT.md, "Accumulators",
/// "Design decision, 2026-09-24: the membership is generated too, not only the kind").
/// Two independent facts, both ways:
///
/// <list type="number">
/// <item>every generated row has a mapping entry, and every mapping entry names a
/// generated row (<see cref="AssertBothWaysConsistent"/>, <see cref="RealFilesEveryGeneratedRowAndMappingEntryAgree"/>);</item>
/// <item>every "ported" entry's write site actually calls <c>AddReal4</c>, not a plain
/// <c>+=</c>, at every one of the generated row's own Fortran-line citations
/// (<see cref="AssertPortedWriteSitesUseAddReal4"/>, <see cref="RealFilesEveryPortedWriteSiteUsesAddReal4"/>).</item>
/// </list>
///
/// Both checks are proven non-degenerate against synthetic fixtures, not just asserted
/// to pass on the real files (root BOOT.md taboo, "every check that guards a
/// quantitative claim is proven twice"): a deliberately removed entry, a deliberately
/// added bogus entry, and a deliberately reverted <c>AddReal4</c> each turn the
/// corresponding check red, and a correct synthetic fixture turns it green.
/// </summary>
public class RealFourAccumulatorMappingTests
{
    private static readonly string GeneratedPath = RepositoryPaths.Resolve("src", "Particle", "RealFourAccumulators.generated.txt");
    private static readonly string MappingPath = RepositoryPaths.Resolve("src", "Particle", "RealFourWriteSites.txt");
    private static readonly string AttemptCsPath = RepositoryPaths.Resolve("src", "Particle", "Attempt.cs");

    private static readonly int[] Line10 = [10];
    private static readonly int[] Line20 = [20];
    private static readonly int[] Line642 = [642];
    private static readonly int[] Line741 = [741];

    private readonly record struct GeneratedRow(string Name, IReadOnlyList<int> Lines);

    private abstract record Disposition;
    private sealed record Ported(string Identifier) : Disposition;
    private sealed record NotPorted(string Pointer) : Disposition;

    private readonly record struct MappingRow(string Name, Disposition Disposition);

    private static List<GeneratedRow> ParseGenerated(string text)
    {
        var rows = new List<GeneratedRow>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var cells = line.Split('|');
            Assert.True(cells.Length == 3, $"malformed generated row: {line}");
            var name = cells[0].Trim();
            var lines = cells[1].Trim().Split(',').Select(int.Parse).ToArray();
            rows.Add(new GeneratedRow(name, lines));
        }

        return rows;
    }

    private static List<MappingRow> ParseMapping(string text)
    {
        var rows = new List<MappingRow>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var cells = line.Split('|', 2);
            Assert.True(cells.Length == 2, $"malformed mapping row: {line}");
            var name = cells[0].Trim();
            var disposition = cells[1].Trim();

            if (disposition.StartsWith("ported:", StringComparison.Ordinal))
            {
                var identifier = disposition["ported:".Length..].Trim();
                Assert.True(identifier.Length > 0, $"{name}: 'ported' with no identifier");
                rows.Add(new MappingRow(name, new Ported(identifier)));
            }
            else if (disposition.StartsWith("not ported:", StringComparison.Ordinal)
                || disposition.StartsWith("not rounded:", StringComparison.Ordinal))
            {
                // "not rounded:" is ported code kept `double` by a recorded decision (a decision
                // variable or an attempt-plane local); like "not ported:", it has no AddReal4 site.
                var prefixLength = disposition.IndexOf(':') + 1;
                var pointer = disposition[prefixLength..].Trim();
                Assert.True(pointer.Length > 0, $"{name}: '{disposition[..(prefixLength - 1)]}' with no pointer");
                rows.Add(new MappingRow(name, new NotPorted(pointer)));
            }
            else
            {
                throw new InvalidOperationException($"{name}: unknown disposition '{disposition}' (expected 'ported: ...', 'not ported: ...' or 'not rounded: ...')");
            }
        }

        return rows;
    }

    /// <summary>
    /// Fact 1: every generated row has a mapping entry, every mapping entry names a
    /// generated row, and neither file names the same Fortran name twice.
    /// </summary>
    private static void AssertBothWaysConsistent(IReadOnlyList<GeneratedRow> generated, IReadOnlyList<MappingRow> mapping)
    {
        var duplicateGenerated = generated.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(n => n).ToArray();
        Assert.True(duplicateGenerated.Length == 0, $"generated file names the same Fortran name more than once: {string.Join(", ", duplicateGenerated)}");

        var duplicateMapping = mapping.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).OrderBy(n => n).ToArray();
        Assert.True(duplicateMapping.Length == 0, $"mapping file names the same Fortran name more than once: {string.Join(", ", duplicateMapping)}");

        var generatedNames = generated.Select(r => r.Name).ToHashSet();
        var mappingNames = mapping.Select(r => r.Name).ToHashSet();

        var missingEntries = generatedNames.Except(mappingNames).OrderBy(n => n).ToArray();
        var bogusEntries = mappingNames.Except(generatedNames).OrderBy(n => n).ToArray();

        Assert.True(missingEntries.Length == 0, $"generated row(s) with no mapping entry: {string.Join(", ", missingEntries)}");
        Assert.True(bogusEntries.Length == 0, $"mapping entry/entries naming no generated row: {string.Join(", ", bogusEntries)}");
    }

    /// <summary>
    /// Fact 2: for every "ported" mapping entry, the C# source names by
    /// <paramref name="cSharpText"/> calls <c>AddReal4(</c>, not a plain <c>+=</c>, on
    /// the line citing each of the generated row's own Fortran lines -- Attempt.cs's own
    /// convention of a trailing "(&lt;line&gt;)" (or "(&lt;line&gt;-&lt;line&gt;)" for a
    /// continued Fortran statement) comment next to every write site.
    /// </summary>
    private static void AssertPortedWriteSitesUseAddReal4(IReadOnlyList<GeneratedRow> generated, IReadOnlyList<MappingRow> mapping, string cSharpText)
    {
        var linesByName = generated.ToDictionary(r => r.Name, r => r.Lines);
        var codeLines = cSharpText.Split('\n');
        var problems = new List<string>();

        foreach (var row in mapping)
        {
            if (row.Disposition is not Ported)
            {
                continue;
            }

            if (!linesByName.TryGetValue(row.Name, out var fortranLines))
            {
                continue; // reported by AssertBothWaysConsistent already
            }

            foreach (var fortranLine in fortranLines)
            {
                var citation = new Regex($@"\({fortranLine}(-\d+)?\)");
                var matchLine = codeLines.FirstOrDefault(citation.IsMatch);
                if (matchLine is null)
                {
                    problems.Add($"{row.Name}: no C# line cites Fortran line {fortranLine}");
                    continue;
                }

                if (!matchLine.Contains("AddReal4("))
                {
                    problems.Add($"{row.Name}: the C# line citing Fortran {fortranLine} does not call AddReal4( -- '{matchLine.Trim()}'");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void RealFilesEveryGeneratedRowAndMappingEntryAgree()
    {
        var generated = ParseGenerated(File.ReadAllText(GeneratedPath));
        var mapping = ParseMapping(File.ReadAllText(MappingPath));

        AssertBothWaysConsistent(generated, mapping);
    }

    [Fact]
    public void RealFilesEveryPortedWriteSiteUsesAddReal4()
    {
        var generated = ParseGenerated(File.ReadAllText(GeneratedPath));
        var mapping = ParseMapping(File.ReadAllText(MappingPath));
        var attemptCs = File.ReadAllText(AttemptCsPath);

        AssertPortedWriteSitesUseAddReal4(generated, mapping, attemptCs);
    }

    [Fact]
    public void MissingMappingEntryFailsTheBothWaysCheck()
    {
        var generated = new[] { new GeneratedRow("foo", Line10) };
        var mapping = Array.Empty<MappingRow>();

        var ex = Record.Exception(() => AssertBothWaysConsistent(generated, mapping));

        Assert.NotNull(ex);
        Assert.Contains("foo", ex.Message);
    }

    [Fact]
    public void BogusMappingEntryFailsTheBothWaysCheck()
    {
        var generated = Array.Empty<GeneratedRow>();
        var mapping = new[] { new MappingRow("bar", new Ported("Bar")) };

        var ex = Record.Exception(() => AssertBothWaysConsistent(generated, mapping));

        Assert.NotNull(ex);
        Assert.Contains("bar", ex.Message);
    }

    [Fact]
    public void DuplicateGeneratedNameFailsTheBothWaysCheck()
    {
        var generated = new[] { new GeneratedRow("foo", Line10), new GeneratedRow("foo", Line20) };
        var mapping = new[] { new MappingRow("foo", new Ported("Foo")) };

        var ex = Record.Exception(() => AssertBothWaysConsistent(generated, mapping));

        Assert.NotNull(ex);
        Assert.Contains("foo", ex.Message);
    }

    [Fact]
    public void MatchingEntriesPassTheBothWaysCheck()
    {
        var generated = new[] { new GeneratedRow("foo", Line10) };
        var mapping = new[] { new MappingRow("foo", new Ported("Foo")) };

        AssertBothWaysConsistent(generated, mapping); // must not throw
    }

    [Fact]
    public void PortedWriteSiteStillUsingPlainPlusEqualsFailsTheWriteSiteCheck()
    {
        var generated = new[] { new GeneratedRow("foo", Line642) };
        var mapping = new[] { new MappingRow("foo", new Ported("foo")) };
        const string cSharpText = "        foo += bar; // (642)\n";

        var ex = Record.Exception(() => AssertPortedWriteSitesUseAddReal4(generated, mapping, cSharpText));

        Assert.NotNull(ex);
        Assert.Contains("foo", ex.Message);
        Assert.Contains("642", ex.Message);
    }

    [Fact]
    public void PortedWriteSiteWithNoCitationAtAllFailsTheWriteSiteCheck()
    {
        var generated = new[] { new GeneratedRow("foo", Line642) };
        var mapping = new[] { new MappingRow("foo", new Ported("foo")) };
        const string cSharpText = "        foo = AddReal4(foo, bar, roundToReal4);\n";

        var ex = Record.Exception(() => AssertPortedWriteSitesUseAddReal4(generated, mapping, cSharpText));

        Assert.NotNull(ex);
        Assert.Contains("foo", ex.Message);
    }

    [Fact]
    public void PortedWriteSiteUsingAddReal4PassesTheWriteSiteCheck()
    {
        var generated = new[] { new GeneratedRow("foo", Line642) };
        var mapping = new[] { new MappingRow("foo", new Ported("foo")) };
        const string cSharpText = "        foo = AddReal4(foo, bar, roundToReal4); // (642)\n";

        AssertPortedWriteSitesUseAddReal4(generated, mapping, cSharpText); // must not throw
    }

    [Fact]
    public void NotPortedEntryIsNeverCheckedAgainstCSharpText()
    {
        var generated = new[] { new GeneratedRow("foo", Line642) };
        var mapping = new[] { new MappingRow("foo", new NotPorted("BOOT.md, somewhere")) };
        const string cSharpText = ""; // no site at all -- must not be a problem for a "not ported" entry

        AssertPortedWriteSitesUseAddReal4(generated, mapping, cSharpText); // must not throw
    }

    /// <summary>
    /// A continued Fortran statement's own citation style, "(741-742)" (Attempt.cs's own
    /// convention when a statement spans a continuation line), must still match on its
    /// generated row's first physical line, 741.
    /// </summary>
    [Fact]
    public void RangedCitationMatchesOnTheStartingLine()
    {
        var generated = new[] { new GeneratedRow("foo", Line741) };
        var mapping = new[] { new MappingRow("foo", new Ported("foo")) };
        const string cSharpText = "        foo = AddReal4(foo, bar, roundToReal4); // (741-742)\n";

        AssertPortedWriteSitesUseAddReal4(generated, mapping, cSharpText); // must not throw
    }
}
