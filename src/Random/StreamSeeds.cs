namespace PropStruct.Random;

/// <summary>
/// A particle's six streams under either layout (BOOT.md, "Independent layout"): the one entry point
/// <c>Particle</c> and <c>Execution</c> call, so a run's stream layout is a single switch rather than two
/// call sites picked apart in the caller. Kernel-compatible: no branch throws, so the method runs unchanged
/// on the host and in a kernel (root BOOT.md, Constraints: "Kernel-compatible C#").
/// </summary>
internal static class StreamSeeds
{
    /// <summary>
    /// <see cref="OriginalSeeds.ForParticle"/> for <see cref="StreamLayout.Original"/>,
    /// <see cref="IndependentSeeds.ForParticle"/> for <see cref="StreamLayout.Independent"/>.
    /// </summary>
    public static StreamSet ForParticle(StreamLayout layout, ulong seed, ulong ordinal)
    {
        if (layout == StreamLayout.Independent)
        {
            return IndependentSeeds.ForParticle(seed, ordinal);
        }

        return OriginalSeeds.ForParticle(seed, ordinal);
    }

    /// <summary>
    /// A batched particle's own six streams (BOOT.md, "Batched derivation"): <see cref="IndependentSeeds.ForParticle"/>
    /// for <see cref="StreamLayout.Independent"/> (its six orbits are already disjoint, so no role-group
    /// offset is needed — it equals <see cref="ForParticle"/>), <see cref="OriginalSeeds.ForBatchedParticle"/>
    /// for <see cref="StreamLayout.Original"/>.
    /// </summary>
    public static StreamSet ForBatchedParticle(StreamLayout layout, ulong seed, ulong ordinal)
    {
        if (layout == StreamLayout.Independent)
        {
            return IndependentSeeds.ForParticle(seed, ordinal);
        }

        return OriginalSeeds.ForBatchedParticle(seed, ordinal);
    }
}
