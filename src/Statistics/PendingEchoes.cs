namespace PropStruct.Statistics;

/// <summary>
/// Every scalar <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> computes for <see cref="SetupEchoes"/>
/// except <c>Dmax</c>, which is not known until the driver has performed the tail draw
/// through <c>Particle.SizeLaw.Sample</c> (API.md, "Setup"). <see cref="Setup.CompleteEchoes"/>
/// combines this with the sampled <c>Dmax</c>.
/// </summary>
internal sealed record PendingEchoes(
    double Dokm, double Doksd, double Ddokmax,
    double TailProbabilityModified, double OxidizerMassFractionEffective);
