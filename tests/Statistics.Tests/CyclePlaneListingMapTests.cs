using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>src/Statistics/CyclePlaneListing.map.txt</c> is the one hand-written artefact between the executable's
/// listing and the generated table: it says which Fortran name lives at which address and which Fortran lines each
/// stretch of code compiles. Every claim in it is held against the excerpt by
/// <c>classify-cycle-plane-listing.py check</c> (<c>src/Statistics/API.md</c>, "## Cycle listing map and table"), and each of
/// the checks has been seen red on a violation: <c>selftest</c> breaks the map and the excerpt in memory (a deleted
/// row, a bogus row, a moved address, a moved row boundary, a call to a thunk with no stub, a missing input, an
/// input with the wrong origin, a wrong array rank, swapped arrays, a wrong tenant type, a wrong register claim, a
/// line neither stored nor claimed, a line declared eliminated that is stored) and requires the matching check to
/// name it. The same script generates <c>CyclePlane.listing.generated.txt</c>; <c>verify</c> regenerates it in
/// memory and fails on any byte difference, and <c>selftest</c> also proves the generator once right (it
/// re-derives the archive-confirmed verdicts at lines 771-784 and 795-796) and twice red (a swapped
/// operand moves the <c>order</c> of line 772; a register read claimed as a home read moves its <c>reads</c>).
/// </summary>
public class CyclePlaneListingMapTests
{
    private const string Script = "classify-cycle-plane-listing.py";

    // These three read the listing excerpt and the source, which lie outside the repository (tools/legacy):
    // Category=Legacy. The pinned rows below read the committed table and stay in the fast set.
    [Fact]
    [Trait("Category", "Legacy")]
    public void MapHoldsAgainstTheListing() => AssertPythonSucceeds("check");

    [Fact]
    [Trait("Category", "Legacy")]
    public void GeneratedTableReproducesFromTheListingByteForByte() => AssertPythonSucceeds("verify");

    [Fact]
    [Trait("Category", "Legacy")]
    public void EveryCheckOfTheMapAndTheGeneratorGoesRedOnAViolationItGuards() => AssertPythonSucceeds("selftest");

    /// <summary>
    /// Rows the design fixed before the table was read by any code, one per kind of site the plane's code follows,
    /// read where the code reads them, from the committed file: a REAL*4 store, a compiler temporary, a sum stored
    /// every pass, a sum carried in the register and stored every pass, an unrolled sum, an unrolled chain and a sum
    /// that is never stored.
    /// </summary>
    [Theory]
    [InlineData("40BDBB", "771", "S", "XSR0", "yes")]
    [InlineData("40950F", "1008+1010", "T", "PLOTsmdok ; vdokleft", "yes")]
    [InlineData("40D769", "899", "P", "qdokkarm(irow)", "yes")]
    [InlineData("40EDFF", "1028", "X", "zdoksmall(kilo)", "yes")]
    [InlineData("40C3B7", "805", "B6", "ALLDOK432", "yes")]
    [InlineData("40C22C", "808", "C6", "FMDOK(kilo+1)", "yes")]
    [InlineData("40E9CB", "990", "R", "D243", "no")]
    public void PinnedSiteHasItsKind(string at, string lines, string kind, string target, string rounds)
    {
        var row = File.ReadAllLines(Path.Combine(RepositoryPaths.Root, "src", "Statistics", "CyclePlane.listing.generated.txt"))
            .Where(l => !l.StartsWith('#') && l.Length > 0)
            .Select(l => l.Split(" | "))
            .Single(c => c[0] == at);
        Assert.Equal([lines, kind, target, rounds], [row[2], row[5], row[3], row[6]]);
    }

    private static void AssertPythonSucceeds(string mode) =>
        PythonScript.RequireSuccess(Path.Combine("src", "Statistics", Script), mode);
}
