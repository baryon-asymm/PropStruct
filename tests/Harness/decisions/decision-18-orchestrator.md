# Decision XVIII — does the count scale with the total at all

**Author: the orchestrator, 2026-09-20.** One number per cell decides which of two models
this node has been arguing about all day, and the alternative is already implemented.

## Why this is the question

Decision XVII established, to floating-point noise, that the whole "centre defect" is
`corr(n, r) * CV_n * CV_r` with `r = k/n`, and that the correlation is **negative in every
family**, offenders and non-offenders alike.

A negative correlation between `n` and `k/n` has a mechanical source that needs no model at
all: if a cell's absolute count `k` is steadier than its unit's total `n`, then `k/n` must
fall as `n` rises. In the limit of a perfectly steady `k`, the correlation is exactly −1.

The beta-binomial asserts the opposite: `k | n ~ BetaBinomial(n, p, rho)` has `E[k|n] = n p`,
the count **proportional** to the total. A universal negative correlation is what that
assumption looks like from outside when it is false.

## The measurement

Per cell, over its contributing replicas, the elasticity

    beta = d log k / d log n,

by an ordinary least-squares line of `log k` on `log n` (cells with any zero count excluded
and counted, since the logarithm has no opinion about them), with its standard error.
Report per family, for offending and non-offending cells apart:

- the distribution of `beta`, and the share of cells whose interval for `beta` excludes 1,
  and separately the share whose interval excludes 0;
- `beta` against `CV_n`, since a small `CV_n` leaves no leverage to estimate it and those
  cells must not be read as evidence either way;
- the implied correlation `corr(n, k/n)` recomputed from `beta` alone under the fitted line,
  against the measured correlation — a second gate, in the same spirit as decision XVII's:
  if the elasticity cannot reproduce the correlation already measured, the fit is not
  describing these data and the rest is void.

## What each outcome means, decided before the numbers

- **`beta` near 1**: the count does scale with the total, conditioning on `n` is right, and
  the correlation is a second-order effect of how `k` and `n` co-vary. The beta-binomial
  stays and the next question is its dispersion, where Check B's 74 % still waits;
- **`beta` near 0**: the count is a quantity in its own right and the total tells it
  nothing. Conditioning on `n` is then not a refinement but an error, and the right model is
  the unconditional count predictive — **which this node already has and already uses**, on
  the 40.7 % of Count-governed cells routed by `phi < 1`. The change would be to the routing,
  not to any new formula;
- **`beta` between, and varying by family**: neither model is right for all of them, the
  routing must be decided per family by a measured criterion, and that criterion is the next
  decision's subject.

## Note on what has already been refuted, so it is not reopened

The partition structure of `fqdokkarm` is **not** the explanation: decision XVII found the
correlation in every family. `fqdokkarm` is where it becomes visible, not where it lives.
Any reading of these numbers that returns to "this is about the one family" is wrong before
it starts.

No fix, no estimator change, no curve re-run.
