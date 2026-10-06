using PropStruct.Input;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>CycleReport.Pdoksmall</c> (the report field <c>Output</c> prints as
/// <c>pdoksmall</c>) is <c>Ndok - 1</c> elements long, not <c>Ndok</c> (BOOT.md, "##
/// Report", "Decisions where the port departs from a transcription"; the defect row of
/// "## Defects of the original", Fortran line 1386:
/// <c>call arrayprint('pdoksmall',pdoksmall,Ndok-1)</c>). Every other <c>Ndok</c>-sized
/// distribution the original prints (<c>fmdok</c>, the printed <c>Allvdokso</c>) uses
/// the full <c>Ndok</c>, so this is not a general array-size mismatch: it is specific
/// to <c>pdoksmall</c>'s own print call, one array short of its own dimension.
///
/// This is deliberately checked against the archived reference `results.m` of each
/// reference formulation, not a typed literal (root BOOT.md, Taboos: "No expected
/// value typed into a test when it exists in a fixture file"): before the fix,
/// `CycleReport.Pdoksmall` carried the full internal array (`Ndok` elements, one more
/// than every reference formulation's own archived `pdoksmall`); after the fix, it is
/// trimmed to the printed length.
/// </summary>
public class PdoksmallReportLengthTests
{
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    public static IEnumerable<object[]> ReferenceFormulations()
    {
        yield return new object[] { "HPEPA3" };
        yield return new object[] { "inpt" };
        yield return new object[] { "P33" };
        yield return new object[] { "PSAN02n" };
        yield return new object[] { "HMX" };
    }

    /// <summary>
    /// <c>CycleStatistics.Compute</c>'s own <c>Pdoksmall</c> field, on a cycle-0,
    /// all-zero-totals scenario built from each reference formulation's real
    /// <c>ModelSetup</c> (so <c>Ndok</c> is the real, binary32-derived value
    /// <c>SetupTests</c> already exercises), equals in length the archived reference
    /// `results.m`'s own printed `pdoksmall` array — not <c>Ndok</c>, which is one
    /// longer on every one of the five reference formulations
    /// (`SetupTests.SizesNdokMatchesThePrintedCategoryRowLengthOfItsArchivedFormulation`'s
    /// own archived-formulation evidence covers `Ndok` itself; this test covers the
    /// separate, print-call-specific "-1").
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void ReportPdoksmallLengthMatchesTheArchivedReferenceFormulationsOwnPrintedArray(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var status = Setup.Prepare(formulation, parameters, Particle.PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out _, out var pending, out var inputs);
        Assert.Equal(SetupStatus.Ok, status);
        var echoes = Setup.CompleteEchoes(ref setup, pending, pending.Ddokmax);

        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        var report = CycleStatistics.Compute(
            in setup, tables, echoes, inputs, cycleIndex: 0,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        var resultsPath = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");
        var cells = ResultsMFile.Parse(resultsPath);
        var expectedLength = cells["pdoksmall"].Length;

        Assert.Equal(expectedLength, report.Pdoksmall.Length);
        Assert.Equal(setup.Ndok - 1, report.Pdoksmall.Length);
    }

    /// <summary>
    /// The trim keeps every retained cell's value bit for bit: it drops the array's
    /// last element only, it does not recompute anything (BOOT.md, "## Report": the
    /// fix "does not move the values of the remaining cells").
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void ReportPdoksmallRetainsEveryOtherCellBitForBit(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var status = Setup.Prepare(formulation, parameters, Particle.PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out _, out var pending, out var inputs);
        Assert.Equal(SetupStatus.Ok, status);
        var echoes = Setup.CompleteEchoes(ref setup, pending, pending.Ddokmax);

        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        var nextPdoksmall = new double[setup.Ndok];
        var report = CycleStatistics.Compute(
            in setup, tables, echoes, inputs, cycleIndex: 0,
            integerTotals, realTotals, nextPdoksmall, out _, out _);

        Assert.Equal(setup.Ndok, nextPdoksmall.Length); // The model-feedback array keeps every element.
        Assert.Equal(setup.Ndok - 1, report.Pdoksmall.Length); // The report drops the last one.
        for (var k = 0; k < report.Pdoksmall.Length; k++)
        {
            Assert.Equal(nextPdoksmall[k], report.Pdoksmall[k]);
        }
    }
}
