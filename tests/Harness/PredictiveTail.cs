namespace PropStruct.Tests.Harness;

/// <summary>
/// The exact two-sided predictive tail probability of an observed count, shared by
/// <see cref="NegativeBinomialPredictive"/> and <see cref="BetaBinomialPredictive"/>
/// (`tests/Harness/HISTORY.md#decision-vi-full-reasoning`, item 5). Split out of <c>StatisticalCriterion.Numerics.cs</c>
/// (decomposition, 2026-09-26).
/// </summary>
internal static class PredictiveTail
{
    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`, item 5: the exact two-sided predictive
    // probability of a value at least as extreme as the one observed — `2 * min(P(X <= k), P(X >= k))`, capped at
    // `1` — from the same `(CdfAtMost, PmfAt)` pair either count predictive already returns (`P(X >= k) =
    // 1 - P(X <= k) + pmf(k)`, so no second, open-ended upper-tail summation is needed). Shared by both
    // predictives, never a second implementation of the same two-line formula.
    internal static double TwoSided(double cdfAtMost, double pmfAt)
    {
        // A `cdfAtMost` summed from thousands of pmf terms can drift a few ULPs past `1.0` (every term is
        // nonnegative, so this is summation rounding, never a sign of a genuinely invalid CDF) — clamped here
        // before the subtraction below, rather than left to turn `upperTail` spuriously negative.
        var clampedCdf = Math.Clamp(cdfAtMost, 0.0, 1.0);
        var upperTail = Math.Max(0.0, 1.0 - clampedCdf + pmfAt);
        return Math.Min(1.0, 2.0 * Math.Min(clampedCdf, upperTail));
    }
}
