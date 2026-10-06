# Decision VII, 2026-09-20 — orchestrator's, NOT Fable 5.1's

Fable 5.1 refused this question too (a fourth refusal today, on a brief with no subject
matter at all). Reversible as before: if Fable later disagrees, its answer wins.

## What the measurement says

Observed exclusions 334 against 2055 expected by the intervals' own attained levels:
the intervals are about six times too wide. The tail probabilities are a broad shift,
not a spike, so this is width, not a diluted denominator (decision VI's hypothesis is
refuted by its own checks, and that refutation stands recorded).

The shortfall grows with cell size and with family population, and NOT with the family's
estimated rho. That is the signature, and it follows from the model's own arithmetic:
the beta-binomial variance is n·p·q·(1 + (n−1)·rho). At n = 11000 and rho = 0.002 the
inflation factor is about 23, so the interval is roughly five times wider than binomial —
for the most populous cells, which is exactly where the missing exclusions are.

## The diagnosis

One rho per family, applied to cells whose n differ by three orders of magnitude, cannot
be right unless the extra variance really is multiplicative in n. Two things make the
present estimate too large for the large cells:

- **phi is biased upward by small cells.** phi is a chi-square-shaped statistic; with
  n_i·p̂ of order one its distribution has a heavy right tail, so cells whose expected
  count is small push the family's pooled phi up. Those cells are numerous.
- **Pooling is by family, and the families mix sizes.** A rho estimated mostly from small
  and medium cells is then applied, through the (n−1) factor, to cells a thousand times
  larger, where it multiplies the variance by tens.

## What to do, in order

1. **The check, before any code change** (one pass over the replicas, no interval
   involved). For every count cell compute the excess-variance ratio
   `e = ( var_i(k_i) − mean_i(n_i p̂ (1−p̂)) ) / ( n̄² p̂² )`,
   an estimate of the run-to-run variance of the cell's own probability. Under the
   mechanism that motivated the extra term — the cell's probability drifting within a run,
   because the pocket histogram is fed back into the neighbour loop — `e` is a property of
   the cell and does NOT depend on n̄. Summarise `e` against n̄ across the cells of each
   family, by size decile. Two outcomes:
   - `e` roughly constant across deciles: the multiplicative form is right, and the fault
     is only in the estimator (step 2);
   - `e` falling with n̄ (in particular like 1/n̄): the extra variance is not multiplicative
     in n, the beta-binomial is the wrong shape, and step 3 applies.
2. **If the form is right, fix the estimator.** Estimate the run-to-run variance directly
   and robustly, per cell, then pool: `sigma2_cell = max(0, var_i(p_i) − mean_i(p_i(1−p_i)/n_i))`
   with `p_i = k_i/n_i`; `rho_cell = sigma2_cell / (p̂(1−p̂))`. Pool over the family by the
   MEDIAN of `rho_cell`, over cells whose expected count `n̄·p̂` is at least 5 — the small
   cells that bias phi upward are excluded from the estimate but still judged by it. Report
   the estimate's own spread beside it in the generated dispersion table, so the next
   reader sees how well determined it is.
3. **If the form is wrong**, replace the multiplicative term by an additive one:
   `Var(k) = n p q + c`, with `c` estimated the same robust way per family, which keeps the
   extra variance from growing with n. Whichever form step 1 selects, the other is recorded
   as refuted with its numbers, not deleted.
4. Re-run gate 1, the calibration curve and the two checks of decision VI. The target is
   unchanged: the count rule inside its attained-level band, gate 1 with no failure.
5. Only then tail pooling, the TailRowMean rewrite, the cumulative mass test.

## What this does not touch

Alpha, R, the exclusion list, `m`, and the acceptance test's own thresholds. This is the
estimator of one parameter of one rule, and the rule's structure — conditioning on the
run's own total, a discrete two-sided boundary, the attained level — stands.
