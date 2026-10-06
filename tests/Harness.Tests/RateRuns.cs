namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Loads the sixteen stored runs of one (formulation, layout, precision) from
/// <c>tests/Fixtures/rate-runs/&lt;formulation&gt;/&lt;layout&gt;/&lt;precision&gt;/&lt;seedIndex&gt;.m.txt</c>
/// (<c>tests/Fixtures/API.md</c>, "## Rate table" (rate-runs); <c>tests/RateTableTool</c> writes them), in seed
/// order — <see cref="StatisticalCriterion.CompareSets"/>'s own candidates must arrive in seed order (this node's
/// BOOT.md, "## Set comparison").
/// </summary>
internal static class RateRuns
{
    private const int SeedCount = 16;

    internal static List<IReadOnlyDictionary<string, double[]>> Load(string formulation, string layout, string precision)
    {
        var directory = RepositoryPaths.Resolve("tests", "Fixtures", "rate-runs", formulation, layout, precision);
        var runs = new List<IReadOnlyDictionary<string, double[]>>(SeedCount);
        for (var seedIndex = 0; seedIndex < SeedCount; seedIndex++)
        {
            var path = Path.Combine(directory, $"{seedIndex}.m.txt");
            runs.Add(ResultsMFile.Parse(path));
        }

        return runs;
    }
}
