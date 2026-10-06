using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;

namespace PropStruct.Tests.Harness;

/// <summary>
/// One ILGPU context with the CPU accelerator, loaded once (this node's BOOT.md, Purpose: "one CPU host for
/// kernel-compatible code"). One per test class or collection fixture: it never creates a CUDA accelerator, so
/// a test node built on it stays testable without CUDA (root BOOT.md, "The CPU path needs no NVIDIA software").
/// </summary>
public sealed class CpuHost : IDisposable
{
    public CpuHost()
    {
        Context = Context.Create(builder => builder.CPU());
        Accelerator = Context.CreateCPUAccelerator(0);
    }

    public Context Context { get; }

    public Accelerator Accelerator { get; }

    public void Dispose()
    {
        Accelerator.Dispose();
        Context.Dispose();
    }
}
