# HISTORY.md — PropStruct (tree root)

Append-only store of what `BOOT.md` no longer needs to state as current truth
(`AGENTS.md`, §15). Newest entry first. Every entry names the date it was moved, the
`BOOT.md` section it came from, and carries the original text in full, unedited. The
start procedure (`AGENTS.md`, §10) does not read this file; it is reached only by
following a dated pointer left in `BOOT.md` at the place the text used to stand.

---

<a id="repository-public-2026-10-04"></a>

## 2026-10-04 — from "## Constraints", "Repository" — private, nothing published

Replaced when the owner decided the delivery (`## Delivery` of the root `BOOT.md`):
the repository becomes public on GitHub as `baryon-asymm/PropStruct` under MIT, with
releases, and its public history begins with one snapshot commit. The bullet as it
stood at 75bc2c4:

>   - **Repository:** git, branch `main`, work on `claude/<phase>` branches merged by the
>     owner, Conventional Commits, English everywhere, nothing published on the owner's
>     behalf without being asked. There is no external ancestor; the loader `CLAUDE.md`
>     carries no subject-matter claims (AGENTS.md §2).

---

<a id="delivery-decided-2026-10-04"></a>

## 2026-10-04 — from "## Purpose" and "## Dependencies" — the archive in the tree, packaging a not-goal

Replaced when the owner decided the delivery on 2026-10-04: the original program is kept
outside the repository (directory named by `PROPSTRUCT_LEGACY_DIR`, identified by
`tools/legacy`), and packaging and publishing are goals of version 1 (`## Delivery` of
the root `BOOT.md`). Three texts as they stood at 75bc2c4.

From "## Purpose", the sentence naming the archive:

>   program PropStructV3 (Digital Visual Fortran 6, 32-bit, build of 2015-03-18; archive
>   `legacy/PropStructV3.zip`), rebuilt so that one numerical program runs on the CPU and,
>   whole cycles of base particles at once, on an NVIDIA GPU in double precision.

From "## Purpose", the sentence of the not-goals of version 1:

>   REAL*4-sensitive branch; interactive input; graphics; packaging and publishing; any
>   change of the model itself. Defects of the original are reproduced or declared, never
>   silently fixed.

From "## Dependencies", the paragraph "Outside the tree":

>   Outside the tree: .NET SDK 10.0 pinned by `global.json`; ILGPU 1.5.3 (NuGet), with the
>   libdevice post-link taken from APThermo
>   (`C:/Projects/AerospacePropellantThermodynamics/src/Execution`); for the GPU path an
>   NVIDIA driver and libnvvm/libdevice from CUDA Toolkit 12.8 or newer; xunit;
>   BenchmarkDotNet; Python 3.8+ for the protocol linter and the fixture generator
>   scripts; the original `PropStructV3.exe` with `dforrt.dll` (32-bit Windows) as the
>   generator of reference outputs.

---

<a id="precision-kind-setup-plane-stage-5b"></a>

## 2026-10-03 — from "## Invariants", "Precision kind" — the setup plane before stage 5b

Replaced when stage 5b (`src/Statistics/ACCEPTANCE.md` A10 and A12) executed `PARAM` in
the listing oracle and read the `JZZ = 2` loop's power expansions and temporaries off
the listing: the setup plane's set was stated as the values declared or typed
implicitly, their stores and a sum's schedule, and the typed part as "the store
schedule of lines 379–406" alone; it is now also the temporaries of lines 378–406, the
products and powers as the listing forms them, and what lines 379–406 and `PARAM` take
from a reading of the listing is the declared deviation of `src/Statistics/BOOT.md`,
"## Setup plane". Lines 90–95 of the root `BOOT.md` as they stood at a4c65de, the
setup plane's bullet:

>   - **the setup plane**: every value computed once per run before the particle loop and
>     held in REAL*4, declared or by implicit typing — the formulation's values read and
>     converted in binary32 from what the `.dat` says (`Input`'s `Length.AsWritten`, whose
>     contract does not change), the literal expressions of those lines folded in binary32,
>     every store rounded, except a REAL*4 sum of REAL*4 terms, stored on the schedule
>     of the executable's listing (`src/Statistics`, "Setup plane");

and lines 101–108, the paragraph on the generated sets:

>   All three sets are **generated**, never typed by hand (AGENTS.md §6), but for the
>   store schedule of lines 379–406, a declared deviation (`src/Statistics`, "Setup
>   plane"): two from the source's declarations and statement structure, the third from
>   the executable's listing by a checked address map. The setup plane's values are
>   data: every consumer, the attempt plane included, receives them as stored. The
>   **attempt plane** — sizes drawn, distances, decision variables — computes in
>   `double` under both kinds, so this reproduces the original executable's binary32
>   values, not its sample path, and the name says so.

---

<a id="speed-criterion-2026-10-02"></a>

## 2026-10-03 — from `ACCEPTANCE.md`, the speed criterion — superseded by the W5 ship window

Replaced when CUDA's `Kernels.RunAttempts` moved to groups of 64 threads (W5) and
`tests/Benchmarks` re-measured every path at that shape (commit 531d66c). The body as it
stood:

> - [x] 2026-10-02: speed of batched CPU and CUDA against the original on HPEPA3 and HMX
>       is re-measured at the shipped default budget 8192 in `tests/Benchmarks` ("##
>       Figures", W2), commit 7e8f6f7, the machine quiet before, between and after, each
>       row carrying its provenance and its budget, launch and attempt counts. Cycle 1,
>       accepted particles/s, mean ± sample spread, n = 3: HPEPA3 original 7275 ± 147,
>       host16 47685 ± 178, CUDA 43503 ± 88; HMX original 313 ± 4, host16 2071 ± 18,
>       CUDA 5095 ± 2. R0 is met at the default (CUDA HMX 5095 against 0.8 × host16
>       2071 = 1657). On HPEPA3 CUDA stands below host16; on HMX CUDA leads it. The
>       pre-registered stop fired on HMX and was explained by an A/B/A against E-B's
>       commit (variation between invocations exceeds the within-invocation spread;
>       `tests/Benchmarks/HISTORY.md#figures-w2-2026-10-02`).

---

<a id="speed-criterion-2026-09-28"></a>

## 2026-10-02 — from `ACCEPTANCE.md`, the speed criterion — superseded by W2 at budget 8192

Replaced when `tests/Benchmarks` re-measured at the shipped default budget 8192 (W2,
commit 7e8f6f7). The criterion's body and the 2026-10-01 note under it, as they stood:

> - [x] 2026-09-28: speed of batched CPU and CUDA against the original on HPEPA3 and HMX
>       is re-measured in `tests/Benchmarks` ("## Figures", Stage 0 of "## Post-acceptance
>       plan"), the machine verified quiet before, during and after, each row carrying its
>       provenance and, new this round, its budget/launch/attempt counts. Cycle 1,
>       accepted particles/s: HPEPA3 original 7399, host16 44211, CUDA 35315; HMX original
>       318, host16 1822, CUDA 397 — the collapse persists (R0: CUDA HMX is far below
>       0.8 × host16 HMX, outside both spreads), so Stage 1 (a profile, not a hypothesis)
>       follows before any kernel change. HPEPA3 CUDA's drop, 47405 → 35315, is the
>       stream layout, not the code: the 2026-09-20 row ran `Original`, today's runs
>       `Independent`, the default since d735f3e; forcing `Independent` on the last fast
>       commit reproduces 35263 ± 118 (`tests/Benchmarks/BOOT.md`, the bisection).
>
>   ⚠ 2026-09-28, later: was "HPEPA3 CUDA also regressed on its own … its cause is not
>   measured", now bisected to the layout default; no code regression is in evidence.
>
>   2026-10-01: at the new default budget 8192, E-B measured HMX CUDA 5042 ± 29 against
>   host16 2085 ± 5 particles/s, so R0 is met (`tests/Benchmarks/BOOT.md`, E-B).

---

<a id="per-cycle-listing"></a>

## 2026-10-02 — from "## Invariants", "Precision kind" — the per-cycle plane as source-read rules

Replaced when the owner decided (2026-10-02) to reproduce the executable's own behaviour
over the whole plane, line 378 included, from its listing: the plane's third set is no
longer a reading of the statement structure but what the listing does at each site,
found by a checked address map and a block-local x87 stack reader over a committed
excerpt (`src/Statistics`, "## Report"; `tests/Fixtures`, "## Cycle-plane listing"). The
bullet and the three lines that followed it, as they stood:

> - **the per-cycle plane** (Fortran 771–1175): a REAL*8 or integer store is rounded
>   where stored, an integer count converts exactly, literal expressions are folded in
>   binary32; what stays in a register is a reading of the statement structure that the
>   executable's listing contradicts at the sites of the `open` row 771–1175 there.
>
> All three sets are **generated** from the source's declaration block, implicit-typing
> rule and statement structure, never typed by hand (AGENTS.md §6); `src/Statistics`
> states which clauses the archive measures and which the model extends. The setup

---

<a id="per-cycle-register-clauses"></a>

## 2026-10-02 — from "## Invariants", "Precision kind" — the per-cycle plane's two register clauses refuted by the executable's listing

⚠ 2026-10-02: was the per-cycle plane's third set stated as what the executable keeps
in its registers and homes: "a REAL*4 value re-read in its own block, or summed through
its loop, stays unrounded until it is handed on, and what the plane hands to a later
block, the print or the next cycle is rounded"; now that clause is a reading of the
statement structure, which the executable's listing contradicts at the sites of the
`open` row 771–1175 of `src/Statistics/BOOT.md`. Found 2026-10-02 by disassembling
`tests/Fixtures/Legacy/PropStructV3.exe` (`dumpbin /disasm`, 14.12): 798 `ALLVDOKS`,
990 `D243` and 1004 `gdoksfr` are never stored where the clause rounds them, 989 `D432`
and 991 `Dqkarm` are stored back every sixth pass of an unrolled loop, 1006, 1008 and
1150 are re-read from their homes where the clause keeps them unrounded; the addresses
and the sites that agree are in `src/Statistics/BOOT.md`, "## Report", the ⚠ of that
date. The same note moved the 2026-10-01 pointer's own evidence clause here: it read
"storage refuted: 795's quotient re-read unrounded in 295 of 295 archived outputs,
`DOKM` summed unrounded", and that refutation of plain storage stands. The text as it
stood, the bullet:

> - **the per-cycle plane** (Fortran 771–1175): a REAL*8 value stored to REAL*4 is
>   rounded where it is stored; an integer count converts exactly; literal expressions
>   are folded in binary32; a REAL*4 value re-read in its own block, or summed through
>   its loop, stays unrounded until it is handed on, and what the plane hands to a later
>   block, the print or the next cycle is rounded (`src/Statistics`, "## Report").

and the 2026-10-01 pointer, in full:

> ⚠ 2026-10-01: was the values the Fortran stores in REAL*4, two places, the per-cycle
> plane `double`, now three; storage refuted: 795's quotient re-read unrounded in 295 of
> 295 archived outputs, `DOKM` summed unrounded → HISTORY.md#precision-kind-values

---

<a id="purpose-executable-values"></a>

## 2026-10-01 — from "## Purpose" — `Original` as the values stored in REAL*4, in two places

Replaced when the per-cycle plane followed the precision kind (the arbiter's verdict of
2026-09-28 on the per-cycle design, amendment A8, applied within the root's 400 lines by
the arbitration of 2026-10-01; its controls green the same day). The text as it stood:

> Every run carries a **precision kind** (see `## Invariants`): `Binary64`, the default,
> computes what the original would have computed without its own rounding loss; `Original`
> reproduces the values the original stores in REAL*4 — its accumulators and its setup
> plane — and so reproduces the original's printed answers up to the declared differences,
> the last printed digit of the per-cycle values among them. Both are the same program,
> and each run says which it was.

---

<a id="precision-kind-values"></a>

## 2026-10-01 — from "## Invariants", "Precision kind" — the REAL*4 storage in two places, the per-cycle plane `double`

Replaced when the per-cycle plane followed the precision kind (the arbiter's verdict of
2026-09-28 on the per-cycle design, amendment A8, applied within the root's 400 lines by
the arbitration of 2026-10-01; its controls green the same day). Plain storage is
refuted twice by the archive: 795's quotient is re-read unrounded in 295 of 295 archived
outputs, and `DOKM` is summed unrounded, PSAN02n's `epsalldok` fitting it in 49 of 49
outputs against 15 of 49 for the per-addition sum
(`tests/Statistics.Tests/PerCyclePlaneOutOfSampleControlsTests`). The per-cycle
paragraph's "decided after acceptance" was overtaken by that verdict. The text as it
stood, from the bullet's head to the end of its per-cycle paragraph:

> - **Precision kind is an option of every run, and `Binary64` is the default** (decided
>   2026-09-21; extended to the setup plane and renamed 2026-09-23; `Double` renamed
>   `Binary64` on 2026-09-24 for CA1720, map in
>   `tests/precision-kind-rename-2026-09-24.txt`). `Binary64` computes in `double`, as the
>   port always has. `Original` reproduces the values the Fortran **stores in REAL*4**, in
>   two places and nowhere else:
>
>   - **the accumulators**: rounded to binary32 after every addition, exactly those the
>     Fortran declares REAL*4 (`src/Particle`);
>   - **the setup plane**: every value computed once per run before the particle loop and
>     stored in REAL*4, declared or by implicit typing — the formulation's values read and
>     converted in binary32 from what the `.dat` says (`Input`'s `Length.AsWritten`, whose
>     contract does not change), the literal expressions of those lines folded in binary32,
>     sums rounded per addition (loop-invariant ones once at the loop's exit), every store
>     rounded (`src/Statistics`, "Setup plane"; ⚠ 2026-10-01: was "per addition" for all).
>
>   Both sets are **generated** from the source's declaration block and its implicit-typing
>   rule, never typed by hand, for the reason AGENTS.md §6 gives for any criterion quantified
>   by "all". The setup plane's values are data: every consumer, the attempt plane included,
>   receives them as stored. The **attempt plane** — sizes drawn, distances, decision
>   variables — computes in `double` under both kinds, so this reproduces the original's
>   REAL*4 *storage*, not its executable, and the name says so.
>
>   The per-cycle plane (Fortran 771–1175) is neither: it computes in `double` under both
>   kinds, and what its REAL*4 stores change in the printed output is declared (decided
>   2026-09-27; `src/Statistics/BOOT.md`, row 771–1175). Extending the kind to it is
>   decided after acceptance, whole or not at all.

---

<a id="literals-per-cycle-plane"></a>

## 2026-10-01 — from "## Constraints", "Fidelity to the original" — literals, the setup plane's expressions only

Replaced when the per-cycle plane followed the precision kind (the arbiter's verdict of
2026-09-28 on the per-cycle design, amendment A8, applied within the root's 400 lines by
the arbitration of 2026-10-01; its controls green the same day). The text as it stood:

>   - literal constants stay as written where they stand: `3.14159` (sphere volume),
>     `3.14` (inside the bridge volume), `12.56636` (full solid angle); the REAL*4
>     rounding of literals is not reproduced, except the literal expressions of the setup
>     plane under the `Original` precision kind (decided 2026-09-23, "Precision kind" above),
>     which the original folds in binary32; the attempt plane's literals stay as written
>     under either kind;

---

<a id="section-15-deviation-lifted"></a>

## 2026-09-28 — from "## Constraints", "Repository" — the §15 deviation, lifted (AGENTS.md 3.1)

Moved when the deviation was lifted (AGENTS.md 3.1, decided 2026-09-27): the root kept
regrowing past it (520 non-blank lines by the review that decided the split, +25 in
under a day), and nothing in the deviation gave it a target to lift at. The text as it
stood:

> ⚠ Declared deviation, §15: this document is over the 250-line limit for a node with
> children; the linter prints the count, not written here. What holds it over is the
> current truth of the six canonical sections and the pointers §15 never lets move.
> Lifts only on a methodology decision by the owner of `AGENTS.md`: a child node or a
> `tests/` node is not a route, since the root is the one common ancestor these
> sections bind (AGENTS.md §3).
>
> ⚠ 2026-09-26: was "every ⚠ correction that could move has moved", with section
> figures of 2026-09-20, now the moves of 2026-09-26 →
> HISTORY.md#section-15-deviation-2026-09-20

The methodology decision the deviation asked for is AGENTS.md 3.1: the root's
acceptance criteria move to `ACCEPTANCE.md`, and the root's own `BOOT.md` limit rises
from 250 to 400 once it uses the pointer. Size after the split: root `BOOT.md` 529 →
392 non-blank lines (620 → 460 total), `ACCEPTANCE.md` holds 136 non-blank lines
(161 total).

---

<a id="benchmark-figures-2026-09-20-superseded"></a>

## 2026-09-28 — from `ACCEPTANCE.md`, the speed criterion — the 2026-09-20 figures

Moved when Decision B's Stage 0 re-measured the same command on the same machine:
twelve commits had touched `Attempt.cs`, `Engine.cs` or `Kernels.cs` since the commit
these figures came from, the `Original` precision kind among them, so
`tests/Benchmarks`' own taboo ("re-measure or delete the row") applied to the root's
citation of them too. The text as it stood:

> - [x] 2026-09-20: speed of batched CPU and CUDA against the original on HPEPA3 and
>       HMX is recorded in `tests/Benchmarks` ("## Figures"), the machine verified
>       quiet, each row carrying its provenance, reproduced by a second run inside its
>       own spread. Cycle 1, accepted particles/s: HPEPA3 original 7041, host16 49346,
>       CUDA 47405; HMX original 321, host16 1954, CUDA 394, a collapse not yet
>       explained.

Replaced by the Stage 0 re-measurement (`tests/Benchmarks/BOOT.md`, "## Figures" and
"## Post-acceptance plan"): HPEPA3 original 7399, host16 44211, CUDA 35315; HMX
original 318, host16 1822, CUDA 397. R0 found the HMX collapse unchanged; Stage 1
(profiling, not a hypothesis) follows.

---

<a id="performance-2026-09-27"></a>

## 2026-09-27 — from "## Constraints", "Performance" — the pre-precision-kind figures and the divergence/tails hypothesis

Moved when Decision B (2026-09-27) found the recorded figures stale by the
Benchmarks node's own rule (twelve later commits touch `Attempt.cs`, `Engine.cs` or
`Kernels.cs`, among them the ones that brought the `Original` precision kind into the
particle program) and found that "the kernel is bound by divergence and tails …, not
arithmetic" had been carried as "Measured", though no profile of either kind exists
anywhere in the tree. The text as it stood:

> **Performance:** CUDA and CPU batched mode are measured against the original
> (HPEPA3: 27 s single thread) in `tests/Benchmarks`, recorded and never asserted.
> Measured 2026-09-19: CUDA runs 39,000 – 43,000 accepted particles/s on HPEPA3
> cycle 1, about the same as 16 host threads in a Release build; the kernel is
> bound by divergence and tails (branch-heavy, variable-length loops, FP64 at
> 1/64 rate on a GeForce), not arithmetic. Decided the same day: no restructuring
> of the kernel before the port is accepted; after acceptance, a profile and three
> cheap experiments (the per-launch attempt budget, a field-major record layout,
> the frozen QKS1 in constant memory) recorded in `tests/Benchmarks` decide what,
> if anything, follows.

Replaced by a plan fixed before any new figure exists (`tests/Benchmarks/BOOT.md`,
"## Post-acceptance plan"): re-measure first (Stage 0), profile next (Stage 1), and
run only the experiment its own rule selects — the three named above are no longer
assumed to be the right three.

---

<a id="rate-figures-e1-e2-2026-09-27"></a>

## 2026-09-27 — from "## Acceptance criteria", the null-rate and rate criteria — figures before E1, E2 and F-a

Moved when the two criteria were re-ticked on the figures measured after E1 (the
feasible mass-quantum interval), E2 (the adaptive comparison's common range) and F-a
(the upper tail summed directly); `tests/Harness/HISTORY.md#e1-mass-bracket-feasible-interval`,
`#e2-adaptive-common-range` and `#f-a-p-value-floor` have the derivations and keyed
diffs. The criteria as they stood, and the corrections stacked under the second:

- [x] 2026-09-24: the original's own null rate against its replicas is measured, an
      input of the rate criterion below: **22 of 197 runs, 11.2 %** — 13/96 lagged and
      8/96 independent leave-one-out, 1/5 archived references against all `R` of the
      lagged layout (`tests/Harness.Tests/NullRateCalibration`; `tests/Harness/BOOT.md`,
      "## Null rate of the original").

- [x] 2026-09-24: Reference mode under `Original` fails the statistical criterion no
      more often than the original fails it against its own replicas (the rate decision
      under "Statistical reference criterion"). Port: **23 of 160 runs, 14.4 %**,
      sixteen seeds `k·2¹⁶` per formulation and layout. A one-sided exact binomial test
      against 11.2 % gives p = 0.124, so no excess at `α`. Per formulation, reported and
      not gated, the smallest p is P33's 0.022. Positive control: `Binary64` fails 121
      of 160, rejected on HPEPA3 and HMX at p = 1.7·10⁻¹⁵. P33 under `Binary64` fails 7
      of 32, fewer than under `Original`, so on that formulation the test has no power
      to see the precision kind (`tests/Harness.Tests/RateCriterionTests`, reading
      `tests/Fixtures/rate-table.json`, which is tied to the criterion's source and the
      seed-0 snapshot).

  ⚠ 2026-09-27 (AGENTS.md §8, reviewer's calibration memo): `p = 0.124` above is
  nominal, not exact — the binomial treats the 160 port runs as independent, but 9 of
  the 23 failing ones share a failing cell with another run of the same replica pool
  (`tests/Harness/BOOT.md`, "## Null rate of the original", has the count and the
  false-failure classification). It also counted a known rule defect in the original's
  null, the mass bracket: reported that day, with it not gating, as an upper bound on
  the fix of 17/197 and 21/160 and p = 0.036.

  ⚠ 2026-09-27, later the same day: E1 landed (the feasible mass-quantum interval,
  `tests/Harness/HISTORY.md#e1-mass-bracket-feasible-interval`) and reached that bound
  exactly, not merely under it: measured **17/197 = 8.6 %** and **21/160 = 13.1 %**,
  `p = 0.0355303 ≈ 0.036`. The verdict is unchanged.

  ⚠ 2026-09-27, later still the same day: E2 landed too (the adaptive index-matched
  comparison's own common range, `tests/Harness/HISTORY.md#e2-adaptive-common-range`),
  removing every one of the 7 null and 6 port failures the two fixes caused together:
  **15/197 = 7.6 %** and **18/160 = 11.3 %**, `p = 0.0624644 ≈ 0.062`
  (`tests/Harness/BOOT.md`, "## Null rate of the original", has the full
  per-formulation figures and both regeneration records). The verdict is unchanged.

  ⚠ 2026-09-27, later still: `p = 1.7·10⁻¹⁵` above (and the memo's own `p = 4.11e-15`)
  was `double`'s own rounding floor at `1.0 - CdfAtMost`, not the true tail probability
  — the same floor for HPEPA3, HMX and `inpt` regardless of how far below it each true
  value sat (F-a, `tests/Harness/HISTORY.md#f-a-p-value-floor`). Fixed by summing the
  upper tail directly: measured `p = 1.63e-36` on HPEPA3 and HMX, 21 orders of
  magnitude smaller. The verdict is unchanged; only figures already near the old
  floor moved.

---

<a id="link-1-da-coef-2026-09-27"></a>

## 2026-09-27 — from "## Acceptance criteria", "The `Original` accumulation kind reproduces…" — link 1's own `da_coef` finding, told in full

Moved because the criterion's own link 1 text above the pointer already states the
correction (`da_coef` is now among the declared differences); this is the mechanism
and the measurement that decided it, not a fact link 1 still needs stated.

> ⚠ 2026-09-27: link 1's "every cell that differs is declared" is false in one cell.
> PSAN02n `da_coef` prints 0.7917714 under `Original` and 0.7917715 in all 49 outputs
> of the original, a constant difference over 32 seeds that the per-run criterion
> cannot see. Mechanism, measured: `CycleStatistics.Matrix` (Fortran 1001–1016) stays
> `double`, but the Fortran stores every value of that chain in REAL*4. Rounding each
> store reproduces 0.7917715. The per-cycle plane (771–1175) lies outside the
> precision kind by design (`src/Statistics/BOOT.md`, "## Setup plane"). Whether to
> extend the kind to it or to declare the difference is the owner's decision.

---

<a id="purpose-printed-answers-2026-09-27"></a>

## 2026-09-27 — from "## Purpose" — the precision kind's unqualified reproduction claim

Moved because the sentence above the pointer already carries the qualified wording
("up to the declared differences, the last printed digit of the per-cycle values
among them"); this is the prior, unqualified claim the correction replaces.

> `Original` reproduces the values the original stores in REAL*4 — its accumulators
> and its setup plane — and so reproduces the original's printed answers.

---

<a id="language-and-build-measurements"></a>

## 2026-09-26 — from "## Constraints", "Language and build" — the warning counts measured before the no-suppression and IDE0005 decisions

Moved because the bullet above the pointer already states the decisions themselves
(no suppression anywhere; IDE code-style rules raised, IDE0005 raised in `src` only);
these are the counts that motivated them, not facts the decisions still need stated.

> Measured before the decision: 581 warnings under 20 rules.

And, from the same bullet, the count behind the later IDE code-style decision:

> Measured before: 6738 warnings with all Style rules raised, 5403 of them one
> preference (`var`).

---

<a id="two-stream-layouts-sequential-measurement"></a>

## 2026-09-17 — from "## Invariants", "Two stream layouts" — the P33 measurement behind the sequential-only decision

Moved because the decision itself (the `Original` layout is sequential only) is
already the invariant's own current wording, above the pointer; this is the
measurement that decided it, not a fact the invariant still needs stated.

> Measured before the decision, P33, three seeds, the port against itself and against
> the original executable (55.407 for `Dkarm10`): sequential `Original` 55.360 ± 0.016
> and 2,811,164 for `Nkarm`; batched `Original` **54.877 ± 0.024** and 2,741,206;
> batched `Independent` 58.287 ± 0.005, agreeing with sequential `Independent` and with
> the executable's own independent replicas. The batched `Original` mean sits some
> thirty seed standard deviations from the sequential one — a different statistical
> program, not a tolerance question. Corroborated by the original's **own** generator
> diagnostic, which it prints for this purpose: `epsx(5)`, the deviation of the mean X3
> draw from one half, reads 5.3e-4 sequentially, 1.2e-3 batched `Independent`, and
> **9.8e-3** batched `Original`, where it does not shrink as the draws multiply. The
> mechanism above is a structural reading and is **not** established; the decision
> rests on the measurement, which is.

---

<a id="lint-criterion-reruns"></a>

## 2026-09-26 — from "## Acceptance criteria" — the lint criterion's box and its two stale re-run notes

Moved because the criterion above the pointer no longer states a warning count at all
(a count is exactly what kept going stale here); this is the superseded box and its
chronicle of re-runs, not a fact the criterion still needs stated.

> - [x] 2026-09-20: the tree passes `protocol_lint` without errors
>       (`python -X utf8 tools/protocol-lint/protocol_lint.py . --exclude templates`:
>       0 errors, 2 warnings, on the head of `claude/wave7`). The two warnings are the
>       §15 deviations this document and `tests/Harness/BOOT.md` declare, each named in
>       the linter's own output so the exemption stays visible.
>
>   ⚠ 2026-09-20: was ticked for a run with `--size` omitted, then unticked when that
>       flag ceased to exist and `tests/Harness` stood as the one error; the deviation
>       that node needed was written the same day, and the criterion is ticked again on
>       a run of the check as it now stands, with both checks unconditional.
>
>       Re-run 2026-09-24 on the head of `claude/wave7`: 0 errors, 4 warnings. The two new
>       warnings are the §15 deviations `src/Particle` and `src/Statistics` declared as
>       their transcriptions grew; each is named in the linter's output. None of the four
>       documents is inside its limit, and the audit of 2026-09-24 (D6) lists this as open.
>       The limit is not being met; it is being declared.
>
>       Re-run 2026-09-26: 0 errors, 2 warnings, this document and `src/Statistics`.
>       Audit item D6 brought `tests/Harness` and `src/Particle` inside their limits.

---

<a id="fidelity-real4-saturation-reproduced"></a>

## 2026-09-26 — from "## Constraints", "Fidelity to the original" — REAL*4 saturation of long sums was declared, not reproduced

Moved because the new wording above the pointer already states the current truth
(reproduced under `Original`); this is the wrong "declared, not reproduced" list this
tree carried until `src/Particle` reproduced the effect.

> - reproduced: side effects of rejected attempts; the QKS1 refresh rule above;
>   accumulators not reset between cycles; X3 and X4 sharing stream 6; in the
>   `Original` layout the correlated seeds (streams 6, 5, 4, 2, 1 start at a⁰, a¹, a²,
>   a⁴, a⁵, so X0 of attempt k+1 equals X of attempt k and X21 of neighbour k+1 equals
>   X2 of neighbour k; stream 3 carries the source's transposed limb 3784 where a³ has
>   3748); the `X1 == 1` retry; `KXX < 1` normalized to 1;
> - declared, not reproduced: `pdoksmall(1)` never assigned (garbage) → 0; the dead
>   test of Fortran line 529; `nn_total` uninitialized → 0; unused arrays and
>   commented output; REAL*4 saturation of long sums;

---

<a id="statistical-criterion-null-rate-wording"></a>

## 2026-09-26 — from "## Constraints", "Statistical reference criterion" — the pass condition's null-rate bullets before the lagged-only correction

Moved because the tightened bullets above the pointer already state the current
truth (the lagged layout only for the archived references, 23 of 160 for the whole
criterion); this is the wrong "both layouts" and "9 of 160" wording, and the figure
that made it wrong.

> - **the pass condition compares failure rates, not single runs** (decided 2026-09-24,
>   owner). One run of the original against its own replicas fails the per-run criterion
>   far more often than the declared 0.1 %: 13 of 96 leave-one-out runs. One run of the
>   port under `Original` fails it in 9 of 160 runs. A single run passing or failing is
>   therefore not evidence either way. The per-run bands, `α` and `R` stay as they are.
>   What is asserted is:
>   - the **null rate of the original**: the share of its runs with at least one failing
>     cell, over each replica against the other `R − 1` and each reference against all
>     `R`, both layouts, five formulations. Leaving one out widens the bands, which is
>     conservative against the port;
>   - the **rate of the port**: the same share over reference-mode runs under `Original`,
>     sixteen evenly spread seeds per formulation and layout, each against all `R`
>     replicas;
>   - the **pass**: a one-sided exact test finds the port's rate no higher than the
>     original's, at the tree's `α`, pooled and per formulation. The per-formulation
>     figures are reported, not gated;
>   - the **positive control**: the same test on `Binary64` runs rejects on HPEPA3 and HMX,
>     whose REAL*4 degradation is the known effect;
>   - the **seed-0 ratchet of known cells is replaced** by an exact snapshot of the seed-0
>     `results.m` under both kinds (time line aside), a change detector like the surface
>     snapshot. A change moves the snapshot and regenerates the port's rate runs in the
>     same commit, and a check ties the two together so that the rate cannot go stale.
>     This retires the three named cells (audit D5: they pinned seed-0 noise, and two of
>     them are measured false failures).

The 23/160 figure actually asserted was over the whole criterion (`Compare`, the
tail-row mean and the canonical-axis comparison together), not `Compare` alone, which
gives 9/160 — see `⚠ 2026-09-24, later` under the "`Original` accumulation kind
reproduces…" criterion's own chronicle, `HISTORY.md#original-kind-criterion-chronicle`.
And the archived references are compared in the lagged layout only: the archived
reference is that layout's own seed and has no such relation to the independent layout
(`tests/Harness/BOOT.md`, "## Null rate of the original").

---

<a id="performance-hmx-collapse"></a>

## 2026-09-26 — from "## Acceptance criteria" — the benchmark node's own note on the HMX CUDA collapse, before the Performance constraint carried a pointer at the claim itself

Moved because the corrected constraint now carries its own pointer next to "about the
same as 16 host threads", naming the HMX figures directly; this is the benchmark
criterion's fuller discussion of the same finding, not a fact the constraint still
needs stated in full.

> One figure deserves the reader's attention because it is new: **CUDA collapses on
> HMX**, 394 against 1954 for sixteen host threads, where on HPEPA3 the two are level at
> 47405 and 49346. The `## Constraints` note above records CUDA as "about the same as 16
> host threads", measured on HPEPA3 alone; that holds there and does not generalise. Why
> it collapses is unmeasured, and the constraint's own decision — no restructuring of
> the kernel before the port is accepted — stands.

---

<a id="section-15-deviation-2026-09-20"></a>

## 2026-09-26 — from "## Constraints", "Repository" — the §15 deviation paragraph, before this restructuring

Moved because the rewritten deviation above the pointer already states the current
reason and the current lift condition; this is the 2026-09-20 line-count accounting
that the restructuring of audit item D6 made obsolete.

> ⚠ Declared deviation, §15: this document is over the 250-line limit for a node with
> children. Its own length is not written here: the linter prints that count in the
> warning this paragraph exempts, and a copy of it in the text it measures moves every
> time the text does — this paragraph carried a figure one line stale within a day of
> being written (AGENTS.md §8, "numbers repeating the length of a list"). The section
> figures below are a measurement of 2026-09-20. Every ⚠ correction that could
> move has moved — eleven of them, oldest first, into `HISTORY.md`, each leaving a
> two-wording pointer — and none of them was holding this document over its limit by
> itself. What is left is the current truth of the six canonical sections, which §15
> never lets move: their sum alone, excluding this paragraph and `## Decomposition`, is
> 327 non-blank lines — `## Invariants` 62, `## Constraints` 129 (the statistical
> reference criterion, the known-bias table, the execution model and the defect-report
> rules the root owns), `## Acceptance criteria` 93 (nine criteria, several carrying
> their own measured evidence), `## Purpose` 19, `## Dependencies` 9, `## Taboos` 15.
> Narrowing any of these is a design-mode decision about what the tree root needs to
> state, not a coding-mode move of stale material, and it is not this task's to make (the
> size limits themselves are likewise not this task's to change). Lifts when a design
> session moves some of this content into a child node's own `BOOT.md`, or decides part
> of it is no longer needed.

---

<a id="decomposition-statistics-lines"></a>

## 2026-09-26 — from "## Decomposition" — `Statistics` transcribing only lines 771–1175

Moved because the current bullet above the pointer already says `Statistics`
transcribes the setup's lines too; this is the narrower claim audit item D6 found
stale once the setup plane became part of that node's own transcription.

> - `Statistics` is apart from `Particle` because it runs once per cycle on the host,
>   allocates, and transcribes a different part of the Fortran (lines 771–1175).

---

<a id="purpose-replay-reason-half-wrong"></a>

## 2026-09-24 — from "## Purpose" — byte-exact replay's reason, half wrong, then its HMX example withdrawn

Moved because the current truth (the REAL*4 storage reproduced, only the sample path
out of reach, no claim particular to HMX) already stands in the "Not goals" paragraph
above the pointer; these are the two corrections that got it there, not facts the
paragraph still needs stated.

> ⚠ 2026-09-21: byte-exact replay is still not a goal, but the reason recorded for it on
> 2026-09-17 — "x87 intermediates and REAL*4 make branch-exact replay unreachable" — was
> half wrong and is corrected here. The REAL*4 half is reachable and is now reproduced: the
> accumulators the Fortran declares REAL*4 are rounded per addition under the `Original`
> kind, and 91 of the 92 cells that failed the statistical criterion pass under it. What
> does not follow is the **trajectory** — HMX's integer counters do not converge on the
> original's — and reaching that would mean rounding the sizes and decision variables too,
> which is where the x87 half of the old reason still bites. So the old sentence named the
> right obstacle for the wrong scope.
>
> ⚠ 2026-09-24: the example above, "HMX's integer counters do not converge on the
> original's", is withdrawn as evidence of anything particular to HMX. Over sixteen seeds per
> arm, `NFQ`, `NFW` and the generator diagnostics do not differ between the two kinds or from
> the original (criterion "The `Original` accumulation kind reproduces…", link 2). Every
> formulation's sample path parts from the original's at its first REAL*4-sensitive branch,
> and each run is then another sample of the same estimator. The paragraph's conclusion
> stands: the trajectory is not reproduced, and that is not a goal.

---

<a id="two-stream-layouts-default"></a>

## 2026-09-24 — from "## Invariants", "Two stream layouts" — the default was still named `Original`

Moved because the current bullet above the pointer already names `Independent` as the
default; this is the architecture-audit finding that corrected it, not a fact the
bullet still needs stated.

> ⚠ 2026-09-24: was "`Original`, the default". The default has been `Independent`
> since 2026-09-21 (`src/Simulation/SimulationOptions.cs`): batched mode is the default
> mode and refuses `Original`, so an `Original` default would refuse every bare run.
> Found by the architecture audit of 2026-09-24.

---

<a id="statistical-agreement-under-original"></a>

## 2026-09-24 — from "## Invariants", "The port agrees with the original statistically" — stated without the precision kind

Moved because the current bullet above the pointer already qualifies the claim under
`Original` only; this is the reasoning that made the unqualified wording wrong, not a
fact the bullet still needs stated.

> ⚠ 2026-09-24: was stated without the precision kind. Since 2026-09-23 the criterion
> has been read as link 1 of that acceptance criterion, which runs under `Original`
> only, so the unqualified wording claimed agreement for `Binary64` runs, which are not
> tested against the original and are not expected to agree.

---

<a id="statistical-criterion-no-failure-condition"></a>

## 2026-09-24 — from "## Constraints", "Statistical reference criterion" — the archived reference's "no failure" condition, before it joined the null rate

Moved because the criterion no longer states a "no failure" condition for the archived
reference at all (the null-rate criterion below covers it); this is the reasoning that
retired it, not a fact the criterion still needs stated.

> ⚠ 2026-09-24: this condition is superseded by the rate comparison below. The original
> fails it in about one run in seven against its own replicas, so "no failure" on the
> single archived run is a coin with known odds and not a property of the original. The
> reference runs join the original's null rate instead.

---

<a id="null-rate-criterion-reformulated"></a>

## 2026-09-24 — from "## Acceptance criteria" — the null-rate criterion's box before it was shortened to the measured rate, and its reformulation note

Moved because the criterion above the pointer already states the measured rate (22 of
197 runs, 11.2 %) and its place; this is the superseded "no failure" box, its
now-obsolete "one cell open" reading, and the note that reformulated it.

> - [x] 2026-09-24, as reformulated in the ⚠ note below (the original's null rate is
>       measured, `tests/Harness.Tests/NullRateCalibration`). Original wording: the original's own reference output passes the criterion against its lagged
>       replicas on every reference formulation, with no failure
>       (`tests/Harness.Tests`:
>       `StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`).
>       Currently one cell open: HMX fails one tail-row cell (a count of 11 against
>       five all-zero contributing replicas); the rules for that cell's class are
>       being written in `tests/Harness/BOOT.md`.
>
>   ⚠ 2026-09-24: reformulated, not deleted (AGENTS.md §6). "No failure" on the one archived
>       run cannot be met by the original's own program: it fails its own replicas in about
>       one run in nine. On the owner's decision this criterion now reads **the original's
>       null rate is measured**, as an input of the rate criterion below. Measured 2026-09-24:
>       **22 of 197 runs, 11.2 %**. That is 13/96 lagged leave-one-out, 8/96 independent
>       leave-one-out, and 1/5 lagged reference against all `R` (HMX: `fqdokkarm(31,:)[7]`,
>       now at the same value as before, and `TailRowMean[67]`). The reference runs are
>       compared in the lagged layout only: the archived reference is that layout's own seed
>       and has no such relation to the independent layout (`tests/Harness/BOOT.md`, "## Null
>       rate of the original"; `tests/Harness.Tests/NullRateCalibration`, whose lagged count
>       reproduces 13/96 exactly).

---

<a id="superseded-per-run-reference-criterion"></a>

## 2026-09-24 — from "## Acceptance criteria" — the per-run reference criterion, superseded by the rate comparison

Moved because the rate criterion above the pointer replaces it entirely (the original
itself fails the per-run criterion in 11.2 % of runs, so "satisfies the criterion" on a
single run was never meetable); this is the whole superseded criterion, its box and
its chronicle of measurement, diagnosis and withdrawal, kept together because its
nested notes belong to one event.

> - [ ] (superseded 2026-09-24 by the criterion above) Reference mode in each layout satisfies the criterion against the replicas of
>       that layout on every reference formulation for every non-excluded quantity of
>       `results.m`; the exclusion list carries its evidence.
>
>       Measured 2026-09-20, failing cells of compared cells, `Original` / `Independent`:
>       HPEPA3 39/1822 and 37/1748; inpt 1/2493 and 1/2500; P33 0/1462 and 1/1456;
>       PSAN02n 6/2020 and 0/2046; HMX 46/4121 and 42/4417. HPEPA3's and HMX's counts
>       barely move between layouts, the signature of a systematic difference rather than
>       a seed effect; the exclusion evidence for those cells (the term count and bound of
>       the original's REAL*4 accumulation against the band width) is the first step of
>       the calibration work list in `tests/Harness/BOOT.md`.
>
>       ⚠ 2026-09-20, later the same day: was P33 `Independent` 0/1456, now 1/1456
>       (`fqkarm_cor[15]`). The figure was wrong when written, not overtaken: the cell fails
>       reproducibly on the criterion as it stood then and as it stands now. Found by the
>       re-measurement below, which had to reproduce the old figures to compare against them
>       and could not reproduce this one.
>
>       Re-measured the same day, after decisions VI to XX rebuilt the criterion's interval
>       rules, its rule assignment and its conditioning: **every one of these ten figures is
>       bit-identical, cell for cell.** Gate 1 is unchanged, gate 2 unchanged at 13 failing
>       runs of 96 and the same runs, and batched mode moves by one cell in one row of HMX
>       `Original`. A day of real defects — three of them sign errors inside interval
>       formulas, one a cell credited to a term that did not govern it, one a conditioning
>       refuted at `p = 0` against its own simulated null — changed the verdicts this
>       criterion delivers by a single cell.
>
>       Two readings follow, and the second is the work. That these numbers are stable under
>       a rebuilt measuring instrument raises rather than lowers the standing of the failures
>       they count: the cells that fail have now survived a criterion reconstructed around
>       them. And the verdicts evidently do not rest on the machinery that was rebuilt —
>       `threshold` is `max(studentTerm, staticFloor, countFloor)`, and a count floor that
>       never reaches the maximum decides nothing.
>
>       ⚠ 2026-09-20, later the same day: a diagnosis run over these cells attributed 167 of
>       the 173 to the original's REAL*4 long-sum accumulation, a declared defect, and that
>       attribution is **withdrawn**. It rested on a chain traced from a neighbouring node's
>       prose, and `src/Statistics` then checked that chain against its own line map and found
>       it wrong for two of the four accumulators: `VdokTotal` and `VdokTotal2` feed `DolM2`
>       and `DolM3` only and never reach `gdokleft`, `vdokleft` or `pdoksmall`. The error
>       survived because the internal cumulative `FMDOK` and the printed quantity `fmdok` share
>       a name and are different fields. With the real chain and the measured accumulation
>       error, of thirteen cells whose ratio can be computed ten rule the REAL*4 story out, one
>       — HMX's `fmdok` — is large enough to matter, and two are inconclusive
>       (`tests/Statistics.Tests/AccumulationConsequenceTests`,
>       `tests/Particle.Tests/RealFourAccumulationErrorTests`). The `Fmkarm` family is ruled out
>       separately, at two per cent or less of the replica spread.
>
>       So the failing cells are **unexplained**, on better ground than they were: two
>       mechanisms are measured and excluded, one is measured and real on a single formulation,
>       three cells belong to the category-merge gate and two to a normalisation where the port
>       is mathematically right. The exclusion evidence the criterion above waits on does not
>       exist for the bulk of these cells, and by the root's own taboo no exclusion may be
>       written without it.
>
>       Classified 2026-09-21 by dose-response in `N` (`tests/Harness/BOOT.md`, "Dose-response
>       classification of the 173 failing cells"): each cell measured in both programs at the
>       shipped `N` and at `N/10` and `N/100`, against classes stated before the data and
>       including one for a port-side defect. **170 of the 173 carry the same signature: the two
>       programs bit-identical or nearly so at the smallest `N`, the port's value then flat while
>       the original's moves and collapses toward the shipped `N`.** A converging estimate does
>       not fall as its sample grows, so the moving side is an accumulator that has stopped
>       accumulating, and it is the original's. Two cells are `inpt`'s `epsdokfr[0]`, the
>       already-declared REAL*4/REAL*8 mixing defect, confirmed as a constant gap at every `N`;
>       one, P33 `Independent`'s `fqkarm_cor[15]`, stands unexplained with no substitute story.
>       **No cell anywhere shows the reverse asymmetry**, so no port-side accumulation defect is
>       in evidence. Of the 27 cells the census found governed by the static floor, 25 share this
>       shape: that floor is a smaller scale of the same mechanism, not a second problem.
>
>       ⚠ 2026-09-21, later the same day: was this shape read as "an accumulator that has
>       stopped accumulating, and it is the original's". The second half stands and is now proven;
>       the first is withdrawn. **Which side is the anomaly is settled**: across four seeds the
>       original's decline holds at 24:1 to 59:1 against the seed spread, while the port's own
>       between-seed spread *shrinks* as `N` grows — a converging computation against a degrading
>       one, and a frozen value would not converge (`tests/Harness/BOOT.md`, "Seed-based
>       dose-response"). **What degrades the original is not known**, and four candidates are now
>       closed, each by its own measurement:
>
>       - REAL*4 summation rounding — Higham's forward-error bound accounts for six cells of 170,
>         and the chain feeding them is closed: every path traced from `src/Statistics`'s own line
>         map, aliasing ruled out, the remaining local sums bounded under 1e-5 relative, and the
>         last unquantified path, `Allvdok` through `gdokleft`, measured at zero with its wire
>         proven live. Dead on complete evidence, not partial;
>       - a single REAL*4 threshold crossing — refuted by 35 values of `N`: the decline is smooth
>         at every resolution, no step carrying more than about a ninth of it;
>       - a port-side defect — refuted by the multi-seed comparison above;
>       - the aggregate per-attempt decision-gate bias of the declared REAL*4 decision variables —
>         refuted by construction: a throwaway build emulating exactly those four variables at
>         exactly their three lines reproduces 3 % of the drop and is flat across the 85 % of the
>         range where the original's decline is largest (`src/Particle/BOOT.md`).
>
>       So the cause is an open question, recorded as one rather than fitted with a fifth story.
>       The exclusion evidence this criterion waits on does not exist, and by the taboo no
>       exclusion may be written without it; whether the permitted form of that evidence should
>       change is a decision for the owner, and one that should not be taken while the mechanism
>       it would license an exclusion for is unknown.
>
>       ⚠ 2026-09-21, later still: **the first of those four bullets is withdrawn, and with it
>       the words "dead on complete evidence, not partial".** The REAL*4 summation candidate was
>       not closed; it was closed on a **lower bound read as a measurement**. Raised by an
>       owner-requested audit in another session and verified here against this tree before any
>       of it was adopted:
>
>       - `tests/Particle.Tests/AccumulatorSweep.cs` feeds its binary32 shadow **one rounding per
>         `Attempt.Run` call** — the field is named `AttemptTermCount` — while the original rounds
>         **once per drawn particle**, of order 69 additions into one cell per attempt on HPEPA3.
>         Fewer, larger partial sums round less;
>       - that class's own remarks say so: for five of its eight fields, `Allvdok` and `Vdokstr`
>         among them and those are the two feeding these cells, its figure "is read as a lower
>         bound on the original's own error". The documents then used it as the effect;
>       - with the true per-addition count for HPEPA3's cell 1, `n = 92,867,285`, Higham's bound
>         is evaluated at `k·u = 5.54`, outside its own domain of validity, where
>         `RealFourAccumulationErrorTests.HighamBound` returns positive infinity by its own guard.
>         At the original's granularity there is **no** finite bound to rule anything out.
>
>       So the "1.8 %", "0.1 %" and "22.6 %" figures, and every verdict resting on them here and
>       in the generated defect page, are lower bounds wearing the clothes of measurements. What
>       this note establishes is only that the closure was unfounded. The audit further reports
>       that a per-draw binary32 emulation reproduces the original's own printed `fmdok`,
>       `pdoksmall` and `Dok43all` and the whole recorded dose-response curve, with the two steep
>       stretches falling on binade crossings of the running sum — a positive claim that has
>       **not** been reproduced in this tree and is not adopted here until it is.
>
>       Re-measured the same day at the original's own granularity, in this tree
>       (`src/Particle/BOOT.md`; the shadow now takes one term per drawn particle, replayed with
>       that node's own published `SizeLaw` and stream types, verified bit for bit against a
>       persistent shadow accumulator and proven non-degenerate by a deliberate stream swap seen
>       red on all five formulations): **the REAL*4 summation candidate is reopened.** The
>       worst-cell relative error reaches **46 to 65 per cent** for `Allvdok` and `Vdokstr`,
>       against the 1.8 and 0.1 per cent these documents quoted. `Dokp41` and `Dokp31`, which feed
>       `dokkarm43`, now **exceed the whole replica-to-replica band**, at a ratio of 1.1 to 2.8 on
>       HPEPA3 and HMX: on those formulations REAL*4 accumulation alone could account for the
>       failing `dokkarm43` cells. The retained fractions, 0.540 on HPEPA3 and 0.346 on HMX,
>       corroborate the audit's independently computed 0.5405 and 0.3469 without importing them.
>
>       What this does and does not settle. It settles that the candidate is alive and that its
>       magnitude at the true scale is of the order the failures are. It does not yet reach
>       `fmdok`, `pdoksmall` or `Dok43all` themselves, whose chains run through nodes outside the
>       measuring node's reach — so the audit's wider claim stands unreproduced here. The other
>       three closures are untouched by this: a single threshold crossing, a port-side defect and
>       the aggregate decision-gate bias were each refuted by their own measurements, none of
>       which depended on the granularity that failed here.
>
>       A measurement found in this work and worth the next reader's attention: taking an
>       attempt's contribution as the difference of the record before and after it breaks down
>       once the record carries a long chain of retries on one particle — a double-precision
>       instance of the same cancellation being measured at REAL*4 scale.
>
>       Propagated the same day through `src/Statistics`' own chains at the corrected
>       granularity (`src/Statistics/BOOT.md`; ratios of the propagated error to
>       `StatisticalCriterion`'s own band for the worst reachable cell, "rules out" under 5 %,
>       "large enough to matter" over 50 %):
>
>       | | `fmdok` | `Dok43all[1]` | `pdoksmall` |
>       |---|---|---|---|
>       | HPEPA3 | 1921 % | 403 % | 587 % |
>       | HMX | 3143 % | 111 % | 2029 % |
>       | PSAN02n | 201 % | 15 % | channel reaches only an unprinted cell |
>       | P33 | 1.7 % | 0.60 % | 2.1 % |
>       | inpt | 1.0 % | 0.11 % | channel closed |
>
>       **So the cause is found for the bulk of the failures, and it is a per-formulation split,
>       not one verdict.** On HPEPA3 and HMX the original's REAL*4 accumulation is consistent with
>       **fully** explaining the failing `fmdok`, `Dok43all[1]` and `pdoksmall` cells — and those
>       two formulations carry 164 of the 173. On PSAN02n it explains `fmdok` alone. On P33 and
>       `inpt` it rules itself out on every propagated cell, and those two had 0 and 1 failing
>       cells to explain. The `gdokleft` path stays at zero everywhere, as its own measurement
>       found, and the three other closures stand.
>
>       What is still not established: that this accounts for each failing cell rather than for
>       the worst one per quantity, and the mechanism by which a whole array fails rather than
>       only its head — an outside audit attributes that to normalisation by a corrupted total,
>       which a single worst-cell perturbation can neither reproduce nor refute without a second
>       implementation of the accumulation, forbidden by the taboo. What this tree can say is
>       that the formulations that flip are the ones that audit names.
>
>       **The computed evidence this criterion's exclusion clause asks for now exists for some of
>       these cells.** Writing an exclusion is the owner's decision and has not been taken; the
>       recommendation not to widen the permitted *form* of that evidence is withdrawn along with
>       the premise it rested on, since the mechanism is no longer unknown where it matters.
>
>       Censused the same day, over all 173 failing cells of both layouts
>       (`tests/Harness/HISTORY.md`, "Governing-term census"): **the Student band governs 145
>       of them (83.8 %), the print-resolution floor 27 (15.6 %) and the count rule one**,
>       against a base rate among passing cells of 53.7 / 26.9 / 18.5 per cent. Two
>       consequences the exclusion evidence must start from. The 27 cells governed by the
>       static floor do not depend on `α` at all, so no calibration can move them — that is
>       the upper bound on what the remaining criterion work can still change. And the same
>       **physical** cells fail in both layouts — 37 of HPEPA3's 39, 41 of HMX's 46 — so this
>       section's reading of a systematic difference rather than a seed effect is now checked
>       cell by cell and not by count alone.
>
>       ⚠ 2026-09-20, later the same day, resolved 2026-09-21: every figure in this
>       criterion and in the one below was taken against the port's output as it stood before
>       `src/Statistics` was fixed to print `pdoksmall` at `Ndok - 1` (Fortran line 1386, one
>       element short of the array's own dimension, which the port had not reproduced). They
>       were marked superseded and a bound was stated to be refuted: the dropped element is
>       the array's tail, the criterion already pairs index for index, and no retained cell's
>       value moves, so a count could fall by at most one per formulation and layout and could
>       not rise.
>
>       Re-measured by rebuilding the pre-fix commit and the post-fix head and running both
>       through the route the earlier sweeps used (`tests/Harness/BOOT.md`, "Re-measurement
>       after the `pdoksmall` print-length fix"): the bound held on all twenty rows, and more
>       strongly than it was argued. The compared and failing counts are **bit-identical**
>       before and after in every row; only the excluded count moves, down by exactly one
>       everywhere, and no failing cell disappeared, checked cell for cell on the two largest
>       failing sets rather than by count. The reason the verdicts could not move at all: no
>       replica ever printed that index, so the criterion's sparse-cell exclusion had already
>       dropped the extra cell before the fix existed. The figures in both criteria therefore
>       stand as measured, and the fix removes none of the 173.
>
>       Decided 2026-09-23 by the orchestrator, under the owner's delegation for the night:
>       this criterion is read as link 1 of "The `Original` accumulation kind reproduces…"
>       below. `Binary64` differs from the original by design, in the original's own REAL*4
>       degradation, so a `Binary64` run against the original's replicas can never pass without
>       exclusions no one has evidence for. `tests/Simulation.Tests/StatisticalCriterionTests`
>       now runs reference mode under `Original`, seed 0, both layouts, as an exact-match
>       ratchet of known open cells, each citing this document. A new failure turns the test
>       red, and so does a known cell that starts passing. Measured the same day:
>       - no failing cell on seven of the ten rows;
>       - HMX `Original`: `fqdokkarm(31,:)[7]`, the cell the original's own reference fails
>         (the gate-1 criterion above);
>       - HPEPA3 `Independent`: `fqkarm_cor[60]`, a seed-0-only failure;
>       - P33 `Independent`: `fqkarm_cor[15]`, unexplained.
>
>       The eleven `Binary64` cases that were red since 2026-09-19 are gone, and nothing was
>       excluded to remove them (`tests/Simulation.Tests/HISTORY.md`).

---

<a id="batched-criterion-chronicle"></a>

## 2026-09-24 — from "## Acceptance criteria" — the batched criterion's per-layout wording and its measurement chronicle

Moved because the criterion above the pointer already states the current claim
(agrees with reference mode, `Independent` layout, `Binary64`, every cell; batched
`Original` refused); this is the chronicle of measurements — the batched `Original`
excess, its understatement, its HMX figure correction, and the CPU/CUDA rewrite as
link 3 — that got it there.

> - [x] 2026-09-24: Batched mode (whole-cycle batches, CPU accelerator and CUDA) satisfies the same
>       criterion on the same formulations, in each layout.
>
>       Measured 2026-09-20: batched `Original` P33 35/1462 and HMX 92/4343, against 0 and
>       46 for reference mode in the same layout; batched `Independent` P33 0/1456 and HMX
>       43/4191, against 0 and 42. The `Independent` excess is nil, the `Original` excess is
>       not, which is a statement about the stream derivation of a batched particle, not
>       about the accelerator (`src/Random/BOOT.md`, "Batched derivation").
>
>       ⚠ 2026-09-21, later: this criterion reads the batched `Original` excess as a count of
>       failing cells — 35 of 1462 on P33 — and that **understates it by an order of magnitude**.
>       A two-sample comparison of the same runs puts 311 of 1106 varying cells past `|t| = 4.5`,
>       with `Dkarm10` off by thirty seed standard deviations and the original's own generator
>       diagnostic degraded twentyfold ("Two stream layouts"). The band is six to nine standard
>       deviations wide and was never built to see an effect of this size; a criterion that
>       reports a different program as a handful of cells is the fourth instance in two days of a
>       figure read as something it is not. The combination is now refused, so this criterion
>       applies to the `Independent` layout, where the excess is nil, and the `Original` row
>       stays here as the measurement that decided the refusal.
>
>       ⚠ 2026-09-21: was HMX `Original` 91/4343, now 92/4343. The figure was wrong when
>       written, not overtaken: the criterion as it stands produces 92 reproducibly, and did
>       so on both the pre-fix and the post-fix builds of the re-measurement above, so it is
>       not an effect of the `pdoksmall` fix. It is the same kind of slip as the P33
>       `Independent` cell corrected under the criterion above, and it was found the same
>       way — by a measurement that had to reproduce the old figures before it could compare
>       against them.
>
>       2026-09-23: rewritten as link 3 in `tests/Simulation.Tests/StatisticalCriterionTests`.
>       Setup: batched mode (CPU, whole-cycle batches) against reference mode, both `Binary64` and
>       `Independent`, eight seeds `k·2¹⁷` each; the seeds spread every stream evenly over its
>       circle. Comparison: Welch per cell at the tree's α (`TwoSampleBiasOfSets`).
>       Result: **zero differing cells on all five formulations**, 1509 to 6635 cells each. The
>       positive control was seen red: reference `Binary64` against reference `Original` on
>       HPEPA3 differs. CUDA is not run in this row; its agreement with the CPU accelerator is
>       the tier table's, below. So for the `Independent` layout the batched half of this
>       criterion is met on the CPU. It stays unticked until CUDA is run the same way.
>
>       Ticked 2026-09-24 by the orchestrator, under the owner's delegation. CUDA was run the
>       same way, in `StatisticalCriterionTests`' CUDA link-3 rows on the reference machine:
>       **zero differing cells on all five formulations**, with the CPU rows' cell counts
>       exactly, and a mutation was seen red. Where CUDA is not bound, the row asserts the
>       refusal status and is never skipped. The `Original` layout is refused in batched mode
>       ("Two stream layouts"), so this criterion covers the `Independent` layout, as its
>       2026-09-21 note says.

---

<a id="original-kind-criterion-chronicle"></a>

## 2026-09-24 — from "## Acceptance criteria" — the "`Original` accumulation kind reproduces…" criterion's full run history

Moved because the criterion above the pointer already states its three links, the
name and "one criterion cannot serve two kinds"; this is the run-by-run history —
every measurement, withdrawal and fix from 2026-09-21 to 2026-09-24 that closed link 1
against the original's own replicas, link 2 against `Binary64` and link 3 against
CPU/CUDA — kept together because its nested notes belong to one chronicle.

> **Run in the tree 2026-09-21, after the fix that made the option reach the computation**
> (`tests/Harness/BOOT.md`; five formulations, both layouts, one seed each, reference mode;
> the apparatus validated first by reproducing this document's own `Binary64` figures exactly
> in all ten rows). Link 1 **reproduces**: 170 of the 173 failures pass under `Original`,
> in **both** layouts, with **no new failure anywhere** — 91 of 92 in the `Original` layout,
> matching the throwaway build, and 79 of 81 in the `Independent` layout, measured here for
> the first time. The three held out are already named and are not new: `inpt`'s
> `epsdokfr[0]` in each layout, the declared REAL*4/REAL*8 mixing defect, and P33
> `Independent`'s `fqkarm_cor[15]`, which this document already records as unexplained.
>
> Link 2 is clean on four of five formulations, where every diverging quantity is an
> accumulator print field. **HMX is not**: at one seed the two kinds did not draw the same
> generator sequence — integer counters apart by 165 to 374 of some 1.2 million, and the
> original's own generator diagnostics by 19 per cent to over a thousand. That is the case
> link 2 above anticipates, and the multi-seed two-sample comparison it calls for **does not
> exist yet**. Link 3 is unaffected and was re-confirmed rather than assumed.
>
> So this criterion stands one measurement short of tickable, and the measurement is named.
>
> Not "equal to print resolution" as a criterion, even sequentially: once a trajectory
> diverges, cells whose Monte Carlo noise exceeds three digits need not match, and the
> exact matches already seen corroborate the mechanism rather than defining the bar.
>
> ⚠ 2026-09-21, later still: this criterion carried two claims about what reproducing the
> accumulators does **not** reach, and both were wrong. They read: "HMX's integer counters
> move under the corrected rule and do not converge on the original's, and HPEPA3's
> headline quantities close from about +7 % to +1–4 % rather than to zero", and link 2
> below was written from the first of them. Raised by an owner-requested audit in another
> session; the second is verified here at the command line before adopting either.
>
> - **HPEPA3's headline quantities close to zero.** The +1–4 % was one seed read against
>   nothing, and one run's own spread is 3.3 % on `Dkarm43_cor(2)` and 1.9 % on
>   `Zkarm_cor(2)`. Over nine seeds the residual against the original's replicas is
>   −0.4 % and +0.6 %, inside its own error, against +6.7 % and +5.0 % under `Binary64`.
>   This is the **third** defect of one shape in two days: a figure with no noise scale,
>   read as a mean;
> - **HMX's trajectory is not special.** Under `Original` the first index where `pdoksmall`
>   reaches 1 is 49, as in the original, where `Binary64` gives 50 — verified here directly.
>   So the accumulation does bring that index together, and line 683 does not govern the
>   counters that stay apart. Those diverge from the original on four of five formulations
>   under **either** kind, at 1e–4 to 1.3e–3, and P33 matches exactly under both: that is
>   the main-trajectory divergence from the original's REAL*4 sizes and decision variables,
>   which this tree's own earlier measurement locates at 1,894 accepted particles on HPEPA3.
>
> **So what is reproduced is the estimator, and what is not is the sample path beyond the
> first REAL*4-sensitive branch** — all 40,000 particles on P33, 1,894 on HPEPA3 — after
> which the run is another sample of the same estimator and is compared statistically. No
> formulation is incomplete in a way the others are not, and no printed number moves beyond
> Monte Carlo noise for want of the path. The premise for widening the scope to the sizes
> and the decision variables was the first claim above, and it does not survive it.
>
> ⚠ 2026-09-23: "no printed number moves beyond Monte Carlo noise for want of the path"
> has one counterexample. The count of "Dok < Dmin" breakings is 1.2e-3 per particle in
> the original on HMX and 5.5e-5 on HPEPA3, over sixteen replicas, and exactly 0 in the
> port under either kind; t = 15–20. It is not a setup-plane sliver: rounding `Dmin` took
> the port's count from 7e-4 to 0, not towards the original. So it is presumably the
> attempt plane's REAL*4, and its mechanism is not established. No other printed cell is
> measured to depend on it.
>
> Re-run 2026-09-23 after the setup plane was merged (the link-1 figures above predate
> it; reference mode, one seed each, `--precision original`, the criterion unchanged).
> `Original` layout: **no failing cell on four formulations**; HMX fails one,
> `fqdokkarm(31,:)[7]`, which is the cell the original's **own** reference fails against
> its lagged replicas (the second criterion of this list), printed at the original's own
> value, 0.599E-04. `Independent` layout: `inpt`'s `epsdokfr[0]`, the declared mixing
> defect held out above, now passes in both layouts; P33's `fqkarm_cor[15]` stands, as
> does its "unexplained"; HPEPA3 fails `fqkarm_cor[60]` at seed 0 only, not at seeds 1–4,
> and `epsx(4)[0]` at every one of seeds 1–4 under **both** kinds — so not an effect of the
> precision kind, and not yet investigated. Link 2's missing HMX measurement is unchanged.
> Investigated the same day: `epsx(4)` is a pure generator diagnostic whose replica
> band is an artefact of the replica design, not a failure of the port — the
> `Independent` replicas are shifts of one X2 sequence packed on a tenth of the circle,
> opposite the port's seeds, and the port's value is reproduced from the generator
> alone (`tests/Fixtures/BOOT.md`, "Seed-patched replicas are shifts"). Excluded the same
> day on the owner's decision, against the independent replicas only, with that evidence
> made durable (`tests/Fixtures/exclusions.json`; `tests/Random.Tests/ReplicaShiftTests`).
> Re-measured after it: HPEPA3 `Independent` seeds 1–4 fail no cell, and every other row
> is unchanged — the lagged rows bit for bit, the `Independent` rows by one excluded cell.
>
> Link 2 on HMX, multi-seed, measured 2026-09-23 (`tests/Harness/HISTORY.md#precision-kind-hmx-multi-seed`;
> sixteen seeds per arm spread evenly over every stream's shift, Welch per cell at the
> criterion's level, half-against-half nulls clean): `Binary64` against `Original` differs
> only in the accumulator families and in the two quantities below; `NFQ`, `NFW`,
> `epsx(5)` and `epsx(6)` do not differ, so the single-seed HMX divergence above was a
> re-roll, not an effect of the kind. The same data compared with the original's own
> replicas finds `Original` agreeing on every cell but two findings, which is why this
> criterion **stays open**: `fineoxy_fr` is 1.6–4.7e-10 under `Original` (`fmdok[0]` a tenth of it) at
> every seed where the original prints 0 in all 33 files, on HPEPA3 too — a value the
> port makes and the original never does, mechanism not established; and the `Dok < Dmin`
> breaking count reaches about 60 % of the original's rate (t ≈ 7, below family-wise
> significance). Links 2 of the other four formulations predate the setup plane, after
> which same-seed trajectories no longer coincide, and are not re-measured.
>
> Re-measured later the same day on all five, after the `Dmin` fix
> (`tests/Harness/HISTORY.md#precision-kind-all-five-multi-seed`). **Link 2 holds on every
> formulation.** `Binary64` and `Original` differ only in accumulator families and
> setup-plane values; `inpt`'s `epsdokfr[0]` of 2.7e-8 is the positive control. The nulls
> are clean, 1 cell in about 26,000. **Link 1 over sixteen seeds leaves four items**:
> - the declared `pdoksmall` garbage;
> - the cosmetic `eps` echo;
> - `fmdok[0]`, a second setup-plane sliver of the `Dmin` kind, through the cell size
>   `Di`; the menu parameters were sent to `src/Statistics` the same night;
> - the "Dok < Dmin" count, the counterexample noted above.
>
> `fineoxy_fr` is fixed. This criterion stays open on the last two.
>
> Later the same night: `fmdok[0]` is fixed. `Di` and `Dj` are now rounded as stored and
> are generated members of the setup plane (`src/Statistics/BOOT.md`). The port prints
> `0.000E+00` there in all 32 HMX and HPEPA3 files under `Original`, and `Binary64` has not
> moved. The link-1 rows of `tests/Simulation.Tests` stay green with an unchanged ratchet.
> Left open: the "Dok < Dmin" count, a probe of which is running in `src/Particle`, and
> the cosmetic `eps` echo. The echo is a proposal: `Output` would round `EpsDok` under
> `Original`, but it has no binary32 helper of its own, and `Statistics`' helper is
> internal.
>
> The "Dok < Dmin" count is explained the same night (`src/Particle/BOOT.md`, its
> defect row for lines 478, 533, 1761–1776). The original stores the drawn size in
> REAL*4 before its `≤ Dmin` test, and a draw a hair above the lowest bound, which equals
> `Dmin`, rounds down onto it. A replay of the port's own draws with that one store
> rounded finds 3 events on HMX and 4 on HPEPA3. Their Poisson 95 % intervals contain the
> original's rate on HPEPA3 and fall just short of it on HMX; P33, `inpt` and PSAN02n are 0,
> as in the original. It lies in the attempt plane, which stays `double` by this document's
> decision, so it is declared, not reproduced. With that, every difference link 1 finds
> between `Original` and the original is either declared (`pdoksmall`, this count) or the
> cosmetic `eps` echo. What still keeps this criterion open is the `eps` echo proposal and
> a CUDA row for link 3 in `tests/Simulation.Tests`, which is in progress. On CUDA, link 3
> measured 0 differing cells on all five formulations, and its positive control was seen
> red.
>
> The `eps` echo is fixed the same night (`src/Output/BOOT.md`). Under `Original` the
> header now equals the original's reference header line for line on all five
> formulations, the filename and time lines aside, and `Binary64` has not moved. What
> remains open is the CUDA row for link 3.
>
> 2026-09-24: the CUDA row for link 3 is in the tree and green (the batched criterion
> above is ticked on it). Every precision-specific item of this criterion is therefore
> closed: link 2 holds on all five formulations, link 3 on CPU and CUDA, and every
> difference link 1 finds over sixteen seeds is declared (`pdoksmall`, the "Dok < Dmin"
> count). **Left unticked on purpose.** Link 1 as written asks for every cell at the
> single seed, and three cells still fail there: HMX `fqdokkarm(31,:)[7]`, which the
> original fails against itself too; HPEPA3 `fqkarm_cor[60]` at seed 0 only; and P33
> `fqkarm_cor[15]`. None of them depends on the precision kind. They are questions of
> the criterion's own calibration, the same questions as the gate-1 criterion and the
> original failing its own criterion in about 7 % of runs. Whether to tick this
> criterion on the precision-specific evidence, or to hold it until that calibration is
> decided, is the owner's call. The orchestrator recommends ticking it, with the three
> cells named.
>
> One of those calibration questions is answered on 2026-09-24
> (`tests/Fixtures/BOOT.md`, "Seed-patched replicas are shifts", last paragraph). The
> replica sets' spread equals the spread of evenly spread independent runs, to a median
> ratio of 0.93–1.02 over some 7000 cells. So the rigid-shift replicas do not narrow the
> criterion's bands, and whatever makes the original fail its own criterion in a tenth
> of new runs is not that. Redesigning the seed jumps would buy nothing for calibration.
> It remains only a cleanliness question for generator-only diagnostics such as
> `epsx(4)`.
>
> And the criterion's own false-failure rate is measured the same day
> (`tests/Harness/HISTORY.md#criterion-false-failure-rate-port-original`). Of the 160
> port runs under `Original`, runs of the same estimator as the original by every
> measurement above, **11 fail at least one cell: 6.9 %, interval 3.5–12 %**, against
> the 0.1 % its level declares. Every failing cell is a sparse tail cell. P33
> `fqkarm_cor[15]` fails at two of the port's own sixteen seeds and is therefore
> explained as a false failure, not a port property. So the three cells holding this
> criterion open are that miscalibration, measured, and not reproduction failures.
>
> Later the same day, five of those seventeen failing cells turned out to be an
> implementation defect of the criterion, not its calibration. The count floor's band
> was centred on the wrong mean, so a candidate exactly at the negative binomial's own
> upper bound failed (`tests/Harness/HISTORY.md#count-floor-boundary-centring-defect`).
> Fixed for sparse cells. The same 160 runs now give **9 failing runs, 5.6 %**, still
> some fifty times the declared rate. The rest is the calibration of sparse tail cells,
> which is open, and none of the three ratchet cells moved.
>
> Ticked 2026-09-24 on the owner's decision, on the precision-specific evidence:
> - link 1: `tests/Simulation.Tests/StatisticalCriterionTests`, the reference rows under
>   `Original`, a ratchet of three named cells, together with the sixteen-seed comparison
>   in which every difference is declared;
> - link 2: `tests/Harness/HISTORY.md#precision-kind-all-five-multi-seed`;
> - link 3: the CPU and CUDA rows of `StatisticalCriterionTests`.
>
> The three cells of the ratchet stay named there and are not excluded. They belong to
> the criterion's calibration of sparse tail cells, which is open under the gate-1
> criterion above, and they leave that ratchet only by an edit.
>
> ⚠ 2026-09-24, later: the "11 fail … 6.9 %" and "9 failing runs, 5.6 %" above counted
> only `StatisticalCriterion.Compare`. The criterion is three comparisons: `Compare`,
> the tail-row mean and the canonical-axis comparison. That measurement's own apparatus
> line says so, and it was narrowed on purpose to find the count-floor defect. Over the
> whole criterion the same 160 runs fail 23 times, 14.4 %; filtered to `Compare` the rate
> table reproduces the 9 cell for cell. The ratchet of three cells is replaced by a seed-0
> snapshot (the rate decision under "Statistical reference criterion").

---

<a id="known-bias-oxidizer-distribution"></a>

## 2026-09-23 — from "## Constraints", "Known bias of the original's seeds" — the oxidizer distribution correction, and the withdrawal of its `epsdokfr` item

Moved because the current sentence above the pointer already states the size
moments/distribution split; this is the refutation of the old "only the oxidizer size
distribution … is unaffected" wording, together with the later withdrawal of its
`epsdokfr` item as a replica-design artefact and the mechanism paragraph that went
with the original wording.

> ⚠ 2026-09-20: was "only the oxidizer size distribution, which the neighbour loop does
> not feed back into, is unaffected", now the narrower claim above. The old wording was an
> absolute resting on one cell — the mass-mean `Dok43all` of the table — and it is refuted
> on three of the five reference formulations by the whole family, measured over every
> quantity the original prints under "Dok parameters" and "Conditional DOK"
> (`tests/Harness.Tests/OxidizerSizeDistributionBiasTests`, the same two-sample statistic
> and level as the rest of the section, membership read from the reference files and each
> excluded name given its reason). What differs: the oxidizer mass-fraction distribution
> `fmdok` on HPEPA3 (+2.4 % in one cell), on HMX (−1.1 %, −2.6 %, +0.9 %) and on PSAN02n
> (eight cells, −3.8 % to +4.0 %), and `epsdokfr` on PSAN02n by −77 %. One cell of
> `Dok43all` itself, index 1 on PSAN02n, is resolved as differing at +0.027 %: statistically
> separable because that quantity's spread is minute, negligible in magnitude, and not a
> counterexample to the size-moment claim above at any scale that matters.
>
> ⚠ 2026-09-23: the `epsdokfr` item above, "on PSAN02n by −77 %", is withdrawn as a bias of
> the seeds; it is an artefact of the independent replica set. Those replicas are rigid
> shifts packed on a tenth of stream 4's circle (`tests/Fixtures/BOOT.md`, "Seed-patched
> replicas are shifts"), and their `epsdokfr[0]` drifts smoothly from 1.6e-3 to 3.0e-3 with
> the replica number. The port's own sixteen seeds spread evenly over every stream's
> circle give 7.9e-4 (sd 6.5e-4) in the `Independent` layout. The lagged replicas give
> 5.3e-4 (sd 4.3e-4), and the two do not differ on this cell (Welch t = 1.3, p = 0.19).
> The arc replicas give 2.35e-3 (sd 3.8e-4). Found by the multi-seed measurement of 2026-09-23
> (`tests/Harness/HISTORY.md#precision-kind-hmx-multi-seed`, apparatus). The other items
> of this correction are untouched: whether any of them shares the artefact is not measured.
>
> The mechanism sentence went with the wording: whatever the neighbour loop does not feed
> back into, it is not the whole of the oxidizer family, and which part of that family the
> feedback reaches is not measured. The four canonical-axis families were measured apart
> and are evidence for neither side, since a per-index comparison does not align that axis;
> the refutation stands without them.

---

<a id="double-precision-only-bounded-by-the-kind"></a>

## 2026-09-21 — from "## Invariants", "Double precision only" — bounded by the accumulation kind, before it was renamed

Moved because the current wording above the pointer already names the precision
kind directly; this is the correction that first bounded the invariant to the kind
(then still called the accumulation kind), not a fact the invariant still needs
stated.

> ⚠ 2026-09-21: was this invariant unconditional over the model's arithmetic, now bounded
> by the accumulation kind below, which rounds the accumulators the original declares REAL*4.
> The model's own arithmetic — sizes, distances, decisions — stays `double` under either
> kind, so what this invariant governs everywhere else is unchanged.

---

<a id="benchmark-criterion-node-existed"></a>

## 2026-09-20 — from "## Acceptance criteria" — "the node does not exist yet"

Moved because the criterion above the pointer already carries the measured figures
from the now-existing `tests/Benchmarks`; this is the review finding that corrected
the stale "does not exist" reading, not a fact the criterion still needs stated.

> ⚠ 2026-09-20: was "the node does not exist yet", which was true when written and false
> from the moment the node was merged the same day. Found by an Opus 5 review of the
> branch, which checked the claim against the solution file. The verdict does not
> change — the criterion stays unticked, because an empty table records nothing — but
> the reason a reader is given for it now matches the tree.

---

<a id="precision-kind-renamed-and-extended"></a>

## 2026-09-23 — from "## Invariants" — the accumulation kind, before it became the precision kind

Moved because the owner decided on 2026-09-23 to extend the switch from the accumulators to the setup plane and to rename it so that the name still says what it covers. This is the wording that stood from 2026-09-21 until then; the paragraphs that followed it in the bullet (the kind as a parameter of the one `Attempt.Run`, sequential only, why it exists) stand unchanged in `BOOT.md`.

> - **Accumulation kind is an option of every run, and `Double` is the default** (decided
>   2026-09-21). `Double` accumulates in `double`, as the port always has. `Original` rounds
>   to binary32 after every addition **exactly those accumulators the Fortran declares REAL*4**,
>   reproducing the original's own loss; the model's sizes, distances and decision variables
>   stay `double` under both kinds, so this reproduces the original's *accumulation*, not its
>   executable, and the name says so. The classification is **generated** from the source's
>   declaration block and its implicit-typing rule, never typed by hand (`src/Particle`), for
>   the reason AGENTS.md §6 gives for any criterion quantified by "all".

<a id="literals-setup-plane"></a>

## 2026-09-23 — from "## Constraints", "Fidelity to the original" — literals were never rounded

Moved because the owner reversed it for one plane on 2026-09-23: without the binary32 fold of the setup plane's literal quotients, the setup plane cannot be reproduced (the fold alone closes `inpt`'s `epsdokfr[0]` from 1.07e-8 to the original's 2.70e-8). The wording that stood from 2026-09-17:

>   - literal constants stay as written where they stand: `3.14159` (sphere volume),
>     `3.14` (inside the bridge volume), `12.56636` (full solid angle); the REAL*4
>     rounding of literals is not reproduced;

---

<a id="one-particle-program-host-thread-decision"></a>

## 2026-09-20 — from "## Invariants" — why the ILGPU CPU accelerator stopped being a production path

Moved because it is the measurement that motivated the decision, not the decision itself: the current invariant (host threads, CUDA and the accelerator's role as a test oracle only) already stands in the bullet's own first sentence, above the pointer.

> ⚠ 2026-09-19: this invariant first named the host thread, the ILGPU CPU accelerator
> and CUDA as equal places of execution, with batched CPU runs on the accelerator.
> Measured on HPEPA3 cycle 1: the accelerator ran 4 190 accepted particles/s on 16
> threads, the same `Attempt.Run` called directly from .NET threads 9 456/s on one thread
> and about 43 000/s on 16, bit-identical to the accelerator's totals, while the
> original runs about 7 400/s on one thread. The accelerator is a warp emulator, not a
> performance path. Owner's decision the same day, after an advisory review by Fable 5.1
> (`src/Execution/BOOT.md`, "Host-thread path").

---

<a id="one-particle-program-build-configuration"></a>

## 2026-09-20 — from "## Invariants" — the first measurement mixed Debug and Release builds

Moved because it is provenance for a figure correction, not a claim the invariant depends on: the decision it reconfirms is already stated above, and every throughput figure in the tree is now quoted from a Release build regardless (`src/Execution/BOOT.md`).

> ⚠ 2026-09-19, the same day: the CPU figures above mixed build configurations. The
> accelerator's 4 190/s was measured under `dotnet test`'s default Debug build, which
> disables JIT optimisation of `Particle` and `Random`; the prototype's figures were
> effectively Release. Measured again, all in Release: the accelerator 11 279/s on 16
> threads; host threads 8 750 – 8 900/s on one thread, 34 600 – 42 700/s on 16;
> reference mode 7 250 – 7 800/s; CUDA 39 100 – 39 300/s (not sensitive to the .NET
> build). The decision stands on the corrected figures (host threads three to four times
> the accelerator), and every throughput figure in this tree is now quoted from a
> Release build.

---

<a id="double-precision-only-first-exception"></a>

## 2026-09-20 — from "## Invariants" — the first fidelity-audit finding that forced the binary32 exception

Moved because the exception it forced is already the invariant's own current wording, above the pointer; this is the audit evidence that made the exception necessary, not a fact still needed to state the exception itself.

> ⚠ 2026-09-17: this invariant first said without exception that REAL*4 is not
> emulated. The fidelity audit of `Statistics` showed that sizes from `double` operands
> change the printed array lengths of five archived formulations (C166: 72 cells
> instead of 71).

---

<a id="double-precision-only-exception-extended"></a>

## 2026-09-20 — from "## Invariants" — the exception widened to `DPRow` and the whole-fraction count

Moved because the widened exception is already the invariant's own current wording, above the pointer; this is why it was widened, not a fact still needed to state the exception itself.

> ⚠ 2026-09-18: the exception first named only `Ndok`, `Nkarm` and `Ncat`. The audit of
> the implemented `Statistics` found two more quotients of the same kind, `DPRow` (which
> fixes the printed length of `Dkarmcat`, `dokkarm43`, `dokkarm10` and every
> `fqdokkarm` row) and the whole-fraction count of `pdoksmall`. No archived formulation
> is near an integer boundary in either, so nothing observed changes; the rule is
> extended by kind rather than left to be rediscovered at the next boundary case. The
> criterion is the original's own arithmetic, not the size of the margin.

---

<a id="batched-mode-qks1-per-launch-wording"></a>

## 2026-09-20 — from "## Invariants" — "at the start of the batch" contradicted "batched mode degenerates to reference mode"

Moved because the corrected, per-launch rule is already the invariant's own current wording, above the pointer; this is why the per-batch wording was wrong, not a fact still needed to state the rule itself.

> ⚠ 2026-09-17: this invariant first read "Within a batch the normalized pocket
> histogram is the one at the start of the batch". That contradicted the invariant
> "Reference mode is batched mode degenerated": with batch 1 an unaccepted particle is
> relaunched, and the original refreshes QKS1 between its attempts when the attempt
> completed the neighbour loop. Found in the design session of `Particle`; the owner
> approved the per-launch rule, which equals the original for batch 1 and changes
> nothing measurable for whole-cycle batches, where about 99 % of particles finish in
> the first launch.

---

<a id="execution-model-batched-jump-ahead-first-grouping"></a>

## 2026-09-20 — from "## Constraints" — a uniform jump recreated the original's spurious short lags inside every particle

Moved because the mechanism (why a common jump is wrong) is provenance for the current grouping rule, which is restated concisely above the pointer and owned in full by `src/Random/BOOT.md`.

> ⚠ 2026-09-19: batched mode first jumped all six initial states of the `Original`
> layout by the same amount. The first run of the port's own `results.m` through the
> criterion showed batched `Original` failing two to five times more cells than
> reference mode (HPEPA3 110 against 39), on the pocket and bridge quantities. The
> cause is arithmetic: the original's six seeds are one sequence offset by `a⁰ … a⁵`,
> so a common jump recreates, at the start of every particle, the short lags the
> original has only at the start of its whole run — stream 6 (X3, X4) replays the
> first neighbour sizes of the same particle two draws later, stream 2 (X0) replays
> them three draws later. In a continued run those lags drift apart within the first
> particle, because the roles consume the sequence at different speeds; only the two
> lags between roles consumed at the same speed persist for the whole run (X0/X of
> consecutive attempts, X21/X2 of consecutive neighbours, the source of the known
> bias). Decided the same day: in batched mode the `Original` layout jumps the role
> groups {1, 2}, {3}, {4, 5} and {6} by distinct offsets inside the particle's stride
> (`src/Random/BOOT.md`, "Batched derivation"), which keeps exactly the persistent
> lags and removes the spurious ones. The `Independent` layout is unaffected (its
> orbits are disjoint).

---

<a id="execution-model-batched-jump-ahead-second-grouping"></a>

## 2026-09-20 — from "## Constraints" — the first grouping put stream 3 apart, and it should not have been

Moved because the mechanism (why stream 3 must join the {4, 5} group) is provenance for the current grouping rule, which is restated concisely above the pointer and owned in full by `src/Random/BOOT.md`.

> ⚠ 2026-09-19, the same day: the groups were first {1, 2}, {3}, {4, 5}, {6}, and
> batched `Original` then failed 5 to 40 times more cells than before (HPEPA3 563,
> PSAN02n 565), behaving like the `Independent` layout (attempts per particle on
> PSAN02n nearly halved, onto the independent replicas' mean). Stream 3 had been put
> apart because its seed `a³′` is not a small power of `a`; but it is consumed in
> lockstep with streams 4 and 5 (every distance draw X1 is followed by one X2/X21
> pair), and `a³′ = a³ + 36 · 2¹⁰⁴`, so `S3 = (a + 36 · 2¹⁰⁴ · a⁻²) · S4` for the whole
> run: X1 of neighbour n and X2 of neighbour n + 1 have a Pearson correlation of
> 0.999994 over 10⁶ draws. The groups are {1, 2}, {3, 4, 5}, {6}. Measured with them,
> batched `Original` against the lagged replicas (reference mode's own count in
> brackets): HPEPA3 44 (39), inpt 1 (1), P33 29 (0), PSAN02n 3 (0), HMX 89 (44) — the
> remaining excess of P33 and HMX is open.

---

<a id="statistical-criterion-gsv3-replicas-withdrawn"></a>

## 2026-09-20 — from "## Constraints" — the GSV=3 replicas estimate a different quantity than the port's `Original` layout

Moved because the revised criterion (lagged and independent replicas, the per-quantity rules) is already the bullet's own current wording, above the pointer; this is the experiment that showed the GSV=3 replicas were the wrong comparison, not a fact the criterion still needs stated.

> ⚠ 2026-09-17: the replicas were first the GSV=3 runs, with a Student band and a
> Poisson floor for every quantity. The original's own reference failed against them on
> 13–27 % of the quantities of every formulation. Runs of the original with patched
> seeds showed why: lagged seeds reproduce the reference (HPEPA3: attempts 1.332–1.334 M,
> bridges per particle 4.361–4.365, all 25 scalars within |z| ≤ 2.5 of 16 lagged
> replicas), while independent seeds reproduce the GSV=3 runs (1.395–1.400 M,
> 4.78–4.81). Making only streams 4 and 5 independent removes the whole shift; making
> streams 1 and 2 independent changes nothing. The GSV=3 replicas therefore estimate a
> different quantity than the port's `Original` layout. The criterion's per-quantity
> rules are revised as well, because a t-band on replicas that are mostly zero in a
> cell has no tail. Advisory review by Fable 5.1, experiments in the session scratch,
> owner's decision the same day.

---

<a id="known-bias-mass-type-quantities-withdrawn"></a>

## 2026-09-20 — from "## Constraints" — the mass-type quantities are not exempt from the seed bias either

Moved because the corrected table above the pointer already shows every pocket and bridge quantity biased, mass-type ones included; this is the refutation that withdrew the narrower claim, not a fact the table still needs stated.

> ⚠ 2026-09-17: this item first said that the mass-type quantities (`Dkarm43`, `Zkarm`,
> the oxidizer size) agree within about one run's spread. That rested on HPEPA3 alone,
> where the shift of `Dkarm43` is two runs' spread; the two-sample test of
> `tests/Harness` over all five formulations refuted it (PSAN02n `Dkarm43` −216 run
> spreads). The claim that archived `Dkarm43` and `Zkarm` values are trustworthy is
> withdrawn.

---

<a id="performance-cuda-measured"></a>

## 2026-09-20 — from "## Constraints" — the early measurement landed, and a decision followed

Moved because the measurement and the decision it produced are now stated directly in the bullet's own current wording, above the pointer, replacing the stale "unknown until an early measurement" framing; this is the reasoning (divergence and tails, not arithmetic) that is provenance for the decision rather than the decision itself.

> ⚠ 2026-09-19, measured: CUDA 39 000 – 43 000 accepted particles/s on HPEPA3 cycle 1,
> about the same as 16 host threads in a Release build. The attempt is branch-heavy with variable-length loops,
> and FP64 on a GeForce runs at 1/64 rate, so the kernel is bound by divergence and
> tails, not arithmetic. Decided the same day: no restructuring of the kernel before
> the port is accepted (a stage-by-stage restructuring would be a second implementation
> in all but name); after acceptance, a profile and three cheap experiments (the
> per-launch attempt budget, a field-major record layout, the frozen QKS1 in constant
> memory) recorded in `tests/Benchmarks` decide what, if anything, follows.

---

<a id="acceptance-criterion-unticked-count-rule"></a>

## 2026-09-20 — from "## Acceptance criteria" — a gate that cannot go red is not evidence

Moved because the current state of the criterion (one cell open on HMX, the rest of the diagnosis resolved) is restated concisely above the pointer; this is the derivation and confirmation of the `p`/`q` swap, not the open item itself.

> ⚠ 2026-09-20, unticked the same day it was ticked. The tick was placed on five
>     formulations with zero failing cells, and that reading was worthless: the count
>     rule it rested on had `p` and `q` swapped throughout its predictive interval, so
>     every count-like cell was compared against a band far too wide to fail — one real
>     cell's upper limit was 77 where the corrected formula gives 8. A gate that cannot
>     go red is not evidence. With the formula fixed the same day, HMX's reference fails
>     on one cell of a tail row (a count of 11 against five contributing replicas that
>     are all zero), and that cell is left red and declared while the rules its class
>     needs are written (`tests/Harness/BOOT.md`). Found by the implementer while
>     proving an unrelated check non-degenerate, confirmed by an independent
>     Poisson-Gamma derivation and by direct numerical summation; the consequences for
>     the criterion were decided by Fable 5.1 the same day.

---

<a id="output-criterion-narrower-than-it-reads"></a>

## 2026-09-20 — from "## Acceptance criteria" — array lengths and accumulated-scalar digits differ for correct code too

Moved because the caveat is restated in one added sentence on the criterion item itself, above the pointer; this is `tests/Output.Tests`'s own measured reasoning for the caveat, not the caveat's statement.

> ⚠ 2026-09-20: this criterion is narrower than it reads, and the narrowing is
>     `tests/Output.Tests`' own, measured and dated there: array lengths tied to a
>     running maximum and the printed digit shape of accumulated scalars differ between
>     a live run and the archived reference even for correct code, so they are not
>     compared here but left to the statistical criterion.
