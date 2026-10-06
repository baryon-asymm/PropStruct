using System.Collections.Immutable;
using PropStruct.Input;
using PropStruct.Particle;

namespace PropStruct.Statistics;

/// <summary>
/// The setup run once before the first cycle: array sizes, preconditions, the
/// fraction-law tables and the analytical oxidizer size (Fortran lines 267-277,
/// 376-406 and subroutine <c>PARAM</c>, 1695-1757). Host code, double precision,
/// allocation allowed (BOOT.md, Constraints).
/// </summary>
internal static class Setup
{
    /// <summary>
    /// Fortran lines 267-277, with the micrometre-to-metre conversion of lines 260-266
    /// redone in binary32 for these three sizes only (BOOT.md, "Array sizes from
    /// binary32 values"): <paramref name="ddokmax"/>, <paramref name="ndok"/>,
    /// <paramref name="nkarm"/>, <paramref name="ncat"/> and the fixed
    /// <paramref name="nc"/> = 1000. <paramref name="ddokmax"/> here serves only
    /// <c>Ndok</c>/<c>Nkarm</c>/<c>Ncat</c>'s own numerators, computed unconditionally
    /// through <see cref="ToBinary32Metres"/> (a single binary32 multiply, not stored a
    /// second time) under the root's array-size exception, which applies under either
    /// precision kind alike and needs no second rounding to size an array correctly.
    /// It is deliberately <em>not</em> the value <see cref="Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> reports as
    /// <c>Ddokmax</c> (<see cref="SetupEchoes"/>'s own doc comment): that one is the
    /// genuinely stored value each fraction's own upper bound already rounds to via
    /// <see cref="ToBinary32MetresStored"/>, computed separately in <see cref="Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/>'s
    /// own fraction loop, once the two were found to disagree by review (2026-09-24) --
    /// measured to make no difference to <c>Ndok</c>/<c>Nkarm</c>/<c>Ncat</c> on any of
    /// the 49 archived formulations at the default cell size, so this method's own
    /// array-sizing numerator is left as it was. <c>Ndok</c>'s division by
    /// <see cref="Binary32.TruncatedQuotient"/> rounding <paramref name="ddokmax"/> again
    /// is correct for that numerator; <c>Nkarm</c>/<c>Ncat</c>'s own numerator,
    /// <c>Ddokmax*AK4</c>, is never stored to a variable of its own
    /// (<see cref="Binary32.Multiply"/>'s own doc comment), so
    /// <see cref="Binary32.TruncatedQuotientOfProduct"/> leaves it unrounded (S-2, audit
    /// of 2026-09-24: the two were wrongly sharing <see cref="Binary32.TruncatedQuotient"/>,
    /// which double-rounded this product).
    /// </summary>
    public static void Sizes(
        ImmutableArray<OxidizerFraction> fractions, Length cellSize, Length categoryStep, double ak4,
        out double ddokmax, out int ndok, out int nkarm, out int ncat, out int nc)
    {
        var di = ToBinary32Metres(cellSize);
        var dj = ToBinary32Metres(categoryStep);
        var ak4Binary32 = Binary32.ToNearestRepresentable(ak4);

        var maxUpperBound = 0.0;
        foreach (var fraction in fractions)
        {
            var upper = ToBinary32Metres(fraction.UpperBound);
            if (upper > maxUpperBound)
            {
                maxUpperBound = upper;
            }
        }

        ddokmax = maxUpperBound;
        ndok = (int)Binary32.TruncatedQuotient(ddokmax, di) + 2;

        var ddokmaxAk4 = Binary32.Multiply(ddokmax, ak4Binary32);
        nkarm = (int)Binary32.TruncatedQuotientOfProduct(ddokmaxAk4, di) + 2;
        ncat = (int)Binary32.TruncatedQuotientOfProduct(ddokmaxAk4, dj) + 2;
        nc = 1000;
    }

    /// <summary>
    /// Fortran lines 379-406: the analytical mass-medium oxidizer size and its standard
    /// deviation. <c>DOKSD</c> uses the same formula for both size laws (lines 385-386,
    /// 399-400); only <c>DOKM</c> differs, and the final <c>DOKSD -= DOKM^2</c> (line
    /// 405) applies to both (BOOT.md, Invariants).
    /// </summary>
    /// <remarks>
    /// Under <see cref="PrecisionKind.Original"/> (BOOT.md, "## Setup plane", "Sums as the
    /// listing stores them"): in the <paramref name="sizeLaw"/> == 2 branch, <c>DOK4</c>/<c>DOK3</c>
    /// (<c>AnalyticSizeNumerator</c>/<c>AnalyticSizeDenominator</c>) are stored every pass and
    /// round after every addition, and <c>DOKM</c> is a single store from already-rounded
    /// operands; for any other law <c>DOKM</c> is unrolled five-fold, stored after the fifth
    /// pass of a block and after every remainder pass (<see cref="UnrolledSchedule"/>). <c>DOKSD</c>'s
    /// sum stays in the register through that loop and <c>DOKSD -= DOKM^2</c> (line 405)
    /// subtracts from it unrounded; the <paramref name="sizeLaw"/> == 2 loop is unrolled
    /// four-fold, stores the sum after passes 1-3 of a block and holds its width and
    /// <c>l**5</c> in REAL*4 temporaries. The literal
    /// <c>0.8</c> (<c>MassUniformMeanSizeCoefficient</c>) is a single REAL*4 literal operand,
    /// rounded once. <paramref name="massShare"/> and <paramref name="bounds"/> are expected
    /// already rounded to binary32 by the caller; <paramref name="share"/> (<c>ZX</c>,
    /// REAL*8) is not. Under <see cref="PrecisionKind.Binary64"/> every rounding collapses to
    /// the identity, reproducing this method's own arithmetic bit for bit. Lines 379-406 lie
    /// outside <c>CyclePlane.listing.generated.txt</c>'s address scope, so both unroll factors,
    /// the stores and the power expansions are typed here and in the twin, a declared deviation
    /// (BOOT.md, "## Setup plane"), held by the executable's pre-loop: the oracle's pins BK10,
    /// KB397, AK157a and CSPX04, the 49-file survey and 120 drawn loops (<c>PreloopSurveyTests</c>).
    ///
    /// ⚠ 2026-10-03: was rule L, <c>DOKM</c> and <c>DOKSD</c> each carried unrounded and rounded
    /// once at the loop's exit; the listing stores <c>DOKM</c> on the five-fold schedule above
    /// and, outside the JZZ = 2 loop, never stores <c>DOKSD</c>'s sum (src/Statistics/HISTORY.md#setup-rule-l-sums-2026-10-03).
    /// </remarks>
    public static void AnalyticSizes(
        int sizeLaw, int fractionCount, double[] massShare, double[] share, double[] bounds, PrecisionKind precision,
        out double dokm, out double doksd)
    {
        double Round(double value) => precision == PrecisionKind.Original ? Binary32.ToNearestRepresentable(value) : value;

        var original = precision == PrecisionKind.Original;

        // DOKSD's own sum stays in a register through its loop and line 405 subtracts DOKM**2 from that
        // register, so it is never stored before the final store, except in the JZZ = 2 loop, which
        // stores it after passes 1-3 of each of its four-fold blocks (0x408D7B and its siblings) and
        // carries it in the register after the fourth and through the remainder.
        var fourFold = new UnrolledSchedule(fractionCount, 4);
        var doksdAccumulator = 0.0;
        for (var j = 0; j < fractionCount; j++)
        {
            var lower = bounds[2 * j];
            var upper = bounds[2 * j + 1];
            var term = massShare[j] * (lower * lower + lower * upper + upper * upper) / 3.0;
            doksdAccumulator += term;
            if (sizeLaw == 2 && fourFold.IsBlockPassBeforeLast(j + 1))
            {
                doksdAccumulator = Round(doksdAccumulator);
            }
        }

        if (sizeLaw == 2)
        {
            // DOK4/DOK3 are stored every pass of their loop: rounded after every addition. Under Original the
            // terms are the listing's (0x408CFE-0x408F8E): DOK**4 is (x*x)*(x*x) and DOK**5 is x times that,
            // l**5 is stored to a REAL*4 temporary every pass and u**5 - l**5 subtracts that stored value, and
            // the width u - l is a REAL*4 temporary in pass 1 of a block and in every remainder pass
            // (a register, unrounded, in passes 2-4).
            var dok4 = 0.0;
            var dok3 = 0.0;
            for (var j = 0; j < fractionCount; j++)
            {
                var lower = bounds[2 * j];
                var upper = bounds[2 * j + 1];
                double term4;
                double term3;
                if (original)
                {
                    var lowerSquared = lower * lower;
                    var upperSquared = upper * upper;
                    var lowerFourth = lowerSquared * lowerSquared;
                    var upperFourth = upperSquared * upperSquared;
                    var width = fourFold.ReloadsBefore(j + 1) ? Binary32.ToNearestRepresentable(upper - lower) : upper - lower;
                    term4 = (upper * upperFourth - Binary32.ToNearestRepresentable(lower * lowerFourth)) * share[j] / width;
                    term3 = (upperFourth - lowerFourth) * share[j] / width;
                }
                else
                {
                    term4 = (Math.Pow(upper, 5) - Math.Pow(lower, 5)) * share[j] / (upper - lower);
                    term3 = (Math.Pow(upper, 4) - Math.Pow(lower, 4)) * share[j] / (upper - lower);
                }

                dok4 = Round(dok4 + term4);
                dok3 = Round(dok3 + term3);
            }

            var massUniformCoefficient = original ? Binary32.ToNearestRepresentable(0.8) : 0.8;
            dokm = Round(massUniformCoefficient * dok4 / dok3);
        }
        else
        {
            // DOKM's loop is unrolled five-fold: the register sum is stored after the fifth pass
            // of a block and after every pass of the remainder (a loop of fewer than five
            // trips has no block). The five is read off the listing by hand, not from the table
            // (declared deviation, BOOT.md "## Setup plane"; held by the oracle's pinned cases).
            var schedule = new UnrolledSchedule(fractionCount, 5);
            dokm = 0.0;
            for (var j = 0; j < fractionCount; j++)
            {
                var lower = bounds[2 * j];
                var upper = bounds[2 * j + 1];
                var term = (lower + upper) * massShare[j] / 2.0;
                dokm += term;
                if (schedule.RoundsAfter(j + 1))
                {
                    dokm = Round(dokm);
                }
            }
        }

        doksd = Round(doksdAccumulator - dokm * dokm);
    }

    /// <summary>
    /// The setup preconditions (BOOT.md, Constraints: "the original hangs or indexes
    /// out of bounds without them"), checked in the order <see cref="SetupStatus"/>
    /// declares them; the fraction-law tables (<see cref="FractionLaw.Build"/>), the
    /// tail-probability draw (<see cref="FractionLaw.TryTailDraw"/>) and the analytical
    /// oxidizer size (<see cref="AnalyticSizes"/>) only run once every precondition
    /// holds. <paramref name="setup"/>'s own <c>Dmax</c> is left at its default (0) and
    /// <paramref name="draw"/> carries what <c>Particle.SizeLaw.Sample</c> needs to
    /// produce it; the caller passes the sampled diameter to
    /// <see cref="CompleteEchoes"/>, which finishes both (API.md, "Setup hand-off").
    /// </summary>
    /// <remarks>
    /// <paramref name="precision"/> (root BOOT.md, "Precision kind is an option of every
    /// run"; BOOT.md, "## Setup plane"): under <see cref="PrecisionKind.Original"/>, every
    /// value the setup plane reads (<c>GDOK</c>, <c>DDOK</c>, <c>Dmin</c>, <c>Di</c>,
    /// <c>Dj</c>, <c>PLOT1</c>, <c>PLOT2</c>, <c>GGG0</c>, <c>gdokns</c> --
    /// <c>SetupPlane.generated.txt</c>'s own <c>input</c> rows) is rounded to binary32
    /// before use, exactly as the Fortran's own REAL*4 storage of it; <c>GGG</c> and λ
    /// (<c>sLamd</c>) are rounded after being stored (the <c>store</c> rows). <c>Di</c>
    /// and <c>Dj</c> round through <see cref="ToBinary32MetresStored"/>, the same store as
    /// <c>Dmin</c> (Fortran lines 264-265, immediately above <c>Dmin</c>'s own line 266);
    /// <see cref="Sizes"/>'s own, separate, unconditional rounding of <c>Di</c>/<c>Dj</c> for
    /// <c>Ndok</c>/<c>Nkarm</c>/<c>Ncat</c> (this node's own BOOT.md, "Array sizes from binary32
    /// values") is untouched by this paragraph: that one array-size exception applies under
    /// both kinds alike and never reaches <paramref name="setup"/>'s own <c>CellSize</c>/
    /// <c>CategoryStep</c> fields, which every other consumer -- <see cref="Categories"/>,
    /// <see cref="CycleStatistics"/>, <see cref="SmallParticles"/> -- reads instead ("##
    /// Setup plane", "`Di`/`Dj` round too"). Under <see cref="PrecisionKind.Binary64"/> every
    /// one of these is exactly what this method always computed, bit for bit.
    /// </remarks>
    /// <summary>
    /// Compatibility overload kept for callers outside this node's subtree that still
    /// build against the pre-2026-09-24 four-out-parameter shape (this node's own BOOT.md,
    /// "## Setup plane", "Design decision, 2026-09-24" adds <see cref="SetupInputs"/>,
    /// AGENTS.md §3: a neighbour's own test node may not be edited from here to follow
    /// the new signature). Forwards to the five-out-parameter overload and discards
    /// <c>inputs</c>; every rounding this date's decision adds (AK1-4, Alpha, NnMin,
    /// NnMax, the two pocket/bridge coefficients into <see cref="ModelSetup"/>) still
    /// runs for a caller that uses this overload, since it depends only on
    /// <paramref name="precision"/>, never on whether <c>inputs</c> is read.
    /// </summary>
    public static SetupStatus Prepare(
        Formulation formulation, ModelParameters parameters, PrecisionKind precision,
        int neighbourBudget, int pocketRedrawBudget,
        out ModelSetup setup, out SetupTables tables, out TailDraw draw, out PendingEchoes pending)
    {
        return Prepare(formulation, parameters, precision, neighbourBudget, pocketRedrawBudget,
            out setup, out tables, out draw, out pending, out _);
    }

    /// <summary>
    /// Fortran lines 267-277, 376-406 and subroutine <c>PARAM</c> (1695-1757), plus the
    /// formulation-read block (93-133) and the menu (144-244) read for the kind of this
    /// node's inputs only (BOOT.md, "## Setup plane", "Design decision, 2026-09-24").
    /// <paramref name="inputs"/> carries every generated setup-plane member of role
    /// <c>input</c> that this node's own per-cycle processing needs and that
    /// <see cref="ModelSetup"/>/<see cref="SetupTables"/> does not already carry
    /// (<see cref="SetupInputs"/>'s own doc comment).
    /// </summary>
    public static SetupStatus Prepare(
        Formulation formulation, ModelParameters parameters, PrecisionKind precision,
        int neighbourBudget, int pocketRedrawBudget,
        out ModelSetup setup, out SetupTables tables, out TailDraw draw, out PendingEchoes pending,
        out SetupInputs inputs)
    {
        setup = default;
        tables = null!;
        draw = default;
        pending = null!;
        inputs = null!;

        var original = precision == PrecisionKind.Original;

        var ak1 = original ? Binary32.ToNearestRepresentable(formulation.Ak1) : formulation.Ak1;
        var ak2 = original ? Binary32.ToNearestRepresentable(formulation.Ak2) : formulation.Ak2;
        var ak3 = original ? Binary32.ToNearestRepresentable(formulation.Ak3) : formulation.Ak3;
        var ak4 = original ? Binary32.ToNearestRepresentable(formulation.Ak4) : formulation.Ak4;
        if (!(ak1 > 0.0) || !(ak2 > 1.0) || !(ak3 > 0.0) || !(ak3 < ak4))
        {
            return SetupStatus.InvalidCoefficients;
        }

        var cellSizeMetres = original ? ToBinary32MetresStored(parameters.CellSize) : parameters.CellSize.Metres;
        var categoryStepMetres = original ? ToBinary32MetresStored(parameters.CategoryStep) : parameters.CategoryStep.Metres;
        if (!(cellSizeMetres > 0.0) || !(categoryStepMetres > 0.0))
        {
            return SetupStatus.InvalidCellSize;
        }

        Sizes(formulation.Fractions, parameters.CellSize, parameters.CategoryStep, ak4,
            out var ddokmax, out var ndok, out var nkarm, out var ncat, out var nc);

        var dminMetres = original ? ToBinary32MetresStored(parameters.Dmin) : parameters.Dmin.Metres;
        if (!(dminMetres >= 0.0) || !(dminMetres < ddokmax))
        {
            return SetupStatus.InvalidMinimumSize;
        }

        var fractionCount = formulation.Fractions.Length;
        var bounds = new double[2 * fractionCount];
        var massShare = new double[fractionCount];
        // The *reported* Ddokmax (site map: "not ported" was wrong; corrected below), distinct from Sizes'
        // own ddokmax above: Fortran line 261 stores DDOK(2*kilo) to REAL*4 before line 271 copies it into
        // Ddokmax, so under Original the echo is the same stored value each upper bound already rounds to
        // here (ToBinary32MetresStored), not Sizes' unconditional, un-stored-rounded one. Tracked over the
        // upper bounds only (never the lower ones), matching "the largest fraction upper bound" exactly.
        var storedDdokmax = 0.0;
        for (var i = 0; i < fractionCount; i++)
        {
            var fraction = formulation.Fractions[i];
            var lower = original ? ToBinary32MetresStored(fraction.LowerBound) : fraction.LowerBound.Metres;
            var upper = original ? ToBinary32MetresStored(fraction.UpperBound) : fraction.UpperBound.Metres;
            if (!(lower > 0.0) || !(lower < upper))
            {
                return SetupStatus.InvalidFractionBounds;
            }

            bounds[2 * i] = lower;
            bounds[2 * i + 1] = upper;
            massShare[i] = original ? Binary32.ToNearestRepresentable(fraction.MassShare) : fraction.MassShare;
            if (upper > storedDdokmax)
            {
                storedDdokmax = upper;
            }
        }

        foreach (var share in massShare)
        {
            if (!(share > 0.0))
            {
                return SetupStatus.ZeroFractionShare;
            }
        }

        var pocketForming = new byte[fractionCount];
        var hasPocketForming = false;
        if (formulation.PocketFormingFractions is { } flags)
        {
            if (flags.Length < fractionCount)
            {
                return SetupStatus.InvalidPocketFormingFractionCount;
            }

            for (var i = 0; i < fractionCount; i++)
            {
                // SFR(Nfract) == 0 (Fortran line 473): zero stays zero, any nonzero flag
                // forms pockets. A plain `(byte)flags[i]` narrows silently instead (D7,
                // audit of 2026-09-24) -- a flag of 256 becomes 0, so a nonzero SFR value
                // outside the byte range would be read as not pocket-forming.
                var forming = flags[i] != 0 ? (byte)1 : (byte)0;
                pocketForming[i] = forming;
                hasPocketForming |= forming != 0;
            }
        }
        else
        {
            for (var i = 0; i < fractionCount; i++)
            {
                pocketForming[i] = 1;
            }

            hasPocketForming = fractionCount > 0;
        }

        if (!hasPocketForming)
        {
            return SetupStatus.NoPocketFormingFraction;
        }

        var tailProbability = parameters.TailProbability;
        if (tailProbability is < 0.0 or >= 1.0)
        {
            return SetupStatus.InvalidTailProbability;
        }

        var ggg0 = original ? Binary32.ToNearestRepresentable(formulation.OxidizerMassFraction) : formulation.OxidizerMassFraction;
        var gdokns = original ? Binary32.ToNearestRepresentable(parameters.HomogenizedOxidizerFraction) : parameters.HomogenizedOxidizerFraction;
        var gggRaw = ggg0 - gdokns * ggg0;
        var ggg = original ? Binary32.ToNearestRepresentable(gggRaw) : gggRaw;
        if (ggg is <= 0.0 or >= 1.0)
        {
            return SetupStatus.InvalidOxidizerFraction;
        }

        // k6/k10 (NnMin/NnMax), REAL explicit in the source, round the same way every
        // other generated input does (SetupPlane.generated.txt's own nn_min/nn_max
        // rows); checked against the rounded values, matching the Dmin/CellSize
        // pattern above (added 2026-09-24, BOOT.md "## Setup plane", "Design decision").
        var nnMin = original ? Binary32.ToNearestRepresentable(parameters.NnMin) : parameters.NnMin;
        var nnMax = original ? Binary32.ToNearestRepresentable(parameters.NnMax) : parameters.NnMax;
        if (!(nnMax > 0.0) || !(nnMin < nnMax))
        {
            return SetupStatus.InvalidNnWindow;
        }

        FractionLaw.Build(formulation.SizeLawCode, fractionCount, massShare, bounds, precision, out var share2, out var cumulative, out var zss);
        if (!FractionLaw.TryTailDraw(fractionCount, bounds, cumulative, tailProbability, out var x, out var x1, out var tailProbabilityModified))
        {
            return SetupStatus.NoActiveFraction;
        }

        var plot1 = original ? Binary32.ToNearestRepresentable(formulation.OxidizerDensity) : formulation.OxidizerDensity;
        var plot2 = original ? Binary32.ToNearestRepresentable(formulation.PropellantDensity) : formulation.PropellantDensity;
        // Line 378: the executable divides once into a REAL*4 temporary, then forms (ZSS * G) * PLOT2
        // and stores it (CyclePlane.listing.generated.txt, the r378 rows).
        var rounding = new CyclePlaneRounding(precision);
        var g378 = rounding.Temporary(ggg / plot1); // Fortran 378
        var lambda = rounding.Store(g378 * zss * plot2); // Fortran 378
        AnalyticSizes(formulation.SizeLawCode, fractionCount, massShare, share2, bounds, precision, out var dokm, out var doksd);

        // k5 (Alpha), k7/k8 (PocketCoefficient/BridgeCoefficient), GM (MetalMassFraction),
        // eps_dok (EpsDok) and eta (AggregatedOxideFraction): every other generated
        // input this node's setup does not already round above (SetupPlane.generated.txt's
        // own alpha/karmcoef/mkmcoef/gm/eps_dok/eta rows), added 2026-09-24 (BOOT.md,
        // "## Setup plane", "Design decision, 2026-09-24"). k5/k7/k8 feed the attempt
        // plane through ModelSetup, exactly as AK1-4 do above; GM/eps_dok/eta feed only
        // this node's own per-cycle processing and Output's header echoes, through
        // SetupInputs (SetupInputs.cs's own doc comment).
        var alpha = original ? Binary32.ToNearestRepresentable(parameters.Alpha) : parameters.Alpha;
        var pocketCoefficient = original ? Binary32.ToNearestRepresentable(parameters.PocketCoefficient) : parameters.PocketCoefficient;
        var bridgeCoefficient = original ? Binary32.ToNearestRepresentable(parameters.BridgeCoefficient) : parameters.BridgeCoefficient;
        var metalMassFraction = original ? Binary32.ToNearestRepresentable(formulation.MetalMassFraction) : formulation.MetalMassFraction;
        var epsDok = original ? Binary32.ToNearestRepresentable(parameters.EpsDok) : parameters.EpsDok;
        var eta = original ? Binary32.ToNearestRepresentable(parameters.AggregatedOxideFraction) : parameters.AggregatedOxideFraction;

        var layout = AccumulatorLayout.Create(fractionCount, ndok, nkarm, ncat, nc);

        setup = new ModelSetup
        {
            FractionCount = fractionCount,
            SizeLaw = formulation.SizeLawCode,
            CellSize = cellSizeMetres,
            CategoryStep = categoryStepMetres,
            Dmin = dminMetres,
            Dmax = 0.0, // Completed by CompleteEchoes once the driver has sampled it.
            Lambda = lambda,
            Ak1 = ak1,
            Ak2 = ak2,
            Ak3 = ak3,
            Ak4 = ak4,
            Variant = parameters.Variant,
            Alpha = alpha,
            PocketCoefficient = pocketCoefficient,
            BridgeCoefficient = bridgeCoefficient,
            NnMin = nnMin,
            NnMax = nnMax,
            Ndok = ndok,
            Nkarm = nkarm,
            Ncat = ncat,
            Nc = nc,
            NeighbourBudget = neighbourBudget,
            PocketRedrawBudget = pocketRedrawBudget,
            Layout = layout,
            Kind = precision, // Setup.Prepare sets the run's kind itself (audit R2); Simulator no longer patches it.
        };

        tables = new SetupTables(bounds, cumulative, share2, pocketForming, zss, massShare);
        draw = new TailDraw(x, x1);
        pending = new PendingEchoes(dokm, doksd, storedDdokmax, tailProbabilityModified, ggg);
        inputs = new SetupInputs(plot1, plot2, ggg0, metalMassFraction, gdokns, epsDok, eta);
        return SetupStatus.Ok;
    }

    /// <summary>
    /// Finishes the setup once the driver has evaluated <c>Particle.SizeLaw.Sample</c>
    /// with the <see cref="TailDraw"/> <see cref="Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> produced: writes
    /// <paramref name="setup"/>'s own <c>Dmax</c> and returns the completed
    /// <see cref="SetupEchoes"/> (API.md, "Setup hand-off").
    /// </summary>
    /// <remarks>
    /// <c>Dmax</c> is <c>PARAM</c>'s own REAL local (Fortran line 1696, 1736, 1739;
    /// <c>SetupPlane.generated.txt</c>'s own <c>dmax</c> row) — the diameter
    /// <c>Particle.SizeLaw.Sample</c> returns in <c>double</c> is what the Fortran's
    /// call-by-reference stores into that REAL*4 slot (Fortran line 1754), so it rounds
    /// here, under <see cref="PrecisionKind.Original"/>, the same way every other
    /// generated setup-plane store does (BOOT.md, "## Setup plane", "Design decision,
    /// 2026-09-24"). <paramref name="setup"/>'s own <see cref="ModelSetup.Kind"/> is
    /// already set by <see cref="Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/> by the time a caller reaches this method
    /// (API.md, "Setup hand-off"), so no separate precision parameter is needed here.
    /// </remarks>
    public static SetupEchoes CompleteEchoes(ref ModelSetup setup, PendingEchoes pending, double dmax)
    {
        var roundedDmax = setup.Kind == PrecisionKind.Original ? Binary32.ToNearestRepresentable(dmax) : dmax;
        setup.Dmax = roundedDmax;
        return new SetupEchoes(pending.Dokm, pending.Doksd, pending.Ddokmax, roundedDmax, pending.TailProbabilityModified, pending.OxidizerMassFractionEffective);
    }

    private static double ToBinary32Metres(Length length)
    {
        return length.IsMicrometres
            ? Binary32.Multiply(length.AsWritten, 1e-6)
            : Binary32.ToNearestRepresentable(length.AsWritten);
    }

    /// <summary>
    /// The setup plane's own micrometre-to-metre conversion (Fortran lines 260-266, BOOT.md
    /// "## Setup plane": "binary32(value) × binary32(1e-6), the product rounded to
    /// binary32"), used by <see cref="Prepare(PropStruct.Input.Formulation, PropStruct.Input.ModelParameters, PropStruct.Particle.PrecisionKind, int, int, out PropStruct.Particle.ModelSetup, out SetupTables, out TailDraw, out PendingEchoes, out SetupInputs)"/>'s own per-fraction bound rounding and by its
    /// <c>Dmin</c>, <c>Di</c> and <c>Dj</c> rounding (Fortran lines 264-266: <c>if
    /// (Di.ge.0.1) Di = Di*1e-6</c>, <c>if (Dj.ge.0.1) Dj = Dj*1e-6</c>, <c>if (Dmin.ge.0.1)
    /// Dmin = Dmin*1e-6</c>, three consecutive stores of the same shape as <c>DDOK</c>'s own
    /// two lines above them; none of the three has a declaration in the source's declaration
    /// block, lines 4-64, so all three are REAL*4 by implicit typing, the same fallback rule
    /// <c>classify-setup-plane.py</c> already applies to <c>gdokns</c>) — all under
    /// <see cref="PrecisionKind.Original"/>. Unlike <see cref="ToBinary32Metres"/>
    /// (<see cref="Sizes"/>'s own, unconditional array-size exception, this node's own BOOT.md,
    /// "Array sizes from binary32 values"), this rounds the product too: the Fortran
    /// statement is a plain assignment, <c>DDOK(2*kilo) = DDOK(2*kilo)*1e-6</c> (and,
    /// identically shaped, <c>Di = Di*1e-6</c>/<c>Dj = Dj*1e-6</c>), storing into the
    /// REAL*4 variable itself, unlike <c>Ddokmax*AK4</c> (<see cref="Binary32.Multiply"/>'s
    /// own doc comment), which feeds a truncating division directly and is never stored --
    /// the same distinction that keeps <see cref="Sizes"/>'s own internal, unconditional
    /// <c>di</c>/<c>dj</c> (feeding only <c>Ndok</c>/<c>Nkarm</c>/<c>Ncat</c>'s truncated
    /// quotients) on <see cref="ToBinary32Metres"/> while <paramref name="length"/> here, once
    /// it becomes <see cref="ModelSetup.CellSize"/>/<see cref="ModelSetup.CategoryStep"/>, the
    /// stored value every other consumer reads, needs the second rounding.
    /// Measured: without this second rounding, `inpt`'s and `p350`'s own <c>epsdokfr[0]</c>
    /// come out an order of magnitude short of the frozen positive controls (8.35e-9 and
    /// 9.50e-9 against 2.7003e-8 and 2.6290e-8); with it, both match bit for bit
    /// (ACCEPTANCE.md). Without <c>Dmin</c>'s own rounding here, HMX and
    /// HPEPA3 print a nonzero <c>fineoxy_fr</c> the original never does — every reference
    /// formulation whose lowest fraction bound equals the default <c>Dmin</c> (both read as
    /// the same decimal, 10 μm) rounds the two to different binary32 values without this fix,
    /// opening a one-ULP sliver <c>Attempt</c>'s own "Dok &lt; Dmin" check can land a draw in.
    /// Without <c>Di</c>'s own rounding here, the printed <c>fmdok</c> cell 0 is nonzero under
    /// <see cref="PrecisionKind.Original"/> where the original always prints zero (BOOT.md,
    /// "## Setup plane", "`Dmin` rounds too; `Di`/`Dj` round too").
    /// </summary>
    private static double ToBinary32MetresStored(Length length)
    {
        return length.IsMicrometres
            ? Binary32.ToNearestRepresentable(Binary32.Multiply(length.AsWritten, 1e-6))
            : Binary32.ToNearestRepresentable(length.AsWritten);
    }
}
