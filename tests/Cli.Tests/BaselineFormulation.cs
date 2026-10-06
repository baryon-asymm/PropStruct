using PropStruct.Input;
using PropStruct.Tests.Harness;

namespace PropStruct.Cli.Tests;

/// <summary>
/// The one formulation every fast (non-<c>Category=Long</c>) test in this node runs: <c>NMM=2, JZZ=1,
/// KXX=0 (normalizes to one cycle), N=100</c> - a couple hundred particles at most, well under the
/// "more than a few seconds" line tests/Cli.Tests/BOOT.md's own Constraints draw for <c>Category=Long</c>
/// (measured: batched CPU, well under a second). Not one of the five reference formulations: those are
/// reserved for the one test that actually needs a reference-mode run to compare against (L1's
/// byte-identical <c>results.m</c> row), which is <c>Category=Long</c>.
/// </summary>
internal static class BaselineFormulation
{
    public static string DatPath { get; } = RepositoryPaths.Resolve("tests", "Fixtures", "cases", "input", "baseline.dat");

    public static Formulation Load(bool readPocketFormingFractions = false) => DatFile.Read(DatPath, readPocketFormingFractions);
}
