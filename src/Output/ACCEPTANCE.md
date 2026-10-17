# ACCEPTANCE.md — Output

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

### Corrected model (designed 2026-10-18, `BOOT.md`, "## Corrected model")

- [ ] `Original` unchanged: for a result whose `Diagnostics.Model` is `Original`, both
      writers produce the bytes they produced before the option
      (`tests/Simulation.Tests`' seed-zero guard, `results.m` and JSON);
      `StructuralTests` stays green unedited but for the `% Model kind:` exclusion.
      Red once: writing the footer line under `Original` turns the guard red.
- [ ] The footer line: under `Corrected` the line of `BOOT.md`, "## Corrected model",
      is written verbatim after the execution-mode line and holds no `=` and no digit;
      two results identical but for `Diagnostics.Model`, at the default ratios and
      `gdokleft = 0`, differ in exactly three lines: this one, the `fqmkm1` step comment
      and condition 5's label. Asserted by content; red once with the line's text
      altered.
- [ ] The `fqmkm1` step: `(step = 0.001)` under `Corrected`, `(step = 0.01)` under
      `Original`, and `fqmkm2`'s and `coef`'s comments unchanged under both; red once
      with the branch removed.
- [ ] The condition labels: a constructed result with `Ak1 = 0.45` prints
      `Dbase < 0.45Dok` under `Corrected` and `Dbase < 0.5Dok` under `Original`;
      condition 5 prints `l > 4.7max(Dbase,Dok)` at the default ratio; every label keeps
      the ` %    K) ` prefix and the 16-character padding; `tests/Harness`' parser reads
      the counts of a `Corrected` file as of an `Original` one. Red once with the ratios
      read as the `Original` literals.
- [ ] `fineoxy_fr`: at `gdokns = 0.1` and `gdokleft > 0` the `Corrected` value equals
      `gdokns + (1 − gdokns)·gdokleft/GGG` computed independently in the test and
      differs from the `Original` value by `gdokns·gdokleft/GGG`; at `gdokleft = 0`
      the two print the same; `PrintExpressionMapTests` stays green with the 1286 row
      unedited. Right once on P312 under `Corrected` with `gdokns = 0.1`.
- [ ] JSON: a `Corrected` result round-trips equal and byte-stable with `Model` and the
      three ratios present; an `Original` result's JSON holds none of the four
      properties; a JSON written before the option reads back with `Model = Original`
      and null ratios. Red once with the modifier removed (the `Original` bytes move).
