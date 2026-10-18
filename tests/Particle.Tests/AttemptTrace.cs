using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using PropStruct.Random;

namespace PropStruct.Particle.Tests;

/// <summary>
/// What one attempt of <see cref="CycleOneRun"/> left: its outcome, the six streams, a copy
/// of every integer total and a copy of the particle's record, the row
/// <c>SnapshotTests</c> hashes for cycle 0.
/// </summary>
internal sealed record AttemptTrace(AttemptOutcome Outcome, StreamSet Streams, long[] IntegerTotals, double[] Record)
{
    /// <summary>The SHA-256 of the row: the outcome byte, the six streams' limbs, the totals, the record.</summary>
    public byte[] Digest()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData([(byte)Outcome]);
        AppendStream(hash, Streams.S1);
        AppendStream(hash, Streams.S2);
        AppendStream(hash, Streams.S3);
        AppendStream(hash, Streams.S4);
        AppendStream(hash, Streams.S5);
        AppendStream(hash, Streams.S6);
        hash.AppendData(MemoryMarshal.AsBytes(IntegerTotals.AsSpan()));
        hash.AppendData(MemoryMarshal.AsBytes(Record.AsSpan()));
        return hash.GetHashAndReset();
    }

    private static void AppendStream(IncrementalHash hash, Mcg128State state)
    {
        Span<byte> limbs = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(limbs, state.Low);
        BinaryPrimitives.WriteUInt64LittleEndian(limbs[8..], state.High);
        hash.AppendData(limbs);
    }
}
