using System.Diagnostics;
using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The early measurement (`src/Execution/BOOT.md`, "Early measurement first"; root BOOT.md, Constraints,
/// "Performance"): particle-kernel throughput (accepted particles per second) on HPEPA3's cycle 1,
/// CUDA and the CPU accelerator with all cores, recorded in <c>src/Execution/BOOT.md</c> before any kernel
/// is tuned. Never asserted beyond "positive and finite" (root BOOT.md, "Performance": "recorded ... and
/// never asserted"); the actual figures are transcribed into BOOT.md by hand from this test's own output.
/// </summary>
[Collection(LongClassesSerialTests.Name)]
public sealed class ThroughputMeasurementTests(ITestOutputHelper output)
{
    private const long RecordBudgetBytes = 2L << 30; // root BOOT.md default: the whole cycle, capped here
    private const int AttemptsPerLaunch = 256;
    private const long MaxAttemptsPerParticle = 10_000_000;

    /// <summary>
    /// Pins the ILGPU CPU accelerator explicitly (`forceIlgpuKernelsOnCpu: true`), so this row keeps
    /// measuring what "## Throughput measurement" in `src/Execution/BOOT.md` has always named: the
    /// kernel-launch path, not `AcceleratorKind.Cpu`'s current default (BOOT.md, "Host-thread path",
    /// 2026-09-19: the host-thread path is now the default; its own throughput is
    /// `HostThreadThroughputTests`' concern, in "## Host-thread throughput"). Without pinning, this test's
    /// own behaviour would have silently started measuring the host-thread path the moment the default
    /// changed, while still being labelled and reported against the accelerator's own historical figure.
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void CpuThroughputOnHpepa3sCycle1()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, forceIlgpuKernelsOnCpu: true);
        Measure(engine, "CPU (" + engine.Accelerator.Name + ")");
    }

    [Fact]
    [Trait("Category", "Long")]
    public void CudaThroughputOnHpepa3sCycle1()
    {
        using var engine = Engine.Create(AcceleratorKind.Cuda, RecordBudgetBytes);
        if (engine.Accelerator.CudaSkippedBecause is not null) // Opus audit item 3, 2026-09-18: this engine's own outcome, not the ambient AcceleratorChoice.CudaForbidden
        {
            CudaRequirement.FailIfRequired(engine.Accelerator.CudaSkippedBecause);
            Assert.Equal(BatchStatus.AcceleratorUnavailable, RunTrivialBatchToProveRefusal(engine));
            output.WriteLine("CUDA forbidden by PROPSTRUCT_NO_CUDA=1; throughput not measured in this run.");
            return;
        }

        Measure(engine, "CUDA (" + engine.Accelerator.Name + ")");
    }

    private void Measure(Engine engine, string label)
    {
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(engine, StreamLayout.Independent, seed: 0UL, "HPEPA3", AttemptsPerLaunch);
        var particlesPerCycle = ready.Formulation.ParticlesPerCycle;

        var stopwatch = Stopwatch.StartNew();
        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, particlesPerCycle,
            AttemptsPerLaunch, MaxAttemptsPerParticle, out var counters);
        stopwatch.Stop();

        Assert.Equal(BatchStatus.Ok, status);
        var particlesPerSecond = particlesPerCycle / stopwatch.Elapsed.TotalSeconds;
        Assert.True(double.IsFinite(particlesPerSecond) && particlesPerSecond > 0);

        output.WriteLine($"{label}: {particlesPerCycle} accepted particles in {stopwatch.Elapsed.TotalSeconds:F3} s " +
                          $"= {particlesPerSecond:N0} particles/s (launches={counters.Launches}, attempts={counters.Attempts}, " +
                          $"attempts/particle={(double)counters.Attempts / particlesPerCycle:F2})");
    }

    private static BatchStatus RunTrivialBatchToProveRefusal(Engine engine)
    {
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        return engine.RunBatch(StreamLayout.Independent, 0UL, 0UL, 1, 1, 10, out _);
    }
}
