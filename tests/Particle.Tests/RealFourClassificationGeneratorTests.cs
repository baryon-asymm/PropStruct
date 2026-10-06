using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// <c>src/Particle/RealFourAccumulators.generated.txt</c> is generated, never typed
/// (BOOT.md, "Accumulators"; AGENTS.md §6). Checked the way
/// <c>docs/ORIGINAL-DEFECTS.md</c> is checked (root BOOT.md, "The defect report is
/// assembled, never written"): a fresh regeneration must reproduce the committed file
/// byte for byte, following the same <c>generate</c>/<c>verify</c> convention and the
/// same <c>python -X utf8 &lt;script&gt; verify</c> invocation
/// <c>tests/Fixtures.Tests/LegacyArchiveTests.cs</c> already uses for
/// <c>tests/Fixtures/generate.py</c>.
/// </summary>
public class RealFourClassificationGeneratorTests
{
    // Reads the Fortran source, which lies outside the repository (tools/legacy): Category=Legacy.
    [Fact]
    [Trait("Category", "Legacy")]
    public void ClassificationReproducesByteForByteOnRegeneration() =>
        PythonScript.RequireSuccess(Path.Combine("src", "Particle", "classify-real4-accumulators.py"), "verify");
}
