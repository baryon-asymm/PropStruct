namespace PropStruct.Execution;

/// <summary>The host platform for libdevice discovery. Any value but <see cref="Windows"/> and <see cref="Linux"/> does no discovery.</summary>
internal enum LocatorPlatform
{
    Windows,
    Linux,
    Other,
}

/// <summary>
/// Finds libnvvm and libdevice under the CUDA toolkit's own layout (BOOT.md, "LibDevicePostLink and
/// LibDeviceLocator are taken from APThermo"). Root BOOT.md, Constraints: "Nothing but CUDA library
/// discovery may be platform-specific, so that Linux is a later constraint change, not a redesign" — this
/// node stays platform-generic even though version 1 runs Windows only.
/// </summary>
internal static class LibDeviceLocator
{
    private const string WindowsDllName = "nvvm64_40_0.dll";
    private const string LinuxDllName = "libnvvm.so";
    private const string BitcodeName = "libdevice.10.bc";

    /// <summary>The environment variable that names the libnvvm file directly, bypassing toolkit discovery.</summary>
    public const string LibNvvmPathVariable = "PROPSTRUCT_LIBNVVM_PATH";

    /// <summary>The environment variable that names the libdevice bitcode file directly, bypassing toolkit discovery.</summary>
    public const string LibDevicePathVariable = "PROPSTRUCT_LIBDEVICE_PATH";

    /// <summary>The platform's libnvvm file name, named in a message when it was not found.</summary>
    public static string LibraryFileName => DllName(CurrentPlatform());

    /// <summary>The dll and bitcode paths, or nulls, with every path examined.</summary>
    public static (string? Dll, string? Bitcode, IReadOnlyList<string> Tried) Locate() => Locate(Environment.GetEnvironmentVariable);

    /// <summary>
    /// <see cref="Locate()"/> with <paramref name="environment"/> in place of
    /// <see cref="Environment.GetEnvironmentVariable(string)"/>, for a caller that already has an injected
    /// environment of its own (<see cref="AcceleratorBinding.Cuda(Func{string, string?})"/>, review item
    /// 3, 2026-09-18: one environment source per engine creation, not real process state mixed with an
    /// injected one).
    /// </summary>
    internal static (string? Dll, string? Bitcode, IReadOnlyList<string> Tried) Locate(Func<string, string?> environment)
    {
        var platform = CurrentPlatform();
        return Locate(platform, environment, DefaultGlobRoot(platform, environment));
    }

    /// <summary>
    /// The seam the tests drive: <paramref name="platform"/> in place of <see cref="OperatingSystem"/>, <paramref name="environment"/>
    /// in place of <see cref="Environment.GetEnvironmentVariable(string)"/>, and <paramref name="globRoot"/> in place of the
    /// platform's own fixed base directory (<c>%ProgramFiles%\NVIDIA GPU Computing Toolkit\CUDA</c> on Windows, <c>/usr/local</c> on
    /// Linux) under which the versioned toolkit directories are found. <see cref="LibNvvmPathVariable"/> and
    /// <see cref="LibDevicePathVariable"/>, when both are set, override toolkit discovery entirely and point straight at the two
    /// files (review item 9, 2026-09-18: a user with a non-standard toolkit layout, e.g. libnvvm and libdevice copied out of
    /// the installer's own directory structure, has no other way to be found by the search below).
    /// </summary>
    internal static (string? Dll, string? Bitcode, IReadOnlyList<string> Tried) Locate(
        LocatorPlatform platform, Func<string, string?> environment, string globRoot)
    {
        var tried = new List<string>();

        var overrideDll = environment(LibNvvmPathVariable);
        var overrideBitcode = environment(LibDevicePathVariable);
        if (!string.IsNullOrWhiteSpace(overrideDll) && !string.IsNullOrWhiteSpace(overrideBitcode))
        {
            // Both set: an explicit override, checked and used or refused on its own terms — not silently
            // ignored in favour of a toolkit search the user's own layout may not satisfy anyway.
            tried.Add(overrideDll);
            tried.Add(overrideBitcode);
            return File.Exists(overrideDll) && File.Exists(overrideBitcode)
                ? (overrideDll, overrideBitcode, tried)
                : (null, null, tried);
        }

        if (platform == LocatorPlatform.Other)
        {
            return (null, null, tried);
        }

        var dllName = DllName(platform);
        foreach (var root in ToolkitRoots(platform, environment, globRoot))
        {
            var bitcode = Path.Combine(root, "nvvm", "libdevice", BitcodeName);
            foreach (var dll in DllCandidates(platform, root, dllName))
            {
                tried.Add(dll);
                if (!File.Exists(dll))
                {
                    continue;
                }

                tried.Add(bitcode);
                if (File.Exists(bitcode))
                {
                    return (dll, bitcode, tried);
                }
            }
        }

        return (null, null, tried);
    }

    /// <summary>The platform's own default glob root: the base directory under which versioned toolkit directories are found.</summary>
    private static string DefaultGlobRoot(LocatorPlatform platform, Func<string, string?> environment) => platform switch
    {
        LocatorPlatform.Windows => Path.Combine(environment("ProgramFiles") ?? @"C:\Program Files", "NVIDIA GPU Computing Toolkit", "CUDA"),
        LocatorPlatform.Linux => "/usr/local",
        LocatorPlatform.Other => "",
        _ => "",
    };

    private static LocatorPlatform CurrentPlatform() =>
        OperatingSystem.IsWindows() ? LocatorPlatform.Windows :
        OperatingSystem.IsLinux() ? LocatorPlatform.Linux : LocatorPlatform.Other;

    private static string DllName(LocatorPlatform platform) => platform switch
    {
        LocatorPlatform.Windows => WindowsDllName,
        LocatorPlatform.Linux => LinuxDllName,
        LocatorPlatform.Other => WindowsDllName,
        _ => WindowsDllName,
    };

    /// <summary>The library file(s) tried under one root, in order: two layouts on Windows, one on Linux.</summary>
    private static IEnumerable<string> DllCandidates(LocatorPlatform platform, string root, string dllName) =>
        platform == LocatorPlatform.Windows
            ? [Path.Combine(root, "nvvm", "bin", dllName), Path.Combine(root, "nvvm", "bin", "x64", dllName)]
            : [Path.Combine(root, "nvvm", "lib64", dllName)];

    /// <summary>
    /// Windows: <c>CUDA_PATH</c>, then the toolkit directories under <paramref name="globRoot"/> from the newest version down.
    /// Linux: <c>CUDA_PATH</c>, then <c>CUDA_HOME</c>, then <c>&lt;globRoot&gt;/cuda</c>, then the <c>cuda-*</c> directories under
    /// <paramref name="globRoot"/> from the newest version down. A root already yielded is skipped.
    /// </summary>
    private static IEnumerable<string> ToolkitRoots(LocatorPlatform platform, Func<string, string?> environment, string globRoot)
    {
        var comparer = platform == LocatorPlatform.Windows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var seen = new HashSet<string>(comparer);

        var cudaPath = environment("CUDA_PATH");
        if (!string.IsNullOrWhiteSpace(cudaPath) && seen.Add(cudaPath))
        {
            yield return cudaPath;
        }

        if (platform == LocatorPlatform.Linux)
        {
            var cudaHome = environment("CUDA_HOME");
            if (!string.IsNullOrWhiteSpace(cudaHome) && seen.Add(cudaHome))
            {
                yield return cudaHome;
            }

            var fixedRoot = Path.Combine(globRoot, "cuda");
            if (seen.Add(fixedRoot))
            {
                yield return fixedRoot;
            }

            foreach (var dir in VersionedDirectories(globRoot, "cuda-*", "cuda-"))
            {
                if (seen.Add(dir))
                {
                    yield return dir;
                }
            }

            yield break;
        }

        foreach (var dir in VersionedDirectories(globRoot, "v*", "v"))
        {
            if (seen.Add(dir))
            {
                yield return dir;
            }
        }
    }

    /// <summary>The subdirectories of <paramref name="baseDir"/> matching <paramref name="pattern"/>, newest version first.</summary>
    private static IEnumerable<string> VersionedDirectories(string baseDir, string pattern, string prefix)
    {
        if (!Directory.Exists(baseDir))
        {
            yield break;
        }

        var versioned = Directory.GetDirectories(baseDir, pattern)
            .Select(dir => (Dir: dir, Version: ParseVersion(Path.GetFileName(dir), prefix)))
            .Where(entry => entry.Version is not null)
            .OrderByDescending(entry => entry.Version)
            .Select(entry => entry.Dir);
        foreach (var dir in versioned)
        {
            yield return dir;
        }
    }

    private static Version? ParseVersion(string name, string prefix) =>
        name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Version.TryParse(name[prefix.Length..], out var version) ? version : null;
}
