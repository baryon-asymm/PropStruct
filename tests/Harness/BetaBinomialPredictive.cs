namespace PropStruct.Tests.Harness;

/// <summary>
/// `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the over-dispersed count predictive, parameterised by the
/// cell's own probability
/// <c>p</c> and the family's own intraclass correlation <c>rho</c> rather than by two independently fitted shape
/// parameters — <c>rho = 0</c> is the ordinary <see cref="BinomialBand"/> exactly, its own documented special
/// case. Split out of <c>StatisticalCriterion.Numerics.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class BetaBinomialPredictive
{
    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the two-sided, equal-tailed predictive interval of a
    // count `k*` out of `n`
    // trials under `BetaBinomial(n, a, b)`, parameterised by the cell's own probability `p` (`= sum(k_i) /
    // sum(n_i)` over the replicas) and the family's own intraclass correlation `rho` (`= max(0, (phi - 1) /
    // (n_bar - 1))`, estimated once per family and configuration, never per cell) rather than by two
    // independently fitted shape parameters: `a = p * (1/rho - 1)`, `b = (1 - p) * (1/rho - 1)`, so
    // `a / (a + b) = p` always holds and `rho` alone carries the extra-binomial dispersion. `rho == 0` is the
    // ordinary `Binomial(n, p)` exactly, not an approximation of one reached by letting `a, b -> infinity`: the
    // formula above is undefined at `rho == 0` (division by zero), so it is a genuine special case, computed with
    // the plain binomial pmf ratio instead — this is the same ratio `BinomialBand` already uses, reused, not
    // rewritten (root BOOT.md Taboos: "no second implementation of ... a formula").
    //
    // Mirrors `NegativeBinomialPredictive.Interval`'s own construction exactly: the mode's own log-pmf is computed
    // once, directly, in log space (`StudentDistribution.LogGamma`), and every further term is reached by the
    // ordinary consecutive-term ratio in whichever direction is being extended — never by a second evaluation of
    // the pmf. The only structural difference is this distribution's own finite support, `[0, n]`: the anchor is
    // the mean (`n * p`, clamped to `[0, n]`) rather than an exact mode formula (the beta-binomial's own mode has
    // no simple closed form for a general `rho`), which is only ever used to place a numerically safe starting
    // point for the walk, never assumed to be the true peak — the walk still finds the true `[low, high]` and
    // `AttainedAlpha` regardless of how far the anchor sits from the true mode, the same way
    // `NegativeBinomialPredictive`'s own mode-anchoring is a numerical safeguard against underflow, not a
    // shortcut that skips terms. The upward walk stops at `k = n` instead of an open-ended cap, since this
    // distribution's own support ends there.
    //
    // Verified against an independent implementation (`tests/Harness.Tests/BetaBinomialIntervalTests.cs`, scipy's
    // own `betabinom`/`binom`, never this file's own formula a second time), across `n` from 1 to 10 983 and
    // `rho` from 0 to 0.5, including the exact `(n, p, rho)` of HMX's own `fqdokkarm(31,:)` row-pooled estimate
    // and `coef`'s own largest `n_bar` (`tests/Harness/HISTORY.md#fable-5-1-decision-v`, "Implementation and
    // measurements").
    //
    // ⚠ 2026-09-20, found by the coordinator, not by this file's own review: the very first draft of this
    // function carried the identical `p`/`q` swap this same day's session had just found and fixed in
    // `NegativeBinomialPredictive.Interval` — twice in one day, in one formula shape. The scipy check (above)
    // caught it immediately (three of the nine `rho == 0` cases), but that check is external and typed by hand;
    // the structural fix is `Pmf`'s own standing self-consistency test
    // (`PmfArrayOwnReconstructedMeanMatchesNTimesP`), which reconstructs this method's own mean from the pmf
    // array it hands back (`sum(k * pmf[k])`) and asserts it against the analytic `n * p` — the exact identity a
    // swapped ratio breaks (a wrong ratio still normalizes to a valid-looking distribution,
    // `tests/Harness/HISTORY.md#fable-5-1-decision-v` already found that for `NegativeBinomialPredictive.Interval`'s own swap; it does not
    // still sum to `n * p`), run on every call this file makes to `Pmf`, not as a one-off case list.
    internal static (double Mean, double Low, double High, double AttainedAlpha) Interval(
        long n, double p, double rho, double alpha)
    {
        if (n <= 0 || p <= 0.0)
        {
            return (0.0, 0.0, 0.0, 0.0);
        }

        if (p >= 1.0)
        {
            return (n, n, n, 0.0);
        }

        var mean = n * p;
        var pmf = Pmf(n, p, rho);

        var cdf = 0.0;
        var low = 0.0;
        var lowExcludedMass = 0.0;
        var lowFound = false;
        for (var k = 0; k <= n; k++)
        {
            var cdfBeforeThisTerm = cdf;
            cdf += pmf[k];
            if (!lowFound && cdf >= alpha / 2.0)
            {
                low = k;
                lowExcludedMass = cdfBeforeThisTerm;
                lowFound = true;
            }

            if (cdf >= 1.0 - alpha / 2.0 || k == n)
            {
                var attainedAlpha = lowExcludedMass + (1.0 - cdf);
                return (mean, low, k, attainedAlpha);
            }
        }

        // Unreachable: the loop above always returns by k == n at the latest.
        throw new InvalidOperationException("BetaBinomialPredictive.Pmf's own array did not cover its own support.");
    }

    // `tests/Harness/HISTORY.md#fable-5-1-decision-v`: the beta-binomial's own full-support pmf array, `[0, n]`,
    // built by the mode-anchored, log-space construction `Interval` and `NegativeBinomialPredictive.Interval`
    // both use — the mode's own probability computed once, directly, in log space
    // (`StudentDistribution.LogGamma`), every further term reached by the ordinary consecutive-term ratio in
    // whichever direction is being extended. `rho == 0` is the ordinary `Binomial(n, p)` exactly (the formula
    // below is undefined at `rho == 0`, division by zero, so it is a genuine special case computed with the
    // plain binomial ratio `BinomialBand` already uses, not an approximation reached by letting `a, b ->
    // infinity`). Extracted from `Interval`'s own body so the two callers of this array — the interval search
    // above, and this file's own standing self-consistency test — share one construction, not two (root BOOT.md
    // Taboos: no second implementation of a formula).
    internal static double[] Pmf(long n, double p, double rho)
    {
        if (n <= 0)
        {
            return [1.0];
        }

        if (p <= 0.0)
        {
            var pmfAtZero = new double[n + 1];
            pmfAtZero[0] = 1.0;
            return pmfAtZero;
        }

        if (p >= 1.0)
        {
            var pmfAtN = new double[n + 1];
            pmfAtN[n] = 1.0;
            return pmfAtN;
        }

        var q = 1.0 - p;
        double? a = rho > 0.0 ? p * (1.0 / rho - 1.0) : null;
        double? b = rho > 0.0 ? q * (1.0 / rho - 1.0) : null;

        double LogPmfAt(long k)
        {
            var logBinomialCoefficient = StudentDistribution.LogGamma(n + 1) - StudentDistribution.LogGamma(k + 1)
                - StudentDistribution.LogGamma(n - k + 1);
            if (a is null || b is null)
            {
                return logBinomialCoefficient + k * Math.Log(p) + (n - k) * Math.Log(q);
            }

            return logBinomialCoefficient
                + StudentDistribution.LogGamma(k + a.Value) + StudentDistribution.LogGamma(n - k + b.Value)
                - StudentDistribution.LogGamma(n + a.Value + b.Value)
                - StudentDistribution.LogGamma(a.Value) - StudentDistribution.LogGamma(b.Value)
                + StudentDistribution.LogGamma(a.Value + b.Value);
        }

        // pmf(k - 1) / pmf(k) and pmf(k + 1) / pmf(k), the beta-binomial's own consecutive-term ratios (derived
        // from B(x, y) = Gamma(x) Gamma(y) / Gamma(x + y), the same primitive `LogPmfAt` above uses); `rho == 0`
        // collapses both to the plain binomial ratio `BinomialBand` already uses.
        double DownRatio(long k) => a is { } av && b is { } bv
            ? (double)k / (n - k + 1) * (n - k + bv) / (k - 1 + av)
            : (double)k / (n - k + 1) * q / p;

        double UpRatio(long k) => a is { } av2 && b is { } bv2
            ? (double)(n - k) / (k + 1) * (k + av2) / (n - 1 - k + bv2)
            : (double)(n - k) / (k + 1) * p / q;

        var mode = Math.Clamp((long)Math.Round(n * p), 0L, n);
        var pmf = new double[n + 1];
        pmf[mode] = Math.Exp(LogPmfAt(mode));

        var down = pmf[mode];
        for (var k = mode; k > 0; k--)
        {
            down *= DownRatio(k);
            pmf[k - 1] = down;
        }

        var up = pmf[mode];
        for (var k = mode; k < n; k++)
        {
            up *= UpRatio(k);
            pmf[k + 1] = up;
        }

        return pmf;
    }

    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`, item 5's own second cheap check: `P(X <=
    // k)` and `pmf(k)` read directly off the already-built full-support array — no second pmf, and no separate
    // walk, since the beta-binomial's own support is finite and this file already builds the whole thing.
    internal static (double CdfAtMost, double PmfAt) CdfAtMost(long k, long n, double p, double rho)
    {
        var pmf = Pmf(n, p, rho);
        var kIndex = (int)Math.Clamp(k, 0, n);
        var sum = 0.0;
        for (var i = 0; i <= kIndex; i++)
        {
            sum += pmf[i];
        }

        return (sum, pmf[kIndex]);
    }
}
