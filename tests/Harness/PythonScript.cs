using System.Diagnostics;

namespace PropStruct.Tests.Harness;

/// <summary>
/// What a finished Python run left behind: the exit code and everything it wrote to the two streams.
/// </summary>
/// <param name="ExitCode">The process's exit code.</param>
/// <param name="StandardOutput">Everything the script wrote to standard output.</param>
/// <param name="StandardError">Everything the script wrote to standard error.</param>
public sealed record PythonRun(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Runs one of the tree's Python scripts (the generators and verifiers of the nodes) from a test, the one place
/// the way <c>python</c> is found, started and read lives. The interpreter is the <c>python</c> on the PATH,
/// started as <c>python -X utf8 &lt;script&gt; &lt;arguments&gt;</c> without a shell, both streams redirected and read
/// in full, with no timeout.
/// </summary>
public static class PythonScript
{
    /// <summary>
    /// Runs <paramref name="script"/> with <paramref name="arguments"/> in <paramref name="workingDirectory"/> and
    /// returns what it did, whatever its exit code was.
    /// </summary>
    /// <param name="workingDirectory">The process's working directory.</param>
    /// <param name="script">The script as python is given it: relative to <paramref name="workingDirectory"/>, or absolute.</param>
    /// <param name="arguments">The script's own arguments, in order.</param>
    /// <returns>The exit code and both streams.</returns>
    /// <exception cref="InvalidOperationException">The interpreter did not start.</exception>
    public static PythonRun Run(string workingDirectory, string script, IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo("python")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-X");
        startInfo.ArgumentList.Add("utf8");
        startInfo.ArgumentList.Add(script);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("could not start python.");
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new PythonRun(process.ExitCode, standardOutput, standardError);
    }

    /// <summary>
    /// Requires that <paramref name="script"/>, run from the repository root with <paramref name="arguments"/>, exits
    /// with code 0, and fails the calling test otherwise by throwing, as <see cref="BitSnapshot.Verify"/> does: this
    /// node does not reference xunit, which would make its surface a test assembly's (Protocol.Tests). The message
    /// names the script's file name, its arguments, the exit code and both streams.
    /// </summary>
    /// <param name="script">The script, relative to the repository root.</param>
    /// <param name="arguments">The script's own arguments, in order.</param>
    /// <exception cref="InvalidOperationException">The exit code was not 0, or the interpreter did not start.</exception>
    public static void RequireSuccess(string script, params string[] arguments)
    {
        var run = Run(RepositoryPaths.Root, script, arguments);

        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{Path.GetFileName(script)} {string.Join(' ', arguments)} exited {run.ExitCode}.\nstdout: {run.StandardOutput}\nstderr: {run.StandardError}");
        }
    }
}
