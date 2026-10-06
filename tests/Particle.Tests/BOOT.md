# BOOT.md — Particle.Tests

## Purpose

The definition of what "Particle is ready" means: the levels of verification, what
each is checked against, what is covered and what is not.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | `SizeLaw.Sample` (both size laws, the fraction-boundary edges), `BridgeGeometry.Volume` (the normal geometry with and without the internal swap, the `a ≥ 2·rk` and `bb < 0` early returns), `BridgeWindow.Build`/`.SamplePocket` (ordinary and early truncation, the `mindk = 1` identity shift, the empty window) on constructed inputs | the hand-derived values of `tests/Fixtures/cases/particle/{size_law,bridge_geometry_volume,bridge_window}.json`, computed by `tests/Fixtures/formulas_particle.py`, not typed | ✅ |
| L0 | layout offsets and lengths for the five reference formulations | `Ndok` against the reference `results.m`'s own `fqdokkarm` row length; `Nkarm`/`Ncat` against `Setup.Prepare`'s own reported sizes, since neither has an untrimmed printed array to check (see the row's own doc comment, `LayoutTests.cs`) | ✅ |
| L1 | one attempt per outcome, budgets, `IndexOutOfRange` | constructed setups and cycle inputs | ✅ |
| L1 | host thread = ILGPU CPU accelerator, bit for bit | the same code on both | ✅ |
| L1 | window once per attempt = window per bridge, constructed inputs | `BridgeWindow.Build` ignores whatever the scratch held before it (a fresh reset vs. an unrelated bridge's leftovers) | ✅ |
| L1 | window once per attempt = window per bridge, HPEPA3 in cycle 1 | the literal rebuild at every bridge over the first 10⁵ attempts | ⏳ |
| L1 | fold order independence | permuted parallel runs of the same particles | ✅ |
| L1 | line-map coverage | the list of executable lines generated from the source | ✅ |
| L1 | `PrecisionKind.Original`'s own rounding, by behaviour | the generated `RealFourAccumulators.generated.txt` (running the same attempt sequence under both kinds and diffing the records); the generator itself checked by regeneration | ✅ |
| L2 | reference-mode driver over this node on HPEPA3 | counters and histograms of the reference `results.m`, statistical criterion | withdrawn 2026-10-02: counters by `tests/Simulation.Tests/Hpepa3CounterTests`, every printed cell by the root's rate criterion (`src/Particle/BOOT.md`) |
| Snapshot | first 1000 attempts of cycle 0 per reference formulation | approved bit snapshots, one case per formulation carrying all 1000 attempts | ✅ |

The snapshot hashes raw `double` bits that depend on `Math.Log` and `Math.Pow`, which
CoreCLR takes from the C runtime; they hold on Windows 11 x64, UCRT with FMA3, .NET 10,
and each approved file opens with the `# platform:` line of that platform
(`tests/Harness/API.md`, "## Repository paths and bit snapshots"). The line is
information, not a gate; a platform move is re-approved under "## Taboos".

Order of coding (decided 2026-09-17): the rows on constructed inputs come first, with
`Particle` itself. The rows over a reference formulation (the layout row, the HPEPA3
window row, L2 and the snapshots) need `Statistics.Setup.Prepare` and are coded once it
exists; until then they stay ⏳ and no stand-in setup is built in this node.

⚠ 2026-09-18: this paragraph assumed the layout row and the snapshot row could start as
soon as `Statistics.Setup.Prepare` existed. It compiled `Setup.Prepare`'s own
signature but not a call to it from here: `src/Statistics/PropStruct.Statistics.csproj`'s
`InternalsVisibleTo` granted `PropStruct.Simulation`, `PropStruct.Simulation.Tests` and
`PropStruct.Statistics.Tests` only, and every type `Setup.Prepare` hands back (`Setup`
itself, `SetupStatus`, `SetupTables`, `TailDraw`, `PendingEchoes`, `SetupEchoes`) is
`internal` — found by a throwaway probe in this node (`ScratchAccessCheck.cs`,
referencing `Statistics.SetupStatus`, deleted after the check), which reproduced `error
CS0122: 'SetupStatus' is inaccessible due to its protection level`. Escalated per
AGENTS.md §11 (widening a neighbour's `InternalsVisibleTo` is `src/Statistics`'s own
contract, not this node's to change) and decided the same day at the tree root, which
owns both this node and `src/Statistics`: `src/Statistics` now also grants
`PropStruct.Particle.Tests` (and, for the same reason, `PropStruct.Execution.Tests`),
`src/Statistics/PropStruct.Statistics.csproj` and its `API.md`'s "Surface internal to
the tree" sentence updated in that node's own commit
(`build(statistics): expose the setup to Particle.Tests and Execution.Tests`). The
layout row and the snapshot row are coded below (`LayoutTests.cs`, `SnapshotTests.cs`,
`ReferenceFormulation.cs`), through `ModelParameters.Default` and `Input.DatFile.Read`
— confirmed against `results.m`'s own printed menu echoes (Dmin/Di/Dj = 10 mkm,
NnMin/max = 3/100, k5 = 0.25, eps = 5e-2, variant = 0, k7 = 8.2, k8 = 7.73, Zok* = 0 for
every one of the five) rather than assumed, since `tests/Fixtures/provenance.json`'s own
stdin for each (`"<name>\ny\n"`) does not by itself say which menu answer the lone `y`
confirms. `Input` is added to `## Dependencies` below accordingly. The HPEPA3 window row
and L2 stay ⏳ regardless, for the separate reason already stated: a cycle loop with
`Statistics` between cycles is `Simulation`'s, not this node's.

## Invariants

- Expected values come from `tests/Fixtures`, never typed into a test.
- Tolerances come from the root criterion through `tests/Harness`; the L0 and L1 levels
  are bit-exact.
- The L2 run is marked long and excluded from the fast set; the mark is checked by
  machine.

## Dependencies

- [Particle](../../src/Particle/API.md) — what is being checked.
- [Random](../../src/Random/API.md) — the original streams for the drivers.
- [Statistics](../../src/Statistics/API.md) — `Setup.Prepare`, the setup of a real
  formulation for the rows that need one (the layout row, the snapshot row, the
  HPEPA3 window row, L2).
- [Input](../../src/Input/API.md) — `DatFile.Read`, `ModelParameters.Default`: the
  formulation and the original's own default parameters for the same rows.
- [Harness](../Harness/API.md) — CPU host, bit snapshots, statistical comparison,
  `PythonScript`.
- [Fixtures](../Fixtures/API.md) — formulations, reference outputs, derived values.

Outside the tree: xunit.

## Constraints

- Part of the default test command.
- The test-only drivers (reference driver, per-bridge window) live in this node and
  call only the `Particle` surface; they are not a second implementation of the
  attempt.

## Acceptance criteria

- [x] Every row of the levels table on constructed inputs is green, with a date and the
      names of the tests: 2026-09-17, `PropStruct.Particle.Tests`, all classes
      (`AttemptOutcomeTests`, `HostEqualsCpuAcceleratorTests`, `BridgeWindowTests`,
      `FoldTests`, `LineMapCoverageTests`, `SizeLawTests`, `BridgeGeometryTests`), 46
      fast cases (`dotnet test tests/Particle.Tests`). At the time of this entry the
      rows over a reference formulation (L0's layout row, the HPEPA3 window row, L2,
      the snapshot) still stayed ⏳, per "Order of coding" above; the layout row and
      the snapshot are ticked separately below, once unblocked.
- [x] The L0 row is ticked for exactly the fixture cases it covers, not as a blanket
      claim (AGENTS.md §6, the quantifier "all"): 2026-09-17, `PropStruct.Particle.Tests`.
      `SizeLawTests.SampleMatchesTheFixtureBitForBit` runs all 6 cases of `size_law.json`,
      covering both `sizeLaw == 2` (lines 1772-1773) and the previously-untested
      uniform-in-1/D² branch (lines 1769-1770, cases `uniform_in_reciprocal_square_interior`
      and `fraction_boundary_at_upper_end_of_last_interval`).
      `BridgeGeometryTests.VolumeMatchesTheFixtureBitForBit` runs all 5 cases of
      `bridge_geometry_volume.json`, covering the normal geometry (with and without the
      internal swap), the `a ≥ 2·rk` early return and the previously-untested `bb < 0`
      early return (lines 1612-1615, case `jj_zero_from_negative_bb`) — this last branch
      is unreachable through `Attempt.Run` under this node's own preconditions, so the
      fixture is the only way to exercise it against the real formula.
      `BridgeWindowTests.BuildMatchesTheFixtureBitForBit` and
      `.SamplePocketMatchesTheFixtureBitForBit` run all 4 cases (3 with samples) of
      `bridge_window.json`, replacing the hand-typed `15.0`/`precision: 12` assertion
      this row used to rely on with a bit-exact comparison, per the invariant below.
- [x] Every check on constructed inputs is proven non-degenerate by a recorded mutation
      (AGENTS.md §13): 2026-09-17, six mutations applied and reverted, each seen red
      then green again (`git diff --stat` empty after revert):
      - SFR-check test: `src/Particle/Attempt.cs`, the base-fraction guard
        `fractions.PocketForming[fractionBase] == 0` → `== 1`. Red: 10 of 22 fast tests
        (every scenario built with `pocketForming: 1`, the default, now restarts before
        it can do anything else — `AttemptOutcomeTests.RestartedInsideLoopWhenTheBaseFractionDoesNotFormPockets`
        included, since it flips to the opposite, still-wrong branch).
      - Host-vs-CPU-accelerator test: `tests/Particle.Tests/HostEqualsCpuAcceleratorTests.cs`,
        `AttemptKernel.Run`'s `records[0] = records[0] + 1.0;` inserted after the call,
        confined to the kernel path. Red: exactly the three
        `RunMatchesOnHostAndTheCpuAccelerator` cases (19 of 22 fast tests still passed).
      - Fold-order test: `src/Particle/Fold.cs`, `Add`'s loop
        `particle = 0; particle < particleCount; particle++` → counting down from
        `particleCount - 1` to `0`. Red: exactly
        `FoldTests.AddsInParticleOrderNotSomeOtherOrder` (21 of 22 fast tests still
        passed; the other three `FoldTests` cases stayed green because their chosen
        values are order-insensitive or the reversal doesn't change a maximum).
      - Bridge-window-purity test: `src/Particle/BridgeWindow.cs`, `Build`'s reset loop
        `for (iks = 1; iks <= nkarm + 1; ...)` → `iks <= mindk - 1`, leaving
        `FQKS(mindk)` unreset. Red: exactly
        `BridgeWindowTests.BuildIgnoresWhateverTheScratchHeldBefore` (21 of 22 fast
        tests still passed): the "fresh" (zeroed) and "dirty" (garbage-filled) scratch
        builds disagreed on `nnn1` (6 vs. 1) because the dirty run's leftover garbage at
        that cell no longer got zeroed first.
      - `IndexOutOfRange`-guard test: `src/Particle/BridgeWindow.cs`,
        `SamplePocket`'s bound `i + 1 >= nnn1` → `i + 1 > nnn1` (an off-by-one that lets
        the search read one cell past the window). Red: exactly
        `BridgeWindowTests.SamplePocketGuardsAnUnboundedSearch`, which failed on the
        resulting out-of-range access (21 of 22 fast tests still passed).
      - Line-map-coverage test: `src/Particle/BOOT.md`, the `Attempt.Run` row's Fortran
        range `428–446` → `428–440`. Red: `LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`,
        naming the five now-uncovered lines (441–445) by number and source text.
- [x] Every check added or changed by the 2026-09-17 audit follow-up (the eight
      should-fix items and the three audit notes) is proven non-degenerate the same way:
      ten mutations applied and reverted, each seen red then green again
      (`git diff --stat` empty after revert):
      - `fractionCount`-honoured test (item 2): `src/Particle/SizeLaw.cs`, `Sample`
        mutated to re-derive `fractionCount = (int)cumulative.Length - 1` instead of
        using the parameter. Red: exactly the new
        `SizeLawTests.FractionCountComesFromTheParameterNotFromCumulativeLength` (fraction
        2 instead of the expected 0), the only test whose fixture deliberately makes the
        two counts disagree; all fixture-driven cases stayed green because their own
        arrays happen to be sized exactly `fractionCount + 1`.
      - Uniform-in-1/D² branch test (item 7): `src/Particle/SizeLaw.cs`, the `else`
        branch's `invLowerSquared - x1 * (...)` sign flipped to `+`. Red: exactly
        `SizeLawTests.SampleMatchesTheFixtureBitForBit(uniform_in_reciprocal_square_interior)`
        (the other `sizeLaw == 1` fixture case, `x1 = 0`, is insensitive to the flip and
        stayed green, showing why more than one interior case is needed).
      - `bb < 0` branch test (item 7): `src/Particle/BridgeGeometry.cs`, the guard
        `bb < 0.0` changed to `bb < -100.0` (never fires for this fixture's own
        `bb ≈ -1.36`). Red: `BridgeGeometryTests.VolumeMatchesTheFixtureBitForBit(jj_zero_from_negative_bb)`
        and, incidentally, `AttemptOutcomeTests.BridgeDrawBudgetExceededAfterExactlyOneDrawWhenTheBudgetIsOne`
        (that scenario's own single required redraw turned out to depend on this exact
        branch to reject the first pocket-size draw).
      - Bit-exact `SamplePocket` test (item 6): `src/Particle/BridgeWindow.cs`,
        `SamplePocket`'s interpolation base cell `dpoc[i]` changed to `dpoc[i + 1]`. Red:
        all 8 `BridgeWindowTests.SamplePocketMatchesTheFixtureBitForBit` cases plus
        `BridgeWindowTests.BuildIgnoresWhateverTheScratchHeldBefore` and
        `AttemptOutcomeTests.BridgeDrawBudgetExceededAfterExactlyOneDrawWhenTheBudgetIsOne`
        (10 of 46 fast tests).
      - Budget-counter tests (item 4): `src/Particle/Attempt.cs`, `neighbourDraws >
        setup.NeighbourBudget` changed to `>=`. Red: exactly
        `AttemptOutcomeTests.NeighbourBudgetExceededAfterExactlyOneDrawWhenTheBudgetIsOne`
        (`NFY` came back 0, not 1: the budget now fires before the one draw budget 1 is
        meant to allow) — the exact `>` vs. `>=` confusion budget 0 could never
        distinguish. The same mutation on `bridgeDraws > setup.PocketRedrawBudget`
        turned red exactly `BridgeDrawBudgetExceededAfterExactlyOneDrawWhenTheBudgetIsOne`
        (`NFQ` 0, not 1), applied and reverted separately.
      - `DpMax`/`DpMaxCor` test (item 5): `src/Particle/Fold.cs`, `Add`'s
        `i == dpMax || i == dpMaxCor` narrowed to `i == dpMax` (and the now-unused
        `dpMaxCor` local removed). Red: exactly
        `FoldTests.DpMaxAndDpMaxCorTakeTheRunningMaximumNotTheSum` (`DpMaxCor` came back
        15.0, the sum of its own three records, instead of the expected maximum 9.0).
      - Host-vs-CPU-accelerator stream/scratch test (item 3): `tests/Particle.Tests/HostEqualsCpuAcceleratorTests.cs`,
        `AttemptKernel.Run`'s `laneStreams.S1.Low ^= 1UL;` inserted before the
        write-back. Red: all three `RunMatchesOnHostAndTheCpuAccelerator` cases, on the
        new `S1` comparison specifically (the old outcome/totals/record assertions all
        still passed). Reverted, then `scratches[0] = scratches[0] + 1.0;` inserted
        instead: red again on all three cases, this time on the new scratch comparison —
        applied and reverted separately, proving the stream check and the scratch check
        each catch a defect the other does not.
      - Self-checking required-ranges test (audit note c): re-ran the line-map-coverage
        mutation above (`428–446` → `428–440`) against the new
        `ParseRequiredRanges`-based derivation instead of the old hardcoded array. Red:
        the identical failure as before (`LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`,
        lines 441–445 missing), confirming the derived ranges do not shrink alongside a
        row's own mistake — the scope-declaration sentence they are read from is a
        separate paragraph the mutation never touches.

      Item 1 (moving the accumulator tables to `API.md`) and item 8 (deleting
      `ConstructedModel.SampleDirect`) are not paired with a red/green mutation: item 1
      is a pure documentation move with no executable check to break, and item 8 removes
      a forbidden duplicate rather than changing what any assertion tests — its assurance
      is structural (`grep` confirms no duplicate remains) and comes from
      `SizeLawTests`' own direct, fixture-driven proof of `SizeLaw.Sample` above, which
      `PeekFirstNeighbour`'s oracle now calls itself: any future defect in `SizeLaw.Sample`
      is caught there, on the function both the oracle and `Attempt.Run` now share, not on
      a copy that could silently drift from it. Audit note (b) (documenting the REAL*4
      decision variables) and note (a) (documenting the `X1 == 1.0` redraw as
      deliberately uncounted, resting on `Random/API.md`'s own `Next` contract) are
      likewise documentation-only.
- [x] The layout row (L0) and the snapshot row are green for all five reference
      formulations, through `Statistics.Setup.Prepare`/`Particle.SizeLaw.Sample`/
      `Statistics.Setup.CompleteEchoes` (`ReferenceFormulation.Prepare`) and
      `ModelParameters.Default`: 2026-09-18, `PropStruct.Particle.Tests`,
      `LayoutTests` (10 cases) and `SnapshotTests` (5 cases), 61 fast cases total
      (`dotnet test tests/Particle.Tests`, 3 s). The HPEPA3 window row and L2 stay ⏳,
      per "Order of coding" above.
- [x] The layout row's two checks are each proven non-degenerate (AGENTS.md §13):
      2026-09-18, `PropStruct.Particle.Tests`. Both mutations are confined to this
      node's own assertion code, not to `Statistics` or `Particle`: the tree root's
      own grant that unblocked this row (`## Dependencies`'s dated note above) was
      scoped narrowly to two files of `src/Statistics`, and a mutation of
      `Statistics.Setup.Sizes`'s own arithmetic (the more direct way to move `Ndok`
      itself) would exceed that grant. What is proven instead: that each assertion
      genuinely compares two independently-obtained values rather than one value
      against itself — not the strongest possible proof (it does not touch the
      production wiring the check ultimately guards), but real, since both operands
      still come from an external file and an external call, not from a literal typed
      into the test:
      - `NdokMatchesTheFqdokkarmRowLengthOfItsReferenceOutput`: the comparison
        `firstCategoryRow.Length == setup.Ndok` changed to `== setup.Ndok + 1`. Red:
        all 5 cases, each reporting the real `fqdokkarm(1,:)` length against the real
        `Ndok` `Setup.Prepare` computed (33/36/33/33/71), not a hand-typed pair.
        Reverted, green again (`git diff --stat` empty for `src/Particle` and
        `src/Statistics` throughout).
      - `LayoutIsBuiltFromTheSetupsOwnSizes`: the "expected" call's own `ndok`/`nkarm`
        arguments swapped, `AccumulatorLayout.Create(setup.FractionCount, setup.Nkarm,
        setup.Ndok, setup.Ncat, setup.Nc)`. Red: all 5 cases, every one failing on the
        `Qks` field first (the field whose offset depends on `Ndok` immediately
        preceding it in the layout — `API.md`, "Accumulator layout"). Reverted, green
        again.
- [x] The snapshot row is proven non-degenerate (AGENTS.md §13): 2026-09-18,
      `PropStruct.Particle.Tests`, against `src/Particle/Attempt.cs`, reverted after
      each check (`git diff --stat` empty for `src/Particle`):
      - A true one-ULP change of `FullSolidAngle` (`12.56636` → `12.566360000000001`,
        the next representable `double`) left all 5 formulations' snapshots
        unchanged — not a degenerate check, but a genuinely uninformative mutation:
        `FullSolidAngle` only gates the neighbour-loop continuation test `TU >
        12.56636/200` (Fortran line 716), and no attempt among the first 1000 of any
        of the five formulations' cycle 0 lands `TU` within one ULP of that threshold,
        so the branch never flips. Recorded rather than discarded, since a future
        reader retrying the "obvious" one-ULP mutation on this same constant would
        otherwise rediscover the same non-result.
      - `SpherePi` (`3.14159` → `3.14259`, a coarser, deliberately non-ULP change,
        since the ULP attempt above showed the finer one proves nothing for this
        family of constants): red on all 5 formulations, each reporting the approved
        hash against a different computed one. Reverted, green again.
- [x] The `PrecisionKind.Original` rounding row is green and proven non-degenerate
      in both directions: 2026-09-21, `PropStruct.Particle.Tests`,
      `RealFourClassificationGeneratorTests.ClassificationReproducesByteForByteOnRegeneration`
      and `PrecisionKindClassificationTests` (`RoundingSetMatchesTheGeneratedClassification`,
      `UnsetKindBehavesAsDouble`). Mutations, each reverted after (full detail:
      `src/Particle/BOOT.md`, "## Acceptance criteria"): a rounded field's own
      `AddReal4` call site changed to a plain `+=` turned the check red naming that
      field; an excluded field's own plain `+=` changed to `AddReal4` turned it red
      naming that field. The generator is proven non-degenerate separately, by
      mutating a Fortran declaration in a scratch copy of the source.
- [x] The snapshot files name the platform they hold on, as information and not as a
      gate: 2026-10-02, `PropStruct.Particle.Tests` `SnapshotTests` (5 cases) and
      `PropStruct.Random.Tests` `DrawStreamTests`, against `BitSnapshot`'s own platform
      line (`tests/Harness/API.md`). Right: unmutated, all green with the platform line
      (Windows 11 x64, .NET 10.0.12, ucrtbase 10.0.26100.9444, fma3 True; exact text in
      every approved file) as the first line of every approved file and every hash line
      byte-identical (`git diff` shows seven added lines and no other change). Red: one
      hash of `HMX.attempts.approved.txt` changed in place and its platform line
      replaced by a Linux one, so that the platforms differ; the case failed, naming
      both the approved and the current `# platform:` line and saying that this may be a
      platform move, and the received file opened with the current line. Information,
      not a gate: the platform line alone changed (`Arm64`, `fma3 False`) with every
      hash equal, all 5 cases still passed. A file with no platform line (HMX's, first
      line deleted) passed all 5 cases, and reads "none recorded" in the failure
      message. Each mutation reverted (`git diff` empty for the file).

- [x] `LineMapCoverageTests` reads `tests/Fixtures/cases/source/executable_lines.json`
      and is red once on a line the map does not cover (stage S2, 2026-10-04: line 447,
      inside `425–767` and in no row, added to the list: red, naming it by number;
      reverted, green);
      `RealFourClassificationGeneratorTests` is `Category=Legacy` (1 fact: green with
      the variable set, red with it unset, 0 of 1); 81 cases of the fast set with the
      original absent.

  ⚠ 2026-10-04: was "red once on a line removed from the list", which no mutation can
  meet: a shorter list asks the map for less. Reformulated as above.

## Taboos

- Do not loosen a tolerance for the sake of green.
- Do not re-approve a moved snapshot after a refactoring.
- Do not fold a platform move into a code change: an approved file whose hashes move
  because the platform moved (another OS, another C runtime, another CPU's FMA3
  choice) is re-approved in a commit of its own that names both platforms, the old
  `# platform:` line and the new one.
