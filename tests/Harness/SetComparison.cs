namespace PropStruct.Tests.Harness;

/// <summary>
/// this node's BOOT.md, "## Set comparison": one <see cref="StatisticalCriterion.CompareSets"/> call — loads
/// the formulation's own <c>R</c> replicas of the requested <see cref="ReplicaKind"/> and the reference's own
/// <c>Dkarmcat</c>, puts every run's canonical-axis families on that reference axis and adds its adaptive-tail
/// summaries (<see cref="CanonicalAxisSetCells"/>), zero-fills every remaining ("ordinary") array to the union
/// length of both sets, then runs three <see cref="TwoSampleComparison.OfSets"/> comparisons at the exact
/// constant-cell bound (<see cref="ConstantCellRule.ExactBound"/>): candidates against replicas (the report's
/// own <see cref="SetCriterionReport.Differences"/>), and two negative controls that split one homogeneous set
/// in half and compare it against itself — candidates by position (seed) parity, replicas by ordinal parity.
/// </summary>
internal static class SetComparison
{
    private static readonly HashSet<string> NoFillNames = new(StringComparer.Ordinal)
    {
        "TailRowMean", "AdaptiveRowCount", "AdaptiveLastBoundaryLog",
    };

    internal static SetCriterionReport Compare(
        string formulation, IReadOnlyList<IReadOnlyDictionary<string, double[]>> candidates, ReplicaKind kind)
    {
        var (candidateSet, replicaSet, excludedQuantityCells) = PrepareSets(formulation, candidates, kind);

        var (cells, ofSetsExcluded) = TwoSampleComparison.OfSets(candidateSet, replicaSet, ConstantCellRule.ExactBound);

        // Candidates split by position parity (this node's BOOT.md, "## Set comparison": candidates arrive in
        // seed order, so position parity is seed parity); replicas split by ordinal parity (a replica's own
        // ordinal is its 1-based list position, index + 1, hence ordinalOffset: 1 below).
        var (candidateFirstHalf, candidateSecondHalf) = SplitByParity(candidateSet, ordinalOffset: 0);
        var (replicaFirstHalf, replicaSecondHalf) = SplitByParity(replicaSet, ordinalOffset: 1);

        var (candidateHalfCells, _) = TwoSampleComparison.OfSets(candidateFirstHalf, candidateSecondHalf, ConstantCellRule.ExactBound);
        var (replicaHalfCells, _) = TwoSampleComparison.OfSets(replicaFirstHalf, replicaSecondHalf, ConstantCellRule.ExactBound);

        return new SetCriterionReport(
            Compared: cells.Count,
            Excluded: excludedQuantityCells + ofSetsExcluded,
            Differences: [.. cells.Where(c => c.Differs)],
            CandidateHalvesDifferences: [.. candidateHalfCells.Where(c => c.Differs)],
            ReplicaHalvesDifferences: [.. replicaHalfCells.Where(c => c.Differs)]);
    }

    /// <summary>this node's BOOT.md, "## Set comparison": the same candidate-set preparation <see cref="Compare"/>
    /// uses (loading the replicas, the canonical-axis alignment, the replica-set exclusion, the zero-fill),
    /// exposed so a caller can run <see cref="TwoSampleComparison.OfSets"/> on an arbitrary split of the
    /// *already-prepared* candidate set — <c>tests/Harness/HISTORY.md#set-comparison-design-2026-09-27</c>'s own
    /// 6,000-random-split re-measurement uses this to avoid a second implementation of the alignment/zero-fill
    /// pipeline; <see cref="Compare"/>'s own fixed parity split is one particular caller of the same seam, kept
    /// inline above because it needs no arbitrary partition.</summary>
    internal static (List<IReadOnlyDictionary<string, double[]>> CandidateSet, List<IReadOnlyDictionary<string, double[]>> ReplicaSet, int ExcludedQuantityCells)
        PrepareSets(string formulation, IReadOnlyList<IReadOnlyDictionary<string, double[]>> candidates, ReplicaKind kind)
    {
        var replicaCount = FixtureReplicas.CountOf(formulation);
        var replicas = FixtureReplicas.LoadReplicas(formulation, kind, replicaCount);

        var referenceCells = ResultsMFile.ParseCells(FixtureReplicas.ReferencePath(formulation));
        var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
            ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
            : [];
        var i0 = CategoryAxis.FixedWidthPrefixLength(referenceDkarmcat);

        var candidateMatches = CategoryAxis.PrefixMatchLengths(referenceDkarmcat, candidates);
        var replicaMatches = CategoryAxis.PrefixMatchLengths(referenceDkarmcat, replicas);

        var candidateAugmented = new List<Dictionary<string, double[]>>(candidates.Count);
        for (var i = 0; i < candidates.Count; i++)
        {
            candidateAugmented.Add(CanonicalAxisSetCells.Augment(candidates[i], i0, candidateMatches[i]));
        }

        var replicaAugmented = new List<Dictionary<string, double[]>>(replicas.Count);
        for (var i = 0; i < replicas.Count; i++)
        {
            replicaAugmented.Add(CanonicalAxisSetCells.Augment(replicas[i], i0, replicaMatches[i]));
        }

        // this node's BOOT.md, "## Set comparison": the only exclusion rule CompareSets applies is the
        // replica-set rule, "not compared against independent replicas" — the defect rule ("reference cells
        // below 1e-30 compare as zero") is deliberately not applied here: a defect of the original must be found
        // and matched to its own declared-differences row, never hidden by zeroing it out first.
        var excludedFromIndependent = kind == ReplicaKind.Independent
            ? ExclusionRules.ExcludedFromIndependentQuantitiesOf(formulation)
            : new HashSet<string>(StringComparer.Ordinal);

        var excludedQuantityCells = CountExcludedQuantityCells(candidateAugmented, replicaAugmented, excludedFromIndependent);
        foreach (var run in candidateAugmented.Concat(replicaAugmented))
        {
            foreach (var name in excludedFromIndependent)
            {
                _ = run.Remove(name);
            }
        }

        ZeroFillOrdinaryToUnionLength(candidateAugmented, replicaAugmented);

        var candidateSet = candidateAugmented.Select(d => (IReadOnlyDictionary<string, double[]>)d).ToList();
        var replicaSet = replicaAugmented.Select(d => (IReadOnlyDictionary<string, double[]>)d).ToList();
        return (candidateSet, replicaSet, excludedQuantityCells);
    }

    // The replica-set rule's own whole-quantity drop, counted toward Excluded before removal — the same shape
    // ReferenceComparison.ExcludeWholeIndependentQuantity already uses for Compare's own version of this rule.
    private static int CountExcludedQuantityCells(
        IReadOnlyList<Dictionary<string, double[]>> candidates, IReadOnlyList<Dictionary<string, double[]>> replicas,
        IReadOnlyCollection<string> excludedNames)
    {
        var total = 0;
        foreach (var name in excludedNames)
        {
            var maxLength = 0;
            foreach (var run in candidates.Concat(replicas))
            {
                if (run.TryGetValue(name, out var array) && array.Length > maxLength)
                {
                    maxLength = array.Length;
                }
            }

            total += maxLength;
        }

        return total;
    }

    private static bool IsNoFillName(string name) =>
        name.EndsWith("@adaptive", StringComparison.Ordinal) || NoFillNames.Contains(name);

    // this node's BOOT.md, "## Set comparison": every remaining ("ordinary") array zero-filled to the union
    // length of both sets combined — a missing quantity counts as zeros, the same rule this node's own BOOT.md
    // already states for `Compare` ("Cells absent from an array ... compare as 0"). The canonical-axis
    // `@adaptive` keys and the three adaptive-tail scalars are excluded from this pass: a position past a run's
    // own reach is not this run's zero, so it is left absent and `TwoSampleComparison.OfSets`'s own per-source
    // presence check drops it instead — the same reasoning `CanonicalAxisSetCells`'s own kept `fqdokkarm` rows
    // rely on for their own, narrower union (only the rows a run actually kept take part in it).
    private static void ZeroFillOrdinaryToUnionLength(
        List<Dictionary<string, double[]>> candidates, List<Dictionary<string, double[]>> replicas)
    {
        var allRuns = candidates.Concat(replicas).ToList();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var run in allRuns)
        {
            foreach (var name in run.Keys)
            {
                if (!IsNoFillName(name))
                {
                    _ = names.Add(name);
                }
            }
        }

        foreach (var name in names)
        {
            var maxLength = 0;
            foreach (var run in allRuns)
            {
                if (run.TryGetValue(name, out var array) && array.Length > maxLength)
                {
                    maxLength = array.Length;
                }
            }

            foreach (var run in allRuns)
            {
                var current = run.GetValueOrDefault(name);
                if (current is not null && current.Length == maxLength)
                {
                    continue;
                }

                var filled = new double[maxLength];
                if (current is not null)
                {
                    Array.Copy(current, filled, current.Length);
                }

                run[name] = filled;
            }
        }
    }

    private static (List<IReadOnlyDictionary<string, double[]>> First, List<IReadOnlyDictionary<string, double[]>> Second) SplitByParity(
        List<IReadOnlyDictionary<string, double[]>> runs, int ordinalOffset)
    {
        var first = new List<IReadOnlyDictionary<string, double[]>>();
        var second = new List<IReadOnlyDictionary<string, double[]>>();
        for (var i = 0; i < runs.Count; i++)
        {
            if ((i + ordinalOffset) % 2 == 0)
            {
                first.Add(runs[i]);
            }
            else
            {
                second.Add(runs[i]);
            }
        }

        return (first, second);
    }
}
