# BOOT.md — Quickstart

## Purpose

The library example as a running program: read a formulation, choose the run, run the
simulator, write `results.m` and `results.json`. It exists because the example in the
root `API.md` ("## Entry points") was checked by no machine (its ⚠ of 2026-09-20:
`DeclarationTests` reads declarations, and an example declares nothing), and because
the `PropStruct.Cli` tool bundles its own copies of the library assemblies, so a run
of the tool proves nothing about the `PropStruct` package a consumer adds with
`dotnet add package`. This node is the one place that code is compiled and run against
the **packed** library, and the source of the example that the root `API.md` and the
package README quote.

Stage S3 of the delivery built the node on 2026-10-04: the project, the program and the
check of the quoting places (`tests/Protocol.Tests`, `QuickstartExampleTests`) exist,
and every declaration of `API.md` is ✅.

## Invariants

- **The package surface only.** The program calls what a consumer of the packed
  `PropStruct` calls: the public types of `Simulation`, `Input`, `Output` and
  `Execution`, and nothing internal, no `InternalsVisibleTo` grant to or from it.
  What the root `API.md` ("## Packages") lists is its whole reach.
- **The example is one marked region.** The code between `// snippet-start: Quickstart`
  and `// snippet-end` is the example, its `using` lines a second region,
  `QuickstartUsings`. The block of the root `API.md` ("## Entry points", the `using`
  region, one blank line, the body), and the two fenced blocks, each after its
  `<!-- snippet: <name> -->` marker, of the package README (`docs/nuget/PropStruct.md`)
  and of the repository `README.md` equal the regions after the common leading
  indentation is stripped, byte for byte; a page that needs other parameters is another
  sample, never an edited region.
- **The region is compiled against the package and runs.** CI builds the project against
  the packed `PropStruct` from a job-local feed and runs it (`.github`); a change to
  `src/` that breaks the public surface fails there before it reaches a release.
- **A failed run is not a quiet one.** The program returns 0 for a run whose status is
  ok, 1 for a failed status (`SimulationFailedException`) and 2 when the formulation
  cannot be read, and prints the paths of the two files it wrote and no figure that
  depends on the machine, the time or the accelerator chosen by `Auto`.
- **The formulation is data of the tree.** The program reads `inpt.dat` of
  `tests/Fixtures/Legacy/formulations` from its working directory, where
  `.github/scripts/check_packages.py` puts a copy in a scratch directory; the example's
  file name is that file's (`QuickstartExampleTests`).

## Dependencies

- [Simulation](../../src/Simulation/API.md) — `SimulationOptions`, `Simulator`,
  `SimulationResult`, the execution mode and the stream and precision switches.
- [Input](../../src/Input/API.md) — `DatFile`, `Formulation`, `ModelParameters`.
- [Output](../../src/Output/API.md) — `ResultsMWriter`, `ResultsJson`.
- [Execution](../../src/Execution/API.md) — `AcceleratorKind`.

Outside the tree: the .NET SDK of `global.json`; the packed `PropStruct` package, in
its second build mode.

## Constraints

Inherited from the root ([BOOT.md](../../BOOT.md)). In addition:

- The project is `samples/Quickstart/PropStruct.Quickstart.csproj`, a console app in the
  solution, namespace `PropStruct.Quickstart` (`samples` is transparent in a namespace,
  as `src` and `tests` are, `tests/Protocol.Tests`); it is not packable and part of no
  package.
- **Two build modes.** By default the project references `Simulation`, `Input`,
  `Output` and `Execution` directly, so a change in `src/` is seen at once. With
  `-p:PropStructPackageVersion=<version>` it instead takes one `PackageReference` to
  the packed `PropStruct` at that version (`VersionOverride`, since package versions
  are managed centrally), from a feed given with `-p:RestoreAdditionalProjectSources`
  or a scratch `nuget.config` outside the tree; nothing under source control names a
  feed path. The region compiles unchanged in both modes.
- **Cost.** The run completes in seconds on a hosted Windows runner without CUDA. The
  formulation is the smallest shipped, `inpt.dat`, which ran in about a second on the
  CPU on the reference machine (2026-10-04); the example in the root `API.md` names it.
- **Not under the Legacy rule.** It reads data only (`tools/legacy`).

## Acceptance criteria

- [x] 2026-10-04 — The region builds and runs in both modes and writes `results.m` and
      `results.json`: `python -X utf8 .github/scripts/check_packages.py sample` builds
      the project against the local feed (the dependency file says `package`) and
      against the projects (`project`), runs both on `inpt.dat`, and the two `results.m`
      are equal byte for byte, time line excluded.
- [x] 2026-10-04 — The block of the root `API.md` and the examples of
      `docs/nuget/PropStruct.md` and `README.md` equal the regions of the program, byte
      for byte after the indentation is stripped, machine-compared and not typed here:
      `QuickstartExampleTests.TheFourPlacesQuoteTheSamplesRegionsByteForByte`; red once
      on an edited character in each place, `AnEditedCharacterInAnyOfThePlacesIsRed`
      (the three mutations on the files are in `tests/Protocol.Tests/BOOT.md`).
- [ ] The package-feed mode restores `PropStruct` from the job-local feed of CI and runs
      the sample green (date, CI run id, commit). Locally the same step is green on the
      feed of `check_packages.py pack` (criterion 1).
- [ ] The run takes seconds on a hosted Windows runner: the duration of the step is
      recorded, with the formulation named (date, CI run id).

## Taboos

- No internal type of another node, no grant from a neighbour.
- No snippet region edited in one place only: a change to the region is a change of the
  root `API.md` block and of both README examples in the same commit.
- No machine-dependent figure printed.
- No file I/O outside the scratch working directory and the one formulation read.
