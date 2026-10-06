using ILGPU;

namespace PropStruct.Particle;

/// <summary>
/// The oxidizer fraction table an attempt draws sizes from (API.md, "Setup and cycle
/// inputs"). Views into host arrays built by the driver from <c>Statistics</c>' output.
/// </summary>
internal struct FractionTable
{
    /// <summary>DDOK: 2·NMM, the lower and upper bound of each fraction, m, pairs (2i, 2i+1).</summary>
    public ArrayView<double> Bounds;

    /// <summary>Z11: NMM + 1, the cumulative fraction weights (Fortran 1-based Z(1..NMM+1)).</summary>
    public ArrayView<double> Cumulative;

    /// <summary>SFR: NMM, 1 where the fraction forms pockets, 0 where it does not.</summary>
    public ArrayView<byte> PocketForming;
}
