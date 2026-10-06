using System.Security.Cryptography;
using System.Text.Json;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Fixtures.Tests;

/// <summary>
/// What the fast set knows of the original without the original (root <c>BOOT.md</c>, "## Delivery"): every file
/// under <c>tests/Fixtures/Legacy</c> is a data member of the archive, equal to it byte for byte by its SHA-256 in
/// <c>archive-members.sha256</c> (the member itself is compared in <c>Category=Legacy</c>, by
/// <c>LegacyArchiveTests</c>), and every digest of the executable that a provenance file records is the manifest's.
/// </summary>
public class LegacyManifestTests
{
    private static string FixturesRoot => RepositoryPaths.Resolve("tests", "Fixtures");

    [Fact]
    public void EveryFileUnderLegacyIsAListedMemberWithItsHashAndEveryListedMemberIsThere()
    {
        var listed = ListedMembers();
        var problems = new List<string>();
        var found = new HashSet<string>(StringComparer.Ordinal);
        var legacyRoot = Path.Combine(FixturesRoot, "Legacy");
        foreach (var path in Directory.EnumerateFiles(legacyRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(legacyRoot, path).Replace(Path.DirectorySeparatorChar, '/');
            var member = MemberNameOf(relative);
            if (member is null)
            {
                problems.Add($"Legacy/{relative} is neither a formulation nor an output of the archive");
                continue;
            }

            _ = found.Add(member);
            if (!listed.TryGetValue(member, out var recorded))
            {
                problems.Add($"Legacy/{relative}: member {member} is not in archive-members.sha256");
            }
            else if (!string.Equals(recorded, Sha256Of(File.ReadAllBytes(path)), StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"Legacy/{relative}: its SHA-256 is not the recorded {recorded}");
            }
        }

        problems.AddRange(listed.Keys.Where(member => !found.Contains(member)).Select(member => $"archive-members.sha256 lists {member}, which is not under Legacy/"));
        Assert.True(problems.Count == 0, string.Join("\n", problems.Take(20)) + (problems.Count > 20 ? $"\nand {problems.Count - 20} more" : string.Empty));
        Assert.NotEmpty(listed);
    }

    [Theory]
    [InlineData("provenance.json")]
    [InlineData("cycle-plane-pairs/provenance.json")]
    public void EveryExecutableDigestOfAProvenanceFileIsTheManifests(string provenanceFile)
    {
        ArgumentNullException.ThrowIfNull(provenanceFile);
        var recorded = OriginalManifest.Sha256Of("PropStructV3.exe");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixturesRoot, provenanceFile.Replace('/', Path.DirectorySeparatorChar))));
        var digests = document.RootElement.EnumerateArray()
            .Select(entry => entry.TryGetProperty("executableSha256", out var digest) ? digest.GetString() : null)
            .Where(digest => digest is not null)
            .ToList();
        Assert.NotEmpty(digests);
        var stale = digests.Count(digest => !string.Equals(digest, recorded, StringComparison.OrdinalIgnoreCase));
        Assert.True(stale == 0, $"{stale} of {digests.Count} executable digests of {provenanceFile} are not the manifest's {recorded}.");
    }

    /// <summary>The name an archive member has for a file under <c>Legacy/</c>: a formulation keeps its name, an output loses
    /// the <c>.txt</c> appended to it (<c>tests/Fixtures/BOOT.md</c>, "## Invariants"); anything else is no member.</summary>
    private static string? MemberNameOf(string relative)
    {
        if (relative.StartsWith("formulations/", StringComparison.Ordinal) && relative.EndsWith(".dat", StringComparison.Ordinal))
        {
            return relative["formulations/".Length..];
        }

        if (relative.StartsWith("outputs/", StringComparison.Ordinal) && relative.EndsWith(".m.txt", StringComparison.Ordinal))
        {
            return relative["outputs/".Length..^".txt".Length];
        }

        return null;
    }

    private static Dictionary<string, string> ListedMembers() =>
        File.ReadAllLines(Path.Combine(FixturesRoot, "archive-members.sha256"))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split("  ", 2))
            .ToDictionary(fields => fields[1], fields => fields[0], StringComparer.Ordinal);

    private static string Sha256Of(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
