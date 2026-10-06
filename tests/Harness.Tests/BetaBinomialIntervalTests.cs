using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// this node's BOOT.md, "Fable 5.1 decision V: the conditional dispersion diagnostic and the beta-binomial
/// predictive (2026-09-20)": cross-checks <see cref="BetaBinomialPredictive.Interval"/> against an
/// independent implementation of the same equal-tailed construction, in Python against
/// <c>scipy.stats.betabinom</c>'s own PMF (<c>scipy.stats.binom</c> at <c>rho == 0</c>, this method's own genuine
/// special case) — never against this node's own formula a second time (root BOOT.md Taboos: "no second
/// implementation of any part of ... a formula" cuts the other way here, so the *test's* own reference values come
/// from a library this node does not otherwise depend on). There is no fixture file for this: `tests/Fixtures` is
/// a neighbour node this coding task may not write into, and no existing fixture holds a beta-binomial equal-
/// tailed interval, so the reference values are typed here directly (root BOOT.md Taboos: "no expected value
/// typed into a test when it exists in a fixture file" — none does), each reproducible from the script below.
///
/// Reference values from (scipy 1.18.1, numpy):
/// <code>
/// import numpy as np
/// from scipy.stats import betabinom, binom
///
/// def pmf_array(n, p, rho):
///     if rho &lt;= 0.0:
///         return binom.pmf(np.arange(n + 1), n, p)
///     a = p * (1.0 / rho - 1.0)
///     b = (1.0 - p) * (1.0 / rho - 1.0)
///     return betabinom.pmf(np.arange(n + 1), n, a, b)
///
/// def equal_tailed(n, p, rho, alpha):
///     pmf = pmf_array(n, p, rho)
///     cdf = np.cumsum(pmf)
///     low = int(np.searchsorted(cdf, alpha / 2.0, side='left'))
///     high = int(np.searchsorted(cdf, 1.0 - alpha / 2.0, side='left'))
///     low_excluded = cdf[low - 1] if low &gt; 0 else 0.0
///     attained = low_excluded + (1.0 - cdf[high])
///     return n * p, low, high, attained
/// </code>
///
/// The two rows carrying `n`, `p` and `rho` from a real formulation (HMX's own `fqdokkarm(31,:)` row-pooled
/// estimate, and `coef`'s own largest `n_bar`) are named for that provenance, not typed as a coincidence: they are
/// `tests/Harness/HISTORY.md#fable-5-1-decision-v`, "Implementation and measurements" own figures, re-verified here
/// against scipy rather than re-derived from this file's own formula.
/// </summary>
public class BetaBinomialIntervalTests
{
    [Theory]
    [InlineData(10L, 0.3, 0.0, 0.05, 3.0, 0L, 6L, 0.0105921)]
    [InlineData(10L, 0.3, 0.2, 0.05, 3.0, 0L, 8L, 0.02287)]
    [InlineData(100L, 0.05, 0.0, 0.05, 5.0, 1L, 10L, 0.0173929)]
    [InlineData(100L, 0.05, 0.1, 0.05, 5.0, 0L, 26L, 0.0228236)]
    [InlineData(2000L, 0.002, 0.0006, 0.05, 4.0, 0L, 11L, 0.0217974)]
    // HMX's own coef, `tests/Harness/HISTORY.md#fable-5-1-decision-v`: n_bar = 10983, rho_hat = 0.0006054.
    [InlineData(10983L, 0.001, 0.0006054, 1e-3, 10.983, 0L, 65L, 0.000460484)]
    // HMX's own fqdokkarm(31,:), row-pooled: n_bar = 2502, p_hat = 11/1256 at the reference's own total.
    [InlineData(1256L, 0.00875796178, 0.02138, 0.05, 11.0, 0L, 61L, 0.0249468)]
    [InlineData(50L, 0.5, 0.5, 0.05, 25.0, 0L, 50L, 0.0)]
    [InlineData(5L, 0.9, 0.0, 0.05, 4.5, 3L, 5L, 0.00856)]
    [InlineData(1L, 0.5, 0.3, 0.05, 0.5, 0L, 1L, 0.0)]
    public void MatchesScipysEqualTailedInterval(
        long n, double p, double rho, double alpha, double expectedMean, long expectedLow, long expectedHigh,
        double expectedAttainedAlpha)
    {
        var (mean, low, high, attainedAlpha) = BetaBinomialPredictive.Interval(n, p, rho, alpha);

        Assert.True(Math.Abs(expectedMean - mean) < 1e-6, $"mean: expected {expectedMean}, got {mean}.");
        Assert.Equal(expectedLow, low);
        Assert.Equal(expectedHigh, high);
        Assert.True(
            Math.Abs(expectedAttainedAlpha - attainedAlpha) < 1e-4,
            $"AttainedAlpha: expected {expectedAttainedAlpha}, got {attainedAlpha}.");
    }

    // Non-degeneracy (AGENTS.md §13): a check that always returns [0, n] would pass every calibration check
    // vacuously. A tight family (small rho, moderate n) must produce a proper subset of [0, n].
    [Fact]
    public void RejectsACountFarOutsideTheBandWhenDispersionIsSmall()
    {
        var (_, _, high, _) = BetaBinomialPredictive.Interval(10983L, 0.001, 0.0006054, 1e-3);
        Assert.True(high < 10983L, "the band must be a proper subset of [0, n] for this case.");
        Assert.True(high < 200L, "with n_bar = 10983, p = 0.001 and this row's own rho, a count of 200 must fall outside the band.");
    }

    // The same regression `NegativeBinomialInterval`'s own test guards against
    // (`tests/Harness/HISTORY.md#fable-5-1-decision-iii`, "A formula
    // defect, not only a numerical one"): a large n must not underflow the walk's own anchor and silently return
    // an `AttainedAlpha` of exactly 1 (the "cdf never moved" signature).
    [Fact]
    public void LargeNDoesNotUnderflowTheWalk()
    {
        var (_, low, high, attainedAlpha) = BetaBinomialPredictive.Interval(10983L, 0.001, 0.0006054, 1e-3);
        Assert.True(attainedAlpha < 0.9, "AttainedAlpha reading near 1 is the underflow signature this test guards against.");
        Assert.True(high > low, "a degenerate [low, high] = [0, 0] is the other half of the underflow signature.");
    }

    [Fact]
    public void DegenerateWhenThereAreNoTrials()
    {
        var (mean, low, high, attainedAlpha) = BetaBinomialPredictive.Interval(0L, 0.05, 0.1, 0.05);
        Assert.Equal(0.0, mean);
        Assert.Equal(0.0, low);
        Assert.Equal(0.0, high);
        Assert.Equal(0.0, attainedAlpha);
    }

    [Fact]
    public void DegenerateWhenPIsZero()
    {
        var (mean, low, high, attainedAlpha) = BetaBinomialPredictive.Interval(100L, 0.0, 0.1, 0.05);
        Assert.Equal(0.0, mean);
        Assert.Equal(0.0, low);
        Assert.Equal(0.0, high);
        Assert.Equal(0.0, attainedAlpha);
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the standing regression the coordinator asked for after this
    // file's own first draft carried the identical p/q swap the session had just found and fixed in
    // NegativeBinomialInterval — an external, hand-typed check (the theory above) caught it, but a swapped ratio
    // still normalizes to a valid-looking pmf, so the identity that actually distinguishes the two is the pmf
    // array's own reconstructed mean (sum(k * pmf[k])) against the analytic n * p: a wrong consecutive-term ratio
    // produces a pmf whose own mean is not n * p, exactly the mechanism this node's BOOT.md already documented
    // for NegativeBinomialInterval's own swap (a numeric mean of 7.5 against a claimed 0.0333). Runs on every
    // (n, p, rho) BetaBinomialInterval's own scipy-checked cases already cover, so it never depends on a
    // separately hand-typed expected value.
    [Theory]
    [InlineData(10L, 0.3, 0.0)]
    [InlineData(10L, 0.3, 0.2)]
    [InlineData(100L, 0.05, 0.0)]
    [InlineData(100L, 0.05, 0.1)]
    [InlineData(2000L, 0.002, 0.0006)]
    [InlineData(10983L, 0.001, 0.0006054)]
    [InlineData(1256L, 0.00875796178, 0.02138)]
    [InlineData(50L, 0.5, 0.5)]
    [InlineData(5L, 0.9, 0.0)]
    [InlineData(1L, 0.5, 0.3)]
    public void PmfArrayOwnReconstructedMeanMatchesNTimesP(long n, double p, double rho)
    {
        var pmf = BetaBinomialPredictive.Pmf(n, p, rho);

        var totalMass = 0.0;
        var reconstructedMean = 0.0;
        for (var k = 0; k < pmf.Length; k++)
        {
            totalMass += pmf[k];
            reconstructedMean += k * pmf[k];
        }

        Assert.True(Math.Abs(totalMass - 1.0) < 1e-6, $"the pmf array must sum to 1; got {totalMass}.");

        var expectedMean = n * p;
        var tolerance = Math.Max(1e-6, Math.Abs(expectedMean) * 1e-6);
        Assert.True(
            Math.Abs(reconstructedMean - expectedMean) < tolerance,
            $"reconstructed mean {reconstructedMean} does not match n*p = {expectedMean} — a swapped " +
            "consecutive-term ratio breaks exactly this identity (this node's BOOT.md, 'Fable 5.1 decision V').");
    }

    [Fact]
    public void DegenerateWhenPIsOne()
    {
        var (mean, low, high, attainedAlpha) = BetaBinomialPredictive.Interval(100L, 1.0, 0.1, 0.05);
        Assert.Equal(100.0, mean);
        Assert.Equal(100.0, low);
        Assert.Equal(100.0, high);
        Assert.Equal(0.0, attainedAlpha);
    }
}
