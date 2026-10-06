using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace PropStruct.Tests.Harness;

/// <summary>
/// A snapshot of exact bits: a tripwire, not a contract, the way <c>PublicSurface.approved.txt</c> is one over
/// the contract (root BOOT.md; AGENTS.md §13). Each call to <see cref="Verify"/> checks one named case of one
/// family against a SHA-256 of the little-endian bits of the values given (<see cref="BitConverter.DoubleToInt64Bits(double)"/>,
/// so a signed zero or a NaN payload is told apart), never against a tolerance. The approved file for a family
/// lives at <c>Snapshots/&lt;family&gt;.approved.txt</c> beside the caller's own source file, one "name hash"
/// line per case. A moved snapshot, or a case missing from the approved file, fails the call: this node never
/// rewrites the approved file, it only ever writes <c>Snapshots/&lt;family&gt;.received.txt</c> beside it, and
/// that file always ends complete through the last case the running test passed, so a reviewer can replace the
/// approved file with it in one step once it looks right (this node's BOOT.md, "a moved bit snapshot fails the
/// test; approval is a deliberate file change"). The first line of every received file, and of every approved
/// file that carries one, is <c>PlatformLine</c>: the platform the bits were produced on. It is
/// information, never a gate: the failure message of a moved hash quotes both platform lines, so that a platform
/// move reads as one and not as a code regression, and a file whose hashes all match passes whatever its
/// platform line says.
/// </summary>
public static class BitSnapshot
{
    private static readonly ConcurrentDictionary<string, object> Gates = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, List<(string Name, string Hash)>> Received = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, bool> Dirty = new(StringComparer.Ordinal);
    private static readonly Lazy<string> CurrentPlatformLine = new(DescribePlatform);

    private const string PlatformPrefix = "# platform:";

    /// <summary>
    /// The platform this process computes on, as the first line of a snapshot file:
    /// <c># platform: &lt;OS description&gt;; &lt;framework description&gt;; &lt;process architecture&gt;; ucrtbase
    /// &lt;FileVersion, or n/a off Windows&gt;; fma3 &lt;Fma.IsSupported&gt;</c>. <c>Math.Log</c> and <c>Math.Pow</c>
    /// come from the C runtime, whose x64 build on Windows picks FMA3 implementations by CPU at startup, and
    /// glibc differs altogether, so these are the facts a moved hash is first checked against.
    /// </summary>
    private static string PlatformLine => CurrentPlatformLine.Value;

    /// <summary>
    /// Checks that <paramref name="values"/> hash to the case <paramref name="name"/> of family
    /// <paramref name="family"/> in the approved snapshot beside the caller's source file. Throws
    /// <see cref="InvalidOperationException"/>, naming the case and where the received file was written, when
    /// the case is missing or its hash has moved.
    /// </summary>
    public static void Verify(string family, string name, ReadOnlySpan<double> values, [CallerFilePath] string callerFilePath = "")
    {
        var approvedPath = ApprovedPath(family, callerFilePath);
        var receivedPath = ReceivedPathOf(approvedPath);
        var actualHash = HashOf(values);

        var gate = Gates.GetOrAdd(approvedPath, static _ => new object());
        lock (gate)
        {
            var pairs = Received.GetOrAdd(approvedPath, static _ => []);
            pairs.Add((name, actualHash));

            var approved = LoadApproved(approvedPath);
            string? problem = null;
            if (!approved.TryGetValue(name, out var recordedHash))
            {
                problem = $"{name}: not in {approvedPath}. A new case is approved like a changed one: review " +
                          $"{receivedPath} and, once it looks right, replace {approvedPath} with it in the same commit.";
            }
            else if (!string.Equals(recordedHash, actualHash, StringComparison.Ordinal))
            {
                problem = $"{name}: {approvedPath} records {recordedHash}, this run gives {actualHash}. Review " +
                          $"{receivedPath} and, if the change is intended, replace {approvedPath} with it, named in the commit.";
            }

            if (problem is not null)
            {
                problem += PlatformNote(approvedPath);
                Dirty[approvedPath] = true;
            }

            if (Dirty.ContainsKey(approvedPath))
            {
                WriteReceived(receivedPath, pairs);
            }

            if (problem is not null)
            {
                throw new InvalidOperationException(problem);
            }
        }
    }

    private static string PlatformNote(string approvedPath)
    {
        var approvedLine = LoadApprovedPlatformLine(approvedPath);
        var current = PlatformLine;
        var note = $" Approved platform: {approvedLine ?? "none recorded"}. Current platform: {current}.";
        if (!string.Equals(approvedLine, current, StringComparison.Ordinal))
        {
            note += " The platforms differ, so this may be a platform move and not a code change: it is " +
                    "re-approved only in a commit naming both platforms, never folded into a code change.";
        }

        return note;
    }

    private static string? LoadApprovedPlatformLine(string approvedPath)
    {
        if (!File.Exists(approvedPath))
        {
            return null;
        }

        return File.ReadLines(approvedPath).FirstOrDefault(line => line.StartsWith(PlatformPrefix, StringComparison.Ordinal));
    }

    private static string DescribePlatform()
    {
        var ucrtbase = OperatingSystem.IsWindows() ? UcrtbaseFileVersion() : "n/a";
        return $"{PlatformPrefix} {RuntimeInformation.OSDescription}; {RuntimeInformation.FrameworkDescription}; " +
               $"{RuntimeInformation.ProcessArchitecture}; ucrtbase {ucrtbase}; fma3 {Fma.IsSupported}";
    }

    private static string UcrtbaseFileVersion()
    {
        using var process = Process.GetCurrentProcess();
        foreach (ProcessModule module in process.Modules)
        {
            if (string.Equals(module.ModuleName, "ucrtbase.dll", StringComparison.OrdinalIgnoreCase))
            {
                return module.FileVersionInfo.FileVersion ?? "n/a";
            }
        }

        return "n/a";
    }

    private static string ApprovedPath(string family, string callerFilePath)
    {
        var callerDirectory = Path.GetDirectoryName(callerFilePath)
            ?? throw new InvalidOperationException("BitSnapshot.Verify could not resolve the caller's source directory.");
        return Path.Combine(callerDirectory, "Snapshots", family + ".approved.txt");
    }

    private static string ReceivedPathOf(string approvedPath)
    {
        var directory = Path.GetDirectoryName(approvedPath)!;
        var fileName = Path.GetFileName(approvedPath).Replace("approved", "received", StringComparison.Ordinal);
        return Path.Combine(directory, fileName);
    }

    private static Dictionary<string, string> LoadApproved(string approvedPath)
    {
        var approved = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(approvedPath))
        {
            return approved;
        }

        foreach (var line in File.ReadAllLines(approvedPath))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var at = line.IndexOf(' ');
            if (at < 0)
            {
                continue;
            }

            approved[line[..at]] = line[(at + 1)..];
        }

        return approved;
    }

    private static void WriteReceived(string receivedPath, List<(string Name, string Hash)> pairs)
    {
        var directory = Path.GetDirectoryName(receivedPath)!;
        _ = Directory.CreateDirectory(directory);
        File.WriteAllLines(receivedPath, pairs.Select(pair => pair.Name + " " + pair.Hash).Prepend(PlatformLine));
    }

    private static string HashOf(ReadOnlySpan<double> values)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        foreach (var value in values)
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes, BitConverter.DoubleToInt64Bits(value));
            hash.AppendData(bytes);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
