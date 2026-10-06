# BOOT.md — RateTableTool

## Purpose

The one place that runs a reference-mode simulation, seed by seed, and turns its
`results.m` into a hash and a criterion verdict — and the two generators built on that
one implementation: `tests/Fixtures/rate-table.json` (root BOOT.md, "the pass condition
compares failure rates, not single runs") and
`tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt` (that node's own
change-detector test, `SeedZeroResultsMMatchesApprovedSnapshot`).

Both files used to be written by `[Fact(Skip = "manual regeneration only")]` tests
inside `tests/Simulation.Tests` (`RateTableGenerator.Regenerate` and
`StatisticalCriterionTests.RegenerateSeedZeroSnapshot`). AGENTS.md §13 forbids a
permanently skipped check ("a perpetually red check is worse than an absent one"), and
while neither one was ever a check — a generator asserts nothing — a `[Fact(Skip = ...)]`
that has stood since before this node existed reads the same way to anyone scanning
`dotnet test`'s output, and running 320 reference-mode simulations inside the ordinary
test pass would make every guarded merge pay for them regardless. Moved here, run by
hand, exactly like `tests/Benchmarks` (BOOT.md, Constraints): a console project
`dotnet test PropStruct.sln` never selects.

⚠ 2026-09-25: created this session, splitting `ReferenceModeRunner`, `RateTable`/
`RateTableRun` and the two generators out of `tests/Simulation.Tests`, where all three
previously lived (`tests/Simulation.Tests/BOOT.md`, "## Statistical criterion
measurement (2026-09-24)"). `ReferenceModeRunner` stays `internal`, as it was there;
`tests/Simulation.Tests` keeps using it for its own one remaining assertion
(`SeedZeroResultsMMatchesApprovedSnapshot`) through this node's `InternalsVisibleTo`.

## Invariants

- **One runner.** `ReferenceModeRunner.RunSeed` is the only place that constructs a
  `Simulator`, runs one reference-mode seed and parses its `results.m`; both
  generators and the neighbour's own snapshot test call it, never a second copy (root
  BOOT.md Taboos).
- **Nothing here is asserted.** Like `tests/Benchmarks`, this node writes fixtures;
  the assertions that read them live in `tests/Harness.Tests` (the rate criterion) and
  `tests/Simulation.Tests` (the seed-0 snapshot).
- **A job's failure does not lose the batch.** The rate table's 320 runs share one
  `Parallel.For`; a job that throws one of `Input`'s or `Simulation`'s own documented
  failure modes is recorded and reported, not left to crash the whole process on a
  background thread — the failure mode a prior attempt at this generator hit five
  times as a `dotnet test` host crash before it was moved here (see "## Regeneration
  runs" below).
- **`rate-table` also writes `rate-runs/`** (decided 2026-09-27, `tests/Fixtures/API.md`,
  "## Rate table"): the same 320 runs' own `results.m` text (`ReferenceModeRunner.Run`'s
  own `NormalizedText`, time line stripped), byte for byte, so `CompareSets`
  (`tests/Harness/BOOT.md`, "## Set comparison") has candidates without re-running the
  simulator. Written only once every one of the 320 jobs has succeeded, after first
  clearing the directory, so a failed regeneration never leaves a stale, partial set.

## Dependencies

- [Simulation](../../src/Simulation/API.md) — `Simulator`, `SimulationOptions`,
  `ExecutionMode`, `StreamLayout`, `PrecisionKind`.
- [Input](../../src/Input/API.md) — `Formulation`, `DatFile`.
- [Execution](../../src/Execution/API.md) — `AcceleratorKind`.
- [Output](../../src/Output/API.md) — `ResultsMWriter`, `ModelParameters`.
- [Harness](../Harness/API.md) — `RepositoryPaths`, `ResultsMFile`, `StatisticalCriterion`,
  `ReplicaKind`.
- [Fixtures](../Fixtures/API.md) — the reference formulations (read directly by file
  path, the same convention `tests/Simulation.Tests` and `tests/Benchmarks` already use).

Outside the tree: none beyond the .NET SDK.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- A console project, not a test project: run by hand, never by `dotnet test`, so that
  no CI run and no fast set ever waits on 320 reference-mode simulations.
- `ReferenceModeRunner` and `RateTable`/`RateTableRun` stay `internal`: the schema of
  `rate-table.json` is documented in prose (`tests/Fixtures/API.md`, "## Rate table"),
  not exported as a type, exactly as it was when these types lived in
  `tests/Simulation.Tests` — moving them does not turn a documented-schema file into a
  shared-type contract.
- Two subcommands, one process: `rate-table` and `seed-zero-snapshot` share
  `ReferenceModeRunner` rather than existing as two separate tools, since a second
  console entry point would still need the same runner and duplicating the project
  scaffolding around it buys nothing.

## Acceptance criteria

- [x] 2026-09-25: the tool builds and both subcommands write their fixture (`## Regeneration runs` below).
- [x] 2026-09-25: the rate table's verdicts (`FailingCells`/`FailingNames`/`ResultSha256` per
      run) are identical to the table committed before this node existed, on every one of
      the 320 rows, once the `PrecisionKind.Binary64` rename is read through (`Original` →
      `Original` unaffected, `Double` → `Binary64` in the `Precision` field only). Evidence:
      this node's own regeneration run (`## Regeneration runs` below), diffed field by field
      in Python against the pre-move table — zero verdict differences, zero `ResultSha256`
      differences, over all 320 runs.
- [x] 2026-09-27: `rate-table` writes 320 files under `tests/Fixtures/rate-runs/`, one
      per (formulation, layout, precision, seed index), and the regeneration that added
      it moves no verdict: zero `FailingCells`/`FailingNames`/`ResultSha256` differences
      against the pre-run table (`## Regeneration runs` below).

## Taboos

- No second implementation of `ReferenceModeRunner.RunSeed`'s "run this seed and parse
  the result" anywhere else in the tree.
- No assertion in this node: a threshold or a comparison belongs to `tests/Harness.Tests`
  or `tests/Simulation.Tests`, which read what this node writes.
- No silent change of a committed fixture: a regeneration that moves a verdict is
  reported, not committed quietly (root BOOT.md Taboos).

## Regeneration runs

Filled in as each subcommand is run and its output reviewed; this section is the record
`tests/Fixtures/BOOT.md`'s and `tests/Simulation.Tests/BOOT.md`'s own acceptance
criteria point back to.

**2026-09-25, `seed-zero-snapshot`.** Debug build (the default; this subcommand's own
20 runs are cheap enough not to need Release). `dotnet run --project tests/RateTableTool
-- seed-zero-snapshot` wrote `tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt`;
diffed against the pre-move file, every one of the twenty SHA-256 hashes is bit-identical,
the only change being the `Double` → `Binary64` label text (`PrecisionKind.Binary64`
rename, CA1720) and the header comment naming the new location. Round-trip verified the
same day: `dotnet test tests/Simulation.Tests --filter
FullyQualifiedName~SeedZeroResultsMMatchesApprovedSnapshot` — 20/20 passing, 17 m 56 s
(Debug; the theory re-derives every hash and compares it against what the generator just
wrote, so this is also this node's own non-degeneracy proof of the generator against the
theory that reads it — a mismatch anywhere would fail one of the twenty cases).

**2026-09-25, `rate-table`.** Release build (`dotnet build tests/RateTableTool -c
Release`, 0 warnings). `dotnet run -c Release --project tests/RateTableTool -- rate-table`
(default parallelism, 16, the reference machine's logical CPU count) wrote
`tests/Fixtures/rate-table.json`: **320 runs in 16 m 39 s, no crash** — the failure mode
the prior attempt at this generator hit five times as a `dotnet test` host crash (`tests/Harness/BOOT.md`,
the now-lifted CA1859/CA1062 exemption) did not reproduce once the generator moved out of
the test host into its own process. 144 of 320 runs failed at least one cell (pooled),
matching the previously committed table's 23 + 121 exactly. Diffed field by field in
Python against the pre-move table (`Formulation`/`Layout`/`SeedIndex`, `Precision` read
through the rename): 320 of 320 runs matched by key, zero differences in `FailingCells`,
zero in `FailingNames`, and **zero in `ResultSha256`** — the `results.m` outputs
themselves, not merely the verdicts drawn from them, are bit-identical to the pre-move
table. `CriterionSha256` and `SnapshotSha256` changed, as expected: `StatisticalCriterion.cs`
and the seed-0 snapshot both changed the same day. `tests/Harness.Tests/RateCriterionTests`,
all four facts, pass against the regenerated table.

**2026-09-26, `rate-table` (audit finding R3).** Release build (`dotnet build
tests/RateTableTool -c Release`, 0 warnings). `StatisticalCriterion.cs` was split by
concern into six files, one partial class (`tests/Harness/BOOT.md`, "## Null rate of
the original ... The tie"; `tests/Harness/API.md`), and the tie's own hash rule
generalized from one file's SHA-256 to a digest over every `StatisticalCriterion*.cs`
file (`tests/Fixtures/BOOT.md`, "## Rate table"). `dotnet run -c Release --project
tests/RateTableTool -- rate-table` (`PROPSTRUCT_NO_CUDA=1`, default parallelism, 16
logical CPUs) wrote `tests/Fixtures/rate-table.json`: **320 runs in 11 m 20 s, no
crash**, 144 of 320 failing at least one cell (pooled) — the same 23 + 121 as every
prior run of this table. Diffed field by field in Python against the pre-split table
(`Formulation`/`Layout`/`Precision`/`SeedIndex` as the key): 320 of 320 runs matched,
**zero differences in `FailingCells`, zero in `FailingNames`, zero in `ResultSha256`**
— the split changed no behaviour anywhere in the criterion's own verdicts, exactly as
a pure refactor requires (root BOOT.md Taboos: no second implementation of a formula).
Only `CriterionSha256`, `GeneratedAtUtc` and `Commit` changed; `SnapshotSha256` did not
(the seed-0 snapshot was untouched by this task). `tests/Harness.Tests/RateCriterionTests`,
all four facts, pass against the regenerated table.

**2026-09-27, `rate-table` (dangling-citation sweep).** Release build (`dotnet build
tests/RateTableTool -c Release`, 0 warnings). The sweep repointed and re-worded prose
citations across the tree, including comments inside `tests/Harness/*.cs` and
`tests/Harness.Tests/*.cs` — form only (AGENTS.md §8), no formula, threshold or
assertion touched, but enough to move `CriterionSha256`, the digest over every
`StatisticalCriterion*.cs` file (`tests/Fixtures/BOOT.md`, "## Rate table"). Backed up
the pre-run table first and confirmed `tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt`
carried no pending change. `PROPSTRUCT_NO_CUDA=1 dotnet run -c Release --project
tests/RateTableTool -- rate-table` (default parallelism, 16 logical CPUs) wrote
`tests/Fixtures/rate-table.json`: **320 runs in 11 m 29 s, no crash**, 144 of 320
failing at least one cell (pooled) — the same 23 + 121 as every prior run of this
table. Diffed field by field in Python (`Formulation`/`Layout`/`Precision`/`SeedIndex`
as the key) against the pre-sweep table: 320 of 320 runs matched by key, **zero
differences in `FailingCells`, zero in `FailingNames`, zero in `ResultSha256`**.
`SnapshotSha256` and `FixturesSha256` are unchanged, confirming the comment sweep
touched neither the snapshot nor the fixture data; only `CriterionSha256`,
`GeneratedAtUtc` and `Commit` moved. `tests/Harness.Tests/RateCriterionTests`, all
facts, pass against the regenerated table.

**2026-09-26, `rate-table` (`FixturesSha256` added).** Release build (`dotnet build
tests/RateTableTool -c Release`, 0 warnings). A third digest, `FixturesSha256`, was added
to the schema (`tests/Fixtures/BOOT.md`, "## Rate table"): the criterion's own comparisons
also read `exclusions.json`, `references/` and the replica directories, and none of that
was in any digest before. `dotnet run -c Release --project tests/RateTableTool --
rate-table` (`PROPSTRUCT_NO_CUDA=1`, default parallelism, 16 logical CPUs) wrote
`tests/Fixtures/rate-table.json`: **320 runs in 11 m 44 s, no crash**, 144 of 320 failing
at least one cell (pooled) — the same 23 + 121 as every prior run. `git diff` against the
pre-existing table touches only five lines: `GeneratedAtUtc`, `Commit`, and the new
`FixturesSha256` line; `CriterionSha256`/`SnapshotSha256` and all 320 `Runs` entries are
byte-identical, confirming the new field changed no verdict. `tests/Harness.Tests/RateCriterionTests`,
all six facts, pass against the regenerated table; the new
`ReplicaKindsUsedByRateRunsAccountForEveryDeclaredReplicaKind` guard is proven
non-degenerate the same way `ExclusionListNamesExactlyTheHarnessFilesThatStillExistUnderThoseNames`
already is for the criterion digest — a one-byte edit to `exclusions.json`, and separately
to one replica file (`replicas-lagged/HPEPA3/1.m.txt`), each turns
`RateTableTiesToTheCurrentCriterionAndSnapshot` red on the new `FixturesSha256` assertion
alone, reverted after, `git diff` clean.

**2026-09-27, `rate-table` (E1, the feasible mass-quantum interval).** Release build
(`dotnet build tests/RateTableTool -c Release`, 0 warnings). `MassFamilyRule.TryBracket`
moved from a point-estimate quantum to a feasible interval bounded by every calibrating
cell's own count bounds (`tests/Harness/HISTORY.md#e1-mass-bracket-feasible-interval`).
Before regenerating, dumped every run's per-`Compare`/`TailRowMean`/`Adaptive` report
`Compared`/`Excluded` counts and failing-cell name set, keyed by
formulation|layout|replica|report, from the committed pre-E1 table
(`before.txt`/`before.txt.counts`, working files, not in the tree); confirmed
`tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt` carried no pending
change. `PROPSTRUCT_NO_CUDA=1 dotnet run -c Release --project tests/RateTableTool --
rate-table` (default parallelism, 16 logical CPUs) wrote `tests/Fixtures/rate-table.json`:
**320 runs in 16 m 9.2 s, no crash**, **141 of 320 failing at least one cell** (pooled;
was 144 = 23 + 121, now 21 `Original` + 120 `Binary64`). Dumped the same keyed counts
from the regenerated table (`after.txt`/`after.txt.counts`) and diffed both files in
Python against the pre-E1 dump: `Compared`/`Excluded` are byte-identical on every one of
the 960 report rows (condition iii); the failing-cell-name diff removes exactly five
entries and adds none -- `HMX|Independent|replica 13|fmkarm_cor[84]`,
`HPEPA3|Lagged|replica 26|fmkarm[66]`, `P33|Independent|replica 14|fmkarm_cor[14]`,
`P33|Lagged|replica 11|fmkarm_cor[12]`, `inpt|Lagged|replica 10|fmkarm_cor[6]` -- all
five Mass-rule cells the feasible interval now contains that the point estimate rejected
(condition ii); no other run's failing set moved. `SnapshotSha256` and `FixturesSha256`
are unchanged; only `CriterionSha256`, `GeneratedAtUtc` and `Commit` moved, as expected
for a criterion-code change. `tests/Harness.Tests/RateCriterionTests`, all facts,
pass against the regenerated table, including the tie.

**2026-09-27, `rate-table` (E2, the adaptive index-matched comparison's own common
range).** Release build (`dotnet build tests/RateTableTool -c Release`, 0 warnings).
`AdaptiveIndexMatchedComparison` moved from scoring the union of every source's own
adaptive length (a row a source lacks compared as an impossible `0.0`) to the common
range every contributing source reaches, narrowing the rest into `Excluded`
(`tests/Harness/HISTORY.md#e2-adaptive-common-range`). Backed up the pre-run table
first and confirmed `tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt`
carried no pending change. `PROPSTRUCT_NO_CUDA=1 dotnet run -c Release --project
tests/RateTableTool -- rate-table` (default parallelism, 16 logical CPUs) wrote
`tests/Fixtures/rate-table.json`: **320 runs in 10 m 52.4 s, no crash**, **135 of 320
failing at least one cell** (pooled; was 141 = 21 `Original` + 120 `Binary64`, now 18
`Original` + 117 `Binary64`). Diffed field by field in Python
(`Formulation`/`Layout`/`Precision`/`SeedIndex` as the key) against the pre-E2 table:
320 of 320 runs matched by key; 7 runs changed. Six of them — P33, seeds 3/4/14, both
precisions — each lost both `dokkarm10@adaptive[0]` and `dokkarm43@adaptive[0]` and had
no other failing cell, so all six flip from failing to clean (the 6-run drop above).
The seventh, HPEPA3 `Original`/`Original` seed 1, was already failing and stays
failing (`FailingCells` 3 → 2): it loses `dokkarm10@adaptive[7]`/`dokkarm43@adaptive[7]`
but gains `Dkarmcat@adaptive[5]` — the design's own declared residual, a displaced
boundary exposed once the narrower comparison shrinks `m` (`tests/Harness/HISTORY.md#e2-adaptive-common-range`),
not a new defect. `SnapshotSha256` and `FixturesSha256` are unchanged; only
`CriterionSha256`, `GeneratedAtUtc` and `Commit` moved. `tests/Harness.Tests/RateCriterionTests`,
all facts, pass against the regenerated table, including the tie.

**2026-09-27, `rate-table` (`CompareSets`, rate-runs added).** Release build (`dotnet
build tests/RateTableTool -c Release`, 0 warnings). `SetComparison`/`CanonicalAxisSetCells`/
`SetCriterionReport` (`tests/Harness/BOOT.md`, "## Set comparison") joined the criterion
digest's own exclusion list (no per-run verdict reaches them); `TwoSampleComparison`,
`CategoryAxis` and `AdaptiveIndexMatchedComparison` gained new members
(`ConstantCellRule`, `PrefixMatchLengths`, `AdaptiveRowCountOf`/`LastBoundaryLogOf`) that
move `CriterionSha256` without changing any comparison `Compare`/`CompareTailRowMean`/
`CompareAdaptiveIndexMatched` runs. This run also writes `rate-runs/` for the first time
(`tests/Fixtures/API.md`, "## Rate table"). `PROPSTRUCT_NO_CUDA=1 dotnet run -c Release
--project tests/RateTableTool -- rate-table` (default parallelism, 16 logical CPUs) wrote
`tests/Fixtures/rate-table.json` and 320 files under `tests/Fixtures/rate-runs/`: **320
runs in 13 m 19.3 s, no crash**, **135 of 320 failing at least one cell** (pooled,
unchanged from the pre-run table). Diffed field by field in Python
(`Formulation`/`Layout`/`Precision`/`SeedIndex` as the key) against the pre-run table:
320 of 320 runs matched by key, **zero differences in `FailingCells`, zero in
`FailingNames`, zero in `ResultSha256`** — the refactor moved no verdict anywhere.
`SnapshotSha256` and `FixturesSha256` are unchanged; only `CriterionSha256`,
`GeneratedAtUtc` and `Commit` moved, as a pure code refactor requires.
`tests/Harness.Tests/RateCriterionTests`, all six facts, pass against the regenerated
table, including the tie.

**2026-09-28, `rate-table` (F-c, `PrintResolution.ExponentOf`'s power-of-ten fix).**
Release build (`dotnet build tests/RateTableTool -c Release`, 0 warnings).
`PrintResolution.ExponentOf` moved from `ceil(log10(|value|) - 1e-9)` to
`floor(log10(|value|) + 1e-9) + 1` (`tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`).
Backed up the pre-run table first (`rate-table-before.json`) and confirmed
`tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt` carried no pending
change. `PROPSTRUCT_NO_CUDA=1 dotnet run -c Release --project tests/RateTableTool --
rate-table` (default parallelism, 16 logical CPUs) wrote `tests/Fixtures/rate-table.json`:
**320 runs in 14 m 33.9 s, no crash**, **135 of 320 failing at least one cell** (pooled,
unchanged — the fix removes a cell from one already-failing run, not a whole run).
Diffed field by field in Python (`Formulation`/`Layout`/`Precision`/`SeedIndex` as the
key) against the pre-run table: 320 of 320 runs matched by key, **zero differences in
`ResultSha256`** across all 320 — the fix touches no `results.m` output, only the
criterion's own reading of it. Exactly one run changed: P33 `Independent`/`Original`
seed 13 loses `fqkarm[62]` (`FailingCells` 2 → 1) and keeps `fqkarm[63]`, matching the
mechanism in the HISTORY entry above. `tests/Fixtures/rate-runs/` is byte-identical
(`git diff --stat` empty). `SnapshotSha256` and `FixturesSha256` are unchanged; only
`CriterionSha256`, `GeneratedAtUtc` and `Commit` moved.
`tests/Harness.Tests/RateCriterionTests`, `SetCriterionTests` (52/52) and
`CalibrationCurveTests` (unchanged 7-row ratchet) pass against the regenerated table,
including the tie.

**2026-10-03, `seed-zero-snapshot` then `rate-table` (x87 A, stage 4).** Release build
(`dotnet build tests/RateTableTool -c Release`, 0 warnings, build of claude/x87-a4 on the
per-cycle plane that follows the executable's listing). `PROPSTRUCT_NO_CUDA=1`, default
parallelism, 16 logical CPUs. `seed-zero-snapshot`: 5 m 8 s, nine of the ten `Original`
hashes moved (P33 `Original`/`Original` did not), the ten `Binary64` hashes are
unchanged. `rate-table`: **320 runs in 16 m 41 s, no crash**, 134 of 320 failing at
least one cell (17 `Original`, 117 `Binary64`), as before. Diffed field by field in Python against the
pre-run table (`Formulation`/`Layout`/`Precision`/`SeedIndex` as the key): 320 of 320
runs matched, **zero differences in `FailingCells` and `FailingNames` on all 320**; in
`ResultSha256` zero of the 160 `Binary64` and 147 of the 160 `Original` differ (the
plane moved cells by a binary32 unit without crossing a threshold). The 160 `Binary64`
files of `rate-runs/` are byte-identical (SHA-256 of each against the commit before;
`git status` lists no `Binary64` file). `CriterionSha256` and `FixturesSha256` did not
move; `SnapshotSha256` did, with the snapshot. `RateCriterionTests` and
`SetCriterionTests`, 59 cases, pass, the tie included.
