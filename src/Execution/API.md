# API.md — Execution

Namespace `PropStruct.Execution`. Accelerator choice and description are public; the
engine is internal to the tree (`InternalsVisibleTo` for `Simulation` and its tests).

⚠ 2026-09-24: was also granted to `Cli` and `Cli.Tests`; the architecture audit (finding
R4) found neither reached an internal member of this node, and the grants were removed
after the solution built and the fast test suite passed without them
(`src/Execution/PropStruct.Execution.csproj`).

## Accelerators ✅

```csharp
public enum AcceleratorKind { Auto, Cpu, Cuda }

public sealed record AcceleratorInfo(
    AcceleratorKind Kind, string Name, long MemoryBytes, bool LibDeviceLinked,
    string? CudaSkippedBecause);          // null unless Auto fell back to the CPU, or Cuda was refused

public static class AcceleratorProbe
{
    public static IReadOnlyList<AcceleratorInfo> Discover(Func<string, string?>? environment = null);
}
```

`Discover` lists the accelerators this process could actually bind to right now: the CPU
accelerator always, and CUDA only when the kill switch is not set and a device, libnvvm
and libdevice are all found — the same rule an engine created with `Cuda` or `Auto`
applies. It holds neither open. `environment` is the same injection seam as
`Engine.Create`'s own (⚠ 2026-09-18, the Opus audit, added alongside it): `null` (every
production caller) keeps the real process environment.

## Engine ✅

```csharp
internal enum BatchStatus
{
    Ok, AttemptCapExceeded, NeighbourBudgetExceeded, BridgeDrawBudgetExceeded,
    IndexOutOfRange, AcceleratorUnavailable, OriginalPrecisionRequiresReferenceMode,
    ParticleNotRun,
}

internal readonly record struct BatchCounters(long Attempts, int Launches, int Qks1Refreshes);

internal sealed class Engine : IDisposable
{
    public static Engine Create(AcceleratorKind kind, long recordBudgetBytes, int? cpuThreads = null,
                                Func<string, string?>? environment = null, bool forceIlgpuKernelsOnCpu = false);
    public AcceleratorInfo Accelerator { get; }

    // Setup hand-off (src/Simulation/API.md): Particle's SizeLaw.Sample on the host
    // thread over CPU memory, for the tail draw of PARAM.
    public void SampleSize(int sizeLaw, int fractionCount, double[] bounds, double[] cumulative,
                           double x, double x1, out double diameter, out int fraction);

    // Run-scoped state.
    public void Load(in ModelSetup setup, double[] bounds, double[] cumulative, byte[] pocketForming);
    public int MaxBatchSize { get; }                                   // after Load
    public void ReadTotals(long[] integerTotals, double[] realTotals);
    public void WriteTotals(long[] integerTotals, double[] realTotals);

    // Cycle inputs; QKS1 is the engine's own (BOOT.md, "QKS1 is the engine's").
    public void SetCycle(int cycleFlag, double dmaxxx, double[] pdoksmall);

    public BatchStatus RunBatch(StreamLayout layout, ulong seed, ulong firstOrdinal, int particleCount,
                                int attemptsPerLaunch, long maxAttemptsPerParticle,
                                out BatchCounters counters);

    public BatchStatus RunContinuedBatch(ref StreamSet streams, int attemptsPerLaunch,
                                         long maxAttemptsPerParticle, out BatchCounters counters);

    public BatchStatus RunReferenceParticle(ref StreamSet streams, long maxAttemptsPerParticle,
                                            out BatchCounters counters);

    // Added in the coding session, beyond the design sketch (AGENTS.md §4): the L0 probe
    // requirement of Execution.Tests/BOOT.md named a kernel the sketch gave no member to
    // call. Internal, not a second public surface: MathProbe/Kernels.ProbeMath are
    // internal already.
    public double[] ProbeMath(double[] inputs);
}
```

Semantics:

- `Load` must precede every call but `SampleSize`; it resets the totals to zero and QKS1
  to `Normalize` of those zeros. `ReadTotals`/`WriteTotals` take arrays of the layout's
  `IntegerLength` and `RecordLength`. `WriteTotals` replaces the totals wholesale and
  always recomputes QKS1 from the written totals in the same call, unconditionally —
  not only when its own bookkeeping thinks the loop-completion count moved, since a
  caller of `WriteTotals` (`Simulation`'s in-place rewrite between cycles, or a test
  forking one engine's state onto another) is handing the engine totals with no
  necessary relation to whatever QKS1 last reflected, so there is nothing to compare
  against.

  ⚠ 2026-09-18, the Opus audit: `WriteTotals` once only resynced its refresh
  bookkeeping to the just-written `LoopCompletions`, without recomputing QKS1 itself;
  the next launch's own refresh check then saw "no change since last refresh" and left
  QKS1 at whatever it held before the call (found by the coordinator from a throughput
  anomaly: a launch immediately after `WriteTotals` with nonzero `LoopCompletions`
  burned its whole attempt budget with no particle accepted). This behaviour is now
  owned by `Qks1Refresher.Recompute`, called unconditionally from both `Load` and
  `WriteTotals`, so the two cannot drift apart again.
- `RunBatch` runs particles `firstOrdinal … firstOrdinal + particleCount − 1` with
  streams derived per particle; `particleCount ≤ MaxBatchSize`.
- `RunContinuedBatch` is a batch of one with the caller's streams, returned advanced;
  it takes no `layout` argument (dropped, architecture audit finding R1, 2026-09-24: its
  one reason for existing was the stream-layout refusal below, now withdrawn from this
  node entirely) — a continued batch never derived streams from a layout anyway, it only
  continues the caller's own.
- `RunReferenceParticle` runs one particle on the host thread until it is accepted; CPU
  engines only.
- `RunBatch` and `RunContinuedBatch` both refuse with `OriginalPrecisionRequiresReferenceMode`,
  before any attempt runs and regardless of the accelerator's own availability, when the
  loaded setup's `Kind` is `Particle.PrecisionKind.Original` (root BOOT.md,
  "Precision kind is an option of every run": "`Original` accumulation is sequential
  only"; BOOT.md, "`Original` accumulation is refused for batched execution, unconditionally").
  This is this node's own mechanism guard: `RunParticlesCore`'s batch-fold path has no
  order-preserving, true-magnitude-seeded accumulation the way `RunReferenceParticle` does,
  at any particle count, so it cannot correctly reproduce binary32 accumulation regardless
  of who calls it. Neither method checks `layout` any more (architecture audit finding R1,
  2026-09-24, withdrawing the twin `OriginalLayoutRequiresReferenceMode` refusal this
  document used to describe here): `Random.StreamLayout.Original` has no mechanism reason
  to fail in this node — `Engine` derives and runs its streams like any other layout's,
  correctly, just with a bias `Simulation`'s own policy declines to reproduce in batched
  mode. Both refusals — precision here, and layout upstream — are primarily enforced by
  `Simulation.SimulationOptionsValidator`, which runs before a `Simulator` (and therefore
  this engine) exists at all; the precision check here is the defensive backstop for a
  caller that reaches `Engine` directly, bypassing `Simulation` (`Engine` is internal to
  the tree, not private to this node) — layout has no such backstop, since it is not a
  mechanism failure to guard against. `RunReferenceParticle` reads `Kind` once per
  particle, but only to choose how its record is seeded and committed (BOOT.md, "Reference
  mode"), never to refuse anything: reference mode accepts either accumulation kind or
  either stream layout unconditionally, and the root's invariants do not restrict it.

  ⚠ 2026-09-21: was "`RunReferenceParticle` never checks `Kind`" — true before the record-seeding fix, false after → `src/Execution/HISTORY.md#reference-mode-record-seeding`

  ⚠ 2026-09-26 (architecture audit finding R1): was both refusals — precision and stream
  layout — enforced unconditionally in this node as "the one place in the tree either
  refusal is enforced", with `Simulation`'s own dispatch merely calling into it. Now the
  layout refusal lives only in `Simulation.SimulationOptionsValidator`, ahead of
  `Statistics.Setup.Prepare`, the tail draw and `Engine.Load`, so a caller never pays for
  that work before learning the combination is invalid; the precision refusal stays here
  too, as a genuine mechanism guard, but is likewise reached first through
  `SimulationOptionsValidator` on the sanctioned path (`src/Simulation/BOOT.md`).
- On a status other than `Ok` the run is to be abandoned; the engine stays usable for
  `ReadTotals` and `Dispose`, but the totals are not "the state at the moment of
  failure": the integer totals already include every attempt of the failing launch (the
  kernel writes their side effects unconditionally, before the host inspects the
  outcomes and decides to stop), not only the attempts up to the one that failed; the
  real totals hold nothing of the batch at all, because `Fold.Add`/`FoldField` runs only
  once every particle in the batch is accepted (`RunParticlesCore`); and
  `RunReferenceParticle` drops the failing particle's own record outright, since it
  folds only on `Accepted`. A caller that needs the failing particle's own record must
  keep it outside the engine.

  ⚠ 2026-09-18, the Opus audit: this line first claimed the totals held "the state at
  the moment of failure", which reads as a clean stopping point; it is not one, for the
  three reasons above, each confirmed by reading `RunParticlesCore` and
  `RunReferenceParticle`. No fold was added to make the claim true — a failed run is
  documented as abandoned, and adding a fold to match a wrong description would be
  scope beyond what a wording fix should do.
- `ProbeMath` runs `MathProbe`/`Kernels.ProbeMath` over `inputs`; does not require `Load`.

⚠ 2026-09-18: `Create`'s signature first had no `cpuThreads` parameter. The L2
determinism-under-thread-count acceptance criterion ("1, 4 and 16 CPU threads") needs a
way to force the CPU accelerator's own thread count, which the sketch had no member for;
`cpuThreads` is `null` for every production caller (all cores, the original behaviour)
and is otherwise `Execution.Tests`' own knob (`AcceleratorBinding.Cpu`: 1 forces
`CPUAcceleratorMode.Sequential`, any other value a `CPUDevice` sized to exactly that
count run in `CPUAcceleratorMode.Parallel`).

⚠ 2026-09-18, the Opus audit: `Create`'s signature had no `environment` parameter either.
A test that needed a specific engine's CUDA request forbidden or allowed
(`AcceleratorTests`, `MathProbeTests`, `ThroughputMeasurementTests`, `TierTableTests`)
mutated the real process's `PROPSTRUCT_NO_CUDA` for the call and reset it afterwards
(`NoCudaEnvironmentGuard`); xunit runs test classes in parallel by default, so that
mutation applied, for its whole scope, to every engine any other concurrently running
test class created too — a CUDA test elsewhere in the suite could see the kill switch
set (or cleared) by a guard it never asked for, going spuriously red or taking the
refusal branch without probing CUDA. `environment` lets a test hand `Decide` its own
`Func<string, string?>` instead, answering only that one call's own reads; `null` (every
production caller) keeps the real process environment. Every test that used
`NoCudaEnvironmentGuard` now injects instead and branches on the engine it actually got
(`Accelerator.CudaSkippedBecause`), not on the ambient `AcceleratorChoice.CudaForbidden`.

⚠ 2026-09-19 (`BOOT.md`, "Host-thread path"): `Create`'s signature gained a fifth
parameter, `forceIlgpuKernelsOnCpu` (default `false`). Batched mode and reference mode
on the CPU accelerator now run `Attempt.Run` and the other kernel-compatible methods
directly on .NET threads instead of through an ILGPU-compiled kernel launch (root
`BOOT.md`, "One particle program": the ILGPU CPU accelerator "is kept as a test oracle
of the kernel path only"). Every production caller, and almost every test, leaves this
`false`; `true` keeps the previous kernel-launch behaviour on the CPU accelerator, for
`Execution.Tests`' own oracle rows (`HostThreadOracleTests`, `HostThreadMatrixTests`)
that check the host-thread path against it. This does not change `Accelerator.Kind`
(still `Cpu` either way) or any other reported field: the choice is an internal dispatch
detail, not a different accelerator. `cpuThreads`' own meaning (the ⚠ above) is
unchanged when this is `true`; when it is `false` (the default), `cpuThreads` instead
sets `System.Threading.Tasks.ParallelOptions.MaxDegreeOfParallelism` for the host-thread
path's own `Parallel.For` calls — the same "how many CPU threads" knob, now driving a
different mechanism.

## Errors

| Situation | Behaviour |
|---|---|
| `Cuda` requested, unavailable | `Create` returns an engine whose every run call (`RunBatch`, `RunContinuedBatch`, `RunReferenceParticle`) returns `AcceleratorUnavailable`; `Accelerator` says why |
| `SampleSize` on a refused engine | runs anyway, on an ephemeral CPU accelerator: `SizeLaw.Sample` has no accelerator-specific behaviour to probe, and `Simulation` calls this before `Load`, as its first call on a freshly created engine (decided 2026-09-18, Opus audit item 10) |
| `ProbeMath` on a refused engine | throws `InvalidOperationException`: unlike `SampleSize`, this member's whole purpose is to probe the bound accelerator's own math intrinsics, and a refused engine has none to probe |
| reference mode on a non-CPU engine | `RunReferenceParticle` returns `AcceleratorUnavailable` |
| `RunBatch`/`RunContinuedBatch` with `ModelSetup.Kind == Particle.PrecisionKind.Original` | `OriginalPrecisionRequiresReferenceMode`, before any attempt runs (this node's own mechanism guard; `Simulation.SimulationOptionsValidator` refuses the same combination earlier still, before this engine exists) |
| a particle passes the attempt cap | `AttemptCapExceeded` |
| a launch leaves an active particle's attempt count unchanged (the particle was never run; its outcome byte is not evidence) | `ParticleNotRun` from `RunBatch`/`RunContinuedBatch`, at once: no fold, no relaunch; a defect of this engine, never an outcome of the model |
| an attempt ends with a failure outcome | the matching status |
| `particleCount > MaxBatchSize`, a call before `Load`, arrays of the wrong length | `ArgumentException` / `InvalidOperationException` (host-side programming errors, not model failures) |

## Side effects

Loads CUDA libraries when the GPU path is used; reads `PROPSTRUCT_NO_CUDA`; holds device
memory until `Dispose`.

## Out of scope

- The cycle loop, batch sizing policy within `MaxBatchSize`, calling `Statistics`
  between cycles: `Simulation`.
