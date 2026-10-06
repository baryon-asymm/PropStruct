using System.Text.Json;
using System.Text.Json.Serialization;

namespace PropStruct.RateTableTool;

/// <summary>
/// The shape of <c>tests/Fixtures/rate-table.json</c> (root BOOT.md, "the pass condition compares failure rates,
/// not single runs"; <c>tests/Fixtures/BOOT.md</c>, "## Rate table"): a pregenerated, compact verdict per port
/// run, never the <c>results.m</c> files themselves. These types stay <c>internal</c> — only this node's own
/// <see cref="Program"/> writes the file; <c>tests/Harness.Tests/RateCriterionTests</c> (a neighbour) reads the
/// same file with its own small deserialization against the documented field names
/// (<c>tests/Fixtures/API.md</c>, "## Rate table"), not this CLR type — the schema is the contract, not the
/// class (the same reason <c>exclusions.json</c>/<c>provenance.json</c> are read independently by every
/// consumer that needs them, never through a shared model type).
///
/// ⚠ 2026-09-25: moved here from <c>tests/Simulation.Tests/RateTable.cs</c>, unchanged in shape, alongside
/// <see cref="ReferenceModeRunner"/> and the generator that used to be
/// <c>tests/Simulation.Tests/RateTableGenerator.Regenerate</c> — see that type's own dated note.
/// </summary>
internal sealed record RateTableRun(
    string Formulation, string Layout, string Precision, int SeedIndex, ulong Seed,
    int FailingCells, IReadOnlyList<string> FailingNames, string ResultSha256);

internal sealed record RateTable(
    string GeneratedAtUtc, string Commit, string CriterionSha256, string SnapshotSha256,
    string FixturesSha256, string SeedStride, IReadOnlyList<RateTableRun> Runs)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static RateTable Load(string path) =>
        JsonSerializer.Deserialize<RateTable>(File.ReadAllText(path), Options)
        ?? throw new InvalidOperationException($"'{path}' deserialized to null.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
}
