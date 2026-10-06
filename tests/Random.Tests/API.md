# API.md — Random.Tests

A test project; it exposes nothing but its tests and its approved snapshots
`Snapshots/draws.approved.txt` (Original layout) and
`Snapshots/independent-draws.approved.txt` (Independent layout).

## Categories after the delivery ✅

Stage S2 of the delivery (root `BOOT.md`, `## Delivery`; `tools/legacy`) moved the
original's program out of the repository on 2026-10-04; since then:

| Fact | Category | Reads |
|---|---|---|
| `SourceLimbsTests`, `ArithmeticTests`, `DrawStreamTests` (the multiplier and seed limbs) | fast, but `DrawStreamTests.MillionDrawsMatchTheBigIntegerStepper`, which is `Long` | `tests/Fixtures/cases/random/source_limbs.json`; no fact of this node reads a file of the original |
| `PythonSeedArithmeticCrossCheckTests` | fast | the `print-states` entries of two scripts, which need no file of the original |

## Out of scope

- What `Random` guarantees: its own `API.md`.
