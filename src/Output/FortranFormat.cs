using System.Globalization;

namespace PropStruct.Output;

/// <summary>
/// The original's print-time number formatting, transcribed from strings cut from the shipped reference
/// <c>results.m</c> files (BOOT.md, "## Constraints"; each primitive below names the file and line it was
/// cut from). Every primitive rounds the port's own <c>double</c> to the digit count the original's
/// declared Fortran type would show, without first converting to <c>float</c> (root BOOT.md, "Double
/// precision only"; this node's BOOT.md, "Design decisions (2026-09-19)").
/// </summary>
internal static class FortranFormat
{
    /// <summary>
    /// Rounds <paramref name="value"/> to <paramref name="significantDigits"/> significant decimal digits and
    /// splits the result into a sign, a digit string of exactly that length (first digit non-zero unless the
    /// value rounds to zero) and a base-10 exponent such that the value equals
    /// <c>±0.d1d2...dN * 10^(exponent+1)</c> (equivalently <c>±d1.d2...dN * 10^exponent</c>).
    /// </summary>
    private static (bool negative, string digits, int exponent) RoundToSignificantDigits(double value, int significantDigits)
    {
        // "E{n}" rounds to n+1 significant digits with a normalized single leading digit (round-to-nearest,
        // exactly the rounding the design decision asks for: the port's own double, not a binary32 copy of it).
        var formatted = value.ToString("E" + (significantDigits - 1), CultureInfo.InvariantCulture);
        var negative = formatted[0] == '-';
        if (negative)
        {
            formatted = formatted[1..];
        }

        var eIndex = formatted.IndexOf('E');
        var mantissa = formatted[..eIndex];
        var exponent = int.Parse(formatted[(eIndex + 1)..], CultureInfo.InvariantCulture);
        var digits = mantissa.Replace(".", string.Empty);
        return (negative, digits, exponent);
    }

    /// <summary>
    /// The original's list-directed <c>REAL</c> (single precision) output: fixed-point with 7 significant
    /// digits when <c>0.1 &lt;= |x| &lt; 1e7</c> (cut <c>tests/Fixtures/references/HPEPA3/results.m.txt:47</c>,
    /// <c>5.676710</c>), exponential with 8 significant digits (one more than the fixed-point branch: DVF's
    /// own default field widths, not a single shared digit budget) otherwise, including exact zero (line 46,
    /// <c>5.5000000E-05</c>; line 45, the zero case, <c>0.0000000E+00</c>) - Fortran source lines 1244-1256.
    /// </summary>
    public static string SingleRealCore(double value) => RealCore(value, fixedDigits: 7, exponentialDigits: 8, exponentWidth: 2, fixedExponentRange: 6);

    /// <summary>
    /// The original's list-directed <c>REAL*8</c> output (only <c>alfa</c>/<c>TailProbability</c> in this
    /// tree): the same fixed/exponential split as <see cref="SingleRealCore"/>, scaled up by the same "one
    /// more digit in the exponential branch" rule (15 fixed / 16 exponential significant digits) and a 3-digit
    /// exponent. Only the zero branch is cut from a fixture
    /// (<c>tests/Fixtures/references/HPEPA3/results.m.txt:15</c>, <c>0.000000000000000E+000</c>, every
    /// reference formulation's default <c>alfa = 0</c>); the fixed-point and non-zero-exponential branches
    /// follow the same algorithm, proven only for the single-precision case above.
    /// </summary>
    public static string DoubleRealCore(double value) => RealCore(value, fixedDigits: 15, exponentialDigits: 16, exponentWidth: 3, fixedExponentRange: 14);

    private static string RealCore(double value, int fixedDigits, int exponentialDigits, int exponentWidth, int fixedExponentRange)
    {
        var absValue = Math.Abs(value);
        var isFixed = absValue is >= 0.1 and < 1e7;

        if (isFixed)
        {
            var (negative, digits, exponent) = RoundToSignificantDigits(value, fixedDigits);
            var sign = negative ? "-" : string.Empty;
            if (exponent > fixedExponentRange)
            {
                // Rounding pushed the value across the F/E boundary (e.g. 9999999.6 -> 1.000000E7):
                // fall through to the exponential branch with the value as rounding decided it.
            }
            else
            {
                var digitsBefore = exponent + 1;
                if (digitsBefore <= 0)
                {
                    return sign + "0." + digits;
                }

                var whole = digits[..digitsBefore];
                var frac = digits[digitsBefore..];
                return sign + whole + "." + frac;
            }
        }

        {
            var (negative, digits, exponent) = RoundToSignificantDigits(value, exponentialDigits);
            var sign = negative ? "-" : string.Empty;
            return sign + digits[..1] + "." + digits[1..] + "E" + (exponent >= 0 ? "+" : "-") +
                   Math.Abs(exponent).ToString("D" + exponentWidth, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A single-precision list-directed real placed right after a character literal: right-justified in an
    /// 11-character field when fixed-point, 15 when exponential (BOOT.md, "## List-directed real placement");
    /// a fixed-point value additionally carries 4 trailing blanks, an exponential value none, matching the
    /// original's own inter-item spacing before whatever follows.
    /// </summary>
    public static string SingleRealField(double value)
    {
        var core = SingleRealCore(value);
        return IsExponential(core) ? core.PadLeft(15) : core.PadLeft(11) + "    ";
    }

    /// <summary>
    /// A single-precision list-directed real placed directly before another list-directed real with no
    /// separating literal (only <c>NN_min</c> and the first element of <c>Zkarm_cor</c> in this tree): the
    /// same leading placement as <see cref="SingleRealField"/> but with none of its 4 trailing blanks - the
    /// whole gap before the next value is that value's own leading pad (see
    /// <see cref="SingleRealAdjacent"/>), not this one's trailing pad on top of it. Cut from
    /// <c>tests/Fixtures/references/HPEPA3/results.m.txt:13</c> (<c>3.000000</c> then 7 blanks, all
    /// attributable to <c>100.0000</c>'s own leading pad, none to this value).
    /// </summary>
    public static string SingleRealBeforeAdjacent(double value)
    {
        var core = SingleRealCore(value);
        return IsExponential(core) ? core.PadLeft(15) : core.PadLeft(11);
    }

    /// <summary>
    /// A single-precision list-directed real placed directly after another list-directed real with no
    /// separating literal (only <c>NN_max</c> and the second element of <c>Zkarm_cor</c> in this tree):
    /// right-justified in a 15-character field regardless of branch (BOOT.md, "## List-directed real
    /// placement"), cut from <c>tests/Fixtures/references/HPEPA3/results.m.txt:13</c> (<c>100.0000</c>, 7
    /// leading blanks) and <c>:95</c> (<c>0.5687045</c>, 6 leading blanks); a fixed-point value carries the
    /// same 4 inherent trailing blanks as <see cref="SingleRealField"/>, proven by the same two cut lines'
    /// trailing spacing before the closing <c>];</c>.
    /// </summary>
    public static string SingleRealAdjacent(double value)
    {
        var core = SingleRealCore(value);
        return IsExponential(core) ? core.PadLeft(15) : core.PadLeft(15) + "    ";
    }

    /// <summary>
    /// A double-precision list-directed real placed right after a character literal: right-justified in a
    /// 24-character field (2 blanks + the 22-character zero representation), matching the single-precision
    /// placement rule at double width; only the zero case is fixture-proven (see <see cref="DoubleRealCore"/>).
    /// </summary>
    public static string DoubleRealField(double value) => DoubleRealCore(value).PadLeft(24);

    private static bool IsExponential(string core) => core.Contains('E');

    /// <summary>
    /// The original's <c>Ew.d</c> edit descriptor with no reserved sign column, mantissa normalized to
    /// <c>[0.1, 1)</c> (<c>"0.d1...dN"</c>), a 2-digit signed exponent: <c>arrayprint</c>'s <c>E9.3</c>
    /// (<paramref name="decimals"/> = 3, Fortran line 1521; cut e.g.
    /// <c>tests/Fixtures/references/HPEPA3/results.m.txt:8</c>, <c>0.100E-04</c>) and <c>arrayprint2</c>'s
    /// <c>E12.6</c> (<paramref name="decimals"/> = 6, Fortran line 1553; cut
    /// <c>tests/Fixtures/references/inpt/results.m.txt:567</c>, <c>0.151219E-01</c>). Exact zero prints as
    /// <c>0.</c> + <paramref name="decimals"/> zeros + <c>E+00</c> (cut
    /// <c>tests/Fixtures/references/HPEPA3/results.m.txt:256</c>, <c>0.000E+00</c>).
    /// </summary>
    public static string ScaledExponent(double value, int decimals)
    {
        if (value == 0)
        {
            return "0." + new string('0', decimals) + "E+00";
        }

        var (negative, digits, exponent) = RoundToSignificantDigits(value, decimals);
        var sign = negative ? "-" : string.Empty;
        var shifted = exponent + 1;
        return sign + "0." + digits + "E" + (shifted >= 0 ? "+" : "-") + Math.Abs(shifted).ToString("D2", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The original's <c>Fw.d</c> edit descriptor: fixed decimal places, right-justified in a field of
    /// <paramref name="width"/> characters (<c>913</c>/<c>946</c>/explicit formats, Fortran lines 1250-1330;
    /// cut e.g. <c>tests/Fixtures/references/HPEPA3/results.m.txt:57</c>, <c>Dok43a =  130.68</c>, F7.2).
    /// </summary>
    public static string FixedPoint(double value, int decimals, int width) =>
        value.ToString("F" + decimals, CultureInfo.InvariantCulture).PadLeft(width);

    /// <summary>
    /// The original's <c>Iw</c> edit descriptor, and the default list-directed <c>INTEGER</c> field widths
    /// (Fortran's own defaults for <c>INTEGER*4</c>/<c>*8</c>/<c>*2</c>, cut e.g.
    /// <c>tests/Fixtures/references/HPEPA3/results.m.txt:31</c>, <c>Nkarm =              24649481 ;</c>,
    /// width 22 for <c>INTEGER*8</c>; <c>:29</c>, <c>Nbase =      100000 ...</c>, width 12 for
    /// <c>INTEGER*4</c>; <c>:367</c>, <c>% Dkarm =     10 mkm</c>, width 7 for <c>INTEGER*2</c>).
    /// </summary>
    public static string IntegerField(long value, int width) => value.ToString(CultureInfo.InvariantCulture).PadLeft(width);

    /// <summary>Default list-directed field width of <c>INTEGER*4</c> (and plain <c>INTEGER</c>).</summary>
    public const int Integer4Width = 12;

    /// <summary>Default list-directed field width of <c>INTEGER*8</c>.</summary>
    public const int Integer8Width = 22;

    /// <summary>Default list-directed field width of <c>INTEGER*2</c>.</summary>
    public const int Integer2Width = 7;

    /// <summary>
    /// The array layout of <c>arrayprint</c>/<c>arrayprint2</c> (Fortran lines 1517-1571): <c>" {name} = ["</c>
    /// then the values via <see cref="ScaledExponent"/> with <paramref name="decimals"/> fraction digits, 6
    /// per physical line separated by one blank, a continuation line's own first value indented by
    /// <c>name.Length + 5</c> spaces (Fortran's <c>m = len_trim(name)+5</c>) instead of the usual separating
    /// blank, and a trailing <c>"];"</c> preceded by the closing <c>write(4,*)'];'</c>'s own leading blank on
    /// top of whatever the array loop's own last item already appended (proven against
    /// <c>tests/Fixtures/references/HPEPA3/results.m.txt:7</c>, <c>Gfr = [0.515E+00 0.485E+00  ];</c>, and the
    /// multi-line <c>:117-122</c>, <c>fmdok</c>). Returns the joined physical lines without a final line
    /// terminator; the caller (<see cref="ResultsMWriter"/>) adds it once.
    /// </summary>
    public static string FormatArray(string name, ReadOnlySpan<double> values, int decimals)
    {
        var indent = name.Length + 5;
        var lines = new List<string>();
        var line = new System.Text.StringBuilder();
        _ = line.Append(' ').Append(name).Append(" = [");

        var nf = 0;
        for (var i = 1; i <= values.Length; i++)
        {
            var text = ScaledExponent(values[i - 1], decimals);
            if (nf == 5)
            {
                if (i < values.Length)
                {
                    _ = line.Append(text).Append("...");
                    lines.Add(line.ToString());
                    _ = line.Clear();
                }
                else
                {
                    _ = line.Append(text);
                }

                nf = 0;
            }
            else
            {
                if (nf == 0 && i > 1)
                {
                    _ = line.Append(' ', indent).Append(text).Append(' ');
                }
                else
                {
                    _ = line.Append(text).Append(' ');
                }

                nf++;
            }
        }

        _ = line.Append(" ];");
        lines.Add(line.ToString());
        return string.Join("\r\n", lines);
    }

    /// <summary>
    /// Fortran's <c>INT()</c> truncation toward zero of a value already scaled by the caller (e.g.
    /// <c>Dmin*1.00001e6</c>, Fortran line 1204), performed on the port's own <c>double</c> (root BOOT.md,
    /// "Double precision only": this quotient is not among the exceptions requiring binary32 mantissa
    /// arithmetic, and the structural test of <c>tests/Output.Tests</c> checks that no reference formulation
    /// sits near enough an integer boundary for that to matter, the same evidence pattern as the exceptions
    /// root BOOT.md does list).
    /// </summary>
    public static long TruncateToInt64(double value) => (long)value;
}
