namespace PropStruct.Tests.Harness;

/// <summary>
/// The family's pooled Pearson dispersion <c>phi</c> and the route it decides — the count-predictive rho
/// (<see cref="CellRho"/>) or the plain Student band (`tests/Harness/HISTORY.md#fable-5-1-decision-v`). Split out of
/// <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class DispersionEstimator
{
    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: `Runs`/`Groups`/`NBar` are the diagnostic's own bookkeeping
    // (how many replicas and pooled groups the estimate rests on); `Phi < 1.0` is the routing signal itself (the
    // family does not fit a count predictive at all, this node's BOOT.md item 3 — the cell takes the ordinary
    // Student band on its own reconstructed count instead, never `BetaBinomialPredictive.Interval`).
    //
    // `tests/Harness/HISTORY.md#decision-vii-full-reasoning`, item 2: `RhoHat`, when `Phi >= 1.0`, is
    // the MEDIAN of the family's own per-cell robust rho estimates (`CellRho.RobustRhoEstimate`), not `(Phi - 1) /
    // (NBar - 1)` any more — item 1's own excess-variance check found the old, phi-derived estimate biased upward
    // by the family's numerous small cells. `RhoHatSpread` (half the interquartile range of those same per-cell
    // estimates) is `null` exactly when the robust estimator itself had no qualifying cell and `RhoHat` fell back
    // to the old formula (this method's own fallback, kept for a family too sparse to say anything better) — a
    // reader who sees `RhoHatSpread` is null knows the reported `RhoHat` is the older, coarser estimate.
    internal readonly record struct DispersionEstimate(int Runs, int Groups, double NBar, double Phi, double RhoHat, double? RhoHatSpread);

    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the number of contributing replicas below which a phi estimate
    // is not attempted at all (decision IV's own diagnostic threshold, carried over unchanged — a family/row this
    // sparse has no evidence either way and keeps whatever fallback the caller uses instead). internal, not
    // private: `CellRho.Compute` reuses this same eligibility gate rather than a second copy of the threshold.
    internal const int MinimumDispersionContributingRuns = 4;

    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the tail-pooling rule brought forward for phi's own estimation
    // — cells (or, for `fqdokkarm`, pooled groups of cells) whose pooled expected count falls under this floor
    // are merged with their neighbours in print order before phi is computed on them, so a handful of near-empty
    // bins cannot swamp the pooled Pearson statistic with noise. Distinct from the *production* tail-pooling
    // mechanism named in Fable's own order (item 3, unstarted): that one pools cells for the criterion's own
    // comparison, at its own threshold; this one only stabilizes the dispersion estimate itself. internal, not
    // private: `CellRho.Compute` reuses this same floor.
    internal const double MinimumExpectedCountForDispersionPooling = 5.0;

    /// <summary>
    /// `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the conditional dispersion diagnostic promoted to production
    /// code — `p_hat = sum(k_i) / sum(n_i)` per pooled group, `phi = (1/(R-1)) * sum((k_i - n_i*p_hat)^2 /
    /// (n_i*p_hat*(1-p_hat)))` averaged over the pooled groups, `rho_hat = max(0, (phi - 1) / (n_bar - 1))`. One
    /// implementation, called both by <see cref="CanonicalAxisCells"/>/<see cref="OrdinaryQuantityCells"/>'s own
    /// per-array wiring and by <c>DispersionDiagnosticTests</c> (which used to carry this formula itself, before
    /// this promotion — `tests/Harness/HISTORY.md#fable-5-1-decision-v`, item 1). Returns <see langword="null"/>
    /// when there are fewer than <see cref="MinimumDispersionContributingRuns"/> contributing replicas, the mean
    /// total is zero, or no pooled group ever clears the pooling floor — exactly the diagnostic's own "not
    /// enough data" cases. <paramref name="replicaTotals"/> must be
    /// <see cref="CountReconstruction.ArrayTotals"/>'s own output for the same <paramref name="replicaCells"/>
    /// and <paramref name="arrayName"/> (never recomputed here, so the totals a caller already has and the ones
    /// this method sums from are provably the same numbers).
    /// </summary>
    internal static DispersionEstimate? Estimate(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string arrayName,
        IReadOnlyList<double?> replicaTotals)
    {
        var (perReplicaCounts, totals) = CountReconstruction.PerCellCounts(replicaCells, arrayName, replicaTotals);

        if (perReplicaCounts.Count < MinimumDispersionContributingRuns)
        {
            return null;
        }

        var nBar = totals.Average();
        if (nBar <= 0.0)
        {
            return null;
        }

        var maxIndex = 0;
        foreach (var counts in perReplicaCounts)
        {
            foreach (var index in counts.Keys)
            {
                maxIndex = Math.Max(maxIndex, index);
            }
        }

        var groupNumerator = 0.0;
        var groupCount = 0;
        var pendingKByReplica = new double[perReplicaCounts.Count];
        var hasPending = false;

        void FlushGroup()
        {
            if (!hasPending)
            {
                return;
            }

            var sumK = pendingKByReplica.Sum();
            var sumN = totals.Sum();
            var pHat = sumK / sumN;
            if (pHat is > 0.0 and < 1.0)
            {
                var numerator = 0.0;
                for (var r = 0; r < perReplicaCounts.Count; r++)
                {
                    var expected = totals[r] * pHat;
                    var residual = pendingKByReplica[r] - expected;
                    numerator += residual * residual / (totals[r] * pHat * (1.0 - pHat));
                }

                groupNumerator += numerator;
                groupCount++;
            }

            Array.Clear(pendingKByReplica);
            hasPending = false;
        }

        for (var j = 0; j <= maxIndex; j++)
        {
            for (var r = 0; r < perReplicaCounts.Count; r++)
            {
                if (perReplicaCounts[r].TryGetValue(j, out var k))
                {
                    pendingKByReplica[r] += k;
                }
            }

            hasPending = true;

            var pendingSumK = pendingKByReplica.Sum();
            var pendingSumN = totals.Sum();
            var pendingPHat = pendingSumN > 0.0 ? pendingSumK / pendingSumN : 0.0;
            if (nBar * pendingPHat >= MinimumExpectedCountForDispersionPooling)
            {
                FlushGroup();
            }
        }

        FlushGroup();

        if (groupCount == 0)
        {
            return null;
        }

        var phi = groupNumerator / ((perReplicaCounts.Count - 1) * groupCount);

        // `tests/Harness/HISTORY.md#decision-vii-full-reasoning`, item 2: phi keeps deciding the
        // routing (phi < 1.0, unchanged, this method's own caller reads it for exactly that) — but once phi >=
        // 1.0 says the family belongs to a count predictive at all, rho_hat itself now comes from the robust,
        // per-cell median estimator, not from (phi - 1) / (n_bar - 1): item 1's own measurement found phi biased
        // upward by the family's numerous small cells and then applied, through the multiplicative (n - 1)
        // factor, to cells three orders of magnitude larger. The old formula is kept only as the fallback when
        // the robust estimator finds no cell clearing its own expected-count floor (too sparse a family/run to
        // say anything better) — never silently reverted to for a family the robust estimator can, in fact, read.
        double rhoHat;
        double? rhoHatSpread = null;
        if (phi < 1.0)
        {
            rhoHat = 0.0;
        }
        else
        {
            var (medianRhoCell, spread) = CellRho.RobustRhoEstimate(perReplicaCounts, totals);
            rhoHat = medianRhoCell ?? Math.Max(0.0, (phi - 1.0) / (nBar - 1.0));
            rhoHatSpread = spread;
        }

        return new DispersionEstimate(perReplicaCounts.Count, groupCount, nBar, phi, rhoHat, rhoHatSpread);
    }
}
