using PropStruct.Particle;
using PropStruct.Random;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L2 (wave-8 assignment, background: "tests green, property false"). Every existing L2 row checks that two
/// execution *paths* agree with each other (reference mode against a continued batch, host threads against
/// the ILGPU-kernel-launch oracle) or that a refresh *count* matches an expected delta; none of them checks
/// the generator itself against an independent oracle over a real run. This class does: after running real
/// HPEPA3 particles, it jumps each of the six original stream states ahead by the number of draws the run's
/// own integer totals counted for that stream (<see cref="Mcg128.Advance"/>, <c>src/Random/API.md</c>) and
/// asserts bit equality with the streams <see cref="Engine"/> actually produced. A counter that under- or
/// over-counts its stream's draws — the exact shape of a silent drift, since <c>Attempt.Run</c> never
/// exposes intermediate stream state for a test to sample — would leave this identity false while every
/// existing L2 row stays green (none of them reads a stream state against a counter at all).
///
/// **Stream-to-counter mapping** (derived from <c>src/Particle/API.md</c>'s "Accumulator layout" and
/// <c>src/Particle/BOOT.md</c>'s "Attempt structure"/"Integer totals" — this class does not read
/// <c>Attempt.cs</c> to derive it, only to build the one temporary mutation of the non-degeneracy proof
/// below, AGENTS.md §3). <c>src/Particle/BOOT.md</c>, "Draws in the original order from the original
/// streams": "Stream 1 → X, 2 → X0, 3 → X1, 4 → X2, 5 → X21, 6 → X3 and X4 (shared)." Particle's own
/// "Attempt structure" names where each draw
/// happens and Particle's "Integer totals" table gives the Fortran lines that write
/// <c>NFX, NFY, NFZ, NFQ, NFW</c> as the single combined list "463, 498, 518, 622, 662, 698" — six lines for
/// five counters, because one counter (`NFW`, below) is written at two mutually exclusive lines, not because
/// the mapping is 1:1 by line position:
///
/// | Fortran line | Block (`## Attempt structure`) | Draw(s) | Counter | Stream(s) |
/// |---|---|---|---|---|
/// | 463 | `base` (448–484): "X, X0 → SIZE → Dr, fraction" | X, X0 (always paired: the base block draws exactly one of each, every time it runs) | `Nfx` | S1 (X), S2 (X0) |
/// | 498 | `loop 501` (487–501): "X1 (redraw if 1.0)" | X1, once per distance draw (once per `loop 501` iteration) | `Nfy` | S3 (X1) |
/// | 518 | `345` (503–543): "X2, X21 → SIZE → Db" | X2, X21 (always paired: one neighbour draw is exactly one of each) | `Nfz` | S4 (X2), S5 (X21) |
/// | 622 | `bridge` (580–667): "451: X3 → DM → Dkarm → VM (JJ = 0 → 451, the XSS5/NFQ updates repeat)" | X3; the same line re-executes (and `Nfq` increments again) on the `JJ = 0` retry named in the same sentence | `Nfq` | S6 (X3) |
/// | 662, 698 | `bridge`'s own "Var#2: X4 vs k8/k7·pdoksmall → 550" (626–667) and `pocket 502`'s own "Var#2: X4 < pdoksmall → 333" (671–703) | X4, drawn in exactly one of these two mutually exclusive branches per neighbour (a neighbour either continues as a bridge or falls through to the pocket branch, `## Attempt structure`'s own "551" block: "AA > AK3·max → 502") | `Nfw` | S6 (X4) |
///
/// Consequence: streams S1/S2 (X, X0) always draw exactly `Nfx` times each; S3 (X1) draws `Nfy` times; S4/S5
/// (X2, X21) always draw exactly `Nfz` times each; S6 carries *both* X3 and X4, so its own total draw count
/// is `Nfq + Nfw`, not either counter alone — this is the one place a stream's own draw count is not a
/// single counter's value, and <see cref="AssertJumpAheadMatchesActual"/> is written accordingly.
/// </summary>
[Collection(LongClassesSerialTests.Name)]
public sealed class GeneratorStateIdentityTests(ITestOutputHelper output)
{
    private const int ParticleCount = 2000;
    private const long MaxAttemptsPerParticle = 10_000_000;

    // Cycle 0 completed through the same driver every other L2 row of this node uses
    // (`ReferenceFormulationDriver.PrepareThroughCycle0`, batched mode with per-particle jump-ahead); the
    // 2000-particle segment this test actually counts draws over runs entirely in cycle 1, the same setup
    // `Qks1RefreshUnderCycle1Tests` uses and for the same reason (QKS1 must be live, and cycle 1 is where a
    // real formulation's attempts are deep enough to draw many times from every stream, not just once).
    private static (Engine Engine, AccumulatorLayout Layout, StreamSet InitialStreams, long[] StartInteger) PrepareCycle1(
        Engine warmup, StreamLayout layout, Engine engine)
    {
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, layout, seed: 0UL, "HPEPA3");
        var setup = ready.Setup;
        engine.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        engine.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        engine.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(engine.DebugReadQks1(), v => v != 0.0);

        var startInteger = new long[setup.Layout.IntegerLength];
        var startReal = new double[setup.Layout.RecordLength];
        engine.ReadTotals(startInteger, startReal);

        var initialStreams = StreamSeeds.ForParticle(layout, 0UL, ready.NextOrdinal);
        return (engine, setup.Layout, initialStreams, startInteger);
    }

    [Fact]
    [Trait("Category", "Long")]
    public void ReferenceModeFinalStreamStatesEqualInitialStatesJumpedByCountedDrawsOverHpepa3sCycle1()
    {
        using var warmup = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        using var engine = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        var (_, layout, initialStreams, startInteger) = PrepareCycle1(warmup, StreamLayout.Independent, engine);

        var streams = initialStreams;
        for (var i = 0; i < ParticleCount; i++)
        {
            var status = engine.RunReferenceParticle(ref streams, MaxAttemptsPerParticle, out _);
            Assert.Equal(BatchStatus.Ok, status);
        }

        var finalInteger = new long[layout.IntegerLength];
        var finalReal = new double[layout.RecordLength];
        engine.ReadTotals(finalInteger, finalReal);

        AssertJumpAheadMatchesActual(layout, initialStreams, startInteger, finalInteger, streams, output, "reference mode");
    }

    // "Also do it for a continued batch" (task instructions): RunContinuedBatch is "a batch of one with the
    // caller's streams, returned advanced" (src/Execution/API.md) — the same per-particle continuation
    // reference mode uses, but through the batched-mode launch/relaunch machinery with a real
    // attemptsPerLaunch budget instead of one attempt per launch. attemptsPerLaunch = 8 forces at least one
    // relaunch for a nontrivial share of HPEPA3's own cycle-1 particles (measured elsewhere in this node,
    // `Qks1RefreshUnderCycle1Tests`: more attempts than particles over the same slice), so this row does not
    // merely repeat the reference-mode row under another name.
    [Fact]
    [Trait("Category", "Long")]
    public void ContinuedBatchFinalStreamStatesEqualInitialStatesJumpedByCountedDrawsOverHpepa3sCycle1()
    {
        const int attemptsPerLaunch = 8;
        using var warmup = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        using var engine = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        var (_, layout, initialStreams, startInteger) = PrepareCycle1(warmup, StreamLayout.Independent, engine);

        var streams = initialStreams;
        for (var i = 0; i < ParticleCount; i++)
        {
            var status = engine.RunContinuedBatch(ref streams, attemptsPerLaunch, MaxAttemptsPerParticle, out _);
            Assert.Equal(BatchStatus.Ok, status);
        }

        var finalInteger = new long[layout.IntegerLength];
        var finalReal = new double[layout.RecordLength];
        engine.ReadTotals(finalInteger, finalReal);

        AssertJumpAheadMatchesActual(layout, initialStreams, startInteger, finalInteger, streams, output, "continued batch");
    }

    private static void AssertJumpAheadMatchesActual(
        AccumulatorLayout layout, StreamSet initial, long[] startInteger, long[] finalInteger, StreamSet actual,
        ITestOutputHelper output, string label)
    {
        var deltaNfx = finalInteger[layout.Nfx] - startInteger[layout.Nfx];
        var deltaNfy = finalInteger[layout.Nfy] - startInteger[layout.Nfy];
        var deltaNfz = finalInteger[layout.Nfz] - startInteger[layout.Nfz];
        var deltaNfq = finalInteger[layout.Nfq] - startInteger[layout.Nfq];
        var deltaNfw = finalInteger[layout.Nfw] - startInteger[layout.Nfw];

        output.WriteLine(
            $"{label}: deltaNfx={deltaNfx} deltaNfy={deltaNfy} deltaNfz={deltaNfz} deltaNfq={deltaNfq} deltaNfw={deltaNfw} " +
            $"(S6 total = deltaNfq + deltaNfw = {deltaNfq + deltaNfw})");

        // Every counted delta must be strictly positive, or this run never exercised the draw at all and the
        // identity below would hold vacuously (Advance by 0 steps is a no-op) — the same non-degeneracy
        // concern this node's own Qks1RefreshUnderCycle1Tests names for its own refresh-count identity.
        Assert.True(deltaNfx > 0, $"{label}: Nfx never advanced; this run drew no base particles at all.");
        Assert.True(deltaNfy > 0, $"{label}: Nfy never advanced; this run never reached a distance draw.");
        Assert.True(deltaNfz > 0, $"{label}: Nfz never advanced; this run never reached a neighbour draw.");
        Assert.True(deltaNfq > 0, $"{label}: Nfq never advanced; this run never drew X3 (a bridge).");
        Assert.True(deltaNfw > 0, $"{label}: Nfw never advanced; this run never drew X4 (a bridge's Var#2 branch).");

        var expectedS1 = Mcg128.Advance(initial.S1, (ulong)deltaNfx, 0);
        var expectedS2 = Mcg128.Advance(initial.S2, (ulong)deltaNfx, 0);
        var expectedS3 = Mcg128.Advance(initial.S3, (ulong)deltaNfy, 0);
        var expectedS4 = Mcg128.Advance(initial.S4, (ulong)deltaNfz, 0);
        var expectedS5 = Mcg128.Advance(initial.S5, (ulong)deltaNfz, 0);
        var expectedS6 = Mcg128.Advance(initial.S6, (ulong)(deltaNfq + deltaNfw), 0);

        AssertState(label, "S1 (X)", expectedS1, actual.S1);
        AssertState(label, "S2 (X0)", expectedS2, actual.S2);
        AssertState(label, "S3 (X1)", expectedS3, actual.S3);
        AssertState(label, "S4 (X2)", expectedS4, actual.S4);
        AssertState(label, "S5 (X21)", expectedS5, actual.S5);
        AssertState(label, "S6 (X3+X4)", expectedS6, actual.S6);
    }

    private static void AssertState(string label, string streamName, Mcg128State expected, Mcg128State actual)
    {
        Assert.True(
            expected.Low == actual.Low && expected.High == actual.High,
            $"{label}, {streamName}: expected (Low={expected.Low}, High={expected.High}) from jumping the " +
            $"initial state by the counted draws, got (Low={actual.Low}, High={actual.High}) from the actual " +
            "run — the stream-to-counter mapping this class documents does not hold for this stream (this is " +
            "the finding to report, not a mapping to adjust until it passes).");
    }
}
