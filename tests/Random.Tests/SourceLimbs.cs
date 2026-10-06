using System.Text.Json;
using PropStruct.Tests.Harness;

namespace PropStruct.Random.Tests;

/// <summary>
/// The seed and multiplier limbs of the original's Fortran source, read from
/// <c>tests/Fixtures/cases/random/source_limbs.json</c>, which <c>tests/Fixtures/generate.py derive</c> writes from
/// the source (the integers written between slashes on a line range, the lines concatenated). The source lies outside
/// the repository (<c>tools/legacy</c>), so the fast set reads the committed limbs; that they equal the source's is
/// <c>Fixtures</c>' <c>Category=Legacy</c> check (<c>generate.py verify</c>). This node declares the Fortran source its
/// specification for this one purpose (AGENTS.md §12).
/// </summary>
internal static class SourceLimbs
{
    private static readonly Lazy<IReadOnlyDictionary<string, int[]>> RangesLazy = new(Read);

    /// <summary>
    /// The limbs recorded for the inclusive line range <paramref name="firstLine"/>..<paramref name="lastLine"/> (1-based,
    /// as printed in the source), in the order they appear.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The fixture records no limbs for that range.</exception>
    public static IReadOnlyList<int> Limbs(int firstLine, int lastLine) => RangesLazy.Value[$"{firstLine}-{lastLine}"];

    private static Dictionary<string, int[]> Read()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "random", "source_limbs.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("limbs").EnumerateObject()
            .ToDictionary(range => range.Name, range => range.Value.EnumerateArray().Select(limb => limb.GetInt32()).ToArray(), StringComparer.Ordinal);
    }
}
