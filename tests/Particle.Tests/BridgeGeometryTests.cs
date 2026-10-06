using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L0 of the BOOT.md table: <see cref="BridgeGeometry.Volume"/> against the hand-derived
/// values of Fortran <c>VM</c> (<c>tests/Fixtures/cases/particle/bridge_geometry_volume.json</c>,
/// script <c>formulas_particle.py</c>), bit for bit — the normal path (with and without the
/// internal <c>r1</c>/<c>r2</c> swap), the <c>a &gt;= 2*rk</c> early return, and the
/// <c>bb &lt; 0</c> early return (lines 1612-1615) that no other test in this node reaches:
/// <c>Attempt.Run</c> cannot construct it (BOOT.md, "Attempt structure"'s bridge branch
/// always reaches a bridge with a positive gap under this node's preconditions), so the
/// fixture is the only way to exercise it against the real formula.
/// </summary>
public class BridgeGeometryTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var testCase in FormulaCaseFiles.ReadBridgeGeometryCases())
        {
            yield return new object[] { testCase.Name };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void VolumeMatchesTheFixtureBitForBit(string name)
    {
        var testCase = FormulaCaseFiles.FindBridgeGeometryCase(name);
        var (r1, r2, rk, a, expected) = (testCase.R1, testCase.R2, testCase.Rk, testCase.A, testCase.Expected);

        var jj = BridgeGeometry.Volume(r1, r2, rk, a, out var volume, out var bb);

        Assert.True(jj == (expected.Jj != 0), $"{name}: expected jj={expected.Jj}, got {jj}");

        if (expected.Bb.HasValue)
        {
            Assert.True(
                BitConverter.DoubleToInt64Bits(expected.Bb.Value) == BitConverter.DoubleToInt64Bits(bb),
                $"{name}: expected bb={expected.Bb.Value}, got {bb}");
        }
        else
        {
            // Not computed on this early return (a >= 2*rk): BridgeGeometry.Volume's own
            // documented contract leaves it at 0 (BridgeGeometry.cs, Volume's doc comment),
            // a code contract this test checks, not a value read from the fixture.
            Assert.Equal(0.0, bb);
        }

        if (expected.Vmkm.HasValue)
        {
            Assert.True(
                BitConverter.DoubleToInt64Bits(expected.Vmkm.Value) == BitConverter.DoubleToInt64Bits(volume),
                $"{name}: expected vmkm={expected.Vmkm.Value}, got {volume}");
        }
        else
        {
            Assert.Equal(0.0, volume);
        }
    }
}
