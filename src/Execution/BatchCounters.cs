namespace PropStruct.Execution;

/// <summary>
/// What a batch (or a reference-mode particle) did, whatever its <see cref="BatchStatus"/>
/// (API.md, "Engine").
/// </summary>
/// <param name="Attempts">The sum of every particle's own attempt count.</param>
/// <param name="Launches">The number of kernel launches the batch took (reference mode: the
/// number of attempts, one host-thread iteration each — API.md, "Semantics").</param>
/// <param name="Qks1Refreshes">How many times <c>PocketHistogram.Normalize</c> ran.</param>
internal readonly record struct BatchCounters(long Attempts, int Launches, int Qks1Refreshes);
