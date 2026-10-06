namespace PropStruct.Random;

/// <summary>
/// The original's GSV=2 generator: a multiplicative congruential generator modulo 2¹²⁸ (Fortran subroutine
/// <c>random2</c>, source lines 1642-1691). Kernel-compatible C#: static methods, 128-bit arithmetic on pairs
/// of <see cref="ulong"/>, no allocation, no exceptions, no recursion (BOOT.md, Constraints).
/// </summary>
internal static class Mcg128
{
    // The multiplier, packed little-endian in base 2^13 from the limbs of the Fortran DATA statement,
    // lines 1649-1650: m0=1, m1=0, m2=7916, m3=6769, m4=8113, m5=7234, m6=4142, m7=5015, m8=3567, m9=1526.
    private const ulong MultiplierLow = 0xfb1d38fbb0000001UL;
    private const ulong MultiplierHigh = 0xbecdef9cbc0bb885UL;

    // a^(2^40) mod 2^128, tabulated for OriginalSeeds.ForParticle's per-particle jump (BOOT.md, "Jump-ahead").
    private const ulong A40Low = 0x0000000000000001UL;
    private const ulong A40High = 0xb13e4cae9d38fbb0UL;

    // a^(2^80) mod 2^128, tabulated for OriginalSeeds.ForParticle's per-particle jump (BOOT.md, "Jump-ahead").
    private const ulong A80Low = 0x0000000000000001UL;
    private const ulong A80High = 0x38fbb00000000000UL;

    // a^(2^38) mod 2^128, tabulated for OriginalSeeds.ForBatchedParticle's role-group offset
    // (BOOT.md, "Batched derivation"): g(r)·2³⁸ steps, g = 0..3, computed as A38^g by g successive
    // multiplications by A38 in ForBatchedParticle itself, not as further tabulated powers.
    private const ulong A38Low = 0x0000000000000001UL;
    private const ulong A38High = 0xec4f932ba74e3eecUL;

    // x_i = 2^(13i - 128), i = 0..9: the weights of the Fortran DATA statement, source lines 1651-1660,
    // exact powers of two (BOOT.md, "Output"). Each literal is the double nearest 2^(13i-128), which is
    // 2^(13i-128) itself since it is exactly representable.
    private const double X0 = 2.938735877055719e-39;  // 2^-128
    private const double X1 = 2.407412430484045e-35;  // 2^-115
    private const double X2 = 1.9721522630525295e-31; // 2^-102
    private const double X3 = 1.6155871338926322e-27; // 2^-89
    private const double X4 = 1.3234889800848443e-23; // 2^-76
    private const double X5 = 1.0842021724855044e-19; // 2^-63
    private const double X6 = 8.881784197001252e-16;  // 2^-50
    private const double X7 = 7.275957614183426e-12;  // 2^-37
    private const double X8 = 5.960464477539063e-08;  // 2^-24
    private const double X9 = 0.00048828125;          // 2^-11

    /// <summary>a^(2⁴⁰) mod 2¹²⁸.</summary>
    public static Mcg128State A40 => new() { Low = A40Low, High = A40High };

    /// <summary>a^(2⁸⁰) mod 2¹²⁸.</summary>
    public static Mcg128State A80 => new() { Low = A80Low, High = A80High };

    /// <summary>a^(2³⁸) mod 2¹²⁸, the batched role-group offset unit (BOOT.md, "Batched derivation").</summary>
    public static Mcg128State A38 => new() { Low = A38Low, High = A38High };

    /// <summary>
    /// Steps <paramref name="state"/> once (<c>s = a·s mod 2¹²⁸</c>) and returns the original's ten-term sum
    /// of the new state's limbs (Fortran lines 1661-1690, evaluated in <see cref="double"/> left to right).
    /// No call can fail.
    /// </summary>
    public static double Next(ref Mcg128State state)
    {
        state = Multiply(state, new Mcg128State { Low = MultiplierLow, High = MultiplierHigh });
        return TenTermSum(state);
    }

    /// <summary>
    /// <paramref name="state"/> advanced by <paramref name="kHigh"/>·2⁶⁴ + <paramref name="kLow"/> steps:
    /// <c>state · a^k mod 2¹²⁸</c>, computed by square-and-multiply over the 128 bits of k.
    /// </summary>
    public static Mcg128State Advance(Mcg128State state, ulong kLow, ulong kHigh)
    {
        var factor = MultiplierPow(kLow, kHigh);
        return Multiply(state, factor);
    }

    /// <summary>
    /// <c>x · y mod 2¹²⁸</c>, a 128-by-128-bit schoolbook multiply keeping only the low 128 bits of the
    /// 256-bit product: each operand split into four 32-bit limbs, only the limb pairs whose indices sum to
    /// at most 3 contribute (a higher pair only ever carries into a dropped limb), so no array or allocation
    /// is needed (BOOT.md, "State": "<c>Math.BigMul</c> is not relied upon in kernel code").
    /// </summary>
    public static Mcg128State Multiply(Mcg128State x, Mcg128State y)
    {
        var x0 = (uint)x.Low;
        var x1 = (uint)(x.Low >> 32);
        var x2 = (uint)x.High;
        var x3 = (uint)(x.High >> 32);

        var y0 = (uint)y.Low;
        var y1 = (uint)(y.Low >> 32);
        var y2 = (uint)y.High;
        var y3 = (uint)(y.High >> 32);
        ulong r0;
        ulong product;
        ulong carry;

        // Row a = 0.
        product = (ulong)x0 * y0;
        r0 = (uint)product;
        carry = product >> 32;

        product = (ulong)x0 * y1 + carry;
        ulong r1 = (uint)product;
        carry = product >> 32;

        product = (ulong)x0 * y2 + carry;
        ulong r2 = (uint)product;
        carry = product >> 32;

        product = (ulong)x0 * y3 + carry;
        ulong r3 = (uint)product;

        // Row a = 1.
        product = (ulong)x1 * y0 + r1;
        r1 = (uint)product;
        carry = product >> 32;

        product = (ulong)x1 * y1 + r2 + carry;
        r2 = (uint)product;
        carry = product >> 32;

        product = (ulong)x1 * y2 + r3 + carry;
        r3 = (uint)product;

        // Row a = 2.
        product = (ulong)x2 * y0 + r2;
        r2 = (uint)product;
        carry = product >> 32;

        product = (ulong)x2 * y1 + r3 + carry;
        r3 = (uint)product;

        // Row a = 3.
        product = (ulong)x3 * y0 + r3;
        r3 = (uint)product;

        var low = r0 | (r1 << 32);
        var high = r2 | (r3 << 32);
        return new Mcg128State { Low = low, High = high };
    }

    /// <summary><c>a^k mod 2¹²⁸</c>, square-and-multiply over the 128 bits of k (at most 128 squarings).</summary>
    private static Mcg128State MultiplierPow(ulong kLow, ulong kHigh)
    {
        var result = new Mcg128State { Low = 1UL, High = 0UL };
        var baseValue = new Mcg128State { Low = MultiplierLow, High = MultiplierHigh };

        for (var bit = 0; bit < 64; bit++)
        {
            if (((kLow >> bit) & 1UL) != 0UL)
            {
                result = Multiply(result, baseValue);
            }

            baseValue = Multiply(baseValue, baseValue);
        }

        for (var bit = 0; bit < 64; bit++)
        {
            if (((kHigh >> bit) & 1UL) != 0UL)
            {
                result = Multiply(result, baseValue);
            }

            baseValue = Multiply(baseValue, baseValue);
        }

        return result;
    }

    /// <summary>
    /// The original's ten-term sum of <paramref name="state"/>'s limbs (Fortran line 1690): limb_i is bits
    /// [13i, 13i+13) of the 128-bit state for i = 0..8 and bits [117, 128) for i = 9, each converted to
    /// <see cref="double"/> exactly and weighted by x_i = 2^(13i-128), accumulated left to right.
    /// </summary>
    private static double TenTermSum(Mcg128State state)
    {
        var limb0 = ExtractLimb(state, 0, 0x1FFFUL);
        var limb1 = ExtractLimb(state, 13, 0x1FFFUL);
        var limb2 = ExtractLimb(state, 26, 0x1FFFUL);
        var limb3 = ExtractLimb(state, 39, 0x1FFFUL);
        var limb4 = ExtractLimb(state, 52, 0x1FFFUL);
        var limb5 = ExtractLimb(state, 65, 0x1FFFUL);
        var limb6 = ExtractLimb(state, 78, 0x1FFFUL);
        var limb7 = ExtractLimb(state, 91, 0x1FFFUL);
        var limb8 = ExtractLimb(state, 104, 0x1FFFUL);
        var limb9 = ExtractLimb(state, 117, 0x7FFUL);

        var sum = limb0 * X0;
        sum += limb1 * X1;
        sum += limb2 * X2;
        sum += limb3 * X3;
        sum += limb4 * X4;
        sum += limb5 * X5;
        sum += limb6 * X6;
        sum += limb7 * X7;
        sum += limb8 * X8;
        sum += limb9 * X9;
        return sum;
    }

    /// <summary>Bits [<paramref name="shift"/>, <paramref name="shift"/> + width) of the 128-bit state, masked to <paramref name="mask"/>.</summary>
    private static ulong ExtractLimb(Mcg128State state, int shift, ulong mask)
    {
        ulong value;
        if (shift == 0)
        {
            value = state.Low;
        }
        else if (shift < 64)
        {
            value = (state.Low >> shift) | (state.High << (64 - shift));
        }
        else
        {
            value = state.High >> (shift - 64);
        }

        return value & mask;
    }
}
