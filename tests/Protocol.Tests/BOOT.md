# BOOT.md — Protocol.Tests

## Purpose

The reflection half of AGENTS.md §13 for this .NET tree, and the root invariants that
only reflection can check: public surface snapshot, every exported type named in its
node's `API.md`, every ✅ declaration existing, declared dependencies equal to the real
ones (signatures and method bodies), no `float` in numerical assemblies, no mutable
static fields in numerical nodes, CUDA named only by `Execution`, namespaces mirroring
directories.

## Invariants

- The node walker determines a node by its directory path from the tree root and skips
  `.git`, `.vs`, `.claude`, `bin`, `obj`, `TestResults`, `legacy` (the Fortran archive,
  not a source directory) and `templates` (the protocol kit's own document templates,
  the linter's own `--exclude templates`).
- The tree root is found from the source file (`[CallerFilePath]`), not from the
  binary.
- A node's namespace is its directory path from the root with `src`, `tests` and
  `samples` dropped (`Node.Namespace`): `samples/Quickstart` is `PropStruct.Quickstart`
  (2026-10-04, stage S3 of the delivery; `samples` was not transparent before, and the
  node would have been attributed to the root).
- **The library example is one text in four places** (`QuickstartExampleTests`,
  `QuickstartExample`): the two marked regions of `samples/Quickstart/Program.cs`, the
  block of the root `API.md` ("## Entry points"), and the examples of
  `docs/nuget/PropStruct.md` and of `README.md` are equal byte for byte after the
  common indentation is stripped; the first of them is compiled and run, so the root
  `API.md`'s ⚠ of 2026-09-20 (an example checked by no machine) is closed.
- Types are looked up in every assembly of the tree: every project of the tree is
  referenced by this node's own project for exactly that (none of their types is used
  directly).
- A parent using its children's types is not a dependency; on this tree no node has a
  descendant node with its own project, so that branch of `DependencyTests` is written
  but not exercised by a real crossing (2026-09-20; see "Mutation record").
- `DependencyTests` reads both directions of AGENTS.md §13's "real dependencies": a
  type's shape (`TypeShape.Shape` — base type, interfaces, fields, properties, events,
  method returns and parameters, local variables) *and* the bodies of its methods
  (`IlBody.BoundTypes`, an IL walk over every instruction's token — a static call
  names its callee's declaring type, return type and parameter types in no signature
  of the caller, and is invisible to a shape-only reader). Both feed one set,
  `TypeShape.ReferencedTypes`, which `DependencyTests.Crossings` is the only reader of;
  there is no second, signature-only pass anywhere in this node that a body-only
  crossing could slip past.
- A dependency the code cannot show by a type crossing (a data-only neighbour such as
  `tests/Fixtures`, whose files are read by path, or a node born ahead of its code such
  as `tools/defect-report`, which has no assembly at all) is never reported as
  "declared but unused": the reflection walk has no way to confirm or refute a
  file-based use, and flagging it would be a permanent false positive against every
  node that legitimately reads such files, not a finding about the document
  (2026-09-20 design decision, `DependencyTests.ProblemsOf`). The *other* direction
  stays exact: a node with no assembly also has no types, so it can never appear as an
  undeclared-but-used dependency either. Nothing here weakens the check for a node that
  *does* have an assembly: `tests/Output.Tests` → `src/Execution` and
  `tests/Statistics.Tests` → `src/Input` were both real, undeclared, type-level
  crossings, and both were caught and closed (`claude/wave7@22a9568`).
- Every check is proven non-degenerate by a recorded mutation, applied to a real file
  of the tree and reverted before the next commit (never committed itself); see
  "Mutation record" below.
- **The single-precision check has one named, singular exemption, asserted so.** Root
  `BOOT.md`, Taboos, permits exactly one `float` write site: the accumulator rounding
  of the `Original` accumulation kind, `Attempt.AddReal4` (`src/Particle`). It is named
  in `InvariantTests.PermittedSinglePrecisionSites` by node, declaring type, method name
  and the exact count of `conv.r4` instructions the body may hold (one) — not "any
  method whose name contains Real4", not "any method of `Attempt`", not
  "`src/Particle` at large". The fact asserts, before anything else, that the list
  holds exactly one entry (`Assert.Single`), and separately tracks, across the walk,
  which entries a real method body actually matched; an entry naming a site that no
  longer exists (renamed or removed) fails the fact by name rather than leaving it
  vacuously green, the same shape `NodeAssemblies.DeclaredNamespaceExceptions` already
  uses for the `tests/Harness` namespace exception above. To add a second exemption:
  name it in that list and widen the `Assert.Single` deliberately, in the same diff —
  the match itself (by node, type, method name and instruction count) is not to be
  loosened to admit a second site silently.

  ⚠ 2026-09-21: the taboo did not carve out any `float` until this decision; when it
  did, `InvariantTests.NumericalNodesHoldNoSinglePrecisionValueOrOperation`
  needed a narrower shape than "no `.r4` opcode anywhere in a numerical node's code",
  because that shape cannot distinguish the one permitted write site from a second one
  appearing anywhere else, including a second `conv.r4` in the same method. See the
  three mutations below.

## Dependencies

None.

Outside the tree: xunit. (`System.Reflection.Emit` and `System.Reflection.Metadata`,
used for the IL opcode table and method-body walk, are part of the .NET SDK, not a
separate package; this line first over-stated an external dependency the design
session guessed at before any code existed.)

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- Adapted from APThermo `tests/Protocol.Tests`; the mistakes listed in AGENTS.md §13
  are already fixed there and must not be reintroduced.
- Created early, with the first assemblies, not last.

## Acceptance criteria

- [x] 2026-09-20 — Every check of the second table of AGENTS.md §13 exists and was
      seen red once: `SurfaceTests`, `CoverageTests` (two facts: undocumented export,
      namespace mirroring), `DeclarationTests`, `DependencyTests`. The mutations are
      listed in "Mutation record" below.
- [x] 2026-09-20 — The root invariants "double precision only", "no hidden state",
      "CUDA only in Execution" and "namespaces mirror the directory path" are checked
      by reflection (`InvariantTests`, `CoverageTests`) and each was seen red once
      (see "Mutation record").
- [x] 2026-09-25 — root BOOT.md's "Language and build" decision (`AnalysisMode=All`,
      `EnforceCodeStyleInBuild`, `TreatWarningsAsErrors`, no suppression anywhere) is
      guarded by `NoSuppressionGuardTests`, eight facts, each seen red once on a real,
      then reverted, violation, or on an in-test synthetic string through the shared
      check function (`## Mutation record` below). One of the eight
      (`DirectoryBuildPropsKeepsTheAnalyzerDecision`) was still red when this row was
      first written, genuinely: `Directory.Build.props` had not yet been flipped (this
      node's own coding task, step 5) — its own non-degeneracy proof and the real
      failure it guards were the same event. Flipped the same day; all eight facts are
      green (`dotnet test tests/Protocol.Tests --filter FullyQualifiedName~NoSuppressionGuardTests`, 8/8).
      Extended the same day (root BOOT.md, IDE0005 src-only scoping, owner's decision):
      `FindLoweredSeverities` became section-aware so a rule held at its own `default`
      severity outside a path-scoped section is not flagged, but only when a narrower,
      path-scoped section (e.g. `[src/**.cs]`) raises that same rule back to at least
      `warning` — exactly the `[src/**.cs]` shape the root `.editorconfig` now carries
      for `IDE0005`. `TheDefaultScopingExceptionIsNarrow` proves the exception is that
      narrow and not a general hole: it still flags the same shape for a different rule
      ID, and it still flags `default` with no narrower raise.
- [x] 2026-09-20 — `dotnet test PropStruct.sln --filter "Category!=Long"` is fully
      green (11 projects, `PropStruct.Protocol.Tests.dll`: 8/8). Two of the three
      divergences the first run found were closed by their owning nodes
      (`claude/wave7@22a9568`: `tests/Output.Tests` now declares `src/Execution`,
      `tests/Statistics.Tests` now declares `src/Input`). The third
      (`tests/Harness`/`tests/Harness.Tests` namespace) is scheduled, not closed yet
      (a concurrent session is inside `tests/Harness`); it is carried as the declared
      deviation below rather than left red or silently patched around.
- [x] 2026-10-02 — `DeclarationTests` reads the ✅ declarations whose return type is a
      tuple (`internal static (long Low, long High) HighestDensityRegion(...)`, the
      multi-line `PerCellCounts` among them): the 2026-09-20 fix of the parser had
      skipped those lines, so a renamed or deleted method passed. Right: 528 → 535
      declarations read across the tree's ✅ blocks (the seven tuple-returning ones of
      `tests/Harness/API.md`), all of them exist, the suite is 20/20 green. Red:
      `ApiDeclarationsTests` (one line, parameters over several lines, tuple over
      several lines and nested, tuple-typed field) fails 3 of 4 on the old parser and
      passes 4 of 4 on the new; and the mutation of the record below.

- [x] 2026-10-04 — `QuickstartExampleTests` is green and red once on an edited character
      in each of the places (three mutations on the files in "Mutation record", and
      `AnEditedCharacterInAnyOfThePlacesIsRed` in-test through the same call), and
      `SurfaceTests` is green after the packaging properties: with `Directory.Build.targets`,
      the package metadata of `src/Output` and `src/Cli` and the merge targets in place,
      the only line pair that moved in `PublicSurface.approved.txt` is the empty section
      `== PropStruct.Quickstart` of the new node's assembly (no exported type), approved
      with the commit that added the node, like `Benchmarks` and `DispersionTool` before
      it; no type of any other assembly moved.

## Mutation record

Every mutation below was applied to a real file of the tree (never to this node's own
check logic), observed red with `dotnet test tests/Protocol.Tests`, then reverted; each
revert was confirmed by `git status --porcelain` on the touched path returning empty
before the next mutation. Nothing here was committed.

| Date | Check | Mutation | Observed failure |
|---|---|---|---|
| 2026-09-20 | `SurfaceTests` | Appended a bogus line to this node's own `PublicSurface.approved.txt` | "no longer matches PublicSurface.approved.txt", first difference at the appended line |
| 2026-09-20 | `CoverageTests` (undocumented export) | Added `PropStruct.Output.MutationGhostType`, an undocumented public type, to `src/Output` | "src/Output/API.md never names MutationGhostType, which PropStruct.Output exports" |
| 2026-09-20 | `CoverageTests` (namespace mirroring, before the declared deviation below existed) | None needed: `tests/Harness` and `tests/Harness.Tests` violated this invariant on this tree from the start | 17 real types reported, e.g. "PropStruct.Tests.Harness.BitSnapshot is in namespace PropStruct.Tests.Harness, and no node of the tree is exactly that namespace" |
| 2026-09-20 | `CoverageTests` (namespace mirroring still catches a *new* divergence, with the declared exception in place) | Added `PropStruct.WrongPlace.MutationWrongNamespaceType` to `src/Output` | "PropStruct.WrongPlace.MutationWrongNamespaceType is in namespace PropStruct.WrongPlace, and no node of the tree is exactly that namespace" — proves the exception list exempts only the two named nodes, not the fact itself |
| 2026-09-20 | `CoverageTests` (a stale declared exception is itself caught) | Added a third, bogus entry `["src/Random"] = "PropStruct.MutationStaleException"` to `NodeAssemblies.DeclaredNamespaceExceptions` | "src/Random is listed in NodeAssemblies.DeclaredNamespaceExceptions, and no type of its own assembly still uses the exempted namespace PropStruct.MutationStaleException: the rename has landed, remove the entry" — proves the exactly-that-set assertion, not just a membership check |
| 2026-09-20 | `DeclarationTests` (nonexistent member) | Added `MutationNonexistentMethod` under `ResultsJson`'s ✅ block in `src/Output/API.md` | "src/Output/API.md: ResultsJson has no member named MutationNonexistentMethod, declared under ✅" |
| 2026-09-20 | `DeclarationTests` (nonexistent type) | Added a `MutationNonexistentType` class to the same ✅ block | "src/Output/API.md: declares the type MutationNonexistentType under ✅, and no assembly of the tree has it" |
| 2026-10-02 | `DeclarationTests` (tuple-returning method, before the fix: the gap) | Renamed `HighestDensityRegion` to `HighestDensityRegionX` in the ✅ block of `tests/Harness/API.md` | None: 16/16 green, the line was skipped by the parser |
| 2026-10-02 | `DeclarationTests` (tuple-returning method, after the fix) | The same rename | "tests/Harness/API.md: BinomialBand has no member named HighestDensityRegionX, declared under ✅" (the type is the one the block opened last: the ✅ block is a flat stream) |
| 2026-10-02 | `ApiDeclarationsTests` (`new` modifier before a tuple return type) | Replaced `"new"` by `"new-removed"` in `ApiDeclarations.IsNewModifier` | `ANewModifierBeforeATupleReturningMethodYieldsItsName` fails: expected `["Derived", "Band", "Edge"]`, actual `["Derived"]`; the other six pass |
| 2026-09-20 | `DependencyTests` (unresolved link) | Changed `src/Random/BOOT.md`'s `## Dependencies` from `None.` to `[Ghost](../Ghost/API.md)` | "src/Random/BOOT.md links ../Ghost/API.md under ## Dependencies, and no node has that API.md" |
| 2026-09-20 | `DependencyTests` (declared but unused) | Changed the same section to `[Input](../Input/API.md)` (a real, but unused, dependency) | "src/Random/BOOT.md declares src/Input, but no type of src/Random refers to it" |
| 2026-09-20 | `DependencyTests` (undeclared use, before `claude/wave7@22a9568` closed it) | None needed: `tests/Output.Tests` → `src/Execution` and `tests/Statistics.Tests` → `src/Input` were real, pre-existing undeclared uses | "tests/Output.Tests/BOOT.md does not declare src/Execution, but tests/Output.Tests uses its types: … AcceleratorInfo, … AcceleratorKind" |
| 2026-09-20 | `InvariantTests.NumericalNodesHoldNoSinglePrecisionValueOrOperation` | Added `PropStruct.Statistics.MutationFloatType.Ghost`, a `public static float`, to `src/Statistics` | "src/Statistics: PropStruct.Statistics.MutationFloatType, field Ghost is Single" |
| 2026-09-20 | `InvariantTests.NumericalNodesHaveNoMutableStaticField` (non-readonly) | Added a `public static int Ghost` (no `readonly`) to `src/Statistics` | "…MutationStaticFieldType.Ghost is a static field that is neither const nor readonly" |
| 2026-09-20 | `InvariantTests.NumericalNodesHaveNoMutableStaticField` (mutable array) | Added a `public static readonly int[] Ghost` to `src/Statistics` | "…MutationStaticArrayType.Ghost is a static readonly array, whose elements are mutable state" |
| 2026-09-20 | `InvariantTests.OnlyTheExecutionNodeNamesCudaTypes` | Added a field of type `ILGPU.Runtime.Cuda.CudaAccelerator` to a new type in `src/Particle` (already an ILGPU consumer) | "src/Particle: PropStruct.Particle.MutationCudaType names ILGPU.Runtime.Cuda.CudaAccelerator" |
| 2026-09-21 | `InvariantTests.NumericalNodesHoldNoSinglePrecisionValueOrOperation` (single-precision exemption: a float in a *different* method of the same node) | Added `private static double MutationFloatInAnotherMethod(double value) => (double)(float)value;` to `Attempt` in `src/Particle` | "src/Particle: PropStruct.Particle.Attempt.MutationFloatInAnotherMethod performs single-precision operations (conv.r4)" — the exemption matches by method name and does not spill onto a neighbour method of the same type |
| 2026-09-21 | `InvariantTests.NumericalNodesHoldNoSinglePrecisionValueOrOperation` (single-precision exemption: a second `conv.r4` inside the exempted method itself) | Added a second, distinct `(float)` conversion inside `Attempt.AddReal4`, folded into the return value (net zero, not a compile-time constant, so the compiler could not discard it as dead code the way a bare `_ = (float)term;` was — that first attempt at this mutation was itself eliminated by the compiler and passed the fact for the wrong reason, so it was not the mutation kept) | "src/Particle: PropStruct.Particle.Attempt.AddReal4 is the declared single-precision exemption (root BOOT.md, Taboos), permitted exactly 1 `conv.r4` and nothing else, but has 2 `conv.r4` — the exemption covers exactly the declared accumulator rounding, nothing more" — proves the exemption counts occurrences, not merely presence, so it can tell two apart |
| 2026-09-21 | `InvariantTests.NumericalNodesHoldNoSinglePrecisionValueOrOperation` (single-precision exemption: its target removed) | Renamed `Attempt.AddReal4` to `Attempt.AddReal4Renamed` everywhere in `src/Particle/Attempt.cs` (all eighteen occurrences, declaration and every call site) | Two lines, not a silent pass: "src/Particle: PropStruct.Particle.Attempt.AddReal4Renamed performs single-precision operations (conv.r4)" (the renamed method is no longer exempted, since the exemption matches by name) and "src/Particle: PropStruct.Particle.Attempt.AddReal4 is listed in PermittedSinglePrecisionSites, and no such method exists in that node's assembly: the exemption names a site that must be present to be exempted, not a standing permission (remove the entry, or restore the site)" — proves the exemption cannot outlive its own target and turn this fact vacuously green |

| 2026-09-25 | `NoSuppressionGuardTests.NoSourceFileDisablesAWarningWithPragma` | Added a scratch file `_guard_probe.cs` at the repository root with `#pragma warning disable CS0168` | "these files contain '#pragma warning disable': _guard_probe.cs" |
| 2026-09-25 | `NoSuppressionGuardTests.NoSourceFileCarriesASuppressMessageAttribute` | Added a scratch file `_guard_probe.cs` with `[SuppressMessage("Category", "CA0000")]` on a throwaway type | "these files apply a SuppressMessage attribute: _guard_probe.cs" |
| 2026-09-25 | `NoSuppressionGuardTests.NoProjectAddsANoWarnBeyondTheSdksOwnDefaults` | Added a scratch `_guard_probe.csproj` at the repository root with `<NoWarn>1701;1702;CA1234</NoWarn>` | "_guard_probe.csproj: <NoWarn>1701;1702;CA1234</NoWarn> suppresses CA1234, beyond the SDK's own 1701/1702" |
| 2026-09-25 | `NoSuppressionGuardTests.NoEditorConfigLowersAnAnalyzerSeverity` (superseded the same day: renamed `NoEditorConfigLowersAnAnalyzerSeverityBelowWarning` once the root `.editorconfig` existed and carried real `:warning` lines — root BOOT.md, "Language and build", decided 2026-09-25 — so a guard for no per-rule severity at all stopped being what the tree needed; a guard for no per-rule severity below warning is) | Added a scratch `_guard_probe_dir/.editorconfig` with `dotnet_diagnostic.CA1234.severity = none` | "these .editorconfig lines set a per-rule severity: _guard_probe_dir/.editorconfig: dotnet_diagnostic.CA1234.severity = none" |
| 2026-09-25 | `NoSuppressionGuardTests.NoEditorConfigLowersAnAnalyzerSeverityBelowWarning`, proven by `TheLoweringCheckCatchesADeliberatelyLoweredSeverity` | In-test synthetic strings through the shared `FindLoweredSeverities`, not a scratch file (the check now runs against the tree's own real, populated `.editorconfig`, so a scratch-directory probe would prove the walker finds a second file, not that the severity comparison itself catches a lowering): `dotnet_diagnostic.IDE0008.severity = silent`, `dotnet_analyzer_diagnostic.category-Style.severity = suggestion`, `csharp_style_var_elsewhere = true:none` | each of the three yields exactly one match; the same function returns none for `true:warning`, `severity = error` and a line with no `:severity` suffix at all — proves the check tells "lowered" from "raised or absent", not just "a severity was written" |
| 2026-09-25 | `NoSuppressionGuardTests.RootEditorConfigKeepsTheCategoryStyleWarningLine` | None needed: the root `.editorconfig` did not exist yet when this fact was written (this node's own coding task, root BOOT.md's IDE-style decision, still in progress) | "C:\...\.editorconfig is missing; root BOOT.md's own IDE-style decision lives here." — the real gap the file's own creation closes in the same commit as this row |
| 2026-09-25 | `NoSuppressionGuardTests.DirectoryBuildPropsKeepsTheAnalyzerDecision` | None needed: `Directory.Build.props` had not yet gained `AnalysisMode`/`EnforceCodeStyleInBuild` when this fact was written (this node's own coding task, step 5, still in progress) | "Directory.Build.props no longer sets <AnalysisMode>All</AnalysisMode>" — the real gap the flip closes in the same commit as this row |
| 2026-09-25 | `NoSuppressionGuardTests.TheDefaultScopingExceptionIsNarrow` | In-test synthetic `.editorconfig` texts through the shared `FindLoweredSeverities` (the same reason `TheLoweringCheckCatchesADeliberatelyLoweredSeverity` uses synthetic strings rather than a scratch file): the documented `[*.cs]` `default` / `[src/**.cs]` `warning` shape for `IDE0005`; the same shape with the `[src/**.cs]` section removed; the same shape for `IDE0008` instead of `IDE0005`; the shape inverted (`[*.cs]` `warning`, `[src/**.cs]` `default`) | the documented shape returns no match; each of the other three returns exactly one — proves the exception fires only for the one named rule ID, only when a narrower section actually raises it back, and never inside the narrower section itself |
| 2026-10-04 | `QuickstartExampleTests.TheFourPlacesQuoteTheSamplesRegionsByteForByte` (root `API.md`) | `EpsDok = 0.02` changed to `0.03` in the block of `API.md` ("## Entry points") only | "the root API.md ('## Entry points') differs from the sample's region at line 8" |
| 2026-10-04 | the same (`docs/nuget/PropStruct.md`) | the same edit in the package README's `Quickstart` block only | "docs/nuget/PropStruct.md (Quickstart) differs from the sample's region at line 3" |
| 2026-10-04 | the same (`README.md`) | the same edit in the repository README's `Quickstart` block only | "README.md (Quickstart) differs from the sample's region at line 3" |
| 2026-10-04 | the same (the sample) | the same edit in the region of `samples/Quickstart/Program.cs` only | three lines, one per quoting place of the body: the root `API.md`, `docs/nuget/PropStruct.md` and `README.md` |

Every scratch path above was created outside any project reference (loose files at the
repository root or in a throwaway directory, never added to `PropStruct.sln` or staged),
observed with a filtered `dotnet test tests/Protocol.Tests` run, then deleted; `git
status --porcelain` on each path returned empty again before the next mutation and
before this commit.

Not exercised on this tree: the "declares its descendant" branch of `DependencyTests`
(no node here has a descendant node with its own project to construct the case from)
and the "no C# block under ✅ was found" fallback of `DeclarationTests` (every node's
`API.md` genuinely has at least one, so the fallback never fires on real input; it
exists to catch the parser losing the documents entirely, not a per-node condition).

## Two gaps of the declaration parser, found and closed 2026-09-20

`DeclarationTests` reads an `API.md` code block as a flat stream of declarations and
attributes each member to the type most recently declared. Two shapes broke that, both
found when `tests/Harness` added declarations in them rather than by review:

- **A type that does not open a body owned everything after it.** `internal readonly
  record struct QuantumEstimate(double Quantum, double Resolution);` inside a static
  class made every following method of that class read as a member of the record, and a
  one-line `enum CalibrationRule { … }` did the same. Ownership now requires the body to
  be left open at the end of the line (`ApiDeclarations.OpensBodyAt`); a bodiless record
  owns only the positional parameters on its own line, which carry `FromTypeLine`, and
  after them the enclosing type is the owner again.
- **A method whose return type is a tuple read as a member named after a modifier.**
  `internal static (long Low, long High) BinomialBand(…)` matched `static (` and produced
  a member called `static`. The modifiers are now in the parser's keyword set.

Both were reported by the check as a document defect when the document was right, which
is the worse failure of the two kinds: a check that cries wolf teaches its reader to edit
the document until the check is quiet. The fix is the parser's, not the document's.

⚠ 2026-10-02: was "The modifiers are now in the parser's keyword set", read as the
tuple-returning declarations being checked; they were not: the line was skipped, so a
renamed method passed (seven declarations of `tests/Harness/API.md`). Now the method name
is the word after the tuple's balanced closing parenthesis, over several lines too
(`ApiDeclarations.TupleReturningMethod`, `ApiDeclarationsTests`); found by the
orchestrator reading `IsKeyword`.

⚠ 2026-10-02, the same day: the fix above left one shape unread: `new` is in
`IsKeyword` but not in `IsModifier`, so `public new (int A, int B) Band(int count);`
was still skipped (found by running this parser beside the kit's reference parser).
`new` is a modifier only when nothing but modifiers precede it on the line
(`ApiDeclarations.IsNewModifier`); as an expression keyword (`= new (0, 0);`) it is
still never a name, and no other line yields anything different: the declarations
read across the tree's ✅ blocks are 536 before and after, the same list
(the 535 above is the count at `fb640f7`; the one added since is
`BatchStatus.ParticleNotRun` in `src/Execution/API.md`, `3eaa2ee` merged by
`5f0bdf7`). Red:
`ApiDeclarationsTests.ANewModifierBeforeATupleReturningMethodYieldsItsName` fails on
the old parser; the mutation is in "Mutation record".

## Declared deviation: tests/Harness namespace (AGENTS.md §12)

⚠ 2026-09-20. **Article not satisfied:** root `BOOT.md`, Constraints — "namespaces
mirror the directory path (AGENTS.md §1) ... under the root namespace `PropStruct`".
`tests/Harness` and `tests/Harness.Tests` violate it: every other node of the tree
drops `src`/`tests` from its namespace (`src/Cli` → `PropStruct.Cli`,
`tests/Cli.Tests` → `PropStruct.Cli.Tests`, …), but these two keep a literal `Tests.`
segment (`PropStruct.Tests.Harness`, `PropStruct.Tests.Harness.Tests`).

**Reason.** Found by `CoverageTests.EveryTypeOfEveryAssemblyLivesInTheNamespaceOfItsNode`
the first time it ran against the real tree (2026-09-20), and also the root cause of
most of that day's `DependencyTests` failures: every node using `RepositoryPaths`,
`CpuHost`, `BitSnapshot`, etc. from `tests/Harness` had its use mis-attributed to "the
tree root" (the only node whose namespace still prefix-matched `PropStruct.Tests.…`),
which in turn made each node's real, correctly declared `tests/Harness` dependency
look unused. `tests/Harness/API.md` names the namespace as if deliberate ("Namespace
`PropStruct.Tests.Harness`"), but the choice is nowhere argued, and the sibling
project this whole tree is ported from
(`AerospacePropellantThermodynamics/tests/Harness`) uses `APThermo.Harness` for the
equivalent node — so this reads as incidental, not considered (owner's decision,
2026-09-20, coordinating this and the `tests/Harness` design session): the namespace
becomes `PropStruct.Harness` / `PropStruct.Harness.Tests`, matching every other node.
The rename is not done here: a concurrent session is inside `tests/Harness`, and a
namespace rename from this node would collide with it (AGENTS.md §3: `tests/Harness`
is a neighbour, its code is not this node's to write in any case).

**What replaces it meanwhile.** `NodeAssemblies.DeclaredNamespaceExceptions`, a map
from a node's relative path to the one exempted namespace string, named and read by
both `NodeOf(Type)` (so `DependencyTests` and `DeclarationTests` attribute these two
nodes' types correctly rather than to the root) and `CoverageTests` (so the
namespace-mirroring fact still reports the exemption instead of silently passing
everything). The exception is not a blanket exclusion: `CoverageTests` collects which
of the two declared entries a real type actually needed and asserts, after the walk,
that the set is exactly `{tests/Harness, tests/Harness.Tests}` — a type anywhere else
under a namespace this map does not name still fails the fact (proven,
"Mutation record": `MutationWrongNamespaceType`), and an entry no type still needs
fails it too, naming the stale entry (proven: the `src/Random` bogus-entry mutation).

**What lifts it.** The `tests/Harness`/`tests/Harness.Tests` rename to
`PropStruct.Harness`/`PropStruct.Harness.Tests` (tracked as a background task,
"Fix tests/Harness namespace to mirror its directory path"). When it lands,
`CoverageTests` turns red on its own (both entries become stale, per the mechanism
above) until both entries are deleted from `DeclaredNamespaceExceptions` and this
section is deleted from this document.

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.

## Taboos

- No check marked skipped; a check that cannot be green is fixed or removed with a
  declared deviation. `tests/Harness`'s namespace is neither: it is a real,
  reported finding in a neighbour node, carried as the declared deviation above
  (AGENTS.md §12) rather than left as a hidden, unexplained red fact or silently
  patched around.
