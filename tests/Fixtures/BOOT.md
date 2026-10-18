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

→ [ACCEPTANCE.md](ACCEPTANCE.md)

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
