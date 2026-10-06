using System.Runtime.InteropServices;
using ILGPU;
using PropStruct.Particle;
using PropStruct.Random;

namespace PropStruct.Execution;

/// <summary>
/// One particle's streams, attempt count and last outcome, packed together and padded to two 64-byte cache
/// lines (BOOT.md, "Host-thread path": "per-particle records and scratch are laid out so that neighbouring
/// particles do not share a cache line"). Before this type, <c>RunAttempts</c> wrote three separate arrays
/// (<c>streams</c>: 96 bytes/particle, <c>attemptCounts</c>: 8, <c>outcomes</c>: 1) once per particle per
/// launch — the last two packed 8 and 64 particles per cache line respectively, so on the host-thread path,
/// where these writes happen concurrently from real OS threads, two unrelated particles finishing at nearly
/// the same time on different threads would invalidate each other's cache line for no reason the model
/// requires. <see cref="StreamSet"/> alone is already 96 bytes (more than one cache line), so folding it in
/// too, rather than leaving it in its own array, needs no extra space to reach a two-line, false-sharing-free
/// stride (105 bytes of payload rounds up to 128 = 2×64 regardless). This is entirely <c>Execution</c>'s own
/// indexing choice — <c>Kernels.RunAttempts</c>'s parameter shapes are not part of any neighbour's contract —
/// and the same struct is used on CUDA too (BOOT.md, Taboos: "no second copy of any Particle or Random
/// function"): the array is 8× larger there for the same particle count, negligible next to the records and
/// scratch a batch already allocates, and CUDA has no OS-thread cache-coherency concept for this padding to
/// help or hurt.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 128)]
internal struct ParticleControl
{
    public StreamSet Streams;
    public long AttemptCount;
    public byte Outcome;
}

/// <summary>The kernel entry points: each launches one static method of <see cref="Particle"/> or
/// <see cref="Random"/> over its own view of the run-scoped buffers. No model logic lives here
/// (BOOT.md, Taboos: "it launches Particle, it does not decide").</summary>
internal static class Kernels
{
    /// <summary>Fills <paramref name="controls"/>[i]'s <see cref="ParticleControl.Streams"/> with the batched
    /// particle streams of ordinal <paramref name="firstOrdinal"/> + i, <see cref="StreamSeeds.ForBatchedParticle"/>
    /// (BOOT.md, "A batch"; ⚠ 2026-09-19: switched from <see cref="StreamSeeds.ForParticle"/>, root BOOT.md
    /// "Execution model"). The caller has already zeroed <paramref name="controls"/>
    /// (<see cref="ParticleControl.AttemptCount"/>/<see cref="ParticleControl.Outcome"/> start at their zero
    /// values, exactly as the three separate buffers this type replaces used to).</summary>
    internal static void DeriveStreams(Index1D index, StreamLayout layout, ulong seed, ulong firstOrdinal, ArrayView<ParticleControl> controls)
    {
        var control = controls[index];
        control.Streams = StreamSeeds.ForBatchedParticle(layout, seed, firstOrdinal + (ulong)index.X);
        controls[index] = control;
    }

    /// <summary>
    /// True for the outcomes that end an attempt's batch immediately with their own
    /// <see cref="BatchStatus"/> (<see cref="Engine.ToFailureStatus"/>): a precondition violation, not a
    /// model branch (<c>Particle/BOOT.md</c>, "Attempt structure"). Kernel-compatible (an enum comparison,
    /// no allocation) so <see cref="RunAttempts"/> can stop its own in-launch loop on exactly the same test
    /// <see cref="Engine.ToFailureStatus"/> uses on the host, rather than each keeping its own list of the
    /// same three outcomes and risking one gaining a fourth without the other (review item 5,
    /// 2026-09-18).
    /// </summary>
    internal static bool IsFailure(AttemptOutcome outcome) =>
        outcome is AttemptOutcome.NeighbourBudgetExceeded
        or AttemptOutcome.BridgeDrawBudgetExceeded
        or AttemptOutcome.IndexOutOfRange;

    /// <summary>
    /// Runs up to <paramref name="attemptsPerLaunch"/> attempts of <see cref="Attempt.Run"/> for the
    /// particle at <paramref name="activeIndices"/>[<paramref name="activeIndex"/>], stopping earlier on
    /// <see cref="AttemptOutcome.Accepted"/>, a failure outcome, or the particle's own attempt cap
    /// (BOOT.md, "A batch"; "A continued batch"). <paramref name="scratchStride"/> is the number of
    /// <see langword="double"/>s between one particle's scratch region and the next in
    /// <paramref name="scratches"/> — <paramref name="setup"/>'s own <c>ScratchLength</c> on every launch on
    /// an ILGPU-compiled accelerator (CUDA, or the CPU accelerator forced for the test oracle: tight packing
    /// is what GPU memory coalescing wants, and there is no OS-thread cache line to share), and
    /// <c>ScratchLength</c> rounded up to a multiple of 8 (one 64-byte cache line of doubles) on the
    /// host-thread path, so that two particles' scratch regions never end up in the same cache line while
    /// their own OS threads write to them concurrently (BOOT.md, "Host-thread path"). <c>records</c> keeps
    /// its plain <c>RecordLength</c> stride on both paths: <c>Particle.Fold.AddField</c> reads it at
    /// <c>particle * recordLength + field</c> unconditionally (<c>Particle/API.md</c>, "Fold by field"), so
    /// padding it here would misalign every fold read against what this kernel wrote.
    /// </summary>
    internal static void RunAttempts(
        Index1D activeIndex,
        ModelSetup setup, FractionTable fractions, CycleInputs cycle,
        int attemptsPerLaunch, long maxAttemptsPerParticle, int scratchStride,
        ArrayView<int> activeIndices,
        ArrayView<ParticleControl> controls,
        ArrayView<long> integerTotals,
        ArrayView<double> records,
        ArrayView<double> scratches)
    {
        var particleIndex = activeIndices[activeIndex];
        var control = controls[particleIndex];
        var streamSet = control.Streams;
        var attemptCount = control.AttemptCount;
        var recordLength = setup.Layout.RecordLength;
        var scratchLength = setup.Layout.ScratchLength;
        var record = records.SubView((long)particleIndex * recordLength, recordLength);
        var scratch = scratches.SubView((long)particleIndex * scratchStride, scratchLength);

        // Sentinel: unchanged only if attemptCount already reached the cap before this launch even
        // started (the host does not relaunch such a particle, so this cannot fire in practice).
        var outcome = AttemptOutcome.RestartedInsideLoop;
        for (var i = 0; i < attemptsPerLaunch; i++)
        {
            if (attemptCount >= maxAttemptsPerParticle)
            {
                break;
            }

            attemptCount++;
            outcome = Attempt.Run(in setup, in fractions, in cycle, ref streamSet, integerTotals, record, scratch);
            if (outcome == AttemptOutcome.Accepted || IsFailure(outcome))
            {
                break;
            }
        }

        control.Streams = streamSet;
        control.AttemptCount = attemptCount;
        control.Outcome = (byte)outcome;
        controls[particleIndex] = control;
    }

    /// <summary>Runs <see cref="PocketHistogram.Normalize"/> once, as a single-thread kernel so QKS1 is
    /// computed by the same code on every accelerator (BOOT.md, "QKS1 is the engine's").</summary>
    internal static void NormalizeQks1(Index1D _, AccumulatorLayout layout, ArrayView<long> integerTotals, ArrayView<double> qks1) => PocketHistogram.Normalize(in layout, integerTotals, qks1);

    /// <summary>One thread per record field, each calling <see cref="Fold.AddField"/>
    /// (BOOT.md, "Fold in particle order, per field in parallel").</summary>
    internal static void FoldField(Index1D field, AccumulatorLayout layout, ArrayView<double> records, int particleCount, ArrayView<double> realTotals) => Fold.AddField(in layout, records, particleCount, field, realTotals);

    /// <summary>
    /// The probe of the particle program's own math list (<see cref="MathProbe"/>):
    /// <c>outputs[input * MathProbe.StrideCount + function]</c>.
    /// </summary>
    internal static void ProbeMath(Index1D index, ArrayView<double> inputs, ArrayView<double> outputs)
    {
        var v = inputs[index];
        var o = index.X * MathProbe.StrideCount;
        outputs[o] = Math.Log(v);
        outputs[o + 1] = Math.Sqrt(v);
        outputs[o + 2] = Math.Pow(v, MathProbe.PowExponent);
        outputs[o + 3] = Math.Acos(v - 1.0);
        outputs[o + 4] = Math.Sin(v);
        outputs[o + 5] = Math.Tan(v);
        outputs[o + 6] = Math.Abs(v - 0.5);
        outputs[o + 7] = Math.Max(v, 0.5);
        outputs[o + 8] = Math.Min(v, 0.5);
        outputs[o + 9] = Math.Floor(v * 10.0);
    }
}
