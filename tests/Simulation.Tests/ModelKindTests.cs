using System.Reflection;
using System.Runtime.CompilerServices;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The rows of <c>BOOT.md</c> for the model kind (<c>src/Simulation/BOOT.md</c>, "## Corrected model";
/// <c>ACCEPTANCE.md</c>, "Corrected model"): the surface, the mapping onto <see cref="CorrectionSet"/> on a
/// known answer, the refusal, the seam with its two controls, determinism and the labelling. Every run is
/// reference mode on the CPU over P33 trimmed to a few particles: the quantity read, <c>Dok43sd</c>, is the
/// analytical one the setup computes, so the particle count does not move it.
/// </summary>
public class ModelKindTests
{
    // The printed Dok43sd of P33 under DoksdConsistent, micrometres at the print's two decimals: the
    // acceptance criterion's own figure (src/Simulation/ACCEPTANCE.md, the mapping row), measured by
    // src/Statistics under that flag (docs/ORIGINAL-DEFECTS.md, row 399-400). No fixture holds it: the
    // archived runs are all Original.
    private const double CorrectedP33Dok43sd = 112.66;

    private static readonly string[] FormulationFixture = ["tests", "Fixtures", "Legacy", "formulations", "P33.dat"];

    private static Formulation SmallP33() =>
        DatFile.Read(RepositoryPaths.Resolve(FormulationFixture)) with { ParticlesPerCycle = 4, Cycles = 1 };

    private static SimulationOptions Reference(ModelKind model) => new()
    {
        Mode = ExecutionMode.Reference,
        Accelerator = AcceleratorKind.Cpu,
        Streams = StreamLayout.Independent,
        Model = model,
    };

    private static SimulationResult Run(SimulationOptions options)
    {
        using var simulator = Simulator.Create(options);
        return simulator.Run(SmallP33());
    }

    /// <summary>The value <c>results.m</c> prints as <c>Dok43sd</c> (micrometres, two decimals), read from the result as the writer reads it.</summary>
    private static double PrintedDok43sd(SimulationResult result) => Math.Round(Math.Sqrt(result.Header.Doksd) * 1e6, 2);

    /// <summary>The same normalisation the acceptance criterion names: the model label, the wall time and the three ratios.</summary>
    private static SimulationResult Normalised(SimulationResult result) => result with
    {
        Diagnostics = result.Diagnostics with { Model = ModelKind.Original, Elapsed = TimeSpan.Zero },
        StoredSetup = result.StoredSetup with { Ak1 = null, Ak2 = null, Ak4 = null },
    };

    private static double ArchivedOriginalDok43sd() =>
        ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "references", "P33", "results.m.txt"))["Dok43sd"][0];

    [Fact]
    public void TheSurfaceHasTheExplicitValuesTheDefaultAndBodyInitProperties()
    {
        Assert.Equal(0, (int)ModelKind.Original);
        Assert.Equal(1, (int)ModelKind.Corrected);
        Assert.Equal(ModelKind.Original, new SimulationOptions().Model);

        AssertBodyInit(typeof(RunDiagnostics), nameof(RunDiagnostics.Model), typeof(ModelKind));
        foreach (var ratio in new[] { nameof(StoredSetup.Ak1), nameof(StoredSetup.Ak2), nameof(StoredSetup.Ak4) })
        {
            AssertBodyInit(typeof(StoredSetup), ratio, typeof(double?));
        }
    }

    /// <summary>A body init property: public, init-only, and no parameter of the 0.1.0 positional constructor.</summary>
    private static void AssertBodyInit(Type record, string name, Type type)
    {
        var property = record.GetProperty(name) ?? throw new InvalidOperationException($"{record.Name}.{name} is missing.");
        Assert.Equal(type, property.PropertyType);
        Assert.Contains(typeof(IsExternalInit), property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
        var constructor = Assert.Single(record.GetConstructors(BindingFlags.Public | BindingFlags.Instance), c => c.GetParameters().Length > 0);
        Assert.DoesNotContain(constructor.GetParameters(), p => p.Name == name);
    }

    [Fact]
    public void OriginalPrintsTheArchivedDoksdOfP33()
    {
        var result = Run(Reference(ModelKind.Original));

        Assert.Equal(ArchivedOriginalDok43sd(), PrintedDok43sd(result));
        Assert.Equal(ModelKind.Original, result.Diagnostics.Model);
    }

    [Fact]
    public void CorrectedPrintsTheConsistentDoksdOfP33()
    {
        var result = Run(Reference(ModelKind.Corrected));

        Assert.Equal(CorrectedP33Dok43sd, PrintedDok43sd(result));
        Assert.NotEqual(ArchivedOriginalDok43sd(), PrintedDok43sd(result));
    }

    [Fact]
    public void AnOriginalRunsResultsAndJsonCarryNoModelLabel()
    {
        var result = Run(Reference(ModelKind.Original));
        var formulation = SmallP33();
        var resultsM = Path.GetTempFileName();
        var json = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, ModelParameters.Default, result, resultsM);
            ResultsJson.Write(result, json);

            Assert.DoesNotContain("% Model kind", File.ReadAllText(resultsM), StringComparison.Ordinal);
            var text = File.ReadAllText(json);
            foreach (var name in new[] { nameof(RunDiagnostics.Model), nameof(StoredSetup.Ak1), nameof(StoredSetup.Ak2), nameof(StoredSetup.Ak4) })
            {
                Assert.DoesNotContain($"\"{name}\"", text, StringComparison.Ordinal);
            }
        }
        finally
        {
            File.Delete(resultsM);
            File.Delete(json);
        }
    }

    [Fact]
    public void CorrectedWithOriginalPrecisionIsRefusedInReferenceModeNamingBothHalves()
    {
        var options = Reference(ModelKind.Corrected) with { Precision = PrecisionKind.Original };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        Assert.Null(exception.SetupFailure);
        Assert.Contains(nameof(ModelKind.Corrected), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(PrecisionKind.Original), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(PrecisionKind.Binary64), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Batched mode holds the two batched refusals first: the same options under the two models fail with the
    /// same message, word for word, so the model refusal never replaces either of them.
    /// </summary>
    [Theory]
    [InlineData(PrecisionKind.Original, StreamLayout.Independent)]
    [InlineData(PrecisionKind.Binary64, StreamLayout.Original)]
    public void TheTwoBatchedRefusalsKeepTheirMessagesUnderCorrected(PrecisionKind precision, StreamLayout streams)
    {
        var original = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Precision = precision,
            Streams = streams,
        };

        var expected = Assert.Throws<SimulationFailedException>(() => Simulator.Create(original));
        var actual = Assert.Throws<SimulationFailedException>(() => Simulator.Create(original with { Model = ModelKind.Corrected }));

        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(RunStatus.InvalidSetup, actual.Status);
    }

    [Theory]
    [InlineData(ExecutionMode.Reference, StreamLayout.Original)]
    [InlineData(ExecutionMode.Reference, StreamLayout.Independent)]
    [InlineData(ExecutionMode.Batched, StreamLayout.Independent)]
    public void CorrectedWithBinary64RunsToCompletionInBothModesAndBothLayouts(ExecutionMode mode, StreamLayout streams)
    {
        var result = Run(Reference(ModelKind.Corrected) with { Mode = mode, Streams = streams });

        Assert.Equal(ModelKind.Corrected, result.Diagnostics.Model);
        Assert.Equal(mode, result.Diagnostics.Mode);
        Assert.Equal(streams, result.Diagnostics.Streams);
    }

    [Fact]
    public void TheSeamIsRefusedUnderOriginal()
    {
        var options = Reference(ModelKind.Original) with { Corrections = CorrectionSet.None };

        _ = Assert.Throws<ArgumentException>(() => Simulator.Create(options));
    }

    [Theory]
    [InlineData((uint)CorrectionSet.All + 1)]
    [InlineData(0x8000_0000u)]
    public void TheSeamIsRefusedForABitOutsideAll(uint bits)
    {
        var options = Reference(ModelKind.Corrected) with { Corrections = (CorrectionSet)bits };

        _ = Assert.Throws<ArgumentException>(() => Simulator.Create(options));
    }

    [Fact]
    public void ARunWithTheSeamedSetAllIsTheCorrectedRun()
    {
        var own = Run(Reference(ModelKind.Corrected));
        var seamed = Run(Reference(ModelKind.Corrected) with { Corrections = CorrectionSet.All });

        ResultReflection.AssertBitIdentical(Normalised(own), Normalised(seamed));
        ResultReflection.AssertBitIdentical(own.StoredSetup, seamed.StoredSetup);
    }

    /// <summary>
    /// The negative control of the seam: no flag set under <c>Corrected</c> is the <c>Original</c> run, bit for
    /// bit, once the label, the wall time and the three ratios are normalised.
    /// </summary>
    [Fact]
    public void CorrectedWithNoFlagSetEqualsTheOriginalRunNormalised()
    {
        var original = Run(Reference(ModelKind.Original));
        var seamNone = Run(Reference(ModelKind.Corrected) with { Corrections = CorrectionSet.None });

        Assert.Equal(ModelKind.Corrected, seamNone.Diagnostics.Model);
        ResultReflection.AssertBitIdentical(Normalised(original), Normalised(seamNone));
    }

    /// <summary>
    /// The positive control of the seam: the one flag <see cref="CorrectionSet.DoksdConsistent"/> moves
    /// <c>Dok43sd</c> to the corrected figure, its evidence case in <c>src/Statistics</c>.
    /// </summary>
    [Fact]
    public void ASingleFlagSeamRunDiffersFromOriginalWhereThatFlagIsVisible()
    {
        var original = Run(Reference(ModelKind.Original));
        var single = Run(Reference(ModelKind.Corrected) with { Corrections = CorrectionSet.DoksdConsistent });

        Assert.Equal(CorrectedP33Dok43sd, PrintedDok43sd(single));
        Assert.NotEqual(PrintedDok43sd(original), PrintedDok43sd(single));
    }

    [Fact]
    public void ACorrectedRunRepeatedGivesTheSameResultElapsedTimeAside()
    {
        var first = Run(Reference(ModelKind.Corrected));
        var second = Run(Reference(ModelKind.Corrected));

        ResultReflection.AssertBitIdentical(Normalised(first), Normalised(second));
    }

    [Fact]
    public void ACorrectedRunsDiagnosticsAndStoredRatiosEqualTheSetupsOwn()
    {
        var formulation = SmallP33();
        var options = Reference(ModelKind.Corrected);
        var result = Run(options);

        var status = Statistics.Setup.Prepare(
            formulation, options.Parameters, Particle.PrecisionKind.Binary64, CorrectionSet.All, options.NeighbourBudget,
            options.PocketRedrawBudget, out var setup, out _, out _, out _, out _);

        Assert.Equal(Statistics.SetupStatus.Ok, status);
        Assert.Equal(ModelKind.Corrected, result.Diagnostics.Model);
        Assert.Equal(setup.Ak1, result.StoredSetup.Ak1);
        Assert.Equal(setup.Ak2, result.StoredSetup.Ak2);
        Assert.Equal(setup.Ak4, result.StoredSetup.Ak4);
    }
}
