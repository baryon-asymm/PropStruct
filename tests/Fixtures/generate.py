"""Runs the original's PropStructV3.exe to produce reference and replica outputs,
verifies Legacy/ (the archive's data) against the archive's members, and derives the
three fixtures the fast set reads in place of the original. The original's files lie
outside the repository, in the directory PROPSTRUCT_LEGACY_DIR names (tools/legacy).

See ./API.md, section "## generate.py", for the command-line contract; this file
implements exactly that contract and nothing else. Generation is a manual, recorded
step (BOOT.md, ## Constraints): it is never invoked from a test run.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import textwrap
import threading
import time
import zipfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from typing import Dict, List, Optional, Tuple

NODE_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(NODE_ROOT.parents[1] / "tools" / "legacy"))
import legacy  # noqa: E402  (the one reader of PROPSTRUCT_LEGACY_DIR, tools/legacy/API.md)

LEGACY = NODE_ROOT / "Legacy"
FORMULATIONS = LEGACY / "formulations"
OUTPUTS = LEGACY / "outputs"
EXE_NAME = "PropStructV3.exe"
DLL_NAME = "dforrt.dll"
FOR_TXT_NAME = "PropStructv3.for.txt"
ARCHIVE_NAME = "PropStructV3.zip"
MEMBERS_PATH = NODE_ROOT / "archive-members.sha256"
EXECUTABLE_LINES_PATH = NODE_ROOT / "cases" / "source" / "executable_lines.json"
SOURCE_LIMBS_PATH = NODE_ROOT / "cases" / "random" / "source_limbs.json"


# The source's line ranges whose slash-delimited integers are the seed and multiplier limbs
# (src/Random/BOOT.md, ## Invariants): one stream per pair of lines, then the multiplier.
SOURCE_LIMB_RANGES = ((52, 53), (54, 55), (56, 57), (58, 59), (60, 61), (62, 63), (1649, 1650))


def executable_path() -> Path:
    """The verified path of the original's executable, resolved at the call so that an
    entry that runs nothing works without PROPSTRUCT_LEGACY_DIR."""
    return Path(legacy.legacy_file(EXE_NAME))


def runtime_path() -> Path:
    return Path(legacy.legacy_file(DLL_NAME))


def source_path() -> Path:
    return Path(legacy.legacy_file(FOR_TXT_NAME))


def archive_path() -> Path:
    return Path(legacy.legacy_file(ARCHIVE_NAME))


PROVENANCE_PATH = NODE_ROOT / "provenance.json"
REFERENCES = NODE_ROOT / "references"
REPLICAS_BY_LAYOUT = {
    "gsv3": NODE_ROOT / "replicas-gsv3",
    "lagged": NODE_ROOT / "replicas-lagged",
    "independent": NODE_ROOT / "replicas-independent",
}

# BOOT.md, ## Constraints: reference formulations and their replica counts. The same
# counts apply to every replica kind (lagged, independent, GSV=3).
REPLICA_COUNTS: Dict[str, int] = {
    "HPEPA3": 32,
    "inpt": 16,
    "P33": 16,
    "PSAN02n": 16,
    "HMX": 16,
}

MAX_PARALLEL = 12
STAGGER_SECONDS = 1.1
RUN_TIMEOUT_SECONDS = 300

# ---------------------------------------------------------------------------
# GSV=2 seed state, independent of the C# port (Fixtures/BOOT.md, ## Invariants:
# "the states are computed from their definitions in src/Random/BOOT.md ... never
# copied from C# code"). The generator is s_{n+1} = a*s_n mod 2^128; the state is
# stored in the executable as ten little-endian int32 limbs, base 2^13, in the file
# order u9..u0 (BOOT.md, "Seed offsets are proven, not assumed").
# ---------------------------------------------------------------------------

STATE_MODULUS = 1 << 128
LIMB_BITS = 13
LIMB_COUNT = 10  # nine 13-bit limbs (u0..u8) plus one top limb (u9)

# src/Random/BOOT.md, ## Invariants: the multiplier's limbs.
_MULTIPLIER_LIMBS = (1, 0, 7916, 6769, 8113, 7234, 4142, 5015, 3567, 1526)
# src/Random/BOOT.md, ## Invariants: stream 3's transposed limb (3784 where a^3 has 3748).
_STREAM3_LIMBS = (1, 0, 7364, 3925, 7109, 884, 2888, 4164, 3784, 1899)


def limb_value(limbs: Tuple[int, ...]) -> int:
    """The 128-bit value packed from ten limbs, base 2^13, low limb first."""
    return sum(limb << (LIMB_BITS * i) for i, limb in enumerate(limbs))


def value_limbs(value: int) -> Tuple[int, ...]:
    """The ten limbs (base 2^13, low limb first) of a 128-bit state or seed value."""
    limbs = [(value >> (LIMB_BITS * i)) & ((1 << LIMB_BITS) - 1) for i in range(LIMB_COUNT - 1)]
    limbs.append(value >> (LIMB_BITS * (LIMB_COUNT - 1)))
    return tuple(limbs)


def pack_state_block(value: int) -> bytes:
    """The 40-byte on-disk block for a state: ten little-endian int32 limbs, file
    order u9..u0 (the limbs of value_limbs() reversed)."""
    return b"".join(struct.pack("<i", limb) for limb in reversed(value_limbs(value)))


def unpack_state_block(block: bytes) -> int:
    limbs = list(struct.unpack("<10i", block))
    limbs.reverse()  # file order is u9..u0; value_limbs()/limb_value() want u0..u9
    return limb_value(tuple(limbs))


MULTIPLIER = limb_value(_MULTIPLIER_LIMBS)

# Seed offsets, proven 2026-09-17 (Fixtures/BOOT.md, "Seed offsets are proven, not
# assumed"): file offsets of each stream's u9 limb, i.e. the start of its 40-byte
# block. Streams 1..5 were proven by an earlier session; stream 6 by this one
# (`seeds locate`, below). `None` would make `replicas --layout lagged|independent`
# refuse (see `_require_proven_offsets`).
SEED_OFFSETS: Dict[int, Optional[int]] = {
    1: 0x20B58,
    2: 0x20B80,
    3: 0x20BA8,
    4: 0x20BD0,
    5: 0x20BF8,
    6: 0x20C20,
}
# Neighbouring blocks that are not seeds and must never be patched (BOOT.md, "Seed
# offsets are proven, not assumed"): the multiplier `a` of random2 (same limbs as
# stream 5, since stream 5 = a^1) and a block of counters.
MULTIPLIER_OFFSET = 0x20B08
COUNTERS_OFFSET = 0x20B30


def original_stream_value(stream: int) -> int:
    """The original GSV=2 seed of `stream` (1..6), src/Random/BOOT.md, ## Invariants:
    "streams 6, 5, 4, 2, 1 equal a^0, a^1, a^2, a^4, a^5; stream 3 carries the limb
    3784 where a^3 has 3748"."""
    if stream == 3:
        return limb_value(_STREAM3_LIMBS)
    return pow(MULTIPLIER, 6 - stream, STATE_MODULUS)


def independent_stream_value(stream: int) -> int:
    """The `Independent` layout's initial state for role `stream` (1..6),
    src/Random/BOOT.md, ## Invariants, "Independent layout": role r starts at
    H_r * 2^28 + (2r + 1), H_r the first 100 bits of
    SHA-256("PropStruct.Random.Independent/r") (ASCII, r in decimal)."""
    digest = hashlib.sha256(f"PropStruct.Random.Independent/{stream}".encode("ascii")).digest()
    top100 = int.from_bytes(digest, "big") >> (256 - 100)
    return (top100 << 28) | (2 * stream + 1)


def replica_jump_exponent(k: int) -> int:
    """src/Random/BOOT.md, ## Invariants, 'Replica jumps': replica k >= 1 jumps every
    initial state by k*2^80 + 2^79."""
    return k * (1 << 80) + (1 << 79)


def advanced(value: int, exponent: int) -> int:
    """`exponent` successive steps of s -> a*s mod 2^128 applied to `value`."""
    return (value * pow(MULTIPLIER, exponent, STATE_MODULUS)) % STATE_MODULUS


def _require_proven_offsets() -> Dict[int, int]:
    unproven = [s for s, off in SEED_OFFSETS.items() if off is None]
    if unproven:
        raise SystemExit(
            f"refusing to generate seed-patched replicas: offset(s) unproven for "
            f"stream(s) {unproven}; run 'generate.py seeds locate' and record them in "
            f"BOOT.md first (see BOOT.md, 'Seed offsets are proven, not assumed')."
        )
    return {s: off for s, off in SEED_OFFSETS.items() if off is not None}


def patched_executable(states: Dict[int, int]) -> bytes:
    """The shipped executable with the given streams' seed blocks overwritten;
    every other byte, including the multiplier and the counters block, unchanged."""
    exe = bytearray(executable_path().read_bytes())
    offsets = _require_proven_offsets()
    for stream, value in states.items():
        offset = offsets[stream]
        exe[offset:offset + 40] = pack_state_block(value)
    return bytes(exe)

# The "NMM  JZZ  KXX    N       NNZ      GSV" header, and the whitespace-separated
# record of six values on the line right after it.
GSV_HEADER = re.compile(
    rb"[ \t]*NMM[ \t]+JZZ[ \t]+KXX[ \t]+N[ \t]+NNZ[ \t]+GSV[ \t]*\r?\n"
    rb"(?P<data>[^\r\n]*)"
)


def sha256_hex(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def now_iso() -> str:
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def git_head() -> str:
    out = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=NODE_ROOT,
        capture_output=True,
        check=True,
        text=True,
    )
    return out.stdout.strip()


_HEADER_FIELDS = ("NMM", "JZZ", "KXX", "N", "NNZ", "GSV")


def replace_header_token(dat_bytes: bytes, index: int, new_value: bytes, expected: Optional[bytes] = None) -> bytes:
    """Replaces the token at `index` (0-based, the record order NMM JZZ KXX N NNZ
    GSV) of the header's data line with `new_value`; every other byte, including
    surrounding whitespace, is unchanged. `expected`, when given, is checked against
    the shipped token first and the call refuses (`ValueError`) on a mismatch, the
    same safety `gsv3_bytes` already had before this function was extracted from it.
    Used by `gsv3_bytes` (index 5, GSV) and by the measurement scripts' own N/KXX
    overrides (`run_original.py`, `run_port.py`: index 2 = KXX, index 3 = N,
    `API.md`, "## Measurement scripts")."""
    m = GSV_HEADER.search(dat_bytes)
    if not m:
        raise ValueError("the 'NMM JZZ KXX N NNZ GSV' header was not found")
    data_line = m.group("data")
    tokens = list(re.finditer(rb"\S+", data_line))
    if len(tokens) != 6:
        raise ValueError(f"expected 6 values in the NMM..GSV record, found {len(tokens)}")
    tok = tokens[index]
    current = data_line[tok.start():tok.end()]
    if expected is not None and current != expected:
        raise ValueError(
            f"the shipped {_HEADER_FIELDS[index]} is {current!r}, expected {expected!r}; "
            "refusing to guess the replacement width"
        )
    start = m.start("data") + tok.start()
    end = m.start("data") + tok.end()
    return dat_bytes[:start] + new_value + dat_bytes[end:]


def gsv3_bytes(dat_bytes: bytes) -> bytes:
    """A GSV=3 copy of a shipped .dat: only the sixth value of the NMM..GSV record
    changes, from '2' to '3', nothing else (BOOT.md, ## Constraints)."""
    return replace_header_token(dat_bytes, 5, b"3", expected=b"2")


def run_case(
    name: str,
    gsv: int,
    workdir: Path,
    executable_bytes: Optional[bytes] = None,
    dat_bytes: Optional[bytes] = None,
    stdin: Optional[bytes] = None,
) -> Tuple[bytes, bytes]:
    """Runs PropStructV3.exe once in a fresh workdir. `executable_bytes`, when given,
    replaces the shipped executable (seed-patched replicas); the input is the shipped
    `.dat`, unchanged for GSV=2/seed-patched runs, GSV-flipped for GSV=3, unless
    `dat_bytes` is given, in which case it replaces the shipped `.dat`'s own bytes as
    the input actually sent (still subject to the same GSV flip when `gsv == 3`) — the
    measurement scripts' own N/KXX header override (`run_original.py`), never used by
    `cmd_reference`/`cmd_replicas` or `seeds locate`, which keep passing `None` and so
    keep the exact behaviour this parameter had before it existed. `stdin`, when given,
    replaces the default `"{name}\\ny\\n"` session (accept the shipped `.dat`, accept
    every default menu parameter) with the caller's own bytes — `run_original.py`'s own
    `--alfa`, which must decline the default-parameters prompt and navigate the menu to
    set item [9] before starting the model, `None` for every other caller, which keeps
    the exact behaviour this parameter had before it existed.
    Returns (input bytes actually run, output results.m bytes)."""
    (workdir / EXE_NAME).write_bytes(executable_bytes if executable_bytes is not None else executable_path().read_bytes())
    shutil.copy2(runtime_path(), workdir / DLL_NAME)

    dat_path = FORMULATIONS / f"{name}.dat"
    original = dat_bytes if dat_bytes is not None else dat_path.read_bytes()
    if gsv == 2:
        sent = original
    elif gsv == 3:
        sent = gsv3_bytes(original)
    else:
        raise ValueError(f"unsupported GSV {gsv}")
    (workdir / f"{name}.dat").write_bytes(sent)

    stdin_bytes = stdin if stdin is not None else f"{name.lower()}\ny\n".encode("ascii")
    proc = subprocess.run(
        [str(workdir / EXE_NAME)],
        cwd=workdir,
        input=stdin_bytes,
        capture_output=True,
        timeout=RUN_TIMEOUT_SECONDS,
    )
    result_path = workdir / "results.m"
    if proc.returncode != 0:
        raise RuntimeError(
            f"{EXE_NAME} exited {proc.returncode} for {name} (GSV={gsv}); "
            f"stderr: {proc.stderr[-2000:]!r}"
        )
    if not result_path.is_file():
        raise RuntimeError(
            f"{EXE_NAME} produced no results.m for {name} (GSV={gsv}); "
            f"stdout tail: {proc.stdout[-2000:]!r}"
        )
    return sent, result_path.read_bytes()


class ProvenanceLog:
    """provenance.json: one entry per generated output, appended and saved after
    every run so a crash mid-batch loses no earlier evidence."""

    def __init__(self) -> None:
        self.entries: List[dict] = (
            json.loads(PROVENANCE_PATH.read_text(encoding="utf-8"))
            if PROVENANCE_PATH.is_file()
            else []
        )
        self._lock = threading.Lock()

    def add(
        self,
        output_rel: str,
        sent: bytes,
        output_bytes: bytes,
        gsv: int,
        name: str,
        extra: Optional[dict] = None,
    ) -> None:
        entry = {
            "output": output_rel,
            "outputSha256": sha256_hex(output_bytes),
            "inputSha256": sha256_hex(sent),
            "executableSha256": sha256_hex(executable_path().read_bytes()),
            "gsv": gsv,
            "stdin": f"{name.lower()}\ny\n",
            "date": now_iso(),
            "scriptCommit": git_head(),
        }
        if extra:
            entry.update(extra)
        with self._lock:
            self.entries = [e for e in self.entries if e["output"] != output_rel]
            self.entries.append(entry)
            self.entries.sort(key=lambda e: e["output"])
            PROVENANCE_PATH.write_text(
                json.dumps(self.entries, indent=2, ensure_ascii=False) + "\n",
                encoding="utf-8",
                newline="\n",
            )


def run_one(
    name: str,
    gsv: int,
    output_path: Path,
    log: ProvenanceLog,
    output_rel: str,
    executable_bytes: Optional[bytes] = None,
    extra: Optional[dict] = None,
) -> None:
    with tempfile.TemporaryDirectory(prefix="propstruct_") as tmp:
        sent, output_bytes = run_case(name, gsv, Path(tmp), executable_bytes)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_bytes(output_bytes)
    log.add(output_rel, sent, output_bytes, gsv, name, extra)


def cmd_reference(names: List[str]) -> None:
    log = ProvenanceLog()
    for name in names:
        output_path = REFERENCES / name / "results.m.txt"
        output_rel = f"references/{name}/results.m.txt"
        run_one(name, 2, output_path, log, output_rel)
        print(f"reference {name}: {output_rel}")


def _states_for_replica(layout: str, k: int) -> Dict[int, int]:
    exponent = replica_jump_exponent(k)
    base_value = original_stream_value if layout == "lagged" else independent_stream_value
    return {stream: advanced(base_value(stream), exponent) for stream in SEED_OFFSETS}


def cmd_print_states(layout: str, ks: List[int]) -> None:
    """`print-states`: the six un-jumped (`base`) states of `layout` and, for every `k`
    in `ks`, the six replica states `_states_for_replica(layout, k)` reaches — printed
    as a JSON object of decimal strings (128-bit values do not fit a JSON number) to
    stdout. Never runs `PropStructV3.exe`, never writes a file: a cross-check entry
    point for `tests/Random.Tests`, which compares this arithmetic against
    `src/Random`'s own (Fixtures/BOOT.md, ## Invariants, 'the states are computed from
    their definitions ... never copied from C# code' — this command lets the C# side
    prove the converse: that its own definitions were copied correctly *from* this
    arithmetic)."""
    base_value = original_stream_value if layout == "lagged" else independent_stream_value
    base = {str(stream): str(base_value(stream)) for stream in SEED_OFFSETS}
    replicas = {
        str(k): {str(stream): str(value) for stream, value in _states_for_replica(layout, k).items()}
        for k in ks
    }
    print(json.dumps({"layout": layout, "base": base, "replicas": replicas}))


def cmd_replicas(name: str, layout: str, count: int) -> None:
    log = ProvenanceLog()
    replicas_dir = REPLICAS_BY_LAYOUT[layout]

    if layout == "gsv3":
        start_lock = threading.Lock()
        last_start = [0.0]

        def staggered_run(k: int) -> None:
            with start_lock:
                wait = last_start[0] + STAGGER_SECONDS - time.monotonic()
                if wait > 0:
                    time.sleep(wait)
                last_start[0] = time.monotonic()
            output_path = replicas_dir / name / f"{k}.m.txt"
            output_rel = f"{replicas_dir.name}/{name}/{k}.m.txt"
            run_one(name, 3, output_path, log, output_rel)
            print(f"replica {name} {k}/{count}: {output_rel}")

        with ThreadPoolExecutor(max_workers=MAX_PARALLEL) as pool:
            list(pool.map(staggered_run, range(1, count + 1)))
        return

    # lagged / independent: seed-patched GSV=2 replicas of the shipped .dat
    # (Fixtures/BOOT.md, ## Invariants, "Seed-patched replicas change nothing but
    # the six seeds"). Distinct by construction (distinct jumps), so no staggering
    # is needed; still capped at MAX_PARALLEL running processes (this node's own
    # BOOT.md, "up to 12 runs in parallel").
    def patched_run(k: int) -> None:
        exponent = replica_jump_exponent(k)
        states = _states_for_replica(layout, k)
        executable_bytes = patched_executable(states)
        output_path = replicas_dir / name / f"{k}.m.txt"
        output_rel = f"{replicas_dir.name}/{name}/{k}.m.txt"
        run_one(
            name,
            2,
            output_path,
            log,
            output_rel,
            executable_bytes=executable_bytes,
            extra={
                "layout": layout,
                "k": k,
                "jump": str(exponent),
                "patchedExecutableSha256": sha256_hex(executable_bytes),
            },
        )
        print(f"replica {name} {k}/{count} ({layout}): {output_rel}")

    with ThreadPoolExecutor(max_workers=MAX_PARALLEL) as pool:
        list(pool.map(patched_run, range(1, count + 1)))


# ---------------------------------------------------------------------------
# `seeds locate`: proves each of the six seed offsets by patching PropStructV3.exe
# and observing HPEPA3.dat/inpt.dat, per the two-part criterion of BOOT.md, "Seed
# offsets are proven, not assumed": patching a block alone must change the output,
# and patching it back to its own original limbs must reproduce the output exactly
# outside the time line and (a pre-existing, seed-independent non-determinism,
# BOOT.md ## Acceptance criteria, "Regeneration reproduces the archive") pdoksmall.
# ---------------------------------------------------------------------------

SEEDS_LOCATE_FORMULATION = "inpt"  # smallest shipped formulation (N=1000): fast

_TIME_LINE = re.compile(r"^.*Calculation time.*$\n?", re.MULTILINE)
_PDOKSMALL_BLOCK = re.compile(r"^\s*pdoksmall\s*=\s*\[.*?\];\s*$\n?", re.MULTILINE | re.DOTALL)


def _normalized_for_comparison(text: str) -> str:
    """`text` with the wall-clock time line and the pdoksmall array (uninitialized
    garbage in pdoksmall(1:2), not seed-dependent, BOOT.md ## Acceptance criteria)
    removed, so what remains reflects only the seeds actually patched."""
    return _PDOKSMALL_BLOCK.sub("", _TIME_LINE.sub("", text))


def _run_inpt(executable_bytes: bytes, tag: str) -> str:
    with tempfile.TemporaryDirectory(prefix=f"propstruct_seeds_{tag}_") as tmp:
        _, output_bytes = run_case(SEEDS_LOCATE_FORMULATION, 2, Path(tmp), executable_bytes)
    return output_bytes.decode("latin1")


def cmd_seeds_locate() -> None:
    shipped = executable_path().read_bytes()
    baseline = _normalized_for_comparison(_run_inpt(shipped, "base"))

    failures: List[str] = []
    for stream in sorted(SEED_OFFSETS):
        offset = SEED_OFFSETS[stream]
        if offset is None:
            failures.append(f"stream {stream}: no candidate offset recorded")
            continue

        original_value = original_stream_value(stream)
        recorded = unpack_state_block(shipped[offset:offset + 40])
        if recorded != original_value:
            failures.append(
                f"stream {stream} at {offset:#x}: holds {recorded}, expected the "
                f"original seed {original_value} (a^{6 - stream} mod 2^128, or the "
                f"transposed limb for stream 3)"
            )
            continue

        identity_exe = patched_executable({stream: original_value})
        identity_text = _normalized_for_comparison(_run_inpt(identity_exe, f"s{stream}_identity"))
        identity_ok = identity_text == baseline

        jumped_value = advanced(original_value, 1)  # one step: s -> a*s mod 2^128
        jump_exe = patched_executable({stream: jumped_value})
        jump_text = _normalized_for_comparison(_run_inpt(jump_exe, f"s{stream}_jump"))
        jump_ok = jump_text != baseline

        status = "OK" if identity_ok and jump_ok else "FAIL"
        print(
            f"stream {stream} @ {offset:#x}: identity-reproduces-baseline={identity_ok}, "
            f"one-step-jump-changes-output={jump_ok} [{status}]"
        )
        if not (identity_ok and jump_ok):
            failures.append(
                f"stream {stream} at {offset:#x}: identity_ok={identity_ok}, jump_ok={jump_ok}"
            )

    if MULTIPLIER_OFFSET in SEED_OFFSETS.values() or COUNTERS_OFFSET in SEED_OFFSETS.values():
        failures.append("the multiplier or counters offset coincides with a seed offset")

    if failures:
        for f in failures:
            print(f"UNPROVEN: {f}", file=sys.stderr)
        raise SystemExit(f"seeds locate: {len(failures)} offset(s) not proven")

    print(
        "seeds locate: all six seed offsets proven on "
        f"{SEEDS_LOCATE_FORMULATION}.dat (identity patch reproduces the baseline, "
        "a one-step jump changes it)."
    )


def archive_data_members(z: zipfile.ZipFile) -> Tuple[List[str], List[str]]:
    """The archive's top-level data members: (formulations `.dat`, outputs `.m`), sorted."""
    top = {n for n in z.namelist() if "/" not in n}
    return sorted(n for n in top if n.lower().endswith(".dat")), sorted(n for n in top if n.lower().endswith(".m"))


def cmd_verify() -> None:
    """Legacy/ against the archive's members, the out-of-tree files against the archive,
    and the three derived fixtures against a regeneration (`derive --check`)."""
    archive = archive_path()
    z = zipfile.ZipFile(archive)
    problems: List[str] = []

    def check(label: str, actual: bytes, expected: bytes) -> None:
        if actual != expected:
            problems.append(f"{label}: {len(actual)} bytes on disk, {len(expected)} bytes in the archive")

    # the transcoded source
    archive_for = z.read("PropStructv3.for")
    expected_for_txt = archive_for.decode("cp1251").replace("\r\n", "\n").encode("utf-8")
    check(FOR_TXT_NAME, source_path().read_bytes(), expected_for_txt)

    # executable and runtime
    check(EXE_NAME, executable_path().read_bytes(), z.read(EXE_NAME))
    check(DLL_NAME, runtime_path().read_bytes(), z.read(DLL_NAME))

    # formulations
    archive_dats, archive_ms = archive_data_members(z)
    on_disk_dats = sorted(p.name for p in FORMULATIONS.glob("*.dat"))
    if [n.lower() for n in archive_dats] != [n.lower() for n in on_disk_dats]:
        problems.append(
            f"formulations/: archive has {archive_dats}, disk has {on_disk_dats}"
        )
    else:
        for n in archive_dats:
            check(f"formulations/{n}", (FORMULATIONS / n).read_bytes(), z.read(n))

    # outputs, ".txt" appended to the archive name (BOOT.md, ## Invariants)
    on_disk_ms = sorted(p.name for p in OUTPUTS.glob("*.m.txt"))
    expected_ms = sorted(n + ".txt" for n in archive_ms)
    if [n.lower() for n in expected_ms] != [n.lower() for n in on_disk_ms]:
        problems.append(f"outputs/: archive implies {expected_ms}, disk has {on_disk_ms}")
    else:
        for n in archive_ms:
            check(f"outputs/{n}.txt", (OUTPUTS / f"{n}.txt").read_bytes(), z.read(n))

    # nothing else under Legacy/
    extra = sorted(
        p.relative_to(LEGACY).as_posix() for p in LEGACY.rglob("*")
        if p.is_file() and p.parent not in (FORMULATIONS, OUTPUTS)
    )
    if extra:
        problems.append(f"Legacy/ holds files that are not the archive's data: {extra}")

    problems.extend(derive_differences(z))

    if problems:
        for p in problems:
            print(f"MISMATCH {p}", file=sys.stderr)
        raise SystemExit(f"verify: {len(problems)} mismatch(es) against {ARCHIVE_NAME}")

    print(
        f"verify: Legacy/ matches {ARCHIVE_NAME} byte for byte "
        f"({len(archive_dats)} formulations, {len(archive_ms)} outputs); the source, the executable "
        f"and its runtime match their members; the three derived fixtures regenerate byte for byte"
    )


def members_text(z: zipfile.ZipFile) -> str:
    """archive-members.sha256: the SHA-256 of every data member of the archive, by its
    member name, `<hex>  <name>`, sorted by name (the name `Legacy/` holds it under is
    the member's, with `.txt` appended to an output's)."""
    dats, ms = archive_data_members(z)
    return "".join(f"{sha256_hex(z.read(name))}  {name}\n" for name in sorted(dats + ms, key=str.lower))


def executable_lines_text() -> str:
    """cases/source/executable_lines.json: the numbers of the source's executable lines,
    a line being executable when it is not blank and does not start with `c` or `C`
    (the rule of the four LineMapCoverageTests, which read this file)."""
    lines = source_path().read_text(encoding="utf-8").split("\n")
    numbers = [n for n, line in enumerate(lines, 1) if line.strip() and line[0] not in "cC"]
    rows = textwrap.wrap(", ".join(str(n) for n in numbers), width=100)
    return (
        "{\n"
        '  "script": "generate.py",\n'
        f'  "source": "{FOR_TXT_NAME}",\n'
        f'  "lineCount": {len(lines)},\n'
        '  "executableLines": [\n'
        + "\n".join("    " + row for row in rows)
        + "\n  ]\n}\n"
    )


def source_limbs_text() -> str:
    """cases/random/source_limbs.json: per line range of SOURCE_LIMB_RANGES, the integers
    written between slashes in those lines (the lines concatenated), in order."""
    lines = source_path().read_text(encoding="utf-8").split("\n")
    entries = []
    for first, last in SOURCE_LIMB_RANGES:
        limbs = [int(m.group(1)) for m in re.finditer(r"/(-?\d+)/", "".join(lines[first - 1:last]))]
        entries.append(f'    "{first}-{last}": [{", ".join(str(limb) for limb in limbs)}]')
    ranges = ", ".join(f"{first}-{last}" for first, last in SOURCE_LIMB_RANGES)
    return (
        "{\n"
        '  "script": "generate.py",\n'
        f'  "fortranLines": "{ranges}",\n'
        '  "limbs": {\n' + ",\n".join(entries) + "\n  }\n}\n"
    )


def derived_files(z: zipfile.ZipFile) -> Dict[Path, str]:
    return {
        MEMBERS_PATH: members_text(z),
        EXECUTABLE_LINES_PATH: executable_lines_text(),
        SOURCE_LIMBS_PATH: source_limbs_text(),
    }


def derive_differences(z: zipfile.ZipFile) -> List[str]:
    """What differs between the committed derived fixtures and their regeneration."""
    differences = []
    for path, text in derived_files(z).items():
        committed = path.read_bytes().decode("utf-8") if path.is_file() else None
        if committed != text:
            differences.append(f"{path.relative_to(NODE_ROOT).as_posix()} does not regenerate byte for byte")
    return differences


def cmd_derive(check_only: bool) -> None:
    """Writes the three fixtures derived from the original (or, with `--check`, compares
    them with a regeneration and writes nothing)."""
    z = zipfile.ZipFile(archive_path())
    if check_only:
        differences = derive_differences(z)
        if differences:
            for difference in differences:
                print(f"MISMATCH {difference}", file=sys.stderr)
            raise SystemExit(f"derive --check: {len(differences)} fixture(s) differ")
        print(f"derive --check: {len(derived_files(z))} fixtures regenerate byte for byte")
        return
    for path, text in derived_files(z).items():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8", newline="\n")
        print(f"derive: wrote {path.relative_to(NODE_ROOT).as_posix()}")


def main(argv: List[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    p_reference = sub.add_parser("reference", help="GSV=2 as shipped")
    p_reference.add_argument("names", nargs="+")

    p_replicas = sub.add_parser("replicas", help="lagged / independent (seed-patched) or GSV=3 replicas")
    p_replicas.add_argument("name")
    p_replicas.add_argument("--layout", choices=sorted(REPLICAS_BY_LAYOUT), required=True)
    p_replicas.add_argument("--count", type=int, required=True)

    p_seeds = sub.add_parser("seeds", help="seed offset proofs")
    seeds_sub = p_seeds.add_subparsers(dest="seeds_command", required=True)
    seeds_sub.add_parser("locate", help="prove the six seed offsets against PropStructV3.exe")

    sub.add_parser("verify", help="Legacy/ against the archive, and the derived fixtures against a regeneration")

    p_derive = sub.add_parser("derive", help="write the fixtures derived from the original")
    p_derive.add_argument("--check", action="store_true", help="compare with a regeneration and write nothing")

    p_print_states = sub.add_parser(
        "print-states",
        help="print seed states as JSON, for cross-checking against src/Random (never runs the executable)",
    )
    p_print_states.add_argument("--layout", choices=("lagged", "independent"), required=True)
    p_print_states.add_argument(
        "--ks", type=int, nargs="*", default=[],
        help="replica numbers k>=1 to include (jumped by replica_jump_exponent(k)); the six un-jumped "
        "base states are always included",
    )

    args = parser.parse_args(argv)

    if args.command == "reference":
        cmd_reference(args.names)
    elif args.command == "replicas":
        cmd_replicas(args.name, args.layout, args.count)
    elif args.command == "seeds" and args.seeds_command == "locate":
        cmd_seeds_locate()
    elif args.command == "verify":
        cmd_verify()
    elif args.command == "derive":
        cmd_derive(args.check)
    elif args.command == "print-states":
        cmd_print_states(args.layout, args.ks)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
