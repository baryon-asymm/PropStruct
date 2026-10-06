using System.Globalization;
using System.Numerics;
using System.Text.Json;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// Cross-checks the seed arithmetic <c>tests/Fixtures</c>' Python scripts (<c>generate.py</c>,
/// <c>run_original.py</c>) use to build every replica fixture and every measurement-script run against
/// <c>src/Random</c>'s own arithmetic, reached only through its published contract
/// (<see cref="OriginalSeeds"/>, <see cref="IndependentSeeds"/>, <see cref="Mcg128.Advance"/> —
/// never a second implementation of the jump formula here, root BOOT.md's Taboos). Nothing here runs
/// <c>PropStructV3.exe</c>: both scripts' <c>print-states</c> modes are pure seed arithmetic printed as
/// JSON (<c>tests/Fixtures/API.md</c>, "## generate.py", "## Measurement scripts").
/// </summary>
public class PythonSeedArithmeticCrossCheckTests
{
    // root BOOT.md, "Statistical reference criterion": the largest replica count of any reference
    // formulation is HPEPA3's R=32; every other formulation's R (16) is a subset of 1..32.
    private const int MaxReplicaCount = 32;

    [Fact]
    public void BaseStatesOfBothLayoutsMatchThePythonScript()
    {
        // The positive control this cross-check is proven right on (task brief, "Proof twice"): the
        // "lagged" base states are original_stream_value(1..6), which SourceLimbsTests already proves
        // equal the Fortran source's own packed seeds (lines 52-63). Matching them here shows the Python
        // arithmetic reproduces the Fortran seeds too, through the Python path.
        using var lagged = RunGeneratePrintStates("lagged");
        AssertBaseMatches(lagged, OriginalSeeds.Streams);

        using var independent = RunGeneratePrintStates("independent");
        AssertBaseMatches(independent, IndependentSeeds.Streams);
    }

    [Fact]
    public void LaggedReplicaStatesForEveryKTheFixturesUseMatchThePythonScript()
    {
        var ks = AllReplicaKs();
        using var doc = RunGeneratePrintStates("lagged", ks);
        AssertReplicasMatch(doc, ks, OriginalSeeds.Streams);
    }

    [Fact]
    public void IndependentReplicaStatesForEveryKTheFixturesUseMatchThePythonScript()
    {
        var ks = AllReplicaKs();
        using var doc = RunGeneratePrintStates("independent", ks);
        AssertReplicasMatch(doc, ks, IndependentSeeds.Streams);
    }

    [Fact]
    public void RunOriginalStatesForTheTreesSeedSpreadMatchThePythonScript()
    {
        var seeds = SeedSpread();

        using var originalDoc = RunOriginalPrintStates("original", seeds);
        using var independentDoc = RunOriginalPrintStates("independent", seeds);

        foreach (var seed in seeds)
        {
            AssertParticleStateMatches(originalDoc, seed, OriginalSeeds.ForParticle(seed, 0));
            AssertParticleStateMatches(independentDoc, seed, IndependentSeeds.ForParticle(seed, 0));
        }
    }

    [Fact]
    public void EveryProvenanceReplicaJumpMatchesTheFormula()
    {
        // AGENTS.md §6: a criterion quantified by "all" is checked against a list the machine generates
        // from the real data, not a typed-in one — every lagged/independent entry provenance.json actually
        // holds, not a hand-picked sample.
        var provenancePath = RepositoryPaths.Resolve("tests", "Fixtures", "provenance.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(provenancePath));

        var checkedCount = 0;
        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("layout", out var layoutProperty))
            {
                continue;
            }

            var layout = layoutProperty.GetString();
            if (layout is not "lagged" and not "independent")
            {
                continue;
            }

            var k = entry.GetProperty("k").GetInt32();
            var expectedJump = ReplicaJumpExponent(k);
            var actualJump = BigInteger.Parse(entry.GetProperty("jump").GetString()!, CultureInfo.InvariantCulture);

            Assert.Equal(expectedJump, actualJump);
            checkedCount++;
        }

        Assert.True(checkedCount > 0, "no lagged/independent provenance entries were found to check");
    }

    private static int[] AllReplicaKs()
    {
        var ks = new int[MaxReplicaCount];
        for (var i = 0; i < MaxReplicaCount; i++)
        {
            ks[i] = i + 1;
        }

        return ks;
    }

    private static ulong[] SeedSpread()
    {
        // root BOOT.md's rate table (seedIndex << 16, seedIndex = 0..15, "Statistical reference
        // criterion") and the batched-mode two-sample setup (k << 17, eight seeds, the criterion's link 3),
        // plus a handful of small seeds.
        var seeds = new SortedSet<ulong>();
        for (ulong s = 0; s <= 4; s++)
        {
            _ = seeds.Add(s);
        }

        for (ulong seedIndex = 0; seedIndex < 16; seedIndex++)
        {
            _ = seeds.Add(seedIndex << 16);
        }

        for (ulong k = 0; k < 8; k++)
        {
            _ = seeds.Add(k << 17);
        }

        return [.. seeds];
    }

    private static BigInteger ReplicaJumpExponent(int k) =>
        (BigInteger)k * (BigInteger.One << 80) + (BigInteger.One << 79);

    private static void AssertBaseMatches(JsonDocument doc, StreamSet expected)
    {
        var baseStates = doc.RootElement.GetProperty("base");
        AssertStreamMatches(baseStates, "1", expected.S1);
        AssertStreamMatches(baseStates, "2", expected.S2);
        AssertStreamMatches(baseStates, "3", expected.S3);
        AssertStreamMatches(baseStates, "4", expected.S4);
        AssertStreamMatches(baseStates, "5", expected.S5);
        AssertStreamMatches(baseStates, "6", expected.S6);
    }

    private static void AssertReplicasMatch(JsonDocument doc, IReadOnlyList<int> ks, StreamSet baseStreams)
    {
        var replicas = doc.RootElement.GetProperty("replicas");
        foreach (var k in ks)
        {
            var exponent = ReplicaJumpExponent(k);
            var (kLow, kHigh) = Big128.Split(exponent);
            var replicaStates = replicas.GetProperty(k.ToString(CultureInfo.InvariantCulture));

            AssertStreamMatches(replicaStates, "1", Mcg128.Advance(baseStreams.S1, kLow, kHigh));
            AssertStreamMatches(replicaStates, "2", Mcg128.Advance(baseStreams.S2, kLow, kHigh));
            AssertStreamMatches(replicaStates, "3", Mcg128.Advance(baseStreams.S3, kLow, kHigh));
            AssertStreamMatches(replicaStates, "4", Mcg128.Advance(baseStreams.S4, kLow, kHigh));
            AssertStreamMatches(replicaStates, "5", Mcg128.Advance(baseStreams.S5, kLow, kHigh));
            AssertStreamMatches(replicaStates, "6", Mcg128.Advance(baseStreams.S6, kLow, kHigh));
        }
    }

    private static void AssertParticleStateMatches(JsonDocument doc, ulong seed, StreamSet expected)
    {
        var states = doc.RootElement.GetProperty("states").GetProperty(seed.ToString(CultureInfo.InvariantCulture));
        AssertStreamMatches(states, "1", expected.S1);
        AssertStreamMatches(states, "2", expected.S2);
        AssertStreamMatches(states, "3", expected.S3);
        AssertStreamMatches(states, "4", expected.S4);
        AssertStreamMatches(states, "5", expected.S5);
        AssertStreamMatches(states, "6", expected.S6);
    }

    private static void AssertStreamMatches(JsonElement streamStates, string streamNumber, Mcg128State actual)
    {
        var expected = BigInteger.Parse(
            streamStates.GetProperty(streamNumber).GetString()!, CultureInfo.InvariantCulture);
        Assert.Equal(expected, Big128.Combine(actual));
    }

    private static JsonDocument RunGeneratePrintStates(string layout, IReadOnlyList<int>? ks = null)
    {
        var arguments = new List<string> { "print-states", "--layout", layout };
        if (ks is { Count: > 0 })
        {
            arguments.Add("--ks");
            foreach (var k in ks)
            {
                arguments.Add(k.ToString(CultureInfo.InvariantCulture));
            }
        }

        return RunPython(RepositoryPaths.Resolve("tests", "Fixtures", "generate.py"), arguments);
    }

    private static JsonDocument RunOriginalPrintStates(string layout, IReadOnlyList<ulong> seeds)
    {
        var arguments = new List<string> { "--layout", layout, "--print-states", "--seeds" };
        foreach (var seed in seeds)
        {
            arguments.Add(seed.ToString(CultureInfo.InvariantCulture));
        }

        return RunPython(RepositoryPaths.Resolve("tests", "Fixtures", "run_original.py"), arguments);
    }

    private static JsonDocument RunPython(string scriptPath, IReadOnlyList<string> arguments)
    {
        var run = PythonScript.Run(Path.GetDirectoryName(scriptPath)!, Path.GetFileName(scriptPath), arguments);

        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"python {scriptPath} {string.Join(' ', arguments)} exited {run.ExitCode}: {run.StandardError}");
        }

        return JsonDocument.Parse(run.StandardOutput);
    }
}
