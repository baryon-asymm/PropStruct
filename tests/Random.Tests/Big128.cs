using System.Numerics;

namespace PropStruct.Random.Tests;

/// <summary>
/// The <see cref="BigInteger"/> oracle every test in this node checks the production 128-bit arithmetic
/// against (BOOT.md, Invariants: "the oracle is <c>System.Numerics.BigInteger</c>, not a second 128-bit
/// implementation").
/// </summary>
internal static class Big128
{
    public static readonly BigInteger Modulus = BigInteger.One << 128;

    /// <summary>The order of the cyclic subgroup <c>⟨a⟩</c> every stream state lives in (BOOT.md,
    /// "Constraints", "Orbits": the multiplier is ≡ 1 mod 2²⁸, so its order divides 2¹²⁸/2²⁸ = 2¹⁰⁰, and is
    /// exactly 2¹⁰⁰ since the six original seeds — a⁰..a⁵ among them — are all distinct).</summary>
    public static readonly BigInteger Order = BigInteger.One << 100;

    /// <summary>Packs limb-endian base-2¹³ digits (a Fortran seed or multiplier DATA statement) into a <see cref="BigInteger"/>.</summary>
    public static BigInteger Pack(IReadOnlyList<int> limbs)
    {
        var value = BigInteger.Zero;
        for (var i = 0; i < limbs.Count; i++)
        {
            value += (BigInteger)(uint)limbs[i] << (13 * i);
        }

        return value;
    }

    /// <summary>The <see cref="Mcg128State"/> fields (Low, High) that hold <paramref name="value"/> mod 2¹²⁸.</summary>
    public static (ulong Low, ulong High) Split(BigInteger value)
    {
        var reduced = (value % Modulus + Modulus) % Modulus;
        var low = (ulong)(reduced & ulong.MaxValue);
        var high = (ulong)(reduced >> 64);
        return (low, high);
    }

    /// <summary>The <see cref="BigInteger"/> a <see cref="Mcg128State"/>'s (Low, High) pair represents.</summary>
    public static BigInteger Combine(ulong low, ulong high) => ((BigInteger)high << 64) | low;

    public static BigInteger Combine(Mcg128State state) => Combine(state.Low, state.High);

    /// <summary><paramref name="value"/>^<paramref name="exponent"/> mod 2¹²⁸.</summary>
    public static BigInteger ModPow(BigInteger value, BigInteger exponent) => BigInteger.ModPow(value, exponent, Modulus);

    /// <summary>
    /// The multiplicative inverse of <paramref name="value"/> within <c>⟨a⟩</c>, the order-<see cref="Order"/>
    /// subgroup every stream state lives in: <c>value^(Order - 1) mod 2¹²⁸</c>, since
    /// <c>value^Order ≡ 1 (mod 2¹²⁸)</c> for any <c>value</c> whose order divides <see cref="Order"/>.
    /// </summary>
    public static BigInteger Inverse(BigInteger value) => ModPow(value, Order - 1);
}
