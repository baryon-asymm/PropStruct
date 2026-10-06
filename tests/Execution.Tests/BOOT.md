# BOOT.md — Execution.Tests

## Purpose

The definition of what "`Execution` is ready" means. This node owns the tolerance
table of CUDA against the CPU accelerator and the recorded throughput figures.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | accelerator choice, the kill switch, the fallback reason; argument and state errors of the engine; `MaxBatchSize` capped so `Fold.AddField`'s `int` indexing cannot overflow; `SampleSize` runs on a refused engine and agrees with the CPU accelerator | the `API.md` of `Execution` | ✅ `AcceleratorTests` |
| L0 | every `System.Math` function the particle program calls, through the post-link on CUDA against the CPU accelerator; the CPU accelerator against `System.Math` bit for bit; `MathProbe.StrideCount` tied to `MathProbe.FunctionCount` | a probe kernel | ✅ `MathProbeTests` |
| L0 | `PROPSTRUCT_LIBNVVM_PATH`/`PROPSTRUCT_LIBDEVICE_PATH`: both set and found used directly, both set and either missing fails without falling back to search, one set or neither leaves discovery unchanged | constructed toolkit layouts under a temp directory | ✅ `LibDeviceLocatorTests` |
| L0 | `RunAttempts`, `DeriveStreams` and `FoldField` reach the CUDA post-link outside `Category=Long`: a tiny constructed batch through `RunBatch`/`RunContinuedBatch`/`SampleSize`, refusal asserted where CUDA is unavailable | a constructed setup, on CUDA when available | ✅ `CudaSmokeTests` |
| L0 | every `System.Math` member called in a method body of `Particle` or `Random` is named in `MathProbe.Functions` | an IL scan of both assemblies, decoded from `System.Reflection.Emit.OpCodes` | ✅ `MathProbeCoverageTests` |
| L1 | fold by field = `Fold.Add` bit for bit; QKS1 refreshed exactly when `LoopCompletions` changed, including unconditionally by `WriteTotals` and by `Load`; each batch status from a constructed setup, including the in-launch stop shared between the kernel and the host through `Kernels.IsFailure`, at `attemptsPerLaunch > 1` | constructed setups built here (`ConstructedSetups`), of `Particle.Tests`' kind | ✅ `FoldByFieldTests`, `Qks1RefreshTests`, `WriteTotalsRefreshesQks1Tests`, `BatchStatusTests` |
| L1 | `RunBatch`'s compacted index list over several particles at once, cycle 0, equals as many sequential `RunContinuedBatch` calls, bit for bit (integer and real totals, total attempts), with the relaunch of the compacted list genuinely exercised (`Launches > 1`) | a constructed setup run both ways | ✅ `RunBatchEqualsSequentialContinuedBatchesTests` |
| L2 | reference mode = continued batch with one attempt per launch, bit for bit, over the first 1000 particles of HPEPA3 | the two paths of the same engine | ✅ `ReferenceModeEqualsContinuedBatchTests` |
| L2 | QKS1 refresh timing under cycle 1: reference mode = a continued batch bit for bit per particle, and the refresh count equals `ΔLoopCompletions − 1` exactly, over 2000 of HPEPA3's real cycle-1 particles, with `RestartedAfterLoop` genuinely firing (`ΔLoopCompletions > particleCount`) so the identity cannot pass vacuously | HPEPA3 cycle 1, forked from a real cycle-0 run | ✅ `Qks1RefreshUnderCycle1Tests` (`Category=Long`) |
| L2 | determinism: two runs, and 1, 4, 16 CPU threads, bit for bit | the same batch | ✅ `DeterminismTests` (`Category=Long`) |
| L2 | CUDA against the CPU accelerator on whole-cycle batches of the five reference formulations | the tier table of this node | ✅ `TierTableTests` (`Category=Long`) |
| L2 | the host-thread path (`Engine`'s default on the CPU accelerator, root `BOOT.md`, "One particle program") equals the ILGPU-kernel-launch oracle (`forceIlgpuKernelsOnCpu: true`) bit for bit: 1000 of HPEPA3's own particles on every build; the full matrix (five formulations, batch 1/7/whole cycle, `attemptsPerLaunch` 1/2/256, 1/4/8/16 threads) as `Category=Long` | the oracle engine, forked from the same state | ✅ `HostThreadOracleTests`; `HostThreadMatrixTests` (`Category=Long`) |
| Measurement | throughput of the particle kernel on HPEPA3, CUDA and CPU with all cores | recorded in `src/Execution/BOOT.md`, never asserted | ✅ `ThroughputMeasurementTests` (`Category=Long`) |
| Measurement | host-thread throughput on HPEPA3's cycle 1 at 1/4/8/16 threads, and reference mode | recorded in `src/Execution/BOOT.md`, never asserted | ✅ `HostThreadThroughputTests` (`Category=Long`) |
| L2 | generator state identity: after a real run, each of the six streams' actual final state equals its initial state jumped ahead by the draws the run's own integer totals counted for it (`## Generator state identity (2026-09-19)` has the stream-to-counter mapping) | HPEPA3 cycle 1, 2000 particles, reference mode and a continued batch | ✅ `GeneratorStateIdentityTests` (`Category=Long`) |
| L2 | positive control (root `BOOT.md`, "Every check that guards a quantitative claim is proven twice"): `RunReferenceParticle`'s `Original`-kind record seed/commit, pre-seeding each REAL*4-classified field to its own `S = 2^k` and asserting it reads back exactly `S`, real*8 fields bit-identical to `Binary64` kind | one real HPEPA3 cycle-1 particle, both kinds | ✅ `AccumulationPositiveControlTests` (`Category=Long`) |
| Guard | every class of this assembly with a `Long` trait, on the class or on any method, found by reflection, is in the one serial collection ("## Invariants"); the count and the names are printed, never typed | the assembly's own types | ✅ `LongClassesSerialTests` |

## Invariants

- **The GPU/CPU tier table lives here, in one file**, every entry with its derivation.
  The particle program branches on thresholds everywhere (acceptance tests, pocket or
  bridge, the `1e-5` window truncation), so a last-ULP difference between libdevice and
  .NET can send one particle down another branch and change every draw after it. The
  table therefore has two parts: a tolerance on the totals of particles whose decisions
  agree, and a bound on the share of particles whose outcome sequence diverges. Both are
  measured in the first coding session, derived, recorded, and never loosened to go
  green.
- **The `Category=Long` classes run one at a time** (decided 2026-10-02, audit item 6).
  xunit runs the classes of different collections in parallel, and most Long classes
  hold a whole-cycle HPEPA3 batch of about 1.15 GB (an estimate from `Engine`'s
  formula, not measured) and saturate every CPU thread: overlapping, they could reach
  7-8 GB and would distort the throughput figures two of them record. Every class with
  a `Long` trait is therefore in one `[CollectionDefinition]` with
  `DisableParallelization = true` (`LongClassesSerialTests.Name`), and
  `LongClassesSerialTests` finds those classes by reflection and asserts each carries
  the collection. Measured 2026-10-02, one run each, `Category=Long` on CUDA, the
  testhost's `PeakWorkingSet64`: 6.82 GiB with the classes in parallel (wave8 5f0bdf7),
  1.36 GiB serialized; wall time 4 min 51 s against 8 min 01 s, 20 of 20 passed both.
- Expected values come from `tests/Fixtures`, never typed into a test.
- A test that needs CUDA and finds none, or finds the kill switch set, asserts the
  refusal instead of skipping: the suite is green, and meaningful, on a machine without
  NVIDIA software.
- The L2 rows over reference formulations get their setup from `Statistics.Setup.Prepare`
  with the tail draw completed through `Engine.SampleSize` (`src/Simulation/API.md`,
  "Setup hand-off"); no stand-in setup is built here.

## Dependencies

- [Execution](../../src/Execution/API.md) — what is being checked.
- [Particle](../../src/Particle/API.md) — layouts, `Fold.Add`, outcomes.
- [Random](../../src/Random/API.md) — stream layouts for continued batches.
- [Statistics](../../src/Statistics/API.md) — setups of the reference formulations.
- [Input](../../src/Input/API.md) — reading the reference formulations.
- [Harness](../Harness/API.md) — repository paths, bit comparison.
- [Fixtures](../Fixtures/API.md) — the reference formulations.

Outside the tree: xunit.

## Constraints

- Part of the default test command. Rows over whole reference formulations are marked
  `Category=Long` if they take more than a few seconds, measured before deciding.
- CUDA rows are tagged so that the guarded merge can run them without the kill switch.

## Acceptance criteria

- [x] Every row of the levels table is green, with a date and the names of the tests:
      2026-09-18, the levels table above.

  ⚠ 2026-09-18, the Opus audit: this tick was true and still is, but it was read as
  stronger than it is. Every QKS1 row (`Qks1RefreshTests`, `WriteTotalsRefreshesQks1Tests`,
  and `ReferenceModeEqualsContinuedBatchTests`) ran at `cycleFlag = 0`, where `Attempt.Run`
  never reads QKS1 and, on HPEPA3, almost every completed loop is `Accepted` — so a
  refresh misplaced relative to the attempt loop (moved out of the relaunch loop or the
  reference attempt loop) left every one of them green regardless. "Green" therefore
  proved the refresh mechanics in isolation, not that a wrongly timed refresh would be
  caught at the one cycle (`cycleFlag = 1`) where QKS1 actually feeds `Attempt.Run`. The
  new `Qks1RefreshUnderCycle1Tests` row closes exactly that gap, over real cycle-1 data
  with a guard against a vacuous pass; the table above now names it.
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13):
      2026-09-18 — `FoldByFieldTests` (wrong expected value on negative DpMaxCor test
      data caught the test's own bug before the kernel was trusted); `Qks1RefreshTests`
      (a `DateTime.UtcNow.Ticks >= 0` mutation of `MaybeRefreshQks1` turned the
      never-refreshes assertion red, reverted); `ReferenceModeEqualsContinuedBatchTests`
      (moving `RunReferenceParticle`'s QKS1 refresh from before to after each attempt
      broke bit-parity with a continued batch at particle 0, reverted); `ToFailureStatus`
      is exercised by `BatchStatusTests` over every `AttemptOutcome` value by a `[Theory]`
      that fails if a case is left unmapped; `WriteTotalsRefreshesQks1Tests` is the
      genuine article rather than a deliberate mutation — it was red against the code
      this session shipped before the coordinator's review (`src/Execution/BOOT.md`,
      "## Throughput measurement", "third look"), green after the fix, both observed.
      `Qks1RefreshUnderCycle1Tests` (moving `RunReferenceParticle`'s refresh outside its
      attempt loop, reverted); `RunBatchEqualsSequentialContinuedBatchesTests` (dropping
      odd-indexed unfinished particles from `RunParticlesCore`'s compacted relaunch list,
      reverted); `BatchStatusTests.NeighbourBudgetExceededStopsTheLaunchOnTheFailingAttempt`
      and `RunContinuedBatchStopsTheLaunchOnTheFailingAttempt` (removing `Kernels.IsFailure`
      from `RunAttempts`' own stopping condition, letting a failing attempt keep drawing
      for the rest of its launch, reverted); `MathProbeTests.StrideCountEqualsFunctionCount`
      (`StrideCount` bumped to 11, reverted); `AcceleratorTests.MaxBatchSizeIsCappedSoFoldsIndexingCannotOverflowInt`
      (the `int.MaxValue / RecordLength` cap removed from `ComputeMaxBatchSize`, letting
      `MaxBatchSize` settle at the plain `int.MaxValue` bound instead, reverted);
      `AcceleratorTests.SampleSizeRunsOnARefusedCudaEngineAndAgreesWithTheCpuAccelerator`
      (`ThrowIfRefused()` restored ahead of `SampleSize`'s ephemeral-accelerator fallback,
      reverted); `LibDeviceLocatorTests.LocateWithBothOverridesSetAndExistingUsesThemDirectlyWithoutSearching`
      (the override's own return branch of `LibDeviceLocator.Locate` neutralized so it
      never returns the override even when both files are found, reverted);
      `CudaSmokeTests` (`KernelCache.Load`'s CUDA branch made to throw unconditionally,
      turning the smoke row red at the very `SampleSize` call that exercises it,
      reverted); `MathProbeCoverageTests` (`Log` removed from `MathProbe.Functions`,
      caught as `PropStruct.Particle.Attempt.Run calls Math.Log`, reverted) — its own
      `totalCalls >= 10` guard against a vacuous pass is met by the real scan (dozens of
      calls found across both assemblies in this session's runs). Every
      `AcceleratorTests`/`MathProbeTests`/`ThroughputMeasurementTests`/`TierTableTests`
      row that branches on CUDA availability was also run for real against this
      machine's RTX 5070 Ti (no kill switch at all) after the injected-environment
      change of item 3, confirming the real-CUDA branch is still reached and still
      agrees with the recorded tiers, not only the refused-by-injection branch; so was
      `CudaSmokeTests` itself (712ms, real post-link, `Ok` status on both `RunBatch` and
      `RunContinuedBatch`).

  ⚠ 2026-09-18, the Opus audit: the same reading applies here as above — a mutation
  proven red at `cycleFlag = 0` does not prove a QKS1-timing check would catch a
  misplacement at `cycleFlag = 1`, since the mutations available at cycle 0 (moving a
  refresh call, not changing what it depends on) happened not to be the specific one the
  blocker found. `Qks1RefreshUnderCycle1Tests`' own mutation, listed above, is proven at
  the cycle where it matters.

- [x] The host-thread path row (`HostThreadOracleTests`, `HostThreadMatrixTests`) is
      proven non-degenerate by a real code mutation: 2026-09-19 —
      `Engine.RunParticlesCore`'s host-thread `Parallel.For` bound over the active list
      was changed from `active.Length` to `active.Length - 1` (dropping the last active
      particle of the launch from the host-thread dispatch only, leaving the
      ILGPU-kernel-launch oracle branch untouched), turning
      `HostThreadOracleTests.HostThreadsEqualsTheIlgpuCpuAcceleratorOracleBitForBitOverHpepa3sFirst1000Particles`
      red (`BatchCounters { Attempts = 6573, ... }` expected from the oracle against
      `{ Attempts = 6568, ... }` actually produced by the mutated host-thread path — five
      fewer attempts, matching five dropped particles across the batch's relaunches),
      reverted, green again. `HostThreadMatrixTests` exercises the identical dispatch
      branch (`Engine`'s own `_hostThreads` code path, not a second implementation of
      it), so this one mutation stands for both rows rather than being repeated at
      `Category=Long` cost. A mutation of the scratch-stride rounding itself
      (`Engine.RoundUpToCacheLineDoubles`) was not tried: the function only ever rounds
      *up* (`(length + 7) / 8 * 8`), so a stride bug that stayed at or above
      `scratchLength` — the only kind this rounding could plausibly produce — changes
      nothing this or any bit-identity check reads (scratch is discarded within the
      attempt that wrote it, never compared or folded); only a stride *below*
      `scratchLength` would make neighbouring particles' scratch regions overlap and
      corrupt each other's in-flight computation, which every bit-identity row here
      would in fact catch (a race, not silently), but reaching that from this five-line
      formula needs a difference-in-kind mutation (e.g. floor instead of ceiling on an
      input already a multiple of 8) that was judged not informative enough to spend a
      `Category=Long` run proving.

  ⚠ 2026-10-02: was "five fewer attempts, matching five dropped particles across the
  batch's relaunches", now five attempts are one particle dropped at its first launch.
  A drop on a relaunch cannot end: the skipped particle keeps its stale outcome and is
  relaunched forever (measured below). What the mutation was measured to prove: a
  skipped particle was folded as accepted with no attempt under status `Ok`, and only
  the counters assertion saw it, so the `Ok` assertion of this row guarded nothing
  against a skipped particle. The check that now does is "## Launch-advance check
  (2026-10-02)".

- [x] A launch that leaves an active particle's attempt count unchanged ends the batch
      with `ParticleNotRun`, proven twice: 2026-10-02, `ParticleNotRunTests` (right:
      unmutated runs return `Ok` with the oracle's counters; red: "## Launch-advance
      check (2026-10-02)").

- [x] The generator state identity holds bit for bit, over real HPEPA3 cycle-1 data, for
      both reference mode and a continued batch, and is proven non-degenerate by a real
      mutation of `Attempt.Run`, reverted, never committed (AGENTS.md §13): 2026-09-19,
      `GeneratorStateIdentityTests`, both cases green (`## Generator state identity
      (2026-09-19)` has the mapping, the measured deltas and the two mutations).
- [x] `Kernels.DeriveStreams` derives a batched particle's streams through
      `StreamSeeds.ForBatchedParticle`, not the unbatched `ForParticle` (root `BOOT.md`,
      "Execution model", ⚠ 2026-09-19; `src/Random/BOOT.md`, "Batched derivation"): every
      test of this node that reproduces `RunBatch`'s own derivation by hand
      (`RunBatchEqualsSequentialContinuedBatchesTests`, `TierTableTests`) switched to
      `ForBatchedParticle` in the same change; `RunReferenceParticle` and
      `RunContinuedBatch` are unaffected (they continue an already-derived `StreamSet`
      and never call either `ForParticle` method), so every test that reproduces *their*
      streams by hand (`AcceleratorTests`, `BatchStatusTests`, `CudaSmokeTests`,
      `GeneratorStateIdentityTests`, `Qks1RefreshUnderCycle1Tests`) is unchanged: 2026-09-19,
      `dotnet test tests/Execution.Tests --filter Category!=Long`, 48 fast cases green.
      Proven non-degenerate (AGENTS.md §13): `DeriveStreams`'s call reverted to
      `StreamSeeds.ForParticle`, reverted back after. Red: exactly
      `RunBatchEqualsSequentialContinuedBatchesTests.RunBatchOverCycle0EqualsSequentialRunContinuedBatchCallsBitForBit`
      (47 of 48 fast tests still passed) — the sequential side already called
      `ForBatchedParticle` by then, so a mismatched formula on either side of the
      comparison is caught. `tests/Random.Tests/BOOT.md`'s own mutation log records the
      companion mutations on the `Random` side of this same feature.

- [x] 2026-10-04 — Every fact that returns when no CUDA accelerator is available calls
      `Harness`'s `CudaRequirement.FailIfRequired`, found by a text check and not typed
      here (`CudaRequirementWiringTests`, `CudaRefusalBranches.Scan`; red once with the
      call removed from `MathProbeTests`, naming `MathProbeTests.cs:63`), and fails
      under `PROPSTRUCT_REQUIRE_CUDA=1` on a machine where CUDA is refused: with
      `PROPSTRUCT_NO_CUDA=1` and the variable `1`, 10 facts of this node fail (ten of
      the ten that branch on the refusal: `CudaSmokeTests`, `MathProbeTests`, two of
      `AcceleratorTests`, five `TierTableTests`, one `ThroughputMeasurementTests`), and
      with `PROPSTRUCT_NO_CUDA=1` alone the same 10 pass.

## Launch-advance check (2026-10-02)

`Engine.RunParticlesCore` takes a particle's downloaded outcome byte as evidence only if
its attempt count grew in that launch (`src/Execution/BOOT.md`, "A batch"); otherwise
the batch ends with `ParticleNotRun`. The mutation of the 2026-09-19 row,
`Parallel.For`'s bound `active.Length` changed to `active.Length - 1` in the host-thread
branch, applied to a scratch copy, reverted, never committed. Setups: HPEPA3's first
1000 particles (`attemptsPerLaunch` 64, as `HostThreadOracleTests`) and
`ConstructedSetups.ReachesNeighbourLoop` with 16 particles.

| Run | Before the fix | After the fix |
|---|---|---|
| HPEPA3, 1000 particles, 64 per launch | `Ok`, 6750 attempts, 1 launch (unmutated: 6752) | `ParticleNotRun`, 6750, 1 |
| constructed, 16 particles, 64 per launch | `Ok`, 23 attempts, 1 launch (unmutated: 24) | `ParticleNotRun`, 23, 1 |
| constructed, 16 particles, 1 per launch | never returned (killed after 120 s) | `ParticleNotRun`, 15, 1 |
| constructed, 1 per launch, skip only from the second launch on | not run | `ParticleNotRun`, 20 attempts, 2 launches |

The last row is a second mutation (`active.Length` at the first launch,
`active.Length - 1` after), made on the fixed code only: a particle skipped at a
relaunch is now caught at that launch, where before it would loop. The first-launch
rows show the same particle counted as accepted with no attempt: under `Ok`, two
attempts short on HPEPA3 (particle 999) and one on the constructed setup.
`ParticleNotRunTests` holds the unmutated half: `Ok` and the oracle's counters at 1
and at 64 attempts per launch (red under the first mutation, whose status is no
longer `Ok`), and the decision itself (`Engine.EveryActiveParticleRan`) on
constructed arrays, for a first launch, a relaunch, and a particle outside the active
list.

## Generator state identity (2026-09-19)

Added for the wave-8 assignment (background: two "tests green, property false" defects
elsewhere in the tree). Every existing row of the levels table above checks that two
execution paths agree with each other, or that a refresh count matches an expected delta;
none of them checks the generator itself against an independent oracle over a real run.
`GeneratorStateIdentityTests` does: after running real HPEPA3 particles (`## Levels`
table), it jumps each of the six original stream states ahead by the number of draws the
run's own integer totals counted for that stream (`Random.Mcg128.Advance`,
`src/Random/API.md`) and asserts bit equality with the streams `Engine` actually
produced.

**Stream-to-counter mapping**, derived from `src/Particle/API.md`'s "Accumulator layout"
and `src/Particle/BOOT.md`'s "Attempt structure" and "Integer totals" (this node did not
read `Attempt.cs` to derive it — only to build the one temporary mutation below, AGENTS.md
§3; the derivation from the two documents alone matched the real run exactly, see
"Measured", so no reading of `Attempt.cs` was needed to get the mapping right).
`src/Particle/BOOT.md`, "Draws in the original order from the original streams":
"Stream 1 → X, 2 → X0, 3 → X1, 4 → X2, 5 → X21, 6 → X3 and X4 (shared)." Particle's own
"Integer totals" table gives `NFX, NFY, NFZ, NFQ,
NFW` written by the single combined line list "463, 498, 518, 622, 662, 698" — six lines
for five counters, because `NFW` is written at two mutually exclusive lines, not because
the mapping is 1:1 by line position:

| Fortran line(s) | Attempt-structure block | Draw(s) | Counter | Stream(s) |
|---|---|---|---|---|
| 463 | `base` (448–484): "X, X0 → SIZE → Dr, fraction" | X, X0 — always paired, the base block draws exactly one of each every time it runs | `Nfx` | S1 (X), S2 (X0) |
| 498 | `loop 501` (487–501): "X1 (redraw if 1.0)" | X1, once per distance draw | `Nfy` | S3 (X1) |
| 518 | `345` (503–543): "X2, X21 → SIZE → Db" | X2, X21 — always paired, one neighbour draw is exactly one of each | `Nfz` | S4 (X2), S5 (X21) |
| 622 | `bridge` (580–667): "451: X3 → DM → Dkarm → VM (JJ = 0 → 451, the XSS5/NFQ updates repeat)" | X3; the same line re-executes on the `JJ = 0` retry named in the same sentence | `Nfq` | S6 (X3) |
| 662, 698 | `bridge`'s "Var#2: X4 vs k8/k7·pdoksmall → 550" (626–667) and `pocket 502`'s "Var#2: X4 < pdoksmall → 333" (671–703) | X4, drawn in exactly one of these two mutually exclusive branches per neighbour | `Nfw` | S6 (X4) |

Consequence: S1/S2 and S4/S5 each draw exactly as many times as their shared counter
(`Nfx`, `Nfz`) says; S3 draws exactly `Nfy` times; S6 carries both X3 and X4, so its own
total draw count is `Nfq + Nfw`, not either counter alone —
`GeneratorStateIdentityTests.AssertJumpAheadMatchesActual` is written accordingly, and is
the one place a stream's draw count is not a single counter's value.

**Measured** (`GeneratorStateIdentityTests`, HPEPA3, 2000 particles from cycle 1's first
ordinal, both reference mode and a continued batch at `attemptsPerLaunch = 8`): identical
deltas either way, `Nfx = 13582`, `Nfy = Nfz = 1145140` (every `loop 501` iteration falls
through into exactly one `345` block, so a distance draw and a neighbour draw always pair
1:1 — not a coincidence of this run, a structural consequence of the attempt's own control
flow), `Nfq = 26283`, `Nfw = 276539` (S6 total 302822). The mapping derived from
documentation alone matched the real run on the first attempt, at every one of the six
streams, with no adjustment (task instructions: "do not adjust the mapping to fit" — none
was needed).

**Non-degeneracy** (AGENTS.md §13), two mutations of `src/Particle/Attempt.cs`, each
applied, run, and reverted (`git diff` empty afterward, `dotnet build PropStruct.sln`
clean, both `GeneratorStateIdentityTests` cases green again):

- The `Nfq` increment at line 622 removed entirely (a draw not counted). Red: both cases,
  on the vacuity guard (`deltaNfq` was exactly 0, never reaching the jump-ahead assertion
  at all) — proving the guard itself matters, the same concern
  `Qks1RefreshUnderCycle1Tests` names for its own refresh-count identity.
- The same increment changed from `Atomic.Add(..., 1L)` to `Atomic.Add(..., 2L)` (a draw
  counted twice). Red: both cases, on the S6 jump-ahead identity itself (`expected` and
  `actual` `Mcg128State` values differ at every limb) — proving the core bit-equality
  assertion, not only the vacuity guard, catches a real miscount. This is the shape of
  defect the task's own background names (a counter over-counting by a fixed factor)
  reproduced directly in this generator's own counters.

- [x] The positive control of the `Original`-kind seed/commit wiring passes on real data
      and is proven non-degenerate both ways (root `BOOT.md`'s new taboo; AGENTS.md §13):
      2026-09-21, `AccumulationPositiveControlTests`. `S` is chosen per REAL*4-classified
      field, not once for all of them: the first cut used one `S` sized to the largest
      field's own scale (`JammedTotal`/`NnTotal`, order 1-10 on this particle) and, when
      the seed/commit split was reverted to prove the check red, only those same two of
      fourteen fields actually turned red — the other twelve (`Vks`, `Allvdok`, … , order
      1e-10 to 1e-2) round away *in plain `double`* against a shared `S` that large,
      whatever `AddReal4` does, so a shared `S` sized to the largest field masks a real
      defect on every smaller one. Per-field `S`, each bounded from that field's own
      largest touched cell under a real `Binary64`-kind run of the same particle, was
      adopted because of this finding: reverting the fix a second time with per-field `S`
      in place turns every one of the fourteen touched fields red, not two.

      Exactly one particle, not the many first tried, for the real*8 half of the claim:
      an early version ran 500 particles and found the real*8 fields agreeing with the
      `Binary64`-kind run to about 13 significant digits, not bit for bit — traced to
      `RunReferenceParticle` seeding and committing the record once *per particle*, so
      `Original` accumulates every particle's terms into one continuously-carried total
      (the shape reproduces the original's own single memory location) while `Binary64`
      commits each particle's own zero-seeded delta with one `Fold.Add`; the two group the
      identical terms differently, and `double` addition is not associative. No defect
      here, purely a property of comparing across more than one seed-then-commit cycle,
      but a real limit this control has to respect to make an "exactly" claim it can prove
      rather than merely observe. One particle is exactly one such cycle, and already
      reaches every one of the twenty-six record fields on HPEPA3's own first cycle-1
      particle (asserted by the test, not assumed): the coverage this control needed was
      never the reason for running many particles.

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.

## Mutations

- 2026-10-02, `LongClassesSerialTests`: right, the unmutated assembly, the guard finds
  the eight classes (`AccumulationPositiveControlTests`, `DeterminismTests`,
  `GeneratorStateIdentityTests`, `HostThreadMatrixTests`, `HostThreadThroughputTests`,
  `Qks1RefreshUnderCycle1Tests`, `ThroughputMeasurementTests`, `TierTableTests`) and
  passes; red, the `[Collection]` attribute removed from `TierTableTests` alone,
  reverted and never committed, the guard fails with "Of 8 classes with a Long trait,
  these are not in collection 'ExecutionLongSerial':
  PropStruct.Execution.Tests.TierTableTests"; repeated by the orchestrator on
  `DeterminismTests`, red naming it, green restored.

## Taboos

- Do not loosen a tier for the sake of green; a divergence is diagnosed (decision
  sequences first) before any figure moves.
- No skipped CUDA test: refusal is asserted instead.
