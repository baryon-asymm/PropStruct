"""Scans every Fortran line this node declares (BOOT.md, "## Setup plane", "Design
decision, 2026-09-24") and classifies every REAL*4 name found there by its **declared
kind** and its **role**, over the same declaration block and implicit-typing rule
`src/Particle/classify-real4-accumulators.py` already uses for the accumulators (that
script's own docstring; this one is its precedent's sibling, not a second design).

**What changed 2026-09-24.** Before this date `SETUP_PLANE_NAMES` was a hand-typed
dict of concept -> (fortran name, role): a human decided which names belonged and what
role each played, and only the *kind* was generated. Two audits found the hand-typed
list stale (it missed `AK1`-`AK4`, `Dmax` and `Ddokmax`) and a second REAL*4 value
existing in two places within one run (`CycleStatistics`/`Output` reading raw
`Formulation`/`ModelParameters` fields the setup plane had already rounded). The owner
and the orchestrator decided the same day that **membership itself must be generated**
(AGENTS.md §6, a criterion quantified by "all": "every REAL*4 variable"), not just the
kind of a hand-picked list.

**What is generated now.** Every name this node's declared lines actually touch:

  - every `READ` target of the formulation-read block (Fortran lines 93-133: `PLOT1`,
    `PLOT2`, `GGG0`, `GM`, `AK1`-`AK4`, `NMM`, `JZZ`, `KXX`, `N`, `NNZ`, `GSV`, `GDOK`,
    `DDOK`), read for the *kind* of this node's inputs only, not transcribed as this
    node's own specification (the menu and the fractions stay `Input`'s and
    `Statistics`' own respectively, unchanged by this reading right);
  - every menu assignment of the interactive-override block (Fortran lines 144-244:
    each `read*, <name>` inside the `nump` dispatch), read the same way;
  - every assignment statement of the setup ranges themselves (`## Line map`'s own
    267-277 extended to include the micrometre-to-metre conversion and `Ddokmax`
    derivation immediately above it, 259-277 -- already cited by this node's own
    `Setup.cs` doc comments at 260-266, 264-265, 266, 271, 274-276 before this script
    existed -- plus 376-406 and 1695-1757, `PARAM`'s own body).

A row survives into the generated file only when its target's **declared kind is
REAL*4** (bare `real`, implicit, or `real*4`; `real*8` excludes -- `alfa`/`TailProbability`
is REAL*8 and no longer a row, unlike before this date, since nothing about it needs
generating: it never rounds and its handling is fully specified elsewhere) -- narrower
than the pre-2026-09-24 file, which also carried `TailProbability` to document that it
does *not* round. `PARAM` (Fortran lines 1695-1757) is its own Fortran program unit with
its own local declarations (lines 1696-1699: `Dmax`, `G`, `DOK`, `Zerror` explicit
`REAL`; `X`, `X1`, `ALFA`, `Z`, `Z1` explicit `REAL*8`; `Imaxx` `INTEGER`) -- Fortran
does not look a subroutine's undeclared names up in its caller's declarations, so this
script keeps two separate kind tables, one for the main program (lines 4-64, used for
every other range) and one for `PARAM`'s own scope (used only for 1695-1757). Getting
this wrong would silently round `Z`/`Z1`/`X`/`X1`/`ALFA`, which are REAL*8 in `PARAM`
and must not.

**Role is inferred from shape, not typed** (over the statements and the loop structure
of the shared reader `fortran_source.py`, 2026-10-01):

  - `input`: the name is a `READ` target of the formulation-read block or a menu
    assignment target -- checked first, and wins even if the same name is later
    converted in place inside a setup range (`DDOK`, `Di`, `Dj`, `Dmin` are all
    converted from micrometres by a self-referential `NAME = NAME*1e-6` a few lines
    after their own `READ`/menu assignment; BOOT.md's own "Inputs as `READ` leaves
    them" already treats that conversion as part of how the input is delivered, not a
    second value);
  - `sum`: some assignment to the name adds a REAL*4 right-hand side to the name itself
    inside a loop whose index the target does not mention (`fortran_source.py`,
    `Program.is_loop_sum`; `DOKM=DOKM+...`, `DOKSD=DOKSD+...`). The role says the sum is
    REAL*4 and held across the loop in a register between stores; **where it is stored,
    so where it rounds, is the executable's listing's and not the source's** (BOOT.md,
    "## Setup plane": `DOKM` five-fold, `DOKSD` once at line 405). The classifier decides
    membership only and carries no schedule;

    ⚠ 2026-10-03: was role `loop`, "carried unrounded across the loop and rounded once at
    its exit" (rule L, decided 2026-10-01), a schedule the source cannot tell and the
    listing refutes for both names; now role `sum` and no schedule.
  - `accumulator`: otherwise, the name is assigned inside a setup range and the
    assignment's own right-hand side reads the same name (`ZS=ZS+Z(J)`,
    `DOK4=DOK4+...`, each with a REAL*8 addend) -- a running sum, rounded after every
    addition (BOOT.md, "Sums rounded per addition");
  - `store`: the name is assigned inside a setup range and is not self-referential
    (`GGG = GGG0 - gdokns*GGG0`, `DDokmax = DDOK(2*kilo)`, `Dmax = DOK(2*I)`) -- a
    scalar computed once and stored (BOOT.md, "every store to a REAL*4 variable
    rounded").

**The site map, replacing `SETUP_PLANE_NAMES`.** `SITE_MAP` below is the one thing this
script does not and cannot generate: which C# member each Fortran name's rounded value
actually reaches, or why none does ("not ported", with a pointer to the paragraph that
says so -- `Zerror`, BOOT.md's own "## Line map": "not ported"; `Ddokmax`, rounded
already but by a *different*, unconditional exception, not this one). `classify()`
checks it **both ways** every run, not just that it parses: every REAL*4 name this
script finds must have a site entry, and every site entry must name a REAL*4 name this
script actually found -- a stale removed name or a typo'd bogus one each abort the run
with a distinct message (BOOT.md's own root taboo, "every check ... proven twice": this
is the check proven red on each of those two mistakes, not merely read).

The literal-expression rows (`SETUP_PLANE_LITERALS`) are unaffected by any of the above:
an unsuffixed REAL literal is single-precision by the language's own default, needing no
declaration-block lookup, so they stay the same fixed, hand-written triple they always
were.

Python 3.8+, standard library only.

    python classify-setup-plane.py generate   # (re)writes SetupPlane.generated.txt
    python classify-setup-plane.py verify      # regenerates into a temp file, fails on any byte difference
"""

from __future__ import annotations

import re
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, Tuple

NODE_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(NODE_ROOT))
import fortran_source as fs  # noqa: E402  (the shared reader beside this script)

OUTPUT_PATH = NODE_ROOT / "SetupPlane.generated.txt"
SCRIPT_NAME = "classify-setup-plane.py"

# The main program's own declaration block, 1-based inclusive -- the same block
# `src/Particle/classify-real4-accumulators.py` reads. Used for every range below
# except PARAM's own body.
MAIN_DECLARATION_FIRST_LINE, MAIN_DECLARATION_LAST_LINE = fs.MAIN_DECLARATIONS

# PARAM's own local declarations (Fortran lines 1696-1699), inside its declared body
# 1695-1757: a separate Fortran program unit has its own implicit-typing environment,
# never the caller's (module docstring).
PARAM_DECLARATION_FIRST_LINE, PARAM_DECLARATION_LAST_LINE = fs.PARAM_DECLARATIONS

# The formulation-read block: every READ(2,*) target (module docstring).
FORMULATION_READ_FIRST_LINE = 93
FORMULATION_READ_LAST_LINE = 133

# The interactive menu's read*, <name> assignments (module docstring).
MENU_FIRST_LINE = 144
MENU_LAST_LINE = 244

# The setup ranges themselves: the micrometre-to-metre conversion and Ddokmax/Ndok/
# Nkarm/Ncat derivation (259-277, extending "## Line map"'s own 267-277 to the lines
# `Setup.cs`'s own doc comments already cited before this script existed), GGG/lambda/
# DOKM/DOKSD (376-406), and PARAM's own body (1695-1757).
SETUP_RANGES: List[Tuple[int, int]] = [(259, 277), (376, 406), (1695, 1757)]
PARAM_RANGE = (1695, 1757)

# fortran name (lowercase) -> where its rounded value actually reaches, or "not ported:
# <reason>" (module docstring, "The site map"). Hand-written, checked both ways by
# classify() below, never generated: AGENTS.md §6 asks for the *lookup* and the
# *classification* to be generated, not this structural correspondence, which does not
# change when the source's declarations change.
SITE_MAP: Dict[str, str] = {
    "plot1": "SetupInputs.OxidizerDensity (Setup.Prepare, already rounded)",
    "plot2": "SetupInputs.PropellantDensity (Setup.Prepare, already rounded)",
    "ggg0": "SetupInputs.OxidizerMassFraction (Setup.Prepare, already rounded; the raw GGG0, printed as 'Gdok =')",
    "gm": "SetupInputs.MetalMassFraction (Setup.Prepare)",
    "ak1": "ModelSetup.Ak1 (Setup.Prepare, attempt plane)",
    "ak2": "ModelSetup.Ak2 (Setup.Prepare, attempt plane)",
    "ak3": "ModelSetup.Ak3 (Setup.Prepare, attempt plane)",
    "ak4": "ModelSetup.Ak4 (Setup.Prepare, attempt plane; Setup.Sizes rounds it again, unconditionally, for Ndok/Nkarm/Ncat only -- a separate, pre-existing exception, root BOOT.md 'Array sizes from binary32 values')",
    "gdok": "SetupTables.MassShare (Setup.Prepare, the raw per-fraction share)",
    "ddok": "SetupTables.Bounds (Setup.Prepare, the per-fraction bounds, already rounded)",
    "dmin": "ModelSetup.Dmin (Setup.Prepare, already rounded)",
    "di": "ModelSetup.CellSize (Setup.Prepare, already rounded)",
    "dj": "ModelSetup.CategoryStep (Setup.Prepare, already rounded)",
    "eps_dok": "SetupInputs.EpsDok (Setup.Prepare)",
    "alpha": "ModelSetup.Alpha (Setup.Prepare, attempt plane)",
    "nn_min": "ModelSetup.NnMin (Setup.Prepare, attempt plane)",
    "karmcoef": "ModelSetup.PocketCoefficient (Setup.Prepare, attempt plane)",
    "mkmcoef": "ModelSetup.BridgeCoefficient (Setup.Prepare, attempt plane)",
    "nn_max": "ModelSetup.NnMax (Setup.Prepare, attempt plane)",
    "gdokns": "SetupInputs.HomogenizedOxidizerFraction (Setup.Prepare, already rounded)",
    "eta": "SetupInputs.AggregatedOxideFraction (Setup.Prepare)",
    "ddokmax": "ported: SetupEchoes.Ddokmax (Setup.Prepare's own fraction loop, the same store each fraction's upper bound already rounds to via ToBinary32MetresStored -- not Setup.Sizes's own ddokmax out-parameter, which the root's array-size exception computes unconditionally and without that second store-rounding, for Ndok/Nkarm/Ncat's numerators only; the two were found to disagree by review, 2026-09-24, with no effect on Ndok/Nkarm/Ncat on any of the 49 archived formulations)",
    "ggg": "SetupEchoes.OxidizerMassFractionEffective (Setup.Prepare, already rounded)",
    "slamd": "ModelSetup.Lambda (Setup.Prepare, already rounded)",
    "dokm": "SetupEchoes.Dokm (Setup.AnalyticSizes: stored as the listing stores it, five-fold unrolled for JZZ other than 2 and DOK4/DOK3 under JZZ = 2; the five is typed there, a declared deviation, BOOT.md '## Setup plane')",
    "doksd": "SetupEchoes.Doksd (Setup.AnalyticSizes: the sum stays in the register and line 405 subtracts DOKM**2 from it and stores once, rounded; in the JZZ = 2 loop it is also stored after passes 1-3 of each four-fold block, typed there, a declared deviation, BOOT.md '## Setup plane')",
    "dok4": "ported inside Setup.AnalyticSizes: an internal accumulator, no separately named C# field, already rounded per addition, feeding only Dokm under the uniform-in-D law",
    "dok3": "ported inside Setup.AnalyticSizes: an internal accumulator, no separately named C# field, already rounded per addition, feeding only Dokm under the uniform-in-D law",
    "zs": "SetupTables.Zss (FractionLaw.Build, already rounded; PARAM's own dummy argument name for the caller's ZSS -- BOOT.md's own 'FractionWeightSum' concept, renamed to the literal token this scan actually finds inside PARAM's body)",
    "zerror": "not ported: BOOT.md '## Line map', 'Zerror (1733) not ported'",
    "dmax": "SetupEchoes.Dmax / ModelSetup.Dmax (Setup.CompleteEchoes, rounded there)",
}

# Literal expressions the compiler folds in binary32 before use (module docstring): not
# looked up, an unsuffixed REAL literal is single-precision by the language's own default.
SETUP_PLANE_LITERALS: List[Tuple[str, str]] = [
    ("NonUniformSizeLawCoefficient", "3/3.14"),   # Z(J) numerator coefficient, JZZ != 2 (line 1704)
    ("UniformSizeLawCoefficient", "24/3.14"),     # Z(J) numerator coefficient, JZZ = 2 (line 1716)
    ("MassUniformMeanSizeCoefficient", "0.8"),    # DOKM = 0.8*DOK4/DOK3 (line 403)
]

def rounds_under_original(kind: str) -> bool:
    """REAL*4 by declared kind (bare `real` or implicit) rounds; `real*8` and every
    `integer*` kind exclude."""
    return kind in ("real", "real*4")


def is_real_kind(kind: str) -> bool:
    return kind in ("real", "real*4", "real*8")


# ---- membership scanning (module docstring: "What is generated now") ---------------

def scan_read_targets(lines_1_based: Tuple[int, int]) -> List[str]:
    """Every `READ(2,*)` target of the formulation-read block, in source order,
    lower-cased, first occurrence only. Handles a plain comma list
    (`READ(2,*)PLOT1,PLOT2,GGG0,GM`) and the implied-DO array form
    (`READ(2,*)(GDOK(II),II=1,NMM)`, whose target is the array name, `GDOK`)."""
    first, last = lines_1_based
    all_lines = fs.read_all_lines()
    read_pattern = re.compile(r"^\s*READ\s*\(\s*2\s*,\s*\*\s*\)\s*(.*)$", re.IGNORECASE)
    implied_do_array = re.compile(r"^\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\(", re.IGNORECASE)

    names: List[str] = []
    seen = set()
    for raw in all_lines[first - 1:last]:
        if fs.is_comment(raw):
            continue
        match = read_pattern.match(raw)
        if not match:
            continue
        rest = match.group(1).strip()
        if rest.startswith("("):
            array_match = implied_do_array.match(rest)
            if not array_match:
                raise SystemExit(f"{SCRIPT_NAME}: could not parse implied-DO READ target: {raw!r}")
            found = [array_match.group(1).lower()]
        else:
            found = fs.strip_name_list_noise(rest)

        for name in found:
            if name not in seen:
                seen.add(name)
                names.append(name)
    return names


def scan_menu_targets(lines_1_based: Tuple[int, int]) -> List[str]:
    """Every `read*, <name>` menu-assignment target, in source order, lower-cased,
    first occurrence only (the menu's own defaults, set once near the top of the
    declaration block, are not scanned: `Input`'s `ModelParameters.Default` already
    mirrors them, and this script's job is the *kind* of each name, unaffected by which
    of its two assignment sites is read)."""
    first, last = lines_1_based
    all_lines = fs.read_all_lines()
    menu_read_pattern = re.compile(r"^\s*read\s*\*\s*,\s*([A-Za-z_][A-Za-z0-9_]*)", re.IGNORECASE)

    names: List[str] = []
    seen = set()
    for raw in all_lines[first - 1:last]:
        if fs.is_comment(raw):
            continue
        match = menu_read_pattern.match(raw)
        if not match:
            continue
        name = match.group(1).lower()
        if name not in seen:
            seen.add(name)
            names.append(name)
    return names


def scan_assignment_roles(lines_1_based: Tuple[int, int], declared: Dict[str, str],
                          arrays: set) -> List[Tuple[str, str]]:
    """Every assignment target inside one setup range, in first-occurrence source
    order, with its role (module docstring, "Role is inferred from shape"): `sum` if any
    assignment to it adds a REAL*4 right-hand side to itself in a loop, else `accumulator` if any assignment reads it on
    its own right-hand side (an initialise-then-accumulate name is an accumulator though
    its first assignment is not), else `store`."""
    program = fs.Program(lines_1_based[0], lines_1_based[1], declared, arrays)
    order: List[str] = []
    roles: Dict[str, str] = {}
    rank = {"store": 0, "accumulator": 1, "sum": 2}
    for op in program.ops:
        if op.kind != "assign":
            continue
        if program.is_loop_sum(op):
            role = "sum"
        elif any(read.name == op.target for read in op.rhs.reads):
            role = "accumulator"
        else:
            role = "store"
        if op.target not in roles:
            order.append(op.target)
            roles[op.target] = role
        elif rank[role] > rank[roles[op.target]]:
            roles[op.target] = role
    return [(name, roles[name]) for name in order]


def classify() -> List[Tuple[str, str, str, bool]]:
    """One row per REAL*4 name this node's declared lines actually touch, in the
    discovery order described in the module docstring: fortran name, role, declared
    kind, whether it rounds. Validates the site map both ways before returning
    (module docstring, "The site map")."""
    fs.verify_no_implicit_statement(SCRIPT_NAME)
    main_declared = fs.parse_declared_kinds(MAIN_DECLARATION_FIRST_LINE, MAIN_DECLARATION_LAST_LINE)
    param_declared = fs.parse_declared_kinds(PARAM_DECLARATION_FIRST_LINE, PARAM_DECLARATION_LAST_LINE)
    main_arrays = fs.parse_array_names(MAIN_DECLARATION_FIRST_LINE, MAIN_DECLARATION_LAST_LINE)
    param_arrays = fs.parse_array_names(PARAM_DECLARATION_FIRST_LINE, PARAM_DECLARATION_LAST_LINE)

    input_names = (
        scan_read_targets((FORMULATION_READ_FIRST_LINE, FORMULATION_READ_LAST_LINE))
        + scan_menu_targets((MENU_FIRST_LINE, MENU_LAST_LINE))
    )
    input_set = set(input_names)

    rows: List[Tuple[str, str, str, bool]] = []
    seen = set()

    for name in input_names:
        kind, explicit = fs.kind_of(name, main_declared)
        if not is_real_kind(kind):
            continue  # An input-block name of non-REAL kind (NMM, JZZ, ..., GSV) is not this file's concern.
        if name in seen:
            continue
        seen.add(name)
        if rounds_under_original(kind):
            rows.append((name, "input", f"{kind} ({'explicit' if explicit else 'implicit'})", True))

    for setup_range in SETUP_RANGES:
        in_param = setup_range == PARAM_RANGE
        declared = param_declared if in_param else main_declared
        arrays = param_arrays if in_param else main_arrays
        for name, role in scan_assignment_roles(setup_range, declared, arrays):
            if name in input_set or name in seen:
                continue  # "input" already decided this name's role and fate (module docstring).
            kind, explicit = fs.kind_of(name, declared)
            if not is_real_kind(kind) or not rounds_under_original(kind):
                continue
            seen.add(name)
            rows.append((name, role, f"{kind} ({'explicit' if explicit else 'implicit'})", True))

    generated_names = {name for name, *_ in rows}
    missing_site_entries = sorted(generated_names - set(SITE_MAP))
    orphan_site_entries = sorted(set(SITE_MAP) - generated_names)
    if missing_site_entries:
        raise SystemExit(
            f"{SCRIPT_NAME}: SITE_MAP is missing an entry for: {', '.join(missing_site_entries)} "
            "-- every generated REAL*4 name needs a site (a C# member, or 'not ported: <reason>')."
        )
    if orphan_site_entries:
        raise SystemExit(
            f"{SCRIPT_NAME}: SITE_MAP names {', '.join(orphan_site_entries)}, which this run did not "
            "generate -- a removed name (the source or the declared ranges changed) or a typo; "
            "remove the stale entry from SITE_MAP."
        )

    return rows


def render(rows: List[Tuple[str, str, str, bool]]) -> str:
    lines = [
        "# Generated by classify-setup-plane.py from the Fortran declaration blocks, the",
        f"# formulation-read block ({fs.SOURCE_NAME}, lines {FORMULATION_READ_FIRST_LINE}-{FORMULATION_READ_LAST_LINE}),",
        f"# the menu (lines {MENU_FIRST_LINE}-{MENU_LAST_LINE}) and the setup ranges themselves",
        "# (" + ", ".join(f"{a}-{b}" for a, b in SETUP_RANGES) + "), plus the implicit-typing rule. Do not edit by",
        "# hand: `python classify-setup-plane.py verify` regenerates this file and fails if it differs,",
        "# or if the hand-written SITE_MAP disagrees with this run's own membership either way",
        "# (src/Statistics/BOOT.md, \"## Setup plane\").",
        "#",
        "# fortran name | role | declared kind | rounds under PrecisionKind.Original | site",
    ]
    for name, role, kind, rounds in rows:
        site = SITE_MAP[name]
        lines.append(f"{name} | {role} | {kind} | {'yes' if rounds else 'no'} | {site}")
    lines.append("#")
    lines.append("# Literal expressions folded in binary32 before use (module docstring: not looked up,")
    lines.append("# an unsuffixed REAL literal is single-precision by the language's own default).")
    lines.append("# concept | expression")
    for concept, expression in SETUP_PLANE_LITERALS:
        lines.append(f"{concept} | {expression}")
    return "\n".join(lines) + "\n"


def cmd_generate() -> None:
    text = render(classify())
    OUTPUT_PATH.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {OUTPUT_PATH}")


def cmd_verify() -> None:
    fresh = render(classify())
    if not OUTPUT_PATH.is_file():
        raise SystemExit(f"verify: {OUTPUT_PATH} is not committed")
    committed = OUTPUT_PATH.read_text(encoding="utf-8")
    if fresh != committed:
        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".txt", prefix="propstruct_setup_plane_classification_", delete=False, encoding="utf-8"
        ) as handle:
            handle.write(fresh)
            tmp_path = handle.name
        raise SystemExit(
            f"verify: {OUTPUT_PATH} does not reproduce byte for byte on regeneration (see {tmp_path})"
        )
    rows = classify()
    print(f"verify: {OUTPUT_PATH.name} reproduces byte for byte "
          f"({len(rows)} names, {len(SETUP_PLANE_LITERALS)} literals, "
          f"SITE_MAP checked both ways)")


def main(argv: List[str]) -> int:
    if len(argv) != 1 or argv[0] not in ("generate", "verify"):
        print(f"usage: {SCRIPT_NAME} generate|verify", file=sys.stderr)
        return 2
    if argv[0] == "generate":
        cmd_generate()
    else:
        cmd_verify()
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
