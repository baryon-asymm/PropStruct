namespace PropStruct.Cli;

/// <summary>The exit codes of "One run, one exit code" (BOOT.md, Invariants).</summary>
internal static class ExitCode
{
    public const int Success = 0;
    public const int RunFailed = 1;
    public const int InvalidArguments = 2;
    public const int InfrastructureError = 3;
    public const int UnhandledException = 4;
    public const int Interrupted = 130;
}
