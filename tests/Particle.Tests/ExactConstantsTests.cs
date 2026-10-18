using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// <c>ExactConstants</c> at this node's sites (<c>ACCEPTANCE.md</c>). Right: on a constructed
/// attempt the sphere-volume terms the record receives (<c>Allvdok</c>, <c>Svd</c>, <c>Vks</c>)
/// are the <c>None</c> terms times pi / 3.14159, within 2 ulp per term added: a cell that
/// sums <c>n</c> terms rounds <c>n</c> times, so its tolerance is <c>2n</c> ulp, and a cell
/// of one term is held to 2. <c>Vks</c> is the exception to "the <c>None</c> term times the
/// ratio": its pocket radius is <c>rrL - (Dr + Db) / 2</c> and <c>rrL</c> itself moves under
/// the flag (the Poisson part, <c>BOOT.md</c>, "ExactConstants"), so its terms are held to
/// pi / 6 times the pocket cubes the record keeps next to them (<c>Dp31</c>) instead. Red:
/// under <c>None</c> the same relations give 3.14159, the ratio of 1, thousands of ulp away.
/// </summary>
public class ExactConstantsTests : IClassFixture<CpuHost>
{
    // The original's literal for pi at the sphere-volume sites (Fortran 469, 484, ...).
    private const double SphereLiteral = 3.14159;

    private const double UlpsPerTerm = 2.0;

    private readonly CpuHost _host;

    public ExactConstantsTests(CpuHost host)
    {
        _host = host;
    }

    [Fact]
    public void EveryAllvdokCellIsScaledByPiOverTheLiteralWithinTwoUlpsPerTerm()
    {
        var (none, exact) = RunBothModels();

        var cells = 0;
        for (var i = 0; i < none.Ndok; i++)
        {
            var original = none.Record[none.Layout.Allvdok + i];
            if (original != 0.0)
            {
                cells++;
                var terms = none.IntegerTotals[none.Layout.Alldok + i];
                var ulps = UlpsFromExpected(original, exact.Record[none.Layout.Allvdok + i]);
                Assert.True(ulps <= UlpsPerTerm * terms, $"Allvdok[{i}] of {terms} terms is {ulps} ulp from the None cell times pi / {SphereLiteral}.");
            }
        }

        Assert.True(cells > 0, "the constructed attempt left no Allvdok term.");
    }

    [Fact]
    public void EverySvdCellIsScaledByPiOverTheLiteralWithinTwoUlpsPerTerm()
    {
        var (none, exact) = RunBothModels();

        // alpha * svd1 + pi / 6 * dr^3: one term per bridge of the accepted attempt and one for the base particle.
        var terms = none.IntegerTotals[none.Layout.IbridgeTotal] + 1;
        var cells = 0;
        for (var i = 0; i < none.Ndok; i++)
        {
            var original = none.Record[none.Layout.Svd + i];
            if (original != 0.0)
            {
                cells++;
                var ulps = UlpsFromExpected(original, exact.Record[none.Layout.Svd + i]);
                Assert.True(ulps <= UlpsPerTerm * terms, $"Svd[{i}] of {terms} terms is {ulps} ulp from the None cell times pi / {SphereLiteral}.");
            }
        }

        Assert.True(cells > 0, "the constructed attempt left no Svd term.");
    }

    [Fact]
    public void TheVksTermsAreOneSixthOfPiTimesTheRecordedPocketCubesUnderTheFlagAndOfTheLiteralUnderNone()
    {
        var (none, exact) = RunBothModels();

        var pockets = Sum(none.IntegerTotals, none.Layout.Qks, none.Nkarm);
        Assert.True(pockets > 0, "the constructed attempt counted no pocket.");

        var underNone = UlpsFromSixthOf(SphereLiteral, none);
        var underFlag = UlpsFromSixthOf(Math.PI, exact);
        Assert.True(underFlag <= UlpsPerTerm * pockets, $"under the flag the Vks cells are {underFlag} ulp from pi / 6 times the pocket cubes.");
        Assert.True(underNone <= UlpsPerTerm * pockets, $"under None the Vks cells are {underNone} ulp from 3.14159 / 6 times the pocket cubes.");

        // Red: the check separates the two constants, each one's own ratio is thousands of ulp from the other's.
        Assert.True(UlpsFromSixthOf(Math.PI, none) > 1000.0 * UlpsPerTerm * pockets, "the check does not tell pi from the literal.");
    }

    [Fact]
    public void UnderNoneTheRatioIsOneAndTheCheckSeesIt()
    {
        var (none, _) = RunBothModels();

        var worst = 0.0;
        for (var i = 0; i < none.Ndok; i++)
        {
            var original = none.Record[none.Layout.Allvdok + i];
            if (original != 0.0)
            {
                worst = Math.Max(worst, UlpsFromExpected(original, original) / none.IntegerTotals[none.Layout.Alldok + i]);
            }
        }

        Assert.True(worst > 1000.0 * UlpsPerTerm, $"under None the cells are {worst} ulp per term from the scaled ones; the check would not see the missing flag.");
    }

    private static double Sum(long[] totals, int offset, int length)
    {
        var sum = 0L;
        for (var i = 0; i < length; i++)
        {
            sum += totals[offset + i];
        }

        return sum;
    }

    private static double UlpsFromSixthOf(double pi, Outcome run)
    {
        var vks = 0.0;
        for (var i = 0; i < run.Nkarm; i++)
        {
            vks += run.Record[run.Layout.Vks + i];
        }

        var expected = pi / 6.0 * run.Record[run.Layout.Dp31];
        return Math.Abs(vks - expected) / (Math.BitIncrement(expected) - expected);
    }

    private (Outcome None, Outcome Exact) RunBothModels()
    {
        var none = Run(CorrectionSet.None);
        var exact = Run(CorrectionSet.ExactConstants);

        Assert.Equal(AttemptOutcome.Accepted, none.Result);
        Assert.Equal(AttemptOutcome.Accepted, exact.Result);

        // The same draws under both: the flag moves values, not which draws follow, on this attempt.
        Assert.True(none.Streams.S1.Low == exact.Streams.S1.Low && none.Streams.S3.Low == exact.Streams.S3.Low
            && none.Streams.S4.Low == exact.Streams.S4.Low && none.Streams.S6.Low == exact.Streams.S6.Low,
            "the flag changed the number of draws of the constructed attempt.");
        Assert.True(none.IntegerTotals.SequenceEqual(exact.IntegerTotals), "the flag moved an integer total of the constructed attempt.");
        return (none, exact);
    }

    private Outcome Run(CorrectionSet corrections)
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, neighbourBudget: 200, pocketRedrawBudget: 200, corrections: corrections);

        var streams = OriginalSeeds.Streams;
        var result = scenario.Run(ref streams);
        return new Outcome(result, streams, scenario.Setup.Layout, scenario.Setup.Ndok, scenario.Setup.Nkarm,
            scenario.AllRecords(), scenario.AllIntegerTotals());
    }

    private static double UlpsFromExpected(double original, double flagged)
    {
        var expected = original * (Math.PI / SphereLiteral);
        return Math.Abs(flagged - expected) / (Math.BitIncrement(Math.Abs(expected)) - Math.Abs(expected));
    }

    private sealed record Outcome(
        AttemptOutcome Result, StreamSet Streams, AccumulatorLayout Layout, int Ndok, int Nkarm, double[] Record, long[] IntegerTotals);
}
