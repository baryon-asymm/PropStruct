namespace PropStruct.Cli;

/// <summary>
/// An <see cref="IProgress{T}"/> that calls back on the reporting thread, synchronously, instead of
/// posting through a <see cref="SynchronizationContext"/> the way <see cref="Progress{T}"/> does. A
/// console tool has no synchronization context, so <see cref="Progress{T}"/> would post through the
/// thread pool: progress lines could print out of cycle order, or after the summary line. This node owns
/// no accumulator and no side effect beyond the callback, so nothing here needs the ordering
/// <see cref="Progress{T}"/> is built for.
/// </summary>
internal sealed class SynchronousProgress<T> : IProgress<T>
{
    private readonly Action<T> _callback;

    public SynchronousProgress(Action<T> callback)
    {
        _callback = callback;
    }

    public void Report(T value) => _callback(value);
}
