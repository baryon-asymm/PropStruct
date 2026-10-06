# Decision XII — the count rule needs enough cells to know its own total

**Author: the orchestrator, 2026-09-20.** Reversible on the population figures below and
on the Count row's own movement, both measured.

## The mechanism, exactly

A run prints `v_j = k_j / N`. `TryInferRunQuantum` finds the granularity of those printed
values, which is `gcd_j(k_j) / N`. So when the true counts happen to share a divisor `g`,
the reconstruction returns `k_j / g` and a total of `N / g`, losslessly and undetectably:
the reconstructed counts have gcd 1 by construction, and nothing in the printed file says
otherwise.

The probability is closed-form and depends on one thing only — `m`, the number of non-zero
resolvable cells the quantum is fitted from: `sum over primes p of p^-m`, which is 0.45 at
`m = 2`, 0.175 at 3, 0.077 at 4, 0.040 at 5, and 0.0083 at 7. Measured against the tree's
own data at `m = 2`: 50 % observed against 45.2 % predicted, a ratio of 1.11. At `m >= 3`
the observed integer-ratio rate exceeds the prediction by 5 to 2489 times, which is not the
artefact but the overdispersion the same measurement found — at that spread a near-integer
ratio of two totals needs no common divisor.

The cost of the artefact is an interval `sqrt(g)` times too wide, from the `n`-dependence of
`BetaBinomialInterval` — wider, once again, in the direction of accepting the port. Measured
on a mutation built as exact division: median 1.368 against 1.414 at `g = 2`, 1.611 against
1.732 at `g = 3`, agreeing within one to eight per cent on the near-binomial family and
running below prediction on the high-`rho` families, where `(1 + (n-1) rho)` does not scale
as pure `sqrt(n)`. No case exceeded its own prediction by more than six per cent.

## A second, independent motivation, measured after this decision was written

Decision XIII measured the model's own centre against the replicas' counts rescaled to the
candidate's total, knowing nothing of divisors. By resolvable cell count, that ratio reads
0.145 at `m = 2`, 0.802 at 4, 0.762 at 5, against 0.996 at `m >= 10`, where 860 of the 1096
measured cells live; the median over all of them is 0.9946. So the centre is sound in the
bulk and wrong exactly where this decision cuts, and the withdrawn decision XI's own case —
a region missing the replicas' mean twofold — is a member of that population rather than a
general property of the criterion.

Two derivations, from different evidence, naming the same cutoff. That is the strongest
argument this node has produced for any of its thresholds, and it is also why the check
below now has a second arm.

## Why it is not priced

`g` is not observable from a printed file: that is the whole content of the mechanism. A
correction factor would have to guess it, and a guess in a criterion is exactly what this
node keeps having to remove. The mechanism is removed instead of priced.

## The decision

**A cell takes the count rule only when its unit's quantum was fitted from at least seven
non-zero resolvable cells.** Below that, the cell takes the ordinary Student band, whose
level is known and whose reconstruction the artefact does not enter.

- **seven** is where the artefact probability falls below one per cent (0.0083). It is not
  tuned to an outcome: it is fixed by the closed form, before any curve is re-run, and it is
  the smallest `m` with that property;
- **the price is measured**: 137 of 338 units sit below it — 40.5 % of units — but they
  hold 535 of 7728.5 cells, **6.9 % of the compared cells**. The cutoff touches many units
  and few cells because the units it touches are the deep tail rows;
- `m` is the unit's own **median** `m` across the replicas that produced a total, since the
  count varies run to run; a unit at the boundary must not flip rule with the candidate;
- the cells that leave the count rule are counted and reported per family. If that count
  differs materially from 6.9 %, the implementation and the measurement disagree and the
  implementation is wrong;
- decision X's array-total rule inherits the same cutoff: a total reconstructed from fewer
  than seven cells is not compared either, for the same reason and with the same report.

## What it makes able to fail, which decision IX requires stating

Nothing, directly — this decision removes intervals rather than adding them, and every
interval it removes was too wide by an unknown factor. What it makes able to fail is the
rest of the curve: the cells it removes carried an unknown level into the Count row, and the
row's calibration could not have been trusted while they were in it.

The claim that they were too wide is therefore what must be checked, and the check has two
arms, both pre-registered:

- **the Count row's attained level must move toward its nominal one** when these cells
  leave. If it does not move, or moves away, the cells were not the problem;
- **decision XIII's centre ratio, recomputed over what remains, must lose its tail**: the
  11.6 per cent of cells beyond a factor of 1.5 must fall to the few per cent the large-`m`
  buckets already show. If the tail survives the cutoff, the centre defect is not the
  small-`m` population and this decision explains nothing.

Either arm failing withdraws the decision, with that number.

## Order

After decision XI. It touches the same rule selection, and two edits to the same branch
would make neither measurable.
