using PropStruct.Particle;
using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md, "`Original` accumulation is refused for batched execution, unconditionally"):
/// <see cref="Engine.RunBatch"/> and <see cref="Engine.RunContinuedBatch"/> both refuse
/// <see cref="PrecisionKind.Original"/> before any attempt runs, on
/// <see cref="ConstructedSetups.ReachesNeighbourLoop"/> — a setup that otherwise completes in one attempt
/// (proven by <c>RunBatchEqualsSequentialContinuedBatchesTests</c> and others), so a passing status here
/// proves the refusal, not an unrelated failure of the setup, stopped the call.
/// <see cref="Engine.RunReferenceParticle"/> on the same setup is unaffected: reference mode is sequential
/// by construction and the root's invariant does not restrict it. This is this node's own **mechanism**
/// guard (architecture audit finding R1, 2026-09-26): <c>Simulation.SimulationOptionsValidator</c> refuses
/// the same combination earlier still, before a <c>Simulator</c> (and this engine) exists at all, so these
/// tests exercise <see cref="Engine"/> directly to prove the backstop still holds independently of that
/// upstream check (`src/Execution/BOOT.md`'s own 2026-09-26 note). The twin refusal for the stream layout,
/// which this class used to mirror, is withdrawn: see <c>StreamLayoutTests</c>.
/// </summary>
public sealed class PrecisionKindTests
{
    [Fact]
    public void RunBatchWithOriginalPrecisionIsRefusedBeforeAnyAttempt()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        setup.Kind = PrecisionKind.Original;
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        // Mutation proof: this setup is exactly ConstructedSetups.ReachesNeighbourLoop's own, which
        // RunBatchEqualsSequentialContinuedBatchesTests already runs to Ok with real attempts and launches.
        // Remove the Kind check from Engine.RunBatch and this call completes the same way, turning every
        // assertion below red.
        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1,
            attemptsPerLaunch: 4, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.OriginalPrecisionRequiresReferenceMode, status);
        Assert.Equal(0, counters.Attempts);
        Assert.Equal(0, counters.Launches);
    }

    /// <summary>
    /// The "not merely the obvious route" proof: a continued batch of one particle is sequential by
    /// construction (the same shape reference mode itself uses), yet the refusal does not special-case it —
    /// "Reference mode is batched mode degenerated" is a claim about <see cref="PrecisionKind.Binary64"/>
    /// only (root BOOT.md).
    /// </summary>
    [Fact]
    public void RunContinuedBatchWithOriginalPrecisionIsRefusedBeforeAnyAttempt()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        setup.Kind = PrecisionKind.Original;
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);

        // Mutation proof: without the Kind check, this call is the ordinary degenerate configuration other
        // tests (e.g. DeterminismTests, ReferenceModeEqualsContinuedBatchTests) already run to Ok on setups
        // of this shape, so removing the refusal turns the status assertion red.
        var status = engine.RunContinuedBatch(ref streams, attemptsPerLaunch: 1, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.OriginalPrecisionRequiresReferenceMode, status);
        Assert.Equal(0, counters.Attempts);
        Assert.Equal(0, counters.Launches);
    }

    [Fact]
    public void RunReferenceParticleWithOriginalPrecisionIsUnaffectedByTheBatchedRefusal()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        setup.Kind = PrecisionKind.Original;
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        var status = engine.RunReferenceParticle(ref streams, maxAttemptsPerParticle: 10, out var counters);

        Assert.Equal(BatchStatus.Ok, status);
        Assert.True(counters.Attempts >= 1);
    }
}
