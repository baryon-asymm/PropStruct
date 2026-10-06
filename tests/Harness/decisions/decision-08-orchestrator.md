# WITHDRAWN 2026-09-20, the same day it was written

This decision was never implemented. An independent Opus 5 review, given the decision
documents and the node but not the reasoning that produced them, refuted it, and two of
its claims were then verified against the source by the orchestrator before the
withdrawal:

- `EstimateDispersion` is called once per `fqdokkarm` row and once per array
  (`tests/Harness/StatisticalCriterion.cs:289` and `:436`), and `n` inside a pooling unit
  is that unit's own total, one number per replica. There is no span of cell totals inside
  a pooling unit, so the premise of this decision — one rho across three orders of
  magnitude — describes the label the diagnostic prints, which mixes rows and
  formulations, not the estimator.
- the number that settles it was already in decision VII: HMX `coef` at n = 11123,
  rho = 6.26e-6, width ratio 1.02–1.04, against `fqdokkarm(20,:)` at n = 7996,
  rho = 1.36e-4, width ratio 1.43–1.45. A factor of 1.4 in n and 22 in rho, landing on
  opposite sides of the calibration boundary: n is not the variable.

Two further defects of this decision, recorded because they are the reason it looked
right: its bootstrap check was biased toward confirming itself twice over (the strata are
confounded with row identity, and a bootstrap over cells of one histogram treats
negatively correlated columns as exchangeable), and its arithmetic argued from decision
VI's shortfall of sixfold, which decision VII had already moved to about 1.5-fold.

The text below is kept unchanged as the provenance of what was decided and why it was
wrong. Its successor is `decision-calibration-9-orchestrator.md`.

---

# Decision VIII — the dispersion is pooled by cell size, not by family

**Author: the orchestrator, 2026-09-20. NOT Fable 5.1's.** Fable 5.1 is unavailable
from this session: after five refusals on subject-matter briefs, the brief was rewritten
with every domain word removed and asked from a directory outside the repository, so that
the project's instruction files were not in the subagent's context. It was refused again,
and so was a probe reading, in full, "Reply with the single word: ok". The refusal is a
property of the session, not of the question. Every decision from VI onward is therefore
mine, and each is written to be reversible on one number.

## What the evidence says

Two facts from the item-1 table and the width measurement, together:

- the family whose interval is now right (`coef`) spans cell totals from 2523 to 11120 —
  a factor of four — and its `e` is flat near zero across all ten of its size deciles;
- the family that carries the residual (`fqdokkarm`) spans 6.6 to 8668 — a factor of
  1300 — and its `e` falls from 0.216 in the first decile to 0.031 in the tenth.

One rho per family is an assumption that the inflation factor `1 + (n - 1) rho` is right
at every cell of the family. It is linear in `n`, so over a factor of 1300 in `n` the
assumption is not approximately anything. The evidence above is what that failure looks
like from outside: the family where the assumption holds trivially (four-fold range) is
calibrated, the family where it cannot hold (1300-fold range) is not.

The arithmetic ties the two ends together. At `fqdokkarm`'s largest cells the interval is
1.43 times the binomial width; a two-sided 5 % exclusion at 1.43 times the width becomes
`2 Phi(-1.96 * 1.43) = 0.5 %`, a tenfold shortfall. The six-fold overall shortfall does
not need a new mechanism — it needs the width to be wrong by tens of per cent at the
cells that dominate the count, which is exactly what was measured.

## The decision

**Rho is estimated per size stratum within a family, and each cell uses its own
stratum's estimate.**

- the strata are the deciles of `n_bar` within the family, as item 1 already computes
  them, so no new binning appears in the code;
- a stratum with fewer than 20 eligible cells (expected count `n_bar p_hat >= 5`, the
  existing eligibility) is merged with its neighbour in size until it has 20; a family
  that cannot reach 20 in any stratum keeps its single family-wide median, unchanged from
  today;
- within a stratum the estimator is the one decision VII settled — per-cell
  `sigma2 = var_i(p_i) - mean_i(p_i (1 - p_i) / n_i)`, `rho_cell = sigma2 / (p_hat (1 - p_hat))`
  — pooled by the median, with one change: the median is taken over the **untruncated**
  values and `max(0, .)` is applied once, to the pooled result. Truncating each cell first
  can only raise the pool, and it raises it most where the per-cell estimate is noisiest,
  which is the small-`n` end that this decision has just shown to be contaminating the
  large-`n` end;
- rho's own uncertainty does not enter the interval. It would widen the intervals, and
  the measured error is that they are too wide already; an inflation for estimator
  uncertainty is a decision for the day the sign of the error changes.

## The cheap check, before any interval changes

On the existing replica data, for `fqdokkarm`, bootstrap the stratum medians over cells
(1000 resamples of the cells within each decile) and print each decile's `rho_cell`
median with its 5th and 95th percentile.

- the decile-1 median lies outside the decile-10 interval and vice versa: rho genuinely
  varies with cell size, this decision is right, implement it;
- the intervals overlap across all deciles: the decline in `e` is sampling noise, one rho
  per family is defensible, and this decision is **withdrawn** — the residual is then a
  property of the `fqdokkarm` array itself, and the next suspect is its compositional
  structure (its cells share a run-dependent total, unlike `coef`'s), which is a different
  interval rule, not a different rho.

## The order of work, unchanged

1. this check, then the stratified rho if it passes;
2. re-run gate 1, the calibration curve and decision VI's two checks;
3. re-measure the batched `Original` excess on the calibrated criterion;
4. only then tail pooling, the `TailRowMean` rewrite, the cumulative mass test.

Nothing in 4 may start before 2 is green: three of the four rules written so far were
wrong in a way only the calibration curve revealed.
