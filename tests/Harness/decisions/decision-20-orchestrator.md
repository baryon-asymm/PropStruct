# Decision XX — stop conditioning on a total that does not predict the count

**Author: the orchestrator, 2026-09-20.** The first change to the criterion since decision
IX, and the first proposed on a refutation rather than on a hypothesis.

## What is established

- the quantity four decisions called a misplaced centre is `1 + corr(n, r) CV_n CV_r`,
  exactly (decision XVII, `3.33e-16` over 1090 cells);
- that correlation's expectation under `k | n ~ BetaBinomial(n, p, rho)` is zero;
- simulated from that model at the observed totals, the measured value is refuted in eight
  of nine groups at `p = 0`, `z` from −4.2 to −39.9 (decision XIX), on a gate that passed
  the ninth group at `p = 0.924` and so is known to be able to do both;
- it is not sampling noise: every group's observed median `|corr|` exceeds its own null's
  by 1.34 to 4.35 times.

`E[k|n] = n p` is false for these quantities. The total is not a nuisance parameter that
conditioning removes; it is a number that carries no reliable information about the count.

## The change

**The count-like cells now governed by the beta-binomial take the unconditional count
predictive instead** — `NegativeBinomialInterval(sum of the replicas' own counts, R, alpha)`,
the Poisson-Gamma predictive already written, already tested, already proven non-degenerate,
and already governing the 40.7 % of Count-governed cells that `phi < 1` routes to it.

This is a change of routing. No formula is added, none is modified, and the beta-binomial
stays where it is for whatever the measurement below leaves to it.

## The danger, named before the numbers

The unconditional predictive ignores `n*` entirely. Where the count does partly follow the
total, it absorbs that variation into the count's own spread and comes out **wider** — and
wider, in a test whose conclusion is "the port agrees", means easier to accept. Every defect
found today widened the interval. This change must not be the next one.

So the check is two-sided, and both arms are pre-registered:

1. **Check B must at least halve.** It is the direct measure of whether the law fits — the
   fraction of cells violating `P(T <= u) <= u`, at 73.96 % today. A model that fits will
   drop it sharply; a model that merely widens will not, because a wider interval moves
   tail probabilities toward the middle without making the law right;
2. **the interval width must not grow systematically without Check A following it.** Report
   the median width ratio, unconditional against conditional, per family, beside Check A. If
   the widths grow and Check A falls further from 1.0, the change has bought fit with
   permissiveness and is **withdrawn**.

Report gate 1 and gate 3 as context. Gate 1 moving is itself information: these cells'
verdicts are what the acceptance claim is made of.

## If it is withdrawn

The standing alternative is the middle road this decision deliberately does not take: a rate
estimated at the candidate's own total from a relation fitted across replicas, pooled per
family because decision XVIII showed a per-cell fit has no power. It is more machinery and a
new formula, which is why it is second in line and not first.
