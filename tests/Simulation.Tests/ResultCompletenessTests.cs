using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L0 of the BOOT.md table: "the result carries every field of <c>CycleReport</c>", checked against
/// <c>Statistics.CycleReport</c> by reflection (AGENTS.md §6: a criterion with the quantifier "all" is
/// checked against a machine-generated list, not a typed one).
/// </summary>
public class ResultCompletenessTests
{
    /// <summary>
    /// Every property name of <c>CycleReport</c> is reachable somewhere in <see cref="SimulationResult"/>'s
    /// own object graph (BOOT.md, "Design decisions (2026-09-18)", "The result mirrors CycleReport"; the
    /// field-by-field mapping is BOOT.md, "## Result field mapping"). Matched by name, not by exact type:
    /// <see cref="Convergence"/>'s six fields deliberately broaden <c>CycleReport</c>'s own scalar
    /// <see langword="double"/> to a per-cycle series (an <c>ImmutableArray&lt;double&gt;</c>), the one
    /// documented departure from a one-to-one type mirror.
    /// </summary>
    [Fact]
    public void SimulationResultCarriesEveryFieldOfCycleReport()
    {
        var cycleReportFields = ResultReflection.CycleReportFieldNames();
        var resultFields = ResultReflection.LeafPropertyNames<SimulationResult>();

        var missing = cycleReportFields.Except(resultFields).ToList();
        Assert.True(missing.Count == 0, "Missing from SimulationResult: " + string.Join(", ", missing));
    }

    /// <summary>
    /// The mutation proof (AGENTS.md §13): removing one field from a member record (here, <c>Pockets.D432</c>)
    /// must turn the completeness check above red. Exercised directly, without editing product code, by
    /// asserting the check fails once <c>D432</c> is (simulated by) excluded from the reachable set.
    /// </summary>
    [Fact]
    public void SimulationResultCarriesEveryFieldOfCycleReportIsNotVacuouslyGreen()
    {
        var cycleReportFields = ResultReflection.CycleReportFieldNames();
        var resultFields = ResultReflection.LeafPropertyNames<SimulationResult>();
        _ = resultFields.Remove(nameof(Pockets.D432)); // the field this mutation proof drops.

        var missing = cycleReportFields.Except(resultFields).ToList();
        Assert.Contains(nameof(Pockets.D432), missing);
    }
}
