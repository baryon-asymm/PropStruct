namespace PropStruct.Tests.Harness;

/// <summary>
/// The robust per-cell intraclass correlation and its pooled median (`tests/Harness/HISTORY.md#decision-vii-full-reasoning`,
/// `tests/Harness/HISTORY.md#decision-viii-full-reasoning`). Split out of <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition,
/// 2026-09-26).
/// </summary>
internal static class CellRho
{
    // `tests/Harness/HISTORY.md#decision-viii-full-reasoning`: one cell's own (p_i = k_i/n_i,
    // sigma2_cell = Var_i(p_i) - Mean_i(p_i(1-p_i)/n_i), rho_cell = sigma2_cell / (p_hat_cell (1 - p_hat_cell)))
    // reconstruction, extracted out of `RobustRhoEstimate`'s own former inner loop (root BOOT.md Taboos: no second
    // implementation of a formula) so decision VIII's own bootstrap check
    // (`IntervalWidthDiagnosticTests.DecisionVIIIBootstrapCheckFqDokKarmRhoCellByDecile`) can read exactly the
    // same per-cell numbers `RobustRhoEstimate` pools from, rather than recomputing them a second time. Deliberately
    // returns the UNTRUNCATED `sigma2_cell`/`rho_cell` (no `Math.Max(0, .)` here): decision VII's own family-wide
    // pooling truncates each cell before taking the median (mathematically identical to truncating the ratio,
    // since the denominator `p_hat_cell (1 - p_hat_cell)` is always positive here — `RobustRhoEstimate` still does
    // exactly that, unchanged), while decision VIII's own stratified estimator pools the untruncated values and
    // truncates once, at the end, on the pooled result — the two callers apply the truncation at different points,
    // so the shared primitive must not decide it for them. `null` when the cell fails any of the three existing
    // eligibility gates: fewer than `MinimumDispersionContributingRuns` contributing replicas, a degenerate
    // `p_hat_cell` (`<= 0` or `>= 1`), or an expected count (`n_bar_cell * p_hat_cell`) under
    // `MinimumExpectedCountForDispersionPooling` — the same three gates `RobustRhoEstimate` already applied inline,
    // moved here verbatim, not loosened or tightened.
    internal readonly record struct CellRhoEstimate(double NBar, double PHat, double RhoCell);

    internal static CellRhoEstimate? Compute(IReadOnlyList<double> ks, IReadOnlyList<double> ns)
    {
        if (ks.Count < DispersionEstimator.MinimumDispersionContributingRuns)
        {
            return null;
        }

        var sumK = ks.Sum();
        var sumN = ns.Sum();
        if (sumN <= 0.0)
        {
            return null;
        }

        var pHatCell = sumK / sumN;
        if (pHatCell is <= 0.0 or >= 1.0)
        {
            return null;
        }

        var nBarCell = ns.Average();
        if (nBarCell * pHatCell < DispersionEstimator.MinimumExpectedCountForDispersionPooling)
        {
            return null;
        }

        var ps = new double[ks.Count];
        for (var i = 0; i < ks.Count; i++)
        {
            ps[i] = ks[i] / ns[i];
        }

        var meanP = ps.Average();
        var sumOfSquares = 0.0;
        foreach (var p in ps)
        {
            sumOfSquares += (p - meanP) * (p - meanP);
        }

        var varianceP = sumOfSquares / (ps.Length - 1);

        var meanBinomialPVariance = 0.0;
        for (var i = 0; i < ps.Length; i++)
        {
            meanBinomialPVariance += ps[i] * (1.0 - ps[i]) / ns[i];
        }

        meanBinomialPVariance /= ps.Length;

        var sigma2Cell = varianceP - meanBinomialPVariance;
        return new CellRhoEstimate(nBarCell, pHatCell, sigma2Cell / (pHatCell * (1.0 - pHatCell)));
    }

    // `tests/Harness/HISTORY.md#decision-vii-full-reasoning`, item 2: pooled over the family by the
    // MEDIAN of `rho_cell` (each cell truncated to `Math.Max(0, .)` first, per decision VII's own settled
    // formula — decision VIII's own stratified estimator, where the truncation instead applies once to the pooled
    // result, is a caller of `Compute` above, not this method), over cells whose own expected count
    // (`n_bar_cell * p_hat_cell`) clears `MinimumExpectedCountForDispersionPooling` — the same floor the
    // tail-pooling groups above already use, reused rather than a second threshold invented for this one estimator.
    // Returns (null, null) when no cell clears both the minimum-replica-count and the expected-count floors.
    internal static (double? Median, double? Spread) RobustRhoEstimate(
        List<Dictionary<int, double>> perReplicaCounts, List<double> totals)
    {
        var maxIndex = 0;
        foreach (var counts in perReplicaCounts)
        {
            foreach (var index in counts.Keys)
            {
                maxIndex = Math.Max(maxIndex, index);
            }
        }

        var rhoCells = new List<double>();
        for (var j = 0; j <= maxIndex; j++)
        {
            var ns = new List<double>();
            var ks = new List<double>();
            for (var r = 0; r < perReplicaCounts.Count; r++)
            {
                if (perReplicaCounts[r].TryGetValue(j, out var k) && totals[r] > 0.0)
                {
                    ks.Add(k);
                    ns.Add(totals[r]);
                }
            }

            if (Compute(ks, ns) is not { } cell)
            {
                continue;
            }

            rhoCells.Add(Math.Max(0.0, cell.RhoCell));
        }

        if (rhoCells.Count == 0)
        {
            return (null, null);
        }

        rhoCells.Sort();
        var median = rhoCells.Count % 2 == 0
            ? (rhoCells[rhoCells.Count / 2 - 1] + rhoCells[rhoCells.Count / 2]) / 2.0
            : rhoCells[rhoCells.Count / 2];

        // Half the interquartile range: a robust companion to the median (decision VII, item 2: "report the
        // estimate's own spread beside it").
        var q1 = Quantile(rhoCells, 0.25);
        var q3 = Quantile(rhoCells, 0.75);
        return (median, (q3 - q1) / 2.0);
    }

    // Linear-interpolation quantile of an already-sorted list. `internal`, not `private`: decision VIII's own
    // bootstrap check (`IntervalWidthDiagnosticTests.DecisionVIIIBootstrapCheckFqDokKarmRhoCellByDecile`) reuses
    // it for the reported 5th/95th percentile of each decile's bootstrap distribution, rather than a second
    // quantile implementation (root BOOT.md Taboos: no second implementation of a formula).
    internal static double Quantile(List<double> sortedValues, double q)
    {
        if (sortedValues.Count == 1)
        {
            return sortedValues[0];
        }

        var pos = q * (sortedValues.Count - 1);
        var lower = (int)Math.Floor(pos);
        var upper = (int)Math.Ceiling(pos);
        if (lower == upper)
        {
            return sortedValues[lower];
        }

        var fraction = pos - lower;
        return sortedValues[lower] + fraction * (sortedValues[upper] - sortedValues[lower]);
    }
}
