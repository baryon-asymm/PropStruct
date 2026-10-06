# BOOT.md — Benchmarks

## Purpose

The node that answers "how fast is the port against the original", and records the
answer with enough provenance that a figure can be trusted a month later or refuted by
a re-run. The root criterion it serves: the speed of batched CPU and CUDA against the
original on HPEPA3 and HMX is **recorded**, not asserted.

What is measured, per formulation: accepted base particles per second on cycle 1, for
reference mode, the host-thread path on 1 and on 16 threads, and CUDA; and, for the same
work, the original executable's own wall time.

⚠ 2026-09-20: was the ILGPU CPU accelerator among the measured paths, now none: its
kernel-launch path is unreachable from here; `--paths cpu` answers "not available" →
HISTORY.md#benchmarks-purpose-cpu-row-2026-09-20

The unit is accepted particles per second because attempts per particle differ between
layouts and modes, so attempts per second compares nothing.

## Invariants

- **Nothing is asserted.** A performance threshold in a test suite is a flake generator:
  it goes red on a busy machine and teaches the reader to ignore red. This node records
  figures; the tree's decisions cite them.
- **Every figure is from a Release build**, and says so. The tree already paid for this
  once: figures measured under `dotnet test`'s default Debug build understated the host
  path by a factor of three (root `BOOT.md`, "One particle program", ⚠ 2026-09-19).
- **A figure without provenance is not a figure.** Each row carries the date, the
  machine (CPU, logical cores, GPU, driver), the .NET version, the commit, the exact
  command, and the formulation with its cycle and particle count.
- **The original's figure is measured, never remembered.** It comes from a run of the
  legacy executable on the same machine in the same week, through `tests/Fixtures`'
  provenance rules; the 27 s of the root `BOOT.md` is a starting point, not a citation.
- **The port's figures come from the library**, through `Simulation`, with the same
  options a user would pass — not from an internal loop that skips the cycle policy.

## Dependencies

- [Simulation](../../src/Simulation/API.md) — the runs being measured.
- [Input](../../src/Input/API.md) — the formulations.
- [Execution](../../src/Execution/API.md) — the accelerator kinds and what is available.
- [Harness](../Harness/API.md) — repository paths, fixture lookup.
- [Fixtures](../Fixtures/API.md) — the formulations and the provenance rules.
- [legacy](../../tools/legacy/API.md) — `legacy.py path`, through which the baseline
  row finds the original's executable and runtime.

Outside the tree: the original `PropStructV3.exe` with `dforrt.dll` for the baseline
row, at `PROPSTRUCT_LEGACY_DIR`.

⚠ 2026-09-20: was BenchmarkDotNet named among the outside-the-tree tools, now dropped,
unused → HISTORY.md#benchmarks-dependencies-benchmarkdotnet-2026-09-20

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- A console project, not a test project: it is run by hand, never by `dotnet test`, so
  that no CI run and no fast set ever waits on it.
- One cycle is enough. A benchmark that runs all `KXX` cycles buys nothing and costs
  minutes; the cycle policy is measured once, separately, and noted.
- The CUDA rows are skipped, not failed, where there is no device; the record says
  "not measured on this machine" rather than leaving a blank that reads as zero.
- The node writes its figures into this document, in the table below, in the same
  commit as the run. There is no generated report file: a table nobody reads in a
  document everybody reads is better than a file nobody opens.

## Implementation notes (2026-09-20 coding session)

Four points BOOT.md left to this session to resolve, kept here rather than only in
code comments, since each narrows or fills a gap the design left open (AGENTS.md §4:
"implementation may reveal a need to refine the contract").

- **Cycle 1 isolation.** `Simulator.Run(formulation, progress)` reports
  `CycleProgress(Cycle, Cycles, AcceptedParticles)`; API.md does not say how often
  within a cycle. `CycleBoundaryRecorder` keeps only the *last* report seen for
  `Cycle == 0` as the cycle-1 window's start, and the timestamp `Run` returns at as its
  end, so the figure is correct regardless of that reporting frequency. A formulation
  whose cycle 0 produces no progress report at all falls back to timing the whole call
  (warm-up included); the row then says so ("warm-up not isolated") instead of quietly
  overstating cycle 1's cost. Every port formulation runs with `Cycles = 1` (a `with`
  expression on the formulation `Input.DatFile.Read` returns), so `Run` always executes
  exactly cycle 0 then cycle 1 and returns.
- **Host-thread counts.** `Simulation`'s public `SimulationOptions` has no field for
  the host-thread path's own thread count (`Execution.Engine.Create`'s `cpuThreads` is
  internal, `InternalsVisibleTo` for `Simulation`/`Cli`/their tests only,
  src/Execution/API.md); `Simulator.Create` takes only `SimulationOptions`. `host1` and
  `host16` therefore relaunch this same executable as a child process, once per repeat,
  in its own hidden `--internal-cycle1-run` mode, with `DOTNET_PROCESSOR_COUNT` set on
  that child alone: the .NET runtime reads it once at process start and reports it back
  through `Environment.ProcessorCount`, which the host-thread path's own `Parallel.For`
  calls consult by default (`MaxDegreeOfParallelism = -1`, "all cores", the production
  setting named in src/Execution/API.md). Verified this session against a throwaway
  console app: `Environment.ProcessorCount` read 16 unset and 1 with
  `DOTNET_PROCESSOR_COUNT=1` on the reference machine. This is an external, process-level
  lever, not a second implementation and not a bypass of `Simulation`'s own cycle
  policy: the relaunched process still calls `Simulator.Run` exactly as any other
  caller would, once per repeat instead of once per particle.

  ⚠ 2026-10-02: was "forced processor count" read as a thread count, now a partitioning
  input only: `Environment.ProcessorCount` steers how `Parallel.For` splits its range,
  not how many threads run, so a `host1` child ran on several CPUs (`host1` 28491 ± 2466
  against `reference` 9291 ± 19 on HPEPA3, Stage 0). Deciding figure, child CPU time ÷
  wall time: 2.42-2.60 unpinned (n = 4), 0.996-1.001 pinned to one CPU (n = 3); the
  proof in full → `HISTORY.md#benchmarks-boot-trims-2026-10-02`. `host1` now pins its
  child (`Process.ProcessorAffinity`, the highest CPU of the parent's own affinity) and
  records no row if the pin fails; a real `--paths host1 --particles 5000 --repeats 1`
  run (output discarded) showed the child at affinity 0x8000. Every earlier `host1` row
  is stale in this sense too → HISTORY.md#figures-stage0-2026-09-28
- **Provenance's commit** (2026-10-02). `git rev-parse --short HEAD`, plus `-dirty` when
  `git status --porcelain` reports anything at the moment the command starts. Modified,
  staged and untracked files count (the project globs its sources, so a new `.cs` file
  is built into the figure); ignored files (`bin/`, `obj/`) do not; `unknown` if git
  cannot answer. Proof: four `--paths cpu` runs, one per state (two modified files,
  clean, one untracked file, removed), and the Stage 0 rows' `ce30f62` read as a dirty
  tree → `HISTORY.md#benchmarks-boot-trims-2026-10-02`.
- **Original's particles/s.** The legacy executable reports no internal per-cycle
  timing; its wall time covers cycle 0's warm-up and every cycle 1..KXX together, so
  the `original` row's accepted-particle count is `(Cycles + 1) × ParticlesPerCycle`,
  the *whole run's* throughput, not a cycle-1-only figure like every other row's. A
  reader comparing this row against the port's cycle-1-only rows should expect it to
  read a little low if cycle 0's own cost differs from a later cycle's — documented
  here rather than guessed away. `--particles` never patches the legacy `.dat`: a
  Fortran fixed-format record cannot safely be edited without reading `Input`'s own
  parser, which this node may not do (AGENTS.md §3); the `original` row always measures
  the formulation's own shipped `N` and `KXX`.
- **Escalation: the CPU accelerator oracle path (AGENTS.md §11).** BOOT.md's own
  Purpose and API.md's example command line both name a `cpu` row for "the ILGPU CPU
  accelerator (the kernel oracle, for the record only)" — root BOOT.md's third,
  test-oracle-only execution context, distinct from the host-thread path `host1`/
  `host16` already cover. It cannot be reached from this node: the kernel-launch path
  on the CPU accelerator is chosen by `Execution.Engine.Create`'s
  `forceIlgpuKernelsOnCpu` parameter, internal to `Execution`
  (`InternalsVisibleTo` for `Simulation`, `Cli` and their own tests only,
  src/Execution/API.md), never set `true` by `Simulation`, the only production caller;
  `Simulation`'s public surface has no equivalent. Reaching it would need either a new
  `Simulation` option that forces the oracle path, or `Execution` granting this node
  `InternalsVisibleTo` — both changes to a neighbour's contract, outside this node's
  subtree, so this session does not make either (AGENTS.md §11). The `cpu` path is
  implemented as an explicit `not available` outcome naming this gap, distinct from
  "not measured on this machine" (a per-machine condition, not a permanent one). The
  proposal: whichever of `Simulation`/`Execution`'s owners next touches that surface
  decides which lever to add; until then this row stays `not available` and the first
  acceptance criterion below is reworded around it.

## Figures

Ship window, 2026-10-03 (05:23-05:44 local, the second attempt), commit `531d66c`: the
code commit `424e3bd`, W5's group of 64 threads, plus documents only; clean tree,
Release, .NET 10.0.12, each formulation's own `N`, no override (budget 8192), `--repeats
3`, command `dotnet run -c Release --project tests/Benchmarks -- --formulation <F>
--paths reference,host1,host16,cpu,cuda,original --repeats 3`. Every figure: accepted
particles/s, cycle 1, mean ± sample spread, n = 3. Quiet checks before (05:31), between
(05:35) and after (05:44), and both attempts of the window, the first void →
`HISTORY.md#ship-window-2026-10-03`.

| Date | Formulation | Path | Accepted particles/s | Budget | Launches | Attempts | Machine | Build | Commit |
|---|---|---|---|---|---|---|---|---|---|
| 2026-10-03 | HPEPA3 | reference | 9134 ± 52 (n=3) | 1 | 1399729 | 1399729 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | host1 | 9219 ± 14 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | host16 | 48981 ± 759 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | cuda | 59727 ± 1480 (n=3) | 8192 | 2 | 1396212 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HPEPA3 | original | 7376 ± 250 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | reference | 366 ± 2 (n=3) | 1 | 6425301 | 6425301 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | host1 | 381 ± 1 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | host16 | 2085 ± 8 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | cuda | 7100 ± 6 (n=3) | 8192 | 2 | 6393938 | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |
| 2026-10-03 | HMX | original | 319 ± 5 (n=3) | n/a | n/a | n/a | AMD64 Family 25 Model 97 Stepping 2, AuthenticAMD, 16 logical CPUs, NVIDIA GeForce RTX 5070 Ti (libdevice linked), Microsoft Windows 10.0.26200 | Release | 531d66c |

`cpu` reads `not available` on both formulations (`## Implementation notes`,
"Escalation"). `Launches` and `Attempts` cover the whole call, cycle 0 included;
`Launches` is 2 on every batched row. A `reference` row's Budget is its effective one, 1
(`RunDiagnostics.AttemptsPerLaunch`). `original` is the whole run's throughput
(`## Implementation notes`, "Original's particles/s"). `host1` is pinned to one CPU and
reads at `reference`'s level (⚠ 2026-10-02, "Host-thread counts").

**R0 at the default: met, and CUDA now leads host16 on both.** HMX CUDA 7100 ± 6 against
0.8 × host16 2085 ± 8 = 1668 (×3.41 host16); HPEPA3 CUDA 59727 ± 1480 against host16
48981 ± 759 (×1.22), spreads apart; against the original ×22.3 and ×8.10. Ratios of one
invocation's means. The gain of the launch shape itself is the A/B/A's, in `### W5`.

The W2 table (`7e8f6f7`, 2026-10-02, before the launch shape), its notes and the stop
it fired, moved 2026-10-03 → `HISTORY.md#figures-w2-superseded-2026-10-03`.

⚠ 2026-10-03: was W2, HPEPA3 CUDA 43503 ± 88 under host16 47685 ± 178 and HMX CUDA
5095 ± 2, now the ship window, CUDA 59727 ± 1480 and 7100 ± 6 →
`HISTORY.md#figures-w2-superseded-2026-10-03`

⚠ 2026-10-02: was the Stage 0 table (`ce30f62`, budget 32; HPEPA3 CUDA 35315 ± 28, HMX
CUDA 397 ± 0) with R0 "collapse not gone", now W2 at budget 8192, R0 met →
`HISTORY.md#figures-w2-2026-10-02`, which holds the section as it stood with the three
earlier corrections these lines point to:

⚠ 2026-09-28: was the 2026-09-20 measurement (`7e703a9`) as the table, now superseded
by Stage 0 → HISTORY.md#figures-2026-09-20-superseded

⚠ 2026-10-02: was R0's "the collapse is not gone", unqualified, now true at budget 32
only (E-B: 5042 ± 29 against 0.8 × 2085 ± 5 = 1668 at 8192) →
HISTORY.md#eb-table-2026-09-28

⚠ 2026-09-28: was HPEPA3 CUDA's drop (47405 ± 249 to 35315 ± 28) "cause not measured"
and then a regression of the code, now the stream layout default →
HISTORY.md#benchmarks-figures-hpepa3-drop-2026-09-28

Cycle cost measurement (2026-09-20) → `HISTORY.md#cycle-cost-measurement-2026-09-20`:
on `inpt`, cycle 1 reads 5-6× below the plateau, an absolute (not per-particle)
warm-up cost, shrinking in relative weight as `N` grows; unchanged by this task.
## Post-acceptance plan (decided 2026-09-27)

Decision B of the arbiter's second 2026-09-27 review (owner's delegation): the recorded
figures are stale under this node's own taboo (twelve commits touched `Attempt.cs`,
`Engine.cs` or `Kernels.cs` since 7e703a9, the precision kind among them) and "the
kernel is bound by divergence and tails ... not arithmetic" was a hypothesis, never a
profile (root `BOOT.md`, "Performance", ⚠ 2026-09-27). What follows is the review's own
plan, verbatim, for the stages and rules:

- Run Stage 0 and Stage 1 in the first quiet window after A2's regeneration. Neither
  changes a source file.
- Run a Stage 2 experiment only when its rule below selects it. The budget sweep
  (E-B) is the likely first, as static reading predicts.
- No kernel restructuring without a design session.

The thresholds below are decision thresholds, fixed before any figure exists; they are
not tolerances.

- **Stage 0: re-measure.** Re-run the node's own command for HPEPA3 and HMX, all
  paths, 3 repeats, on a quiet machine. Each row also records launches and attempts
  from `RunDiagnostics`.
  - R0: if CUDA HMX is at least 0.8 × host16 outside the spreads, the collapse is
    gone. Record it and stop.
- **Stage 1: profile** one HPEPA3 and one HMX cycle 1 on CUDA. Use Nsight Systems for
  the timeline and Nsight Compute on the first launch and on one tail launch, if the
  installed toolkit has them. Otherwise use a throwaway, uncommitted per-launch
  stopwatch.
  - R1: if launch time stays within 2× while the active count falls 10×, or launches
    with under 10 % of N active take over 50 % of the cycle, run E-B first.
  - R2: if host gaps take over 20 % of the cycle, fix the host loop (download only
    active controls, reuse the index buffer). This is `Execution`-only and changes
    no bits.
  - R3: if the bulk launch shows warp efficiency under 25 % or under 8 warps per SM,
    the cause is structural. Stop experimenting; routing or restructuring goes to a
    design session.
  - R4: DRAM or L2 throughput over 60 % of peak selects E-L.
  - R5: `qks1` loads over 10 % of global load requests select E-Q.
  - R6: if atomic stalls dominate, per-attempt integer tallies go to a design
    session. Integer sums are exact, so bits would not change, but it touches every
    write site.
- **E-B: budget sweep.** Budgets {8, 32, 128, 512, 2048, 8192} × {HPEPA3, HMX} ×
  {cuda, host16}, 3 repeats, through a new `tests/Benchmarks` flag.
  - Adopt a new budget only if CUDA HMX improves at least 2× beyond both spreads and
    no HPEPA3 or host16 row regresses beyond its spread.
  - Before the default moves, all of these must hold:
    - `RunDiagnostics` records `AttemptsPerLaunch` (a public API change, with the
      surface snapshot);
    - link 3 is green at the new budget, CPU and CUDA, five formulations, eight
      seeds;
    - the tier table is re-measured at the new budget;
    - `src/Simulation/BOOT.md` "## Budget measurement" is re-based.
  - If CUDA HMX gains less than 1.5×, H1 is refuted as the dominant cost; go to
    R3/R4.
- **E-L: field-major records and scratch** (only if R4 selects it). Kernel path
  only; host threads keep padded particle-major rows.
  - Adopt at ≥1.25× with identical bits in `HostThreadMatrixTests`,
    `ReferenceModeEqualsContinuedBatchTests`, the `TierTableTests` figures, the
    seed-0 snapshot and the `ResultSha256`s.
  - It changes `Particle`'s API, so it escalates to the root before merge.
- **E-Q: `QKS1` in shared memory** (only if R5 selects it). Adopt at ≥1.1× with
  identical bits.

**Invariants.** E-L, E-Q and R2 change no arithmetic, so identical bits prove them.
E-B changes the `QKS1` refresh schedule, which the determinism key already covers
("budgets"), so it is judged by link 3, never by bits. No `double` atomic is added
anywhere. `Attempt.Run` stays one program; a stride parameter is not a second
implementation.

Stage 0's own figures are recorded in `## Figures` below. Running E-B/E-L/E-Q/R2's
host-loop fix themselves is out of this plan's own scope: a rule selecting one is a
verdict, not an experiment run.

### Stage 1: profile (2026-09-28)

Section moved 2026-10-03 → `HISTORY.md#stage1-section-2026-09-28`, the per-launch
figures → `HISTORY.md#stage1-profile-details-2026-09-28`. Decision: R1 (E-B first)
triggered, HMX cycle 1's duration falling 1.28× against active threads 10×; E-B ran.
R2 (host loop) not triggered, host API time under 1 % of the run (W3/W4 record below:
0.81 % and 2.26 % of the span). R3-R6 need counters `ncu` refused (`ERR_NVGPUCTRPERM`).

⚠ 2026-10-02: was "29.8-39.5 % (HMX)", now 29.8-34.4 % (strict; the verdict stands)
→ HISTORY.md#stage1-profile-details-2026-09-28

### E-B: budget sweep (2026-09-28)

Ran verbatim: budgets {8, 32, 128, 512, 2048, 8192} × {HPEPA3, HMX} × {host16, cuda},
3 repeats, through `--attempts-per-launch`, machine confirmed quiet before and after
every one of the twelve invocations (`nvidia-smi` 0-1 % / no compute process,
`Get-Counter` 4-13 %). Commit `2dc74ec`, branch `claude/perf-eb`; full table and
quiet-check log in `HISTORY.md#eb-budget-sweep-2026-09-28`.

Summary table (budgets × paths, accepted particles/s, cycle 1, mean ± sample spread,
n = 3, gain against budget 32) moved 2026-10-02 → `HISTORY.md#eb-table-2026-09-28`. The
figures that decided: HMX CUDA 397 ± 0 at budget 32 and 5042 ± 29 at 8192 (12.70×), HMX
host16 2085 ± 5 at 8192; HPEPA3 CUDA 43530 ± 162 and host16 45938 ± 6413 at 8192
(level).

**Verdict: adopt.** The rule holds at every tested budget from 128 up: HMX CUDA's low
end at 128 (883) already clears 2× the default row's high end (794) and rises through
8192, and no HPEPA3 or host16 row's low end falls below the default row's own. Budget 8
fails (HMX CUDA 0.47×), consistent with H1; the 1.5×-refutation branch does not apply.
The value among {128, 512, 2048, 8192} is `src/Simulation/BOOT.md`'s selection rule's
(`## Budget selection rule (2026-09-28)`); the verdict paragraph as it stood →
`HISTORY.md#eb-verdict-2026-09-28`.

⚠ 2026-10-02: was "not gone at the shipped default", now true only at budget 32, the
default when E-B ran; at the shipped default 8192 (adopted 2026-10-01) R0 is met, 5042
± 29 against 0.8 × 2085 ± 5 = 1668 → HISTORY.md#eb-table-2026-09-28

**E-B adopted 2026-10-01**: the default is 8192. The preconditions were met in that
order: `RunDiagnostics.AttemptsPerLaunch` (2026-09-28), link 3 over the ladder
(2026-10-01, all green), the tier table at 8192 (2026-10-01), the re-base (2026-10-01).
Moved 2026-10-03, their 2026-09-28 listing → HISTORY.md#eb-preconditions-2026-09-28

### HPEPA3 CUDA regression bisection (2026-09-28)

Section moved 2026-10-02 → `HISTORY.md#bisection-section-2026-09-28`, the log →
`HISTORY.md#hpepa3-cuda-bisect-2026-09-28`, this body 2026-10-03 →
`HISTORY.md#bisection-pointer-2026-09-28`. Decision: the step is `d735f3e`, the default
`SimulationOptions.Streams` `Original` → `Independent`; deciding figure: `51e66c9`
with `Independent` forced read 35263 ± 118 (n=3), against 45322 ± 276 unforced.

⚠ 2026-10-02: was "the derivation costs measurably more", now refuted
(`Kernel_DeriveStreams` 0.13-0.14 ms per cycle against a step of 0.63 s), mechanism not
measured → HISTORY.md#bisection-section-2026-09-28

### Pre-registered before any new figure (2026-10-02)

Fixed from the performance review's work list (W2, W4) before any figure exists;
decision thresholds, not tolerances. The owner granted no Nsight Compute counters on
2026-10-02: no rule below needs them, and where one would, it stops.

- **W2, Stage 0'** (re-measure at the shipped default): ran 2026-10-02, recorded in
  `## Figures`; the rule as it stood, moved 2026-10-03 → HISTORY.md#w2-rule-2026-10-02

  ⚠ 2026-10-02: was E-B's n = 3 spreads as the stop's bands and its discriminator; W2
  fired it on HMX (CUDA 5095 ± 2, host16 2071 ± 18) and an A/B/A (`## Figures`)
  showed the bands narrower than the variation between invocations on one machine: a
  false alarm of the rule, not a change of the code. The rule stands as registered,
  not loosened. Proposed, not adopted: a re-measurement runs an A/B/A against the
  previous figure's commit as the stop's discriminator instead of a fixed band
  → `HISTORY.md#figures-w2-2026-10-02`
- **W4, HMX discriminator.** D is the duration of HMX cycle 1's bulk
  `Kernel_RunAttempts` launch at budget 8192; p* is the particle with the most attempts
  in HMX seed 0 cycle 1 (host threads, outside any timing window) and A* its
  AttemptCount; T* is the duration of that same launch rerun with only p* active, under
  the same profiler. Control: p*'s AttemptCount on CUDA equals A*, otherwise T* is void.
  Then D/T*:
  - ≤ 1.25: the cycle is its slowest chain; no occupancy, layout or group-size change
    gains more than D/T* − 1 on HMX;
  - ≥ 2: contention; group size and registers are the levers, each candidate
    pre-registered in a design session;
  - between: not decided here. Record D, T* and A*, and stop for a design session; the
    counter figures that would decide it are not granted.

### W3/W4 record (2026-10-02)

Run by the orchestrator, 23:45-23:46, quiet checks before, between and after, Nsight
Systems 2026.3.2, one `cuda` call per formulation at budget 8192, commit `d499816`; the
timed code is identical at `09664f9` (no `.cs`, project or build file of `src` differs,
`git diff --name-only d499816 09664f9`). The reader, its proofs and the hook patches
are a working note, not in the tree; their SHA-256,
the quiet checks, tables and outputs → `HISTORY.md#w34-2026-10-02`. The W4b hook's
control line went to a file, not the console, so the reader call failed; it was re-run
on the same traces with `--control-log`.

- **W3.** `Kernel_RunAttempts`: 196 registers per thread, block 256, grid 391 (HPEPA3)
  and 40 (HMX), one launch per cycle (so R1's per-launch clauses read nothing); cycle 1
  2235.3 ms and 1899.2 ms. **R2** (GPU idle between kernels, share of the cycle's kernel
  span, cycle 1): 0.81 % and 2.26 %, not triggered; Stage 1 had judged it from host API
  time, a different quantity. Cycle 0 reads 14.84 % and 24.82 %: the cold process
  compiles the kernel at first use, not the host loop's figure. `Kernel_DeriveStreams`:
  0.136 and 0.029 ms per launch.
- **W4, the rule as registered.** Control passed: p* = 2654, A* = 3926 on host threads;
  on CUDA the bulk launch's count at p* is 3926, the three reruns 3926 each, CUDA's own
  argmax is p*. D = 1900.4 ms (hook run), 1899.2 ms (clean W3 run); T* = 585.6, 585.3,
  586.5 ms (n = 3, median 585.6). **D/T* = 3.245**: the band "≥ 2: contention". W4a
  (host threads, no timing): A* is 9.99 times the mean of 393.2 attempts; attempt-level
  SIMT efficiency, equal cost per attempt assumed, at most 0.2454 (32-lane warp) and
  0.1592 (256-lane block). R3 arithmetic: 196 registers give one 8-warp block per SM;
  HMX's 40 blocks meet the review's 70 SMs.

**Decided:** the ≥ 2 branch. The HMX cycle is not its slowest chain (the p* chain alone
is 585.6 of 1899.2 ms, 31 %); group size (E-G) and the register cap are the levers, each
candidate, rule and stop fixed in the W5 design session before any figure. **Not
decided:** which lever gains, or by how much (D/T* places a band, it bounds no gain).
R3's counter clause is not read: warp execution efficiency is an instruction-level
counter, the attempt-level figure (under 25 %) counts attempts at equal cost, no verdict
rests on it; 8 warps per SM is not "under 8". R4, R5, R6 stay unmeasured, no counters
granted. No kernel, default or tolerance changes.

### W5: launch shape (2026-10-03)

Pre-registered by the W5 design session before any figure; decision thresholds, not
tolerances. The pre-registration as it stood moved 2026-10-03 →
`HISTORY.md#w5-prereg-2026-10-03`. Window C, the counted run: 2026-10-03 03:05-03:18
local (the rows read 2026-10-03 too: UTC 00:05-00:18), the hook build of `6df81ae`
(rows read `6df81ae-dirty`), `Kernels.RunAttempts` on CUDA at budget 8192, `--paths
cuda --repeats 3`, four rounds of A, the other arms rotated, A again. Arms: A (ILGPU's
automatic group, 256); G128, G64, G32 (explicit group, same PTX); R (G128, at most 168
registers, 3 groups per SM); controls P (A at budget 2048) and Q (A at `--particles
9999`). The first attempt (01:49-01:53 local; its rows read 2026-10-02, the tool dates
in UTC) stopped in round 1 and is seen, not counted. The tables, the SHA-256 of the
scripts, patches and run logs → `HISTORY.md#w5-record-2026-10-03`.

⚠ 2026-10-03: was P1 read from the `nsys` console, now from the P2 logs; no threshold
moves → HISTORY.md#w5-stop-2026-10-03

⚠ 2026-10-03: was the stop "P's Attempts equal to A's", now Q's clause; the old stop
could never pass → HISTORY.md#w5-stop-2026-10-03

- **Decided: adopt G64**, the rule's verdict: "adopt G64 (geometric mean of the two
  medians 1.363; qualified G128 1.259, G64 1.363, G32 1.358; within 0.02 of the top:
  G64, G32; the larger group wins)". Medians over four rounds of g, cycle-1 particles
  per second over the round's A mean: HMX G128 1.161, G64 1.359, G32 1.348, R 1.161;
  HPEPA3 1.366, 1.366, 1.368, 1.360. No round void (ν_i 0.00-0.06 %), no stop; A read
  HMX 5064-5070 and HPEPA3 43666-43685. **R ÷ G128 = 1.000** on both formulations
  (medians): the register cap gains nothing, R does not qualify.
- **Controls.** P's median g 0.775 (band 0.74-0.81). Q's Attempts 6393346 equal the
  host-thread figure and differ from A's 6393938, so the Attempts control can go red
  and did not. P1: every arm ok, block = G, grid 40, 79, 157, 313 for A, G128, G64,
  G32, 196 registers and no local memory, groups per SM 1, 2, 4, 8; R 168 registers, 3.
- **Predicted against read.** HMX: G128 1.06 (1.0-1.8 by dispatch) read 1.161; G64
  1.34 read 1.359; G32 1.43 read 1.348, so G64 above G32; R ≤ G128 met (R ÷ G128
  1.000). HPEPA3: 1.00-1.05 for every G, read 1.366, 1.366, 1.368: the design's own
  words, "≥ 1.10 refutes (ii)", apply, to the prediction for HPEPA3; R ÷ G128 0.90-1.02
  read 1.000. P0 (HMX, A): D/T* 3.240 (W4: 3.245); T_w/T* 1.234 (≤ 1.3, inside);
  T_b/T_w 2.113 (2.5-3.2) and D/T_b 1.243 (≤ 1.1) outside. Findings for the next
  design session, which owns the mechanism; nothing is adopted on P0.
- **Shipped 2026-10-03** (`424e3bd`, G64, `src/Execution/BOOT.md`, "Launch shape"),
  after text A's checks, run in the ship window (tree `531d66c`): the tier table
  reproduced digit for digit; the seed-0 CUDA `results.m` hashes of the five
  formulations equal across parent, commit, parent; the A/B/A g ≥ 1.10 again. Two
  attempts, both recorded: the first void (HMX ν_i 19.7 and 21.0 %, cause unknown), the
  second g 1.357, 1.356 (HMX) and 1.342, 1.308 (HPEPA3), ν_i ≤ 0.10 %. `## Figures` is
  its step (d) → `HISTORY.md#ship-window-2026-10-03`

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Taboos

- No assertion on a figure, in this node or any other.
- No figure from a Debug build, and none without date, machine and commit.
- No figure carried over from an earlier commit after a change to `Particle`,
  `Random`, `Execution`, `Statistics` or `Simulation`: re-measure or delete the row.

  ⚠ 2026-10-02: was `Particle`, `Random` or `Execution` only, now also `Statistics` and
  `Simulation`: `CycleStatistics.Compute` runs inside the timed window, at the end of
  cycle 0 after its last progress report and again at the end of cycle 1 (the cycle
  loop of `Simulator.Run`), so a host-side change there moves a figure. Commit
  `874d81c` was one, and the old wording did not see it.
- No benchmark that bypasses `Simulation` to make a number look better.
