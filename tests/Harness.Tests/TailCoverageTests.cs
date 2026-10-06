using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// this node's BOOT.md, ## Tail coverage (2026-09-18): the five ordered steps, each proven green before the
/// next is attempted (this class's method order matches the section's numbering; the numbers each step measures
/// are recorded back into this node's ACCEPTANCE.md, not just asserted here).
/// </summary>
public class TailCoverageTests
{
    private readonly ITestOutputHelper _output;

    public TailCoverageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> Formulations()
    {
        foreach (var (name, replicaCount) in ResultsMFileTests.Formulations)
        {
            yield return [name, replicaCount];
        }
    }

    // Gate 2, redefined (Fable 5.1, `SCRATCH/fable/decision-calibration.md`, item 3;
    // `tests/Harness/HISTORY.md#gate-2-redefined-2026-09-20`): the per-cell named-exception list (`KnownOutlierCell`/`Step1KnownOutliers`/
    // `Step2KnownOutliers`/`Step3KnownOutliers`, all deleted here) is replaced by one gate over the same 96
    // leave-one-out applications the three lists used to cover individually (32 + 16*4 — HPEPA3, inpt, P33,
    // PSAN02n, HMX). One run is one (formulation, left-out replica ordinal) pair; it fails if *any* of the three
    // blind-calibration reports (`Compare`, `CompareTailRowMean`, `CompareAdaptiveIndexMatched`) reports a
    // failure for it — a run failing under more than one report, or with more than one failing cell, is still
    // one failing run.
    //
    // ⚠ 2026-09-24: this gate's own bound, "at most one failing run out of 96", is retired. It read 13/96 —
    // far past the bound's own `P(>=2) = 0.004` — and stayed there through the criterion's own rebuild (root
    // BOOT.md's "Reference mode in each layout satisfies the criterion...", the 2026-09-20 re-measurement: "13
    // failing runs of 96 ... every one of these ten figures is bit-identical"). Root BOOT.md's own decision the
    // same date this note is dated ("the pass condition compares failure rates, not single runs") reads this
    // exact 13/96 as the reason: "a single run passing or failing the per-run criterion is not evidence either
    // way". So the bound was never a live pass condition waiting to be met — it was already known unmeetable
    // when written, and a perpetually red assertion is what AGENTS.md §13 forbids ("A perpetually red check is
    // worse than an absent one"). This method keeps the loop and turns it into what it actually is now: the
    // original's own null-rate numerator (`tests/Harness/BOOT.md`, "## Null rate of the original", `p₀ = 22/197`
    // pools this 13/96 with the independent-layout leave-one-out and the lagged reference-vs-R) — a ratchet on
    // the exact failing-run *set*, not a bound on its count, the same form the retired seed-0 ratchet in
    // `tests/Simulation.Tests` used. The live pass condition is `tests/Harness.Tests/RateCriterionTests`'s own
    // rate comparison, which reads this same 13 through `NullRateCalibration`, not through this test.
    [Trait("Category", "Long")]
    [Fact]
    public void Gate2BlindCalibrationLaggedLeaveOneOutNullRateNumeratorMatchesTheRecordedSet()
    {
        // `tests/Harness/BOOT.md`, "## Null rate of the original": the loop itself lives in
        // NullRateCalibration.LeaveOneOutRuns, shared with the root criterion's own rate comparison
        // (RateCriterionTests, tests/Harness.Tests) rather than kept a second time here.
        var runs = NullRateCalibration.LeaveOneOutRuns(ReplicaKind.Lagged);
        var failingRuns = runs.Where(r => r.Failed).ToList();

        foreach (var run in failingRuns)
        {
            _output.WriteLine($"{run.Formulation} {run.Label}: {run.Failures.Count} failures: {string.Join("; ", run.Failures)}");
        }

        _output.WriteLine($"{runs.Count} total leave-one-out runs; {failingRuns.Count} failing.");

        // Non-degeneracy of the "96" figure itself: a mutation that skipped a formulation or under-counted a
        // replica set would silently narrow the numerator's own denominator.
        Assert.True(runs.Count == 96, $"expected 96 total leave-one-out runs (32 + 16*4), counted {runs.Count}.");

        var actual = failingRuns.Select(r => $"{r.Formulation} {r.Label}").OrderBy(s => s, StringComparer.Ordinal).ToList();
        var unexpected = actual.Except(KnownFailingLaggedLeaveOneOutRuns).ToList();
        var resolved = KnownFailingLaggedLeaveOneOutRuns.Except(actual).ToList();

        Assert.True(
            unexpected.Count == 0 && resolved.Count == 0,
            $"the lagged leave-one-out failing-run set no longer matches the recorded set (root BOOT.md, \"the " +
            $"pass condition compares failure rates, not single runs\"; `tests/Harness/BOOT.md`, \"## Null rate of " +
            $"the original\"). {unexpected.Count} new/unexpected run(s): {string.Join(", ", unexpected)}. " +
            $"{resolved.Count} recorded run(s) now passing: {string.Join(", ", resolved)}. Either way, update " +
            "the recorded set below and `tests/Harness/BOOT.md`, \"## Null rate of the original\" (the pooled and " +
            "per-formulation null-rate figures move with it), never loosen a band to make this pass.");
    }

    // Measured 2026-09-24, 13 of 96; re-measured 2026-09-27 after E1 (the mass bracket's feasible interval,
    // `tests/Harness/BOOT.md`, "Mass bracket"), 10 of 96 — the ratchet's own recorded set, moved by an edit only.
    // The three cells the point estimate used to fail (`HPEPA3 replica 26` on `fmkarm[66]`, `inpt replica 10` on
    // `fmkarm_cor[6]`, `P33 replica 11` on `fmkarm_cor[12]`) leave; no run gains a new failure.
    //
    // Re-measured again 2026-09-27 after E2 (the adaptive index-matched comparison's own common range,
    // `tests/Harness/HISTORY.md#e2-adaptive-common-range`), 9 of 96. `HMX replica 3` leaves: it is
    // `ShortestReplicaAsCandidateFailsAtMostAdaptiveRowCount`'s own HMX-lagged case, the shortest replica of the
    // pool, which the union rule failed on six rows compared against an impossible 0.0 and the common-range rule
    // no longer scores at all; no run gains a new failure.
    private static readonly string[] KnownFailingLaggedLeaveOneOutRuns =
    [
        "HMX replica 14",
        "HPEPA3 replica 3", "HPEPA3 replica 7",
        "P33 replica 14", "P33 replica 15", "P33 replica 2",
        "PSAN02n replica 14",
        "inpt replica 1", "inpt replica 11",
    ];

    public static IEnumerable<object[]> FormulationsAndSeedPatchedKinds()
    {
        foreach (var (name, _) in ResultsMFileTests.Formulations)
        {
            yield return [name, ReplicaKind.Lagged];
            yield return [name, ReplicaKind.Independent];
        }
    }

    private static string SeedPatchedReplicaDirectory(ReplicaKind kind) => kind switch
    {
        ReplicaKind.Lagged => "replicas-lagged",
        ReplicaKind.Independent => "replicas-independent",
        ReplicaKind.Gsv3 => throw new ArgumentOutOfRangeException(nameof(kind), kind, "this test covers the two seed-patched replica kinds only."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "this test covers the two seed-patched replica kinds only."),
    };

    // E2's own right control (`tests/Harness/HISTORY.md#e2-adaptive-common-range`): the shortest replica of a
    // formulation's own pool, used as the candidate against the rest, genuinely has fewer adaptive rows than at
    // least one other replica — `AdaptiveRowCount` is built to catch exactly that, so it may fail — but every
    // adaptive-position cell inside the common range it does still reach must compare clean; under the old union
    // rule, the rows the candidate lacked compared against 0.0 and failed instead. Measured before the fix: HMX
    // lagged replica 3 failed 6 cells, HMX independent replica 11 failed 3, both now 0.
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(FormulationsAndSeedPatchedKinds))]
    public void ShortestReplicaAsCandidateFailsAtMostAdaptiveRowCount(string formulation, ReplicaKind kind)
    {
        var replicaCount = ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount;
        var directory = SeedPatchedReplicaDirectory(kind);
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var i0 = CategoryAxis.FixedWidthPrefixLength(ResultsMFile.Parse(referencePath)["Dkarmcat"]);

        var shortestOrdinal = -1;
        var shortestAdaptiveLength = int.MaxValue;
        var tiedAtShortest = 0;
        for (var k = 1; k <= replicaCount; k++)
        {
            var path = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, k + ".m.txt");
            var replica = ResultsMFile.Parse(path);
            var adaptiveLength = replica.TryGetValue("Dkarmcat", out var dkarmcat) ? Math.Max(0, dkarmcat.Length - i0) : 0;
            if (adaptiveLength < shortestAdaptiveLength)
            {
                shortestAdaptiveLength = adaptiveLength;
                shortestOrdinal = k;
                tiedAtShortest = 1;
            }
            else if (adaptiveLength == shortestAdaptiveLength)
            {
                tiedAtShortest++;
            }
        }

        Assert.True(shortestOrdinal > 0, $"{formulation}/{kind}: no replica found.");

        // HMX is the design's own worked example (`tests/Harness/HISTORY.md#e2-adaptive-common-range`): its
        // shortest replica is strictly shortest, no tie, at the ordinal the design measured, so pinning both
        // pins the case the design's own figures (6 and 3 failures under the old union rule) were measured
        // against. Every other formulation's own pool ties several replicas at the minimum adaptive length
        // (measured directly here, `tiedAtShortest > 1`); the "lowest ordinal on ties" rule the loop above
        // already applies covers that without needing a pinned ordinal for each.
        if (formulation == "HMX")
        {
            Assert.Equal(1, tiedAtShortest);
            Assert.Equal(kind == ReplicaKind.Lagged ? 3 : 11, shortestOrdinal);
        }

        var candidatePath = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, shortestOrdinal + ".m.txt");
        var candidate = ResultsMFile.Parse(candidatePath);
        var report = StatisticalCriterion.CompareAdaptiveIndexMatched(formulation, candidate, kind, excludeReplicaOrdinal: shortestOrdinal);

        _output.WriteLine(
            $"{formulation}/{kind} replica {shortestOrdinal} (shortest, {shortestAdaptiveLength} adaptive rows): " +
            $"{report.Failures.Count} failures: {string.Join("; ", report.Failures)}.");

        var unexpected = report.Failures.Where(f => !(f.Quantity == "AdaptiveRowCount" && f.Index == 0)).ToList();
        Assert.True(
            unexpected.Count == 0,
            $"{formulation}/{kind}: expected the shortest replica's own failures to be at most AdaptiveRowCount[0], " +
            $"found: {string.Join("; ", unexpected)}.");
    }

    // this node's BOOT.md, "Fable 5.1 decision II: the calibration curve (2026-09-20)": the 0.05 diagnostic above
    // is deleted, replaced by `CalibrationCurveTests.Gate3CalibrationCurveWithinBinomialBandPerRulePerLevel` —
    // a per-rule, non-degenerate-cells-only, three-level GATE rather than one pooled fraction at one level
    // (decision II, item 4: "replace the diagnostic by the per-rule non-degenerate calibration curve, as a
    // gate"). `StatisticalCriterion.CompareWithAlpha`, the seam this diagnostic alone used, is removed with it;
    // the calibration curve uses `CompareCellVerdicts` and its two siblings instead.

    // Step 4: "Measured sensitivity, written down as numbers. On the reference's own printed file, without
    // rerunning the executable." Run on HMX specifically: it is the formulation the section itself is written
    // for (root BOOT.md's own worked example, "row 31's own boundary already ranges 330-350 mkm", is HMX; the
    // wave-4 investigation this section answers, SCRATCH/wave4/hmx-exclusions.md, is HMX-only), and it is the
    // only one of the five reference formulations whose adaptive tail is more than a couple of rows deep (31
    // rows, this node's BOOT.md's own headline figure) — the other four's single- or double-row tails would make
    // a "roll every tail row" or "truncate the last k rows" sweep close to meaningless.
    private const string SweepFormulation = "HMX";

    // (a) "roll every tail row one column and mix rho * rolled + (1 - rho) * original, sweep rho, record the
    // smallest rho the tail mean catches." A one-column shift toward the smaller-diameter end, zero-filling the
    // vacated far column, not a physical transform (root BOOT.md Taboos' ban on a second implementation of any
    // part of the model) — it only needs to move mass between neighbouring columns by a known amount so the sensitivity of
    // CompareTailRowMean to a shape change can be measured. A *cyclic* shift was tried first and rejected: each
    // fqdokkarm row spans several orders of magnitude from its largest (near-mode) column to its smallest (far)
    // one, so wrapping the largest column's value into the smallest column's slot manufactures a huge, artificial
    // jump at the wrap seam that has nothing to do with rho — every rho above 0 "caught" the check at that one
    // seam column regardless of how small rho was, measuring the wrap artifact, not the check's real sensitivity.
    [Fact]
    public void Step4aTailRowMeanSensitivityRollAndMixSweep()
    {
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", SweepFormulation, "results.m.txt");
        var reference = ResultsMFile.Parse(referencePath);
        var dkarmcat = reference["Dkarmcat"];
        var i0 = CategoryAxis.FixedWidthPrefixLength(dkarmcat);

        // A coarse 2% grid caught the very first step tried, so the grid itself — not a genuine sensitivity
        // floor — would be reported; a finer 0.1% grid below that first coarse hit narrows the figure this node's
        // BOOT.md records to three significant figures instead of "somewhere under 2%".
        var rhoSteps = Enumerable.Range(1, 19).Select(i => i * 0.001).Concat(Enumerable.Range(1, 49).Select(i => i * 0.02));

        var smallestCaughtRho = double.NaN;
        foreach (var rho in rhoSteps)
        {
            var candidate = new Dictionary<string, double[]>(reference);
            for (var row = i0 + 1; row <= dkarmcat.Length; row++)
            {
                var key = $"fqdokkarm({row},:)";
                if (!reference.TryGetValue(key, out var original))
                {
                    continue;
                }

                var rolled = new double[original.Length];
                for (var i = 0; i < original.Length; i++)
                {
                    rolled[i] = i + 1 < original.Length ? original[i + 1] : 0.0;
                }

                var mixed = new double[original.Length];
                for (var i = 0; i < original.Length; i++)
                {
                    mixed[i] = rho * rolled[i] + (1.0 - rho) * original[i];
                }

                candidate[key] = mixed;
            }

            var report = StatisticalCriterion.CompareTailRowMean(SweepFormulation, candidate, ReplicaKind.Lagged);
            if (report.Failures.Any(f => f.Quantity == "TailRowMean"))
            {
                smallestCaughtRho = rho;
                _output.WriteLine($"  first caught at rho={rho:G3}: {string.Join("; ", report.Failures.Take(3))}");
                break;
            }
        }

        _output.WriteLine($"{SweepFormulation}: smallest rho the tail-row-mean check caught = {smallestCaughtRho:G3} (0.1% grid below 2%, 2% grid above).");
        Assert.False(double.IsNaN(smallestCaughtRho), "the sweep never caught a roll-and-mix mutation up to rho = 1.0.");
    }

    // (b) "stretch the adaptive boundaries B(i) <- B(i0) + (1 + delta)(B(i) - B(i0)) snapped to the grid, sweep
    // delta = 1, 2, 5, 10%, record the smallest caught." B(i0) is the first adaptive row's own boundary (the
    // anchor the stretch is measured from, so the fixed-width prefix and the first adaptive row itself never
    // move); "snapped to the grid" is the formulation's own base step (Dkarmcat's first entry, this node's
    // BOOT.md's `FixedWidthPrefixLength`), the same grid the original prints every fixed-width boundary on.
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.02)]
    [InlineData(0.05)]
    [InlineData(0.10)]
    public void Step4bAdaptiveLastBoundarySensitivityStretchSweep(double delta)
    {
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", SweepFormulation, "results.m.txt");
        var reference = ResultsMFile.Parse(referencePath);
        var dkarmcat = reference["Dkarmcat"];
        var i0 = CategoryAxis.FixedWidthPrefixLength(dkarmcat);
        var step = dkarmcat[0];
        var anchor = dkarmcat[i0];

        var stretched = (double[])dkarmcat.Clone();
        for (var i = i0; i < stretched.Length; i++)
        {
            var raw = anchor + (1.0 + delta) * (dkarmcat[i] - anchor);
            stretched[i] = Math.Round(raw / step) * step;
        }

        var candidate = new Dictionary<string, double[]>(reference) { ["Dkarmcat"] = stretched };
        var report = StatisticalCriterion.CompareAdaptiveIndexMatched(SweepFormulation, candidate, ReplicaKind.Lagged);
        var caught = report.Failures.Any(f => f.Quantity is "AdaptiveLastBoundaryLog" or "Dkarmcat@adaptive");

        _output.WriteLine(
            $"{SweepFormulation}: delta={delta:P0}, caught={caught}, failures={string.Join("; ", report.Failures)}.");

        // The comparison itself must have actually run (this node's BOOT.md, ## Tail coverage (2026-09-18),
        // step 4: a sweep that silently compares nothing would report "not caught" for a trivial reason, not the
        // declared low power this step is measuring). Whether any given delta trips a failure is the measurement
        // this step reports, not something this test asserts either way (root BOOT.md Taboos: "no expected value
        // typed into a test" — the smallest-caught delta, if any of the four trips at all, is recorded in this
        // node's ACCEPTANCE.md from the logged output, not hard-coded here).
        Assert.True(report.Compared > 0, $"delta={delta:P0} compared 0 cells; the mutation did not reach the comparison.");
    }

    // (c) "truncate the last k rows, record the smallest k the count and last-boundary bands trip at." Dkarmcat,
    // dokkarm43 and dokkarm10 share one row axis (this node's BOOT.md, ## Invariants, "Canonical category axis"),
    // so all three are truncated together, the same way a run whose own adaptive binning simply stopped k rows
    // earlier would print them.
    [Fact]
    public void Step4cAdaptiveRowCountAndLastBoundarySensitivityTruncationSweep()
    {
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", SweepFormulation, "results.m.txt");
        var reference = ResultsMFile.Parse(referencePath);
        var dkarmcat = reference["Dkarmcat"];
        var i0 = CategoryAxis.FixedWidthPrefixLength(dkarmcat);
        var adaptiveRows = dkarmcat.Length - i0;

        var smallestCaughtK = -1;
        for (var k = 1; k < adaptiveRows; k++)
        {
            var newLength = dkarmcat.Length - k;
            var candidate = new Dictionary<string, double[]>(reference)
            {
                ["Dkarmcat"] = dkarmcat[..newLength],
                ["dokkarm43"] = reference["dokkarm43"][..Math.Min(newLength, reference["dokkarm43"].Length)],
                ["dokkarm10"] = reference["dokkarm10"][..Math.Min(newLength, reference["dokkarm10"].Length)],
            };

            var report = StatisticalCriterion.CompareAdaptiveIndexMatched(SweepFormulation, candidate, ReplicaKind.Lagged);
            if (report.Failures.Any(f => f.Quantity is "AdaptiveRowCount" or "AdaptiveLastBoundaryLog"))
            {
                smallestCaughtK = k;
                break;
            }
        }

        _output.WriteLine($"{SweepFormulation}: smallest k the count/last-boundary bands caught = {smallestCaughtK} (of {adaptiveRows} adaptive rows).");
        Assert.True(smallestCaughtK > 0, $"the sweep never caught a truncation up to {adaptiveRows - 1} rows.");
    }
}
