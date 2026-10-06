using System.Collections.Frozen;
using PropStruct.Particle;

namespace PropStruct.Statistics.Tests;

/// <summary>The two totals buffers one <c>CycleStatistics.Compute</c> reads and rewrites, with the sizes that lay them out.</summary>
internal sealed record ComputeBuffers(AccumulatorLayout Layout, long[] Integers, double[] Reals, int FractionCount, int Ndok, int Nkarm, int Ncat)
{
    /// <summary>The buffers of a setup: every total empty.</summary>
    public static ComputeBuffers Empty(in ModelSetup setup) =>
        new(setup.Layout, new long[setup.Layout.IntegerLength], new double[setup.Layout.RecordLength], setup.FractionCount, setup.Ndok, setup.Nkarm, setup.Ncat);
}

/// <summary>What one <c>CycleStatistics.Compute</c> left: the report, the rewritten totals, the next cycle's inputs.</summary>
internal sealed record ComputeResult(CycleReport Report, ComputeBuffers Buffers, double[] NextPdoksmall, double NextDmaxxx);

/// <summary>
/// The one binding between the listing oracle's names (<c>src/Statistics/CyclePlaneListing.map.txt</c>, the
/// names of its cells and arrays and of its <c>oracle | total</c> rows, lower-cased) and the port's slots: where
/// the port takes each total from, what it reads each result from, and why a name has no slot. Every name of the
/// map is in exactly one of the four tables, and every name here is in the map (<c>ListingOracleTests</c>,
/// "the binding is complete both ways").
/// </summary>
internal static class OracleBinding
{
    /// <summary>The oracle's name of each total, and how its value goes into the port's buffers.</summary>
    public static readonly FrozenDictionary<string, Action<ComputeBuffers, OracleSlot>> Totals = BuildTotals();

    /// <summary>The oracle's name of each result, and the port's elements of it as (oracle index, value).</summary>
    public static readonly FrozenDictionary<string, Func<ComputeResult, IReadOnlyList<(int Index, double Value)>?>> Outputs = BuildOutputs();

    /// <summary>The names that are the setup's: asserted against <c>Setup.Prepare</c> (or taken from the executable's pre-loop), not totals or results.</summary>
    public static readonly FrozenSet<string> SetupInputs = new[]
    {
        "ak2", "di", "dj", "eps_dok", "eta", "gdok", "ggg", "gm", "karmcoef", "mkmcoef", "plot1", "plot2", "sfr", "z11",
        "zss", "zx", "ddok", "dokm", "doksd", "slamd",
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>The names with no slot in the port, each with the reason.</summary>
    public static readonly FrozenDictionary<string, string> Declared = BuildDeclared();

    /// <summary>The names the port reads from the totals but has no oracle input for, rewritten by Compute (a total and a result).</summary>
    public static readonly FrozenSet<string> RewrittenTotals = new[] { "qdoks", "dokp41", "dokp31", "vdok_total", "vdok_total2" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every name the binding knows.</summary>
    public static IEnumerable<string> AllNames => Totals.Keys.Concat(Outputs.Keys).Concat(SetupInputs).Concat(Declared.Keys).Distinct(StringComparer.Ordinal);

    private static Action<ComputeBuffers, OracleSlot> Integer(Func<AccumulatorLayout, int> offset) =>
        (buffers, slot) =>
        {
            for (var i = 0; i < slot.Count; i++)
            {
                buffers.Integers[offset(buffers.Layout) + i] = (long)slot.At(i);
            }
        };

    private static Action<ComputeBuffers, OracleSlot> Real(Func<AccumulatorLayout, int> offset) =>
        (buffers, slot) =>
        {
            for (var i = 0; i < slot.Count; i++)
            {
                buffers.Reals[offset(buffers.Layout) + i] = slot.At(i);
            }
        };

    private static FrozenDictionary<string, Action<ComputeBuffers, OracleSlot>> BuildTotals()
    {
        var totals = new Dictionary<string, Action<ComputeBuffers, OracleSlot>>(StringComparer.Ordinal)
        {
            ["nfx"] = Integer(l => l.Nfx),
            ["nfy"] = Integer(l => l.Nfy),
            ["nfz"] = Integer(l => l.Nfz),
            ["nfq"] = Integer(l => l.Nfq),
            ["nfw"] = Integer(l => l.Nfw),
            ["alldok_fract"] = Integer(l => l.AlldokFract),
            ["alldok"] = Integer(l => l.Alldok),
            ["qks"] = Integer(l => l.Qks),
            ["fqkarm_cor"] = Integer(l => l.FqkarmCor),
            ["coef"] = Integer(l => l.Coef),
            ["qmkm1"] = Integer(l => l.Qmkm1),
            ["qmkm2"] = Integer(l => l.Qmkm2),
            ["sd3"] = Real(l => l.Sd3),
            ["sd4"] = Real(l => l.Sd4),
            ["dok_base31"] = Real(l => l.DokBase31),
            ["dok_base41"] = Real(l => l.DokBase41),
            ["dok_sur31"] = Real(l => l.DokSur31),
            ["dok_sur41"] = Real(l => l.DokSur41),
            ["d31"] = Real(l => l.D31),
            ["d41"] = Real(l => l.D41),
            ["dp31"] = Real(l => l.Dp31),
            ["dp41"] = Real(l => l.Dp41),
            ["dpmax"] = Real(l => l.DpMax),
            ["allvdok"] = Real(l => l.Allvdok),
            ["vdokstr"] = Real(l => l.Vdokstr),
            ["vsmkm"] = Real(l => l.Vsmkm),
            ["svd"] = Real(l => l.Svd),
            ["vmkm_total"] = Real(l => l.VmkmTotal),
            ["vdok_total"] = Real(l => l.VdokTotal),
            ["vmkm_total2"] = Real(l => l.VmkmTotal2),
            ["vdok_total2"] = Real(l => l.VdokTotal2),
            ["vks"] = Real(l => l.Vks),
            ["fmkarm_cor"] = Real(l => l.FmkarmCor),
            ["fmkarm2"] = Real(l => l.Fmkarm2),
            ["dokp41"] = Real(l => l.Dokp41),
            ["dokp31"] = Real(l => l.Dokp31),
            ["qdoks"] = (buffers, slot) =>
            {
                // The oracle's QDOKS(Ncat, Ndok) is column-major, the port's Qdoks row-major by category.
                for (var category = 0; category < buffers.Ncat; category++)
                {
                    for (var cell = 0; cell < buffers.Ndok; cell++)
                    {
                        buffers.Integers[buffers.Layout.Qdoks + category * buffers.Ndok + cell] = (long)slot.At(category + cell * buffers.Ncat);
                    }
                }
            },
        };
        for (var k = 0; k < 7; k++)
        {
            var cell = k;
            totals[$"xss{k}"] = (buffers, slot) => buffers.Reals[buffers.Layout.Xss + cell] = slot.At(0);
        }

        return totals.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static List<(int Index, double Value)> Elements(IEnumerable<double> values) =>
        values.Select((value, index) => (index, value)).ToList();

    private static Func<ComputeResult, IReadOnlyList<(int Index, double Value)>?> Scalar(Func<CycleReport, double> field) =>
        result => double.IsNaN(field(result.Report)) ? null : [(0, field(result.Report))];

    private static Func<ComputeResult, IReadOnlyList<(int Index, double Value)>?> FromBuffer(Func<ComputeResult, double[]> slice) =>
        result => Elements(slice(result));

    private static FrozenDictionary<string, Func<ComputeResult, IReadOnlyList<(int Index, double Value)>?>> BuildOutputs()
    {
        var outputs = new Dictionary<string, Func<ComputeResult, IReadOnlyList<(int Index, double Value)>?>>(StringComparer.Ordinal)
        {
            ["alldok43"] = Scalar(r => r.Alldok43),
            ["alldok432"] = Scalar(r => r.Alldok432),
            ["d432"] = Scalar(r => r.D432),
            ["dfmk432"] = Scalar(r => r.Dfmk432),
            ["dkarm43_cor"] = Scalar(r => r.Dkarm43Cor),
            ["dok43b"] = Scalar(r => r.Dok43b),
            ["dok43s"] = Scalar(r => r.Dok43s),
            ["dolm1"] = Scalar(r => r.DolM1),
            ["dolm2"] = Scalar(r => r.DolM2),
            ["dolm3"] = Scalar(r => r.DolM3),
            ["dp43"] = Scalar(r => r.Dp43),
            ["dqkarm"] = Scalar(r => r.Dqkarm),
            ["dqkarm_cor"] = Scalar(r => r.DqkarmCor),
            ["dqmkm1"] = Scalar(r => r.Dqmkm1),
            ["dqmkm2"] = Scalar(r => r.Dqmkm2),
            ["eps1"] = Scalar(r => r.Eps1),
            ["eps2"] = Scalar(r => r.Eps2),
            ["eps3"] = Scalar(r => r.Eps3),
            ["eps4"] = Scalar(r => r.Eps4),
            ["eps5"] = Scalar(r => r.Eps5),
            ["eps6"] = Scalar(r => r.Eps6),
            ["eps7"] = Scalar(r => r.Eps7),
            ["epsmd3"] = Scalar(r => r.Epsmd3),
            ["epsmd4"] = Scalar(r => r.Epsmd4),
            ["epsx1"] = Scalar(r => r.Epsx1),
            ["epsx2"] = Scalar(r => r.Epsx2),
            ["epsx3"] = Scalar(r => r.Epsx3),
            ["epsy"] = Scalar(r => r.Epsy),
            ["gdokleft"] = Scalar(r => r.Gdokleft),
            ["mp"] = Scalar(r => r.Mp),
            ["plotsmdok"] = Scalar(r => r.Plotsmdok),
            ["plotsm"] = Scalar(r => r.Plotsm),
            ["qmcoef"] = Scalar(r => r.Qmcoef),
            ["sdevp243"] = Scalar(r => r.Sdevp243),
            ["sdevp43"] = Scalar(r => r.Sdevp43),
            ["sdevp43_cor"] = Scalar(r => r.Sdevp43Cor),
            ["vdokleft"] = Scalar(r => r.Vdokleft),
            ["dmaxxx"] = result => [(0, result.NextDmaxxx)],
            ["pdoksmall"] = result => Elements(result.NextPdoksmall),
            ["epsalldok"] = result => Elements(result.Report.Epsalldok),
            ["allvdokso"] = result => Elements(result.Report.Allvdokso),
            ["vkso"] = result => Elements(result.Report.Vkso),
            ["dokp43"] = result => Elements(result.Report.Dokp43),
            ["dpockets"] = result => Elements(result.Report.Dpockets),
            ["qdokkarm"] = result => Elements(result.Report.Qdokkarm),
            ["qdokso"] = result =>
            {
                // QDOKSO(Ncat, Ndok) is column-major: row r, cell i is oracle element r + i * Ncat.
                var elements = new List<(int Index, double Value)>();
                for (var row = 0; row < result.Report.Qdokso.Length; row++)
                {
                    for (var cell = 0; cell < result.Report.Qdokso[row].Length; cell++)
                    {
                        elements.Add((row + cell * result.Buffers.Ncat, result.Report.Qdokso[row][cell]));
                    }
                }

                return elements;
            },
            ["qdoks"] = result =>
            {
                var elements = new List<(int Index, double Value)>();
                var buffers = result.Buffers;
                for (var category = 0; category < buffers.Ncat; category++)
                {
                    for (var cell = 0; cell < buffers.Ndok; cell++)
                    {
                        elements.Add((category + cell * buffers.Ncat, buffers.Integers[buffers.Layout.Qdoks + category * buffers.Ndok + cell]));
                    }
                }

                return elements;
            },
            ["dokp41"] = FromBuffer(r => r.Buffers.Reals.AsSpan(r.Buffers.Layout.Dokp41, r.Buffers.Ncat).ToArray()),
            ["dokp31"] = FromBuffer(r => r.Buffers.Reals.AsSpan(r.Buffers.Layout.Dokp31, r.Buffers.Ncat).ToArray()),
            ["vdok_total"] = FromBuffer(r => r.Buffers.Reals.AsSpan(r.Buffers.Layout.VdokTotal, r.Buffers.Ndok).ToArray()),
            ["vdok_total2"] = result => [(0, result.Buffers.Reals[result.Buffers.Layout.VdokTotal2])],
        };
        return outputs.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, string> BuildDeclared()
    {
        var declared = new Dictionary<string, string>(StringComparer.Ordinal);
        void Declare(string reason, params string[] names)
        {
            foreach (var name in names)
            {
                declared.Add(name, reason);
            }
        }

        Declare("loop bookkeeping the port has no slot for: the cycle's particle count plus one", "ipart");
        Declare("a helper of the oracle's own draw of the totals, never injected", "kmax", "decay");
        Declare("derived by the port from qks (Pockets sums it), not read from the totals", "qkss");
        Declare("derived by the port from qks and qkss, the plane's own line 718", "qks1");
        Declare("feeds nothing the port reports and is not accumulated (Categories.cs's own summary)", "qdok", "vdoks", "vvdoks", "vdokso", "qdokss", "qdokstr");
        Declare("a local of the port's own transcription, not a report field: the plane's internal value", "alldoksd", "allvdok_fr", "dd3", "dd4", "md3", "md4", "ddok3", "ddok4", "mdok3", "mdok4", "dokp431", "dokp432", "epsmdok3", "epsmdok4", "epsydok", "qdoks1", "xr", "xsr0", "xsr1", "xsr2", "xsr3", "xsr4", "xsr5", "xsr6", "fmdok");
        Declare("small-particle internals: only the probability pdoksmall and the next Dmaxxx leave the port's SmallParticles", "zdoksmall", "ddoksmall", "vdoksmall");
        Declare("the plane's scratch arrays, ported as locals of Pockets and CorrectedPockets", "dpoc", "fqks", "fmkarm2_loc");
        Declare("computed by the executable and read by no line (the generated table's kind E); the port does not port them", "gk0", "gk1");
        Declare("convergence arrays indexed by the cycle counter; the port reports the six values as scalars, not bound until the index is read from the oracle", "par1", "par2", "par3", "par4", "par5", "par6");
        return declared.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
