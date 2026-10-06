using PropStruct.Execution;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L0/L1 of the BOOT.md table: <see cref="StreamLayout.Original"/> is refused with batched mode by any
/// route (root BOOT.md, "Two stream layouts": "The `Original` layout is sequential only"), from
/// <see cref="Simulator.Create"/> itself since architecture audit finding R1 (2026-09-24;
/// `SimulationOptionsValidator`) — <c>Execution.Engine</c> keeps no refusal of its own for this rule any
/// more (`src/Execution/BOOT.md`'s own 2026-09-26 note). The <see cref="StreamLayout.Independent"/> layout
/// is unaffected in both modes, and the default configuration (`Mode = Batched`, `Streams = Independent`) is
/// coherent — a bare run works. The mirror of <c>PrecisionKindTests</c> in this node, for the twin decision.
/// </summary>
public class StreamLayoutTests
{
    /// <summary>
    /// The obvious route: a plain batch (no <see cref="SimulationOptions.ContinuedStreams"/>) refuses
    /// <see cref="StreamLayout.Original"/> from <see cref="Simulator.Create"/>, before any setup or
    /// accelerator work exists. No formulation is built here: there is nothing to run
    /// <see cref="Statistics.Setup.Prepare"/> against, which is itself the proof that no setup work happens
    /// before the refusal. Mutation proof: removing this check from
    /// <c>SimulationOptionsValidator.Validate</c> makes <see cref="Simulator.Create"/> return normally,
    /// turning <c>Assert.Throws</c> red.
    /// </summary>
    [Fact]
    public void CreateRejectsOriginalLayoutWithBatchedModeBeforeAnySetupWork()
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
        };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        Assert.Contains(nameof(StreamLayout.Original), exception.Message);
        Assert.Contains(nameof(ExecutionMode.Batched), exception.Message);
        Assert.Null(exception.SetupFailure); // not a Statistics.Setup.Prepare precondition
    }

    /// <summary>
    /// The "not merely the obvious route" proof the root asked for by name: the degenerate one-particle,
    /// continued-streams configuration is still <see cref="ExecutionMode.Batched"/>, and is refused exactly
    /// like a plain batch even though this particular call is sequential by construction —
    /// <c>DegenerateConfigurationTests</c> runs this same shape (batch size 1, attempts per launch 1,
    /// continued streams) to completion under <see cref="StreamLayout.Independent"/>, so "Reference mode is
    /// batched mode degenerated" needs <em>both</em> <see cref="PrecisionKind.Binary64"/> and
    /// <see cref="StreamLayout.Independent"/> (root BOOT.md; `src/Simulation/BOOT.md`, "## Invariants"), not
    /// a loophole this refusal leaves open for the `Original` layout. Mutation proof: removing the check
    /// from <c>SimulationOptionsValidator.Validate</c> makes <see cref="Simulator.Create"/> return normally,
    /// turning <c>Assert.Throws</c> red.
    /// </summary>
    [Fact]
    public void CreateRejectsOriginalLayoutWithDegenerateContinuedBatchBeforeAnySetupWork()
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            BatchSize = 1,
            AttemptsPerLaunch = 1,
            ContinuedStreams = true,
        };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        Assert.Contains(nameof(StreamLayout.Original), exception.Message);
    }

    /// <summary>Reference mode is unaffected by the batched refusal: <see cref="StreamLayout.Original"/> is exactly what it exists for.</summary>
    [Fact]
    public void RunOriginalLayoutWithReferenceModeSucceeds()
    {
        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Streams = StreamLayout.Original,
        });
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(StreamLayout.Original, result.Diagnostics.Streams);
    }

    /// <summary><see cref="StreamLayout.Independent"/> is unaffected by the refusal in either mode.</summary>
    [Fact]
    public void RunIndependentLayoutWithBatchedModeSucceeds()
    {
        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Independent,
        });
        var formulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(StreamLayout.Independent, result.Diagnostics.Streams);
    }

    [Fact]
    public void RunIndependentLayoutWithReferenceModeSucceeds()
    {
        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Streams = StreamLayout.Independent,
        });
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(StreamLayout.Independent, result.Diagnostics.Streams);
    }

    /// <summary>
    /// The default-unaffected proof, the twin of <c>PrecisionKindTests.RunWithPrecisionUnsetDefaultsToBinary64AndIsRecordedAsSuch</c>:
    /// leaving <see cref="SimulationOptions.Streams"/> unset gives <see cref="StreamLayout.Independent"/>,
    /// recorded as such (root BOOT.md, "Two stream layouts"; `src/Simulation/BOOT.md`, "Default stream
    /// layout").
    /// </summary>
    [Fact]
    public void RunWithStreamsUnsetDefaultsToIndependentAndIsRecordedAsSuch()
    {
        using var simulator = Simulator.Create(new SimulationOptions { Mode = ExecutionMode.Reference });
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(StreamLayout.Independent, result.Diagnostics.Streams);
    }

    /// <summary>
    /// The coherence proof this decision exists for: before it, the default configuration (`Mode =
    /// Batched`, `Streams = Original`) was exactly the combination the refusal above rejects, so every bare
    /// invocation refused itself. A completely bare <see cref="SimulationOptions"/> — no <see cref="SimulationOptions.Mode"/>,
    /// no <see cref="SimulationOptions.Streams"/>, nothing but the formulation — must now run to completion,
    /// which is the bar a bare `propstruct run &lt;file.dat&gt;` meets (`src/Cli/API.md`: "Run options
    /// (defaults are `SimulationOptions`'"). Mutation proof: reverting <see cref="SimulationOptions.Streams"/>'
    /// own default to <see cref="StreamLayout.Original"/> turns this from `Ok` into
    /// <see cref="SimulationFailedException"/>, the exact defect this task fixes.
    /// </summary>
    [Fact]
    public void RunWithEveryOptionAtItsOwnDefaultSucceeds()
    {
        using var simulator = Simulator.Create(new SimulationOptions());
        var formulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(ExecutionMode.Batched, result.Diagnostics.Mode);
        Assert.Equal(StreamLayout.Independent, result.Diagnostics.Streams);
        Assert.Equal(PrecisionKind.Binary64, result.Diagnostics.Precision);
    }
}
