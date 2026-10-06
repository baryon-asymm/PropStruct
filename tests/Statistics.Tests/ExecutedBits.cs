using System.Globalization;
using System.Text.Json;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// One value the executable's pre-loop left, as the bits it stored: a REAL*4 (eight hex digits in the fixtures and
/// the survey) or a REAL*8 (sixteen). Read from the <c>executed</c> field of a <c>preloop</c> entry, which is one
/// string for a scalar and a list of strings for an array (<c>read_preloop</c> of
/// <c>tests/Fixtures/cycle_plane_oracle.py</c>).
/// </summary>
internal readonly record struct ExecutedBits(ulong Bits, bool Binary32)
{
    private const int Binary32HexDigits = 8;

    /// <summary>The value widened to <see cref="double"/>.</summary>
    public double Value => Binary32 ? BitConverter.UInt32BitsToSingle((uint)Bits) : BitConverter.UInt64BitsToDouble(Bits);

    /// <summary>The same value with its last bit one higher: the self-tests' one-unit perturbation.</summary>
    public ExecutedBits Next() => this with { Bits = Bits + 1 };

    /// <summary>The bits as the report prints them.</summary>
    public string Hex => Binary32 ? $"0x{Bits:X8}" : $"0x{Bits:X16}";

    /// <summary>The bits of a <c>port</c> value held to this one's width: binary32 for a REAL*4 key, the double itself otherwise.</summary>
    public string HexOf(double port) =>
        Binary32 ? $"0x{BitConverter.SingleToUInt32Bits((float)port):X8}" : $"0x{BitConverter.DoubleToUInt64Bits(port):X16}";

    /// <summary>Reads the <c>executed</c> field of a <c>preloop</c> entry: a string, or a list of strings.</summary>
    public static ExecutedBits[] Read(JsonElement executed) =>
        executed.ValueKind == JsonValueKind.Array
            ? executed.EnumerateArray().Select(element => Parse(element.GetString()!)).ToArray()
            : [Parse(executed.GetString()!)];

    private static ExecutedBits Parse(string hex) =>
        new(ulong.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture), hex.Length == Binary32HexDigits);
}
