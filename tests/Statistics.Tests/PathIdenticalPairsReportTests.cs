using System.Globalization;
using System.Text.Json;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Control C2 of the per-cycle plane's register-lifetime row (`src/Statistics/BOOT.md`, "##
/// Defects of the original", rows 771-1175; the review of 2026-10-02): the original executable
/// and the port on the same input, `Original` layout, seed 1, `Original` precision, at the
/// `N` and `KXX` where every integer-printed cell of the two outputs agrees
/// (`tests/Fixtures/cycle-plane-pairs`, `tests/Fixtures/API.md`, "## Cycle-plane pairs"). The
/// two programs then took the same decisions in the same order, so every other cell, a value
/// the per-cycle plane computes from the same totals, is expected to print the same token. This
/// is a report and a ratchet: it lists every cell whose printed token differs, per formulation,
/// and goes red when that list changes in either direction (`PathIdenticalPairs.approved.txt`).
/// The target of the register-lifetime work is an empty list but for the cells another node
/// declares.
///
/// A cell is one printed number, read by <see cref="ResultsMFile.ParseCells"/>; two cells are
/// the same token when their value and print resolution are equal, so `-.332075E-03` and
/// `-0.332075E-03` are, a cell the other output lacks differs. A differing cell that
/// `docs/declared-differences.json` covers (`tools/defect-report/API.md`, "## Input contract",
/// "How a cell is matched": the quantity, the index range, the original's value) is listed
/// as declared, not counted.
/// </summary>
public class PathIdenticalPairsReportTests(ITestOutputHelper output)
{
    private const string ApprovedFileName = "PathIdenticalPairs.approved.txt";

    private static string PairsRoot => RepositoryPaths.Resolve("tests", "Fixtures", "cycle-plane-pairs");

    private static List<string> Formulations() =>
        [.. Directory.EnumerateDirectories(PairsRoot).Select(path => Path.GetFileName(path)).OrderBy(name => name, StringComparer.Ordinal)];

    private sealed record DeclaredEntry(string[] Formulations, string Quantity, int? First, int? Last, double? OriginalBelow)
    {
        internal bool Covers(string formulation, string quantity, int index, double original) =>
            (Formulations.Contains("*", StringComparer.Ordinal) || Formulations.Contains(formulation, StringComparer.Ordinal))
            && string.Equals(Quantity, quantity, StringComparison.Ordinal)
            && (First is not { } first || index >= first)
            && (Last is not { } last || index <= last)
            && (OriginalBelow is not { } below || Math.Abs(original) < below);
    }

    private static List<DeclaredEntry> LoadDeclared()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Resolve("docs", "declared-differences.json")));
        var entries = new List<DeclaredEntry>();
        foreach (var row in document.RootElement.GetProperty("rows").EnumerateArray())
        {
            foreach (var entry in row.GetProperty("entries").EnumerateArray())
            {
                entries.Add(new DeclaredEntry(
                    [.. entry.GetProperty("formulations").EnumerateArray().Select(f => f.GetString()!)],
                    entry.GetProperty("quantity").GetString()!,
                    entry.GetProperty("first").ValueKind == JsonValueKind.Null ? null : entry.GetProperty("first").GetInt32(),
                    entry.GetProperty("last").ValueKind == JsonValueKind.Null ? null : entry.GetProperty("last").GetInt32(),
                    entry.GetProperty("originalBelow").ValueKind == JsonValueKind.Null ? null : entry.GetProperty("originalBelow").GetDouble()));
            }
        }

        return entries;
    }

    private static bool SameToken(ResultCell original, ResultCell port) =>
        original.Value.Equals(port.Value) && original.Resolution.Equals(port.Resolution) && original.IsIntegerPrinted == port.IsIntegerPrinted;

    private static (IReadOnlyDictionary<string, ResultCell[]> Original, IReadOnlyDictionary<string, ResultCell[]> Port) Read(string formulation) =>
        (ResultsMFile.ParseCells(Path.Combine(PairsRoot, formulation, "original.m.txt")),
         ResultsMFile.ParseCells(Path.Combine(PairsRoot, formulation, "port.m.txt")));

    /// <summary>
    /// The premise of the pair: every integer-printed cell of the original is in the port's
    /// output and equal to it, so the two programs took the same decisions.
    /// </summary>
    [Fact]
    public void EveryIntegerPrintedCellOfEveryPairAgrees()
    {
        var formulations = Formulations();
        Assert.NotEmpty(formulations);
        foreach (var formulation in formulations)
        {
            var (original, port) = Read(formulation);
            var integerCells = 0;
            foreach (var (name, cells) in original)
            {
                for (var i = 0; i < cells.Length; i++)
                {
                    if (!cells[i].IsIntegerPrinted)
                    {
                        continue;
                    }

                    integerCells++;
                    Assert.True(port.TryGetValue(name, out var other) && i < other.Length && SameToken(cells[i], other[i]),
                        $"{formulation}: integer cell {name}[{i}] differs.");
                }
            }

            Assert.True(integerCells > 0, $"{formulation}: no integer-printed cell was compared.");
        }
    }

    private string BuildTable()
    {
        var declared = LoadDeclared();
        var lines = new List<string>();
        foreach (var formulation in Formulations())
        {
            var (original, port) = Read(formulation);
            var compared = 0;
            var differing = new List<string>();
            var declaredCells = new List<string>();
            foreach (var (name, cells) in original.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var others = port.GetValueOrDefault(name) ?? [];
                for (var i = 0; i < Math.Max(cells.Length, others.Length); i++)
                {
                    compared++;
                    if (i < cells.Length && i < others.Length && SameToken(cells[i], others[i]))
                    {
                        continue;
                    }

                    var label = $"{name}[{i}]";
                    var originalValue = i < cells.Length ? cells[i].Value : double.NaN;
                    var portValue = i < others.Length ? others[i].Value : double.NaN;
                    output.WriteLine($"{formulation} {label}: original {originalValue.ToString("R", CultureInfo.InvariantCulture)}, port {portValue.ToString("R", CultureInfo.InvariantCulture)}");
                    (declared.Any(d => d.Covers(formulation, name, i, originalValue)) ? declaredCells : differing).Add(label);
                }
            }

            foreach (var name in port.Keys.Where(key => !original.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal))
            {
                differing.Add($"{name}: only in the port");
            }

            lines.Add($"{formulation}: {differing.Count} differing of {compared} compared cells, {declaredCells.Count} more declared");
            lines.AddRange(differing.Select(label => $"{formulation} {label}"));
            lines.AddRange(declaredCells.Select(label => $"{formulation} {label} declared"));
        }

        return string.Concat(lines.Select(line => line + "\n"));
    }

    [Fact]
    public void TheListOfDifferingCellsIsTheApprovedOne()
    {
        var received = BuildTable();
        var approvedPath = RepositoryPaths.Resolve("tests", "Statistics.Tests", ApprovedFileName);
        var approved = File.Exists(approvedPath) ? File.ReadAllText(approvedPath).Replace("\r\n", "\n", StringComparison.Ordinal) : string.Empty;
        Assert.True(approved == received,
            $"the list of differing cells moved; if that is the intended change, replace {ApprovedFileName} with:\n{received}");
    }
}
