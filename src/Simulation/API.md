# API.md — Simulation

Namespace `PropStruct.Simulation`. The simulator, its options and its result. Public.

## Simulator ✅

```csharp
public enum ExecutionMode { Reference, Batched }

/// <summary>Random's own StreamLayout is internal (Random/API.md); this is the public mirror SimulationOptions exposes (BOOT.md, "Result field mapping").</summary>
public enum StreamLayout { Original, Independent }

/// <summary>
/// Particle's own PrecisionKind is internal (Particle/API.md, "## Precision kind"); this is the public
/// mirror SimulationOptions exposes, the same fix as StreamLayout above (BOOT.md, "Result field mapping").
/// `Original` reproduces the accumulators (Particle's), the setup plane (`Statistics`' own,
/// `src/Statistics/BOOT.md`, "## Setup plane", implemented 2026-09-23) and the per-cycle plane
/// (`Statistics`' own, "## Report", implemented 2026-10-01).
/// </summary>
public enum PrecisionKind { Binary64, Original }

public sealed record SimulationOptions
{
    public ModelParameters Parameters { get; init; } = ModelParameters.Default;
    public ExecutionMode Mode { get; init; } = ExecutionMode.Batched;
    public AcceleratorKind Accelerator { get; init; } = AcceleratorKind.Auto;
    public int? BatchSize { get; init; }                        // null: the whole cycle
    public long RecordBudgetBytes { get; init; } = 2L << 30;
    public int AttemptsPerLaunch { get; init; } = SimulationDefaults.AttemptsPerLaunch;
    public long MaxAttemptsPerParticle { get; init; } = SimulationDefaults.MaxAttemptsPerParticle;
    public int NeighbourBudget { get; init; } = SimulationDefaults.NeighbourBudget;
    public StreamLayout Streams { get; init; } = StreamLayout.Independent; // Original is sequential only (see "## Errors")
    public PrecisionKind Precision { get; init; } = PrecisionKind.Binary64;
    public ulong Seed { get; init; }                            // 0: the layout's initial states
    public bool ContinuedStreams { get; init; }                 // batched, batch size 1 only: the degenerate configuration
}

public sealed class Simulator : IDisposable
{
    public static Simulator Create(SimulationOptions options);
    public AcceleratorInfo Accelerator { get; }
    public SimulationResult Run(Formulation formulation);
    public SimulationResult Run(Formulation formulation, IProgress<CycleProgress> progress);
    public SimulationResult Run(Formulation formulation, IProgress<CycleProgress>? progress,
                                CancellationToken cancellationToken);
    public void Dispose();
}

public readonly record struct CycleProgress(int Cycle, int Cycles, long AcceptedParticles);

public enum RunStatus
{
    Ok, AttemptCapExceeded, NeighbourBudgetExceeded, BridgeDrawBudgetExceeded, IndexOutOfRange,
    AcceleratorUnavailable, InvalidSetup, CategoryCountExceedsCapacity,
}

/// <summary>
/// Which precondition of Statistics.Setup.Prepare failed (the public mirror of Statistics.SetupStatus,
/// which is internal to the tree; the same fix as PrecisionKind/StreamLayout). See SetupFailureReason.cs.
/// </summary>
public enum SetupFailureReason
{
    InvalidCoefficients, InvalidCellSize, InvalidMinimumSize, InvalidFractionBounds,
    ZeroFractionShare, InvalidPocketFormingFractionCount, NoPocketFormingFraction,
    InvalidTailProbability, InvalidOxidizerFraction, InvalidNnWindow, NoActiveFraction,
}

public sealed class SimulationFailedException : Exception
{
    public RunStatus Status { get; }
    public SetupFailureReason? SetupFailure { get; }
    public SimulationFailedException();
    public SimulationFailedException(string message);
    public SimulationFailedException(string message, Exception innerException);
    public SimulationFailedException(RunStatus status);
    public SimulationFailedException(RunStatus status, string message);
    public SimulationFailedException(RunStatus status, string message, SetupFailureReason setupFailure);
}
```

⚠ 2026-09-24: `Pockets.Qdokso` was `double[,]`, now `ImmutableArray<ImmutableArray<double>>`
(root `BOOT.md`, "Language and build": `AnalysisMode=All`, no multidimensional array
anywhere — CA1814 — and no raw array on a public record property — CA1819). The JSON
round trip (`Output`'s own `ResultsJson`) needed no custom converter for the old
`double[,]` shape once this moved: `System.Text.Json` already serializes
`ImmutableArray<ImmutableArray<double>>` as nested arrays natively, row by row, the
same shape the removed converter produced by hand.

⚠ 2026-09-24: `SimulationFailedException` gained the three standard exception
constructors (CA1032), each leaving `Status` at `RunStatus.Ok` (the enum's own zero
value); the two status-carrying constructors are unchanged.

⚠ 2026-09-24, later the same day: the CA1720 finding above is resolved, not merely
reported. `PrecisionKind.Double` is renamed to `PrecisionKind.Binary64` across every
node that named it, `tests/Simulation.Tests/StatisticalCriterionTests.cs` included
(coding mode across the tree, owner's decision, `tests/precision-kind-rename-2026-09-24.txt`):
the cross-node scope that made a coding-mode fix impossible from this node alone is
exactly what that task grants. `## Constraints`'s own "declared exception" paragraph is
withdrawn below.

⚠ 2026-09-26 (architecture audit finding R1): `SimulationFailedException` gains
`SetupFailureReason? SetupFailure` and the constructor that sets it, so a public caller
can tell which precondition of `Statistics.Setup.Prepare` failed without reading
`Statistics.SetupStatus`, which stays internal (`RunStatus.cs`'s own remarks;
`SetupFailureReasonMapping.ToSetupFailureReason()` is the one place the two meet).
`Simulator.Run` is the only place that raises the new constructor. The other combination
refusals that also reuse `RunStatus.InvalidSetup` (precision kind, stream layout, both
now checked by the internal `SimulationOptionsValidator` before `Simulator.Create`
returns) leave `SetupFailure` `null`: they are not a setup precondition, and no member of
this enum names them.

`SimulationDefaults` (the measured budget constants), the internal init property
`SimulationOptions.PocketRedrawBudget`, and the internal `SimulationOptionsValidator`
(every option-combination rule, `Cli`'s own `InternalsVisibleTo`) are BOOT.md's own "##
Budget measurement", "Result field mapping" and "## Invariants"; none is part of this
node's public surface.

**Precision kind, setup plane** (implemented 2026-09-23): `Simulator.Run` passes
`_options.Precision.ToParticle()` into `Statistics.Setup.Prepare`, so
`PrecisionKind.Original` now also reproduces the setup plane's REAL*4 storage
(`src/Statistics/BOOT.md`, "## Setup plane"), not the accumulators alone.

**Precision kind, per-cycle plane** (implemented 2026-10-01): `Statistics` reads the kind
`Setup.Prepare` stored in `ModelSetup.Kind`, so `PrecisionKind.Original` also reproduces
the per-cycle plane (`src/Statistics/BOOT.md`, "## Report", "The per-cycle plane under
`Original`"); `Simulator` passes nothing new.

## Setup hand-off ✅

`Statistics.Setup.Prepare` computes everything of the setup that is pure `double`; the one
step it cannot take by itself is the tail draw of `PARAM`, which maps a drawn pair through
`Particle.SizeLaw.Sample` to `Dmax`. The simulator, which owns the accelerator through
`Execution` and therefore the buffers, evaluates that call (`Engine.SampleSize`,
`src/Execution/API.md`) and hands the result back:
`Prepare` yields the draw, the simulator samples, `Statistics` completes the echoes. No
other node builds `ArrayView`s for `Statistics`, and `Statistics` never creates an
accelerator (decided 2026-09-18 after the audit of `Statistics`; the exact two-phase
signatures are `Statistics`' own, `src/Statistics/API.md`).

## Result ✅

```csharp
public sealed record SimulationResult(
    RunHeader Header, Counters Counters, GeneratorAccuracy GeneratorAccuracy,
    ParticleSizes ParticleSizes, LocalStructure LocalStructure, Pockets Pockets,
    MassFractions MassFractions, Agglomerates Agglomerates, Histograms Histograms,
    Convergence? Convergence, RunDiagnostics Diagnostics, StoredSetup StoredSetup);

public sealed record RunHeader(
    string FormulationName, int Cycles, int ParticlesPerCycle,
    double Dokm, double Doksd, double Ddokmax, double Dmax, double TailProbabilityModified, double Ggg,
    ImmutableArray<double> Zx);

public sealed record Counters(
    long Qkss, long Nfx, long Nfy, long Nfz, long Nfq, long Nfw, ImmutableArray<long> Conditions,
    long IbridgeTotal, double JammedTotal, double NnTotal);

public sealed record GeneratorAccuracy(
    double Eps1, double Eps2, double Eps3, double Eps4, double Eps5, double Eps6, double Eps7,
    double Epsx1, double Epsx2, double Epsx3, ImmutableArray<double> Epsalldok,
    double Epsy, double Epsmd4, double Epsmd3, bool OxidizerAccuracyWarning, bool PocketAccuracyWarning);

public sealed record ParticleSizes(double Dok43b, double Dok43s, double Alldok43, double Alldok432);

public sealed record LocalStructure(double Gdokleft, double Vdokleft, double Plotsmdok, double Plotsm, double Mp);

public sealed record Pockets(
    double Dp43, double D432, double Sdevp43, double Dqkarm,
    double Dkarm43Cor, double Sdevp43Cor, double DqkarmCor, double Dfmk432, double Sdevp243,
    double DpMax, double DpMaxCor,
    int DpRow, ImmutableArray<double> Dpockets, ImmutableArray<double> Dokp43, ImmutableArray<double> Qdokkarm,
    ImmutableArray<ImmutableArray<double>> Qdokso);

public sealed record MassFractions(double DolM1, double DolM2, double DolM3);

public sealed record Agglomerates(
    double Dqmkm1, double Dqmkm2, double Qmcoef, int CoefNmax, int Qmkm1Nmax, int Qmkm2Nmax);

public sealed record Histograms(
    ImmutableArray<double> Allvdokso, ImmutableArray<double> Vkso, ImmutableArray<double> Qks1,
    ImmutableArray<double> FmkarmCorNormalized, ImmutableArray<double> Fmkarm2Normalized,
    ImmutableArray<double> FqkarmCorNormalized, ImmutableArray<double> Qmkm1Normalized,
    ImmutableArray<double> Qmkm2Normalized, ImmutableArray<double> CoefNormalized,
    ImmutableArray<double> Pdoksmall);

public sealed record Convergence(
    ImmutableArray<double> ConvergenceEpsy, ImmutableArray<double> ConvergenceEpsmd3, ImmutableArray<double> ConvergenceEpsmd4,
    ImmutableArray<double> ConvergenceAlldok43, ImmutableArray<double> ConvergenceAlldoksd, ImmutableArray<double> ConvergenceDolM2);

public sealed record RunDiagnostics(
    ExecutionMode Mode, int BatchSize, int AttemptsPerLaunch, AcceleratorInfo Accelerator, StreamLayout Streams,
    PrecisionKind Precision, ulong Seed, bool ContinuedStreams, long TotalAttempts, long TotalLaunches, TimeSpan Elapsed);
```

Every field of `Statistics.CycleReport` (the last cycle's own) is reachable by name
somewhere in this object graph; the record that carries each one, and the two
departures from the API.md sketch this coding session made (the public `StreamLayout`
mirror, the internal-only `PocketRedrawBudget`), are BOOT.md, "## Result field
mapping". `RunDiagnostics.BatchSize` is 1 in reference mode and otherwise the batch size
the run used, `min(BatchSize ?? N, MaxBatchSize)`. `Convergence` is `null` when the formulation's `KXX == 1` (nothing to show a
series of one point against); its six fields broaden `CycleReport`'s own scalar
`double` to a per-cycle `ImmutableArray<double>`, the one field that is not a type
mirror, matched by name only in the reflection test of `tests/Simulation.Tests`.
`RunDiagnostics.Precision` carries `SimulationOptions.Precision` unchanged, beside
`Streams`, for the same reason the stream layout is recorded there (root BOOT.md,
"Precision kind is an option of every run": "The kind is recorded in the run's
result").

Values the original computes only to print them (decided 2026-10-02 at the root,
AGENTS.md §11): six `Histograms` arrays are each the named Fortran array divided by its
own sum, in `double` under either precision kind — `Nkarm` values each for
`FmkarmCorNormalized` (`fmkarm_cor`, Fortran 1353), `Fmkarm2Normalized` (`fmkarm2`,
1357) and `FqkarmCorNormalized` (`fqkarm_cor`, 1364), `Nc` values each for
`Qmkm1Normalized` (`qmkm1`, 1370), `Qmkm2Normalized` (`qmkm2`, 1373) and
`CoefNormalized` (`coef`, 1377). `Pdoksmall` holds the `Ndok − 1` values Fortran 1386
prints, one fewer than the model's own. `Convergence` is non-null exactly when
`KXX > 1`, the gate of Fortran 1418.

⚠ 2026-09-28: `RunDiagnostics` gains `AttemptsPerLaunch` (BOOT.md, "## Budget selection
rule (2026-09-28)"), added so a changed default can be re-verified by link 3 and so
every result carries its whole determinism key (root BOOT.md, "Execution model"). It is
the *effective* per-launch attempt budget the run used: 1 in reference mode (mirroring
`BatchSize`'s own reference-mode value), `SimulationOptions.AttemptsPerLaunch`
otherwise — not the option value unconditionally, which would misreport reference
mode's own budget of 1 as whatever `AttemptsPerLaunch` happened to be set to. A JSON
`SimulationResult` written before this commit reads back with `AttemptsPerLaunch == 0`,
a value no run ever records (`System.Text.Json`'s own default for a missing `int`).

## Stored setup ✅

Decided 2026-09-24, implemented the same day (`src/Statistics/BOOT.md`, "## Setup
plane", "Design decision, 2026-09-24"). The result carries every setup-plane value
`Output` prints or scales by, as the run stored it: rounded to binary32 under
`PrecisionKind.Original`, as given under `Binary64`. `Output` reads these instead of
`Formulation` and `ModelParameters`. There is one field per generated member that
`Output` reads, named by `src/Statistics/SetupPlane.generated.txt`'s own `input` rows
(`Setup.Prepare` builds it from its internal `Statistics.SetupInputs`, `ModelSetup` and
`SetupTables`, none of them public; `Simulator.Run` is the one place that copies from
them into this public record).

```csharp
public sealed record StoredSetup(
    double CellSize, double CategoryStep, double Dmin,
    double OxidizerDensity, double PropellantDensity, double OxidizerMassFraction, double MetalMassFraction,
    double Alpha, double NnMin, double NnMax, double PocketCoefficient, double BridgeCoefficient,
    double HomogenizedOxidizerFraction, double EpsDok,
    ImmutableArray<double> FractionMassShares, ImmutableArray<double> FractionBounds);
```

`Alpha`, `NnMin`, `NnMax`, `PocketCoefficient` and `BridgeCoefficient` also feed the
attempt plane through `Particle.ModelSetup` (internal to the tree; this node already has
that access, `Particle/API.md`'s own `InternalsVisibleTo`) — `StoredSetup` carries the
same rounded value a second time, for `Output`, which has no reach to `ModelSetup` at
all. `CellSize`, `CategoryStep` and `Dmin` are the same duplication, already true before
this date. `MetalMassFraction`, `OxidizerDensity`, `PropellantDensity`,
`OxidizerMassFraction` (the raw menu/formulation value printed as `Gdok =`, not
`RunHeader.Ggg`, a different, derived value), `HomogenizedOxidizerFraction` and `EpsDok`
reach no attempt-plane field and are carried only here and in `Statistics`' own internal
`SetupInputs` (`src/Statistics/API.md`).

`FractionMassShares` holds the `NMM` values the original prints as `Gfr` (Fortran 1201)
and `FractionBounds` the `2·NMM` it prints as `Dfr` (1202), each fraction's lower and
upper bound in turn (decided 2026-10-02 at the root, with "## Result").

## Errors

| Situation | Behaviour |
|---|---|
| a failed batch status other than `ParticleNotRun` | `SimulationFailedException` with the status |
| `Execution`'s `ParticleNotRun` (a launch skipped a particle) | `InvalidOperationException` from `Simulator.Run`, no `RunStatus`: a defect of the engine, never an outcome of the run (decided 2026-10-02) |
| a failed precondition of `Statistics.Setup.Prepare` | `SimulationFailedException` with `RunStatus.InvalidSetup` and `SetupFailure` set to the matching `SetupFailureReason` (architecture audit finding R1, 2026-09-24) |
| `Precision == PrecisionKind.Original` with `Mode == ExecutionMode.Batched`, by any route to batched execution (a plain batch or the degenerate `BatchSize == 1`/`ContinuedStreams` configuration) | `Simulator.Create` throws `SimulationFailedException` with `RunStatus.InvalidSetup`, before any setup or accelerator work exists (`SimulationOptionsValidator`, architecture audit finding R1, 2026-09-24), reused rather than a new status value (decided with the root, 2026-09-21: the message names both halves of the conflict, and a consumer that already reports `InvalidSetup` by echoing the message needs no new case); `SetupFailure` is `null` — this is not a setup precondition |
| `Streams == StreamLayout.Original` with `Mode == ExecutionMode.Batched`, by any route to batched execution (a plain batch or the degenerate `BatchSize == 1`/`ContinuedStreams` configuration) | as above, `Simulator.Create` throws the same way and for the same reason (decided with the root, 2026-09-21, root BOOT.md "Two stream layouts": the message names both halves of the conflict); `SetupFailure` is `null` |
| invalid options (non-positive budgets, batch size, the `ContinuedStreams` combination) | `ArgumentException` before any work, from `Simulator.Create`'s own `SimulationOptionsValidator` |

## Side effects

None beyond the accelerator's.

## Out of scope

- Reading and writing files: `Input`, `Output`.
