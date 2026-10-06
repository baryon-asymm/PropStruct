using ILGPU;
using ILGPU.Runtime;
using PropStruct.Input;
using PropStruct.Statistics;
using PropStruct.Tests.Harness;

namespace PropStruct.Particle.Tests;

/// <summary>
/// The setup of a real reference formulation, completed through the two-phase
/// hand-off <c>Statistics.Setup.Prepare</c>/<c>Particle.SizeLaw.Sample</c>/
/// <c>Statistics.Setup.CompleteEchoes</c> describes (<c>src/Statistics/API.md</c>,
/// "Setup hand-off"; <c>src/Simulation/API.md</c>'s own section of the same name).
/// This node builds the <see cref="ArrayView{T}"/>s itself, over its own CPU
/// accelerator, exactly as <c>Simulation</c> would, since <c>Statistics</c> never
/// builds one (<c>Statistics/API.md</c>, same section). No formula of <c>Statistics</c>
/// or <c>Particle</c> is repeated here: every value comes from calling the published
/// members, never from a second implementation (root BOOT.md, Taboos).
/// </summary>
internal static class ReferenceFormulation
{
    /// <summary>The five reference formulations named in root BOOT.md's own statistical criterion.</summary>
    public static readonly string[] Names = { "HPEPA3", "inpt", "P33", "PSAN02n", "HMX" };

    /// <summary>
    /// Generous enough that no attempt of the first 1000 of cycle 0 of any reference
    /// formulation can exhaust either budget (the original itself has none and would
    /// loop, <c>src/Particle/BOOT.md</c>, "## Constraints"): a budget genuinely
    /// exhausted here would be a finding about the model, not an artifact of this
    /// node's own choice of ceiling.
    /// </summary>
    public const int NeighbourBudget = 1_000_000;

    public const int PocketRedrawBudget = 1_000_000;

    /// <summary>
    /// Reads formulation <paramref name="name"/>'s own <c>.dat</c> and completes its
    /// setup with the original's own defaults (<c>ModelParameters.Default</c>): the
    /// stdin every reference output was generated with is the formulation's name
    /// followed by one confirmation line (`tests/Fixtures/provenance.json`), and the
    /// menu echoes `results.m` itself prints (Dmin/Di/Dj = 10 mkm, Nnmin/max = 3/100,
    /// k5 = 0.25, eps = 5e-2, variant = 0, k7 = 8.2, k8 = 7.73, Zok* = 0 for every one
    /// of the five) are the original's own defaults, not a formulation-specific menu
    /// answer.
    /// </summary>
    public static ModelSetup Prepare(Accelerator accelerator, string name, out SetupTables tables, out SetupEchoes echoes)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat");
        var parameters = ModelParameters.Default;
        var formulation = DatFile.Read(path, parameters.ReadPocketFormingFractions);

        var status = Setup.Prepare(
            formulation, parameters, PrecisionKind.Binary64, NeighbourBudget, PocketRedrawBudget,
            out var setup, out tables, out var draw, out var pending);

        if (status != SetupStatus.Ok)
        {
            throw new InvalidOperationException($"{name}: Setup.Prepare returned {status}.");
        }

        using var boundsBuffer = accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = accelerator.Allocate1D(tables.Cumulative);

        SizeLaw.Sample(
            setup.SizeLaw, setup.FractionCount, boundsBuffer.View.BaseView, cumulativeBuffer.View.BaseView,
            draw.X, draw.X1, out var dmax, out _);

        echoes = Setup.CompleteEchoes(ref setup, pending, dmax);
        return setup;
    }
}
