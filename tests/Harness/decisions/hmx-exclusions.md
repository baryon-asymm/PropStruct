# HMX exclusion breakdown (task C, wave 4)

Investigation only. `tests/Harness/StatisticalCriterion.cs` and the criterion it
implements were **not** changed; temporary `Console.Error` instrumentation and a
scratch xunit file were added to `tests/Harness.Tests` to extract the breakdown below,
then reverted (`git checkout -- tests/Harness/StatisticalCriterion.cs`, scratch file
deleted). `git status` on `tests/Harness` and `tests/Harness.Tests` shows no trace of
either after cleanup, other than the unrelated, intentional wave-4 edits (task B).

Method: ran `StatisticalCriterion.Compare("HMX", reference, ReplicaKind.Lagged)` with
`candidate = reference` — the exact call
`StatisticalCriterionTests.EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`
makes, which is where `compared=4376, excluded=2117` (`tests/Harness/BOOT.md`, ##
Acceptance criteria) comes from — and logged, at each of the two places the
implementation increments `excluded`, the quantity name, the reason
(`candidate-never-reached-{row,index}` vs. `fewer-than-2-contributing-replicas`) and
the cell count added.

## Headline figures

| Formulation | reference cells (`ResultsMFile.ParseCells`, summed) | quantities | `compared` | `excluded` | excluded / reference cells |
|---|---|---|---|---|---|
| HPEPA3 | 1997 | 110 | 2035 | 39 | 2.0% |
| inpt | 2544 | 135 | 2542 | 42 | 1.7% |
| P33 | 1476 | 100 | 1483 | 48 | 3.3% |
| PSAN02n | 2034 | 127 | 2053 | 0 | 0.0% |
| **HMX** | **6334** | **152** | **4376** | **2117** | **33.4%** (raw), **32.7%** (real, see below) |

HMX is not a borderline outlier, it is a different regime: 10–20x the exclusion rate
of the next-highest formulation (P33), and roughly a third of everything HMX's own
reference prints never enters the comparison at all.

## Where the 2117 excluded cells come from

Every excluded cell in HMX falls into exactly two mechanisms, both from the
canonical-category-axis rule (`tests/Harness/BOOT.md`, ## Invariants, "Canonical
category axis") — the exclusions-JSON rule (`pdoksmall`) contributes 0 exclusions here
(it zeroes cells, it never drops them):

| Quantity family | Reason | Cells | Share of 2117 |
|---|---|---|---|
| `fqdokkarm(<row>,:)`, rows 32–59 (28 rows × 71 columns) | fewer than 2 of 16 lagged replicas still share the reference's category boundary at that row | 1988 | 93.9% |
| `Dkarmcat` | fewer than 2 contributing replicas (indices 31–58, 0-based, 28 cells) | 28 | 1.3% |
| `dokkarm43` | same | 28 | 1.3% |
| `dokkarm10` | same | 28 | 1.3% |
| `Dkarmcat` | index beyond the reference's own array (indices 59–73, 15 cells: some replica's own adaptive binning printed *more* categories than the reference ever did) | 15 | 0.7% |
| `dokkarm43` | same | 15 | 0.7% |
| `dokkarm10` | same | 15 | 0.7% |
| **Total** | | **2117** | **100%** |

Splitting "real" vs. "phantom" cells (phantom = an index/row that does not exist in
HMX's own 6334-cell reference output at all, only in a replica that happened to print
further): 2072 of the 2117 (97.9%) are real cells HMX's own reference printed but that
never get compared; 45 (2.1%) are indices some replica invented beyond the reference's
own length, correctly excluded rather than compared against a phantom "0".
2072 / 6334 = **32.7%** of HMX's own printed information goes uncompared; using the
raw 2117 against the same 6334 denominator gives 33.4% (the figure to quote if a
reader wants "excluded / total`Compare`-visible cells" rather than "excluded / cells
HMX itself printed").

`fqdokkarm`'s adaptive tail alone (1988 cells) is 93.9% of the exclusion — this is
overwhelmingly a single mechanism, not a scattering of small ones.

## Root cause, with the numbers behind it

HMX's reference prints a 59-row pocket-size category axis (`Dkarmcat`/`dokkarm43`/
`dokkarm10` all length 59, `fqdokkarm` 59 rows of 71 columns each — `FQROWCOUNT=59`,
`DKARMCATLEN=59`, `FQROWWIDTH=71`, measured directly). The prefix-match length of each
of the 16 lagged replicas' own `Dkarmcat` against the reference's:

| Replica | match length | replica's own `Dkarmcat` length |
|---|---|---|
| 1 | 30 | 48 |
| 2 | 31 | 57 |
| 3 | 30 | 46 |
| 4 | 28 | 57 |
| 5 | 28 | 62 |
| 6 | 30 | 56 |
| 7 | 28 | 59 |
| 8 | 31 | 51 |
| 9 | 28 | 52 |
| 10 | 32 | 74 |
| 11 | 31 | 68 |
| 12 | 31 | 55 |
| 13 | 28 | 58 |
| 14 | 28 | 55 |
| 15 | 29 | 49 |
| 16 | 28 | 63 |

Two things stand out: (1) the match length caps out at 28–32 for *every* replica — the
shared, bit-exact "fixed-width" prefix (root BOOT.md, "Statistical reference
criterion") is only about half of the reference's 59 categories; (2) each replica's
own total category count varies wildly (46 to 74), which is the direct cause of (1) —
HMX's neighbour-size draws in the adaptive tail are volatile enough between
independent 128-bit-seeded runs that no two runs settle on the same bin edges past
roughly the 30th category. By row 32 (0-based 31), at most one of the sixteen lagged
replicas still agrees with the reference, so the canonical-axis rule's "fewer than two
contributing replicas ⇒ no standard deviation ⇒ excluded" clause (this node's BOOT.md)
fires for every remaining row, all the way to row 59. This is exactly the same
mechanism `tests/Harness/HISTORY.md#criterion-revision-2026-09-17` documents for HMX rows
53/56/58/59 as "the deepest surviving failure ... exactly a category no replica's own
binning had produced" — this investigation shows that failure was the visible edge of
a much larger, systematically excluded tail, not an isolated case.

Compare HPEPA3/inpt/P33/PSAN02n, whose reference category axes are much shorter (their
`compared+excluded` totals of 2035–2542 against HMX's 6493 make this visible without
re-running anything) — a formulation with a narrower oxidizer/pocket size range simply
has fewer adaptive-tail categories to begin with, so there is less tail for the
replicas to disagree about.

## Is this a defect of the canonical-axis rule, or sound?

**Sound, as a decision not to compare data that is not comparable.** The canonical-axis
rule exists because raw row-index comparison across independently-binned adaptive
category axes previously produced exactly the false failures this investigation's
numbers explain (`tests/Harness/HISTORY.md#criterion-revision-2026-09-17`, step 1: HPEPA3 vs
GSV=3 unrevised, 581/2124 failures, the bulk from this same category-axis/count
mechanism). Refusing to compare "row 40 of replica A" against "row 40 of the
reference" when the two rows do not represent the same physical pocket-size range is
the correct call — comparing them would not be a stricter check, it would be a
*wrong* one, comparing incommensurate numbers and reporting either a spurious failure
or a spurious pass depending on which side of the divergence the true values happen to
land. Root BOOT.md's own taboo ("no formula the model does not already have") and the
"no interpolation of category boundaries — the file does not print the population
weights an interpolation would need" reasoning already in `tests/Harness/BOOT.md`,
"Canonical category axis" is a considered, not an accidental, limit.

**But the coverage gap is real and HMX-specific, and it is worth a design session, not
because the rule is wrong but because a third of one formulation's printed histogram
is completely unchecked by the port's own regression test.** Three observations:

1. **The gap is concentrated, not diffuse.** 93.9% of it is one mechanism
   (`fqdokkarm`'s adaptive tail). A single, scoped improvement to how that one family
   is compared would recover almost all of the lost coverage; there is no need to
   redesign the whole canonical-axis rule to address the bulk of this finding.
2. **A cumulative comparison on a common axis could recover real signal without
   inventing a formula the model does not have.** `fqdokkarm(<row>,:)` is (per its own
   name and `tests/Harness/BOOT.md`) a cumulative-style density family; summing each
   source's own row-32-to-its-own-end mass into a single "tail total" scalar per
   column, and comparing *that* scalar (candidate vs. replicas) instead of leaving the
   whole tail uncompared, uses only numbers the file already prints (a sum), not an
   interpolated or reconstructed boundary. It would trade per-category resolution in
   the tail for a coarser but real, comparable check of the tail's aggregate mass —
   recovering the "is the tail behaving right in aggregate" question this criterion
   currently answers with silence, at the cost of not being able to localize a defect
   to one specific tail category (which the current design cannot do here anyway,
   since it excludes the whole tail).
3. **A boundary-interpolated (piecewise-linear) comparison is possible but heavier and
   more assumption-laden.** Since `Dkarmcat` prints each row's own physical boundary
   (not just its index), a candidate's and each replica's tail could in principle be
   resampled onto one shared set of diameters by linear interpolation of the
   *cumulative* count/mass curve (an empirical-CDF resampling, not a new physical
   formula) — recovering resolution the aggregate-scalar approach above gives up. This
   needs no per-category population weights beyond what interpolation of a monotonic
   cumulative curve already implies, so it likely does not cross root BOOT.md Taboos'
   ban on a second implementation of any part of the model, but it is materially more code and more
   assumptions (choice of interpolation grid, handling of a replica whose tail is
   shorter than the grid) than the aggregate-scalar option, for a formulation-specific
   problem that only HMX has at this magnitude.

**Recommendation** (not implemented, per this task's scope): raise both options at the
next design session that touches `tests/Harness`, scoped to `fqdokkarm`'s excluded
tail rows specifically (the 84 `Dkarmcat`/`dokkarm43`/`dokkarm10` cells are two orders
of magnitude smaller and likely not worth separate machinery). Start with the
aggregate-tail-scalar option (recovers ~94% of the gap's mass with the least new
mechanism, and is easy to add as one extra derived, clearly-labelled comparison
without touching the existing per-row exclusion behaviour); treat the CDF-interpolation
option as a fallback only if the aggregate check turns out too coarse to catch a real
regression once `Particle`/`Statistics` exist and there is an actual defect to check
it against. Either way, this is `tests/Harness`'s own decision to design and cost, not
something to fold into the current wave silently — the criterion's target ("the
reference passes against its lagged replicas with no failure") is already met, and
this gap is a coverage question, not a correctness one.

## Evidence trail

- `dotnet test tests/Harness.Tests --filter FullyQualifiedName~ScratchHmxExclusionDump`
  (scratch file, deleted after use) produced the raw `EXCL`/`ALLFORM`/`REPLICAMATCH`
  lines this report is built from; raw output kept in this SCRATCH directory
  (`hmx_dump_raw.txt`, `hmx_dump_raw2.txt`, `hmx_dump_raw3.txt`) for anyone who wants to
  re-derive the tables above without re-running the instrumented build.
- `tests/Harness/StatisticalCriterion.cs` is unchanged from `git log`'s last commit to
  it; `git status` on `tests/Harness*` shows only the task-B edits
  (`tests/Harness/BOOT.md`, `tests/Harness.Tests/TwoSampleBiasTests.cs`).
