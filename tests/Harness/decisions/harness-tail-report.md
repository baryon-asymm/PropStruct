# Tail coverage of the category family — wave 5 report

Nodes: `tests/Harness`, `tests/Harness.Tests`. Branch `claude/wave5`.
Spec: `tests/Harness/BOOT.md`, `## Tail coverage (2026-09-18)`.

## What was built

- `StatisticalCriterion.Compare` gained an optional `excludeReplicaOrdinal` (1-based)
  so a test can put one lagged replica in the candidate role and compare it against the
  other `R - 1` (blind calibration, step 1).
- `StatisticalCriterion.CompareTailRowMean` (step 2) and
  `CompareAdaptiveIndexMatched` (step 3): two new comparisons kept *beside*
  `Compare`'s own canonical-axis rule, not a replacement of it — neither changes
  `Compare`'s own `Compared`/`Excluded` counts; both are their own
  family-wise-corrected reports, and both take the same `excludeReplicaOrdinal` seam.
- `FixedWidthPrefixLength` (internal, `InternalsVisibleTo` for `Harness.Tests` only):
  the shared `i0` (first adaptive row) both new comparisons and the sensitivity sweeps
  need.
- The threshold formula shared by `Compare`/`CompareTailRowMean`/
  `CompareAdaptiveIndexMatched` was extracted into one `Finalize` method (no formula
  duplicated across the three).
- A real defect blind calibration surfaced and fixed: `TryInferQuantum` used to fold
  the *official reference's* own value into the population it infers a count-like
  cell's quantum from. That is harmless when the candidate literally is the reference
  (`Compare`'s usual, non-blind call — the reference's value was always a duplicate of
  the candidate's own), but a genuine defect once a replica plays the candidate role: a
  fourth, unrelated run's value can poison the ratio-consistency check and collapse the
  count floor to zero. Fixed by dropping the reference from that population entirely.
  Confirmed the fix changes nothing for `EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas`'s
  own `compared`/`excluded` figures (`## Acceptance criteria` of `tests/Harness/BOOT.md`
  unchanged).

Tests: `tests/Harness.Tests/TailCoverageTests.cs` (21 cases: 15 blind-calibration
theories across steps 1-3, 6 sensitivity-sweep cases for step 4).

## Step 1 — blind calibration of the existing rule

Header text said "zero failures expected"; that did not hold literally, for two
different reasons (verified precisely by reverting the `TryInferQuantum` fix and
rerunning all five formulations comprehensively, see "Coordinator follow-up" below):

- The `TryInferQuantum` defect above: exactly one candidate, `inpt` replica 6 (5
  cells, all at index 46), failed on it alone; every other candidate's outcome is
  identical whether the fix is present or not (the official reference is absent or
  zero at every cell the remaining candidates fail on, so folding it into
  `TryInferQuantum`'s population there is a no-op). This candidate does not recur once
  the fix is applied and is not a named exception.
- Three candidates, 18 cells total, unrelated to the defect and not removable by it:
  `HPEPA3` replica 3 (`fmkarm[70]`, 1 cell), `HPEPA3` replica 7 (`fmkarm_cor`, 3
  cells), `P33` replica 2 (`dokkarm10`/`dokkarm43`/`fqdokkarm` rows 1-4, 14 cells).
  Named exactly — `(formulation, replica ordinal, quantity, index)`, not the whole
  candidate — in `TailCoverageTests.Step1KnownOutliers`; no `exclusions.json` entry, no
  loosened `alpha`/`R`. The statistical reading of these 18 cells (not "rare event") is
  in `tests/Harness/HISTORY.md#implementation-and-measurements-2026-09-18` and repeated
  with the raw data below.

Measured `compared`/`excluded` (summed over the leave-one-out candidates, unchanged by
the `TryInferQuantum` fix): HPEPA3 58244/8454, inpt 40672/816, P33 23728/900,
PSAN02n 32560/288, HMX 68092/37997.

## Step 2 — tail row mean

One systematic, already-declared cause (`P33` replica 14's own adaptive tail is 5 rows
against every other replica's 1 — the row-count-mismatch limitation this section's own
text anticipated in advance) plus three unrelated single-cell rare-event outliers
(`HMX` replica 14, `inpt` replicas 1 and 11, `PSAN02n` replica 5). Measured
compared/excluded: HMX 1136/0, inpt 576/0, HPEPA3 1056/0, P33 528/0, PSAN02n 528/0.

## Step 3 — adaptive index matched

The same `P33` replica 14, from two angles: as the candidate, and as a replica that
contaminates *another* candidate's own pool (`P33` replica 15's own comparison fails
because replica 14 is one of its 15 "other" replicas). One root cause, both angles
named. Measured compared/excluded: HMX 896/0, inpt 80/0, HPEPA3 832/0, P33 80/0,
PSAN02n 80/0.

## Category=Long

Measured: the fast set without `TailCoverageTests` runs 403 cases in about 1 s; steps
1-3's 15 blind-calibration theories alone take about 8 s (HPEPA3 parses
32 × 31 = 992 replica files per theory); step 4's 6 sensitivity-sweep cases take about
1 s. Steps 1-3 marked `[Trait("Category", "Long")]`; step 4 stays in the fast set.
`EachFormulationsOwnGsv2ReferencePassesAgainstItsLaggedReplicas` stayed green with zero
failures throughout.

## Step 4 — measured sensitivity (HMX)

- (a) roll-and-mix (tail row mean): `rho* = 0.1%` (a 0.1% grid below the first
  2%-grid hit). A cyclic roll was tried first and rejected — it wraps a row's largest
  column into its smallest, manufacturing a jump that measures the wrap artifact, not
  `rho`; the reported figure uses a one-column shift with the vacated far column
  zero-filled instead.
- (b) boundary stretch (adaptive index matched): **not caught** at `delta` = 1, 2, 5 or
  10%, the four values the section specifies. The matched-index window mixes
  near-terminal rows of short-tailed replicas with interior rows of long-tailed ones at
  the same position, so the cross-replica spread there is itself large — the declared
  mis-specification of this check, now measured as low power against a boundary-scale
  stretch of this size.
- (c) truncation (adaptive index matched): `k* = 14` of 31 adaptive rows.

## Step 5 — replacing the boundary rule: not proposed

The section's own gate ("only if 1-4 are green on all five formulations") is not met
unconditionally: step 1 needed three named candidates' worth of cells (18) and steps
2-3 needed the P33 replica 14 exception to reach green. "Green" here means every
failure was individually traced to a named cause, cell by exact cell — not that the
boundary rule was shown redundant. **Recommendation for a future design session** (not
performed): the
`rho*`/`delta*`/`k*` figures show `CompareAdaptiveIndexMatched` is markedly weaker than
`CompareTailRowMean` specifically at the boundary-value question the canonical-axis
rule answers (not caught even at 10% stretch, an order of magnitude past `rho*`) — an
argument for narrowing what index matching would replace (if anything), not widening
it, should that session's own evidence point the same way.

## Non-degeneracy (tests/Harness.Tests/BOOT.md, ## Acceptance criteria)

Four mutations applied and reverted, each proven red then green:

1. `FixedWidthPrefixLength` forced to `return dkarmcat.Length` (i0 = full length, no
   adaptive row anywhere): 3 of 10 `Step2`/`Step3` cases went red (`P33`/`PSAN02n`
   step 2: the new `totalCompared > 0` guard fired — without it this mutation would
   have stayed silently green; `HMX` step 3: a genuine wrong `AdaptiveRowCount`
   comparison).
2. `CompareAdaptiveIndexMatched`'s `matchedLength` cap dropped the candidate's own
   adaptive length (kept only the replica-pool minimum): `Step3` on HMX threw
   `IndexOutOfRangeException` inside the method itself — the cap is what prevents an
   out-of-bounds read, not just a mis-specified band.
3. The fixed `TryInferQuantum` call site reverted to fold the reference's own value
   back into the population: `Step1` on `inpt` reproduced the original symptom exactly
   (`inpt` replica 6, 5 unexplained failures, all five distribution-function
   quantities at index 46 collapsed to their bare print-resolution floor).
4. (2026-09-18, coordinator review) A fabricated second failure at `HPEPA3` replica 3
   (`Nkarm` doubled), a candidate already excused for exactly one cell (`fmkarm[70]`):
   `Step1` went red on the new `Nkarm[0]` cell specifically, while `fmkarm[70]` stayed
   excused — proving the per-cell allow-list (not the whole candidate) is what the
   check now enforces. Full detail: "Coordinator follow-up", item 1, below.

## Coordinator follow-up (2026-09-18): narrowing the allow-lists, and the statistics of step 1's three outliers

### 1. Narrowing `Step1KnownOutliers`/`Step2KnownOutliers`/`Step3KnownOutliers` to exact cells

Each entry used to be `(Formulation, ReplicaOrdinal)`: once `("HPEPA3", 3)` was listed,
that candidate could fail on any cell, any number of times, forever, without the check
ever going red for it again. Changed to `KnownOutlierCell(Formulation,
ReplicaOrdinal, Quantity, Index)` — one record per exact cell — with a shared
`FindUnexplained` helper (used by all three steps, so one non-degeneracy proof covers
all three): a failure is excused only if its own `(formulation, replica, quantity,
index)` is listed; anything else at a listed candidate, or any failure at an unlisted
candidate, is unexplained.

**Non-degeneracy, proven and reverted:** temporarily fabricated a second, unrelated
failure at `HPEPA3` replica 3 (which already has exactly one allowed cell,
`fmkarm[70]`) by doubling its own `Nkarm` scalar before comparison. Red:
`Step1_BlindCalibration_CanonicalAxisRule(formulation: "HPEPA3")`,
`Assert.Empty(unexplained)` failed on `"HPEPA3 replica 3: unexplained failure
Nkarm[0] ..."` — the listed `fmkarm[70]` cell stayed excused, the new, unlisted
`Nkarm[0]` cell did not. Reverted; `dotnet test tests/Harness.Tests
--filter "FullyQualifiedName~Step1_|...~Step2_|...~Step3_"` back to 15/15 passed.

### 2. The statistics of step 1's three outlier candidates

At `alpha = 10^-3` family-wise per application and 96 leave-one-out applications, the
model's own naive expectation is `96 * 0.001 ~= 0.1` failing candidates. Three
candidates were observed — about 31x that. This is evidence the model (the Student
band, and the count floor's quantum inference) is mis-specified for two distinct
classes of quantity, not a rare draw within a correct model. Raw data:

**P33's 16 `dokkarm10(1)` values (lagged replicas), plus the reference:**

| Replica | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Value | 26.0 | **37.0** | 26.1 | 26.1 | 26.1 | 26.0 | 26.1 | 26.1 |

| Replica | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | reference |
|---|---|---|---|---|---|---|---|---|---|
| Value | 26.0 | 26.1 | 26.1 | 26.0 | 26.0 | 26.0 | 26.1 | 26.1 | 26.1 |

Excluding replica 2: mean `26.06`, `sd = 0.0507`. Replica 2's `37.0` is `(37.0 -
26.06) / 0.0507 ~= 215.7` standard deviations from the other 15. Total `Nkarm` (the
run's whole pocket count) for all 16 replicas: `2,802,718`-`2,822,580` — replica 2's is
`2,803,165`, unremarkable, near the low end of that same tight range. `fqdokkarm(1,:)`
row sums (a normalized-fraction check, not a population count — `QDOKSS` itself is
never printed) are likewise unremarkable for replica 2 (`0.10002` against everyone
else's `0.0999`-`0.1001`).

**Reading:** fifteen values clustered inside `0.1` of each other and one value `215`
of their own standard deviations away is not a heavy tail — a heavy tail would show
some of the *other* fifteen drifting toward `37` too, with decreasing frequency, not
sitting at a razor-tight `26.0`-`26.1`. This is **one anomalous run**, not a general
property of the `dokkarm10(1)` statistic. The run-wide total pocket count rules out a
whole-run anomaly (replica 2's is ordinary), so if the cause is a genuine Monte Carlo
event rather than a data-generation defect, it is isolated to whatever few pockets
landed in category 1 for that one seed — a mean over a small, unlucky count can swing
this far, and the criterion has no floor for it because the model never prints a
per-category population to size that floor from. The printed file cannot distinguish
"genuine rare structural transition in category 1" from "a defect in how replica 2 was
generated"; what it can rule out is a smooth heavy tail of the ordinary kind.

**HPEPA3's 32 `fmkarm[70]` values (lagged replicas):**

| Replica | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| Value | absent | 9.8e-06 | **1.02e-04** | absent | absent | absent | 1.91e-05 | 9.75e-06 |

| Replica | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 |
|---|---|---|---|---|---|---|---|---|
| Value | 1.94e-05 | 0.0 | absent | absent | absent | 0.0 | absent | absent |

| Replica | 17 | 18 | 19 | 20 | 21 | 22 | 23 | 24 |
|---|---|---|---|---|---|---|---|---|
| Value | absent | absent | absent | 3.0e-05 | 0.0 | 1.89e-05 | absent | 4.11e-05 |

| Replica | 25 | 26 | 27 | 28 | 29 | 30 | 31 | 32 |
|---|---|---|---|---|---|---|---|---|
| Value | absent | absent | 3.96e-05 | absent | absent | absent | 2.02e-05 | absent |

("absent" = the replica's own `fmkarm` array is shorter than index 70, compared as `0`
under this quantity's plain rule; official reference is likewise absent, length 67.)
19 of 32 absent, 3 exactly `0`, 10 nonzero including the candidate's own. Excluding
replica 3 (the candidate), the 9 other nonzero values sorted: `9.75e-06, 9.80e-06,
1.89e-05, 1.91e-05, 1.94e-05, 2.02e-05, 3.00e-05, 3.96e-05, 4.11e-05`.

**Reading:** this is a roughly geometric climb, each step about `1.05`-`1.3x` the
last, from `9.75e-06` to `4.11e-05` (a factor of `4.2`) — and the candidate's own
`1.02e-04` continues that same climb (another factor of `2.5` past the largest of the
other 31), not a discontinuous jump away from it. That is the signature of a
**genuinely heavy right tail**, not an isolated contaminated run: `fmkarm[70]` is a
deep-tail cell of a sparse-count density family, and rarer, larger draws keep
occurring at a roughly constant multiplicative rate rather than the distribution
having a sharp upper edge. `TryInferQuantum`'s 5% multiple tolerance cannot follow
this climb indefinitely: the ratio between the two largest of the 9 other nonzero
values is `4.11e-05 / 9.75e-06 = 4.215`, `0.215` away from the nearest integer
multiple `4`, past the `0.05 * 4 = 0.2` tolerance — so the quantum search already fails
one step before the candidate's own value is even considered, the negative-binomial
count floor never engages, and the plain, thin-tailed Student band is what a
genuinely heavy-tailed quantity is measured against. `HPEPA3` replica 7's
`fmkarm_cor[68]/[71]/[72]` read the same way (same family, same mechanism, three
adjacent columns because one rare large pocket-size draw moves several neighbouring
histogram cells of that one replica together).

**Which of the three (bimodal, heavy-tailed, one contaminated run) each is:** `P33`
`dokkarm10(1)` is **one contaminated/anomalous run** (a single hard outlier against an
otherwise razor-tight cluster, not a spread of values). `HPEPA3` `fmkarm[70]` (and
replica 7's `fmkarm_cor`) is **heavy-tailed** (a continuous, roughly geometric climb
the candidate's own value simply continues). Neither reading is bimodal — no second
cluster of replicas near either outlier's own value.

**What a future design session would decide, not implemented here:** whether the
count floor's multiple tolerance should widen to follow a candidate value that
continues an otherwise-monotonic replica sequence (risk: this would also swallow a
real defect that happens to look like "one more step of the climb"); and whether
`dokkarm10`/`dokkarm43` need a floor sized from an estimate of their own category's
population, which the model does not print today — a `Statistics`/`Simulation`-level
decision (new diagnostic output) or a declared, permanent limitation, either way
outside `Harness`'s own reach and outside this task's scope (root BOOT.md Taboos: "no
loosening of a tolerance ... without computed evidence").

## Final checks (single command, foreground)

```
python -X utf8 tools/protocol-lint/protocol_lint.py . --exclude templates   # 0 errors, 0 warnings
dotnet build PropStruct.sln                                                  # 0 Warning(s), 0 Error(s)
dotnet test tests/Harness.Tests --filter "Category!=Long"                    # 409/409 passed, 1 s
dotnet test tests/Harness.Tests                                              # 424/424 passed, 10 s
```

(Rerun after the coordinator's two fixes above; same pass counts, full-set duration
8-10 s across runs — within measurement noise of the run this section already
attributes to steps 1-3's own file-parsing cost, not a regression.)

Whole-solution fast set also checked (`dotnet test PropStruct.sln --filter
"Category!=Long"`): 69 + 450 + 621 + 46 + 409 = 1595 cases, all passed — no other node
touched or broken.

## Scope note / proposal

`tests/Fixtures` was not touched, as instructed. No missing fixture was needed: every
figure above comes from files already in `tests/Fixtures/references` and
`tests/Fixtures/replicas-lagged`, or from mutating those in memory (step 4). No
escalation needed.

## Files changed

- `tests/Harness/StatisticalCriterion.cs` — `excludeReplicaOrdinal`, `Finalize`
  extraction, `FixedWidthPrefixLength`, `CompareTailRowMean`,
  `CompareAdaptiveIndexMatched`, `TryInferQuantum` reference-contamination fix.
- `tests/Harness/API.md` — the three new/changed public (and one internal) members.
- `tests/Harness/BOOT.md` — ⚠ correction to "Count-like cells", new subsection (now
  `tests/Harness/HISTORY.md#implementation-and-measurements-2026-09-18`, "Implementation
  and measurements (2026-09-18)"; rewritten a second time after the
  coordinator's review: exact bug-vs-outlier accounting, "rare event" replaced by the
  statistical reading above), two Acceptance criteria ticked (one reworded twice, per
  AGENTS.md §6), a new Invariants bullet pointing at the section.
- `tests/Harness.Tests/TailCoverageTests.cs` — 21 cases; `Step1/2/3KnownOutliers`
  narrowed from `(formulation, replica)` to exact `(formulation, replica, quantity,
  index)` cells behind a shared `FindUnexplained` helper.
- `tests/Harness.Tests/BOOT.md` — two new L2 rows, three recorded mutations.
