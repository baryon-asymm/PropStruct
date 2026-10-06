using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The positive control BOOT.md's "## Design decision (2026-09-24): print the stored setup" asks for (root
/// BOOT.md taboo: "every check ... proven twice ... a positive control, passed through the same path and
/// read where the consumer reads"), distinct from <see cref="EpsHeaderEchoTests"/>'s: a second generated
/// setup-plane header echo (<c>P(karm-in-karm) coef</c>, <c>StoredSetup.PocketCoefficient</c>) at a
/// non-default value binary32 cannot hold exactly, run once through the real original executable
/// (<c>tests/Fixtures/cases/output/nondefault-pocket-coefficient/results.m.txt</c>, provenance:
/// <c>tests/Fixtures/provenance.json</c>), the printed line read from that file rather than typed, and
/// compared against the port's own line for the identical menu answer.
/// </summary>
/// <remarks>
/// The value is <c>karmcoef = 0.03</c>, not the branch's first attempt (<c>12.345</c>). Found by review
/// 2026-09-24: DVF's list-directed <c>REAL*4</c> print shows seven significant digits in fixed form for a
/// magnitude &gt;= 0.1 (<c>12.34500</c>), and binary32(12.345) = 12.345000267028809 rounds to that same
/// seven-digit string as the unrounded double does — the line cannot tell the two kinds apart, so the old
/// fixture would pass unchanged even on the pre-change code. Below 0.1 the field switches to eight-digit
/// exponent form, where the error shows: binary32(0.03) = 0.029999999329447746, printed
/// <c>2.9999999E-02</c> against the double's own <c>3.0000000E-02</c> (<see cref="CoefficientLineGivenThePortsDoubleUnroundedDiffersFromTheOriginalsRealFourStore"/>,
/// the discriminating half of this control). <c>karmcoef</c> stays the chosen member: <c>Setup.Prepare</c>
/// validates <c>AK1</c>-<c>AK4</c>/<c>NnMin</c>/<c>NnMax</c>/cell sizes but places no bound on the two
/// structural coefficients, and the original executable itself completed a full run at this value (the
/// archive below), so <c>karmcoef</c> is what the model tolerates: <c>alpha</c>/<c>mkmcoef</c>/<c>nn_min</c>
/// were not tried once this one worked.
/// </remarks>
public class PocketCoefficientHeaderEchoTests
{
    private static readonly string ArchivePath = RepositoryPaths.Resolve(
        "tests", "Fixtures", "cases", "output", "nondefault-pocket-coefficient", "results.m.txt");

    private static string FindCoefficientLine(IEnumerable<string> lines) =>
        Assert.Single(lines, l => l.Contains("P(karm-in-karm) coef", StringComparison.Ordinal));

    // ---- unit level: the writer's own binary32 round trip, isolated from a real run -----------------------

    /// <summary>
    /// Cut from the archive itself, line 18 of the fixture: the original's own REAL*4 store of
    /// <c>karmcoef = 0.03</c> (menu item [7], `run_original.py --karmcoef`). Patching
    /// <see cref="StoredSetup.PocketCoefficient"/> directly (the same pattern
    /// <see cref="EpsHeaderEchoTests"/> uses for <c>EpsDok</c>) proves the writer is a transparent
    /// pass-through here too, without waiting on a full simulator run.
    /// </summary>
    [Fact]
    public void CoefficientLineGivenTheOriginalsRealFourStorePrintsItUnchanged()
    {
        const double karmcoefRoundedToBinary32 = 0.03f;

        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Original);
        var patched = sample with { StoredSetup = sample.StoredSetup with { PocketCoefficient = karmcoefRoundedToBinary32 } };
        var line = FindCoefficientLine(PrecisionFooterLineTests.WriteLines(patched));

        // Cut from tests/Fixtures/cases/output/nondefault-pocket-coefficient/results.m.txt, line 18:
        // ' % P(karm-in-karm) coef =  2.9999999E-02 ;'.
        const string cut = "2.9999999E-02";
        Assert.Contains(cut, line, StringComparison.Ordinal);
        Assert.Contains(cut, File.ReadAllLines(ArchivePath)[17], StringComparison.Ordinal);
    }

    /// <summary>
    /// The discriminating half of the control (this class's own remarks): the port's <em>unrounded</em>
    /// double 0.03, never having passed through binary32, prints a different eighth digit. If this line ever
    /// equalled the one above, the positive control would have stopped discriminating again, silently.
    /// </summary>
    [Fact]
    public void CoefficientLineGivenThePortsDoubleUnroundedDiffersFromTheOriginalsRealFourStore()
    {
        const double karmcoefUnrounded = 0.03;

        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Original);
        var patched = sample with { StoredSetup = sample.StoredSetup with { PocketCoefficient = karmcoefUnrounded } };
        var line = FindCoefficientLine(PrecisionFooterLineTests.WriteLines(patched));

        Assert.Contains("3.0000000E-02", line, StringComparison.Ordinal);
        Assert.DoesNotContain("2.9999999E-02", line, StringComparison.Ordinal);
    }

    // ---- integration level: a real reference-mode run, against the real original archive -------------------

    /// <summary>
    /// The port, run at the same formulation (<c>inpt</c>), the same seed and layout, and
    /// <see cref="PrecisionKind.Original"/> so its setup plane is reproduced in binary32 (root BOOT.md,
    /// "Precision kind is an option of every run"), with <c>ModelParameters.PocketCoefficient</c> set to the
    /// same menu answer the archive was produced with. Shrinks <c>Cycles</c>/<c>ParticlesPerCycle</c> to keep
    /// this fast without <c>Category=Long</c>: the printed line under test is a header echo fixed before the
    /// particle loop and does not depend on either (the fixture itself was generated at <c>inpt</c>'s own
    /// shipped <c>N</c>/<c>KXX</c> — <c>tests/Fixtures</c>' own provenance invariant, "every provenance input
    /// hash matches the shipped .dat", holds only for the unmodified formulation).
    /// </summary>
    [Fact]
    public void PortsCoefficientLineMatchesTheArchivedOriginalDigitForDigitUnderOriginal()
    {
        var archiveLine = FindCoefficientLine(File.ReadAllLines(ArchivePath));
        var portLine = RunPort(PrecisionKind.Original);

        Assert.Equal(archiveLine, portLine);
    }

    /// <summary>
    /// The negative control this positive control needs to be seen right rather than vacuous (root BOOT.md
    /// taboo, "every check ... proven twice"): the identical run under <see cref="PrecisionKind.Binary64"/> —
    /// no binary32 rounding anywhere in the setup plane — must <em>not</em> reproduce the archive's line. Red
    /// on the pre-change code by construction: before this branch, nothing rounded <c>PocketCoefficient</c>
    /// under either kind, so this same assertion would have failed to distinguish
    /// <see cref="PrecisionKind.Original"/> from <see cref="PrecisionKind.Binary64"/> at all — both printed the
    /// double's own <c>3.0000000E-02</c>.
    /// </summary>
    [Fact]
    public void PortsCoefficientLineUnderBinary64DoesNotMatchTheArchivedOriginal()
    {
        var archiveLine = FindCoefficientLine(File.ReadAllLines(ArchivePath));
        var portLine = RunPort(PrecisionKind.Binary64);

        Assert.NotEqual(archiveLine, portLine);
        Assert.Contains("3.0000000E-02", portLine, StringComparison.Ordinal);
    }

    private static string RunPort(PrecisionKind precision)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "inpt.dat");
        var formulation = DatFile.Read(datPath) with { Cycles = 1, ParticlesPerCycle = 5 };
        var parameters = ModelParameters.Default with { PocketCoefficient = 0.03 };

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Parameters = parameters,
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Precision = precision,
            Seed = 0UL,
        });

        var result = simulator.Run(formulation);

        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, parameters, result, tempPath);
            return FindCoefficientLine(File.ReadAllLines(tempPath));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
