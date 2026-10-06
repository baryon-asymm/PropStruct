using System.Reflection;
using PropStruct.Tests.Harness;
using Xunit;

namespace PropStruct.Particle.Tests;

/// <summary>
/// L0's layout row: for each of the five reference formulations,
/// <see cref="AccumulatorLayout"/>'s offsets and lengths as <see cref="ReferenceFormulation.Prepare"/>
/// (through <c>Statistics.Setup.Prepare</c>) actually builds them.
///
/// Two different kinds of evidence, not one, because only one of the three sizes
/// <see cref="AccumulatorLayout.Create"/> takes has an untrimmed printed array to
/// check against:
///
/// - <c>Ndok</c> against the original: <c>fqdokkarm(&lt;row&gt;,:)</c> is
///   <c>QDOKSO</c>'s own row, printed in full for every column (`src/Statistics/BOOT.md`,
///   "## Report": <c>QDOKSO[DPRow × Ndok]</c>), so its length is <c>Ndok</c> exactly —
///   the same pattern `src/Statistics/BOOT.md`'s own acceptance criteria already used
///   for the archived formulations (`fqdokkarm(1,:)` against C166/T56/CSPX01/P18050/PSAN01).
/// - <c>Nkarm</c> and <c>Ncat</c> have no such array: every printed distribution of
///   length <c>Nkarm</c> (`fmkarm`, `fqkarm`) or bounded by <c>Ncat</c> (the category
///   rows) is trimmed to a data-dependent extent (`*_nmax + 2`, or `DPRow ≤ Ncat`) that
///   `src/Statistics/BOOT.md`'s own acceptance criteria already found gives "no clean
///   archived length" for either — confirmed again here:
///   `fmkarm`'s and `fqkarm`'s own printed lengths (67/48/55/36/127 for the five
///   formulations) do not equal <see cref="ModelSetup.Nkarm"/> as `Setup.Prepare` itself
///   computes it (verified once, by hand, while writing this row, not asserted below —
///   asserting a coincidence would misstate what the row actually proves). What is
///   checked instead is that the layout <c>Setup.Prepare</c> hands back was actually
///   built from the sizes it also hands back: a wiring check between two things one
///   call returns, not a claim about the original.
/// </summary>
public class LayoutTests : IClassFixture<CpuHost>
{
    private readonly CpuHost _host;

    public LayoutTests(CpuHost host)
    {
        _host = host;
    }

    public static IEnumerable<object[]> Formulations()
    {
        foreach (var name in ReferenceFormulation.Names)
        {
            yield return new object[] { name };
        }
    }

    [Theory]
    [MemberData(nameof(Formulations))]
    public void NdokMatchesTheFqdokkarmRowLengthOfItsReferenceOutput(string formulation)
    {
        var setup = ReferenceFormulation.Prepare(_host.Accelerator, formulation, out _, out _);

        var resultsPath = RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt");
        var results = ResultsMFile.Parse(resultsPath);
        var firstCategoryRow = results["fqdokkarm(1,:)"];

        Assert.True(
            firstCategoryRow.Length == setup.Ndok,
            $"{formulation}: fqdokkarm(1,:) has {firstCategoryRow.Length} cells, Setup.Prepare computed Ndok = {setup.Ndok}");
    }

    /// <summary>
    /// <c>Nkarm</c> and <c>Ncat</c> have no independently printed, untrimmed array to
    /// check against (this class's own doc comment); what is checked instead is that
    /// <see cref="ModelSetup.Layout"/> is exactly <see cref="AccumulatorLayout.Create"/>
    /// applied to the very sizes <see cref="ModelSetup"/> also reports
    /// (<see cref="ModelSetup.FractionCount"/>, <see cref="ModelSetup.Ndok"/>,
    /// <see cref="ModelSetup.Nkarm"/>, <see cref="ModelSetup.Ncat"/>,
    /// <see cref="ModelSetup.Nc"/>) — a consistency check between two things one call
    /// of <c>Setup.Prepare</c> returns, not a check against the original. Every field of
    /// <see cref="AccumulatorLayout"/> is compared by reflection, not by naming a
    /// hand-picked subset, so a field this row's author forgot cannot pass silently
    /// (AGENTS.md §6, the quantifier "every").
    /// </summary>
    [Theory]
    [MemberData(nameof(Formulations))]
    public void LayoutIsBuiltFromTheSetupsOwnSizes(string formulation)
    {
        var setup = ReferenceFormulation.Prepare(_host.Accelerator, formulation, out _, out _);

        var expected = AccumulatorLayout.Create(setup.FractionCount, setup.Ndok, setup.Nkarm, setup.Ncat, setup.Nc);

        foreach (var field in typeof(AccumulatorLayout).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var expectedValue = field.GetValue(expected);
            var actualValue = field.GetValue(setup.Layout);
            Assert.True(
                Equals(expectedValue, actualValue),
                $"{formulation}: AccumulatorLayout.{field.Name} = {actualValue}, expected {expectedValue} " +
                $"(from AccumulatorLayout.Create({setup.FractionCount}, {setup.Ndok}, {setup.Nkarm}, {setup.Ncat}, {setup.Nc}))");
        }
    }
}
