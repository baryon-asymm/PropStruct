using PropStruct.Input;
using PropStruct.Particle;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>VdokTotalOnCopy</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 1145, 1150): every cycle
/// from the first divides the stored <c>Vdok_total</c> cells and <c>Vdok_total2</c> by <c>1 - gdokleft/GGG</c>, so each
/// later cycle divides the earlier cycles' particles again. Corrected, the quotients feed 1146's sum and 1151 and are
/// stored nowhere. The right side is the closed form of <c>DolM2</c> and <c>DolM3</c> over the undivided totals and the
/// totals themselves, byte for byte, after two calls; the red side is <c>None</c>, whose second call divides again.
/// The totals are those of <see cref="AnalyticCycle"/> for P312, whose <c>gdokleft</c> is 0.122, plus the four of
/// lines 1137-1151, set to different positive numbers per cell.
/// </summary>
public class VdokTotalOnCopyTests(ITestOutputHelper output)
{
    private const double RelativeTolerance = 1e-12;

    /// <summary>
    /// Right: over two cycles the flag leaves <c>VdokTotal</c> and <c>VdokTotal2</c> byte-identical, and the report of each
    /// cycle is the closed form over the undivided totals, <c>DolM2</c> as <c>SVm (1 - vdokleft) GGG PLOTsm / ((1 - GGG)
    /// (SVd/d) PLOT1)</c> and <c>DolM3</c> likewise from the scalar, <c>d = 1 - gdokleft/GGG</c>.
    /// </summary>
    [Fact]
    public void UnderTheFlagTheTotalsAreUntouchedAndTheMassFractionsAreTheClosedForm()
    {
        var run = Cycles(CorrectionSet.VdokTotalOnCopy, 2);

        Assert.Equal(run.Initial.Select(BitConverter.DoubleToInt64Bits), run.Final.Select(BitConverter.DoubleToInt64Bits));
        foreach (var report in run.Reports)
        {
            AssertClosedForms(run, report);
        }
    }

    /// <summary>
    /// Red: under <c>None</c> the second call divides the totals again, so they differ from the initial ones by the
    /// divisor squared, and the report of cycle 2 is not the closed form over the undivided totals.
    /// </summary>
    [Fact]
    public void WithoutTheFlagTheTotalsAreDividedTwiceAndCycleTwoIsNotTheClosedForm()
    {
        var run = Cycles(CorrectionSet.None, 2);
        var divisor = 1.0 - run.Reports[0].Gdokleft / run.Reports[0].Ggg;

        for (var i = 0; i < run.Initial.Length; i++)
        {
            if (run.IsVdokTotal(i))
            {
                AssertRelative(run.Initial[i] / (divisor * divisor), run.Final[i]);
            }
            else
            {
                Assert.Equal(run.Initial[i], run.Final[i]);
            }
        }

        AssertClosedForms(run, run.Reports[0]);
        Assert.True(Math.Abs(run.Reports[1].DolM2 - ClosedDolM2(run, run.Reports[1])) > 1e-3 * Math.Abs(run.Reports[1].DolM2));
        Assert.True(Math.Abs(run.Reports[1].DolM3 - ClosedDolM3(run, run.Reports[1])) > 1e-3 * Math.Abs(run.Reports[1].DolM3));
    }

    /// <summary>Cycle 1 is bit-identical to <c>None</c>: the first division is the same number whether it is stored or not.</summary>
    [Fact]
    public void CycleOneIsBitIdenticalToNone()
    {
        var none = Cycles(CorrectionSet.None, 1).Reports[0];
        var flagged = Cycles(CorrectionSet.VdokTotalOnCopy, 1).Reports[0];

        Assert.Equal(BitConverter.DoubleToInt64Bits(none.DolM2), BitConverter.DoubleToInt64Bits(flagged.DolM2));
        Assert.Equal(BitConverter.DoubleToInt64Bits(none.DolM3), BitConverter.DoubleToInt64Bits(flagged.DolM3));
    }

    /// <summary>With <c>gdokleft = 0</c> the divisor is 1 and the flag changes no bit of the report or the totals, in every cycle.</summary>
    [Fact]
    public void WithoutLeftOxidizerTheFlagChangesNoBit()
    {
        var parameters = ModelParameters.Default with { Dmin = Length.FromMicrometres(5) };
        var none = Cycles(CorrectionSet.None, 3, parameters);
        var flagged = Cycles(CorrectionSet.VdokTotalOnCopy, 3, parameters);

        Assert.Equal(0.0, none.Reports[0].Gdokleft);
        Assert.Equal(none.Final.Select(BitConverter.DoubleToInt64Bits), flagged.Final.Select(BitConverter.DoubleToInt64Bits));
        for (var cycle = 0; cycle < 3; cycle++)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(none.Reports[cycle].DolM2), BitConverter.DoubleToInt64Bits(flagged.Reports[cycle].DolM2));
            Assert.Equal(BitConverter.DoubleToInt64Bits(none.Reports[cycle].DolM3), BitConverter.DoubleToInt64Bits(flagged.Reports[cycle].DolM3));
        }
    }

    /// <summary>
    /// Reported, P312 over three cycles on constant totals: <c>None</c>'s <c>DolM2</c> and <c>DolM3</c> are the flag's times
    /// <c>d^(c - 1)</c> in cycle <c>c</c> (they divide <c>c</c> times where one division is right), so they move from cycle 2
    /// by the figures printed here; the ratio is asserted to the closed form, the printed figures follow from it.
    /// </summary>
    [Fact]
    public void P312OverThreeCyclesMovesDolM2AndDolM3FromCycleTwoByThePowerOfTheDivisor()
    {
        var none = Cycles(CorrectionSet.None, 3);
        var flagged = Cycles(CorrectionSet.VdokTotalOnCopy, 3);
        var divisor = 1.0 - flagged.Reports[0].Gdokleft / flagged.Reports[0].Ggg;

        for (var cycle = 0; cycle < 3; cycle++)
        {
            var expected = Math.Pow(divisor, cycle);
            AssertRelative(expected, none.Reports[cycle].DolM2 / flagged.Reports[cycle].DolM2);
            AssertRelative(expected, none.Reports[cycle].DolM3 / flagged.Reports[cycle].DolM3);
            output.WriteLine($"P312 cycle {cycle + 1}: None/flag DolM2 {none.Reports[cycle].DolM2 / flagged.Reports[cycle].DolM2:R}, DolM3 {none.Reports[cycle].DolM3 / flagged.Reports[cycle].DolM3:R}");
        }
    }

    /// <summary>
    /// Right, an identity that restates none of 1146/1151 from the report: where the totals of the first variant are the
    /// all-bridge totals (<c>VmkmTotal = Vsmkm</c> and <c>VdokTotal = Svd</c> cell by cell, <c>VmkmTotal2</c> and
    /// <c>VdokTotal2</c> their sums), 1137's <c>DolM1</c>, which divides nothing, is what 1146 and 1151 give once each
    /// total is divided exactly once. Constant totals, P312 (<c>gdokleft</c> 0.122 &gt; 0), three cycles: under the flag
    /// <c>DolM2</c> and <c>DolM3</c> equal <c>DolM1</c> in every cycle. The three sides are the same product evaluated in
    /// different orders over sums of <c>Ndok</c> terms, so they agree to a few units of the last place; the tolerance is
    /// <c>1e-12</c>, three orders above that and as tight as the other closed forms of this class.
    /// </summary>
    [Fact]
    public void UnderTheFlagTheOnceDividedMassFractionsEqualDolM1WhereTheTotalsAreTheAllBridgeTotals()
    {
        var run = Cycles(CorrectionSet.VdokTotalOnCopy, 3, allBridge: true);

        Assert.True(run.Reports[0].Gdokleft > 0.0);
        foreach (var report in run.Reports)
        {
            Assert.True(report.DolM1 > 0.0);
            AssertRelative(report.DolM1, report.DolM2);
            AssertRelative(report.DolM1, report.DolM3);
        }
    }

    /// <summary>
    /// Red: on the same totals, without the flag the identity holds in cycle 1, where the division is the first and
    /// only, and breaks from cycle 2, where the stored quotients are divided again: <c>DolM2</c> and <c>DolM3</c> have
    /// moved from <c>DolM1</c> by far more than the tolerance of the right side.
    /// </summary>
    [Fact]
    public void WithoutTheFlagTheIdentityHoldsInCycleOneAndBreaksFromCycleTwo()
    {
        var run = Cycles(CorrectionSet.None, 3, allBridge: true);

        AssertRelative(run.Reports[0].DolM1, run.Reports[0].DolM2);
        AssertRelative(run.Reports[0].DolM1, run.Reports[0].DolM3);
        foreach (var report in run.Reports.Skip(1))
        {
            Assert.True(Math.Abs(report.DolM2 - report.DolM1) > 1e-3 * Math.Abs(report.DolM1));
            Assert.True(Math.Abs(report.DolM3 - report.DolM1) > 1e-3 * Math.Abs(report.DolM1));
        }
    }

    private static double ClosedDolM2(Run run, CycleReport report) =>
        run.VmkmTotalSum * (1.0 - report.Vdokleft) * report.Ggg * report.Plotsm
        / ((1.0 - report.Ggg) * (run.VdokTotalSum / Divisor(report)) * run.OxidizerDensity);

    private static double ClosedDolM3(Run run, CycleReport report) =>
        run.VmkmTotal2 * (1.0 - report.Vdokleft) * report.Ggg * report.Plotsm
        / ((1.0 - report.Ggg) * (run.VdokTotal2 / Divisor(report)) * run.OxidizerDensity);

    private static double Divisor(CycleReport report) => 1.0 - report.Gdokleft / report.Ggg;

    private static void AssertClosedForms(Run run, CycleReport report)
    {
        AssertRelative(ClosedDolM2(run, report), report.DolM2);
        AssertRelative(ClosedDolM3(run, report), report.DolM3);
    }

    private static Run Cycles(CorrectionSet corrections, int cycles, ModelParameters? parameters = null, bool allBridge = false)
    {
        var analytic = AnalyticCycle.Prepare(
            ShippedFormulations.Read("P312"), parameters ?? ModelParameters.Default, PrecisionKind.Binary64, corrections);
        var layout = analytic.Layout;
        var integerTotals = analytic.IntegerTotals();
        var realTotals = analytic.RealTotals();
        for (var i = 0; i < analytic.Ndok; i++)
        {
            realTotals[layout.VdokTotal + i] = 1.0 + 0.5 * i;
            realTotals[layout.VmkmTotal + i] = 2.0 + 0.25 * i;
        }

        realTotals[layout.VdokTotal2] = 7.0;
        realTotals[layout.VmkmTotal2] = 3.0;
        if (allBridge)
        {
            // The totals of the first variant are the all-bridge totals, cell by cell, and the scalars their sums.
            for (var i = 0; i < analytic.Ndok; i++)
            {
                realTotals[layout.Svd + i] = realTotals[layout.VdokTotal + i];
                realTotals[layout.Vsmkm + i] = realTotals[layout.VmkmTotal + i];
            }

            realTotals[layout.VdokTotal2] = Enumerable.Range(0, analytic.Ndok).Sum(i => realTotals[layout.VdokTotal + i]);
            realTotals[layout.VmkmTotal2] = Enumerable.Range(0, analytic.Ndok).Sum(i => realTotals[layout.VmkmTotal + i]);
        }

        var initial = realTotals.ToArray();

        var reports = new List<CycleReport>();
        for (var cycle = 1; cycle <= cycles; cycle++)
        {
            reports.Add(analytic.Compute(cycle, integerTotals, realTotals, out _));
        }

        return new Run(initial, realTotals, reports, layout, analytic.Ndok, analytic.OxidizerDensity);
    }

    private static void AssertRelative(double expected, double actual) =>
        Assert.True(Math.Abs(actual - expected) <= RelativeTolerance * Math.Abs(expected),
            $"expected {expected:R}, got {actual:R}: relative {Math.Abs(actual - expected) / Math.Abs(expected):E3}");

    private sealed record Run(
        double[] Initial, double[] Final, List<CycleReport> Reports, AccumulatorLayout Layout, int Ndok, double OxidizerDensity)
    {
        public double VdokTotalSum => Enumerable.Range(0, Ndok).Sum(i => Initial[Layout.VdokTotal + i]);

        public double VmkmTotalSum => Enumerable.Range(0, Ndok).Sum(i => Initial[Layout.VmkmTotal + i]);

        public double VdokTotal2 => Initial[Layout.VdokTotal2];

        public double VmkmTotal2 => Initial[Layout.VmkmTotal2];

        public bool IsVdokTotal(int index) =>
            index >= Layout.VdokTotal && index < Layout.VdokTotal + Ndok || index == Layout.VdokTotal2;
    }
}
