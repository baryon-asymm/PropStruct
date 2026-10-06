using System.Globalization;
using System.Text;
using PropStruct.Input;
using PropStruct.Particle;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The per-cycle plane of <c>CycleStatistics.Compute</c> under <see cref="PrecisionKind.Original"/> against the
/// listing oracle (<c>tests/Fixtures/cycle_plane_oracle.py</c>): the executable's own bytes, run over seeded
/// cases, with its answers in <c>cycle_plane_oracle.json</c>. Per case, first the setup: every input the oracle
/// injected must be the port's own (<see cref="TheOraclesSetupInputsAreThePortsOwn"/>), and what the executable's
/// pre-loop computed (line 375 and 378, the DOKM and DOKSD sums) is held against <c>Setup.Prepare</c> as a ratchet
/// of its own. Then <c>Compute</c> is run per cycle on the oracle's totals, bound to the port's slots by one
/// table (<see cref="OracleBinding"/>, checked complete both ways against the map), fed the executable's own
/// values for the setup echoes it computed itself, and every mapped output is compared bit for bit.
///
/// The differences are recorded as a ratchet, not asserted away: the approved file
/// (<c>ListingOracle.approved.txt</c>) lists every (case, cycle, output) whose bits differ, both values as hex,
/// and goes red when the set changes in either direction. It has been empty since the plane's rewrite from the
/// listing (<c>src/Statistics/ACCEPTANCE.md</c>, A2, 2026-10-03), so any output that parts from the executable's
/// bytes turns the test red; a difference that must be declared moves the file in the same commit. No expected
/// value is typed here: every number is the fixture's.
/// </summary>
public class ListingOracleTests
{
    private const string ApprovedFileName = "ListingOracle.approved.txt";
    private const string ScriptPath = "tests/Fixtures/cycle_plane_oracle.py";

    private readonly ITestOutputHelper _output;

    public ListingOracleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static PortSetup Prepare(OracleCase oracleCase) => PortSetup.Prepare(oracleCase.Formulation, oracleCase.Flags, oracleCase.Menu);

    /// <summary>The port's value of every setup key the fixture lists, by the oracle's name.</summary>
    private static IEnumerable<(string Key, double[] Port)> PortSetupValues(PortSetup port)
    {
        static double[] One(double value) => [value];
        yield return ("plot1", One(port.Inputs.OxidizerDensity));
        yield return ("plot2", One(port.Inputs.PropellantDensity));
        yield return ("ggg0", One(port.Inputs.OxidizerMassFraction));
        yield return ("gm", One(port.Inputs.MetalMassFraction));
        yield return ("gdokns", One(port.Inputs.HomogenizedOxidizerFraction));
        yield return ("eps_dok", One(port.Inputs.EpsDok));
        yield return ("eta", One(port.Inputs.AggregatedOxideFraction));
        yield return ("ak2", One(port.Setup.Ak2));
        yield return ("ak3", One(port.Setup.Ak3));
        yield return ("ak4", One(port.Setup.Ak4));
        yield return ("dmin", One(port.Setup.Dmin));
        yield return ("di", One(port.Setup.CellSize));
        yield return ("dj", One(port.Setup.CategoryStep));
        yield return ("karmcoef", One(port.Setup.PocketCoefficient));
        yield return ("mkmcoef", One(port.Setup.BridgeCoefficient));
        yield return ("alpha", One(port.Setup.Alpha));
        yield return ("nn_min", One(port.Setup.NnMin));
        yield return ("nn_max", One(port.Setup.NnMax));
        yield return ("nmm", One(port.Setup.FractionCount));
        yield return ("ndok", One(port.Setup.Ndok));
        yield return ("nkarm", One(port.Setup.Nkarm));
        yield return ("ncat", One(port.Setup.Ncat));
        yield return ("nc", One(port.Setup.Nc));
        yield return ("ddokmax", One(port.Pending.Ddokmax));
        yield return ("gdok", port.Tables.MassShare);
        yield return ("ddok", port.Tables.Bounds);
        yield return ("sfr", port.Tables.PocketForming.Select(f => (double)f).ToArray());
    }

    private static string Hex(double value) => $"0x{BitConverter.DoubleToInt64Bits(value):X16}";

    private static bool SameBits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

    [Fact]
    public void TheOraclesSetupInputsAreThePortsOwn()
    {
        var problems = new List<string>();
        foreach (var oracleCase in OracleFixture.Cases)
        {
            var port = Prepare(oracleCase);
            if (port.Status != SetupStatus.Ok)
            {
                problems.Add($"{oracleCase.Id} {oracleCase.Formulation}: Setup.Prepare returned {port.Status}");
                continue;
            }

            foreach (var (key, values) in PortSetupValues(port))
            {
                var element = oracleCase.Setup.GetProperty(key);
                var injected = element.ValueKind == System.Text.Json.JsonValueKind.Array ? oracleCase.SetupArray(key) : [element.GetDouble()];
                if (injected.Length != values.Length || injected.Where((v, i) => !SameBits(v, values[i])).Any())
                {
                    problems.Add($"{oracleCase.Id} {oracleCase.Formulation} {key}: oracle {string.Join(" ", injected.Take(3).Select(Hex))}, port {string.Join(" ", values.Take(3).Select(Hex))}");
                }
            }
        }

        Assert.True(problems.Count == 0, "the setup the oracle injected is not the port's:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void TheExecutablesPreLoopAgreesWithThePortAsApproved()
    {
        var received = new StringBuilder();
        foreach (var oracleCase in OracleFixture.Cases)
        {
            var port = Prepare(oracleCase);
            if (port.Status != SetupStatus.Ok)
            {
                continue;
            }

            foreach (var difference in port.PreloopDifferences(oracleCase.Preloop))
            {
                _ = received.Append(CultureInfo.InvariantCulture, $"{oracleCase.Id} {oracleCase.Formulation} {difference}\n");
            }
        }

        ApprovedFile.AssertMatches("ListingOracleSetup.approved.txt", received.ToString());
    }

    [Fact]
    public void ComputeMatchesTheOracleExceptWhereApproved()
    {
        var received = new StringBuilder();
        var compared = 0;
        var differing = 0;
        foreach (var oracleCase in OracleFixture.Cases)
        {
            var port = Prepare(oracleCase);
            if (port.Status != SetupStatus.Ok)
            {
                continue;
            }

            var (setup, echoes) = OracleRun.Echoes(port, oracleCase);
            foreach (var cycle in oracleCase.Cycles)
            {
                var result = OracleRun.Compute(port, setup, echoes, cycle);

                foreach (var (name, slot) in cycle.Outputs)
                {
                    if (!OracleBinding.Outputs.TryGetValue(name, out var read))
                    {
                        continue;
                    }

                    var elements = read(result);
                    if (elements is null)
                    {
                        continue;
                    }

                    compared++;
                    var differences = elements.Where(e => e.Index < slot.Count && !SameBits(slot.At(e.Index), e.Value)).ToList();
                    if (differences.Count > 0)
                    {
                        differing++;
                        var first = differences[0];
                        _ = received.Append(CultureInfo.InvariantCulture,
                            $"{oracleCase.Id} cycle {cycle.Cycle} {name}: {differences.Count} of {elements.Count} differ, first [{first.Index}] oracle {Hex(slot.At(first.Index))} port {Hex(first.Value)}\n");
                    }
                }
            }
        }

        _output.WriteLine($"{OracleFixture.Cases.Count} cases; {compared} outputs compared; {differing} differ.");
        ApprovedFile.AssertMatches(ApprovedFileName, received.ToString());
    }

    [Fact]
    public void TheBindingIsCompleteBothWaysAgainstTheMap()
    {
        var universe = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in File.ReadLines(RepositoryPaths.Resolve("src", "Statistics", "CyclePlaneListing.map.txt")))
        {
            var line = raw.Split('#')[0].Trim();
            var fields = line.Split('|').Select(f => f.Trim()).ToArray();
            if (fields[0] == "cell")
            {
                foreach (var tenant in fields[2].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!tenant.Contains(":temp", StringComparison.Ordinal))
                    {
                        _ = universe.Add(tenant.Split(':')[0].ToLowerInvariant());
                    }
                }
            }
            else if (fields[0] == "array")
            {
                _ = universe.Add(fields[2].ToLowerInvariant());
            }
            else if (fields[0] == "oracle" && fields[1] == "total")
            {
                _ = universe.Add(fields[3].ToLowerInvariant());
            }
        }

        var bound = OracleBinding.AllNames.ToHashSet(StringComparer.Ordinal);
        var unbound = universe.Where(n => !bound.Contains(n)).Order(StringComparer.Ordinal).ToList();
        var unknown = bound.Where(n => !universe.Contains(n)).Order(StringComparer.Ordinal).ToList();
        Assert.True(unbound.Count == 0, "names of the map with no slot, no setup role and no declared reason: " + string.Join(", ", unbound));
        Assert.True(unknown.Count == 0, "names of the binding that the map does not have: " + string.Join(", ", unknown));

        var twice = OracleBinding.Declared.Keys.Where(n => OracleBinding.Totals.ContainsKey(n) || OracleBinding.Outputs.ContainsKey(n) || OracleBinding.SetupInputs.Contains(n)).ToList();
        Assert.True(twice.Count == 0, "declared as unbound and also bound: " + string.Join(", ", twice));
    }

    [Fact]
    public void TheRecordedPowTriplesAreTheLibrarysOwn()
    {
        var triples = OracleFixture.Cases.SelectMany(c => c.Cycles).SelectMany(c => c.PowTriples).ToList();
        Assert.NotEmpty(triples);
        var differing = triples.Where(t => !SameBits(Math.Pow(t.Base, t.Exponent), t.Result)).ToList();
        if (differing.Count > 0)
        {
            Assert.Fail($"{differing.Count} of {triples.Count} recorded pow results differ from Math.Pow, first ({Hex(differing[0].Base)}, {Hex(differing[0].Exponent)}): oracle {Hex(differing[0].Result)}");
        }
    }

    [Fact]
    public void TheFixtureIsOfTheCommittedBytes()
    {
        var provenance = OracleFixture.Root.GetProperty("provenance");
        var scripts = provenance.GetProperty("scripts_sha256").EnumerateObject().ToList();
        Assert.Contains("formulas_statistics.py", scripts.Select(s => s.Name));
        var expected = new List<(string Name, string Recorded, string Actual)>
        {
            ("executable", provenance.GetProperty("executable_sha256").GetString()!, OriginalManifest.Sha256Of("PropStructV3.exe")),
            ("excerpt", provenance.GetProperty("excerpt_sha256").GetString()!, OriginalManifest.Sha256Of("PropStructV3.cycle-plane.listing.txt")),
            ("map", provenance.GetProperty("map_sha256").GetString()!, OracleFixture.Sha256("src", "Statistics", "CyclePlaneListing.map.txt")),
            ("site table", provenance.GetProperty("site_table_sha256").GetString()!, OracleFixture.Sha256("src", "Statistics", "CyclePlane.listing.generated.txt")),
        };
        expected.AddRange(scripts.Select(s => (s.Name, s.Value.GetString()!, OracleFixture.Sha256("tests", "Fixtures", s.Name))));
        var stale = expected.Where(e => !string.Equals(e.Recorded, e.Actual, StringComparison.OrdinalIgnoreCase)).Select(e => e.Name).ToList();
        Assert.True(stale.Count == 0, $"the fixture was generated from other bytes than these: {string.Join(", ", stale)}; run `python {ScriptPath} verify` and regenerate or restamp.");
    }

    /// <summary>
    /// The twin whose <c>oracle_setup</c> supplies every value the oracle injects reproduces its committed case
    /// files byte for byte (0.7 s measured, 2026-10-03): the fixture's digest of the twin says it is the bytes the
    /// fixture was made from, this says the twin still computes what its cases hold.
    /// </summary>
    [Fact]
    public void TheTwinThatSuppliesTheInjectedSetupReproducesItsCaseFiles() => PythonScript.RequireSuccess("tests/Fixtures/formulas_statistics.py", "verify");

    /// <summary>The oracle's guard: no name of the map and no literal of the plane in the oracle's own code. The
    /// literals of the plane are read off the source, which lies outside the repository (<c>tools/legacy</c>).</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheOracleHoldsNoMapNameAndNoPlaneLiteral() => PythonScript.RequireSuccess(ScriptPath, "guard");

    /// <summary>The machine's own checks: the dumpbin cross-check red on a swapped decoder and on an edited ModRM, the O1-O3 stops.
    /// It decodes the excerpt over the executable, both outside the repository.</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheMachineGoesRedOnEveryViolationItGuards() => PythonScript.RequireSuccess("tests/Fixtures/x87_machine.py", "selftest");

    /// <summary>The oracle's own proofs, each seen red: the guard on a typed literal and name, one moved home (r3), the exp-log pow rule (r5). Runs two cases.</summary>
    [Fact]
    [Trait("Category", "Long")]
    [Trait("Category", "Legacy")]
    public void TheOracleGoesRedOnEveryViolationItGuards() => PythonScript.RequireSuccess(ScriptPath, "selftest");

    /// <summary>The fixture regenerated from the executable, the excerpt and the source, every case and isolation replayed and
    /// every byte compared (465 s, 26 cases, measured 2026-10-03; the digests it records are compared in the fast set by
    /// <see cref="TheFixtureIsOfTheCommittedBytes"/>).</summary>
    [Fact]
    [Trait("Category", "Legacy")]
    public void TheOracleFixtureReproducesFromTheExecutable() => PythonScript.RequireSuccess(ScriptPath, "verify");
}
