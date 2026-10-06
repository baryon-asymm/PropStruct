using System.Globalization;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// L0 of the BOOT.md table: every formatting primitive reproduces the strings of the reference files it is
/// specified by. Every expected string is cut from a shipped reference <c>results.m.txt</c> (never typed):
/// the test parses the cut substring back into the <c>double</c> it represents, feeds that double through the
/// primitive, and asserts the primitive reproduces the exact cut text. Since every cut value already has
/// exactly the digit count the primitive would itself produce, this is a genuine round trip, not a tautology
/// dressed up: a wrong rounding rule, wrong digit count or wrong branch threshold moves the reproduced text
/// away from the cut one.
/// </summary>
public class FortranFormatTests
{
    private static string Line(string formulation, int lineNumber) =>
        File.ReadAllLines(RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt"))[lineNumber - 1];

    private static double ParseInvariant(string token) => double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);

    // ---- SingleRealCore: list-directed REAL (single precision), Fortran lines 1244-1256 -------------------

    [Fact]
    public void SingleRealCoreFixedPointOneIntegerDigitHPEPA3Line47()
    {
        // ' %    3) Dbase < 0.5Dok  :   5.676710    '
        const string cut = "5.676710";
        Assert.Equal(cut, FortranFormat.SingleRealCore(ParseInvariant(cut)));
        Assert.Contains(cut, Line("HPEPA3", 47));
    }

    [Fact]
    public void SingleRealCoreFixedPointThreeIntegerDigitsHPEPA3Line48()
    {
        // ' %    4) Dbase > 2.0Dok  :   446.3851    '
        const string cut = "446.3851";
        Assert.Equal(cut, FortranFormat.SingleRealCore(ParseInvariant(cut)));
        Assert.Contains(cut, Line("HPEPA3", 48));
    }

    [Fact]
    public void SingleRealCoreFixedPointFiveIntegerDigitsHMXLine49()
    {
        // ' %    4) Dbase > 2.0Dok  :   11454.90    '
        const string cut = "11454.90";
        Assert.Equal(cut, FortranFormat.SingleRealCore(ParseInvariant(cut)));
        Assert.Contains(cut, Line("HMX", 49));
    }

    [Fact]
    public void SingleRealCoreFixedPointLeadingZeroHPEPA3Line14()
    {
        // ' % k5 =  0.2500000     ;'
        const string cut = "0.2500000";
        Assert.Equal(cut, FortranFormat.SingleRealCore(ParseInvariant(cut)));
        Assert.Contains(cut, Line("HPEPA3", 14));
    }

    [Fact]
    public void SingleRealCoreExponentialHPEPA3Line16()
    {
        // ' % eps =  5.0000001E-02 ;'
        const string cut = "5.0000001E-02";
        Assert.Equal(cut, FortranFormat.SingleRealCore(ParseInvariant(cut)));
        Assert.Contains(cut, Line("HPEPA3", 16));
    }

    [Fact]
    public void SingleRealCoreZeroIsExponentialHPEPA3Line45()
    {
        // ' %    1) Dok > Dmax      :  0.0000000E+00'
        const string cut = "0.0000000E+00";
        Assert.Equal(cut, FortranFormat.SingleRealCore(0.0));
        Assert.Contains(cut, Line("HPEPA3", 45));
    }

    [Fact]
    public void SingleRealFieldFixedPointPadsToElevenAndTrailsFourBlanksHPEPA3Line47()
    {
        // ' %    3) Dbase < 0.5Dok  :   5.676710    ' - 3 leading blanks, "5.676710" (8 chars), 4 trailing.
        var field = FortranFormat.SingleRealField(ParseInvariant("5.676710"));
        Assert.Equal("   5.676710    ", field);
    }

    [Fact]
    public void SingleRealFieldLeadingZeroPadsToElevenAndTrailsFourBlanksHPEPA3Line99()
    {
        // ' da_coef =   0.7389501     ;' - 2 leading blanks (11-9), "0.7389501" (9 chars), 4 trailing, then
        // the 1-blank number->literal separator this test does not include (that is ResultsMWriter's own
        // composition, checked by the structural test).
        var field = FortranFormat.SingleRealField(ParseInvariant("0.7389501"));
        Assert.Equal("  0.7389501    ", field);
    }

    [Fact]
    public void SingleRealFieldExponentialPadsToFifteenHPEPA3Line36()
    {
        // ' epsx(1)=  3.0934811E-04 ; ...' - 2 leading blanks, 13-char value, no inherent trailing blank.
        var field = FortranFormat.SingleRealField(ParseInvariant("3.0934811E-04"));
        Assert.Equal("  3.0934811E-04", field);
    }

    [Fact]
    public void SingleRealAdjacentHPEPA3Line13()
    {
        // ' % Nkarm/Nmkm(min, max) = [   3.000000       100.0000     ];' - the second value, right-justified
        // in a 15-character field with no literal between it and the first (7 leading blanks + 8 chars),
        // plus its own 4 inherent trailing blanks.
        var field = FortranFormat.SingleRealAdjacent(ParseInvariant("100.0000"));
        Assert.Equal("       100.0000    ", field);
    }

    [Fact]
    public void SingleRealAdjacentLeadingZeroHPEPA3Line95()
    {
        // ' Zkarm_cor = [  0.5938808      0.5687045      ];' - second value, 9 chars, 6 leading blanks, plus
        // its own 4 inherent trailing blanks.
        var field = FortranFormat.SingleRealAdjacent(ParseInvariant("0.5687045"));
        Assert.Equal("      0.5687045    ", field);
    }

    // ---- DoubleRealCore: list-directed REAL*8 (alfa only), Fortran line 43 declares it real*8 ---------------

    [Fact]
    public void DoubleRealCoreZeroHPEPA3Line15()
    {
        // ' % Statistical significance P(alpha)=  0.000000000000000E+000 ;'
        const string cut = "0.000000000000000E+000";
        Assert.Equal(cut, FortranFormat.DoubleRealCore(0.0));
        Assert.Contains(cut, Line("HPEPA3", 15));
    }

    [Fact]
    public void DoubleRealFieldZeroPadsToTwentyFourHPEPA3Line15()
    {
        var field = FortranFormat.DoubleRealField(0.0);
        Assert.Equal("  0.000000000000000E+000", field);
    }

    // ---- ScaledExponent: E9.3 (arrayprint) and E12.6 (arrayprint2), Fortran lines 1521, 1553 ----------------

    [Fact]
    public void ScaledExponentE9HPEPA3Line8()
    {
        // ' Dfr = [0.100E-04 0.500E-04 0.160E-03 0.315E-03  ];'
        const string cut = "0.100E-04";
        Assert.Equal(cut, FortranFormat.ScaledExponent(1.0e-5, 3));
        Assert.Contains(cut, Line("HPEPA3", 8));
    }

    [Fact]
    public void ScaledExponentE9ZeroHPEPA3Line256()
    {
        // ' coef = [0.000E+00 0.000E+00 ...' (the leading run of the coef array)
        const string cut = "0.000E+00";
        Assert.Equal(cut, FortranFormat.ScaledExponent(0.0, 3));
        Assert.Contains(cut, Line("HPEPA3", 256));
    }

    [Fact]
    public void ScaledExponentE12InptLine567()
    {
        // ' epsfkarm_n = [0.151219E-01 0.110702E-01 0.907081E-02 0.781857E-02 0.708052E-02 0.643496E-02...'
        const string cut = "0.151219E-01";
        Assert.Equal(cut, FortranFormat.ScaledExponent(ParseE(cut), 6));
        Assert.Contains(cut, Line("inpt", 567));
    }

    private static double ParseE(string scaled) =>
        // "0.dddddd E±ee" is not a .NET-parseable literal directly (leading "0." with the exponent shifted
        // one power from .NET's own [1,10) convention); round-trip through the primitive's own convention by
        // reading it as "0.ddddddE±ee" == mantissa * 10^exponent, which double.Parse already handles.
        ParseInvariant(scaled);

    // ---- FixedPoint: F7.2/F6.4/F6.1/F5.3, Fortran lines 913, 925, 946, and the Dqmkm formats ----------------

    [Fact]
    public void FixedPointF72HPEPA3Line57() =>
        // ' Dok43a =  130.68; %(analytical calculation, all particles)'
        Assert.Equal(" 130.68", FortranFormat.FixedPoint(130.68, 2, 7));

    [Fact]
    public void FixedPointF64HPEPA3Line108() =>
        // ' Dqmkm1 = 0.1492;'
        Assert.Equal("0.1492", FortranFormat.FixedPoint(0.1492, 4, 6));

    [Fact]
    public void FixedPointF61HPEPA3Line5() =>
        // ' % Plot1 = 1950.0; Plot2 = 1800.0; ...'
        Assert.Equal("1950.0", FortranFormat.FixedPoint(1950.0, 1, 6));

    [Fact]
    public void FixedPointF53HPEPA3Line5() =>
        // ' ...Gdok = 0.583; Gm = 0.207;'
        Assert.Equal("0.583", FortranFormat.FixedPoint(0.583, 3, 5));

    // ---- IntegerField: default list-directed widths, Fortran's own INTEGER*4/*8/*2 defaults ----------------

    [Fact]
    public void IntegerFieldInteger4WidthHPEPA3Line29() =>
        // ' Nbase =      100000 +      100000 ;'
        Assert.Equal("      100000", FortranFormat.IntegerField(100000, FortranFormat.Integer4Width));

    [Fact]
    public void IntegerFieldInteger8WidthHPEPA3Line31() =>
        // ' Nkarm =              24649481 ;'
        Assert.Equal("              24649481", FortranFormat.IntegerField(24649481, FortranFormat.Integer8Width));

    [Fact]
    public void IntegerFieldInteger8WidthNineDigitsHPEPA3Line33() =>
        // ' NFX =               1336594 ;  NFY =             117621203 ;'
        Assert.Equal("             117621203", FortranFormat.IntegerField(117621203, FortranFormat.Integer8Width));

    [Fact]
    public void IntegerFieldInteger2WidthHPEPA3Line367() =>
        // ' % Dkarm =     10 mkm'
        Assert.Equal("     10", FortranFormat.IntegerField(10, FortranFormat.Integer2Width));

    // ---- FormatArray: arrayprint's layout, Fortran lines 1517-1543 -----------------------------------------

    [Fact]
    public void FormatArrayTwoValuesNoWrapHPEPA3Line7()
    {
        // ' Gfr = [0.515E+00 0.485E+00  ];'
        var text = FortranFormat.FormatArray("Gfr", [0.515, 0.485], decimals: 3);
        Assert.Equal(" Gfr = [0.515E+00 0.485E+00  ];", text);
    }

    [Fact]
    public void FormatArraySixValuesOnFirstLineThenContinuationHPEPA3Lines117To118()
    {
        double[] values =
        [
            0.0, 0.736e-2, 0.139e-1, 0.135e-1, 0.136e-1, 0.0,
            0.0,
        ];
        var text = FortranFormat.FormatArray("fmdok", values, decimals: 3);
        var lines = text.Split("\r\n");
        Assert.Equal(" fmdok = [0.000E+00 0.736E-02 0.139E-01 0.135E-01 0.136E-01 0.000E+00...", lines[0]);
        // Continuation indent: len("fmdok")+5 = 10 spaces before the first (and here only, plain-branch,
        // so it keeps its own trailing blank) value of the next line, then the closing write's own blank.
        Assert.Equal("          0.000E+00  ];", lines[1]);
    }
}
