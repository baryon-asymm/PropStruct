using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The seam of the <c>Corrected</c> model (<c>src/Statistics/ACCEPTANCE.md</c>, "Corrected, the seam"): a new overload
/// that takes a <see cref="CorrectionSet"/> equals its old member bit for bit under <see cref="CorrectionSet.None"/>,
/// and under any set with <see cref="PrecisionKind.Original"/>, on every formulation the repository ships; the old
/// members are the ones every other test of the node already holds to the executable and the approved ratchets.
/// </summary>
public class CorrectionSeamTests
{
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    public static IEnumerable<object[]> FormulationsUnderBothKinds() =>
        ShippedFormulations.Names.SelectMany(name => new[]
        {
            new object[] { name, false },
            new object[] { name, true },
        });

    /// <summary>
    /// Right: <c>Prepare</c> under <see cref="CorrectionSet.None"/> returns the old overload's status, setup, tables,
    /// draw, pending echoes and inputs, bit for bit; the setup's own <c>Corrections</c> is <c>None</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(FormulationsUnderBothKinds))]
    public void PrepareUnderNoneEqualsTheOldOverload(string name, bool original)
    {
        var kind = original ? PrecisionKind.Original : PrecisionKind.Binary64;
        var formulation = ShippedFormulations.Read(name);
        var oldResult = PrepareOld(formulation, kind);
        var newResult = PrepareNew(formulation, kind, CorrectionSet.None);

        Assert.Empty(BitwiseComparison.Differences(oldResult, newResult, name));
        Assert.Equal(CorrectionSet.None, newResult.Setup.Corrections);
    }

    /// <summary>
    /// Under <see cref="PrecisionKind.Original"/> the whole <see cref="CorrectionSet.All"/> is replaced by
    /// <see cref="CorrectionSet.None"/> on entry: the result is the old overload's and the stored set is <c>None</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(ShippedNames))]
    public void PrepareUnderOriginalReplacesEveryFlagByNone(string name)
    {
        var formulation = ShippedFormulations.Read(name);
        var newResult = PrepareNew(formulation, PrecisionKind.Original, CorrectionSet.All);

        Assert.Empty(BitwiseComparison.Differences(PrepareOld(formulation, PrecisionKind.Original), newResult, name));
        Assert.Equal(CorrectionSet.None, newResult.Setup.Corrections);
    }

    /// <summary>
    /// <c>AnalyticSizes</c> on the tables and shares <c>Prepare</c> hands back: the overload under
    /// <see cref="CorrectionSet.None"/>, and under <see cref="CorrectionSet.All"/> with
    /// <see cref="PrecisionKind.Original"/>, returns the old overload's <c>DOKM</c> and <c>DOKSD</c> bit for bit.
    /// </summary>
    [Theory]
    [MemberData(nameof(FormulationsUnderBothKinds))]
    public void AnalyticSizesUnderNoneAndUnderOriginalEqualTheOldOverload(string name, bool original)
    {
        var kind = original ? PrecisionKind.Original : PrecisionKind.Binary64;
        var formulation = ShippedFormulations.Read(name);
        var prepared = PrepareOld(formulation, kind);
        Assert.Equal(SetupStatus.Ok, prepared.Status);
        var shares = formulation.Fractions.Length;
        var corrections = original ? CorrectionSet.All : CorrectionSet.None;

        Setup.AnalyticSizes(formulation.SizeLawCode, shares, prepared.Tables.MassShare, prepared.Tables.Share, prepared.Tables.Bounds,
            kind, out var oldDokm, out var oldDoksd);
        Setup.AnalyticSizes(formulation.SizeLawCode, shares, prepared.Tables.MassShare, prepared.Tables.Share, prepared.Tables.Bounds,
            kind, corrections, out var newDokm, out var newDoksd);

        Assert.Empty(BitwiseComparison.Differences((oldDokm, oldDoksd), (newDokm, newDoksd), name));
    }

    /// <summary>
    /// <c>FractionLaw.Build</c> on the formulation's own masses and bounds: the overload under
    /// <see cref="CorrectionSet.None"/>, and under <see cref="CorrectionSet.All"/> with
    /// <see cref="PrecisionKind.Original"/>, returns the old overload's tables and sum bit for bit.
    /// </summary>
    [Theory]
    [MemberData(nameof(FormulationsUnderBothKinds))]
    public void FractionLawBuildUnderNoneAndUnderOriginalEqualTheOldOverload(string name, bool original)
    {
        var kind = original ? PrecisionKind.Original : PrecisionKind.Binary64;
        var formulation = ShippedFormulations.Read(name);
        var prepared = PrepareOld(formulation, kind);
        Assert.Equal(SetupStatus.Ok, prepared.Status);
        var corrections = original ? CorrectionSet.All : CorrectionSet.None;

        FractionLaw.Build(formulation.SizeLawCode, formulation.Fractions.Length, prepared.Tables.MassShare, prepared.Tables.Bounds,
            kind, out var oldShare, out var oldCumulative, out var oldSum);
        FractionLaw.Build(formulation.SizeLawCode, formulation.Fractions.Length, prepared.Tables.MassShare, prepared.Tables.Bounds,
            kind, corrections, out var newShare, out var newCumulative, out var newSum);

        Assert.Empty(BitwiseComparison.Differences((oldShare, oldCumulative, oldSum), (newShare, newCumulative, newSum), name));
    }

    public static IEnumerable<object[]> ProbabilityCasesUnderBothKinds() =>
        FormulaCaseFiles.ReadSmallParticlesProbabilityCases().SelectMany(c => new[]
        {
            new object[] { c.Name, false },
            new object[] { c.Name, true },
        });

    /// <summary>
    /// <c>SmallParticles.Probability</c> on every case of <c>small_particles_probability.json</c>: the overload under
    /// <see cref="CorrectionSet.None"/>, and under <see cref="CorrectionSet.All"/> with <see cref="PrecisionKind.Original"/>,
    /// returns the old overload's <c>pdoksmall</c> bit for bit.
    /// </summary>
    [Theory]
    [MemberData(nameof(ProbabilityCasesUnderBothKinds))]
    public void ProbabilityUnderNoneAndUnderOriginalEqualsTheOldOverload(string caseName, bool original)
    {
        var c = FormulaCaseFiles.FindSmallParticlesProbabilityCase(caseName);
        var kind = original ? PrecisionKind.Original : PrecisionKind.Binary64;
        var corrections = original ? CorrectionSet.All : CorrectionSet.None;

        var oldResult = SmallParticles.Probability(
            c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(), kind);
        var newResult = SmallParticles.Probability(
            c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(), kind,
            corrections);

        Assert.Empty(BitwiseComparison.Differences(oldResult, newResult, caseName));
    }

    /// <summary>
    /// Red, the comparison itself for <c>Probability</c>: on every case with <c>gdokleft &gt; 0</c> the scaling flag applied
    /// under <see cref="PrecisionKind.Binary64"/> is a finding of the same comparison, so that the equalities above are
    /// not vacuous.
    /// </summary>
    [Fact]
    public void AFlagAppliedUnderNoneIsAFindingOnEveryCaseWithLeftOxidizer()
    {
        var cases = FormulaCaseFiles.ReadSmallParticlesProbabilityCases().Where(c => c.Gdokleft > 0.0).ToList();
        Assert.NotEmpty(cases);

        foreach (var c in cases)
        {
            var oldResult = SmallParticles.Probability(
                c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(),
                PrecisionKind.Binary64);
            var flagged = SmallParticles.Probability(
                c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(),
                PrecisionKind.Binary64, CorrectionSet.ZdoksmallCoarseShare);

            Assert.NotEmpty(BitwiseComparison.Differences(oldResult, flagged, c.Name));
        }
    }

    public static IEnumerable<object[]> CategoryCasesUnderBothKinds() =>
        FormulaCaseFiles.ReadCategoriesCases().SelectMany(c => new[]
        {
            new object[] { c.Name, false },
            new object[] { c.Name, true },
        });

    /// <summary>
    /// <c>Categories.MergeAndDescribe</c> on every case of <c>categories.json</c>: the overload under
    /// <see cref="CorrectionSet.None"/>, and under <see cref="CorrectionSet.All"/> with <see cref="PrecisionKind.Original"/>,
    /// reports and rewrites the totals as the old overload does, bit for bit.
    /// </summary>
    [Theory]
    [MemberData(nameof(CategoryCasesUnderBothKinds))]
    public void MergeAndDescribeUnderNoneAndUnderOriginalEqualsTheOldOverload(string caseName, bool original)
    {
        var c = FormulaCaseFiles.FindCategoriesCase(caseName);
        var kind = original ? PrecisionKind.Original : PrecisionKind.Binary64;
        var oldRun = CategoryRun.From(c);
        var newRun = CategoryRun.From(c);

        var oldOutcome = oldRun.MergeWithoutSet(kind);
        var newOutcome = newRun.Merge(kind, original ? CorrectionSet.All : CorrectionSet.None);

        Assert.Empty(BitwiseComparison.Differences((oldOutcome, oldRun.IntegerTotals, oldRun.RealTotals),
            (newOutcome, newRun.IntegerTotals, newRun.RealTotals), caseName));
    }

    /// <summary>
    /// Red, the comparison itself for <c>MergeAndDescribe</c>: on every case the copy flag applied under
    /// <see cref="PrecisionKind.Binary64"/> leaves the totals unrewritten and is a finding of the same comparison.
    /// </summary>
    [Fact]
    public void AFlagAppliedUnderNoneIsAFindingOnEveryCategoryCase()
    {
        foreach (var c in FormulaCaseFiles.ReadCategoriesCases())
        {
            var oldRun = CategoryRun.From(c);
            var flagged = CategoryRun.From(c);

            var oldOutcome = oldRun.MergeWithoutSet(PrecisionKind.Binary64);
            var flaggedOutcome = flagged.Merge(PrecisionKind.Binary64, CorrectionSet.CategoriesOnCopy);

            Assert.NotEmpty(BitwiseComparison.Differences((oldOutcome, oldRun.IntegerTotals, oldRun.RealTotals),
                (flaggedOutcome, flagged.IntegerTotals, flagged.RealTotals), c.Name));
        }
    }

    /// <summary>
    /// <c>Compute</c> under <see cref="PrecisionKind.Original"/> on the analytic totals of every shipped formulation: a
    /// setup whose stored set was put there by hand as <see cref="CorrectionSet.All"/> gives the report, the totals and
    /// <c>nextPdoksmall</c> of the setup that stores <see cref="CorrectionSet.None"/>, bit for bit, in cycles 1 and 2.
    /// </summary>
    [Theory]
    [MemberData(nameof(ShippedNames))]
    public void ComputeUnderOriginalIgnoresAStoredSet(string name)
    {
        var formulation = ShippedFormulations.Read(name);
        var plain = AnalyticCycle.Prepare(formulation, ModelParameters.Default, PrecisionKind.Original, CorrectionSet.None);
        var imposed = AnalyticCycle.Prepare(formulation, ModelParameters.Default, PrecisionKind.Original, CorrectionSet.None, CorrectionSet.All);

        Assert.Empty(BitwiseComparison.Differences(TwoCycles(plain), TwoCycles(imposed), name));
    }

    /// <summary>
    /// Red, the comparison itself for <c>Compute</c>: under <see cref="PrecisionKind.Binary64"/> the same stored
    /// <see cref="CorrectionSet.All"/> is a finding on P312, whose <c>gdokleft</c> is positive.
    /// </summary>
    [Fact]
    public void ComputeUnderBinary64SeesAStoredSet()
    {
        var formulation = ShippedFormulations.Read("P312");
        var plain = AnalyticCycle.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, CorrectionSet.None);
        var imposed = AnalyticCycle.Prepare(formulation, ModelParameters.Default, PrecisionKind.Binary64, CorrectionSet.None, CorrectionSet.All);

        Assert.NotEmpty(BitwiseComparison.Differences(TwoCycles(plain), TwoCycles(imposed), "P312"));
    }

    private static (CycleReport First, CycleReport Second, double[] Totals, long[] IntegerTotals, double[] NextPdoksmall) TwoCycles(
        AnalyticCycle cycle)
    {
        var integerTotals = cycle.IntegerTotals();
        var realTotals = cycle.RealTotals();
        var first = cycle.Compute(1, integerTotals, realTotals, out _);
        var second = cycle.Compute(2, integerTotals, realTotals, out var next);
        return (first, second, realTotals, integerTotals, next);
    }

    public static IEnumerable<object[]> ShippedNames() => ShippedFormulations.Names.Select(name => new object[] { name });

    /// <summary>
    /// Right on an answer known beforehand: under <see cref="PrecisionKind.Binary64"/> the stored set is the one given,
    /// whichever flags are on, and a flag <c>Prepare</c> does not act on (<see cref="CorrectionSet.GdokleftOnce"/>,
    /// <see cref="CorrectionSet.CategoriesOnCopy"/>: the per-cycle statements) leaves every other output the old overload's.
    /// </summary>
    [Fact]
    public void PrepareUnderBinary64StoresTheSetItIsGiven()
    {
        var formulation = ShippedFormulations.Read("P33");
        var flags = CorrectionSet.GdokleftOnce | CorrectionSet.CategoriesOnCopy;
        var oldResult = PrepareOld(formulation, PrecisionKind.Binary64);
        var newResult = PrepareNew(formulation, PrecisionKind.Binary64, flags);

        Assert.Equal(flags, newResult.Setup.Corrections);
        Assert.Empty(BitwiseComparison.Differences(
            oldResult with { Setup = oldResult.Setup with { Corrections = flags } }, newResult, "P33"));
    }

    /// <summary>
    /// Red, the comparison itself: a result with one ulp moved in one table cell and one with another stored set are
    /// each a finding, so that the equalities above are not vacuous.
    /// </summary>
    [Fact]
    public void TheComparisonSeesAnUlpAndAStoredSet()
    {
        var formulation = ShippedFormulations.Read("P33");
        var baseline = PrepareOld(formulation, PrecisionKind.Binary64);
        var moved = baseline.Tables.Share.ToArray();
        moved[0] = double.BitIncrement(moved[0]);
        var shifted = baseline with { Tables = baseline.Tables with { Share = moved } };

        Assert.Collection(
            BitwiseComparison.Differences(baseline, shifted, "P33"),
            line => Assert.StartsWith("P33.Tables.Share[0]:", line, StringComparison.Ordinal));
        Assert.Collection(
            BitwiseComparison.Differences(
                baseline, baseline with { Setup = baseline.Setup with { Corrections = CorrectionSet.ExactConstants } }, "P33"),
            line => Assert.StartsWith("P33.Setup.Corrections:", line, StringComparison.Ordinal));
    }

    private static PreparedSetup PrepareOld(Formulation formulation, PrecisionKind kind)
    {
        var status = Setup.Prepare(formulation, ModelParameters.Default, kind, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out var draw, out var pending, out var inputs);
        return new PreparedSetup(status, setup, tables, draw, pending, inputs);
    }

    private static PreparedSetup PrepareNew(Formulation formulation, PrecisionKind kind, CorrectionSet corrections)
    {
        var status = Setup.Prepare(formulation, ModelParameters.Default, kind, corrections, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out var draw, out var pending, out var inputs);
        return new PreparedSetup(status, setup, tables, draw, pending, inputs);
    }

    private sealed record PreparedSetup(
        SetupStatus Status, ModelSetup Setup, SetupTables Tables, TailDraw Draw, PendingEchoes Pending, SetupInputs Inputs);
}
