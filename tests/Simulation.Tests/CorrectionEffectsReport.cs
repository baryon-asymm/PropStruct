using System.Globalization;
using System.Text;
using PropStruct.Tests.Harness;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// Turns the runs of the arms of one case into the generated tables of
/// <c>Measurements/corrections-2026-10-18</c> (<c>BOOT.md</c>, "## Correction-effects measurement"): the
/// <c>Original</c> means, the shifts with their standard errors, the t statistics, and the cell comparison. Nothing
/// in the tables is typed.
/// </summary>
internal static class CorrectionEffectsReport
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The section of one case: its apparatus, the <c>Original</c> means and the three tables over the other arms.</summary>
    public static string CaseSection(MeasurementCase measurementCase, IReadOnlyList<ArmRuns> arms, int particlesPerCycle)
    {
        var original = arms.Single(arm => arm.Arm == CorrectionArm.Original.Name);
        var others = arms.Where(arm => arm.Arm != CorrectionArm.Original.Name).ToArray();
        var text = new StringBuilder();

        _ = text.AppendLine(Invariant, $"## {measurementCase.Name}");
        _ = text.AppendLine();
        var lastIndex = original.Seeds.Count - 1;
        _ = text.AppendLine(Invariant, $"{measurementCase.Description}. Batched mode, CPU accelerator, `Independent` layout, `Binary64`; seeds `k * 2^17`, k = 0..{lastIndex} ({original.Seeds.Count} seeds); {measurementCase.Formulation.Cycles} cycle(s) of {particlesPerCycle} particles; `Dmin` {Micrometres(measurementCase.Parameters.Dmin.Metres)} um, `Di` {Micrometres(measurementCase.Parameters.CellSize.Metres)} um, `Dj` {Micrometres(measurementCase.Parameters.CategoryStep.Metres)} um.");
        _ = text.AppendLine();
        _ = text.AppendLine(Invariant, $"Work: {arms.Sum(arm => arm.TotalSeconds):F0} s over {arms.Count} arms ({string.Join(", ", arms.Select(arm => $"{arm.Arm} {arm.TotalSeconds:F0}"))} s).");
        _ = text.AppendLine();
        AppendFailures(text, arms);

        _ = text.AppendLine("### Original arm: mean, standard error and standard deviation over the seeds");
        _ = text.AppendLine();
        _ = text.AppendLine("| quantity | mean | standard error | standard deviation |");
        _ = text.AppendLine("|---|---|---|---|");
        foreach (var quantity in HeadlineQuantity.All)
        {
            var values = ValuesOf(original, quantity).Values.ToArray();
            if (values.Length < 2)
            {
                _ = text.AppendLine(Invariant, $"| {quantity.Label} | n/a | n/a | n/a |");
                continue;
            }

            var (mean, variance) = QuantityShift.MeanAndVariance(values);
            _ = text.AppendLine(Invariant, $"| {quantity.Label} | {Plain(mean)} | {Plain(Math.Sqrt(variance / values.Length))} | {Plain(Math.Sqrt(variance))} |");
        }

        _ = text.AppendLine();
        var columns = HeadlineQuantity.All.Select(quantity => quantity.Label).Append("pdoksmall, cell moving most").ToArray();
        var shifts = others.ToDictionary(arm => arm.Arm, arm => ShiftsOf(original, arm));

        _ = text.AppendLine("### Shift against `Original`, per cent of its mean, plus or minus the standard error of the shift");
        _ = text.AppendLine();
        _ = text.AppendLine("`*`: |Welch t| above the two-sided critical value at 10^-3 for that quantity alone (not family-wise). `abs`: the `Original` mean is zero, so the shift is absolute. The last column names the cell `[i]` of the printed `pdoksmall` array whose |Welch t| is largest among those the `Original` arm prints above 1e-30.");
        _ = text.AppendLine();
        AppendTable(text, columns, others, arm => shifts[arm.Arm].Select(shift => shift.Shift).ToArray());

        _ = text.AppendLine("### Welch t, with the paired t over the shared seeds in brackets");
        _ = text.AppendLine();
        AppendTable(text, columns, others, arm => shifts[arm.Arm].Select(shift => shift.T).ToArray());

        _ = text.AppendLine("### Cells: `TwoSampleBiasOfSets` against `Original`, family-wise alpha 10^-3");
        _ = text.AppendLine();
        _ = text.AppendLine("The category-indexed families (`fqdokkarm`, `Dkarmcat`, `dokkarm43`, `dokkarm10`, `coef`) have no canonical axis here (`tests/Harness/API.md`), so an arm that changes the category count moves them by construction.");
        _ = text.AppendLine();
        _ = text.AppendLine("| arm | differing / compared | quantities with a differing cell (count) |");
        _ = text.AppendLine("|---|---|---|");
        foreach (var arm in others)
        {
            var cells = StatisticalCriterion.TwoSampleBiasOfSets(
                original.Completed.Select(run => run.Cells).ToArray(),
                arm.Completed.Select(run => run.Cells).ToArray());
            var differing = cells.Where(cell => cell.Differs).GroupBy(cell => cell.Quantity)
                .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).ToArray();
            var names = differing.Length == 0
                ? "none"
                : string.Join(", ", differing.Take(8).Select(group => $"{group.Key} ({group.Count()})")) + (differing.Length > 8 ? $", and {differing.Length - 8} more" : string.Empty);
            _ = text.AppendLine(Invariant, $"| {arm.Arm} | {differing.Sum(group => group.Count())} / {cells.Count} | {names} |");
        }

        _ = text.AppendLine();
        return text.ToString();
    }

    /// <summary>The per-seed headline values of every run of every arm of a case, one row per run, for anyone who wants to re-aggregate.</summary>
    public static string CaseCsv(IReadOnlyList<ArmRuns> arms)
    {
        var text = new StringBuilder();
        _ = text.AppendLine("arm,seed,status,elapsed_s," + string.Join(',', HeadlineQuantity.All.Select(quantity => quantity.Label)));
        foreach (var arm in arms)
        {
            foreach (var run in arm.Runs)
            {
                var values = HeadlineQuantity.All.Select(quantity => quantity.Read(run.Cells)?.ToString("G17", Invariant) ?? string.Empty);
                _ = text.AppendLine(Invariant, $"{arm.Arm},{run.Seed},{run.Status},{run.ElapsedSeconds:F3},{string.Join(',', values)}");
            }
        }

        return text.ToString();
    }

    /// <summary>Seed to value of <paramref name="quantity"/> over the runs of <paramref name="arm"/> that completed and print it.</summary>
    public static Dictionary<ulong, double> ValuesOf(ArmRuns arm, HeadlineQuantity quantity)
    {
        var values = new Dictionary<ulong, double>();
        foreach (var run in arm.Completed)
        {
            if (quantity.Read(run.Cells) is { } value)
            {
                values[run.Seed] = value;
            }
        }

        return values;
    }

    private static (string Shift, string T)[] ShiftsOf(ArmRuns original, ArmRuns arm)
    {
        var rows = new List<(string Shift, string T)>();
        foreach (var quantity in HeadlineQuantity.All)
        {
            var shift = QuantityShift.Compare(ValuesOf(original, quantity), ValuesOf(arm, quantity));
            rows.Add((FormatShift(shift, prefix: string.Empty), FormatT(shift)));
        }

        var (strongest, strongestShift) = StrongestPdoksmallShift(original, arm);
        rows.Add((FormatShift(strongestShift, $"[{strongest.Index}] "), FormatT(strongestShift)));
        return rows.ToArray();
    }

    private static (HeadlineQuantity Quantity, QuantityShift Shift) StrongestPdoksmallShift(ArmRuns original, ArmRuns arm)
    {
        var length = original.Completed.Concat(arm.Completed)
            .Select(run => run.Cells.TryGetValue("pdoksmall", out var cells) ? cells.Length : 0)
            .DefaultIfEmpty(0).Min();

        HeadlineQuantity? best = null;
        var bestShift = default(QuantityShift);
        var bestT = -1.0;
        for (var index = 0; index < length; index++)
        {
            var quantity = HeadlineQuantity.StrongestPdoksmallCell(index);
            var shift = QuantityShift.Compare(ValuesOf(original, quantity), ValuesOf(arm, quantity));
            if (!shift.IsRelative)
            {
                continue;
            }

            var magnitude = double.IsNaN(shift.WelchT) ? 0.0 : Math.Abs(shift.WelchT);
            if (magnitude > bestT)
            {
                best = quantity;
                bestShift = shift;
                bestT = magnitude;
            }
        }

        return best is null ? (HeadlineQuantity.StrongestPdoksmallCell(0), default) : (best, bestShift);
    }

    private static void AppendTable(StringBuilder text, string[] columns, ArmRuns[] arms, Func<ArmRuns, string[]> row)
    {
        _ = text.AppendLine("| arm | " + string.Join(" | ", columns) + " |");
        _ = text.AppendLine("|---|" + string.Concat(columns.Select(_ => "---|")));
        foreach (var arm in arms)
        {
            _ = text.AppendLine(Invariant, $"| {arm.Arm} | {string.Join(" | ", row(arm))} |");
        }

        _ = text.AppendLine();
    }

    private static void AppendFailures(StringBuilder text, IReadOnlyList<ArmRuns> arms)
    {
        var failed = arms.SelectMany(arm => arm.Runs.Where(run => !run.IsCompleted).Select(run => $"{arm.Arm} seed {run.Seed}: {run.Status}")).ToArray();
        _ = text.AppendLine(failed.Length == 0 ? "Failed runs: none." : "Failed runs: " + string.Join("; ", failed) + ".");
        _ = text.AppendLine();
    }

    private static string FormatShift(QuantityShift shift, string prefix)
    {
        if (shift.OriginalCount < 2 || shift.ArmCount < 2)
        {
            return "n/a";
        }

        var star = shift.IsStarred ? "*" : string.Empty;
        var core = shift.IsRelative
            ? $"{Signed(shift.ShiftPercent)} +- {Plain(shift.ShiftPercentStandardError)}"
            : $"abs {Signed(shift.Difference)} +- {Plain(shift.DifferenceStandardError)}";
        return prefix + core + star;
    }

    private static string FormatT(QuantityShift shift) =>
        shift.OriginalCount < 2 || shift.ArmCount < 2 ? "n/a" : $"{SignedT(shift.WelchT)} ({SignedT(shift.PairedT)})";

    private static string Micrometres(double metres) => (metres * 1e6).ToString("0.###", Invariant);

    /// <summary>A mean or an error: fixed point in the middle of the range, scientific outside it.</summary>
    public static string Plain(double value) =>
        value == 0.0 ? "0" : Math.Abs(value) is >= 0.01 and < 1e4 ? value.ToString("0.000", Invariant) : value.ToString("0.000e+0", Invariant);

    /// <summary>As <see cref="Plain"/>, with an explicit sign.</summary>
    public static string Signed(double value) =>
        value == 0.0 ? "0" : Math.Abs(value) is >= 0.01 and < 1e4 ? value.ToString("+0.000;-0.000", Invariant) : value.ToString("+0.000e+0;-0.000e+0", Invariant);

    private static string SignedT(double t) =>
        double.IsNaN(t) ? "nan" : double.IsInfinity(t) ? (t > 0 ? "+inf" : "-inf") : t.ToString("+0.0;-0.0;0.0", Invariant);
}
