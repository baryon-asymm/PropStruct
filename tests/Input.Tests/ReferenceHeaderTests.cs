using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Input.Tests;

/// <summary>
/// L1 (tests/Input.Tests/BOOT.md): parsed values of the five reference formulations equal the header lines
/// of their <c>results.m.txt</c> at print precision (BOOT.md "Acceptance criteria"), read through
/// <see cref="ResultsMFile"/> (`tests/Harness/API.md`).
/// </summary>
public sealed class ReferenceHeaderTests
{
    // Single-precision (REAL*4) fields of the original are printed already rounded to about seven
    // significant decimal digits (source lines 6, 32, 66-80); a relative tolerance one order coarser
    // absorbs that print rounding without hiding a real mismatch.
    private const double Real4RelativeTolerance = 1e-5;

    // Gfr and Dfr are printed by arrayprint's E9.3 (source line 1531): a mantissa "0.ddd", so the absolute
    // step of its last digit is 0.001 times the printed power of ten; for a mantissa as low as 0.100 that
    // is a relative half-step of 0.0005/0.100 = 0.5 %, so the tolerance below is set past that worst case.
    private const double ArrayPrintRelativeTolerance = 6e-3;

    public static TheoryData<string> ReferenceFormulations() =>
    [
        "HPEPA3",
        "inpt",
        "P33",
        "PSAN02n",
        "HMX",
    ];

    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void ParsedFormulationMatchesTheReferenceHeader(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat");
        var resultsPath = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");

        var formulation = DatFile.Read(datPath);
        var header = ResultsMFile.Parse(resultsPath);

        Assert.Equal(Field(header, "Plot1"), formulation.OxidizerDensity, 1);
        Assert.Equal(Field(header, "Plot2"), formulation.PropellantDensity, 1);
        Assert.Equal(Field(header, "Gdok"), formulation.OxidizerMassFraction, 3);
        Assert.Equal(Field(header, "Gm"), formulation.MetalMassFraction, 3);

        Assert.Equal((int)Field(header, "Nfr"), formulation.Fractions.Length);
        Assert.Equal((int)Field(header, "JZ"), formulation.SizeLawCode);
        Assert.Equal((int)Field(header, "Cycles"), formulation.Cycles);
        Assert.Equal((int)Field(header, "N"), formulation.ParticlesPerCycle);

        var gfr = header["Gfr"];
        Assert.Equal(gfr.Length, formulation.Fractions.Length);
        for (var i = 0; i < gfr.Length; i++)
        {
            AssertRelativelyClose(gfr[i], formulation.Fractions[i].MassShare, ArrayPrintRelativeTolerance);
        }

        var dfr = header["Dfr"];
        Assert.Equal(dfr.Length, 2 * formulation.Fractions.Length);
        for (var i = 0; i < formulation.Fractions.Length; i++)
        {
            AssertRelativelyClose(dfr[2 * i], formulation.Fractions[i].LowerBound.Metres, ArrayPrintRelativeTolerance);
            AssertRelativelyClose(dfr[2 * i + 1], formulation.Fractions[i].UpperBound.Metres, ArrayPrintRelativeTolerance);
        }
    }

    [Fact]
    public void ModelParametersDefaultMatchesTheHpepa3ReferenceHeader()
    {
        var header = ResultsMFile.Parse(
            RepositoryPaths.Resolve("tests", "Fixtures", "references", "HPEPA3", "results.m.txt"));
        var defaults = ModelParameters.Default;

        // The original prints Dmin/Di/Dj as int(Dmin*1.00001e6) (source line 1204), reproduced here rather
        // than compared with a tolerance, since it is an exact integer formula, not a printed rounding.
        Assert.Equal(Field(header, "Dmin"), (int)(defaults.Dmin.Metres * 1.00001e6));
        Assert.Equal(Field(header, "Di"), (int)(defaults.CellSize.Metres * 1.00001e6));
        Assert.Equal(Field(header, "Dj"), (int)(defaults.CategoryStep.Metres * 1.00001e6));

        AssertRelativelyClose(Field(header, "k5"), defaults.Alpha, Real4RelativeTolerance);
        AssertRelativelyClose(Field(header, "eps"), defaults.EpsDok, Real4RelativeTolerance);
        Assert.Equal((int)Field(header, "Calculation variant"), defaults.Variant);
        AssertRelativelyClose(Field(header, "P(karm-in-karm) coef"), defaults.PocketCoefficient, Real4RelativeTolerance);
        AssertRelativelyClose(Field(header, "P(karm-in-MKM) coef"), defaults.BridgeCoefficient, Real4RelativeTolerance);
        Assert.Equal(Field(header, "Statistical significance P(alpha)"), defaults.TailProbability);
        Assert.Equal(Field(header, "Zok*"), defaults.HomogenizedOxidizerFraction);
    }

    private static double Field(IReadOnlyDictionary<string, double[]> header, string name) => header[name][0];

    private static void AssertRelativelyClose(double expected, double actual, double relativeTolerance)
    {
        var tolerance = Math.Max(1e-8, Math.Abs(expected) * relativeTolerance);
        Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"expected {expected} to be within {tolerance} of {actual}");
    }
}
