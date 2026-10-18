using PropStruct.Execution;
using PropStruct.Input;
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
    public void TheMeasuredArmsAreOriginalAllEachSingleFlagAndAllButEachCategoryFlagOnceUnderDistinctNames()
    {
        var arms = CorrectionArm.Measured;
        var single = CorrectionArm.SingleFlags.Count;
        var categories = CorrectionArm.CategoryFlags.Count;

        Assert.Equal(2 + single + categories, arms.Count);
        Assert.Equal(arms.Count, arms.Select(arm => arm.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ModelKind.Original, arms[0].Model);
        Assert.Null(arms[0].Corrections);
        Assert.Equal(ModelKind.Corrected, arms[1].Model);
        Assert.Null(arms[1].Corrections);
        Assert.All(arms.Skip(2), arm => Assert.Equal(ModelKind.Corrected, arm.Model));
        Assert.Equal(CorrectionArm.SingleFlags, arms.Skip(2).Take(single).Select(arm => arm.Corrections!.Value));
        Assert.Equal(CorrectionArm.CategoryFlags, CorrectionArm.SingleFlags.Where(flag => flag.ToString().StartsWith("Categories", StringComparison.Ordinal)));
        Assert.Equal(
            CorrectionArm.CategoryFlags.Select(flag => $"AllBut{flag}"),
            arms.Skip(2 + single).Select(arm => arm.Name));
    }

    /// <summary>
    /// The three category flags, named by the spec of the arms, and the seam of each leave-one-out arm: every flag
    /// but the one it is named after, so that the flag is out and the other ten are in.
    /// </summary>
    [Fact]
    public void TheCategoryFlagsAreTheThreeAndEachAllButArmSetsEveryOtherFlag()
    {
        Assert.Equal(
            [CorrectionSet.CategoriesOnCopy, CorrectionSet.CategoriesLastRowRecomputed, CorrectionSet.CategoriesEmptyRowsMerged],
            CorrectionArm.CategoryFlags);

        foreach (var flag in CorrectionArm.CategoryFlags)
        {
            var arm = CorrectionArm.AllBut(flag);

            Assert.Equal($"AllBut{flag}", arm.Name);
            Assert.False(arm.Corrections!.Value.HasFlag(flag));
            Assert.Equal(CorrectionSet.All, arm.Corrections.Value | flag);
            Assert.Equal(System.Numerics.BitOperations.PopCount((uint)CorrectionSet.All) - 1, System.Numerics.BitOperations.PopCount((uint)arm.Corrections.Value));
            Assert.Equal(arm.Corrections, arm.Options(ModelParameters.Default, ExecutionMode.Batched, 0UL).Corrections);
            Assert.Equal(arm, CorrectionArm.Named(arm.Name));
        }
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
            ["Dkarm43_cor(1)"] = [140.01],
            ["Dkarm43_cor(2)"] = [141.02],
            ["Dagg43_cor(1)"] = [12.34],
            ["Dagg43_cor(2)"] = [13.45],
            ["Dkarmcat"] = [10, 20, 30],
            ["pdoksmall"] = [0.5, 0.25],
        };

        Assert.Equal(136.73, HeadlineQuantity.All.Single(q => q.Label == "Dkarm43").Read(cells));
        Assert.Equal(140.01, HeadlineQuantity.All.Single(q => q.Label == "Dkarm43_cor(1)").Read(cells));
        Assert.Equal(141.02, HeadlineQuantity.All.Single(q => q.Label == "Dkarm43_cor(2)").Read(cells));
        Assert.Equal(12.34, HeadlineQuantity.All.Single(q => q.Label == "Dagg43_cor(1)").Read(cells));
        Assert.Equal(13.45, HeadlineQuantity.All.Single(q => q.Label == "Dagg43_cor(2)").Read(cells));
        Assert.Contains(HeadlineQuantity.All, q => q.Label == "Zkarm_cor");
        Assert.Equal(3.0, HeadlineQuantity.All.Single(q => q.Label == "DPRow").Read(cells));
        Assert.Equal(0.5, HeadlineQuantity.All.Single(q => q.Label == "pdoksmall[0]").Read(cells));
        Assert.Equal(0.25, HeadlineQuantity.StrongestPdoksmallCell(1).Read(cells));
        Assert.Null(HeadlineQuantity.StrongestPdoksmallCell(2).Read(cells));
        Assert.Null(HeadlineQuantity.All.Single(q => q.Label == "Zkarm").Read(cells));
    }

    /// <summary>
    /// The streams jump by <c>seed * 2^80</c> in a period of 2^100, so seeds equal modulo 2^20 are copies. Every case's
    /// seeds at its own count, and the audit's eight, have distinct phases. Red on the stride 2^17 with sixteen seeds
    /// (the next fact), right on 2^16.
    /// </summary>
    [Fact]
    public void EveryCasesSeedsAndTheAuditsEightHaveDistinctPhasesModulo2To20()
    {
        foreach (var measurementCase in MeasurementCase.All)
        {
            var repeated = MeasurementCase.RepeatedPhases(MeasurementCase.SeedsOf(measurementCase.SeedCount));
            Assert.True(repeated.Count == 0, $"{measurementCase.Name}: seeds {string.Join(", ", repeated)} repeat a phase.");
        }

        Assert.Empty(MeasurementCase.RepeatedPhases(CorrectionEffectsMeasurement.AuditSeeds));
    }

    /// <summary>
    /// The positive control of the check, of the size of the effect: sixteen seeds at the stride 2^17 land on eight
    /// phases, seed <c>k + 8</c> repeating seed <c>k</c> (<c>8 * 2^17 = 2^20</c>), so exactly the eight seeds
    /// <c>8..15</c> are reported; at the stride 2^16 none is.
    /// </summary>
    [Fact]
    public void TheOldStrideWithSixteenSeedsRepeatsEightPhasesAndTheCheckNamesThem()
    {
        var oldSeeds = MeasurementCase.SeedsOf(16, MeasurementCase.AuditSeedStrideBits);
        var repeated = MeasurementCase.RepeatedPhases(oldSeeds);

        Assert.Equal(oldSeeds.Skip(8), repeated);
        Assert.Equal(8, repeated.Count);
        _ = Assert.Throws<InvalidOperationException>(() => MeasurementCase.RequireDistinctPhases(oldSeeds));
        MeasurementCase.RequireDistinctPhases(MeasurementCase.SeedsOf(16));
    }

    /// <summary>
    /// The report refuses to run over arms whose seeds repeat a phase, in its section and in the floor of the
    /// comparison; over distinct phases it runs, and the stride it prints is the measurement's.
    /// </summary>
    [Fact]
    public void TheReportRefusesArmsWhoseSeedsRepeatAPhaseAndRunsOverDistinctOnes()
    {
        var measurementCase = MeasurementCase.References[0];
        var repeating = ArmOf(CorrectionArm.Original.Name, MeasurementCase.SeedsOf(16, MeasurementCase.AuditSeedStrideBits));
        var distinct = ArmOf(CorrectionArm.Original.Name, MeasurementCase.SeedsOf(16));

        _ = Assert.Throws<InvalidOperationException>(() => CorrectionEffectsReport.CaseSection(measurementCase, [repeating], 1));
        _ = Assert.Throws<InvalidOperationException>(() => CorrectionEffectsReport.EvenAgainstOdd(repeating));

        var (_, even, odd) = CorrectionEffectsReport.EvenAgainstOdd(distinct);
        var section = CorrectionEffectsReport.CaseSection(measurementCase, [distinct], 1);
        Assert.Equal((8, 8), (even, odd));
        Assert.Contains("seeds `k * 2^16`", section, StringComparison.Ordinal);
    }

    private static ArmRuns ArmOf(string name, IReadOnlyList<ulong> seeds) => new(
        "fingerprint",
        "case",
        name,
        "Batched",
        seeds,
        [.. seeds.Select(seed => new MeasuredRun(seed, MeasuredRun.Completed, 0.0, new Dictionary<string, double[]> { ["Dkarm43(1)"] = [100.0 + (seed >> 16)] }))]);
}
