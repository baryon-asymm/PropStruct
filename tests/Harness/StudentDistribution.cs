namespace PropStruct.Tests.Harness;

/// <summary>
/// The two-sided critical value of a Student's t distribution, for the root criterion's per-quantity threshold
/// (root BOOT.md, "Statistical reference criterion"). Not a transcription of the original program: the legacy
/// Fortran never computed a Student quantile, so there is no source line to reproduce or declare a deviation
/// from (AGENTS.md §12 does not apply here).
///
/// The CDF of <c>T ~ t(df)</c> at <c>t &gt;= 0</c> is <c>F(t) = 1 - 0.5 * I_x(df/2, 1/2)</c>, where
/// <c>x = df / (df + t^2)</c> and <c>I_x</c> is the regularized incomplete beta function (Abramowitz &amp;
/// Stegun 26.7.5); by symmetry <c>P(|T| &gt; t) = I_x(df/2, 1/2)</c>. <see cref="TwoSidedQuantile"/> solves
/// that equation for <c>t</c> by bisection, evaluating <c>I_x</c> with the standard continued-fraction expansion
/// (Numerical Recipes' <c>betacf</c>/<c>betai</c>): `tests/Harness.Tests/BOOT.md`, "Student quantile", records the
/// non-degeneracy mutation and the scipy comparison this is proven against.
/// </summary>
public static class StudentDistribution
{
    private const int MaxContinuedFractionIterations = 300;
    private const double ContinuedFractionEpsilon = 1e-15;
    private const double Tiny = 1e-300;
    private const int MaxBisectionIterations = 200;

    /// <summary>
    /// The value <c>t &gt; 0</c> such that <c>P(|T| &gt; t) = twoSidedAlpha</c> for a Student's t distribution
    /// with <paramref name="df"/> degrees of freedom.
    /// </summary>
    public static double TwoSidedQuantile(int df, double twoSidedAlpha)
    {
        if (df < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(df), df, "degrees of freedom must be at least 1.");
        }

        if (twoSidedAlpha is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(twoSidedAlpha), twoSidedAlpha, "must lie strictly between 0 and 1.");
        }

        var a = df / 2.0;
        const double b = 0.5;

        var lo = 0.0;
        var hi = 1.0;
        while (UpperTailProbability(hi, df, a, b) > twoSidedAlpha)
        {
            hi *= 2.0;
            if (hi > 1e12)
            {
                break;
            }
        }

        for (var iteration = 0; iteration < MaxBisectionIterations; iteration++)
        {
            var mid = 0.5 * (lo + hi);
            if (mid == lo || mid == hi)
            {
                break;
            }

            if (UpperTailProbability(mid, df, a, b) > twoSidedAlpha)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return 0.5 * (lo + hi);
    }

    // P(|T| > t) = I_x(df/2, 1/2), x = df / (df + t^2).
    private static double UpperTailProbability(double t, int df, double a, double b)
    {
        var x = df / (df + t * t);
        return RegularizedIncompleteBeta(x, a, b);
    }

    private static double RegularizedIncompleteBeta(double x, double a, double b)
    {
        if (x <= 0.0)
        {
            return 0.0;
        }

        if (x >= 1.0)
        {
            return 1.0;
        }

        var logBeta = LogGamma(a + b) - LogGamma(a) - LogGamma(b);
        var front = Math.Exp(logBeta + a * Math.Log(x) + b * Math.Log(1.0 - x));

        return x < (a + 1.0) / (a + b + 2.0)
            ? front * ContinuedFraction(x, a, b) / a
            : 1.0 - front * ContinuedFraction(1.0 - x, b, a) / b;
    }

    private static double ContinuedFraction(double x, double a, double b)
    {
        var qab = a + b;
        var qap = a + 1.0;
        var qam = a - 1.0;
        var c = 1.0;
        var d = 1.0 - qab * x / qap;
        if (Math.Abs(d) < Tiny)
        {
            d = Tiny;
        }

        d = 1.0 / d;
        var h = d;

        for (var m = 1; m <= MaxContinuedFractionIterations; m++)
        {
            var m2 = 2 * m;

            var evenTerm = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = 1.0 + evenTerm * d;
            if (Math.Abs(d) < Tiny)
            {
                d = Tiny;
            }

            c = 1.0 + evenTerm / c;
            if (Math.Abs(c) < Tiny)
            {
                c = Tiny;
            }

            d = 1.0 / d;
            h *= d * c;

            var oddTerm = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = 1.0 + oddTerm * d;
            if (Math.Abs(d) < Tiny)
            {
                d = Tiny;
            }

            c = 1.0 + oddTerm / c;
            if (Math.Abs(c) < Tiny)
            {
                c = Tiny;
            }

            d = 1.0 / d;
            var delta = d * c;
            h *= delta;

            if (Math.Abs(delta - 1.0) < ContinuedFractionEpsilon)
            {
                break;
            }
        }

        return h;
    }

    // Lanczos approximation (g = 7, n = 9), standard double-precision coefficients.
    private static readonly double[] LanczosCoefficients =
    [
        0.99999999999980993, 676.5203681218851, -1259.1392167224028,
        771.32342877765313, -176.61502916214059, 12.507343278686905,
        -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7,
    ];

    // internal, not private: `tests/Harness/HISTORY.md#fable-5-1-decision-ii` reuses this same log-gamma to
    // compute an exact binomial tail region (`BinomialBand.HighestDensityRegion`)
    // rather than writing a second one (root BOOT.md Taboos: "no second implementation of any part of ... a
    // formula"). The visibility change alone does not touch this type's public surface.
    internal static double LogGamma(double x)
    {
        if (x < 0.5)
        {
            return Math.Log(Math.PI / Math.Sin(Math.PI * x)) - LogGamma(1.0 - x);
        }

        x -= 1.0;
        var accumulator = LanczosCoefficients[0];
        for (var i = 1; i < LanczosCoefficients.Length; i++)
        {
            accumulator += LanczosCoefficients[i] / (x + i);
        }

        var t = x + 7.5;
        return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(t) - t + Math.Log(accumulator);
    }
}
