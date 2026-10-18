using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>ExactConstants</c> at <c>PARAM</c>'s two sites (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran
/// 1704 and 1716): the coefficients <c>3/3.14</c> and <c>24/3.14</c> are <c>3/π</c> and <c>24/π</c>, so the
/// normalising sum <c>ZSS</c>, and λ with it, is the <c>None</c> value times <c>3.14/π</c>, and <c>ZX</c> and
/// <c>Z11</c>, ratios of the same terms, move in their last bits only. The sites of lines 1014 and 1016 are the
/// per-cycle statements', and the <c>Legacy</c> test that finds the four sites in the line map's ranges comes with them.
/// </summary>
public class ExactConstantsTests
{
    private const double RelativeTolerance = 1e-12;
    // Four ulp of 1 (2^-52 is 2.2e-16): measured over the 49 files, three exceed two ulp and none four.
    private const double ShareTolerance = 8.9e-16;
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    private const double Ratio = 3.14 / Math.PI;

    /// <summary>
    /// Right: on every shipped formulation, both size laws, <c>ZSS</c> and λ equal the <c>None</c> value times
    /// <c>3.14/π</c> to 1e-12 relative, and <c>None</c> reproduces the old bits.
    /// </summary>
    [Theory]
    [MemberData(nameof(ShippedNames))]
    public void ZssAndLambdaAreTheNoneValuesTimesThreePointOneFourOverPi(string name)
    {
        var (noneZss, noneLambda) = Prepared(name, CorrectionSet.None, out _, out _);
        var (exactZss, exactLambda) = Prepared(name, CorrectionSet.ExactConstants, out _, out _);

        AssertRelative(noneZss * Ratio, exactZss);
        AssertRelative(noneLambda * Ratio, exactLambda);
    }

    /// <summary>Red: under <c>None</c> the ratio is exactly 1, which the check above rejects, the effect being 5.1e-4.</summary>
    [Theory]
    [MemberData(nameof(ShippedNames))]
    public void WithoutTheFlagTheRatioIsOneAndTheCheckAboveWouldBeRed(string name)
    {
        var (noneZss, noneLambda) = Prepared(name, CorrectionSet.None, out _, out _);
        var (otherZss, otherLambda) = Prepared(name, CorrectionSet.GdokleftOnce, out _, out _);

        Assert.Equal(BitConverter.DoubleToInt64Bits(noneZss), BitConverter.DoubleToInt64Bits(otherZss));
        Assert.Equal(BitConverter.DoubleToInt64Bits(noneLambda), BitConverter.DoubleToInt64Bits(otherLambda));
        Assert.True(Math.Abs(noneZss * Ratio - noneZss) > 1e-4 * noneZss, "the effect is smaller than the tolerance of the check");
    }

    /// <summary>
    /// <c>ZX</c> and <c>Z11</c> are the same terms divided by their sum, so the constant cancels and they move in
    /// their last bits only: within a few ulp of the <c>None</c> value, cell by cell.
    /// </summary>
    [Theory]
    [MemberData(nameof(ShippedNames))]
    public void ShareAndCumulativeMoveInTheirLastBitsOnly(string name)
    {
        _ = Prepared(name, CorrectionSet.None, out var none, out _);
        _ = Prepared(name, CorrectionSet.ExactConstants, out var exact, out _);

        for (var j = 0; j < none.Share.Length; j++)
        {
            Assert.True(Math.Abs(exact.Share[j] - none.Share[j]) <= ShareTolerance * Math.Max(none.Share[j], 1e-300), $"{name} ZX[{j}]");
        }

        for (var j = 0; j < none.Cumulative.Length; j++)
        {
            Assert.True(Math.Abs(exact.Cumulative[j] - none.Cumulative[j]) <= ShareTolerance, $"{name} Z11[{j}]");
        }
    }

    /// <summary>Right on a known answer: one fraction's <c>ZSS</c> is its own term, <c>g 24/π (u - l)/(u^4 - l^4)</c> for the size law 2.</summary>
    [Fact]
    public void OneFractionOfSizeLawTwoGivesTwentyFourOverPi()
    {
        const double lower = 10e-6;
        const double upper = 100e-6;
        FractionLaw.Build(2, 1, [0.64], [lower, upper], PrecisionKind.Binary64, CorrectionSet.ExactConstants, out _, out _, out var zss);

        AssertRelative(0.64 * (24.0 / Math.PI) * (upper - lower) / (Math.Pow(upper, 4) - Math.Pow(lower, 4)), zss);
    }

    /// <summary>Right on a known answer: one fraction's <c>ZSS</c> for any other law is <c>g 3/π (l + u)/(l^2 u^2)</c>.</summary>
    [Fact]
    public void OneFractionOfAnotherSizeLawGivesThreeOverPi()
    {
        const double lower = 10e-6;
        const double upper = 100e-6;
        FractionLaw.Build(1, 1, [0.64], [lower, upper], PrecisionKind.Binary64, CorrectionSet.ExactConstants, out _, out _, out var zss);

        AssertRelative(0.64 * (3.0 / Math.PI) * (lower + upper) / (lower * lower * upper * upper), zss);
    }

    /// <summary>Under <see cref="PrecisionKind.Original"/> the flag is replaced by <c>None</c> on entry, bit for bit.</summary>
    [Theory]
    [MemberData(nameof(ShippedNames))]
    public void UnderOriginalTheFlagIsIgnored(string name)
    {
        var formulation = ShippedFormulations.Read(name);
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Original, CorrectionSet.None,
            NeighbourBudget, PocketRedrawBudget, out _, out var none, out _, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);

        FractionLaw.Build(formulation.SizeLawCode, formulation.Fractions.Length, none.MassShare, none.Bounds, PrecisionKind.Original,
            CorrectionSet.ExactConstants, out var share, out var cumulative, out var sum);

        Assert.Empty(BitwiseComparison.Differences((none.Share, none.Cumulative, none.Zss), (share, cumulative, sum), name));
    }

    public static IEnumerable<object[]> ShippedNames() => ShippedFormulations.Names.Select(name => new object[] { name });

    private static (double Zss, double Lambda) Prepared(string name, CorrectionSet corrections, out SetupTables tables, out ModelSetup setup)
    {
        var status = Setup.Prepare(ShippedFormulations.Read(name), ModelParameters.Default, PrecisionKind.Binary64, corrections,
            NeighbourBudget, PocketRedrawBudget, out setup, out tables, out _, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);
        return (tables.Zss, setup.Lambda);
    }

    private static void AssertRelative(double expected, double actual) =>
        Assert.True(Math.Abs(actual - expected) <= RelativeTolerance * Math.Abs(expected),
            $"expected {expected:R}, got {actual:R}: relative {Math.Abs(actual - expected) / Math.Abs(expected):E3}");
}
