# BOOT.md — DispersionTool

## Purpose

The manual generator of `tests/Fixtures/dispersion.approved.txt` (root BOOT.md, "the
pass condition compares failure rates, not single runs"; `tests/Fixtures/API.md`, "##
Dispersion table"): a tripwire table, checked by diff, reporting what
`StatisticalCriterion.ReconstructArrayTotals`/`EstimateDispersion` compute for the
fixed families and the `fqdokkarm` rows of every reference formulation.

⚠ 2026-09-25: created this session. `DispersionTable.Compute` (`tests/Harness`, moved
there in the same commit) used to live as private methods
(`ComputeTable`/`AppendLine`) inside `tests/Harness.Tests/DispersionApprovedTests.cs`,
whose `Regenerate` was a `[Fact(Skip = "manual regeneration only")]` test — a
permanently skipped test, which AGENTS.md §13 forbids ("a perpetually red check is
worse than an absent one"). Moved out, run by hand, the same role `tests/Benchmarks`
and `tests/RateTableTool` already have; `tests/Harness.Tests/DispersionApprovedTests.MatchesTheCommittedTable`
(the actual check, unaffected) now reads the same implementation from its neighbour.

## Invariants

- **One implementation.** `DispersionTable.Compute` (`tests/Harness`) is the only place
  that builds the table; this node calls it and writes the file, never recomputing any
  part of it (root BOOT.md Taboos).
- **Nothing here is asserted.** Like `tests/Benchmarks` and `tests/RateTableTool`, this
  node writes a fixture; the assertion that reads it
  (`DispersionApprovedTests.MatchesTheCommittedTable`) lives in `tests/Harness.Tests`.

## Dependencies

- [Harness](../Harness/API.md) — `DispersionTable.Compute`, `RepositoryPaths`.

Outside the tree: none beyond the .NET SDK.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- A console project, not a test project: run by hand, never by `dotnet test`.
- `DispersionTable` stays `internal` in `tests/Harness`; this node reaches it through
  that node's own `InternalsVisibleTo`, the same pattern `tests/RateTableTool` uses for
  `ReferenceModeRunner`.

## Acceptance criteria

- [x] 2026-09-25: the tool builds and writes `tests/Fixtures/dispersion.approved.txt`,
      bit-identical to the file `DispersionApprovedTests.MatchesTheCommittedTable`
      already accepted (`dotnet run --project tests/DispersionTool`, then a diff against
      the pre-move file: no difference; `MatchesTheCommittedTable` was already green
      before and after, since the formula did not move, only its address).

## Taboos

- No second implementation of `DispersionTable.Compute`'s formula anywhere else in the
  tree.
- No assertion in this node: the comparison belongs to `tests/Harness.Tests`.
- No silent change of a committed fixture: a regeneration that moves the table is
  reported, not committed quietly (root BOOT.md Taboos).
