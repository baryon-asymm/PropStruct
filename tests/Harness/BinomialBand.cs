namespace PropStruct.Tests.Harness;

/// <summary>
/// The exact highest-density region of <c>Binomial(n, p)</c>, used by the calibration curve
/// (`tests/Harness/HISTORY.md#fable-5-1-decision-ii`) and, at <c>rho = 0</c>, as
/// <see cref="BetaBinomialPredictive"/>'s own documented ordinary-binomial special case. Split out of
/// <c>StatisticalCriterion.Numerics.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class BinomialBand
{
    /// <summary>
    /// `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: the shortest two-sided
    /// region of <c>Binomial(n, p)</c> carrying at least probability <c>1 - confidenceAlpha</c> — a
    /// highest-density region, not an equal-tailed one, chosen because it is the region a mode-anchored recursion
    /// can build without ever evaluating a term far from the mode: starting from the mode and always absorbing
    /// whichever neighbouring cell (one step further down, or one step further up) currently has the larger
    /// probability keeps the running total's own next candidate terms close to what has already been added, so
    /// the walk never needs a term whose magnitude is wildly different from the total it is being added to. An
    /// equal-tailed interval, by contrast, would need each tail's own boundary probability compared against
    /// <c>confidenceAlpha / 2</c> directly, which for this curve's own <c>n</c> (tens of thousands of
    /// non-degenerate cells at <c>alpha = 0.05</c>) sits many orders of magnitude below the mode's own probability
    /// — reaching it from the mode by accumulating the *intervening* mass first would demand knowing the mode's
    /// own full one-sided tail mass in advance, which is exactly the quantity this construction avoids computing.
    ///
    /// The mode's own probability is computed once, in log space (<see cref="StudentDistribution.LogGamma"/>, the
    /// same log-gamma the Student quantile already uses — root BOOT.md Taboos: "no second implementation of any
    /// part of ... a formula"); every further term is reached by the ordinary consecutive-term ratio (the same
    /// technique <see cref="NegativeBinomialPredictive.Interval"/> already uses), which stays a normal, positive
    /// <c>double</c> throughout because the walk never leaves the region the running total already covers.
    /// </summary>
    internal static (long Low, long High) HighestDensityRegion(long n, double p, double confidenceAlpha)
    {
        if (n <= 0 || p <= 0.0)
        {
            return (0, 0);
        }

        if (p >= 1.0)
        {
            return (n, n);
        }

        var q = 1.0 - p;
        var mode = Math.Clamp((long)Math.Floor((n + 1) * p), 0L, n);

        var logModePmf = StudentDistribution.LogGamma(n + 1) - StudentDistribution.LogGamma(mode + 1)
            - StudentDistribution.LogGamma(n - mode + 1) + mode * Math.Log(p) + (n - mode) * Math.Log(q);
        var modePmf = Math.Exp(logModePmf);

        var low = mode;
        var high = mode;
        var lowerPmf = modePmf;
        var upperPmf = modePmf;
        var totalMass = modePmf;
        var targetMass = 1.0 - confidenceAlpha;

        while (totalMass < targetMass && (low > 0 || high < n))
        {
            // pmf(low - 1) / pmf(low) = (low * q) / ((n - low + 1) * p); pmf(high + 1) / pmf(high) =
            // ((n - high) * p) / ((high + 1) * q) — the same consecutive-term ratio in each direction.
            var candidateLowerPmf = low > 0 ? lowerPmf * (low * q) / ((n - low + 1) * p) : 0.0;
            var candidateUpperPmf = high < n ? upperPmf * (n - high) * p / ((high + 1) * q) : 0.0;

            if (candidateUpperPmf >= candidateLowerPmf)
            {
                high++;
                upperPmf = candidateUpperPmf;
                totalMass += upperPmf;
            }
            else
            {
                low--;
                lowerPmf = candidateLowerPmf;
                totalMass += lowerPmf;
            }
        }

        return (low, high);
    }
}
