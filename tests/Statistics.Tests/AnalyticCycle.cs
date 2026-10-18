using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// A cycle whose oxidizer totals are the formulation's own size distribution, in closed form, so that the quantities
/// that read them (<c>FMDOK</c>, <c>gdokleft</c>, <c>pdoksmall</c>) are those of a run that drew every particle with
/// its exact probability and no scatter: <c>Allvdok</c> in cell <c>k</c> is the mass the fractions place between the
/// cell's edges, <c>Sum GDOK (phi(hi) - phi(lo))</c> over the fractions, with <c>phi</c> the mass CDF of the fraction's
/// law (<c>(x^4 - l^4)/(u^4 - l^4)</c> for <c>JZZ = 2</c>, <c>(x - l)/(u - l)</c> otherwise), and <c>Vdokstr</c> is the
/// same mass in the cells from <c>b = int(Dmin/Di) Di</c> up, the oxidizer drawn as coarse. Every other total is zero, as
/// in <see cref="ConstructedCycle"/>. Used for the evidence cases of <c>ZdoksmallCoarseShare</c>,
/// <c>PdoksmallFirstCell</c> and <c>VdokTotalOnCopy</c>, whose figures are reported and not asserted to the digit.
/// </summary>
internal sealed class AnalyticCycle
{
    private readonly Formulation formulation;
    private readonly ModelSetup setup;
    private readonly SetupTables tables;
    private readonly SetupEchoes echoes;
    private readonly SetupInputs inputs;

    private AnalyticCycle(
        Formulation formulation, ModelSetup setup, SetupTables tables, SetupEchoes echoes, SetupInputs inputs)
    {
        this.formulation = formulation;
        this.setup = setup;
        this.tables = tables;
        this.echoes = echoes;
        this.inputs = inputs;
    }

    /// <summary>Prepares <paramref name="formulation"/> under <paramref name="parameters"/>, <paramref name="precision"/> and <paramref name="corrections"/>.</summary>
    public static AnalyticCycle Prepare(
        Formulation formulation, ModelParameters parameters, PrecisionKind precision, CorrectionSet corrections)
    {
        var status = Statistics.Setup.Prepare(formulation, parameters, precision, corrections, 1000, 1000,
            out var setup, out var tables, out _, out var pending, out var inputs);
        Assert.Equal(SetupStatus.Ok, status);
        var echoes = Statistics.Setup.CompleteEchoes(ref setup, pending, pending.Ddokmax);
        return new AnalyticCycle(formulation, setup, tables, echoes, inputs);
    }

    /// <summary>Fresh integer totals, all zero.</summary>
    public long[] IntegerTotals() => new long[setup.Layout.IntegerLength];

    /// <summary>Fresh real totals: <c>Allvdok</c> and <c>Vdokstr</c> as in the type's summary, the rest zero.</summary>
    public double[] RealTotals()
    {
        var layout = setup.Layout;
        var totals = new double[layout.RecordLength];
        var firstCoarse = (int)(setup.Dmin / setup.CellSize);
        for (var k = 0; k < setup.Ndok; k++)
        {
            var mass = MassInCell(k);
            totals[layout.Allvdok + k] = mass;
            totals[layout.Vdokstr + k] = k >= firstCoarse ? mass : 0.0;
        }

        return totals;
    }

    /// <summary>One <c>CycleStatistics.Compute</c> on the given totals; <paramref name="nextPdoksmall"/> is its model input for the next cycle.</summary>
    public CycleReport Compute(int cycleIndex, long[] integerTotals, double[] realTotals, out double[] nextPdoksmall)
    {
        nextPdoksmall = new double[setup.Ndok];
        return CycleStatistics.Compute(setup, tables, echoes, inputs, cycleIndex, integerTotals, realTotals, nextPdoksmall, out _, out _);
    }

    private double MassInCell(int k)
    {
        var lower = k * setup.CellSize;
        var upper = (k + 1) * setup.CellSize;
        var mass = 0.0;
        for (var j = 0; j < formulation.Fractions.Length; j++)
        {
            var fraction = formulation.Fractions[j];
            mass += tables.MassShare[j] * (Cdf(fraction, upper) - Cdf(fraction, lower));
        }

        return mass;
    }

    private double Cdf(OxidizerFraction fraction, double size)
    {
        var lower = fraction.LowerBound.Metres;
        var upper = fraction.UpperBound.Metres;
        if (size <= lower)
        {
            return 0.0;
        }

        if (size >= upper)
        {
            return 1.0;
        }

        return setup.SizeLaw == 2
            ? (Math.Pow(size, 4) - Math.Pow(lower, 4)) / (Math.Pow(upper, 4) - Math.Pow(lower, 4))
            : (size - lower) / (upper - lower);
    }
}
