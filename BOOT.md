# BOOT.md — PropStruct (tree root)

<!-- Read by every session (AGENTS.md §10): goals, invariants, frame, taboos; details
     live in the child nodes. Written 2026-09-17; decisions of that date are the owner's. -->

## Purpose

A .NET library, with a thin command-line front end, that computes the local structure
of a composite solid propellant by Monte Carlo: pockets between large oxidizer
particles, interpocket bridges, and from them the size distributions and mass-medium
size of metal agglomerates (the pocket model). It is a port of the legacy Fortran
program PropStructV3 (Digital Visual Fortran 6, 32-bit, build of 2015-03-18; kept outside
this repository), rebuilt so that one numerical program runs on the CPU and, whole
cycles of base particles at once, on an NVIDIA GPU in double precision.

Input: a formulation file in the original `.dat` format and the fourteen model
parameters the original asked for interactively, given as options with the original
defaults (`Input`). Output: a structured result holding every quantity the original
prints, and a `results.m` in the original's layout so that existing MATLAB
post-processing keeps working (`Output`). Users are propellant researchers running
formulations from the command line or from .NET code.

Every run carries a **precision kind** (see `## Invariants`): `Binary64`, the default,
computes what the original would have computed without its own rounding loss; `Original`
reproduces the binary32 values the original's executable computes with — in its
accumulators, its setup plane and its per-cycle plane — and so its printed answers up to
the declared differences, the last printed digit of the print plane's own REAL*4
arithmetic among them. Both are the same program, and each run says which it was.

⚠ 2026-09-27: was "and so reproduces the original's printed answers", unqualified; false by one print unit for PSAN02n's `da_coef` → HISTORY.md#purpose-printed-answers-2026-09-27

⚠ 2026-10-01: was the values the original stores in REAL*4, in two places, now its executable's binary32 values, in three → HISTORY.md#purpose-executable-values

Not goals of version 1: the original's generators GSV=1 and GSV=3 (every shipped
formulation uses GSV=2); byte-exact replay of the original executable, whose REAL*4
storage `Original` reproduces but whose sample path parts from the port's at the first
REAL*4-sensitive branch; interactive input; graphics; any change of the model itself.
Defects of the original are reproduced or declared, never silently fixed.

⚠ 2026-10-04: was "packaging and publishing" a not-goal and the original's archive at `legacy/PropStructV3.zip`, now delivered and kept outside the repository (`## Delivery`) → HISTORY.md#delivery-decided-2026-10-04

⚠ 2026-09-21: was "x87 intermediates and REAL*4 make branch-exact replay unreachable", now the REAL*4 storage reproduced and only the sample path out of reach (HMX example withdrawn 2026-09-24) → HISTORY.md#purpose-replay-reason-half-wrong

## Invariants

- **One particle program.** The simulation of one attempt at a base particle with its
  surroundings (sizes, Poisson distances, pocket and bridge decisions, every
  accumulator side effect) exists exactly once in the tree, in kernel-compatible C#
  (`Particle`), and runs unchanged on .NET host threads (reference mode and batched mode
  on the CPU), on CUDA, and on the ILGPU CPU accelerator, which is kept as a test oracle
  of the kernel path only. There is no second, scalar implementation of any part of it.

  ⚠ 2026-09-19: was the host thread, the ILGPU CPU accelerator and CUDA named as equal places of execution, with batched CPU runs on the accelerator, now the host thread and CUDA as the production paths and the accelerator as a kernel-path test oracle only → HISTORY.md#one-particle-program-host-thread-decision

  ⚠ 2026-09-19, the same day: was Debug/Release build configurations mixed across the accelerator and host-thread figures, now all re-measured in Release (host threads three to four times the accelerator; the decision stands) → HISTORY.md#one-particle-program-build-configuration
- **Double precision only.** Numerical nodes hold no `float` or `Half` value or
  operation but the one binary32 conversion of `Original` accumulation (`Particle`,
  guarded by `tests/Protocol.Tests`' `InvariantTests`). The original's REAL*4 is
  reproduced in two places only: the integers it derives from binary32 values by
  division — the array sizes `Ndok`, `Nkarm`, `Ncat`, the printed category count `DPRow`
  and the whole-fraction count of `pdoksmall`, each by integer mantissa arithmetic
  (`src/Statistics/BOOT.md`) — and the values of the `Original` precision kind below.
  The model's own arithmetic — sizes, distances, decisions — stays `double` under either
  kind.

  ⚠ 2026-09-17: was "REAL*4 is not emulated, without exception", now the binary32-derived array-size exception (`Ndok`/`Nkarm`/`Ncat`) → HISTORY.md#double-precision-only-first-exception

  ⚠ 2026-09-18: was the exception naming only `Ndok`, `Nkarm`, `Ncat`, now extended to `DPRow` and the whole-fraction count of `pdoksmall` → HISTORY.md#double-precision-only-exception-extended

  ⚠ 2026-09-21: was "the one exception is the integers …", unconditional over the model's arithmetic, now bounded by the precision kind → HISTORY.md#double-precision-only-bounded-by-the-kind

  ⚠ 2026-09-23: was bounded by the accumulation kind (accumulators only), now by the precision kind below, whose `Original` also reproduces the setup plane; the attempt plane stays `double` under either kind → HISTORY.md#precision-kind-renamed-and-extended
- **Precision kind is an option of every run, and `Binary64` is the default** (decided
  2026-09-21; extended to the setup plane and renamed 2026-09-23, to the per-cycle plane
  2026-10-01; `Double` renamed `Binary64` on 2026-09-24 for CA1720, map in
  `tests/precision-kind-rename-2026-09-24.txt`). `Binary64` computes in `double`, as the
  port always has. `Original` reproduces the binary32 values the original's executable
  computes with, in three places and nowhere else:

  - **the accumulators**: rounded to binary32 after every addition, exactly those the
    Fortran declares REAL*4 (`src/Particle`);
  - **the setup plane**: every value computed once per run before the particle loop and
    held in REAL*4, declared, by implicit typing or as a temporary of lines 378–406 —
    the formulation's values converted in binary32 from what the `.dat` says (`Input`'s
    `Length.AsWritten`, contract unchanged), literal expressions folded in binary32,
    every store rounded, a REAL*4 sum of REAL*4 terms on the listing's schedule,
    products and powers as the listing forms them (`src/Statistics`, "Setup plane");
  - **the per-cycle plane** (Fortran 771–1175): what the executable's listing does at
    each site: its REAL*4 stores, register and home reads, compiler temporaries, sum
    schedules and product order (`src/Statistics`, "## Report").
  ⚠ 2026-10-02: was source-read rules, now the listing → HISTORY.md#per-cycle-listing

  All three sets are **generated**, never typed by hand (AGENTS.md §6), but for what
  lines 379–406 and `PARAM` take from a reading of the listing, a declared deviation
  (`src/Statistics`, "Setup plane"): two from the source's declarations and statement
  structure, the third from the executable's listing by a checked address map. The setup
  plane's values are data: every consumer, the attempt plane included, receives them as
  stored. The **attempt plane** — sizes drawn, distances, decision variables — computes
  in `double` under both kinds, so this reproduces the original executable's binary32
  values, not its sample path, and the name says so.

  ⚠ 2026-10-01: was the values the Fortran stores in REAL*4, two places, the per-cycle plane `double`, now three; storage refuted → HISTORY.md#precision-kind-values

  ⚠ 2026-10-02: was "rounded once after its loop" and "every handed-on value rounded", now refuted by the executable's listing → HISTORY.md#per-cycle-register-clauses

  ⚠ 2026-10-03: was a REAL*4 sum "carried unrounded through its loop and rounded once after it" and all three sets generated, now the listing's schedule, the store schedule of lines 379–406 a declared deviation → src/Statistics/HISTORY.md#setup-rule-l-sums-2026-10-03

  ⚠ 2026-10-03: was values "declared or by implicit typing", stores and sum schedules, "the store schedule of lines 379–406" typed; now 378–406's temporaries, products and powers too, what 379–406 and PARAM take from the listing typed → HISTORY.md#precision-kind-setup-plane-stage-5b

  ⚠ 2026-09-23: was "Accumulation kind", covering the accumulators only, now "Precision kind", covering the accumulators and the setup plane → HISTORY.md#precision-kind-renamed-and-extended

  Reproduced whole, never in pieces: its normalising sum `ZSS` feeds both the fraction thresholds and λ (`src/Statistics/BOOT.md`, "## Setup plane").

  The kind is a parameter of the one `Attempt.Run`, the one `Setup.Prepare` and, through
  its `ModelSetup`, the one `CycleStatistics.Compute`, not a second implementation of
  any: "One particle program" above is untouched. The kind is recorded in the run's
  result and in `results.m`, so no number leaves the program unlabelled.

  **`Original` accumulation is sequential only.** Binary32 accumulation depends on the
  order of the additions, which no batched schedule fixes; an atomic `double` is banned
  and an imposed reduction order would not be the original's. So `Original` accumulation
  with batched mode is **refused with a status** and never silently downgraded, and
  "Reference mode is batched mode degenerated" is a claim about `Binary64` accumulation.
  The setup plane rides the same switch (decided 2026-09-23: one reproduction switch,
  not two).

  Why it exists, since it computes a worse number: the original's REAL*4 accumulators
  move its own headline answers by five to seven per cent on HPEPA3, which the
  statistical criterion's band, six to nine standard deviations wide, does not see.

- **The original generator, bit for bit.** GSV=2 is the multiplicative congruential
  generator modulo 2¹²⁸ with the original multiplier and the six seeds exactly as
  written in the source (`Random`); its 128-bit state sequence is reproduced exactly.
  Its `double` output is the original's ordered ten-term sum; agreement of that sum
  with the executable at the last ULP depends on the x87 precision control of the
  original and is not claimed.
- **Two stream layouts** (decided 2026-09-17). `Original` starts the six
  role streams at the original's seeds, with their defects. `Independent`, the default,
  starts them on six disjoint orbits of the same generator (distinct low 28 bits), so
  that no role ever shares a number with another under any jump-ahead. The layout is an
  option of every run and is recorded in its result.

  ⚠ 2026-09-24: was "`Original`, the default", now `Independent`, the default since 2026-09-21 → HISTORY.md#two-stream-layouts-default

  **The `Original` layout is sequential only** (decided 2026-09-21), for the same reason
  the `Original` precision kind is: its defining property is a dependence a parallel
  schedule cannot carry. Its lags cross **particle** boundaries — the first draw of
  particle p+1 is the last draw of the *accepted* attempt of particle p, a selected
  value — and a batched particle cannot inherit that without serialising. `Original`
  with batched mode is refused with a status, never silently downgraded, exactly as the
  precision kind is. The `Independent` layout, whose orbits are disjoint, runs in both
  modes.

  The decision rests on a measurement, not on that structural reading: on P33 batched
  `Original` put `Dkarm10` some thirty seed standard deviations from sequential
  `Original` (54.877 against 55.360) →
  HISTORY.md#two-stream-layouts-sequential-measurement

- **Reference mode is the original's sequence.** Batch size 1, one attempt per launch,
  the six original streams continued across particles and cycles: the same decisions
  in the same order as the Fortran. The pocket histogram QKS1 is refreshed after every
  attempt that completes the neighbour loop (attempts abandoned inside the loop skip
  the refresh, as in the original); rejected attempts keep all their side effects;
  accumulators are never reset between cycles.
- **Batched mode freezes only QKS1, per launch** (decided 2026-09-17, refined the same
  day). Within one launch of the particle kernel the normalized pocket histogram is
  fixed; before each launch, the first of a batch and every relaunch of its unfinished
  particles, it is refreshed if at least one attempt completed the neighbour loop since
  the last refresh (the integer total `LoopCompletions` of `Particle`); `pdoksmall`,
  `Dmaxxx` and `Dmax` are those of the cycle, as in the original; every other
  accumulator receives the batch's contributions exactly once. Each particle draws
  from its own six streams, derived from the original seeds by jump-ahead.

  ⚠ 2026-09-17: was "the histogram at the start of the batch", now "refreshed per launch, including a relaunch" → HISTORY.md#batched-mode-qks1-per-launch-wording
- **Deterministic under every schedule.** For the same formulation, parameters, mode,
  batch size, budgets, seed and accelerator kind, the result is bit-identical across
  runs and thread counts: integer accumulators are added atomically, real-valued
  accumulators go to a per-particle record folded in particle order; no atomic
  operation on a `double`.
- **Reference mode is batched mode degenerated.** Batched mode with batch 1, budget 1
  and continued streams equals reference mode bit for bit.
- **The CPU path needs no NVIDIA software.** Only `Execution` names CUDA; it falls back
  to the CPU accelerator when there is no device, no libdevice, or the kill switch
  `PROPSTRUCT_NO_CUDA=1` is set.
- **GPU equals CPU** within the tolerance tiers owned by `Execution.Tests`, whose
  figures are measured, recorded and never loosened to go green.
- **The port agrees with the original statistically** (decided 2026-09-17), by the
  criterion in `## Constraints`, on the reference formulations, **under the `Original`
  precision kind**. Under `Binary64` it differs by design wherever the original's REAL*4
  storage degrades the original's own answer ("Precision kind" above). Batched and CUDA
  runs are held to reference `Binary64` on the port's own seeds instead (link 3 of the
  acceptance criterion "The `Original` accumulation kind reproduces…").

  ⚠ 2026-09-24: was stated without the precision kind, now under `Original` only → HISTORY.md#statistical-agreement-under-original

- **No hidden state.** Numerical routines take every input, generator state and
  scratch area through parameters; numerical nodes hold no mutable static field.
- **Failures are values.** Numerical code reports a status per particle and per run
  and never throws; `Simulation` turns a failed status into an exception.

## Dependencies

None.

Outside the tree: .NET SDK 10.0 pinned by `global.json`; ILGPU 1.5.3 (NuGet), with the
libdevice post-link taken from APThermo (github.com/baryon-asymm/APThermo,
`src/Execution`); for the GPU path an NVIDIA driver and libnvvm/libdevice from CUDA
Toolkit 12.8 or newer; xunit; Python 3.8+ for the protocol linter and
the fixture generator scripts; the original program, outside the repository
(`## Delivery`), as the generator of reference outputs; GitHub Actions and nuget.org
(`.github/BOOT.md`).

⚠ 2026-10-04: was the original's executable and runtime in the tree, now outside it → HISTORY.md#delivery-decided-2026-10-04

## Constraints

- **Platform** (decided 2026-09-17): Windows x64 for version 1, CPU accelerator and
  CUDA. Nothing but CUDA library discovery may be platform-specific, so that Linux is a
  later constraint change, not a redesign. Reference machine: RTX 5070 Ti, 16 logical
  CPUs, Windows 11.
- **Language and build:** C#, .NET 10, nullable enabled, warnings are errors. Every .NET
  analyzer rule is on (`AnalysisMode=All`) and code style is enforced in the build
  (`EnforceCodeStyleInBuild`), with **no suppression anywhere**: no `NoWarn` beyond the
  SDK's own defaults, no severity lowered in an `.editorconfig`, no `#pragma warning
  disable`, no `SuppressMessage` (decided 2026-09-24, owner). A rule that conflicts with
  a design decision is satisfied by changing the code or the decision, never by
  silencing the rule. The IDE code-style rules are raised to warning in the root
  `.editorconfig` (decided 2026-09-25, owner), every preference in the direction the
  tree already follows; raising a severity is not a suppression, lowering one stays
  forbidden. IDE0005 is raised in `src` only, where the documentation file is generated
  (`tests/Protocol.Tests`' `NoSuppressionGuardTests`). Measurements before both
  decisions → HISTORY.md#language-and-build-measurements. One assembly per node
  directory holding a project; namespaces mirror the directory path (AGENTS.md §1) under
  the root namespace `PropStruct`; the tool command is `propstruct` (decided
  2026-09-17). One solution at the repository root.

- **Kernel-compatible C#** in `Random`, `Particle` and the kernel entry points of
  `Execution`: static methods, blittable structs, `ArrayView` inputs and per-particle
  scratch; no allocation, exceptions, virtual calls, LINQ, strings, recursion or
  mutable statics; 128-bit arithmetic on `ulong` pairs, not `System.UInt128`; of
  `System.Math` only the `double` overloads `Log`, `Sqrt`, `Pow`, `Acos`, `Sin`,
  `Tan`, `Abs`, `Max`, `Min`, `Floor`, each with a libdevice wrapper; integer powers
  written as products. Kernel types may be internal with
  `InternalsVisibleTo("ILGPURuntime")`; no ILGPU type on the public surface.
- **Fidelity to the original** (decided 2026-09-17):
  - literal constants stay as written where they stand: `3.14159` (sphere volume),
    `3.14` (inside the bridge volume), `12.56636` (full solid angle); the REAL*4
    rounding of literals is not reproduced, except in the setup and per-cycle planes
    under the `Original` precision kind ("Precision kind" above), where a REAL*4 literal
    is its binary32 value and a literal expression is folded in binary32; the attempt
    plane's literals stay as written under either kind;

    ⚠ 2026-09-23: was "the REAL*4 rounding of literals is not reproduced", without exception, now reproduced for the setup plane's literal expressions under `Original` → HISTORY.md#literals-setup-plane

    ⚠ 2026-10-01: was the setup plane's literal expressions only, now the per-cycle plane's literals and literal expressions too → HISTORY.md#literals-per-cycle-plane
  - every other difference is reproduced or declared, one row per defect in the
    `## Defects of the original` of the node that transcribes the lines (AGENTS.md §12),
    assembled in `docs/ORIGINAL-DEFECTS.md`; `nn_total` uninitialized → 0 and the unused
    arrays are declared in `src/Simulation`'s prose, without a row.

    ⚠ 2026-09-26: was "REAL*4 saturation of long sums" among the declared, not reproduced, now reproduced under `Original` (`src/Particle`'s row for its sums) → HISTORY.md#fidelity-real4-saturation-reproduced

- **The defect report is assembled, never written** (decided 2026-09-20). One page,
  `docs/ORIGINAL-DEFECTS.md`, answers "what is wrong with the original program, and what
  does it change"; it is **generated** by `tools/defect-report` from every transcribing
  node's `## Defects of the original`, whose table format and vocabularies are that
  tool's input contract (`tools/defect-report/API.md`), and a check fails when the page
  is stale. A row is an index into its node's own prose, never a retelling; its
  consequence is a measured effect with its place, or `not measured`, never a guess.
  The table's sixth column, `Differing cells`, is assembled into the machine-readable
  twin `docs/declared-differences.json` (`tools/defect-report/API.md`).

- **Execution model:** a cycle is `N` accepted base particles; cycle 0 is the warm-up,
  cycles 1..KXX follow; a cycle runs as batches of `B` particles (default: the whole
  cycle, capped by a 2 GiB record budget, decided 2026-09-17). A particle is one
  thread running attempts until one is accepted or the per-launch attempt budget ends;
  unfinished particles are relaunched by the host; a run-level cap fails the run with a
  status. Per-particle streams: the layout's six initial states jumped ahead by
  `seed · 2⁸⁰ + ordinal · 2⁴⁰` steps (the period is 2¹⁰⁰ since the multiplier is
  ≡ 1 mod 2²⁸).

  In batched mode the `Original` layout's role groups are jumped by distinct offsets
  (`src/Random/BOOT.md`, "Batched derivation"); `Simulation` refuses that combination
  ("Two stream layouts").

  ⚠ 2026-09-19: was a single jump for all six initial states, now grouped by role → HISTORY.md#execution-model-batched-jump-ahead-first-grouping

  ⚠ 2026-09-19, the same day: was groups {1, 2}, {3}, {4, 5}, {6}, now {1, 2}, {3, 4, 5}, {6} → HISTORY.md#execution-model-batched-jump-ahead-second-grouping
- **Statistical reference criterion** (decided 2026-09-17, revised the same day; the
  data live in `tests/Fixtures`, the rules in `tests/Harness`):
  - reference formulations: HPEPA3, inpt, P33, PSAN02n, HMX;
  - for each, the original executable is run once as shipped (the reference) and `R`
    times with its six seeds replaced, the code otherwise identical (`R = 32` for
    HPEPA3, 16 for the others): **lagged replicas** keep the `Original` layout's lag
    structure (all six seeds jumped by the same power of `a`) and validate the
    `Original` layout; **independent replicas** use the `Independent` layout's states
    and validate that layout. GSV=3 replicas of the original (library `drand`,
    clock-seeded) are kept as a cross-check, not as a pass condition;
  - every scalar and histogram cell printed in `results.m` is compared with a
    per-quantity threshold at the family-wise level `α = 10⁻³` per formulation and per
    report, over each report's machine-counted `m` — three reports (`Compare`, the
    tail-row mean, the adaptive index-matched comparison), so a run's nominal level is
    ≤ 3·10⁻³: a Student-t band `t · sd_R · √(1 + 1/R)` for continuous quantities, a
    predictive interval from the counts for count-like cells, floors from the print
    resolution; categories whose index meaning shifts between runs are compared on
    canonical axes. The exact rules are `tests/Harness`'s;

    ⚠ 2026-09-27: was one family-wise level per formulation, now per report → `tests/Harness/BOOT.md`, "## Null rate of the original"

    ⚠ 2026-09-24: was "the original's own reference output must pass against its lagged replicas, with no failure", now its null rate → HISTORY.md#statistical-criterion-no-failure-condition
  - quantities degraded by the original's REAL*4 accumulation are excluded only with
    computed evidence (term count and magnitude against the tolerance) and are then
    compared against the port's reference mode instead;
  - **the pass condition compares failure rates, not single runs** (decided 2026-09-24,
    owner); a single run passing or failing is not evidence either way, and the per-run
    bands, `α` and `R` stay as they are. What is asserted is:
    - the **null rate of the original**: the share of its runs with at least one failing
      cell, over each replica against the other `R − 1`, both layouts, and each archived
      reference against all `R` of the lagged layout, five formulations;
    - the **rate of the port**: the same share over reference-mode runs under `Original`,
      sixteen evenly spread seeds per formulation and layout, each against all `R`;
    - the **pass**: a one-sided exact test finds the port's rate no higher than the
      original's, at the tree's `α`, pooled; per formulation reported, not gated;
    - the **positive control**: the same test on `Binary64` runs rejects on HPEPA3 and
      HMX;
    - an exact snapshot of the seed-0 `results.m` under both kinds, tied to the rate
      table so that the rate cannot go stale;
    - **set-level reproduction** (`CompareSets`, decided 2026-09-27): whether the whole
      set of sixteen port runs reproduces the formulation's own replica set, gated per
      formulation and layout except PSAN02n/`Independent`, reported only
      (`tests/Harness.Tests/SetCriterionTests`, reading `tests/Fixtures/rate-runs` and
      `docs/declared-differences.json`).

    ⚠ 2026-09-26: was the null rate over "each reference against all `R`, both layouts" and the port failing "9 of 160", now the lagged layout for the references and 23 of 160 over the whole criterion → HISTORY.md#statistical-criterion-null-rate-wording

  ⚠ 2026-09-17: was GSV=3 replicas with a Student band and a Poisson floor for every quantity, now lagged/independent replicas with the per-quantity rules above → HISTORY.md#statistical-criterion-gsv3-replicas-withdrawn
- **Known bias of the original's seeds** (measured 2026-09-17, HPEPA3; the per-quantity
  table for every reference formulation is produced by `tests/Harness`). Mechanism:
  within one attempt, the position inside its fraction of neighbour n+1 is the fraction
  draw of neighbour n, so consecutive neighbour sizes are dependent, and the distance
  draw of neighbour n is, to a correlation of 0.999994, the fraction draw of neighbour
  n+1 (stream 3's transposed seed limb, see "Execution model", ⚠ 2026-09-19), so a
  neighbour's size depends on the previous neighbour's distance. Which of the two
  carries how much of the bias is not measured: the experiment that removed the whole
  shift made streams 4 and 5 independent, which cut both ties at once. The pocket and
  bridge quantities are biased, by an amount and even a sign that depend on the
  formulation: lagged against independent replicas, `Dkarm43(1)` differs by −10.9 % on
  PSAN02n and +6.1 % on P33 (difference of means; five quantities in `ACCEPTANCE.md`).

  The oxidizer **size moments** are unaffected: every one of them agrees between the
  layouts on every reference formulation, at the magnitudes `Dok43all` shows (0.05 % at
  most). The oxidizer size **distribution** is not: see the correction below. Results of
  the original, archived or new, carry this bias; the `Independent` layout does not. The
  per-quantity test is `tests/Harness`'s.

  ⚠ 2026-09-20: was "only the oxidizer size distribution … is unaffected", now the moments unaffected and the distribution not (`epsdokfr` item withdrawn 2026-09-23) → HISTORY.md#known-bias-oxidizer-distribution

  ⚠ 2026-09-27: was that withdrawal read as "an artefact of the independent replica set", drifting with the replica number; the drift is not significant at `α` (Spearman ρ = 0.546, p ≈ 0.03), so it rests on the port's own seeds alone → `tests/Harness/HISTORY.md#set-comparison-epsdokfr-evidence-2026-09-27`

  ⚠ 2026-09-17: was the mass-type quantities (`Dkarm43`, `Zkarm`, the oxidizer size) read as agreeing within about one run's spread, now withdrawn → HISTORY.md#known-bias-mass-type-quantities-withdrawn
- **Performance:** CUDA and CPU batched mode are measured against the original in
  `tests/Benchmarks`, recorded and never asserted; the figures of record and the
  attempt-budget default they led to are that node's (E-B, adopted 2026-10-01). Decided
  2026-09-27: a profile first, then only the experiments its figures select, under rules
  fixed before they run (`tests/Benchmarks/BOOT.md`, "## Post-acceptance plan"); none
  changes the particle program's arithmetic, a changed attempt budget is re-verified by
  link 3 and recorded in the run's result, and the kernel is not restructured without a
  design session.

  ⚠ 2026-09-27: was "bound by divergence and tails …, not arithmetic", now Benchmarks' figures and that a hypothesis, no profile recorded → HISTORY.md#performance-2026-09-27

  ⚠ 2026-10-01: was "CUDA level with 16 host threads on HPEPA3 and five times slower on HMX", now cited, not retold: at budget 8192 CUDA leads on both (`tests/Benchmarks`)

  ⚠ 2026-10-03: was "leads on both", then HMX only (2026-10-02), now both (group of 64): HMX 7100 ± 6 against 2085 ± 8, HPEPA3 59727 ± 1480 against 48981 ± 759

  ⚠ 2026-09-26: was "about the same as 16 host threads" unqualified, now on HPEPA3 only: on HMX CUDA runs 394 particles/s against 1954 → HISTORY.md#performance-hmx-collapse

  ⚠ 2026-09-19: was "the size of the GPU gain is unknown until an early measurement", now measured, with the decision above recorded → HISTORY.md#performance-cuda-measured
- **Repository** (decided 2026-10-04, owner): public on GitHub, `baryon-asymm/PropStruct`,
  MIT (© 2026 Eduard Burachek); branch `main`, work on `claude/<phase>` branches merged
  by the owner, Conventional Commits, English everywhere. Nothing is pushed, tagged or
  published without the owner's word, each time. The public history begins with one
  snapshot commit (2026-10-06); a commit hash cited in this tree names the private
  history before it, which the owner keeps as an archive, and does not resolve publicly.
  There is no external ancestor; the loader `CLAUDE.md` carries no subject-matter claims
  (AGENTS.md §2).

  ⚠ 2026-10-04: was a private repository, nothing published, now public with releases (`## Delivery`) → HISTORY.md#repository-public-2026-10-04

  ⚠ 2026-09-28: was a declared §15 deviation at 520 lines against 250, now within 400, criteria in `ACCEPTANCE.md` (AGENTS.md 3.1) → HISTORY.md#section-15-deviation-lifted

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No second implementation of any part of the particle program, in particular no
  "simple scalar version" for reference mode: the reference is the same code.
- No `float`, no `System.UInt128` in kernel code, no atomic operation on a `double`; the
  one exception is the binary32 rounding of the `Original` precision kind, a parameter
  of the one particle program, setup and per-cycle computation, confined to the sites
  its three generated classifications name.
- No CUDA type outside `Execution`.
- No silent fix of a defect of the original: reproduce it or declare it in the node's
  `BOOT.md` with the Fortran line.
- No loosening of a tolerance, no change of `α` or `R`, no entry in the exclusion list
  without computed evidence.
- No reading of the Fortran source from a node that does not declare it as its
  specification.
- No interactive input, no clock-seeded generator, no result depending on wall time
  except the time line of `results.m`.
- No expected value typed into a test when it exists in a fixture file.
- **Every check that guards a quantitative claim is proven twice** (adopted 2026-09-21, as a
  project rule; AGENTS.md §13 asks only for the first): red once on a violation, and **right
  once on an input whose answer is known beforehand and is of the size of the effect** — a
  positive control, passed through the same path and read where the consumer reads. Three
  defects in two days shared one shape: a check with a negative control and no positive one,
  measuring something real but not the thing, and reading as a success.
- **Every figure states what it is**: a measurement or a bound and in which direction, per
  what unit of aggregation, from how many runs and with what spread; and **a verdict may rest
  only on a figure of the matching kind**. A lower bound may not rule anything out, and a
  residual with no noise scale is not a residual. This is what "+1–4 % rather than to zero"
  and "rules the story out at 1.8 %" each cost.
- No public type outside its node's `API.md`.
- No code of the original in the repository, and no file reproducing its text or bytes
  beyond the quotation rule of `tools/legacy` (`## Delivery`).

## Decomposition

The tree is cut along the data flow and the two execution contexts. What runs inside a
particle thread is kernel-compatible and sits low (`Random`, `Particle`); what runs
once per batch or cycle on the host sits above it (`Statistics`, `Simulation`); the
original's file formats sit at the edges (`Input`, `Output`); `Execution` is the only
node that knows accelerators exist.

- `Random` is apart from `Particle` because the bit-for-bit criterion is the one claim
  provable exactly against the original, and the jump-ahead constants need one owner.
- `Particle` owns the accumulator layouts and the fold because the layout is defined
  by which side effect happens where in the attempt; `Execution` only allocates and
  launches.
- `Statistics` is apart from `Particle` because it runs on the host, once for the setup
  and once per cycle, allocates, and transcribes other lines of the Fortran.

⚠ 2026-09-26: was `Statistics` transcribing "lines 771–1175", now also the setup's lines (`src/Statistics/BOOT.md`, `## Purpose`) → HISTORY.md#decomposition-statistics-lines

- `Simulation` sits above `Execution` and `Statistics` because the cycle loop, where
  batches and statistics alternate and QKS1 is frozen, is policy, not mechanism.
- `Input` sits at the bottom so that `Output` and `Cli` can read a formulation without
  the simulator; `Output` sits above `Simulation` so that the library never depends on
  formatting.

Dependencies point one way; each node's own `## Dependencies` is the checked list
(`tests/Protocol.Tests`, `DependencyTests`). The node list, test nodes included, is in
`API.md`.

## Delivery

Decided by the owner on 2026-10-04; 0.1.0 is the first release.

- **The original, out of tree.** Its source, executable, runtime, archive and listing
  excerpt live outside the repository, in the directory `PROPSTRUCT_LEGACY_DIR` names,
  identified by `tools/legacy`'s manifest; a document naming one by its old path names
  that file there. Its data stay: the formulations, its outputs and every fixture derived
  from them. The quotation rule and the scan: [tools/legacy/BOOT.md](tools/legacy/BOOT.md).
- **The `Legacy` category.** A fact that reads the original carries `Category=Legacy`
  and fails, never skips, when the variable is unset or a hash differs. CI excludes the
  category by name; it runs on the reference machine before every release, and the
  release tag names that run.
- **Packages.** `PropStruct`, packed from `src/Output`, merges the library assemblies
  with their XML documentation and, in a `.snupkg`, their PDBs with SourceLink to the
  public commit; its one dependency is ILGPU. `PropStruct.Cli`, the .NET tool
  `propstruct` of `src/Cli`, bundles ILGPU.dll (`MIT AND NCSA`, ILGPU's licence in
  `THIRD-PARTY-NOTICES.txt`); the library is `MIT`, `NOTICE` in both. One version,
  `VersionPrefix`: tag `v<version>` equals it, `CHANGELOG.md` has its notes (`API.md`).
- **Integration, release, the GPU runner**: [.github/BOOT.md](.github/BOOT.md). CI runs on
  GitHub-hosted Windows without CUDA; CUDA runs only in the release, on the reference
  machine's self-hosted runner, never while APThermo's runner is up.
- **Not published.** `.claude/` stays untracked in the public tree. `NOTICE` credits the
  original's authors, nothing of the data (owner, 2026-10-05; `ACCEPTANCE.md`'s ⚠).
