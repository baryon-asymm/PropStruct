namespace PropStruct.Simulation.Tests;

/// <summary>
/// <c>System.Progress&lt;T&gt;</c> captures <c>SynchronizationContext.Current</c> at construction and, with
/// none present (the common case on an xunit worker thread), posts each report through the thread pool
/// instead of calling back synchronously — so a report can still be in flight after <c>Run</c> returns,
/// making a report count or "at least one report happened" assertion racy. <see cref="Simulator.Run"/>
/// itself calls <c>IProgress&lt;T&gt;.Report</c> directly on the calling thread with no marshaling of its
/// own, so this trivial synchronous implementation is what a test needs to observe reports deterministically.
/// </summary>
internal sealed class SynchronousProgress<T>(Action<T> onReport) : IProgress<T>
{
    public void Report(T value) => onReport(value);
}
