# Decision IX — a cell is credited with the level of the term that decided it

**Author: the orchestrator, 2026-09-20, adopting an Opus 5 review's recommendation.**
Fable 5.1 is unavailable from this session (see decision VIII's withdrawal header). The
review that produced this decision was given the decision documents and the node, not the
reasoning that produced them, and its two decisive claims were verified against the source
before adoption. Reversible on the numbers item 4 asks for.

## What decision VIII's own check taught, which is not what it was asked

The check ran and passed: pooled over every `fqdokkarm` row and formulation, the bootstrap
medians of `rho_cell` by size decile are disjoint at the extremes — decile 1
`[0.0066, 0.0187]` against decile 10 `[0.00025, 0.00033]`, a fortythreefold decline. By
decision VIII's own literal criterion that reads "implement it".

It is not implemented, because the check could not have said anything else:

- the deciles are confounded with row identity. Decile 1 is made of deep tail rows and
  decile 10 of populous head rows, across five formulations, and rows genuinely differ in
  dispersion — decision V's own table routes `fqdokkarm` rows differently within one
  formulation. So a disjoint result means "different rows have different rho", which was
  already known and is already handled: `EstimateDispersion` is called once per row
  (`StatisticalCriterion.cs:289`) and once per array (`:436`), and each estimates at its
  own `n`;
- the bootstrap resamples cells of one histogram as if they were independent. They share
  a total, so they are negatively correlated by construction, and they are positively
  coupled through the very run-level drift `rho` exists to model. The band it prints is
  too narrow, which again favours disjointness.

**The lesson is recorded as a rule, not as a remark:** a check offered as able to withdraw
a decision must name, before it runs, the outcome that withdraws it *and* the reason that
outcome is reachable. Decision VIII named the first and not the second. This is the fifth
time in this node that a check could not fail; it is the first time the check was written
for that purpose in the same document as the decision it was meant to guard.

## The decision

`threshold = Math.Max(studentTerm, Math.Max(staticFloor, countFloor))`, and the
calibration curve credits each cell with the attained level of **one** term, chosen by
whether the count floor was computed — not by whether it governed. Every cell whose
threshold is set by a term other than the one it is credited with has a true exclusion
probability below the level the curve expects of it, so the curve reads short with no
error anywhere in any estimator. The code's own comment above that line already states the
intended rule — "classify by which mechanism actually governed the cell" — and the line
does not implement it.

1. **Carry the terms.** `CellVerdict` gains `StudentTerm`, `CountFloor` and `StaticFloor`
   as they were computed for that cell.
2. **Partition by the governing term**, among cells that are not already `degenerate`
   (that filter already removes the cells the static floor governs):
   - `countFloor > studentTerm` and an attained level is known: the Count row, expected
     level that cell's own `AttainedAlpha`;
   - otherwise, if `studentTerm >= countFloor`: the Student row, expected level `alpha`;
   - `countFloor > studentTerm` and no attained level is known (the `phi < 1` route and
     the cells above `MaxCountFloorTotal`, whose `AttainedAlpha` is discarded today): the
     cell enters **neither** row. It is counted and reported as its own population.
3. **`m` does not move.** Decision VI item 4 stands: eligibility and now governance
   restrict the calibration denominator only, never the Bonferroni budget. A cell that
   leaves a curve row was still charged its share of `m` when its interval was built.
4. **Re-run and report, before anything else is written:** gate 1, gate 3, decision VI's
   Check A and Check B, and the size of population 2c. If 2c is large, that is a finding
   in itself: it is a population whose interval is set by a term whose level nobody knows.

## Check B is re-specified in the same task, because its current form has no null

`CountTwoSidedTailProbability` is a doubled one-sided tail of a discrete law. Under any
correct discrete model such a statistic is stochastically **larger** than uniform:
`P(T <= u) <= u` for every `u`. Decision VI read a mean of 0.544 against 0.5 and concluded
"wide intervals"; that reading has no null, and the competing reading it was built to
exclude has none either.

The bound does give a check that can fail, and it fails today: after decision VII the
deciles begin `3.3e-8, 0.0026, 0.040, 0.136, 0.271`, where the bound requires the tenth
percentile to be at least 0.1 and the median at least 0.5. Roughly half the Count cells sit
below what any correct model permits — the count interval is grossly too *narrow* there —
while Check A says the intervals are too wide overall. Both readings are true only if the
count interval is not what decides those cells, which is the defect item 2 fixes.

Check B becomes: the fraction of cells violating `P(T <= u) <= u`, on the standard
Kolmogorov one-sided statistic against that bound, reported before and after item 2. It is
expected to fall. If it does not, item 2's diagnosis is wrong and this decision is
withdrawn in its turn.

## What is not done in this task, and is now on the work list

- **The array total is never tested.** The count rule conditions on the candidate's own
  reconstructed total (`nStar = candidateTotal`), so a port whose histogram has the right
  shape and the wrong normalisation is judged at its own `n` and passes. `p_hat` comes from
  the replicas, so shape is tested; the total is not, anywhere. This is a whole channel
  without a check, and it is where a real porting defect would most plausibly live.
- **The machinery is monotone toward passing.** `threshold` is built only out of
  `Math.Max`, and every fallback in this node is justified as "conservative", meaning
  wider. In a test whose conclusion is "the port agrees", wider is easier to accept, so
  losing power is losing the validity of the claim, not only its sharpness. No rule may be
  added to this node from now on without stating, in the same paragraph, what it makes
  able to fail.
- **A third of HMX's cells never compare at all** (`compared=4376 excluded=2117`), by
  structural canonical-axis drops rather than by the exclusion list, so the root taboo
  "no entry in the exclusion list without computed evidence" never engages on them. The
  tail-coverage measurement already names one mutation never caught at any magnitude.
- **Three gates are permanently red and all three are `Category=Long`**, so no guarded
  merge sees them. That is honest reporting of an unfinished criterion, and it is also
  AGENTS.md §13's "a perpetually red check is worse than an absent one" holding for three
  checks at once. The resolution is a design decision, not a coding one.

## Order

1. item 2, with items 1 and 3, and the report of item 4 including the re-specified Check B;
2. then, on those numbers, whether anything about dispersion is still wrong;
3. then the array-total rule;
4. tail pooling, the `TailRowMean` rewrite and the cumulative mass test last, as before.
