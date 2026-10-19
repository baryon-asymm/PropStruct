# ACCEPTANCE.md — Simulation.Tests

## Acceptance criteria

- [x] Every row of the levels table is green, with a date and the names of the tests.
      (2026-09-24: the statistical-criterion row realigned again — the seed-0 ratchet
      retired for an exact snapshot, link 1's own reproduction claim moved to
      `tests/Harness.Tests/RateCriterionTests` as a rate over sixteen seeds,
      "## Statistical criterion measurement (2026-09-24)" above. "Green" here means
      "the seed-0 output has not moved since approval, and link 3 still agrees", not
      "the underlying science has no open question" — link 2's own still-missing HMX
      multi-seed measurement (root BOOT.md) is unaffected by this task and remains
      exactly as root records it.)
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13).
      (2026-09-19, "## Mutations" below; 2026-09-23 adds link 3's CPU positive control;
      later the same day, link 3's CUDA row adds its own positive control and its own
      mutation of the row itself, same section; 2026-09-24 adds the line-map coverage
      row's own mutation and the seed-0 snapshot's own one-character-edit proof, same
      section — the ratchet's own former proof moved to `HISTORY.md` with the rest of
      the retired link-1 text; 2026-09-28 adds `AttemptsPerLaunchDiagnosticsTests`' two
      echo mutations, its own positive control, and the JSON round trip's own
      `[JsonIgnore]` mutation in `tests/Output.Tests`, same section.)
- [x] 2026-10-03: the per-cycle plane under `Original` passes its controls, frozen
      before its code (`src/Statistics/BOOT.md`, "## Report"), on the seed-0 rate runs
      against the original's archived `results.m` and the frozen pre-change runs
      `Snapshots/CyclePlaneBaseline` (rate runs of 514a157, byte copies)
      (`CyclePlaneSeedZeroControlsTests`, 39 cases): P1 `da_coef` on all five, P2 HMX
      `epsx(1..4)`, P3 P33 `epsx(1..6)`, P4 HMX `epsalldok` and `epsdok(2)` and P33
      `epsalldok`, P5 inpt `epsdok43_n[0..4]` print the original's value; N1 HMX
      `epsdokfr` still does; N2 no cell the baseline printed exactly is lost; N4 no
      counter or printed length moves except onto the original's, and HMX's and P33's
      generator counters equal the original's. Red: the port read from the baseline
      fails the 19 positive controls it missed; read from the `Binary64` run, N2 fails on
      all five, N4 on HMX, HPEPA3 and PSAN02n, the counters on HMX. Reverted, green.

      ⚠ 2026-10-01: `Snapshots/SeedZeroResultsM.approved.txt` re-approved: the ten
      `Original` hashes moved, the ten `Binary64` ones are unchanged (the rate table's
      keyed diff: 160 `Binary64` runs byte-identical, no counter moved in any run).

      ⚠ 2026-10-03: re-approved again, the plane following the executable's listing
      (x87 A, stage 4): nine of the ten `Original` hashes moved, the ten `Binary64` ones
      are unchanged, and the 39 cases above hold on the regenerated seed-0 runs.

      ⚠ 2026-10-03: was dated 2026-10-01; the 39 cases were re-run on the regenerated
      seed-0 runs and hold (`src/Statistics/ACCEPTANCE.md`, A5, A6), so that is the date
      of this evidence, one date for one piece of evidence (AGENTS.md §6).
- [x] 2026-10-02: the stored port output of each cycle-plane pair is what the port
      prints now (`CyclePlanePairsTieTests`, six pairs, `Category=Long`: reference mode,
      `Original` layout, seed 1, `Original` precision, every parameter read from
      `tests/Fixtures/cycle-plane-pairs/provenance.json`), so the ratchet of
      `tests/Statistics.Tests/PathIdenticalPairsReportTests` cannot go stale. Red on
      `epsx1`'s rounding at 814 removed (five of six), reverted, green.

- [x] `LineMapCoverageTests` reads `tests/Fixtures/cases/source/executable_lines.json`
      and is red once on a line the map no longer covers (stage S2, 2026-10-04: the row
      `297–342` of `src/Simulation/BOOT.md`'s line map cut to `297–341`: red, naming
      line 342 by number; reverted, green; 133 cases of the fast set with the original
      absent).

  ⚠ 2026-10-04: was "red once on a line removed from the list", which no mutation can
  meet: a shorter list asks the map for less. Reformulated as above.
- [x] 2026-10-04: Every fact that returns when no CUDA accelerator is available calls
      `Harness`'s `CudaRequirement.FailIfRequired`, found by a text check and not typed
      here (`CudaRequirementWiringTests`), and fails under `PROPSTRUCT_REQUIRE_CUDA=1` on a
      machine where CUDA is refused: 6 facts of this node, 1 of them in the fast set, red
      with `PROPSTRUCT_NO_CUDA=1` and the variable set, green without it (stage S4).
- [x] 2026-10-18: the `Original` model is guarded by hashes of the full result
      (`OriginalModelGuardTests`): 35 cases, 12 of them in the fast set (every
      `batched/*` and `extra/*` case but the three P312 extras that take 44 to 94 s in
      Debug) and 23 `Category=Long`, green in Debug against a Release-written file; the
      Long ones green in Release too. Red once: one ulp added to `Pockets.Dp43` in
      `ReferenceModeRunner.Execute` turned the
      four `seed0/inpt` cases red on the JSON hash, their `results.m` hashes equal to the
      approved ones and `SeedZeroResultsMMatchesApprovedSnapshot` green; one character of
      `extra/P33-defaults`' JSON hash edited, red; reverted. Right once: regenerated three
      times from scratch, identical; its `results.m` column equals the untouched snapshot's.
      U3: one character of the new `extra/PSAN02-sfr-dmin20-cycles3/Original` line
      edited, that case alone red, reverted; its line is generated with the product code
      of `ac1fc20` (`BOOT.md`, "## The Original-model guard"), the 34 older lines
      byte-identical.

  ⚠ 2026-10-19: was "the 34 older lines byte-identical" as the whole provenance of the
  file, every line generated with the product code of `ac1fc20` or unchanged since; now
  two lines were regenerated at the head on purpose, the next criterion.

  ⚠ 2026-10-18: was "33 cases, green in Debug against a Release-written file", the whole
  guard `Category=Long` and so run by no CI →
  HISTORY.md#original-guard-criterion-33-cases
- [x] 2026-10-19 (U6): two lines of the guard were regenerated on purpose, on the
      owner's decision (root `BOOT.md`, "Model kind", the 2026-10-19 exception):
      `extra/PSAN02-sfr-dmin20` and `extra/PSAN02-sfr-dmin20-cycles3` (`Binary64`,
      `Dmin` 20 µm written in µm, default `Di`; the Dmin cell index was 1 and is 2,
      `src/Statistics/BOOT.md`, "The cell index under `Binary64`").
      `original-model-guard` run at the head in Release (35 cases, 6 m 02 s). (a) The
      file differs from the one before in exactly those two lines, the other 33
      byte-identical (`git diff` of the file: 2 insertions, 2 deletions). (b) With only
      the index computation reverted in scratch (`Setup.cs`, `var dminCell =
      (int)(dminMetres / cellSizeMetres)` for both kinds, nothing else touched) the
      regenerated file (35 cases, 6 m 05 s) equals the file before U6 byte for byte,
      SHA-256 `c675b1d7cf6063a7331f989d1b6d72081f50e6575b0ec4e98f6d2dcee1ec33d3` on
      both, so the change is the fix and nothing else; `src/` restored, `git diff`
      clean. Right: the new `results.m` hashes of `-dmin20` and `-di10` are equal, as
      the case written with `Di` 10 µm (whose index the double quotient had right)
      always was. The 47 facts of both guards green in Release, the `Long` ones included
      (6 m 0 s). The only lines ever regenerated (`BOOT.md`, "Provenance").
- [x] 2026-10-18 (T13): the guard sees every one of the eleven `CorrectionSet` flags
      (twelve until `Fmkarm2OnAcceptance` was withdrawn, U1). Each flag
      was applied alone under `Original` (a scratch mapping at `SimulationOptions.EffectiveCorrections`,
      `Model == Original ? <flag> : ...`, the flag read from an environment variable so that one
      Release build served all twelve runs; reverted), `OriginalModelGuardTests` run (35 tests:
      34 cases and the file's own); the unmapped run is green, 35 of 35. A case is red when the
      flag changes a bit of its `results.m` or JSON. "B64" is the 24 `Binary64` cases (ten
      `seed0/*/*/Binary64`, five `batched/*`, nine `extra/*`); no `seed0/*/*/Original` case is red
      under any flag, since `Prepare` stores `None` under `PrecisionKind.Original` (nor
      is the case added by U3, `extra/PSAN02-sfr-dmin20-cycles3/Original`).

      | Flag | Value | Red cases |
      |---|---|---|
      | `ExactConstants` | 1 | all 24 B64 |
      | `BridgeFrustum` | 2 | all 24 B64 |
      | `Fmkarm2OnAcceptance` | 4 — retired 2026-10-18 (withdrawn after the audit) | all 24 B64, when it stood |
      | `WindowLowerEdge` | 8 | all 24 B64 |
      | `VdokTotalOnCopy` | 16 | `extra/P312-cycles3`, `extra/PSAN02-sfr-dmin20-cycles3` |
      | `ZdoksmallCoarseShare` | 32 | `extra/P312-defaults`, `-cycles3`, `-homogenized0.1`, `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` |
      | `GdokleftOnce` | 64 | `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` (U6; `-di10` alone until 2026-10-19) |
      | `DoksdConsistent` | 128 | `seed0/P33/{Original,Independent}/Binary64`, `batched/P33`, `extra/P33-defaults`, `extra/P2-defaults` |
      | `CategoriesOnCopy` | 256 | all 24 B64 |
      | `CategoriesLastRowRecomputed` | 512 | 21: all B64 but `seed0/HMX/*/Binary64` and `batched/HMX` |
      | `CategoriesEmptyRowsMerged` | 1024 — retired 2026-10-19 (withdrawn after the audit) | 12 when it stood: `seed0/{HMX,inpt,PSAN02n}/*/Binary64`, `batched/{HMX,inpt,PSAN02n}`, `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` |
      | `PdoksmallFirstCell` | 2048 | `extra/P312-dmin5` alone |

      `GdokleftOnce` was seen by no case of the 33 before `extra/PSAN02-sfr-dmin20-di10` was added
      (the 33 stay green under it); red once there: flag 64 turns that case red, right once:
      the same case is green unmapped, on a line generated at `ac1fc20`
      (`## The Original-model guard`). `PdoksmallFirstCell` and `GdokleftOnce` rest on one case each.

  ⚠ 2026-10-19 (U6): was "`GdokleftOnce` ... `extra/PSAN02-sfr-dmin20-di10` alone" and
  "rest on one case each"; now `GdokleftOnce` is red on `extra/PSAN02-sfr-dmin20`,
  `-cycles3` and `-di10`, and only `PdoksmallFirstCell` rests on one case. Why it was
  wrong: the exact Dmin cell index of those two cases is 2, not 1, so `b` is 20 µm
  there, as `-di10` always had it (`BOOT.md`, "## The Original-model guard"). Found by
  the re-run of every flag over the 14 `Binary64` cases that are not `seed0/*` and the
  one `Original` case (Release, a scratch mapping at
  `SimulationOptions.EffectiveCorrections` reading the flag from an environment
  variable, reverted): the other nine flags give the sets of the table, cell for cell;
  the `seed0/*` cells were not re-run, their approved lines not having moved. The
  retired flag has no row to re-run.
- [x] 2026-10-18: the model kind is covered by `ModelKindTests` (24 cases) and
      `DegenerateConfigurationTests`' `Corrected` case: surface, mapping on P33's
      `Dok43sd` (the archive's 75.45 for `Original`, 112.66 for `Corrected`), the refusal and
      the batched refusals' unchanged messages, the seam with its negative and positive
      controls, determinism, the stored ratios; every one red once, `## Mutations`, "Model
      kind". The guard stayed green, 34 of 34, and the `SeedZero` facts, 59 of 59.
      U3 adds the mapping itself,
      `TheEffectiveCorrectionsAreEveryFlagUnderCorrectedWithNoSeamAndNoneUnderOriginal`:
      `Corrected` with no seam is `CorrectionSet.All` and `Original` is `None`. Before
      it, a mapping of `All & ~GdokleftOnce` (a scratch edit of `EffectiveCorrections`,
      the dropped flag read from an environment variable) left the other 23 facts green,
      23 of 23; with the new fact it is the one red, its failure printing the ten flags
      that are left; unmapped, 24 of 24; reverted.

  ⚠ 2026-10-18: was "`ModelKindTests` (18 cases)"; the file held 23 cases before U3, so
  the figure was already stale → HISTORY.md#model-kind-tests-case-count
- [x] 2026-10-18 (U3): the `Corrected` results are guarded by hashes of the full result
      (`CorrectedModelGuardTests`, `Snapshots/CorrectedModelSnapshot.approved.txt`,
      written by `dotnet run --project tests/RateTableTool -- corrected-model-snapshot`;
      `BOOT.md`, "## The Corrected-model snapshot"): 10 cases (8 in the fast set, the
      two P312 ones `Category=Long`), generated at the head, `Binary64`, no seam. Green
      in Debug against the Release-written file, 11 of 11. Red once: one character of
      `corrected/extra/P33-defaults`' hash edited, that case alone red, reverted. Each
      of the eleven flags dropped in turn from the `Corrected` mapping (a scratch `All &
      ~<flag>` in `EffectiveCorrections`, the flag read from an environment variable so
      one Release build served all eleven runs; reverted) turns at least one case red,
      none stays green:

      | Flag dropped | Value | Red cases (of 10) |
      |---|---|---|
      | `ExactConstants` | 1 | all 10 |
      | `BridgeFrustum` | 2 | all 10 |
      | `WindowLowerEdge` | 8 | all 10 |
      | `VdokTotalOnCopy` | 16 | `extra/P312-cycles3`, `extra/PSAN02-sfr-dmin20-cycles3` |
      | `ZdoksmallCoarseShare` | 32 | `extra/P312-defaults`, `-cycles3`, `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` |
      | `GdokleftOnce` | 64 | `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` (U6; `-di10` alone until 2026-10-19) |
      | `DoksdConsistent` | 128 | `extra/P33-defaults`, `extra/P2-defaults` |
      | `CategoriesOnCopy` | 256 | all 10 |
      | `CategoriesLastRowRecomputed` | 512 | all 10 |
      | `CategoriesEmptyRowsMerged` | 1024 — retired 2026-10-19 (withdrawn after the audit) | `batched/inpt`, `batched/PSAN02n`, `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10`, when it stood |
      | `PdoksmallFirstCell` | 2048 | `extra/P312-dmin5` alone |

      Every flag is seen by at least one fast case, so the two Long cases are not the
      only guard of any; `GdokleftOnce` and `PdoksmallFirstCell` rest on one case each.
      Right once: the unmapped run is green.

  ⚠ 2026-10-19 (U6): was "each of the eleven flags" and "`GdokleftOnce` ... `-di10`
  alone", "rest on one case each"; now ten flags (`CategoriesEmptyRowsMerged` withdrawn),
  `GdokleftOnce` red on the three PSAN02 extras, `PdoksmallFirstCell` alone on one case. The snapshot was regenerated at the
  head (Release, 10 cases, 48 s): five lines moved (`batched/inpt`, `batched/PSAN02n`,
  by the withdrawal of `CategoriesEmptyRowsMerged`; `extra/PSAN02-sfr-dmin20`,
  `-cycles3`, by that and the exact Dmin cell index; `extra/PSAN02-sfr-dmin20-di10`, by
  the withdrawal alone), five did not (the P312 three, `extra/P33-defaults`,
  `extra/P2-defaults`). The ten flags were dropped in turn over all ten cases again
  (Release, the scratch mapping `All & ~flag` of the flag read from an environment
  variable, reverted): every cell of the table is as written but `GdokleftOnce`'s, no
  flag stays green, and `batched/inpt` and `batched/PSAN02n` are red under five flags
  (`ExactConstants`, `BridgeFrustum`, `WindowLowerEdge`, `CategoriesOnCopy`,
  `CategoriesLastRowRecomputed`), so they stay as the snapshot's batched cases. Right:
  the unmapped run is green, 11 of 11, in Release.
- [x] 2026-10-18 (U3): the Original guard's fast subset still sees every flag. The 12
      fast cases (`batched/*` five, `extra/P312-dmin5`, `extra/PSAN02-sfr-dmin20`,
      `-cycles3`, `-di10`, `extra/P33-defaults`, `extra/P2-defaults`,
      `extra/PSAN02-sfr-dmin20-cycles3/Original`), run with each flag applied alone
      under `Original` as T13 did (a scratch mapping, one Release build): the unmapped
      run is green, 12 of 12; every flag turns at least one red, 11 of the 12 for
      `ExactConstants`, `BridgeFrustum`, `WindowLowerEdge` and `CategoriesOnCopy` (all
      the `Binary64` ones; the `Original`-precision case is red under none), 10 for
      `CategoriesLastRowRecomputed` (all but `batched/HMX`), `VdokTotalOnCopy` by
      `extra/PSAN02-sfr-dmin20-cycles3`, `ZdoksmallCoarseShare` by the three PSAN02
      extras, `GdokleftOnce` by `extra/PSAN02-sfr-dmin20-di10` (by the three PSAN02
      extras since U6), `DoksdConsistent` by `batched/P33`, `extra/P33-defaults` and
      `extra/P2-defaults`, `CategoriesEmptyRowsMerged` by six (`batched/{HMX,inpt,PSAN02n}`,
      the three PSAN02 extras; retired 2026-10-19), `PdoksmallFirstCell` by
      `extra/P312-dmin5`. The cheap subset costs, in Debug, 32 s for the 22 fast tests of `OriginalModelGuardTests` and
      `CorrectedModelGuardTests` together (`batched/HMX` 17 s, `batched/HPEPA3` 7 s, the
      rest 0.03 to 7 s a case); the other 169 fast tests of this assembly take 9 s, and
      inside `dotnet test PropStruct.sln` the assembly took 48 s (1 m 29 s for the whole
      fast set, `tests/Harness.Tests`' 1 m 13 s the longest): a growth of about 32 to 39
      s, under the 90 s the task allowed. The Long ones, in Debug: `extra/P312-cycles3`
      94 s, `-homogenized0.1` 45 s, `-defaults` 44 s, and the Corrected P312 pair 102 s
      and 47 s.
- [x] 2026-10-18 (U3): the correction-effects tooling
      (`CorrectionMeasurementToolingTests`, 10 cases, fast): every case's seeds, at its
      own count, and the audit's eight have distinct phases modulo 2^20
      (`EveryCasesSeedsAndTheAuditsEightHaveDistinctPhasesModulo2To20`); sixteen seeds
      at the old stride 2^17 repeat exactly the eight seeds 8..15
      (`TheOldStrideWithSixteenSeedsRepeatsEightPhasesAndTheCheckNamesThem`); the report
      refuses an arm with a repeated phase, in its section and in the even-against-odd
      floor, and runs over distinct ones
      (`TheReportRefusesArmsWhoseSeedsRepeatAPhaseAndRunsOverDistinctOnes`); the
      headline columns hold `Dkarm43_cor(1)`, `Dkarm43_cor(2)`, `Dagg43_cor(1)`,
      `Dagg43_cor(2)` and `Zkarm_cor`; the arms are `Original`, `All`, eleven single
      flags and the three `AllBut` arms. Red once each (`BOOT.md`, "## Mutations",
      "U3"): the stride set back to 2^17, the phase check removed, `AllBut` mapped to
      `All & flag`, the `AllBut` arms dropped, one column dropped. Smoke run,
      `PROPSTRUCT_MEASUREMENT_SEEDS=2`, `AnalyseCase` on P33 and PSAN02: 16 arms, the
      new columns and the three `AllBut` rows in `case-P33.md` and `raw-P33.csv`; the
      generated files restored after, the run store deleted.
- [x] 2026-10-19 (U7): the effect of each correction is measured on the five references
      (sixteen distinct seeds `k·2¹⁶`) and four extra cases (8 seeds), per arm and
      printed quantity (`CorrectionEffectsMeasurement`, the page
      `Measurements/corrections-2026-10-18.md` at 4558ce1; `BOOT.md`, "##
      Correction-effects measurement"; 9 cases, 14 arms (`Original`, `All`, ten single
      flags, two `AllBut` arms), 1568 runs = 14 × (5 × 16 + 4 × 8), none failed, 5425 s
      of work on the CPU, the sum of the page's "Work per case"; 156 facts green in
      1.56 h, among them U3's fast fact that every case's seeds have distinct phases).
      Positive controls pass: `WindowLowerEdge` alone in reference mode
      lies inside the 2026-09-24 audit's range on all five references (`Dqmkm2` -2.82 % on
      inpt to -6.95 % on HPEPA3, `Zkarm` +4.91 % on HMX to +13.51 % on PSAN02n, the
      standard errors 0.01 to 1.7 points; the range's two ends are reproduced to the
      printed digit); `DoksdConsistent` gives P33 112.66 in all 16 runs, `Original` 75.45;
      `ExactConstants` scales `Lambda` by 3.14 / π to 2.2e-16 on all five. The negative
      control (`Original` even against odd seeds) is clean: 0 differing cells of 1234 to
      7139 in all nine cases, under the asserted one per cent (`BOOT.md`, "Floor").

  ⚠ 2026-10-18 (U3): unticked. The run behind it took its sixteen seeds as `k << 17`,
  which are eight distinct phases: the streams jump by `seed · 2^80` in a period of
  2^100, so seed k equals seed k + 8 (all 560 pairs of the five committed raw CSVs of 16
  seeds, 14 arms times 8 seeds times 5 cases, are identical in every printed column),
  the standard errors were understated and the paired t's were taken over copies.
  Re-ticked by the re-run over `k << 16`, with the `AllBut` arms and the `Dkarm43_cor`,
  `Dagg43_cor` columns (`BOOT.md`, "## Correction-effects measurement") →
  HISTORY.md#correction-effects-seeds-repeat-2026-10-18

  ⚠ 2026-10-18 (U4): was unticked, now re-ticked on the page at 8b29342, 16 arms and 1792 runs where it had 14 and 1568, the negative control clean where it had 0 to 9 differing cells → HISTORY.md#correction-effects-reticked-u4-2026-10-18

  ⚠ 2026-10-19 (U6): was "eleven single flags and the three `AllBut` arms" (16 arms), now ten single flags and two `AllBut` arms (14), `CategoriesEmptyRowsMerged` withdrawn → HISTORY.md#correction-effects-reticked-u7-2026-10-19

  ⚠ 2026-10-19 (U7): was the page at 8b29342 (16 arms, 1792 runs, 6356 s, 174 facts in 1.83 h), now the page at 4558ce1 (14 arms, 1568 runs, 5425 s, 156 facts in 1.56 h) → HISTORY.md#correction-effects-reticked-u7-2026-10-19
- [x] 2026-10-19 (U6): a Dmin written in micrometres and the same value in metres are
      one run at the public path (`DminCellRunTests`, 3 facts, fast, 8 s in Debug): P312
      at 10 particles per cycle, `Binary64`, batched, CPU, `Independent`, seed 0
      (`GuardCase`, `ReferenceModeRunner.Fingerprint`); `Dmin` 10 µm gives the same
      `results.m` hash and the same result JSON hash as the default `Dmin` (10e-6 m),
      and 20 µm the same as 20e-6 m, the stored `StoredSetup.Dmin` set to zero in the
      JSON before the hash as the one number the spelling moves in the last bit; the
      positive control, 10 µm against 20 µm, differs in both hashes. Red once: `Setup.cs`
      storing the double quotient `(int)(dminMetres / cellSizeMetres)` for both kinds
      (scratch): the two equalities red, the control green; right: the same three green
      at the head; `src/` restored, diff clean (`BOOT.md`, "## The Dmin cell index at
      run level").
