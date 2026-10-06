using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>src/Statistics/SetupPlane.generated.txt</c> is generated, never typed (root BOOT.md,
/// "Precision kind is an option of every run": "Both sets are generated ... never typed by
/// hand"; AGENTS.md §6). Checked the same way
/// <c>tests/Particle.Tests/RealFourClassificationGeneratorTests</c> checks the accumulator
/// classifier: a fresh regeneration must reproduce the committed file byte for byte, via the
/// same <c>generate</c>/<c>verify</c> convention.
///
/// <c>classify-setup-plane.py</c>'s own <c>classify()</c> validates the hand-written
/// <c>SITE_MAP</c> against the mechanically scanned names both ways before <c>verify</c> ever
/// compares bytes: it raises if a generated name has no site (an addition the map has not
/// caught up with) and if the map names something the scan did not generate (a stale or a
/// typo'd entry). This one fact test therefore proves three things at once — byte-for-byte
/// reproduction and both halves of that map check — matching the root taboo "every check that
/// guards a quantitative claim is proven twice": this test was seen red on a removed SITE_MAP
/// entry ("SITE_MAP is missing an entry for: gm") and on a bogus one ("SITE_MAP names
/// bogus_name, which this run did not generate"), and green again once each was undone.
/// </summary>
public class SetupPlaneClassificationGeneratorTests
{
    private static readonly string StatisticsDirectory = Path.Combine(RepositoryPaths.Root, "src", "Statistics");

    // Both facts below read the Fortran source, which lies outside the repository (tools/legacy): Category=Legacy.
    [Fact]
    [Trait("Category", "Legacy")]
    public void ClassificationReproducesByteForByteOnRegeneration() =>
        PythonScript.RequireSuccess(Path.Combine("src", "Statistics", "classify-setup-plane.py"), "verify");

    /// <summary>The shared reader <c>fortran_source.py</c>, which both classifiers of the node read the source through.</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void ContinuationRuleReadsTheTabFormLinesAsInitialLines() =>
        PythonScript.RequireSuccess(Path.Combine("src", "Statistics", "fortran_source.py"), "selftest");

    [Fact]
    public void SetupPlaneRegisterSumsAreSumsAndTheRestAccumulators()
    {
        var roles = File.ReadAllLines(Path.Combine(StatisticsDirectory, "SetupPlane.generated.txt"))
            .Where(l => !l.StartsWith('#') && l.Contains(" | ", StringComparison.Ordinal))
            .Select(l => l.Split(" | "))
            .Where(c => c.Length == 5)
            .ToDictionary(c => c[0], c => c[1], StringComparer.Ordinal);
        Assert.Equal("sum", roles["dokm"]);
        Assert.Equal("sum", roles["doksd"]);
        Assert.Equal("accumulator", roles["dok4"]);
        Assert.Equal("accumulator", roles["dok3"]);
        Assert.Equal("accumulator", roles["zs"]);
    }
}
