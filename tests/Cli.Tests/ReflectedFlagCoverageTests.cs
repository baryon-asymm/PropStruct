using System.Reflection;
using PropStruct.Input;
using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table: "every property of <see cref="ModelParameters"/> and of
/// <see cref="SimulationOptions"/> is reachable as a flag and every flag has a property, both ways" -
/// built from <see cref="System.Reflection"/> over the two records' own properties, never a typed list
/// (AGENTS.md §6, the "all" quantifier).
/// </summary>
public class ReflectedFlagCoverageTests
{
    private static IEnumerable<string> PublicInstanceProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name);

    [Fact]
    public void EveryModelParametersPropertyHasExactlyOneFlag()
    {
        var properties = PublicInstanceProperties(typeof(ModelParameters)).ToHashSet();
        var flagged = FlagCatalog.ModelParameterFlags.Select(f => f.PropertyName).ToList();

        Assert.Equal(properties.Count, flagged.Count);
        Assert.Equal(properties.OrderBy(n => n), flagged.OrderBy(n => n));
        Assert.Equal(flagged.Count, flagged.Distinct().Count());
    }

    [Fact]
    public void EveryModelParameterFlagHasAMenuNumberFromOneToFourteen()
    {
        var menuNumbers = FlagCatalog.ModelParameterFlags.Select(f => f.MenuNumber).OrderBy(n => n).ToList();
        Assert.Equal(Enumerable.Range(1, 14), menuNumbers);
    }

    [Fact]
    public void EverySimulationOptionsPropertyHasAFlagOrIsDeliberatelyUnflagged()
    {
        var properties = PublicInstanceProperties(typeof(SimulationOptions)).ToHashSet();
        var flagged = FlagCatalog.SimulationOptionFlags.Select(f => f.PropertyName).ToHashSet();
        var unflagged = FlagCatalog.DeliberatelyUnflagged
            .Where(u => u.Owner == FlagOwner.SimulationOptions)
            .Select(u => u.PropertyName)
            .ToHashSet();

        Assert.Empty(flagged.Intersect(unflagged));
        Assert.Equal(properties.OrderBy(n => n, StringComparer.Ordinal), flagged.Union(unflagged).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void TheDeliberatelyUnflaggedListIsExactlyOneEntryWithAReason()
    {
        var entry = Assert.Single(FlagCatalog.DeliberatelyUnflagged);
        Assert.Equal(FlagOwner.SimulationOptions, entry.Owner);
        Assert.Equal(nameof(SimulationOptions.Parameters), entry.PropertyName);
        Assert.False(string.IsNullOrWhiteSpace(entry.Reason));
    }

    [Fact]
    public void EveryFlagSpellingIsUniqueAcrossTheWholeCatalog()
    {
        var spellings = FlagCatalog.AllValueAndSwitchFlags.SelectMany(f => f.Spellings).ToList();
        Assert.Equal(spellings.Count, spellings.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DefaultsReportMatchesModelParametersDefaultPropertyByProperty()
    {
        var rows = DefaultsReport.Build(ModelParameters.Default);
        Assert.Equal(14, rows.Count);

        foreach (var flag in FlagCatalog.ModelParameterFlags)
        {
            var row = Assert.Single(rows, r => r.MenuNumber == flag.MenuNumber);
            var expected = flag.Property!.GetValue(ModelParameters.Default);
            Assert.Equal(expected, row.Value);
        }
    }

}
