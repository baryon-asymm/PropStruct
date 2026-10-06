# API.md — PropStruct

Tree root. The system is a .NET library under the root namespace `PropStruct`, whose
front door is [Simulation](./src/Simulation/API.md), plus the `propstruct` command-line
tool over it. Everything not named here is internal and may change.

⚠ 2026-09-20: this document said "Every declaration is planned: no code exists yet" and
carried ⏳ on all three of its sections, from the day the tree was born until now, through
the ninety-three commits that implemented them. The claim was false and the mark was worse
than the claim: AGENTS.md §7 exempts a ⏳ block from the build check, so the system's
top-level contract — the first document a consumer of this library reads — was the one
document in the tree verified against nothing. It had drifted accordingly: the example
below called `ResultsMWriter.Write(result, path)`, a two-argument overload that does not
exist and never did. The marks are ✅ as of today and the call is the real one.

Found by an Opus 5 review of the branch, asked whether the documents tell the truth about
the code, which answered by checking this one first. Its remedy, that flipping the marks
would put this file under `DeclarationTests` like every other, was then **measured and is
false**: mutated to `simulator.RunItAll(...)` and again to `new SimulationOptionsZZZ`, the
check stayed green both times. The reason is not the mark. `DeclarationTests` reads blocks
of **declarations** — a type and its members — and this document's block is a **usage
example**, in which `new SimulationOptions { ... }` and `simulator.Run(...)` declare
nothing, so the parser finds nothing to check and passes. The root contract is therefore
still verified by no machine check, and turning its marks green did not change that; what
changed is that the false sentence is gone and the example compiles. Closing the gap means
either giving this document a declaration block beside its example, or stating in
`tests/Protocol.Tests/BOOT.md` that a usage example is out of the check's reach by design —
a decision for a design session, recorded here so the next reader does not assume, as the
review did, that a ✅ implies a check.

## How the system is used ✅

1. Read a formulation from a `.dat` file of the original format, or build a
   `Formulation` in code ([Input](./src/Input/API.md)).
2. Take the model parameters: `ModelParameters.Default` holds the original's menu
   defaults; override what the original's menu would have changed.
3. Choose the run: reference mode (the original's sequence, one thread) or batched
   mode (whole cycles at once, CPU accelerator or CUDA), batch size, budgets, seed.
4. Run the simulator ([Simulation](./src/Simulation/API.md)). The result holds every
   quantity the original prints, grouped, plus run diagnostics.
5. Write the result as `results.m` in the original's layout or as JSON
   ([Output](./src/Output/API.md)).

The command line does steps 1–5 with files ([Cli](./src/Cli/API.md)).

## Entry points ✅

```csharp
using PropStruct.Execution;
using PropStruct.Input;
using PropStruct.Output;
using PropStruct.Simulation;

var formulation = DatFile.Read("inpt.dat");

var parameters = ModelParameters.Default with { EpsDok = 0.02 };

var options = new SimulationOptions
{
    Parameters = parameters,
    Mode = ExecutionMode.Batched,          // or ExecutionMode.Reference
    Accelerator = AcceleratorKind.Auto,    // Auto, Cpu, Cuda
    BatchSize = null,                      // null: the whole cycle, capped by the record budget
    Streams = StreamLayout.Independent,    // the default; Original runs in reference mode only
    Precision = PrecisionKind.Binary64,    // the default; Original runs in reference mode only
    Seed = 0,                              // 0: the layout's initial states
};

using var simulator = Simulator.Create(options);
var result = simulator.Run(formulation);   // SimulationFailedException on a failed status

ResultsMWriter.Write(formulation, parameters, result, "results.m");
ResultsJson.Write(result, "results.json");
```

The block is `samples/Quickstart`'s two marked regions, the `using` lines, one blank line
and the body (`samples/Quickstart/API.md`), and the README of the package `PropStruct`
quotes the same text. The original's own numbers (its seeds and its REAL*4 storage)
need all three switches together, `Mode = ExecutionMode.Reference`,
`Streams = StreamLayout.Original` and `Precision = PrecisionKind.Original`: for
example `options with { Mode = ExecutionMode.Reference, Streams = StreamLayout.Original,
Precision = PrecisionKind.Original }`.

⚠ 2026-10-04: was a block reading `HPEPA3.dat`, with the `using` lines lacking
`PropStruct.Execution` and a `reproduction` variable built from `options` and never
used, which no compiler had seen; now the program of `samples/Quickstart` reading
`inpt.dat`, with the reproduction switches in the sentence above, since an unused local
is a build error in this tree (IDE0059). `QuickstartExampleTests` compares the block
with the sample's regions, so the example is compiled and its text is checked, closing
the gap of the ⚠ of 2026-09-20.

⚠ 2026-09-24: this example set `Mode = Batched` with `Streams = StreamLayout.Original`.
That combination has been refused with a status since 2026-09-21 (root `BOOT.md`, "Two
stream layouts"), so the example failed on every run. It now shows the defaults, and the
reproduction switches separately. Found by the architecture audit of 2026-09-24.

## Command line ✅

```console
propstruct run HPEPA3.dat                                   # results.m in the current directory, original defaults
propstruct run HPEPA3.dat --mode reference --output ref.m   # the original's sequence, one thread
propstruct run HPEPA3.dat --accelerator cuda --seed 3 --json result.json
propstruct run HPEPA3.dat --mode reference --layout original --precision original   # the original's own numbers
propstruct devices                                          # the accelerators found
propstruct defaults                                         # the fourteen parameters with the original values
```

Exit codes: 0 ok, 1 the run failed with a status, 2 invalid arguments or invalid input,
3 accelerator or infrastructure error, 4 an unhandled exception, 130 interrupted. A size on the command line is
written as the original's menu took it, and is kept as written (a value ≥ 0.1 is
micrometres, any other value metres). The flags and their diagnostics are
[Cli](./src/Cli/API.md)'s. `PROPSTRUCT_NO_CUDA=1` refuses CUDA.

⚠ 2026-09-20: this paragraph first said "sizes on the command line are in micrometres,
as in the original's menu", and the example above said `results.m` is written beside
the input file. Both were sketched before `Cli` had a design session and both are
wrong. A size converted to micrometres is no longer the number the file was written
with, and `Statistics` derives the original's binary32 echoes from the number as
written (`src/Input/BOOT.md`, "Lengths keep the value as written"), so the conversion
would silently change a run. The output goes to the working directory because that is
where the original wrote it and where existing post-processing looks
(`src/Cli/BOOT.md`, "Design decisions (2026-09-20)"). The exit-code list gains 130 for
an interrupted run and 4 for an unhandled exception, which otherwise masquerades as a
failed run status.

## Packages ✅

| Package | What it holds | Install |
|---|---|---|
| `PropStruct` | the library for `net10.0`: the public types of [Simulation](./src/Simulation/API.md), [Input](./src/Input/API.md), [Output](./src/Output/API.md) and [Execution](./src/Execution/API.md), with the internal assemblies of `Random`, `Particle` and `Statistics`; one dependency, ILGPU, exactly `[1.5.3]` | `dotnet add package PropStruct` |
| `PropStruct.Cli` | the .NET tool `propstruct`, the command line above; it bundles `ILGPU.dll` (ILGPU 1.5.3, NCSA), whose licence is the package's `THIRD-PARTY-NOTICES.txt`, and its license expression is `MIT AND NCSA` | `dotnet tool install --global PropStruct.Cli` |

Both are released together under one version. Below 1.0.0 a minor version may break the
surface; `CHANGELOG.md` names the break. Supported platform: Windows x64. The CPU path
needs nothing beyond the package; CUDA needs an NVIDIA driver and libnvvm with
`libdevice.10.bc` from a CUDA Toolkit 12.8 or newer, and `Auto` falls back to the CPU
without them. The example under "Entry points" is `samples/Quickstart`'s, built against
the packed `PropStruct`. The original program is credited in `NOTICE` and not distributed.

Both pack: `python -X utf8 .github/scripts/check_packages.py all` packs them from
`src/Output` and `src/Cli` into `artifacts/packages`, checks their contents against the
tree, installs the tool from that feed and builds `samples/Quickstart` against the
package (2026-10-04, `.github/scripts/BOOT.md`). `PropStruct.<version>.nupkg` holds in
`lib/net10.0` the `.dll` and `.xml` of every assembly of `src/` but `Cli`, its
`.snupkg` their PDBs, and ILGPU is its one dependency; the packages are not yet
published, and the first packages made by CI are the criterion of the root
`ACCEPTANCE.md`.

⚠ 2026-10-04: this section was ⏳ ("planned") since the delivery was decided the same
day; it is ✅ now that both packages are packed and the package-content check of
`.github/scripts` is green on them. The section holds no declaration the build can
check, so the mark rests on that script and on `QuickstartExampleTests`.

## Children

- [Simulation](./src/Simulation/API.md) — the front door: options, the simulator, the result.
- [Input](./src/Input/API.md) — the `.dat` format and the model parameters.
- [Output](./src/Output/API.md) — `results.m` and JSON writers.
- [Cli](./src/Cli/API.md) — the `propstruct` command line.
- [Execution](./src/Execution/API.md) — accelerator choice and description.

Nodes whose surface is internal to the tree: [Random](./src/Random/API.md),
[Particle](./src/Particle/API.md), [Statistics](./src/Statistics/API.md).

## Test nodes

- [Fixtures](./tests/Fixtures/API.md) — the original's formulations and outputs (the program itself is outside the repository, [legacy](./tools/legacy/API.md)), reference and replica outputs, provenance, tolerance and exclusion tables.
- [Harness](./tests/Harness/API.md) — shared test scaffolding: CPU host, bit snapshots, `results.m` parser, statistical comparison.
- [Protocol.Tests](./tests/Protocol.Tests/API.md) — the documents against the code (AGENTS.md §13) and the root invariants that need reflection.
- Per-node test nodes, each the definition of its node's readiness:
  [Random.Tests](./tests/Random.Tests/API.md), [Input.Tests](./tests/Input.Tests/API.md),
  [Particle.Tests](./tests/Particle.Tests/API.md),
  [Statistics.Tests](./tests/Statistics.Tests/API.md),
  [Execution.Tests](./tests/Execution.Tests/API.md), [Simulation.Tests](./tests/Simulation.Tests/API.md), [Output.Tests](./tests/Output.Tests/API.md),
  [Cli.Tests](./tests/Cli.Tests/API.md),
  [Fixtures.Tests](./tests/Fixtures.Tests/API.md), [Harness.Tests](./tests/Harness.Tests/API.md).
- [Benchmarks](./tests/Benchmarks/API.md) — the recorded speed of the port against the original.
- [Quickstart](./samples/Quickstart/API.md) — the library example, built against the packed package.

Tool nodes: [protocol-lint](./tools/protocol-lint/API.md),
[defect-report](./tools/defect-report/API.md), [legacy](./tools/legacy/API.md),
[.github](./.github/API.md).
