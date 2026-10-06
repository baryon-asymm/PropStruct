using ILGPU;
using ILGPU.Runtime;
using PropStruct.Particle;

namespace PropStruct.Execution;

/// <summary>
/// Owns QKS1's own buffer and the bookkeeping of when it was last made to match the totals (BOOT.md, "QKS1
/// is the engine's"). Extracted from <see cref="Engine"/> (decided 2026-09-18, the review of this node)
/// so that "resync the bookkeeping without recomputing the buffer" — the exact shape of the defect
/// <see cref="Engine.WriteTotals"/> had (BOOT.md, "third look") — cannot be written again: there is now only
/// one place either operation can happen, and <see cref="RefreshIfLoopsCompleted"/> can only ever call
/// <see cref="Recompute"/>, never touch the bookkeeping alone.
/// </summary>
internal sealed class Qks1Refresher : IDisposable
{
    private readonly Accelerator _accelerator;
    private readonly KernelCache _kernels;
    private readonly bool _hostThreads;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _buffer;
    private long _lastRefreshLoopCompletions;

    /// <summary>
    /// <paramref name="hostThreads"/> (BOOT.md, "Host-thread path", 2026-09-19): <see langword="true"/> for
    /// every engine bound to the CPU accelerator except the ILGPU-kernel test oracle, calls
    /// <see cref="PocketHistogram.Normalize"/> directly on <see cref="Recompute"/> — the same static method,
    /// no kernel launch — instead of going through <paramref name="kernels"/>' launcher, since reference
    /// mode refreshes QKS1 before every attempt and a kernel launch's own dispatch/synchronisation cost
    /// there was the dominant share of reference mode's per-attempt time (measured before this change: about
    /// 45 µs/attempt, the scale of one accelerator launch).
    /// </summary>
    public Qks1Refresher(Accelerator accelerator, KernelCache kernels, int nkarm, bool hostThreads)
    {
        _accelerator = accelerator;
        _kernels = kernels;
        _hostThreads = hostThreads;
        _buffer = accelerator.Allocate1D<double>(nkarm);
    }

    /// <summary>The buffer's current view, for <c>CycleInputs.Qks1</c>.</summary>
    public ArrayView<double> View => _buffer.View;

    /// <summary>Downloads the buffer's current content. For tests only: no production caller needs the raw array.</summary>
    public double[] Download() => _buffer.GetAsArray1D();

    /// <summary>
    /// Recomputes QKS1 from <paramref name="integerTotals"/> unconditionally, and records
    /// <paramref name="currentLoopCompletions"/> as the point it was last made consistent. Called by
    /// <see cref="Engine.Load"/> (a freshly zeroed run) and <see cref="Engine.WriteTotals"/> (the totals were
    /// just replaced wholesale, so there is no incremental history to compare against — BOOT.md, "third
    /// look").
    /// </summary>
    public void Recompute(in AccumulatorLayout layout, ArrayView<long> integerTotals, long currentLoopCompletions)
    {
        if (_hostThreads)
        {
            PocketHistogram.Normalize(in layout, integerTotals, _buffer.View);
        }
        else
        {
            var launcher = _kernels.Get<Action<AcceleratorStream, Index1D, AccumulatorLayout, ArrayView<long>, ArrayView<double>>>(nameof(Kernels.NormalizeQks1));
            launcher(_accelerator.DefaultStream, new Index1D(1), layout, integerTotals, _buffer.View);
            _accelerator.Synchronize();
        }

        _lastRefreshLoopCompletions = currentLoopCompletions;
    }

    /// <summary>
    /// Refreshes QKS1 only if <paramref name="currentLoopCompletions"/> differs from the value it was last
    /// made consistent with (root BOOT.md, "Batched mode freezes only QKS1, per launch"). Called before every
    /// launch (<see cref="Engine"/>'s relaunch loop) and before every reference-mode attempt.
    /// </summary>
    public bool RefreshIfLoopsCompleted(in AccumulatorLayout layout, ArrayView<long> integerTotals, long currentLoopCompletions)
    {
        if (currentLoopCompletions == _lastRefreshLoopCompletions)
        {
            return false;
        }

        Recompute(in layout, integerTotals, currentLoopCompletions);
        return true;
    }

    /// <inheritdoc />
    public void Dispose() => _buffer.Dispose();
}
