using System.Collections;
using System.Globalization;
using System.Reflection;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Two values of the same type compared member by member, a <c>double</c> by its bits: every public field and
/// property of a record or struct, every element of an array. Used where a claim is "bit for bit" over a whole result
/// (<c>CorrectionSeamTests</c>), so that a field added to a result later is compared without the test being edited.
/// </summary>
internal static class BitwiseComparison
{
    /// <summary>One line per member whose value differs, named by its path from <paramref name="name"/>; empty when equal.</summary>
    public static IReadOnlyList<string> Differences(object? expected, object? actual, string name)
    {
        var lines = new List<string>();
        Compare(expected, actual, name, lines);
        return lines;
    }

    private static void Compare(object? expected, object? actual, string path, List<string> lines)
    {
        if (expected is null || actual is null)
        {
            if (expected is not null || actual is not null)
            {
                lines.Add($"{path}: {Describe(expected)} against {Describe(actual)}");
            }

            return;
        }

        if (expected is double expectedDouble && actual is double actualDouble)
        {
            if (BitConverter.DoubleToInt64Bits(expectedDouble) != BitConverter.DoubleToInt64Bits(actualDouble))
            {
                lines.Add($"{path}: {Describe(expected)} against {Describe(actual)}");
            }

            return;
        }

        var type = expected.GetType();
        if (type != actual.GetType())
        {
            lines.Add($"{path}: type {type.Name} against {actual.GetType().Name}");
            return;
        }

        if (expected is IList expectedList && actual is IList actualList)
        {
            CompareLists(expectedList, actualList, path, lines);
            return;
        }

        if (type.IsPrimitive || type.IsEnum || expected is string)
        {
            if (!expected.Equals(actual))
            {
                lines.Add($"{path}: {Describe(expected)} against {Describe(actual)}");
            }

            return;
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            Compare(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}", lines);
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0))
        {
            Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}", lines);
        }
    }

    private static void CompareLists(IList expected, IList actual, string path, List<string> lines)
    {
        if (expected.Count != actual.Count)
        {
            lines.Add($"{path}: length {expected.Count} against {actual.Count}");
            return;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            Compare(expected[i], actual[i], $"{path}[{i}]", lines);
        }
    }

    private static string Describe(object? value) => value switch
    {
        null => "null",
        double d => $"{d.ToString("R", CultureInfo.InvariantCulture)} (0x{BitConverter.DoubleToInt64Bits(d):X16})",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };
}
