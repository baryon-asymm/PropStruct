using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table: "`--help`, `--help` per verb, `--version`, unknown verb, no verb, a flag
/// given twice, `--switch=value`, a value beginning with `-`, a path after `--`" - checked against the
/// exact texts and exit codes, driven through <see cref="Program.Run"/> in-process (BOOT.md, "The entry
/// point is a function, not a process").
/// </summary>
public class ProgramTextAndExitCodeTests
{
    private static (int ExitCode, string Stdout, string Stderr) RunTool(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(args, stdout, stderr, CancellationToken.None);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    public void BareHelpPrintsGeneralUsageToStdoutAndExitsZero()
    {
        var (exitCode, stdout, stderr) = RunTool("--help");
        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Equal(UsageText.General, stdout.TrimEnd());
        Assert.Empty(stderr);
    }

    [Theory]
    [InlineData("run")]
    [InlineData("devices")]
    [InlineData("defaults")]
    public void HelpAfterAVerbPrintsThatVerbsUsageToStdoutAndExitsZero(string verb)
    {
        var (exitCode, stdout, _) = RunTool(verb, "--help");
        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Equal(UsageText.ForVerb(verb), stdout.TrimEnd());
    }

    [Fact]
    public void VersionPrintsToStdoutAndExitsZero()
    {
        var (exitCode, stdout, stderr) = RunTool("--version");
        Assert.Equal(ExitCode.Success, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stdout));
        Assert.Empty(stderr);
    }

    [Fact]
    public void UnknownVerbPrintsUsageToStdoutAndTheDiagnosticToStderrAndExitsTwo()
    {
        var (exitCode, stdout, stderr) = RunTool("fly");
        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("fly", stderr, StringComparison.Ordinal);
        Assert.Equal(UsageText.General, stdout.TrimEnd());
    }

    [Fact]
    public void NoVerbPrintsUsageToStdoutAndExitsTwo()
    {
        var (exitCode, stdout, stderr) = RunTool();
        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stderr));
        Assert.Equal(UsageText.General, stdout.TrimEnd());
    }

    [Fact]
    public void FlagGivenTwicePrintsTheDiagnosticToStderrAndExitsTwo()
    {
        var (exitCode, _, stderr) = RunTool("run", "input.dat", "--dmin", "1", "--dmin", "2");
        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("--dmin", stderr, StringComparison.Ordinal);
        Assert.Contains("twice", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void SwitchEqualsValuePrintsTheDiagnosticToStderrAndExitsTwo()
    {
        var (exitCode, _, stderr) = RunTool("run", "input.dat", "--sfr=true");
        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("--sfr", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void ValueBeginningWithDashIsAcceptedVerbatimNotAsAnUnknownFlag()
    {
        // "input.dat" does not exist, so this still fails - but by reaching DatFile.Read at all (exit 2,
        // "input.dat" named as the missing file), never by CommandLine.Parse rejecting "-1" as an unknown
        // option (also exit 2, but naming "-1" instead, before the file is ever looked up). Reviewed
        // 2026-09-20: the previous assertion only checked the text "unknown option" was absent, which
        // would also pass if the parser had, say, silently dropped "-1" and "--eta" both - a positive
        // assertion that parsing succeeded and reached the file lookup is the actual proof.
        var (exitCode, _, stderr) = RunTool("run", "input.dat", "--eta", "-1");
        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("input.dat", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("--eta", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("unknown option", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathAfterDoubleDashMayBeginWithDash()
    {
        // The file will not be found (it does not exist), but reaching that failure at all, with the
        // dash-led name intact, is exactly what this test is proving: a `--` before it must stop the
        // parser from ever treating "--this-looks-like-a-flag.dat" as a flag. Reviewed 2026-09-20: the
        // previous assertion only checked the text "unknown option" was absent, which says nothing about
        // whether the path actually arrived at DatFile.Read looking the way it was written.
        var (exitCode, _, stderr) = RunTool("run", "--", "--this-looks-like-a-flag.dat");
        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("--this-looks-like-a-flag.dat", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("unknown option", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
