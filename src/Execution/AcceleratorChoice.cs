namespace PropStruct.Execution;

/// <summary>
/// Turns an <see cref="AcceleratorKind"/> into the binding to run on (BOOT.md, Invariants): CUDA when it
/// is not forbidden, libnvvm and libdevice are found and a device exists; otherwise the CPU accelerator
/// with all cores. <see cref="AcceleratorKind.Auto"/> falls back to the CPU accelerator and keeps the
/// reason; <see cref="AcceleratorKind.Cuda"/> does not fall back — it refuses instead
/// (API.md, "Errors").
/// </summary>
internal static class AcceleratorChoice
{
    /// <summary>The name of the environment variable that forbids CUDA when set to <c>1</c> (root BOOT.md, "The CPU path needs no NVIDIA software").</summary>
    public const string NoCudaVariable = "PROPSTRUCT_NO_CUDA";

    /// <summary>True when the real process environment forbids CUDA.</summary>
    public static bool CudaForbidden => IsCudaForbidden(Environment.GetEnvironmentVariable);

    /// <summary>
    /// <see cref="CudaForbidden"/> against an injected <paramref name="environment"/> instead of the real
    /// process environment (review item 3, 2026-09-18: <see cref="Decide"/>'s own injection seam reads
    /// the kill switch through this, so a test can hold its own "CUDA is forbidden" without mutating
    /// <see cref="Environment.SetEnvironmentVariable(string, string?)"/> process-wide — where it would also
    /// apply to every other engine any concurrently running test class creates, xunit running test classes
    /// in parallel by default).
    /// </summary>
    internal static bool IsCudaForbidden(Func<string, string?> environment) => environment(NoCudaVariable)?.Trim() == "1";

    /// <summary>
    /// The binding the kind asks for. <paramref name="cpuThreads"/> forces the CPU accelerator's thread
    /// count whenever a CPU accelerator is the one actually bound (<see cref="AcceleratorKind.Cpu"/>, or
    /// <see cref="AcceleratorKind.Auto"/> once it falls back); see <see cref="AcceleratorBinding.Cpu"/>.
    /// <paramref name="environment"/> replaces <see cref="Environment.GetEnvironmentVariable(string)"/> for
    /// every environment read this call makes (the kill switch, and libnvvm/libdevice discovery); every
    /// production caller leaves it <see langword="null"/> and gets the real process environment.
    /// </summary>
    public static AcceleratorBinding Decide(AcceleratorKind kind, int? cpuThreads = null, Func<string, string?>? environment = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariable;

        if (kind == AcceleratorKind.Cpu)
        {
            return AcceleratorBinding.Cpu(null, cpuThreads);
        }

        if (kind == AcceleratorKind.Cuda)
        {
            return AcceleratorBinding.Cuda(env);
        }

        // Auto: try CUDA, fall back to the CPU accelerator on any failure, keeping the reason.
        var cuda = AcceleratorBinding.Cuda(env);
        if (!cuda.Refused)
        {
            return cuda;
        }

        cuda.Dispose();
        return AcceleratorBinding.Cpu(cuda.Info.CudaSkippedBecause, cpuThreads);
    }
}
