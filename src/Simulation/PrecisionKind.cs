namespace PropStruct.Simulation;

/// <summary>
/// Which of the two precision kinds of <c>Particle</c> a run uses (root BOOT.md, "Precision kind is
/// an option of every run"). <c>Particle.PrecisionKind</c> itself is internal to the tree
/// (<c>Particle/API.md</c>, "## Precision kind"), and <see cref="SimulationOptions"/> is public, so a
/// public property cannot hold the neighbour's own enum type directly (CS0053) — the same fix as
/// <see cref="StreamLayout"/>'s own (<c>StreamLayout.cs</c>): a public mirror with the same cases,
/// translated to <c>PropStruct.Particle.PrecisionKind</c> at the one place <see cref="Simulator"/> calls
/// into <c>Particle</c>/<c>Execution</c>. <c>Particle</c>'s own surface is unchanged, for the same reason
/// <c>StreamLayout.cs</c> gives: the translation costs this node one line, not the neighbour a wider public
/// contract. This node's own `Original` reproduces the accumulators (`Particle`'s own), the setup
/// plane (`Statistics`' own, implemented 2026-09-23, `src/Statistics/BOOT.md`, "## Setup plane") and the
/// per-cycle plane (`Statistics`' own, implemented 2026-10-01, `src/Statistics/BOOT.md`, "## Report").
///
/// ⚠ 2026-09-24: was named <c>Double</c>, now <c>Binary64</c> (CA1720, owner's decision) — an
/// identifier is not allowed to contain a type name, and the port's own arithmetic under this
/// kind is <c>double</c> regardless, so the old name collided with the keyword it described.
/// </summary>
public enum PrecisionKind
{
    /// <summary>Computes in <c>double</c> throughout, as the port always has (root BOOT.md, "Precision kind is an option of every run"); the default.</summary>
    Binary64,

    /// <summary>Reproduces the binary32 values the original's executable computes with: the accumulators (<c>Particle</c>), the setup plane and the per-cycle plane (<c>Statistics</c>); the attempt plane stays <c>double</c> under this kind too.</summary>
    Original,
}

/// <summary>Translates the public mirror to <c>Particle</c>'s own internal enum, the one place the two meet.</summary>
internal static class PrecisionKindMapping
{
    public static Particle.PrecisionKind ToParticle(this PrecisionKind kind) => kind switch
    {
        PrecisionKind.Binary64 => Particle.PrecisionKind.Binary64,
        PrecisionKind.Original => Particle.PrecisionKind.Original,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null),
    };
}
