using System.Globalization;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// The B2b report (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`, arbiter verdict of 2026-10-02, "B2b:
/// deferred": "region exits per family reported beside that factor (never a band)"): for every formulation, level
/// and family of the `Count` rule's own scoreable cells (the cells of the calibration curve's `Count` row), how
/// many cells there are, how many exit the predictive's region, the level the predictive credits them with, and the
/// replicas' own between-run factor `1 + (n_bar - 1) rho` beside it. A report, never a band, never asserted
/// except for the identity <see cref="VerifyAgainstRow"/> checks: that the families add up to the curve's own row.
///
/// What is counted. A cell enters when <c>CalibrationCurveTests</c> puts it in the `Count` row: not degenerate,
/// eligible, <see cref="CalibrationRule.Count"/> and with an attained level. `K` is such a cell whose verdict
/// failed and whose count is plausible (<see cref="CellVerdict.ImplausibleCount"/> false), i.e. whose reconstructed
/// count lies outside the predictive's region after B2c's shift `s`; `Implausible` is the failures the printed
/// value alone forced (not a count of the quantum), reported beside it so that `K + Implausible` is the row's own
/// `K`. The credited mass is the sum of <see cref="CellVerdict.AttainedAlpha"/> over the family's cells, the row's
/// own `p` times its `N`.
///
/// The factor. `rho` is <see cref="DispersionEstimator.Estimate"/>'s robust `RhoHat` (the median of the replicas' own
/// per-cell estimates, <see cref="CellRho"/>) and `n_bar` its mean array total, both from the replicas alone, the
/// whole set of `R` replicas of the formulation (the fqdokkarm rows over the replicas whose category axis matches the
/// reference's, as <see cref="DispersionTable"/> does); the rows are those <see cref="DispersionTable.Compute"/>
/// prints, read back from its text, never a second estimation. The array's factor is attached to each of its cells;
/// a family's factor is the mean of its cells' factors weighted by their credited level, with the smallest and
/// largest factor beside it. A cell of an array the table has no row for (a sparse array, a tail-row or adaptive
/// cell) carries no factor and is counted apart (<c>noFactor</c>).
/// </summary>
internal sealed class CountCoverageReport
{
    private const double IdentityTolerance = 1e-9;

    private readonly Dictionary<(string Formulation, double Alpha, string Family), FamilyTally> _tallies = [];
    private readonly Dictionary<(string Formulation, string Array), double> _factors;

    private CountCoverageReport(Dictionary<(string Formulation, string Array), double> factors)
    {
        _factors = factors;
    }

    private sealed class FamilyTally
    {
        public long Cells;
        public long Exits;
        public long Implausible;
        public double Credited;
        public long WithFactor;
        public double FactorWeighted;
        public double FactorWeight;
        public double FactorMin = double.PositiveInfinity;
        public double FactorMax = double.NegativeInfinity;

        public long Failures => Exits + Implausible;

        public void Merge(FamilyTally other)
        {
            Cells += other.Cells;
            Exits += other.Exits;
            Implausible += other.Implausible;
            Credited += other.Credited;
            WithFactor += other.WithFactor;
            FactorWeighted += other.FactorWeighted;
            FactorWeight += other.FactorWeight;
            FactorMin = Math.Min(FactorMin, other.FactorMin);
            FactorMax = Math.Max(FactorMax, other.FactorMax);
        }
    }

    /// <summary>Reads the replicas' robust factor of every array <see cref="DispersionTable"/> has a row for.</summary>
    public static CountCoverageReport Create()
    {
        var factors = new Dictionary<(string, string), double>();
        foreach (var line in DispersionTable.Compute())
        {
            if (line.StartsWith('#'))
            {
                continue;
            }

            // "formulation array runs groups n_bar phi rho_hat rho_hat_spread route"
            var fields = line.Split(' ');
            var nBar = double.Parse(fields[4], CultureInfo.InvariantCulture);
            var rhoHat = double.Parse(fields[6], CultureInfo.InvariantCulture);
            factors[(fields[0], fields[1])] = 1.0 + (nBar - 1.0) * rhoHat;
        }

        return new CountCoverageReport(factors);
    }

    /// <summary>Adds one cell of the curve's `Count` row.</summary>
    public void Add(string formulation, double alpha, CellVerdict verdict)
    {
        var family = QuantityFamilies.TryMatchFqDokKarmRow(verdict.Name, out _) ? "fqdokkarm" : verdict.Name;
        if (!_tallies.TryGetValue((formulation, alpha, family), out var tally))
        {
            tally = new FamilyTally();
            _tallies[(formulation, alpha, family)] = tally;
        }

        var credited = verdict.AttainedAlpha!.Value;
        tally.Cells++;
        tally.Credited += credited;
        if (verdict.Failed && verdict.ImplausibleCount)
        {
            tally.Implausible++;
        }
        else if (verdict.Failed)
        {
            tally.Exits++;
        }

        if (_factors.TryGetValue((formulation, verdict.Name), out var factor))
        {
            tally.WithFactor++;
            tally.FactorWeighted += credited * factor;
            tally.FactorWeight += credited;
            tally.FactorMin = Math.Min(tally.FactorMin, factor);
            tally.FactorMax = Math.Max(tally.FactorMax, factor);
        }
    }

    /// <summary>
    /// The identity that makes the report read what it says: the families of one formulation and level add up to the
    /// curve's own `Count` row, in cells, failures and credited mass. Returns one message per mismatch.
    /// </summary>
    public IEnumerable<string> VerifyAgainstRow(
        string formulation, double alpha, long rowCompared, long rowFailures, double rowCredited)
    {
        var sum = new FamilyTally();
        foreach (var ((f, a, _), tally) in _tallies)
        {
            if (f == formulation && a == alpha)
            {
                sum.Merge(tally);
            }
        }

        return Mismatches($"{formulation} alpha={alpha}", sum, rowCompared, rowFailures, rowCredited);
    }

    /// <summary>The same identity for the 0.001 row, pooled over the formulations.</summary>
    public IEnumerable<string> VerifyPooledAgainstRow(
        double alpha, long rowCompared, long rowFailures, double rowCredited)
    {
        var sum = new FamilyTally();
        foreach (var ((_, a, _), tally) in _tallies)
        {
            if (a == alpha)
            {
                sum.Merge(tally);
            }
        }

        return Mismatches($"(pooled x5) alpha={alpha}", sum, rowCompared, rowFailures, rowCredited);
    }

    private static IEnumerable<string> Mismatches(
        string scope, FamilyTally sum, long rowCompared, long rowFailures, double rowCredited)
    {
        if (sum.Cells != rowCompared)
        {
            yield return $"{scope}: families hold {sum.Cells} cells, the row N={rowCompared}";
        }

        if (sum.Failures != rowFailures)
        {
            yield return $"{scope}: families hold K+implausible={sum.Failures}, the row K={rowFailures}";
        }

        if (Math.Abs(sum.Credited - rowCredited) > IdentityTolerance)
        {
            yield return $"{scope}: families credit {sum.Credited:R}, the row {rowCredited:R}";
        }
    }

    /// <summary>The report as table lines, formulations in the given order, then the pooled rows.</summary>
    public IEnumerable<string> Render(IReadOnlyList<string> formulations, IReadOnlyList<double> levels)
    {
        yield return "Count coverage per family (B2b report, never a band; K = exits of the region after B2c's shift, " +
                     "implausible = failures forced by the printed value, credited = sum of AttainedAlpha, " +
                     "factor = 1 + (n_bar - 1) rho_hat from the replicas, credited-weighted mean [min, max]):";
        yield return $"{"scope",-11} {"alpha",-6} {"family",-18} {"N",6} {"K",5} {"impl",4} {"credited",9} {"K/cred",7} {"factor (mean [min, max])",-28} noFactor";

        foreach (var alpha in levels)
        {
            foreach (var formulation in formulations)
            {
                foreach (var line in RenderScope(formulation, formulation, alpha))
                {
                    yield return line;
                }
            }

            foreach (var line in RenderScope("(pooled x5)", null, alpha))
            {
                yield return line;
            }
        }
    }

    private IEnumerable<string> RenderScope(string scope, string? formulation, double alpha)
    {
        var families = new SortedDictionary<string, FamilyTally>(StringComparer.Ordinal);
        var total = new FamilyTally();
        foreach (var ((f, a, family), tally) in _tallies)
        {
            if (a != alpha || formulation is not null && f != formulation)
            {
                continue;
            }

            if (!families.TryGetValue(family, out var merged))
            {
                merged = new FamilyTally();
                families[family] = merged;
            }

            merged.Merge(tally);
            total.Merge(tally);
        }

        foreach (var (family, tally) in families)
        {
            yield return Line(scope, alpha, family, tally);
        }

        yield return Line(scope, alpha, "(all families)", total);
    }

    private static string Line(string scope, double alpha, string family, FamilyTally tally)
    {
        var ratio = tally.Credited > 0.0
            ? (tally.Exits / tally.Credited).ToString("F2", CultureInfo.InvariantCulture)
            : "n/a";
        var factor = tally.WithFactor == 0
            ? "n/a"
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{tally.FactorWeighted / tally.FactorWeight:F2} [{tally.FactorMin:F2}, {tally.FactorMax:F2}]");
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{scope,-11} {alpha,-6} {family,-18} {tally.Cells,6} {tally.Exits,5} {tally.Implausible,4} {tally.Credited,9:F2} {ratio,7} {factor,-28} {tally.Cells - tally.WithFactor}");
    }
}
