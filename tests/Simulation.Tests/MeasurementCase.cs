using PropStruct.Input;
using PropStruct.Tests.Harness;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// One formulation with the parameters it is run at, in the correction-effects measurement
/// (<c>BOOT.md</c>, "## Correction-effects measurement"): the five reference formulations at the original's
/// defaults, and four cases where the fixes that are invisible on the references have something to move.
/// </summary>
internal sealed record MeasurementCase(string Name, string Description, Formulation Formulation, ModelParameters Parameters)
{
    private static readonly string[] ReferenceNames = new[] { "HPEPA3", "inpt", "P33", "PSAN02n", "HMX" };

    /// <summary>The five reference formulations at <see cref="ModelParameters.Default"/>.</summary>
    public static IReadOnlyList<MeasurementCase> References { get; } = ReferenceNames
        .Select(name => new MeasurementCase(name, "reference formulation, the original's defaults", ReadFormulation(name), ModelParameters.Default))
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

    /// <summary>The case named <paramref name="name"/>; throws when no case has that name.</summary>
    public static MeasurementCase Named(string name) =>
        All.Single(measurementCase => string.Equals(measurementCase.Name, name, StringComparison.Ordinal));

    /// <summary>The formulation file <paramref name="name"/> of <c>tests/Fixtures</c>, pocket-forming fractions read when asked.</summary>
    public static Formulation ReadFormulation(string name, bool readPocketFormingFractions = false) =>
        DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat"), readPocketFormingFractions);

    private static MeasurementCase[] BuildExtras()
    {
        var p312 = ReadFormulation("P312");
        return new MeasurementCase[]
        {
            new("P312-defaults", "P312 at its defaults (gdokleft 0.122)", p312, ModelParameters.Default),
            new(
                "P312-Dmin5um",
                "P312 at Dmin 5 um",
                p312,
                ModelParameters.Default with { Dmin = Length.FromMicrometres(5) }),
            new("P312-Cycles3", "P312 over three cycles", p312 with { Cycles = 3 }, ModelParameters.Default),
            new(
                "PSAN02-Dmin20um-Di10um-SFR",
                "PSAN02 read with its pocket-forming fractions, Dmin 20 um, Di 10 um",
                ReadFormulation("PSAN02", readPocketFormingFractions: true),
                ModelParameters.Default with
                {
                    Dmin = Length.FromMicrometres(20),
                    CellSize = Length.FromMicrometres(10),
                    ReadPocketFormingFractions = true,
                }),
        };
    }
}
