namespace PropStruct.Tests.Harness;

/// <summary>
/// A small, deterministic, non-cryptographic pseudo-random generator (Vigna's SplitMix64, public domain) for
/// test-only randomness across the tree: generated Theory operands, bootstrap resampling, synthetic diagnostic
/// sweeps. Replaces <see cref="Random"/> everywhere it was used in tests (CA5394: <see cref="Random"/>
/// is flagged as an insecure generator even when seeded for reproducibility, which is the only property any of
/// these call sites ever needed). Every call site here already seeded <see cref="Random"/> with a fixed,
/// written-down integer (root BOOT.md, Taboos: "no clock-seeded generator"), never <see cref="Random"/>'s
/// own time-based default constructor, so the reproducibility this class offers is the same property, not a new
/// one. Not the port's own generator (the `Random` node's GSV=2 MCG): this class has no reproduction claim against
/// the original and is never referenced from production code.
/// </summary>
public sealed class SplitMix64
{
    private ulong _state;

    public SplitMix64(long seed)
    {
        _state = unchecked((ulong)seed);
    }

    /// <summary>Next raw 64-bit output.</summary>
    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform double in [0, 1), the same range as <see cref="Random.NextDouble"/>.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Uniform int in [0, maxExclusive), the same contract as <see cref="Random.Next(int)"/>.</summary>
    public int Next(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxExclusive);
        return (int)(NextDouble() * maxExclusive);
    }

    /// <summary>Uniform int in [minInclusive, maxExclusive), the same contract as
    /// <see cref="Random.Next(int, int)"/>.</summary>
    public int Next(int minInclusive, int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minInclusive, maxExclusive);
        return minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));
    }

    /// <summary>Uniform long in [minInclusive, maxExclusive), the same contract as
    /// <see cref="Random.NextInt64(long, long)"/>.</summary>
    public long NextInt64(long minInclusive, long maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minInclusive, maxExclusive);
        var range = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (long)(NextUInt64() % range);
    }

    /// <summary>Fills <paramref name="buffer"/> with random bytes, the same contract as
    /// <see cref="Random.NextBytes(Span{byte})"/>.</summary>
    public void NextBytes(Span<byte> buffer)
    {
        var i = 0;
        while (i < buffer.Length)
        {
            var word = NextUInt64();
            for (var b = 0; b < 8 && i < buffer.Length; b++, i++)
            {
                buffer[i] = (byte)(word >> (8 * b));
            }
        }
    }
}
