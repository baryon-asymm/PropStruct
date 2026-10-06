namespace PropStruct.Random;

/// <summary>
/// The <see cref="StreamLayout.Independent"/> layout's six seeds and the derivation of a particle's own six
/// streams by jump-ahead (BOOT.md, Constraints: "Independent layout").
///
/// Role <c>r = 1..6</c> starts at <c>H_r·2²⁸ + (2r + 1)</c>, where <c>H_r</c> is the leading 100 bits of
/// <c>SHA-256("PropStruct.Random.Independent/r")</c> (ASCII, <c>r</c> in decimal). The distinct odd tails
/// (3, 5, 7, 9, 11, 13) put the six seeds in six different classes mod 2²⁸, so their orbits under every
/// jump-ahead are disjoint (BOOT.md, "Orbits"); the hash-derived high 100 bits keep every pairwise ratio
/// <c>s_r·s_q⁻¹ mod 2¹²⁸</c> far from a small integer (proven against a from-scratch SHA-256 computation,
/// `tests/Random.Tests/IndependentSeedsTests.cs`).
/// </summary>
internal static class IndependentSeeds
{
    // SHA-256("PropStruct.Random.Independent/1") = 526a61e946089b1b76953ad92e48fac2512b06c75f746cc2145205fbc9c0d4e
    // H_1 = the leading 100 bits of the digest; seed = H_1·2²⁸ + 3.
    private const ulong Seed1Low = 0x76953ad920000003UL;
    private const ulong Seed1High = 0x526a61e946089b1bUL;

    // SHA-256("PropStruct.Random.Independent/2") = 810be2e8033822c75e55f8ffea03e07d3f8b71263f253483141cb52ecbc61c8
    // H_2 = the leading 100 bits of the digest; seed = H_2·2²⁸ + 5.
    private const ulong Seed2Low = 0x5e55f8ffe0000005UL;
    private const ulong Seed2High = 0x810be2e8033822c7UL;

    // SHA-256("PropStruct.Random.Independent/3") = be4de532e63f85b8af9cae1240c4599176dae94ddd5a45fecf19dec15676de8
    // H_3 = the leading 100 bits of the digest; seed = H_3·2²⁸ + 7.
    private const ulong Seed3Low = 0xaf9cae1240000007UL;
    private const ulong Seed3High = 0xbe4de532e63f85b8UL;

    // SHA-256("PropStruct.Random.Independent/4") = 57d84c98d4cdd7f272a7975b4764e48562026e528595d8d09acb67cfa25d073
    // H_4 = the leading 100 bits of the digest; seed = H_4·2²⁸ + 9.
    private const ulong Seed4Low = 0x72a7975b40000009UL;
    private const ulong Seed4High = 0x57d84c98d4cdd7f2UL;

    // SHA-256("PropStruct.Random.Independent/5") = 97cdf182efa45fc60087fc4d85f981caca4692f9b20a29bb29cf471e71d191e
    // H_5 = the leading 100 bits of the digest; seed = H_5·2²⁸ + 11.
    private const ulong Seed5Low = 0x0087fc4d8000000bUL;
    private const ulong Seed5High = 0x97cdf182efa45fc6UL;

    // SHA-256("PropStruct.Random.Independent/6") = dbffac0dae414c525b8cd4eeb3fda72ba68d4504656c0fe7d5da0c32bfba8e2
    // H_6 = the leading 100 bits of the digest; seed = H_6·2²⁸ + 13.
    private const ulong Seed6Low = 0x5b8cd4eeb000000dUL;
    private const ulong Seed6High = 0xdbffac0dae414c52UL;

    /// <summary>The Independent layout's six streams, one per role.</summary>
    public static StreamSet Streams => new()
    {
        S1 = new Mcg128State { Low = Seed1Low, High = Seed1High },
        S2 = new Mcg128State { Low = Seed2Low, High = Seed2High },
        S3 = new Mcg128State { Low = Seed3Low, High = Seed3High },
        S4 = new Mcg128State { Low = Seed4Low, High = Seed4High },
        S5 = new Mcg128State { Low = Seed5Low, High = Seed5High },
        S6 = new Mcg128State { Low = Seed6Low, High = Seed6High },
    };

    /// <summary>
    /// The six Independent streams of the particle at <paramref name="ordinal"/> under <paramref name="seed"/>:
    /// every Independent seed advanced by the same <c>seed·2⁸⁰ + ordinal·2⁴⁰</c> steps
    /// (<see cref="ParticleJump.Factor"/>). <c>seed = 0</c>, <c>ordinal = 0</c> reproduces
    /// <see cref="Streams"/> exactly.
    /// </summary>
    public static StreamSet ForParticle(ulong seed, ulong ordinal)
    {
        var jump = ParticleJump.Factor(seed, ordinal);
        var independent = Streams;
        return new StreamSet
        {
            S1 = Mcg128.Multiply(independent.S1, jump),
            S2 = Mcg128.Multiply(independent.S2, jump),
            S3 = Mcg128.Multiply(independent.S3, jump),
            S4 = Mcg128.Multiply(independent.S4, jump),
            S5 = Mcg128.Multiply(independent.S5, jump),
            S6 = Mcg128.Multiply(independent.S6, jump),
        };
    }
}
