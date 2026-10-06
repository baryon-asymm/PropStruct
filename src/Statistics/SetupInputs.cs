namespace PropStruct.Statistics;

/// <summary>
/// Every generated setup-plane member whose role is <c>input</c>
/// (<c>SetupPlane.generated.txt</c>) that this node's own per-cycle processing or a
/// consumer outside the attempt plane needs, computed once by <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/>
/// and handed to every later reader as stored: rounded to binary32 under
/// <see cref="Particle.PrecisionKind.Original"/>, given as read under
/// <see cref="Particle.PrecisionKind.Binary64"/> (root BOOT.md, "Precision
/// kind"; this node's own BOOT.md, "## Setup plane", "Design decision, 2026-09-24").
/// </summary>
/// <remarks>
/// The members that also feed the attempt plane through <c>ModelSetup</c>
/// (<c>CellSize</c>, <c>CategoryStep</c>, <c>Dmin</c>, <c>Ak1</c>-<c>Ak4</c>,
/// <c>Alpha</c>, <c>NnMin</c>, <c>NnMax</c>, <c>PocketCoefficient</c>,
/// <c>BridgeCoefficient</c>) are not duplicated here: <c>ModelSetup</c> already carries
/// them, rounded, and both <c>CycleStatistics</c> and <c>Simulation</c> (which has the
/// same tree access to <c>Particle</c>'s internal surface this node does) read them
/// from there directly, one source per value (BOOT.md, "## Setup plane": "one stored
/// setup feeds every consumer"). Likewise <see cref="SetupTables.MassShare"/> and
/// <see cref="SetupTables.Bounds"/> carry the per-fraction arrays; this record holds
/// only the scalars neither <c>ModelSetup</c> nor <see cref="SetupTables"/> already
/// carries.
/// </remarks>
/// <param name="OxidizerDensity">PLOT1, kg/m^3 (<c>SetupPlane.generated.txt</c>'s <c>plot1</c>).</param>
/// <param name="PropellantDensity">PLOT2, kg/m^3 (<c>plot2</c>).</param>
/// <param name="OxidizerMassFraction">GGG0, the raw menu/formulation value printed as "Gdok =" (<c>ggg0</c>; not <see cref="SetupEchoes.OxidizerMassFractionEffective"/>, GGG, a different, derived value).</param>
/// <param name="MetalMassFraction">GM, Fortran's <c>Gm</c> (<c>gm</c>).</param>
/// <param name="HomogenizedOxidizerFraction">gdokns, menu [11] (<c>gdokns</c>).</param>
/// <param name="EpsDok">eps_dok, menu [4] (<c>eps_dok</c>).</param>
/// <param name="AggregatedOxideFraction">eta, menu [14] (<c>eta</c>).</param>
internal sealed record SetupInputs(
    double OxidizerDensity, double PropellantDensity, double OxidizerMassFraction, double MetalMassFraction,
    double HomogenizedOxidizerFraction, double EpsDok, double AggregatedOxideFraction);
