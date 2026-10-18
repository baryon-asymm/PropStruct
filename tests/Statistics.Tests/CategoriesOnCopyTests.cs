using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>CategoriesOnCopy</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 911-958, 825-829): the
/// original merges and shifts the category rows in the run's own totals, so a later cycle merges rows that earlier
/// cycles already merged and the old last row counts twice. Corrected, the merge, shift and recomputation run on a copy
/// of the rows <c>1..DPRow</c>, the totals are not written and a second call is the first. The right side is the totals
/// byte for byte and the report of <see cref="CorrectionSet.None"/> on fresh totals, which the case file holds; the red
/// side is <c>None</c>'s own second call, which differs, and the flag on a call that reads no copy.
/// </summary>
public class CategoriesOnCopyTests
{
    public static IEnumerable<object[]> CaseNames() =>
        FormulaCaseFiles.ReadCategoriesCases().Select(c => new object[] { c.Name });

    /// <summary>
    /// Right: under the flag the totals are byte-identical after the call, the report is the case file's (that of
    /// <c>None</c> on fresh totals), and a second call on the same totals gives the report of the first.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheFlagLeavesTheTotalsByteIdenticalAndASecondCallEqualToTheFirst(string caseName)
    {
        var c = FormulaCaseFiles.FindCategoriesCase(caseName);
        var run = CategoryRun.From(c);
        var before = run.Copy();

        var first = run.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesOnCopy);
        var second = run.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesOnCopy);

        Assert.Empty(BitwiseComparison.Differences((before.IntegerTotals, before.RealTotals), (run.IntegerTotals, run.RealTotals), caseName));
        Assert.Empty(BitwiseComparison.Differences(first, second, caseName));
        AssertExpected(c.Expected, first);
    }

    /// <summary>
    /// Red: <c>None</c> rewrites the totals into the case file's "after" rows and its second call, on those rewritten
    /// rows, is not the first, so that the equalities above are not vacuous.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void WithoutTheFlagTheTotalsAreRewrittenAndASecondCallDiffers(string caseName)
    {
        var c = FormulaCaseFiles.FindCategoriesCase(caseName);
        var run = CategoryRun.From(c);
        var before = run.Copy();

        var first = run.Merge(PrecisionKind.Binary64, CorrectionSet.None);

        AssertExpected(c.Expected, first);
        Assert.NotEmpty(BitwiseComparison.Differences((before.IntegerTotals, before.RealTotals), (run.IntegerTotals, run.RealTotals), caseName));
        Assert.Equal(c.Expected.QdoksAfter.Select(row => row.ToArray()), run.QdoksRows(c.Expected.QdoksAfter.Count));

        var second = run.Merge(PrecisionKind.Binary64, CorrectionSet.None);

        Assert.NotEmpty(BitwiseComparison.Differences(first, second, caseName));
    }

    /// <summary>Under <see cref="PrecisionKind.Original"/> the flag is replaced by <c>None</c> on entry: the totals are rewritten as the original rewrites them.</summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    public void UnderOriginalTheFlagIsIgnored(string caseName)
    {
        var c = FormulaCaseFiles.FindCategoriesCase(caseName);
        var run = CategoryRun.From(c);

        var outcome = run.Merge(PrecisionKind.Original, CorrectionSet.CategoriesOnCopy);

        AssertExpected(c.ExpectedOriginal, outcome);
        Assert.Equal(c.ExpectedOriginal.QdoksAfter.Select(row => row.ToArray()), run.QdoksRows(c.ExpectedOriginal.QdoksAfter.Count));
        Assert.Equal(c.ExpectedOriginal.Dokp41After, run.RealTotals.AsSpan(run.Layout.Dokp41, c.ExpectedOriginal.Dokp41After.Count).ToArray());
    }

    /// <summary>
    /// Right through <c>CycleStatistics.Compute</c>: on the totals of a cycle whose rows merge, two calls of cycle 0 under the
    /// flag write no category total and report alike in every field; red: under <c>None</c> the first call rewrites them and
    /// the second report differs.
    /// </summary>
    [Fact]
    public void ComputeWritesNoCategoryTotalUnderTheFlag()
    {
        var flagged = TwoComputes(CorrectionSet.CategoriesOnCopy);
        var none = TwoComputes(CorrectionSet.None);

        Assert.Empty(BitwiseComparison.Differences(flagged.Initial, flagged.Final, "flag totals"));
        Assert.Empty(BitwiseComparison.Differences(flagged.First, flagged.Second, "flag reports"));
        Assert.NotEmpty(BitwiseComparison.Differences(none.Initial, none.Final, "none totals"));
        Assert.NotEmpty(BitwiseComparison.Differences(none.First, none.Second, "none reports"));
        Assert.Empty(BitwiseComparison.Differences(none.First, flagged.First, "first call"));
    }

    private static void AssertExpected(CategoriesExpected expected, CategoryOutcome actual)
    {
        Assert.Equal(expected.DpRow, actual.DpRow);
        Assert.Equal(expected.Dpockets, actual.Dpockets);
        Assert.Equal(expected.Dokp43, actual.Dokp43);
        Assert.Equal(expected.Qdokkarm, actual.Qdokkarm);
        Assert.Equal(expected.Qdokso.Select(row => row.ToArray()), actual.Qdokso);
    }

    private static Computes TwoComputes(CorrectionSet corrections)
    {
        var cycle = AnalyticCycle.Prepare(ShippedFormulations.Read("P33"), ModelParameters.Default, PrecisionKind.Binary64, corrections);
        var layout = cycle.Layout;
        var integerTotals = cycle.IntegerTotals();
        var realTotals = cycle.RealTotals();
        realTotals[layout.DpMax] = 3.5 * cycle.CategoryStep;
        long[][] rows = [[5, 5, 0, 0], [0, 20, 0, 0], [0, 10, 0, 0], [3, 0, 0, 4]];
        for (var r = 0; r < rows.Length; r++)
        {
            for (var i = 0; i < rows[r].Length; i++)
            {
                integerTotals[layout.Qdoks + r * cycle.Ndok + i] = rows[r][i];
            }

            realTotals[layout.Dokp41 + r] = 5.0 + 2.0 * r;
            realTotals[layout.Dokp31 + r] = 1.0 + r;
        }

        var initial = (integerTotals.ToArray(), realTotals.ToArray());
        var first = cycle.Compute(0, integerTotals, realTotals, out _);
        var second = cycle.Compute(0, integerTotals, realTotals, out _);
        return new Computes(initial, (integerTotals, realTotals), first, second);
    }

    private sealed record Computes(
        (long[] Integers, double[] Reals) Initial, (long[] Integers, double[] Reals) Final, CycleReport First, CycleReport Second);
}
