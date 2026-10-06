"""Classifies every site of the per-cycle plane (Fortran 771-1175, the hoisted block and line
378) from the executable's own listing into `CyclePlane.listing.generated.txt`.

What it reads: the excerpt `PropStructV3.cycle-plane.listing.txt`, outside the repository
(`tools/legacy`; through `x87_listing.py`, the block-local x87 stack reader), the hand-written address map
`CyclePlaneListing.map.txt` (through `listing_map.py` and `listing_checks.py`; its checks run
first, and a red check is a stop, never a skip) and the Fortran source (through
`fortran_source.py`, for the typing of every name and the text of every line). It decides what
each site does: which REAL*4 stores round, which reads take the register and which the home,
which sums reload and store on which pass, in what order products are formed. This table drives
the code (`CycleStatistics`, `Categories`, `SmallParticles`, `Setup`) and the twin.

    python classify-cycle-plane-listing.py check      # the map's checks only
    python classify-cycle-plane-listing.py generate   # (re)writes CyclePlane.listing.generated.txt
    python classify-cycle-plane-listing.py verify     # regenerates in memory, fails on any byte difference
    python classify-cycle-plane-listing.py selftest   # the checks' and the generator's own proofs

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path
from typing import Callable, Dict, List, Optional, Tuple

NODE_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(NODE_ROOT))
import listing_checks as lc  # noqa: E402
import listing_map as lm  # noqa: E402
import listing_sites as ls  # noqa: E402
import x87_listing as xl  # noqa: E402

SCRIPT_NAME = "classify-cycle-plane-listing.py"
OUTPUT_PATH = NODE_ROOT / "CyclePlane.listing.generated.txt"
COLUMNS = ("at", "row", "lines", "target", "type", "kind", "rounds", "home", "count", "reads", "schedule", "order")


def check_command() -> int:
    program = lc.load()
    for problem in program.problems:
        print(problem)
    if program.problems:
        print(f"{SCRIPT_NAME}: check: {len(program.problems)} problems")
        return 1
    rows = len(program.map.rows)
    print(f"{SCRIPT_NAME}: check: {rows} rows, {len(program.map.cells)} cells, {len(program.map.arrays)} arrays, "
          f"{len(program.map.runs)} integer runs, {len(program.map.inputs)} inputs, "
          f"{len(program.resolved)} events resolved, 0 problems")
    return 0


# ---- the proofs of the map's checks ------------------------------------------------

class Mutation:
    """One break of the map or the excerpt and the check it must turn red."""

    def __init__(self, what: str, expect: str, edit: Callable[[str], str] = None,
                 instructions: Callable[[List[xl.Ins]], List[xl.Ins]] = None, depth: Dict[int, int] = None):
        self.what = what
        self.expect = expect                  # a substring every red run must show
        self.edit = edit
        self.instructions = instructions
        self.depth = depth                    # thunk address -> x87 depth, overriding the contract


def replace_once(old: str, new: str) -> Callable[[str], str]:
    def edit(text: str) -> str:
        if text.count(old) != 1:
            raise lm.MapError(f"selftest: {old!r} occurs {text.count(old)} times in the map")
        return text.replace(old, new)
    return edit


def drop_line(prefix: str) -> Callable[[str], str]:
    def edit(text: str) -> str:
        kept = [line for line in text.split("\n") if not line.startswith(prefix)]
        if len(kept) != len(text.split("\n")) - 1:
            raise lm.MapError(f"selftest: {prefix!r} does not start exactly one line of the map")
        return "\n".join(kept)
    return edit


def retarget_call(instructions: List[xl.Ins]) -> List[xl.Ins]:
    changed = list(instructions)
    for index, ins in enumerate(changed):
        if ins.address == 0x40D478:
            changed[index] = ins._replace(args=("00418C99",))
    return changed


MUTATIONS = [
    Mutation("a deleted row (r825)", "after a gap", drop_line("row | r825 |")),
    Mutation("a deleted row (r825), seen by the events it held", "in no row", drop_line("row | r825 |")),
    Mutation("a bogus row (r798 claims zss)", "names zss, which nothing",
             replace_once("| 798 | ALLVDOK |", "| 798 | ALLVDOK zss |")),
    Mutation("a bogus row (r771 claims line 775 for line 772)", "eps1 does not occur in the text of lines",
             replace_once("| 771 772 | xss0 xsr0 eps1", "| 771 775 | xss0 xsr0 eps1")),
    Mutation("a moved address (the home of epsx1)", "which the map does not name",
             replace_once("cell | 0x422230 | epsx1:r4", "cell | 0x422231 | epsx1:r4")),
    Mutation("a moved row boundary (into an instruction)", "is not an instruction boundary",
             replace_once("0x40C44C | 0x40C50B | 810", "0x40C44D | 0x40C50B | 810")),
    Mutation("a moved row boundary (r804 ends early)", "after a gap",
             replace_once("| 0x40C1D6 | 0x40C44C | 802", "| 0x40C1D6 | 0x40C440 | 802")),
    Mutation("a call to a thunk with no stub", "a target with no stub", instructions=retarget_call),
    Mutation("a thunk whose contract is not the stack the reader holds", "the thunk's contract is",
             depth={0x418C16: 2}),
    Mutation("a missing input (zss)", "names no input", drop_line("input | 0x4222BC |")),
    Mutation("an input with the wrong origin (sd4 from the plane)", "no instruction of region plane writes it",
             replace_once("input | 0x422168 | loop |", "input | 0x422168 | plane |")),
    Mutation("an array with the wrong rank (QDOKS)", "the Fortran's ALLOCATE",
             lambda text: "\n".join(line.replace("| QDOKS | i8 | 2", "| QDOKS | i8 | 1") for line in text.split("\n"))),
    Mutation("two arrays with their names swapped", "ALLOCATE sites are not",
             lambda text: text.replace("| GDOK |", "| @@ |").replace("| DDOK |", "| GDOK |").replace("| @@ |", "| DDOK |")),
    Mutation("a tenant with the wrong type (epsx1 as REAL*8)", "epsx1 is r8 in the map",
             replace_once("cell | 0x422230 | epsx1:r4", "cell | 0x422230 | epsx1:r8")),
    Mutation("a register claim at the wrong address", "no `phi` value is made there",
             replace_once("allvdoks:798@0x40C175:phi", "allvdoks:798@0x40C176:phi")),
    Mutation("a line declared eliminated that the row stores (epsmd4)", "declares epsmd4 eliminated and the row stores it",
             replace_once("| eliminated=d43:979", "| eliminated=d43:979,epsmd4:980")),
    Mutation("a line the row does not assign (no claim, no store)", "neither stores nor claims",
             replace_once("| register=allvdoks:798@0x40C175:phi", "")),
]


def selftest_command() -> int:
    excerpt = xl.read_excerpt()
    image = xl.Image()
    text = lm.MAP_PATH.read_text(encoding="utf-8")
    baseline = lc.load(lm.parse_text(text), excerpt, image)
    failures = 0
    if baseline.problems:
        print(f"selftest: the committed map is red before any mutation: {baseline.problems[0]}")
        return 1
    print("selftest: the committed map is green")
    for mutation in MUTATIONS:
        edited = mutation.edit(text) if mutation.edit else text
        instructions = mutation.instructions(excerpt[0]) if mutation.instructions else excerpt[0]
        saved = dict(lc.THUNK_DEPTH)
        lc.THUNK_DEPTH.update(mutation.depth or {})
        try:
            red = lc.load(lm.parse_text(edited), (instructions, excerpt[1]), image)
        finally:
            lc.THUNK_DEPTH.clear()
            lc.THUNK_DEPTH.update(saved)
        shown = next((p for p in red.problems if mutation.expect in p), None)
        if shown is None:
            failures += 1
            print(f"selftest: FAIL, {mutation.what} is not red on `{mutation.expect}` "
                  f"({len(red.problems)} problems: {red.problems[:1]})")
        else:
            print(f"selftest: red on {mutation.what}: {shown[:150]}")
    failures += generator_proofs(excerpt, image)
    return 1 if failures else 0


# ---- the generated table -----------------------------------------------------------

HEADER = [
    "# Generated by classify-cycle-plane-listing.py from the executable's listing (the excerpt",
    "# PropStructV3.cycle-plane.listing.txt), the address map CyclePlaneListing.map.txt and the",
    "# Fortran source (PropStructv3.for.txt) with its declaration block; both files lie outside the",
    "# repository (tools/legacy), and this table names source lines, it quotes none of them.",
    "# Do not edit by hand: `python classify-cycle-plane-listing.py verify` regenerates this file",
    "# and fails on any byte difference.",
    "#",
    "# One line per site, in address order. kind: S REAL*4 store (rounds); Q REAL*8 store (exact);",
    "# K constant store or move; M move of a loaded or merged value; T store to a compiler",
    "# temporary; P sum reloaded and stored every pass; B<n> sum unrolled n-fold; X sum carried in",
    "# the register through its loop; C<n> n stores each reading the one before from the register;",
    "# R register value never stored; E value no line reads, or a line with no code; CMP comparison.",
    "# reads: name:home (loaded from its home), name:input (a home the plane never wrote),",
    "# name:register (the value, before rounding, of an earlier store or register value).",
    "# order: the expression as the executable forms it, operands by name, array offsets dropped.",
    "#",
    "# " + " | ".join(COLUMNS),
]


def render_table(sites: List[ls.Site]) -> str:
    lines = list(HEADER)
    for site in sites:
        cells = (f"{site.first:06X}", site.row, "+".join(str(line) for line in site.lines), site.lhs or site.name, site.type, site.kind,
                 site.rounds, site.home, str(site.count), " ".join(site.reads) or "-", site.schedule, site.order)
        lines.append(" | ".join(cells))
    return "\n".join(lines) + "\n"


def kind_counts(sites: List[ls.Site]) -> str:
    counts: Dict[str, int] = {}
    for site in sites:
        counts[site.kind] = counts.get(site.kind, 0) + 1
    return ", ".join(f"{kind} {count}" for kind, count in sorted(counts.items()))


def generate_text(program: lc.Program) -> Tuple[str, List[ls.Site]]:
    if program.problems:
        raise lm.MapError(f"the map is red ({len(program.problems)} problems, first: {program.problems[0]})")
    sites = ls.derive(program)
    return render_table(sites), sites


def generate_command() -> int:
    try:
        text, sites = generate_text(lc.load())
    except lm.MapError as error:
        print(f"{SCRIPT_NAME}: generate: {error}")
        return 1
    OUTPUT_PATH.write_text(text, encoding="utf-8", newline="\n")
    print(f"{SCRIPT_NAME}: wrote {OUTPUT_PATH.name} ({len(sites)} sites: {kind_counts(sites)})")
    return 0


def first_difference(fresh: str, committed: str) -> str:
    left, right = fresh.split("\n"), committed.split("\n")
    for number, (a, b) in enumerate(zip(left, right), 1):
        if a != b:
            return f"line {number}: generated {a[:160]!r} against committed {b[:160]!r}"
    return f"line counts differ: generated {len(left)}, committed {len(right)}"


def verify_command() -> int:
    if not OUTPUT_PATH.is_file():
        print(f"{SCRIPT_NAME}: verify: {OUTPUT_PATH.name} is not committed")
        return 1
    try:
        fresh, sites = generate_text(lc.load())
    except lm.MapError as error:
        print(f"{SCRIPT_NAME}: verify: {error}")
        return 1
    committed = OUTPUT_PATH.read_bytes().decode("utf-8")
    if fresh != committed:
        with tempfile.NamedTemporaryFile(mode="w", suffix=".txt", prefix="propstruct_cycle_listing_", delete=False,
                                         encoding="utf-8", newline="\n") as handle:
            handle.write(fresh)
        print(f"{SCRIPT_NAME}: verify: {OUTPUT_PATH.name} does not reproduce byte for byte "
              f"({first_difference(fresh, committed)}; regenerated copy {handle.name})")
        return 1
    print(f"{SCRIPT_NAME}: verify: {OUTPUT_PATH.name} reproduces byte for byte ({len(sites)} sites: "
          f"{kind_counts(sites)})")
    return 0


# ---- the proofs of the generator ---------------------------------------------------

ARCHIVE_CONFIRMED = tuple(range(771, 785)) + (795, 796)
ARCHIVE_STORES = tuple(range(771, 785)) + (796,)
ARCHIVE_NOT_STORED = ((795, "epsalldok"),)
# where the archive's outputs show a read taking its value from: 772 reads the rounded home of
# xsr0 (771 rounds), 796 the unrounded register value of 795's quotient (795 never stores it)
ARCHIVE_READS = ((772, "xsr0", "home"), (796, "epsalldok", "register"))
STORE_KINDS = ("S", "T", "R", "Q", "X", "P", "B", "C")


def archive_rules(derived: Dict[Tuple[int, str], str]) -> Dict[Tuple[int, str], str]:
    """(line, target) -> `rounds` or `not rounded` at the lines the archive's outputs confirm
    (771-784 and 795-796, ACCEPTANCE.md, the archive criteria): every store of 771-784 and of 796
    rounds, and 795's quotient, which the archive shows 796 reading unrounded, is never stored.
    The keys are the generator's own (line, target) pairs, the verdicts the archive's."""
    rules = {key: "rounds" for key in derived if key[0] in ARCHIVE_STORES}
    rules.update({key: "not rounded" for key in ARCHIVE_NOT_STORED})
    return rules


def derived_rules(sites: List[ls.Site]) -> Dict[Tuple[int, str], str]:
    rules: Dict[Tuple[int, str], str] = {}
    for site in sites:
        if not site.kind.startswith(STORE_KINDS):
            continue
        for line in site.lines:
            if line in ARCHIVE_CONFIRMED:
                rules[(line, site.name)] = "not rounded" if site.kind == "R" else "rounds"
    return rules


def positive_proof(sites: List[ls.Site]) -> Optional[str]:
    """The generator re-derives the archive-confirmed verdicts: every (line, target) of lines
    771-784 and 795-796 with the verdict the archive's outputs give it."""
    derived = derived_rules(sites)
    wanted = archive_rules(derived)
    if not wanted:
        return "the generator derives no archive-confirmed row to check"
    wrong = [f"{line} {name}: archive {rule}, generator {derived.get((line, name), 'nothing')}"
             for (line, name), rule in sorted(wanted.items()) if derived.get((line, name)) != rule]
    for line, name, source in ARCHIVE_READS:
        read = f"{name}:{source}"
        if not any(line in site.lines and read in site.reads for site in sites):
            wrong.append(f"{line}: the archive reads {name} from the {source}, the generator does not")
    return "; ".join(wrong) if wrong else None


class Edit:
    """The instruction at an address becomes another x87 instruction on the same memory
    operand: its operand text is the excerpt's own and its bytes are the excerpt's with the
    opcode byte and the ModRM register field replaced, all read from the excerpt at run
    time (the reader decodes the bytes and compares them with the mnemonic, so an edit of
    the text alone is refused)."""

    def __init__(self, address: int, old: str, mnemonic: str, opcode: int, field: int):
        self.address = address
        self.old = old
        self.mnemonic = mnemonic
        self.opcode = opcode  # the x87 memory form: D8 /field and D9 /field are the 32-bit ones
        self.field = field


def edited(ins: xl.Ins, edit: Edit) -> xl.Ins:
    """`ins` as the edit has it: the mnemonic, the opcode byte and the ModRM register field
    replaced, the operand text and every other byte as read."""
    data = bytes([edit.opcode, (ins.data[1] & 0xC7) | (edit.field << 3)]) + ins.data[2:]
    return ins._replace(mnemonic=edit.mnemonic, data=data)


def apply_edits(instructions: List[xl.Ins], edits: List[Edit]) -> List[xl.Ins]:
    changed = list(instructions)
    for edit in edits:
        index = next((i for i, ins in enumerate(changed) if ins.address == edit.address), None)
        if index is None or changed[index].mnemonic != edit.old:
            raise lm.MapError(f"selftest: {edit.address:06X} is not a {edit.old}")
        changed[index] = edited(changed[index], edit)
    return changed


class GeneratorBreak:
    """One break of the listing and the table line that must move."""

    def __init__(self, what: str, edits: List[Edit], line: str):
        self.what = what
        self.edits = edits
        self.line = line                      # a substring of the table line that must differ


GENERATOR_BREAKS = [
    GeneratorBreak("a swapped operand (the fsub of line 772 reversed: 0.5 - xsr0)",
                   [Edit(0x40BDC7, "fsub", "fsubr", 0xD8, 5)], "| EPS1 |"),
    GeneratorBreak("a register read claimed as a home read (line 772 reads xsr0 from st(0), not from its home)",
                   [Edit(0x40BDBB, "fstp", "fst", 0xD9, 2),
                    Edit(0x40BDC1, "fld", "fcom", 0xD8, 2)], "| EPS1 |"),
]


def generator_proofs(excerpt: Tuple[List[xl.Ins], List[Tuple[int, int, str]]], image: xl.Image) -> int:
    failures = 0
    program = lc.load(None, excerpt, image)
    committed, sites = generate_text(program)
    why = positive_proof(sites)
    if why is not None:
        failures += 1
        print(f"selftest: FAIL, the generator does not re-derive the archive-confirmed rules: {why}")
    else:
        print(f"selftest: right on the archive-confirmed rules: {len(archive_rules(derived_rules(sites)))} (line, target) "
              f"verdicts of lines 771-784 and 795-796 re-derived as the archive's outputs state them")
    for broken in GENERATOR_BREAKS:
        instructions = apply_edits(excerpt[0], broken.edits)
        red = lc.load(None, (instructions, excerpt[1]), image)
        if red.problems:
            print(f"selftest: {broken.what}: the map's checks already say `{red.problems[0][:120]}`")
        try:
            mutated, _ = generate_text(red)
        except lm.MapError as error:
            print(f"selftest: red on {broken.what}: the generator stops: {str(error)[:140]}")
            continue
        before = [line for line in committed.split("\n") if broken.line in line]
        after = [line for line in mutated.split("\n") if broken.line in line and line not in before]
        if not after:
            failures += 1
            print(f"selftest: FAIL, {broken.what} does not move the line `{broken.line}` of the table")
        else:
            print(f"selftest: red on {broken.what}: {before[0][40:140]!r} becomes {after[0][40:140]!r}")
    return failures


COMMANDS = {"check": check_command, "generate": generate_command, "verify": verify_command,
            "selftest": selftest_command}


def main(argv: List[str]) -> int:
    if len(argv) != 2 or argv[1] not in COMMANDS:
        print(__doc__)
        return 2
    return COMMANDS[argv[1]]()


if __name__ == "__main__":
    sys.exit(main(sys.argv))
