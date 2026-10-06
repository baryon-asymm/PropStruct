namespace PropStruct.Input;

/// <summary>
/// The fourteen values the original asked for in its interactive menu (BOOT.md, "Defaults of the menu",
/// source lines 66-80 for the defaults and lines 159-166 for the menu order), given here as options instead.
/// </summary>
public sealed record ModelParameters
{
    /// <summary>The original's defaults, source lines 73-80 (<c>Dmin</c>/<c>Di</c>/<c>Dj</c> lines 66-68).</summary>
    public static ModelParameters Default { get; } = new()
    {
        Dmin = Length.FromMetres(10e-6),
        CellSize = Length.FromMetres(10e-6),
        CategoryStep = Length.FromMetres(10e-6),
        EpsDok = 0.05,
        Alpha = 0.25,
        NnMin = 3,
        NnMax = 100,
        PocketCoefficient = 8.2,
        BridgeCoefficient = 7.73,
        TailProbability = 0,
        HomogenizedOxidizerFraction = 0,
        ReadPocketFormingFractions = false,
        Variant = 0,
        AggregatedOxideFraction = 0,
    };

    /// <summary>Menu [1]: the smallest oxidizer particle diameter (Fortran <c>Dmin</c>).</summary>
    public Length Dmin { get; init; }

    /// <summary>Menu [2]: the cell size (Fortran <c>Di</c>).</summary>
    public Length CellSize { get; init; }

    /// <summary>Menu [3]: the category step (Fortran <c>Dj</c>).</summary>
    public Length CategoryStep { get; init; }

    /// <summary>Menu [4]: <c>eps_dok</c>.</summary>
    public double EpsDok { get; init; }

    /// <summary>Menu [5]: <c>alpha</c> (Fortran <c>k5</c>).</summary>
    public double Alpha { get; init; }

    /// <summary>Menu [6]: <c>nn_min</c> (Fortran <c>k6</c>).</summary>
    public double NnMin { get; init; }

    /// <summary>Menu [7]: <c>karmcoef</c>, the pocket-in-pocket probability coefficient (Fortran <c>k7</c>).</summary>
    public double PocketCoefficient { get; init; }

    /// <summary>Menu [8]: <c>mkmcoef</c>, the pocket-in-bridge probability coefficient (Fortran <c>k8</c>).</summary>
    public double BridgeCoefficient { get; init; }

    /// <summary>Menu [9]: <c>alfa</c>, the tail probability (printed as "Statistical significance P(alpha)").</summary>
    public double TailProbability { get; init; }

    /// <summary>Menu [10]: <c>nn_max</c>.</summary>
    public double NnMax { get; init; }

    /// <summary>Menu [11]: <c>gdokns</c> (printed as "Zok*").</summary>
    public double HomogenizedOxidizerFraction { get; init; }

    /// <summary>Menu [12]: <c>answer1</c>, whether the <c>SFR</c> line of the <c>.dat</c> is read.</summary>
    public bool ReadPocketFormingFractions { get; init; }

    /// <summary>Menu [13]: <c>ivar</c>.</summary>
    public int Variant { get; init; }

    /// <summary>Menu [14]: <c>eta</c>, the aggregated oxide fraction.</summary>
    public double AggregatedOxideFraction { get; init; }
}
