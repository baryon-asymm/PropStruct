# HISTORY.md — Statistics

Append-only. Newest first. Each entry carries the date, the section of `BOOT.md` it
came from, and the original text in full. Read only by following a dated pointer from
`BOOT.md`; the start procedure (AGENTS.md §10) does not read this file.

<a id="specification-out-of-tree-2026-10-04"></a>
## 2026-10-04 — from "## Constraints" — the §6 deviation's line, before the source and the listing excerpt left the repository

Replaced when the owner decided the delivery (root `BOOT.md`, `## Delivery`): the Fortran
source and the executable's listing excerpt lie outside the repository from stage S2, and
the generator scripts and checks that read them are `Category=Legacy`. The declaration as
it stood at 75bc2c4 (one physical line):

> ⚠ Declared deviation, §6: the specification is the Fortran source by the line ranges of `## Line map`, for the same reason as in `Particle`, and, for what the per-cycle plane stores, keeps in registers and multiplies in which order (decided 2026-10-02), the executable's listing excerpt `tests/Fixtures/Legacy/PropStructV3.cycle-plane.listing.txt` through `CyclePlaneListing.map.txt`, replaced by: ## Line map, ## Categories, ## Report, ## Defects of the original.

<a id="c2-four-moved-cells-2026-10-03"></a>

## 2026-10-03 — from "## Acceptance criteria" (A3) — the four cells C2's list moved by in stage 3, told in full

The text as it stood; the 551 cells and their classes did not change.

  The list of differing cells moves by four cells, all in the set the
  plane can move, and stays at 551: HMX `epsfkarm` (5.3383958E-02 against 5.3383961E-02)
  and P33 `Medium coef` (2.309953 against 2.309952) now differ by one binary32 or print
  unit, PSAN01 `Medium coef` and `epsdoksd_n[1]` (0.893818E-02, was 0.893839E-02) now
  equal; per formulation 59, 45, 32, 203, 146 and 66 became 60, 45, 33, 201, 146 and 66,
  besides 3, 2, 2, 2, 32 and 35 declared.

<a id="oracle-fixture-cases-2026-10-03"></a>

## 2026-10-03 — from "## Acceptance criteria" (A2) — how the oracle's fixture came to its cases, told in full

The text as it stood before the stage of this date; the figures of the criterion did not change.

  The oracle's fixture was
  regenerated the same day (`cycle_plane_oracle.py generate`, seed unchanged): its
  injected setup values come from the twin's setup model, which now equals the
  executable's `DOKSD` where it was one binary32 unit off, so the replay of the
  committed cases differed; 19 cases and 58 isolated sites now, 18 `unreached`, 22 and
  54 before. Those 19 never told `DOKM`'s schedule from rule L (the arbiter's F1): 22
  cases since the same day (`extend`), three pinned (`BK10`, `KB397`, `AK157a`, where
  the pre-loop alone decides), the same 58 and 18; the setup ratchet stays empty.

  Since this stage (`extend`, 2026-10-03): four pins added (`HMX`, `P2a` for `ZS`,
  `HPEPA3` for `ZX`, `CSPX04` for the `JZZ = 2` loop), the 22 cases replayed with
  `PARAM` executed: no total and no output of any cycle moved, `init_steps` and the
  preloop gained `zss`, `zx`, `z11`, and the twin's `zx`/`z11` of 16 cases moved by the
  corrected denominator; 26 cases, the same 58 and 18.

<a id="survey-c166-reader-2026-10-03"></a>

## 2026-10-03 — from "## Acceptance criteria" (A13) — the first run of the survey and `C166`, told in full

The note as it stood.

  ⚠ 2026-10-03: the first run gave `C166` besides `CSPX04`: the twin's `.dat` reader
  took tokens across records, so `C166.dat`'s fifteen-value `GDOK` record (`NMM = 14`)
  moved `DDOK` by one and the oracle ran other inputs than the port. It now follows the
  READ statements (lines 94, 124–133); of the 49 files only `C166` changes, `verify`
  and the 22 cases are unmoved. The arbiter's survey, rule L on three files, had the
  same blind spot.

<a id="setup-jzz2-loop-2026-10-03"></a>

## 2026-10-03 — from "## Setup plane" — the `JZZ = 2` loop's stores and the walker's pointer, told in full

The wording as it stood before the stage of this date; the text above it did not change.

  `DOK4` and `DOK3` are stored every pass (`JZZ = 2`); [...] `DOKSD`'s sum stays in the
  register and line 405 subtracts `DOKM**2` from it unrounded.

  Why [the table does not reach 379–406]: the walker would have to read a pointer kept in
  a frame slot (`[ebp-3Ch]`, `DOKSD`'s home too) and the `JZZ = 2` loop's temporaries,
  more than the correction was worth. [...] Not reproduced, a known gap: the `JZZ = 2`
  loop on `CSPX04` (`DOKM` 0x393BAB05 executed, 0x393BAB06 here; `DOKSD` 0x320F1DE8,
  0x320F1DE3), one of six shipped files, the survey's one approved line (A12, A13).

  ⚠ 2026-10-03: was these, now the four-fold loop's stores read from 0x408CFE–0x408F8E
  (`u − l` a REAL*4 temporary in pass 1 of a block and every remainder pass, `l**5`
  stored every pass, `DOK4`/`DOK3` every pass, `DOKSD` after passes 1–3 of a block),
  reproduced on all six shipped `JZZ = 2` files and on 120 drawn loops. The pointer was
  no obstacle: with the scope of the classifier's map extended to 0x408FA6 the check
  reports 130 problems, 128 float events in no row, one cell read before written and
  one scope not covered, and none from the walk; what generating the schedule costs is
  the rows, the temporaries' cells and two new site kinds (`ACCEPTANCE.md` A8).

<a id="qks1-divisor-2026-10-03"></a>

## 2026-10-03 — from "## Report" and "## Defects of the original" — the divisor of line 718, told in full

The wording as it stood before the stage of this date.

  One more residual is outside the excerpt: `QKS1` (718) is `QKS/real(QKSS)` in the
  particle loop, whose bytes the oracle does not run, so it injects `f32(q / f32(QKSS))`
  where `Pockets` computes `r32(q / QKSS)`: one expression while `QKSS` is a binary32
  integer, up to 2^24, two above it (the row `718, 963–996`,
  `tests/Statistics.Tests/Qks1NormalisationTests`).

  The row `718, 963–996` (`open`): "Which the executable computes is not known: the
  divisor's conversion is not in the excerpt."

  ⚠ 2026-10-03: was these, now read: the particle loop's bytes at 0x40B7FA–0x40B8AF lie
  in the excerpt's loop range (kept for its writes), `QKSS` is stored as `INTEGER*8`
  (0x40B7FA), `fild qword [ebp-488h]` loads it exactly (0x40B823) and each cell is
  `fild qword [edx]; fdiv st,st(1); fstp dword [ecx]` (0x40B841–0x40B8AF), the
  remainder loop the same (0x40B899–0x40B8AF): the divisor is the exact count and only
  the quotient is rounded, which is `Pockets`' expression, so the row is `reproduced`
  and the oracle's injection is `f32(q / QKSS)`.

<a id="row-771-1175-closing-wording-2026-10-03"></a>

## 2026-10-03 — from "## Acceptance criteria" — the closing wording of the row 771–1175 criterion, told in full

The correction as it stood before the move; ticks and figures above it did not change.

  ⚠ 2026-10-03: was "`reproduced` with C1 at 22 of 22 and C2's list empty but for cells
  another node declares, or `declared, not reproduced`"; C1 is 22 of 22 but C2's list
  is 551 cells, not empty, and no other node's `Differing cells` names them. By the
  design's attribution of 2026-10-02 (A3: a class, not a per-cell measurement) they
  are the print plane's (`src/Output`, row 1184–1453) and the attempt plane's
  (`src/Particle`'s REAL*4 `Dr`, `Db`, `RK`); the design expected them to stay, and the
  wording had assumed they would be declared elsewhere. The row is `reproduced` for
  what the listing models, held by the oracle's empty ratchet, not by C2; whether those
  cells are declared in the rows of `src/Output` and `src/Particle` is those nodes'
  decision.

<a id="row-771-1175-reproduced-then-open-2026-10-02"></a>

## 2026-10-03 — from "## Acceptance criteria" — the row 771–1175 criterion, `reproduced` then `open`, told in full

The correction as it stood before the move; ticks and figures above it did not change.

  ⚠ 2026-10-02: was "so row 771–1175 is `reproduced`" (the criterion of 2026-10-01
  above), now `open`: the executable's listing contradicts the plane's rules at sites
  that no control here reaches (`BOOT.md`, "## Report", the ⚠ of that date). Every
  control of this file stays green and is unchanged.

<a id="control-iv-reported-only-2026-10-02"></a>

## 2026-10-03 — from "## Acceptance criteria" — control (iv), "reported only, not gated", told in full

The correction as it stood before the move; ticks and figures above it did not change.

  ⚠ 2026-10-02: was "reported only, not gated" (an orchestrator decision of 2026-09-28):
  "neither file has a matching `.dat` fixture", the inputs recovered from the printed
  header, `rps01` giving 0.7282647 against the archive's 0.7284034 and `rps02` 0.6019530
  against 0.6019315; now gated. Both `.dat` files are shipped, and the old figures were
  artefacts of the inputs: the printed `Gfr` has three digits, and the printed
  `fmdok[1]` (`ALLVDOKSO(2)`, 0.235E-02 on `rps01`) was read as `FMDOK(2)`, which is
  `fmdok[0]`. Found by the review of 2026-10-02 and re-run here.

<a id="defect-rows-771-1175-open-2026-10-03"></a>

## 2026-10-03 — from "## Defects of the original" — the two `open` rows of 771–1175, closed as one

Replaced when the plane followed the executable's listing and the criteria A1 to A7
were met (`ACCEPTANCE.md`): the two rows, each told in full, cell by cell, as they stood
at 13c747d (stage 3), both `open`.

Row 1, "771–1175":

- Fortran:
  771–1175
- Kind:
  numeric
- What the original does:
  The per-cycle processing computes in x87 registers and keeps its results in REAL*4
  homes. Read off the executable's listing (2026-10-02, "## Report", the ⚠ of that
  date) and held by the plane: a REAL*8 or integer value stored to REAL*4 is rounded
  at its store, an integer count converts exactly, 795's quotient is never stored
  (796's own store rounds it), literals and all-literal subexpressions are binary32
  (`3.14159/6`, `0.75/3.14159`, `3·0.016`, `2·0.027` are folded binary32 constants in
  the executable's `.rdata`). **Not held**, refuted by the same listing at the sites
  of the next row and of "## Report": that a loop-invariant sum is rounded once after
  its loop (798 `ALLVDOKS` is never stored and 804 divides by its register sum; 989
  `D432` and 991 `Dqkarm` are stored back to their REAL*4 slots every sixth pass of a
  six-fold unrolled loop and every pass of its remainder loop; 990 `D243` is never
  stored and 996 reads the register; 1004 `gdoksfr` is added unrounded into 1006), and
  that every value handed to a later block is rounded (the same sites hand on an
  unrounded value). **Closing rule** (2026-10-02): this row and the next close
  together, as one `reproduced` row for what the listing models and one `declared, not
  reproduced` row for the runtime routines `_FIIfexp_`, `_CIsqrt` and `_FXAMOD`, once
  `ACCEPTANCE.md`'s A1–A7 are met, and on fewer never.
- Consequence:
  measured 2026-09-28 out of sample, at sites the listing agrees with: the 795 rule
  reproduces `epsdokfr` in 295 of 295 archived outputs, R1 puts 2016 of 2016 printed
  `epsx` cells on the 2⁻²⁴ grid, rule L's `DOKM` reproduces `epsalldok` in 293 of 293
  (`tests/Statistics.Tests/PerCyclePlaneOutOfSampleControlsTests`); measured
  2026-10-01 at seed 0 under `Original`: `da_coef` prints the original's on all five
  reference formulations (PSAN02n was one print unit below), and so do HMX
  `epsx(1..4)`, P33 `epsx(1..6)`, HMX and P33 `epsalldok`, HMX `epsdok(2)` and inpt
  `epsdok43_n[0..4]`, with no cell the port printed exactly lost
  (`tests/Simulation.Tests/CyclePlaneSeedZeroControlsTests`). The effect of the
  refuted clauses on the stochastic cells (`ALLDOK432`, `D432`, `sdevP43`, `Dqkarm`
  and what they feed) is not measured; measured 2026-10-03 (stage 3, the plane follows
  the listing's table): the three archive controls above are unchanged (295 of 295,
  2016 of 2016, 293 of 293), `da_coef` 22 of 22, the seed-0 controls and counters
  unmoved; the rate and the pair list are regenerated in stage 4
- The port:
  open
- Differing cells:
  none

Row 2, "771–1175 (register lifetimes)":

- Fortran:
  771–1175 (register lifetimes)
- Kind:
  numeric
- What the original does:
  Where the x87 code keeps a REAL*4 value in a register rather than its home was read
  from the statement structure ("## Report", the declared residuals): the in-block
  reads of 804, 816, 1006, 1008, 1031, 1061 and 1150; the dead stores 1014, 1015 and
  1050; scalars read by a later block; rule L inside loops that call `sum()` (1028,
  1036, 1089–1094, 1101–1102, 1110–1112). The executable's listing (2026-10-02) agrees
  at 795, 804→805/806/808, 1014, 1015, 1028 and 1031, and **contradicts the rule at
  three more**: 1006 `gdokleft` is stored rounded (0x40EA6F) and every in-block read
  takes the stored value, 1008 `PLOTsmdok` is read from its home by 1010 and 1014,
  1150 is stored and re-read from its home by 1151 (no output effect at the first and
  third while `gdokleft` is 0, a rounded 0 being 0). Binary32 temporaries the rules do
  not model: `1./PLOT2`, `1.-GGG`, 1015's factor, `(1-eta)/2000.+eta/3000.` and
  `PLOTsm` are computed once before the cycle loop into REAL*4 slots; `GGG-gdokleft`,
  `1-GGG+gdokleft` and 1150's divisor are spilled to REAL*4 slots. The clamp order at
  1061–1062 is output-neutral (`pdoksmall`'s previous cell is binary32 and rounding is
  monotone). Not read in the listing: 816, 1036, 1050, 1061, 1089–1112, 1137, 825–959.
- Consequence:
  measured 2026-10-02 on `da_coef`, which depends on the setup values and one FMDOK
  cell alone: the plane prints the original's value on 19 of 22 formulations whose
  input the archive ships and misses by one unit of the seventh digit on HPEPA2
  (0.7693585 against 0.7693586), P777 (0.6313831 against 0.6313832) and CSPX01
  (0.7778412 against 0.7778413);
  `tests/Statistics.Tests/LegacyDaCoefReproductionTests`; the review of 2026-10-02
  attributes the three misses to 1008, 1010, 1014 and the hoisted temporaries together
  and finds the five reference formulations printing the same `da_coef` under every
  combination of the rules, so `CompareSets` and `DaCoefTests` are blind here, by a
  recomputation not committed. On path-identical runs of the original and the port
  (`Original` layout and kind, seed 1, every integer cell equal) 32 to 203 cells per
  formulation print a different token (HMX 59, HPEPA3 45, P33 32, PSAN01 203, PSAN02n
  146, inpt 66, besides the declared), which of them the rows of this section cause is
  not measured (`tests/Statistics.Tests/PathIdenticalPairsReportTests`,
  `tests/Fixtures/cycle-plane-pairs`); measured 2026-10-03 (stage 3): the plane now
  prints the original's `da_coef` on 22 of 22 (`LegacyDaCoefReproductionTests`), and
  every site of the table the oracle isolates agrees with the executable's bytes
  (`ListingOracleTests`, empty ratchet)
- The port:
  open
- Differing cells:
  none


<a id="per-cycle-plane-source-read-rules-2026-10-03"></a>

## 2026-10-03 — from "## Invariants" and "## Report" — the per-cycle plane's source-read rules, replaced by the listing's table

Replaced when `Compute` came to follow `CyclePlane.listing.generated.txt` (the
executable's own listing) instead of the rules R1–R6 and L read from the Fortran's
statement structure. The invariant's own wording and the bullet that stated the rules,
each told in full.

The invariant, from "## Invariants":

- **The per-cycle plane follows the precision kind** (decided 2026-09-27, implemented
  2026-10-01). Under `Original` `Compute` rounds exactly the rounding sites of
  `CyclePlane.generated.txt`, each through `CyclePlaneRounding` ("## Report", "The
  per-cycle plane under `Original`"); under `Binary64` it is bit-identical to before.

  ⚠ 2026-10-02: was the rounding sites of the source-read table, now the executable's
  listing, `CyclePlane.listing.generated.txt` ("## Report", "The per-cycle plane from
  the executable's listing"); decided, not yet implemented (`ACCEPTANCE.md`, A1–A7),
  so until the code follows it `Compute` is the plane described above.

The rules, from "## Report" ("The per-cycle plane under `Original`"):

- **The per-cycle plane under `Original`** (decided 2026-09-27, rule of 2026-10-01). Under
  `Original` the plane 771–1175 holds what DVF's x87 code leaves in its REAL*4 homes, a
  model measured where the controls measure it (criteria below), per site of
  `CyclePlane.generated.txt` (`classify-cycle-plane.py`, over `fortran_source.py`):
  - **R1** a store whose right-hand side is REAL*8 or INTEGER is rounded, and every
    read of it, in its block or not, sees binary32;
  - **R2** a REAL*4 store read outside its basic block — a later block, the next
    iteration, the print plane, the next cycle — is rounded; its reads inside its own
    block take the unrounded value (`memory+forwarded`);
  - **R3** a REAL*4 store whose every read lies in its own block is not rounded
    (`forwarded`: 795, 1014, 1015, 1050, and 1141, not ported);
  - **L** a REAL*4 sum of REAL*4 terms whose target its innermost DO loop does not
    vary is carried unrounded through the loop and rounded once after it (`loop`);
  - **R4** integer loads (`real()` and mixed mode), exact constants and copies of
    values already in memory change nothing;
  - **R5** an inexact REAL*4 literal is its binary32 value, an all-literal REAL*4
    subexpression is folded in binary32 from its literals' binary32 values;
  - **R6** `QKS1` (718) arrives rounded where `Pockets` recomputes it; the setup plane's
    values and `Particle`'s accumulators arrive as stored.

  A basic block ends at a label, `DO`/`END DO`, every `IF` form, `GO TO`, `CALL`, I/O,
  `CONTINUE`, `EXIT`, `RETURN` and `END`; an assignment with an array intrinsic
  (`sum()`) starts one, a whole-array assignment is one. Declared residuals, none
  measured: 80-bit against 53-bit intermediates, as in the setup plane (premise refuted
  2026-10-02, the ⚠ below); register spills inside a block; `IF`/`END IF` joins counted
  as block ends; a runtime call (`**` with a REAL exponent) not counted as one; L inside
  loops that call `sum()` (1028, 1036, 1089–1094, 1101–1102, 1110–1112). The print
  plane's own REAL*4 expressions (1176 on) stay `double` (`src/Output/BOOT.md`).
  `CyclePlaneRounding` carries each rounding site, one method per site kind, each call
  citing its Fortran line; `CyclePlaneSites.txt` maps every rounding site to its member
  or to why it is not ported, checked four ways
  (`tests/Statistics.Tests/CyclePlaneSiteMapTests`). Under `Binary64` every method
  returns its argument, so nothing moves.

---

<a id="setup-rule-l-sums-2026-10-03"></a>

## 2026-10-03 — from "## Setup plane" — the sums bullet under rule L and line 378 "not yet implemented"

Replaced when `Setup.Prepare` followed the listing at lines 378–406: `DOKSD` is a
register sum, never stored; `DOKM` (`JZZ ≠ 2`) is unrolled five-fold; `DOK4` and `DOK3` are
stored every pass; line 378 divides into a REAL*4 temporary.

- **Sums rounded per addition, but loop-invariant sums once at the loop's exit (rule L,
  `DOKM`, `DOKSD`); every store to a REAL*4 variable rounded.** `ZSS` is
  `PARAM`'s `ZS` under another name (by reference, line 377); it is summed with a
  binary32 rounding after every addition and divides by its final total in a separate
  loop, as the port already does. Arithmetic between rounded operands runs in `double`
  and is rounded where the Fortran stores to REAL*4; the Fortran's x87 extended
  intermediates are not emulated (a declared residual: both controls below were computed
  exactly this way and hit the original's printed digits).

  ⚠ 2026-10-01: was "per addition" for every sum, now rule L for loop-invariant ones (iii)

  ⚠ 2026-10-02: was "the Fortran's x87 extended intermediates" as the premise of that
  residual, now refuted: 53-bit precision (`## Report`, the second ⚠ of that date).

- **Line 378 holds a compiler temporary** (decided 2026-10-02, not yet implemented,
  `ACCEPTANCE.md` A7). `sLamd = (GGG/PLOT1)*ZSS*PLOT2`: the executable divides once
  into a REAL*4 slot (0x408ABE), then forms `(ZSS·G)·PLOT2` (0x408AC4–0x408AD0), so
  under `Original` λ takes `G = binary32(GGG/PLOT1)` and that product order. The
  hoisted block that holds `G` also holds the per-cycle plane's `T1`–`T4` and `PLOTsm`
  ("## Report"). The reading is the arbiter's; the listing oracle's pre-loop run is
  its evidence before it is relied on. λ moves on nine shipped `.dat` files and on none
  of the five reference formulations; `Binary64` is unchanged. The line map's row 378
  states the formula, not this order.

---

<a id="per-cycle-plane-stays-double-2026-10-01"></a>

## 2026-10-01 — from "## Report" — "The per-cycle plane stays `double`", replaced by the rule

Replaced when the per-cycle plane was reproduced under `Original`. Two of its claims were
wrong: the conversion is at 795 (793 is a comment), and rounding the integer
conversions moves HMX `epsdokfr[0]` to 0.259E-05, away from the original's 0.251E-05.

- **The per-cycle plane stays `double` under either precision kind** (decided
  2026-09-27). The Fortran stores every result of 771–1175 that its declarations or
  implicit typing make REAL*4, converts integer counts to default REAL before dividing
  (793), and folds that range's literal expressions in binary32; the port does none of
  this, so under `Original` a printed per-cycle value can differ from the original's in
  its last printed digit where every input agrees. `da_coef` (`mp`, 1014–1016) shows it
  on every run: `FMDOK(int(Dmin/Di)+1)` is empty in every output of both programs, so
  `mp` depends on the setup plane alone, and PSAN02n prints 0.7917714 against 0.7917715.
  The generator accuracies (771–784) show it only where the generator counters equal the
  original's (HMX, P33, seed 0). Extending the kind here is decided after acceptance,
  whole or not at all: rounding the stores without the folds and conversions regressed
  HMX's `da_coef` and `epsdokfr[0]`.

---

<a id="section-15-lifted-note-2026-10-01"></a>

## 2026-10-01 — from the declared deviations under "## Constraints" — the §15 lifting note told in full

Moved to keep the node within its limit (AGENTS.md §15).

  ⚠ 2026-09-27: the §15 declared deviation is lifted. The D6 second pass (this task)
  moved the remaining stale narration of "## Setup plane" and "## Acceptance criteria"
  to `HISTORY.md`, fused the `Dmin`/`Di`/`Dj` rounding paragraphs and their criteria,
  and re-wrapped the counted prose at 88 columns, bringing the document back inside
  the AGENTS.md §15 400-line leaf limit without moving any current truth → HISTORY.md#section-15-deviation-lifted-2026-09-27

---

<a id="dmin-di-dj-criteria-fused-2026-10-01"></a>

## 2026-10-01 — from "## Acceptance criteria" — the fused `Dmin`/`Di`/`Dj` criterion's correction told in full

Moved to keep the node within its limit (AGENTS.md §15).

  ⚠ 2026-09-27: was two separate criteria for `Dmin` and `Di`/`Dj`, now fused to match
  the setup-plane rule above; "no mechanism established" corrected, since the root's
  own criterion now lists this count among the declared differences (mechanism owned
  by `src/Particle`, not retold here per AGENTS.md §8).

---

<a id="ddokmax-excluded-2026-09-27"></a>

## 2026-10-01 — from "## Acceptance criteria" — the `Ddokmax` correction told in full

Moved to keep the node within its limit (AGENTS.md §15).

  ⚠ 2026-09-27: was "`Ddokmax` is excluded, staying `Setup.Sizes`'s pre-existing
  array-size exception"; since the site-map review (2026-09-24) the script computes it
  from the stored bounds (`max(ddok[1::2])`) and the test asserts it too.

---

<a id="script-no-test-of-its-own-2026-09-27"></a>

## 2026-10-01 — from "## Acceptance criteria" — a correction told in full

Moved to keep the node within its limit (AGENTS.md §15).

  ⚠ 2026-09-27: was "recorded here since the script has no test of its own to carry
  it"; the test above has existed since 2026-09-24.

---

<a id="categories-undeclared-clamp-2026-09-18"></a>

## 2026-10-01 — from "## Categories" — the undeclared clamp, told in full

Moved to keep the node within its limit (AGENTS.md §15), oldest correction first.

⚠ 2026-09-18: this invariant was first an undeclared clamp, `Categories.cs`'s own
step-1 loop additionally bounding `r < Ncat`, which the Fortran `do irow = 1,DPRow:
Dpockets(irow) = irow*Dj` does not have. Found while rewriting `tests/Fixtures/
formulas_statistics.py`'s independent transcription (blocker 2 of the audit of this
node): the honest, unclamped Python rewrite reproduces every existing `categories.json`
case byte for byte, because `DPRow ≤ Ncat` held for all four cases already in evidence,
not proven equivalent in general at the time. Left as a declared, undocumented
defensive divergence pending a design session; the owner's decision the same day
(second session) replaced the silent clamp with the checked status above and stated the
invariant that makes it unreachable, per the proposal this paragraph used to leave open.

---

<a id="report-criterion-reformulated-2026-09-27"></a>
## 2026-09-27 — the report's own end-to-end criterion, before its F4 reformulation

**Section of `BOOT.md`:** "## Acceptance criteria", the report criterion. Found by the
D6 second-pass review (F4): the criterion asked for a per-run pass no run of the
original itself can meet (it fails 22 of 197 times against its own replicas), and named
the reference `results.m` as the band's own source when the band is built from the
replicas, not the reference. Replaced by the reformulated, ticked wording above, citing
the root's own rate criterion.

**Original text:**

> Fed with the totals of a reference-mode run, the report satisfies the statistical
> criterion against the reference `results.m` for every non-excluded quantity it
> computes.

<a id="section-15-deviation-lifted-2026-09-27"></a>
## 2026-09-27 — the §15 declared deviation, lifted a second time

**Section of `BOOT.md`:** "## Constraints", the closing paragraph. Deleted per AGENTS.md §13 ("a perpetually red check is worse than an absent one" — a declared deviation that no longer applies guards nothing) once the D6 second pass (this task) brought the document's counted non-blank lines back inside the AGENTS.md §15 400-line leaf limit, by moving the remaining stale narration of "## Setup plane" (the `Dmin`/`Di`/`Dj` paragraphs' own live-run controls, the "Implemented the same day" paragraph's hand-typed list, the "Re-measured after both fixes" paragraph) and of "## Acceptance criteria" (per-criterion narration beyond tick, date, claim and place) to `HISTORY.md`, and by wrapping the surviving prose to 88 columns; no current-truth content — the six canonical sections' own decisions, the checkbox criteria with their dates and places, or the one-line pointers themselves — was removed to reach the count.

**Original text:**

> Declared deviation, §15: this document is over the 400-line leaf limit; the linter prints its count, which is not copied here (AGENTS.md §8). Audit item D6 (2026-09-26) moved every movable ⚠, run table and duplicated mechanic to `HISTORY.md` and wrapped the counted prose at 88 columns. What remains is the acceptance criteria, which §15 does not let move, and the design rationale of `## Setup plane`. Lifts when a design session decides which of that rationale can move to `HISTORY.md` without losing it. A child node is not a route: it would lower this node's limit to 250.
>
> was this deviation deleted the same day, on a premature reading that D6's moves alone would bring the count under 400; the honest 88-column rewrap showed 75 lines still over, and the deletion is undone here, restoring the deviation.

<a id="dmin-di-dj-rounds-too-fused-2026-09-27"></a>
## 2026-09-27 — "`Dmin` rounds too" and "`Di`/`Dj` round too": the two separate paragraphs and their live-run controls, before the fusion

**Section of `BOOT.md`:** "## Setup plane", the two paragraphs "`Dmin` rounds too, found 2026-09-23" and "`Di`/`Dj` round too, found 2026-09-23", and the "Controls" paragraph between the design decision and the second one. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6, second pass); the two paragraphs are fused into one rule in `BOOT.md`, and the controls' own pass/fail outcome is recorded directly in the acceptance criteria for `Dmin` and `Di`/`Dj` below, citing this entry for the run detail.

**Original text, "`Dmin` rounds too, found 2026-09-23":**

> `Dmin` has no declaration in lines 4–64: REAL*4 by implicit typing, like `gdokns`, and converted from micrometres at line 266 exactly as `DDOK`'s own bounds are at 260–263 (`SetupPlane.generated.txt`'s `dmin` row, role `input`). `Prepare` rounded the fraction bounds but passed `Dmin` through `parameters.Dmin.Metres` unrounded: a one-ULP sliver `Attempt`'s "Dok < Dmin" check (Fortran 478, 533) can land a draw in whenever the default `Dmin` rounds to a different binary32 value than the lowest fraction's own lower bound it is meant to equal. Fixed by rounding `Dmin` through the same `ToBinary32MetresStored` helper (`SetupPlaneTests.DefaultDminRoundsToTheSameBinary32ValueAsTheLowestFractionBound`, three formulations, seen red on the violation before the fix, one binary32 ULP apart).

**Original text, "`Di`/`Dj` round too, found 2026-09-23":**

> Same shape as `Dmin`, immediately above it in the source (lines 264–265, no declaration, `SetupPlane.generated.txt`'s `di`/`dj` rows), consumed everywhere the report bins by cell. `Setup.Sizes`'s own internal, unconditional rounding of `Di`/`Dj` (this node's own BOOT.md, "Array sizes from binary32 values") feeds only `Ndok`/`Nkarm`/`Ncat`, untouched; the *stored* `ModelSetup.CellSize`/`CategoryStep` every other consumer reads went through `parameters.CellSize.Metres` unrounded regardless of kind. On HMX/HPEPA3, whose default `Di` equals the default `Dmin`, this binned a draw at the lowest fraction bound to `int(D/Di) = 0`: printed `fmdok[0]` was nonzero under `Original` (HMX 0.2–0.3e-10, HPEPA3 ≈3.7e-10) where the original always prints zero. Fixed like `Dmin`, through `ToBinary32MetresStored` (`Setup.cs`'s doc comment carries the derivation; `SetupPlaneTests.DefaultCellSizeRoundsToTheSameBinary32ValueAsDminSoTheLowestBoundBinsToCellOne`, seen red before the fix).

**Original text, "Controls":**

> Controls (root taboo, "every check … proven twice"): positive, HMX and HPEPA3 seed 0 under `--precision original` now print `fmdok[0] = 0.000E+00`, matching the original, and `fineoxy_fr = 0.0000000E+00` as before; the "Dok < Dmin" rate stays at the bit-exact zero the `Dmin` fix already reached (unaffected: a different mechanism, `Attempt`'s comparison, not this binning); negative, `--precision double` reproduces the pre-`Di`/`Dj`-fix `results.m` bit for bit (time line aside) on all five reference formulations, run from a worktree at the pre-fix commit.

<a id="setup-plane-implementation-note-2026-09-27"></a>
## 2026-09-27 — the setup-plane "Implemented the same day" paragraph, with its hand-typed "now round" list

**Section of `BOOT.md`:** "## Setup plane", the paragraph after the menu-parameters-superseded pointer. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6, second pass); the current truth (the test name, `SetupPlaneClassificationGeneratorTests`) is restated in "## Acceptance criteria" below, where it belongs (the criterion this test proves). The hand-typed list of names is the exact AGENTS.md §6 failure the 2026-09-24 design decision was made to end (a criterion quantified by "all" checked against a list typed by hand rather than generated); it is archived here, not restated.

**Original text:**

> Implemented the same day (`src/Statistics/classify-setup-plane.py`, `Setup.cs`, `CycleStatistics.cs`, `SetupInputs.cs`; `src/Simulation`, `src/Output`, both `BOOT.md` own implementation notes). `SetupPlaneClassificationGeneratorTests` (`tests/Statistics.Tests`) is the both-ways site-map proof, seen red on a removed entry and on a bogus one; AK1–AK4, `Alpha`, `NnMin`, `NnMax`, the two structural coefficients, `Gm`, `EpsDok` and `Eta` now round under `Original` (`SetupInputs`/`ModelSetup`).

<a id="setup-plane-remeasured-both-fixes-2026-09-27"></a>
## 2026-09-27 — "Re-measured after both fixes": the setup-plane re-measurement paragraph and its print-resolution reasoning

**Section of `BOOT.md`:** "## Setup plane", the closing paragraph and its own pointer. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6, second pass); the deciding figures (`Binary64` bit-identical, `Original` moves no cell, the link-1 ratchet unchanged) are restated in "## Acceptance criteria" below, in the `Binary64`-unchanged criterion, dated 2026-09-24.

**Original text:**

> Re-measured after both fixes: `Binary64` stays bit-identical on all five reference formulations; `Original` moves no cell at seed 0 with default parameters, and the link-1 ratchet of `tests/Simulation.Tests` (`StatisticalCriterionTests`) stayed green with its three named cells unchanged.
>
> was the eighth-significant-digit/print-resolution reasoning for why the 39-formulation `Ddokmax` disagreement stays invisible here told in full, now the measured outcome alone: the 39-formulation disagreement sits at the eighth significant digit, two to three digits past the `F7.2`-micrometre print width the header's own `Ddok_max`/`Dokb_max` lines use, so it is real and measured but invisible at this print resolution on the five formulations this criterion runs.

<a id="binary64-half-hidden-deviation-2026-09-26"></a>
## 2026-09-27 (moved, dated 2026-09-26) — the `Binary64` exception's own hidden-deviation finding

**Section of `BOOT.md`:** "## Invariants", "The setup follows the precision kind". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6, second pass); the current truth (the exception itself, with its date and bounding measurement, in the invariant's own text) did not move.

**Original text:**

> was this invariant's `Binary64` half stated without exception — a hidden deviation (AGENTS.md §12), since "## Setup plane" itself already recorded the `Ddokmax` review as a change of the `Binary64` computation. Found while bringing this document inside the AGENTS.md §15 leaf limit (audit item D6); the exception above, with its date and bounding measurement, is what closes it.

<a id="literals-invariant-exception-2026-09-26"></a>
## 2026-09-27 (moved, dated 2026-09-26) — the "Double precision, literals as written" invariant's own hidden-exception finding

**Section of `BOOT.md`:** "## Invariants", "Double precision, literals as written". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6, second pass); the current truth (the scoping clause naming the setup plane's own literal expressions) did not move.

**Original text:**

> was this invariant stated without exception, false for three of its own examples since 2026-09-23: `3/3.14` and `24/3.14` (`FractionLaw.cs`) and `0.8` (`Setup.cs`) are folded in binary32 under `Original`, exactly as root BOOT.md's own "Fidelity to the original" already scopes the same rule. Found while bringing this document inside the AGENTS.md §15 leaf limit (audit item D6); the scoping clause above closes it. The attempt plane's literals stay as written under either kind, unaffected.

<a id="ndok-criterion-narration-2026-09-26"></a>
## 2026-09-26 — the `Ndok` archive-length criterion's own fixture wording and archive-absence reason

**Section of `BOOT.md`:** "## Acceptance criteria", the `Ndok` archive-length criterion. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the tick, the five archived formulations, the two boundary tests and the C166 control) did not move.

**Original text:**

> (fixture cases, via the printed `fqdokkarm` category rows), and the binary32 quotient helper equals exact rational truncation (the `(double)(float)value` oracle) on a generated set of operands. … `Nkarm`/`Ncat` are not independently checked against the archive (no clean, untrimmed printed array of exactly that length was found in `results.m`'s own print routine for either), but a constructed boundary case each proves the rounding rule they do use is actually the one the source's own unstored intermediate calls for, and a real, archive-anchored formulation (C166) proves it moves neither of them.

<a id="z11-zx-zss-criterion-narration-2026-09-26"></a>
## 2026-09-26 — the `Z11`/`ZX`/`ZSS` criterion's own script provenance and exception name

**Section of `BOOT.md`:** "## Acceptance criteria", the `Z11`/`ZX`/`ZSS`/λ/`Dmax`/echoes formula-script criterion. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the tick, the place, the test name and the fixture, the `Ddokmax` exclusion) did not move.

**Original text:**

> (`setup_plane`/`setup_plane_cases`, importing `formulas_particle.py`'s own already-verified `size_law_sample` for `Dmax` rather than re-deriving SIZE, root BOOT.md taboo) … `Ddokmax` itself is excluded from the fixture's own setup-plane recipe: it is `Setup.Sizes`'s pre-existing, unconditional array-size exception (operand-only rounding, `ToBinary32Metres`), untouched by this task and re-derived that way in the script too, not with the setup plane's own store-rounding `ToBinary32MetresStored`.

<a id="nkarm-ncat-product-rounding-s2-2026-09-24"></a>
## 2026-09-26 (moved, dated 2026-09-24) — S-2: `Nkarm`/`Ncat` never should have shared `Ndok`'s own helper

**Section of `BOOT.md`:** "## Invariants", "Array sizes from binary32 values", and "## Acceptance criteria", the `Ndok` archive-length criterion's own note. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (that `Nkarm`/`Ncat` go through `Binary32.TruncatedQuotientOfProduct`, `Ndok` through `Binary32.TruncatedQuotient`) did not move.

**Original text, "## Invariants":**

> was "through the same `Binary32.TruncatedQuotient` helper `Ndok`/`Nkarm`/`Ncat` already use" for all three — S-2, found by the audit of 2026-09-24. `Ndok`'s own numerator, `Ddokmax`, is a genuinely stored REAL*4 value (`DDOK`'s own maximum), so rounding it again before the truncated division is correct; `Nkarm`/`Ncat`'s own numerator, `Ddokmax·AK4`, is never stored to a REAL*4 variable of its own (`Binary32.Multiply`'s own doc comment: the x87 FPU keeps a compound expression like `Ddokmax*AK4/Di` in its extended registers and rounds to REAL*4 only at a store, and this product is never stored), so rounding it too, the way `Setup.cs` did through `Binary32.TruncatedQuotient`, double-rounds it and can move the truncated integer at a boundary — no archived formulation's own `Nkarm`/`Ncat` moves (the margin this section already measures, `1.68e-5` on HMX/C166/T56, is far from either integer boundary reading), but a constructed boundary case does: 1505 vs. 1506 for a synthetic `Nkarm`, 4389 vs. 4390 for a synthetic `Ncat` (`tests/Statistics.Tests/Binary32Tests.TruncatedQuotientOfProductLeavesTheProductUnroundedAtAnIntegerBoundary`, `SetupTests.SizesUsesTheUnroundedProductForNkarm`, `.SizesUsesTheUnroundedProductForNcat`, each seen red on the pre-fix code; `.SizesNkarmAndNcatOfC166AreUnaffectedByTheProductRoundingFix` for the real-formulation control). `Nkarm`/`Ncat` now go through the new `Binary32.TruncatedQuotientOfProduct`, which rounds only the denominator; `Ndok` is untouched.

**Original text, "## Acceptance criteria":**

> was "they share the same `Binary32.TruncatedQuotient` helper `Ndok` is checked against, but that is self-consistency, not archive evidence" — S-2, found by the audit of 2026-09-24: `Nkarm`/`Ncat` never should have shared `Ndok`'s own helper, since their numerator is an unstored product and `Ndok`'s is a genuinely stored value ("## Invariants" above). They now go through `Binary32.TruncatedQuotientOfProduct`, a different helper, so the old wording no longer describes the code, correct or not.

<a id="array-sizes-margin-attribution-2026-09-26"></a>
## 2026-09-26 — the worst-case margin's own attribution (which quotient, which formulations)

**Section of `BOOT.md`:** "## Invariants", "Array sizes from binary32 values". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the bound itself, `1.68e-5` against `6e-8`, no printed length moves) did not move.

**Original text:**

> the worst-case `DPRow` quotient `Ddokmax·AK4/Dj` — the same one `Ncat` is sized from, evaluated at its own theoretical maximum — sits no closer than `1.68e-5` from an integer boundary (HMX, C166, T56; every other formulation further still), against a binary32 relative epsilon near `6e-8` at that magnitude: no printed length moves.

<a id="pocket-forming-flag-d7-2026-09-24"></a>
## 2026-09-26 (moved, dated 2026-09-24) — D7: the pocket-forming flag, narrowed to `byte` without a length check

**Section of `BOOT.md`:** "## Constraints", the precondition list's own D7 note. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the Fortran zero/nonzero test behind `SetupStatus.InvalidPocketFormingFractionCount`) did not move.

**Original text:**

> was `formulation.PocketFormingFractions is { } flags ? (byte)flags[i] : (byte)1`, silently narrowing a flag to `byte` before testing it (a flag of 256 reads as 0, not pocket-forming) and indexing `flags[i]` without checking `flags.Length` against the fraction count first (`IndexOutOfRangeException` for a `Formulation` built in code with fewer flags than fractions) — D7, found by the audit of 2026-09-24. Now mapped by the Fortran test itself (zero stays zero, any nonzero flag forms pockets) and guarded by the precondition above, reported as `SetupStatus.InvalidPocketFormingFractionCount` (`tests/Statistics.Tests/SetupTests.PrepareTreatsAnyNonzeroPocketFormingFlagAsForming`, `.PrepareRejectsInvalidPocketFormingFractionCount`, both seen red on the pre-fix code).

<a id="why-whole-not-in-pieces-ladder-2026-09-26"></a>
## 2026-09-26 — "Why whole, not in pieces": the two throwaway builds' own intermediate ladder and token counts

**Section of `BOOT.md`:** "## Setup plane", "Why whole, not in pieces." Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the decision, and the one figure that decided it — the literal fold closing `epsdokfr[0]` to 2.70e-8) did not move.

**Original text:**

> Two throwaway builds (2026-09-23, branch `claude/fraction-table-experiment`, not adopted) rounded first `ZSS` alone, then also the mass shares and bounds at `FractionLaw`'s boundary. Each moved the deterministic quantities toward the original — `inpt`'s `epsdokfr[0]` 0 → 3.65e-10 → 1.07e-8, the second predicted before its build — but each was a third program: its table was the original's while its λ, `Dmax` and size draws were not, and its per-configuration token identity re-rolled (9 up / 3 down, then 7 up / 5 down, both sign-test null). The last rung, the literal fold, is what closes `epsdokfr[0]` to the original's 2.70e-8.

<a id="dmin-rounds-too-narration-2026-09-26"></a>
## 2026-09-26 — "`Dmin` rounds too": the HMX/HPEPA3/P33 default-bound coincidence and the `fineoxy_fr` measurement

**Section of `BOOT.md`:** "## Setup plane", "`Dmin` rounds too, found 2026-09-23." Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the mechanism in one sentence and the test name) did not move. The stale row name `SetupPlane.generated.txt`'s `MinimumSize` in this same paragraph is corrected in place to `dmin` (AGENTS.md §8, item 7 of the D6 review).

**Original text:**

> HMX's, HPEPA3's and P33's own default `Dmin` (10 μm) equals their lowest fraction's own lower bound (also 10 μm); before the fix the two round to different binary32 values, a one-ULP sliver `Attempt`'s "Dok < Dmin" check (Fortran 478, 533) can land a draw in, and `fineoxy_fr = gdokns + gdokleft/GGG` (`Output`) prints a value the original's own REAL*4 arithmetic never does — 0 in all 33 HMX files.

<a id="menu-parameters-left-out-2026-09-23"></a>
## 2026-09-26 (moved, dated 2026-09-23) — "Menu parameters considered and left out" (superseded the next day)

**Section of `BOOT.md`:** "## Setup plane". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); this paragraph was already stale (superseded 2026-09-24 by "Design decision, 2026-09-24", below) and is archived rather than restated.

**Original text:**

> **Menu parameters considered and left out** (`EpsDok`, `Alpha`, the pocket/bridge coefficients, `AggregatedOxideFraction`; `TailProbability` is in the generated set, REAL*8, does not round). None is read or stored inside this node's own setup ranges: `Alpha`/the coefficients are attempt-plane or per-cycle-REAL*4 operands, and `EpsDok` gates a per-cycle REAL*4 comparison whose other side is not reproduced either — rounding it alone would be the "third program" "Why whole, not in pieces" already rejected. Widening the kind into per-cycle processing is a design decision, not this task's; the full determination is `classify-setup-plane.py`'s own docstring.

<a id="eps-echo-proposal-2026-09-23"></a>
## 2026-09-26 (moved, dated 2026-09-23) — the `eps` header echo, left as an AGENTS.md §11 proposal (closed the next day)

**Section of `BOOT.md`:** "## Setup plane". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); this paragraph's proposal was already closed 2026-09-24 by "Design decision, 2026-09-24"'s `Output` bullet, below, and is archived rather than restated.

**Original text:**

> `eps`'s own header echo (the original's `5.0000001E-02` against the port's `5.0000000E-02`) is not fixed here: `ModelParameters.EpsDok` reaches `results.m` as a direct `Output` echo, never through this node's own setup plane, so closing it is `Output`'s own change (AGENTS.md §11 proposal, not made here).

<a id="design-decision-audit-findings-2026-09-26"></a>
## 2026-09-26 — "Design decision, 2026-09-24": the two audits' own findings

**Section of `BOOT.md`:** "## Setup plane", "Design decision, 2026-09-24" (opening paragraph). Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the decision's own cause, in one sentence) did not move, and the opening words of the design decision itself are unchanged.

**Original text:**

> Two audits of 2026-09-24 found that the kind of each name was generated but the list of names was typed by hand (`SETUP_PLANE_NAMES`). The list missed AK1–AK4, `Dmax` (line 1754) and `Ddokmax` (268, 271). Separately, `CycleStatistics` and `Output` read raw `plot1`, `plot2`, `MassShare`, `gdokns` and `Di`, so under `Original` one REAL*4 value existed in two values within one run.

<a id="design-decision-acceptance-sublist-2026-09-26"></a>
## 2026-09-26 — "Design decision, 2026-09-24": the acceptance sub-list

**Section of `BOOT.md`:** "## Setup plane", "Design decision, 2026-09-24", the `Output` bullet's own trailing "Acceptance:" sub-list. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the sub-list is met, per the "Re-measured" paragraph below it in `BOOT.md`, so nothing here is still open.

**Original text:**

> - **Acceptance:**
>   - the `Binary64` output is bit-identical on the five reference formulations;
>   - the `Original` output moves within Monte Carlo noise, and the link-1 ratchet of `tests/Simulation.Tests` stays green with its three cells unchanged;
>   - a positive control: an original output generated with a non-default REAL*4 menu value that binary32 cannot hold exactly, whose header echo the port reproduces digit for digit under `Original`.

<a id="menu-parameters-superseded-2026-09-24"></a>
## 2026-09-26 (moved, dated 2026-09-24) — the menu-parameters supersession, told in full

**Section of `BOOT.md`:** "## Setup plane". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (a REAL*4 menu value is a member because the root defines the plane by what is stored) did not move, restated in one sentence in `BOOT.md`.

**Original text:**

> this decision supersedes the paragraph "Menu parameters considered and left out" above. That paragraph kept the REAL*4 menu values out because rounding one side of a comparison whose other side stays `double` would make a "third program". That argument belongs to a computed chain reproduced in pieces, like `ZSS`. It does not apply to an input the original reads once and stores: the root defines the setup plane as every value "computed once per run before the particle loop and stored in REAL*4", and a stored input is one. Its consumers in the attempt plane and in per-cycle processing stay `double` by the root's decision either way. So the generator decides, and a REAL*4 menu value is a member. The `eps` echo proposal of the paragraph above is closed by the `Output` bullet.

<a id="ddokmax-assignment-lines-correction-2026-09-26"></a>
## 2026-09-26 — the implementation note's own `Ddokmax` assignment-line correction

**Section of `BOOT.md`:** "## Setup plane", the "Implemented the same day" paragraph. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the count (`Ddokmax`'s own two assignment lines, 268 and 271) is restated where it matters, in the "review" entry below (`HISTORY.md#setup-plane-review-corrections-2026-09-24`), so it is not repeated here.

**Original text:**

> `Ddokmax`'s own two assignment lines are 268 and 271, a correction of the count above, not a new finding.

<a id="setup-plane-review-corrections-2026-09-24"></a>
## 2026-09-26 (moved, dated 2026-09-24) — the setup-plane review: two wrong claims in the first telling, told in full

**Section of `BOOT.md`:** "## Setup plane", the "review" paragraphs. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the corrected positive control, `karmcoef = 0.03`, and `SetupEchoes.Ddokmax` read through `ToBinary32MetresStored`) did not move.

**Original text:**

> two claims in this paragraph's first telling were wrong, both found by the same review before merge.
>
> First, the positive control named below printed the same seven digits whether rounded or not (`karmcoef = 12.345`, DVF's list-directed field showing fixed-point form at that magnitude): it would have passed unchanged on the pre-change code, so it proved nothing. Replaced with `karmcoef = 0.03`, below the field's own 0.1 threshold where it switches to eight-digit exponent form: binary32(0.03) = 0.029999999329447746, printed `2.9999999E-02` against the double's own `3.0000000E-02` (`tests/Output.Tests/PocketCoefficientHeaderEchoTests`, now four facts: the rounded and unrounded unit-level pass-throughs, the real archive match under `Original`, and a negative control under `Binary64` that prints the un-rounded digits and does not match the archive — checked directly against the pre-change build too, which prints `3.0000000E-02` under `--precision original`, confirming the old control's blindness was real, not merely argued).
>
> Second, `Ddokmax` was mislabelled: "rounded unconditionally... by `Setup.Sizes`'s own pre-existing array-size exception" described a value that only sizes `Ndok`/`Nkarm`/`Ncat` and is never rounded a second time on store (`ToBinary32Metres`, a single binary32 multiply). The Fortran's own `Ddokmax` (line 271) copies an *already-stored* REAL*4 `DDOK` element (line 261), so the reported echo, the default `Dmaxxx` (`SmallParticles.MaxSize`) and the size-law paths downstream of it (`nextDmaxxx` through `Simulator`'s own cycle loop) must all read the same store every other fraction bound already goes through (`ToBinary32MetresStored`), not `Sizes`'s own numerator. `Setup.Prepare`'s fraction loop now tracks this separately (`storedDdokmax`) and feeds it to `SetupEchoes.Ddokmax`; `Setup.Sizes`'s own `ddokmax` out-parameter is untouched, serving `Ndok`/`Nkarm`/`Ncat` only, as the root's array-size exception asks. Measured before choosing not to touch `Sizes`: the two values disagree bit for bit on 39 of the 49 archived formulations (relative differences of order 1e-8, all at or past the eighth significant digit), but produce identical `Ndok`/`Nkarm`/`Ncat` on every one of them at the default cell size — the array-size exception's own reading needs no reconciling. Under `Binary64` the change reaches `SetupEchoes.Ddokmax` too (noted by the orchestrator on 2026-09-24). It used to be `Sizes`' binary32 product, and it is now the largest bound in plain `double`, as "Every other quantity stays double" requires, which the fidelity audit of 2026-09-24 found violated. `Binary64`'s `results.m` stayed bit-identical on the five reference formulations at seed 0: the change sits at the eighth significant digit. It is nonetheless a change of the `Binary64` computation, not a reproduction of the original. `formulas_statistics.py`'s independent transcription carried the identical mislabelling (its own `ddokmax_metres`, commented as deliberately out of scope, fed the exported `"ddokmax"` fixture field); fixed to reuse the same `ddok` array its every other quantity already reads correctly, `max(ddok[1::2])`. Site map entries for `ddokmax` (now `ported:`, naming both values and why they differ) and for `dok4`/`dok3` (now `ported inside Setup.AnalyticSizes`, not `not ported`, since both are folded directly into `Dokm` with no separately named field) corrected to match.

<a id="setup-plane-remeasurement-resolution-note-2026-09-26"></a>
## 2026-09-26 — the re-measurement's own print-resolution reasoning

**Section of `BOOT.md`:** "## Setup plane", "Re-measured after both fixes". Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the measured outcome: `Binary64` bit-identical, `Original` moves no cell, link-1 ratchet green) did not move.

**Original text:**

> the 39-formulation disagreement above sits at the eighth significant digit, two to three digits past the `F7.2`-micrometre print width the header's own `Ddok_max`/`Dokb_max` lines use, so it is real and measured but invisible at this print resolution on the five formulations this criterion runs

<a id="precondition-criterion-audit-narration-2026-09-26"></a>
## 2026-09-26 — the precondition criterion's own audit narration

**Section of `BOOT.md`:** "## Acceptance criteria", "Each precondition violation returns its status." Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the tick, the places, the `NoActiveFraction` exception, the `InvalidNnWindow` extension) did not move. The test name this criterion cites is also corrected in place, from the non-existent `SetupTests.PrepareRejectsInvalidNnWindow` to the two real facts, `PrepareRejectsInvalidNnWindowWhenNnMaxIsNotPositive` and `.PrepareRejectsInvalidNnWindowWhenNnMinIsNotBelowNnMax` (AGENTS.md §8, item 9 of the D6 review).

**Original text:**

> (found by the audit of this node: `InvalidCoefficients` combined four independent clauses — `Ak1 > 0`, `Ak2 > 1`, `Ak3 > 0`, `Ak3 < Ak4` — behind one test, `Ak2 > 1` untested even though `SmallParticles`' own indexing rests on it).

<a id="pdoksmall-length-criterion-narration-2026-09-26"></a>
## 2026-09-26 — the `Pdoksmall` length criterion's own archive framing and elimination argument

**Section of `BOOT.md`:** "## Acceptance criteria", the `CycleReport.Pdoksmall` length criterion. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the tick, the place, the `Ndok − 1` length, the category-merge threshold confirmation and its test name) did not move.

**Original text:**

> `CycleReport.Pdoksmall` is `Ndok − 1` elements, matching the archived `pdoksmall` of every reference formulation, not the `Ndok`-element array `nextPdoksmall` needs for the model loop; the trim moves no retained cell's value (`tests/Statistics.Tests/PdoksmallReportLengthTests`, ten cases over the five reference formulations). The category-merge threshold defect ("## Defects of the original", Fortran 911–959) is confirmed, not merely declared, as the mechanism behind an external measurement of two reference formulations' own row counts, by elimination against this node's own merge algorithm and by a constructed sensitivity demonstration (`tests/Statistics.Tests/MergeThresholdSensitivityTests.EpsydokThresholdFlipsWithinOneBinary32UlpOfTheBoundary`); the two formulations' own totals are not reproduced here (this node has no access to the original's internal `Qdoks`/`Dokp41`/`Dokp31`, only its printed output).

<a id="binary32-totality-criterion-narration-2026-09-26"></a>
## 2026-09-26 — the binary32-totality criterion's own fix scope, edge-class enumeration and mutation-proof narrative

**Section of `BOOT.md`:** "## Acceptance criteria", the `Binary32.ToNearestRepresentable`/`Binary32.Multiply` criterion (2026-09-25). Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (the tick, the date, `Binary32EquivalenceTests`, the sentence "The binary32 rounding is total.", and the deciding figures) did not move. The edge-class enumeration and the mutation proof are owned by `tests/Statistics.Tests/BOOT.md`'s own "## Mutations", not restated in either place.

**Original text:**

> `Binary32.ToNearestRepresentable` and `Binary32.Multiply` (followed by one more rounding step) equal the hardware cast oracle `(double)(float)value` bit for bit over the *whole* `double` domain, not only the range this node's own real values ever reach — closing D3 of the architecture audit of 2026-09-24 ("the tree rounds to binary32 in more than one place ... the emulation has a domain it does not handle"). The audit's domain was real: measured before the fix, binary32 subnormal-range and overflow-range doubles diverged from the oracle systematically (100% of a several-thousand-case sweep in each domain), exactly where `ToNearestRepresentable`'s own doc comment used to disclaim them. **The binary32 rounding is total.** The fix stays inside that one method (denormalized rounding below the smallest normal binary32 exponent, saturation to infinity past the largest one, both folded into the same ties-to-even rule the already-correct normal path used; no new public member, no `float` anywhere, `Multiply`/`TruncatedQuotient`/`TruncatedQuotientOfProduct` untouched — the two truncated-quotient helpers model the x87 FPU's own unstored extended intermediate, not a REAL*4 store, and are not hardware-cast-equivalent by their own doc comments, confirmed against their existing boundary tests rather than a rounding oracle that does not apply to them). Re-measured after the fix: ten million draws spread over the whole finite, normal `double` exponent range (every biased exponent, not only magnitudes near `[0, 1)`), every edge class the task asked for (zeros of both signs, the smallest/largest binary32 subnormals, the normal/subnormal boundary, ties at the subnormal, normal and overflow-to-infinity boundaries proven both ways, the double adjacent to each tie, the largest finite float, outright overflow, `NaN`/both infinities and the negative mirror of every finite case), and one million `Multiply`-then-round draws against a true single-precision multiply, all agree bit for bit; two positive controls (the overflow and underflow ties) reasoned from the ties-to-even rule by hand, independent of the oracle, confirm the same answers (`tests/Statistics.Tests/Binary32EquivalenceTests`). Proven non-degenerate by reverting the ties-to-even parity check to always-round-up on a tie: seven checks in that file failed, and so did the pre-existing `Binary32Tests.ToNearestRepresentableMatchesTheFloatCastOracle` (`tests/Statistics.Tests/BOOT.md`, "## Mutations"); reverted, green again.

<a id="fraction-share-citation-correction-2026-09-23"></a>
## 2026-09-26 (moved, dated 2026-09-23) — the fraction-share citation's own line-range correction, told in full

**Section of `BOOT.md`:** "## Acceptance criteria", the fraction-share normalization defect-row criterion. Moved to make room under the AGENTS.md §15 leaf limit (audit item D6); the current truth (was "792–797, 1712–1714", now "792–797, 1704, 1716, 1723, 1725, 1727") is restated as a one-line pointer in `BOOT.md`.

**Original text:**

> was "792–797, 1712–1714", now "792–797, 1704, 1716, 1723, 1725, 1727" — the citation named the assignment's own line range by its containing `IF`/`GO TO` block rather than the statements themselves; found while implementing "## Setup plane" against the source directly.

<a id="section-15-deviation-lifted-2026-09-26"></a>
## 2026-09-26 — the §15 declared deviation, lifted

**Section of `BOOT.md`:** "## Constraints", the closing paragraph. Deleted per AGENTS.md §13 ("a perpetually red check is worse than an absent one" — likewise, a declared deviation that no longer applies guards nothing) once audit item D6 brought the document's counted non-blank lines from 573 to inside the AGENTS.md §15 400-line leaf limit, by moving stale narration to this file (the entries above) and rewrapping the surviving prose; no current-truth content was removed to reach the count.

**Original text:**

> Declared deviation, §15: this document is over the 400-non-blank-line limit AGENTS.md §15 sets for a leaf node. The §6 deviation above already excludes `## Line map`, `## Categories`, `## Report` and `## Defects of the original`; every ⚠ correction in the sections that count (`## Invariants`, `## Dependencies`, `## Constraints`, `## Acceptance criteria`) is already a dated one-line pointer into `HISTORY.md`, not full text, and the checkbox criteria of `## Acceptance criteria` are the current truth §15 forbids moving. What remains is either that protected current truth or `## Setup plane`'s own design rationale (why the precision kind reproduces the setup plane whole rather than in pieces, why `Dmin`/`Di`/`Dj` round the way they do) — background a future reader needs to not repeat the two rejected throwaway builds, not stale material with a superseded reading. Today's D7 and S-2 corrections (`## Constraints`, `## Invariants`) add real, just-found content on top of an already-trimmed document. Lifts when a design session decides which of `## Setup plane`'s rationale can move to `HISTORY.md` without losing it, or moves part of this node's content to a child node of its own.


<a id="ndok-archive-test-typed-literals-audit-2026-09-18"></a>
## 2026-09-23 (moved, dated 2026-09-18) — the `Ndok` archive test's own typed-literal fix

**Section of `BOOT.md`:** "## Acceptance criteria", the `Ndok` archive-length
criterion's own note. Moved to make room under the §15 line limit for the "`Di`/`Dj`
round too" addition to "## Setup plane"; the current truth (the criterion itself, its
own tick, date and place, and the closing sentence "the original wording's plural …
overstated what is proven here") did not move.

**Original text:**

> the test named here typed the five printed lengths as `InlineData` literals instead
> of reading them from the archive, even though this criterion's own wording already
> claimed "via the printed `fqdokkarm` category rows" — a root-taboo violation ("no
> expected value typed into a test when it exists in a fixture file") the claim itself
> did not match. The test now parses each formulation's own archived `results.m`
> (`rc166.m.txt`, `rt56.m.txt`, `rcspx01.m.txt`, `r_p18050.m.txt`, `rps01.m.txt`)
> through `Harness.ResultsMFile` and asserts against `fqdokkarm(1,:)`'s own length;
> only the formulation name and its archive filename are inline. All five still equal
> 71, 71, 23, 20, 62 — the correction fixed how the test proves the number, not the
> number itself.

<a id="accumulation-consequence-verdict-history-2026-09-21"></a>
## 2026-09-23 (moved, dated 2026-09-21) — the "carried through" criterion's own verdict, corrected three times in one day

**Section of `BOOT.md`:** "## Acceptance criteria", the `Allvdok`/`Vdokstr`/`VdokTotal`/
`VdokTotal2` row's own three notes. Moved to make room under the §15 line limit for the
`fineoxy_fr`/`Dmin` addition to "## Setup plane"; the current truth (the criterion
stays ticked, and the pointer above) did not move.

**Original text, first correction:**

> "carried through … to every printed quantity each reaches" overstated what was
> measured. A completeness audit found `Allvdok` also reaches `pdoksmall` through
> `gdokleft`, a path this criterion's own twenty cases never exercised — put against a
> band width for `fmdok` and `Dok43all[1]` only. Left ticked, since the measured
> majority of the claim stands and the gap is a named, computable follow-up, not a
> refutation → `## Report`, "Completeness audit of this chain".

**Original text, second correction, later the same day:**

> the gap above is closed. Ten more cases (two new theories, five formulations each)
> put the `gdokleft` path against `Pdoksmall` directly: 0% of band on all five
> formulations, and a synthetic-shape proof rules out a broken wiring as the reason.
> "Carried through … to every printed quantity each reaches" now holds without
> qualification for `Allvdok` and `Vdokstr`; `VdokTotal`/`VdokTotal2` remain proven not
> to reach `pdoksmall`/`fmdok`/`Dok43all[1]` at all →
> `HISTORY.md#gdokleft-allvdok-reachable-cell-bound`.

**Original text, third correction, later still:**

> the thirty cases above used `Particle`'s 2026-09-20 worst-cell figures, since
> withdrawn there as a lower bound wearing the clothes of a measurement, one binary32
> rounding per `Attempt.Run` call rather than the original's own one per drawn particle
> (`src/Particle/BOOT.md`, "## Accumulators"). Re-run against `Particle`'s 2026-09-21
> re-measurement (`WorstCellRelativeError` updated in place, same thirty cases, same
> method, same assertions): every case still passes — the assertions check that a
> perturbation moves the derivation and, for the gdokleft theories, non-degeneracy,
> never a specific ratio — so "carried through … to every printed quantity each
> reaches, and put against that quantity's own band width" still holds exactly as
> proven. Only the *verdicts* the resulting ratios support move: from ruling the
> REAL*4-accumulation mechanism out on four of five formulations to finding it large
> enough to matter on two (HPEPA3, HMX) and in part on a third (PSAN02n) → `## Report`,
> "REAL*4 accumulation, carried through", its own dated correction and
> `HISTORY.md#accumulation-consequence-worst-cell-2026-09-20-superseded`.

<a id="nnmin-nnmax-precondition-discovery-2026-09-20"></a>
## 2026-09-23 (moved, dated 2026-09-20) — the `NnMin`/`NnMax` precondition, how it was found missing

**Section of `BOOT.md`:** "## Constraints", the precondition list's own note. Moved to
make room under the §15 line limit for the `fineoxy_fr`/`Dmin` addition to "## Setup
plane"; the current truth (the precondition itself, in the list above) did not move.

**Original text:**

> this precondition list did not cover `NnMin`/`NnMax` until a coder writing
> `tests/Cli.Tests` reported that `Simulator.Run` with `ModelParameters.Default with {
> NnMax = 0.0 }` "kills the whole process". Reproduced directly against
> `Simulation.Simulator` (`ExecutionMode.Batched`, every other option at its default,
> `SmallFormulation.Build(particlesPerCycle: 1, cycles: 1)`): no crash of any kind — the
> run finishes in 4 minutes 2 seconds with `SimulationFailedException(RunStatus.
> AttemptCapExceeded)`, the CPU pegged at 100% of one core the whole time
> (`Get-Process`'s own `CPU`/`WorkingSet64` sampled twice during the run). `NnMax ≤ 0`
> makes `nn ≥ setup.NnMax` (`Attempt.Run`, line 735) true for every finite,
> non-negative `nn`, and the one case it is not (`nn` is `NaN`, `ipocket_loc =
> ibridge_loc = 0`) already fails conditions 6 and 7 (lines 723, 727) instead — so
> every attempt of cycle ≥ 1 restarts after the loop, and the run only stops once every
> particle exhausts `SimulationOptions.MaxAttemptsPerParticle` (default 500,000, `src/
> Simulation/BOOT.md`, "## Budget measurement"), each attempt paying the full cost of
> the neighbour and bridge loops before being discarded. A real formulation's thousands
> of particles would take about as long as this one particle did (batched mode
> launches them together and none is ever accepted, so the launch count — the budget
> divided by `AttemptsPerLaunch` — does not fall with more particles), long past most
> test or CI timeouts: what reaches the person driving the library reads as "the
> process was killed", even though the library itself never left a value unstated. The
> fix is the precondition above, checked before any particle runs, exactly where
> `0 < GGG < 1` already is: `SetupTests.PrepareRejectsInvalidNnWindow`
> (`tests/Statistics.Tests`).

<a id="sizelaw-sample-dependency-restated-2026-09-18"></a>
## 2026-09-23 (moved, dated 2026-09-18) — the `SizeLaw.Sample` dependency, restated against the published member

**Section of `BOOT.md`:** "## Dependencies", the `Particle` row's own note. Moved for
the same reason as the entry above.

**Original text:**

> this line previously read "`SizeLaw.Sample` for `Dmax` (line 1754)" while
> `Particle/API.md` had not yet published `Sample` (AGENTS.md §3: a dependency on an
> undeclared member of a neighbour). Found by the audit of this node together with
> blocker 3 (ILGPU leaving `Statistics`): `Particle` now publishes `Sample`
> (`Particle/API.md`, "## Size law", its own dated note), and the call itself moved to
> `Simulation`, the driver that owns the accelerator — restated against the published
> member and the new two-phase shape rather than the old direct call.

<a id="pockets-guard-removed-2026-09-18"></a>
## 2026-09-23 (moved, dated 2026-09-18, audit of this node) — the `Qks1` guard `CycleStatistics.Pockets` should never have carried

**Section of `BOOT.md`:** "## Invariants", the "IEEE results" invariant's own note.
Moved for the same reason as the entry above.

**Original text:**

> `CycleStatistics.Pockets` carried a `qkssTotal == 0 ? 0.0 : ...` guard on `Qks1` that
> this invariant's own "No guard is added" already forbade. Found by the audit of this
> node; the guard is removed
> (`CycleStatisticsTests.ComputeGivesNaNPocketHistogramWhenQksIsAllZero`).

<a id="cycle-statistics-criterion-split-2026-09-18"></a>
## 2026-09-23 (moved, dated 2026-09-18) — the `CycleStatistics` criterion split into three

**Section of `BOOT.md`:** "## Acceptance criteria", the `Compute`-pipeline row's own
first note. Moved to make room under the §15 line limit for "## Setup plane"; the
current truth (the three split criteria themselves) did not move.

**Original text:**

> this criterion and the two above it were one criterion, "every report value and
> every in-place rewrite [of `Compute`] equals the value computed from the Fortran
> formulas by a script in `tests/Fixtures`". Split once `Categories` (its own class
> per this document's line map, called by `Compute`) turned out to be the one piece
> that actually needed the formula-script treatment (the branchy merge/shift/last-row
> logic BOOT.md's own "## Categories" spells out); the other seven pieces were called
> each a short, branch-free accumulation formula, and writing a second Python
> transcription of every one of them was not done that wave. AGENTS.md §6: a criterion
> half met is not one criterion.

<a id="small-particles-branches-note-2026-09-18"></a>
## 2026-09-23 (moved, dated 2026-09-18, audit of this node) — `SmallParticles`' own genuine branches

**Section of `BOOT.md`:** "## Acceptance criteria", the `Compute`-pipeline row's own
second note. Moved for the same reason as the entry above.

**Original text:**

> "short, branch-free" was false for `SmallParticles.Probability` and `MaxSize`
> specifically — the `zdoksmall > 1e-5` guard, the monotone clamp and `MaxSize`'s
> early-exit loop are genuine branches, and nothing asserted a single value of
> `pdoksmall` or `Dmaxxx` against an independent source even though they are this
> node's only two outputs that feed back into the model loop. Split into its own row
> above, now scripted; the seven remaining pieces (`GeneratorAccuracy`, `OxidizerSizes`,
> `Pockets`, `Matrix`, `CorrectedPockets`, `MassFractions`, `Convergence`) are the ones
> the "short, branch-free" description actually fits, checked so far only as the row
> below states.

<a id="accumulation-consequence-worst-cell-2026-09-20-superseded"></a>
## 2026-09-21 — the "REAL*4 accumulation, carried through" table, its reading and the completeness audit's closing paragraph, at the withdrawn 2026-09-20 epsilon (superseded)

**Section of `BOOT.md`:** "## Report", "REAL*4 accumulation, carried through" (table
and the "Reading it" paragraph that followed it) and "Completeness audit of this
chain" (closing paragraph).

**Why superseded.** Every ratio below was computed from `src/Particle/BOOT.md`'s
2026-09-20 worst-cell relative errors (`Allvdok`/`Vdokstr` up to 1.9%/HMX), which that
node withdrew the same day the correction landing here was made: they rounded the
binary32 shadow once per `Attempt.Run` call, where the original rounds once per drawn
particle — of order 69 additions into one cell per attempt on HPEPA3 — so the figure
was a lower bound wearing the clothes of a measurement, true for five of its eight
fields by that node's own remark. `src/Particle/BOOT.md`'s 2026-09-21 re-measurement at
the original's own granularity (worst-cell error 46–65% for `Allvdok`/`Vdokstr` on
HPEPA3/HMX) is not an incremental refinement of the numbers below; it changes which
formulations the REAL*4-accumulation story can explain.

**The superseded table** (`epsilon` from `Particle`'s 2026-09-20 table:
`WorstCellRelativeError` before this correction):

| Formulation | `fmdok` (← Allvdok) | `Dok43all[1]` (← Allvdok) | `pdoksmall` (← Vdokstr) | `pdoksmall` (← Allvdok, via gdokleft) |
|---|---|---|---|---|
| HPEPA3 | 8.4% of band — inconclusive | 1.8% — rules out | 1.8% — rules out | 0% — rules out |
| inpt | 0.08% — rules out | 8.4% — rules out | channel closed (see above) | 0% — rules out |
| P33 | 0.09% — rules out | 0.03% — rules out | 0.1% — rules out | 0% — rules out |
| PSAN02n | 0.7% — rules out | 0.05% — rules out | reference array entirely below the 1e-30 exclusion floor, nothing to explain | 0% — rules out |
| HMX | 93.2% — large enough to matter | 3.3% — rules out | 22.6% — inconclusive | 0% — rules out |

**The superseded "Reading it" paragraph, in full:**

> Reading it the way `src/Particle/BOOT.md` reads its own table: of the twenty cells
> in this table, two (`pdoksmall` via Vdokstr, `inpt` and `PSAN02n`) carry no ratio at
> all — the channel is shut or there is nothing to compare — and of the remaining
> eighteen, fifteen rule the REAL*4 story out (the ten already counted plus all five
> of the new `pdoksmall` via Allvdok/gdokleft column), one (HMX's `fmdok`) is large
> enough to matter, and two (HPEPA3's `fmdok`, HMX's `pdoksmall` via Vdokstr) are
> inconclusive. It does not come close to explaining the bulk of the 129 failing
> `pdoksmall` cells the root criterion counts: four of five formulations rule it out
> outright or close the channel entirely, and the fifth (HMX) is inconclusive on the
> one cell measured, not confirmed. The same holds for `fmdok`'s 20 failing cells and
> `Dok43all[1]`'s 3: one formulation (HMX) plausibly explained for `fmdok`, everything
> else ruled out or inconclusive. As with `Fmkarm`/`Fmkarm2` in `Particle`'s own table,
> most of what this criterion flags on these three quantities is not this mechanism.

**The superseded completeness-audit closing paragraph, in full:**

> This closes the last open corner of "REAL*4 accumulation, carried through": every
> component of the chain from `Allvdok`/`Vdokstr` to `pdoksmall`/`fmdok`/`Dok43all` is
> now measured, and the REAL*4-summation candidate is dead on complete evidence
> everywhere in this chain except the two cells the table above already keeps as
> survivors (HMX's `fmdok`, large enough to matter; HPEPA3's `fmdok` and HMX's
> `pdoksmall` via `Vdokstr`, inconclusive).

**What replaces them:** `BOOT.md`, "## Report", "REAL*4 accumulation, carried
through" — the re-measured table, reading and completeness-audit conclusion, dated
2026-09-21.

<a id="gdokleft-allvdok-reachable-cell-bound"></a>
## 2026-09-21 — the gdokleft/Allvdok path to pdoksmall: reachable-cell bound (measurement)

**Section of `BOOT.md`:** "## Report", "Completeness audit of this chain".

**Task.** The 2026-09-21 completeness audit (same file, earlier the same day) found
`Allvdok` reaches `pdoksmall` through `gdokleft`'s internal `FMDOK` recurrence, and
that this node's own prose called it "a small additive term" without a computed
bound — an unproven absolute (AGENTS.md §8). The bound was left as a follow-up,
computable from `AccumulationConsequenceTests.AllvdokErrorPropagatesToAllvdoksoAndAlldok432`'s
own perturbation without new production code or a new fixture.

**Method.** `CycleStatistics.Matrix`'s own `fmdokIndex = int(Dmin/Di)` reads
`gdokleft = fmdok[fmdokIndex]·GGG + gdoksfr`, and `fmdok[fmdokIndex] = Σ allvdokso[0 ..
fmdokIndex - 1]` (0-based; `OxidizerSizes`'s own recurrence, `fmdok[0] = 0`). Only
cells strictly below `fmdokIndex` ever enter that sum, so the channel's own reachable
window is `[0, fmdokIndex)`, not the whole `Ndok`-length array
`AllvdokErrorPropagatesToAllvdoksoAndAlldok432` perturbs at its own global worst cell.
A new theory, `AccumulationConsequenceTests.AllvdokErrorPropagatesToPdoksmallThroughGdokleft`,
reuses that test's own scaffolding (`BuildScenario`, `BuildTotals`, `Compute`,
`ReadBandWidth`, `Classify` — no new perturbation machinery) but perturbs the largest
cell *inside* the reachable window instead of the shape's own global maximum, then
reads the resulting delta straight off `CycleReport.Pdoksmall`, already returned by
that same `Compute` call and previously unread for this purpose.

**Measured, per reference formulation** (`ModelParameters.Default`, the parameters
every scenario in this file already uses): `fmdokIndex = int(Dmin/Di) = 1` on **all
five** reference formulations (`Dmin = 10 μm` is the original's own default, shared by
every formulation; each formulation's own `Di` happens to make the quotient's integer
part exactly 1). The reachable window is therefore `[0, 0]` — cell 0 alone — on every
formulation, and every formulation's own real Allvdok shape (its own published
"fmdok"/`Allvdokso`, the same proxy `AllvdokErrorPropagatesToAllvdoksoAndAlldok432`
already uses) is exactly zero at cell 0. A multiplicative perturbation of an exact
zero stays zero (the same shape `VdokstrErrorPropagatesToPdoksmall`'s own doc comment
already records for its own window), so the propagated delta to every printed
`Pdoksmall` cell is bit-for-bit `0.0` on all five formulations — a ratio of exactly
0% against any band width, decisively below the 5% "rules out" line
(`AccumulationConsequenceTests.Classify`, the same convention every cell of the
"REAL*4 accumulation, carried through" table already uses: below 5% of band rules the
mechanism out, above 50% it is large enough to matter, between is inconclusive).

**Non-degeneracy (AGENTS.md §13).** A delta of exactly zero on every formulation could
mean either "no draws in the reachable cell" (a fact about the data) or "the
`gdokleft`/`FMDOK` wiring is disconnected" (a bug this check would then fail to
catch). A second theory,
`AllvdokErrorPropagationThroughGdokleftIsWiredWhenTheReachableCellIsNonzero`, forces
both `Allvdok`'s and `Vdokstr`'s shapes uniform (nonzero at cell 0) and repeats the
same perturbation of cell 0. `gdokleft` itself moves on every formulation under this
substitution (baseline → perturbed, absolute): HPEPA3 `0.017666666666666667 →
0.017699214287662225` (Δ ≈ 3.25e-5), inpt `0.017777777777777778 →
0.017778158024458986` (Δ ≈ 3.80e-7), P33 `0.019393939393939394 →
0.01939471044899611` (Δ ≈ 7.71e-7), PSAN02n `0.020636363636363637 →
0.020638764949945636` (Δ ≈ 2.40e-6), HMX `0.009014084507042254 →
0.0091828947183148161` (Δ ≈ 1.69e-4) — and every one of those changes propagates to a
nonzero `Pdoksmall` delta, proving the wiring itself carries a real, nonzero effect
when the reachable cell is genuinely populated.

The first attempt at this proof used a uniform `Allvdok` shape alone (`Vdokstr` left
at its own formulation's real shape) and failed on `inpt` and `PSAN02n`: both
formulations' own real `Vdokstr` shape is zero throughout the window
`SmallParticles.Probability`'s own `zdoksmall` ever reads for any `kilo` (`inpt`'s own
first nonzero cell, 25, already measured past its own reachable bound,
`floor(Ndok/Ak2) = 18`, in `VdokstrErrorPropagatesToPdoksmall`; `PSAN02n`'s own
`Vdokstr` array is the same one already measured "entirely below the 1e-30 exclusion
floor" there). `vdoksmall[k] = zdoksmall[k] / (1 - GGG + gdokleft + zdoksmall[k]) ·
Pl / OxidizerDensity` (`SmallParticles.cs`) carries `gdokleft` only inside a
denominator that a numerator factor of `zdoksmall[k] = 0` already zeroes out
entirely — a real, independent structural fact about these two formulations' own
Vdokstr data (already established for the sibling channel, not a new gap), not a
defect of the wiring this proof exists to check. Forcing `Vdokstr` uniform too, so
`zdoksmall[k] > 0` for every `k`, isolates the `gdokleft` wiring from that unrelated
zero and all five formulations pass.

**Conclusion, folded into `BOOT.md`'s own table and defect row:** the
`gdokleft`/`Allvdok` path to `pdoksmall` rules the REAL*4-summation story out at 0% of
band on every one of the five reference formulations, not because the channel is
unreachable or the wiring is broken (both checked directly above) but because every
formulation's own real Allvdok shape carries no mass in the one cell this specific
channel can reach. This was the last unquantified corner of the chain from Allvdok/
Vdokstr to pdoksmall/fmdok/Dok43all; with it closed, the REAL*4-summation candidate
for the pdoksmall/fmdok/Dok43all failures is dead on complete evidence everywhere
except the two cells already measured to survive it (HMX's `fmdok`, large enough to
matter; HPEPA3's `fmdok` and HMX's `pdoksmall` via `Vdokstr`, inconclusive) — see
`BOOT.md`, "## Report", the closing sentence of "REAL*4 accumulation, carried
through".
