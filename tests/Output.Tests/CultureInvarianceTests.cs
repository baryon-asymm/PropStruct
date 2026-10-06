using System.Globalization;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// L0 of the BOOT.md table's own invariant: "Formatting never depends on the current culture." Every
/// primitive is proven above against the invariant culture; this class re-runs a representative sample of
/// them under a decimal-comma culture and asserts the exact same text comes out, so a primitive that dropped
/// its explicit <see cref="CultureInfo.InvariantCulture"/> argument would print a comma and fail here even
/// though every invariant-culture test above stayed green.
/// </summary>
public class CultureInvarianceTests
{
    // The repository builds with <InvariantGlobalization>true</InvariantGlobalization> (Directory.Build.props,
    // outside this node), so a named culture ("ru-RU") cannot be looked up at all: CultureInfo.GetCultureInfo
    // throws CultureNotFoundException in this mode. A decimal-comma CultureInfo is still constructible without
    // any culture database lookup, by cloning the invariant culture's own (mutable-once-cloned) NumberFormat
    // and overwriting just its decimal separator - which is all the "non-invariant culture" invariant actually
    // needs to exercise.
    private static readonly CultureInfo DecimalComma = CreateDecimalCommaCulture();

    private static CultureInfo CreateDecimalCommaCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        numberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat = numberFormat;
        return culture;
    }

    private static void UnderDecimalCommaCulture(Action action)
    {
        Assert.Equal(",", DecimalComma.NumberFormat.NumberDecimalSeparator);
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = DecimalComma;
        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void SingleRealCoreFixedPointUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() => Assert.Equal("5.676710", FortranFormat.SingleRealCore(5.67671)));

    [Fact]
    public void SingleRealCoreExponentialUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() => Assert.Equal("5.0000000E-02", FortranFormat.SingleRealCore(0.05)));

    [Fact]
    public void DoubleRealCoreZeroUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() => Assert.Equal("0.000000000000000E+000", FortranFormat.DoubleRealCore(0.0)));

    [Fact]
    public void ScaledExponentUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() => Assert.Equal("0.100E-04", FortranFormat.ScaledExponent(1.0e-5, 3)));

    [Fact]
    public void FixedPointUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() => Assert.Equal(" 130.68", FortranFormat.FixedPoint(130.68, 2, 7)));

    [Fact]
    public void IntegerFieldUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() => Assert.Equal("      100000", FortranFormat.IntegerField(100000, FortranFormat.Integer4Width)));

    [Fact]
    public void FormatArrayUnaffectedByCurrentCulture() =>
        UnderDecimalCommaCulture(() =>
            Assert.Equal(" Gfr = [0.515E+00 0.485E+00  ];", FortranFormat.FormatArray("Gfr", [0.515, 0.485], decimals: 3)));
}
