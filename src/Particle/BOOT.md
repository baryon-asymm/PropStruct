# BOOT.md — Particle

## Purpose

The particle program: one attempt at a base particle with its surroundings, as the
original's main loop performs it (Fortran lines 425–767, with the subroutines `SIZE`,
`DM` and `VM`), in kernel-compatible C#. The node also owns the kernel form of the
model setup, the cycle inputs, the accumulator layout (shared integer totals and a
per-particle real-valued record), the normalization of the pocket histogram, and the
fixed-order fold of records into totals: the layout is defined by which side effect
happens where in the attempt, so no node that does not know the attempt can own it.
It is the one place in the tree where the model's per-particle physics lives. Under the
`Corrected` model (root `BOOT.md`, "Model kind") it is the same program with the
explicit errors of its own lines fixed, each by a flag of the internal `CorrectionSet`
this node owns (`## Corrected model`).

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
  (2026-09-20): an attempt later ended as `RestartedAfterLoop` by the
  acceptance tests of lines 723–738 keeps every histogram, bridge and pocket
  contribution it made before those tests fired — nothing is undone on rejection,
  under every model, `Corrected` included.

  ⚠ 2026-10-18: was every update in place and nothing undone under every model, now line 720's write moved under `Fmkarm2OnAcceptance` → HISTORY.md#side-effects-invariant-corrected-2026-10-18

  ⚠ 2026-10-18: was line 720's write moved under `Fmkarm2OnAcceptance` (the staged sum not kept), now nothing undone under every model, the flag withdrawn → HISTORY.md#fmkarm2-on-acceptance-withdrawn-2026-10-18
- **`None` is the original.** With `ModelSetup.Corrections` unset (`None`) the node
  computes bit for bit what it computed before the field existed. A flag changes only
  the statements its entry under `## Corrected model` names; the stream of each draw
  and the point of the program it is made at stay the original's, though a changed
  value may change which draws follow.
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

- Literal constants as written under the `Original` model, each a named constant with
  its line: `3.14159` in `π/6·D³` (lines 466, 469, 484, 521, 524, 542, 643, 648, 665,
  677, 685, 702, 710, 747, 750, 754) and in `rrL` (line 501), `12.56636` (lines 430,
  716) and `/200` (line 716), `3.14` in `VM` (lines 1616, 1624–1626), `1e-5` (line
  601), `1/3.` (line 501) taken as the `double` `1.0 / 3.0`. Under the `Corrected`
  model `3.14159` is π (`ExactConstants`) and lines 1616–1627 are replaced whole
  (`BridgeFrustum`); the others stay as written under both (`## Corrected model`).

  ⚠ 2026-10-18: was every literal as written under every model, now `3.14159` and `VM`'s lines exact under `Corrected` → HISTORY.md#literal-constants-corrected-2026-10-18
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

⚠ 2026-09-26: the §15 deviation this document used to declare here is withdrawn: the §6 form correction above (the deviation was already true but was not written in the form the linter reads) excludes `## Line map` and `## Defects of the original` from the count, and the moves recorded through this document's ⚠ pointers bring the rest inside the 400-line leaf limit → HISTORY.md#section-15-deviation-withdrawn-2026-09-26

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

⚠ 2026-09-25: was this rule read as a defect of the original (declared for `## Defects of the original`, 2026-09-20) on evidence (`TotalAttempts`/`Nfx`/`IbridgeTotal` bit-identical across batch configurations) that never bore on the question — those three totals are decided by streams 1–5 before any bridge window is touched, so they are independent of QKS1's content by construction (fidelity audit 2026-09-24, U-P2) — now read as a model feature, below → HISTORY.md#qks1-refresh-defect-withdrawn

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

Measured 2026-09-25: 0 of 15,036 compared cells differ (eight seeds, the five
references, largest `|t|` 1.08), the variant live at 2,127 of 117,054 cells of a
same-seed diff → `HISTORY.md#measurements-condensed-2026-10-18`.

## Line map

This table's declared scope (`## Purpose`; `ACCEPTANCE.md`, "The line map
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
| 582–611 | `BridgeWindow.Build` | built at the first bridge of an attempt and reused, see below; line 595's origin under `WindowLowerEdge` (`## Corrected model`) |
| 612–625, 1573–1586 | `BridgeWindow.SamplePocket` | `DM`, with index guard |
| 1588–1629 | `BridgeGeometry.Volume` | `VM`; `error` (line 1611) not ported; argument swap on copies; 1616–1627 replaced whole under `BridgeFrustum` (`## Corrected model`) |
| 626–667 | `Attempt.Run` (`Label501`, the rest of the bridge block) | |
| 671–703 | `Attempt.Run` (`Label502`) | `RK ≤ 0` (line 674) cannot happen under `AK3 > 0`; kept; `ipocket_loc_cor` (701, never read) not ported |
| 704–711 | `Attempt.Run` (`Label333`) | `VDOKS` not accumulated, see accumulators |
| 714–716 | `Attempt.Run` (`Label550`) | |
| 717–718 | `PocketHistogram.Normalize` | called by the driver |
| 719–756 | `Attempt.Run` (`Label550`, the post-loop acceptance tests and commit) | line 720's write before 722's division, the original's commit point under both models (`## Corrected model`, "Line 720") |
| 758–766 | — | console progress, not ported |
| 452–458, 490–494, 507–513, 615–619, 655–659, 691–695 | — | GSV ≠ 2 branches, not ported (root: GSV=2 only) |

**Bridge window once per attempt.** Lines 582–610 depend only on `Dr`, `Di`, `AK3`,
`AK4` and QKS1, none of which changes within an attempt, so the window (`FQKS`,
`DPOC`, `NNN1`) is built at the first bridge of a cycle ≥ 1 attempt and reused by later
bridges of the same attempt. The empty-window restart (line 590) therefore fires at
the first bridge, after the side effects that precede it, exactly as in the original.
Under the `Original` model the window reproduces the original literally: `DPOC(mindk)
= Di·mindk` (the upper bound of the cell, as written), the truncation at `1 − FQKS <
1e-5` (lines 600–606), the shift to index 1 (lines 607–610); under `WindowLowerEdge`
only the origin moves, to the cell's lower edge (`## Corrected model`).

⚠ 2026-10-18: was the window literal under every model, now its origin at the cell's lower edge under `WindowLowerEdge` → HISTORY.md#window-origin-corrected-2026-10-18

`DPOC(mindk) = Di·mindk` is an off-by-one, not an intended upper-bound or centre
convention (fidelity audit 2026-09-24, U-P1): `mindk` uses the source's own
`iks = int(RK/Di)+1` convention (line 675) throughout, while `DPOC`'s own later
consumer, `Dkarm43_cor`/`Dqkarm_cor` (lines 1085–1091), anchors a cell at its *centre*,
never its upper edge. Measured: `Dqmkm2` moves −2.8 % to −7.0 % and `Zkarm` +4.9 % to
+13.5 %, both far beyond the seeds' own spread; full method, controls and table:
`HISTORY.md#bridge-window-origin-audit-2026-09-24`; the defect row is in `##
Defects of the original` below. The `Corrected` model fixes it (`WindowLowerEdge`).

## Accumulators

Sizes from the setup: `NMM` fractions; `Ndok = int(Ddokmax/Di)+2`,
`Nkarm = int(Ddokmax·AK4/Di)+2`, `Ncat = int(Ddokmax·AK4/Dj)+2` (lines 274–276,
computed by `Statistics.Prepare` from the binary32 values the original holds, see
`src/Statistics/BOOT.md`), `Nc = 1000`. Indices are the Fortran indices minus one.

⚠ 2026-09-17: was sizes computed in `double`, now computed with integer mantissa arithmetic on the binary32 values → HISTORY.md#accumulator-sizes-double-vs-binary32

The field layout of both buffers — name, length, stride and cell meaning — is
`API.md`'s contract ("Accumulator layout"), not repeated here. What follows is which
Fortran line writes each field and why it sits in the buffer it does — the attempt's
own reasoning, needed to implement `Attempt.Run`, not to call it.

⚠ 2026-09-17: was this document itself stating the field layout (name, length, stride, cell meaning) inline, now moved out to `API.md`'s own contract → HISTORY.md#field-layout-moved-to-api-md

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
only if it counted a Var#2 bridge (line 752). `Fmkarm2` is not among them: line 720
adds `fmkarm2_loc` to it before the tests of 723–738, under every model. Every other
update happens where the original makes it.

⚠ 2026-10-18: was `Fmkarm2` added before the tests under every model, now committed on acceptance under `Fmkarm2OnAcceptance` → HISTORY.md#fmkarm2-commit-corrected-2026-10-18

⚠ 2026-10-18: was `Fmkarm2` committed on acceptance under `Fmkarm2OnAcceptance`, now added at line 720 under every model, the flag withdrawn → HISTORY.md#fmkarm2-on-acceptance-withdrawn-2026-10-18

## Decisions where the port departs from a transcription

Declared, AGENTS.md §12.

- `VDOKS` (line 710) is not accumulated: it feeds only `VDOKSO` and `DOKP432`, which
  nothing prints (Fortran line 1397 is commented out); see `src/Statistics/BOOT.md`.

  ⚠ 2026-09-17: was `Statistics` deriving `VDOKS` as `Qdoks × constant`, now not derived at all → HISTORY.md#vdoks-not-derived
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
  set is generated too, not only its kind.** The script scans this node's own `## Line
  map` ranges directly, keyed by Fortran name; the only hand-written part is
  `RealFourWriteSites.txt` (each name to its C# write site, "not ported" or "not
  rounded"), guarded both ways by `RealFourAccumulatorMappingTests`. A typed list had
  missed six REAL*4 sums at lines 642–666 (fidelity audit of 2026-09-24, P-1);
  `Binary64` stays bit-identical, `Original` moves `Zkarm_cor` alone (≤ 8.6e-7 relative)
  → `HISTORY.md#measurements-condensed-2026-10-18`, `HISTORY.md#real4-membership-design-2026-10-18`

  `QKS1`, `FQKS`, `AUS` and `TU` are REAL*4 **decision** variables of the original: they
  gate the empty-window test (`AUS`, line 589), the `1.0 − FQKS(iks) < 1E-5` truncation
  (`QKS1`/`FQKS`, line 601) and the neighbour-loop exit (`TU`, line 716). The original's
  REAL*4 rounding can cross these thresholds one draw earlier or later than the port's
  `double` arithmetic, choosing a different `maxdk`/`NNN1` or pass count for the same
  input, without changing which formula runs.

  ⚠ 2026-09-17: was this exclusion list covering only the summed accumulators, now the four REAL*4 decision variables `QKS1`/`FQKS`/`AUS`/`TU` too → HISTORY.md#real4-decision-variables-added-to-the-exclusion-list

  Measured causally 2026-09-21 (a throwaway emulation of the four, HPEPA3): 3 % of the
  original's drop in `pdoksmall[2]`, flat from `N = 14500` — not the mechanism of the
  drift → `HISTORY.md#measurements-condensed-2026-10-18`.
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

  Measured 2026-09-23 (`tests/Particle.Tests/DminBoundaryProbe.cs`): seen at seed 0 on
  HMX (3 of 2,000,000 attempts) and HPEPA3 (4 of 1,200,000), enough for HPEPA3's count
  and part of HMX's; attempt plane, so declared, not reproduced →
  `HISTORY.md#measurements-condensed-2026-10-18`.
- The `X1 == 1.0` redraw of line 496 (`Attempt.Run`, inside `Label501`, "Attempt
  structure") is not counted by `NeighbourBudget` or any budget. `Mcg128.Next` returns
  exactly 1.0 with probability 2⁻⁵⁴ per draw ([Random](../Random/API.md), "Generator"),
  so the redraw is reachable, as it is in the original on line 496's own unbounded pass
  (not a `GO TO 11`, so the original does not count it towards anything either). Two
  such draws in a row are a 2⁻¹⁰⁸ event. Coupling this redraw to a budget would guard
  nothing a run will meet. The same 1.0 drawn as X3 is not redrawn and ends the run
  with `IndexOutOfRange` in `BridgeWindow.SamplePocket`, about 10⁻¹⁰ per run by the
  fidelity audit's estimate and not measured here.

  ⚠ 2026-09-24: was "`Mcg128.Next` is guaranteed to return a value in `[0, 1)`, so the redraw condition is unreachable", now the redraw is reachable at 2⁻⁵⁴ per draw and still needs no budget → HISTORY.md#x1-equals-one-reachability-corrected

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
whole run. Under the `Original` model `Statistics` rewrites some of them in place
between cycles as the original does, and later cycles add to the rewritten values: the
category merge and shift of lines 911–958 on `Qdoks`, `Dokp41`, `Dokp31` (the old last
row is not cleared after any merge), and the division of `VdokTotal` and `VdokTotal2`
by `1 − gdokleft/GGG` (lines 1145, 1150). Under `CategoriesOnCopy` and
`VdokTotalOnCopy` it works on copies and writes no total (`src/Statistics/BOOT.md`,
"## Corrected model"). The attempt neither knows nor cares.

⚠ 2026-10-18: was the in-place rewrites of 911–958, 1145 and 1150 under every model, now under `Original` only → HISTORY.md#totals-between-cycles-corrected-2026-10-18

No accumulator is reset at a cycle boundary beyond the in-place rewrites just named,
under either model (not an explicit error, so `Corrected` keeps it, `## Corrected
model`); this is itself a defect of the original the port reproduces, declared for
`## Defects of the original` (2026-09-20): a per-cycle quantity printed for cycle
`k ≥ 1` already carries every accepted particle of cycles `1..k`, not only cycle `k`'s
own, so the per-cycle convergence series `Simulation` prints
(`src/Simulation/BOOT.md`, "Result field mapping", `Convergence`) is a series of
increasingly-precise cumulative estimates, not of independent per-cycle ones.

**Fold.** `Fold.Add` adds the records of particles `0 … n−1` in that order into the real
totals: sums by addition, maxima by `Math.Max`. The same particle order gives the same
bits on any accelerator whose `double` addition is IEEE.

## Corrected model

Root `BOOT.md`, "Model kind". This node owns the internal `[Flags] enum CorrectionSet :
uint`, its members and their explicit values (API.md, "## Corrected model ✅", the
contract; never renumbered). The `Corrected` model is `All`; a subset exists
in-process only, for measurement. This node reads the three members whose entries
follow; the others fix `Statistics`' lines and are read there. A member is named in
its defect rows' `Corrected` column as `fixed (<Member>)`.

- `ModelSetup.Corrections` (unset = `None`), filled by `Statistics.Setup.Prepare`, is
  read once at the top of `Attempt.Run`, beside `roundToReal4`, into three `bool`
  locals by bitwise `&` (never `Enum.HasFlag`, a boxing call no kernel compiles).
- `BridgeWindow.Build` and `BridgeGeometry.Volume` receive their flag as an explicit
  parameter; the present signatures stay as overloads passing `false`:
  `Build(double dr, double ak3, double ak4, double cellSize, int nkarm, bool
  windowLowerEdge, ArrayView<double> qks1, ArrayView<double> fqks, ArrayView<double>
  dpoc, out int nnn1)`; `Volume(double r1, double r2, double rk, double a, bool
  bridgeFrustum, out double volume, out double bb)`.
- The node does not inspect the combination with `PrecisionKind.Original`: `Simulation`
  refuses it (root, "Model kind"), as it refuses `Original` accumulation in batched
  mode. Every `AddReal4` site keeps its kind argument under either model. Record,
  scratch, fold and integer totals keep their layout; `ModelSetup` gains one field.

**`ExactConstants` — π in the attempt.** Fortran 469, 484, 501, 524, 542, 643, 648,
665, 677, 685, 702, 747, 750, 754 write `3.14159` for π (466, 521, 710 are not ported).
Under the flag it is π, by the root's rule ("Fidelity to the original"): a literal
approximating π or 1/3 is that constant; thresholds and physical parameters stay.
Two constants are chosen once at the top of `Attempt.Run`, each operand a compile-time
constant, so the `Original` arm is the folded value the code uses now:
`exactConstants ? Math.PI / 6.0 : SpherePi / 6.0` takes the place of `SpherePi / 6.0`
in every `π/6·D³`, `exactConstants ? 4.0 * Math.PI : 4.0 * SpherePi` that of
`4.0 * SpherePi` in `rrL`; every statement keeps its shape and order of operations.
Unchanged under both: `1/3.` of line 501 (already `1.0 / 3.0`, the constant itself),
`12.56636` and `/200` of lines 430 and 716 (the solid-angle closure, a modelling
choice), `1e-5` of line 601 (a threshold), and `VM`'s `3.14`, which is
`BridgeFrustum`'s whole. The site list is generated (`ACCEPTANCE.md`). Effect,
computed: every sphere volume ×π/3.14159 (+8.45e−7 relative); the Poisson part of
`rrL` ×(3.14159/π)^(1/3) (−2.8e−7), which can move line 716's exit by a neighbour and
with it the draws that follow; λ's change and `da_coef` are `Statistics`'.

**`BridgeFrustum` — the bridge volume, lines 1616–1627.** The original takes
`BE = 3.14 − AL − DE`, `GA = AL + BE/2` and the difference of two cones through
`tan GA` minus the caps `V1`, `V2`, all with `3.14`. With π that difference is the
frustum, but its two cone heights diverge as `R2 → R1`, and `3.14` shifts `GA` by
8.0e−4 (`tan GA` ≈ 1256 at `R2 = R1`): at `r1 = 50 µm`, `rk = 40 µm`, `a = 5 µm` the
volume is 69 times short at `R2/R1 = 0.998` and negative at 1 (design review of
2026-10-18). Even with π the heights cancel at `R2 = R1`, hence the frustum's formula.
Under the flag, the frustum between the two chord circles less the two caps, with
`Math.PI` whatever `ExactConstants` says: `l = ac - q1 - q2`, `h1 = r1 - q1`,
`h2 = r2 - q2`, `r1A = r1 * Math.Sin(al)`, `r2A = r2 * Math.Sin(de)`, and
`volume = Math.PI / 3.0 * l * (r1A * r1A + r1A * r2A + r2A * r2A) - Math.PI * h1 * h1
* (r1 - h1 / 3.0) - Math.PI * h2 * h2 * (r2 - h2 / 3.0)`, in that order of operations.
It replaces everything after the `bb < 0` test; the swap, both `JJ = 0`
returns, `cosA`, `cosD`, `q1`, `q2`, `al`, `de` and `bb` are shared, so `JJ`, `BB`,
the draws and every integer total stay bit-identical. Effect: `volume` alone, into
`Vsmkm`, `VmkmTotal`, `VmkmTotal2` through `VSMKM1`, `Vmkm_loc`, `Vmkm_loc2`
(`Zkarm`, `Zkarm_cor`, `epszkarm_n`).

Declared, not an error of the fix: the solid is the program's straight frustum between
the contact circles, not the published bridge bounded by the notional pocket surfaces
(concave side), so the fix moves the volume further from the published one (0.53 to
0.64 of it by the audit's integration). Under this flag alone P33's `Zkarm_cor` crosses
zero (the row 1616–1627 holds the figures): nothing bounds `1 − DolM2`, no clamp.

**Line 720 — withdrawn 2026-10-18** (the flag `Fmkarm2OnAcceptance`, value 4, is
retired and never reused). The original adds the staged `fmkarm2_loc` to `fmkarm2`
before the tests of 723–738, as every other pocket statistic counts rejected attempts
too; the `Corrected` model keeps that commit point, since staging it like the bridge
sums (741–755) is no explicit error and only explicit errors are fixed. The deciding
figure: where `pdoksmall` is 0 (inpt, PSAN02n) `Dkarm43_cor(1)` and `(2)` agree under
`Original` (214.81 µm both at seed 0), and under `All` `(2)` went to +14.7 % and
+16.1 %. The audit's paired shifts: `HISTORY.md#fmkarm2-audit-2026-10-18`; the full
paragraph: `HISTORY.md#real4-membership-design-2026-10-18`.

⚠ 2026-10-18: was the row of line 720 `fixed (Fmkarm2OnAcceptance)` with the effect measured by `tests/Simulation.Tests/Measurements/corrections-2026-10-18.md`, now `unchanged` with the audit's paired shifts, the flag withdrawn → HISTORY.md#fmkarm2-on-acceptance-withdrawn-2026-10-18

**`WindowLowerEdge` — line 595.** The original sets `DPOC(mindk) = Di·mindk`, one cell
above the QKS1 cell whose mass the window's first interval carries. Under the flag
`dpoc[mindk − 1] = cellSize * (mindk − 1)`, the cell's lower edge, the one knot at
which `DM`'s linear interpolation lands a draw in the cell whose mass produced it (cells
cover `[(iks − 1)·Di, iks·Di)`, line 675); the `Original` arm is today's
`cellSize * mindk`, and 582–594 and 596–611 are unchanged. Effect: every bridge pocket
a cell smaller, hence `BB`, `qmkm2`, `VMKM` and the `JJ = 0` redraws (`NFQ`, `XSS5`,
stream 6) — measured by the throwaway variant of the 2026-09-24 audit (eight seeds,
the five references): `Dqmkm2` −2.8 % to −7.0 %, `Zkarm` +4.9 % to +13.5 %
(`HISTORY.md#bridge-window-origin-audit-2026-09-24`). Risk: with the upper-edge origin
a pocket is never smaller than `Di·mindk > AK3·Dr`, so a bridge with `Dr ≥ Db` never
redraws on `A ≥ 2·RK`; the lower edge admits pockets below `AK3·Dr` down to
`Di·(mindk − 1)`, the cell resolution of a whole-cell window, the histogram-edge
class of the audit's E6b, declared, not an error of the fix; and a bridge past
`PocketRedrawBudget` (12,800, `Simulation`'s internal default, no flag) fails the run.
The coding task probed the rate (`ACCEPTANCE.md`, 2026-10-18): the largest count per
bridge was 109 draws (P33, 2.4·10⁵ bridges), 11.7 times under a tenth of the budget,
so no proposal to `Simulation` was needed.

Unchanged under `Corrected`, none an explicit error: rejected attempts keep their side
effects, line 720's staged sum included; no reset between cycles; the QKS1 refresh
cadence (a model feature, `## Attempt structure`); the dead line 529; the `max ≤ Dmaxxx`
edge of line 646; the shared stream 6 and the seeds (`Random`'s). The REAL*4 and x87
rows are `not reachable`: `Corrected` runs under `Binary64` only; the `3.14159` row,
`numeric` too, is `ExactConstants`'.

⚠ 2026-10-18: was "sixteen seeds" and the old page's shifts (`k·2¹⁷`, eight distinct phases; `Zkarm` −3.406 ± 0.323 % … −2.722 ± 0.199 %), now sixteen distinct seeds `k·2¹⁶` with each shift's standard error in the rows 1616–1627, 469… and 595 → HISTORY.md#corrections-page-rerun-2026-10-18

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells | Corrected |
|---|---|---|---|---|---|---|
| 425–719, 721–756 | algorithm | An attempt later ended as `RestartedAfterLoop` by the acceptance tests of lines 723–738 keeps every histogram, bridge and pocket accumulator update it made before those tests fired: nothing is undone on rejection (`## Invariants`, "Every side effect of the original, in the same place"). The staged `fmkarm2_loc` sum of line 720 is its own row. | not measured | reproduced | none | unchanged |
| 720 | algorithm | Line 720 adds the attempt's staged `fmkarm2_loc` to `fmkarm2` after the neighbour loop but before the acceptance tests of 723–738, while the bridge sums are committed after them (741–755): a rejected attempt's Var#2 pocket volumes enter `fmkarm2`. Every pocket statistic does the same (`fmkarm_cor` 685–686, QKS, VKS, DP31/41 676–679, QDOKS 711), so the commit point is not an explicit error (`## Corrected model`, "Line 720"). | measured 2026-10-18 by the four-auditor audit at c32821e (eight seeds `k·2¹⁷`, batched CPU, `Independent`, `Binary64`, `All` with the withdrawn flag against `Original`; paired shifts of `Dkarm43_cor(2)`/`Dagg43_cor(2)`): inpt +14.6 %, PSAN02n +16.0 %, HMX +2.0…+2.2 %, P33 −3.3…−3.5 %, HPEPA3 −9.5…−11.2 %; the fix was withdrawn, the original's commit point stands (`src/Particle/HISTORY.md#fmkarm2-audit-2026-10-18`) | reproduced | none | unchanged |
| 1616–1627 | algorithm | `VM` computes the bridge volume as a difference of two cones through `tan GA` with `BE = 3.14 − AL − DE` and `3.14` for π; with π the difference is the frustum, but near `R2 = R1`, where both cone heights diverge, the truncated angle collapses the volume, and at `R2 = R1` it turns negative (`## Corrected model`, "`BridgeFrustum`"). | measured 2026-10-18 (`tests/Simulation.Tests/Measurements/corrections-2026-10-18.md`, the re-run at 8b29342; batched, CPU, `Independent`, `Binary64`, sixteen distinct seeds `k·2¹⁶` per reference formulation; shift in per cent of the `Original` mean ± the standard error of the shift, each past the Welch threshold at 10⁻³ for that quantity alone): the fix alone moves `Zkarm` by −3.400 ± 0.319 % (HPEPA3), −12.434 ± 0.076 % (inpt), −7.037 ± 0.615 % (P33), −12.828 ± 0.058 % (PSAN02n), −2.732 ± 0.192 % (HMX) and `Dqmkm2` not at all; on P33 it carries `Zkarm_cor` through zero, −1174.9 ± 12.6 % of `Original`'s 5.81e-3 (the arm's mean −0.062, all sixteen seeds negative, `raw-P33.csv`), which no clamp bounds (`## Corrected model`, "`BridgeFrustum`") | reproduced | none | fixed (BridgeFrustum) |
| 469, 484, 501, 524, 542, 643, 648, 665, 677, 685, 702, 747, 750, 754 | numeric | The sphere volumes `π/6·D³` and the Poisson radius `rrL` take the truncated literal `3.14159` for π (`## Constraints`; `## Corrected model`, "`ExactConstants`"). | measured 2026-10-18 (`tests/Simulation.Tests/Measurements/corrections-2026-10-18.md`, the re-run at 8b29342; batched, CPU, `Independent`, `Binary64`, sixteen distinct seeds `k·2¹⁶` per reference formulation; shift in per cent of the `Original` mean ± the standard error of the shift): with Statistics' sites, `da_coef` −0.0086 % (HMX) to −0.0100 % (HPEPA3), a seed-independent setup shift whose standard error is at rounding size (3e-15 to 8e-15 points); `Dkarm43` +0.048 ± 0.017 % (PSAN02n) to +0.091 ± 1.324 % (P33), `Dkarm10` +0.027 ± 0.027 % (HMX) to +0.053 ± 0.041 % (P33), `Dagg43` +0.040 ± 0.043 % (inpt) to +0.082 ± 1.324 % (P33), inside the Welch threshold at 10⁻³ in all fifteen but `Dkarm10` on PSAN02n (t +4.0) and seen by the same-seed paired t, +4.6 to +49.0 | reproduced | none | fixed (ExactConstants) |
| 425–767 | algorithm | No accumulator is reset at a cycle boundary beyond the two in-place rewrites `Statistics` makes: a per-cycle quantity printed for cycle `k ≥ 1` already carries every accepted particle of cycles `1..k` ("Totals between cycles"). | not measured | reproduced | none | unchanged |
| 496 | numeric | A draw the original repeats (`X1 == 1`, line 496) is redrawn from the same stream on an unbounded pass (not a `GO TO 11`, so uncounted); the original's REAL*4/x87 rounding could in principle hit `X1 == 1.0` exactly there ("Decisions where the port departs from a transcription"). | not measured | reproduced | none | not reachable |
| 529 | dead | Tests `Dr ≥ Dmax` again inside the neighbour loop; the test cannot fire because line 474 already restarted the attempt whenever `Dr ≥ Dmax` ("Decisions where the port departs from a transcription"; `## Line map`). | not measured | declared, not reproduced | none | unchanged |
| 466, 521, 541, 543, 701, 706, 710 | dead | `VDOKS` (710) feeds only `VDOKSO` and `DOKP432`, which nothing prints (1397 is commented out); `idok_local_all` (541) and `ipocket_loc_cor` (701) are never read; `qdokstr` (543) feeds nothing printed or carried forward; `QDOK` (706)'s only print (1403) and `ALLVDOK_FR` (466, 521)'s only print (1282) are commented out ("Decisions where the port departs from a transcription"). | not measured | declared, not reproduced | none | unchanged |
| 469, 524, 542, 642–643, 648–649, 665–666, 685, 702, 707, 708, 720, 741–743, 747, 749–750, 753–754 | numeric | The original's REAL*4 sums (`ALLVDOK`, `vdokstr`, `DOKP41`, `fmkarm*`, `VSMKM`, `VSMKM1`, `SVD1`, `Vdok_loc`, `Vmkm_loc`, `Vdok_loc2`, `Vmkm_loc2`…) saturate on long runs ("Decisions where the port departs from a transcription"). | measured 2026-09-21 at the original's own one-rounding-per-drawn-particle granularity: worst-cell binary32-vs-double relative error reaches 65% (`Allvdok`/`Vdokstr`) on HMX, 46% on HPEPA3; `Dokp41`/`Dokp31` (feeding `dokkarm43`) exceed the whole replica band on HPEPA3/HMX (ratio 1.1-2.8) — REAL*4 accumulation alone could fully explain those formulations' failing `dokkarm43` cells; `Fmkarm2` rules the story out everywhere. Reproduced by `PrecisionKind.Original`, opt-in, `Binary64` the default (`## Accumulators`; `tests/Particle.Tests/PrecisionKindClassificationTests.cs`). | reproduced | none | not reachable |
| 589, 601, 716 | numeric | `QKS1`, `FQKS`, `AUS` and `TU` are REAL*4 decision variables that gate a branch rather than print a value: `QKS1`/`FQKS` feed the `1.0 − FQKS(iks) < 1E-5` truncation of line 601, `AUS` is their reciprocal sum (line 589's empty-window test), `TU` gates the neighbour-loop exit of line 716; the original's REAL*4 rounding can cross a threshold one draw earlier or later than `double` arithmetic does ("Decisions where the port departs from a transcription"). | measured causally 2026-09-21: a throwaway binary32-emulating variant of exactly these four variables (discarded, never committed) run on HPEPA3 across the same 27-point `N` sweep `tests/Harness`'s own smooth-vs-step check used declines by only 3% (`pdoksmall[2]`) and 3.6% (`fmdok[1]`) of the original's own drop, all of it below `N = 10000`, and is exactly as flat as the unmodified port for `N ≥ 14500` — refuted as the mechanism behind these two cells' own drift ("Decisions where the port departs from a transcription"). | declared, not reproduced | none | unchanged |
| 595 | algorithm | `DPOC(mindk)=Di*mindk` anchors the bridge-window's diameter CDF one cell (`Di`) above the QKS1 cell whose probability mass it pairs with, so `DM` draws every bridge pocket diameter one histogram cell too high (`## Attempt structure`, "Bridge window once per attempt"). | measured (fidelity audit 2026-09-24, U-P1; eight seeds `k·2¹⁷`, all five reference formulations, `Independent` layout, `--precision double`, a throwaway variant with the origin at the cell's lower edge, discarded and never committed): `Dqmkm2` moves −2.8% to −7.0% and `Zkarm` +4.9% to +13.5%, both far beyond the seeds' own spread; 75–95 of 1513–6643 compared cells differ per formulation; a cell-centre origin gives about half the shift on every formulation, a dose-proportional response; `Dqmkm1`/`qmkm1` and the bridge count are bit-identical (`src/Particle/HISTORY.md#bridge-window-origin-audit-2026-09-24`); measured again on the shipped flag 2026-10-18 (`tests/Simulation.Tests/Measurements/corrections-2026-10-18.md`, the re-run at 8b29342; batched, CPU, `Independent`, `Binary64`, sixteen distinct seeds `k·2¹⁶` per reference formulation; shift in per cent of the `Original` mean ± the standard error of the shift): the flag alone moves `Dqmkm2` by −6.953 ± 0.006 % (HPEPA3), −2.805 ± 0.011 % (inpt), −4.376 ± 0.022 % (P33), −3.645 ± 0.006 % (PSAN02n), −4.825 ± 0.013 % (HMX) and `Zkarm` by +11.844 ± 0.314 %, +7.128 ± 0.078 %, +11.552 ± 0.606 %, +13.402 ± 0.054 %, +4.860 ± 0.189 % in that order, each past the Welch threshold at 10⁻³; 87 to 98 of 1518 to 7624 compared cells differ per formulation, the floor 0 of 1506 to 7139 (`## Corrected model`, "`WindowLowerEdge`"). | reproduced | none | fixed (WindowLowerEdge) |
| 299 | dead | `Xss5`, `Xss6` (line 299 zeroes only `XSS0`–`XSS4`) and `NnTotal` are never initialized by the original ("Decisions where the port departs from a transcription"). | not measured | declared, not reproduced | none | unchanged |
| 478, 533, 1761–1776 | numeric | `Dr`/`Db` (implicit REAL*4, no declaration) pass through subroutine `SIZE`'s own REAL*4 store before the `Dr.le.Dmin`/`Db.le.Dmin` test; a draw a hair above `Dmin` can round down to `Dmin`'s own binary32 value and fire the test ("Decisions where the port departs from a transcription"). | measured 2026-09-23: real reference-mode replay observes 3/2,000,000 attempts on HMX (rate 3.72e-4) and 4/1,200,000 on HPEPA3 (2.21e-5), 0/500,000 on P33; against the original's own lagged replicas (mean 1.19e-3 HMX, 5.47e-5 HPEPA3) this mechanism alone reaches the right order of magnitude; counts of 3 and 4 events, whose Poisson 95 % intervals are 7.7e-5–1.09e-3 for HMX and 6.0e-6–5.7e-5 for HPEPA3, so HPEPA3 is fully consistent and HMX narrowly short (`tests/Particle.Tests/DminBoundaryProbe.cs`, `DminBoundaryProbeTests.cs`). | declared, not reproduced | `HPEPA3,HMX: ConditionBreaking(2)` | unchanged |

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No second copy of any branch of the attempt for a special mode.
- No refresh of QKS1, `pdoksmall` or `Dmaxxx` inside the attempt.
- No `double` written to shared memory; no integer histogram in the record.
- No fix of a defect of the original under the `Original` model; under `Corrected`,
  none beyond the flags of `## Corrected model`, each row marked `fixed (<Member>)`.

  ⚠ 2026-10-18: was no "fix" beyond the declared list under any model, now the flags of `## Corrected model` under `Corrected` → HISTORY.md#taboo-fix-corrected-2026-10-18
- No `CorrectionSet` flag read but once, at the top of `Attempt.Run`; the members it
  calls receive one as an explicit `bool`; no `Corrected` statement outside the sites
  `## Corrected model` names, and no copy of a branch to hold one.
