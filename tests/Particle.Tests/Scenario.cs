using ILGPU;
using ILGPU.Runtime;
using PropStruct.Random;

namespace PropStruct.Particle.Tests;

/// <summary>
/// One constructed attempt: a single-fraction <see cref="ModelSetup"/>/<see cref="FractionTable"/>/
/// <see cref="CycleInputs"/> plus the three buffers <see cref="Attempt.Run"/> writes, all owned and
/// disposed together. Every knob defaults to a permissive value so a test only names the one it is
/// pinning down (BOOT.md, "Order of coding": no stand-in for a real formulation, only the minimum a
/// single scenario needs).
/// </summary>
internal sealed class Scenario : IDisposable
{
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _bounds;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _cumulative;
    private readonly MemoryBuffer1D<byte, Stride1D.Dense> _forming;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _qks1;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _pdoksmall;
    private readonly MemoryBuffer1D<long, Stride1D.Dense> _integerTotals;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _record;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _scratch;

    public ModelSetup Setup { get; }
    public FractionTable Fractions { get; }
    public CycleInputs Cycle { get; }

    private Scenario(
        ModelSetup setup, FractionTable fractions, CycleInputs cycle,
        MemoryBuffer1D<double, Stride1D.Dense> bounds, MemoryBuffer1D<double, Stride1D.Dense> cumulative,
        MemoryBuffer1D<byte, Stride1D.Dense> forming,
        MemoryBuffer1D<double, Stride1D.Dense> qks1, MemoryBuffer1D<double, Stride1D.Dense> pdoksmall,
        MemoryBuffer1D<long, Stride1D.Dense> integerTotals,
        MemoryBuffer1D<double, Stride1D.Dense> record, MemoryBuffer1D<double, Stride1D.Dense> scratch)
    {
        Setup = setup;
        Fractions = fractions;
        Cycle = cycle;
        _bounds = bounds;
        _cumulative = cumulative;
        _forming = forming;
        _qks1 = qks1;
        _pdoksmall = pdoksmall;
        _integerTotals = integerTotals;
        _record = record;
        _scratch = scratch;
    }

    public static Scenario Build(
        Accelerator accelerator, double size, double cellSize, double lambda, double ak3, double ak4, int cycleFlag,
        int neighbourBudget = 200, int pocketRedrawBudget = 200,
        byte pocketForming = 1, double? dmax = null, double dmin = 0.0,
        double ak1 = 0.0, int variant = 1, bool uniformQks1 = true, double dmaxxx = 1e6, int arraySize = 2000)
    {
        var setup = ConstructedModel.Permissive(size, cellSize, lambda, ak3, ak4, neighbourBudget, pocketRedrawBudget, arraySize);
        setup.Dmin = dmin;
        setup.Dmax = dmax ?? size * 10.0;
        setup.Ak1 = ak1;
        setup.Variant = variant;

        var fractions = ConstructedModel.SingleFraction(accelerator, size, size, pocketForming,
            out var bounds, out var cumulative, out var forming);

        CycleInputs cycle;
        MemoryBuffer1D<double, Stride1D.Dense> qks1;
        MemoryBuffer1D<double, Stride1D.Dense> pdoksmall;
        if (cycleFlag == 0)
        {
            cycle = ConstructedModel.CycleZero();
            qks1 = accelerator.Allocate1D<double>(1);
            pdoksmall = accelerator.Allocate1D<double>(1);
        }
        else if (uniformQks1)
        {
            cycle = ConstructedModel.CycleOne(accelerator, setup.Nkarm, setup.Ndok, dmaxxx, out qks1, out pdoksmall);
        }
        else
        {
            qks1 = accelerator.Allocate1D<double>(setup.Nkarm);
            qks1.MemSetToZero();
            pdoksmall = accelerator.Allocate1D<double>(setup.Ndok);
            pdoksmall.MemSetToZero();
            cycle = new CycleInputs { CycleFlag = 1, Dmaxxx = dmaxxx, Qks1 = qks1.View.BaseView, Pdoksmall = pdoksmall.View.BaseView };
        }

        var integerTotals = accelerator.Allocate1D<long>(setup.Layout.IntegerLength);
        integerTotals.MemSetToZero();
        var record = accelerator.Allocate1D<double>(setup.Layout.RecordLength);
        record.MemSetToZero();
        var scratch = accelerator.Allocate1D<double>(setup.Layout.ScratchLength);
        scratch.MemSetToZero();

        return new Scenario(setup, fractions, cycle, bounds, cumulative, forming, qks1, pdoksmall, integerTotals, record, scratch);
    }

    /// <summary>Runs one attempt on the host thread, from the given starting streams.</summary>
    public AttemptOutcome Run(StreamSet streams)
    {
        return Attempt.Run(Setup, Fractions, Cycle, ref streams,
            _integerTotals.View.BaseView, _record.View.BaseView, _scratch.View.BaseView);
    }

    public AttemptOutcome Run(ref StreamSet streams)
    {
        return Attempt.Run(Setup, Fractions, Cycle, ref streams,
            _integerTotals.View.BaseView, _record.View.BaseView, _scratch.View.BaseView);
    }

    public long IntegerTotal(int offset) => _integerTotals.GetAsArray1D()[offset];

    public double Record(int offset) => _record.GetAsArray1D()[offset];

    public double[] AllRecords() => _record.GetAsArray1D();

    public long[] AllIntegerTotals() => _integerTotals.GetAsArray1D();

    public double[] AllScratch() => _scratch.GetAsArray1D();

    public ArrayView<long> IntegerTotalsView => _integerTotals.View.BaseView;

    public ArrayView<double> RecordView => _record.View.BaseView;

    public ArrayView<double> ScratchView => _scratch.View.BaseView;

    public void Dispose()
    {
        _bounds.Dispose();
        _cumulative.Dispose();
        _forming.Dispose();
        _qks1.Dispose();
        _pdoksmall.Dispose();
        _integerTotals.Dispose();
        _record.Dispose();
        _scratch.Dispose();
    }
}
