using System.Collections.Immutable;

namespace PropStruct.Input;

/// <summary>
/// The original's oxidizer size law, from <c>JZZ</c> (BOOT.md, Constraints; source lines 245-248 and
/// wherever <c>JZZ</c> selects the law). <c>JZZ == 2</c> reads as <see cref="Uniform"/>; every other value,
/// including <c>1</c>, reads as <see cref="UniformInReciprocalSquare"/>.
/// </summary>
public enum SizeLaw
{
    /// <summary>
    /// No law of the original's; <see cref="Formulation.SizeLaw"/> never produces this value (CA1008, satisfied
    /// without a fabricated reading of <c>JZZ</c>: the mapping stays two-valued, and this is only the enum's
    /// required zero member).
    /// </summary>
    None = 0,

    /// <summary>The original's default oxidizer size law: <c>JZZ != 2</c>, including <c>JZZ == 1</c>
    /// (source lines 245-248).</summary>
    UniformInReciprocalSquare = 1,

    /// <summary>The original's oxidizer size law when <c>JZZ == 2</c> (source lines 245-248).</summary>
    Uniform = 2,
}

/// <summary>
/// One oxidizer fraction: its mass share (an element of <c>GDOK</c>) and its size bounds (a pair of
/// elements of <c>DDOK</c>), each kept as the original wrote it (see <see cref="Length"/>).
/// </summary>
public readonly record struct OxidizerFraction(double MassShare, Length LowerBound, Length UpperBound);

/// <summary>
/// A formulation exactly as the original's <c>.dat</c> reads it (BOOT.md, "The original's reading order",
/// source lines 93-133 and 245-248). No physical plausibility is checked here (BOOT.md, Constraints):
/// <c>Statistics.Setup.Prepare</c> owns the preconditions of the model.
/// </summary>
public sealed record Formulation(
    string Name,
    double OxidizerDensity,
    double PropellantDensity,
    double OxidizerMassFraction,
    double MetalMassFraction,
    double Ak1,
    double Ak2,
    double Ak3,
    double Ak4,
    int SizeLawCode,
    int Cycles,
    int ParticlesPerCycle,
    double GeneratorWarmup,
    ImmutableArray<OxidizerFraction> Fractions,
    ImmutableArray<int>? PocketFormingFractions)
{
    /// <summary><see cref="SizeLawCode"/> (the original's <c>JZZ</c> as read) mapped to <see cref="SizeLaw"/>.</summary>
    public SizeLaw SizeLaw => SizeLawCode == 2 ? SizeLaw.Uniform : SizeLaw.UniformInReciprocalSquare;
}
