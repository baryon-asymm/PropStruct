"""Runs PropStructV3.exe once for a deterministic, single-seed measurement — the
apparatus the arbiter's cause-finding order needs (`tests/Harness/HISTORY.md`, "how
many accepted particles pass before the port and the original first print different
integer counts", and every measurement built on it since): a single seed jump,
an optional N/KXX override for particle-count control, an optional `--alfa` to decline
the default-parameters menu and set item [9] (`src/Output`'s O-1 fixture), an optional
`--karmcoef` to decline it and set item [7] instead (`src/Output`'s "print the stored
setup" fixture, 2026-09-24), and no statistics, replica set or exclusion list involved
at all. `--alfa` and `--karmcoef` are mutually exclusive: the menu session this script
drives navigates to one item and starts modelling, not several, matching the one
fixture each flag was written for.

Reuses `generate.py`'s own proven seed values and jump arithmetic without a second
implementation of any of it (root BOOT.md's Taboos): `original_stream_value`,
`independent_stream_value`, `advanced`, `patched_executable`, `SEED_OFFSETS`,
`replace_header_token`, `run_case`. The one arithmetic difference from a `Fixtures`
replica's own jump is deliberate, not an oversight: this script's `--seed K` jumps by
`K * 2^80` (`src/Random/API.md`'s `OriginalSeeds.ForParticle(seed, ordinal)` at
`ordinal = 0`, the only ordinal reference mode ever uses), not a lagged/independent
replica's `K * 2^80 + 2^79` (`replica_jump_exponent`, BOOT.md's "Seed-patched
replicas"): the two are different constructions for different purposes, and using the
replica jump here would compare the port against a seed no reference-mode run of the
port ever reaches.

Writes `results.m` wherever `--out` says. Never writes into `references/`,
`replicas-*/` or `provenance.json`: a run of this script is a throwaway measurement,
not a fixture (BOOT.md's Taboos, "no replica produced by the port" — not applicable
here since both sides of this script are the original executable — and its own
"Generation is a manual, recorded step, not part of any test run", which this script
follows by writing nowhere inside the committed fixture tree on its own). One output has
been committed as a fixture all the same,
`cases/output/nonzero-tail-probability/results.m.txt` (`API.md`, "## Layout"): this
script still wrote it to a throwaway `--out` path exactly as it would any other run, and
a human then copied the bytes into the committed tree and recorded the provenance by
hand, in `provenance.json`'s own shape — the script's own "never writes there" is
unchanged.

See ./API.md, "## Measurement scripts", for the full command-line contract.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import argparse
import json
import sys
import tempfile
from pathlib import Path
from typing import Dict, Optional

import generate

LAYOUTS = {
    "original": generate.original_stream_value,
    "independent": generate.independent_stream_value,
}


def reference_mode_jump_exponent(seed: int) -> int:
    """`src/Random/API.md`'s `OriginalSeeds.ForParticle(seed, ordinal)` /
    `IndependentSeeds.ForParticle(seed, ordinal)`, both `seed*2^80 + ordinal*2^40`,
    evaluated at `ordinal = 0` — the only ordinal a continuous, unbatched reference-mode
    run ever uses. Distinct from `generate.replica_jump_exponent`, which adds `2^79`
    for a different purpose (BOOT.md's "Seed-patched replicas": distinct, reproducible
    replicas of the *original*, not a matched seed for the *port*)."""
    return seed * (1 << 80)


def states_for_seed(layout: str, seed: int) -> Dict[int, int]:
    """The six patched states for `--layout original|independent --seed K`, always the
    layout's own zero-ordinal state (`generate.original_stream_value`/
    `generate.independent_stream_value`) advanced by `K`'s jump. No special case for
    `K == 0`: at that exponent `generate.advanced` is the identity, so this *is* the
    layout's own un-jumped initial state, for both layouts alike (BOOT.md, "The
    `--seed 0`/`--layout` defect of `run_original.py`, and its fix" — patching
    `--layout original` in with its own un-jumped state reproduces the shipped
    executable's bytes exactly, since those are the values already baked into it; for
    `--layout independent` it is the real, distinct state `IndependentSeeds.
    ForParticle(0, 0)` reaches on the port side, not a stand-in for it). A value-
    dependent branch here (patch for K != 0, skip the patch for K == 0) is exactly the
    shape of the defect this function used to have: it let `--layout` be accepted and
    silently not applied whenever `K == 0`."""
    base_value = LAYOUTS[layout]
    exponent = reference_mode_jump_exponent(seed)
    return {stream: generate.advanced(base_value(stream), exponent) for stream in generate.SEED_OFFSETS}


def menu_stdin(name: str, alfa: Optional[float], karmcoef: Optional[float] = None) -> bytes:
    """The interactive session's stdin: `<name>` then, with both `alfa` and `karmcoef`
    omitted, `y` (accept every default menu parameter, `run_case`'s own default when no
    `stdin` is passed — this function reproduces it explicitly so both branches are
    visible together). With `alfa` given, `n` (decline the defaults), then the menu's
    own item `[9]: P[Dok>Dok_max]` (`alfa`, Fortran lines 148-149/206-207) with the
    requested value, then `0` to start modelling with every other parameter left at its
    compiled-in default (Fortran lines 62-76) — the only parameter this function ever
    changes for that caller. With `karmcoef` given instead, the same `n`/`0` shape
    around the menu's item `[7]: k7` (`karmcoef`, Fortran lines 159-161/199-201,
    compiled-in default 8.2, Fortran line 76). The two are mutually exclusive callers
    (`run`'s own docstring); this function does not special-case both given at once.
    Fortran's list-directed `read*, alfa`/`read*, karmcoef` (lines 207/201) accept a
    plain decimal token, one per line, the same shape `str(float)` produces for every
    value this script is documented to take (`API.md`, "## Measurement scripts")."""
    if karmcoef is not None:
        return f"{name.lower()}\nn\n7\n{karmcoef}\n0\n".encode("ascii")
    if alfa is None:
        return f"{name.lower()}\ny\n".encode("ascii")
    return f"{name.lower()}\nn\n9\n{alfa}\n0\n".encode("ascii")


def run(
    name: str, layout: str, seed: int, n: Optional[int], kxx: Optional[int],
    alfa: Optional[float] = None, karmcoef: Optional[float] = None,
) -> bytes:
    """Runs the original once and returns the raw `results.m` bytes. `n`/`kxx`, when
    given, override the shipped `.dat`'s own `N`/`KXX` header fields (indices 3 and 2
    of the six-value record, `generate.replace_header_token`); everything else in the
    `.dat` is the shipped formulation, unchanged. `--layout` and `--seed` are always
    applied by patching the executable with `states_for_seed`'s states: there is no
    input for which either flag is accepted and then not honoured (BOOT.md, "The
    `--seed 0`/`--layout` defect of `run_original.py`, and its fix"). `alfa`/`karmcoef`,
    when given, are applied by `menu_stdin` above, the same "always applied, never
    silently skipped" rule the other flags already follow; `main` below refuses to call
    this with both set, so `menu_stdin` never has to arbitrate between them."""
    dat_bytes = (generate.FORMULATIONS / f"{name}.dat").read_bytes()
    if kxx is not None:
        dat_bytes = generate.replace_header_token(dat_bytes, 2, str(kxx).encode("ascii"))
    if n is not None:
        dat_bytes = generate.replace_header_token(dat_bytes, 3, str(n).encode("ascii"))

    states = states_for_seed(layout, seed)
    executable_bytes = generate.patched_executable(states)
    stdin = menu_stdin(name, alfa, karmcoef)

    with tempfile.TemporaryDirectory(prefix="propstruct_original_") as tmp:
        _, output_bytes = generate.run_case(
            name, 2, Path(tmp), executable_bytes=executable_bytes, dat_bytes=dat_bytes, stdin=stdin
        )
    return output_bytes


def main(argv: list) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument(
        "name", nargs="?",
        help="shipped formulation name, e.g. HMX, HPEPA3, inpt (omit with --print-states)",
    )
    parser.add_argument("--layout", choices=sorted(LAYOUTS), required=True)
    parser.add_argument(
        "--seed", type=int, default=None,
        help="K; the layout's own zero-ordinal state jumped by K*2^80 (K=0 is that "
        "state un-jumped, byte-identical to the shipped executable for --layout original); "
        "required unless --print-states",
    )
    parser.add_argument("--n", type=int, default=None, help="override the .dat's own N (particles per cycle)")
    parser.add_argument("--kxx", type=int, default=None, help="override the .dat's own KXX (cycle count)")
    parser.add_argument(
        "--alfa", type=float, default=None,
        help="menu item [9], P[Dok>Dok_max] (recommended 1e-5); decline the default-parameters "
        "prompt and set only this item, every other parameter left at its compiled-in default "
        "(omit for the unmodified default-parameters session, alfa = 0)",
    )
    parser.add_argument(
        "--karmcoef", type=float, default=None,
        help="menu item [7], k7 = P[karm-in-karm] coef. (compiled-in default 8.2); decline the "
        "default-parameters prompt and set only this item, every other parameter left at its "
        "compiled-in default; mutually exclusive with --alfa",
    )
    parser.add_argument("--out", default=None, help="path to write the raw results.m bytes to; required unless --print-states")
    parser.add_argument(
        "--print-states", action="store_true",
        help="print the six patched states (states_for_seed) for --layout and each of --seeds as JSON to "
        "stdout, instead of running the executable; a cross-check entry point for tests/Random.Tests "
        "(never runs PropStructV3.exe, writes nothing); takes only --layout and --seeds",
    )
    parser.add_argument(
        "--seeds", type=int, nargs="+", default=None,
        help="seeds to print states for, with --print-states (in place of the single-run --seed)",
    )
    args = parser.parse_args(argv)

    if args.print_states:
        if args.seeds is None:
            parser.error("--print-states requires --seeds")
        if any(v is not None for v in (args.name, args.seed, args.out, args.n, args.kxx, args.alfa, args.karmcoef)):
            parser.error("--print-states takes only --layout and --seeds")
        states = {
            str(seed): {str(stream): str(value) for stream, value in states_for_seed(args.layout, seed).items()}
            for seed in args.seeds
        }
        print(json.dumps({"layout": args.layout, "states": states}))
        return 0

    if args.name is None or args.seed is None or args.out is None:
        parser.error("name, --seed and --out are required unless --print-states is given")
    if args.alfa is not None and args.karmcoef is not None:
        parser.error("--alfa and --karmcoef are mutually exclusive (menu_stdin navigates to one item)")

    output_bytes = run(args.name, args.layout, args.seed, args.n, args.kxx, args.alfa, args.karmcoef)
    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(output_bytes)
    print(f"{args.out}: {generate.sha256_hex(output_bytes)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
