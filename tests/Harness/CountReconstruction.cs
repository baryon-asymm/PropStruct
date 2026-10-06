namespace PropStruct.Tests.Harness;

/// <summary>
/// Printed values reconstructed back to integer counts: per cell, per array, per replica
/// (`tests/Harness/HISTORY.md#fable-5-1-decision-v`). Split out of <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class CountReconstruction
{
    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: one run's own reconstructed integer count for the printed
    // cell `cell.Value`, given that run's own quantum — the inner term `ArrayTotals` and the candidate's own
    // total both need, written once so neither is a second implementation of the other (root BOOT.md Taboos). A
    // cell whose own value is exactly zero contributes zero to the total unconditionally (no resolution guard
    // needed: zero is always distinguishable from a non-zero count); a non-zero cell whose own quantum does not
    // clear its own print resolution carries no information about its count and is left out of the total, not
    // defaulted to zero (the same "resolvable cell" principle `RunQuantum.TryInfer`'s own search uses).
    internal static double ReconstructTotal(IReadOnlyList<ResultCell> array, RunQuantum.QuantumEstimate estimate)
    {
        var total = 0.0;
        foreach (var cell in array)
        {
            if (Math.Abs(cell.Value) <= 0.0)
            {
                continue;
            }

            if (estimate.Quantum <= cell.Resolution)
            {
                continue;
            }

            total += Math.Round(Math.Abs(cell.Value) / estimate.Quantum);
        }

        return total;
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: each replica's own reconstructed grand total for the whole
    // array `arrayName` — the "n_i" the conditional dispersion diagnostic and the beta-binomial predictive both
    // need (p_hat = sum(k_i) / sum(n_i), the cell's own count over the run's own total), using the same per-run
    // quantum `Compare` itself infers (`RunQuantum.TryInfer`) and the same reconstruction `ReconstructTotal` above
    // performs — one implementation, shared by this method, `DispersionEstimator.Estimate` and the wiring in
    // `ReferenceComparison`, never a second one written for any of them. `null` at index `r` means replica `r`'s
    // own array does not support a quantum (`RunQuantum.TryInfer` failed on it) and is excluded from both the
    // dispersion estimate and the per-cell floor, exactly as `replicaQuantums[r] is null` already excludes it
    // from the ordinary per-cell count reconstruction.
    internal static double?[] ArrayTotals(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string arrayName)
    {
        var totals = new double?[replicaCells.Count];
        for (var r = 0; r < replicaCells.Count; r++)
        {
            if (replicaCells[r].TryGetValue(arrayName, out var array) && RunQuantum.TryInfer(array, out var estimate))
            {
                totals[r] = ReconstructTotal(array, estimate);
            }
        }

        return totals;
    }

    // `tests/Harness/HISTORY.md#decision-vii-full-reasoning`: the per-cell (k_i, n_i) reconstruction
    // `DispersionEstimator.Estimate` already needed internally — one contributing replica's own resolved counts
    // by cell index, alongside that same replica's own already-reconstructed grand total — extracted so the
    // excess-variance-ratio check (decision VII, item 1, `IntervalWidthDiagnosticTests`) shares this construction
    // rather than reconstructing per-cell counts a third time in this file (root BOOT.md Taboos: "no second
    // implementation of ... a formula"; `DispersionEstimator.Estimate` is the second, this is now the one place
    // both read from). A replica contributes only when it both has an entry in `replicaTotals` (`ArrayTotals`'s
    // own "quantum resolved" test) and resolves its own quantum again here — the same double-gate
    // `DispersionEstimator.Estimate` always applied, unchanged by this extraction.
    internal static (List<Dictionary<int, double>> PerReplicaCounts, List<double> Totals) PerCellCounts(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string arrayName,
        IReadOnlyList<double?> replicaTotals)
    {
        var perReplicaCounts = new List<Dictionary<int, double>>();
        var totals = new List<double>();
        for (var r = 0; r < replicaCells.Count; r++)
        {
            if (replicaTotals[r] is not { } total || !replicaCells[r].TryGetValue(arrayName, out var array)
                || !RunQuantum.TryInfer(array, out var estimate))
            {
                continue;
            }

            var counts = new Dictionary<int, double>();
            for (var j = 0; j < array.Length; j++)
            {
                var cell = array[j];
                if (Math.Abs(cell.Value) <= 0.0)
                {
                    counts[j] = 0.0;
                    continue;
                }

                if (estimate.Quantum <= cell.Resolution)
                {
                    continue;
                }

                counts[j] = Math.Round(Math.Abs(cell.Value) / estimate.Quantum);
            }

            perReplicaCounts.Add(counts);
            totals.Add(total);
        }

        return (perReplicaCounts, totals);
    }
}
