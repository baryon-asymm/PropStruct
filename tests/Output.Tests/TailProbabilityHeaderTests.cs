using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// O-1 (fidelity audit, 2026-09-24): the header's tail-probability line and the gate that decides whether
/// <c>Dokb_max</c> prints must both read the Fortran's own reduced <c>alfa</c> (Fortran line 377,
/// <c>CALL PARAM(...,alfa,...)</c>, reduced inside <c>PARAM</c> at line 1750,
/// <c>alfa = alfa - (z1(Imax+1) - z1(Imax))</c>, printed already reduced at lines 1209 and 1272) - not the raw
/// menu parameter the caller passed in (<c>ModelParameters.TailProbability</c>). The reduced value already
/// existed as <c>RunHeader.TailProbabilityModified</c> (<c>src/Simulation/API.md</c>) before this fix; the
/// writer simply never read it.
/// </summary>
public class TailProbabilityHeaderTests
{
    // ---- unit level: the writer reads the header's own reduced value, not the raw parameter ----------------

    /// <summary>
    /// Red on the pre-fix code, which formatted <c>parameters.TailProbability</c> (the raw menu value) at this
    /// line: with <paramref name="raw"/> and the header's own <c>TailProbabilityModified</c> chosen distinct,
    /// the printed line can equal only one of the two formatted strings, and this asserts it is the header's.
    /// </summary>
    [Fact]
    public void StatisticalSignificanceLinePrintsTheHeadersReducedValueNotTheRawMenuParameter()
    {
        const double raw = 0.01;
        const double reduced = 0.0073;

        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64);
        var result = sample with { Header = sample.Header with { TailProbabilityModified = reduced } };
        var parameters = ModelParameters.Default with { TailProbability = raw };

        var lines = PrecisionFooterLineTests.WriteLines(result, parameters);
        var line = Assert.Single(lines, l => l.Contains("Statistical significance", StringComparison.Ordinal));

        Assert.Equal(" % Statistical significance P(alpha)=" + FortranFormat.DoubleRealField(reduced) + " ;", line);
        Assert.DoesNotContain(FortranFormat.DoubleRealField(raw), line);
    }

    /// <summary>
    /// Fortran line 1272, <c>if (alfa.GT.0) then</c>, reads the same already-reduced <c>alfa</c> line 1209
    /// prints (both execute after the single call to <c>PARAM</c> at line 377): the gate on <c>Dokb_max</c>
    /// must read <c>header.TailProbabilityModified</c>, not <c>parameters.TailProbability</c>. Chosen so the
    /// two disagree in both directions - red on the pre-fix code, which gated on the raw parameter, in both
    /// rows.
    /// </summary>
    [Theory]
    [InlineData(0.01, 0.0, false)] // raw > 0 but reduced down to exactly 0: the old gate wrongly printed the line
    [InlineData(0.0, 0.005, true)] // raw == 0 but the header's own value is positive: the old gate wrongly omitted it
    public void DokbMaxLineIsGatedOnTheHeadersReducedValueNotTheRawMenuParameter(double raw, double reduced, bool expectLine)
    {
        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64);
        var result = sample with { Header = sample.Header with { TailProbabilityModified = reduced } };
        var parameters = ModelParameters.Default with { TailProbability = raw };

        var lines = PrecisionFooterLineTests.WriteLines(result, parameters);
        var hasDokbMax = lines.Any(l => l.Contains("Dokb_max", StringComparison.Ordinal));

        Assert.Equal(expectLine, hasDokbMax);
    }

    // ---- integration level: a real nonzero-tail-probability run, against a real original archive -----------

    private static readonly string ArchivePath = RepositoryPaths.Resolve(
        "tests", "Fixtures", "cases", "output", "nonzero-tail-probability", "results.m.txt");

    /// <summary>
    /// <c>tests/Fixtures/cases/output/nonzero-tail-probability/results.m.txt</c>: the original executable run
    /// once on HPEPA3 with menu item [9] (<c>alfa</c>) answered <c>0.01</c> instead of accepting every default
    /// (provenance: <c>tests/Fixtures/provenance.json</c>, entry for this output). No archived reference or
    /// replica has a nonzero tail probability (every one of the 336 archived files prints <c>alfa = 0</c>), so
    /// this is the one fixture that can show the reduction actually firing in a real run, not only in the
    /// synthetic cases above.
    /// </summary>
    [Fact]
    public void ArchivedOriginalReallyReducesTheRequestedTailProbability()
    {
        var archive = ResultsMFile.Parse(ArchivePath);
        var archiveReduced = Assert.Single(archive["Statistical significance P(alpha)"]);

        // The menu answer this archive was produced with (provenance.json); not the expected value under
        // test - PARAM's own reduction, read from the archive above, is.
        const double requestedRaw = 0.01;

        Assert.True(
            archiveReduced is > 0 and < requestedRaw,
            $"expected the original's own PARAM call to reduce {requestedRaw} to something smaller and still " +
            $"positive; archive printed {archiveReduced}");
        Assert.True(archive.ContainsKey("Dokb_max"), "a reduced value > 0 should still gate Dokb_max on");
    }

    /// <summary>
    /// The port, run at the same formulation, layout, seed and <c>PrecisionKind.Original</c> (so its setup
    /// plane - the fraction thresholds PARAM's own search reads - is reproduced in binary32, root BOOT.md,
    /// "Precision kind is an option of every run") prints a reduced tail probability matching the archive's own
    /// to a tolerance far tighter than the ~8% gap printing the raw 0.01 back would leave, and no smaller than
    /// the residual repeatedly measured elsewhere in this tree for a REAL*8 sum carried through the original's
    /// x87 intermediates versus the port's own double arithmetic (root BOOT.md: "agreement of that sum with
    /// the executable at the last ULP ... is not claimed"). Long: a real reference-mode run of HPEPA3.
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void PortsReducedTailProbabilityLineMatchesTheArchivedOriginalsWithinFloatingPointTolerance()
    {
        var archive = ResultsMFile.Parse(ArchivePath);
        var archiveReduced = Assert.Single(archive["Statistical significance P(alpha)"]);
        const double requestedRaw = 0.01;

        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "HPEPA3.dat");
        var formulation = DatFile.Read(datPath);
        var parameters = ModelParameters.Default with { TailProbability = requestedRaw };

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Parameters = parameters,
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Precision = PrecisionKind.Original,
            Seed = 0UL,
        });

        var result = simulator.Run(formulation);

        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, parameters, result, tempPath);
            var port = ResultsMFile.Parse(tempPath);
            var portReduced = Assert.Single(port["Statistical significance P(alpha)"]);

            var relativeDifference = Math.Abs(portReduced - archiveReduced) / archiveReduced;
            Assert.True(
                relativeDifference < 1e-8,
                $"port printed {portReduced}, archive printed {archiveReduced}, relative difference " +
                $"{relativeDifference:E3} (raw {requestedRaw} would differ by about " +
                $"{Math.Abs(requestedRaw - archiveReduced) / archiveReduced:P1})");
            Assert.True(port.ContainsKey("Dokb_max"), "the port's own gate must fire on its own reduced value too");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
