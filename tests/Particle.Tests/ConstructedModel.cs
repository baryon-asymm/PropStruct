using ILGPU;
using ILGPU.Runtime;
using PropStruct.Random;

namespace PropStruct.Particle.Tests;

/// <summary>
/// A minimal, single-fraction model setup for the constructed-input rows of
/// <c>BOOT.md</c>'s levels table: small enough to reason about by hand, never a
/// stand-in for a real formulation (BOOT.md, "Order of coding"). Every buffer this
/// builds is owned by the caller and must be disposed.
/// </summary>
internal static class ConstructedModel
{
    private static readonly double[] SingleFractionCumulative = [0.0, 1.0];

    /// <summary>
    /// One oxidizer fraction, uniform-in-D (<c>JZZ = 2</c>), sized <paramref name="lower"/>..<paramref name="upper"/>;
    /// <paramref name="pocketForming"/> is <c>SFR(1)</c>.
    /// </summary>
    public static FractionTable SingleFraction(
        Accelerator accelerator, double lower, double upper, byte pocketForming,
        out MemoryBuffer1D<double, Stride1D.Dense> bounds,
        out MemoryBuffer1D<double, Stride1D.Dense> cumulative,
        out MemoryBuffer1D<byte, Stride1D.Dense> forming)
    {
        bounds = accelerator.Allocate1D(new[] { lower, upper });
        cumulative = accelerator.Allocate1D(SingleFractionCumulative);
        forming = accelerator.Allocate1D(new[] { pocketForming });

        return new FractionTable
        {
            Bounds = bounds.View.BaseView,
            Cumulative = cumulative.View.BaseView,
            PocketForming = forming.View.BaseView,
        };
    }

    /// <summary>
    /// A permissive setup around one fraction: the AK1/AK2 gap test never itself
    /// rejects (AK1 = 0, AK2 huge), Dmin/Dmax never reject a draw up to
    /// <paramref name="upper"/>, and the array sizes comfortably
    /// cover indices up to <paramref name="upper"/> / <paramref name="cellSize"/>.
    /// </summary>
    public static ModelSetup Permissive(
        double upper, double cellSize, double lambda,
        double ak3, double ak4,
        int neighbourBudget = 1000, int pocketRedrawBudget = 1000,
        int arraySize = 64)
    {
        return new ModelSetup
        {
            FractionCount = 1,
            SizeLaw = 2,
            CellSize = cellSize,
            CategoryStep = cellSize,
            Dmin = 0.0,
            Dmax = upper * 10.0,
            Lambda = lambda,
            Ak1 = 0.0,
            Ak2 = 1e12,
            Ak3 = ak3,
            Ak4 = ak4,
            Variant = 1,
            Alpha = 0.25,
            PocketCoefficient = 1.0,
            BridgeCoefficient = 1.0,
            NnMin = 0.0,
            NnMax = 1e9,
            Ndok = arraySize,
            Nkarm = arraySize,
            Ncat = arraySize,
            Nc = 1000,
            NeighbourBudget = neighbourBudget,
            PocketRedrawBudget = pocketRedrawBudget,
            Layout = AccumulatorLayout.Create(1, arraySize, arraySize, arraySize, 1000),
        };
    }

    public static CycleInputs CycleOne(
        Accelerator accelerator, int nkarm, int ndok, double dmaxxx,
        out MemoryBuffer1D<double, Stride1D.Dense> qks1,
        out MemoryBuffer1D<double, Stride1D.Dense> pdoksmall)
    {
        var uniformQks1 = new double[nkarm];
        Array.Fill(uniformQks1, 1.0);
        qks1 = accelerator.Allocate1D(uniformQks1);
        pdoksmall = accelerator.Allocate1D<double>(ndok);
        pdoksmall.MemSetToZero();

        return new CycleInputs
        {
            CycleFlag = 1,
            Dmaxxx = dmaxxx,
            Qks1 = qks1.View.BaseView,
            Pdoksmall = pdoksmall.View.BaseView,
        };
    }

    public static CycleInputs CycleZero()
    {
        return new CycleInputs
        {
            CycleFlag = 0,
            Dmaxxx = 0.0,
            Qks1 = default,
            Pdoksmall = default,
        };
    }

    /// <summary>
    /// The geometry (<c>dr</c>, <c>db</c>, <c>rrL</c>, <c>AA</c>) a chosen seed and setup
    /// would produce through <see cref="Attempt.Run"/>'s own base-and-first-neighbour draw
    /// (Fortran lines 448-501, 503-539): calls the same <see cref="Mcg128.Next"/> and
    /// <see cref="SizeLaw.Sample"/> the attempt calls (2026-09-17: this used to call a
    /// private <c>SampleDirect</c>, a byte-for-byte copy of <see cref="SizeLaw.Sample"/>'s
    /// own arithmetic, forbidden by the root taboo "no second implementation of any part of
    /// the particle program" and by this node's own "drivers call only the Particle surface"
    /// constraint — any drift between the copy and the real method would have stayed green
    /// here forever; removed, this peek now allocates its own tiny accelerator buffers and
    /// calls <see cref="SizeLaw.Sample"/> itself), so a test can choose <c>AK3</c>/<c>AK4</c>
    /// around this seed's own draws instead of guessing them. The <c>KCIL</c>/<c>rrL</c>
    /// arithmetic just below is still repeated here, and stays repeated: unlike the size
    /// draw, it has no separately callable production unit — it lives inline inside
    /// <see cref="Attempt.Run"/>'s <c>Label501</c> block, between two calls to
    /// <see cref="SizeLaw.Sample"/> that this peek must also straddle, so there is nothing
    /// else to call for it (the constructed-input analogue of <c>tests/Random.Tests</c>'
    /// <c>BigInteger</c> oracle for the generator: a genuine oracle, not a shortcut).
    /// </summary>
    public static (double Dr, double Db, double Rrl, double Aa, double MaxDrDb) PeekFirstNeighbour(
        Accelerator accelerator, ModelSetup setup, double[] bounds, double[] cumulative, StreamSet streams)
    {
        using var boundsBuffer = accelerator.Allocate1D(bounds);
        using var cumulativeBuffer = accelerator.Allocate1D(cumulative);
        var boundsView = boundsBuffer.View.BaseView;
        var cumulativeView = cumulativeBuffer.View.BaseView;

        var x = Mcg128.Next(ref streams.S1);
        var x0 = Mcg128.Next(ref streams.S2);
        SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, boundsView, cumulativeView, x, x0, out var dr, out _);

        double x1;
        do
        {
            x1 = Mcg128.Next(ref streams.S3);
        } while (x1 == 1.0);

        var vp = 3.14159 / 6.0 * dr * dr * dr;
        var kcil = -(Math.Log(1.0 - x1) * (1.0 / setup.Lambda)) + vp;
        var rrl = Math.Pow(3.0 * kcil / (4.0 * 3.14159), 1.0 / 3.0);

        var x2 = Mcg128.Next(ref streams.S4);
        var x21 = Mcg128.Next(ref streams.S5);
        SizeLaw.Sample(setup.SizeLaw, setup.FractionCount, boundsView, cumulativeView, x2, x21, out var db, out _);

        var aa = rrl - dr / 2.0 - db / 2.0;
        var maxDrDb = Math.Max(dr, db);

        return (dr, db, rrl, aa, maxDrDb);
    }
}
