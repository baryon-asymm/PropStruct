#!/usr/bin/env python3
"""The release check of `release.yml`'s `check` job (.github/BOOT.md, "The release check").

Three commands, each reading files so that the check runs offline and is proven on files:

    python -X utf8 .github/scripts/check_release.py notes   [--tag TAG] [--notes FILE]
    python -X utf8 .github/scripts/check_release.py message --tag-message FILE --commit SHA [--tag-type TYPE]
    python -X utf8 .github/scripts/check_release.py run     --run-json FILE --run-id ID --commit SHA

`notes` checks that `CHANGELOG.md` has a non-empty section for the version of
`Directory.Build.props` and writes it to `--notes`; with `--tag` (a push of a tag) it also
checks that the tag is `v<version>` and that the section is dated, not "unreleased". On
every run, a dispatch too, it checks the credits: no placeholder of the owner's
attribution, `<AUTHORS>`, `<AUTHORS, AFFILIATION>` or `<PERMISSION>`, may stand in
`NOTICE`, `README.md` or a file a project of `src/` packs as the package's `README.md`.
`message` (a push only) checks the annotated message of the tag, the two lines
`Rehearsal: <run id>` and `Legacy: <commit sha> <date> <passed>/<total>`, and prints
`rehearsal=<run id>` for the step that fetches that run with `gh run view --json
conclusion,event,workflowName,headSha,status`. `run` checks that run's JSON.

Options common to all: `--tree DIR` (default: the repository this file lies in).

Exit 0 when every check holds, 1 when a check fails (each problem on its own line of
standard error), 2 on a usage error.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path
from typing import Dict, List, Optional, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))

from check_packages import StageFailed, version_prefix  # noqa: E402

TREE = Path(__file__).resolve().parents[2]
WORKFLOW_NAME = "Release"
REHEARSAL_EVENT = "workflow_dispatch"
SECTION_HEADING = re.compile(r"^## \[([^\]]+)\](.*)$")
LINK_DEFINITION = re.compile(r"^\[[^\]]+\]:\s")
REHEARSAL_LINE = re.compile(r"^Rehearsal: (\d+)\s*$")
LEGACY_LINE = re.compile(r"^Legacy: ([0-9a-fA-F]{40}) (\d{4}-\d{2}-\d{2}) (\d+)/(\d+)\s*$")
CREDIT_PLACEHOLDER = re.compile(r"<(?:AUTHORS(?:, AFFILIATION)?|PERMISSION)>")


# ---------------------------------------------------------------------------------------
# The checks: each returns the problems it found, none raises for a bad input
# ---------------------------------------------------------------------------------------

def check_tag(version: str, tag: str) -> List[str]:
    expected = f"v{version}"
    if tag != expected:
        return [f"the tag {tag!r} is not {expected!r}, the version of Directory.Build.props"]
    return []


def changelog_section(changelog: str, version: str) -> Optional[Tuple[str, List[str]]]:
    """The heading remainder and the body lines of the section for `version`, or `None`.

    The body ends at the next `## [` heading or, for the last section, where the link
    definitions at the end of the file begin; blank lines at both ends are dropped."""
    lines = changelog.splitlines()
    start: Optional[int] = None
    remainder = ""
    for index, line in enumerate(lines):
        heading = SECTION_HEADING.match(line)
        if heading is None:
            continue
        if start is not None:
            return remainder, trim(lines[start:index])
        if heading.group(1) == version:
            start, remainder = index + 1, heading.group(2)
    if start is None:
        return None
    end = len(lines)
    while end > start and (not lines[end - 1].strip() or LINK_DEFINITION.match(lines[end - 1])):
        end -= 1
    return remainder, trim(lines[start:end])


def trim(lines: List[str]) -> List[str]:
    first, last = 0, len(lines)
    while first < last and not lines[first].strip():
        first += 1
    while last > first and not lines[last - 1].strip():
        last -= 1
    return lines[first:last]


def check_changelog(changelog: str, version: str, released: bool) -> Tuple[List[str], List[str]]:
    """The problems and the notes lines. `released` is a push of a tag: the section must be dated."""
    found = changelog_section(changelog, version)
    if found is None:
        return [f"CHANGELOG.md has no section '## [{version}]'"], []
    remainder, notes = found
    problems: List[str] = []
    if not notes:
        problems.append(f"the section '## [{version}]' of CHANGELOG.md is empty")
    if released and "unreleased" in remainder.lower():
        problems.append(f"the section '## [{version}]' of CHANGELOG.md is still marked unreleased:{remainder}")
    return problems, notes


def check_message(message: str, commit: str, tag_type: Optional[str]) -> Tuple[List[str], Optional[str]]:
    """The problems of an annotated tag message and the rehearsal run id it names."""
    problems: List[str] = []
    if tag_type is not None and tag_type != "tag":
        problems.append(f"the tag is a {tag_type!r} object, not an annotated tag with a message")
    lines = message.splitlines()
    rehearsals = [match.group(1) for match in map(REHEARSAL_LINE.match, lines) if match]
    legacies = [match for match in map(LEGACY_LINE.match, lines) if match]
    run_id: Optional[str] = None
    if len(rehearsals) != 1:
        problems.append(f"the tag message must carry exactly one line 'Rehearsal: <run id>', found {len(rehearsals)}")
    else:
        run_id = rehearsals[0]
    if len(legacies) != 1:
        problems.append("the tag message must carry exactly one line "
                        f"'Legacy: <commit sha> <date> <passed>/<total>', found {len(legacies)}")
    else:
        sha, _, passed, total = legacies[0].groups()
        if sha.lower() != commit.lower():
            problems.append(f"the Legacy line names {sha.lower()}, the tagged commit is {commit.lower()}")
        if int(total) == 0 or int(passed) != int(total):
            problems.append(f"the Legacy line says {passed}/{total}: every Legacy fact must have run and passed")
    return problems, run_id


def packed_readmes(tree: Path) -> List[Path]:
    """The files the projects of `src/` pack as the package's `README.md`, read from their
    `<None ... PackagePath="README.md">` items, never a typed list."""
    found: List[Path] = []
    for project in sorted((tree / "src").rglob("*.csproj")):
        for item in ElementTree.parse(project).getroot().iter("None"):
            if item.get("PackagePath") == "README.md" and item.get("Include"):
                found.append((project.parent / item.get("Include", "").replace("\\", "/")).resolve())
    return found


def check_credits(tree: Path) -> List[str]:
    """One problem per file and line holding a placeholder of the owner's attribution
    (`<AUTHORS>`, `<AUTHORS, AFFILIATION>`, `<PERMISSION>`), in `NOTICE`, `README.md` and every
    file a project of `src/` packs as `README.md`, so that no placeholder reaches nuget.org."""
    root = tree.resolve()
    problems: List[str] = []
    for path in [root / "NOTICE", root / "README.md", *packed_readmes(tree)]:
        name = path.relative_to(root).as_posix() if path.is_relative_to(root) else str(path)
        if not path.is_file():
            problems.append(f"{name}: not found, and the credits it carries cannot be checked")
            continue
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            placeholders = CREDIT_PLACEHOLDER.findall(line)
            if placeholders:
                problems.append(f"{name}:{number}: the placeholder {', '.join(placeholders)} is still in the "
                                "text; the owner's attribution of the original goes here before a release")
    return problems


def check_run(run: Dict[str, object], run_id: str, commit: str) -> List[str]:
    """The problems of the rehearsal run's `gh run view` JSON."""
    problems: List[str] = []
    expected = {"status": "completed", "conclusion": "success", "event": REHEARSAL_EVENT,
                "workflowName": WORKFLOW_NAME}
    for key, wanted in expected.items():
        if run.get(key) != wanted:
            problems.append(f"the rehearsal run {run_id}: {key} is {run.get(key)!r}, expected {wanted!r}")
    head = run.get("headSha")
    if not isinstance(head, str) or head.lower() != commit.lower():
        problems.append(f"the rehearsal run {run_id}: headSha is {head!r}, the tagged commit is {commit.lower()}")
    return problems


# ---------------------------------------------------------------------------------------
# The commands
# ---------------------------------------------------------------------------------------

def read_text(path: Path, what: str) -> str:
    if not path.is_file():
        raise StageFailed(f"{what} not found: {path}")
    return path.read_text(encoding="utf-8")


def command_notes(options: argparse.Namespace) -> List[str]:
    tree = options.tree.resolve()
    version = version_prefix(tree)
    problems = check_tag(version, options.tag) if options.tag is not None else []
    section_problems, notes = check_changelog(read_text(tree / "CHANGELOG.md", "CHANGELOG.md"), version,
                                              released=options.tag is not None)
    problems += section_problems
    problems += check_credits(tree)
    if not problems and options.notes is not None:
        options.notes.write_text("\n".join(notes) + "\n", encoding="utf-8", newline="\n")
        print(f"release notes of {version}: {len(notes)} lines written to {options.notes}")
    return problems


def command_message(options: argparse.Namespace) -> List[str]:
    problems, run_id = check_message(read_text(options.tag_message, "the tag message"), options.commit,
                                     options.tag_type)
    if not problems and run_id is not None:
        print(f"rehearsal={run_id}")
    return problems


def command_run(options: argparse.Namespace) -> List[str]:
    try:
        run = json.loads(read_text(options.run_json, "the run JSON"))
    except json.JSONDecodeError as error:
        raise StageFailed(f"{options.run_json} is not JSON: {error}") from error
    if not isinstance(run, dict):
        raise StageFailed(f"{options.run_json} does not hold one JSON object")
    problems = check_run(run, options.run_id, options.commit)
    if not problems:
        print(f"the rehearsal run {options.run_id} succeeded as a {REHEARSAL_EVENT} of {WORKFLOW_NAME} on {options.commit}")
    return problems


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description="The release check.")
    root.add_argument("--tree", type=Path, default=TREE)
    commands = root.add_subparsers(dest="command", required=True)
    notes = commands.add_parser("notes")
    notes.add_argument("--tag")
    notes.add_argument("--notes", type=Path)
    message = commands.add_parser("message")
    message.add_argument("--tag-message", type=Path, required=True)
    message.add_argument("--commit", required=True)
    message.add_argument("--tag-type")
    run = commands.add_parser("run")
    run.add_argument("--run-json", type=Path, required=True)
    run.add_argument("--run-id", required=True)
    run.add_argument("--commit", required=True)
    return root


def main(arguments: List[str]) -> int:
    try:
        options = parser().parse_args(arguments)
    except SystemExit as usage:
        return 2 if usage.code else 0
    commands = {"notes": command_notes, "message": command_message, "run": command_run}
    try:
        problems = commands[options.command](options)
    except StageFailed as failure:
        problems = [str(failure)]
    for problem in problems:
        print(problem, file=sys.stderr)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
