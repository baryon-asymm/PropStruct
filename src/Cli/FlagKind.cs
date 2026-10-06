namespace PropStruct.Cli;

/// <summary>The shape a flag's value must take, form only (BOOT.md, "The tool validates form, never
/// meaning"). <see cref="PositiveInteger"/>/<see cref="PositiveLong"/> are the budget-style flags whose
/// own grammar is "a positive integer" (BOOT.md, "Flag names are the original's own names, with
/// aliases", the quoted diagnostic "`--batch` expects a positive integer, got `x`") - the CLI never asks
/// the model whether a budget is acceptable, but a non-positive budget is not a value this grammar
/// accepts in the first place.</summary>
internal enum FlagKind
{
    /// <summary>A length handed to <see cref="Input.Length"/> exactly as written.</summary>
    Length,

    /// <summary>Any finite <see cref="double"/>.</summary>
    Number,

    /// <summary>Any <see cref="int"/>, positive, negative or zero.</summary>
    Integer,

    /// <summary>An <see cref="int"/> that must be strictly positive.</summary>
    PositiveInteger,

    /// <summary>A <see cref="long"/> that must be strictly positive.</summary>
    PositiveLong,

    /// <summary>A <see cref="ulong"/>, zero included (zero has its own meaning: "the layout's initial states").</summary>
    UnsignedLong,

    /// <summary>Takes no value; its presence sets a <see cref="bool"/> to <see langword="true"/>.</summary>
    Switch,

    /// <summary>One of a fixed, case-insensitive set of spellings, each mapped to a value.</summary>
    Enum,

    /// <summary>A filesystem path, taken verbatim.</summary>
    Path,
}
