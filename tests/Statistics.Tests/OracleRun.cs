using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>CycleStatistics.Compute</c> run on one cycle of one case of the listing oracle's fixture: the setup of the
/// port, its echoes completed with the executable's own pre-loop values, the cycle's totals written into the port's
/// buffers through <see cref="OracleBinding"/>. Shared by every test that compares the plane with the oracle.
/// </summary>
internal static class OracleRun
{
    /// <summary>The port's setup of a case and its echoes, <c>DOKM</c>, <c>DOKSD</c> and <c>GGG</c> the executable's own.</summary>
    public static (ModelSetup Setup, SetupEchoes Echoes) Echoes(PortSetup port, OracleCase oracleCase)
    {
        var setup = port.Setup;
        var echoes = Setup.CompleteEchoes(ref setup, port.Pending, port.Pending.Ddokmax) with
        {
            Dokm = oracleCase.Executed("dokm"),
            Doksd = oracleCase.Executed("doksd"),
            OxidizerMassFractionEffective = oracleCase.Executed("ggg"),
        };
        return (setup, echoes);
    }

    /// <summary>Runs <c>Compute</c> on the totals the oracle injected in <paramref name="cycle"/>.</summary>
    public static ComputeResult Compute(PortSetup port, ModelSetup setup, SetupEchoes echoes, OracleCycle cycle)
    {
        var buffers = ComputeBuffers.Empty(setup);
        foreach (var (name, slot) in cycle.Totals)
        {
            if (OracleBinding.Totals.TryGetValue(name, out var write))
            {
                write(buffers, slot);
            }
        }

        var nextPdoksmall = new double[setup.Ndok];
        var report = CycleStatistics.Compute(setup, port.Tables, echoes, port.Inputs, cycle.Cycle,
            buffers.Integers, buffers.Reals, nextPdoksmall, out var nextDmaxxx, out var status);
        Assert.Equal(CategoriesStatus.Ok, status);
        return new ComputeResult(report, buffers, nextPdoksmall, nextDmaxxx);
    }
}
