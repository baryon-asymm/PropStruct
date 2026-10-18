"""The search that closes the `ExactConstants` criterion (`ACCEPTANCE.md`, "Corrected,
`ExactConstants`"; BOOT.md, "## Corrected model"): the literals of the original that stand for pi
or one third, found by pattern in the four ranges of the line map and nowhere else.

`verify` reads the source through `fortran_source.read_all_lines` (the one reader of
`PROPSTRUCT_LEGACY_DIR`), applies the four patterns to every non-comment physical line of the
ranges the sentence at the top of BOOT.md's `## Line map` names, and requires the matches to be
exactly the five sites BOOT.md's `ExactConstants` entry lists: one at 1704, one at 1716, one at
1014 and two at 1016. A site that is gone, a site that is new and a pattern that matches nowhere
are each a finding. A finding names line numbers and pattern numbers, never a line of the source
(`tools/legacy`, the quotation rule).

`selftest` runs the same function on synthetic text and needs no original: it is red on a removed
site, on an extra one, on a comment, and on the quotient written after a letter, a digit and a
point.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path
from typing import List, Sequence, Tuple

import fortran_source as fs

NODE_ROOT = Path(__file__).resolve().parent

# The patterns of the criterion, in its order: the truncated pi, the truncated full solid angle,
# the truncated third, and the quotient 1/3. (not after a letter, a digit or a point).
PATTERNS: Tuple[str, ...] = (
    r"3\.14\d*",
    r"12\.566\d*",
    r"0?\.333\d*",
    r"(?<![A-Za-z0-9.])1/3\.",
)

# (line, pattern number counted from 1): BOOT.md, "## Corrected model", `ExactConstants`.
EXPECTED: Tuple[Tuple[int, int], ...] = (
    (1014, 1),
    (1016, 1),
    (1016, 3),
    (1704, 1),
    (1716, 1),
)


def ranges_of_the_line_map(boot_text: str) -> List[Tuple[int, int]]:
    """The `start-end` pairs (en dash) of the one paragraph after the `## Line map` heading, in order."""
    heading = "\n## Line map\n"
    start = boot_text.index(heading) + len(heading)
    end = boot_text.index("\n\n", start)
    return [(int(a), int(b)) for a, b in re.findall(r"(\d+)–(\d+)", boot_text[start:end])]


def sites(lines: Sequence[str], ranges: Sequence[Tuple[int, int]]) -> List[Tuple[int, int]]:
    """Every (line, pattern number) matched on a non-comment physical line inside a range, sorted."""
    found: List[Tuple[int, int]] = []
    for first, last in ranges:
        for number in range(first, min(last, len(lines)) + 1):
            raw = lines[number - 1]
            if fs.is_comment(raw):
                continue
            code = raw.split("!", 1)[0]
            for index, pattern in enumerate(PATTERNS, 1):
                found.extend((number, index) for _ in re.finditer(pattern, code))
    return sorted(found)


def findings(lines: Sequence[str], ranges: Sequence[Tuple[int, int]]) -> List[str]:
    found = sites(lines, ranges)
    expected = sorted(EXPECTED)
    result = [f"missing: line {line}, pattern {pattern}" for line, pattern in expected if (line, pattern) not in found]
    result += [f"new: line {line}, pattern {pattern}" for line, pattern in found if (line, pattern) not in expected]
    return result


def verify() -> None:
    boot_text = (NODE_ROOT / "BOOT.md").read_text(encoding="utf-8")
    ranges = ranges_of_the_line_map(boot_text)
    if len(ranges) != 4:
        raise SystemExit(f"verify: the line map's scope names {len(ranges)} ranges, expected 4")
    lines = fs.read_all_lines()
    problems = findings(lines, ranges)
    if problems:
        raise SystemExit("verify: " + "; ".join(problems))
    print(f"verify: {len(EXPECTED)} sites at lines {sorted({line for line, _ in EXPECTED})}, no other match in "
          f"{', '.join(f'{a}-{b}' for a, b in ranges)}")


def _synthetic(extra: Sequence[Tuple[int, str]] = (), removed: Sequence[int] = ()) -> List[str]:
    """1800 blank lines with the five sites of EXPECTED, written as synthetic statements."""
    lines = ["      CONTINUE"] * 1800
    base = {
        1014: "      v = 3.14159 / 6",
        1016: "      w = 0.75 / 3.14159 * (q) ** 0.3333",
        1704: "      z = (3 / 3.14) * q",
        1716: "      z = (24 / 3.14) * q",
    }
    for number, text in base.items():
        if number not in removed:
            lines[number - 1] = text
    for number, text in extra:
        lines[number - 1] = text
    return lines


def selftest() -> None:
    ranges = [(267, 277), (376, 406), (771, 1175), (1695, 1757)]
    cases = [
        ("the five sites alone", _synthetic(), 0),
        ("a removed site", _synthetic(removed=(1704,)), 1),
        ("both sites of 1016 removed", _synthetic(removed=(1016,)), 2),
        ("a new solid-angle literal", _synthetic(extra=[(800, "      x = 12.56636 * y")]), 1),
        ("a new third as an exponent", _synthetic(extra=[(900, "      x = y ** .3333")]), 1),
        ("a new quotient", _synthetic(extra=[(380, "      x = y * 1/3. * z")]), 1),
        ("a quotient after a letter", _synthetic(extra=[(380, "      x = A1/3. + z")]), 0),
        ("a quotient after a digit", _synthetic(extra=[(380, "      x = 21/3. + z")]), 0),
        ("a quotient after a point", _synthetic(extra=[(380, "      x = 1.1/3. + z")]), 0),
        ("a match in a comment line", _synthetic(extra=[(380, "C     3.14159 12.566 .3333")]), 0),
        ("a match outside the ranges", _synthetic(extra=[(1500, "      x = 3.14159 * y")]), 0),
    ]
    failures = []
    for name, lines, expected_count in cases:
        count = len(findings(lines, ranges))
        print(f"selftest: {name}: {count} findings")
        if count != expected_count:
            failures.append(f"{name}: {count} findings, expected {expected_count}")
    if failures:
        raise SystemExit("selftest: " + "; ".join(failures))


if __name__ == "__main__":
    if sys.argv[1:] == ["verify"]:
        verify()
    elif sys.argv[1:] == ["selftest"]:
        selftest()
    else:
        print("usage: exact_constants_sites.py verify | selftest", file=sys.stderr)
        raise SystemExit(2)
