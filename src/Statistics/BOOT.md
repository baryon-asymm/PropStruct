# BOOT.md — Statistics

## Purpose

The host-side arithmetic of the original around the particle loop: the setup before the
first cycle (Fortran lines 267–277, 376–406 and subroutine `PARAM`, lines 1695–1757) and
the processing at the end of every cycle (lines 771–1175). From the run's totals it
computes generator accuracies, mass-medium sizes and their errors, the conditional size
distributions by pocket category with their iterative merging, pocket distribution
moments, matrix densities, the agglomerate coefficient, `pdoksmall` and `Dmaxxx` for the
next cycle, corrected pocket sizes, bridge sizes, the three variants of the pocket mass
fraction and the per-cycle convergence values. It runs once per cycle on the host, so it
is apart from the kernel-compatible `Particle`.

Line numbers refer to the original source `PropStructv3.for`.

## Invariants

- **No state between calls.** `Prepare` and `Compute` take everything through
  parameters; the only memory of the run is its totals, owned by the caller.
- **The setup follows the precision kind** (decided 2026-09-23). Under `Original` it
  reproduces the Fortran's REAL*4 storage of the setup plane, exactly the generated set
  ("## Setup plane"); under `Binary64` it is bit-identical to what it computed before,
  except `SetupEchoes.Ddokmax` ("## Setup plane", "review"): its stored value moved at
  the eighth significant digit when the 2026-09-24 review corrected it to read the same
  store every other fraction bound goes through, with the five reference formulations
  still bit-identical at seed 0 on every other cell of `results.m`.

  ⚠ 2026-09-26: was this invariant's `Binary64` half stated without exception, a
  hidden deviation (AGENTS.md §12) → HISTORY.md#binary64-half-hidden-deviation-2026-09-26
- **The per-cycle plane follows the precision kind** (decided 2026-09-27, implemented
  2026-10-01, re-implemented from the listing 2026-10-03). Under `Original` `Compute`
  does what the executable's listing does at every site of
  `CyclePlane.listing.generated.txt`, each rounding through `CyclePlaneRounding`
  ("## Report", "The per-cycle plane from the executable's listing"); under `Binary64`
  it is bit-identical to before.

  ⚠ 2026-10-03: was the sites of the source-read table `CyclePlane.generated.txt`, now
  the executable's listing, implemented →
  HISTORY.md#per-cycle-plane-source-read-rules-2026-10-03
- **In-place rewrites of the totals, as the original.** `Compute` rewrites, and later
  cycles add to the rewritten values: the category merge and shift (lines 911–958) on
  `Qdoks`, `Dokp41`, `Dokp31`, without clearing the old last row after any merge (its
  contents then count twice once later cycles rebuild the rows); and in cycles ≥ 1
  `VdokTotal` and `VdokTotal2` divided by `1 − gdokleft/GGG` (lines 1145, 1150). No
  other total is written.
- **The merge loop of the original, literally.** After every pass that merged a pair,
  all category statistics are recomputed (label 600); the final check of the last row
  (lines 947–959) merges without recomputing, so the new last row keeps the statistics
  of before the merge.
- **Every computed statistic printed is in the report.** Every statistic that
  `results.m` or the screen summary prints comes from the report, in SI units, with
  normalized distributions as fractions per cell. The other printed items are inputs,
  cycle bookkeeping (`Simulation`), or print-time arithmetic that `Output` owns:
  `DOKSD**0.5` (1279), `gdokns + gdokleft/GGG` (1286), divisions by `FI` and `FI + N`
  (1242–1256, 1500–1504), relative deviations (1301, 1305, 1309), `1 − DolM` (1318,
  1321, 1492), products with `mp` (1328–1331, 1495–1497), `(EPS1+…+EPS6)/6` (1470), µm
  scaling.
- **IEEE results where the original computes them**, for example accuracies of streams
  not drawn in cycle 0 (`NFQ = NFW = 0`), empty corrected distributions, `DolM2` or
  `DolM3` with no committed Var#1 or Var#2 bridge (0/0), a negative radicand under
  `**0.5` (`DOKSD` of P2.dat is negative), an all-empty pocket histogram (`QKSS = 0`
  makes every `Qks1` cell `0.0 / 0.0`): the same `NaN` or `∞` as the original, which
  runs without floating-point traps. No guard is added.

  ⚠ 2026-09-18: was a `qkssTotal == 0 ? 0.0 : ...` guard on `Qks1` this invariant's own
  "No guard is added" already forbade, now removed → HISTORY.md#pockets-guard-removed-2026-09-18
- **Double precision, literals as written** under `Binary64`; under `Original` the
  setup plane's literal expressions and the per-cycle plane's inexact literals and
  all-literal subexpressions are binary32 (root BOOT.md, "Fidelity to the original";
  "## Setup plane"; "## Report", R5): `3/3.14` and `24/3.14` (lines 1704, 1716),
  `0.8` (line 403), `3.14159`, `0.016`, `0.027`, `2000.`, `3000.`, `0.75`, `0.3333`
  (lines 1014–1016), `1e-30` (lines 856, 869–870, 888, 902), `1e-5` (line 1034), `0.001`
  (line 1110), `0.01` (lines 1111–1112).

  ⚠ 2026-09-26: was this invariant stated without exception, false since 2026-09-23 for
  `3/3.14`/`24/3.14`/`0.8` → HISTORY.md#literals-invariant-exception-2026-09-26

  ⚠ 2026-10-01: was "under either kind, except the setup plane's own literal
  expressions", now the per-cycle plane's literals binary32 under `Original` too.
- **The cell centre first** (decided 2026-10-02). Every moment term over histogram
  cells weights the centre `c = Di·(k − ½)`, formed first: `x·c`, and `(x·c)·c` for a
  second moment, extending `## Categories`' `c_i` to the report; every other product
  runs as written. The contract, with its lines and its reason: `API.md`, "## Cycle".

  ⚠ 2026-10-02: the rule's premise in `API.md`, "the original carries it in 80-bit
  registers, so neither order is the original's", is refuted: the executable multiplies
  in the written order at 805, 806, 989, 990 and 991, at 53-bit precision. The rule is
  unchanged; whether to follow the executable's order is a decision for a later session
  (`API.md`, "## Cycle", the note of the same date).

  ⚠ 2026-10-02, later: decided, under `Original` only and not yet implemented: the
  listing's order replaces the rule (`API.md`, "## Cycle"); `Binary64` keeps it.
- **Array sizes from binary32 values.** `Ddokmax`, `AK4`, `Di` and `Dj` are REAL*4 in
  the original: a value read from the `.dat` or the menu is rounded to binary32, and a
  micrometre value is converted by a binary32 product with the binary32 literal `1e-6`
  (lines 261–266). `Ndok`, `Nkarm`, `Ncat` are the exact quotients of those binary32
  values (`Ddokmax/Di`, `Ddokmax·AK4/Di`, `Ddokmax·AK4/Dj`) truncated, plus 2, computed
  with integer mantissa arithmetic (no `float` type); the same treatment covers
  `Categories.cs`'s own `DPRow` (line 825) and `SmallParticles.cs`'s own
  whole-fraction count `int(real(kilo)/ak2)` (root BOOT.md, "Double precision only",
  decided 2026-09-18), through `Binary32.TruncatedQuotient`, the helper `Ndok` itself
  uses (`Nkarm`/`Ncat` use a different helper, `Binary32.TruncatedQuotientOfProduct`,
  S-2 below). Evidence and measured margins for every reference and archived formulation
  are in "## Defects of the original" below (its own array-sizes row) and root BOOT.md's
  own table, not retold here; `DPmax`'s own reference-mode measurement is still ⏳ (root
  acceptance criteria, the L2 row of `tests/Statistics.Tests/BOOT.md`). Every other
  quantity stays `double`.

  ⚠ 2026-09-24 (S-2): was `Nkarm`/`Ncat` sized through `Binary32.TruncatedQuotient` like
  `Ndok`, now through `Binary32.TruncatedQuotientOfProduct`, which leaves the unstored
  product unrounded → HISTORY.md#nkarm-ncat-product-rounding-s2-2026-09-24

  ⚠ 2026-09-26: was the worst-case margin's own attribution told in full (which
  quotient, which formulations, evaluated at what theoretical maximum), now the bound
  alone → HISTORY.md#array-sizes-margin-attribution-2026-09-26

## Dependencies

- [Particle](../Particle/API.md) — the layouts of `ModelSetup` and the totals, and the
  published `SizeLaw.Sample` (`../Particle/API.md`, "## Size law"), whose `x`/`x1`
  parameters and `Dmax` result shape `Setup.Prepare`'s own `TailDraw` and
  `Setup.CompleteEchoes` (API.md, "Setup hand-off"); this node never calls `Sample`
  itself, only produces and consumes the values around it, because it never builds an
  `ArrayView` or stands up an accelerator (`Execution` is the only node that knows
  accelerators exist, root BOOT.md, "## Decomposition").

  ⚠ 2026-09-18: was "`SizeLaw.Sample` for `Dmax` (line 1754)", an undeclared dependency,
  now restated against the published member and the two-phase hand-off → HISTORY.md#sizelaw-sample-dependency-restated-2026-09-18
- [Input](../Input/API.md) — the formulation and the model parameters (`Length.Metres`,
  `Length.AsWritten` for the binary32 sizes).
- [legacy](../../tools/legacy/API.md) — `legacy_file`, through which the generator
  scripts reach the original's source and listing excerpt; no C# of this node depends
  on it.

Outside the tree: the original's source and listing excerpt, at `PROPSTRUCT_LEGACY_DIR`,
for the generator scripts only; dumpbin 14.12.25835.0, for regenerating the listing
excerpt only (`tests/Fixtures`, "## Cycle-plane listing"); Python 3.8+ for the generator
scripts.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Host code, double precision; allocation allowed.
- Preconditions, checked by `Prepare` and reported as a status (the original hangs or
  indexes out of bounds without them): `0 < AK1`; `1 < AK2` (the index of lines 1031 and
  1039 stays within `Ndok`); `0 < AK3 < AK4`; `Di > 0`, `Dj > 0`; `0 ≤ Dmin < Ddokmax`
  (line 478 rejects every base particle otherwise); `0 < lower < upper` for every
  fraction (lines 1704, 1716, 1769, and the index of lines 522–524); every fraction
  share `> 0` (a zero share makes `Dmax` `NaN` at line 1753 or leaves the fraction
  search empty); at least one pocket-forming fraction (line 473); a port-only
  precondition with no Fortran counterpart — `formulation .PocketFormingFractions`, when
  not `null`, holds at least as many flags as fractions (`DatFile.Read` always reads
  exactly `NMM` flags when it reads any, but `Input/API.md` makes no length promise for
  a `Formulation` built in code, and indexing past a short array would otherwise throw,
  forbidden by the root invariant "Failures are values"); `0 ≤ alfa < 1` (lines
  1744–1754); `0 < GGG < 1` after line 376 (`GGG = 0` makes `λ = 0` and no particle
  completes); `NnMax > 0` and `NnMin < NnMax` (lines 731, 735: `nn =
  ipocket_loc/ibridge_loc ≥ 0` always, so a window that does not intersect `[0, +∞)`
  makes every attempt of every cycle ≥ 1 end in `RestartedAfterLoop`, the same "no
  particle completes" failure the `GGG` precondition above already guards, just after
  the neighbour loop instead of before it).

  ⚠ 2026-09-20: was a precondition list missing `NnMin`/`NnMax`, found when
  `NnMax = 0.0` reproduced as a long, reported failure rather than a hang, now closed
  above → HISTORY.md#nnmin-nnmax-precondition-discovery-2026-09-20

  ⚠ 2026-09-24 (D7): was a `byte`-narrowed pocket-forming flag indexed without a length
  check, now the Fortran's own zero/nonzero test → HISTORY.md#pocket-forming-flag-d7-2026-09-24
- The size law is uniform in `D` when `JZZ = 2` and uniform in `1/D²` (mass-uniform) for
  any other value. `DOKSD` (lines 385–386, 399–400, 405) uses the same formula for both
  laws; only `DOKM` differs.
- Unit conversion from micrometres (lines 260–266) is `Input`'s; `Prepare` receives
  metres. Under the `Original` precision kind the setup plane reproduces that conversion
  itself, in binary32, from `Length.AsWritten` ("## Setup plane"); `Input`'s contract
  does not change, and under `Binary64` `Prepare` takes `Input`'s metres as before
  (decided 2026-09-23).
- The cycle bookkeeping of lines 1078–1082 and 1154–1181 (the cycle flag, `FI` = the
  number of base particles counted after cycle 0, which is `N` times the number of
  completed cycles ≥ 1, `KPRIS`, storing the convergence values per cycle) is
  `Simulation`'s; `Compute` receives the cycle index and returns the values.

⚠ Declared deviation, §6: the specification is the Fortran source by the line ranges of `## Line map`, for the same reason as in `Particle`, and, for what the per-cycle plane stores, keeps in registers and multiplies in which order (decided 2026-10-02), the executable's listing excerpt through `CyclePlaneListing.map.txt`; since 2026-10-04 both lie outside the repository (`tools/legacy`), so a reader of the public tree cannot consult them and every generator and check that reads them carries `Category=Legacy`, replaced by: ## Line map, ## Categories, ## Report, ## Defects of the original.

⚠ 2026-10-04: was the excerpt `tests/Fixtures/Legacy/PropStructV3.cycle-plane.listing.txt` in the tree and the site table's `fortran` column, now the excerpt outside it and the column dropped by stage S2 of the delivery → HISTORY.md#specification-out-of-tree-2026-10-04

This document fixes structure, the report and every departure from a literal
transcription. Lifted only if the model is specified independently of the original.

  ⚠ 2026-09-27: was a declared §15 deviation, now inside the leaf limit →
  HISTORY.md#section-15-lifted-note-2026-10-01

## Setup plane

Decided 2026-09-23 by the owner (root BOOT.md, "Precision kind"); implemented the same
day (`Setup.cs`, `FractionLaw.cs`, `ACCEPTANCE.md`).

Under `PrecisionKind.Original` — a parameter of `Prepare` (`API.md`, "## Setup ✅") —
every value this node's setup computes that the Fortran **stores in REAL*4** is computed
as the Fortran computes it. The setup is lines 267–277, 376–406 and 1695–1757, the
first-cycle ranges of `## Line map`; the per-cycle processing (771–1175) is outside this
decision.

- **Inputs as `READ` leaves them.** Each formulation value the setup reads is binary32
  of what the `.dat` says: a length from `Length.AsWritten`, a plain value from its
  `Formulation` field. A length the Fortran converts from micrometres (lines 260–266)
  is converted as it converts it: binary32(value) × binary32(1e-6), the product
  rounded to binary32. Measured 2026-09-23 on all 1075 input values of the 49 shipped
  `.dat` files: decimal → `double` → binary32 equals decimal → binary32 directly, so
  `AsWritten` loses nothing on the shipped data — a declared residual elsewhere.

  ⚠ 2026-09-27: was "caught by a check over the shipped files the day it bites"; no
  such check exists (`tests/`, `tools/`, `src/` searched); found by the D6 review.
- **Literal expressions folded in binary32.** An expression whose operands are all
  default REAL or INTEGER literals is folded by the compiler in binary32 before use —
  `3/3.14` and `24/3.14` in `Z(J)`, `1e-6` in the conversion. The fold is reproduced.
- **Sums as the listing stores them; every store to a REAL*4 variable rounded.**
  `ZSS` is `PARAM`'s `ZS` under another name (by reference, line 377); it is summed
  with a binary32 rounding after every addition (a six-fold unrolled loop of stores,
  0x4182B7–0x418311; the executed `PARAM` leaves the same bits on all 49 shipped files,
  one rounding after the loop would part from it on 16) and divides by its final total
  in a separate loop. `DOK4` and `DOK3` are stored every pass of the `JZZ = 2` loop,
  which the executable unrolls four-fold (0x408CFE–0x408F8E): `u − l` is a REAL*4
  temporary in pass 1 of a block and in every remainder pass and a register in passes
  2–4, `l**5` is stored to a REAL*4 temporary every pass and `u**5 − l**5` subtracts
  that stored value, and `DOKSD`'s sum is stored after passes 1–3 of a block and carried
  in the register after the fourth and through the remainder (`AnalyticSizes`,
  `UnrolledSchedule(n, 4)`). `DOKM` for any other `JZZ` is unrolled five-fold, stored
  after the fifth pass of a block and after every remainder pass (`UnrolledSchedule`);
  `DOKSD`'s sum there stays in the register and line 405 subtracts `DOKM**2` from it
  unrounded. Arithmetic between rounded operands runs in `double` and is rounded where
  the Fortran stores to REAL*4; the executable computes in 53 bits (`## Report`, the
  second ⚠ of 2026-10-02).

  ⚠ Declared deviation, root BOOT.md "Precision kind" ("generated, never typed by hand";
  AGENTS.md §12), decided 2026-10-03: lines 379–406 and `PARAM` lie outside the table's
  address scope, so what is typed, from a reading of the listing, is the two unroll
  factors (five for `DOKM`, `Setup.AnalyticSizes` and `DOKM_UNROLL` of
  `formulas_statistics.py`; four for the `JZZ = 2` loop, `JZZ2_BLOCK`) and the stores of
  both loops, `DOK4`/`DOK3`, `DOKSD`, the width and `l**5` among them (`JZZ2_RULES`),
  and the power expansions read with them, `**5.` and `**4.` in that loop (`jzz2_sums`)
  and `**4` in `PARAM`'s `Z(J)` (`FractionLaw.Build`); `SetupPlane.generated.txt` holds
  membership only. Why: generating them needs about forty rows, the temporaries' cells
  and two new site kinds, more than the correction was worth (`ACCEPTANCE.md` A8). What
  holds it: the executable's own pre-loop run, on the formulations where one store after
  the loop (the rule before) is a binary32 unit off, `BK10`, `KB397` and `AK157a`, and
  on `CSPX04`, where the loop's l**5 and `DOKSD` stores matter, pinned in the oracle's
  fixture (`ListingOracleTests`, `ListingOracleSetup.approved.txt`, empty); over all 49
  shipped files by the survey (`PreloopSurveyTests`, A13, empty: rule L turns four of
  them red, `C166` the fourth); and by 120 drawn `JZZ = 2` loops the oracle's self-test
  runs, where the listing's stores equal the executable's and each store turned off does
  not (A12). What lifts it: A8, the schedules generated; `PARAM`'s `**4` stays typed
  until its range, 0x4181A5–0x418577, joins A8's scope.

  ⚠ 2026-10-03: was the unroll factors and the stores typed, now the power expansions of that loop and of PARAM's Z(J) too (A10, A12)

  ⚠ 2026-10-03: was "loop-invariant sums once at the loop's exit (rule L, `DOKM`,
  `DOKSD`)", now the listing's schedules → HISTORY.md#setup-rule-l-sums-2026-10-03

  ⚠ 2026-10-03: was "`DOK4` and `DOK3` are stored every pass (`JZZ = 2`)", `DOKSD`
  never stored, the `JZZ = 2` loop on `CSPX04` "not reproduced, a known gap", and the
  walker said to need "a pointer kept in a frame slot"; now the four-fold loop's stores,
  reproduced, and the walker taking that slot (130 problems, none from the walk, with
  the scope extended) → HISTORY.md#setup-jzz2-loop-2026-10-03
- **REAL*8 stays `double`** — `Z`, `Z1` (`ZX`, `Z11`) and whatever else the declaration
  block makes REAL*8. `Z(J)` is formed in the executable's order: the denominator of
  the non-uniform law is `(l·l)·(u·u)`, rounded once, and `DOK**4` is `(x·x)·(x·x)`
  (`FractionLaw.Build` under `Original`; the order before, `l·l·u·u`, rounded twice and
  parted from the executed `PARAM` by 1–3 ulp in `ZX` on 33 of the 49 files and in `Z11`
  on 16; `ACCEPTANCE.md` A10).
- **Line 378 holds a compiler temporary** (decided 2026-10-02, implemented 2026-10-03,
  `ACCEPTANCE.md` A7). `sLamd = (GGG/PLOT1)*ZSS*PLOT2`: the executable divides once
  into a REAL*4 slot (0x408ABE), then forms `(ZSS·G)·PLOT2` (0x408AC4–0x408AD0), so
  under `Original` λ takes `G = binary32(GGG/PLOT1)` and that product order
  (`Setup.Prepare`, sites 408ABE and 408AD6 of `CyclePlane.listing.generated.txt`).
  The hoisted block that holds `G` also holds the per-cycle plane's `T1`–`T4` and
  `PLOTsm` ("## Report"). The listing oracle's pre-loop run is the evidence: it agrees
  with `Prepare` on every formulation (`ListingOracleSetup.approved.txt`, empty). λ
  moves on nine shipped `.dat` files and on none of the five reference formulations;
  `Binary64` is unchanged. The line map's row 378 states the formula, not this order.

**Why whole, not in pieces.** Two throwaway builds (2026-09-23, branch
`claude/fraction-table-experiment`, not adopted) rounded first `ZSS` alone, then also
the mass shares and bounds at `FractionLaw`'s boundary; each was a third program — its
table was the original's while its λ, `Dmax` and size draws were not. The last rung, the
literal fold, is what closes `inpt`'s `epsdokfr[0]` to the original's 2.70e-8.

⚠ 2026-09-26: was the two throwaway builds' own intermediate ladder and token counts
told in full, now the decision and the one figure that decided it → HISTORY.md#why-whole-not-in-pieces-ladder-2026-09-26

**`Dmin` rounds too; `Di`/`Dj` round too, both found 2026-09-23.** Neither has a
declaration in lines 4–64: REAL*4 by implicit typing, like `gdokns`, and converted from
micrometres at lines 264–266 exactly as `DDOK`'s own bounds are at 260–263
(`SetupPlane.generated.txt`'s `dmin`/`di`/`dj` rows, role `input`). `Prepare` rounded
the fraction bounds but passed `Dmin`, `CellSize` and `CategoryStep` through unrounded:
on HMX/HPEPA3/P33, whose default `Dmin` equals the default `Di`, this opened a one-ULP
sliver `Attempt`'s "Dok < Dmin" check (Fortran 478, 533) could land a draw in, and
separately binned a draw at the lowest fraction bound to `int(D/Di) = 0`, printing a
nonzero `fmdok[0]` (HMX 0.2–0.3e-10, HPEPA3 ≈3.7e-10) where the original always prints
zero. Fixed by rounding all three through the same `ToBinary32MetresStored` helper
(`SetupPlaneTests.DefaultDminRoundsToTheSameBinary32ValueAsTheLowestFractionBound`,
`.DefaultCellSizeRoundsToTheSameBinary32ValueAsDminSoTheLowestBoundBinsToCellOne`, three
formulations each, seen red on the violation before the fix, one binary32 ULP apart).

⚠ 2026-09-27: was `Dmin` and `Di`/`Dj` told as two paragraphs with their own live-run
controls, now one fused rule; the controls moved whole → HISTORY.md#dmin-di-dj-rounds-too-fused-2026-09-27

**Design decision, 2026-09-24 (owner and orchestrator): the setup plane's membership is
generated, and one stored setup feeds every consumer.**

⚠ 2026-09-26: was the two audits' own findings told here in prose, now folded entirely
into this pointer → HISTORY.md#design-decision-audit-findings-2026-09-26

- **Membership.** `classify-setup-plane.py` scans lines 259–277 (beyond `## Line map`'s
  267–277: the metre conversion, declared above), 376–406 and 1695–1757, the
  formulation reads and the menu, over the reader it shares with
  `classify-cycle-plane-listing.py`, `fortran_source.py`; mechanics in their docstrings.
- **Site map.** `SETUP_PLANE_NAMES` replaced by one hand-written site map, checked both
  ways as `Particle`'s is.
- **One stored setup.** `Setup.Prepare` sets the run's kind itself; `Simulator` no
  longer patches it. Every consumer — `Particle`'s size law and the tail draw included
  — receives the stored `SetupTables`/`ModelSetup`/`Dmax`/echo values as data, and its
  own arithmetic stays the attempt plane, `double` under either kind.
- **Output.** Reads the stored values only; no setup-plane member from `Formulation` or
  `ModelParameters`.

⚠ 2026-09-26: was the acceptance sub-list told in full here, now met and recorded in
`ACCEPTANCE.md`, the `Binary64`-unchanged criterion → HISTORY.md#design-decision-acceptance-sublist-2026-09-26

⚠ 2026-09-24: was the REAL*4 menu values excluded from the setup plane, first as a
"third program" risk and then as one side of a `double` comparison, now generated
members, since the root defines the plane by what is stored → HISTORY.md#menu-parameters-left-out-2026-09-23;
HISTORY.md#menu-parameters-superseded-2026-09-24

⚠ 2026-09-26: was `Ddokmax`'s own two assignment lines corrected here in place, now
moved: restated where it matters, in the review below → HISTORY.md#ddokmax-assignment-lines-correction-2026-09-26

⚠ 2026-09-24 (review): was `karmcoef = 12.345` and `Ddokmax` read from `Sizes`'s array
exception, now `karmcoef = 0.03` and read through `ToBinary32MetresStored` → HISTORY.md#setup-plane-review-corrections-2026-09-24

## Line map

This node's Fortran source is `tests/Fixtures/Legacy/PropStructv3.for.txt`, lines
267–277, 376–406, 771–1175 and 1695–1757 (the four ranges of the acceptance criteria
below; `tests/Statistics.Tests`' line-map coverage test reads this sentence, not the
table, for the ranges it scans, the same pattern `src/Particle/BOOT.md` uses).

| Fortran | C# member | Note |
|---|---|---|
| 267–277 | `Setup.Sizes` | `Ddokmax`, `Ndok`, `Nkarm`, `Ncat` from binary32 values (invariant "Array sizes from binary32 values"), `Nc = 1000` |
| 376 | `Setup.Prepare` | `GGG = GGG0 − gdokns·GGG0` |
| 1695–1734 | `FractionLaw.Build` | unnormalized `Z` by law, `ZSS`, normalized `ZX`, cumulative `Z11` with `Z11(NMM+1) = 1`; `Zerror` (1733) not ported |
| 377, 1735–1753, 1755–1757 | `FractionLaw.TryTailDraw` | the **modified** tail probability `alfa` (the original changes the caller's `alfa` in place; it is printed afterwards and gates `Dokb_max`) and the `x`/`x1` pair `Particle.SizeLaw.Sample` needs (`X = Z11(Imax+1) − alfa`, whose own fraction search may pick a fraction other than `Imax`); 1755 is the `if`/`else`'s own `end if`, 1756–1757 are `PARAM`'s `RETURN`/`END` |
| 1754 | — | `call SIZE`: the driver (`Simulation`, through `Particle.SizeLaw.Sample`, the published member — `../Particle/API.md`, "## Size law"), not this node; `Setup.Prepare` produces the `TailDraw` the call needs and `Setup.CompleteEchoes` consumes its `Dmax` result (API.md, "Setup hand-off") |
| 378 | `Setup.Prepare` | `λ = (GGG/PLOT1)·ZSS·PLOT2` |
| 379–406 | `Setup.AnalyticSizes` | `DOKM` by law; `DOKSD` the same for both laws |
| 771–784 | `CycleStatistics.GeneratorAccuracy` | seven `EPS` values |
| 789–819 | `CycleStatistics.OxidizerSizes` | `DOK43b`, `DOK43s`, `epsalldok`, `ALLVDOKSO`, `ALLDOK432`, `ALLDOK243`, `FMDOK`, `ALLDOK43`, `ALLDOKsd`, `EPSX1–3`, warning flag |
| 825–959 | `Categories.MergeAndDescribe` | see `## Categories` |
| 963–996 | `CycleStatistics.Pockets` | `QKS1` from `Qks`, `MD3/4`, `DD3/4`, `EPSY` and warning flag, `epsMD3/4`, `VKSO`, `D432`, `D243`, `Dqkarm`, `DP43`, `sdevP43`; `D43` (979) never printed, not ported |
| 1001–1016 | `CycleStatistics.Matrix` | `gdokleft`, `gdoksfr`, `PLOTsmdok`, `vdokleft`, `PLOTsm`, `mp` |
| 1021–1065 | `SmallParticles.Probability` | `zdoksmall`, `ddoksmall`, `vdoksmall`, `pdoksmall` (monotone from index 2) |
| 1066–1074 | `SmallParticles.MaxSize` | `Dmaxxx`; `zdmaxxx` never read, not ported |
| 1087–1113 | `CycleStatistics.CorrectedPockets` | cycles ≥ 1: `Dkarm43_cor`, `sdevP43_cor`, `Dqkarm_cor`, `Dfmk432`, `sdevP243`, `Dqmkm1`, `Dqmkm2`, `qmcoef` |
| 1119–1134 | — | `gk0`, `gk1`: output commented out, not ported |
| 1137–1152 | `CycleStatistics.MassFractions` | cycles ≥ 1: `DolM1`; in-place division and `DolM2`; in-place division and `DolM3`; `Vdok_total0`, `Vdok_total1`, `deltaVdok` (1140–1142) never read, not ported |
| 1168–1175 | `CycleStatistics.Convergence` | the six values `epsy`, `epsmd3`, `epsmd4`, `(ALLDOK43−DokM)/DokM`, `(ALLDOKsd−DokSD)/DokSD`, `1 − DolM2` |
| 1078–1082, 1154–1167, 1176–1181 | — | cycle bookkeeping, `Simulation` |

In cycle 0 the rows 771–1074 run; the rows for cycles ≥ 1 do not.

`FMDOK(k+1) = FMDOK(k) + ALLVDOKSO(k)` with `FMDOK(1) = 0`, so `FMDOK(j)` is the mass
share of the first `j − 1` cells; `gdokleft` reads `FMDOK(int(Dmin/Di)+1)`.

**The screen summary (1454–1516) is declared here, not ported** (2026-09-24, closing a
fidelity-audit finding: the report this node computes was already cited against those
lines — see below — without any node declaring them as read). These are the original's
final `write(*,*)` calls to the console. `src/Cli/BOOT.md` carries its own taboo, "No
reading of the Fortran source: this node does not transcribe it", and its own console
summary (mode, accelerator, batch size, layout, seed, attempts, elapsed, file paths —
`src/Cli/BOOT.md`, "Progress and the summary") is a deliberate replacement, not a
transcription, so `Cli` is not the declaring node. Every quantity these lines print —
`QKSS`, `NFX`, `NFZ`, the six-stream accuracy average (1470), `EPSX1/2`, `DOKM`,
`DOK43b/s`, `ALLDOK43/432`, `EPSY`, `EPSMD3/4`, `1 − DolM1/2/3` (1492), `mp`, `DP43`,
`Dkarm43_cor`, `Dfmk432` and their products with `mp` (1495–1497), `IbridgeTotal`,
`NnTotal`, `JammedTotal` and their ratios to `FI` (1500–1504), the nine conditions — is
already in this node's own `## Report` above and reaches the user through `results.m`
(`Output`); none of it is missing from the port, only from the console. Not part of
this node's own required Line map scope (the sentence at the top of this section), so
the coverage test above does not scan it.

The local variable `D43` (line 979, the quotient of two moments) is unrelated to the
three console labels printed at 1485–1487, `'D43(1) ='`, `'D43(2) ='`, `'D43(3) ='`:
those are text labels attached to `DP43`, `Dkarm43_cor` and `Dfmk432` respectively (all three already
in the `## Line map` table above and in `## Report`), not the variable `D43`. A whole-file
search finds `D43` itself only at line 979 (its assignment) and inside the commented-out,
never-executed line 1166 (`c write(3,*)fi,epsx1,epsx2,epsy,d43`) — so the existing row's
claim, "`D43` (979) never printed, not ported", is confirmed, not contradicted, by
1485–1487.

## Categories

Lines 825–959, over the integer totals `Qdoks` and the real totals `Dokp41`, `Dokp31`.
`VDOKS` and `QDOK` are not needed: `VDOKS` feeds only `VDOKSO` and `DOKP432`, and
`QDOK` only a commented-out print (line 1403); `Particle` does not accumulate them.

1. `DPRow` (line 825: `DPmax/Dj` truncated, plus 1; binary32 truncated quotient,
   "## Invariants", "Array sizes from binary32 values"); `Dpockets(r) = r·Dj` for `r = 1..DPRow` (825–829).
2. Label 600, for rows `1..DPRow`: `QDOKSS = Σ_i Qdoks(r,i)`; `QDOKS1 = Qdoks/QDOKSS` or
   0 when `QDOKSS = 0`; `MDOK4`, `MDOK3` over `c_i = Di·(i − ½)`; `DDOK4`, `DDOK3`;
   `Epsydok` (0 when `QDOKSS = 0` or `MDOK3 ≤ 1e-30` or `MDOK4 ≤ 1e-30`); `QDOKSO =
   Qdoks/QDOKSS` or 0; `qdokkarm = Σ QDOKSO·c_i`; `DOKP43 = Dokp41/Dokp31` or 0 when
   `Dokp31 ≤ 1e-30`. (`DOKP431`, `Epsmdok3/4`, `VVDOKS`, `VDOKSO`, `DOKP432` are never
   printed; not ported.) Arrays over `Ncat` are reset before the pass, as lines 830–837.
3. Pass (911–945), if `DPRow > 1`: walking `r = 1..DPRow−1`, the first row with
   `Epsydok(r) > EpsDok` absorbs row `r+1` (`Dpockets(r) ← Dpockets(r+1)`; `Dokp41`,
   `Dokp31`, `Qdoks` rows added); every later row is shifted from `r+1`. After a
   merge `DPRow −= 1` and step 2 repeats.
4. Last row (947–959): if `Epsydok(DPRow) > EpsDok` and `DPRow > 1`, rows `DPRow−1` and
   `DPRow` merge as in the pass and `DPRow −= 1`, **without** returning to step 2.
5. The report takes `Dpockets`, `DOKP43`, `qdokkarm` and the `QDOKSO` rows for
   `r = 1..DPRow`.

The merge and shift write into the run's totals (`## Invariants`); the old last row
keeps its contents after any merge, and later cycles add pockets into rows by their raw
index `int(RK/Dj)+1`. The original behaves so; it is reproduced.

**`DPRow` never exceeds `Ncat`.** `Ncat = trunc(Ddokmax·AK4/Dj) + 2` (`Setup.Sizes`,
"## Invariants", "Array sizes from binary32 values"); `DPRow = trunc(DPmax/Dj) + 1`
using the same binary32 truncated quotient. Every pocket radius `RK` that can ever
reach `DPmax` (Fortran lines 673, 684, 700, this node's own accumulator) is bounded by
the largest base particle times `AK4`, itself bounded by `Ddokmax` — so `RK ≤
Ddokmax·AK4`, hence `DPmax ≤ Ddokmax·AK4` and `DPRow ≤ trunc(Ddokmax·AK4/Dj) + 1 =
Ncat − 1 < Ncat`. `Categories.MergeAndDescribe` checks this rather than trusting it
silently (root BOOT.md, "Failures are values"; `CategoriesStatus`, `API.md`): if
`DPRow > Ncat` it returns `CategoriesStatus.CategoryCountExceedsCapacity` with every
`out` parameter empty and no total read or written, instead of the Fortran's own
unclamped loop over the rows `1..DPRow` (line 827, which has no clamp at all) or a
defensive truncation that would silently change the model's output (root BOOT.md, Taboos: "No silent fix of
a defect of the original"). `CategoriesTests.MergeAndDescribeReportsStatusWhenDpRow
ExceedsNcat` constructs a `DPRow > Ncat` input directly (bypassing the invariant above
on purpose, since the argument is about the model's own particles, not about what this
method's parameters allow a caller to pass) and proves the status fires instead of the
`IndexOutOfRangeException` the old silent clamp's own downstream `Recompute` loop threw
once `row` was allowed past `ncat` (found restoring the old clamp as this check's own
mutation proof, `tests/Statistics.Tests/BOOT.md`, "## Mutations").

⚠ 2026-09-18: was an undeclared clamp `r < Ncat` in step 1, now the checked status above
→ HISTORY.md#categories-undeclared-clamp-2026-09-18

## Report

`CycleReport` holds, in SI units:

- setup echoes: `DOKM`, `DOKSD`, `Ddokmax`, `Dmax`, the modified `alfa`, `GGG`, `ZX`;
- counters as they stand: `QKSS`, `NFX`, `NFY`, `NFZ`, `NFQ`, `NFW`, the nine
  conditions, `IbridgeTotal`, `JammedTotal`, `NnTotal`;
- accuracies: `EPS1`…`EPS7`, `EPSX1`, `EPSX2`, `EPSX3`, `epsalldok[NMM]`, `EPSY`,
  `epsMD4`, `epsMD3`, and the warning flags `EPSX3 > EpsDok`, `EPSY > EpsDok`;
- sizes: `DOK43b`, `DOK43s`, `ALLDOK43`, `ALLDOK432`, `DP43`, `D432`, `sdevP43`,
  `Dqkarm`, `Dkarm43_cor`, `sdevP43_cor`, `Dqkarm_cor`, `Dfmk432`, `sdevP243`, `Dqmkm1`,
  `Dqmkm2`, `qmcoef`;
- matrix: `gdokleft`, `vdokleft`, `PLOTsmdok`, `PLOTsm`, `mp`;
- mass fractions: `DolM1`, `DolM2`, `DolM3`;
- distributions as fractions per cell: `ALLVDOKSO[Ndok]`, `VKSO[Nkarm]`, `QKS1[Nkarm]`,
  `fmkarm_cor/Σ[Nkarm]`, `fmkarm2/Σ[Nkarm]`, `fqkarm_cor/Σ[Nkarm]`, `qmkm1/Σ[Nc]`,
  `qmkm2/Σ[Nc]`, `coef/Σ[Nc]`, `pdoksmall[Ndok − 1]` (Fortran line 1386: `call
  arrayprint('pdoksmall',pdoksmall,Ndok-1)`, one element short of the array's own
  `Ndok` dimension, unlike every other `Ndok`-sized array here, all of which print the
  full `Ndok` — measured against `fmdok`/`ALLVDOKSO`'s own printed length on all five
  reference formulations, `tests/Statistics.Tests/PdoksmallReportLengthTests`), with
  `DPmax`, `DPmaxCor` and the
  1-based largest non-empty index of `coef`, `qmkm1`, `qmkm2` (the original's `*_nmax`,
  0 when empty) from which `Output` derives the printed lengths `nmax + 2`; these can
  exceed `Nc` (hp2.m prints 1002 `coef` values, the last two read past the array and
  printed as zero), and `Output` then prints zeros;

  This is itself a defect of the original the port reproduces, declared for
  `## Defects of the original` (2026-09-20): printing `nmax + 2` values reads past the
  declared length of `coef`, `qmkm1` and `qmkm2` whenever `nmax` reaches `Nc − 1` or
  `Nc`, as `hp2.m`'s 1002 `coef` values show; the two `*_nmax` write lines this node
  reads (`coef`'s own, `Qmkm1`'s and `Qmkm2`'s) are [`Particle`'s own
  `## Accumulators`](../Particle/BOOT.md#accumulators), lines 563, 631, 638 — this
  node owns the framing and the measured consequence, `Particle` owns the lines, and
  neither retells the other's claim.

  ⚠ 2026-09-24: was "`Output` then prints zeros" and a `## Defects of the original` row
  here marked `reproduced`. Neither was true until today: `Output` cut these arrays at
  `Nc`, and the fidelity audit of 2026-09-24 found the gap against `hp2.m`. `Output` now
  pads them with zeros. The defect row moved to [`Output`](../Output/BOOT.md), which
  transcribes the printing lines 1370, 1373 and 1377, so the defect page carries it once.

  **Escalation (2026-09-20, AGENTS.md §11) resolved 2026-09-21, root session spanning
  `Statistics` and `Output` (AGENTS.md §11): `fmkarm`/`fqkarm`/`fmkarm_cor`/
  `fmkarm_cor2`/`fqkarm_cor` print `int(DPmax/Di) + 2` and `int(DPmax_cor/Di) + 2`
  respectively** (Fortran lines 1345, 1350, 1353, 1357, 1361, 1364 — outside this
  node's declared line map, consulted only to locate the mechanism, not transcribed
  as this node's own specification), and `Output`'s `ResultsMWriter` already computes
  exactly this, in plain `double`, from the `DpMax`/`DpMaxCor` fields `CycleReport`
  already carries.

  ⚠ 2026-09-21: was "this node cannot tell … whether the mismatch is this quotient
  rounded in plain `double` instead of binary32, computed from the wrong field, or not
  computed at all", with a proposal to expose two new binary32-rounded `CycleReport`
  fields and extend the root's exception list. All three candidates are refuted by a
  live reference-mode run (`Original` layout, seed 0) of every reference formulation,
  compared against its own archived `results.m`:

  - **not "the wrong field" or "not computed at all":** the port's `fmkarm`/`fqkarm`
    length equals `int(DpMax/Di) + 2` and matches the archive exactly on **all five**
    reference formulations (67, 48, 55, 36, 127); the `fmkarm_cor`/`fmkarm_cor2`/
    `fqkarm_cor` length equals `int(DpMaxCor/Di) + 2` and matches the archive on
    **four of five** (66, 48, 13, 36) — only HMX misses (82 against the archive's 88).
    `ResultsMWriter` reads `DpMax` for the first pair and `DpMaxCor` for the second,
    exactly as the Fortran source does; no field confusion, no missing computation.
  - **not "plain `double` instead of binary32":** binary32-rounding `DpMax`/`DpMaxCor`
    before the division could move the truncated quotient by at most one unit, at an
    integer boundary (the shape the `Ndok`/`DPRow` cases above already treat). HMX's
    gap is six cells — about 60 μm at the default 10 μm cell size, roughly 5% of
    `DpMax` itself — three orders of magnitude past what a rounding-boundary flip can
    produce, and it is entirely absent on the other four formulations, where the
    plain-`double` quotient already matches exactly. Extending the root's binary32
    exception list to this pairing would fix nothing and is not proposed.

  **What actually produces the gap: `DpMaxCor`'s own accumulated value, not the
  print-length formula.** `DpMax` and `DpMaxCor` are `Particle`'s own accumulators
  (`../Particle/API.md`, "Real-valued record": "running maximum pocket radius"); Fortran
  lines 671–703 (consulted the same way as 1345–1364, not transcribed here) show
  `DPmax` updated unconditionally for every pocket radius `RK` (line 673), while
  `DPmax_cor` is updated only for a pocket that clears two further, `pdoksmall`-gated
  checks inside the same attempt (line 683: `pdoksmall(int(max(Dr,Db)/Di)+1) < 1`;
  line 699: a drawn `X4 < pdoksmall(…)`) — the "Var#1"/"Var#2" pocket-in-pocket tests.
  `pdoksmall` is this node's own next-cycle output (`SmallParticles.Probability`),
  already measured above ("REAL*4 accumulation, carried through") as diverging from
  the original on HMX specifically (its worst reachable cell, driven by `Vdokstr`:
  "22.6% of band — inconclusive", the one formulation neither ruled out nor
  confirmed). Since reference mode draws the same random numbers as the original bit
  for bit, a `pdoksmall` value that differs even slightly at the decisive index can
  flip whether one of the handful of largest-radius pockets clears the gate — exactly
  what the running maximum `DpMaxCor` is most sensitive to, and `DpMax` (an
  unconditional maximum over every pocket, not the rare gated subset) is not. A live
  run confirms the two programs' decisions genuinely drift apart over HMX's full
  particle count, not just in this one cell: the same run's own header counters
  (`NFX`, `NFY`, `NFQ`, `NFW`) already differ measurably from the archive (`NFX`
  6,516,126 in the port's run against 6,507,516 in the archive) — the expected shape
  of the already-declared "double precision only" divergence compounding over many
  particles, not a new phenomenon.

  This is a further, measured consequence of the already-declared `Vdokstr`/
  `pdoksmall` REAL*4-accumulation defect ("## Defects of the original" below), not a
  new defect and not one `Statistics` or `Output` can fix: the gating itself is
  `Particle`'s own per-attempt code (Fortran lines 683, 699), outside both nodes'
  contracts. No change is made to `CycleStatistics`, `CycleReport` or
  `ResultsMWriter`; the escalation's own proposal to add two new binary32-rounded
  `CycleReport` fields is withdrawn, since binary32 rounding would not move any of the
  ten measured lengths. `tests/Output.Tests/PocketPrintLengthTests` pins this finding
  down: the formula is checked against the archive on the nine matching lengths, and
  the HMX gap is measured explicitly from the archive, not excluded silently.

  ⚠ 2026-09-21, later the same day: "can flip" above was a mechanism that fit the
  evidence, not a demonstrated link — two other attributions in this project's own
  history fitted just as well and were withdrawn (the `VdokTotal`/`VdokTotal2` chain,
  root `ACCEPTANCE.md`; the REAL*4-long-sum attribution over 167 of
  173 cells, the same section). Put to the dose-response instrument
  (`tests/Fixtures/run_original.py`/`run_port.py`, `--n` override, `--layout original
  --seed 0`, the sanity-check seed both scripts treat as the unmodified executable) on
  HMX at its shipped `N = 10000`, `N/10 = 1000` and `N/100 = 100`, the mechanism's own
  sharp prediction holds: the `fmkarm_cor`/`fmkarm_cor2`/`fqkarm_cor` length matches
  the original exactly (56/56, 77/77) at the two reduced `N` where `pdoksmall` is
  close to agreement, and only misses (88 against 82) at the shipped `N`, where
  `pdoksmall` has diverged far more:

  | `N` | worst `pdoksmall` cell (relative, `\|original\| ≥ 1e-30` cells only) | cells differing beyond 1e-9 relative | `fmkarm_cor` length |
  |---|---|---|---|
  | 100 (`N/100`) | 0.24% (index 22) | 1 of 68 | 56 / 56 — match |
  | 1000 (`N/10`) | 3.1% (index 2) | 17 of 68 | 77 / 77 — match |
  | 10000 (shipped) | 64.6% (index 2) | 68 of 68 | 88 / 82 — six short |

  (Index 0 and one further leading cell are excluded at every `N`, both original
  values sitting below the already-declared 1e-30 floor, `pdoksmall(1)`'s own
  garbage-copied-forward defect — unrelated to this mechanism and excluded the same
  way `tests/Fixtures/exclusions.json` already excludes it.) The length agrees exactly
  where `pdoksmall` is close to bit-identical and parts company only once `pdoksmall`
  has diverged by two orders of magnitude more — the link the mechanism predicted,
  now measured rather than argued. Had the six-cell gap already been present at
  `N/100`, where `pdoksmall` is nearly unchanged, the mechanism would have been
  refuted outright; it is not what was found.
- categories: `DPRow`, `Dpockets`, `DOKP43`, `qdokkarm`, `QDOKSO[DPRow × Ndok]`;
- convergence: the six values of lines 1169–1174.

In cycle 0 the scalars of cycles ≥ 1 are `NaN`. The report of the last cycle is the
run's result. Next-cycle inputs: `pdoksmall[Ndok]` and `Dmaxxx`. This `pdoksmall` is
`CycleStatistics.Compute`'s own `nextPdoksmall` out-parameter, not `CycleReport.Pdoksmall`
above: the model-feedback array (read by `Particle/Attempt.cs`'s own bridge/pocket
checks, Fortran lines 663, 683, 699) needs every one of the `Ndok` elements the merge
loop's own bound can reach, while the *report* field is one element shorter, matching
only what Fortran 1386 prints. Sharing one array between the two roles is exactly the
port defect this document declares below: before the fix, `CycleReport.Pdoksmall` was
the same, full-`Ndok` array as `nextPdoksmall`, one element longer than the original
ever prints, on every reference formulation.

⚠ 2026-10-02: was "In cycle 0 the quantities of cycles ≥ 1 are `NaN`", now only the
scalars; `Compute` fills the six `*Normalized` arrays and the three `*Nmax` entries from
the totals in every cycle, and cycle 0's are never printed.

**Decisions where the port departs from a transcription** (declared, §12):

- `pdoksmall(1)`, never assigned by the original, is 0. The original's garbage
  (0.338E-39 … 0.153E-37 in the archived outputs) is copied forward by the clamp of
  lines 1062–1064 into the leading cells while they are zero (rc166.m prints six
  cells of 0.762E-38). The port prints zeros there; the comparison treats reference
  values below `1e-30` in `pdoksmall` as zero, recorded in the exclusion list of
  `tests/Fixtures` with this evidence.
- `pdoksmall` is printed at `Ndok − 1` (Fortran line 1386): the original's own print
  call is the defect; the port failed to reproduce it (printing the full `Ndok`
  instead); the fix below reproduces it, reconciling the prose here with the
  `## Defects of the original` row's own `reproduced` (2026-09-20).
  `CycleStatistics.Compute` handed `CycleReport.Pdoksmall` the same, full-`Ndok` array
  it also writes into `nextPdoksmall` (the next cycle's model input), so every reference
  formulation printed one element more than the original. `pdoksmall` carries 129 of
  the 173 cells the root's statistical criterion currently counts as failing, compared
  index by index — but this fix does **not** explain them. `tests/Harness/HISTORY.md`,
  "`pdoksmall`'s length, its alignment, and its dose-response in N" (2026-09-20,
  merged into `claude/wave7` ahead of this fix) already established that the extra
  element sits at the array's own **tail**, not somewhere that shifts every later
  index out of alignment: a sum-of-absolute-differences comparison on HPEPA3 gives
  `0.922` under index-for-index pairing against `1.819` under head alignment, and
  every formulation's rising segment has the same step count in both programs.
  Index-for-index is exactly what the root criterion already does, so it was already
  aligned correctly on every shared cell, and this node's own
  `ReportPdoksmallRetainsEveryOtherCellBitForBit` proves the trim changes none of
  those cells' values. **The bound, stated plainly: this fix can change the failing
  count by at most the one dropped cell per formulation — five cells at the very
  most across all reference formulations, not a large share of 129 — and the 129
  cells themselves are unaffected by it.** What the same measurement entry shows
  about those 129 cells instead, under the correct alignment the criterion already
  uses: the profile is not flat — the port's own value is stable in `N` while the
  original's collapses (HPEPA3's index-2 cell is bit-identical at `N/100`, `70%`
  apart at the shipped `N`; HMX's is bit-identical at `N/100`, `182%` apart at the
  shipped `N`) — an accumulation signature in the *original*, not an artefact of
  this array's length. The fix itself moves the array's length, never a retained
  value (`tests/Statistics.Tests/PdoksmallReportLengthTests`,
  `ReportPdoksmallRetainsEveryOtherCellBitForBit`; trims only the report's own copy,
  `pdoksmall[..^1]`, after `nextPdoksmall` is already written from the full array, so
  the model-feedback path is untouched). This node makes no claim about the
  criterion's own verdicts beyond this bound (out of scope, "## Taboos" below;
  `tests/Harness` owns that count).
- `VDOKS` and everything derived only from it are not ported; so are the other
  never-read quantities marked in the line map.
- Sums the original keeps in REAL*4 arrive as `Particle` stores them: rounded per
  addition under `Original`, `double` under `Binary64` (`src/Particle/BOOT.md`); this
  node's own REAL*4 stores follow the bullet "The per-cycle plane from the executable's
  listing".

  ⚠ 2026-10-01: was "Sums the original keeps in REAL*4 (`ALLVDOK`, `vdokstr`,
  `fmkarm*`, `VSMKM`…) are `double`", stale since the accumulators followed the kind on
  2026-09-21 (D8 of the 2026-09-24 audit).
- The category merge's own threshold comparison, `Epsydok(r) > EpsDok`, compares two
  binary32 values under `Original` (`Epsydok` narrowed at 875, `EpsDok` the setup
  plane's stored menu value) and two `double` values under `Binary64`; the original
  computes `Epsydok` in REAL*4 and compares it against `EpsDok` (also read as REAL*4).

  ⚠ 2026-10-01: was "is `double` on both sides", now binary32 under `Original`; row
  911–959 reproduced.

  The rest of this bullet is its 2026-09-20 reading, `Binary64`'s. This is not a "sum"
  and is named separately from the bullet above (found by the audit of this node: the
  previous wording folded every REAL*4-vs-`double` departure under "sums", which
  `Epsydok` is not) because it gates
  *control flow*, not just a printed value: a REAL*4 rounding of either side could move
  a row across the merge threshold and change `DPRow` itself, not merely one cell's
  value.

  ⚠ 2026-09-20: was "no case in `categories.json` currently sits close enough … a
  divergence would be recorded if one is ever found", now confirmed as the mechanism
  behind a cross-node measurement of two reference formulations' own archived output,
  which this node did not itself run (`docs/ORIGINAL-DEFECTS.md`'s design-session
  review): `Dkarmcat`/`dokkarm43`/`dokkarm10` run 17 rows in the original against 18 in
  the port on HPEPA3 (opposite sign on HMX, 59 against 58), with a prefix of
  identical rows, the same terminal boundary value (ruling out `DPRow`'s own initial
  sizing, "## Categories" above — a wrong `DPmax`/`Dj` quotient would move the *last*
  value, not merely its index), and a tail differing by exactly one merged-away row,
  not a general reshuffling — the one shape `Categories.MergeAndDescribe`'s own merge
  loop produces when the merge *count* differs by one. `Qdoks`, the only per-row input
  `Epsydok` reads, is an exact integer count in both programs, so a merge-count
  divergence has nowhere else to come from. `tests/Statistics.Tests/
  MergeThresholdSensitivityTests.EpsydokThresholdFlipsWithinOneBinary32UlpOfTheBoundary`
  confirms the mechanism's sensitivity on a constructed two-row scenario (black-box
  bisection over `epsDok` against this node's own `Categories.MergeAndDescribe`, no
  independent transcription of its formula): the merge decision flips between two
  `epsDok` probes less than one binary32 ULP apart at that magnitude — exactly the
  scale this defect describes. This node does not have the two formulations' own
  internal `Qdoks`/`Dokp41`/`Dokp31` totals (only their printed, post-merge output), so
  the *incidence* (2 of 5 formulations) is not independently reproduced here, only the
  mechanism that would produce it; unsurprising, given each formulation carries 17 to
  70 such comparisons. `tests/Fixtures`' own exclusion list is still where a
  formulation's specific failing cells would be recorded with evidence, if the
  statistical criterion traces any to this cause — out of scope here ("## Taboos").
- `Categories.cs`'s own `DPRow` (line 825) and `SmallParticles.cs`'s own
  whole-fraction count `int(real(kilo)/ak2)` divide by `Dj` and `AK2`, both REAL*4 in
  the original (this node's own BOOT.md, "Array sizes from binary32 values" already says so for
  `Dj`; `AK2` is a plain formulation coefficient read the same way), through
  `Binary32.TruncatedQuotient`, the same helper `Setup.Sizes`'s own `Ndok` uses
  (`Nkarm`/`Ncat` use a different helper, "## Invariants", S-2).

  ⚠ 2026-09-26: was `Nkarm`/`Ncat` said here too to share `Ndok`'s own
  `Binary32.TruncatedQuotient` helper, now corrected to match S-2
  (`Binary32.TruncatedQuotientOfProduct`) → HISTORY.md#nkarm-ncat-product-rounding-s2-2026-09-24

  ⚠ 2026-09-18: this bullet first left both divisions as plain `double`, declared but
  not fixed, and raised a proposal for a root design session (either extend the root
  exception to name `DPRow`/`wholeFractions`, or decide that only array-size quotients
  get binary32 treatment). The owner decided the same day, in a second session, to
  extend the root exception by kind rather than leave it to the next boundary case (root
  BOOT.md, "Double precision only", its own dated note); this node's own BOOT.md,
  "## Invariants", "Array sizes from binary32 values" carries the implementation
  and the measured margins. `Categories.MergeAndDescribeUsesBinary32ArithmeticForDpRow` and
  `SmallParticlesTests.ProbabilityUsesBinary32ArithmeticForTheWholeFractionCount`
  (`tests/Statistics.Tests`) each construct a boundary case analogous to the C166
  evidence for `Ndok` (`Binary32Tests.TruncatedQuotientAvoidsDoubleRoundingAtAnIntegerBoundary`)
  and prove the binary32 rounding is actually plumbed through, not merely available;
  no archived or reference formulation's own printed length moves (measured margins
  above).
- Precondition violations return a status; the original runs on and indexes out of
  bounds.
- `FractionLaw.TryTailDraw` returns `false` instead of indexing an empty active set
  when every fraction has been excluded; unreachable given the preconditions
  `Setup.Prepare` already checks (`FractionLaw.TryTailDraw`'s own doc comment carries
  the proof), kept as a status rather than a defensive guard because the root
  invariant "Failures are values" forbids an unchecked throw regardless of reachability
  (found by the audit of this node: the previous version indexed `cumulative[-1]` in
  that state instead).
- `epsdokfr`/`epsalldok(kilo)` (lines 792–797, fed by the fraction-share normalization
  of `PARAM`, lines 1723, 1725, 1727: the running sum `ZS` of line 1725, then the
  division of each `Z(J)` by it at 1727) mixes REAL*4
  (`GDOK`, `DDOK`, `ZS`, `epsalldok` itself) with REAL*8 (`ZX`/`Z`, the normalized
  share) throughout. For a single-fraction formulation (`NMM = 1`) both the measured
  share (`real(alldok_fract(1))/ALLDOKQ`, a self-division of the same integer count,
  since `ALLDOK_FRACT` and `ALLDOK` are incremented together at every one of their two
  call sites, lines 465/468 and 520/523) and the target share `ZX(1)` (also a
  self-division, `Z(1)/ZS` where `ZS` is accumulated from `Z(1)` alone when `NM = 1`)
  are mathematically exactly 1: `FractionLaw.Build` and `CycleStatistics.OxidizerSizes`
  compute both sides in `double` throughout (`share[j] = z[j] / zss`, `FractionLaw.cs`;
  `fraction = integerTotals[...] / alldokq`, `CycleStatistics.cs`) and a `double`
  self-division is exact regardless of the dividend, so the port's `epsalldok[0]`
  cancels to exactly `0`. The original's mixed-precision chain does not cancel: this is
  the defect declared below, not a port omission — declared, not reproduced.
- **The per-cycle plane from the executable's listing** (decided 2026-10-02, owner: the
  whole region 771–1175, the hoisted block and line 378; implemented 2026-10-03; stages
  in `ACCEPTANCE.md`). Under `Original` the plane does what `PropStructV3.exe` does,
  read from a committed excerpt of its listing
  (`tests/Fixtures/Legacy/PropStructV3.cycle-plane.listing.txt`;
  `tests/Fixtures`, "## Cycle-plane listing") through the hand-written address map
  `CyclePlaneListing.map.txt` (`API.md`, "## Cycle listing map and table") by the
  block-local x87 stack reader of `classify-cycle-plane-listing.py`, which writes
  `CyclePlane.listing.generated.txt`, never typed (AGENTS.md §6). Each site has a
  kind:
  - **S** a REAL*4 store: rounded where it is stored, read back from its home; **T** a
    store to a compiler temporary, rounded the same way; **Q** and **K** a REAL*8 or a
    constant store, exact; **M** a move of a loaded or merged value, exact;
  - **R** a register value never stored before its last read, so never rounded; a read
    is `name:home` when the executable reloads the home although the register still
    holds the unrounded value, `name:register` when it takes the register;
  - **P** a sum stored and reloaded every pass; **X** a sum carried in the register
    across a pass that also stores a copy every pass (what rule L modelled);
  - **B**`n` a sum unrolled `n`-fold, reloaded from its slot at the head
    of a block, stored after its `n`th pass and after every pass of the remainder loop;
    **C**`n` `n` stores each reading the one before from the register inside a block,
    from memory on a block's first pass and on every remainder pass;
  - **E** a value no line reads or a line with no code; **CMP** a comparison, with the
    home or the register as its operand; the `order` column holds the order of every
    product as the listing multiplies it, `**4.`, `**3.` and `**2` as products.
  The code reads the table, never the other way. `CyclePlaneRounding.Store` rounds at an
  S, P, X, B or C site and `Temporary` at a T site; `UnrolledSchedule(trips, n)` says
  after which pass a B`n` sum is stored and before which a C`n` pass reloads;
  `CyclePlaneOrder` forms the products in the listing's order under `Original` and in
  the port's own (the cell centre first, `Math.Pow`) under `Binary64`; a read marked
  `:register` takes the unrounded value, `:home` the stored one. A rounding call cites
  its Fortran lines, and `CyclePlaneSites.txt` maps every rounding site of the table to
  the members whose calls carry it or to why none does, checked four ways
  (`tests/Statistics.Tests/CyclePlaneSiteMapTests`); `Literal`, `Fold` and `Input`
  carry no row, the table's `order` text holding the binary32 constants. Under
  `Binary64` every method returns its argument, so nothing moves.
  Arithmetic is 53-bit (`_controlfp(0x10000, 0x30000)` at C start-up, 0x418D7A; the
  Fortran runtime leaves precision control alone), so `+ − × ÷` and comparisons are
  IEEE `double`, a store to REAL*4 rounds once, and `int()` is a chop `fistp`. Declared
  residuals, the runtime routines the listing calls that this node has not read:
  `_FIIfexp_` (`**0.3333`, 1016: `Math.Pow`, shows only at a binary32 tie), `_CIsqrt`
  behind `_FIsqrt` (`SQRT`, `**0.5`: `Math.Sqrt`, correctly rounded) and `_FXAMOD`
  (`mod`, 1025: `a − ⌊a/b⌋·b`, exact for these operands). They are the one `declared,
  not reproduced` row of "## Defects of the original"; the rest of 771–1175 is its
  `reproduced` row. `QKS1` (718), `QKS/real(QKSS)` in the particle loop, is read from
  the excerpt's bytes, not run: the executable divides by the exact `INTEGER*8` count
  (`fild qword`, 0x40B823) and rounds only the quotient, which is what `Pockets` computes
  and what the oracle injects (the row `718, 963–996`, `reproduced`).

  ⚠ 2026-10-03: was "injects `f32(q / f32(QKSS))` where `Pockets` computes `r32(q /
  QKSS)`: one expression while `QKSS` is a binary32 integer, up to 2^24, two above it"
  and the divisor "not known"; now the executable's divisor read from 0x40B7FA–0x40B8AF
  → HISTORY.md#qks1-divisor-2026-10-03
- **The per-cycle plane under `Original`, rules R1–R6 and L** (decided 2026-09-27) is
  replaced by the bullet above: the table decides, no rule does.

  ⚠ 2026-10-03: was a model of six rules and L read from the statement structure, now
  the listing's table → HISTORY.md#per-cycle-plane-source-read-rules-2026-10-03

  ⚠ 2026-10-01: was "The per-cycle plane stays `double` under either precision kind",
  with integer counts "converted before dividing (793)" and "rounding the stores without
  the folds and conversions regressed `da_coef` and `epsdokfr[0]`"; the line is 795, and
  rounding the conversions moves `epsdokfr[0]` to 0.259E-05, further from the original →
  HISTORY.md#per-cycle-plane-stays-double-2026-10-01

  ⚠ 2026-10-03: was the row 771–1175 and the row 771–1175 (register lifetimes) both
  `open`, now one `reproduced` row and one `declared, not reproduced` row for the three
  runtime routines, A1–A7 being met → HISTORY.md#defect-rows-771-1175-open-2026-10-03

  ⚠ 2026-10-02: was R2, R3 and L stated as the executable's behaviour, the row 771–1175
  of `## Defects of the original` `reproduced` and the root's "Precision kind" bullet
  repeating "rounded once after its loop" and "every value handed to a later block is
  rounded"; now a reading of the statement structure that the executable's listing
  contradicts at the sites below, the row `open`. Found by disassembling
  `tests/Fixtures/Legacy/PropStructV3.exe` (`dumpbin /disasm`, 14.12, 2026-10-02);
  addresses are the executable's, a review claim kept only where read there again.
  - 798 `ALLVDOKS`: never stored; 804 divides by the register sum
    (0x40C175–0x40C1E0). The rule rounds it.
  - 989 `D432`, 991 `Dqkarm`: the loop is unrolled six-fold; both are reloaded from
    their REAL*4 slots at the head of each block (0x40E773, 0x40E78F), stored on its
    sixth pass (0x40E925, 0x40E94B) and on every pass of the remainder loop (0x40E99D,
    0x40E9C3). 990 `D243` stays in the register throughout and 996 reads it unrounded
    (0x40E9F5). The rule rounds all three once, after the loop.
  - 1004 `gdoksfr`: the register sum is added unrounded into 1006 (0x40EA6D). 1006
    `gdokleft`: stored (0x40EA6F), every in-block read takes the stored value (0x40EA87,
    0x40EACF). 1008 `PLOTsmdok`: stored to its home, read from it by 1010 and 1014
    (0x40EADB, 0x40EAED). 1150: stored, re-read from its home by 1151
    (0x4103C2–0x4103C8). The rule keeps all of these in the register.
  - Agrees with the rules, read in the listing: 795→796 (0x40C0DA–0x40C10A), 804→805,
    806, 808 (0x40C1E3), the dead stores of 1014 and 1015 (`mp` in the register to its
    one store, 0x40EB4A), 1028 (`sum()` inlined and recomputed each pass, 0x40ED52–
    0x40EE01) and 1031 (0x40EEBC–0x40EEC0).
  - Not modelled: five binary32 temporaries computed once before the cycle loop
    (0x409503–0x409595) and three spilled shared subexpressions (0x40EA8D, 0x40EAC9,
    1150's divisor); the row 771–1175 (register lifetimes) listed them, since merged.
    805's accumulator is also reloaded at a block head (0x40C1FE); its schedule is not
    read.

  ⚠ 2026-10-02: was "80-bit against 53-bit intermediates" a declared residual of this
  plane and of the setup plane, and "a runtime call not counted as a block end" another;
  now the 80-bit premise refuted for the C runtime's start-up, which calls
  `_controlfp(0x10000, 0x30000)` (53-bit precision, 0x418D7A, called from 0x418CE1; the
  import at 0x41D0B4 is `_controlfp`), and the executable frees the x87 stack before
  each runtime call (0x40E9F7–0x40EA05, 0x40EB1F–0x40EB2B), so no register value lives
  across `**`. Whether the Fortran runtime's own start-up changes the precision later
  is not checked, and the effect of either on a printed cell is not measured. Found by
  the same disassembly.

**REAL*4 accumulation, carried through to the printed quantities it reaches** (measured
2026-09-20, `tests/Statistics.Tests/AccumulationConsequenceTests`). `src/Particle/BOOT.md`
measured the binary32-vs-double relative error of `Allvdok`, `Vdokstr`, `VdokTotal`,
`VdokTotal2` in their own units but could not put it against a printed quantity's own
band, being unable to read this node's derivation (AGENTS.md §3). This node's own line
map settles it, against the chain handed down for verification, which turns out wrong
for two of the four accumulators:

- **`VdokTotal`/`VdokTotal2` do not reach `gdokleft`, `vdokleft` or `pdoksmall` at all.**
  `MassFractions` (Fortran 1137–1152) is their only reader, and it only ever produces
  `DolM2`/`DolM3`; `gdokleft` and `vdokleft` are *inputs* to that method, not outputs of
  it. `AccumulationConsequenceTests.VdokTotalAndVdokTotal2DoNotReachPdoksmallFmdokOrDok43all`
  perturbs both fivefold on all five reference formulations and finds `Allvdokso`,
  `Alldok432` and `Pdoksmall` bit-for-bit unchanged, while `DolM2`/`DolM3` do move — so
  their own already-measured error
  (`src/Particle/HISTORY.md#accumulator-error-attempt-granularity-superseded`: "≤
  0.003% on every formulation") is not merely small against
  `pdoksmall`/`fmdok`/`Dok43all`, it is zero,
  by construction. (`DolM2`/`DolM3` are the mass-fraction variants #2/#3, not named
  among the three quantities under suspicion; the size of their own inherited error is
  bounded by the same ≤ 0.003%, one to two orders below every "large enough to matter"
  figure this node or `Particle` has measured elsewhere, and is not separately tested.)
- **`gdokleft`/`vdokleft` are fed by `Allvdok`, through an internal cumulative array
  that shares a name with, but is not, the printed quantity.** `OxidizerSizes`'s own
  `fmdok` (Ndok + 1, the Fortran `FMDOK`) is documented at the call site as "never
  printed and not part of the report"; `Matrix` reads one cell of it,
  `fmdok[int(Dmin/Di)]`, into `gdokleft`. The *printed* "fmdok" is a different field,
  `Allvdokso` (`ALLVDOKSO(kilo) = ALLVDOK(kilo)/ALLVDOKS`, the line above it in the
  Fortran source captioned "the mass **density** distribution function", matching the
  printed comment) — a per-cell fraction, not the cumulative sum the recurrence
  `FMDOK(k+1) = FMDOK(k) + ALLVDOKSO(k)` builds. The task handed down to verify this
  drew the chain `Allvdok/VdokTotal/VdokTotal2 → gdokleft/vdokleft → pdoksmall, with
  FMDOK(k+1) = FMDOK(k) + ALLVDOKSO(k) for the [fmdok] chain` — the recurrence is real
  and does gate `gdokleft`, but it is not what "fmdok" prints, and `VdokTotal`/
  `VdokTotal2` are not on the `gdokleft` path at all (the point above). Verified against
  this node's own line map, not assumed.
- **`pdoksmall`'s dominant driver is `Vdokstr`, not `Allvdok`.** `SmallParticles.Probability`
  builds `zdoksmall`/`ddoksmall` entirely from `vdokstr`'s own ratios; `gdokleft` (the
  `Allvdok` channel) enters only as a small additive term inside the density formula
  `Pl`. Reading the loop shows every `zdoksmall(kilo)`, `kilo` in `[1, Ndok]`, reads at
  most `vdokstr[0 .. floor(Ndok/Ak2)]`: a formulation whose real oxidizer-size
  distribution carries no draws inside that window (`inpt`: first nonzero cell 25, window
  ends at 18) closes the channel structurally, regardless of `Vdokstr`'s own error.
- **Measured, worst reachable/perturbable cell, the real "fmdok" shape as each
  formulation's own `Allvdok`/`Vdokstr` proxy** (no printed proxy exists for `Vdokstr`
  itself; checked for robustness against a uniform shape,
  `ConclusionIsRobustToTheAssumedVdokstrShape`), the propagated delta against
  `StatisticalCriterion.Compare`'s own `Threshold` for that cell (the replica
  population's band, read via a synthetic failing candidate since this width does not
  depend on the candidate's own value). Each ratio below is a measurement of one thing
  only: how far `CycleStatistics.Compute`'s own output at one index moves when exactly
  one cell of `Allvdok` or `Vdokstr` — the formulation's own worst (or, for the
  gdokleft channel, worst-*reachable*) cell — is multiplied by `(1 + epsilon)`, divided
  by that same index's own replica-population band width; it is not a replay of the
  original's accumulation, not an estimate of the true original-vs-port gap at that
  cell, and not a claim about any other cell's own error. The verdict convention every
  cell below uses (`AccumulationConsequenceTests.Classify`): below 5% of band **rules
  out**, above 50% is **large enough to matter**, between is **inconclusive**.

  | Formulation | `fmdok` (← Allvdok) | `Dok43all[1]` (← Allvdok) | `pdoksmall` (← Vdokstr) | `pdoksmall` (← Allvdok, via gdokleft) |
  |---|---|---|---|---|
  | HPEPA3 | 1921% of band — large enough to matter | 403% — large enough to matter | 587% — large enough to matter | 0% — rules out |
  | inpt | 1.0% — rules out | 0.11% — rules out | channel closed (see above) | 0% — rules out |
  | P33 | 1.7% — rules out | 0.60% — rules out | 2.1% — rules out | 0% — rules out |
  | PSAN02n | 201% — large enough to matter | 15% — inconclusive | channel reaches only the array's own dropped, unprinted tail cell, nothing to explain for any printed cell | 0% — rules out |
  | HMX | 3143% — large enough to matter | 111% — large enough to matter | 2029% — large enough to matter | 0% — rules out |

  ⚠ 2026-09-21: every ratio and verdict in this table was first computed from
  `src/Particle/BOOT.md`'s 2026-09-20 worst-cell figures (`Allvdok`/`Vdokstr` up to
  1.9%/HMX), themselves since withdrawn there as a lower bound wearing the clothes of a
  measurement — one binary32 rounding per `Attempt.Run` call rather than the original's
  own one per drawn particle
  (`src/Particle/HISTORY.md#real4-accumulation-reconnaissance-2026-09-20-21`, "⚠
  2026-09-20"). Re-measured at the original's own granularity (same place, dated
  2026-09-21): worst-cell relative error reaches 46–65% for `Allvdok`/`Vdokstr` on
  HPEPA3/HMX, against the 1.8–1.9%/0.1–0.7% these documents quoted. The table above,
  its own reading below, and the completeness audit's own closing paragraph are
  recomputed against the corrected epsilon
  (`tests/Statistics.Tests/AccumulationConsequenceTests`, `WorstCellRelativeError`); the
  old table and both superseded paragraphs, in full → `HISTORY.md#accumulation-consequence-worst-cell-2026-09-20-superseded`.

  The fourth column's own reachable cell is 0 on every formulation
  (`fmdokIndex = int(Dmin/Di) = 1` on all five, measured; "## Report", "Completeness
  audit of this chain") and that cell's own real Allvdok share is exactly zero on
  every formulation, so the delta is bit-for-bit `0.0` throughout — not a small
  number, an exact one, checked against a broken-wiring explanation and confirmed
  real (`HISTORY.md#gdokleft-allvdok-reachable-cell-bound`); this column is unaffected
  by the 2026-09-21 re-measurement above, since its own zero is driven by the shape
  carrying no mass at the reachable cell, not by the epsilon's magnitude.

  Reading it the way `src/Particle/BOOT.md` reads its own table: of the twenty cells in
  this table, two (`pdoksmall` via Vdokstr, `inpt` and `PSAN02n`) carry no ratio at all
  — the channel is shut or reaches only the array's own dropped, unprinted tail cell —
  and of the remaining eighteen, ten rule the REAL*4 story out (the five of the
  `pdoksmall` via Allvdok/gdokleft column, unaffected by the re-measurement, plus
  `inpt`'s and P33's own `fmdok`, `Dok43all[1]` and `pdoksmall` cells), one (PSAN02n's
  `Dok43all[1]`) is inconclusive, and **seven are large enough to matter**: every one of
  HPEPA3's and HMX's three propagated cells (`fmdok`, `Dok43all[1]`, `pdoksmall` via
  Vdokstr), plus PSAN02n's `fmdok`. At the original's own per-draw granularity the
  REAL*4-accumulation story is no longer ruled out on three of the five reference
  formulations: it is now consistent with being a full explanation of HPEPA3's and
  HMX's own failing `fmdok`, `Dok43all[1]` and `pdoksmall`-via-Vdokstr cells, and a
  partial one on PSAN02n (`fmdok` only; `Dok43all[1]` inconclusive; the `pdoksmall`
  channel through `Vdokstr` structurally reaches no printed cell there). On P33 and
  `inpt` every propagated cell still rules the story out — the split between the two
  groups of formulations is real, not an artefact of rounding, and is the finding here,
  not a detail to average away.

  This remains a worst-reachable-cell measurement, not a full per-cell reproduction of
  a run — building the latter would replay `Allvdok`/`Vdokstr` at every cell rather
  than their own worst one, a second implementation of what `src/Particle`'s own
  `AccumulatorSweep` already is, forbidden by the root's own taboo — so it bounds what
  the mechanism *could* explain on each formulation rather than counts what it explains
  cell by cell against the root's own failing-cell counts. That bound has moved from
  ruling the mechanism out on four of five formulations to being consistent with it as
  the dominant cause on two (HPEPA3, HMX) and a partial cause on a third (PSAN02n); P33
  and `inpt` are unaffected by the re-measurement's own conclusion, still ruled out on
  every propagated cell here. As with `Fmkarm`/`Fmkarm2` in `Particle`'s own table,
  what this criterion flags on P33 and `inpt` for these three quantities is still not
  this mechanism; what it flags on HPEPA3, HMX and (for `fmdok`) PSAN02n now can be.

  **Testing the independent claim handed down with this task, not assuming it.** An
  owner-requested audit outside this tree reports that a full per-draw binary32
  emulation reproduces the original's own printed `fmdok`, `pdoksmall` and `Dok43all`
  closely on HPEPA3, HMX and PSAN02n, through a corrupted running total that lifts
  every `fmdok` cell by about six percent. This node's own propagation above is a
  single worst-(or worst-reachable-)cell multiplicative perturbation, not a full-array
  replay of the accumulation across every cell and every draw; building that replay
  here would be the same second implementation the paragraph above already rules out,
  so this node cannot reproduce or refute the audit's specific "six percent lift from a
  corrupted total" mechanism or its cited numeric values, and does not adopt them. What
  it does show: the set of formulations whose propagated cells move to "large enough to
  matter" under the corrected epsilon — HPEPA3, HMX, and, for one of its three cells,
  PSAN02n — is exactly the set the audit names, and P33 and `inpt`, which the audit
  does not name, stay ruled out here. This is a qualitative correlation with the
  audit's claim, not a reproduction of it.

**Completeness audit of this chain** (2026-09-21, checking whether the root's own
ellipsis — "`ALLVDOK`, `vdokstr`, `fmkarm*`, `VSMKM`…" — hides a REAL*4 accumulator the
analysis above never priced in). Every REAL*4 quantity reaching `pdoksmall`, `fmdok` or
`Dok43all` was enumerated by tracing each printed name to its one defining Fortran
statement (unique per name, confirmed by a full-file grep, not just this node's own
declared range) and recursively expanding every operand until it terminates in a
Particle-owned accumulator, a `.dat`-read input, or a fixed parameter — the closure
argument, not merely a long list; no `EQUIVALENCE`/`COMMON` aliasing exists anywhere in
the source (grep, zero matches) to hide a further path. Typing each operand needed the
declaration block, lines 1–64, outside this node's own range — consulted only to locate
REAL*4 vs. REAL*8, the same pattern the resolved escalation above already used for
lines 671–703 and 1345–1364.

Beyond `Allvdok` and `Vdokstr` (bounded above), the chain holds only short, local sums:
`ALLVDOKS`, one sum over `ALLVDOK` (line 798), and the per-cell division building
`ALLVDOKSO`; `ALLDOK432`'s own accumulation loop (789–819); `sum(vdokstr)` and `zdoksmall`'s own accumulation
(1021–1043) — every one REAL*4, every one at most `Ndok` terms (≤ 130 across the five
reference formulations), never a sum over the run's draws. Higham's own forward-error
bound for naive summation (this project's instrument for the long sums above) bounds
each at `n·u/(1−n·u)`, `u = 2⁻²⁴ ≈ 5.96e-8`: under 1e-5 relative at every formulation's
own `Ndok`, three to four orders of magnitude below every effect measured in this
section. Not separately tested; negligible by this bound, not by omission.

`gdokleft` — read into `pdoksmall` through `Pl`/`vdoksmall` (1050–1053) — is fed by
`Allvdok` through the internal `FMDOK` recurrence exactly as the defect row below
states, on the reachable window `[0, fmdokIndex)`,
`fmdokIndex = int(Dmin/Di)` (`gdokleft = FMDOK(int(Dmin/Di)+1)·GGG + gdoksfr` is
exactly zero on the `Allvdok` side whenever `Dmin < Di`, `FMDOK(1) = 0` by
construction). Measured at 0% of band on every reference formulation (table above,
`HISTORY.md#gdokleft-allvdok-reachable-cell-bound`): `fmdokIndex = 1` on all five, so
the reachable window is cell 0 alone, and cell 0's own real Allvdok share is exactly
zero on all five — confirmed a real, wired-through zero rather than a broken
connection by a synthetic-shape proof in the same entry.

⚠ 2026-09-21: was "`gdokleft` … enters only as a small additive term", an unproven
absolute (AGENTS.md §8), with "not measured for pdoksmall via gdokleft" left as this
chain's one open corner; now measured at 0% of band on every reference formulation, as
above → `HISTORY.md#gdokleft-allvdok-reachable-cell-bound`.

This closed the last open corner of "REAL*4 accumulation, carried through": every
component of the chain from `Allvdok`/`Vdokstr` to `pdoksmall`/`fmdok`/`Dok43all` is
measured. What "closed" settles is the *completeness* of the chain search, not the
candidate's own magnitude: no further REAL*4 accumulator beyond `Allvdok`/`Vdokstr` was
found to reach these three quantities, and every short local sum beyond them is bounded
negligible above. The candidate itself is **not** dead on this chain: at the original's
own per-draw granularity, the table above now keeps seven of its eighteen ratable cells
as survivors — every propagated cell on HPEPA3 and HMX, plus PSAN02n's `fmdok` — and
rules the mechanism out only on the rest (P33 and `inpt` in full; PSAN02n's `pdoksmall`
via Vdokstr, structurally, and, inconclusively, its `Dok43all[1]`).

⚠ 2026-09-21, later the same day: was "the REAL*4-summation candidate is dead on
complete evidence everywhere in this chain except the two cells the table above already
keeps as survivors (HMX's `fmdok`, large enough to matter; HPEPA3's `fmdok` and HMX's
`pdoksmall` via `Vdokstr`, inconclusive)" — computed from the 2026-09-20 worst-cell
epsilon, itself a lower bound wearing the clothes of a measurement
(`src/Particle/BOOT.md`, "## Accumulators", same date's own withdrawal). Re-measured at
the original's own one-rounding-per-drawn-particle granularity: seven of eighteen
ratable cells survive, not two, and the candidate is alive on HPEPA3 and HMX in full and
on PSAN02n in part → `HISTORY.md#accumulation-consequence-worst-cell-2026-09-20-superseded`.

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells |
|---|---|---|---|---|---|
| 911–958 | algorithm | The category merge and shift (`## Invariants`, "In-place rewrites of the totals, as the original") rewrites `Qdoks`, `Dokp41`, `Dokp31` without clearing the old last row after any merge, so its contents count twice once later cycles rebuild the rows (`## Categories`: "The original behaves so; it is reproduced."). | not measured | reproduced | none |
| 1021–1065 | dead | `pdoksmall(1)`, never assigned by the original, carries garbage (0.338E-39 … 0.153E-37 in the archived outputs) copied forward by the clamp of lines 1062–1064 into leading cells while they are zero ("Decisions where the port departs from a transcription"). | measured: `rc166.m` prints six leading cells at 0.762E-38; reference values below 1e-30 in `pdoksmall` are treated as zero, recorded in the exclusion list of `tests/Fixtures` with this evidence. | declared, not reproduced | `*: pdoksmall where original < 1e-30` |
| 1386 | cosmetic | `call arrayprint('pdoksmall',pdoksmall,Ndok-1)` prints one element short of `pdoksmall`'s own `Ndok` dimension, unlike every other `Ndok`-sized array this node reports, which prints the full `Ndok` ("## Report", "Decisions where the port departs from a transcription"). | measured: the printed `pdoksmall` is `Ndok − 1` long on every one of the five reference formulations (32/33, 35/36, 32/33, 32/33, 70/71 against `fmdok`'s own `Ndok`-length print), confirmed against each archive's own `results.m` (`tests/Statistics.Tests/PdoksmallReportLengthTests`). | reproduced | none |
| 911–959 | numeric | The category merge's threshold comparison, `Epsydok(r) > EpsDok`, is computed and compared in REAL*4 by the original, which gates control flow: a REAL*4 rounding of either side could move a row across the merge threshold and change `DPRow` itself ("Decisions where the port departs from a transcription"). | not measured: the 2026-09-20 row counts (17/18, 59/58) are `Binary64`'s and come from the totals; `MergeThresholdSensitivityTests` shows the mechanism only. Under `Original` since 2026-10-01 `Epsydok` is narrowed at 875 and `EpsDok` is the setup plane's stored value, so both sides are the original's binary32 ("## Report", the merge-threshold bullet). | reproduced | none |
| 771–1175 | numeric | The per-cycle processing computes in x87 registers and keeps its values in REAL*4 homes, binary32 compiler temporaries and six-fold unrolled partial sums. What it does at each site is read off the executable's own listing, by a checked address map and a generated table, not from the statement structure ("## Report", "The per-cycle plane from the executable's listing"): stores that round, values kept in a register to their last read, homes re-read after a store, temporaries, sums stored every sixth pass and every remainder pass, product order, `**4.`, `**3.` and `**2` as products, literals folded in binary32; line 378's temporary of the setup plane is the same reading ("## Setup plane"). The register-lifetime rules this row and its neighbour held from 2026-09-27 to 2026-10-02 were refuted at the sites of "## Report" and replaced → `src/Statistics/HISTORY.md#defect-rows-771-1175-open-2026-10-03` | measured 2026-10-03: `ListingOracleTests`, an x87 interpreter running the executable's own bytes, finds `Compute` and `Setup.Prepare` bit-equal to it on every output of every cycle and every pre-loop value of its 26 cases, three of them pinned where `DOKM`'s five-fold schedule alone decides (`BK10`, `KB397`, `AK157a`; empty ratchet); `da_coef` prints the original's on 22 of 22 archived formulations (C1, `LegacyDaCoefReproductionTests`); 295 of 295, 2016 of 2016 and 293 of 293 archived outputs (`PerCyclePlaneOutOfSampleControlsTests`); the 39 seed-0 cases unmoved on the regenerated runs (`CyclePlaneSeedZeroControlsTests`); on six path-identical pairs 551 cells still print a different token, attributed by class, not measured per cell (`ACCEPTANCE.md` A3): by the design 15 are the print plane's (`src/Output`) and 536 are fed by the attempt plane's REAL*4 inputs `Dr`, `Db` and `RK` (`src/Particle`), none required to reach zero, the list moving by four cells when the plane followed the listing (C2, `PathIdenticalPairsReportTests`, `ACCEPTANCE.md` A3); the rate 17 of 160 against the original's own 15 of 197, p = 0.103, with every failing cell of the 160 `Original` runs unchanged (147 of them differ in bytes) and `Binary64` byte-identical (A6, `tests/Harness.Tests/RateCriterionTests`) | reproduced | none |
| 379–406 (`JZZ = 2`) | numeric | The `JZZ = 2` branch of the `DOKM`/`DOKSD` loop is unrolled four-fold and keeps intermediates in REAL*4 compiler temporaries the listing stores: `u − l` in pass 1 of a block and in every remainder pass, `l**5` every pass, `DOK4`/`DOK3` every pass, `DOKSD` after passes 1–3 of a block ("## Setup plane": the schedule is typed, read from the listing; `ACCEPTANCE.md` A12). | measured 2026-10-03 by the oracle's pre-loop run: over the 49 shipped files `CSPX04` was the one that parted (`DOKM` 0x393BAB05 executed against 0x393BAB06 before, `DOKSD` 0x320F1DE8 against 0x320F1DE3), now none does (`tests/Statistics.Tests/PreloopSurveyTests`, `PreloopSurvey.approved.txt`, empty); on 120 drawn loops of one to sixteen fractions the executable and the listing's stores agree on every bit and each store turned off parts from it on 8 to 27 (`cycle_plane_oracle.py selftest`); the five reference formulations are untouched (`P33` is their one `JZZ = 2` file and agreed before) | reproduced | none |
| 875–879, 975–981, 996, 1016, 1025, 1097, 1105 | numeric | Three runtime routines the listing calls were not read: `_FIIfexp_` (DFORRT) behind `**0.3333` at 1016, `_CIsqrt` (MSVCRT, a system DLL the archive does not ship) behind `_FIsqrt`, the `SQRT` and `**0.5` of 875–879, 975–981, 996, 1097 and 1105, and `_FXAMOD` (DFORRT) behind `mod` at 1025. The port computes `Math.Pow`, `Math.Sqrt` and `a − ⌊a/b⌋·b`, and the oracle stubs the three with the same calls (`CyclePlaneListing.map.txt`, the `call` rows of its `oracle` section), so no check sees a difference there; a correctly rounded square root at 53 bits and an exact `mod` of positive operands are assumptions, a power of a binary32-rounded product showing only at a binary32 tie ("## Report", declared residuals). | not measured | declared, not reproduced | none |
| 261–277, 825–829, 1021–1065 | numeric | `Ddokmax`, `AK4`, `Di` and `Dj` are REAL*4 in the original; `Ndok`, `Nkarm`, `Ncat` are the exact truncated quotients of those binary32 values plus 2, computed with integer mantissa arithmetic, and the same treatment covers `Categories.cs`'s own `DPRow` (line 825) and `SmallParticles.cs`'s own whole-fraction count `int(real(kilo)/ak2)` (`## Invariants`, "Array sizes from binary32 values"). | measured: printed lengths `Ndok` = 71 (C166, T56), 23 (CSPX01), 20 (P18050), 62 (PSAN01), which `double` operands miss; the worst-case `DPRow` quotient sits no closer than 1.68e-5 from an integer boundary (HMX, C166, T56) against a binary32 relative epsilon near 6e-8, so no printed length moves; every formulation's own `AK2` is an exact integer (2.0), so the whole-fraction count carries no rounding risk for the archive. | reproduced | none |
| 718, 963–996 | numeric | `QKS1` is stored in REAL*4 at line 718 of the particle loop, `QKS/real(QKSS)`, and read by the plane; the executable loads `QKSS` (INTEGER*8) exactly with `fild qword` and divides each cell by it, rounding only the quotient (0x40B7FA–0x40B8AF, read from the excerpt, not run), which `CycleStatistics.Pockets` computes as `r32(q / QKSS)` and the oracle injects as `f32(q / QKSS)`. The reading before 2026-10-03, `f32(q / f32(QKSS))`, was a different expression above 2^24 ("## Report", declared residuals). | measured 2026-10-03: the injection and `Compute` agree bit for bit on every cycle of the oracle's cases, and at 2^24 + 1 `Compute`'s cells are the exact quotient rounded once and one binary32 unit from the rounded-divisor reading (`tests/Statistics.Tests/Qks1NormalisationTests`); 110 of the 175 archived outputs print a `QKSS` above 2^24, among the references HMX 42 596 514 and HPEPA3 24 649 481, where the rounded-divisor reading would have moved 56 % and 48 % of the cells by a unit; nothing moves | reproduced | none |
| 792–797, 1704, 1716, 1723, 1725, 1727 | numeric | `epsdokfr`/`epsalldok(kilo)` mixes REAL*4 (`GDOK`, `DDOK`, `ZS`, `epsalldok`) with REAL*8 (`ZX`/`Z`) in the fraction-share normalization and the accuracy comparison, so the mathematically-exact-zero case `NMM = 1` does not cancel to zero (`## Report`, "Decisions where the port departs from a transcription"). | measured: `epsdokfr[0]` is bit-for-bit `0.270E-07` across the `inpt` reference run and all 32 lagged/independent replicas (`tests/Fixtures/references/inpt/results.m.txt`, `tests/Fixtures/replicas-{lagged,independent}/inpt/*.m.txt`) — unchanged despite each replica's different accepted-particle and attempt counts, ruling out an accumulated-count cause — and independently `0.263E-07` on `p350` (`tests/Fixtures/Legacy/outputs/r4.m.txt`, unrelated fraction bounds and run size); both values sit at the scale of one REAL*4 rounding unit near 1 (2⁻²⁴ ≈ 5.96e-8), the signature of a fixed precision floor rather than a formulation-specific coincidence. Beyond the cosmetic `epsdokfr` reading, the same REAL*4 chain (`ZS`, the literal-folded coefficients) moves the fraction thresholds `Z11`/`ZX` and λ themselves under `PrecisionKind.Original`, `Binary64` the default and unaffected ("## Setup plane"; `tests/Statistics.Tests/SetupPlaneTests`). | reproduced | none |
| `src/Particle/BOOT.md`, "## Accumulators", `Allvdok`'s own row | numeric | `Allvdok`'s REAL*4 saturation (`Particle`'s own table) reaches `Allvdokso`/"fmdok" and `Alldok432`/`Dok43all[1]` directly (789–819), and `pdoksmall` (1050–1053) through `gdokleft`, itself fed by the internal cumulative `fmdok` array Fortran's own `FMDOK` recurrence builds — never the printed "fmdok", a different, per-cell field ("## Report", "REAL*4 accumulation, carried through"). | Reproduced by `PrecisionKind.Original` since 2026-09-21; the figures below are `Binary64`'s. Re-measured 2026-09-21 at the original's own per-draw granularity (⚠ "## Report", "REAL*4 accumulation, carried through"; supersedes the 2026-09-20 figures below): against `StatisticalCriterion.Compare`'s own band for each formulation's worst reachable cell, `fmdok` is large enough to matter on three of five formulations (HPEPA3 1921%, PSAN02n 201%, HMX 3143%) and rules out on the other two (inpt 1.0%, P33 1.7%); `Dok43all[1]` is large enough to matter on HPEPA3 (403%) and HMX (111%), inconclusive on PSAN02n (15%), and rules out on inpt (0.1%) and P33 (0.6%) — table and method in `## Report` (`tests/Statistics.Tests/AccumulationConsequenceTests.AllvdokErrorPropagatesToAllvdoksoAndAlldok432`); `pdoksmall` via `gdokleft` is unaffected by this re-measurement, still 0% of band on all five formulations — an exact zero driven by the real Allvdok shape, not by the epsilon magnitude (`## Report`, `src/Statistics/HISTORY.md#gdokleft-allvdok-reachable-cell-bound`, `tests/Statistics.Tests/AccumulationConsequenceTests.AllvdokErrorPropagatesToPdoksmallThroughGdokleft`, `.AllvdokErrorPropagationThroughGdokleftIsWiredWhenTheReachableCellIsNonzero`). | reproduced | none |
| `src/Particle/BOOT.md`, "## Accumulators", `Vdokstr`'s own row | numeric | `Vdokstr`'s REAL*4 saturation (`Particle`'s own table) reaches `pdoksmall` (1021–1065) through `zdoksmall`/`ddoksmall`/`vdoksmall`, the dominant channel of the three quantities under suspicion; `VdokTotal`/`VdokTotal2` (1137–1152) reach only `DolM2`/`DolM3`, never `gdokleft`, `vdokleft` or `pdoksmall` — the chain handed down for verification named them on that path, and the port's own line map shows they are not ("## Report", "REAL*4 accumulation, carried through"). | Reproduced by `PrecisionKind.Original` since 2026-09-21; the figures below are `Binary64`'s. Re-measured 2026-09-21 at the original's own per-draw granularity (⚠ "## Report", "REAL*4 accumulation, carried through"; supersedes the 2026-09-20 figures below): against `StatisticalCriterion.Compare`'s own band for each formulation's worst reachable, perturbable cell, `pdoksmall` is now large enough to matter on HPEPA3 (587%, was: rules out at 1.8%) and HMX (2029%, was: inconclusive at 22.6%), rules out on P33 (2.1%), the channel is structurally closed for `inpt` (its own real distribution carries no draws inside the window `Ak2` lets the model read), and for PSAN02n reaches only the array's own dropped, unprinted tail cell, leaving nothing to explain for any printed cell — table and method in `## Report` (`tests/Statistics.Tests/AccumulationConsequenceTests.VdokstrErrorPropagatesToPdoksmall`, `.VdokTotalAndVdokTotal2DoNotReachPdoksmallFmdokOrDok43all`). | reproduced | none |
| 671–703 | numeric | `DPmax_cor`'s own per-attempt update (`Particle`'s own accumulator, `Particle/API.md`, "Real-valued record") is gated by two `pdoksmall`-dependent checks ("Var#1"/"Var#2", lines 683, 699) inside the same attempt that updates the unconditional `DPmax` (line 673, no gate); a `pdoksmall` value that differs from the original's — the row above — flips whether one of the rare largest-radius pockets clears the gate, without ever touching `DPmax` itself ("## Report", the resolved escalation). | Reproduced by `PrecisionKind.Original` since 2026-09-21; the figures below are `Binary64`'s. Measured: HMX's own archived `fmkarm_cor`/`fmkarm_cor2`/`fqkarm_cor` print at 88 elements (`int(DPmax_cor/Di) + 2`) against the port's 82 — six cells, three orders of magnitude past a rounding-boundary flip; absent on the other four reference formulations, where the port's `DPmax_cor`-driven length matches the archive exactly (`tests/Output.Tests/PocketPrintLengthTests`). Confirmed, not merely fitted, by a dose-response measurement on HMX at `N`, `N/10`, `N/100` (`--n` override, `run_original.py`/`run_port.py`): the length matches exactly (56/56, 77/77) at the two reduced `N` where `pdoksmall`'s worst considered cell is within 0.24%/3.1% of the original, and misses only at the shipped `N`, where that cell has diverged 64.6% — the length parts company exactly where `pdoksmall` does ("## Report" above, the dated dose-response table). | reproduced | none |

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No reading of per-particle records: only the run's totals.
- No formatting, no µm scaling of output.
- No clearing or copying of the totals behind the caller's back: the in-place rewrites
  of `## Invariants` are the only writes.
