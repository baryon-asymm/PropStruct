# Decision XIX — the same question, asked where the power is

**Author: the orchestrator, 2026-09-20.** Decision XVIII's question, with an estimator that
has power and a gate that can pass.

## What decision XVIII established and what it did not

It did not settle whether the count scales with the total. Its gate was mis-specified — the
correlation implied by a noiseless monotone curve is mechanically near ±1 whenever the
elasticity differs from 1, so comparing it against a sample correlation of a few tenths
could not agree at any value of anything. That check could not pass, which voids evidence as
surely as a check that cannot fail admits defects.

It did establish the constraint the next attempt must respect: **at `R = 16` to `32` a
per-cell fit has no power.** Most cells' intervals for the elasticity exclude neither 0 nor
1. Nothing asked of a single cell will answer this.

## Where the signal actually is

Under `k | n ~ BetaBinomial(n, p, rho)`, `E[k|n] = n p` exactly, so `E[r|n] = p` and the
correlation between a unit's total and its own cell rate has **expectation zero** — no
systematic sign, whatever the dispersion. What is measured instead is a negative correlation
in every family, on offenders and non-offenders alike, on thousands of cells.

That consistency is the evidence. One cell's `-0.3` is noise; thousands of cells averaging
negative is not, and it is testable exactly.

## The measurement

1. **The pooled statistic.** Per cell, the sample `corr(n_i, r_i)` that decision XVII already
   computes; per family, the mean of its Fisher transform with the standard error implied by
   the cell count. Report it per family, and separately for the offending and non-offending
   populations, so the comparison with decision XVII's table is line for line.
2. **The gate, which can pass.** Simulate the null this node's model asserts: for each cell,
   draw `k_i ~ BetaBinomial(n_i, p_hat, rho_hat)` at the **observed** `n_i`, with that cell's
   own estimated dispersion, and recompute the pooled statistic of item 1. A thousand
   replications give the null distribution of a statistic whose expectation is zero under the
   model and whose sampling spread is then known rather than assumed.
   - the observed pooled statistic lies inside the simulated null: **the conditioning is not
     refuted**, the negative correlations are what this model produces at this sample size,
     and the question moves to dispersion, where Check B's 74 % waits. This decision is then
     withdrawn as a diagnosis and kept as the measurement that cleared it;
   - it lies far outside: `E[k|n] = n p` is refuted by the data, and the routing question is
     live — with the unconditional count predictive, already written and already used on the
     `phi < 1` population, as the standing alternative.
3. **A second reading of the same simulation**, free once it exists: the simulated null's own
   spread says how large a `|corr|` this model produces by chance at `R` replicas. If that is
   of the order of the measured values, decision XIII's whole "centre ratio" — which decision
   XVII proved equals `corr * CV_n * CV_r` — has been reading sampling noise amplified by
   large coefficients of variation, and the tail that has driven four decisions is an artefact
   of the diagnostic rather than a defect of the criterion. Say so if that is what it shows.

## On the shape of the gate

The gate simulates from the model under test and asks whether the observed statistic is
ordinary under it. It can come out either way by construction, which is what decision
XVIII's could not. Any gate proposed in this node from now on is to be checked for both
failure modes before it runs: can it fail if the claim is false, and can it pass if the
claim is true.

No fix, no estimator change, no curve re-run.
