"""Transcribes the Fortran formulas of `PropStructv3.for.txt` lines 825-959 (the
category merge, `Categories.MergeAndDescribe`) into Python `double`, independently of
any C# (`tests/Fixtures/BOOT.md`, "Formula scripts are transcriptions of formulas, not
of the program"; `tests/Fixtures/API.md`, "## Particle formula cases" set the precedent
this file follows for `Statistics`).

This script simulates no particle: it evaluates the named formulas, in the Fortran
order, for totals built by hand to reach every branch (`## Cases`, below). Expected
values are never typed: `generate()` calls the same functions the constructed inputs
are fed to, and `verify()` proves a rerun reproduces the committed files byte for byte.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import json
import math
import random
import re
import struct
import sys
import tempfile
from pathlib import Path
from typing import Dict, List, NamedTuple, Sequence

NODE_ROOT = Path(__file__).resolve().parent
CASES_DIR = NODE_ROOT / "cases" / "statistics"
SCRIPT_NAME = "formulas_statistics.py"


# ---------------------------------------------------------------------------
# The one exception to "double precision only" (root BOOT.md, "Invariants"): the
# integers the original derives from binary32 values by division -- Ndok, Nkarm, Ncat
# (src/Statistics/Setup.cs), DPRow (src/Statistics/Categories.cs, from DPmax/Dj) and the
# whole-fraction count of pdoksmall (src/Statistics/SmallParticles.cs, from kilo/ak2) --
# computed with integer mantissa arithmetic, no float type in the C# (src/Statistics/
# Binary32.cs); here, independently, through struct.pack('f', ...)/struct.unpack('f',
# ...) rather than manual mantissa arithmetic (tests/Fixtures/BOOT.md, "Formula scripts
# are transcriptions of formulas, not of the program"), which Python's struct module
# implements with the same round-to-nearest-ties-to-even IEEE 754 rule.
# ---------------------------------------------------------------------------


def _to_binary32(value: float) -> float:
    """(double)(float)value: value rounded to the nearest binary32-representable
    double, the same map src/Statistics/Binary32.cs's own ToNearestRepresentable
    computes bit for bit through direct mantissa manipulation."""
    return struct.unpack('<f', struct.pack('<f', value))[0]


def _binary32_truncated_quotient(numerator: float, denominator: float) -> int:
    """int(numerator/denominator) the way the original's REAL*4 division and
    truncation toward zero produce it: both operands rounded to binary32, their
    quotient left at double precision, truncated toward zero (src/Statistics/
    Binary32.cs's own TruncatedQuotient)."""
    return math.trunc(_to_binary32(numerator) / _to_binary32(denominator))


# ---------------------------------------------------------------------------
# The per-cycle plane under PrecisionKind.Original (src/Statistics/BOOT.md, "## Report",
# "The per-cycle plane from the executable's listing"). The transcriptions below are driven
# by src/Statistics/CyclePlane.listing.generated.txt, the table the generator reads off the
# executable's own listing -- never by what the C# does. Each site is named by its address
# in that table; what the table says of it decides, here: whether the store rounds
# (`rounds`), where a sum is stored and where a pass reads memory or the register (`schedule`),
# whether an operand is its register value or its home (a name ending `@register` or not in
# `order`), and in which order the factors of an expression are multiplied (`order`, which is
# parsed and evaluated, never retyped). Under Original every number the plane computes
# at a site comes from its `order` text, so no plane literal is typed here either: the binary32
# constants the compiler folded stand in the text. Under Binary64 every answer is
# the port's own and "unchanged": stores and temporaries are the identity and the products are
# written the way Compute always formed them (the cell centre first), so the Binary64 cases are
# what they always were.
# ---------------------------------------------------------------------------

LISTING_TABLE_PATH = NODE_ROOT.parent.parent / "src" / "Statistics" / "CyclePlane.listing.generated.txt"

_TABLE_FIELDS = ("at", "row", "lines", "target", "type", "kind", "rounds", "home", "count", "reads", "schedule", "order")
_TOKEN = re.compile(r"\s*(?:(?P<number>\d+\.\d*(?:e[+-]?\d+)?)|(?P<name>[A-Za-z_][A-Za-z0-9_]*(?:@[A-Za-z0-9_]+)?)|(?P<op>[-+*/(),?]))")


class Site(NamedTuple):
    at: str
    row: str
    lines: str
    target: str
    type: str
    kind: str
    rounds: str
    home: str
    count: str
    reads: str
    schedule: str
    order: str


def _tokenize(text: str) -> List[tuple]:
    tokens = []
    position = 0
    text = text.strip()
    while position < len(text):
        match = _TOKEN.match(text, position)
        if match is None:
            raise ValueError(f"cannot tokenize {text!r} at {position}")
        position = match.end()
        kind = match.lastgroup
        tokens.append((kind, match.group(kind).lower() if kind == "name" else match.group(kind)))
    return tokens


class _Parser:
    """expression := sum ('?' sum)?; sum := product (('+'|'-') product)*; product := unary (('*'|'/') unary)*;
    unary := '-' unary | primary; primary := number | name | name '(' expression (',' expression)* ')' | '(' expression ')'."""

    def __init__(self, tokens: List[tuple]):
        self.tokens = tokens
        self.index = 0

    def _peek(self):
        return self.tokens[self.index] if self.index < len(self.tokens) else (None, None)

    def _take(self):
        token = self.tokens[self.index]
        self.index += 1
        return token

    def parse(self):
        node = self._expression()
        if self.index != len(self.tokens):
            raise ValueError(f"trailing tokens {self.tokens[self.index:]}")
        return node

    def _expression(self):
        left = self._sum()
        if self._peek() == ("op", "?"):
            self._take()
            return ("compare", left, self._sum())
        return left

    def _sum(self):
        node = self._product()
        while self._peek()[0] == "op" and self._peek()[1] in "+-":
            node = (self._take()[1], node, self._product())
        return node

    def _product(self):
        node = self._unary()
        while self._peek()[0] == "op" and self._peek()[1] in "*/":
            node = (self._take()[1], node, self._unary())
        return node

    def _unary(self):
        if self._peek() == ("op", "-"):
            self._take()
            return ("neg", self._unary())
        return self._primary()

    def _primary(self):
        kind, value = self._take()
        if kind == "number":
            return ("number", float(value))
        if kind == "name":
            if self._peek() == ("op", "("):
                self._take()
                arguments = [self._expression()]
                while self._peek() == ("op", ","):
                    self._take()
                    arguments.append(self._expression())
                if self._take() != ("op", ")"):
                    raise ValueError("unclosed call")
                return ("call", value, arguments)
            return ("name", value)
        if (kind, value) == ("op", "("):
            node = self._expression()
            if self._take() != ("op", ")"):
                raise ValueError("unclosed parenthesis")
            return node
        raise ValueError(f"unexpected token {kind} {value}")


class _Evaluation:
    """One evaluation of a parsed `order` text over named operands. A name binds to a value, or
    to a tuple of values consumed one per occurrence in the text (`real(i)` stands for a count
    in one place and a cell index in another). A name ending `@register` is the operand's
    register value, a plain name its home; a binding that no occurrence reads is an error, so
    the operands the twin supplies are exactly the operands the table names."""

    def __init__(self, bindings: Dict[str, object], swap_reads: bool):
        self.bindings = {name.lower(): value for name, value in bindings.items()}
        self.used = set()
        self.cursor: Dict[str, int] = {}
        self.swap_reads = swap_reads

    def _resolve(self, name: str) -> str:
        if self.swap_reads:
            sibling = name[: -len("@register")] if name.endswith("@register") else name + "@register"
            if sibling in self.bindings and name in self.bindings:
                return sibling
        return name

    def _lookup(self, name: str) -> float:
        name = self._resolve(name)
        if name not in self.bindings:
            raise KeyError(f"the order text names {name!r}, which the twin did not bind")
        self.used.add(name)
        value = self.bindings[name]
        if isinstance(value, tuple):
            position = self.cursor.get(name, 0)
            self.cursor[name] = position + 1
            return float(value[position])
        return float(value)

    def evaluate(self, node):
        kind = node[0]
        if kind == "number":
            return node[1]
        if kind == "name":
            return self._lookup(node[1])
        if kind == "neg":
            return -self.evaluate(node[1])
        if kind == "call":
            arguments = [self.evaluate(argument) for argument in node[2]]
            return {"real": lambda x: float(x), "abs": abs, "sqrt": _sqrt, "pow": _pow}[node[1]](*arguments)
        left, right = self.evaluate(node[1]), self.evaluate(node[2])
        if kind == "+":
            return left + right
        if kind == "-":
            return left - right
        if kind == "*":
            return left * right
        if kind == "/":
            return _div(left, right)
        if kind == "compare":
            return (left, right)
        raise ValueError(f"unknown node {kind}")

    def finish(self, text: str) -> None:
        unread = [name for name in self.bindings if name not in self.used
                  and (name.endswith("@register") and name[: -len("@register")] not in self.used
                       or not name.endswith("@register") and name + "@register" not in self.used)]
        if unread:
            raise KeyError(f"bound but not read by {text!r}: {unread}")
        for name, position in self.cursor.items():
            value = self.bindings[name]
            if isinstance(value, tuple) and position != len(value):
                raise KeyError(f"{name!r} bound {len(value)} times, read {position} times in {text!r}")


def _read_listing_table(path: Path) -> Dict[str, Site]:
    sites: Dict[str, Site] = {}
    for raw in path.read_text(encoding="utf-8").splitlines():
        if raw.startswith("#") or not raw.strip():
            continue
        cells = raw.split(" | ")
        site = Site(*cells)
        sites.setdefault(site.at, site)
    return sites


def schedule_rounds_after(unroll: int, pass_number: int, trips: int) -> bool:
    """B<n>: stored after the last pass of a block and after every remainder pass."""
    return pass_number > trips // unroll * unroll or pass_number % unroll == 0


def schedule_reloads_before(unroll: int, pass_number: int, trips: int) -> bool:
    """C<n>: read from memory on a block's first pass and on every remainder pass."""
    return pass_number > trips // unroll * unroll or (pass_number - 1) % unroll == 0


class CyclePlane:
    """What the executable's listing says of the plane's sites, and the alternative the isolation
    search of the controls tries instead. `flips` holds (address, aspect) pairs: `store` stores
    the other way (an R site rounded, a stored site not), `schedule` the other pass decision,
    `reads` the register where the table has the home and the other way round, `order` the
    Binary64 products. A flip is only ever a probe of whether a control reaches a stored bit;
    no committed value comes from one."""

    _sites: Dict[str, Site] = {}

    def __init__(self, original: bool, flips: frozenset = frozenset()):
        self.original = original
        self.flips = flips
        if not CyclePlane._sites:
            CyclePlane._sites = _read_listing_table(LISTING_TABLE_PATH)
        self._parsed: Dict[str, object] = {}

    def site(self, at: str) -> Site:
        if at not in CyclePlane._sites:
            raise KeyError(f"{LISTING_TABLE_PATH.name} has no site at {at}")
        return CyclePlane._sites[at]

    def _flipped(self, at: str, aspect: str) -> bool:
        return (at, aspect) in self.flips

    def store(self, at: str, value: float) -> float:
        """The value a site leaves where the table says it goes: rounded to binary32 under
        Original when the site rounds (a REAL*4 store or a compiler temporary), and a register
        value when it does not (kind R)."""
        rounds = self.original and self.site(at).rounds == "yes"
        if self.original and self._flipped(at, "store"):
            rounds = not rounds
        return _to_binary32(value) if rounds else value

    def unroll(self, at: str) -> int:
        match = re.match(r"blocks of (\d+)", self.site(at).schedule)
        if match is None:
            raise KeyError(f"site {at} is not an unrolled loop: {self.site(at).schedule!r}")
        return int(match.group(1))

    def rounds_after(self, at: str, pass_number: int, trips: int) -> bool:
        """B<n>: the sum is stored after the n-th pass of a block and after every pass of the
        remainder (a loop of fewer than n trips has no block)."""
        return schedule_rounds_after(self.unroll(at), pass_number, trips) != self._flipped(at, "schedule")

    def reloads_before(self, at: str, pass_number: int, trips: int) -> bool:
        """C<n>: a pass reads the previous element from memory on a block's first pass and on every
        remainder pass, from the register otherwise."""
        return schedule_reloads_before(self.unroll(at), pass_number, trips) != self._flipped(at, "schedule")

    def term(self, at: str, binary64, bindings: Dict[str, object]):
        """The expression the table gives for the site, over `bindings`, under Original; under
        Binary64 `binary64()`, the port's own form."""
        if not self.original or self._flipped(at, "order"):
            return binary64()
        text = self.site(at).order
        if at not in self._parsed:
            body = text[len("sum of "):] if text.startswith("sum of ") else text
            self._parsed[at] = _Parser(_tokenize(body)).parse()
        evaluation = _Evaluation(bindings, swap_reads=self._flipped(at, "reads"))
        value = evaluation.evaluate(self._parsed[at])
        evaluation.finish(text)
        return value

    def input(self, value: float) -> float:
        """A value the particle loop stored in REAL*4 and the plane reads (QKS1, 718)."""
        return _to_binary32(value) if self.original else value


BINARY64 = CyclePlane(original=False)


def _sequential_sum(values: Sequence[float]) -> float:
    """sum(array) the way the original adds, left to right in double; Python 3.12's
    built-in sum() of floats is compensated, which no Fortran loop and no C# loop is."""
    total = 0.0
    for value in values:
        total = total + value
    return total


# ---------------------------------------------------------------------------
# Setup plane (PropStructv3.for.txt, lines 267-277, 376-406, 1695-1757;
# src/Statistics/BOOT.md, "## Setup plane"): the whole `PrecisionKind.Original` setup
# recipe, transcribed independently of the C# for the fixture-equality criterion
# ("Under Original, the setup values ... equal, bit for bit, what a script in
# tests/Fixtures computes"). `formulas_particle.py`'s own `size_law_sample` (Fortran
# subroutine SIZE, lines 1761+) is imported rather than re-transcribed here: SIZE is
# `Particle`'s own line range, not this node's declared one (root BOOT.md taboo, "No
# second implementation of any part of the particle program"), and it is already an
# independent, already-verified transcription this file is entitled to reuse -- the
# tail draw that feeds it (Fortran 1735-1754) *is* this node's own range and is
# transcribed below, matching `FractionLaw.TryTailDraw`'s own algorithm.
# ---------------------------------------------------------------------------

sys.path.insert(0, str(NODE_ROOT))
from formulas_particle import size_law_sample  # noqa: E402  (path set immediately above)


def _numeric_tokens(record: str) -> List[float]:
    values = []
    for token in record.replace("\t", " ").split():
        try:
            values.append(float(token))
        except ValueError:
            pass
    return values


def _read_dat_formulation(dat_path: Path) -> Dict[str, object]:
    """A minimal, independent `.dat` reader (`tests/Fixtures/BOOT.md`'s own "formula
    scripts are transcriptions of formulas, not of the program": this is not a
    transcription of `Input.DatFile.Read`, just the READ statements of lines 94 and 124-133
    over the file's records). A `READ(2,1000)` consumes one record, a list-directed
    `READ(2,*)` of n values takes whole records until it has n of them and drops the rest
    of the last record it touched: `C166.dat`'s `GDOK` record holds fifteen values for
    `NMM = 14`, and the fifteenth is not the first `DDOK`. The header records carry no
    digits (checked by construction of the shipped files), so a record's numeric tokens
    are its values."""
    records = dat_path.read_text(encoding="latin-1").splitlines()
    position = 0

    def header() -> None:
        nonlocal position
        position += 1

    def values(count: int) -> List[float]:
        nonlocal position
        taken: List[float] = []
        while len(taken) < count:
            taken.extend(_numeric_tokens(records[position]))
            position += 1
        return taken[:count]

    header()
    plot1, plot2, ggg0, gm = values(4)
    header()
    ak1, ak2, ak3, ak4 = values(4)
    header()
    nmm_f, jzz_f, kxx_f, n_f, nnz_f, gsv_f = values(6)
    header()
    nmm = int(nmm_f)
    gdok_raw = values(nmm)
    header()
    ddok_um = values(2 * nmm)
    return {"nmm": nmm, "jzz": int(jzz_f), "plot1": plot1, "plot2": plot2, "ggg0": ggg0, "gm": gm,
            "ak": [ak1, ak2, ak3, ak4], "kxx": int(kxx_f), "n": int(n_f), "nnz": int(nnz_f), "gsv": int(gsv_f),
            "gdok_raw": gdok_raw, "ddok_um": ddok_um}


# The unroll factor of DOKM's loop (Fortran 379-406), for every JZZ other than 2. It is typed here
# and in `Setup.AnalyticSizes`, not read from the generated table, which does not reach those
# lines (src/Statistics/BOOT.md, "## Setup plane", the declared deviation): the executable's
# pre-loop, run by the listing oracle on the formulations its `pin` rows name, is what holds it.
# The parameter below exists for the oracle's selftest, which shows each pinned formulation to
# tell this schedule from another (`dokm_unroll` equal to the trip count is rule L).
DOKM_UNROLL = 5


def fraction_count(dat_path: Path) -> int:
    """The number of oxidizer fractions of a `.dat`: DOKM's trip count, and with it the unroll
    factor at which its schedule is rule L (one store, after the last pass)."""
    return int(_read_dat_formulation(dat_path)["nmm"])


# The JZZ = 2 loop of lines 392-404 (0x408CFE-0x408F8E), read from the listing and held by the oracle's
# executed pre-loop (`ListingOracleTests`, `PreloopSurveyTests`, `ACCEPTANCE.md` A12): unrolled four-fold,
# with these stores, each a rule the oracle's self-test shows to matter by turning it off.
JZZ2_BLOCK = 4
JZZ2_RULES = {
    "round_width_first": True,    # u-l is a REAL*4 temporary in pass 1 of a block and in every remainder pass
    "round_width_all": False,     # (the other passes keep it in a register; True is a variant, never the listing)
    "round_lower_fifth": True,    # l**5 is stored to a REAL*4 temporary every pass
    "store_doksd": True,          # DOKSD is stored after passes 1-3 of a block, carried after the 4th and in the remainder
}


def jzz2_sums(gdok: List[float], ddok: List[float], zx: List[float], nmm: int,
              rules: Dict[str, bool] = JZZ2_RULES) -> tuple:
    """DOKM and DOKSD, by their setup keys, of the JZZ = 2 branch, binary32 stores as `rules` say: the
    executable's, by default."""
    dok4 = dok3 = 0.0
    carried = 0.0                 # DOKSD in its register
    stored = 0.0                  # DOKSD in its REAL*4 slot, inside a block
    block_passes = nmm // JZZ2_BLOCK * JZZ2_BLOCK
    for j in range(nmm):
        pass_number = j + 1
        position = (pass_number - 1) % JZZ2_BLOCK + 1 if pass_number <= block_passes else 0
        lower, upper = ddok[2 * j], ddok[2 * j + 1]
        lower_squared, upper_squared = lower * lower, upper * upper
        lower_fourth, upper_fourth = lower_squared * lower_squared, upper_squared * upper_squared
        lower_fifth, upper_fifth = lower * lower_fourth, upper * upper_fourth
        width = upper - lower
        if rules["round_width_all"] or (rules["round_width_first"] and position in (0, 1)):
            width = _to_binary32(width)
        if rules["round_lower_fifth"]:
            lower_fifth = _to_binary32(lower_fifth)
        dok4 = _to_binary32(dok4 + (upper_fifth - lower_fifth) * zx[j] / width)
        dok3 = _to_binary32(dok3 + (upper_fourth - lower_fourth) * zx[j] / width)
        term = gdok[j] * ((lower * upper + lower_squared) + upper_squared) / 3.0
        if position == 1:
            stored = carried + term
        elif position in (2, 3, 4):
            stored = stored + term
        else:
            carried = carried + term
            continue
        if position == 4 or not rules["store_doksd"]:
            carried = stored
        else:
            stored = _to_binary32(stored)
    dokm = _to_binary32(_to_binary32(0.8) * dok4 / dok3)
    return {"dokm": dokm, "doksd": _to_binary32(carried - dokm * dokm)}


JZZ2_LISTING = "the listing"
JZZ2_VARIANTS = {
    "the width u - l rounded in every pass": {"round_width_all": True},
    "the width never rounded": {"round_width_first": False},
    "l**5 not stored": {"round_lower_fifth": False},
    "DOKSD never stored": {"store_doksd": False},
}
JZZ2_CONTROL_FORMULATION = "P3.dat"      # a JZZ = 2 file; its fractions are replaced by the drawn ones


def _hex_r4(value: float) -> str:
    return struct.pack(">f", value).hex()


def _hex_r8(value: float) -> str:
    return struct.pack(">d", value).hex()


def _executed_r8(bits: List[str]) -> List[float]:
    return [struct.unpack(">d", bytes.fromhex(item))[0] for item in bits]


def jzz2_control_draw(rng: random.Random) -> tuple:
    """One drawn JZZ = 2 loop for the oracle's self-test: (the formulation file to run, the `setup`
    overrides that give it the draw's fractions, `check`). Shares and bounds are binary32, the bounds
    ascending. `check(executed)` takes what the executed pre-loop left (`executed` bits by setup key)
    and says, per rule set, whether the twin's `jzz2_sums` under it differs from the executable's DOKM
    or DOKSD: the listing's rules, and each of `JZZ2_VARIANTS` with one store turned off."""
    count = rng.randint(1, 16)
    edges = sorted(_to_binary32(10 ** rng.uniform(-5.3, -3.0)) for _ in range(count + 1))
    bounds = [edges[k + shift] for k in range(count) for shift in (0, 1)]
    raw = [rng.random() for _ in range(count)]
    shares = [_to_binary32(value / sum(raw)) for value in raw]
    overrides = {"nmm": count, "jzz": 2, "gdok": shares, "ddok": bounds, "sfr": [1] * count}

    def check(executed: Dict[str, object]) -> Dict[str, bool]:
        zx = _executed_r8(executed["zx"])  # type: ignore[arg-type]
        found = {}
        for name, rule in [(JZZ2_LISTING, {})] + list(JZZ2_VARIANTS.items()):
            sums = jzz2_sums(shares, bounds, zx, count, dict(JZZ2_RULES, **rule))
            found[name] = any(_hex_r4(value) != executed[key] for key, value in sums.items())
        return found

    return JZZ2_CONTROL_FORMULATION, overrides, check


def pin_check(key: str, dat_path: Path, executed: Dict[str, object]) -> tuple:
    """(ok, what the pin shows, detail) for one `pin` row of the map: the executed pre-loop's bits
    (`executed`, by setup key) against the setup model's and against the variant the pin must tell
    from it. `dokm`: five-fold schedule against one store after the loop (rule L); `zss`: ZS stored
    after every addition against one store after the loop; `zx`: the pairwise denominator against the
    left-to-right one; `jzz2`: the listing's JZZ = 2 stores
    against l**5 unstored and against DOKSD unstored."""
    if key == "dokm":
        listed = _hex_r4(oracle_setup(dat_path)[key])
        once = _hex_r4(oracle_setup(dat_path, dokm_unroll=fraction_count(dat_path))[key])
        return (executed[key] == listed and executed[key] != once, "tells the executable's schedule from rule L",
                f"executed {executed[key]}, the listing's schedule {listed}, one store after the loop {once}")
    if key == "zss":
        listed = _hex_r4(oracle_setup(dat_path)[key])
        once = _hex_r4(oracle_setup(dat_path, zss_once=True)[key])
        return (executed[key] == listed and executed[key] != once,
                "tells ZS stored after every addition from one store after the loop",
                f"executed {executed[key]}, every addition {listed}, once after the loop {once}")
    if key == "zx":
        pairwise = [_hex_r8(value) for value in oracle_setup(dat_path)["zx"]]  # type: ignore[union-attr]
        left = [_hex_r8(value) for value in oracle_setup(dat_path, zx_left_to_right=True)["zx"]]  # type: ignore[union-attr]
        return (executed[key] == pairwise and executed[key] != left,
                "tells ZX's denominator (l*l)*(u*u) from the left-to-right l*l*u*u",
                f"executed equal to the pairwise one: {executed[key] == pairwise}, to the left-to-right one: {executed[key] == left}")
    if key == "jzz2":
        plane = oracle_setup(dat_path)
        sums = jzz2_sums(plane["gdok"], plane["ddok"], plane["zx"], plane["nmm"])  # type: ignore[arg-type]
        same = all(executed[name] == _hex_r4(value) for name, value in sums.items())
        tells = []
        for rule in ("round_lower_fifth", "store_doksd"):
            variant = jzz2_sums(plane["gdok"], plane["ddok"], plane["zx"], plane["nmm"],  # type: ignore[arg-type]
                                dict(JZZ2_RULES, **{rule: False}))
            tells.append(any(_hex_r4(value) != executed[name] for name, value in variant.items()))
        return (same and all(tells), "tells the listing's JZZ = 2 stores from l**5 unstored and DOKSD unstored",
                f"the listing equals the executable: {same}, red on each of the two: {tells}")
    raise ValueError(f"no pin check for the key {key}")


def setup_plane(dat_path: Path, homogenized_oxidizer_fraction: float = 0.0, tail_probability: float = 0.0,
                dokm_unroll: int = DOKM_UNROLL, jzz2_rules: Dict[str, bool] = JZZ2_RULES,
                zss_once: bool = False, zx_left_to_right: bool = False) -> Dict[str, object]:
    """The setup plane under `PrecisionKind.Original`, for one `.dat` formulation, at
    `ModelParameters.Default`'s own `HomogenizedOxidizerFraction`/`TailProbability`
    (both 0.0 -- the defaults every reference formulation and p350 run under). Mirrors
    `Setup.Prepare`/`FractionLaw.Build`/`Setup.AnalyticSizes`/`FractionLaw.TryTailDraw`
    line for line; `Particle.SizeLaw.Sample` (Fortran `SIZE`) is reused through the
    import above, not re-derived, since `x`/`x1` are already fully determined by this
    node's own deterministic tail draw -- no random draw is involved anywhere in this
    function."""
    formulation = _read_dat_formulation(dat_path)
    nmm = formulation["nmm"]
    jzz = formulation["jzz"]

    # Inputs as READ leaves them (BOOT.md, "## Setup plane"): GDOK and DDOK are REAL*4.
    gdok = [_to_binary32(v) for v in formulation["gdok_raw"]]
    ddok = []
    for v in formulation["ddok_um"]:
        # Lines 260-266: a value >= 0.1 is converted from micrometres, the product of two
        # binary32 operands stored back into the REAL*4 array -- rounded again on store
        # (Setup.cs's own ToBinary32MetresStored, distinct from Sizes' unconditional,
        # operand-only-rounded ToBinary32Metres/Multiply, used below for ddokmax alone).
        ddok.append(_to_binary32(_to_binary32(v) * _to_binary32(1e-6)) if v >= 0.1 else _to_binary32(v))

    # Ddokmax, the *reported* one (Setup.cs's own SetupEchoes.Ddokmax, Fortran line 271,
    # DDokmax = DDOK(2*kilo) -- a copy of the already-stored REAL*4 DDOK, not a second,
    # separately-rounded computation of it): the same store `ddok`'s own upper bounds
    # above already went through, so this reuses them rather than recomputing anything.
    # Distinct from Setup.Sizes's own internal ddokmax (`src/Statistics/BOOT.md`, "Array
    # sizes from binary32 values"), the unconditional, operand-only-rounded exception that feeds
    # only Ndok/Nkarm/Ncat's numerators and is out of this task's own scope, untouched by
    # it and not reproduced by this script at all. The two were found to disagree by
    # review (2026-09-24; `src/Statistics/BOOT.md`, "## Setup plane"): this function's
    # own `ddokmax` export was the wrong one, sharing `ddokmax_metres`'s single-rounded
    # formula instead of `ddok`'s double-rounded one, until this fix.
    ddokmax = max(ddok[1::2])

    # FractionLaw.Build (Fortran 1695-1734).
    coefficient_nonuniform = _to_binary32(3.0 / _to_binary32(3.14))
    coefficient_uniform = _to_binary32(24.0 / _to_binary32(3.14))

    # PARAM forms DOK(I)**2 and DOK(I+1)**2 separately and multiplies them (the denominator of the
    # non-uniform law is (l*l)*(u*u), rounded once; ((l*l)*u)*u, `zx_left_to_right`, would round twice and is
    # the variant the oracle's self-test shows the executed ZX to part from), and DOK**4 as
    # (x*x)*(x*x): the executable's own order, held by the oracle's executed PARAM (`expect` rows zx, z11).
    z = []
    for j in range(nmm):
        lower, upper = ddok[2 * j], ddok[2 * j + 1]
        lower_squared, upper_squared = lower * lower, upper * upper
        if jzz == 2:
            z.append(gdok[j] * coefficient_uniform * (upper - lower) / (upper_squared * upper_squared - lower_squared * lower_squared))
        else:
            denominator = lower * lower * upper * upper if zx_left_to_right else lower_squared * upper_squared
            z.append(gdok[j] * coefficient_nonuniform * (lower + upper) / denominator)

    # PARAM's ZS is a REAL*4 that the executable stores after every addition (a six-fold unrolled loop of
    # `fstp [ebp-4]`, 0x4182B7-0x418311). `zss_once` is the variant the oracle's self-test shows the pins to tell
    # from it: a double sum rounded once after the loop, which differs on 16 of the 49 shipped files.
    zss = 0.0
    for j in range(nmm):
        zss = zss + z[j] if zss_once else _to_binary32(zss + z[j])
    zss = _to_binary32(zss)

    zx = [z[j] / zss for j in range(nmm)]
    z11 = [0.0]
    for j in range(nmm):
        z11.append(z11[j] + zx[j])
    z11[nmm] = 1.0  # Z1(NM+1) = 1 (line 1732).

    # GGG, lambda (Fortran 376, 378).
    ggg0 = _to_binary32(formulation["ggg0"])
    gdokns = _to_binary32(homogenized_oxidizer_fraction)
    ggg = _to_binary32(ggg0 - gdokns * ggg0)
    plot1 = _to_binary32(formulation["plot1"])
    plot2 = _to_binary32(formulation["plot2"])
    plane = CyclePlane(original=True)
    g378 = plane.store("408ABE", plane.term("408ABE", lambda: ggg / plot1, {"ggg": ggg, "plot1": plot1}))
    lam = plane.store("408AD6", plane.term("408AD6", lambda: g378 * zss * plot2, {"zss": zss, "g378": g378, "plot2": plot2}))

    # Setup.AnalyticSizes (Fortran 379-406). The generated table does not reach these lines (its
    # scope ends at 0x408ADC); what the executable does here is read from its listing and held
    # by the oracle's pre-loop run (ListingOracleSetup.approved.txt). DOKSD's sum stays in a
    # register and line 405 subtracts DOKM**2 from it unrounded; DOKM's loop is unrolled five-fold
    # (stored after the fifth pass of a block and after every remainder pass); DOK4/DOK3 are
    # stored every pass.
    if jzz == 2:
        sums = jzz2_sums(gdok, ddok, zx, nmm, jzz2_rules)
        dokm, doksd = sums["dokm"], sums["doksd"]
    else:
        doksd_acc = 0.0
        for j in range(nmm):
            lower, upper = ddok[2 * j], ddok[2 * j + 1]
            doksd_acc += gdok[j] * (lower * lower + lower * upper + upper * upper) / 3.0
        dokm = 0.0
        unroll = dokm_unroll
        for j in range(nmm):
            lower, upper = ddok[2 * j], ddok[2 * j + 1]
            dokm += (lower + upper) * gdok[j] / 2.0
            pass_number = j + 1
            if pass_number > nmm // unroll * unroll or pass_number % unroll == 0:
                dokm = _to_binary32(dokm)
        doksd = _to_binary32(doksd_acc - dokm * dokm)

    # FractionLaw.TryTailDraw (Fortran 377, 1735-1754): this node's own range, transcribed
    # (not imported), matching Setup.cs/FractionLaw.cs's own algorithm line for line.
    active = [True] * nmm
    alfa = tail_probability
    while True:
        imax = -1
        candidate_upper = 0.0
        for i in range(nmm):
            if active[i] and ddok[2 * i + 1] > candidate_upper:
                candidate_upper = ddok[2 * i + 1]
                imax = i
        candidate = z11[imax + 1] - alfa
        if candidate - z11[imax] < 0.0:
            active[imax] = False
            alfa -= z11[imax + 1] - z11[imax]
            continue
        x = candidate
        x1 = (candidate - z11[imax]) / (z11[imax + 1] - z11[imax])
        tail_probability_modified = alfa
        break

    sampled = size_law_sample(jzz, x, x1, ddok, z11)

    return {
        "z11": z11,
        "zx": zx,
        "zss": zss,
        "lambda": lam,
        "dmax": sampled["diameter"],
        "dokm": dokm,
        "doksd": doksd,
        "ddokmax": ddokmax,
        "tailProbabilityModified": tail_probability_modified,
        "oxidizerMassFractionEffective": ggg,
    }


ORACLE_MENU_DEFAULTS = {
    # PropStructv3.for.txt lines 30-51: the menu's defaults as the Fortran assigns them, in the
    # units the menu asks for (lengths in metres or micrometres, as written).
    "dmin": 10.0e-6, "di": 10.0e-6, "dj": 10.0e-6, "alpha": 0.25, "nn_min": 3.0, "nn_max": 1.0e2,
    "eps_dok": 5.0e-2, "alfa": 0.0, "gdokns": 0.0, "eta": 0.0, "mkmcoef": 7.73, "karmcoef": 8.2, "ivar": 0,
}


def _menu_length(as_written: float) -> float:
    """The home a menu length ends in: READ gives a REAL*4; a value at or above 0.1 is
    micrometres and is multiplied by 1e-6 and stored (Fortran 260-266, which the original
    applies to Di, Dj and Dmin alike)."""
    value = _to_binary32(as_written)
    if value >= 0.1:
        return _to_binary32(value * _to_binary32(1e-6))
    return value


def oracle_setup(dat_path: Path, menu: Dict[str, object] = None, pocket_forming: Sequence[int] = None,
                 dokm_unroll: int = DOKM_UNROLL, zss_once: bool = False,
                 zx_left_to_right: bool = False) -> Dict[str, object]:
    """Every value the listing oracle injects before the executable's own code runs, by the
    lower-case name the Fortran gives it (`src/Statistics/CyclePlaneListing.map.txt`, the
    `oracle | inject` rows read these keys): the formulation's values as READ leaves them, the
    menu's as the original stores them, the arrays as allocated and filled, what PARAM returns
    (`setup_plane`), the three array sizes and the pre-loop's own answers (`lambda`, `dokm`,
    `doksd`) that the executed bytes must reproduce. `menu` holds the values as typed (lengths
    in metres or micrometres); `pocket_forming` is menu [12]'s flags, when read."""
    typed = dict(ORACLE_MENU_DEFAULTS)
    typed.update(menu or {})
    formulation = _read_dat_formulation(dat_path)
    ak1, ak2, ak3, ak4 = formulation["ak"]
    nmm = formulation["nmm"]
    cycles, particles, generator = formulation["kxx"], formulation["n"], formulation["gsv"]
    plane = setup_plane(dat_path, homogenized_oxidizer_fraction=_to_binary32(typed["gdokns"]),
                        tail_probability=_to_binary32(typed["alfa"]), dokm_unroll=dokm_unroll,
                        zss_once=zss_once, zx_left_to_right=zx_left_to_right)
    gdok = [_to_binary32(v) for v in formulation["gdok_raw"]]
    ddok = []
    for value in formulation["ddok_um"]:
        ddok.append(_to_binary32(_to_binary32(value) * _to_binary32(1e-6)) if value >= 0.1 else _to_binary32(value))
    di = _menu_length(typed["di"])
    dj = _menu_length(typed["dj"])
    ddokmax = plane["ddokmax"]
    ak4_stored = _to_binary32(ak4)
    setup: Dict[str, object] = {
        "plot1": _to_binary32(formulation["plot1"]), "plot2": _to_binary32(formulation["plot2"]),
        "ggg0": _to_binary32(formulation["ggg0"]), "gm": _to_binary32(formulation["gm"]),
        "ak1": _to_binary32(ak1), "ak2": _to_binary32(ak2), "ak3": _to_binary32(ak3), "ak4": ak4_stored,
        "nmm": nmm, "jzz": formulation["jzz"], "kxx": max(cycles, 1), "n": particles, "gsv": generator,
        "dmin": _menu_length(typed["dmin"]), "di": di, "dj": dj,
        "eps_dok": _to_binary32(typed["eps_dok"]), "alpha": _to_binary32(typed["alpha"]),
        "nn_min": _to_binary32(typed["nn_min"]), "nn_max": _to_binary32(typed["nn_max"]),
        "karmcoef": _to_binary32(typed["karmcoef"]), "mkmcoef": _to_binary32(typed["mkmcoef"]),
        "alfa": _to_binary32(typed["alfa"]), "gdokns": _to_binary32(typed["gdokns"]),
        "eta": _to_binary32(typed["eta"]), "ivar": int(typed["ivar"]),
        "gdok": gdok, "ddok": ddok, "sfr": list(pocket_forming) if pocket_forming is not None else [1] * nmm,
        "zx": plane["zx"], "z11": plane["z11"], "zss": plane["zss"], "ggg": plane["oxidizerMassFractionEffective"],
        "lambda": plane["lambda"], "dokm": plane["dokm"], "doksd": plane["doksd"], "ddokmax": ddokmax,
        "ndok": _binary32_truncated_quotient(ddokmax, di) + 2,
        "nkarm": math.trunc(ddokmax * ak4_stored / di) + 2,
        "ncat": math.trunc(ddokmax * ak4_stored / dj) + 2,
        "nc": 1000,
    }
    return setup


def setup_plane_cases() -> List[Dict[str, object]]:
    formulations_dir = NODE_ROOT / "Legacy" / "formulations"
    names = ["HPEPA3", "inpt", "P33", "PSAN02n", "HMX", "P350"]
    cases = []
    for name in names:
        expected = setup_plane(formulations_dir / f"{name}.dat")
        cases.append({"name": name, "datFile": f"{name}.dat", "expected": expected})
    return cases


# ---------------------------------------------------------------------------
# Categories (PropStructv3.for.txt, lines 825-959): the conditional oxidizer-size
# distribution by pocket category, with its iterative merge. Qdoks, Dokp41, Dokp31 are
# 0-based row-major (row r, oxidizer cell i) exactly like the port's own totals
# (src/Particle/API.md, "Accumulator layout"): this module never uses the Fortran
# 1-based convention, unlike formulas_particle.py, because Categories' own C# signature
# (src/Statistics/API.md) is already 0-based end to end and there is no Fortran-index
# caller to match.
# ---------------------------------------------------------------------------


def categories_merge_and_describe(
    ndok: int, ncat: int, cell_size: float, category_step: float, eps_dok: float, dp_max: float,
    qdoks: List[List[int]], dokp41: List[float], dokp31: List[float], plane: "CyclePlane" = BINARY64,
    trace: Dict[str, object] = None,
) -> Dict[str, object]:
    """`trace`, when given, receives the Epsydok of every row in the first pass (`epsydok`, as stored)
    and the same before its store (`epsydokRegister`): what the controls' search reads to set `EpsDok`."""
    qdoks = [row[:] for row in qdoks]  # local copy: the caller inspects post-call mutation separately.
    dokp41 = dokp41[:]
    dokp31 = dokp31[:]

    # DPmax and Dj are REAL*4 in the original (root BOOT.md, "Double precision only",
    # the binary32 exception extended to DPRow); `int(math.floor(dp_max/category_step))`
    # -- this script's own previous version -- is a plain-double division, which the
    # "whole_fraction_count_binary32_boundary"-style boundary of `_binary32_truncated_
    # quotient`'s own docstring can move by one row at an integer boundary, exactly the
    # C166 mechanism the root BOOT.md's own Ndok evidence records.
    row = _binary32_truncated_quotient(dp_max, category_step) + 1
    dpockets = [0.0] * ncat
    for r in range(row):
        # Fortran: `do irow = 1,DPRow: Dpockets(irow) = irow*Dj` -- no clamp to Ncat.
        # DPRow is always <= Ncat by construction of the model (the category array is
        # sized in Setup.Sizes from the same Ddokmax*Ak4/Dj as the largest pocket radius
        # the run can produce), so this range is never wider than `dpockets` itself for
        # any case built here; `Categories.MergeAndDescribe` reports the violation as a
        # status (src/Statistics/BOOT.md, "## Categories"), and this script assumes the
        # invariant holds for every case it constructs, matching the port.
        dpockets[r] = plane.store("40C61E", plane.term("40C61E", lambda: (r + 1) * category_step, {"i": r + 1, "dj": category_step}))

    mdok3 = [0.0] * ncat
    mdok4 = [0.0] * ncat
    epsydok = [0.0] * ncat
    dokp43 = [0.0] * ncat
    qdokkarm = [0.0] * ncat
    qdokso = [[0.0] * ndok for _ in range(ncat)]

    def cell_centre(i: int) -> float:
        return cell_size * (i + 0.5)

    def merge_row(target: int, source: int, at41: str, at31: str) -> None:
        dpockets[target] = dpockets[source]
        for i in range(ndok):
            qdoks[target][i] += qdoks[source][i]
        dokp41[target] = plane.store(at41, dokp41[target] + dokp41[source])
        dokp31[target] = plane.store(at31, dokp31[target] + dokp31[source])

    def shift_row(target: int, source: int) -> None:
        dpockets[target] = dpockets[source]
        for i in range(ndok):
            qdoks[target][i] = qdoks[source][i]
        dokp41[target] = dokp41[source]
        dokp31[target] = dokp31[source]

    def recompute() -> None:
        for r in range(row):
            mdok3[r] = 0.0
            mdok4[r] = 0.0
            dokp43[r] = 0.0
            qdokkarm[r] = 0.0
            for i in range(ndok):
                qdokso[r][i] = 0.0

            row_sum = sum(qdoks[r][i] for i in range(ndok))

            # QDOKS1 (848) feeds the moments, QDOKSO (897) qdokkarm and the report: the same quotient, stored twice.
            def quotient(at: str, i: int) -> float:
                return plane.store(at, plane.term(at, lambda: qdoks[r][i] / row_sum, {"qdoks": qdoks[r][i], "qdokss": row_sum}))

            qdoks1 = [0.0 if row_sum == 0 else quotient("40CE8F", i) for i in range(ndok)]
            for i in range(ndok):
                qdokso[r][i] = 0.0 if row_sum == 0 else quotient("40D713", i)

            for i in range(ndok):
                cell = cell_centre(i)
                mdok4[r] += plane.term("40D07E", lambda: cell ** 4.0 * qdoks1[i], {"i": i + 1, "di": cell_size, "qdoks1": qdoks1[i]})
                mdok3[r] += plane.term("40D090", lambda: cell ** 3.0 * qdoks1[i], {"i": i + 1, "di": cell_size, "qdoks1": qdoks1[i]})

            ddok3 = 0.0
            ddok4 = 0.0
            for i in range(ndok):
                cell = cell_centre(i)
                ddok4 += plane.term("40D360", lambda: (cell ** 4.0 - mdok4[r]) ** 2 * qdoks1[i],
                                    {"i": i + 1, "di": cell_size, "mdok4_irow": mdok4[r], "qdoks1": qdoks1[i]})
                ddok3 += plane.term("40D37C", lambda: (cell ** 3.0 - mdok3[r]) ** 2 * qdoks1[i],
                                    {"i": i + 1, "di": cell_size, "mdok3_irow": mdok3[r], "qdoks1": qdoks1[i]})

            # 869's two compares against the folded 1e-30: MDOK3's reuses 856's flags, MDOK4's is its own.
            _, tiny3 = plane.term("40D104", lambda: (mdok3[r], 1e-30), {"mdok3_irow@register": mdok3[r], "mdok3_irow": mdok3[r]})
            _, tiny4 = plane.term("40D3FC", lambda: (mdok4[r], 1e-30), {"mdok4_irow": mdok4[r]})
            if row_sum == 0 or mdok3[r] <= tiny3 or mdok4[r] <= tiny4:
                epsydok[r] = 0.0
            else:
                inverse = plane.store("40D45C", plane.term("40D45C", lambda: 1.0 / row_sum, {"qdokss": row_sum}))
                register = plane.term(
                    "40D48F", lambda: 3.0 * math.sqrt(inverse * (ddok4 / mdok4[r] ** 2 + ddok3 / mdok3[r] ** 2)),
                    {"qdokss_inverse": inverse, "ddok3": ddok3, "mdok3_irow": mdok3[r], "ddok4": ddok4, "mdok4_irow": mdok4[r]})
                epsydok[r] = plane.store("40D48F", register)
                if trace is not None:
                    trace.setdefault("epsydokRegister", {})[r] = register

            for i in range(ndok):
                qdokkarm[r] = plane.store("40D769", qdokkarm[r] + plane.term(
                    "40D769", lambda: qdokso[r][i] * cell_centre(i), {"i": i + 1, "di": cell_size, "qdokso": qdokso[r][i]}))

            _, tiny_dokp = plane.term("40D7AB", lambda: (dokp31[r], 1e-30), {"dokp31": dokp31[r]})
            dokp43[r] = 0.0 if dokp31[r] <= tiny_dokp \
                else plane.store("40D7D8", plane.term("40D7D8", lambda: dokp41[r] / dokp31[r], {"dokp41": dokp41[r], "dokp31": dokp31[r]}))

    while True:
        recompute()
        if trace is not None and "epsydok" not in trace:
            trace["epsydok"] = epsydok[:]
        if row <= 1:
            break

        shift_flag = False
        for r in range(row - 1):
            if not shift_flag:
                if epsydok[r] > eps_dok:
                    merge_row(r, r + 1, "40D9F6", "40DA01")
                    shift_flag = True
            else:
                shift_row(r, r + 1)

        if not shift_flag:
            break
        row -= 1

    if row > 1 and epsydok[row - 1] > eps_dok:
        merge_row(row - 2, row - 1, "40DEC6", "40DEDF")
        row -= 1

    return {
        "dpRow": row,
        "dpockets": dpockets[:row],
        "dokp43": dokp43[:row],
        "qdokkarm": qdokkarm[:row],
        "qdokso": [qdokso[r][:] for r in range(row)],
        "qdoksAfter": qdoks,
        "dokp41After": dokp41,
        "dokp31After": dokp31,
    }


# ---------------------------------------------------------------------------
# Cases. Every input here is constructed by hand; every expected value comes from
# calling the function above, never typed (root BOOT.md Taboos).
# ---------------------------------------------------------------------------


def categories_cases() -> List[Dict[str, object]]:
    cases = []

    # C1: a single merge pass, no shift needed (only two rows: row 0 absorbs row 1;
    # the loop range r in 0..DpRow-2 has one iteration, so shift_flag never applies to
    # a later row). Row 0's own distribution is spread over two cells (nonzero
    # epsydok); eps_dok is tiny so it exceeds it. Row 1's own distribution stays
    # concentrated in one cell (epsydok = 0) precisely to also cover that branch of
    # line 869-873 for a non-empty row, alongside C4's QDOKSS = 0 row below.
    ndok = 3
    dj = 1.0
    di = 1.0
    qdoks = [[5, 5, 0], [0, 0, 10]]
    dokp41 = [5.0, 7.0]
    dokp31 = [1.0, 1.0]
    dp_max = 1 * dj  # trunc(1/1)+1 = 2 rows.
    result = categories_merge_and_describe(ndok, 4, di, dj, 1e-6, dp_max, qdoks, dokp41, dokp31)
    original = categories_merge_and_describe(ndok, 4, di, dj, 1e-6, dp_max, qdoks, dokp41, dokp31, CyclePlane(original=True))
    cases.append({
        "name": "single_merge_pass_two_rows",
        "ndok": ndok, "ncat": 4, "cellSize": di, "categoryStep": dj, "epsDok": 1e-6, "dpMax": dp_max,
        "qdoks": qdoks, "dokp41": dokp41, "dokp31": dokp31,
        "expected": result,
        "expectedOriginal": original,
    })

    # C2: three rows; row 0's own distribution ([5,5,0], spread over two cells) exceeds
    # eps_dok = 0.5 and absorbs row 1 (a merge, shift_flag set); the loop's next
    # iteration (r=1) then unconditionally copies row 2 into row 1 (a shift), never
    # consulting row 1's own epsydok. Row 1 is engineered ([0,20,0], concentrated in
    # the same cell row 0 is heaviest in) so the merged row 0 ([5,25,0]) is *less*
    # spread than before (epsydok 0.336 < 0.5) and row 2 ([0,10,0]) is concentrated
    # (epsydok 0): the loop's next full pass (`go to 600`) recomputes and finds nothing
    # left to merge, so it stops after exactly this one merge-then-shift pass.
    ndok = 3
    dj = 1.0
    di = 1.0
    eps_dok = 0.5
    qdoks = [[5, 5, 0], [0, 20, 0], [0, 10, 0]]
    dokp41 = [5.0, 7.0, 9.0]
    dokp31 = [1.0, 1.0, 1.0]
    dp_max = 2 * dj  # trunc(2/1)+1 = 3 rows.
    result = categories_merge_and_describe(ndok, 4, di, dj, eps_dok, dp_max, qdoks, dokp41, dokp31)
    original = categories_merge_and_describe(ndok, 4, di, dj, eps_dok, dp_max, qdoks, dokp41, dokp31, CyclePlane(original=True))
    cases.append({
        "name": "merge_then_shift_three_rows",
        "ndok": ndok, "ncat": 4, "cellSize": di, "categoryStep": dj, "epsDok": eps_dok, "dpMax": dp_max,
        "qdoks": qdoks, "dokp41": dokp41, "dokp31": dokp31,
        "expected": result,
        "expectedOriginal": original,
    })

    # C3: rows 0 and 1 are both concentrated in one cell each (epsydok = 0 for both),
    # so the main pass (911-945) never merges and shift_flag never sets: the loop
    # breaks after one no-op pass, row count unchanged at 3. Row 2 alone is spread
    # ([5,5,0], epsydok 1.278 > eps_dok = 0.5): only the separate last-row check
    # (947-959) fires, merging rows 1 and 2 without a further recompute (BOOT.md,
    # "## Categories", step 4) -- row 1's own DOKP43 in the report is therefore its
    # *pre-merge* value (7.0 = 7/1), not the merged 8.0 (16/2), proving the report
    # takes the stale statistics.
    ndok = 3
    dj = 1.0
    di = 1.0
    eps_dok = 0.5
    qdoks = [[0, 10, 0], [0, 10, 0], [5, 5, 0]]
    dokp41 = [5.0, 7.0, 9.0]
    dokp31 = [1.0, 1.0, 1.0]
    dp_max = 2 * dj  # 3 rows.
    result = categories_merge_and_describe(ndok, 4, di, dj, eps_dok, dp_max, qdoks, dokp41, dokp31)
    original = categories_merge_and_describe(ndok, 4, di, dj, eps_dok, dp_max, qdoks, dokp41, dokp31, CyclePlane(original=True))
    cases.append({
        "name": "last_row_merge_without_recompute",
        "ndok": ndok, "ncat": 4, "cellSize": di, "categoryStep": dj, "epsDok": eps_dok, "dpMax": dp_max,
        "qdoks": qdoks, "dokp41": dokp41, "dokp31": dokp31,
        "expected": result,
        "expectedOriginal": original,
    })

    # C4: an empty row (QDOKSS = 0 for one category) among non-empty ones: its epsydok
    # is 0 (the QDOKSS = 0 branch of line 869-873), so it never triggers a merge, and
    # its QDOKSO/DOKP43 are the 0-fallback of lines 845-846, 902-903.
    ndok = 2
    dj = 1.0
    di = 1.0
    qdoks = [[0, 0], [10, 10]]
    dokp41 = [0.0, 5.0]
    dokp31 = [0.0, 1.0]
    dp_max = 1 * dj  # 2 rows.
    result = categories_merge_and_describe(ndok, 4, di, dj, 1e-6, dp_max, qdoks, dokp41, dokp31)
    original = categories_merge_and_describe(ndok, 4, di, dj, 1e-6, dp_max, qdoks, dokp41, dokp31, CyclePlane(original=True))
    cases.append({
        "name": "empty_row_qdokss_zero",
        "ndok": ndok, "ncat": 4, "cellSize": di, "categoryStep": dj, "epsDok": 1e-6, "dpMax": dp_max,
        "qdoks": qdoks, "dokp41": dokp41, "dokp31": dokp31,
        "expected": result,
        "expectedOriginal": original,
    })

    return cases


# ---------------------------------------------------------------------------
# Small particles (PropStructv3.for.txt, lines 1021-1074): the probability of a small
# oxidizer particle inside a pocket around a base particle (`SmallParticles.Probability`)
# and the largest base size that probability still reaches (`SmallParticles.MaxSize`).
# The loop variables `kilo`/`iks` stay 1-based inside these two functions, matching the
# Fortran source directly statement for statement; the returned arrays are 0-based
# Python lists, the same convention as the rest of this file (`categories_cases`'
# own note applies here too).
# ---------------------------------------------------------------------------


def small_particles_probability(
    ndok: int, cell_size: float, ak2: float, ggg: float, gdokleft: float, karmcoef: float,
    oxidizer_density: float, propellant_density: float, vdokstr: Sequence[float], plane: "CyclePlane" = BINARY64,
) -> List[float]:
    vdokstr_sum = _sequential_sum(vdokstr)

    zdoksmall = [0.0] * ndok
    ddoksmall = [0.0] * ndok
    for kilo in range(1, ndok + 1):
        real_kilo = plane.store("40ECB8", plane.term("40ECB8", lambda: float(kilo), {"i": kilo}))
        xr = plane.store("40ECDF", math.fmod(real_kilo, ak2) / ak2)
        # Fortran int(real(kilo)/ak2): kilo converted to REAL and AK2 read as REAL*4
        # (root BOOT.md, "Double precision only", the binary32 exception extended to
        # this whole-fraction count); Xr's own mod/division above stays plain double
        # (BOOT.md, "Fidelity to the original" -- declared, not REAL*4: only the
        # truncated integer count gets the binary32 treatment, not every use of ak2).
        whole = _binary32_truncated_quotient(kilo, ak2)

        # 1028: the sum is carried in the register and copied to the home every pass, so the
        # home holds the rounded final sum.
        register = zdoksmall[kilo - 1]
        for iks in range(1, whole + 1):
            register = register + plane.term("40EDFF", lambda: vdokstr[iks - 1] / vdokstr_sum,
                                             {"vdokstr": vdokstr[iks - 1], "merge@40EDE1": vdokstr_sum})
        zdoksmall[kilo - 1] = plane.store("40EDFF", register)

        # 1031 re-reads the home; its register is what 1034 compares and 1036, 1039 divide by.
        register = zdoksmall[kilo - 1] + plane.term(
            "40EEBC", lambda: vdokstr[whole] / vdokstr_sum * xr, {"vdokstr": vdokstr[whole], "merge@40EE93": vdokstr_sum, "xr": xr})
        zdoksmall[kilo - 1] = plane.store("40EEBC", register)
        compared, threshold = plane.term("40EEC0", lambda: (register, 1e-5),
                                         {"zdoksmall@register": register, "zdoksmall": zdoksmall[kilo - 1]})

        if compared > threshold:
            weighted = ddoksmall[kilo - 1]
            for iks in range(1, whole + 1):
                weighted = weighted + plane.term(
                    "40EFC2", lambda: vdokstr[iks - 1] / vdokstr_sum / register * cell_size * (iks - 0.5),
                    {"i": iks, "vdokstr": vdokstr[iks - 1], "merge@40EF78": vdokstr_sum, "zdoksmall@register": register,
                     "zdoksmall": zdoksmall[kilo - 1], "di": cell_size})
            ddoksmall[kilo - 1] = plane.store("40EFC2", weighted)
            ddoksmall[kilo - 1] = plane.store("40F077", ddoksmall[kilo - 1] + plane.term(
                "40F077", lambda: vdokstr[whole] * xr / vdokstr_sum / register * cell_size * (whole + 0.5 * xr),
                {"i": whole, "xr": xr, "vdokstr": vdokstr[whole], "merge@40F046": vdokstr_sum, "zdoksmall@register": register,
                 "zdoksmall": zdoksmall[kilo - 1], "di": cell_size}))

    for kilo in range(1, ndok + 1):
        zdoksmall[kilo - 1] = plane.store("40F100", plane.term("40F100", lambda: zdoksmall[kilo - 1] * ggg,
                                                               {"zdoksmall": zdoksmall[kilo - 1], "ggg": ggg}))

    # The compiler's temporaries, recomputed from the same operands at each use.
    t1 = plane.store("40950F", plane.term("40950F", lambda: 1.0 / propellant_density, {"plot2": propellant_density}))
    t2 = plane.store("409521", plane.term("409521", lambda: 1.0 - ggg, {"ggg": ggg}))
    s1 = plane.store("40EA8D", plane.term("40EA8D", lambda: ggg - gdokleft, {"ggg": ggg, "gdokleft": gdokleft}))
    s2 = plane.store("40EAC9", plane.term("40EAC9", lambda: 1.0 - ggg + gdokleft, {"t2": t2, "gdokleft": gdokleft}))

    vdoksmall = [0.0] * ndok
    for kilo in range(1, ndok + 1):
        z = zdoksmall[kilo - 1]
        pl = plane.store("40F1C7", plane.term(
            "40F1C7", lambda: (1.0 - (s1 - z)) / (t1 - (s1 - z) / oxidizer_density),
            {"s1": s1, "zdoksmall": z, "t1": t1, "plot1": oxidizer_density}))
        vdoksmall[kilo - 1] = plane.store("40F1DD", plane.term(
            "40F1DD", lambda: z / (s2 + z) * pl / oxidizer_density,
            {"zdoksmall": z, "s2": s2, "pl@register": pl, "plot1": oxidizer_density}))
        ddoksmall[kilo - 1] = plane.store("40F1F7", plane.term(
            "40F1F7", lambda: ddoksmall[kilo - 1] / (cell_size * kilo), {"ddoksmall": ddoksmall[kilo - 1], "i": kilo, "di": cell_size}))

    pdoksmall = [0.0] * ndok  # pdoksmall(1): never assigned by the original (port decision, src/Statistics/BOOT.md).
    for kilo in range(2, ndok + 1):
        register = vdoksmall[kilo - 1] * ddoksmall[kilo - 1] * karmcoef
        pdoksmall[kilo - 1] = plane.store("40F4A5", register)
        if register < pdoksmall[kilo - 2]:
            pdoksmall[kilo - 1] = pdoksmall[kilo - 2]

    return pdoksmall


def small_particles_max_size(ndok: int, cell_size: float, ddokmax: float, karmcoef: float, mkmcoef: float, pdoksmall: Sequence[float],
                             plane: "CyclePlane" = BINARY64) -> float:
    dmaxxx = ddokmax
    for kilo in range(1, ndok + 1):
        reached, one = plane.term("40F650", lambda: (mkmcoef / karmcoef * pdoksmall[kilo - 1], 1.0),
                                  {"mkmcoef": mkmcoef, "karmcoef": karmcoef, "pdoksmall": pdoksmall[kilo - 1]})
        if reached >= one:
            dmaxxx = plane.store("40F728", plane.term("40F728", lambda: cell_size * kilo, {"i": kilo, "di": cell_size}))
            break
    return dmaxxx


def small_particles_probability_cases() -> List[Dict[str, object]]:
    cases = []

    # P1: vdokstr concentrated in cell 1 (index 0): kilo = 1 and 2 (whole = 0, 1 for
    # ak2 = 2.0) both land on the zdoksmall(kilo) <= 1e-5 skip branch (lines 1032-1033
    # never taken because the earlier cells hold almost nothing), then kilo = 3 and 4
    # both compute the same raw ratio (the source is one dominant, already-summed cell),
    # so the monotone clamp (lines 1062-1064) copies pdoksmall(3) forward into
    # pdoksmall(4) unchanged, covering both branches: "skip when zdoksmall is small" and
    # "clamp when the raw value would decrease".
    ndok = 4
    args = dict(ndok=ndok, cellSize=1.0, ak2=2.0, ggg=0.5, gdokleft=0.1, karmcoef=1.0,
                oxidizerDensity=2.0, propellantDensity=2.0, vdokstr=[5.0, 0.1, 0.1, 0.1])
    pdoksmall = small_particles_probability(
        args["ndok"], args["cellSize"], args["ak2"], args["ggg"], args["gdokleft"], args["karmcoef"],
        args["oxidizerDensity"], args["propellantDensity"], args["vdokstr"])
    pdoksmall_original = small_particles_probability(
        args["ndok"], args["cellSize"], args["ak2"], args["ggg"], args["gdokleft"], args["karmcoef"],
        args["oxidizerDensity"], args["propellantDensity"], args["vdokstr"], CyclePlane(original=True))
    cases.append({"name": "concentrated_source_clamps_the_tail", **args, "expected": {"pdoksmall": pdoksmall}, "expectedOriginal": {"pdoksmall": pdoksmall_original}})

    # P2: vdokstr's own first cell is nearly empty (1e-9 against three cells of 5.0
    # each): kilo = 1 and 2 both land under the 1e-5 threshold (their own zdoksmall
    # never accumulates more than the near-empty first cell's tiny share), so
    # pdoksmall(1) and pdoksmall(2) both come from the `pdoksmall(1) = 0` port decision
    # and the clamp, while kilo = 3 and 4 cross the threshold and compute an ordinary,
    # increasing ddoksmall/vdoksmall pair (the clamp never fires here, the other branch
    # P1 does not reach).
    args2 = dict(ndok=ndok, cellSize=1.0, ak2=2.0, ggg=0.5, gdokleft=0.1, karmcoef=1.0,
                 oxidizerDensity=2.0, propellantDensity=2.0, vdokstr=[1e-9, 5.0, 5.0, 5.0])
    pdoksmall2 = small_particles_probability(
        args2["ndok"], args2["cellSize"], args2["ak2"], args2["ggg"], args2["gdokleft"], args2["karmcoef"],
        args2["oxidizerDensity"], args2["propellantDensity"], args2["vdokstr"])
    pdoksmall2_original = small_particles_probability(
        args2["ndok"], args2["cellSize"], args2["ak2"], args2["ggg"], args2["gdokleft"], args2["karmcoef"],
        args2["oxidizerDensity"], args2["propellantDensity"], args2["vdokstr"], CyclePlane(original=True))
    cases.append({"name": "near_empty_source_skips_the_early_cells", **args2, "expected": {"pdoksmall": pdoksmall2}, "expectedOriginal": {"pdoksmall": pdoksmall2_original}})

    # P3: the whole-fraction count's own binary32 boundary (root BOOT.md, "Double
    # precision only", the exception extended to this count; `_binary32_truncated_
    # quotient`'s own docstring). ak2 is one double ULP above 2.0 -- the kind of noise a
    # parsed coefficient can carry -- so kilo = 2's raw double quotient 2/ak2 is just
    # under 1.0 (truncating to 0), but ak2 itself rounds to exactly binary32 2.0 (its own
    # REAL*4 storage in the original), giving 2/2.0 = 1.0 exactly (truncating to 1): the
    # same double-rounding shape as the C166 evidence for Ndok. The two truncations give
    # visibly different pdoksmall(2): 0.28125 (whole = 1, both vdokstr cells reachable)
    # against 0.0681... (whole = 0, only the tail term) if this script's own binary32
    # rounding were removed -- SmallParticlesTests.ProbabilityUsesBinary32ArithmeticForThe
    # WholeFractionCount reads this case and pins the assertion to the specific property.
    ndok3 = 2
    args3 = dict(ndok=ndok3, cellSize=1.0, ak2=2.0000000000000004, ggg=0.5, gdokleft=0.0, karmcoef=1.0,
                 oxidizerDensity=2.0, propellantDensity=2.0, vdokstr=[3.0, 5.0])
    pdoksmall3 = small_particles_probability(
        args3["ndok"], args3["cellSize"], args3["ak2"], args3["ggg"], args3["gdokleft"], args3["karmcoef"],
        args3["oxidizerDensity"], args3["propellantDensity"], args3["vdokstr"])
    pdoksmall3_original = small_particles_probability(
        args3["ndok"], args3["cellSize"], args3["ak2"], args3["ggg"], args3["gdokleft"], args3["karmcoef"],
        args3["oxidizerDensity"], args3["propellantDensity"], args3["vdokstr"], CyclePlane(original=True))
    cases.append({"name": "whole_fraction_count_binary32_boundary", **args3, "expected": {"pdoksmall": pdoksmall3}, "expectedOriginal": {"pdoksmall": pdoksmall3_original}})

    # P4: the per-cycle plane's register reads, under PrecisionKind.Original (A6 of the
    # 2026-10-01 verdict): inputs found by a seeded search (2026-10-01) so that two
    # mistakes each change a stored bit of pdoksmall -- rounding `Pl` at 1050, which the
    # generated classification forwards to 1053 unrounded, and rounding `zdoksmall(kilo)`
    # on every iteration of 1027-1029, which the listing's X site carries in the register.
    # P1-P3 are too short for either to reach a stored bit.
    args4 = dict(ndok=ndok, cellSize=1.0, ak2=2.0, ggg=0.5, gdokleft=0.1, karmcoef=1.0,
                 oxidizerDensity=2.0, propellantDensity=1.7, vdokstr=[2.0, 3.1, 0.7, 9.7])
    pdoksmall4 = small_particles_probability(
        args4["ndok"], args4["cellSize"], args4["ak2"], args4["ggg"], args4["gdokleft"], args4["karmcoef"],
        args4["oxidizerDensity"], args4["propellantDensity"], args4["vdokstr"])
    pdoksmall4_original = small_particles_probability(
        args4["ndok"], args4["cellSize"], args4["ak2"], args4["ggg"], args4["gdokleft"], args4["karmcoef"],
        args4["oxidizerDensity"], args4["propellantDensity"], args4["vdokstr"], CyclePlane(original=True))
    cases.append({"name": "register_reads_and_loop_sums_reach_a_stored_bit", **args4, "expected": {"pdoksmall": pdoksmall4}, "expectedOriginal": {"pdoksmall": pdoksmall4_original}})

    return cases


def small_particles_max_size_cases() -> List[Dict[str, object]]:
    cases = []

    # Reuses P1's own pdoksmall (never typed): mkmcoef/karmcoef = 10 crosses 1.0 at
    # kilo = 2 (pdoksmall(2) = 0.110...), so Dmaxxx = cellSize * 2, not Ddokmax.
    pdoksmall = small_particles_probability(4, 1.0, 2.0, 0.5, 0.1, 1.0, 2.0, 2.0, [5.0, 0.1, 0.1, 0.1])
    args = dict(ndok=4, cellSize=1.0, ddokmax=5.0, karmcoef=1.0, mkmcoef=10.0, pdoksmall=pdoksmall)
    dmaxxx = small_particles_max_size(args["ndok"], args["cellSize"], args["ddokmax"], args["karmcoef"], args["mkmcoef"], args["pdoksmall"])
    dmaxxx_original = small_particles_max_size(args["ndok"], args["cellSize"], args["ddokmax"], args["karmcoef"], args["mkmcoef"], args["pdoksmall"], CyclePlane(original=True))
    cases.append({"name": "threshold_reached_at_kilo_two", **args, "expected": {"dmaxxx": dmaxxx}, "expectedOriginal": {"dmaxxx": dmaxxx_original}})

    # Same pdoksmall, mkmcoef/karmcoef = 5 never reaches 1.0 (max pdoksmall * 5 =
    # 0.550): Dmaxxx stays Ddokmax.
    args2 = dict(ndok=4, cellSize=1.0, ddokmax=5.0, karmcoef=1.0, mkmcoef=5.0, pdoksmall=pdoksmall)
    dmaxxx2 = small_particles_max_size(args2["ndok"], args2["cellSize"], args2["ddokmax"], args2["karmcoef"], args2["mkmcoef"], args2["pdoksmall"])
    dmaxxx2_original = small_particles_max_size(args2["ndok"], args2["cellSize"], args2["ddokmax"], args2["karmcoef"], args2["mkmcoef"], args2["pdoksmall"], CyclePlane(original=True))
    cases.append({"name": "threshold_never_reached", **args2, "expected": {"dmaxxx": dmaxxx2}, "expectedOriginal": {"dmaxxx": dmaxxx2_original}})

    return cases


# ---------------------------------------------------------------------------
# The rest of the per-cycle pipeline (PropStructv3.for.txt, lines 771-819, 963-1016,
# 1087-1113, 1137-1152, 1168-1174; the category merge and the small particles above are
# called, not re-transcribed): `CycleStatistics.Compute`'s GeneratorAccuracy,
# OxidizerSizes, Pockets, Matrix, CorrectedPockets, MassFractions and Convergence. It is
# written from the Fortran and from CyclePlane.listing.generated.txt alone, never from the C#,
# but for the association `_centre` names.
#
# Every expression keeps the Fortran's own operand order (a*b/c is (a*b)/c), but for the
# cell centre formed first (`_centre`); every store, literal, register read and REAL*4
# loop sum asks `plane` what the generated file says about that line. Quantities of
# REAL*8 (xss*, sd*, d41, dok_*, vks, dp*, md*, dd*) are plain double. IEEE results are
# kept where the original computes them: a zero divisor gives inf or NaN, a negative
# radicand NaN. Arrays are 0-based; a cell k of the Fortran's 1-based loop is index + 1,
# so `(kilo-0.5)` is `index + 0.5`.
# ---------------------------------------------------------------------------

_NAN = float("nan")
_INF = float("inf")


def _div(numerator: float, denominator: float) -> float:
    try:
        return numerator / denominator
    except ZeroDivisionError:
        if numerator != numerator or numerator == 0:
            return _NAN
        return math.copysign(_INF, numerator) * math.copysign(1.0, denominator)


def _sqrt(value: float) -> float:
    return _NAN if value < 0 else math.sqrt(value)


def _pow(base: float, exponent: float) -> float:
    return _NAN if base < 0 else math.pow(base, exponent)


def _square(value: float) -> float:
    """x**2 with the integer exponent: the Fortran multiplies, it does not call pow."""
    return value * value


def _centre(cell_size: float, index: int) -> float:
    """Di*(kilo - 0.5) for the 1-based cell kilo = index + 1, formed once per cell.

    Association (fixed by src/Statistics/API.md, "## Cycle", since 2026-10-02, read off
    Compute): the Fortran writes the linear terms `x*Di*(kilo-0.5)` (989, 991, 1089,
    1093, 1101) and `ALLVDOKSO*(kilo-0.5)*Di` (805) left to right, i.e.
    `(x*Di)*(kilo-0.5)`, and the squares `x*(Di*(kilo-0.5))**2` as `x*(c*c)`. `Compute`
    forms the centre first, `x*c` and `(x*c)*c`, which differs from the Fortran's order
    in the last bit of a `double` under `Binary64` (a seeded case: alldok432
    3.168181003715818e-05 in the Fortran's order, 3.1681810037158176e-05 here and in
    `Compute`) and is hidden under `Original` by the binary32 stores in every case
    measured. This transcription takes the one `Compute` takes, as
    `categories_merge_and_describe` already does for `qdokkarm`, and the disagreement is
    reported, not hidden.

    Correction, 2026-10-02: this docstring said the original keeps its REAL*4 operands
    in 80-bit registers, so that no association of a 53-bit program is its own. Refuted
    by the executable's listing (`dumpbin /disasm` of Legacy/PropStructV3.exe): the C
    runtime sets 53-bit precision, `_controlfp(0x10000, 0x30000)` at 0x418D7A, and the
    executable multiplies in the written order at 805, 806, 989, 990 and 991
    (src/Statistics/API.md, "## Cycle", the same date's note). The rule above stays;
    whether to change it is a decision for a later session."""
    return cell_size * (index + 0.5)


_XSR_SITES = ("40BDBB", "40BDE4", "40BE0D", "40BE36", "40BE5F", "40BE8B", "40BEB7")
_EPS_SITES = ("40BDD5", "40BDFE", "40BE27", "40BE50", "40BE79", "40BEA5", "40BED1")


def generator_accuracy(plane: "CyclePlane", xss: Sequence[float], nfx: int, nfy: int, nfz: int, nfq: int, nfw: int) -> Dict[str, float]:
    """Lines 771-784. Streams 4 and 5 both divide by NFZ (777-780)."""
    counts = [nfx, nfx, nfy, nfz, nfz, nfq, nfw]
    result = {}
    for k in range(7):
        xsr = plane.store(_XSR_SITES[k], plane.term(_XSR_SITES[k], lambda: _div(xss[k], counts[k]), {f"xss{k}": xss[k], "i": counts[k]}))
        result[f"eps{k + 1}"] = plane.store(_EPS_SITES[k], plane.term(
            _EPS_SITES[k], lambda: abs(_div(xsr - 0.5, 0.5)), {f"xsr{k}": xsr}))
    return result


def oxidizer_sizes(plane: "CyclePlane", case: Dict[str, object]) -> Dict[str, object]:
    """Lines 789-819."""
    di = case["setup"]["cellSize"]
    dokm = case["echoes"]["dokm"]
    eps_dok = case["inputs"]["epsDok"]
    zx = case["tables"]["share"]
    rt = case["realTotals"]
    it = case["integerTotals"]

    dok43b = plane.store("40BEE3", plane.term("40BEE3", lambda: _div(rt["sd4"], rt["sd3"]), {"sd4": rt["sd4"], "sd3": rt["sd3"]}))
    dok43s = plane.store("40BF07", plane.term("40BF07", lambda: _div(rt["d41"], rt["d31"]), {"d41": rt["d41"], "d31": rt["d31"]}))

    alldokq = sum(it["alldok"])
    epsalldok = []
    for k in range(len(zx)):
        register = plane.store("40BFFD", plane.term("40BFFD", lambda: _div(float(it["alldokFract"][k]), float(alldokq)),
                                                    {"alldok_fract": it["alldokFract"][k], "i": alldokq}))
        epsalldok.append(plane.store("40C007", plane.term(
            "40C007", lambda: _div(abs(register - zx[k]), zx[k]), {"epsalldok@register": register, "zx": zx[k]})))

    allvdok = rt["allvdok"]
    trips = len(allvdok)
    allvdoks = plane.store("40C175", _sequential_sum(allvdok))
    allvdokso = []
    fmdok = [0.0]
    fmdok_register = 0.0
    alldok432 = 0.0
    alldok243 = 0.0
    for k in range(trips):
        pass_number = k + 1
        register = plane.term("40C1E5", lambda: _div(allvdok[k], allvdoks), {"allvdok": allvdok[k], "allvdoks@register": allvdoks})
        allvdokso.append(plane.store("40C1E5", register))
        previous = fmdok[k] if plane.reloads_before("40C22C", pass_number, trips) else fmdok_register
        fmdok_register = previous + register
        fmdok.append(plane.store("40C22C", fmdok_register))
        alldok432 = alldok432 + plane.term("40C3B7", lambda: register * _centre(di, k),
                                           {"i": pass_number, "allvdokso@register": register, "di": di})
        if plane.rounds_after("40C3B7", pass_number, trips):
            alldok432 = plane.store("40C3B7", alldok432)
        alldok243 = alldok243 + plane.term("40C44C", lambda: register * _centre(di, k) * _centre(di, k),
                                           {"di": di, "i": pass_number, "allvdokso@register": register})
    alldok243 = plane.store("40C44C", alldok243)

    alldok43 = plane.store("40C46E", plane.term(
        "40C46E", lambda: _div(rt["dokBase41"] + rt["dokSur41"], rt["dokBase31"] + rt["dokSur31"]),
        {"dok_base41": rt["dokBase41"], "dok_sur41": rt["dokSur41"], "dok_base31": rt["dokBase31"], "dok_sur31": rt["dokSur31"]}))
    alldoksd = plane.store("40C493", plane.term(
        "40C493", lambda: alldok243 - _square(alldok432), {"alldok243@register": alldok243, "alldok432": alldok432}))
    epsx1 = plane.store("40C4B3", plane.term(
        "40C4B3", lambda: abs(_div(dokm - _div(rt["dokBase41"], rt["dokBase31"]), dokm)),
        {"dokm": dokm, "dok_base41": rt["dokBase41"], "dok_base31": rt["dokBase31"]}))
    epsx2 = plane.store("40C4DF", plane.term(
        "40C4DF", lambda: abs(_div(dokm - _div(rt["dokSur41"], rt["dokSur31"]), dokm)),
        {"dokm": dokm, "dok_sur41": rt["dokSur41"], "dok_sur31": rt["dokSur31"]}))
    register = plane.term("40C505", lambda: abs(_div(dokm - alldok43, dokm)), {"dokm": dokm, "alldok43": alldok43})
    epsx3 = plane.store("40C505", register)
    # 817 compares the reloaded home.
    compared, limit = plane.term("40C511", lambda: (epsx3, eps_dok), {"epsx3": epsx3, "epsx3@register": register, "eps_dok": eps_dok})
    oxidizer_warning = compared > limit
    return {
        "dok43b": dok43b, "dok43s": dok43s, "epsalldok": epsalldok, "allvdokso": allvdokso, "alldok432": alldok432,
        "alldok243": alldok243, "fmdok": fmdok, "alldok43": alldok43, "alldoksd": alldoksd,
        "epsx1": epsx1, "epsx2": epsx2, "epsx3": epsx3, "oxidizerAccuracyWarning": oxidizer_warning,
        "epsx3Register": register,
    }


def pockets(plane: "CyclePlane", case: Dict[str, object]) -> Dict[str, object]:
    """Lines 963-996 (718 for QKS1, which `Pockets` recomputes from QKS)."""
    di = case["setup"]["cellSize"]
    eps_dok = case["inputs"]["epsDok"]
    rt = case["realTotals"]
    qks = case["integerTotals"]["qks"]
    nkarm = len(qks)

    qkss = sum(qks)
    qks1 = [plane.input(_div(float(q), float(qkss))) for q in qks]
    md4 = md3 = dd4 = dd3 = 0.0
    for k in range(nkarm):
        md4 = md4 + plane.term("40E23D", lambda: _centre(di, k) ** 4.0 * qks1[k], {"i": k + 1, "di": di, "qks1": qks1[k]})
        md3 = md3 + plane.term("40E257", lambda: _centre(di, k) ** 3.0 * qks1[k], {"i": k + 1, "di": di, "qks1": qks1[k]})
    for k in range(nkarm):
        dd4 = dd4 + plane.term("40E46A", lambda: _square(_centre(di, k) ** 4.0 - md4) * qks1[k],
                               {"i": k + 1, "di": di, "md4": md4, "qks1": qks1[k]})
        dd3 = dd3 + plane.term("40E48C", lambda: _square(_centre(di, k) ** 3.0 - md3) * qks1[k],
                               {"i": k + 1, "di": di, "md3": md3, "qks1": qks1[k]})
    inverse = plane.store("40E52A", plane.term("40E52A", lambda: _div(1.0, qkss), {"i": qkss}))
    epsy_register = plane.term(
        "40E559", lambda: 3.0 * _sqrt(inverse * (_div(dd4, _square(md4)) + _div(dd3, _square(md3)))),
        {"qkss_inverse": inverse, "dd3": dd3, "md3": md3, "dd4": dd4, "md4": md4})
    epsy = plane.store("40E559", epsy_register)
    compared, limit = plane.term("40E571", lambda: (epsy, eps_dok), {"epsy": epsy, "epsy@register": epsy_register, "eps_dok": eps_dok})
    pocket_warning = compared > limit
    epsmd4 = plane.store("40E627", plane.term("40E627", lambda: _div(3.0, md4) * _sqrt(inverse * dd4),
                                              {"md4": md4, "dd4": dd4, "qkss_inverse": inverse}))
    epsmd3 = plane.store("40E66D", plane.term("40E66D", lambda: _div(3.0, md3) * _sqrt(inverse * dd3),
                                              {"md3": md3, "dd3": dd3, "qkss_inverse": inverse}))

    vks = rt["vks"]
    vvks = plane.store("40E6D1", _sequential_sum(vks))
    vkso = []
    d432 = 0.0
    d243 = 0.0
    dqkarm = 0.0
    for k in range(nkarm):
        pass_number = k + 1
        spilled = plane.store("40E744", plane.term("40E744", lambda: _div(vks[k], vvks), {"vks": vks[k], "vvks@register": vvks}))
        vkso.append(plane.store("40E758", plane.term("40E758", lambda: spilled, {"t454": spilled})))
        d432 = d432 + plane.term("40E925", lambda: vkso[k] * _centre(di, k), {"di": di, "vkso@register": vkso[k], "i": pass_number})
        dqkarm = dqkarm + plane.term("40E94B", lambda: qks1[k] * _centre(di, k), {"di": di, "qks1": qks1[k], "i": pass_number})
        if plane.rounds_after("40E925", pass_number, nkarm):
            d432 = plane.store("40E925", d432)
            dqkarm = plane.store("40E94B", dqkarm)
        d243 = d243 + plane.term("40E9CB", lambda: vkso[k] * _centre(di, k) * _centre(di, k),
                                 {"di": di, "i": pass_number, "vkso@register": vkso[k]})
    d243 = plane.store("40E9CB", d243)
    dp43 = plane.store("40E9D7", plane.term("40E9D7", lambda: _div(rt["dp41"], rt["dp31"]), {"dp41": rt["dp41"], "dp31": rt["dp31"]}))
    sdevp43 = plane.store("40EA0A", plane.term("40EA0A", lambda: _pow(d243 - _square(d432), 0.5), {"d243@register": d243, "d432": d432}))
    return {
        "qkss": qkss, "qks1": qks1, "epsy": epsy, "epsyRegister": epsy_register, "pocketAccuracyWarning": pocket_warning, "epsmd4": epsmd4,
        "epsmd3": epsmd3, "vkso": vkso, "d432": d432, "dqkarm": dqkarm, "dp43": dp43, "sdevp43": sdevp43,
    }


def _matrix_temporaries(plane: "CyclePlane", ggg: float, gdokleft: float, plot1: float, plot2: float) -> Dict[str, float]:
    """The compiler's temporaries of 1006-1011: T1 and T2 once before the cycle loop, G from line 378,
    S1 and S2 spilled when gdokleft is known; the same values wherever a line reads them."""
    t1 = plane.store("40950F", plane.term("40950F", lambda: _div(1.0, plot2), {"plot2": plot2}))
    t2 = plane.store("409521", plane.term("409521", lambda: 1.0 - ggg, {"ggg": ggg}))
    g378 = plane.store("408ABE", plane.term("408ABE", lambda: _div(ggg, plot1), {"ggg": ggg, "plot1": plot1}))
    s1 = plane.store("40EA8D", plane.term("40EA8D", lambda: ggg - gdokleft, {"ggg": ggg, "gdokleft": gdokleft}))
    s2 = plane.store("40EAC9", plane.term("40EAC9", lambda: 1.0 - ggg + gdokleft, {"t2": t2, "gdokleft": gdokleft}))
    return {"t1": t1, "t2": t2, "g378": g378, "s1": s1, "s2": s2}


def matrix(plane: "CyclePlane", case: Dict[str, object], fmdok: Sequence[float]) -> Dict[str, float]:
    """Lines 1001-1016."""
    di = case["setup"]["cellSize"]
    ggg = case["echoes"]["ggg"]
    plot1 = case["inputs"]["oxidizerDensity"]
    plot2 = case["inputs"]["propellantDensity"]
    gm = case["inputs"]["metalMassFraction"]
    eta = case["inputs"]["aggregatedOxideFraction"]

    first = plane.store("40EA32", plane.term("40EA32", lambda: fmdok[math.trunc(_div(case["setup"]["dmin"], di))] * ggg,
                                              {"fmdok": fmdok[math.trunc(_div(case["setup"]["dmin"], di))], "ggg": ggg}))
    gdoksfr = 0.0
    for k in range(len(case["tables"]["massShare"])):
        if case["tables"]["pocketForming"][k] == 0:
            gdoksfr = gdoksfr + case["tables"]["massShare"][k] * ggg
    gdoksfr = plane.store("40EA62", gdoksfr)
    gdokleft = plane.store("40EA6F", first + gdoksfr)

    temporaries = _matrix_temporaries(plane, ggg, gdokleft, plot1, plot2)
    t1, t2, g378, s1, s2 = (temporaries[name] for name in ("t1", "t2", "g378", "s1", "s2"))
    plotsmdok = plane.store("40EAB7", plane.term(
        "40EAB7", lambda: _div(1.0 - s1, t1 - _div(s1, plot1)), {"s1": s1, "t1": t1, "plot1": plot1}))
    vdokleft = plane.store("40EAE7", plane.term(
        "40EAE7", lambda: _div(_div(gdokleft, s2) * plotsmdok, plot1), {"gdokleft": gdokleft, "s2": s2, "plotsmdok": plotsmdok, "plot1": plot1}))
    plotsm = plane.store("409539", plane.term("409539", lambda: _div(t2, t1 - g378), {"t2": t2, "t1": t1, "g378": g378}))

    fold_a = 3.0 * 0.016
    fold_b = 2.0 * 0.027
    t3 = plane.store("40956D", plane.term(
        "40956D", lambda: 1.0 + _div(fold_a * eta, fold_b + fold_a * (1.0 - eta)), {"eta": eta}))
    t4 = plane.store("409595", plane.term("409595", lambda: _div(1.0 - eta, 2000.0) + _div(eta, 3000.0), {"eta": eta}))
    mp = plane.store("40EAFF", plane.term(
        "40EAFF", lambda: _div(3.14159 / 6.0 * plotsmdok * gm, s2), {"plotsmdok": plotsmdok, "gm": gm, "s2": s2}))
    mp = plane.store("40EB05", plane.term("40EB05", lambda: mp * t3, {"mp@register": mp, "t3": t3}))
    mp = plane.store("40EB4A", plane.term(
        "40EB4A", lambda: 2.0 * _pow(0.75 / 3.14159 * mp * t4, 0.3333), {"mp@register": mp, "t4": t4}))
    return {"gdokleft": gdokleft, "plotsmdok": plotsmdok, "vdokleft": vdokleft, "plotsm": plotsm, "mp": mp}


def corrected_pockets(plane: "CyclePlane", case: Dict[str, object]) -> Dict[str, object]:
    """Lines 1087-1112 (cycles >= 1). The `*Normalized` and `*Nmax` entries are the
    report's print-time arithmetic, plain double, shown here for the same totals."""
    di = case["setup"]["cellSize"]
    rt = case["realTotals"]
    it = case["integerTotals"]

    def moment_pair(values, first_at, second_at, first_merge, second_merge):
        total = _sequential_sum(values)
        first = 0.0
        second = 0.0
        for k in range(len(values)):
            first = plane.store(first_at, first + plane.term(
                first_at, lambda: _div(values[k], total) * _centre(di, k), {"fmkarm_cor" if first_at == "40F891" else "fmkarm2": values[k], first_merge: total, "di": di, "i": k + 1}))
            second = second + plane.term(
                second_at, lambda: _div(values[k], total) * _centre(di, k) * _centre(di, k),
                {"fmkarm_cor" if second_at == "40FA08" else "fmkarm2": values[k], second_merge: total, "di": di, "i": k + 1})
        return first, plane.store(second_at, second)

    dkarm43_cor, dkarm243_cor = moment_pair(rt["fmkarmCor"], "40F891", "40FA08", "merge@40F85C", "merge@40F8E9")
    fqkarm_cor = it["fqkarmCor"]
    fq_total = sum(fqkarm_cor)
    dqkarm_cor = 0.0
    for k in range(len(fqkarm_cor)):
        dqkarm_cor = plane.store("40F9FC", dqkarm_cor + plane.term(
            "40F9FC", lambda: _div(float(fqkarm_cor[k]), float(fq_total)) * _centre(di, k),
            {"fqkarm_cor": fqkarm_cor[k], "i": (fq_total, k + 1), "di": di}))
    sdevp43_cor = plane.store("40FA29", plane.term(
        "40FA29", lambda: _pow(dkarm243_cor - _square(dkarm43_cor), 0.5), {"dkarm243_cor@register": dkarm243_cor, "dkarm43_cor": dkarm43_cor}))
    dfmk432, dfmk243 = moment_pair(rt["fmkarm2"], "40FB1A", "40FBB2", "merge@40FAE9", "merge@40FB72")
    sdevp243 = plane.store("40FBD3", plane.term(
        "40FBD3", lambda: _pow(dfmk243 - _square(dfmk432), 0.5), {"dfmk243@register": dfmk243, "dfmk432": dfmk432}))

    def mkm_size(counts, at, literal_value, name):
        total = sum(counts)
        accumulated = 0.0
        for k in range(len(counts)):
            accumulated = plane.store(at, accumulated + plane.term(
                at, lambda: _div(float(counts[k]), float(total)) * literal_value * (k + 0.5),
                {name: counts[k], "i": (total, k + 1)}))
        return accumulated

    def largest_non_empty(counts):
        return max((k + 1 for k, c in enumerate(counts) if c != 0), default=0)

    return {
        "dkarm43Cor": dkarm43_cor, "sdevp43Cor": sdevp43_cor, "dqkarmCor": dqkarm_cor, "dfmk432": dfmk432,
        "sdevp243": sdevp243,
        "dqmkm1": mkm_size(it["qmkm1"], "40FD31", 0.001, "qmkm1"),
        "dqmkm2": mkm_size(it["qmkm2"], "40FDF9", 0.01, "qmkm2"),
        "qmcoef": mkm_size(it["coef"], "40FF07", 0.01, "coef"),
        "fmkarmCorNormalized": [_div(v, _sequential_sum(rt["fmkarmCor"])) for v in rt["fmkarmCor"]],
        "fmkarm2Normalized": [_div(v, _sequential_sum(rt["fmkarm2"])) for v in rt["fmkarm2"]],
        "fqkarmCorNormalized": [_div(float(v), float(fq_total)) for v in fqkarm_cor],
        "qmkm1Normalized": [_div(float(v), float(sum(it["qmkm1"]))) for v in it["qmkm1"]],
        "qmkm2Normalized": [_div(float(v), float(sum(it["qmkm2"]))) for v in it["qmkm2"]],
        "coefNormalized": [_div(float(v), float(sum(it["coef"]))) for v in it["coef"]],
        "coefNmax": largest_non_empty(it["coef"]), "qmkm1Nmax": largest_non_empty(it["qmkm1"]),
        "qmkm2Nmax": largest_non_empty(it["qmkm2"]),
    }


def mass_fractions(plane: "CyclePlane", case: Dict[str, object], matrix_values: Dict[str, float]) -> Dict[str, object]:
    """Lines 1137-1151 (cycles >= 1): DolM1, and VdokTotal and VdokTotal2 rewritten in place."""
    ggg = case["echoes"]["ggg"]
    plot1 = case["inputs"]["oxidizerDensity"]
    plot2 = case["inputs"]["propellantDensity"]
    rt = case["realTotals"]
    gdokleft = matrix_values["gdokleft"]
    plotsmdok = matrix_values["plotsmdok"]
    vdokleft = matrix_values["vdokleft"]
    plotsm = matrix_values["plotsm"]
    temporaries = _matrix_temporaries(plane, ggg, gdokleft, plot1, plot2)
    t2, s1, s2 = temporaries["t2"], temporaries["s1"], temporaries["s2"]

    dolm1 = plane.store("410222", plane.term(
        "410222", lambda: _div(_sequential_sum(rt["vsmkm"]) * plotsmdok * (ggg - gdokleft), _sequential_sum(rt["svd"]) * plot1 * (1.0 - ggg + gdokleft)),
        {"merge@410178": _sequential_sum(rt["vsmkm"]), "plotsmdok": plotsmdok, "s1": s1, "merge@4101EE": _sequential_sum(rt["svd"]),
         "plot1": plot1, "s2": s2}))
    divisor = plane.store("41023A", plane.term("41023A", lambda: 1.0 - _div(gdokleft, ggg), {"gdokleft": gdokleft, "ggg": ggg}))
    vdok_total = [plane.store("410261", plane.term("410261", lambda: _div(v, divisor), {"vdok_total": v, "d1145": divisor}))
                  for v in rt["vdokTotal"]]
    dolm2 = plane.store("4103A2", plane.term(
        "4103A2", lambda: _div(_sequential_sum(rt["vmkmTotal"]) * (1.0 - vdokleft),
                               _div(_div(_sequential_sum(vdok_total) * plot1 * (1.0 - ggg), ggg), plotsm)),
        {"merge@410310": _sequential_sum(rt["vmkmTotal"]), "vdokleft": vdokleft, "t2": t2,
         "merge@410384": _sequential_sum(vdok_total), "plot1": plot1, "ggg": ggg, "plotsm": plotsm}))
    vdok_total2 = plane.store("4103C2", plane.term("4103C2", lambda: _div(rt["vdokTotal2"], divisor),
                                                   {"vdok_total2": rt["vdokTotal2"], "d1145": divisor}))
    dolm3 = plane.store("410400", plane.term(
        "410400", lambda: _div(rt["vmkmTotal2"] * (1.0 - vdokleft), _div(_div(vdok_total2 * plot1 * (1.0 - ggg), ggg), plotsm)),
        {"vdokleft": vdokleft, "vmkm_total2": rt["vmkmTotal2"], "t2": t2, "vdok_total2": vdok_total2, "plot1": plot1,
         "ggg": ggg, "plotsm": plotsm}))
    return {"dolM1": dolm1, "dolM2": dolm2, "dolM3": dolm3, "vdokTotalAfter": vdok_total, "vdokTotal2After": vdok_total2}


def convergence(plane: "CyclePlane", case: Dict[str, object], sizes: Dict[str, object], pocket_values: Dict[str, object],
                dolm2: float) -> Dict[str, float]:
    """Lines 1168-1174; the first three are copies (1169-1171), the last three stores."""
    dokm = case["echoes"]["dokm"]
    doksd = case["echoes"]["doksd"]
    return {
        "convergenceEpsy": pocket_values["epsy"],
        "convergenceEpsmd3": pocket_values["epsmd3"],
        "convergenceEpsmd4": pocket_values["epsmd4"],
        "convergenceAlldok43": plane.store("4104E6", plane.term(
            "4104E6", lambda: _div(sizes["alldok43"] - dokm, dokm), {"alldok43": sizes["alldok43"], "dokm": dokm})),
        "convergenceAlldoksd": plane.store("41050F", plane.term(
            "41050F", lambda: _div(sizes["alldoksd"] - doksd, doksd), {"alldoksd": sizes["alldoksd"], "doksd": doksd})),
        "convergenceDolM2": plane.store("410526", plane.term("410526", lambda: 1.0 - dolm2, {"dolm2": dolm2})),
    }


CYCLE_ONLY_SCALARS = (
    "dkarm43Cor", "sdevp43Cor", "dqkarmCor", "dfmk432", "sdevp243", "dqmkm1", "dqmkm2", "qmcoef", "dolM1", "dolM2", "dolM3",
    "convergenceEpsy", "convergenceEpsmd3", "convergenceEpsmd4", "convergenceAlldok43", "convergenceAlldoksd", "convergenceDolM2",
)


def cycle_report(case: Dict[str, object], plane: "CyclePlane", registers: Dict[str, float] = None) -> Dict[str, object]:
    """Everything `CycleStatistics.Compute` returns for the case's constructed totals, in
    the order of Fortran 771-1175, under `plane`; cycle 0 stops after `Dmaxxx` (1078-1082),
    where the quantities of the later lines stay NaN. The unrounded values of EPSX3 and
    EPSY, which the report does not carry, are left in `registers` when it is given."""
    setup = case["setup"]
    echoes = case["echoes"]
    rt = case["realTotals"]
    it = case["integerTotals"]
    di = setup["cellSize"]
    ggg = echoes["ggg"]

    report: Dict[str, object] = {}
    report.update(generator_accuracy(plane, rt["xss"], it["nfx"], it["nfy"], it["nfz"], it["nfq"], it["nfw"]))
    sizes = oxidizer_sizes(plane, case)
    categories = categories_merge_and_describe(
        setup["ndok"], setup["ncat"], di, setup["categoryStep"], case["inputs"]["epsDok"], rt["dpMax"],
        it["qdoks"], rt["dokp41"], rt["dokp31"], plane, registers.setdefault("categories", {}) if registers is not None else None)
    pocket_values = pockets(plane, case)
    matrix_values = matrix(plane, case, sizes["fmdok"])
    pdoksmall = small_particles_probability(
        setup["ndok"], di, setup["ak2"], ggg, matrix_values["gdokleft"], setup["pocketCoefficient"],
        case["inputs"]["oxidizerDensity"], case["inputs"]["propellantDensity"], rt["vdokstr"], plane)
    dmaxxx = small_particles_max_size(
        setup["ndok"], di, echoes["ddokmax"], setup["pocketCoefficient"], setup["bridgeCoefficient"], pdoksmall, plane)

    hidden = ("alldok243", "fmdok", "alldoksd", "epsx3Register", "epsyRegister")
    if registers is not None:
        registers["epsx3"] = sizes["epsx3Register"]
        registers["epsy"] = pocket_values["epsyRegister"]
    report.update({key: value for key, value in sizes.items() if key not in hidden})
    report.update({key: value for key, value in pocket_values.items() if key not in hidden})
    report.update(matrix_values)
    report.update(categories)
    report["pdoksmall"] = pdoksmall[:-1]
    report["nextPdoksmall"] = pdoksmall
    report["nextDmaxxx"] = dmaxxx
    report["vdokTotalAfter"] = rt["vdokTotal"]
    report["vdokTotal2After"] = rt["vdokTotal2"]

    if case["cycleIndex"] == 0:
        for name in CYCLE_ONLY_SCALARS:
            report[name] = _NAN
        return report

    report.update(corrected_pockets(plane, case))
    mass = mass_fractions(plane, case, matrix_values)
    report.update(mass)
    report.update(convergence(plane, case, sizes, pocket_values, mass["dolM2"]))
    return report

# ---------------------------------------------------------------------------
# Cycle cases (cases/statistics/cycle_statistics.json). Every input is constructed;
# every expected value, under both kinds, is `cycle_report`'s own, never typed. Real
# totals Particle stores in REAL*4 and every setup-plane scalar are made binary32 so that
# the same constructed input is what a run under either kind would hand `Compute`.
# ---------------------------------------------------------------------------

SEEDED_ROUNDING_SEED = 1
EPSX3_FLAG_SEED = 3
EPSY_FLAG_SEED = 6

REPORT_MEMBERS = {
    "GeneratorAccuracy": ["eps1", "eps2", "eps3", "eps4", "eps5", "eps6", "eps7"],
    "OxidizerSizes": ["dok43b", "dok43s", "epsalldok", "allvdokso", "alldok432", "alldok43", "epsx1", "epsx2", "epsx3"],
    "Pockets": ["qks1", "epsy", "epsmd4", "epsmd3", "vkso", "d432", "dqkarm", "dp43", "sdevp43"],
    "Matrix": ["gdokleft", "plotsmdok", "vdokleft", "plotsm", "mp"],
    "CorrectedPockets": ["dkarm43Cor", "sdevp43Cor", "dqkarmCor", "dfmk432", "sdevp243", "dqmkm1", "dqmkm2", "qmcoef"],
    "MassFractions": ["dolM1", "dolM2", "dolM3", "vdokTotalAfter", "vdokTotal2After"],
    "Convergence": ["convergenceAlldok43", "convergenceAlldoksd", "convergenceDolM2"],
}


def _flatten(value) -> List[float]:
    if isinstance(value, (list, tuple)):
        return [leaf for item in value for leaf in _flatten(item)]
    return [float(value)]


def _bits_differ(left, right) -> bool:
    pairs = zip(_flatten(left), _flatten(right))
    return any(struct.pack("<d", a) != struct.pack("<d", b) and not (a != a and b != b) for a, b in pairs)


def _members_that_differ(case: Dict[str, object]) -> List[str]:
    return [member for member, keys in REPORT_MEMBERS.items()
            if any(_bits_differ(case["expected"][key], case["expectedOriginal"][key]) for key in keys)]


def _cycle_case(name: str, body: Dict[str, object]) -> Dict[str, object]:
    case = {"name": name, **body}
    case["expected"] = cycle_report(case, BINARY64)
    case["expectedOriginal"] = cycle_report(case, CyclePlane(original=True))
    return case


def _small_body(cycle_index: int) -> Dict[str, object]:
    """Totals small enough to follow by hand: Di = Dj = 1, so the category and pocket
    cells are whole units. Cycle 0 draws no streams 6 and 7 (NFQ = NFW = 0), so EPS6 and
    EPS7 are the 0/0 NaN of the original; cycle 1 draws them. `dmin = 2` reads FMDOK
    at the third cell, so gdokleft is nonzero and the in-place rewrite of VdokTotal and
    VdokTotal2 (1145, 1150) is visible. Fmkarm2 is empty: Dfmk432 and Sdevp243 are 0/0."""
    drawn = cycle_index > 0
    return {
        "cycleIndex": cycle_index,
        "setup": {"fractionCount": 2, "ndok": 4, "nkarm": 3, "ncat": 3, "nc": 4, "cellSize": 1.0, "categoryStep": 1.0,
                  "dmin": 2.0, "ak2": 2.0, "pocketCoefficient": 8.25, "bridgeCoefficient": 40.0},
        "tables": {"share": [0.25, 0.75], "pocketForming": [1, 0], "massShare": [_to_binary32(0.3), _to_binary32(0.45)]},
        "echoes": {"dokm": 2.5, "doksd": 0.5, "ddokmax": 4.0, "ggg": 0.75},
        "inputs": {"oxidizerDensity": 2.0, "propellantDensity": 1.5, "oxidizerMassFraction": 0.8, "metalMassFraction": 0.125,
                   "homogenizedOxidizerFraction": 0.0625, "epsDok": 0.0625, "aggregatedOxideFraction": 0.25},
        "integerTotals": {
            "nfx": 8, "nfy": 12, "nfz": 20, "nfq": 6 if drawn else 0, "nfw": 4 if drawn else 0,
            "alldokFract": [3, 7], "alldok": [1, 2, 3, 4], "qks": [3, 5, 2], "fqkarmCor": [3, 1, 0],
            "coef": [2, 2, 0, 0], "qmkm1": [1, 0, 2, 0], "qmkm2": [0, 0, 0, 3],
            "qdoks": [[1, 0, 2, 0], [0, 3, 1, 1], [0, 0, 0, 0]],
        },
        "realTotals": {
            "xss": [4.1, 3.9, 6.2, 9.7, 10.4, 3.1 if drawn else 0.0, 1.9 if drawn else 0.0],
            "sd4": 10.0, "sd3": 4.0, "d41": 7.0, "d31": 3.0,
            "dokBase41": 6.0, "dokBase31": 2.5, "dokSur41": 5.5, "dokSur31": 2.0,
            "dp41": 3.0, "dp31": 1.5, "dpMax": 1.5, "dpMaxCor": 2.5,
            "allvdok": [1.5, 2.25, 3.5, 0.75], "vdokstr": [2.0, 3.0, 1.0, 4.0], "vks": [1.0, 2.5, 0.5],
            "fmkarmCor": [3.0, 0.0, 1.0], "fmkarm2": [0.0, 0.0, 0.0],
            "dokp41": [5.0, 7.0, 0.0], "dokp31": [1.0, 2.0, 0.0],
            "vsmkm": [1.0, 1.5, 2.0, 0.5], "svd": [2.0, 2.0, 2.0, 1.0], "vmkmTotal": [2.0, 1.0, 3.0, 1.0],
            "vdokTotal": [3.0, 3.0, 1.5, 2.0], "vmkmTotal2": 8.0, "vdokTotal2": 12.0,
        },
    }


def _seeded_body(seed: int, eps_dok: float = 0.3, cycle_index: int = 1, ndok: int = 6, nkarm: int = 5, ggg: float = 0.72, cell: float = 1e-5,
                 plot1: float = 1950.0, plot2: float = 1750.0, pocket_forming: Sequence[int] = (0, 1, 0)) -> Dict[str, object]:
    """Realistic-scale totals (Di = Dj = 10 um) drawn from `random.Random(seed).random()`,
    the one Mersenne Twister call whose stream Python fixes across versions. Every
    REAL*4 input is binary32; the REAL*8 ones (xss, sd*, d41, d31, dok_*, dp*, vks) are
    plain doubles. Three fractions, of which the first and third are not pocket forming."""
    rng = random.Random(seed)

    def draw(low: float, high: float) -> float:
        return low + (high - low) * rng.random()

    def real4(low: float, high: float) -> float:
        return _to_binary32(draw(low, high))

    def count(low: int, high: int) -> int:
        return low + int((high - low + 1) * rng.random())

    ncat, nc, nmm = 5, 6, 3
    di = _to_binary32(cell)
    nfx, nfy, nfz, nfq, nfw = (count(400, 900) for _ in range(5))
    streams = [nfx, nfx, nfy, nfz, nfz, nfq, nfw]
    xss = [n * draw(0.47, 0.53) for n in streams]

    def moments(population: int, low: float, high: float):
        size = draw(low, high)
        return population * size ** 4, population * size ** 3

    sd4, sd3 = moments(count(500, 900), 2e-5, 4e-5)
    d41, d31 = moments(count(500, 900), 2e-5, 4e-5)
    dok_base41, dok_base31 = moments(count(300, 600), 2e-5, 4e-5)
    dok_sur41, dok_sur31 = moments(count(300, 600), 2e-5, 4e-5)
    dp41, dp31 = moments(count(200, 400), 3e-5, 5e-5)
    mass_share = [real4(0.2, 0.4) for _ in range(nmm)]
    weight = _sequential_sum(mass_share)
    alldok = [count(10, 60) for _ in range(ndok)]
    fractions = [count(20, 40), count(20, 40)]
    fractions.append(sum(alldok) - sum(fractions))
    rows = 4  # DPRow = trunc(3.7) + 1: three rows of the five stay out of the report.
    return {
        "cycleIndex": cycle_index,
        "setup": {"fractionCount": nmm, "ndok": ndok, "nkarm": nkarm, "ncat": ncat, "nc": nc, "cellSize": di,
                  "categoryStep": di, "dmin": 2.0 * di, "ak2": 2.5, "pocketCoefficient": _to_binary32(8.2),
                  "bridgeCoefficient": 40.0},
        "tables": {"share": [m / weight for m in mass_share], "pocketForming": list(pocket_forming), "massShare": mass_share},
        "echoes": {"dokm": real4(2.8e-5, 3.2e-5), "doksd": real4(0.5e-10, 1.5e-10), "ddokmax": _to_binary32(6e-5),
                   "ggg": _to_binary32(ggg)},
        "inputs": {"oxidizerDensity": plot1, "propellantDensity": plot2, "oxidizerMassFraction": _to_binary32(0.8),
                   "metalMassFraction": _to_binary32(0.18), "homogenizedOxidizerFraction": _to_binary32(0.1),
                   "epsDok": _to_binary32(eps_dok), "aggregatedOxideFraction": _to_binary32(0.2)},
        "integerTotals": {
            "nfx": nfx, "nfy": nfy, "nfz": nfz, "nfq": nfq, "nfw": nfw,
            "alldokFract": fractions, "alldok": alldok,
            "qks": [count(1, 100) for _ in range(nkarm)], "fqkarmCor": [count(1, 50) for _ in range(nkarm)],
            "coef": [count(0, 30) for _ in range(nc)], "qmkm1": [count(0, 30) for _ in range(nc)],
            "qmkm2": [count(0, 30) for _ in range(nc)],
            "qdoks": [[count(0, 40) if r < rows else 0 for _ in range(ndok)] for r in range(ncat)],
        },
        "realTotals": {
            "xss": xss, "sd4": sd4, "sd3": sd3, "d41": d41, "d31": d31,
            "dokBase41": dok_base41, "dokBase31": dok_base31, "dokSur41": dok_sur41, "dokSur31": dok_sur31,
            "dp41": dp41, "dp31": dp31, "dpMax": _to_binary32(3.7e-5), "dpMaxCor": _to_binary32(3.2e-5),
            "allvdok": [real4(1.0, 10.0) for _ in range(ndok)], "vdokstr": [real4(1.0, 10.0) for _ in range(ndok)],
            "vks": [draw(1.0, 10.0) for _ in range(nkarm)],
            "fmkarmCor": [real4(1.0, 10.0) for _ in range(nkarm)], "fmkarm2": [real4(1.0, 10.0) for _ in range(nkarm)],
            "dokp41": [real4(5e-18, 9e-18) if r < rows else 0.0 for r in range(ncat)],
            "dokp31": [real4(1.5e-13, 3e-13) if r < rows else 0.0 for r in range(ncat)],
            "vsmkm": [real4(1.0, 10.0) for _ in range(ndok)], "svd": [real4(1.0, 10.0) for _ in range(ndok)],
            "vmkmTotal": [real4(1.0, 10.0) for _ in range(ndok)], "vdokTotal": [real4(1.0, 10.0) for _ in range(ndok)],
            "vmkmTotal2": real4(5.0, 50.0), "vdokTotal2": real4(5.0, 50.0),
        },
    }


def _warning_flag_case(name: str, seed: int, quantity: str, flag: str, original_flag: bool) -> Dict[str, object]:
    """`EpsDok` set to the binary32 neighbour just below the unrounded `quantity` the
    original's own register holds for the seeded totals, so its binary32 store equals
    `EpsDok` while the register exceeds it by less than half a unit in the last place. Under
    `Original` both flags read the reloaded home, the rounded store (817 at 0x40C50B, 976 at
    0x40E56B), so both clear; a flag that read the register would stay set (the H control)."""
    registers: Dict[str, float] = {}
    cycle_report(_seeded_body(seed), CyclePlane(original=True), registers)
    unrounded = registers[quantity]
    eps_dok = _to_binary32(unrounded)
    if not eps_dok < unrounded:
        raise SystemExit(f"seed {seed}: binary32({quantity}) is not below it; search another seed")
    case = _cycle_case(name, _seeded_body(seed, eps_dok=eps_dok))
    if case["expectedOriginal"][flag] is not original_flag:
        raise SystemExit(f"seed {seed}: {flag} is not {original_flag} under Original")
    return case


# ---------------------------------------------------------------------------
# The positive controls of the site table (AGENTS.md section 13; root BOOT.md, "Taboos"). Each is
# a constructed body that reaches a stored bit of one site: with the site's flip -- the store the
# other way, or the other pass decision of its schedule -- the twin's own Original report
# differs, so a Compute that did what the table says of the site and one that did the opposite
# are told apart by a bit. `generate` refuses a control whose flip moves nothing, so a control
# cannot rot into a non-control; `body` names the seed and the keyword arguments of
# `_seeded_body`, and `epsFromRow0` sets EpsDok to the first pass's stored Epsydok of row 0, the
# value at which the category merge's decision reads the store. The sites of the table that
# the executable's oracle fixture leaves `unreached` are all here but those `EXACT_SITES` names.
# ---------------------------------------------------------------------------

CONTROLS = [
    ("40C505", [("40C505", "store")], dict(seed=1)),
    ("40EA32", [("40EA32", "store")], dict(seed=1, ggg=0.61)),
    ("40EA62", [("40EA62", "store")], dict(seed=2, ndok=12, nkarm=7)),
    ("40EAE7", [("40EAE7", "store")], dict(seed=1)),
    ("40ECDF", [("40ECDF", "store")], dict(seed=1, ndok=6, nkarm=6)),
    ("40F4A5", [("40F4A5", "store")], dict(seed=1)),
    ("40FA08", [("40FA08", "store")], dict(seed=1)),
    ("40FBB2", [("40FBB2", "store")], dict(seed=1)),
    ("40FD31", [("40FD31", "store")], dict(seed=1)),
    ("410261", [("410261", "store")], dict(seed=1)),
    ("4103C2", [("4103C2", "store")], dict(seed=1)),
    ("40950F", [("40950F", "store")], dict(seed=1, pocket_forming=(1, 1, 1))),
    ("409521", [("409521", "store")], dict(seed=1, ggg=0.42, cell=7.7e-06)),
    ("408ABE", [("408ABE", "store")], dict(seed=1, ggg=0.61)),
    ("40EA8D", [("40EA8D", "store")], dict(seed=1, ggg=0.61, pocket_forming=(1, 1, 1))),
    ("40D45C", [("40D45C", "store")], dict(seed=11, epsFromRow0=True)),
    ("40CE8F", [("40CE8F", "store")], dict(seed=4, ndok=13, nkarm=13, epsFromRow0=True)),
    ("40D48F", [("40D48F", "store")], dict(seed=1, epsFromRow0=True)),
    ("40956D", [("40956D", "store")], dict(seed=1)),
    ("409595", [("409595", "store")], dict(seed=1)),
    ("40E52A", [("40E52A", "store")], dict(seed=1)),
    ("40E744", [("40E744", "store"), ("40E758", "store")], dict(seed=1)),
    ("40C61E", [("40C61E", "store")], dict(seed=1, ggg=0.37, cell=1.3e-05)),
    ("40EDFF", [("40EDFF", "store")], dict(seed=1)),
    ("40EFC2", [("40EFC2", "store")], dict(seed=1, ndok=6, nkarm=6)),
    ("40EEBC", [("40EEBC", "store")], dict(seed=1, ndok=6, nkarm=6)),
    ("40F077", [("40F077", "store")], dict(seed=1, ndok=5, nkarm=5)),
    ("40C3B7", [("40C3B7", "schedule")], dict(seed=1)),
    ("40E925", [("40E925", "schedule")], dict(seed=1)),
    ("40C22C", [("40C22C", "schedule")], dict(seed=3, ndok=6, nkarm=6)),
    ("40C3B7", [("40C3B7", "schedule")], dict(seed=1, ndok=5, nkarm=5)),
    ("40C3B7", [("40C3B7", "schedule")], dict(seed=1, ndok=13, nkarm=13)),
    ("40E925", [("40E925", "schedule")], dict(seed=1, ndok=13, nkarm=13)),
    ("40C22C", [("40C22C", "schedule")], dict(seed=28, ndok=5, nkarm=5)),
    ("40C22C", [("40C22C", "schedule")], dict(seed=24, ndok=13, nkarm=13)),
]

# Sites of the oracle's `unreached` list whose stored value is exact by what the table says of
# them: the zero a register is started from (`order` 0.0) and a count converted to REAL*4
# (`real(i)`, exact below 2**24). Rounding an exact value moves no bit, so no input reaches one;
# `_exact_sites` proves the claim from the table and the generator refuses a site that is
# neither controlled nor exact.
EXACT_ORDERS = ("0.0", "real(i)")


def _exact_sites() -> List[str]:
    sites = CyclePlane(original=True)._sites
    return sorted(at for at, site in sites.items() if site.order in EXACT_ORDERS)


def _control_body(spec: dict) -> Dict[str, object]:
    spec = dict(spec)
    seed = spec.pop("seed")
    if spec.pop("epsFromRow0", False):
        registers: Dict[str, object] = {}
        cycle_report({"cycleIndex": 1, **_seeded_body(seed, **spec)}, CyclePlane(original=True), registers)
        spec["eps_dok"] = registers["categories"]["epsydok"][0]
    return _seeded_body(seed, **spec)


def _control_record(at: str, flips: Sequence[tuple], body: Dict[str, object]) -> Dict[str, object]:
    case = {"name": "control", **body}
    flipped = json.dumps(_json_ready(cycle_report(case, CyclePlane(original=True, flips=frozenset(flips)))), sort_keys=True)
    if json.dumps(_json_ready(cycle_report(case, CyclePlane(original=True))), sort_keys=True) == flipped:
        raise SystemExit(f"control {at}: {flips} moves no bit of the Original report; search another body")
    site = CyclePlane(original=True).site(at)
    return {"at": at, "kind": site.kind, "target": site.target, "lines": site.lines,
            "flips": [{"at": a, "aspect": aspect} for a, aspect in flips]}


def control_cases() -> List[Dict[str, object]]:
    """One case per distinct body, carrying every control that body proves."""
    groups: Dict[str, Dict[str, object]] = {}
    for at, flips, spec in CONTROLS:
        key = json.dumps(spec, sort_keys=True)
        if key not in groups:
            groups[key] = {"spec": spec, "controls": []}
        body = _control_body(spec)
        groups[key]["body"] = body
        groups[key]["controls"].append(_control_record(at, flips, body))
    cases = []
    for index, group in enumerate(groups.values()):
        name = f"control_{index:02d}_" + "_".join(f"{control['at'].lower()}" for control in group["controls"][:2])
        if len(group["controls"]) > 2:
            name += f"_and_{len(group['controls']) - 2}_more"
        case = _cycle_case(name, group["body"])
        case["controls"] = group["controls"]
        cases.append(case)
    return cases


def _check_unreached_sites_are_controlled(cases: Sequence[Dict[str, object]]) -> None:
    oracle = json.loads((CASES_DIR / "cycle_plane_oracle.json").read_text(encoding="utf-8"))
    controlled = {control["at"] for case in cases for control in case.get("controls", [])}
    exact = set(_exact_sites())
    unreached = {site["at"][2:] for site in oracle["sites"] if site["status"] == "unreached"}
    loose = sorted(unreached - controlled - exact)
    if loose:
        raise SystemExit(f"oracle sites left unreached with neither a control nor an exact value: {loose}")


def cycle_statistics_cases() -> List[Dict[str, object]]:
    cases = [
        _cycle_case("cycle_zero_gates_the_later_lines", _small_body(0)),
        _cycle_case("cycle_one_rewrites_vdok_totals_in_place", _small_body(1)),
    ]
    seeded = _cycle_case("seeded_rounding_reaches_every_member", _seeded_body(SEEDED_ROUNDING_SEED))
    missing = [m for m in REPORT_MEMBERS if m not in _members_that_differ(seeded)]
    if missing:
        raise SystemExit(f"seed {SEEDED_ROUNDING_SEED}: Original equals Binary64 for {missing}; search another seed")
    cases.append(seeded)
    controls = control_cases()
    _check_unreached_sites_are_controlled(controls)
    cases.extend(controls)
    cases.append(_warning_flag_case("seeded_epsx3_warning_reads_the_rounded_store", EPSX3_FLAG_SEED, "epsx3", "oxidizerAccuracyWarning", False))
    cases.append(_warning_flag_case("seeded_epsy_warning_reads_the_rounded_store", EPSY_FLAG_SEED, "epsy", "pocketAccuracyWarning", False))
    return cases


# ---------------------------------------------------------------------------
# The forms the plane's products take (cases/statistics/plane_forms.json). `CyclePlaneOrder`'s five
# Original forms are each read here off the `order` text of a site that uses them, evaluated over
# generated operands, and `UnrolledSchedule` is each pass decision of the schedule rule over trip
# counts below, at and above its block size. Binary64's forms are what the bit snapshots hold.
# ---------------------------------------------------------------------------

PLANE_FORM_SITES = {"linear": "40E925", "quadratic": "40E9CB", "fourth": "40D07E", "cube": "40D090", "square": "40D360"}
SCHEDULE_UNROLLS = (5, 6, 7, 8, 9)
SCHEDULE_TRIPS = range(1, 3 * 9 + 2)

# The weight of a linear or quadratic form is a quotient at every site that uses it (805 and 806
# `ALLVDOKSO`, 989 and 990 `VKSO`, 1089-1093 and 1101-1102 a count over a sum): a double of 53 bits,
# not a binary32 value. A binary32 weight times a binary32 step times a small integer is exact in
# whatever association, so no tuple of binary32 weights tells one association from another and a
# form that associated differently would pass; the weights here are quotients of two binary32
# values, from a stream of their own so that the operands of the other forms do not move.
QUOTIENT_FORMS = ("linear", "quadratic")
QUOTIENT_SEED = 20261004
# Per form, the other ways of forming the same product, each a Python double expression over the
# tuple (x, di, c = (i - 1/2)*di, i - 1/2, m). `square` is an equivalent form: pow(y, 2) is y*y
# when pow is correctly rounded, so no tuple tells them apart and none is asked to.
FORM_ALTERNATIVES = {
    "linear": {"x*(di*(i-1/2))": lambda x, di, c, h, m: x * (di * h), "(x*(i-1/2))*di": lambda x, di, c, h, m: x * h * di},
    "quadratic": {"(x*c)*c": lambda x, di, c, h, m: x * c * c},
    "fourth": {"c*(c*(c*c))": lambda x, di, c, h, m: c * (c * (c * c)), "pow(c, 4)": lambda x, di, c, h, m: math.pow(c, 4)},
    "cube": {"pow(c, 3)": lambda x, di, c, h, m: math.pow(c, 3)},
}
FORM_EQUIVALENT = ("square",)
# A tuple set tells an alternative from the Original form when at least this share of its tuples
# differ in the last bit; the generator refuses a form with an alternative below it.
FAIR_SHARE = 0.125


def _check_form_is_told_apart(form: str, draws: Sequence[Sequence[float]], expected: Sequence[float]) -> None:
    """The positive control of `plane_forms.json`: for every other way of forming the product the
    tuples differ from the Original form in at least `FAIR_SHARE` of the tuples, so a `CyclePlaneOrder`
    that formed it that way fails `PlaneFormsTests`. Refuses a form with an alternative no tuple
    separates (a form not listed as an equivalent one has no excuse)."""
    if form in FORM_EQUIVALENT:
        return
    for name, alternative in FORM_ALTERNATIVES[form].items():
        told = 0
        for (x, di, index, m), original in zip(draws, expected):
            half = index - 0.5
            told += alternative(x, di, half * di, half, m) != original
        if told < FAIR_SHARE * len(draws):
            raise SystemExit(f"plane form {form}: {name} differs from the Original form on {told} of {len(draws)} "
                             f"tuples, below the fair share {FAIR_SHARE}; search other operands")


def plane_forms_cases() -> List[Dict[str, object]]:
    plane = CyclePlane(original=True)
    rng = random.Random(20261003)
    quotients = random.Random(QUOTIENT_SEED)
    cases: List[Dict[str, object]] = []
    for form, at in PLANE_FORM_SITES.items():
        draws, expected = [], []
        for _ in range(24):
            di = _to_binary32(1e-6 + 1e-4 * rng.random())
            index = 1 + int(13 * rng.random())
            x = _to_binary32(0.05 + 20.0 * rng.random())
            m = x * di * 1e3
            if form in QUOTIENT_FORMS:
                x = x / _to_binary32(1.5 + 8.0 * quotients.random())
            draws.append([x, di, float(index), m])
            if form == "linear":
                expected.append(plane.term(at, lambda: 0.0, {"di": di, "vkso@register": x, "i": index}))
            elif form == "quadratic":
                expected.append(plane.term(at, lambda: 0.0, {"di": di, "vkso@register": x, "i": index}))
            elif form in ("fourth", "cube"):
                expected.append(plane.term(at, lambda: 0.0, {"di": di, "qdoks1": 1.0, "i": index}))
            else:
                expected.append(plane.term(at, lambda: 0.0, {"di": di, "mdok4_irow": m, "qdoks1": 1.0, "i": index}))
        _check_form_is_told_apart(form, draws, expected)
        cases.append({"name": f"form_{form}", "form": form, "site": at, "unroll": 0,
                      "operands": draws, "expectedOriginal": expected, "roundsAfter": [], "reloadsBefore": []})
    for unroll in SCHEDULE_UNROLLS:
        cases.append({"name": f"schedule_{unroll}", "form": "schedule", "site": "", "unroll": unroll,
                      "operands": [], "expectedOriginal": [],
                      "roundsAfter": [[schedule_rounds_after(unroll, p, trips) for p in range(1, trips + 1)] for trips in SCHEDULE_TRIPS],
                      "reloadsBefore": [[schedule_reloads_before(unroll, p, trips) for p in range(1, trips + 1)] for trips in SCHEDULE_TRIPS]})
    return cases


FILES = {
    "categories.json": {
        "fortranLines": "825-959",
        "subroutine": "the category merge (Categories.MergeAndDescribe)",
        "cases_fn": categories_cases,
    },
    "small_particles_probability.json": {
        "fortranLines": "1021-1065",
        "subroutine": "the small-particle probability (SmallParticles.Probability)",
        "cases_fn": small_particles_probability_cases,
    },
    "small_particles_max_size.json": {
        "fortranLines": "1066-1074",
        "subroutine": "the largest base size the small-particle probability still reaches (SmallParticles.MaxSize)",
        "cases_fn": small_particles_max_size_cases,
    },
    "cycle_statistics.json": {
        "fortranLines": "771-819, 963-1016, 1087-1113, 1137-1152, 1168-1174",
        "subroutine": "the per-cycle pipeline around the category merge and the small particles (CycleStatistics.Compute)",
        "cases_fn": cycle_statistics_cases,
    },
    "plane_forms.json": {
        "fortranLines": "799-806, 852-865, 988-991, 1089-1102",
        "subroutine": "the forms of the plane's products and the schedule of its unrolled sums (CyclePlaneOrder, UnrolledSchedule)",
        "cases_fn": plane_forms_cases,
    },
    "setup_plane.json": {
        "fortranLines": "267-277, 376-406, 1695-1757",
        "subroutine": "the setup plane under PrecisionKind.Original (Setup.Prepare/FractionLaw/Setup.AnalyticSizes)",
        "cases_fn": setup_plane_cases,
    },
}


def _json_ready(value):
    """JSON has no NaN or infinity: they travel as the strings System.Text.Json reads with
    JsonNumberHandling.AllowNamedFloatingPointLiterals."""
    if isinstance(value, float) and not math.isfinite(value):
        return "NaN" if value != value else ("Infinity" if value > 0 else "-Infinity")
    if isinstance(value, dict):
        return {key: _json_ready(item) for key, item in value.items()}
    if isinstance(value, list):
        return [_json_ready(item) for item in value]
    return value


def _document(spec: dict) -> dict:
    return {
        "script": SCRIPT_NAME,
        "fortranLines": spec["fortranLines"],
        "subroutine": spec["subroutine"],
        "convention": "arrays and cell indices are 0-based, matching this node's own C# signatures "
                       "(src/Statistics/API.md) directly -- unlike formulas_particle.py, there is no "
                       "Fortran-1-based caller convention to preserve here.",
        "cases": _json_ready(spec["cases_fn"]()),
    }


def _write(path: Path, document: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")


def cmd_generate() -> None:
    for filename, spec in FILES.items():
        _write(CASES_DIR / filename, _document(spec))
        print(f"wrote {CASES_DIR / filename}")


def cmd_verify() -> None:
    problems: List[str] = []
    with tempfile.TemporaryDirectory(prefix="propstruct_formulas_statistics_") as tmp:
        tmp_dir = Path(tmp)
        for filename, spec in FILES.items():
            fresh = json.dumps(_document(spec), indent=2, ensure_ascii=False) + "\n"
            committed_path = CASES_DIR / filename
            if not committed_path.is_file():
                problems.append(f"{filename}: not committed under {CASES_DIR}")
                continue
            committed = committed_path.read_text(encoding="utf-8")
            if fresh != committed:
                (tmp_dir / filename).write_text(fresh, encoding="utf-8")
                problems.append(f"{filename}: regeneration differs from the committed file (see {tmp_dir / filename})")
    if problems:
        for p in problems:
            print(f"MISMATCH {p}", file=sys.stderr)
        raise SystemExit(f"verify: {len(problems)} case file(s) not byte-equal on regeneration")
    print(f"verify: all {len(FILES)} case files reproduce byte for byte ({', '.join(FILES)})")


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
