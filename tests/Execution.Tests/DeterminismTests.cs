using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L2 (BOOT.md): the same batch on the CPU accelerator is bit-identical with 1, 4 and 16 threads, and
/// between two runs (root BOOT.md, "Deterministic under every schedule"; acceptance criterion "The same
/// configuration run twice, and with 1, 4 and 16 CPU threads, gives bit-identical results").
/// </summary>
[Collection(LongClassesSerialTests.Name)]
public sealed class DeterminismTests
{
    private const long RecordBudgetBytes = 1L << 27;
    private const int BatchParticles = 5000;
    private const int AttemptsPerLaunch = 64;
    private const long MaxAttemptsPerParticle = 10_000_000;

    [Fact]
    [Trait("Category", "Long")] // ~30s: a real HPEPA3 cycle 0 (100000 particles) plus four cycle-1 batches
    public void TheSameBatchIsBitIdenticalAcrossThreadCountsAndRepeatedRuns()
    {
        // One engine drives cycle 0 (its own thread count does not matter: it only produces the shared
        // starting point every configuration below is loaded with) and hands back the cycle-1-ready state.
        using var warmup = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, cpuThreads: 16);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, "HPEPA3");

        // Five configurations: 1 thread twice (proving "two runs" bit-identical), then 4 and 16 threads
        // once each, all compared against the first.
        int[] threadPlan = [1, 1, 4, 16];
        var results = new (long[] Integer, double[] Real)[threadPlan.Length];

        var setup = ready.Setup;
        for (var i = 0; i < threadPlan.Length; i++)
        {
            using var engine = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, threadPlan[i]);
            engine.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
            engine.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
            engine.SetCycle(cycleFlag: 1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
            // Guards the WriteTotals defect (src/Execution/BOOT.md, "third look"): each of these four forks
            // must see the same populated QKS1, not a stale all-zero one.
            Assert.Contains(engine.DebugReadQks1(), v => v != 0.0);

            var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, BatchParticles,
                AttemptsPerLaunch, MaxAttemptsPerParticle, out _);
            Assert.Equal(BatchStatus.Ok, status);

            var resultInteger = new long[ready.Setup.Layout.IntegerLength];
            var resultReal = new double[ready.Setup.Layout.RecordLength];
            engine.ReadTotals(resultInteger, resultReal);
            results[i] = (resultInteger, resultReal);
        }

        for (var i = 1; i < results.Length; i++)
        {
            Assert.Equal(results[0].Integer, results[i].Integer);
            Assert.Equal(results[0].Real, results[i].Real);
        }
    }
}
