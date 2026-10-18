using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PropStruct.Simulation;

namespace PropStruct.Output;

/// <summary>
/// Writes and reads a <see cref="SimulationResult"/> as JSON for programs (BOOT.md, "Design decisions
/// (2026-09-19)"): <c>System.Text.Json</c> over the record graph, property names as declared, doubles
/// written round-trip, <c>NaN</c> and infinities allowed. Round trip means: written, read back, written
/// again gives the same bytes, and the records compare equal field by field with bitwise <c>double</c>
/// equality (this node's acceptance criteria). <see cref="Pockets.Qdokso"/> is an
/// <c>ImmutableArray&lt;ImmutableArray&lt;double&gt;&gt;</c> (`Simulation/API.md`, dated 2026-09-24), which
/// <c>System.Text.Json</c> serializes as nested arrays, rows first, natively: no custom converter is needed
/// for it (the node no longer supplies one; it did while the field was a rectangular <c>double[,]</c>).
/// </summary>
public static class ResultsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { OmitWhatOriginalNeverWrote } },
    };

    /// <summary>
    /// The one contract modifier (BOOT.md, "## Corrected model", "JSON"): <see cref="RunDiagnostics.Model"/> is
    /// omitted when it is <see cref="ModelKind.Original"/> and <see cref="StoredSetup.Ak1"/>, <c>Ak2</c> and
    /// <c>Ak4</c> when <see langword="null"/>, so an <c>Original</c> result's JSON keeps the bytes it had before
    /// the model kind existed. A missing <c>Model</c> reads back <see cref="ModelKind.Original"/> and a missing
    /// ratio <see langword="null"/>, which keeps the round trip exact under both models.
    /// </summary>
    private static void OmitWhatOriginalNeverWrote(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(RunDiagnostics))
        {
            foreach (var property in typeInfo.Properties.Where(p => p.Name == nameof(RunDiagnostics.Model)))
            {
                property.ShouldSerialize = static (_, value) => value is not ModelKind.Original;
            }
        }
        else if (typeInfo.Type == typeof(StoredSetup))
        {
            string[] ratios = [nameof(StoredSetup.Ak1), nameof(StoredSetup.Ak2), nameof(StoredSetup.Ak4)];
            foreach (var property in typeInfo.Properties.Where(p => ratios.Contains(p.Name)))
            {
                property.ShouldSerialize = static (_, value) => value is not null;
            }
        }
    }

    /// <summary>Writes <paramref name="result"/> as JSON to <paramref name="path"/>, overwriting it.</summary>
    public static void Write(SimulationResult result, string path)
    {
        ArgumentNullException.ThrowIfNull(result);
        var json = JsonSerializer.Serialize(result, Options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Reads a <see cref="SimulationResult"/> back from the JSON at <paramref name="path"/>.
    /// Throws <see cref="JsonException"/> on malformed JSON, per this node's <c>API.md</c>.
    /// </summary>
    public static SimulationResult Read(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SimulationResult>(json, Options)
            ?? throw new JsonException($"'{path}' deserialized to a null {nameof(SimulationResult)}.");
    }
}
