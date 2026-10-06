namespace PropStruct.Statistics.Tests;

/// <summary>
/// What the order of a product depends on, and nothing else: the association of its factors. A leaf is
/// <c>H</c> (the half-integer cell index <c>i - 0.5</c>), <c>D</c> (the cell size <c>di</c>), <c>I</c> (a bare
/// index) or <c>W</c> (any other value, a weight); an inner node is a product <c>*</c> or a sum or difference
/// (<c>+</c>, <c>-</c>). A quotient is a <c>W</c>: its operands are not factors of the product it feeds.
/// </summary>
internal sealed record Shape(string Kind, Shape? Left = null, Shape? Right = null)
{
    /// <summary>
    /// The shape as one string, the two operands of a product sorted: <c>a·b</c> and <c>b·a</c> are one
    /// shape (a commutation changes no bit), <c>(a·b)·c</c> and <c>a·(b·c)</c> are two.
    /// </summary>
    public string Canonical
    {
        get
        {
            var left = Left?.Canonical ?? string.Empty;
            var right = Right?.Canonical ?? string.Empty;
            return Kind switch
            {
                "*" => string.CompareOrdinal(left, right) <= 0 ? $"({left}*{right})" : $"({right}*{left})",
                "+" or "-" => $"({left}{Kind}{right})",
                _ => Kind,
            };
        }
    }

    /// <summary>The canonical string of this shape and of every shape inside it.</summary>
    public IEnumerable<string> Subtrees()
    {
        yield return Canonical;
        foreach (var inner in new[] { Left, Right }.OfType<Shape>().SelectMany(s => s.Subtrees()))
        {
            yield return inner;
        }
    }
}

/// <summary>One form of <c>CyclePlaneOrder</c> as its source states it: its parameters and its <c>Original</c> expression.</summary>
internal sealed record OrderForm(IReadOnlyList<string> Parameters, Expression Original)
{
    /// <summary>The forms of <c>CyclePlaneOrder.cs</c>, read from the lines <c>public double Name(double a, ...) =&gt; original ? ... : ...;</c>.</summary>
    public static Dictionary<string, OrderForm> Read(IEnumerable<string> lines)
    {
        const string Marker = "=> original ? ";
        var forms = new Dictionary<string, OrderForm>(StringComparer.Ordinal);
        foreach (var line in lines.Select(l => l.Trim()).Where(l => l.StartsWith("public double ", StringComparison.Ordinal) && l.Contains(Marker, StringComparison.Ordinal)))
        {
            var open = line.IndexOf('(', StringComparison.Ordinal);
            var close = line.IndexOf(')', StringComparison.Ordinal);
            var name = line["public double ".Length..open];
            var parameters = line[(open + 1)..close].Split(',').Select(p => p.Trim().Split(' ')[^1]).ToList();
            var expression = line[(line.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length)..];
            expression = expression[..expression.IndexOf(" : ", StringComparison.Ordinal)];
            forms[name] = new OrderForm(parameters, ExpressionParser.Parse(expression));
        }

        return forms;
    }
}

/// <summary>
/// Reads an <see cref="Expression"/> as a <see cref="Shape"/>. The names it knows are the plane's own:
/// <c>di</c> and <c>cellSize</c> are the cell size, <c>i</c> and <c>k</c> the cell index, <c>x ± 0.5</c> of an
/// index the half-integer, and <c>real(i)</c> of the table the index. A call of an <c>order.</c> form reads as the
/// form's <c>Original</c> expression over the shapes of its arguments; a local function of the source file
/// (<c>double CellCentre(int c) =&gt; ...;</c>) and a local variable declared above the call
/// (<c>var cell = ...;</c>) read as what they are bound to. Anything else is a weight.
/// </summary>
internal sealed class ShapeReader(IReadOnlyDictionary<string, OrderForm> forms, string[]? lines = null, int lineIndex = 0)
{
    private const int DepthLimit = 16;

    private static readonly Shape Weight = new("W");
    private static readonly Shape CellSize = new("D");
    private static readonly Shape Index = new("I");
    private static readonly Shape HalfIndex = new("H");

    private readonly Dictionary<string, (IReadOnlyList<string> Parameters, Expression Body)> functions = LocalFunctions(lines);

    /// <summary>The shape of <paramref name="expression"/>, its names looked up in <paramref name="scope"/> first.</summary>
    public Shape Read(Expression expression, IReadOnlyDictionary<string, Shape>? scope = null, int depth = 0)
    {
        if (depth > DepthLimit)
        {
            throw new InvalidOperationException("an expression nests beyond what the reader follows.");
        }

        return expression switch
        {
            NumberExpression => Weight,
            NameExpression name => ReadName(name.Text, scope, depth),
            BinaryExpression binary => ReadBinary(binary, scope, depth),
            InvocationExpression call => ReadInvocation(call, scope, depth),
            _ => throw new InvalidOperationException($"{expression} is not an expression."),
        };
    }

    private Shape ReadName(string text, IReadOnlyDictionary<string, Shape>? scope, int depth)
    {
        if (scope is not null && scope.TryGetValue(text, out var bound))
        {
            return bound;
        }

        if (text.Contains('[', StringComparison.Ordinal))
        {
            return Weight;
        }

        if (text is "di" or "cellSize")
        {
            return CellSize;
        }

        if (text is "i" or "k")
        {
            return Index;
        }

        return DeclaredAbove(text) is { } definition ? Read(definition, null, depth + 1) : Weight;
    }

    private Shape ReadBinary(BinaryExpression binary, IReadOnlyDictionary<string, Shape>? scope, int depth)
    {
        if (binary.Operator == '/')
        {
            return Weight;
        }

        var left = Read(binary.Left, scope, depth + 1);
        if (binary.Operator is '+' or '-' && binary.Right is NumberExpression { Text: "0.5" } && left.Kind == "I")
        {
            return HalfIndex;
        }

        return new Shape(binary.Operator.ToString(), left, Read(binary.Right, scope, depth + 1));
    }

    private Shape ReadInvocation(InvocationExpression call, IReadOnlyDictionary<string, Shape>? scope, int depth)
    {
        var arguments = call.Arguments.Select(a => Read(a, scope, depth + 1)).ToList();
        if (call.Name == "real" && arguments.Count == 1)
        {
            return arguments[0].Kind == "I" ? Index : Weight;
        }

        if (call.Name.StartsWith("order.", StringComparison.Ordinal) && forms.TryGetValue(call.Name["order.".Length..], out var form))
        {
            return Read(form.Original, Bind(form.Parameters, arguments), depth + 1);
        }

        return functions.TryGetValue(call.Name, out var function) ? Read(function.Body, Bind(function.Parameters, arguments), depth + 1) : Weight;
    }

    private static Dictionary<string, Shape> Bind(IReadOnlyList<string> parameters, List<Shape> arguments)
    {
        if (parameters.Count != arguments.Count)
        {
            throw new InvalidOperationException($"{arguments.Count} arguments for the parameters {string.Join(", ", parameters)}.");
        }

        return parameters.Zip(arguments).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
    }

    /// <summary>The nearest <c>var name = expression;</c> above the call that sits on one line, or <see langword="null"/>.</summary>
    private Expression? DeclaredAbove(string name)
    {
        if (lines is null)
        {
            return null;
        }

        var prefix = $"var {name} = ";
        for (var i = lineIndex - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line.EndsWith(';') ? ExpressionParser.Parse(line[prefix.Length..^1]) : null;
            }
        }

        return null;
    }

    private static Dictionary<string, (IReadOnlyList<string> Parameters, Expression Body)> LocalFunctions(string[]? lines)
    {
        const string Arrow = " => ";
        var functions = new Dictionary<string, (IReadOnlyList<string>, Expression)>(StringComparer.Ordinal);
        foreach (var line in (lines ?? []).Select(l => l.Trim()).Where(l => l.StartsWith("double ", StringComparison.Ordinal)))
        {
            var open = line.IndexOf('(', StringComparison.Ordinal);
            var arrow = line.IndexOf(Arrow, StringComparison.Ordinal);
            if (open < 0 || arrow < open)
            {
                continue;
            }

            var parameters = line[(open + 1)..line.IndexOf(')', StringComparison.Ordinal)].Split(',').Select(p => p.Trim().Split(' ')[^1]).ToList();
            functions[line["double ".Length..open]] = (parameters, ExpressionParser.Parse(line[(arrow + Arrow.Length)..].TrimEnd(';')));
        }

        return functions;
    }
}
