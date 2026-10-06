using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L0 (BOOT.md): accelerator choice, the kill switch, the fallback reason, and the argument/state
/// errors of <see cref="Engine"/>, checked against <c>src/Execution/API.md</c>.
/// </summary>
public sealed class AcceleratorTests
{
    [Fact]
    public void DiscoverAlwaysListsTheCpuAccelerator()
    {
        var found = AcceleratorProbe.Discover();

        Assert.Contains(found, info => info.Kind == AcceleratorKind.Cpu && info.CudaSkippedBecause is null);
    }

    [Fact]
    public void KillSwitchAutoFallsBackToCpuWithReason()
    {
        using var engine = Engine.Create(AcceleratorKind.Auto, 1L << 20, environment: CudaEnvironment.Forbidding);

        Assert.Equal(AcceleratorKind.Cpu, engine.Accelerator.Kind);
        Assert.NotNull(engine.Accelerator.CudaSkippedBecause);
        Assert.Contains(AcceleratorChoice.NoCudaVariable, engine.Accelerator.CudaSkippedBecause);
    }

    [Fact]
    public void KillSwitchCudaRequestedRefusesEveryRunCall()
    {
        using var engine = Engine.Create(AcceleratorKind.Cuda, 1L << 20, environment: CudaEnvironment.Forbidding);

        Assert.Equal(AcceleratorKind.Cuda, engine.Accelerator.Kind);
        Assert.NotNull(engine.Accelerator.CudaSkippedBecause);
        Assert.False(engine.Accelerator.LibDeviceLinked);

        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);

        Assert.Equal(BatchStatus.AcceleratorUnavailable, engine.RunBatch(StreamLayout.Independent, 0UL, 0UL, 1, 1, 10, out _));

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        Assert.Equal(BatchStatus.AcceleratorUnavailable, engine.RunContinuedBatch(ref streams, 1, 10, out _));
        Assert.Equal(BatchStatus.AcceleratorUnavailable, engine.RunReferenceParticle(ref streams, 10, out _));
    }

    /// <summary>
    /// L0 (BOOT.md, Opus audit item 10, 2026-09-18): <see cref="Engine.SampleSize"/> is <c>Simulation</c>'s
    /// first call on a freshly created engine (<c>Simulation/API.md</c>, "Setup hand-off"), before
    /// <see cref="Engine.Load"/>. It used to require a bound accelerator like every other member, so a
    /// caller that created its engine with <see cref="AcceleratorKind.Cuda"/> on a machine without CUDA
    /// would fail at this very first call, before its particle kernel — which may run perfectly well on the
    /// CPU accelerator instead — ever got a chance to run. <see cref="Particle.SizeLaw.Sample"/>
    /// has no accelerator-specific behaviour to probe (unlike <see cref="Engine.ProbeMath"/>, which still
    /// throws on a refused engine: there is no CUDA answer to compare against when CUDA refused to run), so
    /// it now runs regardless, on an ephemeral CPU accelerator when the engine's own binding is refused.
    /// </summary>
    [Fact]
    public void SampleSizeRunsOnARefusedCudaEngineAndAgreesWithTheCpuAccelerator()
    {
        using var refused = Engine.Create(AcceleratorKind.Cuda, 1L << 20, environment: CudaEnvironment.Forbidding);
        Assert.NotNull(refused.Accelerator.CudaSkippedBecause);

        using var cpu = Engine.Create(AcceleratorKind.Cpu, 1L << 20);

        var (setup, bounds, cumulative, _) = ConstructedSetups.ReachesNeighbourLoop();

        refused.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var refusedDiameter, out var refusedFraction);
        cpu.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var cpuDiameter, out var cpuFraction);

        Assert.Equal(cpuDiameter, refusedDiameter);
        Assert.Equal(cpuFraction, refusedFraction);
    }

    [Fact]
    public void KillSwitchDiscoverDoesNotListCuda() => Assert.DoesNotContain(AcceleratorProbe.Discover(CudaEnvironment.Forbidding), info => info.Kind == AcceleratorKind.Cuda);

    /// <summary>
    /// L0 (BOOT.md, Opus audit item 3, 2026-09-18): branches on <c>engine.Accelerator.CudaSkippedBecause</c>,
    /// this specific engine's own reported outcome, rather than the ambient <see cref="AcceleratorChoice.CudaForbidden"/>
    /// — a second, concurrently running test class's own kill-switch injection cannot affect what this
    /// engine, created with the real process environment, actually got.
    /// </summary>
    [Fact]
    public void AutoWithoutKillSwitchBindsCudaOnThisMachine()
    {
        // The reference machine (root BOOT.md, Constraints: "Platform") always has CUDA; this proves Auto
        // does not fall back when nothing forbids CUDA. When the real process's PROPSTRUCT_NO_CUDA=1 forbids
        // it (this node's own fast-set command sets it, CLAUDE.md), the refusal is asserted instead of
        // skipping (BOOT.md, Invariants) — the same rule KillSwitch_AutoFallsBackToCpu proves with an
        // injected environment instead of the real one.
        using var engine = Engine.Create(AcceleratorKind.Auto, 1L << 20);

        if (engine.Accelerator.CudaSkippedBecause is not null)
        {
            CudaRequirement.FailIfRequired(engine.Accelerator.CudaSkippedBecause);
            Assert.Equal(AcceleratorKind.Cpu, engine.Accelerator.Kind);
            return;
        }

        Assert.Equal(AcceleratorKind.Cuda, engine.Accelerator.Kind);
        Assert.True(engine.Accelerator.LibDeviceLinked);
    }

    [Fact]
    public void ReferenceModeOnACudaEngineReturnsAcceleratorUnavailable()
    {
        using var engine = Engine.Create(AcceleratorKind.Cuda, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);

        // On this machine Cuda binds for real (asserted below); on a machine or a run without it, Create
        // already produced a refused engine, and reference mode must refuse it exactly the same way.
        if (engine.Accelerator.CudaSkippedBecause is null)
        {
            Assert.Equal(AcceleratorKind.Cuda, engine.Accelerator.Kind);
            Assert.True(engine.Accelerator.LibDeviceLinked);
        }
        else
        {
            CudaRequirement.FailIfRequired(engine.Accelerator.CudaSkippedBecause);
        }

        Assert.Equal(BatchStatus.AcceleratorUnavailable, engine.RunReferenceParticle(ref streams, 10, out _));
    }

    [Fact]
    public void CreateRejectsANonPositiveRecordBudget()
    {
        _ = Assert.Throws<ArgumentException>(() => Engine.Create(AcceleratorKind.Cpu, 0));
        _ = Assert.Throws<ArgumentException>(() => Engine.Create(AcceleratorKind.Cpu, -1));
    }

    [Fact]
    public void CallsBeforeLoadThrowInvalidOperation()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);

        _ = Assert.Throws<InvalidOperationException>(() => engine.ReadTotals([], []));
        _ = Assert.Throws<InvalidOperationException>(() => engine.WriteTotals([], []));
        _ = Assert.Throws<InvalidOperationException>(() => engine.SetCycle(0, 0.0, []));

        var streams = OriginalSeeds.ForParticle(0UL, 0UL);
        _ = Assert.Throws<InvalidOperationException>(() => engine.RunBatch(StreamLayout.Independent, 0UL, 0UL, 1, 1, 10, out _));
        _ = Assert.Throws<InvalidOperationException>(() => engine.RunContinuedBatch(ref streams, 1, 10, out _));
        _ = Assert.Throws<InvalidOperationException>(() => engine.RunReferenceParticle(ref streams, 10, out _));
    }

    [Fact]
    public void LoadRejectsMismatchedArrayLengths()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();

        _ = Assert.Throws<ArgumentException>(() => engine.Load(in setup, new double[bounds.Length + 1], cumulative, pocketForming));
        _ = Assert.Throws<ArgumentException>(() => engine.Load(in setup, bounds, new double[cumulative.Length + 1], pocketForming));
        _ = Assert.Throws<ArgumentException>(() => engine.Load(in setup, bounds, cumulative, new byte[pocketForming.Length + 1]));
    }

    [Fact]
    public void RunBatchRejectsAParticleCountAboveMaxBatchSize()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 4096); // a tiny budget: MaxBatchSize collapses to 1
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);

        Assert.Equal(1, engine.MaxBatchSize);
        _ = Assert.Throws<ArgumentException>(() => engine.RunBatch(StreamLayout.Independent, 0UL, 0UL, 2, 1, 10, out _));
    }

    /// <summary>
    /// L1 (BOOT.md, Opus audit item 8, 2026-09-18): <c>Particle.Fold.AddField</c> indexes its record
    /// buffer as <c>particle * recordLength + field</c>, all in <see langword="int"/>
    /// (<c>Particle/API.md</c>, "Fold by field"). A record-budget large enough that the byte-budget alone
    /// would allow a batch of <see cref="int.MaxValue"/> particles must still be capped at
    /// <c>int.MaxValue / RecordLength</c>, because <c>Execution</c> owns the budget decision and
    /// <c>Fold</c> has no way to refuse an oversized batch itself.
    /// </summary>
    [Fact]
    public void MaxBatchSizeIsCappedSoFoldsIndexingCannotOverflowInt()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, long.MaxValue); // budget alone would allow far more than int.MaxValue particles
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);

        var recordLength = setup.Layout.RecordLength;
        var foldSafeMax = int.MaxValue / recordLength;

        Assert.True(recordLength > 1, "the setup must have a real record length for this bound to be below int.MaxValue.");
        Assert.True(foldSafeMax < int.MaxValue, "the fold-safe cap must actually be tighter than the plain int.MaxValue cap for this assertion to be informative.");
        Assert.Equal(foldSafeMax, engine.MaxBatchSize);
    }

    [Fact]
    public void RunBatchRejectsNonPositiveBudgets()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);

        _ = Assert.Throws<ArgumentException>(() => engine.RunBatch(StreamLayout.Independent, 0UL, 0UL, 1, 0, 10, out _));
        _ = Assert.Throws<ArgumentException>(() => engine.RunBatch(StreamLayout.Independent, 0UL, 0UL, 1, 1, 0, out _));
    }
}
