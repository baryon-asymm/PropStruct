using PropStruct.RateTableTool;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The guard that keeps the <c>Original</c> model bit for bit what it is (BOOT.md, "## The Original-model
/// guard"). Each case of <see cref="OriginalModelGuard.Cases"/> runs once on the CPU, and two hashes are
/// compared exactly against <c>Snapshots/OriginalModelGuard.approved.txt</c>: that of its <c>results.m</c>
/// (print resolution of four to eight digits) and that of the whole <see cref="SimulationResult"/> as JSON
/// with every double round-tripped, which a change below the print resolution moves. Both come from
/// <see cref="ReferenceModeRunner.Fingerprint"/>, the runner that wrote the file; the approved file is
/// generated, never typed (<c>dotnet run --project tests/RateTableTool -- original-model-guard</c>).
/// </summary>
public class OriginalModelGuardTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> CaseIds() =>
        OriginalModelGuard.Cases.Select(c => new object[] { c.Id });

    [Fact]
    public void ApprovedFileHoldsExactlyTheCasesOfTheGuard()
    {
        var approved = OriginalModelGuard.LoadApproved().Keys.Order(StringComparer.Ordinal);
        var cases = OriginalModelGuard.Cases.Select(c => c.Id).Order(StringComparer.Ordinal);

        Assert.Equal(cases, approved);
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    [Trait("Category", "Long")]
    public void ResultAndResultsMMatchApprovedHashes(string id)
    {
        var guardCase = OriginalModelGuard.Find(id);
        var run = ReferenceModeRunner.Fingerprint(guardCase.ReadFormulation(), guardCase.Options());
        output.WriteLine(OriginalModelGuard.Line(guardCase, run));

        var regenerate = "regenerate with 'dotnet run --project tests/RateTableTool -- original-model-guard' " +
            "in the same commit as the reason for the change, only if the change of the Original model is intended.";
        Assert.True(
            OriginalModelGuard.LoadApproved().TryGetValue(id, out var approved),
            $"no approved hashes for '{id}' in {OriginalModelGuard.ApprovedPath}; {regenerate}");
        Assert.True(
            string.Equals(approved.Json, run.JsonSha256Hex, StringComparison.Ordinal),
            $"{id}: the whole result (JSON, Elapsed and Accelerator normalised) no longer matches " +
            $"{OriginalModelGuard.ApprovedPath}. expected sha256={approved.Json}, got sha256={run.JsonSha256Hex}; {regenerate}");
        Assert.True(
            string.Equals(approved.ResultsM, run.Sha256Hex, StringComparison.Ordinal),
            $"{id}: results.m (time line aside) no longer matches {OriginalModelGuard.ApprovedPath}. " +
            $"expected sha256={approved.ResultsM}, got sha256={run.Sha256Hex}; {regenerate}");
    }
}
