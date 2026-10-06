namespace PropStruct.Tests.Harness;

/// <summary>
/// Loads <see cref="StatisticalCriterion.Compare"/>'s inputs once per call and dispatches each printed quantity
/// name to its own rule: the canonical <c>fqdokkarm</c> row family and the whole-array category axis to
/// <see cref="CanonicalAxisCells"/>, everything else to <see cref="OrdinaryQuantityCells"/>. Split out of
/// <c>StatisticalCriterion.Comparisons.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class ReferenceComparison
{
    internal static (List<PendingCell> Pending, int Excluded) BuildComparePending(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var inputs = LoadCompareInputs(formulation, candidate, kind, excludeReplicaOrdinal);
        var dkarmcatMatches = CategoryAxis.ComputeDkarmcatPrefixMatches(inputs.ReferenceCells, candidate, inputs.ReplicaCells);

        var excluded = 0;
        var pending = new List<PendingCell>();
        foreach (var name in inputs.QuantityNames)
        {
            _ = inputs.ReferenceCells.TryGetValue(name, out var referenceArray);
            _ = candidate.TryGetValue(name, out var candidateArray);

            // this node's BOOT.md, "Exclusion rule vocabulary": a quantity named by the "not compared against
            // independent replicas" rule is dropped whole, only against ReplicaKind.Independent (a no-op
            // otherwise) — every cell counted in Excluded rather than compared, before any of the per-cell
            // machinery below (the row family, the canonical-axis carve-out, the count-like floor) ever sees it.
            if (kind == ReplicaKind.Independent && inputs.ExcludedFromIndependentQuantities.Contains(name))
            {
                excluded += ExcludeWholeIndependentQuantity(referenceArray, candidateArray, inputs.ReplicaCells, name);
                continue;
            }

            var zeroBelow = inputs.ZeroBelowQuantities.Contains(name);
            var isDistributionFunction = QuantityFamilies.IsFractionFamily(name);

            // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #2: print resolution is a property of the
            // array's own field (one shared Fortran edit descriptor for every cell of it), not of a specific
            // reference cell's own printed token — inferred once per name, from any non-zero reference cell
            // (`PrintResolution.InferDecimalDigits`), then applied to the candidate's own value at every index,
            // including one past the reference's own printed length, where a per-index lookup would have no
            // resolution to borrow at all.
            var decimalDigits = PrintResolution.InferDecimalDigits(referenceArray);

            var (candidateQuantum, replicaQuantums) = InferCompareQuantums(
                name, candidateArray, decimalDigits, isDistributionFunction, inputs.ReplicaCells);

            if (QuantityFamilies.TryMatchFqDokKarmRow(name, out var rowIndex1Based))
            {
                excluded += CanonicalAxisCells.AddFqDokKarmRowCells(
                    pending, name, rowIndex1Based, referenceArray, candidateArray, inputs.ReplicaCells,
                    dkarmcatMatches.ReplicaMatch, dkarmcatMatches.CandidateMatch, isDistributionFunction,
                    candidateQuantum, replicaQuantums, decimalDigits, zeroBelow);
                continue;
            }

            if (QuantityFamilies.CategoryLengthVaryingArrays.Contains(name))
            {
                excluded += CanonicalAxisCells.AddCategoryLengthVaryingCells(
                    pending, name, referenceArray, candidateArray, inputs.ReplicaCells,
                    dkarmcatMatches.ReplicaMatch, dkarmcatMatches.CandidateMatch, zeroBelow);
                continue;
            }

            excluded += OrdinaryQuantityCells.AddOrdinaryQuantityCells(
                pending, name, inputs.ReferenceCells, candidate, referenceArray, candidateArray, inputs.ReplicaCells,
                zeroBelow, isDistributionFunction, candidateQuantum, replicaQuantums, decimalDigits);
        }

        return (pending, excluded);
    }

    private static CompareInputs LoadCompareInputs(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal)
    {
        var replicaCount = FixtureReplicas.CountOf(formulation);

        var referenceCells = ResultsMFile.ParseCells(FixtureReplicas.ReferencePath(formulation));

        // `tests/Harness/HISTORY.md#per-run-quantum`: a distribution-function cell's quantum is
        // a property of the one run that printed it (its own total), so every run needs its own print resolution,
        // not just the reference's — the sibling-pool predecessor of this rule loaded replicas as bare
        // `double[]` and paid for it (this section's own "Implementation and measurements"). `Compare` is the
        // only caller that needs this; `CompareTailRowMean`/`CompareAdaptiveIndexMatched`/`TwoSampleBias` never
        // treat a cell as count-like and keep loading replicas as `double[]` (`LoadReplicas`, unchanged, below).
        var replicaCells = FixtureReplicas.LoadReplicaCells(formulation, kind, replicaCount, excludeReplicaOrdinal);

        var zeroBelowQuantities = ExclusionRules.ZeroBelowQuantitiesOf(formulation);
        var excludedFromIndependentQuantities = ExclusionRules.ExcludedFromIndependentQuantitiesOf(formulation);

        var quantityNames = new SortedSet<string>(StringComparer.Ordinal);
        quantityNames.UnionWith(referenceCells.Keys);
        quantityNames.UnionWith(candidate.Keys);
        foreach (var replica in replicaCells)
        {
            quantityNames.UnionWith(replica.Keys);
        }

        return new CompareInputs(referenceCells, replicaCells, zeroBelowQuantities, excludedFromIndependentQuantities, quantityNames);
    }

    // `BuildComparePending`'s own inputs, gathered once per call (this node's BOOT.md, ## Invariants, "`m` and
    // `t` are computed once per `Compare` call, not per cell"): a pure grouping of what used to be five local
    // variables at the top of that method, not a new contract.
    private sealed record CompareInputs(
        IReadOnlyDictionary<string, ResultCell[]> ReferenceCells,
        List<IReadOnlyDictionary<string, ResultCell[]>> ReplicaCells,
        HashSet<string> ZeroBelowQuantities,
        HashSet<string> ExcludedFromIndependentQuantities,
        SortedSet<string> QuantityNames);

    private static int ExcludeWholeIndependentQuantity(
        ResultCell[]? referenceArray, double[]? candidateArray, List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string name)
    {
        var excludedLength = Math.Max(referenceArray?.Length ?? 0, candidateArray?.Length ?? 0);
        foreach (var replica in replicaCells)
        {
            if (replica.TryGetValue(name, out var replicaArray) && replicaArray.Length > excludedLength)
            {
                excludedLength = replicaArray.Length;
            }
        }

        return excludedLength;
    }

    private static (RunQuantum.QuantumEstimate? CandidateQuantum, RunQuantum.QuantumEstimate?[] ReplicaQuantums) InferCompareQuantums(
        string name, double[]? candidateArray, int? decimalDigits, bool isDistributionFunction,
        List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells)
    {
        RunQuantum.QuantumEstimate? candidateQuantum = null;
        var replicaQuantums = new RunQuantum.QuantumEstimate?[replicaCells.Count];
        if (isDistributionFunction)
        {
            if (candidateArray is not null && RunQuantum.TryInfer(PrintResolution.BuildSyntheticCells(candidateArray, decimalDigits), out var candidateEstimate))
            {
                candidateQuantum = candidateEstimate;
            }

            for (var r = 0; r < replicaCells.Count; r++)
            {
                if (replicaCells[r].TryGetValue(name, out var ownArray) && RunQuantum.TryInfer(ownArray, out var replicaEstimate))
                {
                    replicaQuantums[r] = replicaEstimate;
                }
            }
        }

        return (candidateQuantum, replicaQuantums);
    }
}
