using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using PropStruct.Particle;
using PropStruct.Random;

namespace PropStruct.Execution;

/// <summary>
/// Runs the particle program's batches and reference-mode particles over one accelerator's run-scoped
/// buffers (BOOT.md, Purpose; API.md, "Engine"). Used from one thread at a time.
/// </summary>
internal sealed class Engine : IDisposable
{
    private readonly AcceleratorBinding _binding;
    private readonly KernelCache? _kernels;
    private readonly bool _hostThreads;
    private readonly int? _cpuThreads;
    private readonly long _recordBudgetBytes;

    private ModelSetup _setup;
    private int _cycleFlag;
    private double _dmaxxx;
    private bool _loaded;
    private bool _disposed;

    private MemoryBuffer1D<double, Stride1D.Dense>? _boundsBuffer;
    private MemoryBuffer1D<double, Stride1D.Dense>? _cumulativeBuffer;
    private MemoryBuffer1D<byte, Stride1D.Dense>? _pocketFormingBuffer;
    private MemoryBuffer1D<long, Stride1D.Dense>? _integerTotals;
    private MemoryBuffer1D<double, Stride1D.Dense>? _realTotals;
    private Qks1Refresher? _qks1Refresher;
    private MemoryBuffer1D<double, Stride1D.Dense>? _pdoksmall;

    private Engine(AcceleratorBinding binding, long recordBudgetBytes, bool hostThreads, int? cpuThreads)
    {
        _binding = binding;
        _recordBudgetBytes = recordBudgetBytes;
        _hostThreads = hostThreads;
        _cpuThreads = cpuThreads;
        _kernels = binding.Refused ? null : new KernelCache(binding);
    }

    /// <summary>The accelerator this engine is bound to, and why it is that one.</summary>
    public AcceleratorInfo Accelerator => _binding.Info;

    /// <summary>The largest particle count whose batch memory fits the record budget passed to <see cref="Create"/>; valid after <see cref="Load"/>.</summary>
    public int MaxBatchSize { get; private set; }

    /// <summary>
    /// Creates an engine bound to the accelerator <paramref name="kind"/> selects (BOOT.md, Invariants).
    /// Never throws for a missing CUDA device: an unavailable <see cref="AcceleratorKind.Cuda"/> request
    /// yields an engine whose every run call returns <see cref="BatchStatus.AcceleratorUnavailable"/>
    /// (API.md, "Errors"). <paramref name="cpuThreads"/> is an addition of this coding session, beyond the
    /// API.md sketch (AGENTS.md §4): when the accelerator actually bound is the CPU one (kind
    /// <see cref="AcceleratorKind.Cpu"/>, or <see cref="AcceleratorKind.Auto"/> falling back to it), it
    /// forces that many logical threads instead of every core, for the determinism-under-thread-count
    /// acceptance criterion (root BOOT.md: "1, 4 and 16 CPU threads"); <see langword="null"/> (every other
    /// caller, including every production one) keeps the original all-cores behaviour.
    /// <paramref name="environment"/> is a second addition (review item 3, 2026-09-18): a test that
    /// needs a specific engine forbidden or allowed to use CUDA can inject its own answer for
    /// <see cref="AcceleratorChoice.NoCudaVariable"/> instead of mutating the real process environment,
    /// which xunit's parallel test-class execution would otherwise apply to every engine any other,
    /// concurrently running test creates; <see langword="null"/> (every production caller) keeps the real
    /// process environment.
    /// <paramref name="forceIlgpuKernelsOnCpu"/> is a third addition (BOOT.md, "Host-thread path",
    /// 2026-09-19): every production caller, and almost every test, leaves it <see langword="false"/> and
    /// gets the host-thread path this engine now uses whenever it binds the CPU accelerator (batched mode
    /// launches nothing through ILGPU's own kernel dispatch; reference mode already ran this way). Passing
    /// <see langword="true"/> keeps the previous ILGPU-kernel-launch behaviour on the CPU accelerator, which
    /// no longer carries any production load but stays as the test oracle of the kernel path itself (root
    /// BOOT.md, "One particle program": "kept as a test oracle of the kernel path only") — the accelerator
    /// choice this makes is internal to the tree; the public <see cref="AcceleratorKind"/> is unchanged.
    /// </summary>
    public static Engine Create(
        AcceleratorKind kind, long recordBudgetBytes, int? cpuThreads = null, Func<string, string?>? environment = null,
        bool forceIlgpuKernelsOnCpu = false)
    {
        if (recordBudgetBytes <= 0)
        {
            throw new ArgumentException("the record budget must be positive.", nameof(recordBudgetBytes));
        }

        LibDevicePostLink.AssertIlgpu();
        var binding = AcceleratorChoice.Decide(kind, cpuThreads, environment);
        var hostThreads = !forceIlgpuKernelsOnCpu && !binding.Refused && binding.Info.Kind == AcceleratorKind.Cpu;
        return new Engine(binding, recordBudgetBytes, hostThreads, cpuThreads);
    }

    /// <summary>
    /// The tail draw of <c>PARAM</c>, called directly on this host thread
    /// (<c>Particle/API.md</c>, "Size law"; <c>Simulation/API.md</c>, "Setup hand-off") — no ILGPU kernel is
    /// compiled or launched for it, on any engine kind (root BOOT.md, "One particle program": the ILGPU
    /// kernel-launch path is a test oracle only, and this one host draw never needed it). Does not require
    /// <see cref="Load"/>, and runs even on a refused CUDA engine: <see cref="SizeLaw.Sample"/> gives the
    /// same answer regardless of which accelerator's memory backs its <see cref="ArrayView{T}"/> arguments
    /// (there is no accelerator-specific behaviour here to probe, unlike <see cref="ProbeMath"/>), and
    /// <c>Simulation</c> calls this as the very first thing it does with a freshly created engine
    /// (<c>Simulation/API.md</c>, "Setup hand-off") — CUDA being unavailable should not fail a run before
    /// its particle kernel, which may not even need CUDA, ever runs (review item 10, 2026-09-18:
    /// preferred over documenting the throw, since the caller has no accelerator-kind reason to special-case
    /// this one draw). When this engine is itself CPU-bound its own accelerator's host memory is reused
    /// directly, exactly as the rest of the host-thread path reuses it (BOOT.md, "Host-thread path"); a
    /// CUDA-bound or refused engine cannot lend its own binding — CUDA memory is not host-addressable, and a
    /// refused binding has no accelerator at all — so an ephemeral CPU accelerator is built and disposed for
    /// that one call, exactly as before this fix (audit finding D8, 2026-09-24): the fix removes the
    /// ILGPU-kernel-launch dispatch <c>Kernels.SampleSize</c> (a historical name, not a current member) used
    /// to go through, not this fallback.
    /// </summary>
    public void SampleSize(
        int sizeLaw, int fractionCount, double[] bounds, double[] cumulative, double x, double x1,
        out double diameter, out int fraction)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(cumulative);
        ThrowIfDisposed();

        if (!_binding.Refused && _binding.Info.Kind == AcceleratorKind.Cpu)
        {
            RunSampleSize(_binding.Accelerator!, sizeLaw, fractionCount, bounds, cumulative, x, x1, out diameter, out fraction);
            return;
        }

        using var fallback = AcceleratorBinding.Cpu(cudaSkippedBecause: null);
        RunSampleSize(fallback.Accelerator!, sizeLaw, fractionCount, bounds, cumulative, x, x1, out diameter, out fraction);
    }

    /// <summary>
    /// Calls <see cref="SizeLaw.Sample"/> directly on this host thread, over <see cref="ArrayView{T}"/>s
    /// obtained from <paramref name="accelerator"/>'s own memory — the same host-side-view pattern the rest
    /// of the host-thread path uses (<see cref="RunParticlesCore"/>, <see cref="RunReferenceParticle"/>): no
    /// <see cref="KernelCache"/> lookup, no compiled launcher, no kernel dispatch of any kind.
    /// </summary>
    private static void RunSampleSize(
        Accelerator accelerator, int sizeLaw, int fractionCount, double[] bounds, double[] cumulative,
        double x, double x1, out double diameter, out int fraction)
    {
        using var boundsBuffer = accelerator.Allocate1D(bounds);
        using var cumulativeBuffer = accelerator.Allocate1D(cumulative);
        accelerator.Synchronize();

        SizeLaw.Sample(sizeLaw, fractionCount, boundsBuffer.View, cumulativeBuffer.View, x, x1, out diameter, out fraction);
    }

    /// <summary>
    /// Uploads the fraction table and allocates the run's totals, QKS1 and the cycle inputs for the whole
    /// run (BOOT.md, "Run-scoped state lives on the accelerator"). Resets the totals to zero and QKS1 to
    /// <c>Normalize</c> of those zeros.
    /// </summary>
    public void Load(in ModelSetup setup, double[] bounds, double[] cumulative, byte[] pocketForming)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(cumulative);
        ArgumentNullException.ThrowIfNull(pocketForming);
        ThrowIfDisposed();

        if (bounds.Length != 2 * setup.FractionCount)
        {
            throw new ArgumentException($"bounds must hold 2*{setup.FractionCount} entries, held {bounds.Length}.", nameof(bounds));
        }

        if (cumulative.Length != setup.FractionCount + 1)
        {
            throw new ArgumentException($"cumulative must hold {setup.FractionCount + 1} entries, held {cumulative.Length}.", nameof(cumulative));
        }

        if (pocketForming.Length != setup.FractionCount)
        {
            throw new ArgumentException($"pocketForming must hold {setup.FractionCount} entries, held {pocketForming.Length}.", nameof(pocketForming));
        }

        DisposeRunBuffers();

        _setup = setup;
        MaxBatchSize = ComputeMaxBatchSize(setup.Layout, _recordBudgetBytes);
        _cycleFlag = 0;
        _dmaxxx = 0.0;

        if (_binding.Refused)
        {
            _loaded = true;
            return;
        }

        var accelerator = _binding.Accelerator!;
        var layout = setup.Layout;

        _boundsBuffer = accelerator.Allocate1D(bounds);
        _cumulativeBuffer = accelerator.Allocate1D(cumulative);
        _pocketFormingBuffer = accelerator.Allocate1D(pocketForming);
        _integerTotals = accelerator.Allocate1D<long>(layout.IntegerLength);
        _integerTotals.MemSetToZero();
        _realTotals = accelerator.Allocate1D<double>(layout.RecordLength);
        _realTotals.MemSetToZero();
        _qks1Refresher = new Qks1Refresher(accelerator, _kernels!, setup.Nkarm, _hostThreads);
        _pdoksmall = accelerator.Allocate1D<double>(setup.Ndok);
        _pdoksmall.MemSetToZero();
        accelerator.Synchronize();

        // QKS1 starts as Normalize of the loaded (zero) integer totals (BOOT.md, "QKS1 is the engine's").
        _qks1Refresher.Recompute(in layout, _integerTotals.View, currentLoopCompletions: 0);

        _loaded = true;
    }

    /// <summary>Downloads the run's totals as they currently stand.</summary>
    public void ReadTotals(long[] integerTotals, double[] realTotals)
    {
        ArgumentNullException.ThrowIfNull(integerTotals);
        ArgumentNullException.ThrowIfNull(realTotals);
        ThrowIfNotLoaded();
        ValidateTotalsLength(integerTotals, realTotals);

        if (_binding.Refused)
        {
            Array.Clear(integerTotals);
            Array.Clear(realTotals);
            return;
        }

        _integerTotals!.CopyToCPU(integerTotals);
        _realTotals!.CopyToCPU(realTotals);
    }

    /// <summary>Uploads the run's totals, replacing whatever the engine held (used around <c>Statistics</c>' in-place rewrites between cycles).</summary>
    public void WriteTotals(long[] integerTotals, double[] realTotals)
    {
        ArgumentNullException.ThrowIfNull(integerTotals);
        ArgumentNullException.ThrowIfNull(realTotals);
        ThrowIfNotLoaded();
        ValidateTotalsLength(integerTotals, realTotals);

        if (_binding.Refused)
        {
            return;
        }

        _integerTotals!.CopyFromCPU(integerTotals);
        _realTotals!.CopyFromCPU(realTotals);

        // The totals were just replaced wholesale (Simulation's own in-place rewrite of Statistics between
        // cycles; a caller forking one engine's state onto another), not accumulated incrementally by this
        // engine's own launches, so QKS1 cannot be assumed consistent with whatever it last held — it must
        // be recomputed now, exactly as Load recomputes it for a freshly zeroed run (BOOT.md, "QKS1 is the
        // engine's"). Qks1Refresher.Recompute always does both (the buffer and the bookkeeping) together, so
        // "resync without recompute" — the defect this fixed (BOOT.md, "third look") — cannot be written
        // again by a future edit here.
        _qks1Refresher!.Recompute(in _setup.Layout, _integerTotals!.View, ReadLoopCompletions());
    }

    /// <summary>Replaces the cycle inputs. QKS1 is not touched here: it is the engine's own (BOOT.md, "QKS1 is the engine's").</summary>
    public void SetCycle(int cycleFlag, double dmaxxx, double[] pdoksmall)
    {
        ArgumentNullException.ThrowIfNull(pdoksmall);
        ThrowIfNotLoaded();
        if (pdoksmall.Length != _setup.Ndok)
        {
            throw new ArgumentException($"pdoksmall must hold {_setup.Ndok} entries, held {pdoksmall.Length}.", nameof(pdoksmall));
        }

        _cycleFlag = cycleFlag;
        _dmaxxx = dmaxxx;

        if (_binding.Refused)
        {
            return;
        }

        _pdoksmall!.CopyFromCPU(pdoksmall);
    }

    /// <summary>
    /// Runs particles <paramref name="firstOrdinal"/>…<paramref name="firstOrdinal"/> + <paramref name="particleCount"/> − 1 with
    /// streams derived per particle (BOOT.md, "A batch"). Refuses with
    /// <see cref="BatchStatus.OriginalPrecisionRequiresReferenceMode"/>, before any attempt runs, when the
    /// loaded setup's precision kind is <c>Original</c> (BOOT.md, "`Original` accumulation is refused for
    /// batched execution": this node's own mechanism guard, architecture audit finding R1, 2026-09-24).
    /// <paramref name="layout"/> is no longer checked here: <c>StreamLayout.Original</c> is a policy refusal
    /// with no mechanism reason to live in this node (`Simulation.SimulationOptionsValidator`'s own remarks;
    /// BOOT.md, "`Original` stream layout is refused for batched execution", withdrawn) — this method derives
    /// and runs streams of either layout to completion.
    /// </summary>
    public BatchStatus RunBatch(
        StreamLayout layout, ulong seed, ulong firstOrdinal, int particleCount,
        int attemptsPerLaunch, long maxAttemptsPerParticle, out BatchCounters counters)
    {
        ThrowIfNotLoaded();
        ValidateRunArgs(particleCount, attemptsPerLaunch, maxAttemptsPerParticle);

        if (_setup.Kind == PrecisionKind.Original)
        {
            counters = default;
            return BatchStatus.OriginalPrecisionRequiresReferenceMode;
        }

        if (_binding.Refused)
        {
            counters = default;
            return BatchStatus.AcceleratorUnavailable;
        }

        return RunParticlesCore(particleCount, layout, seed, firstOrdinal, null, attemptsPerLaunch, maxAttemptsPerParticle, out counters, out _);
    }

    /// <summary>
    /// A batch of one particle whose streams are passed in and returned advanced (BOOT.md, "A continued
    /// batch"). Refuses with <see cref="BatchStatus.OriginalPrecisionRequiresReferenceMode"/> exactly as
    /// <see cref="RunBatch"/> does, and for the same reason even though this call is itself sequential: the
    /// refusal is unconditional over the loaded setup's precision kind, not over whether this particular call
    /// happens to be single-particle (BOOT.md, "`Original` accumulation is refused for batched execution").
    /// Takes no <c>layout</c> parameter (dropped, architecture audit finding R1, 2026-09-24): this method
    /// never derived streams from one — a continued batch only continues the caller's own
    /// <paramref name="streams"/> (BOOT.md, "A continued batch") — and its one other reason for existing, the
    /// stream-layout refusal, moved entirely to <c>Simulation.SimulationOptionsValidator</c> (see
    /// <see cref="RunBatch"/>'s own remarks).
    /// </summary>
    public BatchStatus RunContinuedBatch(
        ref StreamSet streams, int attemptsPerLaunch, long maxAttemptsPerParticle, out BatchCounters counters)
    {
        ThrowIfNotLoaded();
        ValidateRunArgs(1, attemptsPerLaunch, maxAttemptsPerParticle);

        if (_setup.Kind == PrecisionKind.Original)
        {
            counters = default;
            return BatchStatus.OriginalPrecisionRequiresReferenceMode;
        }

        if (_binding.Refused)
        {
            counters = default;
            return BatchStatus.AcceleratorUnavailable;
        }

        var initial = new[] { streams };
        var status = RunParticlesCore(1, default, 0, 0, initial, attemptsPerLaunch, maxAttemptsPerParticle, out counters, out var finalStreams);
        streams = finalStreams![0];
        return status;
    }

    /// <summary>
    /// Runs one particle on the host thread over CPU-accelerator memory until it is accepted or a failure
    /// or cap ends it (BOOT.md, "Reference mode"). CPU engines only.
    /// </summary>
    /// <remarks>
    /// Under <see cref="PrecisionKind.Original"/> the record is not this particle's own delta folded
    /// onto the totals afterwards: it <em>is</em> the run's one REAL*4 accumulator, seeded from
    /// <see cref="_realTotals"/> before the particle's first attempt and written back over
    /// <see cref="_realTotals"/> on acceptance, in place of <see cref="Fold.Add"/> (BOOT.md, "Record
    /// seeding under Original accumulation"). Reproducing the original's own rounding loss needs every
    /// addition rounded against the accumulator's true, run-scale magnitude (Particle/BOOT.md,
    /// "Precision kind": the shadow measurement that found 46-65% worst-cell error used a persistent
    /// accumulator of this shape); starting every particle's record at zero, the ordinary
    /// <see cref="PrecisionKind.Binary64"/> shape below, rounds a few dozen terms against a magnitude
    /// near zero and then adds the (barely rounded) delta onto the totals in full <see cref="double"/>
    /// precision, which is not the original's own accumulation at all. <see cref="Attempt.Run"/>'s own
    /// write sites are unchanged either way (Particle/API.md, "Precision kind"): only what the record
    /// starts from, and how it is written back, differs by kind, and both are this engine's own
    /// orchestration, not <c>Particle</c>'s.
    /// </remarks>
    public BatchStatus RunReferenceParticle(ref StreamSet streams, long maxAttemptsPerParticle, out BatchCounters counters)
    {
        ThrowIfNotLoaded();
        if (maxAttemptsPerParticle <= 0)
        {
            throw new ArgumentException("maxAttemptsPerParticle must be positive.", nameof(maxAttemptsPerParticle));
        }

        if (_binding.Refused || Accelerator.Kind != AcceleratorKind.Cpu)
        {
            counters = default;
            return BatchStatus.AcceleratorUnavailable;
        }

        var accelerator = _binding.Accelerator!;
        var layout = _setup.Layout;
        var fractionTable = new FractionTable
        {
            Bounds = _boundsBuffer!.View,
            Cumulative = _cumulativeBuffer!.View,
            PocketForming = _pocketFormingBuffer!.View,
        };

        // Original accumulation is sequential only and runs exclusively through this method (root BOOT.md,
        // "Precision kind is an option of every run"; batched execution refuses it), so the record can
        // safely double as the run's one persistent REAL*4 accumulator instead of a per-particle delta.
        var sequentialAccumulation = _setup.Kind == PrecisionKind.Original;

        using var recordBuffer = accelerator.Allocate1D<double>(layout.RecordLength);
        if (sequentialAccumulation)
        {
            var seed = new double[layout.RecordLength];
            _realTotals!.CopyToCPU(seed);
            recordBuffer.CopyFromCPU(seed);
        }
        else
        {
            recordBuffer.MemSetToZero();
        }

        using var scratchBuffer = accelerator.Allocate1D<double>(layout.ScratchLength);
        accelerator.Synchronize();

        long attempts = 0;
        var launches = 0;
        var refreshes = 0;

        while (true)
        {
            if (attempts >= maxAttemptsPerParticle)
            {
                counters = new BatchCounters(attempts, launches, refreshes);
                return BatchStatus.AttemptCapExceeded;
            }

            // Before every attempt, exactly as RunParticlesCore refreshes before every launch: with
            // attemptsPerLaunch = 1 a launch is one attempt, so the two must check at the same point in
            // the sequence for the bit-identity of BOOT.md's "Reference mode is batched mode degenerated".
            if (_qks1Refresher!.RefreshIfLoopsCompleted(in _setup.Layout, _integerTotals!.View, ReadLoopCompletions()))
            {
                refreshes++;
            }

            attempts++;
            launches++;

            var cycleInputs = new CycleInputs { CycleFlag = _cycleFlag, Dmaxxx = _dmaxxx, Qks1 = _qks1Refresher.View, Pdoksmall = _pdoksmall!.View };
            var outcome = Attempt.Run(in _setup, in fractionTable, in cycleInputs, ref streams, _integerTotals!.View, recordBuffer.View, scratchBuffer.View);

            var failure = ToFailureStatus(outcome);
            if (failure is not null)
            {
                counters = new BatchCounters(attempts, launches, refreshes);
                return failure.Value;
            }

            if (outcome == AttemptOutcome.Accepted)
            {
                if (sequentialAccumulation)
                {
                    var updated = new double[layout.RecordLength];
                    recordBuffer.CopyToCPU(updated);
                    _realTotals!.CopyFromCPU(updated);
                }
                else
                {
                    Fold.Add(in layout, recordBuffer.View, 1, _realTotals!.View);
                }

                counters = new BatchCounters(attempts, launches, refreshes);
                return BatchStatus.Ok;
            }

            // RestartedInsideLoop / RestartedAfterLoop: the same particle tries again.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeRunBuffers();
        _binding.Dispose();
    }

    /// <summary>
    /// The shared loop of <see cref="RunBatch"/> and <see cref="RunContinuedBatch"/>: derive or accept the
    /// initial streams, then launch attempts over a host-compacted active list until every particle is
    /// accepted, a failure fires, or the attempt cap is passed (BOOT.md, "A batch").
    /// </summary>
    private BatchStatus RunParticlesCore(
        int particleCount, StreamLayout layout, ulong seed, ulong firstOrdinal, StreamSet[]? initialStreams,
        int attemptsPerLaunch, long maxAttemptsPerParticle, out BatchCounters counters, out StreamSet[]? finalStreams)
    {
        var accelerator = _binding.Accelerator!;
        var recordLength = _setup.Layout.RecordLength;
        var scratchLength = _setup.Layout.ScratchLength;

        // Host-thread path (BOOT.md, "Host-thread path"): scratch is padded to a whole number of 64-byte
        // cache lines per particle, so concurrent OS threads never invalidate each other's scratch region
        // (Kernels.cs, ParticleControl's own doc comment). The kernel-launch path (CUDA, or the CPU
        // accelerator forced for the test oracle) keeps scratch tightly packed: that is what GPU memory
        // coalescing wants, and there is no OS-thread cache line to share. records keeps its plain
        // RecordLength stride on both paths — Particle.Fold.AddField's own indexing is fixed
        // (Particle/API.md, "Fold by field") and padding it here would misalign every fold read.
        var scratchStride = _hostThreads ? RoundUpToCacheLineDoubles(scratchLength) : scratchLength;

        MemoryBuffer1D<ParticleControl, Stride1D.Dense> controlsBuffer;
        if (initialStreams is not null)
        {
            var initialControls = new ParticleControl[particleCount];
            for (var i = 0; i < particleCount; i++)
            {
                initialControls[i] = new ParticleControl { Streams = initialStreams[i] };
            }

            controlsBuffer = accelerator.Allocate1D(initialControls);
        }
        else
        {
            controlsBuffer = accelerator.Allocate1D<ParticleControl>(particleCount);
            controlsBuffer.MemSetToZero();
        }

        using var controls = controlsBuffer;
        using var recordsBuffer = accelerator.Allocate1D<double>((long)particleCount * recordLength);
        recordsBuffer.MemSetToZero();
        using var scratchBuffer = accelerator.Allocate1D<double>((long)particleCount * scratchStride);
        accelerator.Synchronize();

        if (initialStreams is null)
        {
            if (_hostThreads)
            {
                _ = Parallel.For(0, particleCount, HostParallelOptions(),
                    i => Kernels.DeriveStreams(new Index1D(i), layout, seed, firstOrdinal, controls.View));
            }
            else
            {
                var deriveLauncher = _kernels!.Get<Action<AcceleratorStream, Index1D, StreamLayout, ulong, ulong, ArrayView<ParticleControl>>>(nameof(Kernels.DeriveStreams));
                deriveLauncher(accelerator.DefaultStream, new Index1D(particleCount), layout, seed, firstOrdinal, controls.View);
                accelerator.Synchronize();
            }
        }

        var fractionTable = new FractionTable
        {
            Bounds = _boundsBuffer!.View,
            Cumulative = _cumulativeBuffer!.View,
            PocketForming = _pocketFormingBuffer!.View,
        };

        var active = Enumerable.Range(0, particleCount).ToArray();

        // Every particle starts at attempt 0 (the controls were zeroed or built with only Streams above);
        // from the first download on, the previous download is the state before the next launch.
        var attemptsBefore = new long[particleCount];
        long totalAttempts = 0;
        var launches = 0;
        var refreshes = 0;

        while (true)
        {
            if (_qks1Refresher!.RefreshIfLoopsCompleted(in _setup.Layout, _integerTotals!.View, ReadLoopCompletions()))
            {
                refreshes++;
            }

            var cycleInputs = new CycleInputs { CycleFlag = _cycleFlag, Dmaxxx = _dmaxxx, Qks1 = _qks1Refresher.View, Pdoksmall = _pdoksmall!.View };

            using (var activeBuffer = accelerator.Allocate1D(active))
            {
                if (_hostThreads)
                {
                    // No second implementation of the attempt loop (root BOOT.md, Taboos): this calls the
                    // exact same Kernels.RunAttempts that the kernel-launch branch below compiles into a
                    // launcher, just dispatched by .NET's own thread pool instead of ILGPU's (BOOT.md,
                    // "Host-thread path": "the ILGPU CPU accelerator ... carries no production load").
                    _ = Parallel.For(0, active.Length, HostParallelOptions(), i => Kernels.RunAttempts(
                        new Index1D(i), _setup, fractionTable, cycleInputs, attemptsPerLaunch, maxAttemptsPerParticle, scratchStride,
                        activeBuffer.View, controls.View, _integerTotals!.View, recordsBuffer.View, scratchBuffer.View));
                }
                else
                {
                    var runLauncher = _kernels!.Get<Action<AcceleratorStream, Index1D, ModelSetup, FractionTable, CycleInputs, int, long, int,
                        ArrayView<int>, ArrayView<ParticleControl>, ArrayView<long>, ArrayView<double>, ArrayView<double>>>(
                        nameof(Kernels.RunAttempts));
                    runLauncher(
                        accelerator.DefaultStream, new Index1D(active.Length), _setup, fractionTable, cycleInputs,
                        attemptsPerLaunch, maxAttemptsPerParticle, scratchStride, activeBuffer.View, controls.View,
                        _integerTotals!.View, recordsBuffer.View, scratchBuffer.View);
                    accelerator.Synchronize();
                }
            }

            launches++;

            var controlsHost = controls.GetAsArray1D();
            totalAttempts = controlsHost.Sum(c => c.AttemptCount);

            // The outcome byte of a particle the launch never ran is stale (zero, which reads as Accepted,
            // on the first launch; the previous launch's outcome later): it is trusted only after the
            // particle's attempt count has grown.
            if (!EveryActiveParticleRan(active, attemptsBefore, controlsHost))
            {
                counters = new BatchCounters(totalAttempts, launches, refreshes);
                finalStreams = initialStreams is null ? null : controlsHost.Select(c => c.Streams).ToArray();
                return BatchStatus.ParticleNotRun;
            }

            for (var i = 0; i < particleCount; i++)
            {
                attemptsBefore[i] = controlsHost[i].AttemptCount;
            }

            var next = new List<int>(active.Length);
            BatchStatus? failure = null;
            var capExceeded = false;

            foreach (var particle in active)
            {
                var outcome = (AttemptOutcome)controlsHost[particle].Outcome;
                if (outcome == AttemptOutcome.Accepted)
                {
                    continue;
                }

                var particleFailure = ToFailureStatus(outcome);
                if (particleFailure is not null)
                {
                    failure ??= particleFailure;
                    continue;
                }

                if (controlsHost[particle].AttemptCount >= maxAttemptsPerParticle)
                {
                    capExceeded = true;
                }
                else
                {
                    next.Add(particle);
                }
            }

            if (failure is not null)
            {
                counters = new BatchCounters(totalAttempts, launches, refreshes);
                finalStreams = initialStreams is null ? null : controlsHost.Select(c => c.Streams).ToArray();
                return failure.Value;
            }

            if (capExceeded)
            {
                counters = new BatchCounters(totalAttempts, launches, refreshes);
                finalStreams = initialStreams is null ? null : controlsHost.Select(c => c.Streams).ToArray();
                return BatchStatus.AttemptCapExceeded;
            }

            if (next.Count == 0)
            {
                if (_hostThreads)
                {
                    _ = Parallel.For(0, recordLength, HostParallelOptions(),
                        field => Kernels.FoldField(new Index1D(field), _setup.Layout, recordsBuffer.View, particleCount, _realTotals!.View));
                }
                else
                {
                    var foldLauncher = _kernels!.Get<Action<AcceleratorStream, Index1D, AccumulatorLayout, ArrayView<double>, int, ArrayView<double>>>(nameof(Kernels.FoldField));
                    foldLauncher(accelerator.DefaultStream, new Index1D(recordLength), _setup.Layout, recordsBuffer.View, particleCount, _realTotals!.View);
                    accelerator.Synchronize();
                }

                counters = new BatchCounters(totalAttempts, launches, refreshes);
                finalStreams = initialStreams is null ? null : controlsHost.Select(c => c.Streams).ToArray();
                return BatchStatus.Ok;
            }

            active = next.ToArray();
        }
    }

    /// <summary>
    /// True when every particle of <paramref name="active"/> ran at least one attempt in the launch that just
    /// ended: its <see cref="ParticleControl.AttemptCount"/> in <paramref name="controlsHost"/> exceeds its
    /// count <paramref name="attemptsBefore"/> the launch started from. <c>Kernels.RunAttempts</c> increments
    /// the count before every attempt it runs and never reports an outcome without one, so an unchanged count
    /// means the launch skipped the particle and its outcome byte says nothing (BOOT.md, "A batch"). Internal
    /// so <c>Execution.Tests</c> can exercise both answers directly on constructed arrays.
    /// </summary>
    internal static bool EveryActiveParticleRan(int[] active, long[] attemptsBefore, ParticleControl[] controlsHost)
    {
        foreach (var particle in active)
        {
            if (controlsHost[particle].AttemptCount <= attemptsBefore[particle])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// <see cref="ParallelOptions.MaxDegreeOfParallelism"/> for the host-thread path's own <c>Parallel.For</c>
    /// calls: <see cref="_cpuThreads"/> when a caller forced a specific count (the determinism-under-
    /// thread-count acceptance criterion; <see langword="null"/>'s default, -1, lets the thread pool use
    /// every core — the same "all cores" meaning <see cref="_cpuThreads"/> already had for the ILGPU
    /// accelerator's own thread count before this engine had a host-thread path of its own).
    /// </summary>
    private ParallelOptions HostParallelOptions() =>
        _cpuThreads is int threads ? new ParallelOptions { MaxDegreeOfParallelism = threads } : new ParallelOptions();

    /// <summary>Rounds <paramref name="length"/> up to a multiple of 8 <see langword="double"/>s (one 64-byte
    /// cache line), for the host-thread path's own per-particle scratch stride (BOOT.md, "Host-thread path").</summary>
    private static int RoundUpToCacheLineDoubles(int length)
    {
        const int doublesPerCacheLine = 8;
        return (length + doublesPerCacheLine - 1) / doublesPerCacheLine * doublesPerCacheLine;
    }

    /// <summary>
    /// The outcome-to-status mapping every run call applies, exposed at this visibility (rather than
    /// <see langword="private"/>) so <c>Execution.Tests</c> can check it exhaustively over all six
    /// <see cref="AttemptOutcome"/> values without needing a full particle run to reach the two outcomes
    /// (<see cref="AttemptOutcome.BridgeDrawBudgetExceeded"/>, <see cref="AttemptOutcome.IndexOutOfRange"/>)
    /// that Particle's own preconditions make unreachable through <c>Attempt.Run</c> under a realistic
    /// setup (<c>Particle/BOOT.md</c>, "Attempt structure": both are guards against a precondition
    /// violation, not model branches). Not part of the public surface: <see cref="BatchStatus"/> and
    /// <see cref="AttemptOutcome"/> are both internal already.
    ///
    /// Which outcomes are failures at all is <see cref="Kernels.IsFailure"/>'s own list, shared with
    /// <see cref="Kernels.RunAttempts"/>'s in-launch stop (review item 5, 2026-09-18): this switch only
    /// adds the specific <see cref="BatchStatus"/> each failure outcome maps to, and its default case is
    /// unreachable by construction, not a second, independent list of the same three outcomes.
    /// </summary>
    internal static BatchStatus? ToFailureStatus(AttemptOutcome outcome)
    {
        if (!Kernels.IsFailure(outcome))
        {
            return null;
        }

        return outcome switch
        {
            AttemptOutcome.NeighbourBudgetExceeded => BatchStatus.NeighbourBudgetExceeded,
            AttemptOutcome.BridgeDrawBudgetExceeded => BatchStatus.BridgeDrawBudgetExceeded,
            AttemptOutcome.IndexOutOfRange => BatchStatus.IndexOutOfRange,
            AttemptOutcome.Accepted => null,
            AttemptOutcome.RestartedAfterLoop => null,
            AttemptOutcome.RestartedInsideLoop => null,
            _ => null, // unreachable: every outcome Kernels.IsFailure accepts is handled above.
        };
    }

    /// <summary>
    /// Downloads QKS1 as it currently stands, for tests that must confirm it is not the degenerate all-zero
    /// histogram the <see cref="WriteTotals"/> defect once left it at (BOOT.md, "third look") — reading
    /// through <see cref="Qks1Refresher"/> rather than by reflecting on a private field (the review of
    /// this node, decided 2026-09-18). At this visibility for the same reason as <see cref="ToFailureStatus"/>:
    /// not part of the design contract <c>Simulation</c>/<c>Cli</c> rely on, so not in <c>API.md</c>.
    /// </summary>
    internal double[] DebugReadQks1()
    {
        ThrowIfNotLoaded();
        return _qks1Refresher!.Download();
    }

    private long ReadLoopCompletions()
    {
        var offset = _setup.Layout.LoopCompletions;
        if (_binding.Accelerator is CPUAccelerator)
        {
            return _integerTotals!.View[offset];
        }

        return _integerTotals!.View.SubView(offset, 1).GetAsArray1D()[0];
    }

    private static int ComputeMaxBatchSize(in AccumulatorLayout layout, long recordBudgetBytes)
    {
        const long streamBytes = 96L;      // six Mcg128State, 16 bytes each (Random/API.md)
        const long attemptCountBytes = 8L; // long
        const long outcomeBytes = 1L;      // byte
        const long indexBytes = 4L;        // int

        var perParticleBytes = 8L * (layout.RecordLength + layout.ScratchLength) + streamBytes + attemptCountBytes + outcomeBytes + indexBytes;
        var particles = recordBudgetBytes / perParticleBytes;
        if (particles < 1)
        {
            particles = 1;
        }

        // Particle.Fold.AddField indexes its record buffer as `particle * recordLength + field`, all in
        // `int` (Particle/API.md, "Fold by field"): a batch large enough for that product to exceed
        // int.MaxValue would index out of range or silently wrap. Execution owns the record budget that
        // decides batch size, so it is the one that must not hand Fold a batch that large (review item
        // 8, 2026-09-18); Fold itself stays kernel-compatible int-indexed code, unchanged.
        // Particle.Fold.AddField indexes its record buffer as `particle * recordLength + field`, all in
        // `int` (Particle/API.md, "Fold by field"): a batch large enough for that product to exceed
        // int.MaxValue would index out of range or silently wrap. Execution owns the record budget that
        // decides batch size, so it is the one that must not hand Fold a batch that large (review item
        // 8, 2026-09-18); Fold itself stays kernel-compatible int-indexed code, unchanged.
        var recordLength = layout.RecordLength;
        var foldSafeMax = recordLength > 0 ? int.MaxValue / recordLength : int.MaxValue;
        if (particles > foldSafeMax)
        {
            particles = foldSafeMax;
        }

        return particles > int.MaxValue ? int.MaxValue : (int)particles;
    }

    private void ValidateRunArgs(int particleCount, int attemptsPerLaunch, long maxAttemptsPerParticle)
    {
        if (particleCount <= 0)
        {
            throw new ArgumentException("particleCount must be positive.", nameof(particleCount));
        }

        if (particleCount > MaxBatchSize)
        {
            throw new ArgumentException($"particleCount ({particleCount}) exceeds MaxBatchSize ({MaxBatchSize}).", nameof(particleCount));
        }

        if (attemptsPerLaunch <= 0)
        {
            throw new ArgumentException("attemptsPerLaunch must be positive.", nameof(attemptsPerLaunch));
        }

        if (maxAttemptsPerParticle <= 0)
        {
            throw new ArgumentException("maxAttemptsPerParticle must be positive.", nameof(maxAttemptsPerParticle));
        }
    }

    private void ValidateTotalsLength(long[] integerTotals, double[] realTotals)
    {
        if (integerTotals.Length != _setup.Layout.IntegerLength)
        {
            throw new ArgumentException($"integerTotals must hold {_setup.Layout.IntegerLength} entries, held {integerTotals.Length}.", nameof(integerTotals));
        }

        if (realTotals.Length != _setup.Layout.RecordLength)
        {
            throw new ArgumentException($"realTotals must hold {_setup.Layout.RecordLength} entries, held {realTotals.Length}.", nameof(realTotals));
        }
    }

    private void DisposeRunBuffers()
    {
        _boundsBuffer?.Dispose(); _boundsBuffer = null;
        _cumulativeBuffer?.Dispose(); _cumulativeBuffer = null;
        _pocketFormingBuffer?.Dispose(); _pocketFormingBuffer = null;
        _integerTotals?.Dispose(); _integerTotals = null;
        _realTotals?.Dispose(); _realTotals = null;
        _qks1Refresher?.Dispose(); _qks1Refresher = null;
        _pdoksmall?.Dispose(); _pdoksmall = null;
        _loaded = false;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private void ThrowIfRefused()
    {
        if (_binding.Refused)
        {
            throw new InvalidOperationException("the engine has no accelerator: CUDA was requested but is unavailable.");
        }
    }

    private void ThrowIfNotLoaded()
    {
        ThrowIfDisposed();
        if (!_loaded)
        {
            throw new InvalidOperationException("Load must be called before this member.");
        }
    }

    /// <summary>
    /// The probe of the particle program's own math list (<see cref="MathProbe"/>), one launch over every
    /// input: <c>outputs[input * MathProbe.FunctionCount + function]</c>. Added in this coding session
    /// (BOOT.md, Constraints: "a probe kernel compared against the CPU accelerator") — not part of the
    /// design sketch of <c>API.md</c>, which named the requirement in <c>Execution.Tests/BOOT.md</c> without
    /// giving the engine a member to call; this closes that gap within the current task (AGENTS.md §4).
    /// </summary>
    public double[] ProbeMath(double[] inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ThrowIfDisposed();
        ThrowIfRefused();

        if (inputs.Length == 0)
        {
            return [];
        }

        var accelerator = _binding.Accelerator!;
        using var inputBuffer = accelerator.Allocate1D(inputs);
        using var outputBuffer = accelerator.Allocate1D<double>((long)inputs.Length * MathProbe.FunctionCount);
        var launcher = _kernels!.Get<Action<AcceleratorStream, Index1D, ArrayView<double>, ArrayView<double>>>(nameof(Kernels.ProbeMath));
        launcher(accelerator.DefaultStream, new Index1D(inputs.Length), inputBuffer.View, outputBuffer.View);
        accelerator.Synchronize();
        return outputBuffer.GetAsArray1D();
    }
}
