using System.Diagnostics;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L1 of the BOOT.md table, renamed per review (2026-09-20) to match what it actually proves: a real
/// process run with its standard input closed does not hang waiting on it (a closed stdin makes
/// <c>Console.ReadLine</c> return <c>null</c> rather than block, so this alone cannot tell "the tool
/// never reads the console" from "the tool reads it once and moves on"). "The tool never reads the
/// console" itself is <see cref="SourceCodeTests.NoConsoleReadAppearsUnderSrcCli"/>, a source-level scan.
/// </summary>
public class StdinClosedTests
{
    private static int RunWithClosedStdin(TimeSpan timeout, params string[] args)
    {
        var startInfo = new ProcessStartInfo(PropstructExecutable.Path)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start propstruct.");
        process.StandardInput.Close(); // stdin closed before the tool does anything.

        var readOutput = process.StandardOutput.ReadToEndAsync();
        var readError = process.StandardError.ReadToEndAsync();

        var finished = process.WaitForExit((int)timeout.TotalMilliseconds);
        if (!finished)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"propstruct {string.Join(' ', args)} did not finish within {timeout} with stdin closed.");
        }

        Task.WaitAll(readOutput, readError);
        return process.ExitCode;
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    [InlineData("devices")]
    [InlineData("defaults")]
    public void QuickVerbsDoNotHangWithStdinClosed(string arg)
    {
        var exitCode = RunWithClosedStdin(TimeSpan.FromSeconds(15), arg);
        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact]
    public void RunDoesNotHangWithStdinClosed()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        var exitCode = RunWithClosedStdin(
            TimeSpan.FromSeconds(30),
            "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--quiet", "--output", outputPath);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.True(File.Exists(outputPath));
    }
}
