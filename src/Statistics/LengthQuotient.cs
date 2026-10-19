using System.Globalization;
using System.Numerics;
using PropStruct.Input;

namespace PropStruct.Statistics;

/// <summary>
/// The truncated quotient of two lengths taken exactly from the values as written, the
/// <c>Binary64</c> kind's cell index <c>int(Dmin/Di)</c> (Fortran line 1001; BOOT.md,
/// "## Line map", "The cell index under <c>Binary64</c>"). Each length is read as its
/// shortest decimal text (<c>"R"</c>, the digits the file or the option wrote) scaled by
/// 10<sup>-6</sup> when it is a micrometre value, so the quotient of two multiples of the
/// same decimal step is exact whatever the unit of either, which a quotient of the two
/// <c>double</c> metre values is not (<c>10 · 1e-6 / 1e-5 = 0.9999999999999999</c>).
/// </summary>
internal static class LengthQuotient
{
    /// <summary>
    /// The integer part of <c>numerator / denominator</c> as an exact rational, saturated at
    /// <see cref="int.MaxValue"/>. Preconditions: both lengths finite, <paramref name="numerator"/>
    /// not negative and <paramref name="denominator"/> positive (<c>Setup.Prepare</c>
    /// checks both before it calls).
    /// </summary>
    public static int Truncated(Length numerator, Length denominator)
    {
        var (numeratorDigits, numeratorExponent) = Decimal(numerator);
        var (denominatorDigits, denominatorExponent) = Decimal(denominator);
        var shift = numeratorExponent - denominatorExponent;
        var quotient = shift >= 0
            ? numeratorDigits * BigInteger.Pow(10, shift) / denominatorDigits
            : numeratorDigits / (denominatorDigits * BigInteger.Pow(10, -shift));
        return quotient > int.MaxValue ? int.MaxValue : (int)quotient;
    }

    /// <summary>The length as <c>digits times 10^exponent</c> metres, the digits an integer.</summary>
    private static (BigInteger Digits, int Exponent) Decimal(Length length)
    {
        var text = length.AsWritten.ToString("R", CultureInfo.InvariantCulture);
        var exponent = length.IsMicrometres ? -6 : 0;

        var marker = text.IndexOf('E', StringComparison.Ordinal);
        if (marker >= 0)
        {
            exponent += int.Parse(text.AsSpan(marker + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            text = text[..marker];
        }

        var point = text.IndexOf('.', StringComparison.Ordinal);
        if (point >= 0)
        {
            exponent -= text.Length - point - 1;
            text = text.Remove(point, 1);
        }

        return (BigInteger.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture), exponent);
    }
}
