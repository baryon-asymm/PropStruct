using PropStruct.Particle;
using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md): each failure outcome of an attempt ends its batch with its own status, from constructed
/// setups (acceptance criterion "A particle passing the attempt cap ends its batch with
/// AttemptCapExceeded, and each failure outcome of an attempt ends it with its own status").
///
/// <see cref="Engine.ToFailureStatus"/> is checked exhaustively over every <see cref="AttemptOutcome"/>
/// below; two of its six outcomes end a batch through a real, end-to-end run
/// (<see cref="AttemptOutcome.NeighbourBudgetExceeded"/>, and <see cref="AttemptOutcome.Accepted"/> via
/// the <c>Ok</c> case in <see cref="Qks1RefreshTests"/>); <see cref="AttemptOutcome.BridgeDrawBudgetExceeded"/>
/// and <see cref="AttemptOutcome.IndexOutOfRange"/> are, by Particle's own outcome table
/// (<c>src/Particle/BOOT.md</c>, "Attempt structure"), unreachable through <c>Attempt.Run</c> under a
/// precondition-respecting setup — Particle.Tests proves each of them directly against the guard that
/// produces it (<c>AttemptOutcomeTests</c>, <c>BridgeWindowTests.SamplePocketGuardsAnUnboundedSearch</c>);
/// this node's own claim for those two is the mapping to the matching <see cref="BatchStatus"/>, not a
/// from-scratch physical reconstruction of an already-declared-unreachable branch.
/// </summary>
public sealed class BatchStatusTests
{
    // AttemptOutcome and BatchStatus are both internal, so a public [Theory] method cannot name either in
    // its own signature (CS0051): the raw byte/int values cross the boundary instead, cast back to the
    // internal enums inside the method body.
    [Theory]
    [InlineData((byte)AttemptOutcome.Accepted, -1)]
    [InlineData((byte)AttemptOutcome.RestartedAfterLoop, -1)]
    [InlineData((byte)AttemptOutcome.RestartedInsideLoop, -1)]
    [InlineData((byte)AttemptOutcome.NeighbourBudgetExceeded, (int)BatchStatus.NeighbourBudgetExceeded)]
    [InlineData((byte)AttemptOutcome.BridgeDrawBudgetExceeded, (int)BatchStatus.BridgeDrawBudgetExceeded)]
    [InlineData((byte)AttemptOutcome.IndexOutOfRange, (int)BatchStatus.IndexOutOfRange)]
    public void ToFailureStatusMapsEveryOutcome(byte outcomeValue, int expectedValue)
    {
        BatchStatus? expected = expectedValue < 0 ? null : (BatchStatus)expectedValue;
        Assert.Equal(expected, Engine.ToFailureStatus((AttemptOutcome)outcomeValue));
    }

    /// <summary>
    /// <see cref="Kernels.IsFailure"/>'s own list, over every <see cref="AttemptOutcome"/>: the same
    /// question <see cref="ToFailureStatusMapsEveryOutcome"/> asks of the status mapping, asked directly
    /// of the predicate <see cref="Kernels.RunAttempts"/> shares with <see cref="Engine.ToFailureStatus"/>
    /// (Opus audit item 5, 2026-09-18).
    /// </summary>
    [Theory]
    [InlineData((byte)AttemptOutcome.Accepted, false)]
    [InlineData((byte)AttemptOutcome.RestartedAfterLoop, false)]
    [InlineData((byte)AttemptOutcome.RestartedInsideLoop, false)]
    [InlineData((byte)AttemptOutcome.NeighbourBudgetExceeded, true)]
    [InlineData((byte)AttemptOutcome.BridgeDrawBudgetExceeded, true)]
    [InlineData((byte)AttemptOutcome.IndexOutOfRange, true)]
    public void IsFailureAgreesWithEveryOutcome(byte outcomeValue, bool expected) => Assert.Equal(expected, Kernels.IsFailure((AttemptOutcome)outcomeValue));

    /// <summary>
    /// L1 (BOOT.md, Opus audit item 5, 2026-09-18): every failure test before this one ran with
    /// <c>attemptsPerLaunch: 1</c>, so a launch was always exactly one attempt and never exercised
    /// <see cref="Kernels.RunAttempts"/>'s own in-launch stop (the loop that keeps going up to
    /// <c>attemptsPerLaunch</c> unless <see cref="Kernels.IsFailure"/> or <see cref="AttemptOutcome.Accepted"/>
    /// fires first). With <c>attemptsPerLaunch: 4</c> a launch could run up to four attempts; asserting
    /// <c>Attempts == 1</c> proves the failure on the very first attempt actually stopped the launch early
    /// rather than the batch merely reporting the first attempt's own outcome after silently running the
    /// other three.
    /// </summary>
    [Fact]
    public void NeighbourBudgetExceededStopsTheLaunchOnTheFailingAttempt()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.NeighbourBudgetExceeded();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 4, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.NeighbourBudgetExceeded, status);
        Assert.Equal(1, counters.Attempts);
        Assert.Equal(1, counters.Launches);
    }

    /// <summary>
    /// L1 (BOOT.md, Opus audit item 5, 2026-09-18): the earlier cap test set <c>attemptsPerLaunch</c> equal
    /// to the cap, so the cap and the end of the first launch coincided and never separately proved that
    /// the host relaunches an unfinished particle across several launches before the cap stops it. Two
    /// attempts per launch against a cap of five spans three launches (2 + 2 + 1); every attempt restarts
    /// inside the loop unconditionally (<see cref="ConstructedSetups.NeverCompletesTheLoop"/>), so the cap
    /// fires deterministically at exactly five attempts across exactly three launches.
    /// </summary>
    [Fact]
    public void AttemptCapExceededSpansSeveralLaunches()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.NeverCompletesTheLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 2, maxAttemptsPerParticle: 5, out var counters);

        Assert.Equal(BatchStatus.AttemptCapExceeded, status);
        Assert.Equal(5, counters.Attempts);
        Assert.Equal(3, counters.Launches);
    }

    [Fact]
    public void AttemptCapExceededEndsTheBatchWithoutFolding()
    {
        // Reuses Qks1RefreshTests' own construction: SFR = 0 restarts inside the loop unconditionally, so
        // the cap always fires deterministically at exactly maxAttemptsPerParticle attempts, whatever the
        // generator draws.
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.NeverCompletesTheLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 3, maxAttemptsPerParticle: 3, out var counters);

        Assert.Equal(BatchStatus.AttemptCapExceeded, status);
        Assert.Equal(3, counters.Attempts);

        // Not folded: the real totals the engine holds are still all zero. Integer totals are not asserted
        // here (they are not "the state at the moment of failure" either: API.md, "Errors" now documents
        // exactly what they do hold).
        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        engine.ReadTotals(integerTotals, realTotals);
        Assert.All(realTotals, value => Assert.Equal(0.0, value));
    }

    /// <summary>
    /// L1 (BOOT.md, Opus audit item 5, 2026-09-18): <see cref="RunContinuedBatchPropagatesTheSameStatusAsRunBatch"/>
    /// below still uses <c>attemptsPerLaunch: 1</c>, so it only proves status propagation, not the
    /// in-launch stop; that is <see cref="NeighbourBudgetExceededStopsTheLaunchOnTheFailingAttempt"/>'s own
    /// claim, on <see cref="Engine.RunBatch"/>. <see cref="Engine.RunContinuedBatch"/> shares
    /// <see cref="Engine.RunParticlesCore"/> with <see cref="Engine.RunBatch"/>, so the same in-launch stop
    /// applies to both; this repeats the assertion on the continued path for its own attemptsPerLaunch &gt; 1.
    /// </summary>
    [Fact]
    public void RunContinuedBatchStopsTheLaunchOnTheFailingAttempt()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.NeighbourBudgetExceeded();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        var status = engine.RunContinuedBatch(ref streams, attemptsPerLaunch: 4, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.NeighbourBudgetExceeded, status);
        Assert.Equal(1, counters.Attempts);
        Assert.Equal(1, counters.Launches);
    }

    [Fact]
    public void RunContinuedBatchPropagatesTheSameStatusAsRunBatch()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.NeighbourBudgetExceeded();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        var status = engine.RunContinuedBatch(ref streams, attemptsPerLaunch: 1, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.NeighbourBudgetExceeded, status);
        Assert.Equal(1, counters.Attempts);
    }
}
