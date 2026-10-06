using System.Text.RegularExpressions;
using PropStruct.Output;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L1: the content of what a successful <c>run</c> prints and writes beyond "it exists" - `--json`'s own
/// file, the summary line's fields, and the progress line's format (BOOT.md, "Progress and the summary";
/// API.md, "Output"). Reviewed 2026-09-20: these were ✅ in API.md with only "a file gets written"/"a
/// line gets printed" behind them.
/// </summary>
public class RunOutputContentTests
{
    private static readonly Regex ProgressLine = new(@"^cycle \d+ of \d+, particles \d+$", RegexOptions.Compiled);
    private static readonly Regex SummaryLine = new(
        @"^mode=\w+ accelerator=\w+ batch=\d+ layout=\w+ seed=\d+ attempts=\d+ elapsed=[\d:.]+ files=.+$",
        RegexOptions.Compiled);

    [Fact]
    public void RunWithJsonWritesAResultsJsonFileThatMatchesTheSummaryLinesOwnAttemptCount()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");
        var jsonPath = Path.Combine(directory.Path, "results.json");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath, "--json", jsonPath },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.True(File.Exists(jsonPath));

        var result = ResultsJson.Read(jsonPath);

        var summaryLine = stderr.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Last();
        var attemptsInSummary = long.Parse(Regex.Match(summaryLine, @"attempts=(\d+)").Groups[1].Value);
        Assert.Equal(result.Diagnostics.TotalAttempts, attemptsInSummary);
        Assert.Contains(jsonPath, summaryLine, StringComparison.Ordinal);
        Assert.Contains(outputPath, summaryLine, StringComparison.Ordinal);
    }

    [Fact]
    public void RunSummaryLineMatchesTheDocumentedShape()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        var lines = stderr.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Matches(SummaryLine, lines.Last());
    }

    [Fact]
    public void RunWithoutQuietPrintsAtLeastOneProgressLineInTheDocumentedShape()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--mode", "reference", "--accelerator", "cpu", "--output", outputPath },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        var lines = stderr.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(lines, ProgressLine.IsMatch);
    }
}
