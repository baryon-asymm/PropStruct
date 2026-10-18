# ACCEPTANCE.md — Statistics.Tests

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
- [x] 2026-10-18: `PdoksmallFirstCellLimitTests` and the identity in `VdokTotalOnCopyTests`
      (`src/Statistics/ACCEPTANCE.md`, the two criteria of that date): the integral of the
      size law is right on a uniform law (linear, midpoint mean) before it measures anything;
      the totals of `AnalyticCycle` begin `Vdokstr` at `Dmin`; the three reds are in
      "## Mutations".
