# Fable 5.1 decision V, 2026-09-20 (the dispersion diagnostic was unconditional)

1. THE HYPOTHESIS IS RIGHT, ARITHMETICALLY. With k_i | n_i ~ Bin(n_i, p) and n_i varying
   across replicas, Var(k) = E[n]·p(1-p) + p^2·Var(n), so the UNCONDITIONAL index is
   (1-p) + p·Var(n)/E[n]. Anything above binomial is p times the dispersion index of the
   row total, and for fqdokkarm rows on a per-run axis that index is large by
   construction. "65" says nothing about the conditional law; it says n varies.
   It also disposes of the implementer's objection: the beta-binomial is not "narrower
   than Poisson", it is CENTRED on n_ref·p_hat instead of on the replicas' mean k, and
   moving the centre is what removes the n-driven failures. Width is not the criterion;
   calibration is.
   One conditional overdispersion IS expected and must be measured, not assumed away:
   QKS1 feeds the pocket histogram back into the neighbour loop, so a run's p drifts
   within the run - exactly the mixing the beta-binomial's rho models.
   RE-MEASURE, per count cell: p_hat = sum k_i / sum n_i over replicas;
   phi = (1/(R-1)) * sum (k_i - n_i p_hat)^2 / (n_i p_hat (1 - p_hat)); pooled by family
   and configuration, on canonical axes for the 2-D family, with cells whose n_i p_hat < 5
   pooled with their neighbours (the tail-pooling rule brought forward).
   Read: phi ~ 1 -> binomial conditionally, rho = 0; phi > 1 -> beta-binomial with
   rho_hat = (phi - 1)/(n_bar - 1); phi < 1 -> underdispersed, no member of the family fits.
2. ONE PREDICTIVE FAMILY, ONE RULE, A MACHINE-ESTIMATED PARAMETER. Every count cell gets
   the beta-binomial conditioned on the reference's own n, with rho_hat = max(0, (phi-1)/
   (n_bar-1)) estimated from the replicas per family and configuration. rho = 0 IS the
   binomial, so the two families already in the binomial window need no special case. The
   choice is not per family; only the parameter is, and it is computed at test time from
   the fixtures, never typed. It is also emitted as a generated table
   (`dispersion.approved.txt` in Fixtures) checked by diff - the same tripwire pattern as
   the surface snapshot and ORIGINAL-DEFECTS.md.
3. fqmkm1 AT 0.01-0.18 is not a count in behaviour: the run's data pins it nearly
   deterministically, and a binomial band there could never fail - a check never seen red
   (AGENTS.md §13). Rule: when phi < 1 by the same computation that routes the parameter,
   the cell LEAVES the count predictive and takes the continuous Student band on k itself,
   with the print-resolution floor. The routing is computed from phi and lives in the same
   generated table, not in a list.
4. GATE 1's CELL STANDS, PROVISIONALLY: five replicas at exactly 0 give no dispersion
   estimate, and no rho the replicas can support moves a tail of 6e-13 across alpha/m. It
   is re-read once under tail pooling, because the pooled neighbourhood, not the single
   zero column, is the right unit; only then is it final. GATE 2: the 3 tail-row/small-mass
   count cells are re-read under the new predictive; the other 10 are untouched by this.
5. ORDER: (1) the conditional diagnostic, one script over the existing quantum inference;
   (2) beta-binomial with rho_hat and the phi<1 routing, plus the generated dispersion
   table, then re-run: the original's reference must still pass its lagged replicas with no
   failure, and the count rule must sit INSIDE its attained-level band, not below;
   (3) tail pooling - a pooled cell is just another count with its own n, so it needs (2);
   (4) the TailRowMean rewrite; (5) the cumulative mass test. Gates are re-read after (3),
   not before.
