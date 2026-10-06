# Decision XVI — the sparsest cells, and the rule that was parked for them

**Author: the orchestrator, 2026-09-20.** One measurement, whose outcome decides whether a
rule that has been waiting since the first ordering is the right next one or the wrong one.

## Where decision XV left it

The centre defect is not the model's in general (median ratio 0.9946), not the small-`m`
units' (decision XII died on that), and not row-level pooling's (refuted at 62.72 % against
19.88 %). It is **94.85 % concentrated in the deep tail of each row's own column axis**, at
the smallest row totals, the smallest share of the parent histogram and the smallest `p_hat`.

That is a description of sparse cells, and sparse cells have an obvious candidate mechanism
that has nothing to do with the model being wrong: a cell whose expected count is of order
one is reconstructed from a printed value sitting at the edge of its own print resolution.
`ReconstructTotal` already drops a non-zero cell whose quantum does not clear its resolution,
and a cell printed as zero contributes zero unconditionally. At an expected count near one,
the difference between a printed 0 and a printed one quantum is the whole quantity.

## The measurement

For every cell in decision XV's population, the ratio `mu_model / mu_emp` against **the
cell's own expected count** `n_bar * p_hat`, reported by bucket: below 0.5, 0.5–1, 1–2,
2–5, 5–10, 10–30, above 30.

- the departures concentrate below an expected count of a few, and the buckets above are at
  unity: the defect is sparse-cell reconstruction, not the centre of the model, and it is
  what **tail pooling** exists to fix — the rule parked since the first work order;
- the departures persist at expected counts of ten and thirty: sparseness is not the
  mechanism either, the model really does misplace its centre in these rows' tails, and
  tail pooling would paper over a defect rather than remove it. Say so; it is the more
  interesting answer and the more expensive one.

Report alongside it, since both are cheap once the bucketing exists:

- how many cells of the whole compared population fall in each bucket, so the size of what
  tail pooling would touch is known before it is written;
- the same bucketing for the sibling families, which show a zero tail, to confirm they are
  zero because they have no such cells rather than because they behave differently in them.

## What this decides, and what it does not

It decides the **order**: whether tail pooling is the next rule or a postponed one. It does
not decide the pooling rule itself — what pools with what, at what threshold, and what it
makes able to fail — which is a design question for the decision that follows this
measurement.

No fix, no estimator change, no curve re-run.
