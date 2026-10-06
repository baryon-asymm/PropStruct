# Decision XIII — find the centre before touching the spread

**Author: the orchestrator, 2026-09-20.** A measurement, not a change. Nothing in the
criterion moves until its number exists.

## What decision XI's withdrawal established

Judged against its own region, a Count-governed cell fails far more often than its level
allows (Check A 0.54 -> 6.46), and one measured case has the region `[0.005414, 0.008666]`
not containing the replicas' own mean `0.0034144` — the model's centre is out by about a
factor of two against a spread of some twenty per cent. Check B did not move at all.

Three defects, then, not two, and they were confounded because the symmetric band is drawn
around the replicas' mean: **a band centred on the data cannot see a model whose centre is
wrong.** That is why nine decisions passed over it.

Everything about the spread — `rho`, the law, the tail rules — is unfalsifiable while the
centre is unexplained: a wrong centre inflates every tail probability (Check B) and is
invisible to the band (Check A). So the spread work stops here until this measurement
reports.

## The measurement

Per Count-governed cell, in count space and reported per family:

1. `mu_model = n* p_hat`, the region's own centre;
2. `mu_emp = mean_i(k_i * n* / n_i)`, the replicas' own counts rescaled to the candidate's
   total — what the model is claiming to predict;
3. the ratio `mu_model / mu_emp`: median, deciles, and the count of cells beyond a factor
   of 1.5 either way;
4. that ratio against the unit's dispersion index `phi_T` and against `m`, so a mechanism
   is visible rather than inferred.

## The first hypothesis, with its own test

`p_hat = sum_i k_i / sum_i n_i` is a **total-weighted** mean of the per-run rates, and the
quantity it is used to predict is one run's own rate. Decision X measured the totals'
dispersion index at a median of 89 and a maximum of 5512; at that spread the weighted and
the unweighted mean of the same rates are different numbers.

Test it directly: per cell, `p_hat` against `mean_i(k_i / n_i)`, the unweighted rate.
Report the ratio's distribution per family, and how much of item 3's gap it accounts for.

- it accounts for most of the gap: the estimator is answering a different question from the
  one asked, and the fix is to estimate the rate the comparison is about;
- it accounts for little: the centre is wrong for another reason, and items 3 and 4 say
  where to look next. Report that rather than proposing a fix.

## What is not to be done in this task

No change to `EvaluateCells`, no change to any estimator, no re-run of the curve. This
decision exists because four decisions in a row proposed a mechanism before measuring one,
and three of them were wrong.
