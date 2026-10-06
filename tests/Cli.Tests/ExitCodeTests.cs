using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L1 of the BOOT.md table: "each exit code (0, 1, 2, 3, 4, 130) is produced, each by its own cause" -
/// one test per code, driven through <see cref="Program.Run"/> in-process except 130, which the design
/// (BOOT.md, "The entry point is a function, not a process") reserves for the cancellation token alone,
/// with no process-level Ctrl+C test.
/// </summary>
public class ExitCodeTests
{
    private static (int ExitCode, string Stdout, string Stderr) RunTool(CancellationToken token, params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(args, stdout, stderr, token);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static (int ExitCode, string Stdout, string Stderr) RunTool(params string[] args) => RunTool(CancellationToken.None, args);

    [Fact]
    public void ExitZeroASuccessfulRunWritesResultsMAndPrintsTheSummary()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        var (exitCode, stdout, stderr) = RunTool(
            "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Empty(stdout);
        Assert.True(File.Exists(outputPath));
        Assert.False(string.IsNullOrWhiteSpace(stderr)); // the summary line, unless --quiet
    }

    [Fact]
    public void ExitZeroWithQuietPrintsNothingToEitherStream()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        var (exitCode, stdout, stderr) = RunTool(
            "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath, "--quiet");

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Empty(stdout);
        Assert.Empty(stderr);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void ExitOneAFailedRunStatusNamesTheStatusAndItsBudgetFlag()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        // One attempt per particle, virtually guaranteed to exhaust the cap before any particle succeeds.
        var (exitCode, _, stderr) = RunTool(
            "run", BaselineFormulation.DatPath, "--accelerator", "cpu",
            "--max-attempts-per-particle", "1", "--output", outputPath);

        Assert.Equal(ExitCode.RunFailed, exitCode);
        Assert.Contains("AttemptCapExceeded", stderr, StringComparison.Ordinal);
        Assert.Contains("--max-attempts-per-particle", stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void ExitTwoAMalformedInputFileNamesTheLine()
    {
        var badGsvPath = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "input", "bad-gsv.dat");

        var (exitCode, _, stderr) = RunTool("run", badGsvPath, "--accelerator", "cpu");

        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        // Line 6 is bad-gsv.dat's own "GSV, GSV is not 2" row (independently confirmed by catching
        // FormulationFormatException.LineNumber directly from DatFile.Read against this same fixture,
        // reviewed 2026-09-20): the path alone does not prove the line number reached stderr.
        Assert.Contains($"{badGsvPath}:6:", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void ExitThreeAMissingOutputDirectoryNamesTheFullPath()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), "propstruct-cli-tests-missing-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(missingDirectory, "results.m");

        var (exitCode, _, stderr) = RunTool("run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath);

        Assert.Equal(ExitCode.InfrastructureError, exitCode);
        Assert.Contains(missingDirectory, stderr, StringComparison.Ordinal);
        Assert.False(Directory.Exists(missingDirectory));
    }

    [Fact]
    public void ExitFourAnUnhandledExceptionIsCaughtByTheRealDispatchWiring()
    {
        // The catch-all branch exists for a defect of the tool: by construction, nothing in a correct
        // run should ever reach it. A null argv element is not something a real OS-level Main ever hands
        // us, but Program.Run is itself a public in-process entry point (BOOT.md, "The entry point is a
        // function, not a process") that accepts any string[] - this is the one input that reliably makes
        // the parser throw (NullReferenceException from a null token's own .StartsWith), driving the real
        // dispatch-to-mapper wiring end to end instead of calling Program.MapExceptionToExitCode directly
        // (reviewed 2026-09-20).
        var (exitCode, _, stderr) = RunTool("run", null!);

        Assert.Equal(ExitCode.UnhandledException, exitCode);
        Assert.Contains(nameof(NullReferenceException), stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void DevicesCancelledBeforeDispatchReturns130Too()
    {
        // BOOT.md's own dispatch wrapping (reviewed 2026-09-20) is not run-only: a token already
        // cancelled before any verb starts must interrupt devices/defaults/help/version exactly like run.
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var (exitCode, _, stderr) = RunTool(cancellationSource.Token, "devices");

        Assert.Equal(ExitCode.Interrupted, exitCode);
        Assert.Contains("interrupted", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Exit130CancelledThroughTheTokenPrintsInterruptedAndWritesNoOutput()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var (exitCode, _, stderr) = RunTool(
            cancellationSource.Token, "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--output", outputPath);

        Assert.Equal(ExitCode.Interrupted, exitCode);
        Assert.Contains("interrupted", stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(outputPath));
    }
}
