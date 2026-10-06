namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// The leave-one-out and reference-vs-replicas machinery <see cref="TailCoverageTests.Gate2BlindCalibrationLaggedLeaveOneOutNullRateNumeratorMatchesTheRecordedSet"/>
/// already used for the <see cref="ReplicaKind.Lagged"/> set, extracted so the root criterion's own rate
/// comparison (`tests/Harness/BOOT.md`, "## Null rate of the original") can reuse it for both seed-patched replica
/// kinds instead of a second copy of the loop (root BOOT.md Taboos: "no second implementation of any part of ...
/// a formula"). A "run" is one candidate result (a left-out replica, or the archived reference) compared against
/// the rest of its own replica set; it <see cref="RunResult.Failed"/> when any of the three reports this node
/// exposes (<see cref="StatisticalCriterion.Compare"/>, <see cref="StatisticalCriterion.CompareTailRowMean"/>,
/// <see cref="StatisticalCriterion.CompareAdaptiveIndexMatched"/>) names a failing cell for it — Gate2's own rule,
/// unchanged.
/// </summary>
internal static class NullRateCalibration
{
    internal readonly record struct RunResult(string Formulation, string Label, IReadOnlyList<CriterionFailure> Failures)
    {
        public bool Failed => Failures.Count > 0;
    }

    private static string ReplicaDirectory(ReplicaKind kind) => kind switch
    {
        ReplicaKind.Lagged => "replicas-lagged",
        ReplicaKind.Independent => "replicas-independent",
        ReplicaKind.Gsv3 => throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "leave-one-out and reference-vs-R are defined for the two seed-patched replica kinds only."),
        _ => throw new ArgumentOutOfRangeException(
                    nameof(kind), kind, "leave-one-out and reference-vs-R are defined for the two seed-patched replica kinds only."),
    };

    /// <summary>Every replica of every formulation, left out in turn and compared against the other <c>R - 1</c>
    /// of <paramref name="kind"/> (Gate2's own 96-run set, for <see cref="ReplicaKind.Lagged"/>).</summary>
    public static IReadOnlyList<RunResult> LeaveOneOutRuns(ReplicaKind kind)
    {
        var directory = ReplicaDirectory(kind);
        var results = new List<RunResult>();
        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            for (var k = 1; k <= replicaCount; k++)
            {
                var path = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, k + ".m.txt");
                var candidate = ResultsMFile.Parse(path);
                var failures = new List<CriterionFailure>();
                failures.AddRange(StatisticalCriterion.Compare(formulation, candidate, kind, excludeReplicaOrdinal: k).Failures);
                failures.AddRange(StatisticalCriterion.CompareTailRowMean(formulation, candidate, kind, excludeReplicaOrdinal: k).Failures);
                failures.AddRange(StatisticalCriterion.CompareAdaptiveIndexMatched(formulation, candidate, kind, excludeReplicaOrdinal: k).Failures);
                results.Add(new RunResult(formulation, $"replica {k}", failures));
            }
        }

        return results;
    }

    /// <summary>The archived reference of every formulation against all <c>R</c> replicas of <paramref name="kind"/>
    /// — five runs. Only <see cref="ReplicaKind.Lagged"/> is a genuine null comparison: the reference is, by
    /// construction, the <c>Original</c> layout's own un-jumped state (<c>tests/Fixtures/BOOT.md</c>, "Seed
    /// offsets are proven, not assumed" — patching a block with its own original limbs reproduces the shipped
    /// executable exactly), i.e. replica "k = 0" of the lagged set; it carries no such relationship to the
    /// <c>Independent</c> layout, whose own zero-jump state is a different, disjoint orbit
    /// (root BOOT.md, "Two stream layouts"). Comparing the archived reference against <c>Independent</c> replicas
    /// would measure the known cross-layout bias (root BOOT.md, "Known bias of the original's seeds"), not the
    /// criterion's own false-failure rate, so this node's own null rate (below) does not call this method with
    /// <see cref="ReplicaKind.Independent"/> — `tests/Harness/BOOT.md`, "## Null rate of the original" records the
    /// narrowing and why.</summary>
    public static IReadOnlyList<RunResult> ReferenceRuns(ReplicaKind kind)
    {
        var results = new List<RunResult>();
        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var reference = ResultsMFile.Parse(referencePath);
            var failures = new List<CriterionFailure>();
            failures.AddRange(StatisticalCriterion.Compare(formulation, reference, kind).Failures);
            failures.AddRange(StatisticalCriterion.CompareTailRowMean(formulation, reference, kind).Failures);
            failures.AddRange(StatisticalCriterion.CompareAdaptiveIndexMatched(formulation, reference, kind).Failures);
            results.Add(new RunResult(formulation, "reference", failures));
        }

        return results;
    }

    /// <summary>The original's own null rate (`tests/Harness/BOOT.md`, "## Null rate of the original"): 96 lagged
    /// leave-one-out runs, 96 independent leave-one-out runs and 5 lagged reference-vs-R runs, 197 total.</summary>
    public static IReadOnlyList<RunResult> AllOriginalRuns() =>
        [.. LeaveOneOutRuns(ReplicaKind.Lagged), .. LeaveOneOutRuns(ReplicaKind.Independent), .. ReferenceRuns(ReplicaKind.Lagged)];
}
