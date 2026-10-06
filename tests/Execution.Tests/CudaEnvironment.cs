namespace PropStruct.Execution.Tests;

/// <summary>
/// The injected "CUDA forbidden" environment every test in this node that needs a specific engine refused
/// uses, in place of mutating the real process environment (Opus audit item 3, 2026-09-18). xunit runs test
/// classes in parallel by default: <c>Environment.SetEnvironmentVariable(AcceleratorChoice.NoCudaVariable, "1")</c>
/// would apply, for its whole scope, to every engine any other, concurrently running test class creates too
/// — a CUDA test elsewhere in the suite could see the kill switch set (or cleared under it) by a guard it
/// never asked for, going spuriously red or taking the refusal branch without ever probing CUDA.
/// <see cref="Engine.Create"/> and <see cref="AcceleratorProbe.Discover"/> both take an <c>environment</c>
/// parameter for exactly this: <see cref="Forbidding"/> answers only that one call's own reads.
/// </summary>
internal static class CudaEnvironment
{
    /// <summary>Answers <see cref="AcceleratorChoice.NoCudaVariable"/>=1 and nothing else.</summary>
    public static string? Forbidding(string name) => name == AcceleratorChoice.NoCudaVariable ? "1" : null;
}
