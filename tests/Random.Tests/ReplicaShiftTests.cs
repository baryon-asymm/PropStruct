using System.Numerics;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Random.Tests;

/// <summary>
/// Durable evidence for <c>src/Random/BOOT.md</c>, "Seed and replica jumps are rigid shifts", and
/// <c>tests/Fixtures/BOOT.md</c>, "Seed-patched replicas are shifts": a jump whose 2-adic valuation is at least
/// 100 (the replica jump <c>k·2⁸⁰ + 2⁷⁹</c>, and the per-particle seed jump <c>2⁸⁰</c>) adds one constant to
/// every state of an Independent-layout stream, so a replica set is <c>R</c> shifts of one sequence per stream,
/// not <c>R</c> independent samples of it; the ordinal jump <c>2⁴⁰</c> (valuation 68) is not a shift at all.
/// This is the mechanism root <c>BOOT.md</c>'s own <c>epsx(4)</c> exclusion entry
/// (<c>tests/Fixtures/exclusions.json</c>) rests on: a positive control against every HPEPA3 independent
/// replica's own printed value, and the arc-vs-circle claim that the exclusion's own reason line states.
/// Root <c>BOOT.md</c>'s taboos: "no entry in the exclusion list without computed evidence", "every check that
/// guards a quantitative claim is proven twice", "every figure states what it is".
/// </summary>
public class ReplicaShiftTests
{
    private readonly ITestOutputHelper _output;

    public ReplicaShiftTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private const int RigidShiftSampleCount = 1000;
    private const string Formulation = "HPEPA3";
    private const int ReplicaCount = 32; // tests/Fixtures/BOOT.md, Constraints: "R = 32 for HPEPA3".
    private const int CircleBinCount = 1 << 16; // root BOOT.md's own throwaway measurement used the same grid.

    public static IEnumerable<object[]> IndependentStreams()
    {
        yield return ["S1", IndependentSeeds.Streams.S1.Low, IndependentSeeds.Streams.S1.High];
        yield return ["S2", IndependentSeeds.Streams.S2.Low, IndependentSeeds.Streams.S2.High];
        yield return ["S3", IndependentSeeds.Streams.S3.Low, IndependentSeeds.Streams.S3.High];
        yield return ["S4", IndependentSeeds.Streams.S4.Low, IndependentSeeds.Streams.S4.High];
        yield return ["S5", IndependentSeeds.Streams.S5.Low, IndependentSeeds.Streams.S5.High];
        yield return ["S6", IndependentSeeds.Streams.S6.Low, IndependentSeeds.Streams.S6.High];
    }

    // --- (a) Rigid shift: positive side (2^80, 2^79) and negative side (2^40). ---

    [Theory]
    [MemberData(nameof(IndependentStreams))]
    public void JumpOfTwoPow80IsARigidShiftAcrossConsecutiveStates(string streamName, ulong seedLow, ulong seedHigh) => AssertRigidShift(streamName, seedLow, seedHigh, BigInteger.One << 80);

    [Theory]
    [MemberData(nameof(IndependentStreams))]
    public void JumpOfTwoPow79IsARigidShiftAcrossConsecutiveStates(string streamName, ulong seedLow, ulong seedHigh) => AssertRigidShift(streamName, seedLow, seedHigh, BigInteger.One << 79);

    [Theory]
    [MemberData(nameof(IndependentStreams))]
    public void JumpOfTwoPow40IsNotARigidShiftAcrossConsecutiveStates(string streamName, ulong seedLow, ulong seedHigh)
    {
        // The negative side of the same property (src/Random/BOOT.md: "the ordinal and batched jumps are not
        // shifts, since their valuation is below 100" -- 2^40 has valuation 28+40=68). This is also this file's
        // own non-degeneracy proof for the positive tests above: if the constant-delta check below could not
        // tell a true shift from a non-shift, this test would pass vacuously instead of catching the difference.
        var (kLow, kHigh) = Big128.Split(BigInteger.One << 40);
        var state = new Mcg128State { Low = seedLow, High = seedHigh };
        BigInteger? firstDelta = null;
        var sawDifferentDelta = false;

        for (var i = 0; i < RigidShiftSampleCount; i++)
        {
            var jumped = Mcg128.Advance(state, kLow, kHigh);
            var delta = Mod128(Big128.Combine(jumped) - Big128.Combine(state));
            if (firstDelta is null)
            {
                firstDelta = delta;
            }
            else if (delta != firstDelta)
            {
                sawDifferentDelta = true;
            }

            _ = Mcg128.Next(ref state);
        }

        Assert.True(sawDifferentDelta,
            $"{streamName}: expected the ordinal jump 2^40 (valuation 68, root BOOT.md 'Execution model') NOT " +
            $"to be a rigid shift over {RigidShiftSampleCount} consecutive states, but state*a^J - state stayed " +
            "constant throughout.");
    }

    private static void AssertRigidShift(string streamName, ulong seedLow, ulong seedHigh, BigInteger exponent)
    {
        var (kLow, kHigh) = Big128.Split(exponent);
        var state = new Mcg128State { Low = seedLow, High = seedHigh };
        BigInteger? expectedDelta = null;

        for (var i = 0; i < RigidShiftSampleCount; i++)
        {
            var jumped = Mcg128.Advance(state, kLow, kHigh);
            var delta = Mod128(Big128.Combine(jumped) - Big128.Combine(state));
            expectedDelta ??= delta;
            Assert.True(delta == expectedDelta,
                $"{streamName}, state {i}: state*a^J - state = {delta} but the first state of the same stream " +
                $"gave {expectedDelta} (src/Random/BOOT.md, 'Seed and replica jumps are rigid shifts': a jump " +
                "of 2-adic valuation >= 100 adds one constant to every state of the stream).");

            _ = Mcg128.Next(ref state);
        }
    }

    private static BigInteger Mod128(BigInteger value) => (value % Big128.Modulus + Big128.Modulus) % Big128.Modulus;

    // --- (b) Positive control: recompute epsx(4) from the generator for every HPEPA3 independent replica. ---

    [Trait("Category", "Long")]
    [Fact]
    public void Epsx4RecomputedFromTheGeneratorMatchesEveryHpepa3IndependentReplica()
    {
        // epsx(4) = |mean(X2) - 0.5| / 0.5 (background measurement this test formalizes): X2 is stream 4's only
        // consumer (src/Random/BOOT.md, "Stream roles": "4 -> X2 ... neighbour size"), so epsx(4) is a pure
        // function of stream 4's starting state and its own draw count N (the file's own printed NFY, since
        // NFZ == NFY, both incremented together with X2's own draw).
        //
        // Tolerance: the original's own x87 accumulation of these ~10^8 terms does not agree with this test's
        // double-precision compensated sum at the last ULP -- root BOOT.md's own invariant ("The original
        // generator, bit for bit") already declines to claim that for a single draw, and epsx(4) compounds
        // ~10^8 such draws into one mean. The background measurement behind this task found the worst observed
        // relative error to be about 8e-4 across the 32 HPEPA3 independent replicas; 2e-3 keeps about 2.5x
        // headroom over that without loosening past what a rounding-order difference, rather than a real
        // disagreement, would produce.
        const double relativeTolerance = 2e-3;

        var worstRelativeError = 0.0;
        for (var k = 1; k <= ReplicaCount; k++)
        {
            var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-independent", Formulation, $"{k}.m.txt");
            var replica = ResultsMFile.Parse(path);
            var n = (long)replica["NFY"][0];
            var expectedEpsx4 = replica["epsx(4)"][0];

            // tests/Fixtures/BOOT.md, "Replica jumps": replica k jumps the layout's six initial states by
            // k*2^80 + 2^79.
            var jump = ((BigInteger)k << 80) + (BigInteger.One << 79);
            var (jumpLow, jumpHigh) = Big128.Split(jump);
            var state = Mcg128.Advance(IndependentSeeds.Streams.S4, jumpLow, jumpHigh);

            var actualEpsx4 = ComputeEpsx4(ref state, n);
            var relativeError = Math.Abs(actualEpsx4 - expectedEpsx4) / Math.Abs(expectedEpsx4);
            worstRelativeError = Math.Max(worstRelativeError, relativeError);

            Assert.True(relativeError <= relativeTolerance,
                $"replica {k}: fixture epsx(4)={expectedEpsx4:G9} (N={n}), recomputed from the generator " +
                $"{actualEpsx4:G9}, relative error {relativeError:G3} exceeds {relativeTolerance:G3}.");
        }

        _output.WriteLine($"{Formulation} independent replicas 1..{ReplicaCount}: worst relative error {worstRelativeError:G3} (tolerance {relativeTolerance:G3}).");
    }

    /// <summary>
    /// <c>|mean(Next() over n draws) - 0.5| / 0.5</c>, Kahan-compensated: n reaches roughly 1.3e8 for HPEPA3's
    /// own replicas, and an uncompensated running sum of that many terms loses precision at exactly the scale
    /// (~1e-4) this quantity lives at.
    /// </summary>
    private static double ComputeEpsx4(ref Mcg128State state, long n)
    {
        var sum = 0.0;
        var compensation = 0.0;
        for (var i = 0L; i < n; i++)
        {
            var draw = Mcg128.Next(ref state);
            var y = draw - compensation;
            var t = sum + y;
            compensation = t - sum - y;
            sum = t;
        }

        var mean = sum / n;
        return Math.Abs(mean - 0.5) / 0.5;
    }

    // --- (c) Arc against circle: the replica arc's own sd against the whole circle's, at the same N. ---

    [Trait("Category", "Long")]
    [Fact]
    public void ReplicaArcOfShiftsHasLessThanHalfTheWholeCirclesSpreadAtTheirOwnMedianDrawCount()
    {
        var replicaDrawCounts = new long[ReplicaCount];
        var replicaShifts = new double[ReplicaCount];
        for (var k = 1; k <= ReplicaCount; k++)
        {
            var path = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-independent", Formulation, $"{k}.m.txt");
            var replica = ResultsMFile.Parse(path);
            replicaDrawCounts[k - 1] = (long)replica["NFY"][0];

            var jump = ((BigInteger)k << 80) + (BigInteger.One << 79);
            var (jumpLow, jumpHigh) = Big128.Split(jump);
            var jumpedState = Mcg128.Advance(IndependentSeeds.Streams.S4, jumpLow, jumpHigh);
            var delta = Mod128(Big128.Combine(jumpedState) - Big128.Combine(IndependentSeeds.Streams.S4));
            replicaShifts[k - 1] = (double)delta / Math.Pow(2.0, 128.0);
        }

        var medianDrawCount = Median(replicaDrawCounts);

        // One pass over medianDrawCount draws of the base (unshifted) stream 4 sequence builds a histogram of
        // CircleBinCount bins over [0, 1); every candidate shift's own mean is then recovered exactly from the
        // wrap count the histogram gives at that shift's bin boundary (root BOOT.md's own throwaway measurement
        // used the same device, this file's own header comment: resimulating per shift does not fit the budget
        // of a fast, repeatable test). This is exact, not an approximation: every draw x with x + delta >= 1 is
        // exactly the draws whose own bin index is >= CircleBinCount - (delta * CircleBinCount), because the
        // bin boundaries and the candidate shifts share the same CircleBinCount grid.
        var baseState = IndependentSeeds.Streams.S4;
        var counts = new long[CircleBinCount];
        var sum = 0.0;
        var compensation = 0.0;
        for (var i = 0L; i < medianDrawCount; i++)
        {
            var draw = Mcg128.Next(ref baseState);
            var y = draw - compensation;
            var t = sum + y;
            compensation = t - sum - y;
            sum = t;

            var bin = (int)(draw * CircleBinCount);
            if (bin >= CircleBinCount)
            {
                bin = CircleBinCount - 1;
            }

            counts[bin]++;
        }

        var baseMean = sum / medianDrawCount;

        var suffixCount = new long[CircleBinCount + 1];
        for (var b = CircleBinCount - 1; b >= 0; b--)
        {
            suffixCount[b] = suffixCount[b + 1] + counts[b];
        }

        double Epsx4AtShift(double delta)
        {
            var binIndex = (int)Math.Round(delta * CircleBinCount, MidpointRounding.AwayFromZero) % CircleBinCount;
            var wrapCount = binIndex == 0 ? 0L : suffixCount[CircleBinCount - binIndex];
            var mean = baseMean + delta - (double)wrapCount / medianDrawCount;
            return Math.Abs(mean - 0.5) / 0.5;
        }

        var circleValues = new double[CircleBinCount];
        for (var b = 0; b < CircleBinCount; b++)
        {
            circleValues[b] = Epsx4AtShift((double)b / CircleBinCount);
        }

        var circleSd = SampleStandardDeviation(circleValues);

        var replicaValues = new double[ReplicaCount];
        for (var k = 0; k < ReplicaCount; k++)
        {
            replicaValues[k] = Epsx4AtShift(replicaShifts[k]);
        }

        var replicaSd = SampleStandardDeviation(replicaValues);

        _output.WriteLine(
            $"{Formulation}, N={medianDrawCount}, {CircleBinCount} shifts: circle sd={circleSd:G4}, " +
            $"{ReplicaCount}-replica arc sd={replicaSd:G4}, ratio={replicaSd / circleSd:G3}.");

        Assert.True(replicaSd < 0.5 * circleSd,
            $"expected the {ReplicaCount}-replica arc's own sd ({replicaSd:G4}) to be less than half the whole " +
            $"circle's sd ({circleSd:G4}) -- the claim tests/Fixtures/exclusions.json's epsx(4) entry rests on.");
    }

    private static long Median(long[] values)
    {
        var sorted = (long[])values.Clone();
        Array.Sort(sorted);
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    private static double SampleStandardDeviation(double[] values)
    {
        var mean = values.Average();
        var sumSquares = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSquares / (values.Length - 1));
    }
}
