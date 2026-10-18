using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>CategoriesEmptyRowsMerged</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 869-871, 915, 947): a
/// category row with <c>QDOKSS = 0</c>, or <c>MDOK3</c> or <c>MDOK4</c> not above 1e-30, gets <c>Epsydok = 0</c> and so
/// survives every merge test, however few particles it holds. Corrected, such a row is insufficient in both tests, the pass and
/// the last-row check. The right side is a constructed empty middle row the flag merges into the row behind it, an empty last
/// row merged into the row before it, and a row of one particle (<c>QDOKSS = 1</c>, legitimately without variance) that it
/// leaves alone; the red side is <c>None</c>, which keeps the empty rows.
/// </summary>
public class CategoriesEmptyRowsMergedTests
{
    // Three rows of three cells, threshold 0.3: the middle row is empty. Every other row holds its particles in one cell, so
    // its Epsydok is 0 and no row is insufficient by the threshold.
    private static CategoryRun EmptyMiddle() =>
        CategoryRun.Of(ndok: 3, ncat: 6, cellSize: 1.0, categoryStep: 1.0, epsDok: 0.3, dpMax: 2.5,
            qdoks: [[0, 100, 0], [0, 0, 0], [0, 100, 0]], dokp41: [5.0, 0.0, 7.0], dokp31: [1.0, 0.0, 2.0]);

    private static CategoryRun EmptyLast() =>
        CategoryRun.Of(ndok: 3, ncat: 6, cellSize: 1.0, categoryStep: 1.0, epsDok: 0.3, dpMax: 1.5,
            qdoks: [[0, 100, 0], [0, 0, 0]], dokp41: [5.0, 0.0], dokp31: [1.0, 0.0]);

    private static CategoryRun AllEmpty() =>
        CategoryRun.Of(ndok: 3, ncat: 6, cellSize: 1.0, categoryStep: 1.0, epsDok: 0.3, dpMax: 2.5,
            qdoks: [[0, 0, 0], [0, 0, 0], [0, 0, 0]], dokp41: [0.0, 0.0, 0.0], dokp31: [0.0, 0.0, 0.0]);

    // The same rows with a row of one particle in front: QDOKSS = 1, one cell, Epsydok 0, not empty.
    private static CategoryRun OneParticleRow() =>
        CategoryRun.Of(ndok: 3, ncat: 6, cellSize: 1.0, categoryStep: 1.0, epsDok: 0.3, dpMax: 2.5,
            qdoks: [[0, 1, 0], [0, 100, 0], [0, 100, 0]], dokp41: [5.0, 7.0, 9.0], dokp31: [1.0, 2.0, 3.0]);

    /// <summary>
    /// Right: the empty middle row is merged into the row behind it, two rows are reported, the first untouched, the second
    /// holding the empty row's and the last row's particles and reporting the quotient of their sums.
    /// </summary>
    [Fact]
    public void TheFlagMergesAnEmptyMiddleRowIntoTheRowBehindIt()
    {
        var run = EmptyMiddle();

        var outcome = run.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesEmptyRowsMerged);

        Assert.Equal(2, outcome.DpRow);
        Assert.Equal([1.0, 3.0], outcome.Dpockets);
        Assert.Equal(run.MergedDokp43(outcome, 1), outcome.Dokp43[1]);
        Assert.Equal(new long[] { 0, 100, 0 }, run.QdoksRows(2)[1]);
    }

    /// <summary>Red: <c>None</c> keeps the empty middle row, three rows, its <c>DOKP43</c> and distribution zero.</summary>
    [Fact]
    public void WithoutTheFlagTheEmptyMiddleRowSurvives()
    {
        var outcome = EmptyMiddle().Merge(PrecisionKind.Binary64, CorrectionSet.None);

        Assert.Equal(3, outcome.DpRow);
        Assert.Equal(0.0, outcome.Dokp43[1]);
        Assert.Equal(new double[3], outcome.Qdokso[1]);
    }

    /// <summary>
    /// Right: an empty last row is insufficient in the last-row check and merges into the row before it; <c>None</c> keeps
    /// it as the last row.
    /// </summary>
    [Fact]
    public void TheFlagMergesAnEmptyLastRowAndNoneKeepsIt()
    {
        var flagged = EmptyLast().Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesEmptyRowsMerged);
        var none = EmptyLast().Merge(PrecisionKind.Binary64, CorrectionSet.None);

        Assert.Equal(1, flagged.DpRow);
        Assert.Equal(2, none.DpRow);
    }

    /// <summary>
    /// Right: where every row is empty the merge terminates at one row, the loop's own bound, and reports no quotient.
    /// <c>None</c> keeps all three.
    /// </summary>
    [Fact]
    public void WhereEveryRowIsEmptyTheFlagMergesDownToOneRow()
    {
        var flagged = AllEmpty().Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesEmptyRowsMerged);
        var none = AllEmpty().Merge(PrecisionKind.Binary64, CorrectionSet.None);

        Assert.Equal(1, flagged.DpRow);
        Assert.Equal(0.0, flagged.Dokp43[0]);
        Assert.Equal(3, none.DpRow);
    }

    /// <summary>
    /// Right on a known answer: a row of one particle is no empty row, so the flag changes no bit of the report or of the
    /// rewritten totals on rows none of which is empty.
    /// </summary>
    [Fact]
    public void ARowOfOneParticleIsNotMerged()
    {
        var none = OneParticleRow();
        var flagged = OneParticleRow();

        var reportNone = none.Merge(PrecisionKind.Binary64, CorrectionSet.None);
        var reportFlag = flagged.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesEmptyRowsMerged);

        Assert.Equal(3, reportFlag.DpRow);
        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportFlag, flagged.IntegerTotals, flagged.RealTotals), "one particle"));
    }

    /// <summary>
    /// Right through the case file's <c>QDOKSS = 0</c> case: row 0 is empty and row 1 is not; the flag merges row 1 into it in
    /// the pass, and the merged row reports the quotient of its sums where <c>None</c> reports the empty row's zero.
    /// </summary>
    [Fact]
    public void TheCaseFilesEmptyRowReportsTheMergedQuotient()
    {
        var c = FormulaCaseFiles.FindCategoriesCase("empty_row_qdokss_zero");
        var run = CategoryRun.From(c);

        var outcome = run.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesEmptyRowsMerged);

        Assert.Equal(1, outcome.DpRow);
        Assert.Equal(0.0, c.Expected.Dokp43[0]);
        Assert.Equal(run.MergedDokp43(outcome, 0), outcome.Dokp43[0]);
        Assert.NotEqual(c.Expected.Dokp43[0], outcome.Dokp43[0]);
    }

    /// <summary>Under <see cref="PrecisionKind.Original"/> the flag is replaced by <c>None</c> on entry.</summary>
    [Fact]
    public void UnderOriginalTheFlagIsIgnored()
    {
        var none = EmptyMiddle();
        var flagged = EmptyMiddle();

        var reportNone = none.Merge(PrecisionKind.Original, CorrectionSet.None);
        var reportFlag = flagged.Merge(PrecisionKind.Original, CorrectionSet.CategoriesEmptyRowsMerged);

        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportFlag, flagged.IntegerTotals, flagged.RealTotals), "empty middle"));
    }
}
