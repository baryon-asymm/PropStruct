# BOOT.md — Output.Tests

## Purpose

The definition of what "`Output` is ready" means.

| Level | What it checks | Against what | State |
|---|---|---|---|
| L0 | every formatting primitive (list-directed REAL*4 and INTEGER*8 shapes, each edit descriptor used) | strings cut from the reference `results.m` files, each with its file and line | ✅ 2026-09-19, `FortranFormatTests` (26 cases) |
| L0 | formatting is unaffected by the current thread culture | a decimal-comma `CultureInfo`, built without a named-culture lookup (`## Constraints`) | ✅ 2026-09-19, `CultureInvarianceTests` (7 cases) |
| L0 | the print-time arithmetic of lines 1184–1453 is covered expression by expression | a list generated from the source by a script, mapped to the writer's code and to the result members that carry it computed | ✅ 2026-10-02, `PrintExpressionGeneratorTests` (3 cases) and `PrintExpressionMapTests` (9 cases); `src/Output/API.md`, "## Print-expression map" |
| L1 | the port's `results.m` for each reference formulation equals the original's in variable names, comment lines and declared order; array lengths and number shapes are *not* compared here (see below) | the reference files, through a line-by-line diff against a live reference-mode run (not `tests/Harness`' parser: the parser is for cell values, this level is for the file's static shape) | ✅ 2026-09-19, `StructuralTests` (5 reference formulations) |
| L1 | the precision-kind footer line (added 2026-09-21) explains itself rather than printing a bare enum name, differs between `Binary64` and `Original` nowhere but itself, and stays a plain excluded comment — reusing `StructuralTests`' own classification, not a second copy of it | a synthetic `SimulationResult` (`JsonRoundTripTests.BuildSample()`, patched to finite values: that sample's own `NaN`/infinities, deliberate for the JSON round-trip row above, made `ResultsMWriter` throw building an exponent for a non-finite value) written twice, once per kind | ✅ 2026-09-21, `PrecisionFooterLineTests` (4 cases) |
| L1 | the stream-layout and execution-mode footer lines (added 2026-09-21, beside the precision-kind line) each explain themselves rather than printing a bare enum name, each differs between its own two values nowhere but itself, and each stays a plain excluded comment — reusing the same classification and the same sample builder/write helper as the row above, not a second copy of either; the "changes only its own line" and "explains itself" cases assert the *correct* distinguishing wording per value, not only that some line differs (root BOOT.md's project rule, "each asserted by content and not by inequality") | the same synthetic `SimulationResult`, patched per `Streams`/`Mode` value instead of `Precision` | ✅ 2026-09-21, `RunProvenanceFooterLineTests` (8 cases) |
| L1 | JSON round-trips bit for bit | the same result written, read and written again | ✅ 2026-09-19, `JsonRoundTripTests` (3 cases) |
| L2 | the `fmkarm`/`fqkarm`/`fmkarm_cor`/`fmkarm_cor2`/`fqkarm_cor` print-length formula (`int(DpMax/Di) + 2`, `int(DpMaxCor/Di) + 2`) itself — narrower than L1's excluded array lengths: this checks the *formula*, not a byte-for-byte length match | the archived reference lengths, through `tests/Harness`' cell parser, on all five reference formulations; the one known formula-independent miss (HMX's `DpMaxCor`-driven trio) is measured explicitly, not excluded silently | ✅ 2026-09-21, `PocketPrintLengthTests` (5 + 4 + 1 cases; `src/Statistics/BOOT.md`, "## Report", the resolved print-length escalation) |
| L2 | the tail-probability header line and the `Dokb_max` gate read the original's own already-reduced `alfa` (`RunHeader.TailProbabilityModified`), not the raw menu parameter (O-1, fidelity audit) | two synthetic cases isolating the value source and the gate direction, plus a real nonzero-tail-probability original run for the positive control | ✅ 2026-09-24, `TailProbabilityHeaderTests` (5 cases; `src/Output/BOOT.md`, "## Tail-probability header line and gate") |
| L2 | `coef`/`fqmkm1`/`fqmkm2` print `nmax + 2` values padded with zeros beyond their own `Nc`-sized array, not truncated to it (O-2, fidelity audit) | three synthetic shorter-than-`nmax + 2` arrays, one per name, plus the archived `hp2.m.txt`'s own overflow (`Legacy/outputs/hp2.m.txt`, 1002 `coef` values) | ✅ 2026-09-24, `ArrayPaddingTests` (4 cases; `src/Output/BOOT.md`, "## Array-length divergence, measured") |

⚠ 2026-09-19: the L1 structural row first read "…array lengths and number shapes,
line by line". Measured against a live reference-mode run of every reference
formulation: array lengths tied to a running maximum (`Ndok`, `DPmax`, `DPRow`,
`coef_nmax`, …) and the printed digit shape of accumulated scalars can differ between
a live run and the archived reference even for correct code (`src/Output/BOOT.md`,
"Array-length divergence, measured"; `StructuralTests`'s own class doc carries the
measured numbers). The row is narrowed to what is actually invariant — names, static
comment text and declared order — with value and length agreement left to the
statistical criterion of `tests/Simulation.Tests`, which compares distributions
across replicas rather than one archived run.

## Invariants

- Expected strings come from `tests/Fixtures`, never typed into a test.
- The structural comparison excludes the time line and, added 2026-09-21, the
  precision-kind, stream-layout and execution-mode footer lines — the four
  port-specific comment lines with no counterpart in the original — the same way
  `tests/Harness` already excludes the time line from its own parsing. None of the
  three option lines carries a digit, so none can be mistaken for a comment-derived
  quantity by a downstream parser; that is a property of the lines' own text, checked
  here (`PrecisionFooterLineTests`, `RunProvenanceFooterLineTests`), not a claim
  this node makes about `tests/Harness`'s code, which it does not read (AGENTS.md §3).
- Formatting is checked under at least one non-invariant culture (decimal comma) as well.

  ⚠ 2026-09-19: the repository builds with `InvariantGlobalization=true`
  (`Directory.Build.props`, outside this node), under which a *named* culture (e.g.
  `ru-RU`) cannot be looked up — `CultureInfo.GetCultureInfo` throws
  `CultureNotFoundException`. The invariant still holds and is still checked: a
  decimal-comma `CultureInfo` is built by cloning the invariant culture and
  overwriting only its `NumberFormat.NumberDecimalSeparator`, which needs no culture
  database lookup and still exercises exactly what the invariant is about — a
  primitive that omits its explicit `CultureInfo.InvariantCulture` argument
  (`CultureInvarianceTests`, proven by mutating `FortranFormat.FixedPoint` to drop it:
  red with `130,68` instead of `130.68`, then reverted).

## Dependencies

- [Output](../../src/Output/API.md) — what is being checked.
- [Simulation](../../src/Simulation/API.md) — results to write.
- [Input](../../src/Input/API.md) — the reference formulations.
- [Execution](../../src/Execution/API.md) — `AcceleratorInfo` and `AcceleratorKind`,
  which the run diagnostics of a result carry and the writers print.

  ⚠ 2026-09-20: this section did not declare `Execution` while the tests used both
  types. Found by `tests/Protocol.Tests`' dependency check on the day it was first run;
  the use predates the check, the declaration was simply missing.
- [Harness](../Harness/API.md) — `results.m` parser, repository paths, `PythonScript`.
- [Fixtures](../Fixtures/API.md) — the reference outputs.

Outside the tree: xunit; Python 3.8+, for the generator's tests.

## Constraints

- Part of the default test command; runs over whole reference formulations are marked
  `Category=Long` if they take more than a few seconds, measured before deciding.

## Acceptance criteria

- [x] 2026-09-21: every row of the levels table **except the generated print-arithmetic
      list** is green, with a date and the names of the tests (table above).

  ⚠ 2026-09-20: the first criterion read "every row of the levels table is green" and
  was ticked on 2026-09-19 while the print-arithmetic row of the table stood ⏳. A tick
  over a row that is not green is exactly the tick AGENTS.md §6 forbids: it was the
  whole-table wording that was wrong, not the evidence of the other rows, which is
  unchanged and re-dated to the day it was obtained. The open row is now its own
  criterion, unticked.

  ⚠ 2026-09-21: was ticked again the same day the accumulation-kind row was added,
  with that row's own evidence still unread — the same mistake the note above already
  named. Unticked, then re-ticked the same day once `src/Simulation`'s coding session
  merged its finished `AccumulationKind`/`SimulationOptions.Accumulation`/
  `RunDiagnostics.Accumulation` onto this branch (an agent's working branch,
  decided with the root): `dotnet build` succeeds and every row's own tests are green,
  including the new one — `AccumulationFooterLineTests`, 4 cases, and the whole node's
  fast run (`Category!=Long`), 40 of 40 cases.

  ⚠ 2026-09-21, later the same day: the stream-layout and execution-mode row was added
  (root BOOT.md, "Invariants": both change the printed numbers by more than the
  accumulation kind does, and only the console recorded them until now). `dotnet build`
  succeeds and every row's own tests are green, including the new one —
  `RunProvenanceFooterLineTests`, 8 cases, and the whole node's fast run
  (`Category!=Long`), 48 of 48 cases; the Long-only `StructuralTests`, 5 of 5, unaffected
  by the new footer lines (the structural comparison excludes them, "## Invariants"
  above).

  ⚠ 2026-09-24: two more L2 rows added, the tail-probability header line/gate (O-1) and
  the `coef`/`fqmkm1`/`fqmkm2` zero-padding (O-2), both found by a fidelity audit and
  fixed the same day. `dotnet build` succeeds and every row's own tests are green,
  including the two new ones — `TailProbabilityHeaderTests` (5 cases, 1 `Category=Long`)
  and `ArrayPaddingTests` (4 cases) — and the whole node's fast run (`Category!=Long`),
  59 of 59 cases.
- [x] 2026-10-02: the print-arithmetic row: a script generates the list of expressions
      of Fortran lines 1184–1453 from the source, and the test compares the writer
      against it (`PrintExpressionGeneratorTests`, `PrintExpressionMapTests`; upstream
      rows against `src/Simulation/API.md`; right proofs P1–P4; red: "## Mutations"
      12–25).
- [x] The checks of this node are proven non-degenerate by recorded mutations
      (AGENTS.md §13), each test class by the entries and the date named here, none
      by a re-run of the whole list: a class not named here has no recorded proof.
      - 2026-09-19, "## Mutations" 1–5: `FortranFormatTests` (1), `JsonRoundTripTests`
        (2), `StructuralTests` (3–4), `CultureInvarianceTests` (5).
      - 2026-09-21: `PocketPrintLengthTests` (6), `PrecisionFooterLineTests` (7),
        `RunProvenanceFooterLineTests` (8–9).
      - 2026-09-24: `TailProbabilityHeaderTests` (10), `ArrayPaddingTests` (11).
      - 2026-10-02: `PrintExpressionGeneratorTests` and `PrintExpressionMapTests`
        (12–25, with right proofs P1–P4 below the list).
      - `EpsHeaderEchoTests` (2026-09-23), `PocketCoefficientHeaderEchoTests` and
        `StoredSetupSourceReadTests` (2026-09-24): red evidence lives in
        `src/Output/BOOT.md` (`## Header echo`, the stored-setup section), not in
        this node's list.

      Mutations 1–11 were not re-run on 2026-10-02, and the code they mutate was
      renamed or restyled since (`15d231c`, `28f6235`, no change of meaning
      claimed); only 12–25 were obtained that day.

      ⚠ 2026-10-02: was "every check is proven non-degenerate by a recorded
      mutation", dated 2026-09-21, the last of mutations 1–9; mutations 10–11 had
      been added on 2026-09-24 and 12–25 on 2026-10-02 (`PrintExpressionMapTests`,
      `PrintExpressionGeneratorTests`) without the claim or its date moving, and
      three test classes carry their proof outside this list. An "all" claim
      cannot stand over a list nobody generated (AGENTS.md §6); reformulated to
      name what each date covers instead of re-running 25 mutations, the cheaper
      honest course. Found by reading the criterion against `## Mutations`.

⚠ 2026-09-24: the test names cited above were renamed for CA1707 (underscores removed
from method names, no change of meaning); the old → new map is
`tests/test-renames-2026-09-24.txt`. No criterion's date moved.

- [x] The `verify` and `selftest` facts of `PrintExpressionGeneratorTests` are
      `Category=Legacy` and the node's other facts read committed data only (stage S2,
      2026-10-04: the 2 facts green with the variable set, 0 of 2 with it unset; 74
      cases of the fast set with the original absent).

## Taboos

- No value comparison here: values are the statistical criterion's, in
  `tests/Simulation.Tests`.

## Mutations

Every row of the levels table above has been shown red under a real code mutation,
then reverted and confirmed green again (AGENTS.md §13), on 2026-09-19:

1. **`FortranFormat.SingleRealCore`, fixed-point significant digits 7 → 6.**
   `FortranFormatTests` failed on every fixed-point case (wrong digit count against
   the cut fixture strings). Reverted; green.
2. **`ResultsJson`'s `DoubleMatrixConverter` registration removed from
   `JsonSerializerOptions.Converters`.** `JsonRoundTripTests` failed (`double[,]` has
   no built-in converter, so serialization threw). Reverted; green.
3. **`ResultsMWriter`'s `Nbase` line, `"+"` → `"+"` with the leading separating blank
   removed (a literal-after-number transition missing its blank).** Not caught at
   first: `StructuralTests`'s echo-section cut point was then `"% Cycles:"`, and
   `Nbase` fell just after it, outside the byte-shape-checked echo section. Since
   `Nbase` is itself fully determined by the formulation (`KPRIS`, `N`, `FI = Cycles *
   ParticlesPerCycle`, no accumulation over any particle), the echo section's cut
   point was moved to `" Nkarm ="` (the first RNG-accumulated quantity, `QKSS`) so the
   echo section covers every formulation-only line. Re-applied the same mutation:
   `StructuralTests` now fails on all five formulations (echo-section shape mismatch
   at the `Nbase` line). Reverted; green. Recorded here because the miss, not just the
   mutation, is the evidence this check is now non-degenerate.
4. **`ResultsMWriter`'s "Pockets parameters" comment banner text mutated to "Pocket
   parameters".** `StructuralTests` failed on all five formulations (comment-banner
   list mismatch at that position). Reverted; green.
5. **`FortranFormat.FixedPoint`'s `CultureInfo.InvariantCulture` argument dropped.**
   `CultureInvarianceTests.FixedPointUnaffectedByCurrentCulture` failed under the
   decimal-comma culture (`130,68` instead of `130.68`). Reverted; green.
6. **`ResultsMWriter`'s `dpCorLength`, `+ 2` → `+ 3`, on 2026-09-21.**
   `PocketPrintLengthTests` failed on five of its ten cases: the four formulations
   whose corrected-family length matched the archive under the real formula now missed
   by one cell each (HPEPA3 67 against 66, inpt 49/48, P33 14/13, PSAN02n 37/36), and
   HMX's own-formula check failed too (83 against its own `DpMaxCor`-derived 82) —
   every case built on `port["…cor…"].Length` caught the mutation; the plain `DpMax`-
   driven pair (`fmkarm`/`fqkarm`) was unaffected, as it should be. Reverted; green.
7. **`StructuralTests.IsComparableCommentBanner`'s `"% Accumulation:"` exclusion
   removed, on 2026-09-21** (once `src/Simulation`'s implementation had merged onto
   this branch and the whole node could build). `PortResultsM_MatchesOriginal_
   InNamesAndCommentsInOrder` failed on all five reference formulations (the
   accumulation-kind footer line, absent from every archive, now entered the
   comparable-comment list) and two of `AccumulationFooterLineTests`' own cases failed
   with it (`Assert.False(StructuralTests.IsComparableCommentBanner(line))`) — proving
   the new L1 row's own check, not only the pre-existing one, is exercised by removing
   this exclusion. Reverted; all nine green again.
8. **`StructuralTests.IsComparableCommentBanner`'s `"% Stream layout:"` and
   `"% Execution mode:"` exclusions removed, on 2026-09-21** (same shape as mutation 7,
   for the two lines added the same day). Fast set: four of `RunProvenanceFooterLineTests`'
   own cases failed (the two `IsAPlainCommentBanner` theories, both values each) —
   `Assert.False(StructuralTests.IsComparableCommentBanner(line))` directly, no
   simulation needed. Long set: `PortResultsMMatchesOriginalInNamesAndCommentsInOrder`
   failed on all five reference formulations (both new footer lines, absent from every
   archive, now entered the comparable-comment list). Nine cases red in all, the same
   count as mutation 7. Reverted; all green again.
9. **`ResultsMWriter.StreamLayoutCommentLine`'s two branches' message bodies swapped, on
   2026-09-21** — the mutation the row's own "asserted by content, not by inequality"
   wording exists to catch: the two lines still differ from each other after the swap,
   so a check that only diffs the file would stay green. Exactly two cases failed, each
   on a specific missing substring rather than a bare inequality:
   `StreamLayoutLineExplainsItselfRatherThanPrintingABareEnumName` (the `Original`
   line no longer contained `"original program"`) and
   `ChangingTheStreamLayoutChangesOnlyItsOwnFooterLineWithTheCorrectWording` (the
   surviving differing line no longer contained `"known statistical bias"` on the
   `Original` side). The other six `RunProvenanceFooterLineTests` cases, which do not
   assert which value's text is which, stayed green — confirming the mutation is caught
   by the positive-control assertions specifically, not by every case in the file.
   Reverted; all green again.
10. **`ResultsMWriter`'s tail-probability header line and `Dokb_max` gate reverted to
    reading `parameters.TailProbability`, on 2026-09-24 (O-1).** All three synthetic
    `TailProbabilityHeaderTests` cases failed: the header-line case on string content
    (raw `1.000000000000000E-002` printed instead of the header's own `7.300000000000000E-003`),
    and both rows of the gate theory on the opposite boolean (a raw value that should
    have gated the line off left it on, and one that should have gated it on left it
    off). The fixture-backed case failed too, on the same shape (`port printed 0.01,
    archive printed 0.009266132883049108, relative difference 7.920E-002`, far past the
    `1e-8` tolerance). Reverted; all five green again.
11. **`ResultsMWriter`'s `Take` reverted to clamping (`Math.Min(length, values.Length)`
    sized result array) instead of padding, on 2026-09-24 (O-2).** The three synthetic
    `ArrayPaddingTests` cases failed on length (`coef` 3 instead of 6, `fqmkm1` 2
    instead of 5, `fqmkm2` 1 instead of 4) — the fixture-reading case, independent of
    `ResultsMWriter`, stayed green throughout, confirming it exercises the archive's own
    evidence and not the writer under test. Reverted; all four green again.
12. **`PrintExpressionSites.txt`, the row of 1279 deleted, on 2026-10-02.**
    `EveryGeneratedRowHasExactlyOneMapRow` failed (`1279 | item | doksd**0.5*1e6`
    without a map row), and `EveryCitedLineCarriesAFragmentOfARowItCites` with it
    (`ResultsMWriter.cs:204` cites 1279, no ported row left). Reverted; 12 of 12 green.
13. **A bogus row `1280 | item | doksd*1e6` added, on 2026-10-02.**
    `EveryMapRowNamesAGeneratedRow` failed ("map rows naming no generated row") and the
    fragment check with it (no line cites 1280). Reverted; green.
14. **`ResultsMWriter`, the `Dmin` line, `1.00001e6` → `1e6` (Fortran 1204), on
    2026-10-02.**
    `EveryFragmentOfAPortedRowIsOnACitingLineAndTheFirstInsideItsMember`
    failed (no line citing 1204 carries `Trunc(storedSetup.Dmin * 1.00001e6)`) and
    `EveryCitedLineCarriesAFragmentOfARowItCites` with it (`:107`). Reverted; green.
15. **`ResultsMWriter`, the line of `conditions[0]`, `fiPlusN` → `fi` (1248), on
    2026-10-02.** Both checks of 14 failed, on 1248 and `:176`. Reverted; green.
16. **`ResultsMWriter`, the line of `conditions[5]`, `fi` → `fiPlusN` (1253), on
    2026-10-02.** Both checks of 14 failed, on 1253 and `:181`. Run again with the
    matcher's trailing token bound removed, so that `conditions[5] / fi` is a plain
    prefix of `conditions[5] / fiPlusN`: 12 of 12 green. The bound is what sees the
    swap. Reverted both; green.
17. **`ResultsMWriter`, the `Dokb_max` gate, `> 0` → `>= 0` (1272, where O-1 sat), on
    2026-10-02.** Both checks of 14 failed, on 1272 and `:197`. Reverted; green.
18. **`// Fortran 1230` added to the `Nbase` line, on 2026-10-02.**
    `EveryCitedLineCarriesAFragmentOfARowItCites` failed alone (`:157` cites 1230, which
    has no ported row). Reverted; green.
19. **`// Fortran 1396` deleted from the `dokkarm43` line, on 2026-10-02.**
    `EveryFragmentOfAPortedRowIsOnACitingLineAndTheFirstInsideItsMember`
    failed alone (no line citing 1396 carries `ScaleByMicron(pockets.Dokp43)`; its
    second fragment, in `ScaleByMicron`, is still cited). Reverted; green.
20. **One byte of `PrintExpressions.generated.txt` edited (`dmin` → `dmim`, row 1204),
    on 2026-10-02.** `ListReproducesByteForByteOnRegeneration` failed (`verify` exited
    1), and the two map directions with it, on that row. Reverted; green.
21. **`list-print-expressions.py` made to skip argument 3 of `arrayprint`, on
    2026-10-02.** Before regenerating, `verify` and `selftest` failed (the constructed
    fragment lost its `length` row). Regenerated with the mutated script (79 rows
    against 89): `EveryMapRowNamesAGeneratedRow` failed on the ten `length` rows,
    `TheThirteenSitesTheDesignNamedBeforeAnyScriptExistedAreRows` on 1370, 1373 and
    1377, and `selftest` still. Reverted both files; green.
22. **Continuation joining turned off in `statements`, on 2026-10-02.** `selftest`
    failed (`9: unrecognised statement '&  p**2'`) and `verify` with it. Reverted;
    green.
23. **The upstream column of 1377, `Histograms.CoefNormalized` →
    `Histograms.CoefNormalised`, on 2026-10-02.**
    `EveryUpstreamMemberExistsOnItsSimulationRecord` failed, and
    `EveryUpstreamMemberAndItsLineStandInOneParagraphOfTheSimulationContract` with it.
    Reverted; green.
24. **The quoted phrase of the `not ported` reason of 1196 changed to one in no
    `BOOT.md`, on 2026-10-02.** `EveryNotPortedReasonQuotesAPhraseOfTheBootItNames`
    failed alone. Reverted; green.
25. **The upstream column of 1377 set to `Histograms.Allvdokso`, on 2026-10-02.**
    `EveryUpstreamMemberExistsOnItsSimulationRecord` stayed green (the member exists)
    and `EveryUpstreamMemberAndItsLineStandInOneParagraphOfTheSimulationContract`
    failed alone: no paragraph of `src/Simulation/API.md` names `Allvdokso` and 1377.
    Reverted; green.

Right proofs of the print-arithmetic row (2026-10-02), each an answer known before the
artefact existed and read where the check reads it. **P1**
(`TheThirteenSitesTheDesignNamedBeforeAnyScriptExistedAreRows`): the thirteen sites of
the design bullet of 2026-09-19 (the conditions of 1248–1256, line 1396, the three
`nmax + 2` lengths) are rows of the committed list, by the design's own words. **P2**
(`RuleSelfTestPasses`): a constructed fragment gives 6 rows and 3 exclusions, and three
real statements join as pinned. **P3**
(`TheMapRowsOfTheThirteenDesignedSitesCarryTheCSharpTheDesignNamed`): the map rows
of the same thirteen carry `fiPlusN`, the token `fi` without `fiPlusN`, `1.00001e6`
and `+ 2`. **P4** (`TheUpstreamRowsAreTheNineTheSimulationContractStates`): the
upstream rows are those of 1202, 1353, 1357, 1364, 1370, 1373, 1377, 1386 and 1418.
