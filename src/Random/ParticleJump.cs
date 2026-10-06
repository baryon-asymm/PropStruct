namespace PropStruct.Random;

/// <summary>
/// The per-particle jump-ahead factor shared by every stream layout: <c>seed·2⁸⁰ + ordinal·2⁴⁰</c> steps,
/// computed as <c>A80^seed · A40^ordinal</c> (square-and-multiply over the 64 bits of each), so the same
/// jump reaches every stream of a particle (BOOT.md, "Jump-ahead"). One formula, applied to its own six
/// streams by both <see cref="OriginalSeeds.ForParticle"/> and <see cref="IndependentSeeds.ForParticle"/>
/// (root BOOT.md Taboos' ban on a second implementation of any part of the program).
/// </summary>
internal static class ParticleJump
{
    /// <summary><c>A80^seed · A40^ordinal mod 2¹²⁸</c>.</summary>
    public static Mcg128State Factor(ulong seed, ulong ordinal)
    {
        var bySeed = Pow(Mcg128.A80, seed);
        var byOrdinal = Pow(Mcg128.A40, ordinal);
        return Mcg128.Multiply(bySeed, byOrdinal);
    }

    /// <summary><c>value^exponent mod 2¹²⁸</c>, square-and-multiply over the 64 bits of exponent.</summary>
    private static Mcg128State Pow(Mcg128State value, ulong exponent)
    {
        var result = new Mcg128State { Low = 1UL, High = 0UL };
        var baseValue = value;

        for (var bit = 0; bit < 64; bit++)
        {
            if (((exponent >> bit) & 1UL) != 0UL)
            {
                result = Mcg128.Multiply(result, baseValue);
            }

            baseValue = Mcg128.Multiply(baseValue, baseValue);
        }

        return result;
    }
}
