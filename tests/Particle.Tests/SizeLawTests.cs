using ILGPU.Runtime;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L0 of the BOOT.md table: <see cref="SizeLaw.Sample"/> against the hand-derived values of
/// Fortran <c>SIZE</c> (<c>tests/Fixtures/cases/particle/size_law.json</c>, script
/// <c>formulas_particle.py</c>), bit for bit — both the uniform-in-D branch
/// (<c>sizeLaw == 2</c>, lines 1772-1773) and the uniform-in-1/D² branch (any other value,
/// lines 1769-1770), plus the fraction-boundary edge cases the scanning loop's own "last
/// match wins, no break" (lines 1765-1767) produces.
/// </summary>
public class SizeLawTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public SizeLawTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var testCase in FormulaCaseFiles.ReadSizeLawCases())
        {
            yield return new object[]
            {
                testCase.Name, testCase.SizeLaw, testCase.Bounds.ToArray(), testCase.Cumulative.ToArray(),
                testCase.X, testCase.X1, testCase.Expected.Fraction, testCase.Expected.Diameter,
            };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void SampleMatchesTheFixtureBitForBit(
        string name, int sizeLaw, double[] bounds, double[] cumulative,
        double x, double x1, int expectedFractionOneBased, double expectedDiameter)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(cumulative);

        var acc = _host.Accelerator;
        using var boundsBuffer = acc.Allocate1D(bounds);
        using var cumulativeBuffer = acc.Allocate1D(cumulative);
        var fractionCount = cumulative.Length - 1;

        SizeLaw.Sample(
            sizeLaw, fractionCount, boundsBuffer.View.BaseView, cumulativeBuffer.View.BaseView,
            x, x1, out var diameter, out var fraction);

        var expectedFraction = expectedFractionOneBased - 1; // the fixture's MINV is Fortran 1-based
        Assert.True(expectedFraction == fraction, $"{name}: expected fraction {expectedFraction}, got {fraction}");
        Assert.True(
            BitConverter.DoubleToInt64Bits(expectedDiameter) == BitConverter.DoubleToInt64Bits(diameter),
            $"{name}: expected diameter {expectedDiameter}, got {diameter}");
    }

    /// <summary>
    /// Item 2 of the 2026-09-17 audit: <c>fractionCount</c> must be the value the caller
    /// passes (the original's own <c>NM</c> parameter), not one derived from
    /// <paramref name="cumulative"/>'s own length — the two only happen to agree when the
    /// caller's arrays are sized exactly <c>NM + 1</c>. Here they deliberately do not: a
    /// 3-fraction table is passed but the caller declares only 2 of them active, and
    /// <paramref name="x"/> is chosen to fall inside the third (unscanned) fraction if the
    /// count were wrongly re-derived as <c>cumulative.Length - 1</c> (3) instead of read from
    /// the parameter (2) — the scanning loop would then never update <c>fraction</c> off its
    /// start value 0.
    /// </summary>
    private static readonly double[] ThreeFractionBounds = [0.0, 1.0, 1.0, 2.0, 2.0, 3.0]; // 3 fractions
    private static readonly double[] FourEntryCumulative = [0.0, 0.3, 0.6, 1.0]; // length 4: cumulative.Length - 1 == 3

    [Fact]
    public void FractionCountComesFromTheParameterNotFromCumulativeLength()
    {
        var acc = _host.Accelerator;
        using var boundsBuffer = acc.Allocate1D(ThreeFractionBounds);
        using var cumulativeBuffer = acc.Allocate1D(FourEntryCumulative);

        SizeLaw.Sample(
            sizeLaw: 2, fractionCount: 2, boundsBuffer.View.BaseView, cumulativeBuffer.View.BaseView,
            x: 0.8, x1: 0.5, out var diameter, out var fraction);

        // With fractionCount honoured as 2, the loop only ever compares x against
        // cumulative[0..2] (0.0, 0.3, 0.6): 0.8 matches neither (0, 0.3] nor (0.3, 0.6], so
        // fraction stays at its start value 0 and diameter comes from fraction 0's own
        // bounds (0.0, 1.0). Re-deriving the count as cumulative.Length - 1 (3) would instead
        // scan (0.6, 1.0] too, match it, and return fraction 2 with a diameter from
        // bounds[4..5] (2.0, 3.0) — a different, wrong answer this test would then produce.
        Assert.Equal(0, fraction);
        Assert.Equal(0.5, diameter); // x1 * (1.0 - 0.0) + 0.0
    }
}
