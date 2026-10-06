using ILGPU;
using ILGPU.Runtime;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Random.Tests;

/// <summary>
/// L1 of the BOOT.md table: <c>Next</c> and <c>ForParticle</c> give bit-identical results on the host and on
/// the ILGPU CPU accelerator, running the same code (no second, scalar implementation; root BOOT.md,
/// Invariants: "One particle program").
/// </summary>
public class KernelEqualityTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public KernelEqualityTests(CpuHost host)
    {
        _host = host;
    }

    [Fact]
    public void NextMatchesOnHostAndTheCpuAccelerator()
    {
        const int drawsPerStream = 8;
        var seeds = new[]
        {
            OriginalSeeds.Streams.S1, OriginalSeeds.Streams.S2, OriginalSeeds.Streams.S3,
            OriginalSeeds.Streams.S4, OriginalSeeds.Streams.S5, OriginalSeeds.Streams.S6,
        };
        var count = seeds.Length * drawsPerStream;

        var lowIn = new ulong[count];
        var highIn = new ulong[count];
        var expectedLow = new ulong[count];
        var expectedHigh = new ulong[count];
        var expectedDraw = new double[count];

        var index = 0;
        foreach (var seed in seeds)
        {
            var state = seed;
            for (var i = 0; i < drawsPerStream; i++)
            {
                lowIn[index] = state.Low;
                highIn[index] = state.High;

                var stepped = state;
                var draw = Mcg128.Next(ref stepped);
                expectedLow[index] = stepped.Low;
                expectedHigh[index] = stepped.High;
                expectedDraw[index] = draw;

                state = stepped;
                index++;
            }
        }

        using var lowInBuffer = _host.Accelerator.Allocate1D(lowIn);
        using var highInBuffer = _host.Accelerator.Allocate1D(highIn);
        using var lowOutBuffer = _host.Accelerator.Allocate1D<ulong>(count);
        using var highOutBuffer = _host.Accelerator.Allocate1D<ulong>(count);
        using var drawBuffer = _host.Accelerator.Allocate1D<double>(count);

        var kernel = _host.Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>,
            ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<double, Stride1D.Dense>>(NextKernel.Run);
        kernel(count, lowInBuffer.View, highInBuffer.View, lowOutBuffer.View, highOutBuffer.View, drawBuffer.View);
        _host.Accelerator.Synchronize();

        var actualLow = lowOutBuffer.GetAsArray1D();
        var actualHigh = highOutBuffer.GetAsArray1D();
        var actualDraw = drawBuffer.GetAsArray1D();

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(expectedLow[i], actualLow[i]);
            Assert.Equal(expectedHigh[i], actualHigh[i]);
            Assert.Equal(BitConverter.DoubleToInt64Bits(expectedDraw[i]), BitConverter.DoubleToInt64Bits(actualDraw[i]));
        }
    }

    [Fact]
    public void ForParticleMatchesOnHostAndTheCpuAccelerator()
    {
        var seeds = new ulong[] { 0, 1, 7, (1UL << 20) - 1, 123456 };
        var ordinals = new ulong[] { 0, 1, 42, (1UL << 40) - 1, 987654321 };
        var count = seeds.Length;

        var expected = new StreamSet[count];
        for (var i = 0; i < count; i++)
        {
            expected[i] = OriginalSeeds.ForParticle(seeds[i], ordinals[i]);
        }

        using var seedBuffer = _host.Accelerator.Allocate1D(seeds);
        using var ordinalBuffer = _host.Accelerator.Allocate1D(ordinals);
        using var lowOutBuffer = _host.Accelerator.Allocate1D<ulong>(count * 6);
        using var highOutBuffer = _host.Accelerator.Allocate1D<ulong>(count * 6);

        var kernel = _host.Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>,
            ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>>(ForParticleKernel.Run);
        kernel(count, seedBuffer.View, ordinalBuffer.View, lowOutBuffer.View, highOutBuffer.View);
        _host.Accelerator.Synchronize();

        var actualLow = lowOutBuffer.GetAsArray1D();
        var actualHigh = highOutBuffer.GetAsArray1D();

        for (var i = 0; i < count; i++)
        {
            AssertStreamEqual(expected[i].S1, actualLow, actualHigh, i * 6 + 0);
            AssertStreamEqual(expected[i].S2, actualLow, actualHigh, i * 6 + 1);
            AssertStreamEqual(expected[i].S3, actualLow, actualHigh, i * 6 + 2);
            AssertStreamEqual(expected[i].S4, actualLow, actualHigh, i * 6 + 3);
            AssertStreamEqual(expected[i].S5, actualLow, actualHigh, i * 6 + 4);
            AssertStreamEqual(expected[i].S6, actualLow, actualHigh, i * 6 + 5);
        }
    }

    [Fact]
    public void ForBatchedParticleMatchesOnHostAndTheCpuAccelerator()
    {
        var seeds = new ulong[] { 0, 1, 7, (1UL << 20) - 1, 123456 };
        var ordinals = new ulong[] { 0, 1, 42, (1UL << 40) - 1, 987654321 };
        var count = seeds.Length;

        var expected = new StreamSet[count];
        for (var i = 0; i < count; i++)
        {
            expected[i] = OriginalSeeds.ForBatchedParticle(seeds[i], ordinals[i]);
        }

        using var seedBuffer = _host.Accelerator.Allocate1D(seeds);
        using var ordinalBuffer = _host.Accelerator.Allocate1D(ordinals);
        using var lowOutBuffer = _host.Accelerator.Allocate1D<ulong>(count * 6);
        using var highOutBuffer = _host.Accelerator.Allocate1D<ulong>(count * 6);

        var kernel = _host.Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>,
            ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>>(ForBatchedParticleKernel.Run);
        kernel(count, seedBuffer.View, ordinalBuffer.View, lowOutBuffer.View, highOutBuffer.View);
        _host.Accelerator.Synchronize();

        var actualLow = lowOutBuffer.GetAsArray1D();
        var actualHigh = highOutBuffer.GetAsArray1D();

        for (var i = 0; i < count; i++)
        {
            AssertStreamEqual(expected[i].S1, actualLow, actualHigh, i * 6 + 0);
            AssertStreamEqual(expected[i].S2, actualLow, actualHigh, i * 6 + 1);
            AssertStreamEqual(expected[i].S3, actualLow, actualHigh, i * 6 + 2);
            AssertStreamEqual(expected[i].S4, actualLow, actualHigh, i * 6 + 3);
            AssertStreamEqual(expected[i].S5, actualLow, actualHigh, i * 6 + 4);
            AssertStreamEqual(expected[i].S6, actualLow, actualHigh, i * 6 + 5);
        }
    }

    [Fact]
    public void IndependentForParticleMatchesOnHostAndTheCpuAccelerator()
    {
        var seeds = new ulong[] { 0, 1, 7, (1UL << 20) - 1, 123456 };
        var ordinals = new ulong[] { 0, 1, 42, (1UL << 40) - 1, 987654321 };
        var count = seeds.Length;

        var expected = new StreamSet[count];
        for (var i = 0; i < count; i++)
        {
            expected[i] = IndependentSeeds.ForParticle(seeds[i], ordinals[i]);
        }

        using var seedBuffer = _host.Accelerator.Allocate1D(seeds);
        using var ordinalBuffer = _host.Accelerator.Allocate1D(ordinals);
        using var lowOutBuffer = _host.Accelerator.Allocate1D<ulong>(count * 6);
        using var highOutBuffer = _host.Accelerator.Allocate1D<ulong>(count * 6);

        var kernel = _host.Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>,
            ArrayView1D<ulong, Stride1D.Dense>, ArrayView1D<ulong, Stride1D.Dense>>(IndependentForParticleKernel.Run);
        kernel(count, seedBuffer.View, ordinalBuffer.View, lowOutBuffer.View, highOutBuffer.View);
        _host.Accelerator.Synchronize();

        var actualLow = lowOutBuffer.GetAsArray1D();
        var actualHigh = highOutBuffer.GetAsArray1D();

        for (var i = 0; i < count; i++)
        {
            AssertStreamEqual(expected[i].S1, actualLow, actualHigh, i * 6 + 0);
            AssertStreamEqual(expected[i].S2, actualLow, actualHigh, i * 6 + 1);
            AssertStreamEqual(expected[i].S3, actualLow, actualHigh, i * 6 + 2);
            AssertStreamEqual(expected[i].S4, actualLow, actualHigh, i * 6 + 3);
            AssertStreamEqual(expected[i].S5, actualLow, actualHigh, i * 6 + 4);
            AssertStreamEqual(expected[i].S6, actualLow, actualHigh, i * 6 + 5);
        }
    }

    private static void AssertStreamEqual(Mcg128State expected, ulong[] actualLow, ulong[] actualHigh, int index)
    {
        Assert.Equal(expected.Low, actualLow[index]);
        Assert.Equal(expected.High, actualHigh[index]);
    }
}

/// <summary>The kernel entry point of <see cref="NextMatchesOnHostAndTheCpuAccelerator"/>: one <see cref="Mcg128.Next"/> step per lane.</summary>
internal static class NextKernel
{
    public static void Run(
        Index1D index,
        ArrayView1D<ulong, Stride1D.Dense> lowIn,
        ArrayView1D<ulong, Stride1D.Dense> highIn,
        ArrayView1D<ulong, Stride1D.Dense> lowOut,
        ArrayView1D<ulong, Stride1D.Dense> highOut,
        ArrayView1D<double, Stride1D.Dense> draws)
    {
        var state = new Mcg128State { Low = lowIn[index], High = highIn[index] };
        draws[index] = Mcg128.Next(ref state);
        lowOut[index] = state.Low;
        highOut[index] = state.High;
    }
}

/// <summary>The kernel entry point of <see cref="ForParticleMatchesOnHostAndTheCpuAccelerator"/>: one particle's six streams per lane.</summary>
internal static class ForParticleKernel
{
    public static void Run(
        Index1D index,
        ArrayView1D<ulong, Stride1D.Dense> seeds,
        ArrayView1D<ulong, Stride1D.Dense> ordinals,
        ArrayView1D<ulong, Stride1D.Dense> lowOut,
        ArrayView1D<ulong, Stride1D.Dense> highOut)
    {
        var streams = OriginalSeeds.ForParticle(seeds[index], ordinals[index]);
        var baseIndex = index * 6;

        lowOut[baseIndex + 0] = streams.S1.Low;
        highOut[baseIndex + 0] = streams.S1.High;
        lowOut[baseIndex + 1] = streams.S2.Low;
        highOut[baseIndex + 1] = streams.S2.High;
        lowOut[baseIndex + 2] = streams.S3.Low;
        highOut[baseIndex + 2] = streams.S3.High;
        lowOut[baseIndex + 3] = streams.S4.Low;
        highOut[baseIndex + 3] = streams.S4.High;
        lowOut[baseIndex + 4] = streams.S5.Low;
        highOut[baseIndex + 4] = streams.S5.High;
        lowOut[baseIndex + 5] = streams.S6.Low;
        highOut[baseIndex + 5] = streams.S6.High;
    }
}

/// <summary>The kernel entry point of <see cref="KernelEqualityTests.ForBatchedParticleMatchesOnHostAndTheCpuAccelerator"/>: one batched particle's six streams per lane.</summary>
internal static class ForBatchedParticleKernel
{
    public static void Run(
        Index1D index,
        ArrayView1D<ulong, Stride1D.Dense> seeds,
        ArrayView1D<ulong, Stride1D.Dense> ordinals,
        ArrayView1D<ulong, Stride1D.Dense> lowOut,
        ArrayView1D<ulong, Stride1D.Dense> highOut)
    {
        var streams = OriginalSeeds.ForBatchedParticle(seeds[index], ordinals[index]);
        var baseIndex = index * 6;

        lowOut[baseIndex + 0] = streams.S1.Low;
        highOut[baseIndex + 0] = streams.S1.High;
        lowOut[baseIndex + 1] = streams.S2.Low;
        highOut[baseIndex + 1] = streams.S2.High;
        lowOut[baseIndex + 2] = streams.S3.Low;
        highOut[baseIndex + 2] = streams.S3.High;
        lowOut[baseIndex + 3] = streams.S4.Low;
        highOut[baseIndex + 3] = streams.S4.High;
        lowOut[baseIndex + 4] = streams.S5.Low;
        highOut[baseIndex + 4] = streams.S5.High;
        lowOut[baseIndex + 5] = streams.S6.Low;
        highOut[baseIndex + 5] = streams.S6.High;
    }
}

/// <summary>The kernel entry point of <see cref="IndependentForParticleMatchesOnHostAndTheCpuAccelerator"/>: one particle's six Independent streams per lane.</summary>
internal static class IndependentForParticleKernel
{
    public static void Run(
        Index1D index,
        ArrayView1D<ulong, Stride1D.Dense> seeds,
        ArrayView1D<ulong, Stride1D.Dense> ordinals,
        ArrayView1D<ulong, Stride1D.Dense> lowOut,
        ArrayView1D<ulong, Stride1D.Dense> highOut)
    {
        var streams = IndependentSeeds.ForParticle(seeds[index], ordinals[index]);
        var baseIndex = index * 6;

        lowOut[baseIndex + 0] = streams.S1.Low;
        highOut[baseIndex + 0] = streams.S1.High;
        lowOut[baseIndex + 1] = streams.S2.Low;
        highOut[baseIndex + 1] = streams.S2.High;
        lowOut[baseIndex + 2] = streams.S3.Low;
        highOut[baseIndex + 2] = streams.S3.High;
        lowOut[baseIndex + 3] = streams.S4.Low;
        highOut[baseIndex + 3] = streams.S4.High;
        lowOut[baseIndex + 4] = streams.S5.Low;
        highOut[baseIndex + 4] = streams.S5.High;
        lowOut[baseIndex + 5] = streams.S6.Low;
        highOut[baseIndex + 5] = streams.S6.High;
    }
}
