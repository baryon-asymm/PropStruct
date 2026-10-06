using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Fixtures.Tests;

/// <summary>
/// L1 of this node's BOOT.md: "Legacy/ byte-equal to the archive members (source by re-transcoding)". Runs
/// `tests/Fixtures/generate.py verify` (Fixtures' own API.md, "generate.py") rather than re-deriving the CP1251
/// transcoding rule in C#: the rule already has one implementation (`Fixtures`' own), and this node reads only
/// `Fixtures`' `API.md` (AGENTS.md §3) — its documented CLI contract, not its script's internals. The archive, the
/// source and the executable lie outside the repository (`tools/legacy`): the fact is `Category=Legacy`, and
/// `verify` also regenerates the three derived fixtures (`generate.py derive --check`) and fails on a difference.
/// </summary>
public class LegacyArchiveTests
{
    [Fact]
    [Trait("Category", "Legacy")]
    public void LegacyMatchesTheArchiveByteForByte() =>
        PythonScript.RequireSuccess(Path.Combine("tests", "Fixtures", "generate.py"), "verify");
}
