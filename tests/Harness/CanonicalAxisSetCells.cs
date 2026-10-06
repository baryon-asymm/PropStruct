namespace PropStruct.Tests.Harness;

/// <summary>
/// this node's BOOT.md, "## Set comparison": puts one run's four canonical-axis families (<c>Dkarmcat</c>/
/// <c>dokkarm43</c>/<c>dokkarm10</c>/<c>fqdokkarm(&lt;row&gt;,:)</c>) on the reference's own canonical axis, and
/// adds that run's own adaptive-tail summaries (<c>TailRowMean</c>, <c>AdaptiveRowCount</c>,
/// <c>AdaptiveLastBoundaryLog</c>), for <see cref="SetComparison"/>. A run's raw axis keys are replaced, never
/// kept alongside their replacement: <c>Dkarmcat</c>/<c>dokkarm43</c>/<c>dokkarm10</c> are truncated to the
/// reference's own fixed-width prefix (an ordinary, zero-fillable array from here on — this node's BOOT.md, "##
/// Set comparison"'s own class table, row 3) and every entry past it moves to a sibling <c>@adaptive</c> key,
/// sliced to this run's own eligible length (never zero-filled here: a position past this run's own reach is not
/// this run's zero, and <see cref="SetComparison"/> leaves an <c>@adaptive</c> key out of its own union zero-fill
/// so a short run's own absence stays absence). A run's own <c>fqdokkarm(&lt;row&gt;,:)</c> row is kept, whole
/// and unchanged, only when this run's own prefix match reaches that row; dropped otherwise — <c>SetComparison</c>'s
/// own union zero-fill then pads a kept row only to the width of the OTHER runs that also kept it, exactly "the
/// contributing width" this node's BOOT.md names, because a run that never kept the row never enters that union.
/// </summary>
internal static class CanonicalAxisSetCells
{
    private static readonly string[] AxisArrays = ["Dkarmcat", "dokkarm43", "dokkarm10"];

    internal static Dictionary<string, double[]> Augment(IReadOnlyDictionary<string, double[]> run, int i0, int prefixMatch)
    {
        var augmented = new Dictionary<string, double[]>(StringComparer.Ordinal);
        foreach (var (name, values) in run)
        {
            if (AxisArrays.Contains(name, StringComparer.Ordinal) || QuantityFamilies.TryMatchFqDokKarmRow(name, out _))
            {
                continue;
            }

            augmented[name] = values;
        }

        foreach (var name in AxisArrays)
        {
            if (!run.TryGetValue(name, out var values))
            {
                continue;
            }

            var prefixLength = Math.Min(i0, values.Length);
            if (prefixLength > 0)
            {
                augmented[name] = values[..prefixLength];
            }

            var eligibleLength = Math.Max(0, Math.Min(values.Length, prefixMatch) - i0);
            if (eligibleLength > 0)
            {
                augmented[$"{name}@adaptive"] = values[i0..(i0 + eligibleLength)];
            }
        }

        foreach (var (name, values) in run)
        {
            if (QuantityFamilies.TryMatchFqDokKarmRow(name, out var row1Based) && prefixMatch >= row1Based)
            {
                augmented[name] = values;
            }
        }

        var dkarmcat = run.GetValueOrDefault("Dkarmcat");
        var tailRowMean = TailRowMeanComparison.TailRowMean(run, i0);
        if (tailRowMean is not null)
        {
            augmented["TailRowMean"] = tailRowMean;
        }

        if (AdaptiveIndexMatchedComparison.AdaptiveRowCountOf(dkarmcat, i0) is { } rowCount)
        {
            augmented["AdaptiveRowCount"] = [rowCount];
        }

        if (AdaptiveIndexMatchedComparison.LastBoundaryLogOf(dkarmcat) is { } lastBoundaryLog)
        {
            augmented["AdaptiveLastBoundaryLog"] = [lastBoundaryLog];
        }

        return augmented;
    }
}
