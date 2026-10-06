using PropStruct.Execution;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The refusal branch of the CUDA row (tests/Simulation.Tests/BOOT.md, "## Constraints"), forced with the kill
/// switch so it is exercised on a machine that has CUDA as well. Sets the process-wide kill switch
/// <c>PROPSTRUCT_NO_CUDA</c> (<c>Execution/API.md</c>, "## Side effects"), which any engine created
/// concurrently would also read, so this class must run after every other class of this assembly and alone
/// (<see cref="Simulator"/> has no environment seam of its own).
///
/// CA1515, found 2026-09-24 under AnalysisMode=All, previously a genuine conflict with xUnit1027 (a
/// separate, empty <c>[CollectionDefinition]</c> marker class is never referenced outside its assembly, so
/// CA1515 wants it internal, while xUnit1027 requires a collection-definition class to be public). Resolved
/// by attaching <c>[CollectionDefinition]</c> to this class itself rather than to a dedicated marker: this
/// class already has to be public for xUnit to discover its own <c>[Fact]</c> (the same reason CA1515 does
/// not flag any other test class in this tree), so it costs nothing to also be the one class of its own
/// singleton collection. Behaviourally identical to the separate-marker-class form: the same attribute, the
/// same <see cref="CollectionDefinitionAttribute.DisableParallelization"/>, the same collection name, and no
/// other class refers to it.
/// </summary>
[CollectionDefinition(nameof(CudaRefusalTests), DisableParallelization = true)]
[Collection(nameof(CudaRefusalTests))]
public class CudaRefusalTests
{
    private const string KillSwitch = "PROPSTRUCT_NO_CUDA";

    [Fact]
    public void RunBatchedOnCudaWithTheKillSwitchFailsWithAcceleratorUnavailable()
    {
        var previous = Environment.GetEnvironmentVariable(KillSwitch);
        Environment.SetEnvironmentVariable(KillSwitch, "1");
        try
        {
            using var simulator = Simulator.Create(new SimulationOptions { Mode = ExecutionMode.Batched, Accelerator = AcceleratorKind.Cuda });
            Assert.NotNull(simulator.Accelerator.CudaSkippedBecause);

            var exception = Assert.Throws<SimulationFailedException>(
                () => simulator.Run(SmallFormulation.Build(particlesPerCycle: 24, cycles: 1)));
            Assert.Equal(RunStatus.AcceleratorUnavailable, exception.Status);
        }
        finally
        {
            Environment.SetEnvironmentVariable(KillSwitch, previous);
        }
    }
}
