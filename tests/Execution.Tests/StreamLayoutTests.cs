using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (architecture audit finding R1, 2026-09-26): the <c>Original</c> stream layout's refusal in batched
/// mode was withdrawn from this node (BOOT.md, "## Invariants", the 2026-09-26 note). It was always a
/// policy choice about the layout's own statistical bias (root BOOT.md, "Two stream layouts"), never a
/// mechanism this node could not execute — <see cref="Engine.RunBatch"/> and
/// <see cref="Engine.RunContinuedBatch"/> derive and run either layout's streams correctly, exactly as they
/// were measured doing before the policy decision (root BOOT.md's own batched-`Original` row). The refusal
/// now lives entirely in <c>Simulation.SimulationOptionsValidator</c> (`src/Simulation/BOOT.md`), before
/// <c>Engine</c> is ever reached through the sanctioned path; this node keeps no layout check of its own,
/// and <see cref="Engine.RunContinuedBatch"/>'s own <c>layout</c> parameter — which existed solely for that
/// check — is dropped with it. This class proves the negative: both methods now run <c>Original</c>-layout
/// streams to completion, the same as <c>Independent</c>-layout streams always have.
/// </summary>
public sealed class StreamLayoutTests
{
    [Fact]
    public void RunBatchWithOriginalLayoutRunsToCompletion()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        // Mutation proof, the mirror of the withdrawn one: reintroducing a layout check in RunBatch
        // (Engine.cs) would turn this from Ok into a refusal status, since ConstructedSetups.ReachesNeighbourLoop's
        // own setup otherwise completes in one attempt (RunBatchEqualsSequentialContinuedBatchesTests and others).
        var status = engine.RunBatch(StreamLayout.Original, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 4, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.True(counters.Attempts >= 1);
    }

    [Fact]
    public void RunContinuedBatchWithOriginalSeedsRunsToCompletion()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        // OriginalSeeds' own states (not merely Original-layout-shaped streams handed to an Independent
        // call): this method never derived from a layout argument at all, so which seeds produced these
        // streams cannot matter to it, and this is the direct proof.
        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        var status = engine.RunContinuedBatch(ref streams, attemptsPerLaunch: 1, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.True(counters.Attempts >= 1);
    }

    [Fact]
    public void RunBatchWithIndependentLayoutIsUnaffected()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 4, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.True(counters.Attempts >= 1);
    }

    [Fact]
    public void RunContinuedBatchWithIndependentSeedsIsUnaffected()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var streams = IndependentSeeds.ForParticle(0UL, 0UL);
        var status = engine.RunContinuedBatch(ref streams, attemptsPerLaunch: 1, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.True(counters.Attempts >= 1);
    }

    [Fact]
    public void RunReferenceParticleWithOriginalLayoutStreamsIsUnaffected()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        // RunReferenceParticle takes no layout argument at all, and never has: it accepts whichever streams
        // it is handed, Original-seeded or Independent-seeded alike.
        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        var status = engine.RunReferenceParticle(ref streams, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.True(counters.Attempts >= 1);
    }
}
