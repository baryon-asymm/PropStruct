# Decision XI — a cell is judged against the region its level belongs to

**Author: the orchestrator, 2026-09-20.** Found by reading the source after decision IX's
report, not by review. Verified before writing: `StatisticalCriterion.cs`,
`BetaBinomialCountFloor`'s last line and `EvaluateCells`' `exceedsBand`.

## The defect

```
countFloor = Math.Max(high - predictedMean, predictedMean - low) * candidateQuantum
exceedsBand = Math.Abs(cell.CandidateValue - cell.Mean) > threshold + epsilon
```

`BetaBinomialInterval` returns an asymmetric region `[low, high]` **and the exact level
`attainedAlpha` that region excludes**. That level is what the calibration curve credits
the cell with. What the cell is actually judged by is a *symmetric band*, of half-width
the **larger** of the region's two sides, centred not on the model's own mean but on
`cell.Mean`, the average of the replicas' printed values.

The band contains the region and, for any skewed count law, strictly contains it: both
sides are inflated to the longer one. So the tested region's true exclusion probability is
below `attainedAlpha`, systematically, for every Count-governed cell. This is decision IX's
disease one level down — the level belongs to one region, the test to another — and it
survived decision IX because IX fixed which *term* a cell is credited to, not which
*region* a term denotes.

It accounts for Check A quantitatively in the right direction: 0.54 of the expected
exclusions, on cells whose band exceeds their region.

**It does not account for Check B**, and that separation is the useful part of this
decision. Check B compares the candidate against the model's own law and finds 74 % of
Count cells outside a bound a correct model violates on at most `u` of its mass. No
re-centring or re-shaping of the region can fix a model that does not fit. There are two
defects here, not one, and until now they were being read as a contradiction:

- **the region is applied wrongly** — this decision;
- **the law is too narrow** — the dispersion question, item 4 below, now supported by
  decision X's measurement that the arrays' own totals carry dispersion indices with a
  median of 89 and a maximum of 5512, while `rho_hat` runs at 1e-4.

## The decision

1. **A Count-governed cell is judged against `[low, high]` in count space**, the region
   whose level it is credited with: the candidate's own reconstructed count is inside or it
   is not. No symmetric band, no re-centring on the replica mean, no half-width.
2. **The print-resolution guard enters the region, not a competing band**: the region
   widens by the candidate's own resolution expressed in counts, and the size of that
   widening is reported per family. A guard whose magnitude nobody has looked at is the
   next place a level goes missing.
3. `CriterionFailure` carries the region, not a scalar threshold, for these cells; a
   reader of a failure must be able to see what it was outside of.
4. **The Student and static floors keep their present roles** for cells they govern. This
   decision does not add a term and does not take a maximum of anything new.

## What must then hold, and withdraws this decision if it does not

Check A and Check B now measure the same region and the same law, so they must move
together. Report both, with gate 1 and gate 3.

- both improve: correct, keep it;
- Check A reaches about 1.0 while Check B stays far from its bound: the region is now
  applied rightly and the law is still wrong, which is the expected outcome and the
  handover to item 5;
- Check A moves away from 1.0: this decision is wrong and is withdrawn — say so with the
  number rather than tuning it.

## 5. The dispersion question, next and not now

Invert Check B rather than guessing again: per family, find the `rho` that would make the
observed tail-probability distribution satisfy `P(T <= u) <= u`, and report it beside the
`rho_hat` in use. A ratio of a few says the estimator is mis-scaled; a ratio of hundreds
says the beta-binomial is the wrong law for these cells and the next question is which law,
not which estimate. Decision X's total dispersion indices are the prior: they say the
run-to-run drift is large, so a large ratio would be consistent with them rather than with
an arithmetic slip.

No work on tail pooling, `TailRowMean` or the cumulative mass test before that number
exists.
