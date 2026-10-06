using PropStruct.Particle;
using PropStruct.Random;
using PropStruct.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace PropStruct.Execution.Tests;

/// <summary>
/// Positive control for <see cref="Engine.RunReferenceParticle"/>'s <c>Original</c>-kind record
/// seed/commit wiring (root BOOT.md, the new taboo "Every check that guards a quantitative claim
/// is proven twice"). The defect this guards against: <c>Attempt.Run</c>'s own REAL*4 write
/// sites really did round (proven by <c>Particle.Tests.PrecisionKindClassificationTests</c>,
/// which drives <c>Attempt.Run</c> directly), yet the option reached no printed digit, because
/// this node's own <c>RunReferenceParticle</c> used to zero every particle's record and fold it
/// into the run totals in plain <see langword="double"/>, so every rounding happened against a
/// magnitude near zero (real, about 1e-8, invisible at print precision) instead of the run's true
/// scale (<c>src/Execution/HISTORY.md#reference-mode-record-seeding</c>). That test proved
/// <c>Particle</c>'s own code; it never went through this node's seed/commit orchestration, so it
/// stayed green through the whole episode. This test goes through the one path the defect lived
/// in, <see cref="Engine.RunReferenceParticle"/>, and reads the answer where <c>Simulation</c>
/// reads it, <see cref="Engine.ReadTotals"/>.
///
/// The control has a known answer by construction (the task's own argument, restated here):
/// pre-set a REAL*4-classified accumulator's running total to <c>S = 2^k</c>, chosen (see
/// <c>ChooseExponent</c> below) far above <c>2^24</c> times the largest term this run can add to
/// it. <c>AddReal4</c> (<c>Particle/Attempt.cs</c>) computes <c>(double)(float)(current + term)</c>
/// unconditionally under <see cref="PrecisionKind.Original"/>: once <c>current</c> is exactly
/// <c>S</c>, a <c>term</c> below half a binary32 ulp of <c>S</c> leaves <c>current + term</c>
/// rounding back to exactly <c>S</c> in binary32, every single time, so the field must read back
/// bit for bit <c>S</c> after any number of further additions, not merely close to it. Every
/// real*8-declared field, which <c>AddReal4</c> never touches under either kind, is seeded at
/// zero in both the run this test compares against (<c>PrecisionKind.Binary64</c>) and this
/// run — the same starting point, not merely the same kind of starting point — so its own
/// comparison needs no argument about magnitudes at all: both runs execute the identical
/// sequence of ordinary <see langword="double"/> additions to the identical starting value, and
/// must therefore end at the identical bits.
///
/// <c>Particles = 1</c> is load-bearing, not a speed shortcut. <see cref="Engine.RunReferenceParticle"/>
/// seeds the record from <c>_realTotals</c> and commits it back once <em>per particle</em>: over
/// more than one particle, <c>Original</c> keeps accumulating incrementally into the same running
/// total (the original's own shape, one memory location written by every attempt of every
/// particle) while <c>Binary64</c> commits each particle's own zero-seeded delta with one
/// <see cref="Fold.Add"/>, so the two group the identical terms differently -- found empirically
/// while building this test: at <c>Particles = 500</c> the real*8 fields agreed with the Binary64
/// run to about 13 significant digits, not bit for bit, purely from this regrouping, with no
/// defect involved (<see langword="double"/> addition is not associative). One particle is exactly
/// one seed-then-commit cycle in both kinds, so the "identical starting value, identical
/// operations" argument above is not approximate there, and it already reaches every one of the
/// twenty-six record fields on this formulation (asserted below, not assumed).
///
/// Vacuous-pass guard (the failure this project has already hit twice, task instructions): a
/// seeded total that is merely never touched would also read back unchanged. This test rejects
/// that reading twice — first by requiring most of the classification's fields to have actually
/// grown under a real, unseeded <see cref="PrecisionKind.Binary64"/> run of the identical
/// scenario before <c>S</c> is even chosen (<c>ChooseExponent</c> would divide by that growth
/// being zero if it were), second by asserting the two runs' integer totals and final stream
/// states are identical, which is only true if both actually ran the same particle's attempts to
/// completion. A run that skipped every attempt would fail the first guard; a run of a different
/// particle would fail the second.
/// </summary>
[Collection(LongClassesSerialTests.Name)]
public sealed class AccumulationPositiveControlTests(ITestOutputHelper output)
{
    // Load-bearing, not a speed shortcut -- see the class doc's "Particles = 1" paragraph: only a
    // single seed-then-commit cycle makes the real*8 comparison provably bit-exact rather than
    // merely close.
    private const int ParticleCount = 1;
    private const long MaxAttemptsPerParticle = 10_000_000;

    // On top of the 2^24 the argument itself needs (binary32's 24-bit mantissa): covers the gap
    // between "an upper bound on the largest term" (ChooseExponent's own reasoning below) and the
    // term itself, and rounding-mode edge cases at the boundary. More margin only makes the
    // vanishing more certain, never less, so this is a safety allowance, not a fitted constant.
    private const int SafetyMarginBits = 16;

    private sealed record RecordField(string Name, Func<AccumulatorLayout, int> Offset, Func<ModelSetup, int> Length);

    // Every field of Particle/API.md's "Real-valued record" table that is a sum (DpMax/DpMaxCor
    // are running maxima, outside PrecisionKind entirely, root BOOT.md, "Precision kind is
    // an option of every run": not listed here or in the generated classification). Offsets and
    // lengths come from AccumulatorLayout/ModelSetup directly, compile-checked against the real
    // struct fields; only the rounds/does-not-round flag comes from the generated file, read
    // below, never typed by hand (AGENTS.md §6, a criterion quantified by "all").
    private static readonly RecordField[] Fields =
    [
        new("Xss", l => l.Xss, _ => 7),
        new("DokBase41", l => l.DokBase41, _ => 1),
        new("DokBase31", l => l.DokBase31, _ => 1),
        new("DokSur41", l => l.DokSur41, _ => 1),
        new("DokSur31", l => l.DokSur31, _ => 1),
        new("Sd4", l => l.Sd4, _ => 1),
        new("Sd3", l => l.Sd3, _ => 1),
        new("D41", l => l.D41, _ => 1),
        new("D31", l => l.D31, _ => 1),
        new("Dp31", l => l.Dp31, _ => 1),
        new("Dp41", l => l.Dp41, _ => 1),
        new("JammedTotal", l => l.JammedTotal, _ => 1),
        new("NnTotal", l => l.NnTotal, _ => 1),
        new("VmkmTotal2", l => l.VmkmTotal2, _ => 1),
        new("VdokTotal2", l => l.VdokTotal2, _ => 1),
        new("Allvdok", l => l.Allvdok, s => s.Ndok),
        new("Vdokstr", l => l.Vdokstr, s => s.Ndok),
        new("Vsmkm", l => l.Vsmkm, s => s.Ndok),
        new("Svd", l => l.Svd, s => s.Ndok),
        new("VmkmTotal", l => l.VmkmTotal, s => s.Ndok),
        new("VdokTotal", l => l.VdokTotal, s => s.Ndok),
        new("Vks", l => l.Vks, s => s.Nkarm),
        new("FmkarmCor", l => l.FmkarmCor, s => s.Nkarm),
        new("Fmkarm2", l => l.Fmkarm2, s => s.Nkarm),
        new("Dokp41", l => l.Dokp41, s => s.Ncat),
        new("Dokp31", l => l.Dokp31, s => s.Ncat),
    ];

    [Fact]
    [Trait("Category", "Long")]
    public void OriginalAccumulationPreSeededTotalSurvivesExactlyAndDoubleFieldsMatchDoubleKind()
    {
        var classification = ParseGeneratedClassification();
        foreach (var field in Fields)
        {
            Assert.True(classification.ContainsKey(field.Name),
                $"{field.Name}: not named in RealFourAccumulators.generated.txt -- this table and the generated file have drifted apart.");
        }

        using var warmup = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        var ready = ReferenceFormulationDriver.PrepareThroughCycle0(warmup, StreamLayout.Independent, seed: 0UL, "HPEPA3");
        var setup = ready.Setup;
        var layout = setup.Layout;
        var zeroReal = new double[layout.RecordLength];

        // --- Baseline: PrecisionKind.Binary64, every real total starting at zero -- both to
        // measure the largest term this scenario can produce (ChooseExponent) and to be the
        // "grew exactly as it does under Binary64" the real*8 fields are checked against.
        var doubleSetup = setup;
        doubleSetup.Kind = PrecisionKind.Binary64;

        using var baseline = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        baseline.Load(in doubleSetup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        baseline.WriteTotals((long[])ready.IntegerTotals.Clone(), (double[])zeroReal.Clone());
        baseline.SetCycle(cycleFlag: 1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(baseline.DebugReadQks1(), v => v != 0.0);

        var baselineStreams = StreamSeeds.ForParticle(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal);
        RunParticles(baseline, ref baselineStreams);

        var baselineIntegers = new long[layout.IntegerLength];
        var baselineReal = new double[layout.RecordLength];
        baseline.ReadTotals(baselineIntegers, baselineReal);

        // --- Vacuous-pass guard (class doc): the control proves nothing unless the fields it is
        // about to pin actually grew when nothing pinned them.
        var real4Fields = Fields.Where(f => classification[f.Name]).ToArray();
        var real8Fields = Fields.Where(f => !classification[f.Name]).ToArray();
        var real4Touched = real4Fields.Count(f => Cells(baselineReal, f, setup).Any(v => v != 0.0));
        var real8Touched = real8Fields.Count(f => Cells(baselineReal, f, setup).Any(v => v != 0.0));
        output.WriteLine($"under Binary64, over {ParticleCount} cycle-1 particle(s): {real4Touched}/{real4Fields.Length} REAL*4 fields touched, {real8Touched}/{real8Fields.Length} real*8 fields touched");
        Assert.True(real4Touched >= real4Fields.Length - 2,
            $"only {real4Touched}/{real4Fields.Length} REAL*4-classified fields grew under Binary64 kind -- this HPEPA3 particle reaches all fourteen; a lower count means the scenario (formulation, ordinal) changed and needs a look before trusting the rest of this test.");
        Assert.True(real8Touched >= real8Fields.Length - 2,
            $"only {real8Touched}/{real8Fields.Length} real*8 fields grew under Binary64 kind -- see the REAL*4 guard's own remark.");

        // --- Choose S per field (see ChooseExponent's own doc for the bound and the margin). One
        // shared S across every field was tried first and is wrong: the fifteen REAL*4-classified
        // fields span about twelve orders of magnitude on HPEPA3 (JammedTotal ~O(1), Vks ~O(1e-10)),
        // and a global S sized to the largest of them makes a genuine double-precision addition on
        // the smallest fields round away on its own, whatever AddReal4 does -- caught only by
        // deliberately reverting the fix (BOOT.md's non-degeneracy record) and finding twelve of
        // fourteen fields still passing. Each field's own S is bounded from its own largest cell.
        var exponentByField = new Dictionary<string, int>();
        var sByField = new Dictionary<string, double>();
        foreach (var field in real4Fields)
        {
            var largestCellValue = Cells(baselineReal, field, setup).Select(Math.Abs).DefaultIfEmpty(0.0).Max();
            if (largestCellValue == 0.0)
            {
                continue; // never touched under Binary64; nothing to pin, see the coverage guard above.
            }

            var exponent = ChooseExponent(largestCellValue);
            exponentByField[field.Name] = exponent;
            sByField[field.Name] = Math.ScaleB(1.0, exponent);
        }

        output.WriteLine(string.Join(", ", sByField.Select(kv => $"{kv.Key}: S=2^{exponentByField[kv.Key]}={kv.Value:E3}")));

        // --- Control: PrecisionKind.Original, REAL*4-classified cells pre-seeded to their own
        // field's S, every other cell at zero -- the same starting point the baseline used for them
        // (class doc: "the same starting point, not merely the same kind of starting point").
        var seeded = (double[])zeroReal.Clone();
        foreach (var field in real4Fields)
        {
            if (!sByField.TryGetValue(field.Name, out var fieldS))
            {
                continue;
            }

            var offset = field.Offset(layout);
            var length = field.Length(setup);
            for (var c = 0; c < length; c++)
            {
                seeded[offset + c] = fieldS;
            }
        }

        var originalSetup = setup;
        originalSetup.Kind = PrecisionKind.Original;

        using var control = Engine.Create(AcceleratorKind.Cpu, 2L << 30);
        control.Load(in originalSetup, ready.Tables.Bounds, ready.Tables.Cumulative, ready.Tables.PocketForming);
        control.WriteTotals((long[])ready.IntegerTotals.Clone(), seeded);
        control.SetCycle(cycleFlag: 1, ready.Dmaxxx, (double[])ready.Pdoksmall.Clone());
        Assert.Contains(control.DebugReadQks1(), v => v != 0.0);

        var controlStreams = StreamSeeds.ForParticle(StreamLayout.Independent, seed: 0UL, ready.NextOrdinal);
        RunParticles(control, ref controlStreams);

        var controlIntegers = new long[layout.IntegerLength];
        var controlReal = new double[layout.RecordLength];
        control.ReadTotals(controlIntegers, controlReal);

        // Second half of the vacuous-pass guard: no REAL*4-classified accumulator is ever read
        // back inside Attempt.Run (Particle/BOOT.md, "Accumulators"), so the two runs must have
        // drawn and decided identically whatever their record cells held. If this fails, the runs
        // diverged and nothing below is informative.
        Assert.Equal(baselineIntegers, controlIntegers);
        AssertStreamsEqual(baselineStreams, controlStreams);

        // --- The control's own answer.
        var wrongRounding = new List<string>();
        var wrongGrowth = new List<string>();
        foreach (var field in Fields)
        {
            var offset = field.Offset(layout);
            var length = field.Length(setup);
            var rounds = classification[field.Name];

            for (var c = 0; c < length; c++)
            {
                var cell = offset + c;
                if (rounds)
                {
                    var expected = sByField.GetValueOrDefault(field.Name, 0.0); // 0.0: never touched under Binary64, see above.
                    if (controlReal[cell] != expected)
                    {
                        var describeExpected = sByField.TryGetValue(field.Name, out var fieldS)
                            ? $"exactly S = {fieldS:R} (2^{exponentByField[field.Name]})"
                            : "exactly 0 (never touched under Binary64)";
                        wrongRounding.Add($"{field.Name}[{c}] = {controlReal[cell]:R}, expected {describeExpected}");
                    }
                }
                else
                {
                    if (controlReal[cell] != baselineReal[cell])
                    {
                        wrongGrowth.Add($"{field.Name}[{c}]: Original = {controlReal[cell]:R}, Binary64 = {baselineReal[cell]:R}");
                    }
                }
            }
        }

        Assert.True(wrongRounding.Count == 0,
            $"REAL*4-classified cells that did not stay exactly at S (a further addition survived rounding): {string.Join("; ", wrongRounding)}");
        Assert.True(wrongGrowth.Count == 0,
            $"real*8 cells that did not grow bit-identically to the Binary64-kind run: {string.Join("; ", wrongGrowth)}");
    }

    /// <summary>
    /// The exponent for <c>S = 2^k</c>: binary32 has a 24-bit mantissa, so the half-ulp of
    /// <c>2^k</c> in binary32 is <c>2^(k-24)</c>; choosing <c>k</c> so that <c>2^(k-24)</c>
    /// exceeds every term this run can add makes every such term round away entirely
    /// (<c>AddReal4</c>: <c>(double)(float)(S + term)</c> rounds back to exactly <c>S</c> whenever
    /// <c>term</c> is below half a binary32 ulp of <c>S</c>). <paramref name="largestCellValue"/>
    /// is a valid upper bound on the largest single term, not merely on their sum: every write
    /// site of a REAL*4-classified field adds a non-negative physical quantity -- a volume, a
    /// squared distance, a count (Particle/BOOT.md, "Real-valued record"; Attempt.Run never
    /// subtracts from one of these cells) -- so a cell's own final value, itself a sum of
    /// non-negative terms, is at least as large as any one of them. <see cref="SafetyMarginBits"/>
    /// covers the gap between that bound and the term itself, plus rounding-mode edge cases.
    /// </summary>
    private static int ChooseExponent(double largestCellValue)
    {
        Assert.True(largestCellValue > 0.0, "largestCellValue must be positive: ChooseExponent needs a real measured term to bound, not an assumed one.");
        return (int)Math.Ceiling(Math.Log2(largestCellValue)) + 24 + SafetyMarginBits;
    }

    private static void RunParticles(Engine engine, ref StreamSet streams)
    {
        for (var i = 0; i < ParticleCount; i++)
        {
            var status = engine.RunReferenceParticle(ref streams, MaxAttemptsPerParticle, out _);
            Assert.Equal(BatchStatus.Ok, status);
        }
    }

    private static IEnumerable<double> Cells(double[] record, RecordField field, ModelSetup setup)
    {
        var offset = field.Offset(setup.Layout);
        var length = field.Length(setup);
        for (var c = 0; c < length; c++)
        {
            yield return record[offset + c];
        }
    }

    // Since 2026-09-24 the generated file lists only the REAL*4 accumulators, keyed by Fortran
    // name, and `RealFourWriteSites.txt` maps each to its C# site (src/Particle/BOOT.md,
    // "Accumulators", "Design decision, 2026-09-24"). A record field rounds exactly when a
    // `ported:` row names it; every other field of `Fields` stays double. Every `ported:`
    // identifier that is an `AccumulatorLayout` record field must appear in `Fields`, so that
    // this table cannot silently miss a rounded field.
    private static Dictionary<string, bool> ParseGeneratedClassification()
    {
        var path = RepositoryPaths.Resolve("src", "Particle", "RealFourWriteSites.txt");
        var ported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var cells = line.Split('|', 2);
            Assert.True(cells.Length == 2, $"malformed site-map row: {line}");
            var disposition = cells[1].Trim();
            if (disposition.StartsWith("ported:", StringComparison.Ordinal))
            {
                _ = ported.Add(disposition["ported:".Length..].Trim());
            }
        }

        Assert.NotEmpty(ported);
        var recordFields = typeof(AccumulatorLayout).GetFields().Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var identifier in ported.Where(recordFields.Contains))
        {
            Assert.True(Fields.Any(f => f.Name == identifier),
                $"{identifier}: rounds under Original (RealFourWriteSites.txt) but is missing from this test's Fields table.");
        }

        return Fields.ToDictionary(f => f.Name, f => ported.Contains(f.Name));
    }

    private static void AssertStreamsEqual(StreamSet a, StreamSet b)
    {
        Assert.Equal(a.S1.Low, b.S1.Low); Assert.Equal(a.S1.High, b.S1.High);
        Assert.Equal(a.S2.Low, b.S2.Low); Assert.Equal(a.S2.High, b.S2.High);
        Assert.Equal(a.S3.Low, b.S3.Low); Assert.Equal(a.S3.High, b.S3.High);
        Assert.Equal(a.S4.Low, b.S4.Low); Assert.Equal(a.S4.High, b.S4.High);
        Assert.Equal(a.S5.Low, b.S5.Low); Assert.Equal(a.S5.High, b.S5.High);
        Assert.Equal(a.S6.Low, b.S6.Low); Assert.Equal(a.S6.High, b.S6.High);
    }
}
