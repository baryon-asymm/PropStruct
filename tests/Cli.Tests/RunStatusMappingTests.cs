using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0/L1: <c>Program.BudgetFlagOf</c> is a typed table over a neighbour's enum (<see cref="RunStatus"/>,
/// <c>src/Simulation/API.md</c>); this is the reflected check that keeps it from drifting silently when
/// the library adds a new failure status (reviewed 2026-09-20). <see cref="RunStatus.Ok"/>,
/// <see cref="RunStatus.AcceleratorUnavailable"/> and <see cref="RunStatus.InvalidSetup"/> are excluded:
/// <c>Program.ReportFailedRun</c> handles those three itself, before it ever consults the table.
/// </summary>
public class RunStatusMappingTests
{
    private static readonly RunStatus[] HandledBeforeTheTable =
    {
        RunStatus.Ok, RunStatus.AcceleratorUnavailable, RunStatus.InvalidSetup,
    };

    [Fact]
    public void EveryOtherRunStatusHasABudgetFlagTableEntry()
    {
        var missing = Enum.GetValues<RunStatus>()
            .Except(HandledBeforeTheTable)
            .Where(status => !Program.BudgetFlagOf.ContainsKey(status))
            .ToList();

        Assert.True(missing.Count == 0, $"RunStatus values with no Program.BudgetFlagOf entry: {string.Join(", ", missing)}");
    }
}
