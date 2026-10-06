using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// `tests/Harness/HISTORY.md#fable-5-1-decision-v`, item 2(d): the generated dispersion table, checked by diff — the
/// same tripwire pattern <c>PublicSurface.approved.txt</c> and <c>docs/ORIGINAL-DEFECTS.md</c> already use
/// elsewhere in this tree (AGENTS.md §13). The table itself lives in <c>tests/Fixtures/dispersion.approved.txt</c>
/// (a neighbour node; the escalation to write there, and the "generated tripwire, not a stored tolerance"
/// reading of that node's own "Thresholds are not stored" invariant, is this node's BOOT.md's own recorded
/// decision, granted by the session that owns both nodes). This test recomputes the table fresh, from the same
/// production formula <c>Compare</c> itself calls (<see cref="CountReconstruction.ArrayTotals"/>,
/// <see cref="DispersionEstimator.Estimate"/> — never a second implementation of either), and fails
/// with the full recomputed table when it differs from the committed one: nothing in <c>Compare</c> ever reads
/// this file back, so a mismatch means the estimate moved, not that a stored tolerance needs editing.
///
/// ⚠ 2026-09-25: <c>ComputeTable</c>/<c>AppendLine</c> moved to <see cref="DispersionTable"/> (a neighbour in
/// this same node's own tree, <c>tests/Harness</c>), and the manual generator (<c>Regenerate</c>, a
/// <c>[Fact(Skip = "manual regeneration only")]</c> test — a permanently skipped test, which AGENTS.md §13
/// forbids) moved to the new node <c>tests/DispersionTool</c>, run by hand. This test is unaffected: it still
/// recomputes the table fresh and compares it to the committed file, now by calling the shared implementation
/// instead of holding a private copy of it.
/// </summary>
public class DispersionApprovedTests
{
    private static string ApprovedPath => RepositoryPaths.Resolve("tests", "Fixtures", "dispersion.approved.txt");

    [Trait("Category", "Long")]
    [Fact]
    public void MatchesTheCommittedTable()
    {
        var actual = DispersionTable.Compute();
        var expectedLines = File.Exists(ApprovedPath) ? File.ReadAllLines(ApprovedPath) : [];

        Assert.True(
            actual.SequenceEqual(expectedLines, StringComparer.Ordinal),
            "the recomputed dispersion table differs from tests/Fixtures/dispersion.approved.txt. " +
            "If the change is intended (a fixture updated, the pooling rule revised), regenerate the file with " +
            "'dotnet run --project tests/DispersionTool' and review the diff in the same commit as " +
            "tests/Harness/BOOT.md's own record of why. Recomputed table:\n" + string.Join('\n', actual));
    }
}
