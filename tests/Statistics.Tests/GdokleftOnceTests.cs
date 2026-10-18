using System.Collections.Immutable;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>GdokleftOnce</c> (<c>src/Statistics/BOOT.md</c>, "## Corrected model"; Fortran 1001-1006): the mass of an
/// <c>SFR = 0</c> fraction below <c>b = int(Dmin/Di) Di</c> sits in <c>FMDOK(k)</c> and, as the original adds the
/// fraction's whole <c>GDOK</c>, a second time. Corrected, the term is <c>GGG GDOK (1 - phi(b))</c>. The right side is
/// the analytic <c>GGG [FMDOK(k) + GDOK (1 - phi(b))]</c> on constructed totals, each fraction under both size laws;
/// the red side is <c>None</c>, which exceeds it by <c>GGG GDOK phi(b)</c>. The totals are those of
/// <see cref="ConstructedCycle"/>: <c>Allvdok</c> is 1 in every cell but the first, so <c>FMDOK(k) = (k - 2)/(Ndok - 1)</c>
/// for <c>k &gt;= 2</c> and 0 for <c>k = 1</c>, a closed form of the totals and not a value read from the code.
/// </summary>
public class GdokleftOnceTests
{
    private const double RelativeTolerance = 1e-12;

    /// <summary>Right: one <c>SFR = 0</c> fraction across <c>b</c>, both laws, the flag equals the analytic value and <c>None</c> exceeds it by <c>GGG GDOK phi(b)</c>.</summary>
    [Theory]
    [InlineData(1, 30e-6, 90e-6, 50e-6)]
    [InlineData(2, 30e-6, 90e-6, 50e-6)]
    [InlineData(1, 12e-6, 31e-6, 20e-6)]
    [InlineData(2, 12e-6, 31e-6, 20e-6)]
    public void AFractionAcrossTheBoundIsCountedOnceUnderTheFlagAndTwiceWithout(int sizeLaw, double lower, double upper, double dmin)
    {
        var scenario = Scenario(sizeLaw, lower, upper, dmin);
        Assert.True(lower < scenario.Below && scenario.Below < upper, $"b = {scenario.Below} is not inside [{lower}, {upper}]");

        var none = scenario.Gdokleft(CorrectionSet.None);
        var corrected = scenario.Gdokleft(CorrectionSet.GdokleftOnce);

        var cdf = MassCdf(sizeLaw, lower, upper, scenario.Below);
        Assert.InRange(cdf, 0.05, 0.95);
        AssertRelative(scenario.Ggg * (scenario.Fmdok + scenario.MassShareBelowBound * (1.0 - cdf)), corrected);
        AssertRelative(scenario.Ggg * (scenario.Fmdok + scenario.MassShareBelowBound), none);
        AssertRelative(scenario.Ggg * scenario.MassShareBelowBound * cdf, none - corrected);
    }

    /// <summary>The fraction wholly above <c>b</c> carries no mass below it: the flag is bit-identical to <c>None</c>.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ABoundAtOrBelowTheLowerBoundChangesNoBit(int sizeLaw)
    {
        var scenario = Scenario(sizeLaw, 60e-6, 120e-6, dmin: 20e-6);
        Assert.True(scenario.Below <= 60e-6);

        Assert.Equal(
            BitConverter.DoubleToInt64Bits(scenario.Gdokleft(CorrectionSet.None)),
            BitConverter.DoubleToInt64Bits(scenario.Gdokleft(CorrectionSet.GdokleftOnce)));
    }

    /// <summary>The fraction wholly below <c>b</c> is in <c>FMDOK</c> whole, so its <c>GDOK</c> is not added at all.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void AFractionWhollyBelowTheBoundIsCountedOnlyInTheCumulativeMass(int sizeLaw)
    {
        var scenario = Scenario(sizeLaw, 2e-6, 10e-6, dmin: 20e-6);
        Assert.True(scenario.Below >= 10e-6);

        AssertRelative(scenario.Ggg * scenario.Fmdok, scenario.Gdokleft(CorrectionSet.GdokleftOnce));
    }

    /// <summary>Pocket-forming fractions are not touched, and a flag that <c>Matrix</c> does not read leaves <c>gdokleft</c> to the bit.</summary>
    [Fact]
    public void OtherFlagsAndPocketFormingFractionsChangeNoBit()
    {
        var scenario = Scenario(2, 30e-6, 90e-6, dmin: 20e-6, pocketForming: 1);

        Assert.Equal(
            BitConverter.DoubleToInt64Bits(scenario.Gdokleft(CorrectionSet.None)),
            BitConverter.DoubleToInt64Bits(scenario.Gdokleft(CorrectionSet.GdokleftOnce)));
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(Scenario(2, 30e-6, 90e-6, dmin: 20e-6).Gdokleft(CorrectionSet.None)),
            BitConverter.DoubleToInt64Bits(Scenario(2, 30e-6, 90e-6, dmin: 20e-6).Gdokleft(CorrectionSet.VdokTotalOnCopy | CorrectionSet.ExactConstants)));
    }

    /// <summary>
    /// Right at the size of the effect, the evidence case of the criterion: PSAN02 read with its pocket-forming flags
    /// at <c>Dmin = 20 um</c> and <c>Di = 10 um</c> (both as the menu converts them, so that <c>Dmin/Di</c> is exactly 2
    /// and <c>b = 20 um</c>), <c>None</c> minus the flag is 0.0119 of the propellant (0.0175 of the oxidizer), to the
    /// three digits the arbiter's computation states. The difference is <c>GGG Sum GDOK phi(b)</c> and does not depend
    /// on the totals.
    /// </summary>
    [Fact]
    public void Psan02ReadWithPocketFormingFractionsAtTwentyMicrometresDiffersByTheStatedShare()
    {
        var formulation = DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "PSAN02.dat"), true);
        var parameters = ModelParameters.Default with
        {
            Dmin = Length.FromMicrometres(20),
            CellSize = Length.FromMicrometres(10),
            ReadPocketFormingFractions = true,
        };

        var none = ConstructedCycle.Report(formulation, parameters, PrecisionKind.Binary64, CorrectionSet.None);
        var corrected = ConstructedCycle.Report(formulation, parameters, PrecisionKind.Binary64, CorrectionSet.GdokleftOnce);

        var difference = none.Gdokleft - corrected.Gdokleft;
        Assert.Equal(0.0119, difference, 4);
        Assert.Equal(0.0175, difference / none.Ggg, 4);
    }

    /// <summary>Under <see cref="PrecisionKind.Original"/> the flag is ignored in <c>Compute</c>, also in a setup whose stored set was put there by hand.</summary>
    [Fact]
    public void UnderOriginalTheFlagIsIgnored()
    {
        var scenario = Scenario(2, 30e-6, 90e-6, dmin: 20e-6);

        var none = scenario.Report(PrecisionKind.Original, CorrectionSet.None, null).Gdokleft;
        var imposed = scenario.Report(PrecisionKind.Original, CorrectionSet.None, CorrectionSet.GdokleftOnce).Gdokleft;

        Assert.Equal(BitConverter.DoubleToInt64Bits(none), BitConverter.DoubleToInt64Bits(imposed));
    }

    /// <summary>
    /// Two fractions: a pocket-forming one above every bound in play and an <c>SFR = 0</c> one over
    /// <c>[lower, upper]</c> (or the other way round with <paramref name="pocketForming"/> = 1).
    /// </summary>
    private static GdokleftScenario Scenario(int sizeLaw, double lower, double upper, double dmin, int pocketForming = 0)
    {
        var template = ShippedFormulations.Read("P33");
        var formulation = template with
        {
            SizeLawCode = sizeLaw,
            Fractions =
            [
                new OxidizerFraction(0.6, Length.FromMetres(150e-6), Length.FromMetres(300e-6)),
                new OxidizerFraction(0.4, Length.FromMetres(lower), Length.FromMetres(upper)),
            ],
            PocketFormingFractions = ImmutableArray.Create(1, pocketForming),
        };
        var parameters = ModelParameters.Default with { Dmin = Length.FromMetres(dmin), ReadPocketFormingFractions = true };
        return new GdokleftScenario(formulation, parameters);
    }

    private static double MassCdf(int sizeLaw, double lower, double upper, double size) =>
        sizeLaw == 2
            ? (Math.Pow(size, 4) - Math.Pow(lower, 4)) / (Math.Pow(upper, 4) - Math.Pow(lower, 4))
            : (size - lower) / (upper - lower);

    private static void AssertRelative(double expected, double actual) =>
        Assert.True(Math.Abs(actual - expected) <= RelativeTolerance * Math.Abs(expected),
            $"expected {expected:R}, got {actual:R}: relative {Math.Abs(actual - expected) / Math.Abs(expected):E3}");

    private sealed class GdokleftScenario
    {
        private readonly Formulation formulation;
        private readonly ModelParameters parameters;

        public GdokleftScenario(Formulation formulation, ModelParameters parameters)
        {
            this.formulation = formulation;
            this.parameters = parameters;
            var status = Setup.Prepare(formulation, parameters, PrecisionKind.Binary64, 1000, 1000,
                out var setup, out _, out _, out _, out _);
            Assert.Equal(SetupStatus.Ok, status);

            var index = (int)(setup.Dmin / setup.CellSize);
            Below = index * setup.CellSize;
            Fmdok = index <= 1 ? 0.0 : (index - 1.0) / (setup.Ndok - 1.0);
            Ggg = Report(PrecisionKind.Binary64, CorrectionSet.None, null).Ggg;
        }

        /// <summary><c>b</c>, the cell edge <c>int(Dmin/Di) Di</c>.</summary>
        public double Below { get; }

        /// <summary><c>FMDOK(k)</c> of the constructed totals, in closed form.</summary>
        public double Fmdok { get; }

        public double Ggg { get; }

        /// <summary>The raw share <c>GDOK</c> of the <c>SFR = 0</c> fraction, as the file reads it (0.4).</summary>
        public double MassShareBelowBound => formulation.Fractions[1].MassShare;

        public double Gdokleft(CorrectionSet corrections) => Report(PrecisionKind.Binary64, corrections, null).Gdokleft;

        public CycleReport Report(PrecisionKind precision, CorrectionSet corrections, CorrectionSet? imposed) =>
            ConstructedCycle.Report(formulation, parameters, precision, corrections, imposed);
    }
}
