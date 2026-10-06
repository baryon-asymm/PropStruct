# HISTORY.md — Statistics.Tests

Append-only. Newest first. Each entry carries the date, the section of `BOOT.md` it
came from, and the original text in full. Read only by following a dated pointer from
`BOOT.md`; the start procedure (AGENTS.md §10) does not read this file.

<a id="to-nearest-representable-rerun-2026-09-25"></a>
## 2026-10-04 — from "## Mutations" — the re-run of the `ToNearestRepresentable` mutation after its rewrite, told in full

Moved under the §15 leaf limit to make room for the delivery's criterion; the mutation
entry above the pointer stands. The note as it stood at 75bc2c4:

> ⚠ 2026-09-25: `ToNearestRepresentable` was rewritten to round the whole `double`
> domain, not only the normal binary32 range (`src/Statistics/BOOT.md`, "the binary32
> rounding is total", D3); the local variables this entry names moved (`HalfDroppedBit`
> is now the block-scoped `half`, recomputed per call since the shift varies with the
> exponent). Re-run against the new code with the equivalent mutation (the parity check
> dropped, `remainder >= half`): the same pre-existing test failed again, together with
> seven checks of the new `Binary32EquivalenceTests` ("## Acceptance criteria" below);
> reverted, green again — the non-degeneracy proof still holds, on the current source.

<a id="input-dependency-undeclared-2026-09-20"></a>
## 2026-10-04 — from "## Dependencies" — the `Input` declaration found missing, told in full

Moved under the §15 leaf limit to make room for the delivery's `tools/legacy`
dependency; the declaration stands. The note as it stood at 75bc2c4:

> ⚠ 2026-09-20: this section did not declare `Input` while the tests used five of its
> types. Found by `tests/Protocol.Tests`' dependency check on the day it was first run
> (AGENTS.md §13: the declared dependencies against the real ones, from signatures and
> method bodies); the use predates the check, the declaration was simply missing.

<a id="analytic-sizes-mutations-2026-10-03"></a>

## 2026-10-03 — from "## Mutations" — the first `Setup.AnalyticSizes` mutation entry

Moved to keep this node inside its limit (AGENTS.md §15); the stage-5b entry after it
stays in `BOOT.md`.

- `ListingOracleTests` and the setup (2026-10-03), `Setup.AnalyticSizes` edited and
  reverted: `UnrolledSchedule(fractionCount, 5)` made `(fractionCount, fractionCount)`
  (rule L) turns `TheExecutablesPreLoopAgreesWithThePortAsApproved` red on the three
  pinned cases alone (BK10, KB397, AK157a, `dokm` and `doksd`), where it was green on the
  19 cases before them; made `(fractionCount, 6)` it turns that test and
  `SetupPlaneTests.SetupPlaneMatchesTheFormulaScript` (PSAN02n) red.

<a id="cycle-statistics-formula-mutations-2026-10-02"></a>

## 2026-10-03 — from "## Mutations" — the mutation table of `CycleStatisticsFormulaTests`, told in full

The table and the surviving mutation as they stood, moved to keep this node inside its
limit (AGENTS.md §15); the two lines above them stay in `BOOT.md`.

  | Member | Mutation | Red |
  |---|---|---|
  | `GeneratorAccuracy` | `xsr5` (781) not rounded | `seeded_rounding_reaches_every_member/Original` and three more |
  | `GeneratorAccuracy` | `xsr4` (779) divided by `nfq` instead of `nfz` | all ten (five cases, both kinds) |
  | `OxidizerSizes` | `epsx1` (814) not rounded | `seeded_rounding_reaches_every_member/Original` and four more |
  | `OxidizerSizes` | `alldok432` (805) loop sum not rounded | `seeded_rounding_reaches_every_member/Original` and two more |
  | `OxidizerSizes` | warning flag (817) read from the rounded `epsx3` | `seeded_epsx3_warning_reads_the_register/Original` alone |
  | `Pockets` | `epsmd4` (980) not rounded | `seeded_rounding_reaches_every_member/Original` and four more |
  | `Pockets` | warning flag (976) read from the unrounded `epsy` | `seeded_epsy_warning_reads_the_rounded_store/Original` alone |
  | `Matrix` | `plotsm` (1011) not rounded | `seeded_rounding_reaches_every_member/Original` and four more |
  | `Matrix` | `1-GGG+gdokleft` of 1014 reads the 1001 value, not the sum | all ten |
  | `CorrectedPockets` | `qmcoef` (1112) loop sum not rounded | `seeded_rounding_reaches_every_member/Original` and two more |
  | `CorrectedPockets` | `sdevp43Cor` (1097) not rounded | `seeded_rounding_reaches_every_member/Original` and three more |
  | `MassFractions` | `dolM3` (1151) not rounded | `seeded_rounding_reaches_every_member/Original` and three more |
  | `MassFractions` | `VdokTotal2` (1150) rewritten unrounded | `seeded_rounding_reaches_every_member/Original` and three more |
  | `MassFractions` | `VdokTotal` (1145) rewritten unrounded | `seeded_rounding_reaches_every_member/Original` and three more |
  | `Convergence` | `alldoksd` (1173) not rounded | `seeded_rounding_reaches_every_member/Original` and two more |
  | `Convergence` | `Doksd` replaced by `Dokm` in 1173 | the eight cycle-1 runs (four cases, both kinds) |
  | script | `plotsm` (1011) store dropped in `formulas_statistics.py`, regenerated | all five cases under `Original` |

  One mutation survived and stays recorded: `eps6` (782) not rounded is no red. `xsr5`
  is binary32 near 0.5, so `abs((xsr5 - 0.5)/0.5)` is exact in binary32 (Sterbenz) and
  the memory store rounds nothing, at 772–784 alike; `GeneratorAccuracy` is guarded by
  its two rows above.

<a id="plane-checks-of-the-source-read-table-2026-10-03"></a>

## 2026-10-03 — from "## Purpose", "## Acceptance criteria" and "## Mutations" — the checks of the source-read table

Replaced when the plane followed the executable's listing and
`CyclePlane.generated.txt` with its classifier was retired. The table row of the
site classification, the row and the criterion of the oracle's stage 2 (a ratchet not
yet empty), and the mutation entries of the retired generator tests and of the site map
over the old table, each told in full.

From "## Purpose":

| L1 | the per-cycle plane's site classification (771–1175, 718) | the source, regenerated; rows the design fixed in advance | ✅ |

| L1 | `Compute` under `Original` over constructed totals | the listing oracle (`tests/Fixtures`, "## Listing oracle"): the executable's own bytes executed, bit for bit; today's differences are an approved ratchet, not yet empty | ✅ |

From "## Acceptance criteria":

- [ ] `ListingOracleTests` (stage 2): under `Original`, `Compute` equals the listing
      oracle bit for bit on every case, each site covered or written `unreached`
      (`src/Statistics/ACCEPTANCE.md`, A2). Expected in advance: red on the port of
      2026-10-02 at the isolated sites, then green when the plane follows the generated
      listing table. Status 2026-10-03: the test runs, the setup inputs and the
      name-to-slot binding asserted first, and today's differences stand as an approved
      ratchet (`ListingOracle.approved.txt`, both values hex), so it stays unticked.

From "## Mutations":

- `CyclePlaneClassificationGeneratorTests` (2026-10-01): one edited byte of
  `CyclePlane.generated.txt` fails `verify` and the pinned 771 row; `is_loop_sum`
  returning false, regenerated, fails the 805 and 1112 rows and the setup plane's
  `dokm`/`doksd` roles; the in-block shadowing of `definitions_read` removed fails 795,
  1014, 1015, 1061 and the reaching-definitions test; the column-6 continuation rule
  fails the self-test on 873, 1739 and 1740. Each reverted, green again.
- `CyclePlaneSiteMapTests` (2026-10-01): a deleted map row, a bogus map row, a deleted
  `Memory` call (1001), an uncited call (1011) and a call of the wrong method (1011
  `Narrowing`) each turn at least one of its four directions red. Reverted, green.

<a id="l1-row-split-2026-09-18"></a>

## 2026-09-18 — from "## Purpose" — the L1 row's first wording and its split

Moved when the node outgrew its limit (AGENTS.md §15, 2026-10-03), oldest first. The
two notes of 2026-09-18 on the table above, each told in full; the second is the
audit's, kept in `#small-particles-row-split-2026-09-18` below.

⚠ 2026-09-18: the L1 row was first written as one line, "`Compute` on constructed
totals ... against values computed from the Fortran formulas by a script in
`tests/Fixtures`". Split in two once coded: `Categories.MergeAndDescribe` (its own
class per BOOT.md's line map, called by `Compute`) carries the full formula-script
cross-check (`tests/Fixtures/formulas_statistics.py`, `cases/statistics/categories.json`);
the rest of `Compute`'s pipeline (`GeneratorAccuracy`, `OxidizerSizes`, `Pockets`,
`Matrix`, `SmallParticles`, `CorrectedPockets`, `MassFractions`, `Convergence`) is
exercised end to end through `Compute` itself but checked against hand-derived
identities of the constructed scenario, not a second, independent formula-script
transcription of every one of those eight pieces: writing and maintaining a full
Python mirror of the entire end-of-cycle pipeline (as opposed to the one genuinely
branchy piece, the category merge) was judged not to earn its cost for this wave. A
design session may still add one; until then this row's "against what" column states
the weaker guarantee precisely rather than overclaiming the stronger one the first
wording implied.

<a id="small-particles-row-split-2026-09-18"></a>

## 2026-09-18 — from "## Purpose" — `SmallParticles` split out of the row (audit)

⚠ 2026-09-18 (audit of this node): `SmallParticles` split out of the row above into
its own scripted L1 row, for the same reason `Categories` already had one: it is not
"short, branch-free" like the remaining seven pieces (the `zdoksmall > 1e-5` guard, the
monotone `pdoksmall` clamp and `MaxSize`'s first-crossing threshold are genuine
branches), and it is this node's only two outputs that feed back into the next cycle's
model loop (`pdoksmall`, `Dmaxxx`) — the two quantities the rest of `Compute`'s
pipeline only reports. `src/Statistics/BOOT.md`'s own acceptance criteria carry the
matching split and the dated evidence.
