# Fable 5.1 decision II, 2026-09-20 (calibration after the first implementation)

Mapping: A=HPEPA3, B=inpt, C=P33, D=PSAN02n, E=HMX.

1. Gate 2 keeps its target (<=1 failing run of 96) but is no longer what we calibrate to;
   tuning per-cell models until a run count is met is fitting the criterion to the replicas.
   The calibration target is a CALIBRATION CURVE: at per-cell levels 0.05, 0.01, 0.001, the
   blind failure fraction over the 96 x m cell tests, computed PER RULE (Student / count /
   mass) and over NON-DEGENERATE cells only (replicas not constant, band not floor-bound),
   lies inside the binomial band of the nominal level. Calibration is finished when that
   holds for every rule at all three levels on every formulation. No per-family split of the
   gate: a family-level allowance is an exception list by another name.
   The six pre-existing findings get structural fixes:
   (i) the adaptive row count becomes its own count cell; rows are compared on the union of
       row sets, absent rows as zero;
   (ii) tail pooling for every histogram family: merge adjacent cells outward until the
        pooled replica mean count >= 30 (or, without counts, until the pooled mean exceeds
        ten times the print resolution), then test the pooled cell by its regime's rule.
2. Mass families with no sibling number histogram: not untested, not a blind quantum search,
   and exclusion is unavailable (its evidence would need the counts we cannot reconstruct).
   Test the CUMULATIVE mass distribution (running sum along the axis) with the Student band
   plus the print floor, after tail pooling; the quantum drops out and the sums are near
   Gaussian. Weakest at the last one or two cumulative cells; gate 1 decides. If a run-level
   count of the family's events is printed, use it to bracket the total as well.
3. TailRowMean: the row-sum count rule was right, the implementation wrong. ONE quantum per
   family, the same median estimator as the fm fix, resolved once from the family's sibling
   number histogram and applied to every row; the tail-row set fixed by canonical axes,
   identical in every run; NB predictive interval on the sum of reconstructed counts with the
   wide [d_lo^3, d_hi^3] bracket. Without a sibling: continuous with the floor, on the pooled
   tail of item 1. A recorded finding until gate 1 is green on it.
4. The 2.28% does not mean the Student bands are too wide: the pooled number is diluted by
   cells that can never fail and by the conservative count rules. FIRST check one leak: in the
   blind run the judged replica must be excluded from the mean and sd it is judged against; if
   it is included, the band is inflated and 2.28% is exactly that symptom. Then replace the
   diagnostic by the per-rule non-degenerate calibration curve, as a gate. Touch the band
   itself only if the Student-only, non-floored fraction at 0.05 stays below the binomial band
   afterwards, and then by reviewing the sd estimator, never by scaling.
5. Fitness for the design question: yes for a DIFFERENTIAL statement, not an absolute one.
   Supported: batched Independent is indistinguishable from reference Independent (C 0 vs 0,
   E 43 vs 42) provided E's failing cells are the same set; batched Original is worse than
   reference Original (C 35 vs 0, E 91 vs 46), so its stream derivation does not reproduce the
   original's lag structure and is not acceptable as it stands, provided the excess lies on the
   pocket and bridge quantities carrying the known seed bias. Not supported: "batched mode
   passes the criterion", until gate 2 is calibrated and the A/E exclusions carry evidence.
   E fails 42-46 cells in every port run regardless of mode or layout; a constant set is the
   signature of a systematic difference (REAL*4 accumulation), not of a criterion defect.
6. Work list, one implementer:
   1. intersect the failing cells of every port run of A and E; for cells failing in all runs
      compute the exclusion evidence (term count of the REAL*4 accumulation and its bound
      N * 2^-24 * magnitude against the band width). Evidenced cells go to the exclusion list
      and are compared against the port's reference mode instead. Evidence gathering only.
   2. verify the leave-one-out exclusion in the blind run; re-run gate 2 and the diagnostic.
   3. rewrite the diagnostic as the per-rule non-degenerate three-level calibration curve and
      make it a gate; re-run gates 1, 2, curve.
   4. tail pooling for all histogram families; adaptive row count as a count cell with
      union-of-rows comparison; re-run 1, 2, curve, mutations (one new mutation per new rule).
   5. TailRowMean with the family-wide quantum; re-run 1, 2, curve, mutations.
   6. cumulative test for the sibling-less mass families; re-run 1, 2, curve, mutations.
   7. re-run the six port measurements; record the differential statement of item 5 with the
      cell-set checks, and the absolute statement only if gate 2 is at allowance 1 with every
      remaining failing run carrying a mechanism.
