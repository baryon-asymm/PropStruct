using PropStruct.Random;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L2 (BOOT.md, blocker found by the Opus audit, 2026-09-18): every existing QKS1 test and
/// <see cref="ReferenceModeEqualsContinuedBatchTests"/> ran at <c>cycleFlag = 0</c>, where QKS1 is never
/// read by <c>Attempt.Run</c> and (on HPEPA3, at least) almost every completed neighbour loop is
/// <c>Accepted</c> — so a refresh misplaced relative to the attempt loop (e.g. after instead of before, or
/// outside the relaunch loop entirely) left every one of those tests green. This test runs both reference
/// mode and a continued batch at <c>cycleFlag = 1</c>, over enough of HPEPA3's real cycle-1 particles that
/// <c>RestartedAfterLoop</c> genuinely fires more than once (the audit's own guard against a vacuous pass:
/// if every particle finished on its first completed loop, the refresh-count identity below would hold for
/// several wrong placements too, not only the right one).
/// </summary>
/// <remarks>
/// A constructed setup (<see cref="ConstructedSetups"/>) was tried first, for speed, and does not work here:
/// <see cref="ConstructedSetups.ReachesNeighbourLoop"/> throws <see cref="IndexOutOfRangeException"/> inside
/// <c>Attempt.Run</c> as soon as <c>CycleFlag</c> is set to 1 (confirmed both through <c>RunReferenceParticle</c>
/// and through <c>RunBatch</c>, so it is not specific to either path), regardless of <c>Dmaxxx</c>,
/// <c>Pdoksmall</c>, <c>NnMin</c>, or of raising <c>Ndok</c>/<c>Nkarm</c>/<c>Ncat</c>/<c>Nc</c> well past what
/// <c>Dmax</c>/<c>CellSize</c> would need (every combination tried in this session's diagnostics, not
/// committed). None of this node's own inputs to <c>Attempt.Run</c> — <c>ModelSetup</c>, <c>FractionTable</c>,
/// <c>CycleInputs</c> — are documented in enough detail in <c>Particle/API.md</c> to say which field a
/// hand-built <c>CycleFlag = 1</c> setup is missing; real formulations reach <c>CycleFlag = 1</c> only through
/// <c>Statistics.Setup.Prepare</c>/<c>CompleteEchoes</c>, which compute more of <c>ModelSetup</c> than this
/// node's constructed setups do. This is a proposal for whoever owns <c>Particle</c>/<c>Statistics</c>
/// (AGENTS.md §11): a fast constructed-setup row needs that precondition named, and this test node cannot
/// find it by reading <c>Particle</c>'s own code (AGENTS.md §3). Using HPEPA3 is slower
/// (<c>Category = Long</c>) but is not blocked by this gap.
/// </remarks>
[Collection(LongClassesSerialTests.Name)]
public sealed class Qks1RefreshUnderCycle1Tests(ITestOutputHelper output)
{
    private const int ParticleCount = 2000;
    private const long MaxAttemptsPerParticle = 10_000_000;

    [Fact]
    [Trait("Category", "Long")]
    public void ReferenceEqualsContinuedAndRefreshCountOverHpepa3sCycle1()
    {
        using var warmup = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, "HPEPA3");
        var setup = ready.Setup;
        var layout = setup.Layout;

        using var reference = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        reference.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        reference.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        reference.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(reference.DebugReadQks1(), v => v != 0.0);

        using var continued = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        continued.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        continued.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        continued.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(continued.DebugReadQks1(), v => v != 0.0);

        var initialLoopCompletions = new long[layout.IntegerLength];
        var initialReal = new double[layout.RecordLength];
        reference.ReadTotals(initialLoopCompletions, initialReal);
        var startLoopCompletions = initialLoopCompletions[layout.LoopCompletions];

        var referenceStreams = StreamSeeds.ForParticle(StreamLayout.Independent, 0UL, ready.NextOrdinal);
        var continuedStreams = referenceStreams;
        var refreshSum = 0;

        for (var i = 0; i < ParticleCount; i++)
        {
            var referenceStatus = reference.RunReferenceParticle(ref referenceStreams, MaxAttemptsPerParticle, out var referenceCounters);
            Assert.Equal(BatchStatus.Ok, referenceStatus);
            refreshSum += referenceCounters.Qks1Refreshes;

            var continuedStatus = continued.RunContinuedBatch(ref continuedStreams, attemptsPerLaunch: 1, MaxAttemptsPerParticle, out var continuedCounters);
            Assert.Equal(BatchStatus.Ok, continuedStatus);

            // Reference mode and a continued batch with attemptsPerLaunch = 1 are bit-identical at every
            // step (root BOOT.md, "Reference mode is batched mode degenerated"), particle by particle, not
            // only in the aggregate at the end.
            Assert.Equal(referenceCounters, continuedCounters);
            AssertStreamsEqual(referenceStreams, continuedStreams);
        }

        var finalInteger = new long[layout.IntegerLength];
        var finalReal = new double[layout.RecordLength];
        reference.ReadTotals(finalInteger, finalReal);
        var continuedInteger = new long[layout.IntegerLength];
        var continuedReal = new double[layout.RecordLength];
        continued.ReadTotals(continuedInteger, continuedReal);

        Assert.Equal(finalInteger, continuedInteger);
        Assert.Equal(finalReal, continuedReal);

        var deltaLoopCompletions = finalInteger[layout.LoopCompletions] - startLoopCompletions;
        output.WriteLine($"particles={ParticleCount}, deltaLoopCompletions={deltaLoopCompletions}, refreshSum={refreshSum}");

        // Guards against a vacuous pass (the audit's own point): if every particle finished on its first
        // completed loop, deltaLoopCompletions would equal ParticleCount exactly, and the identity below
        // would hold under several wrong refresh placements too, not only the correct "before every attempt"
        // one — so this run is only informative if RestartedAfterLoop genuinely fired at least once.
        Assert.True(deltaLoopCompletions > ParticleCount,
            $"deltaLoopCompletions ({deltaLoopCompletions}) must exceed particleCount ({ParticleCount}) or this run never exercised a RestartedAfterLoop, and proves nothing about refresh timing.");

        // The refresh check runs once before every attempt (root BOOT.md, "Reference mode is the original's
        // sequence"); the very first attempt of this run sees no prior change (checked against whatever
        // LoopCompletions was at the last refresh before this test started), so it is never counted. Every
        // one of the deltaLoopCompletions - 1 attempts *after* the first sees the previous attempt's own
        // completion and refreshes. A refresh moved outside this loop (Engine.cs, the audit's suggested
        // mutation) either never fires (0) or fires once per launch/particle rather than once per completed
        // loop, so it cannot reproduce this exact count.
        Assert.Equal(deltaLoopCompletions - 1, refreshSum);
    }

    private static void AssertStreamsEqual(StreamSet a, StreamSet b)
    {
        Assert.Equal(a.S1.Low, b.S1.Low); Assert.Equal(a.S1.High, b.S1.High);
        Assert.Equal(a.S2.Low, b.S2.Low); Assert.Equal(a.S2.High, b.S2.High);
        Assert.Equal(a.S3.Low, b.S3.Low); Assert.Equal(a.S3.High, b.S3.High);
        Assert.Equal(a.S4.Low, b.S4.Low); Assert.Equal(a.S4.High, b.S4.High);
        Assert.Equal(a.S5.Low, b.S5.Low); Assert.Equal(a.S5.High, b.S5.High);
        Assert.Equal(a.S6.Low, b.S6.Low); Assert.Equal(a.S6.High, b.S6.High);
    }
}
