using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L0 (BOOT.md): every <see cref="Math"/> function the particle program calls (root BOOT.md,
/// Constraints: "Log, Sqrt, Pow, Acos, Sin, Tan, Abs, Max, Min, Floor"), through <see cref="MathProbe"/>
/// and <see cref="Kernels.ProbeMath"/> — the CPU accelerator against <see cref="Math"/> bit for bit, and
/// CUDA (through the libdevice post-link) against the CPU accelerator.
/// </summary>
public sealed class MathProbeTests(ITestOutputHelper output)
{
    // A representative domain: strictly inside (0, 1), so Log/Sqrt/Acos(v - 1) all stay in their domain
    // (Kernels.ProbeMath: outputs[3] = Math.Acos(v - 1.0), v in (0, 1) => v - 1 in (-1, 0)).
    private static readonly double[] Inputs = [0.001, 0.05, 0.1, 0.2, 0.33333333, 0.5, 0.7, 0.9, 0.999];

    /// <summary>
    /// L0 (BOOT.md, Opus audit item 11, 2026-09-18): <see cref="MathProbe.StrideCount"/> is a
    /// compile-time duplicate of <see cref="MathProbe.FunctionCount"/> (<see cref="MathProbe"/>'s own doc
    /// comment on <c>StrideCount</c> says a test ties the two together; none did until this one). Without
    /// this check a new function added to <see cref="MathProbe.Functions"/> without updating
    /// <c>StrideCount</c> would misalign every probe output one column at a time, and
    /// <see cref="CpuAcceleratorMatchesSystemMathBitForBit"/>'s own indexing (<c>i * StrideCount + f</c>,
    /// <c>f &lt; FunctionCount</c>) would silently read inside the next input's row rather than fail loudly.
    /// </summary>
    [Fact]
    public void StrideCountEqualsFunctionCount() => Assert.Equal(MathProbe.StrideCount, MathProbe.FunctionCount);

    [Fact]
    public void CpuAcceleratorMatchesSystemMathBitForBit()
    {
        using var engine = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        var actual = engine.ProbeMath(Inputs);

        for (var i = 0; i < Inputs.Length; i++)
        {
            var v = Inputs[i];
            var expected = new[]
            {
                Math.Log(v), Math.Sqrt(v), Math.Pow(v, MathProbe.PowExponent), Math.Acos(v - 1.0),
                Math.Sin(v), Math.Tan(v), Math.Abs(v - 0.5), Math.Max(v, 0.5), Math.Min(v, 0.5), Math.Floor(v * 10.0),
            };

            for (var f = 0; f < MathProbe.FunctionCount; f++)
            {
                Assert.Equal(expected[f], actual[i * MathProbe.StrideCount + f]);
            }
        }
    }

    [Fact]
    public void CudaAgreesWithTheCpuAcceleratorWithinAMeasuredTolerance()
    {
        using var cpu = Engine.Create(AcceleratorKind.Cpu, 1L << 20);
        using var cuda = Engine.Create(AcceleratorKind.Cuda, 1L << 20);

        // Branches on this specific cuda engine's own reported outcome (Opus audit item 3, 2026-09-18), not
        // the ambient AcceleratorChoice.CudaForbidden: a concurrently running test class's own injected
        // kill switch (CudaEnvironment.Forbidding) cannot affect what this engine, created with the real
        // process environment, actually got.
        if (cuda.Accelerator.CudaSkippedBecause is not null)
        {
            CudaRequirement.FailIfRequired(cuda.Accelerator.CudaSkippedBecause);
            Assert.Equal(BatchStatus.AcceleratorUnavailable, RunTrivialBatchToProveRefusal(cuda));
            return;
        }

        Assert.True(cuda.Accelerator.LibDeviceLinked);

        var cpuValues = cpu.ProbeMath(Inputs);
        var cudaValues = cuda.ProbeMath(Inputs);

        // Tier derived by measurement (2026-09-18, this machine's RTX 5070 Ti, this test's own inputs):
        // the observed maximum absolute difference against the CPU accelerator was 2.22e-16 (Tan, one
        // machine epsilon at 1.0) on the domain above — effectively last-ULP agreement. 1e-12 keeps a
        // thousandfold margin over what was measured rather than the value itself, so the check stays
        // meaningful (not tuned to today's exact float) without being loose enough to hide a real
        // regression (src/Execution/BOOT.md, "Math probe tolerance").
        const double tolerance = 1e-12;
        var maxDiff = 0.0;
        var worstFunction = -1;
        for (var i = 0; i < cpuValues.Length; i++)
        {
            var diff = Math.Abs(cpuValues[i] - cudaValues[i]);
            if (diff > maxDiff)
            {
                maxDiff = diff;
                worstFunction = i % MathProbe.StrideCount;
            }

            Assert.True(diff <= tolerance, $"index {i}: cpu={cpuValues[i]}, cuda={cudaValues[i]}, diff={diff}");
        }

        output.WriteLine($"max |cpu-cuda| = {maxDiff:E} at function {MathProbe.Functions[worstFunction]}");
    }

    private static BatchStatus RunTrivialBatchToProveRefusal(Engine engine)
    {
        var (setup, bounds, cumulative, pocketForming) = ConstructedSetups.ReachesNeighbourLoop();
        engine.Load(in setup, bounds, cumulative, pocketForming);
        return engine.RunBatch(Random.StreamLayout.Independent, 0UL, 0UL, 1, 1, 10, out _);
    }
}
