using System.Globalization;
using System.Text.RegularExpressions;

namespace PropStruct.Tests.Harness;

/// <summary>
/// Which printed family a quantity name belongs to (this node's BOOT.md, ## Invariants, "Count-like cells" and
/// "Canonical category axis"). Split out of <c>StatisticalCriterion.Comparisons.cs</c> (decomposition,
/// 2026-09-26): a lookup shared by every comparison, not one comparison's own cell-building.
/// </summary>
internal static class QuantityFamilies
{
    // this node's BOOT.md, ## Invariants, "Count-like cells": the original's own printed *number* (count)
    // density/distribution families — never the *mass* density ones, which sum a continuous quantity (particle
    // or pocket volume) per bin, not a count, and hold no integer quantum at all. The two are told apart by
    // their own comment header, read from the reference file this node already parses (never the Fortran
    // source, root BOOT.md Taboos): every array whose header says "**Mass** density distribution function ..."
    // (`fmdok`, `fmkarm`, `fmkarm_cor`, `fmkarm_cor2`) is excluded; every array whose header says "**Numeric**
    // density distribution function[s] ..." (`fqkarm`, `fqkarm_cor`, `fqdokkarm`'s own row family) or a plain
    // "... distribution (step = ...)" with no mass/numeric qualifier at all — `fqmkm1`, `fqmkm2` ("Density
    // distribution function of MKM/Dok ...", no mass-weighted counterpart exists to need the qualifier) and
    // `coef` ("Coefficient [(Lij/Ddok)+1] distribution") — is kept. fqdokkarm's row family is handled by name
    // pattern below: it is both a canonical category axis (## Invariants, "Canonical category axis") and a
    // distribution function.
    //
    // ⚠ 2026-09-19: this set first also held `fmdok`/`fmkarm`/`fmkarm_cor`/`fmkarm_cor2`. HMX's own reference
    // failed gate 1 on `fmkarm_cor2[84]` under every quantum estimator tried (the sibling pool and the first,
    // "smallest cell is one count" per-run form): `tests/Harness/HISTORY.md#per-run-quantum`'s own worked example
    // shows the array's own smallest non-zero cell (`fmkarm_cor2[2] = 2.19e-08`) is not a divisor of its very
    // next cell (`fmkarm_cor2[3] = 8.19e-07`, ratio 37.397, nowhere near an integer) — not a bug in the
    // estimator, since `fmkarm_cor2`'s own header reads "**Mass** density distribution function", i.e. its
    // cells sum particle/pocket *volumes*, a continuous quantity with no shared step at all. Removing the four
    // `fm*` families from this set (root cause, not the estimator) is the fix; `fqdokkarm(31,:)[7]`, the row
    // family's own gate-1 failure, is a real numeric-density (count) cell and needed the refined estimator
    // below instead (same section, "Implementation and measurements").
    private static readonly HashSet<string> DistributionFunctionFamilies = new(StringComparer.Ordinal)
    {
        "fqkarm", "fqkarm_cor", "fqmkm1", "fqmkm2", "coef",
    };

    private static readonly Regex FqDokKarmRow = new(@"^fqdokkarm\((\d+),:\)$", RegexOptions.Compiled);

    // this node's BOOT.md, ## Invariants, "Canonical category axis": Dkarmcat/dokkarm43/dokkarm10 are single
    // arrays whose *length* is chosen per run from the sizes that run actually produced.
    internal static readonly HashSet<string> CategoryLengthVaryingArrays = new(StringComparer.Ordinal)
    {
        "Dkarmcat", "dokkarm43", "dokkarm10",
    };

    // `tests/Harness/HISTORY.md#decision-vi-full-reasoning`: the same test that decides
    // `isDistributionFunction` at construction time, reused — not duplicated — for the calibration
    // curve's own eligibility test, where a cell's *name* is all that survives onto `CellVerdict`. This
    // identifies exactly the families that are a normalized fraction of a distribution, each cell of which is
    // therefore bounded to `[0, 1]` by construction (its own header: a "quantity fraction", cells summing to `1`
    // over the family's own axis with no step multiplier, unlike the `fm*` mass-density siblings, which integrate
    // to `1` only after multiplying by their own step). `AdaptiveRowCount`/`TailRowMean`/`{name}@adaptive` are
    // marked `isDistributionFunction: true` at their own call sites for a different reason (to get the same
    // count-like predictive treatment) and are deliberately excluded here: a row count or a tail-row mean has no
    // `[0, 1]` bound at all.
    internal static bool IsFractionFamily(string name) => DistributionFunctionFamilies.Contains(name) || TryMatchFqDokKarmRow(name, out _);

    // `tests/Harness/HISTORY.md#tail-coverage-2026-09-18`, step 3, and this node's BOOT.md, "Canonical category axis": the one place
    // the `fqdokkarm(<row>,:)` name pattern is matched and its 1-based row parsed, so every caller reads the row
    // through this method rather than repeating the regex or the parse (root BOOT.md Taboos: no second
    // implementation of a formula).
    internal static bool TryMatchFqDokKarmRow(string name, out int row)
    {
        var match = FqDokKarmRow.Match(name);
        if (!match.Success)
        {
            row = 0;
            return false;
        }

        row = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return true;
    }
}
