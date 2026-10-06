using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Decision XIII ("find the centre before touching the spread"), a measurement only: decision XI's withdrawal
/// found a Count-governed cell (<c>fqdokkarm(14,:)[19]</c>) whose region does not even contain the replicas' own
/// mean — a centring problem, not a spread problem, invisible to a band drawn around the data (Check A) and
/// indistinguishable from an actual dispersion defect once it inflates every tail probability (Check B). This
/// file measures, per Count-governed cell (<see cref="CalibrationRule.Count"/>, the same
/// population <c>DilutionDiagnosticTests</c>' Check A/B and <c>RegionGuardDiagnosticTests</c> already read off
/// <see cref="StatisticalCriterion.CompareCellVerdicts"/>), in count space. Because the population below is
/// exactly <c>v.Rule == Count</c> off a live <see cref="StatisticalCriterion.CompareCellVerdicts"/> call, never a
/// separate re-derivation of "which cells are count-like", a cell a governance change moves out of the count
/// rule (<c>tests/Harness/BOOT.md</c>, "Decision XII") leaves this measurement with it automatically, on the
/// same run this file already makes — that decision's own arm 2 needed no edit here, only a re-run against the
/// changed library (<c>tests/Harness/BOOT.md</c>, "Decision XII", has the before/after population and figure):
///
/// <list type="bullet">
/// <item><c>mu_model = n* p_hat</c> — the beta-binomial region's own centre, read off
/// <see cref="BetaBinomialPredictive.Interval"/>'s own <c>Mean</c>, never a second implementation of
/// the formula;</item>
/// <item><c>mu_emp = mean_i(k_i * n* / n_i)</c> — the replicas' own counts rescaled to the candidate's own
/// reconstructed total <c>n*</c>, what the model is claiming to predict;</item>
/// <item>the ratio <c>mu_model / mu_emp</c>: median, deciles, and the count of cells beyond a factor of 1.5
/// either way, per family;</item>
/// <item>that ratio against the unit's own total-dispersion index <c>phi_T</c> (`tests/Harness/HISTORY.md`,
/// "Decision X, item 1" / <c>ArrayTotalDiagnosticTests</c>, identical <c>s^2 / T_bar</c> arithmetic, not a second
/// implementation of the reconstruction — only <see cref="CountReconstruction.ArrayTotals"/>'s own
/// output feeds it) and against <c>m</c> (the same per-unit "resolvable non-zero cells" notation
/// <c>ArrayTotalDiagnosticTests</c> already uses).</item>
/// </list>
///
/// The first hypothesis (decision XIII's own text): <c>p_hat = sum_i k_i / sum_i n_i</c> is a total-weighted mean
/// of the per-run rates, while the quantity it is used to predict is one run's own rate, and at decision X's own
/// measured total-dispersion (median 89, max 5512) these are different numbers. Tested directly: per cell,
/// <c>p_hat</c> (weighted) against <c>mean_i(k_i / n_i)</c> (unweighted). Since <c>mu_emp</c> is defined as
/// <c>n*</c> times exactly that unweighted mean, <c>mu_model / mu_emp</c> and <c>p_hat / mean_i(k_i/n_i)</c> are
/// the *same ratio*, algebraically (<c>n*</c> cancels) — this file computes both independently from the same raw
/// per-cell reconstruction and reports the largest observed discrepancy between them, so the algebraic identity
/// is verified numerically rather than assumed.
///
/// No change to <c>EvaluateCells</c>, no change to any estimator, no re-run of the curve (decision XIII's own
/// "what is not to be done"): every quantity below is read from <see cref="StatisticalCriterion"/>'s existing
/// internal building blocks (<see cref="CountReconstruction.ArrayTotals"/>,
/// <see cref="CountReconstruction.PerCellCounts"/>, <see cref="DispersionEstimator.Estimate"/>,
/// <see cref="BetaBinomialPredictive.Interval"/>), never reimplemented. The candidate's own total
/// <c>n*</c> is reconstructed the same way a replica's total is — <c>ReconstructArrayTotals</c> called on the
/// reference file's own parsed cells wrapped as a singleton "replica" list — since <c>Compare</c>'s own
/// candidate-total field (<c>PendingCell.CandidateTotal</c>) is `private` and cannot be read from here; this is
/// the same public reconstruction primitive, not a second one.
///
/// Restricted to cells whose unit's own dispersion has <c>Phi >= 1.0</c>: only then did
/// <c>BetaBinomialCountFloor</c> (removed 2026-09-25, IDE0051 unused since decision XX routed this population
/// through <c>CountFloorFromCounts</c> instead — this diagnostic's own measurement predates that routing change)
/// actually build a beta-binomial region parameterised
/// by <c>n*</c> and <c>p_hat</c> — a <c>Phi &lt; 1.0</c> unit is routed to the negative-binomial fallback instead
/// (a different parameterisation entirely, predicting a replica's own count directly, not rescaled through
/// <c>n*</c>), so "mu_model = n* p_hat" does not describe it. Those cells are counted and reported separately,
/// not silently folded into either population.
///
/// Decision XV ("the centre defect at large cell counts, in one family", <c>tests/Harness/BOOT.md</c>) asks, before
/// anything else, whether the measurement above compares cells on the criterion's own canonical axis or on the raw
/// print index — <see cref="Item0AxisCheckFqdokkarmRowReconstructionAgainstItsOwnCanonicalMembership"/> answers
/// that directly, by reconstructing each <c>fqdokkarm(&lt;row&gt;,:)</c> unit twice: once exactly as
/// <see cref="ReconstructUnit"/> already does above (every lagged replica whose own file happens to contain the
/// key, the same raw dictionary lookup <see cref="CountReconstruction.ArrayTotals"/> performs), and
/// once restricted to the replicas <see cref="StatisticalCriterion"/>'s own <c>BuildComparePending</c> would
/// actually admit for that row — a fixed-width <c>Dkarmcat</c> prefix match strictly past the row's own index,
/// mirrored here (<see cref="LiteralPrefixMatchLength"/>) because the production method (<c>PrefixMatchLength</c>,
/// <c>StatisticalCriterion.cs</c>) is <c>private</c> and the check itself is the three-line exact-equality scan
/// this node's BOOT.md already documents under "Canonical category axis", not a statistical formula — the same
/// kind of restatement this file's own <see cref="Median"/>/<see cref="MBucket"/> already are, never a competing
/// implementation of the model (root BOOT.md Taboos).
/// </summary>
public class CentreDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public CentreDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Capture group added for decision XV's own row-number extraction (`Item0_AxisCheck_...` below), mirroring
    // StatisticalCriterion.cs's own private `FqDokKarmRow` pattern exactly (`@"^fqdokkarm\((\d+),:\)$"`); adding a
    // capturing group changes nothing about `IsMatch`, so `FamilyOf` below is unaffected.
    private static readonly Regex FqDokKarmRow = new(@"^fqdokkarm\((\d+),:\)$", RegexOptions.Compiled);

    private static string FamilyOf(string name) => FqDokKarmRow.IsMatch(name) ? "fqdokkarm" : name;

    // `tests/Harness/HISTORY.md#decision-vii-full-reasoning`: the same per-cell contributing-replica
    // floor `IntervalWidthDiagnosticTests.CollectPerIndexCounts` already applies, reused at the same value so a
    // sparse cell does not report a one- or two-replica "mean rate" as if it meant something.
    private const int MinimumContributingReplicasPerCell = 4;

    // Decision VI's own scope, the same alpha `DilutionDiagnosticTests` (Check A/B) and `RegionGuardDiagnosticTests`
    // already measure at; `BetaBinomialInterval`'s own `Mean` does not depend on `alpha` at all (the beta-binomial
    // mean is `n p` regardless of the two-sided search), so this choice affects nothing here beyond keeping the
    // population identical to the other two checks'.
    private const double Alpha = 0.05;

    private readonly record struct UnitReconstruction(
        DispersionEstimator.DispersionEstimate Dispersion, double NStar,
        List<Dictionary<int, double>> PerReplicaCounts, List<double> ReplicaTotals,
        double PhiT, double MedianM);

    private readonly record struct CentrePoint(
        string Family, string Formulation, string Unit, int Index, int ContributingReplicas,
        double NStar, double PHatWeighted, double PHatUnweighted, double MuModel, double MuEmp,
        double RatioModelOverEmp, double RatioPHat, double PhiT, double MedianM);

    // Extracted from Item3And5's own inner loop (unchanged arithmetic, only lifted out) so decision XV's own
    // Item0 below can compute the identical per-cell measurement over a differently-restricted replica population
    // without a second implementation of the formula (root BOOT.md Taboos). NBar (decision XVI, "the sparsest
    // cells"): the mean of the same `ns` list already built below, over the cell's own contributing replicas —
    // not a new quantity, just a mean already implicit in `sumN` exposed for the bucketing decision XVI asks for.
    // Correlation/CVn/CVr/Low/High (decision XVII, "the centre defect is a correlation"): the population Pearson
    // correlation between a contributing replica's own total `n_i` and its own rate `r_i = k_i/n_i` at this cell,
    // and the two coefficients of variation, computed once from the identical `ks`/`ns` lists below so decision
    // XVII's own identity check and its follow-on measurements never re-gather them; `Low`/`High` are the same
    // `BetaBinomialInterval` call's own bounds, captured rather than recomputed by a second call with the same
    // arguments.
    private readonly record struct CentreMeasurement(
        int ContributingReplicas, double PHatWeighted, double PHatUnweighted, double MuModel, double MuEmp,
        double NBar, double Correlation, double CVn, double CVr, double Low, double High);

    // Decision XVIII's own elasticity fit needs the identical raw (k_i, n_i) pairs TryMeasureCell already gathers
    // here — extracted so both read from one gathering loop, never two (root BOOT.md Taboos).
    private static bool TryCollectContributingCounts(
        UnitReconstruction unit, int index, out List<double> ks, out List<double> ns)
    {
        ks = new List<double>();
        ns = new List<double>();
        for (var r = 0; r < unit.PerReplicaCounts.Count; r++)
        {
            if (unit.PerReplicaCounts[r].TryGetValue(index, out var k))
            {
                ks.Add(k);
                ns.Add(unit.ReplicaTotals[r]);
            }
        }

        return ks.Count >= MinimumContributingReplicasPerCell;
    }

    private static bool TryMeasureCell(UnitReconstruction unit, int index, out CentreMeasurement measurement)
    {
        measurement = default;

        if (!TryCollectContributingCounts(unit, index, out var ks, out var ns))
        {
            return false;
        }

        var sumK = ks.Sum();
        var sumN = ns.Sum();
        if (sumN <= 0.0)
        {
            return false;
        }

        var pHatWeighted = sumK / sumN;
        if (pHatWeighted is <= 0.0 or >= 1.0)
        {
            return false;
        }

        var pHatUnweighted = 0.0;
        for (var i = 0; i < ks.Count; i++)
        {
            pHatUnweighted += ks[i] / ns[i];
        }

        pHatUnweighted /= ks.Count;
        if (pHatUnweighted <= 0.0)
        {
            return false;
        }

        var nStarRounded = (long)Math.Round(unit.NStar);
        var (Mean, Low, High, _) = BetaBinomialPredictive.Interval(nStarRounded, pHatWeighted, unit.Dispersion.RhoHat, Alpha);
        var muModel = Mean;
        var muEmp = unit.NStar * pHatUnweighted;
        if (muEmp <= 0.0)
        {
            return false;
        }

        // Decision XVII's own identity: r_i = k_i / n_i, n_i the contributing replica's own total. `rs` reuses no
        // new arithmetic (`ks[i] / ns[i]`, the same division `pHatUnweighted` above already summed); `CorrelationStats`
        // is the one place this file computes a Pearson correlation and its two coefficients of variation — decision
        // XVIII's own elasticity fit (below) calls it too, rather than a second copy of the same formula.
        var rs = new List<double>(ks.Count);
        for (var i = 0; i < ks.Count; i++)
        {
            rs.Add(ks[i] / ns[i]);
        }

        var (Correlation, MeanX, SdX, MeanY, SdY) = CorrelationStats(CollectionsMarshal.AsSpan(ns), CollectionsMarshal.AsSpan(rs));
        var correlation = Correlation;
        var cvN = MeanX > 0.0 ? SdX / MeanX : double.NaN;
        var cvR = SdY / MeanY;

        measurement = new CentreMeasurement(
            ks.Count, pHatWeighted, pHatUnweighted, muModel, muEmp, MeanX, correlation, cvN, cvR, Low, High);
        return true;
    }

    // Decision XVII's own correlation and CVs, decision XVIII's own leverage (CV_n) and gate (the implied vs.
    // direct correlation) — one Pearson-correlation-and-moments primitive, population (1/N) conventions throughout
    // to match decision XVII's own algebraic identity exactly (root BOOT.md Taboos: no second implementation of a
    // formula). Not a model formula of PropStruct's own — a generic statistic, restated here the way this file
    // already restates Median/MBucket, never shared with production code because `StatisticalCriterion.cs` has no
    // correlation of its own to duplicate.
    //
    // Decision XIX ("the same question, asked where the power is") widened the parameter types from
    // `List<double>` to `IReadOnlyList<double>` so its own per-replication simulated-rate buffer (a reused
    // `double[]`, rebuilt in place every replication rather than reallocated as a `List<double>` a thousand
    // times per cell) could be passed directly. `ReadOnlySpan<double>` (CA1859: a concrete type, not an
    // interface) reaches the same goal with no indirection at all — every `List<double>` call site passes
    // `CollectionsMarshal.AsSpan(list)` (zero-copy, `System.Runtime.InteropServices`) and the reused `double[]`
    // buffer converts to a span implicitly — one implementation still, no second overload.
    private static (double Correlation, double MeanX, double SdX, double MeanY, double SdY) CorrelationStats(
        ReadOnlySpan<double> xs, ReadOnlySpan<double> ys)
    {
        var n = xs.Length;
        var sumX = 0.0;
        for (var i = 0; i < n; i++)
        {
            sumX += xs[i];
        }

        var sumY = 0.0;
        for (var i = 0; i < n; i++)
        {
            sumY += ys[i];
        }

        var meanX = sumX / n;
        var meanY = sumY / n;
        var cov = 0.0;
        var varX = 0.0;
        var varY = 0.0;
        for (var i = 0; i < n; i++)
        {
            var dx = xs[i] - meanX;
            var dy = ys[i] - meanY;
            cov += dx * dy;
            varX += dx * dx;
            varY += dy * dy;
        }

        cov /= n;
        varX /= n;
        varY /= n;
        var sdX = Math.Sqrt(Math.Max(varX, 0.0));
        var sdY = Math.Sqrt(Math.Max(varY, 0.0));
        var correlation = sdX > 0.0 && sdY > 0.0 ? cov / (sdX * sdY) : double.NaN;
        return (correlation, meanX, sdX, meanY, sdY);
    }

    [Trait("Category", "Long")]
    [Fact]
    public void Item3And5ModelCentreAgainstReplicaCentreByFamily()
    {
        var points = new List<CentrePoint>();
        var totalCountGoverned = 0;
        var negativeBinomialGoverned = 0;
        var unmeasurable = 0;

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            // One reconstruction per unit (array or fqdokkarm row), shared by every Count-governed cell of that
            // unit — root BOOT.md Taboos' ban on a second implementation of any part of the program applies to
            // repeated reconstruction of the same unit's own dispersion/total just as much as to a formula.
            var unitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                totalCountGoverned++;

                if (!unitCache.TryGetValue(v.Name, out var cached))
                {
                    cached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    unitCache[v.Name] = cached;
                }

                if (cached is not { } unit)
                {
                    unmeasurable++;
                    continue;
                }

                if (unit.Dispersion.Phi < 1.0)
                {
                    negativeBinomialGoverned++;
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var measurement))
                {
                    unmeasurable++;
                    continue;
                }

                points.Add(new CentrePoint(
                    FamilyOf(v.Name), formulation, v.Name, v.Index, measurement.ContributingReplicas,
                    unit.NStar, measurement.PHatWeighted, measurement.PHatUnweighted, measurement.MuModel, measurement.MuEmp,
                    measurement.MuModel / measurement.MuEmp, measurement.PHatWeighted / measurement.PHatUnweighted,
                    unit.PhiT, unit.MedianM));
            }
        }

        Assert.True(points.Count > 0, "expected at least one measurable Count-governed cell.");

        _output.WriteLine(
            $"Count-governed cells: total={totalCountGoverned}, negative-binomial-governed (Phi<1, no n*p_hat model)=" +
            $"{negativeBinomialGoverned} ({(double)negativeBinomialGoverned / totalCountGoverned:P2}), " +
            $"unmeasurable (reconstruction/degenerate)={unmeasurable} ({(double)unmeasurable / totalCountGoverned:P2}), " +
            $"measured={points.Count} ({(double)points.Count / totalCountGoverned:P2})");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 3: mu_model / mu_emp, by family ---");
        foreach (var group in points.GroupBy(p => p.Family).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            ReportRatioDeciles(group.Key, group.Select(p => p.RatioModelOverEmp).ToList());
        }

        _output.WriteLine(string.Empty);
        ReportRatioDeciles("(pooled, every family)", points.Select(p => p.RatioModelOverEmp).ToList());

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 5: p_hat (weighted) / mean_i(k_i/n_i) (unweighted), by family ---");
        foreach (var group in points.GroupBy(p => p.Family).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            ReportRatioDeciles(group.Key, group.Select(p => p.RatioPHat).ToList());
        }

        _output.WriteLine(string.Empty);
        ReportRatioDeciles("(pooled, every family)", points.Select(p => p.RatioPHat).ToList());

        var maxAlgebraicGap = points.Count > 0 ? points.Max(p => Math.Abs(p.RatioModelOverEmp - p.RatioPHat)) : double.NaN;
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"algebraic identity check: mu_model/mu_emp == p_hat/mean_i(k_i/n_i) for every cell, since n* cancels " +
            $"(mu_emp = n* * mean_i(k_i/n_i) by definition). Measured max|difference| across {points.Count} cells = " +
            $"{maxAlgebraicGap:G6} (floating-point noise expected; a larger value would mean a bug in this file's own " +
            $"arithmetic, not a fact about the model). The hypothesis therefore accounts for the entire item-3 gap by " +
            $"construction of mu_emp, not as an independent empirical finding — item 4 below is what tests whether " +
            $"the weighted/unweighted split is itself explained by the totals' own dispersion.");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 4a: mu_model/mu_emp against the unit's own phi_T (total-dispersion index), by decile ---");
        ReportAgainstPhiT(points);

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 4b: mu_model/mu_emp against the unit's own median m, by bucket ---");
        ReportAgainstM(points);
    }

    /// <summary>
    /// Decision XV's own first item ("does the centre diagnostic compare cells on the canonical axis the criterion
    /// uses, or on the raw print index?"), answered directly rather than asserted. <see cref="ReconstructUnit"/>,
    /// called above with the full <c>replicaCells</c> list, admits any lagged replica whose file happens to
    /// contain the key <c>fqdokkarm(&lt;row&gt;,:)</c> — a raw dictionary lookup
    /// (<see cref="CountReconstruction.ArrayTotals"/>'s own <c>TryGetValue</c>), regardless of whether
    /// that replica's own <c>Dkarmcat</c> genuinely reaches this row as the *same physical category* the reference
    /// means. Production <c>Compare</c>/<c>BuildComparePending</c> never does this for a canonical row: it restricts
    /// to <c>contributingReplicaIndices</c>, the replicas whose own <c>Dkarmcat</c> prefix-matches the reference's
    /// strictly past this row's index (this node's BOOT.md, "Canonical category axis" — measured there: HMX row 31
    /// alone spans boundary values 330-350 mkm across its 16 lagged replicas). This test reconstructs each
    /// <c>fqdokkarm</c> row unit both ways — via the unedited <see cref="ReconstructUnit"/>, once over the full
    /// replica list (Item3And5's own population) and once over only the replicas production would actually admit
    /// for that row — and reports how far apart the two populations, and the two centre measurements built from
    /// them, are. Every other family (`coef`, `fqkarm`, `fqkarm_cor`, `fqmkm2`) has no row dimension and no
    /// canonical-axis restriction of its own (`BuildComparePending`'s own array branch draws every cell of such a
    /// family from the identical, unfiltered replica list `ReconstructUnit` already uses above), so this
    /// discrepancy, if it exists, can only ever appear in `fqdokkarm` — which is exactly the one family decision
    /// XV's own centre tail is concentrated in and every sibling reads 0 % on.
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void Item0AxisCheckFqdokkarmRowReconstructionAgainstItsOwnCanonicalMembership()
    {
        var unitsCompared = 0;
        var unitsWithIdenticalMembership = 0;
        var unitsCanonicalNarrower = 0;
        var totalNaiveReplicas = 0;
        var totalCanonicalReplicas = 0;

        var naiveRatios = new List<double>();
        var canonicalRatios = new List<double>();
        var bothMeasured = new List<(double NaiveRatio, double CanonicalRatio)>();
        var canonicalOnlyUnmeasurable = 0;
        var naiveOnlyUnmeasurable = 0;

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            var naiveUnitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);
            var canonicalUnitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                var rowMatch = FqDokKarmRow.Match(v.Name);
                if (!rowMatch.Success)
                {
                    continue;
                }

                if (!naiveUnitCache.TryGetValue(v.Name, out var naiveCached))
                {
                    naiveCached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    naiveUnitCache[v.Name] = naiveCached;

                    var rowIndex0 = int.Parse(rowMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                    var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                    canonicalUnitCache[v.Name] = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);

                    unitsCompared++;
                    totalNaiveReplicas += membership.NaiveCount;
                    totalCanonicalReplicas += membership.CanonicalCount;
                    if (membership.NaiveCount == membership.CanonicalCount)
                    {
                        unitsWithIdenticalMembership++;
                    }
                    else
                    {
                        unitsCanonicalNarrower++;
                    }
                }

                _ = naiveUnitCache.TryGetValue(v.Name, out var naiveUnit);
                _ = canonicalUnitCache.TryGetValue(v.Name, out var canonicalUnit);

                double? naiveRatio = null;
                if (naiveUnit is { } nu && nu.Dispersion.Phi >= 1.0 && TryMeasureCell(nu, v.Index, out var naiveMeasurement))
                {
                    naiveRatio = naiveMeasurement.MuModel / naiveMeasurement.MuEmp;
                    naiveRatios.Add(naiveRatio.Value);
                }

                double? canonicalRatio = null;
                if (canonicalUnit is { } cu && cu.Dispersion.Phi >= 1.0 && TryMeasureCell(cu, v.Index, out var canonicalMeasurement))
                {
                    canonicalRatio = canonicalMeasurement.MuModel / canonicalMeasurement.MuEmp;
                    canonicalRatios.Add(canonicalRatio.Value);
                }

                if (naiveRatio is { } nr && canonicalRatio is { } cr)
                {
                    bothMeasured.Add((nr, cr));
                }
                else if (naiveRatio is not null)
                {
                    canonicalOnlyUnmeasurable++;
                }
                else if (canonicalRatio is not null)
                {
                    naiveOnlyUnmeasurable++;
                }
            }
        }

        Assert.True(unitsCompared > 0, "expected at least one fqdokkarm row unit among the Count-governed cells.");

        _output.WriteLine(
            $"fqdokkarm row units compared: {unitsCompared}; identical naive/canonical replica membership: " +
            $"{unitsWithIdenticalMembership} ({(double)unitsWithIdenticalMembership / unitsCompared:P2}); " +
            $"canonical membership strictly narrower: {unitsCanonicalNarrower} ({(double)unitsCanonicalNarrower / unitsCompared:P2}). " +
            $"Mean contributing replicas per unit: naive (raw presence)={(double)totalNaiveReplicas / unitsCompared:G4}, " +
            $"canonical (Dkarmcat prefix match past the row)={(double)totalCanonicalReplicas / unitsCompared:G4}.");

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"per-cell measurability: measured under both populations={bothMeasured.Count}, measured under naive " +
            $"only (too few canonically-eligible replicas)={canonicalOnlyUnmeasurable}, measured under canonical " +
            $"only={naiveOnlyUnmeasurable}.");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- naive population (Item3And5's own reconstruction, unedited), fqdokkarm only ---");
        ReportRatioDeciles("fqdokkarm/naive", naiveRatios);
        _output.WriteLine("--- canonical population (BuildComparePending's own row-eligible replicas) ---");
        ReportRatioDeciles("fqdokkarm/canonical", canonicalRatios);

        var naiveBeyond = bothMeasured.Count(p => p.NaiveRatio is > 1.5 or < (1.0 / 1.5));
        var stillBeyondUnderCanonical = bothMeasured.Count(p =>
            (p.NaiveRatio > 1.5 || p.NaiveRatio < 1.0 / 1.5) && (p.CanonicalRatio > 1.5 || p.CanonicalRatio < 1.0 / 1.5));
        var movedInsideTheBand = naiveBeyond - stillBeyondUnderCanonical;
        var newlyBeyondUnderCanonical = bothMeasured.Count(p =>
            !(p.NaiveRatio > 1.5 || p.NaiveRatio < 1.0 / 1.5) && (p.CanonicalRatio > 1.5 || p.CanonicalRatio < 1.0 / 1.5));

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"of the {bothMeasured.Count} cells measured under both populations, {naiveBeyond} are beyond a factor " +
            $"of 1.5 under the naive (raw-presence) reconstruction; of those, {stillBeyondUnderCanonical} stay " +
            $"beyond 1.5x under the canonical (Dkarmcat-prefix-matched) reconstruction and {movedInsideTheBand} " +
            $"move back inside the band " +
            $"({(naiveBeyond > 0 ? (double)movedInsideTheBand / naiveBeyond : double.NaN):P2} of the naive tail); " +
            $"{newlyBeyondUnderCanonical} cells inside the band under naive move beyond 1.5x under canonical.");
    }

    /// <summary>
    /// Decision XV's remaining three items, reached because Item0 above found the measurement sound (the
    /// canonical population does not dissolve the tail — it reads slightly larger, 19.88 % against the naive
    /// 17.39 %). Per Count-governed <c>fqdokkarm</c> cell, canonical population throughout (Item0's own, never the
    /// naive one): the ratio against the row's own physical index, its own reconstructed total, that total's share
    /// of the parent histogram (every row's own total, same formulation, summed — decision XV's item 2), and the
    /// cell's own <c>p_hat</c>; whether the offending cells cluster at one end of the row's own column axis; and
    /// the same ratio recomputed with the row's own per-column <c>p_hat</c> (pooled only over the replicas
    /// canonically eligible for this one row) replaced by a "parent" <c>p_hat</c> pooled over the identical column
    /// index across every row of the same formulation, keeping the row's own <c>n*</c> and <c>rho_hat</c> — the one
    /// substitution decision XV's item 3 names ("row-level pooling"), nothing else changed.
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void Item1To3OffendingCellsAgainstRowCovariatesColumnPositionAndParentPHat()
    {
        var covariates = new List<(
            string Formulation, string Unit, int RowIndex0, int ColumnIndex, double NStar, double Share,
            double PHat, double RatioRow, bool Offending)>();
        var parentPairs = new List<(double RatioRow, double RatioParent, bool RowOffending)>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha)
                .Where(v => v.Rule == CalibrationRule.Count && FqDokKarmRow.IsMatch(v.Name))
                .ToList();

            var rowUnits = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);
            var rowIndex0Of = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var v in verdicts)
            {
                if (rowUnits.ContainsKey(v.Name))
                {
                    continue;
                }

                var rowIndex0 = int.Parse(
                    FqDokKarmRow.Match(v.Name).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                rowIndex0Of[v.Name] = rowIndex0;
                var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                rowUnits[v.Name] = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);
            }

            // decision XV's item 2: the parent histogram every row is a slice of — the sum of every row's own
            // reconstructed total, same formulation, canonical population throughout.
            var parentTotal = rowUnits.Values.Where(u => u is { Dispersion.Phi: >= 1.0 }).Sum(u => u!.Value.NStar);

            // decision XV's item 3: a "parent" p_hat per column index, pooled over every row's own canonically-
            // eligible replicas at that column — the same (k_i, n_i) pairs TryMeasureCell already reads per row,
            // pooled across rows instead of within one, never a second implementation of the pooling arithmetic
            // itself (a plain sum of sums, the same shape `TryMeasureCell`'s own `sumK`/`sumN` already are).
            var parentSumK = new Dictionary<int, double>();
            var parentSumN = new Dictionary<int, double>();
            foreach (var (name, unit) in rowUnits)
            {
                if (unit is not { Dispersion.Phi: >= 1.0 } u)
                {
                    continue;
                }

                for (var r = 0; r < u.PerReplicaCounts.Count; r++)
                {
                    foreach (var (index, k) in u.PerReplicaCounts[r])
                    {
                        if (k <= 0.0 && u.ReplicaTotals[r] <= 0.0)
                        {
                            continue;
                        }

                        parentSumK[index] = parentSumK.GetValueOrDefault(index) + k;
                        parentSumN[index] = parentSumN.GetValueOrDefault(index) + u.ReplicaTotals[r];
                    }
                }
            }

            foreach (var v in verdicts)
            {
                if (rowUnits[v.Name] is not { } unit || unit.Dispersion.Phi < 1.0)
                {
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var rowMeasurement))
                {
                    continue;
                }

                var ratioRow = rowMeasurement.MuModel / rowMeasurement.MuEmp;
                var offending = ratioRow is > 1.5 or < (1.0 / 1.5);
                var rowIndex0 = rowIndex0Of[v.Name];
                var share = parentTotal > 0.0 ? unit.NStar / parentTotal : double.NaN;

                covariates.Add((
                    formulation, v.Name, rowIndex0, v.Index, unit.NStar, share,
                    rowMeasurement.PHatWeighted, ratioRow, offending));

                if (parentSumN.TryGetValue(v.Index, out var pooledN) && pooledN > 0.0
                    && parentSumK.TryGetValue(v.Index, out var pooledK))
                {
                    var parentPHat = pooledK / pooledN;
                    if (parentPHat is > 0.0 and < 1.0)
                    {
                        var nStarRounded = (long)Math.Round(unit.NStar);
                        var muModelParent = BetaBinomialPredictive.Interval(
                            nStarRounded, parentPHat, unit.Dispersion.RhoHat, Alpha).Mean;
                        var ratioParent = muModelParent / rowMeasurement.MuEmp;
                        parentPairs.Add((ratioRow, ratioParent, offending));
                    }
                }
            }
        }

        Assert.True(covariates.Count > 0, "expected at least one measured fqdokkarm cell (canonical population).");

        var offendingCells = covariates.Where(c => c.Offending).ToList();
        var otherCells = covariates.Where(c => !c.Offending).ToList();

        _output.WriteLine(
            $"cells measured (canonical population): {covariates.Count}, offending (beyond 1.5x)={offendingCells.Count} " +
            $"({(double)offendingCells.Count / covariates.Count:P2}).");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 1: row covariates, offending vs. other cells (median [P10, P90]) ---");
        ReportCovariateSplit("row index (rowIndex0)", offendingCells.Select(c => (double)c.RowIndex0).ToList(), otherCells.Select(c => (double)c.RowIndex0).ToList());
        ReportCovariateSplit("row's own n*", offendingCells.Select(c => c.NStar).ToList(), otherCells.Select(c => c.NStar).ToList());
        ReportCovariateSplit("row's share of the parent histogram", offendingCells.Select(c => c.Share).ToList(), otherCells.Select(c => c.Share).ToList());
        ReportCovariateSplit("cell's own p_hat", offendingCells.Select(c => c.PHat).ToList(), otherCells.Select(c => c.PHat).ToList());

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 2: column-index clustering within each row, offending cells only ---");
        foreach (var group in offendingCells.GroupBy(c => (c.Formulation, c.Unit)))
        {
            var rowColumns = covariates.Where(c => c.Formulation == group.Key.Formulation && c.Unit == group.Key.Unit)
                .Select(c => c.ColumnIndex).OrderBy(x => x).ToList();
            var maxColumn = rowColumns.Count > 0 ? rowColumns[^1] : 0;
            var offendingColumns = group.Select(c => c.ColumnIndex).OrderBy(x => x).ToList();
            _output.WriteLine(
                $"{group.Key.Formulation,-9} {group.Key.Unit,-18} row's own measured columns=[0,{maxColumn}] " +
                $"offending columns={string.Join(",", offendingColumns)}");
        }

        var headHalf = offendingCells.Count(c => IsInFirstHalfOfItsRow(c, covariates));
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"of {offendingCells.Count} offending cells, {headHalf} ({(double)headHalf / offendingCells.Count:P2}) sit " +
            $"in the first half of their own row's measured column range (the head, not the deep tail).");

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"--- item 3: row p_hat replaced by the parent (same-column, pooled-over-rows) p_hat, {parentPairs.Count} cells ---");
        ReportRatioDeciles("row p_hat", parentPairs.Select(p => p.RatioRow).ToList());
        ReportRatioDeciles("parent p_hat", parentPairs.Select(p => p.RatioParent).ToList());

        var rowOffendingPairs = parentPairs.Where(p => p.RowOffending).ToList();
        var stillOffendingUnderParent = rowOffendingPairs.Count(p => p.RatioParent is > 1.5 or < (1.0 / 1.5));
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"of {rowOffendingPairs.Count} cells offending under the row's own p_hat, {stillOffendingUnderParent} " +
            $"({(rowOffendingPairs.Count > 0 ? (double)stillOffendingUnderParent / rowOffendingPairs.Count : double.NaN):P2}) " +
            "stay offending under the parent (pooled-over-rows) p_hat instead.");
    }

    /// <summary>
    /// Decision XVI ("the sparsest cells, and the rule that was parked for them"): decision XV's own tail
    /// (deep column position, smallest totals, smallest <c>p_hat</c>) is a description of sparse cells, which have
    /// a candidate mechanism that does not require the model to be wrong — at an expected count of order one, a
    /// printed <c>0</c> against one printed quantum is the entire quantity, and reconstruction itself (a non-zero
    /// cell whose quantum does not clear its own print resolution is dropped) is coarse there. This buckets
    /// decision XV's own population, every Count-governed cell of every family (not <c>fqdokkarm</c> alone — the
    /// bucket must show the siblings' zero tail is due to absent sparse cells, not different behaviour in them), by
    /// the cell's own expected count <c>n_bar * p_hat</c> (<see cref="CentreMeasurement.NBar"/>, the same
    /// contributing-replica mean total <c>TryMeasureCell</c> already sums into <c>sumN</c>, times the same
    /// <c>p_hat</c> `Item3And5` already reports — no new formula). `fqdokkarm` rows use the canonical
    /// (Dkarmcat-prefix-matched) reconstruction decision XV's own item 0 established as the sound one; every
    /// sibling family has no row split and needs no such restriction.
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void DecisionXVIRatioAgainstExpectedCountByFamily()
    {
        var points = new List<(string Family, double ExpectedCount, double Ratio)>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            var unitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                if (!unitCache.TryGetValue(v.Name, out var cached))
                {
                    var rowMatch = FqDokKarmRow.Match(v.Name);
                    if (rowMatch.Success)
                    {
                        var rowIndex0 = int.Parse(
                            rowMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                        var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                        cached = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);
                    }
                    else
                    {
                        cached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    }

                    unitCache[v.Name] = cached;
                }

                if (cached is not { } unit || unit.Dispersion.Phi < 1.0)
                {
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var measurement))
                {
                    continue;
                }

                var expectedCount = measurement.NBar * measurement.PHatWeighted;
                points.Add((FamilyOf(v.Name), expectedCount, measurement.MuModel / measurement.MuEmp));
            }
        }

        Assert.True(points.Count > 0, "expected at least one measured Count-governed cell.");

        _output.WriteLine($"cells measured (canonical fqdokkarm population, plain reconstruction for every sibling): {points.Count}");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- ratio by expected count bucket, pooled (every family) ---");
        foreach (var bucket in ExpectedCountBuckets)
        {
            ReportExpectedCountBucket(bucket, points.Where(p => ExpectedCountBucketOf(p.ExpectedCount) == bucket).Select(p => p.Ratio).ToList());
        }

        foreach (var family in points.Select(p => p.Family).Distinct().OrderBy(f => f, StringComparer.Ordinal))
        {
            _output.WriteLine(string.Empty);
            _output.WriteLine($"--- ratio by expected count bucket, family = {family} ---");
            var familyPoints = points.Where(p => p.Family == family).ToList();
            foreach (var bucket in ExpectedCountBuckets)
            {
                ReportExpectedCountBucket(bucket, familyPoints.Where(p => ExpectedCountBucketOf(p.ExpectedCount) == bucket).Select(p => p.Ratio).ToList());
            }
        }
    }

    // Decision XVI's own bucket edges, in order: below 0.5, [0.5, 1), [1, 2), [2, 5), [5, 10), [10, 30), 30 and above.
    private static readonly string[] ExpectedCountBuckets = ["<0.5", "0.5-1", "1-2", "2-5", "5-10", "10-30", ">=30"];

    private static string ExpectedCountBucketOf(double expectedCount) => expectedCount switch
    {
        < 0.5 => "<0.5",
        < 1.0 => "0.5-1",
        < 2.0 => "1-2",
        < 5.0 => "2-5",
        < 10.0 => "5-10",
        < 30.0 => "10-30",
        _ => ">=30",
    };

    private void ReportExpectedCountBucket(string bucket, List<double> ratios)
    {
        if (ratios.Count == 0)
        {
            _output.WriteLine($"{bucket,-6} N={0,6} (no cells)");
            return;
        }

        ratios.Sort();
        var beyond = ratios.Count(r => r is > 1.5 or < (1.0 / 1.5));
        _output.WriteLine(
            $"{bucket,-6} N={ratios.Count,6} median={Median(ratios):G4} beyond-1.5x={beyond,5} ({(double)beyond / ratios.Count:P2})");
    }

    /// <summary>
    /// Decision XVII ("the centre defect is a correlation, and it has a name"). Item 1 is the gate: with
    /// <c>r_i = k_i / n_i</c> a contributing replica's own rate and <c>n_i</c> its own total,
    /// <c>mu_model / mu_emp = 1 + corr(n, r) * CV_n * CV_r</c> is claimed to hold as a pure algebraic identity
    /// (<c>p_hat = E[n r] / E[n]</c>, <c>mean(r) = E[r]</c>, so <c>p_hat / mean(r) = E[nr] / (E[n] E[r]) =
    /// 1 + Cov(n,r) / (E[n] E[r])</c>). This computes the right-hand side from <see cref="CentreMeasurement"/>'s
    /// own <c>Correlation</c>/<c>CVn</c>/<c>CVr</c> (built from the identical <c>ks</c>/<c>ns</c> lists
    /// <see cref="TryMeasureCell"/> already gathers — no second gathering, no second formula) and compares it,
    /// cell by cell, against the ratio <c>MuModel / MuEmp</c> the earlier decisions already measured. If they do
    /// not agree to rounding the identity is wrong and this test says so and stops — items 2-4 below are gated on
    /// this assertion, not merely printed after it, so a failing identity cannot silently carry a stale story into
    /// the rest of the file's own console output.
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void DecisionXVIIItem1CorrelationIdentityGate()
    {
        var points = CollectDecisionXVIIPoints();
        Assert.True(points.Count > 0, "expected at least one measured Count-governed cell.");

        var maxAbsDiscrepancy = 0.0;
        var worst = default((string Family, string Formulation, string Unit, int Index, double Ratio, double Rhs));
        foreach (var p in points)
        {
            var rhs = 1.0 + p.Correlation * p.CVn * p.CVr;
            var discrepancy = Math.Abs(p.Ratio - rhs);
            if (discrepancy > maxAbsDiscrepancy)
            {
                maxAbsDiscrepancy = discrepancy;
                worst = (p.Family, p.Formulation, p.Unit, p.Index, p.Ratio, rhs);
            }
        }

        _output.WriteLine(
            $"identity check over {points.Count} cells: max|ratio - (1 + corr*CVn*CVr)| = {maxAbsDiscrepancy:G6}, " +
            $"worst cell = {worst.Family} {worst.Formulation} {worst.Unit}[{worst.Index}] " +
            $"(ratio={worst.Ratio:G10}, rhs={worst.Rhs:G10}).");

        // Floating-point noise only, the same bound Item3And5's own algebraic check already measured (2.22e-16) —
        // not tuned to this run's own output (root BOOT.md Taboos: no loosened tolerance). A discrepancy above
        // this is the identity failing, not rounding, and this assertion is the "stop" decision XVII's own item 1
        // requires: it is not adjusted to force a pass.
        Assert.True(maxAbsDiscrepancy < 1e-6, "decision XVII's own identity does not hold to rounding — see the discrepancy reported above; do not adjust the identity or the code to force agreement, report it instead.");
    }

    /// <summary>
    /// Decision XVII, items 2-4, reached only because item 1's own gate
    /// (<see cref="DecisionXVIIItem1CorrelationIdentityGate"/>) holds. Item 2: the distribution of
    /// <c>corr(n, r)</c> per family, offending and non-offending cells apart (the prediction: near zero except
    /// `fqdokkarm`'s offenders). Item 3: for `fqdokkarm`, whether the correlation's sign follows the cell's
    /// position on its own row's column axis (decision XV's own deep-tail finding). Item 4: an ordinary-least-
    /// squares line of `r` on `n` per offending `fqdokkarm` cell (slope <c>corr * sd_r / sd_n</c>, algebraically
    /// derived from the same <c>Correlation</c>/<c>CVn</c>/<c>CVr</c> item 1 already computed — no second
    /// implementation), and what it predicts at the candidate's own total against the pooled `p_hat`, expressed in
    /// units of the beta-binomial interval's own width (<c>High - Low</c>, the same interval `TryMeasureCell`
    /// already builds for `mu_model`).
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void DecisionXVIIItems2To4CorrelationDistributionAxisAndLineFit()
    {
        var points = CollectDecisionXVIIPoints();
        Assert.True(points.Count > 0, "expected at least one measured Count-governed cell.");

        var maxAbsDiscrepancy = points.Max(p => Math.Abs(p.Ratio - (1.0 + p.Correlation * p.CVn * p.CVr)));
        Assert.True(maxAbsDiscrepancy < 1e-6, "item 1's own gate must hold before items 2-4 mean anything; see DecisionXVIIItem1CorrelationIdentityGate.");

        _output.WriteLine("--- item 2: corr(n, r), by family, offending vs. other cells (median [P10, P90]) ---");
        foreach (var family in points.Select(p => p.Family).Distinct().OrderBy(f => f, StringComparer.Ordinal))
        {
            var familyPoints = points.Where(p => p.Family == family).ToList();
            var offending = familyPoints.Where(p => p.Offending).Select(p => p.Correlation).ToList();
            var other = familyPoints.Where(p => !p.Offending).Select(p => p.Correlation).ToList();
            ReportCovariateSplit(family, offending, other);
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 3: fqdokkarm, correlation sign against column position within the row ---");
        var fqdokkarm = points.Where(p => p.Family == "fqdokkarm").ToList();
        var headNegative = 0;
        var headNonNegative = 0;
        var tailNegative = 0;
        var tailNonNegative = 0;
        foreach (var p in fqdokkarm)
        {
            var isHead = IsInFirstHalfOfItsFqdokkarmRow(p, fqdokkarm);
            var isNegative = p.Correlation < 0.0;
            if (isHead)
            {
                if (isNegative)
                {
                    headNegative++;
                }
                else
                {
                    headNonNegative++;
                }
            }
            else
            {
                if (isNegative)
                {
                    tailNegative++;
                }
                else
                {
                    tailNonNegative++;
                }
            }
        }

        _output.WriteLine(
            $"head (first half of its own row): negative corr={headNegative} ({(double)headNegative / (headNegative + headNonNegative):P2} of {headNegative + headNonNegative}); " +
            $"tail (second half): negative corr={tailNegative} ({(double)tailNegative / (tailNegative + tailNonNegative):P2} of {tailNegative + tailNonNegative}).");

        var offendingFqdokkarm = fqdokkarm.Where(p => p.Offending).ToList();
        var offendingNegative = offendingFqdokkarm.Count(p => p.Correlation < 0.0);
        _output.WriteLine(
            $"of {offendingFqdokkarm.Count} offending fqdokkarm cells, {offendingNegative} " +
            $"({(offendingFqdokkarm.Count > 0 ? (double)offendingNegative / offendingFqdokkarm.Count : double.NaN):P2}) have negative corr(n, r).");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- item 4: line fit of r on n, per offending fqdokkarm cell, prediction vs. p_hat (units of interval width) ---");
        var lineFitDeltas = new List<double>();
        foreach (var p in offendingFqdokkarm)
        {
            var width = p.High - p.Low;
            if (width <= 0.0 || p.CVn <= 0.0)
            {
                continue;
            }

            // OLS slope of r on n: b = Cov(n,r) / Var(n) = corr * sd_r / sd_n = corr * (CVr * meanR) / (CVn * meanN);
            // algebraically derived from item 1's own stored fields, never a second computation from ks/ns.
            var sdN = p.CVn * p.NBar;
            var sdR = p.CVr * p.PHatUnweighted;
            var slope = sdN > 0.0 ? p.Correlation * sdR / sdN : 0.0;
            var intercept = p.PHatUnweighted - slope * p.NBar;
            var predictedR = intercept + slope * p.NStarRounded;
            var predictedCount = p.NStarRounded * predictedR;
            var deltaInIntervalWidths = (predictedCount - p.MuModel) / width;
            lineFitDeltas.Add(deltaInIntervalWidths);
        }

        // Not ReportRatioDeciles: that helper's "beyond 1.5x" framing assumes a ratio centred on 1, and this is a
        // signed delta (the line fit can predict either above or below p_hat) — using it here would print a
        // "beyond-1.5x" figure that means nothing for this quantity.
        ReportSignedDeciles("line-fit shift, interval widths", lineFitDeltas);
        var median = lineFitDeltas.Count > 0 ? Median(lineFitDeltas.OrderBy(x => x).ToList()) : double.NaN;
        var beyondOneWidth = lineFitDeltas.Count(d => Math.Abs(d) > 1.0);
        _output.WriteLine(
            $"of {lineFitDeltas.Count} offending fqdokkarm cells with a defined interval width, median|shift|={Math.Abs(median):G4} " +
            $"interval widths, {beyondOneWidth} ({(lineFitDeltas.Count > 0 ? (double)beyondOneWidth / lineFitDeltas.Count : double.NaN):P2}) " +
            "shift by more than one interval width.");
    }

    private readonly record struct DecisionXVIIPoint(
        string Family, string Formulation, string Unit, int Index, int ColumnIndex, bool Offending,
        double Ratio, double Correlation, double CVn, double CVr, double NBar, double PHatUnweighted,
        long NStarRounded, double MuModel, double Low, double High);

    // Shared by both decision XVII tests so item 1's own population and item 2-4's are provably the same cells
    // (root BOOT.md Taboos: no second implementation). Reuses decision XVI's own reconstruction rule unedited:
    // canonical (Dkarmcat-prefix-matched) replicas for an `fqdokkarm` row, the plain unfiltered reconstruction for
    // every sibling family (which has no row split to restrict).
    private static List<DecisionXVIIPoint> CollectDecisionXVIIPoints()
    {
        var points = new List<DecisionXVIIPoint>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            var unitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                if (!unitCache.TryGetValue(v.Name, out var cached))
                {
                    var rowMatch = FqDokKarmRow.Match(v.Name);
                    if (rowMatch.Success)
                    {
                        var rowIndex0 = int.Parse(
                            rowMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                        var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                        cached = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);
                    }
                    else
                    {
                        cached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    }

                    unitCache[v.Name] = cached;
                }

                if (cached is not { } unit || unit.Dispersion.Phi < 1.0)
                {
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var measurement))
                {
                    continue;
                }

                var ratio = measurement.MuModel / measurement.MuEmp;
                points.Add(new DecisionXVIIPoint(
                    FamilyOf(v.Name), formulation, v.Name, v.Index, v.Index, ratio is > 1.5 or < (1.0 / 1.5),
                    ratio, measurement.Correlation, measurement.CVn, measurement.CVr, measurement.NBar,
                    measurement.PHatUnweighted, (long)Math.Round(unit.NStar), measurement.MuModel,
                    measurement.Low, measurement.High));
            }
        }

        return points;
    }

    private static bool IsInFirstHalfOfItsFqdokkarmRow(DecisionXVIIPoint cell, List<DecisionXVIIPoint> fqdokkarm)
    {
        var rowColumns = fqdokkarm.Where(p => p.Formulation == cell.Formulation && p.Unit == cell.Unit)
            .Select(p => p.ColumnIndex).ToList();
        var maxColumn = rowColumns.Max();
        return maxColumn == 0 || cell.ColumnIndex <= maxColumn / 2.0;
    }

    /// <summary>
    /// Decision XVIII ("does the count scale with the total at all"). A negative `corr(n, r)` needs no model — if
    /// a cell's own count `k` is steadier than its unit's total `n`, `r = k/n` falls as `n` rises, mechanically, no
    /// matter what generates `k`. The beta-binomial asserts the opposite: `E[k|n] = n p`, proportional. The
    /// elasticity `beta = d log k / d log n` (an OLS line of `log k` on `log n`, replicas with `k = 0` excluded —
    /// the logarithm has no opinion about them — and counted) is near 1 exactly when the count does scale with the
    /// total (conditioning on `n` is right) and near 0 when it does not (the total tells the count nothing, and the
    /// unconditional count predictive this node already routes 40.7 % of Count-governed cells to via `Phi &lt; 1`
    /// is the right model, not a new one). Two guards, both meant to stop a convenient reading: `beta` against
    /// `CV_n` (a total with no spread gives the fit no leverage, and such a cell must not be read as evidence
    /// either way), and a second gate in decision XVII's own spirit — `corr(n, r)` re-derived from `beta` alone
    /// (`r_hat = n^(beta-1)`, the intercept cancelling out of a correlation) against the correlation measured
    /// directly from the same filtered sample; if the elasticity cannot reproduce the correlation, the fit does
    /// not describe these data and the rest is void.
    ///
    /// Decision XVII's own item 2 already refutes `fqdokkarm`'s partition structure as the explanation (the
    /// correlation is negative in every family, offenders and non-offenders alike) — this measurement does not
    /// reopen that; it asks a different question, whether the count scales with the total at all, of every family.
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void DecisionXVIIIElasticityOfCountAgainstTotalByFamily()
    {
        var points = CollectDecisionXVIIIPoints(out var totalCells, out var noFitCells);
        Assert.True(points.Count > 0, "expected at least one cell with a fitted elasticity.");

        _output.WriteLine(
            $"cells in decision XVII's own population: {totalCells}, no elasticity fit possible (fewer than " +
            $"{MinimumContributingReplicasPerCell} non-zero-count replicas, or no spread in n among them)=" +
            $"{noFitCells}, fitted={points.Count}.");

        // Item 1: the gate. If the elasticity cannot reproduce the correlation decision XVII already measured
        // (recomputed here directly from the identical filtered sample, not merely looked up, so the comparison is
        // apples to apples), the fit does not describe these data and the rest of this report is void.
        var maxGateDiscrepancy = points.Max(p => Math.Abs(p.ImpliedCorrelation - p.DirectCorrelation));
        var worstGate = points.OrderByDescending(p => Math.Abs(p.ImpliedCorrelation - p.DirectCorrelation)).First();
        _output.WriteLine(
            $"gate: max|implied corr(n, r_hat) - direct corr(n, r)| over {points.Count} fitted cells = " +
            $"{maxGateDiscrepancy:G6}, worst cell = {worstGate.Family} {worstGate.Formulation} " +
            $"{worstGate.Unit}[{worstGate.Index}] (implied={worstGate.ImpliedCorrelation:G6}, direct={worstGate.DirectCorrelation:G6}).");

        var gateFailures = points.Count(p => Math.Abs(p.ImpliedCorrelation - p.DirectCorrelation) > 0.05);
        _output.WriteLine(
            $"cells where the two correlations differ by more than 0.05: {gateFailures} " +
            $"({(double)gateFailures / points.Count:P2}) — reported, not excluded, since decision XVIII sets no " +
            "per-cell exclusion rule for this gate; a large share here would void the reading below.");

        // Not asked for verbatim, but cheap once the gate exists and directly relevant to reading it honestly:
        // does the gate's own failure concentrate in the low-leverage cells the CV_n guard already warns about, or
        // does it persist even where beta is well determined (a narrow interval, excluding 0)?
        var leveraged = points.Where(p => p.CVnFit >= 0.05).ToList();
        var leveragedFailures = leveraged.Count(p => Math.Abs(p.ImpliedCorrelation - p.DirectCorrelation) > 0.05);
        var significant = points.Where(p => p.ExcludesZero).ToList();
        var significantFailures = significant.Count(p => Math.Abs(p.ImpliedCorrelation - p.DirectCorrelation) > 0.05);
        _output.WriteLine(
            $"restricted to CV_n >= 0.05 (leveraged, {leveraged.Count} cells): {leveragedFailures} " +
            $"({(leveraged.Count > 0 ? (double)leveragedFailures / leveraged.Count : double.NaN):P2}) differ by more " +
            $"than 0.05. Restricted to beta's own 95 % interval excluding 0 ({significant.Count} cells): " +
            $"{significantFailures} ({(significant.Count > 0 ? (double)significantFailures / significant.Count : double.NaN):P2}) " +
            "differ by more than 0.05.");

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- beta = d log k / d log n, by family, offending vs. other cells (median [P10, P90]) ---");
        foreach (var family in points.Select(p => p.Family).Distinct().OrderBy(f => f, StringComparer.Ordinal))
        {
            var familyPoints = points.Where(p => p.Family == family).ToList();
            var offending = familyPoints.Where(p => p.Offending).Select(p => p.Beta).ToList();
            var other = familyPoints.Where(p => !p.Offending).Select(p => p.Beta).ToList();
            ReportCovariateSplit(family, offending, other);
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- share of cells whose 95 % interval for beta excludes 1, and separately excludes 0, by family ---");
        foreach (var family in points.Select(p => p.Family).Distinct().OrderBy(f => f, StringComparer.Ordinal))
        {
            var familyPoints = points.Where(p => p.Family == family).ToList();
            var excludesOne = familyPoints.Count(p => p.ExcludesOne);
            var excludesZero = familyPoints.Count(p => p.ExcludesZero);
            _output.WriteLine(
                $"{family,-14} N={familyPoints.Count,6} excludes 1={excludesOne,5} ({(double)excludesOne / familyPoints.Count:P2}) " +
                $"excludes 0={excludesZero,5} ({(double)excludesZero / familyPoints.Count:P2})");
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine("--- beta against CV_n (leverage guard): by CV_n decile, pooled every family ---");
        var byCVn = points.OrderBy(p => p.CVnFit).ToList();
        for (var d = 1; d <= 10; d++)
        {
            var lo = (d - 1) * byCVn.Count / 10;
            var hi = d == 10 ? byCVn.Count : d * byCVn.Count / 10;
            if (hi <= lo)
            {
                continue;
            }

            var bucket = byCVn.GetRange(lo, hi - lo);
            var betas = bucket.Select(p => p.Beta).OrderBy(x => x).ToList();
            _output.WriteLine(
                $"CV_n decile {d,2} N={betas.Count,6} CV_n range=[{bucket[0].CVnFit:G4}, {bucket[^1].CVnFit:G4}] " +
                $"median(beta)={Median(betas):G4}");
        }

        var lowLeverage = points.Count(p => p.CVnFit < 0.05);
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"{lowLeverage} of {points.Count} fitted cells ({(double)lowLeverage / points.Count:P2}) have CV_n < 0.05 " +
            "(the unit's own total barely varies across its contributing replicas) — read the low CV_n deciles above " +
            "as having little leverage on beta, not as evidence for either model.");
    }

    private readonly record struct ElasticityFit(
        int FilteredCount, int ExcludedZeroCount, double Beta, double SEBeta, int Df, bool ExcludesOne,
        bool ExcludesZero, double CVnFit, double ImpliedCorrelation, double DirectCorrelation);

    // Decision XVIII's own OLS fit of log k on log n, replicas with k = 0 excluded (counted, not silently
    // dropped). Population (1/N) moment conventions throughout, matching CorrelationStats and decision XVII's own
    // identity. `t`-based confidence interval for beta reuses StudentDistribution.TwoSidedQuantile (Harness's own
    // API, already the criterion's own quantile source) rather than a second quantile implementation.
    // `measuredCorrelation` is decision XVII's own `CentreMeasurement.Correlation` for the identical cell, computed
    // over the identical, unfiltered `ns` — passed in rather than recomputed, so "the correlation already measured"
    // in the gate below is the literal quantity that phrase names, not a fresh statistic over a different
    // (k > 0 only) population that would not be an apples-to-apples comparison.
    private static bool TryFitElasticity(List<double> ks, List<double> ns, double measuredCorrelation, out ElasticityFit fit)
    {
        fit = default;

        var xs = new List<double>();
        var ys = new List<double>();
        var filteredNs = new List<double>();
        var excludedZero = 0;
        for (var i = 0; i < ks.Count; i++)
        {
            if (ks[i] <= 0.0)
            {
                excludedZero++;
                continue;
            }

            xs.Add(Math.Log(ns[i]));
            ys.Add(Math.Log(ks[i]));
            filteredNs.Add(ns[i]);
        }

        var count = xs.Count;
        if (count < MinimumContributingReplicasPerCell)
        {
            return false;
        }

        var (Correlation, MeanX, SdX, MeanY, SdY) = CorrelationStats(CollectionsMarshal.AsSpan(xs), CollectionsMarshal.AsSpan(ys));
        if (SdX <= 0.0)
        {
            return false;
        }

        // beta = Cov(x,y) / Var(x) = corr(x,y) * sd_y / sd_x — the same moments CorrelationStats already computed,
        // not a second pass over xs/ys.
        var beta = Correlation * SdY / SdX;
        var intercept = MeanY - beta * MeanX;

        var ssr = 0.0;
        for (var i = 0; i < count; i++)
        {
            var residual = ys[i] - (intercept + beta * xs[i]);
            ssr += residual * residual;
        }

        var df = count - 2;
        if (df <= 0)
        {
            return false;
        }

        var sumSquaredDeviationsX = SdX * SdX * count;
        var s2 = ssr / df;
        var seBeta = Math.Sqrt(s2 / sumSquaredDeviationsX);
        var tCrit = StudentDistribution.TwoSidedQuantile(df, Alpha);
        var ciLow = beta - tCrit * seBeta;
        var ciHigh = beta + tCrit * seBeta;
        var excludesOne = 1.0 < ciLow || 1.0 > ciHigh;
        var excludesZero = 0.0 < ciLow || 0.0 > ciHigh;

        var nStats = CorrelationStats(CollectionsMarshal.AsSpan(filteredNs), CollectionsMarshal.AsSpan(filteredNs));
        var cvNFit = nStats.MeanX > 0.0 ? nStats.SdX / nStats.MeanX : double.NaN;

        // Decision XVIII's own gate: corr(n, r) implied by the fitted line alone, against "the correlation already
        // measured" — decision XVII's own `Correlation`, the literal same quantity, not a fresh one. r_hat =
        // n^(beta-1) (the fitted intercept cancels out of any correlation, so it is omitted, not merely unused),
        // evaluated at every contributing replica's own total — the *full* `ns`, decision XVII's own population,
        // not the `k > 0` subsample the OLS fit itself needed (that subsample has no zero-count replica to give
        // `log k` an opinion about, but the gate compares like with like: the same population on both sides).
        var rHatFull = new List<double>(ns.Count);
        foreach (var n in ns)
        {
            rHatFull.Add(Math.Pow(n, beta - 1.0));
        }

        var impliedCorrelation = CorrelationStats(CollectionsMarshal.AsSpan(ns), CollectionsMarshal.AsSpan(rHatFull)).Correlation;

        fit = new ElasticityFit(count, excludedZero, beta, seBeta, df, excludesOne, excludesZero, cvNFit, impliedCorrelation, measuredCorrelation);
        return true;
    }

    private readonly record struct DecisionXVIIIPoint(
        string Family, string Formulation, string Unit, int Index, bool Offending, double Beta, double SEBeta,
        bool ExcludesOne, bool ExcludesZero, double CVnFit, double ImpliedCorrelation, double DirectCorrelation);

    // Same outer gathering shape as CollectDecisionXVIIPoints (canonical fqdokkarm rows, plain reconstruction for
    // every sibling), sharing TryMeasureCell (for the offending flag, decision XV/XVI/XVII's own definition) and
    // TryCollectContributingCounts/TryFitElasticity (this decision's own) rather than a second population.
    private static List<DecisionXVIIIPoint> CollectDecisionXVIIIPoints(out int totalCells, out int noFitCells)
    {
        var points = new List<DecisionXVIIIPoint>();
        totalCells = 0;
        noFitCells = 0;

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            var unitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                if (!unitCache.TryGetValue(v.Name, out var cached))
                {
                    var rowMatch = FqDokKarmRow.Match(v.Name);
                    if (rowMatch.Success)
                    {
                        var rowIndex0 = int.Parse(
                            rowMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                        var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                        cached = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);
                    }
                    else
                    {
                        cached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    }

                    unitCache[v.Name] = cached;
                }

                if (cached is not { } unit || unit.Dispersion.Phi < 1.0)
                {
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var measurement))
                {
                    continue;
                }

                totalCells++;
                var ratio = measurement.MuModel / measurement.MuEmp;
                var offending = ratio is > 1.5 or < (1.0 / 1.5);

                if (!TryCollectContributingCounts(unit, v.Index, out var ks, out var ns)
                    || !TryFitElasticity(ks, ns, measurement.Correlation, out var fit))
                {
                    noFitCells++;
                    continue;
                }

                points.Add(new DecisionXVIIIPoint(
                    FamilyOf(v.Name), formulation, v.Name, v.Index, offending, fit.Beta, fit.SEBeta,
                    fit.ExcludesOne, fit.ExcludesZero, fit.CVnFit, fit.ImpliedCorrelation, fit.DirectCorrelation));
            }
        }

        return points;
    }

    // Decision XIX ("the same question, asked where the power is"): a generic inverse-CDF sampler over
    // `BetaBinomialPredictive.Pmf`'s own full-support array — a standard sampling technique, not a
    // second implementation of the beta-binomial formula itself (root BOOT.md Taboos), the same kind of restated
    // primitive as `CorrelationStats` above. `cumulative[^1]` is pinned to `1.0` defensively against
    // floating-point summation drift over a long pmf array, so a draw arbitrarily close to `1` always resolves to
    // a valid index instead of walking past the end of the array.
    private static double[] CumulativeOf(double[] pmf)
    {
        var cumulative = new double[pmf.Length];
        var running = 0.0;
        for (var i = 0; i < pmf.Length; i++)
        {
            running += pmf[i];
            cumulative[i] = running;
        }

        cumulative[^1] = 1.0;
        return cumulative;
    }

    private static long SampleFromCumulative(double[] cumulative, double u)
    {
        var lo = 0;
        var hi = cumulative.Length - 1;
        while (lo < hi)
        {
            var mid = lo + (hi - lo) / 2;
            if (cumulative[mid] >= u)
            {
                hi = mid;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return lo;
    }

    // Decision XIX's own Fisher transform, clamped strictly inside (-1, 1) first: a simulated cell can, rarely,
    // produce a correlation numerically equal to +-1 (a degenerate resample among a handful of points), and an
    // unclamped atanh there would be infinite and corrupt the whole group's pooled mean. The clamp margin (1e-6)
    // is far inside the smallest real spread this file has ever measured (decision XVII's own gate, 3.33e-16) and
    // in practice only ever bites a simulated cell, never an observed one (observed cells with |corr| >= 1 are
    // excluded from the population before this is called — see `CollectDecisionXIXCells`).
    private static double FisherZ(double correlation) => Math.Atanh(Math.Clamp(correlation, -1.0 + 1e-6, 1.0 - 1e-6));

    private readonly record struct DecisionXIXCell(
        string Family, string Formulation, string Unit, int Index, bool Offending, double ObservedCorrelation,
        double PHatWeighted, double RhoHat, List<double> Ns);

    // Decision XIX's own population: the identical gate `CollectDecisionXVIIPoints` applies (Count-governed,
    // `Phi >= 1.0`, `TryMeasureCell` succeeds — the beta-binomial-parameterised cells, the ones decision XVII's
    // own identity actually describes), plus the raw per-replica totals (`Ns`) and the unit's own `RhoHat`, which
    // the null simulation needs and the earlier decisions' point records do not carry. A separate collector, not a
    // widened `DecisionXVIIPoint`, the same choice `CollectDecisionXVIIIPoints` already made for its own extra
    // fields — never a second implementation of the gathering itself, since every gate reused here
    // (`TryMeasureCell`, `TryCollectContributingCounts`, `BuildCanonicalRowMembership`, `ReconstructUnit`) is the
    // identical function decision XVII and XVIII already call. A cell whose observed `corr(n, r)` is not finite,
    // or sits at +-1 exactly (no Fisher transform possible, decision XVII's own gate never rules this out), is
    // excluded and counted, never silently folded into the population.
    private static List<DecisionXIXCell> CollectDecisionXIXCells(out int excludedNonFinite)
    {
        var cells = new List<DecisionXIXCell>();
        excludedNonFinite = 0;

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            var unitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                if (!unitCache.TryGetValue(v.Name, out var cached))
                {
                    var rowMatch = FqDokKarmRow.Match(v.Name);
                    if (rowMatch.Success)
                    {
                        var rowIndex0 = int.Parse(
                            rowMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                        var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                        cached = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);
                    }
                    else
                    {
                        cached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    }

                    unitCache[v.Name] = cached;
                }

                if (cached is not { } unit || unit.Dispersion.Phi < 1.0)
                {
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var measurement))
                {
                    continue;
                }

                if (!double.IsFinite(measurement.Correlation) || Math.Abs(measurement.Correlation) >= 1.0)
                {
                    excludedNonFinite++;
                    continue;
                }

                if (!TryCollectContributingCounts(unit, v.Index, out _, out var ns))
                {
                    continue;
                }

                var ratio = measurement.MuModel / measurement.MuEmp;
                cells.Add(new DecisionXIXCell(
                    FamilyOf(v.Name), formulation, v.Name, v.Index, ratio is > 1.5 or < (1.0 / 1.5),
                    measurement.Correlation, measurement.PHatWeighted, unit.Dispersion.RhoHat, ns));
            }
        }

        return cells;
    }

    /// <summary>
    /// Decision XIX ("the same question, asked where the power is", `tests/Harness/BOOT.md`): decision XVIII's own
    /// per-cell elasticity fit could not settle whether the beta-binomial's own claim <c>E[k|n] = n p</c> holds,
    /// because a single cell's own handful of replicas gives the fit no power. This measurement moves the question
    /// to where the population has power: decision XVII's own item 1 identity gives every Count-governed cell's
    /// <c>corr(n, r)</c>, negative in every family, on offending and non-offending cells alike, over more than a
    /// thousand cells at once — one cell's own sign is noise, a thousand cells averaging negative is not, and it is
    /// testable exactly by simulating the null the model itself asserts.
    ///
    /// <b>Item 1, the pooled statistic.</b> Per (family, offending/non-offending) group, the mean of the Fisher
    /// transform (<see cref="FisherZ"/>) of each cell's own <c>corr(n, r)</c> (decision XVII's own
    /// <see cref="CentreMeasurement.Correlation"/>, read off <see cref="CollectDecisionXIXCells"/>, never
    /// recomputed), with the standard error the group's own cell count implies (<c>sd(z) / sqrt(M)</c>) — an
    /// ordinary one-sample statistic over the population decision XVII already built, not a new estimator.
    ///
    /// <b>Item 2, the gate.</b> For each cell, <see cref="BetaBinomialPredictive.Pmf"/> gives the exact
    /// null distribution of a simulated count at each contributing replica's own, observed total <c>n_i</c>
    /// (<see cref="DecisionXIXCell.Ns"/>), the cell's own <c>p_hat</c>
    /// (<see cref="DecisionXIXCell.PHatWeighted"/>) and the cell's own unit-level <c>rho_hat</c>
    /// (<see cref="DecisionXIXCell.RhoHat"/>) — the model's own claim, at this cell's own sample size, nothing
    /// invented. A seeded (<c>NullSimulationSeed</c>, recorded below so the run is reproducible) inverse-CDF draw
    /// (<see cref="SampleFromCumulative"/>) turns that pmf into a simulated count for each contributing replica,
    /// <c>Replications</c> times; each replication recomputes the simulated cell's own <c>corr(n, r_sim)</c> (the
    /// identical <see cref="CorrelationStats"/> primitive decision XVII's own gate uses) and then item 1's own
    /// pooled statistic, giving the null distribution of that pooled statistic directly from resampling rather
    /// than from an assumed asymptotic formula.
    ///
    /// <b>Why the gate can come out either way</b> (checked before running, per decision XIX's own "on the shape of
    /// the gate"): the null is built by holding every cell's own <c>n_i</c> fixed at its real, observed value and
    /// drawing only <c>k_i</c> from the model. If <c>E[k|n] = n p</c> is right and the population's negative signs
    /// are the ordinary finite-sample behaviour of a Pearson correlation computed between a fixed, unequal set of
    /// <c>n_i</c> and a heteroscedastic count-derived rate (whose own sampling variance shrinks as <c>n_i</c>
    /// grows — a real, structural source of finite-sample correlation even when the true relation is flat), the
    /// simulated null will itself be centred away from zero and wide enough to contain the observed pooled
    /// statistic: a real possibility, not a guaranteed one, since nothing forces the null's own spread to be large
    /// before it is measured — a family with little spread in <c>n_i</c>, or a large, well-resolved <c>n_i</c>
    /// throughout, would instead produce a null clustered tightly near zero. If instead the true relation between
    /// the total and the rate is stronger than the model allows (the total genuinely predicts the rate, not only
    /// through finite-sample noise, e.g. a genuine row-partition effect), the null — drawn from counts independent
    /// of <c>n</c> given the model's own dispersion — clusters near zero regardless of the <c>n_i</c> spread, while
    /// the observed statistic, carrying the real relation, sits far outside it. Both outcomes are reachable by
    /// construction from the same code path; nothing in the simulation is tuned toward either, and which one
    /// obtains is exactly what is measured below, not assumed going in.
    ///
    /// <b>Item 3, the same simulation's own spread.</b> Pooled over every (cell, replication) pair of a group, the
    /// median <c>|corr_sim|</c> the model itself produces by chance at this node's own <c>R</c> — reported beside
    /// the group's own observed median <c>|corr(n, r)|</c> (recomputed here, not merely looked up from decision
    /// XVII's own table, so the two figures on one report line are provably the same population), free once item
    /// 2's own simulation exists, per decision XIX's own text: if the two are of the same order, the tail that has
    /// driven four decisions is the diagnostic's own sampling noise, amplified by large coefficients of variation,
    /// not a defect of the criterion.
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void DecisionXIXPooledCorrelationAgainstItsSimulatedNull()
    {
        const int Replications = 1000;
        const int NullSimulationSeed = 20260920;

        var cells = CollectDecisionXIXCells(out var excludedNonFinite);
        Assert.True(cells.Count > 0, "expected at least one measurable Count-governed cell.");

        _output.WriteLine(
            $"decision XIX population: {cells.Count} cells with a finite, non-degenerate corr(n, r); " +
            $"{excludedNonFinite} excluded (non-finite corr, or |corr| = 1 exactly). Replications={Replications}, seed={NullSimulationSeed}.");

        var rng = new SplitMix64(NullSimulationSeed);
        var simulatedCorrelations = new double[cells.Count][];

        for (var c = 0; c < cells.Count; c++)
        {
            var cell = cells[c];
            var count = cell.Ns.Count;
            var nLongs = new long[count];
            var cumulativeByN = new Dictionary<long, double[]>();
            for (var i = 0; i < count; i++)
            {
                var nLong = (long)Math.Round(cell.Ns[i]);
                nLongs[i] = nLong;
                if (!cumulativeByN.ContainsKey(nLong))
                {
                    var pmf = BetaBinomialPredictive.Pmf(nLong, cell.PHatWeighted, cell.RhoHat);
                    cumulativeByN[nLong] = CumulativeOf(pmf);
                }
            }

            var reps = new double[Replications];
            var rsSim = new double[count];
            for (var rep = 0; rep < Replications; rep++)
            {
                for (var i = 0; i < count; i++)
                {
                    var cumulative = cumulativeByN[nLongs[i]];
                    var kSim = SampleFromCumulative(cumulative, rng.NextDouble());
                    rsSim[i] = cell.Ns[i] > 0.0 ? kSim / cell.Ns[i] : 0.0;
                }

                reps[rep] = CorrelationStats(CollectionsMarshal.AsSpan(cell.Ns), rsSim).Correlation;
            }

            simulatedCorrelations[c] = reps;
        }

        _output.WriteLine(string.Empty);
        _output.WriteLine(
            "--- decision XIX: pooled Fisher-z statistic, observed vs. its own simulated null, by family and offending/non-offending ---");

        foreach (var family in cells.Select(cell => cell.Family).Distinct().OrderBy(f => f, StringComparer.Ordinal))
        {
            foreach (var offending in new[] { true, false })
            {
                var indices = Enumerable.Range(0, cells.Count)
                    .Where(i => cells[i].Family == family && cells[i].Offending == offending)
                    .ToList();

                ReportDecisionXIXGroup(family, offending, cells, simulatedCorrelations, indices, Replications);
            }
        }
    }

    // Decision XIX's own per-group report: item 1 (observed pooled Fisher-z vs. its simulated null, with the
    // rank-based two-sided p-value and z-score the null distribution above gives directly) and item 3 (the two
    // |corr| medians, observed and simulated-null-pooled, on the same line so the comparison decision XIX's own
    // text asks for reads off one row).
    private void ReportDecisionXIXGroup(
        string family, bool offending, List<DecisionXIXCell> cells, double[][] simulatedCorrelations,
        List<int> indices, int replications)
    {
        var label = $"{family} / {(offending ? "offending" : "other")}";
        if (indices.Count == 0)
        {
            _output.WriteLine($"{label,-24} N=    0 (none)");
            return;
        }

        var observedCorrelations = indices.Select(i => cells[i].ObservedCorrelation).ToList();
        var observedZs = observedCorrelations.Select(FisherZ).ToList();
        var observedPooled = observedZs.Average();
        var observedSE = double.NaN;
        if (observedZs.Count > 1)
        {
            var observedVariance = observedZs.Sum(z => (z - observedPooled) * (z - observedPooled)) / (observedZs.Count - 1);
            observedSE = Math.Sqrt(observedVariance) / Math.Sqrt(observedZs.Count);
        }

        // A simulated cell's own resample can be degenerate — every replica draws the identical count (most often
        // all-zero, for a sparse cell whose p_hat is small), giving CorrelationStats a zero-variance rate and
        // therefore a NaN correlation for that one replication. This is a genuine feature of the null at a sparse
        // cell, not a bug: dropping that one cell from that one replication's pooled mean (rather than letting the
        // NaN poison the whole replication's average, which is what a naive `Average()` over the group would do)
        // is the honest treatment — a degenerate resample carries no information about correlation in that
        // replication and is excluded from it, the same "resolvable cell" principle `TryInferRunQuantum` and this
        // file's own count-floor machinery already apply elsewhere. `validCellsPerReplication` is reported so a
        // reader can see how much of the group survives per replication, not left implicit.
        var nullPooled = new List<double>(replications);
        var validCellsPerReplication = new List<int>(replications);
        for (var rep = 0; rep < replications; rep++)
        {
            var sum = 0.0;
            var validCount = 0;
            foreach (var i in indices)
            {
                var z = FisherZ(simulatedCorrelations[i][rep]);
                if (double.IsNaN(z))
                {
                    continue;
                }

                sum += z;
                validCount++;
            }

            validCellsPerReplication.Add(validCount);
            if (validCount > 0)
            {
                nullPooled.Add(sum / validCount);
            }
        }

        var usableReplications = nullPooled.Count;
        var nullMean = usableReplications > 0 ? nullPooled.Average() : double.NaN;
        var nullSd = double.NaN;
        if (usableReplications > 1)
        {
            var nullVariance = nullPooled.Sum(x => (x - nullMean) * (x - nullMean)) / (usableReplications - 1);
            nullSd = Math.Sqrt(nullVariance);
        }

        var countLE = nullPooled.Count(x => x <= observedPooled);
        var countGE = nullPooled.Count(x => x >= observedPooled);
        var pTwoSided = usableReplications > 0
            ? Math.Min(1.0, 2.0 * Math.Min((double)countLE, countGE) / usableReplications)
            : double.NaN;
        var zScore = nullSd > 0.0 ? (observedPooled - nullMean) / nullSd : double.NaN;

        var pooledSimAbs = new List<double>(indices.Count * replications);
        var degenerateSimCells = 0;
        foreach (var i in indices)
        {
            foreach (var r in simulatedCorrelations[i])
            {
                if (double.IsNaN(r))
                {
                    degenerateSimCells++;
                    continue;
                }

                pooledSimAbs.Add(Math.Abs(r));
            }
        }

        pooledSimAbs.Sort();
        var observedAbs = observedCorrelations.Select(Math.Abs).OrderBy(x => x).ToList();
        var verdict = double.IsNaN(pTwoSided)
            ? "no usable replication"
            : pTwoSided >= 0.05 ? "inside the null (not refuted)" : "outside the null (refuted)";
        var meanValidCells = validCellsPerReplication.Count > 0 ? validCellsPerReplication.Average() : double.NaN;

        _output.WriteLine(
            $"{label,-24} N={indices.Count,5} observed pooled z={observedPooled:G6} (SE={observedSE:G4}, " +
            $"corr={Math.Tanh(observedPooled):G4})   null z: mean={nullMean:G6} sd={nullSd:G4}   " +
            $"z-score={zScore:G4} two-sided p={pTwoSided:G4} [{verdict}]");
        _output.WriteLine(
            $"{string.Empty,24}   |corr| median: observed={Median(observedAbs):G4}, " +
            $"simulated null (pooled cell x rep, {(double)degenerateSimCells / (indices.Count * replications):P2} degenerate)=" +
            $"{Median(pooledSimAbs):G4}   usable replications={usableReplications}/{replications}, " +
            $"mean contributing cells/replication={meanValidCells:G4}/{indices.Count}");
    }

    private readonly record struct DecisionXXPoint(
        string Family, string Formulation, string Unit, int Index, double ConditionalWidth,
        double UnconditionalWidth, double WidthRatio);

    // Decision XX ("stop conditioning on a total that does not predict the count", `tests/Harness/BOOT.md`)'s own
    // arm 2: the exact population the routing change moves — Count-governed, `Phi >= 1.0` (the cells that used to
    // reach `BetaBinomialCountFloor` before the change; the `Phi < 1.0` population already used the unconditional
    // predictive and is untouched) — over decision XVII/XIX's own single reference-vs-lagged-replicas population,
    // not `DilutionDiagnosticTests`' larger 96-run leave-one-out one (Check A/B's own population): a different,
    // simpler population than Check A/B's, reported beside it per the decision's own instruction, not a claim that
    // the two populations are identical. Both widths are read from the same two internal interval primitives
    // `EvaluateCells` itself calls (`BetaBinomialInterval`, already computed by `TryMeasureCell` above as
    // `CentreMeasurement.Low`/`High`, and `NegativeBinomialInterval`, called here directly at the identical
    // arguments `CountFloorFromCounts` uses — `sum(counts)`, the contributing replica count, `alpha`) — no second
    // implementation of either formula, and no access to `CountFloorFromCounts` itself needed (`private`,
    // unreachable here; `BetaBinomialCountFloor` no longer exists, removed 2026-09-25), since a width is
    // `High - Low` either way.
    private static List<DecisionXXPoint> CollectDecisionXXPoints()
    {
        var points = new List<DecisionXXPoint>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            var replicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>(replicaCount);
            for (var k = 1; k <= replicaCount; k++)
            {
                replicaCells.Add(ResultsMFile.ParseCells(
                    RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt")));
            }

            var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
            var referenceDict = ResultsMFile.Parse(referencePath);
            var referenceCells = ResultsMFile.ParseCells(referencePath);
            var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
                ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
                : [];

            var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, referenceDict, ReplicaKind.Lagged, Alpha);

            var unitCache = new Dictionary<string, UnitReconstruction?>(StringComparer.Ordinal);

            foreach (var v in verdicts)
            {
                if (v.Rule != CalibrationRule.Count)
                {
                    continue;
                }

                if (!unitCache.TryGetValue(v.Name, out var cached))
                {
                    var rowMatch = FqDokKarmRow.Match(v.Name);
                    if (rowMatch.Success)
                    {
                        var rowIndex0 = int.Parse(
                            rowMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
                        var membership = BuildCanonicalRowMembership(replicaCells, referenceDkarmcat, v.Name, rowIndex0);
                        cached = ReconstructUnit(membership.CanonicalReplicaCells, referenceCells, v.Name);
                    }
                    else
                    {
                        cached = ReconstructUnit(replicaCells, referenceCells, v.Name);
                    }

                    unitCache[v.Name] = cached;
                }

                // Decision XX's own scope: only the population the routing change actually moves. A `Phi < 1.0`
                // unit already used the unconditional predictive before this decision and is unaffected by it.
                if (cached is not { } unit || unit.Dispersion.Phi < 1.0)
                {
                    continue;
                }

                if (!TryMeasureCell(unit, v.Index, out var measurement))
                {
                    continue;
                }

                var conditionalWidth = measurement.High - measurement.Low;
                if (conditionalWidth <= 0.0)
                {
                    continue;
                }

                if (!TryCollectContributingCounts(unit, v.Index, out var ks, out _))
                {
                    continue;
                }

                var sumK = ks.Sum();
                var (Mean, Low, High, AttainedAlpha) = NegativeBinomialPredictive.Interval(sumK, ks.Count, Alpha);
                var unconditionalWidth = High - Low;
                if (unconditionalWidth <= 0.0)
                {
                    continue;
                }

                points.Add(new DecisionXXPoint(
                    FamilyOf(v.Name), formulation, v.Name, v.Index, conditionalWidth, unconditionalWidth,
                    unconditionalWidth / conditionalWidth));
            }
        }

        return points;
    }

    /// <summary>
    /// Decision XX ("stop conditioning on a total that does not predict the count"): arm 2 of the two pre-
    /// registered checks the routing change (production `StatisticalCriterion.EvaluateCells`, "Decision XX") is
    /// judged by. "The interval width must not grow systematically without Check A following it" — the median
    /// width ratio, unconditional (<see cref="NegativeBinomialPredictive.Interval"/>, the predictive
    /// every count-like cell now takes) against conditional (<see cref="BetaBinomialPredictive.Interval"/>,
    /// what the same cell would have taken before this decision), per family, over exactly the cells the routing
    /// change moves (<see cref="CollectDecisionXXPoints"/>). Reported beside `DilutionDiagnosticTests`' own Check A
    /// (a different population, decision XX's own text only requires the two numbers to be read together, not
    /// drawn from one run).
    /// </summary>
    [Trait("Category", "Long")]
    [Fact]
    public void DecisionXXWidthRatioUnconditionalAgainstConditionalByFamily()
    {
        var points = CollectDecisionXXPoints();
        Assert.True(points.Count > 0, "expected at least one Phi>=1.0 Count-governed cell the routing change moves.");

        _output.WriteLine($"decision XX, arm 2: {points.Count} cells (Count-governed, Phi >= 1.0 — the population the routing change moves).");
        _output.WriteLine(string.Empty);
        _output.WriteLine("--- width ratio (unconditional / conditional), by family ---");
        foreach (var group in points.GroupBy(p => p.Family).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            ReportRatioDeciles(group.Key, group.Select(p => p.WidthRatio).ToList());
        }

        _output.WriteLine(string.Empty);
        ReportRatioDeciles("(pooled, every family)", points.Select(p => p.WidthRatio).ToList());

        var grown = points.Count(p => p.WidthRatio > 1.0);
        _output.WriteLine(string.Empty);
        _output.WriteLine(
            $"{grown} of {points.Count} cells ({(double)grown / points.Count:P2}) have a wider unconditional interval " +
            "than the conditional one it replaces.");
    }

    private static bool IsInFirstHalfOfItsRow(
        (string Formulation, string Unit, int RowIndex0, int ColumnIndex, double NStar, double Share, double PHat, double RatioRow, bool Offending) cell,
        List<(string Formulation, string Unit, int RowIndex0, int ColumnIndex, double NStar, double Share, double PHat, double RatioRow, bool Offending)> covariates)
    {
        var rowColumns = covariates.Where(c => c.Formulation == cell.Formulation && c.Unit == cell.Unit)
            .Select(c => c.ColumnIndex).ToList();
        var maxColumn = rowColumns.Max();
        return maxColumn == 0 || cell.ColumnIndex <= maxColumn / 2.0;
    }

    private void ReportCovariateSplit(string label, List<double> offending, List<double> other)
    {
        _output.WriteLine(
            $"{label,-34} offending: N={offending.Count,4} {Summary(offending)}   other: N={other.Count,5} {Summary(other)}");

        static string Summary(List<double> values)
        {
            if (values.Count == 0)
            {
                return "(none)";
            }

            var sorted = values.OrderBy(x => x).ToList();
            var p10 = sorted[Math.Clamp((int)Math.Round(0.10 * (sorted.Count - 1)), 0, sorted.Count - 1)];
            var p90 = sorted[Math.Clamp((int)Math.Round(0.90 * (sorted.Count - 1)), 0, sorted.Count - 1)];
            return $"median={Median(sorted):G4} [P10={p10:G4}, P90={p90:G4}]";
        }
    }

    // Restated, not shared: the identical three-line exact-equality prefix scan `StatisticalCriterion.cs`'s own
    // private `PrefixMatchLength` performs (this node's BOOT.md, "Canonical category axis") — private there,
    // unreachable from this test project, and, like this file's own `Median`/`MBucket` below, a restated
    // primitive rather than a competing implementation of a statistical estimator (root BOOT.md Taboos' ban on
    // a second implementation of any part of the program — a fixed-format token equality scan is not a formula).
    private static int LiteralPrefixMatchLength(double[]? source, double[] reference)
    {
        if (source is null)
        {
            return 0;
        }

        var limit = Math.Min(source.Length, reference.Length);
        var i = 0;
        while (i < limit && source[i] == reference[i])
        {
            i++;
        }

        return i;
    }

    // Item0's own row-eligibility gate (`BuildComparePending`'s `contributingReplicaIndices` for a canonical
    // `fqdokkarm` row: a replica's own `Dkarmcat` prefix must match the reference's strictly past the row's own
    // index, and the replica must actually print the row), extracted so Item1To3 above reuses the identical gate
    // rather than a second copy of it.
    private readonly record struct RowMembership(
        int NaiveCount, int CanonicalCount, List<IReadOnlyDictionary<string, ResultCell[]>> CanonicalReplicaCells);

    private static RowMembership BuildCanonicalRowMembership(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells, double[] referenceDkarmcat,
        string rowName, int rowIndex0)
    {
        var canonicalReplicaCells = new List<IReadOnlyDictionary<string, ResultCell[]>>();
        var naiveCount = 0;
        foreach (var cells in replicaCells)
        {
            var hasKey = cells.ContainsKey(rowName);
            if (hasKey)
            {
                naiveCount++;
            }

            var replicaDkarmcat = cells.TryGetValue("Dkarmcat", out var replicaDkarmcatCells)
                ? Array.ConvertAll(replicaDkarmcatCells, c => c.Value)
                : [];
            if (hasKey && LiteralPrefixMatchLength(replicaDkarmcat, referenceDkarmcat) > rowIndex0)
            {
                canonicalReplicaCells.Add(cells);
            }
        }

        return new RowMembership(naiveCount, canonicalReplicaCells.Count, canonicalReplicaCells);
    }

    private static UnitReconstruction? ReconstructUnit(
        IReadOnlyList<IReadOnlyDictionary<string, ResultCell[]>> replicaCells,
        IReadOnlyDictionary<string, ResultCell[]> referenceCells, string unit)
    {
        var totals = CountReconstruction.ArrayTotals(replicaCells, unit);
        var dispersion = DispersionEstimator.Estimate(replicaCells, unit, totals);
        if (dispersion is not { } d)
        {
            return null;
        }

        var (perReplicaCounts, replicaTotalsList) = CountReconstruction.PerCellCounts(replicaCells, unit, totals);
        if (perReplicaCounts.Count == 0)
        {
            return null;
        }

        if (!referenceCells.TryGetValue(unit, out var referenceArray))
        {
            return null;
        }

        // The candidate's own total, reconstructed through the identical public primitive a replica's total goes
        // through — the reference file wrapped as a singleton "replica" list, never a second implementation of
        // `ReconstructTotal`/`TryInferRunQuantum` (both `private`, unreachable from here; this class-level comment
        // explains why the singleton wrapping is the faithful substitute, not an approximation invented for this
        // file alone).
        var referenceAsSingleton = new List<IReadOnlyDictionary<string, ResultCell[]>>
        {
            new Dictionary<string, ResultCell[]> { [unit] = referenceArray },
        };
        var nStarArr = CountReconstruction.ArrayTotals(referenceAsSingleton, unit);
        if (nStarArr[0] is not { } nStar || nStar <= 0.0)
        {
            return null;
        }

        // phi_T = s^2 / T_bar over the replicas' own reconstructed array totals — `tests/Harness/HISTORY.md`,
        // "Decision X, item 1" / `ArrayTotalDiagnosticTests`' own identical arithmetic (a plain sample variance over mean,
        // not a formula `StatisticalCriterion.cs` itself owns), applied to the same `ReconstructArrayTotals`
        // output that method already uses.
        var produced = totals.Where(t => t is not null).Select(t => t!.Value).ToList();
        var phiT = double.NaN;
        if (produced.Count >= 2)
        {
            var mean = produced.Average();
            var sumSquares = produced.Sum(t => (t - mean) * (t - mean));
            var variance = sumSquares / (produced.Count - 1);
            phiT = mean > 0.0 ? variance / mean : double.NaN;
        }

        // m: the median, across replicas that resolved a quantum for this unit, of the count of non-zero cells
        // that actually determined that replica's own quantum — `ArrayTotalDiagnosticTests`' own notation, reused
        // (not redefined) since it names the same "resolvable cell" gate `TryInferRunQuantum`'s own search uses.
        var perReplicaM = new List<int>();
        foreach (var cells in replicaCells)
        {
            if (!cells.TryGetValue(unit, out var array) || !RunQuantum.TryInfer(array, out var estimate))
            {
                continue;
            }

            var count = 0;
            foreach (var cell in array)
            {
                if (Math.Abs(cell.Value) <= 0.0 || estimate.Quantum <= cell.Resolution)
                {
                    continue;
                }

                count++;
            }

            perReplicaM.Add(count);
        }

        var medianM = double.NaN;
        if (perReplicaM.Count > 0)
        {
            perReplicaM.Sort();
            var mid = perReplicaM.Count / 2;
            medianM = perReplicaM.Count % 2 == 0
                ? (perReplicaM[mid - 1] + perReplicaM[mid]) / 2.0
                : perReplicaM[mid];
        }

        return new UnitReconstruction(d, nStar, perReplicaCounts, replicaTotalsList, phiT, medianM);
    }

    private void ReportRatioDeciles(string label, List<double> ratios)
    {
        if (ratios.Count == 0)
        {
            _output.WriteLine($"{label,-20} (no measurable cells)");
            return;
        }

        ratios.Sort();
        var median = Median(ratios);
        var beyondFactor = ratios.Count(r => r is > 1.5 or < (1.0 / 1.5));
        var deciles = string.Join(", ", Enumerable.Range(1, 9).Select(d => DecileAt(ratios, d).ToString("G4")));
        _output.WriteLine(
            $"{label,-20} N={ratios.Count,6} median={median:G6} beyond-1.5x={beyondFactor,5} " +
            $"({(double)beyondFactor / ratios.Count:P2}) deciles=[{deciles}]");
    }

    // Decision XVII, item 4: the same decile-reporting shape as ReportRatioDeciles above, minus its "beyond 1.5x"
    // framing, which is meaningless for a signed delta not centred on 1. Reuses the same DecileAt/Median this file
    // already has (root BOOT.md Taboos: no second implementation of the same reporting arithmetic).
    private void ReportSignedDeciles(string label, List<double> values)
    {
        if (values.Count == 0)
        {
            _output.WriteLine($"{label,-20} (no cells)");
            return;
        }

        values.Sort();
        var deciles = string.Join(", ", Enumerable.Range(1, 9).Select(d => DecileAt(values, d).ToString("G4")));
        _output.WriteLine($"{label,-20} N={values.Count,6} median={Median(values):G6} deciles=[{deciles}]");
    }

    private void ReportAgainstPhiT(List<CentrePoint> points)
    {
        var finite = points.Where(p => double.IsFinite(p.PhiT)).OrderBy(p => p.PhiT).ToList();
        if (finite.Count == 0)
        {
            _output.WriteLine("(no unit produced a finite phi_T)");
            return;
        }

        for (var d = 1; d <= 10; d++)
        {
            var lo = (d - 1) * finite.Count / 10;
            var hi = d == 10 ? finite.Count : d * finite.Count / 10;
            if (hi <= lo)
            {
                continue;
            }

            var bucket = finite.GetRange(lo, hi - lo);
            var ratios = bucket.Select(p => p.RatioModelOverEmp).OrderBy(x => x).ToList();
            _output.WriteLine(
                $"phi_T decile {d,2} N={ratios.Count,6} phi_T range=[{bucket[0].PhiT:G4}, {bucket[^1].PhiT:G4}] " +
                $"median(ratio)={Median(ratios):G6}");
        }
    }

    private void ReportAgainstM(List<CentrePoint> points)
    {
        var finite = points.Where(p => double.IsFinite(p.MedianM)).ToList();
        if (finite.Count == 0)
        {
            _output.WriteLine("(no unit produced a resolvable m)");
            return;
        }

        foreach (var group in finite.GroupBy(p => MBucket(p.MedianM)).OrderBy(g => MBucketOrder(g.Key)))
        {
            var ratios = group.Select(p => p.RatioModelOverEmp).OrderBy(x => x).ToList();
            _output.WriteLine($"m={group.Key,-4} N={ratios.Count,6} median(ratio)={Median(ratios):G6}");
        }
    }

    // Identical bucket boundaries to `ArrayTotalDiagnosticTests.MBucket` (root BOOT.md Taboos would call a
    // renamed copy of the same rule a second implementation; this is deliberately the same rule, restated in
    // this file the way every diagnostic file here restates its own small reporting arithmetic, e.g. `Median`
    // below, rather than sharing a `Utils` class root BOOT.md's Taboos also forbid).
    private static string MBucket(double medianM)
    {
        if (medianM < 1.5)
        {
            return "1";
        }

        if (medianM < 2.5)
        {
            return "2";
        }

        if (medianM < 3.5)
        {
            return "3";
        }

        if (medianM < 4.5)
        {
            return "4";
        }

        if (medianM < 5.5)
        {
            return "5";
        }

        return medianM < 9.5 ? "6-9" : "10+";
    }

    private static int MBucketOrder(string bucket) => bucket switch
    {
        "1" => 1,
        "2" => 2,
        "3" => 3,
        "4" => 4,
        "5" => 5,
        "6-9" => 6,
        _ => 7,
    };

    private static double DecileAt(List<double> sorted, int decile)
    {
        var index = Math.Clamp((int)Math.Round(decile / 10.0 * (sorted.Count - 1)), 0, sorted.Count - 1);
        return sorted[index];
    }

    // NaN for an empty sample: after B2a (`tests/Harness/HISTORY.md#b2a-amended-2026-10-02`) a group can be so small
    // that every simulated cell is degenerate, and a report must print that, not throw on it.
    private static double Median(List<double> sorted)
    {
        if (sorted.Count == 0)
        {
            return double.NaN;
        }

        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }
}
