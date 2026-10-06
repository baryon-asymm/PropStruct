using System.Text.Json;
using System.Text.Json.Serialization;

namespace PropStruct.Tests.Harness;

/// <summary>
/// <c>tests/Fixtures/exclusions.json</c>'s own rule vocabulary and the quantity sets each rule yields for one
/// formulation. Split out of <c>StatisticalCriterion.cs</c>/<c>StatisticalCriterion.FixtureAccess.cs</c>
/// (decomposition, 2026-09-26).
/// </summary>
internal static class ExclusionRules
{
    // The rule vocabulary tests/Fixtures/exclusions.json carries today. Rules are data (Fixtures' BOOT.md,
    // "Exclusions carry evidence"), so this vocabulary is small and any unrecognized rule text fails loudly
    // rather than being ignored silently — this node's BOOT.md, "Exclusion rule vocabulary".
    internal const string ZeroBelowRule = "reference cells below 1e-30 compare as zero";
    internal const double ZeroBelowThreshold = 1e-30;

    // this node's BOOT.md, "Exclusion rule vocabulary": a whole-quantity drop, scoped to the Independent replica
    // kind only (a no-op against Lagged/Gsv3). Added for tests/Fixtures/exclusions.json's `epsx(4)` entry —
    // src/Random/BOOT.md, "Seed and replica jumps are rigid shifts": the Independent replica set's own stream-4
    // shifts span only a tenth of the circle, so the replica sd this criterion would otherwise compare against
    // is the spread of a short arc, not of independent samples (tests/Random.Tests/ReplicaShiftTests.cs has the
    // computed evidence).
    internal const string NotComparedAgainstIndependentReplicasRule = "not compared against independent replicas";

    private static readonly HashSet<string> RecognizedRules = new(StringComparer.Ordinal)
    {
        ZeroBelowRule,
        NotComparedAgainstIndependentReplicasRule,
    };

    internal static HashSet<string> ZeroBelowQuantitiesOf(string formulation)
    {
        var zeroBelow = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in ExclusionsOf(formulation))
        {
            if (entry.Rule == ZeroBelowRule)
            {
                _ = zeroBelow.Add(entry.Quantity);
            }
        }

        return zeroBelow;
    }

    // this node's BOOT.md, "Exclusion rule vocabulary": the quantities a candidate never compares against
    // Independent replicas at all — every cell of the named quantity is dropped and counted in
    // CriterionReport.Excluded (Compare's own per-name check, below). A no-op for every other ReplicaKind: the
    // caller only consults this set when kind == ReplicaKind.Independent.
    internal static HashSet<string> ExcludedFromIndependentQuantitiesOf(string formulation)
    {
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in ExclusionsOf(formulation))
        {
            if (entry.Rule == NotComparedAgainstIndependentReplicasRule)
            {
                _ = excluded.Add(entry.Quantity);
            }
        }

        return excluded;
    }

    // Shared by every rule-specific extraction method above: filters exclusions.json to this formulation's own
    // entries (or "*") and validates every one of them against the recognized vocabulary before any rule-
    // specific filtering happens, so an unrecognized rule text throws regardless of which quantity set a caller
    // is building (this node's BOOT.md, "Exclusion rule vocabulary": "unrecognized rule text throws rather than
    // being silently ignored").
    private static IEnumerable<ExclusionEntry> ExclusionsOf(string formulation)
    {
        foreach (var entry in LoadExclusions())
        {
            if (entry.Formulation != "*" && !string.Equals(entry.Formulation, formulation, StringComparison.Ordinal))
            {
                continue;
            }

            if (!RecognizedRules.Contains(entry.Rule))
            {
                throw new InvalidOperationException(
                    $"exclusions.json names an unrecognized rule '{entry.Rule}' for '{entry.Quantity}'. " +
                    "StatisticalCriterion's rule vocabulary (this node's BOOT.md) must be extended before this " +
                    "entry can be applied; it is never ignored silently.");
            }

            yield return entry;
        }
    }

    private static List<ExclusionEntry> LoadExclusions()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "exclusions.json");
        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<List<RawExclusionEntry>>(json) ?? [];
        return raw.ConvertAll(entry => new ExclusionEntry(entry.Formulation, entry.Quantity, entry.Rule, entry.Reason, entry.Evidence));
    }

    private sealed record ExclusionEntry(string Formulation, string Quantity, string Rule, string Reason, string Evidence);

    private sealed record RawExclusionEntry(
        [property: JsonPropertyName("formulation")] string Formulation,
        [property: JsonPropertyName("quantity")] string Quantity,
        [property: JsonPropertyName("rule")] string Rule,
        [property: JsonPropertyName("reason")] string Reason,
        [property: JsonPropertyName("evidence")] string Evidence);
}
