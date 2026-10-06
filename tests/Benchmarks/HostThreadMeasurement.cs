using System.Diagnostics;

namespace PropStruct.Benchmarks;

/// <summary>
/// Measures the host-thread path forced to a given number of logical processors (BOOT.md,
/// Purpose: "the host-thread path on 1 and on 16 threads"; "## Host-thread counts" below).
///
/// <c>Simulation</c>'s public <c>SimulationOptions</c> has no thread-count field — the
/// <c>cpuThreads</c> parameter that drives <c>System.Threading.Tasks.ParallelOptions.
/// MaxDegreeOfParallelism</c> for the host-thread path's own <c>Parallel.For</c> calls lives on
/// <c>Execution.Engine.Create</c>, which is internal (src/Execution/API.md), and
/// <c>Simulator.Create</c> takes only <c>SimulationOptions</c>, no such knob. Forcing the count
/// from outside the library therefore has to happen a level below the library: the .NET runtime
/// reads the environment variable <c>DOTNET_PROCESSOR_COUNT</c> at process start and reports it
/// back through <see cref="Environment.ProcessorCount"/> for the rest of that process's life.
/// That changes how <c>Parallel.For</c> partitions its range (it consults the count when
/// <c>MaxDegreeOfParallelism</c> is left at -1, "all cores", the production default named in
/// src/Execution/API.md); it is NOT a cap on how many threads run: the thread pool and the
/// scheduler still use every logical CPU the process may run on. Verified against a throwaway
/// console app that <c>Environment.ProcessorCount</c> reads 16 unset and 1 with
/// <c>DOTNET_PROCESSOR_COUNT=1</c>, and measured 2026-10-02 that the <c>host1</c> child so
/// launched used more than one CPU (BOOT.md, "## Host-thread counts", ⚠ 2026-10-02). So the
/// <c>host1</c> child is also pinned to one logical CPU with <c>Process.ProcessorAffinity</c>,
/// which is a cap: one logical CPU cannot run more than one thread at a time. The <c>host16</c>
/// child is not pinned.
///
/// Because that variable is read once, at CLR start, it cannot be set on the running process —
/// only on a fresh one. This node relaunches itself as a child process, in its own hidden
/// <c>--internal-cycle1-run</c> mode (<see cref="Program"/>), once per repeat, with the
/// variable set on that child alone; nothing about <c>Simulation</c>'s own contract or cycle
/// policy is bypassed, the whole cycle still runs through <c>Simulator.Run</c> exactly as a
/// user's own process would (BOOT.md, Invariants: "not from an internal loop that skips the
/// cycle policy" — the loop here is one repeat, not one particle).
/// </summary>
internal static class HostThreadMeasurement
{
    public static async Task<FigureOutcome> MeasureAsync(
        string formulationName, int threads, int repeats, int? particlesOverride, int? attemptsPerLaunchOverride,
        string ownAssemblyPath)
    {
        var samples = new List<double>(repeats);
        var warmupIncludedAnyRepeat = false;
        var lastAttemptsPerLaunch = 0;
        var lastTotalAttempts = 0L;
        var lastTotalLaunches = 0L;

        for (var i = 0; i < repeats; i++)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(ownAssemblyPath);
            startInfo.ArgumentList.Add(Program.InternalRunFlagName);
            startInfo.ArgumentList.Add(formulationName);
            startInfo.ArgumentList.Add(particlesOverride?.ToString() ?? "");
            startInfo.ArgumentList.Add(attemptsPerLaunchOverride?.ToString() ?? "");
            startInfo.Environment["DOTNET_PROCESSOR_COUNT"] = threads.ToString();

            using var process = Process.Start(startInfo)
                ?? throw new LegacyUnavailableException("could not relaunch the benchmark process for the host-thread path");

            if (threads == 1 && !TryPinToOneLogicalCpu(process))
            {
                return new NotMeasuredOnThisMachine("host1 child could not be pinned to one logical CPU");
            }

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().ConfigureAwait(false);
            var stdOut = await stdOutTask.ConfigureAwait(false);
            var stdErr = await stdErrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return new NotMeasuredOnThisMachine($"host-thread relaunch failed: {stdErr.Trim()}");
            }

            var parsed = Program.ParseInternalResult(stdOut);
            if (parsed is not { } p)
            {
                return new NotMeasuredOnThisMachine($"host-thread relaunch printed no result: {stdOut.Trim()}");
            }

            var m = p.Measurement;
            warmupIncludedAnyRepeat |= !m.Cycle0IsolationConfirmed;
            samples.Add(m.AcceptedParticles / m.ElapsedSeconds);
            lastAttemptsPerLaunch = m.AttemptsPerLaunch;
            lastTotalLaunches = m.TotalLaunches;
            lastTotalAttempts = m.TotalAttempts;
        }

        return new Measured(
            Statistics.Mean(samples), Statistics.StandardDeviation(samples), repeats, warmupIncludedAnyRepeat,
            lastAttemptsPerLaunch, lastTotalLaunches, lastTotalAttempts);
    }

    /// <summary>Pins <paramref name="child"/> to the highest-numbered logical CPU this process
    /// itself may run on (CPU 0 is the usual target of interrupts). Returns <see langword="false"/>
    /// and kills the child when the affinity cannot be set (or the platform has none), so a row labelled <c>host1</c> is never
    /// recorded from a child that runs on more than one CPU. The pin lands a few milliseconds
    /// after the child starts; the cycle window is timed inside the child, long after.</summary>
    private static bool TryPinToOneLogicalCpu(Process child)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            child.Kill();
            return false;
        }

        try
        {
            using var self = Process.GetCurrentProcess();
            var allowed = (ulong)self.ProcessorAffinity;
            var highestBit = 63 - System.Numerics.BitOperations.LeadingZeroCount(allowed);
            child.ProcessorAffinity = (nint)(1UL << highestBit);
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception
            or PlatformNotSupportedException)
        {
            child.Kill();
            return false;
        }
    }
}
