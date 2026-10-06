namespace PropStruct.Statistics;

/// <summary>
/// The precondition <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> found violated, in the order it checks
/// them (BOOT.md, "Constraints": the original hangs or indexes out of bounds without
/// these checks). <see cref="Ok"/> means every precondition held.
/// </summary>
internal enum SetupStatus
{
    Ok,
    InvalidCoefficients,
    InvalidCellSize,
    InvalidMinimumSize,
    InvalidFractionBounds,
    ZeroFractionShare,

    /// <summary>
    /// <c>formulation.PocketFormingFractions</c> is not <c>null</c> but holds fewer
    /// elements than <c>formulation.Fractions</c> (BOOT.md, "Constraints"; a defect
    /// found by the audit of 2026-09-24, D7). <c>Input/API.md</c> makes no length
    /// promise between the two arrays for a <see cref="Input.Formulation"/>
    /// built in code rather than through <c>DatFile.Read</c> (which always reads
    /// exactly <c>NMM</c> flags when it reads any); indexing past the flag array's own
    /// end would otherwise throw <see cref="IndexOutOfRangeException"/>, which the root
    /// invariant "Failures are values" forbids for numerical code. Checked before
    /// <see cref="NoPocketFormingFraction"/>, since that check reads every flag.
    /// </summary>
    InvalidPocketFormingFractionCount,

    NoPocketFormingFraction,
    InvalidTailProbability,
    InvalidOxidizerFraction,

    /// <summary>
    /// <c>NnMax &lt;= 0</c>, or <c>NnMin &gt;= NnMax</c>: the accepted window
    /// <c>(NnMin, NnMax)</c> of <c>Particle.Attempt.Run</c>'s post-loop ratio
    /// <c>nn = ipocketLoc / ibridgeLoc</c> (always <c>&gt;= 0</c>, BOOT.md, Constraints)
    /// does not intersect <c>[0, +∞)</c>, so no attempt in a cycle &gt;= 1 can ever be
    /// accepted (BOOT.md, Constraints; the same failure mode <c>0 &lt; GGG &lt; 1</c>
    /// already guards for <c>λ</c>, just for the acceptance tests after the neighbour
    /// loop instead of before it).
    /// </summary>
    InvalidNnWindow,

    /// <summary>
    /// <see cref="FractionLaw.TryTailDraw"/> excluded every fraction. Unreachable given
    /// the other statuses above already hold (<see cref="FractionLaw.TryTailDraw"/>'s
    /// own doc comment carries the proof); kept as a status, not a defect any input can
    /// trigger (BOOT.md, Constraints).
    /// </summary>
    NoActiveFraction,
}
