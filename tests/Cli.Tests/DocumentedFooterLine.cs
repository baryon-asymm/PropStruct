using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// The text of the <c>results.m</c> footer line under the <c>Corrected</c> model, read where the Output
/// node documents it (src/Output/BOOT.md, "## Corrected model", the code block under "The footer
/// line") instead of being typed into a test (the root BOOT.md's taboo on expected values typed where a
/// file holds them).
/// </summary>
internal static class DocumentedFooterLine
{
    private const string AnchorText = "**The footer line**";
    private const string FenceText = "```text";

    /// <summary>The words every footer line carries after its comment sign.</summary>
    public const string Marker = "% Model kind:";

    /// <summary>
    /// The first line of the code block that follows the anchor, without the indentation of the fence
    /// that holds it, so that the line's own leading blank is kept.
    /// </summary>
    public static string Read()
    {
        var lines = File.ReadAllLines(RepositoryPaths.Resolve("src", "Output", "BOOT.md"));
        var anchor = Array.FindIndex(lines, line => line.Contains(AnchorText, StringComparison.Ordinal));
        Assert.True(anchor >= 0, "src/Output/BOOT.md no longer carries the footer line's anchor.");
        var fence = Array.FindIndex(lines, anchor, line => line.TrimStart().StartsWith(FenceText, StringComparison.Ordinal));
        Assert.True(fence >= 0, "The footer line's code block is missing.");
        var indent = lines[fence].Length - lines[fence].TrimStart().Length;
        var text = lines[fence + 1][indent..];
        Assert.Contains(Marker, text, StringComparison.Ordinal);
        return text;
    }
}
