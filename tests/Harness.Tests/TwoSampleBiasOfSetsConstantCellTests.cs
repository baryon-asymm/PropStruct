using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// A defect found and fixed 2026-09-23 (a coordinator report, verified here; this node's BOOT.md, "Two-sample
/// bias"): <see cref="StatisticalCriterion.TwoSampleBiasOfSets"/> flagged a cell as differing when both sets
/// held the exact same bit-identical value but at different set sizes — measured on HPEPA3, <c>Gfr[0]</c> at
/// <c>0.515</c> in all 32 lagged replicas and all 16 of a port run's own seeds, bit-identical as parsed, read
/// as <c>t ≈ 6.8</c>. Cause: <c>Array.Average()</c> of <c>n</c> copies of the same binary64 value need not
/// equal that value exactly (the running sum can round after an addition even though every addend is equal),
/// so two constant sets of different sizes can average to means one ULP apart; the residual-based sample
/// standard deviation then reads that ULP as a tiny nonzero spread instead of the true zero, and a tiny but
/// nonzero standard error turns a one-ULP mean difference into a large finite <c>|t|</c>.
///
/// Fixed by <c>MeanAndSampleStandardDeviation</c>'s own bit-identical short-circuit (in
/// <c>StatisticalCriterion.cs</c>, beside <c>TwoSampleBiasOfSets</c>): when every value of a set is bit-identical,
/// its mean is that exact value and its standard deviation is exactly <c>0</c>, never computed through
/// <c>Average()</c>. Proven non-degenerate by running both cases here against the pre-fix code (reverted by
/// hand for the run, then restored — the task report has the transcript): the first case below read <c>t</c>
/// in the tens and <c>Differs == true</c> before the fix, and reads <c>t == 0</c> after it; the second case
/// read <c>Differs == true</c> both before and after, proving the fix does not also swallow a real difference
/// between two constants (the <c>eps</c> echo, <c>5.0000001E-02</c> against <c>5.0000000E-02</c>, is exactly
/// this shape and must stay flagged).
/// </summary>
public class TwoSampleBiasOfSetsConstantCellTests
{
    private static List<IReadOnlyDictionary<string, double[]>> ConstantSet(double value, int count) =>
        Enumerable.Range(0, count)
            .Select(_ => (IReadOnlyDictionary<string, double[]>)new Dictionary<string, double[]> { ["Q"] = [value] })
            .ToList();

    [Fact]
    public void EqualConstantsOfDifferentSetSizesDoNotDiffer()
    {
        // The reported shape: Gfr[0] = 0.515 in 32 lagged replicas against 16 of a port run's own seeds.
        var first = ConstantSet(0.515, 32);
        var second = ConstantSet(0.515, 16);

        var cells = StatisticalCriterion.TwoSampleBiasOfSets(first, second);

        var cell = Assert.Single(cells);
        Assert.Equal(0.0, cell.T);
        Assert.False(cell.Differs, $"two bit-identical constant sets of different sizes must not differ (t={cell.T:G17}).");
    }

    [Fact]
    public void DifferentConstantsOfDifferentSetSizesStillDiffer()
    {
        // The eps echo's own shape: a real last-digit difference between two constants, sets of different sizes.
        // Both before and after the fix this reads Differs == true (measured: |t| = infinity after the fix,
        // since each side's own standard deviation is now exactly 0; a large finite |t| in the hundreds of
        // millions before it, since the pre-fix code's own one-ULP noise floor was still small enough next to
        // a genuine ~1e-9 mean difference not to change the verdict — the fix changes how a constant set's own
        // spread is computed, not whether two different constants are flagged).
        var first = ConstantSet(5.0000001E-02, 32);
        var second = ConstantSet(5.0000000E-02, 16);

        var cells = StatisticalCriterion.TwoSampleBiasOfSets(first, second);

        var cell = Assert.Single(cells);
        Assert.True(cell.Differs, $"a real difference between two constants must still be flagged (t={cell.T:G17}).");
    }
}
