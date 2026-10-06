namespace PropStruct.Statistics;

/// <summary>
/// The pass structure of one loop the executable unrolls (generated kinds B<c>n</c> and C<c>n</c>
/// of <c>CyclePlane.listing.generated.txt</c>): <c>trips</c> passes, taken <c>unroll</c> at a
/// time while at least <c>unroll</c> remain, then one at a time. A loop of fewer than
/// <c>unroll</c> trips has no block, only remainder passes. Passes are numbered from 1. The
/// schedule says only where the executable's sum is stored and where its next pass reads a
/// register or the memory slot; the caller decides what a store does to the value, which under
/// <see cref="PropStruct.Particle.PrecisionKind.Binary64"/> is nothing.
/// </summary>
internal readonly struct UnrolledSchedule(int trips, int unroll)
{
    private readonly int blockPasses = trips / unroll * unroll;

    /// <summary>A B<c>n</c> sum is stored after this pass: the last of a block, and every remainder pass.</summary>
    public bool RoundsAfter(int pass) => pass > blockPasses || pass % unroll == 0;

    /// <summary>The pass is in a block and not the last of it: the JZZ = 2 loop stores <c>DOKSD</c> after these passes and carries it in the register after the last pass of a block and through the remainder.</summary>
    public bool IsBlockPassBeforeLast(int pass) => pass <= blockPasses && pass % unroll != 0;

    /// <summary>A C<c>n</c> pass reads the previous element from memory: the first of a block, and every remainder pass. The others read the register.</summary>
    public bool ReloadsBefore(int pass) => pass > blockPasses || (pass - 1) % unroll == 0;
}
