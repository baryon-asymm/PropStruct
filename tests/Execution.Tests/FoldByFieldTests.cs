using ILGPU;
using ILGPU.Runtime;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md): folding by field in parallel (<see cref="Kernels.FoldField"/>, one thread per field)
/// equals <see cref="Fold.Add"/> bit for bit on the same records (`src/Execution/BOOT.md`, "Fold in particle order,
/// per field in parallel"; acceptance criterion "Folding by field in parallel equals Fold.Add bit for bit
/// on the same records").
/// </summary>
public sealed class FoldByFieldTests
{
    [Fact]
    public void FoldFieldKernelEqualsFoldAddBitForBit()
    {
        var (setup, _, _, _) = ConstructedSetups.ReachesNeighbourLoop();
        var layout = setup.Layout;
        const int particleCount = 7;

        var random = new SplitMix64(12345);
        var records = new double[particleCount * layout.RecordLength];
        for (var i = 0; i < records.Length; i++)
        {
            records[i] = random.NextDouble() * 2.0 - 1.0; // includes negatives, so DpMax/DpMaxCor's max is non-trivial
        }

        using var host = new CpuHost();
        using var recordsBuffer = host.Accelerator.Allocate1D(records);

        // Expected: Fold.Add, called directly on the host thread over the same CPU-accelerator memory
        // (no kernel launch — Fold.Add is a plain kernel-compatible static method, exactly as reference
        // mode calls Attempt.Run directly, root BOOT.md, Taboos: "the host thread calls the same static
        // methods over CPU-accelerator views").
        using var expectedTotals = host.Accelerator.Allocate1D<double>(layout.RecordLength);
        expectedTotals.MemSetToZero();
        host.Accelerator.Synchronize();
        Fold.Add(in layout, recordsBuffer.View, particleCount, expectedTotals.View);
        var expected = expectedTotals.GetAsArray1D();

        // Actual: Kernels.FoldField, one thread per field, exactly as Engine.RunBatch/RunContinuedBatch
        // launch it after every particle of a batch is accepted.
        using var actualTotals = host.Accelerator.Allocate1D<double>(layout.RecordLength);
        actualTotals.MemSetToZero();
        host.Accelerator.Synchronize();
        var launcher = host.Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, AccumulatorLayout, ArrayView<double>, int, ArrayView<double>>(Kernels.FoldField);
        launcher(layout.RecordLength, layout, recordsBuffer.View, particleCount, actualTotals.View);
        host.Accelerator.Synchronize();
        var actual = actualTotals.GetAsArray1D();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FoldFieldKernelTakesTheMaximumForDpMaxAndDpMaxCor()
    {
        var (setup, _, _, _) = ConstructedSetups.ReachesNeighbourLoop();
        var layout = setup.Layout;
        const int particleCount = 4;

        // DpMax/DpMaxCor start at 0 (BOOT.md, "Real-valued record": "running maximum pocket radius,
        // start 0"), a real particle record never writes a negative radius here, so the batch values are
        // all positive to keep the maximum distinguishable from that starting value.
        var records = new double[particleCount * layout.RecordLength];
        var dpMaxValues = new[] { 3.0, 7.0, 1.0, 5.0 };
        var dpMaxCorValues = new[] { 2.0, 9.0, 1.0, 4.0 };
        for (var p = 0; p < particleCount; p++)
        {
            records[p * layout.RecordLength + layout.DpMax] = dpMaxValues[p];
            records[p * layout.RecordLength + layout.DpMaxCor] = dpMaxCorValues[p];
        }

        using var host = new CpuHost();
        using var recordsBuffer = host.Accelerator.Allocate1D(records);
        using var totals = host.Accelerator.Allocate1D<double>(layout.RecordLength);
        totals.MemSetToZero();
        host.Accelerator.Synchronize();

        var launcher = host.Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, AccumulatorLayout, ArrayView<double>, int, ArrayView<double>>(Kernels.FoldField);
        launcher(layout.RecordLength, layout, recordsBuffer.View, particleCount, totals.View);
        host.Accelerator.Synchronize();
        var result = totals.GetAsArray1D();

        Assert.Equal(7.0, result[layout.DpMax]);
        Assert.Equal(9.0, result[layout.DpMaxCor]);
    }
}
