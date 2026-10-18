using ILGPU;
using ILGPU.Runtime;
using PropStruct.Random;
using PropStruct.Statistics;

namespace PropStruct.Particle.Tests;

/// <summary>
/// A reference formulation run through <see cref="Attempt.Run"/> on the sequence of
/// <see cref="AccumulatorSweep"/> (<c>ACCEPTANCE.md</c>, "<c>None</c> reproduces the
/// original bit for bit beyond cycle 0"): <see cref="CycleZeroAttempts"/> attempts of cycle 0
/// with the streams of the <c>Original</c> layout continued, then cycle 1 with QKS1
/// normalized from the cycle-0 totals and refreshed
/// (<see cref="PocketHistogram.Normalize"/>) after every attempt that completes the neighbour
/// loop, <c>pdoksmall</c> zero and <c>Dmaxxx</c> 1e30, so the window, <c>VM</c>, both Var#2
/// branches and the tests of lines 723-738 all run. One record per base particle, reset after
/// an accepted attempt, the integer totals never reset. The only calls are the published
/// members of <c>Particle</c>; no <c>Statistics</c> cycle statistics are involved.
/// </summary>
internal sealed class CycleOneRun : IDisposable
{
    /// <summary>The attempts of cycle 0 that build the pocket histogram cycle 1 reads.</summary>
    private const int CycleZeroAttempts = 4_000;

    private readonly MemoryBuffer1D<double, Stride1D.Dense> _bounds;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _cumulative;
    private readonly MemoryBuffer1D<byte, Stride1D.Dense> _forming;
    private readonly MemoryBuffer1D<long, Stride1D.Dense> _integerTotals;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _record;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _scratch;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _qks1;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _pdoksmall;
    private readonly FractionTable _fractions;
    private StreamSet _streams = OriginalSeeds.Streams;
    private CycleInputs _cycle = ConstructedModel.CycleZero();

    private CycleOneRun(Accelerator accelerator, ModelSetup setup, SetupTables tables)
    {
        Setup = setup;
        _bounds = accelerator.Allocate1D(tables.Bounds);
        _cumulative = accelerator.Allocate1D(tables.Cumulative);
        _forming = accelerator.Allocate1D(tables.PocketForming);
        _integerTotals = accelerator.Allocate1D<long>(setup.Layout.IntegerLength);
        _record = accelerator.Allocate1D<double>(setup.Layout.RecordLength);
        _scratch = accelerator.Allocate1D<double>(setup.Layout.ScratchLength);
        _qks1 = accelerator.Allocate1D<double>(setup.Nkarm);
        _pdoksmall = accelerator.Allocate1D<double>(setup.Ndok);
        _integerTotals.MemSetToZero();
        _record.MemSetToZero();
        _scratch.MemSetToZero();
        _qks1.MemSetToZero();
        _pdoksmall.MemSetToZero();
        _fractions = new FractionTable
        {
            Bounds = _bounds.View.BaseView,
            Cumulative = _cumulative.View.BaseView,
            PocketForming = _forming.View.BaseView,
        };
    }

    /// <summary>The setup the run was prepared with.</summary>
    public ModelSetup Setup { get; }

    /// <summary>
    /// Prepares <paramref name="formulation"/> under <paramref name="kind"/> with
    /// <paramref name="corrections"/> set in <c>ModelSetup.Corrections</c> and runs its cycle 0,
    /// leaving the run ready for the first attempt of cycle 1.
    /// </summary>
    public static CycleOneRun TakeToCycleOne(
        Accelerator accelerator, string formulation, PrecisionKind kind, CorrectionSet corrections = CorrectionSet.None)
    {
        var setup = ReferenceFormulation.Prepare(accelerator, formulation, kind, out var tables, out _);
        setup.Corrections = corrections;

        var run = new CycleOneRun(accelerator, setup, tables);
        try
        {
            for (var attempt = 0; attempt < CycleZeroAttempts; attempt++)
            {
                _ = run.Advance();
            }

            PocketHistogram.Normalize(setup.Layout, run._integerTotals.View.BaseView, run._qks1.View.BaseView);
            run._cycle = new CycleInputs
            {
                CycleFlag = 1,
                Dmaxxx = 1e30,
                Qks1 = run._qks1.View.BaseView,
                Pdoksmall = run._pdoksmall.View.BaseView,
            };
            return run;
        }
        catch
        {
            run.Dispose();
            throw;
        }
    }

    /// <summary>
    /// One attempt of cycle 1 in reference mode: what it left (outcome, streams, integer totals,
    /// the particle's record) before an accepted attempt's record is reset.
    /// </summary>
    public AttemptTrace Step()
    {
        var outcome = Advance();
        if (outcome is AttemptOutcome.Accepted or AttemptOutcome.RestartedAfterLoop)
        {
            PocketHistogram.Normalize(Setup.Layout, _integerTotals.View.BaseView, _qks1.View.BaseView);
        }

        var trace = new AttemptTrace(outcome, _streams, _integerTotals.GetAsArray1D(), _record.GetAsArray1D());
        if (outcome == AttemptOutcome.Accepted)
        {
            _record.MemSetToZero();
        }

        return trace;
    }

    public void Dispose()
    {
        _bounds.Dispose();
        _cumulative.Dispose();
        _forming.Dispose();
        _integerTotals.Dispose();
        _record.Dispose();
        _scratch.Dispose();
        _qks1.Dispose();
        _pdoksmall.Dispose();
    }

    // One attempt of the current cycle; in cycle 0 an accepted attempt resets the record here,
    // in cycle 1 Step does it after taking the trace.
    private AttemptOutcome Advance()
    {
        var outcome = Attempt.Run(
            Setup, _fractions, _cycle, ref _streams,
            _integerTotals.View.BaseView, _record.View.BaseView, _scratch.View.BaseView);

        switch (outcome)
        {
            case AttemptOutcome.Accepted:
                if (_cycle.CycleFlag == 0)
                {
                    _record.MemSetToZero();
                }

                break;
            case AttemptOutcome.RestartedAfterLoop:
            case AttemptOutcome.RestartedInsideLoop:
                break;
            case AttemptOutcome.NeighbourBudgetExceeded:
            case AttemptOutcome.BridgeDrawBudgetExceeded:
            case AttemptOutcome.IndexOutOfRange:
            default:
                throw new InvalidOperationException($"The reference run ended an attempt with {outcome}.");
        }

        return outcome;
    }
}
