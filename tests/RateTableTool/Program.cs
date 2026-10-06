using System.Diagnostics;
using System.Globalization;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;

namespace PropStruct.RateTableTool;

/// <summary>
/// The node's one entry point (API.md, "## Command line"). Two subcommands, each writing one committed
/// fixture from reference-mode runs through <see cref="ReferenceModeRunner"/> — the same runner, so neither
/// is a second implementation of "run this seed and parse the result" (root BOOT.md Taboos):
///
/// - <c>rate-table</c> writes <c>tests/Fixtures/rate-table.json</c> (320 runs: five formulations, both
///   layouts, both precision kinds, sixteen seeds each; <c>tests/Fixtures/API.md</c>, "## Rate table");
/// - <c>seed-zero-snapshot</c> writes
///   <c>tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt</c> (20 runs: five formulations, both
///   layouts, both precision kinds, seed 0; <c>tests/Simulation.Tests/BOOT.md</c>, the seed-0 ratchet).
///
/// Both used to be <c>[Fact(Skip = "manual regeneration only")]</c> tests, one in
/// <c>tests/Simulation.Tests/RateTableGenerator</c> and the other in
/// <c>tests/Simulation.Tests/StatisticalCriterionTests</c>. AGENTS.md §13 forbids a permanently skipped
/// check ("a perpetually red check is worse than an absent one"); a generator that never runs under
/// <c>dotnet test</c> reads the same way to anyone scanning test output, and neither one asserted anything
/// in the first place. Moved here, run by hand, exactly like <c>tests/Benchmarks</c> (BOOT.md, Constraints).
/// </summary>
internal static class Program
{
    private static readonly string[] Formulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];
    private static readonly StreamLayout[] Layouts = [StreamLayout.Original, StreamLayout.Independent];
    private static readonly PrecisionKind[] Precisions = [PrecisionKind.Original, PrecisionKind.Binary64];

    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return Usage();
        }

        switch (args[0])
        {
            case "rate-table":
                var parallelism = Environment.ProcessorCount;
                if (args.Length > 1 && args[1] == "--parallelism" && args.Length > 2)
                {
                    parallelism = int.Parse(args[2], CultureInfo.InvariantCulture);
                }

                return RegenerateRateTable(parallelism);

            case "seed-zero-snapshot":
                return RegenerateSeedZeroSnapshot();

            default:
                return Usage();
        }
    }

    private static int Usage()
    {
        Console.Error.WriteLine(
            "usage: dotnet run --project tests/RateTableTool -- rate-table [--parallelism N]\n" +
            "       dotnet run --project tests/RateTableTool -- seed-zero-snapshot");
        return 1;
    }

    /// <summary>
    /// Writes <c>tests/Fixtures/rate-table.json</c> (root BOOT.md, "the pass condition compares failure rates,
    /// not single runs"; <c>tests/Fixtures/BOOT.md</c>, "## Rate table"): reference mode, sixteen seeds
    /// <c>k = 0..15</c>, <c>seed = k &lt;&lt; 16</c>, per formulation and layout, under both
    /// <see cref="PrecisionKind.Original"/> (the pass condition's own data) and <see cref="PrecisionKind.Binary64"/>
    /// (the positive control) — 5 * 2 * 16 * 2 = 320 runs.
    ///
    /// A run "fails" by the same rule <c>tests/Harness.Tests/NullRateCalibration</c> uses for the original's own
    /// null rate: any of <see cref="StatisticalCriterion.Compare"/>, <see cref="StatisticalCriterion.CompareTailRowMean"/>
    /// or <see cref="StatisticalCriterion.CompareAdaptiveIndexMatched"/> reports a failing cell for it, against all
    /// <c>R</c> replicas of its own layout (never leave-one-out: a port run is not a member of the replica set).
    ///
    /// Each of the 320 jobs runs on its own <see cref="Parallel.For(int, int, Action{int})"/> worker; a job that
    /// throws one of Input's/Simulation's own documented failure modes is recorded as a failed job rather than
    /// aborting the whole batch and, under a background thread, the whole process — the failure mode a prior
    /// run of this generator hit five times as a <c>dotnet test</c> host crash (this node's own BOOT.md,
    /// "## Regeneration runs").
    /// </summary>
    private static int RegenerateRateTable(int parallelism)
    {
        const int seedCount = 16;
        var jobs = new List<(string Formulation, StreamLayout Layout, PrecisionKind Precision, int SeedIndex)>();
        foreach (var formulation in Formulations)
        {
            foreach (var layout in Layouts)
            {
                foreach (var precision in Precisions)
                {
                    for (var k = 0; k < seedCount; k++)
                    {
                        jobs.Add((formulation, layout, precision, k));
                    }
                }
            }
        }

        var runs = new RateTableRun?[jobs.Count];
        var normalizedTexts = new string?[jobs.Count];
        var errors = new string?[jobs.Count];
        var stopwatch = Stopwatch.StartNew();

        _ = Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, i =>
        {
            var (name, layout, precision, seedIndex) = jobs[i];
            try
            {
                var seed = (ulong)seedIndex << 16;
                var formulation = ReadFormulation(name);
                var replicaKind = layout == StreamLayout.Original ? ReplicaKind.Lagged : ReplicaKind.Independent;

                var run = ReferenceModeRunner.RunSeed(formulation, layout, precision, seed);

                var failures = new List<string>();
                failures.AddRange(StatisticalCriterion.Compare(name, run.Candidate, replicaKind).Failures.Select(f => $"{f.Quantity}[{f.Index}]"));
                failures.AddRange(StatisticalCriterion.CompareTailRowMean(name, run.Candidate, replicaKind).Failures.Select(f => $"{f.Quantity}[{f.Index}]"));
                failures.AddRange(StatisticalCriterion.CompareAdaptiveIndexMatched(name, run.Candidate, replicaKind).Failures.Select(f => $"{f.Quantity}[{f.Index}]"));

                runs[i] = new RateTableRun(name, layout.ToString(), precision.ToString(), seedIndex, seed, failures.Count, failures, run.Sha256Hex);
                normalizedTexts[i] = run.NormalizedText;
            }
            catch (Exception ex) when (ex is FormulationFormatException or FileNotFoundException or IOException or SimulationFailedException)
            {
                errors[i] = $"{name} {layout} {precision} seed-index {seedIndex}: {ex.GetType().Name}: {ex.Message}";
            }
        });
        stopwatch.Stop();

        var failedJobs = errors.Where(e => e is not null).ToList();
        if (failedJobs.Count > 0)
        {
            Console.Error.WriteLine($"{failedJobs.Count} of {jobs.Count} jobs failed:");
            foreach (var error in failedJobs)
            {
                Console.Error.WriteLine($"  {error}");
            }

            return 1;
        }

        Console.WriteLine($"{jobs.Count} runs in {stopwatch.Elapsed} ({runs.Count(r => r!.FailingCells > 0)} failing), parallelism {parallelism}.");

        // `tests/Fixtures/BOOT.md`, "## Rate table" (rate-runs): the 320 runs' own results.m text, stored so
        // `CompareSets` (`tests/Harness/BOOT.md`, "## Set comparison") has candidates to load without re-running
        // the simulator — written only once every job above has succeeded, after first clearing the directory,
        // so a failed regeneration never leaves the tree with a stale, partial set of runs.
        var rateRunsDirectory = RepositoryPaths.Resolve("tests", "Fixtures", "rate-runs");
        if (Directory.Exists(rateRunsDirectory))
        {
            Directory.Delete(rateRunsDirectory, recursive: true);
        }

        for (var i = 0; i < jobs.Count; i++)
        {
            var (name, layout, precision, seedIndex) = jobs[i];
            var jobDirectory = Path.Combine(rateRunsDirectory, name, layout.ToString(), precision.ToString());
            _ = Directory.CreateDirectory(jobDirectory);
            File.WriteAllText(Path.Combine(jobDirectory, $"{seedIndex}.m.txt"), normalizedTexts[i]!);
        }

        Console.WriteLine($"wrote {jobs.Count} files under {rateRunsDirectory}");

        var criterionSha256 = CriterionSourceDigest();
        var snapshotPath = RepositoryPaths.Resolve("tests", "Simulation.Tests", "Snapshots", "SeedZeroResultsM.approved.txt");
        var snapshotSha256 = Sha256OfText(snapshotPath);
        var fixturesSha256 = FixturesSourceDigest();
        var commit = TryGitCommit() ?? "unknown";

        var table = new RateTable(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Commit: commit,
            CriterionSha256: criterionSha256,
            SnapshotSha256: snapshotSha256,
            FixturesSha256: fixturesSha256,
            SeedStride: "seed = seedIndex << 16, seedIndex = 0..15",
            Runs: [.. runs.Select(r => r!).OrderBy(r => r.Formulation, StringComparer.Ordinal).ThenBy(r => r.Layout, StringComparer.Ordinal)
                .ThenBy(r => r.Precision, StringComparer.Ordinal).ThenBy(r => r.SeedIndex)]);

        var tablePath = RepositoryPaths.Resolve("tests", "Fixtures", "rate-table.json");
        table.Save(tablePath);
        Console.WriteLine($"wrote {tablePath}");
        return 0;
    }

    /// <summary>
    /// Writes <c>tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt</c> (root BOOT.md, "the pass
    /// condition compares failure rates, not single runs": "the seed-0 ratchet of known cells is replaced ...
    /// by an exact snapshot of the seed-0 <c>results.m</c> under both kinds"): twenty cases, five formulations,
    /// both layouts, both precision kinds, seed 0 — the file
    /// <c>tests/Simulation.Tests/StatisticalCriterionTests.SeedZeroResultsMMatchesApprovedSnapshot</c> compares
    /// against on every test run.
    /// </summary>
    private static int RegenerateSeedZeroSnapshot()
    {
        var lines = new List<string> { "# Generated by tests/RateTableTool (ReferenceModeRunner.RunSeed)." };
        foreach (var name in Formulations)
        {
            foreach (var layout in Layouts)
            {
                foreach (var precision in Precisions)
                {
                    var formulation = ReadFormulation(name);
                    var run = ReferenceModeRunner.RunSeed(formulation, layout, precision, seed: 0UL);
                    lines.Add($"{name} {layout} {precision} {run.Sha256Hex}");
                }
            }
        }

        var snapshotPath = RepositoryPaths.Resolve("tests", "Simulation.Tests", "Snapshots", "SeedZeroResultsM.approved.txt");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        File.WriteAllLines(snapshotPath, lines);
        Console.WriteLine($"wrote {snapshotPath}");
        return 0;
    }

    private static Formulation ReadFormulation(string name) =>
        DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat"));

    /// <summary>The commit the table was generated against, best-effort: informational provenance only, never
    /// part of the tie check <c>RateCriterionTests</c> runs (that reads only
    /// <see cref="RateTable.CriterionSha256"/>/<see cref="RateTable.SnapshotSha256"/>, both reproducible from the
    /// working tree with no git dependency) — so a missing git executable degrades this field, not the table's
    /// own usability.</summary>
    private static string? TryGitCommit()
    {
        try
        {
            var startInfo = new ProcessStartInfo("git", "rev-parse HEAD")
            {
                WorkingDirectory = RepositoryPaths.Root,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && output.Length > 0 ? output : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }

    // The same line-ending-normalized hash RateCriterionTests' tie check computes: the content, not the
    // working copy's CRLF or LF, is what the table is tied to.
    private static string Sha256OfText(string path) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal))));

    // tests/Harness.Tests/RateCriterionTests's own twin (that node's own comment names the relationship, and
    // carries the identical exclusion list and its reasons): the criterion is no longer one file, or one file
    // family (audit finding R3, 2026-09-25 — StatisticalCriterion.cs split by concern into a partial class;
    // 2026-09-26 — split further into named single-responsibility classes). tests/Fixtures/BOOT.md, "## Rate
    // table" owns the rule: every *.cs file directly under tests/Harness, minus the exclusion list below (each
    // excluded because no comparison this criterion runs ever reaches it), hashed as one digest in a fixed
    // (ordinal-by-filename) order, each file's own name folded in so the order and the membership are both
    // guarded, not only the bytes.
    private static readonly string[] ExcludedFromCriterionDigest =
    [
        "CpuHost.cs", // the ILGPU CPU-accelerator host: used by kernel-path tests, not by any comparison.
        "BitSnapshot.cs", // a bit-snapshot helper for Random.Tests/Particle.Tests, not read by any comparison.
        "PythonScript.cs", // runs the tree's Python scripts from the generator tests, not read by any comparison.
        "SplitMix64.cs", // test-only deterministic randomness, not the port's own generator, not read by any comparison.
        "DispersionTable.cs", // the dispersion-table report generator; Compute() is called by DispersionApprovedTests
                              // and tests/DispersionTool, never by a comparison.
        "SetComparison.cs", // CompareSets's own pipeline: no per-run verdict (Compare/CompareTailRowMean/
        "CanonicalAxisSetCells.cs", // CompareAdaptiveIndexMatched) ever reaches it — a set-vs-set comparison is a
        "SetCriterionReport.cs", // different question, read by SetCriterionTests directly, never by a rate run.
        "ResultsMTimeLine.cs", // the cut of a results.m's time line for byte comparisons of two files, never read by a
                               // comparison of this criterion, which reads the parsed cells (2026-10-04).
        "CudaRequirement.cs", // the switch that fails a CUDA fact where CUDA is refused, not read by any comparison.
        "CudaRefusalBranches.cs", // the text check that finds those facts in a test node's sources, not read by any comparison.
    ];

    private static string CriterionSourceDigest()
    {
        var harnessDirectory = RepositoryPaths.Resolve("tests", "Harness");
        var files = Directory.GetFiles(harnessDirectory, "*.cs")
            .Where(path => !ExcludedFromCriterionDigest.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal);

        var combined = new System.Text.StringBuilder();
        foreach (var path in files)
        {
            _ = combined.Append(Path.GetFileName(path)).Append('\0');
            _ = combined.Append(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)).Append('\0');
        }

        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined.ToString())));
    }

    // tests/Fixtures/BOOT.md, "## Rate table": the ReplicaKind values the rate runs' own comparisons ever pass —
    // RegenerateRateTable above maps StreamLayout.Original to ReplicaKind.Lagged and StreamLayout.Independent to
    // ReplicaKind.Independent, and never constructs ReplicaKind.Gsv3. tests/Harness.Tests/NullRateCalibration
    // (the original's own null rate this table's rate criterion is held against) is restricted the same way, and
    // itself throws ArgumentOutOfRangeException for Gsv3 — so replicas-gsv3/ carries no rows into FixturesSha256:
    // hashing its 96-per-formulation files would watch data no comparison here ever reads.
    private static readonly ReplicaKind[] ReplicaKindsUsedByRateRuns = [ReplicaKind.Lagged, ReplicaKind.Independent];

    // FixtureReplicas.ReplicaDirectory's own rule (tests/Harness, internal, not reachable from this project — no
    // InternalsVisibleTo grants it, and tests/Harness is out of this task's scope), restated independently here
    // exactly as tests/Harness.Tests/NullRateCalibration already restates it for the same two kinds
    // (tests/Fixtures/BOOT.md, "## Rate table": "each reader ... independently ... never from a shared
    // implementation").
    private static string ReplicaDirectoryNameForRateRuns(ReplicaKind kind) => kind switch
    {
        ReplicaKind.Lagged => "replicas-lagged",
        ReplicaKind.Independent => "replicas-independent",
        ReplicaKind.Gsv3 => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "FixturesSha256 excludes Gsv3 by declared rule; ReplicaKindsUsedByRateRuns never names it."),
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "FixturesSha256 covers only the ReplicaKind values the rate runs use."),
    };

    // tests/Fixtures/BOOT.md, "## Rate table": every file exclusions.json holds, every file under references/,
    // and every file under each replica directory the rate runs' own comparisons read — a directory rule, never a
    // hand-typed file list (AGENTS.md §6, "a criterion with the quantifier 'all' is checked against a list
    // generated by the machine").
    private static IEnumerable<string> FixturesSourcePaths(string fixturesDirectory)
    {
        yield return Path.Combine(fixturesDirectory, "exclusions.json");

        foreach (var path in Directory.EnumerateFiles(Path.Combine(fixturesDirectory, "references"), "*", SearchOption.AllDirectories))
        {
            yield return path;
        }

        foreach (var kind in ReplicaKindsUsedByRateRuns)
        {
            var replicaDirectory = Path.Combine(fixturesDirectory, ReplicaDirectoryNameForRateRuns(kind));
            foreach (var path in Directory.EnumerateFiles(replicaDirectory, "*", SearchOption.AllDirectories))
            {
                yield return path;
            }
        }
    }

    // tests/Harness.Tests/RateCriterionTests's own twin, computed independently from the same rule (see above):
    // files ordered by their path relative to tests/Fixtures, ordinal, forward-slash normalized; each file's own
    // relative path and LF-normalized content fold into the digest in that order, the same shape
    // CriterionSourceDigest already uses for filenames.
    private static string FixturesSourceDigest()
    {
        var fixturesDirectory = RepositoryPaths.Resolve("tests", "Fixtures");
        var orderedFiles = FixturesSourcePaths(fixturesDirectory)
            .Select(path => (Path: path, RelativePath: Path.GetRelativePath(fixturesDirectory, path).Replace('\\', '/')))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal);

        var combined = new System.Text.StringBuilder();
        foreach (var (path, relativePath) in orderedFiles)
        {
            _ = combined.Append(relativePath).Append('\0');
            _ = combined.Append(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)).Append('\0');
        }

        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined.ToString())));
    }
}
