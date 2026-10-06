using System.Text.RegularExpressions;
using Xunit;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// F-c (`tests/Harness/PrintResolution.cs`, `ExponentOf`;
/// `tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`): the old
/// <c>ceil(log10(|value|) - 1e-9)</c> put an exact power of ten one decade low (the Fortran <c>E</c> format's own
/// mantissa convention puts <c>10^k</c> at <c>0.1 x 10^(k+1)</c>, exponent <c>k+1</c>, not <c>k</c>). This class is
/// the right control (every printed <c>E</c> token, over every fixture file, matches its own exponent) and the
/// cell-level proof that the fix moves exactly the one declared verdict, no more.
/// </summary>
public class PrintResolutionTests
{
    // Fortran's own `E` edit descriptor prints a normalized mantissa, `0.d1d2...` in `[0.1, 1)` — a literal
    // leading `0.`, never a scale factor's `1P` widened `[1, 10)` mantissa (`epsx`'s own field, printed
    // un-normalized: `2.3245811E-05`, outside `ExponentOf`'s own documented convention and this test's concern).
    private static readonly Regex EToken = new(@"[+-]?0\.\d+[Ee][+-]?\d+", RegexOptions.Compiled);

    // Every fixture `.m.txt` file the criterion itself reads: the five references, the three replica kinds'
    // 288 files, and (added since F-c was found) `rate-runs`' 320 stored port runs — the same 293 files F-c's
    // own census measured, plus `rate-runs`, never `tests/Fixtures/Legacy`'s archived raw outputs, which no
    // criterion reads.
    private static IEnumerable<string> AllCriterionFixtureFiles()
    {
        foreach (var directory in new[] { "references", "replicas-lagged", "replicas-independent", "replicas-gsv3", "rate-runs" })
        {
            var path = RepositoryPaths.Resolve("tests", "Fixtures", directory);
            foreach (var file in Directory.EnumerateFiles(path, "*.m.txt", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }
    }

    // The token's own printed exponent, read directly from its text (`E+02` -> `2`), independent of
    // `PrintResolution.ExponentOf`'s own log10-based recomputation — the oracle this test's right control needs
    // (root BOOT.md Taboos: "the candidate is never its own witness").
    private static int TokenOwnExponent(string token)
    {
        var e = token.IndexOfAny(['e', 'E']);
        return int.Parse(token[(e + 1)..], System.Globalization.CultureInfo.InvariantCulture);
    }

    // (a) Right: `ExponentOf` agrees with the token's own printed exponent for every non-zero `E` token of every
    // criterion fixture file (538,297+ tokens; F-c's own census found the old formula wrong on 1,982 of them, all
    // `0.100E+-xx`). Red (recorded, not re-asserted here): the pre-fix formula disagreed on 1,982.
    [Trait("Category", "Long")]
    [Fact]
    public void ExponentOfMatchesTheTokensOwnExponentForEveryNonZeroFixtureToken()
    {
        var checkedTokens = 0;
        var mismatches = new List<string>();
        foreach (var file in AllCriterionFixtureFiles())
        {
            foreach (var line in File.ReadLines(file))
            {
                // `%`-prefixed lines are comment/menu-parameter echoes (`ResultsMFile`'s own parser skips them
                // too), some printed in a plain, unnormalized `E` format (mantissa outside `[0.1, 1)`, e.g. `%
                // eps = 5.0000001E-02 ;`) — not the array-print routine's own convention `ExponentOf` mirrors, so
                // not this test's concern.
                if (line.TrimStart().StartsWith('%'))
                {
                    continue;
                }

                foreach (Match match in EToken.Matches(line))
                {
                    var token = match.Value;
                    var value = double.Parse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                    if (value == 0.0)
                    {
                        continue;
                    }

                    checkedTokens++;
                    var expected = TokenOwnExponent(token);
                    var actual = PrintResolution.ExponentOf(value);
                    if (expected != actual)
                    {
                        mismatches.Add($"{Path.GetFileName(file)}: '{token}' expected {expected}, got {actual}");
                    }
                }
            }
        }

        Assert.True(checkedTokens > 500_000, $"expected over 500,000 non-zero E tokens, counted {checkedTokens}.");
        Assert.Empty(mismatches);
    }

    // (b) The eleven arrays F-c's own knock-on names as count-like arrays a rule actually reads (never
    // `Dkarmcat`/`Dfr`/`Gfr`, which no rule reads the digits of): `inpt`'s `fqdokkarm(7,:)` and HMX's
    // `fqdokkarm(52,:)` through `fqdokkarm(59,:)`, eight rows — each array's first non-zero reference cell is
    // `0.100E+00`, the exact power of ten the old formula read one decade low (2 digits instead of 3).
    public static IEnumerable<object[]> CountLikeArraysAffectedByFC()
    {
        yield return ["inpt", "fqdokkarm(7,:)"];
        foreach (var row in new[] { 52, 53, 54, 55, 56, 57, 58, 59 })
        {
            yield return ["HMX", $"fqdokkarm({row},:)"];
        }
    }

    [Theory]
    [MemberData(nameof(CountLikeArraysAffectedByFC))]
    public void InferDecimalDigitsReturnsThreeOnTheCountLikeArraysFCNames(string formulation, string arrayName)
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var cells = ResultsMFile.ParseCells(path);
        var digits = PrintResolution.InferDecimalDigits(cells[arrayName]);
        Assert.Equal(3, digits);
    }

    // (c) The one cell F-c's own fix moves (root BOOT.md, "The `Original` accumulation kind reproduces…", link
    // 1): P33 `Independent`/`Original` seed 13 loses `fqkarm[62]`. The fixed quantum, `3.34e-8`, is the one-count
    // unit `RunQuantum.TryInfer` reads off this array's own smallest nonzero cell now that `0.100E-06`'s own
    // resolution is `1e-09`, not the old formula's `1e-10`. `fqkarm[63]` failed until B2c (2026-10-02) as the
    // region's edge (count 4 of high 4) and passes since
    // (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`; `CountRegionEdgeTests`, K4a, holds the mechanism).
    [Fact]
    public void FqkarmQuantumOnP33IndependentOriginalSeedThirteenPassesAt62And63()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "rate-runs", "P33", "Independent", "Original", "13.m.txt");
        var candidate = ResultsMFile.Parse(path);
        var report = StatisticalCriterion.Compare("P33", candidate, ReplicaKind.Independent);
        var perQuantityAlpha = StatisticalCriterion.Alpha / report.Compared;
        var verdicts = StatisticalCriterion.CompareCellVerdicts("P33", candidate, ReplicaKind.Independent, perQuantityAlpha);

        var cell62 = verdicts.Single(v => v.Name == "fqkarm" && v.Index == 62);
        var cell63 = verdicts.Single(v => v.Name == "fqkarm" && v.Index == 63);

        Assert.True(cell62.Quantum.HasValue, "expected fqkarm[62] to have an inferred quantum.");
        Assert.Equal(3.34e-8, cell62.Quantum!.Value, 0.005e-8);
        Assert.False(cell62.Failed);
        Assert.False(cell63.Failed);
    }
}
