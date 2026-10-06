using PropStruct.Input;
using PropStruct.Random;
using PropStruct.Statistics;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L2 (BOOT.md): reference mode equals a continued batch with one attempt per launch, bit for bit, over
/// the first 1000 particles of HPEPA3 (root BOOT.md, "Reference mode is batched mode degenerated"). The
/// first 1000 particles are cycle 0's own first 1000: cycle 0 needs no prior cycle statistics (Particle's
/// own attempt skips every read of <c>pdoksmall</c>/<c>Dmaxxx</c> when the cycle flag is 0), so both paths
/// start from the true beginning of the generator sequence, with no driver needed beyond
/// <c>Statistics.Setup.Prepare</c> itself.
///
/// <see cref="IndependentSeeds.Streams"/>, not <see cref="OriginalSeeds.Streams"/> (BOOT.md, "## Invariants",
/// the 2026-09-21 stream-layout refusal): <see cref="Engine.RunContinuedBatch"/> is now
/// <see cref="ExecutionMode.Batched"/> machinery that refuses the <c>Original</c> layout unconditionally, so
/// the equivalence this class proves — reference mode equals a continued batch, bit for bit — is proven
/// under the layout `RunContinuedBatch` still accepts. The claim is about the continuation mechanism, not
/// about which layout produced the starting streams, so nothing about what this class checks changes with
/// the switch.
/// </summary>
public sealed class ReferenceModeEqualsContinuedBatchTests
{
    private const int ParticleCount = 1000;
    private const long MaxAttemptsPerParticle = 1_000_000;

    [Fact]
    public void ReferenceModeEqualsContinuedBatchBitForBitOverHpepa3sFirst1000Particles()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "HPEPA3.dat");
        var formulation = DatFile.Read(path);
        var parameters = ModelParameters.Default;

        var prepareStatus = Setup.Prepare(formulation, parameters, Particle.PrecisionKind.Binary64, neighbourBudget: 10_000_000, pocketRedrawBudget: 1_000_000,
            out var setup, out var tables, out var draw, out var pending);
        Assert.Equal(SetupStatus.Ok, prepareStatus);

        using var reference = Engine.Create(AcceleratorKind.Cpu, 1L << 24);
        reference.SampleSize(setup.SizeLaw, setup.FractionCount, tables.Bounds, tables.Cumulative, draw.X, draw.X1, out var dmax, out _);
        _ = Setup.CompleteEchoes(ref setup, pending, dmax); // writes setup.Dmax in place; both engines share this one completed setup

        reference.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);
        reference.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        using var continued = Engine.Create(AcceleratorKind.Cpu, 1L << 24);
        continued.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);
        continued.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var referenceStreams = IndependentSeeds.Streams;
        var continuedStreams = IndependentSeeds.Streams;

        for (var particle = 0; particle < ParticleCount; particle++)
        {
            var referenceStatus = reference.RunReferenceParticle(ref referenceStreams, MaxAttemptsPerParticle, out var referenceCounters);
            var continuedStatus = continued.RunContinuedBatch(ref continuedStreams, attemptsPerLaunch: 1, MaxAttemptsPerParticle, out var continuedCounters);

            Assert.True(referenceStatus == BatchStatus.Ok, $"particle {particle}: reference status {referenceStatus}");
            Assert.Equal(referenceStatus, continuedStatus);
            Assert.Equal(referenceCounters, continuedCounters);
            AssertStreamsEqual(referenceStreams, continuedStreams, particle);
        }

        var referenceIntegerTotals = new long[setup.Layout.IntegerLength];
        var referenceRealTotals = new double[setup.Layout.RecordLength];
        reference.ReadTotals(referenceIntegerTotals, referenceRealTotals);

        var continuedIntegerTotals = new long[setup.Layout.IntegerLength];
        var continuedRealTotals = new double[setup.Layout.RecordLength];
        continued.ReadTotals(continuedIntegerTotals, continuedRealTotals);

        Assert.Equal(referenceIntegerTotals, continuedIntegerTotals);
        Assert.Equal(referenceRealTotals, continuedRealTotals);
    }

    private static void AssertStreamsEqual(StreamSet a, StreamSet b, int particle)
    {
        AssertStateEqual(a.S1, b.S1, particle, "S1");
        AssertStateEqual(a.S2, b.S2, particle, "S2");
        AssertStateEqual(a.S3, b.S3, particle, "S3");
        AssertStateEqual(a.S4, b.S4, particle, "S4");
        AssertStateEqual(a.S5, b.S5, particle, "S5");
        AssertStateEqual(a.S6, b.S6, particle, "S6");
    }

    private static void AssertStateEqual(Mcg128State a, Mcg128State b, int particle, string stream) => Assert.True(a.Low == b.Low && a.High == b.High, $"particle {particle}, stream {stream}: {a.High:X16}{a.Low:X16} != {b.High:X16}{b.Low:X16}");
}
