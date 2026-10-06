# API.md — Statistics.Tests

A test project; it exposes nothing but its tests.

## Artifacts ✅

```text
tests/Fixtures/cases/statistics/categories.json   constructed totals and the values the formula script computed for them, for Categories.MergeAndDescribe
tests/Fixtures/cases/statistics/cycle_statistics.json   constructed cycles and every report value the formula script computed for them, for CycleStatistics.Compute
tests/Fixtures/cases/statistics/plane_forms.json   the forms of the per-cycle plane's products and the pass schedule of its unrolled sums, for PlaneFormsTests
tests/Statistics.Tests/LegacyDaCoef.approved.txt   the formulations whose archived da_coef the constructed cycle reproduces, and those it misses: a ratchet (22 of 22 since 2026-10-03)
tests/Statistics.Tests/PathIdenticalPairs.approved.txt   every cell whose printed token differs between the original and the port in tests/Fixtures/cycle-plane-pairs: a ratchet
tests/Fixtures/cases/statistics/cycle_plane_oracle.json   the listing oracle's cases: the executable's own bytes run over seeded cases, every output of every cycle, for ListingOracleTests
tests/Statistics.Tests/ListingOracle.approved.txt   every (case, cycle, output) whose bits differ between Compute under Original and the oracle, both values as hex: a ratchet, empty since 2026-10-03
tests/Statistics.Tests/ListingOracleSetup.approved.txt   every pre-loop value (line 375, 378, PARAM's ZSS, ZX and Z11, DOKM, DOKSD) the executable computed and Setup.Prepare does not: a ratchet, empty since 2026-10-03
tests/Fixtures/cases/statistics/preloop_survey.json   the executable's pre-loop (ZSS, ZX, Z11, lambda, DOKM, DOKSD, GGG) for every shipped .dat and 120 drawn JZZ = 2 loops, from the oracle, with its provenance, for PreloopSurveyTests
tests/Statistics.Tests/PreloopSurvey.approved.txt   every shipped formulation whose executed pre-loop Setup.Prepare does not reproduce, both values as hex (ZX, Z11 binary64): a ratchet, empty since 2026-10-03 (CSPX04 agreed from the JZZ = 2 loop)
```

## Categories after the delivery ✅

Stage S2 of the delivery (root `BOOT.md`, `## Delivery`; `tools/legacy`) moved the
original's program out of the repository on 2026-10-04; since then:

| Fact | Category | Reads |
|---|---|---|
| `CyclePlaneListingTests`, both facts (`verify`, `selftest`) | `Legacy` | the listing excerpt and the executable |
| `CyclePlaneListingMapTests`: `MapHoldsAgainstTheListing`, `GeneratedTableReproducesFromTheListingByteForByte`, `EveryCheckOfTheMapAndTheGeneratorGoesRedOnAViolationItGuards` | `Legacy` | the excerpt, through `classify-cycle-plane-listing.py` |
| `SetupPlaneClassificationGeneratorTests`: `ClassificationReproducesByteForByteOnRegeneration`, `ContinuationRuleReadsTheTabFormLinesAsInitialLines` | `Legacy` | the source, through `classify-setup-plane.py` and `fortran_source.py` |
| `ListingOracleTests`: `TheMachineGoesRedOnEveryViolationItGuards`, `TheOracleHoldsNoMapNameAndNoPlaneLiteral`, `TheOracleGoesRedOnEveryViolationItGuards`, and `TheOracleFixtureReproducesFromTheExecutable`, which runs `cycle_plane_oracle.py verify` (475 s, 26 cases) | `Legacy` | the executable, the excerpt and the source |
| `PreloopSurveyTests.TheSurveyReproducesFromTheExecutable` | `Legacy` | the executable and the source |
| `ListingOracleTests.TheFixtureIsOfTheCommittedBytes`, `PreloopSurveyTests.TheSurveyIsOfTheCommittedBytes` | fast | the digests the fixtures record against `tools/legacy/original.sha256`, not the files |
| `LineMapCoverageTests` | fast | `tests/Fixtures/cases/source/executable_lines.json`; its failure message names line numbers, never a source line |

Every other fact reads committed data and stays in the fast set: `ListingOracleTests`'
bit comparisons against the committed oracle fixture and the approved ratchets, the
site-map and order facts over the generated tables, the pairs and rate reports.

## Out of scope

- The statistical criterion itself: `Harness`.
