"""The walk of the listing excerpt the map describes, the resolution of every event it records
to a name, and the map's checks (a)-(f) (see `listing_map.py` for what each says).

A check that fails is a stop: it names the address, the row and what the map does not say, and
the cure is the map, never the check.

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
from typing import Dict, List, NamedTuple, Optional, Set, Tuple

import fortran_source as fs
import listing_map as lm
import listing_rows as lr
import x87_listing as xl

ELEMENT_BYTES = {"r4": 4, "r8": 8, "i4": 4, "i8": 8, "i2": 2}
KNOWN_THUNKS = (0x418BA4, 0x418BAA, 0x418BB0, 0x418BF8, 0x418C10, 0x418C16, 0x418C1C)
DESCRIPTOR_BYTES = {1: 0x20, 2: 0x38}
WRITERS = ("mov", "fst", "fstp", "fist", "fistp", "fnstcw", "fnstsw", "add", "sub", "inc", "dec", "adc", "and",
           "or", "xor", "shl", "sar", "neg", "not", "setl", "setle", "setg", "setge", "setne", "setnp", "imul")


class Resolved(NamedTuple):
    event: xl.Event
    row: Optional[lm.Row]
    cls: str                   # float, integer, copy, const, array
    name: Optional[str]
    type: Optional[str]
    temp: bool


class Walked(NamedTuple):
    instructions: List[xl.Ins]
    events: List[xl.Event]
    traces: List[Tuple[lm.Region, xl.Trace]]
    cells: Dict                # the cells after the last walked region
    after_layout: Dict         # the cells after the last `layout` region


# ---- the walk ----------------------------------------------------------------------

def walk(map_: lm.Map, instructions: List[xl.Ins], image: xl.Image) -> Walked:
    """Every walked region in address order; `executed` regions in full, `layout` and `scan`
    regions for their integer content only (their float writes are not recorded)."""
    pointers = {declaration.descriptor: declaration.name for declaration in map_.arrays.values()}
    events: List[xl.Event] = []
    traces: List[Tuple[lm.Region, xl.Trace]] = []
    cells: Dict = {}
    after_layout: Dict = {}
    for region in sorted(map_.regions, key=lambda r: r.first):
        if region.mode not in ("executed", "layout", "scan"):
            continue
        walker = xl.Walker(instructions, image, pointers, integer_only=region.mode != "executed")
        state = xl.State()
        state.cells = dict(cells)
        walker.events = events
        trace = walker.run(region.first, region.end, state)
        cells = dict(walker.state.cells)
        if region.mode == "layout":
            after_layout = cells
        traces.append((region, trace))
    return Walked(instructions, events, traces, cells, after_layout)


# ---- resolution --------------------------------------------------------------------

class Resolver:
    def __init__(self, map_: lm.Map, instructions: List[xl.Ins], image: xl.Image):
        self.map = map_
        self.instructions = instructions
        self.image = image
        self.rows = sorted(map_.rows, key=lambda r: (r.first, r.end))
        self.spans = [(d.descriptor, d.descriptor + lm_descriptor_bytes(d), d) for d in map_.arrays.values()]
        self.fortran_kinds = fs.parse_declared_kinds(*fs.MAIN_DECLARATIONS)

    def rows_at(self, address: int) -> List[lm.Row]:
        return [row for row in self.rows if row.first <= address < row.end]

    def descriptor_of(self, address: int) -> Optional[lm.ArrayDecl]:
        for first, end, declaration in self.spans:
            if first <= address < end:
                return declaration
        return None

    def in_ints(self, loc: Tuple) -> bool:
        for run in self.map.runs:
            if run.first[0] == loc[0] and run.first[1] <= loc[1] <= run.last[1]:
                return True
        return False


def lm_descriptor_bytes(declaration: lm.ArrayDecl) -> int:
    return DESCRIPTOR_BYTES[declaration.rank]


def event_class(event: xl.Event) -> str:
    """float: a float store or load of a cell or an element; integer: an integer move or load;
    copy: an integer move of float bits; const: a load from .rdata; none: no memory."""
    node = event.node
    if event.kind == "store":
        return "integer" if node is not None and node.op == "toint" else "float"
    if event.kind == "chop":
        return "integer"
    if event.kind == "load":
        if node is None:
            return "none"
        return {"const": "const", "int": "integer"}.get(node.op, "float")
    if event.kind == "istore":
        return "copy" if node is not None else "integer"
    if event.kind == "iload":
        return "integer"
    return "none"


def resolve(resolver: Resolver, event: xl.Event, problems: List[str], scoped: bool) -> Optional[Resolved]:
    cls = event_class(event)
    if cls in ("none", "const"):
        return Resolved(event, None, cls, None, None, False)
    loc = event.loc
    where = f"{event.address:06X} {event.text}"
    rows = resolver.rows_at(event.address)
    row = rows[0] if len(rows) == 1 else None
    if scoped and len(rows) != 1 and cls in ("float", "copy"):
        problems.append(f"(b) {where}: {'in no row' if not rows else 'in rows ' + ', '.join(r.id for r in rows)}")
        return None
    if loc is None or loc[0] == "ptr":
        problems.append(f"(b) {where}: an address the reader could not tie to an array or a cell")
        return None
    if loc[0] == "arr":
        declaration = next((d for d in resolver.map.arrays.values() if d.name == loc[1]), None)
        size = ELEMENT_BYTES[declaration.type]
        if declaration.type in ("r4", "r8") and cls == "integer":
            cls = "copy"          # an integer move to or from a float array moves a float
        if event.width != size and not (event.width == 4 and size == 8 and cls != "float"):
            problems.append(f"(b) {where}: {event.width} bytes on {loc[1]}, a {declaration.type} array")
        return Resolved(event, row, cls, loc[1], declaration.type, False)
    cell = resolver.map.cells.get(loc)
    if cell is None:
        descriptor = resolver.descriptor_of(loc[1]) if loc[0] == "abs" else None
        if descriptor is not None and cls != "float":
            return Resolved(event, row, "integer", None, None, False)
        if cls == "float" or cls == "copy":
            problems.append(f"(b) {where}: a float event on {lm.format_location(loc)}, which the map does not name")
            return None
        if resolver.in_ints(loc):
            return Resolved(event, row, "integer", None, None, False)
        if scoped:
            problems.append(f"(b) {where}: an integer event on {lm.format_location(loc)}, which no cell, "
                            f"descriptor or ints run names")
            return None
        return Resolved(event, row, "integer", None, None, False)
    tenant = choose_tenant(cell, row, cls, problems, where)
    if tenant is None:
        return None
    size = ELEMENT_BYTES[tenant.type]
    if cls == "float" and (event.width != size or tenant.high):
        problems.append(f"(b) {where}: {event.width} bytes on {tenant.name}, a {tenant.type} "
                        f"{'upper-dword ' if tenant.high else ''}tenant")
    if cls == "copy" and event.width != size and not (tenant.type == "r8" and event.width == 4):
        problems.append(f"(b) {where}: a {event.width}-byte move on {tenant.name}, a {tenant.type} tenant")
    return Resolved(event, row, cls, tenant.name, tenant.type, tenant.temp)


def choose_tenant(cell: lm.Cell, row: Optional[lm.Row], cls: str, problems: List[str],
                  where: str) -> Optional[lm.Tenant]:
    """Integer events land on the cell's integer tenant, float events and integer moves of float
    bits on the one float tenant its row names."""
    wants_integer = cls == "integer"
    candidates = [t for t in cell.tenants if (t.type in ("i2", "i4", "i8")) == wants_integer
                  and not (cls == "float" and t.high)]
    if not candidates and wants_integer:
        candidates = list(cell.tenants)       # an integer move of a float's bits
    if len(candidates) == 1:
        return candidates[0]
    if row is None:
        problems.append(f"(d) {where}: {lm.format_location(cell.loc)} has {len(candidates)} candidate tenants "
                        f"and the event is in no row")
        return None
    chosen = [t for t in candidates if t.name in row.names]
    if len(chosen) != 1:
        problems.append(f"(d) {where}: row {row.id} names {len(chosen)} of the tenants of "
                        f"{lm.format_location(cell.loc)} ({', '.join(t.name for t in candidates)})")
        return None
    return chosen[0]


# ---- the checks --------------------------------------------------------------------

class Program(NamedTuple):
    map: lm.Map
    instructions: List[xl.Ins]
    excerpt_ranges: List[Tuple[int, int, str]]
    image: xl.Image
    walked: Walked
    resolved: List[Resolved]
    problems: List[str]


def load(map_: Optional[lm.Map] = None, excerpt: Optional[Tuple[List[xl.Ins], List[Tuple[int, int, str]]]] = None,
         image: Optional[xl.Image] = None) -> Program:
    """Reads the excerpt, walks it by the map's regions, resolves every event the map's scopes
    cover and runs the checks; the result carries every problem found, none raised. The map,
    the excerpt and the image may be given (the selftests hand in mutated ones)."""
    map_ = map_ if map_ is not None else lm.parse()
    instructions, ranges = excerpt if excerpt is not None else xl.read_excerpt()
    image = image if image is not None else xl.Image()
    problems: List[str] = []
    check_ranges(map_, instructions, ranges, problems)
    try:
        walked = walk(map_, instructions, image)
    except xl.StopReading as stop:
        return Program(map_, instructions, ranges, image, Walked(instructions, [], [], {}, {}), [], problems + [f"walk: {stop}"])
    resolver = Resolver(map_, instructions, image)
    resolved: List[Resolved] = []
    for event in walked.events:
        if not any(scope.first <= event.address < scope.end for scope in map_.scopes):
            continue
        item = resolve(resolver, event, problems, True)
        if item is not None:
            resolved.append(item)
    check_rows_use_names(map_, resolved, problems)
    check_assignments(map_, resolved, problems)
    check_claims(map_, walked, problems)
    check_fortran_join(map_, problems)
    check_calls(map_, walked, problems)
    check_ints(map_, resolver, walked, instructions, problems)
    check_inputs(map_, walked, instructions, problems)
    check_arrays(map_, instructions, problems)
    return Program(map_, instructions, ranges, image, walked, resolved, problems)


def check_ranges(map_: lm.Map, instructions: List[xl.Ins], ranges: List[Tuple[int, int, str]],
                 problems: List[str]) -> None:
    boundaries = {ins.address for ins in instructions} | {ins.end for ins in instructions}
    for region in map_.regions:
        for point in (region.first, region.end):
            if point not in boundaries:
                problems.append(f"(a) region {region.id} (map line {region.source}): {point:06X} is not an "
                                f"instruction boundary of the excerpt")
    for scope in map_.scopes:
        inside = [r for r in map_.regions if r.mode == "executed" and r.first <= scope.first and scope.end <= r.end]
        if not inside:
            problems.append(f"(a) scope at map line {scope.source} lies in no executed region")
        for point in (scope.first, scope.end):
            if point not in boundaries:
                problems.append(f"(a) scope at map line {scope.source}: {point:06X} is not an instruction boundary")
    for row in map_.rows:
        if row.first == row.end and row.attrs.get("eliminated") == "yes":
            continue
        for point in (row.first, row.end):
            if point not in boundaries:
                problems.append(f"(a) row {row.id} (map line {row.source}): {point:06X} is not an instruction "
                                f"boundary of the excerpt")
        if not any(scope.first <= row.first and row.end <= scope.end for scope in map_.scopes):
            problems.append(f"(a) row {row.id} (map line {row.source}) lies in no scope")
        if row.first >= row.end:
            problems.append(f"(a) row {row.id} (map line {row.source}) is empty and not `eliminated=yes`")
    for scope in map_.scopes:
        inside = sorted((r for r in map_.rows if r.first < r.end and scope.first <= r.first and r.end <= scope.end),
                        key=lambda r: (r.first, r.end))
        cursor = scope.first
        for row in inside:
            if row.first != cursor:
                problems.append(f"(a) row {row.id} starts at {row.first:06X}, "
                                f"{'after a gap' if row.first > cursor else 'inside the row before it'} "
                                f"({cursor:06X} is where the scope at map line {scope.source} is covered to)")
            cursor = max(cursor, row.end)
        if cursor != scope.end:
            problems.append(f"(a) the rows cover the scope at map line {scope.source} only to {cursor:06X}, "
                            f"not {scope.end:06X}")
    covered = sorted((r.first, r.end) for r in map_.regions)
    for first, end, what in ranges:
        if not any(a == first and b == end for a, b in covered):
            problems.append(f"(a) the excerpt's range {first:06X}-{end:06X} ({what}) is not exactly one region")
    for first, end in covered:
        if not any(a == first and b == end for a, b, _ in ranges):
            problems.append(f"(a) region {first:06X}-{end:06X} is not exactly one of the excerpt's ranges")


def check_rows_use_names(map_: lm.Map, resolved: List[Resolved], problems: List[str]) -> None:
    used: Dict[str, Set[str]] = {row.id: set() for row in map_.rows}
    for item in resolved:
        if item.row is not None and item.name is not None and item.type not in ("i2", "i4", "i8"):
            used[item.row.id].add(item.name)
    for row in map_.rows:
        claimed = {token.split("@")[0] for token in row.attrs.get("register", "").split(",") if token}
        for name in row.names:
            if name not in used[row.id] and name not in claimed:
                problems.append(f"(b) row {row.id} (map line {row.source}) names {name}, which nothing in "
                                f"{row.first:06X}-{row.end:06X} uses")
    tenants_used: Set[Tuple[Tuple, str]] = set()
    for item in resolved:
        if item.name is not None and item.event.loc is not None and item.event.loc[0] in ("abs", "frame"):
            tenants_used.add((item.event.loc, item.name))
    for loc, cell in map_.cells.items():
        for tenant in cell.tenants:
            if (loc, tenant.name) not in tenants_used:
                problems.append(f"(d) cell {lm.format_location(loc)} (map line {cell.source}): tenant "
                                f"{tenant.name} is used by no event in a scope")


def all_nodes(events: List[xl.Event]) -> Dict[int, xl.Node]:
    found: Dict[int, xl.Node] = {}

    def visit(node: Optional[xl.Node]) -> None:
        if node is None or id(node) in found:
            return
        found[id(node)] = node
        for argument in node.args:
            if isinstance(argument, xl.Node):
                visit(argument)

    for event in events:
        visit(event.node)
        visit(event.other)
    return found


def claimed_nodes(map_: lm.Map, walked: Walked, problems: List[str]) -> Dict[Tuple[str, int], List[xl.Node]]:
    """The nodes each register claim names, found by the instruction that made them: the claim's
    operator, the k-th node of that instruction. A claim must name a node that exists, that no
    store event stores and that some later expression or comparison consumes."""
    nodes = all_nodes(walked.events)
    stored = {id(e.node) for e in walked.events if e.kind == "store" and e.node is not None}
    consumed: Set[int] = set()
    for node in nodes.values():
        for argument in node.args:
            if isinstance(argument, xl.Node):
                consumed.add(id(argument))
    for event in walked.events:
        if event.kind == "cmp":
            consumed.add(id(event.node))
            consumed.add(id(event.other))
    by_site: Dict[Tuple[int, str], List[xl.Node]] = {}
    for node in sorted(nodes.values(), key=lambda n: n.serial):
        by_site.setdefault((node.born, node.op), []).append(node)
    found: Dict[Tuple[str, int], List[xl.Node]] = {}
    for row in map_.rows:
        for claim in lr.register_claims(row):
            for address, index in claim.sites:
                candidates = by_site.get((address, claim.op), [])
                where = f"row {row.id} (map line {row.source}): {claim.name} line {claim.line}, {address:06X}~{index}"
                if index >= len(candidates):
                    problems.append(f"(b) {where}: no `{claim.op}` value is made there")
                    continue
                node = candidates[index]
                if id(node) in stored:
                    problems.append(f"(b) {where}: the value is stored, not a register value")
                elif id(node) not in consumed:
                    problems.append(f"(b) {where}: nothing consumes the value")
                found.setdefault((claim.name, claim.line), []).append(node)
    return found


def check_claims(map_: lm.Map, walked: Walked, problems: List[str]) -> None:
    claimed_nodes(map_, walked, problems)


def is_constant(event: xl.Event) -> bool:
    """A store or move of a number the executable holds: a constant loaded from .rdata."""
    node = event.node
    if node is None:
        return False
    if node.op == "const":
        return True
    return node.op == "copy" and isinstance(node.args[0], tuple) and node.args[0][0] == "rdata"


def check_assignments(map_: lm.Map, resolved: List[Resolved], problems: List[str]) -> None:
    """Every REAL assignment a row's lines hold is stored (a constant to the literal assignment, a
    computed value to the one computed assignment, or the `stores=` line), claimed as a register
    value or declared eliminated; and every claim is such an assignment of the row."""
    seen: Dict[str, Dict[str, bool]] = {row.id: {} for row in map_.rows}
    temporaries = {t.name for cell in map_.cells.values() for t in cell.tenants if t.temp}
    for item in resolved:
        if item.row is None or item.name is None or item.type not in ("r4", "r8"):
            continue
        if item.name in temporaries and item.event.kind == "store":
            continue                      # a value on its way through a temporary: the move names the home
        kinds = ("constant", "computed") if item.event.node is None else \
            (("constant",) if is_constant(item.event) else ("computed",))
        for kind in kinds:           # a move of a merged value holds every line's value
            seen[item.row.id].setdefault(item.name.lower(), {})[kind] = True
    for row in map_.rows:
        mine = lr.assigns(row)
        claimed = {(c.name, c.line) for c in lr.register_claims(row)}
        eliminated = set(lr.name_lines(row, "eliminated"))
        hints = lr.store_hints(row)
        events = seen[row.id]
        for assign in mine:
            key = (assign.name, assign.line)
            if key in claimed or key in eliminated:
                continue
            kinds = events.get(assign.name, {})
            if assign.literal:
                covered = kinds.get("constant", False)
            else:
                others = [a for a in mine if a.name == assign.name and not a.literal]
                covered = kinds.get("computed", False) and (len(others) == 1 or assign.line in hints.get(assign.name, ()))
            if not covered:
                problems.append(f"(b) row {row.id} (map line {row.source}): line {assign.line} assigns {assign.name}, "
                                f"which the row neither stores nor claims as a register value nor declares "
                                f"eliminated")
        for name, line in sorted(eliminated):
            if name in events:
                problems.append(f"(b) row {row.id} (map line {row.source}): line {line} declares {name} eliminated "
                                f"and the row stores it")
        valid = {(a.name, a.line) for a in mine}
        hinted = {(name, line) for name, lines in hints.items() for line in lines}
        for name, line in sorted(claimed | eliminated | hinted):
            if (name, line) not in valid:
                problems.append(f"(b) row {row.id} (map line {row.source}): the claim on {name} line {line} is no "
                                f"REAL assignment of the row's lines")


def check_fortran_join(map_: lm.Map, problems: List[str]) -> None:
    kinds = fs.parse_declared_kinds(*fs.MAIN_DECLARATIONS)
    texts = lr.line_texts()
    fortran_names = {t.name for cell in map_.cells.values() for t in cell.tenants if not t.temp}
    fortran_names |= {d.name for d in map_.arrays.values()}
    declared_names = fortran_names | {t.name for cell in map_.cells.values() for t in cell.tenants}
    for row in map_.rows:
        for number in row.lines + lr.part_lines(row):
            if number not in texts:
                problems.append(f"(d) row {row.id} (map line {row.source}): Fortran line {number} is not a "
                                f"statement line")
        text = " ".join(texts.get(number, "") for number in row.lines + lr.part_lines(row)).lower()
        for name in row.names:
            if name not in declared_names:
                problems.append(f"(d) row {row.id} (map line {row.source}): {name} is no cell tenant and no array")
                continue
            if name not in fortran_names:
                continue
            if not re.search(r"(?<![a-z0-9_])" + re.escape(name.lower()) + r"(?![a-z0-9_])", text):
                problems.append(f"(d) row {row.id} (map line {row.source}): {name} does not occur in the "
                                f"text of lines {', '.join(str(n) for n in row.lines)}")
    for cell in map_.cells.values():
        for tenant in cell.tenants:
            if not tenant.temp and fs.kind_code(tenant.name.lower(), kinds) != tenant.type:
                problems.append(f"(d) cell {lm.format_location(cell.loc)} (map line {cell.source}): "
                                f"{tenant.name} is {tenant.type} in the map and "
                                f"{fs.kind_code(tenant.name.lower(), kinds)} in the Fortran")
    for declaration in map_.arrays.values():
        if fs.kind_code(declaration.name.lower(), kinds) != declaration.type:
            problems.append(f"(d) array {declaration.name} (map line {declaration.source}): "
                            f"{declaration.type} in the map and {fs.kind_code(declaration.name.lower(), kinds)} "
                            f"in the Fortran")


THUNK_DEPTH = {0x418BA4: 0, 0x418BAA: 0, 0x418BB0: 0, 0x418BF8: 0, 0x418C10: 2, 0x418C16: 1, 0x418C1C: 0}


def check_calls(map_: lm.Map, walked: Walked, problems: List[str]) -> None:
    """Every call in an executed region goes to a thunk the reader has a stub for, with the
    x87 stack at the depth that thunk's contract takes: the compiler's `ffree` before a call
    leave only the operands, and the reader's stack must agree."""
    for region, trace in walked.traces:
        if region.mode != "executed":
            continue
        for ins in (e for e in trace_calls(walked, region)):
            target = int(ins.args[0], 16)
            depth = trace.depth_at.get(ins.address)
            if target not in THUNK_DEPTH:
                problems.append(f"(c) {ins.address:06X}: call {target:06X}, which is none of the thunks the "
                                f"reader has a stub for")
            elif depth != THUNK_DEPTH[target]:
                problems.append(f"(c) {ins.address:06X}: call {target:06X} with {depth} x87 registers live, "
                                f"the thunk's contract is {THUNK_DEPTH[target]}")


def trace_calls(walked: Walked, region: lm.Region) -> List[xl.Ins]:
    return [ins for ins in walked.instructions if region.first <= ins.address < region.end and ins.mnemonic == "call"]


def writes_to(ins: xl.Ins, loc: Tuple) -> bool:
    if ins.mnemonic not in WRITERS or not ins.args or "[" not in ins.args[0]:
        return False
    memory = xl.parse_memory(ins.args[0])
    if memory is None:
        return False
    if memory.absolute:
        return loc == ("abs", memory.disp & 0xFFFFFFFF)
    return memory.base == "ebp" and memory.index is None and loc == ("frame", memory.disp)


def live_ins(map_: lm.Map, walked: Walked) -> Dict[Tuple, int]:
    """The float cells a scoped load reads while the walk has not written them: loc -> address."""
    live: Dict[Tuple, int] = {}
    for event in walked.events:
        if not any(scope.first <= event.address < scope.end for scope in map_.scopes):
            continue
        if event.kind == "load" and event.node is not None and event.node.op in ("ld", "int"):
            if event.loc and event.loc[0] in ("abs", "frame") and event.node.args[2] is None:
                live.setdefault(event.loc, event.address)
    return live


def check_inputs(map_: lm.Map, walked: Walked, instructions: List[xl.Ins], problems: List[str]) -> None:
    live = live_ins(map_, walked)
    for loc, address in sorted(live.items()):
        declared = map_.inputs.get(loc)
        if declared is None and any(run.first[0] == loc[0] and run.first[1] <= loc[1] <= run.last[1]
                                    for run in map_.runs):
            continue                      # an ints run names it and its origin
        if declared is None:
            problems.append(f"(e) {address:06X}: {lm.format_location(loc)} is read before the walk has written "
                            f"it, and the map names no input")
            continue
        region = next((r for r in map_.regions if r.id == declared.origin), None)
        if declared.origin == "external":
            if any(writes_to(ins, loc) for ins in instructions):
                problems.append(f"(e) input {lm.format_location(loc)} (map line {declared.source}) is `external` "
                                f"and an instruction of the excerpt writes it")
        elif region is None:
            problems.append(f"(e) input {lm.format_location(loc)} (map line {declared.source}): origin "
                            f"{declared.origin} is no region")
        elif not any(region.first <= ins.address < region.end and writes_to(ins, loc) for ins in instructions):
            problems.append(f"(e) input {lm.format_location(loc)} (map line {declared.source}): no instruction "
                            f"of region {region.id} writes it")
    for loc, declared in map_.inputs.items():
        if loc not in live:
            problems.append(f"(e) input {lm.format_location(loc)} (map line {declared.source}) is read by nothing "
                            f"before the walk has written it")


def check_ints(map_: lm.Map, resolver: Resolver, walked: Walked, instructions: List[xl.Ins],
               problems: List[str]) -> None:
    """Every integer slot a scoped event touches, outside the named cells and the descriptors, lies
    in an ints run, and the run's origin writes it (or nothing does, for `external`)."""
    touched: Dict[int, Set[Tuple]] = {index: set() for index in range(len(map_.runs))}
    for event in walked.events:
        if not any(scope.first <= event.address < scope.end for scope in map_.scopes):
            continue
        if event_class(event) not in ("integer", "copy") or event.loc is None or event.loc[0] not in ("abs", "frame"):
            continue
        if event.loc in map_.cells or (event.loc[0] == "abs" and resolver.descriptor_of(event.loc[1])):
            continue
        for index, run in enumerate(map_.runs):
            if run.first[0] == event.loc[0] and run.first[1] <= event.loc[1] <= run.last[1]:
                touched[index].add(event.loc)
    regions = {region.id: region for region in map_.regions}
    for index, run in enumerate(map_.runs):
        if not touched[index]:
            problems.append(f"(e) ints run {lm.format_location(run.first)}-{lm.format_location(run.last)} "
                            f"(map line {run.source}) is touched by no event in a scope")
        for loc in sorted(touched[index]):
            writers = [r.id for r in map_.regions if r.mode in ("executed", "layout", "scan")
                       and any(r.first <= i.address < r.end and writes_to(i, loc) for i in instructions)]
            if run.origin == "external":
                if writers:
                    problems.append(f"(e) {lm.format_location(loc)} (map line {run.source}) is `external` and "
                                    f"region {writers[0]} writes it")
            elif run.origin not in regions:
                problems.append(f"(e) ints run at map line {run.source}: origin {run.origin} is no region")
            elif run.origin not in writers:
                problems.append(f"(e) {lm.format_location(loc)} (map line {run.source}): no instruction of "
                                f"region {run.origin} writes it")


def allocate_order() -> List[Tuple[str, int]]:
    """(name, rank) of every array of every ALLOCATE statement, in source order."""
    found: List[Tuple[str, int]] = []
    for statement in fs.statements(1, fs.MAIN_PROGRAM_END):
        match = re.match(r"^allocate\s*\((.*)\)\s*$", statement.text, re.IGNORECASE)
        if not match:
            continue
        for entry in fs._split_top_level(match.group(1), ","):
            name = re.match(r"\s*([A-Za-z_][A-Za-z0-9_]*)\s*\((.*)\)\s*$", entry)
            if name:
                found.append((name.group(1), len(fs._split_top_level(name.group(2), ","))))
    return found


def immediate_stores(instructions: List[xl.Ins], address: int) -> Set[int]:
    """The immediates `mov dword ptr ds:[address], imm` writes anywhere in the excerpt."""
    values: Set[int] = set()
    for ins in instructions:
        if ins.mnemonic == "mov" and len(ins.args) == 2 and ins.args[0].startswith("dword ptr ds:["):
            memory = xl.parse_memory(ins.args[0])
            if memory is not None and memory.absolute and (memory.disp & 0xFFFFFFFF) == address                     and re.fullmatch(r"-?[0-9A-F]+h?", ins.args[1]):
                values.add(int(ins.args[1][:-1], 16) if ins.args[1].endswith("h") else int(ins.args[1]))
    return values


def check_arrays(map_: lm.Map, instructions: List[xl.Ins], problems: List[str]) -> None:
    flag_sites: Dict[int, int] = {}
    for ins in instructions:
        if ins.mnemonic == "mov" and len(ins.args) == 2 and ins.args[0].startswith("byte ptr ds:[")                 and ins.args[1] == "5":
            memory = xl.parse_memory(ins.args[0])
            flag_sites.setdefault((memory.disp & 0xFFFFFFFF) - 0xC, ins.address)
    expected = allocate_order()
    declared = sorted(map_.arrays.values(), key=lambda d: flag_sites.get(d.descriptor, 1 << 40))
    if [d.name.lower() for d in declared] != [n.lower() for n, _ in expected]:
        problems.append("(f) the arrays in the order of their ALLOCATE sites are not the Fortran's allocate "
                        "statements' arrays in source order")
    ranks = {name.lower(): rank for name, rank in expected}
    for declaration in map_.arrays.values():
        if declaration.descriptor not in flag_sites:
            problems.append(f"(f) array {declaration.name} (map line {declaration.source}): no ALLOCATE site "
                            f"writes the flag byte of descriptor {declaration.descriptor:06X}")
            continue
        if ranks.get(declaration.name.lower()) != declaration.rank:
            problems.append(f"(f) array {declaration.name}: rank {declaration.rank} in the map, "
                            f"{ranks.get(declaration.name.lower())} in the Fortran's ALLOCATE")
        for offset, what, value in ((4, "element size", ELEMENT_BYTES[declaration.type]),
                                    (0x10, "rank", declaration.rank)):
            written = immediate_stores(instructions, declaration.descriptor + offset)
            if written != {value}:
                problems.append(f"(f) array {declaration.name}: the ALLOCATE code writes {what} "
                                f"{sorted(written)}, the map says {value}")
