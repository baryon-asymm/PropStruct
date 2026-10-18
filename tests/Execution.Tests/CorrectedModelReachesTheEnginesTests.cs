using PropStruct.Particle;
using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The positive control of every <c>Corrected</c> row of this node (root BOOT.md, Taboos, "proven twice"; this
/// node's BOOT.md, "The model kind rides in <c>ModelSetup</c>"): an identity such as "host threads equal the
/// kernel oracle under <see cref="CorrectionSet.All"/>" holds just as well if the set never reached the
/// attempts, so this class forks one cycle-1 state of HPEPA3, prepared under <c>None</c>, onto an engine twice,
/// loading a setup that differs in nothing but <c>ModelSetup.Corrections</c> (<see cref="CorrectionSet.None"/>
/// against <see cref="CorrectionSet.All"/>; so no setup-plane or statistics difference can stand in for the
/// attempt's), and requires the real totals of the same batch to part by far more than the tier of the GPU/CPU
/// table. Cycle 1, not cycle 0: with an empty pocket histogram cycle 0 forms almost no bridge, and the first
/// 20000 particles of cycle 0 part by only 3.6E-05. The threshold, 1E-6, is far above the observed GPU/CPU
/// noise (5E-14 at most) and far under the effect, which the test prints.
/// </summary>
public sealed class CorrectedModelReachesTheEnginesTests(ITestOutputHelper output)
{
    private const int ParticleCount = 5000;
    private const int AttemptsPerLaunch = 64;
    private const long MaxAttemptsPerParticle = 10_000_000;
    private const long RecordBudgetBytes = 1L << 28;
    private const double MinimumRelativeEffect = 1e-6;

    [Fact]
    public void TheSetReachesTheHostThreadPath() => AssertTheSetChangesTheTotals(AcceleratorKind.Cpu);

    [Fact]
    public void TheSetReachesTheCudaKernel() => AssertTheSetChangesTheTotals(AcceleratorKind.Cuda);

    private void AssertTheSetChangesTheTotals(AcceleratorKind kind)
    {
        using var engine = Engine.Create(kind, RecordBudgetBytes);
        if (engine.Accelerator.CudaSkippedBecause is not null)
        {
            CudaRequirement.FailIfRequired(engine.Accelerator.CudaSkippedBecause);
            output.WriteLine($"CUDA forbidden ({engine.Accelerator.CudaSkippedBecause}); the set's reach is not checked on it in this run.");
            return;
        }

        using var warmup = Engine.Create(AcceleratorKind.Cpu, RecordBudgetBytes);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, "HPEPA3");
        Assert.Equal(CorrectionSet.None, ready.Setup.Corrections);

        var original = RunCycle1Batch(engine, ready, ready.Setup);
        var corrected = ready.Setup;
        corrected.Corrections = CorrectionSet.All;
        var withSet = RunCycle1Batch(engine, ready, corrected);

        var maxRelativeDiff = 0.0;
        for (var i = 0; i < original.Length; i++)
        {
            var scale = Math.Max(Math.Abs(original[i]), Math.Abs(withSet[i]));
            if (scale >= 1e-12)
            {
                maxRelativeDiff = Math.Max(maxRelativeDiff, Math.Abs(original[i] - withSet[i]) / scale);
            }
        }

        output.WriteLine($"{kind}: max relative diff of the real totals, None against All = {maxRelativeDiff:E}");
        Assert.True(maxRelativeDiff > MinimumRelativeEffect,
            $"{kind}: the set changed no total by more than {MinimumRelativeEffect:E} (max {maxRelativeDiff:E}); it did not reach the attempts");
    }

    private static double[] RunCycle1Batch(Engine engine, ReferenceFormulationDriver.Cycle1Ready ready, ModelSetup setup)
    {
        engine.Load(in setup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        engine.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])ready.RealTotals.Clone());
        engine.SetCycle(cycleFlag: 1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        var status = engine.RunBatch(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal, ParticleCount,
            AttemptsPerLaunch, MaxAttemptsPerParticle, out _);
        Assert.Equal(BatchStatus.Ok, status);

        var integerTotals = new long[setup.Layout.IntegerLength];
        var realTotals = new double[setup.Layout.RecordLength];
        engine.ReadTotals(integerTotals, realTotals);
        return realTotals;
    }
}
