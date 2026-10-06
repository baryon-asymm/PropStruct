using System.Globalization;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Pins control P1 of the per-cycle plane under <see cref="PrecisionKind.Original"/>
/// (`src/Statistics/BOOT.md`, "## Report", "The per-cycle plane under `Original`";
/// "## Defects of the original", row 771-1175). <c>da_coef</c> (<c>CycleReport.Mp</c>,
/// Fortran 1001-1016, <c>CycleStatistics.Matrix</c>) was, until 2026-10-01, the one printed
/// cell the setup plane's reproduction did not reach: PSAN02n printed one unit below the
/// original while <c>Matrix</c> stayed <c>double</c>. With the plane's sites rounded (the
/// home of <c>mp</c> at 1016 above all) it prints the original's value on all five. Two
/// arms, root BOOT.md's own taboo ("no expected value typed into a test when it exists in
/// a fixture file"):
///
/// (i) the original's own <c>da_coef</c> is one value per formulation, over the whole
/// machine-generated set of fixture outputs (the reference, and every lagged,
/// independent and GSV=3 replica) -- 49 outputs on PSAN02n and HMX, fewer on the
/// formulations with <c>R = 16</c>, all counted by walking the fixture directories, not
/// typed;
///
/// (ii) <c>CycleReport.Mp</c>, computed from <c>Setup.Prepare</c> and
/// <c>CycleStatistics.Compute</c> on a constructed cycle-1 scenario whose reachable
/// FMDOK cell (<c>Allvdok[0]</c>, "## Report", "the printed fmdok[0]") is empty while
/// <c>Allvdok</c> is non-zero elsewhere -- the shape every reference formulation's own
/// archived "fmdok" already has at cell 0 -- rounded to the fixture token's own print
/// resolution, equals that one value under <see cref="PrecisionKind.Binary64"/> on all
/// five reference formulations, and under <see cref="PrecisionKind.Original"/> on all five.
/// Seen red 2026-10-01 only with both of PSAN02n's routes removed: the <c>Memory</c> call at
/// Fortran 1016 and the folds and literals of 1014-1016 together give 0.79177144596 (prints
/// 0.7917714, the value of before); either one alone still prints 0.7917715, so the design's
/// predicted red, 1016 alone, was not a red.
/// </summary>
public class DaCoefTests
{
    public static IEnumerable<object[]> ReferenceFormulations()
    {
        yield return new object[] { "HPEPA3" };
        yield return new object[] { "inpt" };
        yield return new object[] { "P33" };
        yield return new object[] { "PSAN02n" };
        yield return new object[] { "HMX" };
    }

    private static readonly string[] ReplicaDirectories = ["replicas-lagged", "replicas-independent", "replicas-gsv3"];

    /// <summary>
    /// Every archived output of one formulation: the reference plus every replica of
    /// the three replica kinds, found by walking the fixture directories rather than a
    /// hand-typed count (AGENTS.md §6: a criterion quantified by "all" is checked
    /// against a list the machine generates).
    /// </summary>
    internal static IReadOnlyList<string> EnumerateFixtureOutputs(string formulation)
    {
        var paths = new List<string> { RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt") };
        foreach (var kind in ReplicaDirectories)
        {
            var directory = RepositoryPaths.Resolve("tests", "Fixtures", kind, formulation);
            if (Directory.Exists(directory))
            {
                paths.AddRange(Directory.EnumerateFiles(directory, "*.m.txt").OrderBy(p => p, StringComparer.Ordinal));
            }
        }

        return paths;
    }

    private static double[] ReadDaCoef(IReadOnlyList<string> paths)
    {
        var values = new double[paths.Count];
        for (var i = 0; i < paths.Count; i++)
        {
            values[i] = ResultsMFile.ParseCells(paths[i])["da_coef"][0].Value;
        }

        return values;
    }

    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void DaCoefIsOneValueAcrossEveryArchivedOutput(string formulation)
    {
        var paths = EnumerateFixtureOutputs(formulation);
        Assert.True(paths.Count > 1, $"{formulation}: expected more than one archived output, found {paths.Count}.");

        var distinct = ReadDaCoef(paths).Distinct().ToArray();
        Assert.True(distinct.Length == 1,
            $"{formulation}: da_coef takes {distinct.Length} distinct values across {paths.Count} archived outputs ({string.Join(", ", distinct)}).");
    }

    /// <summary>
    /// Non-degeneracy (AGENTS.md §13): a scratch copy of one replica with its
    /// <c>da_coef</c> line edited by one print unit turns
    /// <see cref="DaCoefIsOneValueAcrossEveryArchivedOutput"/>'s own check red, on the
    /// same list-and-parse path that check uses, not a second implementation of it.
    /// </summary>
    [Fact]
    public void DaCoefIsOneValueAcrossEveryArchivedOutputIsSensitiveToADivergingOutput()
    {
        const string formulation = "PSAN02n";
        var paths = EnumerateFixtureOutputs(formulation).ToList();
        Assert.True(paths.Count > 1);

        var scratchDirectory = Directory.CreateTempSubdirectory("DaCoefTests-");
        try
        {
            var sourcePath = paths[^1];
            var reference = ResultsMFile.ParseCells(sourcePath)["da_coef"][0];
            var digits = PrintDigits(reference);
            var originalToken = reference.Value.ToString($"F{digits}", CultureInfo.InvariantCulture);
            var mutatedToken = (reference.Value + reference.Resolution).ToString($"F{digits}", CultureInfo.InvariantCulture);

            var lines = File.ReadAllLines(sourcePath);
            var daCoefLine = Array.FindIndex(lines, l => l.TrimStart().StartsWith("da_coef", StringComparison.Ordinal));
            Assert.True(daCoefLine >= 0, $"{sourcePath}: no da_coef line found to mutate.");

            var mutatedLine = lines[daCoefLine].Replace(originalToken, mutatedToken, StringComparison.Ordinal);
            Assert.NotEqual(lines[daCoefLine], mutatedLine); // sanity: the replace actually changed the line.
            lines[daCoefLine] = mutatedLine;

            var mutatedPath = Path.Combine(scratchDirectory.FullName, "mutated.m.txt");
            File.WriteAllLines(mutatedPath, lines);

            paths[^1] = mutatedPath;
            var distinct = ReadDaCoef(paths).Distinct().ToArray();
            Assert.True(distinct.Length > 1, "the mutated output should have made da_coef diverge, but it did not.");
        }
        finally
        {
            scratchDirectory.Delete(recursive: true);
        }
    }

    private static double ComputeMp(string formulation, PrecisionKind precision) =>
        ConstructedCycle.Mp(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{formulation}.dat"),
            readPocketFormingFractions: false, precision);

    private static ResultCell ArchivedDaCoef(string formulation)
    {
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        return ResultsMFile.ParseCells(referencePath)["da_coef"][0];
    }

    private static int PrintDigits(ResultCell cell) => (int)Math.Round(-Math.Log10(cell.Resolution));

    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void MpMatchesTheOriginalUnderBinary64(string formulation)
    {
        var archived = ArchivedDaCoef(formulation);
        var mp = ComputeMp(formulation, PrecisionKind.Binary64);
        Assert.Equal(archived.Value, mp, PrintDigits(archived));
    }

    /// <summary>
    /// Control P1: under <see cref="PrecisionKind.Original"/> every reference formulation's
    /// <c>da_coef</c> prints the original's value, PSAN02n's included (until 2026-10-01 one
    /// unit below, the difference row 771-1175 declared).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void MpMatchesTheOriginalUnderOriginal(string formulation)
    {
        var archived = ArchivedDaCoef(formulation);
        var mp = ComputeMp(formulation, PrecisionKind.Original);
        Assert.Equal(archived.Value, mp, PrintDigits(archived));
    }
}
