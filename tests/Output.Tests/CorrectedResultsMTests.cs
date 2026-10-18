using System.Globalization;
using System.Text.RegularExpressions;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Output.Tests;

/// <summary>
/// The <c>results.m</c> row of BOOT.md, "## Corrected model" (ACCEPTANCE.md, "Corrected model"): the footer
/// line, the <c>fqmkm1</c> step comment, the condition labels 3 to 5 and <c>fineoxy_fr</c>, each under
/// <see cref="ModelKind.Corrected"/> only, on constructed results (the sample of
/// <see cref="PrecisionFooterLineTests.BuildWritableSample"/>) and, for <c>fineoxy_fr</c>, on a run of P312.
/// </summary>
public class CorrectedResultsMTests
{
    private const double DefaultAk1 = 0.5;
    private const double DefaultAk2 = 2.0;
    private const double DefaultAk4 = 4.7;
    private const int ConditionLabelWidth = 16;

    private static readonly Regex ConditionLine = new(@"^ %    (?<k>\d)\) (?<label>[^:]*):", RegexOptions.Compiled);

    private static readonly string InptDat =
        RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "inpt.dat");

    /// <summary>The writable sample as an <c>Original</c> result: <c>Model</c> <c>Original</c>, no ratios.</summary>
    private static SimulationResult OriginalSample() => PrecisionFooterLineTests.BuildWritableSample(PrecisionKind.Binary64);

    /// <summary>The writable sample as the simulator hands over a <c>Corrected</c> one: <c>Model</c> and the three ratios set.</summary>
    internal static SimulationResult CorrectedSample(double ak1 = DefaultAk1, double ak2 = DefaultAk2, double ak4 = DefaultAk4)
    {
        var sample = OriginalSample();
        return sample with
        {
            Diagnostics = sample.Diagnostics with { Model = ModelKind.Corrected },
            StoredSetup = sample.StoredSetup with { Ak1 = ak1, Ak2 = ak2, Ak4 = ak4 },
        };
    }

    private static SimulationResult WithGdoknsAndGdokleft(SimulationResult result, double gdokns, double gdokleft) =>
        result with
        {
            StoredSetup = result.StoredSetup with { HomogenizedOxidizerFraction = gdokns },
            LocalStructure = result.LocalStructure with { Gdokleft = gdokleft },
        };

    private const string Fqmkm1Comment = " % Density distribution function of MKM/Dok between Dok particles";
    private const string Fqmkm2Comment = " % Density distribution function of MKM/Dok between pockets";
    private const string CoefComment = " % Coefficient [(Lij/Ddok)+1] distribution";

    /// <summary>The archived line of the original's <c>results.m</c> (inpt) that starts with <paramref name="prefix"/>: the <c>Original</c> text, from the fixture.</summary>
    private static string ArchivedLine(string prefix) =>
        Assert.Single(
            File.ReadAllLines(RepositoryPaths.Resolve("tests", "Fixtures", "references", "inpt", "results.m.txt")),
            l => l.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>The label field (between <c>K) </c> and the colon) of an archived condition line.</summary>
    private static string ArchivedConditionLabel(int k) => ConditionLine.Match(ArchivedLine($" %    {k}) ")).Groups["label"].Value;

    /// <summary>The footer line as API.md holds it: the one fenced <c>text</c> block of "## Corrected model", leading blank included.</summary>
    private static string FooterLineOfApiMd()
    {
        var api = File.ReadAllText(RepositoryPaths.Resolve("src", "Output", "API.md"));
        var match = Regex.Match(api, @"^(?<indent> *)```text\r?\n\k<indent>(?<line>[^\r\n]*Model kind:[^\r\n]*)\r?\n\k<indent>```", RegexOptions.Multiline);
        Assert.True(match.Success, "API.md holds no fenced text block with the footer line.");
        return match.Groups["line"].Value;
    }

    private static string[] FooterLines(string[] lines) =>
        lines.Where(l => l.Contains("% Model kind:", StringComparison.Ordinal)).ToArray();

    internal static Dictionary<int, string> ConditionLabels(string[] lines) =>
        lines.Select(l => ConditionLine.Match(l))
            .Where(m => m.Success)
            .ToDictionary(m => int.Parse(m.Groups["k"].Value, CultureInfo.InvariantCulture), m => m.Groups["label"].Value);

    // ---- the footer line -------------------------------------------------------------------------------

    [Fact]
    public void ACorrectedResultWritesTheFooterLineOfApiMdVerbatimAfterTheExecutionModeLine()
    {
        var lines = PrecisionFooterLineTests.WriteLines(CorrectedSample());

        var footer = Assert.Single(FooterLines(lines));
        Assert.Equal(FooterLineOfApiMd(), footer);

        var footerIndex = Array.IndexOf(lines, footer);
        Assert.Contains("% Execution mode:", lines[footerIndex - 1], StringComparison.Ordinal);
    }

    [Fact]
    public void TheFooterLineHoldsNoEqualsSignAndNoDigitAndIsExcludedFromTheStructuralComparison()
    {
        var footer = Assert.Single(FooterLines(PrecisionFooterLineTests.WriteLines(CorrectedSample())));

        Assert.DoesNotContain('=', footer);
        Assert.DoesNotContain(footer, char.IsDigit);
        Assert.Matches(StructuralTests.CommentBanner, footer);
        Assert.DoesNotMatch(StructuralTests.DeclaredName, footer);
        Assert.False(StructuralTests.IsComparableCommentBanner(footer));
    }

    [Fact]
    public void AnOriginalResultWritesNoModelLine() =>
        Assert.Empty(FooterLines(PrecisionFooterLineTests.WriteLines(OriginalSample())));

    [Fact]
    public void ACorrectedResultAtDefaultRatiosAndNoLeftOxidizerDiffersFromAnOriginalOneInExactlyThreeLines()
    {
        var originalLines = PrecisionFooterLineTests.WriteLines(WithGdoknsAndGdokleft(OriginalSample(), 0.1, 0.0));
        var correctedLines = PrecisionFooterLineTests.WriteLines(WithGdoknsAndGdokleft(CorrectedSample(), 0.1, 0.0));

        // The footer is the first of the three: a line added, so it is taken out before the position-wise diff.
        var footer = Assert.Single(FooterLines(correctedLines));
        var withoutFooter = correctedLines.Where(l => l != footer).ToArray();
        Assert.Equal(originalLines.Length, withoutFooter.Length);

        var differing = Enumerable.Range(0, originalLines.Length).Where(i => originalLines[i] != withoutFooter[i]).ToList();

        Assert.Equal(2, differing.Count);
        Assert.StartsWith(" %    5) " + ArchivedConditionLabel(5) + ":", originalLines[differing[0]], StringComparison.Ordinal);
        Assert.StartsWith(" %    5) l > 4.7max(Dbase,Dok):", withoutFooter[differing[0]], StringComparison.Ordinal);
        Assert.Equal(ArchivedLine(Fqmkm1Comment), originalLines[differing[1]]);
        Assert.Equal(ArchivedLine(Fqmkm1Comment).Replace("(step = 0.01)", "(step = 0.001)", StringComparison.Ordinal), withoutFooter[differing[1]]);
    }

    // ---- the fqmkm1 step -------------------------------------------------------------------------------

    [Theory]
    [InlineData(ModelKind.Original, "(step = 0.01)")]
    [InlineData(ModelKind.Corrected, "(step = 0.001)")]
    public void TheFqmkm1CommentCarriesTheModelsStepAndFqmkm2AndCoefKeepTheirs(ModelKind model, string step)
    {
        var result = model == ModelKind.Corrected ? CorrectedSample() : OriginalSample();

        var lines = PrecisionFooterLineTests.WriteLines(result);

        // Every comment is the archived one; the Corrected fqmkm1 comment differs from it in its step alone.
        Assert.Contains(lines, l => l == ArchivedLine(Fqmkm1Comment).Replace("(step = 0.01)", step, StringComparison.Ordinal));
        Assert.Contains(lines, l => l == ArchivedLine(Fqmkm2Comment));
        Assert.Contains(lines, l => l == ArchivedLine(CoefComment));
    }

    // ---- the condition labels --------------------------------------------------------------------------

    [Fact]
    public void AnAk1OfPointFortyFiveReadsInConditionThreeUnderCorrectedAndPointFiveUnderOriginal()
    {
        var corrected = ConditionLabels(PrecisionFooterLineTests.WriteLines(CorrectedSample(ak1: 0.45)));
        var original = ConditionLabels(PrecisionFooterLineTests.WriteLines(OriginalSample()));

        Assert.Equal("Dbase < 0.45Dok ", corrected[3]);
        Assert.Equal(ArchivedConditionLabel(3), original[3]);
    }

    [Fact]
    public void ConditionFiveNamesTheMaximumOfDbaseAndDokAtTheDefaultRatioAndCarriesTheStoredFour()
    {
        var atDefault = ConditionLabels(PrecisionFooterLineTests.WriteLines(CorrectedSample()));
        var atThree = ConditionLabels(PrecisionFooterLineTests.WriteLines(CorrectedSample(ak4: 3.0)));

        Assert.Equal("l > 4.7max(Dbase,Dok)", atDefault[5]);
        Assert.Equal("l > 3.0max(Dbase,Dok)", atThree[5]);
    }

    [Fact]
    public void ConditionsThreeAndFourPrintTheStoredRatiosAndAtTheDefaultsAsUnderOriginal()
    {
        var corrected = ConditionLabels(PrecisionFooterLineTests.WriteLines(CorrectedSample()));
        var original = ConditionLabels(PrecisionFooterLineTests.WriteLines(OriginalSample()));
        var shifted = ConditionLabels(PrecisionFooterLineTests.WriteLines(CorrectedSample(ak1: 0.125, ak2: 2.25)));

        Assert.Equal(ArchivedConditionLabel(3), corrected[3]);
        Assert.Equal(ArchivedConditionLabel(4), corrected[4]);
        Assert.Equal(original[3], corrected[3]);
        Assert.Equal(original[4], corrected[4]);
        Assert.Equal("Dbase < 0.125Dok", shifted[3]);
        Assert.Equal("Dbase > 2.25Dok ", shifted[4]);
    }

    [Theory]
    [InlineData(ModelKind.Original)]
    [InlineData(ModelKind.Corrected)]
    public void EveryConditionLabelKeepsItsPrefixAndItsSixteenCharacterPadding(ModelKind model)
    {
        var result = model == ModelKind.Corrected ? CorrectedSample(ak1: 0.45) : OriginalSample();

        var labels = ConditionLabels(PrecisionFooterLineTests.WriteLines(result));

        Assert.Equal(Enumerable.Range(1, 9), labels.Keys.Order());
        foreach (var (k, label) in labels)
        {
            if (k == 5 && model == ModelKind.Corrected)
            {
                Assert.True(label.Length >= ConditionLabelWidth, $"condition 5's label '{label}' is shorter than {ConditionLabelWidth}.");
            }
            else
            {
                Assert.Equal(ConditionLabelWidth, label.Length);
            }
        }
    }

    [Fact]
    public void TheHarnessParserReadsTheCountsOfACorrectedFileAsOfAnOriginalOne()
    {
        var originalPath = Path.GetTempFileName();
        var correctedPath = Path.GetTempFileName();
        try
        {
            var formulation = DatFile.Read(InptDat);
            ResultsMWriter.Write(formulation, ModelParameters.Default, OriginalSample(), originalPath);
            ResultsMWriter.Write(formulation, ModelParameters.Default, CorrectedSample(ak1: 0.45, ak2: 2.25, ak4: 3.0), correctedPath);

            var original = ResultsMFile.Parse(originalPath);
            var corrected = ResultsMFile.Parse(correctedPath);

            Assert.Equal(original.Keys.Order(), corrected.Keys.Order());
            // The parser names a condition line by its number K (the key is ConditionBreaking(K), whatever the
            // label says); all nine are there under the Corrected labels, each read as under Original.
            var conditionKeys = Enumerable.Range(1, 9).Select(k => $"ConditionBreaking({k})").ToList();
            foreach (var key in conditionKeys)
            {
                Assert.Contains(key, corrected.Keys);
            }

            foreach (var key in conditionKeys)
            {
                Assert.Equal(original[key], corrected[key]);
            }
        }
        finally
        {
            File.Delete(originalPath);
            File.Delete(correctedPath);
        }
    }

    // ---- fineoxy_fr ------------------------------------------------------------------------------------

    private static ResultCell FineoxyCell(SimulationResult result)
    {
        var path = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(DatFile.Read(InptDat), ModelParameters.Default, result, path);
            return Assert.Single(ResultsMFile.ParseCells(path)["fineoxy_fr"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FineoxyFrIsTheShareOfTheWholeOxidizerUnderCorrectedAndTheOriginalSumUnderOriginal()
    {
        const double gdokns = 0.1;
        const double gdokleft = 0.122;
        var ggg = OriginalSample().Header.Ggg;

        var corrected = FineoxyCell(WithGdoknsAndGdokleft(CorrectedSample(), gdokns, gdokleft));
        var original = FineoxyCell(WithGdoknsAndGdokleft(OriginalSample(), gdokns, gdokleft));

        var correctedExpected = gdokns + (1 - gdokns) * gdokleft / ggg;
        var originalExpected = gdokns + gdokleft / ggg;
        Assert.InRange(corrected.Value, correctedExpected - corrected.Resolution, correctedExpected + corrected.Resolution);
        Assert.InRange(original.Value, originalExpected - original.Resolution, originalExpected + original.Resolution);

        var shift = gdokns * gdokleft / ggg;
        Assert.True(shift > 10 * original.Resolution, "the shift must be well above the print resolution for this check to see it");
        Assert.InRange(original.Value - corrected.Value, shift - 2 * original.Resolution, shift + 2 * original.Resolution);
    }

    [Fact]
    public void FineoxyFrIsTheSameUnderBothModelsWhenNoOxidizerIsLeft()
    {
        var corrected = FineoxyCell(WithGdoknsAndGdokleft(CorrectedSample(), 0.1, 0.0));
        var original = FineoxyCell(WithGdoknsAndGdokleft(OriginalSample(), 0.1, 0.0));

        Assert.Equal(original.Value, corrected.Value);
    }

    [Fact]
    public void FineoxyFrIsTheSameUnderBothModelsWhenNothingIsHomogenized()
    {
        var corrected = FineoxyCell(WithGdoknsAndGdokleft(CorrectedSample(), 0.0, 0.122));
        var original = FineoxyCell(WithGdoknsAndGdokleft(OriginalSample(), 0.0, 0.122));

        Assert.Equal(original.Value, corrected.Value);
    }

    /// <summary>
    /// Right once: a real run of P312 under <c>Corrected</c> with <c>gdokns = 0.1</c> prints the corrected
    /// share, computed here from the run's own <c>Gdokleft</c> and <c>Ggg</c>, and that differs from the
    /// original sum by <c>gdokns·gdokleft/GGG</c>, well above the print resolution (BOOT.md, "## Corrected
    /// model", Evidence).
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    public void ARunOfP312UnderCorrectedWithGdoknsPointOnePrintsTheCorrectedShare()
    {
        const double gdokns = 0.1;
        // P312's own 5000 particles per cycle take more than ten minutes of reference mode in a Debug build;
        // the printed share is checked against the run's own Gdokleft and Ggg, so any size serves.
        var formulation = DatFile.Read(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", "P312.dat")) with
        {
            ParticlesPerCycle = 50,
        };
        var parameters = ModelParameters.Default with { HomogenizedOxidizerFraction = gdokns };

        using var simulator = Simulator.Create(new SimulationOptions
        {
            Parameters = parameters,
            Model = ModelKind.Corrected,
            Mode = ExecutionMode.Reference,
            Accelerator = AcceleratorKind.Cpu,
            Seed = 0UL,
        });
        var result = simulator.Run(formulation);

        var path = Path.GetTempFileName();
        try
        {
            ResultsMWriter.Write(formulation, parameters, result, path);
            var cell = Assert.Single(ResultsMFile.ParseCells(path)["fineoxy_fr"]);

            var gdokleft = result.LocalStructure.Gdokleft;
            var ggg = result.Header.Ggg;
            Assert.True(gdokleft > 0, "P312 at gdokns = 0.1 must leave a non-homogenized oxidizer for this check to see the fix");

            var expected = gdokns + (1 - gdokns) * gdokleft / ggg;
            Assert.InRange(cell.Value, expected - cell.Resolution, expected + cell.Resolution);

            var originalSum = gdokns + gdokleft / ggg;
            var shift = gdokns * gdokleft / ggg;
            Assert.True(shift > 10 * cell.Resolution);
            Assert.InRange(originalSum - cell.Value, shift - 2 * cell.Resolution, shift + 2 * cell.Resolution);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- a Corrected result without its ratios ---------------------------------------------------------

    [Theory]
    [InlineData("Ak1")]
    [InlineData("Ak2")]
    [InlineData("Ak4")]
    public void ACorrectedResultWithANullRatioIsRefusedNamingTheMemberAndLeavesNoFile(string member)
    {
        var corrected = CorrectedSample();
        var broken = corrected with
        {
            StoredSetup = member switch
            {
                "Ak1" => corrected.StoredSetup with { Ak1 = null },
                "Ak2" => corrected.StoredSetup with { Ak2 = null },
                _ => corrected.StoredSetup with { Ak4 = null },
            },
        };
        var formulation = DatFile.Read(InptDat);
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var toPath = Assert.Throws<ArgumentException>(() => ResultsMWriter.Write(formulation, ModelParameters.Default, broken, path));

        Assert.Contains(member, toPath.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(path));

        using var writer = new StringWriter();
        var toWriter = Assert.Throws<ArgumentException>(() => ResultsMWriter.Write(formulation, ModelParameters.Default, broken, writer));
        Assert.Contains(member, toWriter.Message, StringComparison.Ordinal);
        Assert.Equal(string.Empty, writer.ToString());
    }
}
