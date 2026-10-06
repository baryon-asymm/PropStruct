using System.Collections.Immutable;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The precision-kind footer line `ResultsMWriter` writes beside the time line (root BOOT.md,
/// "Precision kind is an option of every run": "The kind is recorded in the run's result and in
/// <c>results.m</c>, so no number leaves the program unlabelled"). Two things this level must prove, named
/// by the design session that approved the line's shape: the line explains itself to a reader who has never
/// seen the option, not a bare enum name; and it stays a plain comment excluded from the structural
/// comparison exactly the way the time line already is - the mistake this arrangement invites is emitting it
/// as a declared MATLAB variable, which would make `StructuralTests`' own name-list comparison fail on every
/// reference formulation (no such name exists in the archive).
/// </summary>
public class PrecisionFooterLineTests
{
    private static readonly string DatPath =
        RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "inpt.dat");

    /// <summary>
    /// <see cref="JsonRoundTripTests.BuildSample"/>, patched to be finite throughout. That sample
    /// deliberately carries a <c>NaN</c> (<c>Histograms.Pdoksmall[2]</c>) and the two infinities
    /// (<c>Convergence.ConvergenceAlldoksd</c>) to stress the JSON round trip
    /// (<c>JsonNumberHandling.AllowNamedFloatingPointLiterals</c>) - values `ResultsMWriter`'s own Fortran
    /// number formatting was never asked to print (a real run never produces them here) and does not handle
    /// (`FortranFormat.RoundToSignificantDigits` throws building the exponent for a non-finite value, found
    /// running this test against the two non-finite cells before this patch existed). Reusing the one sample
    /// builder with only the non-finite cells and the kind patched, rather than a second hand-built literal.
    /// Internal, not private, and taking the stream layout and execution mode as well as the precision
    /// kind: <see cref="RunProvenanceFooterLineTests"/> reuses this exact sanitisation to test the
    /// stream-layout and execution-mode footer lines, rather than a second copy of the NaN/infinity patch.
    /// Omitted parameters keep the sample's own values (<c>Streams = Original</c>, <c>Mode = Batched</c>),
    /// unchanged from before this method took them.
    /// </summary>
    internal static SimulationResult BuildWritableSample(
        PrecisionKind kind, StreamLayout? streams = null, ExecutionMode? mode = null)
    {
        var sample = JsonRoundTripTests.BuildSample();
        return sample with
        {
            Diagnostics = sample.Diagnostics with
            {
                Precision = kind,
                Streams = streams ?? sample.Diagnostics.Streams,
                Mode = mode ?? sample.Diagnostics.Mode,
            },
            Histograms = sample.Histograms with { Pdoksmall = ImmutableArray.Create(0.0, 0.141, 0.0) },
            Convergence = sample.Convergence is null
                ? null
                : sample.Convergence with { ConvergenceAlldoksd = ImmutableArray.Create(0.0, 0.0) },
        };
    }

    /// <summary>
    /// Writes <paramref name="result"/> against <see cref="DatPath"/> and reads the lines back. Internal, not
    /// private, for the same reason as <see cref="BuildWritableSample"/>: <see cref="RunProvenanceFooterLineTests"/>
    /// reuses it rather than a second temp-file-write-and-read helper. <paramref name="parameters"/>, when
    /// given, replaces <see cref="ModelParameters.Default"/> - <see cref="TailProbabilityHeaderTests"/> needs a
    /// caller-supplied <c>TailProbability</c> distinct from the sample's own <c>Header.TailProbabilityModified</c>,
    /// to tell apart printing the raw menu parameter from printing the reduced header value.
    /// </summary>
    internal static string[] WriteLines(SimulationResult result, ModelParameters? parameters = null)
    {
        var formulation = DatFile.Read(DatPath);

        var path = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, parameters ?? ModelParameters.Default, result, path);
            return File.ReadAllLines(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string[] WriteLines(PrecisionKind kind) => WriteLines(BuildWritableSample(kind));

    private static string FindPrecisionLine(string[] lines) =>
        Assert.Single(lines, l => l.Contains("Precision", StringComparison.Ordinal));

    [Theory]
    [InlineData(PrecisionKind.Binary64)]
    [InlineData(PrecisionKind.Original)]
    public void PrecisionLineIsAPlainCommentBannerNotADeclaredVariable(PrecisionKind kind)
    {
        var line = FindPrecisionLine(WriteLines(kind));

        // Reused from StructuralTests, not re-typed: this is the exact classification the acceptance
        // criterion's own comparison runs (root BOOT.md, `results.m` "holds the same variable names ...
        // order"; tests/Output.Tests/BOOT.md's L1 row). If PrecisionCommentLine were ever emitted with an
        // "=" (a declared MATLAB variable, which the original does not have), DeclaredName would match here
        // and StructuralTests.PortResultsMMatchesOriginalInNamesAndCommentsInOrder would fail on all five
        // reference formulations, since no archived file names such a variable.
        Assert.Matches(StructuralTests.CommentBanner, line);
        Assert.False(StructuralTests.DeclaredName.IsMatch(line), $"'{line}' matches a declared-variable pattern.");

        // And it is excluded from the structural comment-list comparison the same way the time line is -
        // proving this test exercises the real exclusion rule, not a private copy of it.
        Assert.False(StructuralTests.IsComparableCommentBanner(line));
    }

    [Fact]
    public void PrecisionLineExplainsItselfRatherThanPrintingABareEnumName()
    {
        var binary64Line = FindPrecisionLine(WriteLines(PrecisionKind.Binary64));
        var originalLine = FindPrecisionLine(WriteLines(PrecisionKind.Original));

        // Neither line is the enum's own ToString() dropped into the file: a reader who has never seen
        // --precision/SimulationOptions.Precision is told what happened in words, not handed a label.
        Assert.DoesNotContain("Binary64", binary64Line, StringComparison.Ordinal);
        Assert.DoesNotContain("Original", originalLine, StringComparison.Ordinal);

        Assert.Contains("double precision", binary64Line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not reproduced", binary64Line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reproduces", originalLine, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REAL*4", originalLine, StringComparison.Ordinal);
    }

    /// <summary>
    /// Read "changes only its own footer line and the eps header echo" between 2026-09-23 and 2026-09-24:
    /// while <c>ResultsMWriter</c> itself rounded <c>eps</c> from <c>PrecisionKind</c>, toggling this sample's
    /// <see cref="RunDiagnostics.Precision"/> alone (with <see cref="SimulationResult.StoredSetup"/> held
    /// fixed) moved that line too. BOOT.md's own "Design decision (2026-09-24): print the stored setup" moved
    /// the rounding upstream, into <c>Statistics.Setup.Prepare</c> (`EpsHeaderEchoTests` now covers that
    /// header line's own behaviour, unrounded pass-through of whatever <c>StoredSetup.EpsDok</c> already
    /// carries): this writer no longer reads <see cref="RunDiagnostics.Precision"/> for anything but the
    /// footer comment itself, so toggling it with <c>StoredSetup</c> held fixed changes exactly that one line
    /// again, the original, narrower claim this test's name states.
    /// </summary>
    [Fact]
    public void ChangingThePrecisionKindChangesOnlyItsOwnFooterLine()
    {
        var doubleLines = WriteLines(PrecisionKind.Binary64);
        var originalLines = WriteLines(PrecisionKind.Original);

        Assert.Equal(doubleLines.Length, originalLines.Length);

        var differingIndices = Enumerable.Range(0, doubleLines.Length)
            .Where(i => doubleLines[i] != originalLines[i])
            .ToList();

        var precisionLine = Assert.Single(differingIndices);
        Assert.Contains("Precision", doubleLines[precisionLine], StringComparison.Ordinal);
        Assert.Contains("Precision", originalLines[precisionLine], StringComparison.Ordinal);
    }
}
