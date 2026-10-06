using System.Diagnostics;
using PropStruct.Input;
using PropStruct.Random;
using PropStruct.Statistics;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The re-measurement BOOT.md's "Host-thread path" asks for once the per-particle layout avoids false
/// sharing (<c>ParticleControl</c>, the padded scratch stride of <see cref="Engine"/>'s own
/// <c>RunParticlesCore</c>): particle-kernel throughput on HPEPA3's cycle 1, on the host-thread path at 1,
/// 4, 8 and 16 threads, and reference mode's own throughput (inherently single-threaded). Never asserted
/// beyond "positive and finite" (root BOOT.md, "Performance": "recorded ... and never asserted"); the figures
/// are transcribed into <c>src/Execution/BOOT.md</c> by hand from this test's own output.
/// </summary>
/// <remarks>
/// Not attempted: pinning the 16-thread run to 8 specific physical cores. <see cref="Process.ProcessorAffinity"/>
/// is process-wide, and <c>dotnet test</c> runs every test class of this assembly in one process with xunit's
/// own parallelization across classes by default — narrowing it here for the few seconds of this one method
/// would also narrow every CUDA/CPU test running concurrently in another class, for reasons that have nothing
/// to do with them. A trustworthy physical-core measurement needs its own process (a benchmark, not an xunit
/// test in the shared run), which is beyond this pass; the 16-thread figure below already reports whatever
/// physical/logical mix the OS scheduler actually used.
/// </remarks>
[Collection(LongClassesSerialTests.Name)]
public sealed class HostThreadThroughputTests(ITestOutputHelper output)
{
    private const long RecordBudgetBytes = 2L << 30;
    private const int AttemptsPerLaunch = 256;
    private const long MaxAttemptsPerParticle = 10_000_000;
    private const int ReferenceModeParticleCount = 5000;
    private static readonly int[] ThreadCounts = [1, 4, 8, 16];

    [Fact]
    [Trait("Category", "Long")]
    public void HostThreadsThroughputOnHpepa3sCycle1AtSeveralThreadCounts()
    {
        foreach (var threads in ThreadCounts)
        {
            using var engine = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes, cpuThreads: threads);
            var ready = ReferenceFormulationDriver.PrepareThroughCycle0(engine, StreamLayout.Independent, seed: 0UL, "HPEPA3", AttemptsPerLaunch);
            var particlesPerCycle = ready.Formulation.ParticlesPerCycle;

            var stopwatch = Stopwatch.StartNew();
            var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, particlesPerCycle,
                AttemptsPerLaunch, MaxAttemptsPerParticle, out var counters);
            stopwatch.Stop();

            Assert.Equal(BatchStatus.Ok, status);
            var particlesPerSecond = particlesPerCycle / stopwatch.Elapsed.TotalSeconds;
            Assert.True(double.IsFinite(particlesPerSecond) && particlesPerSecond > 0);

            output.WriteLine($"host threads={threads}: {particlesPerCycle} accepted particles in {stopwatch.Elapsed.TotalSeconds:F3} s " +
                              $"= {particlesPerSecond:N0} particles/s (launches={counters.Launches}, attempts={counters.Attempts}, " +
                              $"attempts/particle={(double)counters.Attempts / particlesPerCycle:F2})");
        }
    }

    [Fact]
    [Trait("Category", "Long")]
    public void ReferenceModeThroughputOnHpepa3sCycle0()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "HPEPA3.dat");
        var formulation = DatFile.Read(path);
        var parameters = ModelParameters.Default;

        var prepareStatus = Setup.Prepare(formulation, parameters, Particle.PrecisionKind.Binary64, neighbourBudget: 10_000_000, pocketRedrawBudget: 1_000_000,
            out var setup, out var tables, out var draw, out var pending);
        Assert.Equal(SetupStatus.Ok, prepareStatus);

        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 24);
        engine.SampleSize(setup.SizeLaw, setup.FractionCount, tables.Bounds, tables.Cumulative, draw.X, draw.X1, out var dmax, out _);
        _ = Setup.CompleteEchoes(ref setup, pending, dmax);

        engine.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var streams = OriginalSeeds.Streams;
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < ReferenceModeParticleCount; i++)
        {
            var status = engine.RunReferenceParticle(ref streams, MaxAttemptsPerParticle, out _);
            Assert.Equal(BatchStatus.Ok, status);
        }

        stopwatch.Stop();

        var particlesPerSecond = ReferenceModeParticleCount / stopwatch.Elapsed.TotalSeconds;
        Assert.True(double.IsFinite(particlesPerSecond) && particlesPerSecond > 0);

        output.WriteLine($"reference mode: {ReferenceModeParticleCount} accepted particles in {stopwatch.Elapsed.TotalSeconds:F3} s " +
                          $"= {particlesPerSecond:N0} particles/s");
    }
}
