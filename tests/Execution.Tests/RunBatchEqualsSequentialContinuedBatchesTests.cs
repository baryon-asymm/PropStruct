using PropStruct.Random;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md, Opus audit item 7, 2026-09-18): every existing <c>RunBatch</c> test uses one particle
/// (<c>ConstructedSetups</c>' own setups are all built for a single-particle attempt), so the compacted
/// index list of <see cref="Engine.RunBatch"/> — the host-side <c>active</c>/<c>next</c> bookkeeping that
/// drops an accepted particle out of the relaunch list and keeps the rest, over several launches — is
/// never actually exercised by more than one particle at once. This test derives 200 particles' streams
/// the same way <see cref="Engine.RunBatch"/> does internally
/// (<see cref="StreamSeeds.ForBatchedParticle(StreamLayout, ulong, ulong)"/>, one call per particle) and runs each
/// through its own <see cref="Engine.RunContinuedBatch"/> sequentially, then checks that one <c>RunBatch</c>
/// call over the same 200 particles reaches the same integer and real totals bit for bit: the compacted
/// index list must not change any particle's own sequence of attempts, only how many run per host round
/// trip.
/// </summary>
public sealed class RunBatchEqualsSequentialContinuedBatchesTests(ITestOutputHelper output)
{
    private const int ParticleCount = 200;
    private const long MaxAttemptsPerParticle = 1_000_000;

    [Fact]
    public void RunBatchOverCycle0EqualsSequentialRunContinuedBatchCallsBitForBit()
    {
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop(nc: 10);
        var layout = setup.Layout;

        using var batched = Engine.Create(AcceleratorKind.Cpu, 1L << 24);
        batched.Load(in setup, bounds, cumulative, pocketForming);
        batched.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var batchStatus = batched.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, ParticleCount,
            attemptsPerLaunch: 1, MaxAttemptsPerParticle, out var batchCounters);
        Assert.Equal(BatchStatus.Ok, batchStatus);

        // Guards against a vacuous pass (as the audit itself asks of the QKS1 blocker test): if every
        // particle were accepted on its very first attempt, RunBatch would never relaunch the compacted
        // index list, and this comparison would be no different from ParticleCount independent single-
        // particle batches.
        Assert.True(batchCounters.Launches > 1,
            $"launches ({batchCounters.Launches}) must exceed 1 or this run never exercised a relaunch of the compacted index list.");

        using var sequential = Engine.Create(AcceleratorKind.Cpu, 1L << 24);
        sequential.Load(in setup, bounds, cumulative, pocketForming);
        sequential.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        long sequentialAttempts = 0;
        for (var i = 0; i < ParticleCount; i++)
        {
            var streams = StreamSeeds.ForBatchedParticle(StreamLayout.Independent, seed: 0UL, ordinal: (ulong)i);
            var status = sequential.RunContinuedBatch(ref streams, attemptsPerLaunch: 1, MaxAttemptsPerParticle, out var counters);
            Assert.Equal(BatchStatus.Ok, status);
            sequentialAttempts += counters.Attempts;
        }

        output.WriteLine($"batch: attempts={batchCounters.Attempts}, launches={batchCounters.Launches}; sequential: attempts={sequentialAttempts}");
        Assert.Equal(sequentialAttempts, batchCounters.Attempts);

        var batchInteger = new long[layout.IntegerLength];
        var batchReal = new double[layout.RecordLength];
        batched.ReadTotals(batchInteger, batchReal);

        var sequentialInteger = new long[layout.IntegerLength];
        var sequentialReal = new double[layout.RecordLength];
        sequential.ReadTotals(sequentialInteger, sequentialReal);

        Assert.Equal(sequentialInteger, batchInteger);
        Assert.Equal(sequentialReal, batchReal);
    }
}
