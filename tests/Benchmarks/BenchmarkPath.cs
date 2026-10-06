namespace PropStruct.Benchmarks;

/// <summary>
/// One of the execution paths this node measures (BOOT.md, Purpose): the reference-mode
/// sequence, the host-thread path forced to 1 and to 16 threads, the ILGPU CPU accelerator's
/// own kernel-launch path (kept as a record-only curiosity, root BOOT.md, "One particle
/// program"), CUDA, and the legacy executable.
///
/// <see cref="Cpu"/> is declared here because BOOT.md's Purpose and this node's own API.md
/// example command line both name it, but it cannot be reached from this node: the ILGPU
/// kernel-launch path on the CPU accelerator is chosen by
/// <c>Execution.Engine.Create</c>'s <c>forceIlgpuKernelsOnCpu</c> parameter, which is internal
/// to <c>Execution</c> (<c>InternalsVisibleTo</c> for <c>Simulation</c>, <c>Cli</c> and their
/// own tests only, src/Execution/API.md) and is never set true by <c>Simulation</c>, the only
/// production caller. <c>Simulation</c>'s public <c>SimulationOptions</c> has no field that
/// reaches it either. <see cref="Program"/> reports this path as unavailable rather than
/// running anything under a different name (BOOT.md, "## Escalation: the CPU accelerator
/// oracle path").
/// </summary>
internal enum BenchmarkPath
{
    Reference,
    Host1,
    Host16,
    Cpu,
    Cuda,
    Original,
}

internal static class BenchmarkPathNames
{
    public static string ToToken(this BenchmarkPath path) => path switch
    {
        BenchmarkPath.Reference => "reference",
        BenchmarkPath.Host1 => "host1",
        BenchmarkPath.Host16 => "host16",
        BenchmarkPath.Cpu => "cpu",
        BenchmarkPath.Cuda => "cuda",
        BenchmarkPath.Original => "original",
        _ => throw new ArgumentOutOfRangeException(nameof(path), path, message: null),
    };

    public static BenchmarkPath Parse(string token) => token switch
    {
        "reference" => BenchmarkPath.Reference,
        "host1" => BenchmarkPath.Host1,
        "host16" => BenchmarkPath.Host16,
        "cpu" => BenchmarkPath.Cpu,
        "cuda" => BenchmarkPath.Cuda,
        "original" => BenchmarkPath.Original,
        _ => throw new ArgumentException($"unknown path '{token}'; expected one of reference, host1, host16, cpu, cuda, original"),
    };
}
