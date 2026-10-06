using PropStruct.Tests.Harness;

namespace PropStruct.Fixtures.Tests;

/// <summary>
/// The recorded SHA-256 of each of the original's five files (<c>tools/legacy/original.sha256</c>,
/// <c>tools/legacy/API.md</c>): what the provenance digests of the executable are compared with, since the files
/// themselves lie outside the repository. The manifest is read here, never typed.
/// </summary>
internal static class OriginalManifest
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Recorded = new(Read);

    /// <summary>The manifest's hash of the file named <paramref name="name"/> (one of the five manifest names).</summary>
    public static string Sha256Of(string name) => Recorded.Value[name];

    private static Dictionary<string, string> Read() =>
        File.ReadAllLines(RepositoryPaths.Resolve("tools", "legacy", "original.sha256"))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split("  ", 2))
            .ToDictionary(fields => fields[1], fields => fields[0], StringComparer.Ordinal);
}
