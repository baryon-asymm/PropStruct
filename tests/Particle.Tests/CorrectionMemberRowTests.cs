using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// The members of <c>CorrectionSet</c> agree with the generated defect page
/// (<c>src/Particle/ACCEPTANCE.md</c>). Right: on the real <c>docs/ORIGINAL-DEFECTS.md</c> and
/// this node's real <c>API.md</c> the check finds nothing. Red: the same texts with one
/// flag's rows set back to <c>unchanged</c>, with a row naming an unknown member and with one
/// member's value changed in <c>API.md</c> each produce the discrepancy they should.
/// </summary>
public class CorrectionMemberRowTests
{
    private const string ExactConstantsValue = "| fixed (ExactConstants) |";

    public static IEnumerable<object[]> MembersWithOneRow() =>
        Enum.GetValues<CorrectionSet>()
            .Where(flag => flag is not (CorrectionSet.None or CorrectionSet.All or CorrectionSet.ExactConstants))
            .Select(flag => new object[] { flag.ToString() });

    [Fact]
    public void TheRealPageAndTheRealApiAgreeWithTheEnum() =>
        Assert.Empty(CorrectionMemberRowCheck.Discrepancies(Page(), ApiText()));

    [Theory]
    [MemberData(nameof(MembersWithOneRow))]
    public void AMemberWhoseOnlyRowReadsUnchangedIsReported(string member)
    {
        var scratch = Replaced(Page(), $"| fixed ({member}) |", "| unchanged |");

        var problems = CorrectionMemberRowCheck.Discrepancies(scratch, ApiText());

        Assert.Equal([$"CorrectionSet.{member} has no 'fixed ({member})' row on the page"], problems);
    }

    [Fact]
    public void AMemberWithTwoRowsStaysNamedWhenOneRowIsSetBack()
    {
        var page = Page();
        var first = page.IndexOf(ExactConstantsValue, StringComparison.Ordinal);
        var scratch = string.Concat(
            page.AsSpan(0, first), "| unchanged |", page.AsSpan(first + ExactConstantsValue.Length));

        Assert.Empty(CorrectionMemberRowCheck.Discrepancies(scratch, ApiText()));
    }

    [Fact]
    public void AMemberWithTwoRowsSetBackInBothIsReported()
    {
        var scratch = Replaced(Page(), ExactConstantsValue, "| unchanged |");

        var problems = CorrectionMemberRowCheck.Discrepancies(scratch, ApiText());

        Assert.Equal([$"CorrectionSet.{nameof(CorrectionSet.ExactConstants)} has no 'fixed ({nameof(CorrectionSet.ExactConstants)})' row on the page"], problems);
    }

    [Fact]
    public void AnUnknownMemberOnThePageIsReported()
    {
        var scratch = Page() + "\n| `src/Particle` | 1 | A scratch row. | not measured | reproduced | fixed (Phantom) |\n";

        var problems = CorrectionMemberRowCheck.Discrepancies(scratch, ApiText());

        Assert.Equal(["the page names 'Phantom', which CorrectionSet does not have"], problems);
    }

    [Fact]
    public void AnUnknownMemberNextToAKnownOneInOneValueIsReported()
    {
        var scratch = Replaced(Page(), "| fixed (BridgeFrustum) |", "| fixed (BridgeFrustum, Phantom) |");

        var problems = CorrectionMemberRowCheck.Discrepancies(scratch, ApiText());

        Assert.Equal(["the page names 'Phantom', which CorrectionSet does not have"], problems);
    }

    [Fact]
    public void TheReservedNameModelIsExceptedAlone()
    {
        var page = Page() + "\n| `src/Output` | 1 | A scratch row. | not measured | reproduced | fixed (Model) |\n";

        Assert.Empty(CorrectionMemberRowCheck.Discrepancies(page, ApiText()));
    }

    [Fact]
    public void TheReservedNameModelIsExceptedNextToAMember()
    {
        var page = Replaced(Page(), "| fixed (WindowLowerEdge) |", "| fixed (WindowLowerEdge, Model) |");

        Assert.Empty(CorrectionMemberRowCheck.Discrepancies(page, ApiText()));
    }

    [Fact]
    public void AFixedValueOutsideATableRowIsNotRead()
    {
        var page = Page() + "\nProse that says fixed (Phantom) in passing.\n";

        Assert.Empty(CorrectionMemberRowCheck.Discrepancies(page, ApiText()));
    }

    [Fact]
    public void AValueTheApiListsDifferentlyIsReported()
    {
        var scratch = Replaced(ApiText(), "WindowLowerEdge = 8,", "WindowLowerEdge = 16,");

        var problems = CorrectionMemberRowCheck.Discrepancies(Page(), scratch);

        Assert.Contains("CorrectionSet.WindowLowerEdge is 8 and API.md lists 16", problems);
        Assert.Contains("API.md: WindowLowerEdge = 16 shares a bit with another member", problems);
    }

    [Fact]
    public void AnAllThatIsNotTheSumInTheApiIsReported()
    {
        var scratch = Replaced(ApiText(), "All = 4095,", "All = 4094,");

        var problems = CorrectionMemberRowCheck.Discrepancies(Page(), scratch);

        Assert.Contains("CorrectionSet.All is 4095 and API.md lists 4094", problems);
        Assert.Contains("API.md: All = 4094 is not the sum 4095 of the members", problems);
    }

    [Fact]
    public void AMemberMissingFromTheApiIsReported()
    {
        var scratch = Replaced(ApiText(), "    GdokleftOnce = 64,", "    // GdokleftOnce removed");

        var problems = CorrectionMemberRowCheck.Discrepancies(Page(), scratch);

        Assert.Contains("API.md does not list CorrectionSet.GdokleftOnce", problems);
    }

    private static string Page() =>
        File.ReadAllText(RepositoryPaths.Resolve("docs", "ORIGINAL-DEFECTS.md"));

    private static string ApiText() =>
        File.ReadAllText(RepositoryPaths.Resolve("src", "Particle", "API.md"));

    private static string Replaced(string text, string from, string to)
    {
        var result = text.Replace(from, to, StringComparison.Ordinal);
        Assert.NotEqual(text, result);
        return result;
    }
}
