using ILGPU.Runtime;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The setup plane under <see cref="PrecisionKind.Original"/> (BOOT.md, "## Setup
/// plane"): the frozen positive controls computed in advance, and the fixture-equality
/// comparison against <c>tests/Fixtures/formulas_statistics.py</c>'s independent
/// transcription for the five reference formulations and <c>p350</c>.
/// </summary>
public class SetupPlaneTests : IClassFixture<CpuHost>
{
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    private readonly CpuHost _host;

    public SetupPlaneTests(CpuHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Frozen before implementation (src/Statistics/ACCEPTANCE.md): under
    /// <see cref="PrecisionKind.Original"/>, <c>inpt</c> and <c>p350</c> are
    /// single-fraction formulations (<c>NMM = 1</c>), so the measured share
    /// (<c>AlldokFract(1)/ALLDOKQ</c>, a self-division of the same integer count) is
    /// mathematically exactly 1 (<c>CycleStatistics.OxidizerSizes</c>'s own
    /// <c>epsalldok[k] = |(fraction - tables.Share[k]) / tables.Share[k]|</c>, this
    /// node's "## Report", "Decisions where the port departs from a transcription");
    /// this test recomputes that same formula directly from <c>tables.Share</c> rather
    /// than running a cycle, since <c>fraction</c> never varies from 1.0 regardless of
    /// how many particles a run draws. Under <see cref="PrecisionKind.Binary64"/> both
    /// print <c>0.000E+00</c>, unchanged from every run before this task.
    /// </summary>
    [Theory]
    [InlineData("inpt", 2.7003e-8)]
    [InlineData("P350", 2.6290e-8)]
    public void PositiveControlEpsdokfrMatchesTheFrozenAnswer(string name, double expectedEpsdokfr0)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var originalStatus = Setup.Prepare(formulation, parameters, PrecisionKind.Original, NeighbourBudget, PocketRedrawBudget,
            out _, out var originalTables, out _, out _);
        Assert.Equal(SetupStatus.Ok, originalStatus);
        var originalEpsdokfr0 = Math.Abs((1.0 - originalTables.Share[0]) / originalTables.Share[0]);
        Assert.Equal(expectedEpsdokfr0, originalEpsdokfr0, 3); // matches at the printed E-notation's own three significant digits.

        var doubleStatus = Setup.Prepare(formulation, parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out _, out var doubleTables, out _, out _);
        Assert.Equal(SetupStatus.Ok, doubleStatus);
        var doubleEpsdokfr0 = Math.Abs((1.0 - doubleTables.Share[0]) / doubleTables.Share[0]);
        Assert.Equal(0.0, doubleEpsdokfr0);
    }

    /// <summary>
    /// The mechanism behind <c>fineoxy_fr</c> (BOOT.md, "## Setup plane",
    /// "`Dmin` rounds too; `Di`/`Dj` round too"): HMX's, HPEPA3's and P33's own default
    /// <c>Dmin</c> (10 μm) equals their lowest fraction's own lower bound (also 10 μm),
    /// so under <see cref="PrecisionKind.Original"/> the two must round to the same
    /// binary32 value, closing the sliver a fixed-point double comparison could land a
    /// draw in. Proven red on the violation this fixes, by a reverted mutation of
    /// <see cref="Setup.Prepare"/> itself: with <c>dminMetres</c> reverted to plain
    /// <c>parameters.Dmin.Metres</c> (the pre-fix line), all three cases failed —
    /// <c>Expected: 9.9999997473787516E-06, Actual: 1.0000000000000001E-05</c>, one
    /// binary32 ULP apart — reverted immediately after, recorded here rather than kept
    /// as a committed mutation since the fix touches the method under test itself
    /// (BOOT.md, "## Setup plane"). <c>inpt</c>'s and <c>PSAN02n</c>'s own lowest
    /// bounds (257 μm, 160.3388 μm) do not coincide with the default <c>Dmin</c>, so
    /// they are not cases of this test; the fix still rounds their own <c>Dmin</c>
    /// (unaffected by whether a coincidence exists to expose it).
    /// </summary>
    [Theory]
    [InlineData("HMX")]
    [InlineData("HPEPA3")]
    [InlineData("P33")]
    public void DefaultDminRoundsToTheSameBinary32ValueAsTheLowestFractionBound(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var status = Setup.Prepare(formulation, parameters, PrecisionKind.Original, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);

        Assert.Equal(tables.Bounds[0], setup.Dmin); // bit for bit: no sliver between Dmin and the lowest fraction's own lower bound.
    }

    /// <summary>
    /// The mechanism behind cell 0 of the printed <c>fmdok</c> (BOOT.md, "## Setup
    /// plane", "`Di`/`Dj` round too"): under <see cref="PrecisionKind.Original"/>, a
    /// particle drawn at the default <c>Dmin</c> (10 μm, equal to the lowest fraction
    /// bound on HMX, HPEPA3 and P33) must bin to cell 1, not cell 0 --
    /// <c>int(Dmin/Di)</c> is exactly 1 only when <c>Dmin</c> and <c>Di</c> round to the
    /// <em>same</em> binary32 value, which needs <c>Di</c>'s own store rounding, not
    /// only <c>Dmin</c>'s (`src/Statistics/BOOT.md`, "## Report": "`fmdokIndex = 1` on
    /// all five, so"). Proven red
    /// on the violation this fixes, by a reverted mutation of <see cref="Setup.Prepare"/>
    /// itself: with <c>cellSizeMetres</c> reverted to plain
    /// <c>parameters.CellSize.Metres</c> (the pre-fix line), all three cases gave
    /// <c>int(Dmin/Di) = 0</c> (<c>9.9999997473787516E-06 / 1.0E-05 =
    /// 0.99999997...</c>), reverted immediately after, recorded here rather than kept as
    /// a committed mutation since the fix touches the method under test itself (the same
    /// pattern the <c>Dmin</c> test above already uses).
    /// </summary>
    [Theory]
    [InlineData("HMX")]
    [InlineData("HPEPA3")]
    [InlineData("P33")]
    public void DefaultCellSizeRoundsToTheSameBinary32ValueAsDminSoTheLowestBoundBinsToCellOne(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{name}.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var status = Setup.Prepare(formulation, parameters, PrecisionKind.Original, NeighbourBudget, PocketRedrawBudget,
            out var setup, out _, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);

        Assert.Equal(setup.Dmin, setup.CellSize); // bit for bit: Dmin and Di (both default 10 um) round to the same binary32 value.
        Assert.Equal(1, (int)(setup.Dmin / setup.CellSize)); // FMDOK(int(Dmin/Di)+1): the original's own index, all five formulations.
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var c in FormulaCaseFiles.ReadSetupPlaneCases())
        {
            yield return new object[] { c.Name };
        }
    }

    /// <summary>
    /// Under <see cref="PrecisionKind.Original"/>, <c>Z11</c>, <c>ZX</c>, <c>ZSS</c>, λ,
    /// <c>Dmax</c> and the echoes equal, bit for bit, <c>tests/Fixtures/
    /// formulas_statistics.py</c>'s independent transcription of the Fortran's own
    /// arithmetic (src/Statistics/ACCEPTANCE.md). <c>Dmax</c> needs the driver's own
    /// hand-off through <c>Particle.SizeLaw.Sample</c> (API.md, "Setup hand-off"): the
    /// sampled diameter is fully determined by this node's own deterministic tail draw
    /// (<c>TailDraw.X</c>/<c>X1</c>), no random number involved, so the fixture script
    /// reproduces it too, by importing <c>formulas_particle.py</c>'s own already-verified
    /// <c>size_law_sample</c> rather than re-deriving it (root BOOT.md taboo: no second
    /// implementation of any part of the particle program).
    /// </summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void SetupPlaneMatchesTheFormulaScript(string caseName)
    {
        var c = FormulaCaseFiles.FindSetupPlaneCase(caseName);

        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", c.DatFile);
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default;

        var status = Setup.Prepare(formulation, parameters, PrecisionKind.Original, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out var draw, out var pending);
        Assert.Equal(SetupStatus.Ok, status);

        using var boundsBuffer = _host.Accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = _host.Accelerator.Allocate1D(tables.Cumulative);
        Particle.SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, boundsBuffer.View.BaseView, cumulativeBuffer.View.BaseView,
            draw.X, draw.X1, out var dmax, out _);
        var echoes = Setup.CompleteEchoes(ref setup, pending, dmax);

        Assert.Equal(c.Expected.Z11, tables.Cumulative);
        Assert.Equal(c.Expected.Zx, tables.Share);
        Assert.Equal(c.Expected.Zss, tables.Zss);
        Assert.Equal(c.Expected.Lambda, setup.Lambda);
        Assert.Equal(c.Expected.Dmax, echoes.Dmax);
        Assert.Equal(c.Expected.Dokm, echoes.Dokm);
        Assert.Equal(c.Expected.Doksd, echoes.Doksd);
        Assert.Equal(c.Expected.Ddokmax, echoes.Ddokmax);
        Assert.Equal(c.Expected.TailProbabilityModified, echoes.TailProbabilityModified);
        Assert.Equal(c.Expected.OxidizerMassFractionEffective, echoes.OxidizerMassFractionEffective);
    }
}
