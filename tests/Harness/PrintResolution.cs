namespace PropStruct.Tests.Harness;

/// <summary>
/// The resolution a value prints at under an array's own format
/// (`tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #2). Split out of <c>StatisticalCriterion.IntervalRules.cs</c> (decomposition, 2026-09-26).
/// </summary>
internal static class PrintResolution
{
    // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #2: print resolution is a property of the array's own
    // field — one shared Fortran edit descriptor prints every cell of it, in every run of the program, since it
    // is the same `PRINT`/format statement each time — not of one specific cell's own printed token. `decimalDigits`
    // (the format's own fixed count of digits after the point, e.g. `3` for `E9.3`) is therefore read once from
    // any non-zero cell available (here, the reference's), and then applied to a *different* value's own
    // magnitude to get *that* value's own resolution — the same number the format would have printed for it, had
    // it printed it. `ExponentOf` mirrors `ResultsMFile.ResolutionOf`'s own convention exactly (Fortran's `E`
    // edit descriptor normalizes the mantissa to `0.d1d2...` in `[0.1, 1)`, so the exponent is the least integer
    // with `|value| < 10^exponent` — `floor(log10(|value|) + 1e-9) + 1`, the `1e-9` guard against a value one ULP
    // below an exact power of ten reading one decade low; an exact zero always prints as `0.000...E+00`, exponent
    // `0`) — this is the one place both directions of that relationship are needed, so it is written once here
    // rather than duplicated (root BOOT.md Taboos: no second implementation of a formula).
    //
    // ⚠ 2026-09-28 (F-c, `tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`): was
    // `ceil(log10(|value|) - 1e-9)`, one decade low at every exact power of ten (`0.100E+xx`'s own exponent):
    // `ceil` rounds an exact integer log down to itself, but the Fortran `E` format's own mantissa convention
    // puts `10^k` at `0.1 x 10^(k+1)`, exponent `k+1`, not `k`.
    internal static int ExponentOf(double value) => value == 0.0 ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(value)) + 1e-9) + 1;

    internal static double ResolutionFromDecimalDigits(double value, int decimalDigits) => Math.Pow(10, ExponentOf(value) - decimalDigits);

    internal static int? InferDecimalDigits(ResultCell[]? cells)
    {
        if (cells is null)
        {
            return null;
        }

        foreach (var cell in cells)
        {
            if (cell.Value == 0.0 || cell.Resolution <= 0.0)
            {
                continue;
            }

            return (int)Math.Round(ExponentOf(cell.Value) - Math.Log10(cell.Resolution));
        }

        return null;
    }

    // A synthetic `ResultCell` array for the candidate's own array: the candidate carries no print resolution of
    // its own through `Compare`'s public `double[]`-based signature, so each of its values is given the
    // resolution its own magnitude would print at, under the array's own format (`decimalDigits`, above) — never
    // borrowed by index from a specific reference cell, and so never undefined past the reference's own printed
    // length (`decimalDigits is null` only when the reference never printed this array at all).
    //
    // ⚠ 2026-09-19: this first borrowed the reference's own `Resolution` at the *same index* (`resolutionSource`),
    // giving `0.0` — treated as "always resolvable, validated at essentially zero tolerance" — for every
    // candidate index past the reference's own printed length. Under blind calibration a lagged replica standing
    // in as candidate can have a longer adaptive array than the official reference on any given day (measured:
    // HMX's reference `coef` is 844 cells against a real replica's own 852+), so this was common, not an edge
    // case; it also mismatched a within-length candidate whose own value differed enough in magnitude from the
    // reference's own at that index (a real, if rarer, latent defect the format-based fix removes too —
    // `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #2, has the full reasoning).
    internal static ResultCell[] BuildSyntheticCells(double[] values, int? decimalDigits)
    {
        var cells = new ResultCell[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var resolution = decimalDigits is { } digits ? ResolutionFromDecimalDigits(values[i], digits) : 0.0;
            cells[i] = new ResultCell(values[i], resolution, false);
        }

        return cells;
    }
}
