using ILGPU;

namespace PropStruct.Particle;

/// <summary>
/// Folds a batch of particle records into the run's real-valued totals, particle 0
/// first (BOOT.md, "Fold"; API.md, "Attempt"). The same particle order gives the same
/// bits on any accelerator whose <see cref="double"/> addition is IEEE, whatever order
/// the particles themselves ran in.
/// </summary>
internal static class Fold
{
    /// <summary>
    /// Adds <paramref name="records"/>[0 .. <paramref name="particleCount"/>) into
    /// <paramref name="realTotals"/> in that order: every field sums by addition except
    /// <c>DpMax</c> and <c>DpMaxCor</c>, which take the running maximum (BOOT.md,
    /// "Real-valued record"). <paramref name="realTotals"/> keeps whatever it already
    /// held (the run's totals persist across cycles, this node's own BOOT.md, "Totals
    /// between cycles"): this call only adds the batch on top. <see cref="Add"/> is
    /// <see cref="AddField"/> over every field of the record, in field order, so there is
    /// one implementation of the fold (API.md, "Fold by field").
    /// </summary>
    public static void Add(in AccumulatorLayout layout, ArrayView<double> records, int particleCount, ArrayView<double> realTotals)
    {
        var recordLength = layout.RecordLength;
        for (var field = 0; field < recordLength; field++)
        {
            AddField(in layout, records, particleCount, field, realTotals);
        }
    }

    /// <summary>
    /// One field of <see cref="Add"/>: adds field <paramref name="field"/> of
    /// <paramref name="records"/>[0 .. <paramref name="particleCount"/>) into
    /// <paramref name="realTotals"/>[<paramref name="field"/>] in particle order, with the
    /// same rule as <see cref="Add"/> (maximum for <c>DpMax</c> and <c>DpMaxCor</c>, sum
    /// otherwise). The fold kernel of <c>Execution</c> runs one thread per field, each
    /// calling this member, so the same particle order gives the same bits whatever the
    /// accelerator's thread count (API.md, "Fold by field").
    /// </summary>
    public static void AddField(in AccumulatorLayout layout, ArrayView<double> records, int particleCount, int field, ArrayView<double> realTotals)
    {
        var recordLength = layout.RecordLength;
        var isMaximum = field == layout.DpMax || field == layout.DpMaxCor;

        var total = realTotals[field];
        for (var particle = 0; particle < particleCount; particle++)
        {
            var value = records[particle * recordLength + field];
            total = isMaximum ? Math.Max(total, value) : total + value;
        }

        realTotals[field] = total;
    }
}
