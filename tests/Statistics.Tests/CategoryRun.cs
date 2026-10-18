using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// One call of <c>Categories.MergeAndDescribe</c> on constructed totals: the rows of <c>Qdoks</c>, <c>Dokp41</c> and
/// <c>Dokp31</c> a case gives, laid out as <see cref="AccumulatorLayout"/> lays them, and the run's own totals that the
/// call reads and, without <see cref="CorrectionSet.CategoriesOnCopy"/>, rewrites. Shared by the tests of the three
/// category flags and by the seam (<c>CorrectionSeamTests</c>).
/// </summary>
internal sealed class CategoryRun
{
    private readonly int ndok;
    private readonly int ncat;
    private readonly double cellSize;
    private readonly double categoryStep;
    private readonly double epsDok;
    private readonly double dpMax;

    private CategoryRun(
        int ndok, int ncat, double cellSize, double categoryStep, double epsDok, double dpMax,
        AccumulatorLayout layout, long[] integerTotals, double[] realTotals)
    {
        this.ndok = ndok;
        this.ncat = ncat;
        this.cellSize = cellSize;
        this.categoryStep = categoryStep;
        this.epsDok = epsDok;
        this.dpMax = dpMax;
        Layout = layout;
        IntegerTotals = integerTotals;
        RealTotals = realTotals;
    }

    /// <summary>The layout of the totals.</summary>
    public AccumulatorLayout Layout { get; }

    /// <summary>The run's integer totals, rewritten in place by a call without <see cref="CorrectionSet.CategoriesOnCopy"/>.</summary>
    public long[] IntegerTotals { get; }

    /// <summary>The run's real totals, rewritten in place by a call without <see cref="CorrectionSet.CategoriesOnCopy"/>.</summary>
    public double[] RealTotals { get; }

    /// <summary>The run of a case file's case: its rows, its sizes, its threshold and its <c>DPmax</c>.</summary>
    public static CategoryRun From(CategoriesCase c) =>
        Of(c.Ndok, c.Ncat, c.CellSize, c.CategoryStep, c.EpsDok, c.DpMax, c.Qdoks, c.Dokp41, c.Dokp31);

    /// <summary>The run of the rows given, row <c>r</c> of <paramref name="qdoks"/> being category <c>r</c> (0-based).</summary>
    public static CategoryRun Of(
        int ndok, int ncat, double cellSize, double categoryStep, double epsDok, double dpMax,
        IReadOnlyList<IReadOnlyList<long>> qdoks, IReadOnlyList<double> dokp41, IReadOnlyList<double> dokp31)
    {
        var layout = AccumulatorLayout.Create(fractionCount: 1, ndok: ndok, nkarm: 1, ncat: ncat, nc: 1);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];
        for (var r = 0; r < qdoks.Count; r++)
        {
            for (var i = 0; i < ndok; i++)
            {
                integerTotals[layout.Qdoks + r * ndok + i] = qdoks[r][i];
            }

            realTotals[layout.Dokp41 + r] = dokp41[r];
            realTotals[layout.Dokp31 + r] = dokp31[r];
        }

        return new CategoryRun(ndok, ncat, cellSize, categoryStep, epsDok, dpMax, layout, integerTotals, realTotals);
    }

    /// <summary>The same inputs on a copy of the totals as they are now, so that a later call on one does not touch the other.</summary>
    public CategoryRun Copy() =>
        new(ndok, ncat, cellSize, categoryStep, epsDok, dpMax, Layout, (long[])IntegerTotals.Clone(), (double[])RealTotals.Clone());

    /// <summary>One <c>MergeAndDescribe</c> on this run's totals, which must report <see cref="CategoriesStatus.Ok"/>.</summary>
    public CategoryOutcome Merge(PrecisionKind precision, CorrectionSet corrections)
    {
        var status = Categories.MergeAndDescribe(
            ndok, ncat, cellSize, categoryStep, epsDok, dpMax, Layout, IntegerTotals, RealTotals, precision, corrections,
            out var dpRow, out var dpockets, out var dokp43, out var qdokkarm, out var qdokso);
        Assert.Equal(CategoriesStatus.Ok, status);
        return new CategoryOutcome(dpRow, dpockets, dokp43, qdokkarm, qdokso);
    }

    /// <summary>One call of the member that takes no <see cref="CorrectionSet"/>, the original's model, on this run's totals.</summary>
    public CategoryOutcome MergeWithoutSet(PrecisionKind precision)
    {
        var status = Categories.MergeAndDescribe(
            ndok, ncat, cellSize, categoryStep, epsDok, dpMax, Layout, IntegerTotals, RealTotals, precision,
            out var dpRow, out var dpockets, out var dokp43, out var qdokkarm, out var qdokso);
        Assert.Equal(CategoriesStatus.Ok, status);
        return new CategoryOutcome(dpRow, dpockets, dokp43, qdokkarm, qdokso);
    }

    /// <summary>The first <paramref name="rows"/> rows of <c>Qdoks</c> as the totals hold them.</summary>
    public long[][] QdoksRows(int rows) =>
        [.. Enumerable.Range(0, rows).Select(r => IntegerTotals.AsSpan(Layout.Qdoks + r * ndok, ndok).ToArray())];
}

/// <summary>What one <c>MergeAndDescribe</c> reports: the row count and the per-row values, trimmed to it.</summary>
internal sealed record CategoryOutcome(int DpRow, double[] Dpockets, double[] Dokp43, double[] Qdokkarm, double[][] Qdokso);
