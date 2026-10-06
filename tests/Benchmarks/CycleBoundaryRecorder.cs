using System.Diagnostics;
using PropStruct.Simulation;

namespace PropStruct.Benchmarks;

/// <summary>
/// Isolates cycle 1's own wall time from <c>Simulator.Run</c>'s total elapsed time (BOOT.md,
/// "## Cycle 1 isolation"): cycle 0 is the warm-up (root BOOT.md, "Execution model") and its
/// cost is not part of the figure this node records. The recorder keeps the timestamp of the
/// last progress report it sees for cycle 0 — the closest available approximation to "cycle 0
/// has just finished, cycle 1 is about to start" regardless of how often <c>Simulator.Run</c>
/// calls <c>IProgress&lt;CycleProgress&gt;.Report</c> within a cycle (API.md says nothing about
/// that frequency, so the recorder does not assume one report per cycle: it just keeps the
/// latest report tagged cycle 0). <see cref="PortRunner.RunCycle1"/> measures from this
/// timestamp to the moment <c>Run</c> returns.
///
/// <c>Report</c> is assumed to be called synchronously, on whichever thread completes a batch
/// or a cycle, the ordinary shape of a progress callback taken directly (not through
/// <see cref="Progress{T}"/>'s synchronization-context marshaling, which this recorder
/// deliberately avoids by implementing <see cref="IProgress{T}"/> itself); the lock only
/// protects the single field against two calls racing each other, not against reordering.
/// </summary>
internal sealed class CycleBoundaryRecorder : IProgress<CycleProgress>
{
    private readonly Lock _gate = new();

    /// <summary>The <see cref="Stopwatch"/> timestamp of the last progress report seen for
    /// cycle 0, or <see langword="null"/> if none arrived before <c>Run</c> returned (a
    /// formulation whose cycle 0 produces no progress report at all, in which case
    /// <see cref="PortRunner.RunCycle1"/> falls back to the whole call's elapsed time and says
    /// so in the figure).</summary>
    public long? Cycle0EndTimestamp { get; private set; }

    public void Report(CycleProgress value)
    {
        if (value.Cycle != 0)
        {
            return;
        }

        lock (_gate)
        {
            Cycle0EndTimestamp = Stopwatch.GetTimestamp();
        }
    }
}
