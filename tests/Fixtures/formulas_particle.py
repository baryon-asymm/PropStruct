"""Transcribes three Fortran formulas of `PropStructv3.for.txt` into Python `double`
and writes constructed cases with their independently computed expected values as
JSON, for `tests/Particle.Tests` to check the port against (this node's `API.md`,
"## Particle formula cases", has the schema of each file).

This script simulates no particle: it evaluates each named subroutine's formula, in
the Fortran order, for inputs built by hand to reach every branch (`## Cases`, below).
Expected values are never typed: `generate()` calls the same functions the constructed
inputs are fed to, and `verify()` proves a rerun reproduces the committed files byte
for byte.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import json
import math
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, Optional, Sequence

NODE_ROOT = Path(__file__).resolve().parent
CASES_DIR = NODE_ROOT / "cases" / "particle"
SCRIPT_NAME = "formulas_particle.py"

# ---------------------------------------------------------------------------
# SIZE (PropStructv3.for.txt, lines 1761-1776): the base/neighbour size law.
#
# Fortran (1-indexed): DOK(2*NM) fraction bounds, Z(NM+1) cumulative boundaries. The
# scanning loop keeps no `break`, so of every I with Z(I) < X <= Z(I+1) the *last* one
# wins; a well-formed monotonic Z has at most one match. JZ=2 is uniform in D; any
# other value is uniform in 1/D^2 (src/Particle/BOOT.md, ## Constraints).
#
# This module's arrays and indices are the Fortran (1-based) ones throughout: index 1
# is `dok[0]`/`z[0]` in the Python lists below, and the JSON case files name the
# result fraction with the same 1-based convention (this node's API.md says so
# explicitly, so a consumer converting to a 0-based index knows which direction to
# shift).
# ---------------------------------------------------------------------------


def size_law_sample(size_law: int, x: float, x1: float, bounds: Sequence[float], cumulative: Sequence[float]) -> Dict[str, object]:
    fraction_count = len(cumulative) - 1
    minv: Optional[int] = None
    for i in range(1, fraction_count + 1):
        if x > cumulative[i - 1] and x <= cumulative[i]:
            minv = i  # no break: the last match wins, as line 1766-1767 does
    if minv is None:
        raise ValueError(f"x={x!r} matches no fraction of cumulative={cumulative!r}")

    d1 = bounds[2 * minv - 2]  # DOK(2*MINV-1)
    d2 = bounds[2 * minv - 1]  # DOK(2*MINV)
    if size_law == 2:
        d = x1 * (d2 - d1) + d1
    else:
        d = 1.0 / math.sqrt(1.0 / d1 ** 2 - x1 * (1.0 / d1 ** 2 - 1.0 / d2 ** 2))
    return {"fraction": minv, "diameter": d}


# ---------------------------------------------------------------------------
# VM (PropStructv3.for.txt, lines 1588-1629): the bridge volume between two base
# particles' spherical caps. `error` (line 1611) is never read anywhere and is not
# transcribed (src/Particle/BOOT.md, ## Line map). The swap of lines 1591-1596 is
# applied to *local* copies here, never to the caller's r1/r2 (the port's own
# departure from the Fortran call-by-reference original, same BOOT.md entry).
# ---------------------------------------------------------------------------


def bridge_geometry_volume(r1: float, r2: float, rk: float, a: float) -> Dict[str, object]:
    swapped = r2 > r1
    if swapped:
        r1, r2 = r2, r1

    if a >= 2 * rk:
        return {"jj": 0, "swapped": swapped, "bb": None, "vmkm": None}

    ab = r1 + rk
    bc = r2 + rk
    ac = r1 + r2 + a
    cos_a = (ab ** 2 + ac ** 2 - bc ** 2) / (2 * ab * ac)
    cos_d = (ac ** 2 + bc ** 2 - ab ** 2) / (2 * ac * bc)
    q1 = r1 * cos_a
    q2 = r2 * cos_d
    al = math.acos(cos_a)
    de = math.acos(cos_d)
    bb = 2 * (ab * math.sin(al) - rk)
    if bb < 0:
        return {"jj": 0, "swapped": swapped, "bb": bb, "vmkm": None}

    be = 3.14 - al - de
    ga = al + be / 2.0
    r1a = r1 * math.sin(al)
    r2a = r2 * math.sin(de)
    q1m = r1a * math.tan(ga)
    q2m = r2a * math.tan(ga)
    h1 = r1 - q1
    h2 = r2 - q2
    v1 = 3.14 * h1 ** 2 * (r1 - h1 / 3.0)
    v2 = 3.14 * h2 ** 2 * (r2 - h2 / 3.0)
    vmkm = 3.14 * r1a ** 2 * q1m / 3 - 3.14 * r2a ** 2 * q2m / 3
    vmkm = vmkm - v1 - v2
    return {"jj": 1, "swapped": swapped, "bb": bb, "vmkm": vmkm}


# ---------------------------------------------------------------------------
# The bridge window (PropStructv3.for.txt, lines 582-611, `BridgeWindow.Build`) and
# DM (lines 1573-1586, `BridgeWindow.SamplePocket`). QKS1 and the outputs are 1-based
# Fortran arrays, same convention as SIZE above. `mindk` never changes; `maxdk` can
# shrink in the 1e-5 truncation loop (600-606) before the shift to index 1 (607-610).
# ---------------------------------------------------------------------------


def bridge_window_build(cell_size: float, ak3: float, ak4: float, dr: float, qks1: Sequence[float]) -> Dict[str, object]:
    nkarm = len(qks1)
    rr1 = ak3 * dr
    rr2 = ak4 * dr
    mindk = int(rr1 / cell_size) + 1
    maxdk = int(rr2 / cell_size) + 1
    if maxdk + 1 > nkarm + 1:
        raise ValueError(f"maxdk+1={maxdk + 1} exceeds the qks1 layout (nkarm={nkarm}); construct a larger qks1")

    aus = 0.0
    for iks in range(mindk, maxdk + 1):
        aus += qks1[iks - 1]
    if aus == 0.0:
        return {"emptyWindow": True}
    aus = 1.0 / aus

    dpoc = [0.0] * (nkarm + 2)  # index 0 unused; 1..nkarm+1 as in the Fortran DIMENSION
    fqks = [0.0] * (nkarm + 2)
    dpoc[mindk] = cell_size * mindk  # literal as written (BOOT.md, "Bridge window once per attempt")
    for iks in range(mindk, maxdk + 1):
        fqks[iks + 1] = aus * qks1[iks - 1] + fqks[iks]
        dpoc[iks + 1] = cell_size + dpoc[iks]

    for iks in range(mindk, maxdk + 2):
        if (1.0 - fqks[iks]) < 1e-5:
            maxdk = iks - 1
            fqks[iks] = 1.0
            break

    cell_count = maxdk - mindk + 2
    boundaries = [dpoc[mindk - 1 + i] for i in range(1, cell_count + 1)]
    cumulative = [fqks[mindk - 1 + i] for i in range(1, cell_count + 1)]
    return {
        "emptyWindow": False,
        "minCell": mindk,
        "maxCell": maxdk,
        "cellCount": cell_count,
        "boundaries": boundaries,
        "cumulative": cumulative,
    }


def sample_pocket(x3: float, boundaries: Sequence[float], cumulative: Sequence[float]) -> float:
    n = len(cumulative)
    for i in range(1, n):  # 1-based I=1..N-1: Z(I) <= X < Z(I+1), lines 1578-1585
        lo, hi = cumulative[i - 1], cumulative[i]
        if lo <= x3 < hi:
            return boundaries[i - 1] + (x3 - lo) * (boundaries[i] - boundaries[i - 1]) / (hi - lo)
    raise IndexError(f"x3={x3!r} not covered by cumulative={cumulative!r} (DM would run off the array)")


# ---------------------------------------------------------------------------
# Cases. Every input here is constructed by hand; every expected value comes from
# calling the functions above, never typed (root BOOT.md Taboos).
# ---------------------------------------------------------------------------


def size_law_cases() -> List[Dict[str, object]]:
    bounds = [10.0, 50.0, 160.0, 315.0]         # DOK: fraction 1 = [10,50], fraction 2 = [160,315]
    cumulative = [0.0, 0.6, 1.0]                # Z: Z(1)=0, Z(2)=0.6, Z(3)=1.0

    inputs = [
        ("uniform_in_d_interior", 2, 0.3, 0.25),
        ("uniform_in_reciprocal_square_interior", 1, 0.8, 0.5),
        ("fraction_boundary_at_cumulative_picks_lower", 2, 0.6, 0.9),
        ("fraction_boundary_just_above_cumulative_picks_upper", 2, 0.6000000001, 0.1),
        ("fraction_boundary_at_upper_end_of_last_interval", 1, 1.0, 0.0),
        ("x1_at_upper_edge_reproduces_upper_bound", 2, 0.3, 1.0),
    ]
    cases = []
    for name, size_law, x, x1 in inputs:
        expected = size_law_sample(size_law, x, x1, bounds, cumulative)
        cases.append({
            "name": name,
            "sizeLaw": size_law,
            "bounds": bounds,
            "cumulative": cumulative,
            "x": x,
            "x1": x1,
            "expected": expected,
        })
    return cases


def bridge_geometry_volume_cases() -> List[Dict[str, object]]:
    inputs = [
        ("normal_no_swap_needed", 5.0, 3.0, 2.0, 1.0),
        ("normal_swap_needed_same_geometry_as_no_swap_case", 3.0, 5.0, 2.0, 1.0),
        ("jj_zero_a_equals_two_rk_boundary", 5.0, 3.0, 2.0, 4.0),
        ("jj_zero_a_greater_than_two_rk", 5.0, 3.0, 2.0, 10.0),
        ("jj_zero_from_negative_bb", 5.0, 0.1, 3.0, 0.5),
    ]
    cases = []
    for name, r1, r2, rk, a in inputs:
        expected = bridge_geometry_volume(r1, r2, rk, a)
        cases.append({
            "name": name,
            "r1": r1,
            "r2": r2,
            "rk": rk,
            "a": a,
            "expected": expected,
        })
    return cases


def bridge_window_cases() -> List[Dict[str, object]]:
    cases = []

    # W1: ordinary window, truncation fires exactly at the natural end (no cell
    # dropped: every cell of [mindk, maxdk] is nonzero, so cumulative reaches 1 only
    # once the whole window is consumed) and the shift to index 1 is non-trivial
    # (mindk = 2, so boundaries/cumulative are moved, not left in place).
    cell_size, ak3, ak4, dr = 1.0, 1.0, 3.0, 1.0
    qks1 = [1.0, 2.0, 3.0, 4.0, 5.0]
    build = bridge_window_build(cell_size, ak3, ak4, dr, qks1)
    samples = [{"x3": x3, "expected": {"pocketSize": sample_pocket(x3, build["boundaries"], build["cumulative"])}}
               for x3 in (0.0, 0.25, 0.9999999)]
    cases.append({
        "name": "ordinary_window_truncation_at_natural_end",
        "cellSize": cell_size, "ak3": ak3, "ak4": ak4, "dr": dr, "qks1": qks1,
        "expected": build, "samples": samples,
    })

    # W2: a trailing run of zero cells inside [mindk, maxdk] makes the cumulative
    # reach 1 - epsilon well before the nominal maxdk: the 1e-5 truncation fires
    # early and shrinks maxdk from 5 to 2.
    cell_size, ak3, ak4, dr = 1.0, 1.0, 4.0, 1.0
    qks1 = [1.0, 2.0, 0.0, 0.0, 0.0]
    build = bridge_window_build(cell_size, ak3, ak4, dr, qks1)
    samples = [{"x3": x3, "expected": {"pocketSize": sample_pocket(x3, build["boundaries"], build["cumulative"])}}
               for x3 in (0.0, 0.5, 0.9999999)]
    cases.append({
        "name": "early_truncation_from_trailing_zero_cells",
        "cellSize": cell_size, "ak3": ak3, "ak4": ak4, "dr": dr, "qks1": qks1,
        "expected": build, "samples": samples,
    })

    # W3: every cell of [mindk, maxdk] is zero: AUS = 0, the empty-window restart
    # (line 590, GO TO 11).
    cell_size, ak3, ak4, dr = 1.0, 1.0, 2.0, 1.0
    qks1 = [0.0, 0.0, 0.0, 0.0, 0.0]
    build = bridge_window_build(cell_size, ak3, ak4, dr, qks1)
    cases.append({
        "name": "empty_window_all_cells_zero",
        "cellSize": cell_size, "ak3": ak3, "ak4": ak4, "dr": dr, "qks1": qks1,
        "expected": build, "samples": [],
    })

    # W4: mindk = 1, the edge of "DPOC(mindk) = Di*mindk" and of the shift to index 1
    # (mindk - 1 = 0, so the shift is the identity here, unlike W1/W2 above).
    cell_size, ak3, ak4, dr = 5.0, 0.1, 1.0, 5.0
    qks1 = [3.0, 7.0, 0.0, 0.0, 0.0]
    build = bridge_window_build(cell_size, ak3, ak4, dr, qks1)
    samples = [{"x3": x3, "expected": {"pocketSize": sample_pocket(x3, build["boundaries"], build["cumulative"])}}
               for x3 in (0.0, 0.3, 0.9999999)]
    cases.append({
        "name": "mindk_at_one_identity_shift",
        "cellSize": cell_size, "ak3": ak3, "ak4": ak4, "dr": dr, "qks1": qks1,
        "expected": build, "samples": samples,
    })

    return cases


FILES = {
    "size_law.json": {
        "fortranLines": "1761-1776",
        "subroutine": "SIZE",
        "cases_fn": size_law_cases,
    },
    "bridge_geometry_volume.json": {
        "fortranLines": "1588-1629",
        "subroutine": "VM",
        "cases_fn": bridge_geometry_volume_cases,
    },
    "bridge_window.json": {
        "fortranLines": "582-625,1573-1586",
        "subroutine": "the bridge window (build, lines 582-611) and DM (lines 1573-1586, SamplePocket)",
        "cases_fn": bridge_window_cases,
    },
}


def _document(spec: dict) -> dict:
    return {
        "script": SCRIPT_NAME,
        "fortranLines": spec["fortranLines"],
        "subroutine": spec["subroutine"],
        "convention": "arrays and fraction/cell indices are the Fortran 1-based convention of the "
                       "transcribed subroutine, not the port's own 0-based one (this node's API.md, "
                       "'## Particle formula cases').",
        "cases": spec["cases_fn"](),
    }


def _write(path: Path, document: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def cmd_generate() -> None:
    for filename, spec in FILES.items():
        _write(CASES_DIR / filename, _document(spec))
        print(f"wrote {CASES_DIR / filename}")


def cmd_verify() -> None:
    problems: List[str] = []
    with tempfile.TemporaryDirectory(prefix="propstruct_formulas_particle_") as tmp:
        tmp_dir = Path(tmp)
        for filename, spec in FILES.items():
            fresh = json.dumps(_document(spec), indent=2, ensure_ascii=False) + "\n"
            committed_path = CASES_DIR / filename
            if not committed_path.is_file():
                problems.append(f"{filename}: not committed under {CASES_DIR}")
                continue
            committed = committed_path.read_text(encoding="utf-8")
            if fresh != committed:
                (tmp_dir / filename).write_text(fresh, encoding="utf-8")
                problems.append(f"{filename}: regeneration differs from the committed file (see {tmp_dir / filename})")
    if problems:
        for p in problems:
            print(f"MISMATCH {p}", file=sys.stderr)
        raise SystemExit(f"verify: {len(problems)} case file(s) not byte-equal on regeneration")
    print(f"verify: all {len(FILES)} case files reproduce byte for byte ({', '.join(FILES)})")


def main(argv: List[str]) -> int:
    if len(argv) != 1 or argv[0] not in ("generate", "verify"):
        print(f"usage: {SCRIPT_NAME} generate|verify", file=sys.stderr)
        return 2
    if argv[0] == "generate":
        cmd_generate()
    else:
        cmd_verify()
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
