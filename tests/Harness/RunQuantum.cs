namespace PropStruct.Tests.Harness;

/// <summary>
/// One run's own count quantum and the near-multiple test against it
/// (`tests/Harness/HISTORY.md#per-run-quantum`). Split out of
/// <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class RunQuantum
{
    // `tests/Harness/HISTORY.md#per-run-quantum`: the inferred quantum of one run's own array,
    // and the print resolution of the token that defined it (needed to validate other runs'/the candidate's own
    // values against it "within the print resolution of the field", not a flat relative tolerance).
    //
    // internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (this node's API.md):
    // `tests/Harness/HISTORY.md#fable-5-1-decision-iv`'s own dispersion diagnostic reconstructs a run's own integer counts the same way `Compare` itself does
    // — one quantum-inference formula, reused, not a second implementation of it for a one-off script.
    //
    // `Resolution` is the step's own print resolution, `res(min) / k` (B2a, step 1,
    // `tests/Harness/HISTORY.md#b2a-quantum-identification-2026-10-02`): the step `q = min / k` inherits the
    // resolution of the token it was divided out of, shared over its `k` counts. `Identified` is B2a's step 2
    // (amended by `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`): whether the chance that the array's other
    // resolvable cells all fall within print resolution of integer multiples of `q` by accident is at most
    // `StatisticalCriterion.Alpha / K`, `K` the steps the search may try; the estimate is returned either way.
    internal readonly record struct QuantumEstimate(double Quantum, double Resolution, bool Identified);

    // `tests/Harness/HISTORY.md#per-run-quantum`, refined the same day: a run's own quantum is
    // inferred from that run's own printed array alone — its non-zero cells, validated as near-integer multiples
    // of one step within the print resolution of the field. A cell's own value is never its own witness (the
    // same principle as the original "candidate is never its own witness" fix, applied one level down: within
    // one run's own array, one cell of it does not validate itself, but the array's *other* non-zero cells do,
    // since they share this run's own normalization).
    //
    // The array's own smallest non-zero cell need not be exactly one count (a well-populated row can hold zero
    // 1-count cells at all): the largest quantum `q = min / k`, `k = 1, 2, ..., K`, for which every *resolvable*
    // non-zero cell of the array is within print resolution of an integer multiple of `q`, is taken instead —
    // trying `k` in increasing order (`q` decreasing) so the first `k` that fits gives the largest such `q`. A
    // cell is resolvable for a candidate `q` when `q` itself exceeds that cell's own print resolution (one count
    // is then distinguishable from zero counts at that cell's own precision); a cell whose reconstructed count
    // would be so large that `q` no longer clears its resolution carries no information about the step and is
    // left out of the fit, not judged against it. `K` is bounded by the same resolution argument, on the cell
    // that defines `min` itself: once `q = min / k` no longer exceeds `min`'s own resolution, `min`'s own count
    // under that `q` is no longer distinguishable from a neighbouring one either, and a further division could
    // only ever "fit" by coincidence.
    //
    // ⚠ 2026-09-19: this method first took the array's own smallest non-zero cell as `k = 1` unconditionally.
    // HMX's own reference `fqdokkarm(31,:)`
    // (`tests/Harness/HISTORY.md#implementation-and-measurements-2026-09-19-per-run-quantum`) refuted it:
    // its smallest non-zero cell, index 7 (`5.99e-05`), is not one count but eleven (`1.82 ≈ 20/11`, the true
    // step `q ≈ 5.445e-06` found at `k = 11`) — every cell of the row (down to index 69, the last non-zero one)
    // is a near-exact integer multiple of that step, confirmed by direct computation against the reference's own
    // printed text, once cells whose own reconstructed count would run past several thousand (unresolvable at
    // three printed significant digits) are left out of the fit.
    internal static bool TryInfer(IReadOnlyList<ResultCell> ownArray, out QuantumEstimate estimate)
    {
        // Too few non-zero cells to say anything about a shared step; a single one is trivially "consistent with
        // itself" and would let one stray cell define the whole array's quantum. Applies to the *resolvable*
        // cells a candidate `q` actually fits, not to the array's raw non-zero count.
        const int minimumResolvableValues = 2;

        var nonZero = new List<ResultCell>();
        foreach (var cell in ownArray)
        {
            if (Math.Abs(cell.Value) > 0.0)
            {
                nonZero.Add(cell);
            }
        }

        if (nonZero.Count < minimumResolvableValues)
        {
            estimate = default;
            return false;
        }

        var minIndex = 0;
        for (var i = 1; i < nonZero.Count; i++)
        {
            if (Math.Abs(nonZero[i].Value) < Math.Abs(nonZero[minIndex].Value))
            {
                minIndex = i;
            }
        }

        var minCell = nonZero[minIndex];
        var min = Math.Abs(minCell.Value);
        if (minCell.Resolution <= 0.0)
        {
            // No print-resolution evidence to bound K or to validate any candidate against (only the candidate's
            // own synthetic array, built for an index past the reference's own length, can reach this).
            estimate = default;
            return false;
        }

        for (var k = 1; ; k++)
        {
            var q = min / k;
            if (q <= minCell.Resolution)
            {
                // K's own bound (see above): a further division cannot be validated even against the cell that
                // defines it.
                estimate = default;
                return false;
            }

            // B2a, step 1: the step's own resolution is its token's, shared over the token's `k` counts.
            var stepResolution = minCell.Resolution / k;
            var fits = true;
            var resolvableCount = 0;
            foreach (var cell in nonZero)
            {
                var magnitude = Math.Abs(cell.Value);
                if (q <= cell.Resolution)
                {
                    // Unresolvable at this q: one count would not clear this cell's own print resolution, so it
                    // carries no information about the step and is left out of the fit.
                    continue;
                }

                resolvableCount++;
                if (!IsNearIntegerMultiple(magnitude, cell.Resolution, q, stepResolution))
                {
                    fits = false;
                    break;
                }
            }

            if (fits && resolvableCount >= minimumResolvableValues)
            {
                estimate = new QuantumEstimate(q, stepResolution, IsIdentified(nonZero, minIndex, q, stepResolution));
                return true;
            }
        }
    }

    // B2a, step 2 (`tests/Harness/HISTORY.md#b2a-quantum-identification-2026-10-02`, amended by
    // `tests/Harness/HISTORY.md#b2a-amended-2026-10-02`): a step that fits is not thereby a step the data
    // identify. Each of the array's other resolvable non-zero cells `i` falls inside the window of an integer
    // multiple of `q` by accident with probability `(res_i + n_i * rho) / q` (the same tolerance
    // `IsNearIntegerMultiple` accepts, on both sides, over the spacing `q`; at most one), `n_i` its reconstructed
    // count. The search tries up to `K` steps (`StepsTried`) and keeps the first that fits, so the step is
    // identified when all of those cells doing so has probability at most `StatisticalCriterion.Alpha / K`: an
    // array then identifies a wrong step with chance at most `Alpha`. The cell that defines `min` is the step's
    // own witness and is left out.
    private static bool IsIdentified(List<ResultCell> nonZero, int minIndex, double q, double stepResolution)
    {
        var chance = 1.0;
        for (var i = 0; i < nonZero.Count; i++)
        {
            var cell = nonZero[i];
            if (i == minIndex || q <= cell.Resolution)
            {
                continue;
            }

            var count = Math.Round(Math.Abs(cell.Value) / q);
            chance *= Math.Min(1.0, (cell.Resolution + count * stepResolution) / q);
        }

        var minCell = nonZero[minIndex];
        return chance <= StatisticalCriterion.Alpha / StepsTried(Math.Abs(minCell.Value), minCell.Resolution);
    }

    // The number of steps `TryInfer`'s search may try for an array whose smallest non-zero cell is `min`, of print
    // resolution `resolution`: every `k` with `min / k` above `resolution`, counted by the comparison that ends the
    // search. It depends on the `min` token alone, so it is fixed before the search starts.
    private static int StepsTried(double min, double resolution)
    {
        var steps = 0;
        while (min / (steps + 1) > resolution)
        {
            steps++;
        }

        return steps;
    }

    // Shared by the run's own quantum-consistency check above and by the candidate's own plausible-count check
    // in EvaluateCells (root BOOT.md Taboos: "no second implementation of any part of ... a formula"). A printed
    // token's own true value can differ from its printed text by up to half its own resolution; reconstructing
    // `rounded` units of `quantum` inherits half of the quantum's own token's resolution for every one of those
    // units — an absolute tolerance built from two print-resolution figures, not a flat relative percentage
    // (`tests/Harness/HISTORY.md#per-run-quantum`: "validated as near-integer multiples of one step within the
    // print resolution of the field").
    internal static bool IsNearIntegerMultiple(double magnitude, double resolution, double quantum, double quantumResolution)
    {
        var ratio = magnitude / quantum;
        var rounded = Math.Round(ratio);
        var tolerance = 0.5 * resolution + rounded * 0.5 * quantumResolution;
        return Math.Abs(magnitude - rounded * quantum) <= tolerance;
    }

    // this node's BOOT.md, "Mass bracket" (E1, 2026-09-27): the feasible count interval `[n⁻, n⁺]` a printed
    // value `magnitude` of resolution `resolution` is consistent with, under `quantum` — the exact inverse of
    // `IsNearIntegerMultiple`'s own tolerance test above, not a second formula (root BOOT.md Taboos): that test
    // accepts `rounded` when `|magnitude - rounded * quantum| <= 0.5 * resolution + rounded * 0.5 *
    // quantum.Resolution`; solving that same inequality for `rounded` from each side gives
    // `n⁻ = ceil((magnitude - resolution / 2) / (quantum.Quantum + quantum.Resolution / 2))` and
    // `n⁺ = floor((magnitude + resolution / 2) / (quantum.Quantum - quantum.Resolution / 2))`, each clamped so the
    // interval never excludes the point estimate `n̂ = round(magnitude / quantum.Quantum)` itself — a clamp guarding
    // `PrintResolution.ExponentOf`'s own power-of-ten quirk (F-c, fixed 2026-09-28,
    // `tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`), not part of the inequality's own
    // derivation. `magnitude = 0` is its own interval, `[0, 0]` (E-format never
    // prints a nonzero value as `0.000E+00`, so a printed `0` is exactly zero counts); when `quantum.Quantum` does
    // not itself clear `quantum.Resolution / 2`, the upper-bound denominator would be non-positive, so `n⁺` is
    // `+Infinity` instead of a division that would otherwise turn a real bound into a nonsensical negative one.
    // The `1e-12` nudges guard the same boundary the search in `TryInfer` already guards: a ratio landing exactly
    // on an integer must not be pushed to the wrong side of `ceil`/`floor` by floating-point noise.
    internal static (double Low, double High) CountBounds(double magnitude, double resolution, QuantumEstimate quantum)
    {
        var f = Math.Abs(magnitude);
        if (f == 0.0)
        {
            return (0.0, 0.0);
        }

        var q = quantum.Quantum;
        var rho = quantum.Resolution;
        var nHat = Math.Round(f / q);

        var low = Math.Max(0.0, Math.Min(nHat, Math.Ceiling((f - resolution / 2.0) / (q + rho / 2.0) - 1e-12)));
        var high = q <= rho / 2.0
            ? double.PositiveInfinity
            : Math.Max(nHat, Math.Floor((f + resolution / 2.0) / (q - rho / 2.0) + 1e-12));

        return (low, high);
    }
}
