using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// Where the tree is and what its nodes are: the root found from this source file (AGENTS.md §13: from the
/// source, never from the binary), the nodes found by directory path (a directory holding both documents, the
/// protocol kit's own templates and the build directories skipped), and paths relative to the root.
/// </summary>
internal static class Tree
{
    /// <summary>Directories never read as nodes or as source: build output, the agent's session directory
    /// (<c>.claude</c> holds worktrees of other sessions, each a full copy of the tree), and the protocol kit's
    /// own document templates (the linter's own <c>--exclude templates</c>, root <c>CLAUDE.md</c>). The legacy
    /// Fortran archive, a directory of a zip that was no source-code directory (AGENTS.md §1), left the tree
    /// on 2026-10-04 (root <c>BOOT.md</c>, "## Delivery") and <c>tools/legacy</c> is a node.</summary>
    internal static readonly HashSet<string> Skipped = new(StringComparer.Ordinal)
    {
        ".git", ".vs", ".claude", "bin", "obj", "TestResults", "templates",
    };

    private static readonly Lazy<string> RootLazy = new(FindRoot);

    private static readonly Lazy<IReadOnlyList<Node>> NodesLazy = new(FindNodes);

    /// <summary>The directory holding AGENTS.md, found upward from this file (AGENTS.md §13: from the source,
    /// never from the binary).</summary>
    public static string Root => RootLazy.Value;

    /// <summary>Every node of the tree, ordered by path; the root first.</summary>
    public static IReadOnlyList<Node> Nodes => NodesLazy.Value;

    public static string Relative(string path)
    {
        var relative = Path.GetRelativePath(Root, path).Replace('\\', '/');
        return relative == "." ? string.Empty : relative;
    }

    private static string FindRoot()
    {
        var directory = Path.GetDirectoryName(ThisFile());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "AGENTS.md")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException("the tree root (a directory with AGENTS.md) was not found above " + ThisFile());
    }

    private static string ThisFile([CallerFilePath] string path = "") => path;

    /// <summary>The name the build gives the project's assembly: the project file's own name, unless the
    /// project overrides it with an explicit <c>&lt;AssemblyName&gt;</c> (root <c>BOOT.md</c>, "the tool
    /// command is <c>propstruct</c>": <c>src/Cli</c>'s project is <c>PropStruct.Cli.csproj</c> and its
    /// assembly is <c>propstruct</c>).</summary>
    private static string AssemblyNameOf(string projectFile)
    {
        var overridden = Regex.Match(File.ReadAllText(projectFile), @"<AssemblyName>([^<]+)</AssemblyName>");
        return overridden.Success ? overridden.Groups[1].Value.Trim() : Path.GetFileNameWithoutExtension(projectFile);
    }

    private static List<Node> FindNodes()
    {
        var nodes = new List<Node>();
        Walk(Root);
        return nodes.OrderBy(node => node.RelativePath, StringComparer.Ordinal).ToList();

        void Walk(string directory)
        {
            if (File.Exists(Path.Combine(directory, "BOOT.md")) && File.Exists(Path.Combine(directory, "API.md")))
            {
                var projects = Directory.GetFiles(directory, "*.csproj");
                if (projects.Length > 1)
                {
                    throw new InvalidOperationException($"{Relative(directory)} holds {projects.Length} projects; a node has one assembly (root BOOT.md)");
                }

                nodes.Add(new Node(Relative(directory), directory, projects.Length == 1 ? AssemblyNameOf(projects[0]) : null));
            }

            foreach (var child in Directory.GetDirectories(directory))
            {
                if (!Skipped.Contains(Path.GetFileName(child)))
                {
                    Walk(child);
                }
            }
        }
    }
}
