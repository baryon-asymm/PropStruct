using PropStruct.RateTableTool;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The change detector of the <c>Corrected</c> model (<c>BOOT.md</c>, "## The Corrected-model snapshot"): each case of
/// <see cref="CorrectedModelGuard.Cases"/> runs once on the CPU under <see cref="ModelKind.Corrected"/> with no seam, and
/// the hashes of its <c>results.m</c> and of its whole result as JSON are compared exactly against
/// <c>Snapshots/CorrectedModelSnapshot.approved.txt</c>. It asserts that the <c>Corrected</c> results have not moved
/// since the file was approved, not that they are right: the <c>Corrected</c> model has no external reference. The file
/// is generated, never typed (<c>dotnet run --project tests/RateTableTool -- corrected-model-snapshot</c>). The cases
/// split by <see cref="GuardCase.IsLong"/> as the Original guard's do.
/// </summary>
public class CorrectedModelGuardTests(ITestOutputHelper output)
{
    private const string RegenerateCommand = "dotnet run --project tests/RateTableTool -- corrected-model-snapshot";

    public static IEnumerable<object[]> FastCaseIds() =>
        CorrectedModelGuard.Cases.Where(c => !c.IsLong).Select(c => new object[] { c.Id });

    public static IEnumerable<object[]> LongCaseIds() =>
        CorrectedModelGuard.Cases.Where(c => c.IsLong).Select(c => new object[] { c.Id });

    [Fact]
    public void ApprovedFileHoldsExactlyTheCasesOfTheSnapshot()
    {
        var approved = CorrectedModelGuard.LoadApproved().Keys.Order(StringComparer.Ordinal);
        var cases = CorrectedModelGuard.Cases.Select(c => c.Id).Order(StringComparer.Ordinal);

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
        var guardCase = CorrectedModelGuard.Find(id);
        var run = ReferenceModeRunner.Fingerprint(guardCase.ReadFormulation(), CorrectedModelGuard.Options(guardCase));
        output.WriteLine(CorrectedModelGuard.Line(guardCase, run));

        GuardHashAssertions.AssertMatchesApproved(
            id, run, CorrectedModelGuard.LoadApproved(), CorrectedModelGuard.ApprovedPath, RegenerateCommand);
    }
}
