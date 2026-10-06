using ILGPU;
using ILGPU.Runtime;
using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L1 of the BOOT.md table: <see cref="Attempt.Run"/> called directly on the host thread
/// and the same call compiled into an ILGPU CPU-accelerator kernel give bit-identical
/// outcomes, streams, totals, records and scratch — the same code, not a second
/// implementation (root BOOT.md, Invariants: "One particle program"). The ticked
/// acceptance criterion says "streams" and "records" (plural, all of them): checking only
/// outcome, totals and the record left the six <see cref="Mcg128State"/> pairs of
/// <see cref="StreamSet"/> and the particle's own scratch buffer unchecked, so a kernel
/// bug confined to either could have stayed green under the old criterion's own wording
/// (2026-09-17, fixed).
/// </summary>
public class HostEqualsCpuAcceleratorTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public HostEqualsCpuAcceleratorTests(CpuHost host)
    {
        _host = host;
    }

    [Theory]
    [InlineData(1.0, 0.1, 1.0, 0.1, 10.0, 1)]   // accepted, cycle 1, pockets and bridges
    [InlineData(1.0, 0.1, 1.0, 0.1, 10.0, 0)]   // accepted, cycle 0
    [InlineData(2.0, 0.2, 1.0, 0.1, 10.0, 1)]   // restarted after loop (no pocket)
    public void RunMatchesOnHostAndTheCpuAccelerator(double size, double cellSize, double lambda, double ak3, double ak4, int cycleFlag)
    {
        var acc = _host.Accelerator;

        using var hostScenario = Scenario.Build(acc, size, cellSize, lambda, ak3, ak4, cycleFlag);
        var hostStreams = OriginalSeeds.Streams;
        var hostOutcome = hostScenario.Run(ref hostStreams);
        var hostIntegerTotals = hostScenario.AllIntegerTotals();
        var hostRecord = hostScenario.AllRecords();
        var hostScratch = hostScenario.AllScratch();

        using var kernelScenario = Scenario.Build(acc, size, cellSize, lambda, ak3, ak4, cycleFlag);
        using var streamsBuffer = acc.Allocate1D(new[] { OriginalSeeds.Streams });
        using var outcomeBuffer = acc.Allocate1D<byte>(1);

        var kernel = acc.LoadAutoGroupedStreamKernel<
            Index1D, ModelSetup, FractionTable, CycleInputs,
            ArrayView<StreamSet>, ArrayView<long>, ArrayView<double>, ArrayView<double>, ArrayView<byte>>(
            AttemptKernel.Run);

        kernel(1, kernelScenario.Setup, kernelScenario.Fractions, kernelScenario.Cycle,
            streamsBuffer.View.BaseView, kernelScenario.IntegerTotalsView, kernelScenario.RecordView,
            kernelScenario.ScratchView, outcomeBuffer.View.BaseView);
        acc.Synchronize();

        var kernelOutcome = (AttemptOutcome)outcomeBuffer.GetAsArray1D()[0];
        var kernelIntegerTotals = kernelScenario.AllIntegerTotals();
        var kernelRecord = kernelScenario.AllRecords();
        var kernelScratch = kernelScenario.AllScratch();
        var kernelStreams = streamsBuffer.GetAsArray1D()[0];

        Assert.Equal(hostOutcome, kernelOutcome);

        for (var i = 0; i < hostIntegerTotals.Length; i++)
        {
            Assert.True(hostIntegerTotals[i] == kernelIntegerTotals[i], $"integer total {i}: host={hostIntegerTotals[i]} kernel={kernelIntegerTotals[i]}");
        }

        for (var i = 0; i < hostRecord.Length; i++)
        {
            Assert.True(
                BitConverter.DoubleToInt64Bits(hostRecord[i]) == BitConverter.DoubleToInt64Bits(kernelRecord[i]),
                $"record {i}: host={hostRecord[i]} kernel={kernelRecord[i]}");
        }

        for (var i = 0; i < hostScratch.Length; i++)
        {
            Assert.True(
                BitConverter.DoubleToInt64Bits(hostScratch[i]) == BitConverter.DoubleToInt64Bits(kernelScratch[i]),
                $"scratch {i}: host={hostScratch[i]} kernel={kernelScratch[i]}");
        }

        AssertStreamEqual("S1", hostStreams.S1, kernelStreams.S1);
        AssertStreamEqual("S2", hostStreams.S2, kernelStreams.S2);
        AssertStreamEqual("S3", hostStreams.S3, kernelStreams.S3);
        AssertStreamEqual("S4", hostStreams.S4, kernelStreams.S4);
        AssertStreamEqual("S5", hostStreams.S5, kernelStreams.S5);
        AssertStreamEqual("S6", hostStreams.S6, kernelStreams.S6);
    }

    private static void AssertStreamEqual(string name, Mcg128State host, Mcg128State kernel)
    {
        Assert.True(
            host.Low == kernel.Low && host.High == kernel.High,
            $"{name}: host=({host.Low:X16},{host.High:X16}) kernel=({kernel.Low:X16},{kernel.High:X16})");
    }
}

/// <summary>The kernel entry point of <see cref="HostEqualsCpuAcceleratorTests"/>: one attempt per lane.</summary>
internal static class AttemptKernel
{
    public static void Run(
        Index1D index,
        ModelSetup setup, FractionTable fractions, CycleInputs cycle,
        ArrayView<StreamSet> streams,
        ArrayView<long> integerTotals,
        ArrayView<double> records,
        ArrayView<double> scratches,
        ArrayView<byte> outcomes)
    {
        var lane = index.X;
        var laneStreams = streams[lane];

        var outcome = Attempt.Run(setup, fractions, cycle, ref laneStreams, integerTotals, records, scratches);

        streams[lane] = laneStreams;
        outcomes[lane] = (byte)outcome;
    }
}
