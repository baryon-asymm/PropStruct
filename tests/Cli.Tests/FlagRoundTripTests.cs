using System.Globalization;
using PropStruct.Input;
using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table, strengthened per review (2026-09-20): the name-list comparison in
/// <see cref="ReflectedFlagCoverageTests"/> proves every property has *a* flag, but not that a given
/// flag's value actually reaches *that* property and no other - swapping two lines in
/// <see cref="RunOptionsBuilder"/>'s own hand-written value-to-property chain left every test green
/// before this file existed. This drives, for every flag in <see cref="FlagCatalog"/> and every spelling
/// it has (all 35: <see cref="FlagCatalog.ModelParameterFlags"/>, <see cref="FlagCatalog.SimulationOptionFlags"/>
/// and <see cref="FlagCatalog.CliOnlyFlags"/>), a real parse of one non-default sample value and asserts
/// the target property (or command field, for the three Cli-only flags) holds it while every other
/// flagged property still equals its record's own default.
/// </summary>
public class FlagRoundTripTests
{
    // One example value, in command-line text, for every FlagKind a single fixed sample can serve -
    // chosen distinct from every current ModelParameters/SimulationOptions default, so the round trip can
    // tell "the target property changed" from "it already held this value". FlagKind.Enum and
    // FlagKind.Switch need a sample chosen per flag instead (a fixed enum spelling is not valid for every
    // enum flag; a switch takes no value at all) and are handled separately in the test body; every other
    // kind must appear here - EveryFlagKindIsRoundTripTestedOrSpeciallyHandled below turns red the day a
    // new FlagKind is added without a sample or a special case.
    private static readonly Dictionary<FlagKind, string> SampleText = new()
    {
        [FlagKind.Length] = "12.5",
        [FlagKind.Number] = "12.5",
        [FlagKind.Integer] = "7",
        [FlagKind.PositiveInteger] = "7",
        [FlagKind.PositiveLong] = "7",
        [FlagKind.UnsignedLong] = "7",
    };

    private static readonly HashSet<FlagKind> SpeciallyHandledKinds = new() { FlagKind.Enum, FlagKind.Switch, FlagKind.Path };

    // SimulationOptions.ContinuedStreams' own documented precondition, added to the round trip's args
    // whenever that flag is exercised (below).
    private static readonly string[] ContinuedStreamsPrerequisiteArgs = ["--mode", "batched", "--batch", "1"];

    [Fact]
    public void EveryFlagKindIsRoundTripTestedOrSpeciallyHandled()
    {
        foreach (var kind in Enum.GetValues<FlagKind>())
        {
            Assert.True(
                SampleText.ContainsKey(kind) || SpeciallyHandledKinds.Contains(kind),
                $"FlagKind.{kind} has no round-trip sample text and is not one of the specially handled kinds.");
        }
    }

    public static IEnumerable<object[]> ModelAndOptionSpellings() =>
        FlagCatalog.ModelParameterFlags.Concat(FlagCatalog.SimulationOptionFlags)
            .SelectMany(flag => flag.Spellings)
            .Select(spelling => new object[] { spelling });

    [Theory]
    [MemberData(nameof(ModelAndOptionSpellings))]
    public void EachModelOrOptionSpellingSetsExactlyItsOwnPropertyAndNoOther(string spelling)
    {
        Assert.True(FlagCatalog.TryResolve(spelling, out var flag), $"'{spelling}' does not resolve to a flag.");

        var defaultParameters = ModelParameters.Default;
        var defaultOptions = new SimulationOptions();

        var args = new List<string> { "run", "input.dat" };
        object expectedValue;

        switch (flag.Kind)
        {
            case FlagKind.Switch:
                if (flag.PropertyName == nameof(SimulationOptions.ContinuedStreams))
                {
                    // SimulationOptions' own documented precondition ("batched, batch size 1 only",
                    // src/Simulation/API.md): the round trip must supply it to get a successful parse at
                    // all, so Mode/BatchSize are excluded from the "everything else is default" check
                    // below for this one spelling - they are this flag's own required context, not a
                    // side effect of a buggy value-to-property chain.
                    args.AddRange(ContinuedStreamsPrerequisiteArgs);
                }

                args.Add(spelling);
                expectedValue = true;
                break;

            case FlagKind.Enum:
                var defaultInstance = flag.Owner == FlagOwner.ModelParameters ? (object)defaultParameters : defaultOptions;
                var defaultValue = flag.Property!.GetValue(defaultInstance);
                var sample = flag.EnumValues!.First(kv => !Equals(kv.Value, defaultValue));
                args.Add(spelling);
                args.Add(sample.Key);
                expectedValue = sample.Value;
                break;

            case FlagKind.Length:
            case FlagKind.Number:
            case FlagKind.Integer:
            case FlagKind.PositiveInteger:
            case FlagKind.PositiveLong:
            case FlagKind.UnsignedLong:
            case FlagKind.Path:
            default:
                Assert.True(SampleText.TryGetValue(flag.Kind, out var text), $"No round-trip sample for FlagKind.{flag.Kind}.");
                args.Add(spelling);
                args.Add(text!);
                expectedValue = ParseExpected(flag.Kind, text!);
                break;
        }

        var result = CommandLine.Parse(args.ToArray());
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var command = Assert.IsType<RunCommand>(result.Command);

        var actualInstance = flag.Owner == FlagOwner.ModelParameters ? (object)command.Parameters : command.Options;
        Assert.Equal(expectedValue, flag.Property!.GetValue(actualInstance));

        foreach (var other in FlagCatalog.ModelParameterFlags)
        {
            if (other.PropertyName == flag.PropertyName && other.Owner == flag.Owner)
            {
                continue;
            }

            Assert.Equal(other.Property!.GetValue(defaultParameters), other.Property!.GetValue(command.Parameters));
        }

        foreach (var other in FlagCatalog.SimulationOptionFlags)
        {
            if (other.PropertyName == flag.PropertyName && other.Owner == flag.Owner)
            {
                continue;
            }

            if (flag.PropertyName == nameof(SimulationOptions.ContinuedStreams) &&
                other.PropertyName is nameof(SimulationOptions.Mode) or nameof(SimulationOptions.BatchSize))
            {
                continue; // supplied as this flag's own required precondition, not left at its default.
            }

            Assert.Equal(other.Property!.GetValue(defaultOptions), other.Property!.GetValue(command.Options));
        }
    }

    [Theory]
    [InlineData("--output")]
    [InlineData("--json")]
    public void PathFlagsSetTheirOwnCommandFieldLeavingModelAndOptionsAtDefault(string spelling)
    {
        var result = CommandLine.Parse(new[] { "run", "input.dat", spelling, "custom-name.ext" });
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var command = Assert.IsType<RunCommand>(result.Command);

        var expectedFullPath = Path.GetFullPath("custom-name.ext");
        if (spelling == "--output")
        {
            Assert.Equal(expectedFullPath, command.OutputPath);
            Assert.Null(command.JsonPath);
        }
        else
        {
            Assert.Equal(expectedFullPath, command.JsonPath);
            Assert.Equal(Path.GetFullPath("results.m"), command.OutputPath);
        }

        Assert.Equal(ModelParameters.Default, command.Parameters);
        Assert.Equal(new SimulationOptions(), command.Options);
    }

    private static readonly string[] QuietArgs = ["run", "input.dat", "--quiet"];

    [Fact]
    public void QuietFlagSetsQuietLeavingModelAndOptionsAtDefault()
    {
        var result = CommandLine.Parse(QuietArgs);
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var command = Assert.IsType<RunCommand>(result.Command);

        Assert.True(command.Quiet);
        Assert.Equal(ModelParameters.Default, command.Parameters);
        Assert.Equal(new SimulationOptions(), command.Options);
    }

    private static object ParseExpected(FlagKind kind, string text) => kind switch
    {
        FlagKind.Length => new Length(double.Parse(text, CultureInfo.InvariantCulture)),
        FlagKind.Number => double.Parse(text, CultureInfo.InvariantCulture),
        FlagKind.Integer or FlagKind.PositiveInteger => int.Parse(text, CultureInfo.InvariantCulture),
        FlagKind.PositiveLong => long.Parse(text, CultureInfo.InvariantCulture),
        FlagKind.UnsignedLong => ulong.Parse(text, CultureInfo.InvariantCulture),
        FlagKind.Switch => throw new InvalidOperationException($"{kind} is not a sample-text kind."),
        FlagKind.Enum => throw new InvalidOperationException($"{kind} is not a sample-text kind."),
        FlagKind.Path => throw new InvalidOperationException($"{kind} is not a sample-text kind."),
        _ => throw new InvalidOperationException($"{kind} is not a sample-text kind."),
    };
}
