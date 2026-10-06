#!/usr/bin/env python3
"""Proof that legacy.py is non-degenerate: each case of its self-test, and the command
lines run as a user runs them, red on what they guard and right on what they do not.

Synthetic files only: this needs no original and holds no byte of it. The commands
against the real original are the `Legacy` category's gate, in `Fixtures.Tests`.

    python -m unittest tools/legacy/test_legacy.py
"""

from __future__ import annotations

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Dict, List, Tuple

sys.path.insert(0, str(Path(__file__).resolve().parent))

import legacy  # noqa: E402

SCRIPT = Path(legacy.__file__).resolve()


def run_command(arguments: List[str], directory: str = "") -> Tuple[int, str, str]:
    environment = {key: value for key, value in os.environ.items() if key != legacy.ENVIRONMENT_VARIABLE}
    if directory:
        environment[legacy.ENVIRONMENT_VARIABLE] = directory
    completed = subprocess.run(
        [sys.executable, "-X", "utf8", str(SCRIPT), *arguments],
        capture_output=True, text=True, env=environment, check=False,
    )
    return completed.returncode, completed.stdout, completed.stderr


def synthetic_original(root: Path) -> Dict[str, str]:
    """Five synthetic files by the manifest names, and the manifest that records them."""
    contents = {name: f"synthetic {name}".encode("ascii") for name in legacy.MANIFEST_NAMES}
    return legacy.synthetic_directory(root, contents)


class SelfTestCasesTest(unittest.TestCase):
    def test_every_case_of_the_selftest_holds(self) -> None:
        cases = legacy.selftest_cases()
        self.assertGreaterEqual(len(cases), 12)
        self.assertEqual([what for what, held in cases if not held], [])

    def test_selftest_command_exits_0_without_the_variable(self) -> None:
        code, output, _ = run_command(["selftest"])
        self.assertEqual(code, 0)
        self.assertNotIn("FAIL", output)


class ManifestTest(unittest.TestCase):
    def test_the_committed_manifest_names_the_five_files_once_each(self) -> None:
        recorded = legacy.manifest()
        self.assertEqual(tuple(recorded), legacy.MANIFEST_NAMES)
        self.assertEqual(len(set(recorded.values())), 5)

    def test_the_committed_forbidden_list_holds_the_five_hashes(self) -> None:
        self.assertTrue(set(legacy.manifest().values()) <= legacy.forbidden_hashes())
        self.assertNotIn(legacy.sha256_of_bytes(b""), legacy.forbidden_hashes())


class CommandLineTest(unittest.TestCase):
    def test_unset_variable_is_exit_2_for_every_command_that_needs_the_directory(self) -> None:
        for arguments in (["path", legacy.MANIFEST_NAMES[1]], ["check"], ["forbidden"], ["forbidden", "--check"]):
            code, _, error = run_command(arguments)
            self.assertEqual(code, 2, arguments)
            self.assertIn(legacy.ENVIRONMENT_VARIABLE, error)

    def test_a_name_outside_the_manifest_is_exit_2(self) -> None:
        code, _, _ = run_command(["path", "another.exe"], directory=tempfile.gettempdir())
        self.assertEqual(code, 2)

    def test_a_directory_that_is_not_the_original_is_exit_1_naming_file_and_both_hashes(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_legacy_test_") as temporary:
            synthetic_original(Path(temporary))
            code, _, error = run_command(["path", "dforrt.dll"], directory=temporary)
            self.assertEqual(code, 1)
            recorded = legacy.manifest()["dforrt.dll"]
            actual = legacy.sha256_of_bytes(b"synthetic dforrt.dll")
            self.assertIn("dforrt.dll", error)
            self.assertIn(recorded, error)
            self.assertIn(actual, error)

    def test_check_names_every_mismatch_and_a_missing_file(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_legacy_test_") as temporary:
            synthetic_original(Path(temporary))
            (Path(temporary) / legacy.MANIFEST_NAMES[3]).unlink()
            code, output, _ = run_command(["check"], directory=temporary)
            self.assertEqual(code, 1)
            lines = output.splitlines()
            self.assertEqual(len(lines), 5)
            self.assertTrue(all(line.startswith("mismatch ") for line in lines))
            self.assertTrue(lines[3].endswith(" actual missing"))

    def test_forbidden_refuses_an_archive_that_is_not_the_recorded_one(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_legacy_test_") as temporary:
            synthetic_original(Path(temporary))
            code, _, error = run_command(["forbidden", "--check"], directory=temporary)
            self.assertEqual(code, 1)
            self.assertIn(legacy.MANIFEST_NAMES[0], error)


if __name__ == "__main__":
    unittest.main()
