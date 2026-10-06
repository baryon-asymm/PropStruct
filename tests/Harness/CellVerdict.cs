namespace PropStruct.Tests.Harness;

/// <summary>
/// `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: which of the three
/// verdict mechanisms <see cref="CellEvaluator.EvaluateCells"/> actually used to judge a cell — not the family a
/// cell's name belongs to, since a distribution-function cell whose run never determined a quantum is judged by
/// the plain Student band exactly like a continuous cell, and is classified accordingly. Split out of
/// <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal enum CalibrationRule
{
    /// <summary>The plain Student band, <c>t * sd * sqrt(1 + 1/R)</c>, floored by print resolution and, for a
    /// count-like cell whose own run never determined a quantum, the Poisson-like floor.</summary>
    Student,

    /// <summary>The count floor (the negative-binomial or beta-binomial predictive interval of "Count-like
    /// cells") GOVERNS this cell's threshold (<c>countFloor &gt; studentTerm</c>,
    /// `tests/Harness/HISTORY.md#decision-ix-full-reasoning`) — not merely that it was computable. A count-governed
    /// cell can still have <see cref="CellVerdict.AttainedAlpha"/> <c>null</c> (the `phi &lt; 1` route, or
    /// <c>MaxCountFloorTotal</c>'s cap): its own attained level is unknown, and a caller building the
    /// calibration curve must treat that as its own population, entering neither this rule's row nor
    /// <see cref="Student"/>'s.</summary>
    Count,

    /// <summary>The mass-family sibling deterministic bracket or the mass-family absolute ceiling — neither
    /// depends on <c>alpha</c> at all (this node's BOOT.md, same section: "every mass-rule cell is
    /// unconditionally floor-bound").</summary>
    Mass,
}

/// <summary>
/// One cell's verdict at a given <c>alpha</c>, carrying enough to feed the calibration curve
/// (`tests/Harness/HISTORY.md#fable-5-1-decision-ii`): <paramref name="Rule"/> is which mechanism decided it,
/// <paramref name="Degenerate"/> is whether <c>alpha</c> could have changed the outcome at all (a constant
/// replica set, or a threshold the alpha-independent floor already decides regardless of the alpha-dependent
/// term) — the calibration curve is computed over non-degenerate cells only.
/// </summary>
/// <summary><paramref name="AttainedAlpha"/> (`tests/Harness/HISTORY.md#fable-5-1-decision-iii`, "The Count
/// rule's curve row") is the exact two-sided tail mass the Count rule's own negative-binomial or
/// beta-binomial boundary excludes at this cell — set only when that boundary was actually built (the cell's
/// own run determined a quantum, stayed under `MaxCountFloorTotal`/`MaxBetaBinomialTotal`, and its own family
/// did not route to `phi &lt; 1`); <c>null</c> for `Student`/`Mass` (which never attain a level) and, per
/// decision IX, also possibly for a <see cref="CalibrationRule.Count"/> cell whose threshold the count floor
/// governs without ever producing a boundary — that combination (`Rule == Count`, `AttainedAlpha == null`) is
/// its own reportable population, not a `Student` cell and not a scoreable `Count` one.
/// </summary>
/// <summary><paramref name="Eligible"/> (`tests/Harness/HISTORY.md#decision-vi-full-reasoning`):
/// whether some value this cell's own quantity could actually take, printable in its own format, lies outside
/// this cell's own interval at the level it was judged at — an interval that already covers the quantity's
/// whole attainable range says nothing about calibration, however it happened to be constructed, and diluting
/// the pooled cell count with it hides a real miscalibration rather than measuring one. Computed per rule
/// (`Count`: `lo &gt; 0 || hi &lt; n`, `n` the array's own reconstructed total; `Student`: the three cases
/// this node's own BOOT.md names; `Mass`: whether the bracket already spans the family's own theoretical
/// ceiling) and per <c>alpha</c>, exactly as this record's other alpha-dependent fields already are — never
/// folded into <see cref="Degenerate"/>, a different, alpha-independent notion (whether alpha could have
/// moved the outcome at all) that this eligibility test does not replace or narrow.</summary>
/// <summary><paramref name="TailProbability"/> (`tests/Harness/HISTORY.md#decision-vi-full-reasoning`,
/// item 5's own second cheap check): the exact two-sided predictive probability of a value at
/// least as extreme as the one observed — <c>2 * min(P(X &lt;= k), P(X &gt;= k))</c> under the same count
/// predictive <see cref="AttainedAlpha"/> already reports a boundary from — set only for
/// <see cref="CalibrationRule.Count"/> cells, under the same conditions as <see cref="AttainedAlpha"/>; a
/// different quantity from it (a per-observation p-value, not the tail mass a fixed nominal <c>alpha</c>'s
/// own boundary excludes), needed to test the uniformity item 5 asks for.</summary>
/// <summary><paramref name="StudentTerm"/>, <paramref name="CountFloor"/> and <paramref name="StaticFloor"/>
/// (`tests/Harness/HISTORY.md#decision-ix-full-reasoning`, item 1) are the three terms
/// <c>threshold = Math.Max(StudentTerm, Math.Max(StaticFloor, CountFloor))</c> was actually built from for this
/// cell — carried so <paramref name="Rule"/> can be, and can be checked to be, a report of which term governs
/// the threshold rather than of which family the cell's name belongs to or which term happened to be
/// computable. `0.0` for a <see cref="CalibrationRule.Mass"/> cell, whose threshold is not built from these
/// three terms at all (`ForceFailure`'s bound or the mass-sibling bracket,
/// `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`).</summary>
/// <summary><paramref name="RegionLow"/>/<paramref name="RegionHigh"/>
/// (`tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`) are the count-governed region `[low, high]` — the exact boundary
/// <see cref="AttainedAlpha"/>'s own level excludes, converted to value units through the cell's own quantum
/// — informational (decision XI's own attempt to judge <see cref="Failed"/> against this region directly, in
/// count space, was measured and withdrawn: `tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`). `Failed` is the
/// symmetric `Math.Max(StudentTerm, Math.Max(StaticFloor, CountFloor))` band, decision IX's own construction,
/// applied around <see cref="Mean"/> for every cell except the one narrow case
/// `tests/Harness/HISTORY.md#count-floor-boundary-centring-defect` names —
/// where it is applied around the count floor's own predictive mean instead, converted to value units
/// through <paramref name="Quantum"/>; that shift is not reported as a separate field, since `Mean` keeps
/// reporting the replicas' own sample mean for every cell, unchanged, and only <see cref="Failed"/> reads the
/// corrected centre. <paramref name="ResolutionGuardCounts"/> is the candidate's own print resolution
/// expressed in count units (`candidateResolution / quantum`) — `null` whenever no quantum exists for this
/// cell at all (not merely whenever no region exists), reported per family by
/// <c>tests/Harness.Tests/RegionGuardDiagnosticTests.cs</c>. A cell whose rule is <see cref="CalibrationRule.Count"/>
/// is the exception to the value-space reading above (B2c, `tests/Harness/HISTORY.md#count-region-edge-2026-10-02`):
/// it fails when its reconstructed count lies more than `Threshold / Quantum` from `mu + s`, `mu` the predictive's
/// mean in counts and `s` the whole number of counts nearest the band centre's offset from `mu`, or when its
/// count is implausible.</summary>
/// <summary><paramref name="Quantum"/> and <paramref name="ObservedCount"/>
/// (`tests/Harness/HISTORY.md#count-floor-boundary-centring-defect`) are the
/// candidate's own per-run quantum and its reconstructed count at this cell, `null` whenever no quantum was
/// inferred for this cell's own array at all — informational, read by
/// <c>tests/Harness.Tests/CountFloorBoundaryTests.cs</c> to build a candidate exactly at a cell's own
/// `RegionHigh` without retyping it. <paramref name="ImplausibleCount"/> is whether this cell's own
/// candidate value failed <see cref="RunQuantum.IsNearIntegerMultiple"/> against its own quantum — the
/// same flag that already forces <see cref="Failed"/>, exposed here only for a reader inspecting why.
/// <paramref name="PooledCount"/> (B2a, `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`) is the sum of the
/// contributing replicas' reconstructed counts at this cell, the total the evaluator compares with
/// <see cref="CountFloor.HeavyTailRegimeThreshold"/>; <c>null</c> when the cell carries no counts.</summary>
internal readonly record struct CellVerdict(
    string Name, int Index, double CandidateValue, double Mean, double Threshold, bool Failed, CalibrationRule Rule,
    bool Degenerate, double? AttainedAlpha, bool Eligible, double? TailProbability,
    double StudentTerm, double CountFloor, double StaticFloor,
    double? RegionLow, double? RegionHigh, double? ResolutionGuardCounts,
    double? Quantum = null, double? ObservedCount = null, bool ImplausibleCount = false, double? PooledCount = null);
