namespace PropStruct.Cli.Tests;

/// <summary>
/// A directory under the OS temp folder that outlives one test and is removed at its end - so that "run"
/// can write <c>results.m</c> somewhere real without a test ever writing into the repository working tree
/// (tests/Cli.Tests/BOOT.md, Constraints: "no test writes into the repository working tree - outputs go
/// to a temporary directory").
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("propstruct-cli-tests-").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup; a leftover temp directory is not a test failure.
        }
    }
}
