using PropStruct.Input;
using PropStruct.Simulation;
using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0 of the BOOT.md table: the parser itself - every flag, both spellings, switches, <c>--</c>,
/// repetition, unknown flag, missing value, unparsable number - checked against the parse result and the
/// exact diagnostic text (tests/Cli.Tests/BOOT.md, Invariants: "a diagnostic is asserted by its text, not
/// by the exit code alone").
/// </summary>
public class ParserTests
{
    private static readonly string[] RunInputDatArgs = ["run", "input.dat"];

    private static RunCommand ParseRunOk(params string[] runArgs)
    {
        var args = RunInputDatArgs.Concat(runArgs).ToArray();
        var result = CommandLine.Parse(args);
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        return Assert.IsType<RunCommand>(result.Command);
    }

    [Fact]
    public void FlagValueFormAndFlagEqualsValueFormAreEquivalent()
    {
        var byValue = ParseRunOk("--dmin", "12.5");
        var byEquals = ParseRunOk("--dmin=12.5");

        Assert.Equal(12.5, byValue.Parameters.Dmin.AsWritten);
        Assert.Equal(12.5, byEquals.Parameters.Dmin.AsWritten);
    }

    [Fact]
    public void AliasSetsTheSamePropertyAsItsCanonicalName()
    {
        var byCanonical = ParseRunOk("--di", "7");
        var byAlias = ParseRunOk("--cell-size", "7");

        Assert.Equal(byCanonical.Parameters.CellSize, byAlias.Parameters.CellSize);
    }

    [Fact]
    public void KnownK5AlfaTrapK5IsAlphaOnlyAlfaIsTailProbability()
    {
        var command = ParseRunOk("--k5", "1.5", "--alfa", "0.02");

        Assert.Equal(1.5, command.Parameters.Alpha);
        Assert.Equal(0.02, command.Parameters.TailProbability);
    }

    [Fact]
    public void SwitchPresentSetsTrue()
    {
        var command = ParseRunOk("--sfr");
        Assert.True(command.Parameters.ReadPocketFormingFractions);
    }

    [Fact]
    public void SwitchAbsentKeepsDefault()
    {
        var command = ParseRunOk();
        Assert.Equal(ModelParameters.Default.ReadPocketFormingFractions, command.Parameters.ReadPocketFormingFractions);
    }

    private static readonly string[] SingleDashTokenArgs = ["run", "input.dat", "-x"];

    [Fact]
    public void SingleDashTokenInFlagPositionIsAnUnknownOption()
    {
        // Reviewed 2026-09-20: before `--` ends the options, a token starting with a single dash used to
        // fall through to the positional branch and be silently accepted as (or appended to) the input
        // path; CommandLine.ParseRun now rejects it as an unknown option instead, the same as a `--`-typo.
        var result = CommandLine.Parse(SingleDashTokenArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal(Diagnostic.UnknownOption, error.Diagnostic);
        Assert.Contains("-x", error.Message, StringComparison.Ordinal);
    }

    private static readonly string[] DoubleDashArgs = ["run", "--mode", "batched", "--", "--strange.dat"];

    [Fact]
    public void DoubleDashEndsOptionsSoAPathMayBeginWithDash()
    {
        var result = CommandLine.Parse(DoubleDashArgs);
        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var command = Assert.IsType<RunCommand>(result.Command);
        Assert.Equal("--strange.dat", command.InputPath);
    }

    [Fact]
    public void ValueBeginningWithDashIsTakenVerbatimAsTheFlagsValue()
    {
        var command = ParseRunOk("--eta", "-1");
        Assert.Equal(-1.0, command.Parameters.AggregatedOxideFraction);
    }

    private static readonly string[] DuplicateDminArgs = ["run", "input.dat", "--dmin", "1", "--dmin", "2"];

    [Fact]
    public void DuplicateFlagIsAnErrorNamingTheCanonicalFlag()
    {
        var result = CommandLine.Parse(DuplicateDminArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal(Diagnostic.DuplicateOption, error.Diagnostic);
        Assert.Contains("--dmin", error.Message, StringComparison.Ordinal);
    }

    private static readonly string[] DuplicateAliasArgs = ["run", "input.dat", "--di", "1", "--cell-size", "2"];

    [Fact]
    public void DuplicateFlagByAliasAndCanonicalIsStillADuplicate()
    {
        var result = CommandLine.Parse(DuplicateAliasArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.DuplicateOption);
    }

    private static readonly string[] UnknownFlagArgs = ["run", "input.dat", "--dmim"];

    [Fact]
    public void UnknownFlagIsAnErrorNamingIt()
    {
        // Reviewed 2026-09-20: a trailing value token used to follow "--dmim" here. An unrecognized flag
        // never consumes the next token as its value (CommandLine.ParseRun adds the error and continues
        // immediately), so that token fell through as an unclaimed positional argument and raised a
        // second, unrelated RunTooManyPositionalArguments error that Assert.Single would have caught.
        var result = CommandLine.Parse(UnknownFlagArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal(Diagnostic.UnknownOption, error.Diagnostic);
        Assert.Contains("--dmim", error.Message, StringComparison.Ordinal);
    }

    private static readonly string[] MissingValueArgs = ["run", "input.dat", "--dmin"];

    [Fact]
    public void MissingValueAtEndOfArgumentsIsAnError()
    {
        var result = CommandLine.Parse(MissingValueArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.MissingOptionValue);
    }

    private static readonly string[] UnparsableNumberArgs = ["run", "input.dat", "--eps", "not-a-number"];

    [Fact]
    public void UnparsableNumberIsAnError()
    {
        var result = CommandLine.Parse(UnparsableNumberArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors, e => e.Diagnostic == Diagnostic.InvalidNumber);
        Assert.Contains("--eps", error.Message, StringComparison.Ordinal);
    }

    private static readonly string[] FortranExponentArgs = ["run", "input.dat", "--dmin", "10d-6"];

    [Fact]
    public void FortranExponentFormIsRefused()
    {
        var result = CommandLine.Parse(FortranExponentArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors, e => e.Diagnostic == Diagnostic.FortranExponentRejected);
        Assert.Contains("--dmin", error.Message, StringComparison.Ordinal);
        Assert.Contains("10d-6", error.Message, StringComparison.Ordinal);
    }

    private static readonly string[] SwitchEqualsValueArgs = ["run", "input.dat", "--sfr=true"];

    [Fact]
    public void SwitchGivenAsFlagEqualsValueIsAnError()
    {
        var result = CommandLine.Parse(SwitchEqualsValueArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.SwitchDoesNotTakeValue);
    }

    private static readonly string[] InvalidEnumArgs = ["run", "input.dat", "--mode", "fast"];

    [Fact]
    public void InvalidEnumValueIsAnError()
    {
        var result = CommandLine.Parse(InvalidEnumArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors, e => e.Diagnostic == Diagnostic.InvalidEnumValue);
        Assert.Contains("--mode", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("reference", ExecutionMode.Reference)]
    [InlineData("batched", ExecutionMode.Batched)]
    [InlineData("REFERENCE", ExecutionMode.Reference)]
    public void ModeEnumAcceptsBothSpellingsCaseInsensitively(string text, ExecutionMode expected)
    {
        var command = ParseRunOk("--mode", text);
        Assert.Equal(expected, command.Options.Mode);
    }

    private static readonly string[] ContinuedStreamsOnlyArgs = ["run", "input.dat", "--continued-streams"];

    [Fact]
    public void ContinuedStreamsWithoutBatchedBatchOneIsAnError()
    {
        var result = CommandLine.Parse(ContinuedStreamsOnlyArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.ContinuedStreamsRequiresBatchedBatchOne);
    }

    private static readonly string[] ContinuedStreamsBatchTwoArgs =
        ["run", "input.dat", "--mode", "batched", "--batch", "2", "--continued-streams"];

    [Fact]
    public void ContinuedStreamsWithModeBatchedButBatchTwoIsAnError()
    {
        var result = CommandLine.Parse(ContinuedStreamsBatchTwoArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.ContinuedStreamsRequiresBatchedBatchOne);
    }

    [Fact]
    public void ContinuedStreamsWithModeBatchedAndBatchOneSucceeds()
    {
        var command = ParseRunOk("--mode", "batched", "--batch", "1", "--continued-streams");
        Assert.True(command.Options.ContinuedStreams);
        Assert.Equal(1, command.Options.BatchSize);
    }

    [Fact]
    public void NoVerbIsAnError()
    {
        var result = CommandLine.Parse(Array.Empty<string>());
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.NoVerbGiven);
    }

    private static readonly string[] UnknownVerbArgs = ["fly"];

    [Fact]
    public void UnknownVerbIsAnError()
    {
        var result = CommandLine.Parse(UnknownVerbArgs);
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors, e => e.Diagnostic == Diagnostic.UnknownVerb);
        Assert.Contains("fly", error.Message, StringComparison.Ordinal);
    }

    private static readonly string[] RunNoPositionalArgs = ["run", "--mode", "batched"];

    [Fact]
    public void RunWithNoPositionalArgumentIsAnError()
    {
        var result = CommandLine.Parse(RunNoPositionalArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.RunMissingInputFile);
    }

    private static readonly string[] RunTwoPositionalArgs = ["run", "a.dat", "b.dat"];

    [Fact]
    public void RunWithTwoPositionalArgumentsIsAnError()
    {
        var result = CommandLine.Parse(RunTwoPositionalArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.RunTooManyPositionalArguments);
    }

    [Fact]
    public void RunDefaultOutputPathIsResultsMInTheCurrentDirectory()
    {
        var command = ParseRunOk();
        Assert.Equal(Path.GetFullPath("results.m"), command.OutputPath);
        Assert.Null(command.JsonPath);
    }

    [Fact]
    public void RunExplicitOutputAndJsonAreResolvedToFullPaths()
    {
        var command = ParseRunOk("--output", "out.m", "--json", "out.json");
        Assert.Equal(Path.GetFullPath("out.m"), command.OutputPath);
        Assert.Equal(Path.GetFullPath("out.json"), command.JsonPath);
    }

    [Fact]
    public void QuietSwitchSetsTheCommandsQuietFlag()
    {
        var command = ParseRunOk("--quiet");
        Assert.True(command.Quiet);
    }

    private static readonly string[] BareHelpArgs = ["--help"];

    [Fact]
    public void BareHelpParsesAsTheGeneralHelpCommand()
    {
        var result = CommandLine.Parse(BareHelpArgs);
        var help = Assert.IsType<HelpCommand>(result.Command);
        Assert.Null(help.Verb);
    }

    private static readonly string[] RunHelpArgs = ["run", "--help"];

    [Fact]
    public void HelpAfterAVerbParsesAsThatVerbsHelpCommand()
    {
        var result = CommandLine.Parse(RunHelpArgs);
        var help = Assert.IsType<HelpCommand>(result.Command);
        Assert.Equal("run", help.Verb);
    }

    private static readonly string[] VersionArgs = ["--version"];

    [Fact]
    public void VersionParsesAsTheVersionCommand()
    {
        var result = CommandLine.Parse(VersionArgs);
        _ = Assert.IsType<VersionCommand>(result.Command);
    }

    private static readonly string[] DevicesArgs = ["devices"];

    [Fact]
    public void DevicesWithNoArgumentsParses()
    {
        var result = CommandLine.Parse(DevicesArgs);
        Assert.True(result.Success);
        _ = Assert.IsType<DevicesCommand>(result.Command);
    }

    private static readonly string[] DevicesExtraArgs = ["devices", "extra"];

    [Fact]
    public void DevicesWithAnUnexpectedArgumentIsAnError()
    {
        var result = CommandLine.Parse(DevicesExtraArgs);
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Diagnostic == Diagnostic.UnknownOption);
    }

    private static readonly string[] DefaultsArgs = ["defaults"];

    [Fact]
    public void DefaultsWithNoArgumentsParses()
    {
        var result = CommandLine.Parse(DefaultsArgs);
        Assert.True(result.Success);
        _ = Assert.IsType<DefaultsCommand>(result.Command);
    }
}
