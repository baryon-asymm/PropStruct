namespace PropStruct.Simulation;

/// <summary>
/// Every way a run can fail (API.md, "## Simulator"): the batch statuses of <c>Execution.BatchStatus</c>
/// (<see cref="AttemptCapExceeded"/>, <see cref="NeighbourBudgetExceeded"/>,
/// <see cref="BridgeDrawBudgetExceeded"/>, <see cref="IndexOutOfRange"/>,
/// <see cref="AcceleratorUnavailable"/>), <c>Statistics.SetupStatus</c> other than <c>Ok</c> collapsed to
/// <see cref="InvalidSetup"/> (root BOOT.md, "Failures are values": the precondition that failed is not
/// this node's own concern to repeat — a caller who needs it reads <c>SetupStatus</c> from
/// <c>Statistics</c>' own contract), and <c>Statistics.CategoriesStatus.CategoryCountExceedsCapacity</c>.
/// </summary>
public enum RunStatus
{
    /// <summary>The run completed every cycle; no other member of this enum applies.</summary>
    Ok,

    /// <summary>An attempt exhausted <c>SimulationOptions.MaxAttemptsPerParticle</c> without being accepted (<c>Execution.BatchStatus</c>).</summary>
    AttemptCapExceeded,

    /// <summary>An attempt's neighbour loop exceeded <c>SimulationOptions.NeighbourBudget</c> (<c>Execution.BatchStatus</c>).</summary>
    NeighbourBudgetExceeded,

    /// <summary>An attempt exceeded its budget of bridge draws (<c>Execution.BatchStatus</c>).</summary>
    BridgeDrawBudgetExceeded,

    /// <summary>A per-particle scratch or accumulator index fell outside its array's bounds (<c>Execution.BatchStatus</c>).</summary>
    IndexOutOfRange,

    /// <summary>The requested accelerator kind could not be created, and no fallback applies (<c>Execution.BatchStatus</c>).</summary>
    AcceleratorUnavailable,

    /// <summary>
    /// The formulation and parameters failed a precondition of <c>Statistics.Setup.Prepare</c>, or the
    /// options themselves conflict (API.md, "## Errors": the <c>Original</c> precision kind or the
    /// <c>Original</c> stream layout combined with batched mode). <c>Statistics.SetupStatus</c>'s own
    /// value, when the cause was a setup precondition, is not repeated here (root BOOT.md, "Failures
    /// are values"); a caller who needs it reads <c>SetupStatus</c> from <c>Statistics</c>' own contract.
    /// </summary>
    InvalidSetup,

    /// <summary>The category merge's initial row count exceeded its array capacity (<c>Statistics.CategoriesStatus</c>).</summary>
    CategoryCountExceedsCapacity,
}

/// <summary>A run ended with a status other than <see cref="RunStatus.Ok"/> (API.md, "## Errors").</summary>
public sealed class SimulationFailedException : Exception
{
    /// <summary>The status the run ended with; <see cref="RunStatus.Ok"/> when this exception was built by one of the standard constructors (CA1032) rather than by a failed run.</summary>
    public RunStatus Status { get; }

    /// <summary>
    /// Which precondition of <c>Statistics.Setup.Prepare</c> failed, when <see cref="Status"/> is
    /// <see cref="RunStatus.InvalidSetup"/> for that reason; <see langword="null"/> otherwise, including
    /// for the two option-combination refusals that also reuse <see cref="RunStatus.InvalidSetup"/>
    /// (`SimulationOptionsValidator`'s own precision-kind and stream-layout checks), which are not a setup
    /// precondition and have no member of <see cref="SetupFailureReason"/> to name (architecture audit
    /// finding R1, 2026-09-24: "a public mirror of the setup failure on <see cref="SimulationFailedException"/>,
    /// so a public caller can tell which precondition failed without an internal type", the same pattern as
    /// <see cref="PrecisionKind"/> and <see cref="StreamLayout"/>).
    /// </summary>
    public SetupFailureReason? SetupFailure { get; }

    /// <summary>The standard parameterless constructor (CA1032); <see cref="Status"/> is <see cref="RunStatus.Ok"/>.</summary>
    public SimulationFailedException()
    {
    }

    /// <summary>The standard message-only constructor (CA1032); <see cref="Status"/> is <see cref="RunStatus.Ok"/>.</summary>
    public SimulationFailedException(string message)
        : base(message)
    {
    }

    /// <summary>The standard message-and-inner-exception constructor (CA1032); <see cref="Status"/> is <see cref="RunStatus.Ok"/>.</summary>
    public SimulationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The constructor a failed run's own status raises, with a message generated from <paramref name="status"/>.</summary>
    public SimulationFailedException(RunStatus status)
        : base($"The run ended with status {status}.")
    {
        Status = status;
    }

    /// <summary>As <see cref="SimulationFailedException(RunStatus)"/>, with <paramref name="message"/> naming the specific conflict (API.md, "## Errors") instead of the generated one.</summary>
    public SimulationFailedException(RunStatus status, string message)
        : base(message)
    {
        Status = status;
    }

    /// <summary>
    /// As <see cref="SimulationFailedException(RunStatus, string)"/>, additionally carrying
    /// <paramref name="setupFailure"/> for a caller who wants the specific precondition without reading an
    /// internal type (architecture audit finding R1, 2026-09-24). <see cref="Simulator"/> is the one place
    /// that raises this constructor, from <c>Statistics.SetupStatus.ToSetupFailureReason()</c>.
    /// </summary>
    public SimulationFailedException(RunStatus status, string message, SetupFailureReason setupFailure)
        : base(message)
    {
        Status = status;
        SetupFailure = setupFailure;
    }
}
