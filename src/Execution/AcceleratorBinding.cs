using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;

namespace PropStruct.Execution;

/// <summary>
/// One ILGPU context, the accelerator built on it, the libnvvm binding the CUDA path needs, and the
/// description of all three. Disposes them in order, once, and disposes whatever the build created when
/// the build fails: the rule lives here instead of once per creation path (BOOT.md, Invariants: "Only this
/// node names <c>ILGPU.Runtime.Cuda</c>").
/// </summary>
internal sealed class AcceleratorBinding : IDisposable
{
    private readonly Context? _context;
    private bool _disposed;

    private AcceleratorBinding(Context? context, Accelerator? accelerator, NvvmAPI? nvvm, AcceleratorInfo info, bool refused)
    {
        _context = context;
        Accelerator = accelerator;
        Nvvm = nvvm;
        Info = info;
        Refused = refused;
    }

    /// <summary>The accelerator this binding runs on; null when the binding is <see cref="Refused"/>.</summary>
    public Accelerator? Accelerator { get; }

    /// <summary>The libnvvm binding of the CUDA path; null on the CPU accelerator and on a refused binding.</summary>
    public NvvmAPI? Nvvm { get; }

    /// <summary>What the binding is, as <see cref="Engine.Accelerator"/> reports it.</summary>
    public AcceleratorInfo Info { get; }

    /// <summary>
    /// True when <see cref="AcceleratorKind.Cuda"/> was requested but CUDA is unavailable
    /// (API.md, "Errors": "Create returns an engine whose every run call returns
    /// AcceleratorUnavailable"). No accelerator is bound; <see cref="Accelerator"/> is null.
    /// </summary>
    public bool Refused { get; }

    /// <summary>
    /// Binds the CPU accelerator with all cores, or, when <paramref name="threads"/> is given, an
    /// accelerator forced to that many logical threads: 1 runs every kernel launch on a single OS thread
    /// (<see cref="CPUAcceleratorMode.Sequential"/>, which serializes regardless of the device's own
    /// declared thread count), any other value creates a <see cref="CPUDevice"/> sized to exactly that
    /// count and runs it in <see cref="CPUAcceleratorMode.Parallel"/>. Added in this coding session for the
    /// determinism-under-thread-count acceptance criterion (root BOOT.md: "1, 4 and 16 CPU threads"), which
    /// the API.md sketch's <c>Create(AcceleratorKind, long)</c> had no parameter for (AGENTS.md §4);
    /// <see langword="null"/> preserves the original, all-cores behaviour for every other caller.
    /// </summary>
    public static AcceleratorBinding Cpu(string? cudaSkippedBecause, int? threads = null)
    {
        var context = Context.Create(builder => builder.CPU());
        try
        {
            var accelerator = threads is null
                ? context.CreateCPUAccelerator(0)
                : threads.Value == 1
                    ? new CPUDevice(2, 1, 1).CreateCPUAccelerator(context, CPUAcceleratorMode.Sequential)
                    : new CPUDevice(threads.Value, 1, 1).CreateCPUAccelerator(context, CPUAcceleratorMode.Parallel);
            var info = new AcceleratorInfo(AcceleratorKind.Cpu, accelerator.Name, accelerator.MemorySize, false, cudaSkippedBecause);
            return new AcceleratorBinding(context, accelerator, null, info, false);
        }
        catch
        {
            context.Dispose();
            throw;
        }
    }

    /// <summary>Binds a CUDA device found through <see cref="LibDeviceLocator"/> against the real process
    /// environment, or returns a refused binding naming why.</summary>
    public static AcceleratorBinding Cuda() => Cuda(Environment.GetEnvironmentVariable);

    /// <summary>
    /// <see cref="Cuda()"/> with <paramref name="environment"/> in place of the real process environment for
    /// both the kill switch and libnvvm/libdevice discovery (review item 3, 2026-09-18: the seam
    /// <see cref="AcceleratorChoice.Decide"/> threads through from <see cref="Engine.Create"/>).
    /// </summary>
    public static AcceleratorBinding Cuda(Func<string, string?> environment)
    {
        if (AcceleratorChoice.IsCudaForbidden(environment))
        {
            return Refuse($"CUDA was requested, but {AcceleratorChoice.NoCudaVariable}=1 forbids it.");
        }

        var (dll, bitcode, tried) = LibDeviceLocator.Locate(environment);
        if (dll is null || bitcode is null)
        {
            var pathsNote = tried.Count == 0 ? "" : " Paths tried: " + string.Join("; ", tried) + ".";
            return Refuse($"libnvvm ({LibDeviceLocator.LibraryFileName}) and libdevice (libdevice.10.bc) were not found.{pathsNote}");
        }

        Context? context = null;
        try
        {
            // LibDevice() makes ILGPU emit the intrinsic calls; the wrappers themselves come from this node's post-link.
            context = Context.Create(builder => builder.Cuda().Math(MathMode.Default).LibDevice(dll, bitcode));
            var devices = context.GetCudaDevices();
            if (devices.Count == 0)
            {
                context.Dispose();
                return Refuse("no CUDA device was found.");
            }

            var accelerator = context.CreateCudaAccelerator(0);
            var nvvm = NvvmAPI.Create(dll, bitcode);
            var info = new AcceleratorInfo(AcceleratorKind.Cuda, accelerator.Name, accelerator.MemorySize, true, null);
            return new AcceleratorBinding(context, accelerator, nvvm, info, false);
        }
        catch (Exception failure) when (IsDriverOrDeviceFailure(failure))
        {
            context?.Dispose();
            return Refuse("the CUDA context could not be created (driver or device problem): " + failure.Message);
        }
    }

    /// <summary>
    /// The exception types <c>Context.CreateCudaAccelerator</c> (an ILGPU extension method, not a cref target
    /// from here) and <see cref="NvvmAPI.Create(string, string)"/> are
    /// documented (CA1031) to raise for a driver or device problem, rather than <see cref="Exception"/>
    /// at large: ILGPU's own <see cref="CudaException"/> for a failed driver call and
    /// <see cref="NotSupportedException"/> for an unsupported driver version from
    /// <c>CudaAccelerator</c>'s constructor, and, from <c>NvvmAPI.Create</c>'s native library load,
    /// <see cref="DllNotFoundException"/>, <see cref="BadImageFormatException"/>,
    /// <see cref="EntryPointNotFoundException"/>, <see cref="IOException"/> (covering
    /// <see cref="FileNotFoundException"/>) and <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    private static bool IsDriverOrDeviceFailure(Exception failure) =>
        failure is CudaException or NotSupportedException or DllNotFoundException or BadImageFormatException
            or EntryPointNotFoundException or IOException or UnauthorizedAccessException;

    private static AcceleratorBinding Refuse(string reason) =>
        new(null, null, null, new AcceleratorInfo(AcceleratorKind.Cuda, "unavailable", 0, false, reason), true);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Nvvm?.Dispose();
        Accelerator?.Dispose();
        _context?.Dispose();
    }
}
