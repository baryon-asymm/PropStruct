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
    /// own casing) — the snapshot unit both callers above compare.</summary>
    internal readonly record struct Run(IReadOnlyDictionary<string, double[]> Candidate, string NormalizedText, string Sha256Hex);

    public static Run RunSeed(Formulation formulation, StreamLayout layout, PrecisionKind precision, ulong seed)
    {
        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = layout,
            Precision = precision,
            Seed = seed,
        });
        var result = simulator.Run(formulation);

        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, ModelParameters.Default, result, tempPath);
            var candidate = ResultsMFile.Parse(tempPath);

            using var withoutTimeLine = new StringReader(Encoding.UTF8.GetString(ResultsMTimeLine.Remove(File.ReadAllBytes(tempPath))));
            var normalizedText = string.Join('\n', ReadLines(withoutTimeLine));
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText)));

            return new Run(candidate, normalizedText, hash);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static IEnumerable<string> ReadLines(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}
