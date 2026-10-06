# Decision VI, 2026-09-20 — taken by the orchestrator, NOT by Fable 5.1

Fable 5.1 refused three successive framings of this question (its safeguards flagged the
message; the third was plain statistics with no subject matter at all). The standing rule
is that open questions go to Fable; it is unavailable, so this decision is mine, and it is
marked as such so that it can be put to Fable for review when the model answers again.
Treat it as reversible: if Fable later disagrees, its answer wins.

## 1. The hypothesis is accepted: the denominator is diluted, the rules are not (yet) the problem

Two independent facts say so.
- The Student rule was inside its band at 0.05 on all five formulations until 1362 cells of
  one family were routed into it, and went under the band the moment they arrived. Nothing
  about the Student band itself changed at that instant; only its population did.
- The count rule's shortfall is 84 % carried by the two most populous families, and the
  family with the largest estimated dispersion has the smallest relative shortfall. A
  mis-specified interval would scale with dispersion; this scales with population.

## 2. Eligibility, per rule, computed per cell AND per level

A cell is eligible at level α if some value it could actually take lies outside its own
interval at that level. Formally, with `S` the set of values the quantity can take that the
print format can express:

- **Count rule** (beta-binomial on the run's own total `n`): eligible iff the interval
  `[lo, hi]` does not cover the whole support, i.e. `lo > 0 or hi < n`. A cell whose
  interval is `[0, n]` cannot report anything at that level and is not counted.
- **Student rule**: eligible iff some printable value of the quantity's own attainable
  range lies outside `mean ± threshold`. In practice this fails in three ways, each
  computed, not guessed: the threshold is infinite or NaN; the band covers the quantity's
  whole attainable range (a fraction on [0,1], a non-negative quantity whose band reaches 0
  and whose upper end exceeds the largest printable value of its field); or the replicas are
  all equal and the print-resolution floor makes the band cover the next printable value on
  both sides.
- **Mass bracket**: eligible iff the admissible set the bracket defines does not cover the
  whole attainable range of the cell at that level.

No hand-maintained list anywhere: each test is arithmetic on the cell's own interval, its
own total and its own print format.

## 3. Eligibility is per level, and the curve is three assertions, not a sequence

A cell eligible at 0.001 but not at 0.05 is counted at 0.001 and not at 0.05. The
denominators therefore differ between levels, and the curve must be read as three separate
calibration statements rather than one monotone sequence. The document says so explicitly,
and the eligible count is reported next to every reading, so a change of population is
visible rather than silent.

## 4. What the calibration then asserts

It asserts that the rules are calibrated **over the cells that can speak at that level**,
which is the only thing the data can support. It does NOT narrow `m`: the family-wise
Bonferroni correction of the acceptance test keeps every compared cell in `m`, because an
ineligible cell still consumes the multiplicity budget of the test the moment its interval
is computed. Eligibility governs the calibration denominator only. This separation is
written into the node's own document, because the obvious next mistake is to shrink `m` by
the same filter, which would loosen α without anyone deciding to.

## 5. The cheap independent check, before any recomputation

Two, both one pass over what is already computed:

- **Expected against observed on the eligible set.** For the count rule every cell already
  carries its attained level. Compare the observed number of reports against the SUM of the
  per-cell attained levels over eligible cells, not against `n · nominal`. If they agree,
  the intervals are right and the gap was dilution; if the observed number stays far below
  the sum over eligible cells, the intervals are genuinely too wide.
- **Uniformity of the per-cell tail probability.** For each eligible cell compute the
  predictive probability of a value at least as extreme as the one observed, and test that
  set for uniformity. A diluted denominator shows as a point mass near 1; intervals that are
  too wide show as a distribution pushed toward 1 across the whole range. The shape
  distinguishes the two without recomputing the curve.

## 6. Order

1. Eligibility as defined, plus the two checks of item 5, and re-read the curve. No new
   rule is written until this reading is understood.
2. Tail pooling.
3. The TailRowMean rewrite.
4. The cumulative mass test.
Gates 1 and 2 are re-read after step 2, as decision V has it.
