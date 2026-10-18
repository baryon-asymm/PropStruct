using System.Globalization;
using PropStruct.Input;
using PropStruct.Tests.Harness;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// One formulation with the parameters it is run at, in the correction-effects measurement
/// (<c>BOOT.md</c>, "## Correction-effects measurement"): the five reference formulations at the original's
/// defaults, and four cases where the fixes that are invisible on the references have something to move.
/// <paramref name="SeedCount"/> is the number of seeds <c>k * 2^17</c> the case is run over.
/// </summary>
internal sealed record MeasurementCase(string Name, string Description, Formulation Formulation, ModelParameters Parameters, int SeedCount)
{
    /// <summary>Seeds of a reference formulation: fourteen arms of about fourteen seconds a seed over all five make the whole in about an hour.</summary>
    private const int ReferenceSeedCount = 16;

    /// <summary>Seeds of an extra case, which the task asks for at eight at least.</summary>
    private const int ExtraSeedCount = 8;

    /// <summary>
    /// P312 draws about 16000 attempts per particle (measured 2026-10-18 on 400 particles: 7.4 million attempts in 25 s,
    /// batched, CPU), so its 5000 particles take about five minutes a run; the fixes it is here for act on totals
    /// that its own mass distribution fixes, not on the sample.
    /// </summary>
    private const int P312ParticlesPerCycle = 100;

    /// <summary>The same reduction for three cycles, so that the work per run stays that of the one-cycle cases.</summary>
    private const int P312ThreeCycleParticlesPerCycle = 40;

    private static readonly string[] ReferenceNames = new[] { "HPEPA3", "inpt", "P33", "PSAN02n", "HMX" };

    /// <summary>The five reference formulations at <see cref="ModelParameters.Default"/>.</summary>
    public static IReadOnlyList<MeasurementCase> References { get; } = ReferenceNames
        .Select(name => new MeasurementCase(name, "reference formulation, the original's defaults", ReadFormulation(name), ModelParameters.Default, ReferenceSeedCount))
        .ToArray();

    /// <summary>
    /// The cases for the fixes the references do not show (<c>VdokTotalOnCopy</c>, <c>ZdoksmallCoarseShare</c>,
    /// <c>GdokleftOnce</c>, <c>PdoksmallFirstCell</c>, <c>DoksdConsistent</c> beyond P33): P312 at its defaults, at
    /// <c>Dmin</c> 5 µm and over three cycles, and PSAN02 read with its pocket-forming fractions at <c>Dmin</c> 20 µm,
    /// <c>Di</c> 10 µm. Both lengths are <see cref="Length.FromMicrometres"/>: with the default <c>Di</c> a
    /// <c>Dmin</c> of 20 µm truncates to one cell.
    /// </summary>
    public static IReadOnlyList<MeasurementCase> Extras { get; } = BuildExtras();

    /// <summary>The references first, then the extras.</summary>
    public static IReadOnlyList<MeasurementCase> All { get; } = References.Concat(Extras).ToArray();

    /// <summary>The seeds <c>k * 2^17</c> of this case, <c>PROPSTRUCT_MEASUREMENT_SEEDS</c> replacing the count when it is set.</summary>
    public IReadOnlyList<ulong> Seeds
    {
        get
        {
            var overridden = int.TryParse(Environment.GetEnvironmentVariable("PROPSTRUCT_MEASUREMENT_SEEDS"), NumberStyles.None, CultureInfo.InvariantCulture, out var count);
            return SeedsOf(overridden ? count : SeedCount);
        }
    }

    /// <summary>The first <paramref name="count"/> seeds <c>k * 2^17</c>: a stride that lands the streams' phases on the eighths of the circle (link 3, <c>BOOT.md</c>).</summary>
    public static IReadOnlyList<ulong> SeedsOf(int count) => Enumerable.Range(0, count).Select(k => (ulong)k << 17).ToArray();

    /// <summary>The case named <paramref name="name"/>; throws when no case has that name.</summary>
    public static MeasurementCase Named(string name) =>
        All.Single(measurementCase => string.Equals(measurementCase.Name, name, StringComparison.Ordinal));

    /// <summary>The formulation file <paramref name="name"/> of <c>tests/Fixtures</c>, pocket-forming fractions read when asked.</summary>
    public static Formulation ReadFormulation(string name, bool readPocketFormingFractions = false) =>
        DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat"), readPocketFormingFractions);

    private static MeasurementCase[] BuildExtras()
    {
        var p312 = ReadFormulation("P312");
        var reduced = p312 with { ParticlesPerCycle = P312ParticlesPerCycle };
        var note = $"particles per cycle reduced from {p312.ParticlesPerCycle} to";
        return new MeasurementCase[]
        {
            new("P312-defaults", $"P312 at its defaults, {note} {P312ParticlesPerCycle}", reduced, ModelParameters.Default, ExtraSeedCount),
            new(
                "P312-Dmin5um",
                $"P312 at Dmin 5 um, {note} {P312ParticlesPerCycle}",
                reduced,
                ModelParameters.Default with { Dmin = Length.FromMicrometres(5) },
                ExtraSeedCount),
            new(
                "P312-Cycles3",
                $"P312 over three cycles, {note} {P312ThreeCycleParticlesPerCycle}",
                p312 with { Cycles = 3, ParticlesPerCycle = P312ThreeCycleParticlesPerCycle },
                ModelParameters.Default,
                ExtraSeedCount),
            new(
                "PSAN02-Dmin20um-Di10um-SFR",
                "PSAN02 read with its pocket-forming fractions, Dmin 20 um, Di 10 um, particles per cycle as read",
                ReadFormulation("PSAN02", readPocketFormingFractions: true),
                ModelParameters.Default with
                {
                    Dmin = Length.FromMicrometres(20),
                    CellSize = Length.FromMicrometres(10),
                    ReadPocketFormingFractions = true,
                },
                ExtraSeedCount),
        };
    }
}
