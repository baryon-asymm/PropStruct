namespace PropStruct.Tests.Harness;

/// <summary>
/// The family-wise Bonferroni budget (<c>m</c>, <c>Alpha / m</c>) and the reduction of <see cref="CellVerdict"/>s
/// to a <see cref="CriterionReport"/> — shared by <see cref="StatisticalCriterion.Compare"/>,
/// <see cref="StatisticalCriterion.CompareTailRowMean"/> and <see cref="StatisticalCriterion.
/// CompareAdaptiveIndexMatched"/>, each its own family. Split out of <c>StatisticalCriterion.Comparisons.cs</c>
/// (decomposition, 2026-09-26); <see cref="Of"/> is the former <c>Finalize</c>, renamed since "Finalize" reads as
/// the GC hook (CA1716/CA1724).
/// </summary>
internal static class FamilyWiseReport
{
    // The threshold formula shared by every comparison this node exposes (`Compare`, `CompareTailRowMean`,
    // `CompareAdaptiveIndexMatched`): `m`/`perQuantityAlpha` are computed once over the pending cells of the
    // specific report being finalized (this node's BOOT.md, ## Invariants, "`m` and `t` are computed once per
    // `Compare` call, not per cell") — each of the three reports is its own family-wise-corrected family, the
    // same way `TwoSampleBias` computes its own `m` separately from `Compare`'s. Written once so that the
    // Student-band/count-floor/print-resolution-guard formula exists exactly once in this node (root BOOT.md
    // Taboos: "no second implementation of any part of ... a formula"). The calibration curve needs the *raw*
    // per-cell alpha instead of `Alpha / m`; it calls `CellEvaluator.EvaluateCells` directly (`CompareCellVerdicts`
    // and its two siblings), never through `Of`, which always uses the family-wise `Alpha / m`.
    internal static CriterionReport Of(List<PendingCell> pending, int excluded)
    {
        var m = pending.Count;
        var perQuantityAlpha = StatisticalCriterion.Alpha / Math.Max(m, 1);
        var verdicts = CellEvaluator.EvaluateCells(pending, perQuantityAlpha);
        var failures = new List<CriterionFailure>(verdicts.Count);
        foreach (var verdict in verdicts)
        {
            if (verdict.Failed)
            {
                failures.Add(new CriterionFailure(
                    verdict.Name, verdict.Index, verdict.CandidateValue, verdict.Mean, verdict.Threshold,
                    verdict.RegionLow, verdict.RegionHigh));
            }
        }

        return new CriterionReport(m, excluded, failures);
    }
}
