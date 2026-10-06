"""An interpreter for the executable's own x87 and integer bytes, over the committed listing excerpt.

What it runs: the instructions of `Legacy/PropStructV3.cycle-plane.listing.txt` (proved equal to the
executable's bytes by `check_cycle_plane_listing.py`), over the `.rdata` and `.data` of
`Legacy/PropStructV3.exe`. What it knows: addresses and bytes, never a Fortran name, a formula, a
rounding rule or a plane literal; the names belong to `src/Statistics/CyclePlaneListing.map.txt`
and are the driver's (`cycle_plane_oracle.py`). What it computes is what the bytes say under the
Intel manual: the Fortran side of the tree is derived from the source, this one from the machine,
so a slip in either shows as a difference, never as a false agreement.

Two decoders must agree. `decode` reads the bytes into an operation and renders it back to
dumpbin's text; `Machine` refuses to run until every instruction of its executable ranges renders to
the excerpt's own text (the cross-check, `cross_check`). A decoder with the popping subtractions
swapped (DE E0+i and DE E8+i) disagrees with dumpbin at the eight `fsubrp` sites and is refused.

The x87 is modelled with eight physical registers, tags and a TOP, holding Python floats. That is
exact because the precision control is 53 bits (the C start-up calls `_controlfp(0x10000, 0x30000)`)
and every result is checked finite and zero-or-normal, so a double operation and the 53-bit x87
operation round alike. Everything the model does not cover is a stop, never a skip:

  O1  an opcode outside the subset, a cross-check mismatch, an exit from the ranges, a call to a
      target with no stub;
  O2  a read of an undefined byte, register, flag or empty register file slot, an access outside
      every block;
  O3  arithmetic under a control word other than 53-bit precision and round-to-nearest, `fild` of
      2^53 or more, an invalid, zero-divide, overflow or denormal condition, a stack fault.

Memory is tagged: a byte is readable after an injection or a store (or in `.rdata`, the file's own),
so a home that nobody injected can never read its file value silently.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import math
import struct
from pathlib import Path
from typing import Callable, Dict, List, NamedTuple, Optional, Sequence, Tuple

import check_cycle_plane_listing as listing_check

HERE = Path(__file__).resolve().parent
MASK32 = 0xFFFFFFFF
SMALLEST_NORMAL = 2.2250738585072014e-308
SMALLEST_NORMAL32 = 1.1754943508222875e-38
TWO_POW_53 = float(1 << 53)

REG32 = ("eax", "ecx", "edx", "ebx", "esp", "ebp", "esi", "edi")
REG16 = tuple(name[1:] for name in REG32)
REG8 = ("al", "cl", "dl", "bl", "ah", "ch", "dh", "bh")
CONDITIONS = ("o", "no", "b", "ae", "e", "ne", "be", "a", "s", "ns", "p", "np", "l", "ge", "le", "g")


class Stop(Exception):
    """A stop of the machine: `code` is O1, O2 or O3 (the module docstring), never a skip."""

    def __init__(self, code: str, message: str, address: Optional[int] = None):
        super().__init__(message)
        self.code = code
        self.detail = message
        self.address = address

    def __str__(self) -> str:
        where = "" if self.address is None else f"{self.address:08X}: "
        return f"{self.code} {where}{self.detail}"


# ---- the decoder tables -----------------------------------------------------------------------

# The arithmetic families by the ModRM register field. D8 (st(0) op operand) and the memory forms
# follow the plain order; DC and DE (st(i) op st(0)) swap the subtractions and divisions (Intel
# manual, tables of FSUB, FSUBR, FDIV, FDIVR).
D8_FAMILY = {0: "add", 1: "mul", 4: "sub", 5: "subr", 6: "div", 7: "divr"}
DE_FAMILY = {0: "add", 1: "mul", 4: "subr", 5: "sub", 6: "divr", 7: "div"}
DEFAULT_TABLES = {"D8": D8_FAMILY, "DE": DE_FAMILY}


class Memory(NamedTuple):
    base: Optional[int]
    index: Optional[int]
    scale: int
    disp: int
    disp_bytes: int          # 0, 1 or 4: how the displacement was encoded


class Ins:
    __slots__ = ("address", "length", "nxt", "data", "text", "fn", "a", "b", "c", "family")

    def __init__(self, address: int, data: bytes, text: str, fn: Callable, a=None, b=None, c=None, family=""):
        self.address = address
        self.length = len(data)
        self.nxt = address + len(data)
        self.data = data
        self.text = text
        self.fn = fn
        self.a = a
        self.b = b
        self.c = c
        self.family = family


def hex_short(value: int) -> str:
    """dumpbin's rendering of a small displacement or immediate: bare under ten, else hex with a
    leading 0 where the first digit is a letter."""
    if value < 10:
        return str(value)
    digits = f"{value:X}"
    if digits[0] in "ABCDEF":
        digits = "0" + digits
    return digits + "h"


def hex_disp32(value: int) -> str:
    return f"{value & MASK32:X}h"


def render_memory(mem: Memory) -> str:
    if mem.base is None and mem.index is None:
        return f"ds:[{mem.disp & MASK32:08X}h]"
    parts = ""
    if mem.base is not None:
        parts += REG32[mem.base]
    if mem.index is not None:
        parts += ("+" if parts else "") + REG32[mem.index] + (f"*{mem.scale}" if mem.scale != 1 else "")
    if mem.disp_bytes == 1:
        parts += ("-" if mem.disp < 0 else "+") + hex_short(abs(mem.disp))
    elif mem.disp_bytes == 4:
        parts += "+" + (hex_disp32(mem.disp) if mem.disp < 0 else hex_short(mem.disp))
    return f"[{parts}]"


def render_immediate(value: int, bits: int = 32) -> str:
    return hex_short(value & ((1 << bits) - 1))


def signed(value: int, bits: int = 32) -> int:
    value &= (1 << bits) - 1
    return value - (1 << bits) if value >> (bits - 1) else value


def parity(value: int) -> int:
    return 1 if bin(value & 0xFF).count("1") % 2 == 0 else 0


class Reader:
    """The bytes of one instruction, read forward."""

    def __init__(self, data: bytes, address: int):
        self.data = data
        self.position = 0
        self.address = address

    def byte(self) -> int:
        if self.position >= len(self.data):
            raise Stop("O1", "an instruction runs past its bytes", self.address)
        value = self.data[self.position]
        self.position += 1
        return value

    def peek(self) -> int:
        if self.position >= len(self.data):
            raise Stop("O1", "an instruction runs past its bytes", self.address)
        return self.data[self.position]

    def signed8(self) -> int:
        return signed(self.byte(), 8)

    def unsigned16(self) -> int:
        low = self.byte()
        return low | self.byte() << 8

    def unsigned32(self) -> int:
        value = 0
        for shift in (0, 8, 16, 24):
            value |= self.byte() << shift
        return value

    def signed32(self) -> int:
        return signed(self.unsigned32())

    @property
    def done(self) -> bool:
        return self.position == len(self.data)


def read_modrm(reader: Reader) -> Tuple[int, int, int, Optional[Memory]]:
    """(mod, reg field, rm field, memory operand or None for a register r/m)."""
    byte = reader.byte()
    mod, reg, rm = byte >> 6, (byte >> 3) & 7, byte & 7
    if mod == 3:
        return mod, reg, rm, None
    base: Optional[int] = rm
    index: Optional[int] = None
    scale = 1
    if rm == 4:
        sib = reader.byte()
        scale = 1 << (sib >> 6)
        idx = (sib >> 3) & 7
        index = None if idx == 4 else idx
        base = sib & 7
        if base == 5 and mod == 0:
            base = None
    if mod == 0 and rm == 5:
        return mod, reg, rm, Memory(None, None, 1, reader.signed32(), 4)
    if mod == 0:
        if base is None:
            return mod, reg, rm, Memory(None, index, scale, reader.signed32(), 4)
        return mod, reg, rm, Memory(base, index, scale, 0, 0)
    if mod == 1:
        return mod, reg, rm, Memory(base, index, scale, reader.signed8(), 1)
    return mod, reg, rm, Memory(base, index, scale, reader.signed32(), 4)


# ---- the machine ------------------------------------------------------------------------------

class Region:
    __slots__ = ("name", "base", "end", "data", "defined", "writable", "touched")

    def __init__(self, name: str, base: int, size: int, data: Optional[bytes], always_defined: bool, writable: bool):
        self.name = name
        self.base = base
        self.end = base + size
        self.data = bytearray(data if data is not None else bytes(size))
        if len(self.data) < size:
            self.data.extend(bytes(size - len(self.data)))
        self.defined: Optional[bytearray] = None if always_defined else bytearray(size)
        self.writable = writable
        self.touched = False


class Machine:
    """The registers, flags, memory and x87 of one run. Build it with the listing and the
    executable, add the blocks the driver injects, then `run`."""

    def __init__(self, ranges: Sequence[Tuple[int, int]], tables: Optional[Dict[str, Dict[int, str]]] = None,
                 listing_path: Optional[Path] = None, exe_path: Optional[Path] = None, cross_check: bool = True,
                 byte_edits: Optional[Dict[int, int]] = None, listing_text: Optional[str] = None):
        """`listing_text` replaces the committed excerpt with a synthetic one (the self-test's tiny
        programs); it is then not verified against the executable, the excerpt always is."""
        self.tables = tables if tables is not None else DEFAULT_TABLES
        exe_path = exe_path or listing_check.exe_path()
        self.image = exe_path.read_bytes()
        if listing_text is None:
            listing_path = listing_path or listing_check.listing_path()
            self.listing = listing_check.parse(listing_path.read_text(encoding="utf-8"))
            problems = listing_check.verify_listing(self.listing, self.image, listing_check.sha256(exe_path),
                                                    listing_check.sha256(listing_check.dll_path()))
            if problems:
                raise Stop("O1", "the excerpt is not the executable's bytes: " + problems[0])
        else:
            self.listing = listing_check.parse(listing_text)
        self.sections = listing_check.sections(self.image)
        self.executable = list(ranges)
        self.code: Dict[int, Ins] = {}
        self.cross_check_mismatches: List[str] = []
        self._decode_ranges(byte_edits or {}, cross_check)
        self.pages: Dict[int, Region] = {}
        self.regions: List[Region] = []
        self.regs: List[Optional[int]] = [None] * 8
        self.reg_mask: List[int] = [0] * 8
        self.flags: Dict[str, Optional[int]] = {"zf": None, "sf": None, "cf": None, "of": None, "pf": None}
        self.st = [0.0] * 8
        self.tag = [0] * 8
        self.top = 0
        self.cw = 0x027F
        self.c0 = self.c2 = self.c3 = 0
        self.stubs: Dict[int, Callable[["Machine"], Optional[int]]] = {}
        self.steps = 0
        self.pf_source = 0
        self.calls: List[Tuple[int, int]] = []
        self.store_hook: Optional[Callable[[int, int], None]] = None
        self.before: Dict[int, Callable[["Machine", Ins], None]] = {}
        self.after: Dict[int, Callable[["Machine", Ins], None]] = {}
        self.shadow: Dict[int, float] = {}
        self.shadow_hits = 0
        self.current: Optional[Ins] = None
        self.clock = 0
        self.last_store: Dict[int, Tuple[int, bytes, int]] = {}
        self.track_stores = False
        self.watch: Dict[int, List[Tuple[int, int, bytes]]] = {}
        self._map_sections()

    # -- decoding ------------------------------------------------------------------------------

    def _decode_ranges(self, byte_edits: Dict[int, int], cross_check: bool) -> None:
        for ins in self.listing.instructions:
            if not any(first <= ins.address < end for first, end in self.executable):
                continue
            data = bytearray(ins.data)
            for offset in range(len(data)):
                if ins.address + offset in byte_edits:
                    data[offset] = byte_edits[ins.address + offset]
            decoded = decode(bytes(data), ins.address, self.tables)
            self.code[ins.address] = decoded
            shown = " ".join(ins.text.split())
            if cross_check and decoded.text != shown:
                self.cross_check_mismatches.append(f"{ins.address:08X}: decoded {decoded.text!r}, dumpbin {shown!r}")
        if cross_check and self.cross_check_mismatches:
            raise Stop("O1", f"the cross-check fails at {len(self.cross_check_mismatches)} instructions, first "
                       + self.cross_check_mismatches[0])

    def decode_listing_range(self, first: int, end: int) -> List[Ins]:
        """The decoded instructions of a range of the excerpt, for a caller that reads and does not
        run them (the prologue's frame size)."""
        return [decode(ins.data, ins.address, self.tables) for ins in self.listing.instructions
                if first <= ins.address < end]

    # -- memory --------------------------------------------------------------------------------

    def _map_sections(self) -> None:
        for section in self.sections:
            size = max(section.size, section.raw_size)
            raw = self.image[section.raw_offset:section.raw_offset + section.raw_size]
            if section.name == ".text":
                continue
            self.add_region(section.name, section.address, size, raw, always_defined=section.name == ".rdata",
                            writable=section.name != ".rdata")

    def add_region(self, name: str, base: int, size: int, data: Optional[bytes] = None, always_defined: bool = False,
                   writable: bool = True) -> Region:
        region = Region(name, base, size, data, always_defined, writable)
        for page in range(base >> 12, (base + size + 0xFFF) >> 12):
            if page in self.pages:
                raise Stop("O1", f"region {name} overlaps {self.pages[page].name}")
            self.pages[page] = region
        self.regions.append(region)
        return region

    def region_at(self, address: int, size: int) -> Region:
        region = self.pages.get(address >> 12)
        if region is None or address < region.base or address + size > region.end:
            raise Stop("O2", f"access of {size} bytes at {address & MASK32:08X} is outside every block")
        return region

    def read_bytes(self, address: int, size: int) -> bytes:
        region = self.region_at(address, size)
        offset = address - region.base
        if region.defined is not None and region.defined.find(0, offset, offset + size) != -1:
            raise Stop("O2", f"read of {size} bytes at {address:08X} ({region.name}+{offset:X}) holds an undefined byte")
        return bytes(region.data[offset:offset + size])

    def write_bytes(self, address: int, data: bytes) -> None:
        size = len(data)
        region = self.region_at(address, size)
        if not region.writable:
            raise Stop("O1", f"store to read-only {region.name} at {address:08X}")
        offset = address - region.base
        region.data[offset:offset + size] = data
        region.touched = True
        if region.defined is not None:
            region.defined[offset:offset + size] = b"\x01" * size
        if self.shadow:
            for held in [a for a in self.shadow if address - 3 <= a < address + size]:
                del self.shadow[held]
        if self.watch and address in self.watch and self.current is not None:
            self.watch[address].append((self.clock, self.current.address, bytes(data)))
        if self.track_stores and self.current is not None:
            self.last_store[self.current.address] = (address, bytes(data), self.clock)
        if self.store_hook is not None:
            self.store_hook(address, size)

    def read_f32(self, address: int) -> float:
        """A binary32 operand. A store the self-test or the oracle's isolation hooked keeps the
        register's unrounded value beside the rounded bytes, and a reload of those bytes sees it."""
        raw = self.read_bytes(address, 4)
        if self.shadow:
            kept = self.shadow.get(address)
            if kept is not None and struct.pack("<f", kept) == raw:
                self.shadow_hits += 1
                return kept
        return struct.unpack("<f", raw)[0]

    def read_int(self, address: int, size: int) -> int:
        return int.from_bytes(self.read_bytes(address, size), "little")

    def write_int(self, address: int, size: int, value: int) -> None:
        self.write_bytes(address, (value & ((1 << (8 * size)) - 1)).to_bytes(size, "little"))

    def is_defined(self, address: int, size: int) -> bool:
        region = self.pages.get(address >> 12)
        if region is None or address < region.base or address + size > region.end:
            return False
        if region.defined is None:
            return True
        offset = address - region.base
        return region.defined[offset:offset + size].count(1) == size

    # -- registers -----------------------------------------------------------------------------

    def reg(self, index: int, width: int = 32, high: bool = False) -> int:
        if width == 32:
            if self.reg_mask[index] != MASK32:
                raise Stop("O2", f"read of the undefined register {REG32[index]}")
            return self.regs[index]  # type: ignore[return-value]
        if width == 16:
            if self.reg_mask[index] & 0xFFFF != 0xFFFF:
                raise Stop("O2", f"read of the undefined register {REG16[index]}")
            return self.regs[index] & 0xFFFF  # type: ignore[operator]
        if high:
            if self.reg_mask[index] & 0xFF00 != 0xFF00:
                raise Stop("O2", f"read of the undefined register {REG8[index + 4]}")
            return (self.regs[index] >> 8) & 0xFF  # type: ignore[operator]
        if self.reg_mask[index] & 0xFF != 0xFF:
            raise Stop("O2", f"read of the undefined register {REG8[index]}")
        return self.regs[index] & 0xFF  # type: ignore[operator]

    def set_reg(self, index: int, value: int, width: int = 32, high: bool = False) -> None:
        old = self.regs[index] or 0
        if width == 32:
            self.regs[index] = value & MASK32
            self.reg_mask[index] = MASK32
        elif width == 16:
            self.regs[index] = (old & 0xFFFF0000) | (value & 0xFFFF)
            self.reg_mask[index] |= 0xFFFF
        elif high:
            self.regs[index] = (old & 0xFFFF00FF) | ((value & 0xFF) << 8)
            self.reg_mask[index] |= 0xFF00
        else:
            self.regs[index] = (old & 0xFFFFFF00) | (value & 0xFF)
            self.reg_mask[index] |= 0xFF

    def undefine(self, index: int) -> None:
        self.regs[index] = None
        self.reg_mask[index] = 0

    def effective_address(self, mem: Memory) -> int:
        address = mem.disp
        if mem.base is not None:
            address += self.reg(mem.base)
        if mem.index is not None:
            address += self.reg(mem.index) * mem.scale
        return address & MASK32

    # -- flags ---------------------------------------------------------------------------------

    def set_flags(self, result: int, bits: int, cf: Optional[int], of: Optional[int]) -> None:
        mask = (1 << bits) - 1
        result &= mask
        self.flags["zf"] = 1 if result == 0 else 0
        self.flags["sf"] = (result >> (bits - 1)) & 1
        self.flags["pf"] = -1
        self.pf_source = result & 0xFF
        self.flags["cf"] = cf
        self.flags["of"] = of

    def flag(self, name: str) -> int:
        value = self.flags[name]
        if value is None:
            raise Stop("O2", f"read of the undefined flag {name}")
        if value == -1:
            return parity(self.pf_source)
        return value

    def condition(self, cc: int) -> bool:
        f = self.flag
        if cc == 0:
            return bool(f("of"))
        if cc == 1:
            return not f("of")
        if cc == 2:
            return bool(f("cf"))
        if cc == 3:
            return not f("cf")
        if cc == 4:
            return bool(f("zf"))
        if cc == 5:
            return not f("zf")
        if cc == 6:
            return bool(f("cf") or f("zf"))
        if cc == 7:
            return not (f("cf") or f("zf"))
        if cc == 8:
            return bool(f("sf"))
        if cc == 9:
            return not f("sf")
        if cc == 10:
            return bool(f("pf"))
        if cc == 11:
            return not f("pf")
        if cc == 12:
            return f("sf") != f("of")
        if cc == 13:
            return f("sf") == f("of")
        if cc == 14:
            return bool(f("zf") or f("sf") != f("of"))
        return not f("zf") and f("sf") == f("of")

    # -- the x87 -------------------------------------------------------------------------------

    def phys(self, i: int) -> int:
        return (self.top + i) & 7

    def fpu_push(self, value: float) -> None:
        self.top = (self.top - 1) & 7
        if self.tag[self.top]:
            raise Stop("O3", "x87 stack overflow")
        self.st[self.top] = value
        self.tag[self.top] = 1

    def fpu_pop(self) -> float:
        if not self.tag[self.top]:
            raise Stop("O3", "x87 stack underflow")
        value = self.st[self.top]
        self.tag[self.top] = 0
        self.top = (self.top + 1) & 7
        return value

    def fpu_get(self, i: int) -> float:
        p = (self.top + i) & 7
        if not self.tag[p]:
            raise Stop("O3", f"x87 stack underflow reading st({i})")
        return self.st[p]

    def fpu_set(self, i: int, value: float) -> None:
        p = (self.top + i) & 7
        self.st[p] = value
        self.tag[p] = 1

    def fpu_free(self, i: int) -> None:
        self.tag[(self.top + i) & 7] = 0

    def fpu_empty(self) -> bool:
        return not any(self.tag)

    def fpu_depth(self) -> int:
        return sum(self.tag)

    def arithmetic_mode(self) -> None:
        if self.cw & 0x0F00 != 0x0200:
            raise Stop("O3", f"arithmetic under control word {self.cw:04X}, not 53-bit precision and round-to-nearest")

    @staticmethod
    def checked(value: float) -> float:
        if value != value or value in (math.inf, -math.inf):
            raise Stop("O3", "a non-finite x87 value (invalid or overflow)")
        if value != 0.0 and abs(value) < SMALLEST_NORMAL:
            raise Stop("O3", "a denormal x87 value")
        return value

    def status_word(self) -> int:
        return (self.top << 11) | (self.c3 << 14) | (self.c2 << 10) | (self.c0 << 8)

    # -- running -------------------------------------------------------------------------------

    def run(self, start: int, stops: Sequence[int], max_steps: int = 50_000_000) -> int:
        """Run from `start` until the address before an instruction is in `stops`; return it."""
        stop_set = set(stops)
        code = self.code
        before, after = self.before, self.after
        eip = start
        steps = 0
        while True:
            if eip in stop_set and steps > 0:
                self.steps += steps
                return eip
            ins = code.get(eip)
            if ins is None:
                raise Stop("O1", "execution left the executable ranges", eip)
            steps += 1
            if steps > max_steps:
                raise Stop("O1", f"more than {max_steps} instructions without reaching a stop", eip)
            self.current = ins
            self.clock += 1
            try:
                if before:
                    hook = before.get(eip)
                    if hook is not None:
                        hook(self, ins)
                target = ins.fn(self, ins)
                if after:
                    hook = after.get(eip)
                    if hook is not None:
                        hook(self, ins)
            except Stop as stop:
                if stop.address is None:
                    stop.address = ins.address
                raise
            eip = ins.nxt if target is None else target


# ---- the decoder ------------------------------------------------------------------------------

def mem_text(mem: Memory, width: int) -> str:
    prefix = {8: "byte ptr ", 16: "word ptr ", 32: "dword ptr ", 64: "qword ptr ", 0: ""}[width]
    return prefix + render_memory(mem)


def rm_text(mem: Optional[Memory], rm: int, width: int) -> str:
    if mem is not None:
        return mem_text(mem, width)
    return {32: REG32, 16: REG16, 8: REG8}[width][rm]


def reg_read(m: Machine, index: int, width: int) -> int:
    if width == 8 and index >= 4:
        return m.reg(index - 4, 8, high=True)
    return m.reg(index, width)


def reg_write(m: Machine, index: int, width: int, value: int) -> None:
    if width == 8 and index >= 4:
        m.set_reg(index - 4, value, 8, high=True)
    else:
        m.set_reg(index, value, width)


def load_int(m: Machine, mem: Optional[Memory], rm: int, width: int) -> int:
    if mem is None:
        return reg_read(m, rm, width)
    return m.read_int(m.effective_address(mem), width // 8)


def store_int(m: Machine, mem: Optional[Memory], rm: int, width: int, value: int) -> None:
    if mem is None:
        reg_write(m, rm, width, value)
    else:
        m.write_int(m.effective_address(mem), width // 8, value)


def reg_name(index: int, width: int) -> str:
    return {32: REG32, 16: REG16, 8: REG8}[width][index]


def alu_add(m: Machine, a: int, b: int, bits: int, carry: int = 0) -> int:
    mask = (1 << bits) - 1
    total = a + b + carry
    result = total & mask
    of = ((a ^ result) & (b ^ result)) >> (bits - 1) & 1
    m.set_flags(result, bits, total >> bits & 1, of)
    return result


def alu_sub(m: Machine, a: int, b: int, bits: int) -> int:
    mask = (1 << bits) - 1
    result = (a - b) & mask
    of = ((a ^ b) & (a ^ result)) >> (bits - 1) & 1
    m.set_flags(result, bits, 1 if a < b else 0, of)
    return result


def alu_logic(m: Machine, result: int, bits: int) -> int:
    m.set_flags(result, bits, 0, 0)
    return result & ((1 << bits) - 1)


ALU = {
    "add": lambda m, a, b, bits: alu_add(m, a, b, bits),
    "adc": lambda m, a, b, bits: alu_add(m, a, b, bits, m.flag("cf")),
    "sub": alu_sub,
    "cmp": alu_sub,
    "and": lambda m, a, b, bits: alu_logic(m, a & b, bits),
    "or": lambda m, a, b, bits: alu_logic(m, a | b, bits),
    "xor": lambda m, a, b, bits: alu_logic(m, a ^ b, bits),
    "test": lambda m, a, b, bits: alu_logic(m, a & b, bits),
}
WRITES_RESULT = {"add": True, "adc": True, "sub": True, "cmp": False, "and": True, "or": True, "xor": True,
                 "test": False}


def exec_alu_rm_r(m: Machine, ins: Ins):
    """op r/m, r  (store form)"""
    name, bits = ins.family, ins.c
    mem, rm, reg = ins.a
    if name == "test" and mem is not None and m.effective_address(mem) < m.reg(4)             and not m.is_defined(m.effective_address(mem), bits // 8):
        # a stack probe (`test [esi],eax` touches a page of the frame about to be allocated, below
        # esp): the slot was never written, so the flags it would set are undefined, and a read of
        # them stops; a test of an undefined slot at or above esp stays an O2 stop
        for flag in m.flags:
            m.flags[flag] = None
        return
    a = load_int(m, mem, rm, bits)
    b = reg_read(m, reg, bits)
    result = ALU[name](m, a, b, bits)
    if WRITES_RESULT[name]:
        store_int(m, mem, rm, bits, result)


def exec_alu_r_rm(m: Machine, ins: Ins):
    """op r, r/m  (load form); `xor r,r` is the zeroing idiom and reads nothing"""
    name, bits = ins.family, ins.c
    mem, rm, reg = ins.a
    if name == "xor" and mem is None and rm == reg:
        reg_write(m, reg, bits, 0)
        m.set_flags(0, bits, 0, 0)
        return
    a = reg_read(m, reg, bits)
    b = load_int(m, mem, rm, bits)
    result = ALU[name](m, a, b, bits)
    if WRITES_RESULT[name]:
        reg_write(m, reg, bits, result)


def exec_alu_rm_imm(m: Machine, ins: Ins):
    name, bits = ins.family, ins.c
    mem, rm = ins.a
    a = load_int(m, mem, rm, bits)
    result = ALU[name](m, a, ins.b & ((1 << bits) - 1), bits)
    if WRITES_RESULT[name]:
        store_int(m, mem, rm, bits, result)


def exec_alu_acc_imm(m: Machine, ins: Ins):
    name, bits = ins.family, ins.c
    a = reg_read(m, 0, bits)
    result = ALU[name](m, a, ins.b & ((1 << bits) - 1), bits)
    if WRITES_RESULT[name]:
        reg_write(m, 0, bits, result)


def exec_mov_rm_r(m: Machine, ins: Ins):
    mem, rm, reg = ins.a
    store_int(m, mem, rm, ins.c, reg_read(m, reg, ins.c))


def exec_mov_r_rm(m: Machine, ins: Ins):
    mem, rm, reg = ins.a
    reg_write(m, reg, ins.c, load_int(m, mem, rm, ins.c))


def exec_mov_r_imm(m: Machine, ins: Ins):
    m.set_reg(ins.a, ins.b)


def exec_mov_rm_imm(m: Machine, ins: Ins):
    mem, rm = ins.a
    store_int(m, mem, rm, 32, ins.b)


def exec_lea(m: Machine, ins: Ins):
    mem, reg = ins.a
    m.set_reg(reg, m.effective_address(mem))


def exec_incdec(m: Machine, ins: Ins):
    value = m.reg(ins.a)
    cf = m.flags["cf"]
    if ins.family == "inc":
        result = alu_add(m, value, 1, 32)
    else:
        result = alu_sub(m, value, 1, 32)
    m.flags["cf"] = cf
    m.set_reg(ins.a, result)


def exec_shift(m: Machine, ins: Ins):
    mem, rm = ins.a
    count = ins.b & 31
    value = load_int(m, mem, rm, 32)
    if count == 0:
        return
    if ins.family == "shl":
        result = (value << count) & MASK32
        cf = (value >> (32 - count)) & 1
        of = ((result >> 31) ^ cf) if count == 1 else None
    else:
        result = (signed(value) >> count) & MASK32
        cf = (signed(value) >> (count - 1)) & 1
        of = 0 if count == 1 else None
    m.set_flags(result, 32, cf, of)
    store_int(m, mem, rm, 32, result)


def exec_neg(m: Machine, ins: Ins):
    mem, rm = ins.a
    value = load_int(m, mem, rm, 32)
    result = alu_sub(m, 0, value, 32)
    store_int(m, mem, rm, 32, result)


def exec_mov_rm8_imm(m: Machine, ins: Ins):
    mem, rm = ins.a
    store_int(m, mem, rm, 8, ins.b)


def exec_imul(m: Machine, ins: Ins):
    mem, rm, reg = ins.a
    product = signed(m.reg(reg)) * signed(load_int(m, mem, rm, 32))
    result = product & MASK32
    overflow = 0 if product == signed(result) else 1
    m.set_flags(result, 32, overflow, overflow)
    m.flags["zf"] = m.flags["sf"] = m.flags["pf"] = None
    m.set_reg(reg, result)


def exec_push_reg(m: Machine, ins: Ins):
    value = m.reg(ins.a)
    esp = (m.reg(4) - 4) & MASK32
    m.set_reg(4, esp)
    m.write_int(esp, 4, value)


def exec_pop_reg(m: Machine, ins: Ins):
    esp = m.reg(4)
    value = m.read_int(esp, 4)
    m.set_reg(4, (esp + 4) & MASK32)
    m.set_reg(ins.a, value)


def exec_ret(m: Machine, ins: Ins):
    """`ret` and `ret imm16` (the callee pops its arguments): to the address the call pushed."""
    esp = m.reg(4)
    target = m.read_int(esp, 4)
    m.set_reg(4, (esp + 4 + (ins.b or 0)) & MASK32)
    return target


def exec_push_imm(m: Machine, ins: Ins):
    esp = (m.reg(4) - 4) & MASK32
    m.set_reg(4, esp)
    m.write_int(esp, 4, ins.a)


def exec_push_mem(m: Machine, ins: Ins):
    mem, rm = ins.a
    value = load_int(m, mem, rm, 32)
    esp = (m.reg(4) - 4) & MASK32
    m.set_reg(4, esp)
    m.write_int(esp, 4, value)


def exec_setcc(m: Machine, ins: Ins):
    mem, rm = ins.a
    store_int(m, mem, rm, 8, 1 if m.condition(ins.b) else 0)


def exec_jcc(m: Machine, ins: Ins):
    if m.condition(ins.b):
        return ins.a


def exec_jmp(m: Machine, ins: Ins):
    return ins.a


def exec_call(m: Machine, ins: Ins):
    """A call runs the target's own instructions when the target lies in an executed range (the
    routine returns through `exec_ret`), else its stub; neither is an O1 stop."""
    stub = m.stubs.get(ins.a)
    if stub is None:
        if ins.a not in m.code:
            raise Stop("O1", f"call to {ins.a:08X}, a target with no stub and outside the executed ranges")
        esp = (m.reg(4) - 4) & MASK32
        m.set_reg(4, esp)
        m.write_int(esp, 4, ins.nxt)
        m.calls.append((ins.address, ins.a))
        return ins.a
    esp = (m.reg(4) - 4) & MASK32
    m.set_reg(4, esp)
    m.write_int(esp, 4, ins.nxt)
    m.calls.append((ins.address, ins.a))
    popped = stub(m) or 0
    m.set_reg(4, (m.reg(4) + 4 + popped) & MASK32)
    m.undefine(1)
    m.undefine(2)


# the x87 operations

def fpu_binary(op: str, a: float, b: float) -> float:
    """a op b, with `a` the left operand: sub is a-b, subr is b-a, div is a/b, divr is b/a."""
    if op == "add":
        return a + b
    if op == "mul":
        return a * b
    if op == "sub":
        return a - b
    if op == "subr":
        return b - a
    if op == "div":
        if b == 0.0:
            raise Stop("O3", "x87 zero-divide")
        return a / b
    if a == 0.0:
        raise Stop("O3", "x87 zero-divide")
    return b / a


def exec_fld_m(m: Machine, ins: Ins):
    mem = ins.a
    address = m.effective_address(mem)
    if ins.c == 32:
        value = m.read_f32(address)
    else:
        value = struct.unpack("<d", m.read_bytes(address, 8))[0]
    m.fpu_push(m.checked(value))


def exec_fild_m(m: Machine, ins: Ins):
    address = m.effective_address(ins.a)
    value = int.from_bytes(m.read_bytes(address, ins.c // 8), "little", signed=True)
    if abs(value) >= (1 << 53):
        raise Stop("O3", "fild of 2^53 or more")
    m.fpu_push(float(value))


def to_binary32(value: float) -> float:
    try:
        packed = struct.pack("<f", value)
    except OverflowError:
        raise Stop("O3", "a binary32 store overflows") from None
    result = struct.unpack("<f", packed)[0]
    if value != 0.0 and abs(result) < SMALLEST_NORMAL32:
        raise Stop("O3", "a binary32 store is denormal or underflows")
    return result


def exec_fst_m(m: Machine, ins: Ins):
    value = m.fpu_get(0)
    address = m.effective_address(ins.a)
    if ins.c == 32:
        if m.cw & 0x0C00:
            raise Stop("O3", "a binary32 store under a rounding control other than nearest")
        m.write_bytes(address, struct.pack("<f", to_binary32(value)))
    else:
        m.write_bytes(address, struct.pack("<d", value))
    if ins.family == "fstp":
        m.fpu_pop()


def exec_fistp_m(m: Machine, ins: Ins):
    value = m.fpu_get(0)
    rounding = (m.cw >> 10) & 3
    if rounding == 0:
        integer = round(value)
    elif rounding == 1:
        integer = math.floor(value)
    elif rounding == 2:
        integer = math.ceil(value)
    else:
        integer = math.trunc(value)
    if not -(1 << 31) <= integer < (1 << 31):
        raise Stop("O3", "fistp out of range (invalid)")
    m.write_int(m.effective_address(ins.a), 4, integer & MASK32)
    m.fpu_pop()


def exec_arith_m(m: Machine, ins: Ins):
    m.arithmetic_mode()
    address = m.effective_address(ins.a)
    if ins.c == 32:
        operand = m.read_f32(address)
    else:
        operand = struct.unpack("<d", m.read_bytes(address, 8))[0]
    m.checked(operand)
    m.fpu_set(0, m.checked(fpu_binary(ins.family, m.fpu_get(0), operand)))


def exec_arith_st(m: Machine, ins: Ins):
    """D8: st(0) = st(0) op st(i)"""
    m.arithmetic_mode()
    m.fpu_set(0, m.checked(fpu_binary(ins.family, m.fpu_get(0), m.fpu_get(ins.a))))


def exec_arith_pop(m: Machine, ins: Ins):
    """DE: st(i) = st(i) op st(0), pop; the family already says which operand is the minuend"""
    m.arithmetic_mode()
    i = ins.a
    m.fpu_set(i, m.checked(fpu_binary(ins.family, m.fpu_get(i), m.fpu_get(0))))
    m.fpu_pop()


def set_compare(m: Machine, left: float, right: float) -> None:
    if left > right:
        m.c0, m.c2, m.c3 = 0, 0, 0
    elif left < right:
        m.c0, m.c2, m.c3 = 1, 0, 0
    else:
        m.c0, m.c2, m.c3 = 0, 0, 1


def exec_fcomp_m(m: Machine, ins: Ins):
    address = m.effective_address(ins.a)
    if ins.c == 32:
        operand = m.read_f32(address)
    else:
        operand = struct.unpack("<d", m.read_bytes(address, 8))[0]
    m.checked(operand)
    set_compare(m, m.fpu_get(0), operand)
    m.fpu_pop()


def exec_fcomp_st(m: Machine, ins: Ins):
    set_compare(m, m.fpu_get(0), m.fpu_get(ins.a))
    m.fpu_pop()


def exec_fld_st(m: Machine, ins: Ins):
    m.fpu_push(m.fpu_get(ins.a))


def exec_fst_st(m: Machine, ins: Ins):
    value = m.fpu_get(0)
    m.fpu_set(ins.a, value)
    if ins.family == "fstp":
        m.fpu_pop()


def exec_fxch(m: Machine, ins: Ins):
    a, b = m.fpu_get(0), m.fpu_get(ins.a)
    m.fpu_set(0, b)
    m.fpu_set(ins.a, a)


def exec_ffree(m: Machine, ins: Ins):
    m.fpu_free(ins.a)


def exec_fabs(m: Machine, ins: Ins):
    m.fpu_set(0, abs(m.fpu_get(0)))


def exec_fincstp(m: Machine, ins: Ins):
    m.top = (m.top + 1) & 7


def exec_fnstsw(m: Machine, ins: Ins):
    m.set_reg(0, m.status_word(), 16)


def exec_fnstcw(m: Machine, ins: Ins):
    m.write_int(m.effective_address(ins.a), 2, m.cw)


def exec_fldcw(m: Machine, ins: Ins):
    m.cw = m.read_int(m.effective_address(ins.a), 2)


def decode(data: bytes, address: int, tables: Dict[str, Dict[int, str]]) -> Ins:
    """The instruction at `address` from its bytes, rendered in dumpbin's text; an opcode outside
    the subset is an O1 stop."""
    reader = Reader(data, address)
    prefix16 = False
    opcode = reader.byte()
    if opcode == 0x66:
        prefix16 = True
        opcode = reader.byte()
    ins = _decode_body(reader, opcode, prefix16, address, tables)
    if not reader.done:
        raise Stop("O1", f"decoded {ins.text!r} but {len(data) - reader.position} bytes remain", address)
    ins.data = data
    ins.length = len(data)
    ins.nxt = address + len(data)
    return ins


def _alu_name(index: int) -> str:
    return ("add", "or", "adc", "sbb", "and", "sub", "xor", "cmp")[index]


def _decode_body(reader: Reader, opcode: int, prefix16: bool, address: int, tables: Dict[str, Dict[int, str]]) -> Ins:
    bits = 16 if prefix16 else 32
    if prefix16 and opcode in (0xD9,):
        return _decode_fpu(reader, opcode, address, tables)
    if 0xD8 <= opcode <= 0xDF:
        return _decode_fpu(reader, opcode, address, tables)
    # the eight-operation block 00-3F: op r/m,r (+1 low bit set: 32-bit) and op r,r/m
    if opcode < 0x40 and (opcode & 7) in (1, 3) and _alu_name(opcode >> 3) in ALU:
        name = _alu_name(opcode >> 3)
        mod, reg, rm, mem = read_modrm(reader)
        if opcode & 2:
            text = f"{name} {reg_name(reg, bits)},{rm_text(mem, rm, bits)}"
            return Ins(address, b"", text, exec_alu_r_rm, (mem, rm, reg), None, bits, name)
        text = f"{name} {rm_text(mem, rm, bits)},{reg_name(reg, bits)}"
        return Ins(address, b"", text, exec_alu_rm_r, (mem, rm, reg), None, bits, name)
    if opcode < 0x40 and (opcode & 7) == 5 and _alu_name(opcode >> 3) in ALU:
        name = _alu_name(opcode >> 3)
        if prefix16:
            value = reader.unsigned16()
            text = f"{name} ax,{render_immediate(value, 16)}"
        else:
            value = reader.unsigned32()
            text = f"{name} eax,{render_immediate(value)}"
        return Ins(address, b"", text, exec_alu_acc_imm, None, value, bits, name)
    if opcode == 0x85:
        mod, reg, rm, mem = read_modrm(reader)
        text = f"test {rm_text(mem, rm, 32)},{reg_name(reg, 32)}"
        return Ins(address, b"", text, exec_alu_rm_r, (mem, rm, reg), None, 32, "test")
    if opcode in (0x80, 0x81, 0x83):
        mod, reg, rm, mem = read_modrm(reader)
        name = _alu_name(reg)
        if name not in ALU:
            raise Stop("O1", f"opcode {opcode:02X} /{reg} is outside the subset", address)
        width = 8 if opcode == 0x80 else bits
        if opcode == 0x81:
            value = reader.unsigned16() if prefix16 else reader.unsigned32()
        else:
            value = reader.signed8() & ((1 << width) - 1) if opcode == 0x80 else signed(reader.byte(), 8) & MASK32
        text = f"{name} {rm_text(mem, rm, width)},{render_immediate(value, width)}"
        return Ins(address, b"", text, exec_alu_rm_imm, (mem, rm), value, width, name)
    if opcode in (0x89, 0x8B):
        mod, reg, rm, mem = read_modrm(reader)
        if opcode == 0x89:
            text = f"mov {rm_text(mem, rm, bits)},{reg_name(reg, bits)}"
            return Ins(address, b"", text, exec_mov_rm_r, (mem, rm, reg), None, bits)
        text = f"mov {reg_name(reg, bits)},{rm_text(mem, rm, bits)}"
        return Ins(address, b"", text, exec_mov_r_rm, (mem, rm, reg), None, bits)
    if 0xB8 <= opcode <= 0xBF:
        value = reader.unsigned32()
        return Ins(address, b"", f"mov {REG32[opcode - 0xB8]},{render_immediate(value)}", exec_mov_r_imm,
                   opcode - 0xB8, value)
    if opcode == 0xC7:
        mod, reg, rm, mem = read_modrm(reader)
        if reg != 0:
            raise Stop("O1", "opcode C7 outside /0", address)
        value = reader.unsigned32()
        text = f"mov {rm_text(mem, rm, 32)},{render_immediate(value)}"
        return Ins(address, b"", text, exec_mov_rm_imm, (mem, rm), value)
    if opcode == 0xC6:
        mod, reg, rm, mem = read_modrm(reader)
        if reg != 0:
            raise Stop("O1", "opcode C6 outside /0", address)
        value = reader.byte()
        text = f"mov {rm_text(mem, rm, 8)},{render_immediate(value, 8)}"
        return Ins(address, b"", text, exec_mov_rm8_imm, (mem, rm), value)
    if opcode == 0xF7:
        mod, reg, rm, mem = read_modrm(reader)
        if reg != 3:
            raise Stop("O1", f"opcode F7 /{reg} is outside the subset", address)
        return Ins(address, b"", f"neg {rm_text(mem, rm, 32)}", exec_neg, (mem, rm))
    if opcode == 0x8D:
        mod, reg, rm, mem = read_modrm(reader)
        if mem is None:
            raise Stop("O1", "lea of a register", address)
        return Ins(address, b"", f"lea {REG32[reg]},{render_memory(mem)}", exec_lea, (mem, reg))
    if 0x40 <= opcode <= 0x4F:
        name = "inc" if opcode < 0x48 else "dec"
        index = opcode & 7
        return Ins(address, b"", f"{name} {REG32[index]}", exec_incdec, index, None, None, name)
    if 0x50 <= opcode <= 0x57:
        return Ins(address, b"", f"push {REG32[opcode - 0x50]}", exec_push_reg, opcode - 0x50)
    if 0x58 <= opcode <= 0x5F:
        return Ins(address, b"", f"pop {REG32[opcode - 0x58]}", exec_pop_reg, opcode - 0x58)
    if opcode == 0xC3:
        return Ins(address, b"", "ret", exec_ret, None, 0)
    if opcode == 0xC2:
        count = reader.unsigned16()
        return Ins(address, b"", f"ret {render_immediate(count, 16)}", exec_ret, None, count)
    if opcode == 0x68:
        value = reader.unsigned32()
        return Ins(address, b"", f"push {render_immediate(value)}", exec_push_imm, value)
    if opcode == 0xFF:
        mod, reg, rm, mem = read_modrm(reader)
        if reg != 6:
            raise Stop("O1", "opcode FF outside /6 (push)", address)
        return Ins(address, b"", f"push {rm_text(mem, rm, 32)}", exec_push_mem, (mem, rm))
    if opcode in (0xC1, 0xD1):
        mod, reg, rm, mem = read_modrm(reader)
        if reg not in (4, 7):
            raise Stop("O1", f"shift /{reg} is outside the subset", address)
        count = reader.byte() if opcode == 0xC1 else 1
        name = "shl" if reg == 4 else "sar"
        text = f"{name} {rm_text(mem, rm, 32)},{render_immediate(count)}"
        return Ins(address, b"", text, exec_shift, (mem, rm), count, None, name)
    if opcode == 0x0F:
        second = reader.byte()
        if second == 0xAF:
            mod, reg, rm, mem = read_modrm(reader)
            text = f"imul {REG32[reg]},{rm_text(mem, rm, 32)}"
            return Ins(address, b"", text, exec_imul, (mem, rm, reg))
        if 0x90 <= second <= 0x9F:
            mod, reg, rm, mem = read_modrm(reader)
            cc = second & 15
            text = f"set{CONDITIONS[cc]} {rm_text(mem, rm, 8)}"
            return Ins(address, b"", text, exec_setcc, (mem, rm), cc)
        if 0x80 <= second <= 0x8F:
            rel = reader.signed32()
            cc = second & 15
            target = (address + reader.position + rel) & MASK32
            return Ins(address, b"", f"j{CONDITIONS[cc]} {target:08X}", exec_jcc, target, cc)
        raise Stop("O1", f"opcode 0F {second:02X} is outside the subset", address)
    if 0x70 <= opcode <= 0x7F:
        rel = reader.signed8()
        cc = opcode & 15
        target = (address + reader.position + rel) & MASK32
        return Ins(address, b"", f"j{CONDITIONS[cc]} {target:08X}", exec_jcc, target, cc)
    if opcode in (0xE9, 0xEB, 0xE8):
        rel = reader.signed32() if opcode != 0xEB else reader.signed8()
        target = (address + reader.position + rel) & MASK32
        if opcode == 0xE8:
            return Ins(address, b"", f"call {target:08X}", exec_call, target)
        return Ins(address, b"", f"jmp {target:08X}", exec_jmp, target)
    raise Stop("O1", f"opcode {opcode:02X} is outside the subset", address)


def _decode_fpu(reader: Reader, opcode: int, address: int, tables: Dict[str, Dict[int, str]]) -> Ins:
    peeked = reader.peek()
    mod = peeked >> 6
    if mod != 3:
        _, reg, _, mem = read_modrm(reader)
        return _decode_fpu_memory(opcode, reg, mem, address, tables)
    second = reader.byte()
    reg, i = (second >> 3) & 7, second & 7
    if opcode == 0xD8:
        if reg in tables["D8"]:
            name = tables["D8"][reg]
            return Ins(address, b"", f"f{name} st,st({i})", exec_arith_st, i, None, None, name)
        if reg == 3:
            return Ins(address, b"", f"fcomp st({i})", exec_fcomp_st, i)
    if opcode == 0xDE and reg in tables["DE"]:
        name = tables["DE"][reg]
        return Ins(address, b"", f"f{name}p st({i}),st", exec_arith_pop, i, None, None, name)
    if opcode == 0xD9:
        if reg == 0:
            return Ins(address, b"", f"fld st({i})", exec_fld_st, i)
        if reg == 1:
            return Ins(address, b"", f"fxch st({i})", exec_fxch, i)
        if second == 0xE1:
            return Ins(address, b"", "fabs", exec_fabs)
        if second == 0xF7:
            return Ins(address, b"", "fincstp", exec_fincstp)
    if opcode == 0xDD:
        if reg == 0:
            return Ins(address, b"", f"ffree st({i})", exec_ffree, i)
        if reg == 2:
            return Ins(address, b"", f"fst st({i})", exec_fst_st, i, None, None, "fst")
        if reg == 3:
            return Ins(address, b"", f"fstp st({i})", exec_fst_st, i, None, None, "fstp")
    if opcode == 0xDF and second == 0xE0:
        return Ins(address, b"", "fnstsw ax", exec_fnstsw)
    raise Stop("O1", f"x87 opcode {opcode:02X} {second:02X} is outside the subset", address)


def _decode_fpu_memory(opcode: int, reg: int, mem: Memory, address: int, tables: Dict[str, Dict[int, str]]) -> Ins:
    if opcode in (0xD8, 0xDC):
        width = 32 if opcode == 0xD8 else 64
        if reg in tables["D8"]:
            name = tables["D8"][reg]
            return Ins(address, b"", f"f{name} {mem_text(mem, width)}", exec_arith_m, mem, None, width, name)
        if reg == 3:
            return Ins(address, b"", f"fcomp {mem_text(mem, width)}", exec_fcomp_m, mem, None, width)
    if opcode == 0xD9:
        if reg == 0:
            return Ins(address, b"", f"fld {mem_text(mem, 32)}", exec_fld_m, mem, None, 32)
        if reg in (2, 3):
            name = "fst" if reg == 2 else "fstp"
            return Ins(address, b"", f"{name} {mem_text(mem, 32)}", exec_fst_m, mem, None, 32, name)
        if reg == 5:
            return Ins(address, b"", f"fldcw {mem_text(mem, 16)}", exec_fldcw, mem)
        if reg == 7:
            return Ins(address, b"", f"fnstcw {mem_text(mem, 16)}", exec_fnstcw, mem)
    if opcode == 0xDD:
        if reg == 0:
            return Ins(address, b"", f"fld {mem_text(mem, 64)}", exec_fld_m, mem, None, 64)
        if reg in (2, 3):
            name = "fst" if reg == 2 else "fstp"
            return Ins(address, b"", f"{name} {mem_text(mem, 64)}", exec_fst_m, mem, None, 64, name)
    if opcode == 0xDB:
        if reg == 0:
            return Ins(address, b"", f"fild {mem_text(mem, 32)}", exec_fild_m, mem, None, 32)
        if reg == 3:
            return Ins(address, b"", f"fistp {mem_text(mem, 32)}", exec_fistp_m, mem)
    if opcode == 0xDF and reg == 5:
        return Ins(address, b"", f"fild {mem_text(mem, 64)}", exec_fild_m, mem, None, 64)
    raise Stop("O1", f"x87 memory opcode {opcode:02X} /{reg} is outside the subset", address)


# ---- the self-test ----------------------------------------------------------------------------

def synthetic_listing(lines: Sequence[Tuple[int, str, str]]) -> str:
    """A listing text for `Machine(listing_text=...)`: (address, bytes in hex, dumpbin text)."""
    out = []
    for address, data, text in lines:
        raw = " ".join(data[i:i + 2] for i in range(0, len(data), 2)).upper()
        out.append(f"  {address:08X}: {raw:<18}{text}")
    return "\n".join(out) + "\n"

def run_synthetic(lines: Sequence[Tuple[int, str, str]], prepare: Callable[[Machine], None],
                  last: int) -> Machine:
    machine = Machine([(lines[0][0], last)], listing_text=synthetic_listing(lines))
    machine.add_region("scratch", 0x00800000, 0x1000)
    machine.set_reg(5, 0x00800800)
    machine.set_reg(4, 0x00800800)
    prepare(machine)
    machine.run(lines[0][0], [last])
    return machine


def selftest() -> int:
    """The decoder, the cross-check and the semantics proven on the committed excerpt and on tiny
    programs; every check is also seen red on a break of the thing it guards."""
    problems: List[str] = []

    def expect(name: str, ok: bool, detail: str = "") -> None:
        print(f"selftest: {'green' if ok else 'RED  '} {name}{(': ' + detail) if detail else ''}")
        if not ok:
            problems.append(name)

    ranges = [(0x406754, 0x409A10), (0x40BD9C, 0x41058F)]
    machine = Machine(ranges)
    expect("every decoded instruction of the executed ranges renders to dumpbin's text",
           not machine.cross_check_mismatches and len(machine.code) > 8000, f"{len(machine.code)} instructions")

    swapped = {"D8": D8_FAMILY, "DE": {0: "add", 1: "mul", 4: "sub", 5: "subr", 6: "div", 7: "divr"}}
    try:
        Machine(ranges, tables=swapped)
        expect("r1: a decoder with DE E0+i and DE E8+i swapped fails the cross-check", False, "it did not")
    except Stop as stop:
        sites = str(stop).count("fsubrp")
        expect("r1: a decoder with DE E0+i and DE E8+i swapped fails the cross-check",
               stop.code == "O1" and "fails at 8 instructions" in str(stop) and sites >= 1, str(stop)[:110])

    try:
        Machine(ranges, byte_edits={0x40EB31: 0x05})
        expect("r2: a ModRM byte edited in memory (0040EB31 0D to 05) fails the cross-check", False, "it did not")
    except Stop as stop:
        expect("r2: a ModRM byte edited in memory (0040EB31 0D to 05) fails the cross-check",
               stop.code == "O1" and "0040EB30" in str(stop), str(stop)[:110])

    def prepare_subtract(m: Machine) -> None:
        m.write_bytes(0x00800000, struct.pack("<dd", 5.0, 2.0))

    program = [
        (0x1000, "DD0500008000", "fld qword ptr ds:[00800000h]"),
        (0x1006, "DD0508008000", "fld qword ptr ds:[00800008h]"),
        (0x100C, "DEE1", "fsubrp st(1),st"),
        (0x100E, "DD1D10008000", "fstp qword ptr ds:[00800010h]"),
        (0x1014, "90", "nop"),
    ]
    try:
        Machine([(0x1000, 0x1015)], listing_text=synthetic_listing(program))
        expect("an opcode outside the subset is a stop", False, "it did not")
    except Stop as stop:
        expect("an opcode outside the subset is a stop", stop.code == "O1", str(stop))
    program = program[:-1]
    machine = run_synthetic(program, prepare_subtract, 0x1014)
    value = struct.unpack("<d", machine.read_bytes(0x00800010, 8))[0]
    expect("DE E1 (fsubrp st(1),st) leaves st(0) - st(1) of the two loads: 2 - 5", value == -3.0, repr(value))

    program = [
        (0x1000, "D9050000" "8000", "fld dword ptr ds:[00800000h]"),
        (0x1006, "D91D04008000", "fstp dword ptr ds:[00800004h]"),
        (0x100C, "D90504008000", "fld dword ptr ds:[00800004h]"),
        (0x1012, "DD5D00", "fstp qword ptr [ebp+0]"),
    ]
    program = program[:3]
    machine = run_synthetic(program, lambda m: m.write_bytes(0x00800000, struct.pack("<f", 0.1)), 0x1012)
    expect("a binary32 store then reload returns the rounded value",
           machine.st[machine.top] == struct.unpack("<f", struct.pack("<f", 0.1))[0] and machine.tag[machine.top] == 1)

    unread = Machine([(0x1000, 0x1006)], listing_text=synthetic_listing(
        [(0x1000, "D90500008000", "fld dword ptr ds:[00800000h]")]))
    unread.add_region("scratch", 0x00800000, 0x1000)
    try:
        unread.run(0x1000, [0x1006])
        expect("a read of an undefined byte is an O2 stop", False, "it did not")
    except Stop as stop:
        expect("a read of an undefined byte is an O2 stop", stop.code == "O2", str(stop))

    divide = Machine([(0x1000, 0x1012)], listing_text=synthetic_listing([
        (0x1000, "D90500008000", "fld dword ptr ds:[00800000h]"),
        (0x1006, "D83504008000", "fdiv dword ptr ds:[00800004h]"),
        (0x100C, "D91D08008000", "fstp dword ptr ds:[00800008h]"),
    ]))
    divide.add_region("scratch", 0x00800000, 0x1000)
    divide.write_bytes(0x00800000, struct.pack("<ff", 1.0, 0.0))
    try:
        divide.run(0x1000, [0x1012])
        expect("a zero-divide is an O3 stop", False, "it did not")
    except Stop as stop:
        expect("a zero-divide is an O3 stop", stop.code == "O3", str(stop))

    caller = [
        (0x1000, "6805000000", "push 5"),
        (0x1005, "E806000000", "call 00001010"),
        (0x100A, "89C3", "mov ebx,eax"),
    ]
    callee = [
        (0x1010, "55", "push ebp"),
        (0x1011, "8BEC", "mov ebp,esp"),
        (0x1013, "8B4508", "mov eax,dword ptr [ebp+8]"),
        (0x1016, "5D", "pop ebp"),
        (0x1017, "C20400", "ret 4"),
    ]
    both = Machine([(0x1000, 0x1020)], listing_text=synthetic_listing(caller + callee))
    both.add_region("scratch", 0x00800000, 0x1000)
    both.set_reg(5, 0x00800800)
    both.set_reg(4, 0x00800800)
    both.run(0x1000, [0x100C])
    expect("a call to an instruction of an executed range runs the routine and its `ret 4` pops the argument",
           both.reg(3) == 5 and both.reg(4) == 0x00800800 and both.reg(5) == 0x00800800
           and both.calls == [(0x1005, 0x1010)], f"ebx {both.reg(3)}, esp {both.reg(4):08X}")
    alone = Machine([(0x1000, 0x100C)], listing_text=synthetic_listing(caller))
    alone.add_region("scratch", 0x00800000, 0x1000)
    alone.set_reg(5, 0x00800800)
    alone.set_reg(4, 0x00800800)
    try:
        alone.run(0x1000, [0x100C])
        expect("a call to a target outside the executed ranges with no stub is an O1 stop", False, "it did not")
    except Stop as stop:
        expect("a call to a target outside the executed ranges with no stub is an O1 stop", stop.code == "O1",
               str(stop))

    def probe(sign: str, ending: str):
        lines = [
            (0x1000, "8BF4", "mov esi,esp"),
            (0x1002, "83EE10" if sign == "below" else "83C610", "sub esi,10h" if sign == "below" else "add esi,10h"),
            (0x1005, "8506", "test dword ptr [esi],eax"),
            (0x1007, ending, "je 00001000" if ending == "74F7" else "mov ecx,eax"),
        ]
        m = Machine([(0x1000, 0x1009)], listing_text=synthetic_listing(lines))
        m.add_region("scratch", 0x00800000, 0x1000)
        m.set_reg(4, 0x00800800)
        return m

    below = probe("below", "74F7")
    below.run(0x1000, [0x1007])
    expect("a test of the never-written stack below esp (a stack probe) leaves every flag undefined and reads eax not",
           all(value is None for value in below.flags.values()))
    try:
        below.run(0x1007, [0x1009])
        expect("a branch on the flags a stack probe left undefined is an O2 stop", False, "it did not")
    except Stop as stop:
        expect("a branch on the flags a stack probe left undefined is an O2 stop", stop.code == "O2", str(stop))
    above = probe("above", "89C1")
    try:
        above.run(0x1000, [0x1009])
        expect("a test of an undefined slot at or above esp is still an O2 stop", False, "it did not")
    except Stop as stop:
        expect("a test of an undefined slot at or above esp is still an O2 stop", stop.code == "O2", str(stop))

    print(f"selftest: {len(problems)} red")
    return 1 if problems else 0


if __name__ == "__main__":
    import sys
    raise SystemExit(selftest() if sys.argv[1:] == ["selftest"] else 2)
