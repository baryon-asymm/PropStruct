using PropStruct.Input;
using PropStruct.Simulation;

namespace PropStruct.Cli;

/// <summary>One successfully parsed command line (BOOT.md, "The parser is this node's own, hand-written":
/// "<c>CommandLine.Parse(string[] args)</c> returns either a parsed command or a list of errors").</summary>
internal abstract record ParsedCommand;

/// <summary>
/// <c>propstruct run &lt;file.dat&gt; [options]</c>. <paramref name="OutputPath"/> is already resolved to a
/// full path against the process working directory (BOOT.md, "<c>results.m</c> goes where the original
/// put it"); <paramref name="JsonPath"/> is <c>null</c> unless <c>--json</c> was given.
/// </summary>
internal sealed record RunCommand(
    string InputPath,
    ModelParameters Parameters,
    SimulationOptions Options,
    string OutputPath,
    string? JsonPath,
    bool Quiet) : ParsedCommand;

/// <summary><c>propstruct devices</c>.</summary>
internal sealed record DevicesCommand : ParsedCommand;

/// <summary><c>propstruct defaults</c>.</summary>
internal sealed record DefaultsCommand : ParsedCommand;

/// <summary><c>--help</c>/<c>-h</c>, bare or right after a verb; <see cref="Verb"/> is <c>null</c> for the
/// bare form.</summary>
internal sealed record HelpCommand(string? Verb) : ParsedCommand;

/// <summary><c>--version</c>.</summary>
internal sealed record VersionCommand : ParsedCommand;
