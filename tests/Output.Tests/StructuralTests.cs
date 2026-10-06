using System.Text.RegularExpressions;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// L1 of the BOOT.md table: the port's <c>results.m</c> for each reference formulation equals the
/// original's in variable names, comment lines, order and array lengths; values (and, for computed
/// quantities, their printed digit shape) are checked by the statistical criterion, <c>tests/Simulation.Tests</c>,
/// not here.
///
/// Reference mode (<c>Original</c> layout, seed 0) is used because its generator draws are bit-identical to
/// the original's own (root BOOT.md, "The original generator, bit for bit"). The header up to "Cycles:" is a
/// pure echo of the formulation and the model parameters - no computation, no accumulation - so it is
/// compared shape for shape (BOOT.md's own <see cref="FortranFormat.SingleRealCore"/>-style digit-count
/// signature, not byte for byte: measured, HPEPA3's own <c>eps = 0.05</c> prints <c>5.0000001E-02</c> in the
/// archived reference (the original's REAL*4 storage of 0.05 is not exactly 0.05) against the port's
/// <c>5.0000000E-02</c> (the port's own design decision, "the port rounds its double... and does not first
/// convert it to binary32") - same shape, last digit only, exactly the difference that design decision
/// already names). Everything from "Cycles:" onward is a scalar or array built by accumulating over
/// every particle of the run; measured against all five reference formulations on 2026-09-19, this is *not*
/// stable enough for even a shape-only (digit-count) comparison against the archived reference run:
///
/// - a computed <c>F7.2</c> scalar can cross a digit boundary between two runs whose value differs only in
///   the last significant figure (HPEPA3 <c>Dkarm43_cor(2)</c>: reference <c>96.48</c> (2 integer digits),
///   the port's own reference-mode run <c>103.42</c> (3)) - not a formatting defect, since the values
///   themselves are only required to agree statistically (root BOOT.md, "The port agrees with the original
///   statistically"), not bit for bit;
/// - array lengths that depend on a running maximum over the whole run (<c>Ndok</c> via <c>Ddokmax</c>,
///   <c>DPmax</c>/<c>DPmax_cor</c>, <c>DPRow</c>, <c>coef_nmax</c>, <c>qmkm1_nmax</c>, <c>qmkm2_nmax</c>) can
///   differ by one cell near a boundary the same way root BOOT.md already documents for <c>Ndok</c> ("C166:
///   72 cells instead of 71"): measured, HMX's <c>fmkarm</c> block alone was 366 lines in the port's run
///   against 369 in the reference, and <c>pdoksmall</c>'s length (<c>Ndok - 1</c>) differed for three of the
///   five formulations.
///
/// What *is* stable regardless of any of that: every comment banner is static text, and every printed
/// quantity's own name/label is fixed by the formulation's shape (its fraction count, whether <c>SFR</c> is
/// read, whether <c>KXX &gt; 1</c>) rather than by any accumulated value. Those two are what this test checks
/// for the computed section - names and comments, in order - matching the criterion's own wording ("variable
/// names, comment lines, order") without the array-length/shape half, which the evidence above shows a live
/// re-run cannot promise against a fixed archived one.
/// </summary>
public class StructuralTests
{
    // A comment banner: starts with " %" and carries no "=" (so it is not a "% Dkarm = ... mkm" row header,
    // one per category and dropped separately below). Several of these embed an accumulated value on the
    // same line (e.g. "% Medium number of bridges:   4.363190") - stripped of its numbers via
    // NumberPlaceholder below, since that value is exactly the kind of run-dependent figure this test does
    // not compare (the class doc's first bullet).
    //
    // Internal, not private: PrecisionFooterLineTests reuses this exact predicate (and DeclaredName,
    // IsComparableCommentBanner below) to prove the port-specific precision-kind footer line
    // (ResultsMWriter.PrecisionCommentLine) is classified the same way the time line already is, rather
    // than duplicating the classification rule in a second file.
    internal static readonly Regex CommentBanner = new(@"^ %[^=]*$", RegexOptions.Compiled);

    // The name a scalar or array print declares, e.g. "Nbase =", "epsx(1)=", "fqdokkarm(  1,:) =": the
    // identifier (with its optional parenthesised index/slice) up to and including the "=" sign.
    internal static readonly Regex DeclaredName = new(@"^\s*([A-Za-z_][\w]*(?:\([^)]*\))?)\s*=", RegexOptions.Compiled);

    // Every number token in a line, replaced by a tag encoding only its digit counts (sign, integer digits,
    // fraction digits, exponent digits) - two lines with the same layout but different digits (including a
    // REAL*4-vs-double last-digit difference, see the class doc) compare equal.
    private static readonly Regex NumberToken = new(@"-?\d+(\.\d+)?(E[+-]\d+)?", RegexOptions.Compiled);

    private static string ShapeSignature(string line) =>
        NumberToken.Replace(line, m =>
        {
            var token = m.Value;
            var negative = token.StartsWith('-');
            if (negative)
            {
                token = token[1..];
            }

            var eIndex = token.IndexOf('E');
            var mantissa = eIndex < 0 ? token : token[..eIndex];
            var exponentDigits = eIndex < 0 ? -1 : token.Length - eIndex - 2; // - 'E' - sign
            var dotIndex = mantissa.IndexOf('.');
            var intDigits = dotIndex < 0 ? mantissa.Length : dotIndex;
            var fracDigits = dotIndex < 0 ? -1 : mantissa.Length - dotIndex - 1;
            var sign = negative ? "-" : string.Empty;
            return $"<{sign}{intDigits}.{fracDigits}e{exponentDigits}>";
        });

    // Every number replaced by a single fixed placeholder, discarding shape entirely - used for comment
    // banners that embed an accumulated value, where even the shape can cross a boundary between two runs
    // (the class doc's first bullet applies just as much to a comment's own trailing value).
    private static string NumberPlaceholder(string line) => NumberToken.Replace(line, "#");

    private static readonly string[] AllReferenceFormulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];

    public static IEnumerable<object[]> ReferenceFormulations() =>
        AllReferenceFormulations.Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    [Trait("Category", "Long")]
    public void PortResultsMMatchesOriginalInNamesAndCommentsInOrder(string name)
    {
        var datPath = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", name + ".dat");
        var referencePath = RepositoryPaths.Resolve("tests", "Fixtures", "references", name, "results.m.txt");
        var formulation = DatFile.Read(datPath);

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Streams = StreamLayout.Original,
            Seed = 0UL,
        });

        var result = simulator.Run(formulation);
        var tempPath = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, ModelParameters.Default, result, tempPath);

            var portLines = File.ReadAllLines(tempPath);
            var referenceLines = File.ReadAllLines(referencePath);

            var portEcho = TakeEchoSection(portLines, out var portRest);
            var referenceEcho = TakeEchoSection(referenceLines, out var referenceRest);
            Assert.Equal(referenceEcho.Count, portEcho.Count);
            for (var i = 0; i < referenceEcho.Count; i++)
            {
                Assert.True(
                    ShapeSignature(referenceEcho[i]) == ShapeSignature(portEcho[i]),
                    $"echo line {i + 1}: expected shape of \"{referenceEcho[i]}\", got \"{portEcho[i]}\"");
            }

            var referenceComments = referenceRest.Where(IsComparableCommentBanner).Select(NumberPlaceholder).ToList();
            var portComments = portRest.Where(IsComparableCommentBanner).Select(NumberPlaceholder).ToList();
            Assert.Equal(referenceComments, portComments);

            var referenceNames = ExtractDeclaredNames(referenceRest);
            var portNames = ExtractDeclaredNames(portRest);
            Assert.Equal(referenceNames, portNames);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    // "% Dkarm = ... mkm" precedes every fqdokkarm row (one per category) and is dropped here: its count is
    // DPRow, the run-dependent length this test does not compare (the class doc's second bullet).
    // "Calculation time", "Precision:", "Stream layout:" and "Execution mode:" are the four port-specific
    // footer lines with no counterpart in the original (root BOOT.md's own precedent for the first: "the
    // file already carries a port-specific time line"; src/Output/BOOT.md, "Design decisions (2026-09-19)",
    // the precision-kind footer added 2026-09-21 as "Accumulation:", renamed 2026-09-23, and the
    // stream-layout/execution-mode footers added the same day) - excluded for the same reason the time line
    // is, not compared against an archive that cannot have them.
    internal static bool IsComparableCommentBanner(string line) =>
        CommentBanner.IsMatch(line) && !line.Contains("% Dkarm =") && !line.Contains("Calculation time") &&
        !line.Contains("% Precision:") && !line.Contains("% Stream layout:") && !line.Contains("% Execution mode:");

    /// <summary>
    /// Everything up to (and excluding) " Nkarm =" (Fortran line 1233, <c>QKSS</c>): the formulation and
    /// model parameters as read, plus "% Cycles:" and "Nbase" (<c>KPRIS</c>, <c>N</c>, <c>FI = Cycles *
    /// ParticlesPerCycle</c>) - all of it fixed by the formulation alone, with no accumulation over any
    /// particle (<c>QKSS</c>, the very next line, is the first accepted-pocket count and does depend on the
    /// whole run). The input filename line is excluded (it echoes whatever path the caller used to read the
    /// formulation, not a model quantity, and this test's path does not match the archived run's own
    /// operator-typed name).
    /// </summary>
    private static List<string> TakeEchoSection(string[] lines, out List<string> rest)
    {
        var cut = Array.FindIndex(lines, l => l.Contains(" Nkarm ="));
        Assert.True(cut > 0, "'Nkarm =' was not found");

        var head = lines.Take(cut).Where(l => !l.Contains("Input filename")).ToList();
        rest = lines.Skip(cut).ToList();
        return head;
    }

    /// <summary>
    /// Every declared name in order, with the whole "fqdokkarm" row block (one declaration per category,
    /// <c>DPRow</c> of them) collapsed to a single "fqdokkarm" entry: its row count is exactly the kind of
    /// run-dependent length this test does not compare (per the class doc), and the "% Dkarm = ... mkm"
    /// comments that precede each row are dropped entirely for the same reason.
    /// </summary>
    private static List<string> ExtractDeclaredNames(List<string> lines)
    {
        var names = new List<string>();
        var sawFqdokkarm = false;
        foreach (var line in lines)
        {
            if (line.Contains("% Dkarm ="))
            {
                continue;
            }

            var match = DeclaredName.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var declared = match.Groups[1].Value;
            if (declared.StartsWith("fqdokkarm", StringComparison.Ordinal))
            {
                if (!sawFqdokkarm)
                {
                    names.Add("fqdokkarm");
                    sawFqdokkarm = true;
                }

                continue;
            }

            names.Add(declared);
        }

        return names;
    }
}
