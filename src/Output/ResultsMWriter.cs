using PropStruct.Input;
using PropStruct.Simulation;

namespace PropStruct.Output;

/// <summary>
/// Writes a <see cref="SimulationResult"/> as <c>results.m</c> in the original's layout: every print-time
/// expression of Fortran lines 1184-1453 (<c>tests/Fixtures/Legacy/PropStructv3.for.txt</c>), including
/// <c>arrayprint</c>/<c>arrayprint2</c> (lines 1517-1571), transcribed as written or read from the result
/// that computes it (BOOT.md, "## Print-time expressions"; the §6 deviation is declared in BOOT.md,
/// "## Constraints").
/// </summary>
public static class ResultsMWriter
{
    /// <summary>
    /// Writes <paramref name="result"/> to <paramref name="path"/>. The header needs the formulation and the
    /// model parameters of the run in addition to the result (BOOT.md, "## Dependencies": <c>Input</c> -
    /// "formulation and parameters for the header"); this is a real signature, not the <c>API.md</c> sketch's
    /// <c>Write(SimulationResult, string)</c> (this node's <c>API.md</c>, "## Writers", the dated correction
    /// there explains why).
    /// </summary>
    public static void Write(Formulation formulation, ModelParameters parameters, SimulationResult result, string path)
    {
        using var writer = new StreamWriter(path, append: false);
        Write(formulation, parameters, result, writer);
    }

    /// <summary>Writes <paramref name="result"/> to <paramref name="writer"/>. See the path overload.</summary>
    public static void Write(Formulation formulation, ModelParameters parameters, SimulationResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(formulation);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        WriteHeader(writer, formulation, parameters, result.StoredSetup, result.Header.TailProbabilityModified);
        WriteResults(writer, result);
        WriteFunctions(writer, result);
    }

    // ---- primitive composition helpers -------------------------------------------------------------------

    private static void Line(TextWriter w, string content) => w.Write(content + "\r\n");

    private static void Blank(TextWriter w) => Line(w, " ");

    private static string R(double value) => FortranFormat.SingleRealField(value);

    private static string RFirst(double value) => FortranFormat.SingleRealBeforeAdjacent(value);

    private static string RAdj(double value) => FortranFormat.SingleRealAdjacent(value);

    private static string D(double value) => FortranFormat.DoubleRealField(value);

    private static string I4(long value) => FortranFormat.IntegerField(value, FortranFormat.Integer4Width);

    private static string I8(long value) => FortranFormat.IntegerField(value, FortranFormat.Integer8Width);

    private static string I2Field(long value) => FortranFormat.IntegerField(value, FortranFormat.Integer2Width);

    private static string F(double value, int decimals, int width) => FortranFormat.FixedPoint(value, decimals, width);

    private static long Trunc(double value) => FortranFormat.TruncateToInt64(value);

    // ---- header (Fortran lines 1184-1227) ----------------------------------------------------------------

    /// <summary>
    /// Every setup-plane header echo (BOOT.md, "## Design decision (2026-09-24): print the stored setup")
    /// is read from <paramref name="storedSetup"/>, never from <paramref name="formulation"/> or
    /// <paramref name="parameters"/>: under <see cref="PrecisionKind.Original"/> that is the run's own
    /// binary32 store of each value, the same one <c>Statistics</c> used, not a second, unrounded copy of
    /// it. <paramref name="formulation"/>/<paramref name="parameters"/> still supply the header fields that
    /// are not part of the setup plane at all — the name, the fraction/formulation counts (<c>Nfr</c>,
    /// <c>JZ</c>, <c>Cycles</c>, <c>N</c>), the calculation variant (<c>ivar</c>, an <c>INTEGER</c>) and
    /// the pocket-forming flags (<c>sfr</c>, also <c>INTEGER</c>).
    /// </summary>
    private static void WriteHeader(
        TextWriter w, Formulation formulation, ModelParameters parameters, StoredSetup storedSetup, double tailProbabilityModified)
    {
        Line(w, " clear fqdokkarm");
        Line(w, " % ====================================================");
        Line(w, " % Input filename: " + formulation.Name + ".dat");
        Line(w, " % ====================================================");

        // 925 format(1x,a,F6.1,a,F6.1,a,F5.3,a,F5.3,a)
        Line(w,
            " % Plot1 = " + F(storedSetup.OxidizerDensity, 1, 6) +
            "; Plot2 = " + F(storedSetup.PropellantDensity, 1, 6) +
            "; Gdok = " + F(storedSetup.OxidizerMassFraction, 3, 5) +
            "; Gm = " + F(storedSetup.MetalMassFraction, 3, 5) + ";");

        var nmm = formulation.Fractions.Length;

        // 926 format(1x,a,I3,a,I2,a,I3,a,I7\) then, since GSV is always 2 (root BOOT.md, "Not goals of
        // version 1"): write(4,'(a,I2,a)')'; GSV =',gsv,';'
        Line(w,
            " % Nfr =" + FortranFormat.IntegerField(nmm, 3) +
            "; JZ =" + FortranFormat.IntegerField(formulation.SizeLawCode, 2) +
            "; Cycles =" + FortranFormat.IntegerField(formulation.Cycles, 3) +
            "; N =" + FortranFormat.IntegerField(formulation.ParticlesPerCycle, 7) +
            "; GSV = 2;");

        WriteArray(w, "Gfr", storedSetup.FractionMassShares.AsSpan());
        WriteArray(w, "Dfr", storedSetup.FractionBounds.AsSpan()); // Fortran 1202

        Line(w, " %_____________________________________________________");
        Line(w, "  Dmin =" + I4(Trunc(storedSetup.Dmin * 1.00001e6)) + " ; % mkm"); // Fortran 1204
        Line(w, "  Di =" + I4(Trunc(storedSetup.CellSize * 1.00001e6)) + " ; % mkm"); // Fortran 1205
        Line(w, "  Dj =" + I4(Trunc(storedSetup.CategoryStep * 1.00001e6)) + " ; % mkm"); // Fortran 1206
        Line(w, " % Nkarm/Nmkm(min, max) = [" + RFirst(storedSetup.NnMin) + RAdj(storedSetup.NnMax) + " ];");
        Line(w, " % k5 =" + R(storedSetup.Alpha) + " ;");
        Line(w, " % Statistical significance P(alpha)=" + D(tailProbabilityModified) + " ;");
        Line(w, " % eps =" + R(storedSetup.EpsDok) + " ;");
        Line(w, " % Calculation variant =" + I4(parameters.Variant) + " ;");
        Line(w, " % P(karm-in-karm) coef =" + R(storedSetup.PocketCoefficient) + " ;");
        Line(w, " % P(karm-in-MKM) coef =" + R(storedSetup.BridgeCoefficient) + " ;");
        Line(w, " % Zok* =" + R(storedSetup.HomogenizedOxidizerFraction) + " ;");

        if (formulation.PocketFormingFractions is { } sfr && sfr.Any(v => v == 0)) // Fortran 1216
        {
            Line(w, " % Fractions used to form pockets:");
            var asReal = new double[sfr.Length];
            for (var i = 0; i < sfr.Length; i++)
            {
                asReal[i] = sfr[i]; // Fortran 1218
            }

            WriteArray(w, "sfr", asReal);
        }

        Blank(w);
        Blank(w);
        Line(w, " % ====================================================");
        Line(w, " %\t\t\t\tCalculation\tResults                  ");
        Line(w, " % ====================================================");
        Blank(w);
    }

    // ---- results (Fortran lines 1228-1337) ---------------------------------------------------------------

    private static void WriteResults(TextWriter w, SimulationResult result)
    {
        var header = result.Header;
        var counters = result.Counters;
        var accuracy = result.GeneratorAccuracy;
        var sizes = result.ParticleSizes;
        var local = result.LocalStructure;
        var pockets = result.Pockets;
        var mass = result.MassFractions;
        var agglomerates = result.Agglomerates;

        var fi = (double)header.Cycles * header.ParticlesPerCycle;
        var nbase = header.ParticlesPerCycle;

        Line(w, " % Cycles:" + I4(header.Cycles));
        Line(w, " % Total basic particles number:");
        Line(w, " Nbase =" + I4(nbase) + " +" + I4((long)fi) + " ;");
        Line(w, " % Total defined pockets number:");
        Line(w, " Nkarm =" + I8(counters.Qkss) + " ;");
        Line(w, " % Total calls of random generator:");
        Line(w, " NFX =" + I8(counters.Nfx) + " ;  NFY =" + I8(counters.Nfy) + " ;");
        Line(w, " NFQ =" + I8(counters.Nfq) + " ;  NFW =" + I8(counters.Nfw) + " ;");
        Line(w, " % Accuracy of random numbers modeling :");
        Line(w, " epsx(1)=" + R(accuracy.Eps1) + " ; epsx(2)=" + R(accuracy.Eps2) + " ; epsx(3)=" + R(accuracy.Eps3) + " ;");
        Line(w, " epsx(4)=" + R(accuracy.Eps4) + " ; epsx(5)=" + R(accuracy.Eps6) + " ; epsx(6)=" + R(accuracy.Eps7) + " ;");
        Blank(w);
        Line(w, " %_______________Local structure analysis______________");
        Line(w, " % Medium coef [(Lij/Ddok)+1]:" + R(agglomerates.Qmcoef) + " ;");
        Line(w, " % Medium number of bridges:" + R(counters.IbridgeTotal / fi)); // Fortran 1242
        Line(w, " % Medium ratio between number of pockets and bridges :" + R(counters.NnTotal / fi)); // Fortran 1243
        Line(w, " % Medium fraction of paricles number in jammed pack:" + R(counters.JammedTotal / fi)); // Fortran 1245
        Line(w, " % Medium number of conditions breakings:");

        var conditions = counters.Conditions;
        var fiPlusN = fi + nbase; // Fortran 1248, 1249, 1250, 1251, 1252
        Line(w, " %    1) Dok > Dmax      :" + R(conditions[0] / fiPlusN)); // Fortran 1248
        Line(w, " %    2) Dok < Dmin      :" + R(conditions[1] / fiPlusN)); // Fortran 1249
        Line(w, " %    3) Dbase < 0.5Dok  :" + R(conditions[2] / fiPlusN)); // Fortran 1250
        Line(w, " %    4) Dbase > 2.0Dok  :" + R(conditions[3] / fiPlusN)); // Fortran 1251
        Line(w, " %    5) l > 4.7Dbase    :" + R(conditions[4] / fiPlusN)); // Fortran 1252
        Line(w, " %    6) Nkarm = 0       :" + R(conditions[5] / fi)); // Fortran 1253
        Line(w, " %    7) Nmkm < 2        :" + R(conditions[6] / fi)); // Fortran 1254
        Line(w, " %    8) Nkarm/Nmkm < min:" + R(conditions[7] / fi)); // Fortran 1255
        Line(w, " %    9) Nkarm/Nmkm > max:" + R(conditions[8] / fi)); // Fortran 1256
        Blank(w);
        Line(w, " %___________________Dok parameters____________________");
        Line(w, " % Mass-medium diameter of all Dok particles, mkm:");
        Line(w, " Dok43a = " + F(header.Dokm * 1e6, 2, 7) + "; %(analytical calculation, all particles)"); // Fortran 1261
        Line(w, " Dok43all = [" + F(sizes.Alldok43 * 1e6, 2, 7) + " " + F(sizes.Alldok432 * 1e6, 2, 7) + "]; %(all particles, 2 variants)"); // Fortran 1263
        Line(w, " Dok43(1) = " + F(sizes.Dok43b * 1e6, 2, 7) + "; %(basic only)"); // Fortran 1265
        Line(w, " Dok43(2) = " + F(sizes.Dok43s * 1e6, 2, 7) + "; %(surrounding only)"); // Fortran 1266
        Line(w, " % Accuracy of all Dok particles sizes modeling:");
        Line(w, " epsalldok =" + R(accuracy.Epsx3) + " ;");
        Line(w, " % Accuracy of basic and surrounding Dok particles sizes modeling:");
        Line(w, " epsdok(1) =" + R(accuracy.Epsx1) + " ; epsdok(2) =" + R(accuracy.Epsx2) + " ;");

        if (header.TailProbabilityModified > 0) // Fortran 1272
        {
            Line(w, " Dokb_max = " + F(header.Dmax * 1e6, 2, 7) + "; %(maximum size of basic particles)"); // Fortran 1273
        }

        Line(w, " Ddok_max = " + F(header.Ddokmax * 1e6, 2, 7) + "; %(maximum size of all particles)"); // Fortran 1276
        Line(w, " % Standard deviation of all Dok particles, mkm:");
        Line(w, " Dok43sd = " + F(Math.Sqrt(header.Doksd) * 1e6, 2, 7) + "; %(analytical calculation, all particles)"); // Fortran 1279
        Line(w, " % Accuracy of Dok fractions distribution function modeling:");
        WriteArray(w, "epsdokfr", accuracy.Epsalldok.AsSpan());
        Line(w, " % Total fraction of homogenized oxidizer:");
        Line(w, " fineoxy_fr =" + R(result.StoredSetup.HomogenizedOxidizerFraction + local.Gdokleft / header.Ggg) + " ;"); // Fortran 1286
        Blank(w);
        Line(w, " %________________Pockets parameters___________________");
        Line(w, " % Accuracy of pockets distribution function modeling:");
        Line(w, " epsfkarm =" + R(accuracy.Epsy) + " ;");
        Line(w, " % Accuracy of pockets distribution moments determination (4-th and 3-th):");
        Line(w, " epsmkarm(1)=" + R(accuracy.Epsmd4) + " ; epsmkarm(2)=" + R(accuracy.Epsmd3) + " ;");
        Line(w, " % Mass-medium size of pockets (var. #1), mkm:");
        Line(w, " Dkarm43(1) = " + F(pockets.Dp43 * 1e6, 2, 7) + ";"); // Fortran 1296
        Line(w, " % Mass-medium size of pockets (var. #2)and standard deviation, mkm:");
        Line(w,
            " Dkarm43(2) = " + F(pockets.D432 * 1e6, 2, 7) + // Fortran 1300
            "; Dkarm43sd = " + F(pockets.Sdevp43 * 1e6, 2, 7) + // Fortran 1300
            "; %(" + F(pockets.Sdevp43 / pockets.D432, 3, 6) + " relative)"); // Fortran 1300
        Line(w, " % Cor. mass-medium size of pockets and standard deviation (cor. var. #1), mkm:");
        Line(w,
            " Dkarm43_cor(1) = " + F(pockets.Dkarm43Cor * 1e6, 2, 7) + // Fortran 1304
            "; Dkarm43sd_cor(1) =" + F(pockets.Sdevp43Cor * 1e6, 2, 7) + // Fortran 1304
            "; %(" + F(pockets.Sdevp43Cor / pockets.Dkarm43Cor, 3, 6) + " relative)"); // Fortran 1304
        Line(w, " % Cor. mass-medium size of pockets and standard deviation (cor. var. #2), mkm:");
        Line(w,
            " Dkarm43_cor(2) = " + F(pockets.Dfmk432 * 1e6, 2, 7) + // Fortran 1308
            "; Dkarm43sd_cor(2) =" + F(pockets.Sdevp243 * 1e6, 2, 7) + // Fortran 1308
            "; %(" + F(pockets.Sdevp243 / pockets.Dfmk432, 3, 6) + " relative)"); // Fortran 1308
        Line(w, " % Medium size of pockets, mkm:");
        Line(w, " Dkarm10 = " + F(pockets.Dqkarm * 1e6, 2, 7) + ";"); // Fortran 1311
        Line(w, " % Cor. medium size of pockets (cor. var. #1), mkm:");
        Line(w, " Dkarm10_cor = " + F(pockets.DqkarmCor * 1e6, 2, 7) + ";"); // Fortran 1313
        Blank(w);
        Line(w, " % <Zkarm>");
        Line(w, " % Mass fraction of pockets in <binder-metal> composition (not corrected):");
        Line(w, " Zkarm = " + R(1.0 - mass.DolM1) + "  ;"); // Fortran 1318
        Line(w, " % Cor. (var. #1, #2) mass fraction of pockets  in <binder-metal> composition:");
        Line(w, " Zkarm_cor = [" + RFirst(1.0 - mass.DolM2) + RAdj(1.0 - mass.DolM3) + "  ];"); // Fortran 1321
        Blank(w);
        Line(w, " % <Agglomerates>");
        Line(w, " % Pocket to Agglomerate size coefficient:");
        Line(w, " da_coef = " + R(local.Mp) + " ;");
        Line(w, " % Mass medium size of agglomerates (normal var. #1,2  and cor. var. #1,2):");
        Line(w, " Dagg43(1) = " + F(pockets.Dp43 * 1e6 * local.Mp, 2, 7) + ";"); // Fortran 1328
        Line(w, " Dagg43(2) = " + F(pockets.D432 * 1e6 * local.Mp, 2, 7) + ";"); // Fortran 1329
        Line(w, " Dagg43_cor(1) = " + F(pockets.Dkarm43Cor * 1e6 * local.Mp, 2, 7) + ";"); // Fortran 1330
        Line(w, " Dagg43_cor(2) = " + F(pockets.Dfmk432 * 1e6 * local.Mp, 2, 7) + ";"); // Fortran 1331
        Blank(w);
        Line(w, " %___________Interpocket bridges parameters____________");
        Line(w, " % Medium MKM/Dok size between Dok particles:");
        Line(w, " Dqmkm1 = " + F(agglomerates.Dqmkm1, 4, 6) + ";");
        Line(w, " % Medium MKM/Dol size between pockets:");
        Line(w, " Dqmkm2 = " + F(agglomerates.Dqmkm2, 4, 6) + ";");
    }

    // ---- functions (Fortran lines 1338-1453) -------------------------------------------------------------

    private static void WriteFunctions(TextWriter w, SimulationResult result)
    {
        var histograms = result.Histograms;
        var pockets = result.Pockets;
        var agglomerates = result.Agglomerates;
        var cellSize = result.StoredSetup.CellSize;
        // Explicit '(1x,a,I3,a)' format: width 3, no list-directed separator.
        var diMkm = FortranFormat.IntegerField(Trunc(cellSize * 1.00001e6), 3); // Fortran 1343, 1348, 1351, 1355, 1359, 1362

        Blank(w);
        Blank(w);
        Line(w, " %__________________Functions__________________________");
        Blank(w);
        Line(w, " % <Dok>");
        Line(w, " % Mass density distribution function of all dok <particles> sizes (step =" + diMkm + "mkm)");
        WriteArray(w, "fmdok", Scale(histograms.Allvdokso.AsSpan(), cellSize)); // Fortran 1345
        Blank(w);
        Line(w, " % <Pockets>");
        Line(w, " % Mass density distribution function of <pocket>sizes (step =" + diMkm + "mkm):");
        var dpLength = (int)(pockets.DpMax / cellSize) + 2; // Fortran 1350, 1361
        WriteArray(w, "fmkarm", Scale(Take(histograms.Vkso, dpLength), cellSize)); // Fortran 1350
        Line(w, " % Cor. mass density distribution function of <pocket> sizes (step =" + diMkm + "mkm)(cor. var #1):");
        var dpCorLength = (int)(pockets.DpMaxCor / cellSize) + 2; // Fortran 1353, 1357, 1364
        WriteArray(w, "fmkarm_cor", Scale(Take(histograms.FmkarmCorNormalized, dpCorLength), cellSize)); // Fortran 1353
        Line(w, " % Cor. mass density distribution function of  <pocket> sizes (step =" + diMkm + "mkm)(cor. var#2):");
        WriteArray(w, "fmkarm_cor2", Scale(Take(histograms.Fmkarm2Normalized, dpCorLength), cellSize)); // Fortran 1357
        Line(w, " % Numeric density distribution function of  <pocket> sizes (step =" + diMkm + "mkm):");
        WriteArray(w, "fqkarm", Scale(Take(histograms.Qks1, dpLength), cellSize)); // Fortran 1361
        Line(w, " % Cor. numeric density distribution function of <pocket> sizes (step =" + diMkm + "mkm)(cor. var #1):");
        WriteArray(w, "fqkarm_cor", Scale(Take(histograms.FqkarmCorNormalized, dpCorLength), cellSize)); // Fortran 1364
        Blank(w);
        Line(w, " % <MKM/Dok>");
        Line(w, " % Density distribution function of MKM/Dok between Dok particles (step = 0.01)");
        WriteArray(w, "fqmkm1", Take(histograms.Qmkm1Normalized, agglomerates.Qmkm1Nmax + 2)); // Fortran 1370
        Line(w, " % Density distribution function of MKM/Dok between pockets (step = 0.01)");
        WriteArray(w, "fqmkm2", Take(histograms.Qmkm2Normalized, agglomerates.Qmkm2Nmax + 2)); // Fortran 1373
        Blank(w);
        Line(w, " % <Local structure analysis>");
        Line(w, " % Coefficient [(Lij/Ddok)+1] distribution (step = 0.01):");
        WriteArray(w, "coef", Take(histograms.CoefNormalized, agglomerates.CoefNmax + 2)); // Fortran 1377
        Line(w, " % Distribution of P[pocket-in-pocket] on basicDok particles sizes:");
        WriteArray(w, "pdoksmall", histograms.Pdoksmall.AsSpan()); // Fortran 1386
        Blank(w);
        Line(w, " % <Conditional DOK>");
        Line(w, " % Pockets categories sizes (mkm):");
        WriteArray(w, "Dkarmcat", ScaleByMicron(pockets.Dpockets)); // Fortran 1393
        Line(w, " % Dependency of mass-medium Dok particles sizes on pockets categories:");
        WriteArray(w, "dokkarm43", ScaleByMicron(pockets.Dokp43)); // Fortran 1396
        Line(w, " % Dependency of medium Dok particles sizes on pockets categories:");
        WriteArray(w, "dokkarm10", ScaleByMicron(pockets.Qdokkarm)); // Fortran 1400
        Line(w, " % Numeric density distribution functions of  particles on pockets categories:");

        // Every row of Qdokso is Ndok long (Statistics/API.md, Categories.MergeAndDescribe); read each
        // row's own length instead of a separate Ndok, since a jagged/nested Qdokso carries no column
        // count of its own when DpRow is 0 (the multidimensional double[,] this replaced did, but the
        // loop below never runs in that case, so no value of that count was ever observed there either).
        for (var irow = 1; irow <= pockets.DpRow; irow++)
        {
            Line(w, " % Dkarm =" + I2Field(Trunc(pockets.Dpockets[irow - 1] * 1.00001e6)) + " mkm"); // Fortran 1414
            var qdoksoRow = pockets.Qdokso[irow - 1];
            var row = new double[qdoksoRow.Length];
            for (var i = 0; i < qdoksoRow.Length; i++)
            {
                row[i] = qdoksoRow[i] / (cellSize * 1e6); // Fortran 1416
            }

            WriteArray(w, "fqdokkarm(" + FortranFormat.IntegerField(irow, 3) + ",:)", row);
        }

        if (result.Convergence is { } series) // Fortran 1418
        {
            Blank(w);
            Line(w, " %________Dependence on basic particles number_________");
            WriteArray2(w, "epsfkarm_n", series.ConvergenceEpsy.AsSpan());
            WriteArray2(w, "epsm3karm_n", series.ConvergenceEpsmd3.AsSpan());
            WriteArray2(w, "epsm4karm_n", series.ConvergenceEpsmd4.AsSpan());
            WriteArray2(w, "epsdok43_n", series.ConvergenceAlldok43.AsSpan());
            WriteArray2(w, "epsdoksd_n", series.ConvergenceAlldoksd.AsSpan());
            WriteArray2(w, "epszkarm_n", series.ConvergenceDolM2.AsSpan());
        }

        // The time line is the only line that depends on wall time (BOOT.md, "Design decisions
        // (2026-09-19)"); tests/Harness excludes it from every comparison. Fortran lines 1444-1446:
        // write(4,*) (blank), then '(1x,a\)''% Calculation time:'' with no advance, then
        // '(I3,a,I3,a,I3)' hour1,' :',minut1,' :',sec1 continuing the same record.
        Blank(w);
        var elapsed = result.Diagnostics.Elapsed;
        Line(w,
            " % Calculation time:" + FortranFormat.IntegerField(elapsed.Hours, 3) +
            " :" + FortranFormat.IntegerField(elapsed.Minutes, 3) +
            " :" + FortranFormat.IntegerField(elapsed.Seconds, 3));

        // A second port-specific footer line, the same precedent as the time line above: a comment with no
        // "=" and no counterpart in the original, so it carries no declared name and no digits (root
        // BOOT.md, "Precision kind is an option of every run": "The kind is recorded in the run's result
        // and in results.m, so no number leaves the program unlabelled"). Deliberately not a bare enum
        // name: a reader who has never seen this option is told what happened, not shown a label they would
        // have to go look up. Excluded from tests/Output.Tests' structural comparison the same way the time
        // line is (StructuralTests.IsComparableCommentBanner); carrying no digits keeps it out of any
        // comment-derived-quantity scheme a downstream parser might apply, the same property that already
        // protects the time line (this document does not read tests/Harness's code to confirm that, only
        // its API.md, AGENTS.md §3 - it is a property of this line's own text, not a claim about a
        // neighbour's parser).
        Line(w, PrecisionCommentLine(result.Diagnostics.Precision));

        // Two more footer lines of the same shape, added 2026-09-21 (BOOT.md, "Invariants": the run's stream
        // layout and execution mode change the printed numbers by more than the precision kind does, and
        // until now only the console recorded them - a user re-running an old command line from a kept
        // results.m had nothing in the file to say why the numbers moved). Same rules as the line above:
        // no "=", no digit, excluded from the structural comparison the same way
        // (StructuralTests.IsComparableCommentBanner), and worded for a reader who has never seen
        // --streams/--mode rather than shown the bare enum name.
        Line(w, StreamLayoutCommentLine(result.Diagnostics.Streams));
        Line(w, ExecutionModeCommentLine(result.Diagnostics.Mode));
        Line(w, " % __________________________");
        Line(w, " hold on");
        Line(w, " plot(Di:Di:Di*size(fmkarm,2),fmkarm)");
        Line(w, " plot(Di:Di:Di*size(fmkarm_cor,2),fmkarm_cor,'r')");
        Line(w, " plot(Di:Di:Di*size(fmkarm_cor2,2),fmkarm_cor2,'g:')");
    }

    /// <summary>
    /// A plain-English sentence, not a bare enum name: a reader who has never seen <c>--precision</c> (or
    /// the library option it maps to) is told what the run did, not handed a label to go look up. Carries no
    /// digit, deliberately (see the call site's own comment). Describes the three planes `Original`
    /// reproduces (root BOOT.md, "Precision kind is an option of every run"): the accumulators, since
    /// 2026-09-23 the setup plane (`src/Statistics/BOOT.md`, "## Setup plane") and since 2026-10-01 the
    /// per-cycle plane (`src/Statistics/BOOT.md`, "## Report").
    /// </summary>
    private static string PrecisionCommentLine(PrecisionKind kind) => kind switch
    {
        PrecisionKind.Binary64 =>
            " % Precision: double precision throughout; the original's REAL*4 rounding was not reproduced.",
        PrecisionKind.Original =>
            " % Precision: reproduces the original's REAL*4 rounding loss, in its accumulators, its setup plane and its per-cycle plane.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, $"Unhandled {nameof(PrecisionKind)}."),
    };

    /// <summary>
    /// A plain-English sentence, not a bare enum name, for the same reason as <see cref="PrecisionCommentLine"/>.
    /// Root BOOT.md, "Two stream layouts" and "Known bias of the original's seeds": the <c>Original</c> layout
    /// carries the original's own correlated seeds and their measured statistical bias (up to double digits of
    /// per cent on the pocket and bridge quantities); the <c>Independent</c> layout does not.
    /// </summary>
    private static string StreamLayoutCommentLine(StreamLayout layout) => layout switch
    {
        StreamLayout.Original =>
            " % Stream layout: the original program's own generator seeds, correlated as in the source, with their known statistical bias.",
        StreamLayout.Independent =>
            " % Stream layout: independent generator orbits, disjoint from one another, that do not carry the original's seed bias.",
        _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, $"Unhandled {nameof(StreamLayout)}."),
    };

    /// <summary>
    /// A plain-English sentence, not a bare enum name, for the same reason as <see cref="PrecisionCommentLine"/>.
    /// Root BOOT.md, "Reference mode is the original's sequence" and "Batched mode freezes only QKS1, per
    /// launch": the two modes refresh the pocket histogram on different schedules, which is itself a source of
    /// the numbers moving between them, independent of the stream layout or the accumulation kind.
    /// </summary>
    private static string ExecutionModeCommentLine(ExecutionMode mode) => mode switch
    {
        ExecutionMode.Reference =>
            " % Execution mode: reference mode; refreshes the pocket histogram after every completed attempt, as the original does.",
        ExecutionMode.Batched =>
            " % Execution mode: batched mode; freezes the pocket histogram for the whole launch, unlike the original's own sequence.",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, $"Unhandled {nameof(ExecutionMode)}."),
    };

    /// <summary>
    /// Reads the first <paramref name="length"/> cells of <paramref name="values"/>, padded with zeros beyond
    /// <paramref name="values"/>'s own length rather than truncated to it (BOOT.md, "Design decisions
    /// (2026-09-19)": "<c>coef</c>, <c>qmkm1</c>, <c>qmkm2</c> printing <c>nmax + 2</c> values with zeros beyond
    /// <c>Nc</c>"). <c>coef</c>/<c>qmkm1</c>/<c>qmkm2</c> are always <c>Nc</c>-sized
    /// (<c>src/Statistics/BOOT.md</c>, "## Report": <c>qmkm1/Σ[Nc]</c>, <c>qmkm2/Σ[Nc]</c>, <c>coef/Σ[Nc]</c>);
    /// their own <c>*_nmax + 2</c> print length can exceed <c>Nc</c> (Fortran's own `arrayprint` dummy argument
    /// is declared at the caller's `arraysize`, wider than the actual `Nc`-sized array whenever `nmax` reaches
    /// `Nc - 1` or `Nc`, and prints zero past the end — measured, `tests/Fixtures/Legacy/outputs/hp2.m.txt`,
    /// 1002 `coef` values, the last two `0.000E+00`). A clamp to <paramref name="values"/>'s own length silently
    /// dropped those trailing zeros instead of printing them — the defect this method's own history fixes
    /// (`## Defects of the original` cites this row via <c>src/Statistics/BOOT.md</c>; O-2, dated below).
    /// </summary>
    private static double[] Take(System.Collections.Immutable.ImmutableArray<double> values, int length)
    {
        var result = new double[length];
        var count = Math.Min(length, values.Length);
        for (var i = 0; i < count; i++)
        {
            result[i] = values[i];
        }

        return result;
    }

    private static double[] Scale(ReadOnlySpan<double> values, double cellSize)
    {
        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = values[i] / (cellSize * 1e6); // Fortran 1345, 1350, 1353, 1357, 1361, 1364
        }

        return result;
    }

    private static double[] ScaleByMicron(System.Collections.Immutable.ImmutableArray<double> values)
    {
        var result = new double[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            result[i] = values[i] * 1.00001e6; // Fortran 1393, 1396, 1400
        }

        return result;
    }

    // ---- arrayprint / arrayprint2 (Fortran lines 1517-1571) ----------------------------------------------

    /// <summary>Transcribes <c>arrayprint</c> (Fortran lines 1517-1543): <c>E9.3</c>, 6 values per line.</summary>
    private static void WriteArray(TextWriter w, string name, ReadOnlySpan<double> values) =>
        w.Write(FortranFormat.FormatArray(name, values, decimals: 3) + "\r\n");

    /// <summary>Transcribes <c>arrayprint2</c> (Fortran lines 1545-1571): <c>E12.6</c>, 6 values per line.</summary>
    private static void WriteArray2(TextWriter w, string name, ReadOnlySpan<double> values) =>
        w.Write(FortranFormat.FormatArray(name, values, decimals: 6) + "\r\n");
}
