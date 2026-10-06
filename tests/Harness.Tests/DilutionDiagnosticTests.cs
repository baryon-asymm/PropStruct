using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// `tests/Harness/HISTORY.md#decision-vi-full-reasoning`, item 5: the two cheap checks run before any
/// recomputation of the curve, over the pooled leave-one-out population at <c>alpha = 0.05</c> (the level
/// carrying the clearest, best-populated violations in the curve), restricted throughout to eligible,
/// non-degenerate <see cref="CalibrationRule.Count"/> cells (item 1's own second fact — the
/// Count rule's shortfall — is what these checks address; the Student rule's own dilution from the routed
/// <c>fqmkm1</c>/<c>fqmkm2</c> cells is already directly visible in the plain before/after N and K, needing no
/// further statistic to see it).
/// </summary>
public class DilutionDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public DilutionDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private const double Alpha = 0.05;

    [Trait("Category", "Long")]
    [Fact]
    public void Item5ObservedAgainstAttainedSumAndTailProbabilityUniformityOverEligibleCountCells()
    {
        long compared = 0;
        long failures = 0;
        var attainedAlphaSum = 0.0;
        var tailProbabilities = new List<double>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            for (var k = 1; k <= replicaCount; k++)
            {
                var candidate = ResultsMFile.Parse(ReplicaPath(formulation, k));
                var verdicts = new List<CellVerdict>();
                verdicts.AddRange(StatisticalCriterion.CompareCellVerdicts(formulation, candidate, ReplicaKind.Lagged, Alpha, excludeReplicaOrdinal: k));
                verdicts.AddRange(StatisticalCriterion.CompareTailRowMeanCellVerdicts(formulation, candidate, ReplicaKind.Lagged, Alpha, excludeReplicaOrdinal: k));
                verdicts.AddRange(StatisticalCriterion.CompareAdaptiveIndexMatchedCellVerdicts(formulation, candidate, ReplicaKind.Lagged, Alpha, excludeReplicaOrdinal: k));

                foreach (var v in verdicts)
                {
                    // `tests/Harness/HISTORY.md#decision-ix-full-reasoning`, item 2's own third
                    // bullet: a `Count`-governed cell with no known attained level is its own population — it
                    // cannot contribute to `failures`/`compared` here any more than to `attainedAlphaSum`, or the
                    // ratio below compares an observed count that includes it against a sum of levels that does
                    // not.
                    if (v.Rule != CalibrationRule.Count || v.Degenerate || !v.Eligible || v.AttainedAlpha is null)
                    {
                        continue;
                    }

                    compared++;
                    if (v.Failed)
                    {
                        failures++;
                    }

                    if (v.AttainedAlpha is { } attained)
                    {
                        attainedAlphaSum += attained;
                    }

                    if (v.TailProbability is { } p)
                    {
                        tailProbabilities.Add(p);
                    }
                }
            }
        }

        Assert.True(compared > 1000, $"expected a well-populated eligible Count population, got {compared}.");
        Assert.True(tailProbabilities.Count > 1000, $"expected a well-populated tail-probability sample, got {tailProbabilities.Count}.");

        // Check A (decision VI, item 5, first bullet): observed reports against the sum of per-cell attained
        // levels over eligible cells — not against N * nominal, which the attained-level design already knows
        // reads too high for a discrete boundary (`tests/Harness/HISTORY.md#fable-5-1-decision-iii`).
        var ratio = failures / attainedAlphaSum;
        _output.WriteLine(
            $"Check A: eligible N={compared} K={failures} attainedAlphaSum={attainedAlphaSum:G6} " +
            $"(observed / attainedSum = {ratio:G4}; 1.0 means the intervals are right and the gap was dilution, " +
            $"far below 1.0 means the intervals are genuinely too wide even among the cells that could fail).");

        // Check B, re-specified (`tests/Harness/HISTORY.md#decision-ix-full-reasoning`): the earlier
        // form (a mean/KS reading against Uniform(0, 1)) had no null — `CountTwoSidedTailProbability` is a
        // doubled one-sided tail of a discrete law, and under any correct discrete model such a statistic is
        // stochastically LARGER than uniform, not equal to it: `P(T <= u) <= u` for every `u`. Sorted ascending,
        // the bound requires the i-th order statistic (1-based) to satisfy `T_(i) >= i / n`; a violation is where
        // it falls short. Reported as the fraction of order statistics violating the bound (expected to fall once
        // the governing term, not the computed one, decides which cells are Count cells at all — decision IX,
        // item 2) and the standard one-sided Kolmogorov statistic against that same bound,
        // `D+ = max(0, max_i(i/n - T_(i)))`: 0 means the bound is never violated, and a large positive value means
        // the count interval is too NARROW there — the opposite finding from Check A's "too wide overall", both
        // of which are resolved once the term that is not, in fact, a count predictive at all stops being scored
        // as one.
        tailProbabilities.Sort();
        var n = tailProbabilities.Count;

        var violating = 0;
        var dPlus = 0.0;
        for (var i = 0; i < n; i++)
        {
            var u = (i + 1.0) / n;
            var t = tailProbabilities[i];
            if (t < u)
            {
                violating++;
            }

            dPlus = Math.Max(dPlus, u - t);
        }

        var violationFraction = violating / (double)n;
        var median = tailProbabilities[n / 2];

        _output.WriteLine(
            $"Check B (re-specified): N={n} violating P(T<=u)<=u: {violating} ({violationFraction:P2}) " +
            $"one-sided Kolmogorov D+={dPlus:G4} (0 means the bound is never violated) median={median:G4}.");

        // Deciles, to see the shape directly rather than only two summary numbers — each decile's own value must
        // be at least its own rank fraction under the bound (the tenth percentile at least 0.1, the median at
        // least 0.5, and so on).
        var deciles = new double[10];
        for (var d = 0; d < 10; d++)
        {
            deciles[d] = tailProbabilities[Math.Min(n - 1, (int)((d + 1) / 10.0 * n) - 1)];
        }

        _output.WriteLine("Check B deciles (10th..100th percentile): " + string.Join(", ", deciles.Select(d => d.ToString("G4"))));
    }

    private static string ReplicaPath(string formulation, int ordinal) =>
        RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, ordinal + ".m.txt");
}
