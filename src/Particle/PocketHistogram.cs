using ILGPU;

namespace PropStruct.Particle;

/// <summary>
/// The pocket histogram normalization the driver runs between launches (Fortran lines
/// 717-718: <c>QKSS = sum(QKS); QKS1 = QKS/real(QKSS)</c>). Never called by
/// <see cref="Attempt.Run"/> itself (BOOT.md, "Frozen inputs").
/// </summary>
internal static class PocketHistogram
{
    /// <summary>
    /// <paramref name="qks1"/>[i] = <c>Qks</c>[i] / Σ<c>Qks</c> over the current integer
    /// totals (Fortran lines 717-718). With Σ<c>Qks</c> = 0 the original computes
    /// <c>NaN</c> (line 718, <c>0./0.</c>), read only if the whole of cycle 0 produced
    /// no pocket, and then its bridges would build a <c>NaN</c> window; the port stores
    /// zeros instead (declared, not reproduced, BOOT.md, "Decisions where the port
    /// departs from a transcription"), so such a run's bridges restart on the empty
    /// window until a completed loop refreshes QKS1 with a pocket, or the attempt cap
    /// ends the run.
    /// </summary>
    public static void Normalize(in AccumulatorLayout layout, ArrayView<long> integerTotals, ArrayView<double> qks1)
    {
        var nkarm = (int)qks1.Length;

        var sum = 0L;
        for (var i = 0; i < nkarm; i++)
        {
            sum += integerTotals[layout.Qks + i];
        }

        if (sum == 0L)
        {
            for (var i = 0; i < nkarm; i++)
            {
                qks1[i] = 0.0;
            }

            return;
        }

        var total = (double)sum;
        for (var i = 0; i < nkarm; i++)
        {
            qks1[i] = integerTotals[layout.Qks + i] / total;
        }
    }
}
