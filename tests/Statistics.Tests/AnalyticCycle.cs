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
/// same mass above <c>Dmin</c> (inside the cell that holds <c>Dmin</c>, from <c>Dmin</c> itself, since Fortran 533 rejects a base
/// particle with <c>Db &lt;= Dmin</c> before 542 accumulates it), the oxidizer drawn as coarse. Every other total is zero, as
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

    /// <summary>
    /// Prepares <paramref name="formulation"/> under <paramref name="parameters"/>, <paramref name="precision"/> and
    /// <paramref name="corrections"/>; with <paramref name="imposed"/> the stored set is replaced by it afterwards, as in a
    /// setup built by hand.
    /// </summary>
    public static AnalyticCycle Prepare(
        Formulation formulation, ModelParameters parameters, PrecisionKind precision, CorrectionSet corrections,
        CorrectionSet? imposed = null)
    {
        var status = Statistics.Setup.Prepare(formulation, parameters, precision, corrections, 1000, 1000,
            out var setup, out var tables, out _, out var pending, out var inputs);
        Assert.Equal(SetupStatus.Ok, status);
        if (imposed is { } stored)
        {
            setup.Corrections = stored;
        }

        var echoes = Statistics.Setup.CompleteEchoes(ref setup, pending, pending.Ddokmax);
        return new AnalyticCycle(formulation, setup, tables, echoes, inputs);
    }

    /// <summary>The layout of the totals.</summary>
    public AccumulatorLayout Layout => setup.Layout;

    /// <summary>The number of oxidizer cells, <c>Ndok</c>.</summary>
    public int Ndok => setup.Ndok;

    /// <summary>The category-row step, <c>Dj</c>.</summary>
    public double CategoryStep => setup.CategoryStep;

    /// <summary>The oxidizer density, <c>PLOT1</c>.</summary>
    public double OxidizerDensity => inputs.OxidizerDensity;

    /// <summary>The propellant density, <c>PLOT2</c>.</summary>
    public double PropellantDensity => inputs.PropellantDensity;

    /// <summary>The oxidizer cell size, <c>Di</c>.</summary>
    public double CellSize => setup.CellSize;

    /// <summary>The smallest oxidizer particle drawn as coarse, <c>Dmin</c>.</summary>
    public double Dmin => setup.Dmin;

    /// <summary>The formulation's own <c>AK2</c>, the ratio of the cell size to the small-particle step.</summary>
    public double Ak2 => setup.Ak2;

    /// <summary>The pocket coefficient the small-particle probability multiplies by, <c>karmcoef</c>.</summary>
    public double PocketCoefficient => setup.PocketCoefficient;

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
            var cellLower = k * setup.CellSize;
            var cellUpper = (k + 1) * setup.CellSize;
            totals[layout.Allvdok + k] = MassBetween(cellLower, cellUpper);
            totals[layout.Vdokstr + k] = k >= firstCoarse ? MassBetween(Math.Max(cellLower, setup.Dmin), cellUpper) : 0.0;
        }

        return totals;
    }

    /// <summary>One <c>CycleStatistics.Compute</c> on the given totals; <paramref name="nextPdoksmall"/> is its model input for the next cycle.</summary>
    public CycleReport Compute(int cycleIndex, long[] integerTotals, double[] realTotals, out double[] nextPdoksmall)
    {
        nextPdoksmall = new double[setup.Ndok];
        return CycleStatistics.Compute(setup, tables, echoes, inputs, cycleIndex, integerTotals, realTotals, nextPdoksmall, out _, out _);
    }

    /// <summary>The mass the fractions place between <paramref name="lower"/> and <paramref name="upper"/>, 0 when the interval is empty.</summary>
    public double MassBetween(double lower, double upper)
    {
        if (upper <= lower)
        {
            return 0.0;
        }

        var mass = 0.0;
        for (var j = 0; j < formulation.Fractions.Length; j++)
        {
            var fraction = formulation.Fractions[j];
            mass += tables.MassShare[j] * (Cdf(fraction, upper) - Cdf(fraction, lower));
        }

        return mass;
    }

    /// <summary>
    /// <c>Integral D dm</c> over <paramref name="lower"/> to <paramref name="upper"/>, by the midpoint rule on
    /// <paramref name="steps"/> equal sub-intervals of the size law's own mass CDF (not from a closed form of the moment).
    /// </summary>
    public double FirstMomentBetween(double lower, double upper, int steps)
    {
        var moment = 0.0;
        var width = (upper - lower) / steps;
        for (var i = 0; i < steps; i++)
        {
            var low = lower + i * width;
            moment += (low + 0.5 * width) * MassBetween(low, low + width);
        }

        return moment;
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
