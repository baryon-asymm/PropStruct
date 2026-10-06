namespace PropStruct.Statistics;

/// <summary>
/// The scalar setup quantities the setup hand-off produces (API.md, "Setup hand-off"),
/// in SI units, the same ones <c>results.m</c> echoes before the first cycle.
/// <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> computes every field but <see cref="Dmax"/> (returned
/// separately as <see cref="PendingEchoes"/>); <see cref="Setup.CompleteEchoes"/> adds
/// <see cref="Dmax"/> once the driver has sampled it and returns the completed record.
/// </summary>
/// <param name="Dokm">DOKM, the analytical mass-medium oxidizer size, m.</param>
/// <param name="Doksd">DOKSD, the analytical standard deviation of the oxidizer size, m^2.</param>
/// <param name="Ddokmax">
/// Ddokmax, the largest fraction upper bound, m. Under <see cref="PropStruct.Particle.PrecisionKind.Original"/> this is the
/// same stored value <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/>'s own rounded <c>SetupTables.Bounds</c> already holds for
/// that fraction (Fortran line 261 stores <c>DDOK(2*kilo)</c> to REAL*4 before line 271 copies it into
/// <c>Ddokmax</c>) -- not <see cref="Setup.Sizes"/>'s own <c>ddokmax</c> out-parameter, which the root's
/// array-size exception computes unconditionally, without that second store-rounding, for <c>Ndok</c>'s own
/// numerator only (found by review 2026-09-24, BOOT.md, "## Setup plane").
/// </param>
/// <param name="Dmax">Dmax, the largest accepted base-particle size from PARAM's tail-probability draw, m.</param>
/// <param name="TailProbabilityModified">alfa after PARAM (Fortran line 377): the caller's tail probability, reduced by every fraction PARAM excluded.</param>
/// <param name="OxidizerMassFractionEffective">GGG = GGG0 - gdokns*GGG0 (Fortran line 376).</param>
internal sealed record SetupEchoes(
    double Dokm, double Doksd, double Ddokmax, double Dmax,
    double TailProbabilityModified, double OxidizerMassFractionEffective);
