# BOOT.md — Input.Tests

## Purpose

The definition of what "Input is ready" means.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | list-directed reading rules, `Length` | constructed files and values in `tests/Fixtures/cases/input/` | ✅ 2026-09-17, `PositiveCasesTests` |
| L0 | every malformed case rejected with its line number | constructed files | ✅ 2026-09-17, `MalformedCasesTests` |
| L1 | every archived `.dat` parses | the file list of `tests/Fixtures/Legacy/formulations/`, generated | ✅ 2026-09-17, `ArchivedFormulationsTests` |
| L1 | parsed headers | header lines of the fixture `results.m` files at print precision, through `Harness.ResultsMFile` | ✅ 2026-09-17, `ReferenceHeaderTests` |
| L1 | `ModelParameters.Default` | parameter lines of the HPEPA3 reference `results.m` | ✅ 2026-09-17, `ReferenceHeaderTests.ModelParametersDefaultMatchesTheHpepa3ReferenceHeader` |
| L1 | line-map coverage | the list of executable lines of Fortran 66–266, generated from the source | ✅ 2026-09-24, `LineMapCoverageTests` |

## Invariants

- Constructed files live in `tests/Fixtures`, not as strings in test code, so that
  their bytes (encodings, tabs, CR LF) are what the reader sees.

⚠ 2026-09-17: this section first recorded a stand-in, `tests/Input.Tests/LegacyHeaderReader.cs`,
a test-local, narrow reader of only the header lines the "parsed headers" and
`ModelParameters.Default` rows needed, written because `Harness.ResultsMFile` (the general
`results.m` parser, `tests/Harness/API.md`) did not exist yet in this wave. It has since been
written; `ReferenceHeaderTests` now reads through it directly (`header["Name"][0]` for a scalar,
`header["Gfr"]` for an array), and `LegacyHeaderReader.cs` is deleted.

## Dependencies

- [Input](../../src/Input/API.md) — what is being checked.
- [Harness](../Harness/API.md) — the `results.m` parser, repository paths.
- [Fixtures](../Fixtures/API.md) — formulations, outputs, constructed files.

Outside the tree: xunit.

## Constraints

- Part of the default test command.

## Acceptance criteria

- [x] Every row of the levels table is green, with a date and the names of the tests.
      2026-09-17: see the table above.
- [x] Every check is proven non-degenerate by a recorded mutation (AGENTS.md §13).
      2026-09-17, each seen red then restored, `dotnet test PropStruct.Input.Tests.csproj`:
    - the record-continuation rule: `src/Input/DatRecordReader.cs`,
      `ReadValuesWithLines`'s outer `while (found < count)` changed to
      `if (found < count)` &rarr; red only on
      `PositiveCasesTests.ParsedFormulationMatchesTheConstructedCase(caseName: "record-continuation")`
      (`FormulationFormatException: 'label' is not a valid number`, the label record
      read as data once the loop no longer moves to the next record).
    - the &ge; 0.1 &micro;m rule: `src/Input/Length.cs`, `IsMicrometres` changed from
      `AsWritten >= 0.1` to `AsWritten > 0.1` &rarr; red only on
      `PositiveCasesTests.TheGreaterOrEqualPointOneMicrometreRuleAppliesElementByElement`
      (the boundary value `0.1` read back as metres instead of micrometres).
    - `GSV != 2` rejection: `src/Input/DatFile.cs`, the check changed from
      `gsv != 2` to `gsv != 1` &rarr; red on
      `MalformedCasesTests.RejectedWithItsLineNumber(caseName: "bad-gsv")` (no longer
      rejected at its own line; the malformed file runs past it and fails later, at
      the true end of its (short, deliberately truncated) file) and
      `RejectedWithItsLineNumber(caseName: "bad-repeat-count")` (a valid `GSV = 2`
      file now rejected before ever reaching the repeat count it exists to exercise).
    - a menu default: `src/Input/ModelParameters.cs`, `Default.Alpha` changed from
      `0.25` to `0.35` &rarr; red on `ModelParametersTests.DefaultMatchesTheConstructedCase`
      and `ReferenceHeaderTests.ModelParametersDefaultMatchesTheHpepa3ReferenceHeader`.
    - 2026-09-24, `LineMapCoverageTests`: `src/Input/BOOT.md`'s `## Line map` row
      `144–244` removed &rarr; red, missing 182 executable lines starting at 145;
      restored, diffed byte-identical against the pre-mutation file.

- [x] `LineMapCoverageTests` reads `tests/Fixtures/cases/source/executable_lines.json`
      and is red once on a line the map no longer covers (stage S2, 2026-10-04: the
      row `66–80` of `src/Input/BOOT.md`'s line map cut to `66–79`: red, "Line map does
      not cover 2 executable line(s) of the source, by number: 80, 80", no source line
      in the message; reverted, green; 70 cases of the fast set with the original
      absent).

  ⚠ 2026-10-04: was "red once on a line removed from the list", which no mutation can
  meet: a shorter list of executable lines asks the map for less. Reformulated as above.

## Taboos

- Do not type a list of archived formulations.
