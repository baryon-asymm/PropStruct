using System.Globalization;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Control C1 of the per-cycle plane's register-lifetime row (`src/Statistics/BOOT.md`,
/// "## Defects of the original", rows 771-1175; the review of 2026-10-02): the plane's
/// `da_coef` (`CycleReport.Mp`, Fortran 1001-1016) against every archived output of every
/// formulation whose input the archive ships. `da_coef` depends on the setup values and on one
/// FMDOK cell alone, so one constructed cycle (<see cref="ConstructedCycle"/>) stands for
/// every output of its formulation; the five reference formulations cannot tell the plane's
/// rules apart (every combination of the executable's three behaviours prints the same
/// `da_coef` on them), the legacy ones can.
///
/// Which outputs belong, found by reading them, never typed: an output belongs when its
/// `Input filename` names a shipped `.dat` (<see cref="ArchivedOutput"/>), it prints a
/// `da_coef`, its printed `Plot1`, `Plot2`, `Gdok` and `Gm` are the `.dat`'s, its printed
/// `Dmin`, `Di` and `Zok*` are the menu defaults and its printed `fmdok[0]` is empty (the
/// constructed cycle's premise), and it prints what the other outputs of its formulation
/// print. The rest is excluded with its reason, listed in the approved table.
///
/// The test is a ratchet, not a pass condition: the approved table
/// (`LegacyDaCoef.approved.txt`) names the formulations whose printed `da_coef` the plane
/// reproduces and those it misses, and goes red when that set changes in either direction. A
/// change that fixes a miss is not a failure to fix quietly: it moves the table in the same
/// commit. The misses are printed with both values.
/// </summary>
public class LegacyDaCoefReproductionTests
{
    private const string ApprovedFileName = "LegacyDaCoef.approved.txt";
    private const double MenuTolerance = 1e-9;

    private readonly ITestOutputHelper _output;

    public LegacyDaCoefReproductionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed record Candidate(string Output, string Formulation, string DatPath, bool ReadsFlags, ResultCell DaCoef);

    private static List<string> ArchivedOutputPaths()
    {
        var paths = Directory.EnumerateFiles(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "outputs"), "*.m.txt").ToList();
        paths.AddRange(Directory.EnumerateFiles(RepositoryPaths.Resolve("tests", "Fixtures", "references"), "results.m.txt", SearchOption.AllDirectories));
        paths.Sort(StringComparer.Ordinal);
        return paths;
    }

    private static string OutputName(string path)
    {
        var name = Path.GetFileName(path).Replace(".m.txt", string.Empty, StringComparison.Ordinal);
        return name == "results" ? $"{Path.GetFileName(Path.GetDirectoryName(path))}/results" : name;
    }

    private static string Token(double value, ResultCell printed) =>
        value.ToString($"F{(int)Math.Round(-Math.Log10(printed.Resolution))}", CultureInfo.InvariantCulture);

    /// <summary>The reason an output is left out, or <c>null</c> when it is a candidate.</summary>
    private static string? ExclusionReason(IReadOnlyDictionary<string, ResultCell[]> printed, Formulation formulation)
    {
        var headerValues = new (string Name, double Expected)[]
        {
            ("Plot1", formulation.OxidizerDensity),
            ("Plot2", formulation.PropellantDensity),
            ("Gdok", formulation.OxidizerMassFraction),
            ("Gm", formulation.MetalMassFraction),
        };
        foreach (var (name, expected) in headerValues)
        {
            var cell = printed[name][0];
            if (Math.Abs(cell.Value - expected) > cell.Resolution / 2 + MenuTolerance)
            {
                return $"header: printed {name} = {cell.Value:G} where the shipped input has {expected:G}";
            }
        }

        var defaults = ModelParameters.Default;
        var menuValues = new (string Name, double Expected)[]
        {
            ("Dmin", defaults.Dmin.Metres * 1e6),
            ("Di", defaults.CellSize.Metres * 1e6),
            ("Zok*", defaults.HomogenizedOxidizerFraction),
        };
        foreach (var (name, expected) in menuValues)
        {
            if (!printed.TryGetValue(name, out var cells) || Math.Abs(cells[0].Value - expected) > MenuTolerance)
            {
                return $"menu: {name} is not the default {expected:G}";
            }
        }

        return printed["fmdok"][0].Value == 0.0 ? null : "premise: the printed fmdok[0] is not empty";
    }

    /// <summary>
    /// Every candidate output of every formulation, the formulations' own verdicts and the
    /// excluded outputs, as the lines of the approved table; the misses' values go to the
    /// test output. A formulation's outputs that print different `da_coef` leave the
    /// minority out as outliers, a tie is an error.
    /// </summary>
    private string BuildTable()
    {
        var candidates = new List<Candidate>();
        var excluded = new List<string>();
        var skipped = 0;

        foreach (var path in ArchivedOutputPaths())
        {
            var dat = ArchivedOutput.ShippedFormulation(path);
            if (dat is null || !File.ReadLines(path).Any(line => line.TrimStart().StartsWith("da_coef", StringComparison.Ordinal)))
            {
                skipped++;
                continue;
            }

            var printed = ResultsMFile.ParseCells(path);

            var formulationName = Path.GetFileNameWithoutExtension(dat).ToUpperInvariant();
            var readsFlags = printed.ContainsKey("sfr");
            var formulation = DatFile.Read(dat, readsFlags);
            var reason = ExclusionReason(printed, formulation);
            if (reason is null)
            {
                candidates.Add(new Candidate(OutputName(path), formulationName, dat, readsFlags, printed["da_coef"][0]));
            }
            else
            {
                excluded.Add($"excluded {OutputName(path)}: {reason}");
            }
        }

        var lines = new List<string>();
        foreach (var group in candidates.GroupBy(c => c.Formulation, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var byToken = group.GroupBy(c => Token(c.DaCoef.Value, c.DaCoef)).OrderByDescending(g => g.Count()).ToList();
            if (byToken.Count > 1 && byToken[0].Count() == byToken[1].Count())
            {
                throw new InvalidOperationException($"{group.Key}: the outputs split {string.Join(" / ", byToken.Select(g => $"{g.Key} x{g.Count()}"))}, no majority.");
            }

            foreach (var outlier in byToken.Skip(1).SelectMany(g => g))
            {
                excluded.Add($"excluded {outlier.Output}: outlier: prints {Token(outlier.DaCoef.Value, outlier.DaCoef)} where the other {byToken[0].Count()} outputs of {group.Key} print {byToken[0].Key}");
            }

            var printedToken = byToken[0].Key;
            var reference = byToken[0].First();
            var computed = ConstructedCycle.Mp(reference.DatPath, reference.ReadsFlags, PrecisionKind.Original);
            var computedToken = Token(computed, reference.DaCoef);
            var matches = computedToken == printedToken;
            lines.Add($"{(matches ? "match" : "miss ")} {group.Key}");
            if (!matches)
            {
                _output.WriteLine($"miss {group.Key}: printed {printedToken}, the plane computes {computed:R} (prints {computedToken}); outputs: {string.Join(", ", byToken[0].Select(c => c.Output))}");
            }
        }

        var table = string.Concat(lines.Concat(excluded.OrderBy(e => e, StringComparer.Ordinal)).Select(line => line + "\n"));

        _output.WriteLine($"{lines.Count} formulations, {lines.Count(l => l.StartsWith("match", StringComparison.Ordinal))} match; {skipped} outputs without a shipped input or a da_coef line.");
        return table;
    }

    [Fact]
    public void TheMatchedSetOfFormulationsIsTheApprovedOne()
    {
        var received = BuildTable();
        var approvedPath = RepositoryPaths.Resolve("tests", "Statistics.Tests", ApprovedFileName);
        var approved = File.Exists(approvedPath) ? File.ReadAllText(approvedPath).Replace("\r\n", "\n", StringComparison.Ordinal) : string.Empty;
        Assert.True(approved == received,
            $"the matched set moved; if that is the intended change, replace {ApprovedFileName} with:\n{received}");
    }

    /// <summary>
    /// The reason `p777out1` is left out, checked rather than asserted: its header and its
    /// sample path are those of the other P777 outputs, only `da_coef` and what follows from it
    /// differ, and the plane run with `eta = 0.5` (menu [14], printed by no output) prints its
    /// `da_coef`. The default-`eta` constructed cycle cannot reproduce it by construction.
    /// </summary>
    [Fact]
    public void P777Out1IsTheRunOfAnotherEta()
    {
        var path = RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "outputs", "p777out1.m.txt");
        var printed = ResultsMFile.ParseCells(path)["da_coef"][0];
        var dat = ArchivedOutput.ShippedFormulation(path);
        Assert.NotNull(dat);

        var withEta = ModelParameters.Default with { AggregatedOxideFraction = 0.5 };
        var atHalf = ConstructedCycle.Mp(DatFile.Read(dat), withEta, PrecisionKind.Original);
        var atDefault = ConstructedCycle.Mp(DatFile.Read(dat), ModelParameters.Default, PrecisionKind.Original);

        Assert.Equal(Token(printed.Value, printed), Token(atHalf, printed));
        Assert.NotEqual(Token(printed.Value, printed), Token(atDefault, printed));
    }
}
