namespace PropStruct.Tests.Harness;

/// <summary>
/// One cell awaiting its verdict, with the replica statistics its threshold is built from — the reference token,
/// the candidate value, the replicas' own mean/sd/Poisson floor, and every optional field the count/mass rules of
/// <see cref="CellEvaluator"/> read. Split out of <c>StatisticalCriterion.Comparisons.cs</c> (decomposition,
/// 2026-09-26): <see cref="From"/> is the former <c>AddCell</c>, renamed to a factory that returns the cell
/// instead of appending it to a caller-supplied list, so every call site now reads <c>pending.Add(PendingCell.
/// From(...))</c> instead of <c>AddCell(pending, ...)</c> — a pure rename, the body unchanged character for
/// character.
/// </summary>
internal sealed record PendingCell(
    string Name,
    int Index,
    double ReferenceValue,
    double Resolution,
    double PoissonFloor,
    double CandidateValue,
    double Mean,
    double Sd,
    double[] ReplicaValues,
    bool IsDistributionFunction,
    double? CandidateQuantum,
    double? CandidateQuantumResolution,
    bool CandidateQuantumIdentified,
    IReadOnlyList<double>? ReplicaCounts,
    bool ForceFailure,
    double? ForcedThreshold,
    int? DecimalDigits,
    MassFamilyRule.CountRuleVerdict? CountRule,
    IReadOnlyList<double>? ReplicaTotals,
    double? CandidateTotal,
    DispersionEstimator.DispersionEstimate? Dispersion)
{
    internal static PendingCell From(
        string name,
        int index,
        ResultCell referenceCell,
        double candidateValue,
        double[] replicaValues,
        bool zeroBelow,
        bool isDistributionFunction,
        double? candidateQuantum = null,
        double? candidateQuantumResolution = null,
        bool candidateQuantumIdentified = true,
        IReadOnlyList<double>? replicaCounts = null,
        bool forceFailure = false,
        double? forcedThreshold = null,
        int? decimalDigits = null,
        MassFamilyRule.CountRuleVerdict? countRule = null,
        IReadOnlyList<double>? replicaTotals = null,
        double? candidateTotal = null,
        DispersionEstimator.DispersionEstimate? dispersion = null)
    {
        if (zeroBelow && Math.Abs(referenceCell.Value) < ExclusionRules.ZeroBelowThreshold)
        {
            candidateValue = 0.0;
            replicaValues = new double[replicaValues.Length];
        }

        var mean = replicaValues.Average();
        var sd = SampleSpread.SampleStandardDeviation(replicaValues, mean);
        var poissonFloor = referenceCell.IsIntegerPrinted ? Math.Sqrt(Math.Max(mean, 1.0)) : 0.0;

        return new PendingCell(
            name, index, referenceCell.Value, referenceCell.Resolution, poissonFloor, candidateValue, mean, sd,
            replicaValues, isDistributionFunction, candidateQuantum, candidateQuantumResolution,
            candidateQuantumIdentified, replicaCounts,
            forceFailure, forcedThreshold, decimalDigits, countRule, replicaTotals, candidateTotal, dispersion);
    }
}
