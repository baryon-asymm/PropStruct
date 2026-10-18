namespace PropStruct.Particle;

/// <summary>
/// The interpocket bridge's volume between two spheres of radius <c>r1</c>, <c>r2</c>
/// separated by gap <c>a</c> with a pocket of radius <c>rk</c> cut from it (Fortran
/// subroutine <c>VM</c>, source lines 1588-1629). Kernel-compatible: a pure function,
/// the swap of <c>r1</c>/<c>r2</c> is on the method's own copies, never the caller's
/// (BOOT.md, Line map: "argument swap on copies"); the unused <c>error</c> check of
/// line 1611 is not ported (BOOT.md, Line map).
/// </summary>
internal static class BridgeGeometry
{
    // Fortran literal 3.14 (not 3.14159), lines 1616, 1624-1626 (BOOT.md, Constraints).
    private const double ApproximatePi = 3.14;

    /// <summary>
    /// <see langword="false"/> reproduces <c>JJ = 0</c> (line 1598 or line 1613): the gap
    /// swallows the pocket (<paramref name="a"/> ≥ 2·<paramref name="rk"/>) or the chord
    /// <paramref name="bb"/> comes out negative. On <see langword="false"/>,
    /// <paramref name="volume"/> and <paramref name="bb"/> (where not yet computed) are 0,
    /// mirroring the original's unassigned <c>VMKM</c> on early return: the caller never
    /// reads them.
    /// </summary>
    public static bool Volume(double r1, double r2, double rk, double a, out double volume, out double bb)
        => Volume(r1, r2, rk, a, false, out volume, out bb);

    /// <summary>
    /// <see cref="Volume(double, double, double, double, out double, out double)"/> with the
    /// <see cref="CorrectionSet.BridgeFrustum"/> flag as <paramref name="bridgeFrustum"/>:
    /// under it the volume after the <c>bb &lt; 0</c> test is the frustum between the two
    /// chord circles less the two caps, with <see cref="Math.PI"/> (BOOT.md, "## Corrected
    /// model"); the swap, both <c>JJ = 0</c> returns, <c>cosA</c>, <c>cosD</c>, <c>q1</c>,
    /// <c>q2</c>, <c>al</c>, <c>de</c> and <paramref name="bb"/> are shared, so the return
    /// value, <paramref name="bb"/> and every draw are bit-identical under either value.
    /// </summary>
    public static bool Volume(double r1, double r2, double rk, double a, bool bridgeFrustum, out double volume, out double bb)
    {
        if (r2 > r1)
        {
            (r2, r1) = (r1, r2);
        }

        if (a >= 2.0 * rk)
        {
            volume = 0.0;
            bb = 0.0;
            return false;
        }

        var ab = r1 + rk;
        var bc = r2 + rk;
        var ac = r1 + r2 + a;
        var cosA = (ab * ab + ac * ac - bc * bc) / (2.0 * ab * ac);
        var cosD = (ac * ac + bc * bc - ab * ab) / (2.0 * ac * bc);
        var q1 = r1 * cosA;
        var q2 = r2 * cosD;
        var al = Math.Acos(cosA);
        var de = Math.Acos(cosD);
        bb = 2.0 * (ab * Math.Sin(al) - rk);

        if (bb < 0.0)
        {
            volume = 0.0;
            return false;
        }

        var r1A = r1 * Math.Sin(al);
        var r2A = r2 * Math.Sin(de);
        var h1 = r1 - q1;
        var h2 = r2 - q2;

        if (bridgeFrustum)
        {
            // The frustum between the chord circles less the caps, in this order of operations.
            var l = ac - q1 - q2;
            volume = Math.PI / 3.0 * l * (r1A * r1A + r1A * r2A + r2A * r2A)
                - Math.PI * h1 * h1 * (r1 - h1 / 3.0)
                - Math.PI * h2 * h2 * (r2 - h2 / 3.0);
            return true;
        }

        var be = ApproximatePi - al - de;
        var ga = al + be / 2.0;
        var q1M = r1A * Math.Tan(ga);
        var q2M = r2A * Math.Tan(ga);
        var v1 = ApproximatePi * h1 * h1 * (r1 - h1 / 3.0);
        var v2 = ApproximatePi * h2 * h2 * (r2 - h2 / 3.0);

        // Fortran's own VMKM (line 1626, then the self-referencing "VMKM=VMKM-V1-V2" of
        // line 1627) is REAL*4 by implicit typing, but never rounded here: it is a
        // per-call bridge-volume local recomputed fresh on every draw, not summed across
        // attempts, so it is attempt-plane like Dr/Db, not an accumulator (root BOOT.md,
        // "Precision kind"; this node's BOOT.md, "Accumulators", "Decisions where the
        // port departs from a transcription", the VMKM paragraph; RealFourWriteSites.txt).
        volume = ApproximatePi * r1A * r1A * q1M / 3.0 - ApproximatePi * r2A * r2A * q2M / 3.0;
        volume -= v1;
        volume -= v2;
        return true;
    }
}
