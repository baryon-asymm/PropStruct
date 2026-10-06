# BOOT.md — Cli.Tests

## Purpose

The definition of what "`Cli` is ready" means.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | the parser: every flag, both spellings (`--flag value`, `--flag=value`), switches, `--`, repetition, unknown flag, missing value, unparsable number, a single-dash typo in flag position | the parse result and the exact diagnostic text | ✅ 2026-09-20, `ParserTests` (35 cases) |
| L0 | every property of `ModelParameters` and of `SimulationOptions` is reachable as a flag and every flag has a property, both ways; `defaults` prints exactly `ModelParameters.Default` | lists produced by reflection over the two records' properties inside the test, with the deliberately flagless properties named and asserted to be few | ✅ 2026-09-20, `ReflectedFlagCoverageTests` (6 cases) |
| L0 | every flag and every one of its spellings (all 36, `--precision` added 2026-09-21 as `--accumulation`, renamed 2026-09-23) parses a sample value into exactly its own property, leaving every other flagged property at its record's default | `FlagCatalog`'s own spelling lists, driven by `FlagKind`-keyed sample values so an unmapped kind turns the level red | ✅ 2026-09-21, `FlagRoundTripTests` (40 cases: 36 spellings + 2 path flags + `Quiet` + the kind-coverage fact) |
| L0 | every member of the diagnostics enumeration is produced by some input, and by that input alone | the enumeration itself, enumerated by the test; "alone" is `Assert.Single` over the whole error list, not `Assert.Contains` | ✅ 2026-09-20, `DiagnosticCoverageTests` (13 cases) |
| L0 | `--help`, `--help` per verb, `--version`, unknown verb, no verb, a flag given twice, `--switch=value`, a value beginning with `-` (positively: it reaches the file lookup, named in the message, not rejected as a flag), a path after `--` (positively: the dash-led name reaches the file lookup intact) | the exact texts and exit codes | ✅ 2026-09-20, `ProgramTextAndExitCodeTests` (11 cases) |
| L0 | a length reaches `ModelParameters` as written (`--dmin 10e-6` ≠ `--dmin 10`, the first equal to the default) | `Input`'s `Length.AsWritten` | ✅ 2026-09-20, `LengthAsWrittenTests` (3 cases) |
| L0 | numbers parse the same under a decimal-comma culture | a cloned invariant culture with the separator overwritten (as `Output.Tests` does; the build sets `InvariantGlobalization`), and `DefaultsReport`/`DevicesReport` build their own `StringWriter` with the invariant culture explicitly rather than relying on that build switch alone | ✅ 2026-09-20, `CultureInvarianceTests` (2 cases) |
| L0/L1 | `defaults` prints exactly `DefaultsReport.Render`'s own text: an alias next to its canonical flag, a dash where there is none, and the verb prints exactly what `Render` produces | `DefaultsReport.Render(ModelParameters.Default)`, and the verb driven through `Program.Run` | ✅ 2026-09-20, `DefaultsCommandTests` (3 cases) |
| L0 | `AtomicFile.Write`'s own mechanism: the target is untouched until the callback returns, a failing callback leaves neither a partial target nor a stray temporary file, a failed move names the target path in its own rethrown message | direct calls to `AtomicFile.Write`, including one with the target locked open to force the move to fail | ✅ 2026-09-20, `AtomicFileTests` (3 cases) |
| L0/L1 | the tool never reads the console | a source-level scan of every file under `src/Cli` for `Console.Read*`/`Console.In`/`Console.OpenStandardInput` (the actual proof; see the ⚠ below on why the closed-stdin process run alone cannot be it) | ✅ 2026-09-20, `SourceCodeTests` (1 case) |
| L1 | `Program.BudgetFlagOf` (BOOT.md's own "a failed status prints ... the flag that raises its budget") has an entry for every `RunStatus` that reaches it | `RunStatus`'s own members, enumerated by the test, excluding the three statuses `ReportFailedRun` handles before ever consulting the table | ✅ 2026-09-20, `RunStatusMappingTests` (1 case) |
| L1 | each exit code (0, 1, 2, 3, 4, 130) is produced, each by its own cause, and the whole dispatch (not just `run`) is wrapped by the same mapping | the tool driven in-process throughout (`Program.Run`); 130 through the cancellation token, both before dispatch starts (`devices`) and mid-`run`, since neither used to reach it; exit 4 is driven end to end through `Program.Run` itself with a `null` argv element, the one reachable way to make the real parser throw, rather than by calling `Program.MapExceptionToExitCode` directly | ✅ 2026-09-20, `ExitCodeTests` (8 cases) |
| L1 | an out-of-range parameter is refused by the library, not by the tool: `InvalidSetup` → exit 2; `--precision original` with batched mode is the same route, message naming both halves | a run with a value the model rejects; a run combining the two options | ✅ 2026-09-21, `InvalidSetupTests` (2 cases: `DminFarBeyondTheFormulationsFractionBoundsExitsTwoAsInvalidSetup`, `PrecisionOriginalWithBatchedModeExitsTwoNamingBothHalves`) |
| L1 | the content of a successful `run`, not just its existence: `--json`'s file agrees with the summary line's own attempt count, the summary line matches its documented shape, at least one progress line matches its documented shape | `ResultsJson.Read` and two regular expressions built from BOOT.md's own documented shapes | ✅ 2026-09-20, `RunOutputContentTests` (3 cases) |
| L1 | `run` on a reference formulation writes a `results.m` byte-identical to the library's for the same options, time line excluded | a library run of the same formulation and options, compared byte for byte (not line by line: the criterion is bytes) | ✅ 2026-09-20, `ResultsMByteIdenticalTests` (1 case, `Category=Long`, ~10 s) |
| L1 | `devices` lists what `AcceleratorProbe.Discover` reports, and still agrees with the probe when the CUDA kill switch is set | the probe's own output; the kill switch is set on a spawned child process, never on the shared test process (`src/Execution/API.md`'s own ⚠ 2026-09-18 records that mistake against itself) | ✅ 2026-09-20, `DevicesCommandTests` (2 cases) |
| L1 | with stdin closed, every verb does not hang | a process run with a closed input stream (this proves "does not hang", not "never reads": a closed stdin makes `Console.ReadLine` return `null` rather than block, so it cannot tell that apart from "reads once and moves on" — `SourceCodeTests` above is the actual "never reads" proof) | ✅ 2026-09-20, `StdinClosedTests` (5 cases) |
| L0 | `Program.IsProcessFatal`, the `when` filter on `Dispatch`'s/`ExecuteRun`'s catch-alls (CA1031, owner's decision 2026-09-24), reports true for `OutOfMemoryException`/`InsufficientExecutionStackException` and a type derived from one of them, false for a representative sample of the exception types the Errors table already maps | the predicate called directly, never a real fatal exception driven through `Program.Run` (which would tear down the test host itself) | ✅ 2026-09-24, `ProcessFatalExceptionTests` (9 cases) |

⚠ 2026-09-20: the exit-code row first read "one process run for 0 and 2", written before the coding
session. The implementation drives every exit code in-process through `Program.Run` (including 0 and 2);
a real process is used only for the stdin-closed and devices-kill-switch rows, where the thing under test
is the process itself.

⚠ 2026-09-20 (review): the L0 "every flag, both spellings" row was ticked while 17 of the 35 spellings
were parsed by no test at all, and the only reflected check compared name lists, not values:
`RunOptionsBuilder`'s hand-written value-to-property chain could have two of its lines swapped and every
test would have stayed green. `FlagRoundTripTests` is the fix — proven non-degenerate by swapping the
targets of `--eps`/`--eps-dok` in `RunOptionsBuilder.BuildModelParameters` (a scratch edit of this node's
own file) and observing exactly those two spellings' cases fail, nothing else; reverted, green again.

## Invariants

- The tool is driven in-process through `Cli`'s internal entry point; process runs are
  used only where the thing under test is the process itself (exit codes, stdin).
- Expected `results.m` content comes from a library run or from `tests/Fixtures`, never
  typed into a test.
- The flag list and the parameter list are generated inside the test by reflection, so
  that a parameter added to `ModelParameters` without a flag turns the level red
  (AGENTS.md §6, the "all" quantifier).
- A diagnostic is asserted by its text, not by the exit code alone.

## Dependencies

- [Cli](../../src/Cli/API.md) — what is being checked.
- [Simulation](../../src/Simulation/API.md) — the library run to compare against.
- [Input](../../src/Input/API.md) — `ModelParameters`, `Length`, the reference formulations.
- [Output](../../src/Output/API.md) — the library's own `results.m`.
- [Execution](../../src/Execution/API.md) — what `devices` must agree with.
- [Harness](../Harness/API.md) — repository paths, the `results.m` comparison.
- [Fixtures](../Fixtures/API.md) — the reference formulations.

Outside the tree: xunit.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Part of the default test command; a level that runs a whole reference formulation is
  marked `Category=Long`, measured before deciding.
- A test that runs the tool as a process builds it once and reuses the binary; no test
  writes into the repository working tree — outputs go to a temporary directory, and
  the default-path level sets the working directory of the process instead of writing
  beside the sources.

## Acceptance criteria

- [x] 2026-09-21: every row of the levels table is green, with a date and the name of
      the test (table above).

  ⚠ 2026-09-21: was ticked 2026-09-20, then unticked the same day the `--accumulation`
  flag and its `InvalidSetupTests` case were added, before `src/Simulation`'s own
  implementation of `AccumulationKind`/`SimulationOptions.Accumulation` existed (only
  its `API.md` had been published, so `dotnet build` failed on `error CS0246`, one node
  upstream of this one — AGENTS.md §3, this node's coding session does not write that
  neighbour's code). Re-ticked the same day once `src/Simulation`'s coding session
  merged its implementation onto this branch (an agent's working branch,
  decided with the root): `dotnet build PropStruct.sln` succeeds, and every row above
  is green, including the two new ones, confirmed by a full run of this project
  (135 of 135 cases) and of `PropStruct.sln`'s fast set (`Category!=Long`).
- [x] 2026-09-20: the reflected parameter list is shown to work: `FlagCatalog`'s own
      `--dmin` entry was commented out in a scratch edit of `src/Cli/FlagCatalog.cs`
      (this node's own file, not a neighbour's — `ModelParameters` itself was not
      touched), and `ReflectedFlagCoverageTests.EveryModelParametersPropertyHasExactlyOneFlag`,
      `EveryModelParameterFlagHasAMenuNumberFromOneToFourteen` and
      `DefaultsReportMatchesModelParametersDefaultPropertyByProperty` all failed (13
      flags against 14 properties). Reverted; green again.
- [x] 2026-09-20: the exit-code level is shown non-degenerate: each of the six exit
      codes was mutated to a wrong sibling value in `src/Cli/Program.cs`, one at a
      time, each reverted before the next, and each mutation turned exactly its own
      `ExitCodeTests`/`InvalidSetupTests` case red and no other:
      1. exit 0 — `ExecuteRun`'s success return, mutated to `ExitCode.RunFailed`:
         `ExitZeroASuccessfulRunWritesResultsMAndPrintsTheSummary` and
         `ExitZeroWithQuietPrintsNothingToEitherStream` failed (expected 0, got 1).
      2. exit 1 — `ReportFailedRun`'s `default` case, mutated to
         `ExitCode.InvalidArguments`: `ExitOneAFailedRunStatusNamesTheStatusAndItsBudgetFlag`
         failed (expected 1, got 2). Missing from the first version of this record
         (reviewed 2026-09-20); done now.
      3. exit 2 — `ReportFailedRun`'s `InvalidSetup` branch, mutated to
         `ExitCode.InfrastructureError`: `InvalidSetupTests.DminFarBeyondTheFormulationsFractionBoundsExitsTwoAsInvalidSetup`
         failed (expected 2, got 3).
      4. exit 3 — `ExecuteRun`'s missing-output-directory check, mutated to
         `ExitCode.InvalidArguments`: `ExitThreeAMissingOutputDirectoryNamesTheFullPath`
         failed (expected 3, got 2).
      5. exit 4 — `MapExceptionToExitCode`'s `default` case, mutated to
         `ExitCode.RunFailed`: `ExitFourAnUnhandledExceptionIsCaughtByTheRealDispatchWiring`
         failed (expected 4, got 1).
      6. exit 130 — `MapExceptionToExitCode`'s `OperationCanceledException` case,
         mutated to `ExitCode.UnhandledException`:
         `DevicesCancelledBeforeDispatchReturns130Too` and
         `Exit130CancelledThroughTheTokenPrintsInterruptedAndWritesNoOutput` both
         failed (expected 130, got 4).

      Re-run in full 2026-09-20 against the restructured `Program.cs` (`Dispatch`,
      `ExecuteRun`, `MapReadFailureToExitCode`, `MapExceptionToExitCode`,
      `ReportFailedRun`) after the dispatch-wrapping fix of that same review; the
      method names above are the current ones, not the pre-review ones the first
      version of this record named.

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.

## Taboos

- No typed expected value that exists in a fixture or in a library run.
- No test that waits on the console.
- No assertion on the wall clock beyond "the summary line exists".
