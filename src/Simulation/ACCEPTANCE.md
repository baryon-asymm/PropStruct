# ACCEPTANCE.md — Simulation

## Acceptance criteria

- [x] The line map covers every executable line of 278–375 and 407–424; the list of
      lines is generated from the source. 2026-09-24:
      `tests/Simulation.Tests/LineMapCoverageTests.LineMapCoversEveryExecutableLineOfItsFortranRanges`.
- [x] Reference mode under `Original` fails the statistical criterion no more often
      than the original fails it against its own replicas, the rate criterion of root
      `ACCEPTANCE.md`. (2026-10-01, `tests/Harness.Tests/RateCriterionTests`, reading
      `tests/Fixtures/rate-table.json`.)

  ⚠ 2026-10-02: was "Reference mode satisfies the statistical criterion on the
  reference formulations", a single-run pass the original's own runs fail against
  their replicas (root `BOOT.md`, "the pass condition compares failure rates, not
  single runs", 2026-09-24). Found reconciling the open criteria.

  ⚠ 2026-10-02, the same day: was dated 2026-09-27, now 2026-10-01, the root's re-run
  of the same evidence (root `ACCEPTANCE.md`; AGENTS.md §6).
- [x] Batched mode on the CPU accelerator and on CUDA, whole-cycle batches at the
      default attempt budget, agrees with reference mode in the `Independent` layout
      under `Binary64` on the same formulations (root `ACCEPTANCE.md`, the batched
      criterion). (2026-10-01, `tests/Simulation.Tests/StatisticalCriterionTests`, the
      CPU and CUDA link-3 rows.)

  ⚠ 2026-10-02: was "satisfies it", the criterion against the original's replicas,
  which needs the `Original` precision kind that batched mode refuses (`BOOT.md`, "##
  Invariants"); the root reformulated its own criterion so on 2026-09-21. Found
  reconciling the open criteria.
- [x] The degenerate batched configuration equals reference mode bit for bit.
      (2026-09-19, `DegenerateConfigurationTests`, a constructed HPEPA3 with `N = 24`,
      `KXX = 2`: every result record, attempts, launches; proven by mutation,
      `tests/Simulation.Tests/BOOT.md`, "## Mutations".)

      ⚠ 2026-09-21: was proven under `Streams = StreamLayout.Original`, now under
      `Independent` (the degenerate configuration is `Batched` mode, which `Original`
      refuses); the claim proven is unchanged →
      HISTORY.md#degenerate-criterion-fixture-layout-moved
- [x] A run whose particle passes the attempt cap throws `SimulationFailedException`
      with the status. (2026-09-19,
      `SimulatorOptionsTests.CreateAttemptCapExceededThrowsSimulationFailedExceptionWithStatus`
      and `FailureStatusTests.EveryBatchFailureStatusMapsToTheRunStatusOfTheSameName`;
      proven by mutation, same place.)
- [x] `SimulationOptions.Precision` reaches `ModelSetup.Kind` unchanged and is
      recorded in `RunDiagnostics.Precision`; `Binary64`, the default and the value
      every caller before this option existed got by having no field to set, reproduces
      the exact bits `DegenerateConfigurationTests` and `Hpepa3CounterTests` already
      measured, unmoved. (2026-09-21, `tests/Simulation.Tests/PrecisionKindTests`.)
- [x] `Original` accumulation with batched mode — a plain batch and the degenerate
      `BatchSize == 1`/`ContinuedStreams` configuration alike — is refused with
      `SimulationFailedException(RunStatus.InvalidSetup, …)` by `Simulator.Create`,
      before any setup or accelerator work exists, the message naming both halves of
      the conflict; removing the check from `SimulationOptionsValidator` turns the
      assertion red. (2026-09-21, re-verified 2026-09-26 against the moved check —
      architecture audit finding R1: `tests/Simulation.Tests/PrecisionKindTests`,
      `SimulatorOptionsTests`.) `Execution.Engine` keeps its own, narrower mechanism
      guard as a backstop for a caller that reaches it directly (`src/Execution/BOOT.md`).
- [x] `Original` stream layout with batched mode — the same two configurations — is
      refused the same way, by the same validator, the message naming both halves of
      the conflict; `Independent` layout is unaffected in both modes. (2026-09-21,
      re-verified 2026-09-26 against the moved check: `tests/Simulation.Tests/StreamLayoutTests`.)
      Unlike the precision row, `Execution.Engine` keeps no refusal of its own for this
      rule any more: a caller reaching it directly is no longer refused for the layout
      (withdrawn, R1; `src/Execution/BOOT.md`'s own 2026-09-26 note).
- [x] The default configuration is coherent again: `Simulator.Create(new
      SimulationOptions { Parameters = ... })` — `Mode = Batched`, `Streams =
      Independent`, every other option at its own default — runs a small formulation to
      completion without throwing, so a bare `propstruct run <file.dat>` works.
      (2026-09-21, `tests/Simulation.Tests/StreamLayoutTests`.)
- [x] 2026-09-26 (architecture audit finding R1): a failed `Statistics.Setup.Prepare`
      carries a public `SetupFailureReason` on `SimulationFailedException` (the same
      mirror pattern as `PrecisionKind`/`StreamLayout`), so a public caller reads which
      precondition failed without `Statistics.SetupStatus`, which stays internal
      (`SetupFailureReason.cs`; `tests/Simulation.Tests/SimulatorOptionsTests`;
      `PublicSurface.approved.txt` moved the same commit).

### Corrected model (designed 2026-10-18, `BOOT.md`, "## Corrected model")

The statistical and link-3 rows above are the `Original` model's; the `Corrected` model
has no external reference (root `BOOT.md`, "Model kind is an option of every run").

- [ ] Surface: `ModelKind` has the explicit values `Original = 0` and `Corrected = 1`,
      `SimulationOptions.Model` defaults to `Original`, `RunDiagnostics.Model` and
      `StoredSetup.Ak1`/`Ak2`/`Ak4` are body init properties and the 0.1.0 positional
      constructors are unchanged; `PublicSurface.approved.txt` moves in the same commit.
- [ ] `Original` unchanged: the seed-zero guard of `tests/Simulation.Tests` (`results.m`
      and full-precision JSON, every case) is green and unedited after every commit that
      adds `Corrected` code; an `Original` `results.m` holds no `% Model kind` line and
      its JSON no `Model`, `Ak1`, `Ak2` or `Ak4`. Red once: mapping `Original` to
      `CorrectionSet.All` in `Simulator.Run` turns the guard red.
- [ ] The mapping, right once on a known answer: `Corrected` on P33 prints `Dok43sd`
      112.66 µm where `Original` prints 75.45 µm (`DoksdConsistent`,
      `src/Statistics`); red once: mapping `Corrected` to `CorrectionSet.None` gives
      75.45 µm.
- [ ] The refusal: `Model = Corrected` with `Precision = PrecisionKind.Original`, in
      either mode, throws `SimulationFailedException` with `RunStatus.InvalidSetup` from
      `Simulator.Create`, `SetupFailure` `null`, before any setup or accelerator work,
      the message naming both halves; the two batched refusals keep their messages word
      for word; removing the check turns the assertion red. `Corrected` with
      `Binary64` runs a small formulation to completion in both modes and both layouts.
- [ ] The seam: `Corrections` set with `Model = Original`, or holding a bit outside
      `All`, throws `ArgumentException` before any work; `Corrections = None` with
      `Corrected` gives the `Original` run's `SimulationResult`, `Diagnostics.Model`
      normalised, bit for bit (negative control); a single-flag seam run differs from
      `Original` on a case where that flag is visible (positive control, the flag's
      evidence case in its owning node).
- [ ] The degenerate batched configuration equals reference mode bit for bit under
      `Corrected` too (`DegenerateConfigurationTests`' constructed case with
      `Model = Corrected`); a `Corrected` run repeated gives the same result, elapsed
      time aside.
- [ ] Labelling: a `Corrected` run's `RunDiagnostics.Model` is `Corrected`, its
      `StoredSetup.Ak1`/`Ak2`/`Ak4` equal its `ModelSetup`'s, and its `results.m` ends
      its footer with the model line of `BOOT.md`, "## Corrected model", verbatim.
- [ ] Budgets under `Corrected`: the method of `BOOT.md`, "## Budget measurement", run
      under `Corrected` on the five reference formulations, its maxima recorded there,
      and no default below 100 times a `Corrected` maximum.
