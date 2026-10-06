using System.Reflection;

namespace PropStruct.Protocol.Tests;

/// <summary>The assembly each node's own project builds, loaded from this project's build output (every
/// project of the tree is referenced by <c>PropStruct.Protocol.Tests.csproj</c> for exactly this), and a
/// type's own node by the namespace attribution AGENTS.md §1 defines: the deepest node whose namespace equals,
/// or prefixes at a dot boundary, the type's own namespace.</summary>
internal static class NodeAssemblies
{
    /// <summary>Declared AGENTS.md §12 deviation from the root's "namespaces mirror the directory path"
    /// constraint (tests/Protocol.Tests/BOOT.md, "Declared deviation: tests/Harness namespace", 2026-09-20):
    /// <c>tests/Harness</c> and <c>tests/Harness.Tests</c> keep a literal <c>Tests.</c> segment
    /// (<c>PropStruct.Tests.Harness[.Tests]</c>) instead of dropping it like every other node
    /// (<c>PropStruct.Harness[.Tests]</c>). Named here, by node path, so that <em>only</em> a type physically
    /// compiled into one of these two nodes' own assemblies, under exactly this namespace, is exempted from the
    /// namespace-mirroring rule everywhere that rule is enforced (<see cref="NodeOf(Type)"/>,
    /// <see cref="CoverageTests"/>); every other node's types are still held to it without exception, and a type
    /// under any namespace this map does not name is never exempted. <see cref="CoverageTests"/> asserts this
    /// map is exactly the set of nodes still needing it, so a stale entry (the rename has landed) turns the
    /// check red rather than staying silently unused. Scheduled to be lifted when that rename lands.</summary>
    public static readonly IReadOnlyDictionary<string, string> DeclaredNamespaceExceptions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["tests/Harness"] = "PropStruct.Tests.Harness",
        ["tests/Harness.Tests"] = "PropStruct.Tests.Harness.Tests",
    };

    private static readonly Lazy<IReadOnlyDictionary<Node, Assembly>> AssembliesLazy = new(Load);

    private static readonly Lazy<IReadOnlyList<Node>> CodeNodesLazy = new(
        () => Assemblies.Keys.OrderBy(node => node.RelativePath, StringComparer.Ordinal).ToList());

    /// <summary>The assemblies of the nodes that have a project, loaded by the name the project gives them.</summary>
    public static IReadOnlyDictionary<Node, Assembly> Assemblies => AssembliesLazy.Value;

    /// <summary>Every node that owns a project, in path order: what the Coverage, Declaration and Dependency
    /// levels iterate instead of the whole tree, which also holds documentation-only nodes such as
    /// <c>tests/Fixtures</c> and nodes born ahead of their code such as <c>tools/defect-report</c>.</summary>
    public static IReadOnlyList<Node> CodeNodes => CodeNodesLazy.Value;

    /// <summary>The assembly a node's own project builds, or null for a node with no project of its own.</summary>
    public static Assembly? AssemblyOf(Node node) => Assemblies.GetValueOrDefault(node);

    /// <summary>The node whose project built the assembly, or null for an assembly from outside the tree.</summary>
    public static Node? NodeOf(Assembly assembly) => Assemblies.FirstOrDefault(pair => pair.Value == assembly).Key;

    /// <summary>The node a type belongs to: the deepest node of the tree whose namespace equals, or prefixes at
    /// a dot boundary, the type's own namespace (AGENTS.md §1) — unless the type is physically compiled into
    /// one of <see cref="DeclaredNamespaceExceptions"/>'s nodes under exactly the namespace declared there, in
    /// which case it is attributed to that node directly, before the namespace walk ever runs: the walk itself
    /// would otherwise attribute such a type to the tree root (its actual namespace still starts with
    /// "PropStruct.", the root's own namespace, so <see cref="NodeOfNamespace"/> finds a match there before
    /// falling through to the assembly). Falls back to the project node of the assembly the type physically
    /// sits in for a type with no namespace of its own (a top-level, unnamed compiler helper such as
    /// <c>&lt;PrivateImplementationDetails&gt;</c>). Null only when neither resolves: a type truly from outside
    /// the tree.</summary>
    public static Node? NodeOf(Type type)
    {
        var assemblyNode = NodeOf(type.Assembly);
        if (assemblyNode is not null
            && DeclaredNamespaceExceptions.TryGetValue(assemblyNode.RelativePath, out var exemptNamespace)
            && type.Namespace == exemptNamespace)
        {
            return assemblyNode;
        }

        return NodeOfNamespace(type.Namespace) ?? assemblyNode;
    }

    /// <summary>The deepest node whose <see cref="Node.Namespace"/> equals, or prefixes at a dot boundary, the
    /// given namespace; null when no node matches at all. The one namespace-attribution walk
    /// <see cref="NodeOf(Type)"/> uses.</summary>
    public static Node? NodeOfNamespace(string? ns)
    {
        if (ns is null)
        {
            return null;
        }

        Node? best = null;
        foreach (var node in Tree.Nodes)
        {
            if ((ns == node.Namespace || ns.StartsWith(node.Namespace + ".", StringComparison.Ordinal))
                && (best is null || node.Namespace.Length > best.Namespace.Length))
            {
                best = node;
            }
        }

        return best;
    }

    /// <summary>Every type reflection reports for a node's own assembly whose own namespace resolves, by
    /// <see cref="NodeOf(Type)"/>, to this node and no deeper one: the set the tree's checks read as "the
    /// node's own types". Empty for a node with no assembly at all.</summary>
    public static IEnumerable<Type> TypesOf(Node node)
    {
        var assembly = AssemblyOf(node);
        return assembly is null ? [] : assembly.GetTypes().Where(type => NodeOf(type) == node);
    }

    /// <summary>A test assembly references xunit; its public types are its tests, listed by its <c>BOOT.md</c>, not by <c>API.md</c>.</summary>
    public static bool IsTestAssembly(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Any(reference => reference.Name is { } name && name.StartsWith("xunit", StringComparison.Ordinal));

    private static Dictionary<Node, Assembly> Load()
    {
        var assemblies = new Dictionary<Node, Assembly>();
        foreach (var node in Tree.Nodes.Where(node => node.AssemblyName is not null))
        {
            try
            {
                assemblies[node] = Assembly.Load(new AssemblyName(node.AssemblyName!));
            }
            catch (FileNotFoundException e)
            {
                throw new InvalidOperationException(
                    $"the assembly of {node.Name} ({node.AssemblyName}) is not in the build output of Protocol.Tests: " +
                    "add a project reference to it in this node's project file", e);
            }
        }

        return assemblies;
    }
}
