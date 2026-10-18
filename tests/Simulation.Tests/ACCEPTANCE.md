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
      (`OriginalModelGuardTests`): 33 cases, green in Debug against a Release-written file.
      Red once: one ulp added to `Pockets.Dp43` in `ReferenceModeRunner.Execute` turned the
      four `seed0/inpt` cases red on the JSON hash, their `results.m` hashes equal to the
      approved ones and `SeedZeroResultsMMatchesApprovedSnapshot` green; one character of
      `extra/P33-defaults`' JSON hash edited, red; reverted. Right once: regenerated three
      times from scratch, identical; its `results.m` column equals the untouched snapshot's.
- [x] 2026-10-18 (T13): the guard sees every one of the twelve `CorrectionSet` flags. Each flag
      was applied alone under `Original` (a scratch mapping at `SimulationOptions.EffectiveCorrections`,
      `Model == Original ? <flag> : ...`, the flag read from an environment variable so that one
      Release build served all twelve runs; reverted), `OriginalModelGuardTests` run (35 tests:
      34 cases and the file's own); the unmapped run is green, 35 of 35. A case is red when the
      flag changes a bit of its `results.m` or JSON. "B64" is the 24 `Binary64` cases (ten
      `seed0/*/*/Binary64`, five `batched/*`, nine `extra/*`); no `seed0/*/*/Original` case is red
      under any flag, since `Prepare` stores `None` under `PrecisionKind.Original`.

      | Flag | Value | Red cases |
      |---|---|---|
      | `ExactConstants` | 1 | all 24 B64 |
      | `BridgeFrustum` | 2 | all 24 B64 |
      | `Fmkarm2OnAcceptance` | 4 | all 24 B64 |
      | `WindowLowerEdge` | 8 | all 24 B64 |
      | `VdokTotalOnCopy` | 16 | `extra/P312-cycles3`, `extra/PSAN02-sfr-dmin20-cycles3` |
      | `ZdoksmallCoarseShare` | 32 | `extra/P312-defaults`, `-cycles3`, `-homogenized0.1`, `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` |
      | `GdokleftOnce` | 64 | `extra/PSAN02-sfr-dmin20-di10` alone |
      | `DoksdConsistent` | 128 | `seed0/P33/{Original,Independent}/Binary64`, `batched/P33`, `extra/P33-defaults`, `extra/P2-defaults` |
      | `CategoriesOnCopy` | 256 | all 24 B64 |
      | `CategoriesLastRowRecomputed` | 512 | 21: all B64 but `seed0/HMX/*/Binary64` and `batched/HMX` |
      | `CategoriesEmptyRowsMerged` | 1024 | 12: `seed0/{HMX,inpt,PSAN02n}/*/Binary64`, `batched/{HMX,inpt,PSAN02n}`, `extra/PSAN02-sfr-dmin20`, `-cycles3`, `-di10` |
      | `PdoksmallFirstCell` | 2048 | `extra/P312-dmin5` alone |

      `GdokleftOnce` was seen by no case of the 33 before `extra/PSAN02-sfr-dmin20-di10` was added
      (the 33 stay green under it); red once there: flag 64 turns that case red, right once:
      the same case is green unmapped, on a line generated at `ac1fc20`
      (`## The Original-model guard`). `PdoksmallFirstCell` and `GdokleftOnce` rest on one case each.
- [x] 2026-10-18: the model kind is covered by `ModelKindTests` (18 cases) and
      `DegenerateConfigurationTests`' `Corrected` case: surface, mapping on P33's
      `Dok43sd` (the archive's 75.45 for `Original`, 112.66 for `Corrected`), the refusal and
      the batched refusals' unchanged messages, the seam with its negative and positive
      controls, determinism, the stored ratios; every one red once, `## Mutations`, "Model
      kind". The guard stayed green, 34 of 34, and the `SeedZero` facts, 59 of 59.
- [x] 2026-10-18: the effect of each correction is measured on the five references (16
      seeds) and four extra cases (8 seeds), per arm and printed quantity
      (`CorrectionEffectsMeasurement`, `Measurements/corrections-2026-10-18.md`; `BOOT.md`,
      "## Correction-effects measurement"; 9 cases, 14 arms, 1568 runs, none failed, about 95
      minutes of work on the CPU). Positive controls pass: `WindowLowerEdge` alone in reference
      mode lies inside the 2026-09-24 audit's range on all five references (`Dqmkm2` -2.82 % on
      inpt to -6.95 % on HPEPA3, `Zkarm` +4.91 % on HMX to +13.51 % on PSAN02n, the standard
      errors 0.01 to 1.7 points; the range's two ends are reproduced to the printed digit);
      `DoksdConsistent` gives P33 112.66 in all 16 runs, `Original` 75.45; `ExactConstants` scales
      `Lambda` by 3.14 / π to 2.2e-16 on all five. The negative control (`Original` even against
      odd seeds) is not clean: 0 to 9 differing cells of 1200 to 7000, all in cells
      `TwoSampleBiasOfSets` cannot judge (one half prints, the other not; print resolution),
      under the asserted one per cent (`BOOT.md`, "Floor").
