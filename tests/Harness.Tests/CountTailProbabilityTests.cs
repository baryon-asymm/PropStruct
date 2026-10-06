using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// `tests/Harness/HISTORY.md#decision-vi-full-reasoning`, item 5: <see
/// cref="NegativeBinomialPredictive.CdfAtMost"/>, <see cref="BetaBinomialPredictive.CdfAtMost"/>
/// and <see cref="PredictiveTail.TwoSided"/> are the primitives the dilution-vs-mis-
/// specification diagnostic needs (a per-cell exact predictive p-value, not the fixed-alpha
/// <c>AttainedAlpha</c> this node already computes). They are cross-checked here against the already-verified
/// <see cref="NegativeBinomialPredictive.Interval"/>/<see cref="BetaBinomialPredictive.Interval"/>
/// themselves (an internal consistency identity: the boundary `[low, high]` those functions search for is, by
/// construction, the smallest interval whose own endpoints' CDF values straddle `alpha / 2` and `1 - alpha / 2`),
/// not against a second, independent implementation — the identity below is exactly the property that would break
/// if the walk and this file's own new point-CDF read the pmf differently, the same class of defect ("a formula
/// defect, not only a numerical one") this node's BOOT.md has already found twice in this file's own interval
/// searches.
/// </summary>
public class CountTailProbabilityTests
{
    [Theory]
    [InlineData(0.0, 15, 0.05)]
    [InlineData(11.0, 5, 0.05)]
    [InlineData(443.0, 32, 0.05)]
    [InlineData(0.0, 1, 0.01)]
    [InlineData(1200.0, 16, 0.001)]
    public void NegativeBinomialCdfAtMostMatchesTheIntervalSearchAtItsOwnBoundary(double totalCount, int replicaCount, double alpha)
    {
        var (_, low, high, _) = NegativeBinomialPredictive.Interval(totalCount, replicaCount, alpha);

        var (cdfAtHigh, _) = NegativeBinomialPredictive.CdfAtMost(high, totalCount, replicaCount);
        Assert.True(cdfAtHigh >= 1.0 - alpha / 2.0 - 1e-9, $"CDF at high ({cdfAtHigh}) should reach 1 - alpha/2 ({1.0 - alpha / 2.0}).");

        if (low > 0.0)
        {
            var (cdfBelowLow, _) = NegativeBinomialPredictive.CdfAtMost(low - 1.0, totalCount, replicaCount);
            Assert.True(cdfBelowLow < alpha / 2.0 + 1e-9, $"CDF just below low ({cdfBelowLow}) should be under alpha/2 ({alpha / 2.0}).");
        }

        var (cdfAtLow, _) = NegativeBinomialPredictive.CdfAtMost(low, totalCount, replicaCount);
        Assert.True(cdfAtLow >= alpha / 2.0 - 1e-9, $"CDF at low ({cdfAtLow}) should reach alpha/2 ({alpha / 2.0}).");
    }

    [Theory]
    [InlineData(11L, 5L, 0.4, 0.15, 0.05)]
    [InlineData(0L, 100L, 0.02, 0.0, 0.05)]
    [InlineData(2L, 10983L, 0.001, 0.0006054, 0.001)]
    public void BetaBinomialCdfAtMostMatchesTheIntervalSearchAtItsOwnBoundary(long observed, long n, double p, double rho, double alpha)
    {
        var (_, low, high, _) = BetaBinomialPredictive.Interval(n, p, rho, alpha);

        var (cdfAtHigh, _) = BetaBinomialPredictive.CdfAtMost((long)high, n, p, rho);
        Assert.True(cdfAtHigh >= 1.0 - alpha / 2.0 - 1e-9, $"CDF at high ({cdfAtHigh}) should reach 1 - alpha/2 ({1.0 - alpha / 2.0}).");

        if (low > 0.0)
        {
            var (cdfBelowLow, _) = BetaBinomialPredictive.CdfAtMost((long)low - 1, n, p, rho);
            Assert.True(cdfBelowLow < alpha / 2.0 + 1e-9, $"CDF just below low ({cdfBelowLow}) should be under alpha/2 ({alpha / 2.0}).");
        }

        // `observed` is only used to keep the theory data self-documenting about which cell each row came from;
        // the identity checked is about `low`/`high` themselves, not about a specific observed count.
        _ = observed;
    }

    // The two-sided tail probability of the distribution's own mean-nearest value must read close to 1 (nothing
    // is extreme about the most typical outcome); a value far past the boundary the interval search itself found
    // at a loose alpha must read a small, non-degenerate probability — the two sanity ends of the identity
    // `CountTwoSidedTailProbability` is built to report.
    [Fact]
    public void CountTwoSidedTailProbabilityIsNearOneAtTheModeAndSmallFarInTheTail()
    {
        var (cdfAtMode, pmfAtMode) = NegativeBinomialPredictive.CdfAtMost(0.0, 0.0, 15);
        var pAtMode = PredictiveTail.TwoSided(cdfAtMode, pmfAtMode);
        Assert.True(pAtMode > 0.9, $"the most typical count's own tail probability ({pAtMode}) should read close to 1.");

        var (cdfFarOut, pmfFarOut) = NegativeBinomialPredictive.CdfAtMost(500.0, 0.0, 15);
        var pFarOut = PredictiveTail.TwoSided(cdfFarOut, pmfFarOut);
        Assert.True(pFarOut < 0.01, $"a count far into the tail should read a small tail probability, got {pFarOut}.");
        Assert.True(pFarOut >= 0.0, "a tail probability is never negative.");
    }

    [Fact]
    public void CountTwoSidedTailProbabilityNeverExceedsOne()
    {
        // cdfAtMost == upperTail == (1 + pmfAt) / 2 (both tails equal) makes the raw `2 * min(...)` formula read
        // `1 + pmfAt`, past 1 whenever `pmfAt > 0`; the cap is asserted directly against this exact corner, not
        // inferred from a real cell, since no real cell is guaranteed to land on it.
        var p = PredictiveTail.TwoSided(0.7, 0.4);
        Assert.Equal(1.0, p);
    }

    // ⚠ 2026-09-20: found live, by this file's own first run against a real (totalCount, replicaCount) pair whose
    // true CDF is close to 1 (`NegativeBinomialCdfAtMost(500, 0, 15)`): summing thousands of nonnegative pmf
    // terms drifted `cdfAtMost` a few ULPs past `1.0` (`1.0000000000000007`), which made the raw `upperTail =
    // 1 - cdfAtMost + pmfAt` formula read slightly negative and the two-sided probability come out negative too —
    // fixed by clamping `cdfAtMost` to `[0, 1]` before the subtraction (`CountTwoSidedTailProbability`'s own
    // comment). Guarded directly against the exact input that found it.
    [Fact]
    public void CountTwoSidedTailProbabilityToleratesFloatingPointDriftPastOne()
    {
        var (cdf, pmf) = NegativeBinomialPredictive.CdfAtMost(500.0, 0.0, 15);
        Assert.True(cdf >= 1.0, "this regression guard needs the exact drift-past-one input that found the defect.");
        var p = PredictiveTail.TwoSided(cdf, pmf);
        Assert.True(p is >= 0.0 and <= 1.0, $"expected a valid probability, got {p}.");
    }
}
