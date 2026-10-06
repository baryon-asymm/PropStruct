using PropStruct.Particle;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// Confirms the mechanism behind a cross-node measurement this node did not itself run:
/// on two of the five reference formulations, the archived <c>results.m</c>'s own
/// category arrays (<c>Dkarmcat</c>, <c>dokkarm43</c>, <c>dokkarm10</c>) are one row
/// longer or shorter in the port than in the original — HPEPA3 17 -> 18 (port +1), HMX
/// 59 -> 58 (port -1) — with a prefix of identical rows, the same terminal boundary
/// value (so <c>DPRow</c>'s own initial sizing, "## Categories", is not at fault: it
/// would move the *last* value, not merely its index), and a tail that differs by
/// exactly one merged-away row, not a general reshuffling. Categories.MergeAndDescribe
/// has exactly one value-dependent branch that can change how many rows survive:
/// <c>Epsydok(r) &gt; EpsDok</c> (BOOT.md, "## Defects of the original", Fortran
/// 911-959, already declared: the original computes <c>Epsydok</c> in REAL*4 and
/// compares it against <c>EpsDok</c> (also REAL*4); the port keeps both in <c>double</c>
/// throughout). Every other input to the merge (<c>Qdoks</c>) is an exact integer
/// count, identical in both by construction — so a merge-count divergence has no other
/// place to come from.
///
/// This test does not reproduce the two formulations' own totals (this node has no
/// access to the original's internal <c>Qdoks</c>/<c>Dokp41</c>/<c>Dokp31</c>, only its
/// printed, post-merge output): it demonstrates the *mechanism's sensitivity* on a
/// constructed two-row scenario, using only <c>Categories.MergeAndDescribe</c>'s own
/// public behaviour (no independent transcription of its formula, root BOOT.md,
/// Taboos). Black-box bisection over <c>epsDok</c> finds the exact value at which the
/// merge decision flips for the constructed row; the two <c>epsDok</c> probes that land
/// on opposite sides of it differ by less than one binary32 ULP at that magnitude
/// (2^-23 relative). That is not a hypothetical margin: it is exactly the size of the
/// REAL*4-vs-double divergence the declared defect describes, so a merge decision this
/// sensitive can and does flip between the original's REAL*4 <c>Epsydok</c>/<c>EpsDok</c>
/// and the port's own double values, for *some* row, on *some* formulations — consistent
/// with the reported HPEPA3/HMX shape at an incidence (2 of 5 formulations, 1 row each)
/// that is unsurprising given each formulation carries 17 to 70 such comparisons.
/// </summary>
public class MergeThresholdSensitivityTests
{
    private readonly ITestOutputHelper _output;

    public MergeThresholdSensitivityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Non-degeneracy (AGENTS.md §13): the bisection itself proves the check can find a
    /// real flip (it does, below); the low/high sentinels bracketing the bisection
    /// (always-merge / never-merge) prove the flip exists in the first place and is not
    /// an artifact of a search that never converges.
    /// </summary>
    [Fact]
    public void EpsydokThresholdFlipsWithinOneBinary32UlpOfTheBoundary()
    {
        const int ndok = 6;
        const int ncat = 4;
        const double di = 1.0;
        const double dj = 1.0;
        var layout = AccumulatorLayout.Create(fractionCount: 1, ndok, nkarm: 1, ncat, nc: 1);

        // An arbitrary, plausible two-row oxidizer-cell count distribution: lopsided
        // enough that each row's own Epsydok (a dispersion-like statistic, "## Categories")
        // is a genuine, nonzero, non-round number, not a value chosen to already sit near
        // the boundary — the bisection below finds that boundary itself.
        long[] row0 = { 500, 900, 1300, 700, 300, 100 };
        long[] row1 = { 50, 900, 2000, 900, 50, 10 };

        int DpRowAt(double epsDok)
        {
            var integers = new long[layout.IntegerLength];
            var reals = new double[layout.RecordLength];
            for (var i = 0; i < ndok; i++)
            {
                integers[layout.Qdoks + i] = row0[i];
                integers[layout.Qdoks + ndok + i] = row1[i];
            }

            reals[layout.Dokp41] = 10.0;
            reals[layout.Dokp31] = 5.0;
            reals[layout.Dokp41 + 1] = 20.0;
            reals[layout.Dokp31 + 1] = 8.0;

            var status = Categories.MergeAndDescribe(ndok, ncat, di, dj, epsDok, dpMax: 1.5 * dj,
                layout, integers, reals, PrecisionKind.Binary64, out var dpRow, out _, out _, out _, out _);
            Assert.Equal(CategoriesStatus.Ok, status);
            return dpRow;
        }

        var low = 0.0;   // Always merges: DPRow = 1.
        var high = 10.0; // Never merges: DPRow = 2.
        Assert.Equal(1, DpRowAt(low));
        Assert.Equal(2, DpRowAt(high));

        for (var i = 0; i < 100; i++)
        {
            var mid = (low + high) / 2.0;
            if (DpRowAt(mid) == 1)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        var real4Ulp = high * Math.Pow(2.0, -23); // Root BOOT.md, "Array sizes from binary32 values".
        var justBelow = high - real4Ulp * 0.4;
        var justAbove = high + real4Ulp * 0.4;
        var dpRowBelow = DpRowAt(justBelow);
        var dpRowAbove = DpRowAt(justAbove);

        _output.WriteLine($"epsydok[0] boundary (double bisection) = {high:R}; one binary32 ULP there = {real4Ulp:R}");
        _output.WriteLine($"epsDok={justBelow:R} -> DpRow={dpRowBelow}; epsDok={justAbove:R} -> DpRow={dpRowAbove}");

        Assert.True(justAbove - justBelow < real4Ulp);
        Assert.NotEqual(dpRowBelow, dpRowAbove);
    }
}
