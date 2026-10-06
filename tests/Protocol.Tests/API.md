# API.md — Protocol.Tests

A test project; it exposes nothing but its tests and the approved surface snapshot.

## Artifacts ✅

```text
tests/Protocol.Tests/PublicSurface.approved.txt   snapshot of the public surface of every assembly of the tree
```

## Side effects

Writes `PublicSurface.actual.txt` beside the snapshot on mismatch and deletes it on a
match; with no approved snapshot at all, writes `PublicSurface.approved.txt` itself and
fails, asking for it to be read and committed (`SurfaceTests.cs`).

⚠ 2026-10-02: was "Writes `PublicSurface.received.txt`", a name the code never used;
found by the boot-api-protocol skill review comparing this file with `SurfaceTests.cs`.

## Facts of the delivery ✅

Stage S3 of the delivery (root `BOOT.md`, `## Delivery`) adds one fact and one proof:

| Fact | Category | Checks |
|---|---|---|
| `QuickstartExampleTests` (new) | fast | the block of the root `API.md`, "## Entry points", the two marked regions of `samples/Quickstart`, the example of `docs/nuget/PropStruct.md` and the example of `README.md` are equal after the common indentation is stripped |
| `SurfaceTests` (existing) | fast | `PublicSurface.approved.txt` is unchanged by the packaging properties of S3 |

## Out of scope

- The language-independent checks: `tools/protocol-lint`.
