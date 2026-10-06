using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Tests.Harness.Tests;

/// <summary>
/// tests/Harness/BOOT.md, "Oracle mutation" (added for the wave-8 assignment, after two "tests green, property
/// false" defects elsewhere in the tree — an engine that marked a cached histogram fresh without recomputing it,
/// and a counter check whose absolute floor of 1.0 swallowed a factor-2 error on rates of order 0.01). For each
/// comparison rule <see cref="StatisticalCriterion"/> exposes, this class perturbs one real, observed cell of a
/// real reference formulation away from its own passing value and searches, live, against the rule itself — never
/// a second, independently typed copy of its threshold formula (root BOOT.md Taboos, "no second implementation of
/// any part of ... a formula"; this node's BOOT.md, "Finalize", says the same about the threshold formula
/// specifically) — for the boundary between "compares clean" and "fails". <see cref="FindBoundary"/> is the one
/// search this whole class shares, so a rule whose own band turns out to be effectively unreachable for some
/// class of cell shows up as `Boundary.Found == false` at every call site, not as a silently vacuous test.
///
/// Eight rules, one representative real cell each, run on both HPEPA3 and HMX (root BOOT.md's own two
/// formulations with the deepest, most varied printed output): the plain Student band (<c>Dkarm10</c>), the
/// count-like negative-binomial predictive interval (<c>fmkarm</c>), the Poisson-like count floor on a
/// deterministic integer cell (<c>Nbase</c>), the print-resolution floor on a deterministic non-integer cell
/// (<c>Gfr</c>), the canonical-axis branch of <see cref="StatisticalCriterion.Compare"/> (<c>dokkarm43</c>), the
/// derived tail-row mean (<see cref="StatisticalCriterion.CompareTailRowMean"/>), the adaptive-index-matched
/// boundary (<see cref="StatisticalCriterion.CompareAdaptiveIndexMatched"/>), and the two-sample bias's own
/// boundary (<see cref="StatisticalCriterion.TwoSampleBiasOfSets"/>, the internal seam this class needed because
/// the public <see cref="StatisticalCriterion.TwoSampleBias"/> loads both of its replica sets itself and has no
/// candidate to inject — this node's BOOT.md, "Oracle mutation").
///
/// Two more tests found the exercise's own findings (this node's BOOT.md, "Oracle mutation"): a
/// distribution-function cell that every replica of a formulation prints as exactly zero, and
/// <see cref="StatisticalCriterion.CompareTailRowMean"/>'s own derived quantity at one column, each had a band
/// that grew exactly as fast as the candidate perturbation itself, so no finite perturbation, of any magnitude,
/// ever failed them.
///
/// ⚠ 2026-09-19: both findings were closed by "The candidate is never its own witness (2026-09-19)" (the same
/// BOOT.md section): the count-like quantum is now inferred from the replicas alone, never the candidate, and
/// `CompareTailRowMean`'s own cells are no longer treated as count-like at all. `CountPredictiveInterval_
/// AllZeroReplicaCellBoundaryIsRealAfterTheQuantumFix` and (at the time) `TailRowMean_ColumnZero_
/// BoundaryIsRealAfterTheCountFloorFix` (renamed from `...NeverFailsAtAnyFiniteMagnitude`) asserted the real,
/// finite boundary each fix produced, instead of the absence of one.
///
/// ⚠ 2026-09-19, later the same day: "Three decisions (2026-09-19)" (same BOOT.md) added a sparse-cell exclusion
/// — a cell with fewer than two non-zero replicas carries no scale to judge a candidate against and is excluded,
/// not compared — and wired it into `CompareTailRowMean` too. `TailRowMean[0]`, for both formulations, turned
/// out to be exactly such a cell (every adaptive row of every lagged replica reads `0.0` there), so it reverted
/// to never failing at any magnitude — this time for the correct reason (explicitly excluded as scaleless, not a
/// count-like formula silently escaping its own floor). `TailRowMean_ColumnZero_
/// ExcludedAsScalelessUnderTheSparseCellRule` (renamed again) asserts this directly, including that the pool
/// really is all-zero, so a future regression that stops excluding it — without giving it a real floor instead —
/// is caught here rather than reading as coincidental non-degeneracy.
/// </summary>
public class OracleMutationTests
{
    private readonly ITestOutputHelper _output;

    public OracleMutationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> ReferenceFormulations()
    {
        yield return ["HPEPA3"];
        yield return ["HMX"];
    }

    private static Dictionary<string, double[]> CloneReference(string formulation) =>
        new(ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "references", formulation, "results.m.txt")));

    // ---- The one boundary search every rule below shares -------------------------------------------------

    /// <summary><paramref name="Found"/> is false when the search's own doubling cap was reached without the
    /// oracle ever turning red: the reportable "no finite perturbation" case (this class's own summary), not a
    /// thrown exception or a silently empty test. <paramref name="JustInsideDelta"/> and
    /// <paramref name="JustOutsideDelta"/> bracket the boundary the live bisection found, in the same delta units
    /// the caller's own <c>isRed</c> takes.</summary>
    private readonly record struct Boundary(bool Found, double JustInsideDelta, double JustOutsideDelta, int Doublings);

    // A live exponential-then-bisection search against `isRed` — the rule under test itself, called with
    // increasing perturbations, never a formula reimplemented here (root BOOT.md Taboos). `scale` sets the
    // search's own starting step from the cell's own real magnitude (a typed absolute perturbation would be the
    // "typed magnitude" the task instructions rule out); a cell whose real value is exactly 0 still gets a
    // starting step, from the 1e-9 floor. `isRed(0)` must already be false (the real, passing value) or the
    // caller's own setup is wrong, not this search — checked by every call site before calling this.
    private const int MaxDoublings = 80;

    private static Boundary FindBoundary(double scale, Func<double, bool> isRed)
    {
        var startScale = Math.Max(Math.Abs(scale) * 1e-6, 1e-9);
        var lo = 0.0;
        var hi = startScale;
        var doublings = 0;
        while (!isRed(hi))
        {
            if (doublings >= MaxDoublings)
            {
                return new Boundary(false, lo, double.NaN, doublings);
            }

            lo = hi;
            hi *= 2.0;
            doublings++;
        }

        // Bisect until adjacent doubles: `mid` stops changing `lo`/`hi` once the interval is one ULP wide, so
        // this loop always terminates on its own long before any iteration cap would matter.
        for (var i = 0; i < 200; i++)
        {
            var mid = lo + (hi - lo) / 2.0;
            if (mid <= lo || mid >= hi)
            {
                break;
            }

            if (isRed(mid))
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return new Boundary(true, lo, hi, doublings);
    }

    private void AssertRealBoundary(string label, Boundary boundary, Func<double, bool> isRed)
    {
        Assert.True(
            boundary.Found,
            $"{label}: no perturbation up to {MaxDoublings} doublings of the starting scale turned this rule red " +
            "— this is the reportable 'effectively infinite band' case (this class's own summary), and this " +
            "call site was not expected to be one of them; if it now is, that is itself the finding to report.");
        Assert.True(isRed(boundary.JustOutsideDelta), $"{label}: the bisected 'just outside' delta must still fail.");
        Assert.False(isRed(boundary.JustInsideDelta), $"{label}: the bisected 'just inside' delta must still pass.");
        _output.WriteLine(
            $"{label}: boundary delta in [{boundary.JustInsideDelta:G6}, {boundary.JustOutsideDelta:G6}] " +
            $"({boundary.Doublings} doublings from the starting scale).");
    }

    // ---- Compare-based rules: Student band, count predictive interval, count floor, print-resolution floor,
    // canonical axis. All five share one candidate-injection idiom (this node's BOOT.md, ## Invariants: "a test
    // that wants a specific candidate builds one by cloning a real parse and overwriting the cells it needs").

    private static bool IsRedInCompare(
        string formulation, IReadOnlyDictionary<string, double[]> baseCandidate, string quantity, int index, double delta)
    {
        var candidate = new Dictionary<string, double[]>(baseCandidate);
        var array = (double[])candidate[quantity].Clone();
        array[index] += delta;
        candidate[quantity] = array;
        var report = StatisticalCriterion.Compare(formulation, candidate, ReplicaKind.Lagged);
        return report.Failures.Any(f => f.Quantity == quantity && f.Index == index);
    }

    // Dkarm10: a plain scalar, not a distribution-function family member, not a category-length-varying array —
    // the ordinary `t * sd * factor` Student band governs it (measured while picking this cell: HPEPA3 threshold
    // 0.149 against a resolution floor of only 0.01, HMX 0.308 against 0.01 — the band, not the resolution, is
    // what a perturbation must clear).
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void StudentBandDkarm10BoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "Dkarm10";
        const int index = 0;
        var reference = CloneReference(formulation);
        var baseValue = reference[quantity][index];
        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0), $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} Student band {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // fmkarm[10]: originally picked as a populated, well-behaved cell of a family this node's BOOT.md, ## Invariants,
    // "Count-like cells" then listed as count-like. Kept as a Student-band case, not renamed away entirely, because
    // it is still a useful populated/well-behaved cell for that ordinary mechanism (several other fmkarm/fqkarm/coef
    // indices were tried and rejected when this test was first written, because their own negative-binomial floor,
    // driven by this file's very small per-quantity alpha at a heavy-tailed real replica population, was already
    // disproportionate before any test perturbed anything).
    //
    // ⚠ 2026-09-19: `fmkarm` (with `fmdok`/`fmkarm_cor`/`fmkarm_cor2`) was removed from
    // `DistributionFunctionFamilies` (`tests/Harness/BOOT.md`, "The candidate is never its own witness
    // (2026-09-19)", "Two refinements"): its own printed header reads "Mass density distribution function", i.e.
    // its cells sum a continuous quantity (pocket/particle volume), not a count, and hold no shared quantum at
    // all. This test therefore no longer exercises `CountFloorFromCounts`/`TryInferRunQuantum` at this cell —
    // renamed accordingly. Its own boundary is unchanged (measured again after the reclassification: still real
    // and finite on both sides, governed now by the ordinary Student band alone, with no count floor to widen
    // it). `CountPredictiveIntervalFqkarm10BoundaryIsRealOnBothSides` below is the new count-like case, added to
    // keep a live mutation-tested cell for the per-run quantum estimator itself (`fqkarm`'s own header reads
    // "Numeric density distribution function", a genuine count family).
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void StudentBandFmkarm10BoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "fmkarm";
        const int index = 10;
        var reference = CloneReference(formulation);
        var baseValue = reference[quantity][index];
        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0), $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(Math.Max(baseValue, 1e-6), delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} Student band {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // fqkarm[10]: a genuine count family ("Numeric density distribution function", this node's BOOT.md, "Two
    // refinements") at the same index `fmkarm[10]` used above, kept as the live mutation-tested case for
    // `TryInferRunQuantum`'s own largest-`q`-over-resolvable-cells estimator (this node's BOOT.md's own dated
    // mutation record has the `k`-search proof; this test proves the estimator finds a real, finite boundary in
    // ordinary use, not only that reverting it makes a known cell red).
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void CountPredictiveIntervalFqkarm10BoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "fqkarm";
        const int index = 10;
        var reference = CloneReference(formulation);
        var baseValue = reference[quantity][index];
        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0), $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(Math.Max(baseValue, 1e-6), delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} count predictive interval {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // Nbase: an echoed input (the two operands of "Nbase = A + B" in the original's own printed header), bit
    // identical across every lagged replica of a formulation, so the Student band term is exactly 0 and the
    // Poisson-like floor `sqrt(max(mean, 1))` on this integer-printed cell is what a perturbation must clear
    // (measured: HPEPA3 threshold 316.2 = sqrt(100000), HMX 100.0 = sqrt(10000) — both exactly the floor, not the
    // resolution of 1).
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void CountFloorNbaseBoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "Nbase";
        const int index = 0;
        var reference = CloneReference(formulation);
        var baseValue = reference[quantity][index];
        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0), $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} count floor {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // Gfr: an echoed input geometry fraction, also bit identical across every lagged replica (Student band term
    // 0, not integer-printed so no Poisson floor either) — the print-resolution floor (`ResultCell.Resolution`)
    // is the only thing left to clear (measured: HPEPA3 and HMX both 0.001, the 3-decimal-digit resolution of
    // "0.515E+00"/"0.100E+00").
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void PrintResolutionFloorGfrBoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "Gfr";
        const int index = 0;
        var reference = CloneReference(formulation);
        var baseValue = reference[quantity][index];
        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0), $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} print-resolution floor {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // dokkarm43[0]: a category-length-varying array (this node's BOOT.md, ## Invariants, "Canonical category
    // axis"), compared through Compare's own CategoryLengthVaryingArrays branch — its own replica pool is
    // restricted to replicas whose Dkarmcat prefix still agrees with the reference's at this index, distinct
    // machinery from the plain-scalar branch Dkarm10 exercises above, even though index 0 sits inside every
    // formulation's fixed-width prefix (this node's BOOT.md, ## Tail coverage, step 3) and so is never itself
    // excluded.
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void CanonicalAxisDokkarm43BoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "dokkarm43";
        const int index = 0;
        var reference = CloneReference(formulation);
        var baseValue = reference[quantity][index];
        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0), $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} canonical axis {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // ---- ForceFailure: `tests/Harness/HISTORY.md#three-decisions-2026-09-19`, #1's own absolute ceiling. A
    // mass-weighted family's own cell cannot exceed 1/step of its own axis (its own printed step is 10 mkm, so
    // the ceiling is 0.1) — checked unconditionally, ahead of the sparse-cell exclusion that would otherwise
    // treat a cell with no non-zero replicas as untestable and simply skip it. fmdok[0] is exactly such a cell
    // for both reference formulations (measured: 0.0 in the reference and in every one of HPEPA3's 32 and HMX's
    // 16 lagged replicas), so the only mechanism that can turn this candidate red, at any magnitude, is the
    // ceiling itself — not the ordinary Student band (excluded before it would run) and not the count-like floor
    // (`fmdok` is not count-like, ## Invariants, "Three decisions (2026-09-19)").

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void MassFamilyCeilingFmdok0BoundaryIsReal(string formulation)
    {
        const string quantity = "fmdok";
        const int index = 0;
        var reference = CloneReference(formulation);
        Assert.Equal(0.0, reference[quantity][index]);
        for (var k = 1; k <= ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount; k++)
        {
            var replica = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt"));
            Assert.Equal(0.0, replica[quantity][index]);
        }

        Assert.False(
            IsRedInCompare(formulation, reference, quantity, index, 0.0),
            $"{formulation} {quantity}[{index}]'s own real value must start green (excluded, not compared: this node's BOOT.md, #1's sparse-cell exclusion).");

        var boundary = FindBoundary(0.1, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} mass-family ceiling {quantity}[{index}]", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));

        // The boundary found must be the family's own unconditional ceiling (1/step = 0.1), not an incidental
        // statistical one (there should be none active here, but this is what would catch it if there were):
        // the "just inside" delta's own candidate value must still be at or under 0.1, and the "just outside"
        // delta's candidate value must exceed it.
        Assert.True(
            boundary.JustInsideDelta <= 0.1 + 1e-9,
            $"{formulation} {quantity}[{index}]: 'just inside' delta ({boundary.JustInsideDelta:G6}) exceeds the family's own ceiling of 0.1 — this is not the ceiling boundary.");
        Assert.True(
            boundary.JustOutsideDelta > 0.1 - 1e-6,
            $"{formulation} {quantity}[{index}]: 'just outside' delta ({boundary.JustOutsideDelta:G6}) is well under the family's own ceiling of 0.1 — this is not the ceiling boundary.");
    }

    // ---- Heavy-tail count rule (this node's BOOT.md, "Heavy-tail count rule for mass-weighted families
    // (decided 2026-09-19, revised 2026-09-20)"): the sibling deterministic check governs a mass-weighted
    // family's own cell once its sibling-reconstructed count drops below `mu = 30` — deep-tail cells, by the
    // model's own shape (density concentrated toward the middle of the axis, tapering to a handful of counts at
    // the far end). The array's own *last* non-zero cell is deepest in that tail for every reference formulation
    // (measured elsewhere this session: HPEPA3's own deep tail, i = 60..65, carries 1-22 counts), so mutating it
    // exercises the new deterministic bracket, not the ordinary Student band the mass-family ceiling test above
    // already covers.
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void HeavyTailCountRuleFmkarmDeepTailBoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "fmkarm";
        var reference = CloneReference(formulation);
        var array = reference[quantity];
        var index = Array.FindLastIndex(array, v => v != 0.0);
        Assert.True(index >= 0, $"{formulation} {quantity} has no non-zero cell to mutate.");
        var baseValue = array[index];

        Assert.False(
            IsRedInCompare(formulation, reference, quantity, index, 0.0),
            $"{formulation} {quantity}[{index}]'s own real value must start green.");

        var boundary = FindBoundary(Math.Max(baseValue, 1e-8), delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary(
            $"{formulation} heavy-tail count rule {quantity}[{index}] (base={baseValue:G6})", boundary,
            delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }

    // ---- CompareTailRowMean: this node's BOOT.md, ## Tail coverage (2026-09-18), step 2. TailRowMean is a
    // derived value (the unweighted mean of a source's own adaptive fqdokkarm rows at one column), not a single
    // printed cell, so "perturbing one observed value" here means shifting every contributing row's own printed
    // value at that column by the same delta — the mean of (original + delta) is exactly (original mean) + delta,
    // so this is still a single, well-defined perturbation of the derived quantity CompareTailRowMean reports on,
    // not several independent ones.

    private static bool IsRedInTailRowMean(
        string formulation, IReadOnlyDictionary<string, double[]> baseCandidate, int fixedWidthPrefixLength, int columnIndex, double delta)
    {
        var candidate = new Dictionary<string, double[]>(baseCandidate);
        var dkarmcatLength = candidate["Dkarmcat"].Length;
        for (var row = fixedWidthPrefixLength + 1; row <= dkarmcatLength; row++)
        {
            var key = $"fqdokkarm({row},:)";
            if (!candidate.TryGetValue(key, out var original) || columnIndex >= original.Length)
            {
                continue;
            }

            var mutated = (double[])original.Clone();
            mutated[columnIndex] += delta;
            candidate[key] = mutated;
        }

        var report = StatisticalCriterion.CompareTailRowMean(formulation, candidate, ReplicaKind.Lagged);
        return report.Failures.Any(f => f.Quantity == "TailRowMean" && f.Index == columnIndex);
    }

    // The representative column is picked per formulation, not a single shared index: measured while building
    // this test, column 0 (and several others) never fail at any magnitude tried up to 1e100 for either
    // formulation — the same scale-invariant escape as CountPredictiveInterval_AllZeroReplicaCellNever...  below,
    // triggered here even though TailRowMean[0] is not itself all-zero (see
    // TailRowMean_ColumnZero_NeverFailsAtAnyFiniteMagnitude, the finding this measurement turned into its own
    // test). Column 3 (HPEPA3) and column 10 (HMX) were measured to have a genuine, tight, finite boundary
    // instead, and are used here for that reason — the same kind of per-formulation pick
    // tests/Harness.Tests/TwoSampleBiasTests.cs already makes for its own rare-event quantities.
    private static readonly Dictionary<string, int> TailRowMeanWellBehavedColumn = new(StringComparer.Ordinal)
    {
        ["HPEPA3"] = 3,
        ["HMX"] = 10,
    };

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void TailRowMeanBoundaryIsRealOnBothSides(string formulation)
    {
        var columnIndex = TailRowMeanWellBehavedColumn[formulation];
        var reference = CloneReference(formulation);
        var i0 = CategoryAxis.FixedWidthPrefixLength(reference["Dkarmcat"]);
        Assert.False(IsRedInTailRowMean(formulation, reference, i0, columnIndex, 0.0), $"{formulation} TailRowMean[{columnIndex}]'s own real value must start green.");

        var referenceScale = reference.TryGetValue($"fqdokkarm({i0 + 1},:)", out var firstAdaptiveRow) && columnIndex < firstAdaptiveRow.Length
            ? firstAdaptiveRow[columnIndex]
            : 1.0;
        var boundary = FindBoundary(referenceScale, delta => IsRedInTailRowMean(formulation, reference, i0, columnIndex, delta));
        AssertRealBoundary($"{formulation} tail row mean TailRowMean[{columnIndex}]", boundary, delta => IsRedInTailRowMean(formulation, reference, i0, columnIndex, delta));
    }

    // ⚠ 2026-09-19: this test first asserted the opposite of what it does now (`TailRowMean_ColumnZero_
    // NeverFailsAtAnyFiniteMagnitude`): TailRowMean[0] never failed, for either formulation, at any magnitude of
    // this uniform-row perturbation up to 1e100, because CompareTailRowMean's own AddCell call hard-coded
    // `isDistributionFunction: true`, so CountFloor's TryInferRunQuantum always ran for a *derived mean*, and once a
    // perturbed candidate exceeded roughly 2^52 in magnitude, `Math.Round` of any ratio built from it returned the
    // value itself bit for bit, so TryInferRunQuantum's own ratio check accepted it against *any* quantum trivially.
    // Fixed in `tests/Harness/HISTORY.md#candidate-never-its-own-witness-2026-09-19`: TailRowMean is a
    // mean of rows, not a count, and the count floor no longer applies to it at all
    // (`StatisticalCriterion.CompareTailRowMean`'s own `AddCell` call now passes `isDistributionFunction: false`).
    // With the count floor removed at its root, this cell now has an ordinary, tight Student-band boundary, not an
    // unreachable one: measured, both formulations turn red on the very first doubling of the search's own
    // starting scale (a perturbation of about 1e-6 times the first adaptive row's own printed value already
    // exceeds the band). This is the boundary the fix produces, not a search artifact of picking column 0
    // specifically; `TailRowMeanBoundaryIsRealOnBothSides` above already exercises a well-behaved column of the
    // same statistic, so this case is retained to show that the *previously escaping* column now has a real
    // boundary too, not to add new coverage of the ordinary Student band.
    // ⚠ 2026-09-19: this test first asserted a real, finite boundary here (`...BoundaryIsRealAfterTheCountFloorFix`,
    // this node's own ⚠ above explains why: the count floor's removal gave column 0 an ordinary Student-band
    // boundary at the time). This node's BOOT.md, "Three decisions (2026-09-19)", #1's sparse-cell exclusion,
    // wired into `CompareTailRowMean` the same day (found while re-verifying #3, that section's own BOOT.md
    // account has the full history), now excludes `TailRowMean[0]` outright for both reference formulations —
    // measured directly below, every adaptive `fqdokkarm` row of every one of HPEPA3's 32 and HMX's 16 lagged
    // replicas reads exactly `0.0` at column 0, so the pool carries no scale at all, the precise condition #1
    // says must be excluded rather than compared. `TailRowMean` has no absolute-ceiling family of its own (#1's
    // bound is scoped to the mass-weighted print families), so this cell is now, correctly, untestable at any
    // magnitude — a cell with no scale being refused a verdict, not a formula escaping its own floor (the
    // pre-#1 defect the previous form of this test guarded). Renamed to reflect the current, correct mechanism.
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void TailRowMeanColumnZeroExcludedAsScalelessUnderTheSparseCellRule(string formulation)
    {
        const int columnIndex = 0;
        var reference = CloneReference(formulation);
        var i0 = CategoryAxis.FixedWidthPrefixLength(reference["Dkarmcat"]);
        Assert.False(IsRedInTailRowMean(formulation, reference, i0, columnIndex, 0.0), $"{formulation} TailRowMean[{columnIndex}]'s own real value must start green.");

        // The pool itself carries no scale at this column: confirmed directly (not merely inferred from the
        // search's own negative result below) before relying on it.
        for (var k = 1; k <= ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount; k++)
        {
            var replica = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt"));
            var replicaDkarmcatLength = replica["Dkarmcat"].Length;
            for (var row = i0 + 1; row <= replicaDkarmcatLength; row++)
            {
                if (replica.TryGetValue($"fqdokkarm({row},:)", out var rowValues) && columnIndex < rowValues.Length)
                {
                    Assert.Equal(0.0, rowValues[columnIndex]);
                }
            }
        }

        // No finite perturbation of this scaleless cell should ever turn it red (this node's BOOT.md, #1's own
        // exchangeability argument applies here exactly as it does to the mass families' sparse cells, and this
        // cell has no absolute ceiling of its own to fall back to) — so, unlike every other case in this class,
        // `Found == false` up to the search's own cap is the expected, correct outcome, not the finding.
        var boundary = FindBoundary(1.0, delta => IsRedInTailRowMean(formulation, reference, i0, columnIndex, delta));
        Assert.False(
            boundary.Found,
            $"{formulation} TailRowMean[{columnIndex}] turned red at some finite perturbation — this cell was expected to stay excluded (no replica gives it a scale), so a red result here is itself a finding to report, not the expected outcome.");
    }

    // ---- CompareAdaptiveIndexMatched: this node's BOOT.md, ## Tail coverage (2026-09-18), step 3. Dkarmcat's own
    // adaptive row at an arbitrary adaptive index is a single printed cell, so this perturbs it directly, the same
    // idiom as the Compare-based rules above. Generalized from a fixed index-0 perturbation (root cause of E2's
    // own two deleted tests below) so the common-range boundary tests can reuse it at any index.

    private static bool IsRedInAdaptiveIndexMatched(
        string formulation, IReadOnlyDictionary<string, double[]> baseCandidate, int i0, int adaptiveIndex, double delta)
    {
        var candidate = new Dictionary<string, double[]>(baseCandidate);
        var dkarmcat = candidate["Dkarmcat"];
        var row = i0 + adaptiveIndex;
        var mutated = new double[Math.Max(dkarmcat.Length, row + 1)];
        Array.Copy(dkarmcat, mutated, dkarmcat.Length);
        mutated[row] += delta;
        candidate["Dkarmcat"] = mutated;
        var report = StatisticalCriterion.CompareAdaptiveIndexMatched(formulation, candidate, ReplicaKind.Lagged);
        return report.Failures.Any(f => f.Quantity == "Dkarmcat@adaptive" && f.Index == adaptiveIndex);
    }

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void AdaptiveIndexMatchedDkarmcatAdaptiveZeroBoundaryIsRealOnBothSides(string formulation)
    {
        var reference = CloneReference(formulation);
        var i0 = CategoryAxis.FixedWidthPrefixLength(reference["Dkarmcat"]);
        var baseValue = reference["Dkarmcat"][i0];
        Assert.False(IsRedInAdaptiveIndexMatched(formulation, reference, i0, 0, 0.0), $"{formulation} Dkarmcat@adaptive[0]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInAdaptiveIndexMatched(formulation, reference, i0, 0, delta));
        AssertRealBoundary($"{formulation} adaptive index matched Dkarmcat@adaptive[0] (base={baseValue:G6})", boundary, delta => IsRedInAdaptiveIndexMatched(formulation, reference, i0, 0, delta));
    }

    // ---- AdaptiveRowCount: `tests/Harness/HISTORY.md#fable-5-1-decision-iii`, "The adaptive row count cell" — a
    // source's own printed adaptive row count is an exact integer by construction, so it is now compared through
    // the same negative-binomial predictive interval every other count-like cell gets, with a fixed quantum of 1
    // (one row is one count), rather than the plain Student band with an integer-printed Poisson floor it used
    // before. Adding whole rows to the candidate's own Dkarmcat is the one well-defined perturbation of this
    // count; the extra rows' own values are extrapolated from the candidate's own last spacing, never a typed
    // magnitude read off any fixture (root BOOT.md Taboos).

    private static bool IsRedInAdaptiveRowCount(
        string formulation, IReadOnlyDictionary<string, double[]> baseCandidate, double delta)
    {
        var candidate = new Dictionary<string, double[]>(baseCandidate);
        var dkarmcat = candidate["Dkarmcat"];
        var addedRows = (int)Math.Round(delta, MidpointRounding.AwayFromZero);
        if (addedRows > 0)
        {
            var last = dkarmcat.Length > 0 ? dkarmcat[^1] : 0.0;
            var step = dkarmcat.Length > 1 ? dkarmcat[^1] - dkarmcat[^2] : Math.Max(last, 1.0);
            var mutated = new double[dkarmcat.Length + addedRows];
            Array.Copy(dkarmcat, mutated, dkarmcat.Length);
            for (var i = 0; i < addedRows; i++)
            {
                mutated[dkarmcat.Length + i] = last + step * (i + 1);
            }

            candidate["Dkarmcat"] = mutated;
        }

        var report = StatisticalCriterion.CompareAdaptiveIndexMatched(formulation, candidate, ReplicaKind.Lagged);
        return report.Failures.Any(f => f.Quantity == "AdaptiveRowCount" && f.Index == 0);
    }

    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void AdaptiveRowCountExtraRowsBoundaryIsReal(string formulation)
    {
        var reference = CloneReference(formulation);
        var i0 = CategoryAxis.FixedWidthPrefixLength(reference["Dkarmcat"]);
        var baseValue = (double)Math.Max(0, reference["Dkarmcat"].Length - i0);
        Assert.False(IsRedInAdaptiveRowCount(formulation, reference, 0.0), $"{formulation} AdaptiveRowCount[0]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInAdaptiveRowCount(formulation, reference, delta));
        AssertRealBoundary($"{formulation} adaptive row count AdaptiveRowCount[0] (base={baseValue:G6})", boundary, delta => IsRedInAdaptiveRowCount(formulation, reference, delta));
    }

    // ---- CompareAdaptiveIndexMatched's own COMMON-RANGE rule (E2, `tests/Harness/HISTORY.md#e2-adaptive-common-range`):
    // reverses decision III's own union-with-absent-as-zero rule (the two tests this section used to hold,
    // `AdaptiveIndexMatchedUnionOnlyRowPopulatedBoundaryIsReal` and
    // `...SparseExcludedAsScalelessUnderTheSparseCellRule`, deleted with it) in favour of the common range every
    // contributing source reaches. HMX lagged is the same case those two tests used, for the same reason: its
    // own reference and 16-replica pool give both "just past the shortest source, inside the candidate's own
    // reach" and "well past the candidate's own last row" in one formulation.

    private static int[] AdaptiveLengths(string formulation, string replicaDirectory, int i0)
    {
        var lengths = new List<int>();
        for (var k = 1; k <= ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount; k++)
        {
            var replica = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", replicaDirectory, formulation, k + ".m.txt"));
            if (replica.TryGetValue("Dkarmcat", out var replicaDkarmcat))
            {
                lengths.Add(Math.Max(0, replicaDkarmcat.Length - i0));
            }
        }

        return [.. lengths];
    }

    // The last row every one of HMX's 16 lagged replicas and the reference itself still reach (commonLength − 1,
    // computed from the raw fixture files, never a typed number): under the union rule this row already scored,
    // so the common-range rule must still give it a real, finite boundary — the fix narrows what gets scored, it
    // does not stop scoring the range that remains.
    [Trait("Category", "Long")]
    [Fact]
    public void AdaptiveIndexMatchedLastCommonRowBoundaryIsRealOnBothSides()
    {
        const string formulation = "HMX";
        var reference = CloneReference(formulation);
        var i0 = CategoryAxis.FixedWidthPrefixLength(reference["Dkarmcat"]);
        var candidateAdaptiveLength = Math.Max(0, reference["Dkarmcat"].Length - i0);
        var replicaLengths = AdaptiveLengths(formulation, "replicas-lagged", i0);
        var commonLength = Math.Min(candidateAdaptiveLength, replicaLengths.Min());
        var adaptiveIndex = commonLength - 1;

        var baseValue = reference["Dkarmcat"][i0 + adaptiveIndex];
        Assert.False(
            IsRedInAdaptiveIndexMatched(formulation, reference, i0, adaptiveIndex, 0.0),
            $"{formulation} Dkarmcat@adaptive[{adaptiveIndex}]'s own real value must start green.");

        var boundary = FindBoundary(baseValue, delta => IsRedInAdaptiveIndexMatched(formulation, reference, i0, adaptiveIndex, delta));
        AssertRealBoundary(
            $"{formulation} last common row Dkarmcat@adaptive[{adaptiveIndex}] (commonLength={commonLength}, base={baseValue:G6})",
            boundary, delta => IsRedInAdaptiveIndexMatched(formulation, reference, i0, adaptiveIndex, delta));
    }

    // Three rows past the shortest contributing source's own reach, none of them scored under the common-range
    // rule regardless of how populated the union rule found them (measured directly against the raw fixture
    // files, never typed magnitudes): the row just past commonLength, still inside the candidate's own reach and
    // reached by all but one of HMX's 16 lagged replicas (the union rule's own majority would have scored it —
    // the common-range rule does not, because the shortest single source governs, not a count); and the two
    // rows past the candidate's own last row that the deleted tests exercised as "populated" (4 replicas) and
    // "sparse" (1 replica) — both excluded now for the same reason, the candidate itself does not reach them.
    [Trait("Category", "Long")]
    [Fact]
    public void AdaptiveIndexMatchedRowsPastTheShortestSourceAreNeverScored()
    {
        const string formulation = "HMX";
        var reference = CloneReference(formulation);
        var i0 = CategoryAxis.FixedWidthPrefixLength(reference["Dkarmcat"]);
        var candidateAdaptiveLength = Math.Max(0, reference["Dkarmcat"].Length - i0);
        var replicaLengths = AdaptiveLengths(formulation, "replicas-lagged", i0);
        var commonLength = Math.Min(candidateAdaptiveLength, replicaLengths.Min());

        var justPastCommon = commonLength;
        Assert.True(justPastCommon < candidateAdaptiveLength, "expected a row still inside the candidate's own reach.");
        Assert.Equal(replicaLengths.Length - 1, replicaLengths.Count(l => l > justPastCommon));

        var populatedPastCandidate = candidateAdaptiveLength;
        var sparsePastCandidate = candidateAdaptiveLength + 9;
        Assert.True(replicaLengths.Count(l => l > populatedPastCandidate) >= 2, "expected the first row past the candidate's own reach to still be populated by 2+ replicas.");
        Assert.True(replicaLengths.Count(l => l > sparsePastCandidate) < 2, "expected the tenth row past the candidate's own reach to be sparse (fewer than 2 replicas).");

        foreach (var adaptiveIndex in new[] { justPastCommon, populatedPastCandidate, sparsePastCandidate })
        {
            Assert.False(
                IsRedInAdaptiveIndexMatched(formulation, reference, i0, adaptiveIndex, 0.0),
                $"{formulation} Dkarmcat@adaptive[{adaptiveIndex}]'s own zero-delta perturbation must start green.");

            var boundary = FindBoundary(1.0, delta => IsRedInAdaptiveIndexMatched(formulation, reference, i0, adaptiveIndex, delta));
            Assert.False(
                boundary.Found,
                $"{formulation} Dkarmcat@adaptive[{adaptiveIndex}] turned red at some finite perturbation — this row is past the shortest contributing source's own reach and must stay excluded under the common-range rule.");
        }
    }

    // ---- TwoSampleBiasOfSets: unlike every rule above, TwoSampleBias has no candidate parameter at all — both
    // sides are whole replica sets it loads itself (this node's BOOT.md, ## Invariants, "Two-sample bias"). The
    // internal seam StatisticalCriterion.TwoSampleBiasOfSets takes both sets already loaded, so this test loads
    // them the same way tests/Harness.Tests/TailCoverageTests.cs already loads a single replica file (by the same
    // tests/Fixtures path convention, not a new one), then perturbs every member of the "second" set uniformly at
    // one cell — a controlled shift of that set's own mean at the cell, holding its internal spread fixed, the
    // cleanest well-defined single-value perturbation this two-population comparison admits.

    private static List<Dictionary<string, double[]>> LoadSet(string formulation, string replicaDirectory, int replicaCount)
    {
        var set = new List<Dictionary<string, double[]>>(replicaCount);
        for (var k = 1; k <= replicaCount; k++)
        {
            set.Add(new Dictionary<string, double[]>(ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", replicaDirectory, formulation, k + ".m.txt"))));
        }

        return set;
    }

    private static bool IsRedInTwoSampleBias(
        IReadOnlyList<IReadOnlyDictionary<string, double[]>> firstSet,
        IReadOnlyList<Dictionary<string, double[]>> secondSetBase,
        string quantity, int index, double delta)
    {
        var perturbedSecondSet = secondSetBase
            .Select(member =>
            {
                var clone = new Dictionary<string, double[]>(member);
                var array = (double[])clone[quantity].Clone();
                array[index] += delta;
                clone[quantity] = array;
                return (IReadOnlyDictionary<string, double[]>)clone;
            })
            .ToList();

        var cells = StatisticalCriterion.TwoSampleBiasOfSets(firstSet, perturbedSecondSet);
        var cell = cells.FirstOrDefault(c => c.Quantity == quantity && c.Index == index);
        return cell is { Differs: true };
    }

    // Dok43all(1): root HISTORY.md#known-bias-mass-type-quantities-withdrawn's own
    // "trustworthy" quantity, TwoSampleBiasTests.MassMeanOxidizerSizeAgreesBetweenLayouts
    // asserts `Differs == false` for it in every formulation today — the green starting point this boundary
    // search needs.
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void TwoSampleBiasDok43allBoundaryIsRealOnBothSides(string formulation)
    {
        const string quantity = "Dok43all";
        const int index = 0;
        var replicaCount = ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount;
        var laggedSet = LoadSet(formulation, "replicas-lagged", replicaCount);
        var independentSet = LoadSet(formulation, "replicas-independent", replicaCount);
        var baseValue = independentSet[0][quantity][index];

        Assert.False(IsRedInTwoSampleBias(laggedSet, independentSet, quantity, index, 0.0), $"{formulation} {quantity}[{index}] must start agreeing (root BOOT.md, 'trustworthy'; TwoSampleBiasTests).");

        var boundary = FindBoundary(baseValue, delta => IsRedInTwoSampleBias(laggedSet, independentSet, quantity, index, delta));
        AssertRealBoundary($"{formulation} two-sample bias {quantity}[{index}] (base={baseValue:G6})", boundary, delta => IsRedInTwoSampleBias(laggedSet, independentSet, quantity, index, delta));
    }

    // ⚠ 2026-09-19: this test first asserted the opposite of what it does now
    // (`CountPredictiveInterval_AllZeroReplicaCellNeverFailsAtAnyFiniteMagnitude`): an all-zero-across-every-
    // replica distribution-function cell had no finite failing perturbation at all. "coef" is a
    // distribution-function family member (this node's BOOT.md, ## Invariants, "Count-like cells") and its own
    // cell 0 prints as exactly 0 in the reference and, measured, in every one of HPEPA3's and HMX's own lagged
    // replicas; it is not named in exclusions.json, so this is the real Compare path, not a rule that already
    // skips it.
    //
    // The old mechanism (this node's HISTORY.md, "Oracle mutation (2026-09-19)" carries the full derivation): TryInferRunQuantum's
    // own population was {candidateValue} union replicaValues (`CountFloor`'s call site passed the perturbed
    // candidate itself into the quantum search). When every replica is exactly 0, that population collapsed to
    // the singleton {perturbedValue}, so the inferred quantum *was* the perturbed value, exactly, regardless of
    // its magnitude, and the negative-binomial floor was then a strictly larger multiple of that same perturbed
    // value — the candidate could never be measured as having moved "more than the floor" away from a mean of 0,
    // at any magnitude. Fixed first in `tests/Harness/BOOT.md`, "The candidate is never its own witness
    // (2026-09-19)" (the sibling-pool form): the candidate no longer contributed to the quantum at all, so an
    // all-zero-replica cell fell back to the sibling cells of the same printed array (every other column of
    // "coef" this formulation's own replicas print, still replicas only).
    //
    // ⚠ 2026-09-19: the sibling pool was itself replaced the same day by "Per-run quantum" (same section), and
    // this test's own mechanism moved with it, though its boundary is unchanged (still finite, still measured
    // below). Under the per-run design the candidate's own quantum is inferred from the *candidate's own* "coef"
    // array — the mutated clone this test builds still carries the reference's other 591 columns unperturbed, and
    // "coef" is populated and well-quantized enough (measured) for `TryInferRunQuantum` to find a real quantum
    // from those, independently of any replica. Index 0's own perturbed value is then judged against that
    // candidate-own quantum directly (`Finalize`'s `candidateIsImplausibleCount`): a nonzero perturbation not
    // close to an integer multiple of it is a failure in its own right, at a small, finite delta, not after
    // climbing to the search's own 80-doubling cap. Not every distribution-function family determines a quantum
    // this reliably from its own array — `tests/Harness/BOOT.md`'s "Per-run quantum" section's own "Implementation
    // and measurements (2026-09-19, per-run quantum)" has the counterexample (`fmkarm_cor2`, `fqdokkarm` rows).
    [Trait("Category", "Long")]
    [Theory]
    [MemberData(nameof(ReferenceFormulations))]
    public void CountPredictiveIntervalAllZeroReplicaCellBoundaryIsRealAfterTheQuantumFix(string formulation)
    {
        const string quantity = "coef";
        const int index = 0;
        var reference = CloneReference(formulation);
        Assert.Equal(0.0, reference[quantity][index]);
        for (var k = 1; k <= ResultsMFileTests.Formulations.Single(f => f.Name == formulation).ReplicaCount; k++)
        {
            var replica = ResultsMFile.Parse(RepositoryPaths.Resolve("tests", "Fixtures", "replicas-lagged", formulation, k + ".m.txt"));
            Assert.Equal(0.0, replica[quantity][index]);
        }

        Assert.False(IsRedInCompare(formulation, reference, quantity, index, 0.0));

        // No real magnitude to derive a starting scale from (the cell's own real value is 0); 1.0 is an
        // arbitrary unit scale, the same one the pre-fix version of this test used.
        var boundary = FindBoundary(1.0, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
        AssertRealBoundary($"{formulation} count predictive interval (post-fix) {quantity}[{index}], all-zero replicas", boundary, delta => IsRedInCompare(formulation, reference, quantity, index, delta));
    }
}
