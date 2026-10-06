namespace PropStruct.Statistics;

/// <summary>
/// The original's REAL*4 rounding and truncated division, for the one exception to
/// "double precision only" (root BOOT.md, "Invariants": the array sizes <c>Ndok</c>,
/// <c>Nkarm</c>, <c>Ncat</c>, "computed with integer mantissa arithmetic, no float
/// type"). This file never declares a <c>float</c> value: <see cref="ToNearestRepresentable"/>
/// extracts the sign, exponent and 52-bit mantissa of a <c>double</c>'s own IEEE 754 bit
/// pattern (<see cref="BitConverter.DoubleToInt64Bits(double)"/>), rounds the mantissa
/// down to the 23 bits a binary32 significand keeps (round to nearest, ties to even,
/// the same rule <c>(float)value</c> would apply), and rebuilds a <c>double</c> bit
/// pattern from the rounded result — bit for bit the value <c>(double)(float)value</c>
/// would produce, without ever holding a <c>float</c>.
/// </summary>
internal static class Binary32
{
    private const int DoubleMantissaBits = 52;
    private const int SingleMantissaBits = 23;
    private const int MantissaShift = DoubleMantissaBits - SingleMantissaBits; // 29
    private const int DoubleExponentBias = 1023;
    private const long DoubleMantissaMask = (1L << DoubleMantissaBits) - 1;
    private const long SingleMantissaMask = (1L << SingleMantissaBits) - 1;
    private const long ImplicitLeadingBit = 1L << DoubleMantissaBits;
    private const long MantissaOverflow = 1L << (SingleMantissaBits + 1);
    private const int MinNormalExponent = -126; // Smallest unbiased exponent of a normal binary32 value.
    private const int MaxNormalExponent = 127; // Largest unbiased exponent of a normal binary32 value.

    // 2^-149, the binary32 subnormal step, built from its own IEEE 754 double bit
    // pattern (exponent -149 + the double bias, zero mantissa) rather than from
    // Math.Pow, so the scale factor used below is exact by construction, not by an
    // unspecified library rounding guarantee.
    private static readonly double SubnormalStep = BitConverter.Int64BitsToDouble((long)(-149 + DoubleExponentBias) << DoubleMantissaBits);

    // The smallest binary32 subnormal is 2^-149; a double whose own exponent field is 0
    // (|value| < 2^-1022) is many orders below half that step and always rounds to a
    // signed zero, so it is handled before the general 53-bit significand is built.
    // Past a shift of 53 off that significand (exponent <= -150, i.e. at or below the
    // subnormal/zero tie) the kept mantissa is provably always 0 with the remainder
    // always at most half the dropped span (`ACCEPTANCE.md`, "the boundary
    // shift-of-53 argument"), so the general rounding loop is skipped there too.
    private const int ZeroShift = MantissaShift + SingleMantissaBits + 2; // 54

    /// <summary>
    /// Rounds <paramref name="value"/> to the nearest value representable in IEEE 754
    /// binary32 (round to nearest, ties to even), widened back to a <c>double</c>, bit
    /// for bit the value <c>(double)(float)value</c> would produce — over the whole
    /// <c>double</c> domain: zero, subnormal and normal binary32 magnitudes, saturation
    /// to infinity past the largest finite binary32 value, and <c>NaN</c>/infinity
    /// pass-through. No case is left unhandled (`ACCEPTANCE.md`,
    /// "the binary32 rounding is total").
    /// </summary>
    public static double ToNearestRepresentable(double value)
    {
        if (value == 0.0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            return value;
        }

        var bits = BitConverter.DoubleToInt64Bits(value);
        var negative = bits < 0;
        var magnitudeBits = bits & long.MaxValue;

        var doubleExponentField = (magnitudeBits >> DoubleMantissaBits) & 0x7FF;
        if (doubleExponentField == 0)
        {
            return negative ? -0.0 : 0.0;
        }

        var mantissa = magnitudeBits & DoubleMantissaMask;
        var exponent = (int)doubleExponentField - DoubleExponentBias;

        // 53-bit significand (implicit leading 1 + the 52 explicit bits): value equals
        // significand * 2^(exponent - 52).
        var significand = mantissa | ImplicitLeadingBit;

        // Normal result: keep the top 24 bits (implicit 1 + 23 explicit), a 29-bit shift.
        // Subnormal result (exponent below the smallest normal one): the stored exponent
        // is pinned at MinNormalExponent while the true exponent keeps falling, so more
        // of the significand's low end is dropped, one extra bit per exponent step.
        var shift = exponent < MinNormalExponent ? MantissaShift + (MinNormalExponent - exponent) : MantissaShift;

        if (shift >= ZeroShift)
        {
            // The 53-bit significand is at most 2^53 - 1; past this shift the kept
            // mantissa is 0 and the remainder never reaches the halfway point, so the
            // result is a signed zero regardless of the exact (and otherwise very large)
            // shift amount -- computing it directly would risk an undefined 64-bit shift.
            return negative ? -0.0 : 0.0;
        }

        var half = 1L << (shift - 1);
        var remainder = significand & ((half << 1) - 1);
        var keep = significand >> shift;

        if (remainder > half || remainder == half && (keep & 1L) != 0)
        {
            keep += 1;
        }

        if (shift != MantissaShift)
        {
            // Subnormal-result path: keep has at most 23 bits and no implicit leading 1.
            if (keep == 0)
            {
                return negative ? -0.0 : 0.0;
            }

            if (keep < 1L << SingleMantissaBits)
            {
                var subnormal = keep * SubnormalStep; // Exact: an integer below 2^23 times a power of two.
                return negative ? -subnormal : subnormal;
            }

            // keep == 1 << SingleMantissaBits: rounded up into the smallest normal value.
            exponent = MinNormalExponent;
        }
        else if (keep == MantissaOverflow)
        {
            keep >>= 1;
            exponent += 1;
        }

        if (exponent > MaxNormalExponent)
        {
            return negative ? double.NegativeInfinity : double.PositiveInfinity;
        }

        var singleMantissa = keep & SingleMantissaMask;
        var doubleMantissa = singleMantissa << MantissaShift;
        var doubleExponentBits = (long)(exponent + DoubleExponentBias) << DoubleMantissaBits;
        var resultBits = doubleExponentBits | doubleMantissa;
        if (negative)
        {
            resultBits |= long.MinValue;
        }

        return BitConverter.Int64BitsToDouble(resultBits);
    }

    /// <summary>
    /// The original's REAL*4 product of two already-binary32 values (BOOT.md, "Array
    /// sizes from binary32 values"): rounds each operand to binary32, then multiplies
    /// in double precision and leaves the product there. The x87 FPU the original
    /// linked against keeps a compound expression like <c>Ddokmax*AK4/Di</c> in its
    /// 80-bit extended registers between operations and rounds to REAL*4 only when a
    /// result is stored to a named variable; <c>Ddokmax*AK4</c> is never stored to one
    /// (BOOT.md's own "exact quotients of those binary32 values", not of a further
    /// binary32-rounded product) — evidence below.
    /// </summary>
    public static double Multiply(double a, double b) => ToNearestRepresentable(a) * ToNearestRepresentable(b);

    /// <summary>
    /// <c>int(numerator/denominator)</c> the way the original's REAL*4 division and
    /// truncation toward zero produce it: both operands rounded to binary32 (they are
    /// genuine REAL*4 Fortran variables, so they were stored, hence rounded), their
    /// quotient left at double precision (standing in for the x87 FPU's own 80-bit
    /// extended register, never rounded down to REAL*4 before <c>INT()</c> truncates
    /// it) and truncated toward zero. Rounding the quotient itself to binary32 before
    /// truncating — the port's first attempt — gives <c>Ndok = 72</c> for C166 instead
    /// of the archived 71 (caught red by <c>tests/Statistics.Tests</c>' setup test
    /// before this fix): C166's <c>Ddokmax/Di</c> is <c>699.999975.../9.999999...</c>
    /// = <c>69.99999927...</c> at double precision (just under the integer boundary,
    /// truncating to 69, +2 = 71) but rounds up to exactly <c>70.0f</c> at binary32
    /// precision (truncating to 70, +2 = 72) — the double-rounding this method now
    /// avoids. <paramref name="numerator"/> and <paramref name="denominator"/> are
    /// expected non-negative, as every size this node truncates is.
    /// </summary>
    public static long TruncatedQuotient(double numerator, double denominator)
    {
        var n = ToNearestRepresentable(numerator);
        var d = ToNearestRepresentable(denominator);
        return (long)(n / d);
    }

    /// <summary>
    /// <c>int(product/denominator)</c> where <paramref name="product"/> is already the
    /// original's own unstored x87 intermediate -- <see cref="Multiply"/>'s own product
    /// of two binary32-rounded operands, left at double precision and never itself
    /// rounded to binary32 (this method's own doc comment, and BOOT.md, "Array sizes
    /// from binary32 values": <c>Nkarm</c>/<c>Ncat</c>'s own <c>Ddokmax*AK4</c>, which
    /// the Fortran source, <c>int(Ddokmax*AK4/Di)+2</c>, never assigns to a named REAL*4
    /// variable of its own). Rounding <paramref name="product"/> here, the way
    /// <see cref="TruncatedQuotient"/> rounds its own numerator, double-rounds it: found
    /// as S-2 by the audit of 2026-09-24, since <see cref="Setup.Sizes"/> called
    /// <see cref="TruncatedQuotient"/> for <c>Nkarm</c>/<c>Ncat</c> too, silently
    /// contradicting this file's own <see cref="Multiply"/> doc comment and BOOT.md's
    /// "exact quotients of those binary32 values". Only <paramref name="denominator"/>
    /// -- <c>Di</c>/<c>Dj</c>, genuine REAL*4 Fortran variables, already stored and
    /// hence already binary32-exact by the time <see cref="Setup.Sizes"/> calls this --
    /// is rounded before the quotient is taken and truncated toward zero.
    /// </summary>
    public static long TruncatedQuotientOfProduct(double product, double denominator)
    {
        var d = ToNearestRepresentable(denominator);
        return (long)(product / d);
    }
}
