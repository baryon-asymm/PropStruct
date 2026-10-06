namespace PropStruct.Statistics;

/// <summary>
/// The one precondition <see cref="Categories.MergeAndDescribe"/> checks before its
/// step-1 loop (BOOT.md, "## Categories"): the initial category count <c>DPRow</c>
/// (<c>int(DPmax/Dj) + 1</c>, the original's own formula, no clamp) must not exceed
/// <c>Ncat</c>, the capacity every category array — <c>Dpockets</c>, <c>Qdoks</c>'s own
/// row dimension, <c>Dokp41</c>, <c>Dokp31</c> — is allocated with. <see cref="Ok"/>
/// means it held.
/// </summary>
internal enum CategoriesStatus
{
    Ok,

    /// <summary>
    /// <c>DPRow &gt; Ncat</c>: unreachable given <c>Ncat = trunc(Ddokmax·Ak4/Dj) + 2</c>
    /// (<see cref="Setup.Sizes"/>) and the model's own bound on the largest pocket
    /// radius that ever reaches <c>DPmax</c>, <c>RK ≤ Ddokmax·Ak4</c> (BOOT.md,
    /// "## Categories"); kept as a status, not a defect any input can trigger, the same
    /// pattern <see cref="SetupStatus.NoActiveFraction"/> already uses. Reported instead
    /// of the silent <c>r &lt; ncat</c> clamp this node's own audit found undeclared: the
    /// Fortran has no such clamp, so silently truncating the category count would change
    /// the model's output if this status ever fired (root BOOT.md, Taboos: "No silent
    /// fix of a defect of the original").
    /// </summary>
    CategoryCountExceedsCapacity,
}
