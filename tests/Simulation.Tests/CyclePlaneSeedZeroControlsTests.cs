using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Simulation.Tests;

/// <summary>
/// The frozen controls of the per-cycle plane under <c>PrecisionKind.Original</c>
/// (src/Statistics/BOOT.md, "## Report", "The per-cycle plane under `Original`"), on the
/// seed-0 reference-mode run of each reference formulation, <c>Original</c> layout and kind:
/// the port's run is <c>tests/Fixtures/rate-runs/&lt;F&gt;/Original/Original/0.m.txt</c> (the
/// seed-0 snapshot's own row, tied to it by the rate table's digests), the original's is
/// <c>tests/Fixtures/references/&lt;F&gt;/results.m.txt</c>, and the pre-change run is the
/// frozen copy <c>Snapshots/CyclePlaneBaseline/&lt;F&gt;.m.txt</c> (taken byte for byte from
/// the rate runs of 514a157 before any code of the plane changed). This file names cells
/// only; every value is read from those three files (root BOOT.md: "No expected value typed
/// into a test when it exists in a fixture file").
///
/// Positive controls: each named cell prints the original's value (P1 <c>da_coef</c> on all
/// five; P2 HMX <c>epsx(1..4)</c>; P3 P33 <c>epsx(1..6)</c>; P4 HMX <c>epsalldok</c> and
/// <c>epsdok(2)</c>, P33 <c>epsalldok</c>; P5 inpt <c>epsdok43_n[0..4]</c>). Negative controls:
/// N1 HMX <c>epsdokfr</c> still prints the original's; N2 no cell the baseline printed
/// exactly as the original stops doing so; N4 the generator counters of HMX and P33 still
/// equal the original's, and no counter or printed length moves away from the baseline
/// unless onto the original's own value.
/// </summary>
public class CyclePlaneSeedZeroControlsTests
{
    private static readonly string[] ReferenceFormulations = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX"];

    public static IEnumerable<object[]> Formulations() =>
        ReferenceFormulations.Select(f => new object[] { f });

    public static IEnumerable<object[]> PositiveControls()
    {
        foreach (var f in ReferenceFormulations)
        {
            yield return new object[] { "P1", f, "da_coef", 0 };
        }

        for (var i = 1; i <= 4; i++)
        {
            yield return new object[] { "P2", "HMX", $"epsx({i})", 0 };
        }

        for (var i = 1; i <= 6; i++)
        {
            yield return new object[] { "P3", "P33", $"epsx({i})", 0 };
        }

        yield return new object[] { "P4", "HMX", "epsalldok", 0 };
        yield return new object[] { "P4", "HMX", "epsdok(2)", 0 };
        yield return new object[] { "P4", "P33", "epsalldok", 0 };
        for (var i = 0; i < 5; i++)
        {
            yield return new object[] { "P5", "inpt", "epsdok43_n", i };
        }

        for (var i = 0; i < 4; i++)
        {
            yield return new object[] { "N1", "HMX", "epsdokfr", i };
        }
    }

    [Theory]
    [MemberData(nameof(PositiveControls))]
    public void ControlCellPrintsTheOriginalsValue(string control, string formulation, string name, int index)
    {
        var port = Port(formulation)[name][index];
        var archive = Archive(formulation)[name][index];
        Assert.True(port.Value == archive.Value, $"{control} {formulation} {name}[{index}]: port {port.Value:R}, original {archive.Value:R}");
    }

    [Theory]
    [MemberData(nameof(Formulations))]
    public void NoCellTheBaselinePrintedExactlyStopsBeingExact(string formulation)
    {
        var archive = Archive(formulation);
        var baseline = Baseline(formulation);
        var port = Port(formulation);
        var lost = new List<string>();
        var exact = 0;
        foreach (var (name, cells) in baseline)
        {
            if (!archive.TryGetValue(name, out var original))
            {
                continue;
            }

            for (var i = 0; i < Math.Min(cells.Length, original.Length); i++)
            {
                if (cells[i].Value != original[i].Value)
                {
                    continue;
                }

                exact++;
                if (!port.TryGetValue(name, out var now) || i >= now.Length || now[i].Value != original[i].Value)
                {
                    lost.Add($"{name}[{i}]");
                }
            }
        }

        Assert.True(exact > 0, "the baseline shares no exact cell with the original: the control is degenerate");
        Assert.True(lost.Count == 0, $"{formulation}: {lost.Count} of {exact} exact cells lost: {string.Join(", ", lost.Take(20))}");
    }

    [Theory]
    [MemberData(nameof(Formulations))]
    public void NoCounterOrPrintedLengthMovesAwayFromTheOriginal(string formulation)
    {
        var archive = Archive(formulation);
        var baseline = Baseline(formulation);
        var port = Port(formulation);
        var moved = new List<string>();
        foreach (var (name, before) in baseline)
        {
            if (!port.TryGetValue(name, out var now))
            {
                moved.Add($"{name}: gone");
                continue;
            }

            var original = archive.GetValueOrDefault(name);
            if (now.Length != before.Length && (original is null || now.Length != original.Length))
            {
                moved.Add($"{name}: length {before.Length} -> {now.Length}");
            }

            for (var i = 0; i < Math.Min(now.Length, before.Length); i++)
            {
                if (before[i].IsIntegerPrinted && now[i].Value != before[i].Value
                    && (original is null || i >= original.Length || now[i].Value != original[i].Value))
                {
                    moved.Add($"{name}[{i}]: {before[i].Value} -> {now[i].Value}");
                }
            }
        }

        Assert.True(moved.Count == 0, $"{formulation}: " + string.Join("; ", moved));
    }

    [Theory]
    [InlineData("HMX")]
    [InlineData("P33")]
    public void GeneratorCountersStillEqualTheOriginals(string formulation)
    {
        var archive = Archive(formulation);
        var port = Port(formulation);
        foreach (var name in new[] { "NFX", "NFY", "NFQ", "NFW" })
        {
            Assert.Equal(archive[name][0].Value, port[name][0].Value);
        }
    }

    private static IReadOnlyDictionary<string, ResultCell[]> Port(string formulation) =>
        ResultsMFile.ParseCells(RepositoryPaths.Resolve("tests", "Fixtures", "rate-runs", formulation, "Original", "Original", "0.m.txt"));

    private static IReadOnlyDictionary<string, ResultCell[]> Archive(string formulation) =>
        ResultsMFile.ParseCells(RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt"));

    private static IReadOnlyDictionary<string, ResultCell[]> Baseline(string formulation) =>
        ResultsMFile.ParseCells(RepositoryPaths.Resolve("tests", "Simulation.Tests", "Snapshots", "CyclePlaneBaseline", formulation + ".m.txt"));
}
