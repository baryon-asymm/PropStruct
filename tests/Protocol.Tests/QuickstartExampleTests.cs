using System.Text.RegularExpressions;
using Xunit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// The library example is one text in four places (<see cref="QuickstartExample"/>), and the first of them is
/// compiled and run (<c>samples/Quickstart</c>; CI builds it against the packed package, <c>.github</c>). The
/// root <c>API.md</c>'s own ⚠ of 2026-09-20 says its example was checked by no machine, since
/// <c>DeclarationTests</c> reads declarations and an example declares none; this is the check that closes it.
/// </summary>
public sealed class QuickstartExampleTests
{
    private const string Edit = "EpsDok = 0.02";

    private const string Edited = "EpsDok = 0.03";

    private static string Sample => Read("samples/Quickstart/Program.cs");

    private static string RootApi => Read("API.md");

    private static string PackageReadme => Read("docs/nuget/PropStruct.md");

    private static string RepositoryReadme => Read("README.md");

    [Fact]
    public void TheFourPlacesQuoteTheSamplesRegionsByteForByte()
    {
        var problems = QuickstartExample.Mismatches(Sample, RootApi, PackageReadme, RepositoryReadme);

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    /// <summary>Non-degeneracy, the positive control first: the unedited text is green through the same call.
    /// Then one character edited in each place is red, and names that place only (or, for the sample, every place
    /// that quotes it).</summary>
    [Fact]
    public void AnEditedCharacterInAnyOfThePlacesIsRed()
    {
        Assert.Empty(QuickstartExample.Mismatches(Sample, RootApi, PackageReadme, RepositoryReadme));

        var inApi = QuickstartExample.Mismatches(Sample, EditedOnce(RootApi), PackageReadme, RepositoryReadme);
        var inPackageReadme = QuickstartExample.Mismatches(Sample, RootApi, EditedOnce(PackageReadme), RepositoryReadme);
        var inRepositoryReadme = QuickstartExample.Mismatches(Sample, RootApi, PackageReadme, EditedOnce(RepositoryReadme));
        var inSample = QuickstartExample.Mismatches(EditedOnce(Sample), RootApi, PackageReadme, RepositoryReadme);

        Assert.Contains("API.md", Assert.Single(inApi), StringComparison.Ordinal);
        Assert.Contains("docs/nuget/PropStruct.md", Assert.Single(inPackageReadme), StringComparison.Ordinal);
        Assert.StartsWith("README.md", Assert.Single(inRepositoryReadme), StringComparison.Ordinal);
        Assert.Equal(3, inSample.Count);
    }

    [Fact]
    public void ARegionMissingFromTheSampleIsAnErrorNotAnEmptyMatch()
    {
        var withoutBody = Sample.Replace("// snippet-start: Quickstart", "// snippet-start: Renamed", StringComparison.Ordinal);

        _ = Assert.Throws<InvalidOperationException>(() => QuickstartExample.Region(withoutBody, QuickstartExample.BodyRegion));
        _ = Assert.Throws<InvalidOperationException>(() => QuickstartExample.FenceAfterMarker(RootApi, "Absent"));
    }

    [Fact]
    public void TheFormulationTheExampleReadsIsAFileOfTheFixtures()
    {
        var read = Regex.Match(QuickstartExample.Region(Sample, QuickstartExample.BodyRegion), @"DatFile\.Read\(""([^""]+)""\)");

        Assert.True(read.Success, "the example no longer reads a formulation with DatFile.Read");
        Assert.True(
            File.Exists(Path.Combine(Tree.Root, "tests", "Fixtures", "Legacy", "formulations", read.Groups[1].Value)),
            $"{read.Groups[1].Value} is not a formulation of tests/Fixtures/Legacy/formulations");
    }

    private static string EditedOnce(string text)
    {
        var at = text.IndexOf(Edit, StringComparison.Ordinal);
        Assert.True(at >= 0, $"'{Edit}' no longer occurs where this check edits it");
        return string.Concat(text.AsSpan(0, at), Edited, text.AsSpan(at + Edit.Length));
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(Tree.Root, relativePath));
}
