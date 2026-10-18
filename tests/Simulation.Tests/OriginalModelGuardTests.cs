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
/// generated, never typed (<c>dotnet run --project tests/RateTableTool -- original-model-guard</c>). The cases
/// split by <see cref="GuardCase.IsLong"/> into the fast set and <c>Category=Long</c>.
/// </summary>
public class OriginalModelGuardTests(ITestOutputHelper output)
{
    private const string RegenerateCommand = "dotnet run --project tests/RateTableTool -- original-model-guard";

    public static IEnumerable<object[]> FastCaseIds() =>
        OriginalModelGuard.Cases.Where(c => !c.IsLong).Select(c => new object[] { c.Id });

    public static IEnumerable<object[]> LongCaseIds() =>
        OriginalModelGuard.Cases.Where(c => c.IsLong).Select(c => new object[] { c.Id });

    [Fact]
    public void ApprovedFileHoldsExactlyTheCasesOfTheGuard()
    {
        var approved = OriginalModelGuard.LoadApproved().Keys.Order(StringComparer.Ordinal);
        var cases = OriginalModelGuard.Cases.Select(c => c.Id).Order(StringComparer.Ordinal);

        Assert.Equal(cases, approved);
    }

    [Theory]
    [MemberData(nameof(FastCaseIds))]
    public void ResultAndResultsMMatchApprovedHashes(string id) => AssertCaseMatchesApprovedHashes(id);

    [Theory]
    [MemberData(nameof(LongCaseIds))]
    [Trait("Category", "Long")]
    public void LongCaseResultAndResultsMMatchApprovedHashes(string id) => AssertCaseMatchesApprovedHashes(id);

    private void AssertCaseMatchesApprovedHashes(string id)
    {
        var guardCase = OriginalModelGuard.Find(id);
        var run = ReferenceModeRunner.Fingerprint(guardCase.ReadFormulation(), guardCase.Options());
        output.WriteLine(OriginalModelGuard.Line(guardCase, run));

        GuardHashAssertions.AssertMatchesApproved(
            id, run, OriginalModelGuard.LoadApproved(), OriginalModelGuard.ApprovedPath, RegenerateCommand);
    }
}
