using PropStruct.Input;
using PropStruct.Random;
using PropStruct.Statistics;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// The always-on accelerator oracle (BOOT.md, "Host-thread path": "the accelerator oracle stays on every
/// build"). <see cref="Engine.Create"/>'s <c>forceIlgpuKernelsOnCpu</c> parameter keeps the previous
/// ILGPU-kernel-launch behaviour on the CPU accelerator, which no longer carries any production load once
/// the host-thread path is the default (root BOOT.md, "One particle program": "kept as a test oracle of the
/// kernel path only"); this test runs the same 1000 particles through both and checks equal bits, so
/// "kernel-compatible" stays exercised on every build even though the accelerator itself does not. Fast on
/// purpose (HPEPA3's first 1000 particles of cycle 0, no prior cycle needed): unlike the full matrix of
/// <see cref="HostThreadMatrixTests"/>, this row is not <c>Category=Long</c>.
/// </summary>
public sealed class HostThreadOracleTests
{
    private const int ParticleCount = 1000;
    private const int AttemptsPerLaunch = 64;
    private const long MaxAttemptsPerParticle = 1_000_000;

    [Fact]
    public void HostThreadsEqualsTheIlgpuCpuAcceleratorOracleBitForBitOverHpepa3sFirst1000Particles()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "HPEPA3.dat");
        var formulation = DatFile.Read(path);
        var parameters = ModelParameters.Default;

        var prepareStatus = Setup.Prepare(formulation, parameters, Particle.PrecisionKind.Binary64, neighbourBudget: 10_000_000, pocketRedrawBudget: 1_000_000,
            out var setup, out var tables, out var draw, out var pending);
        Assert.Equal(SetupStatus.Ok, prepareStatus);

        using var oracle = Engine.Create(AcceleratorKind.Cpu, 1L << 24, forceIlgpuKernelsOnCpu: true);
        oracle.SampleSize(setup.SizeLaw, setup.FractionCount, tables.Bounds, tables.Cumulative, draw.X, draw.X1, out var dmax, out _);
        _ = Setup.CompleteEchoes(ref setup, pending, dmax); // writes setup.Dmax in place; both engines share this one completed setup

        // The accelerator each engine actually binds to is still Cpu, and identically reported: forcing the
        // ILGPU kernel-launch path is an internal dispatch choice, not a different accelerator kind (BOOT.md,
        // "Host-thread path": "the public AcceleratorKind is unchanged").
        Assert.Equal(AcceleratorKind.Cpu, oracle.Accelerator.Kind);

        oracle.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);
        oracle.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        using var hostThreads = Engine.Create(AcceleratorKind.Cpu, 1L << 24);
        Assert.Equal(AcceleratorKind.Cpu, hostThreads.Accelerator.Kind);
        hostThreads.Load(in setup, tables.Bounds, tables.Cumulative, tables.PocketForming);
        hostThreads.SetCycle(cycleFlag: 0, dmaxxx: 0.0, pdoksmall: new double[setup.Ndok]);

        var oracleStatus = oracle.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, ParticleCount,
            AttemptsPerLaunch, MaxAttemptsPerParticle, out var oracleCounters);
        var hostStatus = hostThreads.RunBatch(StreamLayout.Independent, seed: 0UL, firstOrdinal: 0UL, ParticleCount,
            AttemptsPerLaunch, MaxAttemptsPerParticle, out var hostCounters);

        Assert.Equal(BatchStatus.Ok, oracleStatus);
        Assert.Equal(BatchStatus.Ok, hostStatus);
        Assert.Equal(oracleCounters, hostCounters);

        var oracleInteger = new long[setup.Layout.IntegerLength];
        var oracleReal = new double[setup.Layout.RecordLength];
        oracle.ReadTotals(oracleInteger, oracleReal);

        var hostInteger = new long[setup.Layout.IntegerLength];
        var hostReal = new double[setup.Layout.RecordLength];
        hostThreads.ReadTotals(hostInteger, hostReal);

        Assert.Equal(oracleInteger, hostInteger);
        Assert.Equal(oracleReal, hostReal);
    }
}
