using System.Text.Json;
using System.Text.Json.Serialization;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// Loads <c>docs/declared-differences.json</c> (<c>tools/defect-report/API.md</c>, "## The declared-differences
/// JSON") and matches its entries against the cells <see cref="StatisticalCriterion.CompareSets"/> finds
/// differing — the machine-readable twin of the tree's own "no silent fix" taboo: a difference
/// <c>CompareSets</c> finds must be named by at least one row here, or it is an undeclared defect, not a
/// covered one.
/// </summary>
internal sealed partial class DeclaredDifferences
{
    internal sealed record Entry(IReadOnlyList<string> Formulations, string Quantity, int? First, int? Last, double? OriginalBelow)
    {
        internal bool AppliesTo(string formulation) =>
            Formulations.Contains("*", StringComparer.Ordinal) || Formulations.Contains(formulation, StringComparer.Ordinal);

        // tools/defect-report/API.md, "## Input contract": "How a cell is matched" — every one of these must
        // hold; the range and the threshold are each optional (a null bound never excludes).
        internal bool Covers(string quantity, int index, double meanSecond) =>
            string.Equals(Quantity, quantity, StringComparison.Ordinal)
            && (First is not { } first || index >= first)
            && (Last is not { } last || index <= last)
            && (OriginalBelow is not { } threshold || Math.Abs(meanSecond) < threshold);
    }

    internal sealed record Row(string Node, string Fortran, string Kind, string Port, IReadOnlyList<Entry> Entries);

    internal IReadOnlyList<Row> Rows { get; }

    internal IReadOnlyList<Entry> AllEntries { get; }

    private DeclaredDifferences(IReadOnlyList<Row> rows)
    {
        Rows = rows;
        AllEntries = [.. rows.SelectMany(r => r.Entries)];
    }

    internal static DeclaredDifferences Load() => new(ReadRows());

    /// <summary>Whether at least one entry, scoped to <paramref name="formulation"/>, covers this cell — the
    /// subset check's own per-cell test.</summary>
    internal bool Covers(string formulation, string quantity, int index, double meanSecond) =>
        AllEntries.Any(e => e.AppliesTo(formulation) && e.Covers(quantity, index, meanSecond));

    /// <summary>Every entry scoped to <paramref name="formulation"/> (its own <c>formulations</c> names it, or
    /// <c>"*"</c>) — the found-check's own candidate set: a row that names a formulation but never covers a
    /// difference there has gone stale.</summary>
    internal IEnumerable<Entry> EntriesScopedTo(string formulation) => AllEntries.Where(e => e.AppliesTo(formulation));

    private static List<Row> ReadRows()
    {
        var path = RepositoryPaths.Resolve("docs", "declared-differences.json");
        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize(json, RawDocumentJsonContext.Default.RawDocument)
            ?? throw new InvalidOperationException($"'{path}' deserialized to null.");

        return [.. raw.Rows.Select(r => new Row(
            r.Node, r.Fortran, r.Kind, r.Port,
            [.. r.Entries.Select(e => new Entry(e.Formulations, e.Quantity, e.First, e.Last, e.OriginalBelow))]))];
    }

    private sealed record RawEntry(
        [property: JsonPropertyName("formulations")] List<string> Formulations,
        [property: JsonPropertyName("quantity")] string Quantity,
        [property: JsonPropertyName("first")] int? First,
        [property: JsonPropertyName("last")] int? Last,
        [property: JsonPropertyName("originalBelow")] double? OriginalBelow);

    private sealed record RawRow(
        [property: JsonPropertyName("node")] string Node,
        [property: JsonPropertyName("fortran")] string Fortran,
        [property: JsonPropertyName("kind")] string Kind,
        [property: JsonPropertyName("port")] string Port,
        [property: JsonPropertyName("entries")] List<RawEntry> Entries);

    private sealed record RawDocument(
        [property: JsonPropertyName("generator")] string Generator,
        [property: JsonPropertyName("rows")] List<RawRow> Rows);

    // Source-generated (the same reason RateCriterionTests.cs's own RateTableJsonContext exists): a plain
    // JsonSerializer.Deserialize<RawDocument> call gives CA1812 no evidence that RawDocument/RawRow/RawEntry are
    // ever constructed (reflection-based deserialization is invisible to it), and none may be removed — they are
    // docs/declared-differences.json's own documented shape (tools/defect-report/API.md).
    [JsonSerializable(typeof(RawDocument))]
    private sealed partial class RawDocumentJsonContext : JsonSerializerContext;
}
