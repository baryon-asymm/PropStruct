# Fable 5.1 decision, 2026-09-19 (blind calibration; owner: "do as Fable decides")

Mapping of the abstract brief: A=HPEPA3, B=inpt, C=P33, D=PSAN02n, E=HMX.

1. Merge the three corrections now, WITHOUT adding the five new named exceptions (HPEPA3 r3 fmkarm[73], r7 fmkarm_cor[69], r27 fmkarm_cor[70], PSAN02n r14 TailRowMean[26]); record them as an open finding of gate 2.

2. Heavy-tail rule.
 - Regime switch per fm* cell, machine-computed from the fixtures: replica mean count mu = K/R. mu >= 30: Student band + 1/step ceiling unchanged. mu < 30: count rule.
 - Statistic: bracket N_lo = ceil((v-eps)*step*T/(c*d_hi^3)), N_hi = floor((v+eps)*step*T/(c*d_lo^3)), clamped >= 0; eps = print half-quantum; T the run's normalization total.
   If a same-axis NUMBER histogram is printed for the family, use its count instead and reduce the fm cell to a deterministic consistency check (v within [n*c*d_lo^3, n*c*d_hi^3]/(step*T) +- eps). Check this first.
 - Reference: replica counts n_j = integer nearest the midpoint of replica j's bracket; K = sum n_j; the existing negative-binomial predictive engine, no new parameters. L = largest k with P(N<k) <= alpha/(2m), U = smallest k with P(N>k) <= alpha/(2m). Pass iff [N_lo,N_hi] intersects [L,U]. K=0 still gives finite U: nothing in the count regime is untestable; the "<2 non-zero replicas" exclusion applies only to genuinely continuous families. m grows; accept the tighter alpha/m.
 - TailRowMean: replace the continuous rule by the count rule on the tail-row SUM S (bracket from row count x print resolution if rows are counts; the fm bracket on the row sum if mass-type). Mean = S/rows.
 - P33 replica 2: identify the far-out scalar (dokkarm10(1)). If it is an extreme-value statistic (max/min), a Student band is invalid: exclude it with that as computed evidence and test the cells it feeds. If it is a mean, the run is gate 2's one allowance, not an exception.

3. Gate 2: delete the named-exception list. Gate = at most 1 failing run out of 96 (Binomial(96,1e-3): P(>=1)=0.09, P(>=2)=0.004). The one failing run, if any, is documented with its cells and date as the gate's allowance. Diagnostic (not a gate): recompute every threshold at per-cell level 0.05; the blind failure fraction over the 96 x m cell tests must lie in [0.03, 0.07].

4. Work list:
 1. merge the corrections (gates 1, 3 green; gate 2 open with the five findings);
 2. same-axis number histogram per fm* family? wire it; else bracket with eps from the print format; regime switch at mu=30;
 3. count rule on the NB engine; non-degeneracy: a candidate with N_lo > U goes red; print bracket, L, U for the five cells;
 4. TailRowMean -> tail-row sum under the count rule;
 5. classify P33 replica 2's scalar;
 6. remove the exception list; run gates 1, 2 (<= 1 failing run), 3 and the 0.05 diagnostic;
 7. gate 3: record per mutation which cells catch it and the boundary; if a mutation caught only by a deep-tail fm cell now escapes, add one merged tail-mass cell (fm summed beyond the last mu>=30 cell, count rule) rather than reverting;
 8. re-measure batched mode on P33 and HMX under the corrected criterion; a remaining excess is a finding about the batched Original derivation, not absorbed by the criterion.
