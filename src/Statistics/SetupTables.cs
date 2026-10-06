namespace PropStruct.Statistics;

/// <summary>
/// The fraction-law host arrays <see cref="Setup.Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> produces (API.md, "Setup"):
/// the driver builds <c>Particle.FractionTable</c>'s views from these.
/// </summary>
/// <param name="Bounds">DDOK: 2*NMM, the lower and upper bound of each fraction, m, pairs (2i, 2i+1).</param>
/// <param name="Cumulative">Z11: NMM + 1, the cumulative fraction weights (Fortran 1-based Z1(1..NMM+1)).</param>
/// <param name="Share">ZX: NMM, the normalized fraction weights.</param>
/// <param name="PocketForming">SFR: NMM, 1 where the fraction forms pockets, 0 where it does not.</param>
/// <param name="Zss">
/// ZSS, <c>FractionLaw.Build</c>'s own normalizing sum (PARAM's <c>ZS</c>, line 378's own
/// factor): under <see cref="Particle.PrecisionKind.Original"/> it is the
/// per-addition-rounded binary32 sum (BOOT.md, "## Setup plane"); under
/// <see cref="Particle.PrecisionKind.Binary64"/> it is the plain <c>double</c>
/// sum this node always computed. Not itself printed in <c>results.m</c> (root BOOT.md,
/// "Precision kind": <c>ZSS</c> feeds both the fraction thresholds and λ but is not
/// itself one of the setup plane's printed fields), carried here so a caller — the
/// fixture-equality criterion included — can check it directly rather than back-deriving
/// it from λ through the very division it feeds.
/// </param>
/// <param name="MassShare">
/// GDOK: NMM, the raw per-fraction mass share as read (<c>SetupPlane.generated.txt</c>'s
/// <c>gdok</c>, role <c>input</c>) — not <see cref="Share"/> (ZX, the size-law-normalized
/// weight, a different value): the category matrix (<c>Categories.MergeAndDescribe</c> is
/// not one of its readers, but <c>CycleStatistics</c>' own "Matrix" group is, BOOT.md,
/// "## Line map", 1001-1016) reads this array by fraction index the same way
/// <see cref="PocketForming"/> already is. Added 2026-09-24 (BOOT.md, "## Setup plane",
/// "Design decision, 2026-09-24"): before this date <c>CycleStatistics</c> read
/// <c>formulation.Fractions[i].MassShare</c> directly, a second, un-rounded copy of the
/// same REAL*4 value this array already carries rounded.
/// </param>
internal sealed record SetupTables(
    double[] Bounds, double[] Cumulative, double[] Share, byte[] PocketForming, double Zss, double[] MassShare);
