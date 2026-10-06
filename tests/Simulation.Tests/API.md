# API.md — Simulation.Tests

A test project; it exposes nothing but its tests.

## Categories after the delivery ✅

Stage S2 of the delivery (root `BOOT.md`, `## Delivery`; `tools/legacy`) moved the
original's program out of the repository on 2026-10-04; since then:

| Fact | Category | Reads |
|---|---|---|
| `LineMapCoverageTests` | fast | `tests/Fixtures/cases/source/executable_lines.json`; its failure message names line numbers, never a source line |

## Out of scope

- The statistical criterion itself: `Harness`.
