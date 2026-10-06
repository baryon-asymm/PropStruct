using ILGPU.Runtime;
using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// The Snapshot row of the BOOT.md levels table: the first 1000 attempts of cycle 0 of
/// each reference formulation, run through <see cref="Attempt.Run"/> exactly as
/// reference mode would (root BOOT.md, "Reference mode is the original's sequence"):
/// the <c>Original</c> layout's own initial six streams, threaded from one attempt to
/// the next, one record per base particle (reset to zero only when the previous
/// attempt for it was <see cref="AttemptOutcome.Accepted"/>, matching "real-valued
/// accumulators go to a per-particle record" — root BOOT.md, Invariants), the shared
/// integer totals never reset.
///
/// QKS1 is not refreshed between attempts here, and <see cref="CycleInputs.Qks1"/> is
/// left at its cycle-0 default (<see cref="ConstructedModel.CycleZero"/>): cycle 0
/// never reads it. Every bridge of a cycle-0 attempt takes the "cycle 0 → 550" branch
/// immediately after its own per-bridge counter update, before the QKS1 window is ever
/// built (`src/Particle/BOOT.md`, "Attempt structure", the `bridge` row; the two-line
/// span "580–581" of its own "## Line map" is the per-bridge increment and this
/// cycle-0 jump). So a driver that never calls <c>PocketHistogram.Normalize</c> is not
/// a shortcut for this row specifically: refreshing QKS1 would change a value no
/// cycle-0 attempt ever consults.
///
/// Each attempt's own row of the trace hashed below is, in order: the outcome (as a
/// byte), the six streams' <c>Low</c>/<c>High</c> limbs (as `double`s carrying their
/// exact bit pattern via <see cref="BitConverter.UInt64BitsToDouble"/>, not a numeric
/// conversion — a `ulong` above 2^53 would lose bits to a numeric cast), the shared
/// integer totals (each `long` likewise reinterpreted bit for bit via
/// <see cref="BitConverter.Int64BitsToDouble"/>), then the current particle's own
/// record, verbatim. Scratch is not included: this row asks for "outcomes, stream
/// states, integer totals and records", not the per-attempt working buffer, and
/// nothing this trace carries is a fold's own output (<see cref="Fold.Add"/> is not
/// called here at all) — the coordinator's own condition for this row.
/// </summary>
public class SnapshotTests : IClassFixture<CpuHost>
{
    /// <summary>The scope of every reference BOOT.md row named "the first 1000 attempts".</summary>
    public const int AttemptCount = 1000;

    private readonly CpuHost _host;

    public SnapshotTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> Formulations()
    {
        foreach (var name in ReferenceFormulation.Names)
        {
            yield return new object[] { name };
        }
    }

    [Theory]
    [MemberData(nameof(Formulations))]
    public void FirstThousandCycleZeroAttemptsMatchTheApprovedSnapshot(string formulation)
    {
        var accelerator = _host.Accelerator;
        var setup = ReferenceFormulation.Prepare(accelerator, formulation, out var tables, out _);

        using var boundsBuffer = accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = accelerator.Allocate1D(tables.Cumulative);
        using var formingBuffer = accelerator.Allocate1D(tables.PocketForming);
        var fractions = new FractionTable
        {
            Bounds = boundsBuffer.View.BaseView,
            Cumulative = cumulativeBuffer.View.BaseView,
            PocketForming = formingBuffer.View.BaseView,
        };

        var cycle = ConstructedModel.CycleZero();

        using var integerTotals = accelerator.Allocate1D<long>(setup.Layout.IntegerLength);
        integerTotals.MemSetToZero();
        using var record = accelerator.Allocate1D<double>(setup.Layout.RecordLength);
        record.MemSetToZero();
        using var scratch = accelerator.Allocate1D<double>(setup.Layout.ScratchLength);
        scratch.MemSetToZero();

        var streams = OriginalSeeds.Streams;

        const int fieldsPerStream = 2;
        const int streamCount = 6;
        var perAttempt = 1 + streamCount * fieldsPerStream + setup.Layout.IntegerLength + setup.Layout.RecordLength;
        var trace = new double[perAttempt * AttemptCount];
        var offset = 0;

        for (var attempt = 0; attempt < AttemptCount; attempt++)
        {
            var outcome = Attempt.Run(
                setup, fractions, cycle, ref streams,
                integerTotals.View.BaseView, record.View.BaseView, scratch.View.BaseView);

            trace[offset++] = (byte)outcome;

            AppendStream(trace, ref offset, streams.S1);
            AppendStream(trace, ref offset, streams.S2);
            AppendStream(trace, ref offset, streams.S3);
            AppendStream(trace, ref offset, streams.S4);
            AppendStream(trace, ref offset, streams.S5);
            AppendStream(trace, ref offset, streams.S6);

            foreach (var total in integerTotals.GetAsArray1D())
            {
                trace[offset++] = BitConverter.Int64BitsToDouble(total);
            }

            var recordSnapshot = record.GetAsArray1D();
            Array.Copy(recordSnapshot, 0, trace, offset, recordSnapshot.Length);
            offset += recordSnapshot.Length;

            if (outcome == AttemptOutcome.Accepted)
            {
                record.MemSetToZero();
            }
        }

        BitSnapshot.Verify($"{formulation}.attempts", "cycle0_first_1000_attempts", trace);
    }

    private static void AppendStream(double[] trace, ref int offset, Mcg128State state)
    {
        trace[offset++] = BitConverter.UInt64BitsToDouble(state.Low);
        trace[offset++] = BitConverter.UInt64BitsToDouble(state.High);
    }
}
