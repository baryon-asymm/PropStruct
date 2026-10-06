"""What a row of the address map says about the Fortran lines it compiles: the REAL assignments
those lines hold, the register values and eliminated lines it claims, the line each store of a
name belongs to, and the text of every line (`listing_checks.py` holds the map to the listing,
`listing_sites.py` derives the table from it).

Row attributes read here, besides `parts=` (lines compiled only in part, their stores lying in
a later row):

  register=<name>:<line>@<address>[+<address>...]:<op>
        the value the line assigns to <name> stays in an x87 register and is never stored; an
        <address> is an instruction's (`0x40C175`, or `0x41007C~2` for the third node it makes)
        and <op> the operator of the node: `+ - * / phi const sqrt abs`
  eliminated=<name>:<line>[,...]    the line assigns <name> and the executable has no code for it
  stores=<name>:<line>[+<line>...][,...]
                                    of several lines of the row assigning <name>, the ones whose
                                    computed values the row's stores of <name> hold (several when a
                                    control-flow merge or two moves store them)
  temp=<name>:<line>[,...]          the line a compiler temporary of the row is made for

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
from typing import Dict, List, NamedTuple, Optional, Tuple

import fortran_source as fs
import listing_map as lm

REGISTER_CLAIM = re.compile(r"^([A-Za-z0-9_]+):(\d+)@([^:]+):([^,]+)$")
NAME_LINE = re.compile(r"^([A-Za-z0-9_]+):(\d+)$")
ADDRESS = re.compile(r"^0x([0-9A-Fa-f]+)(?:~(\d+))?$")
LITERAL = re.compile(r"^\s*[-+]?(\d+\.?\d*|\.\d+)([eEdD][-+]?\d+)?(_\w+)?\s*$")


class Claim(NamedTuple):
    name: str
    line: int
    sites: Tuple[Tuple[int, int], ...]        # (the instruction's address, which of its nodes)
    op: str


class Assign(NamedTuple):
    name: str
    line: int
    literal: bool                              # the right-hand side is a lone number
    lhs: str
    text: str


def register_claims(row: lm.Row) -> List[Claim]:
    claims: List[Claim] = []
    for token in row.attrs.get("register", "").split(","):
        if not token:
            continue
        match = REGISTER_CLAIM.match(token)
        if match is None:
            raise lm.MapError(f"row {row.id} (map line {row.source}): register claim {token!r} is malformed")
        sites = []
        for text in match.group(3).split("+"):
            address = ADDRESS.match(text)
            if address is None:
                raise lm.MapError(f"row {row.id} (map line {row.source}): address {text!r} is malformed")
            sites.append((int(address.group(1), 16), int(address.group(2) or 0)))
        claims.append(Claim(match.group(1).lower(), int(match.group(2)), tuple(sites), match.group(4)))
    return claims


def name_lines(row: lm.Row, key: str) -> List[Tuple[str, int]]:
    found: List[Tuple[str, int]] = []
    for token in row.attrs.get(key, "").split(","):
        if token:
            match = NAME_LINE.match(token)
            if match is None:
                raise lm.MapError(f"row {row.id} (map line {row.source}): {key} claim {token!r} is malformed")
            found.append((match.group(1).lower(), int(match.group(2))))
    return found


def part_lines(row: lm.Row) -> Tuple[int, ...]:
    return lm.parse_lines(row.attrs.get("parts", "").replace(",", " "))


def line_texts() -> Dict[int, str]:
    """The text of every statement line, a line holding several statements joined."""
    texts: Dict[int, str] = {}
    for statement in fs.statements(1, fs.MAIN_PROGRAM_END):
        for number in statement.physical:
            texts[number] = (texts.get(number, "") + " " + statement.text).strip()
    return texts


def split_assignment(text: str) -> Optional[Tuple[str, str, str]]:
    """(name, left-hand side, right-hand side) of an assignment, also behind a logical IF's
    condition, or None."""
    body = text.strip()
    match = re.match(r"^if\s*\(", body, re.IGNORECASE)
    if match:
        depth, position = 1, match.end()
        while position < len(body) and depth:
            depth += (body[position] == "(") - (body[position] == ")")
            position += 1
        body = body[position:].strip()
        if re.match(r"^then\b", body, re.IGNORECASE):
            return None
    if re.match(r"^do\s", body, re.IGNORECASE):
        return None
    match = fs.ASSIGNMENT.match(body)
    if match is None:
        return None
    return match.group(1).lower(), body[:body.index("=")].strip() if "=" in body else "", match.group(3)


def assigns(row: lm.Row) -> List[Assign]:
    """Every REAL assignment the row's own lines hold, in line order."""
    kinds = fs.parse_declared_kinds(*fs.MAIN_DECLARATIONS)
    found: List[Assign] = []
    for statement in fs.statements(1, fs.MAIN_PROGRAM_END):
        if statement.line not in row.lines:
            continue
        parsed = split_assignment(statement.text)
        if parsed is not None and fs.kind_code(parsed[0], kinds) in ("r4", "r8"):
            found.append(Assign(parsed[0], statement.line, bool(LITERAL.match(parsed[2])), parsed[1],
                                " ".join(statement.text.split())))
    return found


def store_hints(row: lm.Row) -> Dict[str, Tuple[int, ...]]:
    hints: Dict[str, Tuple[int, ...]] = {}
    for token in row.attrs.get("stores", "").split(","):
        if token:
            name, _, lines = token.partition(":")
            try:
                hints[name.lower()] = tuple(int(part) for part in lines.split("+"))
            except ValueError as error:
                raise lm.MapError(f"row {row.id} (map line {row.source}): stores claim {token!r} is malformed") from error
    return hints


def place(row: lm.Row, name: str, constant: bool) -> Tuple[Tuple[int, ...], Optional[str]]:
    """The lines of the row a store of `name` belongs to, a constant store to the literal
    assignment, a computed one to the non-literal assignment (the row's `stores=` names them when
    there are several): (lines, None), or ((), why not)."""
    mine = [a for a in assigns(row) if a.name == name]
    if not mine:
        return (), f"no line of row {row.id} assigns {name}"
    wanted = [a for a in mine if a.literal == constant]
    if constant:
        if len(wanted) == 1:
            return (wanted[0].line,), None
        return (), f"{len(wanted)} literal assignments of {name} in row {row.id}"
    hinted = store_hints(row).get(name)
    if hinted:
        return hinted, None
    if len(wanted) == 1:
        return (wanted[0].line,), None
    return (), f"{len(wanted)} computed assignments of {name} in row {row.id}, and no `stores=` hint"
