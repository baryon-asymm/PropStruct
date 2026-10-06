# Decision XV — the centre defect at large cell counts, in one family

**Author: the orchestrator, 2026-09-20.** A measurement. Nothing in the criterion moves
until its number exists.

## What is known, after decision XII's withdrawal

The model's centre against the replicas' own counts, rescaled to the candidate's total:

- median 0.9946 over the whole measured population, so the centre is right in the bulk;
- the cells beyond a factor of 1.5 are **13.28 % of `fqdokkarm`** and **0 % of every other
  family**;
- and they are not the small-`m` cells. Decision XII removed those, and the surviving tail
  sits entirely inside the untouched `m >= 10` population — the same 860 cells at the same
  median of 0.995617 to six figures, before and after.

So the defect is one family's, at cell counts where the quantum is reliable, and it is
invisible to the band the criterion actually applies because that band is centred on the
data. Every question about the spread — `rho`, the law, the tail rules, Check B's 74 % —
is downstream of it: a centre that is out by half cannot be repaired by any width.

## What distinguishes `fqdokkarm` from its siblings

Three things, and the task is to find which one carries the defect:

1. **It is estimated per row**, one `EstimateDispersion` and one set of totals per
   `fqdokkarm(row,:)`, where the sibling families are estimated per array;
2. **its rows share a parent array.** The rows of one run are slices of one histogram, so
   a row's total is not an independent quantity — it is a part of a partition whose other
   parts vary with it;
3. **its axis is canonical.** `fqdokkarm` is one of the four families whose category index
   does not mean the same physical bin across runs; the criterion aligns the axis, the
   bias table's per-index test does not, and today's measurement is somewhere between the
   two — which of the two it is, is the first thing to establish.

Point 3 is the one that would make the measurement itself wrong rather than the model, so
it is tested first: **does the centre diagnostic compare cells on the canonical axis the
criterion uses, or on the raw print index?** If the latter, the 13.28 % may be misalignment
rather than a centre defect, and everything below is premature. Report that before anything
else, with the code path that decides it.

## Then, if the measurement is sound

Per `fqdokkarm` row, on the cells beyond a factor of 1.5:

- the ratio `mu_model / mu_emp` against the row's own index, its total, its share of the
  parent array, and its `p_hat`. A defect that follows the row's position in the partition
  is a different bug from one that follows its size;
- whether the offending cells cluster at one end of the row's own axis — the deep tail of
  a distribution, or its head;
- and the same ratio computed with the row's `p_hat` replaced by the parent array's, to
  see whether the row-level pooling is the thing that misplaces the centre.

No fix, no estimator change, no curve re-run. Four decisions in a row proposed a mechanism
before measuring one, and three were wrong; the two that were measured first both held.
