using System.Numerics;

namespace PropStruct.Random.Tests;

/// <summary>
/// An independent, arbitrary-precision implementation of the original's GSV=2 step and ten-term sum (Fortran
/// lines 1661-1690), used only as the oracle L1 of the BOOT.md table checks the production
/// <see cref="Mcg128.Next"/> against. The weights are recomputed here with <see cref="Math.Pow(double, double)"/>
/// rather than copied from <c>Mcg128</c>'s literals, so this is a genuinely independent computation, not a
/// second, faster path through the same formula.
/// </summary>
internal static class BigIntegerStepper
{
    /// <summary>Steps <paramref name="state"/> once and returns the new state and the draw, both computed independently of <c>Mcg128</c>.</summary>
    public static (BigInteger State, double Draw) Step(BigInteger state, BigInteger multiplier)
    {
        var next = state * multiplier % Big128.Modulus;

        var sum = 0.0;
        for (var i = 0; i < 10; i++)
        {
            var shift = 13 * i;
            var mask = i == 9 ? (BigInteger.One << 11) - 1 : (BigInteger.One << 13) - 1;
            var limb = (double)(int)((next >> shift) & mask);
            var weight = Math.Pow(2.0, 13 * i - 128);
            sum += limb * weight;
        }

        return (next, sum);
    }
}
