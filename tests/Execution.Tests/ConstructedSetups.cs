using PropStruct.Particle;

namespace PropStruct.Execution.Tests;

/// <summary>
/// Tiny, hand-built <see cref="ModelSetup"/>s and fraction tables that drive
/// <see cref="Attempt.Run"/> down a specific branch deterministically, for the L1 rows of
/// <c>BOOT.md</c> that need a constructed setup rather than a reference formulation: one
/// oxidizer fraction spanning the whole size range, so the base and neighbour draws always
/// land inside (<c>Dmin</c>, <c>Dmax</c>) whatever the generator draws.
/// </summary>
internal static class ConstructedSetups
{
    /// <summary>
    /// A setup where every attempt reaches the neighbour loop (label 501) at least once: one
    /// pocket-forming fraction, <c>Dmin</c>/<c>Dmax</c> wide enough that the uniform-in-D draw
    /// can never fall outside them.
    /// </summary>
    public static (ModelSetup Setup, double[] Bounds, double[] Cumulative, byte[] PocketForming) ReachesNeighbourLoop(
        int neighbourBudget = 1_000_000, int pocketRedrawBudget = 1_000_000, int nc = 10)
    {
        const int fractionCount = 1;
        const int ndok = 50;
        const int nkarm = 50;
        const int ncat = 50;

        var layout = AccumulatorLayout.Create(fractionCount, ndok, nkarm, ncat, nc);
        var setup = new ModelSetup
        {
            FractionCount = fractionCount,
            SizeLaw = 2, // uniform in D
            CellSize = 1.0,
            CategoryStep = 1.0,
            Dmin = 1e-3,
            Dmax = 100.0,
            Lambda = 1.0,
            Ak1 = 0.5,
            Ak2 = 2.0,
            Ak3 = 0.27,
            Ak4 = 4.7,
            Variant = 0,
            Alpha = 1.0,
            PocketCoefficient = 1.0,
            BridgeCoefficient = 1.0,
            NnMin = 0.0,
            NnMax = 1e9,
            Ndok = ndok,
            Nkarm = nkarm,
            Ncat = ncat,
            Nc = nc,
            NeighbourBudget = neighbourBudget,
            PocketRedrawBudget = pocketRedrawBudget,
            Layout = layout,
        };

        // One fraction, bounds strictly inside (Dmin, Dmax): DOK(1), DOK(2).
        var bounds = new[] { 10.0, 50.0 };
        var cumulative = new[] { 0.0, 1.0 };
        var pocketForming = new byte[] { 1 };

        return (setup, bounds, cumulative, pocketForming);
    }

    /// <summary>
    /// A setup where the base draw's fraction is never pocket-forming (<c>SFR = 0</c>), so every
    /// attempt returns <see cref="AttemptOutcome.RestartedInsideLoop"/> before it ever reaches the
    /// neighbour loop: <c>LoopCompletions</c> never changes, whatever the generator draws.
    /// </summary>
    public static (ModelSetup Setup, double[] Bounds, double[] Cumulative, byte[] PocketForming) NeverCompletesTheLoop()
    {
        var (setup, bounds, cumulative, _) = ReachesNeighbourLoop();
        return (setup, bounds, cumulative, new byte[] { 0 });
    }

    /// <summary>A setup whose neighbour-draw budget is exhausted on the very first pass of label 501.</summary>
    public static (ModelSetup Setup, double[] Bounds, double[] Cumulative, byte[] PocketForming) NeighbourBudgetExceeded()
    {
        var (setup, bounds, cumulative, pocketForming) = ReachesNeighbourLoop(neighbourBudget: 0);
        return (setup, bounds, cumulative, pocketForming);
    }
}
