#!/usr/bin/env python3
"""defect_report - assembles docs/ORIGINAL-DEFECTS.md from the nodes' own tables.

It answers one question for a reader of the port: what is wrong with the legacy
Fortran program PropStructV3, and what does each defect change. Every claim in the
generated page already lives in the '## Defects of the original' section of the node
that transcribes the corresponding Fortran lines (root BOOT.md, "The defect report is
assembled, never written"); this script only collects, orders and formats those rows.
It authors no claim of its own and never reads the Fortran source.

The table's sixth column, 'Differing cells', names the printed cells in which
reference mode under the `Original` precision kind differs from the original because
of that row, as `CompareSets` (tests/Harness) finds them. This script parses that
column's grammar and assembles it into a second, machine-readable page,
docs/declared-differences.json, so that `CompareSets`'s own tests can read it without
parsing Markdown.

Python 3.8+, standard library only, no configuration file:

    python defect_report.py <tree-root> [--output docs/ORIGINAL-DEFECTS.md]
        [--declared-output docs/declared-differences.json] [--check]

Exit code 0 when both pages are written, or are already up to date under --check; 1 on
a malformed table, a missing input, or a stale page under --check; 2 on a usage error.
"""

from __future__ import annotations

import argparse
import datetime
import difflib
import json
import re
import sys
from pathlib import Path
from typing import List, NamedTuple, Optional, Sequence, Set, Tuple

BOOT_NAME = "BOOT.md"
SECTION_TITLE = "Defects of the original"

# Directories that are never nodes: build output, VCS internals and the document
# templates under docs/protocol/templates/, which are examples, not real nodes
# (CLAUDE.md excludes them from protocol_lint the same way).
EXCLUDED_DIRS = {
    ".git", "bin", "obj", "node_modules", "templates", "TestResults", "artifacts",
    ".vs", ".idea", "__pycache__",
}

HEADING = re.compile(r"^ {0,3}(#{1,6})\s+(.*?)\s*#*\s*$")
HEADER_CELLS = (
    "Fortran", "Kind", "What the original does", "Consequence", "The port",
    "Differing cells",
)
SEPARATOR_CELL = re.compile(r"^:?-{2,}:?$")

# The vocabulary root BOOT.md fixes for the two enum-shaped columns.
KINDS: Tuple[str, ...] = ("algorithm", "statistics", "numeric", "dead", "cosmetic")
KIND_RANK = {kind: rank for rank, kind in enumerate(KINDS)}
PORTS: Tuple[str, ...] = ("reproduced", "declared, not reproduced", "open")

NOT_MEASURED = "not measured"
# A "measurement with its place": at least one backtick-quoted test, fixture, file or
# document name. This is the one thing a script can check about "is this a real
# consequence, not a guess" - it cannot verify the number is right, only that a place
# claiming to hold it is named.
PLACE = re.compile(r"`[^`]+`")
LINE_NUMBER = re.compile(r"\d+")

# The 'Differing cells' grammar (root BOOT.md, "The defect report is assembled";
# tools/defect-report/API.md, "Differing cells"): 'none', or one backtick span of
# entries separated by '; ', each 'scope: quantity[first..last] where original <
# threshold', the range and the where-clause both optional. Parsed in this order: the
# scope is the text before the first ": "; a trailing " where original < x" is
# stripped; a trailing "[...]" is stripped; the remainder is the quantity.
DIFFERING_CELLS_NONE = "none"
WHERE_SUFFIX = re.compile(r"^(?P<rest>.*) where original < (?P<threshold>.+)$")
SPAN = re.compile(r"^(?P<rest>.*)\[(?P<body>[^\[\]]*)\]$")
SIMPLE_RANGE = re.compile(r"^(\d+)$")
PAIR_RANGE = re.compile(r"^(\d+)\.\.(\d+)$")

# The line the generated page carries its date on; stripped before a --check diff so
# that a page generated yesterday is not "stale" for that reason alone.
GENERATED_ON = re.compile(r"Generated on \d{4}-\d{2}-\d{2}\.")


class DeclaredCell(NamedTuple):
    formulations: Tuple[str, ...]     # ("*",) or the formulation names, as written
    quantity: str                     # the Harness name, literal
    first: Optional[int]              # 0-based inclusive, or None
    last: Optional[int]               # 0-based inclusive, or None
    original_below: Optional[float]   # the 'where original < x' threshold, or None


class Defect(NamedTuple):
    node: str            # path of the node relative to the tree root
    source_line: int     # line of the row in that node's BOOT.md (1-based)
    fortran: str         # line numbers as written in the row
    kind: str            # algorithm | statistics | numeric | dead | cosmetic
    what: str
    consequence: str
    port: str            # reproduced | declared, not reproduced | open
    differing_cells: List[DeclaredCell]   # parsed 'Differing cells'; [] for 'none'


class DefectFormatError(Exception):
    """A node's table does not match the contract; the run fails, naming the place."""

    def __init__(self, path: str, line: int, message: str) -> None:
        self.path = path
        self.line = line
        self.message = message
        super().__init__("{}:{}: {}".format(path, line, message))


# --------------------------------------------------------------------------- reading


def read(path: Path) -> str:
    """The file as text, BOM and line endings normalised away."""
    return path.read_text(encoding="utf-8-sig", errors="replace").replace("\r\n", "\n")


def relative(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def _excluded(directory_name: str) -> bool:
    return directory_name.startswith(".") or directory_name in EXCLUDED_DIRS


def find_boot_files(root: Path) -> List[Path]:
    """Every BOOT.md under root, outside the excluded directories, sorted."""
    found: List[Path] = []
    stack = [root]
    while stack:
        current = stack.pop()
        try:
            entries = sorted(current.iterdir())
        except OSError:
            continue
        for entry in entries:
            if entry.is_dir():
                if not _excluded(entry.name):
                    stack.append(entry)
            elif entry.name == BOOT_NAME:
                found.append(entry)
    return sorted(found)


# --------------------------------------------------------------- the section's table


def _split_row(line: str) -> Optional[List[str]]:
    """The unescaped cells of one pipe-table row, or None if the line is not one."""
    stripped = line.strip()
    if len(stripped) < 2 or not stripped.startswith("|") or not stripped.endswith("|"):
        return None
    body = stripped[1:-1]
    cells: List[str] = []
    current: List[str] = []
    escaped = False
    for character in body:
        if escaped:
            current.append(character)
            escaped = False
        elif character == "\\":
            escaped = True
        elif character == "|":
            cells.append("".join(current).strip())
            current = []
        else:
            current.append(character)
    cells.append("".join(current).strip())
    return cells


def _is_separator_row(cells: Sequence[str]) -> bool:
    return len(cells) > 0 and all(SEPARATOR_CELL.match(cell) for cell in cells)


def find_section(lines: Sequence[str]) -> Optional[List[Tuple[int, str]]]:
    """The '## Defects of the original' section's body as (0-based line, text) pairs.

    None means the node carries no such section at all - a node with nothing to
    declare gets no section (tools/defect-report/BOOT.md), which is not an error.
    An empty list means the heading exists but the body does not hold a table, which
    the caller turns into a DefectFormatError.
    """
    found = False
    inside = False
    body: List[Tuple[int, str]] = []
    for index, line in enumerate(lines):
        heading = HEADING.match(line)
        if heading:
            level, title = len(heading.group(1)), heading.group(2)
            if inside and level <= 2:
                inside = False
            if level == 2 and title == SECTION_TITLE:
                inside = True
                found = True
            continue
        if inside:
            body.append((index, line))
    return body if found else None


def parse_table(body: List[Tuple[int, str]], path_label: str) -> List[Tuple[int, List[str]]]:
    """The table's data rows as (0-based source line, cells), the header consumed.

    Anything in the section that is not the one table - a blank run aside - is a
    format error: the section holds one table and nothing else (BOOT.md 12).
    """
    non_blank = [(line_number, text) for line_number, text in body if text.strip()]
    if len(non_blank) < 2:
        raise DefectFormatError(path_label, (body[0][0] + 1) if body else 1,
                                 "the '## {}' section has no table".format(SECTION_TITLE))

    header_line, header_text = non_blank[0]
    separator_line, separator_text = non_blank[1]
    header_cells = _split_row(header_text)
    separator_cells = _split_row(separator_text)
    if header_cells is None or separator_cells is None or not _is_separator_row(separator_cells):
        raise DefectFormatError(path_label, header_line + 1,
                                 "the '## {}' section has no table".format(SECTION_TITLE))
    if tuple(header_cells) != HEADER_CELLS:
        raise DefectFormatError(
            path_label, header_line + 1,
            "the table's header must be exactly {}, found {}".format(list(HEADER_CELLS), header_cells),
        )

    rows: List[Tuple[int, List[str]]] = []
    for line_number, text in non_blank[2:]:
        cells = _split_row(text)
        if cells is None:
            raise DefectFormatError(
                path_label, line_number + 1,
                "a line inside '## {}' is not a table row and nothing else belongs "
                "in this section: {!r}".format(SECTION_TITLE, text.strip()),
            )
        rows.append((line_number, cells))

    if not rows:
        raise DefectFormatError(path_label, header_line + 1,
                                 "the table under '## {}' has no rows".format(SECTION_TITLE))
    return rows


def parse_declared_cells(path_label: str, source_line: int, raw: str) -> List[DeclaredCell]:
    """The entries of one 'Differing cells' cell (API.md, "Differing cells").

    'none' parses to the empty list. Any other value must be a single backtick span
    holding entries separated by '; '; each entry is 'scope: quantity', with an
    optional trailing '[first..last]' and an optional trailing
    'where original < threshold', parsed in that order (root BOOT.md's grammar).
    """
    text = raw.strip()
    if text == DIFFERING_CELLS_NONE:
        return []
    if len(text) < 2 or not text.startswith("`") or not text.endswith("`") or "`" in text[1:-1]:
        raise DefectFormatError(
            path_label, source_line + 1,
            "'Differing cells' must be {!r} or a single backtick-quoted span, "
            "found {!r}".format(DIFFERING_CELLS_NONE, text),
        )

    cells: List[DeclaredCell] = []
    seen: Set[Tuple[Tuple[str, ...], str, Optional[int], Optional[int], Optional[float]]] = set()
    for raw_entry in text[1:-1].split("; "):
        entry = raw_entry.strip()
        if not entry or ": " not in entry:
            raise DefectFormatError(
                path_label, source_line + 1,
                "the entry {!r} is not 'scope: quantity', with an optional [range] "
                "and an optional 'where original < threshold'".format(entry),
            )
        scope_text, remainder = entry.split(": ", 1)
        formulations = tuple(part.strip() for part in scope_text.split(","))
        if not scope_text.strip() or any(not part for part in formulations):
            raise DefectFormatError(
                path_label, source_line + 1,
                "the scope in {!r} is empty or holds an empty formulation".format(entry),
            )

        threshold: Optional[float] = None
        where_split = WHERE_SUFFIX.match(remainder)
        if where_split:
            remainder = where_split.group("rest")
            threshold_text = where_split.group("threshold")
            try:
                threshold = float(threshold_text)
            except ValueError:
                raise DefectFormatError(
                    path_label, source_line + 1,
                    "the threshold {!r} in {!r} is not a number".format(threshold_text, entry),
                )

        first: Optional[int] = None
        last: Optional[int] = None
        if "[" in remainder or "]" in remainder:
            span_split = SPAN.match(remainder)
            if span_split is None:
                raise DefectFormatError(
                    path_label, source_line + 1,
                    "the span in {!r} is malformed".format(entry),
                )
            remainder = span_split.group("rest")
            body = span_split.group("body")
            simple = SIMPLE_RANGE.match(body)
            pair = PAIR_RANGE.match(body)
            if simple:
                first = last = int(simple.group(1))
            elif pair:
                first, last = int(pair.group(1)), int(pair.group(2))
                if first > last:
                    raise DefectFormatError(
                        path_label, source_line + 1,
                        "the span [{}] in {!r} has first > last".format(body, entry),
                    )
            else:
                raise DefectFormatError(
                    path_label, source_line + 1,
                    "the span [{}] in {!r} is malformed".format(body, entry),
                )

        quantity = remainder.strip()
        if not quantity:
            raise DefectFormatError(
                path_label, source_line + 1, "the entry {!r} has no quantity".format(entry),
            )

        cell = DeclaredCell(formulations, quantity, first, last, threshold)
        key = (formulations, quantity, first, last, threshold)
        if key in seen:
            raise DefectFormatError(
                path_label, source_line + 1,
                "the entry {!r} duplicates an earlier entry in the same cell".format(entry),
            )
        seen.add(key)
        cells.append(cell)
    return cells


def build_defect(node: str, path_label: str, source_line: int, cells: Sequence[str]) -> Defect:
    if len(cells) != len(HEADER_CELLS):
        raise DefectFormatError(
            path_label, source_line + 1,
            "a row of '## {}' has {} cells, expected {} ({})".format(
                SECTION_TITLE, len(cells), len(HEADER_CELLS), ", ".join(HEADER_CELLS)),
        )

    fortran, kind, what, consequence, port, differing = (cell.strip() for cell in cells)

    if not fortran:
        raise DefectFormatError(path_label, source_line + 1, "Fortran is empty")
    if not what:
        raise DefectFormatError(path_label, source_line + 1, "'What the original does' is empty")
    if kind not in KINDS:
        raise DefectFormatError(
            path_label, source_line + 1,
            "unknown Kind {!r}; expected one of {}".format(kind, ", ".join(KINDS)),
        )
    if port not in PORTS:
        raise DefectFormatError(
            path_label, source_line + 1,
            "unknown value {!r} in 'The port'; expected one of {}".format(port, ", ".join(PORTS)),
        )
    if consequence != NOT_MEASURED and not PLACE.search(consequence):
        raise DefectFormatError(
            path_label, source_line + 1,
            "Consequence {!r} is neither a measurement with its place (a `backtick`-quoted "
            "test, fixture or document) nor exactly {!r}".format(consequence, NOT_MEASURED),
        )
    if port == "reproduced" and differing != DIFFERING_CELLS_NONE:
        raise DefectFormatError(
            path_label, source_line + 1,
            "a 'reproduced' row's 'Differing cells' must be {!r}, found {!r}".format(
                DIFFERING_CELLS_NONE, differing),
        )

    differing_cells = parse_declared_cells(path_label, source_line, differing)
    return Defect(node, source_line + 1, fortran, kind, what, consequence, port, differing_cells)


# ------------------------------------------------------------------------- collect


def collect(tree_root: str) -> List[Defect]:
    """Every defect declared under the tree, node by node, in no particular order."""
    root = Path(tree_root).resolve()
    defects: List[Defect] = []
    for boot in find_boot_files(root):
        text = read(boot)
        body = find_section(text.split("\n"))
        if body is None:
            continue
        path_label = relative(boot, root)
        node = relative(boot.parent, root)
        for source_line, cells in parse_table(body, path_label):
            defects.append(build_defect(node, path_label, source_line, cells))
    return defects


# -------------------------------------------------------------------------- render


PREAMBLE = (
    "This page lists what is wrong with the legacy Fortran program `PropStructV3` and "
    "what each defect changes. It is **generated**: every row is a copy of a row of "
    "the `## Defects of the original` table of the node named in its `Node` column "
    "(root `BOOT.md`, \"The defect report is assembled, never written\"), and carries "
    "no claim of its own. `Kind` orders the sections below: `algorithm` first, because "
    "those are the defects that change the model's answer, then `statistics`, "
    "`numeric`, `dead` and `cosmetic`. `The port` says whether the port reproduces the "
    "defect, declares it without reproducing it, or leaves the question open; a "
    "defect silently fixed does not exist (root `BOOT.md`, Taboos)."
)


def _first_fortran_line(fortran: str) -> float:
    numbers = [int(match) for match in LINE_NUMBER.findall(fortran)]
    return float(min(numbers)) if numbers else float("inf")


def _page_order(defect: Defect) -> Tuple[int, float, str, str]:
    """The one ordering both generated pages share: kind, then Fortran line, then node."""
    return (KIND_RANK[defect.kind], _first_fortran_line(defect.fortran), defect.node, defect.fortran)


def render(defects: List[Defect], generated_on: "datetime.date") -> str:
    """The page's Markdown text; deterministic for the same defects and date."""
    ordered = sorted(defects, key=_page_order)
    nodes = sorted({defect.node for defect in defects})

    lines: List[str] = [
        "<!-- GENERATED FILE. Regenerate with",
        "     `python tools/defect-report/defect_report.py . --output docs/ORIGINAL-DEFECTS.md`;",
        "     a hand edit here is overwritten the next time this runs.",
        "     Generated on {}. -->".format(generated_on.isoformat()),
        "",
        "# Defects of the original",
        "",
        PREAMBLE,
        "",
        "{} defects from {} nodes.".format(len(defects), len(nodes)),
        "",
    ]

    by_kind: dict = {}
    for defect in ordered:
        by_kind.setdefault(defect.kind, []).append(defect)

    for kind in KINDS:
        rows = by_kind.get(kind, [])
        if not rows:
            continue
        lines.append("## {}".format(kind))
        lines.append("")
        lines.append("| Node | Fortran | What the original does | Consequence | The port |")
        lines.append("|---|---|---|---|---|")
        for defect in rows:
            lines.append("| `{}` | {} | {} | {} | {} |".format(
                defect.node, defect.fortran, defect.what, defect.consequence, defect.port,
            ))
        lines.append("")

    return "\n".join(lines).rstrip("\n") + "\n"


DECLARED_DIFFERENCES_GENERATOR = (
    "python tools/defect-report/defect_report.py . "
    "--declared-output docs/declared-differences.json"
)


def declared_differences(defects: List[Defect]) -> str:
    """The JSON twin's text: only the rows that declare an entry, in page order.

    No date and no source line (API.md, "The JSON twin"): a BOOT.md edit elsewhere -
    wording, an unrelated ⚠ - must not make this file stale.
    """
    ordered = sorted((defect for defect in defects if defect.differing_cells), key=_page_order)
    document = {
        "generator": DECLARED_DIFFERENCES_GENERATOR,
        "rows": [
            {
                "node": defect.node,
                "fortran": defect.fortran,
                "kind": defect.kind,
                "port": defect.port,
                "entries": [
                    {
                        "formulations": list(cell.formulations),
                        "quantity": cell.quantity,
                        "first": cell.first,
                        "last": cell.last,
                        "originalBelow": cell.original_below,
                    }
                    for cell in defect.differing_cells
                ],
            }
            for defect in ordered
        ],
    }
    return json.dumps(document, indent=2, ensure_ascii=False) + "\n"


def _normalized(text: str) -> str:
    """The page's content with its own date line blanked out, for a --check diff."""
    return GENERATED_ON.sub("Generated on DATE.", text)


# ---------------------------------------------------------------------------- entry


def _resolve(root: Path, argument: str) -> Path:
    path = Path(argument)
    return path if path.is_absolute() else (root.resolve() / path)


def _check_one(path: Path, label: str, content: str, normalize) -> bool:
    """True when path holds content (after normalize); prints a diff and returns False
    otherwise, including when path does not exist yet."""
    if not path.is_file():
        print("defect_report: {} does not exist; run without --check to create it".format(
            label), file=sys.stderr)
        return False
    existing = read(path)
    if normalize(existing) != normalize(content):
        print("defect_report: {} is stale; regenerate it".format(label), file=sys.stderr)
        diff = difflib.unified_diff(
            normalize(existing).splitlines(keepends=True),
            normalize(content).splitlines(keepends=True),
            fromfile="tree/" + label,
            tofile="generated/" + label,
        )
        sys.stdout.writelines(diff)
        return False
    return True


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        prog="defect_report",
        description="Assemble docs/ORIGINAL-DEFECTS.md and docs/declared-differences.json "
                    "from every node's own '## Defects of the original' table.",
    )
    parser.add_argument("root", help="the tree root - the directory holding AGENTS.md")
    parser.add_argument("--output", default="docs/ORIGINAL-DEFECTS.md",
                        help="where the page goes, relative to root unless absolute")
    parser.add_argument("--declared-output", default="docs/declared-differences.json",
                        help="where the 'Differing cells' JSON twin goes, relative to "
                             "root unless absolute")
    parser.add_argument("--check", action="store_true",
                        help="write nothing; fail if either page in the tree is stale")
    arguments = parser.parse_args(argv)

    root = Path(arguments.root)
    if not root.is_dir():
        print("defect_report: {} is not a directory".format(root), file=sys.stderr)
        return 2

    try:
        defects = collect(str(root))
    except DefectFormatError as error:
        print("defect_report: {}".format(error), file=sys.stderr)
        return 1

    output_path = _resolve(root, arguments.output)
    declared_path = _resolve(root, arguments.declared_output)
    output_label = relative(output_path, root.resolve())
    declared_label = relative(declared_path, root.resolve())
    nodes = sorted({defect.node for defect in defects})
    declared_rows = sum(1 for defect in defects if defect.differing_cells)
    content = render(defects, datetime.date.today())
    declared_content = declared_differences(defects)

    if arguments.check:
        page_ok = _check_one(output_path, output_label, content, _normalized)
        declared_ok = _check_one(declared_path, declared_label, declared_content, lambda text: text)
        if not (page_ok and declared_ok):
            return 1
        print("defect_report: {} defects from {} nodes -> {} (up to date); "
              "{} declared rows -> {} (up to date)".format(
                  len(defects), len(nodes), output_label, declared_rows, declared_label))
        return 0

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(content, encoding="utf-8")
    declared_path.parent.mkdir(parents=True, exist_ok=True)
    declared_path.write_text(declared_content, encoding="utf-8")
    print("defect_report: {} defects from {} nodes -> {}; {} declared rows -> {}".format(
        len(defects), len(nodes), output_label, declared_rows, declared_label))
    return 0


if __name__ == "__main__":
    sys.exit(main())
