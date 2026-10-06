namespace PropStruct.Particle;

/// <summary>
/// The one outcome an attempt ends with (BOOT.md, "Attempt structure"; API.md, "Attempt").
/// </summary>
internal enum AttemptOutcome : byte
{
    /// <summary>End of the loop and, in cycles ≥ 1, no test of Fortran lines 723-738 fired; unconditional in cycle 0.</summary>
    Accepted,

    /// <summary>End of the loop, a test of Fortran lines 723-738 fired (cycles ≥ 1 only).</summary>
    RestartedAfterLoop,

    /// <summary>Any <c>GO TO 11</c> of Fortran lines 473, 476, 480, 549, 568 or 590 (before or inside the neighbour loop).</summary>
    RestartedInsideLoop,

    /// <summary>Distance draws (label 501 passes) of this attempt exceeded <see cref="ModelSetup.NeighbourBudget"/>.</summary>
    NeighbourBudgetExceeded,

    /// <summary>Pocket-size redraws (label 451 passes) at one bridge exceeded <see cref="ModelSetup.PocketRedrawBudget"/>.</summary>
    BridgeDrawBudgetExceeded,

    /// <summary>The <c>DM</c> search (<see cref="BridgeWindow.SamplePocket"/>) left its window layout; a guard, not a model branch.</summary>
    IndexOutOfRange,
}
