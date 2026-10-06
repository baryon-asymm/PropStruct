using System.Reflection;
using PropStruct.Input;
using PropStruct.Simulation;

namespace PropStruct.Cli;

/// <summary>
/// One flag's whole grammar: its canonical spelling, its aliases, how its value parses, and — for a
/// <see cref="FlagOwner.ModelParameters"/> or <see cref="FlagOwner.SimulationOptions"/> flag — the
/// property it reads and writes, found by reflection so that <c>defaults</c> and the reflected flag/
/// property check (tests/Cli.Tests/BOOT.md, L0) never carry a hand-typed second copy of a value.
/// </summary>
internal sealed record FlagDefinition(
    FlagOwner Owner,
    string PropertyName,
    string CanonicalFlag,
    IReadOnlyList<string> Aliases,
    FlagKind Kind,
    int MenuNumber = 0,
    IReadOnlyDictionary<string, object>? EnumValues = null)
{
    /// <summary>The <see cref="ModelParameters"/> or <see cref="SimulationOptions"/> property this flag
    /// maps to; <see langword="null"/> for a <see cref="FlagOwner.Cli"/> flag, which has none.</summary>
    public PropertyInfo? Property { get; } = Owner switch
    {
        FlagOwner.ModelParameters => typeof(ModelParameters).GetProperty(PropertyName)
            ?? throw new InvalidOperationException($"ModelParameters has no property '{PropertyName}'."),
        FlagOwner.SimulationOptions => typeof(SimulationOptions).GetProperty(PropertyName)
            ?? throw new InvalidOperationException($"SimulationOptions has no property '{PropertyName}'."),
        FlagOwner.Cli => null,
        _ => null,
    };

    /// <summary>Every spelling this flag accepts: the canonical name first, then its aliases.</summary>
    public IEnumerable<string> Spellings => new[] { CanonicalFlag }.Concat(Aliases);
}
