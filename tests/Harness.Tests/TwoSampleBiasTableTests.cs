using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Root ACCEPTANCE.md's acceptance criterion: "the bias of the Original layout against the Independent layout
/// is computed per printed quantity for every reference formulation, and the claims of 'Known bias of the
/// original's seeds' are checked by it." <see cref="StatisticalCriterion.TwoSampleBias"/> is already public and is
/// already the machinery <c>TwoSampleBiasTests</c> uses for a handful of hand-picked quantities (root ACCEPTANCE.md's
/// five-row table and a few extras <c>tests/Harness/BOOT.md</c> documents); this file is the first thing that
/// reads its output for every printed quantity, not a hand-picked subset, and holds the result against root
/// BOOT.md's own claims -- no new statistic, no new level, the same Welch two-sample test at the same family-wise
/// alpha <see cref="StatisticalCriterion.TwoSampleBias"/> already computes.
///
/// This file does not edit <c>StatisticalCriterion.cs</c>, <c>tests/Harness/BOOT.md</c> or <c>HISTORY.md</c> (a
/// second coder holds all three for the calibration work); the full per-formulation numbers are reported in this
/// commit's own message for the coordinator to fold into the node's own truth table.
/// </summary>
public class TwoSampleBiasTableTests
{
    private readonly ITestOutputHelper _output;

    public TwoSampleBiasTableTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> FormulationNames() => ResultsMFileTests.FormulationNames();

    // Root ACCEPTANCE.md, criterion "Known bias of the original's seeds": the five rows of its table, mapped to the printed
    // quantity name and index TwoSampleBias reports them under. `Dkarm43(1)` is not `Dkarm43` at array index 0:
    // it is a comment-line echo (this node's own name scheme, "a comment-line assignment ... keeps its literal
    // label text as the name ... because the label is not a bare identifier"), so the parenthesised "(1)" is part
    // of the literal quantity name itself, at that name's own (and only) index 0 -- exactly the name
    // `TwoSampleBiasTests.MassMeanPocketSizeAndPocketMassFractionMatchTheMeasuredTruthTable` already looks up.
    // `Dok43all` (checked first, below, against a real array quantity, confirmed present at index 0 in every
    // formulation) is genuinely indexed; `Zkarm`, `Dkarm10` and `MediumNumberOfBridges` ("bridges per particle",
    // "Medium number of bridges:" -> `MediumNumberOfBridges`) are scalars, index 0 their only index.
    private static readonly (string Label, string Quantity, int Index)[] RootTableRows =
    [
        ("mass-mean pocket size", "Dkarm43(1)", 0),
        ("pocket mass fraction", "Zkarm", 0),
        ("bridges per particle", "MediumNumberOfBridges", 0),
        ("number-mean pocket size", "Dkarm10", 0),
        ("mass-mean oxidizer size", "Dok43all", 0),
    ];

    // Root ACCEPTANCE.md's table, percent difference of means (lagged against independent), exactly as printed
    // there today.
    private static readonly Dictionary<(string Quantity, string Formulation), double> RootTablePercent =
        new()
        {
            [("Dkarm43(1)", "HPEPA3")] = 4.9,
            [("Dkarm43(1)", "inpt")] = -5.8,
            [("Dkarm43(1)", "P33")] = 6.1,
            [("Dkarm43(1)", "PSAN02n")] = -10.9,
            [("Dkarm43(1)", "HMX")] = -5.3,

            [("Zkarm", "HPEPA3")] = 1.6,
            [("Zkarm", "inpt")] = 3.1,
            [("Zkarm", "P33")] = 3.1,
            [("Zkarm", "PSAN02n")] = 17.4,
            [("Zkarm", "HMX")] = 3.1,

            [("MediumNumberOfBridges", "HPEPA3")] = -8.9,
            [("MediumNumberOfBridges", "inpt")] = 3.2,
            [("MediumNumberOfBridges", "P33")] = -9.2,
            [("MediumNumberOfBridges", "PSAN02n")] = 1.1,
            [("MediumNumberOfBridges", "HMX")] = 0.7,

            [("Dkarm10", "HPEPA3")] = -4.2,
            [("Dkarm10", "inpt")] = -3.6,
            [("Dkarm10", "P33")] = -4.9,
            [("Dkarm10", "PSAN02n")] = -6.7,
            [("Dkarm10", "HMX")] = 7.8,

            [("Dok43all", "HPEPA3")] = -0.05,
            [("Dok43all", "inpt")] = 0.00,
            [("Dok43all", "P33")] = 0.04,
            [("Dok43all", "PSAN02n")] = 0.00,
            [("Dok43all", "HMX")] = -0.02,
        };

    // Already documented as biased somewhere in the tree even though not one of root's own five named rows --
    // tests/Harness/BOOT.md's own "Two-sample bias" table (via TwoSampleBiasTests, AlwaysBiasedQuantities and
    // RareEventBiasedQuantitiesByFormulation) already names these. Kept only to separate "genuinely unknown to
    // the tree until this task" from "known elsewhere, just not in root's own five rows" in the report below --
    // it does not change what counts as "not mentioned by root": the root names only the five rows of its ACCEPTANCE.md table
    // and the general prose "the pocket and bridge quantities are biased" / "only the oxidizer size distribution
    // ... is unaffected", neither of which names a specific quantity outside the table.
    private static readonly string[] AlreadyDocumentedElsewhereInTree =
        ["Nkarm", "Dqmkm1", "Dqmkm2", "MediumNumberOfBridges", "NFX", "MediumJammedParticleFraction"];

    private static bool IsAlreadyDocumentedElsewhere(string quantity) =>
        AlreadyDocumentedElsewhereInTree.Contains(quantity, StringComparer.Ordinal)
        || quantity.StartsWith("ConditionBreaking(", StringComparison.Ordinal);

    private static double PercentDifference(double first, double second) =>
        second != 0.0 ? (first - second) / second * 100.0 : double.NaN;

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(FormulationNames))]
    public void FullTwoSampleBiasTableLaggedAgainstIndependent(string formulation)
    {
        var cells = StatisticalCriterion.TwoSampleBias(formulation, ReplicaKind.Lagged, ReplicaKind.Independent);

        // Item 1: the table, for every printed quantity -- every cell TwoSampleBias reports (name and index) is
        // read below, at its own Welch statistic and its own family-wise verdict, nothing recomputed or
        // reclassified. What is not useful is echoing every one of them individually: a histogram family such as
        // fqmkm1 differs, by the same few percent, at nearly every one of its several hundred indices, and a wall
        // of near-duplicate rows would not read as a table. Reported per distinct quantity NAME instead -- how
        // many of its own indices were compared, how many differ, and the spread of the percent difference over
        // the ones that do -- with root ACCEPTANCE.md's five rows (item 2) and every newly found name (item 3)
        // still checked at their own specific index below.
        var differing = cells.Where(c => c.Differs).ToList();

        _output.WriteLine(
            $"--- {formulation}: two-sample bias, lagged vs independent -- {cells.Count} compared cells, " +
            $"{differing.Count} differ at this criterion's own family-wise level ---");

        var byName = cells.GroupBy(c => c.Quantity, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (var group in byName)
        {
            var total = group.Count();
            var differsHere = group.Where(c => c.Differs).ToList();
            if (differsHere.Count == 0)
            {
                continue;
            }

            var percents = differsHere.Select(c => PercentDifference(c.MeanFirst, c.MeanSecond)).Where(double.IsFinite).OrderBy(p => p).ToList();
            var medianPercent = percents.Count > 0 ? Median(percents) : double.NaN;
            _output.WriteLine(
                $"{group.Key,-24} indices={total,4} differ={differsHere.Count,4} ({(double)differsHere.Count / total,6:P1}) " +
                $"percent(median)={medianPercent,8:G4}% percent(min)={(percents.Count > 0 ? percents[0] : double.NaN),8:G4}% " +
                $"percent(max)={(percents.Count > 0 ? percents[^1] : double.NaN),8:G4}%");
        }

        // Item 2: root ACCEPTANCE.md's five rows, checked against this table's own numbers, formulation by
        // formulation. "Agree" is read at the same precision root's own table is printed at (one decimal place,
        // occasionally two for the near-zero oxidizer row): within 0.5 percentage points of root's own figure.
        _output.WriteLine(string.Empty);
        _output.WriteLine($"--- {formulation}: root ACCEPTANCE.md's five named rows against this table ---");
        foreach (var (label, quantity, index) in RootTableRows)
        {
            var cell = cells.SingleOrDefault(c => c.Quantity == quantity && c.Index == index);
            if (cell is null)
            {
                _output.WriteLine($"{label} ({quantity}[{index}]): NOT FOUND among this formulation's compared cells.");
                continue;
            }

            var percent = PercentDifference(cell.MeanFirst, cell.MeanSecond);
            var rootPercent = RootTablePercent[(quantity, formulation)];
            var agrees = Math.Abs(percent - rootPercent) <= 0.5;
            _output.WriteLine(
                $"{label} ({quantity}[{index}]): root={rootPercent:G4}% measured={percent:G4}% " +
                $"(lagged={cell.MeanFirst:G6}, independent={cell.MeanSecond:G6}, t={cell.T:G4}, df={cell.DegreesOfFreedom}, " +
                $"differs={cell.Differs}) -> {(agrees ? "AGREE" : "DISAGREE")}");
        }

        // Item 3: quantities that differ significantly and that root ACCEPTANCE.md's five rows do not name at all.
        _output.WriteLine(string.Empty);
        var rootNames = RootTableRows.Select(r => r.Quantity).ToHashSet(StringComparer.Ordinal);
        var newlyBiasedNames = differing
            .Select(c => c.Quantity)
            .Where(name => !rootNames.Contains(name))
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        _output.WriteLine(
            $"--- {formulation}: {newlyBiasedNames.Count} quantities differ that root ACCEPTANCE.md's five rows do not name ---");
        foreach (var name in newlyBiasedNames)
        {
            _output.WriteLine(
                IsAlreadyDocumentedElsewhere(name)
                    ? $"{name}: differs, root does not name it [already documented in tests/Harness/BOOT.md]"
                    : $"{name}: differs, root does not name it [NOT PREVIOUSLY DOCUMENTED ANYWHERE IN THE TREE]");
        }

        Assert.True(cells.Count > 0, $"expected {formulation} to compare at least one quantity.");
    }

    private static double Median(List<double> sortedValues)
    {
        var mid = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0 ? (sortedValues[mid - 1] + sortedValues[mid]) / 2.0 : sortedValues[mid];
    }
}
