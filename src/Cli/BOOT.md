# BOOT.md — Cli

## Purpose

The `propstruct` command-line tool: files in, files out, over the library. It replaces
the original's interactive console menu with arguments, so that a run is a line in a
script instead of fourteen answers typed by hand.

Three verbs: `run` (read a formulation, simulate, write the results), `devices` (what
accelerators this machine offers) and `defaults` (the fourteen menu parameters with the
original's values). The node owns argument parsing, the mapping of arguments onto
`SimulationOptions` and `ModelParameters`, the exit codes and the progress lines; it
owns no model arithmetic and no output format.

## Invariants

- **Every menu parameter of the original is reachable as a flag.** The list is not typed
  into a document but read from `ModelParameters`' own properties by the test of
  `tests/Cli.Tests` (AGENTS.md §6, the "all" quantifier).
- **The tool never reads the console.** No prompt, no `Console.ReadLine`, no wait for a
  key. A missing value is an error, never a question.
- **Sizes keep the form in which they are written.** A length flag hands its text to
  `Input`'s `Length` unchanged, so `--dmin 10e-6` and `--dmin 10` are two different
  runs: the first is the original's default written as metres, the second is the same
  physical size written as micrometres, and `Statistics` derives different binary32
  echoes from the two (`src/Input/BOOT.md`, "Lengths keep the value as written"). The
  tool neither converts nor normalizes.
- **Defaults are the original's.** Every parameter not named on the command line is
  `ModelParameters.Default`'s; every run option not named is `SimulationOptions`' own
  default.
- **One run, one exit code**: 0 ok, 1 the run failed with a status, 2 invalid arguments
  or invalid input, 3 accelerator or infrastructure error, 4 an unhandled exception (a
  defect of the tool, not a status of the run), 130 interrupted.
- **The tool validates form, never meaning.** It checks that a flag exists, that its
  value parses and that the verb is known; it never checks a parameter's range or a
  combination of options against the model. Those come back from the library as
  `InvalidSetup`, so no second rule set is born here.

  ⚠ 2026-09-20 (review): this read unconditionally, but the grammar itself makes two
  checks that are a range and a combination, not a value's shape: the budget-style
  flags (`--batch`, `--attempts-per-launch`, `--max-attempts-per-particle`,
  `--neighbour-budget`, `--record-budget-bytes`) parse only as a *positive* integer
  (`FlagKind.PositiveInteger`/`PositiveLong`, `src/Cli/FlagKind.cs`), and
  `--continued-streams` is accepted only together with `--mode batched --batch 1`
  (`CommandLine.ParseRun`'s own combination check, matching `SimulationOptions`'
  documented meaning for it). Both stay: a budget's grammar has always been "a
  positive integer" (BOOT.md's own design session, the quoted diagnostic "`--batch`
  expects a positive integer, got `x`"), and rejecting `--continued-streams` without
  its one valid combination at parse time, rather than a run failing an hour in, is
  the whole reason the flag has a documented precondition at all. What the invariant
  actually guarantees is narrower: no *model* parameter's range and no combination of
  *model* parameters is checked here — `SimulationOptions`' own run-control grammar
  is not the model, and a value it still refuses (any `ArgumentException` from
  `Simulator.Create`, `src/Simulation/API.md`) exits 2 like any other invalid
  argument, not 4.

  ⚠ 2026-09-26 (architecture audit finding R1): `CommandLine.ParseRun`'s own
  `--continued-streams` combination check used to retype `Simulator.Create`'s own
  condition verbatim, so the rule lived in two places at once. It now calls
  `Simulation.SimulationOptionsValidator.IsContinuedStreamsCombinationInvalid`
  (`InternalsVisibleTo`, `src/Simulation/PropStruct.Simulation.csproj`), so the rule is
  typed once and this node keeps only its own diagnostic and exit code around it.
- **Nothing depends on the wall clock** except the time line `Output` writes into
  `results.m`. Progress lines carry cycle numbers and counts, not timestamps.

## Dependencies

- [Simulation](../Simulation/API.md) — the run, its options and its result.
- [Output](../Output/API.md) — writing `results.m` and JSON.
- [Input](../Input/API.md) — reading formulations, `ModelParameters` and `Length`.
- [Execution](../Execution/API.md) — `AcceleratorProbe` for `devices`.

Outside the tree: nothing beyond the .NET base class library. The argument parser is
this node's own (see "Design decisions").

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- No model logic and no formatting logic of its own: everything printed about a result
  comes from `Output`, everything computed comes from `Simulation`.
- One assembly, `PropStruct.Cli`, whose assembly name is `propstruct` (the tool command
  of the root `BOOT.md`, "Language and build").
- Parsing is culture-invariant, as `Input`'s is: numbers are read with
  `CultureInfo.InvariantCulture`, never with the current culture.
- `Program` is a shell: it parses, runs and maps the outcome to an exit code. Everything
  it does is reachable in-process through the node's internal types, so that
  `tests/Cli.Tests` can drive the whole tool without starting a process
  (`InternalsVisibleTo("PropStruct.Cli.Tests")`).
- **`Program.Dispatch`'s and `Program.ExecuteRun`'s `catch (Exception ex)` narrow by a
  `when` filter, `!IsProcessFatal(ex)`** (CA1031, owner's decision 2026-09-24, resolved
  the same day in the cross-node analyzer task). Exit code 4 ("an unhandled exception
  ... a defect of the tool", `## Invariants` above) is this node's own value-not-exception
  boundary for whatever the library does not name as one of its documented failures —
  root BOOT.md's own "Failures are values" applied one level up, at the process edge
  instead of the numerical core — for every exception except the two named
  `IsProcessFatal` excludes, `OutOfMemoryException` and `InsufficientExecutionStackException`:
  mapping either to a return value would claim the process can still meaningfully return
  one. Those two now propagate out of `Dispatch`/`ExecuteRun` uncaught, same as they
  always have out of every other node; every other exception keeps exactly its previous
  mapping and exit code.

  ⚠ 2026-09-24, later the same day: this paragraph first declared the catch-alls a
  deviation, reasoning that narrowing to named types and moving the rest to
  `AppDomain.CurrentDomain.UnhandledException` could not preserve
  `ExitCodeTests.ExitFourAnUnhandledExceptionIsCaughtByTheRealDispatchWiring`'s
  in-process, synchronous exit-4 assertion — true of that alternative, but a `when`
  filter needed no such rewrite: the catch clause, and every exception type it maps,
  is unchanged; only the two named process-fatal types now fall through it. Found on
  rereading CA1031's own message a second time, which offers the filter as an
  alternative to narrowing, not only narrowing itself.

## Design decisions (2026-09-20)

These close the three open questions this document carried from the tree's first
decomposition.

### The parser is this node's own, hand-written

`System.CommandLine` is not taken. The surface needed is three verbs, about thirty
flags, no completion, no help generation beyond one static text: a hand-written parser
is smaller than the wiring of a library, adds no package to a tree whose only outside
dependencies are the runtime, ILGPU and the test tools, and — the reason that decides
it — lets every diagnostic be exact and testable ("unknown option `--dmim`",
"`--batch` expects a positive integer, got `x`") instead of a library's phrasing that
changes with its version.

Shape: `CommandLine.Parse(string[] args)` returns either a parsed command or a list of
errors; it never writes to the console and never throws for bad input. `Program` prints
and chooses the exit code. The parser accepts `--flag value` and `--flag=value`,
switches without a value, `--` to end the options, and rejects everything else. Its
rules, in full:

- a flag given twice is an error ("`--dmin` given twice"), never last-wins;
- the token after a value-taking flag is its value verbatim, even when it begins with
  `-` (`--eta -1` is minus one, not an unknown flag);
- `--switch=value` is an error;
- the verb comes first; an unknown verb, or none, prints the usage text and exits 2;
- `run` takes exactly one positional argument, the `.dat` path; after `--` a path may
  begin with `-`;
- `--help`/`-h`, alone or after a verb, prints that verb's usage to stdout and exits 0;
  `--version` prints the assembly's informational version and exits 0;
- every number is parsed with `CultureInfo.InvariantCulture` passed explicitly, and the
  tool never assigns `CurrentCulture`; a Fortran exponent form (`10d-6`) is refused with
  a message naming the flag;
- every diagnostic is a member of one enumeration of diagnostics, so that
  `tests/Cli.Tests` can enumerate them instead of trusting a typed list.

### Flag names are the original's own names, with aliases

The canonical name of a model parameter is the name the original's source and the header
of `results.m` give the variable; where the menu labels the item differently, that label
is an alias, and so is one descriptive name for a reader who never saw the menu. Every
spelling is accepted; `defaults` prints the canonical name **and** its aliases in two
columns, because that print is the only place a user meets the `k5`/`alfa` trap below.

| # | `ModelParameters` | Canonical flag | Alias | Kind |
|---|---|---|---|---|
| 1 | `Dmin` | `--dmin` | — | length |
| 2 | `CellSize` | `--di` | `--cell-size` | length |
| 3 | `CategoryStep` | `--dj` | `--category-step` | length |
| 4 | `EpsDok` | `--eps` | `--eps-dok` | number |
| 5 | `Alpha` | `--k5` | `--alpha` | number |
| 6 | `NnMin` | `--nn-min` | `--k6` | number |
| 7 | `PocketCoefficient` | `--karmcoef` | `--k7` | number |
| 8 | `BridgeCoefficient` | `--mkmcoef` | `--k8` | number |
| 9 | `TailProbability` | `--alfa` | `--tail-probability` | number |
| 10 | `NnMax` | `--nn-max` | — | number |
| 11 | `HomogenizedOxidizerFraction` | `--gdokns` | — | number |
| 12 | `ReadPocketFormingFractions` | `--sfr` | — | switch, default off |
| 13 | `Variant` | `--ivar` | — | integer |
| 14 | `AggregatedOxideFraction` | `--eta` | — | number |

`--sfr` needs no `--no-sfr`: the original's default is off
(`ModelParameters.Default.ReadPocketFormingFractions = false`), so the switch can always
express both states. A parameter whose default ever becomes "on" gets its negation in
the same change.

⚠ The original's own naming has a trap this table keeps rather than fixes: menu [5] is
`k5` and menu [9] is `alfa`, and `k5` is the one usually called alpha in prose. The
canonical names are therefore `--k5` and `--alfa`, and `--alpha` is an alias of `--k5`
only. Renaming either would make a command line that reads plausibly do something else,
which is worse than an awkward name.

Run options, which the original had no menu for, carry descriptive names only:
`--mode reference|batched`, `--accelerator auto|cpu|cuda`, `--layout original|independent`,
`--precision binary64|original`, `--batch <n>`, `--seed <n>`, `--output <path>`,
`--json <path>`, `--quiet`, and the budgets `--attempts-per-launch`,
`--max-attempts-per-particle`, `--neighbour-budget`, `--record-budget-bytes`, plus
`--continued-streams` (accepted only with `--mode batched --batch 1`; otherwise an
error, since `SimulationOptions` gives it meaning only there).

`--precision`, added 2026-09-21 for the root's "Precision kind is an option of
every run" (named `--accumulation` until the rename of 2026-09-23, root BOOT.md,
"Precision kind is an option of every run"; `src/Cli/API.md`, "Precision flag,
setup plane", no alias kept for the old spelling): it names what the run reproduces,
not the executable it is compared against, so it reads `--precision binary64|original`,
never `--reference` or `--fidelity` (root BOOT.md's own caution: the name must not
claim more than it does). `binary64`, `SimulationOptions.Precision`'s own default, is
unchanged for every prior invocation. `original` with batched mode (any route to it,
including the degenerate
`--batch 1`/`--continued-streams` configuration) is refused by `Simulation` itself as
`RunStatus.InvalidSetup` (`src/Simulation/API.md`, "## Errors"), not by a second check
here: the existing `InvalidSetup` branch of `ReportFailedRun` already echoes the
library's own message, which names both the kind and the mode, so no new branch is
needed ("The tool validates form, never meaning" above; AGENTS.md §11 — the decision
that `InvalidSetup` is reused, not a new status, was the root's, made with `Simulation`,
not this node's to make on its own).

⚠ 2026-09-24: the enum value itself, `PrecisionKind.Double`, was renamed to
`PrecisionKind.Binary64` (CA1720, owner's decision). Unlike `--accumulation` above,
`--precision double` is kept as an accepted spelling of the same value
(`src/Cli/FlagCatalog.cs`'s `EnumValues`), because it is a value spelling an existing
command line already uses, not a flag name a design session chose to retire.

### `results.m` goes where the original put it

The default output is `results.m` in the current directory, overwritten if it exists,
exactly as the original wrote it, so that a MATLAB script that worked with the original
works unchanged with the port. `--output` names another path; `--json` additionally
writes `ResultsJson`, and has no default. Nothing else is written.

Both paths are resolved to full paths against the process working directory at parse
time, and the full path appears in the summary line, so that a user who ran from an
unexpected directory sees where the file went. Each output directory is checked at the
start, before the run: a missing one fails immediately with exit 3, naming that
directory, rather than after the cycles. Each file is written to a temporary file in
its own directory and moved over the target, so that a reader never meets a
half-written `results.m`; `AtomicFile.Write` names the target path in whatever
`IOException`/`UnauthorizedAccessException` this produces, since the runtime's own
message rarely does. Two runs writing the same default path are last-writer-wins, as
with the original; the tool takes no lock. Nothing is written when the run fails.

⚠ 2026-09-20 (review): this used to claim the full path appears "in every message of
exit 3". `AcceleratorUnavailable` is also exit 3 and has no path to name (an
accelerator, not a file) - its message is `SimulationFailedException.Message` as the
library gives it. The two routes that do write a file (the missing-directory check,
the write itself) now both name their own path explicitly, the second one added in
this review (`AtomicFile.Write`'s own wrapping, above); the accelerator route was
never going to have one and the claim is narrowed to say so.

⚠ 2026-09-20: the root `API.md` said `results.m` "is written beside the input file"
and "sizes on the command line are in micrometres". Both were sketches written before this node had a
design session, and both are corrected there in the same commit as this document: the
first because the purpose of the file is to feed post-processing that looks in the
working directory, the second because converting a length to micrometres would change
the binary32 echoes `Statistics` derives from the value as written.

### Progress and the summary

`run` prints one line per finished cycle to **stderr** (`cycle k of K, particles n`) from
the `IProgress<CycleProgress>` of `Simulation`, and one summary line at the end (mode,
accelerator, batch size, layout, seed, total attempts, elapsed, and the full path of
every file written). `--quiet` suppresses the progress and the summary, never an error.
No carriage-return animation. Stdout carries nothing for `run`, so that a shell redirect
of stdout is empty on success; `devices`, `defaults` and the help and version texts
print to stdout. Numbers on stderr are formatted with the invariant culture as well; no
number of `results.m` is formatted here, that is `Output`'s.

### Cancellation

Ctrl+C is taken by the tool itself (`Console.CancelKeyPress` with `Cancel = true`), so
the process is never killed by the operating system's own control-C status: the run is
cancelled through the `CancellationToken` of `Simulation.Run`, no output file is
written, "interrupted" goes to stderr and the exit code is 130, the tool's own
documented code on every platform. A second Ctrl+C is left to the runtime.

⚠ With no console attached — a pipe, a scheduled task, a service — `CancelKeyPress`
never fires and the process is terminated by the host. Accepted for version 1 and
written down here so that it is not discovered as a defect.

### Mapping of outcomes onto exit codes

| Outcome | Exit |
|---|---|
| success | 0 |
| `SimulationFailedException` with `AcceleratorUnavailable` | 3 |
| `SimulationFailedException` with `InvalidSetup` | 2 |
| `SimulationFailedException`, any other status | 1 |
| parse error, unknown flag or verb, no verb, missing value, unreadable or malformed `.dat` (`FormulationFormatException`, `FileNotFoundException`, with the path or the line), `GSV ≠ 2` | 2 |
| `IOException`/`UnauthorizedAccessException` while writing an output, or a missing output directory found at the start | 3 |
| any other unhandled exception: a defect of the tool, its type and message on stderr | 4 |
| `OperationCanceledException` from Ctrl+C | 130 |

A failed status prints the status name and what raises the corresponding budget flag.

### The entry point is a function, not a process

`Main` is one line over
`Program.Run(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken)`,
so `tests/Cli.Tests` drives the whole tool in-process, injects writers and cancels
through the token. Exit code 130 is tested that way: sending a real Ctrl+C to a child
process is unreliable in a test runner, and a flaky test is worse than a declared gap —
there is no process-level Ctrl+C test, by decision.

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

## Taboos

- No interactive prompts, no reading of stdin.
- No argument-parsing package.
- No model arithmetic and no number formatting: `Simulation` and `Output` own those.
- No conversion of a length between metres and micrometres.
- No output file the arguments did not name or imply.
- No reading of the Fortran source: this node does not transcribe it.
