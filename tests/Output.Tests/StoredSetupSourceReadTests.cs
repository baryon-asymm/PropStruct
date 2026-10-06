using System.Text.RegularExpressions;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// BOOT.md, "## Design decision (2026-09-24): print the stored setup": "A test fails if the
/// writer reads a generated member from <c>Formulation</c> or <c>ModelParameters</c>." Every
/// setup-plane value <see cref="ResultsMWriter"/> prints or scales by must come from
/// <see cref="Simulation.SimulationResult.StoredSetup"/>, which under
/// <see cref="Simulation.PrecisionKind.Original"/> already carries the run's own
/// binary32 store; reading the same value a second time, unrounded, from the formulation or
/// the parameters record would silently reintroduce the "eps echo" defect this design decision
/// fixed (`EpsHeaderEchoTests`' own history).
///
/// This is a source scan, not a reflection check: the forbidden members are exactly the
/// <c>Formulation</c>/<c>ModelParameters</c> properties this node's neighbour, `Input`
/// (`src/Input/API.md`), declares as the setup plane's own inputs — the same set
/// <c>src/Statistics/SetupPlane.generated.txt</c> names by its `input` rows (`plot1`, `plot2`,
/// `ggg0`, `gm`, the four `ak`, `gdok`/`ddok` via `Fractions`, `dmin`/`di`/`dj`, `eps_dok`,
/// `alpha`, the two structural coefficients, `nn_min`/`nn_max`, `gdokns`, `eta`) — every one
/// of them rounds under `Original` and is therefore stored, never re-read raw. The properties
/// left out of the forbidden set are the ones `SetupPlane.generated.txt` does not generate at
/// all: `Formulation.Name`/`SizeLawCode`/`Cycles`/`ParticlesPerCycle`/`GeneratorWarmup`/
/// `PocketFormingFractions` (`INTEGER`, unused, or a flag array) and
/// `ModelParameters.Variant`/`ReadPocketFormingFractions`/`TailProbability` (`INTEGER`, a
/// flag, or the one menu item that is `REAL*8`, not part of the setup plane at all).
///
/// Proven both ways (root taboo: "every check ... proven twice"): a reintroduced raw read of
/// <c>formulation.MetalMassFraction</c> was seen red here before this commit, and reverting it
/// is seen green again.
/// </summary>
public class StoredSetupSourceReadTests
{
    private static readonly string SourcePath =
        RepositoryPaths.Resolve("src", "Output", "ResultsMWriter.cs");

    // One alternative per generated Formulation/ModelParameters member named above; `\b` on both
    // sides so "formulation.Ak1" does not also match a hypothetical "formulation.Ak10".
    private static readonly Regex ForbiddenRead = new(
        @"\b(formulation|parameters)\.(" +
        "OxidizerDensity|PropellantDensity|OxidizerMassFraction|MetalMassFraction|" +
        "Ak1|Ak2|Ak3|Ak4|" +
        "Dmin|CellSize|CategoryStep|EpsDok|Alpha|NnMin|NnMax|" +
        "PocketCoefficient|BridgeCoefficient|HomogenizedOxidizerFraction|AggregatedOxideFraction" +
        @")\b",
        RegexOptions.Compiled);

    // Formulation.Fractions carries the raw, unrounded GDOK/DDOK pairs (Input/API.md,
    // "OxidizerFraction"); the rounded values are StoredSetup.FractionMassShares/FractionBounds.
    // Indexing or enumerating the array's elements reads them raw; only Fractions.Length (an
    // element count, not a setup-plane value) is legitimate, so that one member name is excluded
    // explicitly rather than blanket-forbidding "Fractions".
    private static readonly Regex ForbiddenFractionElementRead = new(
        @"\bformulation\.Fractions(?!\.Length\b)\S*[\[\.]",
        RegexOptions.Compiled);

    [Fact]
    public void ResultsMWriterNeverReadsAGeneratedSetupPlaneMemberFromFormulationOrParameters()
    {
        var source = File.ReadAllText(SourcePath);

        var directHits = ForbiddenRead.Matches(source).Select(m => m.Value).Distinct().ToList();
        var fractionHits = ForbiddenFractionElementRead.Matches(source).Select(m => m.Value).Distinct().ToList();

        var violations = directHits.Concat(fractionHits).ToList();

        Assert.True(violations.Count == 0,
            "ResultsMWriter.cs reads a generated setup-plane member directly from Formulation/ModelParameters " +
            $"instead of SimulationResult.StoredSetup: {string.Join(", ", violations)}.");
    }
}
