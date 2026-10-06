using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The end-of-cycle processing (Fortran lines 771-1175): generator accuracy, oxidizer
/// and pocket size distributions, the category merge, the matrix and small-particle
/// probability, and, in cycles &gt;= 1, the corrected pocket sizes, mass fractions and
/// convergence values. Host code, double precision, allocation allowed (BOOT.md,
/// Constraints). In cycle 0 the Fortran jumps back before ever reaching lines
/// 1084-1175 (<c>IF(IPRIS.EQ.0) GOTO 700</c>, line 1078): the fields of
/// <see cref="CycleReport"/> that line range alone computes are <see cref="double.NaN"/>
/// for <c>cycleIndex == 0</c> (BOOT.md, "## Report"). <c>categoriesStatus</c> carries
/// <see cref="Categories.MergeAndDescribe"/>'s own status through unchanged: on anything
/// but <see cref="CategoriesStatus.Ok"/> the returned <see cref="CycleReport"/>'s own
/// category fields (<c>DpRow</c>, <c>Dpockets</c>, <c>Dokp43</c>, <c>Qdokkarm</c>,
/// <c>Qdokso</c>) are the empty defaults <see cref="Categories.MergeAndDescribe"/>
/// itself returns in that case, and the totals are left unread and unwritten by that
/// call (root BOOT.md, "Failures are values": this node reports the status, the driver
/// turns it into an exception).
/// </summary>
internal static class CycleStatistics
{
    /// <summary>
    /// The end-of-cycle processing itself. After <c>Setup.Prepare</c> this node reads no
    /// setup-plane member from <c>Formulation</c> or <c>ModelParameters</c> directly:
    /// every generated <c>input</c>-role value it needs arrives through
    /// <paramref name="setup"/> (<c>Ak2</c> and the sizes already did), <paramref name="tables"/>
    /// (<see cref="SetupTables.MassShare"/>, the per-fraction raw share) or
    /// <paramref name="inputs"/> (BOOT.md, "## Setup plane", "Design decision, 2026-09-24").
    /// The run's precision kind arrives as <see cref="ModelSetup.Kind"/>: under
    /// <see cref="PrecisionKind.Original"/> the plane does what the executable's listing does
    /// (<c>CyclePlane.listing.generated.txt</c>): every store rounds through one
    /// <see cref="CyclePlaneRounding"/>, every sum follows its <see cref="UnrolledSchedule"/>,
    /// every product its <see cref="CyclePlaneOrder"/> and every read takes the register or the
    /// home the table names (BOOT.md, "## Report", "The per-cycle plane from the executable's
    /// listing"); under <see cref="PrecisionKind.Binary64"/> every result is bit-identical to the
    /// plane before the kind reached it.
    /// </summary>
    public static CycleReport Compute(
        in ModelSetup setup, SetupTables tables, SetupEchoes echoes,
        SetupInputs inputs,
        int cycleIndex,
        Span<long> integerTotals,
        Span<double> realTotals,
        double[] nextPdoksmall,
        out double nextDmaxxx,
        out CategoriesStatus categoriesStatus)
    {
        var layout = setup.Layout;
        var ndok = setup.Ndok;
        var nkarm = setup.Nkarm;
        var nc = setup.Nc;
        var di = setup.CellSize;
        var epsDok = inputs.EpsDok;
        var rounding = new CyclePlaneRounding(setup.Kind);
        var order = new CyclePlaneOrder(setup.Kind);

        GeneratorAccuracy(rounding, layout, integerTotals, realTotals,
            out var eps1, out var eps2, out var eps3, out var eps4, out var eps5, out var eps6, out var eps7);

        OxidizerSizes(rounding, order, setup, tables, echoes, layout, integerTotals, realTotals, epsDok,
            out var dok43b, out var dok43s, out var epsalldok, out var allvdokso, out var alldok432,
            out var alldok43, out var alldoksd, out var epsx1, out var epsx2, out var epsx3,
            out var oxidizerWarning, out var fmdok);

        categoriesStatus = Categories.MergeAndDescribe(ndok, setup.Ncat, di, setup.CategoryStep, epsDok, realTotals[layout.DpMax],
            layout, integerTotals, realTotals, setup.Kind,
            out var dpRow, out var dpockets, out var dokp43, out var qdokkarm, out var qdokso);

        Pockets(rounding, order, setup, layout, integerTotals, realTotals, epsDok,
            out var qkss, out var epsy, out var epsmd4, out var epsmd3, out var pocketWarning,
            out var vkso, out var d432, out var dqkarm, out var dp43, out var sdevp43, out var qks1);

        Matrix(rounding, setup, tables, inputs, echoes.OxidizerMassFractionEffective, fmdok,
            out var gdokleft, out var vdokleft, out var plotsmdok, out var plotsm, out var mp);

        var vdokstr = realTotals.Slice(layout.Vdokstr, ndok).ToArray();
        var pdoksmall = SmallParticles.Probability(
            ndok, di, setup.Ak2, echoes.OxidizerMassFractionEffective, gdokleft, setup.PocketCoefficient,
            inputs.OxidizerDensity, inputs.PropellantDensity, vdokstr, setup.Kind);
        Array.Copy(pdoksmall, nextPdoksmall, ndok);
        nextDmaxxx = SmallParticles.MaxSize(ndok, di, echoes.Ddokmax, setup.PocketCoefficient, setup.BridgeCoefficient, pdoksmall, setup.Kind);

        // The report's own Pdoksmall is Ndok-1 elements, not Ndok (BOOT.md, "## Report",
        // "Decisions where the port departs from a transcription"): Fortran line 1386
        // prints `pdoksmall` at length `Ndok-1`, one short of the array's own dimension,
        // while `nextPdoksmall` above (the next cycle's model input, Fortran's own
        // `Attempt.cs` reads) stays the full Ndok elements the model loop needs.
        var pdoksmallReport = pdoksmall[..^1];

        var fmkarmCorNormalized = Normalize(realTotals.Slice(layout.FmkarmCor, nkarm).ToArray());
        var fmkarm2Normalized = Normalize(realTotals.Slice(layout.Fmkarm2, nkarm).ToArray());
        var fqkarmCorNormalized = NormalizeIntegers(integerTotals.Slice(layout.FqkarmCor, nkarm));
        var qmkm1Normalized = NormalizeIntegers(integerTotals.Slice(layout.Qmkm1, nc));
        var qmkm2Normalized = NormalizeIntegers(integerTotals.Slice(layout.Qmkm2, nc));
        var coefNormalized = NormalizeIntegers(integerTotals.Slice(layout.Coef, nc));

        double dolM1 = double.NaN, dolM2 = double.NaN, dolM3 = double.NaN;
        double dkarm43Cor = double.NaN, sdevp43Cor = double.NaN, dqkarmCor = double.NaN;
        double dfmk432 = double.NaN, sdevp243 = double.NaN;
        double dqmkm1 = double.NaN, dqmkm2 = double.NaN, qmcoef = double.NaN;
        double convergenceEpsy = double.NaN, convergenceEpsmd3 = double.NaN, convergenceEpsmd4 = double.NaN;
        double convergenceAlldok43 = double.NaN, convergenceAlldoksd = double.NaN, convergenceDolM2 = double.NaN;

        if (cycleIndex >= 1)
        {
            CorrectedPockets(rounding, order, setup, layout, integerTotals, realTotals,
                out dkarm43Cor, out sdevp43Cor, out dqkarmCor, out dfmk432, out sdevp243,
                out dqmkm1, out dqmkm2, out qmcoef);

            MassFractions(rounding, setup, layout, realTotals, echoes.OxidizerMassFractionEffective, inputs.OxidizerDensity,
                gdokleft, plotsmdok, plotsm, vdokleft, out dolM1, out dolM2, out dolM3);

            Convergence(rounding, echoes, epsy, epsmd3, epsmd4, alldok43, alldoksd, dolM2,
                out convergenceEpsy, out convergenceEpsmd3, out convergenceEpsmd4,
                out convergenceAlldok43, out convergenceAlldoksd, out convergenceDolM2);
        }

        return new CycleReport
        {
            Dokm = echoes.Dokm,
            Doksd = echoes.Doksd,
            Ddokmax = echoes.Ddokmax,
            Dmax = echoes.Dmax,
            TailProbabilityModified = echoes.TailProbabilityModified,
            Ggg = echoes.OxidizerMassFractionEffective,
            Zx = tables.Share,

            Qkss = qkss,
            Nfx = integerTotals[layout.Nfx],
            Nfy = integerTotals[layout.Nfy],
            Nfz = integerTotals[layout.Nfz],
            Nfq = integerTotals[layout.Nfq],
            Nfw = integerTotals[layout.Nfw],
            Conditions = integerTotals.Slice(layout.Conditions, 9).ToArray(),
            IbridgeTotal = integerTotals[layout.IbridgeTotal],
            JammedTotal = realTotals[layout.JammedTotal],
            NnTotal = realTotals[layout.NnTotal],

            Eps1 = eps1,
            Eps2 = eps2,
            Eps3 = eps3,
            Eps4 = eps4,
            Eps5 = eps5,
            Eps6 = eps6,
            Eps7 = eps7,
            Epsx1 = epsx1,
            Epsx2 = epsx2,
            Epsx3 = epsx3,
            Epsalldok = epsalldok,
            Epsy = epsy,
            Epsmd4 = epsmd4,
            Epsmd3 = epsmd3,
            OxidizerAccuracyWarning = oxidizerWarning,
            PocketAccuracyWarning = pocketWarning,

            Dok43b = dok43b,
            Dok43s = dok43s,
            Alldok43 = alldok43,
            Alldok432 = alldok432,
            Dp43 = dp43,
            D432 = d432,
            Sdevp43 = sdevp43,
            Dqkarm = dqkarm,
            Dkarm43Cor = dkarm43Cor,
            Sdevp43Cor = sdevp43Cor,
            DqkarmCor = dqkarmCor,
            Dfmk432 = dfmk432,
            Sdevp243 = sdevp243,
            Dqmkm1 = dqmkm1,
            Dqmkm2 = dqmkm2,
            Qmcoef = qmcoef,

            Gdokleft = gdokleft,
            Vdokleft = vdokleft,
            Plotsmdok = plotsmdok,
            Plotsm = plotsm,
            Mp = mp,

            DolM1 = dolM1,
            DolM2 = dolM2,
            DolM3 = dolM3,

            Allvdokso = allvdokso,
            Vkso = vkso,
            Qks1 = qks1,
            FmkarmCorNormalized = fmkarmCorNormalized,
            Fmkarm2Normalized = fmkarm2Normalized,
            FqkarmCorNormalized = fqkarmCorNormalized,
            Qmkm1Normalized = qmkm1Normalized,
            Qmkm2Normalized = qmkm2Normalized,
            CoefNormalized = coefNormalized,
            Pdoksmall = pdoksmallReport,
            DpMax = realTotals[layout.DpMax],
            DpMaxCor = realTotals[layout.DpMaxCor],
            CoefNmax = LargestNonEmptyIndex(integerTotals.Slice(layout.Coef, nc)),
            Qmkm1Nmax = LargestNonEmptyIndex(integerTotals.Slice(layout.Qmkm1, nc)),
            Qmkm2Nmax = LargestNonEmptyIndex(integerTotals.Slice(layout.Qmkm2, nc)),

            DpRow = dpRow,
            Dpockets = dpockets,
            Dokp43 = dokp43,
            Qdokkarm = qdokkarm,
            Qdokso = qdokso,

            ConvergenceEpsy = convergenceEpsy,
            ConvergenceEpsmd3 = convergenceEpsmd3,
            ConvergenceEpsmd4 = convergenceEpsmd4,
            ConvergenceAlldok43 = convergenceAlldok43,
            ConvergenceAlldoksd = convergenceAlldoksd,
            ConvergenceDolM2 = convergenceDolM2,
        };
    }

    /// <summary>Fortran lines 771-784: the seven generator accuracies. Streams 4 and 5 both divide by NFZ (lines 777-780), literally as written.</summary>
    private static void GeneratorAccuracy(
        CyclePlaneRounding rounding, AccumulatorLayout layout, Span<long> integerTotals, Span<double> realTotals,
        out double eps1, out double eps2, out double eps3, out double eps4, out double eps5, out double eps6, out double eps7)
    {
        var nfx = (double)integerTotals[layout.Nfx];
        var nfy = (double)integerTotals[layout.Nfy];
        var nfz = (double)integerTotals[layout.Nfz];
        var nfq = (double)integerTotals[layout.Nfq];
        var nfw = (double)integerTotals[layout.Nfw];

        var xsr0 = rounding.Store(realTotals[layout.Xss + 0] / nfx); // Fortran 771
        eps1 = rounding.Store(Math.Abs((xsr0 - 0.5) / 0.5)); // Fortran 772
        var xsr1 = rounding.Store(realTotals[layout.Xss + 1] / nfx); // Fortran 773
        eps2 = rounding.Store(Math.Abs((xsr1 - 0.5) / 0.5)); // Fortran 774
        var xsr2 = rounding.Store(realTotals[layout.Xss + 2] / nfy); // Fortran 775
        eps3 = rounding.Store(Math.Abs((xsr2 - 0.5) / 0.5)); // Fortran 776
        var xsr3 = rounding.Store(realTotals[layout.Xss + 3] / nfz); // Fortran 777
        eps4 = rounding.Store(Math.Abs((xsr3 - 0.5) / 0.5)); // Fortran 778
        var xsr4 = rounding.Store(realTotals[layout.Xss + 4] / nfz); // Fortran 779
        eps5 = rounding.Store(Math.Abs((xsr4 - 0.5) / 0.5)); // Fortran 780
        var xsr5 = rounding.Store(realTotals[layout.Xss + 5] / nfq); // Fortran 781
        eps6 = rounding.Store(Math.Abs((xsr5 - 0.5) / 0.5)); // Fortran 782
        var xsr6 = rounding.Store(realTotals[layout.Xss + 6] / nfw); // Fortran 783
        eps7 = rounding.Store(Math.Abs((xsr6 - 0.5) / 0.5)); // Fortran 784
    }

    /// <summary>Fortran lines 789-819. <paramref name="fmdok"/> (Ndok + 1, FMDOK) is threaded to <see cref="Matrix"/>: it is never printed and not part of the report (BOOT.md, "## Report" does not list it).</summary>
    private static void OxidizerSizes(
        CyclePlaneRounding rounding, CyclePlaneOrder order, in ModelSetup setup, SetupTables tables, SetupEchoes echoes,
        AccumulatorLayout layout, Span<long> integerTotals, Span<double> realTotals, double epsDok,
        out double dok43b, out double dok43s, out double[] epsalldok, out double[] allvdokso, out double alldok432,
        out double alldok43, out double alldoksd, out double epsx1, out double epsx2, out double epsx3,
        out bool warning, out double[] fmdok)
    {
        var ndok = setup.Ndok;
        var di = setup.CellSize;

        dok43b = rounding.Store(realTotals[layout.Sd4] / realTotals[layout.Sd3]); // Fortran 789
        dok43s = rounding.Store(realTotals[layout.D41] / realTotals[layout.D31]); // Fortran 790

        var alldokq = 0.0;
        for (var i = 0; i < ndok; i++)
        {
            alldokq += integerTotals[layout.Alldok + i];
        }

        epsalldok = new double[setup.FractionCount];
        for (var k = 0; k < setup.FractionCount; k++)
        {
            // 795's quotient is a register value, never stored: 796's own store rounds it.
            var fraction = integerTotals[layout.AlldokFract + k] / alldokq;
            epsalldok[k] = rounding.Store(Math.Abs((fraction - tables.Share[k]) / tables.Share[k])); // Fortran 796
        }

        // 798: the sum stays in the register; 804 divides by it unrounded.
        var allvdoks = 0.0;
        for (var i = 0; i < ndok; i++)
        {
            allvdoks += realTotals[layout.Allvdok + i];
        }

        allvdokso = new double[ndok];
        fmdok = new double[ndok + 1];
        alldok432 = 0.0;
        var alldok243 = 0.0;
        var fmdokRegister = 0.0;
        fmdok[0] = 0.0;
        var schedule = new UnrolledSchedule(ndok, 6);
        for (var k = 0; k < ndok; k++)
        {
            var pass = k + 1;
            var quotient = realTotals[layout.Allvdok + k] / allvdoks;
            allvdokso[k] = rounding.Store(quotient); // Fortran 804
            fmdokRegister = (schedule.ReloadsBefore(pass) ? fmdok[k] : fmdokRegister) + quotient;
            fmdok[k + 1] = rounding.Store(fmdokRegister); // Fortran 808
            alldok432 += order.Linear(quotient, k + 0.5, di); // Fortran 805
            if (schedule.RoundsAfter(pass))
            {
                alldok432 = rounding.Store(alldok432); // Fortran 805
            }

            alldok243 += order.Quadratic(quotient, (k + 0.5) * di); // Fortran 806: a register sum, never stored
        }

        alldok43 = rounding.Store((realTotals[layout.DokBase41] + realTotals[layout.DokSur41]) / (realTotals[layout.DokBase31] + realTotals[layout.DokSur31])); // Fortran 810
        alldoksd = rounding.Store(alldok243 - alldok432 * alldok432); // Fortran 812

        var dokm = echoes.Dokm;
        epsx1 = rounding.Store(Math.Abs((dokm - realTotals[layout.DokBase41] / realTotals[layout.DokBase31]) / dokm)); // Fortran 814
        epsx2 = rounding.Store(Math.Abs((dokm - realTotals[layout.DokSur41] / realTotals[layout.DokSur31]) / dokm)); // Fortran 815
        epsx3 = rounding.Store(Math.Abs((dokm - alldok43) / dokm)); // Fortran 816
        warning = epsx3 > epsDok; // 817 compares the reloaded home
    }

    /// <summary>Fortran lines 963-996: this cycle's own normalized pocket histogram (a fresh <c>QKS/QKSS</c>, distinct from the live QKS1 Particle uses mid-cycle, BOOT.md "Line map") and the pocket-size moments. <c>D43</c> (line 979) is never printed and not ported.</summary>
    private static void Pockets(
        CyclePlaneRounding rounding, CyclePlaneOrder order, in ModelSetup setup, AccumulatorLayout layout, Span<long> integerTotals, Span<double> realTotals, double epsDok,
        out long qkss, out double epsy, out double epsmd4, out double epsmd3, out bool warning,
        out double[] vkso, out double d432, out double dqkarm, out double dp43, out double sdevp43, out double[] qks1)
    {
        var nkarm = setup.Nkarm;
        var di = setup.CellSize;

        var qkssTotal = 0L;
        for (var k = 0; k < nkarm; k++)
        {
            qkssTotal += integerTotals[layout.Qks + k];
        }

        qkss = qkssTotal;
        qks1 = new double[nkarm];
        for (var k = 0; k < nkarm; k++)
        {
            // No guard on QKSS = 0 (BOOT.md, "IEEE results where the original computes
            // them ... No guard is added"): both operands are 0 here, so this is 0.0/0.0
            // = NaN, the same IEEE result the original's unguarded division gives.
            qks1[k] = rounding.Input(integerTotals[layout.Qks + k] / (double)qkssTotal); // Fortran 718
        }

        var md4 = 0.0;
        var md3 = 0.0;
        for (var k = 0; k < nkarm; k++)
        {
            var cell = (k + 0.5) * di;
            md4 += order.Fourth(cell) * qks1[k]; // Fortran 968
            md3 += order.Cube(cell) * qks1[k]; // Fortran 969
        }

        var dd4 = 0.0;
        var dd3 = 0.0;
        for (var k = 0; k < nkarm; k++)
        {
            var cell = (k + 0.5) * di;
            dd4 += order.Square(order.Fourth(cell) - md4) * qks1[k]; // Fortran 972
            dd3 += order.Square(order.Cube(cell) - md3) * qks1[k]; // Fortran 973
        }

        var qkssInverse = rounding.Temporary(1.0 / qkss); // Fortran 975
        epsy = rounding.Store(3.0 * Math.Sqrt(qkssInverse * (dd4 / (md4 * md4) + dd3 / (md3 * md3)))); // Fortran 975
        warning = epsy > epsDok; // 976 compares the stored home
        epsmd4 = rounding.Store(3.0 / md4 * Math.Sqrt(qkssInverse * dd4)); // Fortran 980
        epsmd3 = rounding.Store(3.0 / md3 * Math.Sqrt(qkssInverse * dd3)); // Fortran 981

        var vvks = 0.0;
        for (var k = 0; k < nkarm; k++)
        {
            vvks += realTotals[layout.Vks + k];
        }

        vkso = new double[nkarm];
        d432 = 0.0;
        var d243 = 0.0;
        dqkarm = 0.0;
        var schedule = new UnrolledSchedule(nkarm, 6);
        for (var k = 0; k < nkarm; k++)
        {
            var pass = k + 1;
            // 988 stores the quotient to a temporary and reloads it before storing it to the array,
            // so 989 and 990 read the rounded value from the register.
            var spilled = rounding.Temporary(realTotals[layout.Vks + k] / vvks); // Fortran 988
            vkso[k] = rounding.Store(spilled); // Fortran 988
            d432 += order.Linear(vkso[k], di, k + 0.5); // Fortran 989
            dqkarm += order.Linear(qks1[k], di, k + 0.5); // Fortran 991
            if (schedule.RoundsAfter(pass))
            {
                d432 = rounding.Store(d432); // Fortran 989
                dqkarm = rounding.Store(dqkarm); // Fortran 991
            }

            d243 += order.Quadratic(vkso[k], di * (k + 0.5)); // Fortran 990: a register sum, never stored
        }

        dp43 = rounding.Store(realTotals[layout.Dp41] / realTotals[layout.Dp31]); // Fortran 994
        sdevp43 = rounding.Store(Math.Sqrt(d243 - d432 * d432)); // Fortran 996
    }

    /// <summary>
    /// Fortran lines 1001-1016. <paramref name="ggg"/> is <c>GGG</c> (line 376), already
    /// computed once by <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> and echoed in <c>SetupEchoes</c>;
    /// every call site of this method reads it from there rather than recomputing it
    /// from <paramref name="inputs"/> a second time (one source per value, found by the
    /// audit of this node). <paramref name="inputs"/> carries the generated setup-plane
    /// values this node does not already have through <paramref name="setup"/>/
    /// <paramref name="tables"/> (BOOT.md, "## Setup plane", "Design decision,
    /// 2026-09-24").
    /// </summary>
    private static void Matrix(
        CyclePlaneRounding rounding, in ModelSetup setup, SetupTables tables, SetupInputs inputs, double ggg, double[] fmdok,
        out double gdokleft, out double vdokleft, out double plotsmdok, out double plotsm, out double mp)
    {
        var di = setup.CellSize;
        var plot1 = inputs.OxidizerDensity;
        var plot2 = inputs.PropellantDensity;

        var fmdokIndex = (int)(setup.Dmin / di); // FMDOK(int(Dmin/Di)+1), 1-based; 0-based fmdok[fmdokIndex].
        var gdokleftFirst = rounding.Store(fmdok[fmdokIndex] * ggg); // Fortran 1001

        // 1004: the register sum is added unrounded into 1006.
        var gdoksfr = 0.0;
        for (var i = 0; i < setup.FractionCount; i++)
        {
            if (tables.PocketForming[i] == 0)
            {
                gdoksfr += tables.MassShare[i] * ggg;
            }
        }

        gdokleft = rounding.Store(gdokleftFirst + gdoksfr); // Fortran 1006

        // The compiler's temporaries: T1 and T2 once before the cycle loop, S1 and S2 spilled here.
        var t1 = rounding.Temporary(1.0 / plot2); // Fortran 1008
        var t2 = rounding.Temporary(1.0 - ggg); // Fortran 1010
        var s1 = rounding.Temporary(ggg - gdokleft); // Fortran 1006
        var s2 = rounding.Temporary(t2 + gdokleft); // Fortran 1010
        plotsmdok = rounding.Store((1.0 - s1) / (t1 - s1 / plot1)); // Fortran 1008
        vdokleft = rounding.Store(gdokleft / s2 * plotsmdok / plot1); // Fortran 1010
        plotsm = rounding.Store(t2 / (t1 - rounding.Temporary(ggg / plot1))); // Fortran 1011, 378

        // mp (1014-1016) stays in the register until its one store; T3 and T4 are hoisted temporaries.
        var eta = inputs.AggregatedOxideFraction;
        var t3 = rounding.Temporary(1.0 + rounding.Fold(3.0 * rounding.Literal(0.016)) * eta / (rounding.Fold(2.0 * rounding.Literal(0.027)) + rounding.Fold(3.0 * rounding.Literal(0.016)) * (1.0 - eta))); // Fortran 1015
        var t4 = rounding.Temporary((1.0 - eta) / 2000.0 + eta / 3000.0); // Fortran 1016
        var mpRegister = rounding.Fold(rounding.Literal(3.14159) / 6.0) * plotsmdok * inputs.MetalMassFraction / s2; // Fortran 1014
        mpRegister *= t3; // 1015
        mp = rounding.Store(2.0 * Math.Pow(rounding.Fold(0.75 / rounding.Literal(3.14159)) * mpRegister * t4, rounding.Literal(0.3333))); // Fortran 1016
    }

    /// <summary>Fortran lines 1087-1113 (cycles &gt;= 1 only).</summary>
    private static void CorrectedPockets(
        CyclePlaneRounding rounding, CyclePlaneOrder order, in ModelSetup setup, AccumulatorLayout layout, Span<long> integerTotals, Span<double> realTotals,
        out double dkarm43Cor, out double sdevp43Cor, out double dqkarmCor, out double dfmk432, out double sdevp243,
        out double dqmkm1, out double dqmkm2, out double qmcoef)
    {
        var nkarm = setup.Nkarm;
        var nc = setup.Nc;
        var di = setup.CellSize;

        var fmkarmCorSum = 0.0;
        var fqkarmCorSum = 0L;
        for (var k = 0; k < nkarm; k++)
        {
            fmkarmCorSum += realTotals[layout.FmkarmCor + k];
            fqkarmCorSum += integerTotals[layout.FqkarmCor + k];
        }

        dkarm43Cor = 0.0;
        var dkarm243Cor = 0.0;
        dqkarmCor = 0.0;
        for (var k = 0; k < nkarm; k++)
        {
            var fraction = realTotals[layout.FmkarmCor + k] / fmkarmCorSum;
            dkarm43Cor = rounding.Store(dkarm43Cor + order.Linear(fraction, di, k + 0.5)); // Fortran 1089
            dkarm243Cor += order.Quadratic(fraction, (k + 0.5) * di); // Fortran 1091: a register sum, never stored
            dqkarmCor = rounding.Store(dqkarmCor + order.Linear(integerTotals[layout.FqkarmCor + k] / (double)fqkarmCorSum, di, k + 0.5)); // Fortran 1093
        }

        sdevp43Cor = rounding.Store(Math.Sqrt(dkarm243Cor - dkarm43Cor * dkarm43Cor)); // Fortran 1097

        var fmkarm2Sum = 0.0;
        for (var k = 0; k < nkarm; k++)
        {
            fmkarm2Sum += realTotals[layout.Fmkarm2 + k];
        }

        dfmk432 = 0.0;
        var dfmk243 = 0.0;
        for (var k = 0; k < nkarm; k++)
        {
            var fraction = realTotals[layout.Fmkarm2 + k] / fmkarm2Sum;
            dfmk432 = rounding.Store(dfmk432 + order.Linear(fraction, di, k + 0.5)); // Fortran 1101
            dfmk243 += order.Quadratic(fraction, (k + 0.5) * di); // Fortran 1102: a register sum, never stored
        }

        sdevp243 = rounding.Store(Math.Sqrt(dfmk243 - dfmk432 * dfmk432)); // Fortran 1105

        var qmkm1Sum = 0L;
        var qmkm2Sum = 0L;
        var coefSum = 0L;
        for (var k = 0; k < nc; k++)
        {
            qmkm1Sum += integerTotals[layout.Qmkm1 + k];
            qmkm2Sum += integerTotals[layout.Qmkm2 + k];
            coefSum += integerTotals[layout.Coef + k];
        }

        dqmkm1 = 0.0;
        dqmkm2 = 0.0;
        qmcoef = 0.0;
        for (var k = 0; k < nc; k++)
        {
            dqmkm1 = rounding.Store(dqmkm1 + integerTotals[layout.Qmkm1 + k] / (double)qmkm1Sum * rounding.Literal(0.001) * (k + 0.5)); // Fortran 1110
            dqmkm2 = rounding.Store(dqmkm2 + integerTotals[layout.Qmkm2 + k] / (double)qmkm2Sum * rounding.Literal(0.01) * (k + 0.5)); // Fortran 1111
            qmcoef = rounding.Store(qmcoef + integerTotals[layout.Coef + k] / (double)coefSum * rounding.Literal(0.01) * (k + 0.5)); // Fortran 1112
        }
    }

    /// <summary>Fortran lines 1137-1152 (cycles &gt;= 1 only): in-place division of <c>VdokTotal</c> (per cell) and <c>VdokTotal2</c> (scalar) by <c>1 - gdokleft/GGG</c>, as the original (BOOT.md, "In-place rewrites of the totals").</summary>
    private static void MassFractions(
        CyclePlaneRounding rounding, in ModelSetup setup, AccumulatorLayout layout, Span<double> realTotals,
        double ggg, double plot1, double gdokleft, double plotsmdok, double plotsm, double vdokleft,
        out double dolM1, out double dolM2, out double dolM3)
    {
        var ndok = setup.Ndok;

        var vsmkmSum = 0.0;
        var svdSum = 0.0;
        var vmkmTotalSum = 0.0;
        for (var i = 0; i < ndok; i++)
        {
            vsmkmSum += realTotals[layout.Vsmkm + i];
            svdSum += realTotals[layout.Svd + i];
            vmkmTotalSum += realTotals[layout.VmkmTotal + i];
        }

        var t2 = rounding.Temporary(1.0 - ggg); // Fortran 1008, 1010
        var s1 = rounding.Temporary(ggg - gdokleft); // Fortran 1006, 1008
        var s2 = rounding.Temporary(t2 + gdokleft); // Fortran 1010
        dolM1 = rounding.Store(vsmkmSum * plotsmdok * s1 / (svdSum * plot1 * s2)); // Fortran 1137

        var divisor = rounding.Temporary(1.0 - gdokleft / ggg); // Fortran 1145
        var vdokTotalSum = 0.0;
        for (var i = 0; i < ndok; i++)
        {
            var updated = rounding.Store(realTotals[layout.VdokTotal + i] / divisor); // Fortran 1145
            realTotals[layout.VdokTotal + i] = updated; // in-place rewrite, as the original (BOOT.md).
            vdokTotalSum += updated;
        }

        dolM2 = rounding.Store(vmkmTotalSum * (1.0 - vdokleft) / (t2 * (vdokTotalSum * plot1) / ggg / plotsm)); // Fortran 1146

        // 1150's store is what 1151 reads, and what the next cycle reads.
        var vdokTotal2 = rounding.Store(realTotals[layout.VdokTotal2] / divisor); // Fortran 1150
        realTotals[layout.VdokTotal2] = vdokTotal2;

        dolM3 = rounding.Store(realTotals[layout.VmkmTotal2] * (1.0 - vdokleft) / (t2 * (vdokTotal2 * plot1) / ggg / plotsm)); // Fortran 1151
    }

    private static void Convergence(
        CyclePlaneRounding rounding, SetupEchoes echoes, double epsy, double epsmd3, double epsmd4, double alldok43, double alldoksd, double dolM2,
        out double convergenceEpsy, out double convergenceEpsmd3, out double convergenceEpsmd4,
        out double convergenceAlldok43, out double convergenceAlldoksd, out double convergenceDolM2)
    {
        convergenceEpsy = epsy;
        convergenceEpsmd3 = epsmd3;
        convergenceEpsmd4 = epsmd4;
        convergenceAlldok43 = rounding.Store((alldok43 - echoes.Dokm) / echoes.Dokm); // Fortran 1172
        convergenceAlldoksd = rounding.Store((alldoksd - echoes.Doksd) / echoes.Doksd); // Fortran 1173
        convergenceDolM2 = rounding.Store(1.0 - dolM2); // Fortran 1174
    }

    private static double[] Normalize(double[] values)
    {
        var sum = 0.0;
        foreach (var v in values)
        {
            sum += v;
        }

        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = values[i] / sum;
        }

        return result;
    }

    private static double[] NormalizeIntegers(ReadOnlySpan<long> values)
    {
        var sum = 0L;
        foreach (var v in values)
        {
            sum += v;
        }

        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = values[i] / (double)sum;
        }

        return result;
    }

    private static int LargestNonEmptyIndex(ReadOnlySpan<long> values)
    {
        for (var i = values.Length - 1; i >= 0; i--)
        {
            if (values[i] != 0)
            {
                return i + 1;
            }
        }

        return 0;
    }
}
