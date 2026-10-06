# Decision X — the array total is compared, not only conditioned on

**Author: the orchestrator, 2026-09-20, on an Opus 5 review's finding and the user's
instruction to close it.** Reversible on the measurement item 1 asks for, which also
chooses between the two interval forms item 2 offers.

## The gap

Every count-like cell's interval is built at `nStar = candidateTotal` — the candidate run's
**own** reconstructed array or row total (`StatisticalCriterion.cs`, `BetaBinomialCountFloor`
and the `p_hat = sum k_i / sum n_i` that feeds it). `p_hat` comes from the replicas, so the
**shape** of each distribution is tested. The total itself is compared with nothing,
anywhere: it is not printed as a scalar, it is reconstructed from the printed frequencies
and the run's own inferred quantum, and it then enters the criterion only as a parameter.

Two consequences, and the second is worse than the first:

- a port whose histogram has the right shape and the wrong normalisation — fewer or more
  contributing items per cycle, a miscounted class, a dropped category — passes every cell
  of that array;
- the candidate is judged at its own `n`, so a port that contributes fewer items is
  additionally granted a **wider** interval on every cell of the array. The error and the
  licence for it come from the same number.

This is the channel where a real porting defect is most likely to live, and it is the one
channel with no check on it at all.

## 1. The measurement that chooses the interval (first, in its own commit)

Per array and per `fqdokkarm` row — the same estimation units `EstimateDispersion` already
uses — and per reference formulation, over the replicas of each layout:

- the replicas' reconstructed totals `T_i`, their mean and their sample variance;
- the **dispersion index** `phi_T = s^2 / T_bar`;
- how many replicas produced a total at all (a run whose quantum could not be inferred
  produces none), and how many units are therefore not comparable;
- and one honesty check on the reconstruction itself: the fraction of units where the
  replicas' totals are not mutually consistent with a common quantum — in particular where
  the ratio of two replicas' totals is close to a small integer, the signature of a quantum
  inferred a factor out (every printed count of that run sharing a common divisor). A shared
  artefact cancels between candidate and replicas; one that differs between them would
  manufacture a failure, so its rate is measured before the rule is trusted, not after it
  fails.

## 2. The rule

A new compared quantity per estimation unit: the unit's reconstructed total. Its interval:

- **if every family's `phi_T` measured in item 1 is at most 1.5**, the existing
  `NegativeBinomialInterval(sum of the replicas' totals, R, alpha)` — the Poisson-Gamma
  predictive already written, already tested, already proven non-degenerate. No new formula;
- **otherwise**, the same primitive is wrong (too narrow) and the interval is the ordinary
  Student band on the `R` totals, `t * s * sqrt(1 + 1/R)`, which absorbs the run-to-run
  spread empirically and is the node's own rule for a quantity whose law is not modelled.

The branch is chosen **once**, by the measurement, and the chosen branch is recorded with
the figure that chose it. It is not chosen per unit, and it is **not** `Math.Max` of the two:
decision IX's standing rule for this node is that no rule is added by taking a maximum, and
a rule that widens itself by construction is a rule that cannot fail.

- the totals enter `m`, machine-counted like every other compared quantity (root BOOT.md).
  Report the old and new `m` per formulation and the resulting change in `t` at `alpha / m`:
  this rule makes every other interval slightly wider, and that price is stated, not assumed
  negligible;
- the rule gets its **own row** on the calibration curve, `CalibrationRule.Total`, with its
  own expected level. A rule that is not on the curve is a rule nobody calibrates;
- a unit whose candidate or whose replicas (fewer than four with a total) cannot supply the
  numbers is not compared, is counted, and is reported — never silently skipped.

## 3. The proof that it can fail, which is the point of it

Not a mutation of the code: a mutation of the **data**, in the node's own oracle-mutation
machinery. Take a candidate `results.m` that passes the criterion today, and rescale one
array's printed frequencies onto the granularity of a different total — the same shape, a
different `N` — at +10 % and at +50 %.

- today's criterion must **pass** both. If it fails one of them today, this decision's
  premise is wrong and the gap is narrower than stated: report it and stop;
- with the rule, both must fail, and the +10 % case is the one that matters. If only +50 %
  fails, report the smallest detected deviation rather than declaring the rule proven.

## 4. What this rule does not claim

It tests the total of each distribution, not the physical quantities the original prints as
scalars, and not the agreement of two arrays' totals with each other. It also does not make
the conditioning wrong: conditioning each cell on the run's own total remains the right way
to test a shape. The claim becomes the conjunction of the two — the shape given the total,
and the total — which is what was always meant and never checked.
