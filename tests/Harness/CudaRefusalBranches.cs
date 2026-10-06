using System.Text.RegularExpressions;

namespace PropStruct.Tests.Harness;

/// <summary>
/// What a scan of a test node's sources found of the branches that decide on <c>CudaSkippedBecause</c>: how many
/// there are and which of them do not call <see cref="CudaRequirement.FailIfRequired(string)"/>.
/// </summary>
/// <param name="Branches">The number of conditions on <c>CudaSkippedBecause is [not] null</c> found.</param>
/// <param name="Unguarded">One entry per branch whose refused side lacks the call, <c>path:line</c>.</param>
public sealed record CudaRefusalScan(int Branches, IReadOnlyList<string> Unguarded);

/// <summary>
/// The text check behind "every fact that returns when no CUDA accelerator is available calls
/// <see cref="CudaRequirement.FailIfRequired(string)"/>": a fact that runs on CUDA where an accelerator is bound
/// branches on the engine's own <c>CudaSkippedBecause</c>, and the list of those branches is found in the
/// sources, not typed. The refused side of <c>if (x.CudaSkippedBecause is not null)</c> is its block; of
/// <c>is null</c>, its <c>else</c>, which must exist. A condition split over several lines is not read.
/// </summary>
public static partial class CudaRefusalBranches
{
    private static readonly string[] SkippedDirectories = ["bin", "obj"];

    /// <summary>Scans every <c>.cs</c> file under <paramref name="directory"/>, build output excluded.</summary>
    /// <param name="directory">A test node's directory.</param>
    /// <returns>The branches found and the unguarded ones.</returns>
    public static CudaRefusalScan Scan(string directory)
    {
        var branches = 0;
        var unguarded = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(directory, path).Replace('\\', '/');
            if (relative.Split('/').Any(segment => SkippedDirectories.Contains(segment, StringComparer.Ordinal)))
            {
                continue;
            }

            var found = Branches(File.ReadAllText(path));
            branches += found.Count;
            unguarded.AddRange(found.Where(branch => !branch.Guarded).Select(branch => $"{relative}:{branch.Line}"));
        }

        return new CudaRefusalScan(branches, unguarded);
    }

    internal readonly record struct Branch(int Line, bool Guarded);

    internal static IReadOnlyList<Branch> Branches(string source)
    {
        var lineStarts = new List<int> { 0 };
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                lineStarts.Add(i + 1);
            }
        }

        var branches = new List<Branch>();
        foreach (Match condition in Condition().Matches(source))
        {
            var lineIndex = lineStarts.BinarySearch(condition.Index);
            lineIndex = lineIndex >= 0 ? lineIndex : ~lineIndex - 1;
            var lineStart = lineStarts[lineIndex];
            if (source.AsSpan(lineStart, condition.Index - lineStart).TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var (first, afterFirst) = Statement(source, condition.Index + condition.Length);
            var refused = condition.Groups["not"].Success ? first : Else(source, afterFirst);

            branches.Add(new Branch(lineIndex + 1, refused.Contains("CudaRequirement.FailIfRequired(", StringComparison.Ordinal)));
        }

        return branches;
    }

    [GeneratedRegex(@"\bif\s*\([^\r\n]*CudaSkippedBecause\s+is\s+(?<not>not\s+)?null\s*\)")]
    private static partial Regex Condition();

    /// <summary>The block or the single statement starting at <paramref name="index"/>, and the index after it.</summary>
    private static (string Text, int End) Statement(string source, int index)
    {
        var start = index;
        while (start < source.Length)
        {
            if (char.IsWhiteSpace(source[start]))
            {
                start++;
            }
            else if (source.AsSpan(start).StartsWith("//", StringComparison.Ordinal))
            {
                var newline = source.IndexOf('\n', start);
                start = newline < 0 ? source.Length : newline + 1;
            }
            else
            {
                break;
            }
        }

        if (start >= source.Length)
        {
            return (string.Empty, source.Length);
        }

        if (source[start] != '{')
        {
            var semicolon = source.IndexOf(';', start);
            var end = semicolon < 0 ? source.Length : semicolon + 1;
            return (source[start..end], end);
        }

        var depth = 0;
        for (var i = start; i < source.Length; i++)
        {
            depth += source[i] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return (source[start..(i + 1)], i + 1);
            }
        }

        return (source[start..], source.Length);
    }

    /// <summary>The text of the <c>else</c> that follows at <paramref name="index"/>, empty when there is none.</summary>
    private static string Else(string source, int index)
    {
        var rest = source.AsSpan(index).TrimStart();
        if (!rest.StartsWith("else", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var afterKeyword = source.Length - rest.Length + "else".Length;
        return Statement(source, afterKeyword).Text;
    }
}
