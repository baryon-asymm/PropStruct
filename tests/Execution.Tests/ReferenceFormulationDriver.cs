using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Random;
using PropStruct.Statistics;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The minimal cycle-0-then-cycle-1 driver the L2 rows of <c>BOOT.md</c> need: reads a reference
/// formulation, completes the setup hand-off through <see cref="Engine.SampleSize"/>
/// (<c>Simulation/API.md</c>, "Setup hand-off"), runs cycle 0 to completion, calls
/// <see cref="CycleStatistics.Compute"/> exactly as <c>Simulation</c> will between cycles, and hands back
/// an engine positioned at the start of cycle 1 (<c>SetCycle</c> already called, <c>NextOrdinal</c> the
/// first free particle ordinal). Kept here, in the test node, and nowhere near <c>Simulation</c>'s own
/// design (this assignment's own instruction: "keep that driver in the test node and minimal").
/// </summary>
internal static class ReferenceFormulationDriver
{
    // Generous enough that no reference formulation's cycle 0 or cycle 1 spuriously hits either cap; the
    // original has neither cap and loops without bound (root BOOT.md, "Execution model").
    private const int NeighbourBudget = 10_000_000;
    private const int PocketRedrawBudget = 1_000_000;
    private const long MaxAttemptsPerParticle = 10_000_000;

    /// <summary>
    /// The state after cycle 0, ready for cycle 1: <paramref name="Engine"/> already has
    /// <c>SetCycle(1, Dmaxxx, Pdoksmall)</c> applied; <paramref name="IntegerTotals"/>/
    /// <paramref name="RealTotals"/> are the exact post-cycle-0 totals (after <c>Statistics</c>' in-place
    /// rewrite) a caller can <see cref="Engine.WriteTotals"/> onto a different engine to reproduce the
    /// identical starting point (<see cref="DeterminismTests"/>'s own need: comparing several engines from
    /// one shared cycle 0).
    /// </summary>
    internal sealed record Cycle1Ready(
        Engine Engine, ModelSetup Setup, SetupTables Tables, Formulation Formulation, ulong NextOrdinal,
        long[] IntegerTotals, double[] RealTotals, double Dmaxxx, double[] Pdoksmall);

    /// <summary>Reads <paramref name="formulationName"/>.dat, completes the setup hand-off, runs cycle 0 in full, and calls <see cref="CycleStatistics.Compute"/> to prepare cycle 1's inputs.</summary>
    public static Cycle1Ready PrepareThroughCycle0(
        Engine engine, StreamLayout layout, ulong seed, string formulationName, int attemptsPerLaunch = 64)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", formulationName + ".dat");
        var formulation = DatFile.Read(path);
        var parameters = ModelParameters.Default;

        var prepareStatus = Setup.Prepare(
            formulation, parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out var draw, out var pending, out var inputs);
        if (prepareStatus != SetupStatus.Ok)
        {
            throw new InvalidOperationException($"Setup.Prepare({formulationName}) failed: {prepareStatus}");
        }

        engine.SampleSize(setup.SizeLaw, setup.FractionCount, tables.Bounds, tables.Cumulative, draw.X, draw.X1, out var dmax, out _);
        var echoes = Setup.CompleteEchoes(ref setup, pending, dmax);

        engine.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);
        engine.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var ordinal = 0UL;
        var particlesPerCycle = formulation.ParticlesPerCycle;
        while (ordinal < (ulong)particlesPerCycle)
        {
            var remaining = Math.Min(engine.MaxBatchSize, particlesPerCycle - (int)ordinal);
            var status = engine.RunBatch(layout, seed, ordinal, remaining, attemptsPerLaunch, MaxAttemptsPerParticle, out _);
            if (status != BatchStatus.Ok)
            {
                throw new InvalidOperationException($"{formulationName} cycle 0 batch at ordinal {ordinal} failed: {status}");
            }

            ordinal += (ulong)remaining;
        }

        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        engine.ReadTotals(integerTotals, realTotals);

        var nextPdoksmall = new double[setup.Ndok];
        _ = CycleStatistics.Compute(
            in setup, tables, echoes, inputs, cycleIndex: 0,
            integerTotals, realTotals, nextPdoksmall, out var nextDmaxxx, out var categoriesStatus);
        if (categoriesStatus != CategoriesStatus.Ok)
        {
            throw new InvalidOperationException($"{formulationName} cycle 0 category merge failed: {categoriesStatus}");
        }

        engine.WriteTotals(integerTotals, realTotals);
        engine.SetCycle(cycleFlag: 1, dmaxxx: nextDmaxxx, pdoksmall: nextPdoksmall);

        // Guards the exact defect the coordinator found (2026-09-18): WriteTotals used to leave QKS1 at
        // whatever it held before the call (often Normalize of the zero totals from Load, since cycle 0
        // usually finishes in a single launch and never triggers an organic refresh), silently, because the
        // refresh bookkeeping was resynced without ever recomputing QKS1 itself. Every caller of this driver
        // builds its own cycle-1 fork from the engine returned here (or from its IntegerTotals/Dmaxxx/
        // Pdoksmall), so this one assertion, right after the same WriteTotals + SetCycle(1, ...) pair the
        // defect lived in, is the single place that would have caught it before it reached HPEPA3's own
        // 100,000-particle cycle 1.
        Assert.Contains(engine.DebugReadQks1(), v => v != 0.0);

        return new Cycle1Ready(engine, setup, tables, formulation, ordinal, integerTotals, realTotals, nextDmaxxx, nextPdoksmall);
    }
}
