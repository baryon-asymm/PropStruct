#!/usr/bin/env python3
"""Pack the two packages, check their contents, and prove them by use.

The one script CI calls for the packages (.github/BOOT.md, "Continuous integration"):

    python -X utf8 .github/scripts/check_packages.py all      # every stage below, in order
    python -X utf8 .github/scripts/check_packages.py pack     # pack both into the feed
    python -X utf8 .github/scripts/check_packages.py verify   # the content check of the feed
    python -X utf8 .github/scripts/check_packages.py selftest # red on mutated copies of the feed
    python -X utf8 .github/scripts/check_packages.py tool     # install the tool, run the example
    python -X utf8 .github/scripts/check_packages.py sample   # build and run samples/Quickstart

Options: `--tree DIR` (default: the repository this file lies in), `--feed DIR` (default:
`artifacts/packages` of the tree), `--work DIR` (default: a scratch directory under the
feed's parent), `--nuget-packages DIR` (default: `<work>/nuget-packages`, never the
user's global cache, so that a package of the same version packed earlier cannot be
restored instead of the one just packed).

Exit 0 when every stage holds, 1 when a check fails (each problem on its own line of
standard error), 2 on a usage error.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ElementTree
import zipfile
from pathlib import Path
from typing import Callable, Dict, List, Optional, Tuple

TREE = Path(__file__).resolve().parents[2]
FRAMEWORK = "net10.0"
LIBRARY_PROJECT = "src/Output/PropStruct.Output.csproj"
TOOL_PROJECT = "src/Cli/PropStruct.Cli.csproj"
LIBRARY_ID = "PropStruct"
TOOL_ID = "PropStruct.Cli"
TOOL_COMMAND = "propstruct"
# The tool bundles ILGPU.dll, which is NCSA-licensed, so its expression names both licences and
# its package carries ILGPU's licence text, the tree's THIRD-PARTY-NOTICES.txt.
LIBRARY_LICENSE = "MIT"
TOOL_LICENSE = "MIT AND NCSA"
THIRD_PARTY_NOTICES = "THIRD-PARTY-NOTICES.txt"
FORMULATION = "tests/Fixtures/Legacy/formulations/inpt.dat"
EXAMPLE_ARGUMENTS = ["run", "inpt.dat", "--mode", "reference", "--accelerator", "cpu",
                     "--layout", "original", "--seed", "0"]

# The scratch program that compares two `results.m` through the one cut of the time line, the
# `ResultsMTimeLine` of tests/Harness (.github/scripts/BOOT.md, "Constraints"). It exits 0 when
# the cut files are equal, 1 when they differ, 3 when a file has no time line.
TIME_LINE_PROJECT = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="{harness}" />
  </ItemGroup>
</Project>
"""
TIME_LINE_PROGRAM = """using PropStruct.Tests.Harness;

try
{
    var left = ResultsMTimeLine.Remove(File.ReadAllBytes(args[0]));
    var right = ResultsMTimeLine.Remove(File.ReadAllBytes(args[1]));
    if (left.AsSpan().SequenceEqual(right))
    {
        Console.WriteLine(left.Length);
        return 0;
    }

    Console.Error.WriteLine(
        $"first difference at byte {left.AsSpan().CommonPrefixLength(right)}, lengths {left.Length} and {right.Length}");
    return 1;
}
catch (InvalidOperationException noTimeLine)
{
    Console.Error.WriteLine(noTimeLine.Message);
    return 3;
}
"""
HARNESS_PROJECT = "tests/Harness/PropStruct.Tests.Harness.csproj"

# The NuGet configuration of the tool and sample stages: the packed feed and the public source, and
# every id of this tree mapped to the feed alone, so that a PropStruct of the same version published
# on nuget.org can never be restored instead of the one just packed (the invariant "Nothing
# restored from a cache the script did not make", .github/scripts/BOOT.md).
PUBLIC_SOURCE = "https://api.nuget.org/v3/index.json"
NUGET_CONFIG = """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="packed" value="{feed}" />
    <add key="public" value="{public}" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="packed">
      <package pattern="PropStruct*" />
    </packageSource>
    <packageSource key="public">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"""

Package = Dict[str, bytes]


class StageFailed(Exception):
    """A stage found a problem; the message names it."""


# ---------------------------------------------------------------------------------------
# What the tree says
# ---------------------------------------------------------------------------------------

def version_prefix(tree: Path) -> str:
    text = (tree / "Directory.Build.props").read_text(encoding="utf-8")
    found = re.search(r"<VersionPrefix>([^<]+)</VersionPrefix>", text)
    if found is None:
        raise StageFailed("Directory.Build.props has no VersionPrefix")
    return found.group(1).strip()


def ilgpu_version(tree: Path) -> str:
    """The ILGPU version of `Directory.Packages.props`, brackets of the exact range removed;
    the library package must declare it as that exact range, `[<version>]`."""
    text = (tree / "Directory.Packages.props").read_text(encoding="utf-8")
    found = re.search(r'<PackageVersion Include="ILGPU" Version="\[?([^\]"]+)\]?"', text)
    if found is None:
        raise StageFailed("Directory.Packages.props has no version of ILGPU")
    return found.group(1)


def library_assemblies(tree: Path) -> List[str]:
    """Every project of `src/` but the tool, by the name its assembly gets: the machine's
    list of what the library package must hold, never a typed one."""
    names = []
    for project in sorted((tree / "src").glob("*/*.csproj")):
        text = project.read_text(encoding="utf-8")
        if "<PackAsTool>true</PackAsTool>" in text:
            continue
        renamed = re.search(r"<AssemblyName>([^<]+)</AssemblyName>", text)
        names.append(renamed.group(1).strip() if renamed else project.stem)
    return names


def head_commit(tree: Path) -> Optional[str]:
    completed = subprocess.run(["git", "rev-parse", "HEAD"], cwd=tree, capture_output=True,
                               text=True, check=False)
    return completed.stdout.strip() if completed.returncode == 0 else None


# ---------------------------------------------------------------------------------------
# Reading and rewriting a package
# ---------------------------------------------------------------------------------------

def read_package(path: Path) -> Package:
    with zipfile.ZipFile(path) as archive:
        return {entry.filename: archive.read(entry.filename) for entry in archive.infolist()}


def write_package(path: Path, entries: Package) -> None:
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, data in entries.items():
            archive.writestr(name, data)


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def nuspec_of(entries: Package) -> Optional[ElementTree.Element]:
    for name, data in entries.items():
        if name.endswith(".nuspec") and "/" not in name:
            return ElementTree.fromstring(data.decode("utf-8-sig"))
    return None


def metadata_children(nuspec: ElementTree.Element, name: str) -> List[ElementTree.Element]:
    found = []
    for section in nuspec:
        if local_name(section.tag) == "metadata":
            found.extend(child for child in section if local_name(child.tag) == name)
    return found


def metadata_text(nuspec: ElementTree.Element, name: str) -> Optional[str]:
    children = metadata_children(nuspec, name)
    return (children[0].text or "").strip() if children else None


def dependencies_of(nuspec: ElementTree.Element) -> List[Tuple[str, str]]:
    found = []
    for container in metadata_children(nuspec, "dependencies"):
        for node in container.iter():
            if local_name(node.tag) == "dependency":
                found.append((node.get("id", ""), node.get("version", "")))
    return found


# ---------------------------------------------------------------------------------------
# The content check
# ---------------------------------------------------------------------------------------

def compare_sets(label: str, expected: set, actual: set) -> List[str]:
    problems = [f"{label}: missing {name}" for name in sorted(expected - actual)]
    problems += [f"{label}: unexpected {name}" for name in sorted(actual - expected)]
    return problems


def check_metadata(label: str, entries: Package, identifier: str, version: str) -> List[str]:
    nuspec = nuspec_of(entries)
    if nuspec is None:
        return [f"{label}: no nuspec"]
    problems = []
    if metadata_text(nuspec, "id") != identifier:
        problems.append(f"{label}: id is {metadata_text(nuspec, 'id')}, expected {identifier}")
    if metadata_text(nuspec, "version") != version:
        problems.append(f"{label}: version is {metadata_text(nuspec, 'version')}, expected {version}")
    return problems


def check_documents(label: str, entries: Package, notice: bytes, license_expression: str) -> List[str]:
    nuspec = nuspec_of(entries)
    if nuspec is None:
        return [f"{label}: no nuspec"]
    problems = []
    license_nodes = metadata_children(nuspec, "license")
    if not license_nodes or license_nodes[0].get("type") != "expression" \
            or (license_nodes[0].text or "").strip() != license_expression:
        problems.append(f"{label}: the license is not the expression {license_expression}")
    readme = metadata_text(nuspec, "readme")
    if readme != "README.md":
        problems.append(f"{label}: the nuspec names no README.md as readme")
    if not entries.get("README.md", b"").strip():
        problems.append(f"{label}: README.md missing or empty")
    if entries.get("NOTICE") != notice:
        problems.append(f"{label}: NOTICE missing or not the tree's")
    if not (metadata_text(nuspec, "description") or ""):
        problems.append(f"{label}: no description")
    return problems


def verify_library(nupkg: Package, snupkg: Package, assemblies: List[str], version: str,
                   ilgpu: str, notice: bytes, commit: Optional[str]) -> List[str]:
    problems = check_metadata(f"{LIBRARY_ID}.nupkg", nupkg, LIBRARY_ID, version)
    problems += check_metadata(f"{LIBRARY_ID}.snupkg", snupkg, LIBRARY_ID, version)
    expected = {f"lib/{FRAMEWORK}/{name}.{extension}" for name in assemblies for extension in ("dll", "xml")}
    actual = {name for name in nupkg if name.startswith("lib/")}
    problems += compare_sets(f"{LIBRARY_ID}.nupkg lib", expected, actual)
    expected_symbols = {f"lib/{FRAMEWORK}/{name}.pdb" for name in assemblies}
    actual_symbols = {name for name in snupkg if name.startswith("lib/")}
    problems += compare_sets(f"{LIBRARY_ID}.snupkg lib", expected_symbols, actual_symbols)
    nuspec = nuspec_of(nupkg)
    if nuspec is not None:
        dependencies = dependencies_of(nuspec)
        problems += [f"{LIBRARY_ID}.nupkg: dependency {name} {declared}"
                     for name, declared in dependencies if name != "ILGPU"]
        ilgpu_declared = [declared for name, declared in dependencies if name == "ILGPU"]
        if ilgpu_declared != [f"[{ilgpu}]"]:
            problems.append(f"{LIBRARY_ID}.nupkg: ILGPU declared as {ilgpu_declared}, expected "
                            f"['[{ilgpu}]'], the exact version Execution asserts")
        repository = metadata_children(nuspec, "repository")
        if not repository or repository[0].get("type") != "git" or not repository[0].get("commit"):
            problems.append(f"{LIBRARY_ID}.nupkg: no git repository commit in the nuspec")
        elif commit is not None and repository[0].get("commit") != commit:
            problems.append(f"{LIBRARY_ID}.nupkg: commit {repository[0].get('commit')}, expected {commit}")
    problems += check_documents(f"{LIBRARY_ID}.nupkg", nupkg, notice, LIBRARY_LICENSE)
    return problems


def verify_tool(nupkg: Package, snupkg: Package, assemblies: List[str], version: str,
                notice: bytes, third_party_notices: bytes) -> List[str]:
    label = f"{TOOL_ID}.nupkg"
    problems = check_metadata(label, nupkg, TOOL_ID, version)
    problems += check_metadata(f"{TOOL_ID}.snupkg", snupkg, TOOL_ID, version)
    nuspec = nuspec_of(nupkg)
    if nuspec is not None:
        types = [node.get("name") for container in metadata_children(nuspec, "packageTypes")
                 for node in container]
        if types != ["DotnetTool"]:
            problems.append(f"{label}: package types {types}, expected ['DotnetTool']")
        problems += [f"{label}: dependency {name}" for name, _ in dependencies_of(nuspec)]
    root = f"tools/{FRAMEWORK}/any"
    settings = nupkg.get(f"{root}/DotnetToolSettings.xml", b"").decode("utf-8-sig")
    if f'<Command Name="{TOOL_COMMAND}"' not in settings:
        problems.append(f"{label}: DotnetToolSettings.xml does not name the command {TOOL_COMMAND}")
    required = {f"{root}/{TOOL_COMMAND}.dll", f"{root}/ILGPU.dll"}
    required |= {f"{root}/{name}.dll" for name in assemblies}
    problems += [f"{label}: missing {name}" for name in sorted(required - set(nupkg))]
    problems += check_documents(label, nupkg, notice, TOOL_LICENSE)
    if nupkg.get(THIRD_PARTY_NOTICES) != third_party_notices:
        problems.append(f"{label}: {THIRD_PARTY_NOTICES} missing or not the tree's")
    expected_symbols = {f"{root}/{TOOL_COMMAND}.pdb"} | {f"{root}/{name}.pdb" for name in assemblies}
    actual_symbols = {name for name in snupkg if name.endswith(".pdb")}
    problems += compare_sets(f"{TOOL_ID}.snupkg", expected_symbols, actual_symbols)
    return problems


def feed_files(feed: Path, version: str) -> Dict[str, Path]:
    return {
        "library": feed / f"{LIBRARY_ID}.{version}.nupkg",
        "library-symbols": feed / f"{LIBRARY_ID}.{version}.snupkg",
        "tool": feed / f"{TOOL_ID}.{version}.nupkg",
        "tool-symbols": feed / f"{TOOL_ID}.{version}.snupkg",
    }


def verify_feed(tree: Path, feed: Path) -> List[str]:
    version = version_prefix(tree)
    paths = feed_files(feed, version)
    missing = [f"{key}: {path.name} not in {feed}" for key, path in paths.items() if not path.is_file()]
    if missing:
        return missing
    packages = {key: read_package(path) for key, path in paths.items()}
    assemblies = library_assemblies(tree)
    notice = (tree / "NOTICE").read_bytes()
    problems = verify_library(packages["library"], packages["library-symbols"], assemblies, version,
                              ilgpu_version(tree), notice, head_commit(tree))
    problems += verify_tool(packages["tool"], packages["tool-symbols"], assemblies, version, notice,
                            (tree / THIRD_PARTY_NOTICES).read_bytes())
    return problems


# ---------------------------------------------------------------------------------------
# Mutations: the red of the content check, applied to a scratch copy
# ---------------------------------------------------------------------------------------

def drop_matching(entries: Package, pattern: str) -> None:
    victim = sorted(name for name in entries if re.search(pattern, name))[0]
    del entries[victim]


def edit_nuspec(entries: Package, edit: Callable[[str], str]) -> None:
    name = next(name for name in entries if name.endswith(".nuspec") and "/" not in name)
    entries[name] = edit(entries[name].decode("utf-8-sig")).encode("utf-8")


def add_dependency(text: str) -> str:
    if "<dependencies>" not in text:
        return text.replace("</metadata>",
                            '<dependencies><dependency id="Extra.Package" version="1.0.0" /></dependencies></metadata>')
    return re.sub(r"(<dependencies>.*?)(</dependencies>)",
                  r'\1<dependency id="Extra.Package" version="1.0.0" />\2', text, count=1, flags=re.S)


def ilgpu_as_a_floor(text: str) -> str:
    return re.sub(r'(<dependency id="ILGPU" version=")\[([^\]"]+)\]', r"\g<1>\g<2>", text)


OTHER_COMMAND = b'<DotNetCliTool><Commands><Command Name="other" EntryPoint="x" Runner="dotnet" /></Commands></DotNetCliTool>'

# (name, which file of the feed, what it does, the text its problem must contain)
Mutation = Tuple[str, str, Callable[[Package], None], str]

MUTATIONS: List[Mutation] = [
    ("library without an XML documentation file", "library",
     lambda entries: drop_matching(entries, r"^lib/.*\.xml$"), "missing lib/"),
    ("library without one assembly", "library",
     lambda entries: drop_matching(entries, r"^lib/.*PropStruct\.Particle\.dll$"), "missing lib/"),
    ("library with an extra assembly", "library",
     lambda entries: entries.update({f"lib/{FRAMEWORK}/Extra.Assembly.dll": b"x"}), "unexpected lib/"),
    ("library with a second dependency", "library",
     lambda entries: edit_nuspec(entries, add_dependency), "dependency Extra.Package"),
    ("library without ILGPU", "library",
     lambda entries: edit_nuspec(entries, lambda text: re.sub(r"<dependency id=\"ILGPU\"[^>]*/>", "", text)),
     "ILGPU declared as"),
    ("library without README", "library",
     lambda entries: entries.pop("README.md"), "README.md missing"),
    ("library with ILGPU as a floor, not the exact version", "library",
     lambda entries: edit_nuspec(entries, ilgpu_as_a_floor),
     "ILGPU declared as"),
    ("library without NOTICE", "library",
     lambda entries: entries.pop("NOTICE"), "NOTICE missing"),
    ("library under another license", "library",
     lambda entries: edit_nuspec(entries, lambda text: text.replace(">MIT<", ">Apache-2.0<")),
     "license is not the expression MIT"),
    ("symbols without one PDB", "library-symbols",
     lambda entries: drop_matching(entries, r"^lib/.*\.pdb$"), "missing lib/"),
    ("tool without one library assembly", "tool",
     lambda entries: drop_matching(entries, r"PropStruct\.Simulation\.dll$"), "missing tools/"),
    ("tool under another command name", "tool",
     lambda entries: entries.update({f"tools/{FRAMEWORK}/any/DotnetToolSettings.xml": OTHER_COMMAND}),
     "does not name the command"),
    ("tool without the ILGPU licence", "tool",
     lambda entries: entries.pop(THIRD_PARTY_NOTICES), f"{THIRD_PARTY_NOTICES} missing"),
    ("tool with another ILGPU licence text", "tool",
     lambda entries: entries.update({THIRD_PARTY_NOTICES: b"not ILGPU's licence"}),
     f"{THIRD_PARTY_NOTICES} missing or not the tree's"),
    ("tool under the MIT expression alone", "tool",
     lambda entries: edit_nuspec(entries, lambda text: text.replace(">MIT AND NCSA<", ">MIT<")),
     "license is not the expression MIT AND NCSA"),
    ("tool with a dependency", "tool",
     lambda entries: edit_nuspec(entries, add_dependency), "dependency Extra.Package"),
]


def selftest_feed(tree: Path, feed: Path, scratch: Path) -> List[str]:
    """Verify the feed, then verify a copy of it per mutation: the feed must be green and
    every mutated copy red with the problem the mutation names. Returns what went wrong."""
    failures = [f"the real feed is not green: {problem}" for problem in verify_feed(tree, feed)]
    version = version_prefix(tree)
    for what, key, mutate, expected in MUTATIONS:
        copy = scratch / re.sub(r"\W+", "-", what)
        shutil.rmtree(copy, ignore_errors=True)
        copy.mkdir(parents=True)
        for path in feed_files(feed, version).values():
            shutil.copy2(path, copy / path.name)
        victim = copy / feed_files(feed, version)[key].name
        entries = read_package(victim)
        mutate(entries)
        write_package(victim, entries)
        problems = verify_feed(tree, copy)
        if not any(expected in problem for problem in problems):
            failures.append(f"not red on: {what} (wanted a problem containing {expected!r}, got {problems})")
        else:
            print(f"red on: {what}")
    return failures


# ---------------------------------------------------------------------------------------
# Running things
# ---------------------------------------------------------------------------------------

def run(command: List[str], directory: Path, environment: Dict[str, str]) -> str:
    completed = subprocess.run(command, cwd=directory, env=environment, capture_output=True,
                               text=True, check=False)
    if completed.returncode != 0:
        tail = "\n".join((completed.stdout + completed.stderr).strip().splitlines()[-40:])
        raise StageFailed(f"{' '.join(command)} exited {completed.returncode}\n{tail}")
    return completed.stdout


class Session:
    """The directories and the environment one run of the script uses."""

    def __init__(self, tree: Path, feed: Path, work: Path, nuget_packages: Optional[Path]) -> None:
        self.tree = tree
        self.feed = feed
        self.work = work
        self.version = version_prefix(tree)
        self.environment = dict(os.environ)
        self.environment["NUGET_PACKAGES"] = str(nuget_packages or work / "nuget-packages")
        self.environment["DOTNET_NOLOGO"] = "1"
        self.environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        self.time_line_program: Optional[Path] = None

    def nuget_config(self, public_source: str = PUBLIC_SOURCE) -> Path:
        """The config the tool and sample stages restore with: the feed for `PropStruct*`, the
        public source for everything else."""
        path = self.work / "nuget.config"
        path.write_text(NUGET_CONFIG.replace("{feed}", str(self.feed)).replace("{public}", public_source),
                        encoding="utf-8", newline="\n")
        return path

    def scratch(self, name: str) -> Path:
        path = self.work / name
        shutil.rmtree(path, ignore_errors=True)
        path.mkdir(parents=True)
        return path

    def dotnet(self, *arguments: str) -> str:
        return run(["dotnet", *arguments], self.tree, self.environment)

    def working_directory_with_formulation(self, name: str) -> Path:
        directory = self.scratch(name)
        shutil.copy2(self.tree / FORMULATION, directory / "inpt.dat")
        return directory

    def time_line_cut(self) -> Path:
        """The built scratch program of the time-line comparison, built once per session."""
        if self.time_line_program is None:
            source = self.work / "time-line-cut"
            shutil.rmtree(source, ignore_errors=True)
            source.mkdir(parents=True)
            harness = (self.tree / HARNESS_PROJECT).as_posix()
            (source / "TimeLineCut.csproj").write_text(TIME_LINE_PROJECT.replace("{harness}", harness),
                                                       encoding="utf-8", newline="\n")
            (source / "Program.cs").write_text(TIME_LINE_PROGRAM, encoding="utf-8", newline="\n")
            self.dotnet("build", str(source / "TimeLineCut.csproj"), "--configuration", "Release",
                        "--output", str(source / "out"))
            self.time_line_program = source / "out" / "TimeLineCut.dll"
        return self.time_line_program

    def require_same_results(self, what: str, left: Path, right: Path) -> None:
        """The two `results.m` are equal byte for byte with the time line cut by `Harness`'s
        `ResultsMTimeLine`: nothing else is cut and a file with no time line is an error."""
        completed = subprocess.run(["dotnet", str(self.time_line_cut()), str(left), str(right)],
                                   cwd=self.tree, env=self.environment, capture_output=True, text=True,
                                   check=False)
        if completed.returncode == 0:
            print(f"{what}: results.m equal byte for byte, time line excluded ({completed.stdout.strip()} bytes)")
            return
        reason = "has no time line" if completed.returncode == 3 else "differ, time line excluded"
        raise StageFailed(f"{what}: results.m {reason}: {completed.stderr.strip() or completed.stdout.strip()}")


def stage_pack(session: Session) -> None:
    session.feed.mkdir(parents=True, exist_ok=True)
    for stale in session.feed.glob("PropStruct*.*pkg"):
        stale.unlink()
    for project in (LIBRARY_PROJECT, TOOL_PROJECT):
        session.dotnet("pack", project, "--configuration", "Release", "-p:ContinuousIntegrationBuild=true",
                       "--output", str(session.feed))
    print(f"packed into {session.feed}: {', '.join(sorted(path.name for path in session.feed.iterdir()))}")


def stage_verify(session: Session) -> None:
    problems = verify_feed(session.tree, session.feed)
    if problems:
        raise StageFailed("\n".join(problems))
    print(f"the packages of {session.version} hold exactly the library assemblies "
          f"({', '.join(library_assemblies(session.tree))}), their XML documentation and PDBs; "
          "ILGPU is the one dependency")


def stage_selftest(session: Session) -> None:
    failures = selftest_feed(session.tree, session.feed, session.scratch("mutated-feeds"))
    if failures:
        raise StageFailed("\n".join(failures))
    print(f"the content check is green on the real feed and red on all {len(MUTATIONS)} mutations")


def stage_tool(session: Session) -> None:
    installed = session.scratch("tool")
    session.dotnet("tool", "install", TOOL_ID, "--tool-path", str(installed), "--configfile",
                   str(session.nuget_config()), "--version", session.version, "--ignore-failed-sources")
    executable = shutil.which(TOOL_COMMAND, path=str(installed))
    if executable is None:
        raise StageFailed(f"the tool installed from {session.feed} left no {TOOL_COMMAND} in {installed}")
    version = run([executable, "--version"], session.tree, session.environment).strip()
    if not version.startswith(session.version):
        raise StageFailed(f"{TOOL_COMMAND} --version says {version!r}, expected {session.version}")
    print(f"{TOOL_COMMAND} --version: {version}")
    source_built = session.scratch("cli-source")
    session.dotnet("build", TOOL_PROJECT, "--configuration", "Release", "--output", str(source_built))
    packed_directory = session.working_directory_with_formulation("tool-run")
    run([executable, *EXAMPLE_ARGUMENTS], packed_directory, session.environment)
    source_directory = session.working_directory_with_formulation("cli-source-run")
    run(["dotnet", str(source_built / f"{TOOL_COMMAND}.dll"), *EXAMPLE_ARGUMENTS], source_directory,
        session.environment)
    session.require_same_results("the installed tool against the source-built one",
                                 packed_directory / "results.m", source_directory / "results.m")


def build_sample(session: Session, name: str, *properties: str) -> Path:
    output = session.scratch(name)
    session.dotnet("build", "samples/Quickstart/PropStruct.Quickstart.csproj", "--configuration", "Release",
                   "--no-incremental", "--output", str(output), *properties)
    return output


def require_library_kind(session: Session, built: Path, kind: str) -> None:
    """The sample's own dependency file says whether it got `PropStruct` as a package or as a
    project: the proof that each build mode is the one it names."""
    dependencies = json.loads((built / "PropStruct.Quickstart.deps.json").read_text(encoding="utf-8"))
    found = dependencies["libraries"].get(f"{LIBRARY_ID}/{session.version}", {}).get("type")
    if found != kind:
        raise StageFailed(f"the sample got {LIBRARY_ID} as {found!r}, expected {kind!r} ({built})")


def run_sample(session: Session, built: Path, name: str) -> Path:
    directory = session.working_directory_with_formulation(name)
    printed = run(["dotnet", str(built / "PropStruct.Quickstart.dll")], directory, session.environment)
    names = [Path(line.strip()).name for line in printed.splitlines()]
    if names != ["results.m", "results.json"]:
        raise StageFailed(f"the sample printed {printed!r}, expected the paths of results.m and results.json")
    for produced in names:
        if not (directory / produced).is_file():
            raise StageFailed(f"the sample printed {produced} but wrote none in {directory}")
    return directory


def stage_sample(session: Session) -> None:
    packed = build_sample(session, "sample-package", f"-p:PropStructPackageVersion={session.version}",
                          f"-p:RestoreConfigFile={session.nuget_config()}")
    referenced = build_sample(session, "sample-project")
    require_library_kind(session, packed, "package")
    require_library_kind(session, referenced, "project")
    packed_directory = run_sample(session, packed, "sample-package-run")
    referenced_directory = run_sample(session, referenced, "sample-project-run")
    print("samples/Quickstart built against the packed PropStruct and ran")
    session.require_same_results("the sample on the package against the sample on the projects",
                                 packed_directory / "results.m", referenced_directory / "results.m")


STAGES: Dict[str, Callable[[Session], None]] = {
    "pack": stage_pack,
    "verify": stage_verify,
    "selftest": stage_selftest,
    "tool": stage_tool,
    "sample": stage_sample,
}


def main(arguments: List[str]) -> int:
    parser = argparse.ArgumentParser(description="Pack, check and use the two packages.")
    parser.add_argument("stage", choices=["all", *STAGES])
    parser.add_argument("--tree", type=Path, default=TREE)
    parser.add_argument("--feed", type=Path)
    parser.add_argument("--work", type=Path)
    parser.add_argument("--nuget-packages", type=Path)
    try:
        options = parser.parse_args(arguments)
    except SystemExit as usage:
        return 2 if usage.code else 0
    tree = options.tree.resolve()
    feed = (options.feed or tree / "artifacts" / "packages").resolve()
    work = (options.work or feed.parent / "package-check").resolve()
    work.mkdir(parents=True, exist_ok=True)
    session = Session(tree, feed, work, options.nuget_packages.resolve() if options.nuget_packages else None)
    chosen = list(STAGES) if options.stage == "all" else [options.stage]
    for name in chosen:
        print(f"== {name}")
        try:
            STAGES[name](session)
        except StageFailed as failure:
            print(f"{name} failed:\n{failure}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
