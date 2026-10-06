namespace PropStruct.Statistics;

/// <summary>
/// The pair <c>Particle.SizeLaw.Sample</c> needs for the tail-probability draw of
/// <c>PARAM</c> (Fortran line 1754), produced by <see cref="FractionLaw.TryTailDraw"/>
/// and consumed by the driver that owns the accelerator (API.md, "Setup"; root BOOT.md,
/// "## Decomposition"). <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/>'s own <c>setup.SizeLaw</c> and
/// <c>setup.FractionCount</c> are <c>Sample</c>'s other two scalar arguments; they are
/// not repeated here.
/// </summary>
/// <param name="X">Sample's own <c>x</c>: <c>Z11(Imax+1) - alfa</c>.</param>
/// <param name="X1">Sample's own <c>x1</c>: <c>(x - Z11(Imax)) / (Z11(Imax+1) - Z11(Imax))</c>.</param>
internal readonly record struct TailDraw(double X, double X1);
