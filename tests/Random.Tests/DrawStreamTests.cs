using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// L1 and the snapshot row of the BOOT.md table: a stream's draws against the <see cref="BigIntegerStepper"/>
/// oracle (10⁶ draws, marked long), and the first 1000 draws of every Original stream against the "draws"
/// approved bit snapshot and of every Independent stream against the separate "independent-draws" snapshot
/// (BOOT.md, "Independent layout": "its first draws are held in a bit snapshot"). Theory parameters name a
/// stream by its low/high seed halves, never the internal <c>Mcg128State</c> (xunit only discovers public
/// methods, and a public method may not expose an internal type in its signature).
/// </summary>
public class DrawStreamTests
{
    private const int SnapshotDrawCount = 1000;
    private const int LongDrawCount = 1_000_000;

    public static IEnumerable<object[]> Streams()
    {
        yield return ["S1", OriginalSeeds.Streams.S1.Low, OriginalSeeds.Streams.S1.High];
        yield return ["S2", OriginalSeeds.Streams.S2.Low, OriginalSeeds.Streams.S2.High];
        yield return ["S3", OriginalSeeds.Streams.S3.Low, OriginalSeeds.Streams.S3.High];
        yield return ["S4", OriginalSeeds.Streams.S4.Low, OriginalSeeds.Streams.S4.High];
        yield return ["S5", OriginalSeeds.Streams.S5.Low, OriginalSeeds.Streams.S5.High];
        yield return ["S6", OriginalSeeds.Streams.S6.Low, OriginalSeeds.Streams.S6.High];
    }

    public static IEnumerable<object[]> IndependentStreams()
    {
        yield return ["S1", IndependentSeeds.Streams.S1.Low, IndependentSeeds.Streams.S1.High];
        yield return ["S2", IndependentSeeds.Streams.S2.Low, IndependentSeeds.Streams.S2.High];
        yield return ["S3", IndependentSeeds.Streams.S3.Low, IndependentSeeds.Streams.S3.High];
        yield return ["S4", IndependentSeeds.Streams.S4.Low, IndependentSeeds.Streams.S4.High];
        yield return ["S5", IndependentSeeds.Streams.S5.Low, IndependentSeeds.Streams.S5.High];
        yield return ["S6", IndependentSeeds.Streams.S6.Low, IndependentSeeds.Streams.S6.High];
    }

    [Theory]
    [MemberData(nameof(Streams))]
    [Trait("Category", "Long")]
    public void MillionDrawsMatchTheBigIntegerStepper(string streamName, ulong seedLow, ulong seedHigh)
    {
        var multiplier = Big128.Pack(SourceLimbs.Limbs(1649, 1650));
        var state = new Mcg128State { Low = seedLow, High = seedHigh };
        var bigState = Big128.Combine(seedLow, seedHigh);

        for (var i = 0; i < LongDrawCount; i++)
        {
            var draw = Mcg128.Next(ref state);
            var (expectedState, expectedDraw) = BigIntegerStepper.Step(bigState, multiplier);
            bigState = expectedState;
            var (expectedLow, expectedHigh) = Big128.Split(expectedState);

            Assert.True(expectedLow == state.Low && expectedHigh == state.High,
                $"{streamName}, draw {i}: state expected ({expectedLow:x16},{expectedHigh:x16}), actual ({state.Low:x16},{state.High:x16}).");
            Assert.True(BitConverter.DoubleToInt64Bits(expectedDraw) == BitConverter.DoubleToInt64Bits(draw),
                $"{streamName}, draw {i}: value expected {expectedDraw:R}, actual {draw:R}.");
        }
    }

    [Theory]
    [MemberData(nameof(Streams))]
    public void FirstThousandDrawsMatchTheApprovedBitSnapshot(string streamName, ulong seedLow, ulong seedHigh)
    {
        var state = new Mcg128State { Low = seedLow, High = seedHigh };
        var draws = new double[SnapshotDrawCount];
        for (var i = 0; i < SnapshotDrawCount; i++)
        {
            draws[i] = Mcg128.Next(ref state);
        }

        BitSnapshot.Verify("draws", streamName, draws);
    }

    [Theory]
    [MemberData(nameof(IndependentStreams))]
    public void FirstThousandDrawsOfIndependentStreamsMatchTheApprovedBitSnapshot(string streamName, ulong seedLow, ulong seedHigh)
    {
        var state = new Mcg128State { Low = seedLow, High = seedHigh };
        var draws = new double[SnapshotDrawCount];
        for (var i = 0; i < SnapshotDrawCount; i++)
        {
            draws[i] = Mcg128.Next(ref state);
        }

        BitSnapshot.Verify("independent-draws", streamName, draws);
    }
}
