using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// `tests/Harness/HISTORY.md#decision-xi-withdrawn-full-reasoning`, item 2: the print-resolution guard that
/// widens a Count-governed cell's own region `[low, high]` (`candidateResolution / quantum`,
/// `CellVerdict.ResolutionGuardCounts`) has never been looked at, and "an unexamined guard
/// is where the next level goes missing." This test reports its own magnitude per family, pooled over every
/// reference formulation's own reference file compared against its lagged replicas (the same population
/// <c>StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas</c> already
/// exercises) — read straight off <see cref="CellVerdict.ResolutionGuardCounts"/>, never
/// recomputed a second way here.
/// </summary>
public class RegionGuardDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public RegionGuardDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly System.Text.RegularExpressions.Regex FqDokKarmRow =
        new(@"^fqdokkarm\(\d+,:\)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string FamilyOf(string name) => FqDokKarmRow.IsMatch(name) ? "fqdokkarm" : name;

    [Trait("Category", "Long")]
    [Fact]
    public void Item2ResolutionGuardMagnitudeByFamily()
    {
        var byFamily = new Dictionary<string, List<double>>(StringComparer.Ordinal);

        foreach (var (formulation, _) in ResultsMFileTests.Formulations)
        {
            var reference = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt"));
            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, reference, ReplicaKind.Lagged, 0.05);

            foreach (var v in verdicts)
            {
                if (v.ResolutionGuardCounts is not { } guard)
                {
                    continue;
                }

                var family = FamilyOf(v.Name);
                if (!byFamily.TryGetValue(family, out var list))
                {
                    list = new List<double>();
                    byFamily[family] = list;
                }

                list.Add(guard);
            }
        }

        Assert.True(byFamily.Count > 0, "expected at least one family with a resolution guard.");

        _output.WriteLine($"{"family",-14} {"cells",7} {"min",14} {"median",14} {"max",14} {"nonzero",9}");
        foreach (var (family, values) in byFamily.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            values.Sort();
            var min = values[0];
            var max = values[^1];
            var median = values.Count % 2 == 0
                ? (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2.0
                : values[values.Count / 2];
            var nonzero = values.Count(v => v > 0.0);

            _output.WriteLine($"{family,-14} {values.Count,7} {min,14:G6} {median,14:G6} {max,14:G6} {nonzero,9}");
        }
    }
}
