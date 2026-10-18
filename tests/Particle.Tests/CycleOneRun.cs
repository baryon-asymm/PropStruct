using ILGPU;
using ILGPU.Runtime;
using PropStruct.Random;
using PropStruct.Statistics;

namespace PropStruct.Particle.Tests;

/// <summary>
/// A reference formulation taken through its cycle 0 and the statistics between the
/// cycles, then run attempt by attempt in cycle 1 exactly as reference mode does (root
/// BOOT.md, "Reference mode is the original's sequence"): the six streams of the
/// <c>Original</c> layout continued across particles and cycles, one record per base
/// particle, reset after an accepted attempt, QKS1 refreshed
/// (<see cref="PocketHistogram.Normalize"/>) after every attempt that completes the
/// neighbour loop, the integer totals never reset. The only calls are the published
/// members of <c>Particle</c> and <c>Statistics</c> (no formula is repeated here); the
/// apparatus is that of <c>src/Particle/HISTORY.md#window-cycle1-measurement-2026-10-02</c>.
/// </summary>
internal sealed class CycleOneRun : IDisposable
{
    /// <summary>
    /// The particles of cycle 0 this driver runs: enough for a QKS1 and a <c>pdoksmall</c>
    /// that reach every branch of cycle 1 (<c>CycleOneSnapshotTests</c> checks that they do),
    /// not the formulation's own <c>N</c>, which would take minutes per case.
    /// </summary>
    private const int CycleZeroParticles = 5_000;

    private readonly MemoryBuffer1D<double, Stride1D.Dense> _bounds;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _cumulative;
    private readonly MemoryBuffer1D<byte, Stride1D.Dense> _forming;
    private readonly MemoryBuffer1D<long, Stride1D.Dense> _integerTotals;
    private readonly MemoryBuffer1D<double, Stride1D.Dense> _realTotals;
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
        _realTotals = accelerator.Allocate1D<double>(setup.Layout.RecordLength);
        _record = accelerator.Allocate1D<double>(setup.Layout.RecordLength);
        _scratch = accelerator.Allocate1D<double>(setup.Layout.ScratchLength);
        _qks1 = accelerator.Allocate1D<double>(setup.Nkarm);
        _pdoksmall = accelerator.Allocate1D<double>(setup.Ndok);
        _integerTotals.MemSetToZero();
        _realTotals.MemSetToZero();
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
    /// <paramref name="corrections"/> set in <c>ModelSetup.Corrections</c> and runs its cycle 0
    /// (<see cref="CycleZeroParticles"/> accepted particles), then the statistics between the
    /// cycles, leaving the run ready for the first attempt of cycle 1.
    /// </summary>
    public static CycleOneRun TakeToCycleOne(
        Accelerator accelerator, string formulation, PrecisionKind kind, CorrectionSet corrections = CorrectionSet.None)
    {
        var setup = ReferenceFormulation.Prepare(
            accelerator, formulation, kind, out var read, out var tables, out var echoes, out var inputs);
        setup.Corrections = corrections;

        var run = new CycleOneRun(accelerator, setup, tables);
        try
        {
            run.RunCycleZero(Math.Min(read.ParticlesPerCycle, CycleZeroParticles));

            var integerTotals = run._integerTotals.GetAsArray1D();
            var realTotals = run._realTotals.GetAsArray1D();
            var nextPdoksmall = new double[setup.Ndok];
            _ = CycleStatistics.Compute(
                setup, tables, echoes, inputs, 0, integerTotals, realTotals, nextPdoksmall,
                out var nextDmaxxx, out var categories);

            if (categories != CategoriesStatus.Ok)
            {
                throw new InvalidOperationException($"{formulation}: cycle 0 statistics returned {categories}.");
            }

            run._integerTotals.CopyFromCPU(integerTotals);
            run._realTotals.CopyFromCPU(realTotals);
            run._pdoksmall.CopyFromCPU(nextPdoksmall);
            PocketHistogram.Normalize(setup.Layout, run._integerTotals.View.BaseView, run._qks1.View.BaseView);
            run._cycle = new CycleInputs
            {
                CycleFlag = 1,
                Dmaxxx = nextDmaxxx,
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
        _realTotals.Dispose();
        _record.Dispose();
        _scratch.Dispose();
        _qks1.Dispose();
        _pdoksmall.Dispose();
    }

    private void RunCycleZero(int particleCount)
    {
        for (var particle = 0; particle < particleCount; particle++)
        {
            AttemptOutcome outcome;
            do
            {
                outcome = Advance();
            }
            while (outcome != AttemptOutcome.Accepted);

            Fold.Add(Setup.Layout, _record.View.BaseView, 1, _realTotals.View.BaseView);
            _record.MemSetToZero();
        }
    }

    private AttemptOutcome Advance()
    {
        var outcome = Attempt.Run(
            Setup, _fractions, _cycle, ref _streams,
            _integerTotals.View.BaseView, _record.View.BaseView, _scratch.View.BaseView);

        switch (outcome)
        {
            case AttemptOutcome.Accepted:
            case AttemptOutcome.RestartedAfterLoop:
                PocketHistogram.Normalize(Setup.Layout, _integerTotals.View.BaseView, _qks1.View.BaseView);
                break;
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
