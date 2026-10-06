using System.Collections.Immutable;
using PropStruct.Execution;
using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// L1 of the BOOT.md table: JSON round-trips bit for bit. Written, read back, written again gives the same
/// bytes, and the records compare equal field by field with bitwise <c>double</c> equality (this node's
/// acceptance criteria and <c>API.md</c>'s "Round trip" definition).
/// </summary>
public class JsonRoundTripTests
{
    // Internal, not private: PrecisionFooterLineTests reuses this one sample builder (with only
    // Diagnostics.Precision varied) rather than a second hand-built SimulationResult literal.
    internal static SimulationResult BuildSample()
    {
        var header = new RunHeader(
            "HPEPA3", Cycles: 1, ParticlesPerCycle: 100000,
            Dokm: 130.68e-6, Doksd: 1.18e-8, Ddokmax: 315e-6, Dmax: 315e-6,
            TailProbabilityModified: 0.0, Ggg: 0.79, Zx: ImmutableArray.Create(0.1, 0.9));

        var counters = new Counters(
            Qkss: 24649481L, Nfx: 1336594L, Nfy: 117621203L, Nfz: 12345L, Nfq: 1292687L, Nfw: 13727046L,
            Conditions: ImmutableArray.Create(0L, 55L, 1135342L, 89277020L, 0L, 0L, 1211L, 41L, 213L),
            IbridgeTotal: 436319L, JammedTotal: 2236.2478, NnTotal: 1787240.0);

        var accuracy = new GeneratorAccuracy(
            Eps1: 3.09e-4, Eps2: 3.10e-4, Eps3: 5.87e-5, Eps4: 5.83e-5, Eps5: 1.0e-3, Eps6: 1.64e-3, Eps7: 3.45e-5,
            Epsx1: 5.37e-4, Epsx2: 3.01e-3, Epsx3: 2.98e-3, Epsalldok: ImmutableArray.Create(8.06e-7, 1.17e-3),
            Epsy: 2.28e-2, Epsmd4: 2.05e-2, Epsmd3: 9.97e-3, OxidizerAccuracyWarning: false, PocketAccuracyWarning: true);

        var sizes = new ParticleSizes(Dok43b: 130.75e-6, Dok43s: 131.07e-6, Alldok43: 131.07e-6, Alldok432: 138.12e-6);
        var local = new LocalStructure(Gdokleft: 0.01, Vdokleft: 0.02, Plotsmdok: 0.03, Plotsm: 0.04, Mp: 0.7389501);

        var pockets = new Pockets(
            Dp43: 230.44e-6, D432: 230.42e-6, Sdevp43: 188.61e-6, Dqkarm: 33.34e-6,
            Dkarm43Cor: 230.41e-6, Sdevp43Cor: 187.02e-6, DqkarmCor: 33.32e-6, Dfmk432: 96.48e-6, Sdevp243: 113.88e-6,
            DpMax: 660e-6, DpMaxCor: 660e-6,
            DpRow: 2, Dpockets: ImmutableArray.Create(10e-6, 20e-6),
            Dokp43: ImmutableArray.Create(21.4e-6, 30.3e-6), Qdokkarm: ImmutableArray.Create(17.6e-6, 20.8e-6),
            Qdokso: ImmutableArray.Create(ImmutableArray.Create(0.0, 0.076), ImmutableArray.Create(0.0, 0.057)));

        var mass = new MassFractions(DolM1: 0.4061192, DolM2: 0.4061192, DolM3: 0.4312955);
        var agglomerates = new Agglomerates(Dqmkm1: 0.1492, Dqmkm2: 0.6311, Qmcoef: 2.156227, CoefNmax: 400, Qmkm1Nmax: 300, Qmkm2Nmax: 100);

        var histograms = new Histograms(
            Allvdokso: ImmutableArray.Create(0.0, 0.736e-2, 0.139e-1),
            Vkso: ImmutableArray.Create(0.268e-4, 0.566e-3),
            Qks1: ImmutableArray.Create(0.786e-2, 0.212e-1),
            FmkarmCorNormalized: ImmutableArray.Create(0.267e-4, 0.564e-3),
            Fmkarm2Normalized: ImmutableArray.Create(0.847e-5, 0.335e-3),
            FqkarmCorNormalized: ImmutableArray.Create(0.787e-2, 0.213e-1),
            Qmkm1Normalized: ImmutableArray.Create(0.258e-2, 0.261e-2),
            Qmkm2Normalized: ImmutableArray.Create(0.356e-4, 0.364e-4),
            CoefNormalized: ImmutableArray.Create(0.0, 0.0),
            Pdoksmall: ImmutableArray.Create(0.0, 0.141, double.NaN));

        var convergence = new Convergence(
            ConvergenceEpsy: ImmutableArray.Create(1.0, 2.0),
            ConvergenceEpsmd3: ImmutableArray.Create(3.0, 4.0),
            ConvergenceEpsmd4: ImmutableArray.Create(5.0, 6.0),
            ConvergenceAlldok43: ImmutableArray.Create(7.0, 8.0),
            ConvergenceAlldoksd: ImmutableArray.Create(double.PositiveInfinity, double.NegativeInfinity),
            ConvergenceDolM2: ImmutableArray.Create(9.0, 10.0));

        var diagnostics = new RunDiagnostics(
            Mode: ExecutionMode.Batched, BatchSize: 100000, AttemptsPerLaunch: 7,
            Accelerator: new AcceleratorInfo(AcceleratorKind.Cpu, "CPU", MemoryBytes: 0L, LibDeviceLinked: false, CudaSkippedBecause: null),
            Streams: StreamLayout.Original, Precision: PrecisionKind.Binary64, Seed: 0UL, ContinuedStreams: false,
            TotalAttempts: 1336594L, TotalLaunches: 3L, Elapsed: TimeSpan.FromSeconds(18));

        var storedSetup = new StoredSetup(
            CellSize: 10e-6, CategoryStep: 10e-6, Dmin: 10e-6,
            OxidizerDensity: 1950.0, PropellantDensity: 1800.0, OxidizerMassFraction: 0.79, MetalMassFraction: 0.05,
            Alpha: 0.25, NnMin: 3.0, NnMax: 100.0, PocketCoefficient: 8.2, BridgeCoefficient: 7.73,
            HomogenizedOxidizerFraction: 0.0, EpsDok: 0.05,
            FractionMassShares: ImmutableArray.Create(0.1, 0.9), FractionBounds: ImmutableArray.Create(160e-6, 315e-6, 315e-6, 630e-6));

        return new SimulationResult(
            header, counters, accuracy, sizes, local, pockets, mass, agglomerates, histograms, convergence, diagnostics, storedSetup);
    }

    [Fact]
    public void WriteThenReadGivesAnEqualResultFieldByFieldWithBitwiseDoubleEquality()
    {
        var original = BuildSample();
        var path = Path.GetTempFileName();
        try
        {
            ResultsJson.Write(original, path);
            var roundTripped = ResultsJson.Read(path);

            AssertBitwiseEqual(original, roundTripped);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void WriteReadWriteGivesTheSameBytes()
    {
        var original = BuildSample();
        var path1 = Path.GetTempFileName();
        var path2 = Path.GetTempFileName();
        try
        {
            ResultsJson.Write(original, path1);
            var roundTripped = ResultsJson.Read(path1);
            ResultsJson.Write(roundTripped, path2);

            var bytes1 = File.ReadAllBytes(path1);
            var bytes2 = File.ReadAllBytes(path2);
            Assert.Equal(bytes1, bytes2);
        }
        finally
        {
            File.Delete(path1);
            File.Delete(path2);
        }
    }

    [Fact]
    public void ReadMalformedJsonThrowsJsonException()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ not valid json ");
            _ = Assert.Throws<System.Text.Json.JsonException>(() => ResultsJson.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Every field of every record, compared with bitwise <c>double</c> equality (<see cref="BitConverter.DoubleToInt64Bits"/>,
    /// so that <c>NaN</c> and the infinities compare equal to themselves and a rounding difference of the
    /// last bit is not silently accepted) - the record types have no <c>Equals</c> override strict enough for
    /// this on their own (immutable-array and matrix fields compare by reference otherwise).
    /// </summary>
    private static void AssertBitwiseEqual(SimulationResult expected, SimulationResult actual)
    {
        AssertEqualHeader(expected.Header, actual.Header);
        AssertEqualDoubles(nameof(Counters), [
            (expected.Counters.JammedTotal, actual.Counters.JammedTotal),
            (expected.Counters.NnTotal, actual.Counters.NnTotal),
        ]);
        Assert.Equal(expected.Counters.Qkss, actual.Counters.Qkss);
        Assert.Equal(expected.Counters.Nfx, actual.Counters.Nfx);
        Assert.Equal(expected.Counters.Nfy, actual.Counters.Nfy);
        Assert.Equal(expected.Counters.Nfz, actual.Counters.Nfz);
        Assert.Equal(expected.Counters.Nfq, actual.Counters.Nfq);
        Assert.Equal(expected.Counters.Nfw, actual.Counters.Nfw);
        Assert.Equal(expected.Counters.IbridgeTotal, actual.Counters.IbridgeTotal);
        // ImmutableArray<T>.Equals compares the underlying array by reference, not element by element
        // (two separately built arrays with the same elements are never "equal" that way), so Assert.Equal
        // on the ImmutableArray itself would fail even for a correct round trip: compare as plain arrays.
        Assert.Equal(expected.Counters.Conditions.ToArray(), actual.Counters.Conditions.ToArray());

        AssertEqualDoubles(nameof(GeneratorAccuracy), [
            (expected.GeneratorAccuracy.Eps1, actual.GeneratorAccuracy.Eps1),
            (expected.GeneratorAccuracy.Eps2, actual.GeneratorAccuracy.Eps2),
            (expected.GeneratorAccuracy.Eps3, actual.GeneratorAccuracy.Eps3),
            (expected.GeneratorAccuracy.Eps4, actual.GeneratorAccuracy.Eps4),
            (expected.GeneratorAccuracy.Eps5, actual.GeneratorAccuracy.Eps5),
            (expected.GeneratorAccuracy.Eps6, actual.GeneratorAccuracy.Eps6),
            (expected.GeneratorAccuracy.Eps7, actual.GeneratorAccuracy.Eps7),
            (expected.GeneratorAccuracy.Epsx1, actual.GeneratorAccuracy.Epsx1),
            (expected.GeneratorAccuracy.Epsx2, actual.GeneratorAccuracy.Epsx2),
            (expected.GeneratorAccuracy.Epsx3, actual.GeneratorAccuracy.Epsx3),
            (expected.GeneratorAccuracy.Epsy, actual.GeneratorAccuracy.Epsy),
            (expected.GeneratorAccuracy.Epsmd4, actual.GeneratorAccuracy.Epsmd4),
            (expected.GeneratorAccuracy.Epsmd3, actual.GeneratorAccuracy.Epsmd3),
        ]);
        AssertEqualDoubleArrays(expected.GeneratorAccuracy.Epsalldok, actual.GeneratorAccuracy.Epsalldok);
        Assert.Equal(expected.GeneratorAccuracy.OxidizerAccuracyWarning, actual.GeneratorAccuracy.OxidizerAccuracyWarning);
        Assert.Equal(expected.GeneratorAccuracy.PocketAccuracyWarning, actual.GeneratorAccuracy.PocketAccuracyWarning);

        AssertEqualDoubles(nameof(ParticleSizes), [
            (expected.ParticleSizes.Dok43b, actual.ParticleSizes.Dok43b),
            (expected.ParticleSizes.Dok43s, actual.ParticleSizes.Dok43s),
            (expected.ParticleSizes.Alldok43, actual.ParticleSizes.Alldok43),
            (expected.ParticleSizes.Alldok432, actual.ParticleSizes.Alldok432),
        ]);

        AssertEqualDoubles(nameof(LocalStructure), [
            (expected.LocalStructure.Gdokleft, actual.LocalStructure.Gdokleft),
            (expected.LocalStructure.Vdokleft, actual.LocalStructure.Vdokleft),
            (expected.LocalStructure.Plotsmdok, actual.LocalStructure.Plotsmdok),
            (expected.LocalStructure.Plotsm, actual.LocalStructure.Plotsm),
            (expected.LocalStructure.Mp, actual.LocalStructure.Mp),
        ]);

        AssertEqualPockets(expected.Pockets, actual.Pockets);

        AssertEqualDoubles(nameof(MassFractions), [
            (expected.MassFractions.DolM1, actual.MassFractions.DolM1),
            (expected.MassFractions.DolM2, actual.MassFractions.DolM2),
            (expected.MassFractions.DolM3, actual.MassFractions.DolM3),
        ]);

        AssertEqualDoubles(nameof(Agglomerates), [
            (expected.Agglomerates.Dqmkm1, actual.Agglomerates.Dqmkm1),
            (expected.Agglomerates.Dqmkm2, actual.Agglomerates.Dqmkm2),
            (expected.Agglomerates.Qmcoef, actual.Agglomerates.Qmcoef),
        ]);
        Assert.Equal(expected.Agglomerates.CoefNmax, actual.Agglomerates.CoefNmax);
        Assert.Equal(expected.Agglomerates.Qmkm1Nmax, actual.Agglomerates.Qmkm1Nmax);
        Assert.Equal(expected.Agglomerates.Qmkm2Nmax, actual.Agglomerates.Qmkm2Nmax);

        AssertEqualHistograms(expected.Histograms, actual.Histograms);
        AssertEqualConvergence(expected.Convergence, actual.Convergence);
        AssertEqualDiagnostics(expected.Diagnostics, actual.Diagnostics);
        AssertEqualStoredSetup(expected.StoredSetup, actual.StoredSetup);
    }

    private static void AssertEqualStoredSetup(StoredSetup expected, StoredSetup actual)
    {
        AssertEqualDoubles(nameof(StoredSetup), [
            (expected.CellSize, actual.CellSize),
            (expected.CategoryStep, actual.CategoryStep),
            (expected.Dmin, actual.Dmin),
            (expected.OxidizerDensity, actual.OxidizerDensity),
            (expected.PropellantDensity, actual.PropellantDensity),
            (expected.OxidizerMassFraction, actual.OxidizerMassFraction),
            (expected.MetalMassFraction, actual.MetalMassFraction),
            (expected.Alpha, actual.Alpha),
            (expected.NnMin, actual.NnMin),
            (expected.NnMax, actual.NnMax),
            (expected.PocketCoefficient, actual.PocketCoefficient),
            (expected.BridgeCoefficient, actual.BridgeCoefficient),
            (expected.HomogenizedOxidizerFraction, actual.HomogenizedOxidizerFraction),
            (expected.EpsDok, actual.EpsDok),
        ]);
        AssertEqualDoubleArrays(expected.FractionMassShares, actual.FractionMassShares);
        AssertEqualDoubleArrays(expected.FractionBounds, actual.FractionBounds);
    }

    private static void AssertEqualHeader(RunHeader expected, RunHeader actual)
    {
        Assert.Equal(expected.FormulationName, actual.FormulationName);
        Assert.Equal(expected.Cycles, actual.Cycles);
        Assert.Equal(expected.ParticlesPerCycle, actual.ParticlesPerCycle);
        AssertEqualDoubles(nameof(RunHeader), [
            (expected.Dokm, actual.Dokm),
            (expected.Doksd, actual.Doksd),
            (expected.Ddokmax, actual.Ddokmax),
            (expected.Dmax, actual.Dmax),
            (expected.TailProbabilityModified, actual.TailProbabilityModified),
            (expected.Ggg, actual.Ggg),
        ]);
        AssertEqualDoubleArrays(expected.Zx, actual.Zx);
    }

    private static void AssertEqualPockets(Pockets expected, Pockets actual)
    {
        AssertEqualDoubles(nameof(Pockets), [
            (expected.Dp43, actual.Dp43),
            (expected.D432, actual.D432),
            (expected.Sdevp43, actual.Sdevp43),
            (expected.Dqkarm, actual.Dqkarm),
            (expected.Dkarm43Cor, actual.Dkarm43Cor),
            (expected.Sdevp43Cor, actual.Sdevp43Cor),
            (expected.DqkarmCor, actual.DqkarmCor),
            (expected.Dfmk432, actual.Dfmk432),
            (expected.Sdevp243, actual.Sdevp243),
            (expected.DpMax, actual.DpMax),
            (expected.DpMaxCor, actual.DpMaxCor),
        ]);
        Assert.Equal(expected.DpRow, actual.DpRow);
        AssertEqualDoubleArrays(expected.Dpockets, actual.Dpockets);
        AssertEqualDoubleArrays(expected.Dokp43, actual.Dokp43);
        AssertEqualDoubleArrays(expected.Qdokkarm, actual.Qdokkarm);

        Assert.Equal(expected.Qdokso.Length, actual.Qdokso.Length);
        for (var r = 0; r < expected.Qdokso.Length; r++)
        {
            Assert.Equal(expected.Qdokso[r].Length, actual.Qdokso[r].Length);
            for (var c = 0; c < expected.Qdokso[r].Length; c++)
            {
                AssertEqualDouble($"Qdokso[{r},{c}]", expected.Qdokso[r][c], actual.Qdokso[r][c]);
            }
        }
    }

    private static void AssertEqualHistograms(Histograms expected, Histograms actual)
    {
        AssertEqualDoubleArrays(expected.Allvdokso, actual.Allvdokso);
        AssertEqualDoubleArrays(expected.Vkso, actual.Vkso);
        AssertEqualDoubleArrays(expected.Qks1, actual.Qks1);
        AssertEqualDoubleArrays(expected.FmkarmCorNormalized, actual.FmkarmCorNormalized);
        AssertEqualDoubleArrays(expected.Fmkarm2Normalized, actual.Fmkarm2Normalized);
        AssertEqualDoubleArrays(expected.FqkarmCorNormalized, actual.FqkarmCorNormalized);
        AssertEqualDoubleArrays(expected.Qmkm1Normalized, actual.Qmkm1Normalized);
        AssertEqualDoubleArrays(expected.Qmkm2Normalized, actual.Qmkm2Normalized);
        AssertEqualDoubleArrays(expected.CoefNormalized, actual.CoefNormalized);
        AssertEqualDoubleArrays(expected.Pdoksmall, actual.Pdoksmall);
    }

    private static void AssertEqualConvergence(Convergence? expected, Convergence? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        AssertEqualDoubleArrays(expected.ConvergenceEpsy, actual.ConvergenceEpsy);
        AssertEqualDoubleArrays(expected.ConvergenceEpsmd3, actual.ConvergenceEpsmd3);
        AssertEqualDoubleArrays(expected.ConvergenceEpsmd4, actual.ConvergenceEpsmd4);
        AssertEqualDoubleArrays(expected.ConvergenceAlldok43, actual.ConvergenceAlldok43);
        AssertEqualDoubleArrays(expected.ConvergenceAlldoksd, actual.ConvergenceAlldoksd);
        AssertEqualDoubleArrays(expected.ConvergenceDolM2, actual.ConvergenceDolM2);
    }

    private static void AssertEqualDiagnostics(RunDiagnostics expected, RunDiagnostics actual)
    {
        Assert.Equal(expected.Mode, actual.Mode);
        Assert.Equal(expected.BatchSize, actual.BatchSize);
        Assert.Equal(expected.AttemptsPerLaunch, actual.AttemptsPerLaunch);
        Assert.Equal(expected.Accelerator.Kind, actual.Accelerator.Kind);
        Assert.Equal(expected.Accelerator.Name, actual.Accelerator.Name);
        Assert.Equal(expected.Streams, actual.Streams);
        Assert.Equal(expected.Precision, actual.Precision);
        Assert.Equal(expected.Seed, actual.Seed);
        Assert.Equal(expected.ContinuedStreams, actual.ContinuedStreams);
        Assert.Equal(expected.TotalAttempts, actual.TotalAttempts);
        Assert.Equal(expected.TotalLaunches, actual.TotalLaunches);
        Assert.Equal(expected.Elapsed, actual.Elapsed);
    }

    private static void AssertEqualDoubleArrays(ImmutableArray<double> expected, ImmutableArray<double> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            AssertEqualDouble($"[{i}]", expected[i], actual[i]);
        }
    }

    private static void AssertEqualDoubles(string context, (double Expected, double Actual)[] pairs)
    {
        foreach (var (expected, actual) in pairs)
        {
            AssertEqualDouble(context, expected, actual);
        }
    }

    private static void AssertEqualDouble(string context, double expected, double actual) =>
        Assert.True(
            BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual),
            $"{context}: expected {expected:R}, got {actual:R} (not bitwise equal)");
}
