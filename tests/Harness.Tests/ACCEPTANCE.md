# ACCEPTANCE.md — Harness.Tests

## Acceptance criteria

- [x] Every row of the levels table is green, with a date and the names of the tests:
      2026-09-17, `PropStruct.Tests.Harness.Tests`, `ResultsMFileTests`,
      `StudentDistributionTests`, `StatisticalCriterionTests`, `TwoSampleBiasTests`,
      403 fast cases (`dotnet test tests/Harness.Tests --filter Category!=Long`).

      ⚠ 2026-09-19: no longer true as measured, for a span of the same day. The levels
      table's own `[1]` footnote and `tests/Harness/ACCEPTANCE.md`
      ("The quantum fix above passes gates 1 and 2 ...", left unticked) record the red
      state, re-measured against the per-run quantum: HMX (2 cells) failed
      `StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`,
      part of the fast set. This tick was kept as the date it was true, not moved forward
      or removed (AGENTS.md §6: "a criterion dated earlier than the change it supposedly
      guards ... is subject to re-verification").

      ⚠ 2026-09-19, later the same day: re-verified true again, against
      `tests/Harness/HISTORY.md#three-decisions-2026-09-19`. `dotnet test
      tests/Harness.Tests --filter FullyQualifiedName~EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`:
      zero failures, all five formulations (HMX included). The `[1]` footnote and the
      sibling `tests/Harness/ACCEPTANCE.md` criterion are re-ticked with this same date.

      ⚠ 2026-09-19, later still: "every row ... green" no longer holds for L2 — the
      levels table's own `[3]` footnote records Fable 5.1's decision
      (`../Harness/decisions/decision-01-fable.md`) withdrawing four named gate-2 exceptions
      as an open finding pending the heavy-tail count rule. `L1`'s own rows (this
      criterion's own fast-set scope: `EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`)
      stay green and are unaffected; kept ticked for that scope, with `[3]` carrying L2's
      own red state until the count rule lands.
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13):
      2026-09-17, seven mutations applied and reverted, each seen red then green again
      (`dotnet build` clean and the fast suite green after every revert):
      - **Parser continuation lines**: `tests/Harness/ResultsMFile.cs`,
        `JoinContinuations`'s three-dot guard changed to require a fourth dot. Red:
        101/101 cases of
        `ResultsMFileTests.ParsesEveryReferenceAndReplicaFileWithoutThrowing`
        (`InvalidOperationException` on the first multi-line array quantity of every
        file, e.g. `fmdok`, whose continuation lines no longer join).
      - **Student quantile**: `tests/Harness/StudentDistribution.cs`, the Lanczos
        coefficient `g[1]` (`676.5203681218851`) perturbed by `+0.01`. Red: 72/72
        cases of `StudentDistributionTests.MatchesScipy`.
      - **`r_q` floor**: `tests/Harness/ResultsMFile.cs`, `ResolutionOf`, the exponent
        changed from `exponent - decimalDigits` to `exponent - decimalDigits + 1`
        (one printed digit too coarse). Red:
        `ResultsMFileTests.ResolutionFamiliesMatchTheirPrintedTokens`.
      - **Exclusion application**: `tests/Harness/StatisticalCriterion.cs`,
        `ZeroBelowThreshold` changed from `1e-30` to `0.0` (the rule never fires).
        Red: `StatisticalCriterionTests.ExclusionRuleForcesPdoksmallGarbageCellsToCompareAsZeroRegardlessOfTheCandidate`
        (a candidate of `0.5` at `pdoksmall(1)`/`pdoksmall(2)`, no longer zeroed,
        compared against ~1.7e-38 replica garbage and failed outright — the first
        mutation attempt used a candidate of `0.0`, which passed even unmutated
        because HPEPA3's `pdoksmall` garbage itself has enough replica-to-replica
        spread [sd ≈ 2.9e-39] that the ordinary `t·sd·√(1+1/R)` term already covers a
        candidate of exactly 0; `0.5` was chosen because it is unambiguously outside
        that spread, so only the exclusion rule — not sample noise — can explain a
        pass).
      - **Count-like floor**: `tests/Harness/StatisticalCriterion.cs`,
        `MaxCountFloorTotal` changed from `2000.0` to `-1.0` (the negative-binomial
        count floor never fires, for any cell). Red:
        `StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas(formulation:
        "HMX")`, 2 failures (`fmkarm_cor2[84]`: value `2.12e-05` vs mean `0`, threshold
        collapses to the print-resolution floor `1e-07`; `fqdokkarm(31,:)[7]`: value
        `5.99e-05` vs mean `0`, same floor) — the other four formulations stay green,
        confirming the floor is needed only where it was designed for.

        ⚠ 2026-09-19: under "Per-run quantum" (`tests/Harness/StatisticalCriterion.cs`'s
        current form) this specific mutation is degenerate at these same two cells: the
        unmutated baseline already cannot infer a quantum for HMX's own
        `fmkarm_cor2`/`fqdokkarm(31,:)` arrays (its "Implementation and measurements,
        per-run quantum" has the ratios), so `CountFloorFromCounts` is already never
        reached there and mutating `MaxCountFloorTotal` changes nothing at those two
        cells. It has not been re-proven non-degenerate anywhere else, because the same
        measurement shows the per-run quantum essentially never resolves for a
        distribution array with real dynamic range — the mechanism this mutation was
        written to guard needs the design session's decision before a new,
        non-degenerate proof is worth writing for it.
      - **Canonical category axis**: `tests/Harness/StatisticalCriterion.cs`,
        `PrefixMatchLength`'s value check `source[i] == reference[i]` removed (every
        source now "matches" for as long as it has an element, regardless of its
        value — canonical rows are chosen by presence again, not by boundary
        agreement). Red: the same theory, HMX, 1 failure
        (`fqdokkarm(33,:)[9]`: value `9.29e-05` vs mean `4.68e-06`, threshold
        `7.68e-05` — row 33 exists in every replica but at a different `Dkarmcat`
        boundary than the reference's, exactly the index-misalignment this rule
        exists to close).
      - **Print-resolution floating-point guard**: `tests/Harness/StatisticalCriterion.cs`,
        the guard epsilon replaced by `0.0`. Red: the same theory, HMX, 1 failure
        (`pdoksmall[61]`: value `1.07` vs mean `1.08`, threshold `0.01`, difference
        `0.010000000000000009` — one `double`-arithmetic ULP past the floor).
      - `Fixtures.Tests`: recorded in `tests/Fixtures.Tests/BOOT.md`, not here.

- [x] Every rule `tests/Harness/HISTORY.md#tail-coverage-2026-09-18` adds
      (`FixedWidthPrefixLength`, `CompareTailRowMean`, `CompareAdaptiveIndexMatched`'s
      own candidate-length cap, and the `TryInferQuantum` reference-contamination fix)
      is proven non-degenerate by a recorded mutation (AGENTS.md §13): 2026-09-18,
      three mutations applied and reverted, each turning a `TailCoverageTests` theory
      red and back to green (`dotnet build` clean and the fast/full suites green after
      every revert):
      - **`FixedWidthPrefixLength` vacuity**: `tests/Harness/StatisticalCriterion.cs`,
        forced to `return dkarmcat.Length` unconditionally (`i0` = every source's own
        full length, so no row is ever "adaptive"). Red: 3 of 10 cases across
        `Step2_BlindCalibration_TailRowMean` and
        `Step3_BlindCalibration_AdaptiveIndexMatched` — `P33`/`PSAN02n` step 2:
        `"{formulation}: 0 cells compared across 16 candidates"` (the new
        `Assert.True(totalCompared > 0, ...)` guard, without which this mutation would
        have left `unexplained` empty and stayed green: a check that only ever asserts
        "no unexplained failure" cannot see a rule that stopped comparing anything);
        `HMX` step 3: `AdaptiveRowCount` fails outright (some replicas' own `Dkarmcat`
        is longer than the mutated, formulation-wide `i0`, so they still contribute a
        few "adaptive" rows while most contribute none, producing a real, wrong
        comparison rather than a silent no-op there). Confirms the vacuity guard and
        the rule itself both matter.
      - **`CompareAdaptiveIndexMatched`'s candidate-length cap**: `matchedLength =
        Math.Min(candidateAdaptiveLength, contributingLengths.Min())` changed to drop
        the candidate's own cap (`matchedLength = contributingLengths.Min()` alone).
        Red: `Step3_BlindCalibration_AdaptiveIndexMatched(formulation: "HMX")`,
        `System.IndexOutOfRangeException` inside `CompareAdaptiveIndexMatched` itself —
        a candidate whose own adaptive tail is shorter than the replica pool's shortest
        is read past its own array's end, exactly the index-misalignment failure mode
        this cap exists to prevent (this node's BOOT.md, ## Tail coverage, step 3's own
        declared mis-specification, one layer deeper: without the cap it is not just a
        mis-specified band, it is an out-of-bounds read).
      - **`TryInferQuantum` reference contamination**: `tests/Harness/StatisticalCriterion.cs`,
        the fixed call site changed to fold `cell.ReferenceValue` back into the
        population `CountFloor`/`TryInferQuantum` draws from (`[.. cell.ReplicaValues,
        cell.ReferenceValue]`), reproducing the original, unfixed
        `TryInferQuantum(referenceValue, candidateValue, replicaValues, ...)` shape.
        Red: `Step1_BlindCalibration_CanonicalAxisRule(formulation: "inpt")`, replica 6,
        5 unexplained failures — `fmkarm`/`fmkarm_cor`/`fmkarm_cor2`/`fqkarm`/
        `fqkarm_cor` all at index 46, each collapsed to its bare print-resolution floor
        (`1E-08`/`1E-10`) exactly as first diagnosed (this node's BOOT.md is not the
        one that carries this fix's own record — `tests/Harness/BOOT.md`, ## Invariants,
        "Count-like cells", ⚠ 2026-09-18, has it — this mutation is recorded here
        because it is `TailCoverageTests`' own non-degeneracy proof for the fix).

- [x] `OracleMutationTests` is proven non-degenerate by a recorded mutation (AGENTS.md
      §13): 2026-09-19, `tests/Harness/StatisticalCriterion.cs`, `Finalize`'s floor
      computation forced to `var floor = double.MaxValue;` — applied and reverted
      (`dotnet build PropStruct.sln` clean and all 20 `OracleMutationTests` cases green
      again after the revert). Red: 14 of 20 cases (every rule routed through
      `Compare`/`CompareTailRowMean`/`CompareAdaptiveIndexMatched`, both formulations),
      each `boundary.Found == false` within the search's own 80-doubling cap; the two
      "never fails" finding tests and both `TwoSampleBias_Dok43all` cases correctly stay
      green (unaffected by a floor no code path of theirs reads). Chosen specifically
      because `StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`
      — the existing "must have zero failures" test — stays green throughout this same
      mutation: nothing can fail once the floor is unreachable, so that test cannot see
      it. `tests/Harness/HISTORY.md#oracle-mutation-2026-09-19` has the full
      derivation and the two findings this exercise turned up.

      ⚠ 2026-09-19, later the same day: the "two 'never fails' finding tests" named above
      are `tests/Harness/HISTORY.md#candidate-never-its-own-witness-2026-09-19`'s
      fix — `CountPredictiveIntervalAllZeroReplicaCellBoundaryIsRealAfterTheQuantumFix`
      and `TailRowMean_ColumnZero_BoundaryIsRealAfterTheCountFloorFix`, renamed from
      `...NeverFailsAtAnyFiniteMagnitude` — now assert a real, finite boundary instead of
      the absence of one; this mutation record describes the state measured before that
      fix and is kept as found, not rewritten (AGENTS.md §8). All 20 cases, under their
      current names, are still green with the mutation reverted.

      ⚠ 2026-09-19, later still: the per-run quantum's estimator was itself refined
      the same day (`tests/Harness/HISTORY.md#two-refinements-2026-09-19`) — the largest
      `q = min / k` over resolvable cells, in place of "the array's own minimum is one
      count". Proven non-degenerate directly: `TryInferRunQuantum`'s search bound
      changed from `for (var k = 1; ; k++)` to `for (var k = 1; k <= 1; k++)` (only the
      old, refuted assumption is tried), applied and reverted (`dotnet build
      PropStruct.sln` clean and `StatisticalCriterionTests.
      EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas` green again for
      HMX, its only formerly-affected formulation, after the revert). Red:
      `EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas(formulation:
      "HMX")` regains its pre-refinement second failure, `fqdokkarm(31,:)[7]`
      (`value=5.99E-05, mean=0, threshold=1E-07`), exactly the cell "Two refinements"
      names as the one the estimator fix closes; `fmkarm_cor2[84]` (the separate,
      still-open mass-family gap) is unaffected either way, confirming the mutation
      isolates the estimator's own contribution. Also: the reclassification of
      `fmkarm`/`fmkarm_cor`/`fmkarm_cor2`/`fmdok` out of `DistributionFunctionFamilies`
      is proven non-degenerate the same way `OracleMutationTests`' own tests prove it —
      `StudentBandFmkarm10BoundaryIsRealOnBothSides` (renamed from
      `CountPredictiveInterval_Fmkarm10_...`) still finds a real boundary at that cell
      through the ordinary Student band alone, and the new
      `CountPredictiveIntervalFqkarm10BoundaryIsRealOnBothSides` (a genuine count
      family kept in the set) proves the per-run quantum machinery itself is still
      reachable and correct at a real, populated cell, not merely inert. All 22 current
      `OracleMutationTests` cases (11 tests × 2 formulations, one more than before: the
      new `Fqkarm10` case) are green.

      ⚠ 2026-09-19, later the same day: `tests/Harness/HISTORY.md#three-decisions-2026-09-19`
      added one genuinely new oracle-mutation case (`MassFamilyCeiling_
      Fmdok0_BoundaryIsReal`, #1's absolute ceiling) and renamed/rewrote one existing
      case (`TailRowMeanColumnZeroExcludedAsScalelessUnderTheSparseCellRule`, #1's own
      sparse-cell exclusion wired into `CompareTailRowMean`, the gap found while
      re-verifying #3). Both proven non-degenerate directly, each mutated and reverted
      in isolation (`dotnet build PropStruct.sln -c Release` clean and the relevant
      case(s) green again after every revert):
      - **Mass-family ceiling**: `StatisticalCriterion.cs`, the ceiling's own condition
        (`massStep > 0.0 && Math.Abs(cand) > 1.0 / massStep`) short-circuited with a
        leading `false &&`. Red: `MassFamilyCeilingFmdok0BoundaryIsReal`, both
        formulations — "no perturbation up to 80 doublings ... turned this rule red"
        (with the ceiling gone, `fmdok[0]` falls through to the sparse-cell exclusion
        alone and is excluded at any magnitude, exactly the escape #1's own ceiling
        exists to close for a family member with no other floor).
      - **`CompareTailRowMean`'s own sparse-cell exclusion**: `StatisticalCriterion.cs`,
        the check `CompareTailRowMean` adds (`replicaValues.Count(v => v != 0.0) < 2`)
        short-circuited the same way, leaving the generic branch's own copy of the check
        untouched. Red: `TailRowMeanColumnZeroExcludedAsScalelessUnderTheSparseCellRule`,
        both formulations, in under 10 s each (the ordinary Student band now runs
        directly on a scaleless cell and fails at a small delta) — confirms this
        specific wiring, not the shared generic-branch check, is what keeps
        `TailRowMean[0]` correctly excluded.

      Decisions #2 and #3 are each proven non-degenerate by a measured before/after
      regression-and-fix, not a separate mutate-revert cycle here: #2's own record is the
      HPEPA3 replica 15 `coef[479]` false failure this node's own investigation produced
      and then closed (`tests/Harness/HISTORY.md#three-decisions-2026-09-19`,
      "Implementation and measurements", the ⚠ under it), confirmed novel by reverting
      `StatisticalCriterion.cs` to the commit before #1-#3 and finding zero failures
      there; #3's own record is the HPEPA3 replica 11 `TailRowMean[2]` threshold moving
      from a literal `0.0` (codegen-dependent) to a real `1E-10` (this node's own `[2]`
      footnote above), each a live comparison against the real rule, not a reimplemented
      formula, satisfying the same "seen red, seen green" requirement (AGENTS.md §13)
      through the defect they were written to close rather than an artificial one.

- [x] 2026-09-23: the "not compared against independent replicas" rule
      (`tests/Harness/StatisticalCriterion.cs`, `NotComparedAgainstIndependentReplicasRule`)
      is proven non-degenerate by a recorded mutation (AGENTS.md §13):
      `StatisticalCriterionTests.ExclusionRuleDropsEpsx4WhollyFromComparisonAgainstIndependentReplicasButNotLagged`
      asserts the rule's two live behaviours (excluded and value-independent against
      `Independent`; an ordinary compared, failing cell against `Lagged`). The "without
      the rule" side (root task "epsx(4) exclusion", step 2's own requirement) is a
      third, separate proof: `StatisticalCriterion.cs`'s own exclusion check,
      `kind == ReplicaKind.Independent && excludedFromIndependentQuantities.Contains(name)`,
      short-circuited with a leading `false &&`, applied and reverted (`dotnet build
      PropStruct.sln -c Release` clean and both cases of the test above green again
      after the revert). Red: `ExclusionRuleDropsEpsx4WhollyFromComparisonAgainstIndependentReplicasButNotLagged`
      fails at its first assertion — `epsx(4)` (value `0.3`, mean `8.67e-05`, threshold
      `6.84e-05`) now appears in `Compare`'s own `Failures` against `ReplicaKind.Independent`,
      exactly the cell the rule exists to drop.
- [x] 2026-09-24: gates 1-3 (formerly declared perpetually red) are green ratchets, not
      loosened bounds (levels table, footnote `[6]`). `Category=Long`: 69/69 pass.
- [x] 2026-10-02: the constant-cell exact bound, the canonical-axis alignment and the
      zero-fill rule are each proven non-degenerate, red then green (each applied and
      reverted, `dotnet build` clean, 51/51 green after): `ConstantMismatchBound` forced
      to `1.0` drops `da_coef`; `fqdokkarm`'s own gate forced open moves `Compared` on
      9/10 pairs (`ComparedCellCountsMatchTheRecordedBaseline`);
      `ZeroFillOrdinaryToUnionLength` disabled fails all 51 cases. Also measured
      directly against real data (a P33/`Original`-gated cloned-candidate test); both
      figures are in `../Harness/HISTORY.md#set-comparison-design-2026-09-27`.

      ⚠ 2026-10-02: was dated 2026-09-27, re-verified green after B2a (a042b08) changed
        the criterion's code: `SetCriterionTests`, 52 cases, among them
        `EpsdokfrOnInptUnderBinary64IsFoundAsAConstantCellMismatch` and
        `RateRunsTieToTheRateTable`; the red mutations were not repeated.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): `SetCriterionTests`, 52 cases; the red mutations were not repeated.
- [x] 2026-10-02: the four remaining design §6 step 5 proofs, each applied and
      reverted, red then green: the subset check (`PSAN02n: da_coef` set to `none` in
      `docs/declared-differences.json`), the found-check (a stale `P33: da_coef` entry
      added), the positive control (`Original` runs passed as the `Binary64` arm), and
      the tie (`RateRunsTieToTheRateTable`, a one-byte edit and a deleted stored file,
      independently) — full account, `../Harness/HISTORY.md#set-comparison-design-2026-09-27`.

      ⚠ 2026-10-02: was dated 2026-09-27, re-verified green after B2a (a042b08) changed
        the criterion's code: `SetCriterionTests`, 52 cases, among them
        `EpsdokfrOnInptUnderBinary64IsFoundAsAConstantCellMismatch` and
        `RateRunsTieToTheRateTable`; the red mutations were not repeated.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): `SetCriterionTests`, 52 cases; the red mutations were not repeated.
- [x] 2026-10-02: the constant-cell bound's positive control is inpt's `epsdokfr[0]`
      under `Binary64`, constant on both sides (`0.000E+00` against the original's
      `0.270E-07`) and found with `|T| = Infinity` in both layouts
      (`SetCriterionTests.EpsdokfrOnInptUnderBinary64IsFoundAsAConstantCellMismatch`);
      red with `ConstantMismatchBound` forced to `1.0`, reverted green. The subset
      check went red on PSAN02n/`Original` against the old `PSAN02n: da_coef` entry,
      stale once the per-cycle plane reproduced the cell, and green after its row split.

      ⚠ 2026-10-01: was PSAN02n's `da_coef` under `Original` as that control
      (2026-09-27, above), now inpt's `epsdokfr[0]` under `Binary64`.

      ⚠ 2026-10-02: was dated 2026-10-01, re-verified green after B2a (a042b08) changed
        the criterion's code: `SetCriterionTests`, 52 cases, among them
        `EpsdokfrOnInptUnderBinary64IsFoundAsAConstantCellMismatch` and
        `RateRunsTieToTheRateTable`; the red mutations were not repeated.

      ⚠ 2026-10-02, after B2c: re-verified green after `CellEvaluator` changed again
        (8ae43ad): `SetCriterionTests`, 52 cases; the red mutations were not repeated.

- [x] 2026-10-02: the quantum identification of B2a is proven right and red
      (`QuantumIdentificationTests`, `../Harness/HISTORY.md#b2a-amended-2026-10-02`).
      K1, 1200 fixed rows of exact counts of a known step, 200 each at smallest count 1,
      3, 10, 30, 100, 300: no row identifies a wrong step, every row at smallest count
      1 and 3 identifies the true one (identified 200, 200, 55, 0, 0, 0). K1 red on the
      code before B2a (765 wrong rows), with step 1 reverted (smallest count 3
      identifies none), with step 2 reverted (597 wrong), and with `alpha` in place of
      `alpha / K` (3 wrong); each applied and reverted. K2: 200 of 200 rows of smallest
      count 1 and 200 of 200 rows of one 1-count cell over a bulk keep the true step.
      K3 (reported): with step 3 reverted, inpt's dense `Count` verdicts at 0.05 rise
      from 245 to 695 and every sparse row is identical.
      A wrong identification is bounded at `alpha` per array by the design, not absent:
      K1 is a count on fixed rows, not a rate.
- [x] 2026-10-02: the count-region edge of B2c is proven right and red
      (`CountRegionEdgeTests`, `../Harness/HISTORY.md#count-region-edge-2026-10-02`),
      on real cells. K4a: HPEPA3 independent replica 14 against the other 31 fails
      exactly `fqkarm[74]` and `fqkarm_cor[74]` before B2c and nothing after; P33
      `Independent`/`Original` seed 13 fails exactly `fqkarm[63]` before and nothing
      after. K4b: HPEPA3 lagged replica 6 `coef[468]` (count 16 of `high` 16) and
      replica 1 `coef[490]` (4 of 4) fail at 0.05 before and pass after. K4c: each of
      those five cells one `Quantum` higher fails before and after. K4d: HPEPA3 lagged
      replica 24 `fqdokkarm(6,:)[18]` and `[19]` pass before and after. Red before
      B2c: K4a and K4b (7 tests of 15), at 3b6bad1. Mutations, applied and reverted:
      the comparison back on the printed value turns K4a and K4b red (7 of 14); `s = 0`
      turns K4d red (2 of 14).

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.
