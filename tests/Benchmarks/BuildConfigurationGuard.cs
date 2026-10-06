namespace PropStruct.Benchmarks;

/// <summary>
/// Refuses to record a figure from a Debug build (BOOT.md, Invariants: "the tree already paid
/// for this once", root BOOT.md, "One particle program", the 2026-09-19 ⚠: figures measured
/// under a Debug build understated the host-thread path by a factor of three). The check is a
/// compile-time constant baked into this very binary, not a runtime probe of some other
/// process's build: <c>dotnet run -c Release</c> (this node's own API.md command line) compiles
/// this file with <c>DEBUG</c> undefined; <c>dotnet build</c>/<c>dotnet run</c> without
/// <c>-c Release</c> defines it, matching the default configuration that already cost the tree
/// once. <see cref="Program.Main"/> checks this before parsing a single argument or touching
/// the simulator, so a Debug build never gets far enough to print a row.
/// </summary>
internal static class BuildConfigurationGuard
{
    public static bool IsDebugBuild =>
#if DEBUG
            true;
#else
            false;
#endif

}
