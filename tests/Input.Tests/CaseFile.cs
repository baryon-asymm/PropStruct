using System.Text.Json;
using System.Text.Json.Serialization;
using PropStruct.Tests.Harness;

namespace PropStruct.Input.Tests;

/// <summary>
/// Deserialization shapes for the constructed cases of <c>tests/Fixtures/cases/input/</c> (BOOT.md,
/// "Invariants": expected values live in those files, never as strings in test code) and the small helpers
/// that load them.
/// </summary>
internal static class CaseFile
{
    public static string Directory { get; } = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "input");

    public static IReadOnlyDictionary<string, PositiveCase> ReadPositiveCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "positive-cases.json"));
        return JsonSerializer.Deserialize(json, CaseFileJsonContext.Default.DictionaryStringPositiveCase)!;
    }

    public static IReadOnlyDictionary<string, MalformedCase> ReadMalformedCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "malformed-cases.json"));
        return JsonSerializer.Deserialize(json, CaseFileJsonContext.Default.DictionaryStringMalformedCase)!;
    }

    public static ModelParametersCase ReadModelParametersDefault()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "model-parameters-default.json"));
        return JsonSerializer.Deserialize(json, CaseFileJsonContext.Default.ModelParametersCase)!;
    }
}

/// <summary>
/// Source-generated (de)serialization for <see cref="CaseFile"/>'s own shapes: a plain
/// <c>JsonSerializer.Deserialize&lt;T&gt;</c> call gives CA1812 no evidence that these record types are ever
/// constructed (reflection-based deserialization is invisible to it), and none of them may be removed — they are
/// the fixture-file shapes the taboo "no expected value typed into a test when it exists in a fixture file"
/// depends on. The generated context also instantiates <see cref="FractionCase"/>, reached through
/// <see cref="PositiveCase.Fractions"/>.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Dictionary<string, PositiveCase>))]
[JsonSerializable(typeof(Dictionary<string, MalformedCase>))]
[JsonSerializable(typeof(ModelParametersCase))]
[JsonSerializable(typeof(FractionCase))]
internal sealed partial class CaseFileJsonContext : JsonSerializerContext;

internal sealed record PositiveCase(
    bool ReadPocketFormingFractions,
    double OxidizerDensity,
    double PropellantDensity,
    double OxidizerMassFraction,
    double MetalMassFraction,
    double Ak1,
    double Ak2,
    double Ak3,
    double Ak4,
    int SizeLawCode,
    [property: JsonConverter(typeof(JsonStringEnumConverter<SizeLaw>))] SizeLaw SizeLaw,
    int Cycles,
    int ParticlesPerCycle,
    double GeneratorWarmup,
    FractionCase[] Fractions,
    int[]? PocketFormingFractions);

internal sealed record FractionCase(double MassShare, double LowerBound, double UpperBound);

internal sealed record MalformedCase(int LineNumber, string MessageContains);

internal sealed record ModelParametersCase(
    double DminMetres,
    double CellSizeMetres,
    double CategoryStepMetres,
    double EpsDok,
    double Alpha,
    double NnMin,
    double PocketCoefficient,
    double BridgeCoefficient,
    double TailProbability,
    double NnMax,
    double HomogenizedOxidizerFraction,
    bool ReadPocketFormingFractions,
    int Variant,
    double AggregatedOxideFraction);
