namespace PropStruct.Benchmarks;

/// <summary>The one place that turns a list of repeat samples into the mean and spread every
/// figure carries (API.md: "runs per figure, whose spread is recorded with it").</summary>
internal static class Statistics
{
    public static double Mean(IReadOnlyList<double> values) => values.Sum() / values.Count;

    /// <summary>Sample standard deviation; 0 when there is only one repeat to compare against
    /// itself (a spread of "0" from one repeat is not a claim of perfect reproducibility — the
    /// row's own <c>n=1</c> already says that).</summary>
    public static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var mean = Mean(values);
        var sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }
}
