using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The conditional oxidizer-size distribution by pocket category, with its iterative
/// merge (Fortran lines 825-959, BOOT.md "## Categories"). Operates on the run's own
/// <c>Qdoks</c>, <c>Dokp41</c>, <c>Dokp31</c> totals in place (BOOT.md, "In-place
/// rewrites of the totals, as the original"): <c>VDOKS</c> and <c>QDOK</c> feed nothing
/// this port reports and are not accumulated (BOOT.md, "## Categories").
/// </summary>
internal static class Categories
{
    /// <param name="ndok">Ndok, the oxidizer-size array width: the column count of each category row's own <c>Qdoks</c>/<c>Qdokso</c> (BOOT.md, "## Categories").</param>
    /// <param name="ncat">Ncat, the category-row array capacity that <paramref name="dpRow"/> is checked against (BOOT.md, "## Categories", "`DPRow` never exceeds `Ncat`").</param>
    /// <param name="cellSize">Di, the oxidizer-size histogram cell size: the category rows are Dj-wide, but each row's own Ndok columns are Di-wide (Fortran `Di*(Iks-0.5)`, lines 853-854, 863-866, 893, 899).</param>
    /// <param name="categoryStep">Dj, the category-row step.</param>
    /// <param name="epsDok">EpsDok, the merge threshold (BOOT.md, "## Categories", step 3/4): a row whose own <c>Epsydok</c> exceeds it absorbs the next row (or, for the last row, the row before it).</param>
    /// <param name="dpMax">DPmax, the largest pocket radius reached so far this cycle; the initial row count is <c>trunc(dpMax / categoryStep) + 1</c> (BOOT.md, "## Categories", step 1).</param>
    /// <param name="layout">The offsets of <c>Qdoks</c>, <c>Dokp41</c> and <c>Dokp31</c> within <paramref name="integerTotals"/>/<paramref name="realTotals"/>.</param>
    /// <param name="integerTotals">The run's integer totals; <c>Qdoks</c> is read, and its rows are merged or shifted in place (BOOT.md, "## Invariants", "In-place rewrites of the totals, as the original").</param>
    /// <param name="realTotals">The run's real totals; <c>Dokp41</c> and <c>Dokp31</c> are read, and their rows are merged or shifted in place, the same way as <paramref name="integerTotals"/>.</param>
    /// <param name="precision">The run's precision kind: under <see cref="PrecisionKind.Original"/> the sites of lines 825-959 that <c>CyclePlane.listing.generated.txt</c> names round through <see cref="CyclePlaneRounding"/>; under <see cref="PrecisionKind.Binary64"/> nothing changes.</param>
    /// <param name="dpRow">DPRow after every merge: the number of category rows the returned arrays actually use.</param>
    /// <param name="dpockets">Dpockets, the merged rows' own radii (BOOT.md, "## Categories", step 1/3/4).</param>
    /// <param name="dokp43">DOKP43 per row, <c>Dokp41 / Dokp31</c> or 0 when <c>Dokp31 &lt;= 1e-30</c>.</param>
    /// <param name="qdokkarm">The row's own size moment, <c>Σ Qdokso·c_i</c> over the row's Ndok cells (BOOT.md, "## Categories", step 2).</param>
    /// <param name="qdokso">QDOKSO per row, the row's own conditional oxidizer-size distribution over its Ndok cells.</param>
    /// <returns>
    /// <see cref="CategoriesStatus.Ok"/>, or <see cref="CategoriesStatus.CategoryCountExceedsCapacity"/>
    /// if the initial <c>DPRow</c> exceeds <paramref name="ncat"/> (root BOOT.md, "Failures
    /// are values"): on that status every <see langword="out"/> parameter is empty/zero and
    /// no total is read or written, rather than the source's own unclamped
    /// <c>do irow = 1,DPRow</c> being truncated to fit (<see cref="CategoriesStatus"/>).
    /// </returns>
    public static CategoriesStatus MergeAndDescribe(
        int ndok, int ncat, double cellSize, double categoryStep, double epsDok, double dpMax,
        AccumulatorLayout layout, Span<long> integerTotals, Span<double> realTotals, PrecisionKind precision,
        out int dpRow, out double[] dpockets, out double[] dokp43, out double[] qdokkarm, out double[][] qdokso)
    {
        var rounding = new CyclePlaneRounding(precision);
        var order = new CyclePlaneOrder(precision);
        var row = (int)Binary32.TruncatedQuotient(dpMax, categoryStep) + 1;
        if (row > ncat)
        {
            dpRow = 0;
            dpockets = Array.Empty<double>();
            dokp43 = Array.Empty<double>();
            qdokkarm = Array.Empty<double>();
            qdokso = Array.Empty<double[]>();
            return CategoriesStatus.CategoryCountExceedsCapacity;
        }

        var dpocketsWorking = new double[ncat];
        for (var r = 0; r < row; r++)
        {
            dpocketsWorking[r] = rounding.Store((r + 1) * categoryStep); // Fortran 828
        }

        var mdok3 = new double[ncat];
        var mdok4 = new double[ncat];
        var epsydok = new double[ncat];
        var dokp43Working = new double[ncat];
        var qdokkarmWorking = new double[ncat];
        var qdoksoWorking = new double[ncat][];
        for (var r = 0; r < ncat; r++)
        {
            qdoksoWorking[r] = new double[ndok];
        }

        double CellCentre(int zeroBasedCell) => cellSize * (zeroBasedCell + 0.5);

        // Span<T> is a ref-like type and cannot be captured by a local function's closure
        // (CS9108); integerTotals/realTotals are threaded through as explicit parameters
        // instead of being captured.
        void MergeRow(int target, int source, Span<long> integers, Span<double> reals)
        {
            dpocketsWorking[target] = dpocketsWorking[source];
            for (var i = 0; i < ndok; i++)
            {
                integers[layout.Qdoks + target * ndok + i] += integers[layout.Qdoks + source * ndok + i];
            }

            reals[layout.Dokp41 + target] = rounding.Store(reals[layout.Dokp41 + target] + reals[layout.Dokp41 + source]); // Fortran 918, 950
            reals[layout.Dokp31 + target] = rounding.Store(reals[layout.Dokp31 + target] + reals[layout.Dokp31 + source]); // Fortran 919, 951
        }

        void ShiftRow(int target, int source, Span<long> integers, Span<double> reals)
        {
            dpocketsWorking[target] = dpocketsWorking[source];
            for (var i = 0; i < ndok; i++)
            {
                integers[layout.Qdoks + target * ndok + i] = integers[layout.Qdoks + source * ndok + i];
            }

            reals[layout.Dokp41 + target] = reals[layout.Dokp41 + source];
            reals[layout.Dokp31 + target] = reals[layout.Dokp31 + source];
        }

        void Recompute(Span<long> integers, Span<double> reals)
        {
            Array.Clear(mdok3, 0, ncat);
            Array.Clear(mdok4, 0, ncat);
            Array.Clear(dokp43Working, 0, ncat);
            Array.Clear(qdokkarmWorking, 0, ncat);
            for (var clearRow = 0; clearRow < ncat; clearRow++)
            {
                Array.Clear(qdoksoWorking[clearRow], 0, ndok);
            }

            for (var r = 0; r < row; r++)
            {
                var rowSum = 0L;
                for (var i = 0; i < ndok; i++)
                {
                    rowSum += integers[layout.Qdoks + r * ndok + i];
                }

                for (var i = 0; i < ndok; i++)
                {
                    // QDOKS1 (848) and QDOKSO (897) are the same quotient, each read by a later block.
                    qdoksoWorking[r][i] = rowSum == 0 ? 0.0 : rounding.Store(integers[layout.Qdoks + r * ndok + i] / (double)rowSum); // Fortran 848, 897
                }

                for (var i = 0; i < ndok; i++)
                {
                    var cell = CellCentre(i);
                    mdok4[r] += order.Fourth(cell) * qdoksoWorking[r][i]; // Fortran 853
                    mdok3[r] += order.Cube(cell) * qdoksoWorking[r][i]; // Fortran 854
                }

                var ddok3 = 0.0;
                var ddok4 = 0.0;
                for (var i = 0; i < ndok; i++)
                {
                    var cell = CellCentre(i);
                    ddok4 += order.Square(order.Fourth(cell) - mdok4[r]) * qdoksoWorking[r][i]; // Fortran 863
                    ddok3 += order.Square(order.Cube(cell) - mdok3[r]) * qdoksoWorking[r][i]; // Fortran 865
                }

                var tiny = rounding.Literal(1e-30); // Fortran 869
                epsydok[r] = rowSum == 0 || mdok3[r] <= tiny || mdok4[r] <= tiny
                    ? 0.0
                    : rounding.Store(3.0 * Math.Sqrt(rounding.Temporary(1.0 / rowSum) * (ddok4 / (mdok4[r] * mdok4[r]) + ddok3 / (mdok3[r] * mdok3[r])))); // Fortran 875

                for (var i = 0; i < ndok; i++)
                {
                    qdokkarmWorking[r] = rounding.Store(qdokkarmWorking[r] + order.Linear(qdoksoWorking[r][i], cellSize, i + 0.5)); // Fortran 899
                }

                var dokp31 = reals[layout.Dokp31 + r];
                dokp43Working[r] = dokp31 <= rounding.Literal(1e-30) ? 0.0 : rounding.Store(reals[layout.Dokp41 + r] / dokp31); // Fortran 902, 905
            }
        }

        while (true)
        {
            Recompute(integerTotals, realTotals);
            if (row <= 1)
            {
                break;
            }

            var shiftFlag = false;
            for (var r = 0; r < row - 1; r++)
            {
                if (!shiftFlag)
                {
                    if (epsydok[r] > epsDok)
                    {
                        MergeRow(r, r + 1, integerTotals, realTotals);
                        shiftFlag = true;
                    }
                }
                else
                {
                    ShiftRow(r, r + 1, integerTotals, realTotals);
                }
            }

            if (!shiftFlag)
            {
                break;
            }

            row -= 1;
        }

        if (row > 1 && epsydok[row - 1] > epsDok)
        {
            MergeRow(row - 2, row - 1, integerTotals, realTotals);
            row -= 1;
        }

        dpRow = row;
        dpockets = dpocketsWorking[..row];
        dokp43 = dokp43Working[..row];
        qdokkarm = qdokkarmWorking[..row];
        qdokso = new double[row][];
        for (var r = 0; r < row; r++)
        {
            qdokso[r] = (double[])qdoksoWorking[r].Clone();
        }

        return CategoriesStatus.Ok;
    }
}
