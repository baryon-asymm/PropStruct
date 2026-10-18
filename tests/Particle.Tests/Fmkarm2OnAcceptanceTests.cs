using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// <c>Fmkarm2OnAcceptance</c> on a constructed case (<c>ACCEPTANCE.md</c>): <c>NnMax</c>
/// low enough that line 735 fires after a Var#2 pocket is staged. Red: under <c>None</c>
/// <c>Fmkarm2</c> grows by the staged sum on <c>RestartedAfterLoop</c>. Right: under the
/// flag it is unchanged there, and an <c>Accepted</c> attempt adds the same bits under both.
/// The same seed gives the same draws whatever <c>NnMax</c> is (the tests of lines 723-738
/// decide only the outcome), so the staged sum of the rejected run is the accepted run's.
/// </summary>
public class Fmkarm2OnAcceptanceTests : IClassFixture<CpuHost>
{
    private const double PermissiveNnMax = 1e9;

    // Below any pocket-to-bridge ratio an attempt with a pocket reaches: line 735 fires.
    private const double RejectingNnMax = 1e-9;

    private readonly CpuHost _host;

    public Fmkarm2OnAcceptanceTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> Kinds()
    {
        yield return new object[] { false };
        yield return new object[] { true };
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void UnderNoneARejectedAttemptStillAddsTheStagedSum(bool original)
    {
        var accepted = Run(CorrectionSet.None, PermissiveNnMax, original);
        var rejected = Run(CorrectionSet.None, RejectingNnMax, original);

        Assert.Equal(AttemptOutcome.Accepted, accepted.Result);
        Assert.Equal(AttemptOutcome.RestartedAfterLoop, rejected.Result);
        Assert.Equal(1L, rejected.Conditions9);

        Assert.True(accepted.Fmkarm2.Sum() > 0.0, "the constructed attempt staged no Var#2 pocket.");
        Assert.Equal(accepted.Fmkarm2, rejected.Fmkarm2); // the defect: the rejected attempt's sum entered
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void UnderTheFlagARejectedAttemptAddsNothing(bool original)
    {
        var rejected = Run(CorrectionSet.Fmkarm2OnAcceptance, RejectingNnMax, original);

        Assert.Equal(AttemptOutcome.RestartedAfterLoop, rejected.Result);
        Assert.Equal(1L, rejected.Conditions9);
        Assert.All(rejected.Fmkarm2, cell => Assert.Equal(0.0, cell));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void AnAcceptedAttemptAddsTheSameBitsUnderBoth(bool original)
    {
        var none = Run(CorrectionSet.None, PermissiveNnMax, original);
        var flag = Run(CorrectionSet.Fmkarm2OnAcceptance, PermissiveNnMax, original);

        Assert.Equal(AttemptOutcome.Accepted, none.Result);
        Assert.Equal(AttemptOutcome.Accepted, flag.Result);
        Assert.True(none.Fmkarm2.Sum() > 0.0, "the constructed attempt staged no Var#2 pocket.");
        Assert.Equal(
            none.Fmkarm2.Select(BitConverter.DoubleToInt64Bits),
            flag.Fmkarm2.Select(BitConverter.DoubleToInt64Bits));
        Assert.Equal(
            none.Record.Select(BitConverter.DoubleToInt64Bits),
            flag.Record.Select(BitConverter.DoubleToInt64Bits));
        Assert.Equal(none.IntegerTotals, flag.IntegerTotals);
    }

    private Outcome Run(CorrectionSet corrections, double nnMax, bool original)
    {
        using var scenario = Scenario.Build(_host.Accelerator, size: 1.0, cellSize: 0.1, lambda: 1.0,
            ak3: 0.1, ak4: 10.0, cycleFlag: 1, neighbourBudget: 200, pocketRedrawBudget: 200,
            corrections: corrections, nnMax: nnMax, kind: original ? PrecisionKind.Original : PrecisionKind.Binary64);

        var streams = OriginalSeeds.Streams;
        var result = scenario.Run(ref streams);
        var layout = scenario.Setup.Layout;
        var record = scenario.AllRecords();
        var totals = scenario.AllIntegerTotals();
        return new Outcome(result, record[layout.Fmkarm2..(layout.Fmkarm2 + scenario.Setup.Nkarm)], record, totals,
            totals[layout.Conditions + 8]);
    }

    private sealed record Outcome(AttemptOutcome Result, double[] Fmkarm2, double[] Record, long[] IntegerTotals, long Conditions9);
}
