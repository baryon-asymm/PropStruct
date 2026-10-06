#!/usr/bin/env python3
"""The original program, PropStructV3, kept outside the repository: where its five files
are, whether they are the recorded ones, and the hashes of everything of it that the
public tree must never hold.

The contract is `API.md`; the rules are `BOOT.md` (identity is a recorded hash, one
reader of the variable, unset or wrong is a failure, the forbidden list is generated).

    python tools/legacy/legacy.py path <name>
    python tools/legacy/legacy.py check
    python tools/legacy/legacy.py forbidden [--check]
    python tools/legacy/legacy.py selftest

Python 3.8+, standard library only. This file holds no byte, line or statement of the
original and no hash of it: the hashes are read from `original.sha256`.
"""

from __future__ import annotations

import argparse
import contextlib
import hashlib
import io
import os
import sys
import tempfile
import zipfile
from pathlib import Path
from typing import Dict, FrozenSet, List, Optional, Sequence, Tuple

ENVIRONMENT_VARIABLE = "PROPSTRUCT_LEGACY_DIR"

NODE_ROOT = Path(__file__).resolve().parent
MANIFEST_PATH = NODE_ROOT / "original.sha256"
FORBIDDEN_PATH = NODE_ROOT / "forbidden.sha256"

MANIFEST_NAMES: Tuple[str, ...] = (
    "PropStructV3.zip",
    "PropStructV3.exe",
    "dforrt.dll",
    "PropStructv3.for.txt",
    "PropStructV3.cycle-plane.listing.txt",
)
ARCHIVE_NAME = MANIFEST_NAMES[0]

# A member of the archive is data when it is a formulation or an output of the original.
DATA_SUFFIXES: Tuple[str, ...] = (".dat", ".m")

EXIT_CLEAN = 0
EXIT_MISMATCH = 1
EXIT_USAGE = 2


class LegacyUnset(Exception):
    """PROPSTRUCT_LEGACY_DIR is not set."""

    def __init__(self) -> None:
        super().__init__(
            f"{ENVIRONMENT_VARIABLE} is not set: it names the directory that holds the "
            f"original's five files ({', '.join(MANIFEST_NAMES)}; tools/legacy/BOOT.md)"
        )


class LegacyMismatch(Exception):
    """A file of the original is not the recorded one."""

    def __init__(self, name: str, recorded: str, actual: str) -> None:
        super().__init__(f"{name}: recorded {recorded}, actual {actual}")
        self.name = name
        self.recorded = recorded
        self.actual = actual


def sha256_of_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_of_file(path: Path) -> str:
    """The SHA-256 of the file's bytes, or `missing` when there is no such file."""
    if not path.is_file():
        return "missing"
    return sha256_of_bytes(path.read_bytes())


def read_hash_lines(path: Path) -> List[Tuple[str, str]]:
    """The (hash, label) pairs of a `<hex sha256>  <label>` file; a blank line is skipped."""
    pairs: List[Tuple[str, str]] = []
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        digest, separator, label = line.partition("  ")
        if not separator or len(digest) != 64 or any(c not in "0123456789abcdef" for c in digest):
            raise ValueError(f"{path}:{number}: not a '<hex sha256>  <label>' line")
        pairs.append((digest, label))
    return pairs


def manifest(path: Path = MANIFEST_PATH) -> Dict[str, str]:
    """name -> recorded SHA-256 hex of the five files, in manifest order."""
    recorded = {label: digest for digest, label in read_hash_lines(path)}
    if tuple(recorded) != MANIFEST_NAMES:
        raise ValueError(f"{path} must hold exactly {', '.join(MANIFEST_NAMES)}, in this order")
    return recorded


def legacy_dir() -> str:
    """The directory PROPSTRUCT_LEGACY_DIR names; raises LegacyUnset. The one read of the variable."""
    directory = os.environ.get(ENVIRONMENT_VARIABLE, "")
    if not directory.strip():
        raise LegacyUnset()
    return directory


def verified_path(directory: Path, name: str, recorded: Dict[str, str]) -> str:
    """The path of `name` in `directory`, after its hash is compared with the record."""
    actual = sha256_of_file(directory / name)
    if actual != recorded[name]:
        raise LegacyMismatch(name, recorded[name], actual)
    return str(directory / name)


_verified: Dict[Tuple[str, str], str] = {}


def legacy_file(name: str) -> str:
    """The verified path of one of the five files, checked once per process.
    Raises LegacyUnset, or LegacyMismatch naming the file and both hashes."""
    if name not in MANIFEST_NAMES:
        raise KeyError(f"{name!r} is not a manifest name: {', '.join(MANIFEST_NAMES)}")
    directory = legacy_dir()
    key = (directory, name)
    if key not in _verified:
        _verified[key] = verified_path(Path(directory), name, manifest())
    return _verified[key]


def forbidden_hashes(path: Path = FORBIDDEN_PATH) -> FrozenSet[str]:
    """The hashes of forbidden.sha256."""
    return frozenset(digest for digest, _ in read_hash_lines(path))


def forbidden_lines(archive: Path, recorded: Dict[str, str]) -> List[str]:
    """The lines of forbidden.sha256: the five files by manifest name, then every member of
    the archive that is not data, by `archive:<member>`; a hash once, the hash of empty
    content never."""
    empty = sha256_of_bytes(b"")
    lines: List[str] = []
    seen = {empty}
    for name, digest in recorded.items():
        if digest not in seen:
            seen.add(digest)
            lines.append(f"{digest}  {name}")
    with zipfile.ZipFile(archive) as members:
        for member in members.infolist():
            if member.is_dir() or member.filename.lower().endswith(DATA_SUFFIXES):
                continue
            digest = sha256_of_bytes(members.read(member))
            if digest not in seen:
                seen.add(digest)
                lines.append(f"{digest}  archive:{member.filename}")
    return lines


def check_lines(directory: Path, recorded: Dict[str, str]) -> Tuple[List[str], bool]:
    """One line per file, `ok <name>` or `mismatch <name> recorded <hash> actual <hash>`,
    and whether every file matched."""
    lines: List[str] = []
    clean = True
    for name, digest in recorded.items():
        actual = sha256_of_file(directory / name)
        if actual == digest:
            lines.append(f"ok {name}")
        else:
            clean = False
            lines.append(f"mismatch {name} recorded {digest} actual {actual}")
    return lines, clean


def command_path(name: str) -> int:
    if name not in MANIFEST_NAMES:
        print(f"{name!r} is not a manifest name: {', '.join(MANIFEST_NAMES)}", file=sys.stderr)
        return EXIT_USAGE
    try:
        print(legacy_file(name))
    except LegacyUnset as unset:
        print(unset, file=sys.stderr)
        return EXIT_USAGE
    except LegacyMismatch as mismatch:
        print(mismatch, file=sys.stderr)
        return EXIT_MISMATCH
    return EXIT_CLEAN


def command_check() -> int:
    try:
        directory = Path(legacy_dir())
    except LegacyUnset as unset:
        print(unset, file=sys.stderr)
        return EXIT_USAGE
    lines, clean = check_lines(directory, manifest())
    print("\n".join(lines))
    return EXIT_CLEAN if clean else EXIT_MISMATCH


def command_forbidden(check_only: bool) -> int:
    try:
        archive = legacy_file(ARCHIVE_NAME)
    except LegacyUnset as unset:
        print(unset, file=sys.stderr)
        return EXIT_USAGE
    except LegacyMismatch as mismatch:
        print(mismatch, file=sys.stderr)
        return EXIT_MISMATCH
    lines = forbidden_lines(Path(archive), manifest())
    text = "\n".join(lines) + "\n"
    if not check_only:
        FORBIDDEN_PATH.write_text(text, encoding="utf-8", newline="\n")
        print(len(lines))
        return EXIT_CLEAN
    committed = FORBIDDEN_PATH.read_text(encoding="utf-8") if FORBIDDEN_PATH.is_file() else ""
    if committed == text:
        return EXIT_CLEAN
    now = {line.partition("  ")[0] for line in lines}
    before = {line.partition("  ")[0] for line in committed.splitlines()}
    print(
        f"forbidden.sha256 differs from the archive: {len(now - before)} hash(es) missing, "
        f"{len(before - now)} not in the archive, {len(lines)} regenerated against "
        f"{len(committed.splitlines())} committed"
    )
    return EXIT_MISMATCH


def synthetic_directory(root: Path, contents: Dict[str, bytes]) -> Dict[str, str]:
    """Writes synthetic files under `root` and returns their manifest."""
    for name, data in contents.items():
        (root / name).write_bytes(data)
    return {name: sha256_of_bytes(data) for name, data in contents.items()}


def selftest_cases() -> List[Tuple[str, bool]]:
    """Each case of the self-test as (what it proves, whether it held). Synthetic files
    only: the self-test needs no original and holds no byte of it."""
    results: List[Tuple[str, bool]] = []
    with tempfile.TemporaryDirectory(prefix="propstruct_legacy_selftest_") as temporary:
        root = Path(temporary)
        contents = {name: f"synthetic content of {name}".encode("ascii") for name in MANIFEST_NAMES}
        recorded = synthetic_directory(root, contents)

        lines, clean = check_lines(root, recorded)
        results.append(("check: every synthetic file matches", clean and lines == [f"ok {n}" for n in recorded]))

        results.append(("path: a matching file resolves", verified_path(root, MANIFEST_NAMES[1], recorded) == str(root / MANIFEST_NAMES[1])))

        flipped = bytearray(contents[MANIFEST_NAMES[2]])
        flipped[0] ^= 1
        (root / MANIFEST_NAMES[2]).write_bytes(bytes(flipped))
        lines, clean = check_lines(root, recorded)
        results.append(("check: one flipped byte is a mismatch naming the file and both hashes",
                        not clean and lines[2] == f"mismatch {MANIFEST_NAMES[2]} recorded {recorded[MANIFEST_NAMES[2]]} actual {sha256_of_bytes(bytes(flipped))}"))
        try:
            verified_path(root, MANIFEST_NAMES[2], recorded)
            raised = False
        except LegacyMismatch as mismatch:
            raised = (mismatch.name, mismatch.recorded, mismatch.actual) == (
                MANIFEST_NAMES[2], recorded[MANIFEST_NAMES[2]], sha256_of_bytes(bytes(flipped)))
        results.append(("path: a flipped byte raises LegacyMismatch with the three fields", raised))

        (root / MANIFEST_NAMES[3]).unlink()
        lines, clean = check_lines(root, recorded)
        results.append(("check: a missing file is a mismatch with actual `missing`", not clean and lines[3].endswith(" actual missing")))

        manifest_file = root / "original.sha256"
        manifest_file.write_text("".join(f"{d}  {n}\n" for n, d in recorded.items()), encoding="utf-8")
        results.append(("manifest: the five names in order are read back", manifest(manifest_file) == recorded))
        manifest_file.write_text("".join(f"{d}  {n}\n" for n, d in list(recorded.items())[:4]), encoding="utf-8")
        try:
            manifest(manifest_file)
            refused = False
        except ValueError:
            refused = True
        results.append(("manifest: four names are refused", refused))

        archive = root / "synthetic.zip"
        with zipfile.ZipFile(archive, "w") as members:
            members.writestr("data.dat", b"formulation")
            members.writestr("output.m", b"output")
            members.writestr("tool.exe", b"program")
            members.writestr("copy.exe", b"program")
            members.writestr("empty.opt", b"")
            members.writestr("notes.opt", b"options")
        listed = forbidden_lines(archive, {"first": sha256_of_bytes(b"first"), "second": sha256_of_bytes(b"")})
        expected = [
            f"{sha256_of_bytes(b'first')}  first",
            f"{sha256_of_bytes(b'program')}  archive:tool.exe",
            f"{sha256_of_bytes(b'options')}  archive:notes.opt",
        ]
        results.append(("forbidden: non-data members, once each, the empty hash and data left out", listed == expected))

    saved = os.environ.pop(ENVIRONMENT_VARIABLE, None)
    try:
        try:
            legacy_dir()
            unset_raised = False
        except LegacyUnset:
            unset_raised = True
        results.append(("legacy_dir: the unset variable raises LegacyUnset", unset_raised))
        with contextlib.redirect_stderr(io.StringIO()):
            results.append(("command check: the unset variable exits 2", command_check() == EXIT_USAGE))
            results.append(("command path: the unset variable exits 2", command_path(MANIFEST_NAMES[1]) == EXIT_USAGE))
            results.append(("command path: a name outside the manifest exits 2", command_path("another.exe") == EXIT_USAGE))
    finally:
        if saved is not None:
            os.environ[ENVIRONMENT_VARIABLE] = saved
    return results


def command_selftest() -> int:
    failed = 0
    for what, held in selftest_cases():
        print(f"{'ok' if held else 'FAIL'} {what}")
        failed += 0 if held else 1
    return EXIT_CLEAN if failed == 0 else EXIT_MISMATCH


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    path_parser = commands.add_parser("path", help="the verified path of one of the five files")
    path_parser.add_argument("name")
    commands.add_parser("check", help="every one of the five files against original.sha256")
    forbidden_parser = commands.add_parser("forbidden", help="write forbidden.sha256 from the archive")
    forbidden_parser.add_argument("--check", action="store_true", help="compare only")
    commands.add_parser("selftest", help="the reader and the mismatch path on synthetic files")
    try:
        arguments = parser.parse_args(argv)
    except SystemExit as error:
        return EXIT_CLEAN if error.code == 0 else EXIT_USAGE
    if arguments.command == "path":
        return command_path(arguments.name)
    if arguments.command == "check":
        return command_check()
    if arguments.command == "forbidden":
        return command_forbidden(arguments.check)
    return command_selftest()


if __name__ == "__main__":
    sys.exit(main())
