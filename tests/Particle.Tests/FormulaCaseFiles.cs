using System.Text.Json;
using PropStruct.Tests.Harness;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Deserialization shapes for the formula fixtures of <c>tests/Fixtures/cases/particle/</c>
/// (BOOT.md, "Invariants": expected values come from <c>tests/Fixtures</c>, never typed
/// into a test) and the small helpers that load them, following the same pattern as
/// <c>tests/Input.Tests/CaseFile.cs</c>. Every array and 1-based index inside a case is the
/// Fortran convention of the transcribed subroutine (<c>tests/Fixtures/API.md</c>,
/// "## Particle formula cases"), not the port's own 0-based one: a test converts where it
/// calls the port's own function with it.
/// </summary>
internal static class FormulaCaseFiles
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private static string Directory { get; } = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "particle");

    public static IReadOnlyList<SizeLawCase> ReadSizeLawCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "size_law.json"));
        return JsonSerializer.Deserialize<SizeLawCaseFile>(json, Options)!.Cases;
    }

    public static IReadOnlyList<BridgeGeometryCase> ReadBridgeGeometryCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "bridge_geometry_volume.json"));
        return JsonSerializer.Deserialize<BridgeGeometryCaseFile>(json, Options)!.Cases;
    }

    public static IReadOnlyList<BridgeWindowCase> ReadBridgeWindowCases()
    {
        var json = File.ReadAllText(Path.Combine(Directory, "bridge_window.json"));
        return JsonSerializer.Deserialize<BridgeWindowCaseFile>(json, Options)!.Cases;
    }

    // Looked up by Name from a [Theory]'s own string parameter (CA1515, found 2026-09-24 under
    // AnalysisMode=All: BridgeGeometryExpected/BridgeWindowExpected no longer need to be public now that
    // the case they belong to is looked up here instead of being handed to the Theory piecewise).
    public static BridgeGeometryCase FindBridgeGeometryCase(string name) =>
        ReadBridgeGeometryCases().Single(c => c.Name == name);

    public static BridgeWindowCase FindBridgeWindowCase(string name) =>
        ReadBridgeWindowCases().Single(c => c.Name == name);
}

internal sealed record SizeLawCaseFile(IReadOnlyList<SizeLawCase> Cases);

internal sealed record SizeLawCase(
    string Name, int SizeLaw, IReadOnlyList<double> Bounds, IReadOnlyList<double> Cumulative, double X, double X1,
    SizeLawExpected Expected);

/// <summary><see cref="Fraction"/> is the Fortran 1-based <c>MINV</c>.</summary>
internal sealed record SizeLawExpected(int Fraction, double Diameter);

internal sealed record BridgeGeometryCaseFile(IReadOnlyList<BridgeGeometryCase> Cases);

internal sealed record BridgeGeometryCase(string Name, double R1, double R2, double Rk, double A, BridgeGeometryExpected Expected);

/// <summary>
/// <see cref="Bb"/>/<see cref="Vmkm"/> are <c>null</c> when the branch that sets them never
/// ran (<c>tests/Fixtures/API.md</c>, "## Particle formula cases"): <see cref="BridgeGeometry.Volume"/>
/// leaves its own <c>out</c> parameter at 0 in that case (`BridgeGeometry.cs`, `Volume`'s doc
/// comment), which a case's <c>null</c> is read against, not a value from the fixture.
/// </summary>
internal sealed record BridgeGeometryExpected(int Jj, bool Swapped, double? Bb, double? Vmkm);

internal sealed record BridgeWindowCaseFile(IReadOnlyList<BridgeWindowCase> Cases);

internal sealed record BridgeWindowCase(
    string Name, double CellSize, double Ak3, double Ak4, double Dr, IReadOnlyList<double> Qks1,
    BridgeWindowExpected Expected, IReadOnlyList<BridgeWindowSample> Samples);

/// <summary>
/// <see cref="CellCount"/>/<see cref="Boundaries"/>/<see cref="Cumulative"/> are <c>null</c>
/// when <see cref="EmptyWindow"/> is <see langword="true"/> (the line-590 restart, nothing
/// else computed). <see cref="MinCell"/>/<see cref="MaxCell"/> are the Fortran-1-based
/// <c>mindk</c>/<c>maxdk</c>, not exposed by <see cref="BridgeWindow.Build"/>'s own output
/// (its shifted <c>dpoc</c>/<c>fqks</c> already carry the same information as
/// <see cref="Boundaries"/>/<see cref="Cumulative"/>), so a test checks those two instead.
/// </summary>
internal sealed record BridgeWindowExpected(
    bool EmptyWindow, int? MinCell, int? MaxCell, int? CellCount, IReadOnlyList<double>? Boundaries,
    IReadOnlyList<double>? Cumulative);

internal sealed record BridgeWindowSample(double X3, BridgeWindowSampleExpected Expected);

internal sealed record BridgeWindowSampleExpected(double PocketSize);
