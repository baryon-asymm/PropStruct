using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L0/L1 of the BOOT.md table: <see cref="SimulationOptions.Precision"/> reaches the particle program and
/// the result unchanged, and <see cref="PrecisionKind.Original"/> is refused with batched mode by any
/// route (root BOOT.md, "Precision kind is an option of every run"), from <see cref="Simulator.Create"/>
/// itself since architecture audit finding R1 (2026-09-24; `SimulationOptionsValidator`).
/// </summary>
public class PrecisionKindTests
{
    /// <summary>
    /// The weak half of the carrying proof: two reference-mode runs of the same small, synthetic formulation
    /// and seed, differing only in <see cref="SimulationOptions.Precision"/>, must disagree <em>somewhere</em>
    /// in the object graph. A diagnostics-only echo (asserting <c>result.Diagnostics.Precision ==
    /// options.Precision</c>) would pass even if the option were recorded but never wired into
    /// <c>ModelSetup.Kind</c>; this instead proves the option reaches <c>Particle.Attempt.Run</c> at all, by
    /// observing that <em>some</em> bit of the rounding it is documented to cause survives to the result.
    ///
    /// This is deliberately not the bar for "the feature works": <c>Attempt.Run</c>'s own write sites round
    /// every addition against whatever the record already holds
    /// (<c>Particle/API.md</c>, "Precision kind"), so the size of the effect this test can detect depends
    /// entirely on what the record is seeded with before the particle's first attempt — a decision this
    /// node's own <c>Engine.RunReferenceParticle</c> makes, not <c>Particle</c>'s (BOOT.md, "Record seeding
    /// under Original accumulation"). Before that seeding was corrected, every particle's record started at
    /// zero, was folded once, and this test still passed: the rounding was real but confined to a few dozen
    /// terms near zero, invisible in <c>results.m</c>'s printed precision on any formulation. A test that only
    /// asks "did anything differ, anywhere" cannot tell that apart from the effect a user can actually see;
    /// <see cref="RunOriginalPrecisionProducesPrintedOutputThatDiffersBeyondTheFooter"/> below is the test
    /// that can.
    /// </summary>
    [Fact]
    public void RunOriginalPrecisionDisagreesWithBinary64PrecisionAtTheSameSeed()
    {
        var formulation = SmallFormulation.Build(particlesPerCycle: 100, cycles: 1);

        using var doubleRun = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Seed = 0UL,
        });
        var doubleResult = doubleRun.Run(formulation);

        using var originalRun = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Seed = 0UL,
            Precision = PrecisionKind.Original,
        });
        var originalResult = originalRun.Run(formulation);

        Assert.Equal(PrecisionKind.Binary64, doubleResult.Diagnostics.Precision);
        Assert.Equal(PrecisionKind.Original, originalResult.Diagnostics.Precision);

        var different = false;
        foreach (var assertSame in new Action[]
        {
            () => ResultReflection.AssertBitIdentical(doubleResult.Counters, originalResult.Counters),
            () => ResultReflection.AssertBitIdentical(doubleResult.Histograms, originalResult.Histograms),
            () => ResultReflection.AssertBitIdentical(doubleResult.Pockets, originalResult.Pockets),
            () => ResultReflection.AssertBitIdentical(doubleResult.LocalStructure, originalResult.LocalStructure),
            () => ResultReflection.AssertBitIdentical(doubleResult.Agglomerates, originalResult.Agglomerates),
        })
        {
            try
            {
                assertSame();
            }
            catch (Xunit.Sdk.XunitException)
            {
                different = true;
                break;
            }
        }

        Assert.True(different,
            "Original accumulation must disagree with Binary64 accumulation at the same seed; an equal result " +
            "means Precision never reached ModelSetup.Kind.");
    }

    /// <summary>
    /// The bar a user actually meets: <c>propstruct run &lt;formulation&gt; --mode reference --seed 0</c> with
    /// <c>--precision double</c> against the same command with <c>--precision original</c> must produce
    /// <c>results.m</c> files that differ in more than the one footer line naming the kind
    /// (<c>src/Output/API.md</c>'s own footer; matched here by the substring <c>"Precision:"</c>, never a
    /// neighbour's source). This reproduces exactly the command line the root's own acceptance-chain report
    /// ran (two P33 runs differing only in the flag), using the same real fixture <c>Particle</c> is the
    /// authority on: <see cref="SmallFormulation"/>'s synthetic input is too small a set of write sites, over
    /// too few attempts, to move a printed digit even under the corrected seeding above.
    ///
    /// What it looks like when it fails (the defect this guards against, and the exact state of the tree
    /// before <c>Engine.RunReferenceParticle</c>'s seeding fix): every line of the two files is identical
    /// except the footer's own descriptive sentence, so <c>differingLines</c> is empty and this assertion
    /// reports 0 differing lines, none of them the footer — the option changed nothing the model actually
    /// computed, only what it said about itself.
    /// </summary>
    [Fact]
    public void RunOriginalPrecisionProducesPrintedOutputThatDiffersBeyondTheFooter()
    {
        var formulation = DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "P33.dat"));

        using var doubleRun = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Seed = 0UL,
            Precision = PrecisionKind.Binary64,
        });
        var doubleResult = doubleRun.Run(formulation);

        using var originalRun = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Seed = 0UL,
            Precision = PrecisionKind.Original,
        });
        var originalResult = originalRun.Run(formulation);

        var doublePath = Path.GetTempFileName();
        var originalPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, ModelParameters.Default, doubleResult, doublePath);
            ResultsMWriter.Write(formulation, ModelParameters.Default, originalResult, originalPath);

            var doubleLines = File.ReadAllLines(doublePath);
            var originalLines = File.ReadAllLines(originalPath);

            Assert.Equal(doubleLines.Length, originalLines.Length);

            var differingLines = Enumerable.Range(0, doubleLines.Length)
                .Where(i => doubleLines[i] != originalLines[i])
                .Where(i => !doubleLines[i].Contains("Precision:") && !originalLines[i].Contains("Precision:"))
                .ToList();

            Assert.True(differingLines.Count > 0,
                "Original accumulation must produce a results.m that differs from Binary64 accumulation's in more " +
                "than the footer's own descriptive sentence; 0 differing lines outside the footer means the " +
                "option changed nothing the model computed, only what it said about itself.");
        }
        finally
        {
            File.Delete(doublePath);
            File.Delete(originalPath);
        }
    }

    /// <summary>
    /// The obvious route: a plain batch (no <see cref="SimulationOptions.ContinuedStreams"/>) refuses
    /// <see cref="PrecisionKind.Original"/> from <see cref="Simulator.Create"/> itself, before any setup or
    /// accelerator work exists (architecture audit finding R1, 2026-09-24:
    /// <c>SimulationOptionsValidator</c>, called before <c>Execution.Engine.Create</c>). No formulation is
    /// built or passed here at all — there is nothing to run <c>Statistics.Setup.Prepare</c> against — which
    /// is itself the proof that no setup work happens before the refusal: before this task the same
    /// combination completed <see cref="Statistics.Setup.Prepare"/>, the tail draw and <c>Engine.Load</c>
    /// before failing deep inside <c>Engine.RunBatch</c>. Mutation proof: removing this check from
    /// <c>SimulationOptionsValidator.Validate</c> makes <see cref="Simulator.Create"/> return normally,
    /// turning <c>Assert.Throws</c> red (nothing else in this test could throw, since no formulation exists).
    /// </summary>
    [Fact]
    public void CreateRejectsOriginalPrecisionWithBatchedModeBeforeAnySetupWork()
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Precision = PrecisionKind.Original,
        };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        Assert.Contains(nameof(PrecisionKind.Original), exception.Message);
        Assert.Contains(nameof(ExecutionMode.Batched), exception.Message);
        Assert.Null(exception.SetupFailure); // not a Statistics.Setup.Prepare precondition
    }

    /// <summary>
    /// The "not merely the obvious route" proof the root asked for by name: the degenerate one-particle,
    /// continued-streams configuration is still <see cref="ExecutionMode.Batched"/>, and is refused exactly
    /// like a plain batch even though this particular call is sequential by construction —
    /// <c>DegenerateConfigurationTests</c> runs this same shape (batch size 1, attempts per launch 1,
    /// continued streams) to completion under <see cref="PrecisionKind.Binary64"/>, so "Reference mode is
    /// batched mode degenerated" is a claim about <see cref="PrecisionKind.Binary64"/> only (root BOOT.md),
    /// not a loophole this refusal leaves open. Mutation proof: removing the check from
    /// <c>SimulationOptionsValidator.Validate</c> makes <see cref="Simulator.Create"/> return normally,
    /// turning <c>Assert.Throws</c> red.
    /// </summary>
    [Fact]
    public void CreateRejectsOriginalPrecisionWithDegenerateContinuedBatchBeforeAnySetupWork()
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Precision = PrecisionKind.Original,
            BatchSize = 1,
            AttemptsPerLaunch = 1,
            ContinuedStreams = true,
        };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        Assert.Contains(nameof(PrecisionKind.Original), exception.Message);
    }

    /// <summary>Reference mode is unaffected by the batched refusal: <see cref="PrecisionKind.Original"/> is exactly what it exists for.</summary>
    [Fact]
    public void RunOriginalPrecisionWithReferenceModeSucceeds()
    {
        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Precision = PrecisionKind.Original,
        });
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(PrecisionKind.Original, result.Diagnostics.Precision);
    }

    /// <summary>
    /// The default-unaffected proof: leaving <see cref="SimulationOptions.Precision"/> unset gives
    /// <see cref="PrecisionKind.Binary64"/>, recorded as such in the result — every caller before this
    /// option existed gets exactly the bits it always got (root BOOT.md, "Precision kind is an option of
    /// every run": "`Binary64` is the default").
    /// </summary>
    [Fact]
    public void RunWithPrecisionUnsetDefaultsToBinary64AndIsRecordedAsSuch()
    {
        using var simulator = Simulator.Create(new SimulationOptions { Mode = ExecutionMode.Reference });
        var formulation = SmallFormulation.Build(particlesPerCycle: 5, cycles: 1);

        var result = simulator.Run(formulation);

        Assert.Equal(PrecisionKind.Binary64, result.Diagnostics.Precision);
    }
}
