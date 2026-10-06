using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The four out-of-sample controls the arbiter's per-cycle-plane verdict (2026-09-28,
/// amendment A3) freezes before any per-cycle rounding code is written: whether the
/// design's rule for Fortran 795-796 and the setup plane's own rule L (amendment A1/A2)
/// are <em>predicted</em>, not merely fitted to the one HMX cell that first suggested
/// them. Every figure here is the arbiter's own (a working note, not in the tree: its "## Amendments",
/// A3's table), reproduced by the same algorithm the
/// arbiter's own scripts (<c>zs_by_h.py</c>, <c>oos_epsdokfr.py</c>, <c>oos_epsx.py</c>,
/// <c>oos_epsx3.py</c>) used, in C# rather than Python: this file is that reference
/// computation, not a report about it.
///
/// Controls (i)-(iii) gate: each is proven right at the arbiter's own count and red on
/// the alternative the arbiter named. Control (iv) reported only until 2026-10-02 (an
/// orchestrator decision of 2026-09-28, on inputs recovered from the output's header, which
/// were wrong: <c>psan01.dat</c> and <c>psan02.dat</c> are shipped); it gates now, on those
/// files (<c>src/Statistics/ACCEPTANCE.md</c>, the same criterion's note).
/// </summary>
public class PerCyclePlaneOutOfSampleControlsTests
{
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    private static readonly string[] ReferenceFormulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];

    // -------------------------------------------------------------------------------
    // Shared machinery: the (mantissa, exponent) reading of a three-significant-digit
    // Fortran E9.3 token (e.g. "0.251E-05"), used to decide whether a computed double
    // prints to the same token as an archived cell. `ResultCell.Value` already round-trips
    // the printed decimal exactly (ResultsMFile's own parse), so formatting it through the
    // same function reproduces its own token, and two values format equal exactly when
    // they would print identically.
    // -------------------------------------------------------------------------------

    private static (long Mantissa, int Exponent) Fmt3(double x)
    {
        if (x == 0.0)
        {
            return (0, 0);
        }

        x = Math.Abs(x);
        var exponent = (int)Math.Floor(Math.Log10(x)) + 1;
        for (var guard = 0; guard < 8; guard++)
        {
            var scaled = x / Math.Pow(10.0, exponent);
            var mantissa = (long)Math.Floor(scaled * 1000.0 + 0.5);
            if (mantissa >= 1000)
            {
                exponent++;
                continue;
            }

            if (mantissa < 100)
            {
                exponent--;
                continue;
            }

            return (mantissa, exponent);
        }

        throw new InvalidOperationException($"Fmt3 did not converge for {x:R}");
    }

    private static bool PrintsTheSameThreeDigitToken(double computed, double printed) => Fmt3(computed) == Fmt3(printed);

    private static Formulation ReadFormulation(string name) =>
        DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat"));

    /// <summary>
    /// <c>tables.Share</c> (ZX) and <c>tables.MassShare</c>/<c>tables.Bounds</c> (GDOK/DDOK,
    /// already rounded to binary32) under <see cref="PrecisionKind.Original"/>, read
    /// through the one production <see cref="Setup.Prepare"/> for every control here
    /// (task instruction: "Read ZX and DOKM through Setup.Prepare(Original)"). Shared
    /// across every replica of one formulation: none of these three depend on the run's
    /// own random seed.
    /// </summary>
    private static SetupTables ReadOriginalTables(string formulation)
    {
        var status = Setup.Prepare(ReadFormulation(formulation), ModelParameters.Default, PrecisionKind.Original,
            NeighbourBudget, PocketRedrawBudget, out _, out var tables, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);
        return tables;
    }

    private static List<string> ArchivedOutputs(string formulation, bool includeCasesOutput)
    {
        var paths = new List<string> { RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt") };
        foreach (var kind in new[] { "replicas-lagged", "replicas-independent", "replicas-gsv3" })
        {
            var directory = RepositoryPaths.Resolve("tests", "Fixtures", kind, formulation);
            if (Directory.Exists(directory))
            {
                paths.AddRange(Directory.EnumerateFiles(directory, "*.m.txt").OrderBy(p => p, StringComparer.Ordinal));
            }
        }

        if (includeCasesOutput)
        {
            var extra = formulation switch
            {
                "HPEPA3" => RepositoryPaths.Resolve("tests", "Fixtures", "cases", "output", "nonzero-tail-probability", "results.m.txt"),
                "inpt" => RepositoryPaths.Resolve("tests", "Fixtures", "cases", "output", "nondefault-pocket-coefficient", "results.m.txt"),
                _ => null,
            };
            if (extra is not null)
            {
                paths.Add(extra);
            }
        }

        return paths;
    }

    // =================================================================================
    // Control (i): the 795-796 rounding rule (design.md rule, "nothing rounded before
    // 796's store"), over the printed `epsdokfr` array (Fortran EPSALLDOK).
    // =================================================================================

    /// <summary>
    /// `epsdokfr(kilo) = binary32(|n_kilo/N - zx[kilo]| / zx[kilo])`, `n_kilo` an unknown
    /// nonnegative integer per fraction, `N = NFX + NFY` (NFZ = NFY structurally: the
    /// only two increment sites of `ALLDOK_FRACT`/`ALLDOK`, lines 465/468 and 520/523,
    /// sit next to NFX and NFY, and label 345 is never referenced — design.md, §1.2).
    /// "Consistent" means some nonnegative integer assignment across every fraction, with
    /// `sum(n) = N`, prints every archived `epsdokfr` cell of the file.
    /// </summary>
    private static bool EpsdokfrConsistent(
        Func<long, long, double> fractionAt795, double[] zx, ResultCell[] cells, long total)
    {
        var k = cells.Length;
        double Cell(long n, int fraction) => Binary32.ToNearestRepresentable(Math.Abs(fractionAt795(n, total) - zx[fraction]) / zx[fraction]);

        if (k == 1)
        {
            return PrintsTheSameThreeDigitToken(Cell(total, 0), cells[0].Value);
        }

        var sets = new List<long>[k];
        for (var fraction = 0; fraction < k; fraction++)
        {
            var printed = cells[fraction];
            var found = new HashSet<long>();
            foreach (var sign in new[] { 1.0, -1.0 })
            {
                var centre = zx[fraction] * (1.0 + sign * printed.Value) * total;
                var window = (long)(zx[fraction] * total * printed.Resolution) + 4;
                if (window > 200_000)
                {
                    return false; // too wide to enumerate: not consistent within this search's own reach.
                }

                var lo = Math.Max(0L, (long)centre - window);
                var hi = (long)centre + window;
                for (var n = lo; n <= hi; n++)
                {
                    if (PrintsTheSameThreeDigitToken(Cell(n, fraction), printed.Value))
                    {
                        _ = found.Add(n);
                    }
                }
            }

            sets[fraction] = found.OrderBy(x => x).ToList();
            if (sets[fraction].Count == 0)
            {
                return false;
            }
        }

        var order = Enumerable.Range(0, k).OrderBy(i => sets[i].Count).ToArray();
        var target = new HashSet<long>(sets[order[^1]]);
        var rest = order[..^1];

        bool Recurse(int index, long partialSum)
        {
            if (index == rest.Length)
            {
                return target.Contains(total - partialSum);
            }

            foreach (var n in sets[rest[index]])
            {
                if (Recurse(index + 1, partialSum + n))
                {
                    return true;
                }
            }

            return false;
        }

        return Recurse(0, 0);
    }

    private static (int Consistent, int Total) RunControl1(
        Func<long, long, double> fractionAt795, IReadOnlyList<string> formulations, bool includeCasesOutput)
    {
        var consistent = 0;
        var total = 0;
        foreach (var formulation in formulations)
        {
            var tables = ReadOriginalTables(formulation);
            foreach (var path in ArchivedOutputs(formulation, includeCasesOutput))
            {
                var parsed = ResultsMFile.ParseCells(path);
                var nfx = (long)Math.Round(parsed["NFX"][0].Value);
                var nfy = (long)Math.Round(parsed["NFY"][0].Value);
                var cells = parsed["epsdokfr"];
                total++;
                if (EpsdokfrConsistent(fractionAt795, tables.Share, cells, nfx + nfy))
                {
                    consistent++;
                }
            }
        }

        return (consistent, total);
    }

    /// <summary>
    /// Right: the design rule (nothing rounded at 795; 796 alone rounds) reproduces every
    /// archived `epsdokfr` cell of all 295 outputs of the five reference formulations
    /// (the reference plus every lagged/independent/GSV=3 replica, plus HPEPA3's own
    /// `--alfa` case and `inpt`'s own `--karmcoef` case, neither of which changes the
    /// fraction-share computation `epsdokfr` reads — verdict.md, A3(i)).
    /// </summary>
    [Fact]
    public void Control1EpsdokfrRuleIsPredictedAcrossTheFiveReferenceFormulations()
    {
        var (consistent, total) = RunControl1((n, total) => (double)n / total, ReferenceFormulations, includeCasesOutput: true);
        Assert.Equal(295, total);
        Assert.Equal(295, consistent);
    }

    /// <summary>
    /// Red: the named alternative ("795's store rounded", i.e. the quotient itself is
    /// binary32 before 796 reads it) is inconsistent on 40 of 49 HMX outputs and 84 of 98
    /// HPEPA3 outputs (verdict.md, A3(i)'s own red column) — the two formulations the
    /// verdict names, proving the check actually discriminates rather than always
    /// agreeing regardless of which hypothesis it is fed.
    /// </summary>
    [Fact]
    public void Control1EpsdokfrRuleIsRedOnTheStoreRoundedAlternative()
    {
        var (hmxConsistent, hmxTotal) = RunControl1((n, total) => Binary32.ToNearestRepresentable((double)n / total), ["HMX"], includeCasesOutput: false);
        Assert.Equal(49, hmxTotal);
        Assert.Equal(49 - 40, hmxConsistent);

        var (hpepa3Consistent, hpepa3Total) = RunControl1((n, total) => Binary32.ToNearestRepresentable((double)n / total), ["HPEPA3"], includeCasesOutput: true);
        Assert.Equal(98, hpepa3Total);
        Assert.Equal(98 - 84, hpepa3Consistent);
    }

    // =================================================================================
    // Control (ii): R1 at Fortran 771-783 (the seven generator accuracies XSR0-6): under
    // the rule, XSR is binary32 before the very next line reads it, so every printed
    // `epsx` cell must be an exact multiple of 2^-24.
    // =================================================================================

    // `epsx(k)=<8-significant-digit token>` is read directly from the raw text, exactly
    // as the arbiter's own oos_epsx.py does (`epsx\(\d\)=`): ResultsMFile.ParseCells's own
    // "epsx" key also folds in the differently-shaped `EPSX1=`/`EPSX2=` comment lines some
    // legacy screen dumps carry (a different Fortran pair, EPSX1/EPSX2 of
    // CycleStatistics.OxidizerSizes, not the seven XSR-based accuracies this control is
    // about), so this control reads the parenthesised array only.
    private static readonly System.Text.RegularExpressions.Regex EpsxCellPattern =
        new(@"epsx\(\d\)=\s*([-0-9.Ee+]+)", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static (long Mantissa, int Exponent) Fmt8(double x)
    {
        if (x == 0.0)
        {
            return (0, 0);
        }

        x = Math.Abs(x);
        var exponent = (int)Math.Floor(Math.Log10(x));
        for (var guard = 0; guard < 8; guard++)
        {
            var scaled = x / Math.Pow(10.0, exponent - 7);
            var mantissa = (long)Math.Floor(scaled + 0.5);
            if (mantissa >= 100_000_000)
            {
                exponent++;
                continue;
            }

            if (mantissa < 10_000_000)
            {
                exponent--;
                continue;
            }

            return (mantissa, exponent);
        }

        throw new InvalidOperationException($"Fmt8 did not converge for {x:R}");
    }

    private static double[] ReadEpsxCells(string path)
    {
        var text = File.ReadAllText(path);
        return EpsxCellPattern.Matches(text).Select(m => double.Parse(m.Groups[1].Value)).ToArray();
    }

    private static bool PrintsTheSameEightDigitToken(double computed, double printed) => Fmt8(computed) == Fmt8(printed);

    /// <summary>
    /// Every printed `epsx(k)` cell of every archived output (five reference
    /// formulations, all replica kinds, and every legacy output that prints any `epsx(k)`
    /// cell at all) lies on the binary32 grid: rounding it to the nearest multiple of
    /// 2^-24 and re-printing that multiple at eight significant digits reproduces the
    /// archived token exactly (verdict.md, A3(ii): 2016 of 2016).
    /// </summary>
    [Fact]
    public void Control2EpsxCellsLieOnTheBinary32GridAcrossEveryArchivedOutput()
    {
        var files = new List<string>();
        foreach (var formulation in ReferenceFormulations)
        {
            files.AddRange(ArchivedOutputs(formulation, includeCasesOutput: false));
        }

        var legacyDirectory = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "outputs");
        files.AddRange(Directory.EnumerateFiles(legacyDirectory, "*.m.txt").OrderBy(p => p, StringComparer.Ordinal));

        Assert.Equal(368, files.Count); // 293 archive + 75 legacy; only 43 of the legacy files print any epsx(k) cell.

        var totalCells = 0;
        var onGrid = 0;
        var filesWithCells = 0;
        foreach (var path in files)
        {
            var cells = ReadEpsxCells(path);
            if (cells.Length == 0)
            {
                continue;
            }

            filesWithCells++;
            foreach (var value in cells)
            {
                totalCells++;
                var nearestMultiple = Math.Round(value * Math.Pow(2, 24)) / Math.Pow(2, 24);
                if (PrintsTheSameEightDigitToken(nearestMultiple, value))
                {
                    onGrid++;
                }
            }
        }

        Assert.Equal(336, filesWithCells); // 293 archive + 43 legacy files that print epsx(k).
        Assert.Equal(2016, totalCells);
        Assert.Equal(2016, onGrid);
    }

    /// <summary>
    /// Red on the frozen pre-change runs, right on today's: R1 at 771-783 puts every
    /// `epsx(k)` cell the port prints under <see cref="PrecisionKind.Original"/> on the
    /// binary32 grid, all 960 of the 160 rate runs, while the seed-0 runs frozen before the
    /// per-cycle plane was implemented (`tests/Simulation.Tests/Snapshots/CyclePlaneBaseline`,
    /// byte copies of the rate runs of 514a157) put none of theirs there. Until 2026-10-01
    /// the red side read the rate runs themselves (verdict.md, A3(ii): 0 of 960); they now
    /// carry the implementation, so the pre-change side reads the frozen copies.
    /// </summary>
    [Fact]
    public void Control2EpsxCellsAreOnTheGridInThePortsRunsAndOffItBeforeTheChange()
    {
        var directory = RepositoryPaths.Resolve("tests", "Fixtures", "rate-runs");
        var portFiles = Directory.EnumerateFiles(directory, "*.m.txt", SearchOption.AllDirectories)
            .Where(p => Path.GetFileName(Path.GetDirectoryName(p)) == "Original")
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        var baselineFiles = Directory.EnumerateFiles(RepositoryPaths.Resolve("tests", "Simulation.Tests", "Snapshots", "CyclePlaneBaseline"), "*.m.txt")
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        var (portCells, portOnGrid) = CountOnGrid(portFiles);
        var (baselineCells, baselineOnGrid) = CountOnGrid(baselineFiles);

        Assert.Equal(960, portCells);
        Assert.Equal(960, portOnGrid);
        Assert.Equal(30, baselineCells);
        Assert.Equal(0, baselineOnGrid);
    }

    private static (int Cells, int OnGrid) CountOnGrid(IEnumerable<string> files)
    {
        var totalCells = 0;
        var onGrid = 0;
        foreach (var path in files)
        {
            foreach (var value in ReadEpsxCells(path))
            {
                totalCells++;
                var nearestMultiple = Math.Round(value * Math.Pow(2, 24)) / Math.Pow(2, 24);
                if (PrintsTheSameEightDigitToken(nearestMultiple, value))
                {
                    onGrid++;
                }
            }
        }

        return (totalCells, onGrid);
    }

    // =================================================================================
    // Control (iii): rule L (amendment A1/A2) for a REAL*4 sum whose target is
    // loop-invariant (DOKM here): carried unrounded across the loop, rounded once when it
    // exits. The printed scalar `epsalldok` (Fortran EPSX3, 816) must land on the
    // binary32 grid of ALLDOK43 around the setup's own DOKM.
    // =================================================================================

    private static float NextBinary32(float x, int steps)
    {
        var bits = BitConverter.SingleToInt32Bits(x);
        return BitConverter.Int32BitsToSingle(bits + steps);
    }

    /// <summary>
    /// The once-rounded DOKM of rule L, the control's frozen reference: `DOKM` and the
    /// JZZ=2 branch's `DOK4`/`DOK3` accumulate unrounded across their own loop and round
    /// once, after it. Rule L was retired on 2026-10-03: Setup.AnalyticSizes follows the
    /// executable's listing, `DOKM` unrolled five-fold. On the five reference
    /// formulations the two give the same bits, so this control stands as frozen.
    /// This is the reference computation the verdict names ("R1 at 810, plus L"), kept
    /// here as a small, independent twin — the same role every other formula-script twin
    /// in this tree plays — so control (iii) does not depend on <see cref="Setup.AnalyticSizes"/>
    /// already implementing the rule it is testing for.
    /// </summary>
    private static double DokmOnceRounded(int sizeLaw, int fractionCount, double[] massShare, double[] share, double[] bounds)
    {
        if (sizeLaw == 2)
        {
            var dok4 = 0.0;
            var dok3 = 0.0;
            for (var j = 0; j < fractionCount; j++)
            {
                var lower = bounds[2 * j];
                var upper = bounds[2 * j + 1];
                dok4 += (Math.Pow(upper, 5) - Math.Pow(lower, 5)) * share[j] / (upper - lower);
                dok3 += (Math.Pow(upper, 4) - Math.Pow(lower, 4)) * share[j] / (upper - lower);
            }

            var dok4Rounded = Binary32.ToNearestRepresentable(dok4);
            var dok3Rounded = Binary32.ToNearestRepresentable(dok3);
            var massUniformCoefficient = Binary32.ToNearestRepresentable(0.8);
            return Binary32.ToNearestRepresentable(massUniformCoefficient * dok4Rounded / dok3Rounded);
        }

        var accumulator = 0.0;
        for (var j = 0; j < fractionCount; j++)
        {
            var lower = bounds[2 * j];
            var upper = bounds[2 * j + 1];
            accumulator += (lower + upper) * massShare[j] / 2.0;
        }

        return Binary32.ToNearestRepresentable(accumulator);
    }

    private static bool EpsalldokOnBinary32Grid(double dokm, double printedEpsalldok)
    {
        var e = printedEpsalldok;
        for (var sign = 1; sign >= -1; sign -= 2)
        {
            var centre = (float)Binary32.ToNearestRepresentable(dokm * (1.0 + sign * e));
            for (var step = -40; step <= 40; step++)
            {
                var candidate = (double)NextBinary32(centre, step);
                var computed = Binary32.ToNearestRepresentable(Math.Abs(dokm - candidate) / dokm);
                if (PrintsTheSameEightDigitToken(computed, printedEpsalldok))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (int OnGrid, int Total) RunControl3(Func<string, SetupTables, double> dokmOf)
    {
        var onGrid = 0;
        var total = 0;
        foreach (var formulation in ReferenceFormulations)
        {
            var tables = ReadOriginalTables(formulation);
            var dokm = dokmOf(formulation, tables);
            foreach (var path in ArchivedOutputs(formulation, includeCasesOutput: false))
            {
                var parsed = ResultsMFile.ParseCells(path);
                var printed = parsed["epsalldok"][0].Value;
                total++;
                if (EpsalldokOnBinary32Grid(dokm, printed))
                {
                    onGrid++;
                }
            }
        }

        return (onGrid, total);
    }

    /// <summary>
    /// Right: the once-rounded DOKM (rule L) reproduces the archived `epsalldok` on all
    /// 293 outputs of the five reference formulations (verdict.md, A3(iii)).
    /// </summary>
    [Fact]
    public void Control3EpsalldokRuleIsPredictedWithTheOnceRoundedDokm()
    {
        var (onGrid, total) = RunControl3((formulation, tables) =>
        {
            var formulationData = ReadFormulation(formulation);
            return DokmOnceRounded(formulationData.SizeLawCode, formulationData.Fractions.Length, tables.MassShare, tables.Share, tables.Bounds);
        });

        Assert.Equal(293, total);
        Assert.Equal(293, onGrid);
    }

    /// <summary>
    /// A2's own before/after proof (<c>Setup.Prepare(Original)</c>'s own <c>DOKM</c>, no
    /// independent computation): before the rule-L fix, <c>Setup.AnalyticSizes</c> rounded
    /// <c>DOKM</c> after every addition and matched only 15 of PSAN02n's own 49 archived
    /// `epsalldok` outputs (verdict.md, A2's own "Frozen before code"; seen red on this
    /// exact assertion before the fix landed in the same commit as this dated note). After
    /// the fix, it matches on all 49 — the flip is the fix's own proof, not a separate
    /// measurement. The other four reference formulations are unaffected by the
    /// per-addition-vs-once-rounded difference either way (verdict.md, Q1: "no difference
    /// on HMX, HPEPA3, P33, inpt and P350"), so this control is PSAN02n-only.
    /// </summary>
    [Fact]
    public void Control3ProductionSetupPrepareReproducesEpsalldokOnPsan02nAfterTheA2RuleLFix()
    {
        var status = Setup.Prepare(ReadFormulation("PSAN02n"), ModelParameters.Default, PrecisionKind.Original,
            NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out var pending);
        Assert.Equal(SetupStatus.Ok, status);
        var productionDokm = pending.Dokm;

        var onGridPsan02n = 0;
        var totalPsan02n = 0;
        foreach (var path in ArchivedOutputs("PSAN02n", includeCasesOutput: false))
        {
            var parsed = ResultsMFile.ParseCells(path);
            var printed = parsed["epsalldok"][0].Value;
            totalPsan02n++;
            if (EpsalldokOnBinary32Grid(productionDokm, printed))
            {
                onGridPsan02n++;
            }
        }

        Assert.Equal(49, totalPsan02n);
        Assert.Equal(49, onGridPsan02n); // was 49 - 34 = 15 before the A2 rule-L fix (2026-09-28).
    }

    // =================================================================================
    // Control (iv): da_coef of the two legacy runs rps01/rps02, against the shipped .dat
    // each was run on (psan01.dat, psan02.dat, found by the output's own "Input filename"
    // line). Gating since 2026-10-02; reported only, on other inputs, before.
    // =================================================================================

    /// <summary>
    /// `da_coef` (`CycleReport.Mp`, Fortran 1001-1016) of the constructed cycle
    /// (<see cref="ConstructedCycle"/>) on the shipped `.dat` the output was run on, read
    /// with its pocket-forming flags (the output prints its `sfr`, so the run read them,
    /// menu [12]), `Original` precision kind, against the output's own printed `da_coef`.
    /// Every input but the cell is the formulation's: `eta` is the menu default 0 (the
    /// output does not print it), and the reachable FMDOK cell is the output's printed
    /// `fmdok[0]`, asserted empty (`Dmin = Di` printed, so `int(Dmin/Di) = 1`).
    /// </summary>
    private static (double Computed, ResultCell Printed) Control4Run(string legacyName, bool readPocketFormingFractions)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "outputs", $"{legacyName}.m.txt");
        var printed = ResultsMFile.ParseCells(path);
        Assert.True(printed.ContainsKey("sfr"), $"{legacyName}: the output does not print its pocket-forming flags.");
        Assert.Equal(printed["Di"][0].Value, printed["Dmin"][0].Value);
        Assert.Equal(0.0, printed["fmdok"][0].Value);

        var datPath = ArchivedOutput.ShippedFormulation(path);
        Assert.NotNull(datPath);
        var computed = ConstructedCycle.Mp(datPath, readPocketFormingFractions, PrecisionKind.Original);
        return (computed, printed["da_coef"][0]);
    }

    private static bool PrintsTheSameToken(double computed, ResultCell printed)
    {
        var digits = (int)Math.Round(-Math.Log10(printed.Resolution));
        var format = $"F{digits}";
        return computed.ToString(format, System.Globalization.CultureInfo.InvariantCulture)
            == printed.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Right: both legacy runs' printed `da_coef` (0.7284034 and 0.6019315 in their files) is
    /// what the plane computes from their own `.dat`. Red on the same run without the
    /// pocket-forming flags (the next test), and on a one-ULP shift of `gdoksfr`'s
    /// rounding in `CycleStatistics.Matrix` (recorded in `tests/Statistics.Tests/BOOT.md`).
    /// </summary>
    [Theory]
    [InlineData("rps01")]
    [InlineData("rps02")]
    public void Control4DaCoefOfTheLegacyPairReproducesTheArchivedPrint(string legacyName)
    {
        var (computed, printed) = Control4Run(legacyName, readPocketFormingFractions: true);
        Assert.True(PrintsTheSameToken(computed, printed), $"{legacyName}: computed {computed:R}, printed {printed.Value:R}.");
    }

    /// <summary>
    /// Non-degeneracy through the same path: with the `.dat`'s pocket-forming flags left
    /// unread every fraction forms pockets, `gdoksfr` is empty and `gdokleft` is 0, and
    /// the print moves, so the control above does read the matrix block it guards.
    /// </summary>
    [Theory]
    [InlineData("rps01")]
    [InlineData("rps02")]
    public void Control4IsRedWhenThePocketFormingFlagsAreNotRead(string legacyName)
    {
        var (computed, printed) = Control4Run(legacyName, readPocketFormingFractions: false);
        Assert.False(PrintsTheSameToken(computed, printed), $"{legacyName}: computed {computed:R} still prints {printed.Value:R}.");
    }
}
