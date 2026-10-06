using System.Reflection;
using PropStruct.Input;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The sweep the reported <c>NnMax</c> defect asked for (`SimulatorOptionsTests`,
/// `src/Statistics/BOOT.md`, "## Constraints", 2026-09-20 note): zero, a negative value
/// and a very large value against every one of the fourteen menu parameters of
/// <see cref="ModelParameters"/>, the parameter list itself generated from the record's
/// own properties by reflection rather than typed, so a fifteenth parameter is swept
/// automatically instead of silently missed (AGENTS.md §6, the "all" quantifier). Every
/// combination is run through the same bounded configuration and must end the same way
/// every other invalid input in this tree does (root BOOT.md, "Failures are values"): a
/// value <see cref="Simulator.Create"/> or <see cref="Simulator.Run"/> already documents
/// (<c>ArgumentException</c>, <see cref="SimulationFailedException"/>) or a completed
/// run — never an undocumented exception, and never a run slow enough to look hung
/// (bounded here by tiny attempt/neighbour/redraw budgets, not by a wall clock).
/// </summary>
public class ParameterSweepTests
{
    private readonly ITestOutputHelper _output;

    public ParameterSweepTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly PropertyInfo[] Parameters = typeof(ModelParameters)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanWrite)
        .ToArray();

    public static IEnumerable<object[]> Probes()
    {
        foreach (var property in Parameters)
        {
            foreach (var probe in ProbesFor(property))
            {
                yield return new object[] { property.Name, probe.Label, probe.Value };
            }
        }
    }

    /// <summary>
    /// Proves the sweep itself is not vacuous (AGENTS.md §6, the "all" quantifier is
    /// checked against a machine-generated list): the fourteen parameters of
    /// `Input/API.md`'s own <c>ModelParameters</c> code block, by name, exactly match
    /// what reflection finds here, so a parameter added or removed there moves this count
    /// instead of leaving a stale total unnoticed.
    /// </summary>
    [Fact]
    public void SweptPropertiesAreExactlyTheFourteenMenuParameters()
    {
        var expected = new[]
        {
            "Dmin", "CellSize", "CategoryStep", "EpsDok", "Alpha", "NnMin", "PocketCoefficient",
            "BridgeCoefficient", "TailProbability", "NnMax", "HomogenizedOxidizerFraction",
            "ReadPocketFormingFractions", "Variant", "AggregatedOxideFraction",
        };

        Assert.Equal(expected.OrderBy(n => n), Parameters.Select(p => p.Name).OrderBy(n => n));
    }

    [Theory]
    [MemberData(nameof(Probes))]
    public void DegenerateParameterNeverLeavesAnUndocumentedFailure(string propertyName, string probeLabel, object value)
    {
        var property = Parameters.Single(p => p.Name == propertyName);

        var parameters = ModelParameters.Default with { };
        property.SetValue(parameters, value);

        var options = new SimulationOptions
        {
            Parameters = parameters,
            // Small enough that a parameter which makes every attempt fail (this sweep's
            // whole point) is discovered in milliseconds, not the four minutes the reported
            // NnMax defect took at the real defaults.
            MaxAttemptsPerParticle = 300,
            NeighbourBudget = 300,
            PocketRedrawBudget = 300,
        };
        var formulation = SmallFormulation.Build(particlesPerCycle: 3, cycles: 1);

        string outcome;
        string? undocumented = null;
        try
        {
            using var simulator = Simulator.Create(options);
            var result = simulator.Run(formulation);
            outcome = $"Ok, {result.Diagnostics.TotalAttempts} attempts";
        }
        catch (ArgumentException ex)
        {
            outcome = $"ArgumentException: {ex.Message}";

            // "Documented" is the whole claim of this test's name: a caller who passed a degenerate value must be
            // able to tell from the failure which parameter caused it. A message that does not name the property
            // leaves the caller to guess, which is the failure this sweep exists to catch.
            //
            // Measured 2026-09-20: no probe in this sweep reaches this branch — every outcome is Ok or a
            // SimulationFailedException — so this condition is a guard for a validation style the library does
            // not currently use, and mutating it does not turn the test red. It is kept because an ArgumentException
            // is the natural shape for a parameter rejected before the run, and left unstated it would read as a
            // live check. The branch below is the live one: mutated to reject InvalidSetup it fails on eleven
            // probes, naming each.
            if (!ex.Message.Contains(propertyName, StringComparison.Ordinal)
                && !(ex is ArgumentException { ParamName: { } name } && name.Contains(propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                undocumented = $"the message names neither {propertyName} nor it as a parameter: {ex.Message}";
            }
        }
        catch (SimulationFailedException ex)
        {
            outcome = $"SimulationFailedException: {ex.Status}";

            // A status outside the enum, or Ok on a thrown failure, is a failure the tree has no word for.
            if (!Enum.IsDefined(ex.Status) || ex.Status == RunStatus.Ok)
            {
                undocumented = $"the status is not a named failure: {(int)ex.Status}";
            }
        }

        _output.WriteLine($"{propertyName} = {probeLabel} ({Display(value)}): {outcome}");

        // Until 2026-09-20 this test asserted nothing at all and its name asserted everything: a review of the
        // branch found it among the tests that cannot fail. What it did carry, and still carries, is that any
        // exception other than the two caught above escapes and fails the test — an unnamed failure mode is
        // caught by the absence of a catch. What is added is the other half, that a caught failure identifies
        // itself.
        Assert.True(undocumented is null, $"{propertyName} = {probeLabel}: {undocumented}");
    }

    private static string Display(object value) => value switch
    {
        Length length => $"{length.AsWritten} ({(length.IsMicrometres ? "um" : "m")})",
        _ => value.ToString() ?? "null",
    };

    private readonly record struct Probe(string Label, object Value);

    /// <summary>
    /// Zero, a negative value and a very large value, shaped for the property's own CLR
    /// type rather than its name — so the probes themselves stay meaningful if a
    /// parameter's type ever changes, and a genuinely new type throws here instead of
    /// silently sweeping nothing for it (AGENTS.md §6 again: an unhandled case must be
    /// loud, not a quiet gap).
    /// </summary>
    private static IEnumerable<Probe> ProbesFor(PropertyInfo property)
    {
        if (property.PropertyType == typeof(Length))
        {
            yield return new Probe("zero", Length.FromMetres(0.0));
            yield return new Probe("negative", Length.FromMetres(-1e-6));
            yield return new Probe("huge", Length.FromMicrometres(1e12));
        }
        else if (property.PropertyType == typeof(double))
        {
            yield return new Probe("zero", 0.0);
            yield return new Probe("negative", -1.0);
            yield return new Probe("huge", 1e12);
        }
        else if (property.PropertyType == typeof(int))
        {
            yield return new Probe("zero", 0);
            yield return new Probe("negative", -1);
            yield return new Probe("huge", 1_000_000);
        }
        else if (property.PropertyType == typeof(bool))
        {
            yield return new Probe("false", false);
            yield return new Probe("true", true);
        }
        else
        {
            throw new NotSupportedException(
                $"ParameterSweepTests has no probe values for {property.Name} : {property.PropertyType}; add a case rather than skip it.");
        }
    }
}
