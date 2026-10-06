using ILGPU;

namespace PropStruct.Particle;

/// <summary>
/// The pocket-size window a bridge samples from: built once per attempt from the frozen
/// pocket histogram and reused by every later bridge of the same attempt
/// (BOOT.md, "Bridge window once per attempt"; Line map lines 582-611, 612-625,
/// 1573-1586). Kernel-compatible: pure functions over caller-owned scratch views, no
/// allocation.
/// </summary>
internal static class BridgeWindow
{
    // Fortran line 601: "IF((1.0-FQKS(iks)).LT.1E-5)".
    private const double TruncationThreshold = 1e-5;

    /// <summary>
    /// Builds the cumulative window (<paramref name="fqks"/>, <paramref name="dpoc"/>,
    /// <paramref name="nnn1"/>) over cells <c>AK3·Dr/Di .. AK4·Dr/Di</c> of
    /// <paramref name="qks1"/> (Fortran lines 582-610). <see langword="false"/>
    /// reproduces the empty-window restart of line 590 (<c>AUS = 0</c>, <c>GO TO 11</c>):
    /// the caller ends the attempt with <see cref="AttemptOutcome.RestartedInsideLoop"/>.
    /// <paramref name="fqks"/> and <paramref name="dpoc"/> are the particle's own scratch,
    /// each sized <c>Nkarm + 1</c>.
    /// </summary>
    public static bool Build(
        double dr, double ak3, double ak4, double cellSize, int nkarm,
        ArrayView<double> qks1, ArrayView<double> fqks, ArrayView<double> dpoc,
        out int nnn1)
    {
        var rr1 = ak3 * dr;
        var rr2 = ak4 * dr;
        var mindk = (int)(rr1 / cellSize) + 1; // Fortran 1-based cell number
        var maxdk = (int)(rr2 / cellSize) + 1;

        var aus = 0.0;
        for (var iks = mindk; iks <= maxdk; iks++)
        {
            aus += qks1[iks - 1];
        }

        if (aus == 0.0)
        {
            nnn1 = 0;
            return false;
        }

        aus = 1.0 / aus;

        for (var iks = 1; iks <= nkarm + 1; iks++)
        {
            dpoc[iks - 1] = 0.0;
            fqks[iks - 1] = 0.0;
        }

        dpoc[mindk - 1] = cellSize * mindk;

        for (var iks = mindk; iks <= maxdk; iks++)
        {
            fqks[iks] = aus * qks1[iks - 1] + fqks[iks - 1];
            dpoc[iks] = cellSize + dpoc[iks - 1];
        }

        for (var iks = mindk; iks <= maxdk + 1; iks++)
        {
            if (1.0 - fqks[iks - 1] < TruncationThreshold)
            {
                maxdk = iks - 1;
                fqks[iks - 1] = 1.0;
                break;
            }
        }

        nnn1 = maxdk - mindk + 2;

        // Forward, in place: the source index (mindk - 1 + iks) never trails the destination
        // index (iks), so no cell is overwritten before its own turn as a source (proof in
        // tests/Particle.Tests/BOOT.md).
        for (var iks = 1; iks <= nnn1; iks++)
        {
            dpoc[iks - 1] = dpoc[mindk - 1 + iks - 1];
            fqks[iks - 1] = fqks[mindk - 1 + iks - 1];
        }

        return true;
    }

    /// <summary>
    /// The pocket size at <paramref name="x"/> within the window built by
    /// <see cref="Build"/> (Fortran subroutine <c>DM</c>, source lines 1573-1586):
    /// linear interpolation within the cell <c>i</c> where
    /// <paramref name="fqks"/>[i] ≤ <paramref name="x"/> &lt; <paramref name="fqks"/>[i + 1].
    /// <see langword="false"/> is the port's guard, not a modeled branch (BOOT.md, "IndexOutOfRange"):
    /// the original's unbounded search cannot leave the window under the node's
    /// preconditions, so the guard is never expected to fire.
    /// </summary>
    public static bool SamplePocket(double x, ArrayView<double> fqks, ArrayView<double> dpoc, int nnn1, out double dkarm)
    {
        var i = 0;
        while (true)
        {
            if (i + 1 >= nnn1)
            {
                dkarm = 0.0;
                return false;
            }

            if (x >= fqks[i] && x < fqks[i + 1])
            {
                dkarm = dpoc[i] + (x - fqks[i]) * (dpoc[i + 1] - dpoc[i]) / (fqks[i + 1] - fqks[i]);
                return true;
            }

            i++;
        }
    }
}
