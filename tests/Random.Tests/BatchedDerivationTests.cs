using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// The role-group offset of <see cref="OriginalSeeds.ForBatchedParticle"/> (BOOT.md, "Batched derivation";
/// root BOOT.md, "Execution model", ⚠ 2026-09-19, revised the same day), read directly through the raw draw
/// sequences of the six streams: the three lags the original keeps for a whole run (X0 of attempt k+1 = X of
/// attempt k; X21 of neighbour k+1 = X2 of neighbour k; X1 of neighbour n tied to X2 of neighbour n+1 by the
/// constant near-<c>a</c> multiplier <c>S3·S4⁻¹</c>) survive inside one derived particle, because streams
/// sharing a role-group offset (S1/S2; S3/S4/S5) keep their common relationship to each other — an exact
/// base-exponent difference of one step for S1/S2 and S4/S5, a constant ratio for S3/S4 — unchanged by the
/// shared per-particle jump; the spurious lag the pre-fix common jump introduced between different groups (S6
/// replaying S4's early draws, since a single jump left every one of the six streams' base-exponent
/// differences intact) does not recur once the remaining group starts 2³⁸ steps apart.
/// </summary>
public class BatchedDerivationTests
{
    private const int DrawCount = 1000;
    private const int RatioStepCount = 200;

    public static IEnumerable<object[]> SeedOrdinalPairs()
    {
        yield return [0UL, 0UL];
        yield return [7UL, 42UL];
        yield return [(1UL << 20) - 1, (1UL << 40) - 1];
    }

    [Theory]
    [MemberData(nameof(SeedOrdinalPairs))]
    public void PersistentLagsHoldInsideOneDerivedParticle(ulong seed, ulong ordinal)
    {
        var streams = OriginalSeeds.ForBatchedParticle(seed, ordinal);

        var drawsS1 = Draw(streams.S1, DrawCount);
        var drawsS2 = Draw(streams.S2, DrawCount);
        var drawsS4 = Draw(streams.S4, DrawCount);
        var drawsS5 = Draw(streams.S5, DrawCount);

        // X0 of attempt k+1 = X of attempt k: S1 and S2 share the group-0 offset, so their base-exponent
        // difference of exactly one step (a⁵ against a⁴) is unchanged by the shared per-particle jump.
        for (var k = 0; k < DrawCount - 1; k++)
        {
            AssertBitEqual(drawsS1[k], drawsS2[k + 1], $"k={k}: S2's draw {k + 1} must equal S1's draw {k}.");
        }

        // X21 of neighbour k+1 = X2 of neighbour k: S4 and S5 share the group-1 offset, same reasoning
        // (a² against a¹).
        for (var k = 0; k < DrawCount - 1; k++)
        {
            AssertBitEqual(drawsS4[k], drawsS5[k + 1], $"k={k}: S5's draw {k + 1} must equal S4's draw {k}.");
        }
    }

    /// <summary>
    /// X1 of neighbour n tied to X2 of neighbour n+1 (coordinator's wave-8 follow-up, 2026-09-19, root
    /// BOOT.md, "Execution model"): S3 (X1) and S4 (X2) draw in lockstep (label 501's distance draw, then
    /// exactly one label-345 neighbour pair), so their ratio <c>S3·S4⁻¹</c> is invariant under stepping both
    /// by the same count — a third persistent lag alongside S1/S2 and S4/S5, this one a near-<c>a</c>
    /// multiplier rather than <c>a</c> itself (the original's own transposed limb 8 of <c>a³</c>, BOOT.md,
    /// Invariants: <c>a³′ = a³ + 36·2¹⁰⁴</c>, makes <c>S3·S4⁻¹ = a + 36·2¹⁰⁴·a⁻²</c>). Sharing the S3/S4/S5
    /// role group keeps this ratio fixed at the original's own value for the whole particle, not just at
    /// derivation time.
    /// </summary>
    [Theory]
    [MemberData(nameof(SeedOrdinalPairs))]
    public void PersistentRatioS3TimesS4InverseIsConstantAndEqualsTheOriginals(ulong seed, ulong ordinal)
    {
        var originalRatio = Big128.Combine(OriginalSeeds.Streams.S3) * Big128.Inverse(Big128.Combine(OriginalSeeds.Streams.S4)) % Big128.Modulus;

        var streams = OriginalSeeds.ForBatchedParticle(seed, ordinal);
        var s3 = streams.S3;
        var s4 = streams.S4;

        for (var step = 0; step < RatioStepCount; step++)
        {
            var ratio = Big128.Combine(s3) * Big128.Inverse(Big128.Combine(s4)) % Big128.Modulus;
            Assert.True(ratio == originalRatio, $"step={step}: S3·S4⁻¹ must stay {originalRatio:X32}, was {ratio:X32}.");

            _ = Mcg128.Next(ref s3);
            _ = Mcg128.Next(ref s4);
        }
    }

    [Theory]
    [MemberData(nameof(SeedOrdinalPairs))]
    public void RoleGroupOffsetRemovesTheSpuriousCrossGroupReplay(ulong seed, ulong ordinal)
    {
        var streams = OriginalSeeds.ForBatchedParticle(seed, ordinal);

        var drawsS4 = Draw(streams.S4, DrawCount);
        var drawsS6 = Draw(streams.S6, DrawCount);
        var drawsS2 = Draw(streams.S2, DrawCount);

        var seenS4 = ToBitSet(drawsS4);

        Assert.DoesNotContain(drawsS6, draw => seenS4.Contains(BitConverter.DoubleToInt64Bits(draw)));
        Assert.DoesNotContain(drawsS2, draw => seenS4.Contains(BitConverter.DoubleToInt64Bits(draw)));
    }

    /// <summary>
    /// Non-degeneracy control for <see cref="RoleGroupOffsetRemovesTheSpuriousCrossGroupReplay"/>: the
    /// pre-fix derivation (root BOOT.md, "Execution model", ⚠ 2026-09-19 — <c>Kernels.DeriveStreams</c>'s call
    /// before the fix) jumped every one of the six streams by the very same common offset,
    /// <see cref="OriginalSeeds.ForParticle"/>. Under one common jump, every pair of the six streams keeps its
    /// fixed base-exponent difference (a⁰..a⁵), so a short window of raw draws always contains an exact
    /// collision between two streams that a distinct role-group offset removes — proving the check above is
    /// not vacuously green.
    /// </summary>
    [Theory]
    [MemberData(nameof(SeedOrdinalPairs))]
    public void WithoutTheRoleGroupOffsetTheCrossGroupReplayIsPresent(ulong seed, ulong ordinal)
    {
        var streams = OriginalSeeds.ForParticle(seed, ordinal);

        var drawsS4 = Draw(streams.S4, DrawCount);
        var drawsS6 = Draw(streams.S6, DrawCount);
        var drawsS2 = Draw(streams.S2, DrawCount);

        var seenS4 = ToBitSet(drawsS4);

        Assert.Contains(drawsS6, draw => seenS4.Contains(BitConverter.DoubleToInt64Bits(draw)));
        Assert.Contains(drawsS2, draw => seenS4.Contains(BitConverter.DoubleToInt64Bits(draw)));
    }

    private static HashSet<long> ToBitSet(double[] draws)
    {
        var set = new HashSet<long>(draws.Length);
        foreach (var draw in draws)
        {
            _ = set.Add(BitConverter.DoubleToInt64Bits(draw));
        }

        return set;
    }

    private static void AssertBitEqual(double expected, double actual, string message) => Assert.True(BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual), message);

    private static double[] Draw(Mcg128State state, int count)
    {
        var draws = new double[count];
        for (var i = 0; i < count; i++)
        {
            draws[i] = Mcg128.Next(ref state);
        }

        return draws;
    }
}
