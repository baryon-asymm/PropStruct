using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// A ratchet kept in a file beside the tests: the received text must equal the approved file's, line for line,
/// and the failure names the lines that appeared and the lines that vanished. Setting
/// <c>PROPSTRUCT_WRITE_APPROVED=1</c> writes the received text first (a deliberate, reviewed move of the file).
/// </summary>
internal static class ApprovedFile
{
    /// <summary>Fails unless <paramref name="received"/> is the content of <c>tests/Statistics.Tests/<paramref name="fileName"/></c>.</summary>
    public static void AssertMatches(string fileName, string received)
    {
        var approvedPath = RepositoryPaths.Resolve("tests", "Statistics.Tests", fileName);
        if (Environment.GetEnvironmentVariable("PROPSTRUCT_WRITE_APPROVED") == "1")
        {
            File.WriteAllText(approvedPath, received);
        }

        var approved = File.Exists(approvedPath) ? File.ReadAllText(approvedPath).Replace("\r\n", "\n", StringComparison.Ordinal) : string.Empty;
        var receivedLines = received.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var approvedLines = approved.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        var appeared = receivedLines.Except(approvedLines).Order(StringComparer.Ordinal).Take(10).ToList();
        var vanished = approvedLines.Except(receivedLines).Order(StringComparer.Ordinal).Take(10).ToList();
        Assert.True(approved == received,
            $"{fileName}: {receivedLines.Except(approvedLines).Count()} lines appeared and {approvedLines.Except(receivedLines).Count()} vanished. "
            + $"First appeared:\n{string.Join("\n", appeared)}\nFirst vanished:\n{string.Join("\n", vanished)}");
    }
}
