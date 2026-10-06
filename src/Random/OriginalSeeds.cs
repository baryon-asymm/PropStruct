namespace PropStruct.Random;

/// <summary>
/// The original's six seeds (Fortran source lines 52-63) and the derivation of a particle's own six streams
/// by jump-ahead (BOOT.md, Constraints: "Per-particle streams").
/// </summary>
internal static class OriginalSeeds
{
    // Lines 62-63: u01/1/,u11/0/,u21/6812/,u31/1081/,u41/7705/,u51/5003/,u61/6576/,u71/7149/,u81/6654/,u91/1302/
    // Equals a^5 (BOOT.md, Invariants).
    private const ulong Seed1Low = 0xe1921cea70000001UL;
    private const ulong Seed1High = 0xa2d9fedf6e6c2717UL;

    // Lines 60-61: u02/1/,u12/0/,u22/7088/,u32/2503/,u42/6183/,u52/3683/,u62/6066/,u72/4620/,u82/7519/,u92/1298/
    // Equals a^4.
    private const ulong Seed2Low = 0x8274e3eec0000001UL;
    private const ulong Seed2High = 0xa25d5f9065ec9cc7UL;

    // Lines 58-59: u03/1/,u13/0/,u23/7364/,u33/3925/,u43/7109/,u53/884/,u63/2888/,u73/4164/,u83/3784/,u93/1899/
    // Differs from a^3 only in limb 8 (3784 here, 3748 in a^3 — the transposition of AGENTS.md §12, reproduced
    // as the original wrote it; see this node's BOOT.md, Constraints).
    private const ulong Seed3Low = 0xbc57aaf310000001UL;
    private const ulong Seed3High = 0xed6ec88222d206e9UL;

    // Lines 56-57: u04/1/,u14/0/,u24/7640/,u34/5347/,u44/2291/,u54/4799/,u64/945/,u74/6715/,u84/5714/,u94/914/
    // Equals a^2.
    private const ulong Seed4Low = 0x8f3a71f760000001UL;
    private const ulong Seed4High = 0x725652d1d8ec657eUL;

    // Lines 54-55: u05/1/,u15/0/,u25/7916/,u35/6769/,u45/8113/,u55/7234/,u65/4142/,u75/5015/,u85/3567/,u95/1526/
    // Equals a^1, the multiplier itself.
    private const ulong Seed5Low = 0xfb1d38fbb0000001UL;
    private const ulong Seed5High = 0xbecdef9cbc0bb885UL;

    // Lines 52-53: u06/1/,u16/0/,u26/0/,u36/0/,u46/0/,u56/0/,u66/0/,u76/0/,u86/0/,u96/0/
    // Equals a^0 = 1.
    private const ulong Seed6Low = 0x0000000000000001UL;
    private const ulong Seed6High = 0x0000000000000000UL;

    /// <summary>The original's six streams, as written in the source, unchanged.</summary>
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
    /// The six streams of the particle at <paramref name="ordinal"/> under <paramref name="seed"/>: every
    /// original seed advanced by the same <c>seed·2⁸⁰ + ordinal·2⁴⁰</c> steps, computed as
    /// <c>A80^seed · A40^ordinal</c> (square-and-multiply over the 64 bits of each), so the lag relation
    /// between the original seeds holds inside every particle (BOOT.md, "Jump-ahead"). <c>seed = 0</c>,
    /// <c>ordinal = 0</c> reproduces <see cref="Streams"/> exactly.
    /// </summary>
    public static StreamSet ForParticle(ulong seed, ulong ordinal)
    {
        var jump = ParticleJump.Factor(seed, ordinal);
        var original = Streams;
        return new StreamSet
        {
            S1 = Mcg128.Multiply(original.S1, jump),
            S2 = Mcg128.Multiply(original.S2, jump),
            S3 = Mcg128.Multiply(original.S3, jump),
            S4 = Mcg128.Multiply(original.S4, jump),
            S5 = Mcg128.Multiply(original.S5, jump),
            S6 = Mcg128.Multiply(original.S6, jump),
        };
    }

    /// <summary>
    /// The six streams of the batched particle at <paramref name="ordinal"/> under <paramref name="seed"/>
    /// (BOOT.md, "Batched derivation"; root BOOT.md, "Execution model", ⚠ 2026-09-19, revised the same day).
    /// Role r is jumped by <c>seed·2⁸⁰ + ordinal·2⁴⁰ + g(r)·2³⁸</c> steps: <see cref="ParticleJump.Factor"/>'s
    /// common seed/ordinal offset, times a distinct role-group offset <c>A38^g(r)</c> inside the particle's own
    /// stride (<c>g = 0</c> for S1/S2, <c>1</c> for S3/S4/S5, <c>2</c> for S6). Streams of one group keep their
    /// common offset, so the three lags the original keeps for a whole run survive in every particle (X0 of
    /// attempt k+1 = X of attempt k; X21 of neighbour k+1 = X2 of neighbour k; X1 of neighbour n tied to X2 of
    /// neighbour n+1 by the near-<c>a</c> multiplier <c>S3·S4⁻¹</c>, BOOT.md, "Batched derivation"); the
    /// remaining group starts 2³⁸ apart, far beyond any particle's own draws, so the original's start-of-run-only
    /// lag between it and the others (stream 6 replaying stream 4) does not recur in every particle.
    /// </summary>
    public static StreamSet ForBatchedParticle(ulong seed, ulong ordinal)
    {
        var jump = ParticleJump.Factor(seed, ordinal);
        var group0 = jump;                                // g = 0: S1, S2
        var group1 = Mcg128.Multiply(group0, Mcg128.A38);  // g = 1: S3, S4, S5
        var group2 = Mcg128.Multiply(group1, Mcg128.A38);  // g = 2: S6

        var original = Streams;
        return new StreamSet
        {
            S1 = Mcg128.Multiply(original.S1, group0),
            S2 = Mcg128.Multiply(original.S2, group0),
            S3 = Mcg128.Multiply(original.S3, group1),
            S4 = Mcg128.Multiply(original.S4, group1),
            S5 = Mcg128.Multiply(original.S5, group1),
            S6 = Mcg128.Multiply(original.S6, group2),
        };
    }
}
