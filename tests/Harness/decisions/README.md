# Decisions of the statistical criterion

The design record of this node's criterion, committed 2026-09-20 because it was not.

Until today these documents lived only in a session scratch directory and the node cited
them as `SCRATCH/...`, a path that resolves on no machine but the one that wrote it and
not for long there. An Opus 5 review of the branch named it: the provenance of decisions
II to V — the rules the acceptance criterion is built from — was outside the repository.
The citations now point here.

They are **provenance, not truth**. What is currently true about the criterion is in
`../BOOT.md` and in the code; these are the arguments that produced it, including the ones
that were wrong. Several decisions here were withdrawn by their own pre-registered checks,
and each says so in its own text. Nothing in this directory is to be read as a live
specification, and nothing here is maintained: a document that has been overtaken is left
as written, because its value is that it records what was believed when a change was made.

| File | Author | Outcome |
|---|---|---|
| `decision-01-fable.md` … `decision-05-fable.md` | Fable 5.1 | the calibration design: the curve, the attained-level banding, the routing |
| `decision-06-orchestrator.md` | the orchestrator | per-level eligibility; refuted by its own two checks |
| `decision-07-orchestrator.md` | the orchestrator | the robust dispersion estimator; stands |
| `decision-08-orchestrator.md` | the orchestrator | dispersion pooled by cell size; **withdrawn before implementation** |
| `decision-09-orchestrator.md` | the orchestrator | a cell is credited with the level of the term that decided it; stands |
| `decision-10-orchestrator.md` | the orchestrator | the array total is compared, not only conditioned on; unimplemented |
| `decision-11-orchestrator.md` | the orchestrator | judge against the region, not a symmetric band; **withdrawn by its own condition** |
| `decision-12-orchestrator.md` | the orchestrator | a cell-count cutoff for the count rule; **withdrawn by its second arm** |
| `decision-13-orchestrator.md` … `decision-19-orchestrator.md` | the orchestrator | the measurements that located the defect: the centre, the correlation, the elasticity, the simulated null |
| `decision-20-orchestrator.md` | the orchestrator | stop conditioning on the total; stands |
| `harness-tail-report.md`, `hmx-exclusions.md`, `check_tailmean_outliers.md` | earlier waves | the working notes the node's own prose cites |

The arbiter's answer of 2026-09-20, which set the order of work after decision XX, is in
`../HISTORY.md` rather than here, because it arrived as design input to a task and is dated
there with the measurements it prompted.
