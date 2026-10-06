using System.Collections.Concurrent;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// this node's BOOT.md, levels table, footnote `[8]`: <see cref="StatisticalCriterion.CompareSets"/> read
/// against the pregenerated 320 runs of `tests/Fixtures/rate-runs` (<c>tests/RateTableTool</c> writes them;
/// <see cref="RateRuns.Load"/> reads them back in seed order) and the declared differences of
/// `docs/declared-differences.json` (<see cref="DeclaredDifferences"/>). Four questions, none of them the
/// per-run question <see cref="RateCriterionTests"/> already answers:
/// <list type="number">
/// <item>under `Original` precision, every cell a set of sixteen candidates is found to differ on from its own
/// replica set is named by a declared-differences entry, and every entry scoped to the formulation earns its
/// keep by covering at least one such difference — gated on the `Lagged` layout for every formulation;
/// `Independent` is gated for four of the five, and reported only for PSAN02n
/// (<see cref="OriginalDiffersOnlyInDeclaredCellsIndependentReportedOnlyForPsan02n"/>, this node's BOOT.md,
/// footnote `[8]`: the epsdokfr exclusion the design session proposed did not clear the tree's own alpha).</item>
/// <item>under `Binary64` precision, the same sets differ from the same replicas *outside* the declared cells
/// too — the REAL*4 degradation `Binary64` never reproduces (root BOOT.md, "Precision kind") — gated on
/// HPEPA3/HMX, whose per-run positive control (<see cref="RateCriterionTests.PortRateBinary64PrecisionIsSignificantlyHigherThanTheOriginalsNullRateOnHpepa3AndHmx"/>)
/// already rejects there, reported for the rest.</item>
/// <item>both of <see cref="SetCriterionReport"/>'s own negative controls — a homogeneous set split against
/// itself — are empty everywhere.</item>
/// <item>inpt's `epsdokfr[0]` under `Binary64`, constant on both sides and different, is found by
/// <em>value</em> as a constant-cell mismatch (`|T| = Infinity`) in both layouts: the exact bound's positive
/// control.</item>
/// </list>
/// </summary>
public class SetCriterionTests(ITestOutputHelper output)
{
    private static readonly string[] Layouts = ["Original", "Independent"];
    private static readonly string[] Precisions = ["Binary64", "Original"];

    private static ReplicaKind KindOf(string layout) => layout switch
    {
        // tests/RateTableTool/Program.cs's own mapping, restated independently here (RateCriterionTests.cs's
        // own comment on `ReplicaKindsUsedByRateRuns` names the same pair): a `rate-runs` set of the `Original`
        // layout validates against `ReplicaKind.Lagged`, and one of the `Independent` layout against
        // `ReplicaKind.Independent`.
        "Original" => ReplicaKind.Lagged,
        "Independent" => ReplicaKind.Independent,
        _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, message: null),
    };

    // Loading sixteen rate-runs files and running three Welch passes over every printed cell is not free; every
    // (formulation, layout, precision) triple is read by more than one test method below (SetNullsAreEmpty
    // alone needs all twenty), so each is computed once and shared, the same reasoning RateCriterionTests.cs's
    // own LoadTable would use if it were called from more than one place.
    private static readonly ConcurrentDictionary<(string Formulation, string Layout, string Precision), Lazy<SetCriterionReport>> ReportCache = new();

    private static SetCriterionReport ReportFor(string formulation, string layout, string precision) =>
        ReportCache.GetOrAdd(
            (formulation, layout, precision),
            key => new Lazy<SetCriterionReport>(() =>
            {
                var candidates = RateRuns.Load(key.Formulation, key.Layout, key.Precision);
                return StatisticalCriterion.CompareSets(key.Formulation, candidates, KindOf(key.Layout));
            })).Value;

    /// <summary>Every (formulation, layout) pair except PSAN02n/`Independent`, whose own gate is reported, not
    /// asserted, by <see cref="OriginalDiffersOnlyInDeclaredCellsIndependentReportedOnlyForPsan02n"/> instead
    /// (this node's BOOT.md, footnote `[8]`).</summary>
    public static IEnumerable<object[]> GatedFormulationLayoutPairs()
    {
        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in Layouts)
            {
                if (string.Equals(formulation, "PSAN02n", StringComparison.Ordinal) && string.Equals(layout, "Independent", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return [formulation, layout];
            }
        }
    }

    public static IEnumerable<object[]> AllFormulationLayoutPairs()
    {
        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in Layouts)
            {
                yield return [formulation, layout];
            }
        }
    }

    public static IEnumerable<object[]> AllFormulationLayoutPrecisionTriples()
    {
        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in Layouts)
            {
                foreach (var precision in Precisions)
                {
                    yield return [formulation, layout, precision];
                }
            }
        }
    }

    /// <summary>The ten (formulation, layout) pairs' own <see cref="SetCriterionReport.Compared"/> count, read
    /// once against a real, unmutated build and recorded here as a ratchet (this node's BOOT.md's own mutation
    /// table has the non-degeneracy proof: neither the canonical-axis alignment gate nor the zero-fill rule
    /// moves any of <see cref="OriginalDiffersOnlyInDeclaredCells"/>'s own assertions when disabled — both
    /// change <c>m</c>, the count of cells actually compared, well before either could change which of them
    /// differ — so a bare "differs"/"stale" check alone cannot see either rule break; this count can, and does).</summary>
    public static IEnumerable<object[]> RecordedComparedCounts()
    {
        yield return ["HPEPA3", "Original", 2110];
        yield return ["HPEPA3", "Independent", 1962];
        yield return ["inpt", "Original", 2621];
        yield return ["inpt", "Independent", 2582];
        yield return ["P33", "Original", 1561];
        yield return ["P33", "Independent", 1559];
        yield return ["PSAN02n", "Original", 2099];
        yield return ["PSAN02n", "Independent", 2114];
        yield return ["HMX", "Original", 6437];
        yield return ["HMX", "Independent", 4596];
    }

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(RecordedComparedCounts))]
    public void ComparedCellCountsMatchTheRecordedBaseline(string formulation, string layout, int expectedCompared)
    {
        var report = ReportFor(formulation, layout, "Original");
        output.WriteLine($"{formulation}/{layout}: compared={report.Compared} (recorded {expectedCompared}).");
        Assert.Equal(expectedCompared, report.Compared);
    }

    private static void ReportUncoveredAndStale(
        ITestOutputHelper output, string formulation, SetCriterionReport report, DeclaredDifferences declared,
        out List<TwoSampleCell> uncovered, out List<DeclaredDifferences.Entry> stale)
    {
        uncovered = [.. report.Differences.Where(d => !declared.Covers(formulation, d.Quantity, d.Index, d.MeanSecond))];
        foreach (var cell in uncovered)
        {
            output.WriteLine($"  uncovered: {cell.Quantity}[{cell.Index}] T={cell.T:G6} meanFirst={cell.MeanFirst:G6} meanSecond={cell.MeanSecond:G6}");
        }

        stale = [.. declared.EntriesScopedTo(formulation)
            .Where(entry => !report.Differences.Any(d => entry.Covers(d.Quantity, d.Index, d.MeanSecond)))];
        foreach (var entry in stale)
        {
            output.WriteLine($"  stale entry: quantity={entry.Quantity}, formulations=[{string.Join(", ", entry.Formulations)}]");
        }
    }

    /// <summary>Gated: every `Original`-precision set-vs-replica difference is named by a declared-differences
    /// entry, and every entry scoped to the formulation covers at least one difference in this (layout, kind)
    /// pair — a row that never shows up has gone stale.</summary>
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(GatedFormulationLayoutPairs))]
    public void OriginalDiffersOnlyInDeclaredCells(string formulation, string layout)
    {
        var report = ReportFor(formulation, layout, "Original");
        var declared = DeclaredDifferences.Load();
        output.WriteLine($"{formulation}/{layout}, Original precision: compared={report.Compared}, excluded={report.Excluded}, differing={report.Differences.Count}");
        ReportUncoveredAndStale(output, formulation, report, declared, out var uncovered, out var stale);

        Assert.Empty(uncovered);
        Assert.Empty(stale);
    }

    /// <summary>PSAN02n's own `Independent`-layout subset check (this node's BOOT.md, footnote `[8]`): the
    /// epsdokfr exclusion the design session proposed for it does not hold at the tree's own alpha
    /// (<c>ReplicaArcEvidenceTests</c>, the coordinator-ruled Spearman trend test), so this pair is reported,
    /// never gated — root BOOT.md's own design fallback for a subset check with no exclusion to lean on.</summary>
    [Trait("Category", "Long")]
    [Fact]
    public void OriginalDiffersOnlyInDeclaredCellsIndependentReportedOnlyForPsan02n()
    {
        const string Formulation = "PSAN02n";
        const string Layout = "Independent";
        var report = ReportFor(Formulation, Layout, "Original");
        var declared = DeclaredDifferences.Load();
        output.WriteLine($"{Formulation}/{Layout}, Original precision: compared={report.Compared}, excluded={report.Excluded}, differing={report.Differences.Count} (reported only, not gated).");
        ReportUncoveredAndStale(output, Formulation, report, declared, out var uncovered, out var stale);
        output.WriteLine($"  {uncovered.Count} uncovered cell(s), {stale.Count} stale entry(ies) — neither asserted.");
    }

    /// <summary>Gated on HPEPA3/HMX, the same two formulations <see cref="RateCriterionTests.PortRateBinary64PrecisionIsSignificantlyHigherThanTheOriginalsNullRateOnHpepa3AndHmx"/>
    /// gates: `Binary64` candidates differ from the same replicas at cells no declared-differences entry names
    /// — root BOOT.md's own "differs by design wherever the original's REAL*4 storage degrades the original's
    /// own answer", read here as an undeclared `fmdok` cell specifically, the family
    /// `tests/Harness.Tests/BOOT.md`'s own footnote `[5]` already found REAL*4-sensitive on exactly these two
    /// formulations. Reported, not gated, for the other three.</summary>
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(AllFormulationLayoutPairs))]
    public void Binary64DiffersOutsideTheDeclaredCells(string formulation, string layout)
    {
        var report = ReportFor(formulation, layout, "Binary64");
        var declared = DeclaredDifferences.Load();
        var undeclaredFmdok = report.Differences
            .Where(d => string.Equals(d.Quantity, "fmdok", StringComparison.Ordinal) && !declared.Covers(formulation, d.Quantity, d.Index, d.MeanSecond))
            .ToList();

        output.WriteLine(
            $"{formulation}/{layout}, Binary64 precision: differing={report.Differences.Count}, undeclared fmdok cells={undeclaredFmdok.Count}");

        if (formulation is "HPEPA3" or "HMX")
        {
            Assert.True(
                undeclaredFmdok.Count > 0,
                $"{formulation}/{layout}: expected at least one undeclared fmdok cell under Binary64 precision " +
                "(the REAL*4 degradation Binary64 never reproduces), found none.");
        }
    }

    /// <summary>Both of <see cref="SetCriterionReport"/>'s own negative controls, over all twenty (formulation,
    /// layout, precision) triples: a homogeneous set split against itself must never differ from itself, under
    /// either precision kind — the method's own false-positive rate, not a real difference.</summary>
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(AllFormulationLayoutPrecisionTriples))]
    public void SetNullsAreEmpty(string formulation, string layout, string precision)
    {
        var report = ReportFor(formulation, layout, precision);
        output.WriteLine(
            $"{formulation}/{layout}/{precision}: candidate-half differences={report.CandidateHalvesDifferences.Count}, " +
            $"replica-half differences={report.ReplicaHalvesDifferences.Count}");

        Assert.Empty(report.CandidateHalvesDifferences);
        Assert.Empty(report.ReplicaHalvesDifferences);
    }

    /// <summary>The constant-cell exact bound's positive control, on a cell whose answer is known beforehand:
    /// inpt's `epsdokfr[0]` prints `0.000E+00` in every `Binary64` rate run, which computes the share
    /// normalization of `NMM = 1` exactly, and `0.270E-07` in every output of the original, reference and
    /// replicas of both layouts alike, its REAL*4 floor (`src/Statistics/BOOT.md`, "## Defects of the
    /// original", row 792–797). Both sides constant and different, so it must be found by value in both
    /// layouts as a constant-cell mismatch (`|T| = Infinity`, <see cref="ConstantCellRule.ExactBound"/>'s own
    /// exact-probability floor cleared). It replaces PSAN02n's `da_coef` under `Original` (2026-10-01): the
    /// per-cycle plane now reproduces that cell, so it no longer differs.</summary>
    [Trait("Category", "Long")]
    [Fact]
    public void EpsdokfrOnInptUnderBinary64IsFoundAsAConstantCellMismatch()
    {
        const string Formulation = "inpt";

        foreach (var layout in Layouts)
        {
            var report = ReportFor(Formulation, layout, "Binary64");
            var cells = report.Differences
                .Where(d => string.Equals(d.Quantity, "epsdokfr", StringComparison.Ordinal) && d.Index == 0)
                .ToList();
            output.WriteLine($"{Formulation}/{layout}, Binary64 precision: epsdokfr[0] differing cells={cells.Count}");

            var cell = Assert.Single(cells);
            Assert.True(double.IsInfinity(cell.T), $"{Formulation}/{layout} epsdokfr[0]: expected |T| = Infinity, got {cell.T:G6}.");
        }
    }

    /// <summary>Design §6 step 5, "zero-fill" (a direct unit test of the mutation-and-revert proof recorded in
    /// this node's BOOT.md): every real candidate's own `fmkarm_cor` shortened by three elements — the
    /// replicas untouched — must be found, not silently dropped. The zero-fill rule (this node's BOOT.md, "##
    /// Set comparison") pads the shortened tail with zeros rather than excluding it, so those three positions
    /// compare sixteen zeros against sixteen real (non-zero) replica values: an ordinary Welch cell whose
    /// standard error is not zero (the candidate side is constant only in isolation), so it reports through the
    /// ordinary branch, not the constant-cell exact bound. A defect that instead treated the shortened tail as
    /// absent (excluded, like a cell with fewer than two contributors) would make it vanish silently.</summary>
    /// <summary>Design §6 step 5, "zero-fill": every real candidate's own `fmkarm_cor` shortened by three
    /// elements (replicas untouched), through the full <see cref="StatisticalCriterion.CompareSets"/> pipeline
    /// — a direct measurement of the mechanism, not the (already recorded) mutation-and-revert proof of
    /// `ZeroFillOrdinaryToUnionLength` itself (this node's BOOT.md mutation table). P33/`Original` is gated:
    /// its own `fmkarm_cor` is short enough that the tail's natural inter-replica spread does not swamp the
    /// three zeroed cells, so they are found (2 cells). The other nine pairs are reported only, not gated: at
    /// their own, longer arrays the same perturbation does not clear alpha against the natural spread at those
    /// far-tail positions (root BOOT.md, "a verdict may rest only on a figure of the matching kind" — a report
    /// with no power at nine of ten pairs is not evidence the rule is absent there, only that this one
    /// perturbation is too small to see against their own noise).</summary>
    [Trait("Category", "Long")]
    [Fact]
    public void CandidatesWithFmkarmCorShortenedByThreeMeasuredAgainstRealData()
    {
        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in Layouts)
            {
                var candidates = RateRuns.Load(formulation, layout, "Original");
                var shortened = new List<IReadOnlyDictionary<string, double[]>>(candidates.Count);
                foreach (var candidate in candidates)
                {
                    if (candidate.TryGetValue("fmkarm_cor", out var values) && values.Length > 3)
                    {
                        var clone = new Dictionary<string, double[]>(candidate, StringComparer.Ordinal)
                        {
                            ["fmkarm_cor"] = values[..^3],
                        };
                        shortened.Add(clone);
                    }
                    else
                    {
                        shortened.Add(candidate);
                    }
                }

                var kind = string.Equals(layout, "Original", StringComparison.Ordinal) ? ReplicaKind.Lagged : ReplicaKind.Independent;
                var report = StatisticalCriterion.CompareSets(formulation, shortened, kind);
                var fmkarmCorDifferences = report.Differences.Where(d => string.Equals(d.Quantity, "fmkarm_cor", StringComparison.Ordinal)).ToList();
                output.WriteLine($"{formulation}/{layout}: fmkarm_cor differing cells after shortening every candidate by 3 = {fmkarmCorDifferences.Count}");
                foreach (var cell in fmkarmCorDifferences.Take(3))
                {
                    output.WriteLine($"  fmkarm_cor[{cell.Index}]: T={cell.T:G6}, meanFirst={cell.MeanFirst:G6}, meanSecond={cell.MeanSecond:G6}");
                }

                if (string.Equals(formulation, "P33", StringComparison.Ordinal) && string.Equals(layout, "Original", StringComparison.Ordinal))
                {
                    Assert.NotEmpty(fmkarmCorDifferences);
                }
            }
        }
    }
}
