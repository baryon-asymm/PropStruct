using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The order in which the per-cycle plane forms its products, per precision kind (BOOT.md,
/// "## Report"; the <c>order</c> column of <c>CyclePlane.listing.generated.txt</c>, API.md, "##
/// Cycle"). Under <see cref="PrecisionKind.Binary64"/> the cell centre is formed first and
/// <c>**4.</c>, <c>**3.</c>, <c>**2</c> are <see cref="Math.Pow(double, double)"/>, as the port always computed them;
/// under <see cref="PrecisionKind.Original"/> the products are the executable's: the weight
/// multiplies the factors in the order the listing names them, and the integer powers are
/// products. A product with two equal-valued orders (a commutation) is one order here.
/// </summary>
internal readonly struct CyclePlaneOrder(PrecisionKind kind)
{
    private readonly bool original = kind == PrecisionKind.Original;

    /// <summary><c>x·a·b</c>: Original <c>(x·a)·b</c>, Binary64 <c>x·(a·b)</c> (the cell centre <c>a·b</c> first).</summary>
    public double Linear(double x, double a, double b) => original ? x * a * b : x * (a * b);

    /// <summary><c>x·c²</c>: Original <c>x·(c·c)</c>, Binary64 <c>(x·c)·c</c>.</summary>
    public double Quadratic(double x, double c) => original ? x * (c * c) : x * c * c;

    /// <summary><c>c**4.</c>: Original <c>(c·c)·(c·c)</c>, Binary64 <c>Math.Pow(c, 4)</c>.</summary>
    public double Fourth(double c) => original ? c * c * (c * c) : Math.Pow(c, 4);

    /// <summary><c>c**3.</c>: Original <c>c·(c·c)</c>, Binary64 <c>Math.Pow(c, 3)</c>.</summary>
    public double Cube(double c) => original ? c * (c * c) : Math.Pow(c, 3);

    /// <summary><c>x**2</c> as an exponent: Original <c>x·x</c>, Binary64 <c>Math.Pow(x, 2)</c>.</summary>
    public double Square(double x) => original ? x * x : Math.Pow(x, 2);
}
