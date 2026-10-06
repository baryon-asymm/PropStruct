# Decision XVII — the "centre defect" is a correlation, and it has a name

**Author: the orchestrator, 2026-09-20.** An identity, then a measurement of the thing the
identity names. Still nothing changes in the criterion.

## The identity

Let `r_i = k_i / n_i` be replica `i`'s own rate in a cell and `n_i` its unit's total. Then

    mu_model = n* * p_hat,  p_hat = sum_i k_i / sum_i n_i = E[n r] / E[n]
    mu_emp   = n* * mean_i(r_i) = n* * E[r]

and since `E[n r] = E[n] E[r] + Cov(n, r)`,

    mu_model / mu_emp  =  1 + Cov(n, r) / (E[n] E[r])  =  1 + corr(n, r) * CV_n * CV_r

**So the quantity measured in decisions XIII, XV and XVI as "the model's centre against the
replicas'" is neither more nor less than the correlation between a run's total and its rate
in that cell, scaled by the two coefficients of variation.** A ratio of 0.145 is not a
mysterious misplacement; it is a strong negative correlation in a unit whose total varies a
great deal.

This was not known while those measurements were made, and it recasts all three: the tail
concentrated in `fqdokkarm`, absent in its siblings, persisting at every expected count, is
the statement **that in `fqdokkarm` a row's total and its cell rates move together, and in
the sibling families they do not.**

## Why that is a specification error and not a curiosity

`BetaBinomial(n*, p_hat, rho)` assumes the rate is a property of the cell and the total is a
nuisance parameter to condition on. If the rate depends on the total, conditioning on the
candidate's own `n*` while estimating the rate as a pooled `p_hat` predicts a quantity
nobody wants: the average rate across runs of every size, applied at a size where that
average is wrong.

The mechanism is plain in what `fqdokkarm` is. Its rows are slices of one histogram — a
partition — so a row's total is not a nuisance at all: a row that took a larger share of the
parent this run is a row whose own distribution has shifted. The sibling families are whole
arrays, whose totals carry no such meaning.

## The measurement

Per cell, over the contributing replicas:

1. `corr(n_i, r_i)`, with `CV_n` and `CV_r`, and the identity's right-hand side against the
   ratio already measured — they must agree to rounding. **If they do not, this identity is
   wrong and everything below is void**: report the discrepancy and stop;
2. the distribution of `corr(n_i, r_i)` per family, for the offending cells and the rest
   separately. The prediction is a correlation near zero everywhere except `fqdokkarm`'s
   offending cells;
3. for `fqdokkarm`, whether the correlation's **sign** follows the cell's position on the
   row's own axis — the deep tail being where decision XV found the offenders;
4. a straight line fitted to `r` on `n` per offending cell, and what `E[r | n = n*]` from that
   line would be, against `p_hat`. Not to adopt it: to see how far the right conditioning is
   from the current one, in units of the interval's own width.

## What it decides

Whether the next decision changes **what is estimated** (a rate at the candidate's own total,
from a relation fitted across replicas) or **what is conditioned on** (dropping the
conditioning for families whose totals carry information). Both are real options and the
measurement's item 4 is what tells them apart; neither is chosen here.

No fix, no estimator change, no curve re-run. This node has now measured first six times and
been right both of the times it did not.
