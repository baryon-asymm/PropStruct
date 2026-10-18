using ILGPU.Runtime;
using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// <c>WindowLowerEdge</c> (<c>ACCEPTANCE.md</c>): 10⁶ draws of <see cref="BridgeWindow.SamplePocket"/>
/// over a window built from a constructed QKS1 with <c>mindk ≥ 2</c>. Right: under the flag
/// <c>dpoc[0] = Di·(mindk − 1)</c> and the draws per QKS1 cell match the cell masses (χ² at
/// <c>α = 10⁻³</c>). Red: under <c>None</c> the same test rejects, each draw one cell high.
/// A cell <c>iks</c> covers <c>[(iks − 1)·Di, iks·Di)</c> (Fortran line 675), so the cell of a
/// draw <c>d</c> is <c>⌊d / Di⌋ + 1</c>. <c>Di</c> is a power of two so that the cell edges are
/// exact doubles and no draw is misfiled by rounding.
/// </summary>
public class WindowLowerEdgeTests : IClassFixture<CpuHost>
{
    private const int Draws = 1_000_000;
    private const int Nkarm = 20;
    private const double CellSize = 0.5;

    // Cells mindk = 2 .. maxdk = 8: int(0.2 * 4.0 / 0.5) + 1 and int(0.95 * 4.0 / 0.5) + 1.
    private const double Dr = 4.0;
    private const double Ak3 = 0.2;
    private const double Ak4 = 0.95;
    private const int Mindk = 2;
    private const int Maxdk = 8;

    // The chi-square quantile at 1 - 1e-3 with Maxdk - Mindk = 6 degrees of freedom (NIST/SEMATECH
    // e-Handbook of Statistical Methods, table of the chi-square distribution, 0.999 column, df 6).
    private const double CriticalValue = 22.458;

    private readonly CpuHost _host;

    public WindowLowerEdgeTests(CpuHost host)
    {
        _host = host;
    }

    [Fact]
    public void UnderTheFlagTheDrawsPerCellMatchTheCellMasses()
    {
        var run = Draw(windowLowerEdge: true);

        Assert.Equal(CellSize * (Mindk - 1), run.FirstBreakpoint);
        Assert.Equal(0, run.OutsideTheWindowsCells);
        Assert.True(run.ChiSquare <= CriticalValue, $"chi-square {run.ChiSquare} exceeds {CriticalValue}; counts {string.Join(", ", run.Counts)}.");
    }

    [Fact]
    public void UnderNoneTheSameTestRejectsEachDrawOneCellHigh()
    {
        var run = Draw(windowLowerEdge: false);

        Assert.Equal(CellSize * Mindk, run.FirstBreakpoint);
        Assert.True(run.OutsideTheWindowsCells > 0, "no draw landed above the window's last cell.");
        Assert.True(run.ChiSquare > CriticalValue, $"chi-square {run.ChiSquare} does not exceed {CriticalValue}.");
        Assert.Equal(0, run.Counts[0]); // the window's first cell is never drawn from: one cell high
    }

    /// <summary>
    /// <see cref="Attempt.Run"/> hands the flag to the window: the same constructed attempt
    /// under <c>None</c> and under the flag draws different pockets, so an integer total of
    /// the bridge branch or a record cell moves (red when the flag is not passed on: both
    /// runs are then the same bits).
    /// </summary>
    [Fact]
    public void AttemptRunPassesTheFlagToTheWindow()
    {
        var none = RunAttempt(CorrectionSet.None);
        var flag = RunAttempt(CorrectionSet.WindowLowerEdge);

        Assert.False(
            none.Record.Select(BitConverter.DoubleToInt64Bits).SequenceEqual(flag.Record.Select(BitConverter.DoubleToInt64Bits))
            && none.IntegerTotals.SequenceEqual(flag.IntegerTotals),
            "the flag changed nothing the attempt records.");
    }

    private (double[] Record, long[] IntegerTotals) RunAttempt(CorrectionSet corrections)
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.125, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, neighbourBudget: 200, pocketRedrawBudget: 200,
            corrections: corrections);

        var streams = OriginalSeeds.Streams;
        _ = scenario.Run(ref streams);
        return (scenario.AllRecords(), scenario.AllIntegerTotals());
    }

    private DrawTally Draw(bool windowLowerEdge)
    {
        var masses = new double[Nkarm];
        for (var i = 0; i < Nkarm; i++)
        {
            masses[i] = 1.0 + i;
        }

        var acc = _host.Accelerator;
        using var qks1Buffer = acc.Allocate1D(masses);
        using var fqksBuffer = acc.Allocate1D<double>(Nkarm + 1);
        using var dpocBuffer = acc.Allocate1D<double>(Nkarm + 1);

        Assert.True(BridgeWindow.Build(Dr, Ak3, Ak4, CellSize, Nkarm, windowLowerEdge,
            qks1Buffer.View.BaseView, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, out var nnn1));
        Assert.Equal(Maxdk - Mindk + 2, nnn1); // no early truncation: every cell of the window has mass

        var cells = Maxdk - Mindk + 1;
        var windowMass = 0.0;
        for (var iks = Mindk; iks <= Maxdk; iks++)
        {
            windowMass += masses[iks - 1];
        }

        var firstBreakpoint = dpocBuffer.GetAsArray1D()[0];
        var counts = new int[cells];
        var outside = 0;
        var stream = OriginalSeeds.Streams.S6;
        for (var draw = 0; draw < Draws; draw++)
        {
            var x = Mcg128.Next(ref stream);
            Assert.True(BridgeWindow.SamplePocket(x, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, nnn1, out var pocket));
            var cell = (int)(pocket / CellSize) + 1;
            if (cell is >= Mindk and <= Maxdk)
            {
                counts[cell - Mindk]++;
            }
            else
            {
                outside++;
            }
        }

        var chiSquare = 0.0;
        for (var iks = Mindk; iks <= Maxdk; iks++)
        {
            var expected = Draws * masses[iks - 1] / windowMass;
            var difference = counts[iks - Mindk] - expected;
            chiSquare += difference * difference / expected;
        }

        return new DrawTally(firstBreakpoint, counts, outside, chiSquare);
    }

    private sealed record DrawTally(double FirstBreakpoint, int[] Counts, int OutsideTheWindowsCells, double ChiSquare);
}
