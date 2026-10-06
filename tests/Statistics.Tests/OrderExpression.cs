using System.Globalization;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// A parsed arithmetic expression: a name (an index or member path kept as one token), a number, a binary
/// operation or an invocation. It is the common reading of two texts, the call sites of
/// <c>CyclePlaneOrder</c> in <c>src/Statistics/*.cs</c> and the <c>order</c> column of
/// <c>CyclePlane.listing.generated.txt</c>, which <see cref="CyclePlaneOrderSiteTests"/> compares.
/// </summary>
internal abstract record Expression;

/// <summary>A name, with its indexers when it has any (<c>qks1[k]</c>).</summary>
internal sealed record NameExpression(string Text) : Expression;

/// <summary>A numeric literal, as written.</summary>
internal sealed record NumberExpression(string Text) : Expression;

/// <summary>One of <c>+ - * /</c>, left associative as C# and the table's parentheses read it.</summary>
internal sealed record BinaryExpression(char Operator, Expression Left, Expression Right) : Expression;

/// <summary>A call: <c>order.Linear(x, a, b)</c>, <c>real(i)</c>, <c>CellCentre(i)</c>.</summary>
internal sealed record InvocationExpression(string Name, IReadOnlyList<Expression> Arguments) : Expression;

/// <summary>
/// A recursive-descent reader of the one expression language both texts are written in: names, numbers,
/// <c>+ - * /</c>, parentheses, calls, indexers (kept inside their name) and the <c>(double)</c> cast
/// (dropped). Nothing else is accepted: a text the grammar does not cover throws, so a call site written
/// in a new way is a failure to read, never a pass.
/// </summary>
internal sealed class ExpressionParser
{
    private readonly List<string> tokens;
    private int position;

    private ExpressionParser(List<string> tokens)
    {
        this.tokens = tokens;
    }

    /// <summary>The expression <paramref name="text"/> says; throws <see cref="FormatException"/> on any other text.</summary>
    public static Expression Parse(string text)
    {
        var parser = new ExpressionParser(Tokenize(text));
        var expression = parser.ReadSum();
        if (parser.position != parser.tokens.Count)
        {
            throw new FormatException($"'{parser.tokens[parser.position]}' follows a complete expression in: {text}");
        }

        return expression;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (char.IsDigit(c) || c == '.' && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                var start = i;
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.'))
                {
                    i++;
                }

                if (i < text.Length && text[i] is 'e' or 'E')
                {
                    i++;
                    if (i < text.Length && text[i] is '+' or '-')
                    {
                        i++;
                    }

                    while (i < text.Length && char.IsDigit(text[i]))
                    {
                        i++;
                    }
                }

                tokens.Add(text[start..i]);
            }
            else if (char.IsLetter(c) || c is '_' or '@')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '@' or '.'))
                {
                    i++;
                }

                while (i < text.Length && text[i] == '[')
                {
                    i = AfterBrackets(text, i);
                }

                tokens.Add(text[start..i]);
            }
            else if ("+-*/(),".Contains(c, StringComparison.Ordinal))
            {
                tokens.Add(c.ToString());
                i++;
            }
            else
            {
                throw new FormatException($"'{c}' is not in the expression language: {text}");
            }
        }

        return tokens;
    }

    private static int AfterBrackets(string text, int open)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            depth += text[i] == '[' ? 1 : text[i] == ']' ? -1 : 0;
            if (depth == 0)
            {
                return i + 1;
            }
        }

        throw new FormatException($"an indexer is not closed: {text}");
    }

    private string? Peek(int ahead = 0) => position + ahead < tokens.Count ? tokens[position + ahead] : null;

    private string Take() => position < tokens.Count ? tokens[position++] : throw new FormatException("the expression ends early.");

    private void Expect(string token)
    {
        if (Take() != token)
        {
            throw new FormatException($"'{token}' expected before '{tokens[position - 1]}'.");
        }
    }

    private Expression ReadSum()
    {
        var left = ReadProduct();
        while (Peek() is "+" or "-")
        {
            var op = Take()[0];
            left = new BinaryExpression(op, left, ReadProduct());
        }

        return left;
    }

    private Expression ReadProduct()
    {
        var left = ReadPrimary();
        while (Peek() is "*" or "/")
        {
            var op = Take()[0];
            left = new BinaryExpression(op, left, ReadPrimary());
        }

        return left;
    }

    private Expression ReadPrimary()
    {
        var token = Take();
        if (token == "(")
        {
            if (Peek() == "double" && Peek(1) == ")")
            {
                position += 2;
                return ReadPrimary();
            }

            var inner = ReadSum();
            Expect(")");
            return inner;
        }

        if (token == "-")
        {
            return new BinaryExpression('-', new NumberExpression("0"), ReadPrimary());
        }

        if (char.IsDigit(token[0]) || token[0] == '.')
        {
            _ = double.Parse(token, CultureInfo.InvariantCulture);
            return new NumberExpression(token);
        }

        if (Peek() != "(")
        {
            return new NameExpression(token);
        }

        _ = Take();
        var arguments = new List<Expression>();
        if (Peek() != ")")
        {
            do
            {
                arguments.Add(ReadSum());
            }
            while (Peek() == "," && Take() == ",");
        }

        Expect(")");
        return new InvocationExpression(token, arguments);
    }
}
