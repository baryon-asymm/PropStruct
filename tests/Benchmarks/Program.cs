using System.Globalization;
using System.Reflection;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;

namespace PropStruct.Benchmarks;

/// <summary>
/// The node's one entry point (API.md, "## Command line"). Two modes share this file:
///
/// - the ordinary one, parsing <c>--formulation</c>/<c>--paths</c>/<c>--repeats</c>/
///   <c>--particles</c> and printing the provenance block and the markdown table;
/// - the hidden <c>--internal-cycle1-run</c> mode, which <see cref="HostThreadMeasurement"/>
///   launches as a child process so it can force <c>DOTNET_PROCESSOR_COUNT</c> before the
///   .NET runtime starts (BOOT.md, "## Host-thread counts"). It is not part of this node's
///   public command line (API.md does not list it) and prints one sentinel-prefixed line
///   instead of the table.
/// </summary>
internal static class Program
{
    private const string InternalRunFlag = "--internal-cycle1-run";
    private const string ResultSentinel = "PROPSTRUCT-BENCHMARK-RESULT ";

    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == InternalRunFlag)
        {
            return RunInternal(args);
        }

        if (BuildConfigurationGuard.IsDebugBuild)
        {
            await Console.Error.WriteLineAsync(
                "propstruct benchmarks: refusing to record a figure from a Debug build " +
                "(BOOT.md, Invariants: \"Every figure is from a Release build\"). Run with " +
                "'dotnet run -c Release --project tests/Benchmarks -- ...'.").ConfigureAwait(false);
            return 1;
        }

        var options = CommandLineOptions.Parse(args, Console.Error);
        if (options is null)
        {
            return 1;
        }

        var accelerators = AcceleratorProbe.Discover();
        var cudaAvailable = accelerators.Any(a => a.Kind == AcceleratorKind.Cuda);

        var commandLine = "dotnet run -c Release --project tests/Benchmarks -- " + string.Join(' ', args);
        var provenance = ProvenanceCollector.Collect(commandLine, accelerators);

        Console.WriteLine("# Provenance");
        Console.WriteLine($"Date (UTC): {provenance.DateUtc}");
        Console.WriteLine($"Machine: {provenance.Machine}");
        Console.WriteLine($".NET: {provenance.DotNetVersion}");
        Console.WriteLine($"Commit: {provenance.Commit}");
        Console.WriteLine($"Command: {provenance.CommandLine}");
        Console.WriteLine($"Particle-count override: {(options.Particles is { } n ? n.ToString(CultureInfo.InvariantCulture) : "none (each formulation's own N)")}");
        Console.WriteLine($"Attempts-per-launch override: {(options.AttemptsPerLaunch is { } b ? b.ToString(CultureInfo.InvariantCulture) : "none (SimulationOptions' own default)")}");
        Console.WriteLine();
        Console.WriteLine("| Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts | Machine | Build | Commit |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|");

        var ownAssemblyPath = Assembly.GetExecutingAssembly().Location;

        foreach (var formulationName in options.Formulations)
        {
            foreach (var path in options.Paths)
            {
                var row = await MeasureAsync(
                    formulationName, path, options.Repeats, options.Particles, options.AttemptsPerLaunch,
                    cudaAvailable, ownAssemblyPath).ConfigureAwait(false);
                Console.WriteLine(row.ToMarkdownRow(provenance));
            }
        }

        return 0;
    }

    private static async Task<FigureRow> MeasureAsync(
        string formulationName, BenchmarkPath path, int repeats, int? particlesOverride, int? attemptsPerLaunchOverride,
        bool cudaAvailable, string ownAssemblyPath)
    {
        try
        {
            var outcome = path switch
            {
                BenchmarkPath.Cpu => new NotAvailable(
                    "the ILGPU CPU accelerator's kernel-launch path is Execution-internal " +
                    "(InternalsVisibleTo Simulation, Cli and their tests only, src/Execution/API.md); " +
                    "Simulation's public SimulationOptions has no field that reaches it " +
                    "(BOOT.md, \"## Escalation: the CPU accelerator oracle path\")"),

                BenchmarkPath.Cuda when !cudaAvailable =>
                    new NotMeasuredOnThisMachine("no CUDA device found by Execution.AcceleratorProbe.Discover"),

                BenchmarkPath.Original =>
                    await MeasureOriginalAsync(formulationName, repeats).ConfigureAwait(false),

                BenchmarkPath.Host1 =>
                    await HostThreadMeasurement.MeasureAsync(
                        formulationName, threads: 1, repeats, particlesOverride, attemptsPerLaunchOverride, ownAssemblyPath).ConfigureAwait(false),

                BenchmarkPath.Host16 =>
                    await HostThreadMeasurement.MeasureAsync(
                        formulationName, threads: 16, repeats, particlesOverride, attemptsPerLaunchOverride, ownAssemblyPath).ConfigureAwait(false),
                BenchmarkPath.Reference => MeasureInProcess(formulationName, path, repeats, particlesOverride, attemptsPerLaunchOverride),
                _ => MeasureInProcess(formulationName, path, repeats, particlesOverride, attemptsPerLaunchOverride),
            };

            return new FigureRow(formulationName, path, outcome);
        }
        catch (LegacyUnavailableException ex)
        {
            return new FigureRow(formulationName, path, new NotMeasuredOnThisMachine(ex.Message));
        }
        catch (Exception ex) when (ex is FormulationFormatException or FileNotFoundException or SimulationFailedException)
        {
            // A malformed or missing formulation, or a run the simulator itself refused, is a
            // per-(formulation, path) problem, not a reason to abort the whole table (every
            // other row still has something useful to say).
            return new FigureRow(formulationName, path, new NotMeasuredOnThisMachine(ex.Message));
        }
    }

    private static Measured MeasureInProcess(
        string formulationName, BenchmarkPath path, int repeats, int? particlesOverride, int? attemptsPerLaunchOverride)
    {
        var formulation = LoadCycle1Formulation(formulationName, particlesOverride);
        var options = new SimulationOptions
        {
            Mode = path == BenchmarkPath.Reference ? ExecutionMode.Reference : ExecutionMode.Batched,
            Accelerator = path switch
            {
                BenchmarkPath.Reference => AcceleratorKind.Cpu, // reference mode is CPU-only (src/Execution/API.md)
                BenchmarkPath.Cuda => AcceleratorKind.Cuda,
                BenchmarkPath.Host1 => AcceleratorKind.Auto,
                BenchmarkPath.Host16 => AcceleratorKind.Auto,
                BenchmarkPath.Cpu => AcceleratorKind.Auto,
                BenchmarkPath.Original => AcceleratorKind.Auto,
                _ => AcceleratorKind.Auto,
            },
        };
        if (attemptsPerLaunchOverride is { } perLaunch)
        {
            options = options with { AttemptsPerLaunch = perLaunch };
        }

        var samples = new List<double>(repeats);
        var warmupIncludedAnyRepeat = false;
        var lastTotalAttempts = 0L;
        var lastTotalLaunches = 0L;
        var lastAttemptsPerLaunch = 0;

        for (var i = 0; i < repeats; i++)
        {
            var measurement = PortRunner.RunCycle1(formulation, options);
            warmupIncludedAnyRepeat |= !measurement.Cycle0IsolationConfirmed;
            samples.Add(measurement.AcceptedParticles / measurement.ElapsedSeconds);
            lastTotalAttempts = measurement.TotalAttempts;
            lastTotalLaunches = measurement.TotalLaunches;
            lastAttemptsPerLaunch = measurement.AttemptsPerLaunch;
        }

        return new Measured(
            Statistics.Mean(samples), Statistics.StandardDeviation(samples), repeats, warmupIncludedAnyRepeat,
            lastAttemptsPerLaunch, lastTotalLaunches, lastTotalAttempts);
    }

    private static async Task<FigureOutcome> MeasureOriginalAsync(string formulationName, int repeats)
    {
        var formulation = LoadRawFormulation(formulationName);
        var samples = new List<double>(repeats);

        for (var i = 0; i < repeats; i++)
        {
            var measurement = await LegacyRunner.RunAsync(formulationName, formulation.Cycles, formulation.ParticlesPerCycle).ConfigureAwait(false);
            samples.Add(measurement.AcceptedParticles / measurement.ElapsedSeconds);
        }

        return new Measured(
            Statistics.Mean(samples), Statistics.StandardDeviation(samples), repeats, WarmupIncluded: true,
            AttemptsPerLaunch: null, Launches: null, Attempts: null);
    }

    internal static Formulation LoadRawFormulation(string formulationName) =>
        DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{formulationName}.dat"));

    internal static Formulation LoadCycle1Formulation(string formulationName, int? particlesOverride)
    {
        var formulation = LoadRawFormulation(formulationName);
        return formulation with
        {
            Cycles = 1,
            ParticlesPerCycle = particlesOverride ?? formulation.ParticlesPerCycle,
        };
    }

    /// <summary>The <c>--internal-cycle1-run &lt;formulation&gt; &lt;particlesOrEmpty&gt;
    /// &lt;attemptsPerLaunchOrEmpty&gt;</c> child-process entry point
    /// <see cref="HostThreadMeasurement"/> launches. Always the batched, CPU-accelerator path:
    /// the only reason to relaunch is to run under a forced <c>DOTNET_PROCESSOR_COUNT</c>
    /// (BOOT.md, "## Host-thread counts").</summary>
    private static int RunInternal(string[] args)
    {
        try
        {
            var formulationName = args[1];
            int? particlesOverride = args.Length > 2 && args[2].Length > 0
                ? int.Parse(args[2], CultureInfo.InvariantCulture)
                : null;
            int? attemptsPerLaunchOverride = args.Length > 3 && args[3].Length > 0
                ? int.Parse(args[3], CultureInfo.InvariantCulture)
                : null;

            var formulation = LoadCycle1Formulation(formulationName, particlesOverride);
            var options = new SimulationOptions { Mode = ExecutionMode.Batched, Accelerator = AcceleratorKind.Cpu };
            if (attemptsPerLaunchOverride is { } perLaunch)
            {
                options = options with { AttemptsPerLaunch = perLaunch };
            }

            var measurement = PortRunner.RunCycle1(formulation, options);

            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{ResultSentinel}{measurement.AcceptedParticles} {measurement.ElapsedSeconds:R} " +
                $"{measurement.Cycle0IsolationConfirmed} {measurement.AttemptsPerLaunch} " +
                $"{measurement.TotalLaunches} {measurement.TotalAttempts}"));
            return 0;
        }
        // The known failure modes of this internal entry point: a malformed invocation
        // (IndexOutOfRangeException from a missing args[1], FormatException/OverflowException
        // from a bad args[2]), a formulation Input itself refuses (API.md's own Errors table:
        // FormulationFormatException, FileNotFoundException, IOException,
        // UnauthorizedAccessException), or a run Simulation itself refuses (API.md's own Errors
        // table: ArgumentException, SimulationFailedException). Anything outside this set is a
        // defect of this tool, not of what it measures, and is left to crash the child process:
        // ParseInternalResult's own contract already treats a missing sentinel line as failure
        // and reports the child's stderr, which the runtime's own unhandled-exception report
        // still reaches (CA1031: no test drives this catch in-process the way
        // tests/Cli.Tests/ExitCodeTests does Program.Dispatch's, so narrowing it costs nothing).
        catch (Exception ex) when (ex is IndexOutOfRangeException or FormatException or OverflowException
            or FormulationFormatException or FileNotFoundException or IOException or UnauthorizedAccessException
            or ArgumentException or SimulationFailedException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>Parses one child process's sentinel line, or returns <see langword="null"/> if
    /// none was found (the child crashed before printing one; the caller reports its stderr
    /// instead). The budget field is now <c>Measurement.AttemptsPerLaunch</c> itself
    /// (`PortRunner.cs`'s own doc comment, 2026-09-28): the *effective* budget
    /// <c>SimulationResult.Diagnostics</c> reports for the child's call, not merely the option
    /// this node passed, so a wrapper field of its own is no longer needed here.</summary>
    internal readonly record struct ParsedInternalResult(PortRunner.Cycle1Measurement Measurement);

    internal static ParsedInternalResult? ParseInternalResult(string stdout)
    {
        var line = stdout
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .FirstOrDefault(l => l.StartsWith(ResultSentinel, StringComparison.Ordinal));

        if (line is null)
        {
            return null;
        }

        var fields = line[ResultSentinel.Length..].Split(' ');
        var accepted = long.Parse(fields[0], CultureInfo.InvariantCulture);
        var elapsed = double.Parse(fields[1], CultureInfo.InvariantCulture);
        var isolated = bool.Parse(fields[2]);
        var attemptsPerLaunch = int.Parse(fields[3], CultureInfo.InvariantCulture);
        var totalLaunches = long.Parse(fields[4], CultureInfo.InvariantCulture);
        var totalAttempts = long.Parse(fields[5], CultureInfo.InvariantCulture);
        return new ParsedInternalResult(
            new PortRunner.Cycle1Measurement(accepted, elapsed, isolated, totalAttempts, totalLaunches, attemptsPerLaunch));
    }

    internal static string InternalRunFlagName => InternalRunFlag;
}
