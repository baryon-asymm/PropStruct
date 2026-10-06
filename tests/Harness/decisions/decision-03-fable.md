# Fable 5.1 decision III, 2026-09-20 (the discrete rules and the calibration gate)

1. COUNT RULE: not mid-p (anti-conservative exactly in the small counts these cells are
   made of, and it changes the port's verdicts to please the gate - the same move as
   loosening a tolerance), not randomization (the root invariant forbids it, and a
   borderline pass becomes a coin flip), and not a plain one-sided gate (it can no longer
   see a reconstruction that is over-conservative for a WRONG reason: over-dispersed
   count, mis-reconstructed integer).
   Instead: gate the count rule against its ATTAINED level, not the nominal. The NB CDF is
   already evaluated to place the integer boundary; record per cell the tail mass actually
   excluded, average it over the non-degenerate cells, take the binomial band of that mean.
   Deterministic, two-sided, separates discreteness (explained) from defects (unexplained).
   The gate asserts: the count rule's blind failure fraction lies in the binomial band of
   its mean attained level at 0.05, 0.01 and 0.001, AND the attained level never exceeds
   the nominal. If the attained-level bookkeeping proves too costly, fall back to the
   one-sided gate, declared in tests/Harness/BOOT.md as the weaker gate it is.
2. MASS RULE: a rule that consults no level has no point on the curve but still has a size,
   and the family-wise alpha claim covers it. N=0 must not read as "pass": the gate must
   distinguish "no level" from "no cells" and assert, one-sided, that the mass rule's blind
   failure fraction is at or below the upper binomial limit of the per-cell Bonferroni
   level - otherwise the bracket contributes failures the alpha bound does not account for.
   It sits outside the curve, inside the gate, declared as a deterministic containment
   check, not a test. When the cumulative mass test lands, the mass cells join the curve and
   the bracket assertion is retired.
3. GATE DEFINITION CHANGES:
   (i) attained level for discrete rules, as above;
   (ii) "non-degenerate cell" defined per rule and machine-counted, never typed;
   (iii) N = 0 for any rule is an ERROR of the gate, not a green reading - a check that has
        never been red;
   (iv) the 0.001 point per formulation has almost no power ([0,10] over 96 runs): gate 0.05
        and 0.01 per formulation, and 0.001 POOLED over the five;
   (v) state explicitly that the curve calibrates the rule's TAIL SHAPE; the acceptance
       level alpha/m ~ 1e-7 is an extrapolation from it and 0.001 is the nearest evidence;
   (vi) P33 at 0.001 stays red until the row-count cell lands: declared under AGENTS.md §12
        with the lifting condition named, never by dropping the two contaminated replicas.
4. ORDER: neither red blocks the list. Row-count cell first (it lifts the only substantive
   red), the attained-level gate alongside it; tail pooling and TailRowMean alter the
   Student rule and must re-run the curve; the cumulative mass test lands the Mass rule on
   the curve. Nothing merges with the gate red and undeclared.
