#!/usr/bin/env python3
"""Proof that check_packages.py is non-degenerate: its content check is green on a package
set built to the tree's own list, red on each of its mutations, and its comparison of two
`results.m` is red on a differing byte and blind to the time line alone.

Synthetic packages for the content check: it needs no dotnet and no build; the same mutations run
against the real packages with `check_packages.py selftest`. The comparison of two `results.m`
builds the Harness's time-line cut into a scratch program, so those cases need dotnet.

    python -X utf8 .github/scripts/test_check_packages.py
"""

from __future__ import annotations

import os
import re
import shutil
import sys
import tempfile
import unittest
from pathlib import Path
from typing import Dict, List

sys.path.insert(0, str(Path(__file__).resolve().parent))

import check_packages as packages  # noqa: E402

TREE = packages.TREE
COMMIT = packages.head_commit(TREE) or "0" * 40


def library_nuspec(version: str, ilgpu: str) -> bytes:
    return f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>
<id>{packages.LIBRARY_ID}</id><version>{version}</version><description>synthetic</description>
<license type="expression">{packages.LIBRARY_LICENSE}</license><readme>README.md</readme>
<repository type="git" commit="{COMMIT}" />
<dependencies><group targetFramework="net10.0"><dependency id="ILGPU" version="[{ilgpu}]" exclude="Build,Analyzers" /></group></dependencies>
</metadata></package>""".encode("utf-8")


def tool_nuspec(version: str) -> bytes:
    return f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"><metadata>
<id>{packages.TOOL_ID}</id><version>{version}</version><description>synthetic</description>
<license type="expression">{packages.TOOL_LICENSE}</license><readme>README.md</readme>
<packageTypes><packageType name="DotnetTool" /></packageTypes>
</metadata></package>""".encode("utf-8")


def symbols_nuspec(identifier: str, version: str) -> bytes:
    return f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>
<id>{identifier}</id><version>{version}</version></metadata></package>""".encode("utf-8")


def synthetic_feed(directory: Path) -> Path:
    """The four files of a feed holding exactly what the tree says they must hold."""
    version = packages.version_prefix(TREE)
    assemblies = packages.library_assemblies(TREE)
    notice = (TREE / "NOTICE").read_bytes()
    documents: Dict[str, bytes] = {"README.md": b"readme", "NOTICE": notice}
    framework = packages.FRAMEWORK
    root = f"tools/{framework}/any"
    library: Dict[str, bytes] = {f"{packages.LIBRARY_ID}.nuspec": library_nuspec(version, packages.ilgpu_version(TREE))}
    library.update({f"lib/{framework}/{name}.{extension}": b"x" for name in assemblies for extension in ("dll", "xml")})
    library.update(documents)
    library_symbols: Dict[str, bytes] = {f"{packages.LIBRARY_ID}.nuspec": symbols_nuspec(packages.LIBRARY_ID, version)}
    library_symbols.update({f"lib/{framework}/{name}.pdb": b"x" for name in assemblies})
    tool: Dict[str, bytes] = {f"{packages.TOOL_ID}.nuspec": tool_nuspec(version)}
    tool[f"{root}/DotnetToolSettings.xml"] = (
        f'<DotNetCliTool><Commands><Command Name="{packages.TOOL_COMMAND}" EntryPoint="x.dll" Runner="dotnet" />'
        "</Commands></DotNetCliTool>").encode("utf-8")
    for name in [packages.TOOL_COMMAND, "ILGPU", *assemblies]:
        tool[f"{root}/{name}.dll"] = b"x"
    tool.update(documents)
    tool[packages.THIRD_PARTY_NOTICES] = (TREE / packages.THIRD_PARTY_NOTICES).read_bytes()
    tool_symbols: Dict[str, bytes] = {f"{packages.TOOL_ID}.nuspec": symbols_nuspec(packages.TOOL_ID, version)}
    tool_symbols.update({f"{root}/{name}.pdb": b"x" for name in [packages.TOOL_COMMAND, *assemblies]})
    directory.mkdir(parents=True, exist_ok=True)
    paths = packages.feed_files(directory, version)
    for key, entries in (("library", library), ("library-symbols", library_symbols),
                         ("tool", tool), ("tool-symbols", tool_symbols)):
        packages.write_package(paths[key], entries)
    return directory


class TreeFactsTest(unittest.TestCase):
    def test_the_library_list_is_the_src_projects_but_the_tool(self) -> None:
        assemblies = packages.library_assemblies(TREE)
        self.assertIn("PropStruct.Output", assemblies)
        self.assertNotIn(packages.TOOL_COMMAND, assemblies)
        self.assertNotIn(packages.TOOL_ID, assemblies)
        self.assertEqual(len(assemblies), len(list((TREE / "src").glob("*/*.csproj"))) - 1)

    def test_the_version_is_a_release_version(self) -> None:
        self.assertRegex(packages.version_prefix(TREE), r"^\d+\.\d+\.\d+$")


class ContentCheckTest(unittest.TestCase):
    def test_a_feed_built_to_the_trees_list_is_green(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_packages_test_") as temporary:
            feed = synthetic_feed(Path(temporary) / "feed")
            self.assertEqual(packages.verify_feed(TREE, feed), [])

    def test_an_empty_feed_is_red_naming_the_files(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_packages_test_") as temporary:
            problems = packages.verify_feed(TREE, Path(temporary))
            self.assertEqual(len(problems), 4)

    def test_every_mutation_is_red_with_its_problem_and_the_feed_stays_green(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_packages_test_") as temporary:
            feed = synthetic_feed(Path(temporary) / "feed")
            failures = packages.selftest_feed(TREE, feed, Path(temporary) / "scratch")
            self.assertEqual(failures, [])
            self.assertEqual(packages.verify_feed(TREE, feed), [])

    def test_a_library_missing_the_assembly_of_a_new_project_is_red(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_packages_test_") as temporary:
            feed = synthetic_feed(Path(temporary) / "feed")
            path = packages.feed_files(feed, packages.version_prefix(TREE))["library"]
            entries = packages.read_package(path)
            del entries[f"lib/{packages.FRAMEWORK}/PropStruct.Random.dll"]
            packages.write_package(path, entries)
            problems = packages.verify_feed(TREE, feed)
            self.assertEqual(problems, [f"{packages.LIBRARY_ID}.nupkg lib: missing lib/{packages.FRAMEWORK}/PropStruct.Random.dll"])

    def test_every_mutation_names_a_file_of_the_feed(self) -> None:
        keys = {key for _, key, _, _ in packages.MUTATIONS}
        self.assertEqual(keys, {"library", "library-symbols", "tool"})


class ResultsComparisonTest(unittest.TestCase):
    """The comparison of two `results.m` goes through the one cut of the time line, `Harness`'s
    `ResultsMTimeLine`, built into a scratch program; these cases need dotnet and a build of
    the Harness, and the work directory holds the restored packages between runs."""

    BODY = b"Plot1 = 1950.0;\r\n% Calculation time:  0 : 0 : 3\r\nDok43 = [1 2 3];\r\n"
    session: packages.Session

    @classmethod
    def setUpClass(cls) -> None:
        work = TREE / "artifacts" / "time-line-test"
        work.mkdir(parents=True, exist_ok=True)
        cls.session = packages.Session(TREE, work / "feed", work, None)
        cls.directory = Path(tempfile.mkdtemp(prefix="propstruct_packages_test_"))

    @classmethod
    def tearDownClass(cls) -> None:
        shutil.rmtree(cls.directory, ignore_errors=True)

    def write(self, name: str, content: bytes) -> Path:
        path = self.directory / name
        path.write_bytes(content)
        return path

    def test_the_time_line_alone_is_not_a_difference(self) -> None:
        left = self.write("left.m", self.BODY)
        right = self.write("right.m", self.BODY.replace(b"0 : 0 : 3", b"0 : 1 : 41"))
        self.session.require_same_results("test", left, right)

    def test_a_changed_byte_elsewhere_is_red(self) -> None:
        left = self.write("left.m", self.BODY)
        right = self.write("right.m", self.BODY.replace(b"[1 2 3]", b"[1 2 4]"))
        with self.assertRaises(packages.StageFailed) as raised:
            self.session.require_same_results("test", left, right)
        self.assertIn("differ, time line excluded", str(raised.exception))

    def test_a_line_terminator_changed_elsewhere_is_red(self) -> None:
        left = self.write("left.m", self.BODY)
        right = self.write("right.m", self.BODY.replace(b"Dok43 = [1 2 3];\r\n", b"Dok43 = [1 2 3];\n"))
        with self.assertRaises(packages.StageFailed):
            self.session.require_same_results("test", left, right)

    def test_a_file_without_a_time_line_is_red(self) -> None:
        left = self.write("left.m", self.BODY)
        right = self.write("right.m", b"Plot1 = 1950.0;\r\nDok43 = [1 2 3];\r\n")
        with self.assertRaises(packages.StageFailed) as raised:
            self.session.require_same_results("test", left, right)
        self.assertIn("has no time line", str(raised.exception))


class SourceMappingTest(unittest.TestCase):
    """A `PropStruct` of the same version on the public source must never be restored instead of
    the packed one. Two local folders stand for the packed feed and the public source, each
    holding a package `PropStruct` 0.1.0 whose description says where it came from. A project
    restoring through the session's config gets the packed one; through a config without the
    mapping, and with the public source first, it gets the other, so the instrument sees the
    difference. Restoring from folders needs dotnet and no network."""

    def make_feed(self, directory: Path, origin: str) -> Path:
        directory.mkdir(parents=True)
        nuspec = ('<?xml version="1.0" encoding="utf-8"?><package xmlns="http://schemas.microsoft.com/packaging/'
                  '2013/05/nuspec.xsd"><metadata><id>PropStruct</id><version>0.1.0</version>'
                  f"<authors>test</authors><description>{origin}</description></metadata></package>")
        packages.write_package(directory / "PropStruct.0.1.0.nupkg",
                               {"PropStruct.nuspec": nuspec.encode("utf-8"), "lib/net10.0/_._": b""})
        return directory

    def restored_origin(self, root: Path, config: Path) -> str:
        project = root / "restore"
        project.mkdir(parents=True)
        (project / "Restore.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>'
            "<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>"
            '<ItemGroup><PackageReference Include="PropStruct" Version="0.1.0" /></ItemGroup></Project>',
            encoding="utf-8")
        cache = root / "cache"
        environment = {**os.environ, "NUGET_PACKAGES": str(cache), "DOTNET_NOLOGO": "1"}
        packages.run(["dotnet", "restore", str(project / "Restore.csproj"), "--configfile", str(config)],
                     project, environment)
        text = (cache / "propstruct" / "0.1.0" / "propstruct.nuspec").read_text(encoding="utf-8")
        found = re.search(r"<description>([^<]*)</description>", text)
        self.assertIsNotNone(found)
        return found.group(1) if found else ""

    def test_the_packed_feed_wins_for_propstruct_and_loses_without_the_mapping(self) -> None:
        with tempfile.TemporaryDirectory(prefix="propstruct_packages_test_") as temporary:
            root = Path(temporary)
            packed = self.make_feed(root / "packed", "from the packed feed")
            public = self.make_feed(root / "public", "from the public source")
            session = packages.Session(TREE, packed, root / "work", None)
            session.work.mkdir()
            mapped = session.nuget_config(public_source=str(public))
            self.assertEqual(self.restored_origin(root / "mapped", mapped), "from the packed feed")
            unmapped = root / "unmapped.config"
            unmapped.write_text(
                f'<configuration><packageSources><clear /><add key="public" value="{public}" />'
                f'<add key="packed" value="{packed}" /></packageSources></configuration>', encoding="utf-8")
            self.assertEqual(self.restored_origin(root / "unmapped", unmapped), "from the public source")


def problems_are_lines(problems: List[str]) -> bool:
    return all("\n" not in problem for problem in problems)


if __name__ == "__main__":
    unittest.main()
