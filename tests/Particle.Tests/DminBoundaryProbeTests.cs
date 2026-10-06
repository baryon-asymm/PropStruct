using System.Globalization;
using System.Text.RegularExpressions;
using ILGPU.Runtime;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Particle.Tests;

/// <summary>
/// Confirms the mechanism <see cref="DminBoundaryProbe"/> targets: the original's
/// implicit-REAL*4 <c>Dr</c>/<c>Db</c> can round down to <c>Dmin</c>'s own binary32 value
/// for a draw whose true (double) size sits a hair above it, firing
/// <c>Dr.le.Dmin</c>/<c>Db.le.Dmin</c> (Fortran 478/533) where the port's <c>double</c>
/// comparison does not. Two independent legs, both against fixture data never typed by
/// hand (root BOOT.md, Taboos):
///
/// <list type="bullet">
/// <item><b>Threshold leg</b> (fast, exact, no RNG): <see
/// cref="DminBoundaryProbe.FindHitThreshold"/> finds the precise <c>x1</c> crossing point
/// by bisecting the published <see cref="SizeLaw.Sample"/> itself. Combined with a fast
/// reference-mode sample's own measured draws-per-accepted-particle, this projects a rate
/// with no Monte Carlo noise in the threshold itself, only in the draws-per-particle
/// estimate.</item>
/// <item><b>Replay leg</b> (slow, <c>Category=Long</c>): <see cref="DminBoundaryProbe.Run"/>
/// replays real reference-mode attempts through the actual stream/attempt machinery and
/// counts observed hits directly -- the literal probe, not a projection.</item>
/// </list>
///
/// <c>PSAN02n</c> and <c>inpt</c> are not probed here beyond the threshold leg's own
/// degenerate-threshold check: their lowest fraction bound (160/257 &#181;m) is nowhere
/// near <c>Dmin</c> (10 &#181;m), so the coincidence this mechanism needs does not exist
/// for them at all -- confirmed structurally (<see cref="DminBoundaryProbe.FindHitThreshold"/>
/// reports <c>monotonic = false</c>, threshold 0), not by absence of observed events.
/// </summary>
public class DminBoundaryProbeTests
{
    private readonly ITestOutputHelper _output;

    public DminBoundaryProbeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Reads every lagged replica's own printed "Dok &lt; Dmin" rate for
    /// <paramref name="formulation"/> (root BOOT.md, "Known bias of the original's
    /// seeds": lagged replicas keep the <c>Original</c> layout's own lag structure, the
    /// layout reference mode reproduces) straight out of <c>tests/Fixtures</c>, never
    /// typed into this file. Returns the individual values, not just their mean, so a
    /// caller can read the between-replica spread rather than a single number.
    /// </summary>
    private static double[] ReadOriginalLaggedRates(string formulation)
    {
        var directory = RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation);
        var pattern = new Regex(@"Dok < Dmin\s*:\s*([-\d.]+E[+-]?\d+)", RegexOptions.IgnoreCase);
        var rates = new List<double>();
        foreach (var file in Directory.GetFiles(directory, "*.m.txt").OrderBy(f => f, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);
            var match = pattern.Match(text);
            if (!match.Success)
            {
                throw new InvalidOperationException($"{file}: no \"Dok < Dmin\" line found.");
            }

            rates.Add(double.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture));
        }

        if (rates.Count == 0)
        {
            throw new InvalidOperationException($"{directory}: no replica files found.");
        }

        return rates.ToArray();
    }

    /// <summary>
    /// The threshold leg is degenerate (no coincidence at all) for the two formulations
    /// whose lowest fraction bound is far from <c>Dmin</c>, and non-degenerate,
    /// non-trivial (strictly between 0 and 1) for the three whose lowest bound equals it
    /// -- established structurally from the setup alone, no reference-mode run needed.
    /// </summary>
    [Theory]
    [InlineData("HMX", true)]
    [InlineData("HPEPA3", true)]
    [InlineData("P33", true)]
    [InlineData("PSAN02n", false)]
    [InlineData("inpt", false)]
    public void ThresholdIsNonDegenerateOnlyWhenTheLowestFractionBoundEqualsDmin(string formulation, bool expectCoincidence)
    {
        using var host = new CpuHost();
        var setup = ReferenceFormulation.Prepare(host.Accelerator, formulation, out var tables, out _);
        using var boundsBuffer = host.Accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = host.Accelerator.Allocate1D(tables.Cumulative);
        var fractions = new FractionTable
        {
            Bounds = boundsBuffer.View.BaseView,
            Cumulative = cumulativeBuffer.View.BaseView,
        };

        var xInFraction0 = (tables.Cumulative[0] + tables.Cumulative[1]) / 2.0;
        var threshold = DminBoundaryProbe.FindHitThreshold(setup, fractions, 0, xInFraction0, out var monotonic);

        _output.WriteLine($"{formulation}: sizeLaw={setup.SizeLaw} threshold={threshold:E6} monotonic={monotonic}");

        Assert.Equal(expectCoincidence, monotonic);
        if (expectCoincidence)
        {
            Assert.InRange(threshold, double.Epsilon, 1e-3); // non-zero, and nowhere near a whole fraction.
        }
        else
        {
            Assert.Equal(0.0, threshold);
        }
    }

    /// <summary>
    /// <c>SizeLaw</c>'s two laws give an 8x-different threshold at the same nominal
    /// Dmin/lower-bound coincidence (HMX/HPEPA3, <c>sizeLaw == 1</c>, uniform in 1/D²,
    /// against P33, <c>sizeLaw == 2</c>, uniform in D) -- the quantitative reason P33's
    /// own rate is far smaller than HMX's/HPEPA3's rather than merely "also zero because
    /// JZ differs". The ratio is not asserted to a specific constant (that would be the
    /// taboo's "expected value typed into a test"): both thresholds come from the same
    /// bisection this class already trusts, and the assertion is the qualitative claim
    /// the size laws' own derivatives predict -- <c>sizeLaw == 1</c>'s threshold is
    /// larger, not by how much.
    /// </summary>
    [Fact]
    public void ReciprocalSquareLawHasAWiderThresholdThanTheLinearLawAtTheSameBound()
    {
        using var host = new CpuHost();

        double ThresholdOf(string formulation)
        {
            var setup = ReferenceFormulation.Prepare(host.Accelerator, formulation, out var tables, out _);
            using var boundsBuffer = host.Accelerator.Allocate1D(tables.Bounds);
            using var cumulativeBuffer = host.Accelerator.Allocate1D(tables.Cumulative);
            var fractions = new FractionTable
            {
                Bounds = boundsBuffer.View.BaseView,
                Cumulative = cumulativeBuffer.View.BaseView,
            };
            var xInFraction0 = (tables.Cumulative[0] + tables.Cumulative[1]) / 2.0;
            return DminBoundaryProbe.FindHitThreshold(setup, fractions, 0, xInFraction0, out _);
        }

        var hmx = ThresholdOf("HMX");
        var hpepa3 = ThresholdOf("HPEPA3");
        var p33 = ThresholdOf("P33");

        _output.WriteLine($"HMX threshold={hmx:E6} HPEPA3 threshold={hpepa3:E6} P33 threshold={p33:E6}");

        Assert.True(hmx > p33 * 2, $"HMX threshold {hmx:E6} expected well above P33's {p33:E6}.");
        Assert.True(hpepa3 > p33 * 2, $"HPEPA3 threshold {hpepa3:E6} expected well above P33's {p33:E6}.");
    }

    /// <summary>
    /// Combines the exact threshold with a fast reference-mode sample's own measured
    /// draws-per-accepted-particle (base draws reaching (478) plus neighbour draws
    /// reaching (533), both already gated on <c>SFR(Nfract) != 0</c> exactly as
    /// <c>Attempt.cs</c> is) to project a rate, and compares it with the original's own
    /// measured lagged-replica rates, read from the fixture files above, never typed in.
    /// The projection is expected to <em>undershoot</em>: this class's own draws-per-
    /// particle sample is a few orders of magnitude short of the original's real NFY/NFZ
    /// (root BOOT.md's own diagnosis, echoed by <see cref="AccumulatorSweep"/>'s and
    /// <c>RealFourAccumulationErrorTests</c>' remarks), and the final-store rounding this
    /// class measures may not be the mechanism's only contribution -- the original's own
    /// <c>SIZE</c> subroutine (Fortran 1761-1776) also rounds its own intermediate
    /// reciprocal-square terms to REAL*4 before the final store, for <c>sizeLaw != 2</c>,
    /// which this class does not model (it measures the drawn size's own final-store
    /// rounding only, per this task's own instruction). So the assertion is a floor, not
    /// an equality: the projected rate must be within the same order of magnitude as the
    /// original's own mean, never zero and never wildly larger.
    /// </summary>
    [Theory]
    [Trait("Category", "Long")]
    [InlineData("HMX")]
    [InlineData("HPEPA3")]
    public void ProjectedRateIsTheSameOrderOfMagnitudeAsTheOriginalsMeasuredRate(string formulation)
    {
        using var host = new CpuHost();
        var setup = ReferenceFormulation.Prepare(host.Accelerator, formulation, out var tables, out _);
        using var boundsBuffer = host.Accelerator.Allocate1D(tables.Bounds);
        using var cumulativeBuffer = host.Accelerator.Allocate1D(tables.Cumulative);
        var fractions = new FractionTable
        {
            Bounds = boundsBuffer.View.BaseView,
            Cumulative = cumulativeBuffer.View.BaseView,
        };
        var xInFraction0 = (tables.Cumulative[0] + tables.Cumulative[1]) / 2.0;
        var threshold = DminBoundaryProbe.FindHitThreshold(setup, fractions, 0, xInFraction0, out var monotonic);
        Assert.True(monotonic, $"{formulation}: expected a non-degenerate boundary coincidence at fraction 0.");

        var share0 = tables.Cumulative[1] - tables.Cumulative[0];
        var perDrawProbability = share0 * threshold;

        const int attempts = 200_000;
        var sample = DminBoundaryProbe.Run(host.Accelerator, formulation, seed: 0, attempts);
        Assert.True(sample.AcceptedParticles > 0, $"{formulation}: no accepted particles in the sample.");

        var drawsPerAcceptedParticle = (double)(sample.BaseTested + sample.NeighbourTested) / sample.AcceptedParticles;
        var projectedRate = drawsPerAcceptedParticle * perDrawProbability;

        var originalRates = ReadOriginalLaggedRates(formulation);
        var originalMean = originalRates.Average();

        _output.WriteLine($"{formulation}: threshold={threshold:E6} share0={share0:G6} " +
            $"drawsPerAcceptedParticle={drawsPerAcceptedParticle:F1} projectedRate={projectedRate:E6} " +
            $"originalMean={originalMean:E6} ({originalRates.Length} lagged replicas) " +
            $"ratio(original/projected)={originalMean / projectedRate:F2}");

        Assert.True(projectedRate > 0.0, $"{formulation}: projected rate is exactly 0.");
        Assert.InRange(projectedRate, originalMean / 20.0, originalMean * 5.0);
    }

    /// <summary>
    /// The literal probe: real reference-mode attempts, real streams, real <see
    /// cref="Attempt.Run"/> calls, counting the observed hits directly, no projection.
    /// The attempt counts are chosen from <see
    /// cref="ProjectedRateIsTheSameOrderOfMagnitudeAsTheOriginalsMeasuredRate"/>'s own
    /// projected rates, scaled so a handful of actual hits is plausible for HMX/HPEPA3
    /// and P33's own projection stays under one expected event, matching its reference
    /// and all sixteen lagged replicas printing exactly 0 (root BOOT.md's own known-bias
    /// table). Measured 2026-09-23 at exactly these counts, seed 0 (~5.5 minutes total):
    /// HMX 3/2,000,000 attempts (rate 3.72e-4, against a lagged-replica mean of 1.19e-3,
    /// 3.2x below), HPEPA3 4/1,200,000 (2.21e-5, against 5.47e-5, 2.5x below), P33
    /// 0/500,000. The undershoot is expected and explained in this node's BOOT.md
    /// ("Decisions where the port departs from a transcription", the Dmin boundary
    /// entry): this class measures only the drawn size's own final-store rounding, not
    /// the reciprocal-square law's own intermediate REAL*4 terms inside the original's
    /// <c>SIZE</c> subroutine, which this task's own method does not ask for.
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void ReplayObservesHitsOnHmxAndHpepa3AndNoneOnP33()
    {
        using var host = new CpuHost();

        var hmx = DminBoundaryProbe.Run(host.Accelerator, "HMX", seed: 0, attempts: 2_000_000);
        var hpepa3 = DminBoundaryProbe.Run(host.Accelerator, "HPEPA3", seed: 0, attempts: 1_200_000);
        var p33 = DminBoundaryProbe.Run(host.Accelerator, "P33", seed: 0, attempts: 500_000);

        _output.WriteLine($"HMX: attempts={hmx.Attempts} accepted={hmx.AcceptedParticles} " +
            $"totalHits={hmx.TotalProbeHits} rate={hmx.ProbeRate:E3} totalDoubleHits={hmx.TotalDoubleHits}");
        _output.WriteLine($"HPEPA3: attempts={hpepa3.Attempts} accepted={hpepa3.AcceptedParticles} " +
            $"totalHits={hpepa3.TotalProbeHits} rate={hpepa3.ProbeRate:E3} totalDoubleHits={hpepa3.TotalDoubleHits}");
        _output.WriteLine($"P33: attempts={p33.Attempts} accepted={p33.AcceptedParticles} " +
            $"totalHits={p33.TotalProbeHits} rate={p33.ProbeRate:E3} totalDoubleHits={p33.TotalDoubleHits}");

        // The port's own double comparison never fires (`src/Particle/BOOT.md`, the Dmin boundary measurement above).
        Assert.Equal(0, hmx.TotalDoubleHits);
        Assert.Equal(0, hpepa3.TotalDoubleHits);
        Assert.Equal(0, p33.TotalDoubleHits);

        Assert.True(hmx.TotalProbeHits > 0, "expected at least one binary32 boundary hit on HMX at this sample size.");
        Assert.True(hpepa3.TotalProbeHits > 0, "expected at least one binary32 boundary hit on HPEPA3 at this sample size.");
        Assert.True(
            hmx.ProbeRate > p33.ProbeRate * 5 || p33.TotalProbeHits == 0,
            $"expected HMX's rate ({hmx.ProbeRate:E3}) well above P33's ({p33.ProbeRate:E3}), or P33 to show no hits at all.");
    }
}
