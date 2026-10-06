using System.Collections.Immutable;
using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L1 of the BOOT.md table: <c>CycleStatistics.Compute</c> on a small constructed
/// scenario, covering cycle 0 vs cycle >= 1 (the fields Fortran lines 1084-1175 alone
/// compute are <see cref="double.NaN"/> in cycle 0, BOOT.md "## Report") and the
/// in-place rewrite of <c>VdokTotal</c>/<c>VdokTotal2</c> (Fortran lines 1145, 1150).
/// Every scalar this test asserts is either an exact identity of the constructed
/// inputs (documented at the assertion) or the category-merge coverage already proven
/// against the formula script by <see cref="CategoriesTests"/>, which
/// <c>CycleStatistics.Compute</c> calls internally.
/// </summary>
public class CycleStatisticsTests
{
    /// <summary>
    /// A single-fraction, two-cell scenario built so every generator-accuracy stream
    /// averages exactly 0.5 (Eps1-7 = 0 identically, each against its own distinct
    /// counter) and the category distribution is concentrated in one cell (so the
    /// category-accuracy check is 0, isolating the fields this test actually wants to
    /// exercise: NaN-gating and the in-place mass-fraction rewrite); the pocket
    /// histogram (Qks) is split over two cells instead, so <c>Epsy</c> is an ordinary
    /// nonzero number here, not asserted by this test (BOOT.md's own narrowing: the
    /// rest of `Compute`'s pipeline, `Pockets` included, is not checked against an
    /// independent formula script this wave).
    /// </summary>
    private static readonly double[] ScenarioBounds = [0.5, 1.5];
    private static readonly double[] ScenarioCumulative = [0.0, 1.0];
    private static readonly double[] ScenarioShare = [1.0];
    private static readonly byte[] ScenarioPocketForming = [1];
    private static readonly double[] ScenarioMassShare = [1.0];

    private static (ModelSetup Setup, SetupTables Tables, SetupEchoes Echoes, long[] IntegerTotals, double[] RealTotals) BuildScenario()
    {
        const int ndok = 2;
        const int nkarm = 2;
        const int ncat = 1;
        const int nc = 2;
        const int nmm = 1;
        const double di = 1.0;
        const double dj = 1.0;

        var layout = AccumulatorLayout.Create(nmm, ndok, nkarm, ncat, nc);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];

        // Every stream's ten-term sum still averages to exactly 0.5 per draw (Eps1..Eps7
        // = |0.5 - 0.5| / 0.5 = 0, Fortran 771-784), but the five draw counters are now
        // pairwise distinct: with every counter equal to 10 (as this scenario first
        // read), a mutation that divided Eps5 by NFQ instead of NFZ -- undoing the
        // Fortran quirk that streams 4 and 5 both divide by NFZ, lines 777-780 -- would
        // still pass, because NFQ and NFZ held the same value. Xss is scaled to each
        // stream's own counter (Xss(k) = counter * 0.5) so every Eps stays exactly 0
        // while the counters themselves are load-bearing (found by the audit of this
        // node).
        const long nfx = 8, nfy = 12, nfz = 20, nfq = 6, nfw = 4;
        integerTotals[layout.Nfx] = nfx;
        integerTotals[layout.Nfy] = nfy;
        integerTotals[layout.Nfz] = nfz;
        integerTotals[layout.Nfq] = nfq;
        integerTotals[layout.Nfw] = nfw;

        realTotals[layout.Xss + 0] = nfx * 0.5;
        realTotals[layout.Xss + 1] = nfx * 0.5;
        realTotals[layout.Xss + 2] = nfy * 0.5;
        realTotals[layout.Xss + 3] = nfz * 0.5;
        realTotals[layout.Xss + 4] = nfz * 0.5;
        realTotals[layout.Xss + 5] = nfq * 0.5;
        realTotals[layout.Xss + 6] = nfw * 0.5;

        realTotals[layout.Sd4] = 1.0;
        realTotals[layout.Sd3] = 1.0;
        realTotals[layout.D41] = 1.0;
        realTotals[layout.D31] = 1.0;
        realTotals[layout.DokBase41] = 1.0;
        realTotals[layout.DokBase31] = 1.0;
        realTotals[layout.DokSur41] = 1.0;
        realTotals[layout.DokSur31] = 1.0;

        integerTotals[layout.AlldokFract] = 10; // the one fraction receives every draw.
        integerTotals[layout.Alldok + 0] = 4;
        integerTotals[layout.Alldok + 1] = 6;
        realTotals[layout.Allvdok + 0] = 4.0;
        realTotals[layout.Allvdok + 1] = 6.0;

        // Split over two cells (not concentrated in one): a concentrated histogram makes
        // MD3/MD4's own spread trivially 0 regardless of the cell-centre formula, which
        // would hide a mis-indexed cell centre (found by the audit of this node). QKSS
        // stays 10, as before.
        integerTotals[layout.Qks + 0] = 6;
        integerTotals[layout.Qks + 1] = 4;
        realTotals[layout.Vks + 0] = 10.0;
        realTotals[layout.Dp41] = 1.0;
        realTotals[layout.Dp31] = 1.0;

        // Categories: one row (Ncat = 1), same concentrated shape as Qks so DPRow stays 1 (no merge).
        realTotals[layout.DpMax] = 0.5; // floor(0.5/1)+1 = 1 row.
        integerTotals[layout.Qdoks + 0] = 10;
        realTotals[layout.Dokp41 + 0] = 5.0;
        realTotals[layout.Dokp31 + 0] = 1.0;

        realTotals[layout.Vdokstr + 0] = 5.0;
        realTotals[layout.Vdokstr + 1] = 5.0;

        // Cycle >= 1 only: FmkarmCor/Fmkarm2/FqkarmCor/Qmkm1/Qmkm2/Coef feed the
        // corrected-pocket and mass-fraction scalars. FmkarmCor/FqkarmCor are left
        // concentrated (nonzero sum) so Dkarm43Cor/DqkarmCor are ordinary numbers;
        // Fmkarm2 is left all-zero on purpose (BOOT.md, "IEEE results where the
        // original computes them ... empty corrected distributions"): Dfmk432 and
        // Sdevp243 must come out NaN (0/0) without any guard added.
        realTotals[layout.FmkarmCor + 0] = 3.0;
        realTotals[layout.FmkarmCor + 1] = 0.0;
        integerTotals[layout.FqkarmCor + 0] = 3;
        integerTotals[layout.FqkarmCor + 1] = 0;
        integerTotals[layout.Qmkm1 + 0] = 1;
        integerTotals[layout.Qmkm2 + 0] = 1;
        integerTotals[layout.Coef + 0] = 1;

        // Matrix / mass fractions: Dmin = 1 (with Di = 1) makes gdokleft = fmdok(1)*GGG
        // = 0.4*0.5 = 0.2, nonzero, so the in-place division by (1 - gdokleft/GGG) =
        // 0.6 actually changes VdokTotal/VdokTotal2 (a ratio of exactly 1.0 would make
        // the rewrite numerically invisible).
        realTotals[layout.Vsmkm + 0] = 1.0;
        realTotals[layout.Vsmkm + 1] = 1.0;
        realTotals[layout.Svd + 0] = 1.0;
        realTotals[layout.Svd + 1] = 1.0;
        realTotals[layout.VmkmTotal + 0] = 2.0;
        realTotals[layout.VmkmTotal + 1] = 2.0;
        realTotals[layout.VdokTotal + 0] = 3.0;
        realTotals[layout.VdokTotal + 1] = 3.0;
        realTotals[layout.VmkmTotal2] = 8.0;
        realTotals[layout.VdokTotal2] = 12.0;

        var setup = new ModelSetup
        {
            FractionCount = nmm,
            SizeLaw = 2,
            CellSize = di,
            CategoryStep = dj,
            Dmin = 1.0,
            Dmax = 2.0,
            Lambda = 1.0,
            Ak1 = 0.5,
            Ak2 = 2.0,
            Ak3 = 0.27,
            Ak4 = 4.7,
            Variant = 1,
            Alpha = 0.25,
            PocketCoefficient = 1.0,
            BridgeCoefficient = 1.0,
            NnMin = 0.0,
            NnMax = 1e9,
            Ndok = ndok,
            Nkarm = nkarm,
            Ncat = ncat,
            Nc = nc,
            NeighbourBudget = 1000,
            PocketRedrawBudget = 1000,
            Layout = layout,
        };

        var tables = new SetupTables(
            Bounds: ScenarioBounds,
            Cumulative: ScenarioCumulative,
            Share: ScenarioShare,
            PocketForming: ScenarioPocketForming,
            Zss: 1.0,
            MassShare: ScenarioMassShare);

        var echoes = new SetupEchoes(
            Dokm: 1.0, Doksd: 1.0, Ddokmax: 2.0, Dmax: 2.0,
            TailProbabilityModified: 0.0, OxidizerMassFractionEffective: 0.5);

        return (setup, tables, echoes, integerTotals, realTotals);
    }

    /// <summary>
    /// The constructed scenario's setup-plane inputs, taken straight from
    /// <see cref="BuildFormulation"/> and <see cref="BuildParameters"/> with no rounding:
    /// the scenario's setup is <see cref="PrecisionKind.Binary64"/>.
    /// </summary>
    private static SetupInputs BuildInputs()
    {
        var formulation = BuildFormulation();
        var parameters = BuildParameters();
        return new SetupInputs(
            formulation.OxidizerDensity, formulation.PropellantDensity, formulation.OxidizerMassFraction, formulation.MetalMassFraction,
            parameters.HomogenizedOxidizerFraction, parameters.EpsDok, parameters.AggregatedOxideFraction);
    }

    private static Formulation BuildFormulation()
    {
        return new Formulation(
            Name: "constructed",
            OxidizerDensity: 2.0,
            PropellantDensity: 2.0,
            OxidizerMassFraction: 0.5,
            MetalMassFraction: 0.5,
            Ak1: 0.5, Ak2: 2.0, Ak3: 0.27, Ak4: 4.7,
            SizeLawCode: 2,
            Cycles: 1,
            ParticlesPerCycle: 1,
            GeneratorWarmup: 0,
            // Never read by Compute in this scenario (PocketForming[0] = 1, so Matrix's
            // gdoksfr loop skips this fraction's MassShare entirely); the bounds only
            // need to be well-formed Length values, not physically matched to Di/Dj.
            Fractions: ImmutableArray.Create(new OxidizerFraction(1.0, Length.FromMicrometres(500.0), Length.FromMicrometres(1500.0))),
            PocketFormingFractions: null);
    }

    private static ModelParameters BuildParameters()
    {
        return ModelParameters.Default with
        {
            Dmin = Length.FromMicrometres(1e6), // 1.0 m exactly: fmdokIndex = (int)(Dmin/Di) = 1 for Di = 1 m.
            AggregatedOxideFraction = 0.0,
        };
    }

    /// <summary>
    /// BOOT.md, "IEEE results where the original computes them ... No guard is added":
    /// an all-empty pocket histogram (QKSS = 0) gives <c>Qks1</c> cells of
    /// <c>0.0 / 0.0</c> = <see cref="double.NaN"/>, not a defensive 0 (found by the
    /// audit of this node: the previous version of <c>CycleStatistics.Pockets</c>
    /// special-cased <c>qkssTotal == 0</c> to 0.0, a guard this invariant forbids).
    /// </summary>
    [Fact]
    public void ComputeGivesNaNPocketHistogramWhenQksIsAllZero()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var layout = setup.Layout;
        integerTotals[layout.Qks + 0] = 0;
        integerTotals[layout.Qks + 1] = 0;

        var report = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 0,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.Equal(0L, report.Qkss);
        Assert.True(double.IsNaN(report.Qks1[0]));
        Assert.True(double.IsNaN(report.Qks1[1]));
    }

    [Fact]
    public void ComputeGivesZeroGeneratorAccuraciesForTheConstructedScenario()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var report = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 0,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.Equal(0.0, report.Eps1);
        Assert.Equal(0.0, report.Eps2);
        Assert.Equal(0.0, report.Eps3);
        Assert.Equal(0.0, report.Eps4);
        Assert.Equal(0.0, report.Eps5);
        Assert.Equal(0.0, report.Eps6);
        Assert.Equal(0.0, report.Eps7);
    }

    /// <summary>Cycle 0: the fields only Fortran lines 1084-1175 compute stay NaN (the original never reaches that code for IPRIS = 0, BOOT.md "## Report").</summary>
    [Fact]
    public void ComputeLeavesCycleOnlyFieldsNaNInCycleZero()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var report = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 0,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.True(double.IsNaN(report.Dkarm43Cor));
        Assert.True(double.IsNaN(report.DolM1));
        Assert.True(double.IsNaN(report.DolM2));
        Assert.True(double.IsNaN(report.DolM3));
        Assert.True(double.IsNaN(report.ConvergenceEpsy));
    }

    /// <summary>Cycle 1: the same fields are ordinary numbers, and the empty <c>Fmkarm2</c> distribution (BOOT.md, "IEEE results where the original computes them") gives NaN without a guard.</summary>
    [Fact]
    public void ComputeFillsCycleOnlyFieldsInCycleOne()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var report = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 1,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.False(double.IsNaN(report.Dkarm43Cor));
        Assert.False(double.IsNaN(report.DolM1));
        Assert.False(double.IsNaN(report.DolM2));
        Assert.False(double.IsNaN(report.DolM3));
        Assert.True(double.IsNaN(report.Dfmk432)); // Fmkarm2 is all zero: 0/0.
        Assert.True(double.IsNaN(report.Sdevp243));
    }

    /// <summary>DolM1 = sum(VSMKM)*PLOTsmdok*(GGG-gdokleft) / (sum(SvD)*PLOT1*(1-GGG+gdokleft)) = 2*2*0.3 / (2*2*0.7) = 3/7 for the constructed scenario (GGG = 0.5, gdokleft = 0.4*0.5 = 0.2, PLOTsmdok = 2.0, both computed by <c>CycleStatistics.Matrix</c> from the same inputs -- BOOT.md line map, 1001-1016, 1137-1152).</summary>
    [Fact]
    public void ComputesDolM1ByTheFortranFormula()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var report = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 1,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.Equal(3.0 / 7.0, report.DolM1, precision: 9);
    }

    /// <summary>
    /// In-place rewrite (BOOT.md, "In-place rewrites of the totals"): VdokTotal and
    /// VdokTotal2 are divided by <c>1 - gdokleft/GGG</c> = 0.6 for this scenario, so
    /// the totals after the call must differ from what was written in
    /// <see cref="BuildScenario"/> (3.0 each, 12.0 for the scalar) -- a ratio of
    /// exactly 1.0 (gdokleft = 0) would make this rewrite numerically invisible, which
    /// is why <see cref="BuildScenario"/> deliberately sets Dmin so gdokleft != 0.
    /// </summary>
    [Fact]
    public void ComputeRewritesVdokTotalInPlaceForCyclesAtLeastOne()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var layout = setup.Layout;

        _ = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 1,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.Equal(3.0 / 0.6, realTotals[layout.VdokTotal + 0], precision: 9);
        Assert.Equal(3.0 / 0.6, realTotals[layout.VdokTotal + 1], precision: 9);
        Assert.Equal(12.0 / 0.6, realTotals[layout.VdokTotal2], precision: 9);
    }

    [Fact]
    public void ComputeLeavesVdokTotalUnrewrittenInCycleZero()
    {
        var (setup, tables, echoes, integerTotals, realTotals) = BuildScenario();
        var layout = setup.Layout;

        _ = CycleStatistics.Compute(
            setup, tables, echoes, BuildInputs(), cycleIndex: 0,
            integerTotals, realTotals, new double[setup.Ndok], out _, out _);

        Assert.Equal(3.0, realTotals[layout.VdokTotal + 0]);
        Assert.Equal(3.0, realTotals[layout.VdokTotal + 1]);
        Assert.Equal(12.0, realTotals[layout.VdokTotal2]);
    }
}
