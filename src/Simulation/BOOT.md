# BOOT.md — Simulation

## Purpose

The front door of the library: options, the simulator, the structured result. It owns
the cycle loop of the original (label 700): cycle 0 as the warm-up, cycles 1..KXX,
batches alternating with the statistics of `Statistics`; QKS1 is refreshed inside
`Execution` (before each launch in batched mode, before each attempt in reference mode,
and on every `WriteTotals`). It turns failure statuses into exceptions.

Line numbers refer to the original source `PropStructv3.for` (`## Line map` below).

## Invariants

- The cycle loop never resets accumulators between cycles (confirmed against the
  Fortran lines that zero every accumulator once, before cycle 0: `## Line map`
  below, 2026-09-24).
- Reference mode uses the original streams continued over the whole run; batched mode
  gives particle ordinals counted over the whole run.
- `Batched` with batch size 1, attempts per launch 1 and continued streams produces
  the same result as `Reference`, bit for bit, under `Binary64` accumulation.

  ⚠ 2026-09-21: was this claim unconditional, now bounded to `Binary64` accumulation and
  `Independent` stream layout together (both `Original` combinations refused for batched
  execution), which is why `DegenerateConfigurationTests` runs under `Independent` →
  HISTORY.md#degenerate-configuration-narrowing
- `SimulationOptions.Precision` reaches `Particle.Attempt.Run` through `ModelSetup.Kind`
  unchanged, and is recorded in the result beside the stream layout
  (`RunDiagnostics.Precision`, the same shape as `Streams`, for the same reason).
  `Original` accumulation is refused for batched execution unconditionally over how
  batched execution is reached — a plain batch or the degenerate `BatchSize == 1` /
  `ContinuedStreams` configuration alike — with
  `SimulationFailedException(RunStatus.InvalidSetup, …)`, the message naming both the
  requested precision kind and the requested mode (decided with the root, 2026-09-21:
  reusing `InvalidSetup` rather than adding a status value, so a consumer already
  reporting `InvalidSetup` by echoing the message needs no new case).
- `SimulationOptions.Streams` reaches `Execution.Engine.RunBatch` as the `layout`
  argument unchanged (translated through `StreamLayoutMapping`), and is recorded in the
  result as it always was (`RunDiagnostics.Streams`). `Original` stream layout is
  refused for batched execution unconditionally over how batched execution is reached —
  a plain batch or the degenerate `BatchSize == 1`/`ContinuedStreams` configuration
  alike — with `SimulationFailedException(RunStatus.InvalidSetup, …)`, the message
  naming both the requested layout and the requested mode, mirroring the precision-kind
  refusal above in structure (decided with the root, 2026-09-21; root BOOT.md, "Two
  stream layouts"). `SimulationOptions.Streams`' own default is `Independent`, not
  `Original` (below, "Constraints", "Default stream layout").
- **Both refusals are `Simulator.Create`'s own, enforced by `SimulationOptionsValidator`
  before an engine, a formulation or any setup work exists** (architecture audit finding
  R1, 2026-09-24: they used to surface only from deep inside
  `Execution.Engine.RunBatch`/`RunContinuedBatch`, after `Statistics.Setup.Prepare`, the
  tail draw and `Engine.Load` had already run). `Execution` keeps only the precision
  refusal, as a mechanism backstop for a caller that reaches `Engine` directly — its own
  batch-fold path genuinely cannot reproduce binary32 accumulation, at any particle
  count (`src/Execution/BOOT.md`, "`Original` accumulation is refused for batched
  execution"). The stream-layout refusal has no such mechanism reason and is withdrawn
  from `Execution` entirely: `Engine` derives and runs either layout's streams correctly
  (`src/Execution/BOOT.md`'s own 2026-09-26 note), so this node's own validator is the
  only place it is enforced. `Cli` calls the same validator's
  `IsContinuedStreamsCombinationInvalid` for its one parse-time rule
  (`SimulationOptionsValidator.cs`, `InternalsVisibleTo`), so that rule is typed once.
- The result holds every quantity printed by the original, under true names (all seven
  generator accuracies), and run diagnostics (mode, batch size, attempts per launch —
  the *effective* budget, 1 in reference mode — accelerator, streams, precision kind,
  seed, continued streams, attempts, relaunches, elapsed time).
- Elapsed time is the only field that depends on wall time.
- `Execution`'s `ParticleNotRun` (a launch skipped a particle) is a defect of the
  engine, not a run status: `Simulator.Run` throws `InvalidOperationException` for it,
  never `SimulationFailedException` or an `ArgumentException` (decided 2026-10-02).

## Dependencies

- [Input](../Input/API.md) — formulation and parameters.
- [Random](../Random/API.md) — the original streams for reference mode.
- [Particle](../Particle/API.md) — layouts, attempt outcome.
- [Statistics](../Statistics/API.md) — setup and end-of-cycle processing.
- [Execution](../Execution/API.md) — the engine.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Defaults: batch size the whole cycle capped by a 2 GiB record budget; the attempt
  budget per launch, the run-level attempt cap and the neighbour budget are options
  with defaults fixed in this node's design session.
- **Default stream layout is `Independent`, not `Original`** (decided 2026-09-21, with
  the root, replacing the 2026-09-17 default). Before this task the defaults were
  `Mode = Batched` and `Streams = Original` together — the one combination the new
  refusal above always rejects, so every bare invocation (`propstruct run <file>` with
  no `--mode`/`--layout`, and every library caller passing `SimulationOptions.Parameters`
  alone) would otherwise refuse itself with `InvalidSetup` from its very first cycle-0
  batch. Two ways to make the defaults coherent again were open: move `Mode`'s own
  default to `Reference` (keeps `Original`, the original's own sequence, but a bare run
  becomes single-threaded — roughly 5× slower than 16 host threads on HPEPA3 by root
  BOOT.md's own recorded acceptance-criterion figures, "reference" against "host16" —
  and silently reinherits the original's own known bias, root BOOT.md "Known bias of
  the original's seeds"); or
  move `Streams`' own default to `Independent` (keeps `Mode = Batched`, the execution
  path root BOOT.md's own Purpose names as the reason this port exists: "rebuilt so that
  one numerical program runs ... on an NVIDIA GPU"). The second is chosen: it keeps a
  bare run fast, and its numbers are the ones root BOOT.md's own "Known bias" section
  already documents as free of the `Original` layout's bias. **What a user who relied on
  the old default now gets:** a bare `propstruct run` used to run batched, silently
  reproducing the original's own biased stream sequence (root BOOT.md, "Known bias");
  it now runs batched with the unbiased `Independent` layout instead — a different
  answer, not merely a different default value, for anyone who never named `--layout`.
  Reproducing the legacy sequence needs both `--layout original` and `--mode reference`
  named explicitly from now on. `src/Cli/API.md`'s own defaults are `SimulationOptions`'
  own (`src/Cli/API.md`, "Run options"), so no change was needed in `Cli` itself for a
  bare invocation to pick up the new default; this is noted here as a fact about that
  neighbour's behaviour, not a change to its contract.
- `PrecisionKind.Double` was a declared CA1720 exception, lifted 2026-09-24 (renamed
  tree-wide to `PrecisionKind.Binary64`) →
  HISTORY.md#precisionkind-double-ca1720-exception-lifted

## Line map

This node's Fortran source is `tests/Fixtures/Legacy/PropStructv3.for.txt`, lines
278–375 and 407–424: the state that exists once per run, before and around the cycle
loop (label 700) this node owns (`## Purpose`). Declared 2026-09-24, closing a
fidelity-audit finding — the root invariant "accumulators are never reset between
cycles" above and `src/Particle/BOOT.md`'s own claim about line 299 both rested on
these lines without this node (or any node) declaring them.

| Fortran | C# member | Note |
|---|---|---|
| 278–296 | — | array allocation (`Allocate(...)`), not this node's own: the port's equivalent buffers are built once by `Statistics.Setup.Prepare` and `Particle`'s accumulator layout, driven from here (`## Design decisions`, "The run, step by step") |
| 297–342 | — | every accumulator zeroed once, before cycle 0 (`XSS0`–`XSS4` at 299 through `qdokstr` at 342); the port's equivalent totals start at their default/zero state once, built by `Engine.Load`, and nothing below (408–424) rewrites them |
| 343 | — | comment |
| 344–374 | — | GSV=1 branch, not ported (root BOOT.md: GSV=2 only) |
| 375 | — | comment |
| 407 | — | comment |
| 408–423 | `Simulator.Run` (cycle-loop head, label 700) | per-cycle console progress (`Its`, `IPRIS`/`KPRIS` echoed to the screen) and the GSV=3 branch (409–413, not ported, root: GSV=2 only); not ported — `Cli` prints its own progress (`src/Cli/BOOT.md`, "Progress and the summary"); no accumulator is read or written here |
| 424 | — | comment |

**Both claims this range was declared to check hold.** The zeroing block (297–342)
runs exactly once, before label 700 (408) is ever reached, and the cycle-loop head
(408–424) neither zeroes nor otherwise touches any accumulator named at 297–342 — it
only manages `Ist`, `Its`, the progress print and the GSV=3 branch. This confirms
"The cycle loop never resets accumulators between cycles" above.

`src/Particle/BOOT.md`'s claim that line 299 zeroes only `XSS0`–`XSS4`, and that
`Xss5`, `Xss6` and `nn_total` are never initialized, is confirmed: line 299 reads
exactly `XSS0=0.; XSS1=0.; XSS2=0.; XSS3=0.; XSS4=0.`, and a search of the whole
source file (not only this node's own range) finds `XSS5`/`XSS6` only at their `+=`
sites (lines 621, 661, 697, within `src/Particle/BOOT.md`'s own declared range) and
`nn_total` only at its own `+=` site (line 743, the same range) — neither is
assigned anywhere else in the file. (2026-09-24, this verification.)

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No model arithmetic here: it belongs to `Particle` or `Statistics`.
- No formatting of results.

## Design decisions (2026-09-18)

Settled in the design session that opened wave 7, closing the open questions:

- **The run, step by step.** `Statistics.Setup.Prepare`, the tail draw through
  `Engine.SampleSize`, the echoes completed (`API.md`, "Setup hand-off"), `Engine.Load`.
  Then for `cycle = 0 … KXX`: `SetCycle` (flag 0 for the warm-up, 1 afterwards;
  `pdoksmall` and `Dmaxxx` from the setup for cycle 0 and from the previous cycle's
  `Compute` afterwards); `N` particles, as batches of at most
  `min(BatchSize ?? N, Engine.MaxBatchSize)` in batched mode, or one
  `RunReferenceParticle` per particle in reference mode; `ReadTotals`;
  `CycleStatistics.Compute` with its in-place rewrites; `WriteTotals`. Any status other
  than `Ok` from any of these ends the run with `SimulationFailedException`.
- **Streams and ordinals.** Reference mode: one `StreamSet`, the layout's initial
  states (for `Seed = 0` the layout's own seeds), continued over the whole run. Batched
  mode: particle ordinal `cycle · N + index` over the whole run, streams derived per
  particle by the engine. `ContinuedStreams = true` (batch size 1 only) makes batched
  mode use reference mode's single continued `StreamSet` through
  `RunContinuedBatch`: the degenerate configuration of the root invariant.
- **Accelerator by mode.** Reference mode needs a CPU engine: `Auto` means `Cpu` there,
  and `Cuda` with `Reference` is an `ArgumentException` before any work.
- **The result mirrors `CycleReport`.** `SimulationResult`'s records carry, under the
  names of `src/Statistics/BOOT.md`, "## Report", every field of the last cycle's
  `CycleReport` in SI units, plus the per-cycle convergence series the original prints
  when `KXX > 1` (from each cycle's report), plus the run diagnostics. "Every field" is
  checked by a reflection test against `CycleReport`, not by a typed list. Unit scaling
  for printing is `Output`'s.
- **Budget defaults are measured, not chosen.** The coding session runs the five
  reference formulations, records for each the largest per-particle attempt count,
  neighbour draws per attempt and pocket redraws per bridge, and sets each default to at
  least 100 times the largest observed value; attempts per launch is the smallest power
  of two at which 99 % of HPEPA3's particles finish in the first launch. Figures and
  defaults are recorded here.

  ⚠ 2026-09-28: attempts-per-launch is reconsidered by the selection rule below, not
  by the 99 % rule alone → "## Budget selection rule (2026-09-28)".
- **Progress and cancellation.** Progress is reported after every batch, and after every
  1000 particles in reference mode; a `CancellationToken` is observed at the same points
  and ends the run with `OperationCanceledException`. Neither changes a result: a run
  that is not cancelled gives the same bits with or without either.
- **Counters are the first integration evidence.** A reference-mode run of HPEPA3 must
  reproduce the reference's printed counters (`NFX`, `NFY`, `NFQ`, `NFW`, `Nkarm`, the
  nine conditions per particle, bridges per particle) within a few per cent, compared by
  name through `tests/Harness`' parser, the figures recorded. The `Original` layout
  reproduces the original's streams and REAL*4 storage, but the sample path parts from
  the original's at the first REAL*4-sensitive branch: close, not exact. It caught the
  QKS1 defect of `Execution`; it runs before any other acceptance of this node.

  ⚠ 2026-10-02: was "x87 and REAL*4 make branch-exact replay unreachable", now the sample path alone (root ⚠ 2026-09-21)
- **What this node does not open yet.** The statistical criterion compares printed cells
  of `results.m`, and the port's cells come from `Output`. The criterion rows of
  `## Acceptance criteria` are therefore coded together with `Output`, in the wave after
  this one.

## Result field mapping

`SimulationResult`'s member records carry every field of `Statistics.CycleReport` under
their own names (`tests/Simulation.Tests`' own reflection test checks the name is
reachable somewhere, not which record hosts it); this table is the record each one was
put in, so a reader can find a field without grepping:

| Record | `CycleReport` fields |
|---|---|
| `RunHeader` | `Dokm`, `Doksd`, `Ddokmax`, `Dmax`, `TailProbabilityModified`, `Ggg`, `Zx` (setup echoes), plus the formulation's own name/cycles/particle count |
| `Counters` | `Qkss`, `Nfx`, `Nfy`, `Nfz`, `Nfq`, `Nfw`, `Conditions`, `IbridgeTotal`, `JammedTotal`, `NnTotal` |
| `GeneratorAccuracy` | `Eps1`-`Eps7`, `Epsx1`-`Epsx3`, `Epsalldok`, `Epsy`, `Epsmd4`, `Epsmd3`, both warning flags |
| `ParticleSizes` | `Dok43b`, `Dok43s`, `Alldok43`, `Alldok432` (oxidizer-size scalars) |
| `LocalStructure` | `Gdokleft`, `Vdokleft`, `Plotsmdok`, `Plotsm`, `Mp` (the "Matrix" group) |
| `Pockets` | `Dp43`, `D432`, `Sdevp43`, `Dqkarm`, `Dkarm43Cor`, `Sdevp43Cor`, `DqkarmCor`, `Dfmk432`, `Sdevp243`, `DpMax`, `DpMaxCor`, `DpRow`, `Dpockets`, `Dokp43`, `Qdokkarm`, `Qdokso` |
| `MassFractions` | `DolM1`, `DolM2`, `DolM3` |
| `Agglomerates` | `Dqmkm1`, `Dqmkm2`, `Qmcoef`, `CoefNmax`, `Qmkm1Nmax`, `Qmkm2Nmax` |
| `Histograms` | `Allvdokso`, `Vkso`, `Qks1`, `FmkarmCorNormalized`, `Fmkarm2Normalized`, `FqkarmCorNormalized`, `Qmkm1Normalized`, `Qmkm2Normalized`, `CoefNormalized`, `Pdoksmall` |
| `Convergence` (nullable, `KXX > 1` only) | `ConvergenceEpsy`, `ConvergenceEpsmd3`, `ConvergenceEpsmd4`, `ConvergenceAlldok43`, `ConvergenceAlldoksd`, `ConvergenceDolM2`, each broadened from `CycleReport`'s own scalar to a per-cycle series (`ImmutableArray<double>`, one entry per completed post-warm-up cycle) — the one field-by-field departure from a one-to-one type mirror, matched by name only in the reflection test |

`RunDiagnostics` carries only run bookkeeping with no `CycleReport` counterpart (mode,
batch size, attempts per launch, accelerator, streams, seed, `ContinuedStreams`, total
attempts, total launches, elapsed time). `StoredSetup` (API.md, "## Stored setup",
added 2026-09-24) is the same shape of addition, built once from `Setup.Prepare`'s own
`Statistics.SetupInputs`/`ModelSetup`/`SetupTables.MassShare` out-parameters, not from
`CycleReport`. `Setup.Prepare` now sets its own `ModelSetup.Kind`; `Simulator.Run` no
longer patches it after the call (`src/Statistics/BOOT.md`, "## Setup plane", "Design
decision, 2026-09-24", the audit finding this closes).

`StreamLayout` (public, this node's own mirror of `Random`'s internal enum of the same
two cases) and the internal `PocketRedrawBudget` are two departures from the API.md
sketch, both decided in this coding session and recorded here rather than left as a
silent surface change:

- `Random.StreamLayout` is internal (`Random/API.md`); a public `SimulationOptions`
  cannot expose it directly (`CS0053`). `src/Simulation/StreamLayout.cs` adds a public
  mirror with the same two cases and translates it to `Random`'s own enum at the one
  place `Simulator` calls into `Random`/`Execution`. Escalating to widen `Random`'s own
  surface (AGENTS.md §11) was considered and rejected: the mirror is a one-line
  translation this node can own itself, not a capability `Random` is missing.
- `Statistics.Setup.Prepare` takes a `pocketRedrawBudget` parameter
  (`Particle/API.md`, `ModelSetup.PocketRedrawBudget`) that this node's "Defaults"
  constraint never names as an option. It is therefore not public:
  `SimulationOptions.PocketRedrawBudget` is an `internal` init property (validated
  positive like the other budgets, reachable by this node's tests), defaulting to
  `SimulationDefaults.PocketRedrawBudget`, measured like the other three ("## Budget
  measurement").

  ⚠ 2026-09-19: was "`SimulationOptions` has no such field, the internal constant 10,000
  never observed to bind, needing no measurement", now 12,800, measured, and an internal
  option → HISTORY.md#pocket-redraw-budget-measurement-correction

## Counter check (2026-09-19)

Reference mode, HPEPA3, `Original` layout, `Seed = 0`, the whole formulation (both
cycles, 200,000 accepted particles): `tests/Simulation.Tests/Hpepa3CounterTests`.
Every quantity is compared as an integer count: the port's own counter against the
reference's printed value times the original's denominator (Fortran lines 1248–1256).
`FI` is the number of accepted particles of cycles ≥ 1 (`KXX · N`, 100,000 here);
conditions 1–5 are printed per `FI + N` (200,000, the warm-up included), conditions 6–9
and the bridges per particle per `FI` alone.

| Quantity | Denominator | Reference (events) | Port (events) | Difference | Bound |
|---|---|---|---|---|---|
| `NFX` | 1 | 1,336,594 | 1,336,449 | 145 | 66,830 |
| `NFY` | 1 | 117,621,203 | 117,653,096 | 31,893 | 5,881,060 |
| `NFQ` | 1 | 1,292,687 | 1,293,915 | 1,228 | 64,634 |
| `NFW` | 1 | 13,727,046 | 13,737,688 | 10,642 | 686,352 |
| `Nkarm` (`Qkss`) | 1 | 24,649,481 | 24,649,563 | 82 | 1,232,474 |
| Condition 1 (`Dok > Dmax`) | `FI + N` | 0 | 0 | 0 | 0 |
| Condition 2 (`Dok < Dmin`) | `FI + N` | 11 (5.5e-05) | 0 | 11 | 13.3 |
| Condition 3 (`Dbase < 0.5Dok`) | `FI + N` | 1,135,342 (5.67671) | 1,135,179 (5.675895) | 163 | 56,767 |
| Condition 4 (`Dbase > 2.0Dok`) | `FI + N` | 89,277,020 (446.3851) | 89,307,391 (446.537) | 30,371 | 4,463,851 |
| Condition 5 (`l > 4.7Dbase`) | `FI + N` | 0 | 0 | 0 | 0 |
| Condition 6 (`Nkarm = 0`) | `FI` | 0 | 0 | 0 | 0 |
| Condition 7 (`Nmkm < 2`) | `FI` | 1,211 (0.01211) | 1,231 (0.01231) | 20 | 197.7 |
| Condition 8 (`Nkarm/Nmkm < min`) | `FI` | 41 (0.00041) | 39 (0.00039) | 2 | 35.8 |
| Condition 9 (`Nkarm/Nmkm > max`) | `FI` | 213 (0.00213) | 221 (0.00221) | 8 | 83.3 |
| Bridges per particle | `FI` | 436,319 (4.36319) | 436,245 (4.36245) | 74 | 21,816 |

The bound is the larger of 5 % of the reference count and four standard deviations of
the difference of two Poisson counts, `4 · √(reference + port)`: four is the two-sided
normal quantile of 6.7·10⁻⁵, the root criterion's family-wise `α = 10⁻³` shared over the
15 quantities. A large count is thus held to a few per cent, a count of a few hundred to
its own spread, and a reference count of 0 to exactly 0. Elapsed: 95.3 s for 200,000
particles on the reference machine's host thread (2,098 particles/s, against the
original's ≈ 7,400 particles/s for the same run in 27 s single-threaded), recorded, not
asserted (root BOOT.md, "Performance").

Proven non-degenerate on 2026-09-19 by two mutations of the test's denominators, each
red and then reverted: conditions 6–9 per `2 · FI` (condition 7: 2,422 reference events
against 1,231, bound 241.8), conditions 1–5 per `2 · (FI + N)` (condition 2: 22 against 0,
bound 18.8). Its limit of power is recorded with it: condition 8 (41 events) would still
pass a doubled denominator (82 against 39, bound 44.4); the rows with hundreds of events
and more carry the check.

**Condition 2 is diagnosed, not explained away.** The port draws no `Dok < Dmin` event
where the original drew 11, while the two runs otherwise track each other closely
(`NFX` within 145 of 1.34 million, `Nkarm` within 82 of 24.6 million), so the
difference is systematic, not sampling. Across the five reference outputs
(`tests/Fixtures/references/*/results.m.txt`) the original records condition 2 only for
`JZ = 1` formulations whose smallest fraction bound equals `Dmin` (HPEPA3: 10 µm and
10 µm, 5.5·10⁻⁵; HMX: the same, 1.25·10⁻³); P33 has the same bounds with `JZ = 2` and
records 0, and inpt and PSAN02n, whose smallest bound lies above `Dmin`, record 0. In
exact arithmetic a draw inside a fraction never falls below its lower bound; the
original's events come from the REAL*4 store of the drawn size, which rounds a draw a
hair above `Dmin` down to it before the test — measured 2026-09-23 by `Particle`, which
owns and declares the defect (`src/Particle/BOOT.md`, its row for 478, 533,
1761–1776). The drawn size is attempt-plane arithmetic, `double` under either
precision kind (root BOOT.md, "Precision kind is an option of every run"), so the port
draws none: 0 in all 192 stored rate runs of HPEPA3, HMX and P33. It stays inside the
bound here.

⚠ 2026-10-02: was "rounding of the `1/D²` law at that bound (x87 and REAL*4 …)", a
hypothesis, with a row of this node's own `## Defects of the original`; the 2026-09-23
measurement names the REAL*4 store alone, and the row was a second row of `Particle`'s
defect (root BOOT.md, "one row per defect"), now removed (arbitration, D5).

⚠ 2026-09-19: was conditions 6–9 divided by `Nbase` (`FI + N`) with an absolute floor of
1.0 that no defect could exceed, now the original's own denominators and the count bound
above → HISTORY.md#counter-check-nbase-denominator-correction

⚠ 2026-09-24: cited test names renamed for CA1707, meaning unchanged, no criterion
re-verified and no date moved (`tests/test-renames-2026-09-24.txt`).

## Budget measurement (2026-09-19)

Method. Each budget, when it is never reached, leaves a run unchanged, so success is
monotone in it. The largest attempt count per particle was read exactly from a
reference-mode run of each formulation, driven through `Engine.RunReferenceParticle`
so that every particle's own `BatchCounters.Attempts` is visible, and bracketed again in
batched mode. The neighbour draws per attempt and the pocket redraws per bridge have no
counter on any published contract, so they were bracketed by the failure status of whole
batched runs (CPU accelerator, the other budgets at 2³⁰): doubling from 1 until the
whole formulation completes, then bisecting to a quarter of the bracket; an interval
`(a, b]` means the run failed at `a` and completed at `b`. Every default is at least 100
times the upper end of the largest interval. The scratch
(`tests/Simulation.Tests/BudgetMeasurementScratch`) was run once on 2026-09-19 and
deleted, as `Execution/BOOT.md`'s one-off diagnostics were.

| Formulation | Attempts per particle, reference (mean / max) | Batched | Neighbour draws per attempt | Pocket redraws per bridge |
|---|---|---|---|---|
| HPEPA3 | 6.68 / 90 | (80, 96] | (196,608, 198,656] | (96, 128] |
| inpt | 10.84 / 114 | (96, 128] | (40, 48] | (3, 4] |
| P33 | 2.22 / 18 | (20, 24] | 26,287 (exact) | (20, 24] |
| PSAN02n | 15.57 / 205 | (192, 256] | (40, 48] | (6, 8] |
| HMX | 325.81 / 4,412 | (3,072, 4,096] | (131,072, 163,840] | (40, 48] |

The reference-mode attempt totals are the port's `NFX` (HPEPA3 1,336,449; P33 88,763,
equal to the original's; HMX 6,516,126 against the original's 6,507,516).

Attempts per launch is 8192 since 2026-10-01, chosen by "## Budget selection rule
(2026-09-28)" on the ladder of `tests/Simulation.Tests/BOOT.md`, "## Budget ladder".
⚠ 2026-10-01: was 32 by HPEPA3's 99 % rule (99.457 % within 32 attempts), now 8192 by
the 2026-09-28 rule → HISTORY.md#attempts-per-launch-99-percent-rule

| Default | Value | Basis |
|---|---|---|
| `AttemptsPerLaunch` | 8192 | the 2026-09-28 selection rule over the 2026-10-01 ladder |
| `MaxAttemptsPerParticle` | 500,000 | ≥ 100 × 4,412 (HMX) |
| `NeighbourBudget` | 20,000,000 | ≥ 100 × 198,656 (HPEPA3) |
| `PocketRedrawBudget` (internal) | 12,800 | 100 × 128 (HPEPA3) |

An early, single HPEPA3 host-thread timing taken along the way here is superseded by
`tests/Benchmarks/BOOT.md`'s own measured "## Figures" →
HISTORY.md#budget-measurement-early-throughput-note.

⚠ 2026-09-19: was defaults 128/500,000/30,000,000 from an unrecorded previous session,
now every default re-measured above (the attempt cap unchanged, the other three moved)
→ HISTORY.md#budget-defaults-previous-session-values

## Budget selection rule (2026-09-28)

Pre-registered before the ladder of `tests/Simulation.Tests` runs at a budget other
than 32, per the arbiter's 2026-09-28 review (owner's delegation) after E-B's adoption.

- **Candidates:** `L = 32, 128, 512, 2048, 8192` (E-B's adopted values, today's
  default), one run each on the ladder's own commit.
- **Instrument, frozen as-is:** `Binary64`/`Independent`, `k·2¹⁷` seeds for
  `k = 0..7`, `TwoSampleBiasOfSets`/`ConstantCellRule` at `α = 10⁻³`/formulation,
  five formulations, host threads and CUDA (reference mode never reads the budget).
- **Green** needs both accelerators green on all five formulations, a CUDA refusal
  is not green; **choice** is the largest `B` in `L` with every smaller candidate
  green too — a red anywhere caps the choice below it.
- **Fallback:** `B* = 32` moves nothing, blocked by link 3, red cells recorded, the
  refresh rule to a design session; a red 32 goes to the owner first instead.
- **Frozen after the first run:** no change to seeds, `α`, `R`, the comparator, the
  cell rule or `L` — every run is deterministic, so only the instrument moves it.

A green gate bounds only what link 3 can see (4.6-5.1 seed sd), not a coarser QKS1
refresh's staleness bias, which the ladder's paired-shift figures state as a size, not
a verdict (root taboo: "a verdict rests only on a matching figure").
