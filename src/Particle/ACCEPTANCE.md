# ACCEPTANCE.md — Particle

## Acceptance criteria

- [x] An attempt called on the host thread and the same attempt on the ILGPU CPU
      accelerator give bit-identical outcomes, streams, totals and records: 2026-09-17,
      `tests/Particle.Tests`, `HostEqualsCpuAcceleratorTests.RunMatchesOnHostAndTheCpuAccelerator`
      (constructed setups; the HPEPA3-scale version is `Execution`'s to add once it exists).
- [x] Bit snapshots of the first 1000 attempts of each reference formulation (outcomes,
      stream states, totals, records) guard refactorings: 2026-09-18,
      `tests/Particle.Tests`,
      `SnapshotTests.FirstThousandCycleZeroAttemptsMatchTheApprovedSnapshot` (one case
      per formulation; red on the `SpherePi` mutation its node records). Narrower than
      it reads: all 1000 are cycle-0 attempts under `Binary64`, so none builds the QKS1
      window, takes Var#1/Var#2 or the tests of lines 723–738, or rounds a binary32 sum.
- [x] The line map covers every executable line of Fortran 425–767, 1573–1586,
      1588–1629 and 1761–1776; the list of executable lines is generated from the
      source by a script, not typed: 2026-09-17, `tests/Particle.Tests`,
      `LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`.
- [x] Folding the same records in particle order gives bit-identical totals whatever
      order the particles ran in (records produced by permuted parallel runs):
      2026-09-17, `tests/Particle.Tests`, `FoldTests` (all four cases).
- [x] Each outcome of the table is produced by a test, the budgets and
      `IndexOutOfRange` by constructed inputs: 2026-09-17, `tests/Particle.Tests`,
      `AttemptOutcomeTests` (all six outcomes and both budgets) and
      `BridgeWindowTests.SamplePocketGuardsAnUnboundedSearch` (`IndexOutOfRange`'s own
      guard, unreachable through `Attempt.Run` under this node's preconditions, see
      the outcome table above).
- [x] The window built once per attempt equals the window rebuilt at every bridge, bit
      for bit, on constructed inputs: 2026-09-17, `tests/Particle.Tests`,
      `BridgeWindowTests.BuildIgnoresWhateverTheScratchHeldBefore`.
- [x] The same equality holds over the first 10⁵ attempts of HPEPA3 in cycle 1: the
      per-attempt hash sequences (outcome, streams, totals, record) of the unmodified
      code and of a scratch variant rebuilding the window at every bridge are
      identical: 2026-10-02, a measurement on c14b93f, nothing committed,
      `HISTORY.md#window-cycle1-measurement-2026-10-02`. Red when one cell of QKS1 is
      scaled before a later bridge (first differing attempt 2); right on two
      unmodified runs (identical). Reference mode, host threads, `Binary64`.

  ⚠ 2026-10-02: was "needs `Statistics.Setup.Prepare`", which `tests/Particle.Tests`
  has called since 2026-09-18 (`SnapshotTests`) while this stayed open; the cycle loop
  is what is missing. Found reconciling the open criteria. Later the same day: the
  cycle-1 state came from `tests/Execution.Tests`' `ReferenceFormulationDriver`.
- [x] Reference mode, in the `Original` layout at seed 0 and driven through `Execution`
      and `Simulation`, reproduces the counters of the reference `results.m` of HPEPA3
      within the count bound its test states: 2026-09-19,
      `tests/Simulation.Tests/Hpepa3CounterTests` (red on doubled denominators,
      `tests/Simulation.Tests/BOOT.md`, "## Mutations"). Agreement of every printed
      cell is the root's rate criterion, not this one.

  ⚠ 2026-10-02: was "Reference mode driven by a test over this node alone reproduces
  … within the statistical criterion (integration evidence before `Execution`
  exists)": `Execution` came first (2026-09-18), and one run within the criterion is
  no evidence (root `BOOT.md`, "the pass condition compares failure rates, not single
  runs"). Found reconciling the open criteria.
- [x] The `PrecisionKind.Original` rounding set is generated, never typed, and
      checked against the committed artefact by regeneration: 2026-09-21,
      `tests/Particle.Tests`, `RealFourClassificationGeneratorTests.
      ClassificationReproducesByteForByteOnRegeneration` (`classify-real4-
      accumulators.py verify`); the generator itself is proven non-degenerate by
      mutating the `real*8,allocatable :: VKS(:)` declaration to `real` in a scratch
      copy of the source and observing `Vks` flip from excluded to rounded (this
      session's own scratch, reproducing the fifteen/twelve split already recorded
      under "## Decisions where the port departs from a transcription").
- [x] `Attempt.Run`'s own actual rounding, observed by running the identical attempt
      sequence under both kinds, equals the generated classification for every
      sum-kind field (none untouched): 2026-09-21,
      `tests/Particle.Tests`, `PrecisionKindClassificationTests.
      RoundingSetMatchesTheGeneratedClassification`; `UnsetKindBehavesAsBinary64` for the
      default. Proven non-degenerate in both directions, each mutation reverted after:
      changing `Vsmkm`'s write site from `AddReal4` to a plain `+=` turned the check red
      naming `Vsmkm` ("classified 'yes' ... bit-identical"); changing `Vks`'s plain `+=`
      to `AddReal4` turned it red naming `Vks` ("classified 'no' ... disagreed").
- [x] `PrecisionKind.Binary64` costs no measurable throughput: 2026-09-21,
      `tests/Benchmarks`' own route, HPEPA3, Release, before this change and after (the
      figures and their spread: `HISTORY.md#generated-real4-membership-decision-2026-09-24`)
      — both differences fall inside the other run's own spread.
- [ ] Row 478, 533, 1761–1776 reaches beyond HPEPA3 and HMX: a scratch measurement of
      2026-10-03, not committed, saw the same one-event `ConditionBreaking(2)`
      difference on the PSAN01 path-identical pair (C2's undeclared
      `PSAN01 ConditionBreaking(2)[0]`, class P by `src/Statistics/ACCEPTANCE.md` A3's
      design attribution) and on P33 against its independent replicas (original 3.1e-6,
      port 0). Closed by that measurement committed with its place; the row's
      `Differing cells` widens only where `CompareSets` finds the cell, an owner's
      decision, since `SetCriterionTests` gates every declared entry.
