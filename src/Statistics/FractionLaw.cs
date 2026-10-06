using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The fraction-law half of the original's <c>PARAM</c> subroutine (Fortran source
/// lines 1695-1756): the unnormalized weight by size law, its normalization and
/// cumulative table, and the tail-probability draw that decides which fraction and
/// where in it the largest base-particle size is picked from. Host code, double
/// precision, no accelerator (BOOT.md, Constraints).
/// </summary>
internal static class FractionLaw
{
    /// <summary>
    /// Fortran lines 1695-1734: the unnormalized <c>Z</c> by size law, its sum
    /// <c>ZSS</c>, the normalized <c>ZX</c> and the cumulative <c>Z11</c> with
    /// <c>Z11(NMM+1) = 1</c>. <c>Zerror</c> (line 1733) is never read by anything and
    /// is not ported (BOOT.md, "Line map"). <paramref name="bounds"/> is the Fortran
    /// 1-based <c>DOK</c> convention pairs, 0-based here: fraction <c>j</c>'s
    /// lower/upper bound are <paramref name="bounds"/>[2*j]/[2*j+1].
    /// </summary>
    /// <remarks>
    /// Under <see cref="PrecisionKind.Original"/> (BOOT.md, "## Setup plane"): <c>G(J)</c>
    /// and <c>DOK(I)</c>/<c>DOK(I+1)</c> are REAL*4 in the Fortran, so <paramref
    /// name="massShare"/> and <paramref name="bounds"/> are expected already rounded to
    /// binary32 by the caller (<see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/>); the two size-law coefficients
    /// (<c>3/3.14</c>, <c>24/3.14</c>) are all-literal expressions the compiler folds in
    /// binary32 (<c>SetupPlane.generated.txt</c>, <c>NonUniformSizeLawCoefficient</c>,
    /// <c>UniformSizeLawCoefficient</c>); <c>Z(J)</c> itself is REAL*8 (<c>ZX</c> in the
    /// caller), so its right-hand side is computed from these binary32 operands in
    /// <c>double</c> and stored unrounded -- the one trap this recipe names explicitly.
    /// <c>ZS</c> -- <paramref name="sum"/>, <c>PARAM</c>'s own dummy for the caller's
    /// <c>ZSS</c> -- is REAL*4 (<c>SetupPlane.generated.txt</c>, <c>FractionWeightSum</c>)
    /// and is rounded after every addition; the final <c>share[j] = z[j] / zss</c> divides
    /// an unrounded REAL*8 numerator by the rounded REAL*4 divisor and stores the REAL*8
    /// result unrounded, and <paramref name="cumulative"/> (<c>Z11</c>, REAL*8) is never
    /// rounded either. Under <see cref="PrecisionKind.Binary64"/> every one of these
    /// distinctions collapses: the coefficients and <paramref name="sum"/> are exactly what
    /// this method always computed, bit for bit.
    /// </remarks>
    public static void Build(
        int sizeLaw, int fractionCount, double[] massShare, double[] bounds, PrecisionKind precision,
        out double[] share, out double[] cumulative, out double sum)
    {
        var nonUniformCoefficient = precision == PrecisionKind.Original
            ? Binary32.ToNearestRepresentable(3.0 / Binary32.ToNearestRepresentable(3.14))
            : 3.0 / 3.14;
        var uniformCoefficient = precision == PrecisionKind.Original
            ? Binary32.ToNearestRepresentable(24.0 / Binary32.ToNearestRepresentable(3.14))
            : 24.0 / 3.14;

        var z = new double[fractionCount];
        for (var j = 0; j < fractionCount; j++)
        {
            var lower = bounds[2 * j];
            var upper = bounds[2 * j + 1];
            if (precision == PrecisionKind.Original)
            {
                // The executable forms DOK(I)**2 and DOK(I+1)**2 on their own and multiplies them, so the
                // denominator is rounded once ((l*l)*(u*u)), where C#'s left-to-right l*l*u*u rounds twice;
                // DOK**4 is (x*x)*(x*x). ZX and Z11 differ by one to three ulp of double on 33 and 16 of the
                // 49 shipped files otherwise (0x4181A5-0x418574, the oracle's executed PARAM).
                var lowerSquared = lower * lower;
                var upperSquared = upper * upper;
                z[j] = sizeLaw == 2
                    ? massShare[j] * uniformCoefficient * (upper - lower) / (upperSquared * upperSquared - lowerSquared * lowerSquared)
                    : massShare[j] * nonUniformCoefficient * (lower + upper) / (lowerSquared * upperSquared);
                continue;
            }

            z[j] = sizeLaw == 2
                ? massShare[j] * uniformCoefficient * (upper - lower) / (Math.Pow(upper, 4) - Math.Pow(lower, 4))
                : massShare[j] * nonUniformCoefficient * (lower + upper) / (lower * lower * upper * upper);
        }

        var zss = 0.0;
        for (var j = 0; j < fractionCount; j++)
        {
            var sumWithTerm = zss + z[j];
            zss = precision == PrecisionKind.Original ? Binary32.ToNearestRepresentable(sumWithTerm) : sumWithTerm;
        }

        share = new double[fractionCount];
        for (var j = 0; j < fractionCount; j++)
        {
            share[j] = z[j] / zss;
        }

        cumulative = new double[fractionCount + 1];
        cumulative[0] = 0.0;
        for (var j = 0; j < fractionCount; j++)
        {
            cumulative[j + 1] = cumulative[j] + share[j];
        }

        cumulative[fractionCount] = 1.0; // Z1(NM+1) = 1 (line 1732), overwrites the accumulated sum's own rounding.
        sum = zss;
    }

    /// <summary>
    /// Fortran line 377 and 1735-1754: the largest active fraction's upper bound is the
    /// tail-probability draw's starting point; a fraction whose share the remaining
    /// <paramref name="tailProbability"/> cannot reach is excluded and the search
    /// repeats on the next-largest active fraction, until one fits. This method stops
    /// exactly where line 1754's <c>CALL SIZE(...)</c> begins: the diameter itself comes
    /// from the full <c>Particle.SizeLaw.Sample</c>, called by the driver that owns the
    /// accelerator (<c>Simulation</c>, through <c>Execution</c> — root BOOT.md,
    /// "## Decomposition": "Execution is the only node that knows accelerators exist"),
    /// never re-implemented here (root BOOT.md taboo: "no second implementation of any
    /// part of the particle program"). <paramref name="x"/> and <paramref name="x1"/>
    /// are exactly <c>SizeLaw.Sample</c>'s own <c>x</c>/<c>x1</c> parameters; its
    /// fraction search may land on a different fraction than the Fortran's own <c>imax</c>
    /// (BOOT.md, "Line map").
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when every fraction has been excluded and no active
    /// fraction remains to draw from. This is unreachable given the preconditions
    /// <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> already checks (<c>0 &lt;= tailProbability &lt; 1</c>
    /// and <see cref="Build"/>'s own <c>cumulative[fractionCount] == 1</c>): each
    /// exclusion subtracts that fraction's own share
    /// (<c>cumulative[i+1] - cumulative[i]</c>) from the remaining probability, and the
    /// shares of every fraction sum to exactly 1, so once every fraction but the last
    /// has been excluded the remaining probability is strictly less than the last
    /// fraction's own share and it is never excluded. Returning a status here rather
    /// than indexing an already-empty active set keeps this method itself a value
    /// (root BOOT.md, "Failures are values"), even though the branch it guards cannot
    /// be taken; the alternative was an unchecked index that would throw
    /// <see cref="IndexOutOfRangeException"/>, found by the audit of this node.
    /// </returns>
    public static bool TryTailDraw(
        int fractionCount, double[] bounds, double[] cumulative, double tailProbability,
        out double x, out double x1, out double tailProbabilityModified)
    {
        var active = new bool[fractionCount];
        Array.Fill(active, true);
        var alfa = tailProbability;

        while (true)
        {
            var imax = -1;
            var candidateUpper = 0.0;
            for (var i = 0; i < fractionCount; i++)
            {
                if (active[i] && bounds[2 * i + 1] > candidateUpper)
                {
                    candidateUpper = bounds[2 * i + 1];
                    imax = i;
                }
            }

            if (imax < 0)
            {
                x = default;
                x1 = default;
                tailProbabilityModified = default;
                return false;
            }

            var candidate = cumulative[imax + 1] - alfa;
            if (candidate - cumulative[imax] < 0.0)
            {
                active[imax] = false;
                alfa -= cumulative[imax + 1] - cumulative[imax];
                continue;
            }

            x = candidate;
            x1 = (candidate - cumulative[imax]) / (cumulative[imax + 1] - cumulative[imax]);
            tailProbabilityModified = alfa;
            return true;
        }
    }
}
