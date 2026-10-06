using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0/L1 of the BOOT.md table: "the tool never reads the console" (BOOT.md, Invariants) / "the tool is
/// proven not to read the console" (BOOT.md, Acceptance criteria). A closed-stdin process run
/// (<see cref="StdinClosedTests"/>) only proves the tool does not hang: <c>Console.ReadLine</c> on a
/// closed stream returns <c>null</c> immediately rather than blocking, so a tool that read one line and
/// silently treated <c>null</c> as "no more input" would pass that test too. This is the actual proof:
/// no source file under <c>src/Cli</c> mentions any member of <see cref="Console"/> that reads.
/// </summary>
public class SourceCodeTests
{
    private static readonly string[] ForbiddenSubstrings = { "Console.Read", "Console.In", "Console.OpenStandardInput" };

    [Fact]
    public void NoConsoleReadAppearsUnderSrcCli()
    {
        var srcCliDirectory = RepositoryPaths.Resolve("src", "Cli");
        var sourceFiles = Directory.GetFiles(srcCliDirectory, "*.cs", SearchOption.TopDirectoryOnly);
        Assert.NotEmpty(sourceFiles);

        var offenders = new List<string>();
        foreach (var file in sourceFiles)
        {
            var text = File.ReadAllText(file);
            foreach (var forbidden in ForbiddenSubstrings)
            {
                if (text.Contains(forbidden, StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetFileName(file)}: contains '{forbidden}'");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }
}
