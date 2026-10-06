using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Root BOOT.md, "Known bias of the original's seeds", ends with an absolute: "Only the oxidizer size
/// distribution, which the neighbour loop does not feed back into, is unaffected." The root's own five-row table
/// names a single oxidizer-size quantity, <c>Dok43all</c>, as the evidence for that sentence
/// (<see cref="TwoSampleBiasTests.MassMeanOxidizerSizeAgreesBetweenLayouts"/> tests exactly that one cell). This
/// file checks the word "only" itself (AGENTS.md §8: absolute words break most often) against the whole family the
/// original prints under "%___________________Dok parameters____________________" and "% &lt;Conditional
/// DOK&gt;" (<c>tests/Fixtures/references/*/results.m.txt</c>) — not a new statistic, the same
/// <see cref="StatisticalCriterion.TwoSampleBias"/> Welch test at the same family-wise level
/// <c>TwoSampleBiasTableTests</c> already computes over every printed quantity.
///
/// <b>What counts as the oxidizer size distribution</b> (read directly from every reference file's own comments,
/// not guessed):
/// <list type="bullet">
/// <item><description><b>Unambiguous, non-canonical-axis members</b> (<see cref="CoreOxidizerQuantityNames"/>):
/// <c>Dok43a</c> (analytical mass-medium diameter, all particles), <c>Dok43all</c> (all particles, 2 variants),
/// <c>Dok43(1)</c>/<c>Dok43(2)</c> (basic only / surrounding only), <c>Dok43sd</c> (standard deviation),
/// <c>Ddok_max</c> (maximum size of all particles), <c>epsalldok</c> and <c>epsdok(1)</c>/<c>epsdok(2)</c>
/// (modeling-accuracy diagnostics of the same distribution, printed in the same "Dok parameters" block),
/// <c>epsdokfr</c> (accuracy diagnostic of <c>fmdok</c>), <c>fineoxy_fr</c> (the fraction of oxidizer folded into
/// the homogenized binder rather than sized at all — the distribution's own missing mass), and <c>fmdok</c> itself
/// (the mass density distribution function of Dok particle sizes — the family's centerpiece).</description></item>
/// <item><description><b>Unambiguous, but a canonical-category-axis family</b> (this node's BOOT.md, "Canonical
/// category axis"; <c>tests/Harness/BOOT.md</c>'s own list of the four such families is exactly
/// <c>Dkarmcat</c>/<c>dokkarm43</c>/<c>dokkarm10</c>/<c>fqdokkarm(&lt;row&gt;,:)</c>): <c>dokkarm43</c> and
/// <c>dokkarm10</c> ("Dependency of mass-medium/medium Dok particles sizes on pockets categories") and every
/// <c>fqdokkarm(&lt;row&gt;,:)</c> ("Numeric density distribution functions of [Dok] particles on pockets
/// categories") are oxidizer-size quantities, indexed by pocket category rather than by their own size. A plain
/// per-index Welch test does not align that axis the way the criterion's own comparison does (this node's BOOT.md,
/// "Canonical category axis": two runs' row/column <c>k</c> are almost never the same physical bin once the
/// adaptive tail starts) — so <c>TwoSampleBias</c>'s cells for these three names are reported separately below,
/// marked, and never folded into the unambiguous verdict.</description></item>
/// <item><description><b>Ambiguous, reported under both readings</b>: <c>Dkarmcat</c> itself ("Pockets categories
/// sizes (mkm)") is the fourth name on that same canonical-axis list, but its values are pocket-size category
/// boundaries, not oxidizer sizes — it is the axis the three quantities above are indexed by, not itself a Dok
/// quantity. Reading A (excluded: an axis grid is not a "size distribution" value) and reading B (included: named
/// on the same "four families" list as the other three, and its own comment header sits inside "&lt;Conditional
/// DOK&gt;") are both reported.</description></item>
/// <item><description><b>Named "Dok" but excluded, with the reason recorded</b>: <c>Gdok</c> is an input echo (the
/// formulation's own input, not an output distribution); <c>MediumLijDdokCoefficient</c> ("Medium coef
/// [(Lij/Ddok)+1]") and <c>Dqmkm1</c> ("Medium MKM/Dok size between Dok particles") use a Dok length as a
/// normalizing unit for a bridge-geometry quantity, they are not themselves Dok sizes (and <c>Dqmkm1</c> is
/// already root BOOT.md's own named bridge quantity); <c>ConditionBreaking(1)</c>–<c>(4)</c> count how often a
/// condition on Dok/Dbase was violated, they are event frequencies, not sizes; <c>pdoksmall</c> ("Distribution of
/// P[pocket-in-pocket] on basicDok particles sizes") is indexed by Dok size bins but its own values are a pocket
/// probability, not a Dok size. None of these five names are tested here; they are recorded so a future reader
/// does not have to rediscover why a name containing "Dok" is absent from this file's list.</description></item>
/// </list>
///
/// This file does not edit <c>StatisticalCriterion.cs</c>, <c>tests/Harness/BOOT.md</c> or <c>HISTORY.md</c> (a
/// second coder holds all three for the calibration work); the full per-formulation figures are reported in this
/// commit's own message for the coordinator to weigh against root BOOT.md's "only" claim.
/// </summary>
public class OxidizerSizeDistributionBiasTests
{
    private readonly ITestOutputHelper _output;

    public OxidizerSizeDistributionBiasTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> FormulationNames() => ResultsMFileTests.FormulationNames();

    // Unambiguous, non-canonical-axis members of the oxidizer size distribution (see class comment). Each is
    // looked up as a whole quantity name (every index that name carries), never assumed present: a name absent
    // from a formulation's own compared cells is asserted, not silently read as "nothing to compare" (the task's
    // own warning about Dkarm43(1) vs. Dkarm43[0]).
    private static readonly string[] CoreOxidizerQuantityNames =
    [
        "Dok43a", "Dok43all", "Dok43(1)", "Dok43(2)", "Dok43sd", "Ddok_max",
        "epsalldok", "epsdok(1)", "epsdok(2)", "epsdokfr", "fineoxy_fr", "fmdok",
    ];

    // Unambiguous oxidizer-size quantities, but canonical-category-axis families (this node's BOOT.md, "Canonical
    // category axis"): a plain per-index Welch test does not align the axis the way the criterion's own comparison
    // does, so a "differs" verdict here is reported separately and marked, never mixed into the unambiguous count.
    private static readonly string[] CanonicalAxisOxidizerScalarNames = ["dokkarm43", "dokkarm10"];

    private static bool IsCanonicalAxisOxidizerRow(string quantity) =>
        quantity.StartsWith("fqdokkarm(", StringComparison.Ordinal);

    // The fourth name on tests/Harness/BOOT.md's own canonical-axis list. Its values are pocket-size category
    // boundaries, not oxidizer sizes -- ambiguous membership, reported under both readings (class comment).
    private const string AmbiguousCanonicalAxisName = "Dkarmcat";

    private static double PercentDifference(double first, double second) =>
        second != 0.0 ? (first - second) / second * 100.0 : double.NaN;

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(FormulationNames))]
    public void OxidizerSizeDistributionFamilyLaggedAgainstIndependent(string formulation)
    {
        var cells = StatisticalCriterion.TwoSampleBias(formulation, ReplicaKind.Lagged, ReplicaKind.Independent);
        var byName = cells.ToLookup(c => c.Quantity, StringComparer.Ordinal);

        _output.WriteLine($"=== {formulation}: oxidizer size distribution, lagged vs independent ===");

        // Part 1: the unambiguous, non-canonical-axis members. Every name must actually be among the compared
        // cells -- verified before anything is concluded from its presence or absence, per the task's own warning
        // that "not found" (an empty lookup group) is easy to misread as "nothing to compare".
        _output.WriteLine($"--- {formulation}: core oxidizer quantities (non-canonical-axis) ---");
        var anyCoreDiffers = false;
        foreach (var name in CoreOxidizerQuantityNames)
        {
            var group = byName[name].OrderBy(c => c.Index).ToList();
            Assert.True(group.Count > 0, $"{formulation}: '{name}' was not found among TwoSampleBias's compared cells -- verify the name against the reference file before reading this as \"nothing to compare\".");

            foreach (var cell in group)
            {
                var percent = PercentDifference(cell.MeanFirst, cell.MeanSecond);
                anyCoreDiffers |= cell.Differs;
                _output.WriteLine(
                    $"{name}[{cell.Index}]: lagged={cell.MeanFirst:G6} independent={cell.MeanSecond:G6} " +
                    $"percent={percent:G4}% t={cell.T:G4} df={cell.DegreesOfFreedom} differs={cell.Differs}");
            }
        }

        // Part 2: the canonical-axis oxidizer families (dokkarm43, dokkarm10, fqdokkarm(<row>,:)) -- reported
        // separately and marked, per the task instruction: a "differs" verdict here is not evidence either way,
        // because TwoSampleBias's plain per-index comparison does not align the axis the way the criterion's own
        // canonical-axis machinery does.
        _output.WriteLine(string.Empty);
        _output.WriteLine($"--- {formulation}: canonical-category-axis oxidizer families [MIS-ALIGNED AXIS -- NOT EVIDENCE EITHER WAY] ---");
        foreach (var name in CanonicalAxisOxidizerScalarNames)
        {
            var group = byName[name].OrderBy(c => c.Index).ToList();
            Assert.True(group.Count > 0, $"{formulation}: '{name}' was not found among TwoSampleBias's compared cells.");

            var differsHere = group.Count(c => c.Differs);
            _output.WriteLine($"{name}: indices={group.Count} differ={differsHere} [canonical axis, mis-aligned per-index comparison]");
        }

        var rowNames = cells.Select(c => c.Quantity).Where(IsCanonicalAxisOxidizerRow).Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.True(rowNames.Count > 0, $"{formulation}: no 'fqdokkarm(<row>,:)' cells were found among TwoSampleBias's compared cells.");

        var totalRowCells = 0;
        var totalRowDiffers = 0;
        foreach (var rowName in rowNames)
        {
            var group = byName[rowName].ToList();
            totalRowCells += group.Count;
            totalRowDiffers += group.Count(c => c.Differs);
        }

        _output.WriteLine(
            $"fqdokkarm(<row>,:): {rowNames.Count} rows, {totalRowCells} cells compared, {totalRowDiffers} differ " +
            "[canonical axis, mis-aligned per-index comparison]");

        // Part 3: the ambiguous name, reported under both readings rather than silently assigned to one.
        _output.WriteLine(string.Empty);
        _output.WriteLine($"--- {formulation}: ambiguous name '{AmbiguousCanonicalAxisName}' ---");
        var dkarmcatGroup = byName[AmbiguousCanonicalAxisName].OrderBy(c => c.Index).ToList();
        Assert.True(dkarmcatGroup.Count > 0, $"{formulation}: '{AmbiguousCanonicalAxisName}' was not found among TwoSampleBias's compared cells.");
        var dkarmcatDiffers = dkarmcatGroup.Count(c => c.Differs);
        _output.WriteLine(
            $"{AmbiguousCanonicalAxisName}: indices={dkarmcatGroup.Count} differ={dkarmcatDiffers} " +
            "-- reading A (excluded, an axis grid of pocket-size boundaries, not an oxidizer size) and " +
            "reading B (included, root's own canonical-axis list groups it with dokkarm43/dokkarm10/fqdokkarm) " +
            "are both reported; this cell class is also mis-aligned per-index (Part 2's own caveat applies to it too).");

        // The refutation this file exists to make: at least one unambiguous, non-canonical-axis oxidizer quantity
        // that differs beyond the family-wise threshold is enough to refute root BOOT.md's "only the oxidizer size
        // distribution ... is unaffected" -- the full per-formulation figures are in this commit's own message.
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"=== {formulation}: core oxidizer family verdict -- {(anyCoreDiffers ? "AT LEAST ONE CORE QUANTITY DIFFERS (refutes root's \"only\")" : "no core quantity differs")} ===");
    }
}
