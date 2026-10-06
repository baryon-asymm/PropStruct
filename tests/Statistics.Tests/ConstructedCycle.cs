using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The constructed cycle every `da_coef` control of this node shares (`DaCoefTests`,
/// `LegacyDaCoefReproductionTests`, `PerCyclePlaneOutOfSampleControlsTests`' control (iv)):
/// <c>Setup.Prepare</c> and <c>CycleStatistics.Compute</c> run on cycle-1 totals whose
/// reachable FMDOK cell (<c>Allvdok[0]</c>, the printed "fmdok[0]" of the archived outputs,
/// <c>CycleStatistics.Matrix</c>'s own <c>fmdokIndex = int(Dmin/Di)</c>, which is 1 for every
/// formulation with a default <c>Dmin</c> and <c>Di</c>) is empty, while <c>Allvdok</c>
/// carries an arbitrary non-degenerate share elsewhere, so the normalizing sum it divides by
/// stays well-defined. <c>da_coef</c> (<c>CycleReport.Mp</c>, Fortran 1001-1016) depends on
/// the setup values and on that one cell alone, so the cycle's other totals can stay zero.
/// </summary>
internal static class ConstructedCycle
{
    /// <summary>
    /// <c>CycleReport.Mp</c> of the constructed cycle for <paramref name="formulation"/> under
    /// <paramref name="parameters"/> and <paramref name="precision"/>.
    /// </summary>
    internal static double Mp(Formulation formulation, ModelParameters parameters, PrecisionKind precision)
    {
        var status = Setup.Prepare(formulation, parameters, precision, neighbourBudget: 1000, pocketRedrawBudget: 1000,
            out var setup, out var tables, out _, out var pending, out var inputs);
        Assert.Equal(SetupStatus.Ok, status);

        var echoes = Setup.CompleteEchoes(ref setup, pending, pending.Ddokmax);

        var layout = setup.Layout;
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];
        for (var k = 1; k < setup.Ndok; k++)
        {
            realTotals[layout.Allvdok + k] = 1.0;
        }

        var report = CycleStatistics.Compute(
            setup, tables, echoes, inputs,
            cycleIndex: 1, integerTotals, realTotals, new double[setup.Ndok],
            out _, out _);

        return report.Mp;
    }

    /// <summary>
    /// <c>CycleReport.Mp</c> under <see cref="ModelParameters.Default"/> for a shipped <c>.dat</c>,
    /// read with its pocket-forming flags (menu [12]) when <paramref name="readPocketFormingFractions"/>.
    /// </summary>
    internal static double Mp(string datPath, bool readPocketFormingFractions, PrecisionKind precision)
    {
        var formulation = DatFile.Read(datPath, readPocketFormingFractions);
        var parameters = ModelParameters.Default with { ReadPocketFormingFractions = readPocketFormingFractions };
        return Mp(formulation, parameters, precision);
    }
}
