namespace PropStruct.Simulation;

/// <summary>
/// Which of the two stream layouts of <c>Random</c> a run uses (root BOOT.md, "Two stream layouts").
/// <see cref="Original"/> is sequential only and is refused with <see cref="SimulationOptions.Mode"/> ==
/// <see cref="ExecutionMode.Batched"/> (<c>SimulationFailedException(RunStatus.InvalidSetup, …)</c>,
/// <c>BOOT.md</c>, "`Original` layout is refused for batched execution, unconditionally"); <see cref="Independent"/>
/// runs in both modes and is <see cref="SimulationOptions.Streams"/>'s own default.
/// <c>Random.StreamLayout</c> itself is internal to the tree (<c>Random/API.md</c>), and
/// <see cref="SimulationOptions"/> is public, so a public property cannot hold the neighbour's own enum
/// type directly (CS0053) — this is the smallest honest fix: a public mirror with the same two cases,
/// translated to <c>PropStruct.Random.StreamLayout</c> at the one place <see cref="Simulator"/> calls into
/// <c>Random</c>/<c>Execution</c> (BOOT.md, "Design decisions (2026-09-18)", "Accelerator by mode" and this
/// file's own note below). <c>Random</c>'s own surface is unchanged: escalating to widen it would have cost
/// a neighbour's public contract for a translation this node can do itself in one place.
/// </summary>
public enum StreamLayout
{
    /// <summary>Starts the six role streams at the original's own seeds, with their defects (root BOOT.md, "Two stream layouts"); sequential mode only.</summary>
    Original,

    /// <summary>Starts the six role streams on six disjoint orbits of the same generator, so no role ever shares a number with another (root BOOT.md, "Two stream layouts"); the default, and the only layout batched mode accepts.</summary>
    Independent,
}

/// <summary>Translates the public mirror to <c>Random</c>'s own internal enum, the one place the two meet.</summary>
internal static class StreamLayoutMapping
{
    public static Random.StreamLayout ToRandom(this StreamLayout layout) => layout switch
    {
        StreamLayout.Original => Random.StreamLayout.Original,
        StreamLayout.Independent => Random.StreamLayout.Independent,
        _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, message: null),
    };
}
