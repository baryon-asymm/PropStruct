"""Finds every Fortran name that receives a self-referencing REAL*4 sum assignment
inside this node's own `## Line map` ranges, and writes the result to
`RealFourAccumulators.generated.txt` (this node's BOOT.md, "Accumulators", "Design
decision, 2026-09-24: the membership is generated too, not only the kind").

This script owns two things a hand-typed list cannot (AGENTS.md §6, a criterion
quantified by "all"): the *lookup* -- every `real`/`real*8`/`integer`/`integer*8`/
`character` declaration statement of the Fortran declaration block, lines 4-64, parsed
into a name -> kind table, continuation lines joined first -- and the *discovery* of
every assignment of the shape `X = X +/- ...` (array element included, same index text
on both sides) inside the four Fortran ranges this node's BOOT.md `## Line map` section
names, read from that section's own scope-declaration sentence (the same one
`tests/Particle.Tests/LineMapCoverageTests.cs` reads, not a second, independently-typed
copy of the four numbers). A name is a row of the generated file exactly when its
target is REAL*4 by that lookup (a bare `real` or no declaration at all -- Fortran's
implicit-typing default for a name outside the `I`-`N` range, the source having no
`IMPLICIT` statement anywhere, checked once by `verify_no_implicit_statement`); `real*8`
excludes.

What is deliberately *not* generated, and is not this script's job: whether a found
name is actually reproduced in `Attempt.cs` (with `AddReal4`) or declared "not ported".
That correspondence -- structural, not a criterion quantified by "all", since it does
not change when the source's declarations change -- is the hand-written
`RealFourWriteSites.txt` beside this file, read only by
`tests/Particle.Tests/RealFourAccumulatorMappingTests.cs`. Before 2026-09-24 this
script instead started from a hand-typed `FIELD_FORTRAN_NAMES` dict of
`AccumulatorLayout` field names; that dict could not hold a scalar local (nothing to
key it by), so it silently missed six REAL*4 sums entirely: `VSMKM1`, `SVD1`,
`Vdok_loc`, `Vmkm_loc`, `Vdok_loc2`, `Vmkm_loc2` (lines 642-666). Scanning the source
itself, rather than a curated list of places to look, is what finds them -- and, along
with them, three self-referencing REAL*4 locals that are *not* accumulators in the
sense this node cares about (`AUS`, `TU`: REAL*4 decision variables that gate a branch,
already declared in BOOT.md's "Decisions where the port departs from a transcription";
`VMKM` inside subroutine `VM`: a per-call bridge-volume local, not summed across
attempts, newly declared the same section) -- each held out by `RealFourWriteSites.txt`
as "not ported", not by narrowing this script's scan.

Python 3.8+, standard library only.

    python classify-real4-accumulators.py generate   # (re)writes RealFourAccumulators.generated.txt
    python classify-real4-accumulators.py verify      # regenerates into a temp file, fails on any byte difference
"""

from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, Tuple

NODE_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(NODE_ROOT.parents[1] / "tools" / "legacy"))
from legacy import legacy_file  # noqa: E402  (the one reader of PROPSTRUCT_LEGACY_DIR, tools/legacy/API.md)

SOURCE_NAME = "PropStructv3.for.txt"
BOOT_PATH = NODE_ROOT / "BOOT.md"
OUTPUT_PATH = NODE_ROOT / "RealFourAccumulators.generated.txt"
SCRIPT_NAME = "classify-real4-accumulators.py"

# The Fortran declaration block this node's BOOT.md names as the specification for this
# classification's *lookup* half, 1-based inclusive. Fixed: it is the source's own
# declaration block, not the scanned range (below), and does not move with the Line map.
DECLARATION_FIRST_LINE = 4
DECLARATION_LAST_LINE = 64

TYPE_PATTERN = re.compile(
    r"^\s*(REAL\*8|REAL\*4|REAL|INTEGER\*8|INTEGER\*4|INTEGER|CHARACTER)\b\s*"
    r"(,\s*ALLOCATABLE\s*)?"
    r"(::)?\s*(.*)$",
    re.IGNORECASE,
)
NAME_TOKEN = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)")

# Fortran's implicit-typing default: I-N is INTEGER, everything else is REAL (single
# precision). Applies here only because the source declares no IMPLICIT statement
# anywhere (verified below, not assumed) -- BOOT.md's own audit finding.
IMPLICIT_INTEGER_FIRST_LETTERS = set("ijklmn")

# A self-referencing sum/difference assignment: "NAME = NAME <op> ..." or
# "NAME(idx) = NAME(idx) <op> ...", array element included, an optional leading
# Fortran statement label (columns 1-5) skipped first. `idx` on the two sides is
# compared as text (whitespace-insensitive) by the caller, not folded into the regex,
# so an accumulator recurring into a *different* cell (e.g. "FQKS(iks+1)=...+FQKS(iks)",
# a running-window recurrence, not an accumulator) is not mistaken for one.
ASSIGNMENT_PATTERN = re.compile(
    r"^\s*(?:\d+\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*(\([^()]*\))?\s*=\s*"
    r"\1\s*(\([^()]*\))?\s*[+\-]",
    re.IGNORECASE,
)


def read_source_lines() -> List[str]:
    return Path(legacy_file(SOURCE_NAME)).read_text(encoding="utf-8").splitlines()


def verify_no_implicit_statement() -> None:
    """This script's fallback rule (Fortran's own default) applies only where no
    `IMPLICIT` statement narrows it. Checked against the whole file, not assumed, every
    run -- the same fact this node's BOOT.md records as already checked once by hand."""
    whole_file = Path(legacy_file(SOURCE_NAME)).read_text(encoding="utf-8")
    if re.search(r"^\s*IMPLICIT\b", whole_file, re.IGNORECASE | re.MULTILINE):
        raise SystemExit(
            f"{SCRIPT_NAME}: an IMPLICIT statement now exists in {SOURCE_NAME} -- "
            "the implicit-typing fallback this script uses is no longer Fortran's plain "
            "default and must be reread by hand before this script is trusted again."
        )


def join_declaration_continuations(lines: List[str]) -> List[str]:
    """Classic fixed-form Fortran continuation for a *name-list* declaration: columns
    1-5 blank, column 6 (index 5) non-blank marks a continuation of the previous
    logical line, its own columns 1-6 dropped and a comma inserted (a declaration's
    name list needs the separator its own line break stood in for). Used only for the
    declaration block (lines 4-64); the Line map scan below uses a plain, comma-free
    joiner instead, since a statement's continuation is not a name list."""
    logical_lines: List[str] = []
    for raw in lines:
        is_continuation = (
            len(raw) > 5
            and raw[0:5].strip() == ""
            and raw[5] not in (" ", "\t")
        )
        if is_continuation and logical_lines:
            logical_lines[-1] += "," + raw[6:]
        else:
            logical_lines.append(raw)
    return logical_lines


def strip_name_list_noise(names_text: str) -> List[str]:
    """Splits a declaration statement's name list on top-level commas (not the commas
    inside a `(...)` array-dimension list) and reduces each entry to its bare
    identifier, stripping array dimensions (`(:)`, `(10)`), `=initializer` and
    `/value/` initializer forms alike."""
    entries: List[str] = []
    depth = 0
    current = ""
    for ch in names_text:
        if ch == "(":
            depth += 1
            current += ch
        elif ch == ")":
            depth -= 1
            current += ch
        elif ch == "," and depth == 0:
            entries.append(current)
            current = ""
        else:
            current += ch
    if current.strip():
        entries.append(current)

    names: List[str] = []
    for entry in entries:
        match = NAME_TOKEN.match(entry)
        if match:
            names.append(match.group(1).lower())
    return names


def parse_declared_kinds(source_lines: List[str]) -> Dict[str, str]:
    """Every name the declaration block (lines 4-64) declares explicitly, lower-cased,
    mapped to its own declared kind (`real*8`, `real*4`, `real`, `integer*8`,
    `integer*4`, `integer`, `character`)."""
    declared: Dict[str, str] = {}
    declaration_lines = source_lines[DECLARATION_FIRST_LINE - 1: DECLARATION_LAST_LINE]
    for logical_line in join_declaration_continuations(declaration_lines):
        match = TYPE_PATTERN.match(logical_line)
        if not match:
            continue
        kind = match.group(1).lower()
        rest = match.group(4)
        for name in strip_name_list_noise(rest):
            declared[name] = kind
    return declared


def kind_of(name: str, declared: Dict[str, str]) -> Tuple[str, bool]:
    """(kind, explicit) for one Fortran name: its own declared kind if the block names
    it, otherwise Fortran's implicit-typing default (REAL, or INTEGER for I-N)."""
    if name in declared:
        return declared[name], True
    implicit_kind = "integer" if name[0] in IMPLICIT_INTEGER_FIRST_LETTERS else "real"
    return implicit_kind, False


def rounds_under_original(kind: str) -> bool:
    """REAL*4 by declared kind (bare `real` or implicit) rounds; `real*8` and every
    `integer*` kind exclude (this node's BOOT.md, "Accumulators")."""
    return kind in ("real", "real*4")


def parse_line_map_ranges() -> List[Tuple[int, int]]:
    """The Fortran ranges this script scans, read from the single scope-declaration
    sentence at the top of this node's BOOT.md `## Line map` section -- the same
    sentence and the same en-dash `start-end` pattern
    `tests/Particle.Tests/LineMapCoverageTests.cs`'s own `ParseRequiredRanges` reads --
    not a second, independently-typed copy of the four numbers (AGENTS.md §8, "numbers
    repeating the length of a list")."""
    boot_text = BOOT_PATH.read_text(encoding="utf-8")
    heading = "\n## Line map\n"
    heading_index = boot_text.find(heading)
    if heading_index < 0:
        raise SystemExit(f"{SCRIPT_NAME}: {BOOT_PATH} has no '## Line map' section")

    paragraph_start = heading_index + len(heading)
    paragraph_end = boot_text.find("\n\n", paragraph_start)
    if paragraph_end < paragraph_start:
        raise SystemExit(f"{SCRIPT_NAME}: '## Line map' has no scope-declaration paragraph before its first blank line")

    paragraph = boot_text[paragraph_start:paragraph_end]
    ranges = [(int(a), int(b)) for a, b in re.findall(r"(\d+)–(\d+)", paragraph)]
    if not ranges:
        raise SystemExit(f"{SCRIPT_NAME}: the '## Line map' scope-declaration paragraph names no Fortran line range")
    return ranges


def is_comment_or_blank(raw: str) -> bool:
    """A line is a comment or blank exactly as
    `tests/Particle.Tests/LineMapCoverageTests.cs`'s own `IsExecutable` reads one: blank,
    or its first character (fixed-form comment column) is `c`/`C` (the source has no
    `*`-comment lines, checked once against the whole file when that test was written)."""
    if raw.strip() == "":
        return True
    return raw[0] in ("c", "C")


def is_statement_continuation(raw: str) -> bool:
    """The same fixed-form continuation marker `join_declaration_continuations` uses:
    columns 1-5 blank, column 6 non-blank."""
    return len(raw) > 5 and raw[0:5].strip() == "" and raw[5] not in (" ", "\t")


def build_logical_statements(source_lines: List[str], ranges: List[Tuple[int, int]]) -> List[Tuple[int, str]]:
    """Every executable logical statement of the given 1-based inclusive ranges, as
    `(first_physical_line, joined_text)`, comment and blank lines dropped, a
    continuation line's own columns 1-6 appended directly (no comma: a statement is not
    a name list) to the last *code* logical line -- so a comment sitting between a
    statement and its continuation (legal Fortran, and not present in this source's own
    ranges today) does not break the join."""
    statements: List[Tuple[int, str]] = []
    for start, end in ranges:
        for line_no in range(start, end + 1):
            raw = source_lines[line_no - 1]
            if is_comment_or_blank(raw):
                continue
            if is_statement_continuation(raw) and statements:
                prev_no, prev_text = statements[-1]
                statements[-1] = (prev_no, prev_text + raw[6:].rstrip())
            else:
                statements.append((line_no, raw.rstrip()))
    return statements


def find_self_referencing_assignments(statements: List[Tuple[int, str]]) -> Dict[str, List[int]]:
    """Every Fortran name assigned to by a statement of the shape `X = X +/- ...`
    (array element included, the same index text on both sides), lower-cased, mapped to
    the sorted, de-duplicated list of first-physical-line numbers where it occurs."""
    occurrences: Dict[str, List[int]] = {}
    for line_no, text in statements:
        match = ASSIGNMENT_PATTERN.match(text)
        if not match:
            continue
        lhs_index = (match.group(2) or "").replace(" ", "").replace("\t", "").lower()
        rhs_index = (match.group(3) or "").replace(" ", "").replace("\t", "").lower()
        if lhs_index != rhs_index:
            continue  # a recurrence into a different cell, not a self-accumulation
        name = match.group(1).lower()
        occurrences.setdefault(name, [])
        if line_no not in occurrences[name]:
            occurrences[name].append(line_no)
    for lines in occurrences.values():
        lines.sort()
    return occurrences


def classify() -> List[Tuple[str, List[int], str]]:
    """One row per REAL*4-target self-referencing assignment found in the Line map's
    own ranges, sorted by Fortran name: the name, its first-physical-line occurrences,
    and its declared kind (asserted, not assumed -- a name assigned to from more than
    one occurrence always shares its own single declared kind, since a Fortran name has
    exactly one kind everywhere it is used)."""
    verify_no_implicit_statement()
    source_lines = read_source_lines()
    declared = parse_declared_kinds(source_lines)
    ranges = parse_line_map_ranges()
    statements = build_logical_statements(source_lines, ranges)
    occurrences = find_self_referencing_assignments(statements)

    rows: List[Tuple[str, List[int], str]] = []
    for name in sorted(occurrences):
        kind, explicit = kind_of(name, declared)
        if not rounds_under_original(kind):
            continue
        rows.append((name, occurrences[name], f"{kind} ({'explicit' if explicit else 'implicit'})"))
    return rows


def render(rows: List[Tuple[str, List[int], str]]) -> str:
    lines = [
        "# Generated by classify-real4-accumulators.py by scanning this node's own Fortran",
        "# Line map ranges (BOOT.md, '## Line map') for every self-referencing sum/difference",
        f"# assignment (\"X = X +/- ...\", array element included) whose target is REAL*4 by the",
        f"# declaration block ({SOURCE_NAME}, lines {DECLARATION_FIRST_LINE}-{DECLARATION_LAST_LINE}) plus",
        "# Fortran's implicit-typing rule. Do not edit by hand: `python",
        "# classify-real4-accumulators.py verify` regenerates this file and fails if it differs",
        "# (src/Particle/BOOT.md, \"Accumulators\"). Whether a name here is reproduced in",
        "# Attempt.cs (with AddReal4) or declared \"not ported\" is RealFourWriteSites.txt's own",
        "# hand-written correspondence, not this file's.",
        "#",
        "# fortran name | fortran lines (first physical line of each occurrence) | declared kind",
    ]
    for name, occurrence_lines, kind in rows:
        lines.append(f"{name} | {','.join(str(n) for n in occurrence_lines)} | {kind}")
    return "\n".join(lines) + "\n"


def cmd_generate() -> None:
    text = render(classify())
    OUTPUT_PATH.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {OUTPUT_PATH}")


def cmd_verify() -> None:
    fresh = render(classify())
    if not OUTPUT_PATH.is_file():
        raise SystemExit(f"verify: {OUTPUT_PATH} is not committed")
    committed = OUTPUT_PATH.read_text(encoding="utf-8")
    if fresh != committed:
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".txt", prefix="propstruct_real4_classification_", delete=False, encoding="utf-8"
        ) as handle:
            handle.write(fresh)
            tmp_path = handle.name
        raise SystemExit(
            f"verify: {OUTPUT_PATH} does not reproduce byte for byte on regeneration (see {tmp_path})"
        )
    row_count = sum(1 for line in committed.splitlines() if line and not line.startswith("#"))
    print(f"verify: {OUTPUT_PATH.name} reproduces byte for byte ({row_count} rows)")


def main(argv: List[str]) -> int:
    if len(argv) != 1 or argv[0] not in ("generate", "verify"):
        print(f"usage: {SCRIPT_NAME} generate|verify", file=sys.stderr)
        return 2
    if argv[0] == "generate":
        cmd_generate()
    else:
        cmd_verify()
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
