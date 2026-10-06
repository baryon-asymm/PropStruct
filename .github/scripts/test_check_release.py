#!/usr/bin/env python3
"""Proof that check_release.py is non-degenerate: red on each way a release can be wrong,
right on the files of a good release, on synthetic files and on the tree's own CHANGELOG.md.

Needs no dotnet, no git and no network.

    python -X utf8 .github/scripts/test_check_release.py
"""

from __future__ import annotations

import contextlib
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Dict, List, Optional, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))

import check_release as release  # noqa: E402

VERSION = "1.2.3"
COMMIT = "a" * 40
OTHER_COMMIT = "b" * 40
CHANGELOG = f"""# Changelog

## [Unreleased]

Nothing yet.

## [{VERSION}] - 2026-11-01

The first release of the test.

### Added
- One thing.

## [1.0.0] - 2026-01-01

Older.

[Unreleased]: https://example.invalid/compare/v{VERSION}...HEAD
"""
GOOD_MESSAGE = f"""PropStruct {VERSION}

Rehearsal: 123456789
Legacy: {COMMIT} 2026-10-30 412/412
"""
GOOD_RUN: Dict[str, object] = {"status": "completed", "conclusion": "success", "event": "workflow_dispatch",
                               "workflowName": "Release", "headSha": COMMIT}


CLEAN_NOTICE = "PropStruct is a port of PropStructV3 by A. Author, Some Institute.\n"
CLEAN_README = "# PropStruct\n\nA port, credited in NOTICE.\n"


def add_packed_project(tree: Path, name: str, readme: str) -> Path:
    """A project of `src/` that packs `docs/<name>.md` as the package's `README.md`, and the file."""
    project = tree / "src" / name
    project.mkdir(parents=True, exist_ok=True)
    (project / f"{name}.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup>'
        f'<None Include="..\\..\\docs\\{name}.md" Pack="true" PackagePath="README.md" />'
        '<None Include="..\\..\\docs\\unpacked.md" Pack="true" PackagePath="docs\\" />'
        "</ItemGroup></Project>", encoding="utf-8")
    (tree / "docs").mkdir(exist_ok=True)
    (tree / "docs" / f"{name}.md").write_text(readme, encoding="utf-8")
    return tree / "docs" / f"{name}.md"


def make_tree(directory: Path, changelog: str = CHANGELOG, version: str = VERSION) -> Path:
    """A tree of a good release: a version, its CHANGELOG section and credits without placeholders."""
    (directory / "Directory.Build.props").write_text(
        f"<Project><PropertyGroup><VersionPrefix>{version}</VersionPrefix></PropertyGroup></Project>",
        encoding="utf-8")
    (directory / "CHANGELOG.md").write_text(changelog, encoding="utf-8")
    (directory / "NOTICE").write_text(CLEAN_NOTICE, encoding="utf-8")
    (directory / "README.md").write_text(CLEAN_README, encoding="utf-8")
    add_packed_project(directory, "Library", CLEAN_README)
    return directory


def run_main(arguments: List[str]) -> Tuple[int, str, str]:
    out, err = io.StringIO(), io.StringIO()
    with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
        code = release.main(arguments)
    return code, out.getvalue(), err.getvalue()


class TagTest(unittest.TestCase):
    def test_the_tag_of_the_version_is_right(self) -> None:
        self.assertEqual(release.check_tag(VERSION, f"v{VERSION}"), [])

    def test_a_tag_that_differs_from_the_version_is_red(self) -> None:
        for tag in ("v1.2.4", "1.2.3", "v1.2.3-rc1", ""):
            with self.subTest(tag=tag):
                problems = release.check_tag(VERSION, tag)
                self.assertEqual(len(problems), 1)
                self.assertIn("is not 'v1.2.3'", problems[0])


class ChangelogTest(unittest.TestCase):
    def test_the_notes_are_the_section_alone(self) -> None:
        problems, notes = release.check_changelog(CHANGELOG, VERSION, released=True)
        self.assertEqual(problems, [])
        self.assertEqual(notes, ["The first release of the test.", "", "### Added", "- One thing."])

    def test_the_last_section_stops_before_the_link_definitions(self) -> None:
        problems, notes = release.check_changelog(CHANGELOG, "1.0.0", released=True)
        self.assertEqual(problems, [])
        self.assertEqual(notes, ["Older."])

    def test_a_missing_section_is_red(self) -> None:
        problems, notes = release.check_changelog(CHANGELOG, "9.9.9", released=False)
        self.assertEqual(problems, ["CHANGELOG.md has no section '## [9.9.9]'"])
        self.assertEqual(notes, [])

    def test_an_empty_section_is_red(self) -> None:
        text = CHANGELOG.replace("The first release of the test.\n\n### Added\n- One thing.\n", "")
        problems, _ = release.check_changelog(text, VERSION, released=False)
        self.assertEqual(problems, [f"the section '## [{VERSION}]' of CHANGELOG.md is empty"])

    def test_an_unreleased_section_is_red_on_a_tag_and_right_on_a_dispatch(self) -> None:
        text = CHANGELOG.replace("2026-11-01", "unreleased")
        on_tag, _ = release.check_changelog(text, VERSION, released=True)
        on_dispatch, _ = release.check_changelog(text, VERSION, released=False)
        self.assertEqual(len(on_tag), 1)
        self.assertIn("still marked unreleased", on_tag[0])
        self.assertEqual(on_dispatch, [])


class MessageTest(unittest.TestCase):
    def check(self, message: str, commit: str = COMMIT, tag_type: Optional[str] = "tag") -> Tuple[List[str], Optional[str]]:
        return release.check_message(message, commit, tag_type)

    def test_a_good_message_names_the_rehearsal_run(self) -> None:
        self.assertEqual(self.check(GOOD_MESSAGE), ([], "123456789"))

    def test_a_message_without_the_rehearsal_line_is_red(self) -> None:
        problems, run_id = self.check(GOOD_MESSAGE.replace("Rehearsal: 123456789\n", ""))
        self.assertEqual(len(problems), 1)
        self.assertIn("'Rehearsal: <run id>', found 0", problems[0])
        self.assertIsNone(run_id)

    def test_a_rehearsal_line_without_a_number_is_red(self) -> None:
        problems, _ = self.check(GOOD_MESSAGE.replace("123456789", "latest"))
        self.assertIn("found 0", problems[0])

    def test_two_rehearsal_lines_are_red(self) -> None:
        problems, _ = self.check(GOOD_MESSAGE + "Rehearsal: 5\n")
        self.assertIn("found 2", problems[0])

    def test_a_message_without_the_legacy_line_is_red(self) -> None:
        problems, _ = self.check(GOOD_MESSAGE.replace(f"Legacy: {COMMIT} 2026-10-30 412/412\n", ""))
        self.assertEqual(len(problems), 1)
        self.assertIn("'Legacy: <commit sha> <date> <passed>/<total>', found 0", problems[0])

    def test_a_legacy_line_for_another_commit_is_red(self) -> None:
        problems, _ = self.check(GOOD_MESSAGE, commit=OTHER_COMMIT)
        self.assertEqual(len(problems), 1)
        self.assertIn(f"the tagged commit is {OTHER_COMMIT}", problems[0])

    def test_a_legacy_line_with_a_failing_or_empty_count_is_red(self) -> None:
        for counts in ("411/412", "0/0"):
            with self.subTest(counts=counts):
                problems, _ = self.check(GOOD_MESSAGE.replace("412/412", counts))
                self.assertEqual(len(problems), 1)
                self.assertIn("must have run and passed", problems[0])

    def test_a_lightweight_tag_is_red(self) -> None:
        problems, _ = self.check(GOOD_MESSAGE, tag_type="commit")
        self.assertEqual(len(problems), 1)
        self.assertIn("not an annotated tag", problems[0])

    def test_an_empty_message_is_red_twice(self) -> None:
        problems, run_id = self.check("")
        self.assertEqual(len(problems), 2)
        self.assertIsNone(run_id)


class RunTest(unittest.TestCase):
    def test_the_rehearsal_dispatch_that_succeeded_on_the_tagged_commit_is_right(self) -> None:
        self.assertEqual(release.check_run(GOOD_RUN, "7", COMMIT), [])

    def test_each_wrong_field_is_red_with_its_name(self) -> None:
        wrong = {"conclusion": "failure", "event": "push", "workflowName": "CI", "status": "in_progress"}
        for key, value in wrong.items():
            with self.subTest(key=key):
                problems = release.check_run({**GOOD_RUN, key: value}, "7", COMMIT)
                self.assertEqual(len(problems), 1)
                self.assertIn(f"{key} is {value!r}", problems[0])

    def test_a_head_commit_that_is_not_the_tagged_one_is_red(self) -> None:
        problems = release.check_run({**GOOD_RUN, "headSha": OTHER_COMMIT}, "7", COMMIT)
        self.assertEqual(len(problems), 1)
        self.assertIn("headSha", problems[0])

    def test_a_run_without_the_fields_is_red(self) -> None:
        self.assertEqual(len(release.check_run({}, "7", COMMIT)), 5)


class CreditsTest(unittest.TestCase):
    """No placeholder of the owner's attribution reaches a package: red on each placeholder in each
    file that ships, one problem per file and line, right on a tree without them."""

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="propstruct_release_test_")
        self.addCleanup(self.temporary.cleanup)
        self.tree = make_tree(Path(self.temporary.name))

    def test_a_tree_without_placeholders_is_right(self) -> None:
        self.assertEqual(release.check_credits(self.tree), [])

    def test_each_placeholder_in_notice_is_red_naming_the_file_and_the_line(self) -> None:
        for placeholder in ("<AUTHORS, AFFILIATION>", "<AUTHORS>", "<PERMISSION>"):
            with self.subTest(placeholder=placeholder):
                (self.tree / "NOTICE").write_text(f"Credit.\nA port by {placeholder}.\n", encoding="utf-8")
                problems = release.check_credits(self.tree)
                self.assertEqual(len(problems), 1)
                self.assertTrue(problems[0].startswith("NOTICE:2: "), problems[0])
                self.assertIn(placeholder, problems[0])

    def test_a_placeholder_in_the_repository_readme_is_red(self) -> None:
        (self.tree / "README.md").write_text("# PropStruct\n\nBy <AUTHORS>.\n", encoding="utf-8")
        problems = release.check_credits(self.tree)
        self.assertEqual([problem.split(" ")[0] for problem in problems], ["README.md:3:"])

    def test_a_placeholder_in_a_packed_readme_is_red_in_every_project_that_packs_one(self) -> None:
        second = add_packed_project(self.tree, "Tool", CLEAN_README)
        (self.tree / "docs" / "Library.md").write_text("By <AUTHORS>.\n", encoding="utf-8")
        second.write_text("Clean.\nPermission: <PERMISSION>\n", encoding="utf-8")
        problems = release.check_credits(self.tree)
        self.assertEqual(sorted(problem.split(" ")[0] for problem in problems),
                         ["docs/Library.md:1:", "docs/Tool.md:2:"])

    def test_a_placeholder_in_a_file_that_ships_nowhere_is_not_a_finding(self) -> None:
        (self.tree / "docs" / "unpacked.md").write_text("By <AUTHORS>.\n", encoding="utf-8")
        (self.tree / "docs" / "notes.md").write_text("By <AUTHORS>.\n", encoding="utf-8")
        self.assertEqual(release.check_credits(self.tree), [])

    def test_one_problem_per_file_and_line(self) -> None:
        (self.tree / "NOTICE").write_text("by <AUTHORS, AFFILIATION> and <PERMISSION>\nclean\nby <AUTHORS>\n",
                                          encoding="utf-8")
        problems = release.check_credits(self.tree)
        self.assertEqual([problem.split(" ")[0] for problem in problems], ["NOTICE:1:", "NOTICE:3:"])
        self.assertIn("<PERMISSION>", problems[0])

    def test_a_missing_notice_or_packed_readme_is_red_not_skipped(self) -> None:
        (self.tree / "NOTICE").unlink()
        (self.tree / "docs" / "Library.md").unlink()
        problems = release.check_credits(self.tree)
        self.assertEqual(sorted(problem.split(":")[0] for problem in problems), ["NOTICE", "docs/Library.md"])
        self.assertTrue(all("not found" in problem for problem in problems))

    def test_the_notes_command_is_red_on_a_placeholder_on_a_dispatch_and_writes_nothing(self) -> None:
        (self.tree / "README.md").write_text("By <AUTHORS>.\n", encoding="utf-8")
        notes = Path(self.temporary.name) / "notes.md"
        code, _, err = run_main(["--tree", str(self.tree), "notes", "--notes", str(notes)])
        self.assertEqual(code, 1)
        self.assertIn("README.md:1:", err)
        self.assertFalse(notes.exists())

    def test_the_notes_command_is_right_without_placeholders(self) -> None:
        code, _, err = run_main(["--tree", str(self.tree), "notes", "--tag", f"v{VERSION}"])
        self.assertEqual((code, err), (0, ""))


class CommandLineTest(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="propstruct_release_test_")
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)

    def test_notes_on_a_dispatch_write_the_section_and_exit_zero(self) -> None:
        tree = make_tree(self.directory)
        notes = self.directory / "notes.md"
        code, out, _ = run_main(["--tree", str(tree), "notes", "--notes", str(notes)])
        self.assertEqual(code, 0, out)
        self.assertEqual(notes.read_text(encoding="utf-8"),
                         "The first release of the test.\n\n### Added\n- One thing.\n")

    def test_notes_on_a_good_tag_exit_zero(self) -> None:
        tree = make_tree(self.directory)
        self.assertEqual(run_main(["--tree", str(tree), "notes", "--tag", f"v{VERSION}"])[0], 0)

    def test_notes_on_a_wrong_tag_exit_one_naming_it(self) -> None:
        tree = make_tree(self.directory)
        code, _, err = run_main(["--tree", str(tree), "notes", "--tag", "v1.2.4"])
        self.assertEqual(code, 1)
        self.assertIn("'v1.2.4'", err)

    def test_notes_without_the_changelog_section_exit_one_and_write_nothing(self) -> None:
        tree = make_tree(self.directory, version="9.9.9")
        notes = self.directory / "notes.md"
        code, _, err = run_main(["--tree", str(tree), "notes", "--notes", str(notes)])
        self.assertEqual(code, 1)
        self.assertIn("no section '## [9.9.9]'", err)
        self.assertFalse(notes.exists())

    def test_notes_without_a_changelog_file_exit_one(self) -> None:
        tree = make_tree(self.directory)
        (tree / "CHANGELOG.md").unlink()
        code, _, err = run_main(["--tree", str(tree), "notes"])
        self.assertEqual(code, 1)
        self.assertIn("CHANGELOG.md not found", err)

    def test_message_prints_the_run_id_for_the_next_step(self) -> None:
        message = self.directory / "message.txt"
        message.write_text(GOOD_MESSAGE, encoding="utf-8")
        code, out, _ = run_main(["message", "--tag-message", str(message), "--commit", COMMIT, "--tag-type", "tag"])
        self.assertEqual((code, out), (0, "rehearsal=123456789\n"))

    def test_message_without_the_lines_exits_one(self) -> None:
        message = self.directory / "message.txt"
        message.write_text("PropStruct 1.2.3\n", encoding="utf-8")
        code, out, err = run_main(["message", "--tag-message", str(message), "--commit", COMMIT])
        self.assertEqual((code, out), (1, ""))
        self.assertEqual(len(err.splitlines()), 2)

    def test_run_exit_codes(self) -> None:
        good = self.directory / "good.json"
        good.write_text(json.dumps(GOOD_RUN), encoding="utf-8")
        bad = self.directory / "bad.json"
        bad.write_text(json.dumps({**GOOD_RUN, "event": "push"}), encoding="utf-8")
        garbage = self.directory / "garbage.json"
        garbage.write_text("not json", encoding="utf-8")
        for name, expected in ((good, 0), (bad, 1), (garbage, 1)):
            with self.subTest(file=name.name):
                code, _, _ = run_main(["run", "--run-json", str(name), "--run-id", "7", "--commit", COMMIT])
                self.assertEqual(code, expected)

    def test_a_usage_error_exits_two(self) -> None:
        self.assertEqual(run_main([])[0], 2)
        self.assertEqual(run_main(["message", "--commit", COMMIT])[0], 2)
        self.assertEqual(run_main(["run", "--run-json", "x"])[0], 2)


class RealTreeTest(unittest.TestCase):
    def test_the_trees_own_changelog_has_the_notes_of_its_version(self) -> None:
        changelog = (release.TREE / "CHANGELOG.md").read_text(encoding="utf-8")
        problems, notes = release.check_changelog(changelog, release.version_prefix(release.TREE), released=False)
        self.assertEqual(problems, [])
        self.assertGreater(len(notes), 5)

    def test_the_trees_own_credits_are_green_no_placeholder_is_left(self) -> None:
        # All attribution sites are filled and NOTICE carries no statement about the data, by the
        # owner's decision of 2026-10-05 (root ACCEPTANCE.md, the attribution criterion); the
        # synthetic cases of CreditsTest hold the check red.
        self.assertEqual(release.check_credits(release.TREE), [])


if __name__ == "__main__":
    unittest.main()
