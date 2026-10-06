using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// Closes the print-length escalation of <c>src/Statistics/BOOT.md</c>, "## Report"
/// (AGENTS.md §11, resolved 2026-09-21 in a root session spanning this node and
/// <c>Statistics</c>): whether <c>ResultsMWriter</c>'s <c>fmkarm</c>/<c>fqkarm</c>/
/// <c>fmkarm_cor</c>/<c>fmkarm_cor2</c>/<c>fqkarm_cor</c> print lengths
/// (<c>int(DPmax/Di) + 2</c>, <c>int(DPmax_cor/Di) + 2</c>, Fortran lines 1345, 1350,
/// 1353, 1357, 1361, 1364) are computed correctly.
///
/// The finding: the formula needs no fix. It is exactly <c>(int)(pockets.DpMax /
/// cellSize) + 2</c> and <c>(int)(pockets.DpMaxCor / cellSize) + 2</c> in plain
/// <c>double</c> (<see cref="ResultsMWriter"/>'s own <c>dpLength</c>/<c>dpCorLength</c>),
/// and it matches the archived reference on nine of the ten lengths it drives across
/// the five reference formulations. The tenth (HMX's <c>DpMaxCor</c>-driven trio) is
/// six cells short — <c>DpMaxCor</c>'s own accumulated value diverging from the
/// original, not this formula: a binary32-rounding defect could move a truncated
/// quotient by at most one unit at an integer boundary, and no rounding of any kind
/// can move it by six. This is measured here, not asserted from a typed literal: every
/// expected length is read from the archived <c>results.m</c> through
/// <see cref="ResultsMFile"/>, and the HMX gap is computed as a difference of two
/// parsed lengths, never hand-typed.
/// </summary>
public class PocketPrintLengthTests
{
    private static readonly string[] AllReferenceFormulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];
    private static readonly string[] FormulationsExcludingHmx = ["HPEPA3", "inpt", "P33", "PSAN02n"];

    public static IEnumerable<object[]> ReferenceFormulations() =>
        AllReferenceFormulations.Select(n => new object[] { n });

    public static IEnumerable<object[]> FormulationsWhereTheCorrectedFamilyMatchesTheArchive() =>
        FormulationsExcludingHmx.Select(n => new object[] { n });

    private static (SimulationResult Result, double CellSize,
        IReadOnlyDictionary<string, double[]> Archive, IReadOnlyDictionary<string, double[]> Port)
        RunReferenceMode(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat");
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Seed = 0UL,
        });

        var result = simulator.Run(formulation);
        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, parameters, result, tempPath);
            var portCells = ResultsMFile.Parse(tempPath);
            var archiveCells = ResultsMFile.Parse(referencePath);
            return (result, parameters.CellSize.Metres, archiveCells, portCells);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// <c>fmkarm</c>/<c>fqkarm</c> (the <c>DpMax</c>-driven pair, no correction gate)
    /// print at exactly <c>int(DpMax/Di) + 2</c> and match the archive on every
    /// reference formulation, HMX included: the gap the escalation raised is confined
    /// to the corrected trio below.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    [Trait("Category", "Long")]
    public void FmkarmAndFqkarmLengthEqualsIntDpMaxOverCellSizePlusTwoAndMatchesTheArchive(string name)
    {
        var (result, cellSize, archive, port) = RunReferenceMode(name);

        var expectedLength = (int)(result.Pockets.DpMax / cellSize) + 2;

        Assert.Equal(expectedLength, port["fmkarm"].Length);
        Assert.Equal(expectedLength, port["fqkarm"].Length);
        Assert.Equal(archive["fmkarm"].Length, port["fmkarm"].Length);
        Assert.Equal(archive["fqkarm"].Length, port["fqkarm"].Length);
    }

    /// <summary>
    /// <c>fmkarm_cor</c>/<c>fmkarm_cor2</c>/<c>fqkarm_cor</c> (the <c>DpMaxCor</c>-driven
    /// trio, gated by the Var#1/Var#2 pocket-in-pocket checks) print at exactly
    /// <c>int(DpMaxCor/Di) + 2</c> and match the archive on four of the five reference
    /// formulations. HMX is excluded here and covered on its own below.
    /// </summary>
    [Theory]
    [MemberData(nameof(FormulationsWhereTheCorrectedFamilyMatchesTheArchive))]
    [Trait("Category", "Long")]
    public void CorrectedFamilyLengthEqualsIntDpMaxCorOverCellSizePlusTwoAndMatchesTheArchive(string name)
    {
        var (result, cellSize, archive, port) = RunReferenceMode(name);

        var expectedLength = (int)(result.Pockets.DpMaxCor / cellSize) + 2;

        Assert.Equal(expectedLength, port["fmkarm_cor"].Length);
        Assert.Equal(expectedLength, port["fmkarm_cor2"].Length);
        Assert.Equal(expectedLength, port["fqkarm_cor"].Length);
        Assert.Equal(archive["fmkarm_cor"].Length, port["fmkarm_cor"].Length);
        Assert.Equal(archive["fmkarm_cor2"].Length, port["fmkarm_cor2"].Length);
        Assert.Equal(archive["fqkarm_cor"].Length, port["fqkarm_cor"].Length);
    }

    /// <summary>
    /// HMX's own corrected trio: the formula is still exactly <c>int(DpMaxCor/Di) + 2</c>
    /// (the port prints exactly what its own <c>DpMaxCor</c> and the Fortran formula
    /// give), but the length disagrees with the archive by more than a rounding-boundary
    /// flip could ever produce (at most one cell) — the evidence, from the archive
    /// itself, that this is a value divergence and not a formula defect
    /// (<c>src/Statistics/BOOT.md</c>, "## Report", the resolved escalation, and "##
    /// Defects of the original", Fortran 671-703).
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void CorrectedFamilyLengthOnHmxMatchesItsOwnFormulaButDivergesFromTheArchiveByMoreThanARoundingFlip()
    {
        var (result, cellSize, archive, port) = RunReferenceMode("HMX");

        var expectedLength = (int)(result.Pockets.DpMaxCor / cellSize) + 2;

        Assert.Equal(expectedLength, port["fmkarm_cor"].Length);
        Assert.Equal(expectedLength, port["fmkarm_cor2"].Length);
        Assert.Equal(expectedLength, port["fqkarm_cor"].Length);

        var gap = archive["fmkarm_cor"].Length - port["fmkarm_cor"].Length;
        Assert.True(
            Math.Abs(gap) > 1,
            $"a rounding-boundary cause (binary32 vs. double) could move the length by at most one cell; measured gap is {gap}");
        Assert.Equal(archive["fmkarm_cor"].Length, archive["fmkarm_cor2"].Length);
        Assert.Equal(archive["fmkarm_cor"].Length, archive["fqkarm_cor"].Length);
    }
}
