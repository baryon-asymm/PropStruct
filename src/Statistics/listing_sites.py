"""Derives, from the walked listing and the address map, every site of the per-cycle plane: what
the executable does at each store, move, register value and comparison, joined with the
Fortran line it compiles.

A **site** is the stores of one Fortran name by one line of one row (a statement's unrolled
passes and its remainder loop are one site), a register value the map claims, or a line the map
declares eliminated. What it records is read off the walk, never typed:

  kind   S  a REAL*4 store: rounds to binary32 where it stands
         Q  a REAL*8 store: exact
         K  a store or move of a constant: exact
         M  a move of a value the executable loaded or merged: exact
         T  a store to a compiler temporary no Fortran name stands for: rounds
         P  a sum stored every pass (reloaded from its home every pass)
         B<n> a sum unrolled n-fold: reloaded at each block head, stored after pass n, and
            stored every pass of the remainder loop
         X  a sum loaded once before its loop and carried in the register, stored every pass
         C<n> stores each of which reads the value the one before it stored from the register
         R  a value held in an x87 register and never stored (a claim of the map)
         E  a store of a value no line of the program reads, or a line with no code
  reads  every operand: `name:home` (loaded from its home), `name:input` (a home nothing in the
         walk stored: the particle loop's or the setup's), `name:register` (the value, before
         rounding, of a store or a claimed register value an earlier line made)
  order  the expression as the executable forms it, operands by name, array offsets dropped

Python 3.8+, standard library only.
"""

from __future__ import annotations

import re
from typing import Dict, List, NamedTuple, Optional, Set, Tuple

import fortran_source as fs
import listing_checks as lc
import listing_map as lm
import listing_rows as lr
import x87_listing as xl


class Site(NamedTuple):
    first: int                 # the lowest address of its events
    row: str
    lines: Tuple[int, ...]
    name: str
    lhs: str
    type: str
    kind: str
    rounds: str
    home: str
    count: int
    reads: Tuple[str, ...]
    schedule: str
    order: str


def fmt(value: float) -> str:
    return repr(float(value))


class Reader:
    """Names the nodes of the walk: a load by its resolved tenant, a stored node by its site."""

    def __init__(self, program: lc.Program):
        self.program = program
        self.map = program.map
        events = program.walked.events
        self.resolved = {item.event.index: item for item in program.resolved}
        self.load_event = {id(e.node): e for e in events if e.kind == "load" and e.node is not None
                           and e.node.op in ("ld", "int")}
        self.stores: Dict[int, xl.Event] = {id(e.node): e for e in events if e.kind == "store"
                                            and e.node is not None and e.node.op != "toint"
                                            and lc.event_class(e) == "float"}
        self.copies: Dict[int, List[xl.Event]] = {}
        for e in events:
            if e.kind == "istore" and e.node is not None and e.node.op == "copy":
                source = e.node.args[0]
                if isinstance(source, tuple) and source[0] == "event":
                    self.copies.setdefault(source[1], []).append(e)
        self.claimed: Dict[int, Tuple[str, int]] = {}
        for (name, line), nodes in lc.claimed_nodes(self.map, program.walked, []).items():
            for node in nodes:
                self.claimed[id(node)] = (name, line)
        self.loops: List[Tuple[int, int]] = []
        for region, trace in program.walked.traces:
            if region.mode == "executed":
                self.loops.extend(trace.loops)
        self.site_name: Dict[int, str] = {}
        self.read_names = read_names()
        self.array_first_write: Dict[str, int] = {}
        for e in events:
            if e.kind in ("store", "istore") and e.loc is not None and e.loc[0] == "arr" \
                    and self.in_executed(e.address):
                self.array_first_write.setdefault(e.loc[1], e.index)

    def in_executed(self, address: int) -> bool:
        return any(r.mode == "executed" and r.first <= address < r.end for r in self.map.regions)

    def int_name(self, node: xl.Node) -> str:
        """An integer operand: the array it is an element of, or `i` for a counter."""
        loc = node.args[0]
        return str(loc[1]) if loc[0] == "arr" else "i"

    def name_of_load(self, node: xl.Node) -> str:
        event = self.load_event.get(id(node))
        item = self.resolved.get(event.index) if event is not None else None
        if item is not None and item.name is not None:
            return item.name.lower()
        loc = node.args[0]
        return lm.format_location(loc) if loc[0] in ("abs", "frame") else str(loc[1])

    def innermost_loop(self, store: xl.Event) -> Optional[Tuple[int, int]]:
        around = [(head, back) for head, back in self.loops if head <= store.address <= back]
        return min(around, key=lambda loop: loop[1] - loop[0]) if around else None

    def in_any_loop(self, store: xl.Event) -> bool:
        return self.innermost_loop(store) is not None

    def in_same_loop(self, store: xl.Event, born: int) -> bool:
        """The load of the stored name is one of the innermost loop's own instructions."""
        loop = self.innermost_loop(store)
        return loop is not None and loop[0] <= born <= loop[1]


def addends(node: xl.Node, reader: Reader, root: xl.Node) -> List[xl.Node]:
    """The terms of a sum, nested sums flattened, a stored value of another store kept whole."""
    if node.op == "+" and (node is root or id(node) not in reader.stores):
        return addends(node.args[0], reader, root) + addends(node.args[1], reader, root)
    return [node]


def render(node: xl.Node, reader: Reader, own: Optional[xl.Node] = None, depth: int = 0) -> str:
    if node is not own and id(node) in reader.stores:
        stored = reader.stores[id(node)]
        return f"{reader.site_name.get(stored.index, '?')}@register"
    if node is not own and id(node) in reader.claimed:
        return f"{reader.claimed[id(node)][0]}@register"
    if node.op == "ld":
        return reader.name_of_load(node)
    if node.op == "int":
        return f"real({reader.int_name(node)})"
    if node.op == "const":
        return fmt(node.args[0])
    if node.op in ("+", "-", "*", "/"):
        return f"({render(node.args[0], reader, own, depth + 1)} {node.op} {render(node.args[1], reader, own, depth + 1)})"
    if node.op in ("abs", "chs", "sqrt", "toint"):
        return f"{node.op}({render(node.args[0], reader, own, depth + 1)})"
    if node.op == "pow":
        return f"pow({render(node.args[0], reader, own, depth + 1)}, {render(node.args[1], reader, own, depth + 1)})"
    if node.op == "call":
        return f"{node.args[0]}(...)"
    return f"{'merge' if node.op in ('phi', 'maybe') else node.op}@{node.born:06X}"


def read_source(node: xl.Node, reader: "Reader") -> str:
    """`home` for a load of a home an executed region wrote (a store, or an integer move), `input`
    for one it did not (the particle loop's or the setup's value)."""
    loc = node.args[0]
    if loc[0] == "arr":
        first = reader.array_first_write.get(loc[1])
        event = reader.load_event.get(id(node))
        return "home" if first is not None and event is not None and first < event.index else "input"
    written = node.args[3]
    if written is None:
        return "input"
    return "home" if reader.in_executed(reader.program.walked.events[written].address) else "input"


def collect_reads(node: xl.Node, reader: Reader, own: xl.Node, found: List[str], skip_self: str,
                  into_merges: bool = False) -> None:
    if node is not own and id(node) in reader.stores:
        found.append(f"{reader.site_name.get(reader.stores[id(node)].index, '?')}:register")
        return
    if node is not own and id(node) in reader.claimed:
        if reader.claimed[id(node)][0] != skip_self:
            found.append(f"{reader.claimed[id(node)][0]}:register")
        return
    if node.op == "ld":
        name = reader.name_of_load(node)
        if name != skip_self:
            found.append(f"{name}:{read_source(node, reader)}")
        return
    if node.op == "int":
        if node.args[0][0] == "arr":
            found.append(f"{node.args[0][1].lower()}:{read_source(node, reader)}")
        return
    if node.op == "const":
        return
    if node.op in ("phi", "maybe") and not into_merges:
        found.append(f"merge@{node.born:06X}:register")
        return
    for argument in node.args:
        if isinstance(argument, xl.Node):
            collect_reads(argument, reader, own, found, skip_self, into_merges)


class Group:
    def __init__(self, row: lm.Row, name: str, lines: Tuple[int, ...]):
        self.row = row
        self.name = name
        self.lines = lines
        self.events: List[xl.Event] = []
        self.items: List[lc.Resolved] = []
        self.kinds: Set[str] = set()


def destination(reader: Reader, store: xl.Event, item: lc.Resolved) -> Tuple[str, str, Optional[int]]:
    """(final name, home text, the move that is the home end): a store to a temporary whose bits
    an integer move carries to a home of the same row takes that home's name; a store to a
    temporary nothing moves is the temporary's."""
    if not item.temp:
        return item.name.lower(), home_text(item), None
    moves = [m for m in reader.copies.get(store.index, []) if m.index in reader.resolved
             and not reader.resolved[m.index].temp and reader.resolved[m.index].row == item.row]
    if moves:
        first = min(moves, key=lambda m: m.address)
        target = reader.resolved[first.index]
        return (target.name.lower(), f"via {lm.format_location(store.loc)} to {home_text(target)}", first.index)
    return item.name.lower(), home_text(item), None


def home_text(item: lc.Resolved) -> str:
    loc = item.event.loc
    if loc[0] == "arr":
        return f"array {loc[1]}"
    return f"cell {lm.format_location(loc)}"


def temp_line(row: lm.Row, name: str) -> Tuple[int, ...]:
    hinted = [line for hint, line in lr.name_lines(row, "temp") if hint == name]
    if hinted:
        return (hinted[0],)
    own = tuple(row.lines) or lr.part_lines(row)
    return own[:1] if len(own) == 1 else own


def derive(program: lc.Program) -> List[Site]:
    """Every site of the plane, in address order."""
    reader = Reader(program)
    texts = lr.line_texts()
    problems: List[str] = []
    groups: Dict[Tuple[str, str, Tuple[int, ...]], Group] = {}
    names: Dict[int, str] = {}
    home_ends: Set[int] = set()

    def line_of(row: lm.Row, name: str, constant: bool, event: xl.Event) -> Tuple[int, ...]:
        lines, why = lr.place(row, name, constant)
        if why is not None:
            problems.append(f"sites: {event.address:06X}: {why}")
        return lines

    # 1. the stores and moves, grouped by row, name and line
    for item in program.resolved:
        event = item.event
        if item.row is None or item.type not in ("r4", "r8"):
            continue
        if event.kind == "store" and item.cls == "float":
            final, home, home_end = destination(reader, event, item)
            if home_end is not None:
                home_ends.add(home_end)
            names[event.index] = final
            reader.site_name[event.index] = final
            temporary = item.temp and home_end is None
            lines = temp_line(item.row, final) if temporary else line_of(item.row, final, lc.is_constant(event), event)
        elif event.kind == "istore" and item.cls == "copy":
            source = event.node.args[0] if event.node is not None and event.node.op == "copy" else None
            if event.index in home_ends:
                continue                       # the home end of a store through a temporary
            if item.temp:
                continue                       # an argument or scratch copy
            constant = lc.is_constant(event)
            final, home = item.name.lower(), home_text(item)
            lines = line_of(item.row, final, constant, event)
        else:
            continue
        key = (item.row.id, final, lines)
        group = groups.setdefault(key, Group(item.row, final, lines))
        group.events.append(event)
        group.items.append(item)

    # 2. facts per group
    sites: List[Site] = []
    for group in groups.values():
        sites.append(make_site(group, reader, texts))

    # 3. register values, eliminated lines
    for row in program.map.rows:
        for claim in lr.register_claims(row):
            sites.append(register_site(row, claim, reader, program, texts))
        for name, line in lr.name_lines(row, "eliminated"):
            sites.append(Site(row.first, row.id, (line,), name, lhs_of(texts, line, name), "-", "E", "-",
                              "no code", 0, (), "-", "-"))
    sites.extend(compare_sites(program, reader, texts))
    sites.sort(key=lambda s: (s.first, s.row, s.lines, s.name))
    if problems:
        raise lm.MapError("; ".join(sorted(set(problems))[:5]))
    return sites


def compare_sites(program: lc.Program, reader: Reader, texts: Dict[int, str]) -> List[Site]:
    """A site per distinct comparison of a row: what the executable compares, home or register."""
    found: Dict[Tuple[str, str], Site] = {}
    rows = sorted(program.map.rows, key=lambda r: r.first)
    for event in program.walked.events:
        if event.kind != "cmp" or not any(r.first <= event.address < r.end for r in program.map.scopes):
            continue
        row = next((r for r in rows if r.first <= event.address < r.end), None)
        if row is None:
            continue
        shape = f"{render(event.node, reader)} ? {render(event.other, reader)}"
        key = (row.id, shape)
        if key in found:
            old = found[key]
            found[key] = old._replace(count=old.count + 1)
            continue
        reads: List[str] = []
        for operand in (event.node, event.other):
            collect_reads(operand, reader, operand, reads, "")
        branches = tuple(line for line in row.lines if re.match(r"^(if|else)", texts.get(line, ""), re.IGNORECASE))
        found[key] = Site(event.address, row.id, branches or tuple(row.lines)[:1], "-", "compare", "-", "CMP", "-",
                          "flags", 1, tuple(dict.fromkeys(reads)), "-", shape)
    return list(found.values())


def lhs_of(texts: Dict[int, str], line: int, name: str) -> str:
    parsed = lr.split_assignment(texts.get(line, ""))
    return parsed[1] if parsed else name


def read_names() -> Set[str]:
    """The names some statement of the program reads: every occurrence outside a declaration, an
    ALLOCATE and the left-hand side of an assignment (its subscripts are reads)."""
    found: Set[str] = set()
    word = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
    for statement in fs.statements(1, fs.MAIN_PROGRAM_END):
        text = statement.text
        if re.match(r"^(real|integer|character|logical|allocate|deallocate|implicit|use|dimension)\b", text,
                    re.IGNORECASE):
            continue
        parsed = lr.split_assignment(text)
        if parsed is not None:
            name, lhs, rhs = parsed
            subscripts = lhs[len(name):] if lhs.lower().startswith(name) else lhs
            text = subscripts + " " + rhs
        found.update(w.lower() for w in word.findall(text))
    return found


def make_site(group: Group, reader: Reader, texts: Dict[int, str]) -> Site:
    events = group.events
    first = events[0]
    item = group.items[0]
    stores = [e for e in events if e.kind == "store"]
    width = first.width
    typed = item.type
    kinds: List[str] = []
    schedule = "-"
    counts = {"carry": 0, "memory": 0}
    unrolls: Set[int] = set()
    reads: List[str] = []
    orders: List[str] = []
    if not stores:
        kind = "K" if all(lc.is_constant(e) for e in events) else "M"
    else:
        for event in stores:
            node = event.node
            reader.site_name.setdefault(event.index, group.name)
            terms = addends(node, reader, node) if node.op == "+" else [node]
            accumulators = accumulator_leaves(terms, event, group.name, reader)
            carries = [t for t in terms if t is not node and id(t) in reader.stores
                       and reader.site_name.get(reader.stores[id(t)].index) == group.name]
            if carries:
                kinds.append("C")
                counts["carry"] += 1
            elif accumulators:
                acc = accumulators[0]
                unrolls.add(len(terms) - len(accumulators))
                if reader.in_same_loop(event, acc.born):
                    kinds.append("P")
                else:
                    kinds.append("X" if reader.in_any_loop(event) else "S")
            else:
                kinds.append("Q" if width == 8 else "S")
                counts["memory"] += 1
            body = [t for t in terms if t not in accumulators and t not in carries] if node.op == "+" else [node]
            found: List[str] = []
            for term in body:
                collect_reads(term, reader, node, found, "")
            for carry in carries:
                found.append(f"{group.name}:register")
            for entry in found:
                if entry not in reads:
                    reads.append(entry)
            rendered = " + ".join(render(t, reader, node) for t in body[:1]) if accumulators or carries \
                else render(node, reader, node)
            if rendered not in orders:
                orders.append(rendered)
        kind = combine(kinds, unrolls)
        schedule = describe(kind, kinds, unrolls, counts)
    if stores and item.temp and destination(reader, first, item)[2] is None:
        kind = "T"
    elif stores and not item.temp and group.name not in reader.read_names:
        kind = "E"
    rounds = "yes" if width == 4 and stores else "no"
    if kind == "K" or kind == "M":
        rounds = "no"
    lhs = " ; ".join(lhs_of(texts, line, group.name) for line in group.lines)
    home = destination(reader, first, group.items[0])[1] if stores else home_text(group.items[0])
    return Site(min(e.address for e in events), group.row.id, group.lines, group.name, lhs, typed, kind, rounds,
                home, len(events), tuple(reads), schedule, " ;; ".join(orders) or "-")


def accumulator_leaves(terms: List[xl.Node], store: xl.Event, name: str, reader: Reader) -> List[xl.Node]:
    """The term of a sum that is the stored name's own earlier value: the one load of it, or of
    several loads of one array the one at the stored element."""
    same = [t for t in terms if t.op == "ld" and reader.name_of_load(t) == name]
    if len(same) > 1 and store.loc[0] == "arr":
        same = [t for t in same if t.args[0][2] == store.loc[2]]
    return same


def combine(kinds: List[str], unrolls: Set[int]) -> str:
    if "C" in kinds:
        return f"C{kinds.count('C') + 1}"
    distinct = set(kinds)
    if distinct == {"S"}:
        return "S"
    if distinct == {"Q"}:
        return "Q"
    if distinct == {"X"}:
        return "X"
    if distinct <= {"P", "X"} and "P" in distinct:
        biggest = max(unrolls)
        return f"B{biggest}" if biggest > 1 else "P"
    return "+".join(sorted(distinct))


def describe(kind: str, kinds: List[str], unrolls: Set[int], counts: Dict[str, int]) -> str:
    if kind.startswith("B"):
        biggest = max(unrolls)
        return (f"blocks of {biggest}: reload at the block head, store after pass {biggest}; "
                f"remainder passes: reload and store every pass")
    if kind == "P":
        return "reload and store every pass"
    if kind == "X":
        return "loaded before the loop, carried in the register, stored every pass"
    if kind.startswith("C"):
        return (f"blocks of {counts['carry'] + 1}: the first pass reads the previous element from memory, the "
                f"other {counts['carry']} from the register; remainder passes read memory")
    return "-"


def register_order(node: xl.Node, reader: Reader, claim_name: str) -> str:
    """The value as the executable makes it: a constant, an expression, or for a loop's merged
    sum the term one pass adds."""
    if node.op not in ("phi", "maybe"):
        return render(node, reader, node)
    for argument in node.args:
        if isinstance(argument, xl.Node) and argument.op == "+":
            own = {id(n) for n, c in ((n, reader.claimed.get(id(n))) for n in addends(argument, reader, argument))
                   if c is not None and c[0] == claim_name}
            terms = [t for t in addends(argument, reader, argument) if t.op not in ("phi", "maybe")
                     and id(t) not in own]
            if terms:
                return "sum of " + render(terms[0], reader, argument)
    return "merge of " + " and ".join(render(a, reader, node) for a in node.args if isinstance(a, xl.Node))


def register_site(row: lm.Row, claim: lr.Claim, reader: Reader, program: lc.Program, texts: Dict[int, str]) -> Site:
    nodes = lc.claimed_nodes(program.map, program.walked, [])[(claim.name, claim.line)]
    reads: List[str] = []
    for node in nodes:
        for argument in node.args:
            if isinstance(argument, xl.Node):
                collect_reads(argument, reader, node, reads, claim.name, True)
    unique = tuple(dict.fromkeys(reads))
    ordered = register_order(nodes[0], reader, claim.name) if nodes else "-"
    return Site(min(a for a, _ in claim.sites), row.id, (claim.line,), claim.name,
                lhs_of(texts, claim.line, claim.name), "r8" if "w8" in claim.op else "r4", "R", "no", "register",
                len(nodes), unique, "-", ordered)
