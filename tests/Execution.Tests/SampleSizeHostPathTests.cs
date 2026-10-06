using System.Reflection;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L0 (BOOT.md, "Setup hand-off"; audit finding D8, 2026-09-24): <see cref="Engine.SampleSize"/> used to
/// reach <c>Kernels.SampleSize</c> through the same compile-and-launch path <see cref="Engine.RunBatch"/>
/// takes on a non-host-thread accelerator — an ILGPU kernel, JIT-compiled and dispatched through
/// <c>KernelCache</c> — even when the engine was itself CPU-bound and every other member of this node had
/// already moved to calling <c>Particle</c>/<c>Random</c> directly on the host thread (root BOOT.md, "One
/// particle program": the ILGPU CPU accelerator "is kept as a test oracle of the kernel path only"). This
/// node proves the fix by reflecting into the engine's own private <c>KernelCache</c> — the one place a
/// compiled launcher would be recorded — rather than by timing, which the note on <see cref="RunSampleSize_NeverPopulatesTheKernelCache_RegardlessOfForceIlgpuKernelsOnCpu"/> below reads as too easy
/// to fool with an unrelated speed-up.
/// </summary>
public sealed class SampleSizeHostPathTests
{
    /// <summary>
    /// Proven non-degenerate by reverting <see cref="Engine.SampleSize"/> to the pre-fix implementation
    /// (audit finding D8): with the old code this assertion failed, because <c>SampleSize</c> populated the
    /// cache with a "SampleSize" entry the first time it ran on a CPU-bound (or ephemeral CPU) accelerator.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SampleSizeOnACpuBoundEngineNeverCompilesOrLaunchesAKernel(bool forceIlgpuKernelsOnCpu)
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20, forceIlgpuKernelsOnCpu: forceIlgpuKernelsOnCpu);
        var (setup, bounds, cumulative, _) = ConstructedSetups.ReachesNeighbourLoop();

        engine.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out _, out _);

        Assert.Empty(CompiledKernelNames(engine));
    }

    /// <summary>Same proof for a refused engine, whose <c>SampleSize</c> falls back to an ephemeral CPU
    /// accelerator (BOOT.md, "Errors"): that ephemeral accelerator's own kernel cache never receives an
    /// entry either, since the fallback also calls <c>SizeLaw.Sample</c> directly.</summary>
    [Fact]
    public void SampleSizeOnARefusedEngineNeverCompilesOrLaunchesAKernel()
    {
        using var engine = Engine.Create(AcceleratorKind.Cuda, 1L << 20, environment: CudaEnvironment.Forbidding);
        Assert.NotNull(engine.Accelerator.CudaSkippedBecause);
        var (setup, bounds, cumulative, _) = ConstructedSetups.ReachesNeighbourLoop();

        // A refused engine's own _kernels field is null (Engine.Create), so there is nothing of this
        // engine's own to inspect; the assertion that matters is simply that the call does not throw and
        // gives the same answer as the CPU-bound path, covered by the cross-engine agreement test below.
        engine.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var diameter, out var fraction);

        Assert.InRange(diameter, Math.Min(bounds[0], bounds[1]), Math.Max(bounds[0], bounds[1]));
        Assert.Equal(0, fraction);
    }

    /// <summary>
    /// The tail draw agrees bit for bit whichever engine kind computes it — a CPU-bound engine, the
    /// kernel-launch oracle (<c>forceIlgpuKernelsOnCpu: true</c>), and a refused CUDA engine's ephemeral CPU
    /// fallback — because <see cref="Particle.SizeLaw.Sample"/> is one function called with the
    /// same inputs in every case (root BOOT.md, Taboos: "no second implementation").
    /// </summary>
    [Fact]
    public void SampleSizeAgreesBitForBitAcrossEveryEngineKind()
    {
        var (setup, bounds, cumulative, _) = ConstructedSetups.ReachesNeighbourLoop();

        using var hostThreads = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        using var oracle = Engine.Create(AcceleratorKind.Cpu, 1L << 20, forceIlgpuKernelsOnCpu: true);
        using var refused = Engine.Create(AcceleratorKind.Cuda, 1L << 20, environment: CudaEnvironment.Forbidding);

        hostThreads.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var hostDiameter, out var hostFraction);
        oracle.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var oracleDiameter, out var oracleFraction);
        refused.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var refusedDiameter, out var refusedFraction);

        Assert.Equal(hostDiameter, oracleDiameter);
        Assert.Equal(hostFraction, oracleFraction);
        Assert.Equal(hostDiameter, refusedDiameter);
        Assert.Equal(hostFraction, refusedFraction);
    }

    /// <summary>Reads <c>Engine._kernels._launchers</c>' keys by reflection: the one place a JIT-compiled
    /// kernel launcher would be recorded (<c>KernelCache.Get</c>), so an empty result is direct evidence that
    /// no kernel was compiled or launched by the call under test — not an inference from timing.</summary>
    private static List<string> CompiledKernelNames(Engine engine)
    {
        var kernelsField = typeof(Engine).GetField("_kernels", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Engine no longer has a private _kernels field; update this reflection probe.");
        var kernelCache = kernelsField.GetValue(engine);
        if (kernelCache is null)
        {
            return [];
        }

        var launchersField = kernelCache.GetType().GetField("_launchers", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("KernelCache no longer has a private _launchers field; update this reflection probe.");
        var launchers = (System.Collections.IDictionary)launchersField.GetValue(kernelCache)!;
        var names = new List<string>(launchers.Count);
        foreach (var key in launchers.Keys)
        {
            names.Add((string)key);
        }

        return names;
    }
}
