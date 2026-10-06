namespace PropStruct.Input;

/// <summary>
/// A <c>.dat</c> file could not be read as the original reads it (BOOT.md, "Invariants" and "Taboos"):
/// a missing or malformed value, a repeat count (<c>r*v</c>) or a data-terminating slash (neither used by
/// any archived file), a non-integral value for an integer item, or <c>GSV != 2</c>.
/// </summary>
public sealed class FormulationFormatException : FormatException
{
    /// <summary>The 1-based line number of the record on which the problem was found; 0 when unknown.</summary>
    public int LineNumber { get; }

    /// <summary>The standard parameterless constructor (CA1032); <see cref="LineNumber"/> is 0.</summary>
    public FormulationFormatException()
    {
    }

    /// <summary>The standard message-only constructor (CA1032); <see cref="LineNumber"/> is 0.</summary>
    public FormulationFormatException(string message)
        : base(message)
    {
    }

    /// <summary>The standard message-and-inner-exception constructor (CA1032); <see cref="LineNumber"/> is 0.</summary>
    public FormulationFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The constructor every parse failure in this node actually raises: <paramref name="message"/>
    /// names the problem, <paramref name="lineNumber"/> sets <see cref="LineNumber"/> to the 1-based record
    /// where it was found.</summary>
    public FormulationFormatException(string message, int lineNumber)
        : base(message)
    {
        LineNumber = lineNumber;
    }

    /// <summary>As <see cref="FormulationFormatException(string, int)"/>, with an <paramref name="innerException"/>
    /// this failure was caused by (CA1032).</summary>
    public FormulationFormatException(string message, int lineNumber, Exception innerException)
        : base(message, innerException)
    {
        LineNumber = lineNumber;
    }
}
