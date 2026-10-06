# API.md — .github/scripts

The scripts of the workflows, built by stages S3 and S4 of the delivery (2026-10-04);
the parent's `../API.md`, "## Scripts", says which step runs each.

## check_packages.py ✅

```console
python -X utf8 .github/scripts/check_packages.py all|pack|verify|selftest|tool|sample [--tree DIR] [--feed DIR] [--work DIR] [--nuget-packages DIR]
```

| Stage | What it does |
|---|---|
| `pack` | empties the feed, then `dotnet pack` of `src/Output` (`PropStruct`) and `src/Cli` (`PropStruct.Cli`) with `--configuration Release -p:ContinuousIntegrationBuild=true --output <feed>` |
| `verify` | the content check of the four files of the feed (below) |
| `selftest` | `verify` on the feed, then on a scratch copy per mutation of `MUTATIONS`; red unless every mutation is red with its problem |
| `tool` | installs `PropStruct.Cli` from the feed with `--tool-path`, checks `propstruct --version`, builds `src/Cli` from source, runs `propstruct run inpt.dat --mode reference --accelerator cpu --layout original --seed 0` with each in a scratch directory and compares the two `results.m`, time line cut by `Harness`'s `ResultsMTimeLine` |
| `sample` | builds `samples/Quickstart` against the packed `PropStruct` and against the projects, checks the mode of each from its dependency file, runs both on `inpt.dat` and compares the two `results.m` the same way |
| `all` | the five, in the order above |

Defaults: `--tree` the repository holding the script, `--feed` `artifacts/packages` of
it, `--work` `package-check` beside the feed, `--nuget-packages` `<work>/nuget-packages`.
Exit codes: 0 every stage holds, 1 a check failed (a line per problem on standard
error), 2 a usage error.

The content check: `PropStruct.<version>.nupkg` holds in `lib/net10.0` the `.dll` and
the `.xml` of every project of `src/` but `Cli` and nothing else there, its nuspec has
the id, the version of `Directory.Build.props`, the license expression `MIT`, `README.md`
as readme, the git commit, and as its only dependency ILGPU at the exact range
`[<version>]`, `<version>` being that of `Directory.Packages.props`; the package holds a non-empty `README.md` and the tree's
`NOTICE`; `PropStruct.<version>.snupkg` holds exactly their `.pdb` files.
`PropStruct.Cli.<version>.nupkg` is a `DotnetTool` package with no dependency, the
command `propstruct`, `propstruct.dll`, ILGPU and every library assembly under
`tools/net10.0/any`, the license expression `MIT AND NCSA`, the same README and
`NOTICE`, and `THIRD-PARTY-NOTICES.txt` equal to the tree's (ILGPU's licence, which the
bundled `ILGPU.dll` requires), and its `.snupkg` their PDBs. The tool and sample stages
restore through a generated `nuget.config` in the work directory: the feed and
nuget.org, `PropStruct*` mapped to the feed alone, so that a published package of the
same version is never restored instead of the packed one (`Session.nuget_config`).

The comparison of two `results.m` (`Session.require_same_results`) builds, once per run,
a scratch program under `<work>/time-line-cut` that references
`tests/Harness/PropStruct.Tests.Harness.csproj` and compares the two files through
`ResultsMTimeLine.Remove`; exit 0 equal, 1 different, 3 a file without a time line. No
cut of the time line exists in Python.

Module members the tests read: `library_assemblies`, `version_prefix`, `ilgpu_version`,
`verify_feed`, `selftest_feed`, `MUTATIONS`, `feed_files`, `read_package`,
`write_package`, `Session` with `require_same_results`, `StageFailed`.

## check_release.py ✅

```console
python -X utf8 .github/scripts/check_release.py [--tree DIR] notes [--tag TAG] [--notes FILE]
python -X utf8 .github/scripts/check_release.py message --tag-message FILE --commit SHA [--tag-type TYPE]
python -X utf8 .github/scripts/check_release.py run --run-json FILE --run-id ID --commit SHA
```

| Command | Checks | On success |
|---|---|---|
| `notes` | `CHANGELOG.md` has a non-empty `## [<version>]` section, the version being `VersionPrefix` of `Directory.Build.props`; with `--tag`, the tag is `v<version>` and the section's heading is not marked unreleased; always, no line of `NOTICE`, `README.md` or a file a `src/` project packs as `README.md` holds `<AUTHORS>`, `<AUTHORS, AFFILIATION>` or `<PERMISSION>` (one problem per file and line; green on the tree: all attribution sites filled; NOTICE carries no statement about the data, by the owner's decision of 2026-10-05) | writes the section, its link definitions and surrounding blank lines left out, to `--notes` |
| `message` | `--tag-type` is `tag` when given; the message holds exactly one `Rehearsal: <digits>` line and exactly one `Legacy: <40 hex> <YYYY-MM-DD> <passed>/<total>` line, that sha is `--commit`, and `passed` equals a `total` above zero | prints `rehearsal=<run id>` |
| `run` | the JSON of `gh run view --json status,conclusion,event,workflowName,headSha` says `completed`, `success`, `workflow_dispatch`, `Release`, and a head commit equal to `--commit` | prints one line |

Exit codes: 0 every check holds, 1 a check failed (a line per problem on standard error,
every problem of the command at once), 2 a usage error. Module members the tests read:
`check_tag`, `changelog_section`, `check_changelog`, `check_message`, `check_run`,
`check_credits`, `packed_readmes`, `main`.

## preflight-windows.ps1 ✅

`powershell -File .github/scripts/preflight-windows.ps1 [-ToolkitBase DIR]`, run from
the repository root with `RUNNER_TEMP` set (a job's own, whose grandparent is the
runner's directory). Checks, in this order, collecting every failure: git; the SDK of
`global.json` as `rollForward: latestPatch` resolves it; Python 3.8 or newer;
`nvidia-smi`; libnvvm and libdevice, both files named by `PROPSTRUCT_LIBNVVM_PATH` and
`PROPSTRUCT_LIBDEVICE_PATH` when both are set, else a toolkit directory (`CUDA_PATH`,
then the `v*` directories of `-ToolkitBase`, newest first) holding
`nvvm\bin\nvvm64_40_0.dll` or `nvvm\bin\x64\nvvm64_40_0.dll` together with
`nvvm\libdevice\libdevice.10.bc`; `PROPSTRUCT_NO_CUDA` and `PROPSTRUCT_LEGACY_DIR`
unset; no `Runner.Listener.exe` outside the runner's own directory that is not an
ancestor of the script's own process (the listener running the job is the runner's own
wherever it is installed); no `dotnet`,
`testhost`, `python` or `propstruct` process among the compute applications of
`nvidia-smi`. Exit 0, or 1 after printing `::error::-` and one line per missing item.

## test_check_packages.py ✅

`python -X utf8 .github/scripts/test_check_packages.py`: the proof that the check is
non-degenerate (`BOOT.md`, "Acceptance criteria"): the content check on synthetic
packages, with no dotnet and no build; the comparison of two `results.m` through the
scratch program, which needs dotnet and builds the Harness.

## test_check_release.py ✅

`python -X utf8 .github/scripts/test_check_release.py`: the proof that the release check
is non-degenerate, on synthetic files and on the tree's own `CHANGELOG.md`; no dotnet,
no git, no network.

## test_preflight.py ✅

`python -X utf8 .github/scripts/test_preflight.py`: the proof that the preflight is
non-degenerate, on a stub `nvidia-smi`, a scratch toolkit and runner directory and a
real foreign `Runner.Listener.exe` process; Windows PowerShell 5.1 only, and its cases
are not run on any other system.
