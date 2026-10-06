using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Fable 5.1 decision IV ("the count predictive after the p/q defect"), item 1: before replacing the negative-
/// binomial count predictive with a beta-binomial one, measure which family the replica populations actually
/// belong to. For each count-like family's own cell, across a formulation's own lagged replicas, this
/// reconstructs each replica's own integer count via the same per-run quantum inference <see cref="StatisticalCriterion.Compare"/>
/// itself uses (<see cref="RunQuantum.TryInfer"/> — one formula, reused, never a second
/// implementation of it for a one-off diagnostic), then compares the empirical index of dispersion
/// <c>Var(k_j)/Mean(k_j)</c> against the binomial value <c>1 - phat_j</c> (<c>phat_j = Mean(k_j)/Mean(n_i)</c>,
/// the average share of that run's own total landing in bin <c>j</c>) and the Poisson value <c>1</c>, pooled by
/// family. tests/Harness/BOOT.md, "Fable 5.1 decision IV", has this test's own measured numbers.
/// </summary>
public class DispersionDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public DispersionDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // The count-like distribution-function families this node's own `DistributionFunctionFamilies`
    // (StatisticalCriterion.cs) names, plus `fqdokkarm`'s own per-row family, discovered by name pattern below.
    private static readonly string[] FixedFamilies = ["fqkarm", "fqkarm_cor", "fqmkm1", "fqmkm2", "coef"];

    private const int MinimumContributingRuns = 4;

    [Trait("Category", "Long")]
    [Fact]
    public void DispersionByFamilyVarianceToMeanRatioAgainstBinomialAndPoisson()
    {
        var lines = new List<string>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt");
                replicaCells.Add(ResultsMFile.ParseCells(path));
            }

            // Family names actually printed by this formulation: the fixed list, plus every "fqdokkarm(<row>,:)"
            // key any of its replicas happens to hold.
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

            foreach (var family in FixedFamilies.Concat(dokRowNames))
            {
                MeasureOneFamily(formulation, family, replicaCells, lines);
            }
        }

        foreach (var line in lines)
        {
            _output.WriteLine(line);
        }

        Assert.True(lines.Count > 0, "expected at least one family/formulation combination with enough data to measure dispersion.");
    }

    private static void MeasureOneFamily(
        string formulation, string family, IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, List<string> lines)
    {
        // Per contributing replica: reconstructed integer counts by array index, and this run's own total (the
        // sum of every resolved count) — the run's own printed/design-fixed total this family shares across
        // every one of its bins, `tests/Harness/HISTORY.md#fable-5-1-decision-iv`.
        var perReplicaCounts = new List<Dictionary<int, double>>();
        var totals = new List<double>();
        foreach (var cells in replicaCells)
        {
            if (!cells.TryGetValue(family, out var array) || !RunQuantum.TryInfer(array, out var estimate))
            {
                continue;
            }

            var counts = new Dictionary<int, double>();
            var total = 0.0;
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
                    continue; // unresolvable at this run's own quantum: excluded from the index and the total alike.
                }

                var count = Math.Round(Math.Abs(cell.Value) / estimate.Quantum);
                counts[j] = count;
                total += count;
            }

            perReplicaCounts.Add(counts);
            totals.Add(total);
        }

        if (perReplicaCounts.Count < MinimumContributingRuns)
        {
            return; // too few runs determined a quantum for this family to say anything about its own dispersion.
        }

        var meanTotal = totals.Average();
        if (meanTotal <= 0.0)
        {
            return;
        }

        var maxIndex = 0;
        foreach (var counts in perReplicaCounts)
        {
            foreach (var index in counts.Keys)
            {
                maxIndex = Math.Max(maxIndex, index);
            }
        }

        var ratios = new List<double>();
        var targets = new List<double>();
        for (var j = 0; j <= maxIndex; j++)
        {
            var values = new List<double>();
            foreach (var counts in perReplicaCounts)
            {
                if (counts.TryGetValue(j, out var k))
                {
                    values.Add(k);
                }
            }

            if (values.Count < MinimumContributingRuns)
            {
                continue;
            }

            var mean = values.Average();
            if (mean <= 0.0)
            {
                continue;
            }

            var sumOfSquares = 0.0;
            foreach (var v in values)
            {
                sumOfSquares += (v - mean) * (v - mean);
            }

            var variance = sumOfSquares / (values.Count - 1);
            var phat = mean / meanTotal;
            ratios.Add(variance / mean);
            targets.Add(1.0 - phat);
        }

        if (ratios.Count == 0)
        {
            return;
        }

        var medianRatio = Median(ratios);
        var medianTarget = Median(targets);
        var regime =
            medianRatio >= medianTarget * 0.9 && medianRatio <= medianTarget * 1.1 ? "AT (1-phat): beta-binomial fits" :
            medianRatio > medianTarget && medianRatio < 1.0 ? "BETWEEN (1-phat) and 1: beta-binomial still better" :
            medianRatio >= 1.0 ? "AT/ABOVE 1: overdispersed beyond Poisson too" :
            "BELOW (1-phat): neither family fits without an explicit dispersion parameter";

        lines.Add(
            $"{formulation,-8} {family,-18} runs={perReplicaCounts.Count,3} cells={ratios.Count,4} " +
            $"median(Var/Mean)={medianRatio:G4} median(1-phat)={medianTarget:G6} -> {regime}");
    }

    private static double Median(List<double> values)
    {
        var sorted = new List<double>(values);
        sorted.Sort();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }

    // Fable 5.1 decision IV, item 2: HMX's own gate-1 red cell, `fqdokkarm(31,:)[7]` -- the computation, not the
    // verdict. `Compare`'s own canonical-axis rule (this node's BOOT.md, ## Invariants) restricts row 31's own
    // contributing replicas to those whose Dkarmcat agrees with the reference's, bit-exact, through physical
    // index 30 (row 31, 1-based); this reproduces that same restriction directly against the raw fixture files
    // (not a second implementation of a formula -- an exact-equality prefix check, not the statistical rule
    // itself), to get every contributing replica's own row-31 total (`n_i`, the sum of its own reconstructed
    // counts across the whole row) and the reference's own (`n*`), then the beta-binomial tail
    // `P(K >= 11 | n*, Beta(1/2 + sum k_i, 1/2 + sum(n_i - k_i)))` Fable's own decision IV, item 2 asks for.
    [Trait("Category", "Long")]
    [Fact]
    public void GateOneRedCellHmxFqDokKarm31Index7BetaBinomialComputation()
    {
        const string formulation = "HMX";
        const string rowName = "fqdokkarm(31,:)";
        const int rowIndex0 = 30; // 1-based "31" minus 1, this node's own FqDokKarmRow convention.
        const int columnIndex = 7;

        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var referenceCells = ResultsMFile.ParseCells(referencePath);
        var referenceDkarmcat = referenceCells["Dkarmcat"];

        var (formulationName, replicaCount) = ResultsMFileTests.Formulations.Single(f => f.Name == formulation);
        Assert.Equal(formulation, formulationName);

        var contributingReplicaTotals = new List<double>();
        var contributingReplicaK = new List<double>();
        var contributingReplicaOrdinals = new List<int>();
        for (var k = 1; k <= replicaCount; k++)
        {
            var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt");
            var cells = ResultsMFile.ParseCells(path);
            var replicaDkarmcat = cells.TryGetValue("Dkarmcat", out var d) ? d : [];

            // Bit-exact prefix match, this node's own "Canonical category axis" rule: how many leading entries
            // agree exactly, capped at the reference's own length.
            var matchLength = 0;
            while (matchLength < replicaDkarmcat.Length && matchLength < referenceDkarmcat.Length &&
                   replicaDkarmcat[matchLength].Value == referenceDkarmcat[matchLength].Value)
            {
                matchLength++;
            }

            if (matchLength <= rowIndex0 || !cells.TryGetValue(rowName, out var row))
            {
                continue;
            }

            if (!RunQuantum.TryInfer(row, out var estimate))
            {
                continue;
            }

            var total = 0.0;
            var kAtColumn = 0.0;
            for (var j = 0; j < row.Length; j++)
            {
                if (Math.Abs(row[j].Value) <= 0.0)
                {
                    continue;
                }

                if (estimate.Quantum <= row[j].Resolution)
                {
                    continue;
                }

                var count = Math.Round(Math.Abs(row[j].Value) / estimate.Quantum);
                total += count;
                if (j == columnIndex)
                {
                    kAtColumn = count;
                }
            }

            contributingReplicaTotals.Add(total);
            contributingReplicaK.Add(kAtColumn);
            contributingReplicaOrdinals.Add(k);
        }

        Assert.True(contributingReplicaTotals.Count >= 2, $"expected at least 2 contributing replicas, found {contributingReplicaTotals.Count}.");

        var referenceTotal = 0.0;
        var referenceK = 0.0;
        if (referenceCells.TryGetValue(rowName, out var referenceRow) && RunQuantum.TryInfer(referenceRow, out var referenceEstimate))
        {
            for (var j = 0; j < referenceRow.Length; j++)
            {
                if (Math.Abs(referenceRow[j].Value) <= 0.0 || referenceEstimate.Quantum <= referenceRow[j].Resolution)
                {
                    continue;
                }

                var count = Math.Round(Math.Abs(referenceRow[j].Value) / referenceEstimate.Quantum);
                referenceTotal += count;
                if (j == columnIndex)
                {
                    referenceK = count;
                }
            }
        }

        var sumK = contributingReplicaK.Sum();
        var sumN = contributingReplicaTotals.Sum();
        var a = 0.5 + sumK;
        var b = 0.5 + (sumN - sumK);

        var tail = BetaBinomialUpperTail((long)referenceTotal, (long)Math.Round(referenceK), a, b);

        _output.WriteLine(
            $"contributing replicas: [{string.Join(", ", contributingReplicaOrdinals)}]");
        _output.WriteLine(
            $"n_i (row totals): [{string.Join(", ", contributingReplicaTotals)}], k_i (column {columnIndex}): [{string.Join(", ", contributingReplicaK)}]");
        _output.WriteLine($"sum n_i={sumN}, sum k_i={sumK}, a={a}, b={b}");
        _output.WriteLine($"reference: n*={referenceTotal}, k*={referenceK}");
        _output.WriteLine($"P(K >= {referenceK} | n*={referenceTotal}, Beta({a}, {b})) = {tail:G6}");

        Assert.True(contributingReplicaTotals.Count > 0);
    }

    // P(K >= k | n, a, b) for K ~ BetaBinomial(n, a, b), via the log-gamma primitive this node's own
    // NegativeBinomialInterval/BinomialBand already use (root BOOT.md Taboos: "no second implementation of any
    // part of ... a formula" -- this reuses StudentDistribution.LogGamma, not a rewritten log-gamma).
    private static double BetaBinomialUpperTail(long n, long k, double a, double b)
    {
        if (k <= 0)
        {
            return 1.0;
        }

        var logBetaAB = StudentDistribution.LogGamma(a) + StudentDistribution.LogGamma(b) - StudentDistribution.LogGamma(a + b);
        var tail = 0.0;
        for (var i = k; i <= n; i++)
        {
            var logC = StudentDistribution.LogGamma(n + 1) - StudentDistribution.LogGamma(i + 1) - StudentDistribution.LogGamma(n - i + 1);
            var logTerm = logC + StudentDistribution.LogGamma(i + a) + StudentDistribution.LogGamma(n - i + b)
                - StudentDistribution.LogGamma(n + a + b) - logBetaAB;
            tail += Math.Exp(logTerm);
        }

        return tail;
    }

    // Fable 5.1 decision V ("the dispersion diagnostic was unconditional"): the raw Var(k)/Mean(k) measure of
    // decision IV mixed a run's own varying total n_i into the reading. Re-measured conditionally: p_hat =
    // sum(k_i)/sum(n_i); phi = (1/(R-1)) * sum((k_i - n_i*p_hat)^2 / (n_i*p_hat*(1-p_hat))), pooled by family and
    // configuration (formulation), with cells whose own mean n_i*p_hat < 5 merged with their neighbours first
    // (the tail-pooling rule, brought forward for this diagnostic's own sake). phi ~ 1 -> binomial (rho = 0);
    // phi > 1 -> beta-binomial with rho_hat = (phi-1)/(n_bar-1); phi < 1 -> the family does not fit a count
    // predictive at all and the cell takes the ordinary Student band on its own reconstructed count instead
    // (decision V, item 3). tests/Harness/BOOT.md, "Fable 5.1 decision V", has this test's own measured numbers.
    [Trait("Category", "Long")]
    [Fact]
    public void ConditionalDispersionByFamilyPhiAndRhoHat()
    {
        var lines = new List<string>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var rd) ? rd : [];

            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            var replicaDkarmcatMatch = new List<int>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt");
                var cells = ResultsMFile.ParseCells(path);
                replicaCells.Add(cells);
                var replicaDkarmcat = cells.TryGetValue("Dkarmcat", out var d) ? d : [];
                var matchLength = 0;
                while (matchLength < replicaDkarmcat.Length && matchLength < referenceDkarmcat.Length &&
                       replicaDkarmcat[matchLength].Value == referenceDkarmcat[matchLength].Value)
                {
                    matchLength++;
                }

                replicaDkarmcatMatch.Add(matchLength);
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
                MeasureConditionalOneFamily(formulation, family, replicaCells, null, lines);
            }

            foreach (var rowName in dokRowNames)
            {
                var rowIndex0 = int.Parse(rowName[10..^3], System.Globalization.CultureInfo.InvariantCulture) - 1;
                var eligible = new List<IReadOnlyDictionary<string, ResultCell[]>>();
                for (var r = 0; r < replicaCells.Count; r++)
                {
                    if (replicaDkarmcatMatch[r] > rowIndex0)
                    {
                        eligible.Add(replicaCells[r]);
                    }
                }

                MeasureConditionalOneFamily(formulation, rowName, eligible, "fqdokkarm", lines);
            }
        }

        foreach (var line in lines)
        {
            _output.WriteLine(line);
        }

        Assert.True(lines.Count > 0, "expected at least one family/formulation combination with enough data.");
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the phi/rho computation itself moved into
    // DispersionEstimator.Estimate (the same wiring BuildComparePending now calls in production) —
    // this test only reconstructs each replica's own totals (CountReconstruction.ArrayTotals, the
    // same shared reconstruction) and formats the result line, never a second implementation of the formula.
    private static void MeasureConditionalOneFamily(
        string formulation, string arrayName, IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells,
        string? poolLabel, List<string> lines)
    {
        var totals = CountReconstruction.ArrayTotals(replicaCells, arrayName);
        var estimate = DispersionEstimator.Estimate(replicaCells, arrayName, totals);
        if (estimate is not { } d)
        {
            return;
        }

        var route = d.Phi < 1.0 ? "STUDENT-ON-COUNT (phi < 1)" : $"BETA-BINOMIAL rho_hat={d.RhoHat:G4}";
        var label = poolLabel is null ? arrayName : $"{poolLabel}:{arrayName}";
        lines.Add(
            $"{formulation,-8} {label,-20} runs={d.Runs,3} groups={d.Groups,4} n_bar={d.NBar:G6} " +
            $"phi={d.Phi:G4} -> {route}");
    }

    // `tests/Harness/HISTORY.md#decision-vii-full-reasoning`, item 2: a direct, named regression guard
    // that the robust per-cell median estimator is actually engaged (RhoHatSpread non-null) and reads far below
    // the old phi-derived formula for HMX's own `coef` — the family item 1's own excess-variance check found
    // showing essentially zero excess variance at its largest cells (n_bar ~ 10983) despite phi reading well
    // above 1 (phi biased upward by coef's numerous small cells, `tests/Harness/HISTORY.md#decision-vii-full-reasoning`). Proven
    // non-degenerate by the size of the move itself: the old formula, `max(0, (phi - 1) / (n_bar - 1))`, computed
    // directly here from the SAME phi/n_bar this estimate reports, is three orders of magnitude larger than the
    // new estimate — a check that could not tell the two formulas apart would not catch this.
    [Trait("Category", "Long")]
    [Fact]
    public void RobustRhoEstimateHmxCoefReadsFarBelowTheOldPhiDerivedFormula()
    {
        const string formulation = "HMX";
        const string arrayName = "coef";
        var (_, replicaCount) = ResultsMFileTests.Formulations.Single(f => f.Name == formulation);

        var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
        for (var k = 1; k <= replicaCount; k++)
        {
            var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt");
            replicaCells.Add(ResultsMFile.ParseCells(path));
        }

        var totals = CountReconstruction.ArrayTotals(replicaCells, arrayName);
        var estimate = DispersionEstimator.Estimate(replicaCells, arrayName, totals);
        _ = Assert.NotNull(estimate);
        var d = estimate!.Value;

        Assert.True(d.Phi >= 1.0, $"expected coef's own phi to read above 1 (the beta-binomial regime), got {d.Phi}.");
        _ = Assert.NotNull(d.RhoHatSpread); // the robust estimator engaged; the old-formula fallback was not used.

        var oldFormulaRhoHat = Math.Max(0.0, (d.Phi - 1.0) / (d.NBar - 1.0));
        Assert.True(
            d.RhoHat < oldFormulaRhoHat / 50.0,
            $"expected the robust estimate ({d.RhoHat:G6}) to read well below the old phi-derived formula " +
            $"({oldFormulaRhoHat:G6}, measured ratio {oldFormulaRhoHat / d.RhoHat:G4}x); it did not.");
    }
}
