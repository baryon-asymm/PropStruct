namespace PropStruct.Tests.Harness;

/// <summary>
/// The result of one <see cref="StatisticalCriterion.CompareSets"/> call (this node's BOOT.md, "## Set
/// comparison"). <paramref name="Compared"/> and <paramref name="Excluded"/> are the call's own <c>m</c> and the
/// count of index-cells dropped before comparison — the "not compared against independent replicas" rule's own
/// whole-quantity drop, and a position (ordinary or canonical-axis) that never reached two contributors on
/// either side. <paramref name="Differences"/> is every cell found to differ between the candidates and the
/// formulation's own replicas of the call's <c>kind</c> — the list <c>docs/declared-differences.json</c>'s own
/// entries are checked against. <paramref name="CandidateHalvesDifferences"/> and
/// <paramref name="ReplicaHalvesDifferences"/> are the same comparison's own negative controls: the candidates
/// split by position (seed) parity and the replicas by ordinal parity, each half compared against the other.
/// Both sets are, by construction, one homogeneous pool, so every entry either list finds is a false positive of
/// the method itself, not a real difference — an empty list is the expected reading, not merely the observed one.
/// </summary>
public sealed record SetCriterionReport(
    int Compared,
    int Excluded,
    IReadOnlyList<TwoSampleCell> Differences,
    IReadOnlyList<TwoSampleCell> CandidateHalvesDifferences,
    IReadOnlyList<TwoSampleCell> ReplicaHalvesDifferences);
