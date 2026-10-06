using ILGPU;

namespace PropStruct.Particle;

/// <summary>
/// The original's particle-size draw (Fortran subroutine <c>SIZE</c>, source lines
/// 1761-1776): which fraction a draw falls in, and the diameter within it under either
/// size law. Kernel-compatible: a pure function of its arguments, no allocation, no
/// exceptions (BOOT.md, Line map).
/// </summary>
internal static class SizeLaw
{
    /// <summary>
    /// <paramref name="fractionCount"/> is the original's <c>NM</c>, passed explicitly by
    /// the caller (<c>ModelSetup.FractionCount</c>) rather than derived from
    /// <paramref name="cumulative"/>'s length: the original's <c>SIZE</c> takes <c>NM</c>
    /// as its own parameter, and a derived count would only happen to agree with it when
    /// the caller's arrays are sized exactly <c>NM + 1</c>/<c>2·NM</c> (2026-09-17,
    /// `src/Particle/BOOT.md`, "Decisions where the port departs from a transcription").
    /// <paramref name="fraction"/> is the 0-based fraction index (Fortran <c>MINV - 1</c>):
    /// the highest <c>i</c> with <paramref name="x"/> in
    /// (<paramref name="cumulative"/>[i], <paramref name="cumulative"/>[i + 1]], scanned
    /// over the whole table exactly as the original's unconditional <c>DO</c> loop does
    /// (line 1765-1767). <paramref name="x"/> = 0 cannot occur (the generator's state is
    /// always odd, BOOT.md, Line map): the loop then leaves <paramref name="fraction"/>
    /// at its start value 0, the port's stand-in for the original's stale <c>MINV</c>.
    /// <paramref name="diameter"/> is the uniform-in-D law for
    /// <paramref name="sizeLaw"/> == 2 (line 1772-1773) and the uniform-in-1/D² law
    /// otherwise (line 1769-1770).
    /// </summary>
    public static void Sample(
        int sizeLaw, int fractionCount, ArrayView<double> bounds, ArrayView<double> cumulative,
        double x, double x1,
        out double diameter, out int fraction)
    {
        var minv = 0;
        for (var i = 0; i < fractionCount; i++)
        {
            if (x > cumulative[i] && x <= cumulative[i + 1])
            {
                minv = i;
            }
        }

        var lower = bounds[2 * minv];
        var upper = bounds[2 * minv + 1];

        if (sizeLaw == 2)
        {
            diameter = x1 * (upper - lower) + lower;
        }
        else
        {
            var invLowerSquared = 1.0 / (lower * lower);
            var invUpperSquared = 1.0 / (upper * upper);
            diameter = 1.0 / Math.Sqrt(invLowerSquared - x1 * (invLowerSquared - invUpperSquared));
        }

        fraction = minv;
    }
}
