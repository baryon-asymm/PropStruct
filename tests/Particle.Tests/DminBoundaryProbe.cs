using ILGPU.Runtime;
using PropStruct.Random;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Measures the mechanism root BOOT.md's 2026-09-23 note names as unestablished: the
/// original prints a nonzero rate of "Dok &lt; Dmin" breakings (<c>conditions(2)</c>,
/// Fortran lines 478-480/533-536, <c>ResultsMWriter</c>'s <c>conditions[1] / (FI + N)</c>)
/// on HMX and HPEPA3, where the port prints exactly 0 under either <see
/// cref="PrecisionKind"/>. <c>Dr</c>/<c>Db</c> (the drawn base/neighbour sizes) and
/// <c>Dmin</c> have no declaration in the Fortran source's declaration block (lines
/// 4-64), so both are REAL*4 by Fortran's own implicit-typing default (I-N integer,
/// otherwise real single precision; the source declares no <c>IMPLICIT</c> statement
/// anywhere -- checked here the same way <c>classify-real4-accumulators.py</c> already
/// checks it for the accumulator fields). The comparison at 478/533 is therefore a
/// REAL*4-vs-REAL*4 test in the original.
///
/// <c>Dr</c>/<c>Db</c> also pass through a REAL*4 <em>store</em> before that comparison,
/// not just a REAL*4 declaration: subroutine <c>SIZE</c> (lines 1761-1776) declares
/// <c>D</c> (its output, the caller's <c>Dr</c>/<c>Db</c>) with no type either, so it too
/// is implicit REAL*4, while its own local <c>X1</c> is explicitly <c>real*8</c>. Fortran
/// evaluates the assignment's right-hand side in double precision because of that
/// <c>real*8</c> operand -- <c>D=X1*(DOK(2*MINV)-DOK(2*MINV-1))+DOK(2*MINV-1)</c>
/// (<c>JZ.EQ.2</c>, line 1772) or <c>D=1./SQRT(1/DOK(2*MINV-1)**2-X1*(...))</c> (line
/// 1769) -- and only the final store into <c>D</c> rounds to binary32. The port's
/// <see cref="SizeLaw.Sample"/> keeps its own <c>diameter</c> output <c>double</c>
/// throughout (root BOOT.md's own decision, "attempt plane stays double under either
/// kind"), so this store, and the boundary case it can create, is not reproduced.
///
/// <b>Hypothesis probed.</b> A draw whose true (double) size sits a hair above
/// <c>Dmin</c> can round <em>down</em> to exactly <c>Dmin</c>'s own binary32 value when
/// both are stored as REAL*4, making <c>Dr.le.Dmin</c>/<c>Db.le.Dmin</c> fire in the
/// original where the port's <c>double</c> comparison (strictly above) does not. This
/// class counts exactly that event -- <c>binary32(size) &lt;= binary32(Dmin)</c> holding
/// while the <c>double</c> test does not -- over a real reference-mode replay, without
/// touching <see cref="Attempt.Run"/> or any production code (root BOOT.md's taboo, "no
/// second implementation"; this class only reads the same draws <see cref="Attempt.Run"/>
/// itself already made, the same technique <see cref="AccumulatorSweep"/> uses for
/// <c>Allvdok</c>/<c>Vdokstr</c>).
///
/// <b>Replay technique.</b> Around every real <see cref="Attempt.Run"/> call, this class
/// keeps a copy of the streams taken before it (the real call already advanced the live
/// streams correctly) and, on that copy only, redraws the base size from streams 1/2 and
/// every neighbour size from streams 4/5 through the published <see cref="Mcg128.Next"/>
/// and <see cref="SizeLaw.Sample"/> -- the same functions <see cref="Attempt.Run"/> itself
/// calls, never a copy of their arithmetic. The number of neighbours to replay is the
/// attempt's own <c>NFZ</c> delta, exactly as <see cref="AccumulatorSweep"/> reads it.
/// A replayed draw is only tested against <c>Dmin</c> when the original's own control
/// flow would reach that test: <c>SFR(Nfract) != 0</c> first (line 473 for the base draw,
/// line 528 for the neighbour draw -- both already-public inputs of <see
/// cref="Attempt.Run"/>, <see cref="FractionTable.PocketForming"/>, not a decision this
/// class invents), matching <c>Attempt.cs</c>'s own gate before its own "(478)"/"(533)"
/// comparisons verbatim.
///
/// <b>Cycle 0 only, deliberately.</b> This class runs every attempt under <see
/// cref="ConstructedModel.CycleZero"/>: the Dmin/Dmax control flow above is unconditional
/// on the cycle flag (it sits before any pocket/bridge or acceptance-test code, "##
/// Attempt structure"), and cycle 0 accepts every attempt that completes the neighbour
/// loop, needing no <c>QKS1</c>/<c>pdoksmall</c> history built up first -- a run-scale
/// convenience for a measurement, not a claim about a real cycle 1's own acceptance rate,
/// the same simplification <see cref="AccumulatorSweep"/>'s own cycle0Attempts phase and
/// <c>RealFourAccumulationErrorTests</c> already rely on. The original's own printed rate
/// normalises by <c>FI + N</c>, the total accepted base particles of the whole run
/// (warm-up cycle 0's own N plus every production cycle's N, "Execution model"), so
/// dividing this class's own hit count by its own accepted-particle count is the same
/// kind of per-accepted-particle rate, at this class's own (smaller) sample scale.
/// </summary>
internal static class DminBoundaryProbe
{
    /// <summary>
    /// The exact <c>x1</c> value at which a draw already known to select fraction
    /// <paramref name="fractionIndex"/> (<paramref name="xInFraction"/> is an <c>x</c>
    /// forcing that selection, checked at both ends of the search) stops rounding down to
    /// <c>Dmin</c>'s own binary32 value -- found by bisection on <see
    /// cref="SizeLaw.Sample"/> itself, never a derived closed form. <c>D(x1)</c> is
    /// monotone in <c>x1</c> for both size laws (linear for <c>sizeLaw == 2</c>; for the
    /// reciprocal-square law, <c>dD/dx1 = (Delta/2)&#183;D&#179; &gt; 0</c> since
    /// <c>Delta = 1/lower&#178; - 1/upper&#178; &gt; 0</c>), so a single crossing point
    /// exists whenever the fraction's lower bound hits but its upper end does not;
    /// <paramref name="monotonic"/> reports whether that shape held (it does not, and the
    /// returned value is degenerate 0 or 1, when the fraction's own bound sits far enough
    /// from <c>Dmin</c> that neither end is near the boundary -- <c>PSAN02n</c> and
    /// <c>inpt</c>, whose lowest fraction bound is 160/257 &#181;m, not 10 &#181;m).
    /// Since <c>x1</c> is itself uniform on [0, 1), the returned threshold <em>is</em>
    /// <c>P(hit | fraction == fractionIndex)</c> exactly, not an estimate: no Monte Carlo
    /// sampling of <c>x1</c> is needed once the crossing point is known to double
    /// precision.
    /// </summary>
    public static double FindHitThreshold(
        ModelSetup setup, FractionTable fractions, int fractionIndex, double xInFraction, out bool monotonic)
    {
        var dminFloat = (double)(float)setup.Dmin;

        SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
            xInFraction, 0.0, out var dAtZero, out var fractionAtZero);
        SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
            xInFraction, 1.0 - 1e-12, out var dAtOne, out var fractionAtOne);

        if (fractionAtZero != fractionIndex || fractionAtOne != fractionIndex)
        {
            throw new ArgumentException(
                $"xInFraction={xInFraction:G17} does not select fraction {fractionIndex} across the whole x1 range " +
                $"(got {fractionAtZero} at x1=0, {fractionAtOne} at x1=~1).", nameof(xInFraction));
        }

        var hitAtZero = (double)(float)dAtZero <= dminFloat;
        var hitAtOne = (double)(float)dAtOne <= dminFloat;

        monotonic = hitAtZero && !hitAtOne;
        if (!monotonic)
        {
            return hitAtZero ? 1.0 : 0.0;
        }

        var lo = 0.0;
        var hi = 1.0;
        for (var iter = 0; iter < 200; iter++)
        {
            var mid = (lo + hi) / 2.0;
            if (mid == lo || mid == hi)
            {
                break; // adjacent representable doubles: as tight as bisection can get.
            }

            SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
                xInFraction, mid, out var d, out _);
            if ((double)(float)d <= dminFloat)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return hi; // smallest sampled x1 that no longer hits.
    }

    internal sealed record Result(
        string Formulation,
        ulong Seed,
        int Attempts,
        long AcceptedParticles,
        long BaseTested,
        long BaseProbeHits,
        long BaseDoubleHits,
        long NeighbourTested,
        long NeighbourProbeHits,
        long NeighbourDoubleHits)
    {
        /// <summary>The combined count the original's single <c>conditions(2)</c> accumulator would hold: base and neighbour firings share one cell (lines 479, 534).</summary>
        public long TotalProbeHits => BaseProbeHits + NeighbourProbeHits;

        /// <summary>Should always be 0: the port's own <c>double</c> comparison (Attempt.cs, "(478)"/"(533)") is never expected to fire.</summary>
        public long TotalDoubleHits => BaseDoubleHits + NeighbourDoubleHits;

        /// <summary>The same normalisation the original prints, <c>conditions(2) / (FI + N)</c>, at this class's own sample scale.</summary>
        public double ProbeRate => AcceptedParticles == 0 ? double.NaN : (double)TotalProbeHits / AcceptedParticles;
    }

    /// <summary>
    /// Runs <paramref name="attempts"/> reference-mode attempts of <paramref
    /// name="formulationName"/> starting from <paramref name="seed"/>'s own lagged-replica
    /// streams (<see cref="OriginalSeeds.ForParticle"/> with ordinal 0: all six streams
    /// jumped ahead by the same power of the multiplier, the original's own lagged-replica
    /// recipe, root BOOT.md "Two stream layouts"; <paramref name="seed"/> = 0 is the
    /// original's own reference streams).
    /// </summary>
    public static Result Run(Accelerator accelerator, string formulationName, ulong seed, int attempts)
    {
        var setup = ReferenceFormulation.Prepare(accelerator, formulationName, out var tables, out _);
        var layout = setup.Layout;

        using var boundsBuffer = accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = accelerator.Allocate1D(tables.Cumulative);
        using var formingBuffer = accelerator.Allocate1D(tables.PocketForming);
        var fractions = new FractionTable
        {
            Bounds = boundsBuffer.View.BaseView,
            Cumulative = cumulativeBuffer.View.BaseView,
            PocketForming = formingBuffer.View.BaseView,
        };

        using var integerTotals = accelerator.Allocate1D<long>(layout.IntegerLength);
        integerTotals.MemSetToZero();
        using var record = accelerator.Allocate1D<double>(layout.RecordLength);
        record.MemSetToZero();
        using var scratch = accelerator.Allocate1D<double>(layout.ScratchLength);
        scratch.MemSetToZero();

        // The nearest representable binary32 of Dmin, cast exactly as the original's own
        // implicit-REAL*4 Dmin variable would hold it -- not a tolerance, a store.
        var dminFloat = (double)(float)setup.Dmin;

        var streams = OriginalSeeds.ForParticle(seed, 0);
        var cycle = ConstructedModel.CycleZero();

        long accepted = 0;
        long baseTested = 0, baseProbeHits = 0, baseDoubleHits = 0;
        long neighbourTested = 0, neighbourProbeHits = 0, neighbourDoubleHits = 0;

        var integerBefore = integerTotals.GetAsArray1D();

        for (var i = 0; i < attempts; i++)
        {
            var streamsBeforeAttempt = streams; // copy: the replay below runs on this copy only.

            var outcome = Attempt.Run(
                setup, fractions, cycle, ref streams,
                integerTotals.View.BaseView, record.View.BaseView, scratch.View.BaseView);

            var integerAfter = integerTotals.GetAsArray1D();

            // -- base draw: streams 1/2 (Attempt.cs, "base (448-484)"). --
            var replay = streamsBeforeAttempt;
            var xBase = Mcg128.Next(ref replay.S1);
            var x0Base = Mcg128.Next(ref replay.S2);
            SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
                xBase, x0Base, out var drReplay, out var fractionBase);

            if (fractions.PocketForming[fractionBase] != 0) // SFR(Nfract) != 0 (473): only then is (478) reached.
            {
                baseTested++;
                var doubleHit = drReplay <= setup.Dmin;
                if (doubleHit)
                {
                    baseDoubleHits++;
                }
                else if ((double)(float)drReplay <= dminFloat)
                {
                    baseProbeHits++;
                }
            }

            // -- every neighbour draw of this attempt: streams 4/5 (Attempt.cs, "345 (503-543)"). --
            var neighbourDraws = integerAfter[layout.Nfz] - integerBefore[layout.Nfz];
            for (var n = 0L; n < neighbourDraws; n++)
            {
                var x2 = Mcg128.Next(ref replay.S4);
                var x21 = Mcg128.Next(ref replay.S5);
                SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
                    x2, x21, out var dbReplay, out var fractionNeighbour);

                if (fractions.PocketForming[fractionNeighbour] == 0) // SFR(Nfract) == 0 (528): (533) never reached.
                {
                    continue;
                }

                neighbourTested++;
                var doubleHit = dbReplay <= setup.Dmin;
                if (doubleHit)
                {
                    neighbourDoubleHits++;
                }
                else if ((double)(float)dbReplay <= dminFloat)
                {
                    neighbourProbeHits++;
                }
            }

            integerBefore = integerAfter;
            if (outcome == AttemptOutcome.Accepted)
            {
                accepted++;
            }
        }

        return new Result(
            formulationName, seed, attempts, accepted,
            baseTested, baseProbeHits, baseDoubleHits,
            neighbourTested, neighbourProbeHits, neighbourDoubleHits);
    }
}
