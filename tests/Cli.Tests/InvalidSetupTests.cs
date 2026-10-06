using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L1 of the BOOT.md table: "the tool performs no semantic validation of its own: a value the library
/// refuses comes back as <c>InvalidSetup</c> and exits 2, shown by a test that passes an out-of-range
/// parameter". <c>--dmin</c> set far beyond the baseline formulation's largest fraction bound (315
/// micrometres) makes <c>Statistics.Setup.Prepare</c> refuse the setup
/// (<c>SimulationFailedException.Status == InvalidSetup</c>, message "Statistics.Setup.Prepare failed:
/// InvalidMinimumSize.") - found empirically, without reading <c>Statistics</c>' or <c>Simulation</c>'s
/// code (AGENTS.md §3: only their <c>API.md</c> is this node's to read), by trying candidate out-of-range
/// values against the real library one at a time. <c>Variant = 999</c>, <c>EpsDok = 0</c> and
/// <c>NnMin = -1</c> were tried first and complete normally (not useful here); <c>NnMax = 0</c> was tried
/// and appeared to kill the process, which was wrong: it accepted no particle and spent the whole
/// per-particle attempt cap, about four minutes, before failing with <c>AttemptCapExceeded</c>
/// (measured by the owning node, 2026-09-20; <c>src/Cli/BOOT.md</c> carries the correction). Since then
/// <c>Statistics</c> rejects that window in its setup, so the value would serve here as well; this test
/// keeps <c>Dmin = 1000</c>, which fails in milliseconds.
/// </summary>
public class InvalidSetupTests
{
    [Fact]
    public void DminFarBeyondTheFormulationsFractionBoundsExitsTwoAsInvalidSetup()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[] { "run", BaselineFormulation.DatPath, "--accelerator", "cpu", "--dmin", "1000", "--output", outputPath },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        Assert.Contains("invalid setup", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(outputPath));
    }

    /// <summary>
    /// `--precision original` with batched mode is refused by <c>Simulation</c> itself, reusing
    /// <c>RunStatus.InvalidSetup</c> rather than a new status (<c>src/Simulation/API.md</c>, "## Errors";
    /// decided with the root, root BOOT.md "Precision kind is an option of every run"). This node adds
    /// no second check: the existing <c>InvalidSetup</c> branch of <c>Program.ReportFailedRun</c> already
    /// echoes the library's own message verbatim, and that message is required to name both halves of the
    /// conflict — this test would fail if a future change dropped that requirement, either by the tool
    /// swallowing the message (falling back to a bare "invalid setup" with neither word present) or by
    /// `Simulation` no longer naming one of the two options in its own text.
    /// </summary>
    [Fact]
    public void PrecisionOriginalWithBatchedModeExitsTwoNamingBothHalves()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = Path.Combine(directory.Path, "results.m");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = Program.Run(
            new[]
            {
                "run", BaselineFormulation.DatPath, "--accelerator", "cpu",
                "--mode", "batched", "--precision", "original", "--output", outputPath,
            },
            stdout, stderr, CancellationToken.None);

        Assert.Equal(ExitCode.InvalidArguments, exitCode);
        var message = stderr.ToString();
        Assert.Contains("invalid setup", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Original", message, StringComparison.Ordinal);
        Assert.Contains("atch", message, StringComparison.Ordinal); // "batch"/"Batched", whichever case Simulation's own message uses.
        Assert.False(File.Exists(outputPath));
    }
}
