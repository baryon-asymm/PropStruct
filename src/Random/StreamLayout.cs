namespace PropStruct.Random;

/// <summary>
/// Which of the node's two seed layouts a run's streams start from (BOOT.md, "Independent layout"; root
/// BOOT.md, "Two stream layouts"). <see cref="Original"/> starts the six role streams at the original's own
/// seeds, lag and all (this node's BOOT.md, the declared deviation under "⚠ AGENTS.md §12 deviation").
/// <see cref="Independent"/> starts them on six disjoint orbits of the same generator instead, so no role
/// ever shares a number with another under any jump-ahead (this node's BOOT.md, "Orbits").
/// </summary>
internal enum StreamLayout
{
    Original,
    Independent,
}
