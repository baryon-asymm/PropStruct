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

The `Corrected` model (`BOOT.md`, "## Corrected model"; designed 2026-10-18, none
implemented yet). Each fix is proven twice (root `BOOT.md`, Taboos): red on the
defect, right on an answer known beforehand.

- [ ] `None` reproduces the original bit for bit beyond cycle 0: a snapshot of the
      first 10⁴ cycle-1 attempts of each reference formulation (outcome, streams,
      totals, record; reference mode, under both precision kinds), recorded on the
      commit before the first `Corrected` statement and matched with `Corrections`
      unset after every later change (`tests/Particle.Tests`, beside `SnapshotTests`,
      on `AccumulatorSweep`'s sequence: cycle 0, then cycle 1 with QKS1 refreshed after
      every completed loop, `pdoksmall` zero and `Dmaxxx` 1e30, so the window, `VM`,
      both Var#2 branches and the tests of 723–738 all run; the driver of
      `tests/Execution.Tests` is outside this node). Red:
      each of the four flags' branches forced on under `None` (scratch, reverted)
      turns it red. The run-level guard is the root's seed-zero snapshot
      (`tests/Simulation.Tests`).
- [ ] One flag changes what its entry names and nothing else: on the same cycle-1
      sequence, `BridgeFrustum` alone leaves outcomes, streams, integer totals and
      every record field but `Vsmkm`, `VmkmTotal` and `VmkmTotal2` bit-identical and
      moves those three; `Fmkarm2OnAcceptance` alone leaves everything but `Fmkarm2`
      bit-identical and moves it. Red: a scratch mutation of `bb` under
      `BridgeFrustum` turns the first red.
- [ ] `BridgeFrustum`'s volume (`tests/Particle.Tests`, `BridgeGeometryTests`), at
      `r2/r1` of 0.6, 0.998 and 1 with `r1 = 50 µm`, `rk = 40 µm`, `a = 5 µm`
      (`Volume`'s own arguments; there `Original` is 69 times short at 0.998 and
      −1.17e−13 m³ at 1, computed 2026-10-18). Right: the
      volume under the flag matches an independent numerical quadrature of the same
      solid, the frustum between the chord circles less the two caps, within 1e−9
      relative at all three. Red: the `Original` volume is negative at 1 and more than
      ten times short at 0.998 against the same quadrature. `JJ` and `bb` are
      bit-equal under both on every case, the `JJ = 0` returns included.
- [ ] `ExactConstants` at this node's sites. Right: on a constructed attempt each
      sphere-volume term the record receives (`Allvdok`, `Vks`, `Svd`) equals the
      `None` term times π/3.14159 to 1e−14 relative (the effect is 8.45e−7). Red: under
      `None` the ratio is 1. The site list is generated (`Category=Legacy`): a test
      scans the four line-map ranges of the Fortran for every match of `3\.14\d*`,
      `12\.566\d*`, `0?\.333\d*` and of the quotient `1/3.` not preceded by a letter,
      digit or point, one mapping row per line and literal, and checks them both
      ways against a committed mapping file (Fortran line → C# site, `not ported`,
      `unchanged: <reason>` or `BridgeFrustum`), red on a literal added to a scratch
      copy of the source and on a mapping line removed, right on the real files. The
      constants row turns `fixed (ExactConstants)` only once `Statistics`' sites are
      done too.
- [ ] `Fmkarm2OnAcceptance` on a constructed case (`NnMax` low enough that line 735
      fires after a Var#2 pocket is staged). Red: under `None` `Fmkarm2` grows by the
      staged sum on `RestartedAfterLoop`. Right: under the flag it is unchanged there,
      and an `Accepted` attempt adds the same bits under both.
- [ ] `WindowLowerEdge` on 10⁶ draws of `BridgeWindow.SamplePocket` over a window
      built from a constructed QKS1 with `mindk ≥ 2`. Right: under the flag
      `dpoc[0] = Di·(mindk − 1)` and the draws per QKS1 cell match the cell masses
      (χ² at `α = 10⁻³`). Red: under `None` the same test rejects, each draw one cell
      high.
- [ ] `WindowLowerEdge`'s positive control at program level, once `Simulation`'s seam
      exists: the flag alone on the 2026-09-24 audit's apparatus (reference mode,
      `Independent`, `Binary64`, seeds `k·2¹⁷`, `k = 0..7`, the five references)
      reproduces `Dqmkm2` −2.8 % to −7.0 % and `Zkarm` +4.9 % to +13.5 % within their
      standard errors (`tests/Simulation.Tests`, hand-run category).
- [ ] The redraw rate of label 451 under `WindowLowerEdge`, measured in its coding
      task with a scratch counter in `Attempt.Run` (reverted): the largest count and the
      mean per bridge, against `None`, on `AccumulatorSweep`'s sequence with 10⁵ cycle-1
      attempts per reference formulation, recorded with the place. Closed below a tenth
      of `PocketRedrawBudget`, or by the proposal to `Simulation` `BOOT.md` asks for.
- [ ] An attempt on the host thread and on the ILGPU CPU accelerator stays
      bit-identical with `Corrections = All` (`HostEqualsCpuAcceleratorTests`, its
      cases with the field set).
- [ ] The members agree with the defect report: a test of `tests/Particle.Tests` reads
      `docs/ORIGINAL-DEFECTS.md`, collects the members named in `fixed (…)` values of
      the `Corrected` column, the reserved name `Model` (`src/Output`'s printing fixes,
      no flag) excepted, and asserts that set equals `CorrectionSet`'s members other
      than `None` and `All`, that each member has the value `API.md` lists and that
      `All` is their sum. Red on a scratch page naming an unknown member and on one
      member's rows set back to `unchanged`; right on the real page. Committed with the
      fix that flips the last row: until then the two sets cannot be equal, and a check
      that cannot be green is not committed (AGENTS.md §13). `CorrectionSet` is
      internal: `PublicSurface.approved.txt` does not move for it.
