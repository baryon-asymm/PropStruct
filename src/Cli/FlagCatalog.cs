using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;

namespace PropStruct.Cli;

/// <summary>
/// The whole flag grammar of <c>run</c>, in one place (BOOT.md, "Flag names are the original's own
/// names, with aliases"). <see cref="ModelParameterFlags"/> and <see cref="SimulationOptionFlags"/> are
/// read by <c>tests/Cli.Tests</c>' reflected two-way check against <see cref="ModelParameters"/>' and
/// <see cref="SimulationOptions"/>' own properties; <see cref="CliOnlyFlags"/> (<c>--output</c>,
/// <c>--json</c>, <c>--quiet</c>) has no such counterpart and is not part of that check.
/// <see cref="DeliberatelyUnflagged"/> names the one property that check excludes on purpose, with why.
/// </summary>
internal static class FlagCatalog
{
    public static IReadOnlyList<FlagDefinition> ModelParameterFlags { get; } = new[]
    {
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.Dmin), "--dmin", Array.Empty<string>(), FlagKind.Length, MenuNumber: 1),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.CellSize), "--di", new[] { "--cell-size" }, FlagKind.Length, MenuNumber: 2),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.CategoryStep), "--dj", new[] { "--category-step" }, FlagKind.Length, MenuNumber: 3),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.EpsDok), "--eps", new[] { "--eps-dok" }, FlagKind.Number, MenuNumber: 4),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.Alpha), "--k5", new[] { "--alpha" }, FlagKind.Number, MenuNumber: 5),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.NnMin), "--nn-min", new[] { "--k6" }, FlagKind.Number, MenuNumber: 6),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.PocketCoefficient), "--karmcoef", new[] { "--k7" }, FlagKind.Number, MenuNumber: 7),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.BridgeCoefficient), "--mkmcoef", new[] { "--k8" }, FlagKind.Number, MenuNumber: 8),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.TailProbability), "--alfa", new[] { "--tail-probability" }, FlagKind.Number, MenuNumber: 9),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.NnMax), "--nn-max", Array.Empty<string>(), FlagKind.Number, MenuNumber: 10),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.HomogenizedOxidizerFraction), "--gdokns", Array.Empty<string>(), FlagKind.Number, MenuNumber: 11),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.ReadPocketFormingFractions), "--sfr", Array.Empty<string>(), FlagKind.Switch, MenuNumber: 12),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.Variant), "--ivar", Array.Empty<string>(), FlagKind.Integer, MenuNumber: 13),
        new FlagDefinition(FlagOwner.ModelParameters, nameof(ModelParameters.AggregatedOxideFraction), "--eta", Array.Empty<string>(), FlagKind.Number, MenuNumber: 14),
    };

    public static IReadOnlyList<FlagDefinition> SimulationOptionFlags { get; } = new[]
    {
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.Mode), "--mode", Array.Empty<string>(), FlagKind.Enum,
            EnumValues: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["reference"] = ExecutionMode.Reference,
                ["batched"] = ExecutionMode.Batched,
            }),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.Accelerator), "--accelerator", Array.Empty<string>(), FlagKind.Enum,
            EnumValues: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["auto"] = AcceleratorKind.Auto,
                ["cpu"] = AcceleratorKind.Cpu,
                ["cuda"] = AcceleratorKind.Cuda,
            }),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.Streams), "--layout", Array.Empty<string>(), FlagKind.Enum,
            EnumValues: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = StreamLayout.Original,
                ["independent"] = StreamLayout.Independent,
            }),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.Precision), "--precision", Array.Empty<string>(), FlagKind.Enum,
            EnumValues: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                // "binary64" is the canonical spelling (CA1720, owner's decision 2026-09-24: the enum
                // member itself was renamed from Double to Binary64, since an identifier may not contain
                // a type name). "double" is kept so an existing command line still parses; it is listed
                // second so the canonical spelling comes first wherever these keys are shown.
                ["binary64"] = PrecisionKind.Binary64,
                ["double"] = PrecisionKind.Binary64,
                ["original"] = PrecisionKind.Original,
            }),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.BatchSize), "--batch", Array.Empty<string>(), FlagKind.PositiveInteger),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.Seed), "--seed", Array.Empty<string>(), FlagKind.UnsignedLong),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.RecordBudgetBytes), "--record-budget-bytes", Array.Empty<string>(), FlagKind.PositiveLong),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.AttemptsPerLaunch), "--attempts-per-launch", Array.Empty<string>(), FlagKind.PositiveInteger),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.MaxAttemptsPerParticle), "--max-attempts-per-particle", Array.Empty<string>(), FlagKind.PositiveLong),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.NeighbourBudget), "--neighbour-budget", Array.Empty<string>(), FlagKind.PositiveInteger),
        new FlagDefinition(FlagOwner.SimulationOptions, nameof(SimulationOptions.ContinuedStreams), "--continued-streams", Array.Empty<string>(), FlagKind.Switch),
    };

    public static IReadOnlyList<FlagDefinition> CliOnlyFlags { get; } = new[]
    {
        new FlagDefinition(FlagOwner.Cli, "OutputPath", "--output", Array.Empty<string>(), FlagKind.Path),
        new FlagDefinition(FlagOwner.Cli, "JsonPath", "--json", Array.Empty<string>(), FlagKind.Path),
        new FlagDefinition(FlagOwner.Cli, "Quiet", "--quiet", Array.Empty<string>(), FlagKind.Switch),
    };

    /// <summary>One property of <see cref="ModelParameters"/> or <see cref="SimulationOptions"/> that
    /// deliberately has no flag of its own, with why.</summary>
    internal sealed record UnflaggedProperty(FlagOwner Owner, string PropertyName, string Reason);

    /// <summary>
    /// <see cref="SimulationOptions.Parameters"/> deliberately has no flag: each of its own fields is
    /// reachable individually through <see cref="ModelParameterFlags"/> instead (BOOT.md's own
    /// "Dependencies" - a length keeps the form it was written in, so the mapping is field by field, not
    /// a single "parameters" flag). tests/Cli.Tests/BOOT.md's L0 row asserts this list stays exactly this
    /// one entry, named with its reason.
    /// </summary>
    public static IReadOnlyList<UnflaggedProperty> DeliberatelyUnflagged { get; } = new[]
    {
        new UnflaggedProperty(
            FlagOwner.SimulationOptions,
            nameof(SimulationOptions.Parameters),
            $"mapped field by field through {nameof(ModelParameterFlags)}, not a flag of its own"),
    };

    public static IEnumerable<FlagDefinition> AllValueAndSwitchFlags =>
        ModelParameterFlags.Concat(SimulationOptionFlags).Concat(CliOnlyFlags);

    /// <summary>Finds the flag a token's flag name (without any <c>=value</c> suffix) spells, by
    /// canonical name or alias.</summary>
    public static bool TryResolve(string name, out FlagDefinition flag)
    {
        foreach (var candidate in AllValueAndSwitchFlags)
        {
            foreach (var spelling in candidate.Spellings)
            {
                if (string.Equals(spelling, name, StringComparison.Ordinal))
                {
                    flag = candidate;
                    return true;
                }
            }
        }

        flag = null!;
        return false;
    }
}
