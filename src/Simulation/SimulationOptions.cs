using PropStruct.Execution;
using PropStruct.Input;

namespace PropStruct.Simulation;

/// <summary>Reference mode (the original's own sequence) or batched mode (root BOOT.md, "Execution model").</summary>
public enum ExecutionMode
{
    /// <summary>Batch size 1, one attempt per launch, the six original streams continued across particles and cycles (root BOOT.md, "Reference mode is the original's sequence").</summary>
    Reference,

    /// <summary>Particles run in batches on the host or an accelerator, each drawing from its own jump-ahead streams (root BOOT.md, "Execution model"); the default.</summary>
    Batched,
}

/// <summary>
/// Options of one run (API.md, "## Simulator"). Defaults for <see cref="AttemptsPerLaunch"/>,
/// <see cref="MaxAttemptsPerParticle"/> and <see cref="NeighbourBudget"/> are measured, not chosen
/// (BOOT.md, "Design decisions (2026-09-18)", "Budget defaults are measured, not chosen"; the figures and
/// the method are recorded there).
/// </summary>
public sealed record SimulationOptions
{
    /// <summary>The fourteen model parameters the original asked for interactively (root BOOT.md, "## Purpose"); the original's own defaults unless overridden.</summary>
    public ModelParameters Parameters { get; init; } = ModelParameters.Default;

    /// <summary><see cref="ExecutionMode.Reference"/> or <see cref="ExecutionMode.Batched"/> (root BOOT.md, "Execution model"); batched is the default.</summary>
    public ExecutionMode Mode { get; init; } = ExecutionMode.Batched;

    /// <summary>Which accelerator the run's batches launch on; <see cref="AcceleratorKind.Auto"/> falls back to the CPU accelerator when CUDA is unavailable (root BOOT.md, "The CPU path needs no NVIDIA software").</summary>
    public AcceleratorKind Accelerator { get; init; } = AcceleratorKind.Auto;

    /// <summary>Particles per batch in batched mode; <see langword="null"/> means the whole cycle (capped by <see cref="RecordBudgetBytes"/> and the accelerator's own <c>MaxBatchSize</c>).</summary>
    public int? BatchSize { get; init; }

    /// <summary>The byte budget a whole-cycle batch's per-particle records may not exceed before the cycle is split into smaller batches (root BOOT.md, "Execution model"); 2 GiB by default.</summary>
    public long RecordBudgetBytes { get; init; } = 2L << 30;

    /// <summary>The per-launch attempt budget of a batch (<c>Execution.Engine.RunBatch</c>/<c>RunContinuedBatch</c>).</summary>
    public int AttemptsPerLaunch { get; init; } = SimulationDefaults.AttemptsPerLaunch;

    /// <summary>The run-level attempt cap of one particle; passing it ends the run with <see cref="RunStatus.AttemptCapExceeded"/>.</summary>
    public long MaxAttemptsPerParticle { get; init; } = SimulationDefaults.MaxAttemptsPerParticle;

    /// <summary>The neighbour-draw budget of one attempt (<c>ModelSetup.NeighbourBudget</c>).</summary>
    public int NeighbourBudget { get; init; } = SimulationDefaults.NeighbourBudget;

    /// <summary>
    /// Which of the two stream layouts of <c>Random</c> the run uses (root BOOT.md, "Two stream layouts").
    /// <see cref="StreamLayout.Independent"/> is the default (decided 2026-09-21, with the root): <see cref="StreamLayout.Original"/>
    /// is sequential only and, with <see cref="Mode"/> == <see cref="ExecutionMode.Batched"/> (this record's
    /// own default), is refused with <see cref="RunStatus.InvalidSetup"/> before any setup or accelerator
    /// work exists (BOOT.md, "`Original` layout is refused for batched execution, unconditionally";
    /// <see cref="SimulationOptionsValidator"/>, architecture audit finding R1, 2026-09-24) — so <c>Original</c>
    /// could not stay the default once <c>Batched</c> was already the default mode, on pain of every bare
    /// invocation refusing itself. A caller that wants the original's own (biased, root BOOT.md "Known bias
    /// of the original's seeds") sequence sets both <see cref="Streams"/> = <see cref="StreamLayout.Original"/>
    /// and <see cref="Mode"/> = <see cref="ExecutionMode.Reference"/> explicitly.
    /// </summary>
    public StreamLayout Streams { get; init; } = StreamLayout.Independent;

    /// <summary>
    /// Which of the two precision kinds of <c>Particle</c> the run uses (root BOOT.md, "Precision kind
    /// is an option of every run"); unaffected callers get exactly the bits they always got, since
    /// <see cref="PrecisionKind.Binary64"/> is the default. <see cref="PrecisionKind.Original"/> rounds
    /// the accumulators to binary32 after every addition and holds the setup plane's and the per-cycle
    /// plane's values as the original's REAL*4 homes hold them, reproducing its own rounding loss, and is sequential
    /// only: with <see cref="Mode"/> == <see cref="ExecutionMode.Batched"/>, by any route to batched
    /// execution (a plain batch or the degenerate <see cref="BatchSize"/> == 1 / <see cref="ContinuedStreams"/>
    /// configuration), the run is refused with <see cref="RunStatus.InvalidSetup"/> before any setup or
    /// accelerator work exists (BOOT.md, "Precision kind: sequential only, refused with a status";
    /// <see cref="SimulationOptionsValidator"/>, architecture audit finding R1, 2026-09-24). This kind
    /// covers the accumulators, the setup plane and the per-cycle plane (root BOOT.md, "Precision kind
    /// is an option of every run"; `src/Statistics/BOOT.md`, "## Setup plane" and "## Report").
    /// </summary>
    public PrecisionKind Precision { get; init; } = PrecisionKind.Binary64;

    /// <summary>0: the layout's own initial states (root BOOT.md, "Execution model").</summary>
    public ulong Seed { get; init; }

    /// <summary>
    /// Batched mode, batch size 1 only: uses reference mode's own single continued <c>StreamSet</c> through
    /// <c>Engine.RunContinuedBatch</c> — the degenerate configuration of the root invariant "Reference mode
    /// is batched mode degenerated" (BOOT.md, "Streams and ordinals").
    /// </summary>
    public bool ContinuedStreams { get; init; }

    /// <summary>
    /// The pocket-redraw budget of one bridge (<c>ModelSetup.PocketRedrawBudget</c>). Internal: the node's
    /// "Defaults" constraint names only the three budgets above as options (BOOT.md, "## Budget measurement").
    /// </summary>
    internal int PocketRedrawBudget { get; init; } = SimulationDefaults.PocketRedrawBudget;
}

/// <summary>
/// The measured budget defaults (BOOT.md, "## Budget measurement"). Kept as one named place rather than
/// inline literals on <see cref="SimulationOptions"/>'s own initializers, so the measurement's own evidence
/// and the values it produced stay next to each other in source, not only in prose.
/// </summary>
internal static class SimulationDefaults
{
    // Chosen 2026-10-01 by the pre-registered selection rule (BOOT.md, "## Budget selection rule (2026-09-28)"):
    // the largest candidate with link 3 green on every smaller one, CPU and CUDA, five formulations
    // (tests/Simulation.Tests/BOOT.md, "## Budget ladder (2026-10-01)"). Was 32, HPEPA3's 99 % rule.
    public const int AttemptsPerLaunch = 8192;

    // Measured 2026-09-19: the largest attempt count of one particle over the five reference formulations
    // was 4,412 (HMX, reference mode); 100 times that, rounded up.
    public const long MaxAttemptsPerParticle = 500_000;

    // Measured 2026-09-19: the largest neighbour-draw count of one attempt lies in (196,608, 198,656]
    // (HPEPA3); 100 times the upper end, rounded up.
    public const int NeighbourBudget = 20_000_000;

    // Measured 2026-09-19: the largest pocket-redraw count of one bridge lies in (96, 128] (HPEPA3); 100 times
    // the upper end. Internal (SimulationOptions.PocketRedrawBudget), not a public option.
    public const int PocketRedrawBudget = 12_800;
}
