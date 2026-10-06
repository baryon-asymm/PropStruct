namespace PropStruct.Tests.Harness;

/// <summary>
/// The count predictive's half-width in value units (this node's BOOT.md, ## Invariants, "Count-like cells").
/// Split out of <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class CountFloor
{
    // this node's BOOT.md, ## Invariants, "Count-like cells", and `tests/Harness/HISTORY.md#per-run-quantum`: a
    // printed distribution-function cell is `count / (total * step)`, and `total` is the one run's own (its own
    // number of pockets, particles or bridges), so the quantum is a property of one run's array, inferred from
    // that run alone (never pooled across runs — that entry's own measured reason the earlier sibling-pool
    // design failed). Once every contributing replica's own value is converted to an integer count
    // via its own run's quantum, the counts (dimensionless, comparable across replicas despite each replica's own
    // normalization) feed the same negative-binomial predictive interval as before (r = sum(counts) + 1/2,
    // p = R/(R+1), `tests/Harness/HISTORY.md#criterion-revision-2026-09-17`, step 3); the wider of its two one-sided half-widths,
    // converted back to value units through the *candidate's own* quantum (the unit the candidate's value is
    // itself judged in), is the returned floor. The `MaxCountFloorTotal` cap bounds every `Compare` call's total
    // iteration count regardless of how many distribution-function cells a formulation prints
    // (`tests/Harness/HISTORY.md#criterion-revision-2026-09-17`, step 3, notes a cell this populated no longer needs the negative-binomial search: its
    // own replica-to-replica spread already dominates the ordinary Student band).
    internal const double MaxCountFloorTotal = 2000.0;

    // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`: Fable 5.1's
    // own threshold, machine-applied per cell from each run's own reconstructed count, not tuned to this tree's
    // own data. internal, not private: `MassFamilyRule`'s own bracket and `EvaluateCells`'s own
    // `countPredictedMeanValue` gate both reuse this same regime boundary, never a second one.
    internal const double HeavyTailRegimeThreshold = 30.0;

    // `AttainedAlpha` is null when the cap above short-circuits the search (`MaxCountFloorTotal`): there is no
    // [low, high] boundary to report an attained level for, and the ordinary Student band governs the cell
    // instead (`tests/Harness/HISTORY.md#criterion-revision-2026-09-17`, step 3's own reasoning for the cap). `Low`/`High`
    // (`tests/Harness/HISTORY.md#decision-vi-full-reasoning`) are the same [low, high] boundary in raw
    // count units, carried out alongside the value-unit `Floor` for the eligibility test — never recomputed a
    // second time from `Floor`, which has already lost the count/value distinction by multiplying through the
    // candidate's own quantum. `TailProbability` (decision VI, item 5's own second cheap check) is the exact
    // two-sided predictive probability of a value at least as extreme as `observedCount`, from the identical
    // `NB(r, p)` this floor is already built from — `null` under the same cap that leaves `AttainedAlpha` null.
    // `PredictedMean` (`tests/Harness/HISTORY.md#count-floor-boundary-centring-defect`)
    // is the count-space predictive mean `NegativeBinomialPredictive.Interval` already computes the floor's own
    // half-width from (`r * q / p`), carried out in the same count units as `Low`/`High` — `null` under the same
    // cap.
    internal static (double Floor, double? AttainedAlpha, double? Low, double? High, double? TailProbability, double? PredictedMean) CountFloorFromCounts(
        IReadOnlyList<double> counts, double candidateQuantum, double alpha, double observedCount)
    {
        var total = 0.0;
        foreach (var count in counts)
        {
            total += count;
        }

        if (total > MaxCountFloorTotal)
        {
            return (0.0, null, null, null, null, null);
        }

        var (predictedMean, low, high, attainedAlpha) = NegativeBinomialPredictive.Interval(total, counts.Count, alpha);
        var (cdfAtMost, pmfAt) = NegativeBinomialPredictive.CdfAtMost(observedCount, total, counts.Count);
        var tailProbability = PredictiveTail.TwoSided(cdfAtMost, pmfAt);

        // The predictive interval is built around its own analytic mean (`predictedMean`), not the sample mean
        // of this specific cell's replica counts; re-centering the returned floor on the sample mean would let
        // the two means silently cancel a real shift. Both one-sided half-widths are measured from
        // `predictedMean`, and the floor is the wider of the two, converted back to value units through the
        // candidate's own quantum. `predictedMean` itself is returned too
        // (`tests/Harness/HISTORY.md#count-floor-boundary-centring-defect`): a half-width measured from `predictedMean`
        // only reconstructs `[low, high]` when the comparison it feeds is *also* centred on `predictedMean`: the
        // caller must not apply this width around a different centre (the replicas' own sample mean), which is
        // exactly the defect that section fixes.
        return (Math.Max(high - predictedMean, predictedMean - low) * candidateQuantum, attainedAlpha, low, high, tailProbability, predictedMean);
    }
}
