using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Carries the four accumulator errors <c>src/Particle/BOOT.md</c> measured in their
/// own units (<c>Allvdok</c>, <c>Vdokstr</c>, <c>VdokTotal</c>, <c>VdokTotal2</c>, "##
/// Accumulators", "Decisions where the port departs from a transcription", re-measured
/// 2026-09-21 at the original's own one-rounding-per-drawn-particle granularity,
/// superseding the 2026-09-20 figures) through this node's own derivation to the
/// printed quantities they reach (BOOT.md, "## Line map"), and puts the result against
/// each quantity's own band width (root BOOT.md, "quantities degraded by the original's
/// REAL*4 accumulation are excluded only with computed evidence").
///
/// Method: <c>CycleStatistics.Compute</c> is evaluated twice on the same, otherwise
/// identical totals for a reference formulation's own real <c>ModelSetup</c> — once
/// with the accumulator as a plain <c>double</c> sum, once with its worst cell
/// multiplied by <c>(1 + worst-cell relative error)</c> from <c>Particle</c>'s own
/// table — an exact evaluation of the real derivation, not a hand-derived formula
/// (BOOT.md, Taboos: no second implementation). The band width is read off
/// <c>StatisticalCriterion.Compare</c>'s own <c>Threshold</c> for the failing cell of
/// interest: since <c>Threshold</c> is a property of the replica population alone
/// (Student band, static floor, count floor — all independent of the candidate's own
/// value), a synthetic candidate constructed to fail on exactly that cell recovers the
/// same threshold a real failing candidate would report, without needing the L2 driver
/// this node's own BOOT.md records as not yet built (`tests/Statistics.Tests/BOOT.md`,
/// "L2 ... needs the reference-mode driver of Execution").
/// </summary>
public class AccumulationConsequenceTests
{
    private readonly ITestOutputHelper _output;

    public AccumulationConsequenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Worst-cell binary32-vs-double relative error at each formulation's true term
    /// count, at the original's own one-rounding-per-drawn-particle granularity
    /// (`src/Particle/BOOT.md`, "## Accumulators"; the table itself moved to
    /// `src/Particle/HISTORY.md#accumulator-error-per-drawn-particle-worst-cell-table`,
    /// dated 2026-09-21). Supersedes the 2026-09-20 figures, which rounded once per
    /// <c>Attempt.Run</c> call instead of once per drawn particle and were withdrawn as
    /// a lower bound read as a measurement
    /// (`src/Particle/HISTORY.md#real4-accumulation-reconnaissance-2026-09-20-21`, "⚠
    /// 2026-09-20"; `HISTORY.md#superseded-per-run-reference-criterion`, root's own
    /// "⚠ 2026-09-21, later still"). `VdokTotal`/`VdokTotal2`
    /// are outside this re-measurement (not in `Particle`'s own table: they are proven
    /// not to reach `pdoksmall`/`fmdok`/`Dok43all` at all by
    /// <see cref="VdokTotalAndVdokTotal2DoNotReachPdoksmallFmdokOrDok43all"/> below, so
    /// their own figure never enters a propagation), and keep their prior
    /// "&lt;= 0.003% on every formulation" placeholder unchanged.
    /// </summary>
    private static readonly Dictionary<string, (double Allvdok, double Vdokstr, double VdokTotalFamily)> WorstCellRelativeError = new()
    {
        ["HPEPA3"] = (0.46, 0.46, 0.00003),
        ["inpt"] = (0.00029, 0.00027, 0.00003),
        ["P33"] = (0.00081, 0.00076, 0.00003),
        ["PSAN02n"] = (0.034, 0.032, 0.00003),
        ["HMX"] = (0.65, 0.65, 0.00003),
    };

    public static IEnumerable<object[]> ReferenceFormulations()
    {
        yield return new object[] { "HPEPA3" };
        yield return new object[] { "inpt" };
        yield return new object[] { "P33" };
        yield return new object[] { "PSAN02n" };
        yield return new object[] { "HMX" };
    }

    /// <summary>
    /// Baseline shape for <c>Allvdok</c>: the formulation's own printed "fmdok" — Fortran's
    /// own comment names it the mass DENSITY distribution of all Dok particles
    /// ("определение массовой функции плотности распределения ДОК", the line
    /// immediately above <c>ALLVDOKSO(kilo)=ALLVDOK(kilo)/ALLVDOKS</c>, `## Line map`
    /// 789-819), unlike the internal cumulative `fmdok`/FMDOK this node's own
    /// <c>CycleStatistics.OxidizerSizes</c> XML doc already flags as "never printed and
    /// not part of the report" — so the printed "fmdok" is <c>Allvdokso</c>, a per-cell
    /// fraction (µm-scaled by <c>Output</c>, a pure multiplicative factor a *relative*
    /// comparison does not need to know). Real, published fixture evidence, not a
    /// constructed proxy.
    /// </summary>
    private static double[] LoadAllvdokShape(string name, int ndok)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");
        var cells = ResultsMFile.Parse(path);
        var fmdok = cells["fmdok"];

        var shape = new double[ndok];
        var length = Math.Min(fmdok.Length, ndok);
        for (var i = 0; i < length; i++)
        {
            shape[i] = Math.Max(fmdok[i], 0.0);
        }

        return shape;
    }

    /// <summary>
    /// <c>Vdokstr</c> has no printed proxy at all (BOOT.md, "The failing cells", quoting
    /// `src/Particle/BOOT.md`: "Allvdok, Vdokstr ... are not themselves printed"). Both
    /// are Ndok-length, oxidizer-size-cell-indexed sums over the same drawn population
    /// (`src/Particle/API.md`, "cell k = oxidizer-size cell k (same indexing as Alldok)"
    /// for both fields) — the shape this test uses for <c>Vdokstr</c> is <c>Allvdok</c>'s
    /// own real shape, a stated physical approximation, checked for robustness against a
    /// uniform shape in <see cref="ConclusionIsRobustToTheAssumedVdokstrShape"/> below.
    /// </summary>
    private static double[] LoadVdokstrShape(string name, int ndok) => LoadAllvdokShape(name, ndok);

    private readonly record struct Scenario(
        Formulation Formulation, SetupInputs Inputs, ModelSetup Setup, SetupTables Tables, SetupEchoes Echoes,
        double[] AllvdokShape, double[] VdokstrShape);

    private static Scenario BuildScenario(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var status = Setup.Prepare(formulation, parameters, PrecisionKind.Binary64, neighbourBudget: 1000, pocketRedrawBudget: 1000,
            out var setup, out var tables, out _, out var pending, out var inputs);
        Assert.Equal(SetupStatus.Ok, status);

        // Dmax does not feed any quantity this test reads (OxidizerSizes, Matrix,
        // SmallParticles.Probability); the tail draw itself is L0's own concern
        // (`SetupTests.PrepareAndCompleteEchoesRoundTripThroughSizeLawSample`), so this
        // test stands up no accelerator and uses Ddokmax as a harmless stand-in.
        var echoes = Setup.CompleteEchoes(ref setup, pending, pending.Ddokmax);

        var allvdokShape = LoadAllvdokShape(name, setup.Ndok);
        var vdokstrShape = LoadVdokstrShape(name, setup.Ndok);

        return new Scenario(formulation, inputs, setup, tables, echoes, allvdokShape, vdokstrShape);
    }

    /// <summary>
    /// Builds a minimal, cycle-0-safe totals buffer: every category/pocket/generator
    /// field a zero-safe default (Categories.MergeAndDescribe and Pockets both tolerate
    /// all-zero totals without a guard, BOOT.md "IEEE results where the original
    /// computes them ... No guard is added" — the resulting NaNs are fields this test
    /// does not read), with <c>Allvdok</c> and <c>Vdokstr</c> set to the scenario's own
    /// shapes scaled to an arbitrary, non-degenerate total (the derivation this test
    /// exercises is scale-invariant in the shape, BOOT.md "## Line map": every use of
    /// these two fields normalizes by the field's own sum).
    /// </summary>
    private static (long[] IntegerTotals, double[] RealTotals) BuildTotals(Scenario scenario)
    {
        var layout = scenario.Setup.Layout;
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];

        const double scale = 1e6;
        for (var k = 0; k < scenario.Setup.Ndok; k++)
        {
            realTotals[layout.Allvdok + k] = scenario.AllvdokShape[k] * scale;
            realTotals[layout.Vdokstr + k] = scenario.VdokstrShape[k] * scale;
        }

        return (integerTotals, realTotals);
    }

    private static CycleReport Compute(Scenario scenario, long[] integerTotals, double[] realTotals)
    {
        return CycleStatistics.Compute(
            scenario.Setup, scenario.Tables, scenario.Echoes, scenario.Inputs,
            cycleIndex: 0, integerTotals, realTotals, new double[scenario.Setup.Ndok],
            out _, out _);
    }

    private static int ArgMax(double[] values)
    {
        var best = 0;
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] > values[best])
            {
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// The smallest index whose shape is nonzero — a multiplicative perturbation of a
    /// cell that is genuinely zero (real fixture data: HPEPA3's own printed "fmdok(1)"
    /// is exactly 0) leaves it zero, which is a statement about that cell, not about
    /// whether the derivation reaches it (found while chasing why perturbing literal
    /// cell 0 left every formulation's <c>pdoksmall</c> bit-for-bit unchanged: cell 0
    /// carried no draws in the real distribution these shapes are read from).
    /// </summary>
    private static int FirstNonzeroCell(double[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] > 0.0)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// <c>StatisticalCriterion.Compare</c>'s own <c>Threshold</c> for one cell, read by
    /// constructing a candidate that fails exactly that cell (three times the
    /// reference's own value, plus a additive escape from zero) and taking the matching
    /// <see cref="CriterionFailure"/> back out. <c>Threshold</c> is the replica
    /// population's own band (Student term, static floor, count floor), not a function
    /// of the candidate's value (root BOOT.md, "a Student-t band ... floors from the
    /// print resolution"), so this recovers the same width a real failing candidate
    /// would report.
    /// </summary>
    private static double? ReadBandWidth(string name, ReplicaKind kind, string quantity, int index)
    {
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");
        var reference = ResultsMFile.Parse(referencePath);
        if (!reference.TryGetValue(quantity, out var values) || index >= values.Length)
        {
            return null;
        }

        // An "in-family" nudge scale: the reference array's own median absolute cell
        // that is not itself garbage (BOOT.md, the pdoksmall(1) defect: reference cells
        // below 1e-30 are excluded/compared as zero, `tests/Fixtures/API.md`), so the
        // synthetic candidate stays a plausible value for this quantity rather than an
        // arbitrary large number a count-like cell's own value-dependent floor could
        // react to (`StatisticalCriterion`'s count floor is built from the candidate's
        // own implied total, `tests/Harness/API.md`, "count-like floor"), and does not
        // collapse to a near-zero scale on a formulation whose own array is entirely
        // below that floor (PSAN02n's own reference `pdoksmall`, every cell).
        var real = values.Where(v => Math.Abs(v) > 1e-30).Select(Math.Abs).OrderBy(v => v).ToArray();
        var nudgeScale = real.Length > 0 ? real[real.Length / 2] : 1.0;

        // Grow the perturbation geometrically from a tiny nudge until Compare reports a
        // failure, in both directions; take the first (smallest) one that fails, so the
        // reported Threshold sits as close as the search resolution allows to the true
        // boundary rather than to a candidate far outside the band.
        foreach (var relativeStep in new[] { 0.001, 0.003, 0.01, 0.03, 0.1, 0.3, 1.0, 3.0, 10.0, 30.0, 100.0, 300.0, 1000.0 })
        {
            foreach (var sign in new[] { 1.0, -1.0 })
            {
                var candidate = new Dictionary<string, double[]>(reference);
                var perturbed = (double[])values.Clone();
                var baseValue = perturbed[index];
                var step = sign * relativeStep * nudgeScale;
                perturbed[index] = baseValue + step;
                candidate[quantity] = perturbed;

                var report = StatisticalCriterion.Compare(name, candidate, kind);
                foreach (var failure in report.Failures)
                {
                    if (failure.Quantity == quantity && failure.Index == index)
                    {
                        return failure.Threshold;
                    }
                }
            }
        }

        return null;
    }

    private enum Verdict { RulesOut, LargeEnoughToMatter, Inconclusive, NoBand }

    private static Verdict Classify(double delta, double? band)
    {
        if (band is not { } b || b <= 0.0)
        {
            return Verdict.NoBand;
        }

        var ratio = Math.Abs(delta) / b;
        if (ratio < 0.05)
        {
            return Verdict.RulesOut;
        }

        if (ratio > 0.5)
        {
            return Verdict.LargeEnoughToMatter;
        }

        return Verdict.Inconclusive;
    }

    /// <summary>
    /// Allvdok's worst cell propagated to <c>Allvdokso</c> ("fmdok") and <c>Alldok432</c>
    /// ("Dok43all[1]"): both are exact evaluations of the same real code, so a nonzero
    /// perturbation must move both (a broken wiring — e.g. perturbing a copy never fed
    /// to <c>Compute</c> — would report a delta of exactly zero and fail the first two
    /// asserts, proving this check non-degenerate, AGENTS.md §13).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void AllvdokErrorPropagatesToAllvdoksoAndAlldok432(string name)
    {
        var scenario = BuildScenario(name);
        var (integerTotals, realTotals) = BuildTotals(scenario);
        var baseline = Compute(scenario, integerTotals, realTotals);

        var layout = scenario.Setup.Layout;
        var worstCell = ArgMax(scenario.AllvdokShape);
        var epsilon = WorstCellRelativeError[name].Allvdok;

        var (perturbedIntegerTotals, perturbedRealTotals) = BuildTotals(scenario);
        perturbedRealTotals[layout.Allvdok + worstCell] *= 1.0 + epsilon;
        var perturbed = Compute(scenario, perturbedIntegerTotals, perturbedRealTotals);

        var deltaAllvdokso = perturbed.Allvdokso[worstCell] - baseline.Allvdokso[worstCell];
        var deltaAlldok432 = perturbed.Alldok432 - baseline.Alldok432;

        Assert.NotEqual(0.0, deltaAllvdokso);
        Assert.NotEqual(0.0, deltaAlldok432);

        // Unit conversions, both pure multiplicative factors owned by Output ("## Line
        // map"'s own "µm scaling"), established here empirically rather than assumed:
        // printed "fmdok" sums to ~1/CellSize(µm) over its own cells (a density per
        // micrometre, not a per-cell fraction), and Alldok432 is this node's own SI
        // metres against Dok43all's printed micrometres (the same ×1e6 convention
        // `SetupTests.PrepareMatchesThePrintedSetupQuantitiesOfItsReferenceFormulation`
        // already uses for Dok43a/Dok43sd/Ddok_max).
        var diMicrometres = scenario.Setup.CellSize * 1e6;
        var deltaFmdok = deltaAllvdokso / diMicrometres;
        var deltaDok43all = deltaAlldok432 * 1e6;

        var fmdokBand = ReadBandWidth(name, ReplicaKind.Lagged, "fmdok", worstCell)
            ?? ReadBandWidth(name, ReplicaKind.Independent, "fmdok", worstCell);
        var dok43allBand = ReadBandWidth(name, ReplicaKind.Lagged, "Dok43all", 1)
            ?? ReadBandWidth(name, ReplicaKind.Independent, "Dok43all", 1);

        _output.WriteLine($"{name}: worst Allvdok cell = {worstCell}, share = {scenario.AllvdokShape[worstCell]:G6}, epsilon = {epsilon:P4}");
        _output.WriteLine($"  fmdok[{worstCell}] delta={deltaFmdok:G6} band={fmdokBand:G6} -> {Classify(deltaFmdok, fmdokBand)}");
        _output.WriteLine($"  Dok43all[1] delta={deltaDok43all:G6} um, baseline={baseline.Alldok432 * 1e6:G6} um, band={dok43allBand:G6} um -> {Classify(deltaDok43all, dok43allBand)}");
    }

    /// <summary>
    /// Vdokstr's first nonzero cell propagated to <c>Pdoksmall</c> (the dominant driver
    /// of the three suspect quantities, "## Line map"). Not the shape's own largest cell:
    /// reading <c>SmallParticles.Probability</c> shows every <c>zdoksmall(kilo)</c>, for
    /// <c>kilo</c> in <c>[1, Ndok]</c>, either sums <c>vdokstr[0 .. wholeFractions-1]</c>
    /// in full or reads a partial term at <c>vdokstr[wholeFractions]</c>, where
    /// <c>wholeFractions = floor(kilo / Ak2)</c> — so the largest index any <c>kilo</c>
    /// in that range ever reads is <c>floor(Ndok / Ak2)</c>, and no cell past it is on
    /// any <c>zdoksmall</c> draw's path at all. Cell 0 is always inside that window
    /// (<c>floor(Ndok/Ak2) &gt;= 0</c> always), but a *multiplicative* perturbation of a
    /// cell whose own value is genuinely 0 — the real "fmdok" shape's own cell 0, every
    /// reference formulation — leaves it 0: found while chasing why perturbing literal
    /// cell 0 left every formulation's <c>pdoksmall</c> bit-for-bit unchanged, and why
    /// perturbing the shape's own largest cell left `inpt`'s unchanged too (its own
    /// first nonzero cell, ~30, sits past its own window, `floor(36/2) = 18`, even
    /// though `Ak2 = 2` is far below `Ndok`). The first cell that is both reachable and
    /// perturbable is the smallest index with a nonzero share
    /// (<see cref="FirstNonzeroCell"/>), when one exists inside the window at all:
    /// applying the worst-cell error to a cell the derivation cannot reach, or that is
    /// exactly zero, is not a weaker measurement, it is a wrong one (AGENTS.md §13: this
    /// is what proved the earlier version of this check degenerate).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void VdokstrErrorPropagatesToPdoksmall(string name)
    {
        var scenario = BuildScenario(name);
        var (integerTotals, realTotals) = BuildTotals(scenario);
        var baselineFull = new double[scenario.Setup.Ndok];
        var baseline = CycleStatistics.Compute(
            scenario.Setup, scenario.Tables, scenario.Echoes, scenario.Inputs,
            cycleIndex: 0, integerTotals, realTotals, baselineFull, out _, out _);

        var layout = scenario.Setup.Layout;
        var cell = FirstNonzeroCell(scenario.VdokstrShape);
        var epsilon = WorstCellRelativeError[name].Vdokstr;

        var (perturbedIntegerTotals, perturbedRealTotals) = BuildTotals(scenario);
        perturbedRealTotals[layout.Vdokstr + cell] *= 1.0 + epsilon;
        var perturbedFull = new double[scenario.Setup.Ndok];
        var perturbed = CycleStatistics.Compute(
            scenario.Setup, scenario.Tables, scenario.Echoes, scenario.Inputs,
            cycleIndex: 0, perturbedIntegerTotals, perturbedRealTotals, perturbedFull, out _, out _);

        var ndok = scenario.Setup.Ndok;
        var maxDeltaIndex = 0;
        var maxDelta = 0.0;
        for (var k = 0; k < baseline.Pdoksmall.Length; k++) // Ndok - 1, not Ndok (BOOT.md, "## Report").
        {
            var delta = perturbed.Pdoksmall[k] - baseline.Pdoksmall[k];
            if (Math.Abs(delta) > Math.Abs(maxDelta))
            {
                maxDelta = delta;
                maxDeltaIndex = k;
            }
        }

        // Non-degeneracy (AGENTS.md §13) is proven once, for every formulation, by
        // <see cref="ConclusionIsRobustToTheAssumedVdokstrShape"/>'s own uniform shape
        // (cell 0 nonzero there, unlike the real fixture shape used here): this real-shape
        // run is a *measurement*, not a mechanism proof, and a genuine zero is itself a
        // finding when it occurs. The largest index any kilo in [1, Ndok] ever reads
        // (full or partial term) is floor(Ndok / Ak2) (this method's own doc comment on
        // <c>wholeFractions</c>); `inpt`'s own `Ak2` exceeds `Ndok`, so that bound is 0,
        // and cell 0 is exactly the cell the real "fmdok" shape carries no draws in — so
        // `Vdokstr`'s error reaches `pdoksmall` not weakly but not at all there.
        var maxReachableCell = (int)Math.Floor(ndok / scenario.Formulation.Ak2);
        if (cell > maxReachableCell)
        {
            _output.WriteLine($"{name}: Vdokstr's first nonzero cell ({cell}) sits past the largest index any kilo in [1, Ndok={ndok}] ever reads (floor(Ndok/Ak2)={maxReachableCell}, Ak2={scenario.Formulation.Ak2:G6}) -> the error cannot reach Pdoksmall at all for this formulation, under the real shape.");
            Assert.Equal(0.0, maxDelta);
            return;
        }

        if (maxDelta == 0.0)
        {
            // The printed array is Ndok - 1 elements (BOOT.md, "## Report", Fortran line
            // 1386): "reachable" above means only that some kilo reads the cell, not that
            // it reads it with a nonzero weight (Xr = mod(kilo, Ak2)/Ak2 can land exactly
            // on 0 at the boundary). When that first nonzero-weight kilo is Ndok itself —
            // the one element the print call drops — the perturbation is real but
            // invisible to every printed cell; distinguish that from an unexplained zero
            // by checking the full, untrimmed next-cycle array this same call already
            // produced.
            var lastCellDelta = perturbedFull[^1] - baselineFull[^1];
            Assert.NotEqual(0.0, lastCellDelta);
            for (var k = 0; k < baselineFull.Length - 1; k++)
            {
                Assert.Equal(baselineFull[k], perturbedFull[k]);
            }

            _output.WriteLine($"{name}: Vdokstr cell {cell} perturbed (first nonzero) only reaches the dropped, unprinted Pdoksmall(Ndok) cell (delta={lastCellDelta:G6}); every printed cell (0..Ndok-2) is bit-for-bit unchanged.");
            return;
        }

        var band = ReadBandWidth(name, ReplicaKind.Lagged, "pdoksmall", maxDeltaIndex)
            ?? ReadBandWidth(name, ReplicaKind.Independent, "pdoksmall", maxDeltaIndex);

        _output.WriteLine($"{name}: Vdokstr cell {cell} perturbed (first nonzero), epsilon = {epsilon:P4}, Ak2 = {scenario.Formulation.Ak2:G6}, Ndok = {ndok}");
        _output.WriteLine($"  Pdoksmall worst index = {maxDeltaIndex}, baseline={baseline.Pdoksmall[maxDeltaIndex]:G6} delta={maxDelta:G6} band={band:G6} -> {Classify(maxDelta, band)}");
    }

    /// <summary>
    /// The <c>gdokleft</c>/internal <c>FMDOK</c> recurrence path from <c>Allvdok</c> to
    /// <c>Pdoksmall</c> — the one sub-path the 2026-09-21 completeness audit found
    /// claimed ("a small additive term") but never quantified (BOOT.md, "## Report",
    /// "Completeness audit of this chain"). Same perturbation machinery as
    /// <see cref="AllvdokErrorPropagatesToAllvdoksoAndAlldok432"/> and
    /// <see cref="VdokstrErrorPropagatesToPdoksmall"/> (<see cref="BuildScenario"/>,
    /// <see cref="BuildTotals"/>, <see cref="Compute"/>, <see cref="ReadBandWidth"/>,
    /// <see cref="Classify"/>): the same <c>CycleStatistics.Compute</c> call the
    /// fmdok/Dok43all test already makes returns <c>Pdoksmall</c> in the same
    /// <see cref="CycleReport"/>, unread there.
    ///
    /// <c>CycleStatistics.Matrix</c>'s own <c>fmdokIndex = int(Dmin/Di)</c> reads
    /// <c>fmdok[fmdokIndex] = Σ allvdokso[0 .. fmdokIndex - 1]</c> (0-based), so only
    /// cells strictly below <c>fmdokIndex</c> ever enter the sum <c>gdokleft</c> reads —
    /// the reachable window is <c>[0, fmdokIndex)</c>, not the whole array. Unlike
    /// <see cref="AllvdokErrorPropagatesToAllvdoksoAndAlldok432"/>, which perturbs the
    /// shape's global maximum (every cell reaches <c>Allvdokso</c>/<c>Alldok432</c>
    /// equally), this test perturbs the largest cell <em>inside that window</em> — the
    /// worst cell this specific channel can actually reach, following the same
    /// "reachable, not merely largest" correction <see cref="VdokstrErrorPropagatesToPdoksmall"/>'s
    /// own doc comment already made for its own window.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void AllvdokErrorPropagatesToPdoksmallThroughGdokleft(string name)
    {
        var scenario = BuildScenario(name);
        var (integerTotals, realTotals) = BuildTotals(scenario);
        var baseline = Compute(scenario, integerTotals, realTotals);

        var layout = scenario.Setup.Layout;
        var epsilon = WorstCellRelativeError[name].Allvdok;

        // fmdok[fmdokIndex] sums allvdokso[0 .. fmdokIndex - 1] (CycleStatistics.Matrix's
        // own fmdokIndex = int(Dmin/Di), "## Report" above): the reachable window is
        // [0, fmdokIndex), never the whole array.
        var fmdokIndex = (int)(scenario.Setup.Dmin / scenario.Setup.CellSize);
        var windowEnd = Math.Min(fmdokIndex, scenario.AllvdokShape.Length);

        if (windowEnd <= 0)
        {
            // FMDOK(1) = 0 by construction (CycleStatistics.OxidizerSizes): fmdokIndex <= 0
            // means gdokleft's Allvdok term reads fmdok[0] = 0 regardless of any Allvdok
            // cell, so there is no cell left to perturb at all -- the structural closure
            // BOOT.md's own "## Report" describes ("live only when Dmin >= Di").
            _output.WriteLine($"{name}: fmdokIndex=int(Dmin/Di)={fmdokIndex} <= 0, so gdokleft's Allvdok term reads fmdok[0]=0 (FMDOK(1)=0 by construction) regardless of any cell -> the channel is structurally closed, no perturbation to make.");
            return;
        }

        var reachableCell = 0;
        for (var k = 1; k < windowEnd; k++)
        {
            if (scenario.AllvdokShape[k] > scenario.AllvdokShape[reachableCell])
            {
                reachableCell = k;
            }
        }

        var (perturbedIntegerTotals, perturbedRealTotals) = BuildTotals(scenario);
        perturbedRealTotals[layout.Allvdok + reachableCell] *= 1.0 + epsilon;
        var perturbed = Compute(scenario, perturbedIntegerTotals, perturbedRealTotals);

        var maxDeltaIndex = 0;
        var maxDelta = 0.0;
        for (var k = 0; k < baseline.Pdoksmall.Length; k++) // Ndok - 1, not Ndok (BOOT.md, "## Report").
        {
            var delta = perturbed.Pdoksmall[k] - baseline.Pdoksmall[k];
            if (Math.Abs(delta) > Math.Abs(maxDelta))
            {
                maxDelta = delta;
                maxDeltaIndex = k;
            }
        }

        if (maxDelta == 0.0)
        {
            // A multiplicative perturbation of a cell whose own real shape value is
            // genuinely 0 leaves it 0 (the same shape VdokstrErrorPropagatesToPdoksmall's
            // own doc comment records for its window): the channel is reachable in
            // principle but carries no draws in this formulation's own real Allvdok shape.
            _output.WriteLine($"{name}: Allvdok's reachable window is [0, {windowEnd - 1}]; its largest cell ({reachableCell}, shape={scenario.AllvdokShape[reachableCell]:G6}) perturbed leaves every printed Pdoksmall cell bit-for-bit unchanged -> the channel carries no draws in this formulation's own real Allvdok shape.");
            return;
        }

        var band = ReadBandWidth(name, ReplicaKind.Lagged, "pdoksmall", maxDeltaIndex)
            ?? ReadBandWidth(name, ReplicaKind.Independent, "pdoksmall", maxDeltaIndex);

        _output.WriteLine($"{name}: Allvdok cell {reachableCell} perturbed (epsilon = {epsilon:P4}), fmdokIndex = int(Dmin/Di) = {fmdokIndex}, reachable window = [0, {windowEnd - 1}]");
        _output.WriteLine($"  Pdoksmall worst index = {maxDeltaIndex}, baseline={baseline.Pdoksmall[maxDeltaIndex]:G6} delta={maxDelta:G6} band={band:G6} -> {Classify(maxDelta, band)}");
    }

    /// <summary>
    /// Non-degeneracy proof for <see cref="AllvdokErrorPropagatesToPdoksmallThroughGdokleft"/>
    /// (AGENTS.md §13): on every reference formulation that test's own delta is zero
    /// because the real Allvdok shape carries no mass in cell 0, the one cell the
    /// gdokleft channel reaches (measured: <c>fmdokIndex = int(Dmin/Di) = 1</c> on all
    /// five, so the reachable window is <c>[0, 0]</c>) — a finding about the data, which
    /// a disconnected <c>gdokleft</c>/<c>FMDOK</c> wire would report identically. This
    /// substitutes a uniform shape (nonzero at cell 0, the same substitution
    /// <see cref="ConclusionIsRobustToTheAssumedVdokstrShape"/> already makes for
    /// <c>Vdokstr</c>) and shows the same perturbation of cell 0 does move
    /// <c>Pdoksmall</c> once that cell is genuinely nonzero, proving the wiring itself
    /// is real and the zero result above is the data's, not a broken connection.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void AllvdokErrorPropagationThroughGdokleftIsWiredWhenTheReachableCellIsNonzero(string name)
    {
        var scenario = BuildScenario(name);
        var ndok = scenario.Setup.Ndok;
        var uniformShape = new double[ndok];
        for (var k = 0; k < ndok; k++)
        {
            uniformShape[k] = 1.0;
        }

        // Both shapes uniform, not just Allvdok's: SmallParticles.Probability's own
        // vdoksmall formula multiplies gdokleft's contribution by zdoksmall[k], itself
        // built entirely from vdokstr (VdokstrErrorPropagatesToPdoksmall's own line map
        // reading), so a real Vdokstr shape with no draws in a formulation's own reachable
        // window (already established there for inpt and PSAN02n) would zero out
        // gdokleft's effect regardless of wiring -- a fact about that formulation's own
        // data, not about this channel. Forcing Vdokstr uniform too isolates the wiring
        // this test exists to prove from that unrelated, already-documented zero.
        var uniformScenario = scenario with { AllvdokShape = uniformShape, VdokstrShape = uniformShape };

        var fmdokIndex = (int)(uniformScenario.Setup.Dmin / uniformScenario.Setup.CellSize);
        Assert.True(fmdokIndex > 0,
            $"{name}: fmdokIndex=int(Dmin/Di)={fmdokIndex} <= 0 -- this formulation's own gdokleft channel is structurally closed (AllvdokErrorPropagatesToPdoksmallThroughGdokleft), so this wiring proof cannot run here.");

        var (integerTotals, realTotals) = BuildTotals(uniformScenario);
        var baseline = Compute(uniformScenario, integerTotals, realTotals);

        var layout = uniformScenario.Setup.Layout;
        var epsilon = WorstCellRelativeError[name].Allvdok;

        var (perturbedIntegerTotals, perturbedRealTotals) = BuildTotals(uniformScenario);
        perturbedRealTotals[layout.Allvdok + 0] *= 1.0 + epsilon; // cell 0: always inside [0, fmdokIndex).
        var perturbed = Compute(uniformScenario, perturbedIntegerTotals, perturbedRealTotals);

        var maxDelta = 0.0;
        for (var k = 0; k < baseline.Pdoksmall.Length; k++) // Ndok - 1, not Ndok (BOOT.md, "## Report").
        {
            var delta = perturbed.Pdoksmall[k] - baseline.Pdoksmall[k];
            if (Math.Abs(delta) > Math.Abs(maxDelta))
            {
                maxDelta = delta;
            }
        }

        Assert.NotEqual(0.0, maxDelta);
    }

    /// <summary>
    /// Sensitivity check for the shape assumption <see cref="LoadVdokstrShape"/> makes
    /// (Allvdok's own shape, no printed proxy exists for Vdokstr itself): repeats the
    /// <c>Pdoksmall</c> propagation with a uniform shape instead and reports whether the
    /// classification (rules out / matters / inconclusive) changes. A conclusion that
    /// flips under this substitution is reported as shape-sensitive, not stated as fact.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void ConclusionIsRobustToTheAssumedVdokstrShape(string name)
    {
        var scenario = BuildScenario(name);
        var ndok = scenario.Setup.Ndok;
        var uniformShape = new double[ndok];
        for (var k = 0; k < ndok; k++)
        {
            uniformShape[k] = 1.0;
        }

        var uniformScenario = scenario with { VdokstrShape = uniformShape };

        var (integerTotals, realTotals) = BuildTotals(uniformScenario);
        var baseline = Compute(uniformScenario, integerTotals, realTotals);

        var layout = uniformScenario.Setup.Layout;
        var worstCell = ArgMax(uniformShape); // ties: the first cell, cell 0.
        var epsilon = WorstCellRelativeError[name].Vdokstr;

        var (perturbedIntegerTotals, perturbedRealTotals) = BuildTotals(uniformScenario);
        perturbedRealTotals[layout.Vdokstr + worstCell] *= 1.0 + epsilon;
        var perturbed = Compute(uniformScenario, perturbedIntegerTotals, perturbedRealTotals);

        var maxDeltaIndex = 0;
        var maxDelta = 0.0;
        for (var k = 0; k < baseline.Pdoksmall.Length; k++) // Ndok - 1, not Ndok (BOOT.md, "## Report").
        {
            var delta = perturbed.Pdoksmall[k] - baseline.Pdoksmall[k];
            if (Math.Abs(delta) > Math.Abs(maxDelta))
            {
                maxDelta = delta;
                maxDeltaIndex = k;
            }
        }

        var band = ReadBandWidth(name, ReplicaKind.Lagged, "pdoksmall", maxDeltaIndex)
            ?? ReadBandWidth(name, ReplicaKind.Independent, "pdoksmall", maxDeltaIndex);

        _output.WriteLine($"{name} (uniform Vdokstr shape): worst index = {maxDeltaIndex}, delta={maxDelta:G6} band={band:G6} -> {Classify(maxDelta, band)}");
    }

    /// <summary>
    /// <c>VdokTotal</c>/<c>VdokTotal2</c> feed only <c>DolM2</c>/<c>DolM3</c> (BOOT.md,
    /// "## Line map", Fortran 1137-1152), never <c>pdoksmall</c>, <c>fmdok</c>
    /// (<c>Allvdokso</c>) or <c>Dok43all</c> (<c>Alldok432</c>) — proves the diagnosing
    /// chain's premise wrong for these two accumulators by reflection over
    /// <c>CycleStatistics.MassFractions</c>'s own field reads rather than by argument
    /// alone: a future edit that actually wired <c>VdokTotal</c>/<c>VdokTotal2</c> into
    /// <c>OxidizerSizes</c>, <c>Matrix</c> or <c>SmallParticles.Probability</c> would
    /// change <c>Allvdokso</c>/<c>Alldok432</c>/<c>Pdoksmall</c> when only
    /// <c>VdokTotal</c>/<c>VdokTotal2</c> move, which this test would then catch.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void VdokTotalAndVdokTotal2DoNotReachPdoksmallFmdokOrDok43all(string name)
    {
        var scenario = BuildScenario(name);
        var layout = scenario.Setup.Layout;

        // Cycle >= 1, so MassFractions (and hence VdokTotal/VdokTotal2) actually runs;
        // Vsmkm/Svd/VmkmTotal/VmkmTotal2 need non-zero, safe values too (BuildTotals
        // leaves them zero, which is fine for cycle 0 but would give DolM1-3 = 0/0 =
        // NaN here, still harmless: this test never reads DolM1-3).
        var (integerTotals, realTotals) = BuildTotals(scenario);
        realTotals[layout.VdokTotal] = 1.0;
        realTotals[layout.VdokTotal2] = 1.0;
        realTotals[layout.VmkmTotal] = 1.0;
        realTotals[layout.VmkmTotal2] = 1.0;
        realTotals[layout.Vsmkm] = 1.0;
        realTotals[layout.Svd] = 1.0;

        var baseline = CycleStatistics.Compute(
            scenario.Setup, scenario.Tables, scenario.Echoes, scenario.Inputs,
            cycleIndex: 1, integerTotals, realTotals, new double[scenario.Setup.Ndok], out _, out _);

        var (perturbedIntegerTotals, perturbedRealTotals) = BuildTotals(scenario);
        perturbedRealTotals[layout.VdokTotal] = 1.0 * (1.0 + 5.0); // a large, deliberately unmissable perturbation.
        perturbedRealTotals[layout.VdokTotal2] = 1.0 * (1.0 + 5.0);
        perturbedRealTotals[layout.VmkmTotal] = 1.0;
        perturbedRealTotals[layout.VmkmTotal2] = 1.0;
        perturbedRealTotals[layout.Vsmkm] = 1.0;
        perturbedRealTotals[layout.Svd] = 1.0;

        var perturbed = CycleStatistics.Compute(
            scenario.Setup, scenario.Tables, scenario.Echoes, scenario.Inputs,
            cycleIndex: 1, perturbedIntegerTotals, perturbedRealTotals, new double[scenario.Setup.Ndok], out _, out _);

        Assert.Equal(baseline.Allvdokso, perturbed.Allvdokso);
        Assert.Equal(baseline.Alldok432, perturbed.Alldok432);
        Assert.Equal(baseline.Pdoksmall, perturbed.Pdoksmall);
        Assert.NotEqual(baseline.DolM2, perturbed.DolM2); // sanity: the perturbation does reach *something*.
        Assert.NotEqual(baseline.DolM3, perturbed.DolM3);
    }
}
