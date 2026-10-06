using System.Globalization;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// The known-answer controls of B2a (`tests/Harness/HISTORY.md#b2a-quantum-identification-2026-10-02`): rows of
/// exact integer counts of one known step `s`, printed to three significant digits the way the original prints
/// them, passed through <see cref="RunQuantum.TryInfer"/> by the same path <c>Compare</c> uses
/// (<see cref="PrintResolution.BuildSyntheticCells"/>), and judged against `s`, which no run of the criterion
/// ever knows. K1 is the right control (the step the search returns, and whether it says it is identified);
/// K2 is the no-movement control (where the step was already right, it stays right).
/// </summary>
public sealed class QuantumIdentificationTests
{
    // The one known step: its mantissa, 3.7, is what lets a row of up to about 270 counts stay resolvable at
    // three printed significant digits, the regime the review's scratch program measured.
    private const double TrueStep = 3.7e-6;
    private const int PrintedDigits = 3;
    private const int RowsPerSmallestCount = 200;
    private const int RowCells = 40;
    private const long Seed = 20261002;

    private static readonly int[] SmallestCounts = [1, 3, 10, 30, 100, 300];

    private readonly ITestOutputHelper _output;

    public QuantumIdentificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // One row of exact counts: a bell-shaped profile whose peak is 50 to 249 times the nominal smallest count, every
    // cell below that count dropped, each kept cell given a little integer noise so no row is a clean multiple of
    // anything but the step itself. The nominal count is a label; the row's own smallest count is what is returned.
    private static KnownRow BuildRow(SplitMix64 rng, int nominalSmallestCount)
    {
        var counts = new List<long>();
        var peak = nominalSmallestCount * (50 + rng.Next(200));
        for (var i = 0; i < RowCells; i++)
        {
            var z = (i - RowCells / 2) / 6.0;
            var count = (long)Math.Round(peak * Math.Exp(-(z * z)));
            if (count >= nominalSmallestCount)
            {
                counts.Add(count + rng.Next(nominalSmallestCount / 3 + 1));
            }
        }

        return KnownRow.FromCounts(counts);
    }

    // A single 1-count cell over a bulk of 200 to 500 counts: the shape the count floor exists for.
    private static KnownRow BuildStrayRow(SplitMix64 rng)
    {
        var counts = new List<long> { 1 };
        for (var i = 0; i < RowCells - 1; i++)
        {
            counts.Add(rng.Next(200, 501));
        }

        return KnownRow.FromCounts(counts);
    }

    // The step the search returns for a row, when it is identified.
    private static RunQuantum.QuantumEstimate? IdentifiedStep(KnownRow row) =>
        RunQuantum.TryInfer(PrintResolution.BuildSyntheticCells(row.Printed, PrintedDigits), out var estimate) && estimate.Identified
            ? estimate
            : null;

    // The step the search returns for a row, identified or not: what K2 compares, since B2a moves what is said of
    // a step, never the step itself.
    private static RunQuantum.QuantumEstimate? FoundStep(KnownRow row) =>
        RunQuantum.TryInfer(PrintResolution.BuildSyntheticCells(row.Printed, PrintedDigits), out var estimate)
            ? estimate
            : null;

    private static bool IsTrueStep(RunQuantum.QuantumEstimate estimate, KnownRow row) =>
        Math.Abs(estimate.Quantum - TrueStep) <= row.TrueResolutionOfStep;

    // K1, right (`tests/Harness/HISTORY.md#b2a-quantum-identification-2026-10-02`, amended by
    // `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`). No row returns an identified step other than the true
    // one, and every row of smallest count 1 or 3 identifies the true step. Its kind: a known-answer count on
    // 1200 fixed rows, not a rate. Another row set may show a wrong identification, at up to `alpha` per array by
    // the design's own bound (a scratch replica: 1 in 32,000 rows); that is judged against the bound, never by
    // retuning this control. Red on the code before B2a (765 wrong rows), and red with step 1 reverted, step 2
    // reverted, or `alpha` in place of `alpha / K` in step 2.
    [Fact]
    public void K1RowsOfKnownCountsNeverIdentifyAStepOtherThanTheTrueOne()
    {
        var rng = new SplitMix64(Seed);
        var wrong = new List<string>();
        var unidentifiedSmall = new List<string>();
        foreach (var nominal in SmallestCounts)
        {
            var found = 0;
            var identifiedTrue = 0;
            var ratios = new List<double>();
            for (var trial = 0; trial < RowsPerSmallestCount; trial++)
            {
                var row = BuildRow(rng, nominal);
                var step = IdentifiedStep(row);
                if (step is { } estimate)
                {
                    found++;
                    ratios.Add(estimate.Quantum / TrueStep);
                    if (IsTrueStep(estimate, row))
                    {
                        identifiedTrue++;
                    }
                    else
                    {
                        wrong.Add($"smallest count {nominal} (row's own {row.SmallestCount}): step {estimate.Quantum / TrueStep:F2} x true");
                    }
                }
                else if (nominal <= 3)
                {
                    unidentifiedSmall.Add($"smallest count {nominal} (row's own {row.SmallestCount}): no identified step");
                }
            }

            ratios.Sort();
            var median = ratios.Count > 0 ? ratios[ratios.Count / 2] : double.NaN;
            _output.WriteLine(
                $"smallest count {nominal,3}: identified {found,3} of {RowsPerSmallestCount}, true step {identifiedTrue,3}, " +
                $"median step/true {median.ToString("F2", CultureInfo.InvariantCulture)}");
        }

        Assert.True(wrong.Count == 0, $"{wrong.Count} rows identified a step other than the true one, e.g. {string.Join("; ", wrong.Take(5))}");
        Assert.True(unidentifiedSmall.Count == 0, $"{unidentifiedSmall.Count} rows of smallest count 1 or 3 identified no step, e.g. {string.Join("; ", unidentifiedSmall.Take(5))}");
    }

    // K2, no movement where the step was right. Rows of smallest count 1 return the true step, as the search
    // always did; so do the rows of one 1-count cell over a bulk of 200 to 500 whenever the search returns a step
    // at all (the figures are printed so the same run can be read before and after B2a).
    [Fact]
    public void K2RowsWhoseStepWasAlreadyRightKeepTheTrueStep()
    {
        var rng = new SplitMix64(Seed);
        var moved = new List<string>();
        var found = 0;
        for (var trial = 0; trial < RowsPerSmallestCount; trial++)
        {
            var row = BuildRow(rng, 1);
            if (FoundStep(row) is { } estimate && IsTrueStep(estimate, row))
            {
                found++;
            }
            else
            {
                moved.Add($"min-1 row {trial}: step {(FoundStep(row)?.Quantum ?? double.NaN) / TrueStep:F2} x true");
            }
        }

        var strayFound = 0;
        for (var trial = 0; trial < RowsPerSmallestCount; trial++)
        {
            var row = BuildStrayRow(rng);
            if (FoundStep(row) is not { } estimate)
            {
                continue;
            }

            if (IsTrueStep(estimate, row))
            {
                strayFound++;
            }
            else
            {
                moved.Add($"stray row {trial}: step {estimate.Quantum / TrueStep:F2} x true");
            }
        }

        _output.WriteLine($"min-1 rows: true step {found} of {RowsPerSmallestCount}; stray-1 rows: step returned and true {strayFound} of {RowsPerSmallestCount}");
        Assert.True(moved.Count == 0, $"{moved.Count} rows no longer return the true step, e.g. {string.Join("; ", moved.Take(5))}");
    }

    // K3, the governing term. inpt's `Count`/`Student` cells at the 0.05 level over its leave-one-out runs, the
    // same cells and path the calibration curve reads, split by the term that governs them and by the replicas'
    // pooled count against `CountFloor.HeavyTailRegimeThreshold`, the boundary step 3 reads
    // (`tests/Harness/HISTORY.md#b2a-amended-2026-10-02`). The sparse `Student` row's failing cells are named,
    // each with the runs behind its failures.
    [Fact]
    public void K3ReportsInptsCellsByGoverningTermAndPooledCount()
    {
        const string formulation = "inpt";
        const double alpha = 0.05;
        var replicaCount = ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount;
        var table = new Dictionary<string, (long N, long K)>();
        var sparseStudentFailures = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
        for (var k = 1; k <= replicaCount; k++)
        {
            var candidate = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt"));
            var verdicts = new List<CellVerdict>(StatisticalCriterion.CompareCellVerdicts(
                formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k));
            verdicts.AddRange(StatisticalCriterion.CompareTailRowMeanCellVerdicts(
                formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k));
            verdicts.AddRange(StatisticalCriterion.CompareAdaptiveIndexMatchedCellVerdicts(
                formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k));
            foreach (var v in verdicts)
            {
                if (v.Rule == CalibrationRule.Mass || v.Degenerate || !v.Eligible || v.Quantum is null)
                {
                    continue;
                }

                var density = v.PooledCount switch
                {
                    null => "no count",
                    >= CountFloor.HeavyTailRegimeThreshold => "dense",
                    _ => "sparse",
                };
                var key = $"{density} {v.Rule}{(v.Rule == CalibrationRule.Count && v.AttainedAlpha is null ? " (level unknown)" : string.Empty)}";
                var (n, failures) = table.GetValueOrDefault(key);
                table[key] = (n + 1, failures + (v.Failed ? 1 : 0));
                if (v.Failed && density == "sparse" && v.Rule == CalibrationRule.Student)
                {
                    var cellName = $"{v.Name}[{v.Index}]";
                    if (!sparseStudentFailures.TryGetValue(cellName, out var runs))
                    {
                        runs = [];
                        sparseStudentFailures[cellName] = runs;
                    }

                    runs.Add(k);
                }
            }
        }

        foreach (var (key, (n, failures)) in table.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            _output.WriteLine($"{formulation} alpha={alpha}: {key,-28} N={n,6} K={failures,4}");
        }

        foreach (var (cellName, runs) in sparseStudentFailures)
        {
            _output.WriteLine($"{formulation} sparse Student failure {cellName}: runs {string.Join(",", runs)}");
        }

        // A report cannot go red, so it is at least read where it is consumed: the dense population it speaks of exists.
        Assert.Contains(table, entry => entry.Key.StartsWith("dense", StringComparison.Ordinal));
    }

    // One row of exact integer counts as the original prints it.
    private sealed class KnownRow
    {
        private KnownRow(double[] printed, long smallestCount)
        {
            Printed = printed;
            SmallestCount = smallestCount;
        }

        public double[] Printed { get; }

        public long SmallestCount { get; }

        // The print resolution of the row's own smallest cell, shared out over its own count: the exact
        // tolerance of `q = min / k` at the true `k`, read off the truth the criterion never has.
        public double TrueResolutionOfStep =>
            PrintResolution.ResolutionFromDecimalDigits(Printed.Where(v => v > 0.0).Min(), PrintedDigits) / SmallestCount;

        public static KnownRow FromCounts(IReadOnlyList<long> counts)
        {
            var printed = counts
                .Select(count => double.Parse((count * TrueStep).ToString("E2", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture))
                .ToArray();
            return new KnownRow(printed, counts.Min());
        }
    }
}
