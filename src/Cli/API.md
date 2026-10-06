# API.md — Cli

The `propstruct` tool (assembly `PropStruct.Cli`, assembly name `propstruct`). No
library surface: everything below is the command line itself, and every type of the
node is internal.

## Commands ✅

```console
propstruct run <file.dat> [run options] [model parameters]
propstruct devices
propstruct defaults
propstruct --help | --version
```

Run options (defaults are `SimulationOptions`', `src/Simulation/API.md`):

```console
  --mode reference|batched          --accelerator auto|cpu|cuda
  --layout original|independent     --batch <n>          --seed <n>
  --precision binary64|original     (original: sequential only)
  --output <path>                   --json <path>        --quiet
  --attempts-per-launch <n>         --max-attempts-per-particle <n>
  --neighbour-budget <n>            --record-budget-bytes <n>
  --continued-streams               (batched, --batch 1 only)
```

`--precision original` reproduces the original's REAL*4 accumulation loss (root
BOOT.md, "Precision kind is an option of every run"); `--precision binary64`, the
default, matches every run before this flag existed. `--precision original` with
`--mode batched` (including the degenerate batch-of-one configuration) is refused by
`Simulation` as `RunStatus.InvalidSetup` — see "Errors" below, not a second check here.
The old spelling, `--accumulation`, is not kept as an alias (decided 2026-09-23): an
alias would let a name that no longer says what the flag does survive in scripts.

⚠ 2026-09-24: `PrecisionKind.Double` was renamed to `PrecisionKind.Binary64` (CA1720,
owner's decision: an identifier may not contain a type name). `--precision binary64`
is now the canonical spelling; `--precision double` is kept as an accepted spelling of
the same value, unlike `--accumulation` above, because it is the flag's *value* that
changed name, not the flag itself, and an existing command line using `--precision
double` must keep working (`src/Cli/FlagCatalog.cs`).

Model parameters, the original's menu (canonical name, alias; `src/Cli/BOOT.md`,
"Flag names are the original's own names, with aliases"):

```console
  --dmin <d>            --di <d> (--cell-size)      --dj <d> (--category-step)
  --eps <x> (--eps-dok) --k5 <x> (--alpha)          --nn-min <x> (--k6)
  --karmcoef <x> (--k7) --mkmcoef <x> (--k8)        --alfa <x> (--tail-probability)
  --nn-max <x>          --gdokns <x>                --sfr
  --ivar <n>            --eta <x>
```

A length is handed to `Input`'s `Length` as written: a value `≥ 0.1` is micrometres, any
other value metres. Nothing is converted. Every number is read with the invariant
culture; a Fortran exponent form (`10d-6`) is refused. A flag given twice is an error.
`--help` (also `-h`, also after a verb) and `--version` print to stdout and exit 0.

**Precision flag, setup plane** (implemented 2026-09-23): `--precision original` now
also reproduces the setup plane's REAL*4 storage, through `Simulation`'s own hand-off
to `Statistics.Setup.Prepare` (`src/Statistics/BOOT.md`, "## Setup plane"), not the
accumulators alone.

**Precision flag, per-cycle plane** (implemented 2026-10-01): `--precision original`
also reproduces the per-cycle plane (`src/Statistics/BOOT.md`, "## Report"); the usage
text says so.

## Output ✅

| Verb | stdout | stderr | files |
|---|---|---|---|
| `run` | nothing | one line per finished cycle and one summary line naming every file written, unless `--quiet` | `results.m` in the current directory or `--output`; `--json` as well, if given; each written through a temporary file and moved over the target; none at all if the run fails |
| `devices` | one line per accelerator found (kind, name, memory, libdevice) | — | none |
| `defaults` | the fourteen parameters: menu number, canonical flag, aliases, value | — | none |
| `--help`, `--version` | the usage or the version, exit 0 | — | none |

## Errors ✅

| Situation | Behaviour |
|---|---|
| unknown flag or verb, no verb, a flag given twice, missing or unparsable value, `--switch=value` | message naming the flag or verb on stderr, usage where a verb is missing, exit 2 |
| missing, unreadable or malformed input file, `GSV ≠ 2` | message with the line number on stderr, exit 2 |
| failed run status (other than the two below) | the status name and the flag that raises its budget, exit 1 |
| `InvalidSetup` | message on stderr, exit 2 |
| `AcceleratorUnavailable`, an I/O error writing an output, a missing output directory (found before the run) | message with the full path on stderr, exit 3 |
| any other unhandled exception (a defect of the tool), except the two below | type and message on stderr, exit 4 |
| Ctrl+C | "interrupted" on stderr, no output file, exit 130 |

A value the model refuses is not checked here: it comes back from the library as
`InvalidSetup` and exits 2.

⚠ 2026-09-24: `OutOfMemoryException` and `InsufficientExecutionStackException` are no
longer mapped to exit 4 (CA1031, owner's decision): `Program.Dispatch`/`ExecuteRun`'s
catch-alls now carry a `when (!IsProcessFatal(ex))` filter, and these two propagate out
of the process uncaught instead, since a returned exit code claims the process can still
meaningfully produce one. Every other exception keeps exactly the mapping above.

## Side effects

Writes the output files named or implied, overwriting them. Reads the input file only.
Never reads stdin.

## Out of scope

- Anything a library user needs: [Simulation](../Simulation/API.md), [Output](../Output/API.md).
- Reading `results.m` back: `tests/Harness`.
