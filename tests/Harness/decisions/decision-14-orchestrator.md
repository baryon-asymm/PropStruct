# Decision XIV — the cutoff, re-argued on the evidence that survives

**Author: the orchestrator, 2026-09-20.** This is decision XII's change, proposed again on
one of its two original justifications, after the other was refuted. It is written as a new
decision rather than as an exception to the old one: a condition rewritten after seeing the
number it failed is how a criterion stops being able to refuse, and this node has paid for
that five times.

## What survived the withdrawal

Decision XII rested on two legs. The second — that the small-`m` population carries the
centre defect — is refuted: the defect lives at `m >= 10`, in one family, untouched by the
cutoff (decision XV). The first is untouched by that:

- a run prints `k_j / N`; the inferred quantum is `gcd_j(k_j) / N`; so a shared divisor
  makes the reconstructed total `N / g`, losslessly and undetectably;
- the probability is closed-form in `m` alone, `sum over primes p of p^-m`, and was measured
  against the tree's own data where the formula says it lives: 50 % observed against 45.2 %
  predicted at `m = 2`, a ratio of 1.11;
- the cost is an interval wider by `sqrt(g)`, measured on a mutation built as exact
  division: 1.368 against 1.414 at `g = 2`, 1.611 against 1.732 at `g = 3`.

None of that depends on the centre. An interval built on a total that is a small-integer
fraction of the true one is wrong whatever the centre does.

## The decision

Unchanged from decision XII: a cell takes the count rule only when its unit's quantum was
fitted from at least seven non-zero resolvable cells, seven being where the closed-form
probability falls below one per cent; below it, the ordinary Student band, whose level is
known. `m` is the unit's own median across the replicas that produced a total. Decision X's
array-total rule inherits it.

## The check, which is this decision's own and not decision XII's

The claim is that the intervals on small-`m` units are too wide by an unknown factor. That
is a statement about **those cells**, so it is checked on those cells, not on the curve as a
whole:

**Before the change, measure the small-`m` population's own exclusion rate against its own
attained level, and the same for the large-`m` population.**

- the small-`m` cells exclude markedly less often than their level says, while the large-`m`
  cells do not: the artefact is doing what the derivation says, and removing those cells is
  right;
- both populations exclude at the same relation to their level: the artefact, though real in
  the reconstruction, does not reach the verdicts, and **this decision is withdrawn** — the
  cutoff would then be removing cells for a defect that costs nothing.

Check A and gate 3 are reported as context, not as the condition. They moved the right way
last time (0.5399 to 0.6435, 9 violations to 8) and that was not enough to keep the decision
then; it is not enough to make one now.
