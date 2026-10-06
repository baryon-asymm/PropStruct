namespace PropStruct.Protocol.Tests;

/// <summary>
/// The one example of the library in four places (root <c>BOOT.md</c>, "## Delivery"; <c>samples/Quickstart/BOOT.md</c>,
/// "The example is one marked region"): the two marked regions of <c>samples/Quickstart/Program.cs</c>, which is
/// compiled and run, the block of the root <c>API.md</c> ("## Entry points"), the examples of the package README
/// <c>docs/nuget/PropStruct.md</c> and of the repository README. This type reads the regions and the quoting
/// places and says where they differ; <see cref="QuickstartExampleTests"/> asks it about the files of the tree and,
/// for the non-degeneracy proof, about the same text with one character edited.
/// </summary>
internal static class QuickstartExample
{
    public const string UsingsRegion = "QuickstartUsings";

    public const string BodyRegion = "Quickstart";

    private const string RegionEnd = "// snippet-end";

    /// <summary>The lines between <c>// snippet-start: name</c> and the next <c>// snippet-end</c>, the leading
    /// indentation common to every non-blank line stripped, joined with <c>\n</c>.</summary>
    public static string Region(string source, string name)
    {
        var lines = Normalize(source).Split('\n');
        var start = Array.FindIndex(lines, line => line.Trim() == $"// snippet-start: {name}");
        Require(start >= 0, $"no region '{name}' in the sample");
        var end = Array.FindIndex(lines, start + 1, line => line.Trim() == RegionEnd);
        Require(end > start, $"region '{name}' of the sample has no {RegionEnd}");
        return StripIndentation(lines[(start + 1)..end]);
    }

    /// <summary>The example as the root <c>API.md</c> quotes it: the <c>using</c> region, one blank line, the body.</summary>
    public static string ApiBlock(string source) => Region(source, UsingsRegion) + "\n\n" + Region(source, BodyRegion);

    /// <summary>The first <c>csharp</c> fence after the heading that starts with <paramref name="heading"/>.</summary>
    public static string FenceAfterHeading(string markdown, string heading)
    {
        var text = Normalize(markdown);
        var at = text.IndexOf("\n" + heading, StringComparison.Ordinal);
        Require(at >= 0, $"no heading '{heading}'");
        return FenceAfter(text, at, $"heading '{heading}'");
    }

    /// <summary>The <c>csharp</c> fence that follows the line <c>&lt;!-- snippet: name --&gt;</c>.</summary>
    public static string FenceAfterMarker(string markdown, string name)
    {
        var text = Normalize(markdown);
        var at = text.IndexOf($"<!-- snippet: {name} -->", StringComparison.Ordinal);
        Require(at >= 0, $"no marker '{name}'");
        return FenceAfter(text, at, $"marker '{name}'");
    }

    /// <summary>What differs, one line per difference, between the sample's regions and the root <c>API.md</c>,
    /// the package README and the repository README; empty when the four agree byte for byte.</summary>
    public static List<string> Mismatches(string sample, string rootApi, string packageReadme, string repositoryReadme)
    {
        var problems = new List<string>();
        Compare(problems, "the root API.md ('## Entry points')", FenceAfterHeading(rootApi, "## Entry points"), ApiBlock(sample));
        foreach (var (label, readme) in new[] { ("docs/nuget/PropStruct.md", packageReadme), ("README.md", repositoryReadme) })
        {
            Compare(problems, $"{label} ({UsingsRegion})", FenceAfterMarker(readme, UsingsRegion), Region(sample, UsingsRegion));
            Compare(problems, $"{label} ({BodyRegion})", FenceAfterMarker(readme, BodyRegion), Region(sample, BodyRegion));
        }

        return problems;
    }

    private static void Compare(List<string> problems, string place, string quoted, string expected)
    {
        if (quoted != expected)
        {
            var expectedLines = expected.Split('\n');
            var quotedLines = quoted.Split('\n');
            var line = Enumerable.Range(0, Math.Max(expectedLines.Length, quotedLines.Length))
                .First(index => index >= expectedLines.Length || index >= quotedLines.Length || expectedLines[index] != quotedLines[index]);
            problems.Add($"{place} differs from the sample's region at line {line + 1}");
        }
    }

    private static string FenceAfter(string text, int from, string what)
    {
        const string open = "```csharp\n";
        var start = text.IndexOf(open, from, StringComparison.Ordinal);
        Require(start >= 0, $"no csharp fence after {what}");
        var close = text.IndexOf("\n```", start + open.Length, StringComparison.Ordinal);
        Require(close >= 0, $"the csharp fence after {what} is not closed");
        return text[(start + open.Length)..close];
    }

    private static string StripIndentation(string[] lines)
    {
        var indentation = lines.Where(line => line.Trim().Length > 0).Min(line => line.Length - line.TrimStart().Length);
        return string.Join('\n', lines.Select(line => line.Trim().Length == 0 ? string.Empty : line[indentation..]));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
