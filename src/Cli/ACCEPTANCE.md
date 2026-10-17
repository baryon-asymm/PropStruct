# ACCEPTANCE.md — Cli

## Acceptance criteria

- [x] 2026-09-20: every property of `ModelParameters` **and of `SimulationOptions`** has
      a flag, and every flag has a property: the two lists are produced by reflection
      inside the test and compared both ways, never typed
      (`tests/Cli.Tests/ReflectedFlagCoverageTests`). `SimulationOptions.Parameters` is
      the one property deliberately without a flag (`FlagCatalog.DeliberatelyUnflagged`,
      asserted to hold exactly that one entry with its reason).
- [x] 2026-09-20: `propstruct defaults` prints exactly `ModelParameters.Default`, compared
      property by property by the same reflected list, canonical name and aliases in two
      columns (`tests/Cli.Tests/ReflectedFlagCoverageTests.DefaultsReportMatchesModelParametersDefaultPropertyByProperty`).
- [x] 2026-09-20: each of the six exit codes is produced by a test, each by its own
      cause; 130 is produced in-process through the cancellation token
      (`tests/Cli.Tests/ExitCodeTests`).
- [x] 2026-09-20: every diagnostic of the enumeration is produced by a test: the test
      enumerates the enumeration, so a new diagnostic without a test turns it red
      (`tests/Cli.Tests/DiagnosticCoverageTests.EveryDiagnosticEnumMemberHasAProducingCommandLine`).
- [x] 2026-09-20: the tool performs no semantic validation of its own: a value the
      library refuses comes back as `InvalidSetup` and exits 2, shown by a test that
      passes an out-of-range parameter — `--dmin 1000` on the `inpt`-shaped baseline
      formulation, far beyond its largest fraction bound
      (`tests/Cli.Tests/InvalidSetupTests`).
- [x] 2026-09-20: `--help`, `--help` after each verb and `--version` print to stdout and
      exit 0 (`tests/Cli.Tests/ProgramTextAndExitCodeTests`).
- [x] 2026-09-20: `propstruct run` on a reference formulation (`inpt`) writes a
      `results.m` byte-identical to the one the library writes for the same options
      (`Output`), time line excluded (`tests/Cli.Tests/ResultsMByteIdenticalTests`,
      `Category=Long`).
- [x] 2026-09-20: a `.dat` with `GSV ≠ 2` is refused with a message naming the line,
      exit 2 (`tests/Cli.Tests/ExitCodeTests.ExitTwoAMalformedInputFileNamesTheLine`,
      against `tests/Fixtures/cases/input/bad-gsv.dat`).
- [x] 2026-09-20: `--dmin 10e-6` and `--dmin 10` produce different `ModelParameters`,
      and the first equals the default: asserted on `Length.AsWritten`
      (`tests/Cli.Tests/LengthAsWrittenTests`). (`10e-6` and `0.00001` are the same
      run: the flag carries the parsed `double`, not the text — same file,
      `TenEMinusSixAndZeroPointZeroZeroZeroZeroOneAreTheSameRun`.)
- [x] 2026-09-20: every diagnostic of the parser is asserted by text, not by exit code
      alone (`tests/Cli.Tests/DiagnosticCoverageTests`, `tests/Cli.Tests/ParserTests`).
- [x] 2026-09-20: the tool is proven not to read the console: with stdin closed, every
      verb still finishes (`tests/Cli.Tests/StdinClosedTests`, a real process run with
      its standard input closed); the claim itself is a source-level scan for no
      reachable `Console.Read*`/`Console.In` under `src/Cli`
      (`tests/Cli.Tests/SourceCodeTests.NoConsoleReadAppearsUnderSrcCli`) — a closed
      stdin alone makes `Console.ReadLine` return `null` rather than block, so it
      cannot by itself tell "never reads" from "reads once and moves on".
- [x] 2026-09-20: Escalation (AGENTS.md §11, found while choosing an `InvalidSetup`-
      triggering value for `tests/Cli.Tests/InvalidSetupTests`) raised and closed by the
      owning node: `ModelParameters.Default with { NnMax = 0.0 }` now fails the setup
      with `RunStatus.InvalidSetup`, so the tool exits 2
      (`src/Statistics/BOOT.md`, `SetupStatus.InvalidNnWindow`).

  ⚠ 2026-09-20: this item first claimed that such a run "crashes the whole host
      process", and called it a violation of the root invariant "Failures are values".
      Measured by the owning node: nothing crashes. `NnMax ≤ 0` makes the post-loop
      acceptance test fail for every finite `nn`, so no particle is ever accepted and
      the run spends the whole per-particle attempt cap — four minutes on one particle,
      then a correct `AttemptCapExceeded`. The invariant held; what failed was the
      time it took to say so, and under a test timeout that is indistinguishable from a
      dead process. The observation was right, its diagnosis was not: an escalation
      states what was seen, and this one stated a cause it had not measured.
- [x] 2026-09-21: `--precision` reaches `SimulationOptions.Precision` and no other
      property, its default (`binary64`, spelled `double` until 2026-09-24) leaves every existing invocation unchanged, and
      `--precision original` combined with batched mode exits 2 naming both halves of
      the conflict, through the existing `InvalidSetup` route with no new branch
      (`tests/Cli.Tests/ReflectedFlagCoverageTests`, `FlagRoundTripTests` — both already
      generic over `FlagCatalog`, so the new flag needed no new test code there —
      and `tests/Cli.Tests/InvalidSetupTests.PrecisionOriginalWithBatchedModeExitsTwoNamingBothHalves`).
      Run against `src/Simulation`'s own finished implementation once it merged onto
      this branch (an agent's working branch): 135 of 135 `tests/Cli.Tests`
      cases green, `PropStruct.sln`'s fast set (`Category!=Long`) green throughout.

⚠ 2026-09-24: cited test names renamed for CA1707, meaning unchanged, no criterion
re-verified and no date moved (`tests/test-renames-2026-09-24.txt`).

### Corrected model (designed 2026-10-18, `BOOT.md`, "## Corrected model")

- [ ] `--model` reaches `SimulationOptions.Model` and no other property, and every
      public property still has its flag (`ReflectedFlagCoverageTests`,
      `FlagRoundTripTests`, generic over `FlagCatalog`); `--model original` and no
      `--model` give the same `SimulationOptions`; `ResultsMByteIdenticalTests` stays
      green unedited. Red once: the catalog entry removed turns the coverage test red.
- [ ] `--model corrected --precision original` exits 2 with the library's message
      naming both halves, through the existing `InvalidSetup` branch; `--model
      corrected` alone runs a small formulation to exit 0 and its `results.m` carries
      the model footer line; an unknown value (`--model fixed`) exits 2 naming the
      flag.
- [ ] The summary line under `Original` is byte for byte the line before the option
      (asserted against a fixed expected text built from the same diagnostics), and
      under `Corrected` it differs only by ` model=Corrected` after `layout=…`.
- [ ] The usage text names `--model original|corrected` and its one sentence;
      `--help` after `run` prints it.
