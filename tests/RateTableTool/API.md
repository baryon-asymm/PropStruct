# API.md — RateTableTool

A console project. It exposes no types to the tree beyond `InternalsVisibleTo` for
`tests/Simulation.Tests` (`ReferenceModeRunner`, `OriginalModelGuard`, `GuardCase`; BOOT.md,
Constraints); its public surface, such as it is, is the command that runs it and the files
it writes.

## Command line ✅

```console
$ dotnet run --project tests/RateTableTool -- rate-table [--parallelism N]
$ dotnet run --project tests/RateTableTool -- seed-zero-snapshot
$ dotnet run --project tests/RateTableTool -- original-model-guard
```

| Subcommand | Writes | Runs |
|---|---|---|
| `rate-table` | `tests/Fixtures/rate-table.json`, `tests/Fixtures/rate-runs/<formulation>/<layout>/<precision>/<seedIndex>.m.txt` | 320: five formulations × two layouts × two precision kinds × sixteen seeds |
| `seed-zero-snapshot` | `tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt` | 20: five formulations × two layouts × two precision kinds, seed 0 |
| `original-model-guard` | `tests/Simulation.Tests/Snapshots/OriginalModelGuard.approved.txt`: per case `<id> <SHA-256 of results.m> <SHA-256 of the result JSON>` | 34: the twenty seed-0 cases, the five references batched on the CPU, nine extra formulations (`tests/Simulation.Tests/BOOT.md`, "## The Original-model guard") |

`--parallelism N` (default: `Environment.ProcessorCount`) sets
`ParallelOptions.MaxDegreeOfParallelism` for `rate-table`'s `Parallel.For`; lower it if
a run fails for a reason `## Regeneration runs` (`BOOT.md`) does not already explain.
`seed-zero-snapshot` and `original-model-guard` run their cases sequentially (fast enough on its own that
parallelizing it was not worth the extra surface).

Both subcommands print progress to stdout and the fixture path they wrote; `rate-table`
reports any per-job failure to stderr and exits `1` without writing the file rather than
writing a table with holes in it.

## Side effects

Runs the simulator through `ReferenceModeRunner.RunSeed` (a temp file per run for
`results.m`, deleted after parsing) and writes the fixture(s) its subcommand names,
under `tests/Fixtures/` or `tests/Simulation.Tests/Snapshots/`. `rate-table` clears
`tests/Fixtures/rate-runs/` before writing it, and only after every one of the 320 jobs
has succeeded.

## Out of scope

- Comparing a run against a threshold or a replica set: `tests/Harness` (`StatisticalCriterion`)
  and its consumers (`tests/Harness.Tests/RateCriterionTests`,
  `tests/Simulation.Tests/StatisticalCriterionTests`).
- The schema of `rate-table.json`: documented in `tests/Fixtures/API.md`, "## Rate
  table", not exported from here as a type.
