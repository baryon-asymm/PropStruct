# BOOT.md — Fixtures

## Purpose

The reference data of the port and their provenance: the original's data (the
formulations and the outputs shipped in its archive), the script that runs the original,
the reference outputs (GSV=2 as shipped) and the replicas of the reference formulations
(lagged-seed, independent-seed and GSV=3), the exclusion table of the statistical
criterion, and the formula scripts that compute expected values for constructed cases of
`Particle` and `Statistics`. The original program itself, its source, executable,
runtime, archive and listing excerpt, lies outside the repository from 2026-10-04
(stage S2 of the delivery moves it): `tools/legacy` identifies it, and every generator
and check here that reads it carries `Category=Legacy`. The executable's own behaviour
in the per-cycle plane is here as committed data: the oracle that executes the listing
excerpt and the fixtures it writes (`API.md`, "## Cycle-plane listing", "## Listing
oracle"). Every other test node takes expected values from here and nowhere else.

## Invariants

- **`Legacy/` holds the archive's data.** Every file under `Legacy/`, the formulations
  and the outputs and nothing else, equals byte for byte its member of
  `PropStructV3.zip`: its SHA-256 is in `archive-members.sha256` (the fast set compares
  the files) and the member itself is compared in `Category=Legacy` (`generate.py
  verify`). The archive, the executable, its runtime, the UTF-8 source and the listing
  excerpt live outside the repository (`tools/legacy`).

  ⚠ 2026-10-04: was the executable, its runtime and the transcoded source under `Legacy/`, now outside the repository → HISTORY.md#legacy-out-of-tree-2026-10-04
- **Every `.m` this node writes or holds carries an appended `.txt`.** Not only
  `Legacy/outputs/`: `references/<name>/results.m.txt` and `replicas/<name>/<k>.m.txt`
  too. `protocol_lint.py`'s default source extensions include bare `.m`
  (MATLAB/Objective-C); without the suffix, every one of these per-formulation
  directories — none of which carries its own `BOOT.md`/`API.md`, being data, not a
  level of abstraction — turns into an undocumented node the moment its first `.m`
  file is written.

  ⚠ 2026-09-17: was the `.txt` suffix on `Legacy/PropStructv3.for.txt` alone, now
  every `.m` → HISTORY.md#txt-suffix-generalized-2026-09-17
- **Every generated output has provenance.** Each reference and replica `results.m` was
  produced by `generate.py` with the original's executable (`tools/legacy`);
  `provenance.json` records for it the SHA-256 of the input actually run, of the
  executable and of the output, the GSV used, the standard input sent, the date and
  the script's commit; the fast set compares the executable's digest with the manifest.
- **Replicas are distinct.** Replicas of one formulation and kind are pairwise
  different outside the time line. GSV=3 replicas: the original seeds from the clock
  and reseeds at every cycle (lines 409–413), so the script starts them at least 1.1 s
  apart. Seed-patched replicas: distinct by construction (distinct jumps).
- **Seed-patched replicas change nothing but the six seeds.** A lagged or independent
  replica runs a copy of `PropStructV3.exe` whose six GSV=2 seed blocks (the DATA of
  `u01…u96`, ten little-endian `int32` limbs each, stored `u9…u0`) are overwritten and
  every other byte is unchanged; the input is the shipped `.dat` unchanged. Replica
  `k = 1..R` jumps the layout's six initial states by `k · 2⁸⁰ + 2⁷⁹` (`src/Random/BOOT.md`,
  "Replica jumps"): **lagged** = the original seeds jumped (the `Original` layout's lag
  structure kept); **independent** = the `Independent` layout's states jumped. The
  states are computed from their definitions in `src/Random/BOOT.md` (powers of `a` and
  the SHA-256 rule), never copied from C# code.
- **Seed offsets are proven, not assumed.** Located on 2026-09-17 in the shipped
  executable: `u01` 0x20b58, `u02` 0x20b80, `u03` 0x20ba8, `u04` 0x20bd0, `u05` 0x20bf8,
  `u06` 0x20c20 (file offsets of the `u9` limb; each block is 40 bytes, so the six
  blocks are contiguous). 0x20b08 holds the multiplier `m` of `random2` (the same
  limbs as `u05`, since stream 5 = a¹ = a) and must never be patched; 0x20b30 holds
  counters. The script refuses to generate until every offset is recorded here with
  its proof: patching a block alone changes the output, patching it with its own
  original limbs leaves the output byte-equal outside the time line (and
  `pdoksmall(1:2)`, a pre-existing, seed-independent non-determinism, see below).
  `python tests/Fixtures/generate.py seeds locate` runs this proof for all six streams
  on `inpt.dat` (`inpt` because it is the smallest shipped formulation, so the round
  trip of thirteen executable runs takes a few seconds).
- **Seed-patched replicas are shifts** (measured 2026-09-23). A replica jump moves every
  output of a stream by one constant (`src/Random/BOOT.md`, "Seed and replica jumps are
  rigid shifts"), so the `R` replicas of a set are `R` shifts of one sequence per stream,
  and for the `Independent` set's stream 4 (X2) they span only 0.505–0.608 of the circle.
  Where a printed quantity is a function of one such stream alone, `sd_R` is the spread
  of a short arc, not of independent runs. Measured on `epsx(4)` = `2·|mean X2 − ½|`,
  a function of stream 4's shift and the count of X2 draws only: a throwaway recomputing
  it from the generator alone reproduced all 41 printed values it was given (the 32
  HPEPA3 `Independent` replicas to three or four digits, nine port runs to five). Over
  2¹⁶ shifts spanning the whole circle at `N = 1.25·10⁸` it has mean 3.8e-5 and sd
  2.9e-5 (theory 4.1e-5 mean). The replica arc averages 8.6e-5 (92nd percentile of the
  circle) with a replica sd of 1.1e-5; the port's seeds 0–4 sit at shifts 0–0.013 and
  average 1.0e-5 (18th percentile). So the criterion fails the port's `epsx(4)` at seeds
  1–4 against a band 2.6 times narrower than the quantity's real spread, and the port
  computes the value exactly as the original would at the same shift. The reach beyond
  that one cell was measured the same day by comparing each cell's replica variance with
  the clock-seeded GSV=3 set's (one-sided F test, Bonferroni over the cells tested, α =
  10⁻³). `Independent` set: `epsx(4)` has an sd ratio of 0.25–0.53 on four of five
  formulations, significant on HPEPA3; besides it zero to two other cells per
  formulation, scattered. So the shift does not visibly narrow any model
  quantity at `R` = 16–32. The lagged set shows 9–128 narrower cells per formulation,
  mostly `coef`, but it also carries the `Original` layout's seed correlations, which
  change the program, so this test cannot tell lattice from bias there.
  Settled 2026-09-24 against the right reference: the port's own sixteen seeds `k·2¹⁶`,
  which spread every stream evenly over its circle, run under `--precision original` in
  the matching layout. Cells compared: 611–1119 per formulation, category axes left out.
  The replicas' spread matches that reference in both sets:
  - median sd ratio 0.93–0.98 for the lagged set and 0.98–1.02 for the independent set;
  - tenth to ninetieth percentiles about 0.7 to 1.35, the width F-noise at 16 against 16
    alone gives;
  - significantly narrower, in 3700 lagged and 3770 independent cells: `epsx(4)` twice,
    already excluded, and one cell of `inpt`'s `dokkarm43`.

  So the shift structure narrows no band of a model quantity. The earlier 9–128 lagged
  cells were the `Original` layout's own program against the clock-seeded generator, not
  the lattice.

  ⚠ 2026-09-17, corrected same day: was "`u06` is not at 0x20c20: patching that block
  changed nothing", now `u06` at 0x20c20, proven as the other five are →
  HISTORY.md#u06-offset-correction-2026-09-17
- **Cycle-plane pairs are measurements, not references** (2026-10-02). Each pair holds
  the original's output and the port's on one input at the `N` and `KXX` where every
  integer cell agrees (`API.md`, "## Cycle-plane pairs"); the port's half is regenerated
  with the plane code and tied to the live port by `Simulation.Tests`.
- **The listing excerpt is the executable's bytes** (2026-10-02). Every instruction of
  `PropStructV3.cycle-plane.listing.txt` equals the executable's bytes at its address
  through the PE section table; its header names the command, the tool version and the
  SHA-256 of the executable and of `dforrt.dll`. `check_cycle_plane_listing.py verify`
  needs no dumpbin; only `extract` does. Both files lie outside the repository
  (`tools/legacy`) and the check is `Category=Legacy`.
- **The listing oracle executes those bytes, and its hooks never supply an expected
  value** (decided 2026-10-02, built 2026-10-03). `x87_machine.py` runs the excerpt's
  instructions over the executable's `.rdata` and `.data`, and every decoded instruction
  must render to the excerpt's dumpbin text; `cycle_plane_oracle.py` injects by map
  name, selects seeded cases (a hook only decides which case isolates a site) and writes
  `cases/statistics/cycle_plane_oracle.json`, whose cases `verify` replays. What the
  machine computes is the answer, never typed. The excerpt holds PARAM and the
  machine executes it, so `ZSS`, `ZX` and `Z11` are the executable's; its inputs, `ALFA`
  among them, come from the setup model, as every injected value does (`SIZE` is a stub).
  Everything the pre-loop and the plane compute is the machine's, and
  `preloop_survey.py` runs the pre-loop over every shipped `.dat`. The fixtures these
  write are committed; their `verify` runs need the original and are `Category=Legacy`,
  and the fast set compares the digests they record with the manifest.

  ⚠ 2026-10-03: was "the setup values PARAM computes are injected from the setup model
  (PARAM is outside the excerpt)", now PARAM executed (stage 5b, 0x4181A5–0x418577).
- **Regeneration reproduces the archive.** The reference `results.m` of HPEPA3
  regenerated by the script equals the archive's `results.m` except the time line and
  `pdoksmall(1:2)` (observed 2026-09-17).
- **Thresholds are not stored.** The thresholds of the root criterion are computed at
  test time by `Harness` from the replicas; this node stores outputs, never derived
  tolerances.

  ⚠ 2026-09-20: was read at a glance as a stored threshold, now declared a generated tripwire nothing reads back → HISTORY.md#dispersion-tripwire-2026-09-20
- **Exclusions carry evidence.** `exclusions.json` lists each excluded quantity with
  the reason and its computed evidence. The first entry: `pdoksmall` reference cells
  below `1e-30` compare as zero, because they hold the uninitialized `pdoksmall(1)`
  copied forward by the clamp of lines 1062–1064 (rc166.m.txt: six cells of
  0.762E-38). The second: `epsx(4)` is dropped whole against `Independent` replicas
  only, because that set's own stream-4 shifts span a tenth of the circle ("Seed-
  patched replicas are shifts" above; evidence in `../Random.Tests/ReplicaShiftTests.cs`).
- **Formula scripts are transcriptions of formulas, not of the program.** A script
  evaluates the Fortran formulas of named lines for a constructed case in `double`, in
  the Fortran order, its terms associated as the transcribed node's `API.md` fixes
  (`src/Statistics/API.md`, "## Cycle"), never as the program's output shows, and
  writes the case with its expected values as JSON. No script simulates particles.

  ⚠ 2026-10-02: was "in the Fortran order" alone, broken unseen by `qdokkarm` (899)
  since 2026-09-18 and openly by ten report terms read off `Compute`'s output.
- **A measurement script never accepts a flag it does not honour.** `run_original.py`
  and `run_port.py` (`API.md`, "## Measurement scripts") take `--layout`, `--seed`,
  `--n` and `--kxx`; every one of the four must change the run for every value it is
  given, with no value that the parser accepts and the run then ignores. `--n`/`--kxx`
  already satisfy this by construction: `run()` applies `generate.replace_header_token`
  whenever the value is not `None`, the same for every value, so there is no branch on
  the value itself to get wrong. `--seed`/`--layout` need `states_for_seed` (or, on the
  port side, the CLI's own `OriginalSeeds`/`IndependentSeeds.ForParticle`) to be
  evaluated the same way for every `(layout, seed)` pair, with no special case for a
  particular `seed`.

  ⚠ 2026-09-21: was `states_for_seed` special-casing `seed == 0` to "no patch", which
  silently ran the `original` layout for `--layout independent`, now the special case
  deleted and `check_measurement_scripts.py` its check →
  HISTORY.md#states-for-seed-special-case-2026-09-21

## Dependencies

- [Statistics](../../src/Statistics/API.md) — the address map
  `CyclePlaneListing.map.txt` ("## Cycle listing map and table"), read by the listing
  oracle and by nothing else here.
- [legacy](../../tools/legacy/API.md) — the manifest and `legacy_file`, through which
  every script here reaches the original's five files, outside the repository.

Outside the tree: the original's `PropStructV3.exe` and `dforrt.dll` (Digital Visual
Fortran 6 runtime, 32-bit Windows), its source and its listing excerpt, at
`PROPSTRUCT_LEGACY_DIR`; Python 3.8+ (standard library only) for the scripts; dumpbin
14.12.25835.0, for regenerating the listing excerpt only.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Reference formulations: HPEPA3, inpt, P33, PSAN02n, HMX. Lagged replicas `R = 32`
  for HPEPA3 and 16 for the others; independent replicas the same counts; GSV=3
  replicas as generated on 2026-09-17 (cross-check only). A GSV=3 input is a temporary copy of the shipped `.dat`
  whose sixth value of the `NMM JZZ KXX N NNZ GSV` record is replaced by `3` and
  nothing else is changed.
- The executable runs in a fresh temporary directory holding a copy of itself, its
  runtime and the input, with standard input `<name>\ny\n` (file name, default
  parameters), since it writes `results.m` and asks before overwriting.
- Up to 12 runs in parallel, starts staggered as above.
- Generation is a manual, recorded step, not part of any test run.

⚠ AGENTS.md §12 deviation, declared: this node and `Particle`, `Statistics`, `Output`, `Input` read the Fortran source as their specification; since 2026-10-04 it lies outside the repository with the executable, its runtime, the archive and the listing excerpt ([legacy](../../tools/legacy/API.md)), and every check that reads one of them carries `Category=Legacy`.

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

## Taboos

- No edited expected value: outputs change only by regeneration.
- No plane literal, rounding rule or formula in `x87_machine.py` or
  `cycle_plane_oracle.py`: addresses and map names only.
- No replica produced by the port.
- No stored tolerance (`dispersion.approved.txt`, ## Invariants, "Thresholds are not
  stored": a generated tripwire nothing reads back, not an exception to this taboo).

## Rate table

`rate-table.json`'s own schema is `API.md`'s ("## Rate table"); this section owns one
fact that document does not: how `CriterionSha256` is computed, since a wrong reading of
it would silently let the tie (`tests/Harness.Tests/RateCriterionTests.
RateTableTiesToTheCurrentCriterionAndSnapshot`) stop guarding anything.

`StatisticalCriterion.cs` was one file until audit finding R3 (2026-09-25) split it, by
concern, into a partial class of several files (`tests/Harness/BOOT.md`, "## Null rate of
the original ... The tie"), then (2026-09-26) into named single-responsibility classes,
`StatisticalCriterion` staying only as the entry-point façade (`tests/Harness/API.md`).
`CriterionSha256` is not any one file's own hash: it is one SHA-256 over every `*.cs` file
directly under `tests/Harness`, minus a declared exclusion list, ordered by filename
(ordinal), each file's own name and LF-normalized content folded into the digest in that
order.

⚠ 2026-09-26: was the glob `StatisticalCriterion*.cs`, now every `*.cs` file minus a declared exclusion list → HISTORY.md#rate-table-notes-2026-09-25

Excluded today, each because no comparison this criterion runs ever reaches it (a coder
adding a fifth entry states the same kind of reason in the code, beside the constant):
`CpuHost.cs` (the ILGPU CPU-accelerator host, used by kernel-path tests, not by any
comparison), `BitSnapshot.cs` (a bit-snapshot helper for `Random.Tests`/`Particle.Tests`),
`SplitMix64.cs` (test-only deterministic randomness, not the port's own generator),
`DispersionTable.cs` (the dispersion-table report generator: its `Compute()` is called by
`DispersionApprovedTests` and `tests/DispersionTool`, never by a comparison), and
`PythonScript.cs` (runs the tree's Python scripts from the generator tests, 2026-10-02;
`SetComparison.cs`, `CanonicalAxisSetCells.cs` and `SetCriterionReport.cs` are excluded
in both lists as well, named in the code beside each constant; so, since 2026-10-04, are
`ResultsMTimeLine.cs`, `CudaRequirement.cs` and `CudaRefusalBranches.cs`). Excluding
them matters in practice, not only in principle: each costs a full, ~13-minute, 320-run
regeneration when it changes for a reason that has nothing to do with a verdict.

`tests/RateTableTool/Program.cs` (the writer) and `tests/Harness.Tests/RateCriterionTests`
(the reader) each compute this same digest independently from the rule stated above,
including the identical exclusion list, never from a shared implementation — the same
relationship every reader of this file's own generated fixtures already has with the
schema `API.md` documents for it: the rule is data every consumer re-derives, not a class
either project imports from the other. `RateCriterionTests` also asserts the exclusion
list names exactly the `tests/Harness/*.cs` files that still exist under those names, so a
rename or deletion that leaves a stale entry turns that test red rather than silently
narrowing the set the tie's own digest already reads correctly.

`FixturesSha256` covers what `CriterionSha256` cannot see: the fixture data the
criterion's own comparisons read, none of which was in any digest before this field
existed, so an edit here left the tie green while every verdict quietly went stale. It is
one SHA-256 over `exclusions.json`, every file under `references/`, and every file under
each replica directory the rate runs' own comparisons pass a `ReplicaKind` for — today
`replicas-lagged/` (`ReplicaKind.Lagged`, the `Original`-layout rows) and
`replicas-independent/` (`ReplicaKind.Independent`); `replicas-gsv3/` carries no rows,
since no comparison a rate run makes ever constructs `ReplicaKind.Gsv3`. Files fold into
the digest ordered by their path relative to `tests/Fixtures`, ordinal, forward-slash
normalized; each file's own relative path and LF-normalized content are what fold in, the
same shape `CriterionSha256` already uses for filenames.

`tests/RateTableTool/Program.cs` and `tests/Harness.Tests/RateCriterionTests` each compute
`FixturesSha256` independently too: the reader has `tests/Harness`'s own
`FixtureReplicas.ReplicaDirectory` in reach (`InternalsVisibleTo`) and calls it; the writer
does not (no such grant, and `tests/Harness` is not this task's to change) and restates the
same two-case mapping locally instead, exactly as `tests/Harness.Tests/NullRateCalibration`
already restates it for the same two kinds. `RateCriterionTests` also asserts that its own
list of used `ReplicaKind` values, together with `Gsv3`'s declared exclusion, accounts for
every value the enum declares today, so a third kind a future rate run starts passing
turns that test red until it is classified, rather than silently missing from the hash.
