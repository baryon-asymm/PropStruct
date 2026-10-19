# HISTORY.md — Simulation.Tests

Append-only. Newest first. Each entry carries the date, the section of `BOOT.md` it
came from, and the original text in full. Read only by following a dated pointer from
`BOOT.md`; the start procedure (AGENTS.md §10) does not read this file.

<a id="guard-provenance-ac1fc20-2026-10-19"></a>
## 2026-10-19 — `BOOT.md`, "## The Original-model guard", the lines generated at `ac1fc20`

Three paragraphs of the section as they stood before U6 regenerated two lines of the file at the head. The second and third say that a line was generated from `src/` at `ac1fc20`, which stays true of the lines they name and stopped being true of the file as a whole; the first said that `GdokleftOnce` cannot move a bit in `extra/PSAN02-sfr-dmin20` and that two flags rest on one case each, which held under the double quotient of the Dmin cell index and not after the exact one (`ACCEPTANCE.md`, the per-flag tables); moved here on 2026-10-19 (U6, §15), the pointer left in `BOOT.md`.

```text
**Fast set and `Long`** (U3, 2026-10-18, `GuardCase.IsLong`): the guard was wholly
`Category=Long`, so CI never ran it. The `batched/*` and `extra/*` cases run in the fast
set, 12 cases, 32 s in Debug together with the Corrected snapshot's; `seed0/*` and the
three P312 extras that took 44 to 94 s in Debug (`-defaults`, `-cycles3`,
`-homogenized0.1`) stay `Long`, 23 cases. The 12 still see every flag of `CorrectionSet`
(`ACCEPTANCE.md`, the fast-subset criterion); the two flags that rest on one case each
have it in the fast set: `extra/P312-dmin5` (`PdoksmallFirstCell`, 0.07 s) and
`extra/PSAN02-sfr-dmin20-di10` (`GdokleftOnce`).

`PSAN02-sfr-dmin20-di10` (2026-10-18, T13): `PSAN02-sfr-dmin20` keeps the default `Di`, and
`1.9999999999999998e-05 / 1e-05` truncates to 1, so its `b` is 10 µm, `φ(b)` is 0 and
`GdokleftOnce` cannot move a bit there (the per-flag table in `ACCEPTANCE.md` shows no case
saw it); with `Di` 10 µm `b` is 20 µm. Its line was generated with the product code of the
original port: `src/` checked out at `ac1fc20` (the tree removed and checked out again from
that commit), the generator built and run in Release, `src/` restored from `HEAD`. The
regenerated file differs from the committed one by that one added line, and the 34 cases
pass at the head, `Original` unchanged.

`extra/PSAN02-sfr-dmin20-cycles3/Original` (2026-10-18, U3) was generated the same way:
the tree removed and `src/` checked out at `ac1fc20`, `tests/RateTableTool` as at the
base of U3 but for the case (`Program.cs` without the `corrected-model-snapshot`
subcommand and `CorrectedModelGuard.cs` set aside, since a `Corrected` model does not
exist at `ac1fc20`), `original-model-guard` run in Release (35 cases, 6 m 45 s), `src/`
and the tool restored from `HEAD`. The file differs from the committed one by that one
line; the 34 older lines came out byte-identical, the old model's product code
reproducing them. The case is green at the head.
```

<a id="corrected-snapshot-empty-rows-2026-10-19"></a>
## 2026-10-19 — `BOOT.md`, "## The Corrected-model snapshot", the two batched cases

The text before the flag `CategoriesEmptyRowsMerged` (1024) was withdrawn (U5); moved here on 2026-10-19 (U6, §15), the pointer left in `BOOT.md`.

```text
`extra/P2-defaults`) and `batched/inpt`, `batched/PSAN02n`, which show
`CategoriesEmptyRowsMerged`; each is the Original guard's case run under
`ModelKind.Corrected` with no seam, ids prefixed `corrected/`. `P312-homogenized0.1` is
left out: the other P312 cases cover it. The two P312 cases that take 47 and 102 s in
Debug are `Category=Long`; the other eight run in the fast set (about 10 s).
`Snapshots/CorrectedModelSnapshot.approved.txt` is written by `dotnet run --project
tests/RateTableTool -- corrected-model-snapshot` at the head (Release, 10 cases, 52 s),
never typed; green in Debug. It asserts that the `Corrected` results are those approved,
not that they are right: a regeneration is the owner's decision, in the commit that
changes the model. Its per-flag proof is in `ACCEPTANCE.md`: dropping any of the eleven
flags from the mapping turns at least one fast case red.
```

<a id="budget-ladder-paired-shift"></a>
## 2026-10-19 — `BOOT.md`, "## Budget ladder (2026-10-01)", the paired shift

The paragraph written 2026-10-01 (the decision, 8192, and its figure "all 50 rows green" stay); moved here on 2026-10-19 (U6, §15), the pointer left in `BOOT.md`.

```text
Paired shift, 8192 against 32, host threads, eight seeds (a size, not a verdict): cells
that differ at all — HMX 107, inpt 52, PSAN02n 39, HPEPA3 18, P33 0; the largest mean
paired shift is 0.28 of the 32-row's seed sd (PSAN02n `Dqmkm2[0]`; HMX 0.21, HPEPA3 0.14,
inpt 0.11). Link 3 itself resolves only shifts of about 4.6–5 seed sd (budget decision,
F3), so this record, not the GREEN rows, bounds the staleness of the coarser QKS1
refresh. The null-budget red of the echo assertion is the test author's record, not re-run.
The ladder theory was deleted when the default moved (C5); it stays reproducible at
3ea6471.
```

<a id="statistical-criterion-row-date-2026-10-02"></a>
## 2026-10-19 — `BOOT.md`, "## Purpose", the date of the statistical-criterion row

The ⚠ correction written 2026-10-02; moved here on 2026-10-19 (U6, §15), the pointer left in `BOOT.md`.

```text
⚠ 2026-10-02: the statistical-criterion row was dated 2026-09-24, the day its text was
realigned. The evidence it names was obtained later: the seed-0 snapshot was
re-approved and link 3 re-run at the attempt budget 8192 on 2026-10-01 (874d81c, 82f1de2,
3ea6471; `git log` of `StatisticalCriterionTests.cs` and the snapshot ends at 874d81c),
the date the root's batched criterion carries for the same evidence
(`ACCEPTANCE.md`, "Batched mode"; the rule: a date is the day the evidence now in the
tree was obtained after the last change to what it reads, AGENTS.md §6). Found by the
2026-10-02 arbitration's D4 reading. Not re-run for this correction (`Category=Long`):
3630af3 (2026-10-02) changed the failure path of `Simulator` only, no byte of a
completed run, and the figure "40 cases" is as recorded 2026-09-24.
```

<a id="table-marks-redated-2026-09-19"></a>
## 2026-10-19 — `BOOT.md`, "## Purpose", the marks of the level table

The ⚠ correction written 2026-09-19; moved here on 2026-10-19 (U6, §15), the pointer left in `BOOT.md`.

```text
⚠ 2026-09-19: the ✅ marks of this table were first dated 2026-09-18 by a session that
ended at a usage limit, and two of them did not hold when this session started. The
completeness row was red: `Ggg` had been removed from `RunHeader` for a mutation and
never restored. The counter row passed vacuously for conditions 6–9 (wrong denominator,
absolute floor of 1.0; `src/Simulation/BOOT.md`, "## Counter check"). Every row was
re-proven on 2026-09-19 by the mutations below, each seen red and reverted.
```

<a id="mutation-row-renamed-2026-09-23"></a>
## 2026-10-19 — `BOOT.md`, "## Mutations", the renamed fact of the Independent-layout row

The ⚠ note written 2026-09-23; moved here on 2026-10-19 (U6, §15), the pointer left in `BOOT.md`.

```text
⚠ 2026-09-23: the row above named `ReferenceMode_IndependentLayout_SatisfiesCriterion_AgainstIndependentReplicas`,
which the same day's realignment renamed to
`ReferenceMode_IndependentLayout_OriginalPrecision_MatchesIndependentReplicas_ExceptKnownCells` and changed from
`PrecisionKind.Binary64` to `PrecisionKind.Original`; the row is kept as the historical record of the proof it was
(swapping `ReplicaKind` breaks the comparison) rather than rewritten, since a mutation record is provenance, not
current truth (AGENTS.md §8).
```

<a id="correction-effects-floor-first-run-2026-10-18"></a>
## 2026-10-18 — `BOOT.md`, "## Correction-effects measurement", the floor of the first run

The bullet read as follows before the re-run at 8b29342 (U4); the first run took its
sixteen seeds as `k << 17`, which are eight distinct phases, so its even and odd halves
held copied seeds, which is a reading of why the floor was not clean, not a tested one.

```text
- **Floor** (measured 2026-10-18): the negative control is not clean. `TwoSampleBiasOfSets`
  has no count floor or print resolution, so cells one half prints and the other does not, and
  cells printed more coarsely than their spread, differ with infinite or huge t: 0 to 9 cells in
  1200 to 7000, listed in `control-negative-*.md`. The fact asserts the floor under one per cent.
```

<a id="correction-effects-reticked-u4-2026-10-18"></a>
## 2026-10-18 — `ACCEPTANCE.md`, the correction-effects criterion before its re-tick (U4)

The criterion as it stood unticked (the first run's tick is in
`HISTORY.md#correction-effects-seeds-repeat-2026-10-18`); its figures were the first
run's (14 arms, 1568 runs, about 95 minutes, the negative control not clean):

```text
- [ ] 2026-10-18: the effect of each correction is measured on the five references (16
      seeds) and four extra cases (8 seeds), per arm and printed quantity
      (`CorrectionEffectsMeasurement`, `Measurements/corrections-2026-10-18.md`; `BOOT.md`,
      "## Correction-effects measurement"; 9 cases, 14 arms, 1568 runs, none failed, about 95
      minutes of work on the CPU). Positive controls pass: `WindowLowerEdge` alone in reference
      mode lies inside the 2026-09-24 audit's range on all five references (`Dqmkm2` -2.82 % on
      inpt to -6.95 % on HPEPA3, `Zkarm` +4.91 % on HMX to +13.51 % on PSAN02n, the standard
      errors 0.01 to 1.7 points; the range's two ends are reproduced to the printed digit);
      `DoksdConsistent` gives P33 112.66 in all 16 runs, `Original` 75.45; `ExactConstants` scales
      `Lambda` by 3.14 / π to 2.2e-16 on all five. The negative control (`Original` even against
      odd seeds) is not clean: 0 to 9 differing cells of 1200 to 7000, all in cells
      `TwoSampleBiasOfSets` cannot judge (one half prints, the other not; print resolution),
      under the asserted one per cent (`BOOT.md`, "Floor").
```

<a id="link-3-measurement-tables-2026-09-23"></a>
## 2026-10-18 — Statistical criterion measurement (2026-09-24), link 3's measurement tables of 2026-09-23

Moved out of `BOOT.md` to keep it inside its limit (AGENTS.md §15); the result (zero
differing cells on all five formulations, on both accelerators) stays there. Original
text:

Measured on this branch merged onto `claude/wave7` at `a621e09`:

| Formulation | Cells compared | Differ | Reference (parallel) | CPU batched (sequential) | CPU total |
|---|---|---|---|---|---|
| HPEPA3 | 2071 | 0 | 31.3 s | 33.2 s | 1 m 4 s |
| inpt | 2634 | 0 | 2.7 s | 3.8 s | 6.5 s |
| P33 | 1509 | 0 | 3.1 s | 3.2 s | 6.3 s |
| PSAN02n | 2151 | 0 | 11.5 s | 10.0 s | 21.4 s |
| HMX | 6635 | 0 | 55.3 s | 81.6 s | 2 m 17 s |

CUDA row added 2026-09-23, later the same day, reusing the reference sets above (the
"Reference" column reads "reused" rather than a second measurement):

| Formulation | Cells compared | Differ | Reference | CUDA batched (sequential) |
|---|---|---|---|---|
| HPEPA3 | 2071 | 0 | reused, 24.3 s | 53.4 s |
| inpt | 2634 | 0 | reused, 1.9 s | 19.9 s |
| P33 | 1509 | 0 | reused, 2.1 s | 15.1 s |
| PSAN02n | 2151 | 0 | reused, 8.9 s | 16.8 s |
| HMX | 6635 | 0 | reused, 50.2 s | 5 m 32 s |

Cell counts match the CPU table exactly, as they must: both rows compare the same
eight reference sets against a batched set of the same shape, only the accelerator
differs. HMX's CUDA batched time is the one figure worth a reader's attention: 5 m 32 s
against the CPU row's 1 m 22 s (its own eight-seed sum from the table above), the same
"CUDA collapses on HMX" tier root `ACCEPTANCE.md`'s benchmark criterion already measured
(394 against 1954 accepted particles/s, cycle 1) — this is that collapse's own
consequence on a wall clock, not a new finding; the constraint's decision (no kernel
restructuring before acceptance) is root's, not this node's, to revisit.

No cell of any formulation needed a ratchet, on either accelerator: the
category-indexed families (`fqdokkarm(<row>,:)`, `Dkarmcat`, `dokkarm43`, `dokkarm10`,
`coef`) `Compare`'s own canonical-axis rule exists for (`tests/Harness/BOOT.md`,
"Canonical category axis") produced no spurious difference here either —
`TwoSampleBiasOfSets` has no such handling of its own (`tests/Harness/BOOT.md`,
"Two-sample bias"), but a physical row a given seed's run never produced is simply a
member `ValuesAt` (`tests/Harness`'s own) drops from that cell's sample, not a
mismatch it compares, and with sixteen runs per formulation split across two large
seed strides the coincidence needed to populate a row on one side and not the other,
thinly enough to trip a Welch test by chance at `α = 10⁻³` family-wise, did not occur
on any of the five formulations measured, on either accelerator.

<a id="link-3-proposal-to-root"></a>
## 2026-10-18 — Statistical criterion measurement (2026-09-24), the proposal to root about link 3

Moved out of `BOOT.md` to keep it inside its limit (AGENTS.md §15); it was a proposal to
root, not current truth of this node. Original text:

**What root BOOT.md should record, not written there by this task** (AGENTS.md §11:
both affected acceptance criteria are root's own): the "Reference mode in each layout
satisfies the criterion..." and "The `Original` accumulation kind reproduces..."
criteria cite figures measured under a mix of `Binary64`/original-replica and
`Original`/original-replica comparisons across several sessions; this session's own
link-1 numbers (above) reproduce root's own 2026-09-23 re-run exactly (same three
known-open cells, same compared/excluded counts on every row checked), so they
corroborate rather than change root's own standing text.

Link 3 is new evidence for a criterion root BOOT.md has not yet drafted words for:
"batched and CUDA against sequential `Binary64`, statistically, on the port's own
replicas" is named as a link but carried no measured row in root's own acceptance
criteria the way link 1 and link 2 do. The CPU half of that gap was filled earlier
2026-09-23 (above); the CUDA half is filled the same day, later: zero differing cells
on all five formulations on `AcceleratorKind.Cuda` too, the "## Link 3 measurement"
CUDA table above. Root BOOT.md's own acceptance criterion "Batched mode (whole-cycle
batches, CPU accelerator and CUDA) satisfies the same criterion..." currently reads,
of the `Independent` layout: "It stays unticked until CUDA is run the same way." That
condition is now met — this node offers both the CPU and CUDA rows as its evidence —
and root's own criterion is the owner's to tick, not this node's (AGENTS.md §11: the
criterion is root's own text). `tests/Execution.Tests`' own tier tables remain the
place a CUDA-vs-CPU claim for the kernel path itself belongs (this node's own
`## Dependencies`); this node's own claim is narrower — CUDA batched mode agrees with
the port's own sequential reference mode, statistically, the same claim the CPU row
makes, not a claim that CUDA agrees with the CPU accelerator cell for cell.

<a id="twosamplebiasofsets-note-2026-09-23"></a>
## 2026-10-18 — Statistical criterion measurement (2026-09-24), the `TwoSampleBiasOfSets` note of 2026-09-23

Moved out of `BOOT.md` (AGENTS.md §15). Original text:

⚠ 2026-09-23: `TwoSampleBiasOfSets` itself was found, the same evening these rows were
first measured, to false-positive on a bit-identical constant cell compared across two
sets of *different* sizes (`tests/Harness/HISTORY.md#twosamplebiasofsets-constant-cell-false-positive`;
fixed the same day). It does not touch the rows above: every link-3 comparison here
pairs two equal-size sets (`R = 8` against `R = 8`), and the defect's mechanism —
`Array.Average()` of `n` identical values rounding differently for different `n` —
cannot produce a nonzero gap between two means computed over the *same* `n` from the
*same* bit-identical value. Not re-measured, since nothing that could change it changed.

<a id="original-guard-criterion-33-cases"></a>
## 2026-10-18 — `ACCEPTANCE.md`, the Original-model guard criterion before U3

Original wording of the first sentence of the criterion (`BOOT.md`, "## The
Original-model guard"): "the `Original` model is guarded by hashes of the full result
(`OriginalModelGuardTests`): 33 cases, green in Debug against a Release-written file."
The whole guard carried `Category=Long`, so the fast set and CI ran one fact of it, the
one that checks that the approved file holds the guard's cases. U3 split the cases: the
seed-0 ones and the three slow P312 extras stay `Long`, the others run in the fast set.

<a id="model-kind-tests-case-count"></a>
## 2026-10-18 — `ACCEPTANCE.md`, the `ModelKindTests` count

The criterion read "`ModelKindTests` (18 cases)". The file held 23 test cases (counting
each row of a theory) before U3 added the mapping fact, 24 after.

<a id="correction-effects-seeds-repeat-2026-10-18"></a>
## 2026-10-18 — `ACCEPTANCE.md`, the correction-effects measurement ticked on repeating seeds

Original tick, `- [x] 2026-10-18: the effect of each correction is measured on the five
references (16 seeds) and four extra cases (8 seeds), per arm and printed quantity
(CorrectionEffectsMeasurement, Measurements/corrections-2026-10-18.md; BOOT.md, "##
Correction-effects measurement"; 9 cases, 14 arms, 1568 runs, none failed, about 95
minutes of work on the CPU)`, with the positive and negative controls it names. The
seeds were `k << 17`, `k = 0..15`. The streams jump by `seed · 2^80` steps in a period
of 2^100 (`src/Random/BOOT.md`), so two seeds equal modulo 2^20 draw the same streams:
seed k equals seed k + 8, and all 560 pairs of the five committed raw CSVs of 16 seeds
(14 arms times 8 seeds times 5 cases) are identical in every printed column. The audit
of the four auditors of 2026-10-18 found it; the measurement over `k << 16` replaces it.
The controls that used eight seeds (`WindowLowerEdge` in reference mode over the audit's
own `k << 17`, `k = 0..7`) were not affected: eight seeds at that stride are eight
phases.

<a id="ca1707-renames-2026-09-24"></a>
## 2026-09-24 — CA1707: test names renamed (underscores removed)

The test method names this node's `BOOT.md` and `API.md` cite were renamed for CA1707
(the analyzer rule against underscores in member names): each underscore-separated
segment PascalCased and concatenated, no other change of wording. The full old → new
map, covering every renamed member across the test tree, is
`tests/test-renames-2026-09-24.txt` (generated by a script and applied by the same
script). No acceptance criterion's date moved; this is a rename of the evidence's own
name, not a re-verification of what it shows.

<a id="statistical-criterion-measurement-2026-09-23-link-1"></a>
## 2026-09-24 — Statistical criterion measurement (2026-09-23), link 1 (the ratchet), superseded 2026-09-24

Superseded by root BOOT.md's own rate-based realignment ("the pass condition compares
failure rates, not single runs", decided 2026-09-24, audit D5): a single seed-0 run
passing or failing the per-run criterion against the original's own replicas is not
evidence either way (the original's own runs fail it 22 times of 197, the port's 9
times of 160 under the pre-fix count floor — `tests/Harness/BOOT.md`, "## Null rate of
the original"). Link 1 is now a rate over sixteen seeds per formulation and layout,
owned by `tests/Harness.Tests/RateCriterionTests` over a pregenerated table
(`tests/Fixtures/rate-table.json`); this node keeps only an exact seed-0 snapshot
(`## Statistical criterion measurement (2026-09-24)` in `BOOT.md`), a change detector,
not a reproduction claim. Link 3 (batched/CUDA against sequential `Double`,
statistically) is untouched by this retirement and stays in `BOOT.md`. The link-1 text
below — the ratchet mechanism, its known-open list, its own measurements and
non-degeneracy proof — is preserved verbatim as it stood before the retirement.

`StatisticalCriterionTests` was realigned 2026-09-23 to the three links root BOOT.md's
own acceptance criteria draw (root BOOT.md, "The `Original` accumulation kind
reproduces the original's printed output", decided 2026-09-21): one criterion cannot
serve both "the port agrees with the original" and "the port is deterministic across
schedules". `PrecisionKind.Double` is no longer read against the original's own
replicas — that conflated the two claims, and is why the 2026-09-19 table's own
verdicts do not carry forward. 30 cases, 20 of them `Category=Long` (10 link 1, 5
link 3 CPU, 5 link 3 CUDA — the CUDA row added later the same day, "## Link 3
measurement" below), all pass.

**Link 1** (`ReferenceMode_OriginalLayout_OriginalPrecision_MatchesLaggedReplicas_ExceptKnownCells`,
`ReferenceMode_IndependentLayout_OriginalPrecision_MatchesIndependentReplicas_ExceptKnownCells`):
reference mode, `PrecisionKind.Original`, seed 0, against the replicas of the run's own
layout (`StatisticalCriterion.Compare`), asserted as a **ratchet**: the set of failing
cells must equal a named list exactly, each entry citing where root BOOT.md names it —
a new failure or a known cell passing both turn the test red, so the list only ever
shrinks by an edit (the same form `tests/Protocol.Tests`' own one-namespace exception
takes, AGENTS.md §12). Measured on this branch merged onto `claude/wave7` at `a621e09`
(`src/Statistics`'s own Dmin-rounding fix; re-measurement note below the known-open
list), matching root BOOT.md's own 2026-09-23 re-run exactly:

| Formulation | `Original` layout vs lagged | Time | `Independent` layout vs independent | Time |
|---|---|---|---|---|
| HPEPA3 | 0 failing / 2002 compared | 22 s | 1 known-open / 1747 compared | 23 s |
| inpt | 0 failing / 2493 compared | 2 s | 0 failing / 2499 compared | 2 s |
| P33 | 0 failing / 1462 compared | 2 s | 1 known-open / 1455 compared | 2 s |
| PSAN02n | 0 failing / 2020 compared | 15 s | 0 failing / 2045 compared | 8 s |
| HMX | 1 known-open / 4343 compared | 51 s | 0 failing / 4342 compared | 50 s |

Known-open list (unchanged by this node — each cell is root BOOT.md's own, cited by
name, not diagnosed here):
- `Original` layout, HMX, `fqdokkarm(31,:)[7]`: the original's own reference already
  fails this cell against its own lagged replicas (root BOOT.md, the still-unticked
  acceptance criterion "The original's own reference output passes the criterion
  against its lagged replicas ... Currently one cell open"); reproducing the
  original's REAL*4 storage reproduces this failure too, at the original's own
  printed value `0.599E-04`.
- `Independent` layout, HPEPA3, `fqkarm_cor[60]`: a seed-0-only failure, not present
  at seeds 1–4 under either precision kind (root BOOT.md, "not an effect of the
  precision kind, and not yet investigated").
- `Independent` layout, P33, `fqkarm_cor[15]`: unexplained, standing since the
  criterion's own 2026-09-20/21 re-measurement (root BOOT.md, "Reference mode in each
  layout satisfies the criterion ...").

Re-measured after merging `claude/wave7` at `a621e09` (`src/Statistics`'s own
Dmin-rounding fix under `PrecisionKind.Original`, which zeroes the port's own
`ConditionBreaking(2)` where the original prints a nonzero rate — a coordinator
flag raised while this table was first being written): every one of the ten rows
above is **bit-identical** — same compared/excluded/failed counts, same known-open
cell, on every row. `ConditionBreaking(2)` does not newly fail, and was not already a
member of `tests/Fixtures/exclusions.json`; its own count-floor predictive band
(`tests/Harness`'s own count-like machinery, not this node's) is wide enough at these
replica counts to hold both the pre-fix and the post-fix port value, so the flagged
risk did not materialize in this criterion. No ratchet entry was added.

Non-degeneracy (AGENTS.md §13, the project's own two-proof taboo): the ratchet was
proven red by temporarily emptying HMX's `Original`-layout known-open list to `[]` and
re-running that one case — `fqdokkarm(31,:)[7]` reported as `UNEXPECTED`, the test
failed as designed, and the list was restored (diffed byte-identical against the
pre-mutation file afterward). No separate positive control is needed for the "known
cell now passes" arm: `Compare`'s own family-wise Student/count/floor machinery is
`tests/Harness`'s, proven there; this node's addition is only the set-equality check.

<a id="statistical-criterion-measurement-2026-09-19"></a>
## 2026-09-19 — Statistical criterion measurement (2026-09-19), superseded 2026-09-23

Superseded by the root's own realignment of the statistical criterion into three links
(root BOOT.md, "The `Original` accumulation kind reproduces the original's printed
output", decided 2026-09-21): this table read `PrecisionKind.Double` (the port's own
arithmetic) against the *original's* replicas, which conflated two different claims —
"the port agrees with the original" and "the port is deterministic across schedules" —
into one criterion that could not cleanly serve either. `StatisticalCriterionTests` was
rewritten 2026-09-23 to run link 1 (`PrecisionKind.Original` against the original's
replicas, ratcheted to a named list of open cells) and link 3 (batched `Double` against
sequential `Double`, statistically, on the port's own replicas); the row this table
supported is now `## Statistical criterion measurement (2026-09-23)` in `BOOT.md`. The
text below is preserved verbatim as it stood before the rewrite; nothing in it was
re-verified after `PrecisionKind` and `StreamLayout` were introduced (2026-09-21 and
before), so its own five/twenty-five figures do not carry forward.

---

`StatisticalCriterionTests` runs the criterion (root BOOT.md, "Statistical reference
criterion") for all five reference formulations: reference mode against the replicas
of its own layout, batched mode CPU against the replicas of its own layout, batched
mode CUDA in the `Original` layout only (root BOOT.md names no independent-layout CUDA
row), 25 cases (`-c Release`, wall time 3m40s total). Every run uses the whole
formulation, matching the replicas (`tests/Fixtures/API.md`).

| Formulation | Ref./Original vs lagged | Ref./Independent vs independent | Batched CPU/Original vs lagged | Batched CPU/Independent vs independent | Batched CUDA/Original vs lagged |
|---|---|---|---|---|---|
| HPEPA3 | FAIL 39/1856 | FAIL 37/1788 | FAIL 110/1712 | FAIL 38/1896 | FAIL 110/1712 |
| inpt | FAIL 1/2546 | FAIL 1/2546 | FAIL 11/2543 | FAIL 1/2543 | FAIL 11/2543 |
| P33 | PASS 0/1484 | PASS 0/1480 | FAIL 42/1484 | PASS 0/1477 | FAIL 42/1484 |
| PSAN02n | PASS 0/2054 | PASS 0/2081 | FAIL 13/2018 | PASS 0/2081 | FAIL 13/2018 |
| HMX | FAIL 44/4155 | FAIL 41/4454 | FAIL 95/4155 | FAIL 42/4232 | FAIL 95/4155 |

6 of 25 pass; 19 fail. Nothing here is excluded, loosened or reclassified to go green
(root BOOT.md Taboos; this node's own Taboos): every failing cell is reported by
`StatisticalCriterionTests` with its quantity, index, value, replica mean, threshold
and spread, on `dotnet test --filter "Category=Long"` (the full per-cell output is
reproducible from this test; it is not transcribed here in full because the count
ranges from single digits to 110 per case — the point of this section is the pattern
across cases, not a hand-kept copy of numbers the machine already prints).

Three patterns stand out in the evidence, each a candidate for a design session this
node cannot hold itself (out of this node's subtree — `Random`, `Particle`,
`Simulation`'s own stream derivation, and `Harness`'s comparison rules — AGENTS.md
§11, proposals only):

- **CUDA and CPU batched mode fail identically, cell for cell, in every formulation**
  (same quantity, same value, same failure count in every row of the "Batched
  CPU/Original" and "Batched CUDA/Original" columns above). This is *positive*
  evidence for the "GPU equals CPU" root invariant holding exactly even where the
  statistical criterion itself does not pass — the two engines agree with each other;
  they disagree with the lagged replicas together.
- **Batched mode's `Original` layout fails harder than reference mode's, in every
  formulation, and the failing quantities are dominated by the exact class root
  BOOT.md's "Known bias of the original's seeds" names** (`Zkarm`, `Zkarm_cor`,
  `Dqmkm2`, `MediumNumberOfBridges`, `epsx`/`epszkarm_n`, tails of `fmkarm`/`fqkarm`/
  `fqmkm2`/`coef`). Root BOOT.md's own execution model gives each batched particle its
  streams "derived from the original seeds by jump-ahead" (`## Constraints`,
  "Execution model") rather than continuing the previous particle's ending state; the
  cross-attempt correlation that produces the bias table ("streams 6, 5, 4, 2, 1 start
  at a⁰, a¹, a², a⁴, a⁵, so X0 of attempt k+1 equals X of attempt k") is a
  *continued-stream* phenomenon, and the lagged replicas are generated by literally
  running the original executable, which is always continued-stream, whole-run. A
  batched particle's jump-ahead severs that continuation between particles, so the
  batched-`Original` result sits (by this evidence) closer to unbiased than to the
  lagged mean, on exactly the quantities the bias table already names as bias-bearing.
  Only the batch-size-1-continued-streams degenerate case is proven bit-identical to
  reference mode (`DegenerateConfigurationTests`); no other batch size was ever shown
  to reproduce the *statistics* of continued streams, only reference mode's own
  bit-for-bit sequence at that one degenerate point. Root BOOT.md's acceptance
  criterion "Batched mode ... satisfies the same criterion ... in each layout" and its
  invariant "Reference mode and batched mode exist for both layouts" may need revising
  in light of this: whether whole-cycle batched `Original` layout can satisfy this
  criterion at all, by construction, is now in question, not assumed.
- **`epsdokfr[0]` fails in every one of inpt's five cases** with the port printing
  exactly `0` against a replica mean of `2.7e-08` and a threshold of `1e-10` — the
  replica mean itself is at the scale of double-precision noise on a REAL*4 sum, the
  class root BOOT.md calls out for "quantities degraded by the original's REAL*4
  accumulation", excluded only with computed evidence (`tests/Fixtures/exclusions.json`,
  `Harness`'s own list, outside this node's subtree). A `pdoksmall` cluster (many
  consecutive indices, root BOOT.md's own documented sensitive quantity, "the
  whole-fraction count ... computed with integer mantissa arithmetic") fails
  systematically in HPEPA3 (both layouts, both modes) and HMX (all five cases) but not
  in P33/PSAN02n/inpt at this scale — consistent with an axis-alignment sensitivity of
  the kind `Harness`'s own 2026-09-18 tail-coverage work already treats for
  `Dkarmcat`/`dokkarm43`/`dokkarm10` (`tests/Harness/API.md`) but does not name for
  `pdoksmall`.
- **Condition 2 of HPEPA3** (root BOOT.md's own named candidate, "suspected REAL*4
  rounding effect") does **not** appear among any of HPEPA3's 39/37/110/38/110
  failures in this criterion: it is not one of the cells this measurement flags,
  consistent with `Hpepa3CounterTests`' own finding that it sits inside its bound
  (11 events against 0, bound 13.3).
