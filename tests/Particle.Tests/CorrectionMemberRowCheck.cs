using System.Globalization;
using System.Numerics;

namespace PropStruct.Particle.Tests;

/// <summary>
/// The member-to-row check of <c>CorrectionSet</c> (<c>src/Particle/ACCEPTANCE.md</c>): reads
/// the generated defect page and this node's <c>API.md</c> as text and reports every way in
/// which they and the enum disagree. It takes the texts as arguments so that a scratch page
/// can be passed through the same path as the real one.
/// </summary>
internal static class CorrectionMemberRowCheck
{
    /// <summary>The reserved name of a fix no flag carries: <c>src/Output</c>'s printing fixes.</summary>
    public const string ReservedMember = "Model";

    private const string FixedPrefix = "fixed (";
    private const string EnumDeclaration = "enum CorrectionSet";

    /// <summary>
    /// The members named in the <c>fixed (…)</c> values of the last column of the page's table
    /// rows, <see cref="ReservedMember"/> excepted.
    /// </summary>
    public static SortedSet<string> FixedMembers(string page)
    {
        var members = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var line in page.Split('\n'))
        {
            var row = line.Trim();
            if (!row.StartsWith('|') || !row.EndsWith('|'))
            {
                continue;
            }

            var withoutClosingBar = row[..^1];
            var corrected = withoutClosingBar[(withoutClosingBar.LastIndexOf('|') + 1)..].Trim();
            if (!corrected.StartsWith(FixedPrefix, StringComparison.Ordinal) || !corrected.EndsWith(')'))
            {
                continue;
            }

            var inner = corrected[FixedPrefix.Length..^1];
            foreach (var name in inner.Split(','))
            {
                var member = name.Trim();
                if (member != ReservedMember)
                {
                    _ = members.Add(member);
                }
            }
        }

        return members;
    }

    /// <summary>The <c>Name = value</c> lines of the enum's code block in the <c>API.md</c> text.</summary>
    public static SortedDictionary<string, uint> ListedValues(string apiText)
    {
        var values = new SortedDictionary<string, uint>(StringComparer.Ordinal);
        var inEnum = false;
        foreach (var line in apiText.Split('\n'))
        {
            var text = line.Trim();
            if (text.Contains(EnumDeclaration, StringComparison.Ordinal))
            {
                inEnum = true;
                continue;
            }

            if (!inEnum)
            {
                continue;
            }

            if (text == "}")
            {
                break;
            }

            var equals = text.IndexOf('=', StringComparison.Ordinal);
            var comma = text.IndexOf(',', StringComparison.Ordinal);
            if (equals < 0 || comma < equals)
            {
                continue;
            }

            var valueText = text[(equals + 1)..comma].Trim();
            if (uint.TryParse(valueText, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                values[text[..equals].Trim()] = value;
            }
        }

        return values;
    }

    /// <summary>
    /// Every disagreement between the page, the <c>API.md</c> text and the enum; empty when
    /// the set of fixed members equals the enum's members other than <c>None</c> and
    /// <c>All</c>, each member has the value <c>API.md</c> lists, the values are distinct
    /// powers of two and <c>All</c> is their sum, in the enum and in <c>API.md</c> alike.
    /// </summary>
    public static List<string> Discrepancies(string page, string apiText)
    {
        var problems = new List<string>();
        var enumValues = EnumMembers();
        var flagNames = enumValues.Keys.Where(IsFlag).ToList();

        var named = FixedMembers(page);
        problems.AddRange(named.Where(name => !enumValues.ContainsKey(name))
            .Select(name => $"the page names '{name}', which CorrectionSet does not have"));
        problems.AddRange(flagNames.Where(name => !named.Contains(name))
            .Select(name => $"CorrectionSet.{name} has no 'fixed ({name})' row on the page"));

        var listed = ListedValues(apiText);
        problems.AddRange(listed.Keys.Where(name => !enumValues.ContainsKey(name))
            .Select(name => $"API.md lists '{name}', which CorrectionSet does not have"));
        foreach (var (name, value) in enumValues)
        {
            if (!listed.TryGetValue(name, out var apiValue))
            {
                problems.Add($"API.md does not list CorrectionSet.{name}");
            }
            else if (apiValue != value)
            {
                problems.Add($"CorrectionSet.{name} is {value} and API.md lists {apiValue}");
            }
        }

        problems.AddRange(PowerOfTwoProblems(enumValues, flagNames, "CorrectionSet"));
        problems.AddRange(PowerOfTwoProblems(listed, listed.Keys.Where(IsFlag).ToList(), "API.md"));
        return problems;
    }

    private static SortedDictionary<string, uint> EnumMembers()
    {
        var members = new SortedDictionary<string, uint>(StringComparer.Ordinal);
        foreach (var flag in Enum.GetValues<CorrectionSet>())
        {
            members[flag.ToString()] = (uint)flag;
        }

        return members;
    }

    private static bool IsFlag(string name) =>
        name is not (nameof(CorrectionSet.None) or nameof(CorrectionSet.All));

    private static IEnumerable<string> PowerOfTwoProblems(
        SortedDictionary<string, uint> values, IReadOnlyList<string> flagNames, string source)
    {
        var sum = 0u;
        foreach (var name in flagNames.Where(values.ContainsKey))
        {
            var value = values[name];
            if (!BitOperations.IsPow2(value))
            {
                yield return $"{source}: {name} = {value} is not a power of two";
            }

            if ((sum & value) != 0)
            {
                yield return $"{source}: {name} = {value} shares a bit with another member";
            }

            sum |= value;
        }

        if (values.TryGetValue(nameof(CorrectionSet.All), out var all) && all != sum)
        {
            yield return $"{source}: All = {all} is not the sum {sum} of the members";
        }
    }
}
