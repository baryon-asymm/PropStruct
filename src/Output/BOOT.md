# BOOT.md — Output

## Purpose

Writes a result as `results.m` in the original's layout (Fortran lines 1184–1453, with
`arrayprint` and `arrayprint2`, lines 1517–1571) so that existing MATLAB
post-processing keeps working, and as JSON for programs. It is apart from `Simulation`
so that the library never depends on formatting.

## Invariants

- `results.m` holds the original's variable names, comment lines, order and formats:
  Fortran edit descriptors (`E9.3`, `E12.6`, `F7.2`, `F6.4`, `I3`…) and list-directed
  output emulated as Digital Visual Fortran prints them, six values per line with the
  original's continuation.
- The line of Fortran 1238 is reproduced as printed (`epsx(5)` shows EPS6, `epsx(6)`
  shows EPS7); the JSON carries all seven accuracies under true names.
- `pdoksmall(1)` prints as zero.
- Formatting never depends on the current culture.
- JSON written and read back gives an equal result.
- `RunDiagnostics.Precision` (`src/Simulation/API.md`) is recorded in `results.m` as
  a plain-English comment beside the time line, added 2026-09-21 (root BOOT.md,
  "Precision kind is an option of every run"): a sentence explaining what the run
  did, never the bare enum name, and never a declared MATLAB variable — the original
  has none by that name, so a declared line would break "`results.m` holds the
  original's variable names ... order" above. `ResultsMWriter.PrecisionCommentLine`
  is the one place this text is written. The sentence names the accumulators, the setup
  plane and the per-cycle plane (`src/Statistics/BOOT.md`, "## Setup plane" and "##
  Report"); it named the accumulators alone until 2026-09-23 and the setup plane too
  until 2026-10-01.
- `RunDiagnostics.Streams` and `RunDiagnostics.Mode` are recorded in `results.m` the
  same way, beside the precision-kind line, added 2026-09-21. The gap this closes:
  the console already prints the layout and the mode, but `results.m` — the file a user
  actually keeps — did not, and both change the printed numbers by more than the
  precision kind does (root BOOT.md, "Two stream layouts" and "Known bias of the
  original's seeds": up to double digits of per cent on the pocket and bridge
  quantities; "Reference mode is the original's sequence" against "Batched mode
  freezes only QKS1, per launch": the two modes refresh the pocket histogram on
  different schedules). A user re-running an old command line against a kept
  `results.m` had nothing in the file to say why the numbers moved once the default
  layout changed from `Original` to `Independent` the same day (root BOOT.md, "The
  `Original` layout is sequential only"). Each line is a sentence, not the bare enum
  name, and never a declared MATLAB variable, for the same reason as the precision
  line above. `ResultsMWriter.StreamLayoutCommentLine` and
  `ResultsMWriter.ExecutionModeCommentLine` are the one place each text is written.
- The header echo of the menu parameter `eps` (`ModelParameters.EpsDok`) prints its
  REAL*4 store under the `Original` precision kind, added 2026-09-23; every other
  header echo is unchanged by the precision kind. `## Header echo` below carries the
  finding and the evidence.

## Dependencies

- [Simulation](../Simulation/API.md) — the result.
- [Input](../Input/API.md) — formulation and parameters for the header.
- [legacy](../../tools/legacy/API.md) — `legacy_file`, for the print-expression script.

## Constraints

Inherited from the parent ([BOOT.md](../../BOOT.md)). In addition:

- The formatting primitives are specified by strings cut from the shipped reference
  `results.m`, not by reading the Fortran runtime.

⚠ Declared deviation, §6: the layout specification is the Fortran source, lines 1184–1453 and `arrayprint`/`arrayprint2` 1517–1571, not a self-sufficient retelling, replaced by: ## Print-time expressions, ## Defects of the original.

⚠ 2026-10-04: the source this node's line numbers refer to, named here by its path under `tests/Fixtures/Legacy`, lies outside the repository from stage S2 (`tools/legacy`): a reader of the public tree cannot consult it, and the checks that read it are `Category=Legacy` → tests/Fixtures/HISTORY.md#legacy-out-of-tree-2026-10-04

## Print-time expressions

Scope: Fortran lines 1184–1453 of `tests/Fixtures/Legacy/PropStructv3.for.txt`, read
here by the list's script; every gate and replaced line is a row (decided 2026-10-02,
arbitration delegated by the owner).

**Rule.** Executable statements of the scope, continuations joined (fixed and DVF tab
form), split at a top-level `;`, keyed by their first line; `FORMAT` carries nothing.
Sites: a `write(4,…)` list item (a string item is the context of those after it);
argument 2 or 3 of `call arrayprint`/`arrayprint2`; an `IF`/`ELSE IF` condition; an
assignment's right side; a `DO` bound; an argument of any other `CALL`. Listed when
it holds `**`, `*`, `/`, `+`, `-`, a relational or logical operator, or calls `int`,
`real`, `sum`, `abs`, `sqrt`, `float`, `dble`, `nint`, `mod`, `max`, `min`, `exp` or
`log`. Excluded, named in the list's header: `print`, a `write` to a unit other than
4, a site holding a quote, `//`, `trim` or `len_trim`. Any other statement fails the
script.

**Map.** `list-print-expressions.py` (`generate`, `verify`, `selftest`) writes
`PrintExpressions.generated.txt`, never by hand; `PrintExpressionSites.txt` maps each
row to the `ResultsMWriter` method and C# fragments carrying it, to the result member
carrying it computed (member and line in one paragraph of
[Simulation](../Simulation/API.md)), or to `not ported:` and a quoted `BOOT.md` phrase
saying why. Only fragment lines of `ResultsMWriter.cs` end `// Fortran n[, n…]`. It
proves each transcription present where cited, literals and operands included, not that
nothing else is computed there, nor helpers, nor values (the statistical criterion's).
Checks: `tests/Output.Tests`; grammars: `API.md`, "## Print-expression map".

## Acceptance criteria

- [x] 2026-09-19: for the reference formulations, the port's `results.m` equals the
      original's in names, comments and declared order (`tests/Output.Tests`,
      `StructuralTests.PortResultsMMatchesOriginalInNamesAndCommentsInOrder`, all
      five reference formulations green). Values and the run-dependent array lengths
      are compared by the statistical criterion, `tests/Simulation.Tests`, not here
      (see "Array-length divergence, measured" above for why a byte-for-byte
      comparison against a single archived run cannot be this node's criterion).
- [x] 2026-09-19: every formatting primitive reproduces the strings of the reference
      files it is specified by (`tests/Output.Tests`, `FortranFormatTests`, 26 cases
      each citing its own fixture file and line).
- [x] 2026-09-19: JSON round-trips (`tests/Output.Tests`, `JsonRoundTripTests`:
      `WriteThenReadGivesAnEqualResultFieldByFieldWithBitwiseDoubleEquality`,
      `WriteReadWriteGivesTheSameBytes`, `ReadMalformedJsonThrowsJsonException`).
- [x] 2026-09-21: the `fmkarm`/`fqkarm`/`fmkarm_cor`/`fmkarm_cor2`/`fqkarm_cor`
      print-length formula (`## Array-length divergence, measured`, "The `DPmax`/
      `DPmax_cor` print-length formula itself…") matches the archive on nine of the
      ten reference-formulation lengths it drives; the tenth (HMX's `DpMaxCor`-driven
      trio) is `Particle`'s own accumulator value, not this node's formula
      (`tests/Output.Tests/PocketPrintLengthTests`).
- [x] 2026-09-21: the precision-kind footer line explains itself rather than naming
      a bare enum value, differs between the two kinds nowhere but itself, and stays a
      plain comment excluded from the structural comparison the same way the time line
      is (`tests/Output.Tests/PrecisionFooterLineTests`, 4 cases, run once
      `src/Simulation`'s finished implementation of `PrecisionKind`/
      `SimulationOptions.Precision`/`RunDiagnostics.Precision` merged onto this
      branch — its `API.md` alone, published first so this node's coding session did
      not have to wait, was not enough to build against). Proven non-degenerate the
      same day: with the `IsComparableCommentBanner` exclusion of "% Precision:"
      removed, `StructuralTests.PortResultsMMatchesOriginalInNamesAndCommentsInOrder`
      failed on all five reference formulations and two of
      `PrecisionFooterLineTests`' own cases failed with it; reverted, all nine green
      again (`tests/Output.Tests/BOOT.md`, "## Mutations", entry 7).
- [x] 2026-09-21: the stream-layout and execution-mode footer lines each explain
      themselves rather than naming a bare enum value, each differs between its two
      values nowhere but itself, and each stays a plain comment excluded from the
      structural comparison the same way the time line and the precision-kind line
      already are (`tests/Output.Tests/RunProvenanceFooterLineTests`, 8 cases, reusing
      `PrecisionFooterLineTests`' own sample builder and write helper rather than a
      second copy of either). Content, not mere inequality, per root BOOT.md's project
      rule adopted the same day ("each asserted by content and not by inequality"): the
      "changes only its own line" cases assert the surviving line carries the correct
      distinguishing phrase for each of the two values compared, not only that a single
      line differs, and the "explains itself" cases assert each value's line carries its
      own distinguishing phrase and not the other's. Proven non-degenerate the same day,
      two ways, each red then reverted to green. First, with the `IsComparableCommentBanner`
      exclusions of "% Stream layout:" and "% Execution mode:" removed:
      `StructuralTests.PortResultsMMatchesOriginalInNamesAndCommentsInOrder` failed on
      all five reference formulations and four of `RunProvenanceFooterLineTests`' own
      cases (the four `IsAPlainCommentBanner` cases) failed with it, nine cases in all —
      the same shape as mutation 7 for the precision-kind line. Second, with
      `StreamLayoutCommentLine`'s two branches' message bodies swapped: exactly the two
      cases carrying this row's own positive control failed
      (`StreamLayoutLineExplainsItselfRatherThanPrintingABareEnumName` and
      `ChangingTheStreamLayoutChangesOnlyItsOwnFooterLineWithTheCorrectWording`), each
      on a specific missing substring, not a bare "these two lines differ" — the shape of
      check this row's own wording exists to rule out, and which the swap would not have
      caught (`tests/Output.Tests/BOOT.md`, "## Mutations", entries 8–9).
- [x] 2026-09-23: the header echo of the menu parameter `eps` reproduces the original's
      REAL*4 store under `PrecisionKind.Original` and is unchanged under `Binary64`
      (`## Header echo`; `tests/Output.Tests/EpsHeaderEchoTests`, 3 cases, seen red
      against the pre-fix code). Positive control: the port's `--precision original`
      header equals the archived reference header line for line on all five reference
      formulations (filename and time lines excluded). Negative control: `--precision
      double` output is unchanged apart from the time line. Every other header echo was
      checked the same way and none of them showed the same shape (`## Header echo`).
- [x] 2026-09-24: the tail-probability header line and the `Dokb_max` gate both read the
      original's own already-reduced `alfa` (`RunHeader.TailProbabilityModified`), not
      the raw menu parameter (`## Tail-probability header line and gate`;
      `tests/Output.Tests/TailProbabilityHeaderTests`, 5 cases: two unit-level, seen red
      against the pre-fix code — one for the header line, a two-row theory for the gate
      in both directions — and two fixture-backed, against
      `tests/Fixtures/cases/output/nonzero-tail-probability/results.m.txt`, a real
      original run with a nonzero tail probability generated for this criterion).
- [x] 2026-09-24: `coef`, `fqmkm1` and `fqmkm2` print `nmax + 2` values padded with
      zeros beyond their own `Nc`-sized array, not truncated to it (`## Array-length
      divergence, measured`; `tests/Output.Tests/ArrayPaddingTests`, 4 cases: three
      unit-level, one per array, each seen red against the pre-fix clamp, and one
      reading `tests/Fixtures/Legacy/outputs/hp2.m.txt` through `tests/Harness`' parser
      for the archive's own overflow evidence, no count or value typed into the test).

⚠ 2026-09-24: cited test names renamed for CA1707, meaning unchanged, no criterion
re-verified and no date moved (`tests/test-renames-2026-09-24.txt`).

⚠ 2026-10-02: the first row read 1176–1504, now 1184–1453, the scope of "## Print-time
expressions": 1176–1183 is cycle bookkeeping and the file header, and the console
summary 1454–1504 is declared not ported (`src/Statistics/BOOT.md`, "## Line map",
"The screen summary"). Found by reading the row against that section.

## Defects of the original

| Fortran | Kind | What the original does | Consequence | The port | Differing cells |
|---|---|---|---|---|---|
| 1184–1453 | numeric | The print plane computes its own expressions in REAL*4 (print-time arithmetic such as `DOKSD**0.5`, `1 − DolM`, the products with `mp`) and prints REAL*4 values; the port computes them in `double` and rounds to the printed digits without a binary32 conversion ("## Design decisions (2026-09-19)", the first bullet; `src/Statistics/BOOT.md`, "## Report", the per-cycle plane's bullet). A last printed digit can differ under either precision kind. | not measured: the criterion's print-resolution floor covers one unit of the last digit, and the set criterion finds no such cell under `Original` (`tests/Harness.Tests/SetCriterionTests`, 2026-10-01). | declared, not reproduced | none |
| 1238 | cosmetic | Prints `epsx(5)` labelled as `EPS6` and `epsx(6)` labelled as `EPS7` — a mislabeling in the original's own print statement (`## Invariants`). | not measured | reproduced | none |
| 1370, 1373, 1377; `arrayprint`, 1517–1543 | cosmetic | `arrayprint('coef'/'fqmkm1'/'fqmkm2', ..., *_nmax + 2)` passes an element count taken from the caller, not from the callee's own `Nc`-sized array declaration, so the subroutine reads and prints zero past the array's own end whenever `nmax` reaches `Nc − 1` or `Nc` (`## Array-length divergence, measured`). | measured: `tests/Fixtures/Legacy/outputs/hp2.m.txt` prints 1002 `coef` values, the last two `0.000E+00` (`## Array-length divergence, measured`). | reproduced | none |

## Taboos

- No computation of model quantities here.

## Design decision (2026-09-24): print the stored setup

Every setup-plane value this node prints or scales by is read from the result's stored
setup ([Simulation](../Simulation/API.md), "Stored setup"), never from `Formulation` or
`ModelParameters`. The members are named by `src/Statistics/SetupPlane.generated.txt`. The
header echoes, the fraction table, `fineoxy_fr`'s `gdokns`, and the cell size `Di` that
scales densities and print lengths all follow this rule. So under `Original` a value has
one value per run, the one the model used. The `eps` echo's own `(double)(float)` cast goes
with it. A test fails if the writer reads a generated member from `Formulation` or
`ModelParameters`. Found by the audits of 2026-09-24 (architecture D1, fidelity O-3).

Implemented the same day. `WriteHeader`/`WriteResults`/`WriteFunctions` take
`SimulationResult.StoredSetup` in place of the record it superseded there;
`GetMassShares`/`GetBounds`/`EchoedMenuValue` are gone with it.
`StoredSetupSourceReadTests` (`tests/Output.Tests`) is the forbidden-member test,
proven red both on a reintroduced `formulation.MetalMassFraction` read and on a raw
`formulation.Fractions[0].MassShare` read. `Binary64` is bit-identical on all five
reference formulations against the pre-change build (time line aside);
`--precision original` moves no cell on any of them at seed 0 with default
parameters, every newly-rounded default being either exact in binary32 (`Alpha`,
`NnMin`, `NnMax`) or, where not (`PocketCoefficient`, `BridgeCoefficient`,
`Gm`/`Gdok`/`Plot1`/`Plot2`), rounding below this section's own print widths.
`PocketCoefficientHeaderEchoTests` is the positive control the acceptance list below
asks for: a real original run with `karmcoef = 0.03`
(`tests/Fixtures/cases/output/nondefault-pocket-coefficient/results.m.txt`), whose
header line the port reproduces digit for digit under `Original`.

⚠ 2026-09-24: was `karmcoef = 12.345`, which did not discriminate, now `0.03` →
HISTORY.md#karmcoef-control-value-2026-09-24

## Design decisions (2026-09-19)

Settled in the design session of wave 8, closing the open questions:

- **Numbers print as the original shows them, from the port's doubles.** Every
  list-directed REAL*4 value prints in the original's shape (the fixed or exponent form
  DVF chooses by magnitude, with the number of significant digits the reference files
  show); the port rounds its `double` to those digits and does not first convert it to
  binary32 (root invariant "Double precision only"). A last-digit difference against a
  binary32 print is covered by the criterion's print-resolution floor. INTEGER*8 and
  explicit edit descriptors (`E9.3`, `E12.6`, `F7.2`, `F6.4`, `I3`, …) print by their
  Fortran rules. Every primitive is specified by strings cut from the shipped reference
  files (`## Constraints`), each with the file and line it was cut from.
- **Print-time arithmetic is transcribed, not re-derived.** The
  divisions and scalings the Fortran performs inside its `write` statements are
  reproduced as written, with their line: the conditions divided by `FI + N` (1–5) and by
  `FI` (6–9, lines 1248–1256), the µm scalings including the literal `1.00001e6` of
  `arrayprint` calls such as line 1396, `coef`, `qmkm1`, `qmkm2` printing `nmax + 2`
  values with zeros beyond `Nc`. Where the result carries one already computed, the
  contract of [Simulation](../Simulation/API.md) ("## Result", "## Stored setup") says
  so; the list, its rule and its map are `## Print-time expressions`.

  ⚠ 2026-09-24 (O-2, fidelity audit): "zeros beyond `Nc`" was the stated design from the
  day this bullet was written, but `ResultsMWriter`'s own `Take` clamped its result to
  `Math.Min(length, values.Length)` instead of padding to `length` — the design decision
  was never wired to the code that implements it, so `coef`, `fqmkm1` and `fqmkm2` were
  silently truncated at the array's own `Nc` whenever `nmax + 2` exceeded it, dropping the
  trailing zeros the original prints (Fortran lines 1370, 1373, 1377; `arrayprint` itself,
  1517–1543). Fixed by padding `Take`'s own result array to `length`, its `count` loop
  unchanged; `## Array-length divergence, measured` carries the evidence and the tests.

  ⚠ 2026-10-02: was "Print-time arithmetic is this node's", unqualified: false for
  the `x/Σx` of 1353–1377 and the `Ndok − 1` cut of 1386, which `Statistics` computes
  and the result carries. Found mapping the generated list to the writer (arbiter,
  D2).
- **The time line is the only line that depends on wall time.** It prints the run's own
  duration from `RunDiagnostics.Elapsed` (hours, minutes, seconds, Fortran's
  `write(4,'(I3,a,I3,a,I3)')hour1,' :',minut1,' :',sec1`); `tests/Harness` already
  excludes that line.

  ⚠ 2026-09-19: was the run's own *start time*, now its elapsed duration →
  HISTORY.md#time-line-start-time-2026-09-19

## List-directed real placement

Found while transcribing lines 1184–1453 against a live reference-mode run and fixed
by comparison with the fixture files, not by re-reading the Fortran runtime (this
node's own deviation, `## Constraints`): the gaps DVF's list-directed output puts
around a `REAL*4`/`REAL*8` value are not part of the number's own field width. Every
rule below is a property of the *place* a value is printed, not of the primitive
(`FortranFormat.SingleRealField`/`SingleRealBeforeAdjacent`/`SingleRealAdjacent`
already this node's names for these cases):

- **a real preceded by a literal** (`" ; eps ="`, `" ;  Zkarm ="`, …): the literal
  supplies its own trailing blank; the value follows in its bare field
  (`SingleRealCore`/`DoubleRealCore`), no extra pad;
- **a real followed by a literal** (`" ;"`, `" ];"`, `" +"`): one blank belongs to the
  transition and is written explicitly with the literal, not folded into the number's
  field — missed a dozen call sites the first time (a plain `";"` where `" ;"` was
  needed) and only surfaced on a byte diff against a live run, not from the source
  alone;
- **two reals printed adjacent with nothing between them** (`NN_min`/`NN_max`,
  `Zkarm_cor`'s first pair): the gap between them is entirely the *second* value's own
  leading pad (`SingleRealField`'s width, 11 or 15 chars by branch); the first value
  carries no trailing pad of its own (`SingleRealBeforeAdjacent`) — the opposite
  assumption (first value pads, second also pads) doubled the gap and was only caught
  by `StructuralTests` failing all five formulations at the `Nkarm`/`Nmkm` line;
- **an explicit-format integer inside a comment** (the `(step=... mkm)` lines): these
  use the source's own `(1x,a,I3,a)`, width 3, unrelated to the list-directed
  `INTEGER*2` field width (7) the `"% Dkarm ="` row header uses for the same kind of
  quantity elsewhere;
- **the closing `];` of every array**: the source's own separate
  `write(4,*)'];'` statement, always with its own leading blank, regardless of how the
  array's last row ended.

## Header echo

Found 2026-09-23, closing the last open item of the root's "The `Original`
accumulation kind reproduces…" acceptance criterion: the header echo of the menu
parameter `eps` (`ModelParameters.EpsDok`) prints `5.0000001E-02` in the original —
its REAL*4 store of the default 0.05 — and printed `5.0000000E-02` in the port under
**both** precision kinds, since the header was built straight from `parameters.EpsDok`
with no rounding of any kind. This is not an accumulator or a setup-plane value (root
BOOT.md, "Precision kind is an option of every run" names exactly two places the
`Original` kind rounds, and a menu-parameter header echo is neither): it is a plain
REAL*4 declaration in the Fortran's own variable block, outside the lines 1184-1453
this node's declared deviation (`## Constraints`) covers, so its REAL*4-ness is read
from the printed evidence rather than from a line map, per the root task that found
it — a REAL*4 store prints the binary32 value's own digits, and no port value printed
at the header's own precision does that unless it is rounded to one first.

**Every other header echo was checked the same way**, not assumed: a live
`--mode reference --layout original --seed 0 --precision original` run against each
archived `tests/Fixtures/references/*/results.m.txt`, header lines only (up to the
"Calculation Results" banner), on all five reference formulations, diffed line for
line against the filename line excluded (the input's case differs, irrelevantly).
Before the fix, `eps` was the one differing line on every formulation; every other
header value — `Plot1`/`Plot2`/`Gdok`/`Gm`, `Nfr`/`JZ`/`Cycles`/`N`, `Gfr`/`Dfr`,
`Dmin`/`Di`/`Dj`, `Nkarm`/`Nmkm(min,max)`, `k5`, the tail-probability line,
`Calculation variant`, the two pocket/bridge coefficients and `Zok*` — matched
already. The comparison used `ModelParameters.Default` throughout (none of these
options were passed on the command line, matching how the reference archive itself
was produced), so this does not rule out a REAL*4 store on a menu parameter whose
default value happens to be exactly representable in binary32 (an integer or a power
of two, where the store and the `double` print the same digits regardless); it rules
out a REAL*4 store wherever the default value could show it, which every other menu
parameter's default does not.

**The fix**: `ResultsMWriter.EchoedMenuValue` rounds `EpsDok` through an exact
binary32 round trip, `(double)(float)value`, under `PrecisionKind.Original` only, and
leaves it unchanged under `Binary64`. `src/Output` is not a numerical node
(`tests/Protocol.Tests/InvariantTests.cs`, `NumericalNodes`), so the root's "double
precision only" invariant does not bind this cast; the exception the root taboos list
for the `Original` precision kind's binary32 rounding names the accumulator write
sites and the setup-plane values, both owned by numerical nodes, and does not reach
this node's own header formatting at all. `Statistics` already carries an internal
binary32 helper for the setup plane, but it is internal and AGENTS.md §3 forbids
reading a neighbour's implementation to reach it besides, so this node casts directly
rather than escalating for a public helper over one line of arithmetic.

Verified 2026-09-23, positive control: the fixed port's header, run the same way on
all five reference formulations, equals the archived reference header line for line
(filename and time lines excluded), zero diffs. Negative control: `--precision double`
output is unchanged from before the fix on every line but the time line, on the
formulation checked byte for byte (HPEPA3). `tests/Output.Tests/EpsHeaderEchoTests`
carries the unit-level evidence, seen red against the pre-fix code before this fix
landed; `PrecisionFooterLineTests.ChangingThePrecisionKind_ChangesOnlyItsOwnFooterLineAndTheEpsHeaderEcho`
(renamed from `...ChangesOnlyItsOwnFooterLine`) now names both lines a precision-kind
change touches instead of asserting there is only one.

## Tail-probability header line and gate

Found 2026-09-24 by a fidelity audit, verified by the orchestrator (O-1): Fortran line
377, `CALL PARAM(...,alfa,...)`, runs before any of this node's print block and can
*reduce* `alfa` inside `PARAM` (line 1750, `alfa = alfa - (z1(Imax+1) - z1(Imax))`,
fired when the tail draw's position falls inside the largest fraction's own window).
Both places the original reads `alfa` afterwards — the header echo at line 1209 and the
`Dokb_max` gate at line 1272 — read this already-reduced value, since `PARAM` runs once,
early, and every later reference sees its output. `ResultsMWriter` read
`parameters.TailProbability`, the raw menu value, at both sites instead of
`RunHeader.TailProbabilityModified` (`src/Simulation/API.md`), which already carried the
reduced value before this fix — a silent departure the root taboo forbids ("No silent
fix of a defect of the original") had it gone the other way, and here the reverse: a
silent departure from the *original's own arithmetic*, not one of its declared defects,
so nothing in `## Defects of the original` changes for this one.

No archived reference or replica shows this: every one of the 336 archived files (root
BOOT.md, "Statistical reference criterion") prints `alfa = 0`, since none of the five
reference formulations' `.dat` files set a nonzero tail probability and no default does
either (`ModelParameters.Default.TailProbability = 0`). A dedicated fixture was
generated to see the reduction fire on a real run:
`tests/Fixtures/cases/output/nonzero-tail-probability/results.m.txt`, the original
executable run once on HPEPA3 (`--layout original --seed 0`) with menu item [9]
(`alfa`) answered `0.01` instead of accepting every default — `tests/Fixtures/
run_original.py --alfa`, added for this one fixture (`tests/Fixtures/API.md`,
"## Measurement scripts"; `generate.run_case` gained an optional `stdin` parameter,
backward compatible, `None` preserving every existing caller's behaviour unchanged).
Provenance: `tests/Fixtures/provenance.json`. The archive prints
`9.266132883049108E-003` where the raw menu answer was `0.01`, an 8 % gap no
floating-point noise explains, and prints `Dokb_max` (gated on the same reduced value,
still positive).

**The fix**: `WriteHeader` takes `RunHeader.TailProbabilityModified` as an explicit
parameter instead of reaching into `ModelParameters` for it, and the `Dokb_max` gate in
`WriteResults` reads `header.TailProbabilityModified` (already in scope there) instead
of `parameters.TailProbability`. Verified against the fixture above: the port, run at
the same formulation/layout/seed under `PrecisionKind.Original` (so the setup plane
`PARAM` reads — the fraction thresholds — is reproduced in binary32, "Precision kind is
an option of every run"), prints a reduced value matching the archive's own to a
relative difference under `1e-8`, far inside the residual this tree elsewhere allows a
`REAL*8` sum agreement with the original's x87 intermediates (root BOOT.md: "agreement
of that sum with the executable at the last ULP ... is not claimed") and far outside the
~8 % gap printing the raw parameter back would leave. `tests/Output.Tests/
TailProbabilityHeaderTests` carries the unit-level evidence (seen red against the
pre-fix code: both the header line and the `Dokb_max` gate, the latter in both
directions — a raw value that would wrongly gate the line on and one that would wrongly
gate it off) and the fixture-backed positive control.

## Array-length divergence, measured

Some printed array lengths are running maxima over the whole cycle (`Ndok` via
`Ddokmax`, `DPmax`/`DPmax_cor`, `DPRow`, `coef_nmax`, `qmkm1_nmax`, `qmkm2_nmax`; root
`HISTORY.md#double-precision-only-first-exception` already names `Ndok`, "C166: 72
cells instead of 71"). Measured 2026-09-19,
comparing a live reference-mode run (seed 0, `Original` layout) against each archived
`tests/Fixtures/references/*/results.m.txt`: this is not confined to `Ndok` — HMX's
`fmkarm` block was 366 lines in the live run against 369 archived, and `pdoksmall`'s
length differed for three of the five formulations. This is why `tests/Output.Tests`'s
structural check (`StructuralTests`) stops comparing shape once the run-dependent
section starts (its own class doc carries the same evidence) and leaves value and
length agreement to the statistical criterion of `tests/Simulation.Tests`, which
compares distributions across replicas rather than a single archived run.
- **The `DPmax`/`DPmax_cor` print-length formula itself is confirmed correct, not
  merely close.** A root session spanning this node and `Statistics` (2026-09-21,
  `src/Statistics/BOOT.md`, "## Report", the resolved escalation) checked
  `ResultsMWriter`'s `dpLength`/`dpCorLength` (`(int)(pockets.DpMax / cellSize) + 2`,
  `(int)(pockets.DpMaxCor / cellSize) + 2`) against every reference formulation's own
  archive: the `DpMax`-driven pair (`fmkarm`, `fqkarm`) matches on all five, the
  `DpMaxCor`-driven trio (`fmkarm_cor`, `fmkarm_cor2`, `fqkarm_cor`) on four of five.
  The one miss (HMX) is `DpMaxCor`'s own accumulated value diverging from the
  original — `Statistics`'s own accumulation-consequence measurement, not a defect of
  this node's arithmetic, and not fixable here: no binary32 rounding of `DpMax`/
  `DpMaxCor` would move it (the gap is six cells, far past a rounding-boundary flip),
  and the value itself is `Particle`'s own accumulator, gated inside a per-attempt
  check this node has no reach into. `tests/Output.Tests/PocketPrintLengthTests`
  carries the evidence; no line of `ResultsMWriter` changed. The gating mechanism
  itself is confirmed, not merely a fitting story, by a dose-response measurement
  `Statistics` owns (`src/Statistics/BOOT.md`, "## Report", the dated dose-response
  table): the length matches the original exactly at reduced `N`, where the gate's
  own input is close to agreement, and misses only at the shipped `N`, where it has
  diverged far more.
- **`coef`/`fqmkm1`/`fqmkm2` print `nmax + 2` values padded with zeros beyond `Nc`, not
  truncated to it (fixed 2026-09-24, O-2).** `coef`, `qmkm1` and `qmkm2` are always
  `Nc`-sized ([Simulation](../Simulation/API.md), "## Result", its `x/Σx` arrays);
  their own `*_nmax + 2` print length can exceed `Nc` because Fortran's
  `arrayprint` (lines 1517–1543) takes its element count from the *caller's* argument
  (`call arrayprint('coef',real(coef)/sum(coef),coef_nmax + 2)`, line 1377; `fqmkm1`/
  `fqmkm2` at lines 1370/1373), not from the actual array's own declared dimension, and
  reads zero past the end whenever `nmax` reaches `Nc − 1` or `Nc` — measured,
  `tests/Fixtures/Legacy/outputs/hp2.m.txt` (input `Legacy/formulations/HPEPA10.dat`):
  1002 `coef` values, the last two `0.000E+00`, `coef_nmax` having reached `Nc` exactly.
  This node's own `Take` helper clamped its result to `Math.Min(length, values.Length)`
  instead of padding to `length`, silently dropping those trailing zeros — "Design
  decisions (2026-09-19)" already stated the zeros-beyond-`Nc` design; the code was
  never wired to it. Fixed by padding `Take`'s result array to `length` unconditionally
  (double's own default value is `0.0`, so nothing past `count` needs writing).
  `tests/Output.Tests/ArrayPaddingTests` carries the unit-level evidence (three cases,
  one per array, each seen red against the pre-fix clamp) and reads `hp2.m.txt` through
  `tests/Harness`' own parser for the archive's length and trailing-zero evidence, no
  count or value typed into the test.
- **JSON is `System.Text.Json` over `SimulationResult`** with property names as in the
  records, doubles written round-trip, `NaN` and infinities allowed
  (`JsonNumberHandling.AllowNamedFloatingPointLiterals`). Round trip means: written,
  read back, written again gives the same bytes, and the records compare equal field
  by field with bitwise `double` equality.
- **Where the statistical criterion runs.** The port's `results.m` is parsed by
  `tests/Harness` exactly as the original's; the criterion rows (reference mode and
  batched mode, each layout against its own replicas) live in `tests/Simulation.Tests`,
  whose row "the statistical criterion" waited for this node. This node's own tests
  (`tests/Output.Tests`) check the primitives, the structure and the JSON.
