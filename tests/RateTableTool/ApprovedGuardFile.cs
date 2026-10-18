using System.Globalization;

namespace PropStruct.RateTableTool;

/// <summary>
/// The format of the two approved files of the change detectors (<see cref="OriginalModelGuard"/> and
/// <see cref="CorrectedModelGuard"/>): one line per case, <c>&lt;id&gt; &lt;SHA-256 of results.m&gt; &lt;SHA-256 of the
/// result JSON&gt;</c>, and a <c>#</c> line a comment. One writer and one reader for both files, so that neither guard
/// has a copy of the format (root BOOT.md Taboos: no second implementation).
/// </summary>
internal static class ApprovedGuardFile
{
    /// <summary>The line of <paramref name="id"/>: its <c>results.m</c> hash (time line cut) and its normalised result JSON hash.</summary>
    public static string Line(string id, ReferenceModeRunner.Run run) =>
        string.Create(CultureInfo.InvariantCulture, $"{id} {run.Sha256Hex} {run.JsonSha256Hex}");

    /// <summary>The approved hashes by case id: (<c>results.m</c> hash, JSON hash). Empty when the file does
    /// not exist, so that the test can fail with the regeneration command instead of a file error.</summary>
    public static Dictionary<string, (string ResultsM, string Json)> Load(string path)
    {
        var map = new Dictionary<string, (string ResultsM, string Json)>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return map;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split(' ');
            map[fields[0]] = (fields[1], fields[2]);
        }

        return map;
    }

    /// <summary>Writes <paramref name="comments"/> then <paramref name="lines"/> to <paramref name="path"/>, LF line endings.</summary>
    public static void Write(string path, IEnumerable<string> comments, IEnumerable<string> lines)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Concat(comments.Concat(lines).Select(line => line + '\n')));
    }
}
