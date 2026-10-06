using ILGPU;
using PropStruct.Random;

namespace PropStruct.Particle;

/// <summary>
/// One attempt at a base particle with its surroundings: Fortran label 11 (source line
/// 428) through the acceptance tests after the neighbour loop (lines 719-756), the one
/// place in the tree where the model's per-particle physics lives (BOOT.md, Purpose;
/// "Attempt structure"; "Line map"). Kernel-compatible: a single static method, no
/// allocation, no exception, no recursion, no virtual call.
/// </summary>
internal static class Attempt
{
    // Fortran literal 3.14159 (sphere volume pi/6*D^3, and the cube root of the Poisson
    // volume at line 501), lines 466, 469, 484, 501, 521, 524, 542, 643, 648, 665, 677,
    // 685, 702, 710, 747, 750, 754 (BOOT.md, Constraints).
    private const double SpherePi = 3.14159;

    // Fortran literal 12.56636 (full solid angle), lines 430, 716 (BOOT.md, Constraints).
    private const double FullSolidAngle = 12.56636;

    /// <summary>
    /// Adds <paramref name="term"/> to <paramref name="current"/>, then, when
    /// <paramref name="roundToReal4"/> is set, rounds the new total to binary32 and back --
    /// the original's own REAL*4 accumulation of this cell (root BOOT.md, "Precision kind
    /// is an option of every run"; <c>RealFourAccumulators.generated.txt</c> names exactly the
    /// fields whose write sites pass <see langword="true"/> here, generated from the Fortran
    /// declaration block, never typed). <paramref name="roundToReal4"/> is
    /// <c>setup.Kind == PrecisionKind.Original</c>, computed once per <see cref="Run"/> call
    /// rather than re-tested at every site, since the kind never changes during one attempt;
    /// the ternary evaluates the narrowing cast only when it is actually used, so the default
    /// <see cref="PrecisionKind.Binary64"/> kind pays for nothing beyond the one comparison
    /// already made once above and reproduces the exact bits <see cref="Run"/> always wrote.
    /// </summary>
    private static double AddReal4(double current, double term, bool roundToReal4)
    {
        var sum = current + term;
        return roundToReal4 ? (double)(float)sum : sum;
    }

    /// <summary>
    /// Runs one attempt. Advances <paramref name="streams"/> by exactly the draws the
    /// attempt made; <paramref name="integerTotals"/> (shared, atomic add) and
    /// <paramref name="record"/> (this particle's own) receive the attempt's side
    /// effects whatever the outcome, mirroring the original's side effects of rejected
    /// attempts (BOOT.md, Invariants). <paramref name="scratch"/> is this particle's
    /// own <c>3*Nkarm + 2</c> scratch doubles (<see cref="AccumulatorLayout.ScratchLength"/>).
    /// </summary>
    public static AttemptOutcome Run(
        in ModelSetup setup, in FractionTable fractions, in CycleInputs cycle,
        ref StreamSet streams,
        ArrayView<long> integerTotals,
        ArrayView<double> record,
        ArrayView<double> scratch)
    {
        var layout = setup.Layout;
        var nkarm = setup.Nkarm;
        var roundToReal4 = setup.Kind == PrecisionKind.Original; // AddReal4, computed once (BOOT.md, "Precision kind").

        var fmkarm2Loc = scratch.SubView(0, nkarm);
        var fqks = scratch.SubView(nkarm, nkarm + 1);
        var dpoc = scratch.SubView(nkarm + nkarm + 1, nkarm + 1);

        // label 11 (428-445): reset the attempt's own locals. Real-valued totals in
        // `record` are never reset here: they persist across every attempt of the
        // particle (root BOOT.md, "Deterministic under every schedule").
        var tu = FullSolidAngle;
        for (var i = 0; i < nkarm; i++)
        {
            fmkarm2Loc[i] = 0.0;
        }

        var ipocketLoc = 0;
        var ibridgeLoc = 0;
        var ibridgeLocCor = 0;
        var ibridgeLocCor2 = 0;
        var jammedLoc = 0;
        var vdokLoc = 0.0;
        var vmkmLoc = 0.0;
        var vdokLoc2 = 0.0;
        var vmkmLoc2 = 0.0;
        var svd1 = 0.0;
        var vsmkm1 = 0.0;
        var windowBuilt = false;
        var windowNnn1 = 0;

        // base (448-484)
        var x = Mcg128.Next(ref streams.S1);
        var x0 = Mcg128.Next(ref streams.S2);
        SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative, x, x0, out var dr, out var fractionBase);

        record[layout.Xss + 1] += x;  // XSS1 (462)
        record[layout.Xss + 0] += x0; // XSS0 (461)
        _ = Atomic.Add(ref integerTotals[layout.Nfx], 1L); // NFX (463)

        _ = Atomic.Add(ref integerTotals[layout.AlldokFract + fractionBase], 1L); // ALLDOK_FRACT (465)
        // ALLVDOK_FR not accumulated: its print is commented out (root BOOT.md, exclusion list).
        var iksBase = (int)(dr / setup.CellSize); // 0-based (467)
        _ = Atomic.Add(ref integerTotals[layout.Alldok + iksBase], 1L); // ALLDOK (468)
        record[layout.Allvdok + iksBase] = AddReal4(record[layout.Allvdok + iksBase], SpherePi / 6.0 * dr * dr * dr, roundToReal4); // ALLVDOK (469), REAL*4

        record[layout.DokBase41] += dr * dr * dr * dr; // DOK_base41 (470)
        record[layout.DokBase31] += dr * dr * dr;      // DOK_base31 (471)

        if (fractions.PocketForming[fractionBase] == 0) // SFR(Nfract) == 0 (473)
        {
            return AttemptOutcome.RestartedInsideLoop;
        }

        if (dr >= setup.Dmax) // (474)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 0], 1L); // conditions(1) (475)
            return AttemptOutcome.RestartedInsideLoop; // (476)
        }

        if (dr <= setup.Dmin) // (478)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 1], 1L); // conditions(2) (479)
            return AttemptOutcome.RestartedInsideLoop; // (480)
        }

        record[layout.Sd4] += dr * dr * dr * dr; // Sd4 (482)
        record[layout.Sd3] += dr * dr * dr;      // Sd3 (483)
        var vp = SpherePi / 6.0 * dr * dr * dr;  // VP (484)

        var neighbourDraws = 0;

    Label501:
        neighbourDraws++;
        if (neighbourDraws > setup.NeighbourBudget)
        {
            return AttemptOutcome.NeighbourBudgetExceeded;
        }

        double x1;
        while (true)
        {
            x1 = Mcg128.Next(ref streams.S3); // (488)
            if (x1 != 1.0) // (496): retried from the same stream when exactly 1
            {
                break;
            }
        }

        record[layout.Xss + 2] += x1; // XSS2 (497)
        _ = Atomic.Add(ref integerTotals[layout.Nfy], 1L); // NFY (498)

        var kcil = -(Math.Log(1.0 - x1) * (1.0 / setup.Lambda)) + vp; // KCIL (499)
        vp = kcil; // VP = KCIL (500)
        var rrl = Math.Pow(3.0 * kcil / (4.0 * SpherePi), 1.0 / 3.0); // rrL (501)

        // 345 (503-543)
        var x2 = Mcg128.Next(ref streams.S4);  // (504)
        var x21 = Mcg128.Next(ref streams.S5); // (505)
        SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative, x2, x21, out var db, out var fractionNeighbour);

        record[layout.Xss + 3] += x2;  // XSS3 (516)
        record[layout.Xss + 4] += x21; // XSS4 (517)
        _ = Atomic.Add(ref integerTotals[layout.Nfz], 1L); // NFZ (518)

        _ = Atomic.Add(ref integerTotals[layout.AlldokFract + fractionNeighbour], 1L); // (520)
        var iksNeighbour = (int)(db / setup.CellSize); // 0-based (522)
        _ = Atomic.Add(ref integerTotals[layout.Alldok + iksNeighbour], 1L); // (523)
        record[layout.Allvdok + iksNeighbour] = AddReal4(record[layout.Allvdok + iksNeighbour], SpherePi / 6.0 * db * db * db, roundToReal4); // (524), REAL*4

        record[layout.DokSur41] += db * db * db * db; // (525)
        record[layout.DokSur31] += db * db * db;      // (526)

        if (fractions.PocketForming[fractionNeighbour] == 0) // (528)
        {
            goto Label501;
        }

        // Fortran line 529 ("Dr.ge.Dmax" -> conditions(1), goto 501) omitted: dead code,
        // 474 already restarted the whole attempt on Dr >= Dmax (BOOT.md, Line map).

        if (db <= setup.Dmin) // (533)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 1], 1L); // conditions(2) (534)
            goto Label501; // (535)
        }

        record[layout.D41] += db * db * db * db; // (537)
        record[layout.D31] += db * db * db;      // (538)

        var aa = rrl - dr / 2.0 - db / 2.0; // AA (539)

        // idok_local_all (541) and qdokstr (543) never read, not ported (BOOT.md, Line map).
        record[layout.Vdokstr + iksNeighbour] = AddReal4(record[layout.Vdokstr + iksNeighbour], SpherePi / 6.0 * db * db * db, roundToReal4); // vdokstr (542), REAL*4

        var maxDrDb = Math.Max(dr, db);

        if (dr < setup.Ak1 * db) // (547)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 2], 1L); // conditions(3) (548)
            if (setup.Variant == 0)
            {
                return AttemptOutcome.RestartedInsideLoop; // (549)
            }

            goto Label501; // (550)
        }

        if (dr > setup.Ak2 * db) // (552)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 3], 1L); // conditions(4) (553)
            goto Label501; // (554)
        }

        // 551 (559-576)
        var coefIndex = (int)((aa / maxDrDb + 1.0) * 100.0) + 1; // (560)
        if (coefIndex <= setup.Nc)
        {
            _ = Atomic.Add(ref integerTotals[layout.Coef + coefIndex - 1], 1L); // (562)
        }

        if (aa > maxDrDb * setup.Ak4) // (566)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 4], 1L); // conditions(5) (567)
            return AttemptOutcome.RestartedInsideLoop; // (568)
        }

        if (aa > maxDrDb * setup.Ak3) // (570)
        {
            goto Label502;
        }

        if (aa <= 0.0) // (572)
        {
            rrl = dr / 2.0 + db / 2.0; // (573)
            aa = 0.0;                  // (574)
            jammedLoc++;                // (575)
        }

        ibridgeLoc++; // (580)

        if (cycle.CycleFlag == 0) // (581)
        {
            goto Label550;
        }

        if (!windowBuilt)
        {
            if (!BridgeWindow.Build(dr, setup.Ak3, setup.Ak4, setup.CellSize, nkarm, cycle.Qks1, fqks, dpoc, out windowNnn1))
            {
                return AttemptOutcome.RestartedInsideLoop; // AUS == 0 (590)
            }

            windowBuilt = true;
        }

        var vmkm = 0.0;
        var bb = 0.0;
        var jj = false;
        var bridgeDraws = 0;

        while (!jj)
        {
            bridgeDraws++;
            if (bridgeDraws > setup.PocketRedrawBudget)
            {
                return AttemptOutcome.BridgeDrawBudgetExceeded;
            }

            var x3 = Mcg128.Next(ref streams.S6); // (613)
            record[layout.Xss + 5] += x3;         // XSS5 (621)
            _ = Atomic.Add(ref integerTotals[layout.Nfq], 1L); // NFQ (622)


            if (!BridgeWindow.SamplePocket(x3, fqks, dpoc, windowNnn1, out var dkarm)) // DM (623)
            {
                return AttemptOutcome.IndexOutOfRange;
            }

            jj = BridgeGeometry.Volume(dr / 2.0, db / 2.0, dkarm / 2.0, aa, out vmkm, out bb); // VM (624)
        }

        if (aa > 0.0) // (627)
        {
            var qmkm1Index = (int)(aa / maxDrDb * 1000.0) + 1; // (628)
            if (qmkm1Index <= setup.Nc)
            {
                _ = Atomic.Add(ref integerTotals[layout.Qmkm1 + qmkm1Index - 1], 1L); // (630)
            }
        }

        var qmkm2Index = (int)(bb / maxDrDb * 100.0) + 1; // (635)
        if (qmkm2Index <= setup.Nc)
        {
            _ = Atomic.Add(ref integerTotals[layout.Qmkm2 + qmkm2Index - 1], 1L); // (637)
        }

        vsmkm1 = AddReal4(vsmkm1, vmkm, roundToReal4); // VSMKM1 (642), REAL*4
        svd1 = AddReal4(svd1, SpherePi / 6.0 * db * db * db, roundToReal4); // SVD1 (643), REAL*4

        if (maxDrDb <= cycle.Dmaxxx) // (646)
        {
            ibridgeLocCor++;                             // (647)
            vdokLoc = AddReal4(vdokLoc, SpherePi / 6.0 * db * db * db, roundToReal4); // Vdok_loc (648), REAL*4
            vmkmLoc = AddReal4(vmkmLoc, vmkm, roundToReal4); // Vmkm_loc (649), REAL*4
        }

        var x4Bridge = Mcg128.Next(ref streams.S6); // (653)
        record[layout.Xss + 6] += x4Bridge;         // XSS6 (661)
        _ = Atomic.Add(ref integerTotals[layout.Nfw], 1L); // NFW (662)

        var pdoksmallIndexBridge = (int)(maxDrDb / setup.CellSize); // 0-based (663)
        if (x4Bridge < setup.BridgeCoefficient / setup.PocketCoefficient * cycle.Pdoksmall[pdoksmallIndexBridge]) // (663)
        {
            goto Label550;
        }

        ibridgeLocCor2++;                            // (664)
        vdokLoc2 = AddReal4(vdokLoc2, SpherePi / 6.0 * db * db * db, roundToReal4); // Vdok_loc2 (665), REAL*4
        vmkmLoc2 = AddReal4(vmkmLoc2, vmkm, roundToReal4); // Vmkm_loc2 (666), REAL*4
        goto Label550; // (667)

    Label502:
        ipocketLoc++;   // (671)
        var rk = aa;    // RK = AA (672)

        if (rk > record[layout.DpMax]) // DPmax (673)
        {
            record[layout.DpMax] = rk;
        }

        if (rk <= 0.0) // (674): unreachable under AK3 > 0, kept as a literal transcription (BOOT.md, Constraints)
        {
            goto Label333;
        }

        var pocketIndex = (int)(rk / setup.CellSize); // 0-based (675)
        _ = Atomic.Add(ref integerTotals[layout.Qks + pocketIndex], 1L); // QKS (676)
        record[layout.Vks + pocketIndex] += SpherePi / 6.0 * rk * rk * rk; // VKS (677)
        record[layout.Dp31] += rk * rk * rk;         // (678)
        record[layout.Dp41] += rk * rk * rk * rk;    // (679)

        if (cycle.CycleFlag == 0) // (680)
        {
            goto Label333;
        }

        var pdoksmallIndexPocket = (int)(maxDrDb / setup.CellSize); // 0-based (683)
        if (cycle.Pdoksmall[pdoksmallIndexPocket] >= 1.0) // (683)
        {
            goto Label333;
        }

        if (rk > record[layout.DpMaxCor]) // (684)
        {
            record[layout.DpMaxCor] = rk;
        }

        record[layout.FmkarmCor + pocketIndex] = AddReal4(record[layout.FmkarmCor + pocketIndex], SpherePi / 6.0 * rk * rk * rk, roundToReal4); // (685), REAL*4
        _ = Atomic.Add(ref integerTotals[layout.FqkarmCor + pocketIndex], 1L);       // (686)

        var x4Pocket = Mcg128.Next(ref streams.S6); // (689)
        record[layout.Xss + 6] += x4Pocket;         // XSS6 (697)
        _ = Atomic.Add(ref integerTotals[layout.Nfw], 1L); // NFW (698)

        if (x4Pocket < cycle.Pdoksmall[pdoksmallIndexPocket]) // (699)
        {
            goto Label333;
        }

        if (rk > record[layout.DpMaxCor]) // (700)
        {
            record[layout.DpMaxCor] = rk;
        }

        // ipocket_loc_cor (701) is never read anywhere in the original, not ported (BOOT.md, Line map).
        fmkarm2Loc[pocketIndex] = AddReal4(fmkarm2Loc[pocketIndex], SpherePi / 6.0 * rk * rk * rk, roundToReal4); // (702), REAL*4

    Label333:
        var dpRow = (int)(rk / setup.CategoryStep) + 1; // DPRow, 1-based (705)
        // QDOK (706) never read, its print is commented out, not ported (root BOOT.md, exclusion list).
        record[layout.Dokp41 + (dpRow - 1)] = AddReal4(record[layout.Dokp41 + (dpRow - 1)], maxDrDb * maxDrDb * maxDrDb * maxDrDb, roundToReal4); // (707), REAL*4
        record[layout.Dokp31 + (dpRow - 1)] = AddReal4(record[layout.Dokp31 + (dpRow - 1)], maxDrDb * maxDrDb * maxDrDb, roundToReal4);           // (708), REAL*4

        var oxidizerSizeIndex = (int)(maxDrDb / setup.CellSize); // 0-based (709)
        // VDOKS (710) not accumulated: nothing prints it (declared, BOOT.md, "Decisions where the port departs from a transcription").
        _ = Atomic.Add(ref integerTotals[layout.Qdoks + (dpRow - 1) * setup.Ndok + oxidizerSizeIndex], 1L); // QDOKS (711)

    Label550:
        tu -= db * db / (rrl * rrl); // (715)
        if (tu > FullSolidAngle / 200.0) // (716)
        {
            goto Label501;
        }

        // (717-718): the refresh itself is PocketHistogram.Normalize, called by the driver
        // (BOOT.md, "Frozen inputs"). LoopCompletions counts every reachable refresh point.
        _ = Atomic.Add(ref integerTotals[layout.LoopCompletions], 1L);

        if (cycle.CycleFlag == 0) // IPRIS == 0 (719): cycle 0 always accepts here.
        {
            return AttemptOutcome.Accepted;
        }

        for (var i = 0; i < nkarm; i++)
        {
            record[layout.Fmkarm2 + i] = AddReal4(record[layout.Fmkarm2 + i], fmkarm2Loc[i], roundToReal4); // fmkarm2 += fmkarm2_loc (720), REAL*4
        }

        var nn = (double)ipocketLoc / ibridgeLoc; // (722): IEEE division, may be +Inf or NaN
        var ireset = 0;

        if (ipocketLoc == 0) // (723)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 5], 1L); // conditions(6) (724)
            ireset++; // (725)
        }

        if (ibridgeLoc < 2) // (727)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 6], 1L); // conditions(7) (728)
            ireset++; // (729)
        }

        if (nn <= setup.NnMin) // (731)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 7], 1L); // conditions(8) (732)
            ireset++; // (733)
        }

        if (nn >= setup.NnMax) // (735)
        {
            _ = Atomic.Add(ref integerTotals[layout.Conditions + 8], 1L); // conditions(9) (736)
            ireset++; // (737)
        }

        if (ireset > 0) // (739)
        {
            return AttemptOutcome.RestartedAfterLoop;
        }

        record[layout.JammedTotal] = AddReal4(record[layout.JammedTotal], (double)jammedLoc / (ipocketLoc + ibridgeLoc), roundToReal4); // (741-742), REAL*4
        record[layout.NnTotal] = AddReal4(record[layout.NnTotal], nn, roundToReal4); // (743), REAL*4
        _ = Atomic.Add(ref integerTotals[layout.IbridgeTotal], ibridgeLoc); // (744)

        var drIndex = (int)(dr / setup.CellSize); // 0-based (745)
        record[layout.Vsmkm + drIndex] = AddReal4(record[layout.Vsmkm + drIndex], vsmkm1, roundToReal4); // (746), REAL*4
        record[layout.Svd + drIndex] = AddReal4(record[layout.Svd + drIndex], setup.Alpha * svd1 + SpherePi / 6.0 * dr * dr * dr, roundToReal4); // (747), REAL*4

        if (ibridgeLocCor > 0) // (748)
        {
            record[layout.VmkmTotal + drIndex] = AddReal4(record[layout.VmkmTotal + drIndex], vmkmLoc, roundToReal4); // (749), REAL*4
            record[layout.VdokTotal + drIndex] = AddReal4(record[layout.VdokTotal + drIndex], setup.Alpha * vdokLoc + SpherePi / 6.0 * dr * dr * dr, roundToReal4); // (750), REAL*4
        }

        if (ibridgeLocCor2 > 0) // (752)
        {
            record[layout.VmkmTotal2] = AddReal4(record[layout.VmkmTotal2], vmkmLoc2, roundToReal4); // (753), REAL*4
            record[layout.VdokTotal2] = AddReal4(record[layout.VdokTotal2], setup.Alpha * vdokLoc2 + SpherePi / 6.0 * dr * dr * dr, roundToReal4); // (754), REAL*4
        }

        return AttemptOutcome.Accepted;
    }
}
