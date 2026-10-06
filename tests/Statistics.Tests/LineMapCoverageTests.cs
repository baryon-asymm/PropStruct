using System.Text.Json;
using System.Text.RegularExpressions;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L1 of the BOOT.md table: the line map covers every executable line of the Fortran
/// ranges this node transcribes, the executable-line list generated from the source
/// itself, never typed (AGENTS.md §6, the quantifier "all"; BOOT.md acceptance
/// criteria). The ranges themselves come from the single scope-declaration sentence at
/// the top of BOOT.md's own <c>## Line map</c> section (<see cref="ParseRequiredRanges"/>),
/// following the same pattern as <c>tests/Particle.Tests/LineMapCoverageTests.cs</c>.
/// </summary>
public class LineMapCoverageTests
{
    private static readonly Regex RangePattern = new(@"(\d+)–(\d+)", RegexOptions.Compiled); // en dash

    [Fact]
    public void LineMapCoversEveryExecutableLineOfItsFortranRanges()
    {
        var bootPath = RepositoryPaths.Resolve("src", "Statistics", "BOOT.md");

        var (executableLines, lineCount) = ReadExecutableLines();
        var bootText = File.ReadAllText(bootPath);
        var covered = ParseLineMapCoverage(bootText);
        var requiredRanges = ParseRequiredRanges(bootText);

        var missing = new List<int>();
        foreach (var (start, end) in requiredRanges)
        {
            Assert.True(end <= lineCount, $"The scope names the range {start}-{end}, past the source's {lineCount} lines");
            for (var line = start; line <= end; line++)
            {
                if (executableLines.Contains(line) && !covered.Contains(line))
                {
                    missing.Add(line);
                }
            }
        }

        Assert.True(missing.Count == 0, $"Line map does not cover {missing.Count} executable line(s) of the source, by number: " + string.Join(", ", missing));
    }

    /// <summary>
    /// The numbers of the executable lines of the source, from <c>tests/Fixtures/cases/source/executable_lines.json</c>,
    /// which <c>tests/Fixtures/generate.py derive</c> writes from the source (a line is executable when it is not blank
    /// and does not start with <c>c</c> or <c>C</c>). The source lies outside the repository (<c>tools/legacy</c>), so
    /// this check reads the list and the source's line count, and a failure names line numbers, never a line of the
    /// source. A range of the scope that ends past the line count is a failure, not a pass.
    /// </summary>
    private static (HashSet<int> ExecutableLines, int LineCount) ReadExecutableLines()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Resolve("tests", "Fixtures", "cases", "source", "executable_lines.json")));
        var lines = document.RootElement.GetProperty("executableLines").EnumerateArray().Select(line => line.GetInt32()).ToHashSet();
        Assert.NotEmpty(lines);
        return (lines, document.RootElement.GetProperty("lineCount").GetInt32());
    }

    /// <summary>
    /// The Fortran ranges a coverage check must scan, read from the single
    /// scope-declaration sentence at the top of BOOT.md's <c>## Line map</c> section
    /// (the first paragraph after the heading, before its first blank line): every
    /// <c>start–end</c> (en dash) pair in that one paragraph, in order. Anchoring to
    /// this one sentence, not to the table's own per-row cells, keeps the required
    /// ranges independent of any individual row shrinking by mistake.
    /// </summary>
    private static (int Start, int End)[] ParseRequiredRanges(string bootMd)
    {
        const string heading = "\n## Line map\n";
        var headingIndex = bootMd.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(headingIndex >= 0, "BOOT.md has no '## Line map' section");

        var paragraphStart = headingIndex + heading.Length;
        var paragraphEnd = bootMd.IndexOf("\n\n", paragraphStart, StringComparison.Ordinal);
        Assert.True(paragraphEnd > paragraphStart, "'## Line map' has no scope-declaration paragraph before its first blank line");

        var paragraph = bootMd[paragraphStart..paragraphEnd];
        var matches = RangePattern.Matches(paragraph);
        Assert.True(matches.Count > 0, "the '## Line map' scope-declaration paragraph names no Fortran line range");

        var ranges = new (int Start, int End)[matches.Count];
        for (var i = 0; i < matches.Count; i++)
        {
            ranges[i] = (int.Parse(matches[i].Groups[1].Value), int.Parse(matches[i].Groups[2].Value));
        }

        return ranges;
    }

    /// <summary>
    /// The set of Fortran line numbers named anywhere in BOOT.md's <c>## Line map</c>
    /// table's first column: each cell is a comma-separated list of single numbers or
    /// <c>start–end</c> (en dash) ranges.
    /// </summary>
    private static HashSet<int> ParseLineMapCoverage(string bootMd)
    {
        const string heading = "\n## Line map\n";
        var headingIndex = bootMd.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(headingIndex >= 0, "BOOT.md has no '## Line map' section");

        var sectionStart = headingIndex + heading.Length;
        var nextHeadingIndex = bootMd.IndexOf("\n## ", sectionStart, StringComparison.Ordinal);
        var section = nextHeadingIndex > 0 ? bootMd[sectionStart..nextHeadingIndex] : bootMd[sectionStart..];

        var covered = new HashSet<int>();
        foreach (var rawLine in section.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith('|') || line.StartsWith("|---"))
            {
                continue;
            }

            var cells = line.Split('|');
            if (cells.Length < 2)
            {
                continue;
            }

            var fortranCell = cells[1].Trim();
            if (fortranCell.Length == 0 || fortranCell == "Fortran")
            {
                continue;
            }

            foreach (var part in fortranCell.Split(','))
            {
                var rangeText = part.Trim();
                if (rangeText.Length == 0)
                {
                    continue;
                }

                var bounds = rangeText.Split('–'); // en dash, e.g. "428–446"
                if (bounds.Length == 2 && int.TryParse(bounds[0].Trim(), out var a) && int.TryParse(bounds[1].Trim(), out var b))
                {
                    for (var i = a; i <= b; i++)
                    {
                        _ = covered.Add(i);
                    }
                }
                else if (int.TryParse(rangeText, out var single))
                {
                    _ = covered.Add(single);
                }
            }
        }

        return covered;
    }
}
