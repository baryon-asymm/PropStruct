# API.md — Simulation.Tests

A test project; it exposes nothing but its tests.

## Categories after the delivery ✅

Stage S2 of the delivery (root `BOOT.md`, `## Delivery`; `tools/legacy`) moved the
original's program out of the repository on 2026-10-04; since then:

| Fact | Category | Reads |
|---|---|---|
| `LineMapCoverageTests` | fast | `tests/Fixtures/cases/source/executable_lines.json`; its failure message names line numbers, never a source line |
| `OriginalModelGuardTests` | `Long` (23 cases: the `seed0/*` ones and three slow P312 extras), fast (13: 12 cases and the file holds the guard's cases) | `Snapshots/OriginalModelGuard.approved.txt`, written by `tests/RateTableTool` |
| `CorrectedModelGuardTests` | `Long` (2 cases: the P312 ones), fast (9: 8 cases and the file holds the snapshot's cases) | `Snapshots/CorrectedModelSnapshot.approved.txt`, written by `tests/RateTableTool` (`corrected-model-snapshot`) |
| `DminCellRunTests` | fast | `tests/Fixtures/Legacy/formulations/P312.dat` |

## Category of the hand-run measurement ✅

| Fact | Category | Reads |
|---|---|---|
| `CorrectionEffectsMeasurement` (every fact) | `Long` and `Measurement` | `tests/Fixtures/Legacy/formulations`; writes `Measurements/corrections-2026-10-18/` and the run store `Measurements/cache/` (ignored by git, `PROPSTRUCT_MEASUREMENT_CACHE` moves it) |
| `CorrectionMeasurementToolingTests` | fast | nothing on disk |

`Long` keeps the measurement out of the fast set and CI, whose filter names `Long` and `Legacy`
only; `Measurement` selects it alone: `dotnet test tests/Simulation.Tests -c Release
--filter "Category=Measurement"`, or one slice, `--filter "Category=Measurement&DisplayName~HMX"`.
The full set (`dotnet test PropStruct.sln`) runs it as well. `PROPSTRUCT_MEASUREMENT_SEEDS`
sets the seed count (default 16); the seeds are `k * 2^16`, and the report refuses arms
whose seeds repeat a phase modulo 2^20 (`BOOT.md`, "## Correction-effects measurement").

## Out of scope

- The statistical criterion itself: `Harness`.
