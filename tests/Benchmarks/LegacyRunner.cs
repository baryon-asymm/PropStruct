using System.Diagnostics;
using PropStruct.Tests.Harness;

namespace PropStruct.Benchmarks;

/// <summary>Thrown when the legacy executable, its runtime or the requested formulation is not
/// present on this machine, or is not the recorded one (the message is then that of
/// <c>tools/legacy/legacy.py path</c>); <see cref="Program"/> turns this into a "not measured on this
/// machine" figure rather than a blank or a zero (API.md, "unavailable ones are reported as not
/// measured, never as zero").</summary>
internal sealed class LegacyUnavailableException : Exception
{
    public LegacyUnavailableException()
    {
    }

    public LegacyUnavailableException(string message)
        : base(message)
    {
    }

    public LegacyUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Runs the original executable once and times its whole wall clock (BOOT.md, Invariants: "The
/// original's figure is measured, never remembered... through `tests/Fixtures`' provenance
/// rules"). Follows the interactive protocol every reference and replica output already uses
/// (standard input <c>"&lt;name&gt;\ny\n"</c>: the formulation's file-stem name, then "y" for
/// the results.m overwrite prompt) in a fresh temporary directory holding copies of the
/// executable, its runtime and the formulation — the same convention
/// <c>tests/Fixtures/generate.py</c> uses to run this same binary, so a benchmark run leaves no
/// trace beside the copy it deletes when it is done. The executable and its runtime lie outside
/// the repository (<c>tools/legacy</c>): <see cref="VerifiedPath"/> asks <c>legacy.py path</c>
/// for each, so that an unset <c>PROPSTRUCT_LEGACY_DIR</c> or a file that is not the recorded one
/// ends the row with that command's own message and never falls back to another path.
///
/// This node never patches the legacy `.dat`'s own particle count: <c>--particles</c> (this
/// node's API.md) only overrides the port's paths, since a Fortran fixed-format record cannot
/// safely be edited without reading `Input`'s own parser, which this node may not do (a
/// neighbour's code, AGENTS.md §3). The `original` row always measures the formulation's own
/// shipped `N` and `KXX`.
/// </summary>
internal static class LegacyRunner
{
    internal readonly record struct Measurement(long AcceptedParticles, double ElapsedSeconds);

    /// <summary>The path of one of the original's five files, as <c>tools/legacy/legacy.py path</c> prints it
    /// after comparing the file with the recorded hash.</summary>
    /// <exception cref="LegacyUnavailableException">The command failed; the message is its own.</exception>
    private static string VerifiedPath(string manifestName)
    {
        var run = PythonScript.Run(RepositoryPaths.Root, Path.Combine("tools", "legacy", "legacy.py"), ["path", manifestName]);
        return run.ExitCode == 0
            ? run.StandardOutput.Trim()
            : throw new LegacyUnavailableException(run.StandardError.Trim());
    }

    public static async Task<Measurement> RunAsync(string formulationName, int cycles, int particlesPerCycle)
    {
        var exePath = VerifiedPath("PropStructV3.exe");
        var runtimePath = VerifiedPath("dforrt.dll");
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", $"{formulationName}.dat");

        if (!File.Exists(datPath))
        {
            throw new LegacyUnavailableException($"formulation file not found: {datPath}");
        }

        var workDirectory = Path.Combine(Path.GetTempPath(), "propstruct-benchmark-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(workDirectory);
        try
        {
            var workExePath = Path.Combine(workDirectory, "PropStructV3.exe");
            File.Copy(exePath, workExePath);
            File.Copy(runtimePath, Path.Combine(workDirectory, "dforrt.dll"));
            File.Copy(datPath, Path.Combine(workDirectory, $"{formulationName}.dat"));

            var startInfo = new ProcessStartInfo(workExePath)
            {
                WorkingDirectory = workDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo)
                ?? throw new LegacyUnavailableException("could not start the legacy executable");

            // Drained concurrently with WaitForExitAsync, not after it: the executable's own
            // console output could otherwise fill the redirected pipe and deadlock the run
            // (the classic Process redirection trap).
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            await process.StandardInput.WriteAsync($"{formulationName}\ny\n").ConfigureAwait(false);
            process.StandardInput.Close();

            var stopwatch = Stopwatch.StartNew();
            await process.WaitForExitAsync().ConfigureAwait(false);
            stopwatch.Stop();
            _ = await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new LegacyUnavailableException(
                    $"the legacy executable exited with code {process.ExitCode}: {(await stdErrTask.ConfigureAwait(false)).Trim()}");
            }

            // Cycle 0 (the warm-up) runs too, exactly as it does in the port (root BOOT.md,
            // "Execution model"): the original's own wall time has no per-cycle breakdown, so
            // the accepted-particle count of this row is the whole run's, not cycle 1's alone
            // (this node's BOOT.md, "Original's particles/s").
            var acceptedParticles = (long)(cycles + 1) * particlesPerCycle;
            return new Measurement(acceptedParticles, stopwatch.Elapsed.TotalSeconds);
        }
        finally
        {
            try
            {
                Directory.Delete(workDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort: a lingering antivirus lock on the temporary copy is not this
                // node's figure to fail over.
            }
        }
    }
}
