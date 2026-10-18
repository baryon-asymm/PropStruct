using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>DoksdConsistent</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 399-400, 405): under
/// <c>JZZ = 2</c> the original subtracts this law's <c>DOKM^2</c> from the other law's second moment, so
/// <c>DOKSD</c> is the variance of nothing, and negative on P2. Corrected, it is the mass-weighted variance of the
/// law uniform in <c>D</c>. The right side is a closed form and an independent quadrature, to 1e-12; the red side is
/// the same input without the flag.
/// </summary>
public class DoksdConsistentTests
{
    private const double RelativeTolerance = 1e-12;
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    // Four-point Gauss-Legendre on [-1, 1]: exact for a polynomial of degree 7, the integrands below have degree 5.
    private static readonly (double Node, double Weight)[] GaussLegendre =
    [
        (-0.8611363115940526, 0.3478548451374538),
        (-0.3399810435848563, 0.6521451548625461),
        (0.3399810435848563, 0.6521451548625461),
        (0.8611363115940526, 0.3478548451374538),
    ];

    /// <summary>The name of every shipped formulation whose size law is 2, read off the files.</summary>
    public static IEnumerable<object[]> SizeLawTwoFormulations() =>
        ShippedFormulations.Names.Where(name => ShippedFormulations.Read(name).SizeLawCode == 2).Select(name => new object[] { name });

    /// <summary>One fraction, <c>JZZ = 2</c>: <c>(2/3)(u^6 - l^6)/(u^4 - l^4) - DOKM^2</c>, with <c>DOKM = 0.8 (u^5 - l^5)/(u^4 - l^4)</c>.</summary>
    [Theory]
    [InlineData(10e-6, 100e-6)]
    [InlineData(257e-6, 343e-6)]
    [InlineData(1e-6, 3e-4)]
    public void OneFractionGivesTheClosedForm(double lower, double upper)
    {
        var (dokm, doksd) = Analytic(2, [1.0], [1.0], [lower, upper], CorrectionSet.DoksdConsistent);

        var denominator = Math.Pow(upper, 4) - Math.Pow(lower, 4);
        var expectedMean = 0.8 * (Math.Pow(upper, 5) - Math.Pow(lower, 5)) / denominator;
        var expected = 2.0 / 3.0 * (Math.Pow(upper, 6) - Math.Pow(lower, 6)) / denominator - expectedMean * expectedMean;
        AssertRelative(expectedMean, dokm);
        AssertRelative(expected, doksd);
    }

    /// <summary>
    /// The independent computation: <c>Sum ZX_j/(u_j - l_j) * Integral D^3 (D - m)^2 dD</c> over
    /// <c>Sum ZX_j/(u_j - l_j) * Integral D^3 dD</c>, the density of a law uniform in <c>D</c> within each fraction,
    /// weighted by mass (<c>D^3</c>), integrated by quadrature and not through the moments the code sums.
    /// </summary>
    [Theory]
    [MemberData(nameof(SizeLawTwoFormulations))]
    public void EveryShippedSizeLawTwoFormulationGivesTheQuadratureOfTheMassWeightedVariance(string name)
    {
        var tables = Prepared(name, CorrectionSet.DoksdConsistent, out var pending);

        var (mean, variance) = MassWeightedMoments(tables.Bounds, tables.Share);
        AssertRelative(mean, pending.Dokm);
        AssertRelative(variance, pending.Doksd);
    }

    /// <summary>The same quadrature on constructed fractions of unequal width and share, wider than any shipped one.</summary>
    [Fact]
    public void ThreeConstructedFractionsGiveTheQuadrature()
    {
        double[] bounds = [5e-6, 40e-6, 40e-6, 90e-6, 120e-6, 400e-6];
        double[] share = [0.5, 0.3, 0.2];
        var (mean, variance) = MassWeightedMoments(bounds, share);

        var (dokm, doksd) = Analytic(2, [0.2, 0.3, 0.5], share, bounds, CorrectionSet.DoksdConsistent);

        AssertRelative(mean, dokm);
        AssertRelative(variance, doksd);
    }

    /// <summary>Red: the same inputs under <c>None</c> are the other law's, 75.45 µm on P33 as the original prints it, and negative on P2.</summary>
    [Fact]
    public void WithoutTheFlagP33IsTheOriginalsPrintedValueAndP2IsNegative()
    {
        var resultsPath = RepositoryPaths.Resolve("tests", "Fixtures", "references", "P33", "results.m.txt");
        var printed = ResultsMFile.Parse(resultsPath)["Dok43sd"][0];

        _ = Prepared("P33", CorrectionSet.None, out var p33);
        Assert.Equal(printed, Math.Sqrt(p33.Doksd) * 1e6, 2);

        _ = Prepared("P2", CorrectionSet.None, out var p2);
        Assert.True(p2.Doksd < 0.0, $"P2's DOKSD is {p2.Doksd}");
        Assert.True(double.IsNaN(Math.Sqrt(p2.Doksd)));
    }

    /// <summary>
    /// Right, at the size of the effect: the figures the corrected statement is stated to print, 112.66 µm on P33
    /// (against 75.45 µm) and a finite 8.07 µm on P2 (against <c>NaN</c>), two decimals as the report prints them.
    /// </summary>
    [Fact]
    public void WithTheFlagP33AndP2PrintTheStatedFigures()
    {
        _ = Prepared("P33", CorrectionSet.DoksdConsistent, out var p33);
        Assert.Equal(112.66, Math.Sqrt(p33.Doksd) * 1e6, 2);

        _ = Prepared("P2", CorrectionSet.DoksdConsistent, out var p2);
        Assert.Equal(8.07, Math.Sqrt(p2.Doksd) * 1e6, 2);
    }

    /// <summary>A size law other than 2 is not touched: the flag changes no bit of <c>DOKM</c> or <c>DOKSD</c>.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void AnySizeLawButTwoIsBitIdenticalToNone(int sizeLaw)
    {
        double[] bounds = [5e-6, 40e-6, 40e-6, 90e-6, 120e-6, 400e-6];
        double[] share = [0.5, 0.3, 0.2];
        double[] massShare = [0.2, 0.3, 0.5];

        var without = Analytic(sizeLaw, massShare, share, bounds, CorrectionSet.None);
        var with = Analytic(sizeLaw, massShare, share, bounds, CorrectionSet.DoksdConsistent);

        Assert.Empty(BitwiseComparison.Differences(without, with, $"sizeLaw {sizeLaw}"));
    }

    /// <summary>The flag under <c>JZZ = 2</c> is a difference of the whole size, so that the bit comparison above is not vacuous.</summary>
    [Fact]
    public void UnderSizeLawTwoTheFlagMovesDoksdAndNotDokm()
    {
        double[] bounds = [5e-6, 40e-6, 40e-6, 90e-6, 120e-6, 400e-6];
        double[] share = [0.5, 0.3, 0.2];
        double[] massShare = [0.2, 0.3, 0.5];

        var (dokmWithout, doksdWithout) = Analytic(2, massShare, share, bounds, CorrectionSet.None);
        var (dokmWith, doksdWith) = Analytic(2, massShare, share, bounds, CorrectionSet.DoksdConsistent);

        Assert.Equal(BitConverter.DoubleToInt64Bits(dokmWithout), BitConverter.DoubleToInt64Bits(dokmWith));
        Assert.True(Math.Abs(doksdWith - doksdWithout) > 0.1 * Math.Abs(doksdWithout));
    }

    /// <summary>Under the <c>Original</c> precision kind the flag is replaced by <c>None</c> on entry.</summary>
    [Fact]
    public void UnderOriginalTheFlagIsIgnored()
    {
        double[] bounds = [5e-6, 40e-6, 40e-6, 90e-6, 120e-6, 400e-6];
        double[] share = [0.5, 0.3, 0.2];
        double[] massShare = [0.2, 0.3, 0.5];

        var without = Analytic(2, massShare, share, bounds, CorrectionSet.None, PrecisionKind.Original);
        var with = Analytic(2, massShare, share, bounds, CorrectionSet.DoksdConsistent, PrecisionKind.Original);

        Assert.Empty(BitwiseComparison.Differences(without, with, "Original"));
    }

    private static (double Dokm, double Doksd) Analytic(
        int sizeLaw, double[] massShare, double[] share, double[] bounds, CorrectionSet corrections,
        PrecisionKind precision = PrecisionKind.Binary64)
    {
        Setup.AnalyticSizes(sizeLaw, share.Length, massShare, share, bounds, precision, corrections, out var dokm, out var doksd);
        return (dokm, doksd);
    }

    private static SetupTables Prepared(string name, CorrectionSet corrections, out PendingEchoes pending)
    {
        var status = Setup.Prepare(ShippedFormulations.Read(name), ModelParameters.Default, PrecisionKind.Binary64, corrections,
            NeighbourBudget, PocketRedrawBudget, out _, out var tables, out _, out pending, out _);
        Assert.Equal(SetupStatus.Ok, status);
        return tables;
    }

    private static (double Mean, double Variance) MassWeightedMoments(double[] bounds, double[] share)
    {
        var mass = 0.0;
        var firstMoment = 0.0;
        for (var j = 0; j < share.Length; j++)
        {
            var density = share[j] / (bounds[2 * j + 1] - bounds[2 * j]);
            mass += density * Integrate(bounds[2 * j], bounds[2 * j + 1], d => d * d * d);
            firstMoment += density * Integrate(bounds[2 * j], bounds[2 * j + 1], d => d * d * d * d);
        }

        var mean = firstMoment / mass;
        var spread = 0.0;
        for (var j = 0; j < share.Length; j++)
        {
            var density = share[j] / (bounds[2 * j + 1] - bounds[2 * j]);
            spread += density * Integrate(bounds[2 * j], bounds[2 * j + 1], d => d * d * d * (d - mean) * (d - mean));
        }

        return (mean, spread / mass);
    }

    private static double Integrate(double lower, double upper, Func<double, double> integrand)
    {
        var half = 0.5 * (upper - lower);
        var middle = 0.5 * (upper + lower);
        return GaussLegendre.Sum(point => point.Weight * integrand(middle + half * point.Node)) * half;
    }

    private static void AssertRelative(double expected, double actual) =>
        Assert.True(Math.Abs(actual - expected) <= RelativeTolerance * Math.Abs(expected),
            $"expected {expected:R}, got {actual:R}: relative {Math.Abs(actual - expected) / Math.Abs(expected):E3}");
}
