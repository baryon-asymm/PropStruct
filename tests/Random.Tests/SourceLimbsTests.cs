using System.Numerics;
using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// L0 of the BOOT.md table: the packed multiplier and seeds against the limbs of the Fortran source, and the
/// power relation between the six seeds.
/// </summary>
public class SourceLimbsTests
{
    // Fortran source lines 52-63, one stream per pair of lines, as OriginalSeeds.cs traces them.
    private const int Stream6First = 52, Stream6Last = 53;
    private const int Stream5First = 54, Stream5Last = 55;
    private const int Stream4First = 56, Stream4Last = 57;
    private const int Stream3First = 58, Stream3Last = 59;
    private const int Stream2First = 60, Stream2Last = 61;
    private const int Stream1First = 62, Stream1Last = 63;
    private const int MultiplierFirst = 1649, MultiplierLast = 1650;

    [Fact]
    public void EveryStreamSeedHasTenLimbs()
    {
        Assert.Equal(10, SourceLimbs.Limbs(Stream1First, Stream1Last).Count);
        Assert.Equal(10, SourceLimbs.Limbs(Stream2First, Stream2Last).Count);
        Assert.Equal(10, SourceLimbs.Limbs(Stream3First, Stream3Last).Count);
        Assert.Equal(10, SourceLimbs.Limbs(Stream4First, Stream4Last).Count);
        Assert.Equal(10, SourceLimbs.Limbs(Stream5First, Stream5Last).Count);
        Assert.Equal(10, SourceLimbs.Limbs(Stream6First, Stream6Last).Count);
        Assert.Equal(10, SourceLimbs.Limbs(MultiplierFirst, MultiplierLast).Count);
    }

    [Theory]
    [InlineData(Stream1First, Stream1Last, "S1")]
    [InlineData(Stream2First, Stream2Last, "S2")]
    [InlineData(Stream3First, Stream3Last, "S3")]
    [InlineData(Stream4First, Stream4Last, "S4")]
    [InlineData(Stream5First, Stream5Last, "S5")]
    [InlineData(Stream6First, Stream6Last, "S6")]
    public void SeedMatchesItsSourceLines(int firstLine, int lastLine, string streamName)
    {
        var limbs = SourceLimbs.Limbs(firstLine, lastLine);
        var expected = Big128.Pack(limbs);
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = SelectStream(OriginalSeeds.Streams, streamName);
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Fact]
    public void MultiplierMatchesSourceLines1649To1650AndStream5()
    {
        var limbs = SourceLimbs.Limbs(MultiplierFirst, MultiplierLast);
        var expected = Big128.Pack(limbs);
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var stream5 = OriginalSeeds.Streams.S5;
        Assert.Equal(expectedLow, stream5.Low);
        Assert.Equal(expectedHigh, stream5.High);
    }

    [Fact]
    public void Streams6To1EqualPowersOfTheMultiplierExceptStream3sTransposedLimb()
    {
        var multiplier = Big128.Pack(SourceLimbs.Limbs(MultiplierFirst, MultiplierLast));
        var streams = OriginalSeeds.Streams;

        AssertEqualsPower(streams.S6, multiplier, 0);
        AssertEqualsPower(streams.S5, multiplier, 1);
        AssertEqualsPower(streams.S4, multiplier, 2);
        AssertEqualsPower(streams.S2, multiplier, 4);
        AssertEqualsPower(streams.S1, multiplier, 5);

        // Stream 3 differs from a^3 in limb 8 only (3784 in the source, 3748 in a^3; AGENTS.md §12).
        var cube = Big128.ModPow(multiplier, 3);
        var (cubeLow, cubeHigh) = Big128.Split(cube);
        var cubeState = new Mcg128State { Low = cubeLow, High = cubeHigh };

        for (var limb = 0; limb < 10; limb++)
        {
            var expected = LimbOf(cubeState, limb);
            var actual = LimbOf(streams.S3, limb);
            if (limb == 8)
            {
                Assert.Equal(3748, expected);
                Assert.Equal(3784, actual);
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }
    }

    private static void AssertEqualsPower(Mcg128State stream, BigInteger multiplier, int power)
    {
        var expected = Big128.ModPow(multiplier, power);
        var (expectedLow, expectedHigh) = Big128.Split(expected);
        Assert.Equal(expectedLow, stream.Low);
        Assert.Equal(expectedHigh, stream.High);
    }

    private static int LimbOf(Mcg128State state, int index)
    {
        var value = Big128.Combine(state);
        var shift = 13 * index;
        var mask = index == 9 ? (BigInteger.One << 11) - 1 : (BigInteger.One << 13) - 1;
        return (int)((value >> shift) & mask);
    }

    /// <summary>Selects a named field of <paramref name="streams"/>, so xunit's <c>InlineData</c> can name a stream by a plain string.</summary>
    internal static Mcg128State SelectStream(StreamSet streams, string streamName) => streamName switch
    {
        "S1" => streams.S1,
        "S2" => streams.S2,
        "S3" => streams.S3,
        "S4" => streams.S4,
        "S5" => streams.S5,
        "S6" => streams.S6,
        _ => throw new ArgumentOutOfRangeException(nameof(streamName), streamName, "unknown stream"),
    };
}
