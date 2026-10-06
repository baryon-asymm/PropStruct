namespace PropStruct.Tests.Harness;

/// <summary>
/// The replica and reference files of one formulation, parsed (this node's BOOT.md, ## Invariants: "the replicas
/// and the reference come from <c>tests/Fixtures</c> on every call, computed at test time"). Split out of
/// <c>StatisticalCriterion.cs</c>/<c>StatisticalCriterion.FixtureAccess.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class FixtureReplicas
{
    private static readonly Dictionary<string, int> ReplicaCounts = new(StringComparer.Ordinal)
    {
        ["HPEPA3"] = 32,
        ["inpt"] = 16,
        ["P33"] = 16,
        ["PSAN02n"] = 16,
        ["HMX"] = 16,
    };

    /// <summary>
    /// <paramref name="formulation"/>'s own replica count <c>R</c>, or an <see cref="ArgumentException"/> naming
    /// it as not one of the reference formulations. Replaces four copies of the same check that used to live one
    /// in each of <c>Compare</c>/<c>CompareTailRowMean</c>/<c>CompareAdaptiveIndexMatched</c>'s own builders and
    /// one in <c>TwoSampleBias</c> (decomposition, 2026-09-26; root BOOT.md Taboos: no second implementation of a
    /// formula) — the message and the <see cref="ArgumentException.ParamName"/> (<c>nameof(formulation)</c> at
    /// each of those four call sites) are unchanged, so no caller's own exception text moves.
    /// </summary>
    internal static int CountOf(string formulation)
    {
        if (!ReplicaCounts.TryGetValue(formulation, out var replicaCount))
        {
            throw new ArgumentException(
                $"'{formulation}' is not one of the reference formulations (root BOOT.md, Constraints).", nameof(formulation));
        }

        return replicaCount;
    }

    // `excludeOrdinal` (`tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 1, "Blind calibration") is the
    // 1-based replica ordinal to leave out of the R loaded, so a test can put that replica itself in the
    // candidate role and compare it against the other R - 1 without it appearing on both sides at once; `null`
    // loads all R, unchanged from before this parameter existed.
    internal static List<IReadOnlyDictionary<string, double[]>> LoadReplicas(
        string formulation, ReplicaKind kind, int replicaCount, int? excludeOrdinal = null)
    {
        var replicas = new List<IReadOnlyDictionary<string, double[]>>(replicaCount);
        for (var k = 1; k <= replicaCount; k++)
        {
            if (k == excludeOrdinal)
            {
                continue;
            }

            replicas.Add(ResultsMFile.Parse(ReplicaPath(formulation, kind, k)));
        }

        return replicas;
    }

    // `tests/Harness/HISTORY.md#per-run-quantum`: `Compare`'s own replica loader, parsing every
    // replica through `ParseCells` instead of `Parse` so each run's own print resolution travels with its
    // values — needed to validate that run's own quantum "within the print resolution of the field". The other
    // three public members (`CompareTailRowMean`, `CompareAdaptiveIndexMatched`, `TwoSampleBias`) never treat a
    // cell as count-like and keep using `LoadReplicas` above.
    internal static List<IReadOnlyDictionary<string, ResultCell[]>> LoadReplicaCells(
        string formulation, ReplicaKind kind, int replicaCount, int? excludeOrdinal = null)
    {
        var replicas = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
        for (var k = 1; k <= replicaCount; k++)
        {
            if (k == excludeOrdinal)
            {
                continue;
            }

            replicas.Add(ResultsMFile.ParseCells(ReplicaPath(formulation, kind, k)));
        }

        return replicas;
    }

    internal static string ReferencePath(string formulation) =>
        RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");

    internal static string ReplicaPath(string formulation, ReplicaKind kind, int replicaOrdinal) =>
        RepositoryPaths.Resolve("tests", "Fixtures", ReplicaDirectory(kind), formulation, replicaOrdinal + ".m.txt");

    internal static string ReplicaDirectory(ReplicaKind kind) => kind switch
    {
        ReplicaKind.Lagged => "replicas-lagged",
        ReplicaKind.Independent => "replicas-independent",
        ReplicaKind.Gsv3 => "replicas-gsv3",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "unrecognized replica kind."),
    };
}
