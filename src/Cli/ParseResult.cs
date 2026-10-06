namespace PropStruct.Cli;

/// <summary>Either a parsed command, or the errors that stopped parsing (BOOT.md, "The parser is this
/// node's own, hand-written").</summary>
internal sealed record ParseResult(ParsedCommand? Command, IReadOnlyList<ParseError> Errors)
{
    public bool Success => Command is not null && Errors.Count == 0;

    public static ParseResult OfCommand(ParsedCommand command) => new(command, Array.Empty<ParseError>());

    public static ParseResult OfError(ParseError error) => new(null, new[] { error });

    public static ParseResult OfErrors(IReadOnlyList<ParseError> errors) => new(null, errors);
}
