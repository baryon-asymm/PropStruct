using System.Globalization;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The call-site half of the per-cycle plane's product order (<c>src/Statistics/ACCEPTANCE.md</c>, A9; the
/// arbiter's F3 of 2026-10-03). <c>PlaneFormsTests</c> holds each form of <c>CyclePlaneOrder</c> by its bits;
/// which form a call site picks, and in which order it passes the factors, no bit of any case sees: a
/// difference of one association is 1e-16 relative and the binary32 store after it absorbs it. This holds the
/// choice structurally, over the machinery of <see cref="CyclePlaneSiteMapTests"/>: every <c>order.</c> call of
/// <c>src/Statistics</c> ends its line with <c>// Fortran n</c>, and the association of its factors, read off
/// the <c>Original</c> expression of the form it calls (<c>CyclePlaneOrder.cs</c>) over the shapes of its
/// arguments, is a sub-expression of the <c>order</c> text of a row of
/// <c>CyclePlane.listing.generated.txt</c> at a cited line. A commutation is one order (<see cref="Shape"/>).
/// </summary>
public class CyclePlaneOrderSiteTests
{
    /// <summary>What the check found: the problems, and the number of calls it read.</summary>
    private sealed record Outcome(IReadOnlyList<string> Problems, int Calls);

    [Fact]
    public void EveryOrderCallCitesItsSiteAndPassesTheFactorsInTheTablesOrder()
    {
        var sources = CyclePlaneSiteMapTests.StatisticsSources();
        var outcome = Check(sources);
        Assert.True(outcome.Problems.Count == 0, string.Join(Environment.NewLine, outcome.Problems));
    }

    /// <summary>The check reads every call the source has, counted by a plain scan of the lines, not by its own pattern.</summary>
    [Fact]
    public void TheCheckReadsEveryOrderCallOfTheSource()
    {
        var sources = CyclePlaneSiteMapTests.StatisticsSources();
        var scanned = sources.Where(s => s.Name != "CyclePlaneOrder.cs").Sum(s => s.Lines.Sum(CallsOnLine));
        Assert.True(scanned > 0);
        Assert.Equal(scanned, Check(sources).Calls);
    }

    /// <summary>
    /// Red: a call site that passes its factors in another association, or calls another form, is named at its
    /// own line and at no other. Each row is one edit of one line, reverted by not being written.
    /// </summary>
    [Theory]
    [InlineData("CycleStatistics.cs", "order.Linear(quotient, k + 0.5, di);", "order.Linear(quotient, di, k + 0.5);")]
    [InlineData("CycleStatistics.cs", "order.Linear(fraction, di, k + 0.5)); // Fortran 1089", "order.Linear(k + 0.5, fraction, di)); // Fortran 1089")]
    [InlineData("CycleStatistics.cs", "order.Quadratic(quotient, (k + 0.5) * di);", "order.Linear(quotient, k + 0.5, di);")]
    [InlineData("CycleStatistics.cs", "order.Square(order.Fourth(cell) - md4)", "order.Square(order.Cube(cell) - md4)")]
    [InlineData("Categories.cs", "order.Fourth(cell) * qdoksoWorking[r][i]; // Fortran 853", "order.Cube(cell) * qdoksoWorking[r][i]; // Fortran 853")]
    [InlineData("Categories.cs", "order.Linear(qdoksoWorking[r][i], cellSize, i + 0.5)", "order.Linear(qdoksoWorking[r][i], i + 0.5, cellSize)")]
    public void ASwappedFactorOrderOrFormTurnsTheCheckRedAtItsOwnLine(string file, string original, string mutated)
    {
        var (sources, where) = Edited(file, original, mutated);
        var problems = Check(sources).Problems;
        Assert.NotEmpty(problems);
        Assert.All(problems, problem => Assert.StartsWith(where, problem, StringComparison.Ordinal));
    }

    /// <summary>Red: a call whose line cites no Fortran line.</summary>
    [Theory]
    [InlineData("CycleStatistics.cs", "order.Linear(quotient, k + 0.5, di); // Fortran 805", "order.Linear(quotient, k + 0.5, di);")]
    [InlineData("CycleStatistics.cs", "di); // Fortran 806: a register sum, never stored", "di);")]
    [InlineData("CycleStatistics.cs", "order.Cube(cell) * qks1[k]; // Fortran 969", "order.Cube(cell) * qks1[k];")]
    [InlineData("Categories.cs", "mdok3[r]) * qdoksoWorking[r][i]; // Fortran 865", "mdok3[r]) * qdoksoWorking[r][i];")]
    public void ARemovedCitationTurnsTheCheckRedAtItsOwnLine(string file, string original, string mutated)
    {
        var (sources, where) = Edited(file, original, mutated);
        var problems = Check(sources).Problems;
        Assert.NotEmpty(problems);
        Assert.All(problems, problem => Assert.Equal($"{where} cites no Fortran line", problem));
    }

    /// <summary>
    /// Red: the form itself. <c>Linear</c>'s <c>Original</c> expression associated the other way is read through
    /// every call that names it, so the three call sites of lines 805, 989 and 1089 and the rest all turn red.
    /// </summary>
    [Fact]
    public void AnEditedFormTurnsTheCallsThatNameItRed()
    {
        var (sources, _) = Edited("CyclePlaneOrder.cs", "original ? x * a * b :", "original ? x * (a * b) :");
        var problems = Check(sources).Problems;
        Assert.NotEmpty(problems);
        Assert.All(problems, problem => Assert.Contains("Linear", problem, StringComparison.Ordinal));
    }

    /// <summary>
    /// Right, on inputs whose answer is known beforehand: a commutation changes no bit, so the weight of
    /// <c>x·c²</c> moved to the other side of the product, and the factors of <c>x·a·b</c> exchanged inside the
    /// first product, are read as the order the table has.
    /// </summary>
    [Theory]
    [InlineData("CycleStatistics.cs", "order.Quadratic(quotient, (k + 0.5) * di);", "order.Quadratic(quotient, di * (k + 0.5));")]
    [InlineData("CycleStatistics.cs", "order.Linear(vkso[k], di, k + 0.5);", "order.Linear(di, vkso[k], k + 0.5);")]
    [InlineData("CycleStatistics.cs", "order.Linear(quotient, k + 0.5, di);", "order.Linear(k + 0.5, quotient, di);")]
    public void ACommutationIsTheSameOrder(string file, string original, string mutated)
    {
        var (sources, _) = Edited(file, original, mutated);
        var outcome = Check(sources);
        Assert.True(outcome.Problems.Count == 0, string.Join(Environment.NewLine, outcome.Problems));
    }

    private static Outcome Check(IReadOnlyList<CyclePlaneSiteMapTests.SourceFile> sources)
    {
        var forms = OrderForm.Read(sources.Single(s => s.Name == "CyclePlaneOrder.cs").Lines);
        var rows = CyclePlaneSiteMapTests.TableRows();
        var problems = new List<string>();
        var calls = CyclePlaneSiteMapTests.OrderCalls(sources);
        foreach (var call in calls)
        {
            var where = $"{call.File}:{call.SourceLine}";
            if (call.Cited.Count == 0)
            {
                problems.Add($"{where} cites no Fortran line");
                continue;
            }

            var lines = sources.Single(s => s.Name == call.File).Lines;
            var site = new ShapeReader(forms, lines, call.SourceLine - 1).Read(ExpressionParser.Parse(CallText(lines[call.SourceLine - 1], call.Column)));
            var table = rows
                .Where(row => row[2].Split('+').Select(n => int.Parse(n, CultureInfo.InvariantCulture)).Intersect(call.Cited).Any() && row[11] != "-")
                .Select(row => new ShapeReader(forms).Read(ExpressionParser.Parse(OrderText(row))))
                .ToList();
            if (table.Count == 0)
            {
                problems.Add($"{where}: no row of the table at Fortran {string.Join(", ", call.Cited)} has an order text");
            }
            else if (!table.SelectMany(shape => shape.Subtrees()).Contains(site.Canonical))
            {
                problems.Add($"{where}: {call.Method} reads as {site.Canonical}, the table at Fortran {string.Join(", ", call.Cited)} has {string.Join(" and ", table.Select(shape => shape.Canonical))}");
            }
        }

        return new Outcome(problems, calls.Count);
    }

    /// <summary>A row's <c>order</c> text without the <c>sum of</c> that marks a register sum.</summary>
    private static string OrderText(string[] row) => row[11].StartsWith("sum of ", StringComparison.Ordinal) ? row[11]["sum of ".Length..] : row[11];

    /// <summary>The call that starts at <paramref name="column"/> of <paramref name="line"/>, its parentheses balanced.</summary>
    private static string CallText(string line, int column)
    {
        var depth = 0;
        for (var i = line.IndexOf('(', column); i < line.Length; i++)
        {
            depth += line[i] == '(' ? 1 : line[i] == ')' ? -1 : 0;
            if (depth == 0)
            {
                return line[column..(i + 1)];
            }
        }

        throw new FormatException($"the call at column {column} is not closed: {line}");
    }

    private static int CallsOnLine(string line)
    {
        var calls = 0;
        for (var at = line.IndexOf("order.", StringComparison.Ordinal); at >= 0; at = line.IndexOf("order.", at + 1, StringComparison.Ordinal))
        {
            var end = at + "order.".Length;
            var name = end;
            while (name < line.Length && char.IsLetterOrDigit(line[name]))
            {
                name++;
            }

            if (name > end && char.IsUpper(line[end]) && name < line.Length && line[name] == '(')
            {
                calls++;
            }
        }

        return calls;
    }

    /// <summary>The sources with <paramref name="original"/> replaced by <paramref name="mutated"/> on the one line that holds it, and where that line is.</summary>
    private static (IReadOnlyList<CyclePlaneSiteMapTests.SourceFile> Sources, string Where) Edited(string file, string original, string mutated)
    {
        var sources = CyclePlaneSiteMapTests.StatisticsSources();
        var index = sources.FindIndex(s => s.Name == file);
        var lines = sources[index].Lines.ToArray();
        var held = Enumerable.Range(0, lines.Length).Where(i => lines[i].Contains(original, StringComparison.Ordinal)).ToList();
        Assert.True(held.Count == 1, $"{file} holds '{original}' on {held.Count} lines, not one");
        lines[held[0]] = lines[held[0]].Replace(original, mutated, StringComparison.Ordinal);
        sources[index] = sources[index] with { Lines = lines };
        return (sources, $"{file}:{held[0] + 1}");
    }
}
