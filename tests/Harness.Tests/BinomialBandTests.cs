using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// this node's BOOT.md, "Fable 5.1 decision II: the calibration curve (2026-09-20)": cross-checks
/// <see cref="BinomialBand.HighestDensityRegion"/> against an independent implementation of the same
/// highest-density construction, in Python against <c>scipy.stats.binom</c>'s own PMF (never against this node's
/// own formula a second time — root BOOT.md Taboos: "no second implementation of any part of ... a formula" cuts
/// the other way here, so the *test's* own reference values come from a library this node does not otherwise
/// depend on). There is no fixture file for this: `tests/Fixtures` is a neighbour node this coding task may not
/// write into, and no existing fixture holds a binomial highest-density region, so the reference values are typed
/// here directly (root BOOT.md Taboos: "no expected value typed into a test when it exists in a fixture file" —
/// none does), each reproducible from the script below.
///
/// Reference values from (scipy 1.18.1, numpy):
/// <code>
/// import numpy as np
/// from scipy.stats import binom
/// def hpd_band(n, p, confidence_alpha):
///     pmf = binom.pmf(np.arange(n + 1), n, p)
///     order = np.argsort(-pmf)
///     cum = 0.0
///     included = np.zeros(n + 1, dtype=bool)
///     target = 1.0 - confidence_alpha
///     for idx in order:
///         included[idx] = True
///         cum += pmf[idx]
///         if cum >= target:
///             break
///     idxs = np.nonzero(included)[0]
///     return idxs.min(), idxs.max()
/// </code>
/// </summary>
public class BinomialBandTests
{
    [Theory]
    [InlineData(96L, 0.001, 1e-3, 0L, 2L)]
    [InlineData(1000L, 0.05, 1e-3, 29L, 74L)]
    [InlineData(58244L, 0.05, 1e-3, 2740L, 3086L)]
    [InlineData(58244L, 0.001, 1e-3, 35L, 84L)]
    [InlineData(500L, 0.01, 1e-3, 0L, 13L)]
    [InlineData(1L, 0.5, 1e-3, 0L, 1L)]
    [InlineData(10L, 0.5, 0.05, 2L, 8L)]
    [InlineData(5L, 0.99, 0.05, 5L, 5L)]
    [InlineData(220048L, 0.05, 1e-3, 10668L, 11340L)]
    public void MatchesScipysHighestDensityRegion(long n, double p, double confidenceAlpha, long expectedLow, long expectedHigh)
    {
        var (low, high) = BinomialBand.HighestDensityRegion(n, p, confidenceAlpha);

        Assert.Equal(expectedLow, low);
        Assert.Equal(expectedHigh, high);
    }

    // Non-degeneracy (AGENTS.md §13): a band that always reports "inside" would pass every calibration check
    // vacuously. A count far outside the true region (all n, none matching p) must fall outside the computed band.
    [Fact]
    public void RejectsACountFarOutsideTheBand()
    {
        var (low, high) = BinomialBand.HighestDensityRegion(58244L, 0.05, 1e-3);
        Assert.True(low > 0 && high < 58244L, "the band must be a proper subset of [0, n] for this case.");
        Assert.True(20000L > high, "20000 failures out of 58244 at nominal 5% must fall outside the band.");
    }

    [Fact]
    public void DegenerateWhenThereAreNoCellTests()
    {
        var (low, high) = BinomialBand.HighestDensityRegion(0L, 0.05, 1e-3);
        Assert.Equal(0L, low);
        Assert.Equal(0L, high);
    }
}
