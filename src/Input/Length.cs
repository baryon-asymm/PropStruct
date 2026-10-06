namespace PropStruct.Input;

/// <summary>
/// A length exactly as the original's <c>.dat</c> file wrote it, together with the original's unit rule
/// (BOOT.md, "Lengths keep the value as written", source lines 260-266): a value <c>&gt;= 0.1</c> is
/// micrometres, any other value is metres, decided per element, never per file.
/// </summary>
public readonly record struct Length(double AsWritten)
{
    /// <summary>Whether <see cref="AsWritten"/> is the original's micrometre form (<c>AsWritten &gt;= 0.1</c>).</summary>
    public bool IsMicrometres => AsWritten >= 0.1;

    /// <summary>
    /// <see cref="AsWritten"/> converted to metres: <c>AsWritten * 1e-6</c> when <see cref="IsMicrometres"/>,
    /// <see cref="AsWritten"/> unchanged otherwise.
    /// </summary>
    public double Metres => IsMicrometres ? AsWritten * 1e-6 : AsWritten;

    /// <summary>Builds a <see cref="Length"/> from a value already in metres (so read back as written).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="metres"/> is not less than 0.1.</exception>
    public static Length FromMetres(double metres)
    {
        if (!(metres < 0.1))
        {
            throw new ArgumentOutOfRangeException(nameof(metres), metres,
                "A value written as metres must be less than 0.1, or it would read back as micrometres.");
        }

        return new Length(metres);
    }

    /// <summary>Builds a <see cref="Length"/> from a value already in micrometres (so read back as written).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="micrometres"/> is less than 0.1.</exception>
    public static Length FromMicrometres(double micrometres)
    {
        if (!(micrometres >= 0.1))
        {
            throw new ArgumentOutOfRangeException(nameof(micrometres), micrometres,
                "A value written as micrometres must be at least 0.1, or it would read back as metres.");
        }

        return new Length(micrometres);
    }
}
