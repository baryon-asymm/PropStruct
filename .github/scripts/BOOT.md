# BOOT.md — .github/scripts

## Purpose

The scripts the workflows of `.github` run, each a step of a job and none a second copy
of what a project of the tree already does. Stage S3 of the delivery built the first,
`check_packages.py`: it packs the two packages, checks what they hold against what the
tree says they must hold, installs the packed tool, runs the approved example with it
and builds and runs `samples/Quickstart` against the packed library. Stage S4 built the
scripts of the release: `check_release.py`, which checks the version, `CHANGELOG.md`,
the credits, the tag, its message and the rehearsal run it names, and `preflight-windows.ps1`, which
checks the self-hosted GPU runner. Each has a test with a red and a right.

## Invariants

- **The expectation is the tree's, never typed.** The assemblies a package must hold
  are the projects of `src/` but the tool, read from the project files; the version is
  `VersionPrefix` of `Directory.Build.props`; the ILGPU version is the one of
  `Directory.Packages.props`; the notice is the file `NOTICE`. A project added to
  `src/` is demanded in the package without an edit here (AGENTS.md §6, "all").
- **The content check is proven red on mutated copies.** `selftest` verifies the real
  feed, then a scratch copy of it per mutation of `MUTATIONS` (a dropped `.xml`, a
  dropped assembly, an extra assembly, a second dependency, no ILGPU, ILGPU as a floor
  and not the exact version, no README, no `NOTICE`, another license, a dropped PDB, a
  tool without an assembly, another command name, a tool with a dependency, a tool
  without or with another ILGPU licence, a tool under `MIT` alone); every copy must be
  red with the problem the mutation names, the feed itself untouched (AGENTS.md §13).
- **Nothing restored from a cache or a source the script did not make.** Every `dotnet`
  child runs with its own `NUGET_PACKAGES`, `<work>/nuget-packages` unless
  `--nuget-packages` names one, so that a package of the same version packed earlier is
  never restored instead of the one just packed; the tool and sample stages restore
  through a config that maps `PropStruct*` to the feed alone, so that a published one
  is never restored either (`test_check_packages.py`, `SourceMappingTest`: two feeds
  holding different packages of one id and version, the packed one wins, and without the
  mapping the other does). ILGPU is restored from nuget.org into it; nothing else leaves
  the machine.
- **The example is compared with the source, byte for byte.** The installed tool's
  `results.m` for the approved example equals the source-built tool's, and the sample
  built against the package equals the sample built against the projects, each with
  the one line containing `Calculation time` cut and nothing else changed; a file with
  no such line is an error, not a match.
- **The sample says which mode it got.** The sample's dependency file must list
  `PropStruct` as a `package` in the package build and as a `project` in the other, so
  that two builds of one source are proven to differ in what they were built against.
- **Writes only under the feed and the work directory**, by default `artifacts/` of
  the tree, ignored by git; the tree's own build outputs are written by `dotnet` as
  for any build.

- **The release check reads files, never the network.** `check_release.py` takes the
  tag message, the run's JSON and the tree as files and decides on them alone; the
  `gh` call that produces the JSON is a step of the workflow, so each condition is
  proven red on a file (`test_check_release.py`). A condition that holds nothing back
  is a defect: each of seven was disabled in turn and the test failed.
- **The preflight collects, never throws.** `preflight-windows.ps1` runs under
  Windows PowerShell 5.1 with `$ErrorActionPreference` set to `Continue`, so that the
  wrapper of a composite action, which sets `Stop`, cannot end it on the first item,
  and names every missing item at once. A command that is not found is a missing item,
  never a stale `$LASTEXITCODE`.

## Dependencies

- [Output](../../src/Output/API.md) — the project packed as `PropStruct`.
- [Cli](../../src/Cli/API.md) — the project packed as `PropStruct.Cli`, the flags of
  the approved example and its `--version`.
- [Quickstart](../../samples/Quickstart/API.md) — the sample built in both modes.
- [Fixtures](../../tests/Fixtures/API.md) — `Legacy/formulations/inpt.dat`, the input
  of the example and of the sample.
- [Harness](../../tests/Harness/API.md) — `ResultsMTimeLine`, the one cut of the time
  line, which `check_packages.py` builds into a scratch program for its comparisons.

Outside the tree: Python 3.8 or newer (standard library only); the .NET SDK of
`global.json` with network access to nuget.org for the restore of ILGPU.

## Constraints

Inherited from the parent ([BOOT.md](../BOOT.md)). In addition:

- Python standard library only, type-annotated, one module; exit 0 when every stage
  holds, 1 when a check fails (one problem per line on standard error), 2 on a usage
  error.
- The packing command lines carry `-p:ContinuousIntegrationBuild=true` and nothing
  else; the build and the tests never do (`Directory.Build.targets`).
- The time-line cut is `Harness`'s `ResultsMTimeLine`, and no copy of it exists here:
  `check_packages.py` writes a scratch program of a few lines that references the
  Harness project, builds it once per run in Release into its work directory and calls
  it with the two files (exit 0 equal, 1 different, 3 a file without a time line). The
  comparison therefore needs dotnet and the Harness build, and its tests too; the script
  tests of CI run after the Release build for that reason.
- `check_release.py` imports `version_prefix` from `check_packages.py`: the version is
  read in one place.

## Acceptance criteria

- [x] 2026-10-04 — `python -X utf8 .github/scripts/test_check_packages.py` is green: the
      content check is green on a feed built to the tree's own list and red on every
      mutation of `MUTATIONS` with its named problem; the comparison of two `results.m`
      is red on a changed byte and blind to the time line alone.
- [x] 2026-10-04 — `check_packages.py all` is green on the real packages (`pack`,
      `verify`, `tool`, `sample`) and its `selftest` is red on each of the mutations of
      `MUTATIONS` applied to the real feed, every one with the problem it names (the
      run of the commit that built this node).
- [x] 2026-10-04 — One mutation is made in the build itself, not in the zip:
      `PropStruct.Particle` left out of the assembly merge of `src/Output`, packed to a
      scratch feed, makes `verify` red with "PropStruct.nupkg lib: missing
      lib/net10.0/PropStruct.Particle.dll"; reverted before the commit.
- [x] 2026-10-04 — The comparison of two `results.m` goes through `Harness`'s
      `ResultsMTimeLine` and no cut of the time line is left in Python: green on a
      file pair differing in the time line alone, red on a changed byte, red on a
      changed line terminator, red on a file without the time line, naming it
      (`test_check_packages.py`, 12 cases; `check_packages.py all` green, the tool and
      the sample compared through the scratch program, 40897 and 39407 bytes).
- [x] 2026-10-05 — `check_release.py` and `preflight-windows.ps1` are proven red and
      right: `test_check_release.py`, 40 cases, and `test_preflight.py`, 19 cases
      (`.github/ACCEPTANCE.md` has the list of what each is red on). The release check
      is also red on a placeholder of the owner's attribution, and green on this tree:
      all attribution sites filled, `NOTICE` carrying no statement about the data, by the
      owner's decision of 2026-10-05, held by a case of its own.
- [ ] CI runs `check_packages.py` on a hosted Windows runner (stage S6; date, run id).

## Taboos

- No expectation typed where the tree states it.
- No package, tool or sample step that reads the user's global NuGet cache.
- No network access beyond the NuGet restore.
- No publishing, tagging or pushing: this node packs into a local directory.
