using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Fixtures.Tests;

/// <summary>
/// The gate of the <c>Legacy</c> category (<c>tools/legacy/BOOT.md</c>, "The gate"): every other fact of the category
/// stands on files this one proves to be the recorded ones, and fails, never skips, when
/// <c>PROPSTRUCT_LEGACY_DIR</c> is unset or names a file whose hash is not the manifest's. The second is the
/// committed forbidden list against a regeneration, the third the scan of the working tree for the original's
/// text and bytes, which needs the original present and prints no source line or byte, only where
/// (<c>tools/legacy/API.md</c>, "## scan.py").
/// </summary>
public class OriginalGateTests
{
    /// <summary>Runs <c>legacy.py check</c>: the five files against <c>tools/legacy/original.sha256</c>.</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheOriginalIsTheRecordedOne() =>
        PythonScript.RequireSuccess(Path.Combine("tools", "legacy", "legacy.py"), "check");

    /// <summary>Runs <c>legacy.py forbidden --check</c>: the committed <c>tools/legacy/forbidden.sha256</c> against a
    /// regeneration from the archive, so that a deleted or altered line is red here and not only a scan that no longer
    /// finds what the line would have caught.</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheForbiddenListIsTheOneTheArchiveGenerates() =>
        PythonScript.RequireSuccess(Path.Combine("tools", "legacy", "legacy.py"), "forbidden", "--check");

    /// <summary>Runs <c>scan.py</c> over the repository root with the original present: no forbidden hash, no build-product
    /// name, no window of the original's code bytes, and no file quoting more than eight statements of its source.</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheTreeHoldsNothingOfTheOriginalBeyondTheQuotationRule() =>
        PythonScript.RequireSuccess(Path.Combine("tools", "legacy", "scan.py"), ".");
}
