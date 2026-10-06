using Xunit;

namespace PropStruct.Input.Tests;

/// <summary>
/// L0 (tests/Input.Tests/BOOT.md): list-directed reading rules and <see cref="Length"/>, against constructed
/// files and values in <c>tests/Fixtures/cases/input/</c> - record continuation, tabs and commas as
/// separators, the mixed-unit bounds line, a UTF-8 BOM label, and the <c>SFR</c> line.
/// </summary>
public sealed class PositiveCasesTests
{
    public static TheoryData<string> CaseNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in CaseFile.ReadPositiveCases().Keys)
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void ParsedFormulationMatchesTheConstructedCase(string caseName)
    {
        var expected = CaseFile.ReadPositiveCases()[caseName];
        var path = Path.Combine(CaseFile.Directory, caseName + ".dat");

        var formulation = DatFile.Read(path, expected.ReadPocketFormingFractions);

        Assert.Equal(caseName, formulation.Name);
        Assert.Equal(expected.OxidizerDensity, formulation.OxidizerDensity);
        Assert.Equal(expected.PropellantDensity, formulation.PropellantDensity);
        Assert.Equal(expected.OxidizerMassFraction, formulation.OxidizerMassFraction);
        Assert.Equal(expected.MetalMassFraction, formulation.MetalMassFraction);
        Assert.Equal(expected.Ak1, formulation.Ak1);
        Assert.Equal(expected.Ak2, formulation.Ak2);
        Assert.Equal(expected.Ak3, formulation.Ak3);
        Assert.Equal(expected.Ak4, formulation.Ak4);
        Assert.Equal(expected.SizeLawCode, formulation.SizeLawCode);
        Assert.Equal(expected.SizeLaw, formulation.SizeLaw);
        Assert.Equal(expected.Cycles, formulation.Cycles);
        Assert.Equal(expected.ParticlesPerCycle, formulation.ParticlesPerCycle);
        Assert.Equal(expected.GeneratorWarmup, formulation.GeneratorWarmup);

        Assert.Equal(expected.Fractions.Length, formulation.Fractions.Length);
        for (var i = 0; i < expected.Fractions.Length; i++)
        {
            Assert.Equal(expected.Fractions[i].MassShare, formulation.Fractions[i].MassShare);
            Assert.Equal(expected.Fractions[i].LowerBound, formulation.Fractions[i].LowerBound.AsWritten);
            Assert.Equal(expected.Fractions[i].UpperBound, formulation.Fractions[i].UpperBound.AsWritten);
        }

        if (expected.PocketFormingFractions is null)
        {
            Assert.Null(formulation.PocketFormingFractions);
        }
        else
        {
            _ = Assert.NotNull(formulation.PocketFormingFractions);
            Assert.Equal(expected.PocketFormingFractions, formulation.PocketFormingFractions!.Value);
        }
    }

    [Fact]
    public void TheGreaterOrEqualPointOneMicrometreRuleAppliesElementByElement()
    {
        var formulation = DatFile.Read(Path.Combine(CaseFile.Directory, "mixed-units-bounds.dat"));

        var first = formulation.Fractions[0];
        Assert.True(first.LowerBound.IsMicrometres); // 0.1, the boundary itself, is micrometres
        Assert.Equal(0.1 * 1e-6, first.LowerBound.Metres);
        Assert.True(first.UpperBound.IsMicrometres); // 50

        var second = formulation.Fractions[1];
        Assert.False(second.LowerBound.IsMicrometres); // 0.099999, just under the boundary, is metres
        Assert.Equal(0.099999, second.LowerBound.Metres);
        Assert.False(second.UpperBound.IsMicrometres); // 0.00015, clearly metres
    }
}
