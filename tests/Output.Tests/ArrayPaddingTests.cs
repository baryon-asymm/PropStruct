using System.Collections.Immutable;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// O-2 (fidelity audit, 2026-09-24): <c>coef</c>, <c>fqmkm1</c> and <c>fqmkm2</c> print <c>nmax + 2</c>
/// values with zeros beyond <c>Nc</c> (BOOT.md, "Design decisions (2026-09-19)"), because Fortran's own
/// <c>arrayprint</c> takes its element count from the *caller's* argument, not from the actual array's own
/// <c>Nc</c> declaration (Fortran lines 1517-1543), and reads zero past the end whenever <c>nmax</c> reaches
/// <c>Nc - 1</c> or <c>Nc</c> (measured, <c>tests/Fixtures/Legacy/outputs/hp2.m.txt</c>: 1002 <c>coef</c>
/// values, the last two <c>0.000E+00</c>). <c>ResultsMWriter</c>'s own <c>Take</c> helper clamped its result to
/// <paramref name="length"/> = <c>Math.Min(requested, values.Length)</c> instead, silently truncating those
/// trailing zeros away rather than printing them - the defect this file's tests are red on before the fix and
/// green after.
/// </summary>
public class ArrayPaddingTests
{
    private static readonly string DatPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "inpt.dat");

    private static IReadOnlyDictionary<string, double[]> Write(SimulationResult result)
    {
        var formulation = DatFile.Read(DatPath);
        var path = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, ModelParameters.Default, result, path);
            return ResultsMFile.Parse(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- unit level: a shorter-than-its-own-declared-length histogram, padded not truncated ------------------

    /// <summary>
    /// Three real values, but <c>CoefNmax + 2 = 6</c> asks for six: the pre-fix <c>Take</c> clamped to the
    /// array's own length (3) and silently dropped the trailing three zeros the original prints. Positive
    /// control for "padded, not truncated", per this task's own instruction.
    /// </summary>
    private static readonly double[] ExpectedCoef = [0.1, 0.2, 0.3, 0.0, 0.0, 0.0];

    [Fact]
    public void CoefArrayShorterThanItsOwnNmaxPlusTwoIsPaddedWithZerosNotTruncated()
    {
        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64);
        var result = sample with
        {
            Histograms = sample.Histograms with { CoefNormalized = ImmutableArray.Create(0.1, 0.2, 0.3) },
            Agglomerates = sample.Agglomerates with { CoefNmax = 4 },
        };

        var coef = Write(result)["coef"];

        Assert.Equal(6, coef.Length); // CoefNmax + 2, not clamped to the 3 real values
        Assert.Equal(ExpectedCoef, coef, EquivalentToPrintedPrecision);
    }

    private static readonly double[] ExpectedFqmkm1 = [0.4, 0.5, 0.0, 0.0, 0.0];

    [Fact]
    public void Qmkm1ArrayShorterThanItsOwnNmaxPlusTwoIsPaddedWithZerosNotTruncated()
    {
        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64);
        var result = sample with
        {
            Histograms = sample.Histograms with { Qmkm1Normalized = ImmutableArray.Create(0.4, 0.5) },
            Agglomerates = sample.Agglomerates with { Qmkm1Nmax = 3 },
        };

        var fqmkm1 = Write(result)["fqmkm1"];

        Assert.Equal(5, fqmkm1.Length); // Qmkm1Nmax + 2
        Assert.Equal(ExpectedFqmkm1, fqmkm1, EquivalentToPrintedPrecision);
    }

    private static readonly double[] ExpectedFqmkm2 = [0.6, 0.0, 0.0, 0.0];

    [Fact]
    public void Qmkm2ArrayShorterThanItsOwnNmaxPlusTwoIsPaddedWithZerosNotTruncated()
    {
        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64);
        var result = sample with
        {
            Histograms = sample.Histograms with { Qmkm2Normalized = ImmutableArray.Create(0.6) },
            Agglomerates = sample.Agglomerates with { Qmkm2Nmax = 2 },
        };

        var fqmkm2 = Write(result)["fqmkm2"];

        Assert.Equal(4, fqmkm2.Length); // Qmkm2Nmax + 2
        Assert.Equal(ExpectedFqmkm2, fqmkm2, EquivalentToPrintedPrecision);
    }

    /// <summary>The values round-trip through <c>E9.3</c> printing (three significant digits); compare at that resolution.</summary>
    private static readonly IEqualityComparer<double> EquivalentToPrintedPrecision = new ApproximateComparer(1e-3);

    private sealed class ApproximateComparer(double tolerance) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) <= tolerance;
        public int GetHashCode(double obj) => 0;
    }

    // ---- fixture level: the original's own hp2.m.txt, evidence the overflow is real, not hypothetical --------

    /// <summary>
    /// <c>tests/Fixtures/Legacy/outputs/hp2.m.txt</c> (input <c>Legacy/formulations/HPEPA10.dat</c>): the
    /// archived original prints 1002 <c>coef</c> values, meaning its own <c>coef_nmax</c> reached <c>Nc</c>
    /// (1000) - <c>nmax + 2</c> two past the array's own declared length - with the last two printed as zero.
    /// No typed-in expected count or value: both are read from the archive through the same parser the rest of
    /// this tree uses (`tests/Harness/ResultsMFile`).
    /// </summary>
    [Fact]
    public void ArchivedOriginalHp2PrintsCoefPastItsOwnNcWithTrailingZeros()
    {
        var archivePath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "outputs", "hp2.m.txt");
        var archive = ResultsMFile.Parse(archivePath);
        var coef = archive["coef"];

        // Nc = 1000 (src/Particle/API.md, "Nc = 1000"); an array of exactly Nc values would need no padding.
        const int nc = 1000;
        Assert.True(coef.Length > nc, $"expected the archive's own coef_nmax + 2 to exceed Nc = {nc}; got {coef.Length}");
        Assert.All(coef.Skip(nc), value => Assert.Equal(0.0, value));
    }
}
