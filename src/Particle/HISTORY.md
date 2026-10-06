# HISTORY.md — Particle

Append-only store of what `BOOT.md` no longer needs to state as current truth
(`AGENTS.md`, §15). Newest entry first. Every entry names the date it was moved, the
`BOOT.md` section it came from, and carries the original text in full, unedited. The
start procedure (`AGENTS.md`, §10) does not read this file; it is reached only by
following a dated pointer left in `BOOT.md` at the place the text used to stand.

---

<a id="window-cycle1-measurement-2026-10-02"></a>

## 2026-10-02 — from "## Acceptance criteria" — the window-once equality over HPEPA3's cycle 1

Recorded here, not moved: the full apparatus and figures of the criterion "The same
equality holds over the first 10⁵ attempts of HPEPA3 in cycle 1", ticked in `BOOT.md`.

Measured 2026-10-02 on c14b93f (branch `claude/window-cycle1`), Debug build, CPU only
(`PROPSTRUCT_NO_CUDA=1`). A throwaway xunit class in `tests/Execution.Tests` (never
committed, deleted afterwards) took HPEPA3 to cycle 1 with
`ReferenceFormulationDriver.PrepareThroughCycle0` (`Independent` layout, seed 0,
`Binary64`), then ran the first 100000 cycle-1 attempts through `Attempt.Run` in
reference mode on the host thread: the six streams continued from
`StreamSeeds.ForParticle(Independent, 0, NextOrdinal)`, QKS1 refreshed
(`PocketHistogram.Normalize`) before an attempt whenever `LoopCompletions` had moved,
the record reset after an accepted attempt. After every attempt it took SHA-256 of
the trace row `SnapshotTests` hashes: outcome, the six streams' limbs, every integer
total, the particle's record. The run's own counts: 13599 accepted, 1250
`RestartedAfterLoop`, 85151 `RestartedInsideLoop`; 14849 QKS1 refreshes; over those
attempts `NFQ` rose by 203334 (every draw reads the window) and `IbridgeTotal` by
65171 over the accepted attempts alone (each has at least two bridges, line 727).

Scratch variant of `src/Particle/Attempt.cs`, reverted: the one line below, so that
the guard stays but never holds and the window is rebuilt at every bridge.

```diff
-            windowBuilt = true;
+            windowBuilt = false;
```

| Run | Code | Result |
|---|---|---|
| baseline 1, baseline 2 | unmodified | per-attempt hash files byte-identical (right control); chain digest 42C49DB3E855... |
| rebuild at every bridge | the diff above | byte-identical to the baseline, 100000 of 100000 attempts |
| red control | the diff above, plus before the guard `if (ibridgeLoc >= 2) { cycle.Qks1[(int)(setup.Ak3 * dr / setup.CellSize)] *= 2.0; }` | attempts 0 and 1 equal; first differing attempt 2 (zero-based), 99998 of 100000 differ afterwards (state carries on) |

Time: about 20 s per run of 100000 attempts, plus 3 s to reach cycle 1.

<a id="section-15-deviation-withdrawn-2026-09-26"></a>

## 2026-09-26 — from "## Constraints" — the withdrawn §15 deviation paragraph

Moved to make room, and withdrawn the same day: the §6 form correction of this document's `## Constraints` (the deviation the paragraph below names was already true, but was not written in the form `protocol_lint.py` reads) excludes `## Line map` and `## Defects of the original` from the AGENTS.md §15 line count, and the moves this HISTORY.md file records bring the rest of the document inside the 400-line leaf limit without this deviation. Kept here for provenance (AGENTS.md §8).

⚠ Declared deviation, §15: this document is over the 400-non-blank-line limit AGENTS.md
§15 sets for a leaf node. The two REAL*4-accumulation reconnaissance threads under
"Decisions where the port departs from a transcription" — the shadow-accumulator
worst-cell measurement and the 2026-09-21 end-to-end confirmation — are the current,
still-open investigation the next design session needs whole, not stale material with
a superseded reading; everything genuinely superseded (the per-`Attempt.Run`-call
figure, two small 2026-09-17 corrections) has already moved to `HISTORY.md`, each
leaving a dated pointer. Lifts when the REAL*4-accumulation question is closed (an
exclusion is written, or the per-cell/whole-population split above is either modelled
or ruled uninteresting) and its account can move to `HISTORY.md` in turn.

<a id="qks1-refresh-cadence-measurement-2026-09-25"></a>

## 2026-09-25 — from "## Attempt structure" — the QKS1 refresh-cadence measurement's full apparatus and figures

Moved to make room: `BOOT.md`'s current text states the verdict and the one deciding figure; this entry is the full apparatus, the live-variant check and the controls that produced it, kept for provenance and reproducibility. The throwaway env-var gate this measurement used edited `src/Execution/Qks1Refresher.cs`, a neighbour's own file, which AGENTS.md §3 reserves to that node and §11 says is changed only by escalation; no escalation was recorded at the time this measurement was made, and this entry is where that is now written down.

⚠ 2026-09-26, orchestrator: the measurement was audit item U-P2, assigned by the orchestrator at the tree root. The root is the level that owns both nodes (AGENTS.md §11), and that is where the neighbour's file was opened. The gate lived only in a throwaway worktree and was never merged: `src/Execution/Qks1Refresher.cs` carries no such gate, and its history holds no commit of it. What went unrecorded was that authorisation, not an unauthorised edit.

Measured 2026-09-25 (a throwaway env-var gate on `src/Execution/Qks1Refresher.cs`'s
`RefreshIfLoopsCompleted`, built, run and reverted in this session, `git diff --stat`
empty for `src/Execution` afterwards: set, it makes the method a no-op, leaving only
`Engine.Load`'s and `Engine.WriteTotals`'s own unconditional recomputes — i.e. QKS1
frozen once per cycle, after cycle 0, instead of refreshed after every completed
loop). Reference mode, `--precision binary64`, `Independent` layout, all five
reference formulations, eight seeds `k·2¹⁷`, `tests/Fixtures/run_port.py` against the
unmodified port, compared cell for cell by `tests/Harness`'s own `StatisticalCriterion.
TwoSampleBiasOfSets` (the seam root BOOT.md's batched-vs-reference "link 3" comparison,
"Statistical reference criterion", also uses). **Zero of 15,036 compared cells differ**
(HPEPA3 0/2091, inpt 0/2636, P33
0/1514, PSAN02n 0/2151, HMX 0/6644), largest `|t|` over any cell 1.08 (`PSAN02n`'s
`fqmkm2[75]`), on every quantity a bridge's window content can plausibly reach: `NFQ`,
`NFW`, `Dqmkm2`, `fqmkm2`, `fqkarm_cor`, `fmkarm_cor`, `Dkarm43_cor`, `Dagg43_cor`,
`Zkarm_cor`, `dokkarm43`, `Dkarm10_cor`.

**The variant was live**, checked directly (a second throwaway xunit class, run once
and not committed) by diffing each `base_seed{k}.m` against its own `frozen_seed{k}.m`
at the *same* seed — the same file pairs `TwoSampleBiasOfSets` pools across eight
seeds, read one pair at a time instead, no statistics, `ResultsMFile.Parse` and a plain
cell-by-cell comparison: **2,127 of 117,054 same-seed cells differ** (HPEPA3
291/16,644, inpt 565/20,978, P33 372/12,060, PSAN02n 308/17,041,
HMX 591/50,331), so the frozen arm is demonstrably a different computation at every
seed, not the same run twice. The shifts reach every quantity named above and the
generator diagnostics besides; worst same-seed relative shift per formulation, over
all eight seeds: HPEPA3 7.3 % (`fqmkm2[2]`, seed 8), inpt 98.2 % (`epsx(5)[0]`, seed 3),
P33 33.3 % (`fqmkm2[15]`, seed 7), PSAN02n 68.2 % (`epsx(6)[0]`, seed 1), HMX 100.3 %
(`fqmkm2[93]`, seed 4) — these are sparse tail cells (per-cell counts near the print
floor move by their own full magnitude on one differently-placed draw), not the
headline quantities: HMX seed 1's own `Zkarm` moves 0.58194 → 0.58105 (0.15 %) and
`Dkarm43_cor(2)` 284.96 → 285.05 (0.03 %), confirming a genuinely different bridge
sequence was drawn once the window stops moving, at an ordinary-noise magnitude for
the well-populated cells. The eight-seed statistical comparison above therefore reads as "the effect is
real per seed but is Monte Carlo noise at the population level", not as "the gate did
nothing": the comparison is shown able to see an effect of known size (the positive
control below), so a verdict of zero differing cells over eight seeds means the
per-seed shifts just measured do not exceed ordinary seed-to-seed spread, not that the
comparison was blind to a live variant.

Negative control: the same comparison over the
baseline set split in half (four seeds against the other four) finds 0 of 2069 cells
differing. Positive control: a synthetic +10 % shift of `NFQ`, `NFW`, `Dqmkm2` and
`fqkarm_cor` on the same parsed sets is flagged by the identical comparison (`NFQ`
1/1, `NFW` 1/1, `Dqmkm2` 1/1, `fqkarm_cor` 6/70), and root BOOT.md's own "link 3"
positive control (reference `Binary64` against reference `Original` on HPEPA3) already
proves the same comparison function red on a known, large effect.

This corroborates, with the confound removed, the standing 2026-09-23 "link 3"
measurement (root BOOT.md, "Statistical reference criterion": batched mode, which also
freezes QKS1 per launch, against reference mode: zero differing cells, the same five
formulations, eight seeds): that comparison could not by itself isolate the refresh
rule, since batched mode also differs from reference mode in schedule and stream
derivation; this one varies the refresh cadence alone, everything else held fixed, and
finds the same zero.

<a id="generated-real4-membership-decision-2026-09-24"></a>

## 2026-09-24 — from "## Accumulators / "Decisions where the port departs from a transcription"" — the generated-membership design decision's full implementation and its withdrawn intermediate readings

Moved to make room: `BOOT.md`'s current text states the decision, the figure that decided it, the artefact and test names, and the one outcome figure; this entry is the full four-bullet implementation, the scan's own findings, the `ScratchMovementProbe` measurement's full detail, the outcome-of-the-reconnaissance paragraph and the throughput benchmark figures, kept for provenance.

  **Design decision, 2026-09-24 (owner and orchestrator): the membership is generated
  too, not only the kind.** Until then the script generated each name's kind, but the
  list of names was typed by hand (`FIELD_FORTRAN_NAMES`). That list could not hold a
  scalar local, so six REAL*4 sums were missed: `VSMKM1`, `SVD1`, `Vdok_loc`,
  `Vmkm_loc`, `Vdok_loc2` and `Vmkm_loc2`, at lines 642–666. They stayed `double` under
  `Original` (fidelity audit of 2026-09-24, P-1). Implemented the same day:
  - the script scans this node's own `## Line map` ranges and nothing else. Every
    assignment of the shape `X = X ± …`, array element included, whose target is REAL*4
    by lines 4–64 and the implicit-typing rule, is a row of the generated file, with its
    Fortran lines (`classify-real4-accumulators.py` → `RealFourAccumulators.generated.txt`,
    now keyed by Fortran name, not by `AccumulatorLayout` field);
  - the only hand-written part is a separate data file, `RealFourWriteSites.txt`, mapping
    each Fortran name to its C# write site in this node, to "not ported", or to "not
    rounded" (ported, kept `double` by a recorded decision: `AUS`, `TU`, `VMKM`), each with
    a pointer to the paragraph that says why;
  - `tests/Particle.Tests/RealFourAccumulatorMappingTests.cs` fails when a generated
    REAL*4 row has no entry, when an entry names no generated row, and when a named
    write site is a plain `+=` rather than `AddReal4`; each proven red on a synthetic
    fixture (a removed entry, a bogus entry, a reverted `AddReal4`) and right on a
    matching one, plus once, by hand, on the real files (`vsmkm1`'s own site reverted to
    `+=` and restored: the real-file test named it, `git diff --stat` empty for
    `src/Particle` afterwards);
  - the six sums above round per addition under `Original`, like the other fifteen
    (`Attempt.cs`, lines 642, 643, 648, 649, 665, 666).

  The scan, run for real, also found three self-referencing REAL*4 names beyond the
  fifteen record fields and the six sums above, none of them an accumulator: `ALLVDOK_FR`
  (already declared, "Accumulators that nothing printed..." below), and `AUS` and `TU`
  (already declared REAL*4 decision variables, ⚠ 2026-09-17 below) — confirming the scan
  finds what the old hand list was blind to in both directions, not only the six sums.
  One further name is new: `VMKM` inside subroutine `VM` (line 1627, the self-referencing
  `VMKM=VMKM-V1-V2`) is REAL*4 by implicit typing but is a per-call bridge-volume value
  recomputed fresh on every draw of the bridge-redraw loop, never accumulated across
  attempts — attempt-plane like `Dr`/`Db`, not an accumulator (root BOOT.md, "Precision
  kind": the attempt plane stays `double` under either kind) — declared, not reproduced,
  in `BridgeGeometry.Volume`.

  Measured the same day (`ScratchMovementProbe`, a one-off scratch xunit class run once
  and not committed, reference mode, seed 0, `Original` layout, the five reference
  formulations, before this fix and after, `results.m` parsed back by
  `tests/Harness`'s own parser): `Binary64` output is bit-identical on every one of the
  five, byte for byte. `Original` output moves on three of the five, and only in
  `Zkarm_cor`, by 1.3e-7 to 8.6e-7 relative (`inpt[1]` 0.5159954 → 0.5159955;
  `P33[0]` 0.045379629 → 0.045379590; `HMX[1]` 0.7652749 → 0.7652748); HPEPA3 and
  PSAN02n print no difference in any quantity at seed 0. `tests/Simulation.Tests
  --filter FullyQualifiedName~StatisticalCriterionTests` (`-c Release`) stays green
  afterwards, its three ratchet cells (HMX `fqdokkarm(31,:)[7]`, HPEPA3
  `fqkarm_cor[60]`, P33 `fqkarm_cor[15]`) unchanged.

  This is the outcome of the reconnaissance this document carried through
  2026-09-20/21: a per-`Attempt.Run`-call rounding granularity that read as a lower
  bound, a corrected per-drawn-particle granularity that reopened the REAL*4
  summation candidate (worst-cell error 46-65% on `Allvdok`/`Vdokstr`, HPEPA3/HMX), an
  end-to-end throwaway build that flipped 91 of the root criterion's 92 failing
  `Original`-layout cells to passing but also broke 39 new ones by over-rounding
  `real*8`-declared scalars, and the declared-kind classification (replacing an
  array-vs-scalar proxy and a wrong x87-residency explanation for the 39 new
  failures) that fixed the 39 without moving any of the 92. The full narrative,
  every dated figure and every withdrawn explanation are kept whole, superseded, in
  `HISTORY.md#real4-accumulation-reconnaissance-2026-09-20-21`: this document now
  states the classification as implemented code, not as an open question.

  Measured 2026-09-21, `tests/Benchmarks`' own route (Release, HPEPA3, host1/host16,
  before this change and after, same command): host1 (n=8, 40 000 particles)
  30066±5142 before vs 28751±6072 after; host16 (n=3/n=5, 20 000 particles)
  46825±4291 before vs 49419±2279 after (accepted particles/s). Both differences sit
  inside the other run's own spread: `AccumulationKind.Double` (renamed `PrecisionKind.Double` on 2026-09-23, now `PrecisionKind.Binary64`), the default and every
  path but `AddReal4`'s own `Original` branch, shows no throughput regression at this
  sample size. A tighter figure needs more repeats on a quieter machine — that is
  `tests/Benchmarks`' own concern, not this reconnaissance's.

<a id="x1-equals-one-reachability-corrected"></a>

## 2026-09-24 — from "## Accumulators / "Decisions where the port departs from a transcription"" — the X1==1 reachability correction, full text

Moved to make room: `BOOT.md`'s current text carries the pointer and keeps the X3/`IndexOutOfRange` consequence as current truth in the bullet above; this entry is the full correction, kept for provenance (AGENTS.md §8).

  ⚠ 2026-09-24: was "`Mcg128.Next` is guaranteed to return a value in `[0, 1)` … so the
  redraw condition is unreachable here". The guarantee was false (`Random`'s correction
  of the same date). The conclusion, no budget for this redraw, stands. The same 1.0
  drawn as X3 is not redrawn and ends the run with `IndexOutOfRange` in
  `BridgeWindow.SamplePocket`, about 10⁻¹⁰ per run by the fidelity audit's estimate and
  not measured here.

<a id="dmin-boundary-probe-2026-09-23"></a>

## 2026-09-23 — from "## Accumulators / "Decisions where the port departs from a transcription"" — the Dmin boundary probe's full method, controls and red proofs

Moved to make room: `BOOT.md`'s current text states the decision (declared, not reproduced), the deciding figures and the two test file names; this entry is the full bisection method, the reference-mode replay, the root's Poisson-interval clarification, the remaining-factor hypothesis and the positive/negative/red-proof controls, kept for provenance and reproducibility.

  Measured 2026-09-23 (`tests/Particle.Tests/DminBoundaryProbe.cs`,
  `DminBoundaryProbeTests.cs`). The exact `x1` threshold below which a draw already
  selecting the fraction whose lower bound equals `Dmin` rounds down, found by
  bisecting the published `SizeLaw.Sample` itself (`D(x1)` is monotone for both size
  laws: linear for `sizeLaw == 2`; for the reciprocal-square law
  `dD/dx1 = (Δ/2)·D³ > 0` since `Δ = 1/lower² - 1/upper² > 0`), is nonzero only for
  the three reference formulations whose lowest fraction bound equals the default
  `Dmin` (HMX, HPEPA3, P33 — `PSAN02n`/`inpt`'s own lowest bound, 160/257 μm, is
  nowhere near it, and the search reports a structurally degenerate threshold of 0
  for them). `sizeLaw == 1` (HMX/HPEPA3) carries a threshold about eight times
  `sizeLaw == 2`'s (P33) at the same nominal coincidence
  (`dD/dx1|_{x1=0} = (lower/2)(1-(lower/upper)²)` for the reciprocal-square law
  against the linear law's constant `upper - lower`) — the quantitative reason
  P33's own rate is far smaller than HMX's/HPEPA3's, not merely "also zero because
  `JZ` differs".

  A real reference-mode replay through the actual streams and `Attempt.Run` (the
  same technique `AccumulatorSweep` uses for `Allvdok`/`Vdokstr`, on a copy of the
  streams taken before the real call) observes the mechanism directly, at seed 0:
  3 hits of 2,000,000 attempts on HMX (rate 3.72e-4) and 4 of 1,200,000 on HPEPA3
  (2.21e-5), against 0 of 500,000 on P33; the port's own `double` comparison never
  fires in any of the three. Against the original's own sixteen (HMX) / thirty-two
  (HPEPA3) lagged replicas (`tests/Fixtures/replicas-lagged`), whose mean printed
  rate is 1.19e-3 for HMX and 5.47e-5 for HPEPA3, this mechanism alone reaches the
  right order of magnitude — 3.2x and 2.5x below the mean respectively — without
  closing it. The remaining factor is not established: the original's own `SIZE`
  subroutine also rounds its own intermediate reciprocal-square terms (`1/DOK²`,
  computed at REAL*4 precision before `X1`'s multiplication ever promotes anything)
  for `sizeLaw != 2`, which this measurement does not model (it rounds only the
  drawn size's own final store, this task's own specified method) and which could
  plausibly supply the rest, given the derivative argument above already explains
  P33's near-certain absence through the final-store mechanism alone, at this
  formulation's own scale.

  Clarified by the root the same night, and read with this correction: the "3.2x / 2.5x
  below" above are ratios of counts of 3 and 4 events, and they carry exact Poisson 95 %
  intervals. For HMX the rate interval is 7.7e-5 to 1.09e-3, which falls just short of the
  original's 1.19e-3; its replicas' standard error is about 6e-5. For HPEPA3 it is 6.0e-6
  to 5.7e-5, which **contains** the original's 5.47e-5. So on HPEPA3 this mechanism may
  account for the whole count. On HMX a shortfall is suggested, not established. The
  "remaining factor" and the intermediate-term candidate above are therefore a hypothesis
  about HMX alone. Settling it needs a replay long enough for tens of events, not a
  different model.

  Positive control: HMX and HPEPA3 both show a nonzero, order-of-magnitude-correct
  rate at this sample size. Negative control: P33 (0 of 500,000, against a
  projected rate under one expected event over its own printed `Nbase`) and
  `PSAN02n`/`inpt` (structurally degenerate threshold, no coincidence to probe at
  all). Red proof, both directions tried: dropping the binary32 cast from `Dmin`'s
  own side alone changes nothing (its double value already sits in the same
  float32 rounding bucket as its own store, so the two are numerically
  indistinguishable here); dropping it from the *drawn size*'s side instead — the
  actual mechanism, `Dmin`'s own REAL*4 storage already being reproduced elsewhere
  in the tree — collapses `monotonic` to false and the threshold to 0 on all three
  coincident formulations (HMX, HPEPA3, P33 alike), confirming the drawn size's
  own store, not `Dmin`'s, is what this measurement depends on; applied and
  reverted in this session, not committed (`git diff --stat` empty for
  `tests/Particle.Tests` afterwards).

<a id="decision-variable-gate-bias-refuted-2026-09-21"></a>

## 2026-09-21 — from "## Accumulators / "Decisions where the port departs from a transcription"" — the decision-variable gate-bias refutation's full method and pre-registration

Moved to make room: `BOOT.md`'s current text states the decision (this mechanism does not produce the original's drift) and the two figures that decided it; this entry is the full escalated method, the pre-registered thresholds and the reproducibility note, kept for provenance.

  Measured causally 2026-09-21 (task from `tests/Harness/BOOT.md`'s own "aggregate
  decision-gate-bias" candidate, `tests/Harness/HISTORY.md`'s "reachability of the
  taboo's REAL*4-accumulation exclusion evidence..." entry, escalated per AGENTS.md §11
  since it needed this node's own accumulator internals). Method: outside the tree, a
  throwaway copy of this node rounded exactly
  these four variables to `float` at exactly these points — `BridgeWindow.Build`'s
  `AUS` sum (589), its `FQKS` build and truncation test (597, 601), `Attempt.Run`'s `TU`
  update and exit test (715–716), `QKS1` read as `float` only where those formulas use
  it — every other quantity left `double`; built and run through the unmodified
  `tests/Fixtures/run_port.py` (`--propstruct` pointing at that build), HPEPA3,
  `Original` layout, seed 0, the 27-point coarse `N` grid `tests/Harness/HISTORY.md`'s
  own "smooth-vs-step check" used, read back with `tests/Harness`'s public
  `ResultsMFile.Parse` through a second throwaway tool (never a second parser).
  Pre-registered before reading a value: reproducing the original's own drop over this
  range (`pdoksmall[2]` 0.100, `fmdok[1]` 0.0056) meant at least half of it, growing
  with `N`; refuting it meant at most three times the port's own already-established
  flatness (`pdoksmall[2]` ≤ 0.003, `fmdok[1]` ≤ 0.0003) with no monotonic trend; a
  result between the two would be reported as its own fraction, rounded toward neither.
  Result: the emulating build drops by 0.003 (`pdoksmall[2]`, 3% of the original's own
  drop) and 0.0002 (`fmdok[1]`, 3.6%), every bit of it below `N = 10000`; from
  `N = 14500` to the shipped `N = 100000` — 85% of the range, where the original's own
  decline is largest — the emulating build is exactly as flat as the unmodified port
  (`tests/Harness/HISTORY.md`'s own recorded curve, reused rather than re-run). Below
  the refuted threshold on both cells: this specific mechanism does not produce the
  original's drift. The variant is discarded, never committed (root BOOT.md's "no
  second implementation" taboo); the measurement is reproducible from this paragraph
  and the existing `run_port.py --propstruct <path>` apparatus alone, no new tooling.

<a id="real4-decision-variables-added-to-the-exclusion-list"></a>

## 2026-09-17 — from "## Accumulators / "Decisions where the port departs from a transcription"" — the withdrawn correction adding QKS1/FQKS/AUS/TU to the exclusion list

Moved to make room: `BOOT.md`'s current text restates the four REAL*4 decision variables as plain current truth; this entry is the original correction that added them to the exclusion list, kept for provenance (AGENTS.md §8).

  ⚠ 2026-09-17: this exclusion list covered only the summed accumulators. `QKS1`,
  `FQKS`, `AUS` and `TU` are REAL*4 **decision** variables of the original (they gate a
  branch, they are not printed): `QKS1` and `FQKS` feed the `1.0 − FQKS(iks) < 1E-5`
  truncation of line 601 (`BridgeWindow.Build`'s `maxdk` shrink), `AUS` is their
  reciprocal sum (line 589's empty-window test), and `TU` gates the neighbour-loop exit
  of line 716. Under the original's REAL*4 rounding these four can cross their own
  threshold one draw earlier or later than the port's `double` arithmetic does, choosing
  a different `maxdk`/`NNN1` or a different number of neighbour-loop passes for the same
  input. This does not change which formula runs (`Particle` has no REAL*4 to depart
  from here: `QKS1`/`Pdoksmall` arrive as `double` views, `CycleInputs`), only, on the
  rare draw sitting between the REAL*4 and `double` roundings of the same threshold,
  which branch a given attempt takes — recorded here so `Statistics`' fidelity audit (or
  whoever derives an exclusion for a printed quantity fed by this branch) does not
  mistake the branch's own sensitivity for a bug and does not need to re-discover it by
  reading `Attempt.cs`.

<a id="field-layout-moved-to-api-md"></a>

## 2026-09-17 — from "## Accumulators" — the field-layout-to-API.md move, full paragraph

Moved to make room: `BOOT.md`'s current text keeps the sentence that the layout is `API.md`'s own contract; this entry is the full paragraph, including the 2026-09-17 provenance of that move, kept for provenance (AGENTS.md §8).

The field layout of both buffers — name, length, stride and cell meaning — is
`API.md`'s contract ("Accumulator layout"), not repeated here (2026-09-17, moved: a
neighbour used to have to read this document to learn the `Ndok` stride of `Qdoks` or
that `Conditions + k`/`Xss + k` are indexable, which is exactly the kind of "contract a
neighbour needs without reading BOOT" `API.md` exists for). What follows is which
Fortran line writes each field and why it sits in the buffer it does — the attempt's
own reasoning, needed to implement `Attempt.Run`, not to call it.

<a id="qks1-refresh-defect-withdrawn"></a>

## 2026-09-25 — from "## Attempt structure" — the withdrawn "defect" reading and its evidence

Moved to make room: `BOOT.md`'s current text at this place states the corrected
reading (the refresh is a model feature, not a defect) and the 2026-09-25 measurement
that supports it. This entry is the text that stood there before, kept for provenance
(AGENTS.md §8: a wrong claim's history is part of the context).

This refresh rule is itself a defect of the original the port reproduces, declared for
`## Defects of the original` (2026-09-20): the normalized pocket histogram a bridge
draws its window from moves during the run, refreshed after every completed neighbour
loop rather than held fixed for a cycle or a run. Measured 2026-09-20
(`tests/Simulation.Tests/BatchedExcessSweepScratch`, a one-off sweep run once and not
committed, the pattern `src/Simulation/BOOT.md`'s own "## Budget measurement" scratch
used): P33 (N = 20 000) and HMX (N = 10 000), full batched runs (`Original` layout,
seed 0) at batch sizes `{N, N/10, N/100, 1000}` crossed with `AttemptsPerLaunch`
`{32, 4, 1}` — 12 configurations of P33, 9 of HMX after de-duplicating `N/10 = 1000`
for HMX — give bit-identical `TotalAttempts`, `Nfx` and `IbridgeTotal` within each
formulation across every configuration, and none failed. Refreshing QKS1 more or less
often, at these batch and launch sizes, changes nothing either total exposes.

**Why this was withdrawn** (fidelity audit 2026-09-24, U-P2): `TotalAttempts`, `Nfx`
and `IbridgeTotal` are decided entirely by streams 1–5 (the base/neighbour draws, the
gap coefficient `AA`, the bridge/pocket branch split) before any bridge window is ever
touched — the branch that reaches the window (`ibridge_loc++`, line 580) is already
taken by the time `BridgeWindow.Build` reads QKS1, and whether that branch is taken at
all does not depend on the window either. So these three totals are independent of
QKS1's content by construction, and bit-identical values across refresh cadences prove
nothing about whether the refresh cadence changes anything: the evidence could not
have come out any other way, whatever QKS1 held. Reproduced 2026-09-25 with a properly
targeted measurement (the quantities a bridge's window content can actually reach —
`NFQ`, `NFW`, `Dqmkm2`, `fqmkm2`, `fqkarm_cor`, `fmkarm_cor`, `Dkarm43_cor`,
`Dagg43_cor`, `Zkarm_cor`, `dokkarm43`, `Dkarm10_cor`), current text.

<a id="bridge-window-origin-audit-2026-09-24"></a>

## 2026-09-24 — from "## Defects of the original" / "Bridge window once per attempt" — the U-P1 measurement's full run table

Moved to make room: `BOOT.md`'s current defect-table row and prose paragraph state the
verdict and the headline figures; this entry is the full sweep that produced them, kept
for provenance and to let the measurement be reproduced or extended without re-deriving
the apparatus.

**Question** (fidelity audit 2026-09-24, U-P1): Fortran line 595, `DPOC(mindk)=Di*mindk`,
anchors the bridge-window CDF's diameter axis at the *upper* edge of QKS1's cell
`mindk` instead of its lower edge, so every bridge pocket diameter (`DKARM`) is drawn
one histogram cell (`Di`) above the cell whose probability mass produced it. Does this
change the model's answer, and by how much?

**Reading of the lines** (580-610, declared this node's own specification,
`## Line map`). `mindk = int(RR1/Di)+1` (line 584) is built with the identical
one-based cell-index convention as every other pocket-size cell index in the source,
confirmed at line 675, `iks = int(RK/Di)+1; QKS(iks)=QKS(iks)+1`: cell `iks` covers
`[(iks-1)*Di, iks*Di)`. `DPOC` is then built as a CDF/diameter breakpoint pair with
`FQKS` (lines 592-599): `FQKS(mindk)=0`, `DPOC(mindk)=Di*mindk` (line 595), and the
loop `FQKS(iks+1)=AUS*QKS1(iks)+FQKS(iks)`, `DPOC(iks+1)=Di+DPOC(iks)` (lines 596-598)
steps both arrays forward one cell at a time, in lockstep, starting from that anchor.
`DM` (lines 1573-1586) linearly interpolates diameter against this CDF: the interval
`[FQKS(mindk), FQKS(mindk+1))`, which by construction holds exactly cell `mindk`'s own
probability mass (`AUS*QKS1(mindk)`), is paired with the diameter interval
`[DPOC(mindk), DPOC(mindk+1)) = [Di*mindk, Di*(mindk+1))` — the interval belonging to
cell `mindk+1` under the convention line 675 establishes, one full cell above cell
`mindk`'s own `[Di*(mindk-1), Di*mindk)`. Corroborating evidence against an
intentional upper-bound or centre reading: the source's own downstream consumer of
this exact pocket-size cell convention, `Dkarm43_cor`/`Dqkarm_cor` (lines 1085-1091,
computed from `fmkarm_cor`/`fqkarm_cor`, the pocket-size histogram indexed the same
way as `QKS`), represents cell `kilo` by its *centre*, `Di*(kilo-0.5)` — never by its
upper edge — and no comment or alternate use of `DPOC`/`mindk` anywhere in lines
580-667 suggests the upper edge was intended. The lines admit no reading under which
`Di*mindk` is correct; it is a plain off-by-one, and the fix reads `Di*(mindk-1)`.

**Method.** Two throwaway variants of `BridgeWindow.Build`'s one line (line 56 of
`src/Particle/BridgeWindow.cs`), each built, measured and reverted in this session,
`git diff --stat` empty for `src/Particle` afterwards (root BOOT.md taboo, "no second
implementation"):

- variant A, `dpoc[mindk - 1] = cellSize * (mindk - 1);` — the origin at the cell's own
  lower edge, the reading above;
- variant B, `dpoc[mindk - 1] = cellSize * (mindk - 0.5);` — the cell centre, tested
  because the source's own `Dkarm43_cor`/`Dqkarm_cor` convention uses a centre
  elsewhere, even though nothing in lines 580-667 suggests `DPOC` itself should.

Apparatus: `dotnet build PropStruct.sln -c Release` per variant (this worktree,
`claude/bridge-window-origin`, based on `claude/wave7` at `ba72e05`), each build's
`src/Cli/bin/Release/net10.0` copied aside before the source was reverted;
`tests/Fixtures/run_port.py`'s own `run()` function (reused unchanged, no second
invocation path) driven from a throwaway loop script, `--propstruct` pointing at each
build in turn; reference mode, `--layout independent`, `--precision double` (the
script's default), all five reference formulations, eight seeds `k*2^17` for
`k = 0..7`; 120 runs total (baseline + two variants x 5 formulations x 8 seeds), every
run exit code 0. Comparison: `tests/Harness`'s public `StatisticalCriterion.
TwoSampleBiasOfSets(baselineSet, variantSet)`, one throwaway console tool referencing
`PropStruct.Tests.Harness` (never committed), Welch per cell at the tree's own family-
wise `alpha = 1e-3` (`StatisticalCriterion.Alpha`), baseline's own eight runs against
each variant's eight runs.

**Positive/negative control** (root taboo, "every check proven twice"). Negative:
baseline's own even-`k` seeds against its own odd-`k` seeds, same apparatus, same five
formulations — 0 to 24 of 1504-6155 cells flagged, every one `t = +-infinity` (zero
within-group variance, a print-resolution artefact of the same sparse/tail-cell
miscalibration this tree's own gate-1 criterion already documents, e.g. `pdoksmall`'s
high, rarely-touched indices on P33), none in the families the variants move. Positive:
variant A's own effect, large and dose-proportional (below), serves — no separate
deliberately-larger shift was needed.

**Result, variant A vs baseline, 8 seeds each (mean +- sample sd over the 8 seeds):**

| Formulation | `Dqmkm2` baseline | `Dqmkm2` variant A | shift | `Zkarm` baseline | `Zkarm` variant A | shift | cells compared | cells differing |
|---|---|---|---|---|---|---|---|---|
| HPEPA3 | 0.6368 +- 0.0001 | 0.5925 +- 0.0001 | -6.95% | 0.5867 +- 0.0067 | 0.6561 +- 0.0061 | +11.82% | 2090 | 93 |
| inpt | 0.5201 +- 0.0002 | 0.5054 +- 0.0001 | -2.81% | 0.4998 +- 0.0008 | 0.5352 +- 0.0003 | +7.08% | 2635 | 93 |
| P33 | 0.6489 +- 0.0003 | 0.6204 +- 0.0004 | -4.39% | 0.5123 +- 0.0193 | 0.5703 +- 0.0164 | +11.31% | 1513 | 75 |
| PSAN02n | 0.5053 +- 0.0001 | 0.4869 +- 0.0001 | -3.64% | 0.4064 +- 0.0006 | 0.4614 +- 0.0006 | +13.51% | 2151 | 85 |
| HMX | 0.5770 +- 0.0003 | 0.5492 +- 0.0002 | -4.82% | 0.5829 +- 0.0024 | 0.6115 +- 0.0027 | +4.91% | 6643 | 95 |

`Dqmkm1`/`qmkm1` (fed by the gap coefficient `AA`, line 628, never by `DKARM`) and the
bridge count `MediumNumberOfBridges` (`ibridge_loc`, incremented at line 580 before the
window is even built) are bit-identical between baseline and variant A on every
formulation (`t = 0.00` on every row), confirming the shift is confined to the
diameter-derived family: `Dqmkm2`/`qmkm2` (fed directly, `BB` in `VM`'s geometry
depends on `RK = DKARM/2`), its own histogram `fqmkm2` (39-91 of the differing cells
per formulation), and `Zkarm`/`Zkarm_cor` (fed indirectly, through the run's shared,
never-reset `Qks`/`QKS1` accumulators: a changed `DKARM` can flip `VM`'s own feasibility
flag `JJ` at label 451 or a downstream acceptance test at lines 723-738, changing which
particles this run's own histogram accumulates, which then feeds every later bridge's
own window — a run-wide feedback the two direct, mechanically-obvious cells above do
not need, and which this measurement observes but does not fully trace).

**Result, variant B (cell centre) vs baseline, same seeds, same quantities:**
`Dqmkm2` shift -3.13%/-1.40%/-2.05%/-1.80%/-2.25% (HPEPA3/inpt/P33/PSAN02n/HMX);
`Zkarm` shift +5.91%/+3.59%/+6.01%/+6.87%/+2.58%. Every ratio variant-B-shift /
variant-A-shift falls in 0.45-0.53 across both quantities and all five formulations —
consistent with a response linear in the origin shift (variant B moves the origin by
half of variant A's `Di`), which is what a pure coordinate translation of the lookup
table predicts and corroborates the mechanism read from the lines, not only the
verdict.

`Dkarm43_cor`/`Dagg43_cor`/`Dkarm43sd_cor` (the headline mass-mean pocket size the root
BOOT.md's own precision-kind discussion names) could not be read through `ResultsMFile.
Parse` in this measurement — a pre-existing limitation of that neighbour's parser this
node does not read the source of (AGENTS.md §3) and is not this task's to diagnose.
Direct inspection of the raw printed text for `Dkarm43_cor(2)`/`Dagg43_cor(2)` (the
Var#2 index, HMX, all eight seeds) found no shift beyond the seeds' own spread
(baseline 286.81 +- 0.95, variant A 286.49 +- 0.97, shift -0.11%): consistent with these
two cells being fed by the *pocket* branch's own `RK = AA` (line 673), the same
gap-coefficient path `Dqmkm1` is, not by `DKARM`.

<a id="real4-accumulation-reconnaissance-2026-09-20-21"></a>

## 2026-09-21 — from "## Accumulators", "Decisions where the port departs from a transcription" — the REAL*4 accumulation reconnaissance, superseded by its own implementation

Moved to make room for `AccumulationKind` (root BOOT.md, "Accumulation kind is an
option of every run"): `BOOT.md`'s current paragraph states the classification as
implemented code (`Attempt.AddReal4`, `RealFourAccumulators.generated.txt`) and the
measured `Double`-mode benchmark comparison; everything below is the two-day
reconnaissance that got there — every dated figure, every withdrawn explanation
(the per-`Attempt.Run`-call granularity later read as a lower bound, the
array-vs-scalar proxy, the wrong x87-register-residency story) — kept whole for
provenance, not needed to use or extend the feature today.

> All sums are `double`; the original's REAL*4 sums (`ALLVDOK`, `vdokstr`,
> `DOKP41`, `fmkarm*`, `VSMKM`…) saturate on long runs, which the port does not
> reproduce (root `## Constraints`, exclusion list).
>
> ⚠ 2026-09-20: was measured at one rounding per `Attempt.Run` call, now known to have
> been a lower bound wearing the clothes of a measurement for five of the eight fields
> → HISTORY.md#accumulator-error-attempt-granularity-superseded.
>
> Re-measured 2026-09-21 at the original's own granularity — one rounding per drawn
> particle, verified bit for bit against a persistent shadow accumulator kept in
> lockstep with `record`'s own resets (0 mismatches over 500 000 attempts, proven
> non-degenerate by a deliberate stream-swap mutation; `tests/Particle.Tests/
> AccumulatorSweep.cs`, `RealFourAccumulationErrorTests.cs`) — after an owner-requested
> audit reported the prior (per-`Attempt.Run`-call) figures untrustworthy. Worst-cell
> relative error reached 46%/65% for `Allvdok`/`Vdokstr` on HPEPA3/HMX, and
> `Dokp41`/`Dokp31` (feeding `dokkarm43`, lumped where an attempt touches a cell twice
> or more, 58-98% of true terms by formulation) exceeded the whole replica band on
> HPEPA3/HMX (ratio 1.1-2.8): the REAL*4 summation candidate was reopened, decisively
> for those two formulations' `dokkarm43` cells (full worst-cell table, the
> `Fmkarm2`/`FmkarmCor` reading and the bootstrap cross-check →
> HISTORY.md#accumulator-error-per-drawn-particle-worst-cell-table).
>
> Confirmed end-to-end 2026-09-21, escalated from the root's own reconnaissance ("does
> rounding every accumulator addition to binary32 reproduce the original's printed
> output?"). Method: a throwaway copy of the whole solution outside the tree
> (discarded, never committed), two changes only — every `## Accumulators` real-valued
> write site in `Attempt.cs` rounded `(double)(float)` after each addition, and
> `Execution`'s reference-mode driver (touched only in the copy) made to write straight
> into the run-level totals instead of a per-particle record folded in afterwards,
> since the original's REAL*4 storage is one continuous total for the whole run, never
> reset per particle. Decisions, sizes and distances stayed double; a bit-identical
> integer-total diff against the unmodified port on all five formulations confirms
> nothing else moved. Reference mode, `Original` layout, seed 0 ("as shipped"), all
> five formulations, compared cell by cell against the archived reference and, via the
> unmodified `StatisticalCriterion.Compare`, against the 92 Original-layout cells root
> BOOT.md already carries as failing.
>
> Result: **91 of 92 now pass** — all of HPEPA3 (39), HMX (46) and PSAN02n (6), several
> to the exact printed digit; the one holdout, inpt's `epsdokfr[0]`, is the
> already-declared REAL*4/REAL*8-mixing defect, unaffected as expected. But the same
> rounding, applied to the accumulators that sum the *whole population* in one scalar
> rather than per size-cell (`Xss`, `DokBase41/31`, `DokSur41/31`, `Sd4/Sd3`, `D41/D31`;
> Fortran 772–784/810/814–818, `XSRk = XSSk/count` computed once at run's end over
> hundreds of millions of terms), produces ~90% relative error the reference does not
> show (`epsx(3)` measured 5e-5 there against 0.88 here on HMX): **39 new failures**
> across four formulations (`Dok43all[0]`, `Dok43(2)`, `epsalldok`, `epsdok(2)`, the
> `epsx` family, `epsdok43_n`, `Dagg43`, `Dkarm43`). Print-resolution exact-match counts
> (not committed anywhere, read off the throwaway run) moved only marginally and
> inconsistently across all five formulations: dominated by decisions/sizes staying
> double against the original's own REAL*4 computation of them (the
> `QKS1`/`FQKS`/`AUS`/`TU` entry, still current in `BOOT.md`), not informative here on
> its own.
>
> **So**: REAL*4 accumulation, at per-addition granularity, is confirmed as the
> (near-total) explanation for the currently-unexplained statistical-criterion
> failures, but a uniform, naive rounding is not the right carrier of a fix — it must
> distinguish the accumulators the original actually declares REAL*4 from the ones it
> declares `real*8` (below), which the 39 new failures already point at directly: every
> one of their feeding scalars (`Xss0`-`Xss6`, `DOK_base41/31`, `DOK_sur41/31`,
> `sd3/sd4`, `d31/d41`, `dp31/dp41`) is `real*8` at Fortran lines 44, 42 and 45, so
> rounding them was never a reproduction of the original's arithmetic — no x87 register
> story is needed, and the wrong one that stood here is withdrawn (⚠ below).
> Reproducible from this paragraph and the unmodified
> `run_port.py`/`ResultsMFile.Parse`/`StatisticalCriterion.Compare` apparatus plus the
> two changes named above (`RoundReal4` wrapping every `record[...] +=`/
> `fmkarm2Loc[...] +=` site of `Attempt.cs`; `Engine.RunReferenceParticle` writing
> `_realTotals` directly); discarded, never committed (root's "no float", "no second
> implementation" taboos).
>
> ⚠ 2026-09-21: this paragraph and the one above it first read the 39-new-failures
> result through an x87-register-residency story ("80-bit registers most likely keep
> these long-lived scalar accumulators resident..."), and the write-site list above was
> first claimed "all confirmed REAL*4 by the Fortran's own implicit typing". Both wrong:
> verified against `tests/Fixtures/Legacy/PropStructv3.for.txt` (this node's declared
> specification) that no `IMPLICIT` statement occurs anywhere in the source and that the
> 39-failure fields' own scalars are declared `real*8` explicitly, not implicitly typed
> at all. Raised by an owner-requested audit in another session, verified here before
> adoption. Full old wording → HISTORY.md#accumulator-error-array-vs-scalar-classification-rationale-superseded.
>
> **Classification rule, corrected 2026-09-21** (replaces the array-vs-scalar proxy
> tested the same day, ⚠ above; old wording and why it was wrong:
> HISTORY.md#accumulator-error-array-vs-scalar-classification-rationale-superseded). A
> field is rounded after every addition exactly when the Fortran name(s) it accumulates
> are REAL*4 **by declared kind**: `real*8` (explicit) excludes; a bare `real`
> (explicit) or no declaration at all (implicit — the name is outside the `I`-`N`
> integer range, and no `IMPLICIT` statement narrows the default anywhere in the
> source) rounds. **The classification is derived from the Fortran declaration block
> (`tests/Fixtures/Legacy/PropStructv3.for.txt` lines 4-64, this node's declared
> specification) plus this one implicit-typing rule, read by a script, not typed by
> hand** (AGENTS.md §6, a criterion quantified by "all" against a machine-generated
> list): the script parses every `real`/`real*8`/`integer`/`integer*8`/`character`
> declaration statement (with its `,allocatable`/`::`/initializer forms) into a
> name→kind table, falls back to the implicit rule for a name the table does not carry,
> and classifies each of the fifteen record fields by its own Fortran name(s) (the
> fields already named per Fortran line in the "Real-valued record" table above). It
> would fail — report a different set — if a name's own declaration changed kind, if a
> name lost or gained a `real*8` marker, or if a name presently undeclared (implicit)
> gained an explicit declaration of either kind; proven non-degenerate by mutating the
> `real*8,allocatable :: VKS(:)` declaration to `real` in a scratch copy of the source
> and observing `Vks` flip from excluded to rounded. Result, against the array-vs-scalar
> proxy: **`Vks` moves from rounded to excluded** (an array, but declared `real*8` at
> line 13); **`JammedTotal`, `NnTotal`, `VmkmTotal2`, `VdokTotal2` move from excluded to
> rounded** (`JammedTotal`/`NnTotal` declared `real` at lines 33/32, explicit;
> `VmkmTotal2`/`VdokTotal2` undeclared, implicit `real`). Every other field's
> classification is unchanged from the proxy. Script and mutation test: this session's
> own scratch directory, not committed at the time — committed the next session as
> `classify-real4-accumulators.py`/`RealFourAccumulators.generated.txt`, reproducing
> this same fifteen/twelve split (`RealFourClassificationGeneratorTests.cs`).
>
> **Re-run 2026-09-21** with the corrected classification (same throwaway apparatus,
> `Vks` no longer rounded, the other four fields now rounded): of the same 92
> Original-layout cells root BOOT.md carries as failing, **91 pass, identically to the
> proxy run, cell for cell** — the five-field correction moves none of the 92 verdicts,
> in either direction. Full table: HISTORY.md#accumulator-error-declared-kind-split-result.
>
> Two sharper checks the audit itself proposed, predicted before measuring. **Integer
> counters** (cannot round, a trajectory check): predicted P33's unmodified port
> already matches the original's `NFX`/`NFY`/`NFQ`/`NFW` exactly, and rounding changes
> nothing there since nothing was failing; confirmed, both builds `88763`/`6910685`/
> `110449`/`1555841`, equal to `tests/Fixtures/references/P33`. Predicted the
> declared-kind build might bring HMX's counters to match the original exactly (the
> audit's own hedge, "may"). Refuted: unmodified port `6516126`/`280860099`/
> `1165461`/`27583986` against the original's `6507516`/`280025235`/`1163946`/
> `27557169`; the declared-kind build leaves `NFX`/`NFY` **exactly unchanged** from the
> unmodified port and moves `NFQ`/`NFW` to `1165835`/`27579757` — closer on neither
> count than the unmodified port, on `NFW` further away. The
> `JammedTotal`/`NnTotal`/`VmkmTotal2`/`VdokTotal2` correction does reach the
> between-cycle `pdoksmall` this counter depends on (`Vks` alone would not: a build
> carrying only the `Vks` correction, `NFQ`/`NFW` unchanged from the unmodified port,
> confirms this), but not in a way that reproduces the original's own trajectory.
>
> **Headline outputs the criterion does not see**, HPEPA3: predicted (the audit's own
> figures) the unmodified port's `Dkarm43_cor(2)`/`Zkarm_cor(2)`/`Dagg43_cor(2)` read
> roughly +6.7%/+5.0%/+6.7% high against the original, and declared-kind rounding
> returns them to about 96.4/0.577/71.2. Measured against
> `tests/Fixtures/references/HPEPA3` (the single shipped reference, not the replica
> mean the audit used, so exact percentages differ): unmodified port `103.42`/
> `0.6121959`/`76.42` against the reference's `96.48`/`0.5687045`/`71.29` (+7.2%/+7.7%/
> +7.2%, same direction and order of magnitude); declared-kind build `97.76`/
> `0.5917524`/`72.24` (+1.3%/+4.1%/+1.3%) — closely matching the audit's predicted
> values, confirming the mechanism where the statistical criterion does not reach. The
> array-vs-scalar proxy gives the same three values to four significant figures
> (`97.76`/`0.5917504`/`72.24`): on this formulation and these three cells `Vks`'s own
> rounding error is too small to matter, so the proxy's practical consequence, measured,
> is confined to the counter finding above.
>
> What carrying this into the tree would cost, for the owner: (1) it is a third
> exception to "Double precision only", needing its own explicit paragraph there,
> argued as reproducing a declared numeric defect (as the taboo already permits) rather
> than emulating the model's arithmetic; (2) `RoundReal4`'s `(double)(float)value`
> cast puts the token `float` inside kernel code, against "Kernel-compatible C#"'s
> literal wording, unresolved here; (3) the accumulator-rounding choice can be a
> parameter of the one `Attempt.Run` (a bool threaded to each declared-REAL*4 site), not
> a second implementation; (4) **untested and load-bearing**: this recipe only ran
> reference mode, one particle at a time on one host thread, writing straight into the
> persistent total. Batched/CUDA mode has many particle threads landing on the same
> array cell concurrently, and root's "no atomic operation on a `double`" already rules
> out a lock-free fix — carrying REAL*4 fidelity into batched mode is an open design
> question this reconnaissance did not attempt, not a detail left over from this one.

All four points of that last paragraph are resolved by the implementation this entry
was moved to make room for: (1) the root's "Double precision only" invariant now
states the `AccumulationKind` exception directly; (2) `float` appears in kernel code
only inside `Attempt.AddReal4`, an explicitly declared, tested exception
(`tests/Protocol.Tests`' own reflection check needs a matching update, escalated —
this node cannot make that check green itself, AGENTS.md §11); (3) the kind is exactly
such a parameter, threaded through `ModelSetup.Kind`; (4) batched/CUDA mode is left
unaddressed here too, and the root's own new invariant answers it by refusing
`Original` accumulation in batched mode outright rather than attempting a lock-free
reduction — a decision this node's `API.md` states but does not enforce (the refusal
belongs to whichever node owns run configuration).

<a id="accumulator-error-array-vs-scalar-classification-rationale-superseded"></a>

## 2026-09-21 — from "## Accumulators" — the array-vs-scalar classification's own rationale, superseded

Kept whole for the next design session: the two claims below turned out false, found
by an owner-requested audit verified against `tests/Fixtures/Legacy/PropStructv3.for.txt`
in this same session (`BOOT.md`'s current paragraphs give the corrected account and the
declared-kind classification that replaces this one; the *result* the array-only split
measured — 91/92, zero new failures — is unchanged and still current, only the reason
given for it was wrong).

Claim 1, in "Confirmed end-to-end 2026-09-21" (the uniform-rounding paragraph): "every
`## Accumulators` real-valued write site in `Attempt.cs` (all confirmed REAL*4 by the
Fortran's own implicit typing; `fmkarm2_loc` too) rounded `(double)(float)` after each
addition". False: `Xss0`-`Xss6`, `DOK_base41/31`, `DOK_sur41/31`, `sd3/sd4`, `d31/d41`
and `dp31/dp41` are declared `real*8` explicitly at Fortran source lines 44, 42 and 45
respectively — not implicit, and not REAL*4. No `IMPLICIT` statement occurs anywhere in
the source (checked by search over the whole file), so "implicit typing" was invoked
for names that were never implicitly typed in the first place.

Claim 2, in the same paragraph and in the "**So**:" paragraph that follows it: the
39-new-failures result was attributed to "the x87-intermediate shape `## Purpose`
already named for why byte-exact replay was ruled out: 80-bit registers most likely
keep these long-lived scalar accumulators resident across far more additions than a
literal per-store REAL*4 rounding assumes, over-degrading exactly the sums with the
largest term count, while the per-cell array accumulators — far smaller term counts
each — round correctly at this granularity" and, in the "So" paragraph, "a uniform,
naive rounding is not the right carrier of a fix — it must distinguish per-cell array
accumulators (reproduce well) from whole-population scalar accumulators (over-degrade,
likely masked by x87 register residency in the real executable, unmodelled here)."
Unnecessary and wrong: every one of the 39 newly-failing quantities' feeding scalars is
declared `real*8` in the source (claim 1 above), so rounding them to binary32 was never
a reproduction of the original's own arithmetic at all, whatever a 32-bit compiler's
x87 register allocation does or does not do across the `RANDOM2`/`SIZE` calls the
attempt loop makes on every pass — a 32-bit calling convention has no reason to keep a
value resident across such a call, and the question does not need answering, since the
declared kind alone already accounts for the whole 39-cell result without it.

Classification rule this replaced, from "**Tested 2026-09-21, the split the paragraph
above called for**": "a field is an ARRAY accumulator, rounded after every addition,
exactly when its `AccumulatorLayout` offset advances by `Ndok`, `Nkarm` or `Ncat`
(`Allvdok`, `Vdokstr`, `Vsmkm`, `Svd`, `VmkmTotal`, `VdokTotal`, `Vks`, `FmkarmCor`,
`Fmkarm2`, `Dokp41`, `Dokp31`, and scratch `fmkarm2_loc`, which feeds `Fmkarm2`); every
field whose offset advances by 1, including all seven `Xss` slots, is a SCALAR, left in
`double`, unrounded (`DokBase41/31`, `DokSur41/31`, `Sd4/Sd3`, `D41/D31`, `Dp31/Dp41`,
`JammedTotal`, `NnTotal`, `VmkmTotal2`, `VdokTotal2`; `DpMax`/`DpMaxCor` are
assignments, not sums, and were left alone in both this run and the uniform one)." This
array-vs-scalar proxy happened to agree with the source's own declared kind for ten of
the fifteen accumulator groups, by coincidence of how this particular Fortran program
declares its arrays and scalars, and disagreed on five: `Vks` (an array, but declared
`real*8` at line 13 — the proxy wrongly rounded it); `JammedTotal`/`NnTotal` (scalars,
but declared `real` at lines 33/32 — REAL*4 by explicit declaration, not implicit
typing, and the proxy wrongly left them unrounded); `VmkmTotal2`/`VdokTotal2` (scalars
with no declaration anywhere in the source — REAL*4 by Fortran's implicit-typing
default, since no `IMPLICIT` statement narrows it and `V` is outside the `I`-`N`
integer range — the proxy wrongly left them unrounded too).

<a id="accumulator-error-array-vs-scalar-split-result"></a>

## 2026-09-21 — from "## Accumulators" — the array-vs-scalar rounding split, full result

Kept whole for the next design session, not superseded: `BOOT.md`'s dated paragraph
above this pointer gives the headline; this is the per-formulation table it summarizes.
Method identical to the 2026-09-21 end-to-end confirmation (reused throwaway solution
and `Compare.dll` tool, `tests/Fixtures/run_port.py`, reference mode, `Original`
layout, seed 0, all five formulations), with `Attempt.cs`'s `RoundReal4` sites narrowed
to the array-class accumulators named in `BOOT.md`; `Engine.RunReferenceParticle`'s
direct-to-`_realTotals` write, unchanged from before. Both builds' baselines were
regenerated fresh against this tree's own HEAD (commit `d12f140`) rather than reused
from the earlier run, to rule out drift; the regenerated baseline reproduced the root
acceptance criterion's own recorded counts exactly, cell for cell.

Statistical criterion (lagged replicas, `Original` layout), failing/compared:

| Formulation | Baseline (unmodified port) | Array-only-rounded candidate | New failures |
|---|---|---|---|
| HPEPA3 | 39/1822 | 0/1822 | 0 |
| inpt | 1/2493 | 1/2493 (`epsdokfr[0]`, unchanged) | 0 |
| P33 | 0/1462 | 0/1462 | 0 |
| PSAN02n | 6/2020 | 0/2020 | 0 |
| HMX | 46/4121 | 0/4121 | 0 |
| **Total** | **92/11918** | **1/11918** | **0** |

Every one of the 92 baseline-failing cells was checked by name and index against the
candidate's own statistical verdict, not just counted: 91 flip to passing (39 of
HPEPA3's, 46 of HMX's, all 6 of PSAN02n's), and the one that does not — `inpt`'s
`epsdokfr[0]` — is the REAL*4/REAL*8-mixing defect already declared elsewhere,
unaffected by an accumulator-rounding change as expected (`ref=2.7E-08,
float-build=0`). No cell of any formulation newly fails; the exact-print-resolution
counts (a looser check, not the statistical criterion) moved in the candidate's favor
too — HPEPA3 811/2000 against the baseline's 754/2000, HMX 2645/6334 against 2529/6334,
PSAN02n 1600/2034 against 1587/2034, P33 and inpt materially unchanged — consistent
with the array-class fields now agreeing with the original to more printed digits.

Discarded, never committed: the throwaway solution and `Compare.dll` tool live under
this session's own scratch directory, outside the tree, and are not reachable from a
future session; the table above and `BOOT.md`'s paragraph are the record.

<a id="accumulator-error-declared-kind-split-result"></a>

## 2026-09-21 — from "## Accumulators" — the declared-kind rounding split, full result

Re-run of the array-vs-scalar experiment above with the corrected, declared-kind
classification (`BOOT.md`'s current paragraph gives the rule and its derivation): a
fresh throwaway copy, the same two changes (`RoundReal4` on the declared-REAL*4 write
sites of `Attempt.cs`, `Engine.RunReferenceParticle` writing `_realTotals` directly),
`Compare.dll`/`ResultsMFile.Parse`/`StatisticalCriterion.Compare` unchanged, reference
mode, `Original` layout, seed 0, all five formulations, the port's own unmodified
Release build regenerated fresh against this tree's HEAD as the baseline.

Statistical criterion (lagged replicas, `Original` layout), failing/compared:

| Formulation | Baseline (unmodified port) | Declared-kind-rounded candidate |
|---|---|---|
| HPEPA3 | 39/1822 | 0/1822 |
| inpt | 1/2493 | 1/2493 (`epsdokfr[0]`, unchanged) |
| P33 | 0/1462 | 0/1462 |
| PSAN02n | 6/2020 | 0/2020 |
| HMX | 46/4121 | 0/4121 |
| **Total** | **92/11918** | **1/11918** |

Identical to the array-only proxy's own result, cell for cell: the five-field
misclassification the audit found does not move the verdict on any of the 92
known-failing cells of these five formulations — neither newly fixes one the proxy had
already fixed, nor newly breaks one, nor leaves one of the 91 unfixed. A second build
carrying only the proxy's five-field difference in the opposite direction (round `Vks`,
leave `JammedTotal`/`NnTotal`/`VmkmTotal2`/`VdokTotal2` unrounded — i.e. the original,
wrong proxy, rebuilt fresh in the same session to compare against) reproduces the same
91/92 and, on HPEPA3, headline quantities the criterion does not cover
(`Dkarm43_cor(2)` 97.76, `Zkarm_cor(2)` 0.5917504, `Dagg43_cor(2)` 72.24) matching the
declared-kind build's own (97.76, 0.5917524, 72.24) to four significant figures. `Vks`'s
own REAL*4-vs-REAL*8 error is evidently too small, at this term count, to move either
the criterion or these three headline cells on this formulation; `BOOT.md`'s own
integer-counter check is the one place a measurable difference between the two
classifications was found (HMX's `NFQ`/`NFW`, moved by the `JammedTotal`/`NnTotal`/
`VmkmTotal2`/`VdokTotal2` correction, not by `Vks`).

Discarded, never committed: both throwaway solutions and the `CompareTool` console
project live under this session's own scratch directory, outside the tree.

<a id="accumulator-error-per-drawn-particle-worst-cell-table"></a>

## 2026-09-21 — from "## Accumulators" — the per-drawn-particle worst-cell table, superseded by the end-to-end confirmation

Moved to make room for the 2026-09-21 end-to-end reconnaissance the paragraph above
this pointer now states. Kept for provenance: this table is the shadow-accumulator
diagnostic that first showed the candidate large enough to matter and drove the
escalation to a real, whole-solution run.

> Worst-cell relative error (`Allvdok`/`Vdokstr` exact; the rest lumped as above):
>
> | | HPEPA3 | inpt | P33 | PSAN02n | HMX |
> |---|---|---|---|---|---|
> | `Allvdok` / `Vdokstr` | 46% / 46% | 0.029% / 0.027% | 0.081% / 0.076% | 3.4% / 3.2% | 65% / 65% |
> | `FmkarmCor` / `Fmkarm2` | 0.12% / 0.0020% | 0.0016% / 0.00035% | 0.0041% / 0.000017% | 0.0045% / 0.00086% | 0.027% / 0.00044% |
> | `Dokp41` / `Dokp31` | 0.74% / 0.85% | 0.0014% / 0.00027% | 0.0012% / 0.00064% | 0.010% / 0.049% | 1.4% / 0.78% |
>
> Against the criterion's own scale (unchanged method): `Fmkarm2` rules the REAL*4
> story out everywhere; `FmkarmCor` now reads inconclusive on four of five (the
> lumped share moved it out of "rules out"), rules out only on inpt. `Dokp41`/
> `Dokp31` (both feed `dokkarm43`): **large enough to matter on HPEPA3 and HMX, now
> numerically exceeding the whole replica-to-replica band** (ratio 1.1-2.8) — REAL*4
> error alone could fully explain those two formulations' failing `dokkarm43` cells;
> PSAN02n's `Dokp31` also newly reads "matters" (12%, was 0.25%); inpt and P33 stay
> small. **The REAL*4 summation candidate is reopened**, decisively for HPEPA3's and
> HMX's `dokkarm43` cells. A second check reachable without `Statistics`/`Output`:
> `Allvdok`'s bootstrap-extended retained fraction (float32 ÷ double at the true
> full-run term count) is 0.540 on HPEPA3's cell 1 and 0.346 on HMX's, against the
> audit's own 0.5405/0.3469 — a match this tree's replay did not import, corroborating
> the audit's accumulation mechanism, not its further, unreproduced claim that this
> reaches `fmdok`/`pdoksmall`/`Dok43all` themselves.

---

<a id="accumulator-sizes-double-vs-binary32"></a>

## 2026-09-21 — from "## Accumulators" — sizes first said computed in double

> ⚠ 2026-09-17: the sizes were first said to be computed in `double`. The fidelity audit
> of `Statistics` showed that `double` operands give `Ndok` 72 instead of the printed 71
> for C166 and T56 and differ for CSPX01, P18050 and PSAN01.

---

<a id="vdoks-not-derived"></a>

## 2026-09-21 — from "## Accumulators", "Decisions where the port departs from a transcription" — VDOKS first said derived by Statistics

> ⚠ 2026-09-17: this item first said that `Statistics` derives `VDOKS` as
> `Qdoks × constant`. The design session of `Statistics` found that no printed quantity
> depends on `VDOKS`, so nothing derives it.

---

<a id="accumulator-error-attempt-granularity-superseded"></a>

## 2026-09-21 — from "## Accumulators" — the 2026-09-20 REAL*4 accumulation measurement, superseded by a granularity fix

Moved to make room for the corrected 2026-09-21 measurement the paragraph above this
pointer now states. The superseded text below was not wrong about its own method — its
own remarks already read a lower bound for five of the eight fields — but the documents
built on it as if it were the effect (root BOOT.md, 2026-09-21, "was closed on a lower
bound read as a measurement"). Kept for provenance: it is the record of what the first
measurement actually did and why an owner-requested audit, verified in this tree, found
it insufficient.

> Measured 2026-09-20 (`tests/Particle.Tests/AccumulatorSweep.cs`,
> `RealFourAccumulationErrorTests.cs`), for `Allvdok`, `Vdokstr`, `VdokTotal`,
> `VdokTotal2`, `Dokp41`, `Dokp31`, `FmkarmCor`, `Fmkarm2`: a 100 000-attempt
> reference-mode sample of each formulation (cycle 0, then cycle 1; `pdoksmall`/
> `Dmaxxx` left permissive, since real values need `Simulation`'s own cycle loop, out
> of this node's reach) sums every attempt's own delta to each field twice, `double`
> and a binary32 shadow kept only in the test, never inside `Attempt.Run`. Every term
> is non-negative, so the field's running `double` total is Σ|term| exactly at any
> grouping; the worst cell's own observed deltas are then bootstrap-resampled out to
> that cell's true term count (scaled from the sample's own `NFY` by the reference's
> printed one) to see the saturation this bullet names, which count-and-magnitude
> bounds alone cannot: at HMX's true scale the worst `Allvdok`/`Dokp41` cells have up
> to 53-62% of their most recent additions rounded away entirely. Worst-cell relative
> error at that true scale:
>
> | | HPEPA3 | inpt | P33 | PSAN02n | HMX |
> |---|---|---|---|---|---|
> | `Allvdok` | 0.19% | 0.0022% | 0.0041% | 0.012% | 1.9% |
> | `Vdokstr` | 0.13% | 0.0016% | 0.0042% | 0.11% | 0.72% |
> | `Dokp41` | 0.061% | 0.0001% | 0.0004% | 0.0073% | 0.33% |
> | `Dokp31` | 0.024% | 0.0001% | 0.0003% | 0.001% | 0.16% |
> | `FmkarmCor`/`Fmkarm2`/`VdokTotal`/`VdokTotal2` | ≤ 0.003% on every formulation | | | | |
>
> Against the criterion's own scale — each worst cell's lagged-replica standard
> deviation, floored by the print resolution where replicas agree to more digits than
> that (root `## Constraints`, "floors from the print resolution"), computed here from
> public `Harness`/`Fixtures` data only, not from `Harness`'s own internal calibration
> (AGENTS.md §3) — `FmkarmCor` and `Fmkarm2` rule the REAL*4 story out on all five
> formulations (at most 2% of the replica spread). `Dokp41` and `Dokp31` both feed the
> one printed `dokkarm43` (`Statistics/API.md`'s `CycleStatistics.Compute`, "rewritten
> in place: Dokp41, Dokp31..."; `Categories.MergeAndDescribe`'s own out parameter is
> `dokp43`, matching the printed name — there is no signature evidence here for a
> separate `dokkarm10` fed by either field, so that quantity is not claimed):
> large enough to matter on HMX (23-49% of the replica spread, one side held fixed at
> a time) and on HPEPA3's `Dokp41` (20%, `Dokp31` inconclusive at 8%); ruled out or
> inconclusive-but-small (≤ 1.4%) on the other three. `dokkarm43` is compared by
> `Harness`'s own adaptive-index-matched rule, not the plain per-cell one this
> reproduction approximates (`tests/Harness/API.md`, `CompareAdaptiveIndexMatched`),
> so this verdict is a proxy, not `Harness`'s own, and a one-sided sensitivity (the
> numerator and denominator share correlated rounding that could partly cancel).
> `Allvdok`, `Vdokstr`, `VdokTotal`, `VdokTotal2` are not themselves printed; reaching
> their own band needs `Statistics`'s/`Output`'s derivation, which this node may not
> read (AGENTS.md §3) — widths this measurement could not reach, reported above in
> the accumulator's own units only.

---

<a id="line-map-column-goto-labels-not-method-per-block"></a>

## 2026-09-21 — from "## Line map" — the "C# member" column first tried a method-per-block split that coding proved impossible

Moved to make room under the §15 line limit for the causal measurement of the
QKS1/FQKS/AUS/TU defect (`## Defects of the original`, Fortran 589, 601, 716): the
current wording already states the resolution (one flat `Attempt.Run` using `goto` for
the Fortran labels) in the paragraph above the pointer; what follows is the superseded
attempt and the reasoning that ruled it out, kept for provenance, not needed to
implement or extend the node today.

> ⚠ 2026-09-17: the "C# member" column first split the attempt into method-per-block
> names that do not exist: `Attempt.DrawBase`, `Attempt.DrawDistance`,
> `Attempt.DrawNeighbour`, `Attempt.ClassifyGap`, `Attempt.CountBridge`,
> `Attempt.CountPocket`, `Attempt.CountCategory`, `Attempt.Finish`. Coding revealed why a
> method-per-block split cannot work: several of these Fortran blocks pass control *into*
> another block, not just out of their own end — `GO TO 11` (lines 473, 476, 480, 549,
> 568, 590) must end the attempt itself, and `GO TO 501/502/550` and the fall-through into
> label 333 land in the *middle* of a different block's own code. A method boundary can
> carry a return value out, never a jump into a sibling method's interior; representing
> that honestly would mean every "block method" returns "which label to resume at next"
> and a dispatcher in `Attempt.Run` re-enters accordingly — an ad hoc state machine
> duplicating the very control flow a literal, line-by-line transcription exists to avoid
> (root BOOT.md, Taboos: "no second implementation of any part of the particle program").
> `Attempt.Run` is therefore one flat method using `goto` for the Fortran labels
> themselves (`Label501`, `Label502`, `Label333`, `Label550`), each a real, unique label
> in `src/Particle/Attempt.cs` a reader can find by name; only the blocks with no such
> cross-block jump (`SizeLaw.Sample`, `BridgeWindow.Build`, `BridgeWindow.SamplePocket`,
> `BridgeGeometry.Volume`, `PocketHistogram.Normalize`) are separate members, because they
> are pure functions with nothing to jump into. The column below now names, for every
> row inside `Attempt.Run`, the label it falls under (or "top", before any label). The
> Fortran-line side of the mapping stays machine-checked regardless of this column
> (`tests/Particle.Tests/LineMapCoverageTests.cs`, which reads line ranges only); this
> column's own truth — that the named label exists — is checked by a reader grepping
> `src/Particle/Attempt.cs`, not by a script, since AGENTS.md §13's reflection checks are
> written for exported members, not `goto` labels internal to one.

---
