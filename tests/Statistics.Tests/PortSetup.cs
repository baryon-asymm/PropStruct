using System.Collections.Frozen;
using System.Collections.Immutable;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// What <c>Setup.Prepare</c> under <see cref="PrecisionKind.Original"/> returns for one shipped formulation of
/// <c>tests/Fixtures/Legacy/formulations</c>, with the menu values and the pocket-forming flags of an oracle case or
/// none. Shared by <c>ListingOracleTests</c> and <c>PreloopSurveyTests</c>, which hold it to the executable's bits.
/// </summary>
internal sealed record PortSetup(
    SetupStatus Status, ModelSetup Setup, SetupTables Tables, SetupInputs Inputs, PendingEchoes Pending)
{
    private const int NeighbourBudget = 1000;
    private const int PocketRedrawBudget = 1000;

    private static readonly FrozenDictionary<string, Func<ModelParameters, double, ModelParameters>> MenuBinding =
        new Dictionary<string, Func<ModelParameters, double, ModelParameters>>(StringComparer.Ordinal)
        {
            ["dmin"] = (p, v) => p with { Dmin = new Length(v) },
            ["di"] = (p, v) => p with { CellSize = new Length(v) },
            ["dj"] = (p, v) => p with { CategoryStep = new Length(v) },
            ["eps_dok"] = (p, v) => p with { EpsDok = v },
            ["alpha"] = (p, v) => p with { Alpha = v },
            ["nn_min"] = (p, v) => p with { NnMin = v },
            ["nn_max"] = (p, v) => p with { NnMax = v },
            ["karmcoef"] = (p, v) => p with { PocketCoefficient = v },
            ["mkmcoef"] = (p, v) => p with { BridgeCoefficient = v },
            ["alfa"] = (p, v) => p with { TailProbability = v },
            ["gdokns"] = (p, v) => p with { HomogenizedOxidizerFraction = v },
            ["ivar"] = (p, v) => p with { Variant = (int)v },
            ["eta"] = (p, v) => p with { AggregatedOxideFraction = v },
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Runs <c>Setup.Prepare</c> on the formulation file <paramref name="formulationFile"/>, menu values and flags as given.</summary>
    public static PortSetup Prepare(string formulationFile, int[]? flags, IReadOnlyDictionary<string, double> menu)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations", formulationFile);
        var formulation = DatFile.Read(path);
        var parameters = ModelParameters.Default;
        if (flags is not null)
        {
            formulation = formulation with { PocketFormingFractions = ImmutableArray.Create(flags) };
            parameters = parameters with { ReadPocketFormingFractions = true };
        }

        foreach (var (name, value) in menu)
        {
            parameters = MenuBinding[name](parameters, value);
        }

        var status = Statistics.Setup.Prepare(formulation, parameters, PrecisionKind.Original, NeighbourBudget, PocketRedrawBudget,
            out var setup, out var tables, out _, out var pending, out var inputs);
        return new PortSetup(status, setup, tables, inputs, pending);
    }

    /// <summary>
    /// What the executable's own pre-loop computed (PARAM's <c>ZSS</c>, <c>ZX</c> and <c>Z11</c>, line 378's λ, the
    /// <c>DOKM</c> and <c>DOKSD</c> sums, <c>GGG</c>), against the port's value of the same quantity, by the map's key.
    /// </summary>
    public IEnumerable<(string Key, double[] Port)> PreloopValues()
    {
        yield return ("zss", [Tables.Zss]);
        yield return ("zx", Tables.Share);
        yield return ("z11", Tables.Cumulative);
        yield return ("lambda", [Setup.Lambda]);
        yield return ("dokm", [Pending.Dokm]);
        yield return ("doksd", [Pending.Doksd]);
        yield return ("ggg", [Pending.OxidizerMassFractionEffective]);
    }

    /// <summary>
    /// Every element of <see cref="PreloopValues"/> whose bits differ from the executable's in <paramref name="executed"/>,
    /// as <c>key executed 0x.. port 0x..</c> (an array element as <c>key[i]</c>); a length that differs is one line.
    /// </summary>
    public IEnumerable<string> PreloopDifferences(IReadOnlyDictionary<string, ExecutedBits[]> executed)
    {
        foreach (var (key, port) in PreloopValues())
        {
            var bits = executed[key];
            if (bits.Length != port.Length)
            {
                yield return $"{key}: executed {bits.Length} values, port {port.Length}";
                continue;
            }

            for (var k = 0; k < port.Length; k++)
            {
                if (BitConverter.DoubleToInt64Bits(bits[k].Value) != BitConverter.DoubleToInt64Bits(port[k]))
                {
                    yield return $"{(port.Length == 1 && bits[k].Binary32 ? key : $"{key}[{k}]")} executed {bits[k].Hex} port {bits[k].HexOf(port[k])}";
                }
            }
        }
    }
}
