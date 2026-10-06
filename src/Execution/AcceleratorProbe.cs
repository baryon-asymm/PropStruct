namespace PropStruct.Execution;

/// <summary>
/// Lists the accelerators an <see cref="Engine"/> could actually be created on, without holding either
/// open (API.md, "Accelerators"). The CPU accelerator is always listed; CUDA is listed only when the kill
/// switch is not set and a device, libnvvm and libdevice are all found — the same rule
/// <see cref="AcceleratorChoice"/> applies when a caller asks for <see cref="AcceleratorKind.Cuda"/> or
/// <see cref="AcceleratorKind.Auto"/>.
/// </summary>
public static class AcceleratorProbe
{
    /// <summary>Every accelerator this process could bind to right now: the CPU accelerator, and CUDA when it is available.
    /// <paramref name="environment"/> replaces the real process environment for the kill switch and CUDA discovery
    /// (review item 3, 2026-09-18, the same seam as <see cref="Engine.Create"/>'s own); <see langword="null"/>
    /// (every production caller) keeps the real process environment.</summary>
    public static IReadOnlyList<AcceleratorInfo> Discover(Func<string, string?>? environment = null)
    {
        var env = environment ?? Environment.GetEnvironmentVariable;
        var found = new List<AcceleratorInfo>();

        using (var cpu = AcceleratorBinding.Cpu(null))
        {
            found.Add(cpu.Info);
        }

        using (var cuda = AcceleratorBinding.Cuda(env))
        {
            if (!cuda.Refused)
            {
                found.Add(cuda.Info);
            }
        }

        return found;
    }
}
