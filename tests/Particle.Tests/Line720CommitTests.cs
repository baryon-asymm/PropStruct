using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Line 720 commits <c>Fmkarm2</c> before the acceptance tests of lines 723-738 under every
/// model, <c>Corrected</c> included (<c>ACCEPTANCE.md</c>; the flag that moved it was
/// withdrawn 2026-10-18). A constructed case: <c>NnMax</c> low enough that line 735 fires
/// after a Var#2 pocket is staged. The same seed gives the same draws whatever <c>NnMax</c>
/// is (the tests of 723-738 decide only the outcome), so the staged sum of the rejected run
/// is the accepted run's. Right: a rejected attempt adds exactly the accepted attempt's
/// bits to <c>Fmkarm2</c>, under <c>None</c> and under <c>All</c>. Red: with the write
/// moved behind <c>ireset == 0</c> (scratch, reverted) the rejected run's cells stay 0.
/// </summary>
public class Line720CommitTests : IClassFixture<CpuHost>
{
    private const double PermissiveNnMax = 1e9;

    // Below any pocket-to-bridge ratio an attempt with a pocket reaches: line 735 fires.
    private const double RejectingNnMax = 1e-9;

    private readonly CpuHost _host;

    public Line720CommitTests(CpuHost host)
    {
        _host = host;
    }

    // (Corrected model, Original precision kind): the Corrected model runs under Binary64 only.
    public static IEnumerable<object[]> CorrectedAndOriginal()
    {
        yield return new object[] { false, false };
        yield return new object[] { true, false };
        yield return new object[] { false, true };
    }

    [Theory]
    [MemberData(nameof(CorrectedAndOriginal))]
    public void ARejectedAttemptStillAddsTheStagedSum(bool corrected, bool original)
    {
        var corrections = corrected ? CorrectionSet.All : CorrectionSet.None;
        var accepted = Run(corrections, PermissiveNnMax, original);
        var rejected = Run(corrections, RejectingNnMax, original);

        Assert.Equal(AttemptOutcome.Accepted, accepted.Result);
        Assert.Equal(AttemptOutcome.RestartedAfterLoop, rejected.Result);
        Assert.Equal(1L, rejected.Conditions9);

        Assert.True(accepted.Fmkarm2.Sum() > 0.0, "the constructed attempt staged no Var#2 pocket.");
        Assert.Equal(
            accepted.Fmkarm2.Select(BitConverter.DoubleToInt64Bits),
            rejected.Fmkarm2.Select(BitConverter.DoubleToInt64Bits));
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
        return new Outcome(
            result, record[layout.Fmkarm2..(layout.Fmkarm2 + scenario.Setup.Nkarm)], totals[layout.Conditions + 8]);
    }

    private sealed record Outcome(AttemptOutcome Result, double[] Fmkarm2, long Conditions9);
}
