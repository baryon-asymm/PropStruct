"""Regression check for `run_original.py` and `run_port.py` (`API.md`, "## Measurement
scripts"): no flag either script's parser accepts may be silently not applied to the
run it produces.

Written after finding exactly this shape of defect in `run_original.py`'s own
`states_for_seed` (`BOOT.md`, ## Invariants, "A measurement script never accepts a flag
it does not honour"): a branch on `seed == 0` skipped the executable patch — and so
the `--layout` it was supposed to select — regardless of which layout was asked for,
so `--layout independent --seed 0` silently reproduced `--layout original --seed 0`.

What this checks: `--layout original` and `--layout independent` must produce outputs
that differ outside the time line and the pre-existing `pdoksmall(1:2)` garbage
(`generate._normalized_for_comparison`, the same normalization `generate.py`'s own
`seeds locate` uses for the same reason — those two are the only legitimate source of
agreement despite different input). Checked at `--seed 0` (the seed the defect lived
at) and `--seed 1` (a seed neither script ever special-cased, so it is expected to
pass before and after the fix — its purpose is to show the check is not vacuously
comparing two runs that always differ for an unrelated reason).

Proven non-degenerate 2026-09-21 by running this file against `run_original.py` with
its pre-fix `states_for_seed` (the `seed == 0` branch returning `None` regardless of
`layout`, reinstated locally for the comparison and reverted immediately after): the
`--seed 0` case failed with exactly the message below, the `--seed 1` case passed
(both layouts already honoured at that seed before the fix), and against the fixed
script both pass. A check that could not fail on the broken script would be indistin-
guishable from no check at all (AGENTS.md §13).

Runs `PropStructV3.exe` (via `run_original.run`, twice per seed checked) and, when a
Release build of `PropStruct.sln` is present, `propstruct` (via `run_port.run`, twice);
the port half is skipped with a printed reason, never silently, when no build is
found — this script's own "no silent skip" the same rule it checks for.

Writes nothing under `tests/Fixtures/`: every run goes to a temporary directory
(`run_original.run`/`run_port.run`'s own), the same "throwaway measurement" convention
`run_original.py`/`run_port.py` themselves follow (`BOOT.md`, ## Constraints,
"Generation is a manual, recorded step, not part of any test run").

Python 3.8+, standard library only.
"""

from __future__ import annotations

import sys

import generate
import run_original
import run_port

CHECK_FORMULATION = "inpt"  # smallest shipped formulation: fast
CHECK_N = 5
CHECK_KXX = 1


def _normalized(output_bytes: bytes) -> str:
    return generate._normalized_for_comparison(output_bytes.decode("latin1"))


def _require_divergence(script: str, name: str, seed: int, original_bytes: bytes, independent_bytes: bytes) -> None:
    if _normalized(original_bytes) == _normalized(independent_bytes):
        raise SystemExit(
            f"{script}: --layout original and --layout independent produced "
            f"indistinguishable output at --seed {seed} for {name} (outside the time "
            "line and pdoksmall(1:2)) -- --layout was accepted and not honoured."
        )
    print(f"{script} {name} --seed {seed}: --layout original/independent diverge as expected (OK)")


def check_run_original(name: str, seed: int) -> None:
    original_bytes = run_original.run(name, "original", seed, CHECK_N, CHECK_KXX)
    independent_bytes = run_original.run(name, "independent", seed, CHECK_N, CHECK_KXX)
    _require_divergence("run_original.py", name, seed, original_bytes, independent_bytes)


def check_run_port(name: str, seed: int) -> None:
    try:
        propstruct = run_port.default_propstruct_path()
    except SystemExit as exc:
        print(f"run_port.py {name} --seed {seed}: skipped, {exc}")
        return
    original_bytes = run_port.run(name, "original", seed, CHECK_N, CHECK_KXX, propstruct)
    independent_bytes = run_port.run(name, "independent", seed, CHECK_N, CHECK_KXX, propstruct)
    _require_divergence("run_port.py", name, seed, original_bytes, independent_bytes)


def main(argv: list) -> int:
    del argv  # no arguments: this is a fixed regression check, not a general tool
    for seed in (0, 1):
        check_run_original(CHECK_FORMULATION, seed)
    check_run_port(CHECK_FORMULATION, 0)
    print("check_measurement_scripts: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
