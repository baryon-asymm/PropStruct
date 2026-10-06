using System.Globalization;
using PropStruct.Input;

namespace PropStruct.Cli;

/// <summary>
/// The rows <c>propstruct defaults</c> prints: one per <see cref="ModelParameters"/> property, in menu
/// order, read from the property by reflection so the printed value can never drift from
/// <see cref="ModelParameters.Default"/> itself (BOOT.md, Acceptance criteria: "`propstruct defaults`
/// prints exactly `ModelParameters.Default`, compared property by property by the same reflected list").
/// </summary>
internal static class DefaultsReport
{
    internal sealed record Row(int MenuNumber, string CanonicalFlag, IReadOnlyList<string> Aliases, object? Value);

    public static IReadOnlyList<Row> Build(ModelParameters parameters) =>
        FlagCatalog.ModelParameterFlags
            .OrderBy(flag => flag.MenuNumber)
            .Select(flag => new Row(flag.MenuNumber, flag.CanonicalFlag, flag.Aliases, flag.Property!.GetValue(parameters)))
            .ToList();

    public static string Render(ModelParameters parameters)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        foreach (var row in Build(parameters))
        {
            var aliases = row.Aliases.Count == 0 ? "-" : string.Join(", ", row.Aliases);
            writer.WriteLine(
                "[{0,2}] {1,-24} {2,-24} {3}",
                row.MenuNumber,
                row.CanonicalFlag,
                aliases,
                FlagValueFormatter.Format(row.Value));
        }

        return writer.ToString();
    }
}
