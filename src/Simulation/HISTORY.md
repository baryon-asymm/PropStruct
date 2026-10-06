# HISTORY.md — Simulation

Append-only store of what `BOOT.md` no longer needs to state as current truth
(`AGENTS.md`, §15). Newest entry first. Every entry names the date it was moved, the
`BOOT.md` section it came from, and carries the original text in full, unedited. The
start procedure (`AGENTS.md`, §10) does not read this file; it is reached only by
following a dated pointer left in `BOOT.md` at the place the text used to stand.

---

<a id="attempts-per-launch-99-percent-rule"></a>

## 2026-10-01 — from "## Budget measurement (2026-09-19)" — the 99 % rule for attempts per launch

Moved when the default moved from 32 to 8192 by the 2026-09-28 selection rule. The
paragraph as it stood, and the table row `| AttemptsPerLaunch | 32 | 99.457 % of HPEPA3's
particles within 32 attempts |`:

Attempts per launch, from HPEPA3's reference-mode distribution: 15.141 % of particles
finish within 1 attempt, 47.772 % within 4, 72.692 % within 8, 92.483 % within 16,
99.457 % within 32, 99.996 % within 64. The smallest power of two reaching 99 % is 32.
Reference mode stands in for batched mode's first launch because the attempt program is
the same and only the QKS1 refresh differs; the batched maximum brackets the reference
one ((80, 96] against 90). For HMX, with a mean of 326 attempts, 32 attempts finish
10.955 % of particles in the first launch: the rule is HPEPA3's by decision, and HMX runs
relaunch accordingly.

---

<a id="precisionkind-double-ca1720-exception-lifted"></a>

## 2026-09-28 — from "## Constraints" — the PrecisionKind.Double CA1720 exception, lifted, moved to make room for the budget selection rule

Moved to make room under the §15 line limit for the new "## Budget selection rule
(2026-09-28)" section. The current state — `PrecisionKind.Binary64` is the name
everywhere, the exception closed — is unchanged and is now only the one-line pointer
left in "## Constraints"; this is the full text of the bullet and its own ⚠ that stood
there.

> **`PrecisionKind.Double` was a declared exception to CA1720, now lifted.** Found
> 2026-09-24, the `AnalysisMode=All` pass (root `BOOT.md`, "Language and build"),
> and reported rather than renamed because the fix crosses `Particle`, `Statistics`,
> `Execution`, `Cli`, `Output` and `tests/Simulation.Tests/StatisticalCriterionTests.cs`,
> none of them this node's to change in an ordinary coding-mode task.
>
> ⚠ 2026-09-24, later the same day: lifted. The owner opened a task in coding mode
> across the whole tree for exactly this rename (root BOOT.md's own decision, "Language
> and build"); `PrecisionKind.Double` is now `PrecisionKind.Binary64` everywhere,
> including `StatisticalCriterionTests.cs`, per the rename map
> `tests/precision-kind-rename-2026-09-24.txt`.

---

<a id="counter-check-nbase-denominator-correction"></a>

## 2026-09-28 — from "## Counter check (2026-09-19)" — the Nbase denominator defect, moved to make room for the budget selection rule

Moved to make room under the §15 line limit. The current state — the nine conditions
divided by the original's own denominators (`FI + N` or `FI`), the count bound above
replacing the absolute floor, both proven red by mutation — is unchanged and stated in
full above this pointer; this is the already-closed note of what the section first said.

> ⚠ 2026-09-19: the first version of this section (2026-09-18) divided the nine
> conditions by `Nbase` (`FI + N`) throughout and claimed they "all divide cleanly by
> `Nbase`, confirmed by the same run's own agreement". That was false for conditions
> 6–9: the port printed 0.006155, 0.000195 and 0.001105 against the reference's 0.01211,
> 0.00041 and 0.00213, half of each, and the check passed only because it compared rates
> of order 0.01 against an absolute floor of 1.0 that no defect could exceed. Found by
> the coordinator's review of the salvaged session; the denominators are now the
> original's, the floor is replaced by the count bound above, and both are proven red by
> mutation. The bridges-per-particle denominator (`FI`) was already right.

---

<a id="budget-defaults-previous-session-values"></a>

## 2026-09-28 — from "## Budget measurement (2026-09-19)" — the previous session's unrecorded defaults, moved to make room for the budget selection rule

Moved to make room under the §15 line limit. The current state — every default
re-measured in "## Budget measurement" above, the attempt cap unchanged and the other
three changed — is unchanged and stated in full above this pointer; this is the
already-closed note of what the section first said.

> ⚠ 2026-09-19: the previous session set the defaults to 128, 500,000 and 30,000,000
> with source comments saying "measured 2026-09-18", while this section still held a
> placeholder and no figures had been recorded. `AttemptsPerLaunch = 128` followed a
> different rule ("every batch finishes in one launch", 100 % rather than the design's
> 99 %); the other two were powers-of-two brackets never written down. All four are
> re-measured above; the attempt cap keeps its value, the other three change.

---

<a id="degenerate-criterion-fixture-layout-moved"></a>

## 2026-09-28 — from "## Acceptance criteria" — the degenerate criterion's fixture layout note, moved to make room for the budget selection rule

Moved to make room under the §15 line limit. The current state — the fixture proven
under `Streams = StreamLayout.Independent`, the claim itself unchanged — is unchanged
and is now only the one-line pointer left in "## Acceptance criteria"; this is the full
text of the note that stood there.

> ⚠ 2026-09-21: the fixture underneath moved from `Streams = StreamLayout.Original` to
> `Streams = StreamLayout.Independent`, since the degenerate `ContinuedStreams`
> configuration is now `Batched` mode and the `Original` layout refuses every route
> to `Batched` mode (above); the claim proven is unchanged (batched-degenerate equals
> reference, bit for bit), only which layout it is proven under, matching the
> narrowing already recorded in "## Invariants".

---

<a id="budget-measurement-early-throughput-note"></a>

## 2026-09-28 — from "## Budget measurement (2026-09-19)" — an early, single HPEPA3 host-thread timing, moved to make room for the budget selection rule

Moved to make room under the §15 line limit. Superseded in substance by
`tests/Benchmarks/BOOT.md`'s own measured, provenanced "## Figures" (this node's own
budget defaults are unaffected either way); this is the full text of the note that
stood here.

> Observed along the way, recorded for `tests/Benchmarks` and not asserted: a whole
> batched HPEPA3 run on the CPU accelerator (16 threads, `AttemptsPerLaunch = 128`, the
> previous default) took 44–54 s, against 95 s single-threaded in reference mode. Why
> the gain is so small is not established here; one candidate is the neighbour-draw
> tail (about 2,000 times HPEPA3's mean of 88 per attempt), which a launch has to wait
> for.

---

<a id="pocket-redraw-budget-measurement-correction"></a>

## 2026-09-26 — from "## Result field mapping" — the PocketRedrawBudget measurement correction, moved to make room for the R1 correction

Moved to make room under the §15 line limit. The current state — 12,800, measured,
`SimulationOptions.PocketRedrawBudget` an internal option — is unchanged and stated in
full above this pointer; this is the already-closed note of what the item first said.

> ⚠ 2026-09-19: this item first said `SimulationOptions` had no such field and that the
> internal constant, 10,000, was "never observed to bind" and needed no measurement.
> The design decision "Budget defaults are measured, not chosen" names pocket redraws per
> bridge explicitly; measured, HPEPA3 needs up to (96, 128] redraws, so 10,000 was below
> the required 100-fold margin. The value is now 12,800 and the budget an internal
> option, so the measurement could vary it.

---

<a id="degenerate-configuration-narrowing"></a>

## 2026-09-26 — from "## Invariants" — the degenerate-configuration claim's two narrowings, moved to make room for the R1 correction

Moved to make room under the §15 line limit for the architecture-audit finding R1
correction to this same section (the batched-execution refusals moving to
`SimulationOptionsValidator`). The claim itself — `Batched` with batch size 1, attempts
per launch 1 and continued streams equals `Reference` bit for bit, under `Binary64`
accumulation and `Independent` streams — is unchanged and stated in full above this
pointer; these are the two already-settled corrections that narrowed it there.

> ⚠ 2026-09-21: was this claim unconditional; `Original` accumulation is now refused for
> batched execution altogether (below), so the claim has only a `Double` case left to be
> about (root BOOT.md, "Accumulation kind is an option of every run": "'Reference mode
> is batched mode degenerated' is a claim about `Double` accumulation").
>
> ⚠ 2026-09-21, later the same day: the claim needs a second, independent narrowing:
> `Original` stream layout is now also refused for batched execution altogether (below),
> so of the two restrictions this claim needs `Double` accumulation *and* `Independent`
> stream layout — `DegenerateConfigurationTests` moved to the `Independent` layout to
> keep proving it (root BOOT.md, "Two stream layouts": "The `Original` layout is
> sequential only").
