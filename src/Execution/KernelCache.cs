using System.Reflection;
using ILGPU.Backends.EntryPoints;
using ILGPU.Backends.PTX;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace PropStruct.Execution;

/// <summary>
/// The typed launchers of <see cref="Kernels"/>' entry points, compiled — and on CUDA post-linked — on
/// first use and kept for the binding's lifetime, one entry per entry-point name (taken from APThermo's
/// own <c>KernelCache</c>, BOOT.md, Constraints). An engine is used from one thread at a time (API.md), so
/// this cache needs no lock of its own beyond what a single-threaded caller already implies; it still
/// guards its dictionary since a batch's own multiple kernel launches (derive, run, normalize, fold) each
/// call <see cref="Get{TDelegate}"/>.
/// </summary>
internal sealed class KernelCache(AcceleratorBinding binding)
{
    private readonly Dictionary<string, Delegate> _launchers = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <summary>The launcher of the named entry point of <see cref="Kernels"/>.</summary>
    public TDelegate Get<TDelegate>(string name) where TDelegate : Delegate
    {
        lock (_gate)
        {
            if (_launchers.TryGetValue(name, out var cached))
            {
                return (TDelegate)cached;
            }

            var launcher = Load(name).CreateLauncherDelegate<TDelegate>();
            _launchers[name] = launcher;
            return launcher;
        }
    }

    private Kernel Load(string name)
    {
        var method = typeof(Kernels).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException($"no kernel named {name}");
        var accelerator = binding.Accelerator ?? throw new InvalidOperationException("the binding has no accelerator to load a kernel on.");
        if (accelerator is not CudaAccelerator cuda)
        {
            return accelerator.LoadAutoGroupedKernel(method);
        }

        // Every CUDA kernel goes through the post-link; the CPU accelerator loads the method as ILGPU does.
        // The compile is the same for every group size (no specialization), so the group size chosen by
        // LaunchShape changes the grid only, never the PTX.
        var entry = EntryPointDescription.FromImplicitlyGroupedKernel(method);
        var compiled = (PTXCompiledKernel)cuda.Backend.Compile(entry, KernelSpecialization.Empty);
        var linked = LibDevicePostLink.Link(cuda, binding.Nvvm!, compiled);
        return LaunchShape.GroupSizeOf(name) is { } groupSize
            ? cuda.LoadImplicitlyGroupedKernel(linked, groupSize)
            : cuda.LoadAutoGroupedKernel(linked);
    }
}
