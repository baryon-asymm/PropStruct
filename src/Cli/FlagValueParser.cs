using System.Globalization;
using System.Text.RegularExpressions;
using PropStruct.Input;

namespace PropStruct.Cli;

/// <summary>
/// Turns one flag's raw text into the boxed value <see cref="FlagDefinition.Property"/> expects, or a
/// <see cref="ParseError"/> naming the flag (BOOT.md, "Flag names are the original's own names, with
/// aliases": "every number is parsed with <c>CultureInfo.InvariantCulture</c> passed explicitly ... a
/// Fortran exponent form (<c>10d-6</c>) is refused with a message naming the flag").
/// </summary>
internal static class FlagValueParser
{
    // A Fortran-style exponent letter between two digit runs: "10d-6", "1.5D+3". The .NET double parser
    // never accepts 'd'/'D' as an exponent marker, so this distinguishes "wrong exponent letter" from
    // "not a number at all" for the diagnostic text (BOOT.md's own example, "10d-6").
    private static readonly Regex FortranExponent = new(@"^[+-]?\d+(\.\d+)?[dD][+-]?\d+$", RegexOptions.Compiled);

    public static bool TryParse(FlagDefinition flag, string text, out object value, out ParseError? error)
    {
        switch (flag.Kind)
        {
            case FlagKind.Length:
                if (FortranExponent.IsMatch(text))
                {
                    return Fail(Diagnostic.FortranExponentRejected,
                        $"`{flag.CanonicalFlag}` does not accept the Fortran exponent form `{text}`; write it with `E`", out value, out error);
                }

                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var length))
                {
                    return Fail(Diagnostic.InvalidNumber, $"`{flag.CanonicalFlag}` expects a number, got `{text}`", out value, out error);
                }

                value = new Length(length);
                error = null;
                return true;

            case FlagKind.Number:
                if (FortranExponent.IsMatch(text))
                {
                    return Fail(Diagnostic.FortranExponentRejected,
                        $"`{flag.CanonicalFlag}` does not accept the Fortran exponent form `{text}`; write it with `E`", out value, out error);
                }

                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return Fail(Diagnostic.InvalidNumber, $"`{flag.CanonicalFlag}` expects a number, got `{text}`", out value, out error);
                }

                value = number;
                error = null;
                return true;

            case FlagKind.Integer:
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                {
                    return Fail(Diagnostic.InvalidNumber, $"`{flag.CanonicalFlag}` expects an integer, got `{text}`", out value, out error);
                }

                value = integer;
                error = null;
                return true;

            case FlagKind.PositiveInteger:
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var positiveInt) || positiveInt <= 0)
                {
                    return Fail(Diagnostic.InvalidNumber, $"`{flag.CanonicalFlag}` expects a positive integer, got `{text}`", out value, out error);
                }

                value = positiveInt;
                error = null;
                return true;

            case FlagKind.PositiveLong:
                if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var positiveLong) || positiveLong <= 0)
                {
                    return Fail(Diagnostic.InvalidNumber, $"`{flag.CanonicalFlag}` expects a positive integer, got `{text}`", out value, out error);
                }

                value = positiveLong;
                error = null;
                return true;

            case FlagKind.UnsignedLong:
                if (!ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unsignedLong))
                {
                    return Fail(Diagnostic.InvalidNumber, $"`{flag.CanonicalFlag}` expects a non-negative integer, got `{text}`", out value, out error);
                }

                value = unsignedLong;
                error = null;
                return true;

            case FlagKind.Enum:
                if (flag.EnumValues is null || !flag.EnumValues.TryGetValue(text, out var enumValue))
                {
                    var allowed = string.Join(", ", flag.EnumValues?.Keys ?? Array.Empty<string>());
                    return Fail(Diagnostic.InvalidEnumValue,
                        $"`{flag.CanonicalFlag}` expects one of {allowed}, got `{text}`", out value, out error);
                }

                value = enumValue;
                error = null;
                return true;

            case FlagKind.Path:
                value = text;
                error = null;
                return true;

            case FlagKind.Switch:
            default:
                throw new InvalidOperationException($"{flag.Kind} flags take no value to parse.");
        }
    }

    private static bool Fail(Diagnostic diagnostic, string message, out object value, out ParseError? error)
    {
        value = null!;
        error = new ParseError(diagnostic, message);
        return false;
    }
}
