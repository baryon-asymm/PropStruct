using PropStruct.Execution;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L0 of the BOOT.md table, "each failure status turned into <see cref="SimulationFailedException"/>": the
/// mapping over the machine-generated list of <c>BatchStatus</c> values, and a real run for every status a
/// constructed configuration can reach.
/// </summary>
public class FailureStatusTests(ITestOutputHelper output)
{
    /// <summary>
    /// Every failure of <c>Execution.BatchStatus</c> becomes the <see cref="RunStatus"/> of the same name,
    /// except <see cref="BatchStatus.OriginalPrecisionRequiresReferenceMode"/>: it deliberately reuses
    /// <see cref="RunStatus.InvalidSetup"/> instead of a new status value (decided with the root,
    /// 2026-09-21, so a consumer already reporting <c>InvalidSetup</c> by echoing the message needs no new
    /// case), checked separately below. Its former twin, <c>OriginalLayoutRequiresReferenceMode</c>, is
    /// withdrawn (architecture audit finding R1, 2026-09-26): <c>Execution</c> no longer refuses a stream
    /// layout at all, so <c>BatchStatus</c> has no such member left to map (`src/Execution/BOOT.md`).
    /// <see cref="BatchStatus.ParticleNotRun"/> is excluded too: it is no run status but an engine defect,
    /// so its mapping throws (<see cref="ParticleNotRunIsAnEngineDefectNotARunStatus"/>, decided 2026-10-02).
    /// </summary>
    [Fact]
    public void EveryBatchFailureStatusMapsToTheRunStatusOfTheSameName()
    {
        foreach (var status in Enum.GetValues<BatchStatus>()
            .Where(s => s is not BatchStatus.Ok
                and not BatchStatus.OriginalPrecisionRequiresReferenceMode
                and not BatchStatus.ParticleNotRun))
        {
            Assert.Equal(status.ToString(), Simulator.ToRunStatus(status).ToString());
        }
    }

    /// <summary>The one deliberate name mismatch <see cref="EveryBatchFailureStatusMapsToTheRunStatusOfTheSameName"/> excludes.</summary>
    [Fact]
    public void OriginalPrecisionRequiresReferenceModeMapsToInvalidSetup() => Assert.Equal(RunStatus.InvalidSetup, Simulator.ToRunStatus(BatchStatus.OriginalPrecisionRequiresReferenceMode));

    /// <summary>
    /// <see cref="BatchStatus.ParticleNotRun"/> means the engine's dispatch skipped a particle: a defect of
    /// the tool, which the tree reports as an exception and not as a <see cref="RunStatus"/>. The exact
    /// type matters, since an <see cref="ArgumentException"/> would reach <c>Cli</c>'s exit 2, "invalid
    /// arguments", for a defect that no argument causes.
    /// </summary>
    [Fact]
    public void ParticleNotRunIsAnEngineDefectNotARunStatus()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Simulator.ToRunStatus(BatchStatus.ParticleNotRun));
        Assert.Contains(nameof(BatchStatus.ParticleNotRun), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RunNeighbourBudgetExceededThrowsWithTheStatus()
    {
        var options = new SimulationOptions { Mode = ExecutionMode.Reference, NeighbourBudget = 1 };
        AssertRunFails(options, RunStatus.NeighbourBudgetExceeded, particlesPerCycle: 200);
    }

    [Fact]
    public void RunBridgeDrawBudgetExceededThrowsWithTheStatus()
    {
        // A bridge needing a second pocket draw is rarer than a neighbour loop needing a second draw, so this
        // run is larger and batched.
        var options = new SimulationOptions { Mode = ExecutionMode.Batched, Accelerator = AcceleratorKind.Cpu, PocketRedrawBudget = 1 };
        AssertRunFails(options, RunStatus.BridgeDrawBudgetExceeded, particlesPerCycle: 5000);
    }

    /// <summary>
    /// The CUDA row: where the engine was refused CUDA, the run fails with <see cref="RunStatus.AcceleratorUnavailable"/>;
    /// where CUDA is bound, the same small run completes on it. Never skipped (tests/Simulation.Tests/BOOT.md, "## Constraints").
    /// </summary>
    [Fact]
    public void RunBatchedOnCudaRunsOrFailsWithAcceleratorUnavailable()
    {
        using var simulator = Simulator.Create(new SimulationOptions { Mode = ExecutionMode.Batched, Accelerator = AcceleratorKind.Cuda });
        var formulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 1);
        output.WriteLine($"Accelerator: {simulator.Accelerator.Kind} {simulator.Accelerator.Name}; CUDA skipped because: {simulator.Accelerator.CudaSkippedBecause ?? "-"}");

        if (simulator.Accelerator.CudaSkippedBecause is not null)
        {
            CudaRequirement.FailIfRequired(simulator.Accelerator.CudaSkippedBecause);
            var exception = Assert.Throws<SimulationFailedException>(() => simulator.Run(formulation));
            Assert.Equal(RunStatus.AcceleratorUnavailable, exception.Status);
        }
        else
        {
            Assert.Equal(AcceleratorKind.Cuda, simulator.Accelerator.Kind);
            var result = simulator.Run(formulation);
            Assert.Equal(AcceleratorKind.Cuda, result.Diagnostics.Accelerator.Kind);
            Assert.True(result.Counters.Nfx >= 2L * formulation.ParticlesPerCycle);
        }
    }

    private static void AssertRunFails(SimulationOptions options, RunStatus expected, int particlesPerCycle)
    {
        using var simulator = Simulator.Create(options);
        var formulation = SmallFormulation.Build(particlesPerCycle, cycles: 1);
        var exception = Assert.Throws<SimulationFailedException>(() => simulator.Run(formulation));
        Assert.Equal(expected, exception.Status);
    }
}
