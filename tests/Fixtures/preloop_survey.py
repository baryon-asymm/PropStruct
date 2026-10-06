"""The pre-loop survey: what the executable's own pre-loop leaves for every shipped formulation
(`src/Statistics/ACCEPTANCE.md`, A13; `tests/Fixtures/API.md`, "## Listing oracle").

The listing oracle (`cycle_plane_oracle.py`) builds the state the executable finds after its
allocations, runs the executable's init block and its pre-loop through `x87_machine.py`, and reads
back what the pre-loop computed (line 378's lambda, PARAM's ZSS, ZX and Z11, the DOKM and DOKSD sums
of 379-406, GGG). The fixture of the oracle holds that for the cases it draws; this script runs it
for *every* `.dat` the repository ships, the menu at its defaults, and writes the executed bits
beside the setup model's own value of the same key. `ListingOracleTests` and `PreloopSurveyTests`
hold `Setup.Prepare` under `Original` to those bits; the one file where it parts from them is an
approved ratchet, not a skip.

    python preloop_survey.py generate   # runs the pre-loop of every shipped formulation, writes the file
    python preloop_survey.py verify     # regenerates in memory, fails on any byte difference

It also runs the self-test's drawn JZZ = 2 loops, which the shipped files cover in part only.

Nothing here is typed: the formulations are the directory's, the keys and the cells they are read from
are the map's `expect` rows, every value is the machine's.

Python 3.8+, standard library only.
"""
from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path
from typing import Dict, List

import cycle_plane_oracle as co

HERE = Path(__file__).resolve().parent
SURVEY_PATH = HERE / "cases" / "statistics" / "preloop_survey.json"
SURVEY_FORMAT = 1
SURVEY_FORTRAN_LINES = "376-406"
SURVEY_SCRIPT = "preloop_survey.py"

#: The files the survey's answers depend on: the three scripts that compute and read them, the
#: executable's listing and bytes, the map that names the cells, the table the plane is read from.
SCRIPTS = ("x87_machine.py", "cycle_plane_oracle.py", "formulas_statistics.py", SURVEY_SCRIPT)


def sha256_of(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def provenance() -> Dict[str, object]:
    return {
        "executable_sha256": co.lc.sha256(co.lc.exe_path()), "excerpt_sha256": sha256_of(co.lc.listing_path()),
        "map_sha256": sha256_of(co.MAP_PATH), "site_table_sha256": sha256_of(co.TABLE_PATH),
        "scripts_sha256": {name: sha256_of(HERE / name) for name in SCRIPTS},
    }


def survey_one(name: str, oracle_map: co.OracleMap, sites: List[co.Site]) -> Dict[str, object]:
    """One formulation, the menu at its defaults: the pre-loop run once, what it left read back."""
    case = {"id": name, "formulation": name, "menu": {}, "flags": None, "setup": {}, "seed": 0,
            "max_cycles": 0, "totals": {}}
    setup = co.case_setup(case)
    entry: Dict[str, object] = {"formulation": name, "datSha256": sha256_of(co.FORMULATIONS / name),
                                "jzz": setup["jzz"], "nmm": setup["nmm"]}
    world = co.World(setup, oracle_map, sites)
    try:
        world.run_init()
    except co.xm.Stop as error:
        entry["stop"] = str(error)
        return entry
    entry["steps"] = world.machine.steps
    entry["preloop"] = co.read_preloop(world)
    return entry


def jzz2_controls(oracle_map: co.OracleMap, sites: List[co.Site]) -> List[Dict[str, object]]:
    """The self-test's JZZ = 2 draws (`cycle_plane_oracle.jzz2_runs`): their shares and bounds as the
    executable was given them and what its pre-loop left. `Setup.AnalyticSizes` is held to these bits in
    C# (`PreloopSurveyTests`), on loops no shipped file has."""
    controls: List[Dict[str, object]] = []
    for overrides, _check, seen in co.jzz2_runs(oracle_map, sites):
        controls.append({"shares": overrides["gdok"], "bounds": overrides["ddok"],
                         "zx": seen["zx"]["executed"], "dokm": seen["dokm"]["executed"],
                         "doksd": seen["doksd"]["executed"]})
    return controls


def build() -> Dict[str, object]:
    oracle_map = co.load_map()
    sites = co.load_sites()
    return {
        "format": SURVEY_FORMAT,
        "fortranLines": SURVEY_FORTRAN_LINES,
        "script": SURVEY_SCRIPT,
        "provenance": provenance(),
        "formulations": [survey_one(name, oracle_map, sites) for name in co.formulation_names()],
        "jzz2Controls": jzz2_controls(oracle_map, sites),
    }


def render(survey: Dict[str, object]) -> str:
    return json.dumps(survey, indent=1, sort_keys=False) + "\n"


def cmd_generate() -> int:
    survey = build()
    SURVEY_PATH.parent.mkdir(parents=True, exist_ok=True)
    SURVEY_PATH.write_text(render(survey), encoding="utf-8", newline="\n")
    print(f"wrote {SURVEY_PATH} ({len(survey['formulations'])} formulations)")
    return 0


def cmd_verify() -> int:
    committed = SURVEY_PATH.read_text(encoding="utf-8")
    fresh = render(build())
    if committed != fresh:
        print(f"verify: {SURVEY_PATH.name} differs from what the executable's pre-loop leaves today", file=sys.stderr)
        return 1
    print(f"verify: {SURVEY_PATH.name} reproduces byte for byte")
    return 0


def main(argv: List[str]) -> int:
    commands = {"generate": cmd_generate, "verify": cmd_verify}
    if len(argv) != 1 or argv[0] not in commands:
        print(f"usage: {SURVEY_SCRIPT} generate|verify", file=sys.stderr)
        return 2
    return commands[argv[0]]()


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
