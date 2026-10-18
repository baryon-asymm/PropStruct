using System.Globalization;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;

namespace PropStruct.RateTableTool;

/// <summary>
/// One case of the guard that keeps the <c>Original</c> model bit for bit what it is (BOOT.md, "## The
/// Original-model guard"): a formulation, the options of a CPU run and the model parameters, all fixed here
/// so that the generator and the test read one definition. <paramref name="Id"/> has no blank: it is the
/// first field of a line of the approved file.
/// </summary>
internal sealed record GuardCase(
    string Id,
    string FormulationName,
    int? Cycles,
    int? ParticlesPerCycle,
    ExecutionMode Mode,
    StreamLayout Layout,
    PrecisionKind Precision,
    ModelParameters Parameters)
{
    public Formulation ReadFormulation()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", FormulationName + ".dat");
        var formulation = DatFile.Read(path, Parameters.ReadPocketFormingFractions);
        return formulation with
        {
            Cycles = Cycles ?? formulation.Cycles,
            ParticlesPerCycle = ParticlesPerCycle ?? formulation.ParticlesPerCycle,
        };
    }

    public SimulationOptions Options() => new()
    {
        Parameters = Parameters,
        Mode = Mode,
        Accelerator = AcceleratorKind.Cpu,
        Streams = Layout,
        Precision = Precision,
        Seed = 0UL,
    };
}

/// <summary>
/// The cases of the Original-model guard and the approved file they are compared against
/// (<c>tests/Simulation.Tests/Snapshots/OriginalModelGuard.approved.txt</c>): written by this node's
/// <c>original-model-guard</c> subcommand, read by <c>tests/Simulation.Tests/OriginalModelGuardTests</c>. One
/// list, one line format, one path; neither the generator nor the test has a copy of any of them.
/// </summary>
internal static class OriginalModelGuard
{
    private static readonly string[] References = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];
    private static readonly StreamLayout[] Layouts = [StreamLayout.Original, StreamLayout.Independent];
    private static readonly PrecisionKind[] Precisions = [PrecisionKind.Original, PrecisionKind.Binary64];

    /// <summary>The particle count of every extra case, the same for all (BOOT.md, "## The Original-model
    /// guard"): `P312` costs about 0.28 s per particle in Release (measured), so its own 5000 would cost 23 minutes a
    /// case, and `P2`'s 438000 is out of the question.</summary>
    private const int ExtraParticlesPerCycle = 50;

    public static IReadOnlyList<GuardCase> Cases { get; } = [.. BuildCases()];

    public static string ApprovedPath => RepositoryPaths.Resolve(
        "tests", "Simulation.Tests", "Snapshots", "OriginalModelGuard.approved.txt");

    public static GuardCase Find(string id) => Cases.Single(c => string.Equals(c.Id, id, StringComparison.Ordinal));

    /// <summary>The line the approved file carries for <paramref name="guardCase"/>: its id, the SHA-256 of its
    /// <c>results.m</c> (time line cut) and the SHA-256 of its normalised result JSON.</summary>
    public static string Line(GuardCase guardCase, ReferenceModeRunner.Run run) =>
        string.Create(CultureInfo.InvariantCulture, $"{guardCase.Id} {run.Sha256Hex} {run.JsonSha256Hex}");

    /// <summary>The approved hashes by case id: (<c>results.m</c> hash, JSON hash). Empty when the file does
    /// not exist, so that the test can fail with the regeneration command instead of a file error.</summary>
    public static Dictionary<string, (string ResultsM, string Json)> LoadApproved()
    {
        var map = new Dictionary<string, (string ResultsM, string Json)>(StringComparer.Ordinal);
        if (!File.Exists(ApprovedPath))
        {
            return map;
        }

        foreach (var line in File.ReadAllLines(ApprovedPath))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split(' ');
            map[fields[0]] = (fields[1], fields[2]);
        }

        return map;
    }

    private static IEnumerable<GuardCase> BuildCases()
    {
        // The twenty cases of the seed-0 snapshot: reference mode, both layouts, both precision kinds.
        foreach (var name in References)
        {
            foreach (var layout in Layouts)
            {
                foreach (var precision in Precisions)
                {
                    yield return Whole($"seed0/{name}/{layout}/{precision}", name, ExecutionMode.Reference, layout, precision);
                }
            }
        }

        // Batched mode on the CPU host path: the only layout and kind batched mode accepts.
        foreach (var name in References)
        {
            yield return Whole($"batched/{name}/Independent/Binary64", name, ExecutionMode.Batched, StreamLayout.Independent, PrecisionKind.Binary64);
        }

        // Paths the five references leave dark (no oxidizer left in the smallest pockets there): small
        // particles, a read SFR vector, several cycles, other fraction counts and size laws.
        var defaults = ModelParameters.Default;
        var psan02 = defaults with { ReadPocketFormingFractions = true, Dmin = Length.FromMicrometres(20) };
        var psan02Di10 = psan02 with { CellSize = Length.FromMicrometres(10) };
        yield return Extra("P312-defaults", "P312", cycles: null, defaults);
        yield return Extra("P312-dmin5", "P312", cycles: null, defaults with { Dmin = Length.FromMicrometres(5) });
        yield return Extra("P312-cycles3", "P312", cycles: 3, defaults);
        yield return Extra("P312-homogenized0.1", "P312", cycles: null, defaults with { HomogenizedOxidizerFraction = 0.1 });
        yield return Extra("PSAN02-sfr-dmin20", "PSAN02", cycles: null, psan02);
        yield return Extra("PSAN02-sfr-dmin20-cycles3", "PSAN02", cycles: 3, psan02);
        yield return Extra("PSAN02-sfr-dmin20-di10", "PSAN02", cycles: null, psan02Di10);
        yield return Extra("P33-defaults", "P33", cycles: null, defaults);
        yield return Extra("P2-defaults", "P2", cycles: null, defaults);
    }

    private static GuardCase Whole(string id, string name, ExecutionMode mode, StreamLayout layout, PrecisionKind precision) =>
        new(id, name, Cycles: null, ParticlesPerCycle: null, mode, layout, precision, ModelParameters.Default);

    private static GuardCase Extra(string name, string formulation, int? cycles, ModelParameters parameters) =>
        new($"extra/{name}", formulation, cycles, ExtraParticlesPerCycle, ExecutionMode.Reference, StreamLayout.Independent, PrecisionKind.Binary64, parameters);
}
