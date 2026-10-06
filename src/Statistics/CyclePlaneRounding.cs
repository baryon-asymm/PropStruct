using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The per-cycle plane's rounding sites under one run's precision kind (BOOT.md, "## Report",
/// "The per-cycle plane from the executable's listing"). Each method is one site kind of
/// <c>CyclePlane.listing.generated.txt</c>, and its name is the kind the site map checks
/// (<c>CyclePlaneSites.txt</c>): under <see cref="PrecisionKind.Original"/> it rounds its
/// argument to binary32 through <see cref="Binary32.ToNearestRepresentable"/>, under
/// <see cref="PrecisionKind.Binary64"/> it returns the argument unchanged, so wrapping an
/// existing subexpression changes no bit of a <c>Binary64</c> run. A value the executable keeps
/// in a register is never passed here: it is a local the caller holds unrounded, and only what
/// is stored is rounded. Every call ends with a <c>// Fortran n</c> comment naming the site or
/// sites it carries.
/// </summary>
internal readonly struct CyclePlaneRounding(PrecisionKind kind)
{
    private readonly bool original = kind == PrecisionKind.Original;

    /// <summary>A REAL*4 store to a named home (generated kinds S, P, X, B<c>n</c> and C<c>n</c>): the home holds the binary32 value.</summary>
    public double Store(double value) => Round(value);

    /// <summary>A compiler temporary the executable spills to a REAL*4 slot (generated kind T), recomputed here at each use.</summary>
    public double Temporary(double value) => Round(value);

    /// <summary>A lone REAL*4 literal binary32 cannot hold exactly: its binary32 value.</summary>
    public double Literal(double value) => Round(value);

    /// <summary>An all-literal REAL*4 subexpression the compiler folds in binary32.</summary>
    public double Fold(double value) => Round(value);

    /// <summary><c>QKS1</c>, stored in REAL*4 at Fortran 718 and read by the plane.</summary>
    public double Input(double value) => Round(value);

    private double Round(double value) => original ? Binary32.ToNearestRepresentable(value) : value;
}
