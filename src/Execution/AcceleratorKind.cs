namespace PropStruct.Execution;

/// <summary>
/// Which accelerator an <see cref="Engine"/> binds to (API.md, "Accelerators").
/// </summary>
public enum AcceleratorKind
{
    /// <summary>CUDA when a device, libnvvm and libdevice are found and the kill switch is not set; otherwise the CPU accelerator.</summary>
    Auto,

    /// <summary>The CPU accelerator of ILGPU with all cores; no CUDA API is touched.</summary>
    Cpu,

    /// <summary>A CUDA device. Does not fall back: an engine created with this kind when CUDA is unavailable
    /// reports why on its <see cref="AcceleratorInfo"/> and refuses every run call
    /// (API.md, "Errors").</summary>
    Cuda,
}
