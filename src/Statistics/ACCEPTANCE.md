# ACCEPTANCE.md — Statistics

## Acceptance criteria

- [x] The line map covers every executable line of 267–277, 376–406, 771–1175 and
  1695–1757; the list of lines is generated from the source. (2026-09-18:
  `tests/Statistics.Tests/LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`.)
- [x] 2026-09-18: for small constructed totals, `Categories.MergeAndDescribe`'s every
  report value and in-place rewrite equals the Fortran formulas, computed by a script
  in `tests/Fixtures`, bit for bit (a merge pass, a shift, a last-row merge without
  recomputation, `QDOKSS = 0`)
  (`CategoriesTests.MergeAndDescribeMatchesTheFormulaScript`, four cases against
  `tests/Fixtures/cases/statistics/categories.json`).
- [x] 2026-09-18: `SmallParticles.Probability` and `SmallParticles.MaxSize` — the
  node's only two outputs fed back into the model loop (`pdoksmall`, `Dmaxxx`) — match
  a second, independent formula-script transcription bit for bit, each branch (the
  `zdoksmall > 1e-5` guard, the monotone clamp, `MaxSize`'s first-crossing threshold)
  exercised by its own case
  (`SmallParticlesTests.ProbabilityMatchesTheFormulaScript`,
  `.MaxSizeMatchesTheFormulaScript`, against `small_particles_probability.json` and
  `small_particles_max_size.json`).
- [x] 2026-10-02: the rest of `Compute`'s pipeline (`GeneratorAccuracy`,
  `OxidizerSizes`, `Pockets`, `Matrix`, `CorrectedPockets`, `MassFractions`,
  `Convergence`) matches a second, independent formula-script transcription (⚠ below:
  narrower than the original wording this replaces), bit for bit under both precision
  kinds: every report value, `nextPdoksmall`, `nextDmaxxx` and the rewritten totals, on
  cycle 0, cycle 1 and three seeded cycles, one with the warning flags that read a
  register and one that read a rounded store
  (`CycleStatisticsFormulaTests.ComputeMatchesTheFormulaScript`, against
  `tests/Fixtures/cases/statistics/cycle_statistics.json`; each member's mutation is in
  `tests/Statistics.Tests/BOOT.md`, "## Mutations"). The script is written from the
  Fortran and `CyclePlane.listing.generated.txt` (the source-read table until
  2026-10-03), not from `CycleStatistics.cs`, but for one choice: the cell centre formed
  first, `API.md`'s rule ("## Cycle") since 2026-10-02, read off `Compute` after the
  written order differed from it in the last bit under `Binary64`. On that choice the
  match is a change detector; on every term, weight, index range, summation order and
  rounding site it is independent. In cycle 0 the nine print-time `*Normalized`/`*Nmax`
  entries are not asserted. `CycleStatisticsTests` keeps its hand-derived identities.

  ⚠ 2026-09-18: was one criterion, now split three ways once `Categories` turned out
  to need the formula-script treatment on its own → HISTORY.md#cycle-statistics-criterion-split-2026-09-18

  ⚠ 2026-09-18 (audit): was "short, branch-free" covering `SmallParticles` too, now
  narrowed to the seven pieces it fits → HISTORY.md#small-particles-branches-note-2026-09-18
- [x] 2026-09-18: for the five reference formulations, the setup quantities the
  reference `results.m` prints (`Dok43a`, `Dok43sd`, `Ddok_max`) equal it at print
  precision, and the modified `alfa` equals the unmodified one (all five have
  `alfa = 0`, so `Dokb_max` is never printed or checked)
  (`SetupTests.PrepareMatchesThePrintedSetupQuantitiesOfItsReferenceFormulation`).
- [x] 2026-10-01, reformulated 2026-09-27 (AGENTS.md §6): fed with the totals of
  reference-mode runs under `Original`, the report fails the statistical criterion no
  more often than the original's own runs do. Evidence: the root's rate criterion, whose
  160 runs each print every quantity this node computes (root `ACCEPTANCE.md`,
  "Reference mode under `Original` fails…"; `tests/Harness.Tests/RateCriterionTests`).
  It cannot see a difference within one print unit of a cell every replica prints
  alike (row 771–1175).

  ⚠ 2026-10-02: was dated 2026-09-24 and "23 of 160 against 22 of 197", now the root's re-run → HISTORY.md#rate-criterion-dating-2026-10-02

  ⚠ 2026-09-27: was "Fed with the totals of a reference-mode run, the report satisfies
  the statistical criterion against the reference `results.m` for every non-excluded
  quantity it computes": unmeetable, as the original fails that pass in 22 of 197 of its
  own runs, and misstated, as the band comes from the replicas → HISTORY.md#report-criterion-reformulated-2026-09-27
- [x] 2026-09-18: each precondition violation returns its status
  (`SetupTests.PrepareRejects*`, one test per clause of `InvalidCoefficients` and one
  per every other reject-able `SetupStatus`; `NoActiveFraction` has no test, proven
  unreachable by `FractionLaw.TryTailDraw`'s own doc comment; extended 2026-09-20 with
  `InvalidNnWindow`, `.PrepareRejectsInvalidNnWindowWhenNnMaxIsNotPositive` and
  `.PrepareRejectsInvalidNnWindowWhenNnMinIsNotBelowNnMax`; "## Constraints" carries
  the defect this closes).

  ⚠ 2026-09-26: was the audit's own finding (`InvalidCoefficients` untested for
  `Ak2 > 1`) told in full, now moved → HISTORY.md#precondition-criterion-audit-narration-2026-09-26
- [x] 2026-09-18 (`Nkarm`/`Ncat` evidence 2026-09-24): `Ndok` equals the length printed
  in the archived outputs of C166, T56, CSPX01, P18050 and PSAN01, and the binary32
  quotient helper equals exact rational truncation on a generated set of operands.
  `Nkarm`/`Ncat` have no clean archived length, but a constructed boundary case each
  and C166 confirm the rounding rule moves neither
  (`SetupTests.SizesNdokMatchesThePrintedCategoryRowLengthOfItsArchivedFormulation`,
  `Binary32Tests`, `.SizesUsesTheUnroundedProductForNkarm`,
  `.SizesUsesTheUnroundedProductForNcat`,
  `.SizesNkarmAndNcatOfC166AreUnaffectedByTheProductRoundingFix`; "## Invariants", S-2).

  ⚠ 2026-09-26: was the fixture-case wording and the archive-absence reason told in
  full, now the confirmation alone → HISTORY.md#ndok-criterion-narration-2026-09-26

  ⚠ 2026-09-18 (audit): was a test typing its five printed lengths as literals, now
  parsing each archive directly (all unchanged: 71, 71, 23, 20, 62) → HISTORY.md#ndok-archive-test-typed-literals-audit-2026-09-18

  ⚠ 2026-09-24 (S-2): was `Nkarm`/`Ncat` checked through `Ndok`'s own helper, now
  through `Binary32.TruncatedQuotientOfProduct` → HISTORY.md#nkarm-ncat-product-rounding-s2-2026-09-24
- [x] 2026-09-18: the binary32 exception extended to `DPRow` and the whole-fraction
  count of `pdoksmall` (root BOOT.md, "Double precision only") is plumbed through,
  proven by a constructed boundary case each; no archived or reference formulation's
  printed length moves (`CategoriesTests.MergeAndDescribeUsesBinary32ArithmeticForDpRow`,
  `SmallParticlesTests.ProbabilityUsesBinary32ArithmeticForTheWholeFractionCount`;
  margins in "## Defects of the original" below, its own array-sizes row).
- [x] 2026-09-18: `DPRow > Ncat` is unreachable given `Ncat`'s own sizing and the
  model's bound on the largest pocket radius ("## Categories" above), and
  `Categories.MergeAndDescribe` reports it as a status rather than a silent clamp if it
  ever fires, with no exception
  (`tests/Statistics.Tests/CategoriesTests.MergeAndDescribeReportsStatusWhenDpRowExceedsNcat`,
  a constructed `DPRow > Ncat` input).
- [x] 2026-09-20: `Allvdok`'s, `Vdokstr`'s, `VdokTotal`'s and `VdokTotal2`'s own
  measured REAL*4 accumulation error is carried through to every printed quantity each
  reaches, put against that quantity's own band width; `VdokTotal`/`VdokTotal2` proven
  not to reach `pdoksmall`, `fmdok` nor `Dok43all[1]` at all (`## Report`, "REAL*4
  accumulation, carried through"; `AccumulationConsequenceTests`, thirty cases).

  ⚠ 2026-09-21, three times the same day: was "overstated", then closed, then re-run
  against `Particle`'s re-measured epsilon (every case passes; only verdicts move) → HISTORY.md#accumulation-consequence-verdict-history-2026-09-21
- [x] 2026-09-20: `CycleReport.Pdoksmall` is `Ndok − 1` elements, matching the archived
  `pdoksmall` (`tests/Statistics.Tests/PdoksmallReportLengthTests`, ten cases); the
  category-merge threshold defect ("## Defects of the original", Fortran 911–959) is
  confirmed as the mechanism
  (`MergeThresholdSensitivityTests.EpsydokThresholdFlipsWithinOneBinary32UlpOfTheBoundary`).

  ⚠ 2026-09-26: was the archive framing and elimination argument told in full, now
  the confirmation alone → HISTORY.md#pdoksmall-length-criterion-narration-2026-09-26
- [x] 2026-09-21: the `fmkarm`/`fqkarm`/`fmkarm_cor`/`fmkarm_cor2`/`fqkarm_cor`
  print-length escalation ("## Report" above) is resolved: `Output`'s formula matches
  the archive on all five formulations for the first pair, four of five for the
  second — the sole miss (HMX) being `DpMaxCor`'s own accumulated value, not the
  formula (`tests/Output.Tests/PocketPrintLengthTests`; Fortran 671–703).
- [x] 2026-09-23: the setup plane's REAL*4 set is generated, never typed, and checked
  by regeneration, proven non-degenerate by a mutation (`GDOK` declared `REAL*8`,
  reverted) (`classify-setup-plane.py` → `SetupPlane.generated.txt`, the precedent of
  `src/Particle/classify-real4-accumulators.py`; `SetupPlaneClassificationGeneratorTests`,
  the both-ways site-map proof, seen red on a removed entry and on a bogus one).

  ⚠ 2026-09-27: was "recorded here since the script has no test of its own", now a test
  → HISTORY.md#script-no-test-of-its-own-2026-09-27
- [x] 2026-09-23: positive controls, known answers frozen before implementation: under
  `Original`, `inpt` prints `epsdokfr = [0.270E-07]` (measured 2.700281e-8) and p350
  `[0.263E-07]` (measured 2.629043e-8); under `Binary64` both are exactly `0.0`
  (`SetupPlaneTests.PositiveControlEpsdokfrMatchesTheFrozenAnswer`). Proven twice by
  three single-element removals: no per-addition `ZSS` rounding gives `0.0`; no
  binary32 inputs gives 1.59e-8/1.92e-9; no literal fold gives 1.071e-8/4.258e-8.
- [x] 2026-09-23 (`Ddokmax` evidence 2026-09-24): under `Original`, `Z11`, `ZX`, `ZSS`,
  λ, `Dmax`, `Ddokmax` and the echoes equal, bit for bit,
  `tests/Fixtures/formulas_statistics.py`'s independent transcription, on the five
  reference formulations and p350 (`SetupPlaneTests.SetupPlaneMatchesTheFormulaScript`,
  `tests/Fixtures/cases/statistics/setup_plane.json`).

  ⚠ 2026-09-27: was "`Ddokmax` is excluded", now computed and asserted → HISTORY.md#ddokmax-excluded-2026-09-27

  ⚠ 2026-09-26: was the script's own `Dmax` provenance told in full, now the tick,
  place and figure alone → HISTORY.md#z11-zx-zss-criterion-narration-2026-09-26

  ⚠ 2026-10-01 (A2): was `DOKM`/`DOKSD` under rule L, now the listing's schedules → HISTORY.md#doksd-rule-l-2026-10-03
- [x] 2026-10-03: every site of the per-cycle plane (771–1175, the hoisted block and
  line 378) is generated from the executable's listing, never typed
  (`classify-cycle-plane-listing.py` → `CyclePlane.listing.generated.txt`, over
  `fortran_source.py`; `CyclePlaneListingMapTests`, with seven pinned sites, one per
  kind the code follows; 16 verdicts, `classify-cycle-plane-listing.py selftest`).

  ⚠ 2026-10-03: was the criterion over the source under rule L, now over the listing → HISTORY.md#per-cycle-sites-reformulated-2026-10-03
- [x] 2026-10-03: every rounding site of `CyclePlane.listing.generated.txt` (a row whose
  `rounds` is yes: 96 sites) has a row in `CyclePlaneSites.txt` naming the members whose
  `CyclePlaneRounding` call of its kind cites a line of it, or why none does, and every
  `Store` or `Temporary` call cites rows of its kind in its member; each unrolled kind
  names a schedule of its block size (`CyclePlaneSiteMapTests`, four directions and the
  schedule check; seen red 2026-10-03 on a deleted row, a call of the wrong method, an
  uncited call, a bogus kind and `UnrolledSchedule(ndok, 7)` where the table says 6).
  Reformulated from the 2026-10-01 criterion over the retired table.
- [x] 2026-10-03: under both kinds, `Categories.MergeAndDescribe`,
  `SmallParticles.Probability` and `.MaxSize` equal `formulas_statistics.py`'s
  transcription, which reads `CyclePlane.listing.generated.txt`, never the C#, bit for
  bit (`expectedOriginal` of every case); `CycleReport.Mp` prints the original's
  `da_coef` under both kinds on all five reference formulations (`CategoriesTests`,
  `SmallParticlesTests`, `DaCoefTests`; reds in `tests/Statistics.Tests/BOOT.md`).
  Reformulated: the 2026-10-01 criterion's transcription read the retired table.
- [x] 2026-10-01: the plane's frozen controls are green: out of sample, the three of
  2026-09-28 below; at seed 0, `tests/Simulation.Tests`' `CyclePlaneSeedZeroControlsTests`
  (39 cases, red on the pre-change runs). The set criterion then found PSAN02n's
  `da_coef` entry stale and nothing undeclared, so row 771–1175 is `reproduced` and its
  register-lifetime residuals a row of their own, `open`
  (`tests/Harness.Tests/SetCriterionTests.OriginalDiffersOnlyInDeclaredCells`).

  ⚠ 2026-10-02: was "so row 771–1175 is `reproduced`", now `open` → HISTORY.md#row-771-1175-reproduced-then-open-2026-10-02
- [x] 2026-10-02: the controls of the register-lifetime row are frozen before any plane
  code, each a ratchet that goes red when its list changes in either direction
  (`tests/Statistics.Tests/ACCEPTANCE.md`, the same date). C1,
  `da_coef` against every archived output of every formulation whose input the archive
  ships: 22 formulations, 19 match, HPEPA2, P777 and CSPX01 one print unit below the
  original's seventh digit (`LegacyDaCoefReproductionTests`). C2, every differing cell
  of six path-identical pairs of the original and the port at the `N` and `KXX` where
  every integer cell agrees (`tests/Fixtures/cycle-plane-pairs`,
  `PathIdenticalPairsReportTests`, tied to the live port by `CyclePlanePairsTieTests`).
  C3, the controls above unchanged. Red where stated there.
- [x] 2026-10-03: rows 771–1175 and 771–1175 (register lifetimes) of `## Defects of the
  original` are one `reproduced` row and one `declared, not reproduced` row, for the
  runtime routines `_FIIfexp_`, `_CIsqrt` and `_FXAMOD`, consequence `not measured`
  (`docs/ORIGINAL-DEFECTS.md` regenerated, `docs/declared-differences.json` unchanged;
  evidence A1–A7 below and above). The owner's choice of 2026-10-02, the executable's
  listing as the specification, was made.

  ⚠ 2026-10-03: was "`reproduced` with C1 at 22 of 22 and C2's list empty", now C2's
  551 cells attributed by class, not empty →
  HISTORY.md#row-771-1175-closing-wording-2026-10-03
- [x] 2026-10-02: stage 1 of the plan (W0–W2: the documents, the excerpt, the map, the
  reader and the generated table, 193 sites) was in the tree before any plane code
  (`CyclePlaneListingTests`, `CyclePlaneListingMapTests`), read against the arbiter's
  inventory with no rule of it contradicted.

  ⚠ 2026-10-18: was the stage told with its inventory comparison in full, now the tick
  and places → HISTORY.md#stage-1-narration-2026-10-18
- [x] 2026-10-03: A1, C1 (`LegacyDaCoefReproductionTests`, `LegacyDaCoef.approved.txt`):
  22 of 22 formulations print the original's `da_coef`; the approved file moved from
  19 of 22 (HPEPA2, P777 and CSPX01 one print unit below) in the same commit as the
  plane's rewrite, the count the design predicted in advance (S2 did not fire).
- [x] 2026-10-03: A2, the listing oracle (`tests/Fixtures/cycle_plane_oracle.py`, an x87
  interpreter over the excerpt's bytes; `tests/Statistics.Tests/ListingOracleTests`):
  under `Original`, `Compute` equals it bit for bit on every case of
  `cases/statistics/cycle_plane_oracle.json` and `Setup.Prepare` its pre-loop run (λ,
  `DOKM`, `DOKSD`); `ListingOracle.approved.txt` and `ListingOracleSetup.approved.txt`
  are empty, from 463 and 19 lines (r4, seen once); 26 cases, 58 isolated sites, 18
  `unreached` → HISTORY.md#oracle-fixture-cases-2026-10-03

  ⚠ 2026-10-18: was the unseen reds and the fixture's composition told in full, now the
  figures → HISTORY.md#a2-narration-2026-10-18, the coverage sentences →
  HISTORY.md#a2-coverage-sentences-2026-10-18
- [x] 2026-10-03: A3, C2 (`PathIdenticalPairsReportTests`,
  `PathIdenticalPairs.approved.txt`, `CyclePlanePairsTieTests`): the port side of the
  six pairs regenerated on the stage 3 build, every integer-printed cell still equal (S4
  did not fire), the original's side kept as committed; the differing cells stay at 551,
  four moved (→ HISTORY.md#c2-four-moved-cells-2026-10-03), by the design's attribution,
  not a per-cell measurement, 15 of the print plane and 536 fed by the attempt plane's
  REAL*4 inputs `Dr`, `Db`, `RK`; none outside the set moved.

  ⚠ 2026-10-18: was the per-pair class counts and why the original's side stays told in
  full, now the totals → HISTORY.md#a3-narration-2026-10-18
- [x] 2026-10-03: A4, C3: 295 of 295, 2016 of 2016, 293 of 293, control (iv) and every
  `Binary64` snapshot, unchanged bit for bit (`PerCyclePlaneOutOfSampleControlsTests`,
  `CycleStatisticsFormulaTests` and every snapshot of the fast set; S3 and S7 did not
  fire).
- [x] 2026-10-03: A5, the 39 seed-0 cases (`tests/Simulation.Tests/
  CyclePlaneSeedZeroControlsTests`, `rate-runs/*/Original/Original/0.m.txt`, not
  regenerated): green, `da_coef` and every counter unmoved, so S5 did not fire and the
  rate runs stay as they were until stage 4.
- [x] 2026-10-03: A6, the rate (`tests/Harness.Tests/RateCriterionTests`,
  `SetCriterionTests`, 59 cases) on the 160 `Original` runs regenerated
  (`tests/RateTableTool`), `Binary64` byte-identical: 17 of 160 against the original's
  15 of 197, p = 0.102792, no excess at `α`, the failing cells of every run unchanged;
  the positive control on `Binary64` rejects on HPEPA3 and HMX at p = 1.63e-36 each;
  the seed-0 controls (A5) hold, 39 of 39.

  ⚠ 2026-10-18: was the regeneration's commands, byte and hash counts told in full, now
  the deciding figures → HISTORY.md#a6-narration-2026-10-18
- [x] 2026-10-03: A7, one positive control per site kind the table leaves `unreached` in
  the oracle: 16 cases `control_NN_*` of `cycle_statistics.json` and 35 controls in
  `CONTROLS` of `formulas_statistics.py`, each moving a bit of the `Original` report
  when its store or schedule is flipped in the twin; the 18 `unreached` sites each
  controlled (7) or exact by the table (11). Red on the C# of cdc6237: 28 of 82 tests,
  `Binary64` cases passing on both. Line 378's λ: `ListingOracleSetup.approved.txt`.

  ⚠ 2026-10-18: was the control kinds and the red tests told in full, now the counts → HISTORY.md#a7-narration-2026-10-18
- [ ] A8 (the arbiter's F1 of 2026-10-03): the loops of Fortran 379–406 are generated
  from the listing, not typed: the map's scope covers 0x408ADC–0x408FA6 and
  `Setup.AnalyticSizes` and `formulas_statistics.py` follow rows of the table. It lifts
  the declared deviation of `BOOT.md`, "## Setup plane"; until then the unroll factors
  and stores are typed and held by the pins, the survey and the drawn loops (A12).
  Costed 2026-10-03: about forty rows, two new site kinds, pins for residues 0, 3
  → HISTORY.md#a8-costing-2026-10-03
- [x] 2026-10-03: A9 (F3), the call-site order of the plane's products is held
  structurally (`tests/Statistics.Tests/CyclePlaneOrderSiteTests`; `API.md`, "## Cycle"):
  green on the 23 calls, red at its own line on eleven edits, right on three commutations
  (`tests/Statistics.Tests/BOOT.md`, "## Mutations").

  ⚠ 2026-10-18: was the rule of the association test told in full, now the figures → HISTORY.md#acceptance-narrations-u2-2026-10-18
- [x] 2026-10-03: A10 (F4), `PARAM` runs in the oracle (0x4181A5–0x418577) and `ZSS`,
  `ZX`, `Z11` are the executable's (`ListingOracleTests`, `PreloopSurveyTests`, both
  approved files empty): over the 49 shipped files `ZSS` per addition equals it on 49
  (one rounding after the loop parts on 16); `ZX` parted by 1–3 ulp on 33 and `Z11` on
  16 while `FractionLaw.Build` formed `l·l·u·u`, the executable `(l·l)·(u·u)`, now 49 of
  49. The rate runs, the seed-0 snapshot, C2 and the 39 controls did not move;
  `Binary64` is bit-identical. Pins `HMX`, `P2a` (`ZS`), `HPEPA3` (`ZX`).

  ⚠ 2026-10-18: was the call's stub, the reader fix and the moved references told in
  full, now the deciding figures → HISTORY.md#a10-narration-2026-10-18
- [x] 2026-10-03: A11 (F5), (a) the fixture's provenance digests `formulas_statistics.py`
  (`ListingOracleTests.TheFixtureIsOfTheCommittedBytes`, red until restamped); (b)
  `.TheTwinThatSuppliesTheInjectedSetupReproducesItsCaseFiles` runs the twin's `verify`
  in the fast set, red on `DOKM_UNROLL = 6`; (c) `QKS1`'s injection and `Compute` are
  one expression at every count (A14; `Qks1NormalisationTests`).

  ⚠ 2026-10-03: was (c) "one expression only while `QKSS` is a binary32 integer", now
  at every count (A14); the text moved 2026-10-18 → HISTORY.md#a11-narration-2026-10-18
- [x] 2026-10-03: A14 (from A11), the divisor of line 718 is the exact count: `QKSS` is
  `INTEGER*8`, the oracle's injection `f32(q / QKSS)` equals `Compute` on every cycle
  (`Qks1NormalisationTests`, the 2^24 and 2^24 + 1 pair); nothing moves, the row
  `718, 963–996` is `reproduced`.

  ⚠ 2026-10-18: was the excerpt's loop addresses and instructions told in full, now the figures → HISTORY.md#acceptance-narrations-u2-2026-10-18
- [x] 2026-10-03: A12, the `JZZ = 2` setup loop is the executable's (0x408CFE–0x408F8E;
  the schedule: `BOOT.md`, "## Setup plane"), followed by `Setup.AnalyticSizes` and the
  twin's `jzz2_sums`; the survey's approved file is empty (`CSPX04` agrees). Right: the
  six shipped `JZZ = 2` files and 120 drawn loops, zero differing bits
  (`PreloopSurveyTests.AnalyticSizesReproducesTheExecutablesJzz2LoopOnDrawnLoops`). Red:
  the earlier rule parts on 91 and 121 of 300 drawn loops, four edits of the stores on
  8 to 27 of 120, the same edits turning that test red.

  ⚠ 2026-10-18: was the schedule's addresses and each red edit's count told in full,
  now the figures → HISTORY.md#a12-narration-2026-10-18
- [x] 2026-10-03: A13 (the arbiter's suggestion), the pre-loop survey of the 49 shipped
  files is a ratchet (`PreloopSurveyTests`, `PreloopSurvey.approved.txt`): `Setup.Prepare`
  under `Original` against the executed `zss`, `zx`, `z11`, `lambda`, `DOKM`, `DOKSD`
  and `GGG` differed on `CSPX04` alone until A12; the approved file is empty now. Red:
  the five-fold schedule set to rule L gives `AK157a`, `BK10`, `KB397` and `C166`.

  ⚠ 2026-10-03: the first run gave `C166` besides `CSPX04`, the twin's reader at fault → HISTORY.md#survey-c166-reader-2026-10-03
- [x] 2026-09-23: HMX's `Dok43all` under `Original` minus the original, same seed, seeds
  302–305 and 172–175 (`run_original.py` against `propstruct --precision original`):
  +0.0004 % and 0.0000 % against the ±0.002 % bound (was −0.0102 %); 15 of 16 cells
  print the original's value exactly.
- [x] 2026-09-23 (re-measured 2026-09-24 after the `Dmin`/`Di`/`Dj` fixes): under
  `Binary64`, every snapshot and bit-identity check in the tree passes unchanged, and
  the five reference formulations stay bit-identical at seed 0
  (`dotnet test PropStruct.sln --filter "Category!=Long"`, every node, 0 failures;
  `Particle.Tests.SnapshotTests`, `Execution.Tests.DeterminismTests`,
  `Simulation.Tests.DegenerateConfigurationTests`; setup-plane detail in
  `HISTORY.md#setup-plane-remeasured-both-fixes-2026-09-27`).
- [x] 2026-09-23: the defect row for the fraction-share normalization is re-read against
  the source and corrected: the sum and division live at lines 1723/1725/1727; `The port` is
  `reproduced` (under `Original`; `Binary64` unaffected), `Consequence` names the model effect.

  ⚠ 2026-09-23: was "792–797, 1712–1714", now "792–797, 1704, 1716, 1723, 1725, 1727" → HISTORY.md#fraction-share-citation-correction-2026-09-23
- [x] 2026-09-23: `Dmin` rounds under `Original` too, and so does `Di`/`Dj` ("## Setup
  plane", "`Dmin` rounds too; `Di`/`Dj` round too"), same method for both; positive and
  negative controls both green
  (`SetupPlaneTests.DefaultDminRoundsToTheSameBinary32ValueAsTheLowestFractionBound`,
  `.DefaultCellSizeRoundsToTheSameBinary32ValueAsDminSoTheLowestBoundBinsToCellOne`;
  controls: `HISTORY.md#dmin-di-dj-rounds-too-fused-2026-09-27`). The "Dok < Dmin" rate
  gap against the original is declared, not this fix's to close (mechanism owned by
  `src/Particle`; root `ACCEPTANCE.md`, the criterion "the `Original` accumulation
  kind reproduces…" lists it among the declared differences, link 1).

  ⚠ 2026-09-27: was two criteria for `Dmin` and `Di`/`Dj`, now one, fused →
  HISTORY.md#dmin-di-dj-criteria-fused-2026-10-01
- [x] 2026-09-25: `Binary32.ToNearestRepresentable` and `Binary32.Multiply` equal the
  hardware cast oracle `(double)(float)value` bit for bit over the whole `double`
  domain (D3 of the architecture audit of 2026-09-24: subnormal-/overflow-range
  doubles diverged 100% before the fix). **The binary32 rounding is total.** Ten
  million draws over every edge class, and one million `Multiply`-then-round draws,
  agree bit for bit after the fix (`Binary32EquivalenceTests`; mutation seen red).

  ⚠ 2026-09-26: was the fix's scope and mutation-proof narrative told in full, now the
  deciding figures alone → HISTORY.md#binary32-totality-criterion-narration-2026-09-26
- [x] 2026-09-28: four out-of-sample controls of the per-cycle plane, frozen before its
  code (the arbiter's amendment A3), each right at the arbiter's count and red on the
  named alternative (`tests/Statistics.Tests/PerCyclePlaneOutOfSampleControlsTests`):
  (i) `epsdokfr` on 295 of 295 archived outputs; (ii) every printed `epsx` cell on the
  2⁻²⁴ grid, 2016 of 2016; (iii) `epsalldok` on 293 of 293; (iv) since 2026-10-02,
  `da_coef` of `rps01`/`rps02` exactly the archive's 0.7284034 and 0.6019315 (red
  without the pocket-forming flags). Re-verified 2026-10-03 (A4).

  ⚠ 2026-10-18: was each control's rule, red side and counts told in full, now the
  tick, place and figures → HISTORY.md#out-of-sample-controls-narration-2026-10-18

  ⚠ 2026-10-01: was (ii)'s red side on the rate runs, now on the frozen pre-change
  runs, 0 of 30 → HISTORY.md#out-of-sample-controls-narration-2026-10-18

  ⚠ 2026-10-02: was "reported only, not gated", now gated (the old figures were
  artefacts of recovered inputs) → HISTORY.md#control-iv-reported-only-2026-10-02

- [x] 2026-10-18: Corrected, the seam (`BOOT.md`, "## Corrected model"): the old overloads
  forward `None` and are held by the existing ratchets and the run-level guard; under
  `Original` any set equals `None` bit for bit: `Prepare`, `AnalyticSizes` and
  `FractionLaw.Build` (49 files), `Probability` (4 cases), `Compute` (49 files),
  `MergeAndDescribe` (4 cases, totals too), red with that overload's coercion removed
  (`CorrectionSeamTests`; `Build`'s also needs its `Original` branch read after the
  flag). A structural test finds no `CyclePlaneRounding`, `CyclePlaneOrder` or
  `UnrolledSchedule` call in a block a `CorrectionSet` test guards, red on one planted;
  `CyclePlaneSiteMapTests` green (`CorrectedStatementStructureTests`).
- [x] 2026-10-18: Corrected, `ExactConstants`: on the same inputs `ZSS` (so λ) is the
  `None` value times 3.14/π and `mp` its value times `y^(1/3 − 0.3333)`, `y` the base,
  to 1e-12 relative on 49 files; red: ratio 1 under `None`, `mp` moving 8.6e-5 to
  1.15e-4. A `Legacy` test finds `3\.14\d*`, `12\.566\d*`, `0?\.333\d*` and the quotient
  `1/3.` (not after a letter, digit or point) in the four line-map ranges at exactly
  1704, 1716, 1014 and 1016 (twice) (`ExactConstantsTests`, red on each constant
  reverted; `ExactConstantsSitesTests`, red on a removed site).
- [x] 2026-10-18: Corrected, `DoksdConsistent`: one `JZZ = 2` fraction gives the closed
  form `(2/3)(u⁶ − l⁶)/(u⁴ − l⁴) − DOKM²` and a quadrature of the mass-weighted variance,
  also on every shipped `JZZ = 2` file, to 1e-12; P33 prints `Dok43sd` 112.66 µm, P2
  8.07 µm; red: `None`'s 75.45 µm and `NaN`; `JZZ ≠ 2` bit-identical to `None`
  (`DoksdConsistentTests`; red on `1/3` for `2/3` and on the flag read as another).
- [x] 2026-10-18: Corrected, `GdokleftOnce`: on constructed totals with an `SFR = 0`
  fraction across `b`, `None` exceeds the analytic `GGG·[FMDOK(k) + GDOK(1 − φ(b))]` by
  `GGG·GDOK·φ(b)` and the flag equals it, under both laws; `b` at or below the bound:
  bit-identical; PSAN02 `--sfr`, `Dmin` 20 µm, `Di` 10 µm: `None` minus the flag 0.0119,
  0.0175 of the oxidizer (`GdokleftOnceTests`, red on each mutation).
- [x] 2026-10-18: Corrected, `ZdoksmallCoarseShare` and `PdoksmallFirstCell`, through
  `pdoksmall`: for a `vdokstr` whole in cell 1, `AK2 = 2` and `gdokleft > 0`, where
  1021–1056 give `zdoksmall` ½ in cell 1 and 1 beyond before the scaling and `ddoksmall`
  ¼ and `1/(2k)`, `pdoksmall` is the closed form of 1047–1064, scaled by `GGG` under
  `None` and by `GGG − gdokleft` under the first flag, `pdoksmall[0]` 0 under `None` and
  the 1061 formula under the second; `gdokleft = 0`, cell 1 empty: bit-identical
  (`SmallParticlesCorrectedTests`, red on each mutation). Reported (P312's own mass CDF,
  `CorrectedCycleEvidenceTests`): `gdokleft` 0.122, a cell moves ≤ 21.6 %; `Dmin` 5 µm:
  `pdoksmall[0]` 0 → 0.144.

  ⚠ 2026-10-18: was 0 → 0.191 (`Vdokstr` began at the cell edge, not at `Dmin`), now 0 → 0.144 → HISTORY.md#pdoksmall-first-cell-figure-2026-10-18
- [x] 2026-10-18: Corrected, the declared limit of `PdoksmallFirstCell` against an integral of
  the size law (`PdoksmallFirstCellLimitTests`): the corrected cell is line 1031's spread share,
  to 1e-12; the model's own cell, the coarse mass in `[Dmin, Di/AK2)`, is 0 at 5 and 6 µm where
  the flag gives 0.144 and 0.121; at 3 µm `zdoksmall(1)` 0.109 against 0.0625, the cell 0.185
  against 0.176; red at the old `Vdokstr` start and on a halved first cell, 3 of 4 each.
- [x] 2026-10-18: Corrected, `VdokTotalOnCopy`: over two cycles `None` divides the
  constructed totals in place twice, the flag leaves them byte-identical and `DolM2`,
  `DolM3` equal their closed form over the undivided totals; cycle 1 equals `None`;
  P312, `KXX = 3`: moves from cycle 2 (×0.774, ×0.599; `VdokTotalOnCopyTests`). On constant
  totals with `VmkmTotal = Vsmkm`, `VdokTotal = Svd` (P312, three cycles) `DolM2 = DolM3 =
  DolM1` in every cycle under the flag, to 1e-12 (one product in other orders over `Ndok`
  terms), under `None` in cycle 1 only; red with the store back in place (3 of 7). Program
  (`P312-Cycles3`, eight distinct seeds `k·2¹⁶`, the page
  `tests/Simulation.Tests/Measurements/corrections-2026-10-18.md` at 8b29342): `Zkarm_cor`
  −2.960 ± 0.168 % of the `Original` mean against (1/q² + 1/q + 1)/3 = 1.320, q = 0.774.
  ⚠ 2026-10-18: was −2.944 ± 0.153 % on the first run's apparatus, now −2.960 ± 0.168 % → HISTORY.md#corrections-page-rerun-2026-10-18
- [x] 2026-10-18: Corrected, `CategoriesOnCopy`: the totals are byte-identical after a call
  and a second call equals the first, its report that of `None` on fresh totals, where
  `None`'s second call differs; through `Compute`, cycle 0 twice writes no total (the four
  cases of `categories.json`, `CategoriesOnCopyTests`; red with the copy removed: 6 facts).
  Reported (`CategoryFlagsOnOracleTotalsTests`, a synthetic two-cycle chain on the oracle's
  drawn totals): `DPRow` of cycle 1 moves in 4 of 22 cases; HPEPA3 unmoved, HMX −0.06 %,
  PSAN02n +5.6 % on `Qdokkarm`'s first row (`BOOT.md`, the row 911–958).
- [x] 2026-10-18: Corrected, `CategoriesLastRowRecomputed`: each reported row's `DOKP43` is the
  quotient of the sums over its merged rows and a second call from the final rows merges
  nothing, on the case file's cascade and a four-row one, `None` keeping the stale quotient
  (`CategoriesLastRowRecomputedTests`, red without the return, 5 of 8); 10 of 61 draws move, no reference.
- [x] 2026-10-18: Corrected, `CategoriesEmptyRowsMerged`: an empty middle or last row merges
  where `None` keeps it, all rows empty end at one, a `QDOKSS = 1` row is left alone
  (`CategoriesEmptyRowsMergedTests`, red on the read removed, 4 of 7, and on `QDOKSS <= 1`, 1).
  Reported (`CategoryFlagsOnOracleTotalsTests`): 13 of 61 draws move; HPEPA3 yes, PSAN02n, HMX no.
  Program runs, the page at 8b29342: `DPRow` −15.0 ± 0.4 % (inpt), −10.2 ± 0.2 % (PSAN02n)
  inside the `Corrected` model, the oracle chain blind to it (`BOOT.md`, row 869–871).
- [x] 2026-10-18: Corrected, the rows: each `fixed (<Flag>)` of `## Defects of the original` landed
  with its flag's code and these criteria's evidence; a reference's seed-zero effect is
  `Simulation`'s (`HISTORY.md#corrected-category-flags-every-formulation-2026-10-18`).
