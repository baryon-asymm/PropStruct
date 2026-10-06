using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Design (`comparesets/design.md`) §6 step 5, "Re-measure": repeats the design's own "Random half-splits,
/// 6,000 of them" negative control in C#, against the tree's current code and fixture data — a homogeneous
/// candidate set split at random, not by the fixed parity <see cref="SetCriterionTests.SetNullsAreEmpty"/>
/// already checks, so every split is its own, independent draw from the same null <see cref="SetComparison"/>
/// measures elsewhere. <c>tests/Harness/HISTORY.md#set-comparison-design-2026-09-27</c> records this run's own
/// figure next to the design's own pre-E1/E2/F-a figure, explicitly not copied from it (root BOOT.md Taboos,
/// "no expected value ... when it exists in a fixture file" — the analogous rule for a *measurement*: re-run it,
/// never retype it).
/// </summary>
public class RandomSplitNullRateTests(ITestOutputHelper output)
{
    private const int SplitsPerPair = 600;

    private static readonly string[] Layouts = ["Original", "Independent"];

    /// <summary>
    /// Ten gated (formulation, layout) pairs (`SetCriterionTests.GatedFormulationLayoutPairs`, minus nothing here
    /// — the null rate is a property of the split mechanism, not of PSAN02n's own reported-only subset gate), six
    /// hundred random 8-against-8 splits of the sixteen `Original`-precision candidates each, seeded
    /// deterministically (root BOOT.md Taboos: no clock-seeded generator) so the run is exactly reproducible.
    /// Reports the pooled hit rate (at least one differing cell) and a Wilson 95% interval on it — reported, not
    /// gated: this is a calibration measurement of the method's own false-positive rate, not a pass condition
    /// (root BOOT.md Taboos, "a verdict may rest only on a figure of the matching kind").
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void SixThousandRandomCandidateSplitsMeasureTheMethodsOwnFalsePositiveRate()
    {
        var rng = new SplitMix64(seed: 20260927);
        var hits = 0;
        var totalSplits = 0;

        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in Layouts)
            {
                var candidates = RateRuns.Load(formulation, layout, "Original");
                var kind = string.Equals(layout, "Original", StringComparison.Ordinal) ? ReplicaKind.Lagged : ReplicaKind.Independent;
                var (candidateSet, _, _) = SetComparison.PrepareSets(formulation, candidates, kind);

                for (var split = 0; split < SplitsPerPair; split++)
                {
                    var (first, second) = RandomHalves(candidateSet, rng);
                    var (cells, _) = TwoSampleComparison.OfSets(first, second, ConstantCellRule.ExactBound);
                    totalSplits++;
                    if (cells.Any(c => c.Differs))
                    {
                        hits++;
                    }
                }
            }
        }

        var rate = (double)hits / totalSplits;
        var (low, high) = WilsonInterval(hits, totalSplits);
        output.WriteLine(
            $"random half-splits: {totalSplits} (10 pairs x {SplitsPerPair}), hits (>=1 differing cell)={hits}, " +
            $"rate={rate:P4}, Wilson 95% interval=[{low:P4}, {high:P4}].");
        output.WriteLine(
            "design's own pre-E1/E2/F-a figure (not copied, quoted for comparison only): 6,000 splits, 3 hits " +
            "(5e-4); this run's own splits, count and seed are unrelated to that one, so the two numbers are not " +
            "expected to match exactly, only to be of the same order.");

        Assert.Equal(6000, totalSplits);
    }

    // Fisher-Yates over a copy of the indices, then the first half / second half of the shuffled order — the
    // same shuffle shape RateTableTool and the rest of this tree already use for seeded, reproducible sampling.
    private static (List<IReadOnlyDictionary<string, double[]>> First, List<IReadOnlyDictionary<string, double[]>> Second) RandomHalves(
        List<IReadOnlyDictionary<string, double[]>> candidateSet, SplitMix64 rng)
    {
        var indices = new List<int>(candidateSet.Count);
        for (var i = 0; i < candidateSet.Count; i++)
        {
            indices.Add(i);
        }

        for (var i = indices.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var half = candidateSet.Count / 2;
        var first = new List<IReadOnlyDictionary<string, double[]>>(half);
        var second = new List<IReadOnlyDictionary<string, double[]>>(candidateSet.Count - half);
        for (var i = 0; i < indices.Count; i++)
        {
            if (i < half)
            {
                first.Add(candidateSet[indices[i]]);
            }
            else
            {
                second.Add(candidateSet[indices[i]]);
            }
        }

        return (first, second);
    }

    // A standard closed-form (Wilson score) confidence interval for a measured proportion — reporting math, not
    // a second implementation of any formula this tree owns elsewhere (the exact constant-cell bound and the
    // beta-binomial predictive are both distinct, purpose-built formulas; this is a generic textbook interval
    // used only to state this one measurement's own uncertainty, root BOOT.md Taboos, "every figure states what
    // it is").
    private static (double Low, double High) WilsonInterval(int hits, int total)
    {
        const double Z = 1.959963984540054; // two-sided 95% normal quantile.
        double n = total;
        var phat = hits / n;
        var denominator = 1 + Z * Z / n;
        var center = (phat + Z * Z / (2 * n)) / denominator;
        var halfWidth = Z * Math.Sqrt(phat * (1 - phat) / n + Z * Z / (4 * n * n)) / denominator;
        return (center - halfWidth, center + halfWidth);
    }
}
