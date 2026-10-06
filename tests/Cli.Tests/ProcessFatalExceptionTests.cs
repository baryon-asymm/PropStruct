using Xunit;

namespace PropStruct.Cli.Tests;

/// <summary>
/// <see cref="Program.IsProcessFatal"/> is the <c>when</c> filter on <c>Program.Dispatch</c>'s and
/// <c>Program.ExecuteRun</c>'s catch-alls (CA1031, owner's decision 2026-09-24; BOOT.md, "`catch (Exception
/// ex)` narrow by a `when` filter"). This tests the predicate directly rather than driving a real
/// <see cref="OutOfMemoryException"/> or <see cref="InsufficientExecutionStackException"/> through
/// <see cref="Program.Run"/>: throwing either for real, in-process, would tear down the xunit test host
/// itself (the same reason the BOOT.md note gives for not using <c>AppDomain.UnhandledException</c> as the
/// boundary) rather than fail one test.
/// </summary>
public class ProcessFatalExceptionTests
{
    [Theory]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(InsufficientExecutionStackException))]
    public void NamedProcessFatalTypesAreReportedFatal(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.True(Program.IsProcessFatal(exception));
    }

    // A representative sample of exception types this tool already maps to a specific exit code
    // (BOOT.md's own Errors table) - none of them is process-fatal, so the `when` filter must let every
    // one of them keep reaching MapExceptionToExitCode exactly as it did before the filter was added.
    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(NullReferenceException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(NotSupportedException))]
    public void OrdinaryExceptionTypesAreNotReportedFatal(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.False(Program.IsProcessFatal(exception));
    }

    [Fact]
    public void ADerivedProcessFatalExceptionIsStillReportedFatal() =>
        // The filter matches by type hierarchy (a C# `is` pattern), not by exact type: a custom
        // OutOfMemoryException subtype (nothing in this tree defines one, but the BCL allows it) must
        // still propagate rather than be mapped to exit 4.
        Assert.True(Program.IsProcessFatal(new DerivedOutOfMemoryException()));

    // CA1032: a type derived from Exception needs the three standard constructors, even a
    // private, test-only one exercised through the parameterless constructor alone above.
    private sealed class DerivedOutOfMemoryException : OutOfMemoryException
    {
        public DerivedOutOfMemoryException()
        {
        }

        public DerivedOutOfMemoryException(string message)
            : base(message)
        {
        }

        public DerivedOutOfMemoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
