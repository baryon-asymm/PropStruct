using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// <c>ExactConstants</c> at this node's sites (<c>ACCEPTANCE.md</c>). Right: on a constructed
/// attempt the sphere-volume terms the record receives (<c>Allvdok</c>, <c>Svd</c>,
/// <c>Vks</c>) are the <c>None</c> terms times pi / 3.14159 to 1e-14 relative, while the
/// effect itself is 8.45e-7. <c>Vks</c> is the exception to "the <c>None</c> term times the
/// ratio": its pocket radius is <c>rrL - (Dr + Db) / 2</c> and <c>rrL</c> itself moves under
/// the flag (the Poisson part, <c>BOOT.md</c>, "ExactConstants"), so its terms are held to
/// pi / 6 times the pocket cubes the record keeps next to them (<c>Dp31</c>) instead. Red:
/// under <c>None</c> the same relations give 3.14159, the ratio of 1, 8.45e-7 away.
/// </summary>
public class ExactConstantsTests : IClassFixture<CpuHost>
{
    // The original's literal for pi at the sphere-volume sites (Fortran 469, 484, ...).
    private const double SphereLiteral = 3.14159;

    private const double RelativeTolerance = 1e-14;

    private readonly CpuHost _host;

    public ExactConstantsTests(CpuHost host)
    {
        _host = host;
    }

    [Fact]
    public void EveryAllvdokCellIsScaledByPiOverTheLiteral()
    {
        var (none, exact) = RunBothModels();

        var cells = 0;
        for (var i = 0; i < none.Ndok; i++)
        {
            var original = none.Record[none.Layout.Allvdok + i];
            if (original != 0.0)
            {
                cells++;
                var error = RelativeError(original * (Math.PI / SphereLiteral), exact.Record[none.Layout.Allvdok + i]);
                Assert.True(error <= RelativeTolerance, $"Allvdok[{i}] is {error} from the None cell times pi / {SphereLiteral}.");
            }
        }

        Assert.True(cells > 0, "the constructed attempt left no Allvdok term.");
    }

    [Fact]
    public void EverySvdCellIsScaledByPiOverTheLiteral()
    {
        var (none, exact) = RunBothModels();

        var cells = 0;
        for (var i = 0; i < none.Ndok; i++)
        {
            var original = none.Record[none.Layout.Svd + i];
            if (original != 0.0)
            {
                cells++;
                var error = RelativeError(original * (Math.PI / SphereLiteral), exact.Record[none.Layout.Svd + i]);
                Assert.True(error <= RelativeTolerance, $"Svd[{i}] is {error} from the None cell times pi / {SphereLiteral}.");
            }
        }

        Assert.True(cells > 0, "the constructed attempt left no Svd term.");
    }

    [Fact]
    public void TheVksTermsAreOneSixthOfPiTimesTheRecordedPocketCubesUnderTheFlagAndOfTheLiteralUnderNone()
    {
        var (none, exact) = RunBothModels();

        Assert.True(Sum(none.IntegerTotals, none.Layout.Qks, none.Nkarm) > 0, "the constructed attempt counted no pocket.");

        Assert.True(VksAgainstSixthOf(Math.PI, exact) <= RelativeTolerance, "under the flag Vks is not pi / 6 times the pocket cubes.");
        Assert.True(VksAgainstSixthOf(SphereLiteral, none) <= RelativeTolerance, "under None Vks is not 3.14159 / 6 times the pocket cubes.");

        // Red: the check separates the two constants, each one's own ratio is the effect away from the other's.
        Assert.True(VksAgainstSixthOf(Math.PI, none) > 1e-7, "the check does not tell pi from the literal.");
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
                worst = Math.Max(worst, RelativeError(original * (Math.PI / SphereLiteral), original));
            }
        }

        Assert.True(worst > 1e-7, $"under None the cells are {worst} from the scaled ones; the check would not see the missing flag.");
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

    private static double RelativeError(double expected, double actual) => Math.Abs(actual - expected) / Math.Abs(expected);

    private static double VksAgainstSixthOf(double pi, Outcome run)
    {
        var vks = 0.0;
        for (var i = 0; i < run.Nkarm; i++)
        {
            vks += run.Record[run.Layout.Vks + i];
        }

        return RelativeError(pi / 6.0 * run.Record[run.Layout.Dp31], vks);
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

    private sealed record Outcome(
        AttemptOutcome Result, StreamSet Streams, AccumulatorLayout Layout, int Ndok, int Nkarm, double[] Record, long[] IntegerTotals);
}
