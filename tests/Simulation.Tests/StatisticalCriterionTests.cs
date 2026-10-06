using System.Collections.Concurrent;
using System.Diagnostics;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.RateTableTool;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L2 of the BOOT.md table, "the statistical criterion on the five reference formulations" — coded together
/// with <c>Output</c>, per <c>src/Simulation/BOOT.md</c>, "Design decisions". Realigned 2026-09-23 to the
/// three links root ACCEPTANCE.md's own acceptance criteria draw between the criterion above (a candidate against
/// the <em>original's</em> replicas) and reproduction of the original's REAL*4 storage (root ACCEPTANCE.md,
/// "The `Original` accumulation kind reproduces the original's printed output"): one criterion cannot serve
/// two kinds, so <see cref="PrecisionKind.Binary64"/>, the port's own arithmetic, is no longer read against
/// replicas of the <em>original</em> — that comparison belongs to <see cref="PrecisionKind.Original"/> alone.
/// What <see cref="PrecisionKind.Binary64"/> is compared against is the port's own reference mode, statistically,
/// across seeds (link 3, below): a claim about the port's own determinism-across-schedule invariant, not about
/// agreement with the Fortran.
///
/// Realigned again 2026-09-24 (root BOOT.md, "the pass condition compares failure rates, not single runs"):
/// link 1 (agreement with the original's replicas under <see cref="PrecisionKind.Original"/>) is no longer
/// judged here at all, by a single seed-0 ratchet of named cells. A single run passing or failing the
/// per-run criterion is not evidence either way — the original's own runs fail it 22 times in 197, the port's
/// 9 times in 160 (root BOOT.md, same section) — so link 1 is now a *rate* comparison over sixteen seeds per
/// formulation and layout, owned by <c>tests/Harness.Tests/RateCriterionTests</c> (a pregenerated table,
/// <c>tests/Fixtures/rate-table.json</c>, `tests/Fixtures/BOOT.md`, "## Rate table"). This class keeps only an
/// exact byte-for-byte snapshot of the seed-0 output (<see cref="SeedZeroResultsMMatchesApprovedSnapshot"/>,
/// below) — a change detector, not a reproduction claim — and link 3 (batched/CUDA against sequential
/// <see cref="PrecisionKind.Binary64"/>, statistically, on the port's own replicas), unaffected by this
/// realignment.
///
/// Every failure is reported through <see cref="ITestOutputHelper"/> with its cell (quantity and index), its
/// value, the comparison mean, the threshold (the "band" the criterion allows) and the spread
/// (<c>(value - mean) / threshold</c>, i.e. how many threshold-widths the candidate sits from the replica
/// mean) before the assertion fails the test — nothing here excludes, loosens or reclassifies a failing cell
/// to go green (root BOOT.md Taboos; this node's own BOOT.md Taboos).
///
/// Runtime is dominated by reference-mode attempts, not particle count: HMX's mean 325.81 attempts per
/// particle (<c>src/Simulation/BOOT.md</c>, "## Budget measurement") makes its reference-mode runs the
/// slowest of the five despite the smallest particle count. Every run here uses the whole formulation (no
/// constructed cut-down case), because both the port's own seed-0 snapshot and the runs link 3 compares to
/// each other were generated, or are run, from the whole formulation (<c>tests/Fixtures/API.md</c>, "## Layout").
/// </summary>
public class StatisticalCriterionTests(ITestOutputHelper output)
{
    // internal: was shared with BudgetLadderTests (2026-09-28, deleted 2026-10-01 at 3ea6471), which spans the same five formulations.
    internal static readonly string[] AllReferenceFormulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];

    public static IEnumerable<object[]> ReferenceFormulations() =>
        AllReferenceFormulations.Select(n => new object[] { n });

    /// <summary>
    /// Link 1's own evidence, root BOOT.md, "the pass condition compares failure rates, not single runs": "the
    /// seed-0 ratchet of known cells is replaced ... by an exact snapshot of the seed-0 <c>results.m</c> under
    /// both kinds (time line aside), a change detector like the surface snapshot." The three named-cell
    /// exceptions the old ratchet (<c>KnownOriginalLayoutFailures</c>/<c>KnownIndependentLayoutFailures</c>/
    /// <c>RunAndRatchet</c>, all retired here — audit D5) used to carry are gone with it: this test asserts
    /// nothing about agreement with the original's replicas at all (that claim is
    /// <c>tests/Harness.Tests/RateCriterionTests</c>'s own, over the sixteen-seed rate table, not a single seed);
    /// it only asserts that the seed-0 output has not moved since the file below was approved. Twenty cases: five
    /// formulations, both layouts, both precision kinds.
    /// </summary>
    public static IEnumerable<object[]> SeedZeroSnapshotCases()
    {
        foreach (var name in new[] { "HPEPA3", "inpt", "P33", "PSAN02n", "HMX" })
        {
            foreach (var layout in new[] { StreamLayout.Original, StreamLayout.Independent })
            {
                foreach (var precision in new[] { PrecisionKind.Original, PrecisionKind.Binary64 })
                {
                    yield return new object[] { name, layout, precision };
                }
            }
        }
    }

    private static string ApprovedSnapshotPath => RepositoryPaths.Resolve(
        "tests", "Simulation.Tests", "Snapshots", "SeedZeroResultsM.approved.txt");

    private static string SnapshotKey(string name, StreamLayout layout, PrecisionKind precision) => $"{name} {layout} {precision}";

    private static Dictionary<string, string> LoadApprovedSnapshots()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(ApprovedSnapshotPath))
        {
            return map;
        }

        foreach (var line in File.ReadAllLines(ApprovedSnapshotPath))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var lastSpace = line.LastIndexOf(' ');
            map[line[..lastSpace]] = line[(lastSpace + 1)..];
        }

        return map;
    }

    [Theory]
    [MemberData(nameof(SeedZeroSnapshotCases))]
    [Trait("Category", "Long")]
    public void SeedZeroResultsMMatchesApprovedSnapshot(string name, StreamLayout layout, PrecisionKind precision)
    {
        var formulation = ReadFormulation(name);
        var run = ReferenceModeRunner.RunSeed(formulation, layout, precision, seed: 0UL);
        var key = SnapshotKey(name, layout, precision);
        var approved = LoadApprovedSnapshots();

        output.WriteLine($"{key}: sha256={run.Sha256Hex}");

        Assert.True(
            approved.TryGetValue(key, out var expectedHash),
            $"no approved snapshot for '{key}' in {ApprovedSnapshotPath}; run " +
            "'dotnet run --project tests/RateTableTool -- seed-zero-snapshot' to add it.");
        Assert.True(
            string.Equals(expectedHash, run.Sha256Hex, StringComparison.Ordinal),
            $"{key}: the seed-0 results.m (time line aside) no longer matches the approved snapshot " +
            $"{ApprovedSnapshotPath}. expected sha256={expectedHash}, got sha256={run.Sha256Hex}. If the change " +
            "is intended, review the diff (this test's own ITestOutputHelper carries the new hash) and " +
            "regenerate with 'dotnet run --project tests/RateTableTool -- seed-zero-snapshot', in the same " +
            "commit as the reason for the change and this node's BOOT.md, ## Mutations if it was a product change.");
    }

    /// <summary>
    /// Batched mode with the <c>Original</c> layout is refused before this criterion has anything to compare
    /// (root BOOT.md, "Two stream layouts": "The `Original` layout is sequential only"; this node's own
    /// BOOT.md, "## Invariants"). This theory used to run the full criterion here and measured, on 2026-09-20,
    /// P33 35/1462 and HMX 92/4343 failing cells against 0 and 46 for reference mode in the same layout — the
    /// measurement root ACCEPTANCE.md's own batched acceptance criterion records as the evidence that decided the
    /// refusal (root ACCEPTANCE.md, "Batched mode ... satisfies the same criterion", the `Original` row); that row
    /// is root's own and is not re-run here. What is checked now is the refusal itself: since architecture
    /// audit finding R1 (2026-09-24), it fires from <see cref="Simulator.Create"/> itself, before any setup
    /// or accelerator work exists, not merely "before any attempt runs" — so <paramref name="name"/>'s own
    /// formulation is never even read (`SimulationOptionsValidator`, `src/Simulation/BOOT.md`).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void BatchedModeCpuOriginalLayoutIsRefused(string name)
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
        };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        output.WriteLine($"{name}: refused at Simulator.Create, before any setup work.");
    }

    /// <summary>
    /// Link 3 (root ACCEPTANCE.md, "The `Original` accumulation kind reproduces the original's printed output":
    /// "batched and CUDA against sequential `Binary64`, statistically, on the port's own replicas"). Both sides
    /// run <see cref="PrecisionKind.Binary64"/>, <see cref="StreamLayout.Independent"/> — the only layout batched
    /// mode accepts — at <c>R = 8</c> seeds each, <c>s = k * 2^17</c> for <c>k = 0..7</c>. That choice is not
    /// arbitrary: the per-particle jump advances a stream by <c>seed * 2^80 + ordinal * 2^40</c>
    /// (root BOOT.md, "Execution model"), and <c>src/Random/BOOT.md</c>, "Seed and replica jumps are rigid
    /// shifts" proves the <c>2^80</c> seed jump is a rigid phase shift of the stream's own output: stepping
    /// <c>seed</c> by one moves every `Independent`-layout stream's phase by an odd multiple of
    /// <c>1/2^20</c>, so stepping by <c>2^17</c> moves it by an odd multiple of <c>1/8</c> — and since that
    /// multiple is odd, the eight steps <c>k = 0..7</c> land on all eight eighths of the circle exactly once,
    /// each stream spread over the full phase range in the smallest number of seeds that does so, rather than
    /// clustering near one point the way eight consecutive seeds would (the same document's own per-seed
    /// figures: a third of a percent of the circle per unit seed on one stream).
    ///
    /// The comparison itself is <see cref="StatisticalCriterion.TwoSampleBiasOfSets"/> (`tests/Harness/API.md`,
    /// made public 2026-09-23 for this use, `tests/Harness/BOOT.md`, "Two-sample bias"): a Welch two-sample
    /// t-test at every cell both sets reach, family-wise corrected at the tree's own <c>alpha = 10^-3</c>. It
    /// has no canonical-axis handling, unlike <see cref="StatisticalCriterion.Compare"/> — a caller's problem
    /// to solve, not this seam's (`tests/Harness/BOOT.md`, same section) — which matters here because
    /// `results.m` carries category-indexed quantities (<c>fqdokkarm(&lt;row&gt;,:)</c>, <c>Dkarmcat</c>,
    /// <c>dokkarm43</c>, <c>dokkarm10</c>, <c>coef</c>) whose row/index meaning is chosen per run from the
    /// pocket sizes that run produced (`tests/Harness/BOOT.md`, "Canonical category axis"); measured here
    /// (this node's own BOOT.md, Link 3's own CUDA table), those families did not need a ratchet list of their own — the small
    /// per-cell sample count the mismatch already produces (a physical row printed by only one or two of the
    /// sixteen runs is dropped from the comparison, not zeroed) left no spurious failure across all five
    /// formulations.
    ///
    /// The eight reference-mode runs, single-threaded each, run in parallel (this node's own BOOT.md,
    /// "## Invariants"); the eight batched runs, each already using every host thread through the CPU
    /// accelerator, run sequentially so as not to oversubscribe the machine. Both halves' wall time are
    /// reported.
    ///
    /// The reference sets (mode, layout, precision and seeds all identical to the CUDA row below) are shared
    /// with <see cref="BatchedModeCudaIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds"/>
    /// through <see cref="ReferenceSetsIndependentBinary64"/>'s own cache, keyed by formulation name, so the eight
    /// reference-mode runs are computed once per formulation and reused by whichever of the two CPU/CUDA rows
    /// runs second — never twice (this node's own BOOT.md, "## Link 3 measurement", the wall-time table).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    [Trait("Category", "Long")]
    public void BatchedModeCpuIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds(string name)
    {
        var formulation = ReadFormulation(name);

        var (referenceSets, referenceElapsed, referenceReused) = ReferenceSetsIndependentBinary64(name, formulation);

        var batchedStopwatch = Stopwatch.StartNew();
        var batchedSets = new IReadOnlyDictionary<string, double[]>[TwoSampleSeeds.Length];
        var attemptsPerLaunchUsed = 0;
        for (var k = 0; k < TwoSampleSeeds.Length; k++)
        {
            var (cells, diagnostics) = RunAndParse(formulation, ExecutionMode.Batched, TwoSampleSeeds[k], AcceleratorKind.Cpu);
            batchedSets[k] = cells;
            attemptsPerLaunchUsed = diagnostics.AttemptsPerLaunch;
        }
        batchedStopwatch.Stop();

        output.WriteLine(
            $"{name}: reference {TwoSampleSeeds.Length} seeds (parallel) in {referenceElapsed}" +
            (referenceReused ? " (reused from the CUDA row's cache, not recomputed)" : "") +
            $", CPU batched {TwoSampleSeeds.Length} seeds (sequential) in {batchedStopwatch.Elapsed}, " +
            $"AttemptsPerLaunch={attemptsPerLaunchUsed} (SimulationOptions' own default; this gate rides it, " +
            "the budget ladder was BudgetLadderTests, reproducible at 3ea6471).");

        AssertNoDifferingCells(name, "CPU", referenceSets, batchedSets);
    }

    /// <summary>
    /// Link 3, the CUDA row (root ACCEPTANCE.md, "Batched mode (whole-cycle batches, CPU accelerator and CUDA)
    /// satisfies the same criterion..."; this node's own BOOT.md, "## Statistical criterion measurement
    /// (2026-09-23)", link 3: "It stays unticked until CUDA is run the same way"). Same comparison as
    /// <see cref="BatchedModeCpuIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds"/>:
    /// <see cref="PrecisionKind.Binary64"/>, <see cref="StreamLayout.Independent"/>, the same eight seeds
    /// <c>s = k * 2^17</c>, the same <see cref="StatisticalCriterion.TwoSampleBiasOfSets"/> comparison at the
    /// tree's own family-wise <c>alpha = 10^-3</c> — with batched mode on <see cref="AcceleratorKind.Cuda"/>
    /// instead of <see cref="AcceleratorKind.Cpu"/>.
    ///
    /// The CUDA row follows `Execution.Tests`' own rule (this node's own BOOT.md, "## Constraints": "CUDA rows
    /// follow `Execution.Tests`' rule (refusal asserted, never skipped)"), the same branch
    /// <see cref="FailureStatusTests.RunBatchedOnCudaRunsOrFailsWithAcceleratorUnavailable"/> and
    /// <see cref="CudaRefusalTests"/> take: a cheap probe run decides whether CUDA is actually bound
    /// (<c>Accelerator.CudaSkippedBecause</c>) before the eight statistical runs are attempted. Where CUDA is
    /// not bound (no device, no libdevice, or the kill switch), the probe's own refusal is asserted —
    /// <see cref="RunStatus.AcceleratorUnavailable"/>, exactly as <see cref="BatchedModeCpuOriginalLayoutIsRefused"/>
    /// and the sibling test below assert the layout refusal — and the statistical comparison does not run,
    /// since there is nothing CUDA to compare (never silently downgraded to the CPU accelerator: the taboo
    /// against loosening a criterion for the sake of green applies as much to a quietly substituted
    /// accelerator as to a quietly substituted tolerance). Where CUDA is bound (the reference machine, an RTX
    /// 5070 Ti), the eight batched runs execute on it and are compared exactly as the CPU row's are.
    ///
    /// The eight reference-mode runs are the same runs the CPU row above computes, shared through
    /// <see cref="ReferenceSetsIndependentBinary64"/>'s cache rather than recomputed (this node's own BOOT.md,
    /// "## Link 3 measurement"): whichever of the two rows xunit happens to run first pays for them, and the
    /// other reuses the cached sets and reports zero additional reference wall time.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    [Trait("Category", "Long")]
    public void BatchedModeCudaIndependentLayoutBinary64PrecisionAgreesWithReferenceModeAcrossEightSeeds(string name)
    {
        var formulation = ReadFormulation(name);

        using (var probe = Simulator.Create(new SimulationOptions { Mode = ExecutionMode.Batched, Accelerator = AcceleratorKind.Cuda }))
        {
            output.WriteLine(
                $"{name}: accelerator {probe.Accelerator.Kind} {probe.Accelerator.Name}; " +
                $"CUDA skipped because: {probe.Accelerator.CudaSkippedBecause ?? "-"}");

            if (probe.Accelerator.CudaSkippedBecause is not null)
            {
                CudaRequirement.FailIfRequired(probe.Accelerator.CudaSkippedBecause);
                var smallFormulation = SmallFormulation.Build(particlesPerCycle: 24, cycles: 1);
                var exception = Assert.Throws<SimulationFailedException>(() => probe.Run(smallFormulation));
                Assert.Equal(RunStatus.AcceleratorUnavailable, exception.Status);
                output.WriteLine($"{name}: CUDA not bound on this machine; refusal asserted, statistical comparison not run.");
                return;
            }
        }

        var (referenceSets, referenceElapsed, referenceReused) = ReferenceSetsIndependentBinary64(name, formulation);

        var batchedStopwatch = Stopwatch.StartNew();
        var batchedSets = new IReadOnlyDictionary<string, double[]>[TwoSampleSeeds.Length];
        var attemptsPerLaunchUsed = 0;
        for (var k = 0; k < TwoSampleSeeds.Length; k++)
        {
            var (cells, diagnostics) = RunAndParse(formulation, ExecutionMode.Batched, TwoSampleSeeds[k], AcceleratorKind.Cuda);
            batchedSets[k] = cells;
            attemptsPerLaunchUsed = diagnostics.AttemptsPerLaunch;
        }
        batchedStopwatch.Stop();

        output.WriteLine(
            $"{name}: reference {TwoSampleSeeds.Length} seeds (parallel) in {referenceElapsed}" +
            (referenceReused ? " (reused from the CPU row's cache, not recomputed)" : "") +
            $", CUDA batched {TwoSampleSeeds.Length} seeds (sequential) in {batchedStopwatch.Elapsed}, " +
            $"AttemptsPerLaunch={attemptsPerLaunchUsed} (SimulationOptions' own default; this gate rides it, " +
            "the budget ladder was BudgetLadderTests, reproducible at 3ea6471).");

        AssertNoDifferingCells(name, "CUDA", referenceSets, batchedSets);
    }

    /// <summary>
    /// The CUDA row: <see cref="StreamLayout.Original"/> refuses regardless of accelerator kind or
    /// availability — since architecture audit finding R1 (2026-09-24), from <see cref="Simulator.Create"/>
    /// itself, before an accelerator is even created (`SimulationOptionsValidator`,
    /// `src/Simulation/BOOT.md`), so this row no longer branches on whether CUDA is bound and there is no
    /// accelerator to name in the log line any more — it is the same refusal
    /// <see cref="BatchedModeCpuOriginalLayoutIsRefused"/> proves, with <see cref="AcceleratorKind.Cuda"/>
    /// requested instead of <see cref="AcceleratorKind.Cpu"/>, proving the refusal does not depend on the
    /// requested accelerator either (root ACCEPTANCE.md names no independent-layout CUDA row here; the CPU rows
    /// above already cover that layout, and CUDA-vs-CPU agreement is `TierTableTests`' own concern).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void BatchedModeCudaOriginalLayoutIsRefusedRegardlessOfAcceleratorAvailability(string name)
    {
        var options = new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cuda,
            Streams = StreamLayout.Original,
        };

        var exception = Assert.Throws<SimulationFailedException>(() => Simulator.Create(options));

        Assert.Equal(RunStatus.InvalidSetup, exception.Status);
        output.WriteLine($"{name}: refused at Simulator.Create regardless of CUDA availability, before any accelerator exists.");
    }

    /// <summary><c>s = k * 2^17</c>, <c>k = 0..7</c> — see the "Link 3" doc comment above for why. Internal:
    /// shared with `BudgetLadderTests` (deleted 2026-10-01, reproducible at 3ea6471) (2026-09-28), which rides the same eight seeds.</summary>
    internal static readonly ulong[] TwoSampleSeeds = Enumerable.Range(0, 8).Select(k => (ulong)k << 17).ToArray();

    internal static Formulation ReadFormulation(string name) =>
        DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat"));

    /// <summary>
    /// The eight reference-mode sets link 3 compares batched mode against — <see cref="ExecutionMode.Reference"/>,
    /// <see cref="StreamLayout.Independent"/>, <see cref="PrecisionKind.Binary64"/>, <see cref="TwoSampleSeeds"/> —
    /// cached per formulation name so the CPU and CUDA link-3 rows share one computation instead of each paying
    /// for it (this node's own BOOT.md, "## Link 3 measurement"). <c>Lazy&lt;T&gt;</c> under
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> makes the factory run at most once per
    /// formulation even if both rows reach it concurrently (xunit's default collection behaviour runs the
    /// theory cases of one class sequentially, but the cache does not depend on that): whichever row calls this
    /// first pays the wall time below; the other's own <c>referenceReused</c> flag is <c>true</c> and its own
    /// reported reference time is the first row's, not a fresh measurement.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<(IReadOnlyDictionary<string, double[]>[] Sets, TimeSpan Elapsed)>> ReferenceIndependentBinary64Cache = new();

    /// <summary>Internal: shared with `BudgetLadderTests` (deleted 2026-10-01, reproducible at 3ea6471) (2026-09-28), whose every row also
    /// compares against these same eight reference-mode sets — reference mode never reads the budget ("##
    /// Invariants" above), so the cache below, keyed by formulation name only, serves every budget in the
    /// ladder too, computed at most once per formulation across this whole class and `BudgetLadderTests` (deleted 2026-10-01, reproducible at 3ea6471) combined.</summary>
    internal static (IReadOnlyDictionary<string, double[]>[] Sets, TimeSpan Elapsed, bool Reused) ReferenceSetsIndependentBinary64(string name, Formulation formulation)
    {
        var reused = ReferenceIndependentBinary64Cache.ContainsKey(name);
        var lazy = ReferenceIndependentBinary64Cache.GetOrAdd(name, cacheKey => new Lazy<(IReadOnlyDictionary<string, double[]>[], TimeSpan)>(
            () =>
            {
                var sets = new IReadOnlyDictionary<string, double[]>[TwoSampleSeeds.Length];
                var stopwatch = Stopwatch.StartNew();
                _ = Parallel.For(0, TwoSampleSeeds.Length, k =>
                {
                    sets[k] = RunAndParse(formulation, ExecutionMode.Reference, TwoSampleSeeds[k], AcceleratorKind.Cpu).Cells;
                });
                stopwatch.Stop();
                return (sets, stopwatch.Elapsed);
            },
            LazyThreadSafetyMode.ExecutionAndPublication));

        var (sets, elapsed) = lazy.Value;
        return (sets, elapsed, reused);
    }

    /// <summary>
    /// The link-3 comparison and its reporting, shared between the CPU and CUDA rows: every failing cell is
    /// written to <see cref="ITestOutputHelper"/> before the assertion, exactly as
    /// <see cref="SeedZeroResultsMMatchesApprovedSnapshot"/>'s own hash is (root BOOT.md Taboos).
    /// </summary>
    private void AssertNoDifferingCells(
        string name, string acceleratorLabel,
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> referenceSets,
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> batchedSets)
    {
        var cells = StatisticalCriterion.TwoSampleBiasOfSets(referenceSets, batchedSets);
        var differing = cells.Where(c => c.Differs).ToList();
        output.WriteLine($"{name} ({acceleratorLabel}): {cells.Count} cells compared, {differing.Count} differ.");
        foreach (var cell in differing)
        {
            output.WriteLine(
                $"  DIFFER {cell.Quantity}[{cell.Index}]: reference mean={cell.MeanFirst:G17}, " +
                $"batched mean={cell.MeanSecond:G17}, t={cell.T:G6}, df={cell.DegreesOfFreedom}.");
        }

        Assert.True(
            differing.Count == 0,
            $"{name}: batched mode ({acceleratorLabel}, Independent layout, Binary64 precision, " +
            $"{TwoSampleSeeds.Length} seeds) disagrees with reference mode (same layout and precision, same " +
            $"seeds) on {differing.Count} of {cells.Count} cells (root ACCEPTANCE.md, link 3; see test output for " +
            "each cell).");
    }

    /// <summary>
    /// Shared with `BudgetLadderTests` (deleted 2026-10-01, reproducible at 3ea6471) (2026-09-28, `src/Simulation/BOOT.md`, "## Budget
    /// selection rule"): <paramref name="attemptsPerLaunch"/> is <see langword="null"/> for every call this
    /// class's own gate theories make (they keep riding <c>SimulationOptions.AttemptsPerLaunch</c>'s own
    /// default, 32) and applies to batched runs only — reference mode never reads the budget ("## Invariants"
    /// above), so passing it for <see cref="ExecutionMode.Reference"/> would silently do nothing, which this
    /// method avoids by construction rather than by a caller's discipline. Returns the run's own
    /// <see cref="RunDiagnostics"/> alongside the parsed cells so a caller can assert the *effective* budget
    /// the call used (<see cref="RunDiagnostics.AttemptsPerLaunch"/>), not merely the option it passed.
    /// </summary>
    internal static (IReadOnlyDictionary<string, double[]> Cells, RunDiagnostics Diagnostics) RunAndParse(
        Formulation formulation, ExecutionMode mode, ulong seed, AcceleratorKind accelerator, int? attemptsPerLaunch = null)
    {
        var options = new SimulationOptions
        {
            Mode = mode,
            Accelerator = accelerator,
            Streams = StreamLayout.Independent,
            Precision = PrecisionKind.Binary64,
            Seed = seed,
        };
        if (attemptsPerLaunch is { } perLaunch && mode == ExecutionMode.Batched)
        {
            options = options with { AttemptsPerLaunch = perLaunch };
        }

        using var simulator = Simulator.Create(options);
        var result = simulator.Run(formulation);

        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, ModelParameters.Default, result, tempPath);
            return (ResultsMFile.Parse(tempPath), result.Diagnostics);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
