using System.Text.Json;
using Xunit;

namespace PropStruct.Tests.Harness.Tests;

public class ResultsMFileTests
{
    // The five reference formulations and their replica counts (root BOOT.md, Constraints).
    public static readonly (string Name, int ReplicaCount)[] Formulations =
    [
        ("HPEPA3", 32),
        ("inpt", 16),
        ("P33", 16),
        ("PSAN02n", 16),
        ("HMX", 16),
    ];

    // The three replica kinds, each in its own directory (tests/Fixtures/API.md, "Layout";
    // `tests/Harness/HISTORY.md#criterion-revision-2026-09-17`, step 2).
    private static readonly string[] ReplicaDirectories = ["replicas-lagged", "replicas-independent", "replicas-gsv3"];

    public static IEnumerable<object[]> EveryReferenceAndReplicaFile()
    {
        foreach (var (name, replicaCount) in Formulations)
        {
            yield return [RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt")];
            foreach (var directory in ReplicaDirectories)
            {
                for (var k = 1; k <= replicaCount; k++)
                {
                    yield return [RepositoryPaths.Resolve("tests", "Fixtures", directory, name, k + ".m.txt")];
                }
            }
        }
    }

    public static IEnumerable<object[]> FormulationNames()
    {
        foreach (var (name, _) in Formulations)
        {
            yield return [name];
        }
    }

    [Theory]
    [MemberData(nameof(EveryReferenceAndReplicaFile))]
    public void ParsesEveryReferenceAndReplicaFileWithoutThrowing(string path)
    {
        var quantities = ResultsMFile.Parse(path);
        Assert.NotEmpty(quantities);
        // Every quantity has at least one cell: an empty array would mean a shape matched with nothing captured.
        Assert.All(quantities, kvp => Assert.NotEmpty(kvp.Value));
    }

    // Every value below is transcribed directly from tests/Fixtures/references/HPEPA3/results.m.txt (the
    // fixture holds the number; this test does not invent one, root BOOT.md Taboos).
    [Fact]
    public void HPEPA3ReferenceMatchesTheFileByEye()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "references", "HPEPA3", "results.m.txt");
        var quantities = ResultsMFile.Parse(path);

        // Line 7: Gfr = [0.515E+00 0.485E+00  ];
        Assert.Equal([0.515, 0.485], quantities["Gfr"]);
        // Line 29: Nbase =      100000 +      100000 ; (the two operands of "A + B" become a 2-cell array).
        Assert.Equal([100000.0, 100000.0], quantities["Nbase"]);
        // Line 31: Nkarm =              24649481 ;
        Assert.Equal([24649481.0], quantities["Nkarm"]);
        // Line 27: % Cycles:           1  (distinct from the header echo "Cycles = 1;")
        Assert.Equal([1.0], quantities["CyclesReported"]);
        // Line 41: % Medium number of bridges:   4.363190
        Assert.Equal([4.363190], quantities["MediumNumberOfBridges"]);
        // Line 47: %    3) Dbase < 0.5Dok  :   5.676710
        Assert.Equal([5.676710], quantities["ConditionBreaking(3)"]);
        // Line 50: %    6) Nkarm = 0       :  0.0000000E+00 (the "=" inside the label must not be read as an assignment)
        Assert.Equal([0.0], quantities["ConditionBreaking(6)"]);
        // Line 57: Dok43a =  130.68;
        Assert.Equal([130.68], quantities["Dok43a"]);
        // Lines 59-60: Dok43(1) =  130.75; Dok43(2) =  131.07;
        Assert.Equal([130.75], quantities["Dok43(1)"]);
        Assert.Equal([131.07], quantities["Dok43(2)"]);
        // Lines 355-357: Dkarmcat = [...] (17 pocket-size categories, six values per continuation line but the last).
        Assert.Equal(17, quantities["Dkarmcat"].Length);
        // Lines 368-373: fqdokkarm(  1,:) = [...] (33 Dok-size bins, the same count as fmdok).
        Assert.Equal(33, quantities["fqdokkarm(1,:)"].Length);
        Assert.Equal(quantities["fmdok"].Length, quantities["fqdokkarm(1,:)"].Length);
        // One row per pocket-size category (Dkarm = 10 mkm .. 660 mkm, lines 366-485).
        Assert.Equal(17, quantities.Keys.Count(name => name.StartsWith("fqdokkarm(", StringComparison.Ordinal)));
        // Line 487 is the time line and must never become a quantity.
        Assert.DoesNotContain(quantities.Keys, name => name.Contains("time", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(FormulationNames))]
    public void ReferenceParseRoundTripsThroughJsonWithoutLoss(string formulation)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var original = ResultsMFile.Parse(path);

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<Dictionary<string, double[]>>(json)!;

        Assert.Equal(
            original.Keys.OrderBy(name => name, StringComparer.Ordinal),
            roundTripped.Keys.OrderBy(name => name, StringComparer.Ordinal));
        foreach (var name in original.Keys)
        {
            Assert.Equal(original[name], roundTripped[name]);
        }
    }

    [Fact]
    public void ResolutionFamiliesMatchTheirPrintedTokens()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "references", "HPEPA3", "results.m.txt");
        var cells = ResultsMFile.ParseCells(path);

        // Dok43a =  130.68; -> F, 2 decimal digits -> resolution 0.01.
        AssertClose(0.01, cells["Dok43a"][0].Resolution);
        Assert.False(cells["Dok43a"][0].IsIntegerPrinted);

        // Nkarm =  24649481 ; -> bare integer -> resolution 1, integer-printed.
        AssertClose(1.0, cells["Nkarm"][0].Resolution);
        Assert.True(cells["Nkarm"][0].IsIntegerPrinted);

        // epsx(1)=  3.0934811E-04 ; -> E9.3-style mantissa with 7 decimal digits -> 10^(-4-7).
        AssertClose(Math.Pow(10, -4 - 7), cells["epsx(1)"][0].Resolution);
        Assert.False(cells["epsx(1)"][0].IsIntegerPrinted);

        // pdoksmall's first two cells: 0.198E-37, the printed garbage the exclusion rule targets.
        Assert.True(cells["pdoksmall"][0].Value < 1e-30);
    }

    private static void AssertClose(double expected, double actual) =>
        Assert.True(
            Math.Abs(actual - expected) <= Math.Abs(expected) * 1e-9,
            $"expected {expected:G17}, got {actual:G17}.");
}
