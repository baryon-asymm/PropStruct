using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>QKS1</c>, the pocket histogram the plane reads, in the two places that make it (<c>src/Statistics/
/// ACCEPTANCE.md</c>, A11 and A14). The listing oracle injects it as <c>f32(q / QKSS)</c> (the map's
/// <c>oracle | total</c> row) and <c>CycleStatistics.Compute</c> recomputes <c>r32(q / QKSS)</c>: both divide by
/// the exact count, which is what the executable does at Fortran 718, <c>QKS1 = QKS/real(QKSS)</c> (the bytes at
/// 0x40B7FA-0x40B8AF: <c>fild qword</c> loads <c>QKSS</c> exactly and each cell is <c>fild qword; fdiv; fstp
/// dword</c>, so only the quotient rounds). The reading before 2026-10-03, <c>f32(q / f32(QKSS))</c>, is a
/// different expression above 2^24: the first test holds the agreement on every case of the fixture, the
/// second the pair of known answers at 2^24 and 2^24 + 1 that tells the two readings apart.
/// </summary>
public class Qks1NormalisationTests
{
    private const int Binary32Integers = 1 << 24;

    /// <summary>
    /// Compute's histogram is the oracle's injection, bit for bit, on every cycle of every case. Both divide by
    /// the exact count, so the equality is a fact of the expression, not of the range of the fixture's counts.
    /// </summary>
    [Fact]
    public void ComputeNormalisesQksAsTheOracleInjectsItOnEveryCaseOfTheFixture()
    {
        var compared = 0;
        foreach (var oracleCase in OracleFixture.Cases)
        {
            var port = PortSetup.Prepare(oracleCase.Formulation, oracleCase.Flags, oracleCase.Menu);
            var (setup, echoes) = OracleRun.Echoes(port, oracleCase);
            foreach (var cycle in oracleCase.Cycles)
            {
                var injected = cycle.Totals["qks1"].Values();
                var computed = OracleRun.Compute(port, setup, echoes, cycle).Report.Qks1;
                Assert.Equal(injected.Length, computed.Length);
                for (var k = 0; k < injected.Length; k++)
                {
                    compared++;
                    Assert.True(BitConverter.DoubleToInt64Bits(injected[k]) == BitConverter.DoubleToInt64Bits(computed[k]),
                        $"{oracleCase.Id} cycle {cycle.Cycle} cell {k}: the oracle injected {injected[k]:R}, Compute computed {computed[k]:R} at QKSS {cycle.Totals["qkss"].At(0)}");
                }
            }
        }

        Assert.True(compared > 0);
    }

    /// <summary>
    /// The pair of known answers, through the same path. At <c>QKSS = 2^24</c> the divisor is exact in binary32 either
    /// way and the cells are <c>q·2^-24</c>. At <c>2^24 + 1</c> the reading the executable does not follow, a divisor
    /// rounded to binary32 (<c>2^24</c>), would give exactly 2^-24 and 1 for <c>q</c> = 1 and 2^24; Compute, dividing
    /// by the count as the executable does, gives the quotient of the count itself rounded once: not those values, and
    /// one binary32 unit from them at most.
    /// </summary>
    [Fact]
    public void ComputeDividesByTheExactCountAndPartsFromABinary32DivisorOneCountAboveTwoToTheTwentyFour()
    {
        var oracleCase = OracleFixture.Cases[0];
        var port = PortSetup.Prepare(oracleCase.Formulation, oracleCase.Flags, oracleCase.Menu);
        var (setup, echoes) = OracleRun.Echoes(port, oracleCase);

        var atBound = Qks1(port, setup, echoes, 1L, Binary32Integers - 1L);
        Assert.Equal(Math.ScaleB(1.0, -24), atBound[0]);
        Assert.Equal((Binary32Integers - 1) * Math.ScaleB(1.0, -24), atBound[1]);

        var above = Qks1(port, setup, echoes, 1L, Binary32Integers);
        var count = Binary32Integers + 1.0;
        Assert.Equal((double)(float)(1.0 / count), above[0]);
        Assert.Equal((double)(float)(Binary32Integers / count), above[1]);
        var roundedDivisor = new[] { Math.ScaleB(1.0, -24), 1.0 };
        for (var k = 0; k < roundedDivisor.Length; k++)
        {
            Assert.NotEqual(roundedDivisor[k], above[k]);
            Assert.True(Math.Abs(above[k] - roundedDivisor[k]) <= roundedDivisor[k] * Math.ScaleB(1.0, -23), $"cell {k}: {above[k]:R} is more than one binary32 unit from {roundedDivisor[k]:R}");
        }
    }

    /// <summary>Compute's <c>Qks1</c> for totals whose first two <c>QKS</c> cells are <paramref name="first"/> and <paramref name="second"/>, every other total empty.</summary>
    private static double[] Qks1(PortSetup port, ModelSetup setup, SetupEchoes echoes, long first, long second)
    {
        var buffers = ComputeBuffers.Empty(setup);
        buffers.Integers[setup.Layout.Qks] = first;
        buffers.Integers[setup.Layout.Qks + 1] = second;
        return CycleStatistics.Compute(setup, port.Tables, echoes, port.Inputs, cycleIndex: 0,
            buffers.Integers, buffers.Reals, new double[setup.Ndok], out _, out _).Qks1;
    }
}
