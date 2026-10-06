using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Closes D3 of the architecture audit of 2026-09-24 ("the tree rounds to binary32 in
/// more than one place ... the emulation has a domain it does not handle"): proves
/// <see cref="Binary32.ToNearestRepresentable"/> and <see cref="Binary32.Multiply"/>
/// (followed by rounding) agree with the hardware cast <c>(double)(float)value</c> bit
/// for bit, over the whole <c>double</c> domain this time, not only the generated spread
/// near the node's own real inputs <see cref="Binary32Tests"/> already covers (fraction
/// bounds, cell sizes, the setup plane, all far inside the normal binary32 range).
///
/// Before this file, <see cref="Binary32.ToNearestRepresentable"/>'s own doc comment
/// disclaimed binary32 subnormals and out-of-range magnitudes as "not handled". Measured
/// here (`## Acceptance criteria` below carries the numbers): the disclaimed domain did
/// diverge, systematically and by a wide margin, everywhere the domain's own doc comment
/// said it might -- and the fix was local (`src/Statistics/Binary32.cs`, the same file,
/// the same private method, no new public member, no `float`), so it was made rather than
/// left declared. The two truncated-quotient helpers are untouched: their own doc
/// comments already state what they model (the x87 FPU's unstored 80-bit intermediate,
/// not a further REAL*4 store), and <see cref="Binary32Tests"/>'s own boundary cases
/// already test them against that contract, not against a hardware rounding oracle that
/// does not apply to them.
/// </summary>
public class Binary32EquivalenceTests
{
    // Bit patterns for exact binary32 magnitudes, read once via BitConverter so no
    // decimal literal stands in for a value whose true binary32 encoding is the point
    // being tested (root BOOT.md, Taboos: "No expected value typed into a test").
    private static readonly double SmallestSubnormalFloat = BitConverter.Int32BitsToSingle(0x0000_0001);
    private static readonly double LargestSubnormalFloat = BitConverter.Int32BitsToSingle(0x007F_FFFF);
    private static readonly double SmallestNormalFloat = BitConverter.Int32BitsToSingle(0x0080_0000); // The normal/subnormal boundary.
    private static readonly double LargestFiniteFloat = BitConverter.Int32BitsToSingle(0x7F7F_FFFF);

    // The exact tie between the largest finite binary32 value (significand 0xFFFFFF,
    // odd) and overflow to infinity (significand 0x1000000, even): significand 16777215
    // is 2^24 - 1, at exponent 127 - 23 = 104, plus the half-ULP that makes it a tie.
    private static readonly double OverflowTie = Math.ScaleB(16777215.5, 104);

    // The exact tie between zero (even) and the smallest binary32 subnormal (odd),
    // 2^-149: half of that step.
    private static readonly double UnderflowTie = Math.ScaleB(1.0, -150);

    private static double NextDoubleTowardPositiveInfinity(double value) =>
        BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) + 1);

    private static double NextDoubleTowardZero(double value) =>
        BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) - 1);

    /// <summary>
    /// A tie inside the binary32 *subnormal* range: <paramref name="lowerStep"/> and
    /// <paramref name="lowerStep"/> + 1 (both counted in units of 2^-149, the subnormal
    /// step) straddle the value exactly, so ties-to-even must pick whichever of the two
    /// is even.
    /// </summary>
    private static double SubnormalTie(int lowerStep) => Math.ScaleB(lowerStep + 0.5, -149);

    /// <summary>
    /// Every edge class the equivalence must hold on, hand-picked rather than random:
    /// zeros of both signs; the smallest and largest binary32 subnormals; the
    /// normal/subnormal boundary; exact ties at three different rounding positions
    /// (subnormal, normal, and the overflow-to-infinity boundary itself), each proven
    /// both ways (an odd kept significand rounds up, an even one stays); the double
    /// adjacent to each tie on either side; the largest finite binary32 value; a value
    /// that overflows to infinity outright; <c>NaN</c> and both infinities; and the
    /// negative mirror of every finite, non-zero case above (root BOOT.md, "No expected
    /// value typed into a test": every expected value below is the oracle
    /// <c>(double)(float)value</c>, computed in the test, not written down here).
    /// </summary>
    public static IEnumerable<object[]> EdgeClasses()
    {
        yield return new object[] { 0.0, "positive zero" };
        yield return new object[] { -0.0, "negative zero" };
        yield return new object[] { double.NaN, "NaN" };
        yield return new object[] { double.PositiveInfinity, "positive infinity" };
        yield return new object[] { double.NegativeInfinity, "negative infinity" };

        var magnitudes = new (double Value, string Name)[]
        {
            (SmallestSubnormalFloat, "smallest subnormal float"),
            (LargestSubnormalFloat, "largest subnormal float"),
            (SmallestNormalFloat, "normal/subnormal boundary (smallest normal float)"),
            (LargestFiniteFloat, "largest finite float"),
            (OverflowTie, "exact tie between the largest finite float and overflow to infinity"),
            (NextDoubleTowardZero(OverflowTie), "one double-ULP below the overflow tie (still the largest finite float)"),
            (NextDoubleTowardPositiveInfinity(OverflowTie), "one double-ULP above the overflow tie (already infinity)"),
            (Math.ScaleB(1.0, 200), "a magnitude that overflows to infinity outright, far past the boundary"),
            (UnderflowTie, "exact tie between zero and the smallest subnormal float"),
            (NextDoubleTowardZero(UnderflowTie), "one double-ULP below the underflow tie (still zero)"),
            (NextDoubleTowardPositiveInfinity(UnderflowTie), "one double-ULP above the underflow tie (already the smallest subnormal)"),
            (SubnormalTie(4), "subnormal tie with an even lower step (4|2^-149): ties-to-even stays down"),
            (SubnormalTie(5), "subnormal tie with an odd lower step (5|2^-149): ties-to-even rounds up"),
            (Math.ScaleB(1.0, -140), "an ordinary value deep in the subnormal range, away from any tie"),
        };

        foreach (var (value, name) in magnitudes)
        {
            yield return new object[] { value, name };
            yield return new object[] { -value, "negative mirror of: " + name };
        }
    }

    [Theory]
    [MemberData(nameof(EdgeClasses))]
    public void ToNearestRepresentableMatchesTheFloatCastOracleOnEveryEdgeClass(double value, string name)
    {
        var expected = (double)(float)value;
        var actual = Binary32.ToNearestRepresentable(value);
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{name}: expected NaN, got {actual:R}");
        }
        else
        {
            Assert.True(
                expected.Equals(actual), // Equals, not ==, so -0.0 and +0.0 are told apart, matching the sign this method promises.
                $"{name}: value={value:R} expected={expected:R} actual={actual:R}");
        }
    }

    /// <summary>
    /// The hand-computed positive control the root taboo asks for ("every check ...
    /// proven ... right once on an input whose answer is known beforehand"), independent
    /// of the <c>(double)(float)value</c> oracle every other test in this file leans on:
    /// the overflow tie rounds to infinity because the largest finite float's own
    /// significand, 2^24 - 1, is odd, and ties-to-even always picks the even alternative
    /// -- here, overflow (a significand of 2^24, conceptually even) -- derived from the
    /// IEEE 754 rounding rule directly, not read off the CLR's own <c>float</c> cast.
    /// </summary>
    [Fact]
    public void OverflowTieRoundsToInfinityByHandDerivedTiesToEvenReasoning()
    {
        Assert.Equal(double.PositiveInfinity, Binary32.ToNearestRepresentable(OverflowTie));
        Assert.Equal(double.NegativeInfinity, Binary32.ToNearestRepresentable(-OverflowTie));
    }

    /// <summary>
    /// The same hand-derived positive control at the opposite end: the underflow tie is
    /// exactly halfway between zero (an even significand, 0) and the smallest subnormal
    /// (an odd significand, 1), so ties-to-even stays at zero -- again derived from the
    /// rounding rule, not the oracle.
    /// </summary>
    [Fact]
    public void UnderflowTieRoundsToZeroByHandDerivedTiesToEvenReasoning()
    {
        Assert.Equal(0.0, Binary32.ToNearestRepresentable(UnderflowTie));
        Assert.Equal(-0.0, Binary32.ToNearestRepresentable(-UnderflowTie));
        Assert.True(double.IsNegative(Binary32.ToNearestRepresentable(-UnderflowTie)));
    }

    /// <summary>
    /// Every double whose own exponent field is 0 (a subnormal double, |value| &lt;
    /// 2^-1022) is many orders below half the smallest binary32 subnormal step and always
    /// rounds to a signed zero -- a class the random sweep below does not reach, since it
    /// only ever draws normal doubles (`## Acceptance criteria`, "the whole double
    /// domain, not only [0, 1)").
    /// </summary>
    [Theory]
    [InlineData(4.9406564584124654e-324)] // The smallest positive subnormal double (bit pattern 1).
    [InlineData(2.2250738585072009e-308)] // The largest subnormal double.
    public void SubnormalDoubleAlwaysUnderflowsToZero(double value)
    {
        Assert.Equal((double)(float)value, Binary32.ToNearestRepresentable(value));
        Assert.Equal((double)(float)-value, Binary32.ToNearestRepresentable(-value));
    }

    /// <summary>
    /// The bulk of the equivalence proof: ten million draws from a seeded
    /// <see cref="SplitMix64"/> (root BOOT.md, Taboos: "no clock-seeded generator"),
    /// spread over the whole finite, normal <c>double</c> exponent range (every biased
    /// exponent from 1 to 2046, not only small magnitudes near [0, 1)) with a random sign
    /// and mantissa, so the sweep reaches ordinary binary32-normal values, the subnormal
    /// range, and the overflow range in roughly the proportion a uniform exponent draw
    /// puts them in. <c>Category=Long</c>: ten million calls to <see cref="Binary32.ToNearestRepresentable"/>
    /// plus a <c>float</c> cast each, well outside the fast suite's budget.
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void ToNearestRepresentableMatchesTheFloatCastOracleOverTenMillionDrawsAcrossTheWholeExponentRange()
    {
        var random = new SplitMix64(20260925);
        const int sampleCount = 10_000_000;
        const ulong exponentFieldMask = 0x7FFUL << 52;

        for (var i = 0; i < sampleCount; i++)
        {
            var rawBits = random.NextUInt64();
            var exponentField = rawBits % 2046 + 1; // 1..2046: every finite, normal double exponent.
            var bits = (rawBits & ~exponentFieldMask) | (exponentField << 52);
            var value = BitConverter.UInt64BitsToDouble(bits);

            var expected = (double)(float)value;
            var actual = Binary32.ToNearestRepresentable(value);
            Assert.True(
                expected.Equals(actual),
                $"draw {i}: value={value:R} bits=0x{bits:X16} expected={expected:R} actual={actual:R}");
        }
    }

    /// <summary>
    /// <see cref="Binary32.Multiply"/> deliberately leaves its product at double
    /// precision, unrounded (its own doc comment; S-2, the audit of 2026-09-24) -- it
    /// models the x87 FPU's own unstored extended-precision intermediate, not a value
    /// that was ever stored to a REAL*4 variable. So the hardware equivalent it "has" is
    /// not the product itself but the product *once rounded*: a true single-precision
    /// multiply, correctly rounded, of the same two operands. Verified here over one
    /// million pairs drawn the same way as the sweep above: doubles carry more than
    /// twice a <c>float</c>'s precision, so multiplying two already-binary32-exact
    /// operands at double precision and rounding once at the end always agrees with
    /// rounding once inside a native single-precision multiply (no double rounding error
    /// is possible at this precision gap) -- the property this test measures, not
    /// assumes. <c>Category=Long</c> for the same reason as the sweep above.
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void MultiplyFollowedByRoundingMatchesTheFloatMultiplyOracleOverOneMillionDraws()
    {
        var random = new SplitMix64(20260925_1);
        const int sampleCount = 1_000_000;
        const ulong exponentFieldMask = 0x7FFUL << 52;

        for (var i = 0; i < sampleCount; i++)
        {
            var a = NextFiniteNormalDouble(random, exponentFieldMask);
            var b = NextFiniteNormalDouble(random, exponentFieldMask);

            var expected = (double)((float)a * (float)b);
            var actual = Binary32.ToNearestRepresentable(Binary32.Multiply(a, b));
            Assert.True(
                expected.Equals(actual),
                $"draw {i}: a={a:R} b={b:R} expected={expected:R} actual={actual:R}");
        }
    }

    private static double NextFiniteNormalDouble(SplitMix64 random, ulong exponentFieldMask)
    {
        var rawBits = random.NextUInt64();
        var exponentField = rawBits % 2046 + 1;
        var bits = (rawBits & ~exponentFieldMask) | (exponentField << 52);
        return BitConverter.UInt64BitsToDouble(bits);
    }

    /// <summary>
    /// <see cref="Binary32.Multiply"/> itself -- before the extra rounding step the test
    /// above adds -- does *not* match a native single-precision multiply in general: that
    /// is the whole point of leaving the product unrounded (S-2). Reuses the boundary
    /// operands <see cref="Binary32Tests.TruncatedQuotientOfProductLeavesTheProductUnroundedAtAnIntegerBoundary"/>
    /// already established are binary32-exact, where the true single-precision product
    /// would round differently from the unrounded double product this method returns.
    /// </summary>
    [Fact]
    public void MultiplyItselfDoesNotMatchTheFloatMultiplyOracleAtTheS2Boundary()
    {
        const double ddokmax = 7.669682236155495e-05;
        const double ak4 = 13.121566772460938;

        var unrounded = Binary32.Multiply(ddokmax, ak4);
        var trueSingleProduct = (double)((float)ddokmax * (float)ak4);

        Assert.NotEqual(trueSingleProduct, unrounded);
        Assert.Equal(trueSingleProduct, Binary32.ToNearestRepresentable(unrounded));
    }
}
