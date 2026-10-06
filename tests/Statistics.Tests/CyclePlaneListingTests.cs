using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>PropStructV3.cycle-plane.listing.txt</c> (outside the repository, <c>tools/legacy</c>) is the executable's own listing, and
/// everything built on it (the address map, the generator, the oracle) trusts it only because it is proven to
/// be the executable's bytes: every instruction equals the executable's bytes at its address through the PE
/// section table, and the header's digests are the files' own. The proof is <c>check_cycle_plane_listing.py</c>
/// (<c>tests/Fixtures/API.md</c>, "## Cycle-plane listing"); it needs no dumpbin. Seen red 2026-10-02 on one
/// edited byte of the committed excerpt (0040BDB8, <c>DC 7D A0</c> to <c>DC 7D A1</c>): <c>verify</c> exits 1
/// naming the address; its own <c>selftest</c> goes red on an edited byte, a moved address, a dropped
/// instruction and a stale digest.
/// </summary>
public class CyclePlaneListingTests
{
    private const string Script = "check_cycle_plane_listing.py";

    // The excerpt and the executable lie outside the repository (tools/legacy): both facts are Category=Legacy.
    [Fact]
    [Trait("Category", "Legacy")]
    public void ListingExcerptEqualsTheExecutablesBytes() => AssertPythonSucceeds("verify");

    [Fact]
    [Trait("Category", "Legacy")]
    public void VerifyGoesRedOnEveryViolationItGuards() => AssertPythonSucceeds("selftest");

    private static void AssertPythonSucceeds(string mode) =>
        PythonScript.RequireSuccess(Path.Combine("tests", "Fixtures", Script), mode);
}
