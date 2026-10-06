# BOOT.md — Harness.Tests

## Purpose

The definition of what "Harness is ready" means: the `results.m` parser
(`ResultsMFile`), the Student quantile (`StudentDistribution`) and the statistical
comparison (`StatisticalCriterion`) that `tests/Harness` exposes to every other test
node (`tests/Harness/API.md`).

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | `ResultsMFile.Parse`/`ParseCells` read every reference and replica file (all three kinds) of the five formulations without throwing | `tests/Fixtures/references`, `replicas-lagged`, `replicas-independent`, `replicas-gsv3` | ✅ |
| L0 | spot-checked quantities of the HPEPA3 reference match the printed file by eye | `tests/Fixtures/references/HPEPA3/results.m.txt` | ✅ |
| L0 | a parse round-tripped through JSON loses nothing | the five reference files | ✅ |
| L0 | a cell's resolution and integer-ness match its printed token, for one representative of each format family | the HPEPA3 reference | ✅ |
| L0 | `StudentDistribution.TwoSidedQuantile` matches scipy 1.18.1 | `tests/Fixtures/cases/harness/student_t.json` (72 cases) | ✅ |
| L0 | `StatisticalCriterion.BinomialBand` (Fable 5.1 decision II's calibration curve, `tests/Harness/BOOT.md`) matches an independent scipy implementation of the same highest-density construction, and rejects a count far outside the band | typed directly in `BinomialBandTests.cs` (no fixture holds this; `tests/Fixtures` is a neighbour node this task may not write into) — 9 scipy cases plus 2 non-degeneracy cases | ✅ |
| L1 | `StatisticalCriterion.Compare` against `ReplicaKind.Lagged` passes with zero failures, for each formulation's own GSV=2 reference, except HMX, ratcheted to its one known-open cell (`[1]`, `[6]`) | `tests/Fixtures/references`, `replicas-lagged` | ✅ [1] |
| L1 | `StatisticalCriterion.Compare` against `ReplicaKind.Gsv3` runs end to end and reports every quantity, kept as a cross-check with no pass condition | `tests/Fixtures/references`, `replicas-gsv3` | ✅ |
| L1 | the exclusion rule zeroes both sides of a named cell regardless of the candidate's own value | HPEPA3, `pdoksmall(1)`, `pdoksmall(2)` | ✅ |
| L1 | `StatisticalCriterion.TwoSampleBias` matches the measured truth table of root BOOT.md's "Known bias" claims | `replicas-lagged`, `replicas-independent`, all five formulations | ✅ |
| L2 | blind calibration (steps 1-3 of `tests/Harness/BOOT.md`'s "Tail coverage"): every lagged replica as candidate against the other `R - 1`, `Compare`/`CompareTailRowMean`/`CompareAdaptiveIndexMatched`, ratcheted to the recorded failing set (root BOOT.md, "the pass condition compares failure rates, not single runs"; `[6]`, `[7]`) rather than gated at "at most one" | `tests/Fixtures/replicas-lagged`, all five formulations, `Category=Long` | ✅ [1][2][3][6][7] |
| L2 | the tail-row-mean and adaptive-index-matched sensitivity sweeps (step 4), on HMX's own printed reference file, no rerun of the executable | `tests/Fixtures/references/HMX/results.m.txt`, mutated in memory | ✅ |
| L2 | oracle mutation: every comparison rule's own pass/fail boundary is real (a live search against the rule itself, never a reimplemented formula), on HPEPA3 and HMX, and unreachable-boundary cell classes are reported rather than hidden (`tests/Harness/HISTORY.md#oracle-mutation-2026-09-19`) | `tests/Fixtures/references` and `replicas-lagged`/`replicas-independent`, HPEPA3 and HMX, `Category=Long` | ✅ |
| L2 | the calibration curve (Fable 5.1 decision II): per rule (Student/Count/Mass), over non-degenerate cells only, the blind failure fraction at each of `alpha in {0.05, 0.01, 0.001}` lies inside the exact binomial band of the nominal level, ratcheted to the recorded violating rows (root BOOT.md, "the pass condition compares failure rates, not single runs"; `[6]`, `[7]`) rather than gated at zero | `tests/Fixtures/replicas-lagged`, all five formulations, 96 leave-one-out candidates x 3 levels, `Category=Long` | ✅ [4][6][7] |
| L2 | E1 (`tests/Harness/BOOT.md`, "Mass bracket"): the mass bracket's feasible interval fails no genuine run of the original, and its boundary is real on both sides at a known cell (right control, cell tests, empty-interval and clamp unit tests) | `tests/Fixtures/references`/`replicas-lagged`/`replicas-independent`, all five formulations, `Category=Long` | ✅ [7] |
| L1 | B2a (`tests/Harness/HISTORY.md#b2a-amended-2026-10-02`): `RunQuantum.TryInfer` on rows of exact integer counts of a known step, printed to three significant digits, identifies the true step and never a wrong one (K1), keeps the step where it was right (K2), and K3 reports inpt's cells by governing term and pooled count | `QuantumIdentificationTests`: 1200 fixed rows, and `tests/Fixtures/replicas-lagged/inpt` for K3 | ✅ |
| L1 | B2c (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`): a count-governed cell passes its whole predictive region and still fails a count beyond it — K4a, the sparse edge (two runs the rate criterion lost to it), K4b, the dense edge, K4c, each of those cells one `Quantum` higher fails, K4d, two dense cells whose centre departs from the predictive mean by a whole count pass | `CountRegionEdgeTests`: real cells of `tests/Fixtures/replicas-lagged`, `replicas-independent` and `rate-runs` | ✅ |
| L1 | root BOOT.md's "only the oxidizer size distribution ... is unaffected" checked, not assumed, over the whole Dok family (not just `Dok43all`) | `replicas-lagged`, `replicas-independent`, all five formulations, `Category=Long` | ✅ [5] |
| L1 | the "not compared against independent replicas" rule drops `epsx(4)` whole, only against `ReplicaKind.Independent`; still an ordinary compared cell against `Lagged` | HPEPA3 independent replica 1, `epsx(4)` set far outside its own band | ✅ |
| L2 | `CompareSets` finds exactly the declared cells of `docs/declared-differences.json` on every formulation's own `rate-runs` (`Original` precision), gated on the `Lagged` layout, PSAN02n `Independent` reported only (`[8]`); differs outside them under `Binary64`, gated on HPEPA3/HMX; both negative controls are empty everywhere | `tests/Fixtures/rate-runs`, `tests/Fixtures/replicas-lagged`/`replicas-independent`, all five formulations, `Category=Long` | ✅ [8] |
| L1 | every stored `rate-runs` file ties one to one to the table's own `ResultSha256` (`RateRunsTieToTheRateTable`); 6,000 random candidate half-splits measure the method's own false-positive rate (`RandomSplitNullRateTests`, `../Harness/HISTORY.md#set-comparison-design-2026-09-27`) | `tests/Fixtures/rate-table.json`, `tests/Fixtures/rate-runs`, `Category=Long` | ✅ |
| L1 | F-b (`tests/Harness/BOOT.md`, "Canonical category axis"): the fixed-width prefix is each run's own, not shared — the census over every lagged and independent leave-one-out replica matches the recorded 17/96 and 61/96 | `tests/Fixtures/references`/`replicas-lagged`/`replicas-independent`, all five formulations | ✅ [9] |
| L0 | F-c (`tests/Harness/PrintResolution.cs`, `ExponentOf`): the printed exponent of every non-zero `E` token matches the function's own recomputation, and the one moved cell verdict is checked directly | `tests/Fixtures/references`/`replicas-lagged`/`replicas-independent`/`replicas-gsv3`/`rate-runs`, `Category=Long` | ✅ [10] |

[1] ⚠ 2026-09-27: moved in full, to make room under the §15 leaf limit for E1 (headline
kept: fails only for HMX, `fmkarm_cor2[84]`) → HISTORY.md#footnote-1-moved-2026-09-27

[2] ⚠ 2026-09-27: moved in full, same reason (headline kept: a real, fixed defect, not a
flake — `Step2_BlindCalibration_TailRowMean` green on all five formulations once #1's
sparse-cell exclusion was wired in) → HISTORY.md#footnote-2-moved-2026-09-27

[3] ⚠ 2026-09-19: superseded in full — the four withdrawn gate-2 exceptions this
footnote once tracked as an open finding are long gone; `tests/Harness/BOOT.md`'s "Gate
2, redefined" replaced the per-cell lists with one gate over the same 96 runs, itself
since superseded by the rate criterion → HISTORY.md#footnote-3-superseded-2026-09-27

[4] ⚠ 2026-10-02: was "genuinely red … the `Student` rule fails at `alpha = 0.001` for
P33 alone, traced to the pre-existing P33 replica 14/15 contamination" (2026-09-20),
now a green ratchet (`[6]`) on the rows the two calibration criteria of
`tests/Harness/ACCEPTANCE.md` name; there is no contamination →
HISTORY.md#footnote-4-moved-2026-10-02

[5] ⚠ 2026-09-20: root BOOT.md's own absolute — "Only the oxidizer size distribution,
which the neighbour loop does not feed back into, is unaffected" — named a single
quantity, `Dok43all`, as its evidence. `OxidizerSizeDistributionBiasTests` reads
`StatisticalCriterion.TwoSampleBias` over the whole family the original prints under
"Dok parameters" and "Conditional DOK" instead: 12 unambiguous, non-canonical-axis
names (`Dok43a`, `Dok43all`, `Dok43(1)`, `Dok43(2)`, `Dok43sd`, `Ddok_max`,
`epsalldok`, `epsdok(1)`, `epsdok(2)`, `epsdokfr`, `fineoxy_fr`, `fmdok`), each
verified present before being read (guards the "not found" pitfall the task that
produced this row warned about). Measured: HPEPA3 (`fmdok[1]`), PSAN02n (`Dok43all[1]`,
`epsdokfr[0]`, eight `fmdok` cells) and HMX (`fmdok[1]`, `fmdok[2]`, `fmdok[3]`) each
have at least one such cell differ at the criterion's own family-wise level — the
"only" claim is refuted for three of the five reference formulations; inpt and P33
show no core-family difference in this same reading. `dokkarm43`/`dokkarm10`/
`fqdokkarm(<row>,:)` (also oxidizer-size, but canonical-category-axis families) and the
ambiguous `Dkarmcat` are reported separately, marked, never folded into the verdict
above: `TwoSampleBias`'s plain per-index comparison does not align that axis the way
the criterion's own canonical-axis machinery does, so a "differs" there is not evidence
either way. Root BOOT.md's own correction is AGENTS.md §11 — an ancestor's document —
so this node leaves the finding here rather than editing it; the full per-formulation
figures are this test's own commit message.

⚠ Declared deviation, §13, LIFTED 2026-09-24: this paragraph first declared three
checks red by design (gate 1, gate 2, gate 3, each cited by name below), each a per-run
or per-leave-one-out-set bound this node's own measurements had shown could not be met.
Root BOOT.md's own decision the same date this correction is dated ("the pass condition
compares failure rates, not single runs") reads the same three figures — the original's
own runs failing the per-run criterion far above its declared level — as the reason a
*rate*, not a per-run or per-set bound, is the right pass condition; a bound already
known unmeetable when it was written is not a declared deviation waiting on future work,
it is the perpetually red check §13 forbids outright ("worse than an absent one ...
people get used to red"). All three are rewritten as ratchets on the recorded set (the
same form Gate2's own leave-one-out set and the retired `tests/Simulation.Tests` seed-0
ratchet already used), scope addition `[6]` below has the full account and the red/green
evidence for each. The §13 deviation is lifted, not merely narrowed: none of the three
is red any more, under any filter. The paragraph's own former text, unedited, is
preserved as provenance → HISTORY.md#declared-deviation-13-three-red-checks-2026-09-20

[6] 2026-09-24, a coordinator-flagged scope addition (root BOOT.md, "the pass condition
compares failure rates, not single runs"): the three checks the paragraph above declared
red were still red on a full-suite run, and AGENTS.md §13 forbids that — each converted
to a ratchet on its own recorded set (the known cell; the recorded 13 of 96; the recorded
eight violating rows, down from 11 by an unrelated same-day fix), never loosened. Full
reasoning and mutation evidence: HISTORY.md#footnote-6-three-red-checks-fixed-2026-09-24.
`Category=Long`: 69/69 pass.

[7] 2026-09-27, E1 (`tests/Harness/BOOT.md`, "Mass bracket"): the mass bracket's own
feasible interval removed 5 of the 13 lagged-leave-one-out failures Gate2's own ratchet
recorded (`HPEPA3 replica 26`, `inpt replica 10`, `P33 replica 11`; new figure 10 of
96) and the calibration curve's own "Mass containment" row, the eighth of its recorded
eight violating rows (now seven; `tests/Harness.Tests/CalibrationCurveTests.
KnownCalibrationViolations`). New tests: `tests/Harness.Tests/MassBracketTests`, four
new mutations recorded in `MassFamilyRule.cs`/`RunQuantum.cs`'s own comments and this
node's own regeneration record (`tests/RateTableTool/BOOT.md`).

⚠ 2026-10-02: the recorded violating rows of `[7]` ("now seven") were re-recorded
after B2a, `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`: still seven rows, no
longer the same seven (`CalibrationCurveTests.KnownCalibrationViolations`).

⚠ 2026-10-02, after B2c: the recorded rows were re-recorded again,
`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`: five rows, HPEPA3 at 0.05 and
0.01, `Count`, are inside their bands
(`CalibrationCurveTests.KnownCalibrationViolations`).

[8] 2026-09-27, `CompareSets` (`tests/Harness/BOOT.md`, "## Set comparison"): the
epsdokfr exclusion the design session proposed for PSAN02n does not hold at the tree's
own alpha (`ReplicaArcEvidenceTests`, the Spearman rank-correlation trend test the
coordinator ruled as primary — `tests/Harness/HISTORY.md#set-comparison-epsdokfr-evidence-2026-09-27`
has both lag-1 figures, measured and not used). No exclusions.json entry is added, so
PSAN02n's own `Independent`-layout subset check is not gated, only reported
(`SetCriterionTests.OriginalDiffersOnlyInDeclaredCellsIndependentReportedOnlyForPsan02n`),
design §7 item 4's own declared fallback.

[9] 2026-09-27, F-b (`tests/Harness/BOOT.md`, "Canonical category axis";
`tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`): new test
`tests/Harness.Tests/FixedWidthPrefixCensusTests`, one mutation applied and reverted —
right: 17 of the 96 lagged and 61 of the 96 independent leave-one-out replicas have
their own `Dkarmcat` prefix length differ from their formulation's own reference; red:
substituting the reference's own prefix length for every replica's own (today's
superseded "shared" reading) collapses both counts to 0/96.

[10] 2026-09-28, F-c (`tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`):
new test `tests/Harness.Tests/PrintResolutionTests`, non-degeneracy proven by reverting
`ExponentOf` to the old formula — right: 1,129,554 non-zero `E` tokens over every
criterion fixture file agree with their own printed exponent; red: 4,342 disagree,
every one `0.100E+-xx`; the one moved verdict (P33 `Independent`/`Original` seed 13,
`fqkarm[62]`) is checked directly against `StatisticalCriterion.Compare`.

⚠ 2026-10-02: row L2, blind calibration, read "ratcheted to the recorded 10-of-96
failing set", E1's figure, stale since E2 the same day (9 of 96); now "the recorded
failing set" (`TailCoverageTests.KnownFailingLaggedLeaveOneOutRuns`).

## Invariants

- No test here types an expected value that a fixture file already holds (root BOOT.md
  Taboos): the round-trip and quantity-count assertions compare parses against
  themselves or against `tests/Fixtures/cases/harness/student_t.json`, not against a
  hand-copied number, except the handful of spot-checked constants transcribed
  directly from `tests/Fixtures/references/HPEPA3/results.m.txt` for the eye-check row
  above (each cites its own source line in the test).
- `StatisticalCriterion.Compare` reads `tests/Fixtures` itself (`ResultsMFile`'s own
  paths), so there is no seam to inject a synthetic reference/replica set; a test that
  wants a specific candidate builds one by cloning a real parse and overwriting the
  cells it needs (`ExclusionRuleForcesPdoksmallGarbageCellsToCompareAsZeroRegardlessOfTheCandidate`),
  never by fabricating a fixture file.
- `StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`
  asserts zero failures (`tests/Harness/HISTORY.md#criterion-revision-2026-09-17`): the GSV=3
  finding of `tests/Harness/HISTORY.md#open-finding-2026-09-17` is resolved, not sidestepped, and
  the GSV=3 comparison itself stays as a separate, explicitly unasserted cross-check
  (`EachFormulationsOwnGsv2ReferenceAgainstGsv3ReplicasIsAReportedCrossCheckOnly`).
- `TwoSampleBiasTests` asserts the *measured* truth table of root BOOT.md's "Known
  bias" claims, not the claims exactly as first written: where a claim does not hold
  for a formulation (`Dkarm43(1)`/`Zkarm` mostly, a handful of HMX's rare-event
  quantities), the assertion matches what was measured (`tests/Harness/BOOT.md`,
  "Two-sample bias") rather than being bent to agree with root BOOT.md or silently
  dropped — root BOOT.md Taboos, "no expected value ... no loosening".
- The calibration curve (`CalibrationCurveTests`) judges each row against the binomial
  band of its level, cells taken as independent. A band widened for the clustering of
  failures by run may not be estimated from the failures it judges; the dispersion `D`
  the test prints is a report, never a band (decided 2026-10-02 →
  `tests/Harness/HISTORY.md#calibration-band-not-from-its-own-failures-2026-10-02`).
  The same test prints the `Count` row split by family, region exits beside the credited
  mass and the replicas' factor `1 + (n_bar - 1) rho_hat` (`CountCoverageReport`, B2b
  step 1, a report, never a band; asserted only to add up to the row; table in
  `HISTORY.md#count-coverage-report-2026-10-02`).

## Dependencies

- [Harness](../Harness/API.md) — the code under test.
- [Fixtures](../Fixtures/API.md) — reference, replica and `cases/harness/student_t.json`.
- [defect-report](../../tools/defect-report/API.md) — `docs/declared-differences.json`'s
  own schema, read by `DeclaredDifferences.cs` (data only, no code called).

Outside the tree: xunit.

## Constraints

- Part of the default test command.

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No source-node test lives here (this is `Harness`'s own readiness, not a source
  node's).
