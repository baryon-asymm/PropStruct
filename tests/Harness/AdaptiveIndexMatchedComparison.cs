namespace PropStruct.Tests.Harness;

/// <summary>
/// `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 3: the three whole-array category axes
/// (<c>Dkarmcat</c>/<c>dokkarm43</c>/<c>dokkarm10</c>) compared by adaptive position instead of by canonical
/// (value-matched) row, plus the two adaptive-tail scalars, for
/// <see cref="StatisticalCriterion.CompareAdaptiveIndexMatched"/>. Split out of
/// <c>StatisticalCriterion.Comparisons.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class AdaptiveIndexMatchedComparison
{
    private static readonly string[] AdaptiveAxisArrays = ["Dkarmcat", "dokkarm43", "dokkarm10"];

    internal static (List<PendingCell> Pending, int Excluded) BuildAdaptiveIndexMatchedPending(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var replicaCount = FixtureReplicas.CountOf(formulation);

        var referenceCells = ResultsMFile.ParseCells(FixtureReplicas.ReferencePath(formulation));
        var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
            ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
            : [];
        var i0 = CategoryAxis.FixedWidthPrefixLength(referenceDkarmcat);

        var replicas = FixtureReplicas.LoadReplicas(formulation, kind, replicaCount, excludeReplicaOrdinal);
        var pending = new List<PendingCell>();
        var excluded = 0;

        foreach (var name in AdaptiveAxisArrays)
        {
            var candidateArray = candidate.GetValueOrDefault(name);
            if (candidateArray is null)
            {
                continue;
            }

            var candidateAdaptiveLength = Math.Max(0, candidateArray.Length - i0);

            var contributingArrays = replicas
                .Select(r => r.GetValueOrDefault(name))
                .Where(a => a is not null)
                .Select(a => a!)
                .ToArray();
            var contributingLengths = contributingArrays
                .Select(a => Math.Max(0, a.Length - i0))
                .ToArray();
            if (contributingLengths.Length < 2)
            {
                continue;
            }

            // `tests/Harness/HISTORY.md#e2-adaptive-common-range`: the COMMON range of every source's own
            // adaptive length, not the union — a row past a source's own reach is a boundary or a mean size that
            // source never printed, not a zero (## Invariants' own general rule is for a length-varying array's
            // OWN trailing rows, not for filling in a value a source never had). Comparing a real, finite quantum
            // against an impossible 0.0 miscounts a short source's own missing length twice, once here and once
            // through `AdaptiveRowCount`, whose job this already is. Rows past the common range narrow the
            // comparison honestly: they count toward `Excluded`, not toward a failure.
            var unionLength = Math.Max(candidateAdaptiveLength, contributingLengths.Max());
            var commonLength = Math.Min(candidateAdaptiveLength, contributingLengths.Min());
            excluded += unionLength - commonLength;

            for (var j = 0; j < commonLength; j++)
            {
                var row = i0 + j;
                var cand = candidateArray[row];
                var replicaValues = contributingArrays.Select(a => a[row]).ToArray();

                // The sparse-cell exclusion of `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #1, wired into this loop for the
                // same reason it is already wired into Compare's own generic branch and CompareTailRowMean: a
                // row every contributing source reaches can still carry fewer than two nonzero values (a real,
                // printed zero is not "absent"), and that row carries no scale, under exchangeability, to judge
                // any candidate against.
                if (replicaValues.Count(v => v != 0.0) < 2)
                {
                    excluded++;
                    continue;
                }

                pending.Add(PendingCell.From($"{name}@adaptive", j, default, cand, replicaValues, zeroBelow: false, isDistributionFunction: false));
            }
        }

        // The two scalars (`tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 3), read off Dkarmcat alone:
        // dokkarm43/dokkarm10 share its row axis, so a second row count/last boundary from them would repeat the
        // same two numbers, not add information.
        var candidateDkarmcat = candidate.GetValueOrDefault("Dkarmcat");
        if (AdaptiveRowCountOf(candidateDkarmcat, i0) is { } candidateAdaptiveRows)
        {
            var replicaAdaptiveRows = replicas
                .Select(r => AdaptiveRowCountOf(r.GetValueOrDefault("Dkarmcat"), i0))
                .Where(v => v is not null)
                .Select(v => v!.Value)
                .ToArray();
            if (replicaAdaptiveRows.Length >= 2)
            {
                // `tests/Harness/HISTORY.md#fable-5-1-decision-iii`, "The adaptive row count cell": a row count is
                // already an integer count, not a continuous quantity, so it gets the same negative-binomial
                // predictive interval every other count-like cell gets (isDistributionFunction: true) instead of
                // a Student band with an integer-printed Poisson floor sized for a continuous scalar. The quantum
                // is exactly 1 (one row is one count) and is never inferred — replicaCounts is the row counts
                // themselves, with no division needed.
                pending.Add(PendingCell.From(
                    "AdaptiveRowCount", 0, new ResultCell(candidateAdaptiveRows, 1.0, true),
                    candidateAdaptiveRows, replicaAdaptiveRows, zeroBelow: false, isDistributionFunction: true,
                    candidateQuantum: 1.0, candidateQuantumResolution: 0.0, replicaCounts: replicaAdaptiveRows));
            }
        }

        if (LastBoundaryLogOf(candidateDkarmcat) is { } candidateLastBoundaryLog)
        {
            var replicaLastBoundaryLogs = replicas
                .Select(r => LastBoundaryLogOf(r.GetValueOrDefault("Dkarmcat")))
                .Where(v => v is not null)
                .Select(v => v!.Value)
                .ToArray();
            if (replicaLastBoundaryLogs.Length >= 2)
            {
                pending.Add(PendingCell.From(
                    "AdaptiveLastBoundaryLog", 0, default, candidateLastBoundaryLog,
                    replicaLastBoundaryLogs, zeroBelow: false, isDistributionFunction: false));
            }
        }

        return (pending, excluded);
    }

    // this node's BOOT.md, "## Set comparison": one source's own adaptive row count — the Dkarmcat length past
    // the fixed-width prefix i0 — extracted so CanonicalAxisSetCells shares this exact implementation instead of
    // a second one (root BOOT.md Taboos). Null when the source never printed Dkarmcat at all.
    internal static double? AdaptiveRowCountOf(double[]? dkarmcat, int i0) =>
        dkarmcat is { Length: > 0 } ? Math.Max(0, dkarmcat.Length - i0) : null;

    // this node's BOOT.md, "## Set comparison": the natural log of one source's own last Dkarmcat boundary,
    // shared the same way. Null when the source never printed Dkarmcat, or printed a non-positive last entry
    // (the log is undefined there).
    internal static double? LastBoundaryLogOf(double[]? dkarmcat) =>
        dkarmcat is { Length: > 0 } && dkarmcat[^1] > 0.0 ? Math.Log(dkarmcat[^1]) : null;
}
