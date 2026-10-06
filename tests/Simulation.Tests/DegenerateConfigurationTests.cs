using PropStruct.Execution;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L1 of the BOOT.md table: "the degenerate batched configuration equals reference mode bit for bit", on a
/// constructed formulation with a small <c>N</c> and <c>KXX = 2</c> (BOOT.md's own wording for this row).
/// Both runs use the CPU accelerator: reference mode has no other engine to run on (<c>Execution/API.md</c>,
/// "reference mode on a non-CPU engine" refuses), so a fair comparison holds the accelerator fixed too.
///
/// <see cref="StreamLayout.Independent"/>, not <see cref="StreamLayout.Original"/> (BOOT.md, "## Invariants",
/// the 2026-09-21 narrowing): the degenerate configuration below is still <see cref="ExecutionMode.Batched"/>,
/// and `Original` now refuses every route to batched mode (`StreamLayoutTests` proves the refusal itself,
/// including on this exact configuration shape); the claim this class proves — batched-degenerate equals
/// reference, bit for bit — is unchanged, only which layout it is proven under.
/// </summary>
public class DegenerateConfigurationTests
{
    [Fact]
    public void BatchedDegenerateConfigurationEqualsReferenceModeBitForBit()
    {
        var formulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 2);

        using var reference = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Independent,
            Seed = 0UL,
        });
        var referenceResult = reference.Run(formulation);

        using var degenerate = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Independent,
            Seed = 0UL,
            BatchSize = 1,
            AttemptsPerLaunch = 1,
            ContinuedStreams = true,
        });
        var degenerateResult = degenerate.Run(formulation);

        ResultReflection.AssertBitIdentical(referenceResult.Header, degenerateResult.Header);
        ResultReflection.AssertBitIdentical(referenceResult.Counters, degenerateResult.Counters);
        ResultReflection.AssertBitIdentical(referenceResult.GeneratorAccuracy, degenerateResult.GeneratorAccuracy);
        ResultReflection.AssertBitIdentical(referenceResult.ParticleSizes, degenerateResult.ParticleSizes);
        ResultReflection.AssertBitIdentical(referenceResult.LocalStructure, degenerateResult.LocalStructure);
        ResultReflection.AssertBitIdentical(referenceResult.Pockets, degenerateResult.Pockets);
        ResultReflection.AssertBitIdentical(referenceResult.MassFractions, degenerateResult.MassFractions);
        ResultReflection.AssertBitIdentical(referenceResult.Agglomerates, degenerateResult.Agglomerates);
        ResultReflection.AssertBitIdentical(referenceResult.Histograms, degenerateResult.Histograms);
        ResultReflection.AssertBitIdentical(referenceResult.Convergence, degenerateResult.Convergence);

        // Run bookkeeping differs by construction (Mode, ContinuedStreams, Elapsed); the attempt and launch
        // totals and the batch size of one must agree, since both paths take the same attempts in the same order.
        Assert.Equal(referenceResult.Diagnostics.TotalAttempts, degenerateResult.Diagnostics.TotalAttempts);
        Assert.Equal(referenceResult.Diagnostics.TotalLaunches, degenerateResult.Diagnostics.TotalLaunches);
        Assert.Equal(1, referenceResult.Diagnostics.BatchSize);
        Assert.Equal(1, degenerateResult.Diagnostics.BatchSize);
    }

    /// <summary>
    /// The mutation proof (AGENTS.md §13): a different seed must break bit-for-bit agreement, proving the
    /// comparison above is not vacuously true (e.g. from two all-zero or otherwise-equal-by-accident results).
    /// </summary>
    [Fact]
    public void BatchedDegenerateConfigurationWithADifferentSeedDoesNotAgreeBitForBit()
    {
        var formulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 2);

        using var reference = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Independent,
            Seed = 0UL,
        });
        var referenceResult = reference.Run(formulation);

        using var degenerate = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Independent,
            Seed = 1UL,
            BatchSize = 1,
            AttemptsPerLaunch = 1,
            ContinuedStreams = true,
        });
        var degenerateResult = degenerate.Run(formulation);

        Assert.NotEqual(referenceResult.Counters.Nfx, degenerateResult.Counters.Nfx);
    }
}
