using ILGPU.Runtime;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L1 of the BOOT.md table: <see cref="Fold.Add"/> is order-fixed, particle 0 first
/// (API.md, "Attempt"), so it gives the same bits whatever order the particles
/// themselves ran in — a parallel run only fixes which slot each particle's own record
/// lands in, never the order <see cref="Fold.Add"/> reads the slots back.
/// </summary>
public class FoldTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public FoldTests(CpuHost host)
    {
        _host = host;
    }

    // fractionCount=1, ndok=1, nkarm=1, ncat=1, nc=1: the smallest layout that still has every
    // field of BOOT.md's "Real-valued record" table, each exactly one double wide.
    private static AccumulatorLayout SmallLayout() => AccumulatorLayout.Create(fractionCount: 1, ndok: 1, nkarm: 1, ncat: 1, nc: 1);

    private static double[] OneFieldRecord(AccumulatorLayout layout, int field, double value)
    {
        var record = new double[layout.RecordLength];
        record[field] = value;
        return record;
    }

    private static double[] MultiFieldRecord(AccumulatorLayout layout, params (int Field, double Value)[] fields)
    {
        var record = new double[layout.RecordLength];
        foreach (var (field, value) in fields)
        {
            record[field] = value;
        }

        return record;
    }

    [Fact]
    public void AddsInParticleOrderNotSomeOtherOrder()
    {
        var layout = SmallLayout();
        var acc = _host.Accelerator;

        // Chosen so left-to-right (particle 0, 1, 2) and right-to-left give different bits:
        // 1e16 absorbs a trailing 1.0 in double precision but not a leading one.
        var forwardExpected = 1e16 + 1.0 + 1.0;   // particle 0, 1, 2
        var reverseExpected = 1.0 + 1.0 + 1e16;    // particle 2, 1, 0
        Assert.NotEqual(BitConverter.DoubleToInt64Bits(forwardExpected), BitConverter.DoubleToInt64Bits(reverseExpected));

        var field = layout.Sd4; // an arbitrary plain-sum scalar field
        var records = new double[3 * layout.RecordLength];
        Array.Copy(OneFieldRecord(layout, field, 1e16), 0, records, 0 * layout.RecordLength, layout.RecordLength);
        Array.Copy(OneFieldRecord(layout, field, 1.0), 0, records, 1 * layout.RecordLength, layout.RecordLength);
        Array.Copy(OneFieldRecord(layout, field, 1.0), 0, records, 2 * layout.RecordLength, layout.RecordLength);

        using var recordsBuffer = acc.Allocate1D(records);
        using var totals = acc.Allocate1D<double>(layout.RecordLength);
        totals.MemSetToZero();

        Fold.Add(layout, recordsBuffer.View.BaseView, particleCount: 3, totals.View.BaseView);

        var actual = totals.GetAsArray1D()[field];
        Assert.Equal(BitConverter.DoubleToInt64Bits(forwardExpected), BitConverter.DoubleToInt64Bits(actual));
    }

    [Fact]
    public void DpMaxAndDpMaxCorTakeTheRunningMaximumNotTheSum()
    {
        // Both max-tracked fields get their own, independent per-particle sequence (DpMax's
        // running maximum is 7.0, never visited by DpMaxCor's own 9.0), plus a plain-sum
        // field (Sd4) in the same records: deleting DpMaxCor's own handling from Fold.Add
        // would fold it as a sum instead (3.0 + 9.0 + ... whatever its own sequence sums to,
        // never 9.0 alone), so this proves both halves — "DpMax and DpMaxCor take the
        // maximum" and "everything else still sums" — in one record set.
        var layout = SmallLayout();
        var acc = _host.Accelerator;

        var records = new double[3 * layout.RecordLength];
        Array.Copy(
            MultiFieldRecord(layout, (layout.DpMax, 3.0), (layout.DpMaxCor, 9.0), (layout.Sd4, 1.0)),
            0, records, 0 * layout.RecordLength, layout.RecordLength);
        Array.Copy(
            MultiFieldRecord(layout, (layout.DpMax, 7.0), (layout.DpMaxCor, 2.0), (layout.Sd4, 2.0)),
            0, records, 1 * layout.RecordLength, layout.RecordLength);
        Array.Copy(
            MultiFieldRecord(layout, (layout.DpMax, 5.0), (layout.DpMaxCor, 4.0), (layout.Sd4, 3.0)),
            0, records, 2 * layout.RecordLength, layout.RecordLength);

        using var recordsBuffer = acc.Allocate1D(records);
        using var totals = acc.Allocate1D<double>(layout.RecordLength);
        totals.MemSetToZero();

        Fold.Add(layout, recordsBuffer.View.BaseView, particleCount: 3, totals.View.BaseView);

        var actual = totals.GetAsArray1D();
        Assert.Equal(7.0, actual[layout.DpMax]);
        Assert.Equal(9.0, actual[layout.DpMaxCor]);
        Assert.Equal(6.0, actual[layout.Sd4]);
    }

    [Fact]
    public void GivesTheSameTotalsWhicheverScheduleFilledEachParticlesOwnSlot()
    {
        // A parallel run only ever changes *when* each particle's own slot is written, never
        // which slot it lands in (root BOOT.md, "Deterministic under every schedule"): filling
        // the buffer slot-2-then-0-then-1 instead of 0-then-1-then-2 leaves the same content at
        // the same indices, so Fold.Add — which only ever reads by index — must agree exactly.
        var layout = SmallLayout();
        var acc = _host.Accelerator;
        var field = layout.Sd4;
        var values = new[] { 1e16, 1.0, 1.0 };

        var recordsInOrder = new double[3 * layout.RecordLength];
        for (var i = 0; i < 3; i++)
        {
            Array.Copy(OneFieldRecord(layout, field, values[i]), 0, recordsInOrder, i * layout.RecordLength, layout.RecordLength);
        }

        using var recordsInOrderBuffer = acc.Allocate1D(recordsInOrder);
        using var totalsInOrder = acc.Allocate1D<double>(layout.RecordLength);
        totalsInOrder.MemSetToZero();
        Fold.Add(layout, recordsInOrderBuffer.View.BaseView, particleCount: 3, totalsInOrder.View.BaseView);

        var recordsOutOfOrder = new double[3 * layout.RecordLength];
        var fillOrder = new[] { 2, 0, 1 };
        foreach (var slot in fillOrder)
        {
            Array.Copy(OneFieldRecord(layout, field, values[slot]), 0, recordsOutOfOrder, slot * layout.RecordLength, layout.RecordLength);
        }

        using var recordsOutOfOrderBuffer = acc.Allocate1D(recordsOutOfOrder);
        using var totalsOutOfOrder = acc.Allocate1D<double>(layout.RecordLength);
        totalsOutOfOrder.MemSetToZero();
        Fold.Add(layout, recordsOutOfOrderBuffer.View.BaseView, particleCount: 3, totalsOutOfOrder.View.BaseView);

        Assert.Equal(
            BitConverter.DoubleToInt64Bits(totalsInOrder.GetAsArray1D()[field]),
            BitConverter.DoubleToInt64Bits(totalsOutOfOrder.GetAsArray1D()[field]));
    }

    [Fact]
    public void AddsOntoWhateverTotalsAlreadyHeld()
    {
        // Totals persist across cycles/batches (`src/Particle/BOOT.md`, "Totals between cycles"): Add
        // must accumulate on top of a non-zero starting total, not reset it.
        var layout = SmallLayout();
        var acc = _host.Accelerator;
        var field = layout.Sd4;

        using var records = acc.Allocate1D(OneFieldRecord(layout, field, 2.0));
        var startingTotals = new double[layout.RecordLength];
        startingTotals[field] = 10.0;
        using var totals = acc.Allocate1D(startingTotals);

        Fold.Add(layout, records.View.BaseView, particleCount: 1, totals.View.BaseView);

        Assert.Equal(12.0, totals.GetAsArray1D()[field]);
    }
}
