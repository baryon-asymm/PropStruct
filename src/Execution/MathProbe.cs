namespace PropStruct.Execution;

/// <summary>
/// The probe of the particle program's own math list (root BOOT.md, Constraints: "of System.Math only the
/// double overloads Log, Sqrt, Pow, Acos, Sin, Tan, Abs, Max, Min, Floor ... each with a libdevice
/// wrapper"): one output per function per input, so CUDA can be checked against the CPU accelerator
/// function by function (BOOT.md, Constraints: "a probe kernel compared against the CPU accelerator").
/// </summary>
internal static class MathProbe
{
    /// <summary>The functions of the root's list, in the order of the probe's outputs.</summary>
    public static readonly IReadOnlyList<string> Functions =
        ["Log", "Sqrt", "Pow", "Acos", "Sin", "Tan", "Abs", "Max", "Min", "Floor"];

    /// <summary>Outputs per input.</summary>
    public static int FunctionCount => Functions.Count;

    /// <summary>
    /// The compile-time count <see cref="Kernels.ProbeMath"/> strides by. The kernel cannot call
    /// <see cref="FunctionCount"/> itself: it would touch the managed string array
    /// <see cref="Functions"/>, and kernel-compatible code allows no strings (root BOOT.md); a
    /// <c>const</c> inlines into the kernel, a property getter does not. A test ties it to
    /// <see cref="FunctionCount"/> so the two cannot drift silently.
    /// </summary>
    internal const int StrideCount = 10;

    /// <summary>The exponent the probe passes to <see cref="Math.Pow(double, double)"/>.</summary>
    public const double PowExponent = 1.37;
}
