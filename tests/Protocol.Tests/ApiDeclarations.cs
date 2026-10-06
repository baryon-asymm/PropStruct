using System.Text.RegularExpressions;

namespace PropStruct.Protocol.Tests;

/// <summary>
/// The grammar of an <c>API.md</c>: its ✅ C# blocks and the declarations in them, and the text that sits under
/// ✅ more broadly (AGENTS.md §7). The one meaning of "named in the API.md" for <see cref="DeclarationTests"/>
/// (a specific declaration exists, read from the ✅ C# blocks) and <see cref="CoverageTests"/> (every exported
/// type is at least named, by prose or by code, under a ✅ mark): both ask this type rather than carrying two
/// separate ideas of what counts as documented.
/// </summary>
internal static class ApiDeclarations
{
    /// <summary>Whether a type of the given simple name is named anywhere in the document's ✅-marked text
    /// (prose or code alike; a document without a single status mark counts as ✅ throughout, AGENTS.md §7).</summary>
    public static bool NamesType(string document, string simpleName) =>
        Regex.IsMatch(string.Join("\n", Classify(document).Where(line => line.Implemented).Select(line => line.Text)), $@"\b{Regex.Escape(simpleName)}\b");

    /// <summary>The C# blocks of a document that sit under the nearest status mark above them being ✅ (or no mark at all).</summary>
    public static IEnumerable<string> ImplementedCsharpBlocks(string document)
    {
        var block = new List<string>();
        var blockImplemented = false;
        var wasInsideBlock = false;
        foreach (var (text, implemented, insideBlock) in Classify(document))
        {
            if (insideBlock)
            {
                block.Add(text);
                blockImplemented = implemented;
                wasInsideBlock = true;
                continue;
            }

            if (wasInsideBlock)
            {
                if (blockImplemented)
                {
                    yield return string.Join("\n", block);
                }

                block.Clear();
                wasInsideBlock = false;
            }
        }
    }

    /// <summary>
    /// Every line of the document, classified by the ✅/⏳ status-mark state machine AGENTS.md §7 describes:
    /// whether the line sits under an effective ✅, or no mark at all, rather than ⏳. A mark is read only on a
    /// line outside a ```csharp fence — the nearest mark above a code block decides the block's fate, not a
    /// character that happens to look like a mark inside the example code the block holds — so a line inside a
    /// fence carries the state as of the line that opened it, frozen for the whole block. Also tracks whether
    /// the line itself lies inside such a fence (the fence marker lines do not).
    /// </summary>
    private static IEnumerable<(string Text, bool Implemented, bool InsideCSharpBlock)> Classify(string document)
    {
        var implemented = true;
        var inside = false;
        foreach (var line in document.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inside)
                {
                    inside = false;
                }
                else if (line.Contains("csharp", StringComparison.Ordinal))
                {
                    inside = true;
                }

                yield return (line, implemented, false);
                continue;
            }

            if (!inside)
            {
                if (line.Contains('⏳', StringComparison.Ordinal))
                {
                    implemented = false;
                }
                else if (line.Contains('✅', StringComparison.Ordinal))
                {
                    implemented = true;
                }
            }

            yield return (line, implemented, inside);
        }
    }

    /// <summary>
    /// The names a C# block declares: types (class, struct, record, enum, interface, delegate), the positional
    /// parameters of records, properties, methods, fields (several per line), enum members. Lines inside an
    /// open parameter list are skipped. Tries each declaration kind in turn on every line; the first that
    /// matches wins.
    /// </summary>
    public static IEnumerable<Declaration> Declarations(string block)
    {
        var lines = block.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = StripComment(lines[index]);
            if (IsSkippable(line))
            {
                continue;
            }

            foreach (var declaration in OnLine(lines, ref index, line))
            {
                yield return declaration;
            }
        }
    }

    private static bool IsSkippable(string line) =>
        line.Length == 0 || line.TrimStart().StartsWith("using ", StringComparison.Ordinal)
            || line.TrimStart().StartsWith("namespace ", StringComparison.Ordinal) || line.TrimStart().StartsWith('[');

    /// <summary>The declarations of one line: a type first (which may open a parameter list spanning further
    /// lines), then a property, a method (which may open a parameter list too), a field list, or an enum member.</summary>
    private static List<Declaration> OnLine(string[] lines, ref int index, string line)
    {
        var type = TypeDeclaration(lines, ref index, line);
        if (type is not null)
        {
            return type;
        }

        var property = PropertyDeclaration(line);
        if (property is not null)
        {
            return [property.Value];
        }

        var method = MethodDeclaration(lines, ref index, line);
        if (method is not null)
        {
            return [method.Value];
        }

        var fields = FieldDeclarations(line);
        if (fields is not null)
        {
            return fields;
        }

        var enumMember = EnumMemberDeclaration(line);
        return enumMember is null ? [] : [enumMember.Value];
    }

    /// <summary>A type declaration (class, struct, record, enum, interface, delegate) and the positional
    /// parameters of a record, following the parameter list across lines when it opens one.</summary>
    private static List<Declaration>? TypeDeclaration(string[] lines, ref int index, string line)
    {
        var match = Regex.Match(line, @"\b(?:record\s+struct|record\s+class|record|class|struct|enum|interface|delegate\s+[\w<>\[\],.?]+)\s+(\w+)");
        if (!match.Success)
        {
            return null;
        }

        var declarations = new List<Declaration> { new(match.Groups[1].Value, IsType: true, IsEnumMember: false) };
        foreach (var parameter in ParameterList(lines, ref index, line))
        {
            declarations.Add(new Declaration(parameter, IsType: false, IsEnumMember: false) { FromTypeLine = true });
        }

        // A record declared in one line and closed by a semicolon has no body, so the members that follow it
        // belong to the type that encloses it, not to the record (2026-09-20: `internal readonly record struct
        // QuantumEstimate(...);` inside a static class made every following method of that class read as a
        // member of the record).
        declarations[0] = declarations[0] with { OpensBody = OpensBodyAt(lines, index) };
        return declarations;
    }

    /// <summary>Whether the type declared at <paramref name="index"/> owns what follows it: its body must be
    /// left open at the end of the line. A record closed by a semicolon and an enum whose whole body fits on
    /// one line own nothing beyond themselves, and the members after them belong to the enclosing type.</summary>
    private static bool OpensBodyAt(string[] lines, int index)
    {
        var line = StripComment(lines[index]).TrimEnd();
        if (line.EndsWith(';'))
        {
            return false;
        }

        var open = line.Count(c => c == '{') - line.Count(c => c == '}');
        if (open > 0)
        {
            return true;
        }

        for (var next = index + 1; next < lines.Length; next++)
        {
            var following = StripComment(lines[next]).Trim();
            if (following.Length == 0)
            {
                continue;
            }

            return following.StartsWith('{');
        }

        return false;
    }

    private static Declaration? PropertyDeclaration(string line)
    {
        var match = Regex.Match(line, @"\b(\w+)\s*\{\s*(?:get|set|init)");
        return match.Success ? new Declaration(match.Groups[1].Value, IsType: false, IsEnumMember: false) : null;
    }

    /// <summary>A method, constructor or operator declaration; skips over its parameter list when it spans further lines.</summary>
    private static Declaration? MethodDeclaration(string[] lines, ref int index, string line)
    {
        var match = Regex.Match(line, @"\b(\w+)\s*(?:<[\w,\s]+>)?\s*\(");
        if (!match.Success)
        {
            return null;
        }

        if (IsModifier(match.Groups[1].Value) || IsNewModifier(line, match))
        {
            // A modifier directly before a parenthesis opens the tuple a method returns, and the name is the
            // word after the tuple's balanced closing parenthesis (2026-10-02: the 2026-09-20 fix skipped
            // the line altogether, so such a declaration was never checked).
            return TupleReturningMethod(lines, ref index, line, match.Index + match.Length - 1);
        }

        if (IsKeyword(match.Groups[1].Value))
        {
            return null;
        }

        SkipOpenList(lines, ref index, line);
        return new Declaration(match.Groups[1].Value, IsType: false, IsEnumMember: false);
    }

    /// <summary>Whether the word of <paramref name="match"/> is the member modifier <c>new</c> (hiding an inherited
    /// member) rather than the expression keyword: only modifiers stand before it on the line, as in
    /// <c>public static new (int A, int B) Band(int count);</c>. <c>new</c> is deliberately not in
    /// <see cref="IsModifier"/>, since in <c>var origin = new (0, 0);</c> it opens an expression and no
    /// declaration (2026-10-02).</summary>
    private static bool IsNewModifier(string line, Match match) =>
        match.Groups[1].Value == "new"
            && line[..match.Index].Split(' ', StringSplitOptions.RemoveEmptyEntries).All(IsModifier);

    /// <summary>The method whose return type is the tuple opened at <paramref name="tupleOpen"/> of
    /// <paramref name="line"/>: the tuple, which may span lines, is followed by the name and its parameter
    /// list, which may span lines too. Null when no method name follows the tuple (a field of a tuple type).</summary>
    private static Declaration? TupleReturningMethod(string[] lines, ref int index, string line, int tupleOpen)
    {
        var text = Collect(lines, ref index, line);
        var depth = 0;
        for (var position = tupleOpen; position < text.Length; position++)
        {
            depth += text[position] == '(' ? 1 : text[position] == ')' ? -1 : 0;
            if (depth > 0)
            {
                continue;
            }

            var name = Regex.Match(text[(position + 1)..], @"^\s*(\w+)\s*(?:<[\w,\s]+>)?\s*\(");
            return name.Success && !IsKeyword(name.Groups[1].Value)
                ? new Declaration(name.Groups[1].Value, IsType: false, IsEnumMember: false)
                : null;
        }

        return null;
    }

    private static List<Declaration>? FieldDeclarations(string line)
    {
        var match = Regex.Match(
            line,
            @"^\s*(?:(?:public|internal|private|protected|static|readonly|const|required|new|volatile|unsafe)\s+)*[\w.]+(?:<[^;=]*>)?(?:\[[\s,]*\])*\??\s+(?<names>\w+(?:\s*=\s*[^,;]+)?(?:\s*,\s*\w+(?:\s*=\s*[^,;]+)?)*)\s*;\s*$");
        if (!match.Success)
        {
            return null;
        }

        var declarations = new List<Declaration>();
        foreach (var declarator in match.Groups["names"].Value.Split(','))
        {
            var name = Regex.Match(declarator, @"^\s*(\w+)");
            if (name.Success && !IsKeyword(name.Groups[1].Value))
            {
                declarations.Add(new Declaration(name.Groups[1].Value, IsType: false, IsEnumMember: false));
            }
        }

        return declarations;
    }

    private static Declaration? EnumMemberDeclaration(string line)
    {
        var match = Regex.Match(line, @"^\s*(\w+)\s*(?:=\s*[^,]+?)?\s*,?\s*$");
        return match.Success && !IsKeyword(match.Groups[1].Value) ? new Declaration(match.Groups[1].Value, IsType: false, IsEnumMember: true) : null;
    }

    /// <summary>The parameter names of a positional record, following the list across lines; empty for a type without one.</summary>
    private static List<string> ParameterList(string[] lines, ref int index, string first)
    {
        var names = new List<string>();
        if (!first.Contains('(', StringComparison.Ordinal))
        {
            return names;
        }

        var text = Collect(lines, ref index, first);
        var open = text.IndexOf('(', StringComparison.Ordinal);
        var close = text.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return names;
        }

        foreach (var parameter in text[(open + 1)..close].Split(','))
        {
            var withoutDefault = parameter.Split('=')[0].Trim();
            var name = Regex.Match(withoutDefault, @"(\w+)\s*$");
            if (name.Success && char.IsUpper(name.Groups[1].Value[0]))
            {
                names.Add(name.Groups[1].Value);
            }
        }

        return names;
    }

    private static void SkipOpenList(string[] lines, ref int index, string first) => Collect(lines, ref index, first);

    /// <summary>The line and its continuation lines until the parentheses balance.</summary>
    private static string Collect(string[] lines, ref int index, string first)
    {
        var text = first;
        var depth = Depth(first);
        while (depth > 0 && index + 1 < lines.Length)
        {
            index++;
            var next = StripComment(lines[index]);
            text += " " + next;
            depth += Depth(next);
        }

        return text;

        static int Depth(string line) => line.Count(c => c == '(') - line.Count(c => c == ')');
    }

    private static string StripComment(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var comment = line.IndexOf("//", StringComparison.Ordinal);
        return (comment >= 0 ? line[..comment] : line).TrimEnd();
    }

    private static bool IsKeyword(string word) =>
        IsModifier(word)
            || word is "if" or "for" or "foreach" or "while" or "switch" or "return" or "new" or "get" or "set" or "init" or "throw"
                or "using" or "nameof" or "typeof" or "default" or "sizeof" or "var" or "operator" or "where" or "else" or "do" or "in";

    /// <summary>A word that precedes a type or a member declaration and is never a member's name. A modifier
    /// immediately before a parenthesis starts a tuple return type
    /// (<c>internal static (long Low, long High) BinomialBand(...)</c>), not a member named after the modifier
    /// (2026-09-20, when two such declarations read as members called "static").</summary>
    private static bool IsModifier(string word) =>
        word is "public" or "internal" or "private" or "protected" or "static" or "readonly" or "sealed"
            or "override" or "virtual" or "abstract" or "async" or "extern" or "unsafe" or "partial"
            or "ref" or "out" or "params" or "delegate" or "record" or "class" or "struct" or "enum" or "interface";

    internal readonly record struct Declaration(string Name, bool IsType, bool IsEnumMember)
    {
        /// <summary>A type declaration that opens a body; the members after a bodiless one belong to its enclosing type.</summary>
        public bool OpensBody { get; init; } = true;

        /// <summary>A positional parameter of the record on the type's own line: it belongs to that record
        /// even when the record has no body.</summary>
        public bool FromTypeLine { get; init; }
    }
}
