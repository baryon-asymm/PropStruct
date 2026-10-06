using ILGPU.Runtime;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L1 of the BOOT.md table: the window <see cref="BridgeWindow.Build"/> produces depends
/// only on <c>Dr</c>, <c>AK3</c>, <c>AK4</c>, <c>Di</c> and <c>QKS1</c> (BOOT.md, "Bridge
/// window once per attempt") — never on whatever the scratch it writes into held before the
/// call. Building it "once per attempt" and reusing it therefore equals rebuilding it at
/// every bridge, bit for bit, on constructed inputs (the HPEPA3 10⁵-attempt version needs
/// <c>Statistics.Setup.Prepare</c> and stays out of this node's rows). Also proves
/// <see cref="BridgeWindow.SamplePocket"/>'s <see cref="AttemptOutcome.IndexOutOfRange"/>
/// guard (Fortran's <c>DM</c> search has none), a branch <see cref="AttemptOutcomeTests"/>
/// cannot reach through <see cref="Attempt.Run"/> because it is impossible under this
/// node's own preconditions (BOOT.md, "Attempt structure" outcomes table).
/// </summary>
public class BridgeWindowTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public BridgeWindowTests(CpuHost host)
    {
        _host = host;
    }

    [Fact]
    public void BuildIgnoresWhateverTheScratchHeldBefore()
    {
        const int nkarm = 20;
        var qks1Values = new double[nkarm];
        for (var i = 0; i < nkarm; i++)
        {
            qks1Values[i] = (i % 3 == 0) ? 0.0 : 1.0 + i; // an uneven histogram, several empty cells
        }

        var acc = _host.Accelerator;
        using var qks1Buffer = acc.Allocate1D(qks1Values);
        var qks1 = qks1Buffer.View.BaseView;

        // "Once per attempt": scratch starts at the zero a freshly-reset attempt leaves it at.
        using var fqksFreshBuffer = acc.Allocate1D<double>(nkarm + 1);
        using var dpocFreshBuffer = acc.Allocate1D<double>(nkarm + 1);
        fqksFreshBuffer.MemSetToZero();
        dpocFreshBuffer.MemSetToZero();

        // "Rebuilt at every bridge": scratch carries an unrelated earlier bridge's leftovers.
        var dirty = new double[nkarm + 1];
        for (var i = 0; i < dirty.Length; i++)
        {
            dirty[i] = 999.0 + i;
        }

        using var fqksDirtyBuffer = acc.Allocate1D(dirty);
        using var dpocDirtyBuffer = acc.Allocate1D(dirty);

        const double dr = 3.0, ak3 = 0.2, ak4 = 0.9, cellSize = 0.5;

        var freshOk = BridgeWindow.Build(dr, ak3, ak4, cellSize, nkarm, qks1, fqksFreshBuffer.View.BaseView, dpocFreshBuffer.View.BaseView, out var nnn1Fresh);
        var dirtyOk = BridgeWindow.Build(dr, ak3, ak4, cellSize, nkarm, qks1, fqksDirtyBuffer.View.BaseView, dpocDirtyBuffer.View.BaseView, out var nnn1Dirty);

        Assert.True(freshOk);
        Assert.True(dirtyOk);
        Assert.Equal(nnn1Fresh, nnn1Dirty);

        var fqksFresh = fqksFreshBuffer.GetAsArray1D();
        var fqksDirty = fqksDirtyBuffer.GetAsArray1D();
        var dpocFresh = dpocFreshBuffer.GetAsArray1D();
        var dpocDirty = dpocDirtyBuffer.GetAsArray1D();

        for (var i = 0; i < nnn1Fresh; i++)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(fqksFresh[i]), BitConverter.DoubleToInt64Bits(fqksDirty[i]));
            Assert.Equal(BitConverter.DoubleToInt64Bits(dpocFresh[i]), BitConverter.DoubleToInt64Bits(dpocDirty[i]));
        }

        // And every pocket-size sample against the two windows agrees, bit for bit, across
        // the window's whole covered range.
        for (var k = 0; k < 50; k++)
        {
            var x = k / 50.0 * 0.999; // [0, 0.999), inside [0, 1)
            var okFresh = BridgeWindow.SamplePocket(x, fqksFreshBuffer.View.BaseView, dpocFreshBuffer.View.BaseView, nnn1Fresh, out var dkarmFresh);
            var okDirty = BridgeWindow.SamplePocket(x, fqksDirtyBuffer.View.BaseView, dpocDirtyBuffer.View.BaseView, nnn1Dirty, out var dkarmDirty);

            Assert.Equal(okFresh, okDirty);
            if (okFresh)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(dkarmFresh), BitConverter.DoubleToInt64Bits(dkarmDirty));
            }
        }
    }

    /// <summary>
    /// L0: <see cref="BridgeWindow.Build"/> against the hand-derived window (boundaries and
    /// cumulative fractions) of <c>tests/Fixtures/cases/particle/bridge_window.json</c>, bit
    /// for bit, including the empty-window case (line 590's <c>AUS = 0</c> restart) and the
    /// early truncation from trailing zero cells (the <c>1e-5</c> test of lines 600-606
    /// firing before <c>maxdk</c>'s natural end).
    /// </summary>
    public static IEnumerable<object[]> BuildCases()
    {
        foreach (var testCase in FormulaCaseFiles.ReadBridgeWindowCases())
        {
            yield return new object[] { testCase.Name };
        }
    }

    [Theory]
    [MemberData(nameof(BuildCases))]
    public void BuildMatchesTheFixtureBitForBit(string name)
    {
        var testCase = FormulaCaseFiles.FindBridgeWindowCase(name);
        var (cellSize, ak3, ak4, dr, qks1, expected) =
            (testCase.CellSize, testCase.Ak3, testCase.Ak4, testCase.Dr, testCase.Qks1.ToArray(), testCase.Expected);

        var acc = _host.Accelerator;
        var nkarm = qks1.Length;
        using var qks1Buffer = acc.Allocate1D(qks1);
        using var fqksBuffer = acc.Allocate1D<double>(nkarm + 1);
        using var dpocBuffer = acc.Allocate1D<double>(nkarm + 1);

        var built = BridgeWindow.Build(dr, ak3, ak4, cellSize, nkarm,
            qks1Buffer.View.BaseView, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, out var nnn1);

        Assert.True(built == !expected.EmptyWindow, $"{name}: expected emptyWindow={expected.EmptyWindow}, got built={built}");
        if (!built)
        {
            return;
        }

        Assert.True(expected.CellCount!.Value == nnn1, $"{name}: expected cellCount {expected.CellCount.Value}, got {nnn1}");

        var fqks = fqksBuffer.GetAsArray1D();
        var dpoc = dpocBuffer.GetAsArray1D();
        for (var i = 0; i < nnn1; i++)
        {
            Assert.True(
                BitConverter.DoubleToInt64Bits(expected.Boundaries![i]) == BitConverter.DoubleToInt64Bits(dpoc[i]),
                $"{name}: boundary {i}: expected {expected.Boundaries[i]}, got {dpoc[i]}");
            Assert.True(
                BitConverter.DoubleToInt64Bits(expected.Cumulative![i]) == BitConverter.DoubleToInt64Bits(fqks[i]),
                $"{name}: cumulative {i}: expected {expected.Cumulative[i]}, got {fqks[i]}");
        }
    }

    /// <summary>
    /// L0: <see cref="BridgeWindow.SamplePocket"/> against the same fixture's <c>samples</c>
    /// (built by the same <see cref="BridgeWindow.Build"/> the sample was generated against),
    /// bit for bit — the defect this replaces (2026-09-17) compared a hand-typed <c>15.0</c>
    /// with <c>precision: 12</c> against this node's own stated invariant "the L0 and L1
    /// levels are bit-exact" (BOOT.md, Invariants).
    /// </summary>
    public static IEnumerable<object[]> SampleCases()
    {
        foreach (var testCase in FormulaCaseFiles.ReadBridgeWindowCases())
        {
            foreach (var sample in testCase.Samples)
            {
                yield return new object[] { testCase.Name, testCase.CellSize, testCase.Ak3, testCase.Ak4, testCase.Dr, testCase.Qks1.ToArray(), sample.X3, sample.Expected.PocketSize };
            }
        }
    }

    [Theory]
    [MemberData(nameof(SampleCases))]
    public void SamplePocketMatchesTheFixtureBitForBit(
        string name, double cellSize, double ak3, double ak4, double dr, double[] qks1, double x3, double expectedPocketSize)
    {
        ArgumentNullException.ThrowIfNull(qks1);

        var acc = _host.Accelerator;
        var nkarm = qks1.Length;
        using var qks1Buffer = acc.Allocate1D(qks1);
        using var fqksBuffer = acc.Allocate1D<double>(nkarm + 1);
        using var dpocBuffer = acc.Allocate1D<double>(nkarm + 1);

        var built = BridgeWindow.Build(dr, ak3, ak4, cellSize, nkarm,
            qks1Buffer.View.BaseView, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, out var nnn1);
        Assert.True(built, $"{name}: fixture case with samples must build a non-empty window");

        var ok = BridgeWindow.SamplePocket(x3, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, nnn1, out var pocketSize);

        Assert.True(ok, $"{name}: x3={x3} must fall inside the built window");
        Assert.True(
            BitConverter.DoubleToInt64Bits(expectedPocketSize) == BitConverter.DoubleToInt64Bits(pocketSize),
            $"{name}: x3={x3}: expected pocketSize {expectedPocketSize}, got {pocketSize}");
    }

    /// <summary>
    /// Pins the exact one-cell offset `src/Particle/BOOT.md`'s "## Defects of the original"
    /// records for Fortran line 595 (fidelity audit 2026-09-24, U-P1): <c>DPOC(mindk) =
    /// Di*mindk</c> is the <em>upper</em> edge of QKS1 cell <c>mindk</c>, not its lower edge
    /// <c>Di*(mindk-1)</c> — QKS1's own cell convention, established once, node-wide, at
    /// Fortran line 675 (<c>iks = int(RK/Di)+1; QKS(iks) = QKS(iks)+1</c>, reproduced by
    /// <c>PocketHistogram</c>/<c>Attempt.Run</c>'s own pocket branch), makes cell <c>mindk</c>'s
    /// own interval <c>[Di*(mindk-1), Di*mindk)</c>. The measured consequence (`Dqmkm2` moves
    /// -2.8% to -7.0%, `Zkarm` +4.9% to +13.5%, both far beyond the seeds' own spread, on every
    /// reference formulation) is `src/Particle/HISTORY.md#bridge-window-origin-audit-2026-09-24`; the port
    /// reproduces the original's own value, so this test's expected value is the <em>upper</em>
    /// edge, deliberately — a fixed value here would go unnoticed if <see cref="BridgeWindow.
    /// Build"/> were ever silently "fixed" (root BOOT.md taboo, "no silent fix of a defect").
    /// </summary>
    [Fact]
    public void BuildAnchorsTheWindowOneCellAboveQks1SOwnCellConvention()
    {
        const double dr = 10.0, ak3 = 0.3, ak4 = 0.5, cellSize = 1.0;
        // mindk = int(ak3*dr/cellSize) + 1 = int(3.0) + 1 = 4 (Fortran line 584's own convention,
        // the same one line 675 uses for QKS1 itself): cell 4 covers [3.0, 4.0) in QKS1's own axis.
        const int mindk = 4;
        var qks1Values = new[] { 0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 0.0, 0.0 }; // nonzero at 1-based 4,5,6

        var acc = _host.Accelerator;
        using var qks1Buffer = acc.Allocate1D(qks1Values);
        using var fqksBuffer = acc.Allocate1D<double>(qks1Values.Length + 1);
        using var dpocBuffer = acc.Allocate1D<double>(qks1Values.Length + 1);

        var built = BridgeWindow.Build(dr, ak3, ak4, cellSize, qks1Values.Length,
            qks1Buffer.View.BaseView, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, out _);
        Assert.True(built);

        var dpoc = dpocBuffer.GetAsArray1D();

        // The window's first breakpoint, before the post-loop shift to index 1, is DPOC(mindk).
        // BridgeWindowTests.BuildMatchesTheFixtureBitForBit already pins the whole window
        // bit-for-bit against a fixture; this assertion names the one number the defect is
        // about, and why: cellSize * mindk (the upper edge), not cellSize * (mindk - 1).
        Assert.Equal(cellSize * mindk, dpoc[0]);
        Assert.NotEqual(cellSize * (mindk - 1), dpoc[0]);
    }

    private static readonly double[] TruncatedFqks = [0.0, 0.4];
    private static readonly double[] TruncatedDpoc = [10.0, 20.0];

    [Fact]
    public void SamplePocketGuardsAnUnboundedSearch()
    {
        // A window truncated (by construction) below where the draw actually falls: the
        // original's DM search (Fortran lines 1573-1586) has no such guard and would read
        // past D()/Z() (impossible in Attempt.Run itself under AK3 > 0 and a QKS1 that sums
        // to its own AUS reciprocal, BOOT.md, "IndexOutOfRange"; this is the guard's own unit).
        var acc = _host.Accelerator;
        using var fqksBuffer = acc.Allocate1D(TruncatedFqks);
        using var dpocBuffer = acc.Allocate1D(TruncatedDpoc);

        var ok = BridgeWindow.SamplePocket(0.8, fqksBuffer.View.BaseView, dpocBuffer.View.BaseView, 2, out var dkarm);

        Assert.False(ok);
        Assert.Equal(0.0, dkarm);
    }
}
