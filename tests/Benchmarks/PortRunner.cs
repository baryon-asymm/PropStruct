using System.Diagnostics;
using PropStruct.Input;
using PropStruct.Simulation;

namespace PropStruct.Benchmarks;

/// <summary>
/// Runs one repeat of one port path through <c>Simulation</c>, with the same options a user
/// would pass (BOOT.md, Invariants: "The port's figures come from the library, through
/// `Simulation`... not from an internal loop that skips the cycle policy"). The caller
/// prepares <paramref name="formulation"/> with <c>Cycles = 1</c> (BOOT.md, Constraints: "one
/// cycle is enough") before calling; this type only runs it and measures cycle 1's own window.
/// </summary>
internal static class PortRunner
{
    /// <summary><see cref="TotalAttempts"/>, <see cref="TotalLaunches"/> and
    /// <see cref="AttemptsPerLaunch"/> are <c>SimulationResult.Diagnostics</c>' own fields, copied
    /// verbatim, not isolated to cycle 1: <c>RunDiagnostics</c> counts the whole call, cycle 0's
    /// warm-up included, and this node has no way to split it further (the same reason the
    /// <c>original</c> row's own accepted-particle count covers the whole run, "Original's
    /// particles/s" above). <see cref="AttemptsPerLaunch"/> is <c>RunDiagnostics</c>' own budget
    /// field, added 2026-09-28 (`src/Simulation/BOOT.md`, "## Budget selection rule
    /// (2026-09-28)"): the *effective* budget the call used, 1 in reference mode, not merely the
    /// option this node passed into <paramref name="options"/> — the reason this node no longer
    /// reads <c>options.AttemptsPerLaunch</c> back for the table's own Budget column (`BOOT.md`,
    /// "## Figures", the note under the Stage 0 table).</summary>
    internal readonly record struct Cycle1Measurement(
        long AcceptedParticles, double ElapsedSeconds, bool Cycle0IsolationConfirmed,
        long TotalAttempts, long TotalLaunches, int AttemptsPerLaunch);

    public static Cycle1Measurement RunCycle1(Formulation formulation, SimulationOptions options)
    {
        var recorder = new CycleBoundaryRecorder();
        using var simulator = Simulator.Create(options);

        var runStart = Stopwatch.GetTimestamp();
        var result = simulator.Run(formulation, recorder);
        var runEnd = Stopwatch.GetTimestamp();

        var windowStart = recorder.Cycle0EndTimestamp ?? runStart;
        var elapsedSeconds = (runEnd - windowStart) / (double)Stopwatch.Frequency;

        return new Cycle1Measurement(
            formulation.ParticlesPerCycle, elapsedSeconds, recorder.Cycle0EndTimestamp.HasValue,
            result.Diagnostics.TotalAttempts, result.Diagnostics.TotalLaunches, result.Diagnostics.AttemptsPerLaunch);
    }
}
