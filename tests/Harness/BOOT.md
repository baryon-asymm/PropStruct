# BOOT.md — Harness

⚠ 2026-09-26: was a declared §15 deviation at 830 non-blank lines (measured 2026-09-21), now inside the 400-line limit for a leaf (AGENTS.md §15) → HISTORY.md#deviation-lifted-2026-09-26

## Purpose

Test scaffolding shared by the test nodes: one CPU host for kernel-compatible code, bit
snapshots of result hashes, one runner for the tree's Python scripts, the parser of
`results.m` into named quantities, a Student's t quantile, and the statistical
comparison of the root criterion against `Fixtures`'s tables — shared so the criterion
is implemented once — the one cut of the time line of a `results.m`
(`ResultsMTimeLine`), and the switch that makes a CUDA fact fail where CUDA is refused
when the release's GPU job requires it (`CudaRequirement`).

## Invariants

- The statistical comparison computes the thresholds of the root criterion at test
  time from the replicas in `tests/Fixtures` and applies `exclusions.json`; it holds no
  tolerance of its own and stores none.

  ⚠ 2026-09-17: was the thresholds read from `tests/Fixtures`, now computed here so
  `results.m` is parsed by one parser only → HISTORY.md#invariants-thresholds-computed-here-not-in-fixtures
- The `results.m` parser yields every quantity of the file with its name and index;
  the inventory of compared quantities `m` is taken from it.
- A moved bit snapshot fails the test; approval is a deliberate file change.
- **Name scheme of `ResultsMFile`** (`ResultsMFile.cs`'s own XML docs; `API.md`,
  "Results parser and statistical comparison"): one name per printed token; the
  invented names are `ConditionBreaking(K)` and the five entries of
  `ResultsMFile.LabelledEchoes`; the time line never becomes a quantity.
- **One cut of the time line** (stage S4 of the delivery, 2026-10-04):
  `ResultsMTimeLine` removes the one line containing `Calculation time`, its terminator
  with it, and throws on a file with none, so that a comparison of two files cannot be
  green because a cut found nothing. `Cli.Tests`, `Simulation.Tests`, `RateTableTool`
  and the packed tool's comparison in `.github/scripts` use it; no private copy remains.
- **A CUDA fact that finds CUDA refused fails when `PROPSTRUCT_REQUIRE_CUDA` is `1`.**
  `CudaRequirement.FailIfRequired` is called on the refused side of every branch on
  `CudaSkippedBecause`; the branches are found by `CudaRefusalBranches`, a text check
  each CUDA test node runs on its own sources, so no list of facts is typed.
- **Resolution and integer-ness are read from the printed token, not a per-field format
  table.** `ResultCell.Resolution` is `10^(exponent - decimalDigits)` of the matched
  number's own text (an `E`-less fixed-point token has exponent 0); `E9.3`/`F7.2` are
  consequences of this rule, not cases coded separately. `ResultCell.IsIntegerPrinted`
  is true exactly when the token has neither `.` nor `E`.
- **Exclusion rule vocabulary.** Two `exclusions.json` rule texts are recognized:
  `"reference cells below 1e-30 compare as zero"` (any layout) and `"not compared
  against independent replicas"` (`ReplicaKind.Independent` only; a no-op elsewhere).
  Any other rule text throws rather than being silently ignored (`Fixtures`' BOOT.md,
  "Exclusions carry evidence": rules are data, but unrecognized data is a stop, not a
  no-op).
- **`m` and `t` are computed once per `Compare` call, not per cell** — except a
  canonical-axis cell's own contributing-replica count, below.

  ⚠ 2026-09-17: was one shared `R - 1` degrees-of-freedom, now a canonical-axis cell
  with fewer than `R` contributing replicas uses its own →
  HISTORY.md#invariants-per-cell-degrees-of-freedom
- **Cells absent from an array (a shorter replica, a shorter reference, or a candidate
  that never wrote a quantity) compare as `0`**, not as "missing" (task decision,
  recorded here because the design session left the choice open) — **except the
  canonical category axis below**, where an absent cell means the category never
  existed in that source's own run and is excluded, not zeroed. The length compared is
  the longest of the reference, the candidate and every replica for that quantity.

  ⚠ 2026-09-17: was the plain "absent compares as 0" rule with no exception, now carved
  out below by "Canonical category axis" → HISTORY.md#invariants-index-misalignment-mechanism
- **Canonical category axis.** `Dkarmcat`, `dokkarm43`, `dokkarm10` and every
  `fqdokkarm(<row>,:)` key are chosen per run from the pocket sizes that run actually
  produced (root BOOT.md, "Statistical reference criterion": "categories whose index
  meaning shifts between runs"): two runs' row `k` are almost never the same physical
  category once the *fixed*-width prefix (the leading `Dkarmcat` entries still equal
  to `k·Dj`, the rows before the run's own first category merge — each run's own, not
  the formulation's: it differs from the reference's in 78 of the 197 null runs,
  `FixedWidthPrefixCensusTests`) gives way to the *adaptive* tail, whose boundary
  values diverge from the very first tail row (measured on HMX: row 31's own boundary
  already ranges 330–350 mkm across its 16 lagged replicas, root BOOT.md's 340). No
  interpolation onto shared bin edges is attempted (root BOOT.md Taboos: no formula the
  model does not already have). Instead:

  ⚠ 2026-09-27: was "shared bit-for-bit by every run of a formulation", now each run's
  own → HISTORY.md#fixed-width-prefix-not-shared
  - `fqdokkarm(<row>,:)` is a *whole-row* axis: a replica whose own file never printed
    that key at all is excluded from that row's replica set for every column, not
    defaulted to a row of zeros (a row a run never produced is not evidence the run
    produced zero pockets in every one of its columns — it is evidence the row's
    category never came into existence there);
  - `Dkarmcat`/`dokkarm43`/`dokkarm10` are *whole-array* axes of varying length: an
    index beyond a given source's own printed length is excluded for that source at
    that index, the same way, rather than treated as a trailing zero;
  - the candidate and reference are held to the same rule: a cell the candidate or
    reference never printed is not compared (root's own cell counts toward
    `Excluded`, not toward a phantom failure at `0`);
  - a cell left with fewer than two contributing replicas cannot have a standard
    deviation and is dropped from comparison (`CriterionReport.Excluded`), not
    compared against a threshold of `0`.

  Every other length-varying quantity keeps the plain "absent compares as `0`" rule:
  this carve-out is named, not general, because a quantity whose *value* differs
  while its *length* stays a fixed, model-determined grid (`fmdok`, `fqmkm1`, …) is a
  candidate for a real defect, and zeroing its absent tail must still be able to catch
  that.
- **Count-like cells.** The original's own printed density/distribution families —
  `fmdok`, `fmkarm`, `fmkarm_cor`, `fmkarm_cor2`, `fqkarm`, `fqkarm_cor`, `fqmkm1`,
  `fqmkm2`, `coef` (named from their own comment headers, "... density distribution
  function ...", "... distribution (step = ...)") and every `fqdokkarm(<row>,:)` — are
  reconstructed counts, not continuous quantities, and their printed **resolution**
  can be far coarser than the *quantum* a small category's own population actually
  prints in. `RunQuantum.TryInfer` infers it per run, from that run's own printed
  array, without assuming which printed total normalizes it: the largest `q = min / k`,
  `k = 1, 2, …`, of which every non-zero cell that `q` resolves is a multiple within
  print resolution (`HISTORY.md#two-refinements-2026-09-19`); no run is a witness of
  another's quantum (`HISTORY.md#per-run-quantum`). When no step is found, the count
  floor is `0` and only the ordinary resolution/Poisson floor apply.

  ⚠ 2026-10-02: was "the smallest nonzero magnitude among the candidate and the
  contributing replicas at that cell … (5% relative tolerance …)", the design before
  2026-09-19, never updated; now the search the code runs →
  HISTORY.md#count-like-cells-search-described-2026-10-02

  ⚠ 2026-09-18: was the reference included in `TryInferRunQuantum`'s own population, now
  excluded (a fourth, unrelated run whenever the candidate is not the reference itself)
  → HISTORY.md#invariants-tryinferquantum-drops-the-reference

  Given a quantum, each contributing replica's value is rounded to a reconstructed
  count; their sum plus the replica count give a negative-binomial predictive interval
  for one more draw (`r = sum(counts) + 1/2`, `p = R/(R+1)`, the Gamma-Poisson recipe of
  `HISTORY.md#criterion-revision-2026-09-17`, step 3) at the cell's own
  `perQuantityAlpha`; the wider of the interval's two one-sided half-widths, in value
  units, is the count floor `c_q`. This is why a cell that is zero in every
  contributing replica does not fail on the reference's one stray count, while a cell
  with a real, populous count keeps the ordinary `t · sd · sqrt(1 + 1/R)` band as its
  governing threshold.

  A step that fits is not thereby identified (B2a,
  `HISTORY.md#b2a-quantum-identification-2026-10-02`, amended by
  `HISTORY.md#b2a-amended-2026-10-02`): `q = min / k` carries the resolution
  `res(min) / k`, and it is identified when the chance that the array's other
  resolvable cells all fall that close to multiples of `q` by accident is at most
  `StatisticalCriterion.Alpha / K`, `K` the steps the search may try. Where the
  candidate's step is not identified, the count floor may govern only a cell whose
  replicas' pooled count is below `CountFloor.HeavyTailRegimeThreshold`; every other
  cell of that array takes the `Student` band.

  A count-governed cell is judged on its reconstructed count `n`, not its printed
  value (B2c, `HISTORY.md#count-region-edge-2026-10-02`): it fails when `n` lies more
  than `w = threshold / q` from `mu + s`, `mu` the predictive's mean in counts and `s`
  the whole number of counts, nearest with a tie toward zero, by which the band's
  centre departs from `mu` (`mu` itself where the 2026-09-24 re-centring applies, the
  replicas' mean elsewhere). Every count of the region `[low, high]`, moved by `s`,
  passes; the implausible-count test is unchanged.

  ⚠ 2026-10-02: was the printed value against `centre ± threshold`, which ends on
  `high`: a count of `high` failed when the centre sat below `mu` or its token
  rounded up; now its count against `mu + s` → HISTORY.md#count-region-edge-2026-10-02

  ⚠ 2026-10-02: was "Not so yet in a row whose smallest count is in the tens or more:
  there the search returns a step several times too coarse … until B2a lands", now
  lifted (the `Count` rows it named are inside their bands) →
  HISTORY.md#c-dense-after-b2a-2026-10-02

  ⚠ 2026-09-24: was `c_q` centred on the replicas' raw-value sample mean, now on the
  predictive's own analytic mean `predictedMean`, below `HeavyTailRegimeThreshold` only
  → HISTORY.md#count-floor-boundary-centring-defect
- **Print-resolution floating-point guard.** A reference token and a replica's own
  token are each independently rounded to the printed resolution before being parsed
  back to `double`; a true difference of exactly one unit of that resolution can then
  appear as very slightly more than one unit in `double` arithmetic (measured: HMX's
  own `coef(531)` and `pdoksmall(52)`/`(61)`, each off by about `1e-15` relative). The
  comparison adds a guard of `max(threshold, |mean|) * 1e-9` (plus a `1e-300` floor for
  `threshold = mean = 0`) to the threshold before failing a cell — a fixed multiple of
  floating-point epsilon, never a fraction of `sd`/`t`/`r_q`/`p_q`, so it cannot mask a
  real statistical difference.
- **Two-sample bias.** `StatisticalCriterion.TwoSampleBias` compares two replica sets
  of one formulation to each other, cell by cell, over every printed quantity: a plain
  Welch two-sample t-test, family-wise corrected at `alpha = 10^-3` over the full count
  of cells both sets reach — no print-resolution floor and no canonical-axis carve-out,
  since there is no reference token here. A different question than `Compare` — "do
  two sets disagree" — not "does one run agree with its own replicas".

  `TwoSampleBiasOfSets`, the seam taking both sets already loaded, is public since
  2026-09-23 for `tests/Simulation.Tests`'s reference-vs-batched comparison; `API.md`,
  "Results parser and statistical comparison" has the caller obligation.

  ⚠ 2026-09-23, the same day: `TwoSampleBiasOfSets` false-positived on a bit-identical
  constant cell compared across two sets of different sizes → HISTORY.md#twosamplebiasofsets-constant-cell-false-positive

  `OfSets` gained a `ConstantCellRule` parameter 2026-09-27; `AlwaysDiffers`, the
  default, is unchanged.
- **Tail coverage statistics.** `StatisticalCriterion.CompareTailRowMean` and
  `CompareAdaptiveIndexMatched` are two more comparisons kept beside `Compare`'s own
  canonical-axis rule, not a replacement of it: neither changes `Compare`'s own
  `Compared`/`Excluded` counts. `Compare` itself, plus both of these, take an optional
  `excludeReplicaOrdinal` (1-based) that drops one replica from the `R` loaded, so
  blind calibration can put that replica in the candidate role against the other
  `R - 1`. What they compare and why, and every figure measured: `HISTORY.md#tail-coverage-2026-09-18`.

  Both cut at the reference's own prefix length `i0`: one fixed cut, applied to the
  candidate and every replica alike. A longer-prefixed run contributes fixed-width rows
  past the cut; a shorter-prefixed one has its adaptive rows displaced — the mechanism
  of E2's declared residual (HPEPA3 port `Original` seed 1, prefix 7 against 8). A
  per-run cut was measured and not adopted (`HISTORY.md#fixed-width-prefix-not-shared`).
- **Mass bracket (E1, 2026-09-27).** `MassFamilyRule.TryBracket` bounds a mass-weighted
  deep-tail cell by the *feasible interval* of the family's own volume quantum (print
  half-units and the histogram's own bin edges), never a single point estimate — the
  full derivation, the 7-cell table and the rejected alternatives are
  `HISTORY.md#e1-mass-bracket-feasible-interval`.
- **Set comparison (`CompareSets`, decided 2026-09-27).** Whether a *set* of port runs
  (seed order) reproduces its replica set, not whether one candidate agrees with
  replicas. `SetComparison` zero-fills every ordinary array to the union length of both
  sets (`Compare`'s own "absent compares as 0" rule); the canonical-axis families ride
  the reference's own axis, never past a source's own reach (`CanonicalAxisSetCells`/
  `ConstantCellRule.ExactBound`, `API.md` has both derivations): a constant-both-sets
  cell differs only when `min(g(n1,n2), g(n2,n1)) < alpha/m`. Only "not compared against
  independent replicas" applies; a defect matches its own row, never zeroed first.

## Dependencies

- [Fixtures](../Fixtures/API.md) — reference files and tables.

Outside the tree: xunit; ILGPU 1.5.3, for `CpuHost`'s CPU accelerator; `System.Text.Json`
(in-box), for `exclusions.json`.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Names no source node, so that every test node can use it.

## Acceptance criteria

→ [ACCEPTANCE.md](ACCEPTANCE.md)

## Sections moved to HISTORY.md

⚠ 2026-09-27: "## Re-measurement after the pdoksmall print-length fix" (2026-09-20) moved in full, to make room under the §15 leaf limit for the false-failure correction below (headline kept: the bound held on all twenty rows, `Compared`/`Failed` bit-identical before and after) → HISTORY.md#re-measurement-after-the-pdoksmall-print-length-fix-2026-09-20

⚠ 2026-09-27: "## Dose-response classification of the 173 failing cells, by family" (2026-09-21) moved in full, same reason (headline kept: HPEPA3/HMX/PSAN02n show the accumulation signature on 170 of 173 cells, `inpt` and P33 Independent are the two named exceptions) → HISTORY.md#dose-response-classification-of-the-173-failing-cells-2026-09-21

⚠ 2026-09-27: "## Reachability of the taboo's REAL*4-accumulation evidence" (2026-09-21)
moved in full (headline kept: of 170 dose-response cells, 6 clear the band — `HMX`
`fmdok[1,2,3]`, both layouts — and 3 are unreachable — `dokkarm43[1,2]`, HPEPA3, a
category-merge threshold flip) →
HISTORY.md#reachability-of-the-taboo-s-real4-accumulation-evidence-moved-2026-09-27

⚠ 2026-09-27: "## Smooth-vs-step check of the dose-response classification" (2026-09-21)
moved in full (headline kept: both curves are smooth and monotonic at every resolution
sampled, refuting the single-threshold-flip hypothesis; the aggregate-gating hypothesis
stays open) → HISTORY.md#smooth-vs-step-check-moved-2026-09-27

⚠ 2026-09-27: "## Seed-based dose-response: which side is the anomaly" (2026-09-21)
moved in full (headline kept: checked at four seeds, the original's own `N/10` →
shipped-`N` drop is systematic while the port's own spread shrinks as `N` grows —
**the original is the anomaly**) → HISTORY.md#seed-based-dose-response-which-side-is-the-anomaly

⚠ 2026-09-27: "## Accumulation kind: link 1 and link 2 (2026-09-21)" moved in full, to
make room under the §15 leaf limit for the calibration-memo correction above (headline
kept: link 1 reproduces, 170 of the 173 `double`-accumulation failures pass under
`original` in both layouts, zero new failures; superseded the same day root ticked all
three links) → HISTORY.md#accumulation-kind-link-1-and-link-2-2026-09-21

## Null rate of the original, and the port's rate against it (2026-09-24)

Root BOOT.md, "the pass condition compares failure rates, not single runs": a single run
of the original or the port passing or failing `Compare`/`CompareTailRowMean`/
`CompareAdaptiveIndexMatched` is not evidence either way. `NullRateCalibration`
(`tests/Harness.Tests`) extracts the leave-one-out loop
`Gate2BlindCalibrationLaggedLeaveOneOutNullRateNumeratorMatchesTheRecordedSet` and adds
the reference-vs-R and pooling this rate needs; `RateCriterionTests` (same node) is it.

**The original's own null rate** (measured 2026-09-24, re-measured after E1 and after
E2; the per-figure progression is `HISTORY.md#null-rate-e1-e2-progression-2026-09-27`):
a "run" fails when any of the three reports names a failing cell for it. **Pooled:
15/197 = 7.6 %** (9/96 lagged leave-one-out, 5/96 independent leave-one-out, 1/5 lagged
reference-vs-R).

⚠ 2026-10-02: was 15/197 = 7.6 % (5/96 independent), now 16/197 after B2a: independent
replica 7 gains `fqdokkarm(35,:)[25]` → HISTORY.md#b2a-rate-figures-2026-10-02

⚠ 2026-10-02, after B2c: was 16/197 (6/96 independent), now 15/197: independent
replica 14 of HPEPA3 loses `fqkarm[74]` and `fqkarm_cor[74]`, the region's edge →
HISTORY.md#b2c-rate-figures-2026-10-02

Computed for the `Lagged` layout only (AGENTS.md §8, narrower than root's own "both
layouts" wording): the archived reference is, by construction, the `Original` layout's
own un-jumped state, replica "k = 0" of the lagged set, a genuine null comparison. It
carries no such relationship to the `Independent` layout, whose own zero-jump state is
a disjoint orbit (root BOOT.md, "Two stream layouts"); a genuine `Independent`-layout
reference is not generated here.

**The port's own rate**, `PrecisionKind.Original`, sixteen seeds `seed = k << 16` for
`k = 0..15`, per formulation and layout, each against all `R` replicas of its own
layout — pregenerated, `tests/Fixtures/rate-table.json` (same HISTORY.md entry has the
progression and the per-formulation breakdown):

| Precision | Failing / total (pooled) |
|---|---|
| `Original` (the pass condition's own data) | 17/160 = 10.6 % |
| `Binary64` (the positive control) | 117/160 = 73.1 % |

**Binomial, not Fisher.** A one-sided exact binomial tail `P(X >= k | n, p)` against
the original's own pooled rate `p = 15/197`, treated as the fixed reference rather than
an equally uncertain second sample (its 197 runs outnumber the port's 160), the same
shape this file's own count floor and print-resolution floor already use — a candidate
judged against a band derived from the replicas, never a symmetric two-sample test.
`BetaBinomialPredictive.Pmf(n, p, rho: 0)`, summed directly over the upper tail (F-a,
`RateCriterionTests.UpperTailProbability`; not `1 - CdfAtMost`, which floors at
`double`'s own epsilon next to 1.0 for a rejecting positive control —
`HISTORY.md#f-a-p-value-floor` has the fix and the floor it replaces): `p = 0.102792`
(rounds to `0.103`; never near the floor, so F-a does not move it). Per-formulation
figures: `HISTORY.md#e2-adaptive-common-range`.

⚠ 2026-10-02: was `p = 0.0624644`, now `0.0999378` (the original's pooled rate 15/197
became 16/197, the port's 18/160 unchanged) → HISTORY.md#b2a-rate-figures-2026-10-02

⚠ 2026-10-02, after B2c: was `p = 0.0999378`, now `0.102792` (the original's pooled
rate 16/197 became 15/197, the port's 18/160 became 17/160) →
HISTORY.md#b2c-rate-figures-2026-10-02

**Pass and positive control**: `p = 0.103 >= α`, no excess; `Binary64` rejects on the
two gated formulations, HPEPA3 and HMX, at `p = 1.63e-36` each (was `4.11e-15`,
`double`'s own floor, shared with `inpt`'s reported-only figure regardless of the true
value — F-a's own finding); PSAN02n's own reported `p = 1.23e-12`, already far from
the floor.

⚠ 2026-10-02: was `p = 1.63e-36` for HPEPA3 and HMX and `1.23e-12` for PSAN02n, now
the figures above, the null rate having moved →
HISTORY.md#b2a-rate-figures-2026-10-02

⚠ 2026-10-02, after B2c: was `p = 0.100`, `p = 1.29e-35` for HPEPA3 and HMX and
`3.64e-12` for PSAN02n, now the figures above, the null rate and the port's rate having
moved → HISTORY.md#b2c-rate-figures-2026-10-02

⚠ 2026-09-27 (AGENTS.md §8, reviewer's calibration memo; no rule changed): the nominal
per-run level, the false-failure classification and the pre-E1 rate test's own
`p = 0.124` are each corrected, and **E1 and E2 have both landed** — together removing
every one of the 7 null and 6 port failures the two caused: `15/197` against `18/160`,
`p = 0.062`, verdict unchanged → HISTORY.md#calibration-memo-2026-09-27

**The tie** (`RateCriterionTests.RateTableTiesToTheCurrentCriterionAndSnapshot`):
`rate-table.json` records a digest of `tests/Harness`'s own criterion files, the exact
rule owned by `tests/Fixtures/BOOT.md`, "## Rate table" (not retold here, AGENTS.md §8),
and of `tests/Simulation.Tests/Snapshots/SeedZeroResultsM.approved.txt`; recomputed and
compared on every run, naming the regeneration command on mismatch.
`tests/RateTableTool/Program.cs` computes the identical digest independently.

⚠ 2026-09-26: was the glob `StatisticalCriterion*.cs` (proven red 2026-09-25, reverted),
now every `*.cs` file minus a declared exclusion list → HISTORY.md#the-tie-widened

⚠ 2026-09-25: this file's own former CA1859/CA1062 exemption (kept because any edit of
`StatisticalCriterion.cs` moved the criterion's hash and a table regeneration used to
crash the test host) is lifted: the generator moved to `tests/RateTableTool`, a
console project run by hand → HISTORY.md#criterion-file-analyzer-exemption-lifted

## Taboos

- No test of a source node lives here.
