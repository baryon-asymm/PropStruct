namespace PropStruct.Cli;

/// <summary>
/// Writes a file so a reader never meets a half-written one (BOOT.md, "`results.m` goes where the
/// original put it": "each file is written to a temporary file in its own directory and moved over the
/// target"). The temporary file lives beside the target so the final move is same-volume and atomic.
/// </summary>
internal static class AtomicFile
{
    public static void Write(string targetPath, Action<string> writeToPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        var tempPath = Path.Combine(
            string.IsNullOrEmpty(directory) ? "." : directory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            writeToPath(tempPath);
            File.Move(tempPath, targetPath, overwrite: true);
        }
        catch (IOException ex)
        {
            // The runtime's own message rarely names the file being written; the exit-3 message this
            // becomes needs the path to be useful (reviewed 2026-09-20: BOOT.md claimed "the full path
            // ... in every message of exit 3", which this route did not carry).
            throw new IOException($"{targetPath}: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"{targetPath}: {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
