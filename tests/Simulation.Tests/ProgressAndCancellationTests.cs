using PropStruct.Execution;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L1 of the BOOT.md table: "progress and cancellation change no bit of a completed run" (BOOT.md, "Design
/// decisions (2026-09-18)", "Progress and cancellation").
/// </summary>
public class ProgressAndCancellationTests
{
    private static SimulationOptions BatchedOptions() => new()
    {
        Mode = ExecutionMode.Batched,
        Accelerator = AcceleratorKind.Cpu,
        Streams = StreamLayout.Independent, // Original refuses batched mode (BOOT.md, "## Invariants")
        Seed = 0UL,
    };

    [Fact]
    public void RunWithProgressAndCancellationTokenGivesTheSameResultAsWithNeither()
    {
        var formulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 2);

        using var plain = Simulator.Create(BatchedOptions());
        var plainResult = plain.Run(formulation);

        using var withExtras = Simulator.Create(BatchedOptions());
        var reports = new List<CycleProgress>();
        var progress = new SynchronousProgress<CycleProgress>(reports.Add);
        using var cancellationSource = new CancellationTokenSource();
        var withExtrasResult = withExtras.Run(formulation, progress, cancellationSource.Token);

        Assert.NotEmpty(reports);

        ResultReflection.AssertBitIdentical(plainResult.Header, withExtrasResult.Header);
        ResultReflection.AssertBitIdentical(plainResult.Counters, withExtrasResult.Counters);
        ResultReflection.AssertBitIdentical(plainResult.GeneratorAccuracy, withExtrasResult.GeneratorAccuracy);
        ResultReflection.AssertBitIdentical(plainResult.ParticleSizes, withExtrasResult.ParticleSizes);
        ResultReflection.AssertBitIdentical(plainResult.LocalStructure, withExtrasResult.LocalStructure);
        ResultReflection.AssertBitIdentical(plainResult.Pockets, withExtrasResult.Pockets);
        ResultReflection.AssertBitIdentical(plainResult.MassFractions, withExtrasResult.MassFractions);
        ResultReflection.AssertBitIdentical(plainResult.Agglomerates, withExtrasResult.Agglomerates);
        ResultReflection.AssertBitIdentical(plainResult.Histograms, withExtrasResult.Histograms);
        ResultReflection.AssertBitIdentical(plainResult.Convergence, withExtrasResult.Convergence);
    }

    [Fact]
    public void RunReferenceModeReportsProgressAfterEvery1000ParticlesAndAtTheEnd()
    {
        // 2500 particles per cycle: two 1000-particle marks plus the end-of-cycle mark, once per cycle.
        var formulation = SmallFormulation.Build(particlesPerCycle: 2500, cycles: 1);

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Seed = 0UL,
        });

        var reports = new List<CycleProgress>();
        _ = simulator.Run(formulation, new SynchronousProgress<CycleProgress>(reports.Add));

        // Per cycle: two 1000-particle marks (1000, 2000) and one end-of-cycle mark (2500) = 3; two cycles
        // (0 and 1, since Cycles = 1 normalizes to one post-warm-up cycle) = 6.
        Assert.Equal(6, reports.Count);
    }

    // Measured 2026-09-18: ~0.5 s (cancellation fires after the first 8-particle batch), well under BOOT.md's
    // "Category=Long" threshold of "more than a few seconds" (BOOT.md, "Invariants").
    [Fact]
    public void RunCancelledMidRunThrowsOperationCanceledException()
    {
        var formulation = SmallFormulation.Build(particlesPerCycle: 5000, cycles: 4);

        using var simulator = Simulator.Create(BatchedOptions() with { BatchSize = 8 });
        using var cancellationSource = new CancellationTokenSource();
        var progress = new SynchronousProgress<CycleProgress>(_ => cancellationSource.Cancel());

        _ = Assert.Throws<OperationCanceledException>(() => simulator.Run(formulation, progress, cancellationSource.Token));
    }
}
