using PropStruct.Input;
using PropStruct.Tests.Harness;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// A real reference formulation (HPEPA3), trimmed to a small particle count and cycle count so the L0/L1
/// rows of BOOT.md run the full <see cref="Simulator"/> pipeline (a real accelerator, a real
/// <c>Statistics.Setup.Prepare</c>) in a few seconds rather than reading L2's own full-size fixture
/// (BOOT.md, "Invariants": "Runs over whole reference formulations are marked Category=Long"). Trimming
/// only <c>ParticlesPerCycle</c> and <c>Cycles</c> keeps every precondition <c>Setup.Prepare</c> checks
/// satisfied, since the fractions, densities and coefficients are the real formulation's own.
/// </summary>
internal static class SmallFormulation
{
    public static Formulation Build(int particlesPerCycle = 40, int cycles = 2)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "HPEPA3.dat");
        var formulation = DatFile.Read(path);
        return formulation with { ParticlesPerCycle = particlesPerCycle, Cycles = cycles };
    }
}
