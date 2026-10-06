namespace PropStruct.Protocol.Tests;

/// <summary>
/// A node of the tree: a directory holding both documents. <see cref="RelativePath"/> is the directory path
/// from the tree root with '/' separators, empty for the root; <see cref="AssemblyName"/> is the name of the
/// project in the directory, if any (a data-only node such as <c>tests/Fixtures</c> or a node born ahead of its
/// code such as <c>tools/defect-report</c> has none).
/// </summary>
internal sealed record Node(string RelativePath, string Directory, string? AssemblyName)
{
    /// <summary>The root namespace of the tree (root <c>BOOT.md</c>, Constraints): every node's
    /// <see cref="Namespace"/> starts from here, and it is the one place that name is written.</summary>
    private const string RootNamespace = "PropStruct";

    public string Name => RelativePath.Length == 0 ? "the root" : RelativePath;

    public string Boot => Path.Combine(Directory, "BOOT.md");

    public string Api => Path.Combine(Directory, "API.md");

    /// <summary>The C# namespace this node's own code lives in (AGENTS.md §1: the namespace repeats the
    /// directory path from the tree root; <c>src</c>, <c>tests</c> and <c>samples</c> are transparent, the same
    /// way they are transparent grouping directories and not nodes themselves, AGENTS.md §1). The one attribution every
    /// reflection check in this node reads (<see cref="NodeAssemblies.NodeOf(Type)"/>).</summary>
    public string Namespace => RelativePath.Length == 0
        ? RootNamespace
        : RootNamespace + "." + string.Join('.', RelativePath.Split('/').Where(segment => segment is not ("src" or "tests" or "samples")));

    public bool IsDescendantOf(Node other) =>
        other.RelativePath.Length == 0 ? RelativePath.Length > 0 : RelativePath.StartsWith(other.RelativePath + "/", StringComparison.Ordinal);
}
