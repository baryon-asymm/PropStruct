using PropStruct.Execution;

namespace PropStruct.Simulation;

/// <summary>
/// Every option-combination rule <see cref="Simulator.Create"/> checks before any setup, accelerator or
/// particle work exists (architecture audit finding R1, 2026-09-24: "Run-configuration policy sits in
/// Execution and surfaces through the 'run failed' channel"; API.md, "## Errors"). Collected in one place
/// so a rule is typed once: <c>Cli</c> queries <see cref="IsContinuedStreamsCombinationInvalid"/>, the one
/// rule it also checks at parse time, instead of retyping the same condition
/// (`src/Cli/CommandLine.cs`'s own <c>ParseRun</c>; granted <c>InternalsVisibleTo</c> for exactly this).
/// </summary>
/// <remarks>
/// Before this task, two of these rules - <c>PrecisionKind.Original</c> and <c>StreamLayout.Original</c>
/// combined with <see cref="ExecutionMode.Batched"/> - were enforced only inside
/// <c>Execution.Engine.RunBatch</c>/<c>RunContinuedBatch</c>, so a caller paid for
/// <c>Statistics.Setup.Prepare</c>, the tail draw and <c>Engine.Load</c> before ever learning the
/// combination was invalid. Both now fail here, before <c>Engine.Create</c> is even called, exactly like
/// every other combination rule in this class always has. <c>Execution</c> keeps only the one refusal that
/// is a genuine mechanism guard rather than a policy choice - <c>PrecisionKind.Original</c>'s own
/// accumulation has no order-preserving fold in <c>Engine</c>'s batch path, at any particle count, so that
/// refusal stays there too, as a defensive backstop for a caller that reaches <c>Engine</c> directly
/// (`src/Execution/BOOT.md`, "`Original` accumulation is refused for batched execution"). The
/// <c>StreamLayout.Original</c> refusal has no such mechanism reason - <c>Engine</c> can and does run it
/// mechanically (it was measured doing so before the policy decision, root BOOT.md, "Two stream layouts")
/// - so it is refused here only, and <c>Engine</c> no longer inspects the layout at all
/// (`src/Execution/BOOT.md`, "`Original` stream layout is refused for batched execution", withdrawn).
/// </remarks>
internal static class SimulationOptionsValidator
{
    /// <summary>
    /// Throws <see cref="ArgumentException"/> for a malformed option value or an invalid combination this
    /// node itself owns, and <see cref="SimulationFailedException"/> with <see cref="RunStatus.InvalidSetup"/>
    /// for the two batched-execution refusals (root BOOT.md, "Precision kind is an option of every run";
    /// "Two stream layouts") - reusing the same exception type and status a failed run itself throws, so a
    /// caller sees one failure shape whether the run never started or started and failed midway.
    /// </summary>
    public static void Validate(SimulationOptions options)
    {
        if (options.BatchSize is <= 0)
        {
            throw new ArgumentException("BatchSize must be positive when given.", nameof(options));
        }

        if (options.RecordBudgetBytes <= 0)
        {
            throw new ArgumentException("RecordBudgetBytes must be positive.", nameof(options));
        }

        if (options.AttemptsPerLaunch <= 0)
        {
            throw new ArgumentException("AttemptsPerLaunch must be positive.", nameof(options));
        }

        if (options.MaxAttemptsPerParticle <= 0)
        {
            throw new ArgumentException("MaxAttemptsPerParticle must be positive.", nameof(options));
        }

        if (options.NeighbourBudget <= 0)
        {
            throw new ArgumentException("NeighbourBudget must be positive.", nameof(options));
        }

        if (options.PocketRedrawBudget <= 0)
        {
            throw new ArgumentException("PocketRedrawBudget must be positive.", nameof(options));
        }

        if (options.Mode == ExecutionMode.Reference && options.Accelerator == AcceleratorKind.Cuda)
        {
            throw new ArgumentException("Reference mode needs the CPU accelerator; Cuda was requested.", nameof(options));
        }

        if (IsContinuedStreamsCombinationInvalid(options))
        {
            throw new ArgumentException(
                "ContinuedStreams is the degenerate configuration: batched mode with BatchSize == 1 only.", nameof(options));
        }

        if (options.Precision == PrecisionKind.Original && options.Mode == ExecutionMode.Batched)
        {
            throw new SimulationFailedException(RunStatus.InvalidSetup, PrecisionRefusalMessage(options));
        }

        if (options.Streams == StreamLayout.Original && options.Mode == ExecutionMode.Batched)
        {
            throw new SimulationFailedException(RunStatus.InvalidSetup, LayoutRefusalMessage(options));
        }
    }

    /// <summary>
    /// `--continued-streams`' own combination rule (BOOT.md, "Design decisions", "Streams and ordinals":
    /// batch size 1 only) - <see cref="SimulationOptions.ContinuedStreams"/> means anything only under
    /// <see cref="ExecutionMode.Batched"/> with <see cref="SimulationOptions.BatchSize"/> exactly 1. The one
    /// rule <c>Cli</c> checks at parse time too (`src/Cli/CommandLine.cs`'s own <c>ParseRun</c>), so it is a
    /// public predicate rather than folded silently into <see cref="Validate"/> alone.
    /// </summary>
    public static bool IsContinuedStreamsCombinationInvalid(SimulationOptions options) =>
        options.ContinuedStreams && (options.Mode != ExecutionMode.Batched || options.BatchSize != 1);

    /// <summary>Word for word what <see cref="Simulator"/> threw at run time before this task; unchanged so no consumer's message parsing moves.</summary>
    private static string PrecisionRefusalMessage(SimulationOptions options) =>
        $"SimulationOptions.Precision is {nameof(PrecisionKind.Original)}, which accumulates " +
        $"in an order batched execution does not fix, so it is refused with SimulationOptions.Mode = " +
        $"{options.Mode} (root BOOT.md, \"Precision kind is an option of every run\"); " +
        $"use {nameof(ExecutionMode.Reference)} instead.";

    /// <summary>Word for word what <see cref="Simulator"/> threw at run time before this task; unchanged so no consumer's message parsing moves.</summary>
    private static string LayoutRefusalMessage(SimulationOptions options) =>
        $"SimulationOptions.Streams is {nameof(StreamLayout.Original)}, whose lags cross " +
        $"particle boundaries in a way batched execution does not carry, so it is refused with " +
        $"SimulationOptions.Mode = {options.Mode} (root BOOT.md, \"Two stream layouts\"); " +
        $"use {nameof(ExecutionMode.Reference)} instead.";
}
