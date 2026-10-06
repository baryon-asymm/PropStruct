# API.md — Fixtures

Directory `tests/Fixtures`. Files and scripts, no assembly.

## Layout

### Extracted and generated ✅

```text
tests/Fixtures/
  Legacy/
    formulations/*.dat            every .dat of the archive
    outputs/*.m.txt               every .m of the archive (evidence, not references)
  archive-members.sha256          SHA-256 of every file under Legacy/, by archive member name (## Original out of the tree)
  cases/source/executable_lines.json   the executable line numbers of the source, for the line-map coverage tests
  cases/random/source_limbs.json       the limbs of the source's seeds and multiplier, for Random.Tests
  generate.py                     runs the executable: references, seed-patched and GSV=3 replicas; verify, derive
  references/<name>/results.m.txt the GSV=2 output of each reference formulation
  replicas-lagged/<name>/<k>.m.txt       k = 1..R, Original layout's seeds jumped by k·2⁸⁰ + 2⁷⁹
  replicas-independent/<name>/<k>.m.txt  k = 1..R, Independent layout's states jumped likewise
  replicas-gsv3/<name>/<k>.m.txt  k = 1..R, GSV=3 outputs (moved from replicas/, provenance updated)
  provenance.json                 one entry per generated output
  exclusions.json                 excluded quantities with reason and evidence
  dispersion.approved.txt         generated dispersion tripwire, produced and checked by Harness.Tests (below)
  cases/input/*                   constructed .dat files and expected values for Input.Tests
  cases/harness/student_t.json    scipy 1.18.1 Student quantiles, for Harness's own StudentDistribution
```

⚠ 2026-09-17: `replicas/` (GSV=3 replicas) renamed to `replicas-gsv3/`, and
`replicas-lagged/`/`replicas-independent/` joined it here from a "Seed-patched
replicas ⏳" block below, all three named after their kind (BOOT.md, ## Invariants,
"Replicas are distinct"): HPEPA3 (`R = 32`) and `inpt`/`P33`/`PSAN02n`/`HMX` (`R = 16`
each) are generated for both seed-patched layouts, 320 files; `provenance.json`'s 96
GSV=3 entries were updated to the new path in the same commit as the 320 new ones.

⚠ 2026-09-18: `cases/input/*` and `cases/harness/*` moved here from "Not yet written"
below: `Input.Tests`' coding session populated `cases/input/`, and `Harness.Tests`'
coding session populated `cases/harness/student_t.json` (a scipy reference table for
`Harness`'s own Student quantile, not a transcription of a Fortran formula — it names
its own generating command and library version instead of Fortran lines,
`tests/Harness/BOOT.md`). `cases/particle/` and `cases/statistics/` moved here in
their own later waves, recorded where each one is listed below.

⚠ 2026-09-17: `formulas_particle.py` and `cases/particle/*.json` moved here from "Not
yet written" below: `Particle.Tests`' L0 row (`tests/Particle.Tests/BOOT.md`) needed
its formula cases before `Particle` could be checked against them. `formulas_statistics.py`
and `cases/statistics/*.json` stay below: `Statistics` is not designed yet.

⚠ 2026-09-18: `formulas_statistics.py` and `cases/statistics/categories.json` moved
here from "Not yet written" below: `Statistics`'s coding session needed the category
merge's formula cases before `Categories.MergeAndDescribe` (BOOT.md's own "## Categories")
could be checked against them (`## Statistics formula cases` below). Only the category
merge is covered; the rest of `CycleStatistics.Compute`'s pipeline is not (this node's
`BOOT.md` does not claim otherwise — the only claim about scope lives in
`src/Statistics/BOOT.md` and `tests/Statistics.Tests/BOOT.md`, not repeated here).

⚠ 2026-09-18 (audit of `Statistics`): `cases/statistics/small_particles_probability.json`
and `small_particles_max_size.json` added, from the same script, for
`SmallParticles.Probability`/`MaxSize` — the two outputs of `Statistics`' end-of-cycle
processing that feed back into the model loop, found during the audit to have no
independent check even though the rest of `CycleStatistics.Compute`'s pipeline
genuinely does not need one (`src/Statistics/BOOT.md`'s own acceptance criteria carry
the reasoning).

```text
tests/Fixtures/
  formulas_particle.py            formula script for Particle cases (## Particle formula cases)
  cases/particle/*.json           constructed cases with expected values for Particle.Tests
  formulas_statistics.py          formula script for Statistics cases (## Statistics formula cases)
  cases/statistics/categories.json                    constructed cases with expected values for Statistics.Tests
  cases/statistics/small_particles_probability.json   constructed cases with expected values for Statistics.Tests
  cases/statistics/small_particles_max_size.json      constructed cases with expected values for Statistics.Tests
  cases/statistics/cycle_statistics.json              constructed cycles with every report value, for Statistics.Tests
  run_original.py                 single-seed original run outside the statistical criterion (## Measurement scripts)
  run_port.py                     single-seed port run, matched to run_original.py's own seed (## Measurement scripts)
  check_measurement_scripts.py    regression check: no flag of the two scripts above is accepted and silently ignored (## Measurement scripts)
  cycle_plane_pairs.py            the original and the port on one input at the N, KXX where every integer cell agrees (## Cycle-plane pairs)
  cycle-plane-pairs/<name>/original.m.txt, port.m.txt   one pair per formulation of that script's PAIRS, with provenance.json and ladder.txt beside them
  cases/output/nonzero-tail-probability/results.m.txt   one original run with a nonzero tail probability, for Output.Tests
  cases/output/nondefault-pocket-coefficient/results.m.txt   one original run with a non-default P[karm-in-karm] coefficient, for Output.Tests
```

⚠ 2026-09-20: `run_original.py`/`run_port.py` added. An earlier session's own rig for
this exact measurement (`tests/Harness/HISTORY.md`, "how many accepted particles pass
before the port and the original first print different integer counts") was written
and used, then left outside the repository as a throwaway; this entry commits it so
the measurement it enabled — and the ones built on it since, in the same `HISTORY.md`
— can be repeated (AGENTS.md §13, "a check that has never been seen red/re-run is
indistinguishable from an absent one" applies just as much to an apparatus as to a
check).

⚠ 2026-09-21: `check_measurement_scripts.py` added, after `tests/Harness` found that
`run_original.py --layout independent --seed 0` silently ran the `original` layout
instead (`BOOT.md`, ## Invariants, "A measurement script never accepts a flag it does
not honour"): an apparatus with no check of its own had already produced a whole
class of spurious results once, so the fix comes with the check that would have
caught it, not just the corrected code.

Scripts stay at the node's root: a subdirectory holding a `.py` file would be a node
of its own (AGENTS.md §1). Every `.m` file this node writes or holds carries an
appended `.txt`: `protocol_lint.py`'s default source extensions include bare `.m`, so
without it every `<name>/` directory above would turn into an undocumented node the
moment it holds one (BOOT.md, ## Invariants, 2026-09-17).

⚠ 2026-09-24: `cases/output/nonzero-tail-probability/results.m.txt` added
(`src/Output`'s O-1: `## Measurement scripts`' own `run_original.py --alfa`, above).
Not under `references/` — that name is reserved for the reference formulations'
GSV=2 output at their own defaults (`## Invariants`, "Every generated output has
provenance"; root BOOT.md, "Statistical reference criterion") — and not a throwaway
either, unlike every other `run_original.py` output (`## Measurement scripts`: "Neither
script parses `results.m` itself ... a run of either is a throwaway measurement, not a
fixture"): this one *is* committed and carries a `provenance.json` entry the same shape
a reference's does, plus `"alfa"` for the menu answer, because the whole point is the
one archived number it is evidence for (`src/Output/BOOT.md`, "## Tail-probability
header line and gate"). `cases/<node>/` already holds a fixture that is neither a
reference nor a replica for `Input.Tests`/`Harness.Tests` (`cases/input/`,
`cases/harness/`); this is `Output.Tests`' own.

⚠ 2026-09-24, later: `cases/output/nondefault-pocket-coefficient/results.m.txt` added
(`src/Output`'s "print the stored setup" design decision, `## Measurement scripts`'
own `run_original.py --karmcoef`, above): the positive control that decision's root
taboo asks for ("every check ... proven twice ... a positive control, passed through
the same path and read where the consumer reads"), a value binary32 cannot hold
exactly, distinct from the `eps` case above and from every archived default (`inpt`,
menu item [7] `k7` at `inpt`'s own shipped `N`/`KXX`, unmodified, so
`provenance.json`'s `inputSha256` matches the shipped `.dat` directly, the same
invariant `Fixtures.Tests/ProvenanceTests` holds every entry to). `provenance.json`'s
entry carries `karmcoef` alongside the shared fields, the same "one entry per
generated output" shape as the `alfa` entry.

⚠ 2026-09-24, review: the value was first `12.345`, instead of the compiled-in `8.2`,
and that did not discriminate: DVF's list-directed `REAL*4` field prints seven
significant digits in fixed form at that magnitude, and binary32(12.345) rounds to the
same seven digits as the unrounded double (`src/Output/BOOT.md`'s own dated
correction carries the full account). Regenerated at `karmcoef = 0.03`, below the
field's own 0.1 threshold where it switches to eight-digit exponent form and the
binary32 error shows.

## Original out of the tree ✅

Since stage S2 of the delivery (root `BOOT.md`, `## Delivery`, 2026-10-04) `Legacy/`
holds the archive's data only, and every other section of this document that names
`Legacy/PropStructV3.exe`, `Legacy/dforrt.dll`, `Legacy/PropStructv3.for.txt` or
`Legacy/PropStructV3.cycle-plane.listing.txt` names that file in the directory
`PROPSTRUCT_LEGACY_DIR` names ([legacy](../../tools/legacy/API.md)).

`archive-members.sha256` is `<hex sha256>  <member name>` per data member of the
archive, sorted by name, the member name being the file's name under `Legacy/` with the
`.txt` appended to an output removed. `cases/source/executable_lines.json` holds
`script`, `source`, `lineCount` and `executableLines`, the numbers of the lines that are
not blank and do not start with `c` or `C`. `cases/random/source_limbs.json` holds
`script`, `fortranLines` and `limbs`, an object by line range (`"52-53"`, ...,
`"1649-1650"`, the `SOURCE_LIMB_RANGES` of `generate.py`) of the integers written
between slashes in those lines, the lines concatenated, in order. The fast set reads the
three committed files and the original's own files never; they name lines and hold limbs
and hashes, no statement of the source (`tools/legacy`, the quotation rule).

⚠ 2026-10-04: was "Layout after the delivery ⏳", the plan of this section, now built by
stage S2: `generate.py derive` writes the three files and `generate.py verify`
regenerates them with the comparison of the data of `Legacy/` against the archive's
members.

## Particle formula cases ✅

`formulas_particle.py` transcribes three Fortran subroutines into Python `double`,
independently of any C# (`BOOT.md`, ## Invariants, "Formula scripts are transcriptions
of formulas, not of the program"): `SIZE` (lines 1761-1776, the base/neighbour size
law), `VM` (lines 1588-1629, the bridge volume) and the bridge window build (lines
582-611) together with `DM` (lines 1573-1586, the pocket-size interpolation the window
feeds). It writes three files under `cases/particle/`, one per subroutine (or pair):
`size_law.json`, `bridge_geometry_volume.json`, `bridge_window.json`.

```console
python tests/Fixtures/formulas_particle.py generate   # (re)writes the three files
python tests/Fixtures/formulas_particle.py verify      # regenerates into a temp dir, fails on any byte difference
```

Every file shares this envelope:

```json
{
  "script": "formulas_particle.py",
  "fortranLines": "1761-1776",
  "subroutine": "SIZE",
  "convention": "arrays and fraction/cell indices are the Fortran 1-based convention of the transcribed subroutine, not the port's own 0-based one (this node's API.md, '## Particle formula cases').",
  "cases": [ /* one object per constructed case, shape below */ ]
}
```

`fortranLines` and `script` are read by `Fixtures.Tests`' citation rule
(`FixturesInventoryTests.EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript`); every
array and 1-based index below (`bounds`, `cumulative`, `fraction`, `qks1`, `minCell`,
`maxCell`, `boundaries`) is in the **Fortran** numbering the subroutine itself uses,
not the port's `Nkarm`/`Ndok`-sized, zero-based one (`src/Particle/BOOT.md`,
"Indices are the Fortran indices minus one") — a consumer converts, this node does not
guess the port's own convention for it.

**`size_law.json`** (`SIZE`, one case per object):

```json
{
  "name": "uniform_in_d_interior",
  "sizeLaw": 2,
  "bounds": [10.0, 50.0, 160.0, 315.0],
  "cumulative": [0.0, 0.6, 1.0],
  "x": 0.3,
  "x1": 0.25,
  "expected": { "fraction": 1, "diameter": 20.0 }
}
```

`sizeLaw` is `JZ` (2 = uniform in D, any other value = uniform in 1/D², `src/Particle/BOOT.md`,
## Constraints); `bounds` is `DOK` (length `2·NM`, `DOK(2i-1)`/`DOK(2i)` the lower/upper
bound of fraction `i`); `cumulative` is `Z` (length `NM+1`); `x`, `x1` are the two
draws `SIZE` takes; `expected.fraction` is the Fortran `MINV` (1-based) the scanning
loop of lines 1765-1767 picks (the *last* matching `I`, since the loop has no `break`);
`expected.diameter` is `D`.

**`bridge_geometry_volume.json`** (`VM`, one case per object):

```json
{
  "name": "normal_no_swap_needed",
  "r1": 5.0, "r2": 3.0, "rk": 2.0, "a": 1.0,
  "expected": { "jj": 1, "swapped": false, "bb": 3.7387911774959326, "vmkm": 39.079188688651534 }
}
```

`r1`, `r2`, `rk`, `a` are `VM`'s first four arguments as the caller supplies them,
before the swap of lines 1591-1596 (applied to local copies only — the port's own
departure from Fortran's call-by-reference, `src/Particle/BOOT.md`, ## Line map);
`expected.swapped` is whether that swap fired (`r2 > r1`), reported so a consumer can
check its own swap independently of the final geometry. `expected.jj` is `JJ`;
`expected.bb` and `expected.vmkm` are `BB`/`VMKM` **when the branch that sets them
ran**, `null` otherwise: `jj = 0` from `A ≥ 2·RK` (line 1597) never computes either;
`jj = 0` from `BB < 0` (line 1612) computes `bb` but not `vmkm`. `error` (line 1611)
is never read by anything and is not in the schema (`src/Particle/BOOT.md`, ## Line
map: "not ported").

**`bridge_window.json`** (the window build plus `DM`, one case per object):

```json
{
  "name": "ordinary_window_truncation_at_natural_end",
  "cellSize": 1.0, "ak3": 1.0, "ak4": 3.0, "dr": 1.0,
  "qks1": [1.0, 2.0, 3.0, 4.0, 5.0],
  "expected": {
    "emptyWindow": false, "minCell": 2, "maxCell": 4, "cellCount": 4,
    "boundaries": [2.0, 3.0, 4.0, 5.0],
    "cumulative": [0.0, 0.2222222222222222, 0.5555555555555556, 1.0]
  },
  "samples": [ { "x3": 0.25, "expected": { "pocketSize": 3.0833333333333335 } } ]
}
```

`cellSize`, `ak3`, `ak4`, `dr` are `Di`, `AK3`, `AK4`, `Dr`; `qks1` is `QKS1` (1-based,
length `Nkarm`, `qks1[0]` = `QKS1(1)`). `expected.emptyWindow` is the line-590 restart
(`AUS = 0`); when `false`, `minCell`/`maxCell` are the final `mindk`/`maxdk` (`mindk`
never moves; `maxdk` can shrink in the `1e-5` truncation loop of lines 600-606 before
the shift to index 1 of lines 607-610), `cellCount` is `NNN1`, and `boundaries`/
`cumulative` are the shifted `DPOC`/`FQKS`, both length `cellCount`, index 1 first.
`samples` (empty when `emptyWindow` is `true`) feeds each listed `x3` through `DM`
(lines 1573-1586) against that same window; `expected.pocketSize` is `DC`.

Regenerating (`generate`, then `verify`) reproduces every file byte for byte: proven
2026-09-17 by mutating one committed value (`size_law.json`,
`"diameter": 20.0` → `20.1`), seeing `verify` fail with the mismatch and the changed
file's path, then regenerating and seeing `verify` pass again (`generate.py`'s own
provenance/`verify` pattern, applied here without copying its Windows-executable
machinery — this script never runs `PropStructV3.exe`).

## Statistics formula cases ✅

`formulas_statistics.py` transcribes the category merge (Fortran lines 825-959,
`Categories.MergeAndDescribe`) and the small-particle probability and largest-reaching
base size (Fortran lines 1021-1074, `SmallParticles.Probability`/`MaxSize`) into
Python `double`, independently of any C# (`BOOT.md`, ## Invariants, "Formula scripts
are transcriptions of formulas, not of the program"). It writes four files:
`cases/statistics/categories.json`, `small_particles_probability.json`,
`small_particles_max_size.json` and `cycle_statistics.json` (below), and a fifth,
`setup_plane.json`. Every array and index in `categories.json` is
0-based, matching `Categories.MergeAndDescribe`'s own signature directly
(`src/Statistics/API.md`): there is no Fortran-1-based caller convention to preserve,
since this class's C# signature is already 0-based end to end. The small-particles
functions keep their own loop variables (`kilo`, `iks`) 1-based internally, matching
the Fortran statement for statement, while their returned arrays stay 0-based Python
lists like every other file here.

```console
python tests/Fixtures/formulas_statistics.py generate   # (re)writes every case file
python tests/Fixtures/formulas_statistics.py verify      # regenerates into a temp dir, fails on any byte difference
```

Every file here shares the same envelope as `formulas_particle.py`'s files
(`script`, `fortranLines`, `subroutine`, `convention`, `cases`), read by the same
citation rule (`FixturesInventoryTests.EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript`).
Each case:

```json
{
  "name": "single_merge_pass_two_rows",
  "ndok": 3, "ncat": 4, "cellSize": 1.0, "categoryStep": 1.0, "epsDok": 1e-06, "dpMax": 1.0,
  "qdoks": [[5, 5, 0], [0, 0, 10]],
  "dokp41": [5.0, 7.0], "dokp31": [1.0, 1.0],
  "expected": {
    "dpRow": 1, "dpockets": [1.0], "dokp43": [6.0], "qdokkarm": [0.5],
    "qdokso": [[0.5, 0.5, 0.0]],
    "qdoksAfter": [[5, 5, 10]], "dokp41After": [12.0], "dokp31After": [2.0]
  }
}
```

`ndok`/`ncat`/`cellSize` (Di)/`categoryStep` (Dj)/`epsDok`/`dpMax` are
`Categories.MergeAndDescribe`'s own parameters; `qdoks`/`dokp41`/`dokp31` are the
run's totals before the call, row-major by category (row `r`, oxidizer cell `i`),
sized to the number of rows the case starts with (not necessarily `ncat`, which is
only a capacity bound); `expected.dpRow`/`dpockets`/`dokp43`/`qdokkarm`/`qdokso` are
the method's `out` parameters, trimmed to the final row count; `expected.qdoksAfter`/
`dokp41After`/`dokp31After` are the same totals *after* the call, at their original
(untrimmed) row count, proving the in-place rewrite (`src/Statistics/BOOT.md`,
"In-place rewrites of the totals").

Four cases, each isolating one branch of `## Categories`: a merge with no shift
(`single_merge_pass_two_rows`), a merge immediately followed by a shift within the
same pass (`merge_then_shift_three_rows`), a merge fired only by the separate
last-row check, with no recompute afterward (`last_row_merge_without_recompute`,
`dokp43`'s merged row keeps its *pre-merge* value, proving the "without recomputing"
rule), and a `QDOKSS = 0` row (`empty_row_qdokss_zero`).

Every case of the three per-cycle files (2026-10-01) also carries `expectedOriginal`, the
same fields under `PrecisionKind.Original`: the script asks
`src/Statistics/CyclePlane.listing.generated.txt` (the source-read table until
2026-10-03), never the C#, whether each store of the transcribed lines rounds, on which
pass a sum is stored, in which order a product is formed (its `order` text is parsed and
evaluated, no plane literal is typed in the script) and whether each read takes the
store's unrounded register value or its rounded home (`formulas_statistics.py`,
`CyclePlane`).
`expected` is unchanged by that. The probability file's fourth case,
`register_reads_and_loop_sums_reach_a_stored_bit`, was found by a seeded search so that
rounding `Pl` at 1050, or `zdoksmall` on every iteration of 1027–1029, changes a stored
bit; the first three are too short for either.

`cases/statistics/cycle_statistics.json` (2026-10-02) is the same script's twin of the
rest of `CycleStatistics.Compute`: `GeneratorAccuracy`, `OxidizerSizes`, `Pockets`,
`Matrix`, `CorrectedPockets`, `MassFractions` and `Convergence` (Fortran 771-819,
963-1016, 1087-1113, 1137-1152, 1168-1174), calling the category merge and the
small-particle functions above rather than transcribing them again. It is written from
the Fortran and `CyclePlane.listing.generated.txt` alone, but for the association
(below). Each case has `expected` and `expectedOriginal`:

```json
{
  "name": "cycle_one_rewrites_vdok_totals_in_place", "cycleIndex": 1,
  "setup": {"fractionCount": 2, "ndok": 4, "nkarm": 3, "ncat": 3, "nc": 4, "cellSize": 1.0, "categoryStep": 1.0,
            "dmin": 2.0, "ak2": 2.0, "pocketCoefficient": 8.25, "bridgeCoefficient": 40.0},
  "tables": {"share": [...], "pocketForming": [1, 0], "massShare": [...]},
  "echoes": {"dokm": 2.5, "doksd": 0.5, "ddokmax": 4.0, "ggg": 0.75},
  "inputs": {"oxidizerDensity": 2.0, "propellantDensity": 1.5, "metalMassFraction": 0.125, "epsDok": 0.0625, "...": "..."},
  "integerTotals": {"nfx": 8, "qks": [3, 5, 2], "qdoks": [[...]], "...": "..."},
  "realTotals": {"xss": [...], "sd4": 10.0, "vdokTotal": [...], "vdokTotal2": 12.0, "...": "..."},
  "expected": {"eps1": 0.025, "alldok43": 2.555, "gdokleft": 0.689, "dolM1": 0.034, "vdokTotalAfter": [...], "...": "..."}
}
```

The input sections are `Compute`'s own arguments by name (`setup`, `tables`, `echoes`,
`inputs`) and the totals by their `AccumulatorLayout` field names, 0-based; the decoys
`oxidizerMassFraction` and `homogenizedOxidizerFraction` are inputs the lines never
read. `expected` holds every value the seven members produce, by the report's own names
in camel case (`eps1` is `CycleReport.Eps1`), plus `nextPdoksmall`, `nextDmaxxx` and the
totals after the call (`qdoksAfter`, `dokp41After`, `dokp31After`, `vdokTotalAfter`,
`vdokTotal2After`). NaN and the infinities are the strings `"NaN"`, `"Infinity"` and
`"-Infinity"`. In cycle 0 the quantities of the later lines are `"NaN"` and the nine
print-time entries (`*Normalized`, `*Nmax`) are absent: `Compute` fills them from
the totals in every cycle (measured 2026-10-02, a mismatch with the "NaN" of
`src/Statistics/BOOT.md`, "## Report", reported to the owner, not asserted).

The cases: `cycle_zero_gates_the_later_lines` (`NFQ = NFW = 0`, so `eps6`, `eps7` are
0/0), `cycle_one_rewrites_vdok_totals_in_place` (a nonzero `gdokleft`, an empty
`Fmkarm2`, a `Dmaxxx` that the threshold of 1069 reaches),
`seeded_rounding_reaches_every_member` (seed 1, a `random.Random(seed).random()`-only
draw of realistic totals, with `EpsDok` 0.3 so the categories merge once; the script
refuses a seed under which `Original` equals `Binary64` in any of the seven members),
`seeded_epsx3_warning_reads_the_rounded_store` (seed 3) and
`seeded_epsy_warning_reads_the_rounded_store` (seed 6): `EpsDok` is the binary32 value
just below the register of `EPSX3` or `EPSY` under `Original`, so both flags, which read
the reloaded home (817 at 0x40C50B, 976 at 0x40E56B), clear.

⚠ 2026-10-03: was `seeded_epsx3_warning_reads_the_register`, whose first flag stayed
set because the source-read rule took 817's compare from the register; the listing
compares the home →
`src/Statistics/HISTORY.md#per-cycle-plane-source-read-rules-2026-10-03`

The `control_NN_*` cases (2026-10-03) are the positive controls of the site table: each
is a `_seeded_body` with the seed and keyword arguments of `CONTROLS` in the script, and
carries `controls`, a list of `{at, kind, target, lines, flips}`, one per site it
proves. A flip is the site's store taken the other way or its schedule's other pass
decision; `generate` refuses a control whose flips leave the Original report
unchanged, so each reaches a stored bit of its site. A body shared by several sites
is one case. The sites the oracle fixture leaves `unreached` are all controlled but
those the table says are exact (`order` of `0.0` or `real(i)`: a register's zero
start, a count below 2**24), which no input reaches; `generate` refuses an
`unreached` site that is neither.

`cases/statistics/plane_forms.json` (2026-10-03) holds, per helper of `CyclePlaneOrder`,
24 generated operand tuples with the value the script reads off the `order` text of a
site that uses the form (`form_linear` 40E925, `form_quadratic` 40E9CB, `form_fourth`
40D07E, `form_cube` 40D090, `form_square` 40D360), and per block size of
`UnrolledSchedule` (`schedule_5` ... `schedule_9`) every pass decision of the rule
over trip counts below, at and above it (`tests/Statistics.Tests/PlaneFormsTests`). The
weights of `form_linear` and `form_quadratic` are quotients of two binary32 values (a
stream of their own, `QUOTIENT_SEED`), as at the sites that use them: a binary32 weight
makes every association of the linear form exact, and a form associated otherwise would
pass. For every form but `form_square` (an equivalent form: `pow(y, 2)` is `y·y` when
pow is correctly rounded) `generate` counts, per other way of forming the product
(`FORM_ALTERNATIVES`), the tuples that differ from the Original form in the last bit and
refuses a count below one tuple in eight (`FAIR_SHARE`): today 11 and 9 of 24 for the
two other associations of the linear form, 7 for the quadratic.

Association (measured 2026-10-02, `Binary64` only; under `Original` the order is the
table's): the cell centre `Di*(kilo-0.5)` is formed once and the linear and
second-moment terms are `x*c` and `(x*c)*c`, the order `Compute` takes; the Fortran's
`(x*Di)*(kilo-0.5)` and `x*(c**2)` differ from it in the last bit of a `double`
(`alldok432` 3.168181003715818e-05 against 3.1681810037158176e-05 on seed 1), seen under
`Binary64` only. `_centre`'s docstring carries the reasoning. Read off `Compute` when
the first transcription, in the written order (a1e4226), disagreed: on the association
this file is a change detector, not an independent witness.

Regenerating (`generate`, then `verify`) reproduces the file byte for byte: proven
2026-09-18 by mutating a committed `"dpRow": 1,` to `"dpRow": 9,`, seeing `verify` fail
with the mismatch and the changed file's path, then regenerating and seeing `verify`
pass again (the same pattern `formulas_particle.py`'s own entry above already
established for this node).

## generate.py ✅

```console
python tests/Fixtures/generate.py reference <name>...                       # GSV=2 as shipped
python tests/Fixtures/generate.py seeds locate                              # proves the six seed offsets
python tests/Fixtures/generate.py replicas <name> --layout lagged --count R
python tests/Fixtures/generate.py replicas <name> --layout independent --count R
python tests/Fixtures/generate.py replicas <name> --layout gsv3 --count R   # staggered starts
python tests/Fixtures/generate.py verify                                    # Legacy/ against the archive, the three derived fixtures against a regeneration
python tests/Fixtures/generate.py derive [--check]                          # write (or only compare) archive-members.sha256 and the two cases/ files
python tests/Fixtures/generate.py print-states --layout lagged|independent [--ks K...]
```

Every command that runs the executable or reads the archive or the source reaches them
through `legacy_file` of [legacy](../../tools/legacy/API.md) (`PROPSTRUCT_LEGACY_DIR`),
lazily, at the first use of a file: `print-states` and the formula scripts run without
the variable. `verify` compares the data of `Legacy/` with the archive's members byte
for byte, the source with its member transcoded from CP1251, the executable and its
runtime with theirs, and fails on a file under `Legacy/` that is not data; then `derive
--check`. All of it is `Category=Legacy`.

`replicas --layout lagged` and `--layout independent` refuse (`SystemExit`) if any of
the six seed offsets is unproven (`BOOT.md`, "Seed offsets are proven, not assumed").

⚠ 2026-09-25: `print-states` added, for `tests/Random.Tests`' own cross-check of this
script's seed arithmetic against `src/Random` (`## generate.py`'s docstring reference
below; `tests/Random.Tests/BOOT.md`, "## Acceptance criteria"). It never runs
`PropStructV3.exe` and writes nothing: pure JSON to stdout, the six un-jumped `base`
states of `layout` (`original_stream_value`/`independent_stream_value`) always, and,
for every `k` in `--ks`, the six `_states_for_replica(layout, k)` states — every value
a decimal string, since a 128-bit state does not fit a JSON number. `--layout`'s two
choices are this script's own replica-kind names (`_states_for_replica`'s, "lagged"
for the `Original` layout's base value, "independent" for the `Independent` layout's),
not `run_original.py`'s `--layout original|independent` below — the two scripts
already used different vocabularies for the same two layouts before this addition,
and `print-states` follows each script's own, rather than reconciling them.

`provenance.json` entry, GSV=3:

```json
{ "output": "replicas-gsv3/HPEPA3/7.m.txt", "outputSha256": "…", "inputSha256": "…",
  "executableSha256": "…", "gsv": 3, "stdin": "hpepa3\\ny\\n",
  "date": "2026-09-18T10:00:00Z", "scriptCommit": "…" }
```

`provenance.json` entry, seed-patched (lagged or independent): the same fields, `gsv`
always `2` (the input is the shipped `.dat`, unpatched — only the executable's seed
blocks are patched), plus `"layout"`, `"k"`, `"jump"` (the exponent `k·2⁸⁰ + 2⁷⁹` as a
decimal string) and `"patchedExecutableSha256"` (the SHA-256 of the seed-patched
executable actually run, `"executableSha256"` staying the shipped executable's hash):

```json
{ "output": "replicas-lagged/HPEPA3/7.m.txt", "outputSha256": "…", "inputSha256": "…",
  "executableSha256": "…", "gsv": 2, "stdin": "hpepa3\\ny\\n",
  "date": "2026-09-18T10:00:00Z", "scriptCommit": "…",
  "layout": "lagged", "k": 7, "jump": "852...", "patchedExecutableSha256": "…" }
```

`exclusions.json` entry:

```json
{ "formulation": "*", "quantity": "pdoksmall", "rule": "reference cells below 1e-30 compare as zero",
  "reason": "uninitialized pdoksmall(1) copied forward, Fortran 1062-1064",
  "evidence": "Legacy/outputs/rc166.m.txt: six leading cells 0.762E-38" }
```

⚠ 2026-09-17: the `reason` line first read "Fortran 1059-1064" (the whole `do kilo = 2,Ndok`
loop) and `evidence` named `rc166.m` (pre-rename). `kilo` starts at 2, so `pdoksmall(1)`
is never touched inside the loop at all; the clamp that copies it forward into
`pdoksmall(2)` when the latter is smaller is lines 1062-1064 specifically (checked
against `Legacy/PropStructv3.for.txt`, this node's declared specification for the
Fortran source, AGENTS.md §12). `exclusions.json` and `BOOT.md` ## Invariants now agree
on the narrower range.

## Measurement scripts ✅

Deterministic, single-seed measurements outside the statistical criterion — the
apparatus `tests/Harness/HISTORY.md`'s "how many accepted particles pass before the
port and the original first print different integer counts" needs, and every
measurement built on it since (a signed difference profile between the port and the
original, a dose-response in `N`): one seed, no replica set, no `alpha`, no
exclusion list. Neither script writes into `references/`, `replicas-*/` or
`provenance.json`: a run of either is a throwaway measurement, not a fixture.

```console
python tests/Fixtures/run_original.py <name> --layout original|independent --seed K [--n N] [--kxx KXX] [--alfa VALUE | --karmcoef VALUE] --out <path>
python tests/Fixtures/run_port.py     <name> --layout original|independent --seed K [--n N] [--kxx KXX] --out <path> [--propstruct <path>]
python tests/Fixtures/run_original.py --layout original|independent --print-states --seeds K...
```

`--layout`/`--seed` map straight onto `src/Cli/API.md`'s own `--layout`/`--seed` (both
reach `OriginalSeeds.ForParticle(seed, ordinal)`/`IndependentSeeds.ForParticle(seed,
ordinal)` at `ordinal = 0`, `src/Random/API.md` — reference mode's only ordinal, being
a single continuous stream, never batched): the two scripts run at the *same* seed
reach the *same* patched state by construction, so their two `results.m` files are
directly comparable cell for cell. `run_original.py`'s own jump arithmetic
(`reference_mode_jump_exponent`, `k * 2^80`) is deliberately not
`generate.replica_jump_exponent` (`k * 2^80 + 2^79`, a `Fixtures` replica's own
construction, ## Invariants "Seed-patched replicas"): the two serve different
purposes and using the replica jump here would compare the port against a seed no
reference-mode run of the port ever reaches. `--layout` is always honoured, at every
`--seed` including `0`: `states_for_seed` always patches the executable with the
layout's own zero-ordinal state jumped by `K`'s exponent, never skipping the patch
(`BOOT.md`, ## Invariants, "A measurement script never accepts a flag it does not
honour"). At `--seed 0` the exponent is `0`, so the patched state is that layout's own
un-jumped initial state; for `--layout original` this is, by construction, the same
six values already baked into the shipped executable, so `--layout original --seed 0`
still reproduces the unmodified executable's output exactly outside the time line and
`pdoksmall(1:2)` — not because the patch is skipped, but because patching those values
back in changes nothing.

⚠ 2026-09-21: was "`--seed 0` is `run_original.py`'s own sanity check: the unmodified,
shipped executable, no patch at all" — true only for `--layout original`. For
`--layout independent` the same code path silently ran the *original* layout's
unmodified executable instead, so `--layout independent --seed 0` reproduced
`--layout original --seed 0` bit for bit outside the time line and `pdoksmall(1:2)`.
Found by `tests/Harness` from outside (`HISTORY.md`, "the Independent-layout
non-monotonic shape was the apparatus, not physics") and fixed the same day by
deleting the `seed == 0` special case rather than adding a second one for
`--layout independent`; full account in `BOOT.md`, ## Invariants, "A measurement
script never accepts a flag it does not honour".

`--n`/`--kxx`, on either script, override the shipped `.dat`'s own `N`/`KXX` header
fields (`generate.replace_header_token`, indices 3 and 2 of the six-value
`NMM JZZ KXX N NNZ GSV` record) — the dose-response instrument: the same seed and
formulation at the formulation's own `N`, `N/10`, `N/100`, ... Both scripts apply the
override identically (one shared function, never a second implementation of it), so a
port/original pair at the same `--n`/`--kxx` sees the same input beyond the override.
Both flags are applied whenever given, with no branch on the value itself, so neither
can be passed and silently ignored the way `--layout` once was.

⚠ 2026-09-24: `run_original.py` gained `--alfa` (`src/Output`'s O-1 fixture,
`## Layout` below). Menu item [9], `P[Dok>Dok_max]`, is not in the `.dat` at all (the
original only ever asks for it interactively) and has no default-parameters shortcut, so
honouring it means declining that prompt: `--alfa` given sends `<name>\nn\n9\n<alfa>\n0\n`
(decline the defaults, item 9, the value, then start modelling with every other
parameter left at the compiled-in default); omitted, the session is unchanged,
`<name>\ny\n` (accept every default) — `run_original.menu_stdin`, the one place either
session is built. `run_port.py` has no matching flag: no menu, and no port option for
this quantity exists yet to match it (`src/Input/API.md`'s `ModelParameters.TailProbability`
is set directly, not through an interactive session). `generate.run_case` gained a
matching optional `stdin` parameter, `None` by default, preserving every existing
caller's own `"{name}\ny\n"` session unchanged (`cmd_reference`/`cmd_replicas`/`seeds
locate` still pass no `stdin` at all).

⚠ 2026-09-24, later: `run_original.py` gained `--karmcoef`, the same shape as `--alfa`
above but for menu item [7], `k7` (`PocketCoefficient`, compiled-in default 8.2,
Fortran line 76): a positive-control fixture for `src/Output`'s "print the stored
setup" design decision needed one setup-plane header echo, distinct from `--alfa`'s
`P[Dok>Dok_max]` (menu item [9], `TailProbability`, not part of the setup plane at
all, root BOOT.md's "Precision kind" — `--alfa` could not serve as its own positive
control). `--karmcoef` given sends `<name>\nn\n7\n<karmcoef>\n0\n`, `menu_stdin`'s own
extension; `--alfa` and `--karmcoef` are mutually exclusive (`main` refuses both set,
`argparse.error`), since the session navigates the menu to one item and starts
modelling, not several. `run_port.py` again has no matching flag, for the same reason
as `--alfa`: `ModelParameters.PocketCoefficient` is set directly, not through a menu.

`run_port.py` needs a Release build of `PropStruct.sln` first (root `CLAUDE.md`,
"Build"); `--propstruct` defaults to the conventional
`src/Cli/bin/Release/net10.0/propstruct.exe` (or `.dll`, invoked through `dotnet`)
under the repository root. `run_port.py` passes `--layout`/`--seed` to the
`propstruct run` subprocess unconditionally for every value, so it never had this
defect's shape; checked directly (not only reasoned about), `run_port.py <name>
--layout independent --seed 0` and `--layout original --seed 0` produce substantially
different output (`tests/Fixtures`' own `check_measurement_scripts.py`, below).

⚠ 2026-09-25: `run_original.py` gained `--print-states`, alongside `generate.py`'s own
(`## generate.py`, above), for the same cross-check
(`tests/Random.Tests/BOOT.md`, "## Acceptance criteria"). With `--print-states`,
`name`, `--seed`, `--out`, `--n`, `--kxx`, `--alfa` and `--karmcoef` are all refused
(`argparse.error`) and `--seeds` (one or more `K`, in place of the single-run `--seed`)
is required instead: the command prints `states_for_seed(layout, seed)` for every
`seed` in `--seeds`, as JSON decimal strings, to stdout — never running
`PropStructV3.exe`, never writing `--out`. `run_port.py` gains no matching flag: its
own seed states are `src/Random`'s own `OriginalSeeds`/`IndependentSeeds.ForParticle`,
already reachable directly from C#, so there is nothing for a Python echo to cross-check
there.

Neither script parses `results.m` itself: `tests/Harness`'s `ResultsMFile` is the one
parser (`tests/Harness/BOOT.md`, ## Invariants — "results.m is parsed by one parser only"); a caller
compares the two files' cells with it.

```console
python tests/Fixtures/check_measurement_scripts.py
```

The regression check for the defect above: runs `run_original.py`'s `--layout
original` and `--layout independent` at the same `--seed` (`0` and `1`) and asserts
the two outputs differ outside the time line and `pdoksmall(1:2)`; runs the same
comparison through `run_port.py` at `--seed 0` when a Release build is found, skipped
with a printed reason (never silently) otherwise. Proven non-degenerate 2026-09-21
against the pre-fix `states_for_seed`: fails at `--seed 0`, passes at `--seed 1`
(`BOOT.md`, ## Invariants, "A measurement script never accepts a flag it does not
honour").

## Cycle-plane pairs ✅

Path-identical small-`N` runs of the original and the port, for the per-cycle plane's
controls (`src/Statistics/BOOT.md`, "## Defects of the original", rows 771–1175;
`tests/Statistics.Tests/PathIdenticalPairsReportTests`). Added 2026-10-02.

```console
python tests/Fixtures/cycle_plane_pairs.py ladder   [<path to propstruct.exe/.dll>]
python tests/Fixtures/cycle_plane_pairs.py generate [<path to propstruct.exe/.dll>]
```

A pair is `PropStructV3.exe` with the six seeds of `--layout original --seed 1` patched
in (`run_original.states_for_seed`) against `propstruct run --mode reference --layout
original --seed 1 --precision original`, both on the shipped `.dat` with its `KXX` and
`N` header tokens replaced (`generate.replace_header_token`) and the original's default
menu, except PSAN01, whose pocket-forming flags are read (menu [12] on the original,
`--sfr` on the port). `run_port.py` has no `--precision` and neither script has the menu
session of [12], which is all this script adds to them. `ladder` prints, for `K` in 1..2
and `N` in 50..1600 (doubling), the integer-printed tokens and all tokens that differ
between the two outputs; `PAIRS` takes per formulation the largest `(K + 1) · N` with no
differing integer token (`cycle-plane-pairs/ladder.txt` is the run of 2026-10-02).
Agreement is not monotone in `N`, since `N` changes the totals the plane hands to the
next cycle, so the ladder is evidence, not a search. `generate` refuses when a pair has
lost its integer agreement and writes the pair and its `provenance.json`: per output its
SHA-256, the SHA-256 of the input actually run (the shipped `.dat` with the two header
tokens replaced) beside the shipped one, `N`, `KXX`, seed, layout, the original's
standard input and patched-executable SHA-256 or the port's command line, the date and
the script's commit. This file is not `provenance.json` of the root of this node: its
inputs are not the shipped `.dat`, which `ProvenanceTests` holds that file's entries to.

`generate` rewrites the original's side too, and the executable does not print the same
`pdoksmall` cells below 1e-30 twice (HMX 0.196E-37 and 0.197E-37, the declared cells of
`src/Statistics`' dead row); a regeneration for a port change therefore keeps the
committed original files and their provenance entries and takes only the port's
(2026-10-03, x87 A, stage 4: the integer premise held on all six, the original's side
byte-identical to the commit before).

The port output is a recorded measurement of the port at that commit, not a replica
(BOOT.md, Taboos): `tests/Simulation.Tests/CyclePlanePairsTieTests` (`Category=Long`)
re-runs the port and fails when the stored file no longer equals it, and the pair is
then regenerated in the commit that moved the plane.

## Cycle-plane listing ✅

Added 2026-10-02 (`src/Statistics/BOOT.md`, "## Report", "The per-cycle plane from the
executable's listing"). The excerpt of the executable's listing that
`src/Statistics/CyclePlaneListing.map.txt` is written against and the generator reads.

```console
python tests/Fixtures/check_cycle_plane_listing.py verify
python tests/Fixtures/check_cycle_plane_listing.py selftest
python tests/Fixtures/check_cycle_plane_listing.py extract <output> [dumpbin.exe]
```

```text
tests/Fixtures/Legacy/PropStructV3.cycle-plane.listing.txt   dumpbin /disasm of the ranges below, header first
tests/Fixtures/check_cycle_plane_listing.py                  verify and selftest (no dumpbin), extract (dumpbin)
```

The file is a header of `#` lines (the tool, the command, the SHA-256 of the executable
and of `dforrt.dll`, and one `# range` line per range) followed by dumpbin's instruction
lines, unedited: two spaces, the address, the instruction's bytes, the mnemonic and its
operands; an instruction longer than six bytes carries its remaining bytes on a
continuation line of bytes alone. A `# <what>` line precedes each range. The ranges,
`[first, end)`, are those of `RANGES` in the script, and the header repeats them:

| Range | What | The oracle |
|---|---|---|
| 0x401000–0x40100C | the prologue: `push ebp`, `mov ebp,esp`, `sub esp,4B4h` | reads the frame size |
| 0x401F92–0x408A93 | the ALLOCATE sites, the local descriptors and slots | read for layout, never executed |
| 0x408A93–0x409A10 | the pre-loop: line 378, the hoisted block, the B6 flags | **executes** |
| 0x409A10–0x40BD9E | the cycle head and the particle loop | skips; scanned for its writes |
| 0x40BD9E–0x41058F | the per-cycle plane 771–1175 | **executes** |
| 0x4181A5–0x418577 | `PARAM` (2026-10-03) | **executes**, through the pre-loop's call; its call of `SIZE` is a stub |
| 0x418BA4–0x418C22 | the runtime thunks | stubs by address |
| 0x418CE1–0x418D8B | the C start-up's `_controlfp(0x10000, 0x30000)` | reads the precision control |

`verify` re-reads the executable's bytes at every address through the PE section table
and fails on the first instruction whose bytes differ, on an address that neither
continues its range nor starts the next, on a cut instruction, and on a header whose
digests or ranges are not the files' own; it prints the count of instructions it
proved. `selftest` runs `verify`'s core on the real excerpt (green) and on four
mutations of it, an edited byte, a moved address, a dropped instruction and a stale
digest, each of which must be red. `extract` runs dumpbin 14.12.25835.0 (`DUMPBIN` or
the second argument), under `MSYS_NO_PATHCONV=1` where a Unix shell would rewrite
`/disasm`, asks for `[first, end − 1]` so that a range ends on an instruction, drops an
instruction dumpbin cut at its last byte, writes the file to the explicit `<output>`
and verifies it, printing its SHA-256; an `<output>` inside `PROPSTRUCT_LEGACY_DIR` is
refused, so the recorded excerpt is never overwritten (`selftest` proves the guard).
Checked by `tests/Statistics.Tests/CyclePlaneListingTests`.

## Listing oracle ✅

Stage 2 of `src/Statistics/ACCEPTANCE.md`, A2. An x87 interpreter over the excerpt's
bytes, run over seeded cases, that gives `Compute` exact known answers for the sites no
archived output can: the accumulator-dependent ones, whose inputs never coincide between
the original and the port.

```text
tests/Fixtures/x87_machine.py                  executes the excerpt over .rdata and .data of the executable
tests/Fixtures/cycle_plane_oracle.py           injects by map name, runs, reads back, selects cases, verify
tests/Fixtures/cases/statistics/cycle_plane_oracle.json   the fixture
tests/Fixtures/preloop_survey.py               the oracle's pre-loop over every shipped .dat
tests/Fixtures/cases/statistics/preloop_survey.json   what that pre-loop left, with provenance
```

`x87_machine.py selftest` runs the machine's checks, each red on what it guards (2026-10-03: a call into an executed range and its `ret imm16`, a stack probe below esp, which leaves the flags undefined, and the O1 and O2 stops that go with them). The
machine knows addresses, never names: `Machine(ranges)` decodes the excerpt's executed
ranges with its own decoder and refuses to start unless every instruction renders to the
excerpt's dumpbin text (stop O1); `Machine.run(start, stops)` executes until the address
before an instruction is in `stops`. Stops, all `Stop` exceptions with a code: O1 (an
opcode outside the subset, an exit from the ranges, a call to a target with no stub, a
cross-check mismatch), O2 (a read of an undefined byte, register or flag, an access
outside every block), O3 (arithmetic outside 53-bit round-to-nearest, a denormal,
invalid, zero-divide or overflow, `fild` of 2^53 or more, a stack fault). The self-test
holds the two proofs of the cross-check: the decoder with the `DE E0+i`/`DE E8+i` pair
swapped fails at the eight `fsubrp` sites (r1) and a ModRM edited in memory (0040EB31,
`0D` to `05`) fails at 0040EB30 (r2).

`PARAM` is executed, not stubbed (2026-10-03): the map's `callee` row names its range, `ZSS`, `ZX` and `Z11` are what its bytes store and are `expect` rows, `alfa` is injected, and the stub of `SIZE`, its last call, only pops its eight arguments. `read_preloop` records an array as a list of hex strings. `jzz2_controls` (the self-test) runs the pre-loop on 120 drawn `JZZ = 2` loops against the twin's `jzz2_sums`.

`cycle_plane_oracle.py` commands: `generate` (the seeded search, then a replay of the
kept cases writes the fixture; `--state` and `--budget` let a long search stop and
resume), `extend` (2026-10-03: adds the cases of the map's `pin` rows the fixture does
not hold, after its own, from its recorded seed, and rebuilds coverage, site statuses
and provenance over all of them; from a replay of the held cases under today's bytes; it
does not search, so its draws and `counters` (but `pinned`) are the last `generate`'s,
which ran before `PARAM` was executed (2026-10-03): that a `generate` today keeps the
same draws is inferred from the replay, no output of the 22 held cases having moved, not
run; it holds only while no pin reaches a site with zero executions among the cases kept
(the least executed runs 10 times), and the terminal statuses are rebuilt from the old
ones, not re-derived, `extend_fixture` of `cycle_plane_oracle.py`), `verify` (replays
every recorded case and isolation, fails on any difference, on a stale provenance digest
and on pins of the map the fixture lacks), `restamp` (2026-10-03: rewrites the
provenance digests to the bytes of today and nothing else, for an edit of a digested
script that moves no answer; it claims nothing, `verify` does), `selftest` (the guard
red on a typed `3.14159` and on a typed map name, r3 one home moved by four bytes, r5
the exp-log rule against the recorded pow triples, and per pin that the listing's
schedule gives the executable's bits and one store after the loop does not), `guard` (no
string equal to a name of the map and no float equal to a literal of the plane in the
two scripts), `report`. It reads `src/Statistics/CyclePlaneListing.map.txt` ("## Cycle
listing map and table" in `src/Statistics/API.md`: its `oracle` rows hold every name,
address, generator of a total and draw law) and the generated site table.

A `pin` row of the map (`formulation | setup key | why`) is a case the search keeps
besides the ones it draws: drawn like any candidate (`draw_case`) from a generator named
by the seed, the pin's place and an attempt, its formulation the row's, never isolated,
with `purpose` `pin` and the number after the last draw; of `PIN_ATTEMPTS` attempts the
lowest whose run does not stop is the case. Its reason to exist is a quantity the
pre-loop alone decides and the table does not reach: `BK10`, `KB397` and `AK157a` for
`DOKM`'s five-fold schedule, `HMX` and `P2a` for `ZS` stored after every addition,
`HPEPA3` for `ZX`'s denominator and `CSPX04` for the `JZZ = 2` loop's stores,
`ListingOracleTests` holding the port to the executable's bits there.

Fixture format (`format` 1): `fortranLines` and `script`, which every case file under
`cases/` names (`FixturesInventoryTests`; `verify` checks both against the script's own
constants), `provenance` (SHA-256 of the executable, the excerpt, the
map, the site table and three scripts, the machine, the oracle and the twin
`formulas_statistics.py`, whose `oracle_setup` supplies every injected value; the seed,
the draw counts, the git head),
`counters` (candidates, stops by code), `cases` and, keyed by the generated table,
`coverage` and `sites`. A case: `id`, `formulation` (a shipped `.dat`), `menu` (typed
values), `flags` (menu [12] or `null`), `setup_overrides`, `seed`, `max_cycles`,
`totals_overrides`, `setup` (every value the oracle injected, by Fortran name, binary32
widened), `preloop` (per key the executable's own bits and the setup model's),
`init_steps`, `cycles` and `isolated` (site address to the (cycle, output) pairs its
hook moved). A cycle: `exit` (`warm-up`, `continue`, `print`), `steps`, `io_sites`,
`pow` (the `**0.3333` triples as `r8` bits), `trips` (the measured trip count of every
unrolled loop's runs), `totals` and `outputs`. A value is `{"type", "bits"}` (a scalar,
most significant byte first) or `{"type", "dims", "z"}` (an array in the Fortran's
column-major order, zlib then base64); types `r4`, `r8`, `i4`, `i8`. Outputs are every
non-temporary Fortran target a store reached in that cycle, and every array a store
reached; an element no store reached is zero. `sites` has one entry per site of the
generated table with `executions`, `hook` (its alternative, `null` when the table gives
it none), `isolated_by` and a `status`: `isolated` (a case whose hook moved a compared
output), `terminal` (the hooked value is never read back, so the stored value is itself
the compared output), `no-hook` (the table gives the site no alternative: a REAL*8
store, a constant, a move, a compare, a value no line reads) or `unreached` (never
executed, or executed with a hook that moved nothing), with the reason, never dropped.
`coverage.loops` holds, per unrolled loop, the `n mod block` residues each case reached
with at least one full block and the runs shorter than a block.

Hooks select cases and never supply a value. The alternatives: `shadow` (a reload of a
stored binary32 sees the register's unrounded value: kinds S, T, P, B), `after-register`
and `before-copy` (the carried register is rounded: X, C6), `after-result` (the register
the operation wrote is rounded: R born by an operation), `all-registers` (every occupied
register is rounded: R at a loop exit, whose register the table does not name).

### Pre-loop survey

`preloop_survey.py` runs the oracle's pre-loop (`World.run_init`, `read_preloop`) once
for every `.dat` of `Legacy/formulations`, the menu at its defaults, and writes
`cases/statistics/preloop_survey.json`: `fortranLines` (`376-406`), `script`,
`provenance` (the oracle's digests, with the survey's own script and the twin) and, per
formulation, `datSha256`, `jzz`, `nmm`, `steps` and `preloop`, per key of the map's
`expect` rows (`ggg`, `lambda`, `zss`, `zx`, `z11`, `dokm`, `doksd`, an array as a list)
the executable's bits and the twin's. A formulation the machine stops on carries `stop`
instead. `jzz2Controls` holds the oracle's drawn JZZ = 2 loops
(`cycle_plane_oracle.jzz2_runs`): `shares`, `bounds`, and the executed `zx`, `dokm` and
`doksd`. `generate` writes it (37 s with them); `verify` regenerates in memory and fails
on any byte difference. Consumers: `tests/Statistics.Tests/PreloopSurveyTests`
(`Setup.Prepare` against the executed bits, the approved ratchet
`PreloopSurvey.approved.txt`; `Setup.AnalyticSizes` against the drawn loops) and the
provenance check there.

The twin's `.dat` reader (`formulas_statistics.py`, `_read_dat_formulation`) follows the
READ statements of lines 94 and 124-133 over the file's records: a `READ(2,1000)` takes
one record, a list-directed `READ(2,*)` of n values whole records until it has n and
drops the rest of the last. The reader before 2026-10-03 took the numeric tokens of the
whole file in order and misread `C166.dat`, whose `GDOK` record holds fifteen values for
`NMM = 14`; the other 48 shipped files are read as before.

## Dispersion table ✅

`dispersion.approved.txt` (`BOOT.md`, ## Invariants, "Thresholds are not stored", the
2026-09-20 ⚠): a generated tripwire over `Harness`'s own conditional dispersion
estimate (`DispersionEstimator.Estimate`, `tests/Harness/API.md`), one line per
(formulation, family or `fqdokkarm` row):

```text
# formulation family/row runs groups n_bar phi rho_hat rho_hat_spread route
HPEPA3 fqkarm 32 17 1068.16 13.3538 0.0115764 - beta-binomial
HPEPA3 fqmkm1 32 270 5955.06 0.011556 0 - student-on-count
```

⚠ 2026-09-20: the table gained the `rho_hat_spread` column
(`tests/Harness/HISTORY.md#decision-vii-full-reasoning`, item 2's robust per-cell median estimator); `-` marks a row where
`phi < 1` and `rho_hat` is not estimated at all, exactly as `rho_hat` itself is `0`
there.

`runs`/`groups`/`n_bar`/`phi`/`rho_hat`/`rho_hat_spread` are `DispersionEstimate`'s own
fields (`tests/Harness/API.md`); `route` is `student-on-count` when `phi < 1` (the family
leaves the count predictive entirely, `tests/Harness/HISTORY.md#fable-5-1-decision-v`,
item 3) or `beta-binomial` otherwise. Produced and checked from `tests/Harness.Tests`,
not from a script here (this node's own generation scripts are Python transcriptions of
an *external* source, the Fortran; this table is a report on `Harness`'s own C#
formula, and a Python reimplementation of it would be the second implementation root
BOOT.md's Taboos forbid — `tests/Harness.Tests/DispersionApprovedTests.MatchesTheCommittedTable`
is the check, run every full test pass, against `tests/Harness/DispersionTable.Compute`
(`tests/Harness/API.md`, "## Dispersion table"), the one implementation; the manual
generator is `tests/DispersionTool`, run by hand.

⚠ 2026-09-25: the generator named above moved from `tests/Harness.Tests/DispersionApprovedTests.Regenerate`
(a `[Fact(Skip = ...)]` test, a permanently skipped check AGENTS.md §13 forbids) to the new node
`tests/DispersionTool`, the same "generation is a manual, recorded step" role `tests/Benchmarks` and
`tests/RateTableTool` already have. The formula itself moved from a private method of the check's own file to
`tests/Harness/DispersionTable.cs`, so both callers share one implementation; regenerated and diffed against
the pre-move file the same day: bit-identical bar the header's own generator citation.

## Rate table ✅

`rate-table.json` (root BOOT.md, "the pass condition compares failure rates, not single
runs", decided 2026-09-24): a pregenerated, compact verdict for every port run the rate
criterion needs — reference mode, sixteen seeds `seed = seedIndex << 16` for `seedIndex
= 0..15`, per formulation and layout, under both `PrecisionKind.Original` (the pass
condition's own data) and `PrecisionKind.Binary64` (the positive control): `5 * 2 * 16 *
2 = 320` runs.

⚠ 2026-09-27 (the set-comparison design session, owner decision 3): was "never the
`results.m` files themselves" — reversed. The same 320 runs' own `results.m` text is now
also stored, under `rate-runs/` below, so `CompareSets` (`tests/Harness/BOOT.md`, "## Set
comparison") has candidates without re-running the simulator; the "never" reflected a
cost concern (13-minute regenerations), not a rule this node could not lift for itself.

```json
{
  "GeneratedAtUtc": "2026-09-24T...Z",
  "Commit": "...",
  "CriterionSha256": "...",
  "SnapshotSha256": "...",
  "FixturesSha256": "...",
  "SeedStride": "seed = seedIndex << 16, seedIndex = 0..15",
  "Runs": [
    { "Formulation": "HPEPA3", "Layout": "Original", "Precision": "Original", "SeedIndex": 0,
      "Seed": 0, "FailingCells": 0, "FailingNames": [], "ResultSha256": "..." }
  ]
}
```

`CriterionSha256` is the digest `tests/Fixtures/BOOT.md`, "## Rate table" defines and owns,
at generation time — not the SHA-256 of one file, which this document wrongly said below
until the correction just after this paragraph.

⚠ 2026-09-26: was "`CriterionSha256` is the SHA-256 of `tests/Harness/StatisticalCriterion.cs`
at generation time" — already stale against the 2026-09-25 split of that file into several
under `tests/Harness`, and stale again against the 2026-09-26 widening of the digest to
every `tests/Harness/*.cs` file minus a declared exclusion list. Corrected to a link to the
rule's one owner (`tests/Fixtures/BOOT.md`, "## Rate table") instead of retelling it here,
so this document cannot go stale the same way twice.

`SnapshotSha256` is the SHA-256 of
`tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt` at generation time.

`FixturesSha256` is the digest `tests/Fixtures/BOOT.md`, "## Rate table" defines and owns,
at generation time, added because the criterion also reads fixture data neither digest
above sees: `exclusions.json`, every file under `references/`, and every file under each
replica directory a rate run's own comparisons pass a `ReplicaKind` for
(`replicas-lagged/`, `replicas-independent/`; not `replicas-gsv3/`, never read by a rate
run). All three digests together are the tie
`tests/Harness.Tests/RateCriterionTests.RateTableTiesToTheCurrentCriterionAndSnapshot`
checks on every run, each its own assertion, so a criterion, fixture or port change that
is not followed by regeneration fails loudly, naming which input moved, rather than
silently comparing stale data. `Commit` is informational provenance only (best-effort
`git rev-parse HEAD`, not part of the tie). A run "fails"
by the same rule `tests/Harness.Tests/NullRateCalibration` uses for the original's own
null rate: any of `StatisticalCriterion.Compare`/`CompareTailRowMean`/
`CompareAdaptiveIndexMatched` names a failing cell for it, against all `R` replicas of
its own layout (`Original` layout against `ReplicaKind.Lagged`, `Independent` against
`ReplicaKind.Independent`) — `FailingCells` is the pooled count over the three reports,
`FailingNames` each as `"Quantity[Index]"`. `ResultSha256` is the same per-run hash
`tests/RateTableTool/ReferenceModeRunner.RunSeed` returns (time line stripped),
carried for provenance, not read back by any comparison.

Written by `dotnet run --project tests/RateTableTool -- rate-table` (manual
regeneration only, this node's own "generation is a manual, recorded step, not part of
any test run" convention: running 320 reference-mode simulations inside the ordinary
test pass would make every guarded merge pay for them). This table's own writer lives
in a neighbour node, not here, because writing it needs the simulator (`Simulation`,
`Execution`, `Output`), which this node may not depend on (`## Dependencies`: `None`)
— the schema above is this node's own contract for the file, read independently by
whichever node needs it (`tests/Harness.Tests/RateCriterionTests`), the same
relationship `exclusions.json`/`provenance.json` already have with their own readers.

**`rate-runs/`** (added 2026-09-27, the reversal above): the same 320 runs' own
`results.m` text, byte for byte — UTF-8, LF, the time line stripped, the same
normalization `ResultSha256` is taken from — one file per run:

```text
tests/Fixtures/
  rate-runs/<formulation>/<layout>/<precision>/<seedIndex>.m.txt
```

`<layout>` and `<precision>` are `StreamLayout`/`PrecisionKind`'s own `ToString()`
spellings (`Original`/`Independent`, `Original`/`Binary64`), the same the table's own
`Layout`/`Precision` fields use. Written only once every one of the 320 jobs has
succeeded, after first clearing the directory (never a partial, stale set); read by
`tests/Harness.Tests/RateRuns.Load(formulation, layout, precision)`, in seed order, for
`StatisticalCriterion.CompareSets`.

⚠ 2026-09-25: the writer named above moved from `tests/Simulation.Tests/RateTableGenerator.Regenerate`
(a `[Fact(Skip = ...)]` test, a permanently skipped check AGENTS.md §13 forbids) to the
new node `tests/RateTableTool`, a console project in the same "generation is a manual,
recorded step" role `tests/Benchmarks` already has. The schema this section documents
did not change.

## Side effects

`generate.py` runs a 32-bit Windows executable in temporary directories and writes under
`tests/Fixtures/`. `tests/DispersionTool` writes `dispersion.approved.txt` under
`tests/Fixtures/` the same way, from `tests/Harness`'s own code instead;
`tests/RateTableTool`'s `rate-table` subcommand writes `rate-table.json` here likewise,
from the simulator.

## Out of scope

- Parsing outputs, computing thresholds, comparing: `Harness`.
