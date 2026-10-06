using System.Globalization;
using PropStruct.Execution;

namespace PropStruct.Cli;

/// <summary>Renders <see cref="AcceleratorProbe.Discover"/>'s own list, one line per accelerator (BOOT.md,
/// "Output ✅": "one line per accelerator found (kind, name, memory, libdevice)").</summary>
internal static class DevicesReport
{
    public static string Render(IReadOnlyList<AcceleratorInfo> accelerators)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        foreach (var accelerator in accelerators)
        {
            writer.WriteLine(
                "{0} {1} {2} bytes libdevice={3}{4}",
                accelerator.Kind,
                accelerator.Name,
                accelerator.MemoryBytes.ToString(CultureInfo.InvariantCulture),
                accelerator.LibDeviceLinked ? "yes" : "no",
                accelerator.CudaSkippedBecause is null ? string.Empty : $" (CUDA skipped: {accelerator.CudaSkippedBecause})");
        }

        return writer.ToString();
    }
}
