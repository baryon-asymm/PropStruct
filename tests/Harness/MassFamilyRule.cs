namespace PropStruct.Tests.Harness;

/// <summary>
/// The four mass-weighted families' own printed step, ceiling, same-axis sibling and deterministic bracket
/// (`tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`, and this node's BOOT.md, "Mass bracket" (E1,
/// 2026-09-27)). Split out of <c>StatisticalCriterion.Comparisons.cs</c> (decomposition,
/// 2026-09-26); the surrounding control flow of <see cref="TryBracket"/> (the sparse-replica bookkeeping its own
/// inputs are drawn from, and the <c>continue</c> its own result decides, in <c>OrdinaryQuantityCells</c>) is
/// still the decomposition's own extraction, but the method's own arithmetic is no longer "unchanged, character
/// for character": E1 (2026-09-27) replaced the point-estimate quantum `EstimateMassQuantum` with the feasible
/// interval below, because the point estimate measurably failed 5 of the original's 197 runs and 2 of the port's
/// 160 — `HISTORY.md#e1-mass-bracket-feasible-interval` has the derivation and the measured figures.
/// </summary>
internal static class MassFamilyRule
{
    // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #1: the mass-weighted families' own printed step
    // (their own header, "(step = 10mkm)"), in the same unit their own cells are normalized against — verified
    // directly, for each: sum(cells) * step is 1 within print rounding on every reference file (a density
    // integrates to one over its own axis). Used both for the sparse-cell exclusion and the absolute ceiling
    // below.
    internal static readonly IReadOnlyDictionary<string, double> MassDistributionFamilySteps = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["fmdok"] = 10.0,
        ["fmkarm"] = 10.0,
        ["fmkarm_cor"] = 10.0,
        ["fmkarm_cor2"] = 10.0,
    };

    // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`: the two
    // mass-weighted families that have a same-axis *number*-density sibling printed (same step, same array
    // length, the same comment block one line apart in the reference file) — checked directly against every
    // reference file's own comment headers, never the Fortran source. `fmdok` and `fmkarm_cor2` have none (no
    // "Numeric density ... of dok" quantity is ever printed; no "cor. var#2" numeric counterpart exists).
    internal static readonly Dictionary<string, string> MassFamilySameAxisSibling = new(StringComparer.Ordinal)
    {
        ["fmkarm"] = "fqkarm",
        ["fmkarm_cor"] = "fqkarm_cor",
    };

    // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`: root BOOT.md's own literal, "3.14159 (sphere volume)",
    // used here as the model already uses it elsewhere (volume from diameter, `c = pi / 6`) — not a formula this
    // node invents, a constant root BOOT.md's own Constraints already names as part of the model.
    internal const double SphereVolumeCoefficient = 3.14159 / 6.0;

    // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`, "The count rule itself": the one verdict `Finalize` needs
    // for a `mu < 30` cell, evaluated by its own caller (`ReferenceComparison` for the mass-family sibling check),
    // the mass-family sibling check's own verdict, evaluated before `Finalize` runs (it does not depend on
    // `alpha`/`m`) — a fixed bound against the candidate's own already-reconstructed count, not an alpha-dependent
    // hypothesis test. `Low`/`High`/`Ceiling` (`tests/Harness/HISTORY.md#decision-vi-full-reasoning`)
    // are, respectively, the bracket's own two bounds (`Boundary` alone only ever names the one that mattered for
    // `Fails`) and the mass family's own theoretical maximum, `1 / massStep` — the same quantity `ForceFailure`'s
    // own absolute ceiling checks — needed together for the Mass-rule eligibility test.
    internal readonly record struct CountRuleVerdict(bool Fails, double Boundary, double Low, double High, double Ceiling);

    // this node's BOOT.md, "Mass bracket" (E1, 2026-09-27): the feasible interval of the volume quantum `1 / T`
    // itself — `Low`/`High` bound `q`, not a cell's own printed value — derived from print half-units and the
    // histogram's own bin edges rather than read off a single point estimate (the withdrawn `EstimateMassQuantum`
    // below). `CalibrationCells` is `|W|`, the number of well-populated sibling cells the bounds were derived
    // from (reported for a caller/test to see how much evidence backed the interval; never fed back into the
    // arithmetic itself).
    internal readonly record struct MassQuantumInterval(double Low, double High, int CalibrationCells);

    // this node's BOOT.md, "Mass bracket" (E1, 2026-09-27): replaces the point estimate `EstimateMassQuantum`
    // (the *median* of `fm(i) / (c * d_mid(i)^3 * n(i))` over well-populated cells), which
    // `HISTORY.md#e1-mass-bracket-feasible-interval` measured wrong by 0.11-3.38 print units on the 7 cells this
    // bracket is supposed to never fail: the median treats a bin's own diameter as its midpoint, ignoring the
    // several-per-cent a-priori error that midpoint carries (a bin's own mean `d^3` lies only within
    // `q * [(k/(k+1/2))^3, ((k+1)/(k+1/2))^3]` of it), an error the half-print-unit tolerance never accounted for.
    //
    // Each well-populated cell `k` in `W = {k : v_k &gt; 0, n̂_k &gt;= HeavyTailRegimeThreshold}` bounds `q` from
    // both sides at once, using `RunQuantum.CountBounds`'s own feasible count interval `[n⁻, n⁺]` for that sibling
    // cell together with the bin's own diameter *bounds* (`step * k`, `step * (k + 1)`) instead of its midpoint: a
    // genuine `q` must satisfy `v_k ≈ n_k * c * d_k^3 * q` for some `n_k` in `[n⁻, n⁺]` and some `d_k` in the
    // bin's own edges, so the widest feasible `q` consistent with that one cell alone is
    // `[(v_k - r(v_k) / 2) / (n⁺_k * c * d_hi_k^3), (v_k + r(v_k) / 2) / (n⁻_k * c * d_lo_k^3)]` — intersected
    // over every cell of `W` by taking the max of the lower bounds and the min of the upper. `d_lo_k = step * k`
    // is zero for `k = 0`, so the upper-bound side skips that cell; the lower-bound side, dividing by
    // `d_hi_k = step * (k + 1)`, never needs to. If the true model holds (one `q` for the whole array, printing
    // rounds to nearest) this interval contains it by construction — a containment guarantee, not a fitted band
    // (root BOOT.md Taboos: no expected value read off the failing cells themselves). Returns
    // <see langword="null"/> when `W` is empty (today's fall-through, unchanged).
    internal static MassQuantumInterval? FeasibleMassQuantum(
        double[] massValues, double[] siblingValues, RunQuantum.QuantumEstimate siblingQuantum,
        int? massDecimalDigits, int? siblingDecimalDigits, double step)
    {
        var qLow = double.NegativeInfinity;
        var qHigh = double.PositiveInfinity;
        var calibrationCells = 0;

        var limit = Math.Min(massValues.Length, siblingValues.Length);
        for (var k = 0; k < limit; k++)
        {
            var v = massValues[k];
            if (v <= 0.0)
            {
                continue;
            }

            var siblingResolution = siblingDecimalDigits is { } siblingDigits
                ? PrintResolution.ResolutionFromDecimalDigits(siblingValues[k], siblingDigits)
                : 0.0;
            var (countLow, countHigh) = RunQuantum.CountBounds(siblingValues[k], siblingResolution, siblingQuantum);
            var countPoint = Math.Round(Math.Abs(siblingValues[k]) / siblingQuantum.Quantum);
            if (countPoint < CountFloor.HeavyTailRegimeThreshold)
            {
                continue;
            }

            calibrationCells++;

            var massResolution = massDecimalDigits is { } massDigits ? PrintResolution.ResolutionFromDecimalDigits(v, massDigits) : 0.0;
            var dHi = step * (k + 1);
            qLow = Math.Max(qLow, (v - massResolution / 2.0) / (countHigh * SphereVolumeCoefficient * dHi * dHi * dHi));

            if (k >= 1)
            {
                var dLo = step * k;
                qHigh = Math.Min(qHigh, (v + massResolution / 2.0) / (countLow * SphereVolumeCoefficient * dLo * dLo * dLo));
            }
        }

        return calibrationCells == 0 ? null : new MassQuantumInterval(qLow, qHigh, calibrationCells);
    }

    // `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`, "Regime switch": the sibling's own count reconstruction
    // gives n(i) directly, so a `mu < 30` cell (on the replica population) is a deterministic physical-
    // consistency check of the candidate against itself (its own feasible count interval, its own feasible
    // volume-quantum interval, its own bin edges) — not a fresh hypothesis test against the replicas. Returns
    // <see langword="null"/> when the replica population is empty or well-populated
    // (`mu >= HeavyTailRegimeThreshold`, `tests/Harness/HISTORY.md#heavy-tail-count-rule-2026-09-19`, "Regime
    // switch"): the caller falls through to the ordinary continuous rule in either case.
    internal static CountRuleVerdict? TryBracket(
        List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string massSibling,
        RunQuantum.QuantumEstimate?[] siblingReplicaQuantums, int index, double[] siblingCandidateArray,
        RunQuantum.QuantumEstimate siblingCandidateQuantum, double massStep, MassQuantumInterval massQuantum,
        double candidateValue, int? decimalDigits, int? siblingDecimalDigits)
    {
        var nReplicas = new List<double>(replicaCells.Count);
        for (var r = 0; r < replicaCells.Count; r++)
        {
            if (siblingReplicaQuantums[r] is { } sq
                && replicaCells[r].TryGetValue(massSibling, out var siblingReplicaArray)
                && index < siblingReplicaArray.Length)
            {
                nReplicas.Add(Math.Round(Math.Abs(siblingReplicaArray[index].Value) / sq.Quantum));
            }
        }

        if (nReplicas.Count == 0)
        {
            return null;
        }

        var mu = nReplicas.Sum() / nReplicas.Count;
        if (mu >= CountFloor.HeavyTailRegimeThreshold)
        {
            return null;
        }

        // this node's BOOT.md, "Mass bracket" (E1, 2026-09-27), step 1: the candidate's own feasible count
        // interval at this cell, from the same `CountBounds` the calibration set above uses — never a second
        // point estimate (root BOOT.md Taboos).
        var siblingResolution = siblingDecimalDigits is { } digits
            ? PrintResolution.ResolutionFromDecimalDigits(siblingCandidateArray[index], digits)
            : 0.0;
        var (countLow, countHigh) = RunQuantum.CountBounds(siblingCandidateArray[index], siblingResolution, siblingCandidateQuantum);

        var dLo = massStep * index;
        var dHi = massStep * (index + 1);
        var lowValue = countLow * SphereVolumeCoefficient * dLo * dLo * dLo * massQuantum.Low;

        // `countHigh` can be `+Infinity` (`CountBounds`'s own guard) and `massQuantum.High` can be `+Infinity`
        // too (no `k >= 1` calibration cell) — their product is `+Infinity`, which is the correct "unbounded
        // above" reading, except when `countHigh` is exactly `0` (a candidate whose own sibling value rounds to
        // zero counts): `0 * Infinity` is `NaN`, not `0`, so that case is named explicitly.
        var highValue = countHigh == 0.0 ? 0.0 : countHigh * SphereVolumeCoefficient * dHi * dHi * dHi * massQuantum.High;

        // this node's BOOT.md, "Mass bracket" (E1, 2026-09-27): `r(x) = PrintResolution.ResolutionFromDecimalDigits(x,
        // digits)` for `x != 0`, and `r(0) = 0` — the E-format never prints a nonzero value as `0.000E+00`, so a
        // printed `0` carries no half-print-unit slack the way `ResolutionFromDecimalDigits(0, digits)` (built
        // for the ordinary Student band, which never sees a candidate this far from its own replicas) would
        // otherwise hand it. Without this, `eps` at a printed `0` swamps `lowValue` and the ceiling this bracket
        // exists to enforce never fires for a candidate reporting zero mass under a nonzero count.
        var resolution = decimalDigits is { } valueDigits && candidateValue != 0.0
            ? PrintResolution.ResolutionFromDecimalDigits(candidateValue, valueDigits)
            : 0.0;
        var eps = resolution / 2.0;

        // step 4: an empty `q` interval (the model's own containment guarantee refuted for this run — this
        // node's BOOT.md, "Mass bracket", "Empty interval") fails the cell outright, in addition to the ordinary
        // containment test.
        var fails = massQuantum.Low > massQuantum.High || candidateValue < lowValue - eps || candidateValue > highValue + eps;
        var boundary = candidateValue < lowValue - eps ? lowValue : highValue;
        return new CountRuleVerdict(fails, boundary, lowValue, highValue, 1.0 / massStep);
    }
}
