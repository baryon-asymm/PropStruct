namespace PropStruct.Particle;

/// <summary>
/// Offsets into the two accumulator buffers an attempt writes, and the length of a
/// particle's scratch (API.md, "Accumulator layout"; BOOT.md, "Accumulators"). Built
/// once per formulation by <see cref="Create"/> from the array sizes of
/// <c>Statistics.Setup.Prepare</c>.
/// </summary>
internal struct AccumulatorLayout
{
    // Offsets into the integer totals (BOOT.md, "Integer totals", in this order).
    public int Conditions;
    public int Nfx;
    public int Nfy;
    public int Nfz;
    public int Nfq;
    public int Nfw;
    public int IbridgeTotal;
    public int LoopCompletions;
    public int AlldokFract;
    public int Alldok;
    public int Qks;
    public int FqkarmCor;
    public int Coef;
    public int Qmkm1;
    public int Qmkm2;
    public int Qdoks;
    public int IntegerLength;

    // Offsets into a particle's real-valued record (BOOT.md, "Real-valued record", in this order).
    public int Xss;
    public int DokBase41;
    public int DokBase31;
    public int DokSur41;
    public int DokSur31;
    public int Sd4;
    public int Sd3;
    public int D41;
    public int D31;
    public int Dp31;
    public int Dp41;
    public int DpMax;
    public int DpMaxCor;
    public int JammedTotal;
    public int NnTotal;
    public int VmkmTotal2;
    public int VdokTotal2;
    public int Allvdok;
    public int Vdokstr;
    public int Vsmkm;
    public int Svd;
    public int VmkmTotal;
    public int VdokTotal;
    public int Vks;
    public int FmkarmCor;
    public int Fmkarm2;
    public int Dokp41;
    public int Dokp31;
    public int RecordLength;

    /// <summary>
    /// 3·Nkarm + 2: <c>fmkarm2_loc</c> (Nkarm), <c>FQKS</c> and <c>DPOC</c> (Nkarm + 1
    /// each). The scalar locals of Fortran lines 426-445 (<c>TU</c>, <c>ipocket_loc</c>,
    /// <c>Vdok_loc</c>, ...) need no slot here: each lives for one <c>Attempt.Run</c>
    /// call only, so they are ordinary kernel-thread locals, not scratch
    /// (BOOT.md, "Scratch of one particle").
    /// </summary>
    public int ScratchLength;

    /// <summary>
    /// Assigns every offset above in field order from the array sizes of one formulation
    /// (<c>Statistics.Setup.Prepare</c>'s NMM, Ndok, Nkarm, Ncat and the fixed Nc = 1000).
    /// </summary>
    public static AccumulatorLayout Create(int fractionCount, int ndok, int nkarm, int ncat, int nc)
    {
        var layout = new AccumulatorLayout();

        var offset = 0;
        layout.Conditions = offset; offset += 9;
        layout.Nfx = offset; offset += 1;
        layout.Nfy = offset; offset += 1;
        layout.Nfz = offset; offset += 1;
        layout.Nfq = offset; offset += 1;
        layout.Nfw = offset; offset += 1;
        layout.IbridgeTotal = offset; offset += 1;
        layout.LoopCompletions = offset; offset += 1;
        layout.AlldokFract = offset; offset += fractionCount;
        layout.Alldok = offset; offset += ndok;
        layout.Qks = offset; offset += nkarm;
        layout.FqkarmCor = offset; offset += nkarm;
        layout.Coef = offset; offset += nc;
        layout.Qmkm1 = offset; offset += nc;
        layout.Qmkm2 = offset; offset += nc;
        layout.Qdoks = offset; offset += ncat * ndok;
        layout.IntegerLength = offset;

        offset = 0;
        layout.Xss = offset; offset += 7;
        layout.DokBase41 = offset; offset += 1;
        layout.DokBase31 = offset; offset += 1;
        layout.DokSur41 = offset; offset += 1;
        layout.DokSur31 = offset; offset += 1;
        layout.Sd4 = offset; offset += 1;
        layout.Sd3 = offset; offset += 1;
        layout.D41 = offset; offset += 1;
        layout.D31 = offset; offset += 1;
        layout.Dp31 = offset; offset += 1;
        layout.Dp41 = offset; offset += 1;
        layout.DpMax = offset; offset += 1;
        layout.DpMaxCor = offset; offset += 1;
        layout.JammedTotal = offset; offset += 1;
        layout.NnTotal = offset; offset += 1;
        layout.VmkmTotal2 = offset; offset += 1;
        layout.VdokTotal2 = offset; offset += 1;
        layout.Allvdok = offset; offset += ndok;
        layout.Vdokstr = offset; offset += ndok;
        layout.Vsmkm = offset; offset += ndok;
        layout.Svd = offset; offset += ndok;
        layout.VmkmTotal = offset; offset += ndok;
        layout.VdokTotal = offset; offset += ndok;
        layout.Vks = offset; offset += nkarm;
        layout.FmkarmCor = offset; offset += nkarm;
        layout.Fmkarm2 = offset; offset += nkarm;
        layout.Dokp41 = offset; offset += ncat;
        layout.Dokp31 = offset; offset += ncat;
        layout.RecordLength = offset;

        layout.ScratchLength = 3 * nkarm + 2;

        return layout;
    }
}
