using Xunit;

namespace PropStruct.Input.Tests;

/// <summary>
/// L0 (tests/Input.Tests/BOOT.md): every malformed case rejected with its line number - a missing value, a
/// repeat count, a data-terminating slash, a non-integral value for an integer item, and <c>GSV != 2</c>
/// (BOOT.md "Acceptance criteria"), against <c>tests/Fixtures/cases/input/</c>.
/// </summary>
public sealed class MalformedCasesTests
{
    public static TheoryData<string> CaseNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in CaseFile.ReadMalformedCases().Keys)
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void RejectedWithItsLineNumber(string caseName)
    {
        var expected = CaseFile.ReadMalformedCases()[caseName];
        var path = Path.Combine(CaseFile.Directory, caseName + ".dat");

        var exception = Assert.Throws<FormulationFormatException>(() => DatFile.Read(path));

        Assert.Equal(expected.LineNumber, exception.LineNumber);
        Assert.Contains(expected.MessageContains, exception.Message);
    }
}
