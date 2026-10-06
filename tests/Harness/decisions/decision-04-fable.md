# Fable 5.1 decision IV, 2026-09-20 (the count predictive after the p/q defect)

1. COUNT PREDICTIVE: replace, but MEASURE THE FAMILY FIRST. The direction of the miss
   supports the multinomial mechanism (an over-dispersed model gives fewer failures than
   its attained level = below the band everywhere). But QKS1 feeds the histogram back into
   the pocket decisions, so counts within a run are not independent trials either, and the
   dispersion could sit below or above binomial. Diagnostic, from the replicas alone,
   before any code: per count cell, var(k_i)/mean(k_i) across the R replicas against
   (1 - p_hat) (binomial) and 1 (Poisson), pooled by cell family. At (1 - p_hat):
   beta-binomial is right. Between: beta-binomial is still the better of the two, residual
   recorded. Below: the feedback is anti-correlating and neither family fits without an
   explicit dispersion parameter.
   The replacement, if the diagnostic passes: replica counts k_i with printed totals n_i,
   posterior Beta(1/2 + sum k_i, 1/2 + sum(n_i - k_i)) (Jeffreys); predictive
   BetaBinomial(n*, those two parameters) with n* the candidate's own printed total.
   Boundary: lo = max{k : P(K<k) <= alpha/2m}, hi = min{k : P(K>k) <= alpha/2m}; attained
   level P(K<lo) + P(K>hi) recorded per cell, band = that of the attained level. Totals
   fixed by design (N*KXX accepted particles) count as printed. A family with neither a
   printed nor a design-fixed total keeps the corrected negative-binomial, declared as
   conservative (binomial variance is bounded by Poisson: it loses power, not validity),
   with the number of such cells reported.
2. GATE 1's RED CELL IN HMX: the computation, not the verdict. Compare the reference's row
   total n* with the replicas' row totals n_i for that row. (a) sum n_i comparable to n*
   and the beta-binomial tail P(K >= 11 | n*, Beta(1/2, 1/2 + sum n_i)) below alpha/2m ->
   the reference is genuinely atypical there, and gate 1's zero-failure demand is what is
   then in question. (b) sum n_i small (the row barely populated in five runs, empty in
   eleven) -> the predictive is wide, the cell passes under the corrected rule, and the red
   was an artefact of testing a conditional cell as if it were marginal. (c) n* itself far
   outside the replicas' row totals -> the atypical quantity is the row total, a marginal
   count cell, and the inner cell is untestable and declared with its sum n_i. The cell is
   a tail row and belongs to the class TailRowMean was designed for, so its status is
   provisional until those rules exist. Leave it red and declared meanwhile.
3. GATE 2, 10 -> 13: information, not regression. At least three runs fail on count cells a
   too-wide rule let through. The expected count at a calibrated 1e-3 over 96 judgements is
   ~0.1, so "at most 1" stays; 13 says the rules are not yet calibrated and the gate is
   working. Decompose the 13 by rule and cell class before anything else: clustered in tail
   rows and small-mass cells -> the unstarted rules address them; in continuous cells ->
   the Student band has its own problem, unrelated to this finding.
4. ORDER: dispersion diagnostic (a script over the fixtures, about an hour), then the
   beta-binomial replacement, then tail pooling, TailRowMean, cumulative mass. Building the
   pooled tail cells (which are count cells) on a family known to be wrong means calibrating
   them twice. The cumulative mass test is independent of the family and may run in parallel.
5. WHAT THIS INVALIDATES:
   - the choice of the negative-binomial as the count predictive: the family only, not the
     structure (conjugate posterior from R replicas, discrete two-sided boundary, attained
     level), which stands;
   - every recorded calibration figure, attained level and exclusion-list evidence computed
     under the swapped formula: void, to be regenerated; any dated tick citing them predates
     the fix and is subject to re-verification (AGENTS.md §6);
   - gate 1's previous green: obtained on a rule that could not fail, so it never was
     evidence. If the root criterion "the original's own reference passes ... with no
     failure" carries a tick, UNTICK IT.
   - not invalidated: the attained-level decision (its reasoning is discreteness, independent
     of the defect), the gate 2 threshold, the lagged/independent replica split, the
     known-bias table (two-sample means, no predictive involved).
