using System.Collections.Immutable;
using System.Diagnostics;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Random;
using PropStruct.Statistics;

namespace PropStruct.Simulation;

/// <summary>
/// The cycle loop of the original (label 700, root BOOT.md, "Purpose"): cycle 0 as the warm-up, cycles
/// 1..KXX following, batches (or, in reference mode, single particles) alternating with the statistics of
/// <c>Statistics</c> between cycles. Turns a failure status into <see cref="SimulationFailedException"/>
/// (API.md, "## Errors"). The exact step-by-step order is BOOT.md, "Design decisions (2026-09-18)", "The
/// run, step by step".
/// </summary>
public sealed class Simulator : IDisposable
{
    private readonly SimulationOptions _options;
    private readonly Engine _engine;
    private bool _disposed;

    private Simulator(SimulationOptions options, Engine engine)
    {
        _options = options;
        _engine = engine;
    }

    /// <summary>
    /// Validates <paramref name="options"/> and creates the accelerator before any model work
    /// (API.md, "## Errors": "invalid options ... ArgumentException before any work";
    /// <see cref="SimulationOptionsValidator"/>, architecture audit finding R1, 2026-09-24: every
    /// option-combination rule, including the two that used to surface only from deep inside
    /// <c>Execution.Engine.RunBatch</c>/<c>RunContinuedBatch</c> after <c>Statistics.Setup.Prepare</c>,
    /// the tail draw and <c>Engine.Load</c> had already run). Reference mode needs a CPU engine:
    /// <see cref="AcceleratorKind.Auto"/> resolves to <see cref="AcceleratorKind.Cpu"/> there, and
    /// <see cref="AcceleratorKind.Cuda"/> with <see cref="ExecutionMode.Reference"/> is rejected here, before
    /// an engine is even created (BOOT.md, "Design decisions (2026-09-18)", "Accelerator by mode").
    /// </summary>
    public static Simulator Create(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        SimulationOptionsValidator.Validate(options);

        var kind = options.Mode == ExecutionMode.Reference
            ? (options.Accelerator == AcceleratorKind.Auto ? AcceleratorKind.Cpu : options.Accelerator)
            : options.Accelerator;

        var engine = Engine.Create(kind, options.RecordBudgetBytes);
        return new Simulator(options, engine);
    }

    /// <summary>The accelerator this simulator's engine was created on (root BOOT.md, "The CPU path needs no NVIDIA software"); fixed for the simulator's lifetime.</summary>
    public AcceleratorInfo Accelerator => _engine.Accelerator;

    /// <summary>Runs <paramref name="formulation"/> to completion with no progress reporting and no cancellation.</summary>
    public SimulationResult Run(Formulation formulation) => Run(formulation, progress: null, CancellationToken.None);

    /// <summary>As <see cref="Run(Formulation)"/>, reporting a <see cref="CycleProgress"/> after every cycle.</summary>
    public SimulationResult Run(Formulation formulation, IProgress<CycleProgress> progress) =>
        Run(formulation, progress, CancellationToken.None);

    /// <summary>
    /// Runs the cycle loop for <paramref name="formulation"/> (this type's own summary), reporting to
    /// <paramref name="progress"/> when given and observing <paramref name="cancellationToken"/> between
    /// cycles.
    /// </summary>
    /// <exception cref="SimulationFailedException">A batch, the setup or the category merge ended with a status other than <see cref="RunStatus.Ok"/> (API.md, "## Errors").</exception>
    /// <exception cref="InvalidOperationException">Execution reported <see cref="BatchStatus.ParticleNotRun"/>, a defect of the engine and no run status (decided 2026-10-02).</exception>
    public SimulationResult Run(Formulation formulation, IProgress<CycleProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formulation);
        var stopwatch = Stopwatch.StartNew();

        var prepareStatus = Setup.Prepare(
            formulation, _options.Parameters, _options.Precision.ToParticle(), _options.NeighbourBudget, _options.PocketRedrawBudget,
            out var setup, out var tables, out var draw, out var pending, out var inputs);
        if (prepareStatus != SetupStatus.Ok)
        {
            throw new SimulationFailedException(
                RunStatus.InvalidSetup, $"Statistics.Setup.Prepare failed: {prepareStatus}.", prepareStatus.ToSetupFailureReason());
        }

        _engine.SampleSize(setup.SizeLaw, setup.FractionCount, tables.Bounds, tables.Cumulative, draw.X, draw.X1, out var dmax, out _);
        var echoes = Setup.CompleteEchoes(ref setup, pending, dmax);

        // Setup.Prepare already sets setup.Kind from _options.Precision (audit R2: this node used to patch
        // it here, a second store of the same value CompleteEchoes above already needs through setup.Kind
        // to round Dmax correctly).
        var storedSetup = new StoredSetup(
            setup.CellSize, setup.CategoryStep, setup.Dmin,
            inputs.OxidizerDensity, inputs.PropellantDensity, inputs.OxidizerMassFraction, inputs.MetalMassFraction,
            setup.Alpha, setup.NnMin, setup.NnMax, setup.PocketCoefficient, setup.BridgeCoefficient,
            inputs.HomogenizedOxidizerFraction, inputs.EpsDok,
            tables.MassShare.ToImmutableArray(), tables.Bounds.ToImmutableArray());

        _engine.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);

        var particlesPerCycle = formulation.ParticlesPerCycle;
        var cycles = formulation.Cycles;
        var batchSize = Math.Min(_options.BatchSize ?? particlesPerCycle, _engine.MaxBatchSize);

        var randomLayout = _options.Streams.ToRandom();
        var continuedStreams = StreamSeeds.ForParticle(randomLayout, _options.Seed, ordinal: 0UL);

        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        var pdoksmall = new double[setup.Ndok];
        var dmaxxx = 0.0;

        long totalAttempts = 0;
        long totalLaunches = 0;
        long acceptedParticles = 0;

        CycleReport? lastReport = null;
        var epsySeries = ImmutableArray.CreateBuilder<double>();
        var epsmd3Series = ImmutableArray.CreateBuilder<double>();
        var epsmd4Series = ImmutableArray.CreateBuilder<double>();
        var alldok43Series = ImmutableArray.CreateBuilder<double>();
        var alldoksdSeries = ImmutableArray.CreateBuilder<double>();
        var dolM2Series = ImmutableArray.CreateBuilder<double>();

        for (var cycle = 0; cycle <= cycles; cycle++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _engine.SetCycle(cycleFlag: cycle == 0 ? 0 : 1, dmaxxx, pdoksmall);

            if (_options.Mode == ExecutionMode.Reference)
            {
                RunReferenceCycle(cycle, cycles, particlesPerCycle, ref continuedStreams, ref acceptedParticles,
                    ref totalAttempts, ref totalLaunches, progress, cancellationToken);
            }
            else if (_options.ContinuedStreams)
            {
                RunContinuedBatchCycle(cycle, cycles, particlesPerCycle, ref continuedStreams, ref acceptedParticles,
                    ref totalAttempts, ref totalLaunches, progress, cancellationToken);
            }
            else
            {
                RunBatchedCycle(cycle, cycles, particlesPerCycle, batchSize, randomLayout, ref acceptedParticles,
                    ref totalAttempts, ref totalLaunches, progress, cancellationToken);
            }

            _engine.ReadTotals(integerTotals, realTotals);
            var nextPdoksmall = new double[setup.Ndok];
            var report = CycleStatistics.Compute(
                in setup, tables, echoes, inputs, cycleIndex: cycle,
                integerTotals, realTotals, nextPdoksmall, out var nextDmaxxx, out var categoriesStatus);
            if (categoriesStatus != CategoriesStatus.Ok)
            {
                throw new SimulationFailedException(RunStatus.CategoryCountExceedsCapacity);
            }

            _engine.WriteTotals(integerTotals, realTotals);

            lastReport = report;
            pdoksmall = nextPdoksmall;
            dmaxxx = nextDmaxxx;

            if (cycle >= 1 && cycles > 1)
            {
                epsySeries.Add(report.ConvergenceEpsy);
                epsmd3Series.Add(report.ConvergenceEpsmd3);
                epsmd4Series.Add(report.ConvergenceEpsmd4);
                alldok43Series.Add(report.ConvergenceAlldok43);
                alldoksdSeries.Add(report.ConvergenceAlldoksd);
                dolM2Series.Add(report.ConvergenceDolM2);
            }
        }

        stopwatch.Stop();

        var convergence = cycles > 1
            ? new Convergence(
                epsySeries.ToImmutable(), epsmd3Series.ToImmutable(), epsmd4Series.ToImmutable(),
                alldok43Series.ToImmutable(), alldoksdSeries.ToImmutable(), dolM2Series.ToImmutable())
            : null;

        // Reference mode and the degenerate configuration run one particle at a time (root BOOT.md, "Reference
        // mode is the original's sequence"); the same is true of the per-launch attempt budget, whose
        // *effective* value reference mode always fixes at 1 (BOOT.md, "## Budget selection rule (2026-09-28)").
        var reportedBatchSize = _options.Mode == ExecutionMode.Reference ? 1 : batchSize;
        var reportedAttemptsPerLaunch = _options.Mode == ExecutionMode.Reference ? 1 : _options.AttemptsPerLaunch;
        var diagnostics = new RunDiagnostics(
            _options.Mode, reportedBatchSize, reportedAttemptsPerLaunch, _engine.Accelerator, _options.Streams,
            _options.Precision, _options.Seed, _options.ContinuedStreams, totalAttempts, totalLaunches, stopwatch.Elapsed);

        return BuildResult(formulation, lastReport!, convergence, diagnostics, storedSetup);
    }

    private void RunReferenceCycle(
        int cycle, int cycles, int particlesPerCycle, ref StreamSet streams, ref long acceptedParticles,
        ref long totalAttempts, ref long totalLaunches, IProgress<CycleProgress>? progress, CancellationToken cancellationToken)
    {
        for (var index = 0; index < particlesPerCycle; index++)
        {
            var status = _engine.RunReferenceParticle(ref streams, _options.MaxAttemptsPerParticle, out var counters);
            totalAttempts += counters.Attempts;
            totalLaunches += counters.Launches;
            ThrowIfFailed(status);

            acceptedParticles++;

            if ((index + 1) % 1000 == 0 || index == particlesPerCycle - 1)
            {
                progress?.Report(new CycleProgress(cycle, cycles, acceptedParticles));
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    private void RunContinuedBatchCycle(
        int cycle, int cycles, int particlesPerCycle, ref StreamSet streams, ref long acceptedParticles,
        ref long totalAttempts, ref long totalLaunches, IProgress<CycleProgress>? progress, CancellationToken cancellationToken)
    {
        for (var index = 0; index < particlesPerCycle; index++)
        {
            var status = _engine.RunContinuedBatch(ref streams, _options.AttemptsPerLaunch, _options.MaxAttemptsPerParticle, out var counters);
            totalAttempts += counters.Attempts;
            totalLaunches += counters.Launches;
            ThrowIfFailed(status);

            acceptedParticles++;
            progress?.Report(new CycleProgress(cycle, cycles, acceptedParticles));
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void RunBatchedCycle(
        int cycle, int cycles, int particlesPerCycle, int batchSize, Random.StreamLayout randomLayout, ref long acceptedParticles,
        ref long totalAttempts, ref long totalLaunches, IProgress<CycleProgress>? progress, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < particlesPerCycle)
        {
            var count = Math.Min(batchSize, particlesPerCycle - offset);
            var firstOrdinal = (ulong)cycle * (ulong)particlesPerCycle + (ulong)offset;
            var status = _engine.RunBatch(
                randomLayout, _options.Seed, firstOrdinal, count, _options.AttemptsPerLaunch, _options.MaxAttemptsPerParticle,
                out var counters);
            totalAttempts += counters.Attempts;
            totalLaunches += counters.Launches;
            ThrowIfFailed(status);

            offset += count;
            acceptedParticles += count;
            progress?.Report(new CycleProgress(cycle, cycles, acceptedParticles));
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    /// <summary>
    /// Turns a failed batch status into <see cref="SimulationFailedException"/> (API.md, "## Errors"); does
    /// nothing on <see cref="BatchStatus.Ok"/>; <see cref="BatchStatus.ParticleNotRun"/> is no run status and
    /// throws <see cref="InvalidOperationException"/> through <see cref="ToRunStatus"/> (decided 2026-10-02). <see cref="BatchStatus.OriginalPrecisionRequiresReferenceMode"/>
    /// is singled out rather than routed through <see cref="ToRunStatus"/>'s generic by-name mapping: it
    /// reuses <see cref="RunStatus.InvalidSetup"/> (decided with the root, 2026-09-21, rather than a new
    /// status value the other node reporting <see cref="RunStatus"/> would need a new case for) and needs a
    /// message naming both halves of its own conflict, which a status alone does not carry. By construction
    /// this branch is unreachable through this method under the sanctioned path: <see cref="Create"/>'s own
    /// <see cref="SimulationOptionsValidator"/> already refuses <see cref="PrecisionKind.Original"/> with
    /// <see cref="ExecutionMode.Batched"/> before a <see cref="Simulator"/> exists at all (architecture audit
    /// finding R1, 2026-09-24), so no call this instance makes into <see cref="_engine"/> can still be under
    /// that combination; it stays here as the same defensive mapping <see cref="Execution.Engine"/>'s own
    /// mechanism guard needs for a caller that reaches it directly, bypassing this node
    /// (`src/Execution/BOOT.md`, "`Original` accumulation is refused for batched execution"). The twin
    /// refusal for <see cref="StreamLayout.Original"/>, <c>BatchStatus.OriginalLayoutRequiresReferenceMode</c>,
    /// is withdrawn along with it: <c>Execution.Engine</c> no longer checks the stream layout at all, since
    /// that refusal was a policy choice with no mechanism reason to live there
    /// (<see cref="SimulationOptionsValidator"/>'s own remarks).
    /// </summary>
    private void ThrowIfFailed(BatchStatus status)
    {
        if (status == BatchStatus.Ok)
        {
            return;
        }

        if (status == BatchStatus.OriginalPrecisionRequiresReferenceMode)
        {
            throw new SimulationFailedException(
                RunStatus.InvalidSetup,
                $"SimulationOptions.Precision is {nameof(PrecisionKind.Original)}, which accumulates " +
                $"in an order batched execution does not fix, so it is refused with SimulationOptions.Mode = " +
                $"{_options.Mode} (root BOOT.md, \"Precision kind is an option of every run\"); " +
                $"use {nameof(ExecutionMode.Reference)} instead.");
        }

        throw new SimulationFailedException(ToRunStatus(status));
    }

    /// <summary>
    /// Every failure status of a batch as the run status of the same name (API.md, "## Errors"), except
    /// <see cref="BatchStatus.OriginalPrecisionRequiresReferenceMode"/>, which reuses
    /// <see cref="RunStatus.InvalidSetup"/> (see <see cref="ThrowIfFailed"/>) — the one name mismatch of
    /// this mapping, deliberate (root, 2026-09-21) — and <see cref="BatchStatus.ParticleNotRun"/>, which is no
    /// run status: an engine defect, thrown as <see cref="InvalidOperationException"/> (decided 2026-10-02).
    /// </summary>
    internal static RunStatus ToRunStatus(BatchStatus status) => status switch
    {
        BatchStatus.AttemptCapExceeded => RunStatus.AttemptCapExceeded,
        BatchStatus.NeighbourBudgetExceeded => RunStatus.NeighbourBudgetExceeded,
        BatchStatus.BridgeDrawBudgetExceeded => RunStatus.BridgeDrawBudgetExceeded,
        BatchStatus.IndexOutOfRange => RunStatus.IndexOutOfRange,
        BatchStatus.AcceleratorUnavailable => RunStatus.AcceleratorUnavailable,
        BatchStatus.OriginalPrecisionRequiresReferenceMode => RunStatus.InvalidSetup,
        BatchStatus.ParticleNotRun => throw new InvalidOperationException(
            $"Execution reported {nameof(BatchStatus.ParticleNotRun)}: a launch left an active particle's " +
            "attempt count unchanged, a defect of the engine, not a status of the run."),
        BatchStatus.Ok => throw new ArgumentOutOfRangeException(nameof(status), status, message: null),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: null),
    };

    /// <summary>The field-by-field mapping from <see cref="CycleReport"/> to <see cref="SimulationResult"/>'s member records (BOOT.md, "## Result field mapping").</summary>
    private static SimulationResult BuildResult(
        Formulation formulation, CycleReport report, Convergence? convergence, RunDiagnostics diagnostics, StoredSetup storedSetup)
    {
        var header = new RunHeader(
            formulation.Name, formulation.Cycles, formulation.ParticlesPerCycle,
            report.Dokm, report.Doksd, report.Ddokmax, report.Dmax, report.TailProbabilityModified, report.Ggg,
            report.Zx.ToImmutableArray());

        var counters = new Counters(
            report.Qkss, report.Nfx, report.Nfy, report.Nfz, report.Nfq, report.Nfw, report.Conditions.ToImmutableArray(),
            report.IbridgeTotal, report.JammedTotal, report.NnTotal);

        var generatorAccuracy = new GeneratorAccuracy(
            report.Eps1, report.Eps2, report.Eps3, report.Eps4, report.Eps5, report.Eps6, report.Eps7,
            report.Epsx1, report.Epsx2, report.Epsx3, report.Epsalldok.ToImmutableArray(),
            report.Epsy, report.Epsmd4, report.Epsmd3, report.OxidizerAccuracyWarning, report.PocketAccuracyWarning);

        var particleSizes = new ParticleSizes(report.Dok43b, report.Dok43s, report.Alldok43, report.Alldok432);

        var localStructure = new LocalStructure(report.Gdokleft, report.Vdokleft, report.Plotsmdok, report.Plotsm, report.Mp);

        var pockets = new Pockets(
            report.Dp43, report.D432, report.Sdevp43, report.Dqkarm,
            report.Dkarm43Cor, report.Sdevp43Cor, report.DqkarmCor, report.Dfmk432, report.Sdevp243,
            report.DpMax, report.DpMaxCor,
            report.DpRow, report.Dpockets.ToImmutableArray(), report.Dokp43.ToImmutableArray(), report.Qdokkarm.ToImmutableArray(),
            report.Qdokso.Select(row => row.ToImmutableArray()).ToImmutableArray());

        var massFractions = new MassFractions(report.DolM1, report.DolM2, report.DolM3);

        var agglomerates = new Agglomerates(
            report.Dqmkm1, report.Dqmkm2, report.Qmcoef, report.CoefNmax, report.Qmkm1Nmax, report.Qmkm2Nmax);

        var histograms = new Histograms(
            report.Allvdokso.ToImmutableArray(), report.Vkso.ToImmutableArray(), report.Qks1.ToImmutableArray(),
            report.FmkarmCorNormalized.ToImmutableArray(), report.Fmkarm2Normalized.ToImmutableArray(),
            report.FqkarmCorNormalized.ToImmutableArray(), report.Qmkm1Normalized.ToImmutableArray(),
            report.Qmkm2Normalized.ToImmutableArray(), report.CoefNormalized.ToImmutableArray(),
            report.Pdoksmall.ToImmutableArray());

        return new SimulationResult(
            header, counters, generatorAccuracy, particleSizes, localStructure, pockets, massFractions, agglomerates,
            histograms, convergence, diagnostics, storedSetup);
    }

    /// <summary>Disposes the accelerator engine created by <see cref="Create"/>. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _engine.Dispose();
        _disposed = true;
    }
}
