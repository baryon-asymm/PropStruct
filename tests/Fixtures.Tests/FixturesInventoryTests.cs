using System.Text.Json;
using System.Text.Json.Serialization;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Fixtures.Tests;

/// <summary>
/// The remaining L0 rows of this node's BOOT.md that are not provenance hashing: replica distinctness, the
/// reference formulation/replica-count table, exclusion evidence, and the case-JSON citation rule.
/// </summary>
public partial class FixturesInventoryTests
{
    // root BOOT.md, Constraints: the five reference formulations and their replica counts. The same counts
    // apply to every replica kind (Fixtures/BOOT.md, ## Invariants, "Replicas are distinct").
    public static readonly (string Name, int ReplicaCount)[] Formulations =
    [
        ("HPEPA3", 32),
        ("inpt", 16),
        ("P33", 16),
        ("PSAN02n", 16),
        ("HMX", 16),
    ];

    // Fixtures/API.md, ## Layout: the three replica kinds, each in its own directory.
    public static readonly string[] ReplicaDirectoryNames = ["replicas-lagged", "replicas-independent", "replicas-gsv3"];

    public static IEnumerable<object[]> FormulationNames() => Formulations.Select(f => new object[] { f.Name });

    public static IEnumerable<object[]> FormulationsByReplicaKind() =>
        from formulation in Formulations
        from directoryName in ReplicaDirectoryNames
        select new object[] { formulation.Name, directoryName };

    [Theory]
    [MemberData(nameof(FormulationsByReplicaKind))]
    public void EveryReferenceFormulationHasItsReferenceAndRReplicas(string formulation, string replicaDirectoryName)
    {
        var expectedReplicaCount = Formulations.Single(f => f.Name == formulation).ReplicaCount;

        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        Assert.True(File.Exists(referencePath), $"missing reference: {referencePath}");

        var replicaDirectory = RepositoryPaths.Resolve("tests", "Fixtures", replicaDirectoryName, formulation);
        var replicaFiles = Directory.Exists(replicaDirectory)
            ? Directory.EnumerateFiles(replicaDirectory, "*.m.txt").ToArray()
            : [];
        Assert.Equal(expectedReplicaCount, replicaFiles.Length);

        for (var k = 1; k <= expectedReplicaCount; k++)
        {
            var replicaPath = RepositoryPaths.Resolve("tests", "Fixtures", replicaDirectoryName, formulation, k + ".m.txt");
            Assert.True(File.Exists(replicaPath), $"missing replica: {replicaPath}");
        }
    }

    [Theory]
    [MemberData(nameof(FormulationsByReplicaKind))]
    public void ReplicasOfOneFormulationAndKindArePairwiseDistinctOutsideTheTimeLine(string formulation, string replicaDirectoryName)
    {
        var replicaCount = Formulations.Single(f => f.Name == formulation).ReplicaCount;

        // ResultsMFile.Parse never turns the time line into a quantity (tests/Harness/BOOT.md, Name scheme), so
        // two parses that are otherwise deeply equal are truly identical replicas, not merely different by
        // their run time.
        var parses = new IReadOnlyDictionary<string, double[]>[replicaCount];
        for (var k = 1; k <= replicaCount; k++)
        {
            parses[k - 1] = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", replicaDirectoryName, formulation, k + ".m.txt"));
        }

        for (var i = 0; i < parses.Length; i++)
        {
            for (var j = i + 1; j < parses.Length; j++)
            {
                Assert.False(
                    AreDeeplyEqual(parses[i], parses[j]),
                    $"{formulation} {replicaDirectoryName} replicas {i + 1} and {j + 1} are identical outside the time line.");
            }
        }
    }

    private static bool AreDeeplyEqual(IReadOnlyDictionary<string, double[]> a, IReadOnlyDictionary<string, double[]> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (name, values) in a)
        {
            if (!b.TryGetValue(name, out var otherValues) || !values.AsSpan().SequenceEqual(otherValues))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record ExclusionEntry(
        [property: JsonPropertyName("formulation")] string Formulation,
        [property: JsonPropertyName("quantity")] string Quantity,
        [property: JsonPropertyName("rule")] string Rule,
        [property: JsonPropertyName("reason")] string Reason,
        [property: JsonPropertyName("evidence")] string Evidence);

    // Source-generated: a plain JsonSerializer.Deserialize<List<ExclusionEntry>> call gives CA1812 no evidence
    // that `ExclusionEntry` is ever constructed (reflection-based deserialization is invisible to it), and it may
    // not be removed — it is exclusions.json's own documented shape.
    [JsonSerializable(typeof(List<ExclusionEntry>))]
    private sealed partial class ExclusionEntryJsonContext : JsonSerializerContext;

    [Fact]
    public void EveryExclusionHasANonEmptyReasonAndEvidenceNamingAnExistingFile()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "exclusions.json");
        var entries = JsonSerializer.Deserialize(File.ReadAllText(path), ExclusionEntryJsonContext.Default.ListExclusionEntry) ?? [];
        Assert.NotEmpty(entries);

        foreach (var entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Reason), $"'{entry.Quantity}' has an empty reason.");
            Assert.False(string.IsNullOrWhiteSpace(entry.Evidence), $"'{entry.Quantity}' has empty evidence.");

            // "evidence" reads "<relative path>: <what it shows>" (exclusions.json's own pdoksmall entry).
            var colon = entry.Evidence.IndexOf(':');
            var evidencePath = colon < 0 ? entry.Evidence : entry.Evidence[..colon];
            var resolved = RepositoryPaths.Resolve("tests", "Fixtures", evidencePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(resolved), $"'{entry.Quantity}' evidence names '{evidencePath}', which does not exist at {resolved}.");
        }
    }

    // The rule applies only to formula-script cases (Fixtures' BOOT.md acceptance criterion: "Every formula
    // script case consumed by Particle.Tests and Statistics.Tests exists and names its Fortran lines"), i.e.
    // cases/particle and cases/statistics — never cases/input (constructed .dat files for Input.Tests, added by
    // a sibling coder) or cases/harness (a scipy reference table for a general Student quantile, which names its
    // own generating command and library version instead, tests/Harness/BOOT.md). Both directories do not exist
    // yet: this node's BOOT.md, dated note.
    private static readonly string[] FormulaCaseDirectoryNames = ["particle", "statistics"];

    [Fact]
    public void EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript()
    {
        // Checks every case JSON present under cases/particle and cases/statistics, and passes when there are
        // none (this node's BOOT.md, dated note): the existence of formula cases is guarded where they are
        // consumed (Particle.Tests and Statistics.Tests fail when their own cases are missing, and the Fixtures
        // criterion "every formula script case consumed by Particle.Tests and Statistics.Tests exists" stays
        // unticked until then), not here. TheCitationRuleFailsOnAConstructedCaseMissingItsCitations proves this
        // method's rule itself is not degenerate, on a constructed case, without waiting for either directory.
        var casesRoot = RepositoryPaths.Resolve("tests", "Fixtures", "cases");
        var existingDirectories = FormulaCaseDirectoryNames
            .Select(name => Path.Combine(casesRoot, name))
            .Where(Directory.Exists)
            .ToArray();

        AssertEveryCaseJsonNamesItsFortranLinesAndScript(existingDirectories);
    }

    [Fact]
    public void TheCitationRuleFailsOnAConstructedCaseMissingItsCitations()
    {
        // Proves EveryFormulaCaseJsonNamesItsFortranLinesAndItsScript's rule non-degenerate on a constructed
        // case, not on the absence of cases/particle and cases/statistics (an empty scan passes trivially and
        // proves nothing, this node's BOOT.md, dated note). The case lives in a real temporary directory, never
        // under tests/Fixtures, so this check needs no fixture and leaves no trace in the tree either way.
        var directory = Directory.CreateTempSubdirectory("propstruct-fixtures-tests-");
        try
        {
            File.WriteAllText(
                Path.Combine(directory.FullName, "missing-fortran-lines.json"),
                                     /*lang=json,strict*/
                                     """{ "script": "formulas_particle.py" }""");

            var error = Record.Exception(() => AssertEveryCaseJsonNamesItsFortranLinesAndScript([directory.FullName]));
            Assert.NotNull(error);

            File.WriteAllText(
                Path.Combine(directory.FullName, "missing-fortran-lines.json"),
                                     /*lang=json,strict*/
                                     """{ "fortranLines": "123-145", "script": "formulas_particle.py" }""");
            var noError = Record.Exception(() => AssertEveryCaseJsonNamesItsFortranLinesAndScript([directory.FullName]));
            Assert.Null(noError);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static void AssertEveryCaseJsonNamesItsFortranLinesAndScript(IEnumerable<string> directories)
    {
        foreach (var directory in directories)
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var root = document.RootElement;
                Assert.True(root.TryGetProperty("fortranLines", out _), $"{file} does not name its Fortran lines.");
                Assert.True(root.TryGetProperty("script", out _), $"{file} does not name its generating script.");
            }
        }
    }
}
