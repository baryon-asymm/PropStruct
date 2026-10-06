using System.Globalization;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// <c>src/Output/PrintExpressions.generated.txt</c> is generated, never typed: every print-time
/// expression of Fortran lines 1184–1453 found by <c>list-print-expressions.py</c> under the rule of
/// <c>src/Output/BOOT.md</c>, "## Print-time expressions". A fresh regeneration must reproduce the
/// committed file byte for byte, and the rule must pass its own self-test.
///
/// <see cref="TheThirteenSitesTheDesignNamedBeforeAnyScriptExistedAreRows"/> is the positive control of
/// the generator (P1): the thirteen sites below are the words of the 2026-09-19 design bullet of
/// <c>src/Output/BOOT.md</c> ("Design decisions (2026-09-19)": the conditions divided by <c>FI + N</c>
/// and by <c>FI</c>, the literal <c>1.00001e6</c> of line 1396, the <c>nmax + 2</c> lengths), written
/// before any script existed. Their fragments are the design's, not copies of the artefact under test,
/// so the artefact is read against an answer it did not produce. The self-test is P2: a constructed
/// fragment whose rows and exclusions are known by construction, and three real statements pinned in
/// the continuation forms.
///
/// Seen red 2026-10-02 (<c>tests/Output.Tests/BOOT.md</c>, "## Mutations" 20–22): one edited byte of the
/// generated file fails <c>verify</c>; the extractor made to skip argument 3 of <c>arrayprint</c> fails
/// <c>verify</c> and, once regenerated, the three <c>length</c> rows here; continuation joining turned
/// off fails <c>selftest</c>.
/// </summary>
public class PrintExpressionGeneratorTests
{
    private const string Script = "list-print-expressions.py";

    private static readonly string OutputDirectory = RepositoryPaths.Resolve("src", "Output");

    private sealed record GeneratedRow(int Line, string Role, string Expression);

    // Both read the Fortran source, which lies outside the repository (tools/legacy): Category=Legacy.
    [Fact]
    [Trait("Category", "Legacy")]
    public void ListReproducesByteForByteOnRegeneration() => AssertScriptSucceeds("verify");

    [Fact]
    [Trait("Category", "Legacy")]
    public void RuleSelfTestPasses() => AssertScriptSucceeds("selftest");

    [Fact]
    public void TheThirteenSitesTheDesignNamedBeforeAnyScriptExistedAreRows()
    {
        var rows = Rows();
        var problems = new List<string>();

        for (var line = 1248; line <= 1252; line++)
        {
            RequireRow(rows, problems, line, "item", expression => expression.EndsWith("/(fi+n)", StringComparison.Ordinal), "ending /(fi+n)");
        }

        for (var line = 1253; line <= 1256; line++)
        {
            RequireRow(rows, problems, line, "item", expression => expression.EndsWith("/fi", StringComparison.Ordinal), "ending /fi");
        }

        RequireRow(rows, problems, 1396, "array", expression => expression.Contains("1.00001e6", StringComparison.Ordinal), "containing 1.00001e6");

        foreach (var line in new[] { 1370, 1373, 1377 })
        {
            RequireRow(rows, problems, line, "length", expression => expression.EndsWith("_nmax+2", StringComparison.Ordinal), "ending _nmax+2");
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static void RequireRow(List<GeneratedRow> rows, List<string> problems, int line, string role, Func<string, bool> expression, string description)
    {
        if (!rows.Any(r => r.Line == line && r.Role == role && expression(r.Expression)))
        {
            problems.Add($"no row of role {role} at Fortran {line} {description}");
        }
    }

    private static List<GeneratedRow> Rows() =>
        File.ReadAllLines(Path.Combine(OutputDirectory, "PrintExpressions.generated.txt"))
            .Where(l => !l.StartsWith('#'))
            .Select(l => l.Split(" | "))
            .Select(c => new GeneratedRow(int.Parse(c[0], CultureInfo.InvariantCulture), c[1], c[2]))
            .ToList();

    private static void AssertScriptSucceeds(string mode) =>
        PythonScript.RequireSuccess(Path.Combine("src", "Output", Script), mode);
}
