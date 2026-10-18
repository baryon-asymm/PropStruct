using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>ZdoksmallCoarseShare</c> (Fortran 1047) and <c>PdoksmallFirstCell</c> (1059-1061), seen through
/// <c>pdoksmall</c> as <c>zdoksmall</c> stays inside <c>SmallParticles.Probability</c> (<c>src/Statistics/BOOT.md</c>,
/// "## Corrected model"). The input is the one the criterion names: a <c>vdokstr</c> whole in cell 1, <c>AK2 = 2</c>
/// and <c>gdokleft &gt; 0</c>, for which lines 1021-1056 give <c>zdoksmall</c> 1/2 in cell 1 and 1 beyond before the
/// scaling, and <c>ddoksmall</c> 1/4 in cell 1 and <c>1/(2k)</c> beyond; <c>pdoksmall</c> is then the closed form of
/// 1047-1064 below. The red side is the same input under another set: the scaling by <c>GGG</c> and a first cell of 0.
/// </summary>
public class SmallParticlesCorrectedTests
{
    private const int Ndok = 12;
    private const double CellSize = 10e-6;
    private const double Ak2 = 2.0;
    private const double Ggg = 0.6;
    private const double Gdokleft = 0.1;
    private const double Karmcoef = 0.3;
    private const double OxidizerDensity = 1800.0;
    private const double PropellantDensity = 1700.0;
    private const double RelativeTolerance = 1e-12;

    /// <summary>Right: the closed form with the scaling by <c>GGG - gdokleft</c> and a first cell of 0.</summary>
    [Fact]
    public void TheCoarseShareFlagScalesZdoksmallByTheCoarseMassAndLeavesTheFirstCellZero()
    {
        var pdoksmall = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.ZdoksmallCoarseShare);

        AssertClosedForm(Ggg - Gdokleft, firstCell: false, pdoksmall);
        Assert.Equal(0.0, pdoksmall[0]);
    }

    /// <summary>Right: <c>None</c> is the closed form with the scaling by <c>GGG</c> and a first cell of 0, the original's values.</summary>
    [Fact]
    public void NoneScalesByGggAndLeavesTheFirstCellZero()
    {
        var pdoksmall = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.None);

        AssertClosedForm(Ggg, firstCell: false, pdoksmall);
        Assert.Equal(0.0, pdoksmall[0]);
    }

    /// <summary>Right: the first-cell flag assigns <c>pdoksmall(1)</c> by the formula of line 1061, the scaling stays <c>GGG</c>.</summary>
    [Fact]
    public void TheFirstCellFlagAssignsTheFirstCellByLine1061()
    {
        var pdoksmall = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.PdoksmallFirstCell);

        AssertClosedForm(Ggg, firstCell: true, pdoksmall);
        Assert.True(pdoksmall[0] > 0.0);
    }

    /// <summary>Right: both flags together are both closed forms at once.</summary>
    [Fact]
    public void BothFlagsGiveTheScalingAndTheFirstCell()
    {
        var pdoksmall = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.ZdoksmallCoarseShare | CorrectionSet.PdoksmallFirstCell);

        AssertClosedForm(Ggg - Gdokleft, firstCell: true, pdoksmall);
    }

    /// <summary>
    /// Red: the four sets give four different arrays (the scaling moves every cell, the first cell moves cell 0), so
    /// that the closed forms above are not the same number four times.
    /// </summary>
    [Fact]
    public void TheFourSetsAreFourDifferentArrays()
    {
        var none = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.None);
        var scaling = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.ZdoksmallCoarseShare);
        var first = Probability(WholeInCellOne(), Gdokleft, CorrectionSet.PdoksmallFirstCell);

        Assert.NotEqual(none, scaling);
        Assert.NotEqual(none, first);
        Assert.NotEqual(scaling, first);
    }

    /// <summary>With <c>gdokleft = 0</c> and cell 1 empty both flags change no bit.</summary>
    [Fact]
    public void WithoutLeftOxidizerAndWithCellOneEmptyBothFlagsChangeNoBit()
    {
        var vdokstr = WholeInCellOne();
        vdokstr[0] = 0.0;
        vdokstr[1] = 1.0;

        var none = Probability(vdokstr, 0.0, CorrectionSet.None);
        var both = Probability(vdokstr, 0.0, CorrectionSet.ZdoksmallCoarseShare | CorrectionSet.PdoksmallFirstCell);

        Assert.Equal(none.Select(BitConverter.DoubleToInt64Bits), both.Select(BitConverter.DoubleToInt64Bits));
    }

    /// <summary>Under <see cref="PrecisionKind.Original"/> the flags are replaced by <c>None</c> on entry, bit for bit.</summary>
    [Fact]
    public void UnderOriginalEveryFlagIsIgnored()
    {
        var vdokstr = WholeInCellOne();
        var old = SmallParticles.Probability(Ndok, CellSize, Ak2, Ggg, Gdokleft, Karmcoef, OxidizerDensity, PropellantDensity,
            vdokstr, PrecisionKind.Original);
        var flagged = SmallParticles.Probability(Ndok, CellSize, Ak2, Ggg, Gdokleft, Karmcoef, OxidizerDensity, PropellantDensity,
            vdokstr, PrecisionKind.Original, CorrectionSet.All);

        Assert.Equal(old.Select(BitConverter.DoubleToInt64Bits), flagged.Select(BitConverter.DoubleToInt64Bits));
    }

    private static double[] WholeInCellOne()
    {
        var vdokstr = new double[Ndok];
        vdokstr[0] = 1.0;
        return vdokstr;
    }

    private static double[] Probability(double[] vdokstr, double gdokleft, CorrectionSet corrections) =>
        SmallParticles.Probability(Ndok, CellSize, Ak2, Ggg, gdokleft, Karmcoef, OxidizerDensity, PropellantDensity, vdokstr,
            PrecisionKind.Binary64, corrections);

    /// <summary>
    /// Lines 1047-1064 on the input above, in closed form: <c>zdoksmall</c> is <c>c/2</c> in cell 1 and <c>c</c> beyond,
    /// <c>ddoksmall</c> 1/4 in cell 1 and <c>1/(2k)</c> beyond, <c>vdoksmall = z/(S2 + z) PL/PLOT1</c> with
    /// <c>S1 = GGG - gdokleft</c>, <c>S2 = 1 - GGG + gdokleft</c>, <c>PL = (1 - (S1 - z))/(1/PLOT2 - (S1 - z)/PLOT1)</c>,
    /// <c>pdoksmall(k) = vdoksmall ddoksmall karmcoef</c>, held to its predecessor when it falls below it, from cell 2.
    /// </summary>
    private static void AssertClosedForm(double scale, bool firstCell, double[] actual)
    {
        var s1 = Ggg - Gdokleft;
        var s2 = 1.0 - Ggg + Gdokleft;
        var expected = new double[Ndok];
        for (var k = 1; k <= Ndok; k++)
        {
            var z = k == 1 ? 0.5 * scale : scale;
            var d = k == 1 ? 0.25 : 1.0 / (2.0 * k);
            var pl = (1.0 - (s1 - z)) / (1.0 / PropellantDensity - (s1 - z) / OxidizerDensity);
            var raw = z / (s2 + z) * pl / OxidizerDensity * d * Karmcoef;
            if (k == 1)
            {
                expected[0] = firstCell ? raw : 0.0;
            }
            else
            {
                expected[k - 1] = Math.Max(raw, expected[k - 2]);
            }
        }

        for (var k = 0; k < Ndok; k++)
        {
            Assert.True(Math.Abs(actual[k] - expected[k]) <= RelativeTolerance * Math.Abs(expected[k]) + 1e-300,
                $"pdoksmall[{k}]: expected {expected[k]:R}, got {actual[k]:R}");
        }
    }
}
