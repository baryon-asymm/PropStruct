"""The listing oracle: the executable's own bytes, run over constructed inputs, as the known answer
of the per-cycle plane (`src/Statistics/ACCEPTANCE.md`, A2; `tests/Fixtures/API.md`,
"## Listing oracle").

What it does. It builds the state the executable's code finds after its allocations (the array
descriptors, a few persistent slots, the formulation's and the menu's values), runs the
executable's init block, PARAM's return and the pre-loop through `x87_machine.py`, compares what
the pre-loop computed (line 378, the DOKM sums) with `formulas_statistics.oracle_setup`, then, per
cycle, injects the totals the particle loop would have left, runs the plane to the exit the bytes
take and reads back what it stored. Nothing it computes is typed: every number is the machine's
and every name, address-to-name link and generator of a total is data in
`src/Statistics/CyclePlaneListing.map.txt`.

    python cycle_plane_oracle.py generate   # runs the cases, writes the fixture
    python cycle_plane_oracle.py extend     # adds the cases of the map's `pin` rows the fixture lacks
    python cycle_plane_oracle.py restamp    # rewrites the provenance digests only, the cases untouched
    python cycle_plane_oracle.py --verify   # regenerates in memory, fails on any byte difference
    python cycle_plane_oracle.py selftest   # the guard, the layout check and the oracle's own proofs

No map name, formula or plane literal appears in this file: `guard` fails when one does.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import argparse
import ast
import base64
import hashlib
import json
import math
import multiprocessing
import os
import pickle
import random
import re
import struct
import subprocess
import sys
import time
import zlib
from pathlib import Path
from typing import Callable, Dict, Iterator, List, NamedTuple, Optional, Sequence, Tuple

import check_cycle_plane_listing as lc
import formulas_statistics as fs
import x87_machine as xm

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent
MAP_PATH = ROOT / "src" / "Statistics" / "CyclePlaneListing.map.txt"
TABLE_PATH = ROOT / "src" / "Statistics" / "CyclePlane.listing.generated.txt"
FORMULATIONS = HERE / "Legacy" / "formulations"
FORTRAN_NAME = "PropStructv3.for.txt"
FIXTURE_PATH = HERE / "cases" / "statistics" / "cycle_plane_oracle.json"
RECIPES_PATH = HERE / "cases" / "statistics" / "cycle_plane_oracle_recipes.json"

FRAME_TOP = 0x0012F000                  # the synthetic ebp
FRAME_BYTES = 0x4000
ARRAY_BASE = 0x20000000                 # the synthetic address of the first array block
PLANE_STEP_BUDGET = 40_000_000          # instructions one cycle's run may take before stop O1
ELEMENT_BYTES = {"r4": 4, "r8": 8, "i4": 4, "i8": 8}
DESCRIPTOR_FIELDS = {"base": 0x00, "element": 0x04, "offset": 0x08, "flags": 0x0C, "rank": 0x10, "dims": 0x14}


class OracleError(Exception):
    """Something the oracle's own data or checks refuse: a stop of the oracle, never a skip."""


# ---- the map ----------------------------------------------------------------------------------

class ArrayRow(NamedTuple):
    descriptor: int
    name: str
    type: str
    rank: int


class Tenant(NamedTuple):
    name: str
    type: str
    temp: bool
    high: bool


class OracleMap(NamedTuple):
    arrays: Dict[str, ArrayRow]
    cells: Dict[Tuple, List[Tenant]]
    rows: Dict[str, List[Tuple[str, ...]]]


def parse_location(text: str) -> Tuple[str, int]:
    text = text.strip()
    if text.startswith("ebp"):
        sign = -1 if text[3] == "-" else 1
        return ("frame", sign * int(text[4:], 16))
    return ("abs", int(text, 16))


def location_address(loc: Tuple[str, int]) -> int:
    return FRAME_TOP + loc[1] if loc[0] == "frame" else loc[1]


def load_map(path: Path = MAP_PATH) -> OracleMap:
    return parse_map(path.read_text(encoding="utf-8"))


def parse_map(text: str) -> OracleMap:
    arrays: Dict[str, ArrayRow] = {}
    cells: Dict[Tuple, List[Tenant]] = {}
    rows: Dict[str, List[Tuple[str, ...]]] = {}
    for raw in text.split(chr(10)):
        line = raw.split("#")[0].strip()
        if not line:
            continue
        fields = [field.strip() for field in line.split("|")]
        kind = fields[0]
        if kind == "array":
            arrays[fields[2]] = ArrayRow(int(fields[1], 16), fields[2], fields[3], int(fields[4]))
        elif kind == "cell":
            tenants = []
            for token in fields[2].split():
                parts = token.split(":")
                tenants.append(Tenant(parts[0], parts[1], "temp" in parts[2:], "hi" in parts[2:]))
            cells[parse_location(fields[1])] = tenants
        elif kind == "oracle":
            rows.setdefault(fields[1], []).append(tuple(fields[2:]))
        elif kind == "row":
            rows.setdefault("@row", []).append((fields[1], fields[2], fields[3]))
    return OracleMap(arrays, cells, rows)


class Site(NamedTuple):
    at: int
    row: str
    lines: str
    target: str
    type: str
    kind: str
    rounds: str
    home: str
    count: str
    reads: str
    schedule: str
    order: str


def load_sites(path: Path = TABLE_PATH) -> List[Site]:
    sites = []
    for raw in path.read_text(encoding="utf-8").split("\n"):
        if not raw or raw.startswith("#"):
            continue
        fields = raw.split(" | ")
        sites.append(Site(int(fields[0], 16), *fields[1:]))
    return sites


# ---- the setup, the allocation statements and the descriptors -----------------------------------

def fortran_path() -> Path:
    """The verified path of the Fortran source outside the repository (tools/legacy), resolved at the call."""
    return Path(lc.legacy.legacy_file(FORTRAN_NAME))


def allocation_extents(source: Optional[Path] = None) -> Dict[str, List[str]]:
    """The extent expressions of every ALLOCATE statement of the Fortran, by lower-cased array name."""
    extents: Dict[str, List[str]] = {}
    for raw in (source or fortran_path()).read_text(encoding="utf-8").split("\n"):
        text = raw.strip()
        if not text.lower().startswith("allocate("):
            continue
        body = text[text.index("(") + 1:text.rindex(")")]
        parts: List[str] = []
        depth = 0
        current = ""
        for char in body:
            depth += char == "("
            depth -= char == ")"
            if char == "," and depth == 0:
                parts.append(current.strip())
                current = ""
            else:
                current += char
        parts.append(current.strip())
        for part in parts:
            name, _, dims = part.partition("(")
            extents[name.strip().lower()] = [dim.strip() for dim in dims.rstrip(")").split(",")]
    return extents


SAFE_BUILTINS = {"max": max, "min": min, "sum": sum, "range": range, "round": round, "int": int, "len": len,
                 "enumerate": enumerate, "abs": abs, "float": float, "zip": zip, "list": list}


def evaluate(expression: str, namespace: Dict[str, object]):
    scope = {"__builtins__": SAFE_BUILTINS}
    scope.update(namespace)
    return eval(expression, scope)  # noqa: S307  (the map's own expressions, never user input)


def to_binary32(value: float) -> float:
    return struct.unpack("<f", struct.pack("<f", value))[0]


def encode(type_: str, value) -> bytes:
    if type_ == "r4":
        return struct.pack("<f", value)
    if type_ == "r8":
        return struct.pack("<d", value)
    if type_ == "i4":
        return int(value).to_bytes(4, "little", signed=True)
    if type_ == "i8":
        return int(value).to_bytes(8, "little", signed=True)
    raise OracleError(f"type {type_}")


def decode(type_: str, raw: bytes):
    if type_ == "r4":
        return struct.unpack("<f", raw)[0]
    if type_ == "r8":
        return struct.unpack("<d", raw)[0]
    return int.from_bytes(raw, "little", signed=True)


def hex_bits(type_: str, raw: bytes) -> str:
    """A value's bits, most significant byte first, as the fixture writes them."""
    return raw[::-1].hex()


def hex_of(type_: str, value) -> str:
    return hex_bits(type_, encode(type_, value))


class Block(NamedTuple):
    base: int
    size: int
    type: str
    dims: Tuple[int, ...]


class Totals:
    """The helpers a `total` expression may use, drawing from one seeded generator."""

    def __init__(self, seed: int):
        self.random = random.Random(seed)

    def namespace(self) -> Dict[str, object]:
        r = self.random

        def partition_w(total: int, weights: Sequence[float]) -> List[int]:
            scale = sum(weights)
            counts = [int(total * w / scale) for w in weights]
            positive = [k for k, w in enumerate(weights) if w > 0]
            for _ in range(total - sum(counts)):
                counts[r.choice(positive)] += 1
            return counts

        def partition(total: int, bins: int) -> List[int]:
            return partition_w(total, [1.0] * bins)

        def grid(rows: int, cols: int, active: int, total: int, empty: float) -> List[List[int]]:
            alive = [row < min(active, rows) and r.random() >= empty for row in range(rows)]
            weights = [[r.random() if alive[row] else 0.0 for _ in range(cols)] for row in range(rows)]
            flat = partition_w(total, [w for row in weights for w in row])
            return [flat[row * cols:(row + 1) * cols] for row in range(rows)]

        return {"randint": r.randint, "uniform": r.uniform, "gauss": r.gauss, "f32": to_binary32,
                "partition": partition, "partition_w": partition_w, "grid": grid,
                "choice": lambda *items: r.choice(items)}


# ---- the world: one case's memory, descriptors and runs -----------------------------------------

class World:
    """A machine holding one case: the blocks, the descriptors, the injected setup and the init
    block already run."""

    def __init__(self, case_setup: Dict[str, object], oracle_map: OracleMap, sites: Sequence[Site],
                 machine_options: Optional[Dict[str, object]] = None, hook_site: Optional[int] = None):
        self.setup = case_setup
        self.map = oracle_map
        self.sites = list(sites)
        options = dict(machine_options or {})
        self.runs = {row[0]: (int(row[1], 16), int(row[2], 16)) for row in oracle_map.rows["run"]}
        slices = [(int(row[0], 16), int(row[1], 16)) for row in oracle_map.rows["slice"]]
        self.slices = slices
        callees = [(int(row[1], 16), int(row[2], 16)) for row in oracle_map.rows["callee"]]
        self.machine = xm.Machine(list(self.runs.values()) + callees + slices, **options)
        self.exits = {int(row[0], 16): row[1] for row in oracle_map.rows["exit"]}
        self.step_budget = PLANE_STEP_BUDGET
        self.io_sites: List[int] = []
        self.pow_triples: List[Tuple[float, float, float]] = []
        self.blocks: Dict[str, Block] = {}
        self.regions: Dict[str, xm.Region] = {}
        self.loops = load_loops(oracle_map)
        self.row_ranges = [(int(first, 16), int(end, 16), name) for name, first, end in oracle_map.rows["@row"]]
        self._build()
        self.machine.track_stores = True
        self.instrumentation = Instrumentation(self, hook_site)

    # -- construction -----------------------------------------------------------------------

    def _build(self) -> None:
        m = self.machine
        m.add_region("frame", FRAME_TOP - FRAME_BYTES, FRAME_BYTES)
        m.set_reg(5, FRAME_TOP)
        frame_size = self._frame_size()
        m.set_reg(4, FRAME_TOP - frame_size - 12)
        extents = allocation_extents()
        cursor = ARRAY_BASE
        for name, row in self.map.arrays.items():
            dims_text = extents.get(name.lower())
            if dims_text is None:
                raise OracleError(f"the Fortran allocates no array {name}")
            dims = tuple(int(evaluate(text.lower(), dict(self.setup))) for text in dims_text)
            if len(dims) != row.rank:
                raise OracleError(f"{name}: rank {row.rank} in the map, {len(dims)} extents in the Fortran")
            width = ELEMENT_BYTES[row.type]
            size = width
            for dim in dims:
                size *= dim
            self.regions[name] = m.add_region(name, cursor, max(size, 1))
            self.blocks[name] = Block(cursor, size, row.type, dims)
            self._write_descriptor(row, self.blocks[name])
            cursor += ((size + 0xFFF) // 0x1000 + 1) * 0x1000
        for kind_row in self.map.rows["inject"]:
            self.inject_scalar(parse_location(kind_row[0]), kind_row[2], evaluate(kind_row[3], dict(self.setup)))
        for slot, name, _what in self.map.rows["pointer"]:
            block = self.blocks[name]
            self.inject_scalar(parse_location(slot), "i4", block.base - ELEMENT_BYTES[block.type])
        for name, key, _what in self.map.rows["array"]:
            self.inject_array(name, self.setup[key])
        for target, label, _what in self.map.rows["call"]:
            m.stubs[int(target, 16)] = self._stub(label)

    def _frame_size(self) -> int:
        """The frame the prologue allocates, read from its `sub esp,imm` (the oracle never runs it)."""
        low, high = (0x401000, 0x40100C)
        for ins in self.machine.decode_listing_range(low, high):
            if ins.text.startswith("sub esp,"):
                return int(ins.text.split(",")[1].rstrip("h"), 16)
        raise OracleError("the prologue holds no `sub esp`")

    def _write_descriptor(self, row: ArrayRow, block: Block) -> None:
        m = self.machine
        d = row.descriptor
        width = ELEMENT_BYTES[row.type]
        strides = [width]
        for dim in block.dims[:-1]:
            strides.append(strides[-1] * dim)
        m.write_int(d + DESCRIPTOR_FIELDS["base"], 4, block.base)
        m.write_int(d + DESCRIPTOR_FIELDS["element"], 4, width)
        m.write_int(d + DESCRIPTOR_FIELDS["offset"], 4, (-sum(strides)) & xm.MASK32)
        m.write_int(d + DESCRIPTOR_FIELDS["flags"], 4, 0x09000005)
        m.write_int(d + DESCRIPTOR_FIELDS["rank"], 4, row.rank)
        for k, (dim, stride) in enumerate(zip(block.dims, strides)):
            m.write_int(d + DESCRIPTOR_FIELDS["dims"] + 12 * k, 4, dim)
            m.write_int(d + DESCRIPTOR_FIELDS["dims"] + 12 * k + 4, 4, stride)
            m.write_int(d + DESCRIPTOR_FIELDS["dims"] + 12 * k + 8, 4, 1)

    def row_of(self, address: int) -> str:
        """The map row an instruction address lies in, or the empty string."""
        for first, end, name in self.row_ranges:
            if first <= address < end:
                return name
        return ""

    def watch_homes(self) -> None:
        """Start a log of every store to a scalar home of the map, cleared per plane run."""
        self.machine.watch = {location_address(loc): [] for loc in self.map.cells}

    def clear_written(self) -> None:
        for region in self.regions.values():
            region.touched = False

    def written_names(self) -> List[str]:
        """The arrays a store has reached since `clear_written`."""
        return [name for name, region in self.regions.items() if region.touched]

    # -- injection --------------------------------------------------------------------------

    def inject_scalar(self, loc: Tuple[str, int], type_: str, value) -> None:
        self.machine.write_bytes(location_address(loc), encode(type_, value))

    def inject_array(self, name: str, values) -> None:
        block = self.blocks.get(name)
        if block is None:
            return
        flat = flatten_column_major(values, block.dims)
        width = ELEMENT_BYTES[block.type]
        if len(flat) * width != block.size:
            raise OracleError(f"{name}: {len(flat)} values for a block of {block.size // width}")
        self.machine.write_bytes(block.base, b"".join(encode(block.type, v) for v in flat))

    def read_array(self, name: str) -> bytes:
        """A block's bytes, an element no store reached read as zero (the Fortran leaves it undefined)."""
        block = self.blocks[name]
        region = self.machine.region_at(block.base, block.size)
        offset = block.base - region.base
        data = bytearray(region.data[offset:offset + block.size])
        if region.defined is not None:
            for position in range(block.size):
                if not region.defined[offset + position]:
                    data[position] = 0
        return bytes(data)

    # -- the stubs --------------------------------------------------------------------------

    def _stub(self, label: str) -> Callable[[xm.Machine], Optional[int]]:
        if label == "size":
            return self._stub_size
        if label == "io":
            return self._stub_io
        if label == "sqrt":
            return self._stub_sqrt
        if label == "pow":
            return self._stub_pow
        if label == "mod":
            return self._stub_mod
        raise OracleError(f"no stub {label}")

    @staticmethod
    def _stub_size(m: xm.Machine) -> int:
        """SIZE, the routine PARAM calls last: nothing the oracle reads comes from it (its results are
        Dmax and Nfract), so the call only pops its eight arguments, the `ret 20h` at 0x418644."""
        return 0x20

    def _stub_io(self, m: xm.Machine) -> None:
        self.io_sites.append(m.calls[-1][0])
        m.set_reg(0, 0)

    @staticmethod
    def _stub_sqrt(m: xm.Machine) -> None:
        value = m.fpu_pop()
        if value < 0.0:
            raise xm.Stop("O3", "square root of a negative value (invalid)")
        m.fpu_push(math.sqrt(value))

    def _stub_pow(self, m: xm.Machine) -> None:
        exponent = m.fpu_pop()
        base = m.fpu_pop()
        try:
            result = math.pow(base, exponent)
        except (ValueError, OverflowError):
            raise xm.Stop("O3", "a power that is invalid or overflows") from None
        self.pow_triples.append((base, exponent, result))
        m.fpu_push(m.checked(result))

    @staticmethod
    def _stub_mod(m: xm.Machine) -> int:
        esp = m.reg(4)
        first = m.read_f32(m.read_int(esp + 4, 4))
        second = m.read_f32(m.read_int(esp + 8, 4))
        if second == 0.0:
            raise xm.Stop("O3", "a remainder by zero")
        m.fpu_push(math.fmod(first, second))
        return 8

    # -- the runs ---------------------------------------------------------------------------

    def run_init(self) -> None:
        first, end = self.runs["init"]
        self.machine.run(first, [end])

    def undefine_registers_and_fpu(self) -> None:
        m = self.machine
        for index in range(8):
            if index not in (4, 5):
                m.undefine(index)
        for flag in m.flags:
            m.flags[flag] = None
        m.tag = [0] * 8
        m.top = 0

    def run_slices(self) -> None:
        for first, end in self.slices:
            self.machine.run(first, [end])

    def run_plane(self) -> int:
        first, _end = self.runs["plane"]
        self.undefine_registers_and_fpu()
        return self.machine.run(first, list(self.exits), self.step_budget)


def flatten_column_major(values, dims: Tuple[int, ...]) -> List:
    """A scalar list, or a list of rows for a rank-2 array, in the Fortran's memory order."""
    if len(dims) == 1:
        return list(values)
    rows, cols = dims
    return [values[r][c] for c in range(cols) for r in range(rows)]


# ---- instrumentation: counters, trip counts and the one isolation hook ---------------------------

class Loop(NamedTuple):
    guard: int
    back: int
    remainder: int
    block: int
    sites: Tuple[int, ...]


def load_loops(oracle_map: OracleMap) -> List[Loop]:
    return [Loop(int(row[0], 16), int(row[1], 16), int(row[2], 16), int(row[3]),
                 tuple(int(token, 16) for token in row[4].split())) for row in oracle_map.rows["trip"]]


def hook_plan(site: Site, ins: xm.Ins, previous: Optional[xm.Ins]) -> Optional[str]:
    """What the alternative to a site's rounding is, or None when the table gives it no hook.

    shadow: the reload of the stored home sees the register's unrounded value (S, T, P, B).
    after-register: the register left behind by a store that does not pop is rounded (X).
    before-copy: the carried copy a preceding `fst st(i)` made is rounded (X, C6).
    after-result: the register the instruction wrote is rounded (R born by an operation).
    all-registers: every occupied register is rounded (R at the exit of a loop whose sum is
    one of them; the table does not name the register)."""
    if site.type != "r4":
        return None
    text = ins.text
    kind = site.kind
    if kind in ("S", "T", "P") or kind.startswith("B"):
        return "shadow" if text.startswith("fst") and "dword" in text else None
    if kind == "X" and text.startswith("fst ") and "dword" in text:
        return "after-register"
    if kind in ("X", "C6") and text.startswith("fstp") and "dword" in text:
        if previous is not None and previous.text.startswith("fst st("):
            return "before-copy"
        return None
    if kind == "R":
        first = text.split()[0]
        if first.startswith("fld") or (first in ("fadd", "fsub", "fsubr", "fmul", "fdiv", "fdivr") and ("st," in text or "ptr" in text)):
            return "after-result"
        if first == "mov":
            return "all-registers"
    return None


def plan_for(machine: xm.Machine, site: Site) -> Tuple[Optional[str], Optional[xm.Ins]]:
    """The hook mode of a site and the instruction before it, from the machine's decoded code."""
    ordered = sorted(machine.code)
    index = ordered.index(site.at)
    previous = machine.code[ordered[index - 1]] if index else None
    return hook_plan(site, machine.code[site.at], previous), previous


class Instrumentation:
    """The counters of a run and, at most, one isolation hook, installed as the machine's hooks."""

    def __init__(self, world: "World", hook_site: Optional[int] = None):
        self.world = world
        self.executions: Dict[int, int] = {}
        self.trips: Dict[int, List[int]] = {loop.guard: [] for loop in world.loops}
        self._open: Dict[int, List[int]] = {}
        self.hook_site = hook_site
        self.hook_mode: Optional[str] = None
        machine = world.machine
        for site in world.sites:
            self._add(machine.before, site.at, self._count(site.at))
        for loop in world.loops:
            self._add(machine.before, loop.guard, self._enter(loop))
            self._add(machine.before, loop.back, self._pass(loop, 0))
            self._add(machine.before, loop.remainder, self._pass(loop, 1))
        if hook_site is not None:
            self._install_hook(hook_site)

    @staticmethod
    def _add(table: Dict[int, Callable], address: int, fn: Callable) -> None:
        held = table.get(address)
        if held is None:
            table[address] = fn
            return

        def both(m: xm.Machine, ins: xm.Ins, first=held, second=fn) -> None:
            first(m, ins)
            second(m, ins)
        table[address] = both

    def _count(self, address: int) -> Callable:
        def count(m: xm.Machine, ins: xm.Ins) -> None:
            self.executions[address] = self.executions.get(address, 0) + 1
        return count

    def _enter(self, loop: Loop) -> Callable:
        def enter(m: xm.Machine, ins: xm.Ins) -> None:
            self.close(loop)
            self._open[loop.guard] = [0, 0]
        return enter

    def _pass(self, loop: Loop, index: int) -> Callable:
        def passed(m: xm.Machine, ins: xm.Ins) -> None:
            self._open[loop.guard][index] += 1
        return passed

    def close(self, loop: Loop) -> None:
        held = self._open.pop(loop.guard, None)
        if held is not None:
            self.trips[loop.guard].append(loop.block * held[0] + held[1])

    def close_all(self) -> None:
        for loop in self.world.loops:
            self.close(loop)

    def _install_hook(self, address: int) -> None:
        world = self.world
        machine = world.machine
        site = next(candidate for candidate in world.sites if candidate.at == address)
        mode, previous = plan_for(machine, site)
        self.hook_mode = mode
        if mode is None:
            return
        held: Dict[str, float] = {}
        if mode == "shadow":
            def before(m: xm.Machine, i: xm.Ins) -> None:
                held["v"] = m.fpu_get(0)

            def after(m: xm.Machine, i: xm.Ins) -> None:
                entry = m.last_store.get(i.address)
                if entry is not None and entry[2] == m.clock:
                    m.shadow[entry[0]] = held["v"]
            self._add(machine.before, address, before)
            self._add(machine.after, address, after)
        elif mode == "after-register":
            def after_register(m: xm.Machine, i: xm.Ins) -> None:
                m.fpu_set(0, to_binary32(m.fpu_get(0)))
            self._add(machine.after, address, after_register)
        elif mode == "before-copy":
            register = int(previous.text[len("fst st("):-1])  # type: ignore[union-attr]

            def before_copy(m: xm.Machine, i: xm.Ins) -> None:
                m.fpu_set(register, to_binary32(m.fpu_get(register)))
            self._add(machine.before, address, before_copy)
        elif mode == "after-result":
            def after_result(m: xm.Machine, i: xm.Ins) -> None:
                m.fpu_set(0, to_binary32(m.fpu_get(0)))
            self._add(machine.after, address, after_result)
        else:
            def all_registers(m: xm.Machine, i: xm.Ins) -> None:
                for position in range(8):
                    if m.tag[(m.top + position) & 7]:
                        m.fpu_set(position, to_binary32(m.fpu_get(position)))
            self._add(machine.before, address, all_registers)


# ---- totals, one cycle's run, read-back -----------------------------------------------------------

class Output(NamedTuple):
    type: str
    dims: Tuple[int, ...]       # () for a scalar
    data: bytes


class CycleRun(NamedTuple):
    cycle: int
    exit: str
    steps: int
    totals: Dict[str, Output]
    outputs: Dict[str, Output]
    io_sites: Tuple[int, ...]
    pow_triples: Tuple[Tuple[float, float, float], ...]
    trips: Dict[int, Tuple[int, ...]]
    executions: Dict[int, int]
    shadow_hits: int


def output_scalars(sites: Sequence[Site]) -> Dict[str, List[Site]]:
    """The scalar names a plane run leaves a value for: the targets of every site that stores,
    except a compiler temporary (T), without an index."""
    names: Dict[str, List[Site]] = {}
    for site in sites:
        if site.kind in ("R", "E", "CMP", "T"):
            continue
        for target in site.target.split(" ; "):
            target = target.strip()
            if "(" in target or not target:
                continue
            names.setdefault(target.lower(), []).append(site)
    return names


def evaluate_totals(world: "World", totals: Totals, overrides: Dict[str, str]) -> Dict[str, Tuple[str, object]]:
    """The cycle's totals by map name: (type, value), in the map's order, each from its expression
    (or the case's override of it) over the setup's keys, the helpers and the totals above it."""
    namespace: Dict[str, object] = dict(world.setup)
    namespace.update(totals.namespace())
    values: Dict[str, Tuple[str, object]] = {}
    for row in world.map.rows["total"]:
        where, name, type_, expression = row[0], row[1], row[2], row[3]
        value = evaluate(overrides.get(name, expression), namespace)
        namespace[name] = value
        values[name] = (type_, value)
        values[name + "@"] = (where, None)
    return values


def inject_totals(world: "World", values: Dict[str, Tuple[str, object]]) -> Dict[str, Output]:
    injected: Dict[str, Output] = {}
    for name, (type_, value) in values.items():
        if name.endswith("@"):
            continue
        where = values[name + "@"][0]
        if where == "array":
            block = world.blocks[name]
            world.inject_array(name, value)
            injected[name.lower()] = Output(block.type, block.dims, world.read_array(name))
        elif where != "-":
            loc = parse_location(where)
            world.inject_scalar(loc, type_, value)
            injected[name.lower()] = Output(type_, (), encode(type_, value))
    world.clear_written()
    return injected


def run_cycle(world: "World", cycle: int, totals: Totals, overrides: Dict[str, str],
              scalar_sites: Dict[str, List[Site]]) -> CycleRun:
    machine = world.machine
    values = evaluate_totals(world, totals, overrides)
    injected = inject_totals(world, values)
    machine.last_store.clear()
    world.watch_homes()
    world.io_sites.clear()
    world.pow_triples.clear()
    world.instrumentation.executions.clear()
    for guard in world.instrumentation.trips:
        world.instrumentation.trips[guard] = []
    hits_before = machine.shadow_hits
    steps_before = machine.steps
    world.run_slices()
    reached = world.run_plane()
    world.instrumentation.close_all()
    outputs: Dict[str, Output] = {}
    for name, candidates in scalar_sites.items():
        best = None
        for site in candidates:
            entry = machine.last_store.get(site.at)
            if entry is None:
                continue
            width = ELEMENT_BYTES[site.type]
            final = (entry[2], site.at, entry[1])
            if site.kind.startswith("B"):
                # an unrolled sum: the remainder passes store the value too, from instructions the
                # table does not list; the last store to its home from its own row is the value
                row = world.row_of(site.at)
                own = [store for store in machine.watch.get(entry[0], []) if world.row_of(store[1]) == row]
                if own:
                    final = own[-1]
            if best is None or final[0] > best[0][0]:
                best = (final, site, width)
        if best is not None:
            final, site, width = best
            outputs[name] = Output(site.type, (), final[2][:width])
    for name in world.written_names():
        block = world.blocks[name]
        outputs[name.lower()] = Output(block.type, block.dims, world.read_array(name))
    return CycleRun(cycle, world.exits[reached], machine.steps - steps_before, injected, outputs,
                    tuple(world.io_sites), tuple(world.pow_triples),
                    {guard: tuple(trips) for guard, trips in world.instrumentation.trips.items()},
                    dict(world.instrumentation.executions), machine.shadow_hits - hits_before)


class CaseRun(NamedTuple):
    setup: Dict[str, object]
    preloop: Dict[str, Dict[str, str]]
    cycles: List[CycleRun]
    init_steps: int
    init_executions: Dict[int, int]
    stop: Optional[str]
    hook_mode: Optional[str]


def case_setup(case: Dict[str, object]) -> Dict[str, object]:
    setup = fs.oracle_setup(FORMULATIONS / str(case["formulation"]), dict(case.get("menu") or {}),
                            case.get("flags"))  # type: ignore[arg-type]
    setup.update(case.get("setup") or {})  # type: ignore[arg-type]
    return setup


def read_preloop(world: "World") -> Dict[str, Dict[str, str]]:
    """What the executed pre-loop left, beside the setup model's own value of the same key. The
    bytes are the answer; a difference is the port's setup to be held to, never a stop."""
    seen: Dict[str, Dict[str, str]] = {}
    for key, name, type_, _what in world.map.rows["expect"]:
        if name in world.map.arrays:
            block = world.blocks[name]
            raw = world.machine.read_bytes(block.base, block.size)
            width = ELEMENT_BYTES[type_]
            seen[key] = {"executed": [hex_bits(type_, raw[at:at + width]) for at in range(0, block.size, width)],
                         "model": [hex_of(type_, value) for value in world.setup[key]]}  # type: ignore[union-attr]
            continue
        loc = next(loc for loc, tenants in world.map.cells.items() if any(t.name == name for t in tenants))
        raw = world.machine.read_bytes(location_address(loc), ELEMENT_BYTES[type_])
        seen[key] = {"executed": hex_bits(type_, raw), "model": hex_of(type_, world.setup[key])}
    return seen


def run_case(case: Dict[str, object], oracle_map: OracleMap, sites: Sequence[Site],
             hook_site: Optional[int] = None, machine_options: Optional[Dict[str, object]] = None,
             step_budget: int = PLANE_STEP_BUDGET) -> CaseRun:
    setup = case_setup(case)
    world = World(setup, oracle_map, sites, machine_options, hook_site)
    world.step_budget = step_budget
    scalar_sites = output_scalars(sites)
    cycles: List[CycleRun] = []
    stop: Optional[str] = None
    preloop: Dict[str, Dict[str, str]] = {}
    init_steps = 0
    init_executions: Dict[int, int] = {}
    try:
        world.run_init()
        init_steps = world.machine.steps
        init_executions = dict(world.instrumentation.executions)
        preloop = read_preloop(world)
        totals = Totals(int(case["seed"]))  # type: ignore[arg-type]
        for cycle in range(int(case["max_cycles"])):  # type: ignore[arg-type]
            run = run_cycle(world, cycle, totals, dict(case.get("totals") or {}), scalar_sites)  # type: ignore[arg-type]
            cycles.append(run)
            if run.exit == "print":
                break
    except xm.Stop as error:
        stop = str(error)
    return CaseRun(setup, preloop, cycles, init_steps, init_executions, stop, world.instrumentation.hook_mode)


# ---- the fixture's encoding ---------------------------------------------------------------------

def encode_output(output: Output) -> Dict[str, object]:
    """A scalar as its bits (most significant byte first), an array as its bytes, zlib-compressed
    and base64 (`dims` column-major, the Fortran's order, the first the fastest)."""
    if not output.dims:
        return {"type": output.type, "bits": hex_bits(output.type, output.data)}
    packed = base64.b64encode(zlib.compress(output.data, 9)).decode("ascii")
    return {"type": output.type, "dims": list(output.dims), "z": packed}


def decode_output(entry: Dict[str, object]) -> Output:
    kind = str(entry["type"])
    if "bits" in entry:
        return Output(kind, (), bytes.fromhex(str(entry["bits"]))[::-1])
    return Output(kind, tuple(entry["dims"]), zlib.decompress(base64.b64decode(str(entry["z"]))))  # type: ignore[arg-type]


def setup_json(setup: Dict[str, object]) -> Dict[str, object]:
    return {key: value for key, value in setup.items()}


def pow_hex(triples: Sequence[Tuple[float, float, float]]) -> List[List[str]]:
    return [[hex_of("r8", base), hex_of("r8", exponent), hex_of("r8", result)] for base, exponent, result in triples]


def case_json(case: Dict[str, object], run: CaseRun, isolated: Dict[int, List[Tuple[int, str]]]) -> Dict[str, object]:
    cycles = []
    for cycle in run.cycles:
        cycles.append({
            "cycle": cycle.cycle, "exit": cycle.exit, "steps": cycle.steps,
            "io_sites": [f"0x{address:06X}" for address in cycle.io_sites],
            "pow": pow_hex(cycle.pow_triples),
            "trips": {f"0x{guard:06X}": list(trips) for guard, trips in cycle.trips.items()},
            "totals": {name: encode_output(output) for name, output in sorted(cycle.totals.items())},
            "outputs": {name: encode_output(output) for name, output in sorted(cycle.outputs.items())},
        })
    return {
        "id": case["id"], "purpose": case.get("purpose", ""), "formulation": case["formulation"],
        "menu": case.get("menu") or {}, "flags": case.get("flags"), "setup_overrides": case.get("setup") or {},
        "seed": case["seed"], "max_cycles": case["max_cycles"], "totals_overrides": case.get("totals") or {},
        "setup": setup_json(run.setup), "preloop": run.preloop, "init_steps": run.init_steps,
        "cycles": cycles,
        "isolated": {f"0x{site:06X}": [[cycle, name] for cycle, name in moves] for site, moves in sorted(isolated.items())},
    }


# ---- the cases the search draws ------------------------------------------------------------------

def formulation_names() -> List[str]:
    return sorted(path.name for path in FORMULATIONS.glob("*.dat"))


def draw_value(rng: random.Random, law: str, arguments: Sequence[str]):
    if law == "uniform":
        return round(rng.uniform(float(arguments[0]), float(arguments[1])), int(arguments[2]))
    if law == "loguniform":
        return 10 ** rng.uniform(float(arguments[0]), float(arguments[1]))
    if law == "choice":
        return rng.choice([int(item) if item.lstrip("-").isdigit() else float(item) for item in arguments])
    raise OracleError(f"no law {law}")


def draw_case(rng: random.Random, index: int, oracle_map: OracleMap, formulation: Optional[str] = None,
              purpose: str = "draw") -> Dict[str, object]:
    """One candidate: a formulation and what the map's `draw` rows say to draw, from one seeded
    generator. Every number is drawn or the map's, none is the executable's. A `formulation`
    given (a `pin` row of the map) replaces the drawn choice, which the generator still makes, so
    the rest of the draw is the one the choice would have led to."""
    drawn = rng.choice(formulation_names())
    case: Dict[str, object] = {"id": f"c{index:03d}", "purpose": purpose, "formulation": formulation or drawn,
                               "menu": {}, "flags": None, "setup": {}, "totals": {}}
    base = fs.oracle_setup(FORMULATIONS / str(case["formulation"]))
    for where, names, presence, law, arguments in oracle_map.rows["draw"]:
        present = rng.random() < float(presence)
        if law == "flags":
            if present:
                count = len(base[names])  # type: ignore[arg-type]
                flags = [1 if rng.random() < float(arguments) else 0 for _ in range(count)]
                flags[rng.randrange(count)] = 1
                case["flags"] = flags
            continue
        value = draw_value(rng, law, arguments.split())
        if where == "cycles":
            case[names] = value
        elif present:
            for name in names.split():
                case[where][name] = value  # type: ignore[index]
    case["seed"] = rng.randrange(1 << 31)
    return case


def digest(cycle: CycleRun) -> Dict[str, str]:
    """What a cycle shows: every output's bytes and the warnings the formatted writes raised."""
    shown = {name: hashlib.md5(output.data).hexdigest() for name, output in cycle.outputs.items()}
    shown["@writes"] = ",".join(f"{address:06X}" for address in cycle.io_sites)
    return shown


class IsolationResult(NamedTuple):
    site: int
    mode: Optional[str]
    stop: Optional[str]
    shadow_hits: int
    executed: int
    digests: List[Dict[str, str]]


_WORKER: Dict[str, object] = {}


def _worker_state() -> Tuple[OracleMap, List[Site]]:
    if "map" not in _WORKER:
        _WORKER["map"] = load_map()
        _WORKER["sites"] = load_sites()
    return _WORKER["map"], _WORKER["sites"]  # type: ignore[return-value]


def isolation_task(arguments: Tuple[Dict[str, object], Optional[int]]) -> IsolationResult:
    case, site = arguments
    oracle_map, sites = _worker_state()
    run = run_case(case, oracle_map, sites, hook_site=site)
    address = site if site is not None else -1
    executed = run.init_executions.get(address, 0) + sum(cycle.executions.get(address, 0) for cycle in run.cycles)
    return IsolationResult(address, run.hook_mode, run.stop, sum(cycle.shadow_hits for cycle in run.cycles),
                           executed, [digest(cycle) for cycle in run.cycles])


# ---- coverage and the search -----------------------------------------------------------------------

class Coverage:
    """What the cases reach, measured from the runs, keyed by the generated table's addresses."""

    def __init__(self, sites: Sequence[Site], loops: Sequence[Loop], oracle_map: OracleMap):
        self.sites = list(sites)
        self.warnings = list(oracle_map.rows["warning"])
        self.feature_rows = list(oracle_map.rows["feature"])
        self.loops = list(loops)
        self.executions: Dict[int, int] = {}
        self.residues: Dict[int, Dict[int, List[str]]] = {loop.guard: {} for loop in loops}
        self.short: Dict[int, List[str]] = {loop.guard: [] for loop in loops}
        self.features: Dict[str, List[str]] = {}

    def feature(self, name: str, case_id: str) -> None:
        held = self.features.setdefault(name, [])
        if case_id not in held:
            held.append(case_id)

    def add(self, case: Dict[str, object], run: CaseRun) -> int:
        """Fold one case's run in; return how many entries it added that were not there."""
        before = self.size()
        case_id = str(case["id"])
        for address, count in run.init_executions.items():
            self.executions[address] = self.executions.get(address, 0) + count
        for cycle in run.cycles:
            for address, count in cycle.executions.items():
                self.executions[address] = self.executions.get(address, 0) + count
            self.feature(f"cycle {cycle.cycle}", case_id)
            self.feature(f"exit {cycle.exit}", case_id)
            for loop in self.loops:
                for trips in cycle.trips.get(loop.guard, ()):
                    if trips >= loop.block:
                        held = self.residues[loop.guard].setdefault(trips % loop.block, [])
                        if case_id not in held:
                            held.append(case_id)
                    elif case_id not in self.short[loop.guard]:
                        self.short[loop.guard].append(case_id)
            if cycle.exit != "warm-up":
                self.record_cycle_features(case, cycle)
        return self.size() - before

    def record_cycle_features(self, case: Dict[str, object], cycle: CycleRun) -> None:
        case_id = str(case["id"])
        for label, kind, name in self.feature_rows:
            if kind == "menu-nonzero" and (case.get("menu") or {}).get(name):  # type: ignore[union-attr]
                self.feature(label, case_id)
            if kind == "output-nonzero" and cycle.outputs.get(name) is not None and any(cycle.outputs[name].data):
                self.feature(label, case_id)
            if kind == "case-has-zero" and 0 in (case.get(name) or []):  # type: ignore[operator]
                self.feature(label, case_id)
        for compare, call, _what in self.warnings:
            if cycle.executions.get(int(compare, 16)):
                raised = int(call, 16) in cycle.io_sites
                self.feature(f"warning at {compare} {'raised' if raised else 'clear'}", case_id)

    def size(self) -> int:
        return (sum(len(v) > 0 for r in self.residues.values() for v in r.values())
                + sum(len(v) > 0 for v in self.short.values()) + len(self.features))


def residue_gaps(coverage: Coverage) -> List[Tuple[int, int]]:
    """(guard, residue) pairs no case has reached yet."""
    gaps = []
    for loop in coverage.loops:
        for residue in range(loop.block):
            if not coverage.residues[loop.guard].get(residue):
                gaps.append((loop.guard, residue))
    return gaps


class Kept(NamedTuple):
    case: Dict[str, object]
    run: CaseRun
    isolated: Dict[int, List[Tuple[int, str]]]


def run_pool(function: Callable, tasks: Sequence, workers: int) -> List:
    """Map in a spawn pool (a Windows process cannot fork); one process when `workers` is 1."""
    if workers <= 1:
        return [function(task) for task in tasks]
    context = multiprocessing.get_context("spawn")
    with context.Pool(workers) as pool:
        return list(pool.imap(function, tasks, chunksize=1))


def baseline_task(case: Dict[str, object]) -> CaseRun:
    oracle_map, sites = _worker_state()
    return run_case(case, oracle_map, sites)


def moves_between(base: CaseRun, other: IsolationResult) -> List[Tuple[int, str]]:
    moves: List[Tuple[int, str]] = []
    base_digests = [digest(cycle) for cycle in base.cycles]
    for cycle, hooked in enumerate(other.digests):
        if cycle >= len(base_digests):
            break
        for name in sorted(set(base_digests[cycle]) | set(hooked)):
            if base_digests[cycle].get(name) != hooked.get(name):
                moves.append((cycle, name))
    return moves


def hookable_sites(sites: Sequence[Site]) -> List[int]:
    oracle_map = load_map()
    case = {"id": "plan", "formulation": formulation_names()[0], "seed": 0,
            "max_cycles": 1, "totals": {}, "menu": {}, "flags": None, "setup": {}}
    world = World(case_setup(case), oracle_map, sites)
    return [site.at for site in sites if plan_for(world.machine, site)[0] is not None]


class SearchState:
    """Everything the search holds between candidates, so that a long search can stop and resume."""

    def __init__(self, seed: int, hookable: Sequence[int], coverage: "Coverage"):
        self.rng_state = random.Random(seed).getstate()
        self.index = 0
        self.pending: List[Tuple[Dict[str, object], CaseRun]] = []
        self.isolation_count: Dict[int, int] = {address: 0 for address in hookable}
        self.terminal: Dict[int, str] = {}
        self.coverage = coverage
        self.kept: List[Kept] = []
        self.counters: Dict[str, int] = {"candidates": 0, "stopped": 0}
        self.hook_stops: List[str] = []
        self.pins_done = 0


def isolate_candidate(state: SearchState, case: Dict[str, object], run: CaseRun, hookable: Sequence[int],
                      workers: int) -> Dict[int, List[Tuple[int, str]]]:
    """Hook every site that still needs an isolating case, one run each, and return the moves of
    those whose hook moved an output. A site whose hooked value was never read back is terminal."""
    isolated: Dict[int, List[Tuple[int, str]]] = {}
    need = [address for address in hookable if state.isolation_count[address] < 2 and address not in state.terminal]
    if not need:
        return isolated
    hooked_case = dict(case, max_cycles=min(int(case["max_cycles"]), 2))  # type: ignore[call-overload]
    results = run_pool(isolation_task, [(hooked_case, address) for address in need], workers)
    base = baseline_for_hook(run, hooked_case)
    for result in results:
        if result.stop is not None:
            state.hook_stops.append(f"{case['id']} {result.site:06X} {result.stop[:80]}")
            continue
        moves = moves_between(base, result)
        if moves:
            isolated[result.site] = moves
            state.isolation_count[result.site] += 1
        elif result.mode == "shadow" and result.executed and not result.shadow_hits:
            state.terminal[result.site] = (f"executed {result.executed} times in {case['id']}; the hooked value was "
                                           "never read back, so nothing consumes the rounding")
    return isolated


PIN_ATTEMPTS = 8


def run_pins(oracle_map: OracleMap, seed: int, first: int, draws: int, workers: int,
             log: Callable[[str], None]) -> List[Tuple[Dict[str, object], CaseRun]]:
    """The cases of the map's `pin` rows from the `first`-th on, each with its baseline run. A pin is
    drawn like any candidate (`draw_case`) from a generator of its own, named by `seed`, its place
    among the pins and an attempt, its formulation the row's, its number following the `draws`
    candidates the search drew. A draw whose run stops (a small cell size makes the plane's arrays
    too large for the step budget) is not kept, as the search drops a candidate that stops: all
    `PIN_ATTEMPTS` attempts of a pin run together and the lowest that does not stop is the pin's
    case, so the choice never depends on timing. A pin none of whose attempts runs is an error."""
    chosen: List[Tuple[Dict[str, object], CaseRun]] = []
    for place, (formulation, _key, _why) in enumerate(oracle_map.rows.get("pin", [])):
        if place < first:
            continue
        attempts = [draw_case(random.Random(f"{seed}/pin/{place}/{attempt}"), draws + place, oracle_map, formulation,
                              "pin") for attempt in range(PIN_ATTEMPTS)]
        runs = run_pool(baseline_task, attempts, workers)
        survivors = [(case, run) for case, run in zip(attempts, runs) if run.stop is None]
        if not survivors:
            raise OracleError(f"pin {formulation}: every one of {PIN_ATTEMPTS} attempts stops: {runs[0].stop}")
        chosen.append(survivors[0])
        log(f"{survivors[0][0]['id']} {formulation} pinned at attempt {attempts.index(survivors[0][0])}")
    return chosen


def keep_pins(state: SearchState, oracle_map: OracleMap, seed: int, workers: int, log: Callable[[str], None]) -> None:
    """After the draws: one case per `pin` row of the map, kept whatever coverage it adds (`run_pins`).
    A pin is not isolated: it is there for what the pre-loop and the plane compute on that
    formulation."""
    for case, run in run_pins(oracle_map, seed, state.pins_done, state.index, workers, log):
        gained = state.coverage.add(case, run)
        state.kept.append(Kept(case, run, {}))
        state.counters["pinned"] = state.counters.get("pinned", 0) + 1
        state.pins_done += 1
        log(f"{case['id']} {case['formulation']} cycles={len(run.cycles)} gained {gained}")


def search(seed: int, isolation_draws: int, coverage_draws: int, workers: int, log: Callable[[str], None],
           state_path: Optional[Path] = None, budget_seconds: float = 1e9) -> Tuple[SearchState, bool]:
    """Draw candidates, keep those that isolate a site that still needs one (two per site at most)
    or add coverage. Resumable: the state is written after every candidate to `state_path`; the
    search returns (state, finished) once the budget of seconds is used or the draws are done."""
    started = time.time()
    oracle_map = load_map()
    sites = load_sites()
    hookable = hookable_sites(sites)
    if state_path is not None and state_path.exists():
        with state_path.open("rb") as handle:
            state = pickle.load(handle)
    else:
        state = SearchState(seed, hookable, Coverage(sites, load_loops(oracle_map), oracle_map))
    rng = random.Random()
    total = isolation_draws + coverage_draws
    while True:
        if not state.pending:
            if state.index >= total:
                keep_pins(state, oracle_map, seed, workers, log)
                return state, True
            rng.setstate(state.rng_state)
            batch = min(workers, total - state.index)
            cases = [draw_case(rng, state.index + k, oracle_map) for k in range(batch)]
            state.index += batch
            state.rng_state = rng.getstate()
            state.pending = list(zip(cases, run_pool(baseline_task, cases, workers)))
        case, run = state.pending.pop(0)
        state.counters["candidates"] += 1
        if run.stop is not None:
            state.counters["stopped"] += 1
            code = run.stop.split()[0]
            state.counters["stop " + code] = state.counters.get("stop " + code, 0) + 1
            log(f"{case['id']} {case['formulation']}: {run.stop[:100]}")
        else:
            isolated = {}
            if state.counters["candidates"] <= isolation_draws:
                isolated = isolate_candidate(state, case, run, hookable, workers)
            gained = state.coverage.add(case, run)
            if isolated or gained:
                state.kept.append(Kept(case, run, isolated))
            log(f"{case['id']} {case['formulation']} cycles={len(run.cycles)} steps={sum(c.steps for c in run.cycles)} "
                f"isolates {len(isolated)} gained {gained}; isolated sites so far "
                f"{sum(1 for v in state.isolation_count.values() if v)}/{len(hookable)}, terminal {len(state.terminal)}")
        if state_path is not None:
            with state_path.open("wb") as handle:
                pickle.dump(state, handle)
        if time.time() - started >= budget_seconds:
            return state, state.index >= total and not state.pending


def baseline_for_hook(run: CaseRun, hooked_case: Dict[str, object]) -> CaseRun:
    keep = int(hooked_case["max_cycles"])  # type: ignore[arg-type]
    return run._replace(cycles=run.cycles[:keep])


# ---- provenance, the fixture, `--verify` -----------------------------------------------------------

FIXTURE_FORMAT = 1
FIXTURE_FORTRAN_LINES = "378, 718, 771-1175"   # what the fixture answers for: the pre-loop sums, the plane
FIXTURE_SCRIPT = "cycle_plane_oracle.py"
#: The scripts whose bytes the fixture depends on: the machine, this oracle, and the twin whose
#: `oracle_setup` supplies every value the oracle injects (`formulas_particle.py`, which the twin
#: imports for `dmax`, reaches no injected value).
DIGESTED_SCRIPTS = ("x87_machine.py", "cycle_plane_oracle.py", "formulas_statistics.py")
DIGEST_KEYS = ("executable_sha256", "excerpt_sha256", "map_sha256", "site_table_sha256", "scripts_sha256")


def sha256_of(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git_head() -> str:
    try:
        return subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT, capture_output=True, text=True,
                              check=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def provenance(seed: int, isolation_draws: int, coverage_draws: int) -> Dict[str, object]:
    return {
        "executable_sha256": lc.sha256(lc.exe_path()), "excerpt_sha256": sha256_of(lc.listing_path()),
        "map_sha256": sha256_of(MAP_PATH), "site_table_sha256": sha256_of(TABLE_PATH),
        "scripts_sha256": {name: sha256_of(HERE / name) for name in DIGESTED_SCRIPTS},
        "seed": seed, "isolation_draws": isolation_draws, "coverage_draws": coverage_draws,
        "git_head": git_head(),
    }


def site_status(site: Site, executions: int, isolating: List[str], mode: Optional[str],
                terminal: str) -> Dict[str, object]:
    if isolating:
        status, reason = "isolated", ""
    elif executions == 0:
        status, reason = "unreached", "never executed by any case"
    elif mode is None:
        status, reason = "no-hook", f"the table gives a {site.kind} site of type {site.type} no alternative to its rounding"
    elif terminal:
        status, reason = "terminal", terminal + "; the stored value is itself compared as the output it is"
    else:
        status, reason = "unreached", f"executed {executions} times; the {mode} hook moved no compared output"
    return {"at": f"0x{site.at:06X}", "row": site.row, "kind": site.kind, "type": site.type, "target": site.target,
            "executions": executions, "hook": mode, "isolated_by": isolating, "status": status, "reason": reason}


def replay_task(arguments: Tuple[Dict[str, object], Optional[int]]) -> Tuple[CaseRun, Optional[IsolationResult]]:
    """Run one case afresh (no hook) or, with a site, its hooked run."""
    case, site = arguments
    oracle_map, sites = _worker_state()
    if site is None:
        return run_case(case, oracle_map, sites), None
    hooked_case = dict(case, max_cycles=min(int(case["max_cycles"]), 2))  # type: ignore[call-overload]
    return None, isolation_task((hooked_case, site))  # type: ignore[return-value]


def replay(plan: Sequence[Tuple[Dict[str, object], List[int]]], workers: int) -> List[Kept]:
    """Every case once without a hook and once per recorded isolating site, all in one pool; the
    hooked runs are compared with the case's own baseline."""
    tasks: List[Tuple[Dict[str, object], Optional[int]]] = []
    for case, hooked in plan:
        tasks.append((case, None))
        tasks.extend((case, address) for address in hooked)
    results = run_pool(replay_task, tasks, workers)
    kept: List[Kept] = []
    position = 0
    for case, hooked in plan:
        run = results[position][0]
        isolated: Dict[int, List[Tuple[int, str]]] = {}
        base = baseline_for_hook(run, dict(case, max_cycles=min(int(case["max_cycles"]), 2)))             if run.stop is None else run  # type: ignore[call-overload]
        for offset, address in enumerate(hooked, start=1):
            result = results[position + offset][1]
            moves = moves_between(base, result) if run.stop is None and result.stop is None else []
            if moves:
                isolated[address] = moves
        kept.append(Kept(case, run, isolated))
        position += 1 + len(hooked)
    return kept


def build_fixture(kept_cases: Sequence[Kept], counters: Dict[str, int], seed: int, isolation_draws: int,
                  coverage_draws: int, terminal: Dict[int, str]) -> Dict[str, object]:
    """The fixture from the replayed cases; the coverage is measured over those cases alone, so the
    table claims only what the fixture holds."""
    oracle_map = load_map()
    sites = load_sites()
    coverage = Coverage(sites, load_loops(oracle_map), oracle_map)
    for kept in kept_cases:
        coverage.add(kept.case, kept.run)
    template = {"id": "plan", "formulation": formulation_names()[0], "seed": 0, "max_cycles": 1}
    world = World(case_setup(template), oracle_map, sites)
    modes = {site.at: plan_for(world.machine, site)[0] for site in sites}
    isolating: Dict[int, List[str]] = {site.at: [] for site in sites}
    for kept in kept_cases:
        for address in kept.isolated:
            isolating[address].append(str(kept.case["id"]))
    loops_json = {}
    for loop in coverage.loops:
        loops_json[f"0x{loop.guard:06X}"] = {
            "block": loop.block, "sites": [f"0x{at:06X}" for at in loop.sites],
            "residues": {str(r): coverage.residues[loop.guard].get(r, []) for r in range(loop.block)},
            "short": coverage.short[loop.guard],
            "unreached_residues": [r for r in range(loop.block) if not coverage.residues[loop.guard].get(r)],
        }
    return {
        "format": FIXTURE_FORMAT,
        "fortranLines": FIXTURE_FORTRAN_LINES,
        "script": FIXTURE_SCRIPT,
        "provenance": provenance(seed, isolation_draws, coverage_draws),
        "counters": counters,
        "cases": [case_json(kept.case, kept.run, kept.isolated) for kept in kept_cases],
        "coverage": {"features": coverage.features, "loops": loops_json},
        "sites": [site_status(site, coverage.executions.get(site.at, 0), isolating[site.at], modes[site.at],
                              terminal.get(site.at, "")) for site in sites],
    }


TERMINAL_SUFFIX = "; the stored value is itself compared as the output it is"


def extend_fixture(path: Path, workers: int, log: Callable[[str], None]) -> Dict[str, object]:
    """The fixture with the cases of the map's `pin` rows it does not yet hold added after its own, from
    its recorded seed and draw counts, and nothing else changed: the cases it holds are replayed with
    their recorded isolations, the pins run as `run_pins` runs them (a pin is never isolated), and the
    coverage, the site statuses and the provenance are rebuilt over all of them. A full `generate`
    keeps the pins after the draws the same way (`keep_pins`), so this is the fixture it would write
    for the same seed, reached without the search's hours, and `verify` holds every case of it."""
    fixture = json.loads(path.read_text(encoding="utf-8"))
    recorded = fixture["provenance"]
    oracle_map = load_map()
    held = [entry["formulation"] for entry in fixture["cases"] if entry["purpose"] == "pin"]
    wanted = [row[0] for row in oracle_map.rows.get("pin", [])]
    if held != wanted[:len(held)]:
        raise OracleError(f"the fixture's pins {held} are not the first of the map's {wanted}")
    draws = recorded["isolation_draws"] + recorded["coverage_draws"]
    plan = [(case_of_json(entry), [int(address, 16) for address in entry["isolated"]]) for entry in fixture["cases"]]
    pinned = run_pins(oracle_map, recorded["seed"], len(held), draws, workers, log)
    kept = replay(plan, workers) + [Kept(case, run, {}) for case, run in pinned]
    added = pinned
    counters = dict(fixture["counters"])
    counters["pinned"] = counters.get("pinned", 0) + len(added)
    terminal = {int(site["at"], 16): str(site["reason"]).removesuffix(TERMINAL_SUFFIX)
                for site in fixture["sites"] if site["status"] == "terminal"}
    return build_fixture(kept, counters, recorded["seed"], recorded["isolation_draws"], recorded["coverage_draws"],
                         terminal)


def restamp_fixture(path: Path) -> Dict[str, object]:
    """The fixture with its provenance digests brought to the bytes of today and nothing else changed:
    not a case, not an isolation, not the seed, the draw counts or the git head it was generated at.
    For an edit of a digested script that moves no answer (a file added to the digests, a comment); the
    claim that no answer moved is `verify`'s to make, replaying every case, and is not made here."""
    fixture = json.loads(path.read_text(encoding="utf-8"))
    recorded = fixture["provenance"]
    now = provenance(recorded["seed"], recorded["isolation_draws"], recorded["coverage_draws"])
    for key in DIGEST_KEYS:
        recorded[key] = now[key]
    return fixture


def write_fixture(fixture: Dict[str, object], path: Path = FIXTURE_PATH) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(fixture, indent=1, sort_keys=False) + "\n", encoding="utf-8", newline="\n")


def case_of_json(entry: Dict[str, object]) -> Dict[str, object]:
    return {"id": entry["id"], "purpose": entry["purpose"], "formulation": entry["formulation"],
            "menu": entry["menu"], "flags": entry["flags"], "setup": entry["setup_overrides"],
            "seed": entry["seed"], "max_cycles": entry["max_cycles"], "totals": entry["totals_overrides"]}


def verify_fixture(path: Path, workers: int, log: Callable[[str], None]) -> List[str]:
    """Replay every recorded case and isolation; the problems found, none when the fixture is what
    the bytes and the map produce today."""
    fixture = json.loads(path.read_text(encoding="utf-8"))
    problems: List[str] = []
    for key, expected in (("fortranLines", FIXTURE_FORTRAN_LINES), ("script", FIXTURE_SCRIPT)):
        if fixture.get(key) != expected:
            problems.append(f"{key}: recorded {fixture.get(key)!r}, the script writes {expected!r}")
    recorded = fixture["provenance"]
    now = provenance(recorded["seed"], recorded["isolation_draws"], recorded["coverage_draws"])
    for key in DIGEST_KEYS:
        if recorded[key] != now[key]:
            problems.append(f"provenance {key}: recorded {recorded[key]}, now {now[key]}")
    pinned = [entry["formulation"] for entry in fixture["cases"] if entry["purpose"] == "pin"]
    wanted = [row[0] for row in load_map().rows.get("pin", [])]
    if pinned != wanted:
        problems.append(f"pins: the map pins {wanted}, the fixture holds cases for {pinned}")
    plan = [(case_of_json(entry), [int(address, 16) for address in entry["isolated"]]) for entry in fixture["cases"]]
    for entry, kept in zip(fixture["cases"], replay(plan, workers)):
        replayed = case_json(kept.case, kept.run, kept.isolated)
        if replayed != entry:
            problems.append(f"case {entry['id']}: the replay differs from the fixture")
        log(f"{entry['id']}: " + ("ok" if replayed == entry else "DIFFERS"))
    return problems


# ---- the guard: no map name and no plane literal in the oracle's own code ------------------------------

TRIVIAL_LITERALS = {0.5}


def literal_values(text: str) -> List[float]:
    found = []
    for match in re.finditer(r"(?<![\w.])(\d+\.\d*(?:[eE][-+]?\d+)?|\.\d+(?:[eE][-+]?\d+)?|\d+[eE][-+]?\d+)", text):
        found.append(float(match.group(1)))
    return found


def plane_literals() -> List[float]:
    """The numeric literals of the plane: those of the generated table's order column and of the
    Fortran source's lines 771-1180, as written and as binary32 holds them, without the
    trivial ones (small integers, a half) a program cannot be asked to avoid."""
    values: List[float] = []
    for site in load_sites():
        values.extend(literal_values(site.order))
    lines = fortran_path().read_text(encoding="utf-8", errors="replace").split("\n")
    for line in lines[770:1180]:
        values.extend(literal_values(line.split("!")[0]))
    wanted = set()
    for value in values:
        for candidate in (value, to_binary32(value) if abs(value) < 3e38 else value):
            if candidate == int(candidate) and abs(candidate) <= 10:
                continue
            if candidate in TRIVIAL_LITERALS:
                continue
            wanted.add(candidate)
    return sorted(wanted)


def map_names(oracle_map: OracleMap) -> List[str]:
    """Every name the plane's Fortran gives a cell, an array, a total or a setup value, lower-cased:
    the names of the map's cells, arrays and `oracle` rows, of the generated table's targets and of
    the setup values. The map's own vocabulary (its row kinds, a draw's `where`) is not among them."""
    names = set()
    for tenants in oracle_map.cells.values():
        names.update(tenant.name.lower() for tenant in tenants)
    names.update(name.lower() for name in oracle_map.arrays)
    positions = {"total": (1,), "inject": (1,), "pointer": (1,), "array": (0,), "expect": (0, 1)}
    for kind, indexes in positions.items():
        for row in oracle_map.rows.get(kind, []):
            names.update(row[index].lower() for index in indexes)
    for _label, kind, called in oracle_map.rows.get("feature", []):
        if kind == "output-nonzero":
            names.add(called.lower())
    for where, called, *_rest in oracle_map.rows.get("draw", []):
        if where != "cycles":
            names.update(token.lower() for token in called.split())
    for site in load_sites():
        for target in site.target.split(" ; "):
            target = target.strip().split("(")[0].lower()
            if target:
                names.add(target)
    names.update(key.lower() for key in fs.oracle_setup(FORMULATIONS / formulation_names()[0]))
    return sorted(name for name in names if len(name) > 1)


def guard_violations(source: str, label: str, names: Sequence[str], literals: Sequence[float]) -> List[str]:
    """Every string constant that is a map name and every float constant that is a plane literal in
    `source`, docstrings left out (prose)."""
    tree = ast.parse(source)
    docstrings = set()
    for node in ast.walk(tree):
        if isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef)):
            body = node.body
            if body and isinstance(body[0], ast.Expr) and isinstance(body[0].value, ast.Constant) \
                    and isinstance(body[0].value.value, str):
                docstrings.add(id(body[0].value))
    name_set = set(names)
    problems = []
    for node in ast.walk(tree):
        if not isinstance(node, ast.Constant) or id(node) in docstrings:
            continue
        value = node.value
        if isinstance(value, str) and value.strip().lower() in name_set:
            problems.append(f"{label}:{node.lineno}: the string {value!r} is a name of the map")
        if isinstance(value, float) and any(math.isclose(value, literal, rel_tol=1e-12) for literal in literals):
            problems.append(f"{label}:{node.lineno}: the number {value!r} is a literal of the plane")
    return problems


def guard() -> List[str]:
    names = map_names(load_map())
    literals = plane_literals()
    problems: List[str] = []
    for path in (HERE / "x87_machine.py", Path(__file__).resolve()):
        problems.extend(guard_violations(path.read_text(encoding="utf-8"), path.name, names, literals))
    return problems


# ---- the oracle's own proofs ---------------------------------------------------------------------------

JZZ2_CONTROL_DRAWS = 120


def jzz2_runs(oracle_map: OracleMap, sites: Sequence[Site], draws: int = JZZ2_CONTROL_DRAWS) -> Iterator[tuple]:
    """The JZZ = 2 draws (`fs.jzz2_control_draw`) run through the executable's pre-loop: one
    (`setup` overrides, `check`, what the pre-loop left by key) per draw. Drawn from a generator named by
    a string: the same loops every run, every number the draw's or the machine's."""
    rng = random.Random("jzz2-controls")
    for index in range(draws):
        formulation, overrides, check = fs.jzz2_control_draw(rng)
        case = {"id": f"jzz2-{index}", "purpose": "selftest", "formulation": formulation, "menu": {},
                "flags": None, "setup": overrides, "seed": 1, "max_cycles": 1, "totals": {}}
        world = World(case_setup(case), oracle_map, sites)
        world.run_init()
        yield overrides, check, read_preloop(world)


def jzz2_controls(oracle_map: OracleMap, sites: Sequence[Site], draws: int = JZZ2_CONTROL_DRAWS) -> Dict[str, object]:
    """The JZZ = 2 loop of the pre-loop on drawn loops of one to sixteen fractions: the executable's own
    DOKM and DOKSD against the listing's rules and against each rule with one store turned off. The
    answer to the first is zero differences, to each of the others at least one, so the sample is of the
    size of the effect (`src/Statistics/ACCEPTANCE.md`, A12)."""
    listing = 0
    variants: Dict[str, int] = {}
    for _overrides, check, seen in jzz2_runs(oracle_map, sites, draws):
        for name, differs in check({k: v["executed"] for k, v in seen.items()}).items():
            if name == fs.JZZ2_LISTING:
                listing += differs
            else:
                variants[name] = variants.get(name, 0) + differs
    return {"draws": draws, "listing": listing, "variants": variants}


def selftest() -> int:
    """The oracle's checks, each seen red on the violation it guards: the guard on a typed plane
    literal and on a typed map name; the map's address contract on one home moved by four bytes (r3);
    the pow-triple assertion on the exp-log rule (r5)."""
    problems: List[str] = []

    def expect(name: str, ok: bool, detail: str = "") -> None:
        print(f"selftest: {'green' if ok else 'RED'} {name}{': ' + detail if detail else ''}")
        if not ok:
            problems.append(name)

    oracle_map = load_map()
    names = map_names(oracle_map)
    literals = plane_literals()
    expect("the guard finds nothing in the oracle's own files", not guard(), "; ".join(guard()[:2]))
    expect("the guard is red on a typed plane literal", bool(guard_violations("x = 3.14159\n", "typed", names, literals)))
    expect("the guard is red on a typed map name",
           bool(guard_violations(f"y = {names[0]!r}\n", "typed", names, literals)), names[0])

    sites = load_sites()
    case = {"id": "selftest", "purpose": "selftest", "formulation": formulation_names()[0], "menu": {}, "flags": None,
            "setup": {}, "seed": 1, "max_cycles": 2, "totals": {}}
    base = run_case(case, oracle_map, sites)
    expect("the baseline case runs to the print exit", base.stop is None and base.cycles[-1].exit == "print",
           str(base.stop))

    text = MAP_PATH.read_text(encoding="utf-8")
    moved = None
    for raw in text.split("\n"):
        fields = [field.strip() for field in raw.split("|")]
        if len(fields) > 3 and fields[0] == "oracle" and fields[1] == "inject" and fields[4] == "r4" \
                and fields[2].startswith("0x"):
            moved = raw.replace(fields[2], f"0x{int(fields[2], 16) + 4:X}", 1)
            original = raw
            break
    assert moved is not None
    broken = parse_map(text.replace(original, moved, 1))
    redone = run_case(case, broken, sites)
    differs = redone.stop is not None or [c.outputs for c in redone.cycles] != [c.outputs for c in base.cycles]
    expect("r3: one home moved by four bytes stops the run or moves an output", differs,
           redone.stop or "the outputs moved")

    for formulation, key, _why in oracle_map.rows.get("pin", []):
        pinned = {"id": "selftest", "purpose": "pin", "formulation": formulation, "menu": {}, "flags": None,
                  "setup": {}, "seed": 1, "max_cycles": 1, "totals": {}}
        world = World(case_setup(pinned), oracle_map, sites)
        world.run_init()
        executed = read_preloop(world)
        ok, what, detail = fs.pin_check(key, FORMULATIONS / formulation, {k: v["executed"] for k, v in executed.items()})
        expect(f"the pin {formulation} {what}", ok, detail)

    controls = jzz2_controls(oracle_map, sites)
    expect(f"the JZZ = 2 stores of the listing equal the executable's on {controls['draws']} drawn loops of 1 to 16 fractions",
           controls["listing"] == 0, f"{controls['listing']} differ")
    for rule, mismatches in controls["variants"].items():
        expect(f"red: {rule} differs from the executable on a drawn loop", mismatches > 0,
               f"{mismatches} of {controls['draws']}")

    triples = [triple for cycle in base.cycles for triple in cycle.pow_triples]
    agree = all(math.pow(b, e) == r for b, e, r in triples)
    explog = all(math.exp(e * math.log(b)) == r for b, e, r in triples)
    expect("the recorded pow triples are the library's own", bool(triples) and agree)
    expect("r5: the exp-log rule fails the pow-triple assertion", bool(triples) and not explog,
           f"{sum(math.exp(e * math.log(b)) != r for b, e, r in triples)} of {len(triples)} triples differ")
    print(f"selftest: {len(problems)} red")
    return 1 if problems else 0


# ---- the command line ----------------------------------------------------------------------------------

def main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("command", choices=("generate", "extend", "restamp", "verify", "selftest", "guard", "report", "isolation-report"))
    parser.add_argument("--seed", type=int, default=20261003)
    parser.add_argument("--isolation-draws", type=int, default=32)
    parser.add_argument("--coverage-draws", type=int, default=32)
    parser.add_argument("--workers", type=int, default=min(16, os.cpu_count() or 1))
    parser.add_argument("--state", type=Path, default=None, help="where an interrupted search keeps its state")
    parser.add_argument("--budget", type=float, default=1e9, help="seconds a search call may use before it stops")
    parser.add_argument("--fixture", type=Path, default=FIXTURE_PATH)
    parser.add_argument("--approved", type=Path, default=ROOT / "tests" / "Statistics.Tests" / "ListingOracle.approved.txt")
    options = parser.parse_args(["verify" if argument == "--verify" else argument for argument in argv])
    started = time.time()

    def log(message: str) -> None:
        print(f"{time.time() - started:7.0f}s {message}", flush=True)

    if options.command == "guard":
        problems = guard()
        print("\n".join(problems) if problems else "guard: no map name and no plane literal in the oracle's files")
        return 1 if problems else 0
    if options.command == "selftest":
        return selftest()
    if options.command == "verify":
        problems = verify_fixture(options.fixture, options.workers, log)
        print("\n".join(problems) if problems else f"verify: {options.fixture.name} is what the bytes produce")
        return 1 if problems else 0
    if options.command == "extend":
        write_fixture(extend_fixture(options.fixture, options.workers, log), options.fixture)
        print(f"extend: wrote {options.fixture}")
        return 0
    if options.command == "restamp":
        write_fixture(restamp_fixture(options.fixture), options.fixture)
        print(f"restamp: wrote the digests of today into {options.fixture}; run verify")
        return 0
    if options.command == "report":
        print(report(options.fixture))
        return 0
    if options.command == "isolation-report":
        print(isolation_report(options.fixture, options.approved))
        return 0
    state, finished = search(options.seed, options.isolation_draws, options.coverage_draws, options.workers, log,
                             options.state, options.budget)
    if not finished:
        print("generate: the budget ended before the draws did; run again with the same --state")
        return 3
    plan = [(kept.case, sorted(kept.isolated)) for kept in state.kept]
    replayed = replay(plan, options.workers)
    fixture = build_fixture(replayed, state.counters, options.seed, options.isolation_draws, options.coverage_draws,
                            state.terminal)
    write_fixture(fixture, options.fixture)
    print(f"generate: wrote {options.fixture}")
    return 0


def isolation_report(path: Path, approved: Path) -> str:
    """Which isolated sites the approved mismatch set of the port covers: a site is in the set when
    an output its hook moved, in the case and cycle that isolates it, is a line of the approved file;
    a site that is not is either already right in the port or isolated by a case that does not
    isolate what it claims, and each is a finding."""
    fixture = json.loads(path.read_text(encoding="utf-8"))
    differing = set()
    for line in approved.read_text(encoding="utf-8").split(chr(10)):
        found = re.match(r"(\S+) cycle (\d+) (\S+):", line)
        if found:
            differing.add((found.group(1), int(found.group(2)), found.group(3)))
    inside: List[str] = []
    outside: List[str] = []
    for site in fixture["sites"]:
        if site["status"] != "isolated":
            continue
        hit = False
        for entry in fixture["cases"]:
            for cycle, name in entry["isolated"].get(site["at"], []):
                hit = hit or (entry["id"], cycle, name) in differing
        (inside if hit else outside).append(f"{site['at']} {site['kind']} {site['target'][:30]}")
    lines = [f"isolated sites in the mismatch set: {len(inside)}; not in it: {len(outside)}"]
    lines.extend(f"  not in the set: {entry}" for entry in outside)
    return chr(10).join(lines)


def report(path: Path) -> str:
    fixture = json.loads(path.read_text(encoding="utf-8"))
    lines = [f"cases {len(fixture['cases'])}; counters {fixture['counters']}"]
    for entry in fixture["cases"]:
        steps = [cycle["steps"] for cycle in entry["cycles"]]
        lines.append(f"  {entry['id']} {entry['formulation']} exits {[c['exit'] for c in entry['cycles']]} "
                     f"init {entry['init_steps']} steps {steps} isolates {len(entry['isolated'])}")
    sites = fixture["sites"]
    counts: Dict[str, int] = {}
    for site in sites:
        counts[site["status"]] = counts.get(site["status"], 0) + 1
    lines.append(f"sites {len(sites)}: {counts}")
    for site in sites:
        if site["status"] != "isolated":
            lines.append(f"  {site['status']} {site['at']} {site['kind']} {site['target'][:30]}: {site['reason']}")
    for label, cases in sorted(fixture["coverage"]["features"].items()):
        lines.append(f"feature {label}: {len(cases)} cases")
    for guard_address, loop in fixture["coverage"]["loops"].items():
        lines.append(f"loop {guard_address} block {loop['block']}: unreached residues {loop['unreached_residues']}, "
                     f"short {len(loop['short'])}")
    return "\n".join(lines)


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
