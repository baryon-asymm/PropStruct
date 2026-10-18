using PropStruct.Particle;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The fast facts of the tooling the correction-effects measurement stands on (<c>BOOT.md</c>, "##
/// Correction-effects measurement"): the arm list covers <see cref="CorrectionSet"/> and the shift statistics give
/// a hand-computed answer. The measurement itself is <see cref="CorrectionEffectsMeasurement"/>, hand-run.
/// </summary>
public class CorrectionMeasurementToolingTests
{
    [Fact]
    public void TheSingleFlagArmsAreExactlyTheBitsOfAll()
    {
        var union = CorrectionArm.SingleFlags.Aggregate(CorrectionSet.None, (set, flag) => set | flag);

        Assert.Equal(CorrectionSet.All, union);
        Assert.Equal(System.Numerics.BitOperations.PopCount((uint)CorrectionSet.All), CorrectionArm.SingleFlags.Count);
    }

    [Fact]
    public void TheMeasuredArmsAreOriginalAllAndEachSingleFlagOnceUnderDistinctNames()
    {
        var arms = CorrectionArm.Measured;

        Assert.Equal(2 + CorrectionArm.SingleFlags.Count, arms.Count);
        Assert.Equal(arms.Count, arms.Select(arm => arm.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ModelKind.Original, arms[0].Model);
        Assert.Null(arms[0].Corrections);
        Assert.Equal(ModelKind.Corrected, arms[1].Model);
        Assert.Null(arms[1].Corrections);
        Assert.All(arms.Skip(2), arm => Assert.Equal(ModelKind.Corrected, arm.Model));
        Assert.Equal(CorrectionArm.SingleFlags, arms.Skip(2).Select(arm => arm.Corrections!.Value));
    }

    /// <summary>
    /// A known answer of the size of the effect. Original {1, 2, 3, 4} (mean 2.5, variance 5/3) against the arm
    /// {2, 4, 5, 8} (mean 4.75, variance 6.25): Welch t = 2.25 / sqrt(5/12 + 25/16) = 1.5993 on 4.4938 degrees of
    /// freedom; the per-seed differences {1, 2, 2, 4} (mean 2.25, variance 19/12) give the paired t
    /// 2.25 / sqrt(19/48) = 3.5762.
    /// </summary>
    [Fact]
    public void TheShiftStatisticsGiveTheHandComputedWelchAndPairedT()
    {
        var original = new Dictionary<ulong, double> { [0] = 1, [1] = 2, [2] = 3, [3] = 4 };
        var arm = new Dictionary<ulong, double> { [0] = 2, [1] = 4, [2] = 5, [3] = 8 };

        var shift = QuantityShift.Compare(original, arm);

        Assert.Equal(2.5, shift.MeanOriginal, 12);
        Assert.Equal(4.75, shift.MeanArm, 12);
        Assert.Equal(2.25, shift.Difference, 12);
        Assert.Equal(90.0, shift.ShiftPercent, 10);
        Assert.Equal(2.25 / Math.Sqrt(5.0 / 12.0 + 25.0 / 16.0), shift.WelchT, 10);
        Assert.Equal(1.5993, shift.WelchT, 4);
        Assert.Equal(Math.Pow(95.0 / 48.0, 2) / ((Math.Pow(5.0 / 12.0, 2) + Math.Pow(25.0 / 16.0, 2)) / 3.0), shift.WelchDegreesOfFreedom, 10);
        Assert.Equal(4.4938, shift.WelchDegreesOfFreedom, 4);
        Assert.Equal(2.25 / Math.Sqrt(19.0 / 48.0), shift.PairedT, 10);
        Assert.Equal(3.5762, shift.PairedT, 4);
        Assert.Equal(4, shift.PairedCount);
    }

    [Fact]
    public void ACommonShiftOfIdenticalSeedsHasInfiniteTAndAnUnchangedQuantityHasZero()
    {
        var original = new Dictionary<ulong, double> { [0] = 5, [1] = 5, [2] = 5 };
        var moved = new Dictionary<ulong, double> { [0] = 7, [1] = 7, [2] = 7 };

        Assert.Equal(double.PositiveInfinity, QuantityShift.Compare(original, moved).WelchT);
        Assert.Equal(double.NegativeInfinity, QuantityShift.Compare(moved, original).WelchT);
        Assert.Equal(0.0, QuantityShift.Compare(original, original).WelchT);
        Assert.True(QuantityShift.Compare(original, moved).IsStarred);
        Assert.False(QuantityShift.Compare(original, original).IsStarred);
    }

    [Fact]
    public void ARelativeShiftIsRefusedWhereTheOriginalMeanIsZero()
    {
        var zero = new Dictionary<ulong, double> { [0] = 0, [1] = 0 };
        var moved = new Dictionary<ulong, double> { [0] = 1, [1] = 3 };

        var shift = QuantityShift.Compare(zero, moved);

        Assert.False(shift.IsRelative);
        Assert.Equal(2.0, shift.Difference, 12);
    }

    [Fact]
    public void TheHeadlineQuantitiesAreReadFromTheParsedCellsAndDPRowIsTheCategoryCount()
    {
        var cells = new Dictionary<string, double[]>
        {
            ["Dkarm43(1)"] = [136.73],
            ["Dkarmcat"] = [10, 20, 30],
            ["pdoksmall"] = [0.5, 0.25],
        };

        Assert.Equal(136.73, HeadlineQuantity.All.Single(q => q.Label == "Dkarm43").Read(cells));
        Assert.Equal(3.0, HeadlineQuantity.All.Single(q => q.Label == "DPRow").Read(cells));
        Assert.Equal(0.5, HeadlineQuantity.All.Single(q => q.Label == "pdoksmall[0]").Read(cells));
        Assert.Equal(0.25, HeadlineQuantity.StrongestPdoksmallCell(1).Read(cells));
        Assert.Null(HeadlineQuantity.StrongestPdoksmallCell(2).Read(cells));
        Assert.Null(HeadlineQuantity.All.Single(q => q.Label == "Zkarm").Read(cells));
    }
}
