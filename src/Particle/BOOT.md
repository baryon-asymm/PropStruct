# BOOT.md — Particle

## Purpose

The particle program: one attempt at a base particle with its surroundings, as the
original's main loop performs it (Fortran lines 425–767, with the subroutines `SIZE`,
`DM` and `VM`), in kernel-compatible C#. The node also owns the kernel form of the
model setup, the cycle inputs, the accumulator layout (shared integer totals and a
per-particle real-valued record), the normalization of the pocket histogram, and the
fixed-order fold of records into totals: the layout is defined by which side effect
happens where in the attempt, so no node that does not know the attempt can own it.
It is the one place in the tree where the model's per-particle physics lives.

Line numbers below refer to the original source `PropStructv3.for` (the UTF-8 copy in
`tests/Fixtures` has the same numbering).

## Invariants

- **One attempt, one outcome.** An attempt runs from Fortran label 11 (line 428)
  through the acceptance tests after the neighbour loop (lines 719–756) and ends with
  exactly one `AttemptOutcome` (see `## Attempt structure`).
- **Every side effect of the original, in the same place.** Each accumulator update of
  lines 425–756 happens in the same branch and in the same order relative to the draws
  as in the original, including the updates of attempts that are later rejected, with
  the exceptions listed under "Decisions where the port departs from a transcription".
  This is itself a defect of the original, declared for `## Defects of the original`
  (2026-09-20): an attempt later ended as `RestartedAfterLoop` by the acceptance tests
  of lines 723–738 keeps every histogram, bridge and pocket contribution it made
  before those tests fired — nothing is undone on rejection.
- **Draws in the original order from the original streams.** Stream 1 → X, 2 → X0,
  3 → X1, 4 → X2, 5 → X21, 6 → X3 and X4 (shared). A draw that the original repeats
  (`X1 == 1`, line 496; `JJ == 0`, line 625) is repeated from the same stream.
- **Frozen inputs.** The attempt reads QKS1, `pdoksmall`, `Dmaxxx` and the cycle flag
  only from `CycleInputs`, and the setup only from `ModelSetup`; it never writes
  them. Refreshing QKS1 is `PocketHistogram.Normalize`, called by the driver.
- **Integer side effects by atomic add, real side effects into the own record.**
  Every counter and count histogram goes to the shared integer totals with an atomic
  add; every real-valued sum or maximum goes to the particle's record; locals of the
  attempt live in the particle's scratch. Nothing else is written. The fold adds
  records into real totals in particle order, so totals are bit-identical for any
  execution order of the particles.
- **IEEE semantics as in the original.** Division by zero in `nn` (line 722) yields
  `+∞` or `NaN` and the comparisons of lines 731 and 735 behave as IEEE comparisons,
  as the original executable (no floating-point traps) does.
- **Kernel-compatible.** Static methods, blittable structs, `ArrayView` inputs, no
  `float`, allocation, exception, virtual call, recursion or mutable static.

## Dependencies

- [Random](../Random/API.md) — `StreamSet`, `Mcg128.Next`.
- [legacy](../../tools/legacy/API.md) — `legacy_file`, through which the REAL*4
  classification script reaches the original's source; no C# of this node depends on it.

Outside the tree: ILGPU 1.5.3 (`ArrayView`, `Atomic`), used by internal types only.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Literal constants as written, each a named constant with its line: `3.14159` in
  `π/6·D³` (lines 466, 469, 484, 521, 524, 542, 643, 648, 665, 677, 685, 702, 710,
  747, 750, 754) and in `rrL` (line 501), `12.56636` (lines 430, 716) and `/200` (line 716), `3.14` in `VM`
  (lines 1616, 1624–1626), `1e-5` (line 601), `1/3.` (line 501) taken as the `double`
  `1.0 / 3.0`.
- Preconditions on the setup, checked by `Statistics.Prepare`: `0 < AK3 < AK4`,
  `Di > 0`, `Dj > 0` (the original reads AK1..AK4 unchecked; with `AK3 ≤ 0` the indices
  of lines 584 and 705 can fall below 1). The size law is uniform when `JZZ = 2` and
  uniform in `1/D²` (mass-uniform) for any other value, as every test of the source is `JZ.EQ.2`
  (lines 382, 1700, 1768).
- Scratch of one particle: the `AccumulatorLayout.ScratchLength` array of `fmkarm2_loc`
  (`Nkarm`), `FQKS` and `DPOC` (`Nkarm + 1` each); the reasoning for what stays out of
  it is `AccumulatorLayout.ScratchLength`'s own XML doc.
- Budgets passed in `ModelSetup`: neighbour draws per attempt (distance draws of label
  501) and pocket-size redraws per bridge (label 451). The original has none and would
  loop; exceeding one ends the attempt with a failure outcome.

⚠ Declared deviation, §6: the specification of this node is the Fortran source by the line ranges of `## Line map`, not a self-sufficient retelling — retelling 350 lines of branchy code would produce a second, diverging model — replaced by: ## Line map, ## Defects of the original.

Lifted only if the model is ever specified independently of the original.

⚠ 2026-10-04: the source this node's line numbers refer to, named here by its path under `tests/Fixtures/Legacy`, lies outside the repository from stage S2 (`tools/legacy`): a reader of the public tree cannot consult it, and the checks that read it are `Category=Legacy` → tests/Fixtures/HISTORY.md#legacy-out-of-tree-2026-10-04

⚠ 2026-09-26: the §15 deviation this document used to declare here is withdrawn: the
§6 form correction above (the deviation was already true but was not written in the
form the linter reads) excludes `## Line map` and `## Defects of the original` from
the count, and the moves recorded through this document's ⚠ pointers bring the rest
inside the 400-line leaf limit → HISTORY.md#section-15-deviation-withdrawn-2026-09-26

## Attempt structure

```text
label 11 (428–445)  reset locals, TU = 12.56636
base (448–484)      X, X0 → SIZE → Dr, fraction; base histograms (immediate)
                    SFR = 0 → restart inside; Dr ≥ Dmax → c1, restart inside;
                    Dr ≤ Dmin → c2, restart inside; Sd4, Sd3; VP = π/6·Dr³
loop 501 (487–501)  X1 (redraw if 1.0); KCIL = −(ln(1−X1)·(1/λ)) + VP; VP = KCIL; rrL
     345 (503–543)  X2, X21 → SIZE → Db; neighbour histograms (immediate)
                    SFR = 0 → next 501; [529: Dr ≥ Dmax, dead, omitted];
                    Db ≤ Dmin → c2, next 501; D41, D31; AA; vdokstr, qdokstr
     (547–555)      Dr < AK1·Db → c3, restart inside if ivar = 0 else next 501;
                    Dr > AK2·Db → c4, next 501
     551 (559–576)  coef; AA > AK4·max → c5, restart inside; AA > AK3·max → 502;
                    AA ≤ 0 → rrL = (Dr+Db)/2, AA = 0, jammed
     bridge (580–667) ibridge (every bridge, every cycle); cycle 0 → 550; window CDF
                    from QKS1 (empty → restart inside); 451: X3 → DM → Dkarm → VM
                    (JJ = 0 → 451, the XSS5/NFQ updates repeat); qmkm1 (AA > 0), qmkm2;
                    VSMKM1, SVD1; Var#1 (≤ Dmaxxx); Var#2: X4 vs k8/k7·pdoksmall → 550
     pocket 502 (671–703) ipocket; DPmax; QKS, VKS, DP31, DP41; cycle 0 → 333;
                    Var#1: pdoksmall ≥ 1 → 333, else DPmax_cor, fmkarm_cor, fqkarm_cor;
                    Var#2: X4 < pdoksmall → 333, else DPmax_cor, ipocket_cor, fmkarm2_loc
     333 (705–711)  QDOK, DOKP41, DOKP31, QDOKS by (int(RK/Dj)+1, int(max/Di)+1)
     550 (715–716)  TU −= Db²/rrL²; TU > 12.56636/200 → next 501
end (717–756)       loop completed. Cycle ≥ 1: fmkarm2 += fmkarm2_loc; c6..c9 tests
                    → restart after loop; else committed accumulators (Vmkm_total,
                    Vdok_total only if ibridge_cor > 0; the ·2 scalars only if
                    ibridge_cor2 > 0) → accepted.
                    Cycle 0: accepted.
```

Outcomes:

| Outcome | When | Driver refreshes QKS1 (reference mode) |
|---|---|---|
| `Accepted` | end of the loop and, in cycles ≥ 1, no test of lines 723–738 fired | yes |
| `RestartedAfterLoop` | end of the loop, a test of lines 723–738 fired | yes |
| `RestartedInsideLoop` | any `GO TO 11` of lines 473, 476, 480 (before the loop) or 549, 568, 590 (inside it) | no |
| `NeighbourBudgetExceeded` | distance draws of the attempt exceed the budget | — (run fails) |
| `BridgeDrawBudgetExceeded` | redraws of label 451 exceed the budget | — (run fails) |
| `IndexOutOfRange` | an unguarded cell index or the `DM` search leaves its layout (impossible under the preconditions; a guard, not a model branch) | — (run fails) |

`Coef`, `Qmkm1` and `Qmkm2` skip an index above `Nc` silently, as lines 561, 629 and 636
do; this is model behaviour, not `IndexOutOfRange` (HMX fills `coef` up to index 842).

The refresh of lines 717–718 happens in the original after every completed loop in
every cycle, whatever follows; in the port the driver calls
`PocketHistogram.Normalize` on the outcomes marked "yes". The integer total
`LoopCompletions` counts them, so a batched driver can tell whether the original would
have refreshed since the last refresh; it does not say whether `Qks` changed (attempts
restarted inside the loop add pockets too, and a completed loop may add none).

⚠ 2026-09-25: was this rule read as a defect of the original (declared for `##
Defects of the original`, 2026-09-20) on evidence (`TotalAttempts`/`Nfx`/`IbridgeTotal`
bit-identical across batch configurations) that never bore on the question — those
three totals are decided by streams 1–5 before any bridge window is touched, so they
are independent of QKS1's content by construction (fidelity audit 2026-09-24, U-P2) —
now read as a model feature, below → HISTORY.md#qks1-refresh-defect-withdrawn

**Read again, together with 580–610**: `mindk`/`maxdk` are re-derived from `Dr` at the
first bridge of every attempt ("Bridge window once per attempt" below), against
whatever QKS1 currently holds, and 717–718 refresh QKS1 after *every* completed
neighbour loop of *every* cycle, cycle 0 included. This is an online, self-consistent
estimate of the pocket-size distribution, continuously refined as the run makes more
pockets, not an accident: cycle 0 exists for no purpose *but* to build QKS1 up before
cycle ≥ 1 ever reads it (its own bridges skip the window entirely, `IPRIS.EQ.0 → GO TO
550`, line 581), and every attempt of cycle ≥ 1 both draws from and adds to the same
evolving histogram. Read as a **model feature**, not a defect; the row is withdrawn
from `## Defects of the original` below.

Measured 2026-09-25: zero of 15,036 compared cells differ over eight seeds `k·2¹⁷` and
all five reference formulations (largest `|t|` 1.08), on every quantity a bridge's
window content can plausibly reach, while a same-seed diff proves the variant live at
2,127 of 117,054 cells — the effect is real per seed but is Monte Carlo noise at the
population level. Full method, controls, and a note on a neighbour's file this
measurement touched: `HISTORY.md#qks1-refresh-cadence-measurement-2026-09-25`.

## Line map

This table's declared scope (`## Purpose`; `## Acceptance criteria`, "The line map
covers every executable line...") is every executable line of Fortran 425–767,
1573–1586, 1588–1629 and 1761–1776; `tests/Particle.Tests/LineMapCoverageTests.cs`
reads these four ranges from this one sentence, not from a second, independently-typed
copy of its own (2026-09-17, closed an audit note).

`Attempt.Run` is one flat method using `goto` for the Fortran labels themselves
(`Label501`, `Label502`, `Label333`, `Label550`), each a real, unique label in
`src/Particle/Attempt.cs` a reader can find by name; only the blocks with no
cross-block jump (`SizeLaw.Sample`, `BridgeWindow.Build`, `BridgeWindow.SamplePocket`,
`BridgeGeometry.Volume`, `PocketHistogram.Normalize`) are separate members, because
they are pure functions with nothing to jump into. The column below names, for every
row inside `Attempt.Run`, the label it falls under (or "top", before any label).

⚠ 2026-09-17: was a method-per-block split (`Attempt.DrawBase`, `Attempt.DrawDistance`,
`Attempt.DrawNeighbour`, `Attempt.ClassifyGap`, `Attempt.CountBridge`,
`Attempt.CountPocket`, `Attempt.CountCategory`, `Attempt.Finish`), now the flat,
`goto`-based `Attempt.Run` above → HISTORY.md#line-map-column-goto-labels-not-method-per-block

| Fortran | C# member | Note |
|---|---|---|
| 425–427, 767 | driver (`Execution`) | per-particle loop; `attempt` (unused) not ported |
| 428–446 | `Attempt.Run` (top, before `Label501`) | kernel-thread locals, not scratch (`## Constraints`, "Scratch of one particle") |
| 448–484 | `Attempt.Run` (top, before `Label501`) | |
| 1761–1776 | `SizeLaw.Sample` | `MINV` stale for `X == 0` unreachable (state is odd, `X > 0`): fraction left at 1 |
| 487–501 | `Attempt.Run` (`Label501`) | budget counted here |
| 503–555 | `Attempt.Run` (`Label501`) | line 529 omitted (cannot fire: 474 already restarted on `Dr ≥ Dmax`); `idok_local_all` (541, never read) not ported |
| 559–576 | `Attempt.Run` (`Label501`, the "551" gap-coefficient block) | |
| 580–581 | `Attempt.Run` (`Label501`, before the window is built) | at every bridge |
| 582–611 | `BridgeWindow.Build` | built at the first bridge of an attempt and reused, see below |
| 612–625, 1573–1586 | `BridgeWindow.SamplePocket` | `DM`, with index guard |
| 1588–1629 | `BridgeGeometry.Volume` | `VM`; `error` (line 1611) not ported; argument swap on copies |
| 626–667 | `Attempt.Run` (`Label501`, the rest of the bridge block) | |
| 671–703 | `Attempt.Run` (`Label502`) | `RK ≤ 0` (line 674) cannot happen under `AK3 > 0`; kept; `ipocket_loc_cor` (701, never read) not ported |
| 704–711 | `Attempt.Run` (`Label333`) | `VDOKS` not accumulated, see accumulators |
| 714–716 | `Attempt.Run` (`Label550`) | |
| 717–718 | `PocketHistogram.Normalize` | called by the driver |
| 719–756 | `Attempt.Run` (`Label550`, the post-loop acceptance tests and commit) | |
| 758–766 | — | console progress, not ported |
| 452–458, 490–494, 507–513, 615–619, 655–659, 691–695 | — | GSV ≠ 2 branches, not ported (root: GSV=2 only) |

**Bridge window once per attempt.** Lines 582–610 depend only on `Dr`, `Di`, `AK3`,
`AK4` and QKS1, none of which changes within an attempt, so the window (`FQKS`,
`DPOC`, `NNN1`) is built at the first bridge of a cycle ≥ 1 attempt and reused by later
bridges of the same attempt. The empty-window restart (line 590) therefore fires at
the first bridge, after the side effects that precede it, exactly as in the original.
The window reproduces the original literally: `DPOC(mindk) = Di·mindk` (the upper
bound of the cell, as written), the truncation at `1 − FQKS < 1e-5` (lines 600–606),
the shift to index 1 (lines 607–610).

`DPOC(mindk) = Di·mindk` is an off-by-one, not an intended upper-bound or centre
convention (fidelity audit 2026-09-24, U-P1): `mindk` uses the source's own
`iks = int(RK/Di)+1` convention (line 675) throughout, while `DPOC`'s own later
consumer, `Dkarm43_cor`/`Dqkarm_cor` (lines 1085–1091), anchors a cell at its *centre*,
never its upper edge. Measured: `Dqmkm2` moves −2.8 % to −7.0 % and `Zkarm` +4.9 % to
+13.5 %, both far beyond the seeds' own spread; full method, controls and table:
`HISTORY.md#bridge-window-origin-audit-2026-09-24`; the defect row is in `##
Defects of the original` below.

## Accumulators

Sizes from the setup: `NMM` fractions; `Ndok = int(Ddokmax/Di)+2`,
`Nkarm = int(Ddokmax·AK4/Di)+2`, `Ncat = int(Ddokmax·AK4/Dj)+2` (lines 274–276,
computed by `Statistics.Prepare` from the binary32 values the original holds, see
`src/Statistics/BOOT.md`), `Nc = 1000`. Indices are the Fortran indices minus one.

⚠ 2026-09-17: was sizes computed in `double`, now computed with integer mantissa
arithmetic on the binary32 values → HISTORY.md#accumulator-sizes-double-vs-binary32

The field layout of both buffers — name, length, stride and cell meaning — is
`API.md`'s contract ("Accumulator layout"), not repeated here. What follows is which
Fortran line writes each field and why it sits in the buffer it does — the attempt's
own reasoning, needed to implement `Attempt.Run`, not to call it.

⚠ 2026-09-17: was this document itself stating the field layout (name, length, stride,
cell meaning) inline, now moved out to `API.md`'s own contract → HISTORY.md#field-layout-moved-to-api-md

**Integer totals**, written by:

| Field | Lines |
|---|---|
| `Conditions` | 475, 479, 530, 534, 548, 553, 567, 724, 728, 732, 736 |
| `NFX`, `NFY`, `NFZ`, `NFQ`, `NFW` | 463, 498, 518, 622, 662, 698 |
| `IbridgeTotal` | 744 (committed) |
| `LoopCompletions` | port only: completed neighbour loops |
| `AlldokFract` | 465, 520 |
| `Alldok` | 468, 523 |
| `Qks` | 676 |
| `FqkarmCor` | 686 |
| `Coef`, `Qmkm1`, `Qmkm2` | 562, 630, 637 |
| `Qdoks` | 711 |

The `*_nmax` values of the original (lines 563, 631, 638) are the largest non-empty
index of their histograms and are derived by `Statistics`, not accumulated.

**Real-valued record**, written by:

| Field | Lines |
|---|---|
| `Xss0`…`Xss6` | 461, 462, 497, 516, 517, 621, 661/697 |
| `DokBase41`, `DokBase31`, `DokSur41`, `DokSur31` | 470, 471, 525, 526 |
| `Sd4`, `Sd3`, `D41`, `D31`, `Dp31`, `Dp41` | 482, 483, 537, 538, 678, 679 |
| `DpMax`, `DpMaxCor` | 673, 684/700 |
| `JammedTotal`, `NnTotal`, `VmkmTotal2`, `VdokTotal2` | 741, 743, 753, 754 |
| `Allvdok`, `Vdokstr` | 469, 524, 542 |
| `Vsmkm`, `Svd`, `VmkmTotal`, `VdokTotal` | 746, 747, 749, 750 |
| `Vks`, `FmkarmCor` | 677, 685 |
| `Fmkarm2` | 720 |
| `Dokp41`, `Dokp31` | 707, 708 |

"Committed" updates happen only on `Accepted` in cycles ≥ 1, from the attempt's
locals: `Vsmkm`, `Svd`, `JammedTotal`, `NnTotal` always; `VmkmTotal` and `VdokTotal`
only if the attempt counted a Var#1 bridge (line 748); `VmkmTotal2` and `VdokTotal2`
only if it counted a Var#2 bridge (line 752). Every other update happens where the
original makes it.

## Decisions where the port departs from a transcription

Declared, AGENTS.md §12.

- `VDOKS` (line 710) is not accumulated: it feeds only `VDOKSO` and `DOKP432`, which
  nothing prints (Fortran line 1397 is commented out); see `src/Statistics/BOOT.md`.

  ⚠ 2026-09-17: was `Statistics` deriving `VDOKS` as `Qdoks × constant`, now not derived
  at all → HISTORY.md#vdoks-not-derived
- Sums round to binary32 after every addition, through `Attempt.AddReal4`, exactly for
  the accumulators the Fortran declares REAL*4, when the run's `PrecisionKind` is
  `Original` (root BOOT.md, "Precision kind is an option of every run"; `Binary64`,
  the default, never rounds any of them). The rounded set is **generated**, never
  typed, from the Fortran declaration block plus its implicit-typing rule
  (`classify-real4-accumulators.py` → `RealFourAccumulators.generated.txt`, checked
  the way `docs/ORIGINAL-DEFECTS.md` is checked: a test regenerates and fails on any
  byte difference, `tests/Particle.Tests/RealFourClassificationGeneratorTests.cs`).
  Twenty-one write sites round: the fifteen `AccumulatorLayout`/scratch fields
  (`Allvdok`, `Vdokstr`, `Vsmkm`, `Svd`, `VmkmTotal`, `VdokTotal`, `FmkarmCor`,
  `Fmkarm2`, `Dokp41`, `Dokp31`, `JammedTotal`, `NnTotal`, `VmkmTotal2`, `VdokTotal2`,
  scratch `fmkarm2_loc`) plus the six attempt-local scalars the 2026-09-24 design
  decision below adds (`VSMKM1`, `SVD1`, `Vdok_loc`, `Vmkm_loc`, `Vdok_loc2`,
  `Vmkm_loc2`). Twelve fields stay `double` under either kind, all declared `real*8`
  explicitly (`Xss0`-`Xss6`, `DokBase41/31`, `DokSur41/31`, `Sd4/Sd3`, `D41/D31`,
  `Dp31/Dp41`, `Vks`); `DpMax`/`DpMaxCor` are running maxima, not sums, and are outside
  `PrecisionKind` entirely. The same generated scan (below) also finds further
  self-referencing REAL*4 names that are declared, not reproduced, each in "Decisions
  where the port departs from a transcription": `ALLVDOK_FR` and `AUS`/`TU` (already
  declared there), and `VMKM` (declared there for the first time, 2026-09-24).

  **Design decision, 2026-09-24 (owner and orchestrator): the membership of the rounded
  set is generated too, not only its kind.** Until then the script generated each
  name's kind, but the list of names was typed by hand (`FIELD_FORTRAN_NAMES`), which
  could not hold a scalar local: six REAL*4 sums at lines 642–666 (`VSMKM1`, `SVD1`,
  `Vdok_loc`, `Vmkm_loc`, `Vdok_loc2`, `Vmkm_loc2`) were missed and stayed `double`
  under `Original` (fidelity audit of 2026-09-24, P-1). Implemented the same day: the
  script now scans this node's own `## Line map` ranges directly, keyed by Fortran
  name; the only hand-written part is `RealFourWriteSites.txt`, mapping each Fortran
  name to its C# write site, "not ported", or "not rounded"; and
  `tests/Particle.Tests/RealFourAccumulatorMappingTests.cs` guards the mapping both
  ways, proven red on a synthetic fixture and, once, on the real files. Measured the
  same day (`ScratchMovementProbe`, discarded, never committed): `Binary64` output is
  bit-identical on all five reference formulations; `Original` output moves only
  `Zkarm_cor`, by 1.3e-7 to 8.6e-7 relative, on three of the five.
  `tests/Simulation.Tests --filter FullyQualifiedName~StatisticalCriterionTests` stays
  green afterwards. Full narrative, the withdrawn intermediate readings and the
  benchmark figures: `HISTORY.md#generated-real4-membership-decision-2026-09-24`.

  `QKS1`, `FQKS`, `AUS` and `TU` are REAL*4 **decision** variables of the original: they
  gate the empty-window test (`AUS`, line 589), the `1.0 − FQKS(iks) < 1E-5` truncation
  (`QKS1`/`FQKS`, line 601) and the neighbour-loop exit (`TU`, line 716). The original's
  REAL*4 rounding can cross these thresholds one draw earlier or later than the port's
  `double` arithmetic, choosing a different `maxdk`/`NNN1` or pass count for the same
  input, without changing which formula runs.

  ⚠ 2026-09-17: was this exclusion list covering only the summed accumulators, now the
  four REAL*4 decision variables `QKS1`/`FQKS`/`AUS`/`TU` too → HISTORY.md#real4-decision-variables-added-to-the-exclusion-list

  Measured causally 2026-09-21 (a throwaway build emulating exactly `QKS1`/`FQKS`/`AUS`/
  `TU` at their own three lines, HPEPA3, `--precision double`, the 27-point `N` sweep
  `tests/Harness/HISTORY.md`'s "smooth-vs-step check" used, discarded and never
  committed): the emulating build reproduces only 3 % of the original's own drop in
  `pdoksmall[2]` and 3.6 % in `fmdok[1]`, all of it below `N = 10000`, and is flat from
  `N = 14500` to the shipped `N = 100000` exactly like the unmodified port — this
  mechanism does not produce the original's drift. Full method, controls and
  pre-registered thresholds: `HISTORY.md#decision-variable-gate-bias-refuted-2026-09-21`.
- `Dr`/`Db` (the drawn base/neighbour sizes, "base (448-484)"/"345 (503-543)") and
  `Dmin` have no declaration in the source's own declaration block (lines 4-64), so
  both are REAL*4 by Fortran's implicit-typing default, the same fallback rule
  `classify-real4-accumulators.py` already applies (checked the same way: no
  `IMPLICIT` statement narrows it anywhere in the source). Subroutine `SIZE`
  (1761-1776) declares its own output `D` (the caller's `Dr`/`Db`) the same way,
  implicit REAL*4, while its own `X1` is explicit `real*8`; Fortran promotes only the
  operations `X1` takes part in to double precision, so `D`'s right-hand side —
  `D=X1*(DOK(2*MINV)-DOK(2*MINV-1))+DOK(2*MINV-1)` (`JZ.EQ.2`, 1772) or
  `D=1./SQRT(1/DOK(2*MINV-1)**2-X1*(...))` (1769) — is evaluated in double precision
  and only the final store into `D` rounds to binary32. A draw whose true (double)
  size sits a hair above `Dmin` can round *down* to `Dmin`'s own binary32 value at
  that store, firing `Dr.le.Dmin`/`Db.le.Dmin` (478/533) where the port's `double`
  comparison, strictly above, does not — a mechanism the precision-kind design
  already scopes out (root BOOT.md, "Precision kind": the attempt plane stays
  `double` under either kind).

  Measured 2026-09-23 (`tests/Particle.Tests/DminBoundaryProbe.cs`,
  `DminBoundaryProbeTests.cs`): a real reference-mode replay observes the mechanism
  directly at seed 0 — 3 of 2,000,000 attempts on HMX (rate 3.72e-4), 4 of 1,200,000 on
  HPEPA3 (2.21e-5), 0 of 500,000 on P33, where the port's own `double` comparison never
  fires. Against the original's own replicas (mean rate 1.19e-3 HMX, 5.47e-5 HPEPA3),
  the exact Poisson 95 % intervals (7.7e-5–1.09e-3 HMX, 6.0e-6–5.7e-5 HPEPA3) contain
  HPEPA3's rate and fall just short of HMX's, so this mechanism may fully account for
  HPEPA3's count and is a partial, unestablished explanation for HMX's. It lies in the
  attempt plane, which stays `double` by this document's own decision, so it is
  declared, not reproduced. Full method, controls, red proofs and the remaining-factor
  hypothesis: `HISTORY.md#dmin-boundary-probe-2026-09-23`.
- The `X1 == 1.0` redraw of line 496 (`Attempt.Run`, inside `Label501`, "Attempt
  structure") is not counted by `NeighbourBudget` or any budget. `Mcg128.Next` returns
  exactly 1.0 with probability 2⁻⁵⁴ per draw ([Random](../Random/API.md), "Generator"),
  so the redraw is reachable, as it is in the original on line 496's own unbounded pass
  (not a `GO TO 11`, so the original does not count it towards anything either). Two
  such draws in a row are a 2⁻¹⁰⁸ event. Coupling this redraw to a budget would guard
  nothing a run will meet. The same 1.0 drawn as X3 is not redrawn and ends the run
  with `IndexOutOfRange` in `BridgeWindow.SamplePocket`, about 10⁻¹⁰ per run by the
  fidelity audit's estimate and not measured here.

  ⚠ 2026-09-24: was "`Mcg128.Next` is guaranteed to return a value in `[0, 1)`, so the
  redraw condition is unreachable", now the redraw is reachable at 2⁻⁵⁴ per draw and
  still needs no budget → HISTORY.md#x1-equals-one-reachability-corrected

  The two budgets
  this node does add guard the draws that can genuinely repeat many times (the neighbour
  loop itself and the bridge redraw of label 451).
- The budgets and `IndexOutOfRange` add endings the original does not have.
- `Xss5`, `Xss6` (never initialized, line 299 zeroes only XSS0–XSS4) and `NnTotal`
  (never initialized) start at 0, the value the static storage of the original held.
- The dead test of line 529 is omitted (root `## Constraints`).
- Accumulators that nothing printed or carried into a next cycle ever reads are not
  ported: `qdokstr` (line 543), `QDOK` (line 706, its print at 1403 is commented out),
  `ALLVDOK_FR` (lines 466, 521, its print at 1282 is commented out).
- **Normalization.** `PocketHistogram.Normalize`'s `NaN` (0/0, when `ΣQks = 0`) is
  stored as zero instead (lines 717–718): see its own XML doc for the mechanism and
  its consequence.

**Totals between cycles.** The totals are owned by the driver and persist over the
whole run. Between cycles `Statistics` rewrites some of them in place as the original
does, and later cycles add to the rewritten values: the category merge and shift of
lines 911–958 on `Qdoks`, `Dokp41`, `Dokp31` (the old last row is not cleared after
any merge), and the division of `VdokTotal` and `VdokTotal2` by `1 − gdokleft/GGG`
(lines 1145, 1150). The attempt neither knows nor cares.

No accumulator is reset at a cycle boundary beyond the two in-place rewrites just
named; this is itself a defect of the original the port reproduces, declared for
`## Defects of the original` (2026-09-20): a per-cycle quantity printed for cycle
`k ≥ 1` already carries every accepted particle of cycles `1..k`, not only cycle `k`'s
own, so the per-cycle convergence series `Simulation` prints
(`src/Simulation/BOOT.md`, "Result field mapping", `Convergence`) is a series of
increasingly-precise cumulative estimates, not of independent per-cycle ones.

**Fold.** `Fold.Add` adds the records of particles `0 … n−1` in that order into the real
totals: sums by addition, maxima by `Math.Max`. The same particle order gives the same
bits on any accelerator whose `double` addition is IEEE.

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells |
|---|---|---|---|---|---|
| 425–756, 723–738 | algorithm | An attempt later ended as `RestartedAfterLoop` by the acceptance tests of lines 723–738 keeps every histogram, bridge and pocket accumulator update it made before those tests fired: nothing is undone on rejection (`## Invariants`, "Every side effect of the original, in the same place"). | not measured | reproduced | none |
| 425–767 | algorithm | No accumulator is reset at a cycle boundary beyond the two in-place rewrites `Statistics` makes: a per-cycle quantity printed for cycle `k ≥ 1` already carries every accepted particle of cycles `1..k` ("Totals between cycles"). | not measured | reproduced | none |
| 496 | numeric | A draw the original repeats (`X1 == 1`, line 496) is redrawn from the same stream on an unbounded pass (not a `GO TO 11`, so uncounted); the original's REAL*4/x87 rounding could in principle hit `X1 == 1.0` exactly there ("Decisions where the port departs from a transcription"). | not measured | reproduced | none |
| 529 | dead | Tests `Dr ≥ Dmax` again inside the neighbour loop; the test cannot fire because line 474 already restarted the attempt whenever `Dr ≥ Dmax` ("Decisions where the port departs from a transcription"; `## Line map`). | not measured | declared, not reproduced | none |
| 466, 521, 541, 543, 701, 706, 710 | dead | `VDOKS` (710) feeds only `VDOKSO` and `DOKP432`, which nothing prints (1397 is commented out); `idok_local_all` (541) and `ipocket_loc_cor` (701) are never read; `qdokstr` (543) feeds nothing printed or carried forward; `QDOK` (706)'s only print (1403) and `ALLVDOK_FR` (466, 521)'s only print (1282) are commented out ("Decisions where the port departs from a transcription"). | not measured | declared, not reproduced | none |
| 469, 524, 542, 642–643, 648–649, 665–666, 685, 702, 707, 708, 720, 741–743, 747, 749–750, 753–754 | numeric | The original's REAL*4 sums (`ALLVDOK`, `vdokstr`, `DOKP41`, `fmkarm*`, `VSMKM`, `VSMKM1`, `SVD1`, `Vdok_loc`, `Vmkm_loc`, `Vdok_loc2`, `Vmkm_loc2`…) saturate on long runs ("Decisions where the port departs from a transcription"). | measured 2026-09-21 at the original's own one-rounding-per-drawn-particle granularity: worst-cell binary32-vs-double relative error reaches 65% (`Allvdok`/`Vdokstr`) on HMX, 46% on HPEPA3; `Dokp41`/`Dokp31` (feeding `dokkarm43`) exceed the whole replica band on HPEPA3/HMX (ratio 1.1-2.8) — REAL*4 accumulation alone could fully explain those formulations' failing `dokkarm43` cells; `Fmkarm2` rules the story out everywhere. Reproduced by `PrecisionKind.Original`, opt-in, `Binary64` the default (`## Accumulators`; `tests/Particle.Tests/PrecisionKindClassificationTests.cs`). | reproduced | none |
| 589, 601, 716 | numeric | `QKS1`, `FQKS`, `AUS` and `TU` are REAL*4 decision variables that gate a branch rather than print a value: `QKS1`/`FQKS` feed the `1.0 − FQKS(iks) < 1E-5` truncation of line 601, `AUS` is their reciprocal sum (line 589's empty-window test), `TU` gates the neighbour-loop exit of line 716; the original's REAL*4 rounding can cross a threshold one draw earlier or later than `double` arithmetic does ("Decisions where the port departs from a transcription"). | measured causally 2026-09-21: a throwaway binary32-emulating variant of exactly these four variables (discarded, never committed) run on HPEPA3 across the same 27-point `N` sweep `tests/Harness`'s own smooth-vs-step check used declines by only 3% (`pdoksmall[2]`) and 3.6% (`fmdok[1]`) of the original's own drop, all of it below `N = 10000`, and is exactly as flat as the unmodified port for `N ≥ 14500` — refuted as the mechanism behind these two cells' own drift ("Decisions where the port departs from a transcription"). | declared, not reproduced | none |
| 595 | algorithm | `DPOC(mindk)=Di*mindk` anchors the bridge-window's diameter CDF one cell (`Di`) above the QKS1 cell whose probability mass it pairs with, so `DM` draws every bridge pocket diameter one histogram cell too high (`## Attempt structure`, "Bridge window once per attempt"). | measured (fidelity audit 2026-09-24, U-P1; eight seeds `k·2¹⁷`, all five reference formulations, `Independent` layout, `--precision double`, a throwaway variant with the origin at the cell's lower edge, discarded and never committed): `Dqmkm2` moves −2.8% to −7.0% and `Zkarm` +4.9% to +13.5%, both far beyond the seeds' own spread; 75–95 of 1513–6643 compared cells differ per formulation; a cell-centre origin gives about half the shift on every formulation, a dose-proportional response; `Dqmkm1`/`qmkm1` and the bridge count are bit-identical (`src/Particle/HISTORY.md#bridge-window-origin-audit-2026-09-24`). | reproduced | none |
| 299 | dead | `Xss5`, `Xss6` (line 299 zeroes only `XSS0`–`XSS4`) and `NnTotal` are never initialized by the original ("Decisions where the port departs from a transcription"). | not measured | declared, not reproduced | none |
| 478, 533, 1761–1776 | numeric | `Dr`/`Db` (implicit REAL*4, no declaration) pass through subroutine `SIZE`'s own REAL*4 store before the `Dr.le.Dmin`/`Db.le.Dmin` test; a draw a hair above `Dmin` can round down to `Dmin`'s own binary32 value and fire the test ("Decisions where the port departs from a transcription"). | measured 2026-09-23: real reference-mode replay observes 3/2,000,000 attempts on HMX (rate 3.72e-4) and 4/1,200,000 on HPEPA3 (2.21e-5), 0/500,000 on P33; against the original's own lagged replicas (mean 1.19e-3 HMX, 5.47e-5 HPEPA3) this mechanism alone reaches the right order of magnitude; counts of 3 and 4 events, whose Poisson 95 % intervals are 7.7e-5–1.09e-3 for HMX and 6.0e-6–5.7e-5 for HPEPA3, so HPEPA3 is fully consistent and HMX narrowly short (`tests/Particle.Tests/DminBoundaryProbe.cs`, `DminBoundaryProbeTests.cs`). | declared, not reproduced | `HPEPA3,HMX: ConditionBreaking(2)` |

## Acceptance criteria

- [x] An attempt called on the host thread and the same attempt on the ILGPU CPU
      accelerator give bit-identical outcomes, streams, totals and records: 2026-09-17,
      `tests/Particle.Tests`, `HostEqualsCpuAcceleratorTests.RunMatchesOnHostAndTheCpuAccelerator`
      (constructed setups; the HPEPA3-scale version is `Execution`'s to add once it exists).
- [x] Bit snapshots of the first 1000 attempts of each reference formulation (outcomes,
      stream states, totals, records) guard refactorings: 2026-09-18,
      `tests/Particle.Tests`,
      `SnapshotTests.FirstThousandCycleZeroAttemptsMatchTheApprovedSnapshot` (one case
      per formulation; red on the `SpherePi` mutation its node records). Narrower than
      it reads: all 1000 are cycle-0 attempts under `Binary64`, so none builds the QKS1
      window, takes Var#1/Var#2 or the tests of lines 723–738, or rounds a binary32 sum.
- [x] The line map covers every executable line of Fortran 425–767, 1573–1586,
      1588–1629 and 1761–1776; the list of executable lines is generated from the
      source by a script, not typed: 2026-09-17, `tests/Particle.Tests`,
      `LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`.
- [x] Folding the same records in particle order gives bit-identical totals whatever
      order the particles ran in (records produced by permuted parallel runs):
      2026-09-17, `tests/Particle.Tests`, `FoldTests` (all four cases).
- [x] Each outcome of the table is produced by a test, the budgets and
      `IndexOutOfRange` by constructed inputs: 2026-09-17, `tests/Particle.Tests`,
      `AttemptOutcomeTests` (all six outcomes and both budgets) and
      `BridgeWindowTests.SamplePocketGuardsAnUnboundedSearch` (`IndexOutOfRange`'s own
      guard, unreachable through `Attempt.Run` under this node's preconditions, see
      the outcome table above).
- [x] The window built once per attempt equals the window rebuilt at every bridge, bit
      for bit, on constructed inputs: 2026-09-17, `tests/Particle.Tests`,
      `BridgeWindowTests.BuildIgnoresWhateverTheScratchHeldBefore`.
- [x] The same equality holds over the first 10⁵ attempts of HPEPA3 in cycle 1: the
      per-attempt hash sequences (outcome, streams, totals, record) of the unmodified
      code and of a scratch variant rebuilding the window at every bridge are
      identical: 2026-10-02, a measurement on c14b93f, nothing committed,
      `HISTORY.md#window-cycle1-measurement-2026-10-02`. Red when one cell of QKS1 is
      scaled before a later bridge (first differing attempt 2); right on two
      unmodified runs (identical). Reference mode, host threads, `Binary64`.

  ⚠ 2026-10-02: was "needs `Statistics.Setup.Prepare`", which `tests/Particle.Tests`
  has called since 2026-09-18 (`SnapshotTests`) while this stayed open; the cycle loop
  is what is missing. Found reconciling the open criteria. Later the same day: the
  cycle-1 state came from `tests/Execution.Tests`' `ReferenceFormulationDriver`.
- [x] Reference mode, in the `Original` layout at seed 0 and driven through `Execution`
      and `Simulation`, reproduces the counters of the reference `results.m` of HPEPA3
      within the count bound its test states: 2026-09-19,
      `tests/Simulation.Tests/Hpepa3CounterTests` (red on doubled denominators,
      `tests/Simulation.Tests/BOOT.md`, "## Mutations"). Agreement of every printed
      cell is the root's rate criterion, not this one.

  ⚠ 2026-10-02: was "Reference mode driven by a test over this node alone reproduces
  … within the statistical criterion (integration evidence before `Execution`
  exists)": `Execution` came first (2026-09-18), and one run within the criterion is
  no evidence (root `BOOT.md`, "the pass condition compares failure rates, not single
  runs"). Found reconciling the open criteria.
- [x] The `PrecisionKind.Original` rounding set is generated, never typed, and
      checked against the committed artefact by regeneration: 2026-09-21,
      `tests/Particle.Tests`, `RealFourClassificationGeneratorTests.
      ClassificationReproducesByteForByteOnRegeneration` (`classify-real4-
      accumulators.py verify`); the generator itself is proven non-degenerate by
      mutating the `real*8,allocatable :: VKS(:)` declaration to `real` in a scratch
      copy of the source and observing `Vks` flip from excluded to rounded (this
      session's own scratch, reproducing the fifteen/twelve split already recorded
      under "## Decisions where the port departs from a transcription").
- [x] `Attempt.Run`'s own actual rounding, observed by running the identical attempt
      sequence under both kinds, equals the generated classification for every
      sum-kind field (none untouched): 2026-09-21,
      `tests/Particle.Tests`, `PrecisionKindClassificationTests.
      RoundingSetMatchesTheGeneratedClassification`; `UnsetKindBehavesAsBinary64` for the
      default. Proven non-degenerate in both directions, each mutation reverted after:
      changing `Vsmkm`'s write site from `AddReal4` to a plain `+=` turned the check red
      naming `Vsmkm` ("classified 'yes' ... bit-identical"); changing `Vks`'s plain `+=`
      to `AddReal4` turned it red naming `Vks` ("classified 'no' ... disagreed").
- [x] `PrecisionKind.Binary64` costs no measurable throughput: 2026-09-21,
      `tests/Benchmarks`' own route, HPEPA3, Release, before this change and after (the
      figures and their spread: `HISTORY.md#generated-real4-membership-decision-2026-09-24`)
      — both differences fall inside the other run's own spread.
- [ ] Row 478, 533, 1761–1776 reaches beyond HPEPA3 and HMX: a scratch measurement of
      2026-10-03, not committed, saw the same one-event `ConditionBreaking(2)`
      difference on the PSAN01 path-identical pair (C2's undeclared
      `PSAN01 ConditionBreaking(2)[0]`, class P by `src/Statistics/ACCEPTANCE.md` A3's
      design attribution) and on P33 against its independent replicas (original 3.1e-6,
      port 0). Closed by that measurement committed with its place; the row's
      `Differing cells` widens only where `CompareSets` finds the cell, an owner's
      decision, since `SetCriterionTests` gates every declared entry.

## Taboos

- No second copy of any branch of the attempt for a special mode.
- No refresh of QKS1, `pdoksmall` or `Dmaxxx` inside the attempt.
- No `double` written to shared memory; no integer histogram in the record.
- No "fix" of a defect of the original beyond the declared list.
