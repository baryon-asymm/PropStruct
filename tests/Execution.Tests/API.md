# API.md — Execution.Tests

A test project; it exposes nothing but its tests and its tier table.

## Artifacts ⏳

```text
tests/Execution.Tests/GpuCpuTiers.cs      the tier table of CUDA against the CPU accelerator, with derivations
```

## Categories after the delivery ✅

Stage S2 of the delivery (root `BOOT.md`, `## Delivery`; `tools/legacy`) moved the
original's program out of the repository on 2026-10-04; no fact of this node reads one
of its five files (the formulations are data), so none carries `Category=Legacy`. Stage
S4 changed no category: the facts that run on CUDA keep the one they have (the untagged
ones are in the fast set, `TierTableTests` and `ThroughputMeasurementTests` are `Long`)
and all of them run in the release's GPU job, whose filter is `Category!=Legacy`; the
rows below are stage S4's:

| Fact | Category | Reads |
|---|---|---|
| every fact that returns when no CUDA accelerator is available | as it was, and in the release's GPU job | `PROPSTRUCT_REQUIRE_CUDA`: calls `Harness`'s `CudaRequirement.FailIfRequired`, which fails the fact when the variable is `1` |
| `CudaRequirementWiringTests` | fast | `CudaRefusalBranches.Scan` over this directory: at least one branch on `CudaSkippedBecause`, none whose refused side lacks the call |

## Out of scope

- The statistical criterion against the original: `Harness`, applied by the L2 rows of
  `Particle.Tests`, `Statistics.Tests` and `Simulation.Tests`.
