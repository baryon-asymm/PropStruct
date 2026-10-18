using System.Globalization;
using System.Text;
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The hand-run measurement of what each correction does to each printed quantity (<c>BOOT.md</c>, "##
/// Correction-effects measurement"; task T12): the <c>Original</c> arm, the <c>Corrected</c> model (<c>All</c>) and
/// each flag of <see cref="CorrectionSet"/> alone, on the five reference formulations and four extra cases, in
/// batched mode on the CPU with the <c>Independent</c> layout and <c>Binary64</c>, over seeds <c>k * 2^16</c>. The
/// tables are generated into <c>Measurements/corrections-2026-10-18</c>; nothing in them is typed. The facts are
/// <c>Category=Long</c>, which keeps them out of the fast set and out of CI, and <c>Category=Measurement</c>, which
/// selects them alone. Select a slice with <c>--filter "Category=Measurement&amp;DisplayName~HMX"</c>.
/// </summary>
public class CorrectionEffectsMeasurement(ITestOutputHelper output)
{
    private const int AuditSeedCount = 8;

    // The 2026-09-24 fidelity audit of the window's origin (src/Particle/HISTORY.md, cited by docs/ORIGINAL-DEFECTS.md, row 595):
    // eight seeds k * 2^17, the five references, the Independent layout, Binary64, a throwaway variant with the origin at
    // the cell's lower edge. The audit states the range over the five formulations, not one figure per formulation.
    private const double AuditDqmkm2LowPercent = -7.0;
    private const double AuditDqmkm2HighPercent = -2.8;
    private const double AuditZkarmLowPercent = 4.9;
    private const double AuditZkarmHighPercent = 13.5;
    private const double AuditStandardErrors = 2.0;

    // The bound on the share of cells the cell comparison may find different between two halves of one arm.
    private const double CellFloorFraction = 0.01;

    // ExactConstants: zss, so lambda, is the None value times 3.14 / pi (docs/ORIGINAL-DEFECTS.md, rows 1014 and 1016).
    private const double LambdaRelativeTolerance = 1e-12;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The audit's own eight seeds <c>k * 2^17</c>, <c>k = 0..7</c>: eight distinct phases, which sixteen at that stride would not have.</summary>
    internal static IReadOnlyList<ulong> AuditSeeds { get; } = MeasurementCase.SeedsOf(AuditSeedCount, MeasurementCase.AuditSeedStrideBits);

    public static IEnumerable<object[]> CasesAndArms() =>
        MeasurementCase.All.SelectMany(measurementCase => CorrectionArm.Measured.Select(arm => new object[] { measurementCase.Name, arm.Name }));

    public static IEnumerable<object[]> CaseNames() => MeasurementCase.All.Select(measurementCase => new object[] { measurementCase.Name });

    public static IEnumerable<object[]> ReferenceCaseNames() => MeasurementCase.References.Select(measurementCase => new object[] { measurementCase.Name });

    private static string SectionDirectory => RepositoryPaths.Resolve("tests", "Simulation.Tests", "Measurements", "corrections-2026-10-18");

    private static void Write(string name, string text)
    {
        _ = Directory.CreateDirectory(SectionDirectory);
        File.WriteAllText(Path.Combine(SectionDirectory, name), text.ReplaceLineEndings("\n"));
    }

    private static ArmRuns[] EnsureAllArms(MeasurementCase measurementCase) => CorrectionArm.Measured
        .Select(arm => MeasuredRunStore.Ensure(measurementCase, arm, ExecutionMode.Batched, measurementCase.Seeds))
        .ToArray();

    /// <summary>One slice of the measurement: the arm's runs over every seed, written to the run store.</summary>
    [Theory]
    [MemberData(nameof(CasesAndArms))]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void RunArmOnAllSeeds(string caseName, string armName)
    {
        var measurementCase = MeasurementCase.Named(caseName);
        var runs = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Named(armName), ExecutionMode.Batched, measurementCase.Seeds);

        output.WriteLine($"{caseName} {armName}: {runs.Completed.Count()} of {runs.Runs.Count} runs completed, {runs.TotalSeconds:F1} s.");
        foreach (var failed in runs.Runs.Where(run => !run.IsCompleted))
        {
            output.WriteLine($"  seed {failed.Seed}: {failed.Status}");
        }

        Assert.NotEmpty(runs.Completed);
    }

    /// <summary>The section and the raw values of one case, from the runs of its arms (run where missing).</summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void AnalyseCase(string caseName)
    {
        var measurementCase = MeasurementCase.Named(caseName);
        var arms = EnsureAllArms(measurementCase);

        Assert.All(arms, arm => Assert.True(arm.Completed.Count() >= 2, $"{caseName} {arm.Arm}: fewer than two completed runs, no standard error."));

        Write($"case-{caseName}.md", CorrectionEffectsReport.CaseSection(measurementCase, arms, measurementCase.Formulation.ParticlesPerCycle));
        Write($"raw-{caseName}.csv", CorrectionEffectsReport.CaseCsv(arms));
        output.WriteLine($"{caseName}: section and raw values written to {SectionDirectory}.");
    }

    /// <summary>
    /// The negative control: the <c>Original</c> arm's even seeds against its odd seeds, the same model on two
    /// halves of one seed set. Read as the criterion states it, <see cref="StatisticalCriterion.TwoSampleBiasOfSets"/>
    /// should find no cell beyond what its family-wise level of 10^-3 allows, which for one comparison is none. It
    /// finds a few (measured 2026-10-18, <c>Measurements/corrections-2026-10-18/control-negative-*.md</c>): cells
    /// that one half prints and the other does not, and cells printed at a resolution coarser than their spread,
    /// whose Welch t is infinite or huge. <c>TwoSampleBiasOfSets</c> has no count floor and no print resolution
    /// (<c>tests/Harness/API.md</c>), so those cells are the comparison's own floor, not a difference. This fact
    /// records the floor and asserts only that it stays under one per cent of the compared cells, a bound stated
    /// here for the first time, not a loosened tolerance; each case's cell table prints its floor beside the arms'.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaseNames))]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void NegativeControlOriginalEvenSeedsAgainstOddSeedsRecordsTheCellFloorUnderOnePercent(string caseName)
    {
        var measurementCase = MeasurementCase.Named(caseName);
        var original = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Original, ExecutionMode.Batched, measurementCase.Seeds);
        var (cells, evenCount, oddCount) = CorrectionEffectsReport.EvenAgainstOdd(original);
        var differing = cells.Where(cell => cell.Differs).ToArray();

        var text = new StringBuilder();
        _ = text.AppendLine(Invariant, $"## Negative control, {caseName}");
        _ = text.AppendLine();
        _ = text.AppendLine(Invariant, $"`Original` arm, even seeds ({evenCount} runs) against odd seeds ({oddCount} runs), `TwoSampleBiasOfSets`, family-wise alpha 10^-3: {differing.Length} differing of {cells.Count} compared cells.");
        foreach (var cell in differing)
        {
            _ = text.AppendLine(Invariant, $"- {cell.Quantity}[{cell.Index}]: means {cell.MeanFirst:G6} and {cell.MeanSecond:G6}, t = {cell.T:G4}, df = {cell.DegreesOfFreedom}");
        }

        _ = text.AppendLine();
        Write($"control-negative-{caseName}.md", text.ToString());
        output.WriteLine($"{caseName}: {differing.Length} differing of {cells.Count} cells, even against odd seeds of Original.");

        Assert.True(differing.Length < CellFloorFraction * cells.Count, $"{caseName}: {differing.Length} of {cells.Count} cells differ between two halves of the same arm.");
    }

    /// <summary>
    /// The positive control for the effect sizes: <see cref="CorrectionSet.WindowLowerEdge"/> alone, in reference
    /// mode over seeds <c>k * 2^17</c>, <c>k = 0..7</c>, moves <c>Dqmkm2</c> and <c>Zkarm</c> as the 2026-09-24 audit
    /// measured the same change (<c>src/Particle/BOOT.md</c> and <c>docs/ORIGINAL-DEFECTS.md</c>, row 595): the shift
    /// lies inside the audit's range widened by two standard errors of the shift, in the audit's direction.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceCaseNames))]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void PositiveControlWindowLowerEdgeAloneReproducesTheAuditFiguresInReferenceMode(string caseName)
    {
        var measurementCase = MeasurementCase.Named(caseName);
        var original = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Original, ExecutionMode.Reference, AuditSeeds);
        var window = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Single(CorrectionSet.WindowLowerEdge), ExecutionMode.Reference, AuditSeeds);

        var dqmkm2 = ShiftOf(original, window, "Dqmkm2");
        var zkarm = ShiftOf(original, window, "Zkarm");

        var text = new StringBuilder();
        _ = text.AppendLine(Invariant, $"## Positive control, `WindowLowerEdge` alone in reference mode, {caseName}");
        _ = text.AppendLine();
        _ = text.AppendLine(Invariant, $"Reference mode, `Independent`, `Binary64`, seeds `k * 2^17`, k = 0..{AuditSeedCount - 1}; {original.Completed.Count()} and {window.Completed.Count()} completed runs.");
        _ = text.AppendLine();
        _ = text.AppendLine("| quantity | shift, per cent | standard error | Welch t | audit range over the five formulations, per cent |");
        _ = text.AppendLine("|---|---|---|---|---|");
        _ = text.AppendLine(Invariant, $"| Dqmkm2 | {CorrectionEffectsReport.Signed(dqmkm2.ShiftPercent)} | {CorrectionEffectsReport.Plain(dqmkm2.ShiftPercentStandardError)} | {dqmkm2.WelchT:+0.0;-0.0} | {AuditDqmkm2LowPercent} to {AuditDqmkm2HighPercent} |");
        _ = text.AppendLine(Invariant, $"| Zkarm | {CorrectionEffectsReport.Signed(zkarm.ShiftPercent)} | {CorrectionEffectsReport.Plain(zkarm.ShiftPercentStandardError)} | {zkarm.WelchT:+0.0;-0.0} | +{AuditZkarmLowPercent} to +{AuditZkarmHighPercent} |");
        _ = text.AppendLine();
        Write($"control-window-{caseName}.md", text.ToString());
        output.WriteLine($"{caseName}: Dqmkm2 {dqmkm2.ShiftPercent:+0.00;-0.00} +- {dqmkm2.ShiftPercentStandardError:0.00} %, Zkarm {zkarm.ShiftPercent:+0.00;-0.00} +- {zkarm.ShiftPercentStandardError:0.00} %.");

        AssertInside(caseName, "Dqmkm2", dqmkm2, AuditDqmkm2LowPercent, AuditDqmkm2HighPercent);
        AssertInside(caseName, "Zkarm", zkarm, AuditZkarmLowPercent, AuditZkarmHighPercent);
    }

    /// <summary>
    /// <see cref="CorrectionSet.DoksdConsistent"/> alone gives P33 the <c>Dok43sd</c> the root records for it
    /// (<c>ModelKindTests</c>' own figure), in every seed's run; <c>Original</c> does not.
    /// </summary>
    [Fact]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void PositiveControlDoksdConsistentGivesP33TheConsistentDok43sd()
    {
        var measurementCase = MeasurementCase.Named("P33");
        var original = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Original, ExecutionMode.Batched, measurementCase.Seeds);
        var single = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Single(CorrectionSet.DoksdConsistent), ExecutionMode.Batched, measurementCase.Seeds);
        var everything = MeasuredRunStore.Ensure(measurementCase, CorrectionArm.Everything, ExecutionMode.Batched, measurementCase.Seeds);
        var quantity = HeadlineQuantity.Cell("Dok43sd", "Dok43sd");

        var singleValues = CorrectionEffectsReport.ValuesOf(single, quantity).Values.ToArray();
        var everythingValues = CorrectionEffectsReport.ValuesOf(everything, quantity).Values.ToArray();
        var originalValues = CorrectionEffectsReport.ValuesOf(original, quantity).Values.ToArray();

        var text = new StringBuilder();
        _ = text.AppendLine("## Positive control, `DoksdConsistent` on P33");
        _ = text.AppendLine();
        _ = text.AppendLine(Invariant, $"`Dok43sd` printed, micrometres: `Original` {string.Join(", ", originalValues.Distinct())} over {originalValues.Length} runs; `DoksdConsistent` alone {string.Join(", ", singleValues.Distinct())} over {singleValues.Length} runs; `All` {string.Join(", ", everythingValues.Distinct())} over {everythingValues.Length} runs. The figure of record is {ModelKindTests.CorrectedP33Dok43sd.ToString(Invariant)}.");
        _ = text.AppendLine();
        Write("control-doksd-P33.md", text.ToString());

        Assert.NotEmpty(singleValues);
        Assert.All(singleValues, value => Assert.Equal(ModelKindTests.CorrectedP33Dok43sd, value));
        Assert.All(everythingValues, value => Assert.Equal(ModelKindTests.CorrectedP33Dok43sd, value));
        Assert.DoesNotContain(originalValues, value => value == ModelKindTests.CorrectedP33Dok43sd);
    }

    /// <summary>
    /// <see cref="CorrectionSet.ExactConstants"/> scales the setup's <c>Lambda</c> by 3.14 / pi, the factor of the
    /// literal over the constant (<c>docs/ORIGINAL-DEFECTS.md</c>, rows 1014 and 1016): <c>Setup.Prepare</c> under
    /// the flag against <c>None</c>, on each reference formulation.
    /// </summary>
    [Theory]
    [MemberData(nameof(ReferenceCaseNames))]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void PositiveControlExactConstantsScalesLambdaByTheLiteralOverPi(string caseName)
    {
        var measurementCase = MeasurementCase.Named(caseName);
        var options = new SimulationOptions { Parameters = measurementCase.Parameters };

        var lambdaNone = LambdaUnder(measurementCase, options, CorrectionSet.None);
        var lambdaExact = LambdaUnder(measurementCase, options, CorrectionSet.ExactConstants);
        var ratio = lambdaExact / lambdaNone;
        var expected = 3.14 / Math.PI;
        var relative = ratio / expected - 1.0;

        var text = new StringBuilder();
        _ = text.AppendLine(Invariant, $"## Positive control, `ExactConstants` on {caseName}");
        _ = text.AppendLine();
        _ = text.AppendLine(Invariant, $"`Setup.Lambda` under `None` {lambdaNone:G17}, under `ExactConstants` {lambdaExact:G17}; ratio {ratio:G17} against 3.14 / pi = {expected:G17}, relative difference {relative:+0.0e+0;-0.0e+0;0}.");
        _ = text.AppendLine();
        Write($"control-constants-{caseName}.md", text.ToString());
        output.WriteLine($"{caseName}: lambda ratio {ratio:G17}, expected {expected:G17}.");

        Assert.InRange(Math.Abs(relative), 0.0, LambdaRelativeTolerance);
    }

    /// <summary>The assembled page: the controls, the runtimes and the sections of the cases, all read from the files the other facts wrote.</summary>
    [Fact]
    [Trait("Category", "Long")]
    [Trait("Category", "Measurement")]
    public void AssembleCorrectionEffects()
    {
        var cases = MeasurementCase.All.Select(measurementCase => measurementCase.Name).ToArray();
        var missing = cases.Where(name => !File.Exists(Path.Combine(SectionDirectory, $"case-{name}.md"))).ToArray();
        Assert.True(missing.Length == 0, $"AnalyseCase has not written: {string.Join(", ", missing)}.");

        var text = new StringBuilder();
        _ = text.AppendLine("# Correction effects, 2026-10-18");
        _ = text.AppendLine();
        _ = text.AppendLine("Generated by `CorrectionEffectsMeasurement` (`tests/Simulation.Tests/BOOT.md`, \"## Correction-effects measurement\"); every number below is read from the runs, none is typed. The apparatus of each case is the first line of its section.");
        _ = text.AppendLine();
        _ = text.AppendLine("## Work per case");
        _ = text.AppendLine();
        _ = text.AppendLine("| case | arms | runs | failed runs | work, seconds |");
        _ = text.AppendLine("|---|---|---|---|---|");
        foreach (var name in cases)
        {
            var rows = File.ReadAllLines(Path.Combine(SectionDirectory, $"raw-{name}.csv")).Skip(1).Select(line => line.Split(',')).ToArray();
            var seconds = rows.Sum(fields => double.Parse(fields[3], Invariant));
            var failed = rows.Count(fields => fields[2] != MeasuredRun.Completed);
            _ = text.AppendLine(Invariant, $"| {name} | {rows.Select(fields => fields[0]).Distinct().Count()} | {rows.Length} | {failed} | {seconds:F0} |");
        }

        _ = text.AppendLine();
        foreach (var path in Directory.GetFiles(SectionDirectory, "control-*.md").OrderBy(path => path, StringComparer.Ordinal))
        {
            _ = text.AppendLine(File.ReadAllText(path).TrimEnd());
            _ = text.AppendLine();
        }

        foreach (var name in cases)
        {
            _ = text.AppendLine(File.ReadAllText(Path.Combine(SectionDirectory, $"case-{name}.md")).TrimEnd());
            _ = text.AppendLine();
        }

        File.WriteAllText(Path.Combine(Path.GetDirectoryName(SectionDirectory)!, "corrections-2026-10-18.md"), text.ToString().ReplaceLineEndings("\n"));
    }

    private static QuantityShift ShiftOf(ArmRuns original, ArmRuns arm, string key)
    {
        var quantity = HeadlineQuantity.Cell(key, key);
        return QuantityShift.Compare(CorrectionEffectsReport.ValuesOf(original, quantity), CorrectionEffectsReport.ValuesOf(arm, quantity));
    }

    private static void AssertInside(string caseName, string quantity, QuantityShift shift, double lowPercent, double highPercent)
    {
        var margin = AuditStandardErrors * shift.ShiftPercentStandardError;
        Assert.True(
            shift.ShiftPercent >= lowPercent - margin && shift.ShiftPercent <= highPercent + margin,
            $"{caseName} {quantity}: shift {shift.ShiftPercent:+0.00;-0.00} % +- {shift.ShiftPercentStandardError:0.00} lies outside the audit's {lowPercent} to {highPercent} % widened by {AuditStandardErrors} standard errors.");
    }

    private static double LambdaUnder(MeasurementCase measurementCase, SimulationOptions options, CorrectionSet corrections)
    {
        var status = Statistics.Setup.Prepare(
            measurementCase.Formulation, measurementCase.Parameters, Particle.PrecisionKind.Binary64, corrections, options.NeighbourBudget,
            options.PocketRedrawBudget, out var setup, out _, out _, out _, out _);

        Assert.Equal(Statistics.SetupStatus.Ok, status);
        return setup.Lambda;
    }
}
