using System.Text.RegularExpressions;

namespace PropStruct.Protocol.Tests;

/// <summary>What a node's own <c>BOOT.md</c> declares in its <c>## Dependencies</c> section (AGENTS.md §6):
/// every link resolving to a node's <c>API.md</c>, and the links that resolve to none.</summary>
internal static class NodeDocuments
{
    public static (IReadOnlySet<Node> Nodes, IReadOnlyList<string> Unresolved) DeclaredDependencies(Node node)
    {
        var boot = File.ReadAllText(node.Boot).ReplaceLineEndings("\n");
        var section = Regex.Match(boot, @"^## Dependencies\s*$(.*?)(?=^## |\z)", RegexOptions.Multiline | RegexOptions.Singleline);
        var declared = new HashSet<Node>();
        var unresolved = new List<string>();
        if (!section.Success)
        {
            return (declared, unresolved);
        }

        var byDirectory = Tree.Nodes.ToDictionary(n => Path.GetFullPath(n.Directory), n => n, StringComparer.OrdinalIgnoreCase);
        foreach (Match link in Regex.Matches(section.Groups[1].Value, @"\]\(([^)\s]+)\)"))
        {
            var target = link.Groups[1].Value;
            if (!target.EndsWith("API.md", StringComparison.Ordinal))
            {
                continue;
            }

            var directory = Path.GetFullPath(Path.Combine(node.Directory, Path.GetDirectoryName(target) ?? string.Empty));
            if (byDirectory.TryGetValue(directory, out var found))
            {
                _ = declared.Add(found);
            }
            else
            {
                unresolved.Add(target);
            }
        }

        return (declared, unresolved);
    }
}
