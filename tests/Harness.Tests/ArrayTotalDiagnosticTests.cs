using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Decision X ("the array total is compared, not only conditioned on"), item 1 only: the measurement that
/// chooses which interval item 2 uses for the new "array total" compared quantity. Every count-like cell's
/// interval today is built at the candidate's own reconstructed total, and the total itself is never compared to
/// anything (a working note, not in the tree). Before adding that
/// comparison, this measures, per estimation unit (a distribution-function array, or one
/// <c>fqdokkarm(&lt;row&gt;,:)</c> row — the same units <see cref="DispersionEstimator.Estimate"/>
/// already pools by), per reference formulation, per layout:
///
/// <list type="bullet">
/// <item>the replicas' own reconstructed totals, their mean and sample variance, and the dispersion index
/// <c>phi_T = s^2 / T_bar</c>;</item>
/// <item>how many replicas produced a total at all, and how many units are therefore not comparable (decision
/// X's own item-2 floor of at least four replicas with a total, reused here only to report which units it would
/// exclude, not to apply any rule);</item>
/// <item>an honesty check on the reconstruction itself: whether any pair of a unit's own replica totals sits
/// close to a small-integer ratio of each other, the signature of one replica's quantum having been inferred a
/// factor away from the others'.</item>
/// </list>
///
/// <see cref="CountReconstruction.ArrayTotals"/> (already <c>internal</c>, shared with
/// <see cref="DispersionEstimator.Estimate"/> and <c>DispersionDiagnosticTests</c>) supplies the
/// per-replica totals; this file adds only the plain mean/variance/dispersion-index arithmetic and the pairwise
/// integer-ratio check decision X's item 1 itself asks for — neither duplicates a formula this node already has
/// (root BOOT.md Taboos: "no second implementation of ... a formula"). This is item 1 only: no interval, no
/// change to <c>StatisticalCriterion.cs</c>, no `m`/`alpha` change. Item 2 (the rule) and item 3 (the mutation
/// proof) are a later task, once this measurement chooses the branch.
///
/// Coordinator follow-up on the same task (the merged commit's own caveat about an invented tolerance): the
/// integer-ratio signature needs no tolerance and no simulated null, because the mechanism is exact arithmetic.
/// <c>TryInferRunQuantum</c> fits its candidate quantum against the printed values of only the array's own
/// non-zero *resolvable* cells (count <c>m</c>); if a run prints <c>v_j = k_j / N</c> for integer counts
/// <c>k_j</c> and total <c>N</c>, the quantum this diagnostic's own <c>m</c> sampled cells determine is
/// <c>gcd_j(k_j) / N</c>, so the reconstructed total is <c>N / g</c> for <c>g = gcd_j(k_j)</c> — invisible in the
/// reconstructed counts themselves (their own gcd is 1 by construction once divided by <c>g</c>), so it can only
/// be read off as a probability of occurring at all, closed-form in <c>m</c>: for <c>m</c> otherwise-unrelated
/// integers, the chance they share a prime divisor is <c>sum over primes p of p^-m</c>. This adds, per unit: the
/// per-replica <c>m</c> (the number of non-zero cells the run's own quantum fit actually resolved), the unit's
/// own minimum and median <c>m</c>; <c>phi_T</c> grouped by <c>m</c>; and the observed integer-ratio rate per
/// <c>m</c>-group against this closed form — never a change to the rule itself, still item 1 only.
/// </summary>
public class ArrayTotalDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public ArrayTotalDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // The count-like distribution-function families this node's own `DistributionFunctionFamilies`
    // (StatisticalCriterion.cs) names, plus `fqdokkarm`'s own per-row family, discovered by name pattern below —
    // the same list `DispersionDiagnosticTests` already uses for the per-cell diagnostic, reused here for the
    // per-unit total.
    private static readonly string[] FixedFamilies = ["fqkarm", "fqkarm_cor", "fqmkm1", "fqmkm2", "coef"];

    // Decision X, item 2's own comparability floor ("a unit whose candidate or whose replicas (fewer than four
    // with a total) cannot supply the numbers is not compared"). Reused here only to report, per unit, whether
    // the rule item 2 will eventually apply would find it comparable — this task does not implement that rule.
    private const int MinimumContributingReplicas = 4;

    // Decision X, item 1's own honesty check: "the ratio of two replicas' totals is close to a small integer".
    // No production tolerance exists for a pair of already-reconstructed grand totals —
    // `TryInferRunQuantum`'s own per-cell tolerance (`IsNearIntegerMultipleWithinResolution`, private) is built
    // from a printed cell's own resolution, which a grand total does not carry. This is therefore a new, explicit
    // tolerance for this diagnostic alone, not a second implementation of that private formula: 5% relative, the
    // same figure this node's own BOOT.md, "Count-like cells", already documents as the tolerance chosen "to
    // absorb the print rounding" of an integer-multiple check. Ratios are only tested against small integers (2
    // through 8): a ratio near 1 is ordinary Monte Carlo spread between two replicas' totals, not a quantum
    // inferred a factor out, and a ratio that has to be read as a large integer to "fit" is no longer a
    // meaningful near-miss at 5%.
    //
    // The coordinator's follow-up (class comment above) explains *why* this signature occurs without needing a
    // tolerance at all -- a shared-divisor artefact governed by `m` -- but does not replace this pairwise check:
    // it is still the only observable proxy available (the "true" per-run quantum is never known independently),
    // so it stays exactly as merged, and its rate is now read against the closed-form expectation below rather
    // than trusted on its own.
    private const double IntegerRatioRelativeTolerance = 0.05;
    private const int MaxSmallIntegerRatio = 8;

    // Coordinator follow-up: the closed-form probability that `m` otherwise-unrelated positive integers share a
    // common prime divisor is `sum over primes p of p^-m` (the leading term of `1 - prod_p (1 - p^-m)`, accurate
    // to the precision this diagnostic needs since successive primes' terms fall off geometrically). Primes below
    // 1000 are ample: the largest omitted term, `997^-2`, is on the order of `1e-6`, far below anything this
    // report rounds to. This is new arithmetic for this diagnostic alone -- nothing in `StatisticalCriterion.cs`
    // computes it, so it is not a second implementation of any formula there.
    private static readonly int[] SmallPrimes = ComputePrimesBelow(1000);

    private enum Layout
    {
        Original,
        Independent,
    }

    private readonly record struct UnitResult(
        string Formulation,
        Layout Layout,
        string Unit,
        int Produced,
        int ReplicaCount,
        double TBar,
        double Variance,
        double PhiT,
        bool Comparable,
        bool IntegerRatioSignature,
        int? MinM,
        double? MedianM);

    [Trait("Category", "Long")]
    [Fact]
    public void ArrayTotalDispersionPerUnitPerFormulationPerLayoutChoosesTheItem2Branch()
    {
        var results = new List<UnitResult>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in new[] { Layout.Original, Layout.Independent })
            {
                var directory = layout == Layout.Original ? "replicas-lagged" : "replicas-independent";
                var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
                for (var k = 1; k <= replicaCount; k++)
                {
                    var path = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, k + ".m.txt");
                    replicaCells.Add(ResultsMFile.ParseCells(path));
                }

                // Family names actually printed by this formulation/layout: the fixed list, plus every
                // "fqdokkarm(<row>,:)" key any of its replicas happens to hold (this node's own canonical
                // category axis: a row a run never produced is not zeroed, it is simply not a key there).
                var dokRowNames = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var cells in replicaCells)
                {
                    foreach (var key in cells.Keys)
                    {
                        if (key.StartsWith("fqdokkarm(", StringComparison.Ordinal))
                        {
                            _ = dokRowNames.Add(key);
                        }
                    }
                }

                foreach (var unit in FixedFamilies.Concat(dokRowNames))
                {
                    results.Add(MeasureOneUnit(formulation, replicaCount, layout, unit, replicaCells));
                }
            }
        }

        foreach (var r in results)
        {
            _output.WriteLine(
                $"{r.Formulation,-8} {r.Layout,-11} {r.Unit,-20} produced={r.Produced,3}/{r.ReplicaCount,-3} " +
                $"T_bar={r.TBar,12:G6} s2={r.Variance,14:G6} phi_T={r.PhiT,10:G6} " +
                $"comparable={r.Comparable,-5} intRatio={r.IntegerRatioSignature,-5} " +
                $"minM={(r.MinM is { } mm ? mm.ToString() : "-"),3} medianM={(r.MedianM is { } md ? md.ToString("G4") : "-"),5}");
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- summary by family (comparable units only, phi_T finite) ---");

        var byFamily = results
            .Where(r => r.Comparable && double.IsFinite(r.PhiT))
            .GroupBy(r => Family(r.Unit))
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        var maxPhiTByFamily = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var group in byFamily)
        {
            var phis = group.Select(r => r.PhiT).OrderBy(x => x).ToList();
            var median = Median(phis);
            var max = phis.Max();
            var min = phis.Min();
            maxPhiTByFamily[group.Key] = max;
            _output.WriteLine(
                $"family={group.Key,-12} units={phis.Count,4} min(phi_T)={min:G6} median(phi_T)={median:G6} max(phi_T)={max:G6}");
        }

        var constantFamilies = results
            .Where(r => r.Comparable)
            .GroupBy(r => Family(r.Unit))
            .Where(g => g.All(r => r.Variance == 0.0))
            .Select(g => g.Key)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            constantFamilies.Count == 0
                ? "no family has zero variance on every one of its comparable units."
                : "families with zero variance on every comparable unit (need no interval at all): "
                    + string.Join(", ", constantFamilies));

        var notComparable = results.Count(r => !r.Comparable);
        var signatureCount = results.Count(r => r.IntegerRatioSignature);
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"units total={results.Count}, not comparable (< {MinimumContributingReplicas} replicas with a total)={notComparable}, " +
            $"integer-ratio signature={signatureCount} ({(double)signatureCount / results.Count:P2})");

        var threshold = 1.5;
        var overThreshold = maxPhiTByFamily.Where(kv => kv.Value > threshold).ToList();
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            overThreshold.Count == 0
                ? $"every family's phi_T (max over its comparable units) is at most {threshold} -> item 2's negative-binomial branch."
                : $"families exceeding phi_T = {threshold} (max over comparable units): "
                    + string.Join(", ", overThreshold.Select(kv => $"{kv.Key}={kv.Value:G4}"))
                    + " -> item 2's Student-band branch.");

        // --- coordinator follow-up: is the overdispersion, and the integer-ratio signature, explained by m? ---

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- phi_T by m-group (median-m bucket; coordinator's small-m-tail prediction) ---");

        var byM = results
            .Where(r => r.Comparable && double.IsFinite(r.PhiT) && r.MedianM is not null)
            .GroupBy(r => MBucket(r.MedianM!.Value))
            .OrderBy(g => MBucketOrder(g.Key));

        foreach (var group in byM)
        {
            var phis = group.Select(r => r.PhiT).OrderBy(x => x).ToList();
            _output.WriteLine(
                $"m={group.Key,-4} units={phis.Count,4} median(phi_T)={Median(phis):G6} max(phi_T)={phis.Max():G6}");
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- top 10 units by phi_T, with their own m (tests the small-m-tail prediction directly) ---");
        foreach (var r in results.Where(r => r.Comparable && double.IsFinite(r.PhiT)).OrderByDescending(r => r.PhiT).Take(10))
        {
            _output.WriteLine(
                $"{r.Formulation,-8} {r.Layout,-11} {r.Unit,-20} phi_T={r.PhiT:G6} " +
                $"minM={(r.MinM is { } mm ? mm.ToString() : "-"),3} medianM={(r.MedianM is { } md ? md.ToString("G4") : "-")}");
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- coef's own m, confirming its family minimum phi_T=2.44 reading (large m expected) ---");
        foreach (var r in results.Where(r => r.Unit == "coef" && r.Comparable))
        {
            _output.WriteLine(
                $"{r.Formulation,-8} {r.Layout,-11} phi_T={r.PhiT:G6} " +
                $"minM={(r.MinM is { } mm ? mm.ToString() : "-"),3} medianM={(r.MedianM is { } md ? md.ToString("G4") : "-")}");
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            "--- integer-ratio signature: observed rate vs. closed-form P(m) = sum_p p^-m, by m-group ---");

        // Only units where the pairwise check could actually fire at all (>= 2 replicas produced a total): a
        // unit with fewer never had `HasIntegerRatioSignature` called and would silently deflate the observed
        // rate if counted here as a false-free unit.
        var signatureEligible = results.Where(r => r.Produced >= 2 && r.MedianM is not null).ToList();
        var byMSignature = signatureEligible
            .GroupBy(r => MBucket(r.MedianM!.Value))
            .OrderBy(g => MBucketOrder(g.Key));

        foreach (var group in byMSignature)
        {
            var units = group.ToList();
            var observedRate = units.Count(r => r.IntegerRatioSignature) / (double)units.Count;
            var meanExpected = units.Average(r => ClosedFormSharedDivisorProbability((int)Math.Round(r.MedianM!.Value)));
            var ratio = meanExpected > 0.0 ? observedRate / meanExpected : double.NaN;
            _output.WriteLine(
                $"m={group.Key,-4} units={units.Count,4} observed={observedRate:P2} closed-form(mean)={meanExpected:P2} " +
                $"observed/expected={ratio:G4}");
        }

        Assert.True(results.Count > 0, "expected at least one formulation/layout/unit combination.");
    }

    // Coordinator's second follow-up, corrected once (its own message on the first attempt): the first version
    // of this test rewrote a real array's printed values onto a coarser grid by independent per-cell rounding.
    // That is lossy and changes the shape -- it does not construct the mechanism the derivation is about, and
    // its own measured totals (0.333x to 13.26x the true one, instead of a clean `1/g`) were a property of that
    // rounding, not of a mis-inferred quantum. The real mechanism is exact division: when a run's true counts
    // already share divisor `g`, the printed values sit exactly on the quantum `g/N`, nothing is lost, and the
    // inference finds that quantum precisely. Rebuilt as such:
    //
    // 1. `k_j` is the candidate's own true, resolvable count at each cell (unchanged, from its real true
    //    quantum, verified large-m as before);
    // 2. `k'_j = g * round(k_j / g)` -- counts that genuinely share divisor `g`, the same shape, a different,
    //    legitimate run, not a corruption of this one; `N' = sum_j k'_j` is exactly `g` times an integer by
    //    construction;
    // 3. the **artefact** form prints `v_j = k'_j / N'`. Built as an actual `ResultCell[]` and run through
    //    `TryInferRunQuantum`/`ReconstructArrayTotals` for real (not assumed): the coordinator's own claim is
    //    that inference reads this at quantum `g / N'`, reconstructing `N' / g` -- checked below, not taken on
    //    faith;
    // 4. the **counterfactual** is the same counts read at quantum `1 / N'` (what the pipeline would have seen
    //    had the gcd been 1) -- not run through inference at all, deliberately: `TryInferRunQuantum` on the
    //    exact same `v_j` would only rediscover `g / N'` again, correctly, since these particular counts do
    //    share the divisor; "as if they hadn't" is a different, hypothetical file, so its own total (`N'`) and
    //    quantum (`1/N'`) are the honest arithmetic value, not a further pipeline call;
    // 5. both totals feed the same `BetaBinomialInterval` (same `pHat`/`rho`, read from the untouched R-1
    //    replica pool exactly as before, the same separation of the candidate's total from the pool's shape
    //    parameters `BetaBinomialCountFloor` used before it was removed 2026-09-25, decision XX), and the ratio
    //    of the two half-widths in value units is compared to
    //    `sqrt(g)`.
    private readonly record struct DivisorArtefactResult(
        string Formulation, Layout Layout, string Unit, int G, int CellIndex,
        double PHat, double RhoHat, int M, long NPrime,
        double QArtefactExpected, double QArtefactMeasured, double NStarArtefactMeasured,
        double FloorArtefactValueUnits, double FloorCounterfactualValueUnits,
        double MeasuredRatio, double PredictedRatio);

    [Trait("Category", "Long")]
    [Fact]
    public void QuantumMisinferenceCostHalfWidthScalesWithSqrtGOnLargeMUnits()
    {
        // The same large-m units as before (m >= 10, verified below, never assumed) -- not cherry-picked for the
        // result, picked only for having enough resolvable cells that the candidate's own true quantum is
        // trustworthy (the closed-form mis-inference chance at m = 10 is already down to 0.02%).
        var candidates = new (string Formulation, Layout Layout, string Unit)[]
        {
            ("HPEPA3", Layout.Original, "coef"),
            ("HPEPA3", Layout.Independent, "coef"),
            ("inpt", Layout.Original, "coef"),
            ("P33", Layout.Original, "coef"),
            ("PSAN02n", Layout.Independent, "coef"),
            ("HMX", Layout.Original, "coef"),
            ("HPEPA3", Layout.Original, "fqkarm"),
            ("inpt", Layout.Independent, "fqkarm_cor"),
            ("P33", Layout.Original, "fqmkm2"),
            ("HMX", Layout.Original, "fqdokkarm(20,:)"),
            ("HMX", Layout.Independent, "fqdokkarm(21,:)"),
            ("PSAN02n", Layout.Original, "fqdokkarm(17,:)"),
        };

        // Fixed and identical for the artefact/counterfactual pair in every case: the ratio under test is a
        // ratio of widths at the same alpha, so alpha's own value does not enter the comparison.
        const double alpha = 0.001;

        var results = new List<DivisorArtefactResult>();
        var skipped = new List<string>();

        foreach (var (formulation, layout, unit) in candidates)
        {
            var replicaCount = ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount;
            var directory = layout == Layout.Original ? "replicas-lagged" : "replicas-independent";
            var allReplicas = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                var path = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, k + ".m.txt");
                allReplicas.Add(ResultsMFile.ParseCells(path));
            }

            var label = $"{formulation} {layout} {unit}";

            if (!allReplicas[0].TryGetValue(unit, out var candidateArray))
            {
                skipped.Add($"{label}: replica 1 (the candidate role) never printed this unit.");
                continue;
            }

            if (!RunQuantum.TryInfer(candidateArray, out var qTrueEstimate))
            {
                skipped.Add($"{label}: the candidate's own true quantum could not be inferred.");
                continue;
            }

            var candidateM = CountNonZeroResolvableCells(candidateArray, qTrueEstimate);
            if (candidateM < 10)
            {
                skipped.Add($"{label}: candidate m={candidateM} < 10, not a large-m unit for this replica.");
                continue;
            }

            // The candidate's own true, resolvable counts -- step 1. A non-zero cell unresolvable at qTrue
            // carries no count information (the same "resolvable" gate TryInferRunQuantum itself applies) and is
            // left out of the shape entirely, exactly as the production reconstruction already treats it.
            var trueCounts = new List<double>();
            foreach (var cell in candidateArray)
            {
                if (Math.Abs(cell.Value) <= 0.0)
                {
                    trueCounts.Add(0.0);
                }
                else if (qTrueEstimate.Quantum > cell.Resolution)
                {
                    trueCounts.Add(Math.Round(Math.Abs(cell.Value) / qTrueEstimate.Quantum));
                }
            }

            var pool = allReplicas.Skip(1).ToList();
            var poolTotals = CountReconstruction.ArrayTotals(pool, unit);
            var dispersion = DispersionEstimator.Estimate(pool, unit, poolTotals);
            if (dispersion is not { } d || d.Phi < 1.0)
            {
                skipped.Add($"{label}: pool dispersion unavailable or phi < 1 (not the beta-binomial regime).");
                continue;
            }

            var (poolPerCellCounts, poolTotalsList) = CountReconstruction.PerCellCounts(pool, unit, poolTotals);
            if (poolPerCellCounts.Count == 0)
            {
                skipped.Add($"{label}: the replica pool produced no per-cell counts.");
                continue;
            }

            // The best-populated cell across the pool: pHat is degenerate, or too noisy to trust, on a sparse one.
            var maxIndex = 0;
            foreach (var counts in poolPerCellCounts)
            {
                foreach (var index in counts.Keys)
                {
                    maxIndex = Math.Max(maxIndex, index);
                }
            }

            var bestIndex = -1;
            var bestSum = -1.0;
            for (var j = 0; j <= maxIndex; j++)
            {
                var sum = 0.0;
                foreach (var counts in poolPerCellCounts)
                {
                    if (counts.TryGetValue(j, out var k))
                    {
                        sum += k;
                    }
                }

                if (sum > bestSum)
                {
                    bestSum = sum;
                    bestIndex = j;
                }
            }

            var sumPoolTotals = poolTotalsList.Sum();
            var pHat = sumPoolTotals > 0.0 ? bestSum / sumPoolTotals : 0.0;
            if (pHat is <= 0.0 or >= 1.0)
            {
                skipped.Add($"{label}: pHat degenerate ({pHat:G4}) at its best-populated cell.");
                continue;
            }

            foreach (var g in new[] { 2, 3 })
            {
                // Step 2: counts that genuinely share divisor g -- a different, legitimate run of the same
                // shape. M = sum_j round(k_j / g) is an ordinary integer sum; N' = g * M exactly, by construction
                // (kPrime_j is g times an integer for every j, so their sum is too).
                var kPrime = trueCounts.Select(k => g * Math.Round(k / g)).ToList();
                var m = (long)Math.Round(kPrime.Sum() / g);
                var nPrime = g * m;
                if (m <= 0)
                {
                    skipped.Add($"{label} g={g}: the shared-divisor construction produced a zero or negative total.");
                    continue;
                }

                // Step 3: the artefact form, printed for real and run through the real pipeline. Resolution is
                // set far finer than any real print (1e-12 relative, floored) so the inference is governed by
                // the exact arithmetic of shared-divisor counts, not by an accidental resolution collision --
                // this construction has no printing step to honor a real resolution for (root BOOT.md Taboos:
                // "no second implementation of a formula" does not apply to a resolution choice, only to the
                // quantum-fit formula itself, which is StatisticalCriterion's own, called unchanged below).
                var artefactCells = kPrime.Select(kp =>
                {
                    var value = kp / nPrime;
                    var resolution = value == 0.0 ? 1e-300 : Math.Abs(value) * 1e-12;
                    return new ResultCell(value, resolution, false);
                }).ToArray();

                if (!RunQuantum.TryInfer(artefactCells, out var qArtefactEstimate))
                {
                    skipped.Add($"{label} g={g}: the artefact array's own quantum could not be inferred.");
                    continue;
                }

                var wrapArtefact = new List<IReadOnlyDictionary<string, ResultCell[]>>
                {
                    new Dictionary<string, ResultCell[]> { [unit] = artefactCells },
                };
                var nStarArtefact = CountReconstruction.ArrayTotals(wrapArtefact, unit)[0];
                if (nStarArtefact is not { } nArtefact)
                {
                    skipped.Add($"{label} g={g}: the artefact array's own total could not be reconstructed.");
                    continue;
                }

                // Step 4: the counterfactual, by direct arithmetic, never by inference on the same v_j (which
                // would only rediscover the shared divisor, correctly, since it is really there).
                var quantumCounterfactual = 1.0 / nPrime;

                var (Mean, Low, High, AttainedAlpha) = BetaBinomialPredictive.Interval((long)Math.Round(nArtefact), pHat, d.RhoHat, alpha);
                var counterfactualInterval = BetaBinomialPredictive.Interval(nPrime, pHat, d.RhoHat, alpha);

                var halfWidthArtefactCounts = Math.Max(High - Mean, Mean - Low);
                var halfWidthCounterfactualCounts = Math.Max(
                    counterfactualInterval.High - counterfactualInterval.Mean, counterfactualInterval.Mean - counterfactualInterval.Low);

                var floorArtefact = halfWidthArtefactCounts * qArtefactEstimate.Quantum;
                var floorCounterfactual = halfWidthCounterfactualCounts * quantumCounterfactual;

                var measuredRatio = floorCounterfactual > 0.0 ? floorArtefact / floorCounterfactual : double.NaN;
                var predictedRatio = Math.Sqrt(g);

                results.Add(new DivisorArtefactResult(
                    formulation, layout, unit, g, bestIndex, pHat, d.RhoHat, (int)m, nPrime,
                    1.0 / m, qArtefactEstimate.Quantum, nArtefact,
                    floorArtefact, floorCounterfactual, measuredRatio, predictedRatio));
            }
        }

        foreach (var r in results)
        {
            _output.WriteLine(
                $"{r.Formulation,-8} {r.Layout,-11} {r.Unit,-20} g={r.G} cellIdx={r.CellIndex,4} " +
                $"pHat={r.PHat:G4} rho={r.RhoHat:G4} M={r.M} N'={r.NPrime} (N'/M={(double)r.NPrime / r.M:G4}, expected g={r.G}) " +
                $"qArtefact(expected)={r.QArtefactExpected:G6} qArtefact(measured)={r.QArtefactMeasured:G6} " +
                $"nStarArtefact(measured)={r.NStarArtefactMeasured:G6} (expected M={r.M}) " +
                $"floorArtefact={r.FloorArtefactValueUnits:G6} floorCounterfactual={r.FloorCounterfactualValueUnits:G6} " +
                $"measuredRatio={r.MeasuredRatio:G6} predicted(sqrt g)={r.PredictedRatio:G6} " +
                $"measured/predicted={r.MeasuredRatio / r.PredictedRatio:G4}");
        }

        foreach (var s in skipped)
        {
            _output.WriteLine("skipped: " + s);
        }

        if (results.Count > 0)
        {
            _output.WriteLine(string.Empty);
            foreach (var g in new[] { 2, 3 })
            {
                var atG = results.Where(r => r.G == g).Select(r => r.MeasuredRatio).Where(double.IsFinite).ToList();
                if (atG.Count == 0)
                {
                    continue;
                }

                _output.WriteLine(
                    $"g={g}: n={atG.Count} median(measured ratio)={Median(atG):G6} " +
                    $"min={atG.Min():G6} max={atG.Max():G6} predicted=sqrt({g})={Math.Sqrt(g):G6}");
            }
        }

        Assert.True(results.Count > 0, "expected at least one large-m unit to survive the eligibility checks above.");
    }

    // Coordinator's second follow-up, the population the m >= 7 cutoff (2^-7 + 3^-7 + ... = 0.83%, the point
    // item 1's closed form drops below one per cent) would remove: how many units, and how many of their own
    // resolvable cells, sit at each m. Reuses this file's own `TryInferRunQuantum`/`CountNonZeroResolvableCells`
    // over the identical unit population the first Fact measures -- not a second implementation, the same
    // per-unit median m, read again for a different purpose (sizing a cutoff, not choosing an interval branch).
    [Trait("Category", "Long")]
    [Fact]
    public void MPopulationAndCellsRemovedIfCountRuleRequiredMAtLeast7()
    {
        var perUnitM = new List<(string Formulation, Layout Layout, string Unit, double MedianM)>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            foreach (var layout in new[] { Layout.Original, Layout.Independent })
            {
                var directory = layout == Layout.Original ? "replicas-lagged" : "replicas-independent";
                var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
                for (var k = 1; k <= replicaCount; k++)
                {
                    var path = RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, k + ".m.txt");
                    replicaCells.Add(ResultsMFile.ParseCells(path));
                }

                var dokRowNames = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var cells in replicaCells)
                {
                    foreach (var key in cells.Keys)
                    {
                        if (key.StartsWith("fqdokkarm(", StringComparison.Ordinal))
                        {
                            _ = dokRowNames.Add(key);
                        }
                    }
                }

                foreach (var unit in FixedFamilies.Concat(dokRowNames))
                {
                    var perReplicaM = new List<int>();
                    foreach (var cells in replicaCells)
                    {
                        if (cells.TryGetValue(unit, out var array) && RunQuantum.TryInfer(array, out var estimate))
                        {
                            perReplicaM.Add(CountNonZeroResolvableCells(array, estimate));
                        }
                    }

                    if (perReplicaM.Count == 0)
                    {
                        continue;
                    }

                    perUnitM.Add((formulation, layout, unit, Median(perReplicaM.Select(m => (double)m).ToList())));
                }
            }
        }

        var byExactM = perUnitM
            .GroupBy(u => (int)Math.Round(u.MedianM))
            .OrderBy(g => g.Key)
            .ToList();

        _output.WriteLine("--- units and cells (= units' own m) by exact median m, from its minimum upward ---");
        var totalCells = 0.0;
        foreach (var group in byExactM)
        {
            var cells = group.Sum(u => u.MedianM);
            totalCells += cells;
            _output.WriteLine($"m={group.Key,4} units={group.Count(),4} cells(sum of own m)={cells,10:G6}");
        }

        var unitsBelow7 = byExactM.Where(g => g.Key < 7).Sum(g => g.Count());
        var cellsBelow7 = byExactM.Where(g => g.Key < 7).Sum(g => g.Sum(u => u.MedianM));
        var fraction = totalCells > 0.0 ? cellsBelow7 / totalCells : double.NaN;

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"if the count rule required m >= 7 (closed-form P(shared divisor) at m=7 is " +
            $"{ClosedFormSharedDivisorProbability(7):P2}): {unitsBelow7} of {perUnitM.Count} units " +
            $"({(double)unitsBelow7 / perUnitM.Count:P2}) and {cellsBelow7:G6} of {totalCells:G6} cells " +
            $"({fraction:P2}) would leave the count rule.");

        Assert.True(perUnitM.Count > 0);
    }

    private static UnitResult MeasureOneUnit(
        string formulation, int replicaCount, Layout layout, string unit,
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells)
    {
        var totals = CountReconstruction.ArrayTotals(replicaCells, unit);

        var produced = new List<double>();
        foreach (var t in totals)
        {
            if (t is { } value)
            {
                produced.Add(value);
            }
        }

        // Coordinator follow-up: the same per-replica `TryInferRunQuantum` call `ReconstructArrayTotals` already
        // makes internally, repeated here only to read its own resolvable non-zero cell count `m` -- `m` is not
        // part of `QuantumEstimate` and cannot be read off `ReconstructArrayTotals`'s own return value, so
        // reading it means re-running the same (shared, internal) inference function again. The total itself is
        // never recomputed here; it still comes from `ReconstructArrayTotals` alone, above.
        var perReplicaM = new List<int>();
        foreach (var cells in replicaCells)
        {
            if (cells.TryGetValue(unit, out var array) && RunQuantum.TryInfer(array, out var estimate))
            {
                perReplicaM.Add(CountNonZeroResolvableCells(array, estimate));
            }
        }

        int? minM = null;
        double? medianM = null;
        if (perReplicaM.Count > 0)
        {
            perReplicaM.Sort();
            minM = perReplicaM[0];
            medianM = Median(perReplicaM.Select(m => (double)m).ToList());
        }

        if (produced.Count < 2)
        {
            // Not even a sample variance is defined below two points; reported as not comparable, not silently
            // skipped (decision X, item 2: "is not compared, is counted, and is reported").
            return new UnitResult(formulation, layout, unit, produced.Count, replicaCount,
                double.NaN, double.NaN, double.NaN, false, false, minM, medianM);
        }

        var mean = produced.Average();
        var sumSquares = 0.0;
        foreach (var t in produced)
        {
            sumSquares += (t - mean) * (t - mean);
        }

        var variance = sumSquares / (produced.Count - 1);
        var phiT = mean > 0.0 ? variance / mean : double.NaN;
        var comparable = produced.Count >= MinimumContributingReplicas;
        var signature = HasIntegerRatioSignature(produced);

        return new UnitResult(
            formulation, layout, unit, produced.Count, replicaCount, mean, variance, phiT, comparable, signature, minM, medianM);
    }

    // Coordinator follow-up: the number of non-zero cells that actually determined a run's own accepted quantum
    // -- the same "resolvable" test `TryInferRunQuantum`'s own internal fit already applies to each candidate `q`
    // (a non-zero cell whose value would not clear its own print resolution at `q` carries no information and is
    // excluded), re-run here against the *accepted* estimate only, never against a candidate that was rejected.
    private static int CountNonZeroResolvableCells(IReadOnlyList<ResultCell> array, RunQuantum.QuantumEstimate estimate)
    {
        var count = 0;
        foreach (var cell in array)
        {
            if (Math.Abs(cell.Value) <= 0.0)
            {
                continue;
            }

            if (estimate.Quantum <= cell.Resolution)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    // fqdokkarm's own rows are one printed family, indexed by row; every other unit is its own family.
    private static string Family(string unit) => unit.StartsWith("fqdokkarm(", StringComparison.Ordinal) ? "fqdokkarm" : unit;

    private static bool HasIntegerRatioSignature(List<double> totals)
    {
        for (var i = 0; i < totals.Count; i++)
        {
            for (var j = i + 1; j < totals.Count; j++)
            {
                var a = totals[i];
                var b = totals[j];
                if (a <= 0.0 || b <= 0.0)
                {
                    continue;
                }

                var ratio = Math.Max(a, b) / Math.Min(a, b);
                var rounded = Math.Round(ratio);
                if (rounded is < 2.0 or > MaxSmallIntegerRatio)
                {
                    continue;
                }

                if (Math.Abs(ratio - rounded) / rounded <= IntegerRatioRelativeTolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // this file's own bucketing for the m-grouped reports (coordinator follow-up): 1..5 individually, then
    // 6-9 and 10+, exactly the groups the coordinator named. A fractional median (e.g. 2.5, from an even number
    // of contributing replicas) rounds to the nearer bucket boundary below.
    private static string MBucket(double medianM)
    {
        if (medianM < 1.5)
        {
            return "1";
        }

        if (medianM < 2.5)
        {
            return "2";
        }

        if (medianM < 3.5)
        {
            return "3";
        }

        if (medianM < 4.5)
        {
            return "4";
        }

        if (medianM < 5.5)
        {
            return "5";
        }

        return medianM < 9.5 ? "6-9" : "10+";
    }

    private static int MBucketOrder(string bucket) => bucket switch
    {
        "1" => 1,
        "2" => 2,
        "3" => 3,
        "4" => 4,
        "5" => 5,
        "6-9" => 6,
        _ => 7,
    };

    private static double ClosedFormSharedDivisorProbability(int m)
    {
        var sum = 0.0;
        foreach (var p in SmallPrimes)
        {
            sum += Math.Pow(p, -m);
        }

        return sum;
    }

    private static int[] ComputePrimesBelow(int bound)
    {
        var isComposite = new bool[bound];
        var primes = new List<int>();
        for (var candidate = 2; candidate < bound; candidate++)
        {
            if (isComposite[candidate])
            {
                continue;
            }

            primes.Add(candidate);
            for (var multiple = candidate * 2; multiple < bound; multiple += candidate)
            {
                isComposite[multiple] = true;
            }
        }

        return primes.ToArray();
    }

    private static double Median(List<double> sortedValues)
    {
        var sorted = new List<double>(sortedValues);
        sorted.Sort();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }
}
