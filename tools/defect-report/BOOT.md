# BOOT.md — defect-report

## Purpose

The assembler of `docs/ORIGINAL-DEFECTS.md`: one page saying what is wrong with the
legacy Fortran program PropStructV3, what each defect changes, and what this port does
about it. The page exists for a reader who has to trust, or distrust, a number the
original printed — a researcher reading archived results, a reviewer of the port, the
next person to touch the model. Alongside it, the node assembles a machine-readable
twin, `docs/declared-differences.json`, of the one column a program needs to read
without parsing Markdown: which printed cells a declared defect names as differing
from the original, so that `CompareSets` (`tests/Harness`) can find them by name.

The page is **generated**. Every claim in it already lives in the `BOOT.md` of the node
that transcribes the corresponding Fortran lines; this node only collects, orders and
formats. It invents nothing, and it is the reason the page cannot become a second source
of truth about the original (root `BOOT.md`, "The defect report is assembled, never
written").

## Invariants

- **Nothing is written except the two generated files.** The node reads the tree's
  documents and writes `docs/ORIGINAL-DEFECTS.md` and `docs/declared-differences.json`,
  and nothing else, ever.

  ⚠ 2026-09-27: was "the generated page", one file, now the JSON twin of the
  `Differing cells` column joins it as a second generated file.
- **No claim of its own.** Every row of the output comes from a row of a node's
  `## Defects of the original` table, with the node named. Text is copied, not
  paraphrased; the only text the node authors is the preamble and the section headings.
- **A malformed table is an error, not a silent skip.** A missing column, an unknown
  `Kind`, an unknown value in `The port`, a `Consequence` that is neither a measurement
  with a place nor the words `not measured`: each fails the run, naming file and line.
  A defect quietly dropped from the report is worse than no report.
- **The order is by consequence, not by file.** `algorithm` first, then `statistics`,
  `numeric`, `dead`, `cosmetic`; inside a kind, by the first Fortran line number. The
  reader's question is "what changes the answer", and the page answers it in its first
  screen.
- **Standard library of Python 3.8+ only**, as `tools/protocol-lint` has it, for the
  same reason: the check must run before anything is installed.
- **Idempotent.** Running it twice writes the same bytes; `--check` writes nothing and
  fails when the file in the tree differs from what would be generated.

## Dependencies

None.

Outside the tree: Python 3.8+ (standard library), `unittest` for the self-test.

⚠ The node reads the `BOOT.md` of every node of the tree. That is not a dependency in
the sense of AGENTS.md §6: it reads them as text, as `tools/protocol-lint` does, and
knows nothing of their subject matter.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- The input format is the one the root fixes: a section `## Defects of the original`
  holding one Markdown table with the columns **Fortran | Kind | What the original
  does | Consequence | The port | Differing cells**, and nothing else.
- The node never reads the Fortran source. It has no opinion on whether a row is true;
  that is the transcribing node's own claim, and the protocol's answer to "is the
  document true about the code" is a human and the periodic reconciliation
  (AGENTS.md §13).
- The generated page carries, at its top, the date of generation, the command that
  produces it and the warning that hand edits are overwritten.

## Acceptance criteria

- [x] 2026-09-20: `--check` is part of the repository's checks
      (`CLAUDE.md`, "## Commands") and fails when a node's table changes without the
      page being regenerated, shown red once by hand on the real tree and then
      reverted: the wording of `src/Output/BOOT.md`'s one row was changed
      ("mislabeling" → "mislabelling"), `--check` printed a one-line diff and exited 1,
      the wording was reverted and `--check` exited 0 again. The same is proven as a
      standing test, `tools/defect-report/test_defect_report.py`,
      `DefectReportTest.test_check_fails_when_the_tree_diverges_from_the_generated_page`.
- [x] 2026-09-20: every malformed-table case above is covered by a self-test case, each
      proven red: `tools/defect-report/test_defect_report.py`, `DefectReportTest` —
      no table in the section, a wrong header, prose left in the section, a row with a
      missing column, an unknown `Kind`, an unknown value in `The port`, a `Consequence`
      that is a guess, a measured `Consequence` with no backtick-quoted place, and an
      empty `Fortran` cell.
- [x] 2026-09-20: every node that transcribes Fortran lines has a table, and the count
      of rows in the page equals the sum of rows in the nodes, compared by
      `RealTreeTest.test_row_count_equals_the_sum_of_rows_in_the_nodes` on the real
      tree (a second, independent line walk, not `collect`'s own tally); the count the
      test compares is the tree's own, never a number typed here, and stood at 21 rows
      from 6 nodes on the day of this tick (`src/Random`, `src/Particle`, `src/Statistics`, `src/Simulation`,
      `src/Output`, `src/Input`); `src/Execution` transcribes no Fortran lines of its
      own and carries no section.
- [x] 2026-09-20: the page's first section is `algorithm` and every row of it carries
      either a measurement with its place or the words `not measured`:
      `RealTreeTest.test_the_first_section_is_algorithm_with_a_placed_or_not_measured_consequence`.
- [x] 2026-09-27: every malformed `Differing cells` case (a malformed span, first >
      last, a non-numeric threshold, a duplicate entry, a `reproduced` row whose
      column is not `none`) is covered by a self-test case, each proven red, in
      `tools/defect-report/test_defect_report.py`, `DefectReportTest`:
      `test_a_malformed_span_is_an_error`,
      `test_a_span_with_first_greater_than_last_is_an_error`,
      `test_a_non_numeric_threshold_is_an_error`,
      `test_a_duplicate_entry_is_an_error`,
      `test_a_reproduced_row_with_differing_cells_is_an_error`. `--check` also fails
      when the JSON twin alone goes stale, the page unchanged:
      `test_check_fails_when_the_declared_json_is_stale_but_the_page_is_not`.
- [x] 2026-10-02: `docs/declared-differences.json` holds exactly the rows of the
      defects that fill in `Differing cells`, as the tool's own parser reads them from
      the nodes, compared with no typed count, in `RealTreeTest`:
      `test_the_committed_declared_differences_hold_exactly_the_rows_with_entries`.
      Proven red:
      `test_a_declared_differences_file_with_an_extra_or_a_missing_row_is_rejected`
      runs the same check on a copy with an added row and on one with a dropped row,
      and both are rejected; the committed file was also edited by hand with a fake
      extra row, the test failed, the file was restored and the whole self-test passed
      (31 tests). Right: the unmutated tree passes, three rows on the day of this tick.

  ⚠ 2026-10-02: was "the real tree fills in exactly four `Differing cells` entries, and
  `docs/declared-differences.json` holds exactly those four rows"
  (`test_declared_differences_has_four_rows_equal_to_the_defects_with_entries`, ticked
  2026-09-27). The count was typed by hand and went stale when 874d81c (2026-10-01)
  turned row 771-1175 to `reproduced`, leaving three; the self-test was red from then
  until the orchestrator's kit audit found it, since nothing runs it automatically.
  Now the criterion names no count: "exactly" is checked against the list the parser
  generates (AGENTS.md section 6).

## Taboos

- No claim authored by this node.
- No reading of the Fortran source.
- No writing outside `docs/ORIGINAL-DEFECTS.md` and `docs/declared-differences.json`.

  ⚠ 2026-09-27: was "outside `docs/ORIGINAL-DEFECTS.md`" alone, now both generated
  files, matching the invariant above.
- No network, no package outside the standard library.
- No silent skip of a row it cannot parse.
