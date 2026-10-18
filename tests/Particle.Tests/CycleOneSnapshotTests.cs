using System.Buffers.Binary;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// The guard of the <c>Corrected</c> work (<c>ACCEPTANCE.md</c>, "<c>None</c> reproduces the
/// original bit for bit beyond cycle 0"): the first 10,000 cycle-1 attempts of each
/// reference formulation under both precision kinds, run through <see cref="CycleOneRun"/>
/// (the sequence of <see cref="AccumulatorSweep"/>) with <c>ModelSetup.Corrections</c> unset,
/// one case per formulation and kind. Each
/// attempt contributes the four <c>double</c>s that carry the SHA-256 of its trace row
/// (<see cref="AttemptTrace.Digest"/>: outcome, the six streams' limbs, every integer total,
/// the particle's record, the row <see cref="SnapshotTests"/> hashes for cycle 0), so a bit
/// moved in any of them moves the approved hash. Recorded on the commit before the first
/// <c>Corrected</c> statement and never re-approved after a refactoring (<c>BOOT.md</c>,
/// "Taboos").
/// </summary>
public class CycleOneSnapshotTests : IClassFixture<CpuHost>
{
    /// <summary>The scope of the criterion's "first 10⁴ cycle-1 attempts".</summary>
    public const int AttemptCount = 10_000;

    private const int DoublesPerDigest = 4;

    private readonly CpuHost _host;

    public CycleOneSnapshotTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> FormulationsAndKinds()
    {
        foreach (var name in ReferenceFormulation.Names)
        {
            foreach (var kind in new[] { PrecisionKind.Binary64, PrecisionKind.Original })
            {
                yield return new object[] { name, kind.ToString() };
            }
        }
    }

    [Theory]
    [MemberData(nameof(FormulationsAndKinds))]
    public void FirstTenThousandCycleOneAttemptsMatchTheApprovedSnapshot(string formulation, string kindName)
    {
        var kind = Enum.Parse<PrecisionKind>(kindName);
        using var run = CycleOneRun.TakeToCycleOne(_host.Accelerator, formulation, kind);

        var layout = run.Setup.Layout;
        var trace = new double[AttemptCount * DoublesPerDigest];
        var accepted = 0;
        var restartedAfterLoop = 0;
        var fmkarm2Seen = false;
        var vmkmTotal2Seen = false;
        AttemptTrace? last = null;
        for (var attempt = 0; attempt < AttemptCount; attempt++)
        {
            last = run.Step();
            accepted += last.Outcome == AttemptOutcome.Accepted ? 1 : 0;
            restartedAfterLoop += last.Outcome == AttemptOutcome.RestartedAfterLoop ? 1 : 0;
            fmkarm2Seen |= Sum(last.Record, layout.Fmkarm2, run.Setup.Nkarm) != 0.0;
            vmkmTotal2Seen |= last.Record[layout.VmkmTotal2] != 0.0;

            var digest = last.Digest();
            for (var part = 0; part < DoublesPerDigest; part++)
            {
                var bits = BinaryPrimitives.ReadUInt64LittleEndian(digest.AsSpan(part * sizeof(ulong)));
                trace[attempt * DoublesPerDigest + part] = BitConverter.UInt64BitsToDouble(bits);
            }
        }

        // The scope of the guard: a snapshot of attempts that never reach the window, a
        // Var#1 pocket, a bridge counted as Var#2 or the tests of lines 723-738 guards
        // none of them (the cycle-0 snapshot's own "narrower than it reads").
        var totals = last!.IntegerTotals;
        Assert.True(accepted > 0, $"{formulation} {kind}: no attempt of cycle 1 was accepted.");
        Assert.True(restartedAfterLoop > 0, $"{formulation} {kind}: no attempt was restarted by lines 723-738.");
        Assert.True(totals[layout.Nfq] > 0, $"{formulation} {kind}: no bridge drew from the window.");
        Assert.True(totals[layout.Nfw] > 0, $"{formulation} {kind}: no Var#1 or Var#2 draw was made.");
        Assert.True(Sum(totals, layout.FqkarmCor, run.Setup.Nkarm) > 0, $"{formulation} {kind}: no Var#1 pocket was counted.");
        Assert.True(Sum(totals, layout.Qmkm2, run.Setup.Nc) > 0, $"{formulation} {kind}: no bridge was binned.");
        Assert.True(fmkarm2Seen, $"{formulation} {kind}: no Var#2 pocket reached Fmkarm2.");
        Assert.True(vmkmTotal2Seen, $"{formulation} {kind}: no Var#2 bridge was committed.");

        BitSnapshot.Verify($"{formulation}.cycle1", $"cycle1_first_10000_attempts_{kind}", trace);
    }

    private static double Sum(double[] values, int offset, int length)
    {
        var sum = 0.0;
        for (var i = 0; i < length; i++)
        {
            sum += values[offset + i];
        }

        return sum;
    }

    private static long Sum(long[] totals, int offset, int length)
    {
        var sum = 0L;
        for (var i = 0; i < length; i++)
        {
            sum += totals[offset + i];
        }

        return sum;
    }
}
