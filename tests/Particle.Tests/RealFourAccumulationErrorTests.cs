using System.Text;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Computes the second leg the root BOOT.md's exclusion rule demands for a quantity
/// degraded by the original's REAL*4 accumulation — "the term count and bound of the
/// original's REAL*4 accumulation against the band width" — for the eight
/// accumulator families <see cref="AccumulatorSweep"/> tracks: <c>Allvdok</c>,
/// <c>Vdokstr</c>, <c>VdokTotal</c>, <c>VdokTotal2</c>, <c>Dokp41</c>, <c>Dokp31</c>
/// and the Fmkarm family (<c>FmkarmCor</c>, <c>Fmkarm2</c>).
///
/// <see cref="AccumulatorSweep"/> does the first leg — the term count — at the
/// original's own granularity, one rounding per drawn particle, not per attempt
/// (root BOOT.md, 2026-09-21: the earlier per-attempt granularity "was closed on a
/// lower bound read as a measurement", withdrawn the same day). It observes the
/// actual terms over a real, large sample of each reference formulation (not an
/// estimate), sums them twice, once per true term in double and in a binary32
/// shadow, so this class only has to compare the two sums and put the difference
/// against the criterion's own scale.
///
/// The sample this class runs (100,000 attempts of a real formulation: 20,000 of
/// cycle 0, 80,000 continuing into cycle 1) is far short of the original's own
/// per-formulation NFX/NFY (a few hundred thousand to several hundred million,
/// root BOOT.md's own diagnosis) — reproducing that scale needs the cycle loop only
/// <c>Simulation</c> owns (root BOOT.md, Decomposition; this node depends on
/// <c>Particle</c>, <c>Random</c>, <c>Statistics</c>, <c>Input</c>, <c>Harness</c>,
/// <c>Fixtures</c> only), so this class measures at the sample's own scale exactly —
/// now already at the true per-draw term count, not an attempt count needing a
/// further 60-70x correction before it means what it is used as — then extrapolates
/// to the original's real scale by the ratio of NFY (the exact, REAL*4-unaffected
/// integer the reference's own results.m prints) between the sample and the
/// reference, in both directions the extrapolation can go: linearly (the worst-case
/// analytic bound, Higham 2002 sec. 4.2, scales with the term count) and by square
/// root (the scale a sum of quasi-randomly-signed rounding errors typically grows
/// by). Both are extrapolations, not measurements, and are reported as such, beside
/// the sample's own exact figures.
/// </summary>
public class RealFourAccumulationErrorTests(ITestOutputHelper output)
{
    // 2^-24: the unit roundoff of round-to-nearest binary32 (24-bit mantissa
    // including the implicit bit), the same convention Higham's forward-error bound
    // for sequential summation uses.
    private const double Float32UnitRoundoff = 1.0 / 16777216.0;

    private const int Cycle0Attempts = 20_000;
    private const int Cycle1Attempts = 80_000;

    public static IEnumerable<object[]> Formulations()
    {
        foreach (var name in ReferenceFormulation.Names)
        {
            yield return new object[] { name };
        }
    }

    /// <summary>
    /// Higham's forward-error bound for summing <paramref name="n"/> floating-point
    /// terms sequentially in round-to-nearest arithmetic of unit roundoff
    /// <paramref name="u"/>: |computed - exact| &#8804; gamma(n-1) * &#931;|term|,
    /// gamma(k) = k*u / (1 - k*u) (Higham, "Accuracy and Stability of Numerical
    /// Algorithms", 2nd ed., sec. 4.2). <paramref name="magnitudeSum"/> is
    /// &#931;|term|, exact here because every term this node sums is non-negative
    /// (<see cref="AccumulatorSweep"/>'s own remarks).
    /// </summary>
    private static double HighamBound(long n, double magnitudeSum, double u = Float32UnitRoundoff)
    {
        if (n <= 1 || magnitudeSum == 0.0)
        {
            return 0.0;
        }

        var k = n - 1;
        var ku = k * u;
        if (ku >= 1.0)
        {
            return double.PositiveInfinity; // outside the bound's own domain of validity
        }

        var gamma = ku / (1.0 - ku);
        return gamma * magnitudeSum;
    }

    /// <summary>
    /// The printed quantity each reachable field feeds, and the axis its own cell
    /// index shares with that quantity's array (both 0-based, same cell). Confidence
    /// and evidence for each mapping are in <c>src/Particle/BOOT.md</c>, "## Defects
    /// of the original" (the row this test's own figures update). <c>Dokp41</c> and
    /// <c>Dokp31</c> both map to <c>dokkarm43</c>, not one each to a separate
    /// quantity: `Statistics/API.md`'s own `CycleStatistics.Compute` doc comment
    /// names them as a pair ("rewritten in place: Dokp41, Dokp31, ..."), and
    /// `Categories.MergeAndDescribe`'s own out parameter is `dokp43` (matching the
    /// printed `dokkarm43`), a single 4th/3rd-moment ratio the two together produce —
    /// there is no signature evidence this node can see for a `dokp10`/`dokkarm10`
    /// counterpart fed by either field alone (`qdokkarm`, the other out parameter,
    /// is a plausible source for the printed `dokkarm10` instead, but nothing in
    /// `Statistics/API.md` ties it to `Dokp41`/`Dokp31`, so this test does not claim
    /// it). Comparing each field's own error against the one ratio it jointly feeds,
    /// holding the other side fixed, is a one-sided sensitivity, not the ratio's own
    /// full propagated error (the two sides can partly cancel, since both accumulate
    /// correlated REAL*4 rounding within the same neighbour-loop pass).
    /// <c>Allvdok</c>, <c>Vdokstr</c>, <c>VdokTotal</c> and <c>VdokTotal2</c> are not
    /// printed quantities themselves and are not in this table: reaching a band width
    /// for them needs the derivation `Statistics`/`Output` do, which this node may
    /// not read (AGENTS.md §3) — reported in the class remarks and in BOOT.md as
    /// widths this test could not reach.
    /// </summary>
    private static readonly Dictionary<string, string> PrintedQuantity = new()
    {
        ["Dokp41"] = "dokkarm43",
        ["Dokp31"] = "dokkarm43",
        ["FmkarmCor"] = "fmkarm_cor",
        ["Fmkarm2"] = "fmkarm_cor2",
    };

    /// <summary>
    /// The lagged replicas' own mean and sample standard deviation of
    /// <paramref name="quantity"/>[<paramref name="cellIndex"/>] for
    /// <paramref name="formulation"/>: a model-free, directly-computed proxy for the
    /// criterion's own band width (root BOOT.md, "Statistical reference criterion":
    /// the Student-t band is <c>t * sd_R * sqrt(1 + 1/R)</c> of this same sd_R), read
    /// straight from the fixture files this node may read as data
    /// (`tests/Fixtures/replicas-lagged`) rather than by calling any of
    /// <c>Harness</c>'s internal calibration (AGENTS.md §3): the family-wise
    /// per-cell alpha, the calibration rule assignment and every refinement
    /// `tests/Harness/BOOT.md`'s own decisions record are `Harness`'s, not
    /// reproduced here.
    /// </summary>
    /// <summary>
    /// <paramref name="mean"/>/<paramref name="stdDev"/> of the lagged replicas' own
    /// <paramref name="quantity"/>[<paramref name="cellIndex"/>], and
    /// <paramref name="resolution"/>, the reference file's own print resolution for
    /// that cell (<c>ResultsMFile.ParseCells</c>, `Harness/API.md`) — needed because a
    /// replica-to-replica standard deviation computed on values that all happen to
    /// round to the same printed digits reads as zero without it, which is a
    /// property of the print format, not evidence that the true spread is zero
    /// (root BOOT.md's own criterion keeps the same floor for exactly this reason:
    /// "floors from the print resolution").
    /// </summary>
    private static (double Mean, double StdDev, double Resolution, int R) ReplicaSpread(string formulation, string quantity, int cellIndex)
    {
        var directory = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation);
        var values = new List<double>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.m.txt"))
        {
            var replica = ResultsMFile.Parse(file);
            if (replica.TryGetValue(quantity, out var array) && cellIndex < array.Length)
            {
                values.Add(array[cellIndex]);
            }
        }

        var mean = values.Average();
        var variance = values.Count > 1
            ? values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1)
            : 0.0;

        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var referenceCells = ResultsMFile.ParseCells(referencePath);
        var resolution = referenceCells.TryGetValue(quantity, out var cells) && cellIndex < cells.Length
            ? cells[cellIndex].Resolution
            : 0.0;

        return (mean, Math.Sqrt(variance), resolution, values.Count);
    }

    private readonly record struct SaturationCheckpoint(long N, double DoubleTotal, double FloatTotal, double RelativeDifference, double IneffectiveFraction);

    /// <summary>
    /// Extends <paramref name="observedDeltas"/> (a cell's own real, observed
    /// non-negative deltas) to <paramref name="targetTerms"/> terms by resampling
    /// with replacement (a bootstrap: the real empirical distribution this sweep
    /// measured, not an assumed one), summing the resampled sequence in double and in
    /// a binary32 shadow exactly as <see cref="AccumulatorSweep"/> does, and
    /// reporting checkpoints at geometric steps. <c>IneffectiveFraction</c> at a
    /// checkpoint is the share of additions since the previous checkpoint that left
    /// the float32 total bit-for-bit unchanged (BOOT.md, "REAL*4 sums ... saturate on
    /// long runs") — a run of these approaching 100% is the saturation this class's
    /// other, count-and-magnitude-only figures cannot see coming.
    /// </summary>
    private static List<SaturationCheckpoint> BootstrapExtend(List<double> observedDeltas, long targetTerms, int seed)
    {
        var rng = new SplitMix64(seed);
        var runningDouble = 0.0;
        var runningFloat = 0.0f;
        var checkpoints = new List<SaturationCheckpoint>();

        long n = 0;
        long nextCheckpoint = 1;
        long ineffectiveSinceLastCheckpoint = 0;
        long countSinceLastCheckpoint = 0;

        while (n < targetTerms)
        {
            var term = observedDeltas[rng.Next(observedDeltas.Count)];
            var before = runningFloat;
            runningFloat += (float)term;
            runningDouble += term;
            n++;
            countSinceLastCheckpoint++;
            if (runningFloat == before)
            {
                ineffectiveSinceLastCheckpoint++;
            }

            if (n == nextCheckpoint || n == targetTerms)
            {
                var relative = runningDouble != 0.0 ? Math.Abs(runningFloat - runningDouble) / runningDouble : 0.0;
                var ineffectiveFraction = countSinceLastCheckpoint > 0
                    ? (double)ineffectiveSinceLastCheckpoint / countSinceLastCheckpoint
                    : 0.0;
                checkpoints.Add(new SaturationCheckpoint(n, runningDouble, runningFloat, relative, ineffectiveFraction));
                ineffectiveSinceLastCheckpoint = 0;
                countSinceLastCheckpoint = 0;
                nextCheckpoint = Math.Min(nextCheckpoint * 10, targetTerms);
                if (nextCheckpoint == n && n != targetTerms)
                {
                    nextCheckpoint = n + 1;
                }
            }
        }

        return checkpoints;
    }

    [Theory]
    [MemberData(nameof(Formulations))]
    [Trait("Category", "Long")]
    public void RealFourAccumulationErrorMeasuredAgainstTheAnalyticBound(string formulation)
    {
        using var host = new CpuHost();
        var sweep = AccumulatorSweep.Run(host.Accelerator, formulation, Cycle0Attempts, Cycle1Attempts);

        Assert.True(sweep.AcceptedParticles > 0, "the sample accepted no particle at all: not a representative run");
        Assert.True(sweep.Nfy > 0, "the sample drew no neighbour at all: not a representative run");

        // The replay this sweep feeds Allvdok/Vdokstr from (AccumulatorSweep, class remarks,
        // "replayed") is trusted only once verified: every attempt's replayed per-cell sum must
        // equal the attempt's own recorded double delta bit for bit. A mismatch here would mean
        // the replay drew a different sequence than Attempt.Run itself did (e.g. a mismatched
        // stream, a wrong cell index, a wrong filter) — everything this test reports for those
        // two fields would then rest on the wrong term sequence and would not count (the task
        // this class exists for asked explicitly for this check to be reported, not assumed).
        foreach (var check in sweep.ReplayChecks)
        {
            Assert.True(
                check.Mismatches == 0,
                $"{formulation}.{check.Field}: the replay disagreed with Attempt.Run's own recorded delta " +
                $"in {check.Mismatches:N0} of {check.AttemptsChecked:N0} attempts — first mismatch: " +
                $"{check.FirstMismatch}. The replay is not the original's own term sequence; nothing built " +
                "on it counts.");
        }

        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var reference = ResultsMFile.Parse(referencePath);
        var fullNfy = reference["NFY"][0];
        var fullNfx = reference["NFX"][0];
        var linearScale = fullNfy / sweep.Nfy;
        var sqrtScale = Math.Sqrt(linearScale);

        var report = new StringBuilder();
        _ = report.AppendLine($"=== {formulation} ===");
        _ = report.AppendLine($"sample: cycle0={sweep.Cycle0Attempts:N0} cycle1={sweep.Cycle1Attempts:N0} attempts, " +
                           $"accepted={sweep.AcceptedParticles:N0}, Nfx={sweep.Nfx:N0}, Nfy={sweep.Nfy:N0}, " +
                           $"Nfq={sweep.Nfq:N0}, Nfw={sweep.Nfw:N0}");
        _ = report.AppendLine($"reference (full run): NFX={fullNfx:N0}, NFY={fullNfy:N0}; " +
                           $"linear extrapolation factor={linearScale:F1}, sqrt factor={sqrtScale:F2}");
        foreach (var check in sweep.ReplayChecks)
        {
            _ = report.AppendLine($"replay check: {check.Field} verified over {check.AttemptsChecked:N0} attempts, " +
                               $"{check.Mismatches:N0} mismatches (bit-for-bit against Attempt.Run's own recorded delta)");
        }

        foreach (var field in sweep.Fields)
        {
            var grandDouble = field.DoubleTotal.Sum();
            var grandFloat = (double)field.Float32Total.Aggregate(0.0f, (a, b) => a + b);
            var grandTerms = field.TrueTermCount.Sum();
            var grandLumpedAttempts = field.LumpedAttempts.Sum();
            var grandLumpedTerms = field.LumpedTermsInLumps.Sum();
            var grandDiff = Math.Abs(grandFloat - grandDouble);
            var grandBoundSample = HighamBound(grandTerms, grandDouble);

            // Worst cell: the one with the largest absolute float32-vs-double difference.
            var worstCell = -1;
            var worstDiff = -1.0;
            for (var i = 0; i < field.Length; i++)
            {
                var diff = Math.Abs(field.Float32Total[i] - field.DoubleTotal[i]);
                if (diff > worstDiff)
                {
                    worstDiff = diff;
                    worstCell = i;
                }
            }

            var nonzeroCells = field.TrueTermCount.Count(c => c > 0);
            var termCounts = field.TrueTermCount.Where(c => c > 0).ToArray();

            _ = report.AppendLine($"-- {field.Name} (length {field.Length}, {nonzeroCells} nonzero cells) --");
            _ = report.AppendLine($"   true term count (one per original accumulation-line firing, not per attempt): " +
                               $"total={grandTerms:N0}" +
                               (termCounts.Length > 0
                                   ? $", per-cell min={termCounts.Min():N0} mean={termCounts.Average():F1} max={termCounts.Max():N0}"
                                   : " (no cell ever touched in this sample)"));
            if (grandLumpedAttempts > 0)
            {
                _ = report.AppendLine($"   lumped (a cell this field's own atomic companion counter shows was hit by " +
                                   $"2+ true terms inside one attempt, so this class fed the attempt's own combined " +
                                   $"delta as one addition instead of each term's own): {grandLumpedAttempts:N0} " +
                                   $"attempt-cells, covering {grandLumpedTerms:N0} of the {grandTerms:N0} true terms " +
                                   $"above ({(double)grandLumpedTerms / grandTerms:P2}) — the float32 shadow performed " +
                                   $"{grandTerms - grandLumpedTerms + grandLumpedAttempts:N0} additions for this " +
                                   "field, not the true term count, exactly where this shortfall says so.");
            }
            else
            {
                _ = report.AppendLine("   lumped: none — every true term above was fed the float32 shadow individually.");
            }

            _ = report.AppendLine($"   grand total (double, = sum|term| exactly, all terms non-negative): {grandDouble:G10}");
            _ = report.AppendLine($"   measured float32-vs-double difference, whole field: {grandDiff:G6} " +
                               $"({(grandDouble != 0 ? grandDiff / grandDouble : 0):E3} relative)");
            _ = report.AppendLine($"   Higham worst-case bound at the SAMPLE's own true scale " +
                               $"(n={grandTerms:N0}, self-consistency check): {grandBoundSample:G6}");

            if (worstCell >= 0 && field.TrueTermCount[worstCell] > 0)
            {
                var cellDouble = field.DoubleTotal[worstCell];
                var cellTerms = field.TrueTermCount[worstCell];
                var cellBoundSample = HighamBound(cellTerms, cellDouble);
                var cellBoundFull = HighamBound((long)Math.Round(cellTerms * linearScale), cellDouble * linearScale);
                var cellDiffFullLinear = worstDiff * linearScale;
                var cellDiffFullSqrt = worstDiff * sqrtScale;

                _ = report.AppendLine($"   worst cell: index {worstCell}, terms={cellTerms:N0}, double total={cellDouble:G10}, " +
                                   $"measured diff={worstDiff:G6} ({(cellDouble != 0 ? worstDiff / cellDouble : 0):E3} relative)");
                _ = report.AppendLine($"   worst cell, extrapolated to the reference's own NFY scale: " +
                                   $"typical (sqrt) diff~{cellDiffFullSqrt:G6}, worst-case (linear) diff~{cellDiffFullLinear:G6}, " +
                                   $"Higham bound at full scale={cellBoundFull:G6}");

                // "REAL*4 sums ... saturate on long runs" (root BOOT.md, "Decisions where the port
                // departs from a transcription") is a different, more severe effect than accumulated
                // rounding noise: once a float32 running total is roughly 2^24 times a still-arriving
                // term's own magnitude, that addition rounds away to nothing and the sum stops growing
                // at all. Neither the sqrt-scaled "typical" figure above nor the Higham bound (both
                // built from the sample's own term COUNT and magnitude SUM alone) can see this coming;
                // it needs the actual sequence to keep growing. This class's own sample tops out at a
                // few hundred thousand terms — nowhere near the reference's own tens to hundreds of
                // millions — so this bootstrap-extends the worst cell's own OBSERVED term distribution
                // (sampled with replacement from the real deltas this sweep saw, not a synthetic
                // distribution) out to that cell's own extrapolated full-scale term count, and reports
                // where, if anywhere, the float32 running sum stops moving.
                var cellTrace = field.Deltas[worstCell];
                if (cellTrace.Count >= 100)
                {
                    var targetTerms = Math.Min((long)Math.Round(cellTerms * linearScale), 300_000_000L);
                    var saturation = BootstrapExtend(cellTrace, targetTerms, seed: worstCell + field.Name.GetHashCode());
                    _ = report.AppendLine($"   worst cell, bootstrap-extended to {targetTerms:N0} terms " +
                                      "(resampled with replacement from this sweep's own observed deltas for this cell):");
                    foreach (var checkpoint in saturation)
                    {
                        _ = report.AppendLine($"     n={checkpoint.N:N0}: double={checkpoint.DoubleTotal:G8}, " +
                                          $"float32={checkpoint.FloatTotal:G8}, relative diff={checkpoint.RelativeDifference:E3}, " +
                                          $"ineffective additions in the last block={checkpoint.IneffectiveFraction:P1}");
                    }

                    // A second, independent audit (outside this tree, a different generator, not
                    // this class's own bootstrap) reports that a per-draw binary32 emulation of
                    // Allvdok/vdokstr retains 0.5405 of HPEPA3's own cell 1 and 0.3469 of HMX's own
                    // equivalent cell against the true double sum at the shipped N. This class's own
                    // bootstrap-extended retained fraction at this worst cell, measured inside this
                    // tree from this tree's own replay (not imported from the audit), is reported
                    // here for the direct, apples-to-apples comparison the owning task asked for —
                    // it is evidence about this one number only, not an adoption of the audit's wider
                    // claim about fmdok/pdoksmall/Dok43all, which needs Statistics/Output and stays
                    // unreproduced in this tree (BOOT.md's own dated note).
                    if (field.Name == "Allvdok" && (formulation == "HPEPA3" || formulation == "HMX"))
                    {
                        var last = saturation[^1];
                        var retainedFraction = last.DoubleTotal != 0.0 ? last.FloatTotal / last.DoubleTotal : double.NaN;
                        var audited = formulation == "HPEPA3" ? 0.5405 : 0.3469;
                        _ = report.AppendLine($"   retained fraction at n={last.N:N0} (float32/double): " +
                                          $"{retainedFraction:F4}; the outside audit's own reported figure for this " +
                                          $"formulation's cell 1 is {audited:F4} (worst cell here is index {worstCell}) " +
                                          $"-> {(Math.Abs(retainedFraction - audited) <= 0.02 ? "matches, within 0.02" : "does not match")}.");
                    }

                    if (PrintedQuantity.TryGetValue(field.Name, out var quantity))
                    {
                        var (mean, stdDev, resolution, r) = ReplicaSpread(formulation, quantity, worstCell);
                        // The replica-to-replica spread can never be resolved finer than the print
                        // format's own resolution: a raw sd_R of (near) zero at this cell is a property
                        // of the print format, not evidence of zero true spread (root BOOT.md's own
                        // criterion keeps the same floor, "floors from the print resolution").
                        var effectiveSpread = Math.Max(stdDev, resolution);
                        var cv = mean != 0.0 ? effectiveSpread / Math.Abs(mean) : double.NaN;
                        var extrapolatedRelative = saturation[^1].RelativeDifference;
                        var ratio = double.IsNaN(cv) || cv == 0.0 ? double.NaN : extrapolatedRelative / cv;
                        var verdict = double.IsNaN(ratio)
                            ? "inconclusive (the printed quantity's own reference mean and print resolution are both zero at this cell)"
                            : ratio >= 0.1
                                ? "large enough to matter: within an order of magnitude of the replica-to-replica spread"
                                : ratio <= 0.01
                                    ? "rules the REAL*4 story out here: two orders of magnitude below the replica-to-replica spread"
                                    : "inconclusive: one to two orders of magnitude below the replica-to-replica spread";
                        _ = report.AppendLine($"   feeds printed `{quantity}[{worstCell}]` (R={r} lagged replicas): " +
                                          $"reference mean={mean:G6}, replica sd={stdDev:G4}, print resolution={resolution:G4}, " +
                                          $"effective spread={effectiveSpread:G4}, CV={cv:E3}; " +
                                          $"bootstrap-extrapolated REAL*4 relative error / CV = {ratio:E3} -> {verdict}");
                    }
                    else
                    {
                        _ = report.AppendLine($"   {field.Name} is not itself a printed quantity; reaching its own band width " +
                                          "needs Statistics's/Output's derivation, which this node may not read (AGENTS.md §3) " +
                                          "-> could not reach a band width for this field, reported in its own units only above.");
                    }
                }

                // Self-consistency: the sample's own measured difference can never exceed the sample's own
                // Higham bound. This is the check that can fail if this class's own bookkeeping is wrong
                // (wrong sign, double-counted delta, a term missing from one of the two running sums) and
                // passes only because the double and float32 running sums are driven by the exact same
                // delta sequence (AccumulatorSweep, class remarks).
                Assert.True(
                    worstDiff <= cellBoundSample * 1.05 + 1e-12,
                    $"{formulation}.{field.Name}[{worstCell}]: measured {worstDiff:G6} exceeds its own sample-scale " +
                    $"Higham bound {cellBoundSample:G6} (n={cellTerms}) — the measurement's own bookkeeping is suspect.");
            }

            Assert.True(
                grandDiff <= grandBoundSample * 1.05 + 1e-12,
                $"{formulation}.{field.Name}: measured {grandDiff:G6} exceeds its own sample-scale Higham bound " +
                $"{grandBoundSample:G6} (n={grandTerms}) — the measurement's own bookkeeping is suspect.");
        }

        var text = report.ToString();
        output.WriteLine(text);

        var scratchDir = Environment.GetEnvironmentVariable("PROPSTRUCT_ACCUMULATOR_REPORT_DIR");
        if (!string.IsNullOrEmpty(scratchDir))
        {
            _ = Directory.CreateDirectory(scratchDir);
            File.WriteAllText(Path.Combine(scratchDir, $"{formulation}.accumulator-report.txt"), text, Encoding.UTF8);
        }
    }
}
