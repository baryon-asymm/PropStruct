"""Runs the port's `propstruct` CLI once for a deterministic, single-seed
measurement matched to `run_original.py`'s own run: same formulation, layout, seed
and N/KXX override, so the two files this pair of scripts writes are directly
comparable cell for cell (`tests/Harness`'s `ResultsMFile`, never a second parser).

Reuses `generate.py`'s own header-override helper (`replace_header_token`) for the
N/KXX override, the one piece of machinery both scripts need — never a second
implementation of it. Everything else is `propstruct run` itself
(`src/Cli/API.md`): `--mode reference --layout <layout> --seed <seed>`, exactly the
flags root BOOT.md's execution model and `src/Random/API.md`'s `OriginalSeeds.
ForParticle`/`IndependentSeeds.ForParticle` already define, so `--seed K` here reaches
the same seed as `run_original.py --seed K` for the same `--layout` by construction
(both call `ForParticle(seed, ordinal = 0)`, reference mode's only ordinal) — no
seed arithmetic of this script's own.

Needs a Release build of `PropStruct.sln` first (`dotnet build PropStruct.sln -c
Release`, root `CLAUDE.md`); `--propstruct` points at the built executable/DLL,
defaulting to the conventional `src/Cli/bin/Release/net10.0/propstruct(.exe|.dll)`
under the repository root this script's own `generate.NODE_ROOT` resolves.

Writes `results.m` wherever `--out` says; touches nothing else in the tree (no
`references/`, `replicas-*/` or `provenance.json` — the root's own "no replica
produced by the port" taboo, and this script's own runs are throwaway measurements).

See ./API.md, "## Measurement scripts", for the full command-line contract.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Optional

import generate

RUN_TIMEOUT_SECONDS = 600


def default_propstruct_path() -> Path:
    repo_root = generate.NODE_ROOT.parents[1]
    build_dir = repo_root / "src" / "Cli" / "bin" / "Release" / "net10.0"
    exe = build_dir / "propstruct.exe"
    if exe.is_file():
        return exe
    dll = build_dir / "propstruct.dll"
    if dll.is_file():
        return dll
    raise SystemExit(
        f"no built propstruct found under {build_dir}; run "
        "'dotnet build PropStruct.sln -c Release' first, or pass --propstruct <path>."
    )


def run(name: str, layout: str, seed: int, n: Optional[int], kxx: Optional[int], propstruct: Path) -> bytes:
    """Runs `propstruct run` once in reference mode and returns the raw `results.m`
    bytes. `n`/`kxx`, when given, override the shipped `.dat`'s own `N`/`KXX` header
    fields the same way `run_original.py` does, so both sides see the same input."""
    dat_bytes = (generate.FORMULATIONS / f"{name}.dat").read_bytes()
    if kxx is not None:
        dat_bytes = generate.replace_header_token(dat_bytes, 2, str(kxx).encode("ascii"))
    if n is not None:
        dat_bytes = generate.replace_header_token(dat_bytes, 3, str(n).encode("ascii"))

    with tempfile.TemporaryDirectory(prefix="propstruct_port_") as tmp:
        tmp_dir = Path(tmp)
        dat_path = tmp_dir / f"{name}.dat"
        dat_path.write_bytes(dat_bytes)
        output_path = tmp_dir / "results.m"

        command = [str(propstruct)] if propstruct.suffix.lower() == ".exe" else ["dotnet", str(propstruct)]
        command += [
            "run", str(dat_path),
            "--mode", "reference",
            "--layout", layout,
            "--seed", str(seed),
            "--output", str(output_path),
            "--quiet",
        ]
        proc = subprocess.run(command, capture_output=True, timeout=RUN_TIMEOUT_SECONDS)
        if proc.returncode != 0:
            raise RuntimeError(
                f"propstruct exited {proc.returncode} for {name} "
                f"(layout={layout}, seed={seed}); stderr: {proc.stderr[-2000:]!r}"
            )
        return output_path.read_bytes()


def main(argv: list) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("name", help="shipped formulation name, e.g. HMX, HPEPA3, inpt")
    parser.add_argument("--layout", choices=("original", "independent"), required=True)
    parser.add_argument("--seed", type=int, required=True)
    parser.add_argument("--n", type=int, default=None, help="override the .dat's own N (particles per cycle)")
    parser.add_argument("--kxx", type=int, default=None, help="override the .dat's own KXX (cycle count)")
    parser.add_argument("--out", required=True, help="path to write the raw results.m bytes to")
    parser.add_argument("--propstruct", default=None, help="path to the built propstruct.exe/.dll")
    args = parser.parse_args(argv)

    propstruct = Path(args.propstruct) if args.propstruct else default_propstruct_path()
    output_bytes = run(args.name, args.layout, args.seed, args.n, args.kxx, propstruct)
    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(output_bytes)
    print(f"{args.out}: {generate.sha256_hex(output_bytes)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
