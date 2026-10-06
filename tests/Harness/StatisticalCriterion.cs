namespace PropStruct.Tests.Harness;

/// <summary>One failing cell of a <see cref="StatisticalCriterion.Compare"/> report. Every cell is judged by
/// <c>|Value - Mean| &gt; Threshold</c>, the symmetric band: decision XI, which would have judged a Count-governed
/// cell against its own count region instead, was withdrawn by its own condition
/// (`tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`). <paramref name="RegionLow"/> and <paramref name="RegionHigh"/> report that region, in value
/// units, beside the band that actually decided the cell, because the two are not the same region and the
/// difference between them is a standing finding of this node: the band is centred on the replicas' own mean and
/// the region on the model's, and the withdrawal measured cases where the model's region does not contain the
/// replicas' mean at all. They are <c>null</c> for every cell with no such region.</summary>
public sealed record CriterionFailure(string Quantity, int Index, double Value, double Mean, double Threshold, double? RegionLow = null, double? RegionHigh = null);

/// <summary><paramref name="Compared"/> is <c>m</c> of the root criterion. <paramref name="Excluded"/> counts
/// cells dropped from comparison entirely: today, by <c>exclusions.json</c> (which zeroes cells rather than
/// dropping them, so it never contributes) and by the canonical-axis rule below, when a category a run's own
/// adaptive binning never produced leaves fewer than two contributing replicas for a cell (this node's BOOT.md,
/// ## Invariants, "Canonical category axis").</summary>
public sealed record CriterionReport(int Compared, int Excluded, IReadOnlyList<CriterionFailure> Failures);

/// <summary>One printed cell of a <see cref="StatisticalCriterion.TwoSampleBias"/> report: <paramref name="T"/>
/// and <paramref name="DegreesOfFreedom"/> are Welch's, and <paramref name="Differs"/> is <c>|T|</c> against the
/// family-wise-corrected critical value (this node's BOOT.md, ## Invariants, "Two-sample bias").</summary>
public sealed record TwoSampleCell(string Quantity, int Index, double MeanFirst, double MeanSecond, double T, int DegreesOfFreedom, bool Differs);

/// <summary>
/// The root criterion of BOOT.md, "Statistical reference criterion": every cell of a candidate result is
/// compared against the mean and standard deviation of the formulation's <c>R</c> replicas of the requested
/// <see cref="ReplicaKind"/>, with a threshold of <c>max(t * sd_R * sqrt(1 + 1/R), r_q, p_q, c_q)</c> (<c>c_q</c>
/// is the count floor of this node's BOOT.md, ## Invariants, "Count-like cells"). Holds no tolerance of its own
/// (this node's BOOT.md, ## Invariants): the replicas and the reference come from <c>tests/Fixtures</c> on every
/// call, computed at test time.
///
/// This is the node's public entry point: five public members (<see cref="Compare"/>,
/// <see cref="CompareTailRowMean"/>, <see cref="CompareAdaptiveIndexMatched"/>, <see cref="TwoSampleBias"/>,
/// <see cref="TwoSampleBiasOfSets"/>) plus three internal calibration-curve seams
/// (<see cref="CompareCellVerdicts"/>, <see cref="CompareTailRowMeanCellVerdicts"/>,
/// <see cref="CompareAdaptiveIndexMatchedCellVerdicts"/>), each a thin forward into its own named class
/// (decomposition, 2026-09-26, replacing the "one partial class, several files" split of audit finding R3,
/// 2026-09-25). <see cref="ReferenceComparison"/> (dispatching to <see cref="CanonicalAxisCells"/>/
/// <see cref="OrdinaryQuantityCells"/>), <see cref="TailRowMeanComparison"/> and
/// <see cref="AdaptiveIndexMatchedComparison"/> build each comparison's own <see cref="PendingCell"/> list;
/// <see cref="CellEvaluator"/> (with <see cref="CellEligibility"/>) turns it into <see cref="CellVerdict"/>s at a
/// given alpha; <see cref="FamilyWiseReport"/> reduces those to a <see cref="CriterionReport"/> at the
/// family-wise <c>Alpha / m</c>. <see cref="TwoSampleComparison"/> is the parallel branch behind
/// <see cref="TwoSampleBias"/>/<see cref="TwoSampleBiasOfSets"/>. <see cref="FixtureReplicas"/>/
/// <see cref="ExclusionRules"/> load replicas, the reference and <c>exclusions.json</c> from
/// <c>tests/Fixtures</c>; every other class (<see cref="QuantityFamilies"/>, <see cref="CategoryAxis"/>,
/// <see cref="PrintResolution"/>, <see cref="RunQuantum"/>, <see cref="CountReconstruction"/>,
/// <see cref="DispersionEstimator"/>, <see cref="CellRho"/>, <see cref="CountFloor"/>,
/// <see cref="MassFamilyRule"/>, <see cref="NegativeBinomialPredictive"/>, <see cref="BetaBinomialPredictive"/>,
/// <see cref="BinomialBand"/>, <see cref="PredictiveTail"/>, <see cref="SampleSpread"/>) is a leaf the pipeline
/// above calls into. <c>tests/Harness/API.md</c> has the full decomposition, class by class.
/// </summary>
public static class StatisticalCriterion
{
    // internal, not private: `tests/Harness/HISTORY.md#fable-5-1-decision-ii` reuses this exact constant as the
    // confidence of `BinomialBand`'s own region, rather than choosing a second
    // significance level for the calibration curve (decision II's own "no change of alpha", honoured by never
    // introducing a second number, not merely by leaving this one unedited).
    internal const double Alpha = 1e-3;

    /// <summary>
    /// Compares <paramref name="candidate"/> against the <c>R</c> replicas of <paramref name="formulation"/> and
    /// <paramref name="kind"/> (<c>tests/Fixtures/replicas-&lt;kind&gt;/&lt;formulation&gt;</c>), using the
    /// reference (<c>tests/Fixtures/references/&lt;formulation&gt;</c>) only for each cell's print resolution and
    /// for the cells the exclusion rule names. <paramref name="excludeReplicaOrdinal"/>
    /// (`tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 1, "Blind calibration") drops one
    /// lagged/independent replica (1-based, the same
    /// numbering as its file name) from the <c>R</c> loaded, so a test can put that replica itself in the
    /// <paramref name="candidate"/> role and compare it against the other <c>R - 1</c> without it appearing on
    /// both sides at once; <c>null</c> (the default) loads all <c>R</c>, exactly as before this parameter existed.
    /// </summary>
    public static CriterionReport Compare(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal = null)
    {
        var (pending, excluded) = ReferenceComparison.BuildComparePending(formulation, candidate, kind, excludeReplicaOrdinal);
        return FamilyWiseReport.Of(pending, excluded);
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: the calibration curve
    // needs the same cells `Compare` builds, but classified by `EvaluateCells` at one of the three fixed levels
    // 0.05/0.01/0.001 rather than reduced to a `CriterionReport` — the "raw per-cell alpha" idiom this node used
    // for the 0.05 diagnostic this curve replaces (`CompareWithAlpha`, removed with that diagnostic), applied to
    // the richer verdict instead of just pass/fail. Never a second implementation of `Compare`'s own
    // cell-building: both call `ReferenceComparison.BuildComparePending`.
    internal static IReadOnlyList<CellVerdict> CompareCellVerdicts(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, double alpha, int? excludeReplicaOrdinal = null)
    {
        var (pending, _) = ReferenceComparison.BuildComparePending(formulation, candidate, kind, excludeReplicaOrdinal);
        return CellEvaluator.EvaluateCells(pending, alpha);
    }

    /// <summary>
    /// `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 2: compares one Ndok-length "tail row mean"
    /// vector per source — the unweighted mean of <paramref name="candidate"/>'s (and each replica's) own
    /// <c>fqdokkarm(&lt;row&gt;,:)</c> rows from the first adaptive row (past <c>i0</c>, the reference's own
    /// prefix length, one fixed cut for every source — F-b, `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`:
    /// not each source's own) to that source's own last row — instead of leaving the whole
    /// adaptive tail out of <see cref="Compare"/>'s canonical-axis exclusion. Kept beside the canonical-axis rule,
    /// not a replacement of it (`tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 5): it never changes
    /// <see cref="Compare"/>'s own <see cref="CriterionReport.Compared"/>/<see cref="CriterionReport.Excluded"/>
    /// counts, and is its own family-wise-corrected report. <paramref name="excludeReplicaOrdinal"/> is the same
    /// blind-calibration seam as <see cref="Compare"/>'s.
    /// </summary>
    public static CriterionReport CompareTailRowMean(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal = null)
    {
        var (pending, excluded) = TailRowMeanComparison.BuildTailRowMeanPending(formulation, candidate, kind, excludeReplicaOrdinal);
        return FamilyWiseReport.Of(pending, excluded);
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: the calibration curve's own seam into this report, mirroring
    // `CompareCellVerdicts` above — never a second implementation of `CompareTailRowMean`'s own cell-building.
    internal static IReadOnlyList<CellVerdict> CompareTailRowMeanCellVerdicts(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, double alpha, int? excludeReplicaOrdinal = null)
    {
        var (pending, _) = TailRowMeanComparison.BuildTailRowMeanPending(formulation, candidate, kind, excludeReplicaOrdinal);
        return CellEvaluator.EvaluateCells(pending, alpha);
    }

    /// <summary>
    /// `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 3: compares <c>Dkarmcat</c>/<c>dokkarm43</c>/
    /// <c>dokkarm10</c> at adaptive index <c>i - i0</c> (<c>i0</c> the reference's own prefix length, one fixed
    /// cut for every source — F-b, `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`: not each source's
    /// own) up to the shortest of <paramref name="candidate"/>'s own adaptive
    /// length and every contributing replica's — never past a length one side does not have, so this stays a
    /// same-physical-slot comparison and not a repeat of the index-misalignment failure the canonical-axis rule
    /// was built to close (this node's BOOT.md, ## Invariants, "Canonical category axis") — plus two scalars,
    /// <c>AdaptiveRowCount</c> (candidate's own adaptive row count, from <c>Dkarmcat</c>'s length) and
    /// <c>AdaptiveLastBoundaryLog</c> (the natural log of candidate's own last <c>Dkarmcat</c> entry, compared on
    /// a log scale because the boundary itself is a skewed extreme-value statistic: this node's BOOT.md, ## Tail
    /// coverage (2026-09-18), step 3). Kept beside the canonical-axis rule, not a replacement of it: never
    /// changes <see cref="Compare"/>'s own counts, and is its own family-wise-corrected report. A row past the
    /// shortest of the candidate's and the contributing replicas' own adaptive length is never scored — it counts
    /// toward the report's own <c>Excluded</c> instead (`tests/Harness/HISTORY.md#e2-adaptive-common-range`).
    /// </summary>
    public static CriterionReport CompareAdaptiveIndexMatched(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal = null)
    {
        var (pending, excluded) = AdaptiveIndexMatchedComparison.BuildAdaptiveIndexMatchedPending(formulation, candidate, kind, excludeReplicaOrdinal);
        return FamilyWiseReport.Of(pending, excluded);
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: the calibration curve's own seam into this report, mirroring
    // `CompareCellVerdicts` above — never a second implementation of `CompareAdaptiveIndexMatched`'s own
    // cell-building.
    internal static IReadOnlyList<CellVerdict> CompareAdaptiveIndexMatchedCellVerdicts(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, double alpha, int? excludeReplicaOrdinal = null)
    {
        var (pending, _) = AdaptiveIndexMatchedComparison.BuildAdaptiveIndexMatchedPending(formulation, candidate, kind, excludeReplicaOrdinal);
        return CellEvaluator.EvaluateCells(pending, alpha);
    }

    /// <summary>
    /// The two-sample bias of <paramref name="first"/> against <paramref name="second"/> (this node's BOOT.md,
    /// ## Invariants, "Two-sample bias"): a Welch two-sample t-test at every printed cell that both replica sets
    /// of <paramref name="formulation"/> reach, family-wise corrected over the cells actually compared (the same
    /// <c>alpha = 10^-3</c> per formulation as <see cref="Compare"/>). Unlike <see cref="Compare"/>, a cell absent
    /// from one member of a set simply drops that member from the set's own mean and standard deviation — there
    /// is no reference token to fall back on for a print-resolution floor, and none is needed: this is a plain
    /// two-sample comparison of two replica sets to each other, not of a candidate against replicas.
    /// </summary>
    public static IReadOnlyList<TwoSampleCell> TwoSampleBias(string formulation, ReplicaKind first, ReplicaKind second) =>
        TwoSampleComparison.Bias(formulation, first, second);

    /// <summary>
    /// <see cref="TwoSampleBias"/>'s own body, taking both replica sets already loaded instead of loading them
    /// itself from <c>tests/Fixtures</c> — so, unlike <see cref="Compare"/>/<see cref="CompareTailRowMean"/>/
    /// <see cref="CompareAdaptiveIndexMatched"/>, which take the candidate as a parameter, a caller with no
    /// fixture file at all (two sets of the port's own runs, for instance) still has a seam. Public since
    /// 2026-09-23 (this node's BOOT.md, "Two-sample bias"), for <c>tests/Simulation.Tests</c>'s own
    /// reference-vs-batched comparison (<c>Simulation.Tests/BOOT.md</c>, link 3) as well as this node's own
    /// oracle-mutation sweeps (`tests/Harness/HISTORY.md#oracle-mutation-2026-09-19`), which clone one member of a fixture set
    /// (the same "clone a real parse and overwrite the cells it needs" idiom this node's own tests already use
    /// for <see cref="Compare"/>) without fabricating a fixture file. Carries the same limits as
    /// <see cref="TwoSampleBias"/>: no canonical-axis or count-floor handling, so a caller whose two sets
    /// include a category-indexed quantity (an index whose meaning can shift between runs) must account for
    /// that itself.
    /// </summary>
    public static IReadOnlyList<TwoSampleCell> TwoSampleBiasOfSets(
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> firstSet, IReadOnlyList<IReadOnlyDictionary<string, double[]>> secondSet)
    {
        ArgumentNullException.ThrowIfNull(firstSet);
        ArgumentNullException.ThrowIfNull(secondSet);
        return TwoSampleComparison.OfSets(firstSet, secondSet).Cells;
    }

    /// <summary>
    /// this node's BOOT.md, "## Set comparison": whether a *set* of <paramref name="candidates"/> (reference-mode
    /// runs of <paramref name="formulation"/> under the <paramref name="kind"/> layout, passed in seed order)
    /// reproduces the formulation's own <c>R</c> replicas of <paramref name="kind"/> — the instrument
    /// <c>docs/declared-differences.json</c>'s entries are checked against, distinct from <see cref="Compare"/>'s
    /// own single-candidate-against-replicas question. Every printed cell both sets reach is zero-filled to their
    /// union length and compared by a plain Welch two-sample t-test at the exact constant-cell bound
    /// (<see cref="ConstantCellRule.ExactBound"/>), except the four canonical-axis families
    /// (<c>Dkarmcat</c>/<c>dokkarm43</c>/<c>dokkarm10</c>/<c>fqdokkarm(&lt;row&gt;,:)</c>), put on the reference's
    /// own canonical axis first, and the adaptive-tail summaries added alongside them
    /// (<see cref="CanonicalAxisSetCells"/>). The report's own two negative controls split one homogeneous set in
    /// half and compare it against itself — candidates by position (seed) parity, replicas by ordinal parity —
    /// so every entry either finds is a false positive of the method, not a real difference.
    /// </summary>
    public static SetCriterionReport CompareSets(
        string formulation, IReadOnlyList<IReadOnlyDictionary<string, double[]>> candidates, ReplicaKind kind)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return SetComparison.Compare(formulation, candidates, kind);
    }
}
