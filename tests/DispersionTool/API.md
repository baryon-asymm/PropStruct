# API.md — DispersionTool

A console project. It exposes no types to the tree; its surface is the command that
runs it and the file it writes.

## Command line ✅

```console
$ dotnet run --project tests/DispersionTool
```

No arguments. Writes `tests/Fixtures/dispersion.approved.txt` and prints the path it
wrote.

## Side effects

Reads `tests/Fixtures/references/*/results.m.txt` and
`tests/Fixtures/replicas-lagged/*/*.m.txt` (through `tests/Harness`'s own
`DispersionTable.Compute`); writes `tests/Fixtures/dispersion.approved.txt`.

## Out of scope

- Comparing the table against a threshold or a committed file: `tests/Harness.Tests/DispersionApprovedTests`.
- The formula itself: `tests/Harness/DispersionTable.cs` and
  `StatisticalCriterion.ReconstructArrayTotals`/`EstimateDispersion`.
