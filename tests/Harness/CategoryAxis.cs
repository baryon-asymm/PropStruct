namespace PropStruct.Tests.Harness;

/// <summary>
/// The fixed-width prefix and each source's match length against the reference's own <c>Dkarmcat</c> (this
/// node's BOOT.md, ## Invariants, "Canonical category axis"). Split out of
/// <c>StatisticalCriterion.Comparisons.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class CategoryAxis
{
    // `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 3: "i0 = the first row past the bit-exact
    // fixed-width prefix". Returns the 0-based index of the first row that breaks the "exact multiple of the
    // first entry" pattern (Dkarmcat's own first entry is the bin width itself), i.e. the count of fixed-width
    // rows -- each run's own count, not shared bit-for-bit across a formulation's runs (F-b,
    // `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`: a run's own prefix differs from the reference's
    // own in 78 of the 197 null runs). `TailRowMeanComparison`/`StatisticalCriterion.CompareAdaptiveIndexMatched`
    // call this on the reference's own Dkarmcat only, the same source `ReferenceComparison` already treats as
    // canonical, and use that one value as `i0` for every source alike — the reference's own prefix length, one
    // fixed cut for every source, never each source's own.
    internal static int FixedWidthPrefixLength(double[] dkarmcat)
    {
        if (dkarmcat.Length == 0)
        {
            return 0;
        }

        var step = dkarmcat[0];
        var i = 0;
        while (i < dkarmcat.Length && Math.Abs(dkarmcat[i] - step * (i + 1)) <= step * 1e-9)
        {
            i++;
        }

        return i;
    }

    // The candidate's own and every replica's own match length against the reference's `Dkarmcat` prefix (this
    // node's BOOT.md, ## Invariants, "Canonical category axis") — a pure grouping of two local variables that
    // used to sit at the top of `BuildComparePending`, not a new contract.
    internal sealed record DkarmcatPrefixMatches(int CandidateMatch, int[] ReplicaMatch);

    internal static DkarmcatPrefixMatches ComputeDkarmcatPrefixMatches(
        IReadOnlyDictionary<string, ResultCell[]> referenceCells, IReadOnlyDictionary<string, double[]> candidate,
        List<IReadOnlyDictionary<string, ResultCell[]>> replicaCells)
    {
        // Canonical category axis (this node's BOOT.md, ## Invariants, "Canonical category axis"): a source
        // contributes to canonical row/index `i` of Dkarmcat/dokkarm43/dokkarm10/fqdokkarm(<row>,:) only as far
        // as its own Dkarmcat agrees with the reference's, entry for entry from the start. Past the first entry
        // where a source's Dkarmcat diverges from the reference's, row `i` of that source is a different physical
        // category, not merely a longer or shorter array, so it stops contributing from there on, not just at the
        // point it runs out (F-b, `tests/Harness/HISTORY.md#fixed-width-prefix-not-shared`: a source's own
        // fixed-width prefix is not the formulation's, so this match length is measured per source, never assumed).
        var referenceDkarmcat = referenceCells.TryGetValue("Dkarmcat", out var referenceDkarmcatCells)
            ? Array.ConvertAll(referenceDkarmcatCells, c => c.Value)
            : [];
        var candidateDkarmcatMatch = PrefixMatchLength(candidate.GetValueOrDefault("Dkarmcat"), referenceDkarmcat);
        var replicaDkarmcatMatch = new int[replicaCells.Count];
        for (var r = 0; r < replicaCells.Count; r++)
        {
            var replicaDkarmcat = replicaCells[r].TryGetValue("Dkarmcat", out var replicaDkarmcatCells)
                ? Array.ConvertAll(replicaDkarmcatCells, c => c.Value)
                : [];
            replicaDkarmcatMatch[r] = PrefixMatchLength(replicaDkarmcat, referenceDkarmcat);
        }

        return new DkarmcatPrefixMatches(candidateDkarmcatMatch, replicaDkarmcatMatch);
    }

    // this node's BOOT.md, "## Set comparison": each of `runs`'s own match length against the reference's own
    // Dkarmcat prefix, reusing the same private `PrefixMatchLength` every other caller already shares (root
    // BOOT.md Taboos: no second implementation of a formula) — the plain `double[]`-keyed shape `CompareSets`'s
    // own candidates and replicas already have, unlike `ComputeDkarmcatPrefixMatches`'s `ResultCell[]`-keyed one.
    internal static int[] PrefixMatchLengths(double[] referenceDkarmcat, IReadOnlyList<IReadOnlyDictionary<string, double[]>> runs)
    {
        var matches = new int[runs.Count];
        for (var i = 0; i < runs.Count; i++)
        {
            matches[i] = PrefixMatchLength(runs[i].GetValueOrDefault("Dkarmcat"), referenceDkarmcat);
        }

        return matches;
    }

    // The number of leading entries where `source` equals `reference` exactly: the two are independent re-parses
    // of the same fixed-format decimal token whenever they name the same physical category (this node's BOOT.md,
    // ## Invariants, "Canonical category axis"), so an exact `double` comparison is correct here, not a source of
    // floating-point noise (contrast the "Print-resolution floating-point guard", which compares candidate
    // and replica *values*, not two parses of the same fixed grid).
    private static int PrefixMatchLength(double[]? source, double[] reference)
    {
        if (source is null)
        {
            return 0;
        }

        var limit = Math.Min(source.Length, reference.Length);
        var i = 0;
        while (i < limit && source[i] == reference[i])
        {
            i++;
        }

        return i;
    }
}
