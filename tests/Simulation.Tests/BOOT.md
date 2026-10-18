# BOOT.md — Simulation.Tests

## Purpose

The definition of what "`Simulation` is ready" means.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | option validation; accelerator by mode; each failure status turned into `SimulationFailedException`, except `ParticleNotRun`, which is an engine defect and throws `InvalidOperationException` | the `API.md` of `Simulation`, constructed formulations | ✅ 2026-09-19: `SimulatorOptionsTests`, `FailureStatusTests` (2026-10-02: `ParticleNotRunIsAnEngineDefectNotARunStatus`), `CudaRefusalTests` |
| L0 | the result carries every field of `CycleReport` | reflection over `CycleReport` | ✅ 2026-09-19: `ResultCompletenessTests` |
| L0 | every one of the fourteen `ModelParameters` at zero, negative and a very large value never leaves an undocumented exception or an unreasonably slow run | the property list of `ModelParameters` by reflection, a tightly-budgeted constructed run | ✅ 2026-09-20: `ParameterSweepTests` (42 cases; `SimulatorOptionsTests.RunDegenerateNnMaxThrowsSimulationFailedExceptionWithInvalidSetupImmediately` is the one case the sweep found and `Statistics` now rejects immediately, `src/Statistics/BOOT.md`, "## Constraints") |
| L1 | the degenerate batched configuration equals reference mode bit for bit | the same run through both paths, a constructed formulation with small `N` and `KXX = 2` | ✅ 2026-09-19: `DegenerateConfigurationTests` |
| L1 | progress and cancellation change no bit of a completed run | the same run with and without them | ✅ 2026-09-19: `ProgressAndCancellationTests` |
| L1 | line-map coverage | the list of executable lines of Fortran 278–375 and 407–424, generated from the source | ✅ 2026-09-24: `LineMapCoverageTests` |
| L0/L1 | `RunDiagnostics.AttemptsPerLaunch` is the effective budget (1 in reference mode, the option otherwise), not the option unconditionally; a smaller budget forces more launches on a formulation needing more than one attempt | `SmallFormulation`/HPEPA3 at budgets 1, 1024 and reference mode | ✅ 2026-09-28: `AttemptsPerLaunchDiagnosticsTests` |
| L2 | reference mode on HPEPA3 reproduces the reference's printed counters | `tests/Fixtures/references/HPEPA3/results.m.txt` through `tests/Harness` | ✅ 2026-09-19: `Hpepa3CounterTests` (figures in `src/Simulation/BOOT.md`, "## Counter check") |
| L2 | the statistical criterion on the five reference formulations, realigned again 2026-09-24 to root's rate-based pass condition (root BOOT.md, "the pass condition compares failure rates, not single runs") | link 1 no longer lives here at all — it is a *rate* over sixteen seeds, owned by `tests/Harness.Tests/RateCriterionTests` over the pregenerated `tests/Fixtures/rate-table.json`; this node keeps only an exact seed-0 snapshot (a change detector, not a reproduction claim) and link 3: batched `Binary64` against sequential `Binary64`, statistically, on the port's own replicas, CPU accelerator and CUDA (`StatisticalCriterion.TwoSampleBiasOfSets`, `tests/Harness/API.md`) | ✅ 2026-10-01: `StatisticalCriterionTests`, 40 cases (30 `Category=Long`: 20 seed-0 snapshot, 10 link 3), all pass — see "## Statistical criterion measurement (2026-09-24)" below. The 2026-09-23 ratchet-based measurement (link 1 as a per-run comparison against the original's own replicas, retired the same day audit D5 named it) is superseded, `HISTORY.md#statistical-criterion-measurement-2026-09-23-link-1`. |
| L2 | the `Original` model stays bit for bit what it is, while a second model kind is added | hashes of `results.m` and of the whole result as JSON, 35 CPU runs (`Snapshots/OriginalModelGuard.approved.txt`) | ✅ 2026-10-18: `OriginalModelGuardTests`, 36 tests: 35 cases, 12 in the fast set and 23 `Category=Long`, and the file's own; "## The Original-model guard" |
| L2 | the `Corrected` model's results do not move unnoticed, and it means every flag of `CorrectionSet` | the same hashes, 10 CPU runs under `Corrected` (`Snapshots/CorrectedModelSnapshot.approved.txt`); the mapping read from `EffectiveCorrections` | ✅ 2026-10-18: `CorrectedModelGuardTests`, 11 tests, 2 of them `Long`; `ModelKindTests`; "## The Corrected-model snapshot" |

⚠ 2026-10-18: was "34 cases (33 `Category=Long`)" in the guard's row, the whole guard run by no CI → HISTORY.md#original-guard-criterion-33-cases

⚠ 2026-09-19: the ✅ marks of this table were first dated 2026-09-18 by a session that
ended at a usage limit, and two of them did not hold when this session started. The
completeness row was red: `Ggg` had been removed from `RunHeader` for a mutation and
never restored. The counter row passed vacuously for conditions 6–9 (wrong denominator,
absolute floor of 1.0; `src/Simulation/BOOT.md`, "## Counter check"). Every row was
re-proven on 2026-09-19 by the mutations below, each seen red and reverted.

⚠ 2026-10-02: the statistical-criterion row was dated 2026-09-24, the day its text was
realigned. The evidence it names was obtained later: the seed-0 snapshot was
re-approved and link 3 re-run at the attempt budget 8192 on 2026-10-01 (874d81c, 82f1de2,
3ea6471; `git log` of `StatisticalCriterionTests.cs` and the snapshot ends at 874d81c),
the date the root's batched criterion carries for the same evidence
(`ACCEPTANCE.md`, "Batched mode"; the rule: a date is the day the evidence now in the
tree was obtained after the last change to what it reads, AGENTS.md §6). Found by the
2026-10-02 arbitration's D4 reading. Not re-run for this correction (`Category=Long`):
3630af3 (2026-10-02) changed the failure path of `Simulator` only, no byte of a
completed run, and the figure "40 cases" is as recorded 2026-09-24.

`Snapshots/SeedZeroResultsM.approved.txt` is weakly tied to the platform: a last-ULP
difference of `Math.Log` or `Math.Pow` moves a hash only where it crosses a print
boundary of `results.m` or flips a branch. It carries no `# platform:` header, because
`tests/Fixtures/rate-table.json` digests its bytes, and a platform move of it follows
the rule of `tests/Particle.Tests/BOOT.md`, "## Taboos": its own commit, naming both
platforms (2026-10-02).

## Statistical criterion measurement (2026-09-24)

⚠ 2026-09-19: this section first read `PrecisionKind.Binary64` against the *original's*
replicas, 6 of 25 cases passing → HISTORY.md#statistical-criterion-measurement-2026-09-19

⚠ 2026-09-24: link 1 (the seed-0 ratchet against the original's own replicas, its
measurement table and its known-open list) is superseded by root BOOT.md's own
rate-based realignment (audit D5: a single seed-0 run is not evidence either way) →
HISTORY.md#statistical-criterion-measurement-2026-09-23-link-1

**Link 1's replacement: an exact seed-0 snapshot**
(`SeedZeroResultsMMatchesApprovedSnapshot`, twenty cases — five formulations, both
layouts, both precision kinds — all `Category=Long`). Reference mode, seed 0, its
`results.m` (the time line stripped, `ReferenceModeRunner.RunSeed`) hashed with
SHA-256 and compared exactly against
`tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt` — a change detector
like the surface snapshot (AGENTS.md §13), not a reproduction claim: it asserts only
that the port's own seed-0 output has not moved since the file was approved, nothing
about agreement with the original's replicas (that claim is now a *rate*, over sixteen
seeds, `tests/Harness.Tests/RateCriterionTests`, `tests/Harness/BOOT.md`, "## Null rate
of the original"). Generated and verified 2026-09-24: `RegenerateSeedZeroSnapshot`
wrote the twenty hashes (5 m 13 s), and the theory then reproduced every one of them
(5 m 46 s) — the round trip is its own first proof of determinism. Non-degeneracy
(root BOOT.md's own two-proof taboo): a one-character edit of `inpt`'s
`Original`/`Original` hash turned that one case red with both hashes printed, reverted
after, diffed byte-identical against the pre-mutation file.

⚠ 2026-09-25: `ReferenceModeRunner` and the generator this paragraph names
(`RegenerateSeedZeroSnapshot`) moved to `tests/RateTableTool` (`API.md`, "## Command
line"; `## Dependencies` below), because a `[Fact(Skip = ...)]` generator that
never runs under `dotnet test` is the same permanently-skipped check AGENTS.md §13
forbids. This node keeps only `SeedZeroResultsMMatchesApprovedSnapshot`, the one
genuine assertion, now built on the neighbour's `ReferenceModeRunner` (`InternalsVisibleTo`,
`tests/RateTableTool`'s own csproj); regenerate the snapshot with
`dotnet run --project tests/RateTableTool -- seed-zero-snapshot`. The hashes above are
unaffected in substance by the move; they moved once more the same day for the
`PrecisionKind.Binary64` rename (CA1720) — the snapshot's keys spell the precision kind
by `PrecisionKind.ToString()`, so `... Double ...` became `... Binary64 ...` in the
regenerated file, text only, no run behaviour changed. Regenerated and re-verified
2026-09-25: 20/20 hashes bit-identical to the pre-move file bar that label text, and
`SeedZeroResultsMMatchesApprovedSnapshot` reproduces every one of them (17 m 56 s,
Debug), `tests/RateTableTool/BOOT.md`, "## Regeneration runs".

**Link 3** (`BatchedModeCpuIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds`,
`BatchedModeCudaIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds`):
batched mode, CPU accelerator and CUDA, against sequential reference mode, both
`PrecisionKind.Binary64`, `StreamLayout.Independent` (the only layout batched mode
accepts), `R = 8` seeds each, `s = k · 2¹⁷` for `k = 0..7` — a stride chosen so every
stream's phase moves by an odd multiple of `1/8` per step (`src/Random/BOOT.md`, "Seed
and replica jumps are rigid shifts"), landing on all eight eighths of the circle
exactly once rather than clustering near one point. Compared with
`StatisticalCriterion.TwoSampleBiasOfSets` (`tests/Harness/API.md`, made public for
this use), family-wise `α = 10⁻³`, asserting no cell differs. The eight reference-mode
runs (single-threaded each) run in parallel; the eight batched runs (each already
using every host thread through the CPU accelerator, or the one CUDA device) run
sequentially so as not to oversubscribe the machine. The CPU and CUDA rows share one
set of reference-mode runs per formulation (`ReferenceSetsIndependentDouble`'s own
cache, keyed by formulation name, `LazyThreadSafetyMode.ExecutionAndPublication`):
whichever row xunit runs first for a formulation pays for the eight reference runs,
the other reuses them and reports zero added reference time — so the total cost is not
double-counted.

Measured 2026-09-23 on `claude/wave7` at `a621e09`, CPU accelerator and CUDA: **0 of
the compared cells differ on any of the five formulations**, 1509 cells (P33) to 6635
(HMX); no ratchet was needed on either accelerator. The tables, the wall times and the
note on the category-indexed families moved to history (AGENTS.md §15).

⚠ 2026-10-18: was the per-formulation tables of cells, differences and wall times of the CPU and CUDA
rows, now their result and one figure → HISTORY.md#link-3-measurement-tables-2026-09-23

⚠ 2026-10-18: was the 2026-09-23 note that `TwoSampleBiasOfSets`' constant-cell false positive does not touch link 3's equal-size sets, now moved → HISTORY.md#twosamplebiasofsets-note-2026-09-23

Non-degeneracy (positive control, root BOOT.md's own two-proof taboo — a mutation
inside batched mode's `Original`-layout derivation is refused before it runs, so no
honest red exists on that route): reference mode `PrecisionKind.Binary64` against
reference mode `PrecisionKind.Original` at the same three seeds (`k = 0..2`) on
HPEPA3, where root `ACCEPTANCE.md`'s link 2 is known to differ. Run once from a scratch
test, deleted after (this node's own convention, "## Mutations" below): 5 of 2056
cells differed (`coef[106]`, `coef[139]`, `dokkarm43[1]`, `fmdok[2]`, `fmdok[3]`), the
same accumulator-print fields root `ACCEPTANCE.md`'s link 2 names, confirming the
zero-differences assertion is not vacuously green.

Non-degeneracy of the CUDA row, added 2026-09-23, later the same day, two proofs
(root BOOT.md's own two-proof taboo; this node's own BOOT.md, "## Mutations" below has both in full):

- **positive control**, reproducing the paragraph above at the CUDA row's own eight
  seeds rather than the CPU row's three: reference `PrecisionKind.Binary64` against
  reference `PrecisionKind.Original` on HPEPA3, `R = 8`, run once from a scratch test,
  deleted after — 42 of 2082 cells differed, the same accumulator- and setup-plane
  fields root BOOT.md's own links 1 and 2 name (`pdoksmall`, `fmdok`, `dokkarm43[1]`),
  now visible in bulk at `R = 8` where `R = 3` above saw five;
- **mutation of the row itself**: `BatchedModeCudaIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds`'s
  own reference set, for HPEPA3 only, temporarily recomputed with `PrecisionKind.Original`
  instead of the shared `Binary64` cache — the task's own "compare the CUDA batched set
  against reference Original" — leaving the CUDA batched set (`Binary64`) unchanged: 43
  of 2071 cells differed and the test failed as designed; reverted immediately after,
  the file diffed against the pre-mutation working tree to confirm no residue.

Total wall time for all 30 cases (`-c Release`, measured per-formulation, this
session, sum of `dotnet test`'s own per-test elapsed): HPEPA3 2 m 9 s + 53 s CUDA,
inpt 8.3 s + 20 s CUDA, P33 8.5 s + 15 s CUDA, PSAN02n 37.1 s + 17 s CUDA, HMX
3 m 53 s + 5 m 32 s CUDA — the whole class (link 1, link 3 CPU and CUDA, both refusal
rows) in one `dotnet test` run measured at 13 m 16 s total, dominated by HMX's
reference-mode attempt count (`## Budget measurement` below) on the CPU side and by
the CUDA-collapse tier on HMX's own CUDA side.

The 2026-09-19 measurement's own analysis (the CUDA-equals-CPU-in-batched-failure
observation, the cross-reference to "Known bias of the original's seeds", the
`epsdokfr`/`pdoksmall` notes) was read against `PrecisionKind.Binary64` compared to the
*original's* replicas — a comparison this rewrite retired, not merely re-ran — so none
of it is re-verified or carried forward; it is preserved in full,
`HISTORY.md#statistical-criterion-measurement-2026-09-19`.

⚠ 2026-10-18: was a paragraph proposing to root which of its criteria link 3's CPU and CUDA rows evidence (root's own to tick, AGENTS.md §11), now moved → HISTORY.md#link-3-proposal-to-root

## Invariants

- Expected values come from `tests/Fixtures`, never typed into a test.
- The counter comparison states its tolerance and its reason next to it; a counter
  outside it is diagnosed before anything moves (the QKS1 defect of `Execution` was
  found exactly this way).
- Runs over whole reference formulations are marked `Category=Long` if they take more
  than a few seconds, measured before deciding.
- A test that changes the process environment (the CUDA kill switch) runs in a
  collection with parallelization disabled, since every engine created concurrently
  reads the same variable.

## Dependencies

- [Simulation](../../src/Simulation/API.md) — what is being checked.
- [Input](../../src/Input/API.md) — reading the formulations.
- [Statistics](../../src/Statistics/API.md) — `CycleReport`, for the reflection check;
  `Setup.Prepare`, whose `ModelSetup` ratios `ModelKindTests` compares with the result's.
- [Particle](../../src/Particle/API.md) — `CorrectionSet`, the seam's flags.
- [Execution](../../src/Execution/API.md) — `AcceleratorKind`, `BatchStatus` for the
  status mapping check.
- [Output](../../src/Output/API.md) — writes the port's `results.m` for the criterion
  row to parse (this row's own `results.m` is a temp file, discarded after the compare).
- [Harness](../Harness/API.md) — `results.m` parser, repository paths, the criterion.
- [Fixtures](../Fixtures/API.md) — the reference formulations and outputs.
- [RateTableTool](../RateTableTool/API.md) — `ReferenceModeRunner.RunSeed`, shared with
  that node's own two generators rather than duplicated (root BOOT.md Taboos).

Outside the tree: xunit.

## Constraints

- Part of the default test command; CUDA rows follow `Execution.Tests`' rule (refusal
  asserted, never skipped): `FailureStatusTests.RunBatchedOnCudaRunsOrFailsWithAcceleratorUnavailable`
  runs on CUDA where it is bound, and `CudaRefusalTests` forces the refusal with
  `PROPSTRUCT_NO_CUDA=1` so that branch runs on every machine.

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Mutations

Recorded per AGENTS.md §13, 2026-09-19. Each is a change of product code (or, for the
counter check, of the test's own denominators), applied alone, run against the named
tests, seen red, then reverted; the fast rows by a script in the session scratch.

| Mutation (in `src/Simulation/Simulator.cs` unless named) | Red |
|---|---|
| each of the eight `Validate` guards disabled in turn (`BatchSize`, `RecordBudgetBytes`, `AttemptsPerLaunch`, `MaxAttemptsPerParticle`, `NeighbourBudget`, `PocketRedrawBudget`, `Cuda` with `Reference`, `ContinuedStreams`) | the matching `SimulatorOptionsTests.Create_Rejects*` (two for `ContinuedStreams`) |
| `Auto` not resolved to `Cpu` in reference mode | `CreateResolvesAutoToCpuInReferenceMode`, `Create_AttemptCapExceeded_*` |
| `AttemptCapExceeded` mapped to `IndexOutOfRange` | `FailureStatusTests.EveryBatchFailureStatus_*`, `Create_AttemptCapExceeded_*` |
| `NeighbourBudgetExceeded` and `BridgeDrawBudgetExceeded` swapped | the mapping test, `Run_NeighbourBudgetExceeded_*`, `Run_BridgeDrawBudgetExceeded_*` |
| `AcceleratorUnavailable` mapped to `InvalidSetup` | `CudaRefusalTests` |
| a failed setup reported as `CategoryCountExceedsCapacity` | `Run_InvalidSetup_*` |
| the degenerate path runs on a copy of its streams instead of continuing them | `DegenerateConfigurationTests.BatchedDegenerateConfigurationEqualsReferenceModeBitForBit` |
| reference mode reports the cycle size as its batch size | the same test |
| a run with progress uses half the batch size | `ProgressAndCancellationTests.RunWithProgressAndCancellationTokenGivesTheSameResultAsWithNeither` |
| reference progress every 500 particles | `RunReferenceModeReportsProgressAfterEvery1000ParticlesAndAtTheEnd` |
| `ThrowIfCancellationRequested` replaced by a read everywhere | `RunCancelledMidRunThrowsOperationCanceledException` |
| `Hpepa3CounterTests`: conditions 6–9 per `2 · FI`; separately conditions 1–5 per `2 · (FI + N)` | condition 7 (2,422 against 1,231, bound 241.8); condition 2 (22 against 0, bound 18.8) |
| `StatisticalCriterionTests`: `ReferenceMode_IndependentLayout_SatisfiesCriterion_AgainstIndependentReplicas` compared against `ReplicaKind.Lagged` instead of `ReplicaKind.Independent` | P33 (currently one of the six passing cases): 405 of 1476 quantities fail |
| 2026-09-20, `src/Statistics/Setup.cs`'s `InvalidNnWindow` precondition removed | `SimulatorOptionsTests.RunDegenerateNnMaxThrowsSimulationFailedExceptionWithInvalidSetupImmediately` (`Expected: InvalidSetup, Actual: AttemptCapExceeded`, after the full 4 m 1 s the removed precondition used to take — the reported defect itself, reproduced by un-fixing it); `ParameterSweepTests` stayed green through the same mutation, since it asserts only "no undocumented exception", not the specific status per parameter — recorded here as the sweep's own limit of power, not hidden |
| 2026-09-24, `LineMapCoverageTests`: `src/Simulation/BOOT.md`'s `## Line map` row `297–342` removed | red, missing 39 executable lines; restored, diffed byte-identical against the pre-mutation file |
| 2026-09-24, `SeedZeroResultsMMatchesApprovedSnapshot`: one hex character of `inpt Original Original`'s approved hash in `Snapshots/SeedZeroResultsM.approved.txt` edited (test data, not product code) | that one case failed, both hashes printed in the message; reverted, diffed byte-identical against the pre-mutation file |
| 2026-09-28, `Simulator.Run`'s `reportedAttemptsPerLaunch` changed to report `_options.AttemptsPerLaunch` unconditionally (the option, not the effective budget) | `AttemptsPerLaunchDiagnosticsTests`: the reference-mode echo (expected 1, options set to 1024) `Expected: 1, Actual: 1024`; reverted, diffed byte-identical |
| 2026-09-28, the same field changed to report `SimulationDefaults.AttemptsPerLaunch` (the default, 32) unconditionally | the budget-1 echo `Expected: 1, Actual: 32`; reverted, diffed byte-identical |
| 2026-09-28, `RunDiagnostics.AttemptsPerLaunch` given `[property: JsonIgnore]` (`src/Simulation/SimulationResult.cs`) | `tests/Output.Tests/JsonRoundTripTests.WriteThenReadGivesAnEqualResultFieldByFieldWithBitwiseDoubleEquality`: `Expected: 7, Actual: 0` (the JSON round trip silently drops the field); reverted, diffed byte-identical |
| 2026-10-02, the `ParticleNotRun` arm of `ToRunStatus` throws `ArgumentOutOfRangeException` instead of `InvalidOperationException` | `FailureStatusTests.ParticleNotRunIsAnEngineDefectNotARunStatus`: `Assert.Throws() Failure: Exception type was not an exact match`, `Expected: typeof(System.InvalidOperationException)`, `Actual: typeof(System.ArgumentOutOfRangeException)`; reverted, diffed byte-identical |
| 2026-10-02, the same arm mapped to `RunStatus.IndexOutOfRange` | the same fact: `Assert.Throws() Failure: No exception was thrown`; reverted, diffed byte-identical |
| 2026-10-02, the exclusion `and not BatchStatus.ParticleNotRun` removed from `EveryBatchFailureStatusMapsToTheRunStatusOfTheSameName` | that fact: `System.InvalidOperationException : Execution reported ParticleNotRun: ...`; restored, diffed byte-identical |
| 2026-10-02, the arm deleted | the build: `error IDE0072: Populate switch` at `Simulator.cs(299,73)`; restored, diffed byte-identical |
| 2026-10-02, right proof, end to end, once: `src/Execution/Engine.cs`'s host-thread `Parallel.For(0, active.Length, ...)` of `RunAttempts` changed to `active.Length - 1` (Execution's recorded mutation, `tests/Execution.Tests/BOOT.md`, "## Launch-advance check (2026-10-02)"), reverted after | unmutated, `dotnet src/Cli/bin/Debug/net10.0/propstruct.dll run tests/Fixtures/cases/input/baseline.dat --accelerator cpu --output <scratch>.m` exits 0. Mutated, the same command exits 4 with the stderr line `System.InvalidOperationException: Execution reported ParticleNotRun: a launch left an active particle's attempt count unchanged, a defect of the engine, not a status of the run.`; and a scratch `[Fact]` (deleted) running `Simulator.Run` on `SmallFormulation.Build(24, 1)`, `Mode = Batched`, `Accelerator = Cpu`, passed `Assert.Throws<InvalidOperationException>` with the message containing `ParticleNotRun`; `Engine.cs` diffed clean after |

⚠ 2026-09-23: the row above named `ReferenceMode_IndependentLayout_SatisfiesCriterion_AgainstIndependentReplicas`,
which the same day's realignment renamed to
`ReferenceMode_IndependentLayout_OriginalPrecision_MatchesIndependentReplicas_ExceptKnownCells` and changed from
`PrecisionKind.Binary64` to `PrecisionKind.Original`; the row is kept as the historical record of the proof it was
(swapping `ReplicaKind` breaks the comparison) rather than rewritten, since a mutation record is provenance, not
current truth (AGENTS.md §8).

Two more mutations, 2026-09-23, proving the realigned criterion's own two new mechanisms non-degenerate
(root BOOT.md's own two-proof taboo; "## Statistical criterion measurement (2026-09-23)" above has the full
figures):

| Mutation | Red |
|---|---|
| `KnownOriginalLayoutFailures["HMX"]` emptied to `[]` (test's own data, not product code) | `ReferenceMode_OriginalLayout_OriginalPrecision_MatchesLaggedReplicas_ExceptKnownCells(name: "HMX")`: `fqdokkarm(31,:)[7]` reported `UNEXPECTED`, 1 new/unexpected failure; reverted, diffed byte-identical against the pre-mutation file |
| positive control (not a mutation of this node's own code): reference mode `PrecisionKind.Binary64` vs `PrecisionKind.Original` at the same three seeds on HPEPA3, run from a scratch `[Fact]` deleted after | 5 of 2056 cells differ (`coef[106]`, `coef[139]`, `dokkarm43[1]`, `fmdok[2]`, `fmdok[3]`) — proves `BatchedModeCpuIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds`'s own zero-differences assertion can see a real difference; no honest red exists on the batched route itself, since `Original`-layout batched mode is refused before it runs |

Two more, 2026-09-23, later the same day, proving the CUDA link-3 row non-degenerate (root BOOT.md's own
two-proof taboo; "## Statistical criterion measurement (2026-09-23)" above has the full figures and the CUDA
measurement table):

| Mutation | Red |
|---|---|
| positive control (not a mutation of this node's own code): reference mode `PrecisionKind.Binary64` vs `PrecisionKind.Original` at the CUDA row's own eight seeds (`k = 0..7`) on HPEPA3, run from a scratch `[Fact]` deleted after | 42 of 2082 cells differ (`pdoksmall`, `fmdok`, `dokkarm43[1]`, the same accumulator- and setup-plane fields root BOOT.md's own links 1 and 2 name) — the same positive control as the CPU row's own, above, but at `R = 8` where `R = 3` saw five cells; confirms `TwoSampleBiasOfSets` sees a real difference at this row's own sample size too |
| `BatchedModeCudaIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds(name: "HPEPA3")`'s own reference set temporarily recomputed with `PrecisionKind.Original` instead of the shared `Binary64` cache, leaving the CUDA batched set (`Binary64`) unchanged (the task's own "compare the CUDA batched set against reference Original") | 43 of 2071 cells differ and the test fails as designed; reverted immediately after, the working file diffed against the pre-mutation state to confirm no residue |

The completeness row (`ResultCompletenessTests.SimulationResultCarriesEveryFieldOfCycleReport`)
was found red at the start of this session with `Missing from SimulationResult: Ggg` (the
unreverted mutation above) and went green when `Ggg` was restored to `RunHeader`: a real
red-then-green, not a staged one.

One mutation first stayed green: with the `RecordBudgetBytes` guard disabled,
`CreateRejectsNonPositiveRecordBudgetBytes` still passed, because `Execution`'s
`Engine.Create` rejects a zero budget with an `ArgumentException` of its own, after the
point API.md promises ("before any work"). The validation tests now also assert that the
exception comes from `Simulator`'s own guard (`ParamName == "options"`), and the mutation
goes red.

`AttemptsPerLaunchDiagnosticsTests`' own positive control (root BOOT.md's two-proof
taboo; not a mutation, per `PrecisionKindTests.cs:17-23`'s own "weak vs strong"
distinction): budget 1 gives strictly more `TotalLaunches` than budget 1024 on the same
formulation, whose `TotalAttempts` already exceeds its particle count (guarded so the
comparison cannot pass vacuously) — the echo alone would pass even if the budget never
reached `Execution.Engine`, since the diagnostics value could be recorded correctly
while the run underneath ignored it.

Limits of power, recorded rather than hidden: `Hpepa3CounterTests` would pass a doubled
denominator on condition 8 alone (41 events, 82 against 39 within 44.4); `IndexOutOfRange` is checked by the mapping test only, and `CategoryCountExceedsCapacity`
(raised by `Simulator` itself from `Statistics`' status) by no test, since no constructed
run reaches either; `ResultCompletenessTests` checks names, not
which record carries a field or its value.

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.

### Model kind (2026-10-18)

Each applied alone to `src/Simulation`, run against `ModelKindTests` (and the named
guard), seen red, reverted:

| Mutation | Red |
|---|---|
| the `Corrected` with `Original` precision refusal removed | `CorrectedWithOriginalPrecisionIsRefusedInReferenceModeNamingBothHalves` |
| `Corrected` mapped to `CorrectionSet.None` | `CorrectedPrintsTheConsistentDoksdOfP33`, `ASingleFlagSeamRunDiffersFromOriginalWhereThatFlagIsVisible` |
| `Original` mapped to `CorrectionSet.All` | `OriginalPrintsTheArchivedDoksdOfP33`, `CorrectedWithNoFlagSetEqualsTheOriginalRunNormalised`, the single-flag control, the guard's `ResultAndResultsMMatchApprovedHashes` |
| the seam refused for `Original` no more; the seam's out-of-`All` test disabled | `TheSeamIsRefusedUnderOriginal`; both cases of `TheSeamIsRefusedForABitOutsideAll` |
| `Ak1` copied under `Original` too | `AnOriginalRunsResultsAndJsonCarryNoModelLabel` |
| `RunDiagnostics.Model` fixed at `Original` | `ACorrectedRunsDiagnosticsAndStoredRatiosEqualTheSetupsOwn`, `CorrectedWithBinary64RunsToCompletionInBothModesAndBothLayouts`, the degenerate case under `Corrected` |
| `SimulationOptions.Model` defaulting to `Corrected` | `TheSurfaceHasTheExplicitValuesTheDefaultAndBodyInitProperties` |
| the `Enum.IsDefined` check of the validator removed (T13) | the three cases of `AnUndefinedModelValueIsRefusedNamingTheValueBeforeTheSeamCheck` |
| each `CorrectionSet` flag applied under `Original`, one at a time (T13) | the cases of `ACCEPTANCE.md`'s per-flag table |

### U3 (2026-10-18)

Each applied alone, run against the named facts, seen red, reverted (`git checkout` of
the file, diff clean):

| Mutation | Red |
|---|---|
| the mapping `Corrected` → `All & ~GdokleftOnce` (scratch edit of `EffectiveCorrections`) | `ModelKindTests.TheEffectiveCorrectionsAreEveryFlagUnderCorrectedWithNoSeamAndNoneUnderOriginal` alone; the other 23 `ModelKindTests` cases stayed green, which is the gap it closes |
| each of the eleven flags dropped from the `Corrected` mapping | the cases of `ACCEPTANCE.md`'s Corrected per-flag table |
| each flag applied under `Original` over the 12 fast guard cases | the cases of `ACCEPTANCE.md`'s fast-subset criterion |
| one character of `extra/PSAN02-sfr-dmin20-cycles3/Original`'s hash in `OriginalModelGuard.approved.txt`, and of `corrected/extra/P33-defaults`' in `CorrectedModelSnapshot.approved.txt`, edited | those two cases, nothing else; 18 of 20 fast guard cases green |
| `MeasurementCase.SeedStrideBits` 16 → 17 | `EveryCasesSeedsAndTheAuditsEightHaveDistinctPhasesModulo2To20`, `TheOldStrideWithSixteenSeedsRepeatsEightPhasesAndTheCheckNamesThem`, `TheReportRefusesArmsWhoseSeedsRepeatAPhaseAndRunsOverDistinctOnes` |
| the `RequireDistinctPhases` calls of `CorrectionEffectsReport` commented out | `TheReportRefusesArmsWhoseSeedsRepeatAPhaseAndRunsOverDistinctOnes` |
| `CorrectionArm.AllBut` mapped to `All & flag` | `TheCategoryFlagsAreTheThreeAndEachAllButArmSetsEveryOtherFlag` |
| the `AllBut` arms not added to `Measured` | that fact and `TheMeasuredArmsAreOriginalAllEachSingleFlagAndAllButEachCategoryFlagOnceUnderDistinctNames` |
| `Dagg43_cor(2)` removed from `HeadlineQuantity.All` | `TheHeadlineQuantitiesAreReadFromTheParsedCellsAndDPRowIsTheCategoryCount` |

## Budget ladder (2026-10-01)

Task C3 of the budget decision (`src/Simulation/BOOT.md`, "## Budget selection rule
(2026-09-28)", committed in 4e359b8 before any run at a budget other than 32).
`BudgetLadderTests.LadderRow`, Release, run once on 2026-10-01: 5 formulations × budgets
{32, 128, 512, 2048, 8192} × {Cpu, Cuda}, link 3's own eight seeds, `Binary64`,
`Independent`, `TwoSampleBiasOfSets` at the tree's `α`. **All 50 rows GREEN, 0 cells
differ in any row**; 55 of 55 tests passed in 27.5 min. Cells compared per formulation:
HPEPA3 2071, inpt 2634, P33 1509, PSAN02n 2151, HMX 6635. Launches at seed 0 equal E-B's
(HPEPA3 7/2/2/2/2, HMX 187/47/12/3/2), asserted. **The rule selects 8192.**

Paired shift, 8192 against 32, host threads, eight seeds (a size, not a verdict): cells
that differ at all — HMX 107, inpt 52, PSAN02n 39, HPEPA3 18, P33 0; the largest mean
paired shift is 0.28 of the 32-row's seed sd (PSAN02n `Dqmkm2[0]`; HMX 0.21, HPEPA3 0.14,
inpt 0.11). Link 3 itself resolves only shifts of about 4.6–5 seed sd (budget decision,
F3), so this record, not the GREEN rows, bounds the staleness of the coarser QKS1
refresh. The null-budget red of the echo assertion is the test author's record, not re-run.
The ladder theory was deleted when the default moved (C5); it stays reproducible at
3ea6471.

## The Original-model guard

Added 2026-10-18 before any `Corrected` model code, so that every merge proves `Original`
unchanged: the seed-0 snapshot's mechanism in a file of its own, since
`SeedZeroResultsM.approved.txt` is digested into `rate-table.json` (`SnapshotSha256`).
`OriginalModelGuardTests` reads `Snapshots/OriginalModelGuard.approved.txt`, written by
`dotnet run --project tests/RateTableTool -- original-model-guard`: per case the id, the
SHA-256 of `results.m` (time line cut) and of the `ResultsJson` text of the whole
`SimulationResult` (`Elapsed`, `Accelerator` constants): every double round-tripped, where
`results.m` prints four to eight digits.

Cases (`OriginalModelGuard.Cases`, `tests/RateTableTool`), seed 0, CPU: the twenty seed-0
cases (`seed0/...`); the five references batched, `Independent`, `Binary64` (`batched/...`);
nine reference-mode `Independent`/`Binary64` runs of `P312` (defaults, `Dmin` 5 µm, 3 cycles,
`HomogenizedOxidizerFraction` 0.1), `PSAN02` (SFR read, `Dmin` 20 µm; then 3 cycles; then
`Di` 10 µm too), `P33`, `P2` (`extra/...`), the paths the references leave dark (`Gdokleft`
is 0 there), at `ParticlesPerCycle = 50` (`P312`: 0.28 s per particle in Release); some print
`NaN`, which `ResultsMFile` does not parse, so the runner does not; and one
`Original`-precision run, `extra/PSAN02-sfr-dmin20-cycles3/Original` (U3: reference
mode, `Independent`, three cycles, `Gdokleft` 0.49): the setup plane under the other
precision kind on a path the references leave dark. 35 cases, 7 m 26 s to generate in
Release (34 cases).

**Fast set and `Long`** (U3, 2026-10-18, `GuardCase.IsLong`): the guard was wholly
`Category=Long`, so CI never ran it. The `batched/*` and `extra/*` cases run in the fast
set, 12 cases, 32 s in Debug together with the Corrected snapshot's; `seed0/*` and the
three P312 extras that took 44 to 94 s in Debug (`-defaults`, `-cycles3`,
`-homogenized0.1`) stay `Long`, 23 cases. The 12 still see every flag of `CorrectionSet`
(`ACCEPTANCE.md`, the fast-subset criterion); the two flags that rest on one case each
have it in the fast set: `extra/P312-dmin5` (`PdoksmallFirstCell`, 0.07 s) and
`extra/PSAN02-sfr-dmin20-di10` (`GdokleftOnce`).

`PSAN02-sfr-dmin20-di10` (2026-10-18, T13): `PSAN02-sfr-dmin20` keeps the default `Di`, and
`1.9999999999999998e-05 / 1e-05` truncates to 1, so its `b` is 10 µm, `φ(b)` is 0 and
`GdokleftOnce` cannot move a bit there (the per-flag table in `ACCEPTANCE.md` shows no case
saw it); with `Di` 10 µm `b` is 20 µm. Its line was generated with the product code of the
original port: `src/` checked out at `ac1fc20` (the tree removed and checked out again from
that commit), the generator built and run in Release, `src/` restored from `HEAD`. The
regenerated file differs from the committed one by that one added line, and the 34 cases
pass at the head, `Original` unchanged.

`extra/PSAN02-sfr-dmin20-cycles3/Original` (2026-10-18, U3) was generated the same way:
the tree removed and `src/` checked out at `ac1fc20`, `tests/RateTableTool` as at the
base of U3 but for the case (`Program.cs` without the `corrected-model-snapshot`
subcommand and `CorrectedModelGuard.cs` set aside, since a `Corrected` model does not
exist at `ac1fc20`), `original-model-guard` run in Release (35 cases, 6 m 45 s), `src/`
and the tool restored from `HEAD`. The file differs from the committed one by that one
line; the 34 older lines came out byte-identical, the old model's product code
reproducing them. The case is green at the head.

## The Corrected-model snapshot

U3, 2026-10-18: the `Corrected` model has no external reference, so nothing but a change
detector can say that its results did not move. `CorrectedModelGuardTests` is the
Original guard's mechanism on ten cases (`CorrectedModelGuard.Cases`,
`tests/RateTableTool`): the eight reduced extras of the guard that show the fixes the
references leave dark (`extra/P312-defaults`, `-dmin5`, `-cycles3`,
`extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10`, `extra/P33-defaults`,
`extra/P2-defaults`) and `batched/inpt`, `batched/PSAN02n`, which show
`CategoriesEmptyRowsMerged`; each is the Original guard's case run under
`ModelKind.Corrected` with no seam, ids prefixed `corrected/`. `P312-homogenized0.1` is
left out: the other P312 cases cover it. The two P312 cases that take 47 and 102 s in
Debug are `Category=Long`; the other eight run in the fast set (about 10 s).
`Snapshots/CorrectedModelSnapshot.approved.txt` is written by `dotnet run --project
tests/RateTableTool -- corrected-model-snapshot` at the head (Release, 10 cases, 52 s),
never typed; green in Debug. It asserts that the `Corrected` results are those approved,
not that they are right: a regeneration is the owner's decision, in the commit that
changes the model. Its per-flag proof is in `ACCEPTANCE.md`: dropping any of the eleven
flags from the mapping turns at least one fast case red.

## Correction-effects measurement

Added 2026-10-18 (task T12): what each correction of `CorrectionSet` does to each printed
quantity, before any release. Hand-run, not a readiness row: `CorrectionEffectsMeasurement`
carries `Category=Long` (out of the fast set and CI) and `Category=Measurement` (selects it
alone; the first hand-run category of this node, `API.md`, "## Category of the hand-run
measurement"). Its pages are generated into `Measurements/corrections-2026-10-18.md` and its
folder, never typed.

- **Arms**: `Original`; `All` (the `Corrected` model, no seam); each single flag through the
  seam `SimulationOptions.Corrections`; and, for the three category flags, which act on
  the same arrays and so interact, `AllBut<Flag>`: the seam `All & ~flag` (U3). All read
  from the enum
  (`CorrectionMeasurementToolingTests`). Batched, CPU, `Independent`, `Binary64`, default batch
  and budgets, seeds `k * 2^16` (U3). CUDA not used: other sessions share the GPU.
- **Seeds are phases**: the streams jump by `seed · 2^80` in a period of 2^100, so seeds
  equal modulo 2^20 are copies. `k * 2^17` repeated after eight seeds; `k * 2^16` gives
  sixteen distinct phases. A fast fact asserts it for every case, and the report refuses
  arms whose seeds repeat one. The audit's positive control keeps its own eight seeds `k
  * 2^17`.
  ⚠ 2026-10-18: was seeds `k * 2^17` → HISTORY.md#correction-effects-seeds-repeat-2026-10-18
- **Headline columns**: the quantities of the first tables include `Dkarm43_cor(1)`,
  `Dkarm43_cor(2)`, `Dagg43_cor(1)`, `Dagg43_cor(2)` and `Zkarm_cor`, read from the
  result as the others are (U3).
- **Cases**: the five references, 16 seeds; four extras for the fixes the references do not
  show, 8 seeds: `P312` defaults, `Dmin` 5 µm, three cycles, and `PSAN02` read with its SFR at
  `Dmin` 20 µm, `Di` 10 µm. `P312` runs at 100 particles per cycle (40 for three cycles,
  from 5000): it draws about 16000 attempts a particle. The seed counts follow a one-seed
  timing run.
- **Per quantity and arm**: shift in per cent of the `Original` mean with its standard error,
  Welch t, paired t over the shared seeds, and `TwoSampleBiasOfSets`' differing cells at
  `α = 10⁻³`; the arm tables are read beside the comparison's floor.
- **Controls**: the seam with `None` equals `Original` (`ModelKindTests`). Negative: `Original`
  even against odd seeds. Positive: `WindowLowerEdge` in reference mode over `k = 0..7`,
  `DoksdConsistent` on P33, `ExactConstants` on `Setup.Lambda`.
- **Floor** (measured 2026-10-18, the re-run at 8b29342): the negative control is clean,
  0 differing of 1234 to 7139 compared cells in all nine cases, `Original` even seeds
  against odd (8 against 8 on the references, 4 against 4 on the extras).
  `TwoSampleBiasOfSets` has no count floor or print resolution (`tests/Harness/API.md`),
  so cells one half prints and the other does not, or printed more coarsely than their
  spread, could differ with infinite or huge t; the fact asserts the floor under one per
  cent and each case's table prints it beside the arms.

  ⚠ 2026-10-18: was "not clean: 0 to 9 cells in 1200 to 7000" on the first run, whose halves held copied seeds, now 0 in every case → HISTORY.md#correction-effects-floor-first-run-2026-10-18
- **The store** `Measurements/cache/` (ignored by git, `MeasuredRunStore`) lets a run too long for
  one command be made in slices; it records each build's fingerprint and does not compare it,
  since a rebuild of unchanged source changes it. Delete it before a run that is to be evidence.


## Taboos

- Do not loosen a tolerance for the sake of green.
