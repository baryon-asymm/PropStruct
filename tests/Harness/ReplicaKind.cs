namespace PropStruct.Tests.Harness;

/// <summary>
/// Which replica set a <see cref="StatisticalCriterion.Compare"/> call validates against (root BOOT.md,
/// "Statistical reference criterion", revised 2026-09-17; `tests/Harness/HISTORY.md#criterion-revision-2026-09-17`). Each kind
/// names its own directory under <c>tests/Fixtures</c> (<c>tests/Fixtures/API.md</c>, "Layout"):
/// <list type="bullet">
/// <item><see cref="Lagged"/> — <c>replicas-lagged</c>: the <c>Original</c> layout's seeds jumped by
/// <c>k*2^80 + 2^79</c>, keeping the lag structure of root BOOT.md's correlated seeds. Validates the
/// <c>Original</c> layout, and is the default comparison for a candidate of that layout.</item>
/// <item><see cref="Independent"/> — <c>replicas-independent</c>: the <c>Independent</c> layout's states jumped
/// likewise. Validates the <c>Independent</c> layout.</item>
/// <item><see cref="Gsv3"/> — <c>replicas-gsv3</c>: the original's own GSV=3 generator, clock-seeded. Kept as a
/// reported cross-check, never a pass condition (root BOOT.md, ⚠ 2026-09-17: GSV=3 estimates a different
/// quantity than the port's <c>Original</c> layout, "Known bias of the original's seeds").</item>
/// </list>
/// </summary>
public enum ReplicaKind
{
    Lagged,
    Independent,
    Gsv3,
}
