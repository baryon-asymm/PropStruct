using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Fixtures.Tests;

/// <summary>
/// L0 of this node's BOOT.md: "every generated output has a provenance entry, and its SHA-256 matches" and
/// "every provenance input hash matches the shipped .dat, or the shipped .dat with only the GSV value changed
/// to 3". Never runs the executable (this node's BOOT.md, ## Invariants): every hash is recomputed from the
/// files already on disk, except the executable's, which is the manifest's.
/// </summary>
public class ProvenanceTests
{
    private static string FixturesRoot => RepositoryPaths.Resolve("tests", "Fixtures");

    // Keyed by Output (checked unique in EveryReferenceAndReplicaFileHasAProvenanceEntry's own sibling
    // scan of the same file): a [Theory] takes that key, not the record itself (CA1515, found 2026-09-24
    // under AnalysisMode=All - ProvenanceEntry no longer needs to be public once no [Theory] parameter
    // names it directly).
    public static IEnumerable<object[]> Entries() =>
        LoadEntries().Select(entry => new object[] { entry.Output });

    private static ProvenanceEntry FindEntry(string output) =>
        LoadEntries().Single(entry => entry.Output == output);

    [Theory]
    [MemberData(nameof(Entries))]
    public void OutputAndExecutableHashesMatchTheFilesOnDisk(string output)
    {
        var entry = FindEntry(output);

        var outputPath = Path.Combine(FixturesRoot, entry.Output.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(outputPath), $"provenance.json names '{entry.Output}', which does not exist at {outputPath}.");
        Assert.Equal(entry.OutputSha256, Sha256Of(File.ReadAllBytes(outputPath)), ignoreCase: true);

        // The executable lies outside the repository (tools/legacy): its recorded digest is compared with the
        // manifest's, and the file itself with the manifest by the gate of the Legacy category.
        Assert.Equal(entry.ExecutableSha256, OriginalManifest.Sha256Of("PropStructV3.exe"), ignoreCase: true);
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void InputHashMatchesTheShippedDatOrItsGsv3Variant(string output)
    {
        var entry = FindEntry(output);

        // The formulation's file name is the stdin's first line (Fixtures' BOOT.md, Constraints: standard input
        // "<name>\ny\n"), not the output path's formulation segment, so this does not depend on how the output
        // directory happens to be capitalized.
        var newline = entry.Stdin.IndexOf('\n');
        var formulationFile = entry.Stdin[..(newline < 0 ? entry.Stdin.Length : newline)];
        var datPath = Path.Combine(FixturesRoot, "Legacy", "formulations", formulationFile + ".dat");
        Assert.True(File.Exists(datPath), $"expected '{datPath}' for stdin '{entry.Stdin}'.");

        var shipped = File.ReadAllBytes(datPath);
        var expectedBytes = entry.Gsv switch
        {
            2 => shipped,
            3 => Gsv3VariantOf(shipped),
            _ => throw new InvalidOperationException($"provenance.json entry '{entry.Output}' names an unexpected GSV {entry.Gsv}."),
        };

        Assert.Equal(entry.InputSha256, Sha256Of(expectedBytes), ignoreCase: true);
    }

    // Fixtures/API.md, ## Layout: the reference outputs plus the three replica kinds, each its own directory.
    private static readonly string[] GeneratedOutputDirectoryNames =
        ["references", "replicas-lagged", "replicas-independent", "replicas-gsv3"];

    [Fact]
    public void EveryReferenceAndReplicaFileHasAProvenanceEntry()
    {
        var entries = LoadEntries();
        var recorded = new HashSet<string>(entries.Select(e => e.Output), StringComparer.Ordinal);

        var generatedFiles = GeneratedOutputDirectoryNames
            .Select(name => Path.Combine(FixturesRoot, name))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.m.txt", SearchOption.AllDirectories))
            .ToList();

        // The quantifier is the whole claim (AGENTS.md §6: a criterion with "all" is checked against a list the
        // machine generates). Without this the loop below runs zero times on a missing or empty fixture tree and
        // the test is green having compared nothing — the shape a sibling file documents deliberately
        // (`FixturesInventoryTests`, its own degenerate-case note and companion). Found 2026-09-20 by a review of
        // this branch; the number is the four directories' own content, not a figure to keep in step by hand.
        Assert.NotEmpty(generatedFiles);

        foreach (var path in generatedFiles)
        {
            var relative = Path.GetRelativePath(FixturesRoot, path).Replace(Path.DirectorySeparatorChar, '/');
            Assert.Contains(relative, recorded);
        }
    }

    // A byte-preserving Latin1 round trip (a 1:1 byte<->char mapping, so every byte of the shipped .dat survives
    // untouched) that replaces the GSV record's trailing value with "3" (Fixtures' BOOT.md, Constraints: "the
    // sixth value of the NMM JZZ KXX N NNZ GSV record is replaced by 3 and nothing else is changed").
    private static byte[] Gsv3VariantOf(byte[] shipped)
    {
        var text = Encoding.Latin1.GetString(shipped);
        var headerIndex = text.IndexOf("GSV", StringComparison.Ordinal);
        if (headerIndex < 0)
        {
            throw new InvalidOperationException("the .dat file has no 'GSV' header token.");
        }

        var headerLineEnd = text.IndexOf('\n', headerIndex);
        var valuesLineEnd = text.IndexOf('\n', headerLineEnd + 1);
        var valuesLine = text[(headerLineEnd + 1)..valuesLineEnd];

        var trimmedEnd = valuesLine.TrimEnd();
        var lastTokenStart = trimmedEnd.LastIndexOfAny([' ', '\t']) + 1;
        var gsvToken = trimmedEnd[lastTokenStart..];
        if (gsvToken.Length != 1)
        {
            throw new InvalidOperationException($"expected a single-digit GSV token, found '{gsvToken}'.");
        }

        var newValuesLine = valuesLine[..lastTokenStart] + "3" + valuesLine[(lastTokenStart + gsvToken.Length)..];
        var newText = text[..(headerLineEnd + 1)] + newValuesLine + text[valuesLineEnd..];
        return Encoding.Latin1.GetBytes(newText);
    }

    private static string Sha256Of(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static List<ProvenanceEntry> LoadEntries()
    {
        var path = Path.Combine(FixturesRoot, "provenance.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, ProvenanceEntryJsonContext.Default.ListProvenanceEntry) ?? [];
    }
}

/// <summary>
/// Source-generated (de)serialization for <see cref="ProvenanceEntry"/>: a plain
/// <c>JsonSerializer.Deserialize&lt;T&gt;</c> call gives CA1812 no evidence that this internal record is ever
/// constructed (reflection-based deserialization is invisible to it), following the same pattern as
/// <c>tests/Input.Tests/CaseFile.cs</c>'s <c>CaseFileJsonContext</c>.
/// </summary>
[JsonSerializable(typeof(List<ProvenanceEntry>))]
internal sealed partial class ProvenanceEntryJsonContext : JsonSerializerContext;

internal sealed record ProvenanceEntry(
    [property: JsonPropertyName("output")] string Output,
    [property: JsonPropertyName("outputSha256")] string OutputSha256,
    [property: JsonPropertyName("inputSha256")] string InputSha256,
    [property: JsonPropertyName("executableSha256")] string ExecutableSha256,
    [property: JsonPropertyName("gsv")] int Gsv,
    [property: JsonPropertyName("stdin")] string Stdin);
