using Xunit;

namespace PropStruct.Execution.Tests;

/// <summary>
/// L0 (BOOT.md, Opus audit item 9, 2026-09-18): the explicit <see cref="LibDeviceLocator.LibNvvmPathVariable"/>/
/// <see cref="LibDeviceLocator.LibDevicePathVariable"/> override of <see cref="LibDeviceLocator.Locate(LocatorPlatform, Func{string, string?}, string)"/>,
/// restored from APThermo's own locator (dropped when this node was ported) for a user whose CUDA toolkit
/// is not laid out where the search below the override expects.
/// </summary>
public sealed class LibDeviceLocatorTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "propstruct-libdevice-tests-" + Guid.NewGuid().ToString("N"))).FullName;

    [Fact]
    public void LocateWithBothOverridesSetAndExistingUsesThemDirectlyWithoutSearching()
    {
        var dll = WriteTempFile("nvvm.dll");
        var bitcode = WriteTempFile("libdevice.10.bc");
        var environment = Overriding(dll, bitcode);

        // globRoot has no toolkit layout at all: a non-null result can only come from the override.
        var (foundDll, foundBitcode, tried) = LibDeviceLocator.Locate(LocatorPlatform.Windows, environment, _tempDir);

        Assert.Equal(dll, foundDll);
        Assert.Equal(bitcode, foundBitcode);
        Assert.Contains(dll, tried);
        Assert.Contains(bitcode, tried);
    }

    [Fact]
    public void LocateWithBothOverridesSetButMissingFailsWithoutFallingBackToSearch()
    {
        var dll = Path.Combine(_tempDir, "missing-nvvm.dll");
        var bitcode = Path.Combine(_tempDir, "missing-libdevice.10.bc");
        var environment = Overriding(dll, bitcode);

        // A real, discoverable toolkit layout at the same globRoot the search would use: a fall-back to
        // search (rather than a clean failure of the explicit override) would be caught by a non-null result.
        _ = BuildToolkitLayout();

        var (foundDll, foundBitcode, tried) = LibDeviceLocator.Locate(LocatorPlatform.Windows, environment, _tempDir);

        Assert.Null(foundDll);
        Assert.Null(foundBitcode);
        Assert.Contains(dll, tried);
        Assert.Contains(bitcode, tried);
    }

    [Fact]
    public void LocateWithOnlyOneOverrideSetFallsBackToNormalDiscovery()
    {
        var (realDll, realBitcode) = BuildToolkitLayout();
        var environment = Overriding(WriteTempFile("nvvm.dll"), null); // PROPSTRUCT_LIBNVVM_PATH set, PROPSTRUCT_LIBDEVICE_PATH not

        var (foundDll, foundBitcode, _) = LibDeviceLocator.Locate(LocatorPlatform.Windows, environment, _tempDir);

        Assert.Equal(realDll, foundDll);
        Assert.Equal(realBitcode, foundBitcode);
    }

    [Fact]
    public void LocateWithNeitherOverrideSetFallsBackToNormalDiscovery()
    {
        var (realDll, realBitcode) = BuildToolkitLayout();

        var (foundDll, foundBitcode, _) = LibDeviceLocator.Locate(LocatorPlatform.Windows, _ => null, _tempDir);

        Assert.Equal(realDll, foundDll);
        Assert.Equal(realBitcode, foundBitcode);
    }

    private (string Dll, string Bitcode) BuildToolkitLayout()
    {
        var toolkitRoot = Path.Combine(_tempDir, "v12.8");
        _ = Directory.CreateDirectory(Path.Combine(toolkitRoot, "nvvm", "bin"));
        _ = Directory.CreateDirectory(Path.Combine(toolkitRoot, "nvvm", "libdevice"));
        var dll = Path.Combine(toolkitRoot, "nvvm", "bin", "nvvm64_40_0.dll");
        var bitcode = Path.Combine(toolkitRoot, "nvvm", "libdevice", "libdevice.10.bc");
        File.WriteAllBytes(dll, []);
        File.WriteAllBytes(bitcode, []);
        return (dll, bitcode);
    }

    private string WriteTempFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllBytes(path, []);
        return path;
    }

    private static Func<string, string?> Overriding(string? dll, string? bitcode) => name => name switch
    {
        LibDeviceLocator.LibNvvmPathVariable => dll,
        LibDeviceLocator.LibDevicePathVariable => bitcode,
        _ => null,
    };

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
