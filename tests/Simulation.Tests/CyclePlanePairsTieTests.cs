using System.Text;
using System.Text.Json;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The tie of the cycle-plane pairs (`tests/Fixtures/API.md`, "## Cycle-plane pairs"): the
/// port's stored output of each pair is what the port prints now, so the ratchet
/// `tests/Statistics.Tests/PathIdenticalPairsReportTests` reads is not stale. The run is the one
/// `tests/Fixtures/cycle_plane_pairs.py` ran through `propstruct run`: reference mode, the
/// `Original` layout, seed 1, `Original` precision, the shipped `.dat` with its `N` and `KXX`
/// and, where the pair says so, its pocket-forming flags; every parameter is read from
/// `cycle-plane-pairs/provenance.json`, none is typed here. Compared as text, the time line
/// aside. A Long test, like the seed-0 snapshot it follows: it runs whole simulations.
/// </summary>
public class CyclePlanePairsTieTests
{
    private static string PairsRoot => RepositoryPaths.Resolve("tests", "Fixtures", "cycle-plane-pairs");

    public static IEnumerable<object[]> Pairs()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(PairsRoot, "provenance.json")));
        return [.. document.RootElement.EnumerateArray()
            .Where(entry => entry.GetProperty("side").GetString() == "port")
            .Select(entry => new object[] { entry.GetProperty("formulation").GetString()! })];
    }

    private static string Normalized(string path) =>
        Encoding.UTF8.GetString(ResultsMTimeLine.Remove(File.ReadAllBytes(path))).Replace("\r\n", "\n", StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(Pairs))]
    [Trait("Category", "Long")]
    public void TheStoredPortOutputIsWhatThePortPrintsNow(string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(PairsRoot, "provenance.json")));
        var entry = document.RootElement.EnumerateArray()
            .Single(e => e.GetProperty("side").GetString() == "port" && e.GetProperty("formulation").GetString() == name);
        Assert.Equal("original", entry.GetProperty("layout").GetString());
        var readsFlags = entry.GetProperty("readsPocketFormingFractions").GetBoolean();

        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath, readsFlags) with
        {
            ParticlesPerCycle = entry.GetProperty("n").GetInt32(),
            Cycles = entry.GetProperty("kxx").GetInt32(),
        };
        var parameters = ModelParameters.Default with { ReadPocketFormingFractions = readsFlags };

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Precision = PrecisionKind.Original,
            Seed = entry.GetProperty("seed").GetUInt64(),
        });
        var result = simulator.Run(formulation);

        var temporaryPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, parameters, result, temporaryPath);
            var live = Normalized(temporaryPath);
            var stored = Normalized(Path.Combine(PairsRoot, entry.GetProperty("output").GetString()!));
            Assert.True(live == stored,
                $"{name}: the stored port output no longer equals the port's own; regenerate the pair with `python tests/Fixtures/cycle_plane_pairs.py generate` in the commit that moved the plane, with the approved list of tests/Statistics.Tests.");
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
