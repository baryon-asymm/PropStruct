"""The hand-written address map of the per-cycle plane (`CyclePlaneListing.map.txt`) and the
checks that hold it against the listing excerpt.

The map is the one artefact no tool can derive: it says which Fortran name lives at which
address (the executable carries no symbols and no line tables) and which Fortran lines each
stretch of code compiles. Every claim in it is checked here against the excerpt, so a wrong
claim is a red check, never a silent label:

  (a) every region and row range lies in the excerpt, on instruction boundaries, and the
      regions cover the excerpt exactly;
  (b) completeness both ways: every float store, load and integer copy of a float inside a
      scope lies in exactly one row, resolves to a name that row declares, and every name a
      row declares is used inside it; no float event lands on an undeclared address;
  (c) every call goes to a thunk the reader has a stub for, after the seven `ffree`;
  (d) a cell used under more than one name declares each tenant, and a declared tenant is
      used; a tenant's type is the Fortran's own (`fortran_source.kind_code`) and its name
      occurs in the text of its row's lines;
  (e) every float cell the walk reads before it has written it is an input the map names,
      with a source, and the source's region holds an instruction that writes it;
  (f) every array's descriptor is the one the ALLOCATE statements write, in the Fortran's
      allocate order, with the element size and the rank of its declared type.

Line format, `|`-separated, `#` starts a comment:

  region | <id> | <first> | <end> | <mode> | <what>       mode: executed, layout, scan, callee, thunks, startup, prologue
  scope  | <first> | <end> | <what>                the ranges whose events the rows must claim
  array  | <descriptor address> | <name> | <r4|r8|i4|i8> | <rank>
  cell   | <0xADDRESS or ebp-0xOFFSET> | <name>:<type>[:temp] ...        (several tenants: all listed)
  ints   | <first location> | <last location> | <region id or external> | <what>   (integer slots)
  row    | <id> | <first> | <end> | <lines> | <names> | <attributes>
  input  | <location> | <region id or external> | <what>   (a float cell the walk reads before it writes it)
  identity | <text>
  oracle | <kind> | ...                          (the listing oracle's data: `tests/Fixtures/cycle_plane_oracle.py`
                                                  reads it, nothing here checks it; the kinds are `src/Statistics/API.md`'s)

A row's attributes are `key=value` words, several values separated by commas; their
meaning is `listing_rows.py`'s: `register=` (a value the line assigns that stays in an x87
register and is never stored), `eliminated=` (a line the executable has no code for),
`stores=` (which lines' values a row's stores of a name hold), `temp=` (the line a compiler
temporary is made for), `parts=` (lines compiled only in part, their stores in a later row).

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
from pathlib import Path
from typing import Dict, List, NamedTuple, Optional, Set, Tuple

NODE_ROOT = Path(__file__).resolve().parent
MAP_PATH = NODE_ROOT / "CyclePlaneListing.map.txt"
REGION_MODES = ("executed", "layout", "scan", "callee", "thunks", "startup", "prologue")


class MapError(Exception):
    pass


class Tenant(NamedTuple):
    name: str
    type: str                  # r4, r8, i4, i8, i2
    temp: bool                 # a compiler temporary, not a Fortran variable
    high: bool = False         # the upper dword of a REAL*8 tenant


class Cell(NamedTuple):
    loc: Tuple                 # ('abs', address) or ('frame', disp)
    tenants: Tuple[Tenant, ...]
    source: int


class ArrayDecl(NamedTuple):
    descriptor: int
    name: str
    type: str
    rank: int
    source: int


class IntRun(NamedTuple):
    first: Tuple
    last: Tuple
    origin: str                # the id of the region that writes it, or `external`
    what: str
    source: int


class Region(NamedTuple):
    id: str
    first: int
    end: int
    mode: str
    what: str
    source: int


class Scope(NamedTuple):
    first: int
    end: int
    what: str
    source: int


class Row(NamedTuple):
    id: str
    first: int
    end: int
    lines: Tuple[int, ...]
    names: Tuple[str, ...]
    attrs: Dict[str, str]
    source: int


class Input(NamedTuple):
    loc: Tuple
    origin: str                # the id of the region that writes it, or `external`
    what: str
    source: int


class Map(NamedTuple):
    regions: List[Region]
    scopes: List[Scope]
    arrays: Dict[int, ArrayDecl]
    cells: Dict[Tuple, Cell]
    runs: List[IntRun]
    rows: List[Row]
    inputs: Dict[Tuple, Input]
    identities: List[str]
    oracle: List[Tuple[str, ...]]


def parse_location(text: str) -> Tuple:
    text = text.strip()
    if text.startswith("ebp"):
        sign = -1 if text[3] == "-" else 1
        return ("frame", sign * int(text[4:], 16))
    return ("abs", int(text, 16))


def format_location(loc: Tuple) -> str:
    if loc[0] == "frame":
        return f"ebp{'-' if loc[1] < 0 else '+'}0x{abs(loc[1]):X}"
    return f"0x{loc[1]:06X}"


def parse_lines(text: str) -> Tuple[int, ...]:
    lines: List[int] = []
    for token in text.replace(",", " ").split():
        if token == "-":
            continue
        if "-" in token:
            first, last = token.split("-")
            lines.extend(range(int(first), int(last) + 1))
        else:
            lines.append(int(token))
    return tuple(lines)


def parse_text(text: str, name: str = "map") -> Map:
    regions: List[Region] = []
    scopes: List[Scope] = []
    arrays: Dict[int, ArrayDecl] = {}
    cells: Dict[Tuple, Cell] = {}
    runs: List[IntRun] = []
    rows: List[Row] = []
    inputs: Dict[Tuple, Input] = {}
    identities: List[str] = []
    oracle: List[Tuple[str, ...]] = []
    for number, raw in enumerate(text.split("\n"), start=1):
        line = raw.split("#")[0].strip()
        if not line:
            continue
        fields = [field.strip() for field in line.split("|")]
        kind = fields[0]
        try:
            if kind == "region":
                if fields[4] not in REGION_MODES:
                    raise MapError(f"unknown region mode {fields[4]!r}")
                regions.append(Region(fields[1], int(fields[2], 16), int(fields[3], 16), fields[4], fields[5],
                                      number))
            elif kind == "scope":
                scopes.append(Scope(int(fields[1], 16), int(fields[2], 16), fields[3], number))
            elif kind == "array":
                descriptor = int(fields[1], 16)
                if descriptor in arrays:
                    raise MapError("descriptor declared twice")
                arrays[descriptor] = ArrayDecl(descriptor, fields[2], fields[3], int(fields[4]), number)
            elif kind == "cell":
                loc = parse_location(fields[1])
                tenants = []
                for token in fields[2].split():
                    parts = token.split(":")
                    flags = parts[2:]
                    if any(flag not in ("temp", "hi") for flag in flags):
                        raise MapError(f"unknown tenant flag in {token!r}")
                    tenants.append(Tenant(parts[0], parts[1], "temp" in flags, "hi" in flags))
                if loc in cells:
                    raise MapError("cell declared twice")
                cells[loc] = Cell(loc, tuple(tenants), number)
            elif kind == "ints":
                runs.append(IntRun(parse_location(fields[1]), parse_location(fields[2]), fields[3], fields[4], number))
            elif kind == "row":
                attrs: Dict[str, str] = {}
                for token in fields[6].split() if len(fields) > 6 else []:
                    key, _, value = token.partition("=")
                    attrs[key] = value
                rows.append(Row(fields[1], int(fields[2], 16), int(fields[3], 16), parse_lines(fields[4]),
                                tuple(t for t in fields[5].split() if t != "-"), attrs, number))
            elif kind == "input":
                loc = parse_location(fields[1])
                if loc in inputs:
                    raise MapError("input declared twice")
                inputs[loc] = Input(loc, fields[2], fields[3] if len(fields) > 3 else "", number)
            elif kind == "identity":
                identities.append(fields[1])
            elif kind == "oracle":
                oracle.append(tuple(fields[1:]))
            else:
                raise MapError(f"unknown kind {kind!r}")
        except (MapError, ValueError, IndexError, AttributeError) as error:
            raise MapError(f"{name}:{number}: {error}: {raw.strip()}") from error
    return Map(regions, scopes, arrays, cells, runs, rows, inputs, identities, oracle)


def parse(path: Path = MAP_PATH) -> Map:
    return parse_text(path.read_text(encoding="utf-8"), path.name)
