# ACCEPTANCE.md — PropStruct (tree root)

## Acceptance criteria

- [x] 2026-09-17: the generator reproduces the original state sequence bit for bit, and
      the packed seeds equal the limbs of the Fortran source
      (`tests/Random.Tests`: `SourceLimbsTests`, `ArithmeticTests`, `DrawStreamTests`).
- [x] 2026-10-02: the original's own null rate against its replicas is measured, an
      input of the rate criterion below: **15 of 197 runs, 7.6 %** — 9/96 lagged and
      5/96 independent leave-one-out, 1/5 archived references against all `R` of the
      lagged layout (`tests/Harness.Tests/NullRateCalibration`; `tests/Harness/BOOT.md`,
      "## Null rate of the original").

  ⚠ 2026-09-20: was ticked with "zero failing cells" on all five formulations, now unticked — that reading rested on a predictive-interval formula with `p` and `q` swapped, which could not fail → HISTORY.md#acceptance-criterion-unticked-count-rule

  ⚠ 2026-09-24: was "the original's own reference output passes … with no failure", now
  its null rate is measured → HISTORY.md#null-rate-criterion-reformulated

  ⚠ 2026-09-27: was 22 of 197 (13/96, 8/96, 1/5), now 15 of 197 after two rule defects
  of the criterion were fixed (E1, E2) → HISTORY.md#rate-figures-e1-e2-2026-09-27

  ⚠ 2026-10-02: was dated 2026-09-27, the day before F-c changed the criterion's code
  (`tests/Harness/HISTORY.md#f-c-exponent-of-decade-low-at-powers-of-ten`); the null
  rate stayed 15 of 197, re-read 2026-10-02 (AGENTS.md §6, one date per evidence).

  ⚠ 2026-10-02, after B2a (`tests/Harness/HISTORY.md#b2a-amended-2026-10-02`): was 15
  of 197 (5/96 independent), now 16 of 197: independent replica 7 of inpt gains
  `fqdokkarm(35,:)[25]` → `tests/Harness/HISTORY.md#b2a-rate-figures-2026-10-02`

  ⚠ 2026-10-02, after B2c (`tests/Harness/HISTORY.md#count-region-edge-2026-10-02`): was
  16 of 197 (6/96 independent), now 15 of 197: HPEPA3 independent replica 14 loses
  `fqkarm[74]` and `fqkarm_cor[74]`, the region's edge →
  `tests/Harness/HISTORY.md#b2c-rate-figures-2026-10-02`

- [x] 2026-10-03: Reference mode under `Original` fails the statistical criterion no
      more often than the original fails it against its own replicas (the rate decision
      under "Statistical reference criterion"). Port: **17 of 160 runs, 10.6 %**,
      sixteen seeds `k·2¹⁶` per formulation and layout. A one-sided exact binomial test
      against 7.6 % gives p = 0.103, nominal (below), so no excess at `α`. Per
      formulation, reported and not gated, the smallest p is `inpt`'s 0.092. Positive
      control: `Binary64` fails 117 of 160, rejected on HPEPA3 and HMX at
      p = 1.6·10⁻³⁶.
      P33 under `Binary64` fails 3 of 32, fewer than under `Original`, so on that
      formulation the test has no power to see the precision kind
      (`tests/Harness.Tests/RateCriterionTests`, reading `tests/Fixtures/rate-table.json`,
      which is tied to the criterion's source and the seed-0 snapshot).

  ⚠ 2026-09-24: was "Reference mode in each layout satisfies the criterion against the
  replicas of that layout …", unmeetable (the original itself fails it in 11.2 % of
  runs), now the rate comparison above →
  HISTORY.md#superseded-per-run-reference-criterion

  ⚠ 2026-09-27: was 23 of 160, p = 0.124, `Binary64` 121 of 160 at p = 1.7·10⁻¹⁵, now
  the figures above: E1 and E2 fixed two rule defects, F-a the p-value floor →
  HISTORY.md#rate-figures-e1-e2-2026-09-27

  ⚠ 2026-10-02: was dated 2026-09-27; F-c (2026-09-28) and the per-cycle plane
  (874d81c, 2026-10-01) since changed the criterion and all 160 `Original` runs it
  reads. The table regenerated 2026-10-01 has the same 18 failing runs (F-c took one
  cell from one of them) and every figure above, re-read 2026-10-02.

  ⚠ 2026-10-02, after B2a: was p = 0.062 against 7.6 %, `inpt`'s 0.092 and
  p = 1.6·10⁻³⁶, now the figures above on the regenerated table: the null rate
  moved from 15 to 16 of 197, the 18 of 160 and the 117 of 160 did not; two port
  runs gained one cell, `fqdokkarm(35,:)[25]` →
  `tests/Harness/HISTORY.md#b2a-rate-figures-2026-10-02`

  ⚠ 2026-10-02, after B2c: was 18 of 160 (11.3 %), p = 0.100 against 8.1 %, `inpt`'s
  0.114 and p = 1.3·10⁻³⁵, now the figures above on the regenerated table: the
  original's null rate moved from 16 to 15 of 197 and the port's 18 to 17 of 160
  (P33 `Independent` seed 13 loses `fqkarm[63]`, the region's edge); the 117 of 160
  did not move → `tests/Harness/HISTORY.md#b2c-rate-figures-2026-10-02`

  ⚠ 2026-10-03: was dated 2026-10-02, the table before the per-cycle plane followed
  the executable's listing; the 160 `Original` runs, the table and the seed-0 snapshot
  regenerated, every figure above read the same (147 of the 160 runs differ in bytes,
  none in its failing cells; the 160 `Binary64` runs are byte-identical)
  → `src/Statistics/ACCEPTANCE.md`, A6

  p is nominal, not exact: the binomial treats the 160 port runs as independent, but
  runs of one formulation share a replica pool (`tests/Harness/BOOT.md`, "## Null rate
  of the original", has the count and the false-failure classification).

- [x] 2026-10-01: Batched mode (whole-cycle batches, CPU accelerator and CUDA, at the
      default attempt budget 8192) agrees
      with reference mode in the `Independent` layout under `Binary64`: no cell differs
      on any of the five formulations, Welch per cell at the tree's `α` over eight seeds
      `k·2¹⁷`, positive control seen red
      (`tests/Simulation.Tests/StatisticalCriterionTests`, the CPU and CUDA link-3
      rows). Batched `Original` is refused ("Two stream layouts").

  ⚠ 2026-10-01: was dated 2026-09-24 at budget 32, now re-run at the new default 8192
  (`src/Simulation/BOOT.md`, "## Budget selection rule (2026-09-28)")

  ⚠ 2026-09-21: was "satisfies the same criterion … in each layout", now `Independent`
  against reference mode, batched `Original` refused →
  HISTORY.md#batched-criterion-chronicle

- [x] 2026-09-20: the bias of the `Original` layout against the `Independent` layout is
      computed per printed quantity for every reference formulation, and the claims of
      "Known bias of the original's seeds" are checked by it
      (`tests/Harness.Tests/TwoSampleBiasTableTests` for the table,
      `OxidizerSizeDistributionBiasTests` for the section's closing absolute). Outcome:
      the five rows below (lagged against independent replicas, difference of means;
      the section's own table in `BOOT.md` until 2026-10-01) agree with the measurement
      in all twenty-five cells; between 42 and 71 further named quantities differ per
      formulation, so the bias reaches far past the five named here.

      | | HPEPA3 | inpt | P33 | PSAN02n | HMX |
      |---|---|---|---|---|---|
      | mass-mean pocket size `Dkarm43(1)` | +4.9 % | −5.8 % | +6.1 % | −10.9 % | −5.3 % |
      | pocket mass fraction `Zkarm` | +1.6 % | +3.1 % | +3.1 % | +17.4 % | +3.1 % |
      | bridges per particle | −8.9 % | +3.2 % | −9.2 % | +1.1 % | +0.7 % |
      | number-mean pocket size `Dkarm10` | −4.2 % | −3.6 % | −4.9 % | −6.7 % | +7.8 % |
      | mass-mean oxidizer size `Dok43all` | −0.05 % | 0.00 % | +0.04 % | 0.00 % | −0.02 % |

      Narrower than it reads: the four canonical-axis families are compared per index,
      which does not align the axis this comparison aligns, so their verdicts are
      evidence for neither side. Everything above holds without them.
- [x] 2026-09-19: batched mode with batch 1, budget 1 and continued streams equals
      reference mode bit for bit, result and every counter
      (`tests/Simulation.Tests/DegenerateConfigurationTests`; and, one level down, the
      first 1000 HPEPA3 particles in
      `tests/Execution.Tests/ReferenceModeEqualsContinuedBatchTests`, with the cycle-1
      refresh timing in `Qks1RefreshUnderCycle1Tests`).
- [x] 2026-10-01: CUDA equals the CPU accelerator within the tier table of
      `Execution.Tests` (`TierTableTests`, `Category=Long`, whole-cycle batches of the
      five reference formulations at attempt budget 8192; the tier figures are that
      node's own and measured).

  ⚠ 2026-10-01: was dated 2026-09-18 at budget 256, now re-measured at 8192 →
  `src/Execution/HISTORY.md#tier-table-budget-256`
- [x] 2026-09-18: the same configuration run twice, and with 1, 4 and 16 CPU threads,
      gives bit-identical results (`tests/Execution.Tests/DeterminismTests`,
      `Category=Long`).
- [x] 2026-09-19: `results.m` written by the port holds the same variable names, order
      and formats as the original's file for the same input, and parses back
      (`tests/Output.Tests`: `StructuralTests`, `FortranFormatTests`; the port's own
      file is parsed back by `tests/Harness`' parser in every criterion run). Narrower
      than it reads (`tests/Output.Tests`'s own dated note): array lengths tied to a
      running maximum and the printed digit shape of accumulated scalars are left to the
      statistical criterion instead.

  ⚠ 2026-09-20: was this criterion read at face value, now known to be narrower → HISTORY.md#output-criterion-narrower-than-it-reads
- [x] 2026-10-03: The `Original` accumulation kind reproduces the original's printed output
      (decided 2026-09-21; named before the kind was renamed, and kept because it is
      cited).

      Three links, each testing one thing, because one criterion cannot serve two kinds:

      1. **`Original` against the original's own replicas**: the rate criterion above;
         over sixteen seeds every cell that differs is declared (`pdoksmall(1)` and the
         "Dok < Dmin" count, `src/Particle`);
      2. **`Binary64` against `Original`**: they differ only in the generated
         accumulator and setup-plane sets, sixteen seeds, all five formulations
         (`tests/Harness/HISTORY.md#precision-kind-all-five-multi-seed`), and since
         2026-10-01 the per-cycle plane's: regenerated, the 160 `Original` rate runs
         moved only in cells that plane prints, no counter in any, and the 160
         `Binary64` runs are byte-identical (`tests/Fixtures/rate-table.json`);
      3. **batched and CUDA against sequential `Binary64`**, statistically, on the
         port's own replicas, plus the existing GPU-equals-CPU tiers (the batched
         criterion above).

  ⚠ 2026-09-24: was link 1 "every cell … at the single seed", held by a three-cell
  ratchet, now the rate criterion and the seed-0 snapshot →
  HISTORY.md#original-kind-criterion-chronicle

  ⚠ 2026-09-27: was "every cell that differs is declared" while PSAN02n's `da_coef` was
  not, now declared, not reproduced → HISTORY.md#link-1-da-coef-2026-09-27

  ⚠ 2026-10-01: was PSAN02n's `da_coef` among link 1's declared cells, now reproduced
  (`src/Statistics`, row 771–1175); the rate stays 18 of 160, the same runs failing.

  ⚠ 2026-10-02: was dated 2026-09-24; its links stand on evidence of 2026-10-01 (the
  rate criterion, the regenerated table, the batched criterion), so that is its date.

  ⚠ 2026-10-03: was dated 2026-10-01; link 2's table was regenerated again after the
  per-cycle plane followed the executable's listing, and holds on the new one: 147 of
  the 160 `Original` runs differ in bytes, the failing cells of every one unchanged,
  the 160 `Binary64` runs byte-identical (`src/Statistics/ACCEPTANCE.md`, A6).

- [x] 2026-09-23: the setup plane is reproduced under the `Original` precision kind, by
      controls frozen before any implementation and owned by `src/Statistics/BOOT.md` ("##
      Acceptance criteria", all six ticked the same day): `inpt` and p350 print the
      original's `epsdokfr` of 0.270E-07 and 0.263E-07, computed in advance, where `Binary64`
      prints 0; `Binary64`'s whole `results.m` is unchanged on all five reference
      formulations; HMX's `Dok43all` sits within ±0.002 % of the original at eight seeds.
      Neither the statistical criterion (band six to nine standard deviations wide) nor
      per-configuration token identity (sign-test null under both throwaway builds) can
      referee this, so neither was a gate.
- [x] 2026-10-03: the per-cycle plane is reproduced under the `Original` precision
      kind, by controls frozen before its code: out of sample on the archive (the 795
      rule 295 of 295, `epsx` on the binary32 grid 2016 of 2016, `DOKM` 293 of 293;
      `src/Statistics/BOOT.md`), and at seed 0 on all five formulations, where
      `da_coef`, the named `epsx`, `epsalldok`, `epsdok` and `epsdok43_n` cells print the
      original's value and no exactly printed cell or counter moves away from it
      (`tests/Simulation.Tests/BOOT.md`). Every site is generated, mapped and carried by
      one call checked four ways, and the formula twin reads the generated file, never
      the C# (`src/Statistics/BOOT.md`). `Binary64` is unchanged, bit for bit.

      ⚠ 2026-10-02: was "the per-cycle plane is reproduced", unqualified; now at the
      sites the executable's listing agrees with, these controls' sites among them.
      Where it contradicts the rules (798, 989–991, 1004–1008, 1150) the row 771–1175
      is `open`, `da_coef` matches on 19 of 22 archived formulations and path-identical
      pairs differ in 32 to 203 cells, cause not measured (`src/Statistics/BOOT.md`,
      "## Defects of the original"; `src/Statistics/ACCEPTANCE.md`).

      ⚠ 2026-10-03: was dated 2026-10-01 and qualified by the note above, the row
      `open`, `da_coef` 19 of 22, `DOKM` "under rule L 293 of 293"; now the listing at
      every site, the row `reproduced`, the oracle's two ratchets empty on 22 cases,
      `da_coef` 22 of 22 and the pairs' 551 cells attributed by class, not measured
      per cell → `src/Statistics/ACCEPTANCE.md` A1–A7. Rule L is retired; on the five
      reference formulations it and the listing's five-fold schedule give the same
      bits, so control (iii) stands as frozen and reads "`DOKM` 293 of 293".
- [ ] The print plane's cells and the attempt plane's REAL*4 inputs are the next
      phase, the owner's, and no document but `src/Statistics/ACCEPTANCE.md` A3 names
      it. C2 (`PathIdenticalPairsReportTests`) is brought down to the print plane's
      own cells once the attempt plane's REAL*4 inputs `Dr`, `Db` and `RK` follow the
      original; then the print plane's own cells follow it. Today's class attribution,
      15 print plane and 536 attempt plane of the 551 cells on six path-identical
      pairs, is the design's, not a measurement, and becomes a per-cell test. `RK` has
      no defect row: `src/Particle`'s row for lines 478, 533 and 1761–1776 covers `Dr`
      and `Db` at the `Dmin` decision only. Not started.
- [x] 2026-09-20: every parameter of the original's menu is an option with the original
      default; a `.dat` with `GSV ≠ 2` is rejected with a message
      (`tests/Cli.Tests`: `ReflectedFlagCoverageTests` compares the flag catalogue with
      the properties of `ModelParameters` and `SimulationOptions` both ways,
      `FlagRoundTripTests` drives every spelling of `FlagCatalog` into its own property,
      `ExitCodeTests.ExitTwoAMalformedInputFileNamesTheLine` for the refusal).
- [x] 2026-10-03: speed of batched CPU and CUDA against the original on HPEPA3 and
      HMX is re-measured at the shipped default budget 8192 and the shipped CUDA
      launch shape (groups of 64 threads, W5) in `tests/Benchmarks` ("##
      Figures"), commit 531d66c, clean tree, the machine quiet before, between and
      after, each row carrying its provenance and its budget, launch and attempt
      counts. Cycle 1, accepted particles/s, mean ± sample spread, n = 3: HPEPA3
      original 7376 ± 250, host16 48981 ± 759, CUDA 59727 ± 1480; HMX original 319
      ± 5, host16 2085 ± 8, CUDA 7100 ± 6. R0 is met (CUDA HMX 7100 against 0.8 ×
      host16 2085 = 1668) and CUDA leads host16 on both formulations (×1.22 on
      HPEPA3, ×3.41 on HMX). The launch shape moves no bit (tier table digit for
      digit, seed-0 CUDA hashes equal) and an A/B/A against its parent read g
      1.357, 1.356 (HMX) and 1.342, 1.308 (HPEPA3); a first attempt of that window
      was void (HMX ν 19.7 and 21.0 %), recorded with the second
      (`tests/Benchmarks/HISTORY.md#ship-window-2026-10-03`).

  ⚠ 2026-10-03: was the 2026-10-02 figures (W2, group automatic: HPEPA3 original 7275,
  host16 47685, CUDA 43503; HMX original 313, host16 2071, CUDA 5095) with HPEPA3 CUDA
  below host16, now the ship window → HISTORY.md#speed-criterion-2026-10-02

  ⚠ 2026-10-02: was the 2026-09-28 figures (HPEPA3 original 7399, host16 44211, CUDA
  35315; HMX original 318, host16 1822, CUDA 397) with "the collapse persists", now W2
  at budget 8192, R0 met → HISTORY.md#speed-criterion-2026-09-28

  ⚠ 2026-09-28, later: was "HPEPA3 CUDA also regressed … its cause is not measured",
  now bisected to the layout default → HISTORY.md#speed-criterion-2026-09-28

  ⚠ 2026-09-28: was the 2026-09-20 figures (HPEPA3 original 7041, host16 49346, CUDA
  47405; HMX original 321, host16 1954, CUDA 394), stale under `tests/Benchmarks`' own
  taboo after twelve commits touched `Attempt.cs`/`Engine.cs`/`Kernels.cs`, the
  precision kind among them, now re-measured (Decision B, Stage 0) →
  HISTORY.md#benchmark-figures-2026-09-20-superseded

  ⚠ 2026-09-20: was "the node does not exist yet", now it exists →
  HISTORY.md#benchmark-criterion-node-existed

- [x] 2026-09-26: the tree passes `protocol_lint` without errors (`python -X utf8
      tools/protocol-lint/protocol_lint.py . --exclude templates`); every warning is a
      §15 deviation its node declares, named in the linter's own output.

  ⚠ 2026-09-20: was ticked on a run with `--size` omitted, read as "2 warnings, this
  document and `tests/Harness`", now re-run 2026-09-26 →
  HISTORY.md#lint-criterion-reruns

- [x] 2026-09-20: the reflection checks are written for this stack and each is proven
      non-degenerate (AGENTS.md §13): `tests/Protocol.Tests`, eight facts —
      `SurfaceTests` against `PublicSurface.approved.txt`, `CoverageTests` (every
      exported type named in its node's `API.md`; every type in its node's namespace),
      `DeclarationTests` (every declaration under ✅ exists, type and member),
      `DependencyTests` (declared dependencies against the real ones, from signatures
      and from method bodies), and `InvariantTests` (no `float`/`Half`, no mutable
      static field, no CUDA type outside `Execution`). Each mutation is recorded in
      that node's `BOOT.md`.

      Its first run found two undeclared dependencies, since declared, and the namespace
      of `tests/Harness`, a declared deviation (`tests/Protocol.Tests/BOOT.md`).

⚠ 2026-09-24: cited test names renamed for CA1707, meaning unchanged, no criterion
re-verified and no date moved (`tests/test-renames-2026-09-24.txt`).

- [ ] The repository holds no code of the original (decided 2026-10-04): its five files
      outside it at `PROPSTRUCT_LEGACY_DIR`, identified by `tools/legacy/original.sha256`;
      `tools/legacy/scan.py` green on the exported public tree, and red on the private
      tree before the removal, on a 600-byte fragment of the listing excerpt and on a
      planted block of nine statements (date, the snapshot's commit, the scan's counts).
- [ ] The tree passes without the original: the fast set green on GitHub-hosted Windows
      with `PROPSTRUCT_LEGACY_DIR` unset (CI run id, commit), and `Category=Legacy` green
      on the reference machine with it set (count, date, commit); every Legacy fact red
      with the variable unset and the gate red on a copy of the original with one byte
      changed (date).
      Partly met 2026-10-06: the hosted run 37412082320 on `b76d3a3` is green with the
      variable unset, and `Category=Legacy` is green on the reference machine with it
      set, on that commit's tree: 19 facts of 19, 0 failed, 0 skipped (`Particle.Tests`
      1, `Output.Tests` 2, `Fixtures.Tests` 4, `Statistics.Tests` 12). The clauses on the
      red cases (variable unset, one byte changed) are not re-run here, so the tick waits.
- [ ] The packages of 0.1.0, from CI: `PropStruct`'s `lib/net10.0` holds exactly the
      assemblies of `src/` but `Cli`, each with its XML documentation, and its `.snupkg`
      their PDBs, every document of which resolves through SourceLink to the public
      commit; `samples/Quickstart` restores `PropStruct` from the job-local feed and runs;
      the tool installed from that feed writes, for the example, the `results.m` the
      source-built tool writes on the same runner, time line excluded (CI run id, commit).
      Not yet CI: the same checks ran locally on 2026-10-04 (`.github/scripts`,
      `check_packages.py all`: contents, every mutation red, the tool, the sample, both
      equal to the source builds); SourceLink needs the public commit and is stage S6's.
- [ ] The release 0.1.0: a dispatch rehearsal green through packing on the tagged commit,
      its GPU job on the self-hosted runner with CUDA bound and `PROPSTRUCT_REQUIRE_CUDA=1`;
      the tag `v0.1.0` naming that run and the Legacy run; the release run publishing both
      packages through Trusted Publishing after the owner's approval and creating the
      GitHub release with the notes of `CHANGELOG.md` (run ids, nuget.org versions).
- [x] 2026-10-05 — The original's authors are credited before the first push (decided
      2026-10-04): V. A. Babuk and A. A. Nizyaev, Baltic State Technical University
      "VOENMEH", with the reference of their paper of 2014, in place of the placeholders
      at the four sites that carried them: `NOTICE`, `README.md`, `docs/nuget/PropStruct.md`
      and `docs/nuget/PropStruct.Cli.md` (from the owner's source, 2026-10-05). `NOTICE`
      carries no statement about the original's formulations and outputs, by the owner's
      decision of 2026-10-05. The release check (`check_release.py notes`) reports no
      credit problem on the tree, held by `test_check_release.py` (`RealTreeTest`, the real
      tree's credits green; the synthetic cases of `CreditsTest` keep the check red on each
      placeholder).

      ⚠ 2026-10-05: was "the attribution and the right to publish are recorded": the
      authors, their affiliation and the owner's statement of the permission to publish the
      port, the 49 formulations and the original's outputs, at five sites, the fifth being
      `<PERMISSION>` in `NOTICE`; now the authors credited at four sites and the permission
      sentence withdrawn from `NOTICE` by the owner's decision of 2026-10-05, so that no
      permission is recorded in the tree. The original wording asked for a recorded
      permission; the owner decided the sentence stays out, and the criterion checks what
      the tree now says.
