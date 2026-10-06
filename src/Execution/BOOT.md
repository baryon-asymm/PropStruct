# BOOT.md — Execution

## Purpose

Everything that knows accelerators exist: the ILGPU context, the CPU accelerator and
CUDA, the libdevice post-link, the kill switch, device discovery, the run-scoped device
buffers, the particle, stream-derivation, normalization and fold kernels, the relaunch
loop for unfinished particles, the host-thread driver of reference mode, and the one
host-side call of the particle program that `Statistics` needs (`SizeLaw.Sample` for the
tail draw of `PARAM`). It is apart from everything else so that the CPU path can be
built and tested with no NVIDIA software.

## Invariants

- Only this node names `ILGPU.Runtime.Cuda`; no ILGPU type is on its public surface.
- `PROPSTRUCT_NO_CUDA=1`, a missing device or missing libdevice leads to the CPU
  accelerator when the kind is `Auto`, and to `AcceleratorUnavailable` when `Cuda` was
  requested. The reason of a fallback is on the `AcceleratorInfo`.
- **Run-scoped state lives on the accelerator.** `Load` uploads the fraction table and
  allocates, for the whole run, the integer totals, the real totals, QKS1 and the cycle
  inputs; `SetCycle` replaces the cycle inputs; `ReadTotals` and `WriteTotals` are the
  only transfers of the totals, used by `Simulation` around the in-place rewrites of
  `Statistics` between cycles. Nothing is re-uploaded per batch except what the batch
  itself defines (its stream states and its index list).
- **QKS1 is the engine's.** It starts as `Normalize` of the loaded integer totals (zeros
  at a fresh run, BOOT of `Particle`, "Normalization"). Before every launch, the first of
  a batch and every relaunch, and in reference mode before every attempt, the engine
  refreshes it with `PocketHistogram.Normalize` if the integer total `LoopCompletions`
  differs from its value at the last refresh (root invariant "Batched mode freezes only
  QKS1, per launch"). `Normalize` runs as a single-thread kernel on the batch's
  accelerator, so QKS1 is computed by the same code everywhere. `Load` and `WriteTotals`
  both recompute it unconditionally rather than only resync the refresh bookkeeping
  (HISTORY.md#throughput-third-look): the totals they set are not necessarily an incremental step
  from what QKS1 last reflected, so there is no history to compare against, only a value
  to recompute from. `Qks1Refresher` (decided 2026-09-18, the Opus audit) owns the
  buffer and this bookkeeping so the two operations cannot be pulled apart again.

  ⚠ 2026-09-18: was "QKS1 is refreshed in reference mode after every attempt" (contradicted the code), now "before every attempt" → HISTORY.md#qks1-timing-correction
- **A batch** derives every particle's six streams in a kernel,
  `StreamSeeds.ForBatchedParticle(layout, seed, firstOrdinal + index)`; runs up to
  `attemptsPerLaunch` attempts of `Attempt.Run` per particle per launch, each particle
  keeping its stream state, record and attempt count between launches; relaunches the
  unfinished particles over an index list compacted in particle order, until every
  particle is accepted, or one passes `maxAttemptsPerParticle` (the batch ends with
  `AttemptCapExceeded`), or an attempt ends with a failure outcome (the batch ends with
  the matching status); then folds the records into the real totals. After every launch
  the host trusts a particle's outcome byte only if its attempt count grew in that
  launch (`Engine.EveryActiveParticleRan`; the kernel increments it before every
  attempt it runs): otherwise the particle was never run, its byte is the zero of a
  fresh control, which reads as `Accepted`, or the previous launch's, and the batch
  ends with `ParticleNotRun` (decided 2026-10-02), never `Ok` and never a relaunch.
  Which outcomes are
  failures is `Kernels.IsFailure`, one list shared by the kernel's own in-launch stop
  (`Kernels.RunAttempts`, so a launch with `attemptsPerLaunch > 1` does not keep drawing
  attempts past a failing one) and by the host's `Engine.ToFailureStatus` (decided
  2026-09-18, Opus audit item 5: the two were separate lists of the same three outcomes
  before, one of which could gain a fourth without the other noticing).

  ⚠ 2026-09-19: was `Kernels.DeriveStreams` calling `StreamSeeds.ForParticle` (the same per-particle jump reference mode and continued batches use), now `StreamSeeds.ForBatchedParticle`, which adds the role-group offset of `src/Random/BOOT.md`, "Batched derivation" → HISTORY.md#batched-derivation-stream-seeds
- **A continued batch** is a batch of one particle whose streams are passed in and
  returned instead of derived: `Original` or `Independent` layout, continued across
  particles and cycles by the caller. With `attemptsPerLaunch = 1` it is the batched
  counterpart of reference mode, and the two must agree bit for bit under `Binary64`
  accumulation (root invariant "Reference mode is batched mode degenerated").
- **`Original` accumulation is refused for batched execution, unconditionally** (decided
  2026-09-21, with the root: root BOOT.md, "Precision kind is an option of every
  run"). `RunBatch` and `RunContinuedBatch` both return
  `BatchStatus.OriginalPrecisionRequiresReferenceMode` before any attempt runs
  whenever the loaded setup's `Kind` is `Original`, including the degenerate
  one-particle, continued-streams call above: that call is sequential by construction,
  but the rule does not special-case it, since "Reference mode is batched mode
  degenerated" is a claim about `Binary64` accumulation only, and a rule that had to
  detect "is this particular batched call secretly sequential" would be exactly the
  per-schedule reasoning the ban on atomic `double` operations exists to avoid. This is
  this node's own **mechanism** guard, not a policy mirror: the batch-fold path has no
  order-preserving, true-magnitude-seeded accumulation `RunReferenceParticle` has, at any
  particle count, so it cannot reproduce binary32 accumulation correctly regardless of who
  calls it — a backstop for a caller reaching `Engine` directly, since
  `Simulation.SimulationOptionsValidator` refuses the same combination earlier still,
  before this engine even exists (architecture audit finding R1, 2026-09-26;
  `src/Simulation/BOOT.md`). `RunReferenceParticle` reads `Kind` once per particle only to
  choose how its record is seeded and committed (below), never to refuse anything.

  ⚠ 2026-09-26 (R1): was, in addition, "`Original` stream layout ... refused for batched
  execution, unconditionally" here too, with `RunContinuedBatch` carrying a `layout`
  parameter solely for it. Withdrawn: unlike precision, that refusal was policy, not
  mechanism — `Random.StreamLayout.Original` has no failure mode in this node's own fold
  path, which derives and runs its streams correctly, as measured before the policy
  decision (root BOOT.md, "Two stream layouts", the batched-`Original` row). It now lives
  only in `SimulationOptionsValidator`, ahead of `Statistics.Setup.Prepare`, the tail draw
  and `Engine.Load`; `RunContinuedBatch`'s `layout` parameter is dropped with it.
- **Reference mode** runs `Attempt.Run` on the host thread over CPU-accelerator memory,
  one attempt at a time, refreshing QKS1 itself before each attempt by the rule above.
  On acceptance it commits the particle's record: under `Binary64` the record starts at
  zero and folds onto the real totals (`Fold.Add`); under `Original` it starts as a copy
  of the real totals and is written back over them instead, so every rounded addition
  sees the true, run-scale magnitude — `Original` is sequential-only (root BOOT.md), so
  the record can stand in for the run's one REAL*4 accumulator here (decided
  2026-09-21). It requires an engine created with `AcceleratorKind.Cpu`; on another kind
  it returns `AcceleratorUnavailable`.

  ⚠ 2026-09-18: was "QKS1 is refreshed in reference mode after each attempt" (the same error as the "QKS1 is the engine's" bullet above), now "before each attempt" → HISTORY.md#reference-mode-qks1-timing-correction

  ⚠ 2026-09-21: was "`RunReferenceParticle` never checks `Kind`" and "then folds the particle's record" unconditionally, now the seed/commit split above → HISTORY.md#reference-mode-record-seeding
- **Fold in particle order, per field in parallel.** The fold kernel runs one thread per
  record field; each thread adds the batch's records of its field into the real total in
  particle order, with `Fold.AddField` of `Particle`. That is the association of
  `Fold.Add`, so the result does not depend on scheduling or on the accelerator's thread
  count, and a batch of one gives the bits of reference mode.
- Integer side effects reach the totals by atomic add inside `Attempt.Run`; nothing in
  this node writes a `double` from more than one thread.

## Dependencies

- [Particle](../Particle/API.md) — the attempt, the layouts, the size law, the
  normalization, the fold.
- [Random](../Random/API.md) — stream layouts and per-particle stream derivation.

Outside the tree: ILGPU 1.5.3; libnvvm and libdevice from CUDA Toolkit 12.8+ at run
time on the GPU path.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- `LibDevicePostLink` and `LibDeviceLocator` are taken from APThermo
  (github.com/baryon-asymm/APThermo, `src/Execution`) with namespace changes only,
  keeping their ILGPU version assertion. The comment of `LibDevicePostLink.cs` names
  that address (stage S2 of the delivery, 2026-10-04: it named a local path).

  ⚠ 2026-09-18: was "namespace changes only" for `LibDevicePostLink`/`LibDeviceLocator` taken from APThermo, now the direct-path override restored as `PROPSTRUCT_LIBNVVM_PATH`/`PROPSTRUCT_LIBDEVICE_PATH` → HISTORY.md#libdevicelocator-override-restored

  Every `System.Math` function the
  particle program calls has a libdevice wrapper and appears in a probe kernel compared
  against the CPU accelerator. `MathProbe.Functions` — the typed list a kernel needs,
  since kernel-compatible code cannot enumerate `Math` calls by reflection itself — is
  checked against the real calls of `Particle` and `Random` by
  `MathProbeCoverageTests`, an IL scan of both assemblies' method bodies (decoded from
  `System.Reflection.Emit.OpCodes`' own data, not a hand-transcribed opcode table): a
  new `Math` call added there without a matching entry here would otherwise compile,
  run correctly on the CPU accelerator (which has every `Math` overload as an
  intrinsic) and pass every existing test, leaving CUDA's answer for that one function
  silently unchecked (Opus audit item 4, 2026-09-18).
- **Memory per batch.** Per particle: one record (`RecordLength` doubles), one scratch
  (`ScratchLength` doubles), six stream states (96 bytes), an attempt count, an outcome
  and an index slot. `MaxBatchSize` is the largest particle count whose batch memory
  fits the record budget passed to `Create` (root: 2 GiB by default); `Simulation`
  splits a cycle into batches of at most that size. `MaxBatchSize` is additionally
  capped at `int.MaxValue / RecordLength`, below whatever the byte budget alone would
  allow: `Particle.Fold.AddField` indexes its record buffer as
  `particle * recordLength + field` in `int` (`../Particle/API.md`, "Fold by field"), and a
  batch above that bound would overflow that product. `Execution` owns the record
  budget, so it is the one that keeps a batch within what `Fold` can index, not `Fold`
  itself (`AcceleratorTests.MaxBatchSizeIsCappedSoFoldsIndexingCannotOverflowInt`,
  Opus audit item 8, 2026-09-18: at the default 2 GiB budget this cap is unreachable —
  it exists for a caller with an unusually large explicit budget).
- **Budgets are values passed per call**, never read from configuration here:
  `attemptsPerLaunch`, `maxAttemptsPerParticle`, and the two model budgets of
  `ModelSetup` (neighbour draws, pocket redraws), which `Attempt.Run` enforces.
- **Early measurement first** (root `## Constraints`, "Performance"): the first coding
  session measures the particle kernel's throughput (accepted particles per second) on
  HPEPA3's cycle 1 on CUDA and on the CPU accelerator with all cores, and records both
  here before any kernel is tuned.

## Throughput measurement (2026-09-18)

moved 2026-09-20: the initial, pre-fix cycle-1 throughput table →
HISTORY.md#throughput-initial-measurement

⚠ 2026-09-18: was the "second look" reading 262.75 attempts/particle as intrinsic to HPEPA3 and finding the cause in `Particle`'s own acceptance tests, now withdrawn — the "third look" traced it to `Engine.WriteTotals` never refreshing QKS1 after a wholesale rewrite of the totals → HISTORY.md#throughput-second-look

moved 2026-09-20: the withdrawn implication of the (also withdrawn) "finding for
`Particle`" → HISTORY.md#throughput-second-look-implication

⚠ 2026-09-18: was "the finding for `Particle`" above (already withdrawn), now the real root cause — `Engine.WriteTotals` resynced QKS1's refresh bookkeeping without ever recomputing QKS1 itself, leaving it at `Normalize` of the zero totals from `Load` through the whole of cycle 1; fixed by making `WriteTotals` call `RefreshQks1Now` unconditionally → HISTORY.md#throughput-third-look

moved 2026-09-20: the post-fix, pre-Release "Re-measured" table (itself later
superseded by "## Host-thread throughput" below) → HISTORY.md#throughput-re-measured-pre-release

⚠ 2026-09-19: was two throughput tables with no build configuration named, now known to matter — `-c Debug` understates every CPU-executed figure in this node by ≈ 2.75× (`PropStruct.Particle`/`PropStruct.Random` built unoptimized); CUDA is unaffected (39,311/s Debug vs. 39,101/s Release) → HISTORY.md#throughput-build-configuration-note

**The tier table below was re-measured too**, on the same fixed code — see "## GPU/CPU
tier table" for the real before/after figures and the re-derived tolerance.

⚠ 2026-09-18: was "the tier table needed no new figures" after the `WriteTotals` fix, now re-measured — the pre-fix and post-fix figures cannot be the same run, and the re-measured ones (with a QKS1-non-zero assertion added at every fork) are in "## GPU/CPU tier table" → HISTORY.md#tier-table-fourth-look

## Math probe tolerance (2026-09-18)

Measured once (`tests/Execution.Tests/MathProbeTests.CudaAgreesWithTheCpuAcceleratorWithinAMeasuredTolerance`,
this machine's RTX 5070 Ti): every function of the root's math list, run through the
libdevice post-link, agreed with the CPU accelerator's own `System.Math` to within
2.22×10⁻¹⁶ absolute (one machine epsilon at 1.0, the worst case on `Tan`) over a
representative domain in (0, 1). The test's own tolerance, 10⁻¹², keeps a thousandfold
margin over what was measured rather than the value itself.

## GPU/CPU tier table (2026-09-18)

Measured once per reference formulation (`tests/Execution.Tests/TierTableTests`, this
machine's RTX 5070 Ti), each a whole cycle-1 batch forked from one shared cycle-0 run
onto a CPU and a CUDA engine (`Engine.WriteTotals`/`SetCycle` reproduce the identical
starting point, the same technique `DeterminismTests` uses): the relative difference of
CUDA's aggregate real totals against the CPU accelerator's own, and, over a sample of
`min(2000, N)` cycle-1 particles run one at a time through `RunContinuedBatch` on both
accelerators from the same derived stream, the share whose final stream state diverged
(BOOT.md, class doc of `TierTableTests`: a diverging final stream is exactly "the same
decisions were not all taken", since the generator's own draws are pure integer
arithmetic and never differ between accelerators — only which branches consumed them
can).

moved 2026-09-20: the pre-fix tier table (all-zero QKS1 on both accelerators) →
HISTORY.md#tier-table-before-the-fix

**At attempt budget 8192**, the default the 2026-09-28 selection rule chose
(`tests/Simulation.Tests/BOOT.md`, "## Budget ladder (2026-10-01)"), re-measured
2026-10-01 in Release, one run; the tiers are unchanged and every figure is inside them.

| Formulation | Observed max relative diff | Observed diverged share | Tier: relative tolerance | Tier: max diverged share |
|---|---|---|---|---|
| HPEPA3 | 4.425692E-14 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
| inpt | 1.200943E-14 | 0/1000 | 5E-13 | 3/1000 (0.30 %) |
| P33 | 2.573474E-14 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
| PSAN02n | 2.495024E-15 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
| HMX | 2.187533E-14 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
⚠ 2026-10-01: was the table at budget 256 (2026-09-18, HPEPA3 2.372715E-14 the worst) →
HISTORY.md#tier-table-budget-256

The tier columns are derived, not copied from the observed ones — a margin of roughly
21x over the worst observed figure for the relative tolerance, the rule-of-three upper
confidence bound (`3/n`, ~95% confidence) rather than the literal `0` observed for the
diverged-share tier — and neither moves without a new measurement.

moved 2026-09-21: the full derivation narrative behind those two figures (why the
post-fix numbers genuinely differ from the pre-fix ones, why the observed columns are
not themselves the pass condition, and the exact arithmetic behind each tier column) →
HISTORY.md#tier-table-derivation-narrative

moved 2026-09-20: the grep-complete audit of which tests fork a cycle-1 state →
HISTORY.md#grep-complete-fork-audit

## Host-thread path (decided 2026-09-19)

Owner's decision after the wave-7 measurement and an advisory review by Fable 5.1 (root
`BOOT.md`, "One particle program", ⚠ 2026-09-19). Batched mode on the CPU and reference
mode run `Attempt.Run` directly on .NET threads over CPU memory; the ILGPU CPU accelerator
stays as a test oracle of the kernel path and carries no production load. To implement,
each point proven before the switch:

- **The launch structure stays.** A host-thread batch is launched, runs up to
  `attemptsPerLaunch` attempts per particle, collects the unfinished particles in
  particle order, refreshes QKS1 by the rule of "QKS1 is the engine's", and relaunches
  the remainder, exactly like the accelerator path. A free-running per-thread loop
  would break "Batched mode freezes only QKS1, per launch" and "Reference mode is
  batched mode degenerated".

  Implemented 2026-09-19 → HISTORY.md#host-thread-path-launch-structure-implementation
- **Reference mode is the host-thread path at batch 1, budget 1, continued streams.**
  `PocketHistogram.Normalize` is called on the host over the host buffers — the same
  static method, no kernel launch per attempt. (Measured before the change: 2 100 –
  3 400 particles/s in reference mode against 9 456/s for the same routine on one
  thread; the difference is about 45 µs per attempt, the scale of one accelerator
  launch with synchronisation.)

  Implemented 2026-09-19 → HISTORY.md#host-thread-path-reference-mode-implementation
- **Bit identity with the accelerator path, by matrix:** the five reference
  formulations; batch 1, 7 and the whole cycle; `attemptsPerLaunch` 1, 2 and 256, so
  relaunches are exercised; 1, 4, 8 and 16 threads. Integer totals by
  `Interlocked.Add` (what ILGPU's `Atomic.Add` is on the host); records folded in
  particle order, never completion order.

  Implemented and measured 2026-09-19: confirmed bit-identical over the whole matrix,
  `tests/Execution.Tests/HostThreadMatrixTests`; moved 2026-09-20: the coverage
  rationale (which combinations, and why the whole-cycle row is not repeated at every
  thread count) → HISTORY.md#host-thread-matrix-coverage
- **The accelerator oracle stays on every build:** a fast test runs 1 000 particles of
  HPEPA3 through the ILGPU CPU accelerator and the host-thread path and asserts equal
  bits, so "kernel-compatible" is exercised even though the accelerator no longer
  carries load. Selecting the accelerator is internal to the tree; the public
  `AcceleratorKind` is unchanged (`Cpu` means host threads).

  Implemented 2026-09-19: `Engine.Create`'s `forceIlgpuKernelsOnCpu` parameter (default
  `false`) is the selection point; `tests/Execution.Tests/HostThreadOracleTests` is the
  always-on row (HPEPA3's first 1000 cycle-0 particles, not `Category=Long`).
- **Figures are quoted only after the layout is right:** per-particle records and
  scratch are laid out so that neighbouring particles do not share a cache line (the
  16-thread spread of 27 000 – 43 000/s in the prototype points at false sharing and
  hyper-threading); the throughput is then re-measured at 1, 4, 8 and 16 threads and on
  the 8 physical cores, and recorded in "Throughput measurement".

  moved 2026-09-24: the `ParticleControl` packing correction and the records-not-padded
  omission found while implementing this →
  HISTORY.md#host-thread-path-layout-correction-and-omission

  Re-measured (this machine's RTX 5070 Ti host, 16 logical CPUs; "8 physical cores,
  pinned" was not attempted — see `HostThreadThroughputTests`' own remarks: process-wide
  affinity in the shared xunit test process would also narrow every concurrently running
  CUDA/CPU test in the same run, for reasons unrelated to them; a trustworthy
  physical-core figure needs its own process, out of scope here). "## Host-thread
  throughput" below has the figures.

  ⚠ 2026-09-19: was host-thread single-thread throughput read as "well short of the prototype's" and blamed on the layout fix, now traced to a `-c Debug` measurement; `-c Release` matches the prototype — full diagnosis in "## Host-thread throughput" → HISTORY.md#host-thread-path-coordinators-review-preview

## Host-thread throughput (2026-09-19, corrected the same day — see the ⚠ below)

Measured after the `ParticleControl`/scratch-stride layout above, on the reference
machine (RTX 5070 Ti, 16 logical CPUs), `tests/Execution.Tests/HostThreadThroughputTests`:
HPEPA3's cycle 0 run to completion (host-thread path, 16 threads, fast beforehand — see
below), then its whole cycle 1 (100 000 particles) run as one `RunBatch` call
(`AttemptsPerLaunch = 256`) at each thread count; reference mode over 5 000 of cycle 0's
own particles (`RunReferenceParticle`, inherently one thread). **`dotnet test -c Release`**
(see the ⚠ below for why this matters and supersedes the first measurement):

| Path | Accepted particles/s (3 repeats, `-c Release`) | Attempts/particle |
|---|---|---|
| Host threads, 1 | 8,754 / 8,911 / 8,829 | 6.72 |
| Host threads, 4 | 25,882 / 25,799 / 26,481 | 6.72 |
| Host threads, 8 | 36,749 / 35,664 / 36,014 | 6.72 |
| Host threads, 16 | 34,621 / 42,720 / 38,678 | 6.72 |
| Reference mode (1 thread, by construction) | 7,245 / 7,797 (2 repeats) | — |

Attempts/particle (6.72) matches the "## Throughput measurement" section's post-fix
figure exactly, as it must: the host-thread path calls the identical `Attempt.Run`
sequence, only the dispatch mechanism differs. These figures are close to the wave-7
prototype's own (1: 9,456; 4: 26,758; 8: 36,657; 16: 27,167–43,343, the prototype's own
noisiest row — this session's 16-thread spread, 34,621–42,720, lands inside it) and
close to CUDA's Release figure in the same session (39,101–39,311/s, "## Throughput
measurement" ⚠ below): the host-thread path essentially matches CUDA on this
formulation, as the prototype originally found and root `BOOT.md`'s decision rested on.

⚠ 2026-09-19: was a first cut reporting 3,157/s at one thread (three- to fourfold under the corrected figures), read as atomic contention on the shared integer totals; now known to be the same `-c Debug` JIT de-optimization of `PropStruct.Particle`/`PropStruct.Random` the earlier throughput table hit — re-measured `-c Release` matches the wave-7 prototype, atomic contention was never measured and is withdrawn, and `ThroughputMeasurementTests.CpuThroughputOnHpepa3sCycle1` now pins `forceIlgpuKernelsOnCpu: true` so the accelerator-path row cannot silently become a host-thread one → HISTORY.md#host-thread-throughput-coordinators-review

## Launch shape (decided 2026-10-03, W5)

On CUDA, `Kernels.RunAttempts` launches in groups of 64 threads (`LaunchShape`); every
other kernel, and every kernel on the CPU accelerator, keeps ILGPU's automatic choice.
A group size moves no bit: the PTX is compiled once, unspecialized, and only the grid
changes; integer totals are atomic, real totals fold in particle order
(`## Invariants`).
Chosen by `tests/Benchmarks`' W5 rule (cycle-1 particles per second against
ILGPU's automatic group of 256, medians of four rounds: HMX ×1.359, HPEPA3
×1.366); a constant, never configuration.

Same bits, read 2026-10-03 in the ship window (tree `531d66c`, two attempts,
`tests/Benchmarks/HISTORY.md#ship-window-2026-10-03`): `TierTableTests` reproduced the
2026-10-01 figures of the table above digit for digit, and the seed-0 CUDA `results.m`
hashes of the five reference formulations, time line aside, were equal across the
parent, the commit and the parent again.

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No model logic in this node: it launches `Particle`, it does not decide.
- No tolerance in this node's code; tiers live in `Execution.Tests`.
- No second copy of any `Particle` or `Random` function, not even for the host path:
  the host thread calls the same static methods over CPU-accelerator views.
- No `Statistics` type here: the root's dependency direction is `Execution` →
  `Particle`, `Random` only; host arrays cross the boundary as plain arrays.

## Design decisions (2026-09-18)

Settled in the design session that opened wave 6, closing the open questions of the
first sketch:

- **Buffers are run-scoped, not per batch.** Uploading the totals for every batch and
  downloading them afterwards would cost a round trip per batch for no gain: only
  `Statistics` reads or rewrites them, and only between cycles.
- **The fold is a kernel, one thread per field.** A cycle of HPEPA3 holds of the order
  of 10⁵ particles and records of several hundred doubles; a host fold would download
  the whole record buffer every batch. Per-field threads keep the particle order and
  therefore the bits.
- **QKS1 moves into the engine.** The first sketch asked the caller of reference mode to
  refresh QKS1; with relaunches inside `RunBatch` the caller cannot see the launch
  boundaries, so the rule has to live where the launches are. `Simulation` keeps only
  the cycle-level policy.
- **The tail draw of `PARAM` runs here** on the host thread over CPU memory
  (`Engine.SampleSize`), which is how `Simulation` completes the setup hand-off without
  `Statistics` ever creating an accelerator (`src/Simulation/API.md`, "Setup
  hand-off"). `SampleSize` runs even on a refused CUDA engine, on an ephemeral CPU
  accelerator built and disposed for that one call: it is `Simulation`'s first call on a
  freshly created engine, before `Load`, and `SizeLaw.Sample` has no accelerator-specific
  behaviour for CUDA's absence to block (decided 2026-09-18, Opus audit item 10 —
  `ProbeMath` is not changed the same way, because probing the bound accelerator's own
  math intrinsics is its entire purpose, and a refused engine has none to probe).

  ⚠ 2026-09-24: was `Kernels.SampleSize` compiled and launched through the same
  `KernelCache`/ILGPU-kernel-launch path `RunBatch` takes on a non-host-thread
  accelerator, even when the engine was itself CPU-bound (architecture audit finding
  D8), now `SizeLaw.Sample` called directly on this host thread for every engine kind,
  never through a compiled launcher → HISTORY.md#tail-draw-sample-size-host-path-fix
- **The tier table figures** are measured in the first coding session, as before.

⚠ 2026-09-18: was the first sketch's `RunBatch` taking `in CycleInputs` and host `Span`s directly, with `RunReferenceAttempt` asking its caller to refresh QKS1, now the run-scoped engine above (`Load`/`SetCycle`/`ReadTotals`/`WriteTotals`), since a caller without an accelerator cannot build the `ArrayView`s `CycleInputs` held and cannot see the relaunch boundaries a refresh needs → HISTORY.md#design-decisions-first-sketch-correction
