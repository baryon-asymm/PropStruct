using PropStruct.RateTableTool;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The comparison both change detectors make (<c>BOOT.md</c>, "## The Original-model guard" and "## The Corrected-model
/// snapshot"): the two hashes of a run, those of its <c>results.m</c> and those of its whole result as JSON, against the
/// line of the approved file. One implementation for <see cref="OriginalModelGuardTests"/> and
/// <see cref="CorrectedModelGuardTests"/>, so that the message and the order of the two assertions cannot drift apart.
/// </summary>
internal static class GuardHashAssertions
{
    /// <summary>
    /// Asserts that <paramref name="approved"/> holds <paramref name="id"/> and that both hashes of <paramref name="run"/>
    /// equal it; the failure names the file and the command that regenerates it, <paramref name="regenerateCommand"/>.
    /// </summary>
    public static void AssertMatchesApproved(
        string id,
        ReferenceModeRunner.Run run,
        IReadOnlyDictionary<string, (string ResultsM, string Json)> approved,
        string approvedPath,
        string regenerateCommand)
    {
        var regenerate = $"regenerate with '{regenerateCommand}' in the same commit as the reason for the change, " +
            "only if the change is intended.";
        Assert.True(
            approved.TryGetValue(id, out var line),
            $"no approved hashes for '{id}' in {approvedPath}; {regenerate}");
        Assert.True(
            string.Equals(line.Json, run.JsonSha256Hex, StringComparison.Ordinal),
            $"{id}: the whole result (JSON, Elapsed and Accelerator normalised) no longer matches " +
            $"{approvedPath}. expected sha256={line.Json}, got sha256={run.JsonSha256Hex}; {regenerate}");
        Assert.True(
            string.Equals(line.ResultsM, run.Sha256Hex, StringComparison.Ordinal),
            $"{id}: results.m (time line aside) no longer matches {approvedPath}. " +
            $"expected sha256={line.ResultsM}, got sha256={run.Sha256Hex}; {regenerate}");
    }
}
