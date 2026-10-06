using System.Globalization;

namespace PropStruct.Input;

/// <summary>
/// Digital Visual Fortran list-directed reading, for the forms the archived <c>.dat</c> files use
/// (BOOT.md, "List-directed reading as Digital Visual Fortran does it"): a read draws the values it needs
/// from the current record and, when the record runs out, continues on the following ones; whatever is left
/// of the last record it drew a value from - trailing text included - is discarded, so the next read (or
/// skip) starts at the record after it. Values are separated by blanks, tabs or commas. Skipped records are
/// never decoded: the file is read and split into records as bytes mapped one-to-one to <see cref="char"/>
/// (Latin-1), which reproduces every ASCII byte - digits, signs, letters, separators - exactly regardless of
/// which of the archive's encodings (CP866, UTF-8, UTF-8 with a byte-order mark) produced the surrounding
/// label text; only label bytes, never decoded into meaning, can differ from their source encoding under
/// this mapping.
/// </summary>
internal sealed class DatRecordReader
{
    private readonly string[] _records;
    private int _nextRecord;

    public DatRecordReader(string text)
    {
        _records = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    /// <summary>Skips one record without decoding it, as a Fortran <c>READ</c> with a plain format skips a label line.</summary>
    public void SkipRecord()
    {
        if (_nextRecord >= _records.Length)
        {
            throw new FormulationFormatException("expected a record to skip, found the end of the file", _nextRecord + 1);
        }

        _nextRecord++;
    }

    /// <summary>
    /// Reads <paramref name="count"/> values, in order, from the current record onward. When
    /// <paramref name="requireIntegral"/> is set, every value must have no fractional part (the original's
    /// integer items still accept a real form, such as <c>6E6</c>, as long as its value is integral).
    /// </summary>
    public double[] ReadValues(int count, bool requireIntegral) => ReadValuesWithLines(count, requireIntegral).Values;

    /// <summary>
    /// As <see cref="ReadValues"/>, and also reports the 1-based line number each value was read from, so a
    /// caller can point at a specific value of a multi-value record (for instance <c>GSV</c>, the last of
    /// the <c>NMM JZZ KXX N NNZ GSV</c> record) in a message of its own.
    /// </summary>
    public (double[] Values, int[] LineNumbers) ReadValuesWithLines(int count, bool requireIntegral)
    {
        var values = new double[count];
        var lineNumbers = new int[count];
        var found = 0;
        var record = _nextRecord;

        while (found < count)
        {
            if (record >= _records.Length)
            {
                throw new FormulationFormatException(
                    $"expected {count} value(s), found {found} before the end of the file", record + 1);
            }

            var lineNumber = record + 1;
            foreach (var token in Tokenize(_records[record]))
            {
                if (found == count)
                {
                    break;
                }

                values[found] = ParseValue(token, lineNumber, requireIntegral);
                lineNumbers[found] = lineNumber;
                found++;
            }

            record++;
        }

        _nextRecord = record;
        return (values, lineNumbers);
    }

    private static double ParseValue(string token, int lineNumber, bool requireIntegral)
    {
        if (token.Contains('*'))
        {
            throw new FormulationFormatException($"repeat counts ('{token}') are not supported", lineNumber);
        }

        if (token == "/")
        {
            throw new FormulationFormatException("a data-terminating '/' is not supported", lineNumber);
        }

        var normalized = token.Replace('D', 'E').Replace('d', 'e');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormulationFormatException($"'{token}' is not a valid number", lineNumber);
        }

        if (requireIntegral && value != Math.Truncate(value))
        {
            throw new FormulationFormatException($"'{token}' is not an integral value for an integer item", lineNumber);
        }

        return value;
    }

    private static IEnumerable<string> Tokenize(string record)
    {
        var i = 0;
        while (i < record.Length)
        {
            while (i < record.Length && IsSeparator(record[i]))
            {
                i++;
            }

            if (i >= record.Length)
            {
                yield break;
            }

            var start = i;
            while (i < record.Length && !IsSeparator(record[i]))
            {
                i++;
            }

            yield return record[start..i];
        }
    }

    private static bool IsSeparator(char c) => c is ' ' or '\t' or ',' or '\r';
}
