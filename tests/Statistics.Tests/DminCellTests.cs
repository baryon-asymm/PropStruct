using System.Globalization;
using PropStruct.Input;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The cell index <c>int(Dmin/Di)</c> of Fortran line 1001 (<c>src/Statistics/BOOT.md</c>, "## Line map", "The cell index
/// under <c>Binary64</c>"). Under <see cref="PrecisionKind.Binary64"/> it is the exact quotient of the values as written,
/// whichever unit each is written in; the quotient of the two <c>double</c> metre values truncates one cell low for every
/// multiple of 10 um against the default <c>Di</c> (<c>10 * 1e-6 / 1e-5 = 0.9999999999999999</c>), which the original
/// does not do, since its REAL*4 conversions give 2.0 exactly. Under <see cref="PrecisionKind.Original"/> the quotient
/// of the stored binary32 values is unchanged.
/// </summary>
public class DminCellTests
{
    private static readonly Length DefaultCell = ModelParameters.Default.CellSize;

    /// <summary>
    /// Positive control of the defect, in the units of the menu: the <c>double</c> quotient of the two metre values is
    /// below the integer for 10 um against the default <c>Di</c>, and the exact quotient is that integer.
    /// </summary>
    [Fact]
    public void TheDoubleQuotientOfTenMicrometresAgainstTheDefaultCellIsOneCellLow()
    {
        var dmin = Length.FromMicrometres(10);

        Assert.Equal(0, (int)(dmin.Metres / DefaultCell.Metres));
        Assert.Equal(1, LengthQuotient.Truncated(dmin, DefaultCell));
    }

    /// <summary>
    /// Right on an input whose answer is known beforehand: every pair of a generated grid, written in each of the four
    /// unit combinations, truncates to the integer quotient of the pair's own integers (units of 0.1 um), never read from
    /// the code under test.
    /// </summary>
    [Fact]
    public void TheQuotientIsTheExactTruncationForEveryGeneratedPairInEitherUnit()
    {
        var mismatches = new List<string>();
        var pairs = 0;
        for (var cell = 1; cell <= 60; cell++)
        {
            for (var dmin = 0; dmin <= 600; dmin++)
            {
                var expected = dmin / cell;
                foreach (var (numerator, denominator, name) in WrittenForms(dmin, cell))
                {
                    pairs++;
                    var actual = LengthQuotient.Truncated(numerator, denominator);
                    if (actual != expected)
                    {
                        mismatches.Add($"{name}: expected {expected}, got {actual}");
                    }
                }
            }
        }

        Assert.True(pairs > 100_000, $"only {pairs} pairs generated");
        Assert.True(mismatches.Count == 0, $"{mismatches.Count} of {pairs}: {string.Join("; ", mismatches.Take(10))}");
    }

    /// <summary>
    /// The same property through <c>Setup.Prepare</c> on a shipped formulation: under <c>Binary64</c> the stored index is
    /// the integer quotient of the generated pair, in each of the four unit combinations.
    /// </summary>
    [Fact]
    public void PrepareStoresTheExactIndexUnderBinary64()
    {
        var formulation = ShippedFormulations.Read("P33");
        var mismatches = new List<string>();
        var runs = 0;
        foreach (var cell in new[] { 20, 40, 50, 100, 200, 250 })
        {
            for (var dmin = 1; dmin <= 1200; dmin += 7)
            {
                foreach (var (numerator, denominator, name) in WrittenForms(dmin, cell))
                {
                    var parameters = ModelParameters.Default with { Dmin = numerator, CellSize = denominator };
                    var status = Setup.Prepare(formulation, parameters, PrecisionKind.Binary64, 1000, 1000,
                        out _, out var tables, out _, out _, out _);
                    runs++;
                    if (status != SetupStatus.Ok || tables.DminCell != dmin / cell)
                    {
                        mismatches.Add($"{name}: {status}, index {tables.DminCell}, expected {dmin / cell}");
                    }
                }
            }
        }

        Assert.True(runs > 600, $"only {runs} runs");
        Assert.True(mismatches.Count == 0, $"{mismatches.Count} of {runs}: {string.Join("; ", mismatches.Take(10))}");
    }

    /// <summary>
    /// Red on the old quotient, right on the exact one: 20 um and 20e-6 m, and 10 um against the default <c>Dmin</c>, are
    /// one value each, so they give one index and, through <c>Compute</c>, one <see cref="CycleReport"/> to the bit (the
    /// constructed totals of <see cref="ConstructedCycle"/> make the report differ from index 2 on, so the first case
    /// is held by its index alone).
    /// </summary>
    [Theory]
    [InlineData(20.0, 2, 20e-6, 2)]
    [InlineData(10.0, 1, 10e-6, 1)]
    [InlineData(30.0, 3, 30e-6, 3)]
    public void ADminWrittenInEitherUnitGivesOneIndexAndOneReportUnderBinary64(
        double micrometres, int expectedIndex, double metres, int expectedIndexInMetres)
    {
        var formulation = ShippedFormulations.Read("P33");
        var inMicrometres = ModelParameters.Default with { Dmin = Length.FromMicrometres(micrometres) };
        var inMetres = ModelParameters.Default with { Dmin = Length.FromMetres(metres) };

        Assert.Equal(expectedIndex, Index(formulation, inMicrometres, PrecisionKind.Binary64));
        Assert.Equal(expectedIndexInMetres, Index(formulation, inMetres, PrecisionKind.Binary64));

        var expected = ConstructedCycle.Report(formulation, inMetres, PrecisionKind.Binary64, CorrectionSet.None);
        var actual = ConstructedCycle.Report(formulation, inMicrometres, PrecisionKind.Binary64, CorrectionSet.None);
        Assert.Empty(BitwiseComparison.Differences(expected, actual, "CycleReport"));

        // The constructed totals leave FMDOK(1) empty and put mass in the cells above: an index of 2 or more must show.
        Assert.Equal(expectedIndex > 1, expected.Gdokleft > 0.0);
    }

    /// <summary>
    /// Unchanged under <see cref="PrecisionKind.Original"/>: the stored binary32 values give the exact quotient already
    /// (the original's own <c>float(20 * float(1e-6)) / float(10e-6)</c> is 2.0), so 10 um and the default <c>Dmin</c> are
    /// one index and one report there, and the index is the quotient of the setup's own stored values.
    /// </summary>
    [Theory]
    [InlineData(10.0, 10e-6)]
    [InlineData(20.0, 20e-6)]
    [InlineData(70.0, 70e-6)]
    public void UnderOriginalTheIndexIsTheStoredBinary32QuotientAndUnitsAgree(double micrometres, double metres)
    {
        var formulation = ShippedFormulations.Read("P33");
        var inMicrometres = ModelParameters.Default with { Dmin = Length.FromMicrometres(micrometres) };
        var inMetres = ModelParameters.Default with { Dmin = Length.FromMetres(metres) };

        var status = Setup.Prepare(formulation, inMicrometres, PrecisionKind.Original, 1000, 1000,
            out var setup, out var tables, out _, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);
        Assert.Equal((int)(setup.Dmin / setup.CellSize), tables.DminCell);
        Assert.Equal(Index(formulation, inMetres, PrecisionKind.Original), tables.DminCell);

        var expected = ConstructedCycle.Report(formulation, inMetres, PrecisionKind.Original, CorrectionSet.None);
        var actual = ConstructedCycle.Report(formulation, inMicrometres, PrecisionKind.Original, CorrectionSet.None);
        Assert.Empty(BitwiseComparison.Differences(expected, actual, "CycleReport"));
    }

    /// <summary>The default run, both written in metres, keeps its index 1: the exact quotient is today's wherever the double was right.</summary>
    [Fact]
    public void TheDefaultDminAndCellKeepIndexOneUnderBothKinds()
    {
        var formulation = ShippedFormulations.Read("P33");

        Assert.Equal(1, Index(formulation, ModelParameters.Default, PrecisionKind.Binary64));
        Assert.Equal(1, Index(formulation, ModelParameters.Default, PrecisionKind.Original));
    }

    /// <summary>An infinite cell size is refused with a status, not read as a quotient of zero.</summary>
    [Fact]
    public void AnInfiniteCellSizeIsRefused()
    {
        var parameters = ModelParameters.Default with { CellSize = new Length(double.PositiveInfinity) };

        var status = Setup.Prepare(ShippedFormulations.Read("P33"), parameters, PrecisionKind.Binary64, 1000, 1000,
            out _, out _, out _, out _, out _);

        Assert.Equal(SetupStatus.InvalidCellSize, status);
    }

    private static int Index(Formulation formulation, ModelParameters parameters, PrecisionKind precision)
    {
        var status = Setup.Prepare(formulation, parameters, precision, 1000, 1000,
            out _, out var tables, out _, out _, out _);
        Assert.Equal(SetupStatus.Ok, status);
        return tables.DminCell;
    }

    /// <summary>
    /// The pair <paramref name="dminTenths"/> and <paramref name="cellTenths"/> (units of 0.1 um) written in each of the
    /// four unit combinations, the way a file or an option writes them: as decimal text, a micrometre value from 0.1 up and
    /// a metre value below it. A zero <c>Dmin</c> has only its metre form.
    /// </summary>
    private static IEnumerable<(Length Dmin, Length Cell, string Name)> WrittenForms(int dminTenths, int cellTenths)
    {
        var dminForms = dminTenths == 0
            ? new[] { (Length: new Length(0.0), Unit: "m") }
            : new[] { (Length: Micrometres(dminTenths), Unit: "um"), (Length: Metres(dminTenths), Unit: "m") };
        var cellForms = new[] { (Length: Micrometres(cellTenths), Unit: "um"), (Length: Metres(cellTenths), Unit: "m") };
        foreach (var dmin in dminForms)
        {
            foreach (var cell in cellForms)
            {
                yield return (dmin.Length, cell.Length, $"{dminTenths}/{cellTenths} written {dmin.Unit}/{cell.Unit}");
            }
        }
    }

    private static Length Micrometres(int tenths) =>
        new(double.Parse($"{tenths / 10}.{tenths % 10}", CultureInfo.InvariantCulture));

    private static Length Metres(int tenths) =>
        new(double.Parse($"{tenths}E-7", CultureInfo.InvariantCulture));
}
