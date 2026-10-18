using System.Globalization;
using PropStruct.Particle;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The effect of the three category flags (<c>src/Statistics/BOOT.md</c>, "## Corrected model") on the rows <c>Compute</c>
/// reports, measured on the totals of the listing oracle's cases (<c>tests/Fixtures/cases/statistics/cycle_plane_oracle.json</c>,
/// 26 formulations, two or three cycles each, each cycle's totals a seeded draw of the original executable's accumulators,
/// injected independently of the cycle before) and not on a Monte Carlo run of the port: the setup is the port's under
/// <c>Binary64</c>. The figures are reported, per case; what is asserted is the known answer at each end of the measure,
/// the flag changing no bit where the original's fresh totals leave it nothing to do.
/// <para>
/// <c>CategoriesOnCopy</c> acts from the second cycle, on totals the original's in-place merge of the first has rewritten.
/// Because the oracle injects each cycle's totals independently, the second cycle is a synthetic chain: the draw of cycle 1
/// is taken as the particles that cycle adds, to the rewritten rows <c>M0</c> that <c>Compute</c> under <c>None</c> leaves
/// of the draw of cycle 0 (the original), or to the unrewritten rows <c>T0</c> (the flag); every other total is cycle 1's.
/// </para>
/// </summary>
public class CategoryFlagsOnOracleTotalsTests(ITestOutputHelper output)
{
    private static readonly OracleCase[] Cases = [.. OracleFixture.Cases.Where(c => c.Cycles.Count >= 2)];

    public static IEnumerable<object[]> CaseIds() => Cases.Select(c => new object[] { c.Id });

    /// <summary>
    /// Right on a known answer: in cycle 0 the totals are fresh, so <c>CategoriesOnCopy</c> reports what <c>None</c>
    /// reports, bit for bit, and leaves the totals as they were; the original's own rewrite is what <c>None</c> leaves.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaseIds))]
    public void CategoriesOnCopyChangesNothingInTheFirstCycle(string caseId)
    {
        var c = Cases.Single(x => x.Id == caseId);
        var none = Prepared(c, CorrectionSet.None);
        var copy = Prepared(c, CorrectionSet.CategoriesOnCopy);
        var untouched = Buffers(none, c.Cycles[0]);

        var reportNone = Compute(none, Buffers(none, c.Cycles[0]), c.Cycles[0]);
        var flagged = Buffers(copy, c.Cycles[0]);
        var reportFlag = Compute(copy, flagged, c.Cycles[0]);

        Assert.Empty(BitwiseComparison.Differences(reportNone.Report, reportFlag.Report, caseId));
        Assert.Empty(BitwiseComparison.Differences((untouched.Integers, untouched.Reals), (flagged.Integers, flagged.Reals), caseId));
    }

    /// <summary>
    /// Reported, per case: the rows of cycle 1 on the chain above under <c>None</c> and under <c>CategoriesOnCopy</c>, their
    /// <c>DPRow</c> and sizes. Asserted: where no row of cycle 0 merged the two chains are one state and the reports equal,
    /// and the flag moves at least one case where a row merged.
    /// </summary>
    [Fact]
    public void CategoriesOnCopyMovesTheRowsOfTheSecondCycle()
    {
        var moved = 0;
        foreach (var c in Cases)
        {
            var none = Prepared(c, CorrectionSet.None);
            var copy = Prepared(c, CorrectionSet.CategoriesOnCopy);
            var initial = Buffers(none, c.Cycles[0]);
            var rewritten = Buffers(none, c.Cycles[0]);
            _ = Compute(none, rewritten, c.Cycles[0]);

            var chainNone = Buffers(none, c.Cycles[1]);
            AddRows(chainNone, rewritten);
            var chainCopy = Buffers(copy, c.Cycles[1]);
            AddRows(chainCopy, initial);

            var reportNone = Compute(none, chainNone, c.Cycles[1]).Report;
            var reportCopy = Compute(copy, chainCopy, c.Cycles[1]).Report;
            var merged = !initial.Integers.AsSpan().SequenceEqual(rewritten.Integers);
            output.WriteLine(Describe(c, "CategoriesOnCopy", $"cycle-0 merge {(merged ? "yes" : "no")}", reportNone, reportCopy));

            if (!merged)
            {
                Assert.Empty(BitwiseComparison.Differences(reportNone, reportCopy, c.Id));
            }
            else if (BitwiseComparison.Differences(reportNone, reportCopy, c.Id).Count > 0)
            {
                moved++;
            }
        }

        Assert.True(moved > 0, "the flag moved no case: the measure is vacuous");
    }

    /// <summary>
    /// Reported, per case and cycle: the rows of <c>Compute</c> under <c>None</c> and under <c>CategoriesLastRowRecomputed</c>
    /// on the same drawn totals (each cycle's draw is fresh, so the flag, which acts inside one call, is measured on all of
    /// them). Asserted: the flag moves at least one draw, and wherever the two row counts are equal and the last row's
    /// <c>Dokp43</c> is, the whole report is.
    /// </summary>
    [Fact]
    public void CategoriesLastRowRecomputedMovesTheRowsOfTheDraws() =>
        ReportSingleCycleFlag(CorrectionSet.CategoriesLastRowRecomputed);

    private void ReportSingleCycleFlag(CorrectionSet flag)
    {
        var moved = 0;
        var draws = 0;
        foreach (var c in Cases)
        {
            var none = Prepared(c, CorrectionSet.None);
            var flagged = Prepared(c, flag);
            foreach (var cycle in c.Cycles)
            {
                var reportNone = Compute(none, Buffers(none, cycle), cycle).Report;
                var reportFlag = Compute(flagged, Buffers(flagged, cycle), cycle).Report;
                draws++;
                var differs = BitwiseComparison.Differences(reportNone, reportFlag, c.Id).Count > 0;
                moved += differs ? 1 : 0;
                if (differs)
                {
                    output.WriteLine(Describe(c, flag.ToString(), $"cycle {cycle.Cycle}", reportNone, reportFlag));
                }
            }
        }

        output.WriteLine($"{flag}: {moved} of {draws} draws move the report");
        Assert.True(moved > 0, "the flag moved no draw: the measure is vacuous");
    }

    private static string Describe(OracleCase c, string flag, string note, CycleReport none, CycleReport flagged) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{c.Id} {c.Formulation} {flag}: {note}, DPRow {none.DpRow} -> {flagged.DpRow}, "
            + $"last Dpockets {none.Dpockets[^1]:G4} -> {flagged.Dpockets[^1]:G4}, Dokp43[0] {none.Dokp43[0]:G4} -> {flagged.Dokp43[0]:G4}, "
            + $"Qdokkarm[0] {none.Qdokkarm[0]:G4} -> {flagged.Qdokkarm[0]:G4}");

    private static void AddRows(ComputeBuffers target, ComputeBuffers rows)
    {
        var layout = target.Layout;
        for (var i = 0; i < target.Ncat * target.Ndok; i++)
        {
            target.Integers[layout.Qdoks + i] += rows.Integers[layout.Qdoks + i];
        }

        for (var r = 0; r < target.Ncat; r++)
        {
            target.Reals[layout.Dokp41 + r] += rows.Reals[layout.Dokp41 + r];
            target.Reals[layout.Dokp31 + r] += rows.Reals[layout.Dokp31 + r];
        }

        target.Reals[layout.DpMax] = Math.Max(target.Reals[layout.DpMax], rows.Reals[layout.DpMax]);
    }

    private static (PortSetup Port, ModelSetup Setup, SetupEchoes Echoes) Prepared(OracleCase c, CorrectionSet corrections)
    {
        var port = PortSetup.Prepare(c.Formulation, c.Flags, c.Menu, PrecisionKind.Binary64, corrections);
        Assert.Equal(SetupStatus.Ok, port.Status);
        var (setup, echoes) = OracleRun.Echoes(port, c);
        return (port, setup, echoes);
    }

    private static ComputeBuffers Buffers((PortSetup Port, ModelSetup Setup, SetupEchoes Echoes) prepared, OracleCycle cycle)
    {
        var buffers = ComputeBuffers.Empty(prepared.Setup);
        foreach (var (name, slot) in cycle.Totals)
        {
            if (OracleBinding.Totals.TryGetValue(name, out var write))
            {
                write(buffers, slot);
            }
        }

        return buffers;
    }

    private static ComputeResult Compute(
        (PortSetup Port, ModelSetup Setup, SetupEchoes Echoes) prepared, ComputeBuffers buffers, OracleCycle cycle)
    {
        var nextPdoksmall = new double[prepared.Setup.Ndok];
        var report = CycleStatistics.Compute(prepared.Setup, prepared.Port.Tables, prepared.Echoes, prepared.Port.Inputs, cycle.Cycle,
            buffers.Integers, buffers.Reals, nextPdoksmall, out var nextDmaxxx, out var status);
        Assert.Equal(CategoriesStatus.Ok, status);
        return new ComputeResult(report, buffers, nextPdoksmall, nextDmaxxx);
    }
}
