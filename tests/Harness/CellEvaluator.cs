namespace PropStruct.Tests.Harness;

/// <summary>
/// Which of the three terms (<see cref="CalibrationRule.Student"/>, <see cref="CalibrationRule.Count"/>,
/// <see cref="CalibrationRule.Mass"/>) governs one cell's own threshold, and whether the cell failed — built on
/// <see cref="CountFloor"/>, <see cref="MassFamilyRule"/> and <see cref="CellEligibility"/>. Split out of
/// <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26): the eligibility test itself now
/// lives in <see cref="CellEligibility"/>, called from here rather than inlined.
/// </summary>
internal static class CellEvaluator
{
    // The threshold/verdict formula shared by every comparison this node exposes (`Compare`, `CompareTailRowMean`,
    // `CompareAdaptiveIndexMatched`) and, since `tests/Harness/HISTORY.md#fable-5-1-decision-ii`, by the
    // calibration curve as well: written once so the Student-band/count-floor/print-resolution-guard formula
    // exists exactly once in this node (root
    // BOOT.md Taboos: "no second implementation of any part of ... a formula"). `FamilyWiseReport.Of` reduces
    // this to a `CriterionReport`; the calibration curve reads `Rule`/`Degenerate` directly instead.
    internal static List<CellVerdict> EvaluateCells(List<PendingCell> pending, double perQuantityAlpha)
    {
        var quantileCache = new Dictionary<int, double>();
        double TQuantile(int replicasUsed) =>
            quantileCache.TryGetValue(replicasUsed, out var cached)
                ? cached
                : quantileCache[replicasUsed] = StudentDistribution.TwoSidedQuantile(replicasUsed - 1, perQuantityAlpha);

        var verdicts = new List<CellVerdict>(pending.Count);
        foreach (var cell in pending)
        {
            // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #1: a mass family's own absolute ceiling is
            // unconditional — no Student band, count floor or resolution guard can excuse it, so it is checked
            // before any of that machinery runs (`cell.ForcedThreshold` is reported as the failure's own
            // threshold, the bound itself, not a computed statistical one). It never depends on `alpha`, so it is
            // always `CalibrationRule.Mass`, always `Degenerate`.
            // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`: the ceiling `ForceFailure` checks
            // *is* the family's own theoretical maximum (a density integrating to one over its own axis cannot
            // exceed `1 / step` anywhere on it) — the admissible set and the attainable range are the same
            // interval by the same derivation, so nothing outside either could ever exist to fail against. Never
            // eligible, for every cell of this kind, at every level.
            if (cell.ForceFailure)
            {
                verdicts.Add(new CellVerdict(
                    cell.Name, cell.Index, cell.CandidateValue, cell.Mean, cell.ForcedThreshold ?? 0.0,
                    Failed: true, CalibrationRule.Mass, Degenerate: true, AttainedAlpha: null, Eligible: false, TailProbability: null,
                    StudentTerm: 0.0, CountFloor: 0.0, StaticFloor: 0.0,
                    RegionLow: null, RegionHigh: null, ResolutionGuardCounts: null));
                continue;
            }

            // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`: a
            // `mu < 30` mass-family sibling cell's own verdict is decided here, before the ordinary Student-band
            // machinery below ever runs — the same precedence `ForceFailure` already has, and for the same reason
            // (a rule this specific must not be second-guessed by the general-purpose band). It is a fixed
            // physical bracket with a print-rounding `eps`, never an `alpha`-scaled one, so it is always
            // `CalibrationRule.Mass`, always `Degenerate` (`tests/Harness/HISTORY.md#fable-5-1-decision-ii`: `alpha`
            // could not move this cell's outcome either way).
            if (cell.CountRule is { } verdict)
            {
                // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`: unlike the ceiling above, the physically-derived bracket
                // `[Low, High]` is a genuine sub-range of the family's own theoretical `[0, Ceiling]` in every
                // observed case (it is anchored to one bin's own diameter bounds, always narrower than the whole
                // axis unless the entire distribution sits in one bin) — eligible whenever it does not, in fact,
                // already span the whole range.
                var massEligible = verdict.Low > 0.0 || verdict.High < verdict.Ceiling;
                verdicts.Add(new CellVerdict(
                    cell.Name, cell.Index, cell.CandidateValue, cell.Mean, verdict.Boundary,
                    verdict.Fails, CalibrationRule.Mass, Degenerate: true, AttainedAlpha: null, Eligible: massEligible, TailProbability: null,
                    StudentTerm: 0.0, CountFloor: 0.0, StaticFloor: 0.0,
                    RegionLow: null, RegionHigh: null, ResolutionGuardCounts: null));
                continue;
            }

            var t = TQuantile(cell.ReplicaValues.Length);
            var factor = Math.Sqrt(1.0 + 1.0 / cell.ReplicaValues.Length);

            // `tests/Harness/HISTORY.md#per-run-quantum`: the quantum was already inferred per
            // run, at the point `Compare` built this cell (`CandidateQuantum`/`ReplicaCounts`), from each run's
            // own printed array. Finalize only applies the count rules to the counts it was handed; it never
            // infers a quantum itself.
            // The replicas' pooled count at this cell, read once: by the count-floor branch below and by the centring
            // further down (`CountFloor.HeavyTailRegimeThreshold` bounds both), and reported on the verdict.
            var countTotal = cell.ReplicaCounts is { Count: > 0 } totalCounts ? totalCounts.Sum() : (double?)null;
            var countFloor = 0.0;
            double? attainedAlpha = null;
            double? countLow = null;
            double? countHigh = null;
            double? countTailProbability = null;
            double? countPredictedMean = null;
            double? countRegionMean = null;
            var candidateIsImplausibleCount = false;
            double? resolutionGuardCounts = null;
            double? quantumForVerdict = null;
            double? observedCountForVerdict = null;
            if (cell.IsDistributionFunction && cell.CandidateQuantum is { } quantum && quantum > 0.0)
            {
                quantumForVerdict = quantum;
                // The candidate is judged against its own run's quantum, not merely floored by it: a value that
                // is not close to an integer multiple of it cannot be a count, whatever the Student band says
                // (`tests/Harness/HISTORY.md#per-run-quantum`: "a candidate cell that is not near an integer
                // multiple of its own run's quantum fails in its own right"). Its own resolution here must be
                // the same one `RunQuantum.TryInfer`'s search used to find `quantum` in the first place — the
                // candidate's own magnitude under the array's own format
                // (`tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #2), not `cell.Resolution` (the reference's own token at this index, kept below
                // only for the ordinary print-resolution floor, where no such consistency requirement applies).
                // Mixing the two here reintroduced exactly the magnitude-mismatch bug #2 fixed for the search
                // (measured: HPEPA3 replica 15's own `coef[479]`, candidate value `2.95E-07` against a reference
                // token of `0.735E-07` at that index — the reference's own resolution, `1E-10`, is ten times too
                // tight for the candidate's own order of magnitude, `1E-09`, and spuriously failed a cell that
                // is, in fact, a clean integer multiple of its own run's quantum). Computed unconditionally here
                // (not only inside the implausible-count guard below): decision XI's own region test needs it too.
                var candidateResolution = cell.DecimalDigits is { } digits
                    ? PrintResolution.ResolutionFromDecimalDigits(cell.CandidateValue, digits)
                    : cell.Resolution;

                // `tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`, item 2: the print-resolution
                // guard for the count region, in the same count units as `[Low, High]` — the candidate's own
                // printed resolution divided by its own run's quantum, the natural unit conversion. Its own
                // magnitude is reported per family by `tests/Harness.Tests/RegionGuardDiagnosticTests.cs`, read
                // straight off this same field, never recomputed a second way there.
                resolutionGuardCounts = candidateResolution / quantum;

                if (cell.ReplicaCounts is { Count: > 0 } counts)
                {
                    // decision VI, item 5: the candidate's own reconstructed count, the same "value / quantum"
                    // reconstruction this file already uses everywhere a printed cell's own integer count is
                    // needed (e.g. the implausible-count guard just below).
                    var observedCount = Math.Round(Math.Abs(cell.CandidateValue) / quantum);
                    observedCountForVerdict = observedCount;

                    // `tests/Harness/HISTORY.md#decision-xx-full-reasoning`: a cell "now governed by
                    // the beta-binomial" — `phi >= 1.0` — takes the unconditional count predictive's own full
                    // output instead; `BetaBinomialCountFloor` was no longer called for this population and is
                    // removed (root BOOT.md, "Language and build", decided 2026-09-25: IDE0051, unused since
                    // this decision). Decision V's own
                    // routing conditioned the count on the candidate's own reconstructed total `n*`; decisions
                    // XVII/XIX measured that conditioning itself refuted (`E[k|n] = n p` fails at the pooled
                    // level, `p = 0` in eight of nine measured groups, not sampling noise — decision XIX's own
                    // gate), so `n*` is no longer read for this population at all.
                    //
                    // The `phi < 1.0` population is untouched by this decision — decision XX's own text names only
                    // the cells "now governed by the beta-binomial" — so it keeps decision V's own routing bit for
                    // bit: `CountFloorFromCounts` still runs (it already did; this population never reached the
                    // beta-binomial), but its own `AttainedAlpha`/`Low`/`High`/`TailProbability` are still forced
                    // to `null`, because this family must still never be classified `Count` for the calibration
                    // curve's own bookkeeping (decision V's own reasoning, unchanged: "a binomial band there could
                    // never go red", AGENTS.md §13) — only `Floor` is kept. Losing this forced-null branch in an
                    // earlier draft of this change silently emptied population 2c and let roughly 76 000
                    // previously-unscored cells into the Count row uninvited; `Gate3_CalibrationCurve_
                    // WithinBinomialBandPerRulePerLevel` caught it (14 violations where 9 were expected), which is
                    // why this branch is restated explicitly rather than folded into the call below.
                    //
                    // B2a, step 3 (`tests/Harness/HISTORY.md#b2a-quantum-identification-2026-10-02`, amended by
                    // `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`): a candidate whose step the data do not
                    // identify (`QuantumEstimate.Identified`) lets the count floor govern only a cell whose
                    // replicas' pooled count is below `CountFloor.HeavyTailRegimeThreshold`, the boundary below which
                    // the replicas cannot average over a cell and the band does not hold; every other cell of that
                    // array is judged by the Student band, the count floor left at zero.
                    if (!cell.CandidateQuantumIdentified && countTotal >= CountFloor.HeavyTailRegimeThreshold)
                    {
                        (countFloor, attainedAlpha, countLow, countHigh, countTailProbability, countPredictedMean) =
                            (0.0, null, null, null, null, null);
                    }
                    else if (cell.Dispersion is { Phi: < 1.0 })
                    {
                        var (phiLessThanOneFloor, _, _, _, _, phiLessThanOneMean) = CountFloor.CountFloorFromCounts(counts, quantum, perQuantityAlpha, observedCount);
                        (countFloor, attainedAlpha, countLow, countHigh, countTailProbability, countPredictedMean) = (phiLessThanOneFloor, null, null, null, null, null);

                        // B2c (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`): the predictive mean the
                        // region test below reads, kept in a local of its own so that this population's
                        // `countPredictedMean` stays `null`, as decision XX left it (population 2c).
                        countRegionMean = phiLessThanOneMean;
                    }
                    else
                    {
                        (countFloor, attainedAlpha, countLow, countHigh, countTailProbability, countPredictedMean) =
                            CountFloor.CountFloorFromCounts(counts, quantum, perQuantityAlpha, observedCount);
                        countRegionMean = countPredictedMean;
                    }
                }

                candidateIsImplausibleCount = !RunQuantum.IsNearIntegerMultiple(
                    Math.Abs(cell.CandidateValue), candidateResolution, quantum, cell.CandidateQuantumResolution ?? 0.0);
            }

            // `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: `staticFloor` is the part of the floor that does not
            // depend on `alpha` at all (print resolution, the Poisson-like floor); `countFloor` does depend on
            // `alpha`, through `CountFloorFromCounts`'s own negative-binomial interval width, so it is grouped
            // with the Student term as "the alpha-dependent contribution" for the degeneracy check below, not
            // with the static floor.
            var staticFloor = Math.Max(cell.Resolution, cell.PoissonFloor);
            var studentTerm = t * cell.Sd * factor;
            var floor = Math.Max(staticFloor, countFloor);
            var threshold = Math.Max(studentTerm, floor);

            // `tests/Harness/HISTORY.md#decision-ix-full-reasoning`: the governing-term comparison,
            // computed once and reused both by the pass/fail test just below (decision XI) and by `Rule` further
            // down — never a second implementation of "which term governs" (root BOOT.md Taboos).
            var isCountGoverned = cell.IsDistributionFunction && countFloor > studentTerm;

            // `tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`: the region
            // `[low, high]` — the exact boundary `attainedAlpha`'s own level excludes — is still computed and
            // carried on the verdict/failure (informational: a reader of a Count-governed failure can see where
            // the model's own predictive interval sat), but is NOT the pass/fail test. Since B2c
            // (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`) the pass/fail test of a count-governed
            // cell is its reconstructed count against `mu + s`, in count space, with the same half-width, so that
            // every count of the region passes: decision XI's withdrawal concerned judging against the region's
            // own ends, which B2c does not do. Decision XI's own attempt
            // to judge a Count-governed cell against this region in count space, instead of the symmetric
            // value-space band below, was measured and withdrawn by its own stated criterion: Check A moved from
            // `0.5399` to `6.461` (away from `1.0`, not toward it) and gate 3 worsened from 9 to 15 violations,
            // with every Count row overshooting its own band by a wide margin. The region is anchored to the
            // model's own predicted mean (`n* * p_hat`, `n*` the candidate's own reconstructed total), which
            // decision IX's own "array total is never tested" gap and decision X's own measured dispersion of
            // run-to-run totals (median 89, max 5512) together explain: nothing tests whether a run's own total
            // is where the model assumes it is, so the region can sit far from where real runs cluster even when
            // the shape (`p_hat`) is right. Full measurement: `tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`.
            double? regionLowValue = null;
            double? regionHighValue = null;
            if (isCountGoverned && countLow is { } lowCount && countHigh is { } highCount && cell.CandidateQuantum is { } regionQuantum)
            {
                regionLowValue = lowCount * regionQuantum;
                regionHighValue = highCount * regionQuantum;
            }

            // `tests/Harness/HISTORY.md#count-floor-boundary-centring-defect` (defect found and fixed 2026-09-24): `countFloor`'s own half-width,
            // above, is measured from `predictedMean` (`CountFloorFromCounts`'s own comment: "re-centering the
            // returned floor on the sample mean would let the two means silently cancel a real shift"), so a
            // width measured from `predictedMean` only reconstructs the interval `[low, high]` the negative
            // binomial actually guarantees (`P(X <= high) >= 1 - alpha/2`) when the comparison is *also* centred
            // there — comparing it against `cell.Mean` (the replicas' own sample mean in raw value units)
            // reconstructs a different, mis-centred band and can exclude `high` itself. Measured on the 160-run
            // null of `tests/Harness/HISTORY.md#criterion-false-failure-rate-port-original`: five of the
            // seventeen failing cells (`fqkarm[75,77]` HPEPA3, `fqkarm[48,50]` inpt, `fqkarm[61]` P33) hold a
            // candidate whose own reconstructed count is exactly `countHigh`, failing by the gap this
            // mis-centring predicts (`predictedMean * quantum`, to within the candidate token's own print
            // rounding).
            //
            // Re-centering unconditionally regresses far more than it fixes: `predictedMean` pools every
            // *contributing* replica's own reconstructed count, each rounded under that replica's own,
            // independently inferred quantum ("Per-run quantum"), then converts the pooled total back to value
            // units through the *candidate's* quantum — an implicit assumption that every contributing run's own
            // quantum is close to the candidate's, which measurably fails once a cell is populated enough for
            // replica-to-replica quantum drift to matter (re-run on the same null: unconditional re-centering
            // moved 11 failing runs of 160 to 48, most of them newly-failing well-populated `fqdokkarm` row
            // cells with no boundary artefact at all). So the fix is scoped to the regime the drift cannot reach:
            // `total < HeavyTailRegimeThreshold` (the same 30-count boundary "Heavy-tail count rule" already uses
            // to mark a cell too sparse for its own replica population to average over, reused rather than a
            // second sparsity threshold) restricts re-centering to cells whose *pooled* count is itself below
            // the regime where cross-replica quantum drift has been observed to bite; every one of the five
            // boundary cells above has a pooled total of 0 or 1. `countPredictedMeanValue` is `null` whenever the
            // Student rule governs, the pooled total is not small, the `Dispersion.Phi < 1` routing forces
            // `countPredictedMean` to `null` (Decision XX: that population must never be scored as `Count`), or
            // the cell is not distribution-like at all — every one of those cases keeps comparing against
            // `cell.Mean`, unchanged. The four other Count-governed failing cells of the same seventeen
            // (`fqkarm_cor[15]` P33 twice, `fqkarm[62,63]` P33, `fqdokkarm(31,:)[7]` HMX) all have a pooled total
            // under this same bound too, and each exceeds `countHigh` by a full quantum or more even after
            // re-centering by the (sub-quantum) `predictedMean` shift, so they are correctly unaffected by this
            // fix and keep failing.
            var countPredictedMeanValue = isCountGoverned && countPredictedMean is { } predictedMeanCount
                && cell.CandidateQuantum is { } predictedMeanQuantum && countTotal is { } totalForGate && totalForGate < CountFloor.HeavyTailRegimeThreshold
                ? predictedMeanCount * predictedMeanQuantum
                : (double?)null;
            var comparisonCentre = countPredictedMeanValue ?? cell.Mean;

            // A candidate/reference token and a replica's own token are each independently rounded to the
            // printed resolution; a true difference of exactly one unit of that resolution can then appear as
            // slightly more than one unit once both are parsed back to `double` (this node's BOOT.md,
            // ## Invariants, "Print-resolution floating-point guard"). The guard is a fixed, tiny multiple of
            // the threshold itself (or an absolute floor when the threshold is 0), never a fraction of `sd`,
            // `t`, `r_q` or `p_q`, so it cannot mask a real statistical difference.
            var epsilon = Math.Max(threshold, Math.Abs(comparisonCentre)) * 1e-9 + 1e-300;
            // B2c (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`): a count-governed cell is judged on its
            // reconstructed count against the predictive's own mean, not on its printed value against
            // `comparisonCentre ± threshold`, which ends on the region's `high` only when the centre is `mu` and
            // the token rounds down. Every other cell is judged as before.
            var failed = (isCountGoverned && countRegionMean is { } predictiveMean && quantumForVerdict is { } countQuantum
                    && observedCountForVerdict is { } reconstructedCount
                    ? CountLiesOutsideTheShiftedRegion(reconstructedCount, predictiveMean, comparisonCentre / countQuantum, threshold / countQuantum)
                    : Math.Abs(cell.CandidateValue - comparisonCentre) > threshold + epsilon)
                || candidateIsImplausibleCount;

            // `tests/Harness/HISTORY.md#fable-5-1-decision-ii`: a cell is degenerate for the calibration curve — not
            // for the ordinary criterion, which still compares it — when neither alpha-dependent term (the
            // Student band, nor the count floor) ever exceeds the alpha-independent floor: changing `alpha` could
            // not have moved this cell's own outcome, so its pass/fail carries no evidence about whether `alpha`
            // is calibrated. A cell whose replicas are all equal (`Sd == 0`) is degenerate for the same reason:
            // under exchangeability, only the alpha-independent floor (or the mean itself, if the floor is 0) can
            // ever decide it.
            var alphaDependentTerm = Math.Max(studentTerm, countFloor);
            var degenerate = cell.Sd == 0.0 || alphaDependentTerm <= staticFloor;

            // `tests/Harness/HISTORY.md#decision-ix-full-reasoning`: classify by which term actually
            // governs `threshold = Math.Max(studentTerm, Math.Max(staticFloor, countFloor))`, not by whether the
            // count floor happened to be computable (`attainedAlpha is not null`, decision III's own rule, found
            // to credit a cell with a level it cannot achieve whenever the Student band was the one that actually
            // set the threshold). For every non-degenerate cell `Math.Max(studentTerm, countFloor) > staticFloor`
            // by construction (`degenerate` above is exactly the negation), so comparing `countFloor` against
            // `studentTerm` alone — never the combined `floor` — correctly names the governing term without the
            // static floor ever intruding on a cell this comparison actually runs for.
            //
            // A cell where the count floor governs (`countFloor > studentTerm`) but produced no `attainedAlpha`
            // (only `CountFloorFromCounts`'s own `MaxCountFloorTotal` cap, since "Decision XX (2026-09-20, the
            // orchestrator)" — every count-like cell now takes this same predictive, so there is no second route
            // into this state any more) is still, mechanically, a `Count` cell — its threshold IS the count
            // floor — but has no known attained level to credit it with. `EvaluateCells` reports it as such
            // (`Rule = Count`, `AttainedAlpha = null`) rather than inventing a fourth rule: the callers that build
            // the calibration curve (`CalibrationCurveTests`) and Check A/B (`DilutionDiagnosticTests`) are the
            // ones that must recognize `Rule == Count && AttainedAlpha is null` as its own population, entering
            // neither the Student nor the Count row (decision IX, item 2's own third bullet) — the same place
            // `Eligible` is already filtered, not folded into this enum.
            var rule = isCountGoverned ? CalibrationRule.Count : CalibrationRule.Student;

            var eligible = CellEligibility.Of(rule, cell, threshold, countLow, countHigh);

            verdicts.Add(new CellVerdict(
                cell.Name, cell.Index, cell.CandidateValue, cell.Mean, threshold, failed, rule, degenerate, attainedAlpha, eligible,
                rule == CalibrationRule.Count ? countTailProbability : null,
                StudentTerm: studentTerm, CountFloor: countFloor, StaticFloor: staticFloor,
                RegionLow: regionLowValue, RegionHigh: regionHighValue, ResolutionGuardCounts: resolutionGuardCounts,
                Quantum: quantumForVerdict, ObservedCount: observedCountForVerdict, ImplausibleCount: candidateIsImplausibleCount,
                PooledCount: countTotal));
        }

        return verdicts;
    }

    /// <summary>
    /// B2c (<c>tests/Harness/HISTORY.md#count-region-edge-2026-10-02</c>): whether a count-governed cell's
    /// reconstructed <paramref name="count"/> lies more than <paramref name="halfWidthInCounts"/> from
    /// <c>mu + s</c>, <c>mu</c> being the predictive's mean in counts and <c>s</c> the whole number of counts nearest
    /// the band centre's offset from <c>mu</c> (a tie toward zero). Every count of the region <c>[low, high]</c>,
    /// moved by <c>s</c>, passes. The slack is the print-resolution floating-point guard of the value-space
    /// comparison, in counts.
    /// </summary>
    private static bool CountLiesOutsideTheShiftedRegion(
        double count, double predictiveMean, double centreInCounts, double halfWidthInCounts)
    {
        var offset = centreInCounts - predictiveMean;
        var shift = Math.Sign(offset) * Math.Ceiling(Math.Abs(offset) - 0.5);
        var shiftedMean = predictiveMean + shift;
        var epsilon = Math.Max(halfWidthInCounts, Math.Abs(shiftedMean)) * 1e-9 + 1e-300;
        return Math.Abs(count - shiftedMean) > halfWidthInCounts + epsilon;
    }
}
