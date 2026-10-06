namespace PropStruct.Execution;

/// <summary>
/// The measured CUDA launch geometry of the entry points of <see cref="Kernels"/> (BOOT.md, "Launch shape"): the
/// group size, in threads, a kernel is launched with, where measurement chose one. An entry point without a
/// chosen size keeps ILGPU's automatic choice, and so does every kernel on the CPU accelerator, which never asks
/// this type. A group size moves no bit: the PTX is compiled once, unspecialized, and only the grid changes.
/// Values here are constants decided by <c>tests/Benchmarks</c>' W5 rule, never configuration.
/// </summary>
internal static class LaunchShape
{
    /// <summary>
    /// The group size of <see cref="Kernels.RunAttempts"/> on CUDA, chosen by the W5 rule
    /// (<c>tests/Benchmarks/BOOT.md</c>, "W5: launch shape"): 64 threads, two warps per group.
    /// </summary>
    internal const int RunAttemptsGroupSize = 64;

    /// <summary>
    /// The group size of the named entry point of <see cref="Kernels"/> on CUDA, or <see langword="null"/> when
    /// the entry point keeps ILGPU's automatic choice.
    /// </summary>
    internal static int? GroupSizeOf(string entryPoint) =>
        string.Equals(entryPoint, nameof(Kernels.RunAttempts), StringComparison.Ordinal)
            ? RunAttemptsGroupSize
            : null;
}
