using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using PropStruct.Execution;
using PropStruct.Output;
using PropStruct.Tests.Harness;

namespace PropStruct.Simulation.Tests;

/// <summary>One run of one arm: the parsed <c>results.m</c> cells, or the status that ended it.</summary>
internal sealed record MeasuredRun(ulong Seed, string Status, double ElapsedSeconds, IReadOnlyDictionary<string, double[]> Cells)
{
    /// <summary>The status of a run that completed.</summary>
    public const string Completed = "Ok";

    /// <summary>Whether the run completed and so printed cells.</summary>
    public bool IsCompleted => string.Equals(Status, Completed, StringComparison.Ordinal);
}

/// <summary>
/// Every run of one arm on one case, in seed order, with the fingerprint of the code that made them.
/// </summary>
internal sealed record ArmRuns(
    string Fingerprint,
    string Case,
    string Arm,
    string Mode,
    IReadOnlyList<ulong> Seeds,
    IReadOnlyList<MeasuredRun> Runs)
{
    /// <summary>The runs that completed.</summary>
    public IEnumerable<MeasuredRun> Completed => Runs.Where(run => run.IsCompleted);

    /// <summary>The seeds' wall time added up (reference runs overlap in time, so this is work, not elapsed time).</summary>
    public double TotalSeconds => Runs.Sum(run => run.ElapsedSeconds);
}

/// <summary>
/// Runs an arm of a case over its seeds and keeps the result on disk, so that a measurement too long for one
/// command is made in slices and a later slice, or the analysis, reads what an earlier one wrote. The directory
/// is <c>PROPSTRUCT_MEASUREMENT_CACHE</c>, else <c>Measurements/cache</c> of this node, which its <c>.gitignore</c> keeps out of the repository; a file
/// is read back only when its fingerprint (the module version ids of the assemblies the run executes, which differ
/// between builds and configurations) and its seed list are those of the request, else the arm is run again.
/// </summary>
internal static class MeasuredRunStore
{
    private const string DirectoryVariable = "PROPSTRUCT_MEASUREMENT_CACHE";

    private static readonly JsonSerializerOptions Json = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>The runs of <paramref name="arm"/> on <paramref name="measurementCase"/> in <paramref name="mode"/>, from disk or freshly run and written.</summary>
    public static ArmRuns Ensure(MeasurementCase measurementCase, CorrectionArm arm, ExecutionMode mode, IReadOnlyList<ulong> seeds)
    {
        var path = PathOf(measurementCase, arm, mode);
        if (File.Exists(path))
        {
            var stored = JsonSerializer.Deserialize<ArmRuns>(File.ReadAllText(path), Json);
            if (stored is not null
                && string.Equals(stored.Fingerprint, Fingerprint(), StringComparison.Ordinal)
                && stored.Seeds.SequenceEqual(seeds))
            {
                return stored;
            }
        }

        var runs = Run(measurementCase, arm, mode, seeds);
        var written = new ArmRuns(Fingerprint(), measurementCase.Name, arm.Name, mode.ToString(), seeds, runs);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(written, Json));
        return written;
    }

    private static MeasuredRun[] Run(MeasurementCase measurementCase, CorrectionArm arm, ExecutionMode mode, IReadOnlyList<ulong> seeds)
    {
        var runs = new MeasuredRun[seeds.Count];

        // Batched runs already use every host thread; reference runs are single-threaded and overlap, as link 3's do.
        if (mode == ExecutionMode.Reference)
        {
            _ = Parallel.For(0, seeds.Count, k => runs[k] = RunOne(measurementCase, arm, mode, seeds[k]));
        }
        else
        {
            for (var k = 0; k < seeds.Count; k++)
            {
                runs[k] = RunOne(measurementCase, arm, mode, seeds[k]);
            }
        }

        return runs;
    }

    private static MeasuredRun RunOne(MeasurementCase measurementCase, CorrectionArm arm, ExecutionMode mode, ulong seed)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var simulator = Simulator.Create(arm.Options(measurementCase.Parameters, mode, seed));
            var result = simulator.Run(measurementCase.Formulation);
            var elapsed = stopwatch.Elapsed.TotalSeconds;

            var tempPath = Path.GetTempFileName();
            try
            {
                ResultsMWriter.Write(measurementCase.Formulation, measurementCase.Parameters, result, tempPath);
                return new MeasuredRun(seed, MeasuredRun.Completed, elapsed, ResultsMFile.Parse(tempPath));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
        catch (SimulationFailedException exception)
        {
            return new MeasuredRun(seed, exception.Status.ToString(), stopwatch.Elapsed.TotalSeconds, new Dictionary<string, double[]>());
        }
    }

    private static string PathOf(MeasurementCase measurementCase, CorrectionArm arm, ExecutionMode mode)
    {
        var root = Environment.GetEnvironmentVariable(DirectoryVariable);
        if (string.IsNullOrEmpty(root))
        {
            root = RepositoryPaths.Resolve("tests", "Simulation.Tests", "Measurements", "cache");
        }

        return Path.Combine(root, measurementCase.Name, $"{mode}-{arm.Name}.json");
    }

    private static string Fingerprint() => string.Join(
        '-',
        new[]
        {
            typeof(Simulator), typeof(Particle.CorrectionSet), typeof(Statistics.Setup), typeof(AcceleratorKind),
            typeof(Input.Formulation), typeof(ResultsMWriter),
        }.Select(type => type.Assembly.ManifestModule.ModuleVersionId.ToString("N")));
}
