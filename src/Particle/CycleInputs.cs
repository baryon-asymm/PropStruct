using ILGPU;

namespace PropStruct.Particle;

/// <summary>
/// The per-cycle inputs an attempt reads and never writes (API.md, "Setup and cycle
/// inputs"; BOOT.md, "Frozen inputs").
/// </summary>
internal struct CycleInputs
{
    /// <summary>IPRIS: 0 in cycle 0 (warm-up), 1 in cycles 1..KXX.</summary>
    public int CycleFlag;

    /// <summary>The cycle's Dmaxxx, the size ceiling of the Var#1 bridge correction.</summary>
    public double Dmaxxx;

    /// <summary>QKS1: Nkarm, the normalized pocket histogram, frozen for the whole launch.</summary>
    public ArrayView<double> Qks1;

    /// <summary>pdoksmall: Ndok, the per-cell pocket-in-pocket probability of the cycle.</summary>
    public ArrayView<double> Pdoksmall;
}
