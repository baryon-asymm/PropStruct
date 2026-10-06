using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// L1 of the BOOT.md table: <c>SmallParticles.Probability</c> and
/// <c>SmallParticles.MaxSize</c> against <c>tests/Fixtures/formulas_statistics.py</c>'s
/// independent transcription of Fortran lines 1021-1074, the two methods whose outputs
/// (<c>pdoksmall</c>, <c>Dmaxxx</c>) feed the next cycle's model loop (BOOT.md, "##
/// Report": "Next-cycle inputs"). Added by the audit of this node: the narrowing ⚠ of
/// the BOOT.md acceptance criteria had called these two methods "short, branch-free
/// accumulation formulas" needing no independent check, which their own branches
/// (the <c>zdoksmall &gt; 1e-5</c> guard, the monotone clamp, the early-exit threshold)
/// contradict.
/// </summary>
public class SmallParticlesTests
{
    public static IEnumerable<object[]> ProbabilityCases()
    {
        foreach (var c in FormulaCaseFiles.ReadSmallParticlesProbabilityCases())
        {
            yield return new object[] { c.Name, nameof(PrecisionKind.Binary64) };
            yield return new object[] { c.Name, nameof(PrecisionKind.Original) };
        }
    }

    [Theory]
    [MemberData(nameof(ProbabilityCases))]
    public void ProbabilityMatchesTheFormulaScript(string caseName, string kindName)
    {
        var c = FormulaCaseFiles.FindSmallParticlesProbabilityCase(caseName);
        var kind = Enum.Parse<PrecisionKind>(kindName);
        var expected = kind == PrecisionKind.Original ? c.ExpectedOriginal : c.Expected;

        var pdoksmall = SmallParticles.Probability(
            c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(), kind);

        Assert.Equal(expected.Pdoksmall, pdoksmall);
    }

    public static IEnumerable<object[]> MaxSizeCases()
    {
        foreach (var c in FormulaCaseFiles.ReadSmallParticlesMaxSizeCases())
        {
            yield return new object[] { c.Name, nameof(PrecisionKind.Binary64) };
            yield return new object[] { c.Name, nameof(PrecisionKind.Original) };
        }
    }

    [Theory]
    [MemberData(nameof(MaxSizeCases))]
    public void MaxSizeMatchesTheFormulaScript(string caseName, string kindName)
    {
        var c = FormulaCaseFiles.FindSmallParticlesMaxSizeCase(caseName);
        var kind = Enum.Parse<PrecisionKind>(kindName);
        var expected = kind == PrecisionKind.Original ? c.ExpectedOriginal : c.Expected;

        var dmaxxx = SmallParticles.MaxSize(c.Ndok, c.CellSize, c.Ddokmax, c.Karmcoef, c.Mkmcoef, c.Pdoksmall.ToArray(), kind);

        Assert.Equal(expected.Dmaxxx, dmaxxx);
    }

    /// <summary>
    /// Mutation proof (AGENTS.md §13): replacing <c>SmallParticles.cs</c>'s clamp
    /// condition <c>if (pdoksmall[k] &lt; pdoksmall[k - 1])</c> with <c>if (true)</c>
    /// (forcing the clamp to fire on every cell, cascading <c>pdoksmall[0] = 0</c>
    /// forward into every later cell) failed two of the five
    /// <c>SmallParticlesTests</c> cases; reverted, green again (recorded in this node's
    /// BOOT.md, "## Mutations").
    /// </summary>
    [Fact]
    public void ProbabilityClampsANonMonotoneTail()
    {
        var c = FormulaCaseFiles.ReadSmallParticlesProbabilityCases().Single(x => x.Name == "concentrated_source_clamps_the_tail");
        var pdoksmall = SmallParticles.Probability(
            c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(), PrecisionKind.Binary64);

        // Every cell from index 1 on is the same clamped value: the raw computation at
        // index 3 would be lower (the fixture case is engineered so it is), so a value
        // this uniform is itself evidence the clamp fired, not a coincidence of the
        // formula.
        Assert.Equal(pdoksmall[1], pdoksmall[2]);
        Assert.Equal(pdoksmall[2], pdoksmall[3]);
    }

    /// <summary>
    /// Mutation proof (AGENTS.md §13): removing <c>SmallParticles.cs</c>'s
    /// <c>break;</c> after the threshold is first reached (letting the loop keep
    /// overwriting <c>dmaxxx</c> at every later <c>kilo</c> that also clears the
    /// threshold, instead of stopping at the first) changed
    /// "threshold_reached_at_kilo_two"'s own <c>Dmaxxx</c> from <c>2.0</c> (the first
    /// crossing) to <c>4.0</c> (the last), failing
    /// <see cref="MaxSizeMatchesTheFormulaScript"/> for that case; reverted, green
    /// again. This assertion is the same one, restated as the specific first-crossing
    /// claim so a future reader sees the property this case exists to protect without
    /// re-deriving it from the raw numbers.
    /// </summary>
    [Fact]
    public void MaxSizeStopsAtTheFirstCrossing()
    {
        var c = FormulaCaseFiles.ReadSmallParticlesMaxSizeCases().Single(x => x.Name == "threshold_reached_at_kilo_two");
        var dmaxxx = SmallParticles.MaxSize(c.Ndok, c.CellSize, c.Ddokmax, c.Karmcoef, c.Mkmcoef, c.Pdoksmall.ToArray(), PrecisionKind.Binary64);

        Assert.Equal(c.CellSize * 2, dmaxxx);
    }

    /// <summary>
    /// The whole-fraction count <c>int(real(kilo)/ak2)</c> uses the same binary32
    /// arithmetic <c>Setup.Sizes</c> already uses for <c>Ndok</c> (root BOOT.md,
    /// "Double precision only", the exception extended to this count), not a plain
    /// <c>double</c> division of the loop's own integer <c>kilo</c> by <c>ak2</c>:
    /// the "whole_fraction_count_binary32_boundary" case's own <c>ak2</c> is one double
    /// ULP above <c>2.0</c> (the kind of noise a parsed coefficient can carry) — its raw
    /// double quotient with <c>kilo = 2</c> is just under 1.0 (truncating to 0), but
    /// <c>ak2</c> itself rounds to exactly binary32 <c>2.0</c> (its own REAL*4 storage in
    /// the original), giving <c>2 / 2.0 = 1.0</c> exactly (truncating to 1) — the same
    /// double-rounding shape as the C166 evidence for <c>Ndok</c>
    /// (<c>Binary32Tests.TruncatedQuotientAvoidsDoubleRoundingAtAnIntegerBoundary</c>).
    /// <c>tests/Fixtures/formulas_statistics.py</c>'s own transcription applies the same
    /// binary32 rounding independently, so this assertion is
    /// <see cref="ProbabilityMatchesTheFormulaScript"/> for that one case, restated to
    /// name the property it protects. Mutation proof (AGENTS.md §13): reverting
    /// <c>SmallParticles.cs</c>'s <c>Binary32.TruncatedQuotient</c> call to
    /// <c>(int)(kilo / ak2)</c> made this case fail (<c>pdoksmall[1]</c> came back from
    /// the wholeFractions = 0 branch instead of 1); reverted, green again (recorded in
    /// this node's BOOT.md, "## Mutations").
    /// </summary>
    [Fact]
    public void ProbabilityUsesBinary32ArithmeticForTheWholeFractionCount()
    {
        var c = FormulaCaseFiles.ReadSmallParticlesProbabilityCases().Single(x => x.Name == "whole_fraction_count_binary32_boundary");
        var pdoksmall = SmallParticles.Probability(
            c.Ndok, c.CellSize, c.Ak2, c.Ggg, c.Gdokleft, c.Karmcoef, c.OxidizerDensity, c.PropellantDensity, c.Vdokstr.ToArray(), PrecisionKind.Binary64);

        Assert.Equal(c.Expected.Pdoksmall[1], pdoksmall[1]);
    }
}
