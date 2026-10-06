using PropStruct.Tests.Harness;

namespace PropStruct.DispersionTool;

/// <summary>
/// The node's one entry point (API.md, "## Command line"): writes
/// <c>tests/Fixtures/dispersion.approved.txt</c> from <see cref="DispersionTable.Compute"/> (root BOOT.md, "the
/// pass condition compares failure rates, not single runs"; <c>tests/Fixtures/API.md</c>, "## Dispersion table").
///
/// Used to be <c>tests/Harness.Tests/DispersionApprovedTests.Regenerate</c>, a
/// <c>[Fact(Skip = "manual regeneration only")]</c> test — a permanently skipped test, which AGENTS.md §13
/// forbids ("a perpetually red check is worse than an absent one"). A generator is not a check, so it moved
/// here, run by hand, exactly like <c>tests/Benchmarks</c> and <c>tests/RateTableTool</c> (BOOT.md, Constraints).
/// </summary>
internal static class Program
{
    public static int Main()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "dispersion.approved.txt");
        File.WriteAllLines(path, DispersionTable.Compute());
        Console.WriteLine($"wrote {path}");
        return 0;
    }
}
