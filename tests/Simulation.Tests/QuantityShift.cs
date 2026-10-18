using PropStruct.Tests.Harness;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The shift of one printed quantity from the <c>Original</c> arm to another arm, over the seeds the two share a
/// run for: the difference of means with its standard error, Welch's t (unpaired, as the task asks) and, because
/// both arms use the same seeds, the paired t of the per-seed differences. Every figure is a mean over seeds,
/// never a single run (root BOOT.md, Taboos: "Every figure states what it is").
/// </summary>
internal readonly record struct QuantityShift(
    int OriginalCount,
    int ArmCount,
    int PairedCount,
    double MeanOriginal,
    double MeanArm,
    double StandardErrorOriginal,
    double StandardErrorArm,
    double WelchT,
    double WelchDegreesOfFreedom,
    double PairedT)
{
    /// <summary>Below this magnitude of the <c>Original</c> mean a relative shift is meaningless (the unassigned <c>pdoksmall(1)</c> prints 1e-38).</summary>
    private const double RelativeFloor = 1e-30;

    /// <summary>The per-quantity two-sided level of the stars in the tables; not family-wise (the cell counts are).</summary>
    private const double StarLevel = 1e-3;

    /// <summary>Arm mean minus <c>Original</c> mean.</summary>
    public double Difference => MeanArm - MeanOriginal;

    /// <summary>The standard error of <see cref="Difference"/> from the two standard errors, as Welch's test takes it.</summary>
    public double DifferenceStandardError => Math.Sqrt(StandardErrorOriginal * StandardErrorOriginal + StandardErrorArm * StandardErrorArm);

    /// <summary>Whether the shift can be stated in per cent of the <c>Original</c> mean.</summary>
    public bool IsRelative => Math.Abs(MeanOriginal) >= RelativeFloor;

    /// <summary>The shift in per cent of the <c>Original</c> mean; meaningful only when <see cref="IsRelative"/>.</summary>
    public double ShiftPercent => 100.0 * Difference / MeanOriginal;

    /// <summary>The standard error of <see cref="ShiftPercent"/>, the <c>Original</c> mean taken as exact.</summary>
    public double ShiftPercentStandardError => 100.0 * DifferenceStandardError / Math.Abs(MeanOriginal);

    /// <summary>Whether |Welch t| exceeds the two-sided critical value at the per-quantity level 10⁻³.</summary>
    public bool IsStarred => double.IsInfinity(WelchT) || Math.Abs(WelchT) > StudentDistribution.TwoSidedQuantile(
        Math.Max(1, (int)Math.Round(WelchDegreesOfFreedom)), StarLevel);

    /// <summary>
    /// The shift of <paramref name="arm"/> against <paramref name="original"/>; each argument maps a seed to the
    /// quantity's value in that seed's run, runs that failed or do not print it absent.
    /// </summary>
    public static QuantityShift Compare(IReadOnlyDictionary<ulong, double> original, IReadOnlyDictionary<ulong, double> arm)
    {
        var originalValues = original.Values.ToArray();
        var armValues = arm.Values.ToArray();
        var (meanOriginal, varianceOriginal) = MeanAndVariance(originalValues);
        var (meanArm, varianceArm) = MeanAndVariance(armValues);
        var termOriginal = varianceOriginal / originalValues.Length;
        var termArm = varianceArm / armValues.Length;
        var difference = meanArm - meanOriginal;

        var welchT = Ratio(difference, Math.Sqrt(termOriginal + termArm));
        var degrees = termOriginal + termArm == 0.0
            ? originalValues.Length + armValues.Length - 2
            : Math.Pow(termOriginal + termArm, 2)
                / (termOriginal * termOriginal / (originalValues.Length - 1) + termArm * termArm / (armValues.Length - 1));

        var paired = original.Keys.Where(arm.ContainsKey).Select(seed => arm[seed] - original[seed]).ToArray();
        var (meanDifference, varianceDifference) = MeanAndVariance(paired);
        var pairedT = Ratio(meanDifference, Math.Sqrt(varianceDifference / paired.Length));

        return new QuantityShift(
            originalValues.Length,
            armValues.Length,
            paired.Length,
            meanOriginal,
            meanArm,
            Math.Sqrt(termOriginal),
            Math.Sqrt(termArm),
            welchT,
            degrees,
            pairedT);
    }

    /// <summary>The sample mean and the unbiased sample variance (n − 1) of <paramref name="values"/>.</summary>
    public static (double Mean, double Variance) MeanAndVariance(double[] values)
    {
        var mean = values.Average();
        var sumOfSquares = values.Sum(value => (value - mean) * (value - mean));
        return (mean, sumOfSquares / (values.Length - 1));
    }

    /// <summary>A t statistic: 0 for a zero numerator over a zero error, ±∞ for a nonzero numerator over a zero error.</summary>
    private static double Ratio(double numerator, double standardError) =>
        standardError == 0.0 ? (numerator == 0.0 ? 0.0 : Math.CopySign(double.PositiveInfinity, numerator)) : numerator / standardError;
}
