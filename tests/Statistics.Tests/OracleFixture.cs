using System.Collections.Frozen;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using PropStruct.Tests.Harness;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// One value or one array of the listing oracle, as the fixture holds it
/// (<c>tests/Fixtures/API.md</c>, "## Listing oracle"): its Fortran type, its dimensions in the Fortran's
/// column-major order, and its bytes, little-endian, element after element.
/// </summary>
internal sealed record OracleSlot(string Type, IReadOnlyList<int> Dimensions, byte[] Data)
{
    private int Width => Type is "r4" or "i4" ? 4 : 8;

    /// <summary>The number of elements.</summary>
    public int Count => Data.Length / Width;

    /// <summary>Element <paramref name="index"/> widened to <see cref="double"/>, exactly (a count stays below 2^53).</summary>
    public double At(int index) => Type switch
    {
        "r4" => BitConverter.ToSingle(Data, 4 * index),
        "r8" => BitConverter.ToDouble(Data, 8 * index),
        "i4" => BitConverter.ToInt32(Data, 4 * index),
        "i8" => BitConverter.ToInt64(Data, 8 * index),
        _ => throw new InvalidDataException($"the oracle type {Type} is not one of r4, r8, i4, i8."),
    };

    /// <summary>Every element, widened.</summary>
    public double[] Values()
    {
        var values = new double[Count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = At(i);
        }

        return values;
    }

    /// <summary>Reads a fixture entry: <c>bits</c> (a scalar, most significant byte first) or <c>z</c> (zlib, base64).</summary>
    public static OracleSlot Read(JsonElement entry)
    {
        var type = entry.GetProperty("type").GetString() ?? throw new InvalidDataException("an oracle entry without a type.");
        if (entry.TryGetProperty("bits", out var bits))
        {
            var data = Convert.FromHexString(bits.GetString() ?? string.Empty);
            Array.Reverse(data);
            return new OracleSlot(type, [], data);
        }

        var dimensions = entry.GetProperty("dims").EnumerateArray().Select(d => d.GetInt32()).ToArray();
        using var compressed = new MemoryStream(Convert.FromBase64String(entry.GetProperty("z").GetString() ?? string.Empty));
        using var inflater = new ZLibStream(compressed, CompressionMode.Decompress);
        using var inflated = new MemoryStream();
        inflater.CopyTo(inflated);
        return new OracleSlot(type, dimensions, inflated.ToArray());
    }
}

/// <summary>One cycle of one case: the totals injected, what the executable's plane left, the exit it took.</summary>
internal sealed record OracleCycle(
    int Cycle, string Exit, IReadOnlyDictionary<string, OracleSlot> Totals, IReadOnlyDictionary<string, OracleSlot> Outputs,
    IReadOnlyList<(double Base, double Exponent, double Result)> PowTriples);

/// <summary>One case: the formulation and menu it ran, the setup the executable computed, its cycles.</summary>
internal sealed record OracleCase(
    string Id, string Formulation, IReadOnlyDictionary<string, double> Menu, int[]? Flags, JsonElement Setup,
    IReadOnlyDictionary<string, ExecutedBits[]> Preloop, IReadOnlyList<OracleCycle> Cycles)
{
    /// <summary>The executable's own value of a scalar pre-loop key (<c>lambda</c>, <c>dokm</c>, ...), widened.</summary>
    public double Executed(string key) => Preloop[key].Single().Value;

    /// <summary>A scalar of the setup the oracle injected, as the number it is.</summary>
    public double SetupNumber(string key) => Setup.GetProperty(key).GetDouble();

    /// <summary>An array of the setup the oracle injected.</summary>
    public double[] SetupArray(string key) => Setup.GetProperty(key).EnumerateArray().Select(v => v.GetDouble()).ToArray();
}

/// <summary>
/// The listing oracle's fixture (<c>tests/Fixtures/cases/statistics/cycle_plane_oracle.json</c>), read once.
/// <c>PROPSTRUCT_ORACLE_FIXTURE</c> names another file; the proof that the pow-triple assertion goes red
/// uses it, and nothing else does.
/// </summary>
internal static class OracleFixture
{
    private const string OverrideVariable = "PROPSTRUCT_ORACLE_FIXTURE";

    private static readonly Lazy<JsonDocument> Document = new(() => JsonDocument.Parse(File.ReadAllText(Path)));

    private static readonly Lazy<IReadOnlyList<OracleCase>> LoadedCases = new(() => Document.Value.RootElement.GetProperty("cases")
        .EnumerateArray().Select(ReadCase).ToList());

    public static string Path => Environment.GetEnvironmentVariable(OverrideVariable)
        ?? RepositoryPaths.Resolve("tests", "Fixtures", "cases", "statistics", "cycle_plane_oracle.json");

    public static JsonElement Root => Document.Value.RootElement;

    public static IReadOnlyList<OracleCase> Cases => LoadedCases.Value;

    /// <summary>The SHA-256 of a file of the repository, as the provenance block writes it.</summary>
    public static string Sha256(params string[] relativePath) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(RepositoryPaths.Resolve(relativePath))));

    private static OracleCase ReadCase(JsonElement entry)
    {
        var menu = entry.GetProperty("menu").EnumerateObject().ToFrozenDictionary(p => p.Name, p => p.Value.GetDouble(), StringComparer.Ordinal);
        var flags = entry.GetProperty("flags");
        var preloop = entry.GetProperty("preloop").EnumerateObject().ToFrozenDictionary(
            p => p.Name, p => ExecutedBits.Read(p.Value.GetProperty("executed")), StringComparer.Ordinal);
        var cycles = entry.GetProperty("cycles").EnumerateArray().Select(ReadCycle).ToList();
        return new OracleCase(
            entry.GetProperty("id").GetString()!, entry.GetProperty("formulation").GetString()!, menu,
            flags.ValueKind == JsonValueKind.Null ? null : flags.EnumerateArray().Select(v => v.GetInt32()).ToArray(),
            entry.GetProperty("setup"), preloop, cycles);
    }

    private static OracleCycle ReadCycle(JsonElement entry)
    {
        var totals = entry.GetProperty("totals").EnumerateObject().ToFrozenDictionary(p => p.Name, p => OracleSlot.Read(p.Value), StringComparer.Ordinal);
        var outputs = entry.GetProperty("outputs").EnumerateObject().ToFrozenDictionary(p => p.Name, p => OracleSlot.Read(p.Value), StringComparer.Ordinal);
        var triples = entry.GetProperty("pow").EnumerateArray()
            .Select(t => (Hex(t[0]), Hex(t[1]), Hex(t[2]))).ToList();
        return new OracleCycle(entry.GetProperty("cycle").GetInt32(), entry.GetProperty("exit").GetString()!, totals, outputs, triples);
    }

    private static double Hex(JsonElement bits) => BitConverter.Int64BitsToDouble(long.Parse(bits.GetString()!, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
}
