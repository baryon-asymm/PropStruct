using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;

namespace PropStruct.RateTableTool;

/// <summary>
/// Runs one reference-mode simulation and parses its <c>results.m</c>, shared by this node's own two
/// generators (<see cref="Program"/>'s <c>rate-table</c> and <c>seed-zero-snapshot</c> subcommands) and by
/// <c>tests/Simulation.Tests/StatisticalCriterionTests</c>'s own change-detector test
/// (<c>SeedZeroResultsMMatchesApprovedSnapshot</c>) — one implementation of "run this seed and parse the
/// result" (root BOOT.md Taboos), not a second copy beside that node's own pre-existing
/// <c>RunAndRatchet</c> helper, which this class's own text does not touch (it runs
/// <see cref="ExecutionMode.Batched"/>, for link 3, out of this class's own scope: this class exists only
/// for <see cref="ExecutionMode.Reference"/> at an arbitrary seed and precision, for the fixed-seed
/// snapshot and the sixteen-seed rate table).
///
/// ⚠ 2026-09-25: moved here from <c>tests/Simulation.Tests/ReferenceModeRunner.cs</c>, unchanged in
/// accessibility (<c>internal</c>). Both of its generators (the rate table and the seed-zero snapshot)
/// were <c>[Fact(Skip = "manual regeneration only")]</c> tests there — a permanently skipped test,
/// which AGENTS.md §13 forbids ("a perpetually red check is worse than an absent one"; a generator
/// marked skip reads the same way to a reader scanning test output). A generator is not a check, so
/// it moved out of the test tree entirely into this node. <c>tests/Simulation.Tests</c> — a neighbour
/// now, not the same node — still needs this class for its one remaining, genuine assertion (the
/// byte-identity check against the approved snapshot); rather than make it public (CA1515: this
/// project builds an application, so an unnecessarily public type is flagged), it stays internal and
/// this node's own csproj grants that one neighbour <c>InternalsVisibleTo</c>, the same resolution
/// this tree already uses for <c>tests/Harness</c> → <c>tests/Harness.Tests</c>.
/// </summary>
internal static class ReferenceModeRunner
{
    /// <summary><paramref name="Candidate"/> is the parsed quantities, ready for
    /// <see cref="StatisticalCriterion.Compare"/> and its siblings; <paramref name="NormalizedText"/> is the raw
    /// <c>results.m</c> text with the one line that varies with wall time (<c>tests/Harness/BOOT.md</c>, "The time
    /// line ... is recognized by its <c>Calculation time</c> prefix and never becomes a quantity") removed;
    /// <paramref name="Sha256Hex"/> is that text's own SHA-256, hex-encoded, upper case (<see cref="Convert.ToHexString"/>'s
    /// own casing) — the snapshot unit both callers above compare; <paramref name="JsonSha256Hex"/> is the SHA-256
    /// of the whole <see cref="SimulationResult"/> as <see cref="ResultsJson"/> writes it (every double round-trips,
    /// so a change below <c>results.m</c>'s print resolution moves it), with the fields that depend on the run's
    /// wall time or machine — <c>Diagnostics.Elapsed</c> and <c>Diagnostics.Accelerator</c> — replaced by constants
    /// and the line endings made LF.</summary>
    internal readonly record struct Run(
        IReadOnlyDictionary<string, double[]> Candidate, string NormalizedText, string Sha256Hex, string JsonSha256Hex);

    private static readonly AcceleratorInfo NormalizedAccelerator =
        new(AcceleratorKind.Cpu, "normalized", MemoryBytes: 0L, LibDeviceLinked: false, CudaSkippedBecause: null);

    public static Run RunSeed(Formulation formulation, StreamLayout layout, PrecisionKind precision, ulong seed) =>
        Execute(formulation, new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = layout,
            Precision = precision,
            Seed = seed,
        }, parseCandidate: true, reshapeForJson: null);

    /// <summary>The hashes of one run of a formulation under <paramref name="options"/>, whose
    /// <see cref="SimulationOptions.Parameters"/> are the parameters its <c>results.m</c> is written with. The
    /// returned run carries no parsed <see cref="Run.Candidate"/> (it is empty): the Original-model guard hashes
    /// what the run prints and holds, and a run whose <c>results.m</c> prints <c>NaN</c> is a legitimate case
    /// of it, one <see cref="ResultsMFile"/> does not parse.</summary>
    public static Run Fingerprint(Formulation formulation, SimulationOptions options) =>
        Execute(formulation, options, parseCandidate: false, reshapeForJson: null);

    /// <summary>
    /// <see cref="Fingerprint(Formulation, SimulationOptions)"/> with <paramref name="reshapeForJson"/> applied to the
    /// result before the JSON hash is taken, the <c>results.m</c> hash untouched: for the comparisons that hold two
    /// runs equal in every number but one the parameters' own stored form moves (<c>tests/Simulation.Tests</c>,
    /// "## The Dmin cell index at run level"). The guards never call it, so their hashes are those of the overload above.
    /// </summary>
    public static Run Fingerprint(
        Formulation formulation, SimulationOptions options, Func<SimulationResult, SimulationResult> reshapeForJson) =>
        Execute(formulation, options, parseCandidate: false, reshapeForJson);

    private static Run Execute(
        Formulation formulation, SimulationOptions options, bool parseCandidate, Func<SimulationResult, SimulationResult>? reshapeForJson)
    {
        using var simulator = Simulator.Create(options);
        var result = simulator.Run(formulation);

        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, options.Parameters, result, tempPath);
            var candidate = parseCandidate ? ResultsMFile.Parse(tempPath) : FrozenDictionary<string, double[]>.Empty;

            using var withoutTimeLine = new StringReader(Encoding.UTF8.GetString(ResultsMTimeLine.Remove(File.ReadAllBytes(tempPath))));
            var normalizedText = string.Join('\n', ReadLines(withoutTimeLine));
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText)));

            return new Run(candidate, normalizedText, hash, JsonSha256Of(reshapeForJson?.Invoke(result) ?? result, tempPath));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static string JsonSha256Of(SimulationResult result, string path)
    {
        var normalized = result with
        {
            Diagnostics = result.Diagnostics with { Elapsed = TimeSpan.Zero, Accelerator = NormalizedAccelerator },
        };
        ResultsJson.Write(normalized, path);
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static IEnumerable<string> ReadLines(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
