# ACCEPTANCE.md — Harness

## Acceptance criteria

- [x] The parser reads every `results.m` of the fixtures, and writing its parse of the
      references into a canonical form and back loses nothing. (2026-09-17,
      `tests/Harness.Tests`, `ResultsMFileTests.ParsesEveryReferenceAndReplicaFileWithoutThrowing`
      over all 5 references and 112 replicas, and
      `ReferenceParseRoundTripsThroughJsonWithoutLoss` over all 5 references.)
- [x] 2026-10-02, reformulated 2026-09-24 (AGENTS.md §6, like root's gate-1 criterion):
      the original's own null rate is measured, not required to be zero on the single
      archived run — the original itself fails "no failure" in 15 of its 197 runs
      against its own replicas, a property of the archived program, not of a single
      run. (`tests/Harness.Tests/NullRateCalibration`; "## Null rate of the original"
      in `BOOT.md` has the figures. The "fails when shifted" half of the original
      wording is proven by the `tests/Harness.Tests/ACCEPTANCE.md` mutations of the
      count floor, the canonical-axis exclusion and the print-resolution guard.)

      ⚠ 2026-09-17: was ticked, then unticked the same day (13-28% of cells failed
      against GSV=3 replicas), now ticked again after the criterion revision moved the
      replica kind to lagged → HISTORY.md#ac-original-reference-ticked-unticked-ticked

      ⚠ 2026-09-20: unticked, then split into a fast-set pass and a long-set HMX test
      deliberately left red on the known open cell →
      HISTORY.md#ac-gate1-unticked-2026-09-20, HISTORY.md#ac-gate1-fast-long-split

      ⚠ 2026-09-24: was left red on purpose, now rewritten to a ratchet on this exact
      cell (`HmxOwnGsv2ReferenceMatchesTheKnownOpenCell`), pooled into "## Null rate of
      the original" instead of standing as its own pass condition →
      HISTORY.md#ac-gate1-red-on-purpose-rewritten-ratchet-2026-09-24

      ⚠ 2026-10-02: was dated 2026-09-24 with "about one run in seven" (13/96 lagged
      then); E1, E2 (2026-09-27) and F-c (2026-09-28) changed the criterion's code
      since, and the null rate read 15 of 197 (root `ACCEPTANCE.md`).

      ⚠ 2026-10-02, after B2a: was 15 of 197 dated 2026-09-28, now 16 of 197:
      independent leave-one-out replica 7 of inpt gains `fqdokkarm(35,:)[25]` →
      HISTORY.md#b2a-rate-figures-2026-10-02

      ⚠ 2026-10-02, after B2c: was 16 of 197, now 15 of its 197 runs: HPEPA3
      independent replica 14 loses `fqkarm[74]` and `fqkarm_cor[74]` →
      HISTORY.md#b2c-rate-figures-2026-10-02
- [x] `BitSnapshot.Verify` fails when a value's bits differ from the approved snapshot,
      and passes when they match (proof of non-degeneracy): 2026-09-17,
      `tests/Random.Tests`'s seed and `X9` mutations both turned every
      `DrawStreamTests.FirstThousandDrawsMatchTheApprovedBitSnapshot` case red through
      `BitSnapshot.Verify` itself, then green again on revert.
- [x] `RepositoryPaths.Root` resolves to the directory holding `AGENTS.md` from any
      caller under the tree: 2026-09-17, exercised by every `tests/Random.Tests` case
      that reads `legacy/PropStructV3.zip` through `RepositoryPaths.Resolve`, and by
      `BitSnapshot`'s own approved-file resolution; all 427 fast cases pass from this
      node's own working directory and from the solution root alike.
- [x] `PythonScript.RequireSuccess` fails the calling test, naming script, arguments,
      exit code and both streams, when the script exits non-zero, and `Run` hands
      `Random.Tests` the same exit code and streams: 2026-10-02, seen red and green
      again in four callers: a byte appended to the generated file of
      `SetupPlaneClassificationGeneratorTests` and of `PrintExpressionGeneratorTests`,
      and a syntax error at the top of `tests/Fixtures/generate.py` (`LegacyArchiveTests`,
      `PythonSeedArithmeticCrossCheckTests`).

- [x] The two-sample bias of lagged against independent replicas is computed for every
      printed quantity of every reference formulation, and a test asserts the claims of
      root `BOOT.md` "Known bias of the original's seeds": the quantities it calls
      biased differ beyond the family-wise threshold, the mass-type quantities it calls
      trustworthy do not. (2026-09-17, `tests/Harness.Tests`, `TwoSampleBiasTests`,
      20 cases over the five formulations.)

      ⚠ 2026-09-17: was the claim as first written, now measured to half hold and half
      not (the "biased" list holds except HMX; the "trustworthy" list holds only for
      `Dok43all(1)`) — root `BOOT.md` corrected the same date to match →
      HISTORY.md#ac-two-sample-bias-half-holds

      ⚠ 2026-09-17, follow-up: was `TwoSampleBiasTests` called "a long test", now
      corrected to "a test" (measured: ~2s for its 20 cases, already in the fast set) →
      HISTORY.md#ac-two-sample-bias-not-a-long-test
- [x] The GSV=3 replica set stays available as a reported cross-check, with no pass
      condition: (2026-09-17, `tests/Harness.Tests`,
      `StatisticalCriterionTests.EachFormulationsOwnGsv2ReferenceAgainstGsv3ReplicasIsAReportedCrossCheckOnly`,
      all five formulations run end to end; HPEPA3 486/2016 cells fail, inpt 385/2539,
      P33 384/1521, PSAN02n 520/2065, HMX 999/4349 — expected and unasserted, root
      BOOT.md's "Known bias of the original's seeds").
- [x] Every new comparison rule of `## Invariants` (canonical category axis, count-like
      floor, print-resolution guard) is proven non-degenerate by a recorded mutation
      (AGENTS.md §13), alongside the four already recorded in `tests/Harness.Tests/BOOT.md`:
      2026-09-17, three mutations applied and reverted, each turning
      `EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas(formulation:
      "HMX")` red and back to green (`tests/Harness.Tests/ACCEPTANCE.md`, ## Acceptance
      criteria, has the exact mutation and the failing cell of each).

- [x] Blind calibration: the 96-run lagged leave-one-out loop (every lagged replica of
      every formulation, compared as the candidate against the other `R - 1`) is the
      same loop "## Null rate of the original" in `BOOT.md` reports at 9/96 failing
      runs. (2026-09-18, evidence updated 2026-10-02: `tests/Harness.Tests`,
      `Gate2BlindCalibrationLaggedLeaveOneOutNullRateNumeratorMatchesTheRecordedSet`,
      `NullRateCalibration`.)

      ⚠ 2026-09-26 (AGENTS.md §8): was the per-cell named-exception mechanism this
      criterion originally described, now "Gate 2, redefined" over the same 96 runs
      → HISTORY.md#ac-blind-calibration-per-cell-exceptions-deleted

      ⚠ 2026-10-02: was "13/96" and evidence of 2026-09-24, now 9/96 under F-c's code
      (2026-09-28), as `OriginalNullRateLaggedLeaveOneOutMatchesGate2sOwnFigure`
      asserts.
- [x] The tail of the category family is covered by the statistics of
      `HISTORY.md#tail-coverage-2026-09-18`, and the sensitivity of each is measured and
      recorded as a number, not claimed. (2026-09-18, `tests/Harness.Tests`,
      `TailCoverageTests.Step4aTailRowMeanSensitivityRollAndMixSweep`,
      `Step4bAdaptiveLastBoundarySensitivityStretchSweep`,
      `Step4cAdaptiveRowCountAndLastBoundarySensitivityTruncationSweep`, HMX:
      `rho* = 0.1%` [tail row mean], `delta*` not caught at 1/2/5/10% [adaptive index
      matched], `k* = 14` of 31 adaptive rows [truncation] — HISTORY.md#tail-coverage-2026-09-18
      has the reasoning for each figure.
- [x] Every comparison rule this node exposes has a live, non-reimplemented oracle
      proving its own pass/fail boundary is real on both sides, on HPEPA3 and HMX, and
      any cell class where the boundary is unreachable at any finite magnitude is
      reported, not hidden. (2026-09-19, `tests/Harness.Tests`, `OracleMutationTests`,
      20 cases, all boundary proofs across the eight rules → HISTORY.md#the-candidate-is-never-its-own-witness
      has the findings and the non-degeneracy proof.)

- [x] 2026-10-03, reformulated 2026-10-02 (AGENTS.md §6, as the null-rate criterion
      above was): the quantum fix (`HISTORY.md#the-candidate-is-never-its-own-witness`)
      is judged on the runs of its gates 1 and 2 as a rate, not by their passing. The
      five archived references and the 96 lagged leave-one-out runs are pooled into
      the original's null rate, 15 of 197, against which the port's rate shows no
      excess at `alpha` (p = 0.103) and `Binary64`'s does
      (`tests/Harness.Tests/RateCriterionTests`, the run the rate criterion below
      cites); its gate 3 is the oracle criterion above.

      ⚠ 2026-10-02: was unticked, "passes gates 1 and 2 … Gate 1 is met", false since
      2026-09-20 (HMX `fqdokkarm(31,:)[7]`) and unmeetable since the rate decision of
      2026-09-24; now judged as a rate → HISTORY.md#ac-quantum-fix-as-a-rate-2026-10-02

      ⚠ 2026-10-02, after B2c: was 16 of 197 and p = 0.100, now 15 of 197 and p = 0.103
      → HISTORY.md#b2c-rate-figures-2026-10-02

      ⚠ 2026-10-03: was dated 2026-10-02, the table before the per-cycle plane followed
      the executable's listing; the 160 `Original` runs are regenerated and 15 of 197,
      17 of 160 and p = 0.103 read the same (`RateCriterionTests`, `src/Statistics/
      ACCEPTANCE.md`, A6).
- [ ] The calibration curve's `Student` rows (non-degenerate, eligible cells; 0.05 and
      0.01 per formulation, 0.001 pooled) lie inside the binomial band of their level.
      Not met on 2 of its 11 rows (2026-10-02, after B2a, `CalibrationCurveTests`, its
      ratchet `KnownCalibrationViolations`): PSAN02n at 0.05, low (770 against
      [788, 978]), and the pooled 0.001 row, high (213 against [111, 191]; `D` = 4.10),
      where P33 replica 2 fails 20 cells and replica 14 fails 19 through F-b's fixed cut
      (its prefix is 7, the reference's 6). HMX at 0.05 is inside, at its lower edge
      (2362 against [2360, 2681]). Cells are taken as independent; failures cluster by
      run (the test prints each row's dispersion `D`), and no band may be widened by a
      dispersion estimated from the failures it judges
      (`tests/Harness.Tests/BOOT.md`, ## Invariants).

      ⚠ 2026-10-02: was "3 of its 11 rows" with PSAN02n 603 against [618, 787], HMX
      2100 against [2110, 2414] and the pooled row 169 against [88, 160], before B2a,
      now the rows above → HISTORY.md#ac-calibration-rows-after-b2a-2026-10-02
- [ ] The calibration curve's `Count` rows lie inside the binomial band of their
      attained level. Not met on 3 of its 11 rows (2026-10-02, after B2c,
      `CalibrationCurveTests`): HMX at 0.05 and 0.01, high (70 against [20, 60]; 18
      against [0, 14]), and the pooled 0.001 row, high (13 against [0, 6]). With the
      region's edge passing (B2c), an excess is the predictive's own coverage: sparse
      `fqkarm`/`fqkarm_cor` cells and HMX's `fqdokkarm` rows (C-sparse, open;
      B2b deferred, `HISTORY.md#count-region-edge-2026-10-02`). HPEPA3 at 0.05 and
      0.01 is inside (33 against [14, 49]; 12 against [0, 14]). Population 2c: 6825
      cells, 1 failure over the three levels. Neither curve is an input of the rate
      criterion below.

      ⚠ 2026-10-02: was "Not met on 4 of its 11 rows": `inpt` at 0.05 and 0.01 and
      PSAN02n at 0.05, low, on dense cells whose step was inferred several times too
      coarse (C-dense), and the pooled 0.001 row, high (19 against [0, 11]); now the
      rows above → HISTORY.md#ac-calibration-rows-after-b2a-2026-10-02

      ⚠ 2026-10-02, after B2c: was "Not met on 5 of its 11 rows", HPEPA3 and HMX at
      0.05 and 0.01 "not diagnosed"; HPEPA3's were the region's edge failing →
      HISTORY.md#count-region-edge-2026-10-02

      2026-10-02, B2b step 1: region exits per family beside the replicas' factor, a
      report → `tests/Harness.Tests/HISTORY.md#count-coverage-report-2026-10-02`
- [x] 2026-10-03: the port's own failure rate under `PrecisionKind.Original`, pooled
      over sixteen seeds per formulation and layout, is no higher than the original's
      own null rate, by a one-sided exact test at the tree's `alpha`; the same test on
      `PrecisionKind.Binary64` rejects on HPEPA3 and HMX (`tests/Harness.Tests`,
      `RateCriterionTests`: `PortRateOriginalPrecisionIsNoHigherThanTheOriginalsNullRatePooled`
      p=0.103; `PortRateBinary64PrecisionIsSignificantlyHigherThanTheOriginalsNullRateOnHpepa3AndHmx`
      p=1.63e-36 on both; `RateTableTiesToTheCurrentCriterionAndSnapshot`;
      `OriginalNullRateLaggedLeaveOneOutMatchesGate2sOwnFigure`, a positive control
      against the already-known 9/96 — `BOOT.md`, "##
      Null rate of the original, and the port's rate against it", has every figure).

  ⚠ 2026-09-27: the `13/96` this criterion's own evidence cited moved to `10/96` when E1
  landed, then to `9/96` when E2 landed the same day; the figure was the correct
  evidence for the criterion's own tick date and is not re-verified here, only pointed
  at its current value (AGENTS.md §6).

  ⚠ 2026-10-02: was dated 2026-09-24 with p=0.1242, p=1.67e-15 and 13/96, the figures
  the note above kept; now the 2026-10-01 table's, the root's date (AGENTS.md §6).

  ⚠ 2026-10-02, after B2a: was dated 2026-10-01 with p=0.062 and p=1.63e-36, now
  p=0.100 and p=1.29e-35 on the regenerated table: the original's null rate moved from
  15 to 16 of 197, the port's 18 of 160 did not →
  HISTORY.md#b2a-rate-figures-2026-10-02

  ⚠ 2026-10-02, after B2c: was p=0.100 and p=1.29e-35, now p=0.103 and p=1.63e-36 on
  the regenerated table: the original's null rate moved from 16 to 15 of 197 and the
  port's 18 to 17 of 160 → HISTORY.md#b2c-rate-figures-2026-10-02

  ⚠ 2026-10-03: was dated 2026-10-02; the 160 `Original` runs, the table and the seed-0
  snapshot regenerated after the per-cycle plane followed the executable's listing:
  p=0.103 and p=1.63e-36 on both gated formulations unchanged, 147 of the 160 runs
  differ in bytes, none in its failing cells, the 160 `Binary64` runs byte-identical
  (`src/Statistics/ACCEPTANCE.md`, A6).
- [x] 2026-10-03: the mass bracket fails no genuine run of the original — 0 of 197
      (was 5) and 0 of the port's own 160 (was 2) — by bounding the volume quantum with
      its feasible interval, derived from print half-units and the histogram's own bin
      edges, rather than a single point estimate (`tests/Harness.Tests/MassBracketTests`;
      `HISTORY.md#e1-mass-bracket-feasible-interval`).

      ⚠ 2026-10-02: was dated 2026-09-27, re-verified green after B2a (a042b08) changed
        the criterion's code: `MassBracketTests`, 11 cases, 0 of 197 and 0 of the port's
        160.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): `MassBracketTests`, 11 cases, 0 of 197 and 0 of the port's 160.

      ⚠ 2026-10-03: was dated 2026-10-02; "0 of the port's own 160" rests on runs
        regenerated since, after the per-cycle plane followed the executable's listing.
        Evidence: no failing name moved in any of the 160 (`FailingNames` of
        `tests/Fixtures/rate-table.json` identical on all 320 runs before and after), and
        a Mass-rule failure would show there as a name.
- [x] 2026-10-02: a row-count excursion of the adaptive axis fails at most one cell,
      `AdaptiveRowCount`, never every row past it a source lacks (E2, reversing
      decision III's union rule for a common range): the shortest replica of every
      formulation/layout, as its own pool's candidate, fails at most
      `AdaptiveRowCount[0]` (`tests/Harness.Tests/TailCoverageTests`,
      `ShortestReplicaAsCandidateFailsAtMostAdaptiveRowCount`, 10/10 pairs; HMX lagged
      replica 3 and independent replica 11, six and three cells under the union rule,
      now fail on none); `OracleMutationTests`'s own boundary proofs confirm the range
      still scores real boundaries inside it, never past it (`HISTORY.md#e2-adaptive-common-range`).

      ⚠ 2026-10-02: was dated 2026-09-27, re-verified green after B2a (a042b08) changed
        the criterion's code:
        `TailCoverageTests.ShortestReplicaAsCandidateFailsAtMostAdaptiveRowCount`, 10/10
        pairs, and `OracleMutationTests`, 30 cases.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): the same 10/10 pairs and 30 cases.
- [x] 2026-10-03: `PrintResolution.ExponentOf` no longer reads an exact power of ten one
      decade low (F-c): checked against the printed exponent of 1,129,554 non-zero `E`
      tokens over every criterion fixture file, `rate-runs` included, zero disagree
      (was 4,342, every one `0.100E+-xx`); moves exactly one cell, P33
      `Independent`/`Original` seed 13's `fqkarm[62]` (`tests/Harness.Tests/PrintResolutionTests`;
      `HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`).

      ⚠ 2026-10-02: was dated 2026-09-28, re-verified green after B2a (a042b08) changed
        the criterion's code: `PrintResolutionTests`, 11 cases, among them
        `ExponentOfMatchesTheTokensOwnExponentForEveryNonZeroFixtureToken` over every
        fixture token.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): `PrintResolutionTests`, 11 cases; the P33 seed 13 test now asserts
        that `fqkarm[63]` passes too.

      ⚠ 2026-10-03: was dated 2026-10-02, before `rate-runs` was regenerated (the plane
        followed the listing): the same census over the regenerated files, 1,129,554
        non-zero `E` tokens (as a script counting with the test's own rule finds), zero
        disagree (`PrintResolutionTests`, 11 cases green).
- [x] 2026-10-03: `CompareSets` finds exactly the declared cells on `rate-runs`, gated
      on `Lagged` (all five) and `Independent` (four of five; PSAN02n reported only,
      `tests/Harness.Tests/BOOT.md` footnote `[8]`); both negative controls empty on all
      twenty (formulation, layout, precision) triples; `Binary64` finds an undeclared
      `fmdok` cell, gated on HPEPA3/HMX (`tests/Harness.Tests/SetCriterionTests`); the
      random-split re-measurement is `HISTORY.md#set-comparison-design-2026-09-27`.

      ⚠ 2026-10-02: was dated 2026-09-27, re-verified green after B2a (a042b08) changed
        the criterion's code: `SetCriterionTests`, 52 cases.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): `SetCriterionTests`, 52 cases.

      ⚠ 2026-10-03: was dated 2026-10-02, before `rate-runs` was regenerated: green on
        the regenerated runs, `SetCriterionTests`, 52 cases, declared cells unchanged.

- [x] 2026-10-04 — `ResultsMTimeLine.Remove`, `CudaRequirement.FailIfRequired` and
      `CudaRefusalBranches.Scan` are public (stage S4): `ResultsMTimeLineTests`, 302
      cases, equal `Remove` to an independent line filter (the copies it replaced are
      gone) on all 293 references and replicas of the fixtures, each with exactly one
      line cut, and red on a file without the marker; with the cut's end moved by one
      byte, 295 of the 302 fail. `CudaRequirementTests`, 9 cases: `FailIfRequired`
      throws, naming the reason, with the variable `1`, and returns with it unset, `0`,
      empty, `true` or `11`; with the condition changed to "set", 4 of the 9 fail.
      `CudaRefusalBranchesTests`, 9 cases, red on a refused block without the call, on
      the call after the block, on an `is null` with no `else`, right on each guarded
      shape. `Cli.Tests`, `Simulation.Tests`, `RateTableTool` and `check_packages.py`
      use `Remove`, no private copy remains.
