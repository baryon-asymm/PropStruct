namespace PropStruct.Tests.Harness;

/// <summary>
/// Cells on the canonical category axis (this node's BOOT.md, ## Invariants, "Canonical category axis"): the
/// <c>fqdokkarm(&lt;row&gt;,:)</c> row family, matched by canonical row rather than printed index, and the
/// whole-array category-length-varying arrays (<c>Dkarmcat</c>/<c>dokkarm43</c>/<c>dokkarm10</c>). Split out of
/// <c>StatisticalCriterion.Comparisons.cs</c> (decomposition, 2026-09-26); called from
/// <see cref="ReferenceComparison"/>.
/// </summary>
internal static class CanonicalAxisCells
{
    // Extracted from `BuildComparePending`'s own canonical-`fqdokkarm`-row branch (a pure move, no behaviour
    // change). Returns the number of newly excluded cells for this row; every index that reaches the per-index
    // loop below is always added to `pending`, so 0 is returned once that loop runs.
    internal static int AddFqDokKarmRowCells(
        List<PendingCell> pending, string name, int row1Based, ResultCell[]? referenceArray, double[]? candidateArray,
        List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, int[] replicaDkarmcatMatch,
        int candidateDkarmcatMatch, bool isDistributionFunction, RunQuantum.QuantumEstimate? candidateQuantum,
        RunQuantum.QuantumEstimate?[] replicaQuantums, int? decimalDigits, bool zeroBelow)
    {
        var rowIndex0 = row1Based - 1;
        var maxLength = Math.Max(referenceArray?.Length ?? 0, candidateArray?.Length ?? 0);
        for (var r = 0; r < replicaCells.Count; r++)
        {
            if (replicaDkarmcatMatch[r] > rowIndex0 && replicaCells[r].TryGetValue(name, out var replicaRow))
            {
                maxLength = Math.Max(maxLength, replicaRow.Length);
            }
        }

        if (candidateArray is null || candidateDkarmcatMatch <= rowIndex0)
        {
            return maxLength;
        }

        var contributingReplicaIndices = Enumerable.Range(0, replicaCells.Count)
            .Where(r => replicaDkarmcatMatch[r] > rowIndex0 && replicaCells[r].ContainsKey(name))
            .ToArray();
        if (contributingReplicaIndices.Length < 2)
        {
            return maxLength;
        }

        // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the dispersion estimate for a canonical
        // `fqdokkarm` row rests only on the replicas eligible for that row (`contributingReplicaIndices`,
        // the same canonical-axis restriction the row's own comparison already applies) — a replica whose
        // Dkarmcat never reaches this row is a different physical category from here on, not merely
        // absent data, and must not feed a rho estimate for this row any more than it feeds the row's own
        // comparison.
        var rowEligibleCells = contributingReplicaIndices.Select(r => replicaCells[r]).ToArray();
        var rowReplicaTotals = isDistributionFunction ? CountReconstruction.ArrayTotals(rowEligibleCells, name) : null;
        var rowDispersion = rowReplicaTotals is not null
            ? DispersionEstimator.Estimate(rowEligibleCells, name, rowReplicaTotals)
            : null;
        var rowCandidateTotal = isDistributionFunction && candidateQuantum is { } rowCq
            ? CountReconstruction.ReconstructTotal(PrintResolution.BuildSyntheticCells(candidateArray, decimalDigits), rowCq)
            : (double?)null;

        for (var index = 0; index < maxLength; index++)
        {
            var referenceCell = referenceArray is not null && index < referenceArray.Length ? referenceArray[index] : default;
            var cand = index < candidateArray.Length ? candidateArray[index] : 0.0;
            var replicaValues = contributingReplicaIndices
                .Select(r => index < replicaCells[r][name].Length ? replicaCells[r][name][index].Value : 0.0)
                .ToArray();

            List<double>? replicaCountsForCell = null;
            List<double>? replicaTotalsForCell = null;
            if (isDistributionFunction)
            {
                replicaCountsForCell = new List<double>(contributingReplicaIndices.Length);
                replicaTotalsForCell = new List<double>(contributingReplicaIndices.Length);
                for (var i = 0; i < contributingReplicaIndices.Length; i++)
                {
                    var r = contributingReplicaIndices[i];
                    if (replicaQuantums[r] is not { } rowEstimate)
                    {
                        continue;
                    }

                    var value = index < replicaCells[r][name].Length ? replicaCells[r][name][index].Value : 0.0;
                    replicaCountsForCell.Add(Math.Round(Math.Abs(value) / rowEstimate.Quantum));
                    replicaTotalsForCell.Add(rowReplicaTotals![i]!.Value);
                }
            }

            pending.Add(PendingCell.From(
                name, index, referenceCell, cand, replicaValues, zeroBelow, isDistributionFunction,
                candidateQuantum?.Quantum, candidateQuantum?.Resolution, candidateQuantum?.Identified ?? true,
                replicaCountsForCell,
                decimalDigits: decimalDigits, replicaTotals: replicaTotalsForCell, candidateTotal: rowCandidateTotal,
                dispersion: rowDispersion));
        }

        return 0;
    }

    // Extracted from `BuildComparePending`'s own canonical whole-array category-axis branch (Dkarmcat/
    // dokkarm43/dokkarm10) — a pure move, no behaviour change. Returns the number of cells excluded.
    internal static int AddCategoryLengthVaryingCells(
        List<PendingCell> pending, string name, ResultCell[]? referenceArray, double[]? candidateArray,
        List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, int[] replicaDkarmcatMatch,
        int candidateDkarmcatMatch, bool zeroBelow)
    {
        var excluded = 0;
        var maxLength = Math.Max(referenceArray?.Length ?? 0, candidateArray?.Length ?? 0);
        for (var r = 0; r < replicaCells.Count; r++)
        {
            if (replicaCells[r].TryGetValue(name, out var replicaArray) && replicaArray.Length > maxLength)
            {
                maxLength = replicaArray.Length;
            }
        }

        for (var index = 0; index < maxLength; index++)
        {
            if (candidateArray is null || index >= candidateArray.Length || candidateDkarmcatMatch <= index)
            {
                excluded++;
                continue;
            }

            var replicaValues = Enumerable.Range(0, replicaCells.Count)
                .Where(r => replicaDkarmcatMatch[r] > index && replicaCells[r].TryGetValue(name, out var a) && index < a.Length)
                .Select(r => replicaCells[r][name][index].Value)
                .ToArray();
            if (replicaValues.Length < 2)
            {
                excluded++;
                continue;
            }

            var referenceCell = referenceArray is not null && index < referenceArray.Length ? referenceArray[index] : default;
            var cand = candidateArray[index];
            pending.Add(PendingCell.From(name, index, referenceCell, cand, replicaValues, zeroBelow, isDistributionFunction: false));
        }

        return excluded;
    }
}
