using System.Diagnostics;
using System.Runtime.InteropServices;
using PropStruct.Execution;
using PropStruct.Tests.Harness;

namespace PropStruct.Benchmarks;

/// <summary>Everything BOOT.md's Invariants require beside the date, formulation and figure
/// itself: "the machine (CPU, logical cores, GPU, driver), the .NET version, the commit, the
/// exact command, and the formulation with its cycle and particle count." One instance covers
/// the whole invocation (every row of one run shares it); the per-row cycle/particle count is
/// carried by <see cref="FigureRow"/> instead, since it can differ between paths when
/// <c>--particles</c> is given only for the smoke configuration.</summary>
internal sealed record Provenance(string DateUtc, string Machine, string DotNetVersion, string Commit, string CommandLine);

internal static class ProvenanceCollector
{
    private const string DirtySuffix = "-dirty";

    public static Provenance Collect(string commandLine, IReadOnlyList<AcceleratorInfo> accelerators)
    {
        var cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")
            ?? RuntimeInformation.ProcessArchitecture.ToString();
        var logicalCores = Environment.ProcessorCount;

        var cuda = accelerators.FirstOrDefault(a => a.Kind == AcceleratorKind.Cuda);
        var gpu = cuda is null
            ? "no CUDA device"
            : $"{cuda.Name} (libdevice {(cuda.LibDeviceLinked ? "linked" : "not linked")})";

        var machine = $"{cpu}, {logicalCores} logical CPUs, {gpu}, {RuntimeInformation.OSDescription}";
        var dotNetVersion = RuntimeInformation.FrameworkDescription;
        var commit = ReadCommit();

        return new Provenance(DateTime.UtcNow.ToString("yyyy-MM-dd"), machine, dotNetVersion, commit, commandLine);
    }

    /// <summary>The commit a figure was built from: <c>git rev-parse --short HEAD</c>, followed by
    /// <c>-dirty</c> when <c>git status --porcelain</c> reports anything. Untracked files count,
    /// ignored ones (<c>bin/</c>, <c>obj/</c>) do not: the project globs its sources, so a new
    /// untracked <c>.cs</c> file is built into the figure exactly as a modified tracked one is.
    /// <c>unknown</c> when git cannot answer either question, never a bare hash that might be
    /// stale (BOOT.md, Invariants: "A figure without provenance is not a figure").</summary>
    private static string ReadCommit()
    {
        var head = RunGit("rev-parse --short HEAD");
        var status = RunGit("status --porcelain");
        if (string.IsNullOrEmpty(head) || status is null)
        {
            return "unknown";
        }

        return status.Length > 0 ? head + DirtySuffix : head;
    }

    /// <summary>The trimmed standard output of one git call in the repository root (empty for a
    /// clean <c>status --porcelain</c>), or <see langword="null"/> when git is missing or exits
    /// non-zero.</summary>
    private static string? RunGit(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = RepositoryPaths.Root,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
