using System.Collections;
using System.Globalization;
using System.Text.Json;
using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L1 of the BOOT.md table: the rest of <c>CycleStatistics.Compute</c>'s pipeline —
/// <c>GeneratorAccuracy</c>, <c>OxidizerSizes</c>, <c>Pockets</c>, <c>Matrix</c>,
/// <c>CorrectedPockets</c>, <c>MassFractions</c> and <c>Convergence</c> — against
/// <c>tests/Fixtures/formulas_statistics.py</c>'s independent transcription of Fortran
/// lines 771-819, 963-1016, 1087-1113, 1137-1152 and 1168-1174, written from the Fortran
/// and from <c>CyclePlane.listing.generated.txt</c> alone. Each case of
/// <c>cases/statistics/cycle_statistics.json</c> feeds its constructed totals to
/// <c>Compute</c> under each <see cref="PrecisionKind"/> and compares, bit for bit, every
/// value the script computed: the report, <c>nextPdoksmall</c>, <c>nextDmaxxx</c> and the
/// in-place rewrite of the totals. The names the script emits and the report members
/// below are paired both ways, so a value either side lacks fails the test rather than
/// going unchecked.
/// </summary>
public class CycleStatisticsFormulaTests
{
    private sealed record Outcome(
        CycleReport Report, double[] NextPdoksmall, double NextDmaxxx, long[][] QdoksAfter, double[] Dokp41After,
        double[] Dokp31After, double[] VdokTotalAfter, double VdokTotal2After);

    private static readonly Dictionary<string, Func<Outcome, object>> Members = new()
    {
        ["eps1"] = o => o.Report.Eps1,
        ["eps2"] = o => o.Report.Eps2,
        ["eps3"] = o => o.Report.Eps3,
        ["eps4"] = o => o.Report.Eps4,
        ["eps5"] = o => o.Report.Eps5,
        ["eps6"] = o => o.Report.Eps6,
        ["eps7"] = o => o.Report.Eps7,
        ["dok43b"] = o => o.Report.Dok43b,
        ["dok43s"] = o => o.Report.Dok43s,
        ["epsalldok"] = o => o.Report.Epsalldok,
        ["allvdokso"] = o => o.Report.Allvdokso,
        ["alldok432"] = o => o.Report.Alldok432,
        ["alldok43"] = o => o.Report.Alldok43,
        ["epsx1"] = o => o.Report.Epsx1,
        ["epsx2"] = o => o.Report.Epsx2,
        ["epsx3"] = o => o.Report.Epsx3,
        ["oxidizerAccuracyWarning"] = o => o.Report.OxidizerAccuracyWarning,
        ["qkss"] = o => o.Report.Qkss,
        ["qks1"] = o => o.Report.Qks1,
        ["epsy"] = o => o.Report.Epsy,
        ["pocketAccuracyWarning"] = o => o.Report.PocketAccuracyWarning,
        ["epsmd4"] = o => o.Report.Epsmd4,
        ["epsmd3"] = o => o.Report.Epsmd3,
        ["vkso"] = o => o.Report.Vkso,
        ["d432"] = o => o.Report.D432,
        ["dqkarm"] = o => o.Report.Dqkarm,
        ["dp43"] = o => o.Report.Dp43,
        ["sdevp43"] = o => o.Report.Sdevp43,
        ["gdokleft"] = o => o.Report.Gdokleft,
        ["plotsmdok"] = o => o.Report.Plotsmdok,
        ["vdokleft"] = o => o.Report.Vdokleft,
        ["plotsm"] = o => o.Report.Plotsm,
        ["mp"] = o => o.Report.Mp,
        ["dpRow"] = o => o.Report.DpRow,
        ["dpockets"] = o => o.Report.Dpockets,
        ["dokp43"] = o => o.Report.Dokp43,
        ["qdokkarm"] = o => o.Report.Qdokkarm,
        ["qdokso"] = o => o.Report.Qdokso,
        ["qdoksAfter"] = o => o.QdoksAfter,
        ["dokp41After"] = o => o.Dokp41After,
        ["dokp31After"] = o => o.Dokp31After,
        ["pdoksmall"] = o => o.Report.Pdoksmall,
        ["nextPdoksmall"] = o => o.NextPdoksmall,
        ["nextDmaxxx"] = o => o.NextDmaxxx,
        ["vdokTotalAfter"] = o => o.VdokTotalAfter,
        ["vdokTotal2After"] = o => o.VdokTotal2After,
        ["dkarm43Cor"] = o => o.Report.Dkarm43Cor,
        ["sdevp43Cor"] = o => o.Report.Sdevp43Cor,
        ["dqkarmCor"] = o => o.Report.DqkarmCor,
        ["dfmk432"] = o => o.Report.Dfmk432,
        ["sdevp243"] = o => o.Report.Sdevp243,
        ["dqmkm1"] = o => o.Report.Dqmkm1,
        ["dqmkm2"] = o => o.Report.Dqmkm2,
        ["qmcoef"] = o => o.Report.Qmcoef,
        ["fmkarmCorNormalized"] = o => o.Report.FmkarmCorNormalized,
        ["fmkarm2Normalized"] = o => o.Report.Fmkarm2Normalized,
        ["fqkarmCorNormalized"] = o => o.Report.FqkarmCorNormalized,
        ["qmkm1Normalized"] = o => o.Report.Qmkm1Normalized,
        ["qmkm2Normalized"] = o => o.Report.Qmkm2Normalized,
        ["coefNormalized"] = o => o.Report.CoefNormalized,
        ["coefNmax"] = o => o.Report.CoefNmax,
        ["qmkm1Nmax"] = o => o.Report.Qmkm1Nmax,
        ["qmkm2Nmax"] = o => o.Report.Qmkm2Nmax,
        ["dolM1"] = o => o.Report.DolM1,
        ["dolM2"] = o => o.Report.DolM2,
        ["dolM3"] = o => o.Report.DolM3,
        ["convergenceEpsy"] = o => o.Report.ConvergenceEpsy,
        ["convergenceEpsmd3"] = o => o.Report.ConvergenceEpsmd3,
        ["convergenceEpsmd4"] = o => o.Report.ConvergenceEpsmd4,
        ["convergenceAlldok43"] = o => o.Report.ConvergenceAlldok43,
        ["convergenceAlldoksd"] = o => o.Report.ConvergenceAlldoksd,
        ["convergenceDolM2"] = o => o.Report.ConvergenceDolM2,
    };

    public static IEnumerable<object[]> Cases()
    {
        foreach (var c in FormulaCaseFiles.ReadCycleCases())
        {
            yield return new object[] { c.Name, nameof(PrecisionKind.Binary64) };
            yield return new object[] { c.Name, nameof(PrecisionKind.Original) };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ComputeMatchesTheFormulaScript(string caseName, string kindName)
    {
        var c = FormulaCaseFiles.FindCycleCase(caseName);
        var kind = Enum.Parse<PrecisionKind>(kindName);
        var expected = kind == PrecisionKind.Original ? c.ExpectedOriginal : c.Expected;

        var outcome = Run(c, kind);

        Assert.Empty(expected.Keys.Except(Members.Keys));
        if (c.CycleIndex > 0)
        {
            Assert.Empty(Members.Keys.Except(expected.Keys));
        }

        var mismatches = new List<string>();
        foreach (var (name, expectedValue) in expected)
        {
            Compare(name, expectedValue, Members[name](outcome), mismatches);
        }

        Assert.True(mismatches.Count == 0, $"{caseName}/{kindName}: {string.Join("; ", mismatches)}");
    }

    private static Outcome Run(CycleCase c, PrecisionKind kind)
    {
        var s = c.Setup;
        var layout = AccumulatorLayout.Create(s.FractionCount, s.Ndok, s.Nkarm, s.Ncat, s.Nc);
        var integerTotals = new long[layout.IntegerLength];
        var realTotals = new double[layout.RecordLength];
        FillIntegerTotals(c, layout, integerTotals);
        FillRealTotals(c, layout, realTotals);

        var setup = new ModelSetup
        {
            FractionCount = s.FractionCount,
            SizeLaw = 2,
            CellSize = s.CellSize,
            CategoryStep = s.CategoryStep,
            Dmin = s.Dmin,
            Dmax = c.Echoes.Ddokmax,
            Lambda = 1.0,
            Ak1 = 0.5,
            Ak2 = s.Ak2,
            Ak3 = 0.27,
            Ak4 = 4.7,
            Variant = 1,
            Alpha = 0.25,
            PocketCoefficient = s.PocketCoefficient,
            BridgeCoefficient = s.BridgeCoefficient,
            NnMin = 0.0,
            NnMax = 1e9,
            Ndok = s.Ndok,
            Nkarm = s.Nkarm,
            Ncat = s.Ncat,
            Nc = s.Nc,
            NeighbourBudget = 1000,
            PocketRedrawBudget = 1000,
            Kind = kind,
            Layout = layout,
        };
        var tables = new SetupTables(
            Bounds: new double[2 * s.FractionCount],
            Cumulative: new double[s.FractionCount + 1],
            Share: c.Tables.Share.ToArray(),
            PocketForming: c.Tables.PocketForming.ToArray(),
            Zss: 1.0,
            MassShare: c.Tables.MassShare.ToArray());
        var echoes = new SetupEchoes(
            c.Echoes.Dokm, c.Echoes.Doksd, c.Echoes.Ddokmax, c.Echoes.Ddokmax,
            TailProbabilityModified: 0.0, OxidizerMassFractionEffective: c.Echoes.Ggg);
        var i = c.Inputs;
        var inputs = new SetupInputs(
            i.OxidizerDensity, i.PropellantDensity, i.OxidizerMassFraction, i.MetalMassFraction,
            i.HomogenizedOxidizerFraction, i.EpsDok, i.AggregatedOxideFraction);

        var nextPdoksmall = new double[s.Ndok];
        var report = CycleStatistics.Compute(
            setup, tables, echoes, inputs, c.CycleIndex, integerTotals, realTotals, nextPdoksmall,
            out var nextDmaxxx, out var status);
        Assert.Equal(CategoriesStatus.Ok, status);

        return new Outcome(
            report, nextPdoksmall, nextDmaxxx,
            Enumerable.Range(0, s.Ncat).Select(r => integerTotals.AsSpan(layout.Qdoks + r * s.Ndok, s.Ndok).ToArray()).ToArray(),
            realTotals.AsSpan(layout.Dokp41, s.Ncat).ToArray(), realTotals.AsSpan(layout.Dokp31, s.Ncat).ToArray(),
            realTotals.AsSpan(layout.VdokTotal, s.Ndok).ToArray(), realTotals[layout.VdokTotal2]);
    }

    private static void FillIntegerTotals(CycleCase c, AccumulatorLayout layout, long[] totals)
    {
        var t = c.IntegerTotals;
        totals[layout.Nfx] = t.Nfx;
        totals[layout.Nfy] = t.Nfy;
        totals[layout.Nfz] = t.Nfz;
        totals[layout.Nfq] = t.Nfq;
        totals[layout.Nfw] = t.Nfw;
        Copy(t.AlldokFract, totals, layout.AlldokFract);
        Copy(t.Alldok, totals, layout.Alldok);
        Copy(t.Qks, totals, layout.Qks);
        Copy(t.FqkarmCor, totals, layout.FqkarmCor);
        Copy(t.Coef, totals, layout.Coef);
        Copy(t.Qmkm1, totals, layout.Qmkm1);
        Copy(t.Qmkm2, totals, layout.Qmkm2);
        for (var r = 0; r < t.Qdoks.Count; r++)
        {
            Copy(t.Qdoks[r], totals, layout.Qdoks + r * c.Setup.Ndok);
        }
    }

    private static void FillRealTotals(CycleCase c, AccumulatorLayout layout, double[] totals)
    {
        var t = c.RealTotals;
        Copy(t.Xss, totals, layout.Xss);
        totals[layout.Sd4] = t.Sd4;
        totals[layout.Sd3] = t.Sd3;
        totals[layout.D41] = t.D41;
        totals[layout.D31] = t.D31;
        totals[layout.DokBase41] = t.DokBase41;
        totals[layout.DokBase31] = t.DokBase31;
        totals[layout.DokSur41] = t.DokSur41;
        totals[layout.DokSur31] = t.DokSur31;
        totals[layout.Dp41] = t.Dp41;
        totals[layout.Dp31] = t.Dp31;
        totals[layout.DpMax] = t.DpMax;
        totals[layout.DpMaxCor] = t.DpMaxCor;
        Copy(t.Allvdok, totals, layout.Allvdok);
        Copy(t.Vdokstr, totals, layout.Vdokstr);
        Copy(t.Vks, totals, layout.Vks);
        Copy(t.FmkarmCor, totals, layout.FmkarmCor);
        Copy(t.Fmkarm2, totals, layout.Fmkarm2);
        Copy(t.Dokp41, totals, layout.Dokp41);
        Copy(t.Dokp31, totals, layout.Dokp31);
        Copy(t.Vsmkm, totals, layout.Vsmkm);
        Copy(t.Svd, totals, layout.Svd);
        Copy(t.VmkmTotal, totals, layout.VmkmTotal);
        Copy(t.VdokTotal, totals, layout.VdokTotal);
        totals[layout.VmkmTotal2] = t.VmkmTotal2;
        totals[layout.VdokTotal2] = t.VdokTotal2;
    }

    private static void Copy<T>(IReadOnlyList<T> source, T[] destination, int offset)
    {
        for (var k = 0; k < source.Count; k++)
        {
            destination[offset + k] = source[k];
        }
    }

    private static void Compare(string label, JsonElement expected, object actual, List<string> mismatches)
    {
        switch (actual)
        {
            case double value:
                var wanted = ReadDouble(expected);
                var same = double.IsNaN(wanted) ? double.IsNaN(value) : BitConverter.DoubleToInt64Bits(wanted) == BitConverter.DoubleToInt64Bits(value);
                if (!same)
                {
                    mismatches.Add(string.Create(CultureInfo.InvariantCulture, $"{label} expected {wanted:R}, got {value:R}"));
                }

                break;
            case bool flag:
                if (expected.GetBoolean() != flag)
                {
                    mismatches.Add($"{label} expected {expected.GetBoolean()}, got {flag}");
                }

                break;
            case int or long:
                var integer = Convert.ToInt64(actual, CultureInfo.InvariantCulture);
                if (expected.GetInt64() != integer)
                {
                    mismatches.Add($"{label} expected {expected.GetInt64()}, got {integer}");
                }

                break;
            case IEnumerable items:
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = items.Cast<object>().ToArray();
                if (expectedItems.Length != actualItems.Length)
                {
                    mismatches.Add($"{label} length {actualItems.Length}, expected {expectedItems.Length}");
                    break;
                }

                for (var k = 0; k < actualItems.Length; k++)
                {
                    Compare($"{label}[{k}]", expectedItems[k], actualItems[k], mismatches);
                }

                break;
            default:
                mismatches.Add($"{label} has no comparison for {actual.GetType().Name}");
                break;
        }
    }

    private static double ReadDouble(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? double.Parse(element.GetString()!, CultureInfo.InvariantCulture) : element.GetDouble();
}
