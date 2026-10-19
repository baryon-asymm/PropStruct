using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The empty category rows of Fortran 869-871, 915 and 947 stay as the original keeps them under every model
/// (<c>src/Statistics/BOOT.md</c>, "## Corrected model", "<c>CategoriesEmptyRowsMerged</c>, withdrawn"): a row with
/// <c>QDOKSS = 0</c>, or <c>MDOK3</c> or <c>MDOK4</c> not above 1e-30, gets <c>Epsydok = 0</c> and so survives every merge
/// test, as does a row of one particle, which the same estimator gives the same <c>Epsydok = 0</c>. The flag that merged the
/// first and not the second was withdrawn (2026-10-19), so the right side is that <see cref="CorrectionSet.All"/>, the
/// <c>Corrected</c> model, reports what <see cref="CorrectionSet.None"/> reports, bit for bit, on constructed empty rows;
/// the withdrawn flag merged exactly these rows, so the cases are red on the code that still had it.
/// </summary>
public class CategoriesEmptyRowsKeptTests
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

    // A row of one particle in front: QDOKSS = 1, one cell, Epsydok 0, never merged, the same Epsydok an empty row gets.
    private static CategoryRun OneParticleRow() =>
        CategoryRun.Of(ndok: 3, ncat: 6, cellSize: 1.0, categoryStep: 1.0, epsDok: 0.3, dpMax: 2.5,
            qdoks: [[0, 1, 0], [0, 100, 0], [0, 100, 0]], dokp41: [5.0, 7.0, 9.0], dokp31: [1.0, 2.0, 3.0]);

    /// <summary>
    /// Known answer: under <c>None</c> the empty middle row survives with its <c>DOKP43</c> and distribution zero, three
    /// rows, and under <c>All</c> it is the same, bit for bit in the report and in the rewritten totals.
    /// </summary>
    [Fact]
    public void AnEmptyMiddleRowSurvivesUnderNoneAndUnderAll()
    {
        var none = EmptyMiddle();
        var all = EmptyMiddle();

        var reportNone = none.Merge(PrecisionKind.Binary64, CorrectionSet.None);
        var reportAll = all.Merge(PrecisionKind.Binary64, CorrectionSet.All);

        Assert.Equal(3, reportNone.DpRow);
        Assert.Equal(0.0, reportNone.Dokp43[1]);
        Assert.Equal(new double[3], reportNone.Qdokso[1]);
        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportAll, all.IntegerTotals, all.RealTotals), "empty middle"));
    }

    /// <summary>Known answer: an empty last row stays the last row, two rows, under <c>None</c> and under <c>All</c>.</summary>
    [Fact]
    public void AnEmptyLastRowStaysUnderNoneAndUnderAll()
    {
        var none = EmptyLast();
        var all = EmptyLast();

        var reportNone = none.Merge(PrecisionKind.Binary64, CorrectionSet.None);
        var reportAll = all.Merge(PrecisionKind.Binary64, CorrectionSet.All);

        Assert.Equal(2, reportNone.DpRow);
        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportAll, all.IntegerTotals, all.RealTotals), "empty last"));
    }

    /// <summary>Known answer: where every row is empty all three rows stay, under <c>None</c> and under <c>All</c>.</summary>
    [Fact]
    public void WhereEveryRowIsEmptyAllRowsStayUnderNoneAndUnderAll()
    {
        var none = AllEmpty();
        var all = AllEmpty();

        var reportNone = none.Merge(PrecisionKind.Binary64, CorrectionSet.None);
        var reportAll = all.Merge(PrecisionKind.Binary64, CorrectionSet.All);

        Assert.Equal(3, reportNone.DpRow);
        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportAll, all.IntegerTotals, all.RealTotals), "all empty"));
    }

    /// <summary>Known answer: a row of one particle, whose <c>Epsydok</c> is the empty row's, is no more merged than the empty row.</summary>
    [Fact]
    public void ARowOfOneParticleIsNotMergedUnderNoneOrAll()
    {
        var none = OneParticleRow();
        var all = OneParticleRow();

        var reportNone = none.Merge(PrecisionKind.Binary64, CorrectionSet.None);
        var reportAll = all.Merge(PrecisionKind.Binary64, CorrectionSet.All);

        Assert.Equal(3, reportAll.DpRow);
        Assert.Empty(BitwiseComparison.Differences((reportNone, none.IntegerTotals, none.RealTotals), (reportAll, all.IntegerTotals, all.RealTotals), "one particle"));
    }
}
