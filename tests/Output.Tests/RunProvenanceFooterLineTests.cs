using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The stream-layout and execution-mode footer lines <c>ResultsMWriter</c> writes beside the accumulation-kind
/// line (root BOOT.md, "Invariants": both change the printed numbers by more than the accumulation kind does -
/// up to double digits of per cent for the stream layout, "Known bias of the original's seeds", and the whole
/// difference between refreshing and freezing the pocket histogram for the execution mode, "Batched mode
/// freezes only QKS1, per launch" - and until this session only the console printed them, so a user who kept
/// only <c>results.m</c> had nothing in the file to say why a re-run's numbers moved).
///
/// This class proves the two new lines the same three things <see cref="PrecisionFooterLineTests"/> proves
/// for the accumulation line, reusing that class's own sample builder and write helper rather than a second
/// copy of either: each is a plain excluded comment, never a declared variable; each explains itself in words
/// rather than printing a bare enum name; and changing its own diagnostic changes only its own line, and
/// changes it to the *correct* wording, checked by content and not merely by inequality (root BOOT.md's own
/// project rule, adopted 2026-09-21: "each asserted by content and not by inequality" - a diff that only
/// proves "some line changed" would not catch the two branches' wording being swapped).
/// </summary>
public class RunProvenanceFooterLineTests
{
    private static string FindLine(string[] lines, string label) =>
        Assert.Single(lines, l => l.Contains(label, StringComparison.Ordinal));

    // ---- plain comment, not a declared variable, excluded from the structural comparison -------------------

    [Theory]
    [InlineData(StreamLayout.Original)]
    [InlineData(StreamLayout.Independent)]
    public void StreamLayoutLineIsAPlainCommentBannerNotADeclaredVariable(StreamLayout layout)
    {
        var result = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, streams: layout);
        var line = FindLine(PrecisionFooterLineTests.WriteLines(result), "Stream layout");

        // Reused from StructuralTests, exactly as PrecisionFooterLineTests reuses it: if
        // ResultsMWriter.StreamLayoutCommentLine were ever emitted with an "=" (a declared MATLAB variable,
        // which the original does not have), DeclaredName would match here and
        // StructuralTests.PortResultsMMatchesOriginalInNamesAndCommentsInOrder would fail on all five
        // reference formulations, since no archived file names such a variable.
        Assert.Matches(StructuralTests.CommentBanner, line);
        Assert.False(StructuralTests.DeclaredName.IsMatch(line), $"'{line}' matches a declared-variable pattern.");
        Assert.False(StructuralTests.IsComparableCommentBanner(line));
    }

    [Theory]
    [InlineData(ExecutionMode.Reference)]
    [InlineData(ExecutionMode.Batched)]
    public void ExecutionModeLineIsAPlainCommentBannerNotADeclaredVariable(ExecutionMode mode)
    {
        var result = PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, mode: mode);
        var line = FindLine(PrecisionFooterLineTests.WriteLines(result), "Execution mode");

        Assert.Matches(StructuralTests.CommentBanner, line);
        Assert.False(StructuralTests.DeclaredName.IsMatch(line), $"'{line}' matches a declared-variable pattern.");
        Assert.False(StructuralTests.IsComparableCommentBanner(line));
    }

    // ---- explains itself, rather than printing a bare enum name --------------------------------------------

    [Fact]
    public void StreamLayoutLineExplainsItselfRatherThanPrintingABareEnumName()
    {
        var originalLine = FindLine(
            PrecisionFooterLineTests.WriteLines(
                PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, streams: StreamLayout.Original)),
            "Stream layout");
        var independentLine = FindLine(
            PrecisionFooterLineTests.WriteLines(
                PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, streams: StreamLayout.Independent)),
            "Stream layout");

        // Neither line is the enum's own ToString() dropped into the file.
        Assert.DoesNotContain("Original", originalLine, StringComparison.Ordinal);
        Assert.DoesNotContain("Independent", independentLine, StringComparison.Ordinal);

        // Each carries the phrase that distinguishes it from the other, and only that one - the positive
        // control a bare "these two lines differ" check cannot give: a wording swap between the two branches
        // would still make the lines differ, but would fail these four assertions.
        Assert.Contains("original program", originalLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("known statistical bias", originalLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("do not carry", originalLine, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("independent generator orbits", independentLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not carry", independentLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("known statistical bias", independentLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecutionModeLineExplainsItselfRatherThanPrintingABareEnumName()
    {
        var referenceLine = FindLine(
            PrecisionFooterLineTests.WriteLines(
                PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, mode: ExecutionMode.Reference)),
            "Execution mode");
        var batchedLine = FindLine(
            PrecisionFooterLineTests.WriteLines(
                PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, mode: ExecutionMode.Batched)),
            "Execution mode");

        Assert.DoesNotContain("Reference", referenceLine, StringComparison.Ordinal);
        Assert.DoesNotContain("Batched", batchedLine, StringComparison.Ordinal);

        Assert.Contains("refreshes the pocket histogram", referenceLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("after every completed attempt", referenceLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("freezes", referenceLine, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("freezes the pocket histogram", batchedLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("whole launch", batchedLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshes", batchedLine, StringComparison.OrdinalIgnoreCase);
    }

    // ---- changing one diagnostic changes only its own line, and to the correct wording ---------------------

    [Fact]
    public void ChangingTheStreamLayoutChangesOnlyItsOwnFooterLineWithTheCorrectWording()
    {
        var originalLines = PrecisionFooterLineTests.WriteLines(
            PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, streams: StreamLayout.Original));
        var independentLines = PrecisionFooterLineTests.WriteLines(
            PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, streams: StreamLayout.Independent));

        Assert.Equal(originalLines.Length, independentLines.Length);

        var differingIndices = Enumerable.Range(0, originalLines.Length)
            .Where(i => originalLines[i] != independentLines[i])
            .ToList();

        var onlyDifference = Assert.Single(differingIndices);
        Assert.Contains("Stream layout", originalLines[onlyDifference], StringComparison.Ordinal);
        Assert.Contains("Stream layout", independentLines[onlyDifference], StringComparison.Ordinal);
        Assert.Contains("known statistical bias", originalLines[onlyDifference], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not carry", independentLines[onlyDifference], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangingTheExecutionModeChangesOnlyItsOwnFooterLineWithTheCorrectWording()
    {
        var referenceLines = PrecisionFooterLineTests.WriteLines(
            PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, mode: ExecutionMode.Reference));
        var batchedLines = PrecisionFooterLineTests.WriteLines(
            PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64, mode: ExecutionMode.Batched));

        Assert.Equal(referenceLines.Length, batchedLines.Length);

        var differingIndices = Enumerable.Range(0, referenceLines.Length)
            .Where(i => referenceLines[i] != batchedLines[i])
            .ToList();

        var onlyDifference = Assert.Single(differingIndices);
        Assert.Contains("Execution mode", referenceLines[onlyDifference], StringComparison.Ordinal);
        Assert.Contains("Execution mode", batchedLines[onlyDifference], StringComparison.Ordinal);
        Assert.Contains("refreshes the pocket histogram", referenceLines[onlyDifference], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("freezes the pocket histogram", batchedLines[onlyDifference], StringComparison.OrdinalIgnoreCase);
    }
}
