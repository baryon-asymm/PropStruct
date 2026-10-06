using ILGPU.Runtime;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L1 (BOOT.md): <see cref="Engine.WriteTotals"/> must leave QKS1 consistent with the totals it just wrote,
/// not with whatever QKS1 held before the call (BOOT.md, "QKS1 is the engine's": QKS1 starts as
/// <c>Normalize</c> of the loaded totals and is refreshed whenever the totals change in a way this node
/// cannot assume it already tracked). <c>WriteTotals</c> replaces the totals wholesale — <c>Simulation</c>'s
/// own in-place rewrite between cycles, or a caller forking one engine's state onto another
/// (<c>DeterminismTests</c>, <c>TierTableTests</c>) — so unlike the incremental per-launch refresh, there is
/// no history to compare against: QKS1 must be recomputed unconditionally, every time.
/// </summary>
/// <remarks>
/// Found by the coordinator (2026-09-18): a batch that follows <c>WriteTotals</c> with nonzero
/// <c>LoopCompletions</c> already in the uploaded totals burned an entire launch's attempt budget with no
/// particle accepted (HPEPA3 cycle 1: launch 1 hit exactly <c>attemptsPerLaunch</c> attempts on every one of
/// its 100,000 particles, `AttemptCapExceeded`, before launch 2 — now seeing a correctly refreshed QKS1 —
/// behaved like the reference, ≈6.75 attempts/particle). The old <c>WriteTotals</c> only resynced the
/// <c>_lastRefreshLoopCompletions</c> bookkeeping to the just-written value, without ever calling
/// <c>RefreshQks1Now</c>; the next launch's own refresh check then saw "no change since last refresh" and
/// left QKS1 exactly as it was before the write — <c>Normalize</c> of whatever (often all-zero) totals the
/// engine held beforehand. Fixed by having <c>WriteTotals</c> call <c>RefreshQks1Now</c> before resyncing
/// the bookkeeping, exactly as <c>Load</c> already does for a freshly zeroed run.
/// </remarks>
public sealed class WriteTotalsRefreshesQks1Tests
{
    [Fact]
    public void WriteTotalsLeavesQks1EqualToNormalizeOfTheWrittenTotals()
    {
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        var layout = setup.Layout;

        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        engine.Load(in setup, bounds, cumulative, pocketForming);

        // Craft totals with a populated Qks histogram and a nonzero LoopCompletions, as Statistics' own
        // in-place rewrite between cycles would hand back — never obtained by actually running particles,
        // so this test stays fast and exercises WriteTotals in isolation.
        var integerTotals = new long[layout.IntegerLength];
        integerTotals[layout.LoopCompletions] = 5;
        integerTotals[layout.Qks + 0] = 3;
        integerTotals[layout.Qks + 7] = 11;
        integerTotals[layout.Qks + 20] = 4;
        var realTotals = new double[layout.RecordLength];

        engine.WriteTotals(integerTotals, realTotals);

        var actualQks1 = engine.DebugReadQks1();

        using var host = new CpuHost();
        using var totalsBuffer = host.Accelerator.Allocate1D(integerTotals);
        using var expectedBuffer = host.Accelerator.Allocate1D<double>(setup.Nkarm);
        host.Accelerator.Synchronize();
        PocketHistogram.Normalize(in layout, totalsBuffer.View, expectedBuffer.View);
        var expectedQks1 = expectedBuffer.GetAsArray1D();

        Assert.Equal(expectedQks1, actualQks1);
        Assert.Contains(actualQks1, v => v != 0.0); // guards against a vacuously equal all-zero pass
    }
}
