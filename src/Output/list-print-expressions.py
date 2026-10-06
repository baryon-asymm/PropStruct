"""Lists every print-time expression of the Fortran print block (BOOT.md, "## Print-time
expressions") into `PrintExpressions.generated.txt`.

A **row** is one site of an executable statement of the scope whose value reaches the
output file and which is an expression, not a bare name. The sites, by role:

  - `item`: a top-level item of a `write(4,...)` output list (DVF's `write(4,*),list`
    form too); a string item is the context of the items after it;
  - `array`, `length`: arguments 2 and 3 of `call arrayprint` / `call arrayprint2`
    (argument 1 is the label, the context);
  - `condition`: the condition of an `IF` or `ELSE IF`;
  - `assignment`: the right-hand side of an assignment;
  - `bound`: a bound of a `DO`; `argument`: an argument of any other `CALL`. Neither
    qualifies in today's source; they stay in the rule so that an edit cannot slip past.

A site is an expression when it holds `**`, `*`, `/`, `+`, `-`, a relational or logical
operator, or calls `int`, `real`, `sum`, `abs`, `sqrt`, `float`, `dble`, `nint`, `mod`,
`max`, `min`, `exp` or `log`. Not rows, named in the generated header: a `print`
statement, a `write` to a unit other than 4, and a site holding a quote, `//`, `trim` or
`len_trim` (a character site). Any other statement fails the script.

What this script does not decide: which C# member carries each row, or that a row is not
ported. That is the hand-written map's (`PrintExpressionSites.txt`).

The scope (first and last Fortran line) is read from the first paragraph of the BOOT.md
section, never typed here a second time. Python 3.8+, standard library only.

    python list-print-expressions.py generate   # (re)writes PrintExpressions.generated.txt
    python list-print-expressions.py verify     # regenerates in memory, fails on any byte difference
    python list-print-expressions.py selftest   # the rule on a constructed fragment and on pinned real lines
"""

from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, NamedTuple, Optional, Tuple

NODE_ROOT = Path(__file__).resolve().parent
BOOT_PATH = NODE_ROOT / "BOOT.md"
sys.path.insert(0, str(NODE_ROOT.parents[1] / "tools" / "legacy"))
from legacy import legacy_file  # noqa: E402  (the one reader of PROPSTRUCT_LEGACY_DIR, tools/legacy/API.md)

SOURCE_NAME = "PropStructv3.for.txt"
SOURCE_RELATIVE = "tests/Fixtures/Legacy/" + SOURCE_NAME  # the name `## Print-time expressions` cites
OUTPUT_PATH = NODE_ROOT / "PrintExpressions.generated.txt"
SCRIPT_NAME = "list-print-expressions.py"
SECTION_HEADING = "## Print-time expressions"

NUMERIC_INTRINSICS = {
    "int", "real", "sum", "abs", "sqrt", "float", "dble", "nint", "mod", "max", "min", "exp", "log",
}
OPERATOR = re.compile(r"\*\*|[-+*/]|\.(eq|ne|gt|ge|lt|le|and|or|not)\.|==|/=|<=|>=|<|>", re.I)
CHARACTER = re.compile(r"'|\"|//|\btrim\s*\(|\blen_trim\s*\(", re.I)
STRING_ITEM = re.compile(r"^(?:'[^']*'|\"[^\"]*\")$")
CITATION_SEPARATOR = " | "


class Statement(NamedTuple):
    line: int
    physical: List[int]
    text: str


class Row(NamedTuple):
    line: int
    role: str
    expression: str
    context: str


class Exclusion(NamedTuple):
    line: int
    reason: str
    text: str


def read_scope() -> Tuple[int, int]:
    """The first and last Fortran line, from the first paragraph of the BOOT.md section."""
    text = BOOT_PATH.read_text(encoding="utf-8")
    section = re.search(r"^" + re.escape(SECTION_HEADING) + r"[ \t]*\n\s*\n(.*?)(?:\n[ \t]*\n|\Z)", text, re.S | re.M)
    if section is None:
        raise SystemExit(f"{BOOT_PATH}: no section {SECTION_HEADING!r} with a first paragraph")
    paragraph = " ".join(section.group(1).split())
    scope = re.match(r"Scope: Fortran lines (\d+)–(\d+) of `([^`]+)`", paragraph)
    if scope is None:
        raise SystemExit(f"{BOOT_PATH}: the first paragraph of {SECTION_HEADING!r} does not start "
                         "'Scope: Fortran lines <first>–<last> of `<source>`'")
    if scope.group(3) != SOURCE_RELATIVE:
        raise SystemExit(f"{BOOT_PATH}: the scope names {scope.group(3)!r}, this script reads {SOURCE_RELATIVE!r}")
    return int(scope.group(1)), int(scope.group(2))


def is_comment(raw: str) -> bool:
    return len(raw) > 0 and raw[0] in "cC*"


def tab_index(raw: str) -> int:
    """The column of the tab that opens DVF's tab form (blanks or a label before it), else -1."""
    for index, char in enumerate(raw[:6]):
        if char == "\t":
            return index
        if char != " " and not char.isdigit():
            return -1
    return -1


def is_continuation(raw: str) -> bool:
    """Fixed form: columns 1-5 blank and column 6 neither blank nor 0. Tab form: a tab after
    blanks or a label, followed by a digit 1-9."""
    if is_comment(raw) or not raw.strip():
        return False
    tab = tab_index(raw)
    if tab >= 0:
        return raw[:tab].strip() == "" and raw[tab + 1:tab + 2] in tuple("123456789")
    return len(raw) > 5 and raw[0:5].strip() == "" and raw[5] not in (" ", "0")


def continuation_text(raw: str) -> str:
    tab = tab_index(raw)
    return raw[tab + 2:] if tab >= 0 else raw[6:]


def statement_body(raw: str) -> str:
    tab = tab_index(raw)
    if tab >= 0:
        return raw[tab + 1:]
    return raw[5:] if raw[:5].strip().isdigit() else raw


def statements(lines: List[str], first: int, last: int) -> List[Statement]:
    """The executable statements of physical lines first..last (1-based), continuations joined,
    each keyed by its first physical line."""
    out: List[Statement] = []
    for number in range(first, last + 1):
        raw = lines[number - 1]
        if is_comment(raw) or not raw.strip():
            continue
        if is_continuation(raw) and out:
            previous = out[-1]
            out[-1] = Statement(previous.line, previous.physical + [number],
                                previous.text + continuation_text(raw).rstrip())
            continue
        out.append(Statement(number, [number], statement_body(raw).rstrip()))
    return out


def split_top(text: str, separator: str = ",") -> List[str]:
    """Splits at a separator outside parentheses and string literals."""
    parts: List[str] = []
    depth = 0
    quote = ""
    current = ""
    for char in text:
        if quote:
            current += char
            if char == quote:
                quote = ""
            continue
        if char in "'\"":
            quote = char
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        if char == separator and depth == 0:
            parts.append(current)
            current = ""
        else:
            current += char
    parts.append(current)
    return parts


def close_paren(text: str, start: int) -> int:
    depth = 0
    quote = ""
    for index in range(start, len(text)):
        char = text[index]
        if quote:
            if char == quote:
                quote = ""
            continue
        if char in "'\"":
            quote = char
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
            if depth == 0:
                return index
    raise ValueError(f"unbalanced parentheses in {text!r}")


def is_character_site(site: str) -> bool:
    return CHARACTER.search(site) is not None


def is_print_time_expression(site: str) -> bool:
    """An operator or a numeric intrinsic call, after the quotes are out of the way."""
    stripped = site.strip()
    if OPERATOR.search(stripped):
        return True
    return any(m.group(1).lower() in NUMERIC_INTRINSICS for m in re.finditer(r"([A-Za-z_]\w*)\s*\(", stripped))


def normalise(site: str) -> str:
    return re.sub(r"\s+", "", site).lower()


class Collector:
    """The rows and exclusions of the statements fed to it, in source order."""

    def __init__(self) -> None:
        self.rows: List[Row] = []
        self.exclusions: List[Exclusion] = []

    def take(self, line: int, role: str, site: str, context: str) -> None:
        if not site.strip():
            return
        if is_character_site(site):
            self.exclusions.append(Exclusion(line, "character site", site.strip()))
        elif is_print_time_expression(site):
            self.rows.append(Row(line, role, normalise(site), context or "-"))

    def feed(self, statement: Statement) -> None:
        for part in split_top(statement.text, ";"):
            if part.strip():
                self.handle(statement.line, part.strip())

    def handle(self, line: int, text: str) -> None:
        lowered = text.lower().strip()
        if re.match(r"^format\s*\(", lowered):
            return
        conditional = re.match(r"^(else\s*)?if\s*\(", lowered)
        if conditional:
            opening = text.lower().index("(", conditional.start())
            closing = close_paren(text, opening)
            self.take(line, "condition", text[opening + 1:closing], "if")
            rest = text[closing + 1:].strip()
            if rest and rest.lower() != "then":
                self.handle(line, rest)
            return
        if re.match(r"^print\b", lowered):
            self.exclusions.append(Exclusion(line, "print statement (console)", text.strip()))
            return
        if re.match(r"^write\s*\(", lowered):
            self.handle_write(line, text)
            return
        call = re.match(r"^call\s+(\w+)\s*\((.*)\)\s*$", text.strip(), re.I)
        if call:
            self.handle_call(line, call.group(1).lower(), split_top(call.group(2)))
            return
        loop = re.match(r"^do\s+(\w+)\s*=\s*(.+)$", lowered)
        if loop:
            for bound in split_top(loop.group(2)):
                self.take(line, "bound", bound, "do")
            return
        if re.match(r"^(end\s*do|end\s*if|else|exit|continue|enddo|endif)$", lowered):
            return
        assignment = re.match(r"^([A-Za-z_]\w*)\s*(\([^=]*\))?\s*=(?!=)(.*)$", text.strip())
        if assignment:
            self.take(line, "assignment", assignment.group(3), assignment.group(1).lower() + " =")
            return
        raise SystemExit(f"{line}: unrecognised statement {text!r}")

    def handle_write(self, line: int, text: str) -> None:
        opening = text.index("(")
        closing = close_paren(text, opening)
        unit = split_top(text[opening + 1:closing])[0].strip()
        rest = text[closing + 1:].strip()
        if rest.startswith(","):
            rest = rest[1:]
        if unit != "4":
            self.exclusions.append(Exclusion(line, f"write to unit {unit}", text.strip()))
            return
        label = ""
        for item in split_top(rest):
            stripped = item.strip()
            if STRING_ITEM.match(stripped):
                label = stripped
                continue
            self.take(line, "item", stripped, label)

    def handle_call(self, line: int, name: str, arguments: List[str]) -> None:
        if name in ("arrayprint", "arrayprint2"):
            context = f"{name} {arguments[0].strip()}"
            self.take(line, "array", arguments[1], context)
            self.take(line, "length", arguments[2], context)
        else:
            for argument in arguments:
                self.take(line, "argument", argument, f"call {name}")


def collect(lines: List[str], first: int, last: int) -> Collector:
    collector = Collector()
    for statement in statements(lines, first, last):
        collector.feed(statement)
    seen: Dict[Tuple[int, str, str], int] = {}
    for row in collector.rows:
        key = (row.line, row.role, row.expression)
        seen[key] = seen.get(key, 0) + 1
    repeated = sorted(key for key, count in seen.items() if count > 1)
    if repeated:
        raise SystemExit(f"repeated (line, role, expression): {repeated}")
    return collector


def source_lines() -> List[str]:
    return Path(legacy_file(SOURCE_NAME)).read_text(encoding="utf-8").splitlines()


def render(collector: Collector, first: int, last: int) -> str:
    out = [
        f"# Generated by {SCRIPT_NAME} from the Fortran source ({SOURCE_RELATIVE}),",
        f"# lines {first}-{last}, the range read from src/Output/BOOT.md, \"{SECTION_HEADING}\".",
        f"# Do not edit by hand: `python {SCRIPT_NAME} verify` regenerates this file and fails if it",
        "# differs. The rule is that section's; the map of rows to code is PrintExpressionSites.txt.",
        "#",
        "# line | role | expression (lower case, blanks removed) | context",
        "# roles: item (write(4,...) list item; context: the string item before it), array and length",
        "# (arguments 2 and 3 of arrayprint/arrayprint2; context: call and label), condition (IF, ELSE",
        "# IF), assignment (right side; context: its target), bound (DO bound), argument (CALL argument).",
        "#",
        "# Not rows, by the rule:",
    ]
    for exclusion in collector.exclusions:
        out.append(f"#   {exclusion.line}: {exclusion.reason}: {exclusion.text}")
    out += [
        "#",
        "# Outside the range: the bodies of arrayprint and arrayprint2 (1517-1571) only format the",
        "# values they are handed; tests/Output.Tests' FortranFormatTests covers them.",
    ]
    for row in collector.rows:
        fields = [str(row.line), row.role, row.expression, row.context]
        for field in fields:
            if CITATION_SEPARATOR in field:
                raise SystemExit(f"{row.line}: the field {field!r} holds the column separator")
        out.append(CITATION_SEPARATOR.join(fields))
    return "\n".join(out) + "\n"


def generated_text() -> Tuple[str, Collector]:
    first, last = read_scope()
    collector = collect(source_lines(), first, last)
    return render(collector, first, last), collector


def cmd_generate() -> None:
    text, _ = generated_text()
    OUTPUT_PATH.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {OUTPUT_PATH}")


def cmd_verify() -> None:
    fresh, collector = generated_text()
    if not OUTPUT_PATH.is_file():
        raise SystemExit(f"verify: {OUTPUT_PATH} is not committed")
    if OUTPUT_PATH.read_bytes() != fresh.encode("utf-8"):
        with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", prefix="propstruct_print_expressions_",
                                         delete=False, encoding="utf-8", newline="\n") as handle:
            handle.write(fresh)
        raise SystemExit(f"verify: {OUTPUT_PATH} does not reproduce byte for byte (see {handle.name})")
    counts: Dict[str, int] = {}
    for row in collector.rows:
        counts[row.role] = counts.get(row.role, 0) + 1
    print(f"verify: {OUTPUT_PATH.name} reproduces byte for byte ({len(collector.rows)} rows: "
          + ", ".join(f"{role} {count}" for role, count in sorted(counts.items()))
          + f"; {len(collector.exclusions)} excluded)")


CONSTRUCTED_FRAGMENT = [
    "      write(4,*)'a =',x*2.0,y",
    "      write(*,*)'console',x*2",
    "      write(name,'(a)')'n',i+1",
    "      call arrayprint('arr',v/(d*1e6),n+2)",
    "      if (k.gt.1) then",
    "      z = a+b",
    "      write(4,*)'s',trim(f)//'.x'",
    "      write(4,*)'t',",
    "     &  p**2",
    "      end if",
]

CONSTRUCTED_ROWS = [
    Row(1, "item", "x*2.0", "'a ='"),
    Row(4, "array", "v/(d*1e6)", "arrayprint 'arr'"),
    Row(4, "length", "n+2", "arrayprint 'arr'"),
    Row(5, "condition", "k.gt.1", "if"),
    Row(6, "assignment", "a+b", "z ="),
    Row(8, "item", "p**2", "'t'"),
]

CONSTRUCTED_EXCLUSIONS = [
    (2, "write to unit *"),
    (3, "write to unit name"),
    (7, "character site"),
]

# Pinned real statements: (first physical line, physical lines), each opening in a different form.
PINNED_STATEMENTS = [
    (1243, [1243, 1244]),  # fixed-form continuation, its tab after the continuation mark
    (1300, [1300, 1301]),  # fixed-form continuation of a tab-form initial line
    (1396, [1396]),        # a blank, a tab, then `call`: a tab-form initial line
]


def cmd_selftest() -> None:
    failures: List[str] = []
    fragment = collect(CONSTRUCTED_FRAGMENT, 1, len(CONSTRUCTED_FRAGMENT))
    if fragment.rows != CONSTRUCTED_ROWS:
        failures.append(f"constructed fragment rows: expected {CONSTRUCTED_ROWS}, got {fragment.rows}")
    if [(e.line, e.reason) for e in fragment.exclusions] != CONSTRUCTED_EXCLUSIONS:
        failures.append("constructed fragment exclusions: expected "
                        f"{CONSTRUCTED_EXCLUSIONS}, got {[(e.line, e.reason) for e in fragment.exclusions]}")
    first, last = read_scope()
    real = {statement.line: statement.physical for statement in statements(source_lines(), first, last)}
    for key, physical in PINNED_STATEMENTS:
        if real.get(key) != physical:
            failures.append(f"real statement {key}: expected physical lines {physical}, got {real.get(key)}")
    if failures:
        raise SystemExit("selftest failed:\n  " + "\n  ".join(failures))
    print(f"selftest: the constructed fragment gives {len(fragment.rows)} rows and "
          f"{len(fragment.exclusions)} exclusions; {len(PINNED_STATEMENTS)} pinned real statements join as expected")


def main(argv: List[str]) -> int:
    commands = {"generate": cmd_generate, "verify": cmd_verify, "selftest": cmd_selftest}
    command: Optional[str] = argv[0] if len(argv) == 1 else None
    if command not in commands:
        print(f"usage: {SCRIPT_NAME} generate|verify|selftest", file=sys.stderr)
        return 2
    commands[command]()
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
