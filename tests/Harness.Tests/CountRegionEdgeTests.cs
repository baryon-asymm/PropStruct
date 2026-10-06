using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// The known-answer controls of B2c (<c>tests/Harness/HISTORY.md#count-region-edge-2026-10-02</c>): a
/// count-governed cell passes its whole predictive region, the edge `high` included, and still fails a count
/// beyond it. Every control is a real cell of a stored run, read through the same
/// <see cref="StatisticalCriterion"/> entry points the criterion's own callers use; no value is retyped, and
/// the one count a cell is raised to is its own printed value plus its own run's quantum.
///
/// K4a is the sparse edge (the two runs the rate criterion lost to it), K4b the dense edge (the leave-one-out
/// cells the calibration curve counted), K4c the positive control (each of those cells, one quantum higher,
/// fails), K4d the no-movement control (two dense `fqdokkarm` cells whose centre departs from the predictive
/// mean by a whole count, which a comparison centred on the predictive mean alone would fail).
/// </summary>
public sealed class CountRegionEdgeTests
{
    private const string Hpepa3 = "HPEPA3";
    private const string P33 = "P33";
    private const double DenseLevel = 0.05;

    private readonly ITestOutputHelper _output;

    public CountRegionEdgeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed record EdgeCell(
        string Formulation, ReplicaKind Kind, string Quantity, int Index, int? LeftOutOrdinal, string Source);

    private static Dictionary<string, double[]> Replica(string formulation, ReplicaKind kind, int ordinal)
    {
        var directory = kind == ReplicaKind.Lagged ? "replicas-lagged" : "replicas-independent";
        return new Dictionary<string, double[]>(
            ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", directory, formulation, ordinal + ".m.txt")));
    }

    private static Dictionary<string, double[]> RateRun(string formulation, string layout, string precision, int seedIndex) =>
        new(RateRuns.Load(formulation, layout, precision)[seedIndex]);

    // The cell's own verdict at a level; `alpha` is the per-cell level `Compare` would apply (`Alpha / m`) unless
    // the control names a level of its own.
    private static CellVerdict VerdictAt(
        EdgeCell cell, IReadOnlyDictionary<string, double[]> candidate, double alpha) =>
        StatisticalCriterion.CompareCellVerdicts(
            cell.Formulation, candidate, cell.Kind, alpha, cell.LeftOutOrdinal)
            .Single(v => v.Name == cell.Quantity && v.Index == cell.Index);

    private static double FamilyWiseLevel(EdgeCell cell, IReadOnlyDictionary<string, double[]> candidate) =>
        StatisticalCriterion.Alpha / Math.Max(
            StatisticalCriterion.Compare(cell.Formulation, candidate, cell.Kind, cell.LeftOutOrdinal).Compared, 1);

    private static Dictionary<string, double[]> WithCellRaisedByOneQuantum(
        IReadOnlyDictionary<string, double[]> candidate, CellVerdict verdict)
    {
        var raised = new Dictionary<string, double[]>(candidate);
        var array = (double[])raised[verdict.Name].Clone();
        array[verdict.Index] = verdict.CandidateValue + Assert.IsType<double>(verdict.Quantum);
        raised[verdict.Name] = array;
        return raised;
    }

    private void AssertCellIsOnTheEdge(CellVerdict verdict)
    {
        Assert.Equal(CalibrationRule.Count, verdict.Rule);
        var quantum = Assert.IsType<double>(verdict.Quantum);
        var regionHigh = Assert.IsType<double>(verdict.RegionHigh);
        var observed = Assert.IsType<double>(verdict.ObservedCount);
        _output.WriteLine(
            $"{verdict.Name}[{verdict.Index}]: count {observed:G6} of region high {regionHigh / quantum:G6}, " +
            $"failed={verdict.Failed}.");
        Assert.Equal(Math.Round(regionHigh / quantum), observed);
    }

    // K4a and K4b over one cell: the cell sits on its predictive region's own edge `high` and passes it; one
    // quantum higher (K4c) must fail, at every code state.
    private void AssertEdgeCellPasses(
        EdgeCell cell, IReadOnlyDictionary<string, double[]> candidate, double alpha)
    {
        var verdict = VerdictAt(cell, candidate, alpha);
        AssertCellIsOnTheEdge(verdict);
        Assert.False(verdict.Failed, $"{cell.Source} {verdict.Name}[{verdict.Index}] fails on the region's own edge");
    }

    private static void AssertOneQuantumMoreFails(
        EdgeCell cell, IReadOnlyDictionary<string, double[]> candidate, double alpha)
    {
        var verdict = VerdictAt(cell, candidate, alpha);
        var raised = VerdictAt(cell, WithCellRaisedByOneQuantum(candidate, verdict), alpha);
        Assert.True(raised.Failed, $"{cell.Source} {verdict.Name}[{verdict.Index}] passes one quantum beyond the edge");
    }

    public static TheoryData<string, string, int> SparseEdgeCellNames => new()
    {
        { "HPEPA3 independent replica 14", "fqkarm", 74 },
        { "HPEPA3 independent replica 14", "fqkarm_cor", 74 },
        { "P33 Independent/Original seed 13", "fqkarm", 63 },
    };

    private static (EdgeCell Cell, Dictionary<string, double[]> Candidate) SparseRun(string source, string quantity, int index)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.StartsWith(Hpepa3, StringComparison.Ordinal))
        {
            return (new EdgeCell(Hpepa3, ReplicaKind.Independent, quantity, index, 14, source),
                Replica(Hpepa3, ReplicaKind.Independent, 14));
        }

        return (new EdgeCell(P33, ReplicaKind.Independent, quantity, index, null, source),
            RateRun(P33, "Independent", "Original", 13));
    }

    [Fact]
    public void K4aSparseEdgeHpepa3IndependentReplica14FailsNoCell()
    {
        var candidate = Replica(Hpepa3, ReplicaKind.Independent, 14);
        var failures = StatisticalCriterion.Compare(Hpepa3, candidate, ReplicaKind.Independent, excludeReplicaOrdinal: 14).Failures;
        _output.WriteLine("failing cells: " + string.Join(", ", failures.Select(f => $"{f.Quantity}[{f.Index}]")));
        Assert.Empty(failures);
    }

    [Fact]
    public void K4aSparseEdgeP33IndependentOriginalSeed13FailsNoCell()
    {
        var candidate = RateRun(P33, "Independent", "Original", 13);
        var failures = StatisticalCriterion.Compare(P33, candidate, ReplicaKind.Independent).Failures;
        _output.WriteLine("failing cells: " + string.Join(", ", failures.Select(f => $"{f.Quantity}[{f.Index}]")));
        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(SparseEdgeCellNames))]
    public void K4aSparseEdgeCellPassesOnItsOwnEdge(string source, string quantity, int index)
    {
        var (cell, candidate) = SparseRun(source, quantity, index);
        AssertEdgeCellPasses(cell, candidate, FamilyWiseLevel(cell, candidate));
    }

    [Theory]
    [MemberData(nameof(SparseEdgeCellNames))]
    public void K4cSparseEdgeCellOneQuantumHigherFails(string source, string quantity, int index)
    {
        var (cell, candidate) = SparseRun(source, quantity, index);
        AssertOneQuantumMoreFails(cell, candidate, FamilyWiseLevel(cell, candidate));
    }

    public static TheoryData<int, string, int> DenseEdgeCells => new()
    {
        { 6, "coef", 468 },
        { 1, "coef", 490 },
    };

    [Theory]
    [MemberData(nameof(DenseEdgeCells))]
    public void K4bDenseEdgeCellPassesOnItsOwnEdge(int ordinal, string quantity, int index)
    {
        var cell = new EdgeCell(Hpepa3, ReplicaKind.Lagged, quantity, index, ordinal, $"HPEPA3 lagged replica {ordinal}");
        AssertEdgeCellPasses(cell, Replica(Hpepa3, ReplicaKind.Lagged, ordinal), DenseLevel);
    }

    [Theory]
    [MemberData(nameof(DenseEdgeCells))]
    public void K4cDenseEdgeCellOneQuantumHigherFails(int ordinal, string quantity, int index)
    {
        var cell = new EdgeCell(Hpepa3, ReplicaKind.Lagged, quantity, index, ordinal, $"HPEPA3 lagged replica {ordinal}");
        AssertOneQuantumMoreFails(cell, Replica(Hpepa3, ReplicaKind.Lagged, ordinal), DenseLevel);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(19)]
    public void K4dDenseCellWhoseCentreDepartsFromThePredictiveMeanPassesAtEveryCodeState(int index)
    {
        const string quantity = "fqdokkarm(6,:)";
        var cell = new EdgeCell(Hpepa3, ReplicaKind.Lagged, quantity, index, 24, "HPEPA3 lagged replica 24");
        var verdict = VerdictAt(cell, Replica(Hpepa3, ReplicaKind.Lagged, 24), DenseLevel);
        _output.WriteLine($"{verdict.Name}[{verdict.Index}]: rule {verdict.Rule}, count {verdict.ObservedCount}, failed={verdict.Failed}.");
        Assert.Equal(CalibrationRule.Count, verdict.Rule);
        Assert.False(verdict.Failed);
    }
}
