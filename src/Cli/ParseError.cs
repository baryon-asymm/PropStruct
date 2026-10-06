namespace PropStruct.Cli;

/// <summary>One parse-time complaint: which <see cref="Diagnostic"/> it is, and the exact text that goes
/// to stderr (BOOT.md, "every diagnostic is a member of one enumeration"; tests/Cli.Tests/BOOT.md,
/// "a diagnostic is asserted by its text, not by the exit code alone").</summary>
internal sealed record ParseError(Diagnostic Diagnostic, string Message);
