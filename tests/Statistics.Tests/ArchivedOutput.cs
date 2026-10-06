using System.Text.RegularExpressions;
using PropStruct.Tests.Harness;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// What an archived <c>results.m.txt</c> says about the input it was run on: the
/// <c>Input filename</c> line of its header, resolved to the shipped <c>.dat</c> of
/// <c>tests/Fixtures/Legacy/formulations</c> that carries that name (case and the blanks the
/// original pads the name with ignored), or to none. The name an output was run on is read
/// from the output, never typed in a test (root BOOT.md, "No expected value typed into a test
/// when it exists in a fixture file").
/// </summary>
internal static class ArchivedOutput
{
    private static readonly Regex InputFilenameLine = new(@"^\s*%\s*Input filename:\s*(?<name>.*?)\s*$", RegexOptions.Compiled);

    /// <summary>
    /// The <c>Input filename</c> of the output's header with its blanks removed, upper case, or
    /// <c>null</c> for an output whose header has none (the oldest ones).
    /// </summary>
    internal static string? InputFilename(string outputPath)
    {
        foreach (var line in File.ReadLines(outputPath).Take(8))
        {
            var match = InputFilenameLine.Match(line);
            if (match.Success)
            {
                return Regex.Replace(match.Groups["name"].Value, @"\s+", string.Empty).ToUpperInvariant();
            }
        }

        return null;
    }

    /// <summary>
    /// The shipped <c>.dat</c> whose file name equals the output's <c>Input filename</c>, or
    /// <c>null</c> when the archive ships no such input or the output names none.
    /// </summary>
    internal static string? ShippedFormulation(string outputPath)
    {
        var wanted = InputFilename(outputPath);
        return wanted is null ? null : Directory.EnumerateFiles(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations"), "*.dat")
            .SingleOrDefault(path => string.Equals(Path.GetFileName(path), wanted, StringComparison.OrdinalIgnoreCase));
    }
}
