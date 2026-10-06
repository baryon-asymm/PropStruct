using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table: "every member of the diagnostics enumeration is produced by some input" -
/// the test enumerates <see cref="Diagnostic"/> itself, so a new member with no producing input turns
/// this red instead of being silently uncovered (AGENTS.md §6, the "all" quantifier).
/// </summary>
public class DiagnosticCoverageTests
{
    // One command line per Diagnostic member that reliably produces exactly that diagnostic.
    private static readonly Dictionary<Diagnostic, string[]> ProducingArguments = new()
    {
        [Diagnostic.NoVerbGiven] = Array.Empty<string>(),
        [Diagnostic.UnknownVerb] = new[] { "fly" },
        [Diagnostic.UnknownOption] = new[] { "run", "input.dat", "--dmim" },
        [Diagnostic.DuplicateOption] = new[] { "run", "input.dat", "--dmin", "1", "--dmin", "2" },
        [Diagnostic.MissingOptionValue] = new[] { "run", "input.dat", "--dmin" },
        [Diagnostic.SwitchDoesNotTakeValue] = new[] { "run", "input.dat", "--sfr=true" },
        [Diagnostic.InvalidNumber] = new[] { "run", "input.dat", "--eps", "not-a-number" },
        [Diagnostic.FortranExponentRejected] = new[] { "run", "input.dat", "--dmin", "10d-6" },
        [Diagnostic.InvalidEnumValue] = new[] { "run", "input.dat", "--mode", "fast" },
        [Diagnostic.RunMissingInputFile] = new[] { "run" },
        [Diagnostic.RunTooManyPositionalArguments] = new[] { "run", "a.dat", "b.dat" },
        [Diagnostic.ContinuedStreamsRequiresBatchedBatchOne] = new[] { "run", "input.dat", "--continued-streams" },
    };

    [Fact]
    public void EveryDiagnosticEnumMemberHasAProducingCommandLine()
    {
        var members = Enum.GetValues<Diagnostic>();
        var missing = members.Where(m => !ProducingArguments.ContainsKey(m)).ToList();
        Assert.True(missing.Count == 0, $"No producing input recorded for: {string.Join(", ", missing)}");
        Assert.Equal(members.Length, ProducingArguments.Count);
    }

    // The Diagnostic enum is internal; a [Theory] method must be public, and a public member cannot carry
    // an internal type in its signature even under InternalsVisibleTo. The diagnostic travels as its name
    // instead and is parsed back inside the (internal-visible) method body.
    //
    // "Exactly its own" (reviewed 2026-09-20) is Assert.Single over the whole error list, not
    // Assert.Contains: every recorded command line is chosen to raise one diagnostic and no other, and a
    // line that quietly grew a second error (for example an unknown flag whose value token turns into an
    // unclaimed positional argument) would pass a Contains check while actually being a worse fixture.
    [Theory]
    [MemberData(nameof(Cases))]
    public void EachRecordedCommandLineProducesExactlyItsOwnDiagnostic(string diagnosticName, string[] args)
    {
        var expected = Enum.Parse<Diagnostic>(diagnosticName);
        var result = CommandLine.Parse(args);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal(expected, error.Diagnostic);
    }

    public static IEnumerable<object[]> Cases() => ProducingArguments.Select(kv => new object[] { kv.Key.ToString(), kv.Value });
}
