using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace PropStruct.Tests.Harness.Tests;

public partial class StudentDistributionTests
{
    private sealed record Case(
        [property: JsonPropertyName("df")] int Df,
        [property: JsonPropertyName("twoSidedAlpha")] double TwoSidedAlpha,
        [property: JsonPropertyName("quantile")] double Quantile);

    private sealed record CaseFile(
        [property: JsonPropertyName("generatedWith")] string GeneratedWith,
        [property: JsonPropertyName("cases")] List<Case> Cases);

    // Source-generated: a plain JsonSerializer.Deserialize<CaseFile> call gives CA1812 no evidence that `Case`
    // and `CaseFile` are ever constructed (reflection-based deserialization is invisible to it), and neither may
    // be removed — they are `student_t.json`'s own shape.
    [JsonSerializable(typeof(CaseFile))]
    private sealed partial class CaseFileJsonContext : JsonSerializerContext;

    public static IEnumerable<object[]> ScipyCases()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "harness", "student_t.json");
        var file = JsonSerializer.Deserialize(File.ReadAllText(path), CaseFileJsonContext.Default.CaseFile)!;
        Assert.StartsWith("scipy 1.18.1", file.GeneratedWith, StringComparison.Ordinal);
        foreach (var testCase in file.Cases)
        {
            yield return [testCase.Df, testCase.TwoSidedAlpha, testCase.Quantile];
        }
    }

    [Theory]
    [MemberData(nameof(ScipyCases))]
    public void MatchesScipy(int df, double twoSidedAlpha, double expectedQuantile)
    {
        var actual = StudentDistribution.TwoSidedQuantile(df, twoSidedAlpha);

        // The two-sided quantile of a heavy-tailed distribution spans orders of magnitude across the case
        // table (df = 1, alpha = 1e-8 gives t ~ 6.4e7); a relative tolerance is the meaningful comparison, with
        // an absolute floor for the near-1 quantiles.
        var tolerance = Math.Max(1e-6, Math.Abs(expectedQuantile) * 1e-7);
        Assert.True(
            Math.Abs(actual - expectedQuantile) <= tolerance,
            $"df={df}, twoSidedAlpha={twoSidedAlpha}: expected {expectedQuantile}, got {actual} (tolerance {tolerance}).");
    }
}
