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

    // The geometry of the BridgeFrustum cases (ACCEPTANCE.md): the larger sphere's radius, the
    // pocket's radius and the gap, metres; R2/R1 is the case. For these three, `bb` is positive
    // at all three ratios, so the volume is computed rather than the JJ = 0 return taken.
    private const double R1 = 5.0e-5;
    private const double Rk = 0.4 * R1;
    private const double A = 0.1 * R1;

    private static readonly double[] Ratios = [0.6, 0.998, 1.0];

    private const int SimpsonIntervals = 4000;

    /// <summary>
    /// <c>BridgeFrustum</c>, right: the volume under the flag equals an independent numerical
    /// quadrature of the same solid, within 1e-9 relative, at <c>R2/R1</c> of 0.6, 0.998 and 1.
    /// The solid is the frustum between the two chord circles less the two caps; the quadrature
    /// integrates its cross-section (an area, <c>π·ρ(z)²</c> less the caps' own discs) along the
    /// axis of the two spheres, with the chord circles placed from the coordinates of the
    /// triangle of the three centres, so it shares no formula with <see cref="BridgeGeometry"/>.
    /// </summary>
    [Theory]
    [InlineData(0.6)]
    [InlineData(0.998)]
    [InlineData(1.0)]
    public void BridgeFrustumMatchesAnIndependentQuadratureOfTheSolid(double ratio)
    {
        var quadrature = QuadratureOfTheBridge(R1, ratio * R1, Rk, A);

        var jj = BridgeGeometry.Volume(R1, ratio * R1, Rk, A, true, out var volume, out _);

        Assert.True(jj);
        Assert.True(quadrature > 0.0, $"ratio {ratio}: the quadrature is {quadrature}.");
        Assert.True(
            Math.Abs(volume - quadrature) <= 1e-9 * quadrature,
            $"ratio {ratio}: volume {volume} against the quadrature {quadrature}.");
    }

    /// <summary>
    /// <c>BridgeFrustum</c>, red: the original's difference of cones has a negative volume at
    /// <c>R2 = R1</c> and is more than ten times short of the same quadrature at
    /// <c>R2/R1 = 0.998</c> (the old signature and the flag unset are the same call).
    /// </summary>
    [Fact]
    public void TheOriginalVolumeIsNegativeAtEqualRadiiAndMoreThanTenTimesShortNearThem()
    {
        Assert.True(BridgeGeometry.Volume(R1, R1, Rk, A, false, out var atEqualRadii, out _));
        Assert.True(atEqualRadii < 0.0, $"R2 = R1: the original volume is {atEqualRadii}.");

        var nearEqual = 0.998 * R1;
        Assert.True(BridgeGeometry.Volume(R1, nearEqual, Rk, A, out var original, out _));
        var quadrature = QuadratureOfTheBridge(R1, nearEqual, Rk, A);
        Assert.True(
            original * 10.0 < quadrature,
            $"R2/R1 = 0.998: the original volume {original} against the quadrature {quadrature}.");
    }

    /// <summary>
    /// <c>JJ</c> and <c>bb</c> are bit-equal with the flag set and unset on every case, the two
    /// <c>JJ = 0</c> returns included: every case of <c>bridge_geometry_volume.json</c> and the
    /// three radius ratios above.
    /// </summary>
    [Fact]
    public void JjAndBbAreBitEqualUnderBothVolumesOnEveryCase()
    {
        var cases = FormulaCaseFiles.ReadBridgeGeometryCases()
            .Select(c => (c.R1, c.R2, c.Rk, c.A))
            .Concat(Ratios.Select(ratio => (R1, ratio * R1, Rk, A)))
            .ToArray();

        Assert.Contains(cases, c => c.A >= 2.0 * c.Rk);
        foreach (var (r1, r2, rk, a) in cases)
        {
            var jjOriginal = BridgeGeometry.Volume(r1, r2, rk, a, false, out _, out var bbOriginal);
            var jjFrustum = BridgeGeometry.Volume(r1, r2, rk, a, true, out _, out var bbFrustum);

            Assert.Equal(jjOriginal, jjFrustum);
            Assert.Equal(BitConverter.DoubleToInt64Bits(bbOriginal), BitConverter.DoubleToInt64Bits(bbFrustum));
        }
    }

    /// <summary>
    /// The unset flag is the original, bit for bit: every case of the fixture, through the new
    /// signature.
    /// </summary>
    [Fact]
    public void TheFlagUnsetIsTheOldSignatureBitForBit()
    {
        foreach (var c in FormulaCaseFiles.ReadBridgeGeometryCases())
        {
            var jjOld = BridgeGeometry.Volume(c.R1, c.R2, c.Rk, c.A, out var volumeOld, out var bbOld);
            var jjNew = BridgeGeometry.Volume(c.R1, c.R2, c.Rk, c.A, false, out var volumeNew, out var bbNew);

            Assert.Equal(jjOld, jjNew);
            Assert.Equal(BitConverter.DoubleToInt64Bits(volumeOld), BitConverter.DoubleToInt64Bits(volumeNew));
            Assert.Equal(BitConverter.DoubleToInt64Bits(bbOld), BitConverter.DoubleToInt64Bits(bbNew));
        }
    }

    /// <summary>
    /// The volume of the bridge between spheres of radii <paramref name="r1"/> &gt;= <paramref name="r2"/>
    /// a gap <paramref name="a"/> apart, with a tangent pocket sphere of radius <paramref name="rk"/>:
    /// the frustum between the two circles where the pocket touches the spheres, less the two
    /// spherical caps, by composite Simpson quadrature of the cross-section along the axis.
    /// </summary>
    private static double QuadratureOfTheBridge(double r1, double r2, double rk, double a)
    {
        // Centres on the axis: A at 0, C at ac; the pocket's centre B at (bx, by) from the triangle.
        var ab = r1 + rk;
        var bc = r2 + rk;
        var ac = r1 + r2 + a;
        var bx = (ab * ab + ac * ac - bc * bc) / (2.0 * ac);
        var by = Math.Sqrt(ab * ab - bx * bx);

        // The circle where the pocket touches sphere 1 (the point of AB at distance r1 from A) and
        // sphere 2 (the point of CB at distance r2 from C): axial position and radius.
        var z1 = r1 * bx / ab;
        var rho1 = r1 * by / ab;
        var z2 = ac - r2 * (ac - bx) / bc;
        var rho2 = r2 * by / bc;

        double CrossSection(double z)
        {
            var rho = rho1 + (rho2 - rho1) * (z - z1) / (z2 - z1);
            var area = rho * rho;
            area -= z <= r1 ? r1 * r1 - z * z : 0.0;
            area -= z >= ac - r2 ? r2 * r2 - (ac - z) * (ac - z) : 0.0;
            return Math.PI * area;
        }

        // The caps start at r1 and at ac - r2, where the integrand is not smooth: integrate the
        // three pieces apart.
        return Simpson(CrossSection, z1, r1) + Simpson(CrossSection, r1, ac - r2) + Simpson(CrossSection, ac - r2, z2);
    }

    private static double Simpson(Func<double, double> f, double lower, double upper)
    {
        var h = (upper - lower) / SimpsonIntervals;
        var sum = f(lower) + f(upper);
        for (var i = 1; i < SimpsonIntervals; i++)
        {
            sum += (i % 2 == 0 ? 2.0 : 4.0) * f(lower + i * h);
        }

        return sum * h / 3.0;
    }
}
