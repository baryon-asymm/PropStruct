using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Simulation;

namespace PropStruct.Cli;

/// <summary>
/// Turns the flag values <see cref="CommandLine"/> collected (keyed by the <see cref="ModelParameters"/>
/// or <see cref="SimulationOptions"/> property name they set) into the two records themselves, starting
/// from each record's own default (BOOT.md, "Defaults are the original's"). Explicit per-property
/// assignment, not reflection-based <c>SetValue</c>: fourteen and ten properties respectively are few
/// enough that a typo here is a compiler error instead of a silent no-op.
/// </summary>
internal static class RunOptionsBuilder
{
    public static ModelParameters BuildModelParameters(IReadOnlyDictionary<string, object> values)
    {
        var parameters = ModelParameters.Default;

        if (values.TryGetValue(nameof(ModelParameters.Dmin), out var dmin))
        {
            parameters = parameters with { Dmin = (Length)dmin };
        }

        if (values.TryGetValue(nameof(ModelParameters.CellSize), out var cellSize))
        {
            parameters = parameters with { CellSize = (Length)cellSize };
        }

        if (values.TryGetValue(nameof(ModelParameters.CategoryStep), out var categoryStep))
        {
            parameters = parameters with { CategoryStep = (Length)categoryStep };
        }

        if (values.TryGetValue(nameof(ModelParameters.EpsDok), out var epsDok))
        {
            parameters = parameters with { EpsDok = (double)epsDok };
        }

        if (values.TryGetValue(nameof(ModelParameters.Alpha), out var alpha))
        {
            parameters = parameters with { Alpha = (double)alpha };
        }

        if (values.TryGetValue(nameof(ModelParameters.NnMin), out var nnMin))
        {
            parameters = parameters with { NnMin = (double)nnMin };
        }

        if (values.TryGetValue(nameof(ModelParameters.PocketCoefficient), out var pocketCoefficient))
        {
            parameters = parameters with { PocketCoefficient = (double)pocketCoefficient };
        }

        if (values.TryGetValue(nameof(ModelParameters.BridgeCoefficient), out var bridgeCoefficient))
        {
            parameters = parameters with { BridgeCoefficient = (double)bridgeCoefficient };
        }

        if (values.TryGetValue(nameof(ModelParameters.TailProbability), out var tailProbability))
        {
            parameters = parameters with { TailProbability = (double)tailProbability };
        }

        if (values.TryGetValue(nameof(ModelParameters.NnMax), out var nnMax))
        {
            parameters = parameters with { NnMax = (double)nnMax };
        }

        if (values.TryGetValue(nameof(ModelParameters.HomogenizedOxidizerFraction), out var homogenizedOxidizerFraction))
        {
            parameters = parameters with { HomogenizedOxidizerFraction = (double)homogenizedOxidizerFraction };
        }

        if (values.TryGetValue(nameof(ModelParameters.ReadPocketFormingFractions), out var readPocketFormingFractions))
        {
            parameters = parameters with { ReadPocketFormingFractions = (bool)readPocketFormingFractions };
        }

        if (values.TryGetValue(nameof(ModelParameters.Variant), out var variant))
        {
            parameters = parameters with { Variant = (int)variant };
        }

        if (values.TryGetValue(nameof(ModelParameters.AggregatedOxideFraction), out var aggregatedOxideFraction))
        {
            parameters = parameters with { AggregatedOxideFraction = (double)aggregatedOxideFraction };
        }

        return parameters;
    }

    public static SimulationOptions BuildSimulationOptions(IReadOnlyDictionary<string, object> values, ModelParameters parameters)
    {
        var options = new SimulationOptions { Parameters = parameters };

        if (values.TryGetValue(nameof(SimulationOptions.Mode), out var mode))
        {
            options = options with { Mode = (ExecutionMode)mode };
        }

        if (values.TryGetValue(nameof(SimulationOptions.Accelerator), out var accelerator))
        {
            options = options with { Accelerator = (AcceleratorKind)accelerator };
        }

        if (values.TryGetValue(nameof(SimulationOptions.Streams), out var streams))
        {
            options = options with { Streams = (StreamLayout)streams };
        }

        if (values.TryGetValue(nameof(SimulationOptions.Precision), out var precision))
        {
            options = options with { Precision = (PrecisionKind)precision };
        }

        if (values.TryGetValue(nameof(SimulationOptions.BatchSize), out var batchSize))
        {
            options = options with { BatchSize = (int)batchSize };
        }

        if (values.TryGetValue(nameof(SimulationOptions.Seed), out var seed))
        {
            options = options with { Seed = (ulong)seed };
        }

        if (values.TryGetValue(nameof(SimulationOptions.RecordBudgetBytes), out var recordBudgetBytes))
        {
            options = options with { RecordBudgetBytes = (long)recordBudgetBytes };
        }

        if (values.TryGetValue(nameof(SimulationOptions.AttemptsPerLaunch), out var attemptsPerLaunch))
        {
            options = options with { AttemptsPerLaunch = (int)attemptsPerLaunch };
        }

        if (values.TryGetValue(nameof(SimulationOptions.MaxAttemptsPerParticle), out var maxAttemptsPerParticle))
        {
            options = options with { MaxAttemptsPerParticle = (long)maxAttemptsPerParticle };
        }

        if (values.TryGetValue(nameof(SimulationOptions.NeighbourBudget), out var neighbourBudget))
        {
            options = options with { NeighbourBudget = (int)neighbourBudget };
        }

        if (values.TryGetValue(nameof(SimulationOptions.ContinuedStreams), out var continuedStreams))
        {
            options = options with { ContinuedStreams = (bool)continuedStreams };
        }

        return options;
    }
}
