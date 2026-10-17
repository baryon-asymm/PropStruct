# ACCEPTANCE.md — Fixtures

## Acceptance criteria

- [x] `Legacy/` holds the files listed in `API.md`, and a script check proves each
      byte-equal to its archive member (the transcoded source by re-transcoding).
      (2026-09-17: `python tests/Fixtures/generate.py verify`, green against
      `legacy/PropStructV3.zip`.)
- [x] Reference outputs and replicas exist for every reference formulation with
      provenance. (2026-09-17: `tests/Fixtures/references/*/results.m.txt`,
      `tests/Fixtures/replicas-gsv3/*/*.m.txt`, 101 entries in `provenance.json`.)
- [x] The six seed offsets are recorded with their proofs (see "Seed offsets are
      proven"). (2026-09-17: `python tests/Fixtures/generate.py seeds locate`, all
      six streams `OK` against `Legacy/formulations/inpt.dat`.)
- [x] Lagged and independent replicas exist for every reference formulation with
      provenance (layout, `k`, jump exponent, SHA-256 of the patched executable).
      (2026-09-17: `tests/Fixtures/replicas-lagged/*/*.m.txt`,
      `tests/Fixtures/replicas-independent/*/*.m.txt` — HPEPA3 32 each, `inpt`/`P33`/
      `PSAN02n`/`HMX` 16 each, 320 files; 320 new entries in `provenance.json` with
      `layout`, `k`, `jump` and `patchedExecutableSha256`.)
- [x] The original's own null rate against these replicas is measured, the archived
      references against all `R` lagged replicas among its runs. (2026-10-02:
      `tests/Harness.Tests/NullRateCalibration`; root `ACCEPTANCE.md`.)

  ⚠ 2026-10-02: was "The HPEPA3 reference passes with no failure against its lagged replicas", the retired per-run condition, now the original's null rate → HISTORY.md#null-rate-notes-2026-10-02

  ⚠ 2026-10-02: was 15 of 197 dated 2026-09-27, then 16 (B2a), now 15 of 197 again (B2c) → HISTORY.md#null-rate-notes-2026-10-02
- [x] Replica distinctness is proven by `Fixtures.Tests`. (2026-09-17:
      `PropStruct.Fixtures.Tests`,
      `FixturesInventoryTests.ReplicasOfOneFormulationAndKindArePairwiseDistinctOutsideTheTimeLine`,
      parameterized over all three replica kinds (lagged, independent, GSV=3) and the
      five reference formulations, 15 green cases.)

  ⚠ 2026-09-17: was "Not yet: `Fixtures.Tests` does not exist", stale →
  HISTORY.md#fixtures-tests-row-stale-2026-09-17
- [x] Regenerating the HPEPA3 reference reproduces the archive's `results.m` as stated.
      (2026-09-17: `diff tests/Fixtures/references/HPEPA3/results.m.txt
      tests/Fixtures/Legacy/outputs/results.m.txt` differs only on the
      `pdoksmall`/time line.)
- [x] Every formula script case consumed by `Particle.Tests` exists and names its
      Fortran lines. (2026-09-17: `formulas_particle.py`, `cases/particle/size_law.json`,
      `bridge_geometry_volume.json`, `bridge_window.json`, each with `fortranLines` and
      `script`, checked by `Fixtures.Tests`'
      `EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript`; regeneration proven
      byte-equal by `formulas_particle.py verify`, non-degenerate by a mutate-and-revert
      of a committed value, `API.md`, "## Particle formula cases".)
- [x] Every formula script case consumed by `Statistics.Tests` exists and names its
      Fortran lines. (2026-09-18: `formulas_statistics.py`,
      `cases/statistics/categories.json`, `small_particles_probability.json` and
      `small_particles_max_size.json` (the last two added by the audit of
      `Statistics`), each with `fortranLines` and `script`, checked
      by `Fixtures.Tests`' `EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript`;
      regeneration proven byte-equal by `formulas_statistics.py verify`,
      non-degenerate by a mutate-and-revert of a committed value, `API.md`, "##
      Statistics formula cases". The category merge and the small-particle
      probability/max-size are covered. 2026-10-02: `cycle_statistics.json` covers the
      rest of `CycleStatistics.Compute`, under the same checks.)

  ⚠ 2026-09-17: this row first bundled both `Particle.Tests` and `Statistics.Tests`
  under one checkbox. Split so the `Particle` half, done by this task, can be ticked
  with its own date and evidence without claiming the `Statistics` half as well
  (AGENTS.md §6: a criterion with "and" is not one criterion once only half is met).
- [x] `dispersion.approved.txt` matches a fresh recomputation from the replicas.
      (2026-09-20: `tests/Harness.Tests/DispersionApprovedTests.MatchesTheCommittedTable`,
      132 lines, five reference formulations' own fixed count-like families plus every
      `fqdokkarm` row; non-degenerate by construction, since the same test fails loudly
      on any recomputed line that differs from the committed file, this file's own
      generation run being the first such comparison.)
- [x] `rate-table.json` exists, carries the provenance `## Rate table` (`API.md`)
      documents, and ties to the current criterion and seed-0 snapshot.
      (2026-09-24: 320 runs, `tests/Simulation.Tests/RateTableGenerator.Regenerate`;
      `tests/Harness.Tests/RateCriterionTests.RateTableTiesToTheCurrentCriterionAndSnapshot`,
      non-degenerate by a one-character edit of the recorded criterion hash, reverted
      after; consumed by the same node's rate-criterion tests, root BOOT.md, "the pass
      condition compares failure rates, not single runs".)

      ⚠ 2026-09-25: was the writer in `tests/Simulation.Tests`, now `tests/RateTableTool`, the table regenerated there → HISTORY.md#rate-table-notes-2026-09-25

      ⚠ 2026-09-26: was a tie of two digests, now three, `FixturesSha256` added, the table regenerated → HISTORY.md#rate-table-notes-2026-09-25

- [x] `generate.py`'s and `run_original.py`'s own seed arithmetic (the multiplier and
      seed limbs, `original_stream_value`/`independent_stream_value`,
      `replica_jump_exponent`, `states_for_seed`) matches `src/Random`'s, reached only
      through its published contract, state for state: the base states of both
      layouts, every lagged/independent replica state the fixtures use, and
      `run_original.py`'s states for a spread of seeds including the tree's own
      `k·2¹⁶`/`k·2¹⁷` strides (architecture audit 2026-09-24, R5: nothing checked that
      this node's Python seed arithmetic agreed with the C# generator it validates). (2026-09-25:
      `tests/Random.Tests/PythonSeedArithmeticCrossCheckTests`, 5 fast cases, via the
      `print-states` entry points this task added to both scripts (`API.md`, "##
      generate.py", "## Measurement scripts"); every `provenance.json` lagged/
      independent entry's own `"jump"` checked against the formula too, machine-read,
      not typed. Full evidence, including the mutation proofs, is
      `tests/Random.Tests/BOOT.md`'s own criterion.)

- [x] 2026-10-02: the excerpt, 15303 instructions in seven ranges, equals the
      executable's bytes, and the check was seen red on an edited byte of the committed
      file (0040BDB8) and on four mutations (`check_cycle_plane_listing.py verify` and
      `selftest`; `tests/Statistics.Tests/CyclePlaneListingTests`).
- [x] 2026-10-03: the oracle's right proofs (`cycle_plane_oracle_proofs.py`): k1, the
      constructed cycle's `da_coef` from `mp`'s home prints the archive's token for 22
      of 22 formulations (today's rule gets 19); k2, rps01/rps02 print 0.7284034 and
      0.6019315 and move without the flags; k4, the oracle's rows 795-796 and 810-816
      as the rule reproduce `epsdokfr` 295 of 295 and `epsalldok` 293 of 293, and fall
      to 0 of 98 (HPEPA3, ZX in binary32) and 0 of 97 (DOKM 0.1 % off). k3, the
      oracle's `da_coef` against the scratch listing model, bit for bit: 980 of 980
      variants, T1 133, T3 30, T4 41, home 39; 20,000 of 20,000 crafted, `gdokleft`
      237, T2 293, S1 95, S2 837 (a scratch driver, not kept in the tree).
- [x] 2026-10-03: the red proofs: r1 and r2 in `x87_machine.py selftest`, r3, r5 and
      the guard on a typed 3.14159 and a typed map name in `cycle_plane_oracle.py
      selftest`; r4, the oracle red on the port of 2026-10-02 at the isolated sites,
      is the approved ratchets `ListingOracleTests` kept until the plane followed the
      table (463 and 19 lines, empty since 2026-10-03).
- [x] 2026-10-03: every site the oracle leaves `unreached` has a positive control in
      `cases/statistics/cycle_statistics.json` or is exact by the table's own `order`
      (`formulas_statistics.py generate` refuses otherwise, and a control whose flipped
      store or schedule moves no bit of the twin's Original report); `verify` reproduces
      the six statistics case files byte for byte. Red on the C# of cdc6237: 28 of 82
      tests of the four consumer classes.
- [x] 2026-10-03: `cases/statistics/cycle_plane_oracle.json` regenerates byte for byte
      (`cycle_plane_oracle.py verify`, 465 s, 26 cases, red on a map pin the fixture lacks)
      and `src/Statistics/ACCEPTANCE.md`'s A2 holds: 58 isolated sites, 18 `unreached`.
      ⚠ 2026-10-03: was 447 s, 19 cases and 407 s, 22 cases, now 465 s, 26 cases → HISTORY.md#oracle-verify-figures-2026-10-03

      ⚠ 2026-10-03: was told with the same-day regeneration (`generate`, seed
      20261003; 15 of 18 replayed cases had failed `verify`) →
      HISTORY.md#oracle-regenerated-2026-10-03

- [x] 2026-10-03: the oracle's provenance digests the twin (`formulas_statistics.py`),
      restamped by `cycle_plane_oracle.py restamp` with the 22 cases byte for byte (the
      sha256 of `cases` 7b5b5793… before and after); `verify` green. The pre-loop
      survey (`preloop_survey.py`, 8.3 s for the files, `verify` byte for byte) holds all 49
      formulations; its first run found the twin's `.dat` reader wrong on `C166.dat`
      (a record of fifteen values for `NMM = 14`), now the READ statements' own.

- [x] `Legacy/` holds only the archive's data and each file equals its member (stage S2
      of the delivery, root `BOOT.md`, `## Delivery`): `archive-members.sha256` lists
      them (124 files, 49 formulations and 75 outputs), the fast set compares the files
      with it (`LegacyManifestTests`), and `generate.py verify` compares the members
      under `Category=Legacy`; red once on a flipped byte of a data file and once of the
      list (2026-10-04, `Fixtures.Tests` `LegacyManifestTests`, `LegacyArchiveTests`).
- [x] The three fixtures derived from the original, `archive-members.sha256`,
      `cases/source/executable_lines.json` and `cases/random/source_limbs.json`,
      regenerate byte for byte by `generate.py derive` (`verify` runs `derive --check`)
      under `Category=Legacy`, and their fast consumers read them (2026-10-04,
      `generate.py derive --check`: 3 fixtures; seven consumer classes: the four
      `LineMapCoverageTests`, `SourceLimbsTests`, `ArithmeticTests`, `DrawStreamTests`).
- [x] The fast set is green with `PROPSTRUCT_LEGACY_DIR` unset and the five files
      absent, and the category run with it unset is red on every fact, so each generator
      and check of this node that reads one of them carries `Category=Legacy`
      (2026-10-04: the fast set 11 projects passed, none failed; `Category=Legacy` with
      the variable unset, 19 facts, 0 passed, 19 failed, the nineteenth added by the
      fix-up; `tools/legacy/BOOT.md`, last criterion).
- [x] The oracle fixture and the pre-loop survey are restamped after the listing table
      drops its `fortran` column (`src/Statistics/API.md`) and their `verify` runs are
      green under `Category=Legacy` (2026-10-04: `cycle_plane_oracle.py verify`, 475 s,
      26 cases; `preloop_survey.py verify`; the `cases` member of the oracle fixture
      hashes `eb340e7e715a` before and after, and the survey's `formulations` and
      `jzz2Controls` `527fabac598e` and `105edfdc0a66` before and after; the files'
      SHA-256 `fa3b28f5` to `653d8362` and `ff629171` to `b58f2a54`, the provenance
      block the only difference: the site table's digest and those of the digested
      scripts; the executable's and the excerpt's unchanged).

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.
