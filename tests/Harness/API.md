# API.md — Harness

Namespace `PropStruct.Tests.Harness`. For test projects only.

`StatisticalCriterion` was `partial` from 2026-09-25 (audit finding R3) to 2026-09-26,
its ~3000 lines split across six files by concern but still one class, one public
surface. On 2026-09-26 that split was carried one step further: every one of its former
internal members that named a distinct concern moved out into its own named,
`internal static` class, and the `partial` class collapsed back into one non-partial
`StatisticalCriterion.cs` holding only the five public entry points, the three internal
calibration-curve seams, and the class-wide `Alpha` constant. `tests/Harness/BOOT.md`
has the invariants; this document has the decomposition, class by class, grouped as the
data flows:

- **Contract**: `StatisticalCriterion` itself (below) — the only class this document's
  ✅ block still nests members under.
- **Comparison pipeline**: `ReferenceComparison` (loads `Compare`'s inputs once and
  dispatches each printed quantity name to its rule), `CanonicalAxisCells` (the
  canonical `fqdokkarm` row family and the whole-array category axis),
  `OrdinaryQuantityCells` (every other quantity, including the mass-family ceiling and
  heavy-tail bracket), `TailRowMeanComparison` and `AdaptiveIndexMatchedComparison`
  (the two comparisons of "## Tail coverage"; the latter's own two per-run adaptive-tail
  scalars, `AdaptiveRowCountOf`/`LastBoundaryLogOf`, are shared with `SetComparison`
  below, not reimplemented), `TwoSampleComparison` (the parallel two-sample branch
  behind `TwoSampleBias`/`TwoSampleBiasOfSets`, and, since 2026-09-27, `SetComparison`'s
  own `OfSets` call at the exact constant-cell bound — `## Set comparison` below), and
  `FamilyWiseReport` (the family-wise `Alpha / m` budget and the reduction to a
  `CriterionReport`).
- **Set comparison** (`CompareSets`, this node's BOOT.md, "## Set comparison"):
  `SetComparison` (one call: loads the replica set, puts every run's canonical-axis
  families on the reference axis and adds the adaptive-tail summaries, zero-fills the
  rest, runs the three `OfSets` comparisons, builds the report) and
  `CanonicalAxisSetCells` (one run's own augmented cell dictionary — see below).
- **Cell rules**: `PendingCell` (one cell with the replica statistics its threshold is
  built from, `From` the factory that used to be `AddCell`), `CellVerdict` and
  `CalibrationRule` (the verdict contract the calibration curve reads), `CellEvaluator`
  (which of the three terms governs a cell's threshold, and did it fail),
  `CellEligibility` (Decision VI's own per-rule eligibility test), `CountFloor` (the
  count predictive's half-width in value units), `MassFamilyRule` (the four
  mass-weighted families' own step, ceiling, sibling and deterministic bracket),
  `QuantityFamilies` (which printed family a quantity name belongs to), `CategoryAxis`
  (the fixed-width prefix and each source's match length against the reference, for
  both a `ResultCell[]`-keyed candidate/replica pool and, since 2026-09-27, a plain
  `double[]`-keyed list of runs — `PrefixMatchLengths`, `SetComparison`'s own seam),
  `PrintResolution` (the resolution a value prints at under an array's own format),
  `RunQuantum` (one run's own count quantum and the near-multiple test against it),
  `CountReconstruction` (printed values back to integer counts), `DispersionEstimator`
  (the family's pooled Pearson dispersion `phi` and the route it decides), and
  `CellRho` (the robust per-cell intraclass correlation and its pooled median).
- **Fixtures and numerics**: `FixtureReplicas` (the replica/reference files of one
  formulation, parsed, `CountOf` the one place the replica-count lookup lives),
  `ExclusionRules` (`exclusions.json`'s own rule vocabulary), `NegativeBinomialPredictive`
  (the count predictive the floor is built from), `BetaBinomialPredictive` (the
  over-dispersed count predictive, `rho = 0` its own binomial case), `BinomialBand`
  (the highest-density region of `Binomial(n, p)`), `PredictiveTail` (the two-sided
  predictive probability of an observed count), and `SampleSpread` (mean and sample
  standard deviation, with the constant-set guard).

Every one of the classes above is `internal static`, none is named in a checked ✅
code block unless `Harness.Tests`, `RateTableTool` or `DispersionTool` calls across the
assembly boundary into it — the same rule the previous split's own opening paragraph
stated, restated here rather than retold per class: a class the node's own internal
plumbing never crosses is implementation, not contract, and this document names it in
prose so a reader has the map, without pinning a signature no outside caller depends on.

`StatisticalCriterion`'s `Alpha` is internal, surface internal to the tree
(`InternalsVisibleTo` for `Harness.Tests` only, this node's `.csproj`) — listed below
because `Harness.Tests`' own sensitivity sweeps read it, not because it is part of the
outward contract. The same reasoning covers every internal member listed under the ✅
block below: `CategoryAxis.FixedWidthPrefixLength`, `RunQuantum.QuantumEstimate`/
`TryInfer`, `CountReconstruction.ArrayTotals`/`PerCellCounts`,
`DispersionEstimator.DispersionEstimate`/`Estimate`, `CellRho.CellRhoEstimate`/
`Compute`/`Quantile`, `NegativeBinomialPredictive.Interval`/`CdfAtMost`,
`BetaBinomialPredictive.Pmf`/`Interval`/`CdfAtMost`, `BinomialBand.HighestDensityRegion`,
`PredictiveTail.TwoSided`, and the top-level `CalibrationRule`/`CellVerdict` the
calibration curve reads — called by `DispersionDiagnosticTests`
(`HISTORY.md#fable-5-1-decision-iv`), `BetaBinomialIntervalTests`
(`HISTORY.md#fable-5-1-decision-v`), `CountTailProbabilityTests`/`DilutionDiagnosticTests`
(`HISTORY.md#decision-vi-full-reasoning`), `IntervalWidthDiagnosticTests`
(`HISTORY.md#decision-vii-full-reasoning`) and
`CalibrationCurveTests`/`CentreDiagnosticTests`/`CountFloorBoundaryTests`/
`RegionGuardDiagnosticTests` (`HISTORY.md#fable-5-1-decision-ii`), not because
any of them are part of the outward contract.
`DispersionTable.Compute` is internal the same way, `InternalsVisibleTo` for
`Harness.Tests` and `PropStruct.DispersionTool` (this node's `.csproj`): the one
implementation of the dispersion table, called by
`Harness.Tests/DispersionApprovedTests.MatchesTheCommittedTable` (the check) and
`tests/DispersionTool` (the manual generator) — added 2026-09-25, moved here from
`tests/Harness.Tests/DispersionApprovedTests.cs`.

## Results parser and statistical comparison ✅

```csharp
public readonly record struct ResultCell(double Value, double Resolution, bool IsIntegerPrinted);

public static class ResultsMFile
{
    public static IReadOnlyDictionary<string, double[]> Parse(string path);
    public static IReadOnlyDictionary<string, ResultCell[]> ParseCells(string path);
}

public static class StudentDistribution
{
    // The two-sided critical value t such that P(|T| > t) = twoSidedAlpha, df degrees of freedom.
    public static double TwoSidedQuantile(int df, double twoSidedAlpha);
}

public enum ReplicaKind
{
    Lagged,
    Independent,
    Gsv3,
}

public static class StatisticalCriterion
{
    internal const double Alpha = 1e-3;

    // excludeReplicaOrdinal (1-based, the replica file's own number) drops one replica from the R loaded before
    // comparing; null (the default, and the only value before 2026-09-18) loads all R, unchanged from before
    // this parameter existed. HISTORY.md#tail-coverage-2026-09-18, step 1, "Blind calibration".
    public static CriterionReport Compare(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal = null);
    public static IReadOnlyList<TwoSampleCell> TwoSampleBias(string formulation, ReplicaKind first, ReplicaKind second);

    // HISTORY.md#tail-coverage-2026-09-18, steps 2 and 3: two comparisons kept beside Compare's
    // own canonical-axis rule, not a replacement of it — neither changes Compare's own Compared/Excluded counts,
    // and each is its own family-wise-corrected report.
    public static CriterionReport CompareTailRowMean(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal = null);
    public static CriterionReport CompareAdaptiveIndexMatched(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, int? excludeReplicaOrdinal = null);

    // TwoSampleBias's own body, taking both replica sets already loaded instead of loading them itself from
    // tests/Fixtures. Public since 2026-09-23 (this node's BOOT.md, "Two-sample bias"): a caller with two sets
    // of runs that never touched a fixture file — tests/Simulation.Tests's own reference-vs-batched comparison
    // (Simulation.Tests/BOOT.md, link 3) — has a seam here, the same one this node's own oracle-mutation tests
    // use to substitute one perturbed member of a set (`HISTORY.md#oracle-mutation-2026-09-19`). No
    // canonical-axis or count-floor handling, exactly as TwoSampleBias itself has none (see its own note
    // above): a caller whose sets include a category-indexed quantity must account for that itself.
    public static IReadOnlyList<TwoSampleCell> TwoSampleBiasOfSets(
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> firstSet, IReadOnlyList<IReadOnlyDictionary<string, double[]>> secondSet);

    // this node's BOOT.md, "## Set comparison" (decided 2026-09-27): whether a SET of candidates (a
    // formulation's own port runs, passed in seed order) reproduces the formulation's own replica set of `kind` —
    // distinct from Compare's own single-candidate question. SetComparison is the one implementation;
    // CanonicalAxisSetCells puts the canonical-axis families on the reference's own axis first.
    public static SetCriterionReport CompareSets(
        string formulation, IReadOnlyList<IReadOnlyDictionary<string, double[]>> candidates, ReplicaKind kind);

    // internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): the calibration curve's own
    // seam into each of the three reports, returning every cell's verdict at a raw per-cell alpha instead of a
    // CriterionReport at the family-wise alpha / m. Each shares its report's own cell-building with the public
    // method (Compare/CompareTailRowMean/CompareAdaptiveIndexMatched respectively) — never a second
    // implementation of it.
    internal static IReadOnlyList<CellVerdict> CompareCellVerdicts(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, double alpha, int? excludeReplicaOrdinal = null);
    internal static IReadOnlyList<CellVerdict> CompareTailRowMeanCellVerdicts(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, double alpha, int? excludeReplicaOrdinal = null);
    internal static IReadOnlyList<CellVerdict> CompareAdaptiveIndexMatchedCellVerdicts(
        string formulation, IReadOnlyDictionary<string, double[]> candidate, ReplicaKind kind, double alpha, int? excludeReplicaOrdinal = null);
}

public sealed record CriterionReport(int Compared, int Excluded, IReadOnlyList<CriterionFailure> Failures);
public sealed record CriterionFailure(string Quantity, int Index, double Value, double Mean, double Threshold, double? RegionLow = null, double? RegionHigh = null);
public sealed record TwoSampleCell(string Quantity, int Index, double MeanFirst, double MeanSecond, double T, int DegreesOfFreedom, bool Differs);

// this node's BOOT.md, "## Set comparison": CompareSets's own result. Compared/Excluded are the call's own m and
// the index-cells dropped before comparison; Differences reuses TwoSampleCell (MeanFirst the candidates,
// MeanSecond the replicas); CandidateHalvesDifferences/ReplicaHalvesDifferences are the report's own negative
// controls (candidates split by position/seed parity, replicas by ordinal parity) — expected empty.
public sealed record SetCriterionReport(int Compared, int Excluded, IReadOnlyList<TwoSampleCell> Differences,
    IReadOnlyList<TwoSampleCell> CandidateHalvesDifferences, IReadOnlyList<TwoSampleCell> ReplicaHalvesDifferences);

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): which of the three verdict
// mechanisms decided a cell, and one cell's own verdict at a given alpha — this node's BOOT.md, "Fable 5.1
// decision II: the calibration curve (2026-09-20)" (`HISTORY.md#fable-5-1-decision-ii`). Both are top-level types since the 2026-09-26 decomposition
// (they no longer nest inside StatisticalCriterion): CellEvaluator builds them, CellEligibility decides Eligible.
// Eligible and TailProbability: `HISTORY.md#decision-vi-full-reasoning` — whether some
// printable value of the cell's own quantity lies outside its own interval at this level (the calibration
// curve's own denominator, never touching Compare's own m), and the exact two-sided predictive probability of
// the observed value (Count cells only). StudentTerm/CountFloor/StaticFloor/RegionLow/RegionHigh/
// ResolutionGuardCounts/Quantum/ObservedCount/ImplausibleCount: `HISTORY.md#decision-ix-full-reasoning`/
// `HISTORY.md#decision-xi-withdrawn-full-reasoning`/"Count floor: the boundary is judged around its own predictive mean" — the three terms the
// threshold was built from, the count-governed region in value units, the print-resolution guard in count
// units, and the candidate's own quantum/reconstructed count/implausible-count flag. PooledCount: the
// contributing replicas' pooled reconstructed count at the cell (`HISTORY.md#b2a-amended-2026-10-02`), null
// without counts.
internal enum CalibrationRule { Student, Count, Mass }
internal readonly record struct CellVerdict(
    string Name, int Index, double CandidateValue, double Mean, double Threshold, bool Failed, CalibrationRule Rule,
    bool Degenerate, double? AttainedAlpha, bool Eligible, double? TailProbability,
    double StudentTerm, double CountFloor, double StaticFloor,
    double? RegionLow, double? RegionHigh, double? ResolutionGuardCounts,
    double? Quantum, double? ObservedCount, bool ImplausibleCount, double? PooledCount);

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): the fixed-width prefix length of
// Dkarmcat (this node's BOOT.md, ## Invariants, "Canonical category axis"), reused by DispersionTable/
// diagnostic tests that build their own eligible-replica sets without going through ReferenceComparison.
internal static class CategoryAxis
{
    internal static int FixedWidthPrefixLength(double[] dkarmcat);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): the per-run quantum inference
// Compare itself uses, reused by DispersionDiagnosticTests (`HISTORY.md#fable-5-1-decision-iv`) to
// reconstruct a run's own integer counts without a second implementation of the formula.
internal static class RunQuantum
{
    // Resolution is res(min) / k (B2a); Identified: the first fitting k's chance product is at most
    // StatisticalCriterion.Alpha / K (`HISTORY.md#b2a-amended-2026-10-02`). The estimate is returned either way.
    internal readonly record struct QuantumEstimate(double Quantum, double Resolution, bool Identified);
    internal static bool TryInfer(IReadOnlyList<ResultCell> ownArray, out QuantumEstimate estimate);

    // E1 (2026-09-27, `HISTORY.md#e1-mass-bracket-feasible-interval`): the feasible count interval a printed
    // value is consistent with under a run's own quantum, reused by MassFamilyRule.FeasibleMassQuantum/TryBracket
    // instead of a second implementation of the near-integer-multiple inequality.
    internal static (double Low, double High) CountBounds(double magnitude, double resolution, QuantumEstimate quantum);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): `HISTORY.md#fable-5-1-decision-v`
// — each replica's own reconstructed array/row total, and the per-cell (k_i, n_i) reconstruction
// DispersionEstimator.Estimate needs internally, both shared by ReferenceComparison's own wiring and by
// DispersionDiagnosticTests/IntervalWidthDiagnosticTests rather than reconstructed a second or third time.
internal static class CountReconstruction
{
    internal static double?[] ArrayTotals(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string arrayName);
    internal static (List<Dictionary<int, double>> PerReplicaCounts, List<double> Totals) PerCellCounts(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string arrayName,
        IReadOnlyList<double?> replicaTotals);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): the promoted conditional
// dispersion diagnostic (`HISTORY.md#fable-5-1-decision-v`). RhoHatSpread:
// `HISTORY.md#decision-vii-full-reasoning` — half the interquartile range of the qualifying per-cell rho estimates the robust estimator
// pools by median; null exactly when that estimator found no qualifying cell and RhoHat fell back to the older
// (phi - 1) / (n_bar - 1) formula.
internal static class DispersionEstimator
{
    internal readonly record struct DispersionEstimate(int Runs, int Groups, double NBar, double Phi, double RhoHat, double? RhoHatSpread);
    internal static DispersionEstimate? Estimate(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, string arrayName,
        IReadOnlyList<double?> replicaTotals);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above):
// `HISTORY.md#decision-vii-full-reasoning`/`HISTORY.md#decision-viii-full-reasoning`
// — one cell's own (p_hat, rho) reconstruction and the family's own
// pooled median, exercised directly by IntervalWidthDiagnosticTests' own bootstrap check
// (DecisionVIIIBootstrapCheckFqDokKarmRhoCellByDecile) rather than recomputed a second time.
internal static class CellRho
{
    internal readonly record struct CellRhoEstimate(double NBar, double PHat, double RhoCell);
    internal static CellRhoEstimate? Compute(IReadOnlyList<double> ks, IReadOnlyList<double> ns);
    internal static double Quantile(List<double> sortedValues, double q);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): the negative-binomial
// predictive interval Compare's own count floor is built from (HISTORY.md#criterion-revision-2026-09-17, step 3),
// exposed for CountTailProbabilityTests' own cross-check against the beta-binomial's point-CDF primitives
// (`HISTORY.md#decision-vi-full-reasoning`).
internal static class NegativeBinomialPredictive
{
    internal static (double Mean, double Low, double High, double AttainedAlpha) Interval(
        double totalCount, int replicaCount, double alpha);
    internal static (double CdfAtMost, double PmfAt) CdfAtMost(double k, double totalCount, int replicaCount);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above):
// `HISTORY.md#fable-5-1-decision-v`: the beta-binomial's own two-sided, equal-tailed predictive interval and attained level,
// parameterised by the cell's own probability p and the family's own intraclass correlation rho (rho == 0 is
// the ordinary binomial, computed as its own special case). Not yet called by Compare or any of the reports
// above (`HISTORY.md#fable-5-1-decision-v`, "What this leaves undone"); exercised today only by
// BetaBinomialIntervalTests' own independent verification. Pmf: the full-support pmf array Interval's own
// search consumes, extracted so BetaBinomialIntervalTests.PmfArrayOwnReconstructedMeanMatchesNTimesP shares one
// construction with it, never a second one.
internal static class BetaBinomialPredictive
{
    internal static double[] Pmf(long n, double p, double rho);
    internal static (double Mean, double Low, double High, double AttainedAlpha) Interval(
        long n, double p, double rho, double alpha);
    internal static (double CdfAtMost, double PmfAt) CdfAtMost(long k, long n, double p, double rho);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above): the calibration curve's own
// binomial highest-density region (`HISTORY.md#fable-5-1-decision-ii`), reusing StatisticalCriterion's
// own Alpha as that region's own confidence rather than choosing a second significance level.
internal static class BinomialBand
{
    internal static (long Low, long High) HighestDensityRegion(long n, double p, double confidenceAlpha);
}

// internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests only (see above):
// `HISTORY.md#decision-vi-full-reasoning`, item 5 — the exact two-sided predictive tail probability of an observed
// count (2 * min(P(X <= k), P(X >= k)), capped at 1), built from either count predictive's own CdfAtMost. Needed
// for the dilution-vs-mis-specification diagnostic (DilutionDiagnosticTests), not called by Compare itself.
internal static class PredictiveTail
{
    internal static double TwoSided(double cdfAtMost, double pmfAt);
}
```

`ResultsMFile`'s own name scheme for a comment-derived or indexed quantity is documented
on `ResultsMFile` and `ResultsMFile.LabelledEchoes` themselves (their own XML docs), not
repeated here. `StatisticalCriterion`'s exclusion rule vocabulary, canonical category
axis and count-like floor are this node's BOOT.md, ## Invariants — not repeated here
because they are implementation behaviour a caller observes through the dictionary keys
and the report it gets back, not a second contract. `CompareTailRowMean`'s and
`CompareAdaptiveIndexMatched`'s own quantity names (`TailRowMean`,
`Dkarmcat@adaptive`/`dokkarm43@adaptive`/`dokkarm10@adaptive`, `AdaptiveRowCount`,
`AdaptiveLastBoundaryLog`) and rules are `HISTORY.md#tail-coverage-2026-09-18`, for the
same reason. `CompareAdaptiveIndexMatched` scores an adaptive row only up to the
shortest of the candidate's and the contributing replicas' own adaptive length (the
common range, not their union); a row past that length counts toward the report's own
`Excluded`, never toward a failure (`HISTORY.md#e2-adaptive-common-range`).

## Repository paths and bit snapshots ✅

```csharp
public static class RepositoryPaths
{
    public static string Root { get; }                                 // the directory holding AGENTS.md, found from this source file
    public static string Resolve(params string[] segments);            // a path relative to Root
}

public static class BitSnapshot
{
    // A SHA-256 of the little-endian bits of `values` checked against the case `name` of `family` in
    // Snapshots/<family>.approved.txt beside the CALLER's own source file (an optional trailing
    // [CallerFilePath] parameter, not part of the call site). Throws InvalidOperationException, naming the
    // case and the received file, when the case is missing or its hash has moved; the message also names the
    // approved file's platform line and the current one (below).
    public static void Verify(string family, string name, ReadOnlySpan<double> values);
}
```

Added 2026-10-02 (audit 2026-10-02, item 5): every received file, and every approved
file that carries one, opens with one platform line, so that a moved hash can be told
from a moved platform. `Math.Log` and `Math.Pow` come from the C runtime, whose x64
build on Windows picks FMA3 implementations by CPU at startup (`_set_FMA3_enable`), and
glibc differs altogether; no hash of raw `double` bits is portable across them.

```text
# platform: <OS description>; <framework description>; <process architecture>; ucrtbase <FileVersion, n/a off Windows>; fma3 <Fma.IsSupported>
```

The line is computed once per process by `BitSnapshot` itself (`RuntimeInformation`,
the loaded `ucrtbase.dll`'s `FileVersion`, `Fma.IsSupported`) and is information, never
a gate: a case whose hash matches passes whatever the approved file's own `# platform:`
line says, and a file with no such line passes the same way. The failure message of a
missing or moved case names the approved file's `# platform:` line (or "none recorded")
and the current one, and, when they differ, says that this may be a platform move,
re-approved only in a commit naming both platforms and never folded into a code change.
The check stays red; nothing here skips it. The line is the first line of the approved
file that begins `# platform:`; every hash line stays as it was.
Proven red and right 2026-10-02 in `tests/Particle.Tests/BOOT.md`, "## Acceptance
criteria", last row.

## Running a Python script ✅

```csharp
public sealed record PythonRun(int ExitCode, string StandardOutput, string StandardError);

public static class PythonScript
{
    // `python -X utf8 <script> <arguments>` on the PATH's python, no shell, both streams redirected and read in
    // full, no timeout; returns whatever the exit code was. `script` is relative to `workingDirectory` or absolute.
    // Throws InvalidOperationException when the interpreter does not start.
    public static PythonRun Run(string workingDirectory, string script, IReadOnlyList<string> arguments);

    // Run from the repository root (`script` relative to it); throws InvalidOperationException, which fails the
    // calling test as BitSnapshot.Verify does, unless the exit code is 0. The message is
    // "<script file name> <arguments> exited <code>.\nstdout: ...\nstderr: ...".
    public static void RequireSuccess(string script, params string[] arguments);
}
```

Added 2026-10-02, replacing the private copies in six test classes of `Fixtures.Tests`,
`Output.Tests`, `Particle.Tests`, `Random.Tests` and `Statistics.Tests`. `Random.Tests`
alone needs the output, not an assertion: it works in the script's own directory and turns a non-zero
exit into its own `InvalidOperationException`, so it calls `Run`. The file is excluded
from the rate-table digest in both lists (`tests/Fixtures/BOOT.md`, "## Rate table"):
no comparison reaches it. It throws and does not use `Xunit.Assert`: a reference to
`xunit` would make this node's surface a test assembly's, which `Protocol.Tests` leaves
out of `PublicSurface.approved.txt`.

## CPU accelerator host ✅

```csharp
public sealed class CpuHost : IDisposable   // one ILGPU context, one CPU accelerator; never CUDA
{
    public CpuHost();
    public Context Context { get; }
    public Accelerator Accelerator { get; }
    public void Dispose();
}
```

## Deterministic test-only randomness ✅

```csharp
public sealed class SplitMix64   // Vigna's SplitMix64; not the port's own GSV=2 generator, no reproduction claim
{
    public SplitMix64(long seed);
    public ulong NextUInt64();
    public double NextDouble();                                       // [0, 1), same range as System.Random.NextDouble
    public int Next(int maxExclusive);                                 // [0, maxExclusive), same contract as System.Random.Next(int)
    public int Next(int minInclusive, int maxExclusive);                // same contract as System.Random.Next(int, int)
    public long NextInt64(long minInclusive, long maxExclusive);        // same contract as System.Random.NextInt64(long, long)
    public void NextBytes(Span<byte> buffer);                           // same contract as System.Random.NextBytes(Span<byte>)
}
```

Added 2026-09-24 (CA1707/CA1861/CA5394 analyzer pass, root BOOT.md Constraints): `System.Random` is flagged by
CA5394 as an insecure generator even when seeded for reproducibility, the only property any test call site ever
needed. Replaces every seeded `System.Random` use across the test tree; every seed was already a fixed,
written-down integer (root BOOT.md Taboos: "no clock-seeded generator"), so this changes no test's reproducibility
property.

## Dispersion table ✅

```csharp
internal static class DispersionTable
{
    // internal, InternalsVisibleTo PropStruct.Tests.Harness.Tests and PropStruct.DispersionTool (see above): the
    // one implementation of the dispersion table (tests/Fixtures/API.md, "## Dispersion table"), built on
    // CountReconstruction.ArrayTotals/DispersionEstimator.Estimate.
    public static List<string> Compute();
}
```

Added 2026-09-25, moved here from `tests/Harness.Tests/DispersionApprovedTests.cs`'s own private
`ComputeTable`/`AppendLine` (AGENTS.md §13: a permanently skipped `[Fact(Skip = ...)]` generator is forbidden,
so the manual regeneration step moved to the new node `tests/DispersionTool`, and this is now the one
implementation both that node and `Harness.Tests`' own check call).

## Delivery additions ✅

Stage S4 of the delivery (root `BOOT.md`, `## Delivery`) added four public types: the
one cut of the time line that `results.m` comparisons need, which replaced the copies in
`Cli.Tests`, `Simulation.Tests` and `RateTableTool` and is the one `.github/scripts`'s
`check_packages.py` compares through; the switch that turns a CUDA fact's early return
into a failure in the release's GPU job; and the text check that finds those facts.

```csharp
public static class ResultsMTimeLine
{
    // The bytes without the first line containing "Calculation time", its line
    // terminator included, nothing else changed; throws InvalidOperationException
    // when no line contains the marker.
    public static byte[] Remove(byte[] resultsM);
}

public static class CudaRequirement
{
    public const string VariableName = "PROPSTRUCT_REQUIRE_CUDA";
    public static bool IsRequired { get; }        // the variable is set to "1"
    // For a fact that returns when no CUDA accelerator is available: throws
    // InvalidOperationException naming `reason` when IsRequired, returns otherwise.
    public static void FailIfRequired(string reason);
}

public sealed record CudaRefusalScan(int Branches, IReadOnlyList<string> Unguarded);

public static class CudaRefusalBranches
{
    // Every .cs file under `directory` but bin/ and obj/: the conditions
    // `if (... CudaSkippedBecause is [not] null)`, and the `path:line` of each whose
    // refused side (the block of `is not null`, the `else` of `is null`) does not call
    // CudaRequirement.FailIfRequired.
    public static CudaRefusalScan Scan(string directory);
}
```

`Execution.Tests` and `Simulation.Tests` each run `Scan` over their own directory and
require at least one branch and no unguarded one: the list of facts is found, not typed.
A condition split over several lines is not read.

## Side effects

Reads fixture files (`results.m.txt` reference and replica files, `exclusions.json`);
`BitSnapshot` writes a `Snapshots/<family>.received.txt` file beside the approved one
once a case of that family is missing or moved, complete through the last case the run
passed it, its first line the platform line above; `CpuHost` creates an ILGPU context
and a CPU accelerator.

## Out of scope

- Generating references: `Fixtures`.
- CUDA: a test node that needs it creates its own accelerator.
