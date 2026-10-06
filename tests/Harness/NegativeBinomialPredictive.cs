namespace PropStruct.Tests.Harness;

/// <summary>
/// this node's BOOT.md, ## Invariants, "Count-like cells", and `tests/Harness/HISTORY.md#per-run-quantum`: the count
/// predictive <see cref="CountFloor"/>'s own floor is built from — negative binomial with <c>r = totalCount +
/// 1/2</c>, <c>p = replicaCount / (replicaCount + 1)</c> (the Gamma-Poisson recipe of
/// <c>HISTORY.md#criterion-revision-2026-09-17</c>, step 3). Split out of <c>StatisticalCriterion.Numerics.cs</c>
/// (decomposition, 2026-09-26): one of the generic numerics primitives, not tied to a "cell" or "replica" as a
/// domain concept.
/// </summary>
internal static class NegativeBinomialPredictive
{
    // Negative binomial predictive interval for a new draw given R replicas summing to `totalCount`: mean and
    // the smallest [low, high] with P(X < low) <= alpha/2 <= P(X <= low) and P(X <= high) >= 1 - alpha/2, over the
    // pmf p(k) = C(k+r-1, k) * (1-p)^r * p^k, r = totalCount + 1/2, p = replicaCount / (replicaCount + 1) — the
    // exact recipe of `tests/Harness/HISTORY.md#criterion-revision-2026-09-17`, step 3. `AttainedAlpha`
    // (`tests/Harness/HISTORY.md#fable-5-1-decision-iii`, "The Count rule's curve row") is the exact two-sided tail mass this same [low, high]
    // excludes, `P(X < low) + P(X > high)`, captured from the same cdf walk that finds low/high rather than
    // recomputed afterwards — one recursion, one function, no second implementation of the pmf update.
    //
    // ⚠ 2026-09-20: found live, by the attained-level sanity check this same decision added (`Gate3` reported
    // 94 226 cells with `AttainedAlpha` reading exactly `1`, exceeding nominal at every level): the walk used to
    // start at `pmf(0) = q^r`, computed as `Math.Exp(r * Math.Log(1.0 - p))`. Once `r` (essentially `totalCount`)
    // passes a few hundred — routine for a distribution-function cell nowhere near `MaxCountFloorTotal`'s own cap
    // of 2000 — `r * Math.Log(q)` falls past a double's exponent range and `pmf(0)` underflows to exactly `0.0`.
    // Every subsequent term is `0.0` times a finite ratio, so `cdf` never moves, `high` is never found, the loop
    // exhausts its own 1e6 cap, and the cell silently gets `[low, high] = [0, 0]` with `AttainedAlpha = 1` — a
    // valid-looking but completely wrong interval, invisible before this session because nothing previously
    // checked the walk's own success. Fixed by anchoring the walk at the distribution's own mode instead of at
    // `k = 0`: the mode's pmf is computed once, directly, in log space (`StudentDistribution.LogGamma`, the same
    // technique and the same function `BinomialBand` already uses for exactly this reason — root BOOT.md Taboos,
    // "no second implementation of ... a formula", so this reuses the log-gamma primitive, not a rewritten one),
    // which never underflows for any `(r, p)` this node's own `MaxCountFloorTotal` cap admits (the mode's own log-
    // pmf is bounded by the Gaussian approximation's own normalizing term, `-0.5 * log(2*pi*variance)`, nowhere
    // near a double's exponent range for a variance this cap allows). Every further term, in both directions, is
    // still reached by the same consecutive-term ratio the walk always used — this changes only where the walk
    // starts, not the recursion or the low/high/AttainedAlpha semantics built on it.
    //
    // ⚠ 2026-09-20, the same day, found while proving the fix above: the walk's own pmf, as originally written,
    // used `p` (not `q`) as the per-step continuation probability, and `q^r` (not `p^r`) as its k = 0 term — the
    // two probabilities were swapped throughout, not merely started from an unsafe point. Proven independently
    // (not from this code, from the Poisson-Gamma derivation the comment above already cites): posterior
    // `lambda ~ Gamma(r, rate = R)` after `R` replicas summing to `totalCount` with a Jeffreys prior, marginalized
    // against a new replica's own `Poisson(lambda)` draw, gives `P(X = k) = C(k+r-1,k) * p^r * q^k` with
    // `p = R/(R+1)` — the mean of *that* pmf is `r*q/p`, exactly the formula this function already returned, but
    // the pmf whose ratio and k = 0 term the walk actually computed (`q^r`, ratio `*p/k`) has mean `r*p/q`
    // instead, verified by direct summation (this node's own session scratch): for `totalCount = 0`,
    // `replicaCount = 15`, the walk's own numeric mean came to `7.5`, not the `0.0333` it reported and the count
    // rule's callers relied on. Because `p` is close to `1` for most formulations' own replica counts, the
    // swapped pmf decays far slower than the correct one — every count-like cell's own `[low, high]` was too wide
    // by construction, not merely at the underflow boundary (measured on the same example: the swapped walk's own
    // `high` was `39`; the corrected walk's own `high` is `1`). This is very likely the true mechanism behind the
    // count rule's own structural conservatism `NegativeBinomialInterval`'s design record already named as an
    // open question (this node's own "Fable 5.1 decision III" section) — a mis-derived pmf, not merely a discrete
    // boundary's own unavoidable slack. The fix swaps `p` and `q` everywhere the walk uses them (the k = 0 term,
    // the mode's own log-pmf, and both ratios); the returned `mean` formula itself was never wrong and is
    // unchanged; the mode-anchoring fix above is kept, now anchored at the *correct* distribution's own mode
    // (verified, this node's own session scratch: sane, finite `[low, high]` even at `replicaCount = 1`,
    // `totalCount` at the node's own `MaxCountFloorTotal` cap, where `p^r` alone would still underflow).
    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`: the mode of `P(X = k) = C(k+r-1,k) *
    // p^r * q^k`, computed once, directly, in log space (never by walking from `k = 0`,
    // `tests/Harness/HISTORY.md#fable-5-1-decision-iii`, "A
    // formula defect, not only a numerical one"), and the pmf(mode) it anchors. Extracted from
    // `NegativeBinomialInterval`'s own body so it and the point-CDF walk below (item 5's own tail-probability
    // diagnostic) share one construction, not two (root BOOT.md Taboos: no second implementation of a formula) —
    // the exact defect class ("a formula defect, not only a numerical one") this file has already paid for twice
    // this same day in this same shape.
    private static (double Mode, double ModePmf) NegativeBinomialMode(double r, double p, double q)
    {
        var mode = r > 1.0 ? Math.Max(0.0, Math.Floor((r - 1.0) * q / p)) : 0.0;
        var logModePmf = StudentDistribution.LogGamma(r + mode) - StudentDistribution.LogGamma(r)
            - StudentDistribution.LogGamma(mode + 1.0) + r * Math.Log(p) + mode * Math.Log(q);
        return (mode, Math.Exp(logModePmf));
    }

    // pmf(k - 1) / pmf(k), `k` the term already reached (not yet decremented) — `Interval`'s own downward walk
    // and the point-CDF walk below both call this, never reimplementing the ratio.
    private static double NegativeBinomialDownRatio(double k, double r, double q) => k / ((r + k - 1.0) * q);

    // pmf(k) / pmf(k - 1), `k` the term just reached (already incremented) — the upward counterpart of the ratio
    // above, shared the same way.
    private static double NegativeBinomialUpRatio(double k, double r, double q) => (r + k - 1.0) * q / k;

    // internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (this node's API.md):
    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`'s own cross-check needs this function's own `[low, high]` boundary to
    // verify the new point-CDF primitives against it directly, the same reasoning `BetaBinomialPredictive.Interval`
    // already has for the same accessibility.
    internal static (double Mean, double Low, double High, double AttainedAlpha) Interval(
        double totalCount, int replicaCount, double alpha)
    {
        var r = totalCount + 0.5;
        var p = (double)replicaCount / (replicaCount + 1);
        var q = 1.0 - p;
        var mean = r * q / p;
        var (mode, modePmf) = NegativeBinomialMode(r, p, q);

        // Walk left from the mode down to 0 first, collecting pmf(mode), pmf(mode - 1), ..., pmf(0) in that
        // (decreasing-k) order — the only direction the consecutive-term ratio runs without ever dividing by a
        // vanishing denominator — then consume them in the reverse (increasing-k) order the rest of this function
        // always assumed, exactly reproducing the original k = 0 upward walk's own cdf/low/lowExcludedMass
        // bookkeeping up to and including the mode.
        var belowMode = new List<double>((int)mode + 1);
        var pmfAtK = modePmf;
        for (var k = mode; k > 0.0; k -= 1.0)
        {
            belowMode.Add(pmfAtK);
            pmfAtK *= NegativeBinomialDownRatio(k, r, q);
        }

        belowMode.Add(pmfAtK); // The loop's last transform (or modePmf itself, when mode == 0): pmf(0).

        var cdf = 0.0;
        var low = 0.0;
        var lowExcludedMass = 0.0; // P(X < low); 0 when low is found at k = 0.
        var lowFound = false;
        for (var i = belowMode.Count - 1; i >= 0; i--)
        {
            var cdfBeforeThisTerm = cdf;
            cdf += belowMode[i];
            if (!lowFound && cdf >= alpha / 2.0)
            {
                low = belowMode.Count - 1 - i;
                lowExcludedMass = cdfBeforeThisTerm;
                lowFound = true;
            }
        }

        var high = mode;
        var highFound = cdf >= 1.0 - alpha / 2.0;
        var pmf = modePmf;
        var k2 = mode;
        while (!highFound && k2 < 1_000_000.0)
        {
            var cdfBeforeThisTerm = cdf;
            k2 += 1.0;
            pmf *= NegativeBinomialUpRatio(k2, r, q);
            cdf += pmf;
            if (!lowFound && cdf >= alpha / 2.0)
            {
                low = k2;
                lowExcludedMass = cdfBeforeThisTerm;
                lowFound = true;
            }

            if (cdf >= 1.0 - alpha / 2.0)
            {
                high = k2;
                highFound = true;
            }
        }

        // `cdf` here is P(X <= high) regardless of whether the loop past the mode ran at all or exited through
        // it, so P(X > high) = 1 - cdf holds in both cases without a separate branch.
        var attainedAlpha = lowExcludedMass + (1.0 - cdf);
        return (mean, low, high, attainedAlpha);
    }

    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`, item 5's own second cheap check: `P(X <=
    // k)` and `pmf(k)` for the same `NB(r, p)` this file's own count floor fits, needed for a per-cell exact
    // two-sided predictive tail probability of the *observed* count — a different quantity from `AttainedAlpha`
    // above (the tail mass a fixed nominal `alpha`'s own `[low, high]` excludes), reusing the identical
    // mode/ratio primitives, never a second pmf. Walks down from the mode to `0` exactly as `Interval` does (the
    // only safe direction to reach `pmf(0)` without underflow), summing every term up to `min(k, mode)` along the
    // way; if `k` is past the mode, continues the same upward walk `Interval` uses, capped at the same `1e6`
    // safety bound.
    internal static (double CdfAtMost, double PmfAt) CdfAtMost(double k, double totalCount, int replicaCount)
    {
        var r = totalCount + 0.5;
        var p = (double)replicaCount / (replicaCount + 1);
        var q = 1.0 - p;
        var (mode, modePmf) = NegativeBinomialMode(r, p, q);

        var pmfAtJ = modePmf;
        var sumUpToMinOfKAndMode = 0.0;
        double? pmfAtK = null;
        for (var j = mode; j >= 0.0; j -= 1.0)
        {
            if (j <= k)
            {
                sumUpToMinOfKAndMode += pmfAtJ;
            }

            if (j == k)
            {
                pmfAtK = pmfAtJ;
            }

            if (j > 0.0)
            {
                pmfAtJ *= NegativeBinomialDownRatio(j, r, q);
            }
        }

        if (k <= mode)
        {
            return (sumUpToMinOfKAndMode, pmfAtK ?? 0.0);
        }

        // `sumUpToMinOfKAndMode` is already the full `[0, mode]` mass (every `j` in the loop above satisfied
        // `j <= k` once `k > mode`); continue upward from the mode to `k`.
        var cdf = sumUpToMinOfKAndMode;
        var pmf = modePmf;
        var j2 = mode;
        while (j2 < k && j2 < 1_000_000.0)
        {
            j2 += 1.0;
            pmf *= NegativeBinomialUpRatio(j2, r, q);
            cdf += pmf;
        }

        return (cdf, j2 >= k ? pmf : 0.0);
    }
}
