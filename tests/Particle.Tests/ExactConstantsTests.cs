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

    /// <summary>
    /// The 4π of <c>rrL</c> (line 501) by value: the first neighbour's pocket radius, which
    /// <c>DpMax</c> keeps (line 673, <c>RK = rrL - Dr/2 - Db/2</c>), against the oracle of
    /// <see cref="ConstructedModel.PeekFirstNeighbour"/> given π at both sites, and under
    /// <c>None</c> given the literal at both. The neighbour budget of one ends the attempt
    /// right after that first pocket, so <c>DpMax</c> is that radius alone. Red: the oracle
    /// with the literal at <c>rrL</c> only, the state a site left behind would produce, is
    /// 1e-8 away from the flagged run (the effect is 2.8e-7 of <c>rrL</c>), and so is the
    /// <c>None</c> run from the π oracle.
    /// </summary>
    [Fact]
    public void TheFirstPocketRadiusCarriesTheExactFourPiOfRrlUnderTheFlag()
    {
        var geometry = ConstructedModel.Permissive(1.0, 0.1, 0.1, ak3: 0.01, ak4: 1.0);
        double[] bounds = [1.0, 1.0];
        double[] cumulative = [0.0, 1.0];
        var streams = OriginalSeeds.Streams;

        var (_, _, _, literalAa, maxDrDb) = ConstructedModel.PeekFirstNeighbour(_host.Accelerator, geometry, bounds, cumulative, streams);
        var (_, _, _, exactAa, _) = ConstructedModel.PeekFirstNeighbour(
            _host.Accelerator, geometry, bounds, cumulative, streams, spherePi: Math.PI, radiusPi: Math.PI);
        var (_, _, _, radiusOnlyLiteralAa, _) = ConstructedModel.PeekFirstNeighbour(
            _host.Accelerator, geometry, bounds, cumulative, streams, spherePi: Math.PI, radiusPi: SphereLiteral);

        // The first neighbour is a pocket: AK3 and AK4 bracket its gap ratio.
        var ratio = literalAa / maxDrDb;
        Assert.True(ratio > 0.0, "this seed's first neighbour must not be jammed.");

        var none = FirstPocketRadius(CorrectionSet.None, ratio / 2.0, ratio * 2.0);
        var exact = FirstPocketRadius(CorrectionSet.ExactConstants, ratio / 2.0, ratio * 2.0);

        Assert.True(RelativeError(exactAa, exact) <= RelativeTolerance, $"under the flag the radius is {RelativeError(exactAa, exact)} from the pi oracle.");
        Assert.True(RelativeError(literalAa, none) <= RelativeTolerance, $"under None the radius is {RelativeError(literalAa, none)} from the literal oracle.");
        Assert.True(RelativeError(radiusOnlyLiteralAa, exact) > 1e-8, "the check does not see the 4 pi of rrL left at the literal.");
        Assert.True(RelativeError(exactAa, none) > 1e-8, "the check does not tell None from the flag.");
    }

    private double FirstPocketRadius(CorrectionSet corrections, double ak3, double ak4)
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 0.1,
            ak3: ak3, ak4: ak4, cycleFlag: 0, neighbourBudget: 1, corrections: corrections);

        var streams = OriginalSeeds.Streams;
        Assert.Equal(AttemptOutcome.NeighbourBudgetExceeded, scenario.Run(ref streams));
        Assert.Equal(1L, scenario.IntegerTotal(scenario.Setup.Layout.Nfy));
        return scenario.Record(scenario.Setup.Layout.DpMax);
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
