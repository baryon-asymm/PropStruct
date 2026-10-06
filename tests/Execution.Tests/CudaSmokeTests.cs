using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L0 (BOOT.md, Opus audit item 4, 2026-09-18): before this test, <c>Kernels.RunAttempts</c>,
/// <c>Kernels.DeriveStreams</c> and <c>Kernels.FoldField</c> reached the CUDA post-link only in
/// <c>Category=Long</c> rows (<see cref="TierTableTests"/>, <see cref="ThroughputMeasurementTests"/>), so a
/// run of the default command (no <c>Category=Long</c> filter) never actually compiled or ran the particle
/// kernel itself on CUDA — only <see cref="MathProbeTests"/>'s math probe did. This test is untagged, like
/// that one, so it runs in the default command too: a tiny constructed-setup batch through
/// <see cref="Engine.RunBatch"/> and <see cref="Engine.RunContinuedBatch"/>, plus
/// <see cref="Engine.SampleSize"/>, on a CUDA engine — reaching the post-link for real whenever CUDA is
/// available, and asserting the refusal (BOOT.md, "Errors") when it is not.
/// </summary>
public sealed class CudaSmokeTests
{
    [Fact]
    public void RunBatchRunContinuedBatchSampleSizeReachCudaOrAssertRefusal()
    {
        using var engine = Engine.Create(AcceleratorKind.Cuda, 1L << 20);
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();

        // SampleSize runs regardless of the engine's own binding (src/Execution/API.md, "Errors", Opus
        // audit item 10): it must not throw either way.
        engine.SampleSize(setup.SizeLaw, setup.FractionCount, bounds, cumulative, x: 0.37, x1: 0.61, out var diameter, out var fraction);
        Assert.InRange(diameter, Math.Min(bounds[0], bounds[1]), Math.Max(bounds[0], bounds[1]));

        engine.Load(in setup, bounds, cumulative, pocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        if (engine.Accelerator.CudaSkippedBecause is not null)
        {
            CudaRequirement.FailIfRequired(engine.Accelerator.CudaSkippedBecause);
            Assert.Equal(BatchStatus.AcceleratorUnavailable,
                engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 1, attemptsPerLaunch: 1, maxAttemptsPerParticle: 10, out _));

            var refusedStreams = OriginalSeeds.ForParticle(0UL, 0UL);
            Assert.Equal(BatchStatus.AcceleratorUnavailable,
                engine.RunContinuedBatch(ref refusedStreams, attemptsPerLaunch: 1, maxAttemptsPerParticle: 10, out _));
            return;
        }

        Assert.True(engine.Accelerator.LibDeviceLinked);

        var batchStatus = engine.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, particleCount: 5,
            attemptsPerLaunch: 4, maxAttemptsPerParticle: 1_000_000, out var batchCounters);
        Assert.Equal(BatchStatus.Ok, batchStatus);
        Assert.True(batchCounters.Attempts >= 5);

        var streams = OriginalSeeds.ForParticle(0UL, 100UL);
        var continuedStatus = engine.RunContinuedBatch(ref streams, attemptsPerLaunch: 1, maxAttemptsPerParticle: 1_000_000, out var continuedCounters);
        Assert.Equal(BatchStatus.Ok, continuedStatus);
        Assert.True(continuedCounters.Attempts >= 1);
    }
}
