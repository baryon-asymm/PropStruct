using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// The evidence test for the epsdokfr exclusion the design session proposed (2026-09-27): PSAN02n's own
/// <c>epsdokfr(1)</c> would be dropped whole against <c>ReplicaKind.Independent</c> only, the same shape
/// <c>tests/Fixtures/exclusions.json</c>'s own <c>epsx(4)</c> entry already uses, if a trend fixed before this
/// task looked at the sixteen replicas' own values held up. It does not: the primary statistic below does not
/// clear the tree's own alpha, so this task adds no exclusions.json entry, per the coordinator's own ruling on
/// this evidence (recorded here and in <c>tests/Harness/HISTORY.md</c>).
///
/// The primary statistic is fixed before any of this task's own measurement: root BOOT.md's own 2026-09-23
/// entry and <c>tests/Harness/HISTORY.md#precision-kind-hmx-multi-seed</c> already claim <c>epsdokfr(1)</c>
/// "drifts smoothly ... with the replica number" — a trend on the replica ordinal, tested here by the Spearman
/// rank correlation of the value against its own 1-based ordinal, over the sixteen independent replicas, with a
/// two-sided permutation p from 20,000 seeded shuffles of the ordinal. The same statistic on the sixteen lagged
/// replicas is the negative control: no rigid-shift mechanism is claimed for that layout, so no trend is
/// expected there either.
/// </summary>
public class ReplicaArcEvidenceTests
{
    private const string Formulation = "PSAN02n";
    private const string Quantity = "epsdokfr";
    private const int Index = 0;
    private const int Permutations = 20_000;

    [Fact]
    public void IndependentEpsdokfrTrendDoesNotClearTheTreesAlpha()
    {
        var values = LoadValues(ReplicaKind.Independent);
        var (rho, pValue) = SpearmanTrendPermutationTest(values, seed: 0x5eed1);

        // Reported, not gated (design §7 item 4's own fallback, since the primary test fails at alpha): the
        // trend claimed for epsdokfr(1) does not clear the tree's own significance level over these sixteen
        // replicas, so this task adds no exclusions.json entry for it. CompareSets's own subset check therefore
        // gates PSAN02n's lagged layout only and reports (never asserts) the independent one
        // (tests/Harness.Tests/SetCriterionTests.cs).
        Assert.True(
            pValue >= StatisticalCriterion.Alpha,
            $"expected the claimed trend NOT to clear the tree's own alpha (it does not warrant the exclusion), " +
            $"measured rho={rho:G6}, p={pValue:G6} against {Permutations} permutations.");
    }

    [Fact]
    public void LaggedEpsdokfrTrendIsAlsoNotSignificant()
    {
        var values = LoadValues(ReplicaKind.Lagged);
        var (rho, pValue) = SpearmanTrendPermutationTest(values, seed: 0x5eed2);

        Assert.True(
            pValue >= StatisticalCriterion.Alpha,
            $"the lagged layout carries no claimed rigid-shift mechanism, so no trend is expected here either, " +
            $"measured rho={rho:G6}, p={pValue:G6}.");
    }

    private static double[] LoadValues(ReplicaKind kind)
    {
        var replicaCount = FixtureReplicas.CountOf(Formulation);
        var replicas = FixtureReplicas.LoadReplicas(Formulation, kind, replicaCount);
        return [.. replicas.Select(r => r[Quantity][Index])];
    }

    // A permutation test on the ordinal order: reshuffle `values` (whose natural order is the replica ordinal)
    // `Permutations` times and count how many shuffles reach an equally or more extreme |Spearman rho| between
    // the (shuffled) values and the fixed ordinal 1..n than the observed, unshuffled order does. `SplitMix64`
    // (this node's own API.md, "Deterministic test-only randomness"), not `System.Random` (root BOOT.md Taboos:
    // no clock-seeded generator; CA5394 flags a seeded one too).
    private static (double Rho, double PValue) SpearmanTrendPermutationTest(double[] values, long seed)
    {
        var ordinalRanks = Ranks([.. Enumerable.Range(1, values.Length).Select(i => (double)i)]);
        var observed = Math.Abs(SpearmanRho(values, ordinalRanks));

        var random = new SplitMix64(seed);
        var extreme = 0;
        var shuffled = (double[])values.Clone();
        for (var trial = 0; trial < Permutations; trial++)
        {
            Shuffle(shuffled, random);
            if (Math.Abs(SpearmanRho(shuffled, ordinalRanks)) >= observed)
            {
                extreme++;
            }
        }

        var pValue = (double)(extreme + 1) / (Permutations + 1);
        return (observed, pValue);
    }

    // Spearman's rho: the Pearson correlation of the two rank sequences. `ordinalRanks` is precomputed once
    // (the ordinal 1..n is never shuffled, only `values` is), so every permutation ranks only `values` itself.
    private static double SpearmanRho(double[] values, double[] ordinalRanks)
    {
        var valueRanks = Ranks(values);
        var n = valueRanks.Length;
        var meanA = valueRanks.Average();
        var meanB = ordinalRanks.Average();
        var numerator = 0.0;
        var sumSquaresA = 0.0;
        var sumSquaresB = 0.0;
        for (var i = 0; i < n; i++)
        {
            var deviationA = valueRanks[i] - meanA;
            var deviationB = ordinalRanks[i] - meanB;
            numerator += deviationA * deviationB;
            sumSquaresA += deviationA * deviationA;
            sumSquaresB += deviationB * deviationB;
        }

        var denominator = Math.Sqrt(sumSquaresA * sumSquaresB);
        return denominator == 0.0 ? 0.0 : numerator / denominator;
    }

    // 1-based ranks, averaged over ties (the standard Spearman tie-breaking rule).
    private static double[] Ranks(double[] values)
    {
        var order = Enumerable.Range(0, values.Length).OrderBy(i => values[i]).ToArray();
        var ranks = new double[values.Length];
        var i = 0;
        while (i < order.Length)
        {
            var j = i;
            while (j + 1 < order.Length && values[order[j + 1]] == values[order[i]])
            {
                j++;
            }

            var averageRank = (i + j) / 2.0 + 1;
            for (var k = i; k <= j; k++)
            {
                ranks[order[k]] = averageRank;
            }

            i = j + 1;
        }

        return ranks;
    }

    private static void Shuffle(double[] values, SplitMix64 random)
    {
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
