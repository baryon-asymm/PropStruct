using Xunit;

namespace PropStruct.Input.Tests;

/// <summary>
/// L0 (tests/Input.Tests/BOOT.md): the default of every one of the fourteen menu parameters, against
/// <c>tests/Fixtures/cases/input/model-parameters-default.json</c> (BOOT.md, "Defaults of the menu",
/// source lines 66-80).
/// </summary>
public sealed class ModelParametersTests
{
    [Fact]
    public void DefaultMatchesTheConstructedCase()
    {
        var expected = CaseFile.ReadModelParametersDefault();
        var actual = ModelParameters.Default;

        Assert.Equal(expected.DminMetres, actual.Dmin.Metres);
        Assert.Equal(expected.CellSizeMetres, actual.CellSize.Metres);
        Assert.Equal(expected.CategoryStepMetres, actual.CategoryStep.Metres);
        Assert.Equal(expected.EpsDok, actual.EpsDok);
        Assert.Equal(expected.Alpha, actual.Alpha);
        Assert.Equal(expected.NnMin, actual.NnMin);
        Assert.Equal(expected.PocketCoefficient, actual.PocketCoefficient);
        Assert.Equal(expected.BridgeCoefficient, actual.BridgeCoefficient);
        Assert.Equal(expected.TailProbability, actual.TailProbability);
        Assert.Equal(expected.NnMax, actual.NnMax);
        Assert.Equal(expected.HomogenizedOxidizerFraction, actual.HomogenizedOxidizerFraction);
        Assert.Equal(expected.ReadPocketFormingFractions, actual.ReadPocketFormingFractions);
        Assert.Equal(expected.Variant, actual.Variant);
        Assert.Equal(expected.AggregatedOxideFraction, actual.AggregatedOxideFraction);
    }
}
