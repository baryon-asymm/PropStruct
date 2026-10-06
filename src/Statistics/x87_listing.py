"""The block-local x87 stack reader over the executable's listing excerpt.

What it reads: `tests/Fixtures/Legacy/PropStructV3.cycle-plane.listing.txt` (dumpbin's text
with each instruction's bytes, proved equal to the executable's by `tests/Fixtures/
check_cycle_plane_listing.py`) and, for the constants it names, the executable's `.rdata`.
What it does: it walks a range of the excerpt in address order with a symbolic x87 stack, a
symbolic integer register file and a symbolic frame, and records, in order, every memory
store, every memory load and every comparison, each with the expression tree of the value
involved. It decides nothing about the Fortran: names of cells, the lines they belong to and
what a kind of site is are the map's and the generator's (`classify-cycle-plane-listing.py`).

Decoding is by bytes, not by dumpbin's mnemonic: dumpbin names `DE E0+i` and `DE F0+i` the
other way round from the Intel manual, and a reader that trusted the name would swap the
operands of every popping subtraction and division. The mnemonic is cross-checked against the
decoded family and a disagreement outside that known quirk stops the walk.

Control flow: a forward jump records the state at the jump for its target, an unconditional
jump leaves the code after it unreachable until a target is reached, a join keeps what the
joining states agree on; a backward jump is checked (the x87 depth at the loop head must be
the depth at the back edge) and not followed, so a loop body is read once, from the state the
code before it left.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
import struct
import sys
from pathlib import Path
from typing import Dict, List, NamedTuple, Optional, Tuple

NODE_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(NODE_ROOT.parents[1] / "tools" / "legacy"))
from legacy import legacy_file  # noqa: E402  (the one reader of PROPSTRUCT_LEGACY_DIR, tools/legacy/API.md)

EXCERPT_NAME = "PropStructV3.cycle-plane.listing.txt"
EXE_NAME = "PropStructV3.exe"

INSTRUCTION = re.compile(r"^  ([0-9A-F]{8}): ((?:[0-9A-F]{2} )*[0-9A-F]{2})(?:\s+(\S.*))?$")
CONTINUATION = re.compile(r"^ {10,}((?:[0-9A-F]{2} )*[0-9A-F]{2})$")
RANGE_LINE = re.compile(r"^# range 0x([0-9A-F]{6})-0x([0-9A-F]{6}) (.*)$")


class StopReading(Exception):
    """The walk met something the reader does not model (a stop, never a skip)."""


# ---- the excerpt -------------------------------------------------------------------

class Ins(NamedTuple):
    address: int
    data: bytes
    mnemonic: str
    args: Tuple[str, ...]

    @property
    def end(self) -> int:
        return self.address + len(self.data)

    @property
    def text(self) -> str:
        return (self.mnemonic + " " + ",".join(self.args)).strip()


def split_arguments(text: str) -> Tuple[str, ...]:
    parts: List[str] = []
    depth = 0
    current = ""
    for char in text:
        if char == "[":
            depth += 1
        elif char == "]":
            depth -= 1
        if char == "," and depth == 0:
            parts.append(current.strip())
            current = ""
        else:
            current += char
    if current.strip():
        parts.append(current.strip())
    return tuple(parts)


def read_excerpt(path: Optional[Path] = None) -> Tuple[List[Ins], List[Tuple[int, int, str]]]:
    """The excerpt's instructions and ranges; `path` defaults to the excerpt outside the
    repository, resolved and verified here, at the call."""
    path = path or Path(legacy_file(EXCERPT_NAME))
    instructions: List[Ins] = []
    ranges: List[Tuple[int, int, str]] = []
    pending: Optional[Tuple[int, bytes, str]] = None
    for line in path.read_text(encoding="utf-8").split("\n"):
        if not line:
            continue
        if line.startswith("#"):
            match = RANGE_LINE.match(line)
            if match:
                ranges.append((int(match.group(1), 16), int(match.group(2), 16), match.group(3)))
            continue
        match = INSTRUCTION.match(line)
        if match:
            if pending:
                instructions.append(_instruction(*pending))
            pending = (int(match.group(1), 16), bytes.fromhex(match.group(2).replace(" ", "")),
                       (match.group(3) or "").strip())
            continue
        match = CONTINUATION.match(line)
        if match and pending:
            pending = (pending[0], pending[1] + bytes.fromhex(match.group(1).replace(" ", "")), pending[2])
            continue
        raise StopReading(f"excerpt line {line!r} is not an instruction")
    if pending:
        instructions.append(_instruction(*pending))
    return instructions, ranges


def _instruction(address: int, data: bytes, text: str) -> Ins:
    head, _, rest = text.partition(" ")
    return Ins(address, data, head, split_arguments(rest.strip()))


# ---- the executable's constants ----------------------------------------------------

class Image:
    """The executable's file-backed bytes by virtual address (the section table)."""

    def __init__(self, path: Optional[Path] = None):
        self.data = (path or Path(legacy_file(EXE_NAME))).read_bytes()
        pe = struct.unpack_from("<I", self.data, 0x3C)[0]
        count = struct.unpack_from("<H", self.data, pe + 6)[0]
        optional = struct.unpack_from("<H", self.data, pe + 20)[0]
        base = struct.unpack_from("<I", self.data, pe + 24 + 28)[0]
        self.sections: List[Tuple[str, int, int, int, int]] = []
        offset = pe + 24 + optional
        for _ in range(count):
            name = self.data[offset:offset + 8].rstrip(b"\0").decode("ascii")
            size, rva, raw_size, raw_offset = struct.unpack_from("<IIII", self.data, offset + 8)
            self.sections.append((name, base + rva, size, raw_size, raw_offset))
            offset += 40

    def section_of(self, address: int) -> Optional[str]:
        for name, start, size, raw_size, _ in self.sections:
            if start <= address < start + max(size, raw_size):
                return name
        return None

    def read(self, address: int, length: int) -> Optional[bytes]:
        for _, start, size, raw_size, raw_offset in self.sections:
            if start <= address < start + max(size, raw_size):
                relative = address - start
                if relative + length <= raw_size:
                    return self.data[raw_offset + relative:raw_offset + relative + length]
        return None

    def float32(self, address: int) -> Optional[float]:
        raw = self.read(address, 4)
        return None if raw is None else struct.unpack("<f", raw)[0]

    def float64(self, address: int) -> Optional[float]:
        raw = self.read(address, 8)
        return None if raw is None else struct.unpack("<d", raw)[0]


# ---- operands ----------------------------------------------------------------------

REGISTERS32 = ("eax", "ebx", "ecx", "edx", "esi", "edi", "ebp", "esp")
SIZES = {"byte": 1, "word": 2, "dword": 4, "qword": 8, "tbyte": 10}


class Mem(NamedTuple):
    size: int                  # bytes, 0 for lea
    base: Optional[str]
    index: Optional[str]
    scale: int
    disp: int                  # signed
    absolute: bool             # ds:[address]


def parse_memory(argument: str) -> Optional[Mem]:
    match = re.fullmatch(r"(?:(byte|word|dword|qword|tbyte) ptr )?(ds:)?\[(.+)\]", argument)
    if not match:
        return None
    size = SIZES.get(match.group(1) or "", 0)
    inner = match.group(3)
    if match.group(2):
        return Mem(size, None, None, 1, int(inner[:-1], 16), True)
    base = index = None
    scale = 1
    disp = 0
    for sign, term in re.findall(r"([+-]?)([^+-]+)", inner):
        sign_value = -1 if sign == "-" else 1
        if term in REGISTERS32:
            if base is None:
                base = term
            else:
                index = term
        elif re.fullmatch(r"(?:eax|ebx|ecx|edx|esi|edi|ebp)\*[0-9]", term):
            index, scale = term.split("*")[0], int(term.split("*")[1])
        else:
            disp += sign_value * (int(term[:-1], 16) if term.endswith("h") else int(term, 10))
    if disp >= 0x80000000:
        disp -= 0x100000000
    return Mem(size, base, index, scale, disp, False)


# ---- x87 decoding by bytes ---------------------------------------------------------

ARITHMETIC = {0: "add", 1: "mul", 4: "sub", 5: "subr", 6: "div", 7: "divr"}


def modrm(data: bytes) -> Tuple[int, int, int]:
    byte = data[1]
    return byte >> 6, (byte >> 3) & 7, byte & 7


class X87(NamedTuple):
    """A decoded x87 instruction: `family` names what it does, `index` the register i,
    `destination` whether the result goes to st(i) (else st(0)), `pop` whether it pops,
    `memory` the operand's width in bytes for a memory form (4, 8, 2 or 10)."""
    family: str
    index: int
    destination: int
    pop: bool
    memory: int


def decode_x87(data: bytes) -> Optional[X87]:
    if data[0] == 0x66:            # the operand-size prefix of fnstcw and fldcw
        data = data[1:]
    opcode = data[0]
    if not 0xD8 <= opcode <= 0xDF:
        return None
    mod, reg, rm = modrm(data)
    if mod != 3:
        width = {0xD8: 4, 0xDC: 8, 0xD9: 4, 0xDD: 8, 0xDB: 4, 0xDF: 8}.get(opcode, 0)
        if opcode in (0xD8, 0xDC):
            if reg == 2:
                return X87("com", 0, 0, False, width)
            if reg == 3:
                return X87("com", 0, 0, True, width)
            return X87(ARITHMETIC[reg], 0, 0, False, width)
        if opcode == 0xD9:
            if reg == 0:
                return X87("ld", 0, 0, False, 4)
            if reg == 2:
                return X87("st", 0, 0, False, 4)
            if reg == 3:
                return X87("st", 0, 0, True, 4)
            if reg == 5:
                return X87("ldcw", 0, 0, False, 2)
            if reg == 7:
                return X87("stcw", 0, 0, False, 2)
        if opcode == 0xDD:
            if reg == 0:
                return X87("ld", 0, 0, False, 8)
            if reg == 2:
                return X87("st", 0, 0, False, 8)
            if reg == 3:
                return X87("st", 0, 0, True, 8)
        if opcode == 0xDB:
            if reg == 0:
                return X87("ild", 0, 0, False, 4)
            if reg == 3:
                return X87("ist", 0, 0, True, 4)
        if opcode == 0xDF:
            if reg == 5:
                return X87("ild", 0, 0, False, 8)
            if reg == 7:
                return X87("ist", 0, 0, True, 8)
        return X87("unknown", 0, 0, False, 0)
    i = rm
    if opcode == 0xD8:
        if reg in (2, 3):
            return X87("com", i, 0, reg == 3, 0)
        return X87(ARITHMETIC[reg], i, 0, False, 0)
    if opcode == 0xDC:
        # the manual's table: DC C0 add, C8 mul, E0 subr, E8 sub, F0 divr, F8 div (to st(i))
        family = {0: "add", 1: "mul", 4: "subr", 5: "sub", 6: "divr", 7: "div"}.get(reg)
        return X87(family, i, 1, False, 0) if family else X87("unknown", 0, 0, False, 0)
    if opcode == 0xDE:
        family = {0: "add", 1: "mul", 4: "subr", 5: "sub", 6: "divr", 7: "div"}.get(reg)
        return X87(family, i, 1, True, 0) if family else X87("unknown", 0, 0, False, 0)
    if opcode == 0xD9:
        if reg == 0:
            return X87("ldst", i, 0, False, 0)
        if reg == 1:
            return X87("xch", i, 0, False, 0)
        if data[1] == 0xE1:
            return X87("abs", 0, 0, False, 0)
        if data[1] == 0xE0:
            return X87("chs", 0, 0, False, 0)
        if data[1] == 0xEE:
            return X87("ldz", 0, 0, False, 0)
        if data[1] == 0xE8:
            return X87("ld1", 0, 0, False, 0)
    if opcode == 0xDD:
        if reg == 0:
            return X87("free", i, 0, False, 0)
        if reg == 2:
            return X87("sti", i, 0, False, 0)
        if reg == 3:
            return X87("sti", i, 0, True, 0)
    if opcode == 0xDF and data[1] == 0xE0:
        return X87("nstsw", 0, 0, False, 0)
    return X87("unknown", 0, 0, False, 0)


# the family a dumpbin mnemonic belongs to, for the cross-check. dumpbin follows the Intel manual
# (DE E0+i is fsubrp, DE E8+i fsubp), so a decoder with those two swapped disagrees with the text
# at every popping subtraction; the check is strict.
MNEMONIC_FAMILY = {
    "fadd": "add", "faddp": "add", "fmul": "mul", "fmulp": "mul", "fsub": "sub", "fsubp": "sub",
    "fsubr": "subr", "fsubrp": "subr", "fdiv": "div", "fdivp": "div", "fdivr": "divr", "fdivrp": "divr",
    "fcom": "com", "fcomp": "com", "fild": "ild", "fistp": "ist",
    "fxch": "xch", "fabs": "abs", "fchs": "chs", "fldz": "ldz", "fld1": "ld1", "ffree": "free",
    "fnstsw": "nstsw", "fnstcw": "stcw", "fldcw": "ldcw",
}


def check_mnemonic(ins: Ins, decoded: X87) -> None:
    if ins.mnemonic in ("fld", "fst", "fstp"):
        expected = {"fld": ("ld", "ldst"), "fst": ("st", "sti"), "fstp": ("st", "sti")}[ins.mnemonic]
        if decoded.family not in expected:
            raise StopReading(f"{ins.address:08X}: {ins.text} decodes as {decoded.family}")
        return
    if MNEMONIC_FAMILY.get(ins.mnemonic) != decoded.family:
        raise StopReading(f"{ins.address:08X}: {ins.text} decodes as {decoded.family}, bytes {ins.data.hex(' ')}")


# ---- values ------------------------------------------------------------------------

class Node:
    """A value on the x87 stack: a leaf (a load, an integer load, a constant, a live-in) or an
    operation over nodes. Identity matters: a copy of a register is the same node."""
    __slots__ = ("serial", "op", "args", "born")
    counter = 0

    def __init__(self, op: str, args: Tuple, born: int):
        Node.counter += 1
        self.serial = Node.counter
        self.op = op
        self.args = args
        self.born = born

    def __repr__(self) -> str:
        return f"<{self.op}#{self.serial}>"


def leaf_nodes(node: Node, seen: Optional[set] = None) -> List[Node]:
    """The leaves under `node` in evaluation order, a shared node once per occurrence."""
    if node.op in ("ld", "int", "const", "livein", "call"):
        return [node]
    found: List[Node] = []
    for argument in node.args:
        if isinstance(argument, Node):
            found.extend(leaf_nodes(argument))
    return found


Loc = Tuple  # ('abs', address) | ('frame', disp) | ('arr', name, disp) | ('ptr', register, disp)


class Event(NamedTuple):
    index: int
    address: int
    kind: str                  # store, istore, load, iload, cmp, call, chop
    loc: Optional[Loc]
    width: int
    node: Optional[Node]       # the value stored, loaded, or the first operand compared
    other: Optional[Node]      # the second operand compared
    text: str


class Val(NamedTuple):
    """What an integer register or a cell holds: nothing known, a number, a pointer into
    an array, or the bits of a float that a store event or the executable's .rdata made."""
    kind: str                  # 'c', 'p', 'f'
    value: object


# ---- the machine state -------------------------------------------------------------

class State:
    def __init__(self) -> None:
        self.st: List[Optional[Node]] = []          # st[0] is ST(0)
        self.regs: Dict[str, Optional[Val]] = {}
        self.cells: Dict[Tuple, object] = {}        # ('abs'|'frame', n) -> Val or ('store', event index)
        self.last_compare: Optional[int] = None
        self.chop = False
        self.pushed: List[Optional[Val]] = []

    def copy(self) -> "State":
        other = State()
        other.st = list(self.st)
        other.regs = dict(self.regs)
        other.cells = dict(self.cells)
        other.last_compare = self.last_compare
        other.chop = self.chop
        other.pushed = list(self.pushed)
        return other


def merge_states(states: List[State], address: int) -> State:
    """Where paths meet. A register that one path holds and another has freed becomes a 'maybe'
    node, which nothing may read; the compiler's `fld x; fstp st(1)` idiom replaces it."""
    merged = State()
    depth = max(len(s.st) for s in states)
    for position in range(depth):
        column = [s.st[position] if position < len(s.st) else None for s in states]
        if all(item is column[0] for item in column):
            merged.st.append(column[0])
        elif any(item is None for item in column):
            merged.st.append(Node("maybe", tuple(item for item in column if item is not None), address))
        else:
            merged.st.append(Node("phi", tuple(column), address))
    for name in set().union(*[s.regs.keys() for s in states]):
        values = [s.regs.get(name) for s in states]
        merged.regs[name] = values[0] if all(v == values[0] for v in values) else None
    for key in set(states[0].cells).intersection(*[set(s.cells) for s in states[1:]]):
        values = [s.cells[key] for s in states]
        if key[0] == "written" or all(v == values[0] for v in values):
            merged.cells[key] = values[0]        # a home every path wrote: written, whichever event
    merged.last_compare = states[0].last_compare
    merged.chop = states[0].chop
    return merged


class Trace(NamedTuple):
    events: List[Event]
    exits: List[Tuple[int, int, int]]          # (from, to, x87 depth)
    dead: List[int]                            # addresses reached by no path the walk followed
    depth_at: Dict[int, int]
    loops: List[Tuple[int, int]]               # (head, back edge)


SQRT_THUNK = 0x418C16
POW_THUNK = 0x418C10
AMOD_THUNK = 0x418C1C
IO_THUNKS = (0x418BA4, 0x418BAA, 0x418BB0)
JUMPS = {"jmp", "je", "jne", "jg", "jge", "jl", "jle", "jp", "jnp"}
SET_CC = {"setl", "setle", "setg", "setge", "setne", "setnp"}
NAMES16 = {"al": "eax", "ah": "eax", "ax": "eax", "bl": "ebx", "bh": "ebx", "bx": "ebx", "cl": "ecx",
           "ch": "ecx", "cx": "ecx", "dl": "edx", "dh": "edx", "dx": "edx"}


class Walker:
    """Walks `instructions` in address order. `arrays` maps the .data address of a descriptor's
    base-pointer word to the array's name; a register loaded from it points into that array."""

    def __init__(self, instructions: List[Ins], image: Image, arrays: Dict[int, str], integer_only: bool = False):
        self.integer_only = integer_only
        self.instructions = instructions
        self.image = image
        self.arrays = arrays
        self.events: List[Event] = []
        self.state = State()

    # -- recording ---------------------------------------------------------------------

    def record(self, ins: Ins, kind: str, loc: Optional[Loc], width: int, node: Optional[Node] = None,
               other: Optional[Node] = None) -> int:
        self.events.append(Event(len(self.events), ins.address, kind, loc, width, node, other, ins.text))
        return len(self.events) - 1

    def resolve(self, memory: Mem) -> Loc:
        if memory.absolute:
            return ("abs", memory.disp & 0xFFFFFFFF)
        if memory.base == "ebp" and memory.index is None:
            return ("frame", memory.disp)
        tag = self.state.regs.get(memory.base) if memory.base else None
        if tag is not None and tag.kind == "p":
            return ("arr", tag.value, memory.disp)
        return ("ptr", memory.base, memory.disp)

    @staticmethod
    def cell_key(loc: Loc) -> Optional[Tuple]:
        return loc if loc[0] in ("abs", "frame") else None

    # -- x87 ---------------------------------------------------------------------------

    def push(self, node: Node) -> None:
        if len(self.state.st) >= 8:
            raise StopReading(f"x87 stack overflow before {node}")
        self.state.st.insert(0, node)

    def top(self, index: int, ins: Ins) -> Node:
        st = self.state.st
        if index >= len(st) or st[index] is None:
            raise StopReading(f"{ins.address:08X}: {ins.text} reads an empty register st({index})")
        if st[index].op == "maybe":
            raise StopReading(f"{ins.address:08X}: {ins.text} reads st({index}), empty on one path that reaches it")
        return st[index]

    def trim(self) -> None:
        while self.state.st and self.state.st[-1] is None:
            self.state.st.pop()

    def pop(self) -> None:
        del self.state.st[0]
        self.trim()

    def memory_leaf(self, ins: Ins, memory: Mem, width: int, integer: bool) -> Node:
        loc = self.resolve(memory)
        key = self.cell_key(loc)
        source = self.state.cells.get(key) if key else None
        if memory.absolute and self.image.section_of(loc[1]) == ".rdata":
            value = self.image.float32(loc[1]) if width == 4 else self.image.float64(loc[1])
            node = Node("const", (value, loc[1], width), ins.address)
            self.record(ins, "load", loc, width, node)
            return node
        written = self.state.cells.get(("written", key)) if key else None
        node = Node("int" if integer else "ld", (loc, width, source, written), ins.address)
        self.record(ins, "load", loc, width, node)
        return node

    def x87(self, ins: Ins, decoded: X87) -> None:
        check_mnemonic(ins, decoded)
        s = self.state
        family = decoded.family
        memory = parse_memory(ins.args[0]) if decoded.memory and ins.args else None
        if family in ("add", "sub", "subr", "mul", "div", "divr", "com", "abs", "chs") and s.chop:
            raise StopReading(f"{ins.address:08X}: arithmetic under chop rounding")
        if family == "ld":
            self.push(self.memory_leaf(ins, memory, decoded.memory, False))
        elif family == "ild":
            self.push(self.memory_leaf(ins, memory, decoded.memory, True))
        elif family == "ldst":
            node = self.top(decoded.index, ins)
            self.record(ins, "regread", ("st", decoded.index), 0, node)
            self.push(node)
        elif family in ("ldz", "ld1"):
            self.push(Node("const", (0.0 if family == "ldz" else 1.0, None, 8), ins.address))
        elif family == "st":
            loc = self.resolve(memory)
            value = self.top(0, ins)
            event = self.record(ins, "store", loc, decoded.memory, value)
            key = self.cell_key(loc)
            if key:
                s.cells[key] = ("store", event)
                s.cells[("written", key)] = event
            if decoded.pop:
                self.pop()
        elif family == "sti":
            value = self.top(0, ins)
            while len(s.st) <= decoded.index:
                s.st.append(None)
            s.st[decoded.index] = value
            if decoded.pop:
                self.pop()
        elif family == "ist":
            value = self.top(0, ins)
            loc = self.resolve(memory)
            event = self.record(ins, "chop" if s.chop else "store", loc, decoded.memory,
                                Node("toint", (value,), ins.address))
            key = self.cell_key(loc)
            if key:
                s.cells[key] = ("int", event)
                s.cells[("written", key)] = event
            self.pop()
        elif family in ("add", "sub", "subr", "mul", "div", "divr"):
            self.arithmetic(ins, decoded, memory)
        elif family == "com":
            first = self.top(0, ins)
            if memory:
                second = self.memory_leaf(ins, memory, decoded.memory, False)
            else:
                second = self.top(decoded.index, ins)
                self.record(ins, "regread", ("st", decoded.index), 0, second)
            s.last_compare = self.record(ins, "cmp", None, 0, first, second)
            if decoded.pop:
                self.pop()
        elif family == "nstsw":
            pass
        elif family in ("abs", "chs"):
            s.st[0] = Node(family, (self.top(0, ins),), ins.address)
        elif family == "xch":
            a, b = self.top(0, ins), self.top(decoded.index, ins)
            s.st[0], s.st[decoded.index] = b, a
        elif family == "free":
            while len(s.st) <= decoded.index:
                s.st.append(None)
            s.st[decoded.index] = None
            self.trim()
        elif family == "stcw":
            s.cells[self.cell_key(self.resolve(memory))] = Val("cw", "saved")
        elif family == "ldcw":
            held = s.cells.get(self.cell_key(self.resolve(memory)))
            s.chop = held == Val("cw", "chop")
        else:
            raise StopReading(f"{ins.address:08X}: {ins.text} is not modelled")

    def arithmetic(self, ins: Ins, decoded: X87, memory: Optional[Mem]) -> None:
        s = self.state
        family = decoded.family
        if memory:
            x = self.top(0, ins)
            y = self.memory_leaf(ins, memory, decoded.memory, False)
            destination = 0
        else:
            destination = decoded.index if decoded.destination else 0
            source = 0 if decoded.destination else decoded.index
            x, y = self.top(destination, ins), self.top(source, ins)
            self.record(ins, "regread", ("st", decoded.index), 0, self.top(decoded.index, ins))
        symbol = {"add": "+", "mul": "*", "sub": "-", "subr": "-", "div": "/", "divr": "/"}[family]
        left, right = (y, x) if family in ("subr", "divr") else (x, y)
        s.st[destination] = Node(symbol, (left, right), ins.address)
        if decoded.pop:
            self.pop()

    # -- calls -------------------------------------------------------------------------

    def call(self, ins: Ins) -> None:
        s = self.state
        target = int(ins.args[0], 16)
        for register in ("eax", "ecx", "edx"):
            s.regs[register] = None
        if self.integer_only:
            s.pushed = []
            return
        if target == SQRT_THUNK:
            s.st[0] = Node("sqrt", (self.top(0, ins),), ins.address)
        elif target == POW_THUNK:
            exponent, base = self.top(0, ins), self.top(1, ins)
            self.pop()
            s.st[0] = Node("pow", (base, exponent), ins.address)
        elif target == AMOD_THUNK:
            self.push(Node("call", ("amod", tuple(s.pushed)), ins.address))
        elif target in IO_THUNKS:
            self.record(ins, "call", None, 0)
        else:
            raise StopReading(f"{ins.address:08X}: call to {target:08X}, a target with no stub")
        s.pushed = []

    # -- integer instructions ----------------------------------------------------------

    def value_of(self, argument: str) -> Optional[Val]:
        s = self.state
        if argument in REGISTERS32:
            return s.regs.get(argument)
        if "[" not in argument and re.fullmatch(r"-?[0-9A-F]+h?", argument):
            return Val("c", int(argument[:-1], 16) if argument.endswith("h") else int(argument))
        memory = parse_memory(argument)
        if memory is None:
            return None
        loc = self.resolve(memory)
        if loc[0] == "abs":
            address = loc[1]
            if address in self.arrays:
                return Val("p", self.arrays[address])
            if self.image.section_of(address) == ".rdata":
                return Val("f", ("rdata", address, self.image.read(address, memory.size or 4)))
        key = self.cell_key(loc)
        content = s.cells.get(key) if key else None
        if isinstance(content, Val):
            return content
        if isinstance(content, tuple) and content[0] == "store":
            return Val("f", ("event", content[1]))
        return None

    @staticmethod
    def combine(left: Optional[Val], right: Optional[Val], subtract: bool) -> Optional[Val]:
        left_ptr = left is not None and left.kind == "p"
        right_ptr = right is not None and right.kind == "p"
        if left_ptr and not right_ptr:
            return left
        if right_ptr and not left_ptr and not subtract:
            return right
        return None

    def store_integer(self, ins: Ins, memory: Mem, value: Optional[Val]) -> None:
        loc = self.resolve(memory)
        node = Node("copy", (value.value,), ins.address) if value is not None and value.kind == "f" else None
        event = self.record(ins, "istore", loc, memory.size or 4, node)
        key = self.cell_key(loc)
        if key:
            self.state.cells[key] = value if value is not None else ("unknown", ins.address)
            self.state.cells[("written", key)] = event

    def load_integer(self, ins: Ins, memory: Mem) -> None:
        loc = self.resolve(memory)
        if not (loc[0] == "abs" and self.image.section_of(loc[1]) == ".rdata"):
            self.record(ins, "iload", loc, memory.size or 4)

    def clobber(self, name: str) -> None:
        self.state.regs[NAMES16.get(name, name)] = None

    def integer(self, ins: Ins) -> None:
        s = self.state
        m = ins.mnemonic
        a = ins.args
        if m == "push":
            s.pushed.append(self.value_of(a[0]) if len(a) == 1 else None)
        elif m in ("mov", "movsx", "movzx"):
            destination, source = a
            memory_destination = parse_memory(destination)
            if memory_destination:
                self.store_integer(ins, memory_destination, self.value_of(source))
            else:
                memory_source = parse_memory(source)
                if memory_source:
                    self.load_integer(ins, memory_source)
                if destination in REGISTERS32 and m == "mov":
                    s.regs[destination] = self.value_of(source)
                else:
                    self.clobber(destination)
        elif m == "lea":
            memory = parse_memory(a[1])
            tag = s.regs.get(memory.base) if memory and memory.base else None
            if memory and memory.absolute:
                s.regs[a[0]] = Val("a", memory.disp & 0xFFFFFFFF)
            else:
                s.regs[a[0]] = tag if tag is not None and tag.kind == "p" else None
        elif m in ("add", "sub", "adc", "and", "or", "xor", "shl", "sar", "imul", "inc", "dec", "neg", "idiv"):
            destination = a[0]
            memory_destination = parse_memory(destination)
            if memory_destination:
                self.store_integer(ins, memory_destination, None)
            elif destination not in REGISTERS32:
                self.clobber(destination)
            else:
                memory_source = parse_memory(a[1]) if len(a) == 2 else None
                if memory_source:
                    self.load_integer(ins, memory_source)
                if m == "xor" and len(a) == 2 and a[1] == destination:
                    s.regs[destination] = Val("c", 0)
                elif m in ("add", "sub", "adc") and len(a) == 2:
                    s.regs[destination] = self.combine(s.regs.get(destination), self.value_of(a[1]), m == "sub")
                else:
                    s.regs[destination] = None
        elif m in ("cmp", "test"):
            for argument in a:
                memory = parse_memory(argument)
                if memory:
                    self.load_integer(ins, memory)
        elif m in SET_CC:
            self.clobber(a[0])
        elif m == "cdq":
            s.regs["edx"] = None
        elif m == "call":
            self.call(ins)
        elif m in JUMPS:
            pass
        else:
            raise StopReading(f"{ins.address:08X}: {ins.text} is not modelled")

    def word_forms(self, ins: Ins) -> bool:
        """The control-word dance and the 16-bit moves around fnstsw, which carry no value."""
        m, a = ins.mnemonic, ins.args
        s = self.state
        if m == "mov" and a and a[0] == "ax" and parse_memory(a[1]):
            s.regs["eax"] = Val("cw", "saved")
            return True
        if m == "or" and a and a[0] == "ax":
            s.regs["eax"] = Val("cw", "chop") if a[1] == "0C00h" else None
            return True
        if m == "mov" and a and parse_memory(a[0]) is not None and parse_memory(a[0]).size == 2:
            memory = parse_memory(a[0])
            loc = self.resolve(memory)
            self.record(ins, "istore", loc, 2, None)
            key = self.cell_key(loc)
            if key:
                s.cells[key] = s.regs.get("eax") if a[1] == "ax" else None
            return True
        return False

    # -- the walk ----------------------------------------------------------------------

    def run(self, first: int, end: int, state: Optional[State] = None) -> Trace:
        self.state = state if state is not None else State()
        pending: Dict[int, List[State]] = {}
        exits: List[Tuple[int, int, int]] = []
        dead: List[int] = []
        depth_at: Dict[int, int] = {}
        loops: List[Tuple[int, int]] = []
        live: Optional[State] = self.state
        for ins in self.instructions:
            if not first <= ins.address < end:
                continue
            arrivals = pending.pop(ins.address, [])
            if arrivals:
                live = merge_states(([live] if live is not None else []) + arrivals, ins.address)
            elif live is None:
                dead.append(ins.address)
                continue
            self.state = live
            depth_at[ins.address] = len(live.st)
            decoded = decode_x87(ins.data) if ins.mnemonic.startswith("f") else None
            if decoded is not None:
                if not self.integer_only:
                    self.x87(ins, decoded)
            elif not self.word_forms(ins):
                self.integer(ins)
            if ins.mnemonic in JUMPS:
                target = int(ins.args[0], 16)
                if target >= end or target < first:
                    exits.append((ins.address, target, len(live.st)))
                elif target > ins.address:
                    pending.setdefault(target, []).append(live.copy())
                else:
                    # the compiler parks per-pass copies in registers below the live ones (fst st(5)),
                    # so the depth may grow over a pass, never shrink under the head's
                    if depth_at.get(target, 0) > len(live.st):
                        raise StopReading(f"{ins.address:08X}: back edge to {target:08X} at x87 depth "
                                          f"{len(live.st)}, the head had {depth_at.get(target)}")
                    loops.append((target, ins.address))
                if ins.mnemonic == "jmp":
                    live = None
        return Trace(self.events, exits, dead, depth_at, loops)


# ---- rendering ---------------------------------------------------------------------

def render_loc(loc: Loc) -> str:
    if loc[0] == "abs":
        return f"@{loc[1]:06X}"
    if loc[0] == "frame":
        return f"[ebp{loc[1]:+#x}]"
    if loc[0] == "arr":
        return f"{loc[1]}{loc[2]:+#x}" if loc[2] else str(loc[1])
    return f"[{loc[1]}{loc[2]:+#x}]"


def render(node: Node, labels=None, depth: int = 0) -> str:
    """A node as an expression; `labels(loc)` may name a memory leaf."""
    if node.op in ("ld", "int"):
        loc = node.args[0]
        name = labels(loc) if labels else None
        return ("i:" if node.op == "int" else "") + (name or render_loc(loc))
    if node.op == "const":
        return repr(node.args[0])
    if node.op in ("+", "-", "*", "/"):
        return f"({render(node.args[0], labels, depth + 1)} {node.op} {render(node.args[1], labels, depth + 1)})"
    if node.op in ("abs", "chs", "sqrt", "toint"):
        return f"{node.op}({render(node.args[0], labels, depth + 1)})"
    if node.op == "pow":
        return f"pow({render(node.args[0], labels, depth + 1)}, {render(node.args[1], labels, depth + 1)})"
    return f"<{node.op}#{node.serial}>"
