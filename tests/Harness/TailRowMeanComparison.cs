namespace PropStruct.Tests.Harness;

/// <summary>
/// `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 2: the adaptive tail's own row-mean vector — one
/// Ndok-length "tail row mean" per source, the unweighted mean of its own <c>fqdokkarm(&lt;row&gt;,:)</c> rows
/// past <c>i0</c>, the reference's own prefix length, one fixed cut for every source (F-b,
/// `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`: not each source's own) — and the cells built from
/// it for <see cref="StatisticalCriterion.CompareTailRowMean"/>. Split out of
/// <c>StatisticalCriterion.Comparisons.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class TailRowMeanComparison
{
    // `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 2: "the mean over each source's own adaptive
    // rows, from the first adaptive row to the end of that source's own axis" — one Ndok-length vector per
    // source, or `null` when the source's own Dkarmcat never reaches `i0` (no adaptive row exists in that source
    // at all), the reference's own prefix length, one fixed cut for every source (F-b,
    // `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`: not each source's own). A source contributes
    // every `fqdokkarm(<row>,:)` row it has printed at a 1-based row number greater than `i0`; a row named in
    // [i0+1, ownDkarmcatLength] that the source did not print (should not happen in practice, since fqdokkarm
    // rows are printed contiguously) is simply absent from the running sum, not defaulted to zero, so a
    // genuinely missing row does not pull the mean toward zero.
    internal static double[]? TailRowMean(IReadOnlyDictionary<string, double[]> source, int i0)
    {
        var axisLength = source.TryGetValue("Dkarmcat", out var axis) ? axis.Length : 0;
        if (axisLength <= i0)
        {
            return null;
        }

        double[]? sum = null;
        var rows = 0;
        for (var row = i0 + 1; row <= axisLength; row++)
        {
            if (!source.TryGetValue($"fqdokkarm({row},:)", out var values))
            {
                continue;
            }

            if (sum is null)
            {
                sum = new double[values.Length];
            }
            else if (values.Length != sum.Length)
            {
                // A row narrower or wider than the first contributing row of this same source would silently
                // bias the shared columns; this has never been observed (every row of one source's own
                // fqdokkarm shares one width, this node's BOOT.md), so it is a stop, not a truncation.
                throw new InvalidOperationException(
                    $"fqdokkarm({row},:) has {values.Length} columns, but this source's tail rows started at " +
                    $"{sum.Length} (tests/Harness/HISTORY.md#tail-coverage-2026-09-18, step 2).");
            }

            for (var i = 0; i < values.Length; i++)
            {
                sum[i] += values[i];
            }

            rows++;
        }

        if (sum is null || rows == 0)
        {
            return null;
        }

        for (var i = 0; i < sum.Length; i++)
        {
            sum[i] /= rows;
        }

        return sum;
    }

    internal static (List<PendingCell> Pending, int Excluded) BuildTailRowMeanPending(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var replicaCount = FixtureReplicas.CountOf(formulation);

        var referenceCells = ResultsMFile.ParseCells(FixtureReplicas.ReferencePath(formulation));
        var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
            ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
            : [];
        var i0 = CategoryAxis.FixedWidthPrefixLength(referenceDkarmcat);

        // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #3: TailRowMean is an unweighted mean of one
        // source's own `fqdokkarm(<row>,:)` cells, so it inherits that family's own print resolution as its
        // floor — read once, from any `fqdokkarm` row the reference printed (`InferDecimalDigits`), the same
        // way `Compare` reads an array's own format for its synthetic candidate cells (above).
        int? tailRowMeanDecimalDigits = null;
        foreach (var (key, cells) in referenceCells)
        {
            if (!QuantityFamilies.TryMatchFqDokKarmRow(key, out _))
            {
                continue;
            }

            tailRowMeanDecimalDigits = PrintResolution.InferDecimalDigits(cells);
            if (tailRowMeanDecimalDigits is not null)
            {
                break;
            }
        }

        var replicas = FixtureReplicas.LoadReplicas(formulation, kind, replicaCount, excludeReplicaOrdinal);
        var candidateMean = TailRowMean(candidate, i0);
        var replicaMeans = replicas.Select(r => TailRowMean(r, i0)).Where(v => v is not null).Select(v => v!).ToArray();

        var pending = new List<PendingCell>();
        var excluded = 0;
        var width = candidateMean?.Length ?? replicaMeans.FirstOrDefault()?.Length ?? 0;
        if (candidateMean is null || replicaMeans.Length < 2)
        {
            excluded += width;
        }
        else
        {
            for (var index = 0; index < width; index++)
            {
                var replicaValues = replicaMeans.Where(v => index < v.Length).Select(v => v[index]).ToArray();
                if (replicaValues.Length < 2)
                {
                    excluded++;
                    continue;
                }

                var cand = index < candidateMean.Length ? candidateMean[index] : 0.0;

                // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #1: the sparse-cell exclusion is general
                // to every non-count-like cell ("not only the mass families") — TailRowMean is exactly that
                // (isDistributionFunction: false below), so a column where fewer than two of the pool's own
                // means are non-zero carries no scale to judge a candidate against, on the same exchangeability
                // argument as the mass families. Measured, HPEPA3 replica 11's own TailRowMean[2]: 31 lagged
                // replicas all read 0.0 at this column (zero non-zero, not merely "sparse"), and #3's own real
                // floor (`1E-10`, below) turned what had been a flaky pass/fail on a summed quantity's last-bit
                // codegen (this node's BOOT.md, ⚠ 2026-09-19 above) into a deterministic fail instead of the
                // exclusion this rule calls for; without this check #3's fix would have swapped one defect (a
                // codegen-dependent comparison) for another (a comparison with no scale behind it at all).
                if (replicaValues.Count(v => v != 0.0) < 2)
                {
                    excluded++;
                    continue;
                }

                // TailRowMean is a *mean* of rows, not a count, so the count-like floor does not apply to it
                // (isDistributionFunction: false,
                // `tests/Harness/HISTORY.md#the-candidate-is-never-its-own-witness`): only the ordinary Student
                // band, plus the print-resolution floor below, governs this cell.
                //
                // ⚠ 2026-09-19: this call first passed isDistributionFunction: true (fqdokkarm's own rows are
                // count-like, and this looked like "a mean of count-like rows is still count-like"). The oracle
                // mutation exercise found the opposite: because TailRowMean is a *derived* value, not a printed
                // token, its own magnitude can be driven past 2^52 by a uniform per-row shift, at which point
                // `Math.Round` of any ratio built from it returns the value itself bit for bit and the
                // quantum-consistency check accepted the candidate against any quantum trivially --
                // `tests/Harness/HISTORY.md#oracle-mutation-2026-09-19`'s second finding. Turning the count
                // floor off for this derived quantity removes the escape at its root, not just at the magnitude
                // this exercise measured.
                //
                // ⚠ 2026-09-19, later the same day: the cell was still given `default` here (resolution `0`),
                // leaving a cell whose replica mean and spread are exactly zero with a threshold of exactly `0`
                // — a comparison a summed quantity's own last-bit codegen could then decide, not the criterion
                // (`tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #3, has the measured HPEPA3 case). The
                // resolution field below (`fqdokkarm`'s own format, applied to this cell's own candidate value)
                // gives every such cell a real, positive floor instead.
                var resolution = tailRowMeanDecimalDigits is { } digits ? PrintResolution.ResolutionFromDecimalDigits(cand, digits) : 0.0;
                pending.Add(PendingCell.From("TailRowMean", index, new ResultCell(cand, resolution, false), cand, replicaValues, zeroBelow: false, isDistributionFunction: false));
            }
        }

        return (pending, excluded);
    }
}
