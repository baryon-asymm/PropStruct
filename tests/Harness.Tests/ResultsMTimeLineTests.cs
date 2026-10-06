using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// <see cref="ResultsMTimeLine.Remove"/> against an independent construction of the same cut on every reference
/// and replica file of the fixtures, and on synthetic files for the shapes the fixtures do not hold.
/// </summary>
public class ResultsMTimeLineTests
{
    private const string Marker = "Calculation time";

    public static IEnumerable<object[]> EveryReferenceAndReplicaFile() => ResultsMFileTests.EveryReferenceAndReplicaFile();

    /// <summary>The oracle: the file's lines with their terminators, the lines holding the marker dropped, the rest
    /// joined, on a string of one char per byte so that no decoding can change a byte.</summary>
    private static byte[] LinesWithoutTheMarker(byte[] bytes, out int removed)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var lines = Regex.Split(text, "(?<=\n)").Where(line => line.Length > 0).ToArray();
        var kept = lines.Where(line => !line.Contains(Marker, StringComparison.Ordinal)).ToArray();
        removed = lines.Length - kept.Length;
        return Encoding.Latin1.GetBytes(string.Concat(kept));
    }

    [Theory]
    [MemberData(nameof(EveryReferenceAndReplicaFile))]
    public void RemoveEqualsTheLineFilterOnEveryReferenceAndReplicaFile(string path)
    {
        var bytes = File.ReadAllBytes(path);

        var expected = LinesWithoutTheMarker(bytes, out var removed);

        Assert.Equal(1, removed);
        Assert.Equal(expected, ResultsMTimeLine.Remove(bytes));
    }

    [Fact]
    public void RemoveKeepsTheLineTerminatorsOfEveryOtherLine()
    {
        var withTimeLine = "Plot1 = 1;\r\n % Calculation time:  0 : 0 : 3\r\nDok43 = [1 2];\n";
        var expected = "Plot1 = 1;\r\nDok43 = [1 2];\n";

        Assert.Equal(Encoding.ASCII.GetBytes(expected), ResultsMTimeLine.Remove(Encoding.ASCII.GetBytes(withTimeLine)));
    }

    [Theory]
    [InlineData("% Calculation time: 1\nA = 1;\n", "A = 1;\n")]
    [InlineData("A = 1;\n% Calculation time: 1", "A = 1;\n")]
    [InlineData("% Calculation time: 1", "")]
    public void RemoveCutsTheLineAtTheStartAndAtTheEndOfTheFile(string input, string expected) =>
        Assert.Equal(Encoding.ASCII.GetBytes(expected), ResultsMTimeLine.Remove(Encoding.ASCII.GetBytes(input)));

    [Fact]
    public void RemoveDoesNotModifyItsArgument()
    {
        var bytes = Encoding.ASCII.GetBytes("A = 1;\n% Calculation time: 1\nB = 2;\n");
        var before = bytes.ToArray();

        _ = ResultsMTimeLine.Remove(bytes);

        Assert.Equal(before, bytes);
    }

    [Fact]
    public void RemoveIsBlindToTheTimeLineAloneAndRedOnAnyOtherByte()
    {
        var first = Encoding.ASCII.GetBytes("A = 1;\n% Calculation time:  0 : 0 : 3\nB = 2;\n");
        var secondTime = Encoding.ASCII.GetBytes("A = 1;\n% Calculation time:  0 : 7 : 9\nB = 2;\n");
        var otherByte = Encoding.ASCII.GetBytes("A = 1;\n% Calculation time:  0 : 0 : 3\nB = 3;\n");

        Assert.Equal(ResultsMTimeLine.Remove(first), ResultsMTimeLine.Remove(secondTime));
        Assert.NotEqual(ResultsMTimeLine.Remove(first), ResultsMTimeLine.Remove(otherByte));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A = 1;\nB = 2;\n")]
    [InlineData("% calculation time: 1\n")]
    public void RemoveThrowsOnAFileWithoutTheMarker(string input) =>
        _ = Assert.Throws<InvalidOperationException>(() => ResultsMTimeLine.Remove(Encoding.ASCII.GetBytes(input)));
}
