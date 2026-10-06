namespace PropStruct.Tests.Harness;

/// <summary>
/// Plain sample mean and standard deviation, with the constant-set guard <see cref="MeanAndSampleStandardDeviation"/>
/// carries for <see cref="StatisticalCriterion.TwoSampleBias"/>. Split out of
/// <c>StatisticalCriterion.Numerics.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class SampleSpread
{
    internal static double SampleStandardDeviation(double[] values, double mean)
    {
        if (values.Length < 2)
        {
            return 0.0;
        }

        var sumOfSquares = 0.0;
        foreach (var value in values)
        {
            var deviation = value - mean;
            sumOfSquares += deviation * deviation;
        }

        return Math.Sqrt(sumOfSquares / (values.Length - 1));
    }

    /// <summary>
    /// The mean and sample standard deviation of a two-sample cell's own set — with one guard
    /// <see cref="SampleStandardDeviation"/> alone does not carry: when every value is bit-identical, the mean
    /// is returned as that exact value and the standard deviation as exactly 0, rather than as whatever
    /// <c>Array.Average()</c>'s own left-to-right summation happens to round to. Found 2026-09-23 (a coordinator
    /// report, verified here): `Array.Average()` of `n` bit-identical binary64 values need not equal that value
    /// exactly (the running sum can round after an addition even though every addend is the same), and when two
    /// constant sets of different sizes round to means that differ by so much as one ULP, the residual-based
    /// <see cref="SampleStandardDeviation"/> reads a tiny nonzero spread from what is actually zero spread,
    /// producing a small but nonzero standard error and a finite <c>|t|</c> in the tens — large enough to cross
    /// the family-wise critical value on a real formulation (`tests/Harness/BOOT.md`, "Two-sample bias", and
    /// `tests/Harness.Tests/TwoSampleBiasOfSetsConstantCellTests`). Two constant sets of *different*
    /// values still report <c>|t| = infinity</c> below, in the caller's own zero-standard-error branch — this
    /// guard changes only how a constant set's own spread is computed, not whether two different constants are
    /// flagged.
    /// </summary>
    internal static (double Mean, double Sd) MeanAndSampleStandardDeviation(double[] values)
    {
        var first = values[0];
        var constant = true;
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] != first)
            {
                constant = false;
                break;
            }
        }

        if (constant)
        {
            return (first, 0.0);
        }

        var mean = values.Average();
        return (mean, SampleStandardDeviation(values, mean));
    }
}
