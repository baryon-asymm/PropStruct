using PropStruct.Particle;
using PropStruct.Random;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md, acceptance criterion "A launch that leaves an active particle's attempt count unchanged
/// ends the batch with <c>ParticleNotRun</c>"): the host trusts a downloaded outcome byte only for a
/// particle whose attempt count grew in that launch. A zeroed <see cref="ParticleControl"/> reads as
/// <see cref="AttemptOutcome.Accepted"/>, so before this check a particle the launch never ran was folded
/// as accepted at its first launch, and relaunched forever at a later one.
///
/// Proven twice (root BOOT.md, Taboos): the answer is right on unmutated runs, and red on the recorded
/// mutation, in <c>BOOT.md</c> of this node, "## Launch-advance check (2026-10-02)".
/// </summary>
public sealed class ParticleNotRunTests
{
    private const int ParticleCount = 16;

    private static ParticleControl[] Controls(params (long Attempts, AttemptOutcome Outcome)[] particles) =>
        particles.Select(p => new ParticleControl { AttemptCount = p.Attempts, Outcome = (byte)p.Outcome }).ToArray();

    [Fact]
    public void AnActiveParticleWhoseCountGrewRan()
    {
        var controls = Controls((2, AttemptOutcome.Accepted), (5, AttemptOutcome.RestartedAfterLoop));

        Assert.True(Engine.EveryActiveParticleRan([0, 1], [1, 3], controls));
    }

    /// <summary>First launch: the untouched control is the zero value, whose outcome is Accepted.</summary>
    [Fact]
    public void AnActiveParticleAtItsFirstLaunchWithCountZeroDidNotRun()
    {
        var controls = Controls((1, AttemptOutcome.Accepted), (0, AttemptOutcome.Accepted));

        Assert.False(Engine.EveryActiveParticleRan([0, 1], [0, 0], controls));
    }

    /// <summary>Relaunch: the stale outcome is the previous launch's, a restart that would be relaunched forever.</summary>
    [Fact]
    public void AnActiveParticleAtARelaunchWithAnUnchangedCountDidNotRun()
    {
        var controls = Controls((3, AttemptOutcome.RestartedInsideLoop), (4, AttemptOutcome.RestartedAfterLoop));

        Assert.False(Engine.EveryActiveParticleRan([0, 1], [3, 3], controls));
    }

    /// <summary>A particle finished in an earlier launch is not in the active list, so its frozen count is no skip.</summary>
    [Fact]
    public void AParticleOutsideTheActiveListNeedsNoGrowth()
    {
        var controls = Controls((7, AttemptOutcome.Accepted), (6, AttemptOutcome.RestartedAfterLoop));

        Assert.True(Engine.EveryActiveParticleRan([1], [7, 5], controls));
    }

    /// <summary>
    /// Right on an unmutated run, with the answer known beforehand: a batch of sixteen on a setup where every
    /// particle ends accepted returns <c>Ok</c> and at least one attempt per particle, with the host-thread
    /// path's counters equal to the ILGPU-kernel oracle's, at one attempt per launch (relaunches) and at many
    /// (one launch).
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    public void AnUnmutatedBatchReturnsOkWithTheOraclesCounters(int attemptsPerLaunch)
    {
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        var hostCounters = RunConstructed(forceIlgpuKernelsOnCpu: false, attemptsPerLaunch, in setup, bounds, cumulative, pocketForming, out var hostStatus);
        var oracleCounters = RunConstructed(forceIlgpuKernelsOnCpu: true, attemptsPerLaunch, in setup, bounds, cumulative, pocketForming, out var oracleStatus);

        Assert.Equal(BatchStatus.Ok, hostStatus);
        Assert.Equal(BatchStatus.Ok, oracleStatus);
        Assert.True(hostCounters.Attempts >= ParticleCount);
        Assert.Equal(oracleCounters, hostCounters);
    }

    private static BatchCounters RunConstructed(
        bool forceIlgpuKernelsOnCpu, int attemptsPerLaunch, in ModelSetup setup, double[] bounds, double[] cumulative,
        byte[] pocketForming, out BatchStatus status)
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20, forceIlgpuKernelsOnCpu: forceIlgpuKernelsOnCpu);
        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, ParticleCount,
            attemptsPerLaunch, maxAttemptsPerParticle: 1_000_000, out var counters);
        return counters;
    }
}
