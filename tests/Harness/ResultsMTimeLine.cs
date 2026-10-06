namespace PropStruct.Tests.Harness;

/// <summary>
/// The one cut of the time line of a <c>results.m</c> that every comparison of two files of the original's
/// layout needs: the single line the output writes from the wall clock is the only one two runs of the same
/// options may differ in (root BOOT.md: "no result depending on wall time except the time line of
/// <c>results.m</c>").
/// </summary>
public static class ResultsMTimeLine
{
    private static ReadOnlySpan<byte> Marker => "Calculation time"u8;

    /// <summary>
    /// The bytes of <paramref name="resultsM"/> without the one line containing <c>Calculation time</c>, its line
    /// terminator included, nothing else changed (a CRLF/LF difference elsewhere survives).
    /// </summary>
    /// <param name="resultsM">The bytes of a <c>results.m</c>.</param>
    /// <returns>A new array.</returns>
    /// <exception cref="InvalidOperationException">No line contains the marker: a file without a time line is not
    /// a <c>results.m</c> of this program, and a cut that returned it unchanged would compare as equal.</exception>
    public static byte[] Remove(byte[] resultsM)
    {
        ArgumentNullException.ThrowIfNull(resultsM);

        var bytes = resultsM.AsSpan();
        var markerIndex = bytes.IndexOf(Marker);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException("the results.m holds no line containing 'Calculation time'.");
        }

        var lineStart = bytes[..markerIndex].LastIndexOf((byte)'\n') + 1;
        var terminator = bytes[markerIndex..].IndexOf((byte)'\n');
        var lineEnd = terminator < 0 ? bytes.Length : markerIndex + terminator + 1;

        var result = new byte[bytes.Length - (lineEnd - lineStart)];
        bytes[..lineStart].CopyTo(result);
        bytes[lineEnd..].CopyTo(result.AsSpan(lineStart));
        return result;
    }
}
