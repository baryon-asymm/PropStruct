#!/usr/bin/env python3
"""Proof that a tree holds nothing of the original program beyond the quotation rule.

    python tools/legacy/scan.py <tree> [--hashes]

The contract is `API.md`; the rules are `BOOT.md` (the scan reports where, never what;
the quotation rule; what the scan reads). A finding names a file, a line or an offset and,
for a statement, the number of the source line it matches; it never prints the matched
text or bytes, since a failing log may be public.

Checks, in order: the SHA-256 of every file against `forbidden.sha256`; the file's name
against the extensions of the original's build products; then, unless `--hashes`, 16-byte
windows of the code sections of the executable and its runtime in every file, raw, in the
byte columns of a disassembly listing and in hexadecimal runs; and the 25-character
fragments of the source's statements in every text file and, for a statement too short for
a fragment, the whole statement in the quoted spans of every text file, a file touching
more than eight distinct statements being a finding.

Python 3.8+, standard library only. This file holds no byte, line or statement of the
original.
"""

from __future__ import annotations

import argparse
import bisect
import collections
import hashlib
import os
import re
import struct
import sys
from pathlib import Path
from typing import Dict, FrozenSet, Iterable, Iterator, List, NamedTuple, Optional, Sequence, Set, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))

import legacy  # noqa: E402

EXIT_CLEAN = 0
EXIT_FINDINGS = 1
EXIT_USAGE = 2

# Never exported and never the tree's own: version control, build output, Python's bytecode, and the
# agent sessions' worktrees (`.claude/worktrees`, each a full copy of another tree, ignored by git).
SKIPPED_DIRECTORIES = frozenset((".git", "bin", "obj", "__pycache__"))
SKIPPED_BELOW = {(".claude", "worktrees")}
BUILD_PRODUCT_NAME = re.compile(r"\.(exe|dll|zip|obj|pdb|for|dsp|dsw|opt|plg)$|\.for\.", re.IGNORECASE)

WINDOW_BYTES = 16
PADDING_DISTINCT_VALUES = 3  # a window of at most this many distinct values is padding
FRAGMENT_CHARACTERS = 25
STATEMENT_LIMIT = 8  # one file may quote at most this many distinct statements
SHORT_STATEMENT_FLOOR = 10  # a statement shorter than the fragment is counted whole from this length on
IMAGE_SCN_CNT_CODE = 0x20

DATA_DIRECTORY = "tests/Fixtures/Legacy"  # the original's data, not scanned for statements
OUTPUT_SUFFIX = ".m.txt"  # the original's outputs and every output derived from them

COLUMN_FIRST = re.compile(rb"\s*[0-9A-Fa-f]{8}:\s+((?:[0-9A-Fa-f]{2} )+)")
COLUMN_CONTINUATION = re.compile(rb"\s+((?:[0-9A-Fa-f]{2} )+)\s*$")
HEX_RUN = re.compile(rb"[0-9A-Fa-f]{32,}|(?:[0-9A-Fa-f]{2} ){16,}")
QUOTED = re.compile(r"'[^']*'|\"[^\"]*\"")
NOT_CODE = re.compile(r"\s*(data|format|write|print)\b", re.IGNORECASE)
BACKTICK_SPAN = re.compile(r"``([^`]+)``|`([^`]+)`")
XML_CODE_SPAN = re.compile(r"<c>(.*?)</c>")
CODE_COMMENT_LINE = re.compile(r"\s*(?://+|#)(.*)")
MARKDOWN_FENCE = re.compile(r"\s*(```|~~~)")


class Finding(NamedTuple):
    kind: str  # hash, name, bytes or statement
    file: str  # relative to the scanned tree, forward slashes
    line: Optional[int] = None
    offset: Optional[int] = None
    source_line: Optional[int] = None

    def render(self) -> str:
        where = self.file if self.line is None else f"{self.file}:{self.line}"
        if self.offset is not None:
            where += f" offset {self.offset}"
        if self.source_line is not None:
            where += f" source line {self.source_line}"
        return f"{self.kind} {where}"


class Reference(NamedTuple):
    """What the full scan searches for: the code windows and the statement fragments."""

    windows: FrozenSet[bytes]
    fragments: Dict[str, FrozenSet[int]]  # 25-character fragment -> first lines of its statements
    statement_count: int
    short_statements: Dict[str, int] = {}  # a whole statement shorter than a fragment -> its first line


class ScanResult(NamedTuple):
    files: int
    findings: List[Finding]
    most_statements: Tuple[int, str]  # the most distinct statements one file touches, and the file


def normalise(text: str) -> str:
    return re.sub(r"\s+", "", text).lower()


def source_statements(text: str) -> List[Tuple[int, str]]:
    """The statements of fixed or tab form Fortran source as (first line, text): comment
    lines left out, a continuation line joined to its statement, a statement label left
    out of the text."""
    statements: List[Tuple[int, str]] = []
    for number, line in enumerate(text.splitlines(), 1):
        if not line.strip() or line[0] in "cC*!":
            continue
        tab = line.find("\t")
        if 0 <= tab < 6:
            body = line[tab + 1:]
            continuation = tab == 0 and body[:1] in tuple("123456789")
            body = body[1:] if continuation else body
        else:
            body = line[6:]
            continuation = len(line) > 5 and line[:5].strip() == "" and line[5] not in " 0"
        if continuation and statements:
            statements[-1] = (statements[-1][0], statements[-1][1] + body)
        else:
            statements.append((number, body))
    return statements


def statement_fragments(statements: Iterable[Tuple[int, str]]) -> Dict[str, FrozenSet[int]]:
    """The 25-character fragments of the statements that are code: data, format, write and
    print statements contribute none, and neither does text between quotes."""
    found: Dict[str, Set[int]] = collections.defaultdict(set)
    for first_line, text in statements:
        if NOT_CODE.match(text):
            continue
        for piece in QUOTED.split(text):
            squeezed = normalise(piece)
            for start in range(0, max(0, len(squeezed) - FRAGMENT_CHARACTERS + 1)):
                found[squeezed[start:start + FRAGMENT_CHARACTERS]].add(first_line)
    return {fragment: frozenset(lines) for fragment, lines in found.items()}


def short_statement_texts(statements: Iterable[Tuple[int, str]]) -> Dict[str, int]:
    """The code statements too short to give a fragment, whole and normalised, at least
    `SHORT_STATEMENT_FLOOR` characters long, as text -> first line of the first statement
    that reads so. Statements with quoted text are output format, not counted."""
    found: Dict[str, int] = {}
    for first_line, text in statements:
        if NOT_CODE.match(text) or QUOTED.search(text):
            continue
        squeezed = normalise(text)
        if SHORT_STATEMENT_FLOOR <= len(squeezed) < FRAGMENT_CHARACTERS:
            found.setdefault(squeezed, first_line)
    return found


def quoted_spans(relative: str, text: str) -> Iterator[Tuple[int, str]]:
    """The spans of a text file that quote something as code, as (line, span): a backtick
    span, a `<c>` element, the body of a comment line of a file that is not Markdown, and
    every line of a fenced block of a Markdown file."""
    markdown = relative.endswith(".md")
    fenced = False
    for number, line in enumerate(text.splitlines(), 1):
        if markdown and MARKDOWN_FENCE.match(line):
            fenced = not fenced
            continue
        if fenced:
            yield number, line
            continue
        for match in BACKTICK_SPAN.finditer(line):
            yield number, match.group(1) or match.group(2)
        for match in XML_CODE_SPAN.finditer(line):
            yield number, match.group(1)
        comment = None if markdown else CODE_COMMENT_LINE.match(line)
        if comment:
            yield number, comment.group(1)


def code_section(image: bytes) -> bytes:
    """The bytes of the sections of a PE image that hold code."""
    pe_offset = struct.unpack_from("<I", image, 0x3C)[0]
    section_count = struct.unpack_from("<H", image, pe_offset + 6)[0]
    optional_size = struct.unpack_from("<H", image, pe_offset + 20)[0]
    code = b""
    for index in range(section_count):
        header = pe_offset + 24 + optional_size + 40 * index
        size, pointer = struct.unpack_from("<II", image, header + 16)
        characteristics = struct.unpack_from("<I", image, header + 36)[0]
        if characteristics & IMAGE_SCN_CNT_CODE:
            code += image[pointer:pointer + size]
    return code


def code_windows(images: Iterable[bytes]) -> FrozenSet[bytes]:
    """Every 16-byte window of the code sections of the images that is not padding."""
    windows: Set[bytes] = set()
    for image in images:
        code = code_section(image)
        for start in range(0, len(code) - WINDOW_BYTES + 1):
            window = code[start:start + WINDOW_BYTES]
            if len(set(window)) > PADDING_DISTINCT_VALUES:
                windows.add(window)
    return frozenset(windows)


def first_window_offset(data: bytes, windows: FrozenSet[bytes]) -> Optional[int]:
    for start in range(0, len(data) - WINDOW_BYTES + 1):
        if data[start:start + WINDOW_BYTES] in windows:
            return start
    return None


def decoded_chunks(data: bytes) -> List[bytes]:
    """The bytes a text file may carry as hexadecimal: the byte columns of a disassembly
    listing joined, and every long run of hexadecimal digits."""
    columns: List[bytes] = []
    for line in data.splitlines():
        match = COLUMN_FIRST.match(line) or COLUMN_CONTINUATION.match(line)
        if match:
            columns.append(match.group(1))
    chunks: List[bytes] = []
    if columns:
        chunks.append(bytes.fromhex(re.sub(rb"\s", b"", b"".join(columns)).decode()))
    for run in HEX_RUN.finditer(data):
        digits = re.sub(rb"\s", b"", run.group(0)).decode()
        chunks.append(bytes.fromhex(digits[: len(digits) // 2 * 2]))
    return chunks


def byte_finding(relative: str, data: bytes, windows: FrozenSet[bytes]) -> Optional[Finding]:
    offset = first_window_offset(data, windows)
    if offset is not None:
        return Finding("bytes", relative, offset=offset)
    for chunk in decoded_chunks(data):
        offset = first_window_offset(chunk, windows)
        if offset is not None:
            return Finding("bytes", relative, offset=offset)
    return None


def statement_findings(relative: str, text: str, reference: Reference) -> Tuple[int, List[Finding]]:
    """How many distinct statements of the source the text touches and, when that is more
    than the limit, one finding per statement at the first line of the text that touches it.
    A statement is touched by one of its fragments anywhere in the text, or, when it is too
    short for a fragment, by a quoted span that is that whole statement."""
    squeezed: List[str] = []
    line_starts: List[int] = []
    length = 0
    for line in text.splitlines():
        line_starts.append(length)
        piece = normalise(line)
        squeezed.append(piece)
        length += len(piece)
    flat = "".join(squeezed)
    touched: Dict[int, int] = {}  # source line of a statement -> first line of the text that touches it
    for start in range(0, max(0, len(flat) - FRAGMENT_CHARACTERS + 1)):
        lines = reference.fragments.get(flat[start:start + FRAGMENT_CHARACTERS])
        if lines:
            number = bisect.bisect_right(line_starts, start)
            for source_line in lines:
                touched[source_line] = min(number, touched.get(source_line, number))
    if reference.short_statements:
        for number, span in quoted_spans(relative, text):
            source_line = reference.short_statements.get(normalise(span))
            if source_line is not None:
                touched[source_line] = min(number, touched.get(source_line, number))
    if len(touched) <= STATEMENT_LIMIT:
        return len(touched), []
    findings = [
        Finding("statement", relative, line=number, source_line=source_line)
        for source_line, number in sorted(touched.items(), key=lambda item: (item[1], item[0]))
    ]
    return len(touched), findings


def tree_files(tree: Path) -> List[Path]:
    files: List[Path] = []
    for directory, subdirectories, names in os.walk(tree):
        parent = Path(directory).name
        subdirectories[:] = sorted(
            d for d in subdirectories if d not in SKIPPED_DIRECTORIES and (parent, d) not in SKIPPED_BELOW)
        files.extend(Path(directory) / name for name in sorted(names))
    return files


def is_data_of_the_original(relative: str) -> bool:
    return relative.startswith(DATA_DIRECTORY + "/") or relative.endswith(OUTPUT_SUFFIX)


def scan_tree(tree: Path, forbidden: FrozenSet[str], reference: Optional[Reference]) -> ScanResult:
    """The findings of a tree; `reference` is None for the hash and name checks alone."""
    empty = hashlib.sha256(b"").hexdigest()
    findings: List[Finding] = []
    most: Tuple[int, str] = (0, "")
    files = tree_files(tree)
    for path in files:
        relative = path.relative_to(tree).as_posix()
        data = path.read_bytes()
        digest = hashlib.sha256(data).hexdigest()
        if digest in forbidden and digest != empty:
            findings.append(Finding("hash", relative))
        if BUILD_PRODUCT_NAME.search(path.name):
            findings.append(Finding("name", relative))
        if reference is None:
            continue
        found = byte_finding(relative, data, reference.windows)
        if found is not None:
            findings.append(found)
        if is_data_of_the_original(relative):
            continue
        try:
            text = data.decode("utf-8")
        except UnicodeDecodeError:
            continue
        count, statement_found = statement_findings(relative, text, reference)
        findings.extend(statement_found)
        if count > most[0]:
            most = (count, relative)
    return ScanResult(len(files), findings, most)


def build_reference(images: Iterable[bytes], source: str) -> Reference:
    """What the full scan searches for, from the PE images and the source text."""
    statements = source_statements(source)
    return Reference(code_windows(images), statement_fragments(statements), len(statements),
                     short_statement_texts(statements))


def reference_from_the_original() -> Reference:
    """The reference of the original in PROPSTRUCT_LEGACY_DIR, each file verified against
    the manifest by `legacy_file`."""
    images = [Path(legacy.legacy_file(name)).read_bytes() for name in ("PropStructV3.exe", "dforrt.dll")]
    source = Path(legacy.legacy_file("PropStructv3.for.txt")).read_text(encoding="utf-8")
    return build_reference(images, source)


def summary(result: ScanResult, hashes_only: bool) -> List[str]:
    count = collections.Counter(finding.kind for finding in result.findings)
    unread = "-" if hashes_only else None
    lines = [
        f"scan: {result.files} files; forbidden hashes {count['hash']}, names {count['name']}, "
        f"code windows {unread or count['bytes']}, statements {unread or count['statement']}"
    ]
    if not hashes_only:
        lines.append(f"most statements in one file: {result.most_statements[0]} ({result.most_statements[1] or 'none'})")
    return lines


def reference_line(reference: Reference) -> str:
    """What the full scan searched for, counted."""
    return (f"reference: {reference.statement_count} statements, {len(reference.fragments)} fragments, "
            f"{len(reference.short_statements)} short statements, {len(reference.windows)} code windows")


def command(tree: Path, hashes_only: bool) -> int:
    if not tree.is_dir():
        print(f"{tree} is not a directory", file=sys.stderr)
        return EXIT_USAGE
    try:
        forbidden = legacy.forbidden_hashes()
        reference = None if hashes_only else reference_from_the_original()
    except legacy.LegacyUnset as unset:
        print(unset, file=sys.stderr)
        return EXIT_USAGE
    except legacy.LegacyMismatch as mismatch:
        print(mismatch, file=sys.stderr)
        return EXIT_USAGE
    result = scan_tree(tree, forbidden, reference)
    for finding in result.findings:
        print(finding.render())
    for line in summary(result, hashes_only):
        print(line)
    if reference is not None:
        print(reference_line(reference))
    return EXIT_FINDINGS if result.findings else EXIT_CLEAN


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("tree", type=Path, help="the directory to scan, normally the export of the public tree")
    parser.add_argument("--hashes", action="store_true", help="the hash and name checks only; needs no original")
    try:
        arguments = parser.parse_args(argv)
    except SystemExit as error:
        return EXIT_CLEAN if error.code == 0 else EXIT_USAGE
    return command(arguments.tree, arguments.hashes)


if __name__ == "__main__":
    sys.exit(main())
