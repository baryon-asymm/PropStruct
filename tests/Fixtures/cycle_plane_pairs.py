"""Path-identical small-N pairs: the original executable and the port run on the same
input, so that the per-cycle plane's printed cells can be compared token for token while
every integer cell agrees (`src/Statistics/BOOT.md`, "## Defects of the original", rows
771-1175; `API.md`, "## Cycle-plane pairs").

One pair per formulation of `PAIRS`: `PropStructV3.exe` with the six seeds of `--layout
original --seed 1` patched in (`run_original.states_for_seed`, no second implementation
of the jump) against `propstruct run --mode reference --layout original --seed 1
--precision original`, both on the shipped `.dat` with its `KXX` and `N` header tokens
replaced (`generate.replace_header_token`). Menu parameters are the original's defaults,
except `PSAN01`, whose pocket-forming flags are read (menu [12], `--sfr` on the port):
the shipped `.dat` carries them and `gdokleft` is zero without them. This script adds to
the two measurement scripts exactly what they cannot say: `--precision original` for the
port and the menu session of [12] for the original.

`ladder` prints, for every formulation, `K` in 1..2 and `N` in 50..1600 (doubling), the
number of integer-printed tokens that differ between the two programs (a token with no
decimal point or exponent, the rule `tests/Harness`'s `ResultCell.IsIntegerPrinted`
uses) and the number of all tokens that differ. `PAIRS` takes, per formulation, the pair
of largest `(K + 1) * N` whose integer count is zero (the method of
`tests/Harness/HISTORY.md`, "how many accepted particles pass before the port and the
original first print different integer counts"); agreement is not monotone in `N`,
because `N` changes the totals the per-cycle plane hands to the next cycle, so the
ladder is the evidence, not a search.

`generate` runs every pair and writes `cycle-plane-pairs/<name>/original.m.txt`,
`port.m.txt` and `cycle-plane-pairs/provenance.json`. It needs a Release build of
`PropStruct.sln` (`run_port.py`'s own rule) and `PROPSTRUCT_NO_CUDA=1`.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, Tuple

import generate
import run_original
import run_port

PAIRS_DIRECTORY = generate.NODE_ROOT / "cycle-plane-pairs"
PROVENANCE = PAIRS_DIRECTORY / "provenance.json"
SEED = 1
LAYOUT = "original"

# name -> (N, KXX, read the pocket-forming flags): see the module docstring and
# `ladder`.
PAIRS: Dict[str, Tuple[int, int, bool]] = {
    "HMX": (800, 1, False),
    "HPEPA3": (1600, 2, False),
    "PSAN01": (1600, 2, True),
    "PSAN02n": (1600, 2, False),
    "P33": (1600, 2, False),
    "inpt": (1600, 2, False),
}

LADDER_N = (50, 100, 200, 400, 800, 1600)
LADDER_K = (1, 2)

NUMBER = re.compile(r"[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?|NaN|Infinity")
INTEGER = re.compile(r"[+-]?\d+")
PORT_ONLY_PREFIXES = (" % Precision:", " % Stream layout:", " % Execution mode:")


def input_bytes(name: str, n: int, kxx: int) -> bytes:
    dat = (generate.FORMULATIONS / f"{name}.dat").read_bytes()
    dat = generate.replace_header_token(dat, 2, str(kxx).encode("ascii"))
    return generate.replace_header_token(dat, 3, str(n).encode("ascii"))


def original_session(name: str, n: int, kxx: int, sfr: bool) -> Tuple[bytes, bytes, bytes]:
    """(input bytes run, standard input sent, results.m bytes)."""
    dat = input_bytes(name, n, kxx)
    executable = generate.patched_executable(run_original.states_for_seed(LAYOUT, SEED))
    stdin = f"{name.lower()}\nn\n12\ny\n0\n".encode("ascii") if sfr else run_original.menu_stdin(name, None, None)
    with tempfile.TemporaryDirectory(prefix="propstruct_pair_original_") as tmp:
        sent, output = generate.run_case(name, 2, Path(tmp), executable_bytes=executable, dat_bytes=dat, stdin=stdin)
    return sent, stdin, output


def port_session(name: str, n: int, kxx: int, sfr: bool, propstruct: Path) -> Tuple[List[str], bytes]:
    """(the command line without the temporary paths, results.m bytes)."""
    with tempfile.TemporaryDirectory(prefix="propstruct_pair_port_") as tmp:
        directory = Path(tmp)
        dat_path = directory / f"{name}.dat"
        dat_path.write_bytes(input_bytes(name, n, kxx))
        output_path = directory / "results.m"
        flags = ["--mode", "reference", "--layout", LAYOUT, "--seed", str(SEED), "--precision", "original"]
        if sfr:
            flags.append("--sfr")
        launcher = [str(propstruct)] if propstruct.suffix.lower() == ".exe" else ["dotnet", str(propstruct)]
        command = launcher + ["run", str(dat_path)] + flags + ["--output", str(output_path), "--quiet"]
        completed = subprocess.run(command, capture_output=True, timeout=run_port.RUN_TIMEOUT_SECONDS)
        if completed.returncode != 0:
            raise RuntimeError(f"propstruct exited {completed.returncode} for {name}: {completed.stderr[-2000:]!r}")
        return ["propstruct", "run", f"{name}.dat"] + flags + ["--quiet"], output_path.read_bytes()


def tokens_by_line(data: bytes) -> List[List[str]]:
    text = data.decode("utf-8", "replace").replace("\r\n", "\n")
    lines = [
        line for line in text.split("\n")
        if "Calculation time" not in line and not line.startswith(PORT_ONLY_PREFIXES)
    ]
    return [NUMBER.findall(line) for line in lines]


def differing_tokens(first: bytes, second: bytes) -> Tuple[int, int]:
    """(integer-printed tokens that differ, all tokens that differ); a token a file has
    and the other lacks differs."""
    a, b = tokens_by_line(first), tokens_by_line(second)
    integer = total = 0
    for i in range(max(len(a), len(b))):
        left = a[i] if i < len(a) else []
        right = b[i] if i < len(b) else []
        for j in range(max(len(left), len(right))):
            x = left[j] if j < len(left) else None
            y = right[j] if j < len(right) else None
            if x != y:
                total += 1
                if any(t is not None and INTEGER.fullmatch(t) for t in (x, y)):
                    integer += 1
    return integer, total


def cmd_ladder(propstruct: Path) -> None:
    for name, (_, _, sfr) in PAIRS.items():
        for kxx in LADDER_K:
            for n in LADDER_N:
                _, _, original = original_session(name, n, kxx, sfr)
                _, port = port_session(name, n, kxx, sfr, propstruct)
                integer, total = differing_tokens(original, port)
                print(f"{name} K={kxx} N={n}: integer tokens differing {integer}, all tokens differing {total}", flush=True)


def cmd_generate(propstruct: Path) -> None:
    entries: List[dict] = []
    for name, (n, kxx, sfr) in PAIRS.items():
        sent, stdin, original = original_session(name, n, kxx, sfr)
        command, port = port_session(name, n, kxx, sfr, propstruct)
        integer, total = differing_tokens(original, port)
        if integer != 0:
            raise SystemExit(f"{name}: N={n} K={kxx} no longer has every integer token equal ({integer} differ); rerun ladder.")
        directory = PAIRS_DIRECTORY / name
        directory.mkdir(parents=True, exist_ok=True)
        (directory / "original.m.txt").write_bytes(original)
        (directory / "port.m.txt").write_bytes(port)
        common = {
            "formulation": name, "n": n, "kxx": kxx, "seed": SEED, "layout": LAYOUT, "readsPocketFormingFractions": sfr,
            "inputSha256": generate.sha256_hex(sent), "shippedInputSha256": generate.sha256_hex((generate.FORMULATIONS / f"{name}.dat").read_bytes()),
            "date": generate.now_iso(), "scriptCommit": generate.git_head(),
        }
        entries.append({
            "output": f"{name}/original.m.txt", "side": "original", "outputSha256": generate.sha256_hex(original),
            "executableSha256": generate.sha256_hex(generate.executable_path().read_bytes()), "stdin": stdin.decode("ascii"),
            "patchedExecutableSha256": generate.sha256_hex(generate.patched_executable(run_original.states_for_seed(LAYOUT, SEED))),
            **common,
        })
        entries.append({
            "output": f"{name}/port.m.txt", "side": "port", "outputSha256": generate.sha256_hex(port),
            "command": " ".join(command), **common,
        })
        print(f"{name}: N={n} K={kxx}: all tokens differing {total}, integer tokens differing {integer}", flush=True)
    PROVENANCE.write_text(json.dumps(entries, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def main(argv: List[str]) -> int:
    if len(argv) not in (1, 2) or argv[0] not in ("ladder", "generate"):
        print("usage: cycle_plane_pairs.py ladder|generate [path to propstruct.exe/.dll]", file=sys.stderr)
        return 2
    propstruct = Path(argv[1]) if len(argv) == 2 else run_port.default_propstruct_path()
    (cmd_ladder if argv[0] == "ladder" else cmd_generate)(propstruct)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
