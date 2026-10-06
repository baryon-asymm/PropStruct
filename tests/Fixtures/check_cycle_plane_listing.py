"""The listing excerpt of the executable's per-cycle plane, and its byte check.

`PropStructV3.cycle-plane.listing.txt` holds `dumpbin /disasm /range` output of
`PropStructV3.exe` over the ranges of `RANGES` below, unedited, under a header that
records the command, the tool version and the SHA-256 of the executable and of
`dforrt.dll` (`API.md`, "## Cycle-plane listing"); the three files lie outside the
repository, in the directory `PROPSTRUCT_LEGACY_DIR` names (`tools/legacy`). Everything that reads the excerpt trusts
it only because `verify` proves it: every instruction's bytes equal the executable's bytes
at its address, read through the PE section table, so no dumpbin is needed to check it.

    python check_cycle_plane_listing.py verify     # no dumpbin: header, ranges, every instruction's bytes
    python check_cycle_plane_listing.py selftest   # verify must go red on an edited byte, a moved
                                                   # address, a dropped instruction and a stale digest
    python check_cycle_plane_listing.py extract [dumpbin.exe]   # the only step that needs dumpbin

Python 3.8+, standard library only.
"""

from __future__ import annotations

import hashlib
import os
import re
import struct
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, NamedTuple, Optional, Tuple

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "tools" / "legacy"))
import legacy  # noqa: E402  (the one reader of PROPSTRUCT_LEGACY_DIR, tools/legacy/API.md)

EXE_NAME = "PropStructV3.exe"
DLL_NAME = "dforrt.dll"
LISTING_NAME = "PropStructV3.cycle-plane.listing.txt"
TOOL_VERSION = "14.12.25835.0"


# The three files lie outside the repository (tools/legacy). Each path is resolved, and the
# file compared with the manifest, at the call, so a command that needs none runs without
# PROPSTRUCT_LEGACY_DIR.
def exe_path() -> Path:
    return Path(legacy.legacy_file(EXE_NAME))


def dll_path() -> Path:
    return Path(legacy.legacy_file(DLL_NAME))


def listing_path() -> Path:
    return Path(legacy.legacy_file(LISTING_NAME))

# (first, end exclusive, what it is for). The oracle (src/Statistics/ACCEPTANCE.md, A2) executes the
# pre-loop and the plane; the others are read, never executed: the prologue's frame, the ALLOCATE
# sites' descriptor layout and the prelude's slots (the local array descriptors the plane reads), the
# skipped region's writes (the static scan), PARAM (executed through its call, from the pre-loop's
# call at 0x408A8E), the runtime thunks and the C start-up's precision control.
RANGES: List[Tuple[int, int, str]] = [
    (0x401000, 0x40100C, "prologue: push ebp, mov ebp,esp, sub esp (the frame)"),
    (0x401F92, 0x408A93, "ALLOCATE sites, the local array descriptors and slots: read for layout, never executed"),
    (0x408A93, 0x409A10, "pre-loop, executed by the oracle: line 378, the hoisted block, the B6 flags"),
    (0x409A10, 0x40BD9E, "cycle head and particle loop, skipped by the oracle, scanned for its writes"),
    (0x40BD9E, 0x41058F, "the per-cycle plane 771-1175, executed by the oracle"),
    (0x4181A5, 0x418577, "PARAM, executed by the oracle through its call: ZSS, ZX, Z11 (SIZE, the next routine, is a stub)"),
    (0x418BA4, 0x418C22, "runtime thunks: the seven calls the plane makes"),
    (0x418CE1, 0x418D8B, "C start-up: _controlfp(0x10000, 0x30000)"),
]

INSTRUCTION = re.compile(r"^  ([0-9A-F]{8}): ((?:[0-9A-F]{2} )*[0-9A-F]{2})(?:\s+(\S.*))?$")
CONTINUATION = re.compile(r"^ {10,}((?:[0-9A-F]{2} )*[0-9A-F]{2})$")
RANGE_LINE = re.compile(r"^# range 0x([0-9A-F]{6})-0x([0-9A-F]{6}) (.*)$")
DIGEST_LINE = re.compile(r"^# (exe|dll) sha256 ([0-9a-f]{64})$")


class Instruction(NamedTuple):
    address: int
    data: bytes
    text: str


class Listing(NamedTuple):
    exe_digest: str
    dll_digest: str
    tool_version: str
    ranges: List[Tuple[int, int, str]]
    instructions: List[Instruction]


class Section(NamedTuple):
    name: str
    address: int
    size: int
    raw_size: int
    raw_offset: int


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def sections(image: bytes) -> List[Section]:
    pe = struct.unpack_from("<I", image, 0x3C)[0]
    count = struct.unpack_from("<H", image, pe + 6)[0]
    optional = struct.unpack_from("<H", image, pe + 20)[0]
    base = struct.unpack_from("<I", image, pe + 24 + 28)[0]
    found = []
    offset = pe + 24 + optional
    for _ in range(count):
        name = image[offset:offset + 8].rstrip(b"\0").decode("ascii")
        size, rva, raw_size, raw_offset = struct.unpack_from("<IIII", image, offset + 8)
        found.append(Section(name, base + rva, size, raw_size, raw_offset))
        offset += 40
    return found


def image_bytes(image: bytes, table: List[Section], address: int, length: int) -> Optional[bytes]:
    """The `length` bytes at virtual `address` as the file holds them, or None when they are
    not all file-backed inside one section."""
    for section in table:
        if section.address <= address < section.address + max(section.size, section.raw_size):
            start = address - section.address
            if start + length <= section.raw_size:
                return image[section.raw_offset + start:section.raw_offset + start + length]
            return None
    return None


def parse(text: str) -> Listing:
    exe = dll = ""
    tool = ""
    ranges: List[Tuple[int, int, str]] = []
    instructions: List[Instruction] = []
    for number, line in enumerate(text.split("\n"), start=1):
        if not line:
            continue
        if line.startswith("#"):
            match = DIGEST_LINE.match(line)
            if match:
                if match.group(1) == "exe":
                    exe = match.group(2)
                else:
                    dll = match.group(2)
                continue
            match = RANGE_LINE.match(line)
            if match:
                ranges.append((int(match.group(1), 16), int(match.group(2), 16), match.group(3)))
                continue
            if line.startswith("# tool "):
                tool = line[len("# tool "):]
            continue
        match = INSTRUCTION.match(line)
        if match:
            data = bytes.fromhex(match.group(2).replace(" ", ""))
            instructions.append(Instruction(int(match.group(1), 16), data, (match.group(3) or "").strip()))
            continue
        match = CONTINUATION.match(line)
        if match and instructions:
            last = instructions[-1]
            instructions[-1] = Instruction(last.address, last.data + bytes.fromhex(match.group(1).replace(" ", "")),
                                           last.text)
            continue
        raise SystemExit(f"listing line {number} is neither a header, an instruction nor a continuation: {line!r}")
    return Listing(exe, dll, tool, ranges, instructions)


def verify_listing(listing: Listing, image: bytes, exe_digest: str, dll_digest: str) -> List[str]:
    """Every way the excerpt can fail to be the executable's bytes; empty when it is."""
    problems: List[str] = []
    if listing.exe_digest != exe_digest:
        problems.append(f"header exe sha256 {listing.exe_digest} is not the executable's {exe_digest}")
    if listing.dll_digest != dll_digest:
        problems.append(f"header dll sha256 {listing.dll_digest} is not the runtime's {dll_digest}")
    if listing.tool_version != f"dumpbin {TOOL_VERSION}":
        problems.append(f"header tool is {listing.tool_version!r}, not dumpbin {TOOL_VERSION}")
    if listing.ranges != RANGES:
        problems.append("header ranges are not the script's RANGES")
    table = sections(image)
    expected: Optional[int] = None
    range_index = -1
    for ins in listing.instructions:
        if expected is None or ins.address != expected:
            range_index += 1
            if range_index >= len(listing.ranges) or ins.address != listing.ranges[range_index][0]:
                problems.append(f"{ins.address:08X}: not the start of the next range and not contiguous with the "
                                "instruction before it")
                return problems
        expected = ins.address + len(ins.data)
        if not ins.text:
            problems.append(f"{ins.address:08X}: an instruction without its mnemonic (a cut instruction)")
        held = image_bytes(image, table, ins.address, len(ins.data))
        if held is None:
            problems.append(f"{ins.address:08X}: not file-backed in one section of the executable")
        elif held != ins.data:
            problems.append(f"{ins.address:08X}: listing bytes {ins.data.hex(' ')} but the executable holds "
                            f"{held.hex(' ')}")
        if range_index < len(listing.ranges) and expected == listing.ranges[range_index][1]:
            expected = None
    if expected is not None:
        problems.append("the last range ends inside an instruction")
    if range_index != len(listing.ranges) - 1:
        problems.append(f"{range_index + 1} ranges present, {len(listing.ranges)} declared")
    return problems


def verify() -> int:
    return verify_file(listing_path())


def verify_file(path: Path) -> int:
    listing = parse(path.read_text(encoding="utf-8"))
    problems = verify_listing(listing, exe_path().read_bytes(), sha256(exe_path()), sha256(dll_path()))
    if problems:
        for problem in problems[:20]:
            print(f"verify: {problem}", file=sys.stderr)
        print(f"verify: {len(problems)} problems", file=sys.stderr)
        return 1
    print(f"verify: {len(listing.instructions)} instructions in {len(listing.ranges)} ranges equal the "
          "executable's bytes")
    return 0


def selftest() -> int:
    """`verify_listing` must go red on each violation, and stay green on the real excerpt."""
    listing = parse(listing_path().read_text(encoding="utf-8"))
    image = exe_path().read_bytes()
    exe_digest, dll_digest = sha256(exe_path()), sha256(dll_path())
    if verify_listing(listing, image, exe_digest, dll_digest):
        print("selftest: the committed excerpt does not verify", file=sys.stderr)
        return 1
    middle = len(listing.instructions) // 2
    victim = listing.instructions[middle]
    edited = bytes([victim.data[0] ^ 0x01]) + victim.data[1:]
    mutations = {
        "an edited byte": Listing(listing.exe_digest, listing.dll_digest, listing.tool_version, listing.ranges,
                                  listing.instructions[:middle] + [Instruction(victim.address, edited, victim.text)]
                                  + listing.instructions[middle + 1:]),
        "a moved address": Listing(listing.exe_digest, listing.dll_digest, listing.tool_version, listing.ranges,
                                   listing.instructions[:middle]
                                   + [Instruction(victim.address + 1, victim.data, victim.text)]
                                   + listing.instructions[middle + 1:]),
        "a dropped instruction": Listing(listing.exe_digest, listing.dll_digest, listing.tool_version,
                                         listing.ranges,
                                         listing.instructions[:middle] + listing.instructions[middle + 1:]),
        "a stale executable digest": Listing("0" * 64, listing.dll_digest, listing.tool_version, listing.ranges,
                                             listing.instructions),
    }
    status = 0
    for name, mutated in mutations.items():
        problems = verify_listing(mutated, image, exe_digest, dll_digest)
        if not problems:
            print(f"selftest: verify stayed green on {name}", file=sys.stderr)
            status = 1
        else:
            print(f"selftest: red on {name}: {problems[0]}")
    with tempfile.TemporaryDirectory(prefix="propstruct_listing_") as temporary:
        for name, destination, expected in (
                ("an output file inside the original's directory", Path(legacy.legacy_dir()) / "other.txt", True),
                ("the recorded excerpt itself", listing_path(), True),
                ("an output file outside it", Path(temporary) / LISTING_NAME, False)):
            if inside_the_original_directory(destination) != expected:
                print(f"selftest: extract's guard is wrong on {name}", file=sys.stderr)
                status = 1
            else:
                print(f"selftest: extract {'refuses' if expected else 'accepts'} {name}")
    return status


def dumpbin_path(explicit: Optional[str]) -> str:
    if explicit:
        return explicit
    if os.environ.get("DUMPBIN"):
        return os.environ["DUMPBIN"]
    raise SystemExit("extract: pass the dumpbin.exe of version " + TOOL_VERSION + " or set DUMPBIN")


def run_range(dumpbin: str, first: int, end: int) -> List[str]:
    """dumpbin's instruction lines for [first, end), a cut last instruction dropped."""
    env = dict(os.environ, MSYS_NO_PATHCONV="1")
    command = [dumpbin, "/disasm", f"/range:0x{first:X},0x{end - 1:X}", str(exe_path())]
    output = subprocess.run(command, check=True, capture_output=True, text=True, env=env).stdout
    lines = [line for line in output.split("\n") if INSTRUCTION.match(line) or CONTINUATION.match(line)]
    # an instruction that dumpbin cut at the range's last byte has bytes and no mnemonic
    while lines and INSTRUCTION.match(lines[-1]) and not INSTRUCTION.match(lines[-1]).group(3):
        lines.pop()
    return lines


def inside_the_original_directory(destination: Path) -> bool:
    """Whether `destination` lies in PROPSTRUCT_LEGACY_DIR, where the recorded files are."""
    directory = os.path.normcase(str(Path(legacy.legacy_dir()).resolve()))
    target = os.path.normcase(str(destination.resolve()))
    try:
        return os.path.commonpath([directory, target]) == directory
    except ValueError:  # another drive
        return False


def extract(output: str, explicit: Optional[str]) -> int:
    destination = Path(output)
    if inside_the_original_directory(destination):
        raise SystemExit(f"extract: {destination} lies inside PROPSTRUCT_LEGACY_DIR, whose files are the recorded "
                         "original and are never overwritten; write elsewhere and compare the digest with "
                         "tools/legacy/original.sha256")
    dumpbin = dumpbin_path(explicit)
    version = subprocess.run([dumpbin], capture_output=True, text=True).stdout.split("\n")[0]
    if TOOL_VERSION not in version:
        raise SystemExit(f"extract: {dumpbin} is {version!r}, not version {TOOL_VERSION}")
    out = [
        "# The executable's listing over the per-cycle plane and what the oracle and the map need around it.",
        "# Written by check_cycle_plane_listing.py extract; checked by `verify`, which needs no dumpbin.",
        f"# tool dumpbin {TOOL_VERSION}",
        "# command dumpbin /disasm /range:<first>,<last> tests/Fixtures/Legacy/PropStructV3.exe (MSYS_NO_PATHCONV=1)",
        f"# exe sha256 {sha256(exe_path())}",
        f"# dll sha256 {sha256(dll_path())}",
    ]
    for first, end, what in RANGES:
        out.append(f"# range 0x{first:06X}-0x{end:06X} {what}")
    for first, end, what in RANGES:
        out.append(f"# {what}")
        out.extend(run_range(dumpbin, first, end))
    # The excerpt is written outside the original's directory and outside the repository; the
    # manifest records a new hash only by an owner's decision (tools/legacy).
    destination.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    print(f"wrote {destination}, sha256 {sha256(destination)}")
    return verify_file(destination)


def main(argv: List[str]) -> int:
    if argv and argv[0] == "verify" and len(argv) == 1:
        return verify()
    if argv and argv[0] == "selftest" and len(argv) == 1:
        return selftest()
    if argv and argv[0] == "extract" and len(argv) in (2, 3):
        return extract(argv[1], argv[2] if len(argv) == 3 else None)
    print("usage: check_cycle_plane_listing.py verify | selftest | extract <output> [dumpbin.exe]", file=sys.stderr)
    return 2


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
