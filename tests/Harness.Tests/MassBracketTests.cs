using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// E1 (2026-09-27, `tests/Harness/BOOT.md`, "Mass bracket"; `tests/Harness/HISTORY.md#e1-mass-bracket-feasible-interval`): the
/// mass-family bracket (<see cref="MassFamilyRule.TryBracket"/>, through
/// <see cref="StatisticalCriterion.CompareCellVerdicts"/>) is stated as deterministic and alpha-free, yet the
/// point-estimate quantum it replaced (the withdrawn <c>EstimateMassQuantum</c>) failed 5 of the original's 197
/// runs and 2 of the port's 160 — the bracket's own half-print-unit tolerance ignored the a-priori error of its
/// own estimates. This class is the right control (no genuine run of the original may ever fail a Mass-rule
/// cell) and the cell-level proof that the fix's own boundary is real on both sides, not merely widened until the
/// 7 known cells happened to pass.
/// </summary>
public class MassBracketTests
{
    // The candidate every cell test below mutates: HPEPA3 lagged replica 26 against the other 31 lagged
    // replicas (`excludeReplicaOrdinal: 26`) — one of the 7 cells the point estimate measurably failed
    // (`fmkarm[66]`, n = 1). Cloned per call so each test mutates its own copy, never a shared array.
    private static Dictionary<string, double[]> Hpepa3Replica26WithFmkarm66(double value)
    {
        var candidate = new Dictionary<string, double[]>(ResultsMFile.Parse(FixtureReplicas.ReplicaPath("HPEPA3", ReplicaKind.Lagged, 26)));
        var fmkarm = (double[])candidate["fmkarm"].Clone();
        fmkarm[66] = value;
        candidate["fmkarm"] = fmkarm;
        return candidate;
    }

    private static CellVerdict Fmkarm66Verdict(IReadOnlyDictionary<string, double[]> candidate)
    {
        var verdicts = StatisticalCriterion.CompareCellVerdicts("HPEPA3", candidate, ReplicaKind.Lagged, StatisticalCriterion.Alpha, excludeReplicaOrdinal: 26);
        return verdicts.Single(v => v.Name == "fmkarm" && v.Index == 66);
    }

    private static bool RelativelyClose(double actual, double expected, double relativeTolerance) =>
        Math.Abs(actual - expected) <= relativeTolerance * Math.Max(Math.Abs(actual), Math.Abs(expected));

    // The right control (root BOOT.md Taboos: "every check that guards a quantitative claim is proven twice"):
    // over every run of the original's own 197-run null (`tests/Harness/BOOT.md`, "## Null rate of the
    // original" — the same 96 lagged leave-one-out, 96 independent leave-one-out and 5 lagged reference-vs-R
    // runs `NullRateCalibration.AllOriginalRuns()` pools), no Mass-rule cell may ever fail: if the bracket's own
    // containment model holds (one quantum per array, printing rounds to nearest), a genuine run cannot fail it.
    // Also names the 5 cells the point estimate used to fail, now present and passing.
    [Trait("Category", "Long")]
    [Fact]
    public void NoRunOfTheOriginalFailsAMassRuleCell()
    {
        var knownCells = new HashSet<string>(StringComparer.Ordinal)
        {
            "HPEPA3|Lagged|replica 26|fmkarm[66]",
            "inpt|Lagged|replica 10|fmkarm_cor[6]",
            "P33|Lagged|replica 11|fmkarm_cor[12]",
            "P33|Independent|replica 14|fmkarm_cor[14]",
            "HMX|Independent|replica 13|fmkarm_cor[84]",
        };
        var presentKnownCells = new HashSet<string>(StringComparer.Ordinal);
        var failing = new List<string>();

        foreach (var (formulation, replicaCount) in ResultsMFileTests.Formulations)
        {
            foreach (var kind in new[] { ReplicaKind.Lagged, ReplicaKind.Independent })
            {
                for (var k = 1; k <= replicaCount; k++)
                {
                    var candidate = ResultsMFile.Parse(FixtureReplicas.ReplicaPath(formulation, kind, k));
                    var verdicts = StatisticalCriterion.CompareCellVerdicts(formulation, candidate, kind, StatisticalCriterion.Alpha, excludeReplicaOrdinal: k);
                    foreach (var v in verdicts)
                    {
                        if (v.Rule != CalibrationRule.Mass)
                        {
                            continue;
                        }

                        var key = $"{formulation}|{kind}|replica {k}|{v.Name}[{v.Index}]";
                        if (knownCells.Contains(key))
                        {
                            _ = presentKnownCells.Add(key);
                        }

                        if (v.Failed)
                        {
                            failing.Add(key);
                        }
                    }
                }
            }

            var reference = ResultsMFile.Parse(FixtureReplicas.ReferencePath(formulation));
            foreach (var v in StatisticalCriterion.CompareCellVerdicts(formulation, reference, ReplicaKind.Lagged, StatisticalCriterion.Alpha))
            {
                if (v.Rule == CalibrationRule.Mass && v.Failed)
                {
                    failing.Add($"{formulation}|Lagged|reference|{v.Name}[{v.Index}]");
                }
            }
        }

        Assert.True(failing.Count == 0, $"{failing.Count} Mass-rule cell(s) failed on a genuine run: {string.Join(", ", failing)}");
        Assert.True(
            presentKnownCells.SetEquals(knownCells),
            $"expected all 5 known cells to appear as passing Mass verdicts; missing: {string.Join(", ", knownCells.Except(presentKnownCells))}");
    }

    // The genuine value (tests/Harness/HISTORY.md#e1-mass-bracket-feasible-interval): passes under the feasible interval,
    // where the withdrawn point estimate failed it.
    [Trait("Category", "Long")]
    [Fact]
    public void TheGenuineValuePassesWhereThePointEstimateFailed()
    {
        var verdict = Fmkarm66Verdict(Hpepa3Replica26WithFmkarm66(8.13E-06));
        Assert.Equal(CalibrationRule.Mass, verdict.Rule);
        Assert.False(verdict.Failed, "the genuine value 8.13E-06 must pass the feasible-interval bracket (E1).");
    }

    // Red: the value n = 2 would print (roughly double the genuine one) and zero mass under a nonzero count —
    // the second only fails because `r(0) = 0` (this node's BOOT.md, "Mass bracket"): the E-format never prints
    // a nonzero value as `0.000E+00`, so a printed 0 carries no half-print-unit slack the way
    // `PrintResolution.ResolutionFromDecimalDigits(0, digits)` would otherwise hand it.
    [Trait("Category", "Long")]
    [Theory]
    [InlineData(1.63E-05)]
    [InlineData(0.0)]
    public void MutatedValuesFailTheBracket(double value)
    {
        var verdict = Fmkarm66Verdict(Hpepa3Replica26WithFmkarm66(value));
        Assert.Equal(CalibrationRule.Mass, verdict.Rule);
        Assert.True(verdict.Failed, $"value {value:G9} must fail the feasible-interval bracket.");
    }

    // Sharpness: measured 2026-09-27, HPEPA3 lagged replica 26 (excludeReplicaOrdinal 26), q in
    // [5.26524223E-14, 5.52881408E-14] from 61 calibration cells, Low = 7.92589688E-06, High = 8.70672143E-06 —
    // the boundary is real on both sides of the bracket, not merely widened until the known cell passed.
    [Trait("Category", "Long")]
    [Theory]
    [InlineData(7.92E-06, true)]
    [InlineData(7.93E-06, false)]
    [InlineData(8.71E-06, false)]
    [InlineData(8.72E-06, true)]
    public void SharpnessAtTheBracketsOwnBoundary(double value, bool expectedFailed)
    {
        var verdict = Fmkarm66Verdict(Hpepa3Replica26WithFmkarm66(value));
        Assert.Equal(expectedFailed, verdict.Failed);
    }

    // Threshold equals the measured Low or High bound (to 1e-6 relative, print-rounding and floating-point guard
    // aside), not some other quantity that happens to fail at the same probe values.
    [Trait("Category", "Long")]
    [Fact]
    public void ThresholdEqualsTheMeasuredLowOrHighBound()
    {
        var low = Fmkarm66Verdict(Hpepa3Replica26WithFmkarm66(7.92E-06));
        Assert.True(RelativelyClose(low.Threshold, 7.92589688E-06, 1e-6), $"low threshold {low.Threshold:G9} != 7.92589688E-06.");

        var high = Fmkarm66Verdict(Hpepa3Replica26WithFmkarm66(8.72E-06));
        Assert.True(RelativelyClose(high.Threshold, 8.70672143E-06, 1e-6), $"high threshold {high.Threshold:G9} != 8.70672143E-06.");
    }

    // Empty interval (this node's BOOT.md, "Mass bracket"): doubling the array's own maximum (fmkarm[5],
    // n̂ = 2,332,512) refutes the bracket's own one-quantum-per-array model for this run — q_lo (8.079E-14)
    // exceeds q_hi (5.529E-14) — and every Mass-rule cell fails, including fmkarm[66] with its own value
    // unchanged: an infeasible interval fails the cell outright (this node's BOOT.md, "Mass bracket", step 4),
    // never merely widening past it.
    [Trait("Category", "Long")]
    [Fact]
    public void DoublingTheArraysOwnMaximumMakesTheIntervalEmptyAndFmkarm66StillFails()
    {
        var candidate = new Dictionary<string, double[]>(ResultsMFile.Parse(FixtureReplicas.ReplicaPath("HPEPA3", ReplicaKind.Lagged, 26)));
        var fmkarm = (double[])candidate["fmkarm"].Clone();
        var maxIndex = 0;
        for (var i = 1; i < fmkarm.Length; i++)
        {
            if (fmkarm[i] > fmkarm[maxIndex])
            {
                maxIndex = i;
            }
        }

        fmkarm[maxIndex] *= 2.0;
        candidate["fmkarm"] = fmkarm;

        var verdict = Fmkarm66Verdict(candidate);
        Assert.Equal(CalibrationRule.Mass, verdict.Rule);
        Assert.True(
            verdict.Failed,
            $"an infeasible q interval (from doubling fmkarm[{maxIndex}]) must fail every Mass-rule cell, fmkarm[66]'s own unchanged value included.");
    }

    // The clamp against `PrintResolution.ExponentOf`'s own power-of-ten quirk (F-c, fixed 2026-09-28,
    // `tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`): without `Math.Min(nHat,
    // ...)`/`Math.Max(nHat, ...)`, this exact input returns (12, 11) — an interval that excludes its own point
    // estimate.
    [Fact]
    public void CountBoundsClampGuardsThePowerOfTenQuirk()
    {
        var (low, high) = RunQuantum.CountBounds(1.0e-7, 1.0e-10, new RunQuantum.QuantumEstimate(8.4e-9, 1.0e-11, true));
        Assert.Equal(12.0, low);
        Assert.Equal(12.0, high);
    }
}
