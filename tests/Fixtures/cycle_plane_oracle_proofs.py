"""The listing oracle's right-proofs (x87 A, W2b): the oracle's answers held against archived prints.

    python cycle_plane_oracle_proofs.py k1   # C1: the printed da_coef of every archived output
    python cycle_plane_oracle_proofs.py k2   # control (iv): rps01 and rps02 with their pocket-forming flags
    python cycle_plane_oracle_proofs.py k4   # controls (i) and (iii): the oracle's rows 795-796 and 810-816 as the rule

Each case is the constructed cycle of `tests/Statistics.Tests/ConstructedCycle.cs` run through the
executable's own bytes: the formulation and the menu defaults, every total valid, the one reachable
FMDOK cell empty. `da_coef` is what the plane stores at the home the oracle names `mp`; the archive
prints it with a resolution, and the proof is that the oracle's value prints the same token.

This file may name cells of the map: it is a reader of the archive, not the oracle (the guard of
`cycle_plane_oracle.py` covers the machine and the oracle only).

Python 3.8+, standard library only.
"""

from __future__ import annotations

import math
import re
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Dict, List, Optional, Sequence, Tuple

import cycle_plane_oracle as co

FIXTURES = Path(__file__).resolve().parent
OUTPUTS = sorted((FIXTURES / "Legacy" / "outputs").glob("*.m.txt")) + sorted(
    (FIXTURES / "references").glob("*/results.m.txt"))
EMPTY_CELL = "[0.0] + [1.0] * (ndok - 1)"
PROOF_STEP_BUDGET = 400_000_000  # HMX's warm-up cycle alone takes 44 million instructions
POSITIVE = "[uniform(0.1, 0.5) for v in ALLVDOK]"  # the arrays the empty cell would zero stay positive
CONSTRUCTED_TOTALS = {"ALLVDOK": EMPTY_CELL, "VSMKM": POSITIVE, "SVD": POSITIVE, "Vdok_total": POSITIVE,
                      "Vmkm_total": POSITIVE}


def tokens_of(path: Path) -> List[float]:
    found = []
    for line in path.read_text(encoding="latin-1").splitlines():
        for token in line.replace("\t", " ").split():
            try:
                found.append(float(token))
            except ValueError:
                pass
    return found


def shipped(name: str) -> Optional[Path]:
    for candidate in co.FORMULATIONS.iterdir():
        if candidate.name.lower() == name.lower():
            return candidate
    return None


def flags_of(dat: Path) -> Optional[List[int]]:
    """Menu [12]'s flags as the .dat carries them after its bounds, when it carries them."""
    values = tokens_of(dat)
    count = int(values[8])
    rest = values[10:]
    tail = rest[4 + 3 * count:4 + 3 * count + count]
    return [int(v) for v in tail] if len(tail) == count else None


def printed_da_coef(path: Path) -> Optional[Tuple[str, str, bool]]:
    text = path.read_text(encoding="latin-1")
    named = re.search(r"Input filename:\s*(\S+)", text)
    da = re.search(r"da_coef\s*=\s*([-0-9.Ee+]+)", text)
    if not named or not da or shipped(named.group(1)) is None:
        return None
    return named.group(1), da.group(1), bool(re.search(r"sfr\s*=", text))


def token_of(value: float, printed: str) -> str:
    digits = len(printed.split(".")[1]) if "." in printed else 0
    return f"{value:.{digits}f}"


def case_of(dat: Path, flags: Optional[List[int]], eta: float, seed: int) -> Dict[str, object]:
    return {"id": dat.stem, "purpose": "proof", "formulation": dat.name, "menu": {"eta": eta} if eta else {},
            "flags": flags, "setup": {}, "seed": seed, "max_cycles": 2,
            "totals": dict(CONSTRUCTED_TOTALS)}


def oracle_da_coef(case: Dict[str, object], oracle_map: co.OracleMap, sites: Sequence[co.Site]) -> Tuple[float, str]:
    """The `mp` of cycle 1, or NaN and the stop's text: a worker never raises, a pool would hang."""
    run = co.run_case(case, oracle_map, sites, step_budget=PROOF_STEP_BUDGET)
    if run.stop is not None or len(run.cycles) < 2:
        return math.nan, f"stopped: {run.stop}"
    data = run.cycles[1].outputs["mp"].data
    return struct.unpack("<f", data)[0], run.cycles[1].exit


def candidates() -> List[Tuple[str, Path, str, Optional[List[int]]]]:
    groups: Dict[str, List[Tuple[str, Path, str, bool]]] = defaultdict(list)
    for path in OUTPUTS:
        found = printed_da_coef(path)
        if found is None:
            continue
        dat = shipped(found[0])
        label = path.name if path.name != "results.m.txt" else path.parent.name + "/results"
        groups[dat.stem.upper()].append((label, dat, found[1], found[2]))
    chosen = []
    for formulation, items in sorted(groups.items()):
        counts = Counter(printed for _, _, printed, _ in items)
        printed, _ = counts.most_common(1)[0]
        label, dat, _, with_flags = next(item for item in items if item[2] == printed)
        chosen.append((formulation, dat, printed, flags_of(dat) if with_flags else None))
    return chosen


def k1() -> int:
    oracle_map = co.load_map()
    sites = co.load_sites()
    chosen = candidates()
    tasks = [case_of(dat, flags, 0.0, 7) for _, dat, _, flags in chosen]
    results = co.run_pool(proof_task, tasks, 8)
    matches = 0
    for (formulation, dat, printed, flags), (value, exit_name) in zip(chosen, results):
        token = token_of(value, printed)
        matches += token == printed
        print(f"{formulation:9s} printed {printed} oracle {token} {'ok' if token == printed else 'MISS'} "
              f"flags={'y' if flags else 'n'} {value!r}")
    print(f"k1: {matches} of {len(chosen)} archived formulations print what the oracle computes")
    return 0 if matches == len(chosen) else 1


def proof_task(case: Dict[str, object]) -> Tuple[float, str]:
    oracle_map, sites = co._worker_state()
    return oracle_da_coef(case, oracle_map, sites)


def k2() -> int:
    ok = 0
    for name in ("rps01", "rps02"):
        path = FIXTURES / "Legacy" / "outputs" / f"{name}.m.txt"
        found = printed_da_coef(path)
        assert found is not None
        dat = shipped(found[0])
        value, _ = proof_task(case_of(dat, flags_of(dat), 0.0, 7))
        without, _ = proof_task(case_of(dat, None, 0.0, 7))
        token, other = token_of(value, found[1]), token_of(without, found[1])
        print(f"{name}: printed {found[1]} oracle {token} {'ok' if token == found[1] else 'MISS'}; "
              f"flags unread {other} {'moves' if other != found[1] else 'DOES NOT MOVE'}")
        ok += token == found[1] and other != found[1]
    print(f"k2: {ok} of 2")
    return 0 if ok == 2 else 1


REFERENCE_FORMULATIONS = ("HPEPA3", "inpt", "P33", "PSAN02n", "HMX")
EXTRA_OUTPUTS = {"HPEPA3": FIXTURES / "cases" / "output" / "nonzero-tail-probability" / "results.m.txt",
                 "inpt": FIXTURES / "cases" / "output" / "nondefault-pocket-coefficient" / "results.m.txt"}
FIRST_EPS_ROW = 0x40BEEF   # the quotient of line 789 is the first instruction that reads a live-in; flags and sums are set by hand
EPS_ROWS_END = 0x40C10F
ROW_810, ROW_816_END = 0x40C44C, 0x40C50B
EPS_ULP_WINDOW = 40
INERT_QUOTIENTS = ("0x422168", "0x422170", "ebp-0x88", "ebp-0x90")


def archived_outputs(formulation: str, with_cases: bool) -> List[Path]:
    paths = [FIXTURES / "references" / formulation / "results.m.txt"]
    for kind in ("replicas-lagged", "replicas-independent", "replicas-gsv3"):
        directory = FIXTURES / kind / formulation
        if directory.is_dir():
            paths.extend(sorted(directory.glob("*.m.txt")))
    if with_cases and formulation in EXTRA_OUTPUTS:
        paths.append(EXTRA_OUTPUTS[formulation])
    return paths


def scalar_token(text: str, name: str) -> str:
    return re.search(name + r"\s*=\s*([-0-9.Ee+]+)", text).group(1)


def array_tokens(text: str, name: str) -> List[str]:
    return re.search(name + r"\s*=\s*\[([^\]]*)\]", text).group(1).split()


def significant(value: float, digits: int, shift: int):
    """The mantissa and exponent a value prints with: `digits` significant digits, the Fortran's `E` form
    (`shift` 1 for E9.3's 0.xxx, 0 for 1.xxxxxxx)."""
    if value == 0.0:
        return (0, 0)
    value = abs(value)
    exponent = math.floor(math.log10(value)) + shift
    for _ in range(8):
        mantissa = math.floor(value / 10.0 ** (exponent - (digits - shift)) + 0.5)
        if mantissa >= 10 ** digits:
            exponent += 1
        elif mantissa < 10 ** (digits - 1):
            exponent -= 1
        else:
            return (mantissa, exponent)
    raise SystemExit(f"{value!r} does not print at {digits} digits")


def print_resolution(token: str) -> float:
    """One unit of the last digit of an `E9.3` token (`0.806E-06` is 1e-9)."""
    return 1e-3 * 10.0 ** int(token.upper().split("E")[1])


class EpsWorld:
    """A formulation's world after its init block, the rows of lines 795-796 and 810-816 run over it as often
    as a control asks, each time over the injected inputs alone."""

    def __init__(self, formulation: str, oracle_map: co.OracleMap, sites: Sequence[co.Site]):
        dat = shipped(formulation + ".dat")
        case = {"id": formulation, "formulation": dat.name, "menu": {}, "flags": None, "setup": {}}
        self.setup = co.case_setup(case)
        self.world = co.World(self.setup, oracle_map, sites)
        self.world.run_init()
        self.zx: List[float] = [float(v) for v in self.setup["zx"]]
        executed = co.read_preloop(self.world)["dokm"]["executed"]
        self.dokm = struct.unpack("<f", bytes.fromhex(executed)[::-1])[0]

    def eps_cells(self, counts: Sequence[int], total: int) -> List[float]:
        """The epsalldok of the executable's rows 795-796 for these per-fraction counts and this total."""
        w = self.world
        extent = w.blocks["ALLDOK"].dims[0]
        w.inject_array("ALLDOK", [total] + [0] * (extent - 1))
        w.inject_array("ALLDOK_fract", list(counts))
        for slot in INERT_QUOTIENTS:  # lines 789-790's quotients, which the rows below never read
            w.inject_scalar(co.parse_location(slot), "r8", 1.0)
        w.undefine_registers_and_fpu()
        for register, value in ((0, 0), (1, 0), (2, 1), (6, extent), (7, 0)):  # the sum's registers, set by code outside the slice
            w.machine.set_reg(register, value)
        w.machine.flags["zf"] = 1
        w.machine.run(FIRST_EPS_ROW, [EPS_ROWS_END])
        data = w.read_array("epsalldok")
        return [struct.unpack_from("<f", data, 4 * k)[0] for k in range(len(counts))]

    def eps_of_alldok43(self, candidate: float) -> float:
        """The epsx3 of the executable's rows 810-816 when ALLDOK43 comes out as `candidate`."""
        w = self.world
        for slot, value in (("ebp-0x228", 1.0), ("ebp-0x238", 1.0), ("ebp-0x230", candidate), ("ebp-0x240", candidate)):
            w.inject_scalar(co.parse_location(slot), "r8", value)
        w.inject_scalar(co.parse_location("ebp-0x440"), "r4", 0.0)
        w.undefine_registers_and_fpu()
        for _ in range(2):  # the registers the previous row leaves: a dead one and line 806's running sum, which no cell compared here reads
            w.machine.fpu_push(0.0)
        w.machine.run(ROW_810, [ROW_816_END])
        return struct.unpack("<f", w.machine.read_bytes(0x422228, 4))[0]


def eps_consistent(world: EpsWorld, tokens: List[str], total: int) -> bool:
    """Some nonnegative integer counts summing to `total` print every archived token (the C# control's own
    search; only the cell function is the executable's)."""
    zx, count = world.zx, len(tokens)
    printed = [float(token) for token in tokens]
    candidates: List[List[int]] = []
    for fraction in range(count):
        found = set()
        window = int(zx[fraction] * total * print_resolution(tokens[fraction])) + 4
        for sign in (1.0, -1.0):
            centre = zx[fraction] * (1.0 + sign * printed[fraction]) * total
            found.update(range(max(0, int(centre) - window), int(centre) + window + 1))
        candidates.append(sorted(found))
    runs = max(len(c) for c in candidates)
    admitted: List[List[int]] = [[] for _ in range(count)]
    for position in range(runs):
        counts = [c[min(position, len(c) - 1)] for c in candidates]
        for fraction, value in enumerate(world.eps_cells(counts, total)):
            if position < len(candidates[fraction]) and \
                    significant(value, 3, 1) == significant(printed[fraction], 3, 1):
                admitted[fraction].append(counts[fraction])
    if count == 1:
        return total in admitted[0]
    if any(not a for a in admitted):
        return False
    order = sorted(range(count), key=lambda i: len(admitted[i]))
    target, rest = set(admitted[order[-1]]), order[:-1]

    def recurse(index: int, partial: int) -> bool:
        if index == len(rest):
            return total - partial in target
        return any(recurse(index + 1, partial + n) for n in admitted[rest[index]])
    return recurse(0, 0)


def step_binary32(value: float, steps: int) -> float:
    bits = struct.unpack("<i", struct.pack("<f", value))[0] + steps
    return struct.unpack("<f", struct.pack("<i", bits))[0]


def on_grid(world: EpsWorld, printed: float) -> bool:
    for sign in (1.0, -1.0):
        centre = struct.unpack("<f", struct.pack("<f", world.dokm * (1.0 + sign * printed)))[0]
        for step in range(-EPS_ULP_WINDOW, EPS_ULP_WINDOW + 1):
            if significant(world.eps_of_alldok43(step_binary32(centre, step)), 8, 0) == significant(printed, 8, 0):
                return True
    return False


def k4_task(formulation: str) -> Tuple[str, int, int, int, int]:
    """A stop of the machine is a failure counted, never an exception: a pool would hang on it."""
    try:
        return k4_counts(formulation)
    except co.xm.Stop as stop:
        print(f"{formulation}: the oracle stopped: {stop}")
        return formulation, 0, 1, 0, 1


def k4_counts(formulation: str) -> Tuple[str, int, int, int, int]:
    oracle_map, sites = co._worker_state()
    world = EpsWorld(formulation, oracle_map, sites)
    first = [0, 0]
    second = [0, 0]
    for path in archived_outputs(formulation, True):
        text = path.read_text(encoding="latin-1")
        total = int(float(scalar_token(text, "NFX"))) + int(float(scalar_token(text, "NFY")))
        first[1] += 1
        first[0] += eps_consistent(world, array_tokens(text, "epsdokfr"), total)
    for path in archived_outputs(formulation, False):
        text = path.read_text(encoding="latin-1")
        second[1] += 1
        second[0] += on_grid(world, float(scalar_token(text, "epsalldok")))
    return formulation, first[0], first[1], second[0], second[1]


def k4_red_task(formulation: str) -> Tuple[str, int, int, int]:
    """Control (i) on a rule the executable does not follow: ZX held in binary32 (the injected array rounded)
    instead of the REAL*8 the executable divides by; control (iii) with DOKM a tenth of a per cent off. Both
    counts must fall."""
    oracle_map, sites = co._worker_state()
    world = EpsWorld(formulation, oracle_map, sites)
    world.world.inject_array("ZX", [struct.unpack("<f", struct.pack("<f", z))[0] for z in world.zx])
    paths = archived_outputs(formulation, True)
    consistent = 0
    for path in paths:
        text = path.read_text(encoding="latin-1")
        total = int(float(scalar_token(text, "NFX"))) + int(float(scalar_token(text, "NFY")))
        consistent += eps_consistent(world, array_tokens(text, "epsdokfr"), total)
    world.dokm = struct.unpack("<f", struct.pack("<f", world.dokm * 1.001))[0]
    on_the_grid = sum(on_grid(world, float(scalar_token(path.read_text(encoding="latin-1"), "epsalldok")))
                      for path in archived_outputs(formulation, False))
    return formulation, consistent, len(paths), on_the_grid


def k4() -> int:
    for formulation, consistent, count, on_the_grid in co.run_pool(k4_red_task, ["HPEPA3", "HMX", "P33"], 3):
        print(f"red: {formulation} with ZX rounded to binary32: epsdokfr {consistent}/{count}; "
              f"with DOKM 0.1 % off: epsalldok {on_the_grid}/{len(archived_outputs(formulation, False))}")
    results = co.run_pool(k4_task, list(REFERENCE_FORMULATIONS), 5)
    totals = [0, 0, 0, 0]
    for formulation, ok1, n1, ok3, n3 in results:
        print(f"{formulation:8s} epsdokfr {ok1}/{n1}  epsalldok {ok3}/{n3}")
        for index, value in enumerate((ok1, n1, ok3, n3)):
            totals[index] += value
    print(f"k4: epsdokfr {totals[0]} of {totals[1]}, epsalldok {totals[2]} of {totals[3]} reproduced by the oracle's rows")
    return 0 if totals[0] == totals[1] == 295 and totals[2] == totals[3] == 293 else 1


if __name__ == "__main__":
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    raise SystemExit({"k1": k1, "k2": k2, "k4": k4}.get(command, lambda: 2)())
