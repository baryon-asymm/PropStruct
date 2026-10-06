namespace PropStruct.Tests.Harness;

/// <summary>
/// Cells of every printed quantity with no canonical-axis carve-out (everything <see cref="CanonicalAxisCells"/>
/// does not own): the mass-weighted families' own absolute ceiling and heavy-tail sibling bracket
/// (<see cref="MassFamilyRule"/>), the sparse-cell exclusion, and the ordinary continuous/count-like cell
/// otherwise. Split out of <c>StatisticalCriterion.Comparisons.cs</c> (decomposition, 2026-09-26); called from
/// <see cref="ReferenceComparison"/>.
/// </summary>
internal static class OrdinaryQuantityCells
{
    // Extracted from `BuildComparePending`'s own generic (non-canonical-axis) branch — a pure move, no
    // behaviour change. `referenceCells`/`candidate` are the whole per-formulation dictionaries (needed for the
    // mass-weighted sibling lookups); every other parameter is per-name context already computed by the caller.
    // Returns the number of cells excluded.
    internal static int AddOrdinaryQuantityCells(
        List<PendingCell> pending, string name, IReadOnlyDictionary<string, ResultCell[]> referenceCells,
        IReadOnlyDictionary<string, double[]> candidate, ResultCell[]? referenceArray, double[]? candidateArray,
        List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, bool zeroBelow,
        bool isDistributionFunction, RunQuantum.QuantumEstimate? candidateQuantum, RunQuantum.QuantumEstimate?[] replicaQuantums,
        int? decimalDigits)
    {
        var excluded = 0;
        var maxLength = Math.Max(referenceArray?.Length ?? 0, candidateArray?.Length ?? 0);
        foreach (var replica in replicaCells)
        {
            if (replica.TryGetValue(name, out var replicaArray) && replicaArray.Length > maxLength)
            {
                maxLength = replicaArray.Length;
            }
        }

        // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #1: 0.0 for every family but the four
        // mass-weighted ones, where it is the printed step (never 0 — `MassFamilyRule.MassDistributionFamilySteps`
        // holds no zero entries).
        var massStep = MassFamilyRule.MassDistributionFamilySteps.GetValueOrDefault(name);

        // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`: only the two mass-weighted families with a
        // validated same-axis sibling (`MassFamilyRule.MassFamilySameAxisSibling`) get the new count rule;
        // `fmdok`/`fmkarm_cor2` have none and stay on the ordinary continuous rule below — a declared, narrower
        // scope, not a silent gap (same entry, "No-sibling case: an open finding").
        //
        // The sibling's own count reconstruction (already validated elsewhere by this same criterion, at
        // the sibling's own entry of this very loop) gives n(i) directly; `massQuantum` is the candidate's own
        // feasible interval of the volume quantum `1 / T`, derived once per name from the *well-populated* cells
        // the sibling's own counts identify (`MassFamilyRule.FeasibleMassQuantum`, this node's BOOT.md, "Mass
        // bracket", E1 2026-09-27) — never from a blind search of the whole array, and never a single point
        // estimate any more (the withdrawn `EstimateMassQuantum` measurably failed 7 cells its own bracket was
        // supposed to never fail).
        string? massSibling = null;
        double[]? siblingCandidateArray = null;
        RunQuantum.QuantumEstimate? siblingCandidateQuantum = null;
        MassFamilyRule.MassQuantumInterval? massQuantum = null;
        int? siblingDecimalDigits = null;
        var siblingReplicaQuantums = new RunQuantum.QuantumEstimate?[replicaCells.Count];

        if (massStep > 0.0 && MassFamilyRule.MassFamilySameAxisSibling.TryGetValue(name, out massSibling))
        {
            _ = referenceCells.TryGetValue(massSibling, out var siblingReferenceArray);
            _ = candidate.TryGetValue(massSibling, out siblingCandidateArray);
            siblingDecimalDigits = PrintResolution.InferDecimalDigits(siblingReferenceArray);

            if (siblingCandidateArray is not null
                && RunQuantum.TryInfer(PrintResolution.BuildSyntheticCells(siblingCandidateArray, siblingDecimalDigits), out var siblingCandidateEstimate))
            {
                siblingCandidateQuantum = siblingCandidateEstimate;

                if (candidateArray is not null)
                {
                    massQuantum = MassFamilyRule.FeasibleMassQuantum(
                        candidateArray, siblingCandidateArray, siblingCandidateEstimate, decimalDigits, siblingDecimalDigits, massStep);
                }
            }

            for (var r = 0; r < replicaCells.Count; r++)
            {
                if (replicaCells[r].TryGetValue(massSibling, out var siblingOwnArray)
                    && RunQuantum.TryInfer(siblingOwnArray, out var siblingReplicaEstimate))
                {
                    siblingReplicaQuantums[r] = siblingReplicaEstimate;
                }
            }
        }

        // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the array's own total per replica and the family's
        // own dispersion estimate, computed once for the whole array — every ordinary distribution-
        // function family (no canonical-axis restriction of its own, unlike `fqdokkarm`'s row family
        // above) draws on the same replica set for every one of its cells.
        var arrayReplicaTotals = isDistributionFunction ? CountReconstruction.ArrayTotals(replicaCells, name) : null;
        var arrayDispersion = arrayReplicaTotals is not null
            ? DispersionEstimator.Estimate(replicaCells, name, arrayReplicaTotals)
            : null;
        var arrayCandidateTotal = isDistributionFunction && candidateQuantum is { } arrayCq && candidateArray is not null
            ? CountReconstruction.ReconstructTotal(PrintResolution.BuildSyntheticCells(candidateArray, decimalDigits), arrayCq)
            : (double?)null;

        for (var index = 0; index < maxLength; index++)
        {
            var referenceCell = referenceArray is not null && index < referenceArray.Length ? referenceArray[index] : default;
            var cand = candidateArray is not null && index < candidateArray.Length ? candidateArray[index] : 0.0;

            var replicaValues = new double[replicaCells.Count];
            for (var r = 0; r < replicaCells.Count; r++)
            {
                replicaValues[r] = replicaCells[r].TryGetValue(name, out var replicaArray2) && index < replicaArray2.Length
                    ? replicaArray2[index].Value
                    : 0.0;
            }

            // #1's absolute ceiling: a density integrates to one over its own axis, so no cell of a mass
            // family can exceed 1/step — checked unconditionally, on every cell of the family, ahead of
            // the sparse-replica exclusion below (a defect this large is a failure, not something the
            // sparse rule should quietly exclude).
            if (massStep > 0.0 && Math.Abs(cand) > 1.0 / massStep)
            {
                pending.Add(PendingCell.From(
                    name, index, referenceCell, cand, replicaValues, zeroBelow, isDistributionFunction: false,
                    forceFailure: true, forcedThreshold: 1.0 / massStep));
                continue;
            }

            // Heavy-tail count rule (`tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`): the sibling's own count
            // reconstruction gives n(i) directly, so a `mu < 30` cell (on the replica population) is a
            // deterministic physical-consistency check of the candidate against itself (its own n, its
            // own volume quantum, its own bin edges) — not a fresh hypothesis test against the replicas.
            // Scoped to the sibling-bearing families only (`massSibling is not null`); `fmdok`/
            // `fmkarm_cor2` fall straight through to the ordinary continuous rule below. The bracket itself
            // (MassFamilyRule.TryBracket) is a genuine extraction, not a pure move (this file's own
            // decomposition, 2026-09-26): the surrounding control flow (this `if`, the `continue` below)
            // stays here, and every arithmetic expression inside it is unchanged, character for character.
            if (massSibling is not null && siblingCandidateQuantum is { } scq && siblingCandidateArray is not null
                && index < siblingCandidateArray.Length && massQuantum is { } mq
                && MassFamilyRule.TryBracket(
                    replicaCells, massSibling, siblingReplicaQuantums, index, siblingCandidateArray, scq, massStep, mq,
                    cand, decimalDigits, siblingDecimalDigits) is { } countRuleVerdict)
            {
                pending.Add(PendingCell.From(
                    name, index, referenceCell, cand, replicaValues, zeroBelow, isDistributionFunction: false,
                    decimalDigits: decimalDigits,
                    countRule: countRuleVerdict));
                continue;
            }

            // #1's sparse-cell exclusion: with fewer than two non-zero replicas, the replicas carry no
            // scale to judge any candidate magnitude against — under exchangeability the candidate being
            // the only non-zero run among the R + 1 has probability at least 1/(R + 1), far above the
            // per-cell level alpha/m this node's BOOT.md's own criterion runs at, so no comparison here
            // can ever legitimately fail. Scoped to every non-count-like cell, not only the mass
            // families (the argument is general; the ceiling above is the part that is family-specific).
            if (!isDistributionFunction && replicaValues.Count(v => v != 0.0) < 2)
            {
                excluded++;
                continue;
            }

            List<double>? replicaCountsForCell = null;
            List<double>? replicaTotalsForCell = null;
            if (isDistributionFunction)
            {
                replicaCountsForCell = new List<double>(replicaCells.Count);
                replicaTotalsForCell = new List<double>(replicaCells.Count);
                for (var r = 0; r < replicaCells.Count; r++)
                {
                    if (replicaQuantums[r] is not { } estimate)
                    {
                        continue;
                    }

                    replicaCountsForCell.Add(Math.Round(Math.Abs(replicaValues[r]) / estimate.Quantum));
                    replicaTotalsForCell.Add(arrayReplicaTotals![r]!.Value);
                }
            }

            pending.Add(PendingCell.From(
                name, index, referenceCell, cand, replicaValues, zeroBelow, isDistributionFunction,
                candidateQuantum?.Quantum, candidateQuantum?.Resolution, candidateQuantum?.Identified ?? true,
                replicaCountsForCell,
                decimalDigits: decimalDigits, replicaTotals: replicaTotalsForCell, candidateTotal: arrayCandidateTotal,
                dispersion: arrayDispersion));
        }

        return excluded;
    }
}
