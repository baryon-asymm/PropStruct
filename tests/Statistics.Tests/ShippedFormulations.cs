using PropStruct.Input;
using PropStruct.Tests.Harness;

namespace PropStruct.Statistics.Tests;

/// <summary>The formulation files the repository ships in <c>tests/Fixtures/Legacy/formulations</c>, by name.</summary>
internal static class ShippedFormulations
{
    private static readonly string Directory = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations");

    /// <summary>The name of every shipped <c>.dat</c> file, without the extension, in ordinal order.</summary>
    public static IReadOnlyList<string> Names { get; } = System.IO.Directory
        .GetFiles(Directory, "*.dat")
        .Select(path => Path.GetFileNameWithoutExtension(path))
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>The formulation file <paramref name="name"/>, read with <see cref="DatFile.Read(string)"/>.</summary>
    public static Formulation Read(string name) => DatFile.Read(Path.Combine(Directory, $"{name}.dat"));
}
