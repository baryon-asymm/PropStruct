using ILGPU.Runtime;
using PropStruct.Random;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Measures the original's REAL*4 accumulation error for the eight accumulator
/// families <c>src/Particle/BOOT.md</c>'s "## Defects of the original" names: the
/// three committed fields (<c>VdokTotal</c>, <c>VdokTotal2</c>, <c>Fmkarm2</c>, each
/// written at most once per attempt) and the five fields the original writes
/// <em>per drawn particle</em>, not per attempt (<c>Allvdok</c>, <c>Vdokstr</c>,
/// <c>FmkarmCor</c>, <c>Dokp41</c>, <c>Dokp31</c>).
///
/// Runs a real reference-mode sample of a real formulation's own setup (cycle 0
/// warm-up with continued streams, then cycle 1 with the same streams continued
/// further) through <see cref="Attempt.Run"/> exactly as it already exists — nothing
/// here is a second implementation of any part of the attempt (root BOOT.md,
/// Taboos). Around every single <see cref="Attempt.Run"/> call this class feeds its
/// binary32 shadow at the original's own granularity, one rounding per drawn
/// particle, by three routes, none of which re-derives a decision <see
/// cref="Attempt.Run"/> makes:
///
/// <list type="bullet">
/// <item><b><c>Allvdok</c>, <c>Vdokstr</c> — replayed.</b> Around the real call, this
/// class keeps a copy of the streams taken before it and, on that copy only (the real
/// streams already advanced correctly by the real call), draws the base particle
/// from streams 1/2 and every neighbour from streams 4/5 — exactly the stream roles
/// BOOT.md's own "Draws in the original order from the original streams" names —
/// through the published <see cref="Mcg128.Next"/> and <see cref="SizeLaw.Sample"/>,
/// the same functions <see cref="Attempt.Run"/> itself calls (the precedent for
/// calling them from a test driver instead of copying their arithmetic is
/// <c>ConstructedModel.PeekFirstNeighbour</c>'s own class remarks). The number of
/// neighbours to replay is the attempt's own <c>NFZ</c> delta (BOOT.md, "Integer
/// totals": incremented once per neighbour draw, unconditionally, Fortran line 518)
/// — "their number from the printed-count delta", not a count this class derives
/// from any branch of its own. <c>Allvdok</c>'s neighbour term (line 524) fires for
/// every one of those draws unconditionally, so every replayed neighbour contributes
/// one term. <c>Vdokstr</c>'s term (line 542) fires only when the neighbour's own
/// fraction forms pockets (line 528) and its diameter clears <c>Dmin</c> (line 533)
/// — two single-line gates already visible verbatim in <see cref="Attempt.Run"/>,
/// evaluated here only on data the replay itself just produced (the drawn fraction
/// and diameter) and on <see cref="ModelSetup.Dmin"/> and
/// <see cref="FractionTable.PocketForming"/>, both already public inputs of
/// <see cref="Attempt.Run"/> — not a rule this class invents, and not a geometry or
/// gap-coefficient decision of the kind the taboo protects. Every replayed term is
/// verified, not trusted: <see cref="SweepResult.ReplayChecks"/> keeps a persistent
/// shadow double accumulator per cell, fed the exact same terms in the exact same
/// order as <c>record</c>'s own cell and reset in lockstep with it, and compares the
/// two directly (never by subtracting a possibly-long-accumulated <c>record</c> value
/// from itself, which does not always recover a small increment bit for bit once the
/// accumulated value dwarfs it — a double-precision instance of the very cancellation
/// this class exists to measure at REAL*4 scale, just far smaller). Two independently
/// computed running totals built from the identical sequence of IEEE754 additions in
/// the identical order are bit-identical whenever the replayed terms are the
/// original's own, for every attempt.</item>
/// <item><b><c>FmkarmCor</c>, <c>Dokp41</c>, <c>Dokp31</c> — read from their own
/// atomic companion.</b> <see cref="Attempt.Run"/> writes each of these together
/// with an integer counter in the same branch, unconditionally, with no test between
/// the two (<c>FmkarmCor</c> with <c>FqkarmCor</c>, lines 685-686; <c>Dokp41</c>/
/// <c>Dokp31</c> with one cell of <c>Qdoks</c>'s own row, lines 707-708/711 — visible
/// verbatim in <c>Attempt.cs</c>). The counter's own before/after delta is
/// therefore the exact number of times the original added to that cell in this one
/// attempt, with no filter of this class's own devising. When that count is exactly
/// one, the attempt's own recorded double delta for the cell <em>is</em> that one
/// term, so this class feeds it as one addition. When the count is more than one —
/// the same cell hit by two or more drawn particles inside one attempt — recovering
/// the individual terms needs the gap-coefficient/bridge-vs-pocket decision the
/// taboo forbids re-deriving, so this class feeds the attempt's own combined delta as
/// one lump addition instead and records the shortfall
/// (<see cref="FieldSample.LumpedAttempts"/>, <see
/// cref="FieldSample.LumpedTermsInLumps"/>) rather than hide it.</item>
/// <item><b><c>VdokTotal</c>, <c>VdokTotal2</c>, <c>Fmkarm2</c> — already exact.</b>
/// Each is written at most once per attempt (BOOT.md, "Accumulators": committed or
/// loop-end), so the attempt's own recorded double delta already <em>is</em> the one
/// true term, with no replay needed.</item>
/// </list>
///
/// Every field's <see cref="FieldSample.DoubleTotal"/> is, as before, the exact
/// running double total: every term this node sums is a non-negative physical
/// quantity (a sphere or bridge volume, a size raised to a power), so it equals
/// &#931;|term| exactly at any grouping (BOOT.md's own reasoning, unchanged).
/// <see cref="FieldSample.TrueTermCount"/> is now the original's own true
/// per-drawn-particle addition count, not an attempt count: this is the fix for the
/// error this class's own earlier remarks named honestly and the documents built on
/// anyway (root BOOT.md, 2026-09-21, "was closed on a lower bound read as a
/// measurement"). This class no longer produces a lower bound for any of the eight
/// fields: <c>Allvdok</c> and <c>Vdokstr</c> are exact by verified replay,
/// <c>FmkarmCor</c>/<c>Dokp41</c>/<c>Dokp31</c> are exact wherever
/// <see cref="FieldSample.LumpedAttempts"/> is zero for a cell and honestly flagged
/// where it is not, and the three committed fields were already exact.
/// </summary>
internal static class AccumulatorSweep
{
    // Fortran literal 3.14159 (sphere volume pi/6*D^3), lines 469, 524, 542, matching the
    // same constant Attempt.cs names SpherePi (BOOT.md, Constraints). Repeating the literal
    // here is the same kind of repetition ConstructedModel.PeekFirstNeighbour's own remarks
    // already justify for the inline KCIL/rrL arithmetic: the value, not a decision.
    private const double SpherePi = 3.14159;

    /// <summary>One of the eight target fields, sampled over the whole sweep.</summary>
    internal sealed class FieldSample
    {
        public required string Name { get; init; }
        public required int Offset { get; init; }
        public required int Length { get; init; }

        /// <summary>Per cell: the exact running double total (&#931;|term|, see class remarks).</summary>
        public double[] DoubleTotal { get; init; } = null!;

        /// <summary>
        /// Per cell: the running float32 shadow, fed one addition per original
        /// accumulation-line firing wherever that is known exactly, and one lump addition
        /// per multi-term attempt otherwise (see class remarks).
        /// </summary>
        public float[] Float32Total { get; init; } = null!;

        /// <summary>
        /// Per cell: the original's own true addition count — Fortran line firings, not
        /// attempts — known exactly by replay (<c>Allvdok</c>, <c>Vdokstr</c>) or by the
        /// field's own atomic companion counter (<c>FmkarmCor</c>, <c>Dokp41</c>,
        /// <c>Dokp31</c>; the three committed fields never exceed one per attempt).
        /// </summary>
        public long[] TrueTermCount { get; init; } = null!;

        /// <summary>
        /// Per cell: how many attempts contributed two or more true terms to this cell in
        /// one <see cref="Attempt.Run"/> call and so were fed as one lump instead of their
        /// own individual additions (zero for <c>Allvdok</c>, <c>Vdokstr</c> and the three
        /// committed fields; see class remarks).
        /// </summary>
        public long[] LumpedAttempts { get; init; } = null!;

        /// <summary>
        /// Per cell: how many of <see cref="TrueTermCount"/>'s own true terms are inside a
        /// lump above, i.e. were fed as part of a combined addition rather than their own.
        /// </summary>
        public long[] LumpedTermsInLumps { get; init; } = null!;

        /// <summary>
        /// Per cell: every addition this sweep actually fed the float32 shadow, in order —
        /// one entry per true term for <c>Allvdok</c>/<c>Vdokstr</c> and for a
        /// single-hit attempt of the other fields, one entry per lump otherwise. Used to
        /// extend a cell's own empirical term distribution by bootstrap resampling out to
        /// the reference's real scale (<see cref="RealFourAccumulationErrorTests"/>,
        /// "saturation").
        /// </summary>
        public List<double>[] Deltas { get; init; } = null!;
    }

    /// <summary>
    /// The result of verifying, for every attempt, that the terms this class replayed for
    /// <paramref name="Field"/> sum to the attempt's own recorded double delta bit for bit,
    /// cell for cell (class remarks, "replayed"). A verification failure means the replay
    /// is not the original's own term sequence and nothing built on it counts (the check
    /// the owning task asked this class to report): <paramref name="Mismatches"/> is the
    /// count of (attempt, cell) pairs where the replayed sum and the recorded delta
    /// disagreed, and <paramref name="FirstMismatch"/> describes the first one, naming the
    /// cell and both values, so a reader can tell at a glance whether a future change broke
    /// the replay's own correctness.
    /// </summary>
    internal sealed record ReplayCheck(string Field, long AttemptsChecked, long Mismatches, string? FirstMismatch);

    internal sealed record SweepResult(
        FieldSample[] Fields,
        ReplayCheck[] ReplayChecks,
        long Cycle0Attempts, long Cycle1Attempts, long AcceptedParticles,
        long Nfx, long Nfy, long Nfq, long Nfw);

    private static readonly (string Name, Func<AccumulatorLayout, int> Offset)[] TargetFields =
    {
        ("Allvdok", l => l.Allvdok),
        ("Vdokstr", l => l.Vdokstr),
        ("VdokTotal", l => l.VdokTotal),
        ("VdokTotal2", l => l.VdokTotal2),
        ("FmkarmCor", l => l.FmkarmCor),
        ("Fmkarm2", l => l.Fmkarm2),
        ("Dokp41", l => l.Dokp41),
        ("Dokp31", l => l.Dokp31),
    };

    private static int FieldLength(string name, ModelSetup setup) => name switch
    {
        "Allvdok" or "Vdokstr" or "VdokTotal" => setup.Ndok,
        "VdokTotal2" => 1,
        "FmkarmCor" or "Fmkarm2" => setup.Nkarm,
        "Dokp41" or "Dokp31" => setup.Ncat,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "unknown target field"),
    };

    private static void FeedTerm(FieldSample field, int cell, double term)
    {
        field.Float32Total[cell] += (float)term;
        field.TrueTermCount[cell] += 1;
        field.Deltas[cell].Add(term);
    }

    /// <summary>A field written at most once per attempt: the recorded delta already is the one true term.</summary>
    private static void FeedCommitted(FieldSample field, int offset, int length, double[] before, double[] after)
    {
        for (var c = 0; c < length; c++)
        {
            var delta = after[offset + c] - before[offset + c];
            field.DoubleTotal[c] += delta;
            if (delta != 0.0)
            {
                FeedTerm(field, c, delta);
            }
        }
    }

    /// <summary>
    /// A field with a one-cell atomic companion counter written in the same branch,
    /// unconditionally (<c>FmkarmCor</c>/<c>FqkarmCor</c>). <paramref name="companionDelta"/>
    /// is that counter's own before/after delta for cell <c>c</c>.
    /// </summary>
    private static void FeedFromCompanionCounter(
        FieldSample field, int offset, int length, double[] before, double[] after, Func<int, long> companionDelta)
    {
        for (var c = 0; c < length; c++)
        {
            var delta = after[offset + c] - before[offset + c];
            field.DoubleTotal[c] += delta;
            if (delta == 0.0)
            {
                continue;
            }

            var count = companionDelta(c);
            if (count <= 0)
            {
                throw new InvalidOperationException(
                    $"{field.Name}[{c}]: record delta {delta:G17} but its atomic companion counter's own " +
                    $"delta is {count} -- the two are no longer written together unconditionally in " +
                    "Attempt.Run, so this class's own assumption is wrong and nothing it reports counts.");
            }

            field.TrueTermCount[c] += count;
            field.Deltas[c].Add(delta); // one entry either way: the true term (count == 1) or the lump.
            field.Float32Total[c] += (float)delta;
            if (count > 1)
            {
                field.LumpedAttempts[c] += 1;
                field.LumpedTermsInLumps[c] += count;
            }
        }
    }

    /// <summary>
    /// A field whose atomic companion is one row of a larger table (<c>Dokp41</c>/<c>Dokp31</c>
    /// with the matching row of <c>Qdoks</c>, lines 707-708/711): the companion delta for cell
    /// <c>c</c> is the sum of <paramref name="rowWidth"/> cells starting at
    /// <c><paramref name="companionRowBase"/> + c * rowWidth</c>.
    /// </summary>
    private static void FeedFromCompanionRow(
        FieldSample field, int offset, int length, int rowWidth, int companionRowBase,
        double[] before, double[] after, long[] integerBefore, long[] integerAfter)
    {
        FeedFromCompanionCounter(field, offset, length, before, after, c =>
        {
            var rowStart = companionRowBase + c * rowWidth;
            long count = 0;
            for (var i = 0; i < rowWidth; i++)
            {
                count += integerAfter[rowStart + i] - integerBefore[rowStart + i];
            }

            return count;
        });
    }

    /// <summary>
    /// Runs the sweep for one reference formulation: <paramref name="cycle0Attempts"/>
    /// attempts of cycle 0 (builds a real Qks histogram from the formulation's own
    /// physics), then <paramref name="cycle1Attempts"/> attempts of cycle 1 with that
    /// histogram normalized into Qks1 (<see cref="PocketHistogram.Normalize"/>,
    /// refreshed after every attempt reference mode would refresh it after — root
    /// BOOT.md, "Reference mode is the original's sequence"). <c>pdoksmall</c> is left
    /// at all-zero and <c>Dmaxxx</c> at a permissive ceiling: real values need
    /// <c>Statistics.CycleStatistics.Compute</c>'s own end-of-cycle pipeline, which in
    /// turn needs the cycle loop that only <c>Simulation</c> owns (root BOOT.md,
    /// Decomposition) — out of this node's reach without escalation, and not needed
    /// for this sweep's own purpose, which is to observe a large, real sample of the
    /// same term distribution the original's own run draws from, not to reproduce one
    /// formulation's own results.m bit for bit. The permissive values bias which
    /// branch of the pocket/bridge "Var#1"/"Var#2" correction each attempt takes
    /// (documented, not hidden) toward exercising <c>FmkarmCor</c>/<c>Fmkarm2</c>/
    /// <c>VdokTotal</c>/<c>VdokTotal2</c> more often than a real cycle 1 would, which
    /// only enlarges this sweep's own sample of their term distribution.
    /// </summary>
    public static SweepResult Run(Accelerator accelerator, string formulationName, int cycle0Attempts, int cycle1Attempts)
    {
        var setup = ReferenceFormulation.Prepare(accelerator, formulationName, out var tables, out _);
        var layout = setup.Layout;
        var ndok = setup.Ndok;
        var nkarm = setup.Nkarm;
        var ncat = setup.Ncat;

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

        var fields = TargetFields
            .Select(t => new FieldSample
            {
                Name = t.Name,
                Offset = t.Offset(layout),
                Length = FieldLength(t.Name, setup),
                DoubleTotal = new double[FieldLength(t.Name, setup)],
                Float32Total = new float[FieldLength(t.Name, setup)],
                TrueTermCount = new long[FieldLength(t.Name, setup)],
                LumpedAttempts = new long[FieldLength(t.Name, setup)],
                LumpedTermsInLumps = new long[FieldLength(t.Name, setup)],
                Deltas = Enumerable.Range(0, FieldLength(t.Name, setup)).Select(_ => new List<double>()).ToArray(),
            })
            .ToArray();
        var fieldByName = fields.ToDictionary(f => f.Name);
        var allvdok = fieldByName["Allvdok"];
        var vdokstr = fieldByName["Vdokstr"];
        var vdokTotal = fieldByName["VdokTotal"];
        var vdokTotal2 = fieldByName["VdokTotal2"];
        var fmkarmCor = fieldByName["FmkarmCor"];
        var fmkarm2 = fieldByName["Fmkarm2"];
        var dokp41 = fieldByName["Dokp41"];
        var dokp31 = fieldByName["Dokp31"];

        long attemptsRun = 0;
        long allvdokMismatches = 0;
        string? allvdokFirstMismatch = null;
        long vdokstrMismatches = 0;
        string? vdokstrFirstMismatch = null;

        // Persistent shadow accumulators, fed the exact same term sequence in the exact same
        // order as record's own Allvdok/Vdokstr cells and reset in lockstep with them (only on
        // Accepted, never per attempt): comparing recordAfter[cell]-recordBefore[cell] against a
        // per-attempt replayed sum is unsound once record already carries a long chain of prior
        // attempts on the same particle (RestartedInsideLoop/RestartedAfterLoop never reset it),
        // because fl(fl(a + term) - a) does not always recover fl(term) bit for bit once `a`
        // dwarfs `term` -- a double-precision instance of the same cancellation the REAL*4 story
        // is about, just nine-plus orders of magnitude smaller, and enough to break a bit-for-bit
        // check though not to matter to any reported figure. Comparing two independently-computed
        // running totals directly (never subtracted) sidesteps it: both take the identical
        // sequence of IEEE754 additions in the identical order, so they are bit-identical
        // whenever the replayed terms themselves are the original's own.
        var myAllvdok = new double[ndok];
        var myVdokstr = new double[ndok];

        var streams = OriginalSeeds.Streams;
        long acceptedParticles = 0;

        var integerBefore = integerTotals.GetAsArray1D();
        var recordBefore = record.GetAsArray1D();

        void RunOneAttempt(in CycleInputs cycle)
        {
            attemptsRun++;
            var streamsBeforeAttempt = streams; // copy: the replay below runs on this copy only.

            var outcome = Attempt.Run(
                setup, fractions, cycle, ref streams,
                integerTotals.View.BaseView, record.View.BaseView, scratch.View.BaseView);

            var integerAfter = integerTotals.GetAsArray1D();
            var recordAfter = record.GetAsArray1D();

            // -- Allvdok / Vdokstr: replay the base and every neighbour draw (class remarks). --
            var replay = streamsBeforeAttempt;
            var xBase = Mcg128.Next(ref replay.S1);
            var x0Base = Mcg128.Next(ref replay.S2);
            SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
                xBase, x0Base, out var drReplay, out _);
            var iksBase = (int)(drReplay / setup.CellSize);
            var baseTerm = SpherePi / 6.0 * drReplay * drReplay * drReplay;
            FeedTerm(allvdok, iksBase, baseTerm);
            myAllvdok[iksBase] += baseTerm;

            var neighbourDraws = integerAfter[layout.Nfz] - integerBefore[layout.Nfz];
            for (var i = 0L; i < neighbourDraws; i++)
            {
                var x2 = Mcg128.Next(ref replay.S4);
                var x21 = Mcg128.Next(ref replay.S5);
                SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, fractions.Bounds, fractions.Cumulative,
                    x2, x21, out var dbReplay, out var fractionNeighbourReplay);

                var iksNeighbour = (int)(dbReplay / setup.CellSize);
                var term = SpherePi / 6.0 * dbReplay * dbReplay * dbReplay;

                FeedTerm(allvdok, iksNeighbour, term); // line 524: unconditional, every draw.
                myAllvdok[iksNeighbour] += term;

                // lines 528, 533: pocket-forming fraction and db > Dmin -- both already-public
                // inputs of Attempt.Run itself, not a decision this class invents.
                if (fractions.PocketForming[fractionNeighbourReplay] != 0 && dbReplay > setup.Dmin)
                {
                    FeedTerm(vdokstr, iksNeighbour, term); // line 542.
                    myVdokstr[iksNeighbour] += term;
                }
            }

            for (var c = 0; c < ndok; c++)
            {
                allvdok.DoubleTotal[c] += recordAfter[layout.Allvdok + c] - recordBefore[layout.Allvdok + c];
                if (myAllvdok[c] != recordAfter[layout.Allvdok + c])
                {
                    allvdokMismatches++;
                    allvdokFirstMismatch ??=
                        $"cell {c}: record holds {recordAfter[layout.Allvdok + c]:G17}, replay shadow holds {myAllvdok[c]:G17}";
                }
            }

            for (var c = 0; c < ndok; c++)
            {
                vdokstr.DoubleTotal[c] += recordAfter[layout.Vdokstr + c] - recordBefore[layout.Vdokstr + c];
                if (myVdokstr[c] != recordAfter[layout.Vdokstr + c])
                {
                    vdokstrMismatches++;
                    vdokstrFirstMismatch ??=
                        $"cell {c}: record holds {recordAfter[layout.Vdokstr + c]:G17}, replay shadow holds {myVdokstr[c]:G17}";
                }
            }

            // -- FmkarmCor, Dokp41, Dokp31: read the true count from their own atomic companion. --
            FeedFromCompanionCounter(fmkarmCor, layout.FmkarmCor, nkarm, recordBefore, recordAfter,
                c => integerAfter[layout.FqkarmCor + c] - integerBefore[layout.FqkarmCor + c]);
            FeedFromCompanionRow(dokp41, layout.Dokp41, ncat, ndok, layout.Qdoks,
                recordBefore, recordAfter, integerBefore, integerAfter);
            FeedFromCompanionRow(dokp31, layout.Dokp31, ncat, ndok, layout.Qdoks,
                recordBefore, recordAfter, integerBefore, integerAfter);

            // -- VdokTotal, VdokTotal2, Fmkarm2: already exact, at most one term per attempt. --
            FeedCommitted(vdokTotal, layout.VdokTotal, ndok, recordBefore, recordAfter);
            FeedCommitted(vdokTotal2, layout.VdokTotal2, 1, recordBefore, recordAfter);
            FeedCommitted(fmkarm2, layout.Fmkarm2, nkarm, recordBefore, recordAfter);

            integerBefore = integerAfter;
            recordBefore = recordAfter;

            if (outcome == AttemptOutcome.Accepted)
            {
                acceptedParticles++;
                record.MemSetToZero();
                recordBefore = record.GetAsArray1D();
                Array.Clear(myAllvdok); // kept in lockstep with record's own reset (class remarks).
                Array.Clear(myVdokstr);
            }
        }

        var cycleZero = ConstructedModel.CycleZero();
        for (var i = 0; i < cycle0Attempts; i++)
        {
            RunOneAttempt(cycleZero);
        }

        using var qks1 = accelerator.Allocate1D<double>(setup.Nkarm);
        PocketHistogram.Normalize(layout, integerTotals.View.BaseView, qks1.View.BaseView);

        using var pdoksmall = accelerator.Allocate1D<double>(setup.Ndok);
        pdoksmall.MemSetToZero();

        var cycleOne = new CycleInputs
        {
            CycleFlag = 1,
            Dmaxxx = 1e30,
            Qks1 = qks1.View.BaseView,
            Pdoksmall = pdoksmall.View.BaseView,
        };

        var loopCompletionsBefore = integerBefore[layout.LoopCompletions];
        for (var i = 0; i < cycle1Attempts; i++)
        {
            RunOneAttempt(cycleOne);

            // Reference mode: QKS1 is refreshed after every attempt that completed the
            // neighbour loop (root BOOT.md, "Reference mode is the original's
            // sequence"; `src/Particle/BOOT.md`, "Attempt structure"). LoopCompletions
            // (an exact integer total) tells us whether this attempt was one of those,
            // without decoding its AttemptOutcome a second time. RunOneAttempt already
            // refreshed the cached integerBefore snapshot to the post-attempt array, so
            // no extra device read is needed here.
            var loopCompletionsAfter = integerBefore[layout.LoopCompletions];
            if (loopCompletionsAfter != loopCompletionsBefore)
            {
                // Normalize only writes qks1, never integerTotals (API.md, "Side effects"), so
                // the cached integerBefore snapshot RunOneAttempt already set stays valid.
                PocketHistogram.Normalize(layout, integerTotals.View.BaseView, qks1.View.BaseView);
                loopCompletionsBefore = loopCompletionsAfter;
            }
        }

        var totals = integerTotals.GetAsArray1D();

        var replayChecks = new[]
        {
            new ReplayCheck("Allvdok", attemptsRun, allvdokMismatches, allvdokFirstMismatch),
            new ReplayCheck("Vdokstr", attemptsRun, vdokstrMismatches, vdokstrFirstMismatch),
        };

        return new SweepResult(
            fields, replayChecks, cycle0Attempts, cycle1Attempts, acceptedParticles,
            totals[layout.Nfx], totals[layout.Nfy], totals[layout.Nfq], totals[layout.Nfw]);
    }
}
