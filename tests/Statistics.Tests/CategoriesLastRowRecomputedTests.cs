using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>CategoriesLastRowRecomputed</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 947-959): the original
/// merges an insufficient last row into the row before it and goes on without label 600, so the new last row keeps the
/// statistics of before the merge, <c>DOKP43</c> and the size distribution among them, and is never checked again.
/// Corrected, the loop returns to label 600 after a last-row merge until no last-row check merges. The right side is the
/// merged row's <c>DOKP43</c> as the quotient of the sums over the rows merged into it, which the reported <c>Dpockets</c>
/// name, and a second call on the final rows that merges nothing, which is "until none is"; the red side is <c>None</c>,
/// whose last row keeps the quotient of before the merge. The cascade case of the case file merges twice, the constructed
/// one three rows into one reported row after two merges, derived by hand in the comments below.
/// </summary>
public class CategoriesLastRowRecomputedTests
{
    private const string FixtureCascade = "last_row_merge_without_recompute";

    // Four rows of three cells, cell centres 0.5, 1.5, 2.5, threshold 0.15: the last row [50, 50, 0] has Epsydok 0.404, merged
    // into the row before it [50, 150, 0] has 0.168, merged into the next [50, 250, 0] has 0.106, which stops the cascade
    // at two reported rows. None stops after the first merge, three rows, the last with the statistics of [0, 100, 0].
    private static CategoryRun FourRows() =>
        CategoryRun.Of(ndok: 3, ncat: 6, cellSize: 1.0, categoryStep: 1.0, epsDok: 0.15, dpMax: 3.5,
            qdoks: [[0, 100, 0], [0, 100, 0], [0, 100, 0], [50, 50, 0]],
            dokp41: [5.0, 7.0, 9.0, 11.0], dokp31: [1.0, 2.0, 3.0, 4.0]);

    private static CategoryRun Cascade() => CategoryRun.From(FormulaCaseFiles.FindCategoriesCase(FixtureCascade));

    public static IEnumerable<object[]> Runs() => [["fixture cascade"], ["four rows"]];

    private static CategoryRun RunNamed(string name) => name == "four rows" ? FourRows() : Cascade();

    private static double[] Dokp41(string name) => name == "four rows" ? [5.0, 7.0, 9.0, 11.0] : [.. FormulaCaseFiles.FindCategoriesCase(FixtureCascade).Dokp41];

    private static double[] Dokp31(string name) => name == "four rows" ? [1.0, 2.0, 3.0, 4.0] : [.. FormulaCaseFiles.FindCategoriesCase(FixtureCascade).Dokp31];

    /// <summary>The reported <c>DOKP43</c> of row <c>r</c>: the sums of <c>Dokp41</c> and <c>Dokp31</c> over the rows merged into it, which its <c>Dpockets</c> and the one before it name.</summary>
    private static double MergedDokp43(string name, CategoryOutcome outcome, int r)
    {
        var first = r == 0 ? 0 : (int)Math.Round(outcome.Dpockets[r - 1]);
        var last = (int)Math.Round(outcome.Dpockets[r]);
        var dokp41 = Dokp41(name);
        var dokp31 = Dokp31(name);
        return Enumerable.Range(first, last - first).Sum(i => dokp41[i]) / Enumerable.Range(first, last - first).Sum(i => dokp31[i]);
    }

    /// <summary>
    /// Right: under the flag every reported row's <c>DOKP43</c> is the quotient of the sums over its member rows, and a second
    /// call from the final rows, on the totals the first call rewrote, merges nothing and reports the same row count and quotients: no row is
    /// left insufficient (its radii restart at the row index, as the original's first loop sets them).
    /// </summary>
    [Theory]
    [MemberData(nameof(Runs))]
    public void TheFlagRecomputesTheLastRowUntilNoRowIsInsufficient(string name)
    {
        var run = RunNamed(name);

        var outcome = run.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesLastRowRecomputed);

        for (var r = 0; r < outcome.DpRow; r++)
        {
            AssertRelative(MergedDokp43(name, outcome, r), outcome.Dokp43[r]);
        }

        var again = run.WithDpMax(run.DpMaxForRows(outcome.DpRow)).Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesLastRowRecomputed);
        Assert.Equal(outcome.DpRow, again.DpRow);
        Assert.Empty(BitwiseComparison.Differences(outcome.Dokp43, again.Dokp43, name));
    }

    /// <summary>
    /// Red: under <c>None</c> the cascade stops after one last-row merge, one row more than the flag keeps, and the new last
    /// row reports the <c>DOKP43</c> of the single row it was before the merge, not the quotient over the rows now in it.
    /// </summary>
    [Theory]
    [MemberData(nameof(Runs))]
    public void WithoutTheFlagTheMergedLastRowKeepsTheStatisticsOfBeforeTheMerge(string name)
    {
        var flagged = RunNamed(name).Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesLastRowRecomputed);
        var none = RunNamed(name).Merge(PrecisionKind.Binary64, CorrectionSet.None);

        Assert.True(none.DpRow > flagged.DpRow, $"None keeps {none.DpRow} rows, the flag {flagged.DpRow}");
        var last = none.DpRow - 1;
        var dokp41 = Dokp41(name);
        var dokp31 = Dokp31(name);
        Assert.Equal(dokp41[last] / dokp31[last], none.Dokp43[last]);
        Assert.NotEqual(MergedDokp43(name, none, last), none.Dokp43[last]);
    }

    /// <summary>
    /// Right on the hand-derived cascade of the four-row case (the comment above): the flag stops at two reported rows after two
    /// last-row merges, <c>None</c> at three after one.
    /// </summary>
    [Fact]
    public void TheFourRowCascadeStopsAtTwoRowsUnderTheFlagAndThreeWithout()
    {
        Assert.Equal(2, FourRows().Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesLastRowRecomputed).DpRow);
        Assert.Equal(3, FourRows().Merge(PrecisionKind.Binary64, CorrectionSet.None).DpRow);
    }

    /// <summary>
    /// Right on a known answer: where the last-row check merges nothing (the pass merge and shift cases of the case file) the
    /// flag changes no bit of the report or of the rewritten totals.
    /// </summary>
    [Theory]
    [InlineData("single_merge_pass_two_rows")]
    [InlineData("merge_then_shift_three_rows")]
    public void WhereNoLastRowMergesTheFlagChangesNoBit(string caseName)
    {
        var c = FormulaCaseFiles.FindCategoriesCase(caseName);
        var none = CategoryRun.From(c);
        var flagged = CategoryRun.From(c);

        var reportNone = none.Merge(PrecisionKind.Binary64, CorrectionSet.None);
        var reportFlag = flagged.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesLastRowRecomputed);

        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportFlag, flagged.IntegerTotals, flagged.RealTotals), caseName));
    }

    /// <summary>Under <see cref="PrecisionKind.Original"/> the flag is replaced by <c>None</c> on entry: the case file's own expected report.</summary>
    [Fact]
    public void UnderOriginalTheFlagIsIgnored()
    {
        var c = FormulaCaseFiles.FindCategoriesCase(FixtureCascade);

        var outcome = CategoryRun.From(c).Merge(PrecisionKind.Original, CorrectionSet.CategoriesLastRowRecomputed);

        Assert.Equal(c.ExpectedOriginal.DpRow, outcome.DpRow);
        Assert.Equal(c.ExpectedOriginal.Dokp43, outcome.Dokp43);
    }

    private static void AssertRelative(double expected, double actual) =>
        Assert.True(Math.Abs(actual - expected) <= 1e-12 * Math.Abs(expected), $"expected {expected:R}, got {actual:R}");
}
