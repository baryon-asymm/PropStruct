using System.Globalization;
using PropStruct.Output;
using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0/L1 of BOOT.md's table for <c>--model original|corrected</c> (src/Cli/BOOT.md, "## Corrected
/// model"): the flag reaches <see cref="SimulationOptions.Model"/> and no other property, the library's
/// refusal of <c>Corrected</c> with <c>--precision original</c> comes back as exit 2 naming both halves,
/// a corrected run succeeds and says so in <c>results.m</c>, an unknown value is refused naming the flag,
/// and the stderr summary line gains <c> model=Corrected</c> after <c>layout=…</c> under
/// <c>Corrected</c> only.
/// </summary>
public class ModelFlagTests
{
    private static readonly string[] NoModelArgs = ["run", "input.dat"];
    private static readonly string[] ModelOriginalArgs = ["run", "input.dat", "--model", "original"];
    private static readonly string[] ModelCorrectedArgs = ["run", "input.dat", "--model", "corrected"];
    private static readonly string[] RunHelpArgs = ["run", "--help"];

    private static SimulationOptions ParseOptions(string[] args)
    {
        var result = CommandLine.Parse(args);
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        return Assert.IsType<RunCommand>(result.Command).Options;
    }

    [Fact]
    public void ModelOriginalAndNoModelFlagGiveTheSameSimulationOptions()
    {
        var withFlag = ParseOptions(ModelOriginalArgs);
        var without = ParseOptions(NoModelArgs);

        Assert.Equal(ModelKind.Original, without.Model);
        Assert.Equal(without, withFlag);
    }

    [Fact]
    public void ModelCorrectedChangesTheModelPropertyAndNoOther()
    {
        var corrected = ParseOptions(ModelCorrectedArgs);

        Assert.Equal(ModelKind.Corrected, corrected.Model);
        Assert.Equal(ParseOptions(NoModelArgs) with { Model = ModelKind.Corrected }, corrected);
    }

    [Fact]
    public void ModelCorrectedWithPrecisionOriginalExitsTwoNamingBothHalves()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[]
            {
                "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--mode", "reference",
                "--model", "corrected", "--precision", "original", "--output", outputPath,
            },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        var message = stderr.ToString();
        Assert.Contains("invalid setup", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Corrected", message, StringComparison.Ordinal);
        Assert.Contains("Original", message, StringComparison.Ordinal);
        Assert.False(File.Exists(outputPath));
    }

    [Theory]
    [InlineData("corrected", ModelKind.Corrected)]
    [InlineData("original", ModelKind.Original)]
    public void ModelFlagRunsToExitZeroAndTheResultRecordsTheModelRun(string value, ModelKind expected)
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");
        var jsonPath = Path.Combine(directory.Path, "results.json");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--model", value, "--output", outputPath, "--json", jsonPath },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.True(File.Exists(outputPath));
        Assert.Equal(expected, ResultsJson.Read(jsonPath).Diagnostics.Model);
    }

    [Theory]
    [InlineData("corrected", 1)]
    [InlineData("original", 0)]
    public void ResultsMCarriesTheModelFooterLineUnderCorrectedOnceAndNeverUnderOriginal(string value, int expectedCount)
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--mode", "reference", "--model", value, "--output", outputPath },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        var footer = DocumentedFooterLine.Read();
        var lines = File.ReadAllLines(outputPath);
        Assert.Equal(expectedCount, lines.Count(line => string.Equals(line, footer, StringComparison.Ordinal)));
        Assert.Equal(expectedCount, lines.Count(line => line.Contains(DocumentedFooterLine.Marker, StringComparison.Ordinal)));
    }

    [Fact]
    public void AnUnknownModelValueExitsTwoNamingTheFlag()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--model", "fixed" },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("--model", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSummaryLineUnderOriginalIsTheLineBeforeTheOption()
    {
        var (summary, expectedBase) = RunAndReadSummary("--model", "original");

        Assert.Equal(expectedBase, summary);
        Assert.DoesNotContain("model=", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSummaryLineWithoutTheFlagIsTheSameLineAsUnderOriginal()
    {
        var (summary, expectedBase) = RunAndReadSummary();

        Assert.Equal(expectedBase, summary);
    }

    [Fact]
    public void TheSummaryLineUnderCorrectedDiffersOnlyByModelCorrectedAfterLayout()
    {
        var (summary, expectedBase) = RunAndReadSummary("--model", "corrected");

        var layoutEnd = expectedBase.IndexOf(" seed=", StringComparison.Ordinal);
        Assert.True(layoutEnd > 0, expectedBase);
        var expected = expectedBase.Insert(layoutEnd, " model=Corrected");
        Assert.Equal(expected, summary);
    }

    [Fact]
    public void TheRunUsageNamesTheModelFlagAndItsSentence()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(RunHelpArgs, stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        var usage = stdout.ToString();
        Assert.Contains("--model original|corrected", usage, StringComparison.Ordinal);
        Assert.Contains("(corrected: binary64 only)", usage, StringComparison.Ordinal);
        Assert.Contains("fixes the original's explicit errors, each marked fixed in", usage, StringComparison.Ordinal);
        Assert.Contains("docs/ORIGINAL-DEFECTS.md", usage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Runs the baseline formulation with <c>--json</c> and returns the stderr summary line together with
    /// the line the documented shape (BOOT.md, "Progress and the summary") yields for the diagnostics the
    /// JSON file holds, with no model field: the fixed text the summary under <c>Original</c> must equal.
    /// </summary>
    private static (string Summary, string ExpectedWithoutModel) RunAndReadSummary(params string[] extraArgs)
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");
        var jsonPath = Path.Combine(directory.Path, "results.json");

        var args = new List<string> { "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath, "--json", jsonPath };
        args.AddRange(extraArgs);

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(args.ToArray(), stdout, stderr, CancellationToken.None);
        Assert.Equal(ExitCode.Success, exitCode);

        var diagnostics = ResultsJson.Read(jsonPath).Diagnostics;
        var inv = CultureInfo.InvariantCulture;
        var expected = $"mode={diagnostics.Mode} accelerator={diagnostics.Accelerator.Kind} batch={diagnostics.BatchSize.ToString(inv)} " +
                       $"layout={diagnostics.Streams} seed={diagnostics.Seed.ToString(inv)} attempts={diagnostics.TotalAttempts.ToString(inv)} " +
                       $"elapsed={diagnostics.Elapsed.ToString("c", inv)} files={outputPath}, {jsonPath}";

        var summary = stderr.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Last();
        return (summary, expected);
    }
}
