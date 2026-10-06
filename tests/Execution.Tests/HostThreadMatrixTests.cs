using PropStruct.Random;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The bit-identity matrix of BOOT.md, "Host-thread path": the five reference formulations, batch sizes 1,
/// 7 and the whole cycle, <c>attemptsPerLaunch</c> 1, 2 and 256 (so relaunches are genuinely exercised), 1,
/// 4, 8 and 16 threads — the host-thread path (<see cref="Engine.Create"/>'s default on the CPU accelerator)
/// against the ILGPU-kernel-launch oracle (<c>forceIlgpuKernelsOnCpu: true</c>), all forked from the same
/// cycle-1-ready state (<see cref="ReferenceFormulationDriver"/>'s own technique, as
/// <see cref="DeterminismTests"/> and <see cref="TierTableTests"/> already use it).
///
/// The whole-cycle row uses a single thread count (16, all cores) rather than repeating all four: 1, 4 and
/// 16 threads are already proven bit-identical to each other on the host-thread path by
/// <see cref="DeterminismTests"/>, so re-running the full formulation at every thread count here would only
/// re-confirm that determinism at whole-cycle scale, not test anything the small-batch rows below do not —
/// while costing several minutes per additional thread count (the ILGPU-kernel oracle itself, not the
/// host-thread path, is the slow side: BOOT.md, "Throughput measurement", "third look" — the oracle's own
/// per-launch kernel-dispatch cost is why the host-thread path exists). The small-batch rows below vary the
/// thread count directly, so a wrong host-thread dispatch (e.g. one that only works with the thread pool's
/// default degree of parallelism) is still caught.
/// </summary>
[Collection(LongClassesSerialTests.Name)]
public sealed class HostThreadMatrixTests(ITestOutputHelper output)
{
    private const long RecordBudgetBytes = 1L << 27;
    private const long WholeCycleRecordBudgetBytes = 2L << 30;
    private const long MaxAttemptsPerParticle = 10_000_000;
    private const int WholeCycleAttemptsPerLaunch = 256;
    private const int WholeCycleThreads = 16;

    private static readonly string[] Formulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];
    private static readonly int[] SmallBatchSizes = [1, 7];
    private static readonly int[] AttemptsPerLaunchValues = [1, 2, 256];
    private static readonly int[] ThreadCounts = [1, 4, 8, 16];

    [Fact]
    [Trait("Category", "Long")]
    public void HostThreadsEqualsTheIlgpuCpuAcceleratorOracleBitForBitOnSmallCycle1Batches()
    {
        var checkedCombinations = 0;

        foreach (var formulationName in Formulations)
        {
            checkedCombinations += CheckFormulation(formulationName);
        }

        output.WriteLine($"checked {checkedCombinations} (formulation, threads, batch, attemptsPerLaunch) combinations");
        Assert.Equal(Formulations.Length * ThreadCounts.Length * SmallBatchSizes.Length * AttemptsPerLaunchValues.Length, checkedCombinations);
    }

    // Split out of HostThreadsEqualsTheIlgpuCpuAcceleratorOracleBitForBitOnSmallCycle1Batches so that
    // `warmup`'s creation and disposal sit in a method whose control-flow graph is simple enough for CA2000 to
    // follow: with the three nested loops inlined in the caller, CA2000 flagged `warmup` as possibly
    // undisposed even under the `using var` declaration, the `using (...) { }` block form and an explicit
    // try/finally with `warmup?.Dispose()` — the three patterns its own diagnostic message recommends, in
    // that order. The object graph is unchanged; only the amount of code the analyzer must track between
    // `warmup`'s creation and disposal did.
    private static int CheckFormulation(string formulationName)
    {
        var checkedCombinations = 0;

        using var warmup = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, cpuThreads: 16);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, formulationName);
        var setup = ready.Setup;

        foreach (var threads in ThreadCounts)
        {
            using var oracle = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, cpuThreads: threads, forceIlgpuKernelsOnCpu: true);
            using var hostThreads = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, cpuThreads: threads);

            foreach (var batchSize in SmallBatchSizes)
            {
                foreach (var attemptsPerLaunch in AttemptsPerLaunchValues)
                {
                    var label = $"{formulationName} threads={threads} batch={batchSize} attemptsPerLaunch={attemptsPerLaunch}";

                    oracle.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
                    oracle.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
                    oracle.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
                    Assert.Contains(oracle.DebugReadQks1(), v => v != 0.0);

                    hostThreads.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
                    hostThreads.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
                    hostThreads.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
                    Assert.Contains(hostThreads.DebugReadQks1(), v => v != 0.0);

                    var oracleStatus = oracle.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, batchSize,
                        attemptsPerLaunch, MaxAttemptsPerParticle, out var oracleCounters);
                    var hostStatus = hostThreads.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, batchSize,
                        attemptsPerLaunch, MaxAttemptsPerParticle, out var hostCounters);

                    Assert.True(oracleStatus == hostStatus, $"{label}: status differs (oracle {oracleStatus}, host threads {hostStatus})");
                    Assert.True(oracleCounters.Equals(hostCounters), $"{label}: counters differ (oracle {oracleCounters}, host threads {hostCounters})");

                    var oracleInteger = new long[setup.Layout.IntegerLength];
                    var oracleReal = new double[setup.Layout.RecordLength];
                    oracle.ReadTotals(oracleInteger, oracleReal);

                    var hostInteger = new long[setup.Layout.IntegerLength];
                    var hostReal = new double[setup.Layout.RecordLength];
                    hostThreads.ReadTotals(hostInteger, hostReal);

                    Assert.True(oracleInteger.AsSpan().SequenceEqual(hostInteger), $"{label}: integer totals differ");
                    Assert.True(oracleReal.AsSpan().SequenceEqual(hostReal), $"{label}: real totals differ");
                    checkedCombinations++;
                }
            }
        }

        return checkedCombinations;
    }

    [Theory]
    [Trait("Category", "Long")]
    [InlineData("HPEPA3")]
    [InlineData("inpt")]
    [InlineData("P33")]
    [InlineData("PSAN02n")]
    [InlineData("HMX")]
    public void HostThreadsEqualsTheIlgpuCpuAcceleratorOracleBitForBitOnAWholeCycle1Batch(string formulationName)
    {
        using var warmup = Engine.Create(AcceleratorKind.Cpu, WholeCycleRecordBudgetBytes, cpuThreads: WholeCycleThreads);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, formulationName, WholeCycleAttemptsPerLaunch);
        var setup = ready.Setup;

        using var oracle = Engine.Create(AcceleratorKind.Cpu, WholeCycleRecordBudgetBytes, cpuThreads: WholeCycleThreads, forceIlgpuKernelsOnCpu: true);
        oracle.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        oracle.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        oracle.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(oracle.DebugReadQks1(), v => v != 0.0);

        using var hostThreads = Engine.Create(AcceleratorKind.Cpu, WholeCycleRecordBudgetBytes, cpuThreads: WholeCycleThreads);
        hostThreads.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        hostThreads.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        hostThreads.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(hostThreads.DebugReadQks1(), v => v != 0.0);

        var particlesPerCycle = ready.Formulation.ParticlesPerCycle;

        var oracleStatus = oracle.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, particlesPerCycle,
            WholeCycleAttemptsPerLaunch, MaxAttemptsPerParticle, out var oracleCounters);
        var hostStatus = hostThreads.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, particlesPerCycle,
            WholeCycleAttemptsPerLaunch, MaxAttemptsPerParticle, out var hostCounters);

        Assert.Equal(BatchStatus.Ok, oracleStatus);
        Assert.Equal(BatchStatus.Ok, hostStatus);
        Assert.Equal(oracleCounters, hostCounters);

        var oracleInteger = new long[setup.Layout.IntegerLength];
        var oracleReal = new double[setup.Layout.RecordLength];
        oracle.ReadTotals(oracleInteger, oracleReal);

        var hostInteger = new long[setup.Layout.IntegerLength];
        var hostReal = new double[setup.Layout.RecordLength];
        hostThreads.ReadTotals(hostInteger, hostReal);

        output.WriteLine($"{formulationName}: {particlesPerCycle} particles, oracle attempts={oracleCounters.Attempts}, host-thread attempts={hostCounters.Attempts}");
        Assert.Equal(oracleInteger, hostInteger);
        Assert.Equal(oracleReal, hostReal);
    }
}
