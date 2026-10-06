namespace PropStruct.Cli;

/// <summary>
/// Every kind of parse-time complaint the tool can raise (BOOT.md, "The parser is this node's own,
/// hand-written": "every diagnostic is a member of one enumeration of diagnostics, so that
/// <c>tests/Cli.Tests</c> can enumerate them instead of trusting a typed list"). A test enumerates this
/// type and drives the parser to produce each member at least once.
/// </summary>
internal enum Diagnostic
{
    /// <summary>No verb was given at all.</summary>
    NoVerbGiven,

    /// <summary>The first token is not one of <c>run</c>, <c>devices</c>, <c>defaults</c>.</summary>
    UnknownVerb,

    /// <summary>A token starting with <c>--</c> does not name a flag known to the current verb.</summary>
    UnknownOption,

    /// <summary>The same flag (by its canonical property, whichever spelling was used) was given twice.</summary>
    DuplicateOption,

    /// <summary>A value-taking flag has no following token, and no <c>=value</c> was attached.</summary>
    MissingOptionValue,

    /// <summary>A switch flag was given as <c>--switch=value</c>: switches take no value.</summary>
    SwitchDoesNotTakeValue,

    /// <summary>A value-taking flag's text does not parse as the number or length it expects.</summary>
    InvalidNumber,

    /// <summary>A value uses the Fortran exponent letter (<c>10d-6</c>) instead of <c>E</c>.</summary>
    FortranExponentRejected,

    /// <summary>An enumeration-valued flag (<c>--mode</c>, <c>--accelerator</c>, <c>--layout</c>) got a
    /// value outside its fixed set of spellings.</summary>
    InvalidEnumValue,

    /// <summary><c>run</c> was given no formulation path.</summary>
    RunMissingInputFile,

    /// <summary><c>run</c> was given more than one positional argument.</summary>
    RunTooManyPositionalArguments,

    /// <summary><c>--continued-streams</c> was given without <c>--mode batched --batch 1</c>.</summary>
    ContinuedStreamsRequiresBatchedBatchOne,
}
