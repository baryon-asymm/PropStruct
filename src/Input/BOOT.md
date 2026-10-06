# BOOT.md — Input

## Purpose

The original's input vocabulary as records: the `.dat` formulation file and the
fourteen model parameters the original asked for in its interactive menu, with the
original defaults. A node of its own at the bottom of the tree so that `Output` and
`Cli` can read a formulation without the simulator.

Line numbers refer to the original source `PropStructv3.for`.

⚠ 2026-10-04: the source this node's line numbers refer to, named here by its path under `tests/Fixtures/Legacy`, lies outside the repository from stage S2 (`tools/legacy`): a reader of the public tree cannot consult it, and the checks that read it are `Category=Legacy` → tests/Fixtures/HISTORY.md#legacy-out-of-tree-2026-10-04

## Invariants

- **The original's reading order.** A `.dat` file is read as lines 93–133 and 245–248
  read it: skip one record; list-directed read of `PLOT1 PLOT2 GGG Gm`; skip one
  record; `AK1 AK2 AK3 AK4`; skip; `NMM JZZ KXX N NNZ GSV`; skip; `NMM` mass shares;
  skip; `2·NMM` fraction bounds; and, only when `ReadPocketFormingFractions` is set,
  skip and `NMM` integers `SFR`.
- **List-directed reading as Digital Visual Fortran does it, for the forms the files
  use.** A read takes the values it needs from the current record and continues on
  following records if the record runs out; the rest of the last record used is
  ignored (trailing text included); values are separated by blanks, tabs or commas; a
  number may carry a decimal point, an exponent (`E`/`D`, either case) or neither.
  Integer items accept a real form with an integral value (`6E6` for `NNZ`). Repeat
  counts (`r*v`) and `/` are rejected with a message, since no archived file uses
  them.
- **Skipped records are never decoded.** Their encodings vary across the archive
  (CP866, UTF-8, a UTF-8 byte-order mark); the reader works on bytes for them.
- **Lengths keep the value as written.** Fraction bounds, `Dmin`, `Di`, `Dj` are
  `Length` values holding the number as written; a value `≥ 0.1` is micrometres, any
  other value metres, element by element (lines 260–266). `Metres` is
  `AsWritten·1e-6` or `AsWritten` in `double`. `Statistics` derives the binary32 values
  of the original from `AsWritten`.
- **Defaults of the menu** (lines 66–80): `Dmin = Di = Dj = 10e-6` written as metres,
  `EpsDok = 0.05`, `Alpha = 0.25`, `NnMin = 3`, `NnMax = 100`,
  `PocketCoefficient = 8.2`, `BridgeCoefficient = 7.73`, `TailProbability = 0`,
  `HomogenizedOxidizerFraction = 0`, `ReadPocketFormingFractions = false`,
  `Variant = 0`, `AggregatedOxideFraction = 0`.
- **Source of the coefficients' meaning:** V. A. Babuk, A. A. Nizyaev, Khimicheskaya
  Fizika i Mezoskopiya 16 (1), 2014, pp. 31–42 (reference in `NOTICE`). Its matching
  coefficients are `Dmin` (Dок min = 10 µm, 30 µm for active-binder propellants),
  `Alpha` (k1 = 0.25), `NnMin` (k2 = 3.0) and `BridgeCoefficient` (k3 = 7.73), equal to
  the defaults above; `PocketCoefficient` is not in that paper.
- **Normalizations of the original:** `KXX < 1` becomes 1 (lines 136–143). A `.dat` with
  `GSV ≠ 2` is rejected (root decision). `NNZ` is read and kept, never used.
- **Culture-invariant** parsing; no console input.

## Dependencies

None.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Host code: allocation and exceptions are allowed.
- The `.dat` value `JZZ` is kept as read in `Formulation.SizeLawCode`; `SizeLaw` maps
  `2` to `Uniform` and any other value to `UniformInReciprocalSquare`.
- Physical plausibility (shares summing to 1, bounds ordered) is not checked here:
  `Statistics.Setup.Prepare` owns the preconditions of the model.
- A number converted from text to `double` and then to binary32 can differ from a
  direct text-to-binary32 conversion only at exact binary32 ties; the port accepts
  that (declared).

## Line map

This node's Fortran source is `tests/Fixtures/Legacy/PropStructv3.for.txt`, lines
66–266 (`tests/Input.Tests`' line-map coverage test reads this sentence, not the
table, for the ranges it scans, the same pattern `src/Particle/BOOT.md` uses). Of
these, 66–80, 93–133, 136–143, 245–248 and 260–266 were already transcribed by the
`## Invariants` above; 81–92, 134–135, 144–244 and 249–259 are declared here
(2026-09-24, closing a fidelity-audit finding: claims already rested on these lines
without any node declaring them).

| Fortran | C# member | Note |
|---|---|---|
| 66–80 | `ModelParameters.Default` | the original's own menu defaults, "## Invariants", "Defaults of the menu" |
| 81 | — | comment |
| 82–92 | — | console banner and the interactive input-file-name prompt/open; not ported (`## Taboos`, "No reading of the console or of any file other than the one given") |
| 93–119 | — | interactive dialogue for the output `.m` file (exists/overwrite prompt); not ported, same reason |
| 120–133 | `DatFile.Parse` | the `.dat`'s own content, in order: `PLOT1 PLOT2 GGG Gm`; `AK1 AK2 AK3 AK4`; `NMM JZZ KXX N NNZ GSV`; `NMM` mass shares; `2·NMM` fraction bounds — "## Invariants", "The original's reading order" |
| 134 | — | `119 CONTINUE`, an unreferenced label: no `goto`/`go to` anywhere in the source targets it |
| 135 | `Formulation.PocketFormingFractions` | `SFR = 1`: the original's default when the fractions are not read from the file is "every fraction pocket-forming"; the port represents that as `PocketFormingFractions == null` (`API.md`, `Formulation`) |
| 136–143 | — | `KXX < 1` normalized to 1, "## Invariants", "Normalizations of the original" |
| 144–244 | `ModelParameters` | the interactive menu, fourteen items; not read interactively — the port takes every value as an option with the original default (root decision; `ModelParameters.cs:5` cites 159–166 for the menu's own aliasing labels) |
| 245–248 | `DatFile.Parse` | conditional `SFR` read, "## Invariants", "The original's reading order" |
| 249 | — | `CLOSE(UNIT=2)` |
| 250–257 | — | timestamp and console echo of `N`/cycles; not ported — `Cli` prints its own progress (`src/Cli/BOOT.md`, "Progress and the summary") |
| 258–259 | — | comment |
| 260–266 | `Length` | unit conversion µm → m, "## Invariants", "Lengths keep the value as written" |

## Acceptance criteria

- [x] Every `.dat` of the original archive parses; the list is generated from the
      fixture directory. 2026-09-17:
      `tests/Input.Tests/ArchivedFormulationsTests.EveryArchivedFormulationParses`,
      one case per file of `tests/Fixtures/Legacy/formulations/` (49 files, enumerated
      by the test, not typed).
- [x] Parsed values of the formulations whose `results.m` is in the fixtures equal the
      header lines of that file at their print precision (`Plot1`, `Plot2`, `Gdok`,
      `Gm`, `Nfr`, `JZ`, `Cycles`, `N`, `Gfr`, `Dfr` in metres). 2026-09-17:
      `tests/Input.Tests/ReferenceHeaderTests.ParsedFormulationMatchesTheReferenceHeader`,
      HPEPA3, inpt, P33, PSAN02n, HMX against
      `tests/Fixtures/references/<name>/results.m.txt`.
- [x] `ModelParameters.Default` equals the parameter lines of the reference
      `results.m` of HPEPA3. 2026-09-17:
      `tests/Input.Tests/ReferenceHeaderTests.ModelParametersDefaultMatchesTheHpepa3ReferenceHeader`.
- [x] Constructed files: a value spread over two records, trailing text, tabs and
      commas, `6E6` as an integer, a mixed-unit bounds line (as P35050n), a UTF-8 BOM
      label, the SFR line; each malformed case (missing value, `r*v`, `/`, a
      non-integral integer, `GSV ≠ 2`) is rejected with its line number. 2026-09-17:
      `tests/Input.Tests/PositiveCasesTests` and `MalformedCasesTests`, against
      `tests/Fixtures/cases/input/*.dat` and their `*.json` expected values.
- [x] The line map covers every executable line of 66–266; the list of lines is
      generated from the source. 2026-09-24:
      `tests/Input.Tests/LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`.

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells |
|---|---|---|---|---|---|
| 136–143 | algorithm | `KXX < 1` becomes 1 (`## Invariants`, "Normalizations of the original"). | not measured | reproduced | none |
| 93–133 | dead | `NNZ` is read and kept, never used (`## Invariants`, "Normalizations of the original"). | not measured | reproduced | none |

## Taboos

- No reading of the console or of any file other than the one given.
- No silent defaulting of a value missing from the file.
- No decoding of label records.
