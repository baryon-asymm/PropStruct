# HISTORY.md — Execution

Append-only store of what `BOOT.md` no longer needs to state as current truth
(`AGENTS.md`, §15). Newest entry first. Every entry names the date it was moved, the
`BOOT.md` section it came from, and carries the original text in full, unedited. The
start procedure (`AGENTS.md`, §10) does not read this file; it is reached only by
following a dated pointer left in `BOOT.md` at the place the text used to stand.

---

<a id="tier-table-budget-256"></a>

## 2026-10-01 — from "## GPU/CPU tier table (2026-09-18)" — the table at attempt budget 256

Moved when the table was re-measured at budget 8192, the new default. The text as it stood:

**After the fix**, re-measured with a QKS1-non-zero assertion added at every fork this
test and `DeterminismTests` build, so the fix cannot regress unnoticed here again
(`tests/Execution.Tests/EngineIntrospection`, reflection-based, diagnostic only —
`Engine`'s own `API.md` contract is unchanged). Confirmed reproducible: two separate
runs of the fixed code produced these same seven-significant-digit figures.

| Formulation | Observed max relative diff | Observed diverged share | Tier: relative tolerance | Tier: max diverged share |
|---|---|---|---|---|
| HPEPA3 | 2.372715E-14 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
| inpt | 8.482273E-15 | 0/1000 | 5E-13 | 3/1000 (0.30 %) |
| P33 | 5.406108E-14 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
| PSAN02n | 6.272130E-15 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |
| HMX | 1.118670E-14 | 0/2000 | 5E-13 | 3/2000 (0.15 %) |

---

<a id="tail-draw-sample-size-host-path-fix"></a>

## 2026-09-26 — from "## Design decisions (2026-09-18)" — the SampleSize host-path fix (D8), moved to make room for the R1 correction

Moved to make room under the §15 line limit for the architecture-audit finding R1
correction to "## Invariants" (the stream-layout refusal's withdrawal). The current
state — `SampleSize` calls `SizeLaw.Sample` directly on the host thread for every
engine kind — is unchanged and stated in full in the bullet above this pointer; this is
the already-closed note of the one bug found and fixed while implementing it.

> ⚠ 2026-09-24: this bullet's own words, "runs here on the host thread over CPU memory",
> were the design intent from the first sketch onward but not what the code did: the
> implementation compiled and launched `Kernels.SampleSize` through the same
> `KernelCache`/ILGPU-kernel-launch path `RunBatch` takes on a non-host-thread
> accelerator, even when the engine was itself CPU-bound (architecture audit finding D8,
> 2026-09-24). Fixed the same day: `SampleSize` now calls `SizeLaw.Sample` directly on
> this host thread for every engine kind, reusing the engine's own accelerator's memory
> when it is CPU-bound and an ephemeral CPU accelerator otherwise (CUDA memory is not
> host-addressable, and a refused binding has none at all) — never through a compiled
> launcher. `Kernels.SampleSize`, the now-unreferenced kernel wrapper, is removed:
> `SizeLaw.Sample`'s own CUDA-compilability was already exercised through `Attempt.Run`'s
> kernel compilation (it is called from inside an attempt too, not only for this tail
> draw), so the wrapper kernel proved nothing a dedicated test still needs
> (`tests/Execution.Tests/SampleSizeHostPathTests.cs`, proven red on the pre-fix code by
> reverting it and seeing the kernel-cache assertion fail).

---

<a id="host-thread-path-layout-correction-and-omission"></a>

## 2026-09-24 — from "## Host-thread path" — the ParticleControl packing correction and the records-not-padded omission

Moved to make room under the §15 line limit for the D8 correction note (BOOT.md, "The
tail draw of `PARAM` runs here"): the decision itself ("Figures are quoted only after
the layout is right") and its own acceptance stay in `BOOT.md`; this is the
already-closed implementation note that recorded the one correction and the one
omission found while carrying it out.

> Implemented 2026-09-19, with one correction to the plan and one omission, both
> declared here rather than silently: `Kernels.cs`'s new `ParticleControl` struct packs
> each particle's streams (96 bytes), attempt count and last outcome into one
> 128-byte (two-cache-line) slot, replacing the three separate `streams`/
> `attemptCounts`/`outcomes` arrays that used to pack 1 (streams already spanned more
> than one line), 8 and 64 particles per cache line respectively — the worst offenders,
> since those three fields are written once per particle per *launch*. `scratch`'s
> per-particle stride is rounded up to a whole number of 8-double (64-byte) cache lines
> on the host-thread path only (`Engine.RunParticlesCore`'s own `scratchStride`, passed
> to the now-shared `Kernels.RunAttempts`); the kernel-launch path keeps it tight, which
> is what CUDA's memory coalescing wants and has no OS-thread cache line to protect.
> **`records` is not padded**, unlike the plan's own wording ("records and scratch"):
> `Particle.Fold.AddField` reads it at the fixed `particle * recordLength + field`
> offset (`Particle/API.md`, "Fold by field"), which is not this node's contract to
> change, and a padded write buffer would need a compaction copy into a tight one before
> every fold — not attempted in this pass. `RecordLength` is large enough (HPEPA3: on
> the order of a hundred doubles) that only the two boundary cache lines per particle,
> not its whole record, could ever be shared with a neighbour, so this is a smaller
> residual than the one the `ParticleControl` change removes, not the dominant one.

---

<a id="host-thread-path-launch-structure-implementation"></a>

## 2026-09-21 — from "## Host-thread path" — how the launch structure carried over to host threads

Moved to make room under the §15 line limit for the new positive-control acceptance
criterion (BOOT.md, "Acceptance criteria"): the decision itself ("The launch structure
stays") and its acceptance criterion stay in `BOOT.md`; this is the implementation note
that confirmed it, already closed and unchanged since.

> Implemented 2026-09-19: `Engine.RunParticlesCore` keeps one `while` loop, one compacted
> `active` index list and one `Qks1Refresher.RefreshIfLoopsCompleted` call per iteration,
> exactly as before; only the three dispatch points (derive streams, run attempts, fold)
> now branch on `Engine`'s own `_hostThreads` field between the ILGPU kernel launcher and
> a `System.Threading.Tasks.Parallel.For` calling the very same `Kernels` method directly
> (`Kernels.RunAttempts`, `Kernels.DeriveStreams`, `Kernels.FoldField` — no second
> implementation of any of the three, root BOOT.md, Taboos).

---

<a id="host-thread-path-reference-mode-implementation"></a>

## 2026-09-21 — from "## Host-thread path" — how reference mode's own QKS1 refresh moved off the kernel launcher

Moved for the same reason as the entry above: the decision ("Reference mode is the
host-thread path at batch 1, budget 1, continued streams") and its measurement stay in
`BOOT.md`; this is the implementation note that confirmed it.

> Implemented 2026-09-19: `RunReferenceParticle` already called `Attempt.Run` directly on
> the host thread from before this decision; the one change is `Qks1Refresher.Recompute`,
> which now takes a `hostThreads` flag (set from `Engine`'s own) and calls
> `PocketHistogram.Normalize` directly instead of through `KernelCache` whenever it is
> set — the per-attempt kernel-launch cost the estimate above named. Re-measured:
> "## Host-thread throughput" below.

---

<a id="tier-table-derivation-narrative"></a>

## 2026-09-21 — from "## GPU/CPU tier table" — the derivation narrative behind the tier figures

Moved to make room under the §15 line limit for the new `Original`-stream-layout
refusal (BOOT.md, "Invariants"): the two decided figures themselves (5E-13 relative
tolerance, the rule-of-three diverged-share bound) and the table they apply to stay in
`BOOT.md`; this is the reasoning that produced them, not itself depended on going
forward.

> The post-fix figures genuinely differ from the pre-fix ones (as they must, since every
> bridge of cycle 1 now reaches `BridgeWindow`/`DM`/`VM` against a populated histogram
> instead of an empty one) while staying in the same order of magnitude — both are
> double-precision summation noise from a last-ULP libdevice/CoreCLR difference in some
> particle's draws over a whole cycle's worth of accepted particles (root BOOT.md,
> "Deterministic under every schedule"), the fix changed which particles' draws produce
> that noise, not its scale. The observed columns are one run of `TierTableTests` against
> a deliberately loose (1.0, 1.0) placeholder tier (AGENTS.md §6, "a criterion with the
> quantifier all is checked against a list generated by the machine, not typed by hand" —
> the table is the by-hand record of what that run found, not a substitute for it: the
> actual pass condition is `TierTableTests.Tiers`). The tier columns are derived, not
> copied from the observed ones: the relative tolerance is one figure for all five
> formulations, since their worst observed diffs still cluster within one order of
> magnitude (6.3E-15 to 2.4E-14, tighter than the pre-fix spread) — 5E-13, a margin of
> roughly 21x over the actual worst observed figure (HPEPA3), tightened from the pre-fix
> 1E-12 now that a genuine post-fix measurement exists to derive it from; the
> diverged-share tier is unchanged (same sample sizes, still zero observed divergences):
> the rule-of-three upper confidence bound for zero observed successes in the sample size
> actually drawn (`3/n`, ~95% confidence), not the literal `0` every formulation showed,
> since typing `0` would fail the very next run that observes a single divergent particle
> by chance and would not be a bound at all. Neither figure moves without a new
> measurement.

---

<a id="reference-mode-record-seeding"></a>

## 2026-09-21 — from "## Invariants" — the record starting at zero left `Original` accumulation with no measurable effect

Moved because it is provenance for a wiring defect and its fix, not a claim `BOOT.md` depends on going forward: the current rule (the record starts as a copy of the real totals under `Original`, and is written back over them on acceptance, instead of folding a zero-seeded delta) already stands in `BOOT.md`, "Reference mode".

> ⚠ 2026-09-21: this document first read "`RunReferenceParticle` never checks `Kind`:
> reference mode is sequential by construction and the root's invariant does not
> restrict it", and "Reference mode" read "then folds the particle's record"
> unconditionally, for every kind. Both were true of the code as merged, and both were
> the defect: the root's own acceptance-chain report found `--accumulation original`
> and `--accumulation double` producing `results.m` files identical but for the
> footer's own descriptive sentence, on all five reference formulations. `Attempt.Run`'s
> own write sites did round under `Original` (proven separately, `Particle`'s own
> accumulation-kind contract), so the option reached the particle program; it just never
> reached a magnitude a printed digit could show, because every particle's record
> started at zero and was folded, once, into the real totals in plain `double` — a few
> dozen terms rounded against a magnitude near zero, then added onto the run's true
> totals with no further rounding at all. The original has no such reset: one REAL*4
> memory location, written by every attempt of every particle of the whole run, in
> order. `RunReferenceParticle` now reads `_setup.Kind` once per particle to choose: under
> `Double`, unchanged (zero, then `Fold.Add`); under `Original`, the record is seeded
> from `_realTotals` before the particle's first attempt and copied back over
> `_realTotals` on acceptance, in place of `Fold.Add` — safe only because `Original`
> accumulation is refused for every batched route (`RunBatch`/`RunContinuedBatch`,
> unchanged by this fix) and reference mode is therefore strictly sequential, one
> particle's commit always complete before the next particle's first attempt reads the
> totals it seeds from. `Attempt.Run`'s own write sites are untouched: rounding still
> happens only at the accumulator write sites `RealFourAccumulators.generated.txt`
> names, inside `Particle`; this fix is entirely this node's own orchestration of what
> the record starts from and how it is committed. Measured after the fix, reference
> mode, seed 0: P33's worst `Allvdok` cell moved from a 3×10⁻⁸ relative difference
> (invisible at print precision) to 7.8×10⁻⁴ (moves printed digits); HMX's worst cell
> moved from 1.9×10⁻⁸ to 0.65 — inside the 46-65% worst-cell range `Particle/BOOT.md`'s
> own persistent-accumulator shadow measurement recorded for `Allvdok`/`Vdokstr` on
> HPEPA3/HMX. Guarded by
> `tests/Simulation.Tests/AccumulationKindTests.cs`,
> `Run_OriginalAccumulation_ProducesPrintedOutputThatDiffersBeyondTheFooter`, proven
> non-degenerate by reverting the seeding and observing it fail with 0 differing lines
> outside the footer, exactly the reported defect.

---

<a id="qks1-timing-correction"></a>

## 2026-09-20 — from "## Invariants" — the Opus audit's "in reference mode after every attempt" → "before" correction

Moved because it is provenance for a wording correction the audit made, not a claim `BOOT.md` depends on going forward: the current rule ("before every launch, and in reference mode before every attempt") stays in `BOOT.md` next to the pointer, already stated correctly.

> ⚠ 2026-09-18, the Opus audit: this line first read "in reference mode after every
> attempt", contradicting the code (`RunReferenceParticle` has refreshed before every
> attempt since the QKS1-timing design correction of the same day, "Reference mode is
> batched mode degenerated" — moving it to "after" broke bit-parity with a continued
> batch at particle 0, per this node's own earlier finding). Judged equivalent at
> particle and cycle boundaries by the audit (`Qks` is written only at Fortran line 676,
> nothing reads QKS1 between particles, `Statistics` never rewrites `Qks`) and left as
> "before" rather than changed to match the wrong prose, since "before" is what
> `tests/Execution.Tests/ReferenceModeEqualsContinuedBatchTests` and
> `Qks1RefreshUnderCycle1Tests` both verify bit-for-bit against a continued batch.

---

<a id="batched-derivation-stream-seeds"></a>

## 2026-09-20 — from "## Invariants" — why batched derivation moved from `ForParticle` to `ForBatchedParticle`

Moved because it is the diagnosis that motivated the fix, not the fix itself: the current rule (a batch derives streams through `StreamSeeds.ForBatchedParticle`, reference mode and continued batches never derive) stays in `BOOT.md` next to the pointer.

> ⚠ 2026-09-19: this line first read `StreamSeeds.ForParticle(layout, seed, firstOrdinal
> + index)`, the same per-particle jump `RunContinuedBatch`/`RunReferenceParticle` use.
> The first run of the port's own `results.m` through the statistical criterion found
> batched mode's `Original` layout failing two to five times more cells than reference
> mode on every reference formulation (`tests/Simulation.Tests/BOOT.md`, "Statistical
> criterion measurement"); root `BOOT.md`, "Execution model", traces the cause to a
> common jump recreating the original's start-of-run-only cross-stream lags inside every
> particle. `Kernels.DeriveStreams` (the sole call site: `RunContinuedBatch` and
> `RunReferenceParticle` continue an already-derived `StreamSet` and never call either
> `ForParticle` method) now calls `StreamSeeds.ForBatchedParticle`, which adds the
> role-group offset of `src/Random/BOOT.md`, "Batched derivation". Reference mode and
> continued batches are unaffected: they do not derive.

---

<a id="reference-mode-qks1-timing-correction"></a>

## 2026-09-20 — from "## Invariants" — the Opus audit's twin "after each attempt" → "before" correction on Reference mode

Moved because it is the twin of "qks1-timing-correction" above, corrected for the same reason on the same day: the current rule ("before each attempt") already stands in `BOOT.md`.

> ⚠ 2026-09-18, the Opus audit: this line first read "after each attempt", the same
> error as the "QKS1 is the engine's" bullet above and corrected for the same reason;
> see that bullet's ⚠ for the evidence and why "before" is what stands.

---

<a id="libdevicelocator-override-restored"></a>

## 2026-09-20 — from "## Constraints" — why the APThermo direct-path override was restored under new env var names

Moved because it is the provenance of a since-implemented fix: the current behaviour (both env vars set and found is used directly; both set and either missing fails without falling back; one set or neither leaves discovery unchanged) is the fact a caller needs, and it is restated in `BOOT.md` next to the pointer.

> ⚠ 2026-09-18, the Opus audit: "namespace changes only" was not quite true —
> APThermo's own `LibDeviceLocator` lets a caller name the libnvvm and libdevice files
> directly, bypassing toolkit-root search entirely, and the port dropped that override
> without noting it. Restored as `PROPSTRUCT_LIBNVVM_PATH`/`PROPSTRUCT_LIBDEVICE_PATH`:
> both set and found is used directly; both set and either missing fails without falling
> back to search (an explicit override is checked and refused on its own terms, not
> silently ignored); one set or neither leaves discovery unchanged. A user with a
> non-standard toolkit layout needs this, and the search below finds nothing for it
> otherwise; `LibDeviceLocatorTests` covers all four cases.

---

<a id="throughput-second-look"></a>

## 2026-09-20 — from "## Throughput measurement" — the "second look": the withdrawn "finding for `Particle`"

Moved because it is a withdrawn diagnosis, superseded in full by the "third look" correction that follows it: keeping both in `BOOT.md` would read as two live findings where only one stands. The corrected root cause and fix are the current truth and stay in `BOOT.md` ("third look").

> ⚠ 2026-09-18, second look: this section first went on to read the 262.75
> attempts-per-particle figure as intrinsic to HPEPA3 ("the neighbour loop and the
> post-loop acceptance tests restart a particle roughly 262 times on average"), and to
> recommend tuning the attempt's own draws. That claim is withdrawn: the coordinator
> found it does not add up against the reference's own printed counters
> (`tests/Fixtures/references/HPEPA3/results.m.txt`, lines 29–54: `NFX = 1336594` over
> `Nbase = 100000 + 100000` accepted particles, i.e. ≈ 6.68 attempts/particle over both
> cycles combined, not 262.75) and asked for a diagnosis. Three hypotheses, checked in
> this order, with the evidence (`ReferenceFormulationDriver`-style diagnostics run once
> in this session and discarded, since they exist only to answer this question, not to
> guard it going forward — the finding below is what is kept):
>
> 1. **A counting artifact** — ruled out. `BatchCounters.Attempts` (the host loop's own
>    tally of `Attempt.Run` calls) equals the port's own `Nfx` integer total exactly in
>    every run checked (e.g. 6,738 both ways over 1000 particles at ordinals
>    100000–100999 under cycle-0-like inputs): "attempts" in the throughput table is the
>    same quantity the reference calls `NFX`, counted the same way.
> 2. **Different model parameters** — ruled out. Every field of `ModelParameters.Default`
>    (the parameters `ReferenceFormulationDriver` and `ThroughputMeasurementTests` pass to
>    `Statistics.Setup.Prepare`) matches the reference's own parameter echo exactly,
>    `Variant` (`ivar`) included: `Dmin`/`Di`/`Dj` = 10 µm, `EpsDok` = 0.05, `Alpha` (k5) =
>    0.25, `PocketCoefficient`/`BridgeCoefficient` (k7/k8) = 8.2/7.73, `TailProbability`
>    (alfa) = 0, `NnMin`/`NnMax` = 3/100, `Variant` (ivar) = 0, `AggregatedOxideFraction`
>    (eta) = 0 — all of it against `results.m.txt` lines 10–20. The formulation read from
>    `HPEPA3.dat` matches the echo too (`Plot1`/`Plot2`/`Gdok`/`Gm`, both fractions'
>    bounds and mass shares).
> 3. **A real defect** — confirmed, and localized. Driving HPEPA3's cycle 0 (100,000
>    particles) and then its cycle 1 (100,000 particles) with the engine and reading the
>    integer totals at both points:
>
>    | | cycle 0 alone | cycle 1 alone | reference (both cycles) |
>    |---|---|---|---|
>    | particles | 100,000 | 100,000 | 200,000 |
>    | `Nfx`/particle | 6.6475 | 262.75 | 6.68297 (`NFX`) |
>    | Conditions[3] (`Dbase<0.5Dok`)/particle | 5.6475 | 60.53 | 5.67671 |
>    | Conditions[4] (`Dbase>2Dok`)/particle | 526.73 | 1256.31 | 446.3851 |
>    | `LoopCompletions`/particle | 1.0000 | 1.0234 | — |
>    | `IbridgeTotal`/particle | 0 | 4.3462 | 4.363190 (medium bridges) |
>
>    Cycle 0 alone already reproduces the reference closely on every row that is defined
>    in cycle 0 (`Nfx`, condition 3, condition 4, all within a few per cent — the
>    agreement root BOOT.md's "Statistical reference criterion" expects from the
>    `Original` layout against x87/REAL*4 drift, not the ≈40× gap cycle 1 shows). Cycle
>    1's own bridge count per accepted particle (4.35, from `IbridgeTotal`) also matches
>    the reference's 4.363 closely — the particles cycle 1 *does* accept are the right
>    kind of particle. The defect is not in what gets accepted; it is in how many whole
>    attempts it takes to accept one.
>
>    Isolating the cause (four engines, same ordinal range 100000–100999, same 1000
>    particles, only the cycle inputs varied):
>
>    | Engine | `CycleFlag` | `Dmaxxx` | `Pdoksmall` | QKS1 seed totals | Attempts/particle |
>    |---|---|---|---|---|---|
>    | A | 0 | 0 | zeros | zero (fresh `Load`) | 6.738 |
>    | D | 0 | 0 | zeros | cycle 0's real totals | 6.738 |
>    | C | 1 | 0 | zeros | cycle 0's real totals | 262.747 |
>    | B | 1 | real (0.000315) | real | cycle 0's real totals | 262.747 |
>
>    A and D are identical (QKS1's own seed content changes nothing under `CycleFlag =
>    0`); C and B are identical (`Dmaxxx` and `Pdoksmall` change nothing under `CycleFlag
>    = 1`, real values or not). The explosion tracks `CycleFlag` alone, not any value this
>    node computes or passes — `Execution` derives, uploads and never mutates `Dmaxxx`,
>    `Pdoksmall` and QKS1 correctly (BOOT.md, "QKS1 is the engine's"; `Statistics/API.md`,
>    "Setup hand-off"), and this session's own wiring of them is cleared.
>
>    The condition breakdown of B against A over the same 1000 particles narrows it
>    further: `LoopCompletions`/particle rises from 1.0 (A) to 101.0 (B) — under
>    `CycleFlag = 1` an accepted particle completes the whole neighbour loop about 101
>    times on average, not once, and only 4,340 of those 101,022 completions (≈4.3%) end
>    in a committed bridge (`IbridgeTotal`); the rest are discarded and the whole attempt
>    restarts (`RestartedAfterLoop`). None of the nine tallied `Conditions` cells grows in
>    proportion to this (conditions 6–9, the pocket/`Nmkm`-count checks that would
>    plausibly gate such a restart, fire on 22 and 18 of the 101,022 completions — not the
>    ≈96,700 that go unaccounted for). This is a **finding for `Particle`, not a defect of
>    this node**: a post-neighbour-loop, `CycleFlag = 1`–only acceptance test — not one of
>    the nine named conditions — discards a loop completion and forces
>    `RestartedAfterLoop` at a rate the reference does not show, most likely at or near
>    the "bridges restart on the empty window" mechanism the coordinator named (Fortran
>    line 590); `Particle`'s own `BOOT.md`/Fortran transcription is the place to confirm
>    the exact line, and only `Particle` may act on it (AGENTS.md §3, §11 — this node
>    neither reads nor edits `Particle`'s implementation).

---

<a id="throughput-third-look"></a>

## 2026-09-20 — from "## Throughput measurement" — the "third look": root cause in `Engine.WriteTotals`, and the fix

Moved because it is the diagnostic narrative (the coordinator's own arithmetic, the launch-by-launch measurement, the QKS1 download) that led to the fix, not the fix's lasting shape: the fact that matters going forward — `WriteTotals` now refreshes QKS1 unconditionally, proven by `WriteTotalsRefreshesQks1Tests` — is short enough to restate in the pointer itself.

> ⚠ 2026-09-18, third look — **the "finding for `Particle`" above was wrong; the defect
> was in this node.** The coordinator's own second read of the same numbers noticed
> 262.75 = 256 (`attemptsPerLaunch`) + 6.75 (the original's own rate) exactly, with both
> accelerators needing exactly two launches — the signature of a launch that burns its
> whole budget with (almost) nothing accepted, followed by a launch that behaves
> normally, not of a genuinely expensive attempt. They also read `Particle/Attempt.cs`
> against the Fortran directly (their own subtree includes it) and found every
> `RestartedAfterLoop` increments one of `Conditions[5..8]` — so "restarts no condition
> accounts for" cannot come from `Attempt.Run` at all, ruling out the "finding" above on
> its own terms. Asked to measure launch 1 and launch 2 of HPEPA3's cycle 1 separately:
>
> | | launch 1 alone (capped at `attemptsPerLaunch`) | launch 2 alone (by subtraction) | reference (both cycles) |
> |---|---|---|---|
> | status | `AttemptCapExceeded` | (part of `Ok`) | — |
> | attempts | 25,600,000 (= 100,000 × 256, exactly) | 675,184 | — |
> | attempts/particle | 256.0000 (every one of 100,000 particles) | 6.7518 | 6.68297 |
>
> Not one particle was accepted in launch 1 — every single one used its entire budget.
> Repeating with `attemptsPerLaunch = 16` and `1024` (production `maxAttemptsPerParticle`,
> letting the batch relaunch normally): 22.7448/particle and 1030.7229/particle, against a
> predicted `attemptsPerLaunch + 6.75` of 22.75 and 1030.75 — matching to three
> significant figures at every budget tried. The attempts per particle track the launch
> budget, exactly as the coordinator predicted; the particle is not being recognized as
> accepted inside launch 1, or rather: nothing gets accepted inside launch 1, and QKS1
> tells why. Downloading it directly (`Engine`'s own `_qks1` buffer, by reflection —
> diagnostic only, not a permanent access path) right before cycle 1's launch 1 showed it
> all zero (`sum = 0`, 0/150 cells nonzero), unchanged after launch 1 returns; the same
> totals run independently through `Particle.PocketHistogram.Normalize` (the function
> `Execution` already calls to build QKS1, called directly here as a check, not a second
> implementation) produced a properly populated histogram (`sum = 1`, 80/150 cells
> nonzero) — QKS1 *should* have been that, and would have been, had it been refreshed.
>
> **Root cause, in `Engine.WriteTotals`.** Cycle 0 finishes in a single launch (its own
> 6.6 attempts/particle is far under the 256 budget), so `MaybeRefreshQks1`'s
> before-a-launch check — the only place QKS1 is ever refreshed — runs exactly once, when
> `LoopCompletions` was still 0 from `Load`, and never again: QKS1 stays `Normalize` of
> the *zero* totals for the whole of cycle 0, harmlessly, since cycle 0's own `CycleFlag =
> 0` branch does not depend on it. `WriteTotals` (called between cycles, carrying
> `Statistics`' in-place rewrite of the totals back onto the engine) then resynced
> `_lastRefreshLoopCompletions` to the *just-written* `LoopCompletions` (≈100,000) without
> ever calling `RefreshQks1Now` — marking QKS1 "fresh as of 100,000" while its buffer
> still held `Normalize` of the *zero* totals from `Load`. Cycle 1's `MaybeRefreshQks1`
> then saw "no change since the last refresh" and left it exactly there. Every attempt
> under `CycleFlag = 1` samples a bridging window against this empty histogram and finds
> it empty — the mechanism the coordinator named, Fortran line 590, in `Particle`, is
> real, but it was being fed a QKS1 `Execution` never populated, not a QKS1 `Particle`
> computed wrongly. The earlier isolation experiment (engines A/B/C/D, "## Throughput
> measurement" above) never actually varied QKS1 despite believing it did: every one of
> those four engines went through `Load` → `WriteTotals` → `SetCycle`, and the very same
> missing refresh left all four with the identical (zero) QKS1 regardless of which totals
> were written — so "swapping QKS1's seed content changes nothing" was true, but for the
> wrong reason: nothing was ever actually being swapped.
>
> **Fixed**: `WriteTotals` now calls `RefreshQks1Now` before resyncing the bookkeeping
> (`src/Execution/Engine.cs`), so QKS1 is unconditionally recomputed from whatever totals
> were just written — matching how `Load` already treats a freshly zeroed run — rather
> than trusting the totals' own `LoopCompletions` value as a proxy for "QKS1 is already
> right", which only held when the totals were built up incrementally by this same
> engine's own launches, not replaced wholesale. Regression test:
> `tests/Execution.Tests/WriteTotalsRefreshesQks1Tests.WriteTotals_LeavesQks1EqualToNormalizeOfTheWrittenTotals`
> — red on the code before this fix (asserted `Normalize` of freshly written, nonzero
> `Qks` cells; got the all-zero QKS1 of a fresh `Load` instead), green after, without
> running a single real particle (crafted totals, not a driven cycle 0). `Particle` was
> not touched; the "finding for `Particle`" above is withdrawn, not deleted, so anyone who
> acted on it can see what changed and why.

---

<a id="throughput-build-configuration-note"></a>

## 2026-09-20 — from "## Throughput measurement" — neither earlier table named its build configuration, and it mattered

Moved because it is the diagnosis of a measurement-methodology gap in the two tables above it (both already moved to `HISTORY.md`), not a claim the document still needs standing on its own: the corrected, Release-build figures live in "## Host-thread throughput" in `BOOT.md`.

> ⚠ 2026-09-19: neither this table nor the one above it named the build configuration
> `dotnet test` used, and it matters for every CPU-executed figure in it (not CUDA's).
> The coordinator's review of "## Host-thread throughput" below found the host-thread
> path's own single-thread figure inexplicably far under the wave-7 prototype's; the
> diagnosis (same section) traced it to `dotnet test`'s default `-c Debug`, which
> disables JIT optimization for `PropStruct.Particle`/`PropStruct.Random` — the assembly
> `Attempt.Run` itself lives in — regardless of which mechanism calls it. Reproducing
> this table's own CPU row with `Engine.Create(..., forceIlgpuKernelsOnCpu: true)`
> (`ThroughputMeasurementTests.Cpu_Throughput_OnHpepa3sCycle1`, now pinned to the
> accelerator explicitly, since `AcceleratorKind.Cpu`'s default changed underneath it):
> **4,091 particles/s in `-c Debug`** (matches the 4,190 above, confirming it was a Debug
> figure all along) versus **11,279 particles/s in `-c Release`** — a genuine ≈2.75×
> difference on the *same* ILGPU-kernel-launch path this table has always measured, not
> only on the host-thread path added afterward. CUDA showed no such sensitivity
> (39,311/s `-c Debug` vs. 39,101/s `-c Release`, this machine, same run): its kernel
> compiles through NVVM/PTX, never through the .NET JIT that `-c Debug` de-optimizes.
> The 1,522/4,190 CPU figures above are correctly attributed to the CPU accelerator and
> not otherwise wrong — they were real, reproducible measurements of that path — but the
> `≈13.5×`/`≈10.4×` "CUDA advantage" framing understates the CPU accelerator's own
> Release-build capability (CUDA/CPU-accelerator-Release ≈ 3.5×, not ≈10.4×) and should
> not be read as a durable performance ceiling. Not re-measured further here: this
> table's own job (the record before any kernel is tuned) is done; "## Host-thread
> throughput" below is where the currently-relevant CPU figures live, correctly labelled
> by build configuration.

---

<a id="tier-table-fourth-look"></a>

## 2026-09-20 — from "## GPU/CPU tier table" — the "fourth look": the tier table's own re-measurement was owed, not optional

Moved because it is the provenance of why the tier table was re-measured, not the re-measurement itself: the current tier table ("After the fix", still in `BOOT.md`) is what a reader needs.

> ⚠ 2026-09-18, fourth look: this paragraph first claimed the tier table "needed no new
> figures" because the re-measured diffs "stayed within the already-recorded tiers", and
> left the tier table's own section unchanged with the pre-fix numbers still in it. The
> coordinator caught this: the pre-fix and post-fix tables cannot show the *same* figures
> to seven significant digits, because before the fix no bridge of cycle 1 ever reached a
> populated QKS1 window on either accelerator, and after it every one does — identical
> relative differences over a materially different computation is not plausible, and
> finding it exactly would have been the thing needing an explanation, not a quiet "no
> change needed". The re-measurement itself was real (the numbers this paragraph quoted
> were the true post-fix ones), but they were never written into the section whose whole
> purpose is to hold them, and the tolerance was left at its pre-fix value by omission
> rather than by a checked decision to keep it. Both are fixed in "## GPU/CPU tier table"
> below, with the fork's QKS1 now asserted non-zero at every point this test and
> `DeterminismTests` write one, so the same defect cannot pass unnoticed through this
> table again.

---

<a id="host-thread-path-coordinators-review-preview"></a>

## 2026-09-20 — from "## Host-thread path" — the coordinator's review, forward reference (see the full review below)

Moved because it only forward-references the full diagnosis kept in `BOOT.md` under "## Host-thread throughput": once that pointer is read there is nothing this paragraph adds.

> ⚠ 2026-09-19, the coordinator's review: this paragraph first read the measurement
> above as landing at "the *low* end of the prototype's 27 000–43 000/s spread" with
> single-thread throughput "well short of the prototype's ... 9 456/s", and attributed
> the shortfall to the layout fix not being enough. That measurement was taken through
> `dotnet test`'s default `-c Debug`; re-measured `-c Release`, the figures match the
> prototype's own (1 thread 8,754–8,911 against 9,456; 16 threads 34,621–42,720,
> overlapping the prototype's own noisy 27,167–43,343) — the layout fix was never
> the thing to re-examine. Full diagnosis, corrected figures and what was fixed
> (`ThroughputMeasurementTests`' own CPU row, which had the identical Debug-vs-Release
> gap already) are in "## Host-thread throughput" below.

---

<a id="host-thread-throughput-coordinators-review"></a>

## 2026-09-20 — from "## Host-thread throughput" — the coordinator's review: the first cut was wrong, and the real cause

Moved because it is the bisection narrative (reproducing the prototype, three staged measurements, the root-cause paragraph, the withdrawal and the fix) that led to the corrected, `-c Release` table already in `BOOT.md` above the pointer: the table is the current truth, this is how it was found to be trustworthy.

> ⚠ 2026-09-19, the coordinator's review: the first cut of this section reported 3,157
> (1 thread), 10,805/18,570/27,969 (4/8/16), and 3,426 (reference mode) — three- to
> fourfold under the figures above — and read the gap as atomic contention on the shared
> integer totals. The coordinator rejected that reading (atomic contention cannot explain
> a *single-thread* loss, and reference mode's own shortfall against the prototype's
> "same routine" claim needed a shared explanation) and asked for a diagnosis in this
> order: reproduce the prototype's figure first, in the same build configuration as the
> original measurement; bisect the difference stage by stage; fix what is this node's to
> fix and re-measure.
>
> **1. Reproducing the prototype.** A throwaway test (`Wave8ScratchMeasurement.cs`,
> deleted before commit, per the assignment) ran the *plainest possible* loop: one
> `CpuHost` accelerator, `Attempt.Run` called directly in a `for` loop over 30 000 of
> HPEPA3's real cycle-1 particles, one attempt at a time until `Accepted`, no `Engine`, no
> `Kernels`, no `Parallel.For` — the same shape the wave-7 prototype's own item 2
> describes. Run through `dotnet test` (`-c Debug`, the default the root `CLAUDE.md`
> command uses): **2,964 particles/s.** Run through `dotnet test -c Release`, same code,
> same particles: **8,748 particles/s** — within 8% of the prototype's own 9,456. **The
> comparison in the first cut of this section was wrong**: it compared a `-c Debug` figure
> of this node's own measurement against a `-c Release`-equivalent figure of the
> prototype's, not two measurements of the same thing.
>
> **2. Bisection.** Three stages between the raw loop and the full engine, same 30 000
> particles, same machine, both configurations:
>
> | Stage | `-c Debug` | `-c Release` |
> |---|---|---|
> | Raw loop (`Attempt.Run` direct, no `Engine`/`Kernels`/`Parallel.For`) | 2,964/s | 8,748/s |
> | `Kernels.RunAttempts` called directly, one call per particle, no `Parallel.For` | 2,698/s | 7,682/s |
> | `Parallel.For` with `MaxDegreeOfParallelism = 1` | 2,811/s | 7,424/s |
> | Full `Engine.RunBatch`, `cpuThreads: 1` (100 000 particles) | 2,970/s | 8,079/s |
>
> Every stage moves together with the build configuration and stays within noise of every
> other stage *in the same configuration*: the `ParticleControl` read/write, the
> `Kernels.RunAttempts` indirection and `Parallel.For`'s own dispatch overhead each cost
> at most a few percent, not the 3× the coordinator's arithmetic predicted. No stage
> inside `Engine`'s own code explains the gap — it was never there to fix.
>
> **3. Root cause.** `dotnet test`'s default configuration is `-c Debug`
> (confirmed from the actual `csc` invocation: `/define:TRACE;DEBUG;...`), which the .NET
> SDK compiles with `Optimize=false` — a `DebuggableAttribute(IsJITOptimizerDisabled:
> true)` on every assembly built that way, `PropStruct.Particle` (where `Attempt.Run`
> lives) and `PropStruct.Random` included. The JIT honours that attribute on the
> *defining* assembly regardless of which mechanism calls into it — an ILGPU-compiled
> kernel delegate and a `Parallel.For` worker both eventually call the same
> `Attempt.Run` method, so both pay the same de-optimization once `PropStruct.Particle`
> was built `-c Debug`. This is not new to the host-thread path: reproducing the
> *existing* "## Throughput measurement" table's own CPU-accelerator row with
> `forceIlgpuKernelsOnCpu: true` (`ThroughputMeasurementTests.Cpu_Throughput_OnHpepa3sCycle1`,
> now pinned — see its own ⚠ above) gives 4,091/s `-c Debug` (matching the 4,190 recorded
> there in 2026-09-18) against 11,279/s `-c Release` — the *original*, pre-host-thread
> figure was itself a Debug number, on the ILGPU-kernel-launch path this table has always
> measured. CUDA is the one path immune to this: 39,311/s `-c Debug` vs. 39,101/s
> `-c Release` (this session, same machine) — its kernels compile through NVVM/PTX, never
> through the .NET JIT that `-c Debug` de-optimizes.
>
> **The "atomic contention" reading is withdrawn.** No contention was ever actually
> measured (the first cut named it as "the most likely remaining cost, not measured
> directly"); the diagnosis above accounts for the entire gap without it. Nothing in
> `Particle` or `Random` needed a change: the cost was never in their code, only in how
> the assembly containing it was built for the measurement.
>
> **Fixed in this pass:** `ThroughputMeasurementTests.Cpu_Throughput_OnHpepa3sCycle1` now
> passes `forceIlgpuKernelsOnCpu: true` explicitly, so it keeps measuring the accelerator
> path its own name and this file's "## Throughput measurement" section describe, rather
> than silently switching to the host-thread path the moment `AcceleratorKind.Cpu`'s
> default changed. Bit identity is unaffected by build configuration (checked: both
> `HostThreadOracleTests` and the matrix pass under `-c Release` too, same as `-c Debug`)
> — this was a measurement-methodology gap, not a correctness one.
>
> **Scaling, from the corrected table:** sub-linear past 4 threads and noisy at 16 (as the
> wave-7 prototype's own repeats already showed at this scale) — consistent with 16
> *logical* CPUs on what the root `BOOT.md` platform note calls a 16-logical-CPU machine
> (typically 8 physical cores under hyper-threading): the second thread on each physical
> core adds less than a whole core's worth of throughput. Reference mode (7,245–7,797)
> sits below the raw-loop figure (8,748) by an amount consistent with its own per-attempt
> QKS1-refresh check (`_qks1Refresher!.RefreshIfLoopsCompleted`/`ReadLoopCompletions`,
> called every attempt, root `BOOT.md`: "the pocket histogram QKS1 is refreshed after
> every attempt that completes the neighbour loop"), which batched mode's per-*launch*
> check does not pay — a real, small, already-required cost of reference mode's own
> semantics, not a defect.
>
> **Not chased further in this pass**, per root `BOOT.md`'s "Performance" constraint
> (records figures, defers kernel restructuring until after the port is accepted): why
> 16-thread throughput is noisier than 8-thread across repeats (both this session's and
> the prototype's own), and whether `-c Release`'s own JIT settings (tiered
> compilation/PGO warm-up) shift the picture further on a longer-running batch.

---

<a id="design-decisions-first-sketch-correction"></a>

## 2026-09-20 — from "## Design decisions" — the first sketch's `RunBatch`/`RunReferenceAttempt` shape did not work

Moved because it is provenance for a design already settled and stated above it in `BOOT.md`: the run-scoped engine is the current truth, this note is only why the first sketch did not work.

> ⚠ 2026-09-18: the first sketch's `RunBatch` took `in CycleInputs` and host `Span`s of the
> totals, and `RunReferenceAttempt` asked its caller to refresh QKS1. `CycleInputs` holds
> `ArrayView`s that a caller without an accelerator cannot build, the fraction table was
> missing from the signature, the stream layout was missing, and the caller cannot refresh
> QKS1 between relaunches it does not see. Replaced by the run-scoped engine above.

---

<a id="host-thread-matrix-coverage"></a>

## 2026-09-20 — from "## Host-thread path" — the bit-identity matrix's coverage rationale

Moved because it is the detailed justification of a test's coverage choices (an
"experiment record" in `AGENTS.md` §15's sense), not a claim `BOOT.md` itself depends
on going forward; the current, load-bearing fact — the matrix passed — stays in
`BOOT.md` next to the pointer.

> Implemented and measured 2026-09-19: `tests/Execution.Tests/HostThreadMatrixTests`
> forks each of the five reference formulations' cycle-1-ready state onto an
> ILGPU-kernel-launch oracle engine (`Engine.Create(..., forceIlgpuKernelsOnCpu: true)`)
> and a host-thread engine (the new default), and compares integer and real totals bit
> for bit: 120 combinations of (formulation × 1/4/8/16 threads × batch 1/7 ×
> `attemptsPerLaunch` 1/2/256), plus a whole-cycle row per formulation at 16 threads.
> The whole-cycle row is not repeated at every thread count: `DeterminismTests` already
> proves the host-thread path itself is bit-identical across 1, 4 and 16 threads, so a
> wrong host-thread dispatch is what this matrix is checking for, and the small-batch
> rows already vary the thread count directly — repeating the (expensive, oracle-side)
> whole-cycle run at every thread count would only re-confirm that same determinism, not
> test anything new. All green.

---

<a id="grep-complete-fork-audit"></a>

## 2026-09-20 — from "## GPU/CPU tier table" — the grep-complete fork audit

Moved because it is a one-time coverage audit (2026-09-18) confirming which tests
exercise the post-fix bridge path, not a claim the tier table itself depends on; the
tier table's own current figures ("After the fix" above the pointer) stand on their
own without it.

> **Every test in `Execution.Tests` that forks a cycle-1 state** (`grep`-complete: every
> call site of `WriteTotals` or `SetCycle(cycleFlag: 1, ...)` /
> `SetCycle(1, ...)` in the project, 2026-09-18):
>
> | Test | How it forks | Runs the bridge path after the fix? |
> |---|---|---|
> | `ReferenceFormulationDriver.PrepareThroughCycle0` | drives cycle 0 on its own engine, then `WriteTotals` + `SetCycle(1, ...)` on that same engine | yes — the shared entry point every row below builds on; asserts QKS1 non-zero right there |
> | `ThroughputMeasurementTests.Measure` | uses the driver's own engine directly, no second fork | yes, inherits the driver's assertion |
> | `TierTableTests` | four fresh engines (`cpu`, `cuda`, `cpuSample`, `cudaSample`), each its own `Load` + `WriteTotals` + `SetCycle(1, ...)` | yes, each now asserts QKS1 non-zero individually |
> | `DeterminismTests` | four fresh engines (thread counts 1, 1, 4, 16), each its own `Load` + `WriteTotals` + `SetCycle(1, ...)` | yes, each now asserts QKS1 non-zero individually |
> | `ReferenceModeEqualsContinuedBatchTests` | neither: both its engines run `SetCycle(cycleFlag: 0, ...)` only, never reach cycle 1 | not applicable — this test is entirely a cycle-0 comparison |
>
> No other test forks a cycle-1 state; `BatchStatusTests` and `Qks1RefreshTests` use
> constructed setups at `cycleFlag = 0` only, and `AcceleratorTests`/`MathProbeTests`/
> `FoldByFieldTests` never call `WriteTotals` or `SetCycle` with a real fork at all.

---

<a id="tier-table-before-the-fix"></a>

## 2026-09-20 — from "## GPU/CPU tier table" — the pre-fix table

Moved because it measures a run that no longer exists: every fork this test built went
through the `WriteTotals` QKS1 defect ("## Throughput measurement", "third look", still
in `BOOT.md`), so the numbers below are the tier table run against an all-zero pocket
histogram on both accelerators. The current, post-fix table stays in `BOOT.md`.

> **Before the `WriteTotals` fix** (BOOT.md, "## Throughput measurement", "third look"):
> every fork this test builds went through the same defect, so every bridge draw of
> cycle 1 sampled the same all-zero QKS1 on both accelerators.
>
> | Formulation | Observed max relative diff | Observed diverged share |
> |---|---|---|
> | HPEPA3 | 1.688699E-14 | 0/2000 |
> | inpt | 6.210907E-15 | 0/1000 |
> | P33 | 6.769958E-14 | 0/2000 |
> | PSAN02n | 6.674763E-14 | 0/2000 |
> | HMX | 2.220092E-14 | 0/2000 |

---

<a id="throughput-re-measured-pre-release"></a>

## 2026-09-20 — from "## Throughput measurement" — the pre-Release "Re-measured" table

Moved because these figures are themselves superseded: they were measured through
`dotnet test`'s default `-c Debug`, which the same day's "## Host-thread throughput"
section found understates every CPU-executed figure in this node by roughly 2.75–3×
(the JIT leaves `PropStruct.Particle`/`PropStruct.Random` unoptimized). The `-c Release`
figures that replaced them live in "## Host-thread throughput" in `BOOT.md`.

> **Re-measured** (same machine, same method as "## Throughput measurement" above, after
> the fix):
>
> | Accelerator | Accepted particles/s | Attempts/s | Attempts/particle | Launches |
> |---|---|---|---|---|
> | CPU (`CPUAccelerator`, 16 threads) | 4,190 | ≈ 28,200 | 6.72 | 1 |
> | CUDA (`NVIDIA GeForce RTX 5070 Ti`) | 43,436 | ≈ 292,000 | 6.72 | 1 |
>
> Attempts/particle now matches the reference's 6.68 closely, and both accelerators
> finish cycle 1 in the single launch the design expects. CPU throughput rose ≈2.75×,
> CUDA ≈2.1× (not the ≈40× the attempts/particle ratio alone would suggest: launch 1's
> 25,600,000 wasted attempts were mostly overlapped/parallel work rather than 256×
> serial cost, and a fixed per-launch dispatch overhead now amortizes over far fewer
> attempts) — CUDA's advantage over the CPU accelerator narrows from ≈13.5× to ≈10.4×,
> since a smaller, well-parallelized run pays proportionally more launch/kernel-dispatch
> overhead the CPU accelerator does not carry the same way. Both figures superseded; the
> first measurement above is kept, not deleted (AGENTS.md §8).

---

<a id="throughput-second-look-implication"></a>

## 2026-09-20 — from "## Throughput measurement" — the withdrawn implication paragraph

Moved because it draws out the consequence of the "second look" ⚠'s "finding for
`Particle`" — which the "third look" ⚠ (still in `BOOT.md`) explicitly withdraws as
wrong, the defect having been in this node's own `Engine.WriteTotals`, not in
`Particle`. The ⚠ paragraphs that narrate the finding and its withdrawal are younger
than the fourteen days `AGENTS.md` §15 asks and stay in `BOOT.md`; this paragraph is
not itself a ⚠ and states an implication that no longer holds now the cause is known.

> **The throughput and math-probe-tolerance figures above are not renormalized by this
> finding**: the math-probe figure does not depend on how many attempts a cycle takes,
> and the CUDA/CPU *agreement* the tier table checks is unaffected by whether cycle 1
> takes 6.7 or 262.75 attempts per particle — both accelerators run the same (currently
> over-restarting) attempt logic identically, so their mutual agreement is still real.
> Only the **throughput figures' absolute size** is suspect: 1,522/20,617 particles/s
> measure a cycle 1 that is doing roughly 40× the attempt work the original's own cycle 1
> does, so once `Particle`'s defect above is fixed, both figures are expected to rise by
> roughly that factor (the fold/launch/kernel-dispatch overhead this node owns is
> unaffected either way — BOOT.md, "the dominant cost … is the attempt loop itself,"
> which remains true, just not at this size). They are kept, dated, rather than deleted,
> so whoever re-measures after the fix has the before-and-after (AGENTS.md §8): a claim
> is corrected in place, not erased.

---

<a id="throughput-initial-measurement"></a>

## 2026-09-20 — from "## Throughput measurement" — the initial, pre-fix table

Moved because it measures the same defective run as the tables above: cycle 1's
262.75 attempts/particle, later root-caused in "## Throughput measurement", "third
look" (still in `BOOT.md`) to `Engine.WriteTotals` never refreshing QKS1 after a
wholesale rewrite of the totals. Superseded first by the post-fix re-measurement (this
file, "the pre-Release 'Re-measured' table"), then by "## Host-thread throughput" in
`BOOT.md`.

> Measured once, before any kernel was tuned, on the reference machine (RTX 5070 Ti, 16
> logical CPUs), `tests/Execution.Tests/ThroughputMeasurementTests`: HPEPA3's cycle 0 run
> to completion, then its whole cycle 1 (100000 particles, `N` of the `.dat`) run as one
> `RunBatch` call with the default 2 GiB record budget (`AttemptsPerLaunch = 256`).
>
> | Accelerator | Accepted particles/s | Attempts/s | Attempts/particle | Launches |
> |---|---|---|---|---|
> | CPU (`CPUAccelerator`, 16 threads) | 1,522 | ≈ 399,900 | 262.75 | 2 |
> | CUDA (`NVIDIA GeForce RTX 5070 Ti`) | 20,617 | ≈ 5,417,600 | 262.75 | 2 |
>
> CUDA is ≈ 13.5× the CPU accelerator's throughput here; both need two launches for the
> whole cycle (BOOT.md, "A batch": ≈ 99% of particles finish in the first launch, a small
> tail is relaunched). No kernel has been tuned to reach either figure; both are the
> design above, run once.
