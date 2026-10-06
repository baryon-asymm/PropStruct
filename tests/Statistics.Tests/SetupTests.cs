using System.Collections.Immutable;
using ILGPU.Runtime;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L0 of the BOOT.md table: <c>Setup.Prepare</c> against the printed setup quantities
/// of the five reference formulations' <c>results.m</c>, one constructed violation per
/// <see cref="SetupStatus"/>, and the two-phase hand-off of API.md ("Setup hand-off")
/// round-tripped through <c>Particle.SizeLaw.Sample</c>.
/// </summary>
public class SetupTests : IClassFixture<CpuHost>
{
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    private readonly CpuHost _host;

    public SetupTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> ReferenceFormulations()
    {
        yield return new object[] { "HPEPA3" };
        yield return new object[] { "inpt" };
        yield return new object[] { "P33" };
        yield return new object[] { "PSAN02n" };
        yield return new object[] { "HMX" };
    }

    /// <summary>
    /// The setup echoes the reference `results.m` prints (`Dok43a`, `Dok43sd`,
    /// `Ddok_max`), and, for the modified tail probability, an internal-consistency
    /// check: every reference formulation's own `alfa` is 0 (results.m's own "P(alpha)"
    /// echo, checked once here rather than typed), so PARAM's exclusion loop never
    /// fires and the modified alfa must equal the unmodified one. None of these three
    /// quantities needs the sampled `Dmax` (API.md, "Setup hand-off"), so this test
    /// reads them straight off <c>Prepare</c>'s own <c>pending</c> echoes without
    /// performing the tail draw itself (that round trip is
    /// <see cref="PrepareAndCompleteEchoesRoundTripThroughSizeLawSample"/>'s own row).
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void PrepareMatchesThePrintedSetupQuantitiesOfItsReferenceFormulation(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var resultsPath = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");

        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;
        Assert.Equal(0.0, parameters.TailProbability);

        var status = Setup.Prepare(formulation, parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out _, out _, out _, out var pending);

        Assert.Equal(SetupStatus.Ok, status);

        var cells = ResultsMFile.Parse(resultsPath);

        Assert.Equal(cells["Dok43a"][0], pending.Dokm * 1e6, 2);
        Assert.Equal(cells["Dok43sd"][0], Math.Sqrt(pending.Doksd) * 1e6, 2);
        Assert.Equal(cells["Ddok_max"][0], pending.Ddokmax * 1e6, 2);
        Assert.Equal(parameters.TailProbability, pending.TailProbabilityModified);
    }

    /// <summary>
    /// The two-phase hand-off of API.md, "Setup hand-off": <c>Prepare</c>'s own
    /// <c>TailDraw</c> feeds <c>Particle.SizeLaw.Sample</c> directly (built through this
    /// test's own <c>CpuHost</c> accelerator, never one <c>Statistics</c> stands up
    /// itself — the audit finding behind blocker 3), and <c>CompleteEchoes</c> writes
    /// the sampled diameter into both <c>setup.Dmax</c> and the returned
    /// <see cref="SetupEchoes"/>.
    /// </summary>
    [Fact]
    public void PrepareAndCompleteEchoesRoundTripThroughSizeLawSample()
    {
        var formulation = ValidFormulation();
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out var draw, out var pending);
        Assert.Equal(SetupStatus.Ok, status);

        using var boundsBuffer = _host.Accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = _host.Accelerator.Allocate1D(tables.Cumulative);

        Particle.SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, boundsBuffer.View.BaseView, cumulativeBuffer.View.BaseView,
            draw.X, draw.X1, out var dmax, out var fraction);

        Assert.InRange(fraction, 0, setup.FractionCount - 1);
        Assert.Equal(0.0, setup.Dmax); // Not yet completed.

        var echoes = Setup.CompleteEchoes(ref setup, pending, dmax);

        Assert.Equal(dmax, setup.Dmax);
        Assert.Equal(dmax, echoes.Dmax);
        Assert.Equal(pending.Dokm, echoes.Dokm);
        Assert.Equal(pending.Doksd, echoes.Doksd);
        Assert.Equal(pending.Ddokmax, echoes.Ddokmax);
        Assert.Equal(pending.TailProbabilityModified, echoes.TailProbabilityModified);
        Assert.Equal(pending.OxidizerMassFractionEffective, echoes.OxidizerMassFractionEffective);
    }

    /// <summary>
    /// <c>Ndok</c> against the archived, directly-printed evidence: the category row
    /// `fqdokkarm(1,:)` of each formulation's own reference output is exactly `Ndok`
    /// cells long, read from the archive through <c>Harness.ResultsMFile</c> rather than
    /// typed by hand (root BOOT.md, "Invariants": found by the audit of this node —
    /// the previous version of this test typed the printed lengths as
    /// <c>InlineData</c> literals instead of reading them, contradicting the very claim
    /// this doc comment made about it).
    /// </summary>
    [Theory]
    [InlineData("C166", "rc166.m.txt")]
    [InlineData("T56", "rt56.m.txt")]
    [InlineData("CSPX01", "rcspx01.m.txt")]
    [InlineData("P18050", "r_p18050.m.txt")]
    [InlineData("PSAN01", "rps01.m.txt")]
    public void SizesNdokMatchesThePrintedCategoryRowLengthOfItsArchivedFormulation(string name, string archivedOutput)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        Setup.Sizes(formulation.Fractions, parameters.CellSize, parameters.CategoryStep, formulation.Ak4,
            out _, out var ndok, out _, out _, out _);

        var outputPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "outputs", archivedOutput);
        var cells = ResultsMFile.Parse(outputPath);
        var key = cells.Keys.Single(k => k.Replace(" ", "") == "fqdokkarm(1,:)");

        Assert.Equal(cells[key].Length, ndok);
    }

    /// <summary>
    /// Proves the binary32 array-size evidence non-degenerate (AGENTS.md §13): the same
    /// C166 computation done in plain <c>double</c> (no binary32 rounding at all) gives
    /// <c>Ndok = 72</c>, not 71 (root BOOT.md, "the fidelity audit of Statistics showed
    /// that sizes from double operands change the printed array lengths... C166: 72
    /// cells instead of 71"). Mutation record: BOOT.md of this node.
    /// </summary>
    [Fact]
    public void SizesNdokOfC166WouldBeSeventyTwoWithoutBinary32Rounding()
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "C166.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var di = parameters.CellSize.Metres; // plain double, no binary32 rounding.
        var ddokmax = 0.0;
        foreach (var fraction in formulation.Fractions)
        {
            if (fraction.UpperBound.Metres > ddokmax)
            {
                ddokmax = fraction.UpperBound.Metres;
            }
        }

        var ndokWithoutBinary32 = (int)(ddokmax / di) + 2;
        Assert.Equal(72, ndokWithoutBinary32);

        Setup.Sizes(formulation.Fractions, parameters.CellSize, parameters.CategoryStep, formulation.Ak4,
            out _, out var ndok, out _, out _, out _);
        Assert.Equal(71, ndok);
    }

    /// <summary>
    /// S-2 (audit of 2026-09-24): <c>Nkarm</c>'s own numerator, <c>Ddokmax*AK4</c>, is
    /// never stored to a variable of its own in the original, so rounding it a second
    /// time before the truncated division -- what <c>Setup.Sizes</c> used to do, through
    /// <c>Binary32.TruncatedQuotient</c> -- double-rounds it and can move the truncated
    /// integer at a boundary built to sit on it, the same shape as the C166 evidence for
    /// <c>Ndok</c> (<c>Binary32Tests.TruncatedQuotientAvoidsDoubleRoundingAtAnIntegerBoundary</c>).
    /// The unrounded product's own quotient here sits just under 1506
    /// (1505.9999589...); rounding the product first pushes it just over
    /// (1506.0000213...) -- correct is 1507 (1505 + 2), the old, double-rounded reading
    /// 1508 (<c>Binary32Tests.TruncatedQuotientOfProductLeavesTheProductUnroundedAtAnIntegerBoundary</c>'s
    /// own numbers). <c>categoryStep</c> is kept off any boundary of its own, so this
    /// case exercises <c>Nkarm</c> alone.
    /// </summary>
    [Fact]
    public void SizesUsesTheUnroundedProductForNkarm()
    {
        var fractions = ImmutableArray.Create(new OxidizerFraction(1.0, Length.FromMetres(1e-9), Length.FromMetres(7.669682236155495e-05)));
        var cellSize = Length.FromMetres(6.682486741738103e-07);
        var categoryStep = Length.FromMetres(1e-6);

        Setup.Sizes(fractions, cellSize, categoryStep, 13.121566772460938,
            out _, out _, out var nkarm, out _, out _);

        Assert.Equal(1507, nkarm);
    }

    /// <summary>
    /// S-2's own dual for <c>Ncat</c>, the second array this defect reaches (BOOT.md,
    /// "Array sizes from binary32 values": "the same treatment covers ... `Ncat`").
    /// Built the same way as <see cref="SizesUsesTheUnroundedProductForNkarm"/>, on
    /// <c>categoryStep</c> instead of <c>cellSize</c>: the unrounded product's own
    /// quotient sits just over 4389 (4389.0000137...); rounding the product first pulls
    /// it just under (4388.9999236...) -- correct is 4391 (4389 + 2), the old,
    /// double-rounded reading 4390.
    /// </summary>
    [Fact]
    public void SizesUsesTheUnroundedProductForNcat()
    {
        var fractions = ImmutableArray.Create(new OxidizerFraction(1.0, Length.FromMetres(1e-9), Length.FromMetres(0.0006847677868790925)));
        var cellSize = Length.FromMetres(1e-6);
        var categoryStep = Length.FromMetres(1.7806955838750582e-06);

        Setup.Sizes(fractions, cellSize, categoryStep, 11.413318634033203,
            out _, out _, out _, out var ncat, out _);

        Assert.Equal(4391, ncat);
    }

    /// <summary>
    /// Positive control for S-2, on a real, archive-anchored formulation rather than a
    /// constructed boundary (C166's own <c>Ndok</c> is independently checked against its
    /// archived output, <see cref="SizesNdokMatchesThePrintedCategoryRowLengthOfItsArchivedFormulation"/>).
    /// C166's <c>Ddokmax*AK4/Di</c> quotient sits at 328.99997574826597, 1.68e-5 short of
    /// the 329 boundary (`src/Statistics/BOOT.md`, "Invariants", "Array sizes from binary32 values":
    /// "the worst-case DPRow quotient Ddokmax*AK4/Dj -- the same one Ncat is sized
    /// from ... sits no closer than 1.68e-5 from an integer boundary (HMX, C166, T56)")
    /// -- close, but on neither reading does it cross, so both the fixed and the old,
    /// double-rounded reading agree at 330 here. This formulation cannot by itself prove
    /// the fix (the constructed boundary cases above do that); it is the audit's own
    /// finding recorded as a regression guard: "no archived formulation whose
    /// Nkarm/Ncat changes either way".
    /// </summary>
    [Fact]
    public void SizesNkarmAndNcatOfC166AreUnaffectedByTheProductRoundingFix()
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "C166.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        Setup.Sizes(formulation.Fractions, parameters.CellSize, parameters.CategoryStep, formulation.Ak4,
            out _, out _, out var nkarm, out var ncat, out _);

        Assert.Equal(330, nkarm);
        Assert.Equal(330, ncat);
    }

    /// <summary>AK1 &gt; 0 (line 1704's <c>Z</c> divides by no AK1 term itself, but the coefficient gates the whole matrix; BOOT.md, Constraints).</summary>
    [Fact]
    public void PrepareRejectsInvalidCoefficientsWhenAk1IsNotPositive()
    {
        var formulation = ValidFormulation() with { Ak1 = 0.0 };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidCoefficients, status);
    }

    /// <summary>
    /// AK2 &gt; 1: the precondition <c>SmallParticles.Probability</c>'s own indexing
    /// rests on (BOOT.md, Constraints: "the index of lines 1031 and 1039 stays within
    /// Ndok"; <c>SmallParticles.cs</c>, <c>wholeFractions = (int)(kilo / ak2)`, read as
    /// an index into <c>vdokstr[wholeFractions]</c> — AK2 &lt;= 1 lets
    /// <c>wholeFractions</c> reach or exceed <c>kilo</c>, and for <c>kilo = ndok</c>
    /// that indexes past the array).
    /// </summary>
    [Fact]
    public void PrepareRejectsInvalidCoefficientsWhenAk2IsNotAboveOne()
    {
        var formulation = ValidFormulation() with { Ak2 = 1.0 };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidCoefficients, status);
    }

    /// <summary>AK3 &gt; 0 (Fortran lines 1704, 1716's own AK3-derived bounds).</summary>
    [Fact]
    public void PrepareRejectsInvalidCoefficientsWhenAk3IsNotPositive()
    {
        var formulation = ValidFormulation() with { Ak3 = 0.0 };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidCoefficients, status);
    }

    /// <summary>AK3 &lt; AK4 (BOOT.md, Constraints).</summary>
    [Fact]
    public void PrepareRejectsInvalidCoefficientsWhenAk3IsNotBelowAk4()
    {
        var formulation = ValidFormulation() with { Ak3 = 5.0, Ak4 = 4.7 };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidCoefficients, status);
    }

    [Fact]
    public void PrepareRejectsInvalidCellSize()
    {
        var parameters = ModelParameters.Default with { CellSize = Length.FromMetres(0.0) };
        var status = Setup.Prepare(ValidFormulation(), parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidCellSize, status);
    }

    [Fact]
    public void PrepareRejectsInvalidMinimumSize()
    {
        // Ddokmax is 315 um (the single fraction's upper bound); Dmin above it is invalid.
        var parameters = ModelParameters.Default with { Dmin = Length.FromMicrometres(1000.0) };
        var status = Setup.Prepare(ValidFormulation(), parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidMinimumSize, status);
    }

    [Fact]
    public void PrepareRejectsInvalidFractionBounds()
    {
        var formulation = ValidFormulation();
        var badFraction = formulation.Fractions[0] with { UpperBound = formulation.Fractions[0].LowerBound };
        formulation = formulation with { Fractions = ImmutableArray.Create(badFraction) };

        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidFractionBounds, status);
    }

    [Fact]
    public void PrepareRejectsZeroFractionShare()
    {
        var formulation = ValidFormulation();
        var badFraction = formulation.Fractions[0] with { MassShare = 0.0 };
        formulation = formulation with { Fractions = ImmutableArray.Create(badFraction) };

        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.ZeroFractionShare, status);
    }

    [Fact]
    public void PrepareRejectsNoPocketFormingFraction()
    {
        var formulation = ValidFormulation() with { PocketFormingFractions = ImmutableArray.Create(0) };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.NoPocketFormingFraction, status);
    }

    /// <summary>
    /// D7 (audit of 2026-09-24): a flag of 256 narrows to <c>(byte)256 == 0</c> under
    /// the old <c>(byte)flags[i]</c> cast, so the single fraction of
    /// <see cref="ValidFormulation"/> would read as not pocket-forming and the run would
    /// wrongly fail with <see cref="SetupStatus.NoPocketFormingFraction"/> (Fortran line
    /// 473, <c>SFR(Nfract) == 0</c>: zero stays zero, any nonzero flag forms pockets).
    /// The fix maps by that test directly, so this and every other nonzero flag are
    /// accepted regardless of their own magnitude.
    /// </summary>
    [Fact]
    public void PrepareTreatsAnyNonzeroPocketFormingFlagAsForming()
    {
        var formulation = ValidFormulation() with { PocketFormingFractions = ImmutableArray.Create(256) };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out _, out var tables, out _, out _);

        Assert.Equal(SetupStatus.Ok, status);
        Assert.Equal((byte)1, tables.PocketForming[0]); // Normalized to exactly 1, not the raw 256 (SetupTables.cs: "1 where the fraction forms pockets").
    }

    /// <summary>
    /// D7's own second half: a <c>Formulation</c> built in code (not through
    /// <c>DatFile.Read</c>, which always reads exactly <c>NMM</c> flags when it reads
    /// any) can hold fewer pocket-forming flags than fractions. Indexing past the flag
    /// array's own end used to throw <see cref="IndexOutOfRangeException"/>, which the
    /// root invariant "Failures are values" forbids; the fix reports
    /// <see cref="SetupStatus.InvalidPocketFormingFractionCount"/> instead.
    /// </summary>
    [Fact]
    public void PrepareRejectsInvalidPocketFormingFractionCount()
    {
        var formulation = ValidFormulation() with
        {
            Fractions = ImmutableArray.Create(
                new OxidizerFraction(0.5, Length.FromMicrometres(160.0), Length.FromMicrometres(250.0)),
                new OxidizerFraction(0.5, Length.FromMicrometres(250.0), Length.FromMicrometres(315.0))),
            PocketFormingFractions = ImmutableArray.Create(1), // One flag for two fractions.
        };

        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidPocketFormingFractionCount, status);
    }

    [Fact]
    public void PrepareRejectsInvalidTailProbability()
    {
        var parameters = ModelParameters.Default with { TailProbability = 1.0 };
        var status = Setup.Prepare(ValidFormulation(), parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidTailProbability, status);
    }

    [Fact]
    public void PrepareRejectsInvalidOxidizerFraction()
    {
        var formulation = ValidFormulation() with { OxidizerMassFraction = 0.0 };
        var status = Setup.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidOxidizerFraction, status);
    }

    /// <summary>
    /// <c>NnMax &lt;= 0</c>: <c>nn = ipocket_loc/ibridge_loc</c> (<c>Attempt.Run</c>,
    /// line 722) is always <c>&gt;= 0</c>, so every attempt of every cycle &gt;= 1 would
    /// fail the acceptance test of line 735 (BOOT.md, Constraints: the same "no particle
    /// completes" failure the <c>GGG</c> precondition already guards, discovered when a
    /// run with <c>NnMax = 0.0</c> took 4 minutes to exhaust
    /// <c>MaxAttemptsPerParticle</c> instead of failing immediately).
    /// </summary>
    [Fact]
    public void PrepareRejectsInvalidNnWindowWhenNnMaxIsNotPositive()
    {
        var parameters = ModelParameters.Default with { NnMax = 0.0 };
        var status = Setup.Prepare(ValidFormulation(), parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidNnWindow, status);
    }

    /// <summary>
    /// <c>NnMin &gt;= NnMax</c>: the accepted window is empty (or a single point), the
    /// dual of the clause above (BOOT.md, Constraints).
    /// </summary>
    [Fact]
    public void PrepareRejectsInvalidNnWindowWhenNnMinIsNotBelowNnMax()
    {
        var parameters = ModelParameters.Default with { NnMin = 100.0, NnMax = 50.0 };
        var status = Setup.Prepare(ValidFormulation(), parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.InvalidNnWindow, status);
    }

    [Fact]
    public void PrepareAcceptsAValidFormulation()
    {
        var status = Setup.Prepare(ValidFormulation(), ModelParameters.Default, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget, out _, out _, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);
    }

    /// <summary>A minimal, valid single-fraction formulation every precondition test starts from, changing exactly one field to violate exactly one precondition (BOOT.md, "one constructed violation per precondition status").</summary>
    private static Formulation ValidFormulation()
    {
        return new Formulation(
            Name: "constructed",
            OxidizerDensity: 1950.0,
            PropellantDensity: 1800.0,
            OxidizerMassFraction: 0.5,
            MetalMassFraction: 0.2,
            Ak1: 0.5,
            Ak2: 2.0,
            Ak3: 0.27,
            Ak4: 4.7,
            SizeLawCode: 2,
            Cycles: 1,
            ParticlesPerCycle: 1000,
            GeneratorWarmup: 6e6,
            Fractions: ImmutableArray.Create(new OxidizerFraction(1.0, Length.FromMicrometres(160.0), Length.FromMicrometres(315.0))),
            PocketFormingFractions: null);
    }
}
