namespace PropStruct.Random;

/// <summary>
/// The 128-bit state of the original's GSV=2 generator, <c>s = High·2⁶⁴ + Low</c> (BOOT.md, "State").
/// Blittable and kernel-compatible: no member but the two 64-bit halves.
/// </summary>
internal struct Mcg128State
{
    public ulong Low;
    public ulong High;
}
