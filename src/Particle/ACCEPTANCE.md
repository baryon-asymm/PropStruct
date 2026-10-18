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

The `Corrected` model (`BOOT.md`, "## Corrected model"; designed 2026-10-18;
the four flags this node reads implemented 2026-10-18). Each fix is
proven twice (root `BOOT.md`, Taboos): red on the defect, right on an answer known
beforehand.

- [x] `None` reproduces the original bit for bit beyond cycle 0: a snapshot of the
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
      (`tests/Simulation.Tests`). 2026-10-18, `tests/Particle.Tests`,
      `CycleOneSnapshotTests.FirstTenThousandCycleOneAttemptsMatchTheApprovedSnapshot`
      (ten cases, five formulations under both kinds, `Snapshots/<formulation>.cycle1.approved.txt`,
      the apparatus `CycleOneRun`): recorded with `Attempt.cs`, `BridgeGeometry.cs` and
      `BridgeWindow.cs` checked out at da0754b, then matched by the code with `Corrections`
      unset after every later commit. Each case asserts, before it hashes, that an attempt
      was accepted and one restarted by 723–738, the window drew, a Var#1 pocket was counted
      and a Var#2 pocket (`Fmkarm2`) and bridge (`VmkmTotal2`) reached the record. Red,
      each branch forced on under `None` (scratch, reverted): `BridgeFrustum` and
      `ExactConstants` (the bool read `|| true`), 10 of 10 cases each; `Fmkarm2OnAcceptance`
      (line 720's loop moved after 738 under `ireset == 0`) and `WindowLowerEdge`
      (`Build(…, true, …)` in `Attempt.Run`) were first scratch emulations, 10 of 10 each;
      re-proved on their own implementations 2026-10-18 (the flag's bool read `|| true`
      under `None`, scratch, reverted): 10 of 10 cases each.
- [x] One flag changes what its entry names and nothing else: on the same cycle-1
      sequence, `BridgeFrustum` alone leaves outcomes, streams, integer totals and
      every record field but `Vsmkm`, `VmkmTotal` and `VmkmTotal2` bit-identical and
      moves those three; `Fmkarm2OnAcceptance` alone leaves everything but `Fmkarm2`
      bit-identical and moves it. Red: a scratch mutation of `bb` under
      `BridgeFrustum` turns the first red. 2026-10-18, `tests/Particle.Tests`,
      `CorrectionIsolationTests.BridgeFrustumMovesTheBridgeVolumeFieldsAndNothingElse`
      and `Fmkarm2OnAcceptanceMovesFmkarm2AndNothingElse` (HPEPA3, inpt, P33, 10⁴
      cycle-1 attempts, `Binary64`; PSAN02n and HMX are not in the set, as for the
      first half): green, and red, reverted, under `bb *= 1.1` in the frustum arm and
      under `record[JammedTotal] += 1e-3` in the `Fmkarm2OnAcceptance` arm, 3 of 3 each.
      `WindowLowerEdge` has no isolation case: its entry says it changes the draws that
      follow, so a bit-identical remainder is not claimed.
- [x] `BridgeFrustum`'s volume (`tests/Particle.Tests`, `BridgeGeometryTests`), at
      `r2/r1` of 0.6, 0.998 and 1 with `r1 = 50 µm`, `rk = 40 µm`, `a = 5 µm`
      (`Volume`'s own arguments; there `Original` is 69 times short at 0.998 and
      −1.17e−13 m³ at 1, computed 2026-10-18) and at the clamped gap `a = 0` of every
      jammed neighbour (lines 572–576). Right: the
      volume under the flag matches an independent numerical quadrature of the same
      solid, the frustum between the chord circles less the two caps, within 1e−9
      relative at all six. Red: the `Original` volume is negative at 1 and more than
      ten times short at 0.998 against the same quadrature. `JJ` and `bb` are
      bit-equal under both on every case, the `JJ = 0` returns included. 2026-10-18,
      `BridgeGeometryTests`: `BridgeFrustumMatchesAnIndependentQuadratureOfTheSolid` (six
      cases; composite Simpson of the cross-section along the axis, the chord circles
      placed from the triangle's coordinates),
      `TheOriginalVolumeIsNegativeAtEqualRadiiAndMoreThanTenTimesShortNearThem`,
      `JjAndBbAreBitEqualUnderBothVolumesOnEveryCase` (the fixture's cases and the three
      ratios, each at both gaps). The fact comparing the old overload with the flag unset
      was deleted 2026-10-18 (review C1): the old overload forwards that very call, so it
      could not fail; the `None` path is held by the snapshots and the cycle-1 guard.
      Red, reverted: the second cap's
      sign flipped in the flag's arm turns the quadrature cases red (volume 1.03e−13
      against 5.81e−14 at ratio 1); `bb *= 1.000001` there turns the JJ/bb case red alone.
      The clamped gap, 2026-10-18 (review R7): `l += 1e−6·r1` when `a == 0` in the flag's
      arm turns the three `a = 0` cases red (relative error 2.5e−6 to 2.9e−6) and no other.
- [x] `ExactConstants` at this node's sites. Right: on a constructed attempt the
      sphere-volume terms the record receives are π/6 times their cubes, `Allvdok` and
      `Svd` equal the `None` term times π/3.14159 and `Vks` equals π/6 times the recorded
      pocket cubes `Dp31`, each to 1e−14 relative (the effect is 8.45e−7), and the first
      pocket's radius, `rrL − (Dr + Db)/2`, equals the oracle with π at both `rrL` and
      `VP` to 1e−14. Red: under `None` the ratio is 1, and the radius is 1e−8 or more
      from the π oracle. The site list is generated (`Category=Legacy`): a test
      scans the four line-map ranges of the Fortran for every match of `3\.14\d*`,
      `12\.566\d*`, `0?\.333\d*` and of the quotient `1/3.` not preceded by a letter,
      digit or point, one mapping row per line and literal, and checks them both
      ways against a committed mapping file (Fortran line → C# site, `not ported`,
      `unchanged: <reason>` or `BridgeFrustum`), red on a literal added to a scratch
      copy of the source and on a mapping line removed, right on the real files. The
      constants row turns `fixed (ExactConstants)` only once `Statistics`' sites are
      done too. 2026-10-18, `tests/Particle.Tests`: `ExactConstantsTests` (`Allvdok` and
      `Svd` cells against the `None` cells times π/3.14159, `Vks` against π/6 of the
      recorded pocket cubes `Dp31`, since its radius `rrL − (Dr + Db)/2` moves with
      `rrL` under the same flag, all to 1e−14; under `None` the ratio is 1, 8.45e−7 away),
      `ExactConstantSiteTests` (`TheCommittedListTheMappingAndAttemptAgree`,
      `ARowMissingFromEitherFileIsReported`, `ASiteMissingFromAttemptOrAddedToItIsReported`,
      and the two `Legacy` facts, `ScanReproducesTheCommittedListOnRegeneration` and
      `AScratchCopyWithALiteralAddedIsReportedAndAnUntouchedCopyIsNot`; the list is
      `ExactConstantLiterals.generated.txt` by `scan-exact-constants.py`, the mapping
      `ExactConstantSites.txt`). Red, reverted: `Vks`'s site back to the literal turns the
      `Vks` test and the site check red; both constants forced on under `None` turn the
      ten cycle-1 cases and the five cycle-0 snapshots red; a row removed from the
      mapping, a row added to the list and a use added to `Attempt.cs` each name their
      line. 2026-10-18, the `rrL` site by value: `ExactConstantsTests.
      TheFirstPocketRadiusCarriesTheExactFourPiOfRrlUnderTheFlag` (one neighbour of a
      single fraction, `DpMax` against `ConstructedModel.PeekFirstNeighbour` given π at
      both sites; the oracle with the literal at `rrL` only is 1e−8 or more away):
      red, reverted, when the flag's arm of `fourPi` is the literal (1.06e−6 from the π
      oracle), and no other check of this item turns red then.

  ⚠ 2026-10-18: this item first said each of `Allvdok`, `Vks` and `Svd` "equals the
  `None` term times π/3.14159", which cannot hold for `Vks`: its radius
  `rk = rrL − (Dr + Db)/2` moves with `rrL` under the same flag, so its term is not the
  `None` term scaled. Found by T3's coder; reworded to what `ExactConstantsTests` tests,
  `Vks` against π/6 of the recorded pocket cubes, under both models.
- [x] `Fmkarm2OnAcceptance` on a constructed case (`NnMax` low enough that line 735
      fires after a Var#2 pocket is staged). Red: under `None` `Fmkarm2` grows by the
      staged sum on `RestartedAfterLoop`. Right: under the flag it is unchanged there,
      and an `Accepted` attempt adds the same bits under both. 2026-10-18,
      `tests/Particle.Tests`, `Fmkarm2OnAcceptanceTests` (each under both precision
      kinds): `UnderNoneARejectedAttemptStillAddsTheStagedSum` (`NnMax` 1e−9, outcome
      `RestartedAfterLoop`, `conditions(9)` = 1, `Fmkarm2` equal to the accepted run's
      cells, which the same seed gives with `NnMax` 1e9), `UnderTheFlagARejectedAttemptAddsNothing`
      (every cell 0), `AnAcceptedAttemptAddsTheSameBitsUnderBoth` (record, `Fmkarm2` and
      integer totals bit-equal). Red, reverted: the flag read forced off turns the second
      red (both kinds); the `None` cycle-1 and cycle-0 snapshots stay green with the write
      moved after line 738.
- [x] `WindowLowerEdge` on 10⁶ draws of `BridgeWindow.SamplePocket` over a window
      built from a constructed QKS1 with `mindk ≥ 2`. Right: under the flag
      `dpoc[0] = Di·(mindk − 1)` and the draws per QKS1 cell match the cell masses
      (χ² at `α = 10⁻³`). Red: under `None` the same test rejects, each draw one cell
      high. 2026-10-18, `tests/Particle.Tests`, `WindowLowerEdgeTests`
      (`Di = 0.5`, `Dr = 4`, `AK3 = 0.2`, `AK4 = 0.95`, cells 2..8 of a QKS1 of mass
      `1 + i`, 10⁶ draws from stream 6 of the original seeds, critical value 22.458 at
      6 degrees of freedom): `UnderTheFlagTheDrawsPerCellMatchTheCellMasses` (χ² =
      1.659, no draw outside cells 2..8, `dpoc[0] = 0.5`) and
      `UnderNoneTheSameTestRejectsEachDrawOneCellHigh` (χ² = 92 033, draws above cell 8,
      none in cell 2, `dpoc[0] = 1.0`); `AttemptRunPassesTheFlagToTheWindow` (the same
      constructed attempt moves a record cell or total under the flag), red, reverted,
      when `Attempt.Run` hands `Build` the flag as false.
- [ ] `WindowLowerEdge`'s positive control at program level, once `Simulation`'s seam
      exists: the flag alone on the 2026-09-24 audit's apparatus (reference mode,
      `Independent`, `Binary64`, seeds `k·2¹⁷`, `k = 0..7`, the five references)
      reproduces `Dqmkm2` −2.8 % to −7.0 % and `Zkarm` +4.9 % to +13.5 % within their
      standard errors (`tests/Simulation.Tests`, hand-run category).
- [x] The redraw rate of label 451 under `WindowLowerEdge`, measured in its coding
      task with a scratch counter in `Attempt.Run` (reverted): the largest count and the
      mean per bridge, against `None`, on `AccumulatorSweep`'s sequence with 10⁵ cycle-1
      attempts per reference formulation, recorded with the place. Closed below a tenth
      of `PocketRedrawBudget`, or by the proposal to `Simulation` `BOOT.md` asks for.
      2026-10-18, a scratch measurement on f72cde0, nothing committed, the full table in
      `HISTORY.md#window-lower-edge-redraw-rate-2026-10-18` (Release, reference mode,
      `Binary64`, host thread, one run per cell: a count of bridges, no spread). Largest
      number of draws of one bridge, `None` against the flag: HPEPA3 4 against 14, inpt 4
      against 5, P33 27 against 109, PSAN02n 5 against 7, HMX 17 against 40; mean draws per
      bridge, 1.0002–1.0117 against 1.0050–1.0301. The largest, 109 draws (108 redraws) on
      P33, is below the 1,280 a tenth of `PocketRedrawBudget` (12,800) asks, by a factor
      of 11.7: closed, no proposal. The figure bounds the 2.4·10⁵ bridges measured; a
      longer run's maximum is not bounded by it, and P33's tail (8 bridges of 20 draws or
      more, the largest 109) is the one to read first if a run ever fails
      `BridgeDrawBudgetExceeded` under the flag.
- [x] An attempt on the host thread and on the ILGPU CPU accelerator stays
      bit-identical with `Corrections = All` (`HostEqualsCpuAcceleratorTests`, its
      cases with the field set). 2026-10-18, `tests/Particle.Tests`,
      `HostEqualsCpuAcceleratorTests.RunMatchesOnHostAndTheCpuAccelerator`, the cases
      `BridgeFrustum`, `All` (accepted, cycle 1) and `All` with `NnMax` 1e−9 (rejected by
      line 735, `Fmkarm2` not committed), each against the host call, bit for bit in
      outcome, streams, totals, record and scratch. Red, reverted: the kernel launched
      with `Corrections` cleared turns the three cases red.
- [x] The members agree with the defect report: a test of `tests/Particle.Tests` reads
      `docs/ORIGINAL-DEFECTS.md`, collects the members named in `fixed (…)` values of
      the `Corrected` column, the reserved name `Model` (`src/Output`'s printing fixes,
      no flag) excepted, and asserts that set equals `CorrectionSet`'s members other
      than `None` and `All`, that each member has the value `API.md` lists and that
      `All` is their sum. 2026-10-18, `tests/Particle.Tests`, `CorrectionMemberRowTests`
      (22 cases, `CorrectionMemberRowCheck`): right on the real page, whose
      members (`ExactConstants` in two rows, `Model` absent) equal the enum's, and on
      the real `API.md`. Red on scratch pages through the same path: a member's only
      row set back to `unchanged` (each but that one; `ExactConstants`, which has two
      rows, stays named with one set back and is reported with both), a row naming an
      unknown member alone and next to a known one, and, in `API.md`, a changed value,
      an `All` that is not the sum and an unlisted member; `Model` alone and next to a
      member, and a `fixed (…)` outside a table row, are not reported. Red, reverted,
      on the real files: `WindowLowerEdge = 16` in `API.md`, and a `Phantom = 4096`
      member in `CorrectionSet.cs`. Committed with the criterion's own condition met:
      the last row flipped on the target branch (Particle: `BridgeFrustum`,
      `Fmkarm2OnAcceptance`, `WindowLowerEdge`, `ExactConstants`; `Statistics`: the
      other eight). `CorrectionSet` is internal: `PublicSurface.approved.txt` does not
      move for it.

  ⚠ 2026-10-18: was "not written" with `ExactConstants` and the eight `Statistics`
  members still `unchanged` (T4), now written once every one of the twelve flags has its
  `fixed (<Flag>)` row (T13a).
