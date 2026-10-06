# HISTORY.md — Fixtures

Append-only, newest first (AGENTS.md §15). Not read by the start procedure; reached
only by following a dated pointer left in `BOOT.md`.

<a id="rate-table-notes-2026-09-25"></a>
## 2026-10-04 — from "## Acceptance criteria", the `rate-table.json` row — the notes of 2026-09-25 and 2026-09-26 on the rate table, told in full

Moved under the §15 leaf limit to make room for the delivery's criteria; the criterion,
its date and `## Rate table`'s rule stand. The notes as they stood at 75bc2c4, the two
of the criterion first, then the one of `## Rate table` (the digest's glob):

> ⚠ 2026-09-25: the writer moved to `tests/RateTableTool` (that node's own BOOT.md
> and `tests/Fixtures/API.md`, "## Rate table"). Regenerated there after the
> `Binary64` rename (CA1720) and the `StatisticalCriterion.cs` CA1859/CA1062 fix
> moved both of this file's tied hashes: Release build, sixteen-way `Parallel.For`,
> 320 runs in 16 m 39 s, no crash. Verdicts (`FailingCells`/`FailingNames`/`ResultSha256`)
> are bit-identical to the table this row's date names, cell for cell, once the
> precision label is read through the rename; the tie test is green again.
>
> ⚠ 2026-09-26: the tie gained a third digest, `FixturesSha256` (this section, "##
> Rate table"), since the criterion's own comparisons read fixture data — `exclusions.json`,
> `references/`, the two seed-patched replica directories — none of which was in any
> digest before. Regenerated: 320 runs, verdicts and `Runs` bit-identical to the
> pre-existing table (`tests/RateTableTool/BOOT.md`'s own dated entry has the run).
>
> ⚠ 2026-09-26: was the glob `StatisticalCriterion*.cs`, which degenerates the moment the
> criterion is no longer one file family — after the 2026-09-26 decomposition it would match
> only the façade, silently dropping every named class the split moved code into from the
> tie entirely (AGENTS.md §13: "a check never seen red is indistinguishable from an
> absent one"). Replaced by "every `*.cs` file, minus a declared exclusion list": the
> default is *in*, so a coder adding a file must argue it out by naming it in the exclusion
> list with its own reason, not merely by choosing a filename the old glob would miss.

<a id="legacy-out-of-tree-2026-10-04"></a>
## 2026-10-04 — from "## Invariants" — "`Legacy/` is the archive", before the original left the repository

Replaced when the owner decided the delivery (root `BOOT.md`, `## Delivery`): the
executable, its runtime, the transcoded source, the archive and the listing excerpt
move outside the repository, and `Legacy/` keeps the data. The invariant as it stood at
75bc2c4, in full:

> - **`Legacy/` is the archive.** Every file under `Legacy/` equals, byte for byte, its
> member of `legacy/PropStructV3.zip`, except `Legacy/PropStructv3.for.txt`, which is
> the CP1251 → UTF-8 transcoding of `PropStructv3.for` with CRLF → LF and nothing else
> (same line numbers), and `Legacy/outputs/*.m.txt`, each a raw copy of its `.m` member
> with `.txt` appended to the name (content unchanged). Only the files named in
> `## Layout` of `API.md` are extracted; the rest of the archive stays in the zip.

<a id="dispersion-tripwire-2026-09-20"></a>
## 2026-10-04 — from "## Invariants", "Thresholds are not stored" — why `dispersion.approved.txt` is no stored threshold, told in full

Moved under the §15 leaf limit to make room for the delivery's criteria; the invariant
and the taboo stand. The note as it stood at 75bc2c4:

> ⚠ 2026-09-20: `dispersion.approved.txt` (below) computes and stores per-family
> dispersion figures (`phi`, `rho_hat`) that feed `Harness`'s own beta-binomial
> predictive — read at a glance, this looks like exactly the derived tolerance this
> invariant forbids. It is not one: nothing in `Compare` or any other `Harness`
> comparison ever reads this file back — every run recomputes `phi`/`rho_hat` fresh
> from the replicas, the same way it always has, and this file is a **generated
> tripwire**, checked by diff against that live computation
> (`tests/Harness.Tests/DispersionApprovedTests.MatchesTheCommittedTable`), the same
> role `PublicSurface.approved.txt` and `docs/ORIGINAL-DEFECTS.md` already play
> elsewhere in this tree (AGENTS.md §13: "a tripwire, not a contract" — a generated
> list carries no intent, and a stale one fails loudly rather than silently). A
> genuine stored tolerance would be read back and substituted for the live
> computation; this file is read by nothing, so a code change that shifts the real
> estimate is caught in review, not hidden by one. Escalated and granted the same day
> (AGENTS.md §11): `Harness`'s own BOOT.md, "Fable 5.1 decision V", "Item 2, continued",
> has the full record of the request and why it could not be resolved from that node
> alone (the file's own placement here, `tests/Fixtures`, is Fable 5.1's own decision;
> `Harness` may not write into a neighbour's subtree unilaterally).

<a id="null-rate-notes-2026-10-02"></a>
## 2026-10-04 — from "## Acceptance criteria" — four notes of the null-rate criterion, told in full

Moved under the §15 leaf limit; the tick and its figure stay. The four notes as they
stood at 75bc2c4, in order:

> ⚠ 2026-10-02: was "The HPEPA3 reference passes with no failure against its lagged
> replicas (`tests/Harness`)", the root's per-run condition, retired 2026-09-24: a
> single run passing is not evidence either way (root `BOOT.md`, "the pass condition
> compares failure rates, not single runs"). Found reconciling the open criteria.
>
> ⚠ 2026-10-02, the same day: was dated 2026-09-27, the day before F-c changed the
> criterion's code; the null rate stayed 15 of 197 (root `ACCEPTANCE.md`).
>
> ⚠ 2026-10-02, after B2a: was 15 of 197 dated 2026-09-28, now 16 of 197 →
> `tests/Harness/HISTORY.md#b2a-rate-figures-2026-10-02`
>
> ⚠ 2026-10-02, after B2c: was 16 of 197, now 15 of 197 →
> `tests/Harness/HISTORY.md#b2c-rate-figures-2026-10-02`

<a id="oracle-verify-figures-2026-10-03"></a>
## 2026-10-03 — from "## Acceptance criteria" — the oracle fixture's `verify` figures before `PARAM` was executed

Moved under the §15 leaf limit when the 26-case figure (465 s) was recorded; the tick
stays, with the one figure that now decides it. The criterion as it stood at a4c65de,
the figures of 19 and 22 cases in it:

> - [x] 2026-10-03: `cases/statistics/cycle_plane_oracle.json` regenerates byte for byte
>       (`cycle_plane_oracle.py verify`, 447 s, 19 cases; 407 s, 22 cases once `extend`
>       added the three pins for `DOKM`'s schedule, red on a map pin the fixture lacks) and
>       `src/Statistics/ACCEPTANCE.md`'s A2 holds: 58 isolated sites, 18 `unreached`.

<a id="txt-suffix-generalized-2026-09-17"></a>
## 2026-10-03 — from "## Invariants" and "## Acceptance criteria" — the `.txt` suffix invariant's generalisation, told in full

Moved under the §15 leaf limit; the text as it stood:

  ⚠ 2026-09-17: this invariant first appended `.txt` only to `Legacy/PropStructv3.for.txt`
  and left every other `.m` under its own name. `protocol_lint` caught `Legacy/outputs/`
  the moment its 75 archived `.m` files were extracted, and `references/`/`replicas/`
  the moment `generate.py reference`/`replicas` first ran. Generalized to every `.m` in
  the node instead of patching each directory as the lint found it.

<a id="fixtures-tests-row-stale-2026-09-17"></a>
## 2026-10-03 — from "## Invariants" and "## Acceptance criteria" — the stale "Not yet" row, told in full

Moved under the §15 leaf limit; the text as it stood:

  ⚠ 2026-09-17: this row first read "Not yet: `Fixtures.Tests` does not exist", true
  when this document was written but stale once `tests/Fixtures.Tests` was created by
  a later wave (`FixturesInventoryTests.cs` already carried the GSV=3-only version of
  this test). Left uncorrected until this task noticed it while extending the test to
  the two new replica kinds.

<a id="oracle-regenerated-2026-10-03"></a>
## 2026-10-03 — from "## Acceptance criteria" — the oracle fixture's regeneration of the same day, told in full

Moved under the §15 leaf limit; the tick and its figures stay. The text that followed
"`src/Statistics/ACCEPTANCE.md`'s A2 holds.":

The fixture was regenerated the same day (`generate`, seed 20261003): its injected
setup values come from the twin's setup model, which now equals the executable's
`DOKSD`, and `verify` had failed on 15 of 18 replayed cases before. 58 isolated sites,
18 `unreached`; 54 and 22 before.

<a id="states-for-seed-special-case-2026-09-21"></a>
## 2026-10-02 — from "## Invariants" — `states_for_seed`'s `seed == 0` special case, found violated 2026-09-21

Moved under the §15 leaf limit to make room for the listing oracle's text; the current
truth, the invariant "A measurement script never accepts a flag it does not honour",
stays. The text, in full:

> ⚠ 2026-09-21: **found violated.** `run_original.py`'s own `states_for_seed` special-
> cased `seed == 0` to return `None` (no patch: the unmodified, shipped executable),
> regardless of `layout` — its own comment called this "the K=0 sanity check", correct
> for `--layout original` (the shipped executable already holds that layout's seeds)
> but wrong for `--layout independent`, which the special case silently downgraded to
> `original`. Found by `tests/Harness` from outside (`HISTORY.md`, "the Independent-
> layout non-monotonic shape was the apparatus, not physics"): a dose-response sweep
> compared `run_original.py --layout independent --seed 0` against a properly
> configured port run and read the two unrelated quantities' difference as a
> divergent, non-monotonic shape, when the "independent" original file was in fact the
> original layout throughout. Confirmed directly here, before any fix, by running both
> layouts at `--seed 0` on `inpt` (`--n 5 --kxx 1`) and diffing the raw output: the two
> files agreed everywhere except the pre-existing `pdoksmall(1:2)` non-determinism
> (BOOT.md, "Exclusions carry evidence") — `--layout` made no difference at all.
>
> **Fixed** by removing the special case: `states_for_seed` now always computes
> `{stream: advanced(base_value(stream), exponent)}` from the layout's own zero-
> ordinal state, for every `seed` including `0` (`exponent = seed * 2^80` is `0` there,
> and `advanced(value, 0) = value`, `generate.advanced`'s own identity case). This is
> not a workaround grafted onto the special case; it deletes it, because the layout's
> own un-jumped state was always the mathematically right answer for `seed == 0` too:
> for `--layout original` it is `original_stream_value`, the same six values already
> baked into the shipped executable, so patching them back in changes no byte relative
> to the unmodified executable (`## Invariants`, "Seed offsets are proven, not
> assumed": identity patch reproduces the baseline exactly) — the "K=0 sanity check"
> survives, now as a consequence of the general rule rather than as a carve-out from
> it. For `--layout independent` it is `independent_stream_value`, exactly the state
> `IndependentSeeds.ForParticle(0, 0)` reaches on the port side, not an approximation
> of it. Verified after the fix, same `inpt`/`--n 5 --kxx 1` run: `--layout original
> --seed 0` still reproduces the unmodified executable's output exactly outside
> `pdoksmall(1:2)`, and `--layout independent --seed 0` now diverges from it
> substantially (`Nkarm` 765 vs 320, `NFX`/`NFY`/`NFQ`/`NFW` all different) and agrees,
> cell for cell on every one of those integers, with `run_port.py`'s own `--layout
> independent --seed 0` run on the same input — direct evidence that the patched state
> is genuinely that layout's own, not a plausible-looking substitute.
>
> **Rejected:** refusing the combination (`SystemExit` on `--layout independent
> --seed 0`) instead of patching it. Refusing is the safer edit — it cannot mislead —
> but it throws away a real, reachable state for no reason once honouring it is shown
> correct by the identity above; it would also have kept the special case's shape (a
> branch on `seed == 0`), which is the shape that produced this defect, only with a
> different wrong branch (raising) in place of the old one (skipping the patch).
> Deleting the branch removes the defect class, not just this one instance of it.
>
> **Audited for the same shape**, the same day: `--n`/`--kxx` (see the invariant
> above, no defect: no branch on the value, only on whether it was given at all) and
> `run_port.py`'s own `--layout`/`--seed` (`run()` passes both to the `propstruct run`
> subprocess unconditionally, for every `(layout, seed)`, so there is no branch in this
> script's own text to special-case; the CLI's own honouring of the pair at every seed
> is that node's contract, `src/Cli/API.md`, not this node's to re-verify, but it was
> exercised directly all the same, see below).
>
> **The check that would have caught this**, `check_measurement_scripts.py` (`API.md`,
> "## Measurement scripts"): runs `run_original.run` for `--layout original` and
> `--layout independent` at the same `--seed` and asserts the two outputs differ
> outside the time line and `pdoksmall(1:2)` (`generate._normalized_for_comparison`,
> already used by `seeds locate` for the same reason: those are the only two sources of
> legitimate agreement-despite-different-input). Proven non-degenerate by running it
> against the pre-fix `states_for_seed` (the `seed == 0` branch reinstated locally):
> it fails, reporting `--layout` accepted and not honoured, exactly the defect above;
> against the fixed script it passes at `seed = 0` and `seed = 1`. It also runs the
> same comparison through `run_port.py` at `seed = 0`, skipped with a printed reason
> (not silently) when no Release build of `PropStruct.sln` is found, and passing
> otherwise — the empirical half of the port audit above.

<a id="u06-offset-correction-2026-09-17"></a>
## 2026-10-02 — from "## Invariants" — the `u06` offset correction of 2026-09-17

Moved under the §15 leaf limit for the reformulated null-rate criterion; the current
truth, `u06` at 0x20c20 with its proof, stays in the invariant. The text, in full:

> ⚠ 2026-09-17, corrected same day: this invariant first read "`u06` is not at
> 0x20c20: patching that block changed nothing", based on an earlier scratch
> experiment (`E4_s6` of the design session's Fable-reviewed mechanism tests) that
> multiplied the block's value by a large power of `a` on HPEPA3 and compared only
> four scalars: `NFX`, mean bridges per particle, `Dqmkm1`, `Dkarm43(1)`. Those four
> are dominated by whether a base particle is accepted at all (streams 1, 2, 3, 4, 5),
> not by streams 6's role (source `random2` calls at lines 613, 653, 689: X3 feeds
> `NFQ`/the pocket-size distributions `fqmkm2`/`fmkarm_cor2`; X4 feeds `NFW`/bridge
> decisions `Zkarm`/`Dkarm43_cor(2)`), so the experiment's negative result was a false
> negative from measuring the wrong quantities, not evidence that 0x20c20 is inert.
> Found while reproving all six offsets for this task: on both HPEPA3 and `inpt.dat`,
> patching 0x20c20 with its own original limb (value 1, i.e. a⁰) reproduces the
> baseline byte for byte outside the time line and `pdoksmall(1:2)`; patching it with
> any other value (a one-step jump, or unstructured bytes) changes `NFQ`, `Zkarm`,
> `Dkarm43_cor(2)`, `fqmkm2` and `fmkarm_cor2` while leaving the four narrowly-tracked
> scalars unchanged — exactly the pattern X3/X4's call sites predict. The three other
> occurrences of the "nine zero `int32` then 1" byte pattern in the executable
> (0x20f34, 0x20fb4, 0x21004) sit inside the code section: patching any of them
> crashes the executable (access violation) instead of producing a different, valid
> `results.m`, which rules them out as `u06`'s block.

<a id="ca1707-renames-2026-09-24"></a>
## 2026-09-24 — CA1707: test names renamed (underscores removed)

The test method names this node's `BOOT.md` and `API.md` cite were renamed for CA1707
(the analyzer rule against underscores in member names): each underscore-separated
segment PascalCased and concatenated, no other change of wording. The full old → new
map, covering every renamed member across the test tree, is
`tests/test-renames-2026-09-24.txt` (generated by a script and applied by the same
script). No acceptance criterion's date moved; this is a rename of the evidence's own
name, not a re-verification of what it shows.

