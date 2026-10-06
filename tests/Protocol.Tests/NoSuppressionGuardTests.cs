using System.Text.RegularExpressions;
using Xunit;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// Root BOOT.md, "Language and build" (decided 2026-09-24, owner): every analyzer rule is on
/// (<c>AnalysisMode=All</c>), code style is enforced in the build (<c>EnforceCodeStyleInBuild</c>), warnings are
/// errors, and none of that may be silenced anywhere — "no <c>NoWarn</c> beyond the SDK's own defaults, no
/// severity lowered in an <c>.editorconfig</c>, no <c>#pragma warning disable</c>, no <c>SuppressMessage</c>". A
/// rule that conflicts with a design decision is satisfied by changing the code or the decision, never by
/// silencing the rule.
///
/// This is a tree-wide guard, not a per-node one: a suppression anywhere in the tree defeats the decision, so
/// this test walks every file under the root exactly as <see cref="Tree"/> does for nodes (the same
/// <see cref="Tree.Skipped"/> directories), not only the nodes it knows about.
/// </summary>
public sealed class NoSuppressionGuardTests
{
    /// <summary><c>Directory.Build.props</c> is what the root BOOT.md decision is expressed as; losing any of
    /// these three properties silently reopens every warning the decision closed (AnalysisMode's own default is
    /// a narrower "recommended" set, EnforceCodeStyleInBuild's own default is <see langword="false"/>, and
    /// without TreatWarningsAsErrors every other property here is advisory only).</summary>
    [Fact]
    public void DirectoryBuildPropsKeepsTheAnalyzerDecision()
    {
        var path = Path.Combine(Tree.Root, "Directory.Build.props");
        Assert.True(File.Exists(path), $"{Tree.Relative(path)} is missing; root BOOT.md's own analyzer decision lives here.");
        var text = File.ReadAllText(path);

        Assert.True(
            Regex.IsMatch(text, @"<AnalysisMode>\s*All\s*</AnalysisMode>"),
            $"{Tree.Relative(path)} no longer sets <AnalysisMode>All</AnalysisMode> (root BOOT.md, \"Language and build\").");
        Assert.True(
            Regex.IsMatch(text, @"<EnforceCodeStyleInBuild>\s*true\s*</EnforceCodeStyleInBuild>", RegexOptions.IgnoreCase),
            $"{Tree.Relative(path)} no longer sets <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> (root BOOT.md, \"Language and build\").");
        Assert.True(
            Regex.IsMatch(text, @"<TreatWarningsAsErrors>\s*true\s*</TreatWarningsAsErrors>", RegexOptions.IgnoreCase),
            $"{Tree.Relative(path)} no longer sets <TreatWarningsAsErrors>true</TreatWarningsAsErrors> (root BOOT.md, \"Language and build\").");
    }

    /// <summary>The SDK's own default <c>NoWarn</c> (<c>CS1701;CS1702</c>, binding redirects) is not a
    /// suppression this taboo names; anything beyond it, in any project file, is.</summary>
    [Fact]
    public void NoProjectAddsANoWarnBeyondTheSdksOwnDefaults()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1701", "1702", "CS1701", "CS1702" };
        var problems = new List<string>();

        foreach (var path in EnumerateFiles("*.csproj", "*.props", "*.targets"))
        {
            var text = File.ReadAllText(path);
            foreach (Match match in Regex.Matches(text, @"<NoWarn>([^<]*)</NoWarn>"))
            {
                var codes = match.Groups[1].Value.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries);
                var disallowed = codes.Where(c => !allowed.Contains(c.Trim())).ToList();
                if (disallowed.Count > 0)
                {
                    problems.Add($"{Tree.Relative(path)}: <NoWarn>{match.Groups[1].Value}</NoWarn> suppresses {string.Join(", ", disallowed)}, beyond the SDK's own 1701/1702");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void NoSourceFileDisablesAWarningWithPragma()
    {
        var problems = EnumerateFiles("*.cs")
            .Where(path => File.ReadAllLines(path).Any(line => line.TrimStart().StartsWith("#pragma warning disable", StringComparison.Ordinal)))
            .Select(Tree.Relative)
            .ToList();

        Assert.True(problems.Count == 0, "these files contain '#pragma warning disable':\n" + string.Join("\n", problems));
    }

    /// <summary>Matches the attribute application syntax — an opening bracket, an optional namespace
    /// qualifier, the attribute's own name, then an opening parenthesis — not a bare mention of the word: this
    /// guard's own file necessarily names the attribute in prose and in this test's own name, which is not a
    /// suppression.</summary>
    [Fact]
    public void NoSourceFileCarriesASuppressMessageAttribute()
    {
        const string attributeName = "SuppressMessage";
        var pattern = @"\[\s*(System\.Diagnostics\.CodeAnalysis\.)?" + attributeName + @"\s*\(";
        var problems = EnumerateFiles("*.cs")
            .Where(path => Regex.IsMatch(File.ReadAllText(path), pattern))
            .Select(Tree.Relative)
            .ToList();

        Assert.True(problems.Count == 0, $"these files apply a {attributeName} attribute:\n" + string.Join("\n", problems));
    }

    /// <summary>Root BOOT.md, "Language and build" (decided 2026-09-25, owner): the root <c>.editorconfig</c>
    /// raises every IDE code-style rule to warning — <c>dotnet_analyzer_diagnostic.category-Style.severity =
    /// warning</c>, plus a <c>:warning</c> suffix on every measured preference option. "Raising a severity is
    /// not a suppression; lowering one stays forbidden" — so this guard no longer bans a per-rule severity
    /// outright (that would make the decision's own file fail its own guard); it bans only a severity <em>below</em>
    /// <c>warning</c>, in either of the two forms an <c>.editorconfig</c> can carry one:
    /// <c>dotnet_diagnostic.&lt;RULE&gt;.severity = X</c> / <c>dotnet_analyzer_diagnostic.category-&lt;C&gt;.severity
    /// = X</c>, and the <c>option = value:X</c> suffix an IDE preference line carries. <see cref="FindLoweredSeverities"/>
    /// holds the shared rule both this test and <see cref="TheLoweringCheckCatchesADeliberatelyLoweredSeverity"/>
    /// exercise, so the second proves the first is not vacuously green (AGENTS.md §13).</summary>
    [Fact]
    public void NoEditorConfigLowersAnAnalyzerSeverityBelowWarning()
    {
        var problems = new List<string>();
        foreach (var path in EnumerateFiles(".editorconfig"))
        {
            foreach (var lowered in FindLoweredSeverities(File.ReadAllText(path)))
            {
                problems.Add($"{Tree.Relative(path)}: {lowered}");
            }
        }

        Assert.True(problems.Count == 0, "these .editorconfig lines set a severity below warning:\n" + string.Join("\n", problems));
    }

    /// <summary>The root <c>.editorconfig</c> is where root BOOT.md's decision lives in the build
    /// (<c>dotnet_analyzer_diagnostic.category-Style.severity = warning</c>): losing this one line would
    /// silently reopen every IDE code-style rule the decision raised, the same failure mode
    /// <see cref="DirectoryBuildPropsKeepsTheAnalyzerDecision"/> guards for <c>Directory.Build.props</c>.</summary>
    [Fact]
    public void RootEditorConfigKeepsTheCategoryStyleWarningLine()
    {
        var path = Path.Combine(Tree.Root, ".editorconfig");
        Assert.True(File.Exists(path), $"{Tree.Relative(path)} is missing; root BOOT.md's own IDE-style decision lives here.");
        var text = File.ReadAllText(path);

        Assert.True(
            Regex.IsMatch(text, @"dotnet_analyzer_diagnostic\.category-Style\.severity\s*=\s*warning\b", RegexOptions.IgnoreCase),
            $"{Tree.Relative(path)} no longer sets dotnet_analyzer_diagnostic.category-Style.severity = warning " +
            "(root BOOT.md, \"Language and build\").");
    }

    /// <summary>Proves <see cref="NoEditorConfigLowersAnAnalyzerSeverityBelowWarning"/> is not a check that has
    /// never been seen red (AGENTS.md §13): the same <see cref="FindLoweredSeverities"/> the tree scan calls is
    /// fed a line of each shape this guard must catch, none of them present in the real tree.</summary>
    [Fact]
    public void TheLoweringCheckCatchesADeliberatelyLoweredSeverity()
    {
        const string loweredDiagnostic = "dotnet_diagnostic.IDE0008.severity = silent";
        const string loweredCategory = "dotnet_analyzer_diagnostic.category-Style.severity = suggestion";
        const string loweredOption = "csharp_style_var_elsewhere = true:none";
        const string raisedOption = "csharp_style_var_elsewhere = true:warning";
        const string raisedToError = "dotnet_diagnostic.IDE0008.severity = error";
        const string noSeveritySuffix = "dotnet_sort_system_directives_first = true";

        _ = Assert.Single(FindLoweredSeverities(loweredDiagnostic));
        _ = Assert.Single(FindLoweredSeverities(loweredCategory));
        _ = Assert.Single(FindLoweredSeverities(loweredOption));
        Assert.Empty(FindLoweredSeverities(raisedOption));
        Assert.Empty(FindLoweredSeverities(raisedToError));
        Assert.Empty(FindLoweredSeverities(noSeveritySuffix));
    }

    /// <summary>
    /// Proves the one documented exception <see cref="IsDocumentedDefaultScoping"/> grants — <c>IDE0005</c>
    /// held at <c>default</c> outside a path-scoped section, root BOOT.md's own <c>[src/**.cs]</c> shape — is
    /// narrow rather than a hole a suppression could hide in (AGENTS.md §13, "must be proven non-degenerate
    /// once"): the real shape passes; the same line with no narrower section raising the rule back still
    /// fails, and the same shape for a different rule ID (not the one root BOOT.md names) still fails too.
    /// </summary>
    [Fact]
    public void TheDefaultScopingExceptionIsNarrow()
    {
        const string documentedShape = """
            [*.cs]
            dotnet_diagnostic.IDE0005.severity = default

            [src/**.cs]
            dotnet_diagnostic.IDE0005.severity = warning
            """;
        const string withoutTheNarrowerRaise = """
            [*.cs]
            dotnet_diagnostic.IDE0005.severity = default
            """;
        const string forADifferentRule = """
            [*.cs]
            dotnet_diagnostic.IDE0008.severity = default

            [src/**.cs]
            dotnet_diagnostic.IDE0008.severity = warning
            """;
        const string narrowerSectionLowersInstead = """
            [*.cs]
            dotnet_diagnostic.IDE0005.severity = warning

            [src/**.cs]
            dotnet_diagnostic.IDE0005.severity = default
            """;

        Assert.Empty(FindLoweredSeverities(documentedShape));
        _ = Assert.Single(FindLoweredSeverities(withoutTheNarrowerRaise));
        _ = Assert.Single(FindLoweredSeverities(forADifferentRule));
        _ = Assert.Single(FindLoweredSeverities(narrowerSectionLowersInstead));
    }

    /// <summary>Every severity an <c>.editorconfig</c> line can assign, below <c>warning</c>
    /// (case-insensitively): the four ordinary levels below it, plus <c>default</c>, which this guard treats as
    /// unverified rather than as an implicit warning (root BOOT.md's decision names <c>:warning</c> explicitly;
    /// "default" could resolve to any of the SDK's own per-rule defaults, most of them below it) — except the
    /// one documented, path-scoped exception <see cref="IsDocumentedDefaultScoping"/> recognises.</summary>
    private static readonly string[] SeveritiesBelowWarning = ["none", "silent", "suggestion", "refactoring", "default"];

    /// <summary>
    /// The one rule this tree holds at <c>default</c> outside <c>src</c> (root BOOT.md, "Language and build",
    /// decided 2026-09-25): "IDE0005 is raised in src, where the documentation file is generated; test
    /// projects generate no documentation file and leave IDE0005 at its default, a rule not raised rather
    /// than one lowered." A rule's own <c>default</c> severity is not a lowering by itself — it asks for the
    /// SDK's own out-of-the-box severity, not something this file pushed downward — so this guard accepts it
    /// for a rule, in a section whose glob is not itself path-scoped (i.e. applies file-tree-wide, such as
    /// <c>[*.cs]</c> or the file's top, before any <c>[glob]</c> header), only when a narrower, path-scoped
    /// section (a glob containing <c>/</c>, other than the bare <c>*.cs</c>) raises that same rule to at
    /// least <c>warning</c> elsewhere in the file. That is exactly the shape of the <c>[src/**.cs]</c>
    /// override above: every file's own IDE0005 severity is then either untouched (tests) or raised
    /// (<c>src</c>), never lowered relative to what it would otherwise have been.
    /// </summary>
    private const string DefaultScopedRuleId = "IDE0005";

    private static IEnumerable<string> FindLoweredSeverities(string editorConfigText)
    {
        var sections = SplitSections(editorConfigText);
        var raisedInNarrowerSection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in sections.Where(section => IsPathScoped(section.Glob)))
        {
            foreach (Match match in Regex.Matches(section.Body, @"dotnet_diagnostic\.([A-Za-z0-9_]+)\.severity\s*=\s*(\S+)"))
            {
                if (!SeveritiesBelowWarning.Contains(match.Groups[2].Value.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    _ = raisedInNarrowerSection.Add(match.Groups[1].Value.Trim());
                }
            }
        }

        foreach (var section in sections)
        {
            foreach (Match match in Regex.Matches(section.Body, @"dotnet_diagnostic\.([A-Za-z0-9_]+)\.severity\s*=\s*(\S+)"))
            {
                var ruleId = match.Groups[1].Value.Trim();
                var severity = match.Groups[2].Value.Trim();
                if (!SeveritiesBelowWarning.Contains(severity, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsDocumentedDefaultScoping(section.Glob, ruleId, severity, raisedInNarrowerSection))
                {
                    continue;
                }

                yield return match.Value.Trim();
            }
        }

        foreach (Match match in Regex.Matches(editorConfigText, @"dotnet_analyzer_diagnostic\.category-[A-Za-z0-9_]+\.severity\s*=\s*(\S+)"))
        {
            if (SeveritiesBelowWarning.Contains(match.Groups[1].Value.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                yield return match.Value.Trim();
            }
        }

        foreach (Match match in Regex.Matches(editorConfigText, @"^[a-z0-9_.]+\s*=\s*[^:=\r\n]+:(\S+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (SeveritiesBelowWarning.Contains(match.Groups[1].Value.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                yield return match.Value.Trim();
            }
        }
    }

    private static bool IsDocumentedDefaultScoping(
        string sectionGlob, string ruleId, string severity, HashSet<string> raisedInNarrowerSection) =>
        !IsPathScoped(sectionGlob)
        && string.Equals(ruleId, DefaultScopedRuleId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(severity, "default", StringComparison.OrdinalIgnoreCase)
        && raisedInNarrowerSection.Contains(ruleId);

    /// <summary>A glob that restricts its section to files under a specific path, such as <c>src/**.cs</c>,
    /// as opposed to one that applies to every file of a kind tree-wide (<c>*.cs</c>, or the section with no
    /// glob at all — the file's own top, before its first <c>[glob]</c> header).</summary>
    private static bool IsPathScoped(string glob) =>
        !string.IsNullOrEmpty(glob) && glob.Contains('/', StringComparison.Ordinal);

    private readonly record struct EditorConfigSection(string Glob, string Body);

    /// <summary>Splits an <c>.editorconfig</c>'s text on its <c>[glob]</c> headers. Text before the first
    /// header, if any, is one section with an empty glob (applies everywhere, like <c>root = true</c>).</summary>
    private static List<EditorConfigSection> SplitSections(string editorConfigText)
    {
        var headers = Regex.Matches(editorConfigText, @"^\s*\[(.+)\]\s*$", RegexOptions.Multiline);
        var sections = new List<EditorConfigSection>();
        if (headers.Count == 0)
        {
            sections.Add(new EditorConfigSection(Glob: string.Empty, Body: editorConfigText));
            return sections;
        }

        if (headers[0].Index > 0)
        {
            sections.Add(new EditorConfigSection(Glob: string.Empty, Body: editorConfigText[..headers[0].Index]));
        }

        for (var i = 0; i < headers.Count; i++)
        {
            var bodyStart = headers[i].Index + headers[i].Length;
            var bodyEnd = i + 1 < headers.Count ? headers[i + 1].Index : editorConfigText.Length;
            sections.Add(new EditorConfigSection(headers[i].Groups[1].Value.Trim(), editorConfigText[bodyStart..bodyEnd]));
        }

        return sections;
    }

    private static IEnumerable<string> EnumerateFiles(params string[] patterns)
    {
        foreach (var file in Walk(Tree.Root))
        {
            if (patterns.Any(pattern => MatchesPattern(Path.GetFileName(file), pattern)))
            {
                yield return file;
            }
        }
    }

    private static bool MatchesPattern(string fileName, string pattern) =>
        pattern.StartsWith('*')
            ? fileName.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase)
            : string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> Walk(string directory)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            yield return file;
        }

        foreach (var child in Directory.GetDirectories(directory))
        {
            if (!Tree.Skipped.Contains(Path.GetFileName(child)))
            {
                foreach (var file in Walk(child))
                {
                    yield return file;
                }
            }
        }
    }
}
