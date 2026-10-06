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

  ⚠ 2026-10-02: was dated 2026-09-24, before the per-cycle plane changed the report
  under `Original` (2026-10-01); now the root's re-run of that day. ⚠ 2026-09-28: was
  "23 of 160 against 22 of 197, p = 0.124", stale; now cited (§8)

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

  ⚠ 2026-09-27: was "`Ddokmax` is excluded", now computed and asserted →
  HISTORY.md#ddokmax-excluded-2026-09-27

  ⚠ 2026-09-26: was the script's own `Dmax` provenance told in full, now the tick,
  place and figure alone → HISTORY.md#z11-zx-zss-criterion-narration-2026-09-26

  ⚠ 2026-10-01 (A2): `DOKM`/`DOKSD` under rule L; only PSAN02n's `Original` runs moved.
  Rule L is retired, 2026-10-03: the listing's schedules (`BOOT.md`, "## Setup plane").
- [x] 2026-10-03: every site of the per-cycle plane (771–1175, the hoisted block and
  line 378) is generated from the executable's listing, never typed
  (`classify-cycle-plane-listing.py` → `CyclePlane.listing.generated.txt`, over
  `fortran_source.py`; `CyclePlaneListingMapTests`, with seven pinned sites, one per
  kind the code follows). Reformulated: the 2026-10-01 criterion read the source under
  rule L (`classify-cycle-plane.py` → `CyclePlane.generated.txt`), retired 2026-10-03,
  nothing reading it; its verdicts the archive confirms are the generator's own right
  proof now (16 verdicts, `classify-cycle-plane-listing.py selftest`).
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

  ⚠ 2026-10-02: was "so row 771–1175 is `reproduced`", now `open` →
  HISTORY.md#row-771-1175-reproduced-then-open-2026-10-02
- [x] 2026-10-02: the controls of the register-lifetime row are frozen before any plane
  code, each a ratchet that goes red when its list changes in either direction
  (`tests/Statistics.Tests/BOOT.md`, "## Acceptance criteria", the same date). C1,
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
- [x] 2026-10-02: stage 1 of the plan above (W0 documents, W1 the excerpt, W2 the map,
  the reader and the generated table) is in the tree and drives no code: the excerpt is
  the executable's bytes (`CyclePlaneListingTests`), the map holds against it and
  `CyclePlane.listing.generated.txt` (193 sites) reproduces byte for byte
  (`CyclePlaneListingMapTests`, `tests/Statistics.Tests/BOOT.md`, the same date). Read
  against the arbiter's scratch inventory, every cited address lies in a row of the
  Fortran lines it names and no rule of the inventory is contradicted; the table is
  finer in labels (REAL*8 sums carry B`n` with `rounds = no` where it says W; a stored
  named home is S where it says T) and adds sites (lines 979, 1071 and 1140-1142
  eliminated, `gk0`/`gk1` unread, REAL*8 temporaries at 856).
- [x] 2026-10-03: A1, C1 (`LegacyDaCoefReproductionTests`, `LegacyDaCoef.approved.txt`):
  22 of 22 formulations print the original's `da_coef`; the approved file moved from
  19 of 22 (HPEPA2, P777 and CSPX01 one print unit below) in the same commit as the
  plane's rewrite, the count the design predicted in advance (S2 did not fire).
- [x] 2026-10-03: A2, the listing oracle (`tests/Fixtures/cycle_plane_oracle.py`, an x87
  interpreter over the excerpt's bytes; `tests/Statistics.Tests/ListingOracleTests`):
  under `Original`, `Compute` equals it bit for bit on every case of
  `cases/statistics/cycle_plane_oracle.json`, and `Setup.Prepare` equals its pre-loop
  run (line 378's λ, `DOKM`, `DOKSD`): both ratchets are empty,
  `ListingOracle.approved.txt` from 463 lines and `ListingOracleSetup.approved.txt` from
  19, which is the proof r4 seen once, the oracle red on the port of 2026-10-02 at the
  isolated sites. Every site of the table is covered by an isolating case or written
  `unreached`, and each `unreached` one has a control or the table's own reason for
  being exact (`formulas_statistics.py`, `_check_unreached_sites_are_controlled`). The
  right proofs k1–k4 and the red proofs r1–r3 and r5 are `tests/Fixtures/BOOT.md`'s. Not
  seen red: a product order swapped at a call site (five tried: 805, 806, MD4, DD4,
  1089) moves no bit of any case, a 1e-16 difference being absorbed by the binary32
  store that follows (mutations 2026-10-03, `tests/Statistics.Tests/BOOT.md`); the forms
  themselves are held at `double` by `PlaneFormsTests`, each but `Square`, an equivalent
  form (`pow(y, 2)` is `y·y` when pow is correctly rounded). The fixture holds 26
  cases: 19 drawn, three pinned for `DOKM`'s schedule (`BK10`, `KB397`, `AK157a`) and
  four for `ZS`, `ZX` and the `JZZ = 2` loop (A10, A12); 58 isolated sites, 18
  `unreached`; the setup ratchet is empty → HISTORY.md#oracle-fixture-cases-2026-10-03
- [x] 2026-10-03: A3, C2 (`PathIdenticalPairsReportTests`,
  `PathIdenticalPairs.approved.txt`, `CyclePlanePairsTieTests`): the port side of the
  six pairs regenerated alone by `cycle_plane_pairs.py generate` on the stage 3 build,
  every integer-printed cell still equal on all six (S4 did not fire). The original's
  files and provenance entries are kept as committed: a second run of the executable
  prints other values in its own `pdoksmall` cells below 1e-30 (HMX 0.196E-37 then
  0.197E-37, all six pairs), the declared cells of the dead row, so the original's side
  is not rewritten. The list of differing cells stays at 551, four of them moved
  (→ HISTORY.md#c2-four-moved-cells-2026-10-03). Classes by the design's attribution of
  2026-10-02, not a per-cell measurement: P, the print plane (`ConditionBreaking(k)`,
  `MediumJammedParticleFraction`, `MediumNumberOfBridges`), 2, 3, 2, 5, 2 and 1 cells;
  I, every other, fed by the attempt plane's REAL*4 inputs `Dr`, `Db`, `RK`, 58, 42, 31,
  196, 144 and 65; D, declared, as above; the four moved cells are I, two of them now
  zero. No cell was required to reach zero and none outside the set moved.
- [x] 2026-10-03: A4, C3: 295 of 295, 2016 of 2016, 293 of 293, control (iv) and every
  `Binary64` snapshot, unchanged bit for bit (`PerCyclePlaneOutOfSampleControlsTests`,
  `CycleStatisticsFormulaTests` and every snapshot of the fast set; S3 and S7 did not
  fire).
- [x] 2026-10-03: A5, the 39 seed-0 cases (`tests/Simulation.Tests/
  CyclePlaneSeedZeroControlsTests`, `rate-runs/*/Original/Original/0.m.txt`, not
  regenerated): green, `da_coef` and every counter unmoved, so S5 did not fire and the
  rate runs stay as they were until stage 4.
- [x] 2026-10-03: A6, the rate (`tests/Harness.Tests/RateCriterionTests`,
  `SetCriterionTests`, 59 cases): the 160 `Original` runs, `rate-table.json` and the
  seed-0 snapshot regenerated (`dotnet run -c Release --project tests/RateTableTool --
  rate-table`, 16 m 41 s, then `seed-zero-snapshot`), `Binary64` untouched. 147 of the
  160 `Original` runs differ in bytes from before, 13 do not, and the failing cells of
  every one of the 160 are the same: 17 of 160 against the original's 15 of 197,
  p = 0.102792, no excess at `α`; the positive control on `Binary64` rejects on HPEPA3
  and HMX at p = 1.63e-36 each, the 160 `Binary64` runs and their ten snapshot hashes
  byte-identical (S3, S6 did not fire). In the snapshot nine of ten `Original` hashes
  moved (P33 `Original` did not). The seed-0 controls (A5) hold on the regenerated runs,
  39 of 39 (S5 did not fire).
- [x] 2026-10-03: A7, one positive control per site kind the table leaves `unreached` in
  the oracle (22 sites in the fixture of cdc6237, 18 in the one regenerated 2026-10-03):
  16 cases `control_NN_*` of `cycle_statistics.json`, 35 controls in `CONTROLS` of
  `formulas_statistics.py` (a store, a temporary, a sum stored every pass, B6 and C6 at
  trip counts 5, 6 and 13, X, H at 817 and 976), each a seeded body whose store or
  schedule, flipped in the twin, moves a bit of the Original report (`generate` refuses
  a control that moves nothing); the 22 sites (and the 18) are each controlled (11 and
  7) or exact by the table (11 and 11) (`order` of `0.0` or `real(i)`). Red on the C# of
  cdc6237: all 16 control cases, `seeded_rounding_reaches_every_member`, the cycle
  cases, both warning cases, two small-particles cases and the setup plane's five
  formulations, 28 of 82 tests, with the new case files and the old code; `Binary64`
  cases pass on both. Line 378's λ is `ListingOracleSetup.approved.txt`, empty.
- [ ] A8 (the arbiter's F1 of 2026-10-03): the loops of Fortran 379–406 are generated
  from the listing, not typed: the map's scope covers 0x408ADC–0x408FA6 and
  `Setup.AnalyticSizes` and `formulas_statistics.py` follow rows of the table. It lifts
  the declared deviation of `BOOT.md`, "## Setup plane"; until then the unroll factors
  and stores are typed and held by the pins, the survey and the drawn loops (A12).
  Costed 2026-10-03: the walker is no obstacle (scope extended: 130 problems, 128
  float events in no row, none from the walk); about forty rows, the temporaries'
  cells, two new site kinds, both consumers following the table, pins for residues 0, 3.
- [x] 2026-10-03: A9 (F3), the call-site order of the plane's products is held
  structurally (`tests/Statistics.Tests/CyclePlaneOrderSiteTests`; `API.md`, "##
  Cycle"): each `order.` call cites its Fortran lines, and its factors' association,
  read through the named form's `Original` expression, is a sub-expression of the
  `order` text at a cited line, a commutation one order. Green on the 23 calls; red at
  its own line on eleven edits, right on three commutations
  (`tests/Statistics.Tests/BOOT.md`, "## Mutations").
- [x] 2026-10-03: A10 (F4), `PARAM` runs in the oracle: the excerpt holds its bytes
  (0x4181A5–0x418577), the machine executes the call (SIZE, the next routine, is a stub
  nothing reads) and `ZSS`, `ZX`, `Z11` are the executable's, compared in
  `ListingOracleTests` and `PreloopSurveyTests` (both approved files empty). Over the 49
  shipped files `ZSS` per addition equals the executable on 49 (one rounding after the
  loop parts on 16, `C166` the sixteenth, since the twin's `.dat` reader was fixed: the
  old one left them equal); `ZX` parted by 1–3 ulp of double on 33 and `Z11` on 16
  because `FractionLaw.Build` formed `l·l·u·u` left to right where the executable forms
  `(l·l)·(u·u)`; under `Original` it is the executable's on 49 of 49. Of the references
  `HMX` and `HPEPA3` moved (1–2 ulp), `inpt`, `P33`, `PSAN02n` not; the 160 `Original`
  rate runs, the seed-0 snapshot, C2 and the 39 controls did not move and `Binary64` is
  bit-identical (64 of 64 runs of `HMX` and `HPEPA3` byte-equal on a scratch tree). Pins
  `HMX`, `P2a` (`ZS`) and `HPEPA3` (`ZX`) tell the executable's rule from the other.
- [x] 2026-10-03: A11 (F5), (a) the fixture's provenance digests
  `formulas_statistics.py` (`cycle_plane_oracle.py restamp`: the 22 cases byte for byte,
  `verify` green; `ListingOracleTests.TheFixtureIsOfTheCommittedBytes`, red until
  restamped); (b) `.TheTwinThatSuppliesTheInjectedSetupReproducesItsCaseFiles` runs the
  twin's `verify` in the fast set, 0.7 s, red on `DOKM_UNROLL = 6`; (c) `QKS1` is one
  expression in the oracle's injection and in `Compute` only while `QKSS` is a binary32
  integer, up to 2^24 (`Qks1NormalisationTests`: equal on every cycle of the fixture,
  `QKSS` at most 4e5, red on the divisor `QKSS + 1`; equal at 2^24, one unit apart at
  2^24 + 1, red on `Compute` dividing by `f32(QKSS)`). The bound is no excuse: 110 of
  the 175 archived outputs print a larger `QKSS`, among the references HMX and HPEPA3.
  `Compute` is unchanged; the finding is the `open` row `718, 963–996` and A14.

  ⚠ 2026-10-03: was "one expression only while `QKSS` is a binary32 integer", now at
  every count (A14).
- [x] 2026-10-03: A14 (from A11), the divisor of line 718 is the exact count. The
  excerpt holds the loop's bytes (0x40B7FA–0x40B8AF, read, not run): `QKSS` is
  `INTEGER*8`, `fild qword` loads it exactly (0x40B823), each cell is `fild qword; fdiv;
  fstp dword`, so only the quotient rounds, as in `Pockets`. The oracle's injection is
  `f32(q / QKSS)` and equals `Compute` on every cycle (`Qks1NormalisationTests`, the 2^24
  and 2^24 + 1 pair kept against a divisor rounded to binary32). Nothing moves; the row
  `718, 963–996` is `reproduced`.
- [x] 2026-10-03: A12, the `JZZ = 2` setup loop is the executable's. From
  0x408CFE–0x408F8E: unrolled four-fold; `u − l` is a REAL*4 temporary (0x408D18,
  0x408F10) in pass 1 of a block and in every remainder pass, a register in passes 2–4;
  `l**5` is stored to a REAL*4 temporary every pass (0x408D29), `u**5` is `u·(u²·u²)`;
  `DOK4`, `DOK3` stored every pass; `DOKSD` stored after passes 1–3 of a block
  (0x408D7B), carried after the fourth and through the remainder. `Setup.AnalyticSizes`
  (`UnrolledSchedule(n, 4)`) and the twin's `jzz2_sums` follow it; the survey's approved
  file is empty (`CSPX04` agrees). Right: the six shipped `JZZ = 2` files and 120 drawn
  loops of 1–16 fractions, zero differing bits, in C# by
  `PreloopSurveyTests.AnalyticSizesReproducesTheExecutablesJzz2LoopOnDrawnLoops`
  (`preloop_survey.json`'s `jzz2Controls`). Red: the earlier rule parts on 91 and 121 of
  300 drawn loops; the width rounded every pass, never rounded, `l**5` unstored, `DOKSD`
  unstored part on 8, 9, 27, 14 of 120, and the same edits of `Setup.AnalyticSizes` turn
  that test red (the width never rounded is green on the 49 files alone).
- [x] 2026-10-03: A13 (the arbiter's suggestion), the pre-loop survey of the 49 shipped
  files is a ratchet (`PreloopSurveyTests`, `PreloopSurvey.approved.txt`; the oracle's
  pre-loop over every `.dat`, `tests/Fixtures/preloop_survey.py generate` into
  `cases/statistics/preloop_survey.json`, its `verify` the one `Long` test):
  `Setup.Prepare` under `Original` against the executed `zss`, `zx`, `z11`, `lambda`,
  `DOKM`, `DOKSD` and `GGG` differed on `CSPX04` alone until A12; the approved file is
  empty now. Red: the five-fold schedule set to rule L gives `AK157a`, `BK10`,
  `KB397` and `C166`; one unit moved in memory gives its file. Right: today's code.

  ⚠ 2026-10-03: the first run gave `C166` besides `CSPX04`, the twin's reader at fault →
  HISTORY.md#survey-c166-reader-2026-10-03
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
  the source and corrected: the sum and division live at lines 1723/1725/1727, not
  1712–1714; `The port` is set to `reproduced` (under `Original`; `Binary64`
  unaffected), and `Consequence` now names the model effect the positive controls above
  measure, beside the pre-existing cosmetic `epsdokfr` reading.

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
- [x] 2026-09-28: the arbiter's per-cycle-plane verdict of 2026-09-28 (amendment A3)
  freezes four out-of-sample controls before any per-cycle
  rounding code is written, each proven right at the arbiter's own count and red on the
  named alternative (`tests/Statistics.Tests/PerCyclePlaneOutOfSampleControlsTests`):
  (i) the Fortran 795-796 rule (nothing rounded before 796's own store) reproduces
  `epsdokfr` on all 295 archived outputs of the five reference formulations, red on
  "795's store rounded" (HMX 9 of 49, HPEPA3 14 of 98 consistent); (ii) R1 at 771-783
  puts every printed `epsx` cell on the 2⁻²⁴ grid, 2016 of 2016 over 336 archived and
  legacy outputs, red on today's unrounded port (0 of 960); (iii) rule L's once-rounded
  `DOKM` reproduces `epsalldok` on all 293 archived outputs, red on today's
  per-addition `DOKM` (PSAN02n 15 of 49) — the mechanism A2 below fixes.

  ⚠ 2026-10-01: (ii)'s red side read the rate runs, which now carry R1 (960 of 960 on
  the grid); it reads the frozen pre-change runs instead, 0 of 30
  (`.Control2EpsxCellsAreOnTheGridInThePortsRunsAndOffItBeforeTheChange`).

  Control (iv), `da_coef` of the two legacy runs `rps01`/`rps02`, gates since
  2026-10-02: the plane, run on the shipped `psan01.dat` and `psan02.dat` (found by the
  outputs' own `Input filename`, read with their pocket-forming flags) and the printed
  `fmdok[0]` (empty) as the reachable cell, prints exactly the archive's 0.7284034 and
  0.6019315 (`PerCyclePlaneOutOfSampleControlsTests`, `.Control4DaCoefOfTheLegacyPair`
  `ReproducesTheArchivedPrint`). Red on the same run without the flags
  (`.Control4IsRedWhenThePocketFormingFlagsAreNotRead`, both files), with 1016's store
  unrounded (`rps02`) and with the folds of 1014–1016 off (both files), each reverted.
  `eta` is the menu default 0, not printed by either.

  ⚠ 2026-10-02: was "reported only, not gated", now gated (the old figures were
  artefacts of recovered inputs) → HISTORY.md#control-iv-reported-only-2026-10-02
