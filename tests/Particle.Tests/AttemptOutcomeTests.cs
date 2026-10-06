using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L1 of the BOOT.md table: one attempt per <see cref="AttemptOutcome"/>, the two budgets
/// and <see cref="AttemptOutcome.IndexOutOfRange"/> (the last via
/// <see cref="BridgeWindowTests"/>, the guard's own unit), all on constructed setups and
/// cycle inputs (no stand-in for a real formulation, BOOT.md, "Order of coding").
/// </summary>
public class AttemptOutcomeTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public AttemptOutcomeTests(CpuHost host)
    {
        _host = host;
    }

    [Fact]
    public void AcceptedInCycleZeroOnceTheNeighbourLoopCompletes()
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 0, neighbourBudget: 200, pocketRedrawBudget: 200);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.Accepted, outcome);
    }

    [Fact]
    public void AcceptedInCycleOneWhenPocketsAndBridgesBothClearTheAcceptanceTests()
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, neighbourBudget: 200, pocketRedrawBudget: 200);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.Accepted, outcome);
        var ibridgeTotal = scenario.IntegerTotal(scenario.Setup.Layout.IbridgeTotal);
        Assert.True(ibridgeTotal >= 2, $"expected at least 2 committed bridges, got {ibridgeTotal}");
        Assert.True(scenario.Record(scenario.Setup.Layout.DpMax) > 0.0, "expected at least one pocket (DpMax > 0)");
    }

    [Fact]
    public void RestartedAfterLoopWhenTheLoopCompletesWithNoPocket()
    {
        // Same seed and shape as the accepted case, only the fraction size changed: every
        // neighbour then clears the AK3 gap into the bridge branch and never the pocket
        // branch, so ipocket_loc stays 0 (conditions(6), Fortran line 723-725).
        using var scenario = Scenario.Build(_host.Accelerator, size: 2.0, cellSize: 0.2, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, neighbourBudget: 200, pocketRedrawBudget: 200);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedAfterLoop, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Conditions + 5)); // conditions(6): ipocket_loc == 0
    }

    [Fact]
    public void RestartedInsideLoopWhenTheBaseFractionDoesNotFormPockets()
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, pocketForming: 0);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedInsideLoop, outcome);
        // No condition counter guards SFR(Nfract) == 0 (Fortran line 473): nothing else
        // ran past the base draw, so every integer total below NFX/AlldokFract/Alldok is 0.
        Assert.Equal(0L, scenario.IntegerTotal(scenario.Setup.Layout.Nfy));
    }

    [Fact]
    public void RestartedInsideLoopWhenTheBaseDrawIsAtOrAboveDmax()
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, dmax: 0.0 /* below any positive draw */);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedInsideLoop, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Conditions + 0)); // conditions(1)
    }

    [Fact]
    public void RestartedInsideLoopWhenTheBaseDrawIsAtOrBelowDmin()
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, dmin: 1e6 /* above any draw within the fraction */);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedInsideLoop, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Conditions + 1)); // conditions(2)
    }

    [Fact]
    public void RestartedInsideLoopWhenTheGapFailsAk1AndTheVariantRestartsTheWholeAttempt()
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, ak1: 1e6, variant: 0);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedInsideLoop, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Conditions + 2)); // conditions(3)
    }

    private static readonly double[] SingleFractionBounds = [1.0, 1.0];
    private static readonly double[] SingleFractionCumulative = [0.0, 1.0];

    [Fact]
    public void RestartedInsideLoopWhenTheGapExceedsAk4()
    {
        // Peek the seed's own first-neighbour geometry (BOOT.md, "Bridge window once per
        // attempt" companion: ConstructedModel.PeekFirstNeighbour calls the same Mcg128.Next
        // and SizeLaw.Sample Attempt.Run calls) and set AK4 just under the ratio it will
        // compare against, so the very first neighbour trips conditions(5) deterministically.
        var setup = ConstructedModel.Permissive(1.0, 0.1, 0.1, ak3: 0.01, ak4: 1.0);
        var (_, _, _, aa, maxDrDb) = ConstructedModel.PeekFirstNeighbour(
            _host.Accelerator, setup, SingleFractionBounds, SingleFractionCumulative, OriginalSeeds.Streams);
        Assert.True(aa > 0.0, "this seed's first neighbour must be positive (not jammed) for the AK4 reject to be the branch taken");
        var ak4 = aa / maxDrDb / 2.0;

        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 0.1,
            ak3: ak4 / 2.0, ak4: ak4, cycleFlag: 1);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedInsideLoop, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Conditions + 4)); // conditions(5)
    }

    [Fact]
    public void RestartedInsideLoopWhenThePocketHistogramWindowIsEmpty()
    {
        // Qks1 left at zero (Scenario.Build's caller can override it): AUS sums to 0 over the
        // whole AK3..AK4 window (Fortran line 590), the empty-window restart.
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, uniformQks1: false);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.RestartedInsideLoop, outcome);
    }

    [Fact]
    public void NeighbourBudgetExceededAfterExactlyOneDrawWhenTheBudgetIsOne()
    {
        // Budget 0 could not tell a ">" check from a ">=" one apart: neither lets a single
        // draw happen, so NFY would stay 0 under either bug. Budget 1 with the same
        // multi-neighbour scenario as AcceptedInCycleOneWhenPocketsAndBridgesBothClearTheAcceptanceTests
        // (>= 2 committed bridges there) forces a second label-501 pass, so the exceeded
        // outcome and NFY == 1 together prove the guard let exactly one neighbour draw
        // through before firing on the next one.
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, neighbourBudget: 1);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.NeighbourBudgetExceeded, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Nfy));
    }

    [Fact]
    public void BridgeDrawBudgetExceededAfterExactlyOneDrawWhenTheBudgetIsOne()
    {
        // Same reasoning as the neighbour budget above, for the bridge redraw of label 451:
        // budget 0 rejects before any draw (NFQ stays 0 under a ">" or a ">=" bug alike).
        // NFQ counts every pocket-size draw of the whole attempt, not just the failing
        // bridge's own (a jammed attempt can draw several bridges before the one that
        // needs a redraw), so the scenario is chosen empirically for the very first bridge
        // of the attempt to already need a second draw: with budget 1, its first pocket-size
        // draw is spent and, since it does not clear BridgeGeometry.Volume's jj test on this
        // seed, the redraw loop asks for a second one and exceeds the budget there — before
        // any other bridge of the attempt gets a chance to draw at all, leaving NFQ == 1.
        using var scenario = Scenario.Build(_host.Accelerator, size: 0.5, cellSize: 0.05, lambda: 0.5,
            ak3: 0.6, ak4: 0.7, cycleFlag: 1, pocketRedrawBudget: 1);

        var outcome = scenario.Run(OriginalSeeds.Streams);

        Assert.Equal(AttemptOutcome.BridgeDrawBudgetExceeded, outcome);
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Nfq));
    }
}
