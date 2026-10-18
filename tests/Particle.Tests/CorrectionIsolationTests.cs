using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// One flag changes what its entry names and nothing else (<c>ACCEPTANCE.md</c>): the same
/// cycle-1 sequence of a reference formulation under <c>None</c> and under one flag, attempt
/// by attempt, outcomes, streams, integer totals and every record field bit-identical but the
/// fields the flag's entry names, which move. Red when the flag changes anything else: a
/// scratch mutation of <c>bb</c> under <c>BridgeFrustum</c> turns it red.
/// </summary>
public class CorrectionIsolationTests : IClassFixture<CpuHost>
{
    private const int AttemptCount = 10_000;

    private readonly CpuHost _host;

    public CorrectionIsolationTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> Formulations()
    {
        foreach (var name in new[] { "HPEPA3", "inpt", "P33" })
        {
            yield return new object[] { name };
        }
    }

    /// <summary>
    /// <c>BridgeFrustum</c> alone leaves outcomes, streams, integer totals and every record
    /// field but <c>Vsmkm</c>, <c>VmkmTotal</c> and <c>VmkmTotal2</c> bit-identical and moves
    /// those three.
    /// </summary>
    [Theory]
    [MemberData(nameof(Formulations))]
    public void BridgeFrustumMovesTheBridgeVolumeFieldsAndNothingElse(string formulation)
    {
        using var original = CycleOneRun.TakeToCycleOne(_host.Accelerator, formulation, PrecisionKind.Binary64);
        using var frustum = CycleOneRun.TakeToCycleOne(
            _host.Accelerator, formulation, PrecisionKind.Binary64, CorrectionSet.BridgeFrustum);

        var layout = original.Setup.Layout;
        var named = new List<int>();
        AddRange(named, layout.Vsmkm, original.Setup.Ndok);
        AddRange(named, layout.VmkmTotal, original.Setup.Ndok);
        AddRange(named, layout.VmkmTotal2, 1);

        var moved = new HashSet<int>();
        for (var attempt = 0; attempt < AttemptCount; attempt++)
        {
            var left = original.Step();
            var right = frustum.Step();

            Assert.Equal(left.Outcome, right.Outcome);
            AssertStreamsEqual(attempt, left.Streams, right.Streams);
            Assert.True(left.IntegerTotals.SequenceEqual(right.IntegerTotals), $"attempt {attempt}: an integer total moved.");

            for (var field = 0; field < left.Record.Length; field++)
            {
                var same = BitConverter.DoubleToInt64Bits(left.Record[field]) == BitConverter.DoubleToInt64Bits(right.Record[field]);
                if (named.Contains(field))
                {
                    if (!same)
                    {
                        _ = moved.Add(field);
                    }
                }
                else
                {
                    Assert.True(same, $"attempt {attempt}: record field {field} moved and is not named by BridgeFrustum.");
                }
            }
        }

        Assert.Contains(moved, field => field >= layout.Vsmkm && field < layout.Vsmkm + original.Setup.Ndok);
        Assert.Contains(moved, field => field >= layout.VmkmTotal && field < layout.VmkmTotal + original.Setup.Ndok);
        Assert.Contains(layout.VmkmTotal2, moved);
    }

    private static void AddRange(List<int> fields, int offset, int length)
    {
        for (var i = 0; i < length; i++)
        {
            fields.Add(offset + i);
        }
    }

    private static void AssertStreamsEqual(int attempt, StreamSet left, StreamSet right)
    {
        AssertStreamEqual(attempt, "S1", left.S1, right.S1);
        AssertStreamEqual(attempt, "S2", left.S2, right.S2);
        AssertStreamEqual(attempt, "S3", left.S3, right.S3);
        AssertStreamEqual(attempt, "S4", left.S4, right.S4);
        AssertStreamEqual(attempt, "S5", left.S5, right.S5);
        AssertStreamEqual(attempt, "S6", left.S6, right.S6);
    }

    private static void AssertStreamEqual(int attempt, string name, Mcg128State left, Mcg128State right)
        => Assert.True(left.Low == right.Low && left.High == right.High, $"attempt {attempt}: stream {name} moved.");
}
