using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Input.Tests;

/// <summary>
/// L1 (tests/Input.Tests/BOOT.md): every archived <c>.dat</c> parses, the list generated from the fixture
/// directory (BOOT.md "Taboos": do not type a list of archived formulations).
/// </summary>
public sealed class ArchivedFormulationsTests
{
    private static readonly string FormulationsDirectory =
        RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations");

    public static TheoryData<string> ArchivedFiles()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.EnumerateFiles(FormulationsDirectory, "*.dat"))
        {
            data.Add(Path.GetFileName(path));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ArchivedFiles))]
    public void EveryArchivedFormulationParses(string fileName)
    {
        var formulation = DatFile.Read(Path.Combine(FormulationsDirectory, fileName));

        Assert.Equal(Path.GetFileNameWithoutExtension(fileName), formulation.Name);
        Assert.NotEmpty(formulation.Fractions);
    }
}
