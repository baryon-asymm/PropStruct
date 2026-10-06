using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Root BOOT.md, "the pass condition compares failure rates, not single runs" (decided 2026-09-24): a single
/// run of the port or of the original passing or failing the per-run criterion (<see cref="StatisticalCriterion.Compare"/>
/// and its two siblings) is not evidence either way — `tests/Harness/BOOT.md`, "## Null rate of the original"
/// measures the original's own runs failing it 15 times out of 197 (7.6 %, after E1's mass-bracket fix and E2's
/// adaptive common range; 17/197 after E1 alone, 22/197 before either), far above the nominal per-run
/// level: a run fails if any of the three reports fails, each Bonferroni-corrected at <c>alpha = 10^-3</c>
/// separately, so the nominal level is at most 3 * 10^-3, not 10^-3 alone (⚠ 2026-09-27, AGENTS.md §8: this
/// comment previously read "far above the per-run `alpha = 10^-3`", one report's own level read as the run's;
/// `tests/Harness/BOOT.md`, "## Null rate of the original", has the false-failure classification this
/// correction also fixes). What this class asserts instead: the port's own failure rate under <see cref="PrecisionKind.Original"/>, over
/// sixteen seeds per formulation and layout (pregenerated, <c>tests/Fixtures/rate-table.json</c> —
/// <c>tests/Fixtures/BOOT.md</c>, "## Rate table"; <c>tests/RateTableTool</c> writes it), is
/// no higher than the original's own null rate, by a one-sided exact binomial test at the tree's own <c>alpha</c>
/// (<see cref="StatisticalCriterion.Alpha"/>) — never a second significance level (root BOOT.md Taboos).
/// </summary>
public partial class RateCriterionTests(ITestOutputHelper output)
{
    // `tests/Fixtures/BOOT.md`, "## Rate table": the schema tests/RateTableTool/RateTable.cs writes, read here with
    // this node's own minimal deserialization against the documented field names — a neighbour's own CLR type
    // (internal to that project, never exported) is not part of its contract; the schema is documented in prose
    // in tests/Fixtures/API.md instead, so this is not a second implementation of anything, only a second reader
    // of one documented JSON shape (the same relationship this node already has with exclusions.json/
    // provenance.json, each read independently by whichever node needs them).
    private sealed record RateTableRun(
        string Formulation, string Layout, string Precision, int SeedIndex, ulong Seed,
        int FailingCells, IReadOnlyList<string> FailingNames, string ResultSha256);

    private sealed record RateTable(
        string GeneratedAtUtc, string Commit, string CriterionSha256, string SnapshotSha256,
        string FixturesSha256, string SeedStride, IReadOnlyList<RateTableRun> Runs);

    // Source-generated: a plain JsonSerializer.Deserialize<RateTable> call gives CA1812 no evidence that
    // `RateTable` and `RateTableRun` are ever constructed (reflection-based deserialization is invisible to it),
    // and neither may be removed — they are rate-table.json's own documented shape.
    [JsonSerializable(typeof(RateTable))]
    private sealed partial class RateTableJsonContext : JsonSerializerContext;

    private static string TablePath => RepositoryPaths.Resolve("tests", "Fixtures", "rate-table.json");
    private static string HarnessDirectory => RepositoryPaths.Resolve("tests", "Harness");
    private static string SnapshotPath => RepositoryPaths.Resolve(
        "tests", "Simulation.Tests", "Snapshots", "SeedZeroResultsM.approved.txt");

    private static RateTable LoadTable() =>
        JsonSerializer.Deserialize(File.ReadAllText(TablePath), RateTableJsonContext.Default.RateTable)
        ?? throw new InvalidOperationException($"'{TablePath}' deserialized to null.");

    // Line endings are normalized before hashing: the repository stores these files with LF
    // (`.gitattributes`), but a working copy may hold CRLF, and the table's first hash was taken
    // from such a copy (2026-09-24). The content, not the checkout's line endings, is what the tie guards.
    private static string Sha256OfFile(string path) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal))));

    // `tests/Harness/BOOT.md`, "## Null rate of the original ... the tie", and `tests/Fixtures/BOOT.md`, "## Rate
    // table": the criterion is no longer one file, or one file family (audit finding R3, 2026-09-25:
    // `StatisticalCriterion.cs` split by concern into a partial class; 2026-09-26: split further into named
    // single-responsibility classes, the façade the only survivor of the old name). A glob on the old name would
    // degenerate silently the moment the split lands (AGENTS.md §13: "a check never seen red is indistinguishable
    // from an absent one") — the digest now covers every `*.cs` file directly under `tests/Harness`, minus the
    // declared exclusion list below, each named because no comparison this criterion runs ever reaches it. The
    // default is *in*: a coder adding a file must argue it out by naming it here, not merely by choosing a
    // filename an old glob would have missed.
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

    // The set is defined by a rule, not a hand-typed list of what to include (AGENTS.md §6, "a criterion with the
    // quantifier 'all' is checked against a list generated by the machine, not typed by hand"): every `*.cs` file
    // under `tests/Harness` not named above, hashed as one digest in a fixed (ordinal-by-filename) order, each
    // file's own name folded into the digest so the order and the membership are both part of what the tie
    // guards, not only the concatenated bytes. `tests/RateTableTool/Program.cs` computes the identical digest,
    // with the identical exclusion list, when writing the table — a second reader of the same rule, not a second
    // implementation of the hash formula, the same relationship the single-file <see cref="Sha256OfFile"/>
    // already had with its own writer-side twin.
    private static List<string> CriterionSourceFiles() =>
        [.. Directory.GetFiles(HarnessDirectory, "*.cs")
            .Where(path => !ExcludedFromCriterionDigest.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)];

    /// <summary>
    /// The exclusion list itself, guarded (design decision, tests/Fixtures/BOOT.md, "## Rate table"): every name
    /// in <see cref="ExcludedFromCriterionDigest"/> must still exist under `tests/Harness`, or the entry is stale
    /// (a rename or deletion the list was never updated for) and this test turns red rather than silently
    /// narrowing the digest by one file nobody excluded on purpose.
    /// </summary>
    [Fact]
    public void ExclusionListNamesExactlyTheHarnessFilesThatStillExistUnderThoseNames()
    {
        var existingFiles = Directory.GetFiles(HarnessDirectory, "*.cs").Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        foreach (var name in ExcludedFromCriterionDigest)
        {
            Assert.True(
                existingFiles.Contains(name),
                $"'{name}' is named in the criterion-digest exclusion list but no longer exists under " +
                $"'{HarnessDirectory}' — a stale entry (tests/Fixtures/BOOT.md, \"## Rate table\").");
        }
    }

    private static string CriterionSourceDigest()
    {
        var combined = new System.Text.StringBuilder();
        foreach (var path in CriterionSourceFiles())
        {
            _ = combined.Append(Path.GetFileName(path)).Append('\0');
            _ = combined.Append(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)).Append('\0');
        }

        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined.ToString())));
    }

    private static string FixturesDirectory => RepositoryPaths.Resolve("tests", "Fixtures");

    /// <summary>
    /// tests/Fixtures/BOOT.md, "## Rate table": the <see cref="ReplicaKind"/> values the rate runs' own
    /// comparisons ever pass — <c>tests/RateTableTool/Program.cs</c>'s own <c>layout == Original ? Lagged :
    /// Independent</c> mapping, mirrored by <see cref="NullRateCalibration"/>'s own null-rate runs. <see
    /// cref="ReplicaKind.Gsv3"/> carries no rows into <c>FixturesSha256</c>: neither place ever constructs it (<see
    /// cref="NullRateCalibration"/>'s own <c>ReplicaDirectory</c> throws for it), so hashing its 96-per-formulation
    /// files would watch data no comparison here ever reads.
    /// </summary>
    private static readonly ReplicaKind[] ReplicaKindsUsedByRateRuns = [ReplicaKind.Lagged, ReplicaKind.Independent];

    /// <summary>
    /// The exclusion itself, guarded the same way <see cref="ExclusionListNamesExactlyTheHarnessFilesThatStillExistUnderThoseNames"/>
    /// guards the criterion digest's own list: every <see cref="ReplicaKind"/> value declared today must be
    /// either used above or explicitly accounted for as excluded here, or a new kind a future rate run starts
    /// passing to <see cref="StatisticalCriterion.Compare"/> would silently miss <c>FixturesSha256</c> instead of
    /// turning this test red.
    /// </summary>
    [Fact]
    public void ReplicaKindsUsedByRateRunsAccountForEveryDeclaredReplicaKind()
    {
        var accountedFor = new HashSet<ReplicaKind>(ReplicaKindsUsedByRateRuns) { ReplicaKind.Gsv3 };
        foreach (var kind in Enum.GetValues<ReplicaKind>())
        {
            Assert.True(
                accountedFor.Contains(kind),
                $"ReplicaKind.{kind} is neither in ReplicaKindsUsedByRateRuns nor declared excluded — " +
                "tests/Fixtures/BOOT.md, \"## Rate table\" needs a decision before FixturesSha256 can cover it.");
        }

        foreach (var kind in ReplicaKindsUsedByRateRuns)
        {
            var directory = Path.Combine(FixturesDirectory, FixtureReplicas.ReplicaDirectory(kind));
            Assert.True(Directory.Exists(directory), $"'{directory}' (ReplicaKind.{kind}) does not exist.");
        }
    }

    // tests/Fixtures/BOOT.md, "## Rate table": every file exclusions.json holds, every file under references/, and
    // every file under each replica directory the rate runs' own comparisons read (FixtureReplicas.ReplicaDirectory,
    // the one implementation this reader has IVT access to, unlike tests/RateTableTool/Program.cs's own independent
    // restatement of the same two-case rule) — a directory rule, never a hand-typed file list (AGENTS.md §6).
    private static IEnumerable<string> FixturesSourcePaths()
    {
        yield return Path.Combine(FixturesDirectory, "exclusions.json");

        foreach (var path in Directory.EnumerateFiles(Path.Combine(FixturesDirectory, "references"), "*", SearchOption.AllDirectories))
        {
            yield return path;
        }

        foreach (var kind in ReplicaKindsUsedByRateRuns)
        {
            var replicaDirectory = Path.Combine(FixturesDirectory, FixtureReplicas.ReplicaDirectory(kind));
            foreach (var path in Directory.EnumerateFiles(replicaDirectory, "*", SearchOption.AllDirectories))
            {
                yield return path;
            }
        }
    }

    // tests/RateTableTool/Program.cs's own twin, computed independently from the same rule (see above): files
    // ordered by their path relative to tests/Fixtures, ordinal, forward-slash normalized; each file's own
    // relative path and LF-normalized content fold into the digest in that order, the same shape
    // CriterionSourceDigest already uses for filenames.
    private static string FixturesSourceDigest()
    {
        var orderedFiles = FixturesSourcePaths()
            .Select(path => (Path: path, RelativePath: Path.GetRelativePath(FixturesDirectory, path).Replace('\\', '/')))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal);

        var combined = new System.Text.StringBuilder();
        foreach (var (path, relativePath) in orderedFiles)
        {
            _ = combined.Append(relativePath).Append('\0');
            _ = combined.Append(File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)).Append('\0');
        }

        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined.ToString())));
    }

    /// <summary>
    /// The tie (root BOOT.md, "a check ties the two together so that the rate cannot go stale"): the table's own
    /// recorded hash of the criterion's source, of the fixture data the criterion reads, and of the seed-0
    /// snapshot must each equal what is in the tree right now, or the table was generated against a different
    /// criterion, different fixture data, or a different port and no longer means anything — regenerating is the
    /// only fix, never a loosened comparison. Three separate assertions, not one combined check, so a failure
    /// names which input moved (tests/Fixtures/BOOT.md, "## Rate table").
    /// </summary>
    [Fact]
    public void RateTableTiesToTheCurrentCriterionAndSnapshot()
    {
        var table = LoadTable();
        var currentCriterion = CriterionSourceDigest();
        var currentSnapshot = Sha256OfFile(SnapshotPath);
        var currentFixtures = FixturesSourceDigest();

        output.WriteLine($"table criterion sha256={table.CriterionSha256}, current={currentCriterion}");
        output.WriteLine($"table snapshot  sha256={table.SnapshotSha256}, current={currentSnapshot}");
        output.WriteLine($"table fixtures  sha256={table.FixturesSha256}, current={currentFixtures}");

        Assert.True(
            string.Equals(table.CriterionSha256, currentCriterion, StringComparison.Ordinal),
            $"{TablePath} was generated against a different tests/Harness/StatisticalCriterion*.cs " +
            $"(table sha256={table.CriterionSha256}, current sha256={currentCriterion}). Regenerate the table: " +
            "dotnet run --project tests/RateTableTool -- rate-table");
        Assert.True(
            string.Equals(table.SnapshotSha256, currentSnapshot, StringComparison.Ordinal),
            $"{TablePath} was generated against a different seed-0 snapshot " +
            $"(table sha256={table.SnapshotSha256}, current sha256={currentSnapshot}). Regenerate the table: " +
            "dotnet run --project tests/RateTableTool -- rate-table");
        Assert.True(
            string.Equals(table.FixturesSha256, currentFixtures, StringComparison.Ordinal),
            $"{TablePath} was generated against different fixture data (exclusions.json, references/ or a " +
            $"replica directory the rate runs read; table sha256={table.FixturesSha256}, " +
            $"current sha256={currentFixtures}). Regenerate the table: " +
            "dotnet run --project tests/RateTableTool -- rate-table");
    }

    /// <summary>
    /// design §4, "the tie": the 320 stored files under <c>tests/Fixtures/rate-runs</c> and the table's own rows
    /// match one to one, and each file's LF-normalised SHA-256 (<see cref="Sha256OfFile"/>, the same normalisation
    /// as the digests above) equals its row's <see cref="RateTableRun.ResultSha256"/> — without this, a stored
    /// file could drift from the table (a hand edit, a partial regeneration) unnoticed, since
    /// <see cref="RateTableTiesToTheCurrentCriterionAndSnapshot"/> never reads the files themselves.
    /// </summary>
    [Fact]
    public void RateRunsTieToTheRateTable()
    {
        var table = LoadTable();
        var rateRunsDirectory = RepositoryPaths.Resolve("tests", "Fixtures", "rate-runs");

        var expectedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var run in table.Runs)
        {
            var path = Path.Combine(rateRunsDirectory, run.Formulation, run.Layout, run.Precision, $"{run.SeedIndex}.m.txt");
            _ = expectedPaths.Add(path);

            Assert.True(
                File.Exists(path),
                $"'{path}' is missing — the table has a row for {run.Formulation}/{run.Layout}/{run.Precision}, " +
                $"seed index {run.SeedIndex}, but no stored file. Regenerate: " +
                "dotnet run --project tests/RateTableTool -- rate-table");

            var actualSha = Sha256OfFile(path);
            Assert.True(
                string.Equals(actualSha, run.ResultSha256, StringComparison.Ordinal),
                $"'{path}': the stored file's LF-normalised SHA-256 ({actualSha}) does not match the table row's " +
                $"ResultSha256 ({run.ResultSha256}) for {run.Formulation}/{run.Layout}/{run.Precision}, seed index " +
                $"{run.SeedIndex} — the file drifted from the table it is supposed to tie to.");
        }

        var actualFiles = Directory.EnumerateFiles(rateRunsDirectory, "*.m.txt", SearchOption.AllDirectories)
            .ToHashSet(StringComparer.Ordinal);
        var unmatched = actualFiles.Except(expectedPaths).ToList();
        Assert.True(
            unmatched.Count == 0,
            $"{unmatched.Count} file(s) under '{rateRunsDirectory}' have no matching row in the table: " +
            $"{string.Join(", ", unmatched)}");
    }

    /// <summary>One-sided exact binomial tail: <c>P(X &gt;= k | n, p)</c>, summed directly from
    /// <see cref="BetaBinomialPredictive.Pmf"/> at <c>rho = 0</c> — its own documented ordinary-binomial special
    /// case (<c>tests/Harness/API.md</c>: "rho == 0 is the ordinary binomial") — never a second implementation of
    /// the binomial tail formula (root BOOT.md Taboos).
    ///
    /// ⚠ 2026-09-27 (F-a, AGENTS.md §8): was <c>1.0 - CdfAtMost(k - 1, ...).CdfAtMost</c>. Both compute the same
    /// mathematical quantity, but a rejecting positive control puts <c>CdfAtMost(k - 1, ...)</c> within a
    /// double's own epsilon of 1.0 (HPEPA3/HMX/inpt's own Binary64 runs: all 32 of 32 seeds fail, so
    /// <c>CdfAtMost(31, 32, p)</c> sums 32 lower-tail terms that add to <c>1.0</c> minus something far smaller
    /// than <c>double</c> can represent next to 1.0) — subtracting that sum from <c>1.0</c> does not compute the
    /// tail probability, it reads off <c>double</c>'s own rounding floor at 1.0 (measured: `p` printed as
    /// `4.10783e-15` for all three, a constant every one of them shared regardless of how much further below it
    /// the true value actually sits), not the true one. Summing the upper tail's own pmf terms directly never
    /// subtracts from 1.0, so it has no such floor. Measured after the fix: HPEPA3/HMX/inpt's shared
    /// `4.10783e-15` becomes `1.6293e-36` (21 orders of magnitude smaller, no longer a shared constant, each
    /// computed on its own terms); PSAN02n's own `1.22924e-12` — far enough from the floor already — moves only
    /// in its last two significant digits, to `1.22502e-12`; every value not near the floor (P33's `0.444265`,
    /// the pooled port rate's `0.0624644`) is bit-identical. No verdict changes anywhere.</summary>
    private static double UpperTailProbability(int k, int n, double p)
    {
        if (k <= 0)
        {
            return 1.0;
        }

        var pmf = BetaBinomialPredictive.Pmf(n, p, rho: 0.0);
        var sum = 0.0;
        for (var j = k; j <= n; j++)
        {
            sum += pmf[j];
        }

        return sum;
    }

    private readonly record struct RateFigure(int Failing, int Total)
    {
        public double Rate => Total == 0 ? double.NaN : (double)Failing / Total;
    }

    private static RateFigure OriginalNullRate() =>
        Pool(NullRateCalibration.AllOriginalRuns().Select(r => r.Failed));

    private static RateFigure Pool(IEnumerable<bool> failed)
    {
        var list = failed.ToList();
        return new RateFigure(list.Count(f => f), list.Count);
    }

    /// <summary>
    /// `tests/Harness/BOOT.md`, "## Null rate of the original": reproduces the exact figures Gate2's own lagged set
    /// gives on its own (9/96, after E2 — `tests/Harness/HISTORY.md#e2-adaptive-common-range`; 10/96 after E1
    /// alone, 13/96 before either) as a positive control that <see cref="NullRateCalibration"/>'s refactor changed
    /// nothing about the leave-one-out loop it now shares with this class.
    /// </summary>
    [Fact]
    public void OriginalNullRateLaggedLeaveOneOutMatchesGate2sOwnFigure()
    {
        var lagged = NullRateCalibration.LeaveOneOutRuns(ReplicaKind.Lagged);
        var failing = lagged.Count(r => r.Failed);
        output.WriteLine($"lagged leave-one-out: {failing}/{lagged.Count}");
        Assert.Equal(96, lagged.Count);
        Assert.Equal(9, failing);
    }

    /// <summary>
    /// The pass condition (root BOOT.md, "the pass": "a one-sided exact test finds the port's rate no higher
    /// than the original's, at the tree's alpha, pooled"). Binomial, not Fisher
    /// (`tests/Harness/BOOT.md`, "## Null rate of the original, and the port's rate against it",
    /// "Binomial, not Fisher" has the justification): the original's own null rate is estimated from
    /// 197 runs, more than the port's own 160, and is treated as the fixed reference rate the port is held to —
    /// the same "one side is the standard, the other is being checked against it" shape the print-resolution
    /// floor and the count floor already use elsewhere in this file, not a symmetric two-sample question.
    /// </summary>
    [Fact]
    public void PortRateOriginalPrecisionIsNoHigherThanTheOriginalsNullRatePooled()
    {
        var table = LoadTable();
        var originalNull = OriginalNullRate();
        var portOriginal = Pool(table.Runs.Where(r => r.Precision == "Original").Select(r => r.FailingCells > 0));

        var pValue = UpperTailProbability(portOriginal.Failing, portOriginal.Total, originalNull.Rate);

        output.WriteLine(
            $"original null rate: {originalNull.Failing}/{originalNull.Total} = {originalNull.Rate:P2}; " +
            $"port rate (Original precision, pooled): {portOriginal.Failing}/{portOriginal.Total} = {portOriginal.Rate:P2}; " +
            $"one-sided exact binomial p-value = {pValue:G6} (alpha = {StatisticalCriterion.Alpha:G3}).");

        foreach (var group in table.Runs.Where(r => r.Precision == "Original").GroupBy(r => r.Formulation))
        {
            var figure = Pool(group.Select(r => r.FailingCells > 0));
            var formulationP = UpperTailProbability(figure.Failing, figure.Total, originalNull.Rate);
            output.WriteLine($"  {group.Key}: {figure.Failing}/{figure.Total} = {figure.Rate:P2}, p-value = {formulationP:G6} (reported, not gated).");
        }

        Assert.True(
            pValue >= StatisticalCriterion.Alpha,
            $"the port's pooled Original-precision failure rate ({portOriginal.Failing}/{portOriginal.Total}) is " +
            $"significantly higher than the original's own null rate ({originalNull.Failing}/{originalNull.Total}, " +
            $"rate {originalNull.Rate:P2}): one-sided exact binomial p-value {pValue:G6} < alpha {StatisticalCriterion.Alpha:G3}.");
    }

    /// <summary>
    /// The positive control (root BOOT.md, "the positive control": "the same test on `Binary64` runs rejects on
    /// HPEPA3 and HMX, whose REAL*4 degradation is the known effect") — root BOOT.md, "Precision kind": the
    /// original's REAL*4 accumulators change HPEPA3's own headline answers by five to seven per cent, and HMX's
    /// trajectory diverges from the original's own REAL*4 storage from the first sensitive branch on; `Binary64`
    /// never reproduces either, so it must fail this test where the port's own `Original` precision passes it.
    /// If it does not reject here, the rate test has no power and the decision needs revisiting (this task's own
    /// instruction) — reported for every formulation, gated on these two only.
    /// </summary>
    [Fact]
    public void PortRateBinary64PrecisionIsSignificantlyHigherThanTheOriginalsNullRateOnHpepa3AndHmx()
    {
        var table = LoadTable();
        var originalNull = OriginalNullRate();

        var gated = new[] { "HPEPA3", "HMX" };
        foreach (var group in table.Runs.Where(r => r.Precision == "Binary64").GroupBy(r => r.Formulation))
        {
            var figure = Pool(group.Select(r => r.FailingCells > 0));
            var pValue = UpperTailProbability(figure.Failing, figure.Total, originalNull.Rate);
            output.WriteLine($"  {group.Key}: Binary64 precision {figure.Failing}/{figure.Total} = {figure.Rate:P2}, p-value = {pValue:G6}.");

            if (gated.Contains(group.Key, StringComparer.Ordinal))
            {
                Assert.True(
                    pValue < StatisticalCriterion.Alpha,
                    $"the positive control failed to reject: {group.Key}'s Binary64-precision failure rate " +
                    $"({figure.Failing}/{figure.Total}) is not significantly higher than the original's own null " +
                    $"rate (p-value {pValue:G6} >= alpha {StatisticalCriterion.Alpha:G3}) - the rate test has no " +
                    "measured power here and the decision needs revisiting (root BOOT.md's own instruction).");
            }
        }
    }
}
