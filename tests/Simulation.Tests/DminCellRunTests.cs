using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.RateTableTool;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The Dmin cell index through the public path (<c>BOOT.md</c>, "## The Dmin cell index at run level"): the same
/// minimum size written in micrometres and in metres is the same parameter, so a <c>Binary64</c> run of P312,
/// reduced to a few particles as the guard's extras are, batched on the CPU, prints the same <c>results.m</c> and
/// holds the same result as JSON whichever unit the option was written in. The index <c>int(Dmin/Di)</c> of Fortran
/// line 1001 was the one place the unit moved the answer (<c>src/Statistics/BOOT.md</c>, "The cell index under
/// <c>Binary64</c>"): 10 µm against the default <c>Di</c> of 10e-6 m truncated to cell 0 instead of 1, 20 µm to 1
/// instead of 2. The result's JSON stores the metres of <c>Dmin</c> itself (<c>StoredSetup.Dmin</c>), which the two
/// spellings may write differently in the last bit, so that one number is set to zero before the JSON is hashed
/// and every other number of the result is compared.
/// </summary>
public class DminCellRunTests
{
    private const int Particles = 10;

    private static readonly Lazy<ReferenceModeRunner.Run> TenMicrometres = new(() => RunWithDmin(Length.FromMicrometres(10)));
    private static readonly Lazy<ReferenceModeRunner.Run> TenInMetres = new(() => RunWithDmin(Length.FromMetres(10e-6)));
    private static readonly Lazy<ReferenceModeRunner.Run> TwentyMicrometres = new(() => RunWithDmin(Length.FromMicrometres(20)));
    private static readonly Lazy<ReferenceModeRunner.Run> TwentyInMetres = new(() => RunWithDmin(Length.FromMetres(20e-6)));

    private static ReferenceModeRunner.Run RunWithDmin(Length dmin)
    {
        var parameters = ModelParameters.Default with { Dmin = dmin };
        var guardCase = new GuardCase(
            "dmin-cell-run", "P312", Cycles: null, Particles, ExecutionMode.Batched,
            StreamLayout.Independent, PrecisionKind.Binary64, parameters);

        return ReferenceModeRunner.Fingerprint(guardCase.ReadFormulation(), guardCase.Options(), WithoutStoredDmin);
    }

    /// <summary>The one number of the result that records the parameter itself: the stored metres of <c>Dmin</c>.</summary>
    private static SimulationResult WithoutStoredDmin(SimulationResult result) =>
        result with { StoredSetup = result.StoredSetup with { Dmin = 0.0 } };

    [Fact]
    public void TenMicrometresIsTheDefaultDminAtRunLevel()
    {
        var written = TenMicrometres.Value;
        var asDefault = TenInMetres.Value;

        Assert.Equal(asDefault.Sha256Hex, written.Sha256Hex);
        Assert.Equal(asDefault.JsonSha256Hex, written.JsonSha256Hex);
    }

    [Fact]
    public void TwentyMicrometresIsTwentyMicrometresWrittenInMetresAtRunLevel()
    {
        var written = TwentyMicrometres.Value;
        var inMetres = TwentyInMetres.Value;

        Assert.Equal(inMetres.Sha256Hex, written.Sha256Hex);
        Assert.Equal(inMetres.JsonSha256Hex, written.JsonSha256Hex);
    }

    /// <summary>
    /// The positive control, of the size of the effect: 20 µm and 10 µm are different minimum sizes and print
    /// different answers, so the two facts above are not equalities of a run the parameter never reaches.
    /// </summary>
    [Fact]
    public void ADifferentDminPrintsADifferentAnswer()
    {
        var twenty = TwentyMicrometres.Value;
        var ten = TenMicrometres.Value;

        Assert.NotEqual(ten.Sha256Hex, twenty.Sha256Hex);
        Assert.NotEqual(ten.JsonSha256Hex, twenty.JsonSha256Hex);
    }
}
