using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The four literal sites of <c>ExactConstants</c> are what a search of the line map's ranges finds
/// (<c>src/Statistics/ACCEPTANCE.md</c>, "Corrected, <c>ExactConstants</c>"): the patterns <c>3\.14\d*</c>,
/// <c>12\.566\d*</c>, <c>0?\.333\d*</c> and the quotient <c>1/3.</c> match exactly at 1704, 1716, 1014 and 1016
/// (twice), nowhere else. <c>exact_constants_sites.py</c> reads the Fortran source, which lies outside the
/// repository (<c>tools/legacy</c>), so the first fact is <c>Category=Legacy</c>; the script's own <c>selftest</c>
/// reads nothing outside the tree and is red on a removed site, an extra one, a comment and the quotient after a
/// letter, a digit or a point.
/// </summary>
public class ExactConstantsSitesTests
{
    private const string Script = "exact_constants_sites.py";

    [Fact]
    [Trait("Category", "Legacy")]
    public void TheLiteralSitesOfTheLineMapAreExactlyTheFiveOfTheCriterion() => RunScript("verify");

    [Fact]
    public void TheSearchGoesRedOnEveryViolationItGuards() => RunScript("selftest");

    private static void RunScript(string mode) => PythonScript.RequireSuccess(Path.Combine("src", "Statistics", Script), mode);
}
