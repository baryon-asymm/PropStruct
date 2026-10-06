using System.Numerics;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// L0 of the BOOT.md table: <c>Multiply</c>, <c>Advance</c>, <c>A40</c>, <c>A80</c> and <c>ForParticle</c>
/// against <see cref="BigInteger"/> modular arithmetic on generated operands. Operands come from a fixed-seed
/// <see cref="SplitMix64"/> (test-only, not the port's own generator), so a failure is reproducible. Theory
/// parameters are raw <see cref="ulong"/> pairs, never the internal <c>Mcg128State</c>: xunit only discovers
/// public test methods, and a public method may not expose an internal type in its signature.
/// </summary>
public class ArithmeticTests
{
    private const int GeneratedCaseCount = 200;

    private static readonly BigInteger Multiplier = Big128.Pack(SourceLimbs.Limbs(1649, 1650));

    public static IEnumerable<object[]> GeneratedOperandPairs()
    {
        var random = new SplitMix64(2026_09_17);
        for (var i = 0; i < GeneratedCaseCount; i++)
        {
            var (xLow, xHigh) = RandomUInt64Pair(random);
            var (yLow, yHigh) = RandomUInt64Pair(random);
            yield return [xLow, xHigh, yLow, yHigh];
        }
    }

    public static IEnumerable<object[]> GeneratedExponents()
    {
        var random = new SplitMix64(2026_09_18);
        for (var i = 0; i < GeneratedCaseCount; i++)
        {
            var (stateLow, stateHigh) = RandomUInt64Pair(random);
            var (kLow, kHigh) = RandomUInt64Pair(random);
            yield return [stateLow, stateHigh, kLow, kHigh];
        }

        // The boundary values named by the acceptance criterion.
        var (boundaryStateLow, boundaryStateHigh) = RandomUInt64Pair(new SplitMix64(1));
        yield return [boundaryStateLow, boundaryStateHigh, 0UL, 0UL];

        var (boundaryStateLow2, boundaryStateHigh2) = RandomUInt64Pair(new SplitMix64(2));
        yield return [boundaryStateLow2, boundaryStateHigh2, ulong.MaxValue, ulong.MaxValue];
    }

    [Theory]
    [MemberData(nameof(GeneratedOperandPairs))]
    public void MultiplyMatchesBigIntegerModularMultiplication(ulong xLow, ulong xHigh, ulong yLow, ulong yHigh)
    {
        var x = new Mcg128State { Low = xLow, High = xHigh };
        var y = new Mcg128State { Low = yLow, High = yHigh };

        var expected = Big128.Combine(xLow, xHigh) * Big128.Combine(yLow, yHigh) % Big128.Modulus;
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = Mcg128.Multiply(x, y);
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Theory]
    [MemberData(nameof(GeneratedExponents))]
    public void AdvanceMatchesTheBigIntegerModularPower(ulong stateLow, ulong stateHigh, ulong kLow, ulong kHigh)
    {
        var state = new Mcg128State { Low = stateLow, High = stateHigh };

        var factor = Big128.ModPow(Multiplier, Big128.Combine(kLow, kHigh));
        var expected = Big128.Combine(stateLow, stateHigh) * factor % Big128.Modulus;
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = Mcg128.Advance(state, kLow, kHigh);
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Fact]
    public void A40EqualsTheBigIntegerModularPower()
    {
        var expected = Big128.ModPow(Multiplier, BigInteger.One << 40);
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = Mcg128.A40;
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Fact]
    public void A80EqualsTheBigIntegerModularPower()
    {
        var expected = Big128.ModPow(Multiplier, BigInteger.One << 80);
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = Mcg128.A80;
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Fact]
    public void A38EqualsTheBigIntegerModularPower()
    {
        var expected = Big128.ModPow(Multiplier, BigInteger.One << 38);
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = Mcg128.A38;
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Fact]
    public void ForParticleOfSeedZeroOrdinalZeroEqualsTheOriginalSeeds()
    {
        var expected = OriginalSeeds.Streams;
        var actual = OriginalSeeds.ForParticle(0, 0);

        AssertEqual(expected.S1, actual.S1);
        AssertEqual(expected.S2, actual.S2);
        AssertEqual(expected.S3, actual.S3);
        AssertEqual(expected.S4, actual.S4);
        AssertEqual(expected.S5, actual.S5);
        AssertEqual(expected.S6, actual.S6);
    }

    [Theory]
    [InlineData(0UL, 0UL)]
    [InlineData(1UL, 1UL)]
    [InlineData((1UL << 20) - 1, 7UL)]
    [InlineData(11UL, (1UL << 40) - 1)]
    [InlineData((1UL << 20) - 1, (1UL << 40) - 1)]
    public void ForParticleEqualsTheBigIntegerJump(ulong seed, ulong ordinal)
    {
        var steps = seed * (BigInteger.One << 80) + ordinal * (BigInteger.One << 40);
        var factor = Big128.ModPow(Multiplier, steps);
        var original = OriginalSeeds.Streams;

        var actual = OriginalSeeds.ForParticle(seed, ordinal);

        AssertEqualsJump(original.S1, factor, actual.S1);
        AssertEqualsJump(original.S2, factor, actual.S2);
        AssertEqualsJump(original.S3, factor, actual.S3);
        AssertEqualsJump(original.S4, factor, actual.S4);
        AssertEqualsJump(original.S5, factor, actual.S5);
        AssertEqualsJump(original.S6, factor, actual.S6);
    }

    [Theory]
    [InlineData(0UL, 0UL)]
    [InlineData(1UL, 1UL)]
    [InlineData((1UL << 20) - 1, 7UL)]
    [InlineData(11UL, (1UL << 40) - 1)]
    [InlineData((1UL << 20) - 1, (1UL << 40) - 1)]
    public void ForBatchedParticleEqualsTheBigIntegerJumpWithTheRoleGroupOffset(ulong seed, ulong ordinal)
    {
        // BOOT.md, "Batched derivation": role r is jumped by seed·2⁸⁰ + ordinal·2⁴⁰ + g(r)·2³⁸,
        // g = 0 for S1/S2, 1 for S3/S4/S5, 2 for S6.
        var baseSteps = seed * (BigInteger.One << 80) + ordinal * (BigInteger.One << 40);
        var group38 = BigInteger.One << 38;
        var original = OriginalSeeds.Streams;

        var actual = OriginalSeeds.ForBatchedParticle(seed, ordinal);

        AssertEqualsJump(original.S1, Big128.ModPow(Multiplier, baseSteps + 0 * group38), actual.S1);
        AssertEqualsJump(original.S2, Big128.ModPow(Multiplier, baseSteps + 0 * group38), actual.S2);
        AssertEqualsJump(original.S3, Big128.ModPow(Multiplier, baseSteps + 1 * group38), actual.S3);
        AssertEqualsJump(original.S4, Big128.ModPow(Multiplier, baseSteps + 1 * group38), actual.S4);
        AssertEqualsJump(original.S5, Big128.ModPow(Multiplier, baseSteps + 1 * group38), actual.S5);
        AssertEqualsJump(original.S6, Big128.ModPow(Multiplier, baseSteps + 2 * group38), actual.S6);
    }

    [Fact]
    public void IndependentForParticleOfSeedZeroOrdinalZeroEqualsIndependentSeeds()
    {
        var expected = IndependentSeeds.Streams;
        var actual = IndependentSeeds.ForParticle(0, 0);

        AssertEqual(expected.S1, actual.S1);
        AssertEqual(expected.S2, actual.S2);
        AssertEqual(expected.S3, actual.S3);
        AssertEqual(expected.S4, actual.S4);
        AssertEqual(expected.S5, actual.S5);
        AssertEqual(expected.S6, actual.S6);
    }

    [Theory]
    [InlineData(0UL, 0UL)]
    [InlineData(1UL, 1UL)]
    [InlineData((1UL << 20) - 1, 7UL)]
    [InlineData(11UL, (1UL << 40) - 1)]
    [InlineData((1UL << 20) - 1, (1UL << 40) - 1)]
    public void IndependentForParticleEqualsTheBigIntegerJump(ulong seed, ulong ordinal)
    {
        var steps = seed * (BigInteger.One << 80) + ordinal * (BigInteger.One << 40);
        var factor = Big128.ModPow(Multiplier, steps);
        var independent = IndependentSeeds.Streams;

        var actual = IndependentSeeds.ForParticle(seed, ordinal);

        AssertEqualsJump(independent.S1, factor, actual.S1);
        AssertEqualsJump(independent.S2, factor, actual.S2);
        AssertEqualsJump(independent.S3, factor, actual.S3);
        AssertEqualsJump(independent.S4, factor, actual.S4);
        AssertEqualsJump(independent.S5, factor, actual.S5);
        AssertEqualsJump(independent.S6, factor, actual.S6);
    }

    private static void AssertEqualsJump(Mcg128State original, BigInteger factor, Mcg128State actual)
    {
        var expected = Big128.Combine(original) * factor % Big128.Modulus;
        var (expectedLow, expectedHigh) = Big128.Split(expected);
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    private static void AssertEqual(Mcg128State expected, Mcg128State actual)
    {
        Assert.Equal(expected.Low, actual.Low);
        Assert.Equal(expected.High, actual.High);
    }

    private static (ulong Low, ulong High) RandomUInt64Pair(SplitMix64 random)
    {
        Span<byte> bytes = stackalloc byte[16];
        random.NextBytes(bytes);
        var low = BitConverter.ToUInt64(bytes[..8]);
        var high = BitConverter.ToUInt64(bytes[8..]);
        return (low, high);
    }
}
