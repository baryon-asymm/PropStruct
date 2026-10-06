namespace PropStruct.Simulation;

/// <summary>
/// Which precondition of <c>Statistics.Setup.Prepare</c> failed (architecture audit finding R1,
/// 2026-09-24: "<c>InvalidSetup</c> absorbs every setup precondition failure ... <c>RunStatus.cs</c> tells
/// public callers to read <c>SetupStatus</c>, which is internal"). <c>Statistics.SetupStatus</c> itself is
/// internal to the tree (<c>Statistics/API.md</c>, "## Setup"), and <see cref="SimulationFailedException"/>
/// is public, so a public property cannot hold the neighbour's own enum type directly (CS0053) - the same
/// fix as <see cref="PrecisionKind"/> and <see cref="StreamLayout"/> (<c>PrecisionKind.cs</c>,
/// <c>StreamLayout.cs</c>): a public mirror with the same cases, translated from
/// <c>PropStruct.Statistics.SetupStatus</c> at the one place <see cref="Simulator"/> turns a failed
/// <c>Prepare</c> call into <see cref="SimulationFailedException"/>. <c>Ok</c> has no member here: this
/// mirror is only ever read from a failed run (<see cref="SimulationFailedException.SetupFailure"/> is
/// <see langword="null"/> otherwise), so there is nothing for an "Ok" case to mean. <c>Statistics</c>' own
/// surface is unchanged, for the same reason <c>StreamLayout.cs</c> gives: the translation costs this node
/// one line, not the neighbour a wider public contract.
/// </summary>
public enum SetupFailureReason
{
    /// <summary>One of the pocket/bridge coefficients (<c>Alpha</c>, <c>PocketCoefficient</c>, <c>BridgeCoefficient</c>) is out of its valid range.</summary>
    InvalidCoefficients,

    /// <summary>The cell size is non-positive or otherwise invalid.</summary>
    InvalidCellSize,

    /// <summary>The minimum oxidizer size (<c>Dmin</c>) is non-positive or not below every fraction's own upper bound.</summary>
    InvalidMinimumSize,

    /// <summary>The formulation's fraction bounds are not a valid, ordered partition.</summary>
    InvalidFractionBounds,

    /// <summary>A fraction's own mass share is zero where the setup needs it positive.</summary>
    ZeroFractionShare,

    /// <summary>The count of pocket-forming fractions does not match what the formulation declares.</summary>
    InvalidPocketFormingFractionCount,

    /// <summary>No fraction is marked pocket-forming, though the setup needs at least one.</summary>
    NoPocketFormingFraction,

    /// <summary>The tail probability parameter is outside its valid range.</summary>
    InvalidTailProbability,

    /// <summary>The oxidizer mass fraction is outside its valid range.</summary>
    InvalidOxidizerFraction,

    /// <summary>The window <c>NnMin</c>/<c>NnMax</c> is empty or otherwise invalid (`src/Cli/BOOT.md`'s own escalation, `NnMax = 0.0`).</summary>
    InvalidNnWindow,

    /// <summary>No fraction is active under the given parameters.</summary>
    NoActiveFraction,
}

/// <summary>Translates <c>Statistics</c>' own internal status to the public mirror, the one place the two meet.</summary>
internal static class SetupFailureReasonMapping
{
    public static SetupFailureReason ToSetupFailureReason(this Statistics.SetupStatus status) => status switch
    {
        Statistics.SetupStatus.InvalidCoefficients => SetupFailureReason.InvalidCoefficients,
        Statistics.SetupStatus.InvalidCellSize => SetupFailureReason.InvalidCellSize,
        Statistics.SetupStatus.InvalidMinimumSize => SetupFailureReason.InvalidMinimumSize,
        Statistics.SetupStatus.InvalidFractionBounds => SetupFailureReason.InvalidFractionBounds,
        Statistics.SetupStatus.ZeroFractionShare => SetupFailureReason.ZeroFractionShare,
        Statistics.SetupStatus.InvalidPocketFormingFractionCount => SetupFailureReason.InvalidPocketFormingFractionCount,
        Statistics.SetupStatus.NoPocketFormingFraction => SetupFailureReason.NoPocketFormingFraction,
        Statistics.SetupStatus.InvalidTailProbability => SetupFailureReason.InvalidTailProbability,
        Statistics.SetupStatus.InvalidOxidizerFraction => SetupFailureReason.InvalidOxidizerFraction,
        Statistics.SetupStatus.InvalidNnWindow => SetupFailureReason.InvalidNnWindow,
        Statistics.SetupStatus.NoActiveFraction => SetupFailureReason.NoActiveFraction,
        Statistics.SetupStatus.Ok => throw new ArgumentOutOfRangeException(nameof(status), status, "Ok is not a setup failure."),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, message: null),
    };
}
