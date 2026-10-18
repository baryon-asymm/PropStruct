using PropStruct.Input;
using PropStruct.Particle;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The evidence cases of the per-cycle fixes (<c>src/Statistics/ACCEPTANCE.md</c>): P312, the shipped formulation whose
/// <c>gdokleft</c> is large, on the totals <see cref="AnalyticCycle"/> builds from its own size distribution. The figures
/// they report are measurements of that cycle, in the unit stated at each, not of a Monte Carlo run.
/// </summary>
public class CorrectedCycleEvidenceTests(ITestOutputHelper output)
{
    private static Formulation P312 => ShippedFormulations.Read("P312");

    /// <summary>
    /// P312 at its defaults: <c>gdokleft</c> is 0.122 (three digits) of the propellant, so the coarse oxidizer is
    /// <c>GGG - gdokleft</c> and not <c>GGG</c>; <c>ZdoksmallCoarseShare</c> moves <c>pdoksmall</c> in the cells it reads.
    /// </summary>
    [Fact]
    public void P312AtItsDefaultsHasLeftOxidizerAndTheCoarseShareMovesPdoksmall()
    {
        var none = Report(ModelParameters.Default, CorrectionSet.None, out var nextNone);
        var share = Report(ModelParameters.Default, CorrectionSet.ZdoksmallCoarseShare, out var nextShare);

        Assert.Equal(0.122, none.Gdokleft, 3);
        Assert.Equal(none.Gdokleft, share.Gdokleft);
        var moved = nextNone.Zip(nextShare, (a, b) => a == 0.0 ? 0.0 : Math.Abs(b - a) / a).Max();
        output.WriteLine($"P312 defaults: gdokleft {none.Gdokleft:R}, ZdoksmallCoarseShare moves pdoksmall by at most {moved:E3} relative");
        Assert.True(moved > 1e-3, $"pdoksmall moved by {moved:E3} relative only");
    }

    /// <summary>
    /// P312 at <c>Dmin = 5 um</c>: no mass sits below <c>b = 0</c>, so <c>gdokleft = 0</c> and the first cell of
    /// <c>pdoksmall</c> is read, 0 under <c>None</c> and a positive probability under <c>PdoksmallFirstCell</c>.
    /// </summary>
    [Fact]
    public void P312AtFiveMicrometresReadsTheFirstCellOfPdoksmall()
    {
        var parameters = ModelParameters.Default with { Dmin = Length.FromMicrometres(5) };
        var none = Report(parameters, CorrectionSet.None, out var nextNone);
        var first = Report(parameters, CorrectionSet.PdoksmallFirstCell, out var nextFirst);

        Assert.Equal(0.0, none.Gdokleft);
        Assert.Equal(0.0, nextNone[0]);
        Assert.True(nextFirst[0] > 0.0);
        output.WriteLine($"P312 Dmin 5 um: gdokleft {none.Gdokleft:R}, pdoksmall[0] {nextNone[0]:R} under None, {nextFirst[0]:R} under PdoksmallFirstCell");
        _ = first;
    }

    private static CycleReport Report(ModelParameters parameters, CorrectionSet corrections, out double[] nextPdoksmall)
    {
        var cycle = AnalyticCycle.Prepare(P312, parameters, PrecisionKind.Binary64, corrections);
        return cycle.Compute(1, cycle.IntegerTotals(), cycle.RealTotals(), out nextPdoksmall);
    }
}
