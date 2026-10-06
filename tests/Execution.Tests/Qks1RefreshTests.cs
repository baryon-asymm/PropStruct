using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md): QKS1 is refreshed exactly when the integer total <c>LoopCompletions</c> changed since
/// the last refresh (root BOOT.md, "Batched mode freezes only QKS1, per launch"; acceptance criterion "a
/// batch whose attempts all restart inside the loop does not refresh it").
/// </summary>
public sealed class Qks1RefreshTests
{
    [Fact]
    public void ABatchThatNeverCompletesTheNeighbourLoopNeverRefreshesQks1()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.NeverCompletesTheLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        // SFR = 0 for the only fraction: every attempt returns RestartedInsideLoop before it ever reaches
        // the neighbour loop, so LoopCompletions never changes and 5 attempts always exhaust the cap.
        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 5, maxAttemptsPerParticle: 5, out var counters);

        Assert.Equal(BatchStatus.AttemptCapExceeded, status);
        Assert.Equal(5, counters.Attempts);
        Assert.Equal(0, counters.Qks1Refreshes);
    }

    [Fact]
    public void ABatchAcceptedInItsFirstLaunchDefersTheRefreshToTheNextRunCall()
    {
        // Refreshing happens "before every launch" (BOOT.md, "QKS1 is the engine's"), not after: a batch
        // whose one particle is accepted inside its very first launch (attemptsPerLaunch generous enough
        // that every attempt it needs fits in one kernel call) never reaches a second launch, so the
        // refresh its own accepted attempt earns is only checked for at the top of whichever run call
        // comes next (see ASecondBatchRightAfterAnAcceptedOneSeesTheRefreshFromTheFirst below) — the same
        // framing "Reference mode is batched mode degenerated" needs to hold with attemptsPerLaunch = 1
        // (Engine.cs, RunReferenceParticle checks before each attempt for the same reason). This is
        // therefore not a bug: LoopCompletions truly changed (asserted below), QKS1 itself is simply not
        // re-normalized until the next launch boundary.
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        // Cycle 0: the loop always accepts once it completes (Particle/BOOT.md, "Attempt structure",
        // "Cycle 0: accepted"), regardless of the acceptance tests of lines 723-738.
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 1000, maxAttemptsPerParticle: 1_000_000, out var counters);

        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        engine.ReadTotals(integerTotals, realTotals);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.Equal(1, counters.Launches); // confirms the whole particle fit in one launch
        Assert.True(integerTotals[setup.Layout.LoopCompletions] > 0); // its accepted attempt did complete a loop
        Assert.Equal(0, counters.Qks1Refreshes); // yet nothing refreshed QKS1 within this call
    }

    [Fact]
    public void ASecondBatchRightAfterAnAcceptedOneSeesTheRefreshFromTheFirst()
    {
        // The refresh deferred by the test above is picked up at the top of the very next run call: a
        // second, otherwise independent batch (attemptsPerLaunch = 1, so its own first launch cannot yet
        // have completed a loop of its own) still reports at least one refresh, coming entirely from the
        // first batch's own tail.
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var first = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 1000, maxAttemptsPerParticle: 1_000_000, out var firstCounters);
        Assert.Equal(BatchStatus.Ok, first);
        Assert.Equal(0, firstCounters.Qks1Refreshes);

        var second = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 1UL, particleCount: 1,
            attemptsPerLaunch: 1, maxAttemptsPerParticle: 1_000_000, out var secondCounters);
        Assert.Equal(BatchStatus.Ok, second);
        Assert.True(secondCounters.Qks1Refreshes >= 1);
    }
}
