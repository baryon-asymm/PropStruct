# ACCEPTANCE.md — Execution

## Acceptance criteria

- [x] The throughput of the particle kernel on CUDA and on the CPU accelerator is
      measured on HPEPA3 and recorded here, before any other kernel is optimized:
      2026-09-18, `tests/Execution.Tests/ThroughputMeasurementTests`, "## Throughput
      measurement" in `BOOT.md`.
- [x] Reference mode and a continued batch with `attemptsPerLaunch = 1` give
      bit-identical totals, stream states and counters over the first 1000 particles of
      HPEPA3: 2026-09-18,
      `tests/Execution.Tests/ReferenceModeEqualsContinuedBatchTests.ReferenceModeEqualsContinuedBatchBitForBitOverHpepa3sFirst1000Particles`.
- [x] The same batch on the CPU accelerator is bit-identical with 1, 4 and 16 threads,
      and between two runs: 2026-09-18,
      `tests/Execution.Tests/DeterminismTests.TheSameBatchIsBitIdenticalAcrossThreadCountsAndRepeatedRuns`.
- [x] The same batch on CUDA and on the CPU accelerator agrees within the tier table of
      `Execution.Tests`, and the share of particles with diverging decisions is bounded
      by that table: 2026-10-03, `tests/Execution.Tests/TierTableTests`, "## GPU/CPU
      tier table" in `BOOT.md`, at budget 8192.

  ⚠ 2026-10-03: was dated 2026-10-01, the day the figures were measured, now re-verified
  with the CUDA group of 64: the five figures read digit for digit the same, in both
  attempts of the ship window (`tests/Benchmarks/HISTORY.md#ship-window-2026-10-03`).
- [x] Folding by field in parallel equals `Fold.Add` bit for bit on the same records:
      2026-09-18, `tests/Execution.Tests/FoldByFieldTests.FoldFieldKernelEqualsFoldAddBitForBit`.
- [x] QKS1 is refreshed exactly when `LoopCompletions` changed since the last refresh:
      a batch whose attempts all restart inside the loop does not refresh it: 2026-09-18,
      `tests/Execution.Tests/Qks1RefreshTests`. Also: `WriteTotals` itself refreshes QKS1
      unconditionally from the totals it just wrote, rather than trusting them already
      consistent: 2026-09-18, `tests/Execution.Tests/WriteTotalsRefreshesQks1Tests` (the
      defect this closes is HISTORY.md#throughput-third-look, moved from
      "## Throughput measurement").
- [x] With the kill switch set, `Auto` runs on the CPU and `Cuda` returns
      `AcceleratorUnavailable`; reference mode on a CUDA engine returns
      `AcceleratorUnavailable`: 2026-09-18, `tests/Execution.Tests/AcceleratorTests`.
- [x] A particle passing the attempt cap ends its batch with `AttemptCapExceeded`, and
      each failure outcome of an attempt ends it with its own status (constructed
      setups): 2026-09-18, `tests/Execution.Tests/BatchStatusTests`.
- [x] The host-thread path (batched mode and reference mode on the CPU accelerator) is
      bit-identical with the ILGPU-kernel-launch oracle over the matrix of "## Host-thread
      path": five reference formulations, batch 1/7/whole cycle, `attemptsPerLaunch`
      1/2/256, 1/4/8/16 threads: 2026-09-19,
      `tests/Execution.Tests/HostThreadMatrixTests` (`Category=Long`). The always-on
      oracle row (1000 of HPEPA3's own particles, every build) is 2026-09-19,
      `tests/Execution.Tests/HostThreadOracleTests`.
- [x] `AcceleratorKind.Cpu` (and `Auto` without CUDA) means host threads in production;
      the ILGPU CPU accelerator is reachable only through the internal,
      test-only `Engine.Create(..., forceIlgpuKernelsOnCpu: true)`, and the public
      surface (`AcceleratorKind`, `AcceleratorInfo`) is unchanged: 2026-09-19,
      `tests/Execution.Tests/HostThreadOracleTests` (asserts `Accelerator.Kind ==
      AcceleratorKind.Cpu` under both), `PublicSurface.approved.txt` unchanged by this
      work (checked by `dotnet test tests/Protocol.Tests`).
- [x] Host-thread throughput is measured at 1, 4, 8 and 16 threads, and for reference
      mode, after the cache-line layout of "## Host-thread path" is in place, and
      recorded here: 2026-09-19, `tests/Execution.Tests/HostThreadThroughputTests`,
      run `-c Release` (`-c Debug`, `dotnet test`'s own default, understates every
      CPU-executed figure in this node by ≈2.75–3×, root cause and evidence in "##
      Host-thread throughput" in `BOOT.md`), "## Host-thread throughput" in `BOOT.md`.
- [x] Every test of this node green under the host-thread default, including the
      existing CUDA-vs-CPU tier table (now comparing CUDA with the host-thread path):
      2026-09-19, full `dotnet test tests/Execution.Tests` run (real CUDA, this
      machine's RTX 5070 Ti) — tiers did not move (`## GPU/CPU tier table` in
      `BOOT.md` is untouched by this work; TierTableTests passed against its existing
      figures without a new measurement).
- [x] `RunBatch` and `RunContinuedBatch` both refuse `Original` accumulation with
      `OriginalPrecisionRequiresReferenceMode`, before any attempt runs, on a
      constructed setup that would otherwise complete — including the degenerate
      one-particle, continued-streams call, proving the refusal does not depend on
      which route reaches batched execution — while `RunReferenceParticle` on the same
      setup is unaffected: 2026-09-21, `tests/Execution.Tests/PrecisionKindTests`.
- [x] (reformulated 2026-09-26, architecture audit finding R1 — original wording below)
      `RunBatch` and `RunContinuedBatch` derive and run streams of either layout to
      completion, `Original` included: this node no longer refuses a stream layout at all,
      since it has no mechanism reason to (`## Invariants` in `BOOT.md`, the 2026-09-26
      note). The refusal itself moved to `Simulation.SimulationOptionsValidator`:
      2026-09-26,
      `tests/Execution.Tests/StreamLayoutTests`.

      Original wording, true from 2026-09-21 to 2026-09-26: "`RunBatch` and
      `RunContinuedBatch` both refuse `Original` stream layout with
      `OriginalLayoutRequiresReferenceMode`, before any attempt runs, on a constructed
      setup that would otherwise complete — including the degenerate one-particle,
      continued-streams call — while `RunReferenceParticle` on the same setup and the
      `Independent` layout in both `RunBatch`/`RunContinuedBatch` are unaffected."
- [x] `RunReferenceParticle`'s `Original`-kind seed/commit wiring (root BOOT.md, "Every
      check that guards a quantitative claim is proven twice") passes a positive control
      with a known answer: one REAL*4-classified field pre-seeded through `WriteTotals` to
      its own `S = 2^k`, chosen far above its own largest term, reads back exactly `S`
      after a real HPEPA3 particle's attempts; every real*8 field grows bit-identically to
      the same particle under `Binary64` kind: 2026-09-21,
      `tests/Execution.Tests/AccumulationPositiveControlTests`. Proven non-degenerate by
      reverting the kind check (`RunReferenceParticle` always zero-starting the record) and
      observing every touched REAL*4 field read back wrong — under one `S` shared by every
      field, the first cut of this control, only two of fourteen did, the other twelve
      cells' own natural scale being far enough below the shared `S` that even the reverted
      `Binary64`-style addition rounded away on its own; per-field `S` was adopted because of
      that finding, not before it.

⚠ 2026-09-24: cited test names renamed for CA1707, meaning unchanged, no criterion
re-verified and no date moved (`tests/test-renames-2026-09-24.txt`).

- [x] A launch that leaves an active particle's attempt count unchanged ends the batch
      with `ParticleNotRun` (no fold, no relaunch): the host trusted the zero outcome
      byte of a particle the launch never ran, which reads as `Accepted`. 2026-10-02,
      `tests/Execution.Tests/ParticleNotRunTests` (right: unmutated runs return `Ok`
      with the oracle's counters; red: the recorded mutation, "## Launch-advance check
      (2026-10-02)" in that node's `BOOT.md`).
- [x] `Kernels.RunAttempts` on CUDA launches in groups of 64 (`LaunchShape`, "## Launch
      shape" in `BOOT.md`) and moves no bit: 2026-10-03, the ship window (two attempts,
      tree `531d66c`): `TierTableTests` reproduces the 2026-10-01 figures of the table in
      `BOOT.md` digit for digit, and the seed-0 CUDA `results.m` of the five reference
      formulations, time line aside, hashes equal between the commit and its parent
      (and the parent again); a control, HMX at seed 1, differs. Evidence and the
      SHA-256 of the logs: `tests/Benchmarks/HISTORY.md#ship-window-2026-10-03`.
