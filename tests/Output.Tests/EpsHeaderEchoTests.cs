using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The header echo of the menu parameter <c>eps</c> (Fortran menu item [4], <c>SetupPlane.generated.txt</c>'s
/// own <c>eps_dok</c> row). Found comparing a live <c>--precision original</c> run's header against the
/// archived reference headers on all five reference formulations (`src/Output/BOOT.md`, "## Header echo"):
/// the original prints <c>5.0000001E-02</c>, its REAL*4 store of the default 0.05, where the port printed
/// <c>5.0000000E-02</c> under both precision kinds before that fix.
/// </summary>
/// <remarks>
/// Moved 2026-09-24 (BOOT.md, "## Design decision (2026-09-24): print the stored setup"): the rounding
/// itself is no longer this writer's own cast, it is <c>Statistics.Setup.Prepare</c>'s, one generated
/// setup-plane member among many (<c>SetupPlane.generated.txt</c>'s own <c>eps_dok</c> row, site
/// <c>SetupInputs.EpsDok</c>). This class now proves the narrower, still-load-bearing claim that is
/// <c>ResultsMWriter</c>'s own to prove: the header line prints <see cref="StoredSetup.EpsDok"/> exactly as
/// given, with no rounding, casting or other arithmetic of its own — a writer that reads a generated member
/// unrounded, or rounds one that arrives already rounded, would both be caught by a differing printed line
/// for the same two frozen inputs below. <c>Setup.Prepare</c>'s own rounding of <c>eps_dok</c> is
/// <c>tests/Statistics.Tests/SetupPlaneTests</c>' concern, not this one's (one check per fact, root BOOT.md
/// taboo: "every check ... proven twice", not proven by two nodes at once).
/// </remarks>
public class EpsHeaderEchoTests
{
    private static readonly string InptReferencePath =
        RepositoryPaths.Resolve("tests", "Fixtures", "references", "inpt", "results.m.txt");

    // The original's own REAL*4 store of the default 0.05, the same value Setup.Prepare now computes under
    // PrecisionKind.Original (SetupPlaneTests' own frozen-answer pattern) — computed here from the binary32
    // round trip directly, not copied from Setup's internal Binary32 helper (internal to Statistics, AGENTS.md
    // §3), matching how the header line printed it before the 2026-09-24 move: (double)(float)value.
    private const double EpsDokRoundedToBinary32 = 0.05f;
    private const double EpsDokUnrounded = 0.05;

    private static string[] WriteLines(double epsDok)
    {
        var sample = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Original);
        var patched = sample with { StoredSetup = sample.StoredSetup with { EpsDok = epsDok } };
        return PrecisionFooterLineTests.WriteLines(patched);
    }

    private static string FindEpsLine(string[] lines) =>
        Assert.Single(lines, l => l.TrimStart().StartsWith("% eps =", StringComparison.Ordinal));

    [Fact]
    public void EpsHeaderLineGivenTheOriginalsRealFourStorePrintsItUnchanged()
    {
        var epsLine = FindEpsLine(WriteLines(EpsDokRoundedToBinary32));

        // Cut from tests/Fixtures/references/inpt/results.m.txt line 16: ' % eps =  5.0000001E-02 ;' - the
        // same text on all five reference formulations, since it echoes the default EpsDok, unrelated to the
        // sample's own formulation.
        const string cut = "5.0000001E-02";
        Assert.Contains(cut, epsLine, StringComparison.Ordinal);
        Assert.Contains(cut, File.ReadAllLines(InptReferencePath)[15], StringComparison.Ordinal);
    }

    [Fact]
    public void EpsHeaderLineGivenThePortsDoubleUnroundedPrintsItUnchanged()
    {
        var epsLine = FindEpsLine(WriteLines(EpsDokUnrounded));

        // The port's own double-precision formatting of 0.05, with no REAL*4 rounding applied by this writer:
        // distinct from the rounded store above only in the last digit.
        Assert.Contains("5.0000000E-02", epsLine, StringComparison.Ordinal);
        Assert.DoesNotContain("5.0000001E-02", epsLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangingStoredSetupEpsDokChangesTheEpsHeaderLine()
    {
        var unroundedLine = FindEpsLine(WriteLines(EpsDokUnrounded));
        var roundedLine = FindEpsLine(WriteLines(EpsDokRoundedToBinary32));

        Assert.NotEqual(unroundedLine, roundedLine);
    }
}
