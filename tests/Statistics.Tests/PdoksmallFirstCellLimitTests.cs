using PropStruct.Input;
using PropStruct.Particle;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The limit of <c>PdoksmallFirstCell</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 1031, 1059-1061),
/// measured against an integral that does not pass through the code. The corrected first cell is the 1061 formula, but its
/// input <c>zdoksmall(1)</c> is line 1031's partial-cell share: <c>Xr = 1/AK2</c> of the cell's coarse mass, as if that mass
/// were spread uniformly over <c>[0, Di)</c>, while <c>vdokstr(1)</c> holds only particles above <c>Dmin</c> (Fortran 533
/// rejects <c>Db &lt;= Dmin</c> before 542 accumulates). The model's own definition of the cell is the coarse mass in
/// <c>[Dmin, Di/AK2)</c> over all coarse mass, and its size; the integral below is that, taken from the size law through
/// <see cref="AnalyticCycle"/>, and the closed form of 1050-1061 is <see cref="SmallParticlesClosedForm"/>. The limit is
/// declared, not fixed (the histogram-edge class of <c>GdokleftOnce</c>'s resolution limit).
/// </summary>
public class PdoksmallFirstCellLimitTests(ITestOutputHelper output)
{
    private const double RelativeTolerance = 1e-12;
    private const int QuadratureSteps = 2000;
    private static readonly Formulation P312 = ShippedFormulations.Read("P312");

    /// <summary>
    /// Positive control of the integral itself, on an answer known beforehand: P312's first fraction is uniform in mass
    /// over its bounds (the law of <c>JZZ = 1</c>), so the coarse mass between <c>Dmin</c> and <c>Di/AK2</c> is linear in
    /// <c>Dmin</c> and its mass-weighted mean size is the midpoint, whatever the density.
    /// </summary>
    [Fact]
    public void TheIntegralOfACoarseMassBelowHalfACellIsLinearAndItsMeanIsTheMidpoint()
    {
        var cycle = Cycle(3e-6);
        var half = cycle.CellSize / cycle.Ak2;

        var three = cycle.MassBetween(3e-6, half);
        var four = cycle.MassBetween(4e-6, half);

        AssertRelative((half - 3e-6) / (half - 4e-6), three / four);
        AssertRelative(0.5 * (3e-6 + half), cycle.FirstMomentBetween(3e-6, half, QuadratureSteps) / three);
    }

    /// <summary>
    /// The size of the limit below <c>Di/AK2</c>, P312 at <c>Dmin = 3 um</c> (<c>gdokleft</c> is 0 there, as below any <c>Di</c>).
    /// Right: the corrected cell is what line 1031 gives from the cell's coarse mass, spread uniformly, i.e.
    /// <c>zdoksmall = GGG (mass in [Dmin, Di) / coarse mass) / AK2</c> and <c>ddoksmall = 1/(2 AK2)</c>, in the closed form
    /// of 1050-1061. Reported, and asserted in direction: against the integral over <c>[Dmin, Di/AK2)</c> the share is
    /// overstated (spread over <c>[0, Di/AK2)</c> where the particles are above <c>Dmin</c>), the mean size is understated,
    /// and the cell, their product, is overstated by the smaller of the two effects.
    /// </summary>
    [Fact]
    public void BelowHalfACellTheCorrectedFirstCellIsLine1031sSpreadShareAndOverstatesTheIntegral()
    {
        var measured = Measure(3e-6);

        Assert.Equal(0.0, measured.Gdokleft);
        Assert.Equal(0.0, measured.None);
        AssertRelative(measured.Predicted, measured.Corrected);
        Assert.True(measured.True > 0.0);
        Assert.True(measured.ZPredicted > measured.ZTrue);
        Assert.True(measured.Corrected > measured.True, $"corrected {measured.Corrected:R}, true {measured.True:R}");
        output.WriteLine($"P312 Dmin 3 um: zdoksmall(1) {measured.ZPredicted:R} against {measured.ZTrue:R} true (x{measured.ZPredicted / measured.ZTrue:F2}); "
            + $"pdoksmall(1) {measured.Corrected:R} against {measured.True:R} true (x{measured.Corrected / measured.True:F2})");
    }

    /// <summary>
    /// The size of the limit from <c>Di/AK2</c> up, P312 at <c>Dmin = 5 um</c> (<c>Di/AK2</c> exactly) and <c>6 um</c>:
    /// no coarse particle has <c>Db</c> below <c>Di/AK2</c>, so the model's own cell is 0 (the integral is empty), while
    /// the corrected cell is positive: it is line 1031's spread share all the same.
    /// </summary>
    [Theory]
    [InlineData(5e-6)]
    [InlineData(6e-6)]
    public void FromHalfACellUpTheTrueFirstCellIsZeroAndTheCorrectedOneIsPositive(double dmin)
    {
        var measured = Measure(dmin);

        Assert.Equal(0.0, measured.True);
        Assert.Equal(0.0, measured.None);
        AssertRelative(measured.Predicted, measured.Corrected);
        Assert.True(measured.Corrected > 0.0);
        output.WriteLine($"P312 Dmin {dmin * 1e6:F0} um: pdoksmall(1) {measured.Corrected:R} corrected, 0 true");
    }

    private static AnalyticCycle Cycle(double dmin, CorrectionSet corrections = CorrectionSet.None) =>
        AnalyticCycle.Prepare(P312, ModelParameters.Default with { Dmin = Length.FromMetres(dmin) }, PrecisionKind.Binary64, corrections);

    private static Limit Measure(double dmin)
    {
        var none = Cycle(dmin);
        var corrected = Cycle(dmin, CorrectionSet.PdoksmallFirstCell);
        var reportNone = none.Compute(1, none.IntegerTotals(), none.RealTotals(), out var nextNone);
        var report = corrected.Compute(1, corrected.IntegerTotals(), corrected.RealTotals(), out var nextCorrected);

        var cell = corrected.CellSize;
        var half = cell / corrected.Ak2;
        var coarse = corrected.MassBetween(dmin, corrected.Ndok * cell);

        // Line 1031 on the cell's coarse mass: Xr = 1/AK2 of it, spread uniformly, so its mean size is half of Di/AK2.
        var zPredicted = report.Ggg * corrected.MassBetween(dmin, cell) / coarse / corrected.Ak2;
        var predicted = SmallParticlesClosedForm.Product(zPredicted, 0.5 / corrected.Ak2, report.Ggg, report.Gdokleft,
            corrected.PocketCoefficient, corrected.OxidizerDensity, corrected.PropellantDensity);

        // The model's own definition: the coarse mass in [Dmin, Di/AK2), its share of the coarse mass and its mean size.
        var below = corrected.MassBetween(dmin, half);
        var zTrue = report.Ggg * below / coarse;
        var trueCell = below > 0.0
            ? SmallParticlesClosedForm.Product(zTrue, corrected.FirstMomentBetween(dmin, half, QuadratureSteps) / below / cell,
                report.Ggg, report.Gdokleft, corrected.PocketCoefficient, corrected.OxidizerDensity, corrected.PropellantDensity)
            : 0.0;

        Assert.Equal(reportNone.Gdokleft, report.Gdokleft);
        return new Limit(report.Gdokleft, nextNone[0], nextCorrected[0], predicted, trueCell, zPredicted, zTrue);
    }

    private static void AssertRelative(double expected, double actual) =>
        Assert.True(Math.Abs(actual - expected) <= RelativeTolerance * Math.Abs(expected),
            $"expected {expected:R}, got {actual:R}: relative {Math.Abs(actual - expected) / Math.Abs(expected):E3}");

    private sealed record Limit(
        double Gdokleft, double None, double Corrected, double Predicted, double True, double ZPredicted, double ZTrue);
}
