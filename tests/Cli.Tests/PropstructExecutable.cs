namespace PropStruct.Cli.Tests;

/// <summary>The compiled <c>propstruct</c> executable, copied beside this test assembly by the project
/// reference's own apphost copy - the one path every test that must observe the tool as a real process
/// (rather than in-process, through <see cref="Program.Run"/>) needs, kept in one place so it is computed
/// once, not once per test file.</summary>
internal static class PropstructExecutable
{
    public static string Path { get; } = System.IO.Path.Combine(
        AppContext.BaseDirectory, "propstruct" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty));
}
