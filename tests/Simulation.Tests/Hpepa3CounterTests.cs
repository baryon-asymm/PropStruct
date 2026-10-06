using System.Diagnostics;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L2 of the BOOT.md table: "reference mode on HPEPA3 reproduces the reference's printed counters"
/// (<c>src/Simulation/BOOT.md</c>, "Design decisions (2026-09-18)", "Counters are the first integration
/// evidence"). Runs the whole formulation (both cycles, 200,000 accepted particles), so it is
/// <c>Category=Long</c>. The figures it prints are recorded in <c>src/Simulation/BOOT.md</c>, "## Counter check".
/// </summary>
public class Hpepa3CounterTests(ITestOutputHelper output)
{
    // "Within a few per cent": the Original layout reproduces the original's streams and REAL*4 storage, but the
    // sample path parts from the original's at the first REAL*4-sensitive branch (root BOOT.md, "## Purpose"), so
    // the two runs drift apart and a large count is compared by a relative band.
    private const double RelativeTolerance = 0.05;

    // A small count is compared by its own Poisson spread instead: the difference of two independent counts
    // has the standard deviation sqrt(reference + port). Four of them is the two-sided normal quantile of
    // 6.7e-5, the root criterion's family-wise alpha = 1e-3 shared over the 15 quantities compared here
    // (root BOOT.md, "Statistical reference criterion"). A reference with 0 events therefore demands 0 events.
    private const double CountSigmas = 4.0;

    [Fact]
    [Trait("Category", "Long")]
    public void ReferenceModeOnHpepa3ReproducesTheReferencesPrintedCounters()
    {
        var formulation = DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "HPEPA3.dat"));
        var reference = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "references", "HPEPA3", "results.m.txt"));

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Seed = 0UL,
        });

        var stopwatch = Stopwatch.StartNew();
        var result = simulator.Run(formulation);
        stopwatch.Stop();

        // The original's denominators (Fortran lines 1248-1256): FI counts the accepted particles of cycles >= 1;
        // conditions 1-5 are printed per FI + N (every accepted particle, the warm-up included), conditions 6-9
        // and the bridges per particle per FI alone.
        var fi = (long)formulation.Cycles * formulation.ParticlesPerCycle;
        var fiPlusN = fi + formulation.ParticlesPerCycle;

        output.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds:F2} s for {fiPlusN:N0} accepted particles " +
                         $"({fiPlusN / stopwatch.Elapsed.TotalSeconds:N0} particles/s); " +
                         $"attempts={result.Diagnostics.TotalAttempts:N0}, launches={result.Diagnostics.TotalLaunches:N0}");

        AssertCount("NFX", reference["NFX"][0], 1, result.Counters.Nfx);
        AssertCount("NFY", reference["NFY"][0], 1, result.Counters.Nfy);
        AssertCount("NFQ", reference["NFQ"][0], 1, result.Counters.Nfq);
        AssertCount("NFW", reference["NFW"][0], 1, result.Counters.Nfw);
        AssertCount("Nkarm", reference["Nkarm"][0], 1, result.Counters.Qkss);

        for (var i = 0; i < result.Counters.Conditions.Length; i++)
        {
            var name = $"ConditionBreaking({i + 1})";
            AssertCount(name, reference[name][0], i < 5 ? fiPlusN : fi, result.Counters.Conditions[i]);
        }

        AssertCount("MediumNumberOfBridges", reference["MediumNumberOfBridges"][0], fi, result.Counters.IbridgeTotal);
    }

    /// <summary>
    /// Compares the port's integer count with the reference's printed value times the original's own
    /// denominator, within the larger of <see cref="RelativeTolerance"/> and <see cref="CountSigmas"/>.
    /// </summary>
    private void AssertCount(string name, double printed, long denominator, long portCount)
    {
        var referenceCount = Math.Round(printed * denominator);
        var difference = Math.Abs(referenceCount - portCount);
        var bound = Math.Max(RelativeTolerance * referenceCount, CountSigmas * Math.Sqrt(referenceCount + portCount));
        var line = $"{name}: reference={printed:G7} ({referenceCount:F0} events), " +
                   $"port={(double)portCount / denominator:G7} ({portCount} events), " +
                   $"difference={difference:F0}, bound={bound:F1}";
        output.WriteLine(line);
        Assert.True(difference <= bound, line);
    }
}
