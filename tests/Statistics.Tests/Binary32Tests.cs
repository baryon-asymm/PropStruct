using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L0/L1 support: <c>Binary32.ToNearestRepresentable</c> against an independent oracle
/// on a generated set of operands. The oracle is <c>(double)(float)value</c> — the one
/// place in this whole node's test suite where the <c>float</c> CLR type is allowed to
/// appear, precisely because it is not exercising <see cref="Binary32"/>'s own
/// production code path, only checking it from the outside (the same role
/// <c>System.Numerics.BigInteger</c> plays as an oracle for <c>tests/Random.Tests</c>'
/// generator, a genuinely independent implementation rather than a shortcut).
/// </summary>
public class Binary32Tests
{
    /// <summary>
    /// A generated spread of magnitudes and mantissas (a seeded <see cref="SplitMix64"/>
    /// over the raw IEEE-754 bit pattern, not a handpicked list of literals: found by
    /// the audit of this node, which read the previous version as typed rather than
    /// generated, contradicting this node's own BOOT.md), plus two operands built to be
    /// an exact tie at bit 29 -- the boundary between the 23 mantissa bits binary32
    /// keeps and the 29 it drops, where <see cref="Binary32.ToNearestRepresentable"/>'s
    /// ties-to-even branch is the only way to decide which way to round. Without a tie
    /// case, every generated operand's dropped bits land off-centre and only the
    /// ordinary round-half-away branch ever runs.
    /// </summary>
    public static IEnumerable<object[]> GeneratedOperands()
    {
        var random = new SplitMix64(20260918); // Seeded: reproducible across runs, never wall-time (root BOOT.md, Taboos).
        for (var i = 0; i < 32; i++)
        {
            var sign = random.Next(2) == 0 ? 1L : 0L;
            var exponent = random.Next(-40, 41) + 1023; // Away from the subnormal (0) and infinite/NaN (2047) exponents.
            var mantissa = random.NextInt64(0, 1L << 52);
            var bits = (sign << 63) | ((long)exponent << 52) | mantissa;
            yield return new object[] { BitConverter.Int64BitsToDouble(bits) };
        }

        yield return new object[] { 0.0 };
        yield return new object[] { BuildBinary32Tie(exponent: 3, kept23: 0b101_0101_0101_0101_0101_010, oddLsb: true) };
        yield return new object[] { BuildBinary32Tie(exponent: 3, kept23: 0b101_0101_0101_0101_0101_010, oddLsb: false) };
    }

    /// <summary>
    /// A positive double whose 52-bit mantissa is <paramref name="kept23"/> (its own
    /// low bit forced to <paramref name="oddLsb"/>) followed by exactly <c>1000...0</c>
    /// (29 bits: a leading 1, then 28 zeros) -- the dropped half of the ULP at the kept
    /// 23rd bit, an exact tie for round-to-nearest-even. <paramref name="oddLsb"/> picks
    /// which side of the tie ties-to-even rounds to: <see langword="true"/> rounds up
    /// (odd kept mantissa becomes even), <see langword="false"/> stays down (already
    /// even).
    /// </summary>
    private static double BuildBinary32Tie(int exponent, long kept23, bool oddLsb)
    {
        kept23 &= 0x7F_FFFF;
        kept23 = oddLsb ? kept23 | 1 : kept23 & ~1L;
        var mantissa52 = (kept23 << 29) | (1L << 28);
        var bits = ((long)(exponent + 1023) << 52) | mantissa52;
        return BitConverter.Int64BitsToDouble(bits);
    }

    [Theory]
    [MemberData(nameof(GeneratedOperands))]
    public void ToNearestRepresentableMatchesTheFloatCastOracle(double value)
    {
        var expected = (double)(float)value;
        Assert.Equal(expected, Binary32.ToNearestRepresentable(value));
    }

    /// <summary>
    /// <c>TruncatedQuotient</c> against exact rational truncation for operands that
    /// are themselves already binary32-exact (so the oracle's own division needs no
    /// further rounding): 700/10 = 70 exactly, matching both the oracle and the port.
    /// </summary>
    [Theory]
    [InlineData(700.0, 10.0, 70)]
    [InlineData(9.0, 3.0, 3)]
    [InlineData(1.0, 3.0, 0)]
    [InlineData(0.0, 5.0, 0)]
    public void TruncatedQuotientMatchesExactRationalTruncationForBinary32ExactOperands(double numerator, double denominator, long expected)
    {
        Assert.Equal(numerator, (double)(float)numerator);
        Assert.Equal(denominator, (double)(float)denominator);
        Assert.Equal(expected, Binary32.TruncatedQuotient(numerator, denominator));
    }

    /// <summary>
    /// The double-rounding case at the heart of the C166 evidence (root BOOT.md,
    /// "Invariants"): dividing the two binary32-rounded operands at double precision
    /// and truncating directly gives 69, matching the archive; rounding that quotient
    /// to binary32 first (proven wrong by <c>SetupTests.SizesNdokOfC166WouldBeSeventyTwoWithoutBinary32Rounding</c>'s
    /// sibling check) would give 70.
    /// </summary>
    [Fact]
    public void TruncatedQuotientAvoidsDoubleRoundingAtAnIntegerBoundary()
    {
        var numerator = Binary32.Multiply(700.0, 1e-6);
        var denominator = Binary32.Multiply(10.0, 1e-6);

        Assert.Equal(69, Binary32.TruncatedQuotient(numerator, denominator));
    }

    /// <summary>
    /// S-2 (audit of 2026-09-24): <c>TruncatedQuotientOfProduct</c>'s own product
    /// argument is <c>Nkarm</c>/<c>Ncat</c>'s own <c>Ddokmax*AK4</c>, never stored to a
    /// variable of its own in the original (<see cref="Binary32.Multiply"/>'s own doc
    /// comment), unlike <c>Ndok</c>'s own genuinely-stored <c>Ddokmax</c>
    /// (<see cref="TruncatedQuotientAvoidsDoubleRoundingAtAnIntegerBoundary"/>'s own
    /// case). All three operands are already binary32-exact by construction, built so
    /// the unrounded product's own quotient sits just under an integer boundary
    /// (1505.9999...) while rounding the product first -- what
    /// <see cref="Binary32.TruncatedQuotient"/> used to be called with here, before this
    /// method existed -- pushes it just over (1506.0000...): 1505 is the reading
    /// <see cref="Setup.Sizes"/>'s own <c>Nkarm</c>/<c>Ncat</c> now use, 1506 the old,
    /// double-rounded one.
    /// </summary>
    [Fact]
    public void TruncatedQuotientOfProductLeavesTheProductUnroundedAtAnIntegerBoundary()
    {
        const double ddokmax = 7.669682236155495e-05;
        const double ak4 = 13.121566772460938;
        const double di = 6.682486741738103e-07;
        Assert.Equal(ddokmax, (double)(float)ddokmax);
        Assert.Equal(ak4, (double)(float)ak4);
        Assert.Equal(di, (double)(float)di);

        var product = Binary32.Multiply(ddokmax, ak4);

        Assert.Equal(1505, Binary32.TruncatedQuotientOfProduct(product, di));
        Assert.Equal(1506, Binary32.TruncatedQuotient(product, di)); // The old, double-rounded reading (BOOT.md, S-2).
    }
}
