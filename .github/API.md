# API.md — .github

What the directory provides to the tree: two workflows, the composite actions and
scripts they share, and a declaration file for actionlint. It declares no C# surface.
Designed on 2026-10-04 and built the same day by stages S3 and S4 of the delivery; what
remains unproven is a run of each workflow on GitHub (stages S6 and S7,
`ACCEPTANCE.md`).

## Workflows ✅

| File | Triggers | Jobs, in the order of `needs` |
|---|---|---|
| `workflows/ci.yml` (name `CI`) | every push, every pull request, manual dispatch | `build` on `windows-latest`: `actions/hosted-checks` (lint, tool self-tests, `defect_report.py --check`, `scan.py --hashes`, Release build, script self-tests, fast set), then `check_packages.py all` (packing, package-content check, the packed tool's approved example, the sample against the package), the packages uploaded |
| `workflows/release.yml` (name `Release`) | a tag `v*`, manual dispatch | `check` (`ubuntu-latest`), `hosted` (`windows-latest`), `gpu` (`[self-hosted, windows, gpu]`), `pack` (`windows-latest`), `publish` (`ubuntu-latest`, nuget.org, environment `release`, on a push only), `github-release` (`ubuntu-latest`, on a push only) |

A manual dispatch of `release.yml` is the rehearsal: it runs `check` to `pack` and never
`publish` or `github-release`. A tag `v<version>` must equal the packed version, checked
by `scripts/check_release.py`.

## Composite actions ✅

| Action | Used by | What it does |
|---|---|---|
| `actions/preflight` | `gpu` | runs `scripts/preflight-windows.ps1`: asserts what the self-hosted GPU runner must provide and names every missing item |
| `actions/runner-diagnostics` | every job but `publish` | prints the CPU model and the logical core count; no input |
| `actions/hosted-checks` | `build` of `ci.yml`, `hosted` of `release.yml` | the protocol lint, the self-tests of the tools and the scripts, `defect_report.py --check`, `scan.py --hashes`, the Release build and the fast set; input `diagnostics-name`, the artifact that holds the bit-snapshot files of a failed run |

## Scripts ✅

| Script | Run by | What it does |
|---|---|---|
| `scripts/check_release.py` | `check` | the release check of `BOOT.md`; three commands, `notes`, `message` and `run`, that read files; exit 0 good, 1 a failed check named on standard error, 2 a usage error ([scripts/API.md](scripts/API.md)) |
| `scripts/check_packages.py` | `build`, `pack` | `pack`, `verify`, `selftest`, `tool`, `sample`, `all`: the package-content check, the packed tool's approved example and the sample against the package |
| `scripts/preflight-windows.ps1` | `actions/preflight` | the preflight of `BOOT.md` under Windows PowerShell 5.1: exit 0 when every item is present, else 1 with one `::error::- ` line per missing item; parameter `-ToolkitBase`, the directory whose `v*` children are the CUDA toolkits, by default under `%ProgramFiles%` |
| `scripts/test_check_release.py`, `scripts/test_check_packages.py`, `scripts/test_preflight.py` | `hosted-checks` | the red and right proofs of the three scripts above |

`actionlint.yaml` declares the runner label `gpu` of the self-hosted job to actionlint.
