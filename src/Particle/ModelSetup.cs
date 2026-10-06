namespace PropStruct.Particle;

/// <summary>
/// The model parameters and array sizes an attempt reads, produced by
/// <c>Statistics.Setup.Prepare</c> (API.md, "Setup and cycle inputs"). Blittable and
/// kernel-compatible: value fields only.
/// </summary>
internal struct ModelSetup
{
    /// <summary>NMM, the number of oxidizer fractions.</summary>
    public int FractionCount;

    /// <summary>JZZ: 2 selects the uniform-in-D size law, any other value the uniform-in-1/D² law (BOOT.md, Constraints).</summary>
    public int SizeLaw;

    /// <summary>Di, the histogram cell size, m.</summary>
    public double CellSize;

    /// <summary>Dj, the category step of the conditional oxidizer distribution, m.</summary>
    public double CategoryStep;

    /// <summary>Dmin, the smallest accepted particle size, m.</summary>
    public double Dmin;

    /// <summary>Dmax, the largest accepted particle size (from PARAM's tail probability), m.</summary>
    public double Dmax;

    /// <summary>sLamd, the Poisson rate of the neighbour-distance draw (Fortran line 378).</summary>
    public double Lambda;

    public double Ak1;
    public double Ak2;
    public double Ak3;
    public double Ak4;

    /// <summary>ivar: 0 restarts the whole attempt on the AK1 gap test, any other value only the neighbour draw.</summary>
    public int Variant;

    /// <summary>k5, the weight of the local bridge/pocket volume terms.</summary>
    public double Alpha;

    /// <summary>k7 (karmcoef), the pocket-in-pocket correction coefficient.</summary>
    public double PocketCoefficient;

    /// <summary>k8 (mkmcoef), the bridge correction coefficient.</summary>
    public double BridgeCoefficient;

    /// <summary>k6 (nn_min), the lower acceptance bound of pockets per bridge.</summary>
    public double NnMin;

    /// <summary>nn_max, the upper acceptance bound of pockets per bridge.</summary>
    public double NnMax;

    public int Ndok;
    public int Nkarm;
    public int Ncat;
    public int Nc;

    /// <summary>Maximum distance draws (label 501 passes) allowed in one attempt before it fails.</summary>
    public int NeighbourBudget;

    /// <summary>Maximum pocket-size redraws (label 451 passes) allowed at one bridge before the attempt fails.</summary>
    public int PocketRedrawBudget;

    /// <summary>
    /// How the accumulators of this run combine their terms (root BOOT.md, "Precision kind
    /// is an option of every run, and Binary64 is the default"). The default value of this field
    /// (unset, zero) is <see cref="PrecisionKind.Binary64"/>, so a caller built before this
    /// option existed gets exactly the behaviour it always had, unchanged.
    /// </summary>
    public PrecisionKind Kind;

    public AccumulatorLayout Layout;
}
