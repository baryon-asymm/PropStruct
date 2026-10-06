# BOOT.md — Statistics.Tests

## Purpose

The definition of what "Statistics is ready" means.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | `Setup.Prepare`/`CompleteEchoes` on the five reference formulations, and round-tripped through `Particle.SizeLaw.Sample` | printed setup quantities of the reference `results.m`; the hand-off contract for the round trip | ✅ |
| L0 | preconditions | one constructed violation per status | ✅ |
| L1 | `Categories.MergeAndDescribe` on constructed totals: merge pass, shift, last-row merge, `QDOKSS = 0`, in-place rewrites | values computed from the Fortran formulas by a script in `tests/Fixtures` | ✅ |
| L1 | `SmallParticles.Probability`/`MaxSize` on constructed totals: the `zdoksmall > 1e-5` guard, the monotone clamp, the first-crossing threshold | values computed from the Fortran formulas by a script in `tests/Fixtures` | ✅ |
| L1 | `CycleStatistics.Compute` on a constructed cycle: cycle 0 vs cycle ≥ 1 field gating, the empty `Fmkarm2` distribution, the in-place `VdokTotal`/`VdokTotal2` rewrite, `DolM1` by the Fortran formula, an all-empty pocket histogram | hand-derived identities of the constructed inputs (the category and small-particles halves of `Compute` are their own rows above) | ✅ |
| L1 | `CycleStatistics.Compute`'s seven members (`GeneratorAccuracy`, `OxidizerSizes`, `Pockets`, `Matrix`, `CorrectedPockets`, `MassFractions`, `Convergence`) on two small constructed cycles (0 and 1) and three seeded ones, under both kinds: every report value, `nextPdoksmall`, `nextDmaxxx` and the rewritten totals | values computed from the Fortran formulas and `CyclePlane.listing.generated.txt` by a script in `tests/Fixtures`, written without reading `CycleStatistics.cs`; its association under `Binary64` is `src/Statistics/API.md`'s, read off `Compute`, under `Original` the table's | ✅ |
| L1 | line-map coverage | the list of executable lines generated from the source | ✅ |
| L1 | the setup plane's site classification and the shared source reader | `classify-setup-plane.py` and `fortran_source.py`, regenerated and self-tested; the roles of `dokm`, `doksd`, `dok4`, `dok3` and `zs` pinned | ✅ |
| L1 | the plane's forms: the order of its products and the pass schedule of its unrolled sums; and the call sites' form and factor order | `cases/statistics/plane_forms.json`, read off the table's `order` text and generated rule, bit for bit at `double`; `CyclePlaneOrderSiteTests`, structurally, over `CyclePlaneSiteMapTests`' machinery | ✅ |
| L1 | the listing excerpt of the executable's per-cycle plane | the executable's bytes through its section table (`tests/Fixtures`, "## Cycle-plane listing"), red on an edited byte | ✅ |
| L1 | the address map of the per-cycle plane and the table generated from it (`src/Statistics`, "## Cycle listing map and table") | the excerpt: six map checks, the generator's `verify` byte for byte, its archive-confirmed rules, red on a broken map and a broken listing | ✅ |
| L1 | `Compute` and `Setup.Prepare` under `Original` over constructed totals, and `Setup.Prepare` over the 49 shipped formulations | the listing oracle (`tests/Fixtures`, "## Listing oracle"): the executable's own bytes executed, bit for bit; every approved ratchet is empty since 2026-10-03, the survey's (`PreloopSurveyTests`) included | ✅ |
| L2 | `Compute` over the totals of a reference-mode run of HPEPA3 | the reference `results.m`, statistical criterion | ⏳ |

⚠ 2026-09-18: was one L1 row for `Compute` on constructed totals against a script, now split in two
once coded, `Categories` scripted and the rest hand-derived → HISTORY.md#l1-row-split-2026-09-18

⚠ 2026-10-02: was "not a second, independent formula-script transcription" of the seven
remaining pieces, now one, with its own L1 row (`CycleStatisticsFormulaTests`).

⚠ 2026-09-18 (audit): was `SmallParticles` inside the row of the other seven pieces, now its own
scripted L1 row → HISTORY.md#small-particles-row-split-2026-09-18

## Invariants

- Expected values come from `tests/Fixtures`, never typed into a test.
- L0 and L1 compare bit for bit where the formula script evaluates in `double` in the
  Fortran order, terms associated as `src/Statistics/API.md` ("## Cycle") fixes,
  otherwise at a tolerance recorded with the case.

  ⚠ 2026-10-02: was "in the Fortran order" alone; the categories cases since
  2026-09-18 and the cycle cases form the cell centre first. Still bit for bit.
- `ListingOracleTests` types no expected value and applies no rule of its own: it fills
  the totals through one name-to-slot binding checked against the map both ways, runs
  `Compute`, and compares every mapped output bit for bit (decided 2026-10-02).
- The L2 run is marked long and excluded from the fast set.

## Dependencies

- [Statistics](../../src/Statistics/API.md) — what is being checked.
- [Particle](../../src/Particle/API.md) — layouts to build totals.
- [Input](../../src/Input/API.md) — `Formulation`, `Length`, `ModelParameters`,
  `OxidizerFraction` and `DatFile`, the inputs every setup case is built from.

  ⚠ 2026-09-20: was a section that did not declare `Input` while the tests used five of its types, now declared → HISTORY.md#input-dependency-undeclared-2026-09-20
- [Harness](../Harness/API.md) — statistical comparison, repository paths,
  `PythonScript`.
- [Fixtures](../Fixtures/API.md) — formulations, cases, reference outputs.
- [legacy](../../tools/legacy/API.md) — the manifest `original.sha256`, which the
  facts of `API.md` ## Categories read.

Outside the tree: xunit.

## Constraints

- Part of the default test command.
- The L2 run needs the reference-mode driver of `Execution`; the row stays ⏳ until it
  exists.

## Acceptance criteria

- [x] Every row of the levels table but L2 is green, with a date and the names of the
      tests. (2026-09-18: `SetupTests.PrepareMatchesThePrintedSetupQuantitiesOfItsReferenceFormulation`
      (5 formulations), `SetupTests.PrepareAndCompleteEchoesRoundTripThroughSizeLawSample`,
      `SetupTests.SizesNdokMatchesThePrintedCategoryRowLengthOfItsArchivedFormulation`
      (5 formulations), `SetupTests.PrepareRejects*` (13 tests over 9 statuses —
      `InvalidCoefficients` split into one test per clause, `InvalidNnWindow` into one
      test per clause (2026-09-20); `NoActiveFraction` is
      proven unreachable by argument, not by a test, `src/Statistics/BOOT.md`'s own
      `FractionLaw.TryTailDraw` doc comment) and
      `SetupTests.PrepareAcceptsAValidFormulation` for L0 (extended 2026-09-24 with
      `.PrepareTreatsAnyNonzeroPocketFormingFlagAsForming`,
      `.PrepareRejectsInvalidPocketFormingFractionCount`,
      `.SizesUsesTheUnroundedProductForNkarm`, `.SizesUsesTheUnroundedProductForNcat`,
      `.SizesNkarmAndNcatOfC166AreUnaffectedByTheProductRoundingFix` and
      `Binary32Tests.TruncatedQuotientOfProductLeavesTheProductUnroundedAtAnIntegerBoundary`,
      D7 and S-2, "## Mutations" below);
      `CategoriesTests.MergeAndDescribeMatchesTheFormulaScript` (4 cases),
      `.MergeAndDescribeUsesBinary32ArithmeticForDpRow` and
      `.MergeAndDescribeReportsStatusWhenDpRowExceedsNcat` for the category half of L1;
      `SmallParticlesTests.ProbabilityMatchesTheFormulaScript` (3 cases, the third at the
      whole-fraction count's own binary32 boundary)/`MaxSizeMatchesTheFormulaScript`
      (2 cases) and `.ProbabilityUsesBinary32ArithmeticForTheWholeFractionCount` for the
      small-particles half;
      `CycleStatisticsTests.*` (7 tests) for the rest of L1; 2026-10-02:
      `CycleStatisticsFormulaTests.ComputeMatchesTheFormulaScript` (21 cases of
      `cases/statistics/cycle_statistics.json`, both kinds, 16 of them the positive
      controls of 2026-10-03) for its seven members;
      `LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges` for the
      line-map row; 2026-10-03: `SetupPlaneClassificationGeneratorTests` for the setup
      classification row (`CyclePlaneClassificationGeneratorTests` retired with the
      source-read table it guarded, its reader's self-test moving there) and
      `PlaneFormsTests` for the forms row. L2 stays ⏳: it needs `Execution`'s
      reference-mode driver, which does not exist yet.)
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13).
      (2026-09-18, see "## Mutations" below.)
- [x] 2026-09-25: `Binary32.ToNearestRepresentable` and `Binary32.Multiply` (followed by
      rounding) equal the hardware cast `(double)(float)value` over the whole `double`
      domain, closing D3 of the architecture audit of 2026-09-24 (`src/Statistics/BOOT.md`,
      "## Acceptance criteria", the same date). `Binary32EquivalenceTests`: every named
      edge class (zeros, the smallest/largest binary32 subnormals, the normal/subnormal
      boundary, ties at the subnormal, normal and overflow boundaries proven both ways,
      the largest finite float, outright overflow, `NaN`/infinities, negative mirrors),
      ten million draws spread over the whole finite normal `double` exponent range
      (`Category=Long`), one million `Multiply`-then-round draws against a true
      single-precision multiply (`Category=Long`), and two hand-derived positive controls
      independent of the oracle. Proven non-degenerate against the current source (see
      "## Mutations" below, the updated entry).
- [x] 2026-10-01: `da_coef` takes one value in every output of the original per
      reference formulation; `CycleReport.Mp` prints it under both kinds on all five
      (`DaCoefTests`; `src/Statistics/BOOT.md`, row 771–1175).

      ⚠ 2026-10-01: was "under `Original` on four, PSAN02n one print unit below"
      (2026-09-27), now all five, the per-cycle plane reproduced
- [x] 2026-10-02: the controls of the register-lifetime row (`src/Statistics/BOOT.md`,
      "## Defects of the original", rows 771–1175) are frozen before any plane code, as
      ratchets that go red when what they list changes in either direction. C1
      (`LegacyDaCoefReproductionTests`, `LegacyDaCoef.approved.txt`): the constructed
      cycle's `da_coef` against every archived output of every formulation whose input
      the archive ships, found by the output's `Input filename`: 22 formulations, 19
      match, HPEPA2, P777 and CSPX01 miss by one unit of the seventh digit (0.7693585
      against 0.7693586, 0.6313831 against 0.6313832, 0.7778412 against 0.7778413),
      `p777out1` and `r2` left out by rule (below). C2 (`PathIdenticalPairsReportTests`,
      `PathIdenticalPairs.approved.txt`): every cell whose printed token differs between
      the original and the port in the six path-identical pairs of
      `tests/Fixtures/cycle-plane-pairs`: 59 (HMX), 45 (HPEPA3), 32 (P33), 203 (PSAN01),
      146 (PSAN02n) and 66 (inpt) cells, besides those declared; every integer-printed
      cell agrees in all six. C3: the controls of 2026-09-28 and 2026-10-01 are
      unchanged and green (295 of 295, 2016 of 2016, 293 of 293, the 39 seed-0 cases,
      `Binary64` bit for bit in `CycleStatisticsFormulaTests`). `p777out1` prints the
      plane's own value at `eta = 0.5` (`P777Out1IsTheRunOfAnotherEta`): its header and
      sample path are those of the other P777 outputs and no output prints `eta`, so it
      is left out of C1, not a miss; `r2` prints `Plot2 = 1800` where `hpepa2.dat` has
      1900.
- [x] 2026-10-02: the listing excerpt is the executable's bytes
      (`CyclePlaneListingTests`: `verify` on the committed file, `selftest` red on an
      edited byte, a moved address, a dropped instruction and a stale digest; seen red
      once on the committed file itself, 0040BDB8).
- [x] 2026-10-02: the address map holds against the excerpt and the generated table
      reproduces from it (`CyclePlaneListingMapTests`: `check` 0 problems, `verify` byte
      for byte, `selftest` red on 17 breaks of the map and the excerpt,
      and on two breaks of the listing the generator reads: line 772's `fsub`
      reversed moves its `order` to `(0.5 - xsr0)`, line 772's home reload replaced by a
      register use moves its read to `xsr0:register`). The right proof: the generator
      re-derives all 16 (line, target) verdicts of the old table at 771-784 and 795-796
      and the reads 772 `xsr0:home`, 796 `epsalldok:register`. Stage 1 only: the old
      table still drives `Compute`.
- [x] 2026-10-03: `ListingOracleTests`: under `Original`, `Compute` equals the listing
      oracle bit for bit on every case and `Setup.Prepare` its pre-loop run, each site
      covered or written `unreached` (`src/Statistics/ACCEPTANCE.md`, A2). The setup
      inputs and the name-to-slot binding are asserted first. Both approved ratchets
      are empty, from 463 and 19 lines on the port of 2026-10-02, the red the test
      showed until the plane followed the listing.
- [x] 2026-10-03: C1 is 22 of 22 (`LegacyDaCoef.approved.txt`: CSPX01, HPEPA2 and P777
      moved from `miss` to `match`, in the commit that moved the plane).
- [x] 2026-10-03: `PlaneFormsTests`: every Original form of `CyclePlaneOrder` equals the
      value read off a site's `order` text over 24 generated operand tuples, and every
      pass decision of `UnrolledSchedule` for blocks of 5 to 9 the schedule rule over
      trip counts below, at and above the block (10 cases, `plane_forms.json`). Linear
      and Quadratic weights are quotients; `generate` refuses a form (`Square` apart)
      with an alternative association that fewer than 1 tuple in 8 tells apart. Red on
      `Linear`'s Original branch as `x·(a·b)` and as `x·b·a`, each reverted; the file
      of before this date was green on the first (2026-10-03).
- [x] 2026-10-03: C2 re-approved on the regenerated port side of the six pairs: 60, 45,
      33, 201, 146 and 66 cells, 551 as before, four cells moved, all in the set the
      plane can move (`src/Statistics/ACCEPTANCE.md`, A3, has the cells and classes).
- [x] 2026-10-03: `CyclePlaneOrderSiteTests` (23 calls green, 11 reds and 3 right
      proofs, "## Mutations"); `PreloopSurveyTests` (the ratchet, empty over the 49
      files, zss, zx and z11 compared, red on rule L and on one unit moved); `Qks1NormalisationTests` (the
      fixture's cycles, the pair at 2^24 and 2^24 + 1); and the oracle's setup and twin
      digests in `ListingOracleTests` (`src/Statistics/ACCEPTANCE.md`, A9, A11, A13).

- [x] The facts of `API.md` ## Categories after the delivery are so categorised (S2,
      2026-10-04): 12 `Legacy` facts, 12 of 12 green with the variable set (11 in 1 m 24
      s, `TheOracleFixtureReproducesFromTheExecutable` alone in 7 m 55 s) and 0 of 12
      with it unset; `TheFixtureIsOfTheCommittedBytes` and
      `TheSurveyIsOfTheCommittedBytes` red on one character of the manifest's executable
      hash (2 of 2) and green reverted; `LineMapCoverageTests` red on line 785 added to
      the list (in `771–1175` and in no row), naming it by number, green reverted; 324
      cases of the fast set with the original absent.

## Mutations

Recorded per AGENTS.md §13 ("every check must be proven non-degenerate once"), one
entry per check, not per test method. Every entry below names an actual source-code
edit that was run red then reverted, not only an input varied across an unmodified
implementation (⚠ below).

- `SetupTests.PrepareMatchesThePrintedSetupQuantitiesOfItsReferenceFormulation` /
  `SetupTests.SizesNdokMatchesThePrintedCategoryRowLengthOfItsArchivedFormulation`:
  proven by construction, not a code mutation — `SizesNdokOfC166WouldBeSeventyTwoWithoutBinary32Rounding`
  computes C166's `Ndok` both with and without binary32 rounding from the same inputs
  and asserts they differ (71 vs. 72), so the printed-length check is provably
  sensitive to the rounding it exists to verify.
- `SetupTests.PrepareRejectsInvalidCoefficientsWhenAk2IsNotAboveOne` (representative of
  the eight `PrepareRejects*` checks): `Setup.cs`'s `!(ak2 > 1.0)` clause weakened to
  `!(ak2 > 0.0)` made the test fail (`Ak2 = 1.0` no longer rejected); reverted, green
  again. The other seven `PrepareRejects*` tests are not independently mutation-proven
  by a code edit — each is a distinct branch of the same `if`, and one representative
  proof stands for the pattern, not for every branch individually.
- `CategoriesTests.MergeAndDescribeMatchesTheFormulaScript` and
  `CategoriesTests.MergeRowAddsRatherThanOverwritesTheSourceRow`: proven red by
  changing `Categories.MergeRow`'s two `+=` (Dokp41/Dokp31) to `=` and running
  `dotnet test --filter FullyQualifiedName~CategoriesTests` — all four formula-script
  cases and the dedicated mutation test failed (`single_merge_pass_two_rows`'s
  `Dokp43[0]` moved from 6.0 to 7.0, matching an overwrite of the source row instead of
  an add); reverted, green again. The Qdoks integer merge (`+=` on the array) was left
  unmutated since every formula-script case already depends on it (a plain `=` there
  would already fail `single_merge_pass_two_rows`'s own `QdoksAfter` assertion).
- `SmallParticlesTests.ProbabilityClampsANonMonotoneTail` /
  `ProbabilityMatchesTheFormulaScript`: `SmallParticles.cs`'s clamp condition
  `if (pdoksmall[k] < pdoksmall[k - 1])` replaced with `if (true)` (cascading
  `pdoksmall(1) = 0` forward into every later cell) failed two of the five
  `SmallParticlesTests` cases; reverted, green again.
- `SmallParticlesTests.MaxSizeStopsAtTheFirstCrossing` / `MaxSizeMatchesTheFormulaScript`:
  `SmallParticles.cs`'s `break;` after the threshold is first reached removed (letting
  the loop keep overwriting `dmaxxx` at every later crossing instead of stopping at the
  first) failed `"threshold_reached_at_kilo_two"`'s own case (`Dmaxxx` moved from `2.0`
  to `4.0`); reverted, green again.
- `LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`: proven
  red by the check itself while writing this node — the line map's
  `FractionLaw.MaxBaseSize` row first read `377, 1735–1756`, one line short of
  `PARAM`'s own `END` at 1757; the test failed with `1757: END` until the row was
  widened to `377, 1735–1757`.
- `CycleStatisticsTests.ComputeLeavesCycleOnlyFieldsNaNInCycleZero`: `CycleStatistics.cs`'s
  `if (cycleIndex >= 1)` weakened to `if (cycleIndex >= 0)` made the test fail (the
  cycle-only fields stopped being `NaN` in cycle 0); reverted, green again. The
  sibling tests `ComputeFillsCycleOnlyFieldsInCycleOne` /
  `ComputeRewritesVdokTotalInPlaceForCyclesAtLeastOne` /
  `ComputeLeavesVdokTotalUnrewrittenInCycleZero` are not independently mutation-proven
  by a code edit; they assert the opposite outcome of the same gate on the same
  constructed scenario, which the one proof above already exercises.
- `CycleStatisticsTests.ComputeGivesNaNPocketHistogramWhenQksIsAllZero`:
  `CycleStatistics.cs`'s `Pockets` re-fitted with the `qkssTotal == 0 ? 0.0 : ...` guard
  the audit of this node found and removed made the test fail (`Qks1` cells came back
  `0.0` instead of `NaN`); reverted, green again.
- `Binary32Tests.ToNearestRepresentableMatchesTheFloatCastOracle`: `Binary32.cs`'s
  ties-to-even condition `remainder == HalfDroppedBit && (keep & 1L) != 0` widened to
  `remainder == HalfDroppedBit` (always rounding a tie up, i.e. round-half-up instead
  of round-half-even) failed exactly the generated tie operand whose kept mantissa was
  already even (the odd one rounds up either way, so it stayed green); reverted, green
  again. This is also the non-degeneracy proof for the tie-at-bit-29 operands
  themselves: without them, no generated operand ever lands on the tie branch and this
  mutation would have passed unnoticed.

  ⚠ 2026-09-25: was a mutation entry written against the old locals of `ToNearestRepresentable`, now re-run on the rewritten code with the same result, the proof standing → HISTORY.md#to-nearest-representable-rerun-2026-09-25
- `CycleStatisticsTests.ComputesDolM1ByTheFortranFormula`: `CycleStatistics.cs`'s
  `Matrix` call site changed to pass `echoes.OxidizerMassFractionEffective + 0.1`
  instead of the echo itself made the test fail (`DolM1` no longer equalled `3/7`);
  reverted, green again — proof that `Matrix` reading `GGG` from one source (the echo)
  rather than recomputing it locally is load-bearing, not merely tidier.
- `CategoriesTests.MergeAndDescribeUsesBinary32ArithmeticForDpRow`: `Categories.cs`'s
  `(int)Binary32.TruncatedQuotient(dpMax, categoryStep) + 1` reverted to
  `(int)Math.Floor(dpMax / categoryStep) + 1` made the test fail (`DpRow` came back 71
  instead of 70, for `dpMax`/`categoryStep` built the same way as the C166 evidence for
  `Ndok`); reverted, green again.
- `SmallParticlesTests.ProbabilityUsesBinary32ArithmeticForTheWholeFractionCount` (and
  `ProbabilityMatchesTheFormulaScript`'s own `whole_fraction_count_binary32_boundary`
  case): `SmallParticles.cs`'s `(int)Binary32.TruncatedQuotient(kilo, ak2)` reverted to
  `(int)(kilo / ak2)` made both fail (`pdoksmall[1]` came back `0.0681...`, the
  wholeFractions = 0 branch, instead of `0.28125`, the wholeFractions = 1 branch, for
  `ak2` one double ULP above `2.0`); reverted, green again.
- `CategoriesTests.MergeAndDescribeReportsStatusWhenDpRowExceedsNcat`: `Categories.cs`'s
  checked capacity return replaced with the old silent clamp (`for (var r = 0; r < row
  && r < ncat; r++)`, no early return) made the test fail — not merely with the wrong
  status, but with an `IndexOutOfRangeException` thrown by `Recompute`'s own `for (var r
  = 0; r < row; r++)` once `row` was let past `ncat`, proving the old clamp was not just
  silently wrong but a live crash risk once `DPRow` ever exceeded `Ncat`; reverted,
  green again.
- `SetupTests.PrepareRejectsInvalidNnWindowWhenNnMinIsNotBelowNnMax` (2026-09-20,
  representative of the two `InvalidNnWindow` clauses, the same pattern as
  `InvalidCoefficients` above): `Setup.cs`'s `!(parameters.NnMax > 0.0) ||
  !(parameters.NnMin < parameters.NnMax)` narrowed to drop the second clause made the
  test fail (`NnMin = 100, NnMax = 50` came back `Ok` instead of `InvalidNnWindow`,
  since `50 > 0` alone no longer flags it); reverted, green again.
  `PrepareRejectsInvalidNnWindowWhenNnMaxIsNotPositive` (the literal reported defect,
  `NnMax = 0.0` at the default `NnMin = 3`) is not independently mutation-proven by a
  code edit: with the default `NnMin`, that input already trips the second clause too
  (`3 < 0` is false), so it is kept as the direct regression test for the reported
  input, not as the isolation proof for the first clause.
- `SetupTests.PrepareTreatsAnyNonzeroPocketFormingFlagAsForming` /
  `.PrepareRejectsInvalidPocketFormingFractionCount` (D7, 2026-09-24): reverting
  `Setup.cs`'s fix to the reported `formulation.PocketFormingFractions is { } flags ?
  (byte)flags[i] : (byte)1` made both fail — the first with `NoPocketFormingFraction`
  instead of `Ok` (a flag of `256` narrowing to `(byte)0`), the second with an
  uncaught `IndexOutOfRangeException` instead of the new status (no length check before
  `flags[i]`); reverted, green again.
- `Binary32Tests.TruncatedQuotientOfProductLeavesTheProductUnroundedAtAnIntegerBoundary`
  / `SetupTests.SizesUsesTheUnroundedProductForNkarm` /
  `.SizesUsesTheUnroundedProductForNcat` (S-2, 2026-09-24): reverting `Setup.cs`'s fix
  (`Binary32.TruncatedQuotientOfProduct` back to `Binary32.TruncatedQuotient` for
  `Nkarm`/`Ncat`) made all three fail at the constructed boundary (1506 instead of
  1507, 4390 instead of 4391); reverted, green again.
  `.SizesNkarmAndNcatOfC166AreUnaffectedByTheProductRoundingFix` is not independently
  mutation-proven by this edit — C166's own quotient sits far enough from its boundary
  (BOOT.md, "## Invariants") that both readings agree, which is the point of that test;
  it is the positive control, not the sensitivity proof.
- `DaCoefTests.MpMatchesTheOriginalUnderOriginalExceptPsan02nByOnePrintUnit` (2026-09-27;
  replaced 2026-10-01 by `MpMatchesTheOriginalUnderOriginal`, entry below):
  `CycleStatistics.cs`'s `Matrix` rounding `mp` to binary32 at the end, under
  `Original`, made the PSAN02n case fail (the declared one-print-unit gap closes, so the
  "one unit below" assertion no longer holds); reverted, green again. Right proof: the
  `Binary64` arm's known answer (the original's value on all five,
  `MpMatchesTheOriginalUnderBinary64`) passes unchanged, the same construction read at a
  kind the mutation does not touch.
  `DaCoefIsOneValueAcrossEveryArchivedOutput` is proven by construction, not a code
  mutation: a scratch copy of one replica with `da_coef` edited by one print unit turns
  it red (`DaCoefIsOneValueAcrossEveryArchivedOutputIsSensitiveToADivergingOutput`).

⚠ 2026-09-18 (audit of this node): the entries above for `SetupTests.PrepareRejects*`
and the four `CycleStatisticsTests` cycle-gating tests previously described varying the
*input* across an unmodified implementation (a different `Formulation` field, a
different `cycleIndex`) as the "mutation". That is not AGENTS.md §13's mutation — no
source code was broken and seen red — it is ordinary test coverage of distinct
branches, and cannot by itself prove any single branch load-bearing against a plausible
bug in that branch specifically. Each pattern now carries one genuine code mutation
(above) as a representative proof that the general mechanism (branch-per-status,
gate-per-cycle-index) is real, rather than retracting the claim entirely; the remaining
tests in each family stay recorded as coverage, not as independently mutation-proven.

- `CyclePlaneSiteMapTests` (2026-10-03, over the listing table): a deleted map row, a
  call of the wrong method, an uncited call, a bogus kind and
  `UnrolledSchedule(ndok, 7)` where the table says 6 each turn at least one of its
  directions red. Reverted, green.

  ⚠ 2026-10-03: was the mutation entries of the source-read table's checks, now the
  listing's → HISTORY.md#plane-checks-of-the-source-read-table-2026-10-03
- `CategoriesTests`/`SmallParticlesTests` under both kinds (2026-10-01): `Pl` rounded at
  1050 and `zdoksmall` rounded on every iteration at 1028 each fail
  `register_reads_and_loop_sums_reach_a_stored_bit` under `Original`; a
  `CyclePlaneRounding` that rounds under `Binary64` fails `Binary64` cases of both
  files. Moving the rounding at 1061 ahead of the clamp test is not a red: `pdoksmall`'s
  previous cell is binary32 and rounding is monotone, so the stored value cannot change
  (the verdict's "1062 is output-neutral").
- `DaCoefTests.MpMatchesTheOriginalUnderOriginal` (2026-10-01): red on PSAN02n (0.7917714)
  only with both the `Memory` at 1016 and the folds and literals of 1014–1016 removed;
  either alone still prints 0.7917715, so the design's predicted red, 1016 alone, is
  not one.
- `PerCyclePlaneOutOfSampleControlsTests.Control4*` (2026-10-02), the gating control
  (iv) on `psan01.dat` and `psan02.dat`: the pocket-forming flags left unread turns both
  files red (the control's own `Control4IsRedWhenThePocketFormingFlagsAreNotRead`, the
  same path); an edit of `src/Statistics/CycleStatistics.cs`, `Matrix` — 1016's `Memory`
  removed (`rps02` red), the folds of 1014–1016 removed (both red) — each reverted,
  green again. `gdoksfr`'s `Loop` at 1004, `gdokleft`'s `Memory` at 1001 and the
  in-block read of `gdokleft` at 1008 moved nothing: the control does not see them.
- `LegacyDaCoefReproductionTests` (2026-10-02): 1016's `Memory` removed from
  `CycleStatistics.Matrix` moves C166 and PSAN02 from `match` to `miss` and the test
  goes red naming both; reverted, green. The other direction by the approved file: `miss
  CSPX01` edited to `match CSPX01` turns the test red; restored, green. The five
  reference formulations stay `match` under it: they cannot tell the rules apart.
- `PathIdenticalPairsReportTests` and `CyclePlanePairsTieTests` (2026-10-02): `epsx1`'s
  `Narrowing` at 814 removed from `CycleStatistics.OxidizerSizes` turns the tie red on
  five of the six pairs (P33 is unaffected), reverted, green; the
  report itself reads stored files, so it moves only with them and with the declared
  differences. A line of `PathIdenticalPairs.approved.txt` deleted turns it red.

- `CycleStatisticsFormulaTests.ComputeMatchesTheFormulaScript` (2026-10-02): each
  mutation below was applied to `src/Statistics/CycleStatistics.cs` and reverted, and
  at least one case went red; `dotnet test --filter
  FullyQualifiedName~CycleStatisticsFormulaTests`, red cases named as `case/kind`.

  The table of the 17 edits, each red, and the one that survived (`eps6`, 782, exact in
  binary32 by Sterbenz) → HISTORY.md#cycle-statistics-formula-mutations-2026-10-02

- `CycleStatisticsFormulaTests` and `ListingOracleTests` over the listing-driven plane
  (2026-10-03), each edit applied to `src/Statistics/CycleStatistics.cs` and reverted:
  `epsx3`'s store (816) removed turns the control cases red (`control_04_40950f` and
  fourteen more); `OxidizerSizes`' `UnrolledSchedule(ndok, 6)` made `(ndok, 5)` turns
  `control_08_40ce8f`, `control_11_40f077_40c3b7`, `control_14_40c22c` and
  `ComputeMatchesTheOracleExceptWhereApproved` red. Two kinds of edit survive and stay
  recorded: the 988 temporary removed (its store rounds the same value, so rounding it
  twice is the identity); and the product order at a call site swapped for
  `Binary64`'s (805, 806, MD4 and DD4 at 968–973, 1089), which no case and no oracle
  output sees, a difference of 1e-16 relative that the following binary32 store
  absorbs. The forms themselves are held at `double` by `PlaneFormsTests`, each but
  `Square`, an equivalent form (`pow(y, 2)` is `y·y` when pow is correctly rounded); a
  call site that uses the wrong form is held by no bit, but by
  `CyclePlaneOrderSiteTests` (entry below). A case that reaches one needs a sum within
  1e-16 of a binary32 midpoint, a construction no seeded search of this stage found;
  not claimed.

  ⚠ 2026-10-03: was "the forms themselves are held at `double`", all five, false for
  `Linear`: its 24 tuples had binary32 weights, which make every association of `x·di·c`
  exact, so `x·(a·b)` in its Original branch stayed green. Now quotient weights (11 and 9
  of 24 tuples tell the other associations apart); that edit is red, the old file green.

- ⚠ 2026-10-03: `Setup.AnalyticSizes`' schedule as rule L, or as `(fractionCount, 6)`, red
  on the three pinned cases alone → HISTORY.md#analytic-sizes-mutations-2026-10-03
- `ListingOracleTests` and `PreloopSurveyTests` (2026-10-03, stage 5b), edited and
  reverted: `FractionLaw.Build`'s `Original` lower fourth power as `l·l·l·l` turns the
  oracle comparison and the survey's ratchet red (`zx`, `z11`); the JZZ = 2 loop's
  `l**5` unrounded, and `DOKSD` never stored after passes 1-3, turn those two and the
  drawn-loops test red; the width `u - l` never rounded, and rounded in every pass,
  turn the drawn-loops test alone red, the 49 shipped files telling neither apart.
- `ListingOracleTests` (2026-10-03): the pow-triple assertion is red on a copy of the
  fixture whose `pow` results are `exp(e·log b)` (`PROPSTRUCT_ORACLE_FIXTURE`; 19 of 41
  results differ), green on the committed one. The guard test is red on a
  typed `3.14159` appended to `cycle_plane_oracle.py` (`guard`, exit 1), green restored.
  The machine's and the oracle's own selftests carry r1, r2 and r3 (`tests/Fixtures`).
- `PlaneFormsTests`' generator guard (2026-10-03, `_check_form_is_told_apart` of
  `formulas_statistics.py`): run on the old tuples, the binary32 weights of before,
  `generate` refuses them: `Linear`'s alternative associations tell apart 0 of 24
  tuples and `Quadratic`'s 2 of 24, both below the fair share of 1 in 8 (3 of 24).
  Green on the quotient weights now committed.

- `CyclePlaneOrderSiteTests` (2026-10-03), each edit made in memory to one line of
  `src/Statistics` and named at that line: 805 `(quotient, di, k + 0.5)`, 1089
  `(k + 0.5, fraction, di)` and 899 `(q, i + 0.5, cellSize)` (other associations), 806
  `Quadratic` as `Linear`, 853 `Fourth` as `Cube`, 972 `Cube` inside `Square`, four
  removed citations, `Linear`'s `Original` expression as `x·(a·b)`: eleven reds, none
  at another line; right on three commutations (`di · (k + 0.5)` for `(k + 0.5) · di`).
- `Qks1NormalisationTests` (2026-10-03): `Pockets`' divisor `QKSS + 1` turns both tests
  red, `(double)(float)QKSS` only the pair at 2^24 + 1; each reverted.
- `PreloopSurveyTests` (2026-10-03): `Setup.AnalyticSizes`' `UnrolledSchedule(count, 5)`
  as `(count, count)` adds `AK157a`, `BK10`, `KB397` and `C166` to the approved file;
  an executed `DOKM` one unit away, in memory, adds its file; reverted, the file is empty.
- `ListingOracleTests.TheTwinThatSuppliesTheInjectedSetupReproducesItsCaseFiles`
  (2026-10-03): `DOKM_UNROLL = 6` in `formulas_statistics.py` turns it red
  (`setup_plane.json`); reverted, green. `TheFixtureIsOfTheCommittedBytes` is red on
  any digested script edited and not restamped.

## Taboos

- Do not loosen a tolerance for the sake of green.
- Do not type an expected value computed by hand.
