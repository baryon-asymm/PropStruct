using System.Globalization;
using PropStruct.Input;

namespace PropStruct.Cli;

/// <summary>Renders a property's runtime value back to the text a user would type for it, invariant
/// culture throughout (BOOT.md, "Parsing is culture-invariant, as Input's is").</summary>
internal static class FlagValueFormatter
{
    public static string Format(object? value) => value switch
    {
        Length length => length.AsWritten.ToString("R", CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        int integer => integer.ToString(CultureInfo.InvariantCulture),
        long integer => integer.ToString(CultureInfo.InvariantCulture),
        ulong integer => integer.ToString(CultureInfo.InvariantCulture),
        null => string.Empty,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty,
    };
}
