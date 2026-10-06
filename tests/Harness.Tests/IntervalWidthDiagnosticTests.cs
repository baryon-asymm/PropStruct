using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// `tests/Harness/HISTORY.md#decision-vii-full-reasoning`, item 1: before touching the beta-binomial
/// interval itself, checks whether its own variance formula — <c>Var(k) = n p q (1 + (n - 1) rho)</c>, one
/// <c>rho</c> per family applied across cells whose own <c>n</c> differ by orders of magnitude — is the right
/// *shape* for the extra, non-binomial variance this family of cells actually shows. For each count-like cell,
/// the excess-variance ratio <c>e = (Var_i(k_i) - Mean_i(n_i p̂ (1-p̂))) / (n̄² p̂²)</c> is, under the mechanism that
/// motivated the extra term (the cell's own probability drifting run to run), an estimate of that drift's own
/// variance and should not depend on <c>n̄</c>. Summarised by size decile, a flat <c>e</c> across deciles means the
/// multiplicative form is right (decision VII, item 2: fix the estimator); <c>e</c> falling with <c>n̄</c> means
/// the beta-binomial is the wrong shape and the extra variance is additive, not multiplicative (item 3).
/// </summary>
public class IntervalWidthDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public IntervalWidthDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly string[] FixedFamilies = ["fqkarm", "fqkarm_cor", "fqmkm1", "fqmkm2", "coef"];
    private const int MinimumContributingRuns = 4;

    private readonly record struct CellPoint(string Family, string Formulation, int Index, double NBar, double PHat, double E);

    [Trait("Category", "Long")]
    [Fact]
    public void Item1ExcessVarianceRatioAgainstCellSizeByDecile()
    {
        var points = new List<CellPoint>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt");
                replicaCells.Add(ResultsMFile.ParseCells(path));
            }

            var dokRowNames = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var cells in replicaCells)
            {
                foreach (var key in cells.Keys)
                {
                    if (key.StartsWith("fqdokkarm(", StringComparison.Ordinal))
                    {
                        _ = dokRowNames.Add(key);
                    }
                }
            }

            foreach (var family in FixedFamilies)
            {
                MeasureOneArray(formulation, family, family, replicaCells, points);
            }

            foreach (var rowName in dokRowNames)
            {
                MeasureOneArray(formulation, rowName, "fqdokkarm", replicaCells, points);
            }
        }

        Assert.True(points.Count > 1000, $"expected a well-populated cell sample, got {points.Count}.");

        _output.WriteLine($"Total cells measured: {points.Count}");
        _output.WriteLine("");

        // Per family (pooled across formulations, and across every fqdokkarm row for that one family), decile of
        // n_bar against the median e in that decile — decision VII, item 1's own "summarised by size decile".
        foreach (var familyGroup in points.GroupBy(p => p.Family).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            ReportDeciles(familyGroup.Key, familyGroup.ToList());
        }

        _output.WriteLine("");
        ReportDeciles("(pooled, every family)", points);
    }

    private static void MeasureOneArray(
        string formulation, string arrayName, string familyLabel,
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, List<CellPoint> points)
    {
        foreach (var (j, (ks, ns)) in CollectPerIndexCounts(arrayName, replicaCells))
        {
            var sumK = ks.Sum();
            var sumN = ns.Sum();
            if (sumN <= 0.0)
            {
                continue;
            }

            // decision VII, item 1: the cell's own p_hat, from the same restricted replica subset that resolved
            // this specific cell (decision IV's own "phat_j = Mean(k_j)/Mean(n_i)" notation, self-contained per
            // cell rather than pooled from the family's own EstimateDispersion, which this check exists to
            // question in the first place).
            var pHat = sumK / sumN;
            if (pHat is <= 0.0 or >= 1.0)
            {
                continue;
            }

            var meanK = ks.Average();
            var sumOfSquares = 0.0;
            foreach (var k in ks)
            {
                sumOfSquares += (k - meanK) * (k - meanK);
            }

            var varianceK = sumOfSquares / (ks.Count - 1);
            var nBar = ns.Average();
            var meanBinomialVariance = nBar * pHat * (1.0 - pHat);
            var e = (varianceK - meanBinomialVariance) / (nBar * nBar * pHat * pHat);

            points.Add(new CellPoint(familyLabel, formulation, j, nBar, pHat, e));
        }
    }

    // Extracted from `MeasureOneArray`'s own former body (root BOOT.md Taboos: no second implementation of a
    // formula) so the coordinator's two follow-up questions on decision VII's item 1 — where the residual
    // Count-rule violations sit by cell size, and how wide the actual interval is against the plain-binomial one
    // at the largest cells — can join the same per-cell (k_i, n_i) reconstruction this file already trusts,
    // rather than a second pass over `ReconstructArrayTotals`/`ReconstructPerCellCounts` with its own bookkeeping.
    private static Dictionary<int, (List<double> Ks, List<double> Ns)> CollectPerIndexCounts(
        string arrayName, IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells)
    {
        var result = new Dictionary<int, (List<double> Ks, List<double> Ns)>();
        var totals = CountReconstruction.ArrayTotals(replicaCells, arrayName);
        var (perReplicaCounts, replicaTotals) = CountReconstruction.PerCellCounts(replicaCells, arrayName, totals);
        if (perReplicaCounts.Count < MinimumContributingRuns)
        {
            return result;
        }

        var maxIndex = 0;
        foreach (var counts in perReplicaCounts)
        {
            foreach (var index in counts.Keys)
            {
                maxIndex = Math.Max(maxIndex, index);
            }
        }

        for (var j = 0; j <= maxIndex; j++)
        {
            var ks = new List<double>();
            var ns = new List<double>();
            for (var r = 0; r < perReplicaCounts.Count; r++)
            {
                if (perReplicaCounts[r].TryGetValue(j, out var k))
                {
                    ks.Add(k);
                    ns.Add(replicaTotals[r]);
                }
            }

            if (ks.Count >= MinimumContributingRuns)
            {
                result[j] = (ks, ns);
            }
        }

        return result;
    }

    // The coordinator's first follow-up on decision VII, item 1 (2026-09-20, asked directly after the item-2/
    // item-4 report): among the five (formulation, alpha) Count-rule rows that still violate their band after the
    // robust estimator (decision VII, item 4's re-read — `CalibrationCurveTests.Gate3...`, `tests/Harness/
    // HISTORY.md`'s own "before/after" table), do the individual FAILING cells sit at large n (as the size
    // hypothesis of decision VII's own diagnosis predicts), or are they scattered across the cell-size
    // distribution? Answered by joining `CompareCellVerdicts` — the exact verdict stream `CalibrationCurveTests`
    // pools into its own `K` — against this file's own per-cell n_bar (`CollectPerIndexCounts`, the same
    // reconstruction item 1 uses), never a second implementation of either.
    [Trait("Category", "Long")]
    [Fact]
    public void Item1FollowupViolatingRowsFailuresAgainstCellSize()
    {
        var violatingRows = new (string Formulation, double Alpha)[]
        {
            ("inpt", 0.05), ("P33", 0.05), ("PSAN02n", 0.05), ("HMX", 0.05), ("HMX", 0.01),
        };

        foreach (var (formulation, alpha) in violatingRows)
        {
            var replicaCount = ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount;
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            // Every count-like array this formulation ever produces a verdict for, resolved once and cached by
            // its own array name — a `fqdokkarm(row,:)` row is its own array, not the pooled `fqdokkarm` family
            // label `CellVerdict.Name` never uses.
            var nBarByArray = new Dictionary<string, Dictionary<int, double>>(StringComparer.Ordinal);
            double? LookupNBar(string arrayName, int index)
            {
                if (!nBarByArray.TryGetValue(arrayName, out var perIndex))
                {
                    perIndex = CollectPerIndexCounts(arrayName, replicaCells)
                        .ToDictionary(kv => kv.Key, kv => kv.Value.Ns.Average());
                    nBarByArray[arrayName] = perIndex;
                }

                return perIndex.TryGetValue(index, out var nBar) ? nBar : null;
            }

            var allNBars = new List<double>();
            var failingNBars = new List<(string Name, int Index, double NBar)>();

            for (var k = 1; k <= replicaCount; k++)
            {
                var candidate = ResultsMFile.Parse(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt"));
                var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k);

                foreach (var v in verdicts)
                {
                    if (v.Rule != CalibrationRule.Count || v.Degenerate || !v.Eligible)
                    {
                        continue;
                    }

                    var nBar = LookupNBar(v.Name, v.Index);
                    if (nBar is not { } nb)
                    {
                        continue;
                    }

                    allNBars.Add(nb);
                    if (v.Failed)
                    {
                        failingNBars.Add((v.Name, v.Index, nb));
                    }
                }
            }

            allNBars.Sort();
            var medianAll = Median(allNBars);
            var failingOnly = failingNBars.Select(f => f.NBar).OrderBy(x => x).ToList();
            var medianFailing = failingOnly.Count > 0 ? Median(failingOnly) : double.NaN;
            var aboveMedianCount = failingNBars.Count(f => f.NBar >= medianAll);

            _output.WriteLine(
                $"{formulation} alpha={alpha}: {allNBars.Count} eligible Count verdicts (n_bar median={medianAll:G6}), " +
                $"{failingNBars.Count} failing (n_bar median={medianFailing:G6}), " +
                $"{aboveMedianCount} of {failingNBars.Count} failing cells at or above the population's own median n_bar");
            _output.WriteLine(
                "  failing cells (name[index]=n_bar): " +
                string.Join(", ", failingNBars.OrderByDescending(f => f.NBar).Select(f => $"{f.Name}[{f.Index}]={f.NBar:G6}")));
        }
    }

    // The coordinator's second follow-up: for one violating family, at its largest-n cells, how wide is the
    // actual (beta-binomial, decision VII's own robust `rho_hat`) interval against the plain-binomial one (the
    // same call with `rho = 0`, `BetaBinomialInterval`'s own documented special case) — a ratio near one says the
    // estimator is now right and the residual cause is elsewhere; several says the inflation term is still doing
    // the damage. Checked on two of HMX's own arrays, both named heavily in the previous test's own failing-cell
    // list at large n: `coef`, this formulation's largest-n Count-rule family overall
    // (`tests/Fixtures/dispersion.approved.txt`: n_bar 10983.2, `rho_hat` 6.257E-6, the same cell decision VII's
    // own non-degeneracy test already measured), and `fqdokkarm(20,:)`, its largest-n `fqdokkarm` row and a much
    // more overdispersed one (`phi` 31.86 against `coef`'s 7.65) — a second, harsher case in case `coef`'s own
    // reading turns out to be the easy one. `alpha = 0.05` is where HMX's Count row still violates
    // (`tests/Harness/HISTORY.md`'s own re-read table).
    [Trait("Category", "Long")]
    [Theory]
    [InlineData("coef")]
    [InlineData("fqdokkarm(20,:)")]
    public void Item1FollowupHmxIntervalWidthAgainstBinomialAtLargestN(string arrayName)
    {
        const string formulation = "HMX";
        const double alpha = 0.05;
        var replicaCount = ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount;
        var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
        for (var k = 1; k <= replicaCount; k++)
        {
            replicaCells.Add(ResultsMFile.ParseCells(
                RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
        }

        var totals = CountReconstruction.ArrayTotals(replicaCells, arrayName);
        var dispersion = DispersionEstimator.Estimate(replicaCells, arrayName, totals);
        _ = Assert.NotNull(dispersion);
        var rhoHat = dispersion!.Value.RhoHat;
        Assert.True(rhoHat > 0.0, $"expected HMX {arrayName}'s rho_hat > 0 (beta-binomial route), got {rhoHat:G6}.");

        var perIndex = CollectPerIndexCounts(arrayName, replicaCells);
        Assert.True(perIndex.Count > 10, $"expected {arrayName}'s own well-populated index range, got {perIndex.Count} cells.");

        var rows = perIndex
            .Select(kv =>
            {
                var (ks, ns) = kv.Value;
                var sumN = ns.Sum();
                var pHat = ks.Sum() / sumN;
                var nBar = ns.Average();
                return (Index: kv.Key, NBar: nBar, PHat: pHat);
            })
            .Where(r => r.PHat is > 0.0 and < 1.0)
            .OrderByDescending(r => r.NBar)
            .Take(10)
            .ToList();

        Assert.True(rows.Count > 0, $"expected at least one HMX {arrayName} cell with a usable p_hat.");

        _output.WriteLine($"HMX {arrayName}, rho_hat={rhoHat:G6} (rho_hat_spread={dispersion.Value.RhoHatSpread:G6}), alpha={alpha}:");
        foreach (var (index, nBar, pHat) in rows)
        {
            var n = (long)Math.Round(nBar);
            var (_, actualLow, actualHigh, _) = BetaBinomialPredictive.Interval(n, pHat, rhoHat, alpha);
            var (_, binomialLow, binomialHigh, _) = BetaBinomialPredictive.Interval(n, pHat, 0.0, alpha);
            var actualWidth = actualHigh - actualLow;
            var binomialWidth = binomialHigh - binomialLow;
            var ratio = binomialWidth > 0.0 ? actualWidth / binomialWidth : double.NaN;

            _output.WriteLine(
                $"  {arrayName}[{index}]: n={n} p_hat={pHat:G6} actual=[{actualLow},{actualHigh}] (width={actualWidth}) " +
                $"binomial=[{binomialLow},{binomialHigh}] (width={binomialWidth}) ratio={ratio:G4}");
        }
    }

    // `tests/Harness/HISTORY.md#decision-viii-full-reasoning`: before any stratified estimator is
    // written, the cheap check the decision itself demands — does `rho_cell` genuinely vary with a cell's own
    // `n_bar` across the `fqdokkarm` family's size deciles, or is the decline decision VII's own item 1 measured
    // (0.216 at the first decile, 0.031 at the tenth) sampling noise that a stratified estimator would just be
    // fitting? The strata are exactly item 1's own deciles (`CollectPerIndexCounts`, the same per-cell (k_i, n_i)
    // reconstruction, pooled across every `fqdokkarm` row and every reference formulation — no new binning, per
    // the decision's own text), and `rho_cell` is `CellRho.Compute`'s own UNTRUNCATED value —
    // decision VIII's "one change" from decision VII's settled formula: the median is taken over the untruncated
    // values and `Math.Max(0, .)` applied once, to the pooled (here: resampled) result, not to each cell first.
    // Each decile is bootstrapped 1000 times (resampling its own cells with replacement, a fixed seed per root
    // BOOT.md Taboos: no clock-seeded generator), and the reported figures are the resulting distribution's own
    // median and 5th/95th percentiles — read per decision VIII's own criterion: the deciles' intervals overlapping
    // across the whole range withdraws the decision; the extremes' own intervals staying disjoint confirms it.
    [Trait("Category", "Long")]
    [Fact]
    // Renamed for CA1707 2026-09-24 (owner's analyzer task, coding mode across the tree): a prior session left
    // this one test unrenamed with the 2026-09-24 CA1707 pass because tests/Harness/StatisticalCriterion.cs cited
    // it and was then frozen byte-for-byte to protect tests/Fixtures/rate-table.json's CriterionSha256 tie, and
    // the tie's regeneration (tests/Simulation.Tests/RateTableGenerator.Regenerate) reproducibly crashed the test
    // host in that session. That constraint no longer holds here: StatisticalCriterion.cs already changed hash
    // this task (CA1859/CA1062 fixes), the rate table is regenerated once as this task's own step (moved to a
    // console tool, not a test), and this task has write access to every node the citation touches - so the
    // rename and its citation move together in one commit, and nothing is left dangling.
    public void DecisionVIIIBootstrapCheckFqDokKarmRhoCellByDecile()
    {
        const int BootstrapResamples = 1000;
        const int BootstrapSeed = 20260920;

        var cells = new List<CellRho.CellRhoEstimate>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var dokRowNames = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var cellsOfReplica in replicaCells)
            {
                foreach (var key in cellsOfReplica.Keys)
                {
                    if (key.StartsWith("fqdokkarm(", StringComparison.Ordinal))
                    {
                        _ = dokRowNames.Add(key);
                    }
                }
            }

            foreach (var rowName in dokRowNames)
            {
                foreach (var (_, (ks, ns)) in CollectPerIndexCounts(rowName, replicaCells))
                {
                    if (CellRho.Compute(ks, ns) is { } cell)
                    {
                        cells.Add(cell);
                    }
                }
            }
        }

        Assert.True(cells.Count >= 20 * 10, $"expected at least 20 eligible cells per decile, got {cells.Count} total.");

        var sorted = cells.OrderBy(c => c.NBar).ToList();
        var n = sorted.Count;
        var rng = new SplitMix64(BootstrapSeed);

        _output.WriteLine($"fqdokkarm, eligible cells: {n}, n_bar range [{sorted[0].NBar:G6}, {sorted[^1].NBar:G6}]");
        _output.WriteLine($"bootstrap: {BootstrapResamples} resamples per decile, seed {BootstrapSeed}");
        _output.WriteLine("");

        var decileMedians = new List<(int Decile, double Median, double P05, double P95)>();

        for (var d = 0; d < 10; d++)
        {
            var start = d * n / 10;
            var end = (d + 1) * n / 10;
            var slice = sorted.GetRange(start, end - start);
            var rhoCells = slice.Select(c => c.RhoCell).ToList();

            var bootstrapMedians = new List<double>(BootstrapResamples);
            for (var b = 0; b < BootstrapResamples; b++)
            {
                var resample = new List<double>(rhoCells.Count);
                for (var i = 0; i < rhoCells.Count; i++)
                {
                    resample.Add(rhoCells[rng.Next(rhoCells.Count)]);
                }

                bootstrapMedians.Add(Math.Max(0.0, Median(resample)));
            }

            bootstrapMedians.Sort();
            var median = Median(bootstrapMedians);
            var p05 = CellRho.Quantile(bootstrapMedians, 0.05);
            var p95 = CellRho.Quantile(bootstrapMedians, 0.95);
            decileMedians.Add((d + 1, median, p05, p95));

            _output.WriteLine(
                $"decile {d + 1,2}: cells={slice.Count,4} n_bar=[{slice[0].NBar,10:G5}, {slice[^1].NBar,10:G5}] " +
                $"rho_cell median={median,12:G6} p05={p05,12:G6} p95={p95,12:G6}");
        }

        var first = decileMedians[0];
        var last = decileMedians[^1];
        var disjoint = first.P95 < last.P05 || last.P95 < first.P05;
        _output.WriteLine("");
        _output.WriteLine(
            $"decile 1 vs decile 10: [{first.P05:G6}, {first.P95:G6}] vs [{last.P05:G6}, {last.P95:G6}] " +
            $"-> {(disjoint ? "disjoint (decision VIII's own criterion to proceed)" : "overlapping (decision VIII withdrawn)")}");
    }

    private void ReportDeciles(string label, List<CellPoint> points)
    {
        if (points.Count < 10)
        {
            _output.WriteLine($"{label,-14}: only {points.Count} cells, too few for deciles.");
            return;
        }

        var sorted = points.OrderBy(p => p.NBar).ToList();
        var n = sorted.Count;
        var line = new List<string> { $"{label} (N={n}, n_bar range [{sorted[0].NBar:G4}, {sorted[^1].NBar:G4}]):" };
        for (var d = 0; d < 10; d++)
        {
            var start = d * n / 10;
            var end = (d + 1) * n / 10;
            if (end <= start)
            {
                continue;
            }

            var slice = sorted.GetRange(start, end - start);
            var medianNBar = Median(slice.Select(p => p.NBar).ToList());
            var medianE = Median(slice.Select(p => p.E).ToList());
            line.Add($"  decile {d + 1,2}: n={slice.Count,5} median(n_bar)={medianNBar,10:G5} median(e)={medianE,12:G6}");
        }

        foreach (var l in line)
        {
            _output.WriteLine(l);
        }
    }

    private static double Median(List<double> values)
    {
        values.Sort();
        var mid = values.Count / 2;
        return values.Count % 2 == 0 ? (values[mid - 1] + values[mid]) / 2.0 : values[mid];
    }
}
