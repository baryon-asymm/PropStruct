# API.md — Benchmarks

A console project. It exposes no types to the tree; its surface is the command that
runs it and the table it fills in its own `BOOT.md`.

## Command line ✅

```console
$ dotnet run -c Release --project tests/Benchmarks -- [--formulation HPEPA3,HMX]
                                                     [--paths reference,host1,host16,cpu,cuda,original]
                                                     [--repeats 3]
                                                     [--particles N]
                                                     [--attempts-per-launch N]
```

| Argument | Meaning |
|---|---|
| `--formulation` | which reference formulations to measure; the default is HPEPA3 and HMX |
| `--paths` | which paths to measure; unavailable ones are reported as not measured, never as zero |
| `--repeats` | runs per figure, whose spread is recorded with it; the default is 3 |
| `--particles` | overrides each formulation's own particle-per-cycle count for the port paths only (`reference`/`host1`/`host16`/`cpu`/`cuda`); the `original` row always runs the formulation's own shipped count. Added this coding session (`BOOT.md`, "## Implementation notes") to make a small, fast verification run possible; the default is the formulation's own count. |
| `--attempts-per-launch` | overrides `SimulationOptions.AttemptsPerLaunch` for the port paths only (`reference`/`host1`/`host16`/`cpu`/`cuda`); the `original` row has no such concept. Added in the post-acceptance performance session (`BOOT.md`, "## Post-acceptance plan", E-B) so the budget sweep can run through this node without a source change each time; the default is `SimulationOptions`' own default. |

Every port path also runs with `Cycles = 1`, unconditionally (`BOOT.md`, Constraints:
"one cycle is enough") — there is no flag for this, since running more would only cost
time for no figure this node reports.

Every row also carries the budget the call actually used (`n/a` for `original`), and
the launch and attempt totals `SimulationResult.Diagnostics` reports for that same call
(`n/a` for `original`, which is not a `Simulation` run at all). The totals cover the
whole call, cycle 0's warm-up included, not the isolated cycle-1 window the
particles/s figure uses — the same kind of whole-run figure the `original` row's own
particle count already is (`BOOT.md`, "Original's particles/s").

⚠ 2026-09-28: was "`RunDiagnostics` itself carries no budget field ...; this node
reports the value it passed instead of reading one back", now `RunDiagnostics.AttemptsPerLaunch`
exists (`src/Simulation/BOOT.md`, "## Budget selection rule (2026-09-28)") and every
row's Budget column is that *effective* value, read back from the call's own
`Diagnostics`, not the option this node passed — the two agree for every port path
except `reference`, whose effective budget is always 1 (`BOOT.md`, "## Figures", the
note under the W2 table).

`--paths cpu` is accepted but always reports `not available`: the ILGPU CPU
accelerator's own kernel-launch path cannot be reached through `Simulation`'s public
surface (`BOOT.md`, "## Implementation notes", "Escalation: the CPU accelerator oracle
path").

It prints a provenance preamble (date, machine, .NET version, commit, the exact
command, the `--particles` override if any) once per invocation, then one markdown row
per figure in the shape of the table in `BOOT.md`, ready to be pasted into that table
in the same commit. The commit reads `<hash>-dirty` when the working tree holds
uncommitted or untracked changes when the command starts, and `unknown` when git cannot
say (`BOOT.md`, "Provenance's commit").

The `host1` row runs its child process pinned to one logical CPU, and is reported as not
measured when the pin fails (`BOOT.md`, "Host-thread counts", ⚠ 2026-10-02); `host16`
is not pinned.

## Side effects

Runs the simulator and, for the baseline row, the legacy executable. Writes nothing
except its console output.

## Out of scope

- Any assertion about speed: this node records, `BOOT.md` of the tree's nodes cite.
- Correctness of a run: `tests/Simulation.Tests` and `tests/Execution.Tests`.
