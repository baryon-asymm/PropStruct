using System.Reflection;
using Xunit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// Coverage level (AGENTS.md §13, second table, row 2): every type a library assembly exports is named in the
/// <c>API.md</c> of its node (asking <see cref="ApiDeclarations"/>, the one meaning of "named"), and every type
/// of every assembly lives in the namespace of its node — the reflection reading of AGENTS.md §1's "the
/// namespace repeats the directory path", and of the root <c>BOOT.md</c> Constraints sentence "namespaces
/// mirror the directory path ... under the root namespace PropStruct". The snapshot cannot catch an added and
/// undescribed type: it is generated from the same code.
/// </summary>
public sealed class CoverageTests
{
    [Fact]
    public void EveryExportedTypeOfALibraryAssemblyIsNamedInItsNodesApi()
    {
        var problems = NodeAssemblies.CodeNodes
            .Where(node => !NodeAssemblies.IsTestAssembly(NodeAssemblies.AssemblyOf(node)!))
            .SelectMany(UndocumentedTypeProblems)
            .ToList();
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    /// <summary>Every type of every assembly resolves, by its own namespace, to exactly one node's own
    /// namespace, and that node's own project is the assembly the type was found in — except the nodes named in
    /// <see cref="NodeAssemblies.DeclaredNamespaceExceptions"/> (AGENTS.md §12 declared deviation,
    /// tests/Protocol.Tests/BOOT.md, "Declared deviation: tests/Harness namespace"), whose own types are let
    /// through under exactly the namespace declared there and no other. The exception list is asserted to be
    /// exactly the nodes that still need it: a node named there whose code no longer uses the exempted
    /// namespace (the rename has landed) fails this fact too, naming the stale entry, so the exception cannot
    /// silently outlive the divergence it excuses.</summary>
    [Fact]
    public void EveryTypeOfEveryAssemblyLivesInTheNamespaceOfItsNode()
    {
        var seenExceptions = new HashSet<string>(StringComparer.Ordinal);
        var problems = NodeAssemblies.Assemblies.Values.Distinct().OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
            .SelectMany(assembly => MisplacedTypeProblems(assembly, seenExceptions))
            .ToList();

        foreach (var stale in NodeAssemblies.DeclaredNamespaceExceptions.Keys.Except(seenExceptions).OrderBy(path => path, StringComparer.Ordinal))
        {
            problems.Add($"{stale} is listed in NodeAssemblies.DeclaredNamespaceExceptions, and no type of its own assembly still uses the " +
                         $"exempted namespace {NodeAssemblies.DeclaredNamespaceExceptions[stale]}: the rename has landed, remove the entry " +
                         "(and this fact's declared deviation in tests/Protocol.Tests/BOOT.md)");
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static IEnumerable<string> UndocumentedTypeProblems(Node node)
    {
        var api = File.ReadAllText(node.Api);
        var exported = NodeAssemblies.AssemblyOf(node)!.GetExportedTypes().ToHashSet();
        foreach (var type in NodeAssemblies.TypesOf(node).Where(exported.Contains).OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            var name = TypeShape.SimpleName(type);
            if (!ApiDeclarations.NamesType(api, name))
            {
                yield return $"{Tree.Relative(node.Api)} never names {name}, which {node.Namespace} exports (root BOOT.md, Taboos: no public type outside its node's API.md)";
            }
        }
    }

    private static IEnumerable<string> MisplacedTypeProblems(Assembly assembly, HashSet<string> seenExceptions)
    {
        foreach (var type in assembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            if (type.IsNested || type.Namespace is null || TypeShape.IsCompilerGenerated(type))
            {
                continue;
            }

            // NodeAssemblies.NodeOf already resolves a type physically compiled into an exception node's own
            // assembly, under exactly its declared exempt namespace, to that node directly (rather than the
            // tree root a plain namespace-prefix walk would find). Recognising the same case here, instead of
            // just accepting whatever NodeOf says, is what lets this fact both skip it and track that it was
            // still needed — the one thing a global attribution fix alone could not do by itself.
            var owner = NodeAssemblies.NodeOf(type);
            if (owner is not null
                && NodeAssemblies.DeclaredNamespaceExceptions.TryGetValue(owner.RelativePath, out var exemptNamespace)
                && type.Namespace == exemptNamespace)
            {
                _ = seenExceptions.Add(owner.RelativePath);
                continue;
            }

            if (owner is null || owner.Namespace != type.Namespace)
            {
                yield return $"{type.FullName} is in namespace {type.Namespace}, and no node of the tree is exactly that namespace (AGENTS.md §1)";
            }
            else if (NodeAssemblies.AssemblyOf(owner) != assembly)
            {
                yield return $"{type.FullName} is in namespace {type.Namespace}; its node {owner.Name} compiles into " +
                             $"{NodeAssemblies.AssemblyOf(owner)!.GetName().Name}, not {assembly.GetName().Name} (AGENTS.md §1)";
            }
        }
    }
}
