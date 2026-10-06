using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// `tests/Harness/HISTORY.md#fable-5-1-decision-ii`, "Fable 5.1 decision II: the calibration curve" and "Fable 5.1 decision III: the discrete
/// rules and the calibration gate": at each of the three per-cell levels 0.05/0.01/0.001, the blind failure
/// fraction over the leave-one-out candidates' own non-degenerate cell tests — pooling
/// <see cref="StatisticalCriterion.Compare"/>, <see cref="StatisticalCriterion.CompareTailRowMean"/> and
/// <see cref="StatisticalCriterion.CompareAdaptiveIndexMatched"/> the same way gate 2 does — must lie inside the
/// exact binomial highest-density region of the level, for the Student and Count rules
/// (<see cref="CalibrationRule"/>). 0.05 and 0.01 are gated per formulation; 0.001 is gated
/// pooled over all five (decision III, item 3(iv): a per-formulation 0.001 band has too little power for one
/// contaminated replica not to move it past either edge). The Count rule is gated against its own ATTAINED level
/// (decision III, item 1), not the nominal one, since a discrete boundary cannot land exactly on a continuous
/// target. The Mass rule has no level to calibrate and is checked separately, one-sided, as a containment check
/// (decision III, item 2) — never as a curve row, and never read as a pass when it has no cells (decision III,
/// item 3(iii): `N = 0` for Student or Count is an error of the gate, not a green reading).
///
/// Decision VI (2026-09-20, the orchestrator — this node's BOOT.md names the full decision; superseded by
/// Fable 5.1's own answer if the model later disagrees): the population each rule's own `N`/`K` is drawn from is
/// restricted to <see cref="CellVerdict.Eligible"/> cells — a cell whose own interval
/// already covers every value its quantity could print says nothing about calibration, and pooling it in dilutes
/// the count with a cell that could never have failed either way. The pre-eligibility population is reported
/// alongside (`NonDegenerate`), so a reader sees exactly how much the population moved, never a silently smaller
/// `N`. Eligibility never touches `m` (`StatisticalCriterion.Alpha / m`, `Compare`'s own family-wise budget):
/// an ineligible cell still spent its share of `m` the moment its interval was computed (decision VI, item 4).
/// </summary>
public class CalibrationCurveTests
{
    private readonly ITestOutputHelper _output;

    public CalibrationCurveTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly double[] Levels = [0.05, 0.01, 0.001];
    private const double Pooled001 = 0.001;

    private sealed class RuleAccumulator
    {
        // Every non-degenerate cell of this rule, eligible or not — reported beside the eligible-only figures
        // below so a reader sees the population `Eligible` actually excludes (decision VI, item 3).
        public long NonDegenerateCompared;

        // The calibration curve's own denominator/numerator (decision VI): eligible, non-degenerate cells only.
        public long Compared;
        public long Failures;
        public double AttainedAlphaSum;
        public long AttainedAlphaCount;

        // The same eligible, non-degenerate cells, split by leave-one-out run: the report of
        // `tests/Harness/HISTORY.md#calibration-band-not-from-its-own-failures-2026-10-02`, never a band.
        public List<RunTally> Runs { get; } = [];
    }

    private sealed class RunTally(string formulation, int ordinal)
    {
        public string Label { get; } = $"{formulation} r{ordinal}";
        public long Compared;
        public long Failures;
    }

    // `tests/Harness/HISTORY.md#decision-ix-full-reasoning`, item 2's own third bullet: a non-
    // degenerate cell whose threshold the count floor governs (`Rule == Count`) but whose own `AttainedAlpha` is
    // unknown (the `phi < 1` route, forced null since decision XX folded `BetaBinomialCountFloor`'s old routing
    // into `CountFloorFromCounts` and removed the former as unused, 2026-09-25; or `CountFloorFromCounts`'s own
    // `MaxCountFloorTotal` cap) enters neither the Student row (the count floor, not the Student band, set its
    // threshold) nor the Count row (there is no attained level to credit it with, or to average into `p`). It is
    // its own reported population, pooled by level — never folded into `m` (decision IX, item 3: `m` does not
    // move; a cell here still spent its share of `m` when its interval was built).
    private sealed class UngovernedAccumulator
    {
        public long Count;
        public long Failures;
    }

    [Trait("Category", "Long")]
    [Fact]
    public void Gate3CalibrationCurveWithinBinomialBandPerRulePerLevel()
    {
        var totalRuns = 0;
        var violations = new List<string>();
        var table = new List<string>();

        // decision III, item 3(iv): the 0.001 level is gated pooled over the five formulations, not per
        // formulation, so it needs a running total across the whole outer formulation loop.
        var pooled001 = new Dictionary<CalibrationRule, RuleAccumulator>();

        // decision III, item 2: the Mass rule's own containment check, pooled over every leave-one-out run
        // regardless of alpha level (a Mass cell's own verdict never reads alpha, so it is filled once, during
        // the alpha = 0.05 pass, rather than three times over).
        long massCompared = 0;
        long massFailures = 0;
        long massNonDegenerateCompared = 0;
        var bonferroniLevelSum = 0.0;
        long bonferroniLevelCount = 0;

        // decision IX, item 2's own third bullet: population 2c, pooled by level across every formulation (the
        // same pooling `Levels`'s own 0.001 row already applies to `byRule`).
        var ungovernedByAlpha = new Dictionary<double, UngovernedAccumulator>();

        // B2b's report (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`): the `Count` row's own cells split by
        // family, never a band; the identity against the row is checked below, the only assertion it carries.
        var coverage = CountCoverageReport.Create();
        var coverageIdentity = new List<string>();

        foreach (var alpha in Levels)
        {
            foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
            {
                var byRule = new Dictionary<CalibrationRule, RuleAccumulator>();
                for (var k = 1; k <= replicaCount; k++)
                {
                    if (alpha == Levels[0])
                    {
                        totalRuns++;
                    }

                    var candidate = ResultsMFile.Parse(ReplicaPath(formulation, k));
                    var compareVerdicts = StatisticalCriterion.CompareCellVerdicts(
                        formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k);

                    if (alpha == Levels[0])
                    {
                        // decision III, item 2: this run's own per-cell Bonferroni level is Alpha / m, m being
                        // this same run's own Compared count — the identical pending list Compare itself builds
                        // (one cell-building path, this node's own established rule), not a second m.
                        var m = compareVerdicts.Count;
                        var runHasMass = false;
                        foreach (var v in compareVerdicts)
                        {
                            if (v.Rule != CalibrationRule.Mass)
                            {
                                continue;
                            }

                            runHasMass = true;
                            massNonDegenerateCompared++;

                            // decision VI, item 3/4: the containment check's own N/K are eligible cells only (`m`
                            // above is untouched by this filter — it is read from `compareVerdicts.Count` before
                            // any filtering at all, decision VI item 4's own requirement).
                            if (!v.Eligible)
                            {
                                continue;
                            }

                            massCompared++;
                            if (v.Failed)
                            {
                                massFailures++;
                            }
                        }

                        if (runHasMass && m > 0)
                        {
                            bonferroniLevelSum += StatisticalCriterion.Alpha / m;
                            bonferroniLevelCount++;
                        }
                    }

                    var tallies = new Dictionary<CalibrationRule, RunTally>
                    {
                        [CalibrationRule.Student] = new RunTally(formulation, k),
                        [CalibrationRule.Count] = new RunTally(formulation, k),
                    };

                    var otherVerdicts = new List<CellVerdict>();
                    otherVerdicts.AddRange(StatisticalCriterion.CompareTailRowMeanCellVerdicts(
                        formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k));
                    otherVerdicts.AddRange(StatisticalCriterion.CompareAdaptiveIndexMatchedCellVerdicts(
                        formulation, candidate, ReplicaKind.Lagged, alpha, excludeReplicaOrdinal: k));

                    foreach (var v in Enumerable.Concat(compareVerdicts, otherVerdicts))
                    {
                        // Mass cells never enter the curve (decision III, item 2); handled above, once.
                        if (v.Rule == CalibrationRule.Mass || v.Degenerate)
                        {
                            continue;
                        }

                        // decision IX, item 2's own third bullet: the count floor governs but no attained level
                        // is known — its own population, not `Student` and not a scoreable `Count`.
                        if (v.Rule == CalibrationRule.Count && v.AttainedAlpha is null)
                        {
                            if (!ungovernedByAlpha.TryGetValue(alpha, out var ungoverned))
                            {
                                ungoverned = new UngovernedAccumulator();
                                ungovernedByAlpha[alpha] = ungoverned;
                            }

                            ungoverned.Count++;
                            if (v.Failed)
                            {
                                ungoverned.Failures++;
                            }

                            continue;
                        }

                        if (!byRule.TryGetValue(v.Rule, out var accumulator))
                        {
                            accumulator = new RuleAccumulator();
                            byRule[v.Rule] = accumulator;
                        }

                        accumulator.NonDegenerateCompared++;

                        // decision III, item 1: a valid two-sided integer interval is conservative by
                        // construction, so its own attained level must never exceed the nominal one it was
                        // computed at; this is the per-cell check that keeps that guarantee visible rather
                        // than assumed. Checked on every non-degenerate cell, eligible or not — it is a
                        // correctness invariant of the formula itself, not a calibration-population question.
                        if (v.AttainedAlpha is { } attainedForSanity && attainedForSanity > alpha + 1e-12)
                        {
                            violations.Add(
                                $"{formulation} alpha={alpha} Count[{v.Name}[{v.Index}]]: attained level {attainedForSanity:G6} EXCEEDS nominal {alpha}");
                        }

                        // decision VI (2026-09-20, the orchestrator): the curve's own denominator/numerator are
                        // eligible cells only — an ineligible cell's interval already covers everything its
                        // quantity could print, so it could never have failed either way and dilutes the count
                        // rather than measuring it.
                        if (!v.Eligible)
                        {
                            continue;
                        }

                        accumulator.Compared++;
                        if (v.Rule == CalibrationRule.Count)
                        {
                            coverage.Add(formulation, alpha, v);
                        }

                        var tally = tallies[v.Rule];
                        tally.Compared++;
                        if (v.Failed)
                        {
                            accumulator.Failures++;
                            tally.Failures++;
                        }

                        if (v.AttainedAlpha is { } attained)
                        {
                            accumulator.AttainedAlphaSum += attained;
                            accumulator.AttainedAlphaCount++;
                        }
                    }

                    foreach (var (rule, tally) in tallies)
                    {
                        if (!byRule.TryGetValue(rule, out var runAccumulator))
                        {
                            runAccumulator = new RuleAccumulator();
                            byRule[rule] = runAccumulator;
                        }

                        runAccumulator.Runs.Add(tally);
                    }
                }

                var countRow = byRule.GetValueOrDefault(CalibrationRule.Count);
                coverageIdentity.AddRange(coverage.VerifyAgainstRow(
                    formulation, alpha, countRow?.Compared ?? 0, countRow?.Failures ?? 0, countRow?.AttainedAlphaSum ?? 0.0));

                if (alpha == Pooled001)
                {
                    foreach (var (rule, accumulator) in byRule)
                    {
                        if (!pooled001.TryGetValue(rule, out var pooledAccumulator))
                        {
                            pooledAccumulator = new RuleAccumulator();
                            pooled001[rule] = pooledAccumulator;
                        }

                        pooledAccumulator.NonDegenerateCompared += accumulator.NonDegenerateCompared;
                        pooledAccumulator.Compared += accumulator.Compared;
                        pooledAccumulator.Failures += accumulator.Failures;
                        pooledAccumulator.AttainedAlphaSum += accumulator.AttainedAlphaSum;
                        pooledAccumulator.AttainedAlphaCount += accumulator.AttainedAlphaCount;
                        pooledAccumulator.Runs.AddRange(accumulator.Runs);
                    }
                }
                else
                {
                    GateOneRow(formulation, alpha, CalibrationRule.Student, byRule.GetValueOrDefault(CalibrationRule.Student), table, violations);
                    GateOneRow(formulation, alpha, CalibrationRule.Count, byRule.GetValueOrDefault(CalibrationRule.Count), table, violations);
                }
            }
        }

        GateOneRow("(pooled x5)", Pooled001, CalibrationRule.Student, pooled001.GetValueOrDefault(CalibrationRule.Student), table, violations);
        GateOneRow("(pooled x5)", Pooled001, CalibrationRule.Count, pooled001.GetValueOrDefault(CalibrationRule.Count), table, violations);

        var pooledCountRow = pooled001.GetValueOrDefault(CalibrationRule.Count);
        coverageIdentity.AddRange(coverage.VerifyPooledAgainstRow(
            Pooled001, pooledCountRow?.Compared ?? 0, pooledCountRow?.Failures ?? 0, pooledCountRow?.AttainedAlphaSum ?? 0.0));
        table.AddRange(coverage.Render([.. ResultsMFileTests.Formulations.Select(f => f.Name)], Levels));

        // decision IX, item 2's own third bullet, and item 4: report the size of population 2c plainly, whatever
        // it is — a population whose interval is set by a term (the count floor) whose level nobody knows. Not a
        // gate row and never asserted against: there is no attained level to build a binomial band from.
        var ungovernedTotal = 0L;
        var ungovernedFailuresTotal = 0L;
        foreach (var alpha in Levels)
        {
            var pop = ungovernedByAlpha.GetValueOrDefault(alpha);
            var count = pop?.Count ?? 0;
            var failures = pop?.Failures ?? 0;
            ungovernedTotal += count;
            ungovernedFailuresTotal += failures;
            table.Add($"Ungoverned (count floor governs, level unknown): alpha={alpha,-6} N={count,6} K={failures,5} (enters neither curve row)");
        }

        table.Add($"Ungoverned (count floor governs, level unknown): TOTAL across all three levels N={ungovernedTotal,6} K={ungovernedFailuresTotal,5}");

        // decision III, item 2: the Mass rule's own containment check — one-sided (only the upper limit matters:
        // an unusually LOW Mass-rule failure rate is not evidence of a defect the way an unusually high one is),
        // declared as a deterministic containment check, not a statistical calibration test.
        if (bonferroniLevelCount == 0)
        {
            table.Add("Mass containment (pooled): N=0 -- ERROR (no leave-one-out run ever produced a Mass-rule cell; this check has never been exercised).");
            violations.Add("Mass containment: N=0, an error of the gate (decision III, item 3(iii) applied to the containment check).");
        }
        else
        {
            var meanBonferroniLevel = bonferroniLevelSum / bonferroniLevelCount;
            var (_, upperLimit) = BinomialBand.HighestDensityRegion(massCompared, meanBonferroniLevel, StatisticalCriterion.Alpha);
            var contained = massFailures <= upperLimit;
            table.Add(
                $"Mass containment (pooled): N={massCompared,6} (of {massNonDegenerateCompared,6} non-degenerate) K={massFailures,5} meanBonferroniLevel={meanBonferroniLevel:G4} upperLimit={upperLimit} {(contained ? "OK" : "VIOLATION")}");
            if (!contained)
            {
                violations.Add(
                    $"Mass containment: N={massCompared} K={massFailures} exceeds the upper binomial limit {upperLimit} at meanBonferroniLevel={meanBonferroniLevel:G4}");
            }
        }

        foreach (var line in table)
        {
            _output.WriteLine(line);
        }

        Assert.True(
            coverageIdentity.Count == 0,
            $"the per-family Count report no longer adds up to the calibration curve's own Count row: {string.Join("; ", coverageIdentity)}");

        // Non-degeneracy of the "96" figure itself, the same check gate 2 makes of its own denominator.
        Assert.True(totalRuns == 96, $"expected 96 total leave-one-out runs (32 + 16*4), counted {totalRuns}.");

        // ⚠ 2026-09-24: was `Assert.True(violations.Count == 0, ...)` — a bound this gate has never once met
        // (decision II through decision XX's own re-measurements, this node's BOOT.md, "Fable 5.1 decision II"
        // onward, every one red). Root BOOT.md's own rate decision the same date this note is dated reads the
        // *per-run* criterion's own known miscalibration as the reason a rate, not a single run, is the right
        // pass condition (root BOOT.md, "the pass condition compares failure rates, not single runs") — this
        // gate measures exactly that miscalibration, at three levels and two rules, so a bound of zero was
        // always going to stay red for the same underlying reason the rate decision now works around. A
        // perpetually red gate is what AGENTS.md §13 forbids; this is the same fix Gate2's own leave-one-out
        // set and `HmxOwnGsv2ReferenceMatchesTheKnownOpenCell` take above — a ratchet on the recorded set, not
        // a bound the gate cannot meet. Every row's own figures are still printed in full (`table`, above); the
        // key compared here is only "which row", stable across small numeric drift in `N`/`K`/the band itself,
        // the same "identity, not exact figures" ratchet `tests/Simulation.Tests`'s own retired seed-0 ratchet
        // used. This node's BOOT.md, "Decision IX" onward, has the per-row diagnosis; none of the underlying
        // machinery (the binomial band, the eligible-cell population, `alpha`) is loosened by this change.
        var violationKeys = new HashSet<string>(violations.Select(v => v[..v.IndexOf(':')]));
        var unexpectedViolations = violationKeys.Except(KnownCalibrationViolations).ToList();
        var resolvedViolations = KnownCalibrationViolations.Except(violationKeys).ToList();

        Assert.True(
            unexpectedViolations.Count == 0 && resolvedViolations.Count == 0,
            $"the calibration-curve violation set no longer matches the recorded set (this node's BOOT.md, " +
            $"\"Decision IX\" onward). {unexpectedViolations.Count} new/unexpected row(s): " +
            $"{string.Join(", ", unexpectedViolations)}. {resolvedViolations.Count} recorded row(s) now within " +
            $"band: {string.Join(", ", resolvedViolations)}. Full figures for every row are in the test output " +
            "above. Update the recorded set below and this node's BOOT.md together — never loosen alpha, a band " +
            "or the eligible-cell population to make this pass.");
    }

    // Measured 2026-09-24 (`tests/Harness/HISTORY.md#decision-ix-full-reasoning` onward, re-measured after the count-floor
    // boundary-centring fix): the rows the calibration curve violated, keyed by "scope alpha=X rule" (or "Mass
    // containment"), the stable identity `GateOneRow` always prefixes its own violation message with. Re-measured
    // 2026-09-27 after E1 (the mass bracket's feasible interval, this node's BOOT.md, "Mass bracket"): "Mass
    // containment" (K=0 of 2178, within its own upper limit) left. Re-recorded 2026-10-02 after B2a
    // (`tests/Harness/HISTORY.md#b2a-quantum-identification-2026-10-02`, amended by
    // `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`): inpt 0.05 and 0.01 Count, PSAN02n 0.05 Count and HMX 0.05
    // Student are now inside their bands; HPEPA3 and HMX at 0.05 and 0.01, Count, are above theirs (K 50 against at
    // most 49, 84 against 60, 16 against 14, 21 against 14) — moved by an edit only, together with the recorded
    // figures in this node's documents. Re-recorded 2026-10-02 after B2c
    // (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`): HPEPA3 at 0.05 and 0.01, Count, are inside their
    // bands (K 33 against at most 49; 12 against 14), the region's edge no longer failing; HMX at 0.05 and 0.01 stay
    // above theirs (70 against 60; 18 against 14) and the pooled 0.001 Count row too (13 against 6).
    private static readonly HashSet<string> KnownCalibrationViolations =
    [
        "PSAN02n alpha=0.05 Student",
        "HMX alpha=0.05 Count",
        "HMX alpha=0.01 Count",
        "(pooled x5) alpha=0.001 Student",
        "(pooled x5) alpha=0.001 Count",
    ];

    // ⚠ 2026-09-20: this test's own first run found NegativeBinomialInterval's walk silently returning
    // AttainedAlpha == 1 for any count-like cell whose total replica count passed a few hundred (pmf(0) = q^r
    // underflows to exactly 0.0 in double precision, so the walk from k = 0 never moved at all) — 94 226
    // violations, all of the form "attained level 1 EXCEEDS nominal", across every formulation. tests/Harness/
    // BOOT.md, "The Count rule's curve row" ⚠, has the full account; the fix anchors the walk at the
    // distribution's own mode (StudentDistribution.LogGamma, the same technique BinomialBand already uses)
    // instead of at k = 0. This is a direct regression guard on a real, previously-broken cell — not another
    // boundary search, since the fix changed a numerical starting point, not a rule's own threshold, so there is
    // no boundary to search for; the guard is that a real, large-count cell now attains a sane level at all.
    [Trait("Category", "Long")]
    [Fact]
    public void NegativeBinomialIntervalLargeTotalCountCellAttainedAlphaIsSaneNotDegenerate()
    {
        const string formulation = "HPEPA3";
        var reference = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt"));
        var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, reference, ReplicaKind.Lagged, 0.05);

        var underflowSignature = verdicts
            .Where(v => v.Rule == CalibrationRule.Count && v.AttainedAlpha is { } a && a > 0.9)
            .ToList();
        Assert.True(
            underflowSignature.Count == 0,
            $"{underflowSignature.Count} Count-rule cells still read an attained level near 1 (the underflow " +
            $"signature): {string.Join(", ", underflowSignature.Select(v => $"{v.Name}[{v.Index}]={v.AttainedAlpha:G6}"))}");

        // The specific cell this defect was found on, named directly: HPEPA3's own "coef" family reaches a total
        // count past the underflow threshold well within its own printed array.
        var coefVerdicts = verdicts.Where(v => v.Name == "coef" && v.Index == 443).ToList();
        Assert.True(coefVerdicts.Count == 1, $"expected exactly one HPEPA3 coef[443] verdict, found {coefVerdicts.Count}.");
        var coefVerdict = coefVerdicts[0];
        Assert.Equal(CalibrationRule.Count, coefVerdict.Rule);
        _ = Assert.NotNull(coefVerdict.AttainedAlpha);
        Assert.True(
            coefVerdict.AttainedAlpha!.Value <= 0.05 + 1e-9,
            $"coef[443]'s own attained level ({coefVerdict.AttainedAlpha}) still exceeds nominal — the underflow fix did not take on this exact cell.");
        Assert.True(
            coefVerdict.AttainedAlpha.Value < 0.9,
            $"coef[443]'s own attained level ({coefVerdict.AttainedAlpha}) is still near 1 — the underflow signature.");
    }

    // decision III, item 3(iii): "N = 0 for the Student or Count rule is an error of the gate, not a green
    // reading." The Count rule's own level is its mean ATTAINED alpha (decision III, item 1), not the nominal
    // one; the Student rule's level is the nominal alpha itself, unchanged from decision II.
    private static void GateOneRow(
        string scope, double alpha, CalibrationRule rule, RuleAccumulator? accumulator,
        List<string> table, List<string> violations)
    {
        if (accumulator is null || accumulator.Compared == 0)
        {
            table.Add($"{scope,-11} alpha={alpha,-6} {rule,-7}: N=0 -- ERROR (this row has never been red).");
            violations.Add($"{scope} alpha={alpha} {rule}: N=0, an error of the gate (decision III, item 3(iii)).");
            return;
        }

        double p;
        if (rule == CalibrationRule.Count)
        {
            if (accumulator.AttainedAlphaCount == 0)
            {
                table.Add($"{scope,-11} alpha={alpha,-6} {rule,-7}: N={accumulator.Compared} but no cell produced an attained level -- ERROR.");
                violations.Add($"{scope} alpha={alpha} {rule}: {accumulator.Compared} cells, none with an attained level.");
                return;
            }

            p = accumulator.AttainedAlphaSum / accumulator.AttainedAlphaCount;
        }
        else
        {
            p = alpha;
        }

        var (low, high) = BinomialBand.HighestDensityRegion(accumulator.Compared, p, StatisticalCriterion.Alpha);
        var withinBand = accumulator.Failures >= low && accumulator.Failures <= high;

        // decision VI, item 3: the eligible count is reported beside every reading, next to the pre-eligibility
        // (non-degenerate) population it was drawn from, so a change of population is visible, not silent.
        table.Add(
            $"{scope,-11} alpha={alpha,-6} {rule,-7}: N={accumulator.Compared,6} (of {accumulator.NonDegenerateCompared,6} non-degenerate) K={accumulator.Failures,5} p={p:G4} band=[{low},{high}] {(withinBand ? "OK" : "VIOLATION")}");
        table.Add($"{"",-11} {"",-6} {"",-7}  {ClusteringReport(accumulator)}");

        if (!withinBand)
        {
            violations.Add(
                $"{scope} alpha={alpha} {rule}: N={accumulator.Compared} K={accumulator.Failures} p={p:G4} band=[{low},{high}]");
        }
    }

    // `tests/Harness/HISTORY.md#calibration-band-not-from-its-own-failures-2026-10-02`: a report, never a band
    // and never asserted. `D` is the Pearson dispersion of the runs' failure counts about the row's own rate
    // `r = K / N`, `sum (K_i - r N_i)^2 / sum N_i r (1 - r)`; "n/a" when `K = 0`. The runs failing and the three
    // runs with the most failures are printed beside it, each as `failures / eligible cells`.
    private static string ClusteringReport(RuleAccumulator accumulator)
    {
        var rate = (double)accumulator.Failures / accumulator.Compared;
        var top = accumulator.Runs
            .OrderByDescending(run => run.Failures)
            .Take(3)
            .Select(run => $"{run.Label}:{run.Failures}/{run.Compared}");
        var failing = accumulator.Runs.Count(run => run.Failures > 0);
        var dispersion = "n/a";
        if (accumulator.Failures > 0)
        {
            var pearson = accumulator.Runs.Sum(run => Math.Pow(run.Failures - rate * run.Compared, 2));
            var binomial = accumulator.Runs.Sum(run => run.Compared * rate * (1.0 - rate));
            dispersion = (pearson / binomial).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }

        return $"D={dispersion} runsWithFail={failing}/{accumulator.Runs.Count} top={string.Join(",", top)}";
    }

    private static string ReplicaPath(string formulation, int ordinal) =>
        RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, ordinal + ".m.txt");
}
