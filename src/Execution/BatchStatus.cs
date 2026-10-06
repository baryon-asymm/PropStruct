namespace PropStruct.Execution;

/// <summary>How a batch (or a reference-mode particle) ended (API.md, "Engine").</summary>
internal enum BatchStatus
{
    /// <summary>Every particle of the batch was accepted; its record was folded into the real totals.</summary>
    Ok,

    /// <summary>A particle's total attempt count reached <c>maxAttemptsPerParticle</c> without being accepted.</summary>
    AttemptCapExceeded,

    /// <summary>An attempt's distance draws exceeded <c>ModelSetup.NeighbourBudget</c> (<see cref="Particle"/>'s own budget).</summary>
    NeighbourBudgetExceeded,

    /// <summary>An attempt's pocket-size redraws exceeded <c>ModelSetup.PocketRedrawBudget</c>.</summary>
    BridgeDrawBudgetExceeded,

    /// <summary>An attempt's <c>DM</c> search left its window layout (a guard, not a model branch).</summary>
    IndexOutOfRange,

    /// <summary>The engine cannot run: a CUDA engine was requested but is unavailable, or reference mode was
    /// asked of a non-CPU engine (API.md, "Errors").</summary>
    AcceleratorUnavailable,

    /// <summary>
    /// <c>ModelSetup.Kind</c> is <c>Particle.PrecisionKind.Original</c> and the call was
    /// <see cref="Engine.RunBatch"/> or <see cref="Engine.RunContinuedBatch"/> (root BOOT.md, "Precision
    /// kind is an option of every run": "`Original` accumulation is sequential only"). This node's own
    /// batch-fold path (<c>Engine.RunParticlesCore</c>) has no order-preserving, true-magnitude-seeded
    /// accumulation the way <see cref="Engine.RunReferenceParticle"/> does — it zero-seeds every particle's
    /// record and folds once by ordinary addition — so it cannot correctly reproduce binary32 accumulation
    /// at any particle count; the call is refused before any attempt runs, including the degenerate
    /// one-particle, continued-streams configuration a <see cref="Engine.RunContinuedBatch"/> call is, since
    /// that configuration's equivalence to reference mode is a claim about <c>PrecisionKind.Binary64</c>
    /// only. This is Execution's own mechanism guard (architecture audit finding R1, 2026-09-24: the
    /// combination is also, and primarily, refused upstream by <c>Simulation.SimulationOptionsValidator</c>
    /// before this node is ever reached through the sanctioned path — this status stays as the defensive
    /// backstop for a caller that reaches <see cref="Engine"/> directly). <see cref="Engine.RunReferenceParticle"/>
    /// never returns this: reference mode is sequential by construction and needs no such refusal.
    /// </summary>
    OriginalPrecisionRequiresReferenceMode,

    /// <summary>
    /// A launch left an active particle's attempt count unchanged: the kernel increments it before every
    /// attempt it runs, so the particle was never run (a dispatch that skipped it) and its outcome byte,
    /// zero (<c>AttemptOutcome.Accepted</c>) at the first launch and stale at a relaunch, is not evidence of
    /// anything. The batch ends at once, with no fold and no relaunch; it reports a defect of this engine,
    /// never an outcome of the model (decided 2026-10-02). Returned by <see cref="Engine.RunBatch"/> and
    /// <see cref="Engine.RunContinuedBatch"/> only.
    /// </summary>
    ParticleNotRun,
}
