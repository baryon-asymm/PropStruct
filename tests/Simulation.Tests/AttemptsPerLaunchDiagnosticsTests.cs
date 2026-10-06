using PropStruct.Execution;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// L0/L1 of the BOOT.md table: <see cref="RunDiagnostics.AttemptsPerLaunch"/> is the *effective* per-launch
/// attempt budget the run used (root BOOT.md, "## Budget selection rule (2026-09-28)"; this node's own
/// BOOT.md, "## Result field mapping"): 1 in reference mode, which mirrors <see cref="RunDiagnostics.BatchSize"/>'s
/// own reference-mode value, and <see cref="SimulationOptions.AttemptsPerLaunch"/> otherwise -- never the option
/// value unconditionally, which would misreport reference mode's own fixed budget of 1.
///
/// Proven both as an echo (the diagnostics value itself) and, per <c>PrecisionKindTests</c>'s own "weak vs
/// strong" distinction, as behaviour: a smaller budget forces more relaunches on a formulation whose particles
/// need more than one attempt on average (<see cref="SmallFormulation"/>/HPEPA3), so <see cref="RunDiagnostics.TotalLaunches"/>
/// at budget 1 must exceed the same at budget 1024 -- a comparison that cannot pass or fail vacuously only
/// because <see cref="RunDiagnostics.TotalAttempts"/> already exceeds the particle count (more than one
/// attempt was needed at all, on both runs).
/// </summary>
public class AttemptsPerLaunchDiagnosticsTests
{
    [Fact]
    public void DiagnosticsAttemptsPerLaunchIsTheEffectiveBudgetNotTheOptionUnconditionally()
    {
        var formulation = SmallFormulation.Build(particlesPerCycle: 40, cycles: 1);

        using var batchedBudget1 = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            AttemptsPerLaunch = 1,
        });
        var batchedBudget1Result = batchedBudget1.Run(formulation);

        using var batchedBudget1024 = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Batched,
            Accelerator = AcceleratorKind.Cpu,
            AttemptsPerLaunch = 1024,
        });
        var batchedBudget1024Result = batchedBudget1024.Run(formulation);

        // AttemptsPerLaunch is deliberately not 1 here: reference mode must still echo 1, proving the echo is
        // not merely "whatever SimulationOptions.AttemptsPerLaunch happened to hold" (the "report the option
        // unconditionally" mutation this test guards against, BOOT.md, "## Mutations").
        using var reference = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            AttemptsPerLaunch = 1024,
        });
        var referenceResult = reference.Run(formulation);

        Assert.Equal(1, batchedBudget1Result.Diagnostics.AttemptsPerLaunch);
        Assert.Equal(1024, batchedBudget1024Result.Diagnostics.AttemptsPerLaunch);
        Assert.Equal(1, referenceResult.Diagnostics.AttemptsPerLaunch);

        // Sanity guard against a vacuous launch-count comparison below: both batched runs must actually have
        // needed more than one attempt per particle (HPEPA3's own mean is 6.68, BOOT.md "## Budget measurement").
        var particleCount = (formulation.Cycles + 1) * formulation.ParticlesPerCycle;
        Assert.True(
            batchedBudget1Result.Diagnostics.TotalAttempts > particleCount,
            $"budget 1's own TotalAttempts ({batchedBudget1Result.Diagnostics.TotalAttempts}) must exceed the " +
            $"particle count ({particleCount}) for the launch-count comparison below to be non-vacuous.");
        Assert.True(
            batchedBudget1024Result.Diagnostics.TotalAttempts > particleCount,
            $"budget 1024's own TotalAttempts ({batchedBudget1024Result.Diagnostics.TotalAttempts}) must exceed " +
            $"the particle count ({particleCount}) for the launch-count comparison below to be non-vacuous.");

        // The behaviour, not just the echo: a smaller per-launch budget forces more relaunches per particle
        // that needs one, hence more launches overall.
        Assert.True(
            batchedBudget1Result.Diagnostics.TotalLaunches > batchedBudget1024Result.Diagnostics.TotalLaunches,
            $"budget 1 gave {batchedBudget1Result.Diagnostics.TotalLaunches} launches, budget 1024 gave " +
            $"{batchedBudget1024Result.Diagnostics.TotalLaunches}; a smaller budget must need more of them.");
    }
}
