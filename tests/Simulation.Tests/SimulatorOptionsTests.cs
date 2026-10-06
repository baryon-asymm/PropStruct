using PropStruct.Execution;
using PropStruct.Input;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L0 of the BOOT.md table: option validation, accelerator resolution by mode, and every failure status
/// turned into <see cref="SimulationFailedException"/> (API.md, "## Errors").
/// </summary>
public class SimulatorOptionsTests
{
    [Fact]
    public void CreateRejectsNonPositiveBatchSize()
    {
        var options = new SimulationOptions { BatchSize = 0 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsNonPositiveRecordBudgetBytes()
    {
        var options = new SimulationOptions { RecordBudgetBytes = 0 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsNonPositiveAttemptsPerLaunch()
    {
        var options = new SimulationOptions { AttemptsPerLaunch = 0 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsNonPositiveMaxAttemptsPerParticle()
    {
        var options = new SimulationOptions { MaxAttemptsPerParticle = 0 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsNonPositiveNeighbourBudget()
    {
        var options = new SimulationOptions { NeighbourBudget = 0 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsNonPositivePocketRedrawBudget()
    {
        var options = new SimulationOptions { PocketRedrawBudget = 0 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsCudaWithReferenceModeBeforeAnyWork()
    {
        var options = new SimulationOptions { Mode = ExecutionMode.Reference, Accelerator = AcceleratorKind.Cuda };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsContinuedStreamsWithoutBatchSizeOne()
    {
        var options = new SimulationOptions { Mode = ExecutionMode.Batched, ContinuedStreams = true, BatchSize = 4 };
        AssertRejectedByValidation(options);
    }

    [Fact]
    public void CreateRejectsContinuedStreamsWithReferenceMode()
    {
        var options = new SimulationOptions { Mode = ExecutionMode.Reference, ContinuedStreams = true, BatchSize = 1 };
        AssertRejectedByValidation(options);
    }

    /// <summary>
    /// The rejection must come from <see cref="Simulator"/>'s own validation, before any engine exists (API.md,
    /// "## Errors"), not from a neighbour further in: <c>Execution.Engine.Create</c> rejects a zero record budget
    /// by itself, and a check of the exception type alone stayed green with this node's guard removed.
    /// </summary>
    private static void AssertRejectedByValidation(SimulationOptions options)
    {
        var exception = Assert.Throws<ArgumentException>(() => Simulator.Create(options));
        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>BOOT.md, "Accelerator by mode": reference mode needs a CPU engine, so <c>Auto</c> resolves to <c>Cpu</c> there.</summary>
    [Fact]
    public void CreateResolvesAutoToCpuInReferenceMode()
    {
        var options = new SimulationOptions { Mode = ExecutionMode.Reference, Accelerator = AcceleratorKind.Auto };
        using var simulator = Simulator.Create(options);
        Assert.Equal(AcceleratorKind.Cpu, simulator.Accelerator.Kind);
    }

    /// <summary>Root BOOT.md acceptance criterion: "A run whose particle passes the attempt cap throws <see cref="SimulationFailedException"/> with the status."</summary>
    [Fact]
    public void CreateAttemptCapExceededThrowsSimulationFailedExceptionWithStatus()
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            MaxAttemptsPerParticle = 1, // HPEPA3's own particles need several attempts (BOOT.md, "## Budget measurement"): one is never enough.
        };
        using var simulator = Simulator.Create(options);
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var exception = Assert.Throws<SimulationFailedException>(() => simulator.Run(formulation));
        Assert.Equal(RunStatus.AttemptCapExceeded, exception.Status);
    }

    /// <summary>Statistics/BOOT.md's own precondition "0 ≤ Dmin &lt; Ddokmax": a <c>Dmin</c> far past every fraction's own upper bound rejects the setup before any particle runs.</summary>
    [Fact]
    public void RunInvalidSetupThrowsSimulationFailedExceptionWithStatus()
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Parameters = ModelParameters.Default with { Dmin = Length.FromMicrometres(1e7) },
        };
        using var simulator = Simulator.Create(options);
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var exception = Assert.Throws<SimulationFailedException>(() => simulator.Run(formulation));
        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
    }

    /// <summary>
    /// The reported defect (`tests/Cli.Tests`, wave 7): <c>NnMax = 0.0</c> made <c>nn &gt;=
    /// setup.NnMax</c> (`Particle/Attempt.cs`, line 735) true for every attempt of cycle
    /// &gt;= 1, so the run only ever stopped once every particle exhausted
    /// <see cref="SimulationOptions.MaxAttemptsPerParticle"/>'s default of 500,000 — about
    /// four minutes for a single particle, read as "the process was killed" by whatever
    /// external timeout was watching it, even though the run itself never left a value
    /// unstated (`src/Statistics/BOOT.md`, "## Constraints", the 2026-09-20 note). The fix
    /// is a `Statistics.Setup.Prepare` precondition, so this run must now fail before any
    /// particle work, immediately rather than after the attempt cap.
    /// </summary>
    [Fact]
    public void RunDegenerateNnMaxThrowsSimulationFailedExceptionWithInvalidSetupImmediately()
    {
        var options = new SimulationOptions
        {
            Parameters = ModelParameters.Default with { NnMax = 0.0 },
        };
        using var simulator = Simulator.Create(options);
        var formulation = SmallFormulation.Build(particlesPerCycle: 1, cycles: 1);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var exception = Assert.Throws<SimulationFailedException>(() => simulator.Run(formulation));
        stopwatch.Stop();

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        // The point of the fix: this used to take 4 minutes 2 seconds (the BOOT.md note),
        // burning the whole attempt cap one particle at a time. A generous bound (still two
        // orders of magnitude under the old figure, to stay reliable on a slower machine)
        // proves the rejection now happens before any particle work, not merely that some
        // exception eventually arrives.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Took {stopwatch.Elapsed}; the precondition should reject before any particle runs.");
    }
}
