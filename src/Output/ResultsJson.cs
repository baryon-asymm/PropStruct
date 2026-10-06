using System.Text.Json;
using System.Text.Json.Serialization;
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
    };

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
