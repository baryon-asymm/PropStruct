namespace PropStruct.Tests.Harness;

/// <summary>
/// this node's BOOT.md, "## Set comparison": which rule <see cref="TwoSampleComparison.OfSets"/> applies to a
/// cell where both sets are bit-identical within themselves at different values. <see cref="AlwaysDiffers"/> is
/// every caller's behaviour before this rule existed (an infinite <c>|T|</c> always exceeds any finite critical
/// value, so the cell always differs) and stays the default, unchanged for
/// <see cref="StatisticalCriterion.TwoSampleBias"/>/<see cref="StatisticalCriterion.TwoSampleBiasOfSets"/>.
/// <see cref="ExactBound"/> is <see cref="SetComparison"/>'s own rule (this node's BOOT.md, "## Set comparison",
/// "Why the constant-cell bound is valid"): the cell differs only when the exact probability of two constant
/// sets landing on different values by chance, <c>min(g(n1,n2), g(n2,n1))</c>, is itself below the call's own
/// per-quantity alpha — without it, a seed-parity artefact at a rounding boundary reads as a difference no
/// sample size could ever clear.
/// </summary>
internal enum ConstantCellRule
{
    AlwaysDiffers,
    ExactBound,
}

/// <summary>
/// The two-sample bias comparison (root BOOT.md, "Known bias of the original's seeds"; this node's BOOT.md,
/// ## Invariants, "Two-sample bias"): whether two sets of runs disagree with each other, not whether one run
/// agrees with its own replicas — a different question from <see cref="ReferenceComparison"/>'s family, and
/// this class's own reason for being separate. Split out of <c>StatisticalCriterion.TwoSampleBias.cs</c>
/// (decomposition, 2026-09-26); called from the façade's <see cref="StatisticalCriterion.TwoSampleBias"/>/
/// <see cref="StatisticalCriterion.TwoSampleBiasOfSets"/>, which keep the public surface and the argument
/// validation (CA1062), and from <see cref="SetComparison"/> (this node's BOOT.md, "## Set comparison").
/// </summary>
internal static class TwoSampleComparison
{
    // this node's BOOT.md, ## Invariants, "Two-sample bias": loads both replica sets from tests/Fixtures, then
    // hands them to `OfSets` below.
    internal static IReadOnlyList<TwoSampleCell> Bias(string formulation, ReplicaKind first, ReplicaKind second)
    {
        var replicaCount = FixtureReplicas.CountOf(formulation);

        var firstSet = FixtureReplicas.LoadReplicas(formulation, first, replicaCount);
        var secondSet = FixtureReplicas.LoadReplicas(formulation, second, replicaCount);
        return OfSets(firstSet, secondSet).Cells;
    }

    // The former `TwoSampleBiasOfSets` body: a Welch two-sample t-test at every printed cell that both replica
    // sets reach, family-wise corrected over the cells actually compared (the same `alpha = 10^-3` per
    // formulation as `Compare`). Unlike `Compare`, a cell absent from one member of a set simply drops that
    // member from the set's own mean and standard deviation — there is no reference token to fall back on for a
    // print-resolution floor, and none is needed: this is a plain two-sample comparison of two replica sets to
    // each other, not of a candidate against replicas. Returns every compared cell (not only the differing
    // ones, exactly as before this method gained a second return value) alongside the count of index-cells that
    // never reached two contributors on either side (this node's BOOT.md, "## Set comparison": `SetComparison`
    // folds this into its own report's `Excluded`; every pre-existing caller ignores it).
    internal static (IReadOnlyList<TwoSampleCell> Cells, int Excluded) OfSets(
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> firstSet,
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> secondSet,
        ConstantCellRule rule = ConstantCellRule.AlwaysDiffers)
    {
        var quantityNames = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var replica in firstSet.Concat(secondSet))
        {
            quantityNames.UnionWith(replica.Keys);
        }

        var pending = new List<(string Name, int Index, double Mean1, double Sd1, int N1, double Mean2, double Sd2, int N2)>();
        var excluded = 0;
        foreach (var name in quantityNames)
        {
            var maxLength = 0;
            foreach (var replica in firstSet.Concat(secondSet))
            {
                if (replica.TryGetValue(name, out var array) && array.Length > maxLength)
                {
                    maxLength = array.Length;
                }
            }

            for (var index = 0; index < maxLength; index++)
            {
                var values1 = ValuesAt(firstSet, name, index);
                var values2 = ValuesAt(secondSet, name, index);
                if (values1.Length < 2 || values2.Length < 2)
                {
                    excluded++;
                    continue;
                }

                var (mean1, sd1) = SampleSpread.MeanAndSampleStandardDeviation(values1);
                var (mean2, sd2) = SampleSpread.MeanAndSampleStandardDeviation(values2);
                pending.Add((name, index, mean1, sd1, values1.Length, mean2, sd2, values2.Length));
            }
        }

        var m = pending.Count;
        var perQuantityAlpha = StatisticalCriterion.Alpha / Math.Max(m, 1);
        var quantileCache = new Dictionary<int, double>();
        double TQuantile(int df) =>
            quantileCache.TryGetValue(df, out var cached) ? cached : quantileCache[df] = StudentDistribution.TwoSidedQuantile(df, perQuantityAlpha);

        var results = new List<TwoSampleCell>(m);
        foreach (var cell in pending)
        {
            var standardError = Math.Sqrt(cell.Sd1 * cell.Sd1 / cell.N1 + cell.Sd2 * cell.Sd2 / cell.N2);
            double t;
            int df;
            bool differs;
            if (standardError == 0.0)
            {
                df = Math.Min(cell.N1, cell.N2) - 1;
                if (cell.Mean1 == cell.Mean2)
                {
                    t = 0.0;
                    differs = false;
                }
                else
                {
                    t = double.PositiveInfinity * Math.Sign(cell.Mean1 - cell.Mean2);
                    differs = rule != ConstantCellRule.ExactBound || ConstantMismatchBound(cell.N1, cell.N2) < perQuantityAlpha;
                }
            }
            else
            {
                t = (cell.Mean1 - cell.Mean2) / standardError;
                var variance1 = cell.Sd1 * cell.Sd1 / cell.N1;
                var variance2 = cell.Sd2 * cell.Sd2 / cell.N2;
                var denominator = variance1 * variance1 / (cell.N1 - 1) + variance2 * variance2 / (cell.N2 - 1);
                df = denominator > 0.0
                    ? Math.Max(1, (int)Math.Floor(standardError * standardError * standardError * standardError / denominator))
                    : Math.Min(cell.N1, cell.N2) - 1;
                var criticalT = TQuantile(Math.Max(df, 1));
                differs = Math.Abs(t) > criticalT;
            }

            results.Add(new TwoSampleCell(cell.Name, cell.Index, cell.Mean1, cell.Mean2, t, df, differs));
        }

        return (results, excluded);
    }

    // this node's BOOT.md, "## Set comparison", "Why the constant-cell bound is valid": the exact probability
    // that two constant sets of sizes n1/n2 land on different values by chance, under the null that both are
    // drawn from the same discrete distribution: g(a, b) = ((a-1)/(a+b-1))^(a-1) * (b/(a+b-1))^b, the tighter of
    // the two orderings. `Math.Pow(x, 0)` is 1 for every `x`, including 0, matching the `a = 1` convention the
    // derivation needs (a single-run set is trivially "constant").
    private static double ConstantMismatchBound(int n1, int n2)
    {
        static double G(int a, int b)
        {
            var total = a + b - 1;
            var ratioA = (double)(a - 1) / total;
            var ratioB = (double)b / total;
            return Math.Pow(ratioA, a - 1) * Math.Pow(ratioB, b);
        }

        return Math.Min(G(n1, n2), G(n2, n1));
    }

    private static double[] ValuesAt(IReadOnlyList<IReadOnlyDictionary<string, double[]>> set, string name, int index)
    {
        var values = new List<double>(set.Count);
        foreach (var replica in set)
        {
            if (replica.TryGetValue(name, out var array) && index < array.Length)
            {
                values.Add(array[index]);
            }
        }

        return [.. values];
    }
}
