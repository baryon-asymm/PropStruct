#!/usr/bin/env python3
"""Proof that preflight-windows.ps1 is non-degenerate: right on a prepared machine, red once
on each missing item, naming it.

The machine is made of a stub `nvidia-smi.cmd` on the front of the PATH, a scratch CUDA
toolkit, a scratch runner directory and, for the foreign runner, a real process named
`Runner.Listener.exe` started from another directory. Windows PowerShell 5.1 only, as the
script is; the real `git`, `dotnet` and `python` are the ones found on this machine.

    python -X utf8 .github/scripts/test_preflight.py
"""

from __future__ import annotations

import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Dict, List, Optional, Tuple

TREE = Path(__file__).resolve().parents[2]
SCRIPT = Path(__file__).resolve().parent / "preflight-windows.ps1"
POWERSHELL = shutil.which("powershell")
ON_WINDOWS = sys.platform == "win32" and POWERSHELL is not None

STUB = """@echo off
rem cmd splits the first argument at "=" and ",", so %1 is the option's name alone.
if "%~1"=="--query-compute-apps" (
  if defined STUB_COMPUTE echo %STUB_COMPUTE%
  exit /b 0
)
echo stub nvidia-smi
exit /b 0
"""


def directory_of(command: str) -> str:
    found = shutil.which(command)
    if found is None:
        raise AssertionError(f"{command} is not on this machine's PATH: the tests need the real one")
    return str(Path(found).parent)


@unittest.skipUnless(ON_WINDOWS, "the preflight is a Windows PowerShell 5.1 script")
class PreflightTest(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(prefix="propstruct_preflight_test_")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.children: List[subprocess.Popen] = []
        self.addCleanup(self.stop_children)

        self.stub = self.root / "stub"
        self.stub.mkdir()
        (self.stub / "nvidia-smi.cmd").write_text(STUB, encoding="ascii")

        self.toolkit_base = self.root / "toolkits"
        self.toolkit = self.toolkit_base / "v12.9"
        (self.toolkit / "nvvm" / "bin" / "x64").mkdir(parents=True)
        (self.toolkit / "nvvm" / "libdevice").mkdir(parents=True)
        (self.toolkit / "nvvm" / "bin" / "x64" / "nvvm64_40_0.dll").write_bytes(b"")
        (self.toolkit / "nvvm" / "libdevice" / "libdevice.10.bc").write_bytes(b"")

        self.runner = self.root / "runner"
        (self.runner / "_work" / "_temp").mkdir(parents=True)

        self.path = os.pathsep.join([str(self.stub), directory_of("git"), directory_of("dotnet"),
                                     directory_of("python"), str(Path(POWERSHELL).parent)])
        self.environment: Dict[str, str] = {key: value for key, value in os.environ.items()
                                            if not key.startswith(("PROPSTRUCT_", "STUB_", "CUDA_"))}
        self.environment["PATH"] = self.path
        self.environment["RUNNER_TEMP"] = str(self.runner / "_work" / "_temp")

    def stop_children(self) -> None:
        for child in self.children:
            child.kill()
            child.wait()

    def run_preflight(self, working_directory: Path = TREE, **changes: Optional[str]) -> Tuple[int, str]:
        environment = dict(self.environment)
        for key, value in changes.items():
            if value is None:
                environment.pop(key, None)
            else:
                environment[key] = value
        completed = subprocess.run(
            [POWERSHELL, "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", str(SCRIPT),
             "-ToolkitBase", str(self.toolkit_base)],
            cwd=working_directory, env=environment, capture_output=True, text=True, check=False)
        return completed.returncode, completed.stdout + completed.stderr

    def start_listener(self, directory: Path) -> None:
        directory.mkdir(parents=True, exist_ok=True)
        executable = directory / "Runner.Listener.exe"
        shutil.copy2(Path(os.environ.get("SystemRoot", r"C:\Windows")) / "System32" / "ping.exe", executable)
        self.children.append(subprocess.Popen([str(executable), "-n", "60", "127.0.0.1"],
                                              stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))

    def missing_items(self, output: str) -> List[str]:
        return [line for line in output.splitlines() if line.startswith("::error::- ")]

    def test_a_prepared_runner_passes(self) -> None:
        code, output = self.run_preflight()
        self.assertEqual(code, 0, output)
        self.assertIn("preflight: all checks passed", output)
        self.assertEqual(self.missing_items(output), [])

    def test_the_step_wrapper_of_a_composite_action_sees_the_same_exit_code(self) -> None:
        wrapper = (f"$ErrorActionPreference = 'stop'; & '{SCRIPT}' -ToolkitBase '{self.toolkit_base}'; "
                   "if ((Test-Path -LiteralPath variable:\\LASTEXITCODE)) { exit $LASTEXITCODE }")
        for changes, expected in (({}, 0), ({"PROPSTRUCT_NO_CUDA": "1"}, 1)):
            with self.subTest(changes=changes):
                environment = {**self.environment, **changes}
                completed = subprocess.run([POWERSHELL, "-NoProfile", "-NonInteractive", "-Command", wrapper],
                                           cwd=TREE, env=environment, capture_output=True, text=True, check=False)
                self.assertEqual(completed.returncode, expected, completed.stdout + completed.stderr)

    def test_the_runners_own_listener_is_not_foreign(self) -> None:
        self.start_listener(self.runner / "bin")
        code, output = self.run_preflight()
        self.assertEqual(code, 0, output)

    def run_preflight_under_ancestor_listener(self) -> Tuple[int, str]:
        """A hosted image runs its listener from its own directory, outside the one RUNNER_TEMP names:
        the listener here is a copy of cmd.exe, installed there, that starts the preflight."""
        installed = self.root / "hosted-image" / "bin"
        installed.mkdir(parents=True)
        listener = installed / "Runner.Listener.exe"
        shutil.copy2(Path(os.environ.get("SystemRoot", r"C:\Windows")) / "System32" / "cmd.exe", listener)
        completed = subprocess.run(
            [str(listener), "/c", POWERSHELL, "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
             "-File", str(SCRIPT), "-ToolkitBase", str(self.toolkit_base)],
            cwd=TREE, env=self.environment, capture_output=True, text=True, check=False)
        return completed.returncode, completed.stdout + completed.stderr

    def test_a_listener_that_is_an_ancestor_is_the_runners_own_wherever_it_is_installed(self) -> None:
        code, output = self.run_preflight_under_ancestor_listener()
        self.assertEqual(code, 0, output)
        self.assertIn("an ancestor of this process", output)

    def test_an_ancestor_listener_does_not_excuse_another_one_beside_it(self) -> None:
        self.start_listener(self.root / "apthermo-runner" / "bin")
        code, output = self.run_preflight_under_ancestor_listener()
        self.assertEqual(code, 1, output)
        items = self.missing_items(output)
        self.assertEqual(len(items), 1, output)
        self.assertIn("apthermo-runner", items[0])

    def test_no_libdevice_is_red_naming_it(self) -> None:
        (self.toolkit / "nvvm" / "libdevice" / "libdevice.10.bc").unlink()
        code, output = self.run_preflight()
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("nvvm64_40_0.dll with libdevice.10.bc not found", output)

    def test_no_nvvm_dll_is_red_naming_it(self) -> None:
        (self.toolkit / "nvvm" / "bin" / "x64" / "nvvm64_40_0.dll").unlink()
        code, output = self.run_preflight()
        self.assertEqual(code, 1, output)
        self.assertIn("nvvm64_40_0.dll with libdevice.10.bc not found", output)

    def test_a_toolkit_named_by_cuda_path_is_found(self) -> None:
        elsewhere = self.root / "elsewhere"
        shutil.copytree(self.toolkit, elsewhere)
        shutil.rmtree(self.toolkit_base)
        code, output = self.run_preflight(CUDA_PATH=str(elsewhere))
        self.assertEqual(code, 0, output)

    def test_the_direct_override_is_used_when_both_files_exist_and_red_when_one_does_not(self) -> None:
        dll = self.toolkit / "nvvm" / "bin" / "x64" / "nvvm64_40_0.dll"
        bitcode = self.toolkit / "nvvm" / "libdevice" / "libdevice.10.bc"
        shutil.rmtree(self.toolkit_base / "v12.9" / "nvvm" / "libdevice")
        code, output = self.run_preflight(PROPSTRUCT_LIBNVVM_PATH=str(dll), PROPSTRUCT_LIBDEVICE_PATH=str(bitcode))
        self.assertEqual(code, 1, output)
        self.assertIn(str(bitcode), output)

    def test_no_cuda_kill_switch_is_red_naming_it(self) -> None:
        code, output = self.run_preflight(PROPSTRUCT_NO_CUDA="1")
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("PROPSTRUCT_NO_CUDA is set", output)

    def test_the_legacy_directory_is_red_naming_it(self) -> None:
        code, output = self.run_preflight(PROPSTRUCT_LEGACY_DIR=str(self.root))
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("PROPSTRUCT_LEGACY_DIR is set", output)

    def test_a_foreign_runner_is_red_naming_its_process(self) -> None:
        self.start_listener(self.root / "apthermo-runner" / "bin")
        code, output = self.run_preflight()
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("another runner is up", output)
        self.assertIn("apthermo-runner", output)

    def test_a_compute_process_on_the_gpu_is_red_and_graphics_processes_are_not(self) -> None:
        graphics = r"12428, C:\Windows\explorer.exe, [N/A]"
        code, output = self.run_preflight(STUB_COMPUTE=graphics.replace(",", "^,"))
        self.assertEqual(code, 0, output)
        compute = r"7788, C:\Program Files\dotnet\dotnet.exe, 1024 MiB"
        code, output = self.run_preflight(STUB_COMPUTE=compute.replace(",", "^,"))
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("a compute process holds the GPU", output)

    def test_no_nvidia_smi_is_red_naming_it(self) -> None:
        (self.stub / "nvidia-smi.cmd").unlink()
        code, output = self.run_preflight()
        self.assertEqual(code, 1, output)
        self.assertIn("nvidia-smi did not run", output)

    def test_an_sdk_that_is_not_installed_is_red_naming_it(self) -> None:
        elsewhere = self.root / "tree"
        elsewhere.mkdir()
        (elsewhere / "global.json").write_text('{"sdk": {"version": "99.0.100", "rollForward": "latestPatch"}}',
                                               encoding="ascii")
        code, output = self.run_preflight(working_directory=elsewhere)
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("dotnet SDK 99.0.100 (global.json", output)

    def test_no_git_is_red_naming_it(self) -> None:
        without_git = os.pathsep.join(part for part in self.path.split(os.pathsep) if part != directory_of("git"))
        code, output = self.run_preflight(PATH=without_git)
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("git not found on PATH", output)

    def test_a_python_older_than_3_8_is_red_naming_it(self) -> None:
        (self.stub / "python.cmd").write_text("@echo off\r\necho Python 3.7.9\r\n", encoding="ascii")
        code, output = self.run_preflight()
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 1, output)
        self.assertIn("python is Python 3.7.9, 3.8 or newer is required", output)

    def test_a_missing_runner_directory_variable_is_red(self) -> None:
        code, output = self.run_preflight(RUNNER_TEMP=None)
        self.assertEqual(code, 1, output)
        self.assertIn("RUNNER_TEMP is not set", output)

    def test_every_missing_item_is_named_at_once(self) -> None:
        (self.toolkit / "nvvm" / "libdevice" / "libdevice.10.bc").unlink()
        code, output = self.run_preflight(PROPSTRUCT_NO_CUDA="1", PROPSTRUCT_LEGACY_DIR=str(self.root))
        self.assertEqual(code, 1, output)
        self.assertEqual(len(self.missing_items(output)), 3, output)


if __name__ == "__main__":
    unittest.main()
