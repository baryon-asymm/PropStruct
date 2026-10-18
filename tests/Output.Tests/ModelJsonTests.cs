using System.Text.Json.Nodes;
using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The JSON modifier of <c>ResultsJson</c> (BOOT.md, "## Corrected model", "JSON"; ACCEPTANCE.md, "Corrected
/// model", the JSON criterion): an <c>Original</c> result's JSON holds none of <c>Model</c>, <c>Ak1</c>,
/// <c>Ak2</c>, <c>Ak4</c>; a <c>Corrected</c> result's holds all four and round-trips; a JSON written before
/// the option reads back <c>Original</c> with null ratios.
/// </summary>
public class ModelJsonTests
{
    private static readonly string[] ModelProperties =
        [nameof(RunDiagnostics.Model), nameof(StoredSetup.Ak1), nameof(StoredSetup.Ak2), nameof(StoredSetup.Ak4)];

    /// <summary>The sample with <c>Diagnostics.Model</c> and the three ratios set as the simulator sets them under <c>Corrected</c>.</summary>
    private static SimulationResult BuildCorrectedSample()
    {
        var sample = JsonRoundTripTests.BuildSample();
        return sample with
        {
            Diagnostics = sample.Diagnostics with { Model = ModelKind.Corrected },
            StoredSetup = sample.StoredSetup with { Ak1 = 0.45, Ak2 = 2.2, Ak4 = 4.7 },
        };
    }

    private static string WriteToText(SimulationResult result)
    {
        var path = Path.GetTempFileName();
        try
        {
            ResultsJson.Write(result, path);
            return File.ReadAllText(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SimulationResult ReadFromText(string json)
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            return ResultsJson.Read(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static bool HoldsProperty(JsonNode root, string section, string name) =>
        root[section]!.AsObject().ContainsKey(name);

    [Fact]
    public void AnOriginalResultsJsonHoldsNoneOfTheFourProperties()
    {
        var json = WriteToText(JsonRoundTripTests.BuildSample());
        var root = JsonNode.Parse(json)!;

        Assert.False(HoldsProperty(root, nameof(SimulationResult.Diagnostics), nameof(RunDiagnostics.Model)));
        foreach (var ratio in ModelProperties.Skip(1))
        {
            Assert.False(HoldsProperty(root, nameof(SimulationResult.StoredSetup), ratio), ratio);
        }

        Assert.DoesNotContain("\"Ak", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ACorrectedResultsJsonHoldsAllFourPropertiesAndRoundTripsEqualAndByteStable()
    {
        var corrected = BuildCorrectedSample();
        var json = WriteToText(corrected);
        var root = JsonNode.Parse(json)!;

        Assert.Equal(
            (int)ModelKind.Corrected,
            root[nameof(SimulationResult.Diagnostics)]![nameof(RunDiagnostics.Model)]!.GetValue<int>());
        Assert.Equal(corrected.StoredSetup.Ak1, root[nameof(SimulationResult.StoredSetup)]![nameof(StoredSetup.Ak1)]!.GetValue<double>());
        Assert.Equal(corrected.StoredSetup.Ak2, root[nameof(SimulationResult.StoredSetup)]![nameof(StoredSetup.Ak2)]!.GetValue<double>());
        Assert.Equal(corrected.StoredSetup.Ak4, root[nameof(SimulationResult.StoredSetup)]![nameof(StoredSetup.Ak4)]!.GetValue<double>());

        var roundTripped = ReadFromText(json);
        JsonRoundTripTests.AssertBitwiseEqual(corrected, roundTripped);
        Assert.Equal(ModelKind.Corrected, roundTripped.Diagnostics.Model);
        Assert.Equal(json, WriteToText(roundTripped));
    }

    [Fact]
    public void AJsonWrittenBeforeTheOptionReadsBackOriginalWithNullRatios()
    {
        // The pre-option shape: the Corrected JSON with the four properties removed, which is what a 0.1.0 file holds.
        var root = JsonNode.Parse(WriteToText(BuildCorrectedSample()))!;
        _ = root[nameof(SimulationResult.Diagnostics)]!.AsObject().Remove(nameof(RunDiagnostics.Model));
        foreach (var ratio in ModelProperties.Skip(1))
        {
            _ = root[nameof(SimulationResult.StoredSetup)]!.AsObject().Remove(ratio);
        }

        var legacy = ReadFromText(root.ToJsonString());

        Assert.Equal(ModelKind.Original, legacy.Diagnostics.Model);
        Assert.Null(legacy.StoredSetup.Ak1);
        Assert.Null(legacy.StoredSetup.Ak2);
        Assert.Null(legacy.StoredSetup.Ak4);
    }

    [Fact]
    public void AnOriginalResultRoundTripsEqualAndByteStableWithModelOriginalAndNullRatios()
    {
        var original = JsonRoundTripTests.BuildSample();
        var json = WriteToText(original);

        var roundTripped = ReadFromText(json);

        JsonRoundTripTests.AssertBitwiseEqual(original, roundTripped);
        Assert.Equal(json, WriteToText(roundTripped));
    }

    [Fact]
    public void AnExplicitNullRatioInAFileStillReadsAsNull()
    {
        var root = JsonNode.Parse(WriteToText(BuildCorrectedSample()))!;
        root[nameof(SimulationResult.StoredSetup)]![nameof(StoredSetup.Ak2)] = null;

        var read = ReadFromText(root.ToJsonString());

        Assert.Null(read.StoredSetup.Ak2);
        Assert.True(read.StoredSetup.Ak1.HasValue);
    }
}
