namespace PropStruct.Particle;

/// <summary>
/// How an attempt's real-valued accumulators combine their terms (root BOOT.md,
/// "Precision kind is an option of every run, and Binary64 is the default"; API.md,
/// "Attempt"). A parameter of the one <see cref="Attempt.Run"/>, not a second
/// implementation: every draw, decision and geometry is identical under either kind; only
/// the accumulator write sites <c>RealFourAccumulators.generated.txt</c> names change how
/// they add, through <see cref="Attempt.AddReal4"/>. This node covers the accumulators
/// only; the setup plane the root's precision kind also names is <c>Statistics</c>' own
/// (implemented 2026-09-23, <c>src/Statistics/BOOT.md</c>, "## Setup plane").
///
/// ⚠ 2026-09-24: was named <c>Double</c>, now <c>Binary64</c> (CA1720, owner's decision) — an
/// identifier is not allowed to contain a type name, and the port's own arithmetic under this
/// kind is <c>double</c> regardless, so the old name collided with the keyword it described.
/// </summary>
internal enum PrecisionKind : byte
{
    /// <summary>
    /// Every accumulator sums in <c>double</c>, as the port always has. The zero value of
    /// this enum, so a caller built before this option existed (an unset <see
    /// cref="ModelSetup.Kind"/>) gets exactly this kind.
    /// </summary>
    Binary64 = 0,

    /// <summary>
    /// Rounds to binary32 after every addition exactly the accumulators the Fortran
    /// declares REAL*4 (<c>RealFourAccumulators.generated.txt</c>), reproducing the
    /// original's own accumulation loss; every other accumulator, and every size, distance
    /// and decision variable, stays <c>double</c> under this kind too (root BOOT.md).
    /// Sequential only: batched mode with this kind is refused by whichever node owns run
    /// configuration, not by this one (API.md, "Out of scope").
    /// </summary>
    Original = 1,
}
