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
the other reuses them and reports zero added reference time — so the total cost below
is not double-counted.

Measured on this branch merged onto `claude/wave7` at `a621e09`:

| Formulation | Cells compared | Differ | Reference (parallel) | CPU batched (sequential) | CPU total |
|---|---|---|---|---|---|
| HPEPA3 | 2071 | 0 | 31.3 s | 33.2 s | 1 m 4 s |
| inpt | 2634 | 0 | 2.7 s | 3.8 s | 6.5 s |
| P33 | 1509 | 0 | 3.1 s | 3.2 s | 6.3 s |
| PSAN02n | 2151 | 0 | 11.5 s | 10.0 s | 21.4 s |
| HMX | 6635 | 0 | 55.3 s | 81.6 s | 2 m 17 s |

CUDA row added 2026-09-23, later the same day, reusing the reference sets above (the
"Reference" column reads "reused" rather than a second measurement):

| Formulation | Cells compared | Differ | Reference | CUDA batched (sequential) |
|---|---|---|---|---|
| HPEPA3 | 2071 | 0 | reused, 24.3 s | 53.4 s |
| inpt | 2634 | 0 | reused, 1.9 s | 19.9 s |
| P33 | 1509 | 0 | reused, 2.1 s | 15.1 s |
| PSAN02n | 2151 | 0 | reused, 8.9 s | 16.8 s |
| HMX | 6635 | 0 | reused, 50.2 s | 5 m 32 s |

Cell counts match the CPU table exactly, as they must: both rows compare the same
eight reference sets against a batched set of the same shape, only the accelerator
differs. HMX's CUDA batched time is the one figure worth a reader's attention: 5 m 32 s
against the CPU row's 1 m 22 s (its own eight-seed sum from the table above), the same
"CUDA collapses on HMX" tier root `ACCEPTANCE.md`'s benchmark criterion already measured
(394 against 1954 accepted particles/s, cycle 1) — this is that collapse's own
consequence on a wall clock, not a new finding; the constraint's decision (no kernel
restructuring before acceptance) is root's, not this node's, to revisit.

No cell of any formulation needed a ratchet, on either accelerator: the
category-indexed families (`fqdokkarm(<row>,:)`, `Dkarmcat`, `dokkarm43`, `dokkarm10`,
`coef`) `Compare`'s own canonical-axis rule exists for (`tests/Harness/BOOT.md`,
"Canonical category axis") produced no spurious difference here either —
`TwoSampleBiasOfSets` has no such handling of its own (`tests/Harness/BOOT.md`,
"Two-sample bias"), but a physical row a given seed's run never produced is simply a
member `ValuesAt` (`tests/Harness`'s own) drops from that cell's sample, not a
mismatch it compares, and with sixteen runs per formulation split across two large
seed strides the coincidence needed to populate a row on one side and not the other,
thinly enough to trip a Welch test by chance at `α = 10⁻³` family-wise, did not occur
on any of the five formulations measured, on either accelerator.

⚠ 2026-09-23: `TwoSampleBiasOfSets` itself was found, the same evening these rows were
first measured, to false-positive on a bit-identical constant cell compared across two
sets of *different* sizes (`tests/Harness/HISTORY.md#twosamplebiasofsets-constant-cell-false-positive`;
fixed the same day). It does not touch the rows above: every link-3 comparison here
pairs two equal-size sets (`R = 8` against `R = 8`), and the defect's mechanism —
`Array.Average()` of `n` identical values rounding differently for different `n` —
cannot produce a nonzero gap between two means computed over the *same* `n` from the
*same* bit-identical value. Not re-measured, since nothing that could change it changed.

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

**What root BOOT.md should record, not written there by this task** (AGENTS.md §11:
both affected acceptance criteria are root's own): the "Reference mode in each layout
satisfies the criterion..." and "The `Original` accumulation kind reproduces..."
criteria cite figures measured under a mix of `Binary64`/original-replica and
`Original`/original-replica comparisons across several sessions; this session's own
link-1 numbers (above) reproduce root's own 2026-09-23 re-run exactly (same three
known-open cells, same compared/excluded counts on every row checked), so they
corroborate rather than change root's own standing text.

Link 3 is new evidence for a criterion root BOOT.md has not yet drafted words for:
"batched and CUDA against sequential `Binary64`, statistically, on the port's own
replicas" is named as a link but carried no measured row in root's own acceptance
criteria the way link 1 and link 2 do. The CPU half of that gap was filled earlier
2026-09-23 (above); the CUDA half is filled the same day, later: zero differing cells
on all five formulations on `AcceleratorKind.Cuda` too, the "## Link 3 measurement"
CUDA table above. Root BOOT.md's own acceptance criterion "Batched mode (whole-cycle
batches, CPU accelerator and CUDA) satisfies the same criterion..." currently reads,
of the `Independent` layout: "It stays unticked until CUDA is run the same way." That
condition is now met — this node offers both the CPU and CUDA rows as its evidence —
and root's own criterion is the owner's to tick, not this node's (AGENTS.md §11: the
criterion is root's own text). `tests/Execution.Tests`' own tier tables remain the
place a CUDA-vs-CPU claim for the kernel path itself belongs (this node's own
`## Dependencies`); this node's own claim is narrower — CUDA batched mode agrees with
the port's own sequential reference mode, statistically, the same claim the CPU row
makes, not a claim that CUDA agrees with the CPU accelerator cell for cell.

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
- [Statistics](../../src/Statistics/API.md) — `CycleReport`, for the reflection check.
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

- [x] Every row of the levels table is green, with a date and the names of the tests.
      (2026-09-24: the statistical-criterion row realigned again — the seed-0 ratchet
      retired for an exact snapshot, link 1's own reproduction claim moved to
      `tests/Harness.Tests/RateCriterionTests` as a rate over sixteen seeds,
      "## Statistical criterion measurement (2026-09-24)" above. "Green" here means
      "the seed-0 output has not moved since approval, and link 3 still agrees", not
      "the underlying science has no open question" — link 2's own still-missing HMX
      multi-seed measurement (root BOOT.md) is unaffected by this task and remains
      exactly as root records it.)
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13).
      (2026-09-19, "## Mutations" below; 2026-09-23 adds link 3's CPU positive control;
      later the same day, link 3's CUDA row adds its own positive control and its own
      mutation of the row itself, same section; 2026-09-24 adds the line-map coverage
      row's own mutation and the seed-0 snapshot's own one-character-edit proof, same
      section — the ratchet's own former proof moved to `HISTORY.md` with the rest of
      the retired link-1 text; 2026-09-28 adds `AttemptsPerLaunchDiagnosticsTests`' two
      echo mutations, its own positive control, and the JSON round trip's own
      `[JsonIgnore]` mutation in `tests/Output.Tests`, same section.)
- [x] 2026-10-03: the per-cycle plane under `Original` passes its controls, frozen
      before its code (`src/Statistics/BOOT.md`, "## Report"), on the seed-0 rate runs
      against the original's archived `results.m` and the frozen pre-change runs
      `Snapshots/CyclePlaneBaseline` (rate runs of 514a157, byte copies)
      (`CyclePlaneSeedZeroControlsTests`, 39 cases): P1 `da_coef` on all five, P2 HMX
      `epsx(1..4)`, P3 P33 `epsx(1..6)`, P4 HMX `epsalldok` and `epsdok(2)` and P33
      `epsalldok`, P5 inpt `epsdok43_n[0..4]` print the original's value; N1 HMX
      `epsdokfr` still does; N2 no cell the baseline printed exactly is lost; N4 no
      counter or printed length moves except onto the original's, and HMX's and P33's
      generator counters equal the original's. Red: the port read from the baseline
      fails the 19 positive controls it missed; read from the `Binary64` run, N2 fails on
      all five, N4 on HMX, HPEPA3 and PSAN02n, the counters on HMX. Reverted, green.

      ⚠ 2026-10-01: `Snapshots/SeedZeroResultsM.approved.txt` re-approved: the ten
      `Original` hashes moved, the ten `Binary64` ones are unchanged (the rate table's
      keyed diff: 160 `Binary64` runs byte-identical, no counter moved in any run).

      ⚠ 2026-10-03: re-approved again, the plane following the executable's listing
      (x87 A, stage 4): nine of the ten `Original` hashes moved, the ten `Binary64` ones
      are unchanged, and the 39 cases above hold on the regenerated seed-0 runs.

      ⚠ 2026-10-03: was dated 2026-10-01; the 39 cases were re-run on the regenerated
      seed-0 runs and hold (`src/Statistics/ACCEPTANCE.md`, A5, A6), so that is the date
      of this evidence, one date for one piece of evidence (AGENTS.md §6).
- [x] 2026-10-02: the stored port output of each cycle-plane pair is what the port
      prints now (`CyclePlanePairsTieTests`, six pairs, `Category=Long`: reference mode,
      `Original` layout, seed 1, `Original` precision, every parameter read from
      `tests/Fixtures/cycle-plane-pairs/provenance.json`), so the ratchet of
      `tests/Statistics.Tests/PathIdenticalPairsReportTests` cannot go stale. Red on
      `epsx1`'s rounding at 814 removed (five of six), reverted, green.

- [x] `LineMapCoverageTests` reads `tests/Fixtures/cases/source/executable_lines.json`
      and is red once on a line the map no longer covers (stage S2, 2026-10-04: the row
      `297–342` of `src/Simulation/BOOT.md`'s line map cut to `297–341`: red, naming
      line 342 by number; reverted, green; 133 cases of the fast set with the original
      absent).

  ⚠ 2026-10-04: was "red once on a line removed from the list", which no mutation can
  meet: a shorter list asks the map for less. Reformulated as above.
- [x] 2026-10-04: Every fact that returns when no CUDA accelerator is available calls
      `Harness`'s `CudaRequirement.FailIfRequired`, found by a text check and not typed
      here (`CudaRequirementWiringTests`), and fails under `PROPSTRUCT_REQUIRE_CUDA=1` on a
      machine where CUDA is refused: 6 facts of this node, 1 of them in the fast set, red
      with `PROPSTRUCT_NO_CUDA=1` and the variable set, green without it (stage S4).

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

## Taboos

- Do not loosen a tolerance for the sake of green.
