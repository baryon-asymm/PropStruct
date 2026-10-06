# API.md — Fixtures.Tests

A test project; it exposes nothing but its tests.

## Categories after the delivery ✅

Stage S2 of the delivery (root `BOOT.md`, `## Delivery`; `tools/legacy`) moved the
original's program out of the repository on 2026-10-04; since then:

| Fact | Category | Reads |
|---|---|---|
| `LegacyArchiveTests.LegacyMatchesTheArchiveByteForByte`, which runs `generate.py verify`, the check of `Legacy/` against the archive and of the three derived fixtures against a regeneration (`derive --check`) | `Legacy` | the archive, the source and the executable, through `legacy.py` |
| `OriginalGateTests.TheOriginalIsTheRecordedOne` (the gate) | `Legacy` | `legacy.py check`: the five files against `original.sha256` |
| `OriginalGateTests.TheForbiddenListIsTheOneTheArchiveGenerates` | `Legacy` | `legacy.py forbidden --check`: the committed `tools/legacy/forbidden.sha256` against a regeneration from the archive |
| `OriginalGateTests.TheTreeHoldsNothingOfTheOriginalBeyondTheQuotationRule` (the full scan) | `Legacy` | `scan.py .` from the tree root, the original present |
| `LegacyManifestTests` | fast | every file under `tests/Fixtures/Legacy` against `archive-members.sha256`, both ways; every executable digest of `provenance.json` and of `cycle-plane-pairs/provenance.json` against `tools/legacy/original.sha256` |
| `ProvenanceTests.OutputAndExecutableHashesMatchTheFilesOnDisk` | fast | the outputs on disk; the executable's digest against the manifest, not the file |

## Out of scope

- What `Fixtures` guarantees: its own `API.md`.
