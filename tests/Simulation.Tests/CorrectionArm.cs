using System.Numerics;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Particle;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// One arm of the correction-effects measurement (<c>BOOT.md</c>, "## Correction-effects measurement"): the
/// <c>Original</c> model, the <c>Corrected</c> model with its own set (<c>All</c>), the <c>Corrected</c> model
/// with the one flag of <see cref="CorrectionSet"/> the arm is named after, or the <c>Corrected</c> model with every
/// flag but one of the category flags (<c>AllBut</c>, which interact), run through the seam
/// <see cref="SimulationOptions.Corrections"/> (<c>src/Simulation/API.md</c>, "## Model kind").
/// </summary>
internal sealed record CorrectionArm(string Name, ModelKind Model, CorrectionSet? Corrections)
{
    /// <summary>The model of 0.1.0, the arm every other arm is compared against.</summary>
    public static CorrectionArm Original { get; } = new("Original", ModelKind.Original, null);

    /// <summary>The <c>Corrected</c> model as the public option maps it, with no seam set: <c>CorrectionSet.All</c>.</summary>
    public static CorrectionArm Everything { get; } = new("All", ModelKind.Corrected, null);

    /// <summary>The flags of <see cref="CorrectionSet"/> that are one bit each: every member but <c>None</c> and <c>All</c>.</summary>
    public static IReadOnlyList<CorrectionSet> SingleFlags { get; } = Enum.GetValues<CorrectionSet>()
        .Where(flag => flag != CorrectionSet.None && BitOperations.IsPow2((uint)flag))
        .ToArray();

    /// <summary>
    /// The flags the category fixes are, those of <see cref="SingleFlags"/> whose name begins with
    /// <c>Categories</c>: they act on the same arrays (Fortran 869–959), so the effect of the three together is not
    /// the sum of their single effects, and the measurement takes each of them out of <see cref="Everything"/> as well.
    /// </summary>
    public static IReadOnlyList<CorrectionSet> CategoryFlags { get; } = SingleFlags
        .Where(flag => flag.ToString().StartsWith("Categories", StringComparison.Ordinal))
        .ToArray();

    /// <summary>
    /// <see cref="Original"/>, <see cref="Everything"/>, each single flag, then <c>AllBut</c> each of the
    /// <see cref="CategoryFlags"/>, the flags read from the enum itself so that the list cannot fall behind it
    /// (AGENTS.md §6, a criterion with the quantifier "all").
    /// </summary>
    public static IReadOnlyList<CorrectionArm> Measured { get; } = BuildMeasured();

    /// <summary>The single-flag arm of <paramref name="flag"/>.</summary>
    public static CorrectionArm Single(CorrectionSet flag) => new(flag.ToString(), ModelKind.Corrected, flag);

    /// <summary>The <c>Corrected</c> model with every flag but <paramref name="flag"/>, named <c>AllBut</c> and the flag.</summary>
    public static CorrectionArm AllBut(CorrectionSet flag) => new($"AllBut{flag}", ModelKind.Corrected, CorrectionSet.All & ~flag);

    /// <summary>The arm named <paramref name="name"/>; throws when no measured arm has that name.</summary>
    public static CorrectionArm Named(string name) =>
        Measured.Single(arm => string.Equals(arm.Name, name, StringComparison.Ordinal));

    /// <summary>The options of one run of this arm: the CPU accelerator, the <c>Independent</c> layout, <c>Binary64</c>.</summary>
    public SimulationOptions Options(ModelParameters parameters, ExecutionMode mode, ulong seed) => new()
    {
        Parameters = parameters,
        Mode = mode,
        Accelerator = AcceleratorKind.Cpu,
        Streams = StreamLayout.Independent,
        Precision = PrecisionKind.Binary64,
        Seed = seed,
        Model = Model,
        Corrections = Corrections,
    };

    private static CorrectionArm[] BuildMeasured()
    {
        var arms = new List<CorrectionArm> { Original, Everything };
        arms.AddRange(SingleFlags.Select(Single));
        arms.AddRange(CategoryFlags.Select(AllBut));
        return arms.ToArray();
    }
}
