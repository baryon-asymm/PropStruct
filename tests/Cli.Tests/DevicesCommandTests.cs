using System.Diagnostics;
using PropStruct.Execution;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L1 of the BOOT.md table: "`devices` lists what <see cref="AcceleratorProbe.Discover"/> reports, and is
/// green on a machine without CUDA (<c>PROPSTRUCT_NO_CUDA=1</c>)".
/// </summary>
public class DevicesCommandTests
{
    private static readonly string[] DevicesArgs = ["devices"];

    [Fact]
    public void DevicesPrintsOneLinePerAcceleratorTheProbeReports()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = Program.Run(DevicesArgs, stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
        Assert.Empty(stderr.ToString());

        var expected = AcceleratorProbe.Discover();
        var lines = stdout.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(expected.Count, lines.Length);

        foreach (var accelerator in expected)
        {
            Assert.Contains(lines, l => l.Contains(accelerator.Kind.ToString(), StringComparison.Ordinal)
                && l.Contains(accelerator.Name, StringComparison.Ordinal));
        }
    }

    // BOOT.md's own table: "green on a machine without CUDA (PROPSTRUCT_NO_CUDA=1)". The kill switch is
    // read by AcceleratorProbe.Discover itself (Execution/API.md); this only confirms `devices` still
    // succeeds and still agrees with the probe when it is set, without asserting CUDA is actually absent
    // on the machine running the test (root BOOT.md: "the CPU path needs no NVIDIA software").
    //
    // Reviewed 2026-09-20: this used to mutate PROPSTRUCT_NO_CUDA on the current (test) process.
    // src/Execution/API.md records exactly that mistake against its own tests (⚠ 2026-09-18): xunit runs
    // test classes in parallel by default, so the mutation would have applied, for its whole scope, to
    // every other test's engines too. A child process keeps it to that one process; the "expected" side
    // uses AcceleratorProbe.Discover's own environment-injection seam instead, in this test process.
    [Fact]
    public async Task DevicesWithTheCudaKillSwitchSetOnAChildProcessStillAgreesWithTheProbe()
    {
        var startInfo = new ProcessStartInfo(PropstructExecutable.Path)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("devices");
        startInfo.Environment["PROPSTRUCT_NO_CUDA"] = "1";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start propstruct.");
        await process.StandardInput.DisposeAsync();
        var readOutputTask = process.StandardOutput.ReadToEndAsync();
        var readErrorTask = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("propstruct devices did not finish within 15 s.");
        }

        // ConfigureAwait(true), not (false): xUnit1030 forbids (false) in a test method (it may bypass xUnit's
        // own parallelization limits), and CA2007 is satisfied by any explicit ConfigureAwait call.
        var stdout = await readOutputTask.ConfigureAwait(true);
        var stderr = await readErrorTask.ConfigureAwait(true);

        Assert.Equal(0, process.ExitCode);
        Assert.Empty(stderr);

        var expected = AcceleratorProbe.Discover(name => name == "PROPSTRUCT_NO_CUDA" ? "1" : null);
        Assert.DoesNotContain(expected, a => a.Kind == AcceleratorKind.Cuda);
        Assert.Equal(DevicesReport.Render(expected), stdout);
    }
}
