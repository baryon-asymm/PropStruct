using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// L0: <see cref="AtomicFile.Write"/>'s own mechanism (BOOT.md: "each file is written to a temporary file
/// in its own directory and moved over the target, so that a reader never meets a half-written
/// `results.m`"), tested directly rather than only observed as "a run leaves a file behind" (reviewed
/// 2026-09-20).
/// </summary>
public class AtomicFileTests
{
    [Fact]
    public void WriteLeavesTheTargetUntouchedUntilTheCallbackReturnsThenMovesOverIt()
    {
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.Path, "results.m");
        File.WriteAllText(targetPath, "old content");

        string? tempPathSeenDuringWrite = null;
        AtomicFile.Write(targetPath, path =>
        {
            tempPathSeenDuringWrite = path;
            Assert.NotEqual(targetPath, path);
            Assert.True(File.Exists(targetPath), "the pre-existing target must still be the old file while writing.");
            Assert.Equal("old content", File.ReadAllText(targetPath));
            File.WriteAllText(path, "new content");
        });

        Assert.Equal("new content", File.ReadAllText(targetPath));
        Assert.NotNull(tempPathSeenDuringWrite);
        Assert.False(File.Exists(tempPathSeenDuringWrite), "the temporary file must not survive a successful write.");

        // No other file (the temporary one, before its move) was left behind in the directory.
        Assert.Equal(new[] { targetPath }, Directory.GetFiles(directory.Path));
    }

    [Fact]
    public void WriteOnAFailingCallbackLeavesNoTemporaryFileBehindAndTheTargetUnwritten()
    {
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.Path, "results.m");

        _ = Assert.Throws<InvalidOperationException>(() =>
            AtomicFile.Write(targetPath, path =>
            {
                File.WriteAllText(path, "partial content");
                throw new InvalidOperationException("simulated failure mid-write");
            }));

        Assert.False(File.Exists(targetPath), "a failed write must not create the target at all.");
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public void WriteOnAFailedMoveNamesTheTargetPathInTheRethrownMessage()
    {
        // Locking the target open on Windows (without FileShare.Delete) makes File.Move's overwrite fail
        // - measured, as UnauthorizedAccessException, not IOException - with a message the runtime itself
        // does not name the target path in; AtomicFile.Write's own wrapping (added in review, 2026-09-20)
        // must.
        using var directory = new TemporaryDirectory();
        var targetPath = Path.Combine(directory.Path, "results.m");
        File.WriteAllText(targetPath, "old content");

        using var lockedHandle = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            AtomicFile.Write(targetPath, path => File.WriteAllText(path, "new content")));

        Assert.Contains(targetPath, exception.Message, StringComparison.Ordinal);
    }
}
