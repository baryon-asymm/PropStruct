using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// F-b (`tests/Harness/BOOT.md`, "Canonical category axis";
/// `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`): the fixed-width prefix
/// <see cref="CategoryAxis.FixedWidthPrefixLength"/> returns is each run's own — the rows before its own first
/// category merge — not shared bit-for-bit by every run of a formulation. This is the machine-generated census
/// backing that correction (AGENTS.md §6, a criterion quantified by "all" is checked against a list the machine
/// generates, not one typed by hand): every lagged and independent leave-one-out replica, the same 96 + 96
/// <see cref="NullRateCalibration.LeaveOneOutRuns"/> reads, has its own <c>Dkarmcat</c> prefix length compared
/// against its formulation's own archived reference.
/// </summary>
public class FixedWidthPrefixCensusTests
{
    private readonly ITestOutputHelper _output;

    public FixedWidthPrefixCensusTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static int ReferencePrefixLength(string formulation)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        return CategoryAxis.FixedWidthPrefixLength(ResultsMFile.Parse(path).GetValueOrDefault("Dkarmcat") ?? []);
    }

    private static IEnumerable<(string Formulation, string Label, bool Differs)> Census(ReplicaKind kind)
    {
        var directory = kind == ReplicaKind.Lagged ? "replicas-lagged" : "replicas-independent";
        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var referencePrefix = ReferencePrefixLength(formulation);
            for (var k = 1; k <= replicaCount; k++)
            {
                var path = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, k + ".m.txt");
                var replicaDkarmcat = ResultsMFile.Parse(path).GetValueOrDefault("Dkarmcat") ?? [];
                var replicaPrefix = CategoryAxis.FixedWidthPrefixLength(replicaDkarmcat);
                yield return (formulation, $"replica {k}", replicaPrefix != referencePrefix);
            }
        }
    }

    // The census `tests/Harness/BOOT.md`, "Canonical category axis" cites — the corrected reading of the
    // fixed-width prefix, replacing "shared bit-for-bit by every run of a formulation"
    // (`tests/Harness/HISTORY.md#fixed-width-prefix-not-shared` has the per-formulation breakdown, the 517-run
    // count over the null and rate-table runs together, and what a per-run cut would change — measured and not
    // adopted).
    [Fact]
    public void SeventeenOfNinetySixLaggedReplicasHaveTheirOwnPrefixLength()
    {
        var census = Census(ReplicaKind.Lagged).ToList();
        Assert.True(census.Count == 96, $"expected 96 lagged leave-one-out replicas, counted {census.Count}.");

        var differing = census.Where(c => c.Differs).ToList();
        foreach (var (formulation, label, _) in differing)
        {
            _output.WriteLine($"{formulation} {label}: own prefix differs from the reference's.");
        }

        Assert.Equal(17, differing.Count);
    }

    [Fact]
    public void SixtyOneOfNinetySixIndependentReplicasHaveTheirOwnPrefixLength()
    {
        var census = Census(ReplicaKind.Independent).ToList();
        Assert.True(census.Count == 96, $"expected 96 independent leave-one-out replicas, counted {census.Count}.");

        var differing = census.Where(c => c.Differs).ToList();
        foreach (var (formulation, label, _) in differing)
        {
            _output.WriteLine($"{formulation} {label}: own prefix differs from the reference's.");
        }

        Assert.Equal(61, differing.Count);
    }
}
