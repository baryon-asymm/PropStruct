# API.md — defect-report

The node exposes a command line and one Python module. Everything else is internal and
may change.

## Command line ✅

```console
$ python defect_report.py <tree-root> [--output docs/ORIGINAL-DEFECTS.md]
      [--declared-output docs/declared-differences.json] [--check]
defect_report: <n> defects from <k> nodes -> docs/ORIGINAL-DEFECTS.md; <m> declared rows -> docs/declared-differences.json
```

| Argument | Meaning |
|---|---|
| `<tree-root>` | the tree root: the directory holding `AGENTS.md` |
| `--output` | where the page goes; the default is `docs/ORIGINAL-DEFECTS.md` under the root |
| `--declared-output` | where the `Differing cells` JSON twin goes; the default is `docs/declared-differences.json` under the root |
| `--check` | write nothing; exit non-zero when either generated file differs from what would be generated, printing a diff per stale file |

Exit code: `0` — both files written, or both up to date under `--check`; `1` — a
malformed table, a missing input or, under `--check`, either file stale; `2` — bad
arguments.

## Module ✅

```python
def collect(tree_root: str) -> "list[Defect]": ...
def render(defects: "list[Defect]", generated_on: "datetime.date") -> str: ...
def declared_differences(defects: "list[Defect]") -> str: ...

class DeclaredCell(NamedTuple):
    formulations: "tuple[str, ...]"   # ("*",) or the formulation names, as written
    quantity: str                     # the Harness name, literal
    first: "int | None"               # 0-based inclusive, or None
    last: "int | None"                # 0-based inclusive, or None
    original_below: "float | None"    # the 'where original < x' threshold, or None

class Defect(NamedTuple):
    node: str            # path of the node relative to the tree root
    source_line: int     # line of the row in that node's BOOT.md
    fortran: str         # line numbers as written in the row
    kind: str            # algorithm | statistics | numeric | dead | cosmetic
    what: str
    consequence: str
    port: str                        # reproduced | declared, not reproduced | open
    differing_cells: "list[DeclaredCell]"  # parsed 'Differing cells'; [] for 'none'

class DefectFormatError(Exception):
    path: str
    line: int
```

## Input contract

Read by every node that transcribes Fortran lines, to write its own `## Defects of
the original` section (root `BOOT.md`, "The defect report is assembled, never
written"): the table's header is exactly **Fortran | Kind | What the original does |
Consequence | The port | Differing cells**, one row per defect, and nothing else in
that section.

- `Kind` is one of `algorithm` (the model's answer changes), `statistics` (the
  generator or the seeds bias the answer), `numeric` (REAL*4 or x87 rounding), `dead`
  (unreachable, unassigned or unused), `cosmetic` (printing only);
- `Consequence` is the measured effect with the place the measurement lives, or the
  words `not measured`; a guess is not a consequence;
- `The port` is `reproduced`, `declared, not reproduced` or `open`; a defect silently
  fixed is forbidden by the root's own taboo, so that value never appears;
- `Differing cells` names the printed cells in which reference mode under `Original`
  differs from the original because of that row, as `CompareSets` (`tests/Harness`)
  finds them. Its value is `none`, or one backtick span of entries separated by
  `; `: `` `scope: quantity[first..last] where original < threshold` ``. `scope` is
  `*` or a comma-separated list of formulation names; `quantity` is the Harness name,
  literal; the range is optional, 0-based and inclusive (`[i]` or `[i..j]`); the
  `where` clause is optional. Parsed in this order: the scope is the text before the
  first `": "`; a trailing `" where original < x"` is stripped; a trailing `[...]` is
  stripped; the remainder is the quantity. A `reproduced` row's column must be `none`
  — a defect the port reproduces differs from nothing;
- the row is an index into the node's own prose, never a retelling: the prose stays
  where it is and the row links to it.

Formulation names and quantities are not checked here against the reference list:
that is `tests/Harness`'s own domain, and a typo there fails its found-check instead.

## The declared-differences JSON ✅

`docs/declared-differences.json`, `declared_differences`'s own output, holds only the
rows that carry at least one entry, in the same order as the Markdown page (`Kind`,
then the first Fortran line, then node). It carries no date and no source line, so an
edit to a `BOOT.md` section unrelated to `Differing cells` never makes it stale.

```json
{
  "generator": "python tools/defect-report/defect_report.py . --declared-output docs/declared-differences.json",
  "rows": [
    {
      "node": "src/Statistics",
      "fortran": "1021–1065",
      "kind": "dead",
      "port": "declared, not reproduced",
      "entries": [
        {
          "formulations": ["*"],
          "quantity": "pdoksmall",
          "first": null,
          "last": null,
          "originalBelow": 1e-30
        }
      ]
    }
  ]
}
```

The same quantity and scope may be named by more than one row; a consumer matches "at
least one row", never exactly one.

⚠ 2026-10-02: the example was `src/Simulation` and `src/Particle` both declaring
`HPEPA3,HMX: ConditionBreaking(2)`, "for two different mechanisms": one defect in two
rows; `src/Simulation`'s is removed (root `BOOT.md`, "one row per defect").

## Side effects

Reads every `BOOT.md` under the tree root; writes the two output pages, and nothing
else.

## Out of scope

- Whether a row is true of the Fortran: the transcribing node's claim.
- The protocol's own checks: [protocol-lint](../protocol-lint/API.md).
