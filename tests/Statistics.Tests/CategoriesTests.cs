using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L1 of the BOOT.md table: <c>Categories.MergeAndDescribe</c> on constructed totals
/// against <c>tests/Fixtures/formulas_statistics.py</c>'s independent transcription of
/// Fortran lines 825-959, covering a merge pass, a merge followed by a shift, a
/// cascade that reaches the last-row check, and a <c>QDOKSS = 0</c> row. Every case
/// also proves the in-place rewrite of <c>Qdoks</c>/<c>Dokp41</c>/<c>Dokp31</c>
/// (BOOT.md, "In-place rewrites of the totals").
/// </summary>
public class CategoriesTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var c in FormulaCaseFiles.ReadCategoriesCases())
        {
            yield return new object[] { c.Name, nameof(PrecisionKind.Binary64) };
            yield return new object[] { c.Name, nameof(PrecisionKind.Original) };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MergeAndDescribeMatchesTheFormulaScript(string caseName, string kindName)
    {
        var c = FormulaCaseFiles.FindCategoriesCase(caseName);
        var kind = Enum.Parse<PrecisionKind>(kindName);
        var expected = kind == PrecisionKind.Original ? c.ExpectedOriginal : c.Expected;

        var layout = AccumulatorLayout.Create(fractionCount: 1, ndok: c.Ndok, nkarm: 1, ncat: c.Ncat, nc: 1);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];

        var rowCount = c.Qdoks.Count;
        for (var r = 0; r < rowCount; r++)
        {
            for (var i = 0; i < c.Ndok; i++)
            {
                integerTotals[layout.Qdoks + r * c.Ndok + i] = c.Qdoks[r][i];
            }

            realTotals[layout.Dokp41 + r] = c.Dokp41[r];
            realTotals[layout.Dokp31 + r] = c.Dokp31[r];
        }

        var status = Categories.MergeAndDescribe(
            c.Ndok, c.Ncat, c.CellSize, c.CategoryStep, c.EpsDok, c.DpMax,
            layout, integerTotals, realTotals, kind,
            out var dpRow, out var dpockets, out var dokp43, out var qdokkarm, out var qdokso);

        Assert.Equal(CategoriesStatus.Ok, status);
        Assert.Equal(expected.DpRow, dpRow);
        Assert.Equal(expected.Dpockets, dpockets);
        Assert.Equal(expected.Dokp43, dokp43);
        Assert.Equal(expected.Qdokkarm, qdokkarm);

        for (var r = 0; r < expected.DpRow; r++)
        {
            for (var i = 0; i < c.Ndok; i++)
            {
                Assert.Equal(expected.Qdokso[r][i], qdokso[r][i]);
            }
        }

        // In-place rewrite of the run's own totals (BOOT.md, "In-place rewrites of the totals").
        for (var r = 0; r < rowCount; r++)
        {
            for (var i = 0; i < c.Ndok; i++)
            {
                Assert.Equal(expected.QdoksAfter[r][i], integerTotals[layout.Qdoks + r * c.Ndok + i]);
            }

            Assert.Equal(expected.Dokp41After[r], realTotals[layout.Dokp41 + r]);
            Assert.Equal(expected.Dokp31After[r], realTotals[layout.Dokp31 + r]);
        }
    }

    /// <summary>
    /// Mutation proof (AGENTS.md §13): the "merge pass" case's row 0 absorbing row 1
    /// depends on the pass actually adding the source row rather than merely marking it
    /// merged. Flipping <c>+=</c> to a plain assignment in <c>Categories.MergeRow</c>
    /// (the change proven red here, then reverted) drops <c>Dokp41[0]</c> from 12.0 to
    /// 7.0 for the "single_merge_pass_two_rows" case; recorded in this node's BOOT.md.
    /// </summary>
    [Fact]
    public void MergeRowAddsRatherThanOverwritesTheSourceRow()
    {
        var c = FormulaCaseFiles.ReadCategoriesCases().Single(x => x.Name == "single_merge_pass_two_rows");
        var layout = AccumulatorLayout.Create(1, c.Ndok, 1, c.Ncat, 1);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];

        for (var r = 0; r < c.Qdoks.Count; r++)
        {
            for (var i = 0; i < c.Ndok; i++)
            {
                integerTotals[layout.Qdoks + r * c.Ndok + i] = c.Qdoks[r][i];
            }

            realTotals[layout.Dokp41 + r] = c.Dokp41[r];
            realTotals[layout.Dokp31 + r] = c.Dokp31[r];
        }

        _ = Categories.MergeAndDescribe(
            c.Ndok, c.Ncat, c.CellSize, c.CategoryStep, c.EpsDok, c.DpMax,
            layout, integerTotals, realTotals, PrecisionKind.Binary64,
            out _, out _, out var dokp43, out _, out _);

        // The merged row's own DOKP43 = (5+7)/(1+1), read from the fixture's own
        // expectation rather than typed here, distinguishing "add" (this assertion)
        // from "overwrite" (which would leave it at 5.0/1.0 = 5.0, found by the audit
        // of this node: the previous version of this test typed the expected 6.0
        // instead of reading it from the case that already carries it).
        Assert.Equal(c.Expected.Dokp43[0], dokp43[0]);
    }

    /// <summary>
    /// The initial <c>DPRow = int(DPmax/Dj) + 1</c> uses the same binary32 arithmetic
    /// <c>Setup.Sizes</c> already uses for <c>Ndok</c> (root BOOT.md, "Double precision
    /// only", the exception extended to <c>DPRow</c>), not a plain <c>double</c>
    /// division: <c>dpMax</c>/<c>categoryStep</c> are built the same way as the C166
    /// evidence for <c>Ndok</c> (<c>Binary32Tests.TruncatedQuotientAvoidsDoubleRoundingAtAnIntegerBoundary</c>),
    /// where the raw double quotient sits exactly at 70.0 but both operands round down
    /// under binary32, truncating to 69 (<c>DPRow</c> = 70, not 71). Mutation proof
    /// (AGENTS.md §13): reverting <c>Categories.cs</c>'s <c>Binary32.TruncatedQuotient</c>
    /// call to <c>(int)Math.Floor(dpMax / categoryStep)</c> made this test fail
    /// (<c>DpRow</c> came back 71); reverted, green again (recorded in this node's
    /// BOOT.md, "## Mutations").
    /// </summary>
    [Fact]
    public void MergeAndDescribeUsesBinary32ArithmeticForDpRow()
    {
        const int ndok = 1;
        const int ncat = 75;
        var dpMax = Binary32.Multiply(700.0, 1e-6);
        var categoryStep = Binary32.Multiply(10.0, 1e-6);

        var layout = AccumulatorLayout.Create(fractionCount: 1, ndok: ndok, nkarm: 1, ncat: ncat, nc: 1);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];

        var status = Categories.MergeAndDescribe(
            ndok, ncat, cellSize: 1.0, categoryStep, epsDok: 1e-6, dpMax,
            layout, integerTotals, realTotals, PrecisionKind.Binary64,
            out var dpRow, out _, out _, out _, out _);

        Assert.Equal(CategoriesStatus.Ok, status);
        Assert.Equal(70, dpRow);
    }

    /// <summary>
    /// The undeclared <c>r &lt; ncat</c> clamp this node's own audit found is replaced
    /// by a checked status (BOOT.md, "## Categories"): a <c>DPRow</c> built to exceed
    /// <c>ncat</c> reports <see cref="CategoriesStatus.CategoryCountExceedsCapacity"/>
    /// instead of silently truncating the category count, and every <see langword="out"/>
    /// parameter comes back empty rather than a truncated, misleading result. Mutation
    /// proof (AGENTS.md §13): restoring the old silent clamp (looping
    /// <c>for (var r = 0; r &lt; row &amp;&amp; r &lt; ncat; r++)</c> without the
    /// capacity check above it) made this test fail (<c>status</c> came back
    /// <see cref="CategoriesStatus.Ok"/> with a truncated <c>dpRow</c> instead of the
    /// reported status); reverted, green again (recorded in this node's BOOT.md,
    /// "## Mutations").
    /// </summary>
    [Fact]
    public void MergeAndDescribeReportsStatusWhenDpRowExceedsNcat()
    {
        const int ndok = 1;
        const int ncat = 5;
        const double dpMax = 100.0;
        const double categoryStep = 1.0; // DPRow = trunc(100/1) + 1 = 101 > ncat.

        var layout = AccumulatorLayout.Create(fractionCount: 1, ndok: ndok, nkarm: 1, ncat: ncat, nc: 1);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];

        var status = Categories.MergeAndDescribe(
            ndok, ncat, cellSize: 1.0, categoryStep, epsDok: 1e-6, dpMax,
            layout, integerTotals, realTotals, PrecisionKind.Binary64,
            out var dpRow, out var dpockets, out var dokp43, out var qdokkarm, out var qdokso);

        Assert.Equal(CategoriesStatus.CategoryCountExceedsCapacity, status);
        Assert.Equal(0, dpRow);
        Assert.Empty(dpockets);
        Assert.Empty(dokp43);
        Assert.Empty(qdokkarm);
        Assert.Empty(qdokso);
    }
}
