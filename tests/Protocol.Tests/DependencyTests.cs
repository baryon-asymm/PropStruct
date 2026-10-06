using Xunit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// Dependencies level (AGENTS.md §13, second table, row 4): the <c>## Dependencies</c> of every node with an
/// assembly equals the nodes whose types its code uses, in the shapes of its types and in the bodies of its
/// methods (a static call names its type in no signature). A parent using the types of its children declares
/// nothing; an ancestor whose own types a node uses is declared like a neighbour (AGENTS.md §6).
/// </summary>
public sealed class DependencyTests
{
    [Fact]
    public void EveryNodeDeclaresTheNeighboursItUsesAndNoOther()
    {
        var problems = NodeAssemblies.CodeNodes.SelectMany(ProblemsOf).ToList();
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static IEnumerable<string> ProblemsOf(Node node)
    {
        var (crossings, usedNodes) = Crossings(node);
        var (declared, unresolved) = NodeDocuments.DeclaredDependencies(node);
        var boot = Tree.Relative(node.Boot);
        foreach (var link in unresolved)
        {
            yield return $"{boot} links {link} under ## Dependencies, and no node has that API.md";
        }

        var declaredPaths = declared.Select(d => d.RelativePath).ToHashSet(StringComparer.Ordinal);
        foreach (var used in crossings.Keys.Where(used => !declaredPaths.Contains(used)))
        {
            yield return $"{boot} does not declare {usedNodes[used].Name}, but {node.Name} uses its types: {string.Join(", ", crossings[used].Take(6))}" +
                         (crossings[used].Count > 6 ? $" and {crossings[used].Count - 6} more" : string.Empty);
        }

        foreach (var unused in declared.Where(d => !crossings.ContainsKey(d.RelativePath)).OrderBy(d => d.RelativePath, StringComparer.Ordinal))
        {
            if (unused.IsDescendantOf(node))
            {
                yield return $"{boot} declares its descendant {unused.Name}; a parent owns its children and declares no dependency on them (AGENTS.md §6)";
                continue;
            }

            if (NodeAssemblies.AssemblyOf(unused) is null)
            {
                // A data-only node (tests/Fixtures, or a node born ahead of its code such as tools/defect-report)
                // has no type this walk could ever see used: its own dependency, if real, is a file the declaring
                // node reads by path, which no reflection check can confirm or refute. Flagging it as "unused"
                // here would be a permanent false positive against every node that legitimately reads its files,
                // not a finding about the document.
                continue;
            }

            yield return $"{boot} declares {unused.Name}, but no type of {node.Name} refers to it: the dependency went away and the document did not, or it was never real";
        }
    }

    /// <summary>Every neighbour or ancestor node a node's own types refer to, and every (type → referenced type)
    /// pair that shows it; <see cref="ProblemsOf"/> names the first six in its message.</summary>
    private static (SortedDictionary<string, SortedSet<string>> Crossings, Dictionary<string, Node> UsedNodes) Crossings(Node node)
    {
        var crossings = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var usedNodes = new Dictionary<string, Node>(StringComparer.Ordinal);
        foreach (var type in NodeAssemblies.TypesOf(node))
        {
            foreach (var referenced in TypeShape.ReferencedTypes(type))
            {
                var target = NodeAssemblies.NodeOf(referenced);
                if (target is null || target == node || target.IsDescendantOf(node))
                {
                    continue;
                }

                usedNodes[target.RelativePath] = target;
                if (!crossings.TryGetValue(target.RelativePath, out var users))
                {
                    crossings[target.RelativePath] = users = new SortedSet<string>(StringComparer.Ordinal);
                }

                _ = users.Add(TypeShape.SimpleName(TypeShape.Outermost(type)) + " → " + TypeShape.SimpleName(referenced));
            }
        }

        return (crossings, usedNodes);
    }
}
