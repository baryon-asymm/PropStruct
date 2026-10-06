using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// <see cref="StreamSeeds.ForParticle"/> dispatches to <see cref="OriginalSeeds.ForParticle"/> or
/// <see cref="IndependentSeeds.ForParticle"/> by <see cref="StreamLayout"/>, with no formula of its own
/// (BOOT.md, "Independent layout").
/// </summary>
public class StreamSeedsTests
{
    [Fact]
    public void OriginalLayoutMatchesOriginalSeeds()
    {
        var expected = OriginalSeeds.ForParticle(7, 42);
        var actual = StreamSeeds.ForParticle(StreamLayout.Original, 7, 42);

        AssertEqual(expected, actual);
    }

    [Fact]
    public void IndependentLayoutMatchesIndependentSeeds()
    {
        var expected = IndependentSeeds.ForParticle(7, 42);
        var actual = StreamSeeds.ForParticle(StreamLayout.Independent, 7, 42);

        AssertEqual(expected, actual);
    }

    [Fact]
    public void ForBatchedParticleOriginalLayoutMatchesOriginalSeedsForBatchedParticle()
    {
        var expected = OriginalSeeds.ForBatchedParticle(7, 42);
        var actual = StreamSeeds.ForBatchedParticle(StreamLayout.Original, 7, 42);

        AssertEqual(expected, actual);
    }

    [Fact]
    public void ForBatchedParticleIndependentLayoutMatchesIndependentSeedsForParticle()
    {
        var expected = IndependentSeeds.ForParticle(7, 42);
        var actual = StreamSeeds.ForBatchedParticle(StreamLayout.Independent, 7, 42);

        AssertEqual(expected, actual);
    }

    private static void AssertEqual(StreamSet expected, StreamSet actual)
    {
        AssertEqual(expected.S1, actual.S1);
        AssertEqual(expected.S2, actual.S2);
        AssertEqual(expected.S3, actual.S3);
        AssertEqual(expected.S4, actual.S4);
        AssertEqual(expected.S5, actual.S5);
        AssertEqual(expected.S6, actual.S6);
    }

    private static void AssertEqual(Mcg128State expected, Mcg128State actual)
    {
        Assert.Equal(expected.Low, actual.Low);
        Assert.Equal(expected.High, actual.High);
    }
}
