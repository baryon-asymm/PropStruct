# BOOT.md — Fixtures.Tests

## Purpose

The definition of what "Fixtures are trustworthy": form and provenance of the reference
data, checked on every test run because every other test node trusts them.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | every generated output has a provenance entry, and its SHA-256 matches | `provenance.json` | ✅ |
| L0 | every provenance input hash matches the shipped `.dat`, or the shipped `.dat` with only the GSV value changed to 3 | `Legacy/formulations/` | ✅ |
| L0 | replicas of one formulation and kind (lagged, independent, GSV=3) pairwise distinct outside the time line | the replica files | ✅ |
| L0 | every reference formulation has its reference and `R` replicas of each kind (lagged, independent, GSV=3) | the root criterion's list and `R` values, read from one constant table in this node | ✅ |
| L0 | every `exclusions.json` entry has a non-empty reason and evidence naming an existing file | the file | ✅ |
| L0 | every case JSON present under `cases/particle/` and `cases/statistics/` names its Fortran lines and its script (passes when neither directory exists) | those two directories only, never `cases/input/` or `cases/harness/` | ✅ |
| L0 | every file under `Legacy/` is a listed data member of the archive with its recorded hash, and every listed member is there; every executable digest of a provenance file is the manifest's | `archive-members.sha256`, `tools/legacy/original.sha256` | ✅ |
| L1 | `Legacy/` byte-equal to the archive members (source by re-transcoding), and the three derived fixtures regenerated | the archive at `PROPSTRUCT_LEGACY_DIR`, via `python tests/Fixtures/generate.py verify`; `Category=Legacy` | ✅ |
| L1 | the five files of the original are the recorded ones; the tree holds nothing of them beyond the quotation rule | `legacy.py check`, `scan.py .`; `Category=Legacy` | ✅ |

## Invariants

- These checks never run the executable.
- The L1 check shells out to `Fixtures`' own `generate.py verify` (its documented CLI,
  `Fixtures/API.md`) instead of re-deriving the CP1251 transcoding rule in C#: AGENTS.md
  §3 lets this node read only `Fixtures`' `API.md`, and duplicating a transcoding rule
  in two languages is a second implementation of the same check, not a second check.
- The GSV=3 input-hash check (`ProvenanceTests.Gsv3VariantOf`) is this node's own
  byte-level transform, built from `Fixtures`' BOOT.md, Constraints ("the sixth value
  of the NMM JZZ KXX N NNZ GSV record is replaced by 3 and nothing else is changed"),
  which this task's assignment named as a spec to read; it is proven against every
  real GSV=3 `provenance.json` entry (96 of them), not against a constructed example.

## Dependencies

- [Fixtures](../Fixtures/API.md) — what is being checked.
- [Harness](../Harness/API.md) — repository paths, `PythonScript`.
- [legacy](../../tools/legacy/API.md) — the manifest `original.sha256`, and the
  commands `legacy.py check` and `scan.py`, which the facts of `API.md`, "## Categories
  after the delivery", read and run.

Outside the tree: xunit.

## Constraints

- Part of the default test command.

## Acceptance criteria

- [x] Every row of the levels table is green, with a date and the names of the tests:
      2026-09-17, `PropStruct.Fixtures.Tests`, `ProvenanceTests`,
      `FixturesInventoryTests`, `LegacyArchiveTests`, 621 fast cases
      (`dotnet test tests/Fixtures.Tests --filter Category!=Long`).

  ⚠ 2026-09-17: this row read 217 cases from 2026-09-18 onward, when
  `FixturesInventoryTests`' two theories ran over one replica kind (GSV=3) only. This
  task's coding session gave `Fixtures` two more replica kinds (`replicas-lagged/`,
  `replicas-independent/`); both theories were reparameterized over
  `ReplicaDirectoryNames` instead of a single hardcoded path, tripling their case count
  (5 formulations × 3 kinds × 2 theories = 30 vs. 10), for a new total of 621.

  ⚠ 2026-09-18: this row first claimed all seven table rows green with 216 passing
  cases. Merging coder A's `tests/Fixtures/cases/input/*.json` (Input.Tests'
  constructed `.dat` cases) turned `EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript`
  red for the wrong reason: the check scanned every `cases/*` subdirectory except
  `harness`, so `cases/input/malformed-cases.json` — never a formula-script case — was
  held to a citation rule it was never subject to. Narrowed to `cases/particle/` and
  `cases/statistics/` only (`Fixtures/BOOT.md`'s acceptance criterion: "every formula
  script case consumed by `Particle.Tests` and `Statistics.Tests`").

  ⚠ 2026-09-18, same day: with the false positive gone, this row briefly failed on
  purpose instead when both directories were absent (`Assert.Fail`, "say so
  explicitly, not pass vacuously"), reasoning that an empty loop passing is
  indistinguishable from "checked and clean". That made
  `EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript` a check that stays red until
  `Particle`/`Statistics` exist — exactly the "perpetually red check" AGENTS.md §13
  forbids ("keeping it red ... is not allowed"), because the *existence* of formula
  cases is already guarded where they are consumed: `Particle.Tests`/`Statistics.Tests`
  fail when their own cases are missing, and the root criterion's acceptance checkbox
  ("every formula script case consumed by `Particle.Tests` and `Statistics.Tests`
  exists and names its Fortran lines") stays unticked until then. This row only needs
  to check the *shape* of whatever case JSON exists, which an empty scan does
  correctly (there is nothing malformed to find). Reverted to pass on an empty scan;
  `TheCitationRuleFailsOnAConstructedCaseMissingItsCitations` (below) remains the
  non-degeneracy proof, on a real constructed case in a temporary directory, so the
  rule is proven without depending on either directory's existence either way.
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13):
      2026-09-17, one mutation applied and reverted, seen red then green again:
      `tests/Fixtures.Tests/ProvenanceTests.cs`, `Gsv3VariantOf`'s replacement
      character changed from `"3"` to `"4"`. Red: 96/101 cases of
      `ProvenanceTests.InputHashMatchesTheShippedDatOrItsGsv3Variant` (the 5 GSV=2
      cases never call `Gsv3VariantOf` and stayed green, which is itself evidence the
      branch is exercised correctly). 2026-09-18: the case-JSON citation rule's own
      non-degeneracy does not use a source mutation — it is proven directly by
      `TheCitationRuleFailsOnAConstructedCaseMissingItsCitations`, which asserts the
      rule throws on a constructed case missing `fortranLines` and does not throw once
      the same case gains it.

- [x] The facts of `API.md`, "## Categories after the delivery", exist and are proven
      (stage S2, 2026-10-04): `LegacyManifestTests` red once on a flipped byte of a data
      file (`formulations/A1.dat`), on one character of `archive-members.sha256`, on a
      file added under `Legacy/` and on one executable digest of `provenance.json` (1 of
      its 3 cases each, 2 of 594 with `ProvenanceTests`), green again reverted;
      `TheOriginalIsTheRecordedOne` red with the variable unset (exit 2) and on a copy
      of the directory with one byte of `PropStructv3.for.txt` changed (`legacy.py
      check` exit 1, naming the file and both hashes);
      `TheForbiddenListIsTheOneTheArchiveGenerates` red with one line of
      `forbidden.sha256` deleted (`forbidden --check` exit 1) and green restored; the 4
      `Legacy` facts of this node green with the variable set (17 s) and 0 of 4 with it
      unset; the fast set green with the five files absent and the variable unset, 627
      cases of this node.

## Taboos

- Do not skip a check because a fixture is missing: a missing fixture is a failure.

  ⚠ 2026-09-18: read literally this taboo would also demand
  `EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript` fail while `cases/particle/`
  and `cases/statistics/` are absent, which was tried and reverted (see the
  acceptance-criteria note above): that row does not guard the *existence* of a
  fixture, `Particle.Tests`/`Statistics.Tests` do, so an absent directory there is not
  "a missing fixture" in this taboo's sense — it is nothing to check yet. The taboo
  still stands for every other row: a missing `provenance.json` entry, replica file
  or `exclusions.json` entry is a failure, not a skip, because those rows check that a
  fixture already claimed to exist truly does.
