using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// L0 of the BOOT.md table, Independent layout: the tabulated constants against the SHA-256 rule of BOOT.md
/// ("Independent layout"), recomputed here with <see cref="SHA256"/> rather than copied from
/// <c>IndependentSeeds</c>'s literals, and the two properties that rule is chosen for (BOOT.md, "Orbits"):
/// the six low-28-bit classes are distinct, and every pairwise ratio stays far from a small integer.
/// </summary>
public class IndependentSeedsTests
{
    private const int HashBits = 100;
    private const int MinimumRatioHighBit = 90;

    public static IEnumerable<object[]> Roles()
    {
        yield return [1, "S1"];
        yield return [2, "S2"];
        yield return [3, "S3"];
        yield return [4, "S4"];
        yield return [5, "S5"];
        yield return [6, "S6"];
    }

    [Theory]
    [MemberData(nameof(Roles))]
    public void SeedMatchesTheSha256Rule(int role, string streamName)
    {
        var expected = RoleSeed(role);
        var (expectedLow, expectedHigh) = Big128.Split(expected);

        var actual = SourceLimbsTests.SelectStream(IndependentSeeds.Streams, streamName);
        Assert.Equal(expectedLow, actual.Low);
        Assert.Equal(expectedHigh, actual.High);
    }

    [Fact]
    public void LowTwentyEightBitClassesAreDistinct()
    {
        var lowBitMask = (BigInteger.One << 28) - 1;
        var classes = new HashSet<BigInteger>();

        for (var role = 1; role <= 6; role++)
        {
            var residue = ProductionSeed(role) & lowBitMask;
            Assert.True(classes.Add(residue), $"role {role}: low-28-bit class {residue} repeats another role's.");
        }
    }

    [Fact]
    public void PairwiseRatiosStayFarFromASmallInteger()
    {
        var modulus = Big128.Modulus;

        for (var r = 1; r <= 6; r++)
        {
            var sr = ProductionSeed(r);
            for (var q = 1; q <= 6; q++)
            {
                if (r == q)
                {
                    continue;
                }

                var sq = ProductionSeed(q);
                var ratio = sr * ModularInverse(sq, modulus) % modulus;
                var highBit = HighestSetBit(ratio);

                Assert.True(highBit >= MinimumRatioHighBit,
                    $"ratio s{r}·s{q}⁻¹ mod 2^128 has its highest set bit at {highBit}, expected >= {MinimumRatioHighBit}.");
            }
        }
    }

    /// <summary>
    /// <c>H_role·2²⁸ + (2·role + 1)</c>, <c>H_role</c> the leading <see cref="HashBits"/> bits of
    /// <c>SHA-256("PropStruct.Random.Independent/role")</c> (BOOT.md, "Independent layout"), computed here
    /// independently of <see cref="IndependentSeeds"/>'s tabulated constants.
    /// </summary>
    private static BigInteger RoleSeed(int role)
    {
        var label = $"PropStruct.Random.Independent/{role}";
        var digest = SHA256.HashData(Encoding.ASCII.GetBytes(label));
        var digestValue = new BigInteger(digest, isUnsigned: true, isBigEndian: true);
        var leadingBits = digestValue >> (256 - HashBits);
        return (leadingBits << 28) + (2 * role + 1);
    }

    /// <summary>The tabulated constant <c>IndependentSeeds</c> actually ships for <paramref name="role"/>, as a <see cref="BigInteger"/>.</summary>
    private static BigInteger ProductionSeed(int role)
    {
        var streamName = "S" + role;
        var state = SourceLimbsTests.SelectStream(IndependentSeeds.Streams, streamName);
        return Big128.Combine(state);
    }

    /// <summary>
    /// The modular inverse of <paramref name="value"/> mod <paramref name="modulus"/> by the extended
    /// Euclidean algorithm (there is no <c>BigInteger.ModInverse</c> for a composite modulus in the
    /// framework); every Independent seed is odd, hence a unit mod 2¹²⁸.
    /// </summary>
    private static BigInteger ModularInverse(BigInteger value, BigInteger modulus)
    {
        BigInteger oldR = value, r = modulus;
        BigInteger oldS = 1, s = 0;

        while (r != 0)
        {
            var quotient = oldR / r;
            (oldR, r) = (r, oldR - quotient * r);
            (oldS, s) = (s, oldS - quotient * s);
        }

        Assert.Equal(BigInteger.One, oldR); // value must be a unit mod modulus.
        return (oldS % modulus + modulus) % modulus;
    }

    private static int HighestSetBit(BigInteger value) => (int)value.GetBitLength() - 1;
}
