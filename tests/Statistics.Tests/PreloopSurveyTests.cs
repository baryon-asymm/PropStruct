using System.Globalization;
using System.Text;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// <c>Setup.Prepare</c> under <c>Original</c> against what the executable's own pre-loop leaves, for every
/// <c>.dat</c> the repository ships (<c>src/Statistics/ACCEPTANCE.md</c>, A13). The oracle's fixture holds the
/// pre-loop for the cases it draws, <c>ListingOracleTests</c> compares them; this is the same comparison over all
/// the formulations, from <c>preloop_survey.json</c>, with the files where the port parts from the executable's
/// bits an approved ratchet (<c>PreloopSurvey.approved.txt</c>): it goes red when a second file differs, and when
/// the file it names stops differing. The survey's values are the executable's; no expected value is typed here.
/// </summary>
public class PreloopSurveyTests
{
    private const string ApprovedFileName = "PreloopSurvey.approved.txt";
    private const string ScriptPath = "tests/Fixtures/preloop_survey.py";

    [Fact]
    public void SetupPrepareAgreesWithTheExecutablesPreLoopOnEveryShippedFormulationAsApproved() =>
        ApprovedFile.AssertMatches(ApprovedFileName, Differences(PreloopSurvey.Entries));

    /// <summary>Red: one binary32 unit moved in the executable's value of one file, in memory, and that file is a line.</summary>
    [Fact]
    public void AnExecutedValueOneUnitAwayIsAFindingOfTheSurvey()
    {
        var entries = PreloopSurvey.Entries.ToList();
        var index = entries.FindIndex(e => e.Stop is null && !Differences([e]).Contains(e.Formulation, StringComparison.Ordinal));
        var moved = entries[index].Executed.ToDictionary(p => p.Key, p => p.Key == "dokm" ? p.Value.Select(b => b.Next()).ToArray() : p.Value, StringComparer.Ordinal);
        entries[index] = entries[index] with { Executed = moved };
        var lines = Differences(entries).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains(lines, line => line.StartsWith(entries[index].Formulation + ": dokm", StringComparison.Ordinal));
        Assert.Equal(Differences(PreloopSurvey.Entries).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length + 1, lines.Length);
    }

    /// <summary>
    /// The JZZ = 2 loop on loops of one to sixteen fractions the oracle drew: <c>Setup.AnalyticSizes</c> under <c>Original</c> against
    /// the bits the executable's pre-loop stored for <c>DOKM</c> and <c>DOKSD</c>. The shipped files tell the loop's stores apart in
    /// part only (<c>src/Statistics/ACCEPTANCE.md</c>, A12); these draws tell each of them apart, which the oracle's self-test shows.
    /// </summary>
    [Fact]
    public void AnalyticSizesReproducesTheExecutablesJzz2LoopOnDrawnLoops()
    {
        Assert.NotEmpty(PreloopSurvey.Jzz2Controls);
        var differing = Jzz2Differences(PreloopSurvey.Jzz2Controls);
        Assert.True(differing.Count == 0, $"{differing.Count} of {PreloopSurvey.Jzz2Controls.Count} drawn loops differ: {string.Join("; ", differing.Take(3))}");
    }

    /// <summary>Red: one binary32 unit moved in the executable's <c>DOKSD</c> of one drawn loop, in memory, and that loop is found.</summary>
    [Fact]
    public void ADrawnJzz2LoopWithAnExecutedValueOneUnitAwayIsFound()
    {
        var controls = PreloopSurvey.Jzz2Controls.ToList();
        controls[0] = controls[0] with { Doksd = controls[0].Doksd.Next() };
        _ = Assert.Single(Jzz2Differences(controls));
    }

    [Fact]
    public void TheSurveyHoldsEveryShippedFormulationAndStopsOnNone()
    {
        var shipped = Directory.EnumerateFiles(RepositoryPaths.Resolve("tests", "Fixtures", "Legacy", "formulations"), "*.dat")
            .Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(shipped, PreloopSurvey.Entries.Select(e => e.Formulation).ToList());
        Assert.All(PreloopSurvey.Entries, entry => Assert.Null(entry.Stop));
    }

    [Fact]
    public void TheSurveyIsOfTheCommittedBytes()
    {
        var provenance = PreloopSurvey.Root.GetProperty("provenance");
        var scripts = provenance.GetProperty("scripts_sha256");
        var recorded = new List<(string Name, string Recorded, string Actual)>
        {
            ("executable", provenance.GetProperty("executable_sha256").GetString()!, OriginalManifest.Sha256Of("PropStructV3.exe")),
            ("excerpt", provenance.GetProperty("excerpt_sha256").GetString()!, OriginalManifest.Sha256Of("PropStructV3.cycle-plane.listing.txt")),
            ("map", provenance.GetProperty("map_sha256").GetString()!, OracleFixture.Sha256("src", "Statistics", "CyclePlaneListing.map.txt")),
            ("site table", provenance.GetProperty("site_table_sha256").GetString()!, OracleFixture.Sha256("src", "Statistics", "CyclePlane.listing.generated.txt")),
        };
        recorded.AddRange(scripts.EnumerateObject().Select(s => (s.Name, s.Value.GetString()!, OracleFixture.Sha256("tests", "Fixtures", s.Name))));
        recorded.AddRange(PreloopSurvey.Entries.Select(e => (e.Formulation, e.DatSha256, OracleFixture.Sha256("tests", "Fixtures", "Legacy", "formulations", e.Formulation))));
        var stale = recorded.Where(r => !string.Equals(r.Recorded, r.Actual, StringComparison.OrdinalIgnoreCase)).Select(r => r.Name).ToList();
        Assert.True(stale.Count == 0, $"the survey was generated from other bytes than these: {string.Join(", ", stale)}; run `python {ScriptPath} verify` and regenerate.");
    }

    /// <summary>The survey regenerated from the executable and the source, which lie outside the repository, byte for byte
    /// (about half a minute; the digests it records are compared in the fast set by <see cref="TheSurveyIsOfTheCommittedBytes"/>).</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheSurveyReproducesFromTheExecutable() => PythonScript.RequireSuccess(ScriptPath, "verify");

    /// <summary>One line per formulation where the executable's pre-loop and <c>Setup.Prepare</c> differ, every differing key with both binary32 values.</summary>
    private static string Differences(IEnumerable<SurveyEntry> entries)
    {
        var received = new StringBuilder();
        foreach (var entry in entries)
        {
            if (entry.Stop is not null)
            {
                _ = received.Append(CultureInfo.InvariantCulture, $"{entry.Formulation}: the executable's pre-loop stopped: {entry.Stop}\n");
                continue;
            }

            var port = PortSetup.Prepare(entry.Formulation, null, new Dictionary<string, double>());
            if (port.Status != SetupStatus.Ok)
            {
                _ = received.Append(CultureInfo.InvariantCulture, $"{entry.Formulation}: Setup.Prepare returned {port.Status}\n");
                continue;
            }

            var differing = port.PreloopDifferences(entry.Executed).ToList();
            if (differing.Count > 0)
            {
                _ = received.Append(CultureInfo.InvariantCulture, $"{entry.Formulation}: {string.Join("; ", differing)}\n");
            }
        }

        return received.ToString();
    }

    private static List<string> Jzz2Differences(IReadOnlyList<Jzz2Control> controls)
    {
        var differing = new List<string>();
        for (var i = 0; i < controls.Count; i++)
        {
            var control = controls[i];
            Statistics.Setup.AnalyticSizes(2, control.Shares.Length, control.Shares, control.Zx, control.Bounds, PrecisionKind.Original, out var dokm, out var doksd);
            if (control.Dokm.HexOf(dokm) != control.Dokm.Hex || control.Doksd.HexOf(doksd) != control.Doksd.Hex)
            {
                differing.Add($"loop {i}: DOKM {control.Dokm.HexOf(dokm)} against {control.Dokm.Hex}, DOKSD {control.Doksd.HexOf(doksd)} against {control.Doksd.Hex}");
            }
        }

        return differing;
    }
}
