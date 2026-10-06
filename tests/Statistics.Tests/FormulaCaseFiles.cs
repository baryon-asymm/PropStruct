using System.Text.Json;
using System.Text.Json.Serialization;
using PropStruct.Tests.Harness;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Deserialization shapes for the formula fixtures of
/// <c>tests/Fixtures/cases/statistics/</c> (BOOT.md, "Invariants": expected values come
/// from <c>tests/Fixtures</c>, never typed into a test), following the same pattern as
/// <c>tests/Particle.Tests/FormulaCaseFiles.cs</c>. Unlike that file's cases, every
/// array and index here is 0-based, matching this node's own C# signatures directly
/// (<c>tests/Fixtures/formulas_statistics.py</c>, "## Categories" convention note).
///
/// Every case type below is <c>internal</c> (CA1720's sibling CA1515, "types can be made
/// internal", found 2026-09-24 under <c>AnalysisMode=All</c>): each `[Theory]` that used
/// to take a case record directly now takes the case's own <c>Name</c> (a <c>string</c>,
/// already unique per fixture file - checked once, at the bottom of this file, rather than
/// assumed) and looks the full record up through <see cref="FindCategoriesCase"/> and its
/// three siblings, so no public surface is needed only to satisfy xUnit's parameter-type
/// visibility rule (CS0051).
/// </summary>
internal static class FormulaCaseFiles
{
    private static string Directory { get; } = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "statistics");

    public static IReadOnlyList<CategoriesCase> ReadCategoriesCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "categories.json"));
        return JsonSerializer.Deserialize(json, FormulaCaseFileJsonContext.Default.CategoriesCaseFile)!.Cases;
    }

    public static IReadOnlyList<SmallParticlesProbabilityCase> ReadSmallParticlesProbabilityCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "small_particles_probability.json"));
        return JsonSerializer.Deserialize(json, FormulaCaseFileJsonContext.Default.SmallParticlesProbabilityCaseFile)!.Cases;
    }

    public static IReadOnlyList<SmallParticlesMaxSizeCase> ReadSmallParticlesMaxSizeCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "small_particles_max_size.json"));
        return JsonSerializer.Deserialize(json, FormulaCaseFileJsonContext.Default.SmallParticlesMaxSizeCaseFile)!.Cases;
    }

    public static IReadOnlyList<CycleCase> ReadCycleCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "cycle_statistics.json"));
        return JsonSerializer.Deserialize(json, FormulaCaseFileJsonContext.Default.CycleCaseFile)!.Cases;
    }

    public static IReadOnlyList<PlaneFormsCase> ReadPlaneFormsCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "plane_forms.json"));
        return JsonSerializer.Deserialize(json, FormulaCaseFileJsonContext.Default.PlaneFormsCaseFile)!.Cases;
    }

    public static IReadOnlyList<SetupPlaneCase> ReadSetupPlaneCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "setup_plane.json"));
        return JsonSerializer.Deserialize(json, FormulaCaseFileJsonContext.Default.SetupPlaneCaseFile)!.Cases;
    }

    // Looked up by Name from a [Theory]'s own string parameter, never by the caller re-reading and
    // re-filtering the file itself - one place owns "how a case is found by name" for each fixture file.
    public static CategoriesCase FindCategoriesCase(string name) =>
        ReadCategoriesCases().Single(c => c.Name == name);

    public static SmallParticlesProbabilityCase FindSmallParticlesProbabilityCase(string name) =>
        ReadSmallParticlesProbabilityCases().Single(c => c.Name == name);

    public static SmallParticlesMaxSizeCase FindSmallParticlesMaxSizeCase(string name) =>
        ReadSmallParticlesMaxSizeCases().Single(c => c.Name == name);

    public static CycleCase FindCycleCase(string name) =>
        ReadCycleCases().Single(c => c.Name == name);

    public static PlaneFormsCase FindPlaneFormsCase(string name) =>
        ReadPlaneFormsCases().Single(c => c.Name == name);

    public static SetupPlaneCase FindSetupPlaneCase(string name) =>
        ReadSetupPlaneCases().Single(c => c.Name == name);
}

/// <summary>
/// Source-generated (de)serialization for this file's own shapes: a plain
/// <c>JsonSerializer.Deserialize&lt;T&gt;</c> call gives CA1812 no evidence that the four <c>*CaseFile</c>
/// wrapper records are ever constructed (reflection-based deserialization is invisible to it), following the
/// same pattern as <c>tests/Input.Tests/CaseFile.cs</c>'s <c>CaseFileJsonContext</c>.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
[JsonSerializable(typeof(CategoriesCaseFile))]
[JsonSerializable(typeof(SmallParticlesProbabilityCaseFile))]
[JsonSerializable(typeof(SmallParticlesMaxSizeCaseFile))]
[JsonSerializable(typeof(CycleCaseFile))]
[JsonSerializable(typeof(PlaneFormsCaseFile))]
[JsonSerializable(typeof(SetupPlaneCaseFile))]
internal sealed partial class FormulaCaseFileJsonContext : JsonSerializerContext;

internal sealed record CategoriesCaseFile(IReadOnlyList<CategoriesCase> Cases);

internal sealed record CategoriesCase(
    string Name, int Ndok, int Ncat, double CellSize, double CategoryStep, double EpsDok, double DpMax,
    IReadOnlyList<IReadOnlyList<long>> Qdoks, IReadOnlyList<double> Dokp41, IReadOnlyList<double> Dokp31,
    CategoriesExpected Expected, CategoriesExpected ExpectedOriginal);

internal sealed record CategoriesExpected(
    int DpRow, IReadOnlyList<double> Dpockets, IReadOnlyList<double> Dokp43, IReadOnlyList<double> Qdokkarm,
    IReadOnlyList<IReadOnlyList<double>> Qdokso, IReadOnlyList<IReadOnlyList<long>> QdoksAfter,
    IReadOnlyList<double> Dokp41After, IReadOnlyList<double> Dokp31After);

internal sealed record SmallParticlesProbabilityCaseFile(IReadOnlyList<SmallParticlesProbabilityCase> Cases);

internal sealed record SmallParticlesProbabilityCase(
    string Name, int Ndok, double CellSize, double Ak2, double Ggg, double Gdokleft, double Karmcoef,
    double OxidizerDensity, double PropellantDensity, IReadOnlyList<double> Vdokstr,
    SmallParticlesProbabilityExpected Expected, SmallParticlesProbabilityExpected ExpectedOriginal);

internal sealed record SmallParticlesProbabilityExpected(IReadOnlyList<double> Pdoksmall);

internal sealed record SmallParticlesMaxSizeCaseFile(IReadOnlyList<SmallParticlesMaxSizeCase> Cases);

internal sealed record SmallParticlesMaxSizeCase(
    string Name, int Ndok, double CellSize, double Ddokmax, double Karmcoef, double Mkmcoef,
    IReadOnlyList<double> Pdoksmall, SmallParticlesMaxSizeExpected Expected, SmallParticlesMaxSizeExpected ExpectedOriginal);

internal sealed record SmallParticlesMaxSizeExpected(double Dmaxxx);

internal sealed record PlaneFormsCaseFile(IReadOnlyList<PlaneFormsCase> Cases);

/// <summary>
/// One form of the plane's products (<c>Form</c> names the helper of <c>CyclePlaneOrder</c>, operands
/// <c>[x, di, index, m]</c> and the value the script read off the table's <c>order</c> text) or, for
/// <c>Form = schedule</c>, the pass decisions of one unroll over trip counts 1, 2, 3, ... (one list per
/// trip count, one entry per pass).
/// </summary>
internal sealed record PlaneFormsCase(
    string Name, string Form, string Site, int Unroll, IReadOnlyList<IReadOnlyList<double>> Operands,
    IReadOnlyList<double> ExpectedOriginal, IReadOnlyList<IReadOnlyList<bool>> RoundsAfter,
    IReadOnlyList<IReadOnlyList<bool>> ReloadsBefore);

internal sealed record SetupPlaneCaseFile(IReadOnlyList<SetupPlaneCase> Cases);

internal sealed record SetupPlaneCase(string Name, string DatFile, SetupPlaneExpected Expected);

internal sealed record SetupPlaneExpected(
    IReadOnlyList<double> Z11, IReadOnlyList<double> Zx, double Zss, double Lambda, double Dmax, double Dokm,
    double Doksd, double Ddokmax, double TailProbabilityModified, double OxidizerMassFractionEffective);

internal sealed record CycleCaseFile(IReadOnlyList<CycleCase> Cases);

/// <summary>
/// One constructed <c>CycleStatistics.Compute</c> scenario: the setup, the echoes, the inputs and the
/// totals the script's own pipeline was fed, and every value that pipeline produced under each kind. The
/// expected values stay a name-to-JSON map: the test pairs each name with its report member and fails on a
/// name either side lacks, so no value of the file can go unchecked.
/// </summary>
internal sealed record CycleCase(
    string Name, int CycleIndex, CycleCaseSetup Setup, CycleCaseTables Tables, CycleCaseEchoes Echoes,
    CycleCaseInputs Inputs, CycleCaseIntegerTotals IntegerTotals, CycleCaseRealTotals RealTotals,
    IReadOnlyDictionary<string, JsonElement> Expected, IReadOnlyDictionary<string, JsonElement> ExpectedOriginal);

internal sealed record CycleCaseSetup(
    int FractionCount, int Ndok, int Nkarm, int Ncat, int Nc, double CellSize, double CategoryStep, double Dmin,
    double Ak2, double PocketCoefficient, double BridgeCoefficient);

internal sealed record CycleCaseTables(IReadOnlyList<double> Share, IReadOnlyList<byte> PocketForming, IReadOnlyList<double> MassShare);

internal sealed record CycleCaseEchoes(double Dokm, double Doksd, double Ddokmax, double Ggg);

internal sealed record CycleCaseInputs(
    double OxidizerDensity, double PropellantDensity, double OxidizerMassFraction, double MetalMassFraction,
    double HomogenizedOxidizerFraction, double EpsDok, double AggregatedOxideFraction);

internal sealed record CycleCaseIntegerTotals(
    long Nfx, long Nfy, long Nfz, long Nfq, long Nfw, IReadOnlyList<long> AlldokFract, IReadOnlyList<long> Alldok,
    IReadOnlyList<long> Qks, IReadOnlyList<long> FqkarmCor, IReadOnlyList<long> Coef, IReadOnlyList<long> Qmkm1,
    IReadOnlyList<long> Qmkm2, IReadOnlyList<IReadOnlyList<long>> Qdoks);

internal sealed record CycleCaseRealTotals(
    IReadOnlyList<double> Xss, double Sd4, double Sd3, double D41, double D31, double DokBase41, double DokBase31,
    double DokSur41, double DokSur31, double Dp41, double Dp31, double DpMax, double DpMaxCor,
    IReadOnlyList<double> Allvdok, IReadOnlyList<double> Vdokstr, IReadOnlyList<double> Vks,
    IReadOnlyList<double> FmkarmCor, IReadOnlyList<double> Fmkarm2, IReadOnlyList<double> Dokp41,
    IReadOnlyList<double> Dokp31, IReadOnlyList<double> Vsmkm, IReadOnlyList<double> Svd,
    IReadOnlyList<double> VmkmTotal, IReadOnlyList<double> VdokTotal, double VmkmTotal2, double VdokTotal2);
