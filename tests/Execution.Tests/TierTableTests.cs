using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L2 (BOOT.md): CUDA against the CPU accelerator on whole-cycle batches of the five reference
/// formulations, against the tier table this test derives (root BOOT.md, "GPU equals CPU"; this node's own
/// BOOT.md, "The GPU/CPU tier table lives here, in one file"). Two figures per formulation, both measured
/// on this machine (RTX 5070 Ti) before being written down: a relative tolerance on the aggregate totals of
/// particles whose decisions agree, and the share of particles whose own decision sequence diverges
/// (detected through <see cref="Engine.RunContinuedBatch"/>'s own returned stream state: any last-ULP
/// difference between libdevice and <see cref="Math"/> that tips a branch changes every draw the
/// particle takes afterwards, so its final stream — otherwise a pure function of the draws taken — differs
/// too; agreement of two accelerators' final stream for the same particle is therefore exactly "the same
/// decisions were taken").
/// </summary>
[Collection(LongClassesSerialTests.Name)]
public sealed class TierTableTests(ITestOutputHelper output)
{
    private const long RecordBudgetBytes = 2L << 30;
    // The default attempt budget chosen by the 2026-09-28 selection rule (src/Simulation/BOOT.md, "## Budget
    // selection rule (2026-09-28)"; tests/Simulation.Tests/BOOT.md, "## Budget ladder (2026-10-01)"). A literal:
    // this node does not depend on Simulation, so it cannot read SimulationOptions' default.
    private const int AttemptsPerLaunch = 8192;
    private const long MaxAttemptsPerParticle = 10_000_000;
    private const int DivergenceSampleSize = 2000;

    // Tier table (this machine's RTX 5070 Ti; src/Execution/BOOT.md, "GPU/CPU tier table").
    //
    // ⚠ 2026-09-18: first measured before the WriteTotals fix (src/Execution/BOOT.md, "third look"), which
    // left cycle 1's QKS1 all-zero on every fork this test builds — no bridge draw of cycle 1 ever reached a
    // populated window on either accelerator, so the figures below were an agreement measurement over a
    // degenerate computation, not the real one:
    //
    //   HPEPA3:  max relative diff 1.688699E-14, diverged 0/2000
    //   inpt:    max relative diff 6.210907E-15, diverged 0/1000
    //   P33:     max relative diff 6.769958E-14, diverged 0/2000
    //   PSAN02n: max relative diff 6.674763E-14, diverged 0/2000
    //   HMX:     max relative diff 2.220092E-14, diverged 0/2000
    //
    // Re-measured after the fix, with a QKS1-non-zero assertion added at every fork this test builds so the
    // same defect cannot pass silently again (below, and at each `WriteTotals`/`SetCycle(1, ...)` pair in
    // the theory body):
    //
    //   HPEPA3:  max relative diff 2.372715E-14, diverged 0/2000
    //   inpt:    max relative diff 8.482273E-15, diverged 0/1000
    //   P33:     max relative diff 5.406108E-14, diverged 0/2000
    //   PSAN02n: max relative diff 6.272130E-15, diverged 0/2000
    //   HMX:     max relative diff 1.118670E-14, diverged 0/2000
    //
    // Re-measured 2026-10-01 at attempt budget 8192 (the constant above; previously 256), Release, one run;
    // every figure inside the unchanged tiers:
    //
    //   HPEPA3:  max relative diff 4.425692E-14, diverged 0/2000
    //   inpt:    max relative diff 1.200943E-14, diverged 0/1000
    //   P33:     max relative diff 2.573474E-14, diverged 0/2000
    //   PSAN02n: max relative diff 2.495024E-15, diverged 0/2000
    //   HMX:     max relative diff 2.187533E-14, diverged 0/2000
    //
    // The 2026-09-18 figures above and the paragraph below are the budget-256 record that derived the tiers.
    //
    // Genuinely different figures from the same formulations and the same code path otherwise (confirmed by
    // running this theory twice on the fixed code: both runs reproduced the figures above to the same seven
    // significant digits, so the change from the pre-fix numbers is the fix, not run-to-run noise) — as
    // expected, since every bridge of cycle 1 now actually reaches `BridgeWindow`/`DM`/`VM` against a
    // populated histogram, rather than the empty one QKS1 used to be.
    //
    // RelativeTolerance: all five still cluster within one order of magnitude of each other (6.3E-15 to
    // 2.4E-14, tighter than the pre-fix spread), the scale of double-precision summation noise over a whole
    // cycle's worth of accepted particles added in a different order/hardware path (root BOOT.md,
    // "Deterministic under every schedule" — the fold is order-fixed *within* an accelerator, not *across*
    // accelerators, so a last-ULP libdevice/CoreCLR difference in one particle's draws is the only source of
    // this noise, not scheduling). One tolerance is kept for all five formulations, rather than one
    // tightened to each formulation's own worst case, to avoid overfitting a threshold to a single run:
    // 5E-13, a margin of roughly 21x over the actual worst observed figure (HPEPA3) — tightened from the
    // pre-fix 1E-12 now that a real re-measurement exists to derive it from, rather than kept merely because
    // the new figures still happen to fit under it.
    //
    // MaxDivergedShare: every formulation still observed zero diverging particles in its sample (same sample
    // sizes as before the fix), so the figure typed here is not "0" (that would be an expected value read
    // off a single run rather than a derived bound, and would fail the very next run that observes one
    // divergent particle by chance) but the standard upper confidence bound for "zero successes in n
    // Bernoulli trials", the rule of three (3/n, ~95% confidence the true share is at or below it): 3/2000 =
    // 0.0015 for the four formulations whose cycle-1 has at least 2000 particles, 3/1000 = 0.003 for inpt —
    // unchanged from before the fix, since the sample sizes and the zero-divergence observation are both
    // the same.
    //
    // Neither figure is loosened without a new measurement (this node's own BOOT.md, Taboos).
    public static readonly IReadOnlyDictionary<string, (double RelativeTolerance, double MaxDivergedShare)> Tiers =
        new Dictionary<string, (double, double)>
        {
            ["HPEPA3"] = (5e-13, 3.0 / 2000.0),
            ["inpt"] = (5e-13, 3.0 / 1000.0),
            ["P33"] = (5e-13, 3.0 / 2000.0),
            ["PSAN02n"] = (5e-13, 3.0 / 2000.0),
            ["HMX"] = (5e-13, 3.0 / 2000.0),
        };

    [Theory]
    [Trait("Category", "Long")]
    [InlineData("HPEPA3")]
    [InlineData("inpt")]
    [InlineData("P33")]
    [InlineData("PSAN02n")]
    [InlineData("HMX")]
    public void CudaAgreesWithTheCpuAcceleratorOnAWholeCycleBatch(string formulationName)
    {
        using var cuda = Engine.Create(AcceleratorKind.Cuda, RecordBudgetBytes);

        // This engine's own outcome (Opus audit item 3, 2026-09-18), not the ambient AcceleratorChoice.CudaForbidden:
        // a concurrently running test class's own injected kill switch cannot affect what this engine,
        // created with the real process environment, actually got.
        if (cuda.Accelerator.CudaSkippedBecause is not null)
        {
            CudaRequirement.FailIfRequired(cuda.Accelerator.CudaSkippedBecause);
            var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
            cuda.Load(in setup, bounds, cumulative, pocketForming);
            Assert.Equal(BatchStatus.AcceleratorUnavailable,
                cuda.RunBatch(StreamLayout.Independent, 0UL, 0UL, 1, 1, 10, out _));
            output.WriteLine($"{formulationName}: CUDA forbidden ({cuda.Accelerator.CudaSkippedBecause}); tier not checked in this run.");
            return;
        }

        var (RelativeTolerance, MaxDivergedShare) = Tiers[formulationName];

        // Cycle 0 on the CPU accelerator; forked onto a CPU and the CUDA engine above for cycle 1
        // (DeterminismTests' own forking technique: WriteTotals + SetCycle reproduce the identical starting
        // point).
        using var warmup = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, formulationName, AttemptsPerLaunch);
        var setupValue = ready.Setup;

        using var cpu = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes);
        cpu.Load(in setupValue, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        cpu.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        cpu.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        // Guards the WriteTotals defect (src/Execution/BOOT.md, "third look"): without it, this fork's QKS1
        // silently stayed all-zero and cycle 1's bridge draws never reached a populated window.
        Assert.Contains(cpu.DebugReadQks1(), v => v != 0.0);

        cuda.Load(in setupValue, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        cuda.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        cuda.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(cuda.DebugReadQks1(), v => v != 0.0);

        var particlesPerCycle = ready.Formulation.ParticlesPerCycle;

        // Aggregate totals of the whole cycle-1 batch, one launch chain per accelerator.
        var cpuStatus = cpu.RunBatch(StreamLayout.Independent, 0UL, ready.NextOrdinal, particlesPerCycle, AttemptsPerLaunch, MaxAttemptsPerParticle, out _);
        var cudaStatus = cuda.RunBatch(StreamLayout.Independent, 0UL, ready.NextOrdinal, particlesPerCycle, AttemptsPerLaunch, MaxAttemptsPerParticle, out _);
        Assert.Equal(BatchStatus.Ok, cpuStatus);
        Assert.Equal(BatchStatus.Ok, cudaStatus);

        var cpuInteger = new long[setupValue.Layout.IntegerLength];
        var cpuReal = new double[setupValue.Layout.RecordLength];
        cpu.ReadTotals(cpuInteger, cpuReal);

        var cudaInteger = new long[setupValue.Layout.IntegerLength];
        var cudaReal = new double[setupValue.Layout.RecordLength];
        cuda.ReadTotals(cudaInteger, cudaReal);

        var maxRelativeDiff = 0.0;
        for (var i = 0; i < cpuReal.Length; i++)
        {
            var scale = Math.Max(Math.Abs(cpuReal[i]), Math.Abs(cudaReal[i]));
            if (scale < 1e-12)
            {
                continue;
            }

            var relativeDiff = Math.Abs(cpuReal[i] - cudaReal[i]) / scale;
            maxRelativeDiff = Math.Max(maxRelativeDiff, relativeDiff);
        }

        // Decision-divergence sample: an independent pair of engines loaded with the exact same cycle-1
        // state, run particle by particle so each side's own final stream can be compared.
        using var cpuSample = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes);
        cpuSample.Load(in setupValue, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        cpuSample.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        cpuSample.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(cpuSample.DebugReadQks1(), v => v != 0.0);

        using var cudaSample = Engine.Create(AcceleratorKind.Cuda, RecordBudgetBytes);
        cudaSample.Load(in setupValue, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        cudaSample.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        cudaSample.SetCycle(1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(cudaSample.DebugReadQks1(), v => v != 0.0);

        var sampleSize = Math.Min(DivergenceSampleSize, particlesPerCycle);
        var diverged = 0;
        for (var i = 0; i < sampleSize; i++)
        {
            var ordinal = ready.NextOrdinal + (ulong)i;
            var cpuStreams = StreamSeeds.ForBatchedParticle(StreamLayout.Independent, 0UL, ordinal);
            var cudaStreams = cpuStreams;

            var cpuParticleStatus = cpuSample.RunContinuedBatch(ref cpuStreams, AttemptsPerLaunch, MaxAttemptsPerParticle, out _);
            var cudaParticleStatus = cudaSample.RunContinuedBatch(ref cudaStreams, AttemptsPerLaunch, MaxAttemptsPerParticle, out _);

            if (cpuParticleStatus != cudaParticleStatus || !StreamsEqual(cpuStreams, cudaStreams))
            {
                diverged++;
            }
        }

        var divergedShare = (double)diverged / sampleSize;

        output.WriteLine($"{formulationName}: max relative diff on real totals = {maxRelativeDiff:E} (tier {RelativeTolerance:E}); " +
                          $"diverged {diverged}/{sampleSize} sampled particles = {divergedShare:P2} (tier {MaxDivergedShare:P2})");

        Assert.True(maxRelativeDiff <= RelativeTolerance,
            $"{formulationName}: max relative diff {maxRelativeDiff:E} exceeds the tier {RelativeTolerance:E}");
        Assert.True(divergedShare <= MaxDivergedShare,
            $"{formulationName}: diverged share {divergedShare:P2} exceeds the tier {MaxDivergedShare:P2}");
    }

    private static bool StreamsEqual(StreamSet a, StreamSet b) =>
        StateEqual(a.S1, b.S1) && StateEqual(a.S2, b.S2) && StateEqual(a.S3, b.S3) &&
        StateEqual(a.S4, b.S4) && StateEqual(a.S5, b.S5) && StateEqual(a.S6, b.S6);

    private static bool StateEqual(Mcg128State a, Mcg128State b) => a.Low == b.Low && a.High == b.High;
}
