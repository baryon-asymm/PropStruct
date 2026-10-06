"""The one reader of the Fortran source that this node's classifiers share
(`classify-setup-plane.py`, `classify-cycle-plane-listing.py`; BOOT.md, "## Setup plane").

What it holds, each once:

  - the source's physical lines, the no-`IMPLICIT` check and the comment rule;
  - **statements** assembled from physical lines in DVF's fixed form *and* tab form. A
    tab in columns 1-6, preceded by blanks or a label only, opens tab form; such a line
    continues the previous one only when the tab is followed by a digit 1-9. Before
    2026-10-01 each script tested column 6 alone, which read the tab-form lines 873,
    1739 and 1740 (a tab and spaces, five tabs) as continuations of the line above;
  - the declaration tables (main program 4-64, `PARAM` 1696-1699), array names, and
    Fortran's implicit typing;
  - expression kinds: a tokenizer and a precedence parser that types every node
    (`r4`, `r8`, `i4`, `i8`, `log`), records integer promotions to REAL*4, inexact
    REAL*4 literals and all-literal REAL*4 subexpressions (folds), and every read with
    its subscript;
  - a control-flow graph over a line range, its **basic blocks** under the x87 storage
    model (BOOT.md, "## Cycle plane"), reaching definitions with kills and element
    identity, and **rule L** (a same-kind REAL*4 sum whose target does not vary with
    its innermost loop).

Python 3.8+, standard library only. `python fortran_source.py selftest` checks the
continuation rule on the lines that decided it.
"""

from __future__ import annotations

import re
import struct
import sys
from dataclasses import dataclass, field
from functools import lru_cache
from pathlib import Path
from typing import Dict, FrozenSet, List, Optional, Set, Tuple

NODE_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(NODE_ROOT.parents[1] / "tools" / "legacy"))
from legacy import legacy_file  # noqa: E402  (the one reader of PROPSTRUCT_LEGACY_DIR, tools/legacy/API.md)

SOURCE_NAME = "PropStructv3.for.txt"

MAIN_DECLARATIONS = (4, 64)
PARAM_DECLARATIONS = (1696, 1699)
MAIN_PROGRAM_END = 1515

IMPLICIT_INTEGER_FIRST_LETTERS = set("ijklmn")


# ---- the source ------------------------------------------------------------------

def source_path() -> Path:
    """The verified path of the source outside the repository; resolved at the call, so
    that an entry that reads no file runs without PROPSTRUCT_LEGACY_DIR."""
    return Path(legacy_file(SOURCE_NAME))


@lru_cache(maxsize=1)
def read_all_lines() -> Tuple[str, ...]:
    return tuple(source_path().read_text(encoding="utf-8").splitlines())


def verify_no_implicit_statement(script_name: str) -> None:
    """The implicit-typing fallback below is Fortran's own default only where no
    `IMPLICIT` statement narrows it; checked against the whole file every run."""
    whole_file = source_path().read_text(encoding="utf-8")
    if re.search(r"^\s*IMPLICIT\b", whole_file, re.IGNORECASE | re.MULTILINE):
        raise SystemExit(
            f"{script_name}: an IMPLICIT statement now exists in {SOURCE_NAME} -- "
            "the implicit-typing fallback is no longer Fortran's plain default and must be "
            "reread by hand before this script is trusted again."
        )


def is_comment(raw_line: str) -> bool:
    """`c`, `C` or `*` in column 1 exactly."""
    return len(raw_line) > 0 and raw_line[0] in ("c", "C", "*")


def _tab_form_index(raw: str) -> int:
    """Index of the tab that opens DVF tab form (columns 1-6, preceded by blanks or
    label digits only), or -1 for a fixed-form line."""
    for index, char in enumerate(raw[:6]):
        if char == "\t":
            return index
        if char != " " and not char.isdigit():
            return -1
    return -1


def is_continuation(raw: str) -> bool:
    """Tab form: a blank label field and a digit 1-9 right after the tab. Fixed form:
    columns 1-5 blank and column 6 neither blank nor `0`."""
    if is_comment(raw) or not raw.strip():
        return False
    tab = _tab_form_index(raw)
    if tab >= 0:
        return raw[:tab].strip() == "" and raw[tab + 1:tab + 2] in tuple("123456789")
    return len(raw) > 5 and raw[0:5].strip() == "" and raw[5] not in (" ", "0")


def continuation_text(raw: str) -> str:
    tab = _tab_form_index(raw)
    return raw[tab + 2:] if tab >= 0 else raw[6:]


def label_and_body(raw: str) -> Tuple[Optional[str], str]:
    tab = _tab_form_index(raw)
    if tab >= 0:
        label = raw[:tab].strip()
        return (label or None), raw[tab + 1:]
    label = raw[:5].strip()
    if label.isdigit():
        return label, raw[5:]
    return None, raw


def join_continuations(lines: List[str]) -> List[str]:
    """Logical lines for the declaration scan: a continuation is appended to the line
    before it after a comma, its own columns 1-6 (or tab and digit) dropped."""
    logical_lines: List[str] = []
    for raw in lines:
        if is_continuation(raw) and logical_lines:
            logical_lines[-1] += "," + continuation_text(raw)
        else:
            logical_lines.append(raw)
    return logical_lines


@dataclass(frozen=True)
class Statement:
    line: int                      # first physical line, 1-based
    physical: Tuple[int, ...]
    label: Optional[str]
    text: str                      # without label, stripped


def _split_top_level(text: str, separator: str) -> List[str]:
    parts: List[str] = []
    depth, quote, current = 0, "", ""
    for char in text:
        if quote:
            current += char
            if char == quote:
                quote = ""
            continue
        if char in ("'", '"'):
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


def statements(first: int, last: int) -> List[Statement]:
    """Every statement of physical lines first..last (1-based, inclusive): comments and
    blank lines skipped, continuations joined, semicolon-separated statements split
    (each keeps the first physical line and the label of its line, the label on the
    first part only)."""
    lines = read_all_lines()
    assembled: List[Tuple[int, List[int], Optional[str], str]] = []
    for number in range(first, last + 1):
        raw = lines[number - 1]
        if is_comment(raw) or not raw.strip():
            continue
        if is_continuation(raw) and assembled:
            line, physical, label, text = assembled[-1]
            assembled[-1] = (line, physical + [number], label, text + continuation_text(raw).rstrip())
            continue
        label, body = label_and_body(raw)
        assembled.append((number, [number], label, body.rstrip()))
    result: List[Statement] = []
    for line, physical, label, text in assembled:
        for index, part in enumerate(p for p in _split_top_level(text, ";") if p.strip()):
            result.append(Statement(line, tuple(physical), label if index == 0 else None, part.strip()))
    return result


# ---- declarations ----------------------------------------------------------------

TYPE_PATTERN = re.compile(
    r"^\s*(REAL\*8|REAL\*4|REAL|INTEGER\*8|INTEGER\*4|INTEGER|CHARACTER)\b\s*"
    r"(,\s*ALLOCATABLE\s*)?"
    r"(::)?\s*(.*)$",
    re.IGNORECASE,
)
NAME_TOKEN = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(\()?")


def _name_list_entries(names_text: str) -> List[str]:
    return [entry for entry in _split_top_level(names_text, ",") if entry.strip()]


def strip_name_list_noise(names_text: str) -> List[str]:
    """Bare identifiers of a top-level comma list, array dimensions, `=initializer` and
    `/value/` forms stripped."""
    names: List[str] = []
    for entry in _name_list_entries(names_text):
        match = NAME_TOKEN.match(entry)
        if match:
            names.append(match.group(1).lower())
    return names


def _declarations(first: int, last: int) -> List[Tuple[str, bool, str]]:
    """(kind, allocatable, names text) for every type declaration of the block."""
    lines = list(read_all_lines()[first - 1:last])
    found: List[Tuple[str, bool, str]] = []
    for logical_line in join_continuations(lines):
        match = TYPE_PATTERN.match(logical_line)
        if match:
            found.append((match.group(1).lower(), match.group(2) is not None, match.group(4)))
    return found


def parse_declared_kinds(first: int, last: int) -> Dict[str, str]:
    """Every name a declaration block explicitly declares, lower-cased, to its kind."""
    declared: Dict[str, str] = {}
    for kind, _, rest in _declarations(first, last):
        for name in strip_name_list_noise(rest):
            declared[name] = kind
    return declared


def parse_array_names(first: int, last: int) -> Set[str]:
    """Names the block declares as arrays: allocatable, or with a dimension list."""
    arrays: Set[str] = set()
    for _, allocatable, rest in _declarations(first, last):
        for entry in _name_list_entries(rest):
            match = NAME_TOKEN.match(entry)
            if match and (allocatable or match.group(2)):
                arrays.add(match.group(1).lower())
    return arrays


def kind_of(name: str, declared: Dict[str, str]) -> Tuple[str, bool]:
    """(kind, explicit): the declared kind, else REAL, or INTEGER for I-N."""
    if name in declared:
        return declared[name], True
    return ("integer" if name[0] in IMPLICIT_INTEGER_FIRST_LETTERS else "real"), False


KIND_CODES = {"real": "r4", "real*4": "r4", "real*8": "r8", "integer": "i4",
              "integer*4": "i4", "integer*8": "i8", "character": "ch"}


def kind_code(name: str, declared: Dict[str, str]) -> str:
    return KIND_CODES[kind_of(name, declared)[0]]


def binary32(value: float) -> float:
    return struct.unpack("<f", struct.pack("<f", value))[0]


# ---- expressions -----------------------------------------------------------------

NUMBER = r"\d+\.\d*(?:[eEdD][+-]?\d+)?|\.\d+(?:[eEdD][+-]?\d+)?|\d+[eEdD][+-]?\d+|\d+"
TOKEN = re.compile(
    r"\s*(?:(?P<num>" + NUMBER + r")|(?P<dotop>\.[A-Za-z]+\.)|(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
    r"|(?P<op>\*\*|==|/=|<=|>=|[-+*/(),<>=:]))"
)
DOT_OPERATORS = {"eq", "ne", "gt", "ge", "lt", "le", "and", "or", "not", "eqv", "neqv"}
SCALAR_INTRINSICS = {"real", "int", "abs", "sqrt", "mod", "max", "min", "dble", "float",
                     "nint", "exp", "log"}
ARRAY_INTRINSICS = {"sum", "maxval", "minval", "product", "count"}
BINARY = {".or.": 1, ".and.": 2, ".eq.": 4, ".ne.": 4, ".gt.": 4, ".ge.": 4, ".lt.": 4,
          ".le.": 4, "==": 4, "/=": 4, "<": 4, ">": 4, "<=": 4, ">=": 4, "+": 5, "-": 5,
          "*": 6, "/": 6, "**": 8}


def tokenize(text: str) -> List[Tuple[str, str]]:
    tokens: List[Tuple[str, str]] = []
    index = 0
    while index < len(text):
        if text[index].isspace():
            index += 1
            continue
        match = TOKEN.match(text, index)
        if not match:
            raise ValueError("cannot tokenize at " + repr(text[index:]))
        if match.group("num"):
            number = match.group("num")
            # `1.eq.` is the integer 1 and an operator, not the real `1.`
            dotted = re.match(r"(\d+)\.([A-Za-z]+)\.", text[match.start("num"):])
            if dotted and dotted.group(2).lower() in DOT_OPERATORS:
                tokens.append(("num", dotted.group(1)))
                index = match.start("num") + len(dotted.group(1))
                continue
            tokens.append(("num", number))
        elif match.group("dotop"):
            tokens.append(("op", match.group("dotop").lower()))
        elif match.group("name"):
            tokens.append(("name", match.group("name")))
        else:
            tokens.append(("op", match.group("op")))
        index = match.end()
    return tokens


@dataclass
class Node:
    kind: str                      # lit, name, elem, call, bin, neg, not, paren
    typ: str                       # r4, r8, i4, i8, log
    text: str
    kids: List["Node"] = field(default_factory=list)
    literal: bool = False
    value: Optional[float] = None
    operator: str = ""


@dataclass(frozen=True)
class Read:
    name: str
    subscript: Optional[str]       # None: a scalar, or a whole array
    subscript_names: FrozenSet[str] = frozenset()


@dataclass
class ExpressionSite:
    kind: str                      # promotion, literal, fold
    text: str
    detail: str


class Parser:
    """Precedence parser that types every node by Fortran's rules and records, as it
    goes, the reads and the promotion, literal and fold sites of one expression."""

    def __init__(self, text: str, declared: Dict[str, str], arrays: Set[str]):
        self.tokens = tokenize(text)
        self.position = 0
        self.declared = declared
        self.arrays = arrays
        self.reads: List[Read] = []
        self.promotions: List[ExpressionSite] = []
        self.array_intrinsic = False

    def parse(self) -> Node:
        node = self.expression()
        if self.position != len(self.tokens):
            raise ValueError("trailing tokens: " + repr(self.tokens[self.position:]))
        return node

    def peek(self) -> Tuple[Optional[str], Optional[str]]:
        return self.tokens[self.position] if self.position < len(self.tokens) else (None, None)

    def take(self) -> Tuple[str, str]:
        token = self.tokens[self.position]
        self.position += 1
        return token

    def expect(self, token: Tuple[str, str]) -> None:
        if self.take() != token:
            raise ValueError("expected " + repr(token))

    def expression(self, minimum: int = 0) -> Node:
        left = self.prefix()
        while True:
            kind, value = self.peek()
            if kind != "op" or value not in BINARY or BINARY[value] < minimum:
                return left
            self.take()
            right = self.expression(BINARY[value] if value == "**" else BINARY[value] + 1)
            left = self.binary(value, left, right)

    def arguments(self) -> List[Node]:
        self.expect(("op", "("))
        arguments: List[Node] = []
        if self.peek() != ("op", ")"):
            while True:
                arguments.append(self.expression())
                if self.peek() != ("op", ","):
                    break
                self.take()
        self.expect(("op", ")"))
        return arguments

    def prefix(self) -> Node:
        kind, value = self.take()
        if kind == "num":
            if re.search(r"[.eEdD]", value):
                number = float(value.replace("d", "e").replace("D", "e"))
                return Node("lit", "r8" if re.search(r"[dD]", value) else "r4", value,
                            literal=True, value=number)
            if abs(int(value)) > 2 ** 24:
                raise ValueError("integer literal not exact in binary32: " + value)
            return Node("lit", "i4", value, literal=True, value=int(value))
        if kind == "op" and value in ("-", "+"):
            operand = self.expression(7)
            return Node("neg", operand.typ, value + operand.text, [operand], literal=operand.literal)
        if kind == "op" and value == ".not.":
            operand = self.expression(3)
            return Node("not", "log", ".not." + operand.text, [operand])
        if kind == "op" and value == "(":
            inner = self.expression()
            self.expect(("op", ")"))
            return Node("paren", inner.typ, "(" + inner.text + ")", [inner], literal=inner.literal)
        if kind == "name":
            name = value.lower()
            if self.peek() == ("op", "("):
                arguments = self.arguments()
                text = name + "(" + ",".join(a.text for a in arguments) + ")"
                if name not in self.declared and name not in self.arrays and (
                        name in SCALAR_INTRINSICS or name in ARRAY_INTRINSICS):
                    return self.intrinsic(name, arguments, text)
                subscript = ",".join(a.text for a in arguments).lower()
                self.reads.append(Read(name, subscript, frozenset(_names_in(arguments))))
                return Node("elem", kind_code(name, self.declared), text, arguments)
            self.reads.append(Read(name, None))
            return Node("name", kind_code(name, self.declared), name)
        raise ValueError("unexpected token %s %s" % (kind, value))

    def intrinsic(self, name: str, arguments: List[Node], text: str) -> Node:
        if name in ARRAY_INTRINSICS:
            self.array_intrinsic = True
            return Node("call", arguments[0].typ, text, arguments)
        if name in ("real", "float"):
            if arguments[0].typ in ("i4", "i8") and not arguments[0].literal:
                self.promotions.append(ExpressionSite("promotion", arguments[0].text,
                                                      arguments[0].typ + " by real()"))
            return Node("call", "r4", text, arguments)
        if name in ("int", "nint"):
            return Node("call", "i4", text, arguments)
        if name == "dble":
            return Node("call", "r8", text, arguments)
        return Node("call", self.common(arguments), text, arguments)

    def common(self, nodes: List[Node]) -> str:
        types = [n.typ for n in nodes]
        if "r8" in types:
            return "r8"
        if "r4" in types:
            for node in nodes:
                if node.typ in ("i4", "i8") and not node.literal:
                    self.promotions.append(ExpressionSite("promotion", node.text, node.typ + " mixed-mode"))
            return "r4"
        return "i8" if "i8" in types else "i4"

    def binary(self, operator: str, left: Node, right: Node) -> Node:
        text = left.text + operator + right.text
        literal = left.literal and right.literal
        if operator in ("+", "-", "*", "/"):
            typ = self.common([left, right])
        elif operator == "**":
            typ = left.typ if right.typ in ("i4", "i8") else self.common([left, right])
        else:
            if operator not in (".and.", ".or."):
                self.common([left, right])
            typ, literal = "log", False
        return Node("bin", typ, text, [left, right], literal=literal, operator=operator)


def _names_in(nodes: List[Node]) -> Set[str]:
    names: Set[str] = set()
    for node in nodes:
        if node.kind in ("name", "elem"):
            names.add(node.text.split("(")[0].lower())
        names |= _names_in(node.kids)
    return names


def _has_operator(node: Node) -> bool:
    return node.kind == "bin" or any(_has_operator(k) for k in node.kids)


def _inexact_literals(node: Node) -> List[ExpressionSite]:
    if node.kind == "lit":
        if node.typ == "r4" and binary32(node.value) != node.value:
            return [ExpressionSite("literal", node.text, "binary32 " + repr(binary32(node.value)))]
        return []
    sites: List[ExpressionSite] = []
    for kid in node.kids:
        sites += _inexact_literals(kid)
    return sites


def literal_sites(node: Node) -> List[ExpressionSite]:
    """An all-literal REAL*4 subexpression with an operator is a fold (the compiler
    stores its binary32 value, computed from its literals' binary32 values, each listed
    after the fold); a lone REAL*4 literal is a site only when binary32 cannot hold it
    exactly."""
    if node.literal and _has_operator(node):
        if node.typ != "r4":
            return []
        return [ExpressionSite("fold", node.text, "binary32")] + _inexact_literals(node)
    if node.kind == "lit":
        return _inexact_literals(node)
    sites: List[ExpressionSite] = []
    for kid in node.kids:
        sites += literal_sites(kid)
    return sites


@dataclass
class Expression:
    node: Node
    reads: List[Read]
    sites: List[ExpressionSite]
    array_intrinsic: bool


def analyse(text: str, declared: Dict[str, str], arrays: Set[str]) -> Expression:
    parser = Parser(text, declared, arrays)
    node = parser.parse()
    return Expression(node, parser.reads, parser.promotions + literal_sites(node), parser.array_intrinsic)


def analyse_list(text: str, declared: Dict[str, str], arrays: Set[str]) -> List[Expression]:
    """A top-level comma list (a subscript, DO bounds, CALL arguments), each parsed."""
    return [analyse(part, declared, arrays) for part in _split_top_level(text, ",") if part.strip()]


# ---- statements as operations ----------------------------------------------------

ASSIGNMENT = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)\s*(\((?:[^()]|\([^()]*\))*\))?\s*=(?!=)(.*)$")
DO = re.compile(r"^do\s+(?:(\d+)\s*,?\s*)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+)$", re.IGNORECASE)
NON_EXECUTABLE = re.compile(
    r"^(subroutine|function|program|use|real|integer|character|logical|double|dimension"
    r"|common|data|parameter|allocate|deallocate|format|implicit)\b", re.IGNORECASE)


def split_if(text: str) -> Tuple[Optional[str], str]:
    """(condition, rest) of `if (condition) rest`, parentheses balanced; (None, text)
    when the statement is no IF."""
    stripped = text.strip()
    match = re.match(r"(?:else\s*)?if\s*\(", stripped, re.IGNORECASE)
    if not match:
        return None, stripped
    depth = 0
    for index in range(match.end() - 1, len(stripped)):
        if stripped[index] == "(":
            depth += 1
        elif stripped[index] == ")":
            depth -= 1
            if depth == 0:
                return stripped[match.end():index], stripped[index + 1:].strip()
    raise ValueError("unbalanced IF: " + repr(text))


@dataclass
class Op:
    index: int
    line: int
    label: Optional[str]
    kind: str                       # assign, do, enddo, if, elseif, else, endif, cond,
                                    # goto, exit, continue, io, call, return, end, other
    text: str
    target: Optional[str] = None
    subscript: Optional[str] = None
    subscript_names: FrozenSet[str] = frozenset()
    whole_array: bool = False
    rhs: Optional[Expression] = None
    reads: List[Read] = field(default_factory=list)
    sites: List[ExpressionSite] = field(default_factory=list)
    array_intrinsic: bool = False
    do_label: Optional[str] = None
    do_variable: Optional[str] = None
    goto: Optional[str] = None
    inner_of_logical_if: bool = False
    successors: List[int] = field(default_factory=list)
    block: int = -1


def _io_reads(text: str, declared: Dict[str, str], arrays: Set[str]) -> Expression:
    """Reads of a WRITE/PRINT output list: string literals dropped, each item parsed."""
    rest = re.sub(r"^(write|print|read)\s*", "", text, flags=re.IGNORECASE)
    if rest.startswith("("):
        depth = 0
        for index, char in enumerate(rest):
            depth += char == "("
            depth -= char == ")"
            if depth == 0:
                rest = rest[index + 1:]
                break
    else:
        rest = ",".join(_split_top_level(rest, ",")[1:])
    reads: List[Read] = []
    sites: List[ExpressionSite] = []
    for item in _split_top_level(rest, ","):
        item = re.sub(r"'[^']*'|\"[^\"]*\"", "", item).strip()
        if item:
            expression = analyse(item, declared, arrays)
            reads += expression.reads
            sites += expression.sites
    return Expression(Node("lit", "i4", ""), reads, sites, False)


class Program:
    """Operations of physical lines first..last with their control-flow graph, basic
    blocks and reaching definitions. `outside` names what is read after the range is
    left (the next cycle's particle loop, the print plane): the range's end and every
    jump out of it reach one pseudo-operation reading those names, which flows back to
    the range's first operation (the next cycle)."""

    def __init__(self, first: int, last: int, declared: Dict[str, str], arrays: Set[str],
                 outside: Optional[Dict[str, Set[str]]] = None):
        self.declared = declared
        self.arrays = arrays
        self.outside = outside or {}
        self.ops: List[Op] = []
        for statement in statements(first, last):
            self._add(statement.line, statement.label, statement.text)
        self.exit_index = len(self.ops)
        self._link()
        self._blocks()
        self.loops = self._loops()

    # -- construction --

    def _new(self, line: int, label: Optional[str], kind: str, text: str) -> Op:
        op = Op(len(self.ops), line, label, kind, text)
        self.ops.append(op)
        return op

    def _expression(self, op: Op, text: str) -> Expression:
        expression = analyse(text, self.declared, self.arrays)
        self._absorb(op, expression)
        return expression

    @staticmethod
    def _absorb(op: Op, expression: Expression) -> None:
        op.reads += expression.reads
        op.sites += expression.sites
        op.array_intrinsic = op.array_intrinsic or expression.array_intrinsic

    def _add(self, line: int, label: Optional[str], text: str, inner: bool = False) -> None:
        lower = text.lower()
        assignment = ASSIGNMENT.match(text)
        if assignment and not re.match(r"^do\s", lower):
            op = self._new(line, label, "assign", text)
            op.inner_of_logical_if = inner
            op.target = assignment.group(1).lower()
            if assignment.group(2):
                parts = analyse_list(assignment.group(2)[1:-1], self.declared, self.arrays)
                for part in parts:
                    self._absorb(op, part)
                op.subscript = ",".join(part.node.text for part in parts).lower()
                op.subscript_names = frozenset(_names_in([part.node for part in parts]))
            op.whole_array = op.subscript is None and op.target in self.arrays
            op.rhs = self._expression(op, assignment.group(3))
            return
        do = DO.match(text)
        if do:
            op = self._new(line, label, "do", text)
            op.do_label, op.do_variable = do.group(1), do.group(2).lower()
            for bound in analyse_list(do.group(3), self.declared, self.arrays):
                self._absorb(op, bound)
            return
        if re.match(r"^end\s*do$", lower):
            self._new(line, label, "enddo", text)
            return
        if re.match(r"^else\s*if\s*\(", lower):
            condition, _ = split_if(text)
            self._expression(self._new(line, label, "elseif", text), condition)
            return
        if re.match(r"^if\s*\(", lower):
            condition, rest = split_if(text)
            if rest.lower() == "then":
                self._expression(self._new(line, label, "if", text), condition)
                return
            self._expression(self._new(line, label, "cond", text), condition)
            self._add(line, None, rest, inner=True)
            return
        simple = [("else", r"^else$"), ("endif", r"^end\s*if$"), ("exit", r"^exit$"),
                  ("continue", r"^continue$"), ("return", r"^return$"),
                  ("end", r"^end(\s+subroutine.*)?$")]
        for kind, pattern in simple:
            if re.match(pattern, lower):
                self._new(line, label, kind, text).inner_of_logical_if = inner
                return
        goto = re.match(r"^go\s*to\s*(\d+)$", lower)
        if goto:
            op = self._new(line, label, "goto", text)
            op.goto, op.inner_of_logical_if = goto.group(1), inner
            return
        if re.match(r"^call\b", lower):
            op = self._new(line, label, "call", text)
            op.inner_of_logical_if = inner
            arguments = re.search(r"\((.*)\)\s*$", text)
            if arguments:
                for argument in analyse_list(arguments.group(1), self.declared, self.arrays):
                    self._absorb(op, argument)
            return
        if re.match(r"^(write|print|read)\b", lower):
            op = self._new(line, label, "io", text)
            op.inner_of_logical_if = inner
            expression = _io_reads(text, self.declared, self.arrays)
            op.reads += expression.reads
            op.sites += expression.sites
            return
        if NON_EXECUTABLE.match(lower):
            return
        raise ValueError(f"line {line}: unrecognised statement {text!r}")

    def _link(self) -> None:
        """Successors of every operation. An ELSE or ELSE IF is entered only by the
        false edge of the condition before it; every other edge into it, the fall
        through from the branch above and a loop's exit included, goes to its END IF."""
        ops = self.ops
        labelled = {op.label: op.index for op in ops if op.label}

        def after(index: int) -> int:
            return index + 1 if index + 1 < len(ops) else self.exit_index

        fallthrough: Dict[int, int] = {op.index: after(op.index) for op in ops}
        false_edges: Set[Tuple[int, int]] = set()
        endif_of: Dict[int, int] = {}
        do_stack: List[Op] = []
        if_stack: List[Tuple[int, List[int]]] = []
        loop_end: Dict[int, int] = {}
        for op in ops:
            if op.kind == "do":
                do_stack.append(op)
            elif op.kind == "if":
                if_stack.append((op.index, []))
            elif op.kind in ("elseif", "else"):
                pending, branches = if_stack.pop()
                false_edges.add((pending, op.index))
                branches.append(op.index)
                if_stack.append((op.index if op.kind == "elseif" else -1, branches))
            elif op.kind == "endif":
                pending, branches = if_stack.pop()
                if pending >= 0:
                    false_edges.add((pending, op.index))
                for branch in branches:
                    endif_of[branch] = op.index
            if op.kind == "enddo" or (op.label and any(d.do_label == op.label for d in do_stack)):
                while do_stack and (op.kind == "enddo" or do_stack[-1].do_label == op.label):
                    header = do_stack.pop()
                    loop_end[header.index] = op.index
                    fallthrough[op.index] = header.index
                    header.successors.append(after(op.index))
                    if op.kind == "enddo":
                        break
        if do_stack or if_stack:
            raise ValueError("unbalanced DO or IF in the range")
        self.loop_end = loop_end
        for op in ops:
            if op.kind == "cond":
                op.successors += [op.index + 1, after(op.index + 1)]
            elif op.kind == "goto":
                op.successors.append(labelled.get(op.goto, self.exit_index))
            elif op.kind in ("return", "end"):
                op.successors.append(self.exit_index)
            elif op.kind == "exit":
                header = max(h for h, end in loop_end.items() if h < op.index <= end)
                op.successors.append(after(loop_end[header]))
            else:
                op.successors.append(fallthrough[op.index])
        for source, target in false_edges:
            ops[source].successors.append(target)
        for op in ops:
            op.successors = [endif_of[t] if t in endif_of and (op.index, t) not in false_edges else t
                             for t in op.successors]

    def _blocks(self) -> None:
        """A block ends at every operation that is not a plain assignment (DO, END DO,
        IF, ELSE, END IF, a logical IF's condition and its statement, GO TO, CALL, I/O,
        CONTINUE, EXIT, RETURN, END); a labelled operation, an assignment with an array
        intrinsic and a whole-array assignment start one, and a whole-array assignment
        also ends its own."""
        block = 0
        ends_block = True
        for op in self.ops:
            starts = op.label is not None or op.array_intrinsic or op.whole_array
            if (ends_block or starts) and op.index > 0:
                block += 1
            op.block = block
            ends_block = op.kind != "assign" or op.inner_of_logical_if or op.whole_array

    def _loops(self) -> List[Tuple[int, int, str]]:
        return sorted((h, end, self.ops[h].do_variable) for h, end in self.loop_end.items())

    def innermost_loop(self, index: int) -> Optional[Tuple[int, int, str]]:
        enclosing = [loop for loop in self.loops if loop[0] < index <= loop[1]]
        return max(enclosing, key=lambda loop: loop[0]) if enclosing else None

    # -- rule L --

    def is_loop_sum(self, op: Op) -> bool:
        """The shape of rule L: a REAL*4 target added to itself (same subscript) with a REAL*4
        right-hand side, inside a DO loop whose index its subscript does not mention,
        no other operation of the loop touching the target. As a rounding rule (rounded
        once after the loop) it is retired, 2026-10-03: the executable's listing stores
        such a sum on its own schedule (src/Statistics/BOOT.md, "## Setup plane")."""
        if op.kind != "assign" or kind_code(op.target, self.declared) != "r4" or op.rhs is None:
            return False
        node = op.rhs.node
        if node.typ != "r4" or node.kind != "bin" or node.operator not in ("+", "-"):
            return False
        own = op.target + ("(" + op.subscript + ")" if op.subscript else "")
        if node.kids[0].text.lower().replace(" ", "") != own:
            return False
        loop = self.innermost_loop(op.index)
        if loop is None or loop[2] in op.subscript_names:
            return False
        for other in self.ops[loop[0]:loop[1] + 1]:
            if other.index == op.index:
                continue
            if other.target == op.target or any(r.name == op.target for r in other.reads):
                return False
            if other.kind == "assign" and other.target in op.subscript_names:
                return False
        return True

    # -- reaching definitions --

    def reaching(self) -> List[Set[Tuple[int, bool]]]:
        """IN set of every operation (and of the exit pseudo-operation, last): facts
        (defining op, stale). A fact goes stale when a name in its subscript is assigned,
        a DO header steps its index, or it flows round into the next cycle; a stale fact
        no longer names the element its subscript text says."""
        ops = self.ops
        count = len(ops) + 1
        predecessors: List[List[int]] = [[] for _ in range(count)]
        for op in ops:
            for successor in op.successors:
                predecessors[successor].append(op.index)
        incoming: List[Set[Tuple[int, bool]]] = [set() for _ in range(count)]
        outgoing: List[Set[Tuple[int, bool]]] = [set() for _ in range(count)]
        changed = True
        while changed:
            changed = False
            for index in range(count):
                facts: Set[Tuple[int, bool]] = set()
                for predecessor in predecessors[index]:
                    facts |= outgoing[predecessor]
                if index == 0:
                    facts |= {(d, ops[d].subscript is not None or stale) for d, stale in outgoing[self.exit_index]}
                if facts != incoming[index]:
                    incoming[index] = facts
                result = facts if index == self.exit_index else self._transfer(ops[index], facts)
                if result != outgoing[index]:
                    outgoing[index] = result
                    changed = True
        return incoming

    def definitions_read(self, incoming: List[Set[Tuple[int, bool]]], op: Op,
                         read: Read) -> Tuple[Optional[int], List[int]]:
        """(in-block definition, definitions read from memory) for one read. A block is
        straight-line, so an identical definition earlier in the read's own block is the
        one read and shadows every other; otherwise every reaching definition of the
        name is read from memory, but one whose element provably differs."""
        facts = [(d, stale) for d, stale in incoming[op.index] if self.ops[d].target == read.name]
        local = [d for d, stale in facts if not stale and d < op.index
                 and self.ops[d].block == op.block and not self.ops[d].whole_array
                 and self.ops[d].subscript == read.subscript]
        if local:
            return max(local), []
        others = [d for d, stale in facts
                  if stale or self.ops[d].subscript is None or read.subscript is None
                  or not subscripts_differ(self.ops[d].subscript, read.subscript)]
        return None, sorted(others)

    def _transfer(self, op: Op, facts: Set[Tuple[int, bool]]) -> Set[Tuple[int, bool]]:
        result: Set[Tuple[int, bool]] = set()
        stepped = op.do_variable if op.kind == "do" else (
            op.target if op.kind == "assign" and op.subscript is None and not op.whole_array else None)
        for definition, stale in facts:
            other = self.ops[definition]
            if op.kind == "assign" and other.target == op.target:
                if op.subscript is None:
                    continue
                if not stale and other.subscript == op.subscript:
                    continue
            if op.kind == "call" and any(r.name == other.target for r in op.reads):
                continue
            if stepped is not None and stepped in other.subscript_names:
                stale = True
            result.add((definition, stale))
        if op.kind == "assign":
            result.add((op.index, False))
        return result


def subscripts_differ(left: str, right: str) -> bool:
    """True when two subscripts provably name different elements: in some position both
    are `v`, `v+c` or `v-c` over the same name with different constants."""
    def affine(part: str) -> Optional[Tuple[str, int]]:
        match = re.fullmatch(r"([a-z_][a-z0-9_]*)(?:([+-])(\d+))?", part)
        if not match:
            return None
        offset = int(match.group(3) or 0)
        return match.group(1), (-offset if match.group(2) == "-" else offset)
    lefts, rights = left.split(","), right.split(",")
    if len(lefts) != len(rights):
        return False
    for a, b in zip(lefts, rights):
        fa, fb = affine(a), affine(b)
        if fa and fb and fa[0] == fb[0] and fa[1] != fb[1]:
            return True
    return False


# ---- names read outside a range --------------------------------------------------

def names_read(first: int, last: int) -> Set[str]:
    """Every name occurring in an executable statement of first..last other than as a
    bare assignment target (string literals and FORMAT statements dropped). A
    deliberately wide reading: a name it reports read is at worst read from memory."""
    names: Set[str] = set()
    for statement in statements(first, last):
        text = re.sub(r"'[^']*'|\"[^\"]*\"", "", statement.text)
        if NON_EXECUTABLE.match(text.lower()):
            continue
        _, rest = split_if(text) if re.match(r"^if\s*\(", text, re.IGNORECASE) else (None, text)
        assignment = ASSIGNMENT.match(rest)
        if assignment:
            text = text[:len(text) - len(rest)] + (assignment.group(2) or "") + " " + assignment.group(3)
        names |= {word.lower() for word in re.findall(r"[A-Za-z_][A-Za-z0-9_]*", text)}
    return names


# ---- self-test of the continuation rule ------------------------------------------

def selftest() -> None:
    """The continuation rule on the lines that decided it (module docstring)."""
    lines = read_all_lines()
    initial = (873, 1739, 1740)
    continued = (864, 866, 870, 876, 878, 880, 1032, 1037, 1040, 1152)
    failures = [n for n in initial if is_continuation(lines[n - 1])]
    failures += [n for n in continued if not is_continuation(lines[n - 1])]
    if failures:
        raise SystemExit(f"selftest: continuation rule wrong on lines {failures}")
    every = [n for n, raw in enumerate(lines, 1) if is_continuation(raw)]
    old = [n for n, raw in enumerate(lines, 1)
           if not is_comment(raw) and len(raw) > 5 and raw[0:5].strip() == "" and raw[5] not in (" ", "\t")]
    if sorted(set(old) - set(every)) != list(initial) or set(every) - set(old):
        raise SystemExit(f"selftest: the rule moved other lines: old-only {sorted(set(old) - set(every))}, "
                         f"new-only {sorted(set(every) - set(old))}")
    print(f"selftest: {len(every)} continuation lines; 873, 1739, 1740 initial, as before "
          "2026-10-01 they were not; no other line moved")


if __name__ == "__main__":
    if sys.argv[1:] != ["selftest"]:
        print("usage: fortran_source.py selftest", file=sys.stderr)
        raise SystemExit(2)
    selftest()
