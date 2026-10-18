#!/usr/bin/env python3
"""Proof that defect_report's collection and rendering are non-degenerate.

Two groups of tests. `DefectReportTest` builds a small synthetic tree and breaks
exactly one thing the contract guards, so that each malformed-table case is seen red
once (AGENTS.md 13). `RealTreeTest` runs the tool against the actual repository tree,
because two of defect-report's own acceptance criteria - the row count matching the
nodes, and the committed page being up to date - can only be proven on the real tree,
not on a fixture.

    python -m unittest tools/defect-report/test_defect_report.py
"""

from __future__ import annotations

import contextlib
import datetime
import io
import json
import re
import sys
import tempfile
import unittest
from pathlib import Path
from typing import List

sys.path.insert(0, str(Path(__file__).resolve().parent))

import defect_report  # noqa: E402

AGENTS = "# AGENTS.md\n\nThe protocol.\n"

ROOT_BOOT = """# BOOT.md - root

## Purpose

The tree root.

## Invariants

- None of its own.

## Dependencies

None.

## Constraints

None.

## Acceptance criteria

- [x] It exists (2026-09-20, `RootExists`).

## Taboos

- None.
"""

ROOT_API = "# API.md - root\n\nThe system as a whole.\n"

NODE_BOOT = """# BOOT.md - {name}

## Purpose

{name} transcribes some Fortran lines.

## Invariants

- It transcribes them faithfully.

## Dependencies

None.

## Constraints

Inherited from the parent ([BOOT.md](../BOOT.md)).

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells | Corrected |
|---|---|---|---|---|---|---|
{rows}

## Acceptance criteria

- [x] It transcribes them (2026-09-20, `TranscribesThem`).

## Taboos

- None.
"""

NODE_API = "# API.md - {name}\n\nWhat {name} offers outward.\n"

ONE_ROW = ("| 100 | dead | leaves X unassigned | not measured | declared, not reproduced "
           "| none | unchanged |")
MEASURED_ROW = ("| 200 | statistics | biases Y | measured: `tests/Fixtures` records a 3% shift "
                "| reproduced | none | unchanged |")


class DefectReportTest(unittest.TestCase):
    """A synthetic two-node tree, mutated one way per test."""

    def setUp(self) -> None:
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name).resolve()

        self.write("AGENTS.md", AGENTS)
        self.write("BOOT.md", ROOT_BOOT)
        self.write("API.md", ROOT_API)

        self.write("a/BOOT.md", NODE_BOOT.format(name="a", rows=ONE_ROW))
        self.write("a/API.md", NODE_API.format(name="a"))

        self.write("b/BOOT.md", NODE_BOOT.format(name="b", rows=MEASURED_ROW))
        self.write("b/API.md", NODE_API.format(name="b"))

    def write(self, relative: str, text: str) -> Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path

    def collect(self) -> List[defect_report.Defect]:
        return defect_report.collect(str(self.root))

    def assertFormatError(self, needle: str, node: str) -> None:
        with self.assertRaises(defect_report.DefectFormatError) as caught:
            self.collect()
        self.assertIn(needle, str(caught.exception))
        self.assertIn(node, caught.exception.path)

    # -------------------------------------------------------------- the baseline

    def test_a_conforming_tree_collects_two_defects(self) -> None:
        defects = self.collect()
        self.assertEqual(2, len(defects))
        self.assertEqual({"a", "b"}, {defect.node for defect in defects})

    def test_a_node_with_no_section_is_skipped_without_error(self) -> None:
        self.write("c/BOOT.md", ROOT_BOOT.replace("root", "c"))
        self.write("c/API.md", NODE_API.format(name="c"))
        defects = self.collect()
        self.assertEqual({"a", "b"}, {defect.node for defect in defects})

    # ------------------------------------------------------- malformed: no table

    def test_a_section_with_no_table_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "| Fortran | Kind | What the original does | Consequence | The port "
                "| Differing cells | Corrected |\n|---|---|---|---|---|---|---|\n" + ONE_ROW,
                "Prose instead of a table.",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("no table", "a")

    def test_a_wrong_header_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("Fortran | Kind", "Line | Kind"),
            encoding="utf-8",
        )
        self.assertFormatError("header must be exactly", "a")

    def test_prose_left_inside_the_section_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                ONE_ROW, ONE_ROW + "\n\nA sentence that is not a table row.",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("not a table row", "a")

    # -------------------------------------------------------- malformed: a row

    def test_a_row_with_a_missing_column_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                ONE_ROW,
                "| 100 | dead | leaves X unassigned | not measured | declared, not reproduced "
                "| none |",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("has 6 cells, expected 7", "a")

    def test_an_unknown_kind_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(boot.read_text(encoding="utf-8").replace("| dead |", "| lethal |"), encoding="utf-8")
        self.assertFormatError("unknown Kind", "a")

    def test_an_unknown_port_value_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("declared, not reproduced", "fixed"),
            encoding="utf-8",
        )
        self.assertFormatError("unknown value", "a")

    def test_a_consequence_that_is_a_guess_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("not measured", "probably harmless"),
            encoding="utf-8",
        )
        self.assertFormatError("neither a measurement", "a")

    def test_a_measured_consequence_without_a_backtick_place_is_an_error(self) -> None:
        boot = self.root / "b" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "measured: `tests/Fixtures` records a 3% shift", "measured: it shifts by about 3%",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("neither a measurement", "b")

    def test_an_empty_fortran_cell_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(boot.read_text(encoding="utf-8").replace("| 100 |", "|  |"), encoding="utf-8")
        self.assertFormatError("Fortran is empty", "a")

    # ------------------------------------------------------- malformed: differing cells

    def test_a_reproduced_row_with_differing_cells_is_an_error(self) -> None:
        boot = self.root / "b" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "| reproduced | none |", "| reproduced | `Y[0]` |",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("a 'reproduced' row's 'Differing cells' must be 'none'", "b")

    def test_a_malformed_span_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("| none |", "| `*: X[abc]` |"),
            encoding="utf-8",
        )
        self.assertFormatError("the span [abc] in '*: X[abc]' is malformed", "a")

    def test_a_span_with_first_greater_than_last_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("| none |", "| `*: X[5..2]` |"),
            encoding="utf-8",
        )
        self.assertFormatError("has first > last", "a")

    def test_a_non_numeric_threshold_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "| none |", "| `*: X where original < abc` |",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("is not a number", "a")

    def test_a_duplicate_entry_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "| none |", "| `*: X; *: X` |",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("duplicates an earlier entry", "a")

    def test_differing_cells_parses_scope_range_and_threshold(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "| none |",
                "| `HPEPA3,HMX: fqdokkarm(3,:)[1..2] where original < 1e-30; *: pdoksmall` |",
            ),
            encoding="utf-8",
        )
        defects = [defect for defect in self.collect() if defect.node == "a"]
        self.assertEqual(1, len(defects))
        cells = defects[0].differing_cells
        self.assertEqual(2, len(cells))
        self.assertEqual(
            defect_report.DeclaredCell(("HPEPA3", "HMX"), "fqdokkarm(3,:)", 1, 2, 1e-30),
            cells[0],
        )
        self.assertEqual(defect_report.DeclaredCell(("*",), "pdoksmall", None, None, None), cells[1])

    # ------------------------------------------------------- the 'Corrected' column

    def set_corrected(self, node: str, old: str, new: str) -> None:
        """Replace the 'Corrected' cell of the node's one row (old must be that cell)."""
        boot = self.root / node / "BOOT.md"
        text = boot.read_text(encoding="utf-8")
        self.assertEqual(1, text.count("| " + old + " |\n"))
        boot.write_text(text.replace("| " + old + " |\n", "| " + new + " |\n"), encoding="utf-8")

    def test_an_old_six_column_header_is_an_error(self) -> None:
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace(
                "| Differing cells | Corrected |\n|---|---|---|---|---|---|---|\n" + ONE_ROW,
                "| Differing cells |\n|---|---|---|---|---|---|\n"
                "| 100 | dead | leaves X unassigned | not measured "
                "| declared, not reproduced | none |",
            ),
            encoding="utf-8",
        )
        self.assertFormatError("header must be exactly", "a")

    def test_an_unknown_corrected_value_is_an_error(self) -> None:
        self.set_corrected("a", "unchanged", "mostly unchanged")
        self.assertFormatError("unknown 'Corrected' value 'mostly unchanged'", "a")

    def test_an_empty_corrected_value_is_an_error(self) -> None:
        self.set_corrected("a", "unchanged", "")
        self.assertFormatError("unknown 'Corrected' value ''", "a")

    def test_not_reachable_on_a_non_numeric_row_is_an_error(self) -> None:
        self.set_corrected("a", "unchanged", "not reachable")
        self.assertFormatError("allowed only on a 'numeric' row, this row is 'dead'", "a")

    def test_not_reachable_on_a_row_the_port_does_not_reproduce_is_an_error(self) -> None:
        for port in ("declared, not reproduced", "open"):
            with self.subTest(port=port):
                self.write("a/BOOT.md", NODE_BOOT.format(
                    name="a",
                    rows="| 100 | numeric | rounds in REAL*4 | not measured | {} | none "
                         "| not reachable |".format(port)))
                self.assertFormatError(
                    "allowed only where 'The port' is 'reproduced', this row's is "
                    "{!r}".format(port), "a")

    def test_unchanged_is_accepted_on_a_numeric_row_the_port_does_not_reproduce(self) -> None:
        self.write("a/BOOT.md", NODE_BOOT.format(
            name="a",
            rows="| 100 | numeric | rounds in REAL*4 | not measured "
                 "| declared, not reproduced | none | unchanged |"))
        defects = [defect for defect in self.collect() if defect.node == "a"]
        self.assertEqual(["unchanged"], [defect.corrected for defect in defects])

    def test_not_reachable_on_a_numeric_row_is_accepted(self) -> None:
        self.write("a/BOOT.md", NODE_BOOT.format(
            name="a",
            rows="| 100 | numeric | rounds in REAL*4 | not measured | reproduced | none "
                 "| not reachable |",
        ))
        defects = [defect for defect in self.collect() if defect.node == "a"]
        self.assertEqual(["not reachable"], [defect.corrected for defect in defects])
        self.assertEqual([()], [defect.corrections for defect in defects])

    def test_a_malformed_fixed_value_is_an_error(self) -> None:
        for malformed in ("fixed", "fixed ()", "fixed (1Bad)", "fixed (A,B)", "fixed (A, )",
                          "fixed (A-B)", "fixed A", "Fixed (A)", "fixed (A) extra"):
            with self.subTest(malformed=malformed):
                self.write("a/BOOT.md", NODE_BOOT.format(
                    name="a", rows=ONE_ROW.replace("unchanged", malformed)))
                self.assertFormatError("unknown 'Corrected' value", "a")

    def test_a_fixed_value_naming_a_member_twice_is_an_error(self) -> None:
        self.set_corrected("a", "unchanged", "fixed (A, B, A)")
        self.assertFormatError("names a member twice", "a")

    def test_a_good_fixed_value_is_parsed_into_corrections(self) -> None:
        self.set_corrected("a", "unchanged", "fixed (A, Bridge_2, _c)")
        defects = {defect.node: defect for defect in self.collect()}
        self.assertEqual("fixed (A, Bridge_2, _c)", defects["a"].corrected)
        self.assertEqual(("A", "Bridge_2", "_c"), defects["a"].corrections)
        self.assertEqual(("unchanged", ()), (defects["b"].corrected, defects["b"].corrections))

    def test_a_fixed_value_is_accepted_on_any_kind(self) -> None:
        for kind in defect_report.KINDS:
            with self.subTest(kind=kind):
                self.write("a/BOOT.md", NODE_BOOT.format(
                    name="a",
                    rows="| 100 | {} | does X | not measured | open | none "
                         "| fixed (A) |".format(kind)))
                defects = [defect for defect in self.collect() if defect.node == "a"]
                self.assertEqual([("A",)], [defect.corrections for defect in defects])

    def test_the_page_shows_the_column_and_counts_the_rows_by_value(self) -> None:
        self.write("a/BOOT.md", NODE_BOOT.format(
            name="a",
            rows="| 100 | numeric | rounds in REAL*4 | not measured | reproduced | none "
                 "| not reachable |\n"
                 "| 101 | numeric | rounds again | not measured | reproduced | none "
                 "| not reachable |\n"
                 "| 102 | algorithm | drops a term | not measured | open | none "
                 "| fixed (A, B) |\n"
                 "| 103 | dead | leaves X unassigned | not measured | open | none "
                 "| fixed (C) |\n"
                 "| 104 | cosmetic | misprints | not measured | open | none "
                 "| unchanged |",
        ))
        content = defect_report.render(self.collect(), datetime.date(2026, 9, 20))
        # Node b's one row is 'unchanged' too: 2 unchanged, 2 not reachable, 2 fixed.
        self.assertIn("By `Corrected`: 2 unchanged, 2 not reachable, 2 fixed.", content)
        self.assertIn("| The port | Corrected |", content)
        self.assertIn("| open | fixed (A, B) |", content)
        self.assertIn("| reproduced | not reachable |", content)

    def test_the_declared_json_does_not_carry_the_corrected_column(self) -> None:
        self.set_corrected("a", "unchanged", "fixed (A)")
        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("| none |", "| `*: X` |"),
            encoding="utf-8",
        )
        document = json.loads(defect_report.declared_differences(self.collect()))
        self.assertEqual(1, len(document["rows"]))
        self.assertEqual({"node", "fortran", "kind", "port", "entries"},
                         set(document["rows"][0].keys()))

    # ------------------------------------------------------------- rendering

    def test_rendering_is_idempotent(self) -> None:
        defects = self.collect()
        date = datetime.date(2026, 9, 20)
        self.assertEqual(defect_report.render(defects, date), defect_report.render(defects, date))

    def test_kinds_are_ordered_algorithm_first(self) -> None:
        self.write("a/BOOT.md", NODE_BOOT.format(
            name="a",
            rows="| 999 | algorithm | changes the answer | not measured | reproduced | none "
                 "| unchanged |",
        ))
        defects = self.collect()
        content = defect_report.render(defects, datetime.date(2026, 9, 20))
        headings = re.findall(r"^## (\w+)$", content, re.MULTILINE)
        self.assertEqual("algorithm", headings[0])

    def test_rows_are_ordered_by_first_fortran_line_inside_a_kind(self) -> None:
        self.write("a/BOOT.md", NODE_BOOT.format(
            name="a",
            rows="| 500 | dead | second in Fortran order | not measured | reproduced | none "
                 "| unchanged |\n"
                 "| 50 | dead | first in Fortran order | not measured | reproduced | none "
                 "| unchanged |",
        ))
        defects = [defect for defect in self.collect() if defect.node == "a"]
        content = defect_report.render(defects, datetime.date(2026, 9, 20))
        self.assertLess(content.index("first in Fortran order"), content.index("second in Fortran order"))

    # -------------------------------------------------------------- the driver

    def test_check_fails_when_the_tree_diverges_from_the_generated_page(self) -> None:
        output = self.root / "docs" / "ORIGINAL-DEFECTS.md"
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output)]))
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output), "--check"]))

        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("leaves X unassigned", "leaves X and Y unassigned"),
            encoding="utf-8",
        )
        with contextlib.redirect_stdout(io.StringIO()) as captured:
            self.assertEqual(1, defect_report.main([str(self.root), "--output", str(output), "--check"]))
        self.assertIn("leaves X and Y unassigned", captured.getvalue())

    def test_check_fails_when_the_declared_json_is_stale_but_the_page_is_not(self) -> None:
        """Editing only 'Differing cells' leaves render()'s page byte-identical (that
        column is not one of its five displayed ones) but must still fail --check:
        the two generated files are checked independently (BOOT.md, "Invariants")."""
        output = self.root / "docs" / "ORIGINAL-DEFECTS.md"
        declared = self.root / "docs" / "declared-differences.json"
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output)]))
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output), "--check"]))

        boot = self.root / "a" / "BOOT.md"
        boot.write_text(
            boot.read_text(encoding="utf-8").replace("| none |", "| `*: X` |"),
            encoding="utf-8",
        )
        page_before = output.read_text(encoding="utf-8")
        with contextlib.redirect_stdout(io.StringIO()) as captured:
            self.assertEqual(1, defect_report.main([str(self.root), "--output", str(output), "--check"]))
        self.assertIn(str(declared.name), captured.getvalue())
        self.assertEqual(page_before, output.read_text(encoding="utf-8"), "--check must write nothing")

    def test_check_ignores_only_the_generation_date(self) -> None:
        output = self.root / "docs" / "ORIGINAL-DEFECTS.md"
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output)]))

        stale_date = output.read_text(encoding="utf-8").replace(
            datetime.date.today().isoformat(), "2020-01-01",
        )
        output.write_bytes(stale_date.encode("utf-8"))
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output), "--check"]))

    def test_check_reads_a_crlf_page_as_stale(self) -> None:
        """Red once: the old writer's output, write_text on Windows, is the same page
        with CRLF, and --check must fail it, naming the CR bytes."""
        output = self.root / "docs" / "ORIGINAL-DEFECTS.md"
        declared = self.root / "docs" / "declared-differences.json"
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, defect_report.main([str(self.root), "--output", str(output)]))
        output.write_bytes(output.read_bytes().replace(b"\n", b"\r\n"))
        with contextlib.redirect_stdout(io.StringIO()), \
                contextlib.redirect_stderr(io.StringIO()) as captured:
            self.assertEqual(1, defect_report.main([str(self.root), "--output", str(output), "--check"]))
        self.assertIn("CR bytes", captured.getvalue())

        output.write_bytes(output.read_bytes().replace(b"\r\n", b"\n"))
        declared.write_bytes(declared.read_bytes().replace(b"\n", b"\r\n"))
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(1, defect_report.main([str(self.root), "--output", str(output), "--check"]))

    def test_the_writer_emits_no_carriage_return_on_any_platform(self) -> None:
        """Right once: a regenerated page and twin hold LF only, and regenerating over
        CRLF files repairs them so that --check is green again."""
        output = self.root / "docs" / "ORIGINAL-DEFECTS.md"
        declared = self.root / "docs" / "declared-differences.json"
        arguments = [str(self.root), "--output", str(output)]
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(0, defect_report.main(arguments))
            output.write_bytes(output.read_bytes().replace(b"\n", b"\r\n"))
            declared.write_bytes(declared.read_bytes().replace(b"\n", b"\r\n"))
            self.assertEqual(0, defect_report.main(arguments))
            self.assertEqual(0, defect_report.main(arguments + ["--check"]))
        for path in (output, declared):
            content = path.read_bytes()
            self.assertIn(b"\n", content)
            self.assertNotIn(b"\r", content, path.name)

    def test_check_without_a_page_yet_is_an_error_not_a_write(self) -> None:
        output = self.root / "docs" / "ORIGINAL-DEFECTS.md"
        with contextlib.redirect_stderr(io.StringIO()) as captured:
            self.assertEqual(1, defect_report.main([str(self.root), "--output", str(output), "--check"]))
        self.assertFalse(output.exists())
        self.assertIn("does not exist", captured.getvalue())

    def test_a_non_directory_root_is_a_usage_error(self) -> None:
        with contextlib.redirect_stderr(io.StringIO()):
            self.assertEqual(2, defect_report.main([str(self.root / "missing")]))


class RealTreeTest(unittest.TestCase):
    """Acceptance criteria that only mean something on the real tree."""

    ROOT = Path(__file__).resolve().parents[2]

    def test_row_count_equals_the_sum_of_rows_in_the_nodes(self) -> None:
        """Counted here by a second, independent walk - not collect()'s own tally."""
        expected = 0
        for boot in self.ROOT.rglob(defect_report.BOOT_NAME):
            relative_parts = boot.relative_to(self.ROOT).parts[:-1]
            if any(part in defect_report.EXCLUDED_DIRS or part.startswith(".") for part in relative_parts):
                continue
            text = defect_report.read(boot)
            body = defect_report.find_section(text.split("\n"))
            if body is None:
                continue
            non_blank = [line for _, line in body if line.strip()]
            expected += max(0, len(non_blank) - 2)  # header and separator are not rows

        defects = defect_report.collect(str(self.ROOT))
        self.assertEqual(expected, len(defects))
        self.assertGreater(len(defects), 0, "the real tree should carry at least one declared defect")

    def test_the_first_section_is_algorithm_with_a_placed_or_not_measured_consequence(self) -> None:
        defects = defect_report.collect(str(self.ROOT))
        content = defect_report.render(defects, datetime.date(2026, 9, 20))
        headings = re.findall(r"^## (\w+)$", content, re.MULTILINE)
        self.assertEqual("algorithm", headings[0])
        for defect in defects:
            if defect.kind == "algorithm":
                self.assertTrue(
                    defect.consequence == defect_report.NOT_MEASURED
                    or defect_report.PLACE.search(defect.consequence),
                    "algorithm row from {} has no place and is not 'not measured': {}".format(
                        defect.node, defect.consequence),
                )

    def test_the_summary_line_of_the_real_page_counts_every_row(self) -> None:
        defects = defect_report.collect(str(self.ROOT))
        content = defect_report.render(defects, datetime.date(2026, 9, 20))
        counted = re.search(
            r"^By `Corrected`: (\d+) unchanged, (\d+) not reachable, (\d+) fixed\.$",
            content, re.MULTILINE)
        self.assertIsNotNone(counted)
        self.assertEqual(len(defects), sum(int(number) for number in counted.groups()))

    def test_rendering_the_real_tree_is_idempotent(self) -> None:
        defects = defect_report.collect(str(self.ROOT))
        date = datetime.date(2026, 9, 20)
        self.assertEqual(defect_report.render(defects, date), defect_report.render(defects, date))

    def test_the_committed_page_is_up_to_date(self) -> None:
        """--check with no path overrides checks both committed pages: the rendered
        docs/ORIGINAL-DEFECTS.md and its JSON twin, docs/declared-differences.json."""
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            code = defect_report.main([str(self.ROOT), "--check"])
        self.assertEqual(0, code, output.getvalue())

    def assert_rows_are_those_of_the_defects_with_entries(self, document: dict, defects: list) -> None:
        """The committed twin's rows are exactly the defects that fill in `Differing
        cells`, as the tool's own parser reads them from the nodes; no typed count."""
        with_entries = [defect for defect in defects if defect.differing_cells]
        self.assertEqual({"generator", "rows"}, set(document.keys()))
        self.assertEqual(
            sorted((defect.node, defect.fortran) for defect in with_entries),
            sorted((row["node"], row["fortran"]) for row in document["rows"]),
        )
        for row in document["rows"]:
            self.assertEqual({"node", "fortran", "kind", "port", "entries"}, set(row.keys()))
            self.assertNotEqual("reproduced", row["port"])
            for entry in row["entries"]:
                self.assertEqual(
                    {"formulations", "quantity", "first", "last", "originalBelow"},
                    set(entry.keys()),
                )

    def test_the_committed_declared_differences_hold_exactly_the_rows_with_entries(self) -> None:
        defects = defect_report.collect(str(self.ROOT))
        committed = (self.ROOT / "docs" / "declared-differences.json").read_text(encoding="utf-8")
        self.assert_rows_are_those_of_the_defects_with_entries(json.loads(committed), defects)
        self.assertGreater(len(json.loads(committed)["rows"]), 0)

    def test_a_declared_differences_file_with_an_extra_or_a_missing_row_is_rejected(self) -> None:
        """The red proof of the test above: the same check on a mutated copy."""
        defects = defect_report.collect(str(self.ROOT))
        committed = (self.ROOT / "docs" / "declared-differences.json").read_text(encoding="utf-8")

        extra = json.loads(committed)
        extra["rows"].append(dict(extra["rows"][0], fortran="0-0"))
        with self.assertRaises(AssertionError):
            self.assert_rows_are_those_of_the_defects_with_entries(extra, defects)

        missing = json.loads(committed)
        del missing["rows"][0]
        with self.assertRaises(AssertionError):
            self.assert_rows_are_those_of_the_defects_with_entries(missing, defects)


if __name__ == "__main__":
    unittest.main()
