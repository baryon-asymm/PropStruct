using System.Collections.Immutable;
using PropStruct.Execution;

namespace PropStruct.Simulation;

/// <summary>
/// Every quantity <c>results.m</c> prints, under the field names of
/// <c>src/Statistics/BOOT.md</c>, "## Report" (<c>Statistics.CycleReport</c>, the last cycle's own), plus
/// the per-cycle convergence series and the run diagnostics (API.md, "## Result"; BOOT.md, "Design
/// decisions (2026-09-18)", "The result mirrors CycleReport"). "Every field" is checked by
/// <c>tests/Simulation.Tests</c>' own reflection test against <c>CycleReport</c>, not by a typed list: the
/// mapping from a <c>CycleReport</c> field to the member record below that carries it is this file's own,
/// recorded in <c>src/Simulation/BOOT.md</c>, "## Result field mapping".
/// </summary>
public sealed record SimulationResult(
    RunHeader Header, Counters Counters, GeneratorAccuracy GeneratorAccuracy,
    ParticleSizes ParticleSizes, LocalStructure LocalStructure, Pockets Pockets,
    MassFractions MassFractions, Agglomerates Agglomerates, Histograms Histograms,
    Convergence? Convergence, RunDiagnostics Diagnostics, StoredSetup StoredSetup);

/// <summary>
/// Every setup-plane value <c>Output</c> prints or scales by, as the run stored it:
/// rounded to binary32 under <see cref="PrecisionKind.Original"/>, given as read under
/// <see cref="PrecisionKind.Binary64"/> (root BOOT.md, "Precision kind"; this node's own
/// API.md, "## Stored setup"; <c>src/Statistics/SetupPlane.generated.txt</c> names the generated
/// members this mirrors). <c>Output</c> reads these instead of <c>Formulation</c> and
/// <c>ModelParameters</c> for any of them, so under <c>Original</c> a value has one
/// value per run, the one the model used.
/// </summary>
public sealed record StoredSetup(
    double CellSize, double CategoryStep, double Dmin,
    double OxidizerDensity, double PropellantDensity, double OxidizerMassFraction, double MetalMassFraction,
    double Alpha, double NnMin, double NnMax, double PocketCoefficient, double BridgeCoefficient,
    double HomogenizedOxidizerFraction, double EpsDok,
    ImmutableArray<double> FractionMassShares, ImmutableArray<double> FractionBounds);

/// <summary>The formulation identity and the setup echoes of <c>CycleReport</c>.</summary>
public sealed record RunHeader(
    string FormulationName, int Cycles, int ParticlesPerCycle,
    double Dokm, double Doksd, double Ddokmax, double Dmax, double TailProbabilityModified, double Ggg,
    ImmutableArray<double> Zx);

/// <summary>Counters as they stand at the end of the run (<c>CycleReport</c>, "Counters as they stand").</summary>
public sealed record Counters(
    long Qkss, long Nfx, long Nfy, long Nfz, long Nfq, long Nfw, ImmutableArray<long> Conditions,
    long IbridgeTotal, double JammedTotal, double NnTotal);

/// <summary>Generator accuracies and their warning flags (<c>CycleReport</c>, "Accuracies").</summary>
public sealed record GeneratorAccuracy(
    double Eps1, double Eps2, double Eps3, double Eps4, double Eps5, double Eps6, double Eps7,
    double Epsx1, double Epsx2, double Epsx3, ImmutableArray<double> Epsalldok,
    double Epsy, double Epsmd4, double Epsmd3, bool OxidizerAccuracyWarning, bool PocketAccuracyWarning);

/// <summary>Oxidizer (base) particle size moments (<c>CycleReport</c>, "Sizes", the oxidizer-size scalars).</summary>
public sealed record ParticleSizes(double Dok43b, double Dok43s, double Alldok43, double Alldok432);

/// <summary>The local matrix structure around a base particle (<c>CycleReport</c>, "Matrix").</summary>
public sealed record LocalStructure(double Gdokleft, double Vdokleft, double Plotsmdok, double Plotsm, double Mp);

/// <summary>Pocket-size moments and the conditional size distribution by pocket category (<c>CycleReport</c>, "Sizes" (pocket scalars) and "categories").</summary>
public sealed record Pockets(
    double Dp43, double D432, double Sdevp43, double Dqkarm,
    double Dkarm43Cor, double Sdevp43Cor, double DqkarmCor, double Dfmk432, double Sdevp243,
    double DpMax, double DpMaxCor,
    int DpRow, ImmutableArray<double> Dpockets, ImmutableArray<double> Dokp43, ImmutableArray<double> Qdokkarm,
    ImmutableArray<ImmutableArray<double>> Qdokso);

/// <summary>The three variants of the pocket mass fraction, cycles &gt;= 1 (<c>CycleReport</c>, "Mass fractions").</summary>
public sealed record MassFractions(double DolM1, double DolM2, double DolM3);

/// <summary>The bridge/agglomerate coefficient quantities (<c>CycleReport</c>, "Sizes" (bridge scalars) and their distributions' support markers).</summary>
public sealed record Agglomerates(
    double Dqmkm1, double Dqmkm2, double Qmcoef, int CoefNmax, int Qmkm1Nmax, int Qmkm2Nmax);

/// <summary>Every distribution as a fraction per cell (<c>CycleReport</c>, "Distributions as fractions per cell").</summary>
public sealed record Histograms(
    ImmutableArray<double> Allvdokso, ImmutableArray<double> Vkso, ImmutableArray<double> Qks1,
    ImmutableArray<double> FmkarmCorNormalized, ImmutableArray<double> Fmkarm2Normalized,
    ImmutableArray<double> FqkarmCorNormalized, ImmutableArray<double> Qmkm1Normalized,
    ImmutableArray<double> Qmkm2Normalized, ImmutableArray<double> CoefNormalized,
    ImmutableArray<double> Pdoksmall);

/// <summary>
/// The per-cycle convergence series the original prints when <c>KXX &gt; 1</c> (BOOT.md, "Design decisions
/// (2026-09-18)", "The result mirrors CycleReport"). Null when the run has only one post-warm-up cycle
/// (<c>KXX == 1</c>): a series of one point has nothing to converge against. Each array has one entry per
/// completed post-warm-up cycle, in cycle order (index 0 is cycle 1).
/// </summary>
public sealed record Convergence(
    ImmutableArray<double> ConvergenceEpsy, ImmutableArray<double> ConvergenceEpsmd3, ImmutableArray<double> ConvergenceEpsmd4,
    ImmutableArray<double> ConvergenceAlldok43, ImmutableArray<double> ConvergenceAlldoksd, ImmutableArray<double> ConvergenceDolM2);

/// <summary>
/// Run bookkeeping that depends on how the run was driven, not on the model (BOOT.md, "Elapsed time is the
/// only field that depends on wall time"). <see cref="Precision"/> sits beside <see cref="Streams"/>: both
/// are options of the run recorded in its result unchanged (root BOOT.md, "Two stream layouts", "Precision
/// kind is an option of every run"). <see cref="AttemptsPerLaunch"/> is the *effective* per-launch attempt
/// budget the run used, not merely the option passed: 1 in reference mode (which mirrors
/// <see cref="BatchSize"/>'s own reference-mode value), <c>SimulationOptions.AttemptsPerLaunch</c> for
/// batched and continued-streams runs (BOOT.md, "## Budget selection rule (2026-09-28)").
/// </summary>
public sealed record RunDiagnostics(
    ExecutionMode Mode, int BatchSize, int AttemptsPerLaunch, AcceleratorInfo Accelerator, StreamLayout Streams,
    PrecisionKind Precision, ulong Seed, bool ContinuedStreams, long TotalAttempts, long TotalLaunches, TimeSpan Elapsed);
