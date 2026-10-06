using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PropStruct.Tests.Harness;

/// <summary>
/// One cell of a parsed quantity: its value, the print resolution of its source token (one unit of the last
/// printed digit: <c>E9.3</c> gives <c>1e-3 * 10^exponent</c>, a fixed-point token gives one unit of its last
/// decimal digit, a bare integer gives 1), and whether the source token was printed as a bare integer (a
/// candidate for a Poisson floor, root BOOT.md's statistical reference criterion).
/// </summary>
public readonly record struct ResultCell(double Value, double Resolution, bool IsIntegerPrinted);

/// <summary>
/// Parses a <c>results.m.txt</c> of <c>tests/Fixtures</c> into every quantity it holds, by name. This node's
/// BOOT.md, "the results.m parser yields every quantity of the file with its name and index": the file is
/// MATLAB-ish list-directed output, not a language this parser interprets as MATLAB — it recognizes exactly the
/// shapes the original program prints (a fixed statement grammar), not general MATLAB syntax.
///
/// The recognized shapes, uniformly for a code line and a comment line once its leading <c>%</c> is stripped
/// (API.md, "Name scheme"):
/// <list type="bullet">
/// <item><c>name = number;</c> — a scalar cell under <c>name</c>.</item>
/// <item><c>name = [n1 n2 ...];</c> — an array, continuation lines ending in <c>...</c> joined first.</item>
/// <item><c>name = n1 + n2;</c> (only <c>Nbase</c> in the observed files) — a 2-cell array under <c>name</c>,
/// the same shape as <c>name = [n1 n2];</c> would give.</item>
/// <item><c>fqdokkarm(  i,:) = [...]</c> — an array under <c>fqdokkarm(i,:)</c> (inner whitespace of an index
/// normalized away, so <c>fqdokkarm(  1,:)</c> and <c>fqdokkarm(1,:)</c> name the same cell).</item>
/// <item>several statements on one line, separated by <c>;</c> (<c>NFX = ...;  NFY = ...;</c>).</item>
/// <item>a comment-line assignment (<c>% Plot1 = 1950.0; Gm = 0.207;</c>) keeps its literal label text as the
/// name, spaces, parentheses, slashes and all (<c>P(karm-in-karm) coef</c>, <c>Nkarm/Nmkm(min, max)</c>,
/// <c>Statistical significance P(alpha)</c>, <c>Zok*</c>), because the label is not a bare identifier and
/// inventing one would be a second, competing name for the same file text.</item>
/// </list>
///
/// A comment line that is not one of the shapes above is tried against two more patterns before being ignored as
/// decorative, because their own label has no natural token to reuse: the conditions-breaking table row
/// (<c>%    K) description :  value</c>, under <c>ConditionBreaking(K)</c>) and the five fixed labels of
/// <see cref="LabelledEchoes"/>, matched by exact prefix (<c>% Cycles:  N</c>, under <c>CyclesReported</c>,
/// distinct from the header's own <c>Cycles = N;</c> echo). The time line
/// (<c>% Calculation time:  H : M : S</c>) is recognized by its <c>Calculation time</c> prefix and ignored,
/// never turned into a quantity.
/// </summary>
public static class ResultsMFile
{
    // A signed integer, fixed-point or scientific-notation token. The original also prints a fixed-point
    // mantissa with no leading zero before the point (e.g. "-.410517E-04"), so the integer part is optional as
    // long as the fractional part is present (`\d*\.\d+`), not just the usual `\d+\.?\d*`.
    private const string NumberPattern = @"[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?";

    private static readonly Regex NumberToken = new(NumberPattern, RegexOptions.Compiled);
    private static readonly Regex PlusPair = new($@"^(?<a>{NumberPattern})\s+\+\s+(?<b>{NumberPattern})$", RegexOptions.Compiled);
    private static readonly Regex SingleNumber = new($@"^(?<v>{NumberPattern})$", RegexOptions.Compiled);
    private static readonly Regex ConditionTableRow = new($@"^(?<index>\d+)\)\s.*:\s*(?<v>{NumberPattern})\s*$", RegexOptions.Compiled);

    /// <summary>
    /// The five comment labels that are not a bare identifier and so get an invented name instead of their
    /// literal text, in the order they must be tried (none is a prefix of an earlier one, so order does not
    /// matter for correctness, only for readability): <c>Cycles:</c> to <c>CyclesReported</c> (distinct from the
    /// header's own <c>Cycles = N;</c> echo, which keeps the name <c>Cycles</c>), <c>Medium coef
    /// [(Lij/Ddok)+1]:</c> to <c>MediumLijDdokCoefficient</c>, <c>Medium number of bridges:</c> to
    /// <c>MediumNumberOfBridges</c>, <c>Medium ratio between number of pockets and bridges :</c> to
    /// <c>MediumPocketBridgeRatio</c>, and <c>Medium fraction of paricles number in jammed pack:</c> to
    /// <c>MediumJammedParticleFraction</c>.
    /// </summary>
    private static readonly (string Label, string Name)[] LabelledEchoes =
    [
        ("Cycles:", "CyclesReported"),
        ("Medium coef [(Lij/Ddok)+1]:", "MediumLijDdokCoefficient"),
        ("Medium number of bridges:", "MediumNumberOfBridges"),
        ("Medium ratio between number of pockets and bridges :", "MediumPocketBridgeRatio"),
        ("Medium fraction of paricles number in jammed pack:", "MediumJammedParticleFraction"),
    ];

    /// <summary>Every quantity of the file, by name, as its bare values.</summary>
    public static IReadOnlyDictionary<string, double[]> Parse(string path)
    {
        var cells = ParseCells(path);
        var result = new Dictionary<string, double[]>(cells.Count, StringComparer.Ordinal);
        foreach (var (name, values) in cells)
        {
            result[name] = Array.ConvertAll(values, cell => cell.Value);
        }

        return result;
    }

    /// <summary>Every quantity of the file, by name, with each cell's print resolution and integer-ness.</summary>
    public static IReadOnlyDictionary<string, ResultCell[]> ParseCells(string path)
    {
        var quantities = new Dictionary<string, ResultCell[]>(StringComparer.Ordinal);
        foreach (var line in JoinContinuations(File.ReadAllLines(path)))
        {
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed[0] == '%')
            {
                ParseCommentLine(trimmed[1..].TrimStart(), quantities);
            }
            else
            {
                ParseCodeLine(StripTrailingComment(line), path, line, quantities);
            }
        }

        return quantities;
    }

    private static string StripTrailingComment(string line)
    {
        var hash = line.IndexOf('%');
        return hash < 0 ? line : line[..hash];
    }

    private static void ParseCodeLine(string codePart, string path, string sourceLine, Dictionary<string, ResultCell[]> quantities)
    {
        foreach (var statement in codePart.Split(';'))
        {
            var trimmed = statement.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var eq = trimmed.IndexOf('=');
            if (eq < 0)
            {
                // Not an assignment: a bare MATLAB command such as "clear fqdokkarm", "hold on" or a plot(...) call.
                continue;
            }

            var name = NormalizeName(trimmed[..eq].Trim());
            var valueText = trimmed[(eq + 1)..].Trim();
            if (!TryParseValue(valueText, out var cells))
            {
                throw new InvalidOperationException(
                    $"{path}: could not parse the value of '{name}' ('{valueText}') on line '{sourceLine.Trim()}'. " +
                    "This is a shape ResultsMFile does not recognize yet (this node's BOOT.md, ## Invariants).");
            }

            quantities[name] = cells;
        }
    }

    private static void ParseCommentLine(string content, Dictionary<string, ResultCell[]> quantities)
    {
        if (content.Length == 0 || content.StartsWith("Calculation time", StringComparison.Ordinal))
        {
            return;
        }

        var matchedAnyAssignment = false;
        foreach (var statement in content.Split(';'))
        {
            var trimmed = statement.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var eq = trimmed.IndexOf('=');
            if (eq < 0)
            {
                continue;
            }

            var name = NormalizeName(trimmed[..eq].Trim());
            var valueText = trimmed[(eq + 1)..].Trim();
            if (!TryParseValue(valueText, out var cells))
            {
                continue;
            }

            quantities[name] = cells;
            matchedAnyAssignment = true;
        }

        if (matchedAnyAssignment)
        {
            return;
        }

        var tableMatch = ConditionTableRow.Match(content);
        if (tableMatch.Success)
        {
            quantities[$"ConditionBreaking({tableMatch.Groups["index"].Value})"] = [MakeCell(tableMatch.Groups["v"].Value)];
            return;
        }

        foreach (var (label, name) in LabelledEchoes)
        {
            if (!content.StartsWith(label, StringComparison.Ordinal))
            {
                continue;
            }

            var afterLabel = content[label.Length..];
            var numberMatch = NumberToken.Match(afterLabel);
            if (numberMatch.Success)
            {
                quantities[name] = [MakeCell(numberMatch.Value)];
            }

            return;
        }

        // Anything else under a comment is decorative (a section banner, a units note, a table header) and
        // carries no quantity: this node's BOOT.md, "Name scheme" lists every recognized comment shape.
    }

    private static bool TryParseValue(string valueText, out ResultCell[] cells)
    {
        if (valueText.Length >= 2 && valueText[0] == '[' && valueText[^1] == ']')
        {
            var inner = valueText[1..^1];
            var matches = NumberToken.Matches(inner);
            if (matches.Count == 0 || NumberToken.Replace(inner, string.Empty).Trim().Length != 0)
            {
                cells = [];
                return false;
            }

            cells = new ResultCell[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                cells[i] = MakeCell(matches[i].Value);
            }

            return true;
        }

        var plus = PlusPair.Match(valueText);
        if (plus.Success)
        {
            cells = [MakeCell(plus.Groups["a"].Value), MakeCell(plus.Groups["b"].Value)];
            return true;
        }

        var single = SingleNumber.Match(valueText);
        if (single.Success)
        {
            cells = [MakeCell(single.Groups["v"].Value)];
            return true;
        }

        cells = [];
        return false;
    }

    private static ResultCell MakeCell(string token)
    {
        var value = double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
        return new ResultCell(value, ResolutionOf(token), IsIntegerPrinted(token));
    }

    private static double ResolutionOf(string token)
    {
        var e = token.IndexOfAny(['e', 'E']);
        var mantissa = e < 0 ? token : token[..e];
        var exponent = e < 0 ? 0 : int.Parse(token[(e + 1)..], CultureInfo.InvariantCulture);
        var dot = mantissa.IndexOf('.');
        var decimalDigits = dot < 0 ? 0 : mantissa.Length - dot - 1;
        return Math.Pow(10, exponent - decimalDigits);
    }

    private static bool IsIntegerPrinted(string token) => token.IndexOfAny(['.', 'e', 'E']) < 0;

    private static string NormalizeName(string rawName)
    {
        var open = rawName.IndexOf('(');
        if (open < 0)
        {
            return rawName;
        }

        var close = rawName.LastIndexOf(')');
        if (close < open)
        {
            return rawName;
        }

        var head = rawName[..open];
        var inside = rawName[(open + 1)..close];
        var tail = rawName[(close + 1)..];
        var normalizedInside = new string([.. inside.Where(c => !char.IsWhiteSpace(c))]);
        return $"{head}({normalizedInside}){tail}";
    }

    private static List<string> JoinContinuations(string[] rawLines)
    {
        var result = new List<string>(rawLines.Length);
        var buffer = new StringBuilder();
        var continuing = false;
        foreach (var raw in rawLines)
        {
            if (continuing)
            {
                _ = buffer.Append(raw);
            }
            else
            {
                _ = buffer.Clear();
                _ = buffer.Append(raw);
            }

            if (buffer.Length >= 3 && buffer[^1] == '.' && buffer[^2] == '.' && buffer[^3] == '.')
            {
                buffer.Length -= 3;
                continuing = true;
            }
            else
            {
                result.Add(buffer.ToString());
                continuing = false;
            }
        }

        if (continuing)
        {
            result.Add(buffer.ToString());
        }

        return result;
    }
}
