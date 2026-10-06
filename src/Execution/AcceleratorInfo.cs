namespace PropStruct.Execution;

/// <summary>
/// What an accelerator binding is and why it is that one (API.md, "Accelerators").
/// </summary>
/// <param name="Kind">Which accelerator was bound.</param>
/// <param name="Name">The name of the bound device, as ILGPU reports it (or a fixed
/// placeholder when <see cref="AcceleratorKind.Cuda"/> was requested and refused,
/// API.md, "Errors").</param>
/// <param name="MemoryBytes">The device's total memory, bytes; 0 when the accelerator
/// could not be bound.</param>
/// <param name="LibDeviceLinked">True when this binding is a CUDA device whose kernels
/// go through the libdevice post-link (BOOT.md, Constraints); always false on the CPU
/// accelerator and on a refused CUDA request.</param>
/// <param name="CudaSkippedBecause">Why CUDA was not bound: set when
/// <see cref="AcceleratorKind.Auto"/> fell back to the CPU accelerator, and when
/// <see cref="AcceleratorKind.Cuda"/> was requested but is unavailable; null otherwise
/// (root BOOT.md, "The CPU path needs no NVIDIA software").</param>
public sealed record AcceleratorInfo(
    AcceleratorKind Kind, string Name, long MemoryBytes, bool LibDeviceLinked, string? CudaSkippedBecause);
