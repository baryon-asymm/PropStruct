namespace PropStruct.Tests.Harness;

/// <summary>
/// `tests/Harness/HISTORY.md#decision-vi-full-reasoning`: whether some value this cell's own
/// quantity could actually take, printable in its own format, lies outside this cell's own interval at the
/// level it was judged at — an interval that already covers the quantity's whole attainable range says nothing
/// about calibration, however it happened to be constructed, and diluting the pooled cell count with it hides a
/// real miscalibration rather than measuring one. Split out of <c>StatisticalCriterion.IntervalRules.cs</c>
/// (decomposition, 2026-09-26); one caller, <see cref="CellEvaluator"/>.
/// </summary>
internal static class CellEligibility
{
    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`: eligibility is computed per rule,
    // per cell, at this same `perQuantityAlpha` — never folded into `degenerate`, a different,
    // alpha-independent question. `m`/`Compare`'s own `Alpha / m` Bonferroni budget reads none of this:
    // an ineligible cell was already charged its share of `m` the moment `BuildComparePending` built its
    // interval, so this flag never touches that count (decision VI, item 4).
    internal static bool Of(CalibrationRule rule, PendingCell cell, double threshold, double? countLow, double? countHigh)
    {
        if (rule == CalibrationRule.Count)
        {
            // "some value it could actually take ... lies outside its own interval": the array/row's own
            // reconstructed total (`CandidateTotal`, `n*`) bounds every cell's own count from above by a
            // physical argument that does not depend on which predictive built `[lo, hi]` — no bin of a
            // partition can hold more count than the partition's own total — so it still applies unchanged
            // under "Decision XX"'s routing, which reads `CandidateTotal` nowhere else any more. An interval
            // already covering `[0, n]` says nothing. `CandidateTotal` is absent only when
            // this branch could not have been reached at all (it is required to reach `Count` classification
            // in the first place), so the fallback below never actually fires; it exists so a future caller
            // that reaches `Count` some other way fails open (eligible), not silently excluded.
            return cell.CandidateTotal is not { } n || n <= 0.0 || countLow is not { } lo || countHigh is not { } hi
                || lo > 0.0 || hi < n;
        }

        // Student rule: `tests/Harness/HISTORY.md#decision-vi-full-reasoning`, the three computed cases. `Sd == 0` (case 3)
        // is checked first and alone decides it — `staticFloor` already includes `cell.Resolution`, so
        // whenever the replicas are all equal, `threshold >= staticFloor >= cell.Resolution` always holds
        // and the band always covers the next printable value on both sides, exactly the condition case 3
        // names; the remaining cases only apply to a cell some replica spread could still move.
        if (cell.Sd == 0.0)
        {
            return false;
        }

        if (double.IsNaN(threshold) || double.IsPositiveInfinity(threshold))
        {
            return false; // case 1: an infinite or NaN threshold covers everything printable.
        }

        var lowEnd = cell.Mean - threshold;
        var highEnd = cell.Mean + threshold;
        if (QuantityFamilies.IsFractionFamily(cell.Name))
        {
            // case 2, the fraction sub-case: every cell of a normalized "quantity fraction" family
            // lies in [0, 1] by construction (its own header, above).
            return !(lowEnd <= 0.0 && highEnd >= 1.0);
        }

        // case 2, the non-negative sub-case: whether the quantity is non-negative is read off
        // this cell's own observed data (candidate, mean, every replica), not asserted for the
        // whole family — this node's own free-format printer (`ResultsMFile`, list-directed, no
        // fixed field width) puts no finite cap on a non-negative quantity's own upper end, so
        // "the largest printable value of its field" is only ever exceeded when the band's own
        // upper end is genuinely unbounded (`double.IsPositiveInfinity`), not some fabricated
        // finite width this format does not actually have. A cell that has ever shown a negative
        // value (e.g. `AdaptiveLastBoundaryLog`, a boundary size's own natural logarithm, which
        // can be negative) is left eligible here: nothing computed establishes a lower bound for
        // it, so this test does not presume one.
        var empiricallyNonNegative = cell.Mean >= 0.0 && cell.CandidateValue >= 0.0
            && Array.TrueForAll(cell.ReplicaValues, v => v >= 0.0);
        return !(empiricallyNonNegative && lowEnd <= 0.0 && double.IsPositiveInfinity(highEnd));
    }
}
